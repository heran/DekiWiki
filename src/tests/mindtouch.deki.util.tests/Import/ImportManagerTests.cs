/*
 * MindTouch Core - open source enterprise collaborative networking
 * Copyright (c) 2006-2010 MindTouch Inc.
 * www.mindtouch.com  oss@mindtouch.com
 *
 * For community documentation and downloads visit developer.mindtouch.com;
 * please review the licensing section.
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License along
 * with this program; if not, write to the Free Software Foundation, Inc.,
 * 59 Temple Place - Suite 330, Boston, MA 02111-1307, USA.
 * http://www.gnu.org/copyleft/gpl.html
 */

using System;
using System.IO;

using MindTouch.Deki.Import;
using MindTouch.Dream;
using MindTouch.Dream.Test;
using MindTouch.Tasking;
using MindTouch.Xml;
using Moq;
using NUnit.Framework;

namespace MindTouch.Deki.Util.Tests.Import {

    [TestFixture]
    public class ImportManagerTests {

        [TearDown]
        public void Teardown() {
            MockPlug.DeregisterAll();
        }

        [Test]
        public void ImportManager_chains_reader_to_importer() {

            // Arrange
            var dekiApiUri = new XUri("http://mock/@api/deki");
            var importManifest = new XDoc("manifest");
            var item1Uri = dekiApiUri.At("foo", "bar", "abc");
            var item1Doc = new XDoc("item1");
            var item2Uri = dekiApiUri.At("foo", "bar", "def");
            var item2Doc = new XDoc("item2");
            var importResponse = new XDoc("requests")
                .Start("request")
                    .Attr("method", "POST")
                    .Attr("dataid", "abc")
                    .Attr("href", item1Uri)
                    .Start("header").Attr("name", "h_1").Attr("value", "v_1").End()
                    .Start("header").Attr("name", "h_2").Attr("value", "v_2").End()
                .End()
                .Start("request")
                    .Attr("method", "PUT")
                    .Attr("dataid", "def")
                    .Attr("href", item2Uri)
                .End();
            var mock = MockPlug.Register(dekiApiUri);
            mock.Expect().Verb("POST").Uri(dekiApiUri.At("site", "import").With("relto", "0")).RequestDocument(importManifest).Response(DreamMessage.Ok(importResponse));
            mock.Expect().Verb("POST").Uri(item1Uri).RequestHeader("h_1", "v_1").RequestHeader("h_2", "v_2").RequestDocument(item1Doc);
            mock.Expect().Verb("PUT").Uri(item2Uri).RequestDocument(item2Doc);

            var mockPackageReader = new Mock<IPackageReader>();
            mockPackageReader.Setup(x => x.ReadManifest(It.IsAny<Result<XDoc>>())).Returns(importManifest.AsResult()).Verifiable("didn't get manifest");
            var item1stream = new MemoryStream(item1Doc.ToBytes());
            mockPackageReader.Setup(x => x.ReadData(It.Is<ImportItem>(y => y.DataId == "abc"), It.IsAny<Result<ImportItem>>()))
                .Returns(() => new ImportItem("abc", importResponse["request[@dataid='abc']"], null, item1stream, item1stream.Length).AsResult())
                .Verifiable();
            var item2stream = new MemoryStream(item2Doc.ToBytes());
            mockPackageReader.Setup(x => x.ReadData(It.Is<ImportItem>(y => y.DataId == "def"), It.IsAny<Result<ImportItem>>()))
                .Returns(() => new ImportItem("def", importResponse["request[@dataid='def']"], null, item2stream, item2stream.Length).AsResult())
                .Verifiable();
            mockPackageReader.Setup(x => x.Dispose()).Verifiable();
            
            // Act
            var manager = ImportManager.CreateAsync(Plug.New(dekiApiUri), 0, mockPackageReader.Object, new Result<ImportManager>()).Wait();
            manager.ImportAsync(new Result()).Wait();

            //Assert
            Assert.IsTrue(mock.WaitAndVerify(TimeSpan.FromSeconds(1)), mock.VerificationFailure);
            mockPackageReader.VerifyAll();
        }

        [Test]
        public void ImportManager_only_tries_to_read_from_package_when_there_is_a_dataid() {
            var dekiApiUri = new XUri("http://mock/@api/deki");
            var importManifest = new XDoc("manifest");
            var item1Uri = dekiApiUri.At("foo", "bar", "abc");
            var importResponse = new XDoc("requests")
                .Start("request")
                    .Attr("method", "GET")
                    .Attr("href", item1Uri)
                .End();
            var mock = MockPlug.Register(dekiApiUri);
            mock.Expect().Verb("POST").Uri(dekiApiUri.At("site", "import").With("relto", "0")).RequestDocument(importManifest).Response(DreamMessage.Ok(importResponse));
            mock.Expect().Verb("GET").Uri(item1Uri);
            var mockPackageReader = new Mock<IPackageReader>();
            mockPackageReader.Setup(x => x.ReadManifest(It.IsAny<Result<XDoc>>())).Returns(() => importManifest.AsResult());

            // Act
            var manager = ImportManager.CreateAsync(Plug.New(dekiApiUri), 0, mockPackageReader.Object, new Result<ImportManager>()).Wait();
            manager.ImportAsync(new Result()).Wait();

            //Assert
            Assert.IsTrue(mock.WaitAndVerify(TimeSpan.FromSeconds(1)), mock.VerificationFailure);
            mockPackageReader.Verify(x => x.ReadManifest(It.IsAny<Result<XDoc>>()), Times.Once());
            mockPackageReader.Verify(x => x.ReadData(It.IsAny<ImportItem>(), It.IsAny<Result<ImportItem>>()), Times.Never());
        }
    }
}

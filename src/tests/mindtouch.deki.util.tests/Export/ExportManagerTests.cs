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
using System.Collections.Generic;
using MindTouch.Deki.Export;
using MindTouch.Dream;
using MindTouch.Dream.Test;
using MindTouch.Tasking;
using MindTouch.Xml;
using Moq;
using NUnit.Framework;

namespace MindTouch.Deki.Util.Tests.Export {

    [TestFixture]
    public class ExportManagerTests {

        [TearDown]
        public void Teardown() {
            MockPlug.DeregisterAll();
        }

        [Test]
        public void ExportManager_chains_exporter_to_packager() {

            // Arrange
            XUri dekiApiUri = new XUri("http://mock/@api/deki");
            XDoc exportDocument = new XDoc("export");
            XUri item1Uri = dekiApiUri.At("foo", "bar", "abc");
            XDoc item1Doc = new XDoc("item1");
            XUri item2Uri = dekiApiUri.At("foo", "bar", "def");
            XDoc item2Doc = new XDoc("item2");
            XDoc exportResponse = new XDoc("export")
                .Start("requests")
                    .Start("request")
                        .Attr("method", "GET")
                        .Attr("dataid", "abc")
                        .Attr("href", item1Uri)
                        .Start("header").Attr("name", "h_1").Attr("value", "v_1").End()
                        .Start("header").Attr("name", "h_2").Attr("value", "v_2").End()
                    .End()
                    .Start("request")
                        .Attr("method", "GET")
                        .Attr("dataid", "def")
                        .Attr("href", item2Uri)
                    .End()
                .End()
                .Start("manifest")
                    .Start("foo").Attr("dataid", "abc").End()
                    .Start("bar").Attr("dataid", "def").End()
                .End();
            AutoMockPlug mock = MockPlug.Register(dekiApiUri);
            mock.Expect().Verb("POST").Uri(dekiApiUri.At("site", "export").With("relto", "0")).RequestDocument(exportDocument).Response(DreamMessage.Ok(exportResponse));
            mock.Expect().Verb("GET").Uri(item1Uri).RequestHeader("h_1", "v_1").RequestHeader("h_2", "v_2").Response(DreamMessage.Ok(item1Doc));
            mock.Expect().Verb("GET").Uri(item2Uri).Response(DreamMessage.Ok(item2Doc));
            var writes = new List<string>();
            var mockPackageWriter = new Mock<IPackageWriter>();
            mockPackageWriter.Setup(x => x.WriteDataAsync(It.IsAny<ExportItem>(), It.IsAny<Result>()))
                .Returns(() => new Result().WithReturn())
                .Callback((ExportItem item, Result result) => writes.Add(item.DataId))
                .AtMost(2)
                .Verifiable();
            mockPackageWriter.Setup(x => x.WriteManifest(It.IsAny<XDoc>(), It.IsAny<Result>()))
                .Returns(() => new Result().WithReturn())
                .AtMostOnce()
                .Verifiable();

            // Act
            ExportManager manager = ExportManager.CreateAsync(Plug.New(dekiApiUri), exportDocument, 0, mockPackageWriter.Object, new Result<ExportManager>()).Wait();
            manager.ExportAsync(new Result()).Wait();

            // Assert
            Assert.IsTrue(mock.WaitAndVerify(TimeSpan.FromSeconds(1)), mock.VerificationFailure);
            Assert.AreEqual(2, manager.TotalItems);
            Assert.AreEqual(2, manager.CompletedItems);
            Assert.AreEqual(new[] { "abc", "def" }, writes.ToArray());
            mockPackageWriter.Verify(x => x.Dispose(), Times.Once());
            mockPackageWriter.VerifyAll();
        }
    }
}

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
using System.Linq;
using log4net;

using MindTouch.Deki.Import;
using MindTouch.Dream;
using MindTouch.Dream.Test;
using MindTouch.Tasking;
using MindTouch.Xml;

using NUnit.Framework;

namespace MindTouch.Deki.Util.Tests.Import {
    [TestFixture]
    public class ImportTests {


        private static readonly ILog _log = LogUtils.CreateLog();

        [TearDown]
        public void Teardown() {
            MockPlug.DeregisterAll();
        }

        [Test]
        public void Importer_hits_import_feature_with_relto() {

            // Arrange
            XUri dekiApiUri = new XUri("http://mock/@api/deki");
            XDoc importManifest = new XDoc("manifest");
            XDoc importResponse = new XDoc("requests")
                .Start("request").Attr("dataid", "a").End()
                .Start("request").Attr("dataid", "b").End()
                .Start("request").Attr("dataid", "c").End();
            AutoMockPlug mock = MockPlug.Register(dekiApiUri);
            mock.Expect("POST", dekiApiUri.At("site", "import").With("relto", 5.ToString()), importManifest, DreamMessage.Ok(importResponse));

            // Act
            Importer importer = Importer.CreateAsync(Plug.New(dekiApiUri), importManifest, 5, new Result<Importer>()).Wait();

            //Assert
            Assert.IsTrue(mock.WaitAndVerify(TimeSpan.FromSeconds(1)));
            Assert.AreEqual(importManifest, importer.Manifest);
            Assert.AreEqual(new[] {"a","b","c"},importer.Items.Select(x => x.DataId).ToArray());
        }

        [Test]
        public void Importer_hits_import_feature_with_reltopath() {

            // Arrange
            XUri dekiApiUri = new XUri("http://mock/@api/deki");
            XDoc importManifest = new XDoc("manifest");
            XDoc importResponse = new XDoc("requests")
                .Start("request").Attr("dataid", "a").End()
                .Start("request").Attr("dataid", "b").End()
                .Start("request").Attr("dataid", "c").End();
            AutoMockPlug mock = MockPlug.Register(dekiApiUri);
            mock.Expect("POST", dekiApiUri.At("site", "import").With("reltopath", "/foo/bar"), importManifest, DreamMessage.Ok(importResponse));

            // Act
            Importer importer = Importer.CreateAsync(Plug.New(dekiApiUri), importManifest, "/foo/bar", new Result<Importer>()).Wait();

            //Assert
            Assert.IsTrue(mock.WaitAndVerify(TimeSpan.FromSeconds(1)));
            Assert.AreEqual(importManifest, importer.Manifest);
            Assert.AreEqual(new[] { "a", "b", "c" }, importer.Items.Select(x => x.DataId).ToArray());
        }

        [Test]
        public void Importer_Items_are_populated_with_request_and_manifest_docs() {

            // Arrange
            var dekiApiUri = new XUri("http://mock/@api/deki");
            var importManifest = new XDoc("manifest")
                .Start("item").Attr("dataid", "abc").Elem("foo", "bar").End()
                .Start("item").Attr("dataid", "def").Elem("baz", "flip").End();
            var item1Uri = dekiApiUri.At("foo", "bar", "abc");
            var item2Uri = dekiApiUri.At("foo", "bar", "def");
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

            // Act
            var importer = Importer.CreateAsync(Plug.New(dekiApiUri), importManifest, 0, new Result<Importer>()).Wait();

            //Assert
            Assert.IsTrue(mock.WaitAndVerify(TimeSpan.FromSeconds(1)));
            var item1 = importer.Items.Where(x => x.DataId == "abc").FirstOrDefault();
            Assert.IsNotNull(item1);
            Assert.IsNotNull(item1.Manifest);
            Assert.AreEqual(importManifest[".//*[@dataid='abc']"], item1.Manifest);
            Assert.IsNotNull(item1.Request);
            Assert.AreEqual(importResponse[".//*[@dataid='abc']"], item1.Request);
            var item2 = importer.Items.Where(x => x.DataId == "def").FirstOrDefault();
            Assert.IsNotNull(item2);
            Assert.IsNotNull(item2.Manifest);
            Assert.AreEqual(importManifest[".//*[@dataid='def']"], item2.Manifest);
            Assert.IsNotNull(item1.Request);
            Assert.AreEqual(importResponse[".//*[@dataid='def']"], item2.Request);
        }

        [Test]
        public void Importer_can_send_ImportItem_with_stream() {

            // Arrange
            var dekiApiUri = new XUri("http://mock/@api/deki");
            var importManifest = new XDoc("manifest");
            var item1Uri = dekiApiUri.At("foo", "bar", "abc");
            var item1Doc = new XDoc("item1");
            var importResponse = new XDoc("requests");
            var mock = MockPlug.Register(dekiApiUri);
            mock.Expect().Verb("POST").Uri(dekiApiUri.At("site", "import").With("relto", "0")).RequestDocument(importManifest).Response(DreamMessage.Ok(importResponse));
            mock.Expect().Verb("POST").Uri(item1Uri).RequestDocument(item1Doc);

            // Act
            var importer = Importer.CreateAsync(Plug.New(dekiApiUri), importManifest, 0, new Result<Importer>()).Wait();
            var item1Stream = new MemoryStream(item1Doc.ToBytes());
            var item1 = new ImportItem(
                "abc",
                new XDoc("request").Attr("method", "POST").Attr("dataid", "abc").Attr("href", item1Uri),
                new XDoc("manifest"),
                item1Stream,
                item1Stream.Length);
            importer.WriteDataAsync(item1, new Result()).Wait();

            //Assert
            Assert.IsTrue(mock.WaitAndVerify(TimeSpan.FromSeconds(1)));
        }

        [Test]
        public void Importer_can_send_ImportItem_without_any_body() {

            // Arrange
            var dekiApiUri = new XUri("http://mock/@api/deki");
            var importManifest = new XDoc("manifest");
            var item1Uri = dekiApiUri.At("foo", "bar", "abc");
            var importResponse = new XDoc("requests");
            var mock = MockPlug.Register(dekiApiUri);
            mock.Expect().Verb("POST").Uri(dekiApiUri.At("site", "import").With("relto", "0")).RequestDocument(importManifest).Response(DreamMessage.Ok(importResponse));
            mock.Expect().Verb("GET").Uri(item1Uri);

            // Act
            Importer importer = Importer.CreateAsync(Plug.New(dekiApiUri), importManifest, 0, new Result<Importer>()).Wait();
            var item1 = new ImportItem(
                "abc",
                new XDoc("request").Attr("method", "GET").Attr("dataid", "abc").Attr("href", item1Uri),
                new XDoc("manifest"),
                null,
                0);
            importer.WriteDataAsync(item1, new Result()).Wait();

            //Assert
            Assert.IsTrue(mock.WaitAndVerify(TimeSpan.FromSeconds(1)));
        }

        [Test]
        public void Importer_can_send_ImportItem_with_xml_body_in_request_doc() {

            // Arrange
            var dekiApiUri = new XUri("http://mock/@api/deki");
            var importManifest = new XDoc("manifest");
            var item1Uri = dekiApiUri.At("foo", "bar", "abc");
            var importResponse = new XDoc("requests");
            var mock = MockPlug.Register(dekiApiUri);
            mock.Expect().Verb("POST").Uri(dekiApiUri.At("site", "import").With("relto", "0")).RequestDocument(importManifest).Response(DreamMessage.Ok(importResponse));
            mock.Expect().Verb("POST").Uri(item1Uri).RequestDocument(new XDoc("item1").Elem("foo", "bar"));

            // Act
            Importer importer = Importer.CreateAsync(Plug.New(dekiApiUri), importManifest, 0, new Result<Importer>()).Wait();
            var item1 = new ImportItem(
                "abc",
                new XDoc("request")
                    .Attr("method", "POST")
                    .Attr("dataid", "abc")
                    .Attr("href", item1Uri)
                    .Start("body")
                        .Attr("type","xml")
                        .Start("item1").Elem("foo","bar").End()
                    .End(),
                new XDoc("manifest"),
                null,
                0);
            importer.WriteDataAsync(item1, new Result()).Wait();

            //Assert
            Assert.IsTrue(mock.WaitAndVerify(TimeSpan.FromSeconds(1)));
        }

    }
}

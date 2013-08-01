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
using System.IO;
using log4net;

using MindTouch.Deki.Export;
using MindTouch.Dream;
using MindTouch.Dream.Test;
using MindTouch.Tasking;
using MindTouch.Xml;

using NUnit.Framework;

namespace MindTouch.Deki.Util.Tests.Export {
    using Yield = IEnumerator<IYield>;

    [TestFixture]
    public class ExportTests {

        private static readonly ILog _log = LogUtils.CreateLog();

        [TearDown]
        public void Teardown() {
            MockPlug.DeregisterAll();
        }

        [Test]
        public void Exporter_hits_export_feature_on_creation_using_relto() {

            // Arrange
            XUri dekiApiUri = new XUri("http://mock/@api/deki");
            XDoc exportDocument = new XDoc("export")
                .Start("page")
                    .Attr("path", "/")
                    .Attr("recursive", "true")
                    .Attr("exclude", "all")
                .End();
            XDoc exportResponse = new XDoc("export")
                .Start("requests")
                .End()
                .Start("manifest")
                    .Elem("justanode")
                .End();
            AutoMockPlug mock = MockPlug.Register(dekiApiUri);
            mock.Expect("POST", dekiApiUri.At("site", "export").With("relto", 5.ToString()), exportDocument, DreamMessage.Ok(exportResponse));

            // Act
            Exporter exporter = Exporter.CreateAsync(Plug.New(dekiApiUri), exportDocument, 5, new Result<Exporter>()).Wait();

            //Assert
            Assert.IsTrue(mock.WaitAndVerify(TimeSpan.FromSeconds(1)));
            Assert.AreEqual(exportResponse["manifest"], exporter.Manifest);
        }

        [Test]
        public void Exporter_hits_export_feature_on_creation_using_reltopath() {

            // Arrange
            XUri dekiApiUri = new XUri("http://mock/@api/deki");
            XDoc exportDocument = new XDoc("export")
                .Start("page")
                    .Attr("path", "/")
                    .Attr("recursive", "true")
                    .Attr("exclude", "all")
                .End();
            XDoc exportResponse = new XDoc("export")
                .Start("requests")
                .End()
                .Start("manifest")
                    .Elem("justanode")
                .End();
            AutoMockPlug mock = MockPlug.Register(dekiApiUri);
            mock.Expect("POST", dekiApiUri.At("site", "export").With("reltopath", "/foo/bar"), exportDocument, DreamMessage.Ok(exportResponse));

            // Act
            Exporter exporter = Exporter.CreateAsync(Plug.New(dekiApiUri), exportDocument, "/foo/bar", new Result<Exporter>()).Wait();

            //Assert
            Assert.IsTrue(mock.WaitAndVerify(TimeSpan.FromSeconds(1)));
            Assert.AreEqual(exportResponse["manifest"], exporter.Manifest);
        }


        [Test]
        public void Exporter_can_retrieve_items_by_dataid() {

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

            // Act
            Exporter exporter = Exporter.CreateAsync(Plug.New(dekiApiUri), exportDocument, 0, new Result<Exporter>()).Wait();
            ExportItem item1 = exporter.GetItemAsync("abc", new Result<ExportItem>()).Wait();
            ExportItem item2 = exporter.GetItemAsync("def", new Result<ExportItem>()).Wait();

            //Assert
            Assert.IsTrue(mock.WaitAndVerify(TimeSpan.FromSeconds(1)));
            Assert.AreEqual(exportResponse["manifest"], exporter.Manifest);
            Assert.AreEqual(new string[] { "abc", "def" }, exporter.DataIds);
            Assert.AreEqual(exporter.Manifest["*[@dataid='abc']"], item1.ItemManifest);
            Assert.AreEqual(exporter.Manifest["*[@dataid='def']"], item2.ItemManifest);
            Assert.AreEqual(item1Doc, XDocFactory.From(new StreamReader(item1.Data), MimeType.TEXT_XML));
            Assert.AreEqual(item2Doc, XDocFactory.From(new StreamReader(item2.Data), MimeType.TEXT_XML));
        }
    }
}

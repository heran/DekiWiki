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
using System.Linq;
using System.Xml;
using MindTouch.Deki.Export;
using MindTouch.Deki.Import;
using MindTouch.Dream;
using MindTouch.Tasking;
using MindTouch.Xml;

using NUnit.Framework;

namespace MindTouch.Deki.Util.Tests.Packaging {

    [TestFixture]
    public class FilePackagerTests {

        [Test]
        public void Pack_and_unpack_manifest() {

            // Arrange
            string directory = Path.Combine(Path.GetTempPath(), StringUtil.CreateAlphaNumericKey(6));
            Directory.CreateDirectory(directory);
            List<XDoc> docs = new List<XDoc>();
            docs.Add(new XDoc("doc1").Attr("dataid", "a"));
            docs.Add(new XDoc("doc2").Attr("dataid", "b"));
            docs.Add(new XDoc("doc3").Attr("dataid", "c"));
            List<Tuplet<string, MemoryStream>> data = new List<Tuplet<string, MemoryStream>>();
            foreach(XDoc doc in docs) {
                string id = doc["@dataid"].AsText;
                data.Add(new Tuplet<string, MemoryStream>(id, new MemoryStream(doc.ToBytes())));
            }
            XDoc manifest = new XDoc("manifest")
                .Start("page").Attr("dataid", "a").End()
                .Start("page").Attr("dataid", "b").End()
                .Start("page").Attr("dataid", "c").End();

            // Act
            using(FilePackageWriter packageWriter = new FilePackageWriter(directory)) {
                foreach(Tuplet<string, MemoryStream> tuple in data) {
                    var item = new ExportItem(tuple.Item1, tuple.Item2, tuple.Item2.Length, new XDoc("item").Elem("path", "abc"));
                    packageWriter.WriteDataAsync(item, new Result()).Wait();
                }
                packageWriter.WriteManifest(manifest, new Result()).Wait();
            }

            XDoc manifest2;
            List<XDoc> docs2 = new List<XDoc>();
            using(FilePackageReader packageReader = new FilePackageReader(directory)) {
                manifest2 = packageReader.ReadManifest(new Result<XDoc>()).Wait();
                foreach(XDoc id in manifest2["*/@dataid"]) {
                    using(ImportItem item = packageReader.ReadData(new ImportItem(id.AsText, null, null), new Result<ImportItem>()).Wait()) {
                        using(StreamReader reader = new StreamReader(item.Data)) {
                            docs2.Add(XDocFactory.From(reader, MimeType.TEXT_XML));
                        }
                    }
                }
            }

            // Assert
            Assert.IsTrue(File.Exists(Path.Combine(directory, "package.xml")));
            Assert.AreEqual(ToCanonical(manifest), ToCanonical(manifest2));
            Assert.AreEqual(docs.Count, docs2.Count);
            foreach(var doc in docs) {
                Assert.IsTrue(docs2.Select(x => x == doc).Any());
            }
        }

        private string ToCanonical(XDoc doc) {
            var newDoc = new XmlDocument();
            newDoc.PreserveWhitespace = false;
            newDoc.Load(new XmlNodeReader(doc.AsXmlNode));
            return newDoc.OuterXml;
        }
    }
}

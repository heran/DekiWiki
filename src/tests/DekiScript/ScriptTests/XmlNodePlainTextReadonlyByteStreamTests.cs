/*
 * MindTouch DekiScript - embeddable web-oriented scripting runtime
 * Copyright (c) 2006-2010 MindTouch Inc.
 * www.mindtouch.com  oss@mindtouch.com
 *
 * For community documentation and downloads visit wiki.developer.mindtouch.com;
 * please review the licensing section.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 *     http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System.IO;
using System.Text;
using System.Xml;
using MindTouch.Xml;
using NUnit.Framework;

namespace MindTouch.Deki.Script.Tests.ScriptTests {

    [TestFixture]
    public class XmlNodePlainTextReadonlyByteStreamTests {

        [Test]
        public void Can_roundtrip_single_text_element() {
            Compare(
                "sdfdsf slfslelfs dslfslew fsldflslelfldsf swerfwejlwef;hjfwa;onfnksfnwenfskmfsdlkjewnfk",
                "sdfdsf slfslelfs dslfslew fsldflslelfldsf swerfwejlwef;hjfwa;onfnksfnwenfskmfsdlkjewnfk"
            );
        }


        [Test]
        public void Stream_strips_on_save_pattern() {
            Compare("foo bar", "save: foo bar");
        }

        [Test]
        public void Stream_strips_on_subst_pattern() {
            Compare("foo bar", "subst: foo bar");
        }

        [Test]
        public void Stream_strips_on_edit_pattern() {
            Compare("foo bar", "edit: foo bar");
        }

        [Test]
        public void Stream_replaces_non_breaking_space() {
            Compare("sdfdsf sdfsdfds sdfsdfs", "sdfdsf sdfsdfds\u00A0sdfsdfs");
        }

        [Test]
        public void Stream_removes_soft_hyphen() {
            Compare("sdfdsf sdfsdfdssdfdfdfdfdfdfewefs", "sdfdsf sdfsdfds\u00ADsdfdfdfdfdfdfewefs");
        }

        [Test]
        public void Stream_trims_beginning() {
            Compare("sdfdfsdfdsfdsfsdefdsfs", "        sdfdfsdfdsfdsfsdefdsfs");
        }

        [Test]
        public void Stream_trims_around_pattern() {
            Compare("foo bar", "  save:   foo bar");
        }

        [Test]
        public void Stream_strips_all_markup() {
            var doc = new XDoc("code")
                 .Value("public ")
                 .Elem("b", "void")
                 .Value(" ")
                 .Start("i").Value("Foo( ").Elem("b", "int bar ").Value(");").End();
            Compare("public void Foo( int bar );", doc.AsXmlNode);
        }

        [Test]
        public void Reading_in_tiny_buffer_sizes_can_roundtrip() {
            var doc = new XDoc("code")
                 .Value("public ")
                 .Elem("b", "void")
                 .Value(" ")
                 .Start("i").Value("Foo( ").Elem("b", "int bar ").Value(");").End();
            var expected = "public void Foo( int bar );";
            var actual = new StringBuilder();
            var stream = new XmlNodePlainTextReadonlyByteStream(doc.AsXmlNode);
            var buffer = new byte[8];
            var read = stream.Read(buffer, 0, buffer.Length);
            while(read > 0) {
                actual.Append(Encoding.UTF8.GetString(buffer, 0, read));
                read = stream.Read(buffer, 0, buffer.Length);
            }
            Assert.AreEqual(expected, actual.ToString());
        }

        [Test]
        public void Can_read_large_blocks_from_doc_with_large_text_nodes() {
            var builder = new StringBuilder();
            for(var i = 0; i < 10000; i++) {
                builder.AppendFormat("{0:000000}abcdef", i);
            }
            var doc = new XDoc("doc").Elem("a", builder.ToString()).Elem("b", builder.ToString());
            var expected = doc.AsXmlNode.InnerText;
            var actual = new StringBuilder();
            var stream = new XmlNodePlainTextReadonlyByteStream(doc.AsXmlNode);
            var buffer = new byte[32 * 1024];
            var read = stream.Read(buffer, 0, buffer.Length);
            while(read > 0) {
                actual.Append(Encoding.UTF8.GetString(buffer, 0, read));
                read = stream.Read(buffer, 0, buffer.Length);
            }
            Assert.AreEqual(expected, actual.ToString());
        }

        [Test]
        public void Can_read_an_empty_value() {
            Compare("", " ");
        }

        #region Helpers
        private void Compare(string expected, string input) {
            var doc = new XDoc("doc").Value(input);
            Compare(expected, doc.AsXmlNode);
        }

        private void Compare(string expected, XmlNode node) {
            var stream = new XmlNodePlainTextReadonlyByteStream(node);
            using(var reader = new StreamReader(stream)) {
                var read = reader.ReadToEnd();
                Assert.AreEqual(expected, read);
            }
        }
        #endregion
    }
}

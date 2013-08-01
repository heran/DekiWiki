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

using MindTouch.Deki.Script.Expr;
using MindTouch.Deki.Script.Tests.Util;
using MindTouch.Dream;
using MindTouch.Dream.Test;
using MindTouch.Dream.Test.Mock;
using MindTouch.Xml;
using NUnit.Framework;

namespace MindTouch.Deki.Script.Tests.ScriptTests {

    [TestFixture]
    public class DekiScriptLibraryXmlTests {

        //--- Fields ---
        private DekiScriptTester _t;

        // <testxml>
        //    <somedata>	
	    //      <date>01/01/2000</date>
	    //      <text>mindtouch</text>
	    //      <num>42</num>
        //    </somedata>
        //    <somedata>	
        //      <date>5/11/2010</date>
        //      <text>foo</text>
        //      <num>3.14</num>
        //     </somedata>
        //     <some>
        //       <deeply>
        //         <claused>data</claused>
        //       </deeply>
        //     </some>
        // </testxml>

        // melder: cheating a bit, using web.xml() to convert string to XML
        private string testXml = "<testxml><somedata><date>01/01/2000</date><text>mindtouch</text><num>42</num></somedata><somedata><date>5/11/2010</date><text>foo</text><num>3.14</num></somedata><some><deeply><claused>data</claused></deeply></some></testxml>";
        private string simpleXml = "<a><b><c>test</c></b></a>";
        private string xhtml = "<html><body>test</body></html>";

        [SetUp]
        public void Setup() {
            _t = new DekiScriptTester();
        }

        [Test]
        public void Date() {
            _t.Test(
                "xml.date(web.xml(\"" + testXml + "\"), \"somedata/date\");",
                @"Sat, 01 Jan 2000 08:00:00 GMT",
                typeof(DekiScriptString)
            );
        }

        [Test]
        public void Date_EmptyDoc() {
            _t.Test(
                @"xml.date(web.xml(""""));",
                @"nil",
                typeof(DekiScriptNil)
            );
        }

        [Test]
        public void Date_BadXPath() {
            _t.Test(
                "xml.date(web.xml(\"" + testXml + "\"), \"i/dont/exist\");",
                @"nil",
                typeof(DekiScriptNil)
            );
        }

        [Test]
        public void Date_NotADate() {
            _t.Test(
                "xml.date(web.xml(\"" + testXml + "\"), \"somedata/text\");",
                @"nil",
                typeof(DekiScriptNil)
            );
        }

        [Test]
        public void Format_Indented() {
            _t.Test(
                "xml.format(web.xml(\"" + simpleXml + "\"));",
                "<a>\r\n  <b>\r\n    <c>test</c>\r\n  </b>\r\n</a>",
                typeof(DekiScriptString)
            );
        }

        [Test]
        public void Format_XHtml() { // TODO: (melder) find a better example XML -> HTML
            _t.Test(
                "xml.format(web.xml(\"" + simpleXml + "\"), \"xhtml\");",
                simpleXml,
                typeof(DekiScriptString)
            );
        }

        [Test]
        public void Format_Plain() {
            _t.Test(
                "xml.format(web.xml(\"" + simpleXml + "\"), \"plain\");",
                 simpleXml,
                typeof(DekiScriptString)
            );
        }

        [Test]
        public void Html() {
            _t.Test(
                "xml.html(web.xml(\"" + testXml + "\"), \"some/deeply/claused\");",
                "<html><body>data</body></html>",
                typeof(DekiScriptXml)
            );
        }

        [Test]
        public void Html_HtmlDoc() {
            _t.Test(
                "xml.html(web.xml(\"" + xhtml + "\"));",
                xhtml,
                typeof(DekiScriptXml)
            );
        }

        [Test]
        public void Html_EmptyXPath() {
            _t.Test(
                "xml.html(web.xml(\"" + testXml + "\"));",
                // fancy way of removing root element
                "<html><body>" + testXml.Remove(testXml.LastIndexOf("<")).Substring(testXml.IndexOf(">") + 1) + "</body></html>",
                typeof(DekiScriptXml)
            );
        }

        [Test]
        public void List() {
            _t.Test(
                "xml.list(web.xml(\"" + testXml + "\"), \"somedata/num\");",
                @"[ ""42"", ""3.14"" ]",
                typeof(DekiScriptList)
            );
        }

        [Test]
        public void List_EmptyDoc() {
            _t.Test(
                "xml.list(web.xml(\"\"), \"somedata/num\");",
                @"[]",
                typeof(DekiScriptList)
            );
        }

        [Test]
        public void Name() {
            _t.Test(
                "xml.name(web.xml(\"" + testXml + "\"));",
                @"testxml",
                typeof(DekiScriptString)
            );
        }

        [Test]
        public void Name_EmptyDoc() {
            _t.Test(
                "xml.name(web.xml(\"\"));",
                @"nil",
                typeof(DekiScriptNil)
            );
        }

        [Test]
        public void Num() {
            _t.Test(
                "xml.num(web.xml(\"" + testXml + "\"), \"somedata[2]/num\");",
                @"3.14",
                typeof(DekiScriptNumber)
            );
        }

        [Test]
        public void Num_EmptyDoc() {
            _t.Test(
                "xml.num(web.xml(\"\"), \"somedata[2]/num\");",
                @"nil",
                typeof(DekiScriptNil)
            );
        }

        [ExpectedException("MindTouch.Deki.Script.Runtime.DekiScriptInvokeException")]
        [Test]
        public void Num_NotANum() {
            _t.Test(
                "xml.num(web.xml(\"" + testXml + "\"), \"somedata[2]/date\");",
                @"explosion",
                typeof(DekiScriptNil)
            );
        }

        [Test]
        public void Select() {
            _t.Test(
                "xml.select(web.xml(\"" + testXml + "\"), \"some/deeply\");",
                @"<deeply><claused>data</claused></deeply>",
                typeof(DekiScriptXml)
            );
        }

        [Test]
        public void Select_BadXPath() {
            _t.Test(
                "xml.select(web.xml(\"" + testXml + "\"), \"i/dont/exist\");",
                @"",
                typeof(DekiScriptXml)
            );
        }

        [Test]
        public void Text() {
            _t.Test(
                "xml.text(web.xml(\"" + testXml + "\"), \"somedata/text\");",
                @"mindtouch",
                typeof(DekiScriptString)
            );
        }

        [Test]
        public void Text_EmptyDoc() {
            _t.Test(
                "xml.text(web.xml(\"\"), \"somedata/text\");",
                @"nil",
                typeof(DekiScriptNil)
            );
        }

        [Test]
        public void Text_BadXpath() {
            _t.Test(
                "xml.text(web.xml(\"" + testXml + "\"), \"i/dont/exist\");",
                @"nil",
                typeof(DekiScriptNil)
            );
        }

        [Test]
        public void Text_WithElement() {
            _t.Test(
                "xml.text(web.xml(\"" + testXml + "\"), \"somedata/text\", _, true);",
                @"<text>mindtouch</text>",
                typeof(DekiScriptString)
            );
        }

    }
}
/*
 * MindTouch MediaWiki Converter
 * Copyright (C) 2006-2008 MindTouch Inc.
 * www.mindtouch.com  oss@mindtouch.com
 *
 * For community documentation and downloads visit developer.mindtouch.com;
 * please review the licensing section.
 *
 * This library is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 2.1 of the License, or (at your option) any later version.
 * 
 * This library is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 * 
 * You should have received a copy of the GNU Lesser General Public
 * License along with this library; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
 * http://www.gnu.org/copyleft/lesser.html
 */

using MindTouch.Deki.Script.Runtime.Library;
using MindTouch.Dream;
using MindTouch.Xml;

using NUnit.Framework;

namespace MindTouch.Deki.Script.Tests {
    
    [TestFixture]
    public class XhtmlValidatorTests {

        [Test]
        public void SafeXhtml_cleanses_Video_element() {
            XDoc doc = XDocFactory.From("<doc><video src=\"\" poster=\"\" autoplay=\"\" loop=\"\" controls=\"\" width=\"\" height=\"\" onloadstart=\"\" onprogress=\"\"></video></doc>", MimeType.TEXT_XML);
            DekiScriptLibrary.ValidateXHtml(doc, true, true);
            Assert.AreEqual("<doc><video src=\"\" poster=\"\" autoplay=\"\" loop=\"\" controls=\"\" width=\"\" height=\"\"></video></doc>", doc.ToString());
        }

        [Test]
        public void SafeXhtml_cleanses_Video_with_sources() {
            XDoc doc = XDocFactory.From("<doc><video poster=\"\" onloadstart=\"\" onprogress=\"\"><source src=\"\" unsafe=\"\"/></video></doc>", MimeType.TEXT_XML);
            DekiScriptLibrary.ValidateXHtml(doc, true, true);
            Assert.AreEqual("<doc><video poster=\"\"><source src=\"\" /></video></doc>", doc.ToString());
        }

        [Test]
        public void SafeXhtml_cleanses_Video_src_and_poster_attributes() {
            XDoc doc = XDocFactory.From("<doc><video src=\"javascript://\" poster=\"javascript://\" autoplay=\"\" loop=\"\" controls=\"\" width=\"\" height=\"\"></video></doc>", MimeType.TEXT_XML);
            DekiScriptLibrary.ValidateXHtml(doc, true, true);
            Assert.AreEqual("<doc><video autoplay=\"\" loop=\"\" controls=\"\" width=\"\" height=\"\"></video></doc>", doc.ToString());
        }

        [Test]
        public void UnSafeXhtml_cleanses_Video_element() {
            XDoc doc = XDocFactory.From("<doc><video src=\"\" poster=\"\" width=\"\" height=\"\" onloadstart=\"\" onprogress=\"\" unsafe=\"\"></video></doc>", MimeType.TEXT_XML);
            DekiScriptLibrary.ValidateXHtml(doc, false, true);
            Assert.AreEqual("<doc><video src=\"\" poster=\"\" width=\"\" height=\"\" onloadstart=\"\" onprogress=\"\"></video></doc>", doc.ToString());
        }

        [Test]
        public void UnSafeXhtml_cleanses_Video_with_sources() {
            XDoc doc = XDocFactory.From("<doc><video poster=\"\" onloadstart=\"\" onprogress=\"\"><source src=\"\" unsafe=\"\"/></video></doc>", MimeType.TEXT_XML);
            DekiScriptLibrary.ValidateXHtml(doc, false, true);
            Assert.AreEqual("<doc><video poster=\"\" onloadstart=\"\" onprogress=\"\"><source src=\"\" /></video></doc>", doc.ToString());
        }

        [Test]
        public void SafeXhtml_cleanses_Audio_element() {
            XDoc doc = XDocFactory.From("<doc><audio src=\"\" poster=\"\" autoplay=\"\" loop=\"\" controls=\"\" onloadstart=\"\" onprogress=\"\"></audio></doc>", MimeType.TEXT_XML);
            DekiScriptLibrary.ValidateXHtml(doc, true, true);
            Assert.AreEqual("<doc><audio src=\"\" autoplay=\"\" loop=\"\" controls=\"\"></audio></doc>", doc.ToString());
        }

        [Test]
        public void SafeXhtml_cleanses_Audio_src_attribute() {
            XDoc doc = XDocFactory.From("<doc><audio src=\"javascript://\" poster=\"javascript://\" autoplay=\"\" loop=\"\" controls=\"\"></audio></doc>", MimeType.TEXT_XML);
            DekiScriptLibrary.ValidateXHtml(doc, true, true);
            Assert.AreEqual("<doc><audio autoplay=\"\" loop=\"\" controls=\"\"></audio></doc>", doc.ToString());
        }

        [Test]
        public void UnSafeXhtml_cleanses_Audio_element() {
            XDoc doc = XDocFactory.From("<doc><audio src=\"\" poster=\"\" onloadstart=\"\" onprogress=\"\" unsafe=\"\"></audio></doc>", MimeType.TEXT_XML);
            DekiScriptLibrary.ValidateXHtml(doc, false, true);
            Assert.AreEqual("<doc><audio src=\"\" onloadstart=\"\" onprogress=\"\"></audio></doc>", doc.ToString());
        }

        [Test]
        public void SafeXhtml_cleanses_Source_element() {
            XDoc doc = XDocFactory.From("<doc><source src=\"\" poster=\"\" autoplay=\"\" loop=\"\" controls=\"\" onloadstart=\"\" onprogress=\"\"></source></doc>", MimeType.TEXT_XML);
            DekiScriptLibrary.ValidateXHtml(doc, true, true);
            Assert.AreEqual("<doc><source src=\"\"></source></doc>", doc.ToString());
        }

        [Test]
        public void SafeXhtml_cleanses_Source_src_attribute() {
            XDoc doc = XDocFactory.From("<doc><source src=\"javascript://\" poster=\"javascript://\" type=\"\" loop=\"\" controls=\"\"></source></doc>", MimeType.TEXT_XML);
            DekiScriptLibrary.ValidateXHtml(doc, true, true);
            Assert.AreEqual("<doc><source type=\"\"></source></doc>", doc.ToString());
        }

        [Test]
        public void UnSafeXhtml_cleanses_Source_element() {
            XDoc doc = XDocFactory.From("<doc><source src=\"\" poster=\"\" onloadstart=\"\" onprogress=\"\" unsafe=\"\"></source></doc>", MimeType.TEXT_XML);
            DekiScriptLibrary.ValidateXHtml(doc, false, true);
            Assert.AreEqual("<doc><source src=\"\"></source></doc>", doc.ToString());
        }
    }
}

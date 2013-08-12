/*
 * MindTouch DekiScript - embeddable web-oriented scripting runtime
 * Copyright (c) 2006-2010 MindTouch Inc.
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
using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;

namespace MindTouch.Deki.Script.Tests {

    [TestFixture]
    public class SimpleHtmlFormatterTests {

        [Test]
        public void Null_creates_empty_doc() {
            AssertHtml("<html><body><p></p>\n</body></html>", null);
        }

        [Test]
        public void Empty_string_creates_empty_doc() {
            AssertHtml("<html><body><p></p>\n</body></html>", "");
        }

        [Test]
        public void Entities_get_converted() {
            AssertHtml("<html><body><p>&amp;&lt;&gt;&quot;</p>\n</body></html>", "&<>\"");
        }

        [Test]
        public void Html_string_gets_encoded() {
            AssertHtml("<html><body><p>&lt;html&gt;&lt;body&gt;&lt;a href=&quot;http://foo.com/&quot;&gt;x&lt;/a&gt;&lt;/body&gt;&lt;/html&gt;</p>\n</body></html>", "<html><body><a href=\"http://foo.com/\">x</a></body></html>");
        }

        [Test]
        public void Running_spaces_create_nbsp_runs() {
            AssertHtml("<html><body><p>foo&nbsp; bar&nbsp;&nbsp; baz</p>\n</body></html>", "foo  bar   baz");
        }

        [Test]
        public void Double_line_breaks_define_paragraphs() {
            AssertHtml(
@"<html><body><p>Lorem ipsum dolor sit amet, consectetur adipiscing elit.</p>
<p>Sed sit amet orci orci. Phasellus eleifend facilisis sollicitudin. Sed quis augue odio.</p>
<p>Nam varius aliquet orci quis elementum. Donec in dapibus eros.</p>
<p>Pellentesque habitant morbi tristique senectus et netus et malesuada.</p>
<p>Morbi eu sem nec velit posuere elementum. Morbi in dolor ac purus imperdiet</p>
</body></html>",

@"Lorem ipsum dolor sit amet, consectetur adipiscing elit.

Sed sit amet orci orci. Phasellus eleifend facilisis sollicitudin. Sed quis augue odio.

Nam varius aliquet orci quis elementum. Donec in dapibus eros.

Pellentesque habitant morbi tristique senectus et netus et malesuada.

Morbi eu sem nec velit posuere elementum. Morbi in dolor ac purus imperdiet");
        }

        [Test]
        public void Triple_and_more_line_breaks_also_define_paragraphs() {
            AssertHtml(
@"<html><body><p>Lorem ipsum dolor sit amet, consectetur adipiscing elit.</p>
<p>Sed sit amet orci orci. Phasellus eleifend facilisis sollicitudin. Sed quis augue odio.</p>
<p>Nam varius aliquet orci quis elementum. Donec in dapibus eros.</p>
<p>Pellentesque habitant morbi tristique senectus et netus et malesuada.</p>
<p>Morbi eu sem nec velit posuere elementum. Morbi in dolor ac purus imperdiet</p>
</body></html>",

@"Lorem ipsum dolor sit amet, consectetur adipiscing elit.


Sed sit amet orci orci. Phasellus eleifend facilisis sollicitudin. Sed quis augue odio.




Nam varius aliquet orci quis elementum. Donec in dapibus eros.

Pellentesque habitant morbi tristique senectus et netus et malesuada.


Morbi eu sem nec velit posuere elementum. Morbi in dolor ac purus imperdiet");
        }

        [Test]
        public void Single_line_breaks_define_break_elements() {
            AssertHtml(
@"<html><body><p>Lorem ipsum dolor sit amet, consectetur adipiscing elit.<br />
Sed sit amet orci orci. Phasellus eleifend facilisis sollicitudin. Sed quis augue odio.<br />
Nam varius aliquet orci quis elementum. Donec in dapibus eros.<br />
Pellentesque habitant morbi tristique senectus et netus et malesuada.<br />
Morbi eu sem nec velit posuere elementum. Morbi in dolor ac purus imperdiet</p>
</body></html>",

@"Lorem ipsum dolor sit amet, consectetur adipiscing elit.
Sed sit amet orci orci. Phasellus eleifend facilisis sollicitudin. Sed quis augue odio.
Nam varius aliquet orci quis elementum. Donec in dapibus eros.
Pellentesque habitant morbi tristique senectus et netus et malesuada.
Morbi eu sem nec velit posuere elementum. Morbi in dolor ac purus imperdiet");
        }

        [Test]
        public void Single_and_double_line_breaks_define_paragraphs_and_simple_breaks() {
            AssertHtml(
@"<html><body><p>Lorem ipsum dolor sit amet, consectetur adipiscing elit.<br />
Sed sit amet orci orci. Phasellus eleifend facilisis sollicitudin. Sed quis augue odio.</p>
<p>Nam varius aliquet orci quis elementum. Donec in dapibus eros.<br />
Pellentesque habitant morbi tristique senectus et netus et malesuada.</p>
<p>Morbi eu sem nec velit posuere elementum. Morbi in dolor ac purus imperdiet</p>
</body></html>",

@"Lorem ipsum dolor sit amet, consectetur adipiscing elit.
Sed sit amet orci orci. Phasellus eleifend facilisis sollicitudin. Sed quis augue odio.

Nam varius aliquet orci quis elementum. Donec in dapibus eros.
Pellentesque habitant morbi tristique senectus et netus et malesuada.

Morbi eu sem nec velit posuere elementum. Morbi in dolor ac purus imperdiet");
        }


        private void AssertHtml(string html, string input) {
            Assert.AreEqual(html.Replace("\r\n", "\n"), SimpleHtmlFormatter.Format(input).ToXHtml().Replace("\r\n", "\n"));
        }
    }
}

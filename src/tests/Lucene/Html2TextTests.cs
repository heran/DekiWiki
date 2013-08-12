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
using MindTouch.Dream;
using MindTouch.LuceneService;
using MindTouch.Xml;
using NUnit.Framework;

namespace MindTouch.Lucene.Tests {
    
    [TestFixture]
    public class Html2TextTests {

        //--- Class Methods ---
        private static void AssertConversion(string html, string text) {
            var doc = XDocFactory.From(html, MimeType.TEXT_XML);
            Assert.AreEqual(text, new Html2Text().Convert(doc));
        }

        [Test]
        public void Can_convert_html() {
            AssertConversion(@"<html><body><script>script</script><h1>Title</h1><div>Paragraph with <b>bold</b> text</div><style>style</style><p class=""noindex"">don't index</p><div>Paragraph with <i>italic</i> text</div></body></html>",
                @"Title
Paragraph with bold text
Paragraph with italic text
");
        }

        [Test]
        public void NoIndex_class_is_omitted_from_output() {
            AssertConversion(
                "<html><body>foo<i class=\"noindex\">bar</i>baz</body></html>",
                "foobaz\r\n"
            );
        }

        [Test]
        public void Script_is_removed() {
            AssertConversion(
                "<html><body>foo<script>bar</script>baz</body></html>",
                "foobaz\r\n"
            );
        }

        [Test]
        public void Style_is_removed() {
            AssertConversion(
                "<html><body>foo<style>bar</style>baz</body></html>",
                "foobaz\r\n"
            );
        }

        [Test]
        public void Only_body_content_is_considered() {
            AssertConversion(
                "<html>foo<body>bar</body>baz</html>",
                "bar\r\n"
            );
        }

        [Test]
        public void Body_with_target_is_ignored() {
            AssertConversion(
                "<html><body target=\"first\">foo</body><body>bar</body></html>",
                "bar\r\n"
            );
        }

        [Test]
        public void Block_elements_add_leading_and_trailing_linefeed() {
            AssertConversion(
                "<html><body>foo<div>bar</div>baz</body></html>",
                "foo\r\nbar\r\nbaz\r\n"
            );
        }

        [Test]
        public void Non_block_elements_are_just_removed() {
            AssertConversion(
                "<html><body>foo<i>bar</i>baz</body></html>",
                "foobarbaz\r\n"
            );
        }

        [Test]
        public void Whitespace_stays_intact() {
            AssertConversion(
                "<html><body><i>foo</i> <i>bar</i></body></html>",
                "foo bar\r\n"
            );
        }
    }
}

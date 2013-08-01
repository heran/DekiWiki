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
using System.Linq;
using MindTouch.Deki.Search;
using NUnit.Framework;

namespace MindTouch.Deki.Util.Tests.Search {

    [TestFixture]
    public class LuceneClauseParserTests {

        [Test]
        public void Can_parse_single_simple_unprefixed_term() {
            var c = LuceneClauseParser.Parse("foo");
            Assert.AreEqual(1, c.Unprefixed.Count());
            Assert.AreEqual(0, c.PlusPrefixed.Count());
            Assert.AreEqual(0, c.MinusPrefixed.Count());
            Assert.AreEqual("foo", c.Unprefixed.First());
        }

        [Test]
        public void Can_parse_single_simple_plus_prefixed_term() {
            var c = LuceneClauseParser.Parse("+foo");
            Assert.AreEqual(0, c.Unprefixed.Count());
            Assert.AreEqual(1, c.PlusPrefixed.Count());
            Assert.AreEqual(0, c.MinusPrefixed.Count());
            Assert.AreEqual("+foo", c.PlusPrefixed.First());
        }

        [Test]
        public void Can_parse_single_simple_minus_prefixed_term() {
            var c = LuceneClauseParser.Parse("-foo");
            Assert.AreEqual(0, c.Unprefixed.Count());
            Assert.AreEqual(0, c.PlusPrefixed.Count());
            Assert.AreEqual(1, c.MinusPrefixed.Count());
            Assert.AreEqual("-foo", c.MinusPrefixed.First());
        }

        [Test]
        public void Can_parse_single_parenthesized_unprefixed_term() {
            var c = LuceneClauseParser.Parse("(foo)");
            Assert.AreEqual(1, c.Unprefixed.Count());
            Assert.AreEqual(0, c.PlusPrefixed.Count());
            Assert.AreEqual(0, c.MinusPrefixed.Count());
            Assert.AreEqual("(foo)", c.Unprefixed.First());
        }

        [Test]
        public void Can_parse_single_parenthesized_plus_prefixed_term() {
            var c = LuceneClauseParser.Parse("+(foo)");
            Assert.AreEqual(0, c.Unprefixed.Count());
            Assert.AreEqual(1, c.PlusPrefixed.Count());
            Assert.AreEqual(0, c.MinusPrefixed.Count());
            Assert.AreEqual("+(foo)", c.PlusPrefixed.First());
        }

        [Test]
        public void Can_parse_single_parenthesized_minus_prefixed_term() {
            var c = LuceneClauseParser.Parse("-(foo)");
            Assert.AreEqual(0, c.Unprefixed.Count());
            Assert.AreEqual(0, c.PlusPrefixed.Count());
            Assert.AreEqual(1, c.MinusPrefixed.Count());
            Assert.AreEqual("-(foo)", c.MinusPrefixed.First());
        }

        [Test]
        public void Can_parse_terms_with_quotes() {
            var c = LuceneClauseParser.Parse("+foo:\"bar\" -a:\"b\" x:\"y\"");
            Assert.AreEqual(1, c.Unprefixed.Count());
            Assert.AreEqual(1, c.PlusPrefixed.Count());
            Assert.AreEqual(1, c.MinusPrefixed.Count());
            Assert.AreEqual(new[] { "x:\"y\"" }, c.Unprefixed.ToArray());
            Assert.AreEqual(new[] { "+foo:\"bar\"" }, c.PlusPrefixed.ToArray());
            Assert.AreEqual(new[] { "-a:\"b\"" }, c.MinusPrefixed.ToArray());
        }

        [Test]
        public void Can_parse_terms_with_parentheses() {
            var c = LuceneClauseParser.Parse("a:(b) +c:(d e) -(f g)");
            Assert.AreEqual(1, c.Unprefixed.Count());
            Assert.AreEqual(1, c.PlusPrefixed.Count());
            Assert.AreEqual(1, c.MinusPrefixed.Count());
            Assert.AreEqual(new[] { "a:(b)" }, c.Unprefixed.ToArray());
            Assert.AreEqual(new[] { "+c:(d e)" }, c.PlusPrefixed.ToArray());
            Assert.AreEqual(new[] { "-(f g)" }, c.MinusPrefixed.ToArray());
        }

        [Test]
        public void Can_parse_terms_with_escaped_chars() {
            var c = LuceneClauseParser.Parse("a\\b -\"d\\e\" +\\c");
            Assert.AreEqual(1, c.Unprefixed.Count());
            Assert.AreEqual(1, c.PlusPrefixed.Count());
            Assert.AreEqual(1, c.MinusPrefixed.Count());
            Assert.AreEqual(new[] { "a\\b" }, c.Unprefixed.ToArray());
            Assert.AreEqual(new[] { "+\\c" }, c.PlusPrefixed.ToArray());
            Assert.AreEqual(new[] { "-\"d\\e\"" }, c.MinusPrefixed.ToArray());
        }

        [Test]
        public void Extra_spaces_are_ignored_except_in_term() {
            var c = LuceneClauseParser.Parse("  abc  +(foo bar)  -\"sdfsf\"  ");
            Assert.AreEqual(1, c.Unprefixed.Count());
            Assert.AreEqual(1, c.PlusPrefixed.Count());
            Assert.AreEqual(1, c.MinusPrefixed.Count());
            Assert.AreEqual(new[] { "abc" }, c.Unprefixed.ToArray());
            Assert.AreEqual(new[] { "+(foo bar)" }, c.PlusPrefixed.ToArray());
            Assert.AreEqual(new[] { "-\"sdfsf\"" }, c.MinusPrefixed.ToArray());
        }

        [Test]
        public void Can_parse_parenthesized_complex_clause() {
            var clause = "(title:+foo:\"blah((\"\\x OR foo* +bar something:(some thing))";
            var c = LuceneClauseParser.Parse(clause);
            Assert.AreEqual(1, c.Unprefixed.Count());
            Assert.AreEqual(0, c.PlusPrefixed.Count());
            Assert.AreEqual(0, c.MinusPrefixed.Count());
            Assert.AreEqual(clause, c.Unprefixed.First());
        }
        [Test]
        public void Can_parse_multiple_simple_terms() {
            var c = LuceneClauseParser.Parse("a b c");
            Assert.AreEqual(new[] { "a", "b","c" }, c.Unprefixed.ToArray());
            Assert.AreEqual(new string[0], c.PlusPrefixed.ToArray());
            Assert.AreEqual(new string[0], c.MinusPrefixed.ToArray());
        }

        [Test]
        public void Can_parse_mixed_terms() {
            var c = LuceneClauseParser.Parse("a +b (c d) -(+d x) -e");
            Assert.AreEqual(new[] { "a", "(c d)" }, c.Unprefixed.ToArray());
            Assert.AreEqual(new[] { "+b" }, c.PlusPrefixed.ToArray());
            Assert.AreEqual(new[] { "-(+d x)", "-e" }, c.MinusPrefixed.ToArray());
        }

        [Test]
        public void Plus_in_term_does_not_make_it_plus_prefixed() {
            var c = LuceneClauseParser.Parse("foo+bar");
            Assert.AreEqual(1, c.Unprefixed.Count());
            Assert.AreEqual(0, c.PlusPrefixed.Count());
            Assert.AreEqual(0, c.MinusPrefixed.Count());
            Assert.AreEqual("foo+bar", c.Unprefixed.First());
        }

        [Test]
        public void Unmatched_parentheses_shortcircuits_parse() {
            var c = LuceneClauseParser.Parse("foo(bar");
            Assert.AreEqual(1, c.Unprefixed.Count());
            Assert.AreEqual(0, c.PlusPrefixed.Count());
            Assert.AreEqual(0, c.MinusPrefixed.Count());
            Assert.AreEqual("(foo(bar)", c.Unprefixed.First());
        }

        [Test]
        public void Unclosed_quotes_shortcircuit_parse() {
            var c = LuceneClauseParser.Parse("foo\"bar");
            Assert.AreEqual(1, c.Unprefixed.Count());
            Assert.AreEqual(0, c.PlusPrefixed.Count());
            Assert.AreEqual(0, c.MinusPrefixed.Count());
            Assert.AreEqual("(foo\"bar)", c.Unprefixed.First());
        }

        [Test]
        public void Trailing_escape_shortcircuits_parse() {
            var c = LuceneClauseParser.Parse("foo\\");
            Assert.AreEqual(1, c.Unprefixed.Count());
            Assert.AreEqual(0, c.PlusPrefixed.Count());
            Assert.AreEqual(0, c.MinusPrefixed.Count());
            Assert.AreEqual("(foo\\)", c.Unprefixed.First());
        }

        [Test]
        public void Reserved_word_shortcircuits_parse() {
            var c = LuceneClauseParser.Parse("-a AND +b");
            Assert.AreEqual(1, c.Unprefixed.Count());
            Assert.AreEqual(0, c.PlusPrefixed.Count());
            Assert.AreEqual(0, c.MinusPrefixed.Count());
            Assert.AreEqual("(-a AND +b)", c.Unprefixed.First());
        }

        [Test]
        public void Can_parse_nested_parentheses_terms() {
            var c = LuceneClauseParser.Parse("a(b(c (d)e))");
            Assert.AreEqual(1, c.Unprefixed.Count());
            Assert.AreEqual(0, c.PlusPrefixed.Count());
            Assert.AreEqual(0, c.MinusPrefixed.Count());
            Assert.AreEqual("a(b(c (d)e))", c.Unprefixed.First());
        }

        [Test]
        public void Quoted_parentheses_do_not_throw_off_parenethese_balancing() {
            var c = LuceneClauseParser.Parse("a(\")\")");
            Assert.AreEqual(1, c.Unprefixed.Count());
            Assert.AreEqual(0, c.PlusPrefixed.Count());
            Assert.AreEqual(0, c.MinusPrefixed.Count());
            Assert.AreEqual("a(\")\")", c.Unprefixed.First());
        }
    }
}

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
using System.Linq;
using MindTouch.Deki.Search;
using NUnit.Framework;

// ReSharper disable InconsistentNaming
#pragma warning disable 1591
namespace MindTouch.Deki.Util.Tests.Search {

    [TestFixture]
    public class SearchQueryParserTests {

        //--- Fields ---
        private readonly SearchQueryParser _parser = new SearchQueryParser();

        [Test]
        public void Can_parse_simple_term_string() {
            Assert.AreEqual(new[] { "abc", "def" }, _parser.GetQueryTerms("abc def", false).ToEscaped());
        }

        [Test]
        public void Leading_whitespace_is_ignored() {
            Assert.AreEqual(new[] { "abc", "def" }, _parser.GetQueryTerms("   abc def", false).ToEscaped());
        }

        [Test]
        public void Trailing_whitespace_is_ignored() {
            Assert.AreEqual(new[] { "abc", "def" }, _parser.GetQueryTerms("abc def   ", false).ToEscaped());
        }

        [Test]
        public void Extra_whitespace_is_ignored() {
            Assert.AreEqual(new[] { "abc", "def" }, _parser.GetQueryTerms("abc   def", false).ToEscaped());
        }

        [Test]
        public void Term_parse_dedups_list() {
            Assert.AreEqual(new[] { "abc", "def" }, _parser.GetQueryTerms("abc def abc", false).ToEscaped());
        }

        [Test]
        public void Term_dedup_uses_case_ignorant_comparison() {
            Assert.AreEqual(new[] { "abc", "def" }, _parser.GetQueryTerms("abc def Abc", false).ToEscaped());
        }

        [Test]
        public void Term_parse_does_not_dedup_based_on_normalized_value() {
            var terms = _parser.GetQueryTerms("abc* def \"abc*\"", false);
            Assert.AreEqual(new[] { "abc*", "abc\\*", "def" }, terms.ToEscaped());
            Assert.AreEqual(new[] { "abc*", "abc*", "def" }, terms.ToNormalized());
        }

        [Test]
        public void Normalized_is_lowercase() {
            Assert.AreEqual(new[] { "abc", "def" }, _parser.GetQueryTerms("Abc dEf ", false).ToNormalized());
        }

        [Test]
        public void Term_parse_leaves_known_field_alone() {
            Assert.AreEqual("path:baz", "path:baz".ToEscapedTerm());
        }

        [Test]
        public void Term_parse_recognizes_field_with_leading_plus() {
            Assert.AreEqual("+path:baz", "+path:baz".ToEscapedTerm());
        }

        [Test]
        public void Term_parse_recognizes_field_with_leading_minus() {
            Assert.AreEqual("-path:baz", "-path:baz".ToEscapedTerm());
        }

        [Test]
        public void Term_parse_recognizes_properties() {
            Assert.AreEqual("xyz#abc:baz", "xyz#abc:baz".ToEscapedTerm());
        }

        [Test]
        public void Term_parse_recognizes_no_namespace_properties() {
            Assert.AreEqual("#abc:baz", "#abc:baz".ToEscapedTerm());
        }

        [Test]
        public void Term_parse_escapes_special_chars() {
            Assert.AreEqual("foo\\:baz", "foo:baz".ToEscapedTerm());
        }

        [Test]
        public void Term_parse_strips_quotes_escapes() {
            Assert.AreEqual("foo\\-baz", "\"foo-baz\"".ToEscapedTerm());
        }

        [Test]
        public void Term_parse_treats_quoted_string_as_single_term() {
            var terms = _parser.GetQueryTerms("\"Foo baz\" bar", false);
            Assert.AreEqual(new[] { "bar", "foo baz" }, terms.ToNormalized(), "incorrect normalized terms");
            Assert.AreEqual(new[] { "bar", "Foo\\ baz" }, terms.ToEscaped(), "incorrect escaped terms");
        }

        [Test]
        public void Whitespace_in_quoted_string_gets_escaped() {
            Assert.AreEqual("title\\ baz", "\"title baz\"".ToEscapedTerm());
        }

        [Test]
        public void Whitespace_in_prefixed_quoted_string_gets_escaped() {
            Assert.AreEqual("tag:title\\ baz", "tag:\"title baz\"".ToEscapedTerm());
        }

        [Test]
        public void Escaped_whitespace_quoted_string_stays_escaped() {
            Assert.AreEqual("title\\ baz", "\"title\\ baz\"".ToEscapedTerm());
        }

        [Test]
        public void Special_characters_in_quoted_string_get_escaped() {
            Assert.AreEqual("title\\-baz", "\"title-baz\"".ToEscapedTerm());
        }

        [Test]
        public void Special_characters_in_prefixed_quoted_string_get_escaped() {
            Assert.AreEqual("title:foo\\:bar\\-baz", "title:\"foo:bar-baz\"".ToEscapedTerm());
        }

        [Test]
        public void Escaped_special_characters_in_quoted_string_stay_escaped() {
            Assert.AreEqual("title\\-baz", "\"title\\-baz\"".ToEscapedTerm());
        }

        [Test]
        public void Special_characters_in_quoted_string_are_not_escaped_in_normalized_version() {
            Assert.AreEqual("title-baz", "\"title-baz\"".ToNormalizedTerm());
        }

        [Test]
        public void Escaped_special_characters_in_quoted_string_are_not_escaped_in_normalized_version() {
            Assert.AreEqual("title-baz", "\"title\\-baz\"".ToNormalizedTerm());
        }

        [Test]
        public void Escaped_quote_in_quoted_string_stays_escaped() {
            Assert.AreEqual("Foo\\\"baz", "\"Foo\\\"baz\"".ToEscapedTerm());
        }

        [Test]
        public void Escaped_quote_in_quoted_string_in_unescaped_in_normalized_term() {
            Assert.AreEqual("foo\"baz", "\"Foo\\\"baz\"".ToNormalizedTerm());
        }

        [Test]
        public void Escaped_backslash_in_quoted_string_stays_escaped() {
            Assert.AreEqual("Foo\\\\baz", "\"Foo\\\\baz\"".ToEscapedTerm());
        }

        [Test]
        public void Escaped_backslash_in_quoted_string_in_unescaped_in_normalized_term() {
            Assert.AreEqual("foo\\baz", "\"Foo\\\\baz\"".ToNormalizedTerm());
        }

        [Test]
        [ExpectedException(typeof(FormatException))]
        public void Unclosed_quoted_term_throws_with_abort_set() {
            _parser.GetQueryTerms("foo \"blah", true);
        }

        [Test]
        [ExpectedException(typeof(FormatException))]
        public void Unclosed_quoted_term_throws_with_abort_unset() {
            _parser.GetQueryTerms("foo \"blah", false);
        }

        [Test]
        public void Escaped_quote_in_unquoted_term_gets_parsed() {
            Assert.AreEqual("foo\\\"bar", "foo\\\"bar".ToEscapedTerm());
        }

        [Test]
        [ExpectedException(typeof(FormatException))]
        public void Unescaped_quote_in_unquoted_term_throws() {
            _parser.GetQueryTerms("foo \"blah", false);
        }

        [Test]
        [ExpectedException(typeof(FormatException))]
        public void Unescaped_quote_in_middle_of_quoted_term_throws() {
            _parser.GetQueryTerms("\"foo \"blah\"", false);
        }

        [Test]
        public void Leading_plus_in_quoted_string_gets_escaped() {
            Assert.AreEqual("\\+foo", "\"+foo\"".ToEscapedTerm());
        }

        [Test]
        public void Leading_minus_in_quoted_string_gets_escaped() {
            Assert.AreEqual("\\-foo", "\"-foo\"".ToEscapedTerm());
        }

        [Test]
        public void Embedded_plus_in_quoted_string_gets_escaped() {
            Assert.AreEqual("bar\\+foo", "\"bar+foo\"".ToEscapedTerm());
        }

        [Test]
        public void Embedded_minus_in_quoted_string_gets_escaped() {
            Assert.AreEqual("bar\\-foo", "\"bar-foo\"".ToEscapedTerm());
        }

        [Test]
        public void Leading_plus_does_not_get_escaped_with_abort_unset() {
            Assert.AreEqual("+foo", "+foo".ToEscapedTerm());
        }

        [Test]
        public void Leading_plus_does_not_abort_with_abort_set() {
            Assert.AreEqual("+foo", "+foo".ToEscapedTermOrAbort());
        }

        [Test]
        public void Leading_minus_does_not_get_escaped_with_abort_unset() {
            Assert.AreEqual("-foo", "-foo".ToEscapedTerm());
        }

        [Test]
        public void Leading_mins_does_not_abort_with_abort_set() {
            Assert.AreEqual("-foo", "-foo".ToEscapedTermOrAbort());
        }

        [Test]
        public void Plus_in_term_gets_escaped_with_abort_unset() {
            Assert.AreEqual("bar\\+foo", "bar+foo".ToEscapedTerm());
        }

        [Test]
        public void Plus_in_term_gets_escaped_with_abort_set() {
            Assert.AreEqual("bar\\+foo", "bar+foo".ToEscapedTermOrAbort());
        }

        [Test]
        public void Minus_in_term_gets_escaped_with_abort_unset() {
            Assert.AreEqual("bar\\-foo", "bar-foo".ToEscapedTerm());
        }

        [Test]
        public void Minus_in_term_gets_escaped_with_abort_set() {
            Assert.AreEqual("bar\\-foo", "bar-foo".ToEscapedTermOrAbort());
        }

        [Test]
        public void Plus_leading_prefixed_term_gets_escaped_with_abort_unset() {
            Assert.AreEqual("title:\\+foo", "title:+foo".ToEscapedTerm());
        }

        [Test]
        public void Plus_leading_prefixed_term_gets_escaped_with_abort_set() {
            Assert.AreEqual("title:\\+foo", "title:+foo".ToEscapedTermOrAbort());
        }

        [Test]
        public void Minus_leading_prefixed_term_gets_escaped_with_abort_unset() {
            Assert.AreEqual("title:\\-foo", "title:-foo".ToEscapedTerm());
        }

        [Test]
        public void Minus_leading_prefixed_term_gets_escaped_with_abort_set() {
            Assert.AreEqual("title:\\-foo", "title:-foo".ToEscapedTermOrAbort());
        }

        [Test]
        public void Wildcard_star_in_quoted_vs_unquoted_string_is_ambiguous_in_normalized_form() {
            Assert.AreEqual(
                "f*oo".ToEscapedTerm(),
               "\"f*oo\"".ToNormalizedTerm());
        }

        [Test]
        public void Wildcard_questionmark_in_quoted_vs_unquoted_string_is_ambiguous_in_normalized_form() {
            Assert.AreEqual(
                "f?oo".ToEscapedTerm(),
                "\"f?oo\"".ToNormalizedTerm());
        }

        [Test]
        public void Leading_minus_in_quoted_vs_unquoted_string_is_ambiguous_in_normalized_form() {
            Assert.AreEqual(
                "-foo".ToEscapedTerm(),
                "\"-foo\"".ToNormalizedTerm());
        }

        [Test]
        public void Leading_plus_gets_dropped_in_normalized_form() {
            Assert.AreEqual("foo", "+foo".ToNormalizedTerm());
        }

        [Test]
        public void Wildcard_star_is_valid() {
            Assert.AreEqual("fo*o*", "fo*o*".ToEscapedTerm());
        }

        [Test]
        public void Wildcard_questionmark_is_valid() {
            Assert.AreEqual("fo?o?", "fo?o?".ToEscapedTerm());
        }

        [Test]
        public void Wildcard_star_in_quoted_string_gets_escaped() {
            Assert.AreEqual("fo\\*o\\*", "\"fo*o*\"".ToEscapedTerm());
        }

        [Test]
        public void Wildcard_questionmark_in_quoted_string_gets_escaped() {
            Assert.AreEqual("fo\\?o\\?", "\"fo?o?\"".ToEscapedTerm());
        }

        [Test]
        public void Unescaped_wildcard_in_prefix_not_treated_as_prefix() {
            Assert.AreEqual("t?tle\\:bar", "t?tle:bar".ToEscapedTerm());
        }

        [Test]
        public void Trailing_field_colon_gets_escaped() {
            Assert.AreEqual("bar\\:", "bar:".ToEscapedTerm());
        }

        [Test]
        public void Boost_gets_escaped_with_abort_unset() {
            Assert.AreEqual("foo\\^4", "foo^4".ToEscapedTerm());
        }

        [Test]
        public void Quoted_boost_gets_escaped_with_abort_unset() {
            Assert.AreEqual("foo\\^4", "\"foo^4\"".ToEscapedTerm());
        }

        [Test]
        public void Quoted_boost_gets_escaped_with_abort_set() {
            Assert.AreEqual("foo\\^4", "\"foo^4\"".ToEscapedTermOrAbort());
        }

        [Test]
        public void Boost_shortcircuits_with_abort_set() {
            Assert.IsNull(_parser.GetQueryTerms("foo^4", true));
        }

        [Test]
        public void AND_gets_quoted_with_abort_unset() {
            Assert.AreEqual("\"AND\"", "AND".ToEscapedTerm());
        }

        [Test]
        public void AND_shortcircuits_with_abort_set() {
            Assert.IsNull(_parser.GetQueryTerms("AND", true));
        }

        [Test]
        public void OR_gets_quoted_with_abort_unset() {
            Assert.AreEqual("\"OR\"", "OR".ToEscapedTerm());
        }

        [Test]
        public void OR_shortcircuits_with_abort_set() {
            Assert.IsNull(_parser.GetQueryTerms("OR", true));
        }

        [Test]
        public void NOT_gets_quoted_with_abort_unset() {
            Assert.AreEqual("\"NOT\"", "NOT".ToEscapedTerm());
        }

        [Test]
        public void NOT_shortcircuits_with_abort_set() {
            Assert.IsNull(_parser.GetQueryTerms("NOT", true));
        }

        [Test]
        public void Can_parse_wildcard_in_prefixed_query() {
            Assert.AreEqual("+path:user_guide +tag:task\\-tutorial\\-*", "+path:user_guide +tag:task-tutorial-*".ToEscapedTerms());
        }
    }

    public static class SearchQueryParserTestHelpers {

        //--- Fields ---
        private static readonly SearchQueryParser _parser = new SearchQueryParser();

        public static string ToEscapedTerms(this string query) {
            return string.Join(" ", _parser.GetQueryTerms(query, false).Select(x => x.Escaped).ToArray());
        }

        public static string ToEscapedTerm(this string query) {
            return _parser.GetQueryTerms(query, false).First().Escaped;
        }

        public static string ToEscapedTermOrAbort(this string query) {
            var x = _parser.GetQueryTerms(query, true).FirstOrDefault();
            return x == null ? null : x.Escaped;
        }

        public static string ToNormalizedTerm(this string query) {
            return _parser.GetQueryTerms(query, false).First().Normalized;
        }

        public static string ToNormalizedTermOrAbort(this string query) {
            var x = _parser.GetQueryTerms(query, true).FirstOrDefault();
            return x == null ? null : x.Normalized;
        }

        public static string[] ToEscaped(this IEnumerable<QueryTerm> terms) {
            return terms.OrderBy(x => x.Escaped).Select(x => x.Escaped).ToArray();
        }

        public static string[] ToNormalized(this IEnumerable<QueryTerm> terms) {
            return terms.OrderBy(x => x.Normalized).Select(x => x.Normalized).ToArray();
        }
    }
}
#pragma warning restore 1591
// ReSharper restore InconsistentNaming

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
using System.Collections.Generic;
using System.Linq;
using MindTouch.Deki.Search;
using NUnit.Framework;

namespace MindTouch.Deki.Util.Tests.Search {

    [TestFixture]
    public class SearchQueryTests {

        [Test]
        public void Constraints_are_ANDed_to_Query() {
            var constraint = new LuceneClauseBuilder();
            constraint.And("constraint");
            var query = new SearchQuery("raw", "cooked", constraint, null);
            Assert.AreEqual(string.Format("+(cooked) {0}",constraint.Clause),query.LuceneQuery);
        }

        [Test]
        public void Null_constraint_is_not_appended_to_query() {
            var query = new SearchQuery("raw", "cooked", new LuceneClauseBuilder(), null);
            Assert.AreEqual("cooked", query.LuceneQuery);
        }

        [Test]
        public void Can_get_termstring() {
            Assert.AreEqual("a b c", T(new[] { "a", "b", "c" }).GetOrderedNormalizedTermString());
        }

        [Test]
        public void Can_get_termstring_with_whitespace_term() {
            Assert.AreEqual(@"a ""b c"" d", T(L(M("a"), Q("b c"), M("d"))).GetOrderedNormalizedTermString());
        }

        [Test]
        public void GetNormalizedTerms_does_not_quote() {
            Assert.AreEqual(new[] { "a", "b c", "d" }, T(L(M("a"), Q("b c"), M("d"))).GetNormalizedTerms().OrderBy(x => x).ToArray());
        }

        [Test]
        public void GetOrderedNormalizedTermString_orders_results() {
            Assert.AreEqual("a b c", T(new[] { "b", "a", "c" }).GetOrderedNormalizedTermString());
        }

        [Test]
        public void GetOrderedNormalizedTermString_lower_cases() {
            Assert.AreEqual("aa bb cc", T(L(M("AA"), M("BB"), M("CC"))).GetOrderedNormalizedTermString());
        }

        [Test]
        public void GetOrderedTermsHash_does_not_care_about_order_or_casing() {
            var a = T(L(M("AA"), M("BB"), M("CC")));
            var b = T(L(M("bb"), M("AA"), M("Cc")));
            Assert.AreEqual(a.GetOrderedTermsHash(), b.GetOrderedTermsHash());
        }

        // shortcut helper to create a TermTuple
        public QueryTerm M(object normalized) {
            return new QueryTerm(null, normalized.ToString(), false, false);
        }

        public QueryTerm Q(object normalized) {
            return new QueryTerm(null, normalized.ToString(), false, true);
        }

        // shortcut helper to create an enumerable of TermTuples from a parameter sequence
        public IEnumerable<QueryTerm> L(params QueryTerm[] termTuple) {
            return termTuple;
        }

        public SearchQuery T(string[] strings) {
            return T(strings.Select(x => new QueryTerm(x, x, false, false)));
        }

        public SearchQuery T(IEnumerable<QueryTerm> terms) {
            return new SearchQuery(null, null, new LuceneClauseBuilder(), terms);
        }
    }
}

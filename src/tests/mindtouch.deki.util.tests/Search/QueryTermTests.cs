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
    public class QueryTermTests {

        [Test]
        public void QueryTerm_lowercases_normalized() {
            var term = new QueryTerm("Foo", "Foo", false, false);
            Assert.AreEqual("foo", term.Normalized);
        }

        [Test]
        public void QueryTerm_bases_equality_on_escaped_value() {
            var t1 = new QueryTerm("a", "x", false, false);
            var t2 = new QueryTerm("a", "y", false, false);
            Assert.AreEqual(t1, t2);
            Assert.AreNotEqual(t1.Normalized, t2.Normalized);
        }

        [Test]
        public void QueryTerm_equality_is_case_insensitive() {
            var t1 = new QueryTerm("a", "x", false, false);
            var t2 = new QueryTerm("A", "y", false, false);
            Assert.AreEqual(t1, t2);
            Assert.AreNotEqual(t1.Escaped, t2.Escaped);
            Assert.AreNotEqual(t1.Normalized, t2.Normalized);
        }

        [Test]
        public void QueryTerm_bases_hashing_on_escaped_value() {
            var t1 = new QueryTerm("a", "x", false, false);
            var t2 = new QueryTerm("a", "y", true, false);
            var set = new HashSet<QueryTerm>();
            set.Add(t1);
            set.Add(t2);
            Assert.AreEqual(1, set.Count);
            Assert.AreEqual("a", set.First().Escaped);
        }

        [Test]
        public void QueryTerm_hashing_ignores_case() {
            var t1 = new QueryTerm("a", "x", false, false);
            var t2 = new QueryTerm("A", "y", true, false);
            var set = new HashSet<QueryTerm>();
            set.Add(t1);
            set.Add(t2);
            Assert.AreEqual(1, set.Count);
        }

        [Test]
        public void SafeNormalized_quotes_if_hasWhitespace_is_specified() {
            Assert.AreEqual("\"a b\"", new QueryTerm(null, "a b", false, true).SafeNormalized);
        }
    }
}
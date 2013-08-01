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
using MindTouch.Deki.Search;
using NUnit.Framework;

namespace MindTouch.Deki.Util.Tests.Search {

    [TestFixture]
    public class LuceneClauseBuilderTests {

        // . + -
        // 1 0 0
        [Test]
        public void Clause_with_1_plain_0_plus_0_minus() {
            var input = "a";
            var output = "+a";
            Assert.AreEqual(output, input.ToClause());
        }

        // . + -
        // 0 1 0
        [Test]
        public void Clause_with_0_plain_1_plus_0_minus() {
            var input = "+a";
            var output = "+a";
            Assert.AreEqual(output, input.ToClause());
        }

        // . + -
        // 0 0 1
        [Test]
        public void Clause_with_0_plain_0_plus_1_minus() {
            var input = "-a";
            var output = "-a";
            Assert.AreEqual(output, input.ToClause());
        }

        // . + -
        // 1 1 0
        [Test]
        public void Clause_with_1_plain_1_plus_0_minus() {
            var input = "a +b";
            var output = "+a +b";
            Assert.AreEqual(output, input.ToClause());
        }

        // . + -
        // 1 0 1
        [Test]
        public void Clause_with_1_plain_0_plus_1_minus() {
            var input = "a -b";
            var output = "+a -b";
            Assert.AreEqual(output, input.ToClause());
        }

        // . + -
        // 0 1 1
        [Test]
        public void Clause_with_0_plain_1_plus_1_minus() {
            var input = "+a -b";
            var output = "+a -b";
            Assert.AreEqual(output, input.ToClause());
        }

        // . + -
        // 1 1 1
        [Test]
        public void Clause_with_1_plain_1_plus_1_minus() {
            var input = "a +b -c";
            var output = "+a +b -c";
            Assert.AreEqual(output, input.ToClause());
        }

        // . + -
        // 2 0 0
        [Test]
        public void Clause_with_2_plain_0_plus_0_minus() {
            var input = "a b";
            var output = "+(a b)";
            Assert.AreEqual(output, input.ToClause());
        }

        // . + -
        // 2 1 0
        [Test]
        public void Clause_with_2_plain_1_plus_0_minus() {
            var input = "a +b c";
            var output = "+(a c) +b";
            Assert.AreEqual(output, input.ToClause());
        }

        // . + -
        // 2 2 0
        [Test]
        public void Clause_with_2_plain_2_plus_0_minus() {
            var input = "a +b c +d";
            var output = "+(a c) +b +d";
            Assert.AreEqual(output, input.ToClause());
        }

        // . + -
        // 2 2 1
        [Test]
        public void Clause_with_2_plain_2_plus_1_minus() {
            var input = "a +b -c d +e";
            var output = "+(a d) +b +e -c";
            Assert.AreEqual(output, input.ToClause());
        }

        // . + -
        // 2 2 2
        [Test]
        public void Clause_with_2_plain_2_plus_2_minus() {
            var input = "a +b -c d +e -f";
            var output = "+(a d) +b +e -c -f";
            Assert.AreEqual(output, input.ToClause());
        }

        // . + -
        // 0 2 0
        [Test]
        public void Clause_with_0_plain_2_plus_0_minus() {
            var input = "+a +b";
            var output = "+a +b";
            Assert.AreEqual(output, input.ToClause());
        }

        // . + -
        // 1 2 0
        [Test]
        public void Clause_with_1_plain_2_plus_0_minus() {
            var input = "a +b +c";
            var output = "+a +b +c";
            Assert.AreEqual(output, input.ToClause());
        }

        // . + -
        // 0 0 2
        [Test]
        public void Clause_with_0_plain_0_plus_2_minus() {
            var input = "-a -b";
            var output = "-a -b";
            Assert.AreEqual(output, input.ToClause());
        }

        // . + -
        // 1 0 2
        [Test]
        public void Clause_with_1_plain_0_plus_2_minus() {
            var input = "a -b -c";
            var output = "+a -b -c";
            Assert.AreEqual(output, input.ToClause());
        }

        // . + -
        // 2 0 2
        [Test]
        public void Clause_with_2_plain_0_plus_2_minus() {
            var input = "a -b c -d";
            var output = "+(a c) -b -d";
            Assert.AreEqual(output, input.ToClause());
        }

        // . + -
        // 2 1 2
        [Test]
        public void Clause_with_2_plain_1_plus_2_minus() {
            var input = "a +b -c d -e";
            var output = "+(a d) +b -c -e";
            Assert.AreEqual(output, input.ToClause());
        }

    }

    public static class LuceneClauseBuilderTestEx {
        public static string ToClause(this string clause) {
            var c = new LuceneClauseBuilder();
            c.And(clause);
            return c.Clause;
        }
    }
}

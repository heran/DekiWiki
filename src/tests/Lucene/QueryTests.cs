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
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.QueryParsers;
using Lucene.Net.Search;
using NUnit.Framework;
using System.Linq;

namespace MindTouch.Lucene.Tests {

    [TestFixture]
    public class QueryTests {
        QueryParser _parser = new QueryParser("x", new StandardAnalyzer());

        [Test]
        public void Grouping_on_field_is_equivalent_to_separate_field_queries() {
            var grouping = "title:(foo bar)";
            var separate = "title:foo title:bar";
            var separateQuery = _parser.Parse(separate);
            var groupingQuery = _parser.Parse(grouping);
            Assert.AreEqual(separateQuery.ToString(), groupingQuery.ToString());
        }

        [Test]
        public void Can_boost_grouping() {
            var q = _parser.Parse("title:(foo bar)^4");
            Assert.AreEqual("(title:foo title:bar)^4.0", q.ToString());
        }

        [Test]
        public void Can_use_exclusion_inside_grouping() {
            var q = _parser.Parse("title:(+foo)");
            Assert.AreEqual("+title:foo", q.ToString());
        }

        [Test]
        public void Can_escape_space() {
            var q = _parser.Parse("title:foo\\ bar");
            Assert.AreEqual("title:\"foo bar\"", q.ToString());
        }

        [Test]
        public void Can_inverse_score_search() {
            var index = new TestIndex();
            var d = new Document();
            d.Add(new Field(index.Default, "Foo", Field.Store.NO, Field.Index.ANALYZED));
            index.Add(d);
            d = new Document();
            d.Add(new Field(index.Default, "Foo Foo", Field.Store.NO, Field.Index.ANALYZED));
            index.Add(d);
            d = new Document();
            d.Add(new Field(index.Default, "Foo Foo Foo", Field.Store.NO, Field.Index.ANALYZED));
            index.Add(d);
            var hits = index.Search("Foo", new Sort(new SortField(SortField.FIELD_SCORE.GetField(), SortField.SCORE, true)));
            Assert.AreEqual(3, hits.Count());
            var score0 = hits.ElementAt(0).Score;
            var score1 = hits.ElementAt(1).Score;
            var score2 = hits.ElementAt(2).Score;
            Assert.Less(score0, score1);
            Assert.Less(score1, score2);
        }

        [Test]
        public void Parse_drops_casing_explodes_field_prefixed_terms_and_uses_prefix_notation() {
            var q = _parser.Parse("(title:\"GetSearch\"^2) AND ((type:(wiki document image comment binary)))");
            Assert.AreEqual("+title:getsearch^2.0 +(type:wiki type:document type:image type:comment type:binary)", q.ToString());
        }

        [Test]
        public void Can_query_for_all_documents() {
            var index = new TestIndex();
            var d = new Document();
            d.Add(new Field(index.Default, "Foo", Field.Store.YES, Field.Index.ANALYZED));
            index.Add(d);
            d = new Document();
            d.Add(new Field(index.Default, "Foo Foo", Field.Store.YES, Field.Index.ANALYZED));
            index.Add(d);
            d = new Document();
            d.Add(new Field(index.Default, "bar", Field.Store.YES, Field.Index.ANALYZED));
            index.Add(d);
            var hits = index.Search(new MatchAllDocsQuery());
            Assert.AreEqual(3, hits.Count());
            var matches = new List<string>();
            foreach(var result in hits) {
                matches.Add(result.Document.Get(index.Default));
            }
            Assert.AreEqual(new[] { "bar", "Foo", "Foo Foo" }, matches.OrderBy(x => x).ToArray());
        }
    }
}

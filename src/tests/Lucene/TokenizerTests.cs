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
using System.IO;
using System.Linq;
using System.Text;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.QueryParsers;
using MindTouch.LuceneService;
using NUnit.Framework;

namespace MindTouch.Lucene.Tests {

    [TestFixture]
    public class TokenizerTests {

        [Test]
        public void UntokenizedAnalyzer_field_lowercases_terms_in_query() {
            var parser = new QueryParser("x",new UntokenizedAnalyzer());
            var query = parser.Parse("FOO");
            Assert.AreEqual("x:foo",query.ToString());
        }

        [Test]
        public void UnTokenizer_produces_single_token() {
            var reader = new StringReader("FOO bar");
            var tokenizer = new UnTokenizer(reader);
            var first = tokenizer.Next();
            Assert.AreEqual("FOO bar", first.Term());
            Assert.IsNull(tokenizer.Next());
        }

        [Test]
        public void UntokenizedAnalyzer_lower_cases_and_produces_single_token() {
            var reader = new StringReader("FOO bar");
            var analyzer = new UntokenizedAnalyzer();
            var tokenStream = analyzer.TokenStream("x", reader);
            var first = tokenStream.Next();
            Assert.AreEqual("foo bar", first.Term());
            Assert.IsNull(tokenStream.Next());
        }

        [Test]
        public void UntokenizedAnalyzer_does_not_drop_stopword_from_query() {
            var analyzer = new UntokenizedAnalyzer();
            QueryParser parser = new QueryParser("x", analyzer);
            Assert.AreEqual("x:a", parser.Parse("a").ToString());
        }

        [Test]
        public void UntokenizedAnalyzer_field_treats_whitespace_as_part_of_token() {
            var index = new TestIndex(new UntokenizedAnalyzer());
            var d = new Document();
            d.Add(new Field("id", "a", Field.Store.YES, Field.Index.UN_TOKENIZED));
            d.Add(new Field(index.Default, "foo bar", Field.Store.NO, Field.Index.ANALYZED));
            index.Add(d);
            var hits = index.Search("\"foo bar\"");
            Assert.AreEqual(1, hits.Count());
            Assert.AreEqual("a", hits.First().Document.Get("id"));
        }

        [Test]
        public void UntokenizedAnalyzer_field_is_case_insensitive() {
            var index = new TestIndex(new UntokenizedAnalyzer());
            var d = new Document();
            d.Add(new Field("id", "a", Field.Store.YES, Field.Index.UN_TOKENIZED));
            d.Add(new Field(index.Default, "FOO", Field.Store.NO, Field.Index.ANALYZED));
            index.Add(d);
            var hits = index.Search("foo");
            Assert.AreEqual(1, hits.Count());
            Assert.AreEqual("a", hits.First().Document.Get("id"));
            hits = index.Search("FOO");
            Assert.AreEqual(1, hits.Count());
            Assert.AreEqual("a", hits.First().Document.Get("id"));
        }

        [Test]
        public void UntokenizedAnalyzer_field_accepts_wildcards() {
            var index = new TestIndex(new UntokenizedAnalyzer());
            var d = new Document();
            d.Add(new Field("id", "a", Field.Store.YES, Field.Index.UN_TOKENIZED));
            d.Add(new Field(index.Default, "FOO BAR", Field.Store.NO, Field.Index.ANALYZED));
            index.Add(d);
            var hits = index.Search("foo*");
            Assert.AreEqual(1, hits.Count());
            Assert.AreEqual("a", hits.First().Document.Get("id"));
        }

        [Test]
        public void UntokenizedAnalyzer_field_accepts_whitespace_term_followed_by_wildcards() {
            var index = new TestIndex(new UntokenizedAnalyzer());
            var d = new Document();
            d.Add(new Field("id", "a", Field.Store.YES, Field.Index.UN_TOKENIZED));
            d.Add(new Field(index.Default, "FOO BAR", Field.Store.NO, Field.Index.ANALYZED));
            index.Add(d);
            var hits = index.Search("foo\\ ba*");
            Assert.AreEqual(1, hits.Count());
            Assert.AreEqual("a", hits.First().Document.Get("id"));
        }

        [Test]
        public void FilenameAnalyzer_translates_whitespace_and_dashes_to_underscore_and_lower_cases_query() {
            var parser = new QueryParser("x", new FilenameAnalyzer());
            var query = parser.Parse("foo\\ Bar-baz");
            Assert.AreEqual("x:foo_bar_baz", query.ToString());
        }

        [Test]
        public void FilenameAnalyzer_field_treats_whitespace_underscore_and_dash_the_same() {
            var index = new TestIndex(new FilenameAnalyzer());
            var d = new Document();
            d.Add(new Field("id", "a", Field.Store.YES, Field.Index.UN_TOKENIZED));
            d.Add(new Field(index.Default, "foo bar-baz_boom", Field.Store.NO, Field.Index.ANALYZED));
            index.Add(d);
            var hits = index.Search("foo_bar\\ baz-boom");
            Assert.AreEqual(1, hits.Count());
            Assert.AreEqual("a", hits.First().Document.Get("id"));
        }


        [Test]
        public void Can_find_file_with_dash_in_it() {
            var index = new TestIndex(new UntokenizedAnalyzer());
            var d = new Document();
            d.Add(new Field("id", "a", Field.Store.YES, Field.Index.UN_TOKENIZED));
            d.Add(new Field(index.Default, "cube-teal", Field.Store.NO, Field.Index.ANALYZED));
            index.Add(d);
            Console.WriteLine("query");
            var query = index.Parse("CUBE-*");
            Assert.AreEqual("content:cube-*",query.ToString());
            var hits = index.Search(query);
            Assert.AreEqual(1, hits.Count());
            Assert.AreEqual("a", hits.First().Document.Get("id"));
        }

        [Test]
        public void Can_parse_tag_term_with_colon_and_dash() {
            var parser = new QueryParser("x", new TagAnalyzer());
            var q = parser.Parse("title:foo\\:bar\\-baz");
            Assert.AreEqual("title:foo:bar-baz", q.ToString());
        }

        [Test]
        public void Tokenizing_dekiscript_with_EnglishAnalyzer_is_weird() {
            Assert.AreEqual(
                "web.pr json.format searchanalytics.term ",
                "{{web.pre(json.format(searchanalytics.terms()))}}".ToTokenStreamString(new EnglishAnalyzer())
            );
        }
    }

    public static class TokenizerEx {
        public static string ToTokenStreamString(this string text, Analyzer analyzer) {
            var tokens = new StringBuilder();
            var reader = new StringReader(text);
            var tokenStream = analyzer.TokenStream("x", reader);
            while(true) {
                var term = tokenStream.Next();
                if(term == null) {
                    break;
                }
                tokens.Append(term.Term());
                tokens.Append(" ");
            }
            return tokens.ToString();
        }
    }
}

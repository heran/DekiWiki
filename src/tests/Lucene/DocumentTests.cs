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
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using MindTouch.Xml;
using NUnit.Framework;

namespace MindTouch.Lucene.Tests {

    [TestFixture]
    public class DocumentTests {

        [Test]
        public void Can_modify_doc_in_index() {
            var index = new TestIndex();
            var doc = new XDoc("doc").Elem("foo", "bar");
            var d = new Document();
            d.Add(new Field("id", "123", Field.Store.YES, Field.Index.UN_TOKENIZED));
            d.Add(new Field("doc", doc.ToString(), Field.Store.YES, Field.Index.UN_TOKENIZED));
            d.Add(new Field("foo", "bar", Field.Store.NO, Field.Index.ANALYZED));
            index.Add(d);
            var hits = index.Search(index.Parse("id:123"));
            Assert.AreEqual(1, hits.Count());
            var d2 = hits.First().Document;
            Assert.AreEqual("123", d2.Get("id"));
            Assert.AreEqual(doc.ToString(), d2.Get("doc"));
            d2.RemoveField("doc");
            d2.RemoveField("foo");
            doc = new XDoc("doc").Elem("foo", "baz");
            d2.Add(new Field("doc", doc.ToString(), Field.Store.YES, Field.Index.UN_TOKENIZED));
            d2.Add(new Field("foo", "baz", Field.Store.NO, Field.Index.ANALYZED));
            index.Update(new Term("id", "123"), d2);
            hits = index.Search(index.Parse("id:123"));
            Assert.AreEqual(1, hits.Count());
            var d3 = hits.First().Document;
            Assert.AreEqual("123", d3.Get("id"));
            Assert.AreEqual(doc.ToString(), d3.Get("doc"));
        }

        [Test]
        public void Can_modify_doc_in_index_and_retrieve_by_secondary_key() {
            var index = new TestIndex();
            var doc = new XDoc("doc").Elem("foo", "bar");
            var d = new Document();
            d.Add(new Field("id", "123", Field.Store.YES, Field.Index.UN_TOKENIZED));
            d.Add(new Field("doc", doc.ToString(), Field.Store.YES, Field.Index.UN_TOKENIZED));
            d.Add(new Field("foo", "bar", Field.Store.NO, Field.Index.ANALYZED));
            index.Add(d);
            var hits = index.Search(index.Parse("foo:bar"));
            Assert.AreEqual(1, hits.Count());
            var d2 = hits.First().Document;
            Assert.AreEqual("123", d2.Get("id"));
            Assert.AreEqual(doc.ToString(), d2.Get("doc"));
            d2.RemoveField("doc");
            d2.RemoveField("foo");
            doc = new XDoc("doc").Elem("foo", "baz");
            d2.Add(new Field("doc", doc.ToString(), Field.Store.YES, Field.Index.UN_TOKENIZED));
            d2.Add(new Field("foo", "baz", Field.Store.NO, Field.Index.ANALYZED));
            index.Update(new Term("id", "123"), d2);
            hits = index.Search(index.Parse("foo:baz"));
            Assert.AreEqual(1, hits.Count());
            var d3 = hits.First().Document;
            Assert.AreEqual("123", d3.Get("id"));
            Assert.AreEqual(doc.ToString(), d3.Get("doc"));
        }

        [Test]
        public void Can_add_same_field_multiple_times() {
            var index = new TestIndex();
            var doc = new XDoc("doc").Elem("foo", "bar");
            var d = new Document();
            d.Add(new Field("id", "123", Field.Store.YES, Field.Index.UN_TOKENIZED));
            d.Add(new Field("doc", doc.ToString(), Field.Store.YES, Field.Index.UN_TOKENIZED));
            d.Add(new Field("foo", "bar", Field.Store.NO, Field.Index.ANALYZED));
            d.Add(new Field("foo", "baz", Field.Store.NO, Field.Index.ANALYZED));
            index.Add(d);
            var hits = index.Search(index.Parse("foo:bar"));
            Assert.AreEqual(1, hits.Count());
            var d2 = hits.First().Document;
            Assert.AreEqual("123", d2.Get("id"));
            hits = index.Search(index.Parse("foo:baz"));
            Assert.AreEqual(1, hits.Count());
            var d3 = hits.First().Document;
            Assert.AreEqual("123", d3.Get("id"));
        }

        [Test]
        public void RemoveFields_removes_all_occurences() {
            var index = new TestIndex();
            var doc = new XDoc("doc").Elem("foo", "bar");
            var d = new Document();
            d.Add(new Field("id", "123", Field.Store.YES, Field.Index.UN_TOKENIZED));
            d.Add(new Field("doc", doc.ToString(), Field.Store.YES, Field.Index.UN_TOKENIZED));
            d.Add(new Field("foo", "bar", Field.Store.YES, Field.Index.ANALYZED));
            d.Add(new Field("foo", "baz", Field.Store.YES, Field.Index.ANALYZED));
            index.Add(d);
            var hits = index.Search(index.Parse("foo:bar"));
            Assert.AreEqual(1, hits.Count());
            var d2 = hits.First().Document;
            var fields = d2.GetFields("foo");
            Assert.AreEqual(2, fields.Length);
            d2.RemoveFields("foo");
            fields = d2.GetFields("foo");
            Assert.AreEqual(0, fields.Length);
        }

        [Test]
        public void Update_does_not_keep_unstored_fields() {
            var index = new TestIndex();
            var doc = new XDoc("doc").Elem("foo", "bar");
            var d = new Document();
            d.Add(new Field("id", "123", Field.Store.YES, Field.Index.UN_TOKENIZED));
            d.Add(new Field("doc", doc.ToString(), Field.Store.YES, Field.Index.UN_TOKENIZED));
            d.Add(new Field("foo", "bar", Field.Store.NO, Field.Index.ANALYZED));
            index.Add(d);
            var hits = index.Search(index.Parse("foo:bar"));
            Assert.AreEqual(1, hits.Count());
            var d2 = hits.First().Document;
            index.Update(new Term("id", "123"), d2);
            hits = index.Search(index.Parse("foo:bar"));
            Assert.AreEqual(0, hits.Count());
        }

        [Test]
        public void Can_use_update_for_new_document() {
            var index = new TestIndex();
            var doc = new XDoc("doc").Elem("foo", "bar");
            var d = new Document();
            d.Add(new Field("id", "123", Field.Store.YES, Field.Index.UN_TOKENIZED));
            d.Add(new Field("doc", doc.ToString(), Field.Store.YES, Field.Index.UN_TOKENIZED));
            d.Add(new Field("foo", "bar", Field.Store.NO, Field.Index.ANALYZED));
            index.Update(new Term("id", "123"), d);
            var hits = index.Search(index.Parse("foo:bar"));
            Assert.AreEqual(1, hits.Count());
            var d2 = hits.First().Document;
            Assert.AreEqual("123", d2.Get("id"));
        }
    }
}

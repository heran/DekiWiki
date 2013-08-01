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
using MindTouch.Deki.Data;
using MindTouch.Deki.Search;
using NUnit.Framework;

namespace MindTouch.Deki.Tests.SearchTests {

    [TestFixture]
    public class SearchSerializerTests {
        private SearchSerializer _serializer;

        [TestFixtureSetUp]
        public void GlobalSetup() {
            _serializer = new SearchSerializer();
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Cannot_serialize_null_SearchResult() {
            using(var ms = new MemoryStream()) {
                _serializer.Serialize(ms, (SearchResult)null);
            }
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Cannot_serialize_null_SearchResultDetail() {
            using(var ms = new MemoryStream()) {
                _serializer.Serialize(ms, (SearchResultDetail)null);
            }
        }

        [Test]
        public void Can_roundtrip_SearchResult() {
            var item = new SearchResultItem(1, SearchResultType.Page, "bar", 0.5, DateTime.UtcNow.WithoutMilliseconds());
            var result = new SearchResult("foo", new[] { item });
            using(var ms = new MemoryStream()) {
                _serializer.Serialize(ms, result);
                ms.Position = 0;
                var result2 = _serializer.Deserialize<SearchResult>(ms);
                Assert.AreEqual(result.ExecutedQuery, result2.ExecutedQuery);
                Assert.AreEqual(result.Count, result2.Count);
                Assert.AreEqual(result.First().TypeId, result2.First().TypeId);
            }
        }

        [Test]
        public void Can_roundtrip_SearchResult_with_empty_string() {
            var item = new SearchResultItem(1, SearchResultType.Page, "", 0.5, DateTime.UtcNow.WithoutMilliseconds());
            var result = new SearchResult("foo", new[] { item });
            using(var ms = new MemoryStream()) {
                _serializer.Serialize(ms, result);
                ms.Position = 0;
                var result2 = _serializer.Deserialize<SearchResult>(ms);
                Assert.AreEqual(result.ExecutedQuery, result2.ExecutedQuery);
                Assert.AreEqual(result.Count, result2.Count);
                Assert.AreEqual(result.First().Title, result2.First().Title);
            }
        }

        [Test]
        public void Can_roundtrip_SearchResult_with_null_string() {
            var item = new SearchResultItem(1, SearchResultType.Page, null, 0.5, DateTime.UtcNow.WithoutMilliseconds());
            var result = new SearchResult("foo", new[] { item });
            using(var ms = new MemoryStream()) {
                _serializer.Serialize(ms, result);
                ms.Position = 0;
                var result2 = _serializer.Deserialize<SearchResult>(ms);
                Assert.AreEqual(result.ExecutedQuery, result2.ExecutedQuery);
                Assert.AreEqual(result.Count, result2.Count);
                Assert.AreEqual(result.First().Title, result2.First().Title);
            }
        }

        [Test]
        public void Can_roundtrip_SearchResultDetail() {
            var data = new SearchResultDetail();
            data["foo"] = "bar";
            data["bzz"] = "bonk";
            using(var ms = new MemoryStream()) {
                _serializer.Serialize(ms, data);
                ms.Position = 0;
                var data2 = _serializer.Deserialize<SearchResultDetail>(ms);
                Assert.AreEqual(data["foo"], data2["foo"]);
                Assert.AreEqual(data["bzz"], data2["bzz"]);
            }
        }

        [Test]
        public void Can_roundtrip_SearchResultDetail_with_empty_string() {
            var data = new SearchResultDetail();
            data["empty"] = string.Empty;
            using(var ms = new MemoryStream()) {
                _serializer.Serialize(ms, data);
                ms.Position = 0;
                var data2 = _serializer.Deserialize<SearchResultDetail>(ms);
                Assert.AreEqual(string.Empty, data2["empty"]);
            }
        }

        [Test]
        public void Can_roundtrip_SearchResultDetail_with_empty_string2() {
            var data = new SearchResultDetail();
            data["empty"] = "";
            using(var ms = new MemoryStream()) {
                _serializer.Serialize(ms, data);
                ms.Position = 0;
                var data2 = _serializer.Deserialize<SearchResultDetail>(ms);
                Assert.AreEqual("", data2["empty"]);
            }
        }

        [Test]
        public void Can_roundtrip_SearchResultDetail_with_null_string() {
            var data = new SearchResultDetail();
            data["null"] = null;
            using(var ms = new MemoryStream()) {
                _serializer.Serialize(ms, data);
                ms.Position = 0;
                var data2 = _serializer.Deserialize<SearchResultDetail>(ms);
                Assert.IsNull(data2["null"]);
            }
        }
    }
}

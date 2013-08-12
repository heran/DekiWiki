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
using log4net;
using Lucene.Net.Documents;
using MindTouch.Dream;
using MindTouch.Dream.Test;
using MindTouch.Dream.Test.Mock;
using MindTouch.LuceneService;
using MindTouch.Tasking;
using NUnit.Framework;
using MindTouch.Extensions.Time;
using System;

namespace MindTouch.Lucene.Tests {

    [TestFixture]
    public class LuceneResultFilterTests {

        //--- Class Fields ---
        private static readonly ILog _log = LogUtils.CreateLog();

        //--- Fields ---
        private XUri _authUri;
        private Plug _authPlug;

        [SetUp]
        public void Setup() {
            MockPlug.DeregisterAll();
            _authUri = new XUri("mock://auth.uri/");
            _authPlug = Plug.New(_authUri);
        }

        [Test]
        public void Items_are_filtered_by_pageId() {
            MockPlug.Setup(_authUri)
                .Verb("POST")
                .WithMessage(m => {
                    var t = m.ToText();
                    return t.EqualsInvariant("1,2");
                })
                .Returns(DreamMessage.Ok(MimeType.TEXT, "1"))
                .ExpectCalls(Times.Once());
            var items = new[] {
                Result(1),
                Result(2),
            };
            var set = LuceneResultFilter.Filter(_authPlug, items, 0, int.MaxValue, new Result<IList<LuceneResult>>()).Wait();
            Assert.AreEqual(1, set.Count);
            Assert.AreEqual(items[1], set[0]);
            MockPlug.VerifyAll(1.Seconds());
        }

        [Test]
        public void Can_apply_offset() {
            MockPlug.Setup(_authUri)
                .Verb("POST")
                .Returns(DreamMessage.Ok(MimeType.TEXT, ""));
            var items = new[] {
                Result(1),
                Result(2),
            };
            var set = LuceneResultFilter.Filter(_authPlug, items, 1, int.MaxValue, new Result<IList<LuceneResult>>()).Wait();
            Assert.AreEqual(1, set.Count);
            Assert.AreEqual(items[1], set[0]);
        }

        [Test]
        public void Can_apply_limit() {
            MockPlug.Setup(_authUri)
                .Verb("POST")
                .Returns(DreamMessage.Ok(MimeType.TEXT, ""));
            var items = new[] {
                Result(1),
                Result(2),
            };
            var set = LuceneResultFilter.Filter(_authPlug, items, 0, 1, new Result<IList<LuceneResult>>()).Wait();
            Assert.AreEqual(1, set.Count);
            Assert.AreEqual(items[0], set[0]);
        }

        [Test]
        public void Can_apply_offset_and_limit() {
            MockPlug.Setup(_authUri)
                .Verb("POST")
                .Returns(DreamMessage.Ok(MimeType.TEXT, ""));
            var items = new[] {
                Result(1),
                Result(2),
                Result(3),
                Result(4),
            };
            var set = LuceneResultFilter.Filter(_authPlug, items, 1, 2, new Result<IList<LuceneResult>>()).Wait();
            Assert.AreEqual(2, set.Count);
            Assert.AreEqual(items[1], set[0]);
            Assert.AreEqual(items[2], set[1]);
        }

        [Test]
        public void Items_without_pageid_are_not_affected_by_filtering() {
            MockPlug.Setup(_authUri).Verb("POST").ExpectCalls(Times.Never());
            var items = new[] {
                Result(1,null),
                Result(2,null),
            };
            var set = LuceneResultFilter.Filter(_authPlug, items, 0, int.MaxValue, new Result<IList<LuceneResult>>()).Wait();
            Assert.AreEqual(2, set.Count);
            Assert.AreEqual(items[0], set[0]);
            MockPlug.VerifyAll(1.Seconds());
        }

        [Test]
        public void Duplicate_pageIds_do_not_affect_filtering() {
            MockPlug.Setup(_authUri)
                .Verb("POST")
                .WithMessage(m => {
                    var t = m.ToText();
                    _log.DebugFormat("input data: {0}", t);
                    return t.EqualsInvariant("1,2,5");
                })
                .Returns(DreamMessage.Ok(MimeType.TEXT, "1,5"))
                .ExpectCalls(Times.Once());
            var items = new[] {
                Result(1,1),
                Result(2,2),
                Result(3,2),
                Result(4,1),
                Result(5,5),
            };
            var set = LuceneResultFilter.Filter(_authPlug, items, 0, int.MaxValue, new Result<IList<LuceneResult>>()).Wait();
            Assert.AreEqual(2, set.Count);
            Assert.AreEqual(items[1], set[0]);
            Assert.AreEqual(items[2], set[1]);
            MockPlug.VerifyAll(1.Seconds());
        }

        [Test]
        public void Offset_and_limit_are_applied_post_filtering() {
            MockPlug.Setup(_authUri)
                .Verb("POST")
                .WithMessage(m => {
                    var t = m.ToText();
                    _log.DebugFormat("input data: {0}", t);
                    return t.EqualsInvariant("1,2,3,4,5,6,7,8,9,10");
                })
                .Returns(DreamMessage.Ok(MimeType.TEXT, "1,5,6"))
                .ExpectCalls(Times.Once());
            var items = new List<LuceneResult>();
            for(var i = 1; i <= 10; i++) {
                items.Add(Result(i));
            }
            var set = LuceneResultFilter.Filter(_authPlug, items, 2, 4, new Result<IList<LuceneResult>>()).Wait();
            Assert.AreEqual(4, set.Count);
            Assert.AreEqual(4, set[0].PageId.Value.ToInt());
            Assert.AreEqual(7, set[1].PageId.Value.ToInt());
            Assert.AreEqual(8, set[2].PageId.Value.ToInt());
            Assert.AreEqual(9, set[3].PageId.Value.ToInt());
            MockPlug.VerifyAll(1.Seconds());
        }

        [Test]
        public void Item_sets_larger_than_maxAuthItems_are_authorized_in_chunks() {
            var items = new List<LuceneResult>();
            for(var i = 1; i <= 50; i++) {
                items.Add(Result(i));
            }
            MockPlug.Setup(_authUri)
                .Verb("POST")
                .WithMessage(m => {
                    var t = m.ToText();
                    var match = t.EqualsInvariant(items.Select(x => x.PageId.Value).Take(30).ToCommaDelimitedString());
                    _log.DebugFormat("first chunk match? {0} => {1}", match, t);
                    return match;
                })
                .Returns(DreamMessage.Ok(MimeType.TEXT, ""))
                .ExpectCalls(Times.Once());
            MockPlug.Setup(_authUri)
                .Verb("POST")
                .WithMessage(m => {
                    var t = m.ToText();
                    var match = t.EqualsInvariant(items.Select(x => x.PageId.Value).Skip(30).ToCommaDelimitedString());
                    _log.DebugFormat("second chunk match? {0} => {1}", match, t);
                    return match;
                })
                .Returns(DreamMessage.Ok(MimeType.TEXT, ""))
                .ExpectCalls(Times.Once());
            var builder = new LuceneResultFilter(_authPlug, 30, 1000);
            var set = Coroutine.Invoke(builder.Filter, items, 0, int.MaxValue, new Result<IList<LuceneResult>>()).Wait();
            Assert.AreEqual(items.Count, set.Count);
            Assert.AreEqual(
                items.Select(x => x.PageId.Value).ToCommaDelimitedString(),
                set.Select(x => x.PageId.Value).ToCommaDelimitedString()
            );
            MockPlug.VerifyAll(1.Seconds());
        }

        [Test]
        public void Limit_smaller_than_item_set_forces_authorization_in_chunks_as_candidates_nears_limit() {
            var items = new List<LuceneResult>();
            for(var i = 1; i <= 200; i++) {
                items.Add(Result(i));
            }

            // first chunk should ask for 50 ids and we'll filter 20 from that set
            MockPlug.Setup(_authUri)
                .Verb("POST")
                .WithMessage(m => {
                    var t = m.ToText();
                    var match = t.EqualsInvariant(items.Select(x => x.PageId.Value).Take(50).ToCommaDelimitedString());
                    _log.DebugFormat("first chunk match? {0} => {1}", match, t);
                    return match;
                })
                .Returns(DreamMessage.Ok(MimeType.TEXT, S(10,39).ToCommaDelimitedString()))
                .ExpectCalls(Times.Once());

            // second chunk should ask for 30 and we'll filter 5 from that set
            // which gives it a total larger than our limit, i.e. it won't try for a third chunk
            MockPlug.Setup(_authUri)
                .Verb("POST")
                .WithMessage(m => {
                    var t = m.ToText();
                    var match = t.EqualsInvariant(items.Select(x => x.PageId.Value).Skip(50).Take(30).ToCommaDelimitedString());
                    _log.DebugFormat("second chunk match? {0} => {1}", match, t);
                    return match;
                })
                .Returns(DreamMessage.Ok(MimeType.TEXT, S(51,55).ToCommaDelimitedString()))
                .ExpectCalls(Times.Once());
            var builder = new LuceneResultFilter(_authPlug, 10000, 20);
            var set = Coroutine.Invoke(builder.Filter, items, 0, 30, new Result<IList<LuceneResult>>()).Wait();
            Assert.AreEqual(30, set.Count);

            // we expect a sequence of 1-9,40-50,56-... and take the first 30
            var expected = S(1, 9).Union(S(40, 50)).Union(S(56, 100)).Take(30);
            Assert.AreEqual(
                expected.ToCommaDelimitedString(),
                set.Select(x => x.PageId.Value).ToCommaDelimitedString()
            );
            MockPlug.VerifyAll(1.Seconds());
        }

        private LuceneResult Result(int id) {
            return Result(id, (ulong)id);
        }

        private LuceneResult Result(int id, ulong? pageId) {
            var d = new Document();
            d.Add(new Field("id", id.ToString(), Field.Store.YES, Field.Index.UN_TOKENIZED));
            if(pageId.HasValue) {
                d.Add(new Field("id.page", pageId.ToString(), Field.Store.YES, Field.Index.UN_TOKENIZED));
            }
            return new LuceneResult(d, 1);
        }

        private IEnumerable<int> S(int start, int end) {
            if(start > end) {
                throw new ArgumentException();
            }
            var i = start;
            while(i <= end) {
                yield return i;
                i++;
            }
        }
    }
}

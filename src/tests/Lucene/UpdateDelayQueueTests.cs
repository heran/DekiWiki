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
using System.IO;
using System.Threading;
using MindTouch.Collections;
using MindTouch.Dream.Test;
using MindTouch.IO;
using MindTouch.LuceneService;
using MindTouch.Tasking;
using MindTouch.Xml;
using NUnit.Framework;

namespace MindTouch.Lucene.Tests {
    using Yield = IEnumerator<IYield>;

    [TestFixture]
    public class UpdateDelayQueueTests {

        private static readonly log4net.ILog _log = LogUtils.CreateLog();

        private TransactionalQueue<XDoc> _peekQueue;
        private MockUpdateRecordDispatcher _dispatcher;
        private UpdateDelayQueue _queue;

        public class MockUpdateRecordDispatcher : IUpdateRecordDispatcher {

            public List<Tuplet<DateTime, UpdateRecord>> Dispatches = new List<Tuplet<DateTime, UpdateRecord>>();
            public ManualResetEvent ResetEvent = new ManualResetEvent(false);

            public Result Dispatch(UpdateRecord updateRecord, Result result) {
                _log.DebugFormat("got dispatch");
                Dispatches.Add(new Tuplet<DateTime, UpdateRecord>(DateTime.Now, updateRecord));
                result.Return();
                ResetEvent.Set();
                return result;
            }
        }

        [SetUp]
        public void Setup() {
            _peekQueue = new TransactionalQueue<XDoc>(new SingleFileQueueStream(new MemoryStream()), new XDocQueueItemSerializer());
            _dispatcher = new MockUpdateRecordDispatcher();
            _queue = new UpdateDelayQueue(TimeSpan.FromSeconds(3), _dispatcher, _peekQueue);
        }

        [Test]
        public void Mods_followed_by_delete_result_in_delete() {
            ActionStack actionStack = new ActionStack();

            //mod
            actionStack.PushDelete();
            actionStack.PushAdd();

            //mod
            actionStack.PushDelete();
            actionStack.PushAdd();

            //delete
            actionStack.PushDelete();
            Assert.IsFalse(actionStack.IsAdd);
            Assert.IsTrue(actionStack.IsDelete);
        }

        [Test]
        public void Create_mod_delete_results_in_no_op() {
            ActionStack actionStack = new ActionStack();

            //create
            actionStack.PushAdd();

            //mod
            actionStack.PushDelete();
            actionStack.PushAdd();

            //delete
            actionStack.PushDelete();
            Assert.IsFalse(actionStack.IsAdd);
            Assert.IsFalse(actionStack.IsDelete);
        }

        [Test]
        public void Delete_create_mod_results_in_add_and_delete() {
            ActionStack actionStack = new ActionStack();

            //delete
            actionStack.PushDelete();

            //create
            actionStack.PushAdd();

            //mod
            actionStack.PushDelete();
            actionStack.PushAdd();
            Assert.IsTrue(actionStack.IsAdd);
            Assert.IsTrue(actionStack.IsDelete);
        }

        [Test]
        public void Delete_create_delete_results_in_delete() {
            ActionStack actionStack = new ActionStack();

            //delete
            actionStack.PushDelete();

            //create
            actionStack.PushAdd();

            //delete
            actionStack.PushDelete();
            Assert.IsFalse(actionStack.IsAdd);
            Assert.IsTrue(actionStack.IsDelete);
        }

        [Test]
        public void Callback_in_delay_time() {
            XDoc queued = new XDoc("deki-event")
                .Attr("wikiid", "abc")
                .Elem("channel", "event://abc/deki/pages/update")
                .Elem("uri", "http://foo/bar")
                .Elem("content.uri", "")
                .Elem("revision.uri", "")
                .Elem("path", "bar")
                .Elem("previous-path", "bar");

            _log.DebugFormat("queueing item");
            _queue.Enqueue(queued);
            Assert.AreEqual(1, _peekQueue.Count);
            Assert.AreEqual(0, _dispatcher.Dispatches.Count);
            if(!_dispatcher.ResetEvent.WaitOne(5000, true)) {
                _log.Debug("reset event never fired");
                Assert.AreEqual(0, _peekQueue.Count, "item still in the queue");
                Assert.Fail("callback didn't happen");
            }
            Assert.IsTrue(_dispatcher.ResetEvent.WaitOne(5000));
            Assert.AreEqual(1, _dispatcher.Dispatches.Count);
            Assert.AreEqual(queued.ToString(), _dispatcher.Dispatches[0].Item2.Meta.ToString());
            Assert.IsNotNull(_dispatcher.Dispatches[0].Item2.Id);
            Assert.IsTrue(_dispatcher.Dispatches[0].Item2.ActionStack.IsAdd);
            Assert.IsTrue(_dispatcher.Dispatches[0].Item2.ActionStack.IsDelete);
        }

        [Test]
        public void Same_item_in_queue_updates_item_not_time() {
            XDoc m1 = new XDoc("deki-event")
                .Attr("wikiid", "abc")
                .Elem("channel", "event://abc/deki/pages/update")
                .Elem("uri", "http://foo/bar")
                .Elem("content.uri", "")
                .Elem("revision.uri", "")
                .Elem("path", "bar")
                .Elem("previous-path", "bar");
            _queue.Enqueue(m1);
            Thread.Sleep(500);
            XDoc m2 = new XDoc("deki-event")
                .Attr("wikiid", "abc")
                .Elem("channel", "event://abc/deki/pages/update")
                .Elem("uri", "http://foo/bar")
                .Elem("content.uri", "")
                .Elem("revision.uri", "")
                .Elem("path", "bar")
                .Elem("previous-path", "bar");
            _queue.Enqueue(m2);
            Assert.AreEqual(0, _dispatcher.Dispatches.Count);
            if(!_dispatcher.ResetEvent.WaitOne(5000, true)) {
                Assert.Fail("callback didn't happen");
            }
            Assert.AreEqual(1, _dispatcher.Dispatches.Count);
            Assert.AreEqual(m2.ToString(), _dispatcher.Dispatches[0].Item2.Meta.ToString());
            Assert.IsNotNull(_dispatcher.Dispatches[0].Item2.Id);
            Assert.IsTrue(_dispatcher.Dispatches[0].Item2.ActionStack.IsAdd);
            Assert.IsTrue(_dispatcher.Dispatches[0].Item2.ActionStack.IsDelete);
            Thread.Sleep(1500);
            Assert.AreEqual(1, _dispatcher.Dispatches.Count);
            Assert.AreEqual(0, _peekQueue.Count);
        }

        [Test]
        public void Different_items_in_queue_fire_separately() {
            XDoc m1 = new XDoc("deki-event")
                .Attr("wikiid", "abc")
                .Elem("channel", "event://abc/deki/pages/update")
                .Elem("uri", "http://foo/bar")
                .Elem("content.uri", "")
                .Elem("revision.uri", "")
                .Elem("path", "bar")
                .Elem("previous-path", "bar");
            DateTime queueTime1 = DateTime.Now;
            _queue.Enqueue(m1);
            Thread.Sleep(1000);
            XDoc m2 = new XDoc("deki-event")
                .Attr("wikiid", "abc")
                .Elem("channel", "event://abc/deki/pages/update")
                .Elem("uri", "http://foo/baz")
                .Elem("content.uri", "")
                .Elem("revision.uri", "")
                .Elem("path", "baz")
                .Elem("previous-path", "baz");
            DateTime queueTime2 = DateTime.Now;
            _queue.Enqueue(m2);
            Assert.AreEqual(0, _dispatcher.Dispatches.Count);
            Assert.IsTrue(_dispatcher.ResetEvent.WaitOne(5000, true), "first callback didn't happen");
            _dispatcher.ResetEvent.Reset();
            Assert.IsTrue(_dispatcher.ResetEvent.WaitOne(2000, true), "second callback didn't happen");
            Assert.AreEqual(2, _dispatcher.Dispatches.Count);
            Assert.AreEqual(m1.ToString(), _dispatcher.Dispatches[0].Item2.Meta.ToString());
            Assert.AreEqual(m2.ToString(), _dispatcher.Dispatches[1].Item2.Meta.ToString());
            Assert.AreEqual(0, _peekQueue.Count);
        }

        [Test]
        public void Adding_same_document_after_first_dispatch_fires_after_normal_delay() {
            XDoc m1 = new XDoc("deki-event")
                .Attr("wikiid", "abc")
                .Elem("channel", "event://abc/deki/pages/create")
                .Elem("uri", "http://foo/baz")
                .Elem("content.uri", "")
                .Elem("revision.uri", "")
                .Elem("path", "baz")
                .Elem("previous-path", "bar");
            DateTime queueTime1 = DateTime.Now;
            _queue.Enqueue(m1);
            Assert.AreEqual(0, _dispatcher.Dispatches.Count);
            Assert.IsTrue(_dispatcher.ResetEvent.WaitOne(5000, true), "first callback didn't happen");
            _dispatcher.ResetEvent.Reset();
            Assert.AreEqual(1, _dispatcher.Dispatches.Count);
            _queue.Enqueue(m1.Clone());
            Assert.AreEqual(1, _dispatcher.Dispatches.Count);
            Assert.IsTrue(_dispatcher.ResetEvent.WaitOne(5000, true), "second callback didn't happen");
            Assert.AreEqual(2, _dispatcher.Dispatches.Count);
            Assert.AreEqual(0, _peekQueue.Count);
        }
    }
}

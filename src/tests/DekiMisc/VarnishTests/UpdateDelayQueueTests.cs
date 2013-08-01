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
using System.Threading;

using MindTouch.Deki.Varnish;
using MindTouch.Tasking;
using MindTouch.Xml;

using NUnit.Framework;

namespace MindTouch.Deki.Tests.VarnishTests {
    using Yield = IEnumerator<IYield>;

    [TestFixture]
    public class UpdateDelayQueueTests {

        //--- Types ---
        public class MockUpdateRecordDispatcher : IUpdateRecordDispatcher {
            private readonly int _expectedDispatches;
            public int QueueSizeCalled;
            public List<Tuplet<DateTime, UpdateRecord>> Dispatches = new List<Tuplet<DateTime, UpdateRecord>>();
            public readonly AutoResetEvent ResetEvent = new AutoResetEvent(false);

            public MockUpdateRecordDispatcher(int expectedDispatches) {
                _expectedDispatches = expectedDispatches;
            }

            public void Dispatch(UpdateRecord updateRecord) {
                Dispatches.Add(new Tuplet<DateTime, UpdateRecord>(DateTime.Now, updateRecord));
                if(Dispatches.Count >= _expectedDispatches) {
                    ResetEvent.Set();
                }
            }

            public int QueueSize {
                get { QueueSizeCalled++; return 1; }
            }
        }

        [Test]
        public void QueueSize_queries_dispatcher_QueueSize() {
            var dispatcher = new MockUpdateRecordDispatcher(1);
            var queue = new UpdateDelayQueue(TimeSpan.FromSeconds(1), dispatcher);
            Assert.AreEqual(1, queue.QueueSize);
            Assert.AreEqual(1, dispatcher.QueueSizeCalled);
        }

        [Test]
        public void Callback_in_delay_time() {
            var dispatcher = new MockUpdateRecordDispatcher(1);
            var queue = new UpdateDelayQueue(TimeSpan.FromSeconds(1), dispatcher);
            XDoc queued = new XDoc("deki-event")
                .Attr("wikiid", "abc")
                .Elem("channel", "event://abc/deki/pages/update")
                .Elem("pageid", "1")
                .Elem("path", "bar");

            DateTime queueTime = DateTime.Now;
            queue.Enqueue(queued);
            Assert.AreEqual(0, dispatcher.Dispatches.Count);
            if(!dispatcher.ResetEvent.WaitOne(5000, true)) {
                Assert.Fail("callback didn't happen");
            }
            Assert.AreEqual(1, dispatcher.Dispatches.Count);
            Assert.AreEqual(1, dispatcher.Dispatches[0].Item2.Id);
            Assert.AreEqual("abc", dispatcher.Dispatches[0].Item2.WikiId);
            Assert.AreEqual("bar", dispatcher.Dispatches[0].Item2.Path);
            Assert.AreEqual(RecordType.Page, dispatcher.Dispatches[0].Item2.Type);
            Assert.GreaterOrEqual(dispatcher.Dispatches[0].Item1, queueTime.Add(TimeSpan.FromMilliseconds(1000)));
        }

        [Test]
        public void Multiple_events_for_same_page_fire_once_on_first_plus_delay() {
            var dispatcher = new MockUpdateRecordDispatcher(1);
            var queue = new UpdateDelayQueue(TimeSpan.FromSeconds(2), dispatcher);
            XDoc queued = new XDoc("deki-event")
                .Attr("wikiid", "abc")
                .Elem("channel", "event://abc/deki/pages/create")
                .Elem("pageid", "1")
                .Elem("path", "bar");
            DateTime queueTime = DateTime.Now;
            queue.Enqueue(queued);
            Assert.AreEqual(0, dispatcher.Dispatches.Count);

            Thread.Sleep(300);
            queued = new XDoc("deki-event")
                .Attr("wikiid", "abc")
                .Elem("channel", "event://abc/deki/pages/update")
                .Elem("pageid", "1")
                .Elem("path", "bar");
            queue.Enqueue(queued);
            Assert.AreEqual(0, dispatcher.Dispatches.Count);

            Thread.Sleep(300);
            queued = new XDoc("deki-event")
                .Attr("wikiid", "abc")
                .Elem("channel", "event://abc/deki/pages/foop")
                .Elem("pageid", "1")
                .Elem("path", "bar");
            queue.Enqueue(queued);
            Assert.AreEqual(0, dispatcher.Dispatches.Count);

            if(!dispatcher.ResetEvent.WaitOne(3000, true)) {
                Assert.Fail("callback didn't happen");
            }
            Assert.AreEqual(1, dispatcher.Dispatches.Count);
            Assert.AreEqual(1, dispatcher.Dispatches[0].Item2.Id);
            Assert.AreEqual("abc", dispatcher.Dispatches[0].Item2.WikiId);
            Assert.AreEqual("bar", dispatcher.Dispatches[0].Item2.Path);
            Assert.AreEqual(RecordType.Page, dispatcher.Dispatches[0].Item2.Type);
            Assert.GreaterOrEqual(dispatcher.Dispatches[0].Item1, queueTime.Add(TimeSpan.FromMilliseconds(2000)));
            Assert.LessOrEqual(dispatcher.Dispatches[0].Item1, queueTime.Add(TimeSpan.FromMilliseconds(2200)));
            Thread.Sleep(1000);
            Assert.AreEqual(1, dispatcher.Dispatches.Count);

        }

        [Test]
        public void Multiple_events_for_different_types_fire_for_each_type() {
            var dispatcher = new MockUpdateRecordDispatcher(3);
            var queue = new UpdateDelayQueue(TimeSpan.FromSeconds(1), dispatcher);
            XDoc queued = new XDoc("deki-event")
                .Attr("wikiid", "abc")
                .Elem("channel", "event://abc/deki/pages/create")
                .Elem("pageid", "1")
                .Elem("path", "bar");
            DateTime queueTime = DateTime.Now;
            queue.Enqueue(queued);
            Assert.AreEqual(0, dispatcher.Dispatches.Count);

            Thread.Sleep(100);
            queued = new XDoc("deki-event")
                .Attr("wikiid", "abd")
                .Elem("channel", "event://abc/deki/pages/update")
                .Elem("pageid", "2")
                .Elem("path", "baz");
            queue.Enqueue(queued);
            Assert.AreEqual(0, dispatcher.Dispatches.Count);

            Thread.Sleep(100);
            queued = new XDoc("deki-event")
                .Attr("wikiid", "abf")
                .Elem("channel", "event://abc/deki/files/foop")
                .Elem("fileid", "1");
            queue.Enqueue(queued);
            Assert.AreEqual(0, dispatcher.Dispatches.Count);

            if(!dispatcher.ResetEvent.WaitOne(2000, true)) {
                Assert.Fail("callbacks didn't happen");
            }
            Assert.AreEqual(3, dispatcher.Dispatches.Count);
            Assert.AreEqual(1, dispatcher.Dispatches[0].Item2.Id);
            Assert.AreEqual("abc", dispatcher.Dispatches[0].Item2.WikiId);
            Assert.AreEqual("bar", dispatcher.Dispatches[0].Item2.Path);
            Assert.AreEqual(RecordType.Page, dispatcher.Dispatches[0].Item2.Type);
            Assert.AreEqual(2, dispatcher.Dispatches[1].Item2.Id);
            Assert.AreEqual("abd", dispatcher.Dispatches[1].Item2.WikiId);
            Assert.AreEqual("baz", dispatcher.Dispatches[1].Item2.Path);
            Assert.AreEqual(RecordType.Page, dispatcher.Dispatches[1].Item2.Type);
            Assert.AreEqual(1, dispatcher.Dispatches[2].Item2.Id);
            Assert.AreEqual("abf", dispatcher.Dispatches[2].Item2.WikiId);
            Assert.IsTrue(string.IsNullOrEmpty(dispatcher.Dispatches[2].Item2.Path));
            Assert.AreEqual(RecordType.File, dispatcher.Dispatches[2].Item2.Type);

        }

        [Test]
        public void Multiple_events_for_same_page_update_path_in_dispatched_callback() {
            var dispatcher = new MockUpdateRecordDispatcher(1);
            var queue = new UpdateDelayQueue(TimeSpan.FromSeconds(1), dispatcher);
            XDoc queued = new XDoc("deki-event")
                .Attr("wikiid", "abc")
                .Elem("channel", "event://abc/deki/pages/create")
                .Elem("pageid", "1")
                .Elem("path", "bar");
            DateTime queueTime = DateTime.Now;
            queue.Enqueue(queued);
            Assert.AreEqual(0, dispatcher.Dispatches.Count);

            Thread.Sleep(100);
            queued = new XDoc("deki-event")
                .Attr("wikiid", "abc")
                .Elem("channel", "event://abc/deki/pages/update")
                .Elem("pageid", "1")
                .Elem("path", "baz");
            queue.Enqueue(queued);
            Assert.AreEqual(0, dispatcher.Dispatches.Count);

            if(!dispatcher.ResetEvent.WaitOne(2000, true)) {
                Assert.Fail("callbacks didn't happen");
            }
            Assert.AreEqual(1, dispatcher.Dispatches.Count);
            Assert.AreEqual(1, dispatcher.Dispatches[0].Item2.Id);
            Assert.AreEqual("abc", dispatcher.Dispatches[0].Item2.WikiId);
            Assert.AreEqual("baz", dispatcher.Dispatches[0].Item2.Path);
            Assert.AreEqual(RecordType.Page, dispatcher.Dispatches[0].Item2.Type);
        }

        [Test]
        public void Can_dispatch_using_coroutine_dispatcher() {
            var helper = new CallbackHelper();
            var queue = new UpdateDelayQueue(TimeSpan.FromSeconds(1), new UpdateRecordDispatcher(helper.Invoke));
            var queued = new XDoc("deki-event")
                .Attr("wikiid", "abc")
                .Elem("channel", "event://abc/deki/pages/update")
                .Elem("pageid", "1")
                .Elem("path", "bar");
            queue.Enqueue(queued);
            Assert.AreEqual(0, helper.Callbacks.Count);
            if(!helper.ResetEvent.WaitOne(2000, true)) {
                Assert.Fail("callback didn't happen");
            }
            Assert.AreEqual(1, helper.Callbacks.Count);
            Assert.AreEqual(1, helper.Callbacks[0].Id);
            Assert.AreEqual("abc", helper.Callbacks[0].WikiId);
            Assert.AreEqual("bar", helper.Callbacks[0].Path);
            Assert.AreEqual(RecordType.Page, helper.Callbacks[0].Type);
        }

        private class CallbackHelper {
            public readonly List<UpdateRecord> Callbacks = new List<UpdateRecord>();
            public readonly AutoResetEvent ResetEvent = new AutoResetEvent(false);
            public Yield Invoke(UpdateRecord data, Result result) {
                Callbacks.Add(data);
                ResetEvent.Set();
                yield break;
            }
        }
    }
}

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
using System.Diagnostics;
using System.Linq;
using MindTouch.Dream;
using MindTouch.Extensions.Time;
using MindTouch.LuceneService;
using MindTouch.Tasking;
using MindTouch.Xml;
using NUnit.Framework;

namespace MindTouch.Lucene.Tests {
    using Yield = IEnumerator<IYield>;

    [TestFixture]
    public class UpdateRecordDispatcherTests {

        [Test]
        public void Dispatcher_retries_specified_times() {
            var handler = new DispatchHandler();
            var dispatcher = new UpdateRecordDispatcher(handler.Dispatch, 1, 2, 0.Seconds());
            var firstAttempt = handler.AddCallback((d, r) => r.Throw(new Exception()), new Result<UpdateRecord>(2.Seconds()));
            var secondAttempt = handler.AddCallback((d, r) => r.Throw(new Exception()), new Result<UpdateRecord>(2.Seconds()));
            var thirddAttempt = handler.AddCallback((d, r) => r.Throw(new Exception()), new Result<UpdateRecord>(2.Seconds()));
            var fourthAttempt = handler.AddCallback((d, r) => r.Throw(new Exception()), new Result<UpdateRecord>(5.Seconds()));
            dispatcher.Dispatch(new UpdateRecord(new XUri("mock://foo"), new XDoc("meta"), "default"), new Result());
            Assert.IsFalse(firstAttempt.Block().HasException, "first attempt wasn't called");
            Assert.IsFalse(secondAttempt.Block().HasException, "second attempt wasn't called");
            Assert.IsFalse(thirddAttempt.Block().HasException, "third attempt wasn't called");
            Assert.IsTrue(fourthAttempt.Block().HasException, "fourth attempt shouldn't have happened");
        }


        [Test]
        public void Failed_tasks_sleeps_and_retries() {
            var handler = new DispatchHandler();
            var dispatcher = new UpdateRecordDispatcher(handler.Dispatch, 1, 1, 2.Seconds());
            var firstAttempt = handler.AddCallback((d, r) => r.Throw(new Exception()), new Result<UpdateRecord>(5.Seconds()));
            var secondAttempt = handler.AddCallback((d, r) => { }, new Result<UpdateRecord>(5.Seconds()));
            dispatcher.Dispatch(new UpdateRecord(new XUri("mock://foo"), new XDoc("meta"), "default"), new Result());
            Assert.IsFalse(firstAttempt.Block().HasException, "first attempt wasn't called");
            var stopwatch = Stopwatch.StartNew();
            Assert.IsFalse(secondAttempt.Block().HasException, "second attempt wasn't called");
            stopwatch.Stop();
            Assert.GreaterOrEqual(stopwatch.Elapsed, 2.Seconds(), string.Format("expected at least 2 second delay, took {0:0.00}s", stopwatch.Elapsed.TotalSeconds));
        }

        [Test]
        public void Failed_task_sleep_does_not_block_next_task() {
            var handler = new DispatchHandler();
            var dispatcher = new UpdateRecordDispatcher(handler.Dispatch, 1, 1, 2.Seconds());
            var r1 = new UpdateRecord(new XUri("mock://foo"), new XDoc("meta"), "default");
            var r2 = new UpdateRecord(new XUri("mock://foo"), new XDoc("meta"), "default");
            var r1firstAttempt = handler.AddCallback((d, r) => r.Throw(new Exception()), new Result<UpdateRecord>(1.Seconds()));
            var r2firstAttempt = handler.AddCallback((d, r) => { }, new Result<UpdateRecord>(1.Seconds()));
            var r1SecondAttempt = handler.AddCallback((d, r) => { }, new Result<UpdateRecord>(5.Seconds()));
            dispatcher.Dispatch(r1, new Result());
            dispatcher.Dispatch(r2, new Result());
            Assert.IsFalse(r1firstAttempt.Block().HasException, "r1 first attempt wasn't called");
            Assert.IsFalse(r1firstAttempt.Block().HasException, "r2 first attempt wasn't called");
            Assert.IsFalse(r1SecondAttempt.Block().HasException, "r2 second attempt wasn't called");
        }

        public class DispatchHandler {
            private readonly Queue<Tuplet<Action<UpdateRecord, Result>, Result<UpdateRecord>>> _callbacks = new Queue<Tuplet<Action<UpdateRecord, Result>, Result<UpdateRecord>>>();
            public Yield Dispatch(UpdateRecord data, Result result) {
                if(_callbacks.Any()) {
                    var tuple = _callbacks.Dequeue();
                    tuple.Item1(data, result);
                    tuple.Item2.Return(data);
                }
                if(!result.HasFinished) {
                    result.Return();
                }
                yield break;
            }
            public Result<UpdateRecord> AddCallback(Action<UpdateRecord, Result> callback, Result<UpdateRecord> result) {
                _callbacks.Enqueue(new Tuplet<Action<UpdateRecord, Result>, Result<UpdateRecord>>(callback, result));
                return result;
            }
        }
    }
}

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
using log4net;
using MindTouch.Collections;
using MindTouch.Dream;
using MindTouch.Tasking;
using MindTouch.Xml;

namespace MindTouch.LuceneService {
    public class UpdateDelayQueue : IDisposable {

        //--- Class Fields ---
        private static readonly ILog _log = LogUtils.CreateLog();

        //--- Fields ---
        private readonly TimeSpan _delay;
        private readonly IUpdateRecordDispatcher _dispatcher;
        private readonly TaskTimer _queueTimer;
        private readonly TaskTimer _pollTimer;
        private readonly Queue<Tuplet<DateTime, XUri>> _queue = new Queue<Tuplet<DateTime, XUri>>();
        private readonly Dictionary<XUri, UpdateRecord> _data = new Dictionary<XUri, UpdateRecord>();
        private readonly ITransactionalQueue<XDoc> _persistentQueue;
        private int _pendingCount;
        private bool _poll;

        //--- Constructors ---
        public UpdateDelayQueue(TimeSpan delay, IUpdateRecordDispatcher dispatcher, ITransactionalQueue<XDoc> queue) {
            _delay = delay;
            _dispatcher = dispatcher;
            _queueTimer = TaskTimerFactory.Current.New(_delay, CheckExpire, null, TaskEnv.None);
            _persistentQueue = queue;
            _poll = true;
            _pollTimer = TaskTimerFactory.Current.New(TimeSpan.Zero, Poll, null, TaskEnv.None);
            _log.DebugFormat("created queue with {0} items recovered", queue.Count);
        }

        //--- Properties ---
        public int QueueSize {
            get { lock(_data) return _queue.Count + _pendingCount; }
        }

        //--- Methods ---
        public void Clear() {
            _persistentQueue.Clear();
            _queue.Clear();
            _data.Clear();
        }

        public void Enqueue(XDoc meta) {
            _persistentQueue.Enqueue(meta);
            _poll = true;
        }

        private void Poll(TaskTimer timer) {
            if(!_poll) {
                timer.Change(TimeSpan.FromSeconds(1), TaskEnv.Current);
                return;
            }
            _poll = false;
            while(true) {

                // pull item from queue to store in out accumulation queue and hold on to it
                var item = _persistentQueue.Dequeue(TimeSpan.MaxValue);
                if(item == null) {

                    // didn't find an item, drop out of loop and set timer to check again later
                    timer.Change(TimeSpan.FromSeconds(1), TaskEnv.Current);
                    return;
                }
                var doc = item.Value;
                var wikiid = doc["@wikiid"].AsText;
                var id = new XUri("http://" + wikiid + "/" + doc["path"].AsText);
                lock(_data) {
                    UpdateRecord data;
                    XUri channel = doc["channel"].AsUri;
                    string action = channel.Segments[2];
                    if(!_data.TryGetValue(id, out data)) {
                        _log.DebugFormat("queueing '{0}' for '{1}'", action, id);
                        _queue.Enqueue(new Tuplet<DateTime, XUri>(DateTime.UtcNow.Add(_delay), id));
                        data = new UpdateRecord(id, doc, wikiid);
                    } else {
                        _log.DebugFormat("appending existing queue record '{0}' for '{1}'", action, id);
                        data = data.With(doc);
                    }
                    if(action != "create" && action != "move") {
                        data.ActionStack.PushDelete();
                    }
                    if(action != "delete") {
                        data.ActionStack.PushAdd();
                    }
                    data.QueueIds.Add(item.Id);
                    _data[id] = data;
                }
            }
        }

        private void CheckExpire(TaskTimer timer) {
            while(true) {

                // get the next scheduled item
                UpdateRecord data = null;
                lock(_data) {
                    if(_queue.Count == 0) {
                        _queueTimer.Change(_delay, TaskEnv.None);
                        return;
                    }
                    Tuplet<DateTime, XUri> key = _queue.Peek();
                    if(key.Item1 > DateTime.UtcNow) {
                        _queueTimer.Change(key.Item1, TaskEnv.None);
                        return;
                    }
                    data = _data[key.Item2];
                    _queue.Dequeue();
                    _data.Remove(key.Item2);
                }
                Interlocked.Increment(ref _pendingCount);
                _dispatcher.Dispatch(data, new Result(TimeSpan.MaxValue)).WhenDone(r => {

                    // cleanup items from the queue
                    var poll = false;
                    foreach(var itemId in data.QueueIds) {
                        if(!_persistentQueue.CommitDequeue(itemId)) {

                            // if we couldn't take an item, it must have gone back to the queue, so we better poll again
                            poll = true;
                        }
                    }
                    if(poll) {
                        _poll = true;
                    }
                    Interlocked.Decrement(ref _pendingCount);
                    if(r.HasException) {
                        _log.Error(string.Format("dispatch of '{0}' encountered an error", data.Id), r.Exception);
                    }
                });
            }
        }

        public void Dispose() {
            _pollTimer.Change(DateTime.MinValue, TaskEnv.None);
            _queueTimer.Change(DateTime.MinValue, TaskEnv.None);
            _persistentQueue.Dispose();
        }
    }
}

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
using log4net;
using MindTouch.Collections;
using MindTouch.Dream;
using MindTouch.Tasking;
using MindTouch.Threading;

namespace MindTouch.Deki.UserSubscription {
    using Yield = IEnumerator<IYield>;

    public class NotificationDelayQueue {

        //--- Class Fields ---
        private static readonly ILog _log = LogUtils.CreateLog();

        //--- Fields ---
        private readonly TimeSpan _delay;
        private readonly TaskTimer _queueTimer;
        private readonly Dictionary<string, NotificationUpdateRecord> _pending = new Dictionary<string, NotificationUpdateRecord>();
        private readonly Queue<Tuplet<DateTime, string>> _queue = new Queue<Tuplet<DateTime, string>>();
        private readonly CoroutineHandler<NotificationUpdateRecord, Result> _callback;
        private readonly ProcessingQueue<NotificationUpdateRecord> _dispatchQueue;

        //--- Constructors ---
        public NotificationDelayQueue(TimeSpan delay, CoroutineHandler<NotificationUpdateRecord, Result> callback) {
            _delay = delay;
            _callback = callback;
            _queueTimer = new TaskTimer(CheckExpire, null);
            _queueTimer.Change(_delay, TaskEnv.None);
            _dispatchQueue = new ProcessingQueue<NotificationUpdateRecord>(Dispatch, 10);
        }

        //--- Methods ---
        public void Enqueue(string wikiid, uint userId, uint pageId, DateTime modificationDate) {
            lock(_pending) {
                NotificationUpdateRecord pending;
                string key = wikiid + ":" + userId;
                if(!_pending.TryGetValue(key, out pending)) {
                    pending = new NotificationUpdateRecord(wikiid, userId);
                    _pending[key] = pending;
                    _queue.Enqueue(new Tuplet<DateTime, string>(DateTime.UtcNow.Add(_delay), key));
                }
                pending.Add(pageId, modificationDate);
            }
        }

        private void CheckExpire(TaskTimer timer) {

            // get the next scheduled item
            NotificationUpdateRecord data;
            lock(_pending) {
                if(_queue.Count == 0) {
                    _queueTimer.Change(_delay, TaskEnv.Current);
                    return;
                }
                Tuplet<DateTime, string> key = _queue.Peek();
                if(key.Item1 > DateTime.UtcNow) {
                    _queueTimer.Change(key.Item1, TaskEnv.Current);
                    return;
                }
                data = _pending[key.Item2];
                _queue.Dequeue();
                _pending.Remove(key.Item2);
            }

            // stuff data into dispatch queue so our worker thread can pick it up and process all data synchronously
            if(!_dispatchQueue.TryEnqueue(data)) {
                throw new InvalidOperationException(string.Format("Enqueue for user '{0}' failed.", data.UserId));
            }

            // check for optimal sleep interval
            lock(_pending) {
                if(_queue.Count == 0) {
                    _queueTimer.Change(_delay, TaskEnv.Current);
                    return;
                }
                Tuplet<DateTime, string> key = _queue.Peek();
                _queueTimer.Change(key.Item1, TaskEnv.Current);
            }
        }

        private void Dispatch(NotificationUpdateRecord updateRecord, Action completionCallback) {
            Coroutine.Invoke(_callback, updateRecord, new Result()).WhenDone(
              r => {
                  completionCallback();
                  if(r.HasException) {
                      _log.ErrorExceptionMethodCall(r.Exception, "DispatchFromQueue", string.Format("dispatch for user '{0}' encountered an error", updateRecord.UserId));
                  } else {
                      _log.DebugFormat("finished dispatch of update record for user '{0}'", updateRecord.UserId);
                  }
              });
        }

        public void Cleanup() {
            _queueTimer.Change(DateTime.MinValue, TaskEnv.Current);
        }
    }
}

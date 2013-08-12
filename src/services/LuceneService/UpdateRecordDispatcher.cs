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
using System.Threading;
using log4net;
using MindTouch.Collections;
using MindTouch.Extensions.Time;
using MindTouch.Tasking;
using MindTouch;

namespace MindTouch.LuceneService {
    public interface IUpdateRecordDispatcher {
        Result Dispatch(UpdateRecord updateRecord, Result result);
    }

    public class UpdateRecordDispatcher : IUpdateRecordDispatcher {

        //--- Types ---
        private class QueueItem {

            //--- Fields ---
            public readonly UpdateRecord Record;
            public readonly Result Result;
            public int Attempt = 1;

            //--- Constructors ---
            public QueueItem(UpdateRecord record, Result result) {
                Record = record;
                Result = result;
            }
        }

        //--- Class Fields ---
        private static readonly ILog _log = LogUtils.CreateLog();

        //--- Fields ---
        private readonly CoroutineHandler<UpdateRecord, Result> _callback;
        private readonly int _maxRetry;
        private readonly TimeSpan _retrySleep;
        private readonly ProcessingQueue<QueueItem> _dispatchQueue;

        //--- Constructors ---
        public UpdateRecordDispatcher(CoroutineHandler<UpdateRecord, Result> callback, int maxParallelism, int maxRetry, TimeSpan retrySleep) {
            _dispatchQueue = new ProcessingQueue<QueueItem>(DispatchRecord, maxParallelism);
            _callback = callback;
            _maxRetry = maxRetry;
            _retrySleep = retrySleep;
        }

        //--- Methods ---
        public Result Dispatch(UpdateRecord updateRecord, Result result) {
            _log.DebugFormat("moving update record '{0}' to dispatch queue ({1})", updateRecord.Id, _dispatchQueue.Count);
            if(!_dispatchQueue.TryEnqueue(new QueueItem(updateRecord, result))) {
                throw new InvalidOperationException(string.Format("Enqueue of '{0}' failed.", updateRecord.Id));
            }
            return result;
        }

        private void DispatchRecord(QueueItem item, Action completionTrigger) {
            _log.DebugFormat("dispatching update record '{0}'", item.Record.Id);
            Coroutine.Invoke(_callback, item.Record, new Result()).WhenDone(r => {
                completionTrigger();
                if(r.HasException) {
                    var e = r.Exception;
                    if(item.Attempt <= _maxRetry) {
                        _log.DebugFormat("dispatch of '{0}' failed, sleeping for {1:0.0}s before attempting re-queue", item.Record.Id, _retrySleep.TotalSeconds);
                        item.Attempt++;
                        Async.Sleep(_retrySleep).WhenDone(r2 => {
                            if(!_dispatchQueue.TryEnqueue(item)) {
                                item.Result.Throw(new InvalidOperationException(string.Format("Unable to re-queue '{0}' for retry {1}.", item.Record.Id, item.Attempt)));
                                return;
                            }
                            _log.DebugFormat("re-queued '{0}' for retry {1}", item.Record.Id, item.Attempt);
                        });
                    } else {
                        _log.DebugFormat("dispatch of '{0}' permanently failed after {1} tries", item.Record.Id, item.Attempt);
                        item.Result.Throw(e);
                    }
                } else {
                    _log.DebugFormat("finished dispatch of update record '{0}'", item.Record.Id);
                    item.Result.Return();
                }
            });
        }
    }
}
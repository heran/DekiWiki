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
using MindTouch.Tasking;
using MindTouch.Threading;
using MindTouch.Xml;

namespace MindTouch.Deki.Varnish {
    using Yield = IEnumerator<IYield>;

    public interface IUpdateRecordDispatcher {
        void Dispatch(UpdateRecord updateRecord);
        int QueueSize { get; }
    }

    public class UpdateRecordDispatcher : IUpdateRecordDispatcher {

        //--- Class Fields ---
        private static readonly ILog _log = LogUtils.CreateLog();

        //--- Fields ---
        private readonly CoroutineHandler<UpdateRecord, Result> _callback;
        private int _dispatchCount;
        private readonly ProcessingQueue<UpdateRecord> _dispatchQueue;

        //--- Constructors ---
        public UpdateRecordDispatcher(CoroutineHandler<UpdateRecord, Result> callback) {
            _callback = callback;
            _dispatchQueue = new ProcessingQueue<UpdateRecord>(DispatchQueued, 10);
        }

        //--- Properties ---
        public int QueueSize {
            get { return _dispatchCount; }
        }

        //--- Methods ---
        public void Dispatch(UpdateRecord updateRecord) {
            _log.DebugFormat("moving update record '{0}' from delay to dispatch queue", updateRecord.Id);
            Interlocked.Increment(ref _dispatchCount);
            if(!_dispatchQueue.TryEnqueue(updateRecord)) {
                Interlocked.Decrement(ref _dispatchCount);
                throw new InvalidOperationException(string.Format("Enqueue of '{0}' failed.", updateRecord.Id));
            }
        }

        private void DispatchQueued(UpdateRecord updateRecord, Action completionCallback) {
            _log.DebugFormat("dispatching update record '{0}'", updateRecord.Id);
            Coroutine.Invoke(_callback, updateRecord, new Result()).WhenDone(
                r => {
                    completionCallback();
                    Interlocked.Decrement(ref _dispatchCount);
                    if(r.HasException) {
                        _log.ErrorExceptionMethodCall(r.Exception, "DispatchFromQueue", string.Format("dispatch of '{0}' encountered an error", updateRecord.Id));
                    } else {
                        _log.DebugFormat("finished dispatch of update record '{0}'", updateRecord.Id);
                    }
                });
        }
    }

}

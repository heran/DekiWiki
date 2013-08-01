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

using MindTouch.Dream;
using MindTouch.Tasking;
using MindTouch.Xml;

namespace MindTouch.Deki.Varnish {
    public class UpdateDelayQueue {

        //--- Class Fields ---
        private static readonly ILog _log = LogUtils.CreateLog();

        //--- Fields ---
        private readonly TimeSpan _delay;
        private readonly IUpdateRecordDispatcher _dispatcher;
        private readonly TaskTimer _queueTimer;
        private readonly Queue<Tuplet<string, DateTime>> _queue = new Queue<Tuplet<string, DateTime>>();
        private readonly Dictionary<string, UpdateRecord> _data = new Dictionary<string, UpdateRecord>();

        //--- Constructors ---
        public UpdateDelayQueue(TimeSpan delay, IUpdateRecordDispatcher dispatcher) {
            _delay = delay;
            _dispatcher = dispatcher;
            _queueTimer = new TaskTimer(CheckExpire, null);
            _queueTimer.Change(_delay, TaskEnv.None);
        }

        //--- Properties ---
        public int QueueSize {
            get { lock(_data) return _queue.Count + _dispatcher.QueueSize; }
        }

        //--- Methods ---
        public void Enqueue(XDoc meta) {
            XUri channel = meta["channel"].AsUri;
            RecordType type;
            int id;
            string path = string.Empty;
            string wikiid = meta["@wikiid"].AsText;
            if(channel.Segments[1] == "pages") {
                type = RecordType.Page;
                id = meta["pageid"].AsInt ?? 0;
                path = meta["path"].AsText;
            } else {
                type = RecordType.File;
                id = meta["fileid"].AsInt ?? 0;
            }
            string key = string.Format("{0}:{1}:{2}", wikiid, type, id);
            lock(_data) {
                UpdateRecord data;
                if(!_data.TryGetValue(key, out data)) {
                    _log.DebugFormat("queueing '{0}'", key);
                    _queue.Enqueue(new Tuplet<string, DateTime>(key, DateTime.UtcNow.Add(_delay)));
                    data = new UpdateRecord(id, type, wikiid);
                    _data.Add(key,data);
                }
                if(!string.IsNullOrEmpty(path)) {
                    data.Path = path;
                }
            }
        }

        private void CheckExpire(TaskTimer timer) {
            // get the next scheduled item
            UpdateRecord data = null;
            lock(_data) {
                if(_queue.Count == 0) {
                    _queueTimer.Change(_delay, TaskEnv.Current);
                    return;
                }
                Tuplet<string, DateTime> key = _queue.Peek();
                if(key.Item2 > DateTime.UtcNow) {
                    _queueTimer.Change(key.Item2, TaskEnv.Current);
                    return;
                }
                data = _data[key.Item1];
                _queue.Dequeue();
                _data.Remove(key.Item1);
            }
            _dispatcher.Dispatch(data);

            // check for optimal sleep interval
            lock(_data) {
                if(_queue.Count == 0) {
                    _queueTimer.Change(_delay, TaskEnv.Current);
                    return;
                }
                Tuplet<string, DateTime> key = _queue.Peek();
                _queueTimer.Change(key.Item2, TaskEnv.Current);
            }
        }

        public void Cleanup() {
            _queueTimer.Change(DateTime.MinValue, TaskEnv.Current);
        }
    }
}

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
using MindTouch.Tasking;
using MindTouch.Data;

namespace MindTouch.Deki.Data.MySql {
    public partial class MySqlDekiDataSession : IDekiDataSession, IDekiDataStats, ITaskLifespan {

        //--- Class Fields ---
        private static readonly ILog _log = LogUtils.CreateLog();
        private static int _concurrentSessions;

        //--- Fields ---
        private readonly IInstanceSettings _settings;
        private readonly DataCatalog _catalog;
        private readonly object _statsLock = new object();
        private IDekiDataSession _head;
        private long _totalQueryCount;
        private TimeSpan _totalQueryTime = TimeSpan.Zero;
        private bool _isDisposed;

        //--- Constructors ---
        public MySqlDekiDataSession(IInstanceSettings settings, DataCatalog catalog) {
            _settings = settings;
            _catalog = catalog;
            Interlocked.Increment(ref _concurrentSessions);
            _catalog.OnQueryFinished += OnQueryFinished;
            Head = this;
        }

        //--- Properties ---
        public IDekiDataSession Head {
            get {
                return _head;
            }
            set {
                _head = value;
            }
        }

        public IDekiDataSession Next { get { return null; } }

        // TODO (arnec): This shouldn't be leaked out of the session, but the MediaWiki converter is hardcoded against it and mysql ATM.
        public DataCatalog Catalog { get { return _catalog; } }

        //--- Methods ---
        public object Clone() {
            return new MySqlDekiDataSession(_settings, _catalog);
        }

        public void Dispose() {
            if(_isDisposed) {
                return;
            }
            _isDisposed = true;
            Catalog.OnQueryFinished -= OnQueryFinished;
            var concurrent = Interlocked.Decrement(ref _concurrentSessions);
            _log.DebugFormat("concurrent sessions left: {0}", concurrent);
        }

        public Dictionary<string, string> GetStats() {
            var ret = new Dictionary<string, string>();
            ret["mysql-queries"] = _totalQueryCount.ToString();
            ret["mysql-time-ms"] = ((int)_totalQueryTime.TotalMilliseconds).ToString();
            return ret;
        }

        private void OnQueryFinished(IDataCommand command) {
            Interlocked.Increment(ref _totalQueryCount);
            lock(_statsLock) {
                _totalQueryTime = _totalQueryTime.Add(command.ExecutionTime);
            }
        }
    }
}

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
using System.Diagnostics;
using log4net;
using MindTouch.Xml;

namespace MindTouch.LuceneService {

    public class LuceneProfiler : IDisposable {

        //--- Types ---
        public interface IProfileTimer : IDisposable { }

        private class ProfileTimer : IProfileTimer {

            //--- Fields ---
            private readonly long _start;
            private readonly Stopwatch _timer;
            public long Elapsed;

            //--- constructors ---
            public ProfileTimer(Stopwatch timer) {
                _timer = timer;
                _start = timer.ElapsedMilliseconds;
            }

            //--- Methods ---
            public void Dispose() {
                Elapsed = _timer.ElapsedMilliseconds - _start;
            }
        }

        //--- Class Fields ---
        private static readonly ILog _log = LogUtils.CreateLog();

        //--- Fields ---
        private readonly TimeSpan _slowQueryThreshhold;
        private readonly string _queryType;
        private readonly string _wikiId;
        private readonly Stopwatch _timer = Stopwatch.StartNew();
        private ProfileTimer _parseTime;
        private ProfileTimer _queryTime;
        private ProfileTimer _queryInternalsTime;
        private ProfileTimer _processingTime;
        private string _rawQuery;
        private int _count;

        //--- Constructors ---
        public LuceneProfiler(TimeSpan slowQueryThreshhold, string queryType, string wikiId) {
            _slowQueryThreshhold = slowQueryThreshhold;
            _queryType = queryType;
            _wikiId = wikiId;
        }

        //--- Properties ---
        private long ParseTime { get { return _parseTime == null ? -1 : _parseTime.Elapsed; } }
        private long QueryTime { get { return _queryTime == null ? -1 : _queryTime.Elapsed; } }
        private long ProcessingTime { get { return _processingTime == null ? -1 : _processingTime.Elapsed; } }

        private long LockContention {
            get {
                var queryTime = QueryTime;
                if(_queryInternalsTime == null || queryTime == -1) {
                    return 0;
                }
                return QueryTime - _queryInternalsTime.Elapsed;
            }
        }

        //--- Methods ---
        public IProfileTimer ProfileParse(string query) {
            _rawQuery = query;
            return _parseTime = new ProfileTimer(_timer);
        }

        public IProfileTimer ProfileQuery() {
            return _queryTime = new ProfileTimer(_timer);
        }

        public IProfileTimer ProfileQueryInternals() {
            return _queryInternalsTime = new ProfileTimer(_timer);
        }

        public IProfileTimer ProfilePostProcess(int count) {
            _count = count;
            return _processingTime = new ProfileTimer(_timer);
        }

        public XDoc GetProfile() {
            return new XDoc("explain")
                .Elem("count", _count)
                .Elem("querytime", QueryTime)
                .Elem("parsetime", ParseTime)
                .Elem("processingtime", ProcessingTime)
                .Elem("totaltime", _timer.ElapsedMilliseconds);
        }

        public void Dispose() {
            _timer.Stop();
            if(_timer.Elapsed >= _slowQueryThreshhold) {
                _log.WarnFormat("Slow {0} query for '{1}': count: {2}, totaltime: {3}ms, querytime: {4}ms, parsetime: {5}ms, processingtime: {6}ms, locktime: {7}ms, query: {8}", _queryType, _wikiId, _count, _timer.ElapsedMilliseconds, QueryTime, ParseTime, ProcessingTime, LockContention, _rawQuery);
            }
        }
    }
}
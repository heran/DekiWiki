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

using System.Text;
using System.Diagnostics;
using System.Collections;
using log4net;

namespace MindTouch.Deki.Data {
    public class LoggingDekiDataSession : ADekiDataSessionLogger {

        //--- Class Fields ---
        private static readonly ILog _log = LogUtils.CreateLog();

        //--- Class Properties ---
        public static bool IsLoggingEnabled { get { return _log.IsTraceEnabled(); } }

        //--- Class Methods ---
        private static string ParameterToString(object p) {
            if(p == null) {
                return "[null]";
            }
            if(p is string) {
                return (string)p;
            }
            if(p is IEnumerable) {
                StringBuilder s = new StringBuilder("[");
                bool comma = false;
                foreach(object o in (IEnumerable)p) {
                    s.AppendFormat("{0}{1}", comma ? ", " : string.Empty, ParameterToString(o));
                    comma = true;
                }
                s.Append("]");
                return s.ToString();
            }
            if(p is PageBE) {
                return ((PageBE)p).ID.ToString();
            }
            if(p is UserBE) {
                return ((UserBE)p).ID.ToString();
            }
            if(p is Title) {
                return ((Title)p).AsPrefixedDbPath();
            }
            return p.ToString();
        }

        //--- Fields ---
        private long _loggedCallsCount;

        //--- Constructors ---
        public LoggingDekiDataSession(IDekiDataSession nextSession) : base(nextSession) { }

        //--- Methods ---
        protected override void LogQuery(string category, string function, Stopwatch sw, params object[] parameters) {
            sw.Stop();
            System.Threading.Interlocked.Increment(ref _loggedCallsCount);
            StringBuilder parametersSb = new StringBuilder();
            if(parameters != null) {
                bool comma = false;
                foreach(object p in parameters) {
                    parametersSb.AppendFormat("{0}{1}", comma ? ", " : string.Empty, ParameterToString(p));
                    comma = true;
                }
            }
            _log.TraceFormat("{0,25} {1,25} [{2,25}] | Duration: {3,5}ms", category + ".", function, parametersSb.ToString(), sw.ElapsedMilliseconds);
        }
    }
}
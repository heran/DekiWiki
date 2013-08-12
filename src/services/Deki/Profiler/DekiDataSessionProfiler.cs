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
using MindTouch.Deki.Data;

namespace MindTouch.Deki.Profiler {
    internal class DekiDataSessionProfiler : ADekiDataSessionLogger {

        //--- Types ---
        public class DataAccessProfile {

            //--- Properties ---
            public string Category { get; internal set; }
            public string Function { get; internal set; }
            public TimeSpan Elapsed { get; internal set; }
        }

        //--- Fields ---
        private readonly List<DataAccessProfile> _history = new List<DataAccessProfile>();

        //--- Constructors ---
        public DekiDataSessionProfiler(IDekiDataSession nextSession) : base(nextSession) { }

        //--- Properties ---
        public List<DataAccessProfile> History {
            get {
                lock(_history) {
                    return new List<DataAccessProfile>(_history);
                }
            }
        }

        //--- Methods ---
        protected override void LogQuery(string category, string function, Stopwatch sw, params object[] parameters) {
            sw.Stop();
            lock(_history) {
                _history.Add(new DataAccessProfile {
                    Category = category,
                    Function = function,
                    Elapsed = sw.Elapsed
                });
            }
        }
    }
}
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
using System.Text;
using System.Threading;

namespace MindTouch.Deki.Caching {
    public class NullCache : ICacheProvider {

        //--- Class Fields ---
        public static readonly ICacheProvider Instance = new NullCache();

        //--- Constructors ---
        private NullCache() { }

        //--- Fields ---
        private long _misses;
        private long _puts;

        //--- Properties ---
        public long Hits { get { return 0; } }
        public long Misses { get{ return _misses;} }
        public long Puts { get { return _puts; } }
        public long Deletes { get { return 0; } }
        
        public object this[string key] {
            get {
                return Get(key);
            }
            set {
                Set(key, value);
            }
        }

        //--- Methods ---
        public int Delete(params string[] keys) {
            if(ArrayUtil.IsNullOrEmpty(keys)) {
                throw new ArgumentNullException("keys");
            }
            return 0;
        }

        public void Set(string key, object val, TimeSpan expiration) {
            Interlocked.Increment(ref _puts);
        }
        
        public void Set(string key, object val) {
            Interlocked.Increment(ref _puts);
        }
        
        public void Set(string[] keys, object val) {
            Interlocked.Increment(ref _puts);
        }
        
        public void Set(string[] keys, object val, TimeSpan expiration) {
            Interlocked.Increment(ref _puts);
        }

        public void Set(string key, object val, DateTime expires) {
            Interlocked.Increment(ref _puts);
        }

        public void Set(string[] keys, object val, DateTime expires) {
            Interlocked.Increment(ref _puts);
        }

        public T Get<T>(string key, T def) {
            Interlocked.Increment(ref _misses);
            return def;
        }

        public bool TryGet(string key, out object value) {
            value = null;
            Interlocked.Increment(ref _misses);
            return false;
        }

        public object Get(string key) {
            Interlocked.Increment(ref _misses);
            return null;
        }

        public Dictionary<string, T> Get<T>(string[] keys) {
            return new Dictionary<string, T>();
        }

        public void Dispose() {
        }
    }
}

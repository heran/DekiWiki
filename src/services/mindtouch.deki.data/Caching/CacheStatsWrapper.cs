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
using System.Linq;

namespace MindTouch.Deki.Caching {
    public class CacheStatsWrapper : ICacheProvider {

        // --- Fields ---
        private readonly ICacheProvider _cacheProvider;
        private long _hits;
        private long _misses;
        private long _puts;
        private long _deletes;

        // --- Constructors ---
        public CacheStatsWrapper(ICacheProvider cacheProvider) {
            if(cacheProvider == null) {
                throw new ArgumentNullException("cacheProvider");
            }
            _cacheProvider = cacheProvider;
        }

        // --- Properties ---
        public long Hits { get { return _hits; } }
        public long Misses { get { return _misses; } }
        public long Puts { get { return _puts; } }
        public long Deletes { get { return _deletes; } }

        public object this[string key] {
            get {
                return Get(key);
            }
            set {
                Set(key, value);
            }
        }

        // --- Methods ---
        public int Delete(params string[] keys) {
            int ret = _cacheProvider.Delete(keys);
            if(ret > 0) {
                Interlocked.Add(ref _deletes, ret);
            }
            return ret;
        }

        public object Get(string key) {
            return Get<object>(key, null);
        }

        public T Get<T>(string key, T def) {
            object o;
            return (TryGet(key, out o)) ? (T)o : def;
        }

        public bool TryGet(string key, out object value) {
            bool ret = _cacheProvider.TryGet(key, out value);
            if(ret) {
                Interlocked.Increment(ref _hits);
            } else {
                Interlocked.Increment(ref _misses);
            }
            return ret;
        }

        public Dictionary<string, T> Get<T>(string[] keys) {
            if(keys == null || keys.Length == 0) {
                return new Dictionary<string, T>();
            }
            keys = keys.Distinct().ToArray();
            var ret = _cacheProvider.Get<T>(keys);
            if(ret.Count > 0) {
                Interlocked.Add(ref _hits, ret.Count);
            }
            if(keys.Length - ret.Count > 0) {
                Interlocked.Add(ref _misses, keys.Length - ret.Count);
            }
            return ret;
        }

        public void Set(string key, object val, TimeSpan expiration) {
            _cacheProvider.Set(key, val, expiration);
            Interlocked.Increment(ref _puts);
        }

        public void Set(string key, object val) {
            _cacheProvider.Set(key, val);
            Interlocked.Increment(ref _puts);
        }

        public void Set(string[] keys, object val, TimeSpan expiration) {
            _cacheProvider.Set(keys, val, expiration);
            Interlocked.Increment(ref _puts);
        }

        public void Set(string[] keys, object val) {
            _cacheProvider.Set(keys, val);
            Interlocked.Increment(ref _puts);
        }

        public void Set(string key, object val, DateTime expires) {
            _cacheProvider.Set(key, val, expires);
            Interlocked.Increment(ref _puts);
        }

        public void Set(string[] keys, object val, DateTime expires) {
            _cacheProvider.Set(keys, val, expires);
            Interlocked.Increment(ref _puts);
        }

        public void Dispose() {
            _cacheProvider.Dispose();
        }
    }
}

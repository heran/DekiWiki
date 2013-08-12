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
using System.Linq;
using System.Threading;

namespace MindTouch.Deki.Caching {
    public class CacheIterator : ICacheProvider {

        // --- Fields ---
        private readonly ICacheProvider[] _cacheProviders;
        private long _hits;
        private long _misses;
        private long _puts;
        private long _deletes;

        // --- Constructors ---
        public CacheIterator(ICacheProvider l1Cache, ICacheProvider l2Cache) {
            if(l1Cache == null || l2Cache == null) {
                throw new ArgumentNullException();
            }
            _cacheProviders = new ICacheProvider[] { l1Cache, l2Cache };
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
            if(keys == null || keys.Length == 0) {
                return 0;
            }
            keys = keys.Distinct().ToArray();

            // delete from all providers
            for(int i = _cacheProviders.Length - 1; i >= 0; i--) {
                _cacheProviders[i].Delete(keys);
            }
            Interlocked.Add(ref _deletes, keys.Length);
            return keys.Length;
        }

        public object Get(string key) {
            return Get<object>(key, null);
        }

        public T Get<T>(string key, T def) {
            object o;
            return (TryGet(key, out o)) ? (T)o : def;
        }

        public bool TryGet(string key, out object value) {
            bool ret = false;
            value = null;
            for(int i = 0; i < _cacheProviders.Length; i++) {
                if(_cacheProviders[i].TryGet(key, out value)) {
                    ret = true;

                    // seed the cache layers before the one that had the item                        
                    for(int j = 0; j < i; j++) {
                        _cacheProviders[j][key] = value;
                    }                    
                    break;
                }
            }
            if(ret) {
                Interlocked.Increment(ref _hits);
            } else {
                Interlocked.Increment(ref _misses);
            }
            return ret;
        }

        public Dictionary<string, T> Get<T>(string[] keys) {
            var ret = new Dictionary<string, T>();
            if(keys == null || keys.Length == 0) {
                return ret;
            }

            keys = keys.Distinct().ToArray();
            if(keys.Length == 1) {
                object o = null;
                if(TryGet(keys[0], out o)) {
                    ret[keys[0]] = (T)o;
                }
                return ret;
            }

            var keysToLookup = new List<string>(keys);
            for(int i = 0; i < _cacheProviders.Length; i++) {
                if(keysToLookup.Count > 0) {
                    var partialSet = _cacheProviders[i].Get<T>(keysToLookup.ToArray());
                    foreach(KeyValuePair<string, T> cachedObjects in partialSet) {
                        keysToLookup.Remove(cachedObjects.Key);
                        ret[cachedObjects.Key] = cachedObjects.Value;
                        for(int k = 0; k < i; k++) {
                            _cacheProviders[k][cachedObjects.Key] = cachedObjects.Value;
                        }
                    }
                }
            }
            if(ret.Count > 0) {
                Interlocked.Add(ref _hits, ret.Count);
            }
            if(keys.Length - ret.Count > 0) {
                Interlocked.Add(ref _misses, keys.Length - ret.Count);
            }
            return ret;
        }

        public void Set(string key, object val, TimeSpan expiration) {
            foreach(ICacheProvider c in _cacheProviders) {
                c.Set(key, val, expiration);
            }
            Interlocked.Increment(ref _puts);
        }

        public void Set(string key, object val) {
            foreach(ICacheProvider c in _cacheProviders) {
                c.Set(key, val);
            }
            Interlocked.Increment(ref _puts);
        }

        public void Set(string[] keys, object val, TimeSpan expiration) {
            foreach(ICacheProvider c in _cacheProviders) {
                c.Set(keys, val, expiration);
            }
            Interlocked.Increment(ref _puts);
        }

        public void Set(string[] keys, object val) {
            foreach(ICacheProvider c in _cacheProviders) {
                c.Set(keys, val);
            }
            Interlocked.Increment(ref _puts);
        }

        public void Set(string key, object val, DateTime expires) {
            foreach(ICacheProvider c in _cacheProviders) {
                c.Set(key, val, expires);
            }
            Interlocked.Increment(ref _puts);
        }

        public void Set(string[] keys, object val, DateTime expires) {
            foreach(ICacheProvider c in _cacheProviders) {
                c.Set(keys, val, expires);
            }
            Interlocked.Increment(ref _puts);
        }

        public void Dispose() {
            foreach(ICacheProvider c in _cacheProviders) {
                c.Dispose();
            }
        }
    }
}

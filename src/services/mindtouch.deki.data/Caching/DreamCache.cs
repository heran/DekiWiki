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
using System.Linq;

using MindTouch.Collections;
using MindTouch.Tasking;

namespace MindTouch.Deki.Caching {
    public class DreamCache : ICacheProvider {

        // --- Constants ---
        const int DEFAULT_EXPIRATION_SECS = 300;

        // --- Types ---
        private class CacheItem : IDisposable {

            //--- Fields ---
            public object Value;
            public string[] Keys;
            public TimeSpan SlidingExpiration;
            public DateTime Expires;

            //--- Methods ---
            public void Dispose() {
                Value = null;
            }

            public override string ToString() {
                return "DreamCache+CacheItem:" + string.Join(",", Keys);
            }
        }

        // --- Constructors ---
        public DreamCache(TaskTimerFactory taskTimerFactory) : this(taskTimerFactory, TimeSpan.FromSeconds(DEFAULT_EXPIRATION_SECS)) { }

        public DreamCache(TaskTimerFactory taskTimerFactory, TimeSpan defaultExpiration) {
            _expirationTime = defaultExpiration;
            _expirations = new ExpiringHashSet<CacheItem>(taskTimerFactory);
            _expirations.EntryExpired += OnExpiration;
        }

        // --- Fields ---
        private readonly TimeSpan _expirationTime;
        private readonly ExpiringHashSet<CacheItem> _expirations;
        private readonly Dictionary<string, CacheItem> _cache = new Dictionary<string, CacheItem>();
        private long _hits;
        private long _misses;
        private long _puts;
        private long _deletes;

        //--- Properties ---
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

        //--- Methods ---
        public int Delete(params string[] keys) {
            int numDeleted = 0;
            if(ArrayUtil.IsNullOrEmpty(keys)) {
                throw new ArgumentNullException("keys");
            }
            lock(_cache) {
                foreach(string key in keys) {
                    CacheItem item;
                    if(_cache.TryGetValue(key, out item)) {
                        Delete(item);
                        numDeleted++;
                    }
                }
                _deletes += numDeleted;
            }
            return numDeleted;
        }

        public object Get(string key) {
            object o;
            TryGet(key, out o);
            return o;
        }

        public T Get<T>(string key, T def) {
            object o = Get(key);
            return (o != null) ? (T)o : def;
        }

        public bool TryGet(string key, out object value) {
            lock(_cache) {
                CacheItem item;
                if(_cache.TryGetValue(key, out item)) {
                    value = item.Value;
                    RefreshTimer(item, false);
                    ++_hits;
                    return true;
                } else {
                    value = null;
                    --_misses;
                    return false;
                }
            }
        }

        public Dictionary<string, T> Get<T>(string[] keys) {
            Dictionary<string, T> ret = new Dictionary<string, T>();
            if(keys == null || keys.Length == 0) {
                return ret;
            }
            keys = keys.Distinct().ToArray();
            lock(_cache) {
                for(int i = 0; i < keys.Length; i++) {
                    object o;
                    if(TryGet(keys[i], out o)) {
                        ret[keys[i]] = (T)o;
                    }
                }
            }
            return ret;
        }

        public void Set(string key, object val, TimeSpan slidingExpiration) {
            SetInternal(new string[] { key }, val, slidingExpiration, DateTime.MaxValue);
        }

        public void Set(string key, object val, DateTime expires) {
            SetInternal(new string[] { key }, val, TimeSpan.MaxValue, expires);
        }

        public void Set(string key, object val) {
            SetInternal(new string[] { key }, val, _expirationTime, DateTime.MaxValue);
        }

        public void Set(string[] keys, object val) {
            SetInternal(keys, val, _expirationTime, DateTime.MaxValue);
        }

        public void Set(string[] keys, object val, DateTime expires) {
            SetInternal(keys, val, TimeSpan.MaxValue, expires);
        }

        public void Set(string[] keys, object val, TimeSpan slidingExpiration) {
            SetInternal(keys, val, slidingExpiration, DateTime.MaxValue);
        }

        private void SetInternal(string[] keys, object val, TimeSpan slidingExpiration, DateTime expires) {
            if(ArrayUtil.IsNullOrEmpty(keys)) {
                throw new ArgumentNullException("keys");
            }
            var item = new CacheItem {
                Keys = keys,
                Value = val,
                SlidingExpiration = slidingExpiration,
                Expires = expires
            };
            lock(_cache) {
                Delete(keys);
                foreach(string k in keys) {
                    _cache[k] = item;
                }
                RefreshTimer(item, true);
                ++_puts;
            }
        }

        private void RefreshTimer(CacheItem item, bool isNew) {

            if(item.SlidingExpiration != TimeSpan.MaxValue) {

                // set sliding time
                _expirations.SetExpiration(item, item.SlidingExpiration);
            } else if(isNew && item.Expires != DateTime.MaxValue) {

                // only set absolute timeout on new items that don't have a maxvalue expiration date
                _expirations.SetExpiration(item, item.Expires);
            }
        }

        private void OnExpiration(object sender, ExpirationEventArgs<CacheItem> e) {
            lock(_cache) {
                Delete(e.Entry.Value);
            }
        }

        private void Delete(CacheItem item) {
            if(item.Value != null) {
                string[] itemKeys = item.Keys;
                item.Dispose();
                _expirations.RemoveExpiration(item);

                // Remove the lookup by this key to this item.
                foreach(string itemKey in itemKeys) {
                    _cache.Remove(itemKey);
                }
            }
        }

        public void Dispose() {
            _expirations.EntryExpired -= OnExpiration;
            _expirations.Dispose();
        }
    }
}

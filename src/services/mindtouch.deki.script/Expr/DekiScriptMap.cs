/*
 * MindTouch DekiScript - embeddable web-oriented scripting runtime
 * Copyright (c) 2006-2010 MindTouch Inc.
 * www.mindtouch.com  oss@mindtouch.com
 *
 * For community documentation and downloads visit wiki.developer.mindtouch.com;
 * please review the licensing section.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 *     http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using MindTouch.Deki.Script.Runtime;
using MindTouch.Xml;

namespace MindTouch.Deki.Script.Expr {
    public class DekiScriptMap : DekiScriptComplexLiteral {

        //--- Class Methods ---
        private static void FillDictionaryRecursively(DekiScriptMap map, Dictionary<string, DekiScriptLiteral> dictionary) {
            if(map.Outer != null) {
                FillDictionaryRecursively(map.Outer, dictionary);
            }
            foreach(var entry in map._value) {
                dictionary[entry.Key] = entry.Value;
            }
        }

        //--- Fields ---
        private readonly Dictionary<string, DekiScriptLiteral> _value = new Dictionary<string, DekiScriptLiteral>(StringComparer.OrdinalIgnoreCase);
        public readonly DekiScriptMap Outer;
        private bool _readonly;

        //--- Constructors ---
        public DekiScriptMap() : this(null, null) { }
        public DekiScriptMap(Hashtable value) : this(value, null) { }
        public DekiScriptMap(DekiScriptMap outer) : this(null, outer) { }

        public DekiScriptMap(Hashtable value, DekiScriptMap outer) {
            this.Outer = outer;
            if(value != null) {
                foreach(DictionaryEntry entry in value) {
                    Add(entry.Key.ToString(), FromNativeValue(entry.Value));
                }
            }
        }

        //--- Properties ---
        public override DekiScriptType ScriptType { get { return DekiScriptType.MAP; } }
        public bool IsReadOnly { get { return _readonly; } }

        public bool IsEmpty {
            get {
                return _value.Count == 0 && (Outer == null || Outer.IsEmpty);
            }
        }

        public Dictionary<string, DekiScriptLiteral> Value {
            get {
                if(Outer == null) {
                    return _value;
                }
                var value = new Dictionary<string, DekiScriptLiteral>();
                FillDictionaryRecursively(this, value);
                return value;
            }
        }

        public override object NativeValue {
            get {
                Hashtable result = new Hashtable(StringComparer.OrdinalIgnoreCase);
                foreach(KeyValuePair<string, DekiScriptLiteral> entry in Value) {
                    result[entry.Key] = entry.Value.NativeValue;
                }
                return result;
            }
        }

        public DekiScriptLiteral this[string name] {
            get {
                DekiScriptLiteral result;
                if(!_value.TryGetValue(name, out result)) {
                    if(Outer != null) {
                        return Outer[name];
                    }
                    return DekiScriptNil.Value;
                }
                return result;
            }
            set {

                // check if collection is readonly
                if(_readonly) {
                    throw new ReadOnlyException(string.Format("map is read-only (attempted to set key '{0}')", name));
                }

                // find defining scope (if any)
                DekiScriptMap scope = this;
                while((scope != null) && !scope._value.ContainsKey(name) && (scope.Outer == null || !scope.Outer.IsReadOnly)) {
                    scope = scope.Outer;
                }

                // check if we found a scope
                if(scope != null) {
                    scope._value[name] = value;
                } else {
                    Add(name, value);
                }
            }
        }

        public DekiScriptLiteral this[DekiScriptLiteral index] {
            get {
                if(index == null) {
                    throw new ArgumentNullException("index");
                }
                if((index.ScriptType == DekiScriptType.NUM) || (index.ScriptType == DekiScriptType.STR)) {
                    return this[SysUtil.ChangeType<string>(index.NativeValue)];
                } else {
                    throw new DekiScriptBadTypeException(Location.None, index.ScriptType, new[] { DekiScriptType.NUM, DekiScriptType.STR });
                }
            }
            set {
                if(index == null) {
                    throw new ArgumentNullException("index");
                }
                if((index.ScriptType == DekiScriptType.NUM) || (index.ScriptType == DekiScriptType.STR)) {
                    this[SysUtil.ChangeType<string>(index.NativeValue)] = value;
                } else {
                    throw new DekiScriptBadTypeException(Location.None, index.ScriptType, new[] { DekiScriptType.NUM, DekiScriptType.STR });
                }
            }
        }

        //--- Methods ---
        public DekiScriptMap Add(string key, DekiScriptLiteral value) {
            if(key == null) {
                throw new ArgumentNullException("key");
            }

            // check if colleciton is readonly
            if(_readonly) {
                throw new ReadOnlyException(string.Format("map is read-only (attempted to add key '{0}')", key));
            }

            // update value
            if(value == null) {
                _value.Remove(key);
            } else {
                _value[key] = value;
            }
            return this;
        }

        public DekiScriptMap AddAt(string[] keys, DekiScriptLiteral value) {
            return AddAt(keys, value, true);
        }

        public DekiScriptMap AddAt(string[] keys, DekiScriptLiteral value, bool ignoreOnError) {
            if(ArrayUtil.IsNullOrEmpty(keys)) {
                throw new ArgumentNullException("keys");
            }
            if(value == null) {
                throw new ArgumentNullException("value");
            }

            // loop over all keys, except last, to get to the last map
            string key;
            DekiScriptMap current = this;
            for(int i = 0; i < keys.Length - 1; ++i) {
                if(keys[i] == null) {
                    if(ignoreOnError) {
                        return this;
                    }
                    throw new ArgumentException(string.Format("keys[{0}] is null", i));
                }
                key = keys[i];
                DekiScriptLiteral next = current[key];
                if(next.ScriptType == DekiScriptType.NIL) {
                    next = new DekiScriptMap();
                    current.Add(key, next);
                } else if(next.ScriptType != DekiScriptType.MAP) {
                    if(ignoreOnError) {
                        return this;
                    }
                    throw new Exception(string.Format("entry at '{0}' is not a map", string.Join(".", keys, 0, i + 1)));
                }
                current = (DekiScriptMap)next;
            }

            // add new item using the final key
            current.Add(keys[keys.Length - 1], value);
            return this;
        }

        public DekiScriptMap AddAt(string path, DekiScriptLiteral value) {
            if(path == null) {
                throw new ArgumentNullException("path");
            }
            if(value == null) {
                throw new ArgumentNullException("value");
            }
            return AddAt(path.Split('.'), value);
        }

        public DekiScriptMap AddNativeValueAt(string path, object value) {
            if(path == null) {
                throw new ArgumentNullException("path");
            }
            return AddAt(path.Split('.'), FromNativeValue(value));
        }

        public DekiScriptLiteral GetAt(string[] keys) {
            if(ArrayUtil.IsNullOrEmpty(keys)) {
                return DekiScriptNil.Value;
            }
            DekiScriptMap container = this;
            for(int i = 0; i < keys.Length - 1; ++i) {
                container = container[keys[i]] as DekiScriptMap;
                if(container == null) {
                    return DekiScriptNil.Value;
                }
            }
            return container[keys[keys.Length - 1]];
        }

        public DekiScriptLiteral GetAt(string path) {
            if(path == null) {
                throw new ArgumentNullException("path");
            }
            return GetAt(path.Split('.'));
        }

        public DekiScriptMap AddRange(DekiScriptMap map) {

            // check if colleciton is readonly
            if(_readonly) {
                throw new ReadOnlyException("map is read-only");
            }

            // add values
            foreach(KeyValuePair<string, DekiScriptLiteral> entry in map.Value) {
                _value[entry.Key] = entry.Value;
            }
            return this;
        }

        public bool TryGetValue(string name, out DekiScriptLiteral value) {
            if(!_value.TryGetValue(name, out value)) {
                if(Outer != null) {
                    return Outer.TryGetValue(name, out value);
                }
                value = DekiScriptNil.Value;
                return false;
            }
            return true;
        }

        public bool TryGetValue(DekiScriptLiteral index, out DekiScriptLiteral value) {
            if(index == null) {
                throw new ArgumentNullException("index");
            }
            if((index.ScriptType == DekiScriptType.NUM) || (index.ScriptType == DekiScriptType.STR)) {
                return TryGetValue(SysUtil.ChangeType<string>(index.NativeValue), out value);
            } else {
                throw new DekiScriptBadTypeException(Location.None, index.ScriptType, new[] { DekiScriptType.NUM, DekiScriptType.STR });
            }
        }

        public XDoc ToXml() {
            XDoc result = new XDoc("value").Attr("type", ScriptTypeName);
            AppendXml(result);
            return result;
        }

        public override void AppendXml(XDoc doc) {
            if(Outer != null) {
                Outer.AppendXml(doc);
            }
            foreach(KeyValuePair<string, DekiScriptLiteral> entry in _value) {
                doc.Start("value").Attr("key", entry.Key).Attr("type", entry.Value.ScriptTypeName);
                entry.Value.AppendXml(doc);
                doc.End();
            }
        }

        public override DekiScriptLiteral Convert(DekiScriptType type) {
            switch(type) {
            case DekiScriptType.ANY:
            case DekiScriptType.MAP:
                return this;
            case DekiScriptType.LIST: {
                DekiScriptList result = new DekiScriptList();
                foreach(DekiScriptLiteral value in Value.Values) {
                    result.Add(value);
                }
                return result;
            }
            }
            throw new DekiScriptInvalidCastException(Location, ScriptType, type);
        }

        public override TReturn VisitWith<TState, TReturn>(IDekiScriptExpressionVisitor<TState, TReturn> visitor, TState state) {
            return visitor.Visit(this, state);
        }

        public void MakeReadOnly() {
            _readonly = true;
        }
    }
}
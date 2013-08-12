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
using MindTouch.Deki.Script.Runtime;
using MindTouch.Xml;

namespace MindTouch.Deki.Script.Expr {
    public class DekiScriptList : DekiScriptComplexLiteral {

        //--- Fields ---
        public readonly List<DekiScriptLiteral> Value = new List<DekiScriptLiteral>();

        //--- Constructors ---
        public DekiScriptList() { }

        public DekiScriptList(IEnumerable values) {
            foreach(object entry in values) {
                Add(FromNativeValue(entry));
            }
        }

        public DekiScriptList(ArrayList value) {
            foreach(object entry in value) {
                Add(FromNativeValue(entry));
            }
        }

        public DekiScriptList(DekiScriptList value) {
            if(value == null) {
                throw new ArgumentNullException("value");
            }
            this.Value.AddRange(value.Value);
        }

        //--- Properties ---
        public override DekiScriptType ScriptType { get { return DekiScriptType.LIST; } }

        public override object NativeValue {
            get {
                ArrayList result = new ArrayList();
                foreach(DekiScriptLiteral entry in Value) {
                    result.Add(entry.NativeValue);
                }
                return result;
            }
        }

        public DekiScriptLiteral this[int index] {
            get {
                if(index < 0) {
                    index += Value.Count;                    
                }
                return ((index >= 0) && (index < Value.Count)) ? Value[index] : DekiScriptNil.Value;
            }
        }

        public DekiScriptLiteral this[DekiScriptLiteral index] {
            get {
                if(index == null) {
                    return DekiScriptNil.Value;
                }
                if(index.ScriptType == DekiScriptType.NUM) {
                    return this[SysUtil.ChangeType<int>(index.NativeValue)];
                } else {
                    return DekiScriptNil.Value;
                }
            }
        }

        //--- Methods ---
        public DekiScriptList Add(DekiScriptLiteral value) {
            if(value == null) {
                throw new ArgumentNullException("value");
            }
            Value.Add(value);
            return this;
        }

        public DekiScriptList AddNativeValue(object value) {
            return Add(FromNativeValue(value));
        }

        public DekiScriptList AddRange(DekiScriptList list) {
            Value.AddRange(list.Value);
            return this;
        }

        public XDoc ToXml() {
            XDoc result = new XDoc("value").Attr("type", ScriptTypeName);
            AppendXml(result);
            return result;
        }

        public override void AppendXml(XDoc doc) {
            foreach(DekiScriptLiteral entry in Value) {
                doc.Start("value").Attr("key", "#").Attr("type", entry.ScriptTypeName);
                entry.AppendXml(doc);
                doc.End();
            }
        }

        public override DekiScriptLiteral Convert(DekiScriptType type) {
            switch(type) {
            case DekiScriptType.ANY:
            case DekiScriptType.LIST:
                return this;
            }
            throw new DekiScriptInvalidCastException(Location, ScriptType, type);
        }

        public override TReturn VisitWith<TState, TReturn>(IDekiScriptExpressionVisitor<TState, TReturn> visitor, TState state) {
            return visitor.Visit(this, state);
        }
    }
}
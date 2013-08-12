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
using MindTouch.Deki.Script.Runtime;
using MindTouch.Xml;

namespace MindTouch.Deki.Script.Expr {
    public class DekiScriptBool : DekiScriptLiteral {

        //--- Class Fields ---
        public static DekiScriptBool True = new DekiScriptBool(true);
        public static DekiScriptBool False = new DekiScriptBool(false);

        //--- Fields ---
        public readonly bool Value;

        //--- Constructors ---
        private DekiScriptBool(bool value) {
            this.Value = value;
        }

        //--- Properties ---
        public override DekiScriptType ScriptType { get { return DekiScriptType.BOOL; } }
        public override object NativeValue { get { return Value; } }

        //--- Methods ---
        public override bool? AsBool() { return Value; }
        public override double? AsNumber() { return Value ? 1.0 : 0.0; }
        public override string AsString() { return ToString(); }
        public override string ToString() { return Value ? "true" : "false"; }

        public override DekiScriptLiteral Convert(DekiScriptType type) {
            switch(type) {
            case DekiScriptType.ANY:
            case DekiScriptType.BOOL:
                return this;
            case DekiScriptType.NUM:
                return Constant(Value ? 1 : 0);
            case DekiScriptType.STR:
                return Constant(AsString());
            }
            throw new DekiScriptInvalidCastException(Location, ScriptType, type);
        }

        public override XDoc AsEmbeddableXml(bool safe) {
            return new XDoc("html").Start("body").Value(ToString()).End();
        }

        public override TReturn VisitWith<TState, TReturn>(IDekiScriptExpressionVisitor<TState, TReturn> visitor, TState state) {
            return visitor.Visit(this, state);
        }
    }
}
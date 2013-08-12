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

using MindTouch.Deki.Script.Runtime;
using MindTouch.Xml;

namespace MindTouch.Deki.Script.Expr {
    public class DekiScriptNumber : DekiScriptLiteral {

        //--- Class Fields ---
        public static readonly DekiScriptNumber Zero = new DekiScriptNumber(0.0);

        //--- Fields ---
        public readonly double Value;

        //--- Constructors ---
        internal DekiScriptNumber(double value) {
            this.Value = value;
        }

        //--- Properties ---
        public override DekiScriptType ScriptType { get { return DekiScriptType.NUM; } }
        public override object NativeValue { get { return Value; } }

        //--- Methods ---
        public override bool? AsBool() { return Value != 0.0; }
        public override double? AsNumber() { return Value; }
        public override string AsString() { return ToString(); }
        public override string ToString() { return Value.ToString(); }

        public override DekiScriptLiteral Convert(DekiScriptType type) {
            switch(type) {
            case DekiScriptType.ANY:
            case DekiScriptType.NUM:
                return this;
            case DekiScriptType.BOOL:
                return Constant(AsBool());
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
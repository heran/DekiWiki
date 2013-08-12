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
using System.Text;
using MindTouch.Deki.Script.Runtime;
using MindTouch.Dream;
using MindTouch.Xml;

namespace MindTouch.Deki.Script.Expr {
    public class DekiScriptString : DekiScriptLiteral {

        //--- Class Fields ---
        public static readonly DekiScriptString Empty = new DekiScriptString(string.Empty);

        //--- Class Methods ---
        public static string QuoteString(string text) {
            return "\"" + EscapeString(text) + "\"";
        }

        public static string EscapeString(string text) {
            if(string.IsNullOrEmpty(text)) {
                return string.Empty;
            }

            // escape any special characters
            StringBuilder result = new StringBuilder(2 * text.Length);
            foreach(char c in text) {
                switch(c) {
                case '\a':
                    result.Append("\\a");
                    break;
                case '\b':
                    result.Append("\\b");
                    break;
                case '\f':
                    result.Append("\\f");
                    break;
                case '\n':
                    result.Append("\\n");
                    break;
                case '\r':
                    result.Append("\\r");
                    break;
                case '\t':
                    result.Append("\\t");
                    break;
                case '\v':
                    result.Append("\\v");
                    break;
                case '"':
                    result.Append("\\\"");
                    break;
                case '\'':
                    result.Append("\\'");
                    break;
                case '\\':
                    result.Append("\\\\");
                    break;
                default:
                    if(char.IsControl(c)) {
                        result.Append("\\u");
                        result.Append(((int)c).ToString("x4"));
                    } else {
                        result.Append(c);
                    }
                    break;
                }
            }
            return result.ToString();
        }

        //--- Fields ---
        public readonly string Value;

        //--- Constructors ---
        internal DekiScriptString(string value) {
            if(value == null) {
                throw new ArgumentNullException("value");
            }
            this.Value = value;
        }

        //--- Properties ---
        public override DekiScriptType ScriptType { get { return DekiScriptType.STR; } }
        public override object NativeValue { get { return Value; } }

        //--- Methods ---
        public override bool? AsBool() {
            bool value;
            if(bool.TryParse(Value, out value)) {
                return value;
            }
            return null;
        }

        public override double? AsNumber() {
            double value;
            if(double.TryParse(Value, out value)) {
                return value;
            }
            return null;
        }

        public override string AsString() { return Value; }

        public override DekiScriptLiteral Convert(DekiScriptType type) {
            switch(type) {
            case DekiScriptType.ANY:
            case DekiScriptType.STR:
                return this;
            case DekiScriptType.BOOL: {
                bool? value = AsBool();
                if(value != null) {
                    return Constant(value.Value);
                }
                break;
            }
            case DekiScriptType.NUM: {
                double? value = AsNumber();
                if(value != null) {
                    return Constant(value.Value);
                }
                break;
            }
            case DekiScriptType.URI: {
                XUri uri = XUri.TryParse(Value);
                if(uri != null) {
                	
                	// NOTE (steveb): need a special converstion here to ensure that the produced
                	//                uri is not executed.
                    return Constant(uri, false);
                }
                break;
            }
            }
            throw new DekiScriptInvalidCastException(Location, ScriptType, type);
        }

        public override XDoc AsEmbeddableXml(bool safe) {
            return new XDoc("html").Start("body").Value(Value).End();
        }

        public override TReturn VisitWith<TState, TReturn>(IDekiScriptExpressionVisitor<TState, TReturn> visitor, TState state) {
            return visitor.Visit(this, state);
        }
    }
}
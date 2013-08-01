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
using MindTouch.Deki.Script.Runtime.Library;
using MindTouch.Xml;

namespace MindTouch.Deki.Script.Expr {
    public class DekiScriptXml : DekiScriptComplexLiteral {

        //--- Fields ---
        public readonly XDoc Value;

        //--- Constructors ---
        internal DekiScriptXml(XDoc value) {
            if(value == null) {
                throw new ArgumentNullException("value");
            }
            this.Value = value;
        }

        internal DekiScriptXml(XDoc value, bool safe) {
            if(value == null) {
                throw new ArgumentNullException("value");
            }
            if(safe) {
                value = DekiScriptLibrary.CleanseHtmlDocument(value);
            }
            this.Value = value;
        }

        //--- Properties ---
        public override DekiScriptType ScriptType { get { return DekiScriptType.XML; } }
        public override object NativeValue { get { return Value; } }

        //--- Methods ---
        public override bool? AsBool() {
            string value = AsString();
            bool result;
            if((value != null) && bool.TryParse(value, out result)) {
                return result;
            }
            return null;
        }

        public override double? AsNumber() {
            string value = AsString();
            double result;
            if((value != null) && double.TryParse(value, out result)) {
                return result;
            }
            return null;
        }

        public override string AsString() {
            XDoc body = Value["body[not(@target)]"];
            return body.IsEmpty ? null : body.Contents;
        }

        public override DekiScriptLiteral Convert(DekiScriptType type) {
            switch(type) {
            case DekiScriptType.ANY:
            case DekiScriptType.XML:
                return this;
            case DekiScriptType.BOOL:
            case DekiScriptType.NUM:
            case DekiScriptType.STR:
            case DekiScriptType.URI: {
                string value = AsString();
                if(value != null) {
                    DekiScriptLiteral str = Constant(value);
                    return str.Convert(type);
                }
                break;
            }
            }
            throw new DekiScriptInvalidCastException(Location, ScriptType, type);
        }

        public override XDoc AsEmbeddableXml(bool safe) {
            XDoc result = Value;
            if(!Value.IsEmpty && !Value.HasName("html")) {
                result = new XDoc("html").Start("body").Add(Value).End();
                if(safe) {
                    foreach(XDoc body in result["body"]) {
                        DekiScriptLibrary.ValidateXHtml(body, true, true);
                    }
                }
            }
            return result;
        }

        public override TReturn VisitWith<TState, TReturn>(IDekiScriptExpressionVisitor<TState, TReturn> visitor, TState state) {
            return visitor.Visit(this, state);
        }
    }
}
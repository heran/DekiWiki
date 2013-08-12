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
using MindTouch.Deki.Script.Runtime;
using MindTouch.Deki.Script.Runtime.Library;
using MindTouch.Dream;
using MindTouch.Xml;

namespace MindTouch.Deki.Script.Expr {
    public class DekiScriptUri : DekiScriptComplexLiteral {

        //--- Fields ---
        public readonly XUri Value;
        public readonly DekiScriptLiteral Arguments;

        //--- Constructors ---
        internal DekiScriptUri(XUri value, DekiScriptLiteral args) {
            if(value == null) {
                throw new ArgumentNullException("value");
            }
            if(args == null) {
                throw new ArgumentNullException("args");
            }
            Value = value;
            Arguments = args;
        }

        //--- Properties ---
        public override DekiScriptType ScriptType { get { return DekiScriptType.URI; } }

        public override object NativeValue {
            get {
                if(!Arguments.IsNil) {
                    return Value.WithFragment(Arguments.ToString());
                }
                return Value;
            }
        }

        //--- Methods ---
        public override string AsString() {
            return Value.ToString();
        }

        public override DekiScriptLiteral Convert(DekiScriptType type) {
            switch(type) {
            case DekiScriptType.ANY:
            case DekiScriptType.URI:
                return this;
            case DekiScriptType.STR:
                return Constant(AsString());
            }
            throw new DekiScriptInvalidCastException(Location, ScriptType, type);
        }

        public override XDoc AsEmbeddableXml(bool safe) {
            MimeType mime = (Value.Segments.Length > 0) ? MimeType.FromFileExtension(Value.LastSegment ?? string.Empty) : MimeType.BINARY;
            if(StringUtil.EqualsInvariant(mime.MainType, "image")) {

                // embed <img> tag
                return DekiScriptLibrary.WebImage(AsString(), null, null, null);
            } else {

                // embed <a> tag
                return DekiScriptLibrary.WebLink(AsString(), null, null, null);
            }
        }

        public override TReturn VisitWith<TState, TReturn>(IDekiScriptExpressionVisitor<TState, TReturn> visitor, TState state) {
            return visitor.Visit(this, state);
        }
    }
}
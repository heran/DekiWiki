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
using MindTouch.Xml;

namespace MindTouch.Deki.Script.Expr {
    public class DekiScriptUnknown : DekiScriptLiteral {

        //--- Class Fields ---
        public static DekiScriptUnknown Value = new DekiScriptUnknown();

        //--- Constructors ---
        private DekiScriptUnknown() { }

        //--- Properties ---
        public override DekiScriptType ScriptType { get { return DekiScriptType.ANY; } }
        public override object NativeValue { get { throw new NotImplementedException("undefined value does not have a native value"); } }

        //--- Methods ---
        public override bool? AsBool() { throw new NotImplementedException("undefined value does not have a native value"); }
        public override double? AsNumber() { throw new NotImplementedException("undefined value does not have a native value"); }
        public override string AsString() { throw new NotImplementedException("undefined value does not have a native value"); }
        public override void AppendXml(XDoc doc) { throw new NotImplementedException("undefined value does not have a native value"); }

        public override DekiScriptLiteral Convert(DekiScriptType type) {
            throw new NotImplementedException("undefined value does not have a native value");
        }

        public override XDoc AsEmbeddableXml(bool safe) {
            throw new NotImplementedException("undefined value does not have a native value");
        }

        public override TReturn VisitWith<TState, TReturn>(IDekiScriptExpressionVisitor<TState, TReturn> visitor, TState state) {
            return visitor.Visit(this, state);
        }
    }
}
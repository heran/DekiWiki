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

namespace MindTouch.Deki.Script.Expr {
    public class DekiScriptGeneratorForeachKeyValue : DekiScriptGenerator {

        //--- Fields ---
        public readonly string Key;
        public readonly string Value;
        public readonly DekiScriptExpression Collection;

        //--- Constructors ---
        public DekiScriptGeneratorForeachKeyValue(Location location, string key, string value, DekiScriptExpression collection, DekiScriptGenerator next) : base(location, next) {
            if(string.IsNullOrEmpty(key)) {
                throw new ArgumentNullException("key");
            }
            if(string.IsNullOrEmpty(value)) {
                throw new ArgumentNullException("value");
            }
            if(collection == null) {
                throw new ArgumentNullException("collection");
            }
            this.Key = key;
            this.Value = value;
            this.Collection = collection;
        }

        //--- Methods ---
        public override TReturn VisitWith<TState, TReturn>(IDekiScriptGeneratorVisitor<TState, TReturn> visitor, TState state) {
            return visitor.Visit(this, state);
        }

        public override string ToString() {
            StringBuilder code = new StringBuilder();
            code.Append("var ");
            code.Append(Key);
            code.Append(" : ");
            code.Append(Value);
            code.Append(" in ");
            code.Append(Collection.ToString());
            ToString(code);
            return code.ToString();
        }
    }
}
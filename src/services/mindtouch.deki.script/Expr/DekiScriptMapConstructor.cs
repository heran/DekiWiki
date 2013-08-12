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

namespace MindTouch.Deki.Script.Expr {
    public class DekiScriptMapConstructor : DekiScriptOperation {

        //--- Types ---
        public class FieldConstructor {

            //--- Fields ---
            public readonly Location Location;
            public readonly DekiScriptExpression Key;
            public readonly DekiScriptExpression Value;

            //--- Constructors ---
            public FieldConstructor(Location location, DekiScriptExpression key, DekiScriptExpression value) {
                if(key == null) {
                    throw new ArgumentNullException("key");
                }
                if(value == null) {
                    throw new ArgumentNullException("value");
                }
                this.Location = location;
                this.Key = key;
                this.Value = value;
            }
        }

        //--- Fields ---
        public readonly FieldConstructor[] Fields;
        public readonly DekiScriptGenerator Generator;

        //--- Constructors ---
        internal DekiScriptMapConstructor(DekiScriptGenerator generator, params FieldConstructor[] fields) {
            if(fields == null) {
                throw new ArgumentNullException("fields");
            }
            for(int i = 0; i < fields.Length; ++i) {
                if(fields[i] == null) {
                    throw new ArgumentNullException(string.Format("fields[{0}]", i));
                }
            }
            this.Generator = generator;
            this.Fields = fields;
        }

        //--- Methods ---
        public override TReturn VisitWith<TState, TReturn>(IDekiScriptExpressionVisitor<TState, TReturn> visitor, TState state) {
            return visitor.Visit(this, state);
        }
    }
}
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
    public class DekiScriptXmlElement : DekiScriptOperation {

        //--- Types ---
        public class Attribute {

            //--- Constants ---
            public const string XMLNS = "xmlns";

            //--- Fields ---
            public readonly string Prefix;
            public readonly DekiScriptExpression Name;
            public readonly DekiScriptExpression Value;
            public readonly bool IsNamespaceDefinition;

            //--- Constructors ---
            internal Attribute(Location location, string prefix, DekiScriptExpression name, DekiScriptExpression value) {
                if(name == null) {
                    throw new ArgumentNullException("name");
                }
                if(value == null) {
                    throw new ArgumentNullException("value");
                }
                this.Location = location;
                this.Prefix = prefix;
                this.Name = name;
                this.Value = value;
                if(string.IsNullOrEmpty(Prefix)) {
                    if(name is DekiScriptString) {
                        string text = ((DekiScriptString)name).Value;
                        this.IsNamespaceDefinition = text.EqualsInvariant(XMLNS);
                    }
                } else {
                    this.IsNamespaceDefinition = Prefix.EqualsInvariant(XMLNS);
                }
            }

            //--- Properties ---
            public Location Location { get; private set; }

            public bool IsDynamic {
                get {
                    if(!(Value is DekiScriptLiteral) || !(Name is DekiScriptLiteral)) {
                        return true;
                    }
                    return (Name is DekiScriptString) && ((DekiScriptString)Name).Value.EqualsInvariant("ctor");
                }
            }
        }

        //--- Fields ---
        public readonly string Prefix;
        public readonly DekiScriptExpression Name;
        public readonly Attribute[] Attributes;
        public readonly DekiScriptExpression Node;

        //--- Constructors ---
        internal DekiScriptXmlElement(string prefix, DekiScriptExpression name, Attribute[] attributes, DekiScriptExpression node) {
            if(name == null) {
                throw new ArgumentNullException("name");
            }
            if(node == null) {
                throw new ArgumentNullException("node");                
            }
            this.Prefix = prefix;
            this.Name = name;
            this.Attributes = attributes ?? new Attribute[0];
            this.Node = node;
        }

        //--- Methods ---
        public override TReturn VisitWith<TState, TReturn>(IDekiScriptExpressionVisitor<TState, TReturn> visitor, TState state) {
            return visitor.Visit(this, state);
        }
    }
}
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
using System.Collections.Generic;
using System.Text;
using System.Xml;
using MindTouch.Deki.Script.Compiler;
using MindTouch.Xml;

namespace MindTouch.Deki.Script.Expr {
    internal class DekiScriptExpressionTextWriter : IDekiScriptExpressionVisitor<StringBuilder, Empty> {

        //--- Class Fields ---
        public static readonly DekiScriptExpressionTextWriter Instance = new DekiScriptExpressionTextWriter();

        //--- Constructors ---
        private DekiScriptExpressionTextWriter() { }

        //--- Methods ---
        public Empty Visit(DekiScriptAbort expr, StringBuilder state) {
            switch(expr.FlowControl) {
            case DekiScriptAbort.Kind.Break:
                state.Append("break");
                break;
            case DekiScriptAbort.Kind.Continue:
                state.Append("continue");
                break;
            default:
                throw new ShouldNeverHappenException();
            }
            return Empty.Value;
        }

        public Empty Visit(DekiScriptAccess expr, StringBuilder state) {
            expr.Prefix.VisitWith(this, state);
            DekiScriptString index = expr.Index as DekiScriptString;
            if((index != null) && DekiScriptParser.IsIdentifier(index.Value)) {
                state.Append(".");
                state.Append(index.Value);
            } else {
                state.Append("[");
                expr.Index.VisitWith(this, state);
                state.Append("]");
            }
            return Empty.Value;
        }

        public Empty Visit(DekiScriptAssign expr, StringBuilder state) {
            if(expr.Define) {
                state.Append("var ");
            } else {
                state.Append("let ");
            }
            state.Append(expr.Variable);
            if(!(expr.Value is DekiScriptNil)) {
                state.Append(" = ");
                expr.Value.VisitWith(this, state);
            }
            return Empty.Value;
        }

        public Empty Visit(DekiScriptBinary expr, StringBuilder state) {
            ScopeVisit(expr.Left, state);
            switch(expr.OpCode) {
            case DekiScriptBinary.Op.Addition:
                state.Append(" + ");
                break;
            case DekiScriptBinary.Op.Division:
                state.Append(" / ");
                break;
            case DekiScriptBinary.Op.Equal:
                state.Append(" == ");
                break;
            case DekiScriptBinary.Op.GreaterOrEqual:
                state.Append(" >= ");
                break;
            case DekiScriptBinary.Op.GreaterThan:
                state.Append(" > ");
                break;
            case DekiScriptBinary.Op.LeftValue:
                break;
            case DekiScriptBinary.Op.LessOrEqual:
                state.Append(" <= ");
                break;
            case DekiScriptBinary.Op.LessThan:
                state.Append(" < ");
                break;
            case DekiScriptBinary.Op.LogicalAnd:
                state.Append(" && ");
                break;
            case DekiScriptBinary.Op.LogicalOr:
                state.Append(" || ");
                break;
            case DekiScriptBinary.Op.Modulo:
                state.Append(" % ");
                break;
            case DekiScriptBinary.Op.Multiplication:
                state.Append(" * ");
                break;
            case DekiScriptBinary.Op.NotEqual:
                state.Append(" != ");
                break;
            case DekiScriptBinary.Op.NullCoalesce:
                state.Append(" ?? ");
                break;
            case DekiScriptBinary.Op.IdentityEqual:
                state.Append(" === ");
                break;
            case DekiScriptBinary.Op.IdentityNotEqual:
                state.Append(" !== ");
                break;
            case DekiScriptBinary.Op.IsType:
                state.Append(" is ");
                break;
            case DekiScriptBinary.Op.Concat:
                state.Append(" .. ");
                break;
            case DekiScriptBinary.Op.Subtraction:
                state.Append(" - ");
                break;
            case DekiScriptBinary.Op.UriAppend:
                state.Append(" & ");
                break;
            case DekiScriptBinary.Op.InCollection:
                state.Append(" in ");
                break;
            default:
                throw new InvalidOperationException("invalid op code:" + expr.OpCode);
            }
            if(expr.OpCode == DekiScriptBinary.Op.IsType) {
                state.Append(((DekiScriptString)expr.Right).Value);
            } else if(expr.OpCode != DekiScriptBinary.Op.LeftValue) {
                ScopeVisit(expr.Right, state);
            }
            return Empty.Value;
        }

        public Empty Visit(DekiScriptReturnScope expr, StringBuilder state) {

            // TODO (steveb): we need some kind of visual clue that 'return' statements inside of this block don't escape further; maybe once we have lambdas
            expr.Value.VisitWith(this, state);
            state.Append(" !! web.showerror(__error)");
            return Empty.Value;
        }

        public Empty Visit(DekiScriptBool expr, StringBuilder state) {
            state.Append(expr.Value);
            return Empty.Value;
        }

        public Empty Visit(DekiScriptCall expr, StringBuilder state) {
            expr.Prefix.VisitWith(this, state);
            if(expr.IsCurryOperation) {
                state.Append(".");
            }
            if(expr.Arguments is DekiScriptList) {
                WriteList((DekiScriptList)expr.Arguments, '(', ')', false, state);
            } else if(expr.Arguments is DekiScriptListConstructor) {
                WriteListConstructor((DekiScriptListConstructor)expr.Arguments, '(', ')', false, state);
            } else {
                expr.Arguments.VisitWith(this, state);
            }
            return Empty.Value;
        }

        public Empty Visit(DekiScriptDiscard expr, StringBuilder state) {
            state.Append("discard (");
            expr.Value.VisitWith(this, state);
            state.Append(")");
            return Empty.Value;
        }

        public Empty Visit(DekiScriptForeach expr, StringBuilder state) {
            state.Append("foreach(");
            state.Append(expr.Generator.ToString());
            state.Append(") { ");
            if(expr.Body != null) {
                expr.Body.VisitWith(this, state);
            }
            state.Append(" }");
            return Empty.Value;
        }

        public Empty Visit(DekiScriptList expr, StringBuilder state) {
            WriteList(expr, '[', ']', true, state);
            return Empty.Value;
        }

        public Empty Visit(DekiScriptListConstructor expr, StringBuilder state) {
            WriteListConstructor(expr, '[', ']', true, state);
            return Empty.Value;
        }

        public Empty Visit(DekiScriptMagicId expr, StringBuilder state) {
            state.Append('@');
            state.Append(expr.Name);
            return Empty.Value;
        }

        public Empty Visit(DekiScriptMap expr, StringBuilder state) {
            state.Append("{");

            // convert values to an array so they can be sorted
            var values = new List<KeyValuePair<string, DekiScriptLiteral>>();
            foreach(KeyValuePair<string, DekiScriptLiteral> entry in expr.Value) {
                values.Add(entry);
            }
            values.Sort((left, right) => left.Key.CompareInvariantIgnoreCase(right.Key));

            // emit values
            bool first = true;
            foreach(KeyValuePair<string, DekiScriptLiteral> entry in values) {
                if(!first) {
                    state.Append(", ");
                } else {
                    state.Append(" ");
                }
                first = false;
                if(DekiScriptParser.IsIdentifier(entry.Key) || DekiScriptParser.IsNumber(entry.Key)) {
                    state.Append(entry.Key);
                } else {
                    state.Append(entry.Key.QuoteString());
                }
                state.Append(" : ");
                entry.Value.VisitWith(this, state);
            }
            state.Append(" }");
            return Empty.Value;
        }

        public Empty Visit(DekiScriptMapConstructor expr, StringBuilder state) {
            state.Append("{");
            bool first = true;
            foreach(var field in expr.Fields) {
                if(!first) {
                    state.Append(", ");
                } else {
                    state.Append(" ");
                }
                first = false;

                // check if key is a string and a valid identifier
                if(field.Key is DekiScriptString) {
                    string key = ((DekiScriptString)field.Key).Value;
                    if(DekiScriptParser.IsIdentifier(key)) {
                        state.Append(key + " : " + field.Value);
                    } else {
                        state.Append(key.QuoteString() + " : " + field.Value);
                    }
                } else {
                    state.Append("(" + field.Key + ") : " + field.Value);
                }
            }
            if(expr.Generator != null) {
                state.Append(" foreach ");
                state.Append(expr.Generator.ToString());
            }
            state.Append(" }");
            return Empty.Value;
        }

        public Empty Visit(DekiScriptNil expr, StringBuilder state) {
            state.Append("nil");
            return Empty.Value;
        }

        public Empty Visit(DekiScriptNumber expr, StringBuilder state) {
            state.Append(expr.Value);
            return Empty.Value;
        }

        public Empty Visit(DekiScriptReturn expr, StringBuilder state) {
            state.Append("return");
            if(expr.Value != DekiScriptNil.Value) {
                state.Append(" ");
                expr.Value.VisitWith(this, state);
            }
            return Empty.Value;
        }

        public Empty Visit(DekiScriptSequence expr, StringBuilder state) {
            switch(expr.List.Length) {
            case 0:
                state.Append("nil");
                break;
            case 1:
                expr.List[0].VisitWith(this, state);
                break;
            default:
                state.Append('(');
                bool first = true;
                foreach(var expression in expr.List) {
                    if(!first) {
                        state.Append("; ");
                    }
                    first = false;
                    expression.VisitWith(this, state);
                }
                state.Append(')');
                break;
            }
            return Empty.Value;
        }

        public Empty Visit(DekiScriptString expr, StringBuilder state) {
            state.Append(expr.Value.QuoteString());
            return Empty.Value;
        }

        public Empty Visit(DekiScriptSwitch expr, StringBuilder state) {
            state.Append("switch(");
            expr.Value.VisitWith(this, state);
            state.Append(") { ");
            foreach(var c in expr.Cases) {
                WriteCaseBlock(c, state);
            }
            state.Append("}");
            return Empty.Value;
        }

        public Empty Visit(DekiScriptTernary expr, StringBuilder state) {
            if(expr.IsIfElse) {
                state.Append("if(");
                expr.Test.VisitWith(this, state);
                state.Append(") { ");
                expr.Left.VisitWith(this, state);
                state.Append(" }");
                if(!(expr.Right is DekiScriptNil)) {
                    state.Append(" else { ");
                    expr.Right.VisitWith(this, state);
                    state.Append(" }");
                }
            } else {
                expr.Test.VisitWith(this, state);
                state.Append(" ? ");
                expr.Left.VisitWith(this, state);
                state.Append(" : ");
                expr.Right.VisitWith(this, state);
            }
            return Empty.Value;
        }

        public Empty Visit(DekiScriptTryCatchFinally expr, StringBuilder state) {
            if(!expr.Variable.EqualsInvariantIgnoreCase(DekiScriptTryCatchFinally.DEFAULT_VARIALBE) || (expr.Finally != DekiScriptNil.Value)) {
                state.Append("try ");
                expr.Try.VisitWith(this, state);
                state.AppendFormat(" catch({0}) ", expr.Variable);
                expr.Catch.VisitWith(this, state);
                if(expr.Finally != null) {
                    state.Append(" finally ");
                    expr.Finally.VisitWith(this, state);
                }
            } else {
                expr.Try.VisitWith(this, state);
                state.Append(" !! ");
                expr.Catch.VisitWith(this, state);
            }
            return Empty.Value;
        }

        public Empty Visit(DekiScriptUnary expr, StringBuilder state) {
            switch(expr.OpCode) {
            case DekiScriptUnary.Op.Negate:
                state.Append("-");
                ScopeVisit(expr.Value, state);
                break;
            case DekiScriptUnary.Op.LogicalNot:
                state.Append("!");
                ScopeVisit(expr.Value, state);
                break;
            case DekiScriptUnary.Op.TypeOf:
                state.Append("typeof(");
                expr.Value.VisitWith(this, state);
                state.Append(")");
                break;
            case DekiScriptUnary.Op.Length:
                state.Append("#");
                ScopeVisit(expr.Value, state);
                break;
            default:
                throw new InvalidOperationException("invalid op code:" + expr.OpCode);
            }
            return Empty.Value;
        }

        public Empty Visit(DekiScriptUnknown expr, StringBuilder state) {
            state.Append("__UNKNOWN");
            return Empty.Value;
        }

        public Empty Visit(DekiScriptUri expr, StringBuilder state) {
            state.Append(expr.Value.ToString().QuoteString());
            return Empty.Value;
        }

        public Empty Visit(DekiScriptVar expr, StringBuilder state) {
            state.Append(expr.Name);
            return Empty.Value;
        }

        public Empty Visit(DekiScriptXml expr, StringBuilder state) {
            XDoc doc = expr.Value;
            if(!doc.IsEmpty) {
                XmlNode node = doc.AsXmlNode;
                if(node.NodeType == XmlNodeType.Attribute) {
                    state.Append(node.OuterXml);
                } else {
                    DekiScriptParser.Parse(doc, false).VisitWith(this, state);
                }
            }
            return Empty.Value;
        }

        public Empty Visit(DekiScriptXmlElement expr, StringBuilder state) {
            state.Append('<');
            if(!string.IsNullOrEmpty(expr.Prefix)) {
                state.Append(expr.Prefix).Append(':');
            }
            WriteTag(expr.Name, state);

            // check if any attribute need to be rendered
            if(expr.Attributes.Length > 0) {

                // render attributes
                for(int i = 0; i < expr.Attributes.Length; i++) {
                    state.Append(' ');
                    WriteAttribute(expr.Attributes[i], state);
                }
            }

            // check if node has any child nodes
            StringBuilder inner = new StringBuilder();
            expr.Node.VisitWith(this, inner);
            if(inner.Length == 0) {
                state.Append("/>");
            } else {
                state.Append(">");

                // render contents
                state.Append(inner);

                // render closing tag
                state.Append("</");
                if(expr.Name is DekiScriptString) {
                    if(!string.IsNullOrEmpty(expr.Prefix)) {
                        state.Append(expr.Prefix).Append(':');
                    }
                    WriteTag(expr.Name, state);
                }
                state.Append(">");
            }
            return Empty.Value;
        }

        private void WriteList(DekiScriptList expr, char open, char close, bool spacer, StringBuilder state) {
            state.Append(open);
            bool first = true;
            foreach(DekiScriptLiteral entry in expr.Value) {
                if(!first) {
                    state.Append(", ");
                } else if(spacer) {
                    state.Append(" ");
                }
                first = false;
                entry.VisitWith(this, state);
            }
            if(spacer && !first) {
                state.Append(" ");
            }
            state.Append(close);
        }

        private void WriteListConstructor(DekiScriptListConstructor expr, char open, char close, bool spacer, StringBuilder state) {
            state.Append(open);
            bool first = true;
            foreach(DekiScriptExpression item in expr.Items) {
                if(!first) {
                    state.Append(", ");
                } else if(spacer) {
                    state.Append(" ");
                }
                first = false;
                item.VisitWith(this, state);
            }
            if(expr.Generator != null) {
                state.Append(" foreach ");
                state.Append(expr.Generator.ToString());
            }
            if(spacer && !first) {
                state.Append(" ");
            }
            state.Append(close);
        }

        private void WriteCaseBlock(DekiScriptSwitch.CaseBlock expr, StringBuilder state) {
            foreach(DekiScriptExpression condition in expr.Conditions) {
                if(condition != null) {
                    state.Append("case ");
                    condition.VisitWith(this, state);
                    state.Append(": ");
                } else {
                    state.Append("default: ");
                }
            }
            if(expr.Body == null) {
                return;
            }
            expr.Body.VisitWith(this, state);
            state.Append("; ");
        }

        private void WriteTag(DekiScriptExpression tag, StringBuilder state) {
            if(tag is DekiScriptString) {
                state.Append(XmlConvert.EncodeLocalName(((DekiScriptString)tag).Value));
            } else {
                state.Append('(');
                tag.VisitWith(this, state);
                state.Append(')');
            }
        }

        private void WriteAttribute(DekiScriptXmlElement.Attribute expr, StringBuilder state) {
            if(!string.IsNullOrEmpty(expr.Prefix)) {
                state.Append(expr.Prefix).Append(':');
            }
            WriteTag(expr.Name, state);
            state.Append('=');
            if(expr.Value is DekiScriptString) {
                expr.Value.VisitWith(this, state);
            } else {
                state.Append('(');
                expr.Value.VisitWith(this, state);
                state.Append(')');
            }
        }

        private void ScopeVisit(DekiScriptExpression expr, StringBuilder state) {
            if((expr is DekiScriptBinary) || (expr is DekiScriptTernary) || ((expr is DekiScriptSequence) && ((DekiScriptSequence)expr).List.Length > 1)) {
                state.Append("(");
                expr.VisitWith(this, state);
                state.Append(")");
            } else {
                expr.VisitWith(this, state);
            }
        }
    }
}
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
    public class DekiScriptBinary : DekiScriptOperation {

        //--- Constants ---
        public enum Op {
            LeftValue,
            NullCoalesce,
            Concat,
            LogicalOr,
            LogicalAnd,
            NotEqual,
            Equal,
            IdentityNotEqual,
            IdentityEqual,
            IsType,
            LessThan,
            GreaterThan,
            LessOrEqual,
            GreaterOrEqual,
            Addition,
            Subtraction,
            Multiplication,
            Division,
            Modulo,
            UriAppend,
            InCollection
        }

        //--- Class Methods ---
        public static DekiScriptLiteral IdentityEqual(DekiScriptLiteral left, DekiScriptLiteral right) {
            if(left.ScriptType == right.ScriptType) {
                switch(left.ScriptType) {
                case DekiScriptType.NIL:
                    return DekiScriptBool.True;
                case DekiScriptType.BOOL:
                    return Constant(left.AsBool() == right.AsBool());
                case DekiScriptType.NUM:
                    return Constant(left.AsNumber() == right.AsNumber());
                case DekiScriptType.STR:
                    return Constant(StringUtil.EqualsInvariant(left.AsString(), right.AsString()));
                default:
                    return Constant(ReferenceEquals(left, right));
                }
            }
            return DekiScriptBool.False;
        }

        public static DekiScriptLiteral IdentityNotEqual(DekiScriptLiteral left, DekiScriptLiteral right) {
            if(left.ScriptType == right.ScriptType) {
                switch(left.ScriptType) {
                case DekiScriptType.NIL:
                    return DekiScriptBool.False;
                case DekiScriptType.BOOL:
                    return Constant(left.AsBool() != right.AsBool());
                case DekiScriptType.NUM:
                    return Constant(left.AsNumber() != right.AsNumber());
                case DekiScriptType.STR:
                    return Constant(!StringUtil.EqualsInvariant(left.AsString(), right.AsString()));
                default:
                    return Constant(!ReferenceEquals(left, right));
                }
            }
            return DekiScriptBool.True;
        }

        public static DekiScriptLiteral Equal(DekiScriptLiteral left, DekiScriptLiteral right) {
            if(DekiScriptLiteral.CoerceValuesToSameType(ref left, ref right)) {
                switch(left.ScriptType) {
                case DekiScriptType.NIL:
                    return DekiScriptBool.True;
                case DekiScriptType.BOOL:
                    return Constant(left.AsBool() == right.AsBool());
                case DekiScriptType.NUM:
                    return Constant(left.AsNumber() == right.AsNumber());
                case DekiScriptType.STR:
                    return Constant(StringUtil.EqualsInvariant(left.AsString(), right.AsString()));
                default:
                    return Constant(object.ReferenceEquals(left, right));
                }
            }
            return DekiScriptBool.False;
        }

        public static DekiScriptLiteral NotEqual(DekiScriptLiteral left, DekiScriptLiteral right) {
            if(DekiScriptLiteral.CoerceValuesToSameType(ref left, ref right)) {
                switch(left.ScriptType) {
                case DekiScriptType.NIL:
                    return DekiScriptBool.False;
                case DekiScriptType.BOOL:
                    return Constant(left.AsBool() != right.AsBool());
                case DekiScriptType.NUM:
                    return Constant(left.AsNumber() != right.AsNumber());
                case DekiScriptType.STR:
                    return Constant(!StringUtil.EqualsInvariant(left.AsString(), right.AsString()));
                default:
                    return Constant(!object.ReferenceEquals(left, right));
                }
            }
            return DekiScriptBool.True;
        }

        //--- Fields ---
        public readonly Op OpCode;
        public readonly DekiScriptExpression Left;
        public readonly DekiScriptExpression Right;

        //--- Constructors ---
        internal DekiScriptBinary(Op opcode, DekiScriptExpression left, DekiScriptExpression right) {
            if(left == null) {
                throw new ArgumentNullException("left");
            }
            if(right == null) {
                throw new ArgumentNullException("right");
            }
            if((opcode == Op.IsType) && !(right is DekiScriptString)) {
                throw new ArgumentException("the 'is' operator requires a string as right hand side value", "right");
            }
            this.OpCode = opcode;
            this.Left = left;
            this.Right = right;

        }

        //--- Methods ---
        public override TReturn VisitWith<TState, TReturn>(IDekiScriptExpressionVisitor<TState, TReturn> visitor, TState state) {
            return visitor.Visit(this, state);
        }
    }
}
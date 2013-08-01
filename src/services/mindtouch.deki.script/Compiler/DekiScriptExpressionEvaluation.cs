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
using System.Collections.Generic;
using System.Xml;
using MindTouch.Deki.Script.Expr;
using MindTouch.Deki.Script.Runtime;
using MindTouch.Deki.Script.Runtime.Library;
using MindTouch.Dream;
using MindTouch.Xml;

namespace MindTouch.Deki.Script.Compiler {
    internal class DekiScriptExpressionEvaluation : IDekiScriptExpressionVisitor<DekiScriptExpressionEvaluationState, DekiScriptOutputBuffer.Range> {

        //--- Constants ---
        private const string XMLNS = "xmlns";

        //--- Class Fields ---
        public static readonly DekiScriptExpressionEvaluation Instance = new DekiScriptExpressionEvaluation();

        //--- Constructors ---
        private DekiScriptExpressionEvaluation() { }

        //--- Methods ---
        public DekiScriptOutputBuffer.Range Visit(DekiScriptAbort expr, DekiScriptExpressionEvaluationState state) {
            switch(expr.FlowControl) {
            case DekiScriptAbort.Kind.Break:
                throw new DekiScriptBreakException(expr.Location);
            case DekiScriptAbort.Kind.Continue:
                throw new DekiScriptContinueException(expr.Location);
            }
            throw new ShouldNeverHappenException();
        }

        public DekiScriptOutputBuffer.Range Visit(DekiScriptAccess expr, DekiScriptExpressionEvaluationState state) {
            return state.Push(Evaluate(expr, state, true));
        }

        public DekiScriptOutputBuffer.Range Visit(DekiScriptAssign expr, DekiScriptExpressionEvaluationState state) {
            if(expr.Define) {
                state.Env.Vars.Add(expr.Variable, state.Pop(expr.Value.VisitWith(this, state)));
            } else {
                state.Env.Vars[expr.Variable] = state.Pop(expr.Value.VisitWith(this, state));
            }
            return DekiScriptOutputBuffer.Range.Empty;
        }

        public DekiScriptOutputBuffer.Range Visit(DekiScriptBinary expr, DekiScriptExpressionEvaluationState state) {
            DekiScriptExpression Left = expr.Left;
            DekiScriptExpression Right = expr.Right;
            switch(expr.OpCode) {
            case DekiScriptBinary.Op.LeftValue:
                return Left.VisitWith(this, state);
            case DekiScriptBinary.Op.IdentityEqual: {
                    DekiScriptLiteral left = state.Pop(Left.VisitWith(this, state));
                    DekiScriptLiteral right = state.Pop(Right.VisitWith(this, state));
                    return state.Push(DekiScriptBinary.IdentityEqual(left, right));
                }
            case DekiScriptBinary.Op.IdentityNotEqual: {
                    DekiScriptLiteral left = state.Pop(Left.VisitWith(this, state));
                    DekiScriptLiteral right = state.Pop(Right.VisitWith(this, state));
                    return state.Push(DekiScriptBinary.IdentityNotEqual(left, right));
                }
            case DekiScriptBinary.Op.IsType: {
                    DekiScriptLiteral left = state.Pop(Left.VisitWith(this, state));
                    string type = ((DekiScriptString)Right).Value;
                    return state.Push(DekiScriptExpression.Constant(type.EqualsInvariantIgnoreCase("any") || left.ScriptTypeName.EqualsInvariantIgnoreCase(type)));
                }
            case DekiScriptBinary.Op.Equal: {
                    DekiScriptLiteral left = state.Pop(Left.VisitWith(this, state));
                    DekiScriptLiteral right = state.Pop(Right.VisitWith(this, state));
                    return state.Push(DekiScriptBinary.Equal(left, right));
                }
            case DekiScriptBinary.Op.NotEqual: {
                    DekiScriptLiteral left = state.Pop(Left.VisitWith(this, state));
                    DekiScriptLiteral right = state.Pop(Right.VisitWith(this, state));
                    return state.Push(DekiScriptBinary.NotEqual(left, right));
                }
            case DekiScriptBinary.Op.GreaterOrEqual: {
                    DekiScriptLiteral left = state.Pop(Left.VisitWith(this, state));
                    DekiScriptLiteral right = state.Pop(Right.VisitWith(this, state));
                    if(DekiScriptLiteral.CoerceValuesToSameType(ref left, ref right)) {
                        switch(left.ScriptType) {
                        case DekiScriptType.BOOL:
                        case DekiScriptType.NUM:
                            return state.Push(DekiScriptExpression.Constant(left.AsNumber() >= right.AsNumber()));
                        case DekiScriptType.STR:
                            return state.Push(DekiScriptExpression.Constant(left.AsString().CompareInvariant(right.AsString()) >= 0));
                        default:
                            return DekiScriptOutputBuffer.Range.Empty;
                        }
                    } else {
                        return DekiScriptOutputBuffer.Range.Empty;
                    }
                }
            case DekiScriptBinary.Op.GreaterThan: {
                    DekiScriptLiteral left = state.Pop(Left.VisitWith(this, state));
                    DekiScriptLiteral right = state.Pop(Right.VisitWith(this, state));
                    if(DekiScriptLiteral.CoerceValuesToSameType(ref left, ref right)) {
                        switch(left.ScriptType) {
                        case DekiScriptType.BOOL:
                        case DekiScriptType.NUM:
                            return state.Push(DekiScriptExpression.Constant(left.AsNumber() > right.AsNumber()));
                        case DekiScriptType.STR:
                            return state.Push(DekiScriptExpression.Constant(left.AsString().CompareInvariant(right.AsString()) > 0));
                        default:
                            return DekiScriptOutputBuffer.Range.Empty;
                        }
                    } else {
                        return DekiScriptOutputBuffer.Range.Empty;
                    }
                }
            case DekiScriptBinary.Op.LessOrEqual: {
                    DekiScriptLiteral left = state.Pop(Left.VisitWith(this, state));
                    DekiScriptLiteral right = state.Pop(Right.VisitWith(this, state));
                    if(DekiScriptLiteral.CoerceValuesToSameType(ref left, ref right)) {
                        switch(left.ScriptType) {
                        case DekiScriptType.BOOL:
                        case DekiScriptType.NUM:
                            return state.Push(DekiScriptExpression.Constant(left.AsNumber() <= right.AsNumber()));
                        case DekiScriptType.STR:
                            return state.Push(DekiScriptExpression.Constant(left.AsString().CompareInvariant(right.AsString()) <= 0));
                        default:
                            return DekiScriptOutputBuffer.Range.Empty;
                        }
                    } else {
                        return DekiScriptOutputBuffer.Range.Empty;
                    }
                }
            case DekiScriptBinary.Op.LessThan: {
                    DekiScriptLiteral left = state.Pop(Left.VisitWith(this, state));
                    DekiScriptLiteral right = state.Pop(Right.VisitWith(this, state));
                    if(DekiScriptLiteral.CoerceValuesToSameType(ref left, ref right)) {
                        switch(left.ScriptType) {
                        case DekiScriptType.BOOL:
                        case DekiScriptType.NUM:
                            return state.Push(DekiScriptExpression.Constant(left.AsNumber() < right.AsNumber()));
                        case DekiScriptType.STR:
                            return state.Push(DekiScriptExpression.Constant(left.AsString().CompareInvariant(right.AsString()) < 0));
                        default:
                            return DekiScriptOutputBuffer.Range.Empty;
                        }
                    } else {
                        return DekiScriptOutputBuffer.Range.Empty;
                    }
                }
            case DekiScriptBinary.Op.LogicalAnd: {
                    DekiScriptLiteral result = state.Pop(Left.VisitWith(this, state));
                    if(!result.IsNilFalseZero) {
                        return Right.VisitWith(this, state);
                    }
                    return state.Push(result);
                }
            case DekiScriptBinary.Op.LogicalOr: {
                    DekiScriptLiteral result = state.Pop(Left.VisitWith(this, state));
                    if(result.IsNilFalseZero) {
                        return Right.VisitWith(this, state);
                    }
                    return state.Push(result);
                }
            case DekiScriptBinary.Op.Addition:
                return state.Push(DekiScriptExpression.Constant(state.Pop(Left.VisitWith(this, state)).AsNumber() + state.Pop(Right.VisitWith(this, state)).AsNumber()));
            case DekiScriptBinary.Op.Division:
                return state.Push(DekiScriptExpression.Constant(state.Pop(Left.VisitWith(this, state)).AsNumber() / state.Pop(Right.VisitWith(this, state)).AsNumber()));
            case DekiScriptBinary.Op.Modulo: {
                    DekiScriptLiteral left = state.Pop(Left.VisitWith(this, state));
                    DekiScriptLiteral right = state.Pop(Right.VisitWith(this, state));
                    if((left is DekiScriptString) && ((right is DekiScriptMap) || (right is DekiScriptList))) {

                        // NOTE (steveb): string.format shorthand notation: "abc = $abc" % { abc: 123 } -OR- "0 = $0" % [ 123 ]
                        return state.Push(DekiScriptLiteral.FromNativeValue(DekiScriptLibrary.StringFormat(((DekiScriptString)left).Value, right.NativeValue)));
                    } else {
                        return state.Push(DekiScriptExpression.Constant(left.AsNumber() % right.AsNumber()));
                    }
                }
            case DekiScriptBinary.Op.Multiplication:
                return state.Push(DekiScriptExpression.Constant(state.Pop(Left.VisitWith(this, state)).AsNumber() * state.Pop(Right.VisitWith(this, state)).AsNumber()));
            case DekiScriptBinary.Op.Subtraction:
                return state.Push(DekiScriptExpression.Constant(state.Pop(Left.VisitWith(this, state)).AsNumber() - state.Pop(Right.VisitWith(this, state)).AsNumber()));
            case DekiScriptBinary.Op.NullCoalesce: {
                    DekiScriptLiteral result = state.Pop(Left.VisitWith(this, state));
                    if(result.IsNil) {
                        return Right.VisitWith(this, state);
                    }
                    return state.Push(result);
                }
            case DekiScriptBinary.Op.Concat: {
                    DekiScriptLiteral left = state.Pop(Left.VisitWith(this, state));
                    DekiScriptLiteral right = state.Pop(Right.VisitWith(this, state));
                    if(left is DekiScriptNil) {
                        return state.Push(right);
                    } else if(right is DekiScriptNil) {
                        return state.Push(left);
                    } else if((left is DekiScriptMap) && (right is DekiScriptMap)) {

                        // left and right expressions are maps, merge them
                        DekiScriptMap result = new DekiScriptMap();
                        result.AddRange((DekiScriptMap)left);
                        result.AddRange((DekiScriptMap)right);
                        return state.Push(result);
                    } else if((left is DekiScriptList) && (right is DekiScriptList)) {

                        // left and right expressions are lists, concatenate them
                        DekiScriptList result = new DekiScriptList();
                        result.AddRange((DekiScriptList)left);
                        result.AddRange((DekiScriptList)right);
                        return state.Push(result);
                    } else {

                        // treat left and right expressions as strings
                        string leftText = left.AsString();
                        string rightText = right.AsString();
                        if((leftText != null) && (rightText != null)) {
                            return state.Push(DekiScriptExpression.Constant(leftText + rightText));
                        } else if(leftText != null) {
                            return state.Push(DekiScriptExpression.Constant(leftText));
                        } else if(rightText != null) {
                            return state.Push(DekiScriptExpression.Constant(rightText));
                        } else {
                            return DekiScriptOutputBuffer.Range.Empty;
                        }
                    }
                }
            case DekiScriptBinary.Op.UriAppend: {

                    // TODO (steveb): we should throw an exception when the LHS is not a valid string or uri

                    XUri left = XUri.TryParse(state.Pop(Left.VisitWith(this, state)).AsString());
                    string result = null;
                    if(left != null) {
                        DekiScriptLiteral right = state.Pop(Right.VisitWith(this, state));
                        if(right is DekiScriptString) {
                            result = DekiScriptLibrary.UriBuild(left, right.AsString(), null);
                        } else if(right is DekiScriptMap) {
                            result = DekiScriptLibrary.UriBuild(left, null, (Hashtable)right.NativeValue);
                        } else {
                            result = left.ToString();
                        }
                    }
                    return state.Push(DekiScriptLiteral.FromNativeValue(result));
                }
            case DekiScriptBinary.Op.InCollection: {
                    DekiScriptLiteral left = state.Pop(Left.VisitWith(this, state));
                    DekiScriptLiteral right = state.Pop(Right.VisitWith(this, state));
                    if(right is DekiScriptList) {
                        foreach(DekiScriptLiteral item in ((DekiScriptList)right).Value) {
                            if(!DekiScriptBinary.Equal(left, item).IsNilFalseZero) {
                                return state.Push(DekiScriptBool.True);
                            }
                        }
                        return state.Push(DekiScriptBool.False);
                    } else if(right is DekiScriptMap) {
                        foreach(DekiScriptLiteral item in ((DekiScriptMap)right).Value.Values) {
                            if(!DekiScriptBinary.Equal(left, item).IsNilFalseZero) {
                                return state.Push(DekiScriptBool.True);
                            }
                        }
                        return state.Push(DekiScriptBool.False);
                    } else {
                        return state.Push(DekiScriptBool.False);
                    }
                }
            }
            throw new InvalidOperationException("invalid op code:" + expr.OpCode);
        }

        public DekiScriptOutputBuffer.Range Visit(DekiScriptReturnScope expr, DekiScriptExpressionEvaluationState state) {
            int marker = state.Buffer.Marker;
            try {
                if(state.FatalEvaluationError == null) {
                    state.ThrowIfTimedout();
                    return expr.Value.VisitWith(this, state);
                }
                state.Push(state.FatalEvaluationError);
            } catch(DekiScriptReturnException e) {
                state.Push(e.Value);
            } catch(DekiScriptControlFlowException) {

                // nothing to do
            } catch(Exception e) {
                var error = DekiScriptLibrary.MakeErrorObject(e, state.Env);
                state.Runtime.LogExceptionInOutput(error);
                state.Push(new DekiScriptXml(DekiScriptLibrary.WebShowError((Hashtable)error.NativeValue)));
            }
            return state.Buffer.Since(marker);
        }

        public DekiScriptOutputBuffer.Range Visit(DekiScriptBool expr, DekiScriptExpressionEvaluationState state) {
            return state.Push(expr);
        }

        public DekiScriptOutputBuffer.Range Visit(DekiScriptCall expr, DekiScriptExpressionEvaluationState state) {
            state.ThrowIfTimedout();

            // evaluate prefix
            DekiScriptLiteral prefix = state.Pop(expr.Prefix.VisitWith(this, state));
            if(prefix.ScriptType != DekiScriptType.URI) {
                if(prefix.ScriptType == DekiScriptType.NIL) {
                    throw new DekiScriptUndefinedNameException(expr.Location, expr.Prefix.ToString());
                } else {
                    throw new DekiScriptBadTypeException(expr.Location, prefix.ScriptType, new[] { DekiScriptType.URI });
                }
            }

            // evaluate arguments
            DekiScriptLiteral arguments = state.Pop(expr.Arguments.VisitWith(this, state));
            if((arguments.ScriptType != DekiScriptType.MAP) && (arguments.ScriptType != DekiScriptType.LIST)) {
                throw new DekiScriptBadTypeException(expr.Location, arguments.ScriptType, new[] { DekiScriptType.MAP, DekiScriptType.LIST });
            }

            // check if the URI was curried
            DekiScriptUri uri = (DekiScriptUri)prefix;
            if(!uri.Arguments.IsNil) {
                switch(uri.Arguments.ScriptType) {
                case DekiScriptType.LIST:

                    // append argument to list
                    DekiScriptList list = new DekiScriptList((DekiScriptList)uri.Arguments);
                    list.Add(arguments);
                    arguments = list;
                    break;
                case DekiScriptType.MAP:
                    if(arguments.ScriptType == DekiScriptType.MAP) {

                        // concatenate both maps
                        DekiScriptMap map = new DekiScriptMap();
                        map.AddRange((DekiScriptMap)uri.Arguments);
                        map.AddRange((DekiScriptMap)arguments);
                        arguments = map;
                    } else if((arguments.ScriptType != DekiScriptType.LIST) || ((DekiScriptList)arguments).Value.Count > 0) {

                        // we can't append a list to a map
                        throw new DekiScriptBadTypeException(expr.Location, arguments.ScriptType, new[] { DekiScriptType.MAP });
                    }
                    break;
                default:
                    throw new DekiScriptBadTypeException(expr.Location, arguments.ScriptType, new[] { DekiScriptType.MAP, DekiScriptType.LIST });
                }
            }

            // check if this is an invocation or curry operation
            if(expr.IsCurryOperation) {
                return state.Push(new DekiScriptUri(uri.Value, arguments));
            }

            // invoke function
            try {
                return state.Push(state.Runtime.Invoke(expr.Location, uri.Value, arguments, state.Env));
            } catch(DekiScriptFatalException) {
                throw;
            } catch(Exception e) {
                var descriptor = state.Runtime.ResolveRegisteredFunctionUri(uri.Value);
                throw new DekiScriptInvokeException(expr.Location, uri.Value, (descriptor != null) ? descriptor.Name : uri.Value.ToString(), e);
            }
        }

        public DekiScriptOutputBuffer.Range Visit(DekiScriptDiscard expr, DekiScriptExpressionEvaluationState state) {
            int marker = state.Buffer.Marker;
            try {
                expr.Value.VisitWith(this, state);
            } finally {
                state.Buffer.Reset(marker);
            }
            return DekiScriptOutputBuffer.Range.Empty;
        }

        public DekiScriptOutputBuffer.Range Visit(DekiScriptForeach expr, DekiScriptExpressionEvaluationState state) {
            state.ThrowIfTimedout();
            int marker = state.Buffer.Marker;
            try {
                DekiScriptGeneratorEvaluation.Generate(expr.Generator, delegate(DekiScriptEnv subEnv) {

                    // iterate over block statements
                    try {
                        expr.Body.VisitWith(this, state.With(subEnv));
                    } catch(DekiScriptContinueException) {

                        // ignore continue exceptions
                    }
                }, state);
            } catch(DekiScriptBreakException) {

                // ignore break exceptions
            }
            return state.Buffer.Since(marker);
        }

        public DekiScriptOutputBuffer.Range Visit(DekiScriptList expr, DekiScriptExpressionEvaluationState state) {
            return state.Push(expr);
        }

        public DekiScriptOutputBuffer.Range Visit(DekiScriptListConstructor expr, DekiScriptExpressionEvaluationState state) {
            DekiScriptList result = new DekiScriptList();
            if(expr.Generator == null) {
                foreach(DekiScriptExpression item in expr.Items) {
                    result.Add(state.Pop(item.VisitWith(this, state)));
                }
            } else {
                DekiScriptGeneratorEvaluation.Generate(expr.Generator, delegate(DekiScriptEnv subEnv) {
                    foreach(DekiScriptExpression item in expr.Items) {
                        var eval = state.Pop(item.VisitWith(this, state.With(subEnv)));
                        result.Add(eval);
                    }
                }, state);
            }
            return state.Push(result);
        }

        public DekiScriptOutputBuffer.Range Visit(DekiScriptMagicId expr, DekiScriptExpressionEvaluationState state) {
            return state.Push(state.Env.GetMagicId(expr.Name));
        }

        public DekiScriptOutputBuffer.Range Visit(DekiScriptMap expr, DekiScriptExpressionEvaluationState state) {
            return state.Push(expr);
        }

        public DekiScriptOutputBuffer.Range Visit(DekiScriptMapConstructor expr, DekiScriptExpressionEvaluationState state) {
            DekiScriptMap result = new DekiScriptMap();
            if(expr.Generator == null) {
                foreach(DekiScriptMapConstructor.FieldConstructor field in expr.Fields) {
                    DekiScriptLiteral key = state.Pop(field.Key.VisitWith(this, state));

                    // check that key is a simple type
                    string text = key.AsString();
                    if(text != null) {
                        DekiScriptLiteral value = state.Pop(field.Value.VisitWith(this, state));
                        result.Add(text, value);
                    }
                }
            } else {
                DekiScriptGeneratorEvaluation.Generate(expr.Generator, delegate(DekiScriptEnv subEnv) {
                    foreach(DekiScriptMapConstructor.FieldConstructor field in expr.Fields) {
                        DekiScriptLiteral key = state.Pop(field.Key.VisitWith(this, state.With(subEnv)));

                        // check that key is a simple type
                        string text = key.AsString();
                        if(text != null) {
                            DekiScriptLiteral value = state.Pop(field.Value.VisitWith(this, state.With(subEnv)));
                            result.Add(text, value);
                        }
                    }
                }, state);
            }
            return state.Push(result);
        }

        public DekiScriptOutputBuffer.Range Visit(DekiScriptNil expr, DekiScriptExpressionEvaluationState state) {
            return state.Push(expr);
        }

        public DekiScriptOutputBuffer.Range Visit(DekiScriptNumber expr, DekiScriptExpressionEvaluationState state) {
            return state.Push(expr);
        }

        public DekiScriptOutputBuffer.Range Visit(DekiScriptReturn expr, DekiScriptExpressionEvaluationState state) {
            var value = state.Pop(expr.Value.VisitWith(this, state));
            throw new DekiScriptReturnException(expr.Location, value);
        }

        public DekiScriptOutputBuffer.Range Visit(DekiScriptSequence expr, DekiScriptExpressionEvaluationState state) {
            int marker = state.Buffer.Marker;
            foreach(DekiScriptExpression expression in expr.List) {
                expression.VisitWith(this, state);
            }
            return state.Buffer.Since(marker);
        }

        public DekiScriptOutputBuffer.Range Visit(DekiScriptString expr, DekiScriptExpressionEvaluationState state) {
            return state.Push(expr);
        }

        public DekiScriptOutputBuffer.Range Visit(DekiScriptSwitch expr, DekiScriptExpressionEvaluationState state) {
            DekiScriptLiteral value = state.Pop(expr.Value.VisitWith(this, state));
            DekiScriptSwitch.CaseBlock caseBlock = null;

            // have to use for instead of foreach, since a fallthrough default case needs to be able to look ahead
            for(int i = 0; i < expr.Cases.Length; i++) {
                DekiScriptSwitch.CaseBlock current = expr.Cases[i];

                // check for default case
                foreach(DekiScriptExpression condition in current.Conditions) {
                    if(condition == null) {

                        // check if this is the first default we've found
                        if(caseBlock == null) {
                            caseBlock = current;
                        }

                        // continue in case loop, since default only gets executed if there is no match
                        continue;
                    }

                    // evaluate test
                    DekiScriptExpression test = DekiScriptExpression.BinaryOp(current.Location, DekiScriptBinary.Op.Equal, value, condition);
                    DekiScriptLiteral caseMatch = state.Pop(test.VisitWith(this, state));

                    // evaluate body on success
                    if(!caseMatch.IsNilFalseZero) {

                        // found a matching cast statement
                        caseBlock = current;
                        break;
                    }
                }
            }

            // haven't found a match yet, so if we have a default, return it
            if(caseBlock != null) {
                int marker = state.Buffer.Marker;
                try {
                    return caseBlock.Body.VisitWith(this, state);
                } catch(DekiScriptBreakException) {

                    // nothing to do
                }
                return state.Buffer.Since(marker);
            }
            return DekiScriptOutputBuffer.Range.Empty;
        }

        public DekiScriptOutputBuffer.Range Visit(DekiScriptTernary expr, DekiScriptExpressionEvaluationState state) {
            DekiScriptLiteral test = state.Pop(expr.Test.VisitWith(this, state));
            DekiScriptLiteral result;

            // check which branch should be executed
            if(!test.IsNilFalseZero) {
                return expr.Left.VisitWith(this, state);
            } else {
                return expr.Right.VisitWith(this, state);
            }
        }

        public DekiScriptOutputBuffer.Range Visit(DekiScriptTryCatchFinally expr, DekiScriptExpressionEvaluationState state) {
            int marker = state.Buffer.Marker;
            try {
                expr.Try.VisitWith(this, state);
            } catch(DekiScriptFatalException) {
                throw;
            } catch(DekiScriptControlFlowException) {
                throw;
            } catch(Exception e) {
                state.Buffer.Reset(marker);

                // translate exception to an error object
                DekiScriptMap error = DekiScriptLibrary.MakeErrorObject(e, state.Env);

                // capture error object in a nested environment
                try {
                    state.Env.Vars.Add(expr.Variable, error);
                    expr.Catch.VisitWith(this, state);
                } finally {
                    state.Env.Vars.Add(expr.Variable, DekiScriptNil.Value);
                }
            } finally {
                expr.Finally.VisitWith(this, state);
            }
            return state.Buffer.Since(marker);
        }

        public DekiScriptOutputBuffer.Range Visit(DekiScriptUnary expr, DekiScriptExpressionEvaluationState state) {
            switch(expr.OpCode) {
            case DekiScriptUnary.Op.Negate:
                return state.Push(DekiScriptExpression.Constant(-state.Pop(expr.Value.VisitWith(this, state)).AsNumber()));
            case DekiScriptUnary.Op.LogicalNot:
                return state.Push(DekiScriptExpression.Constant(state.Pop(expr.Value.VisitWith(this, state)).IsNilFalseZero));
            case DekiScriptUnary.Op.TypeOf:
                return state.Push(DekiScriptExpression.Constant(state.Pop(expr.Value.VisitWith(this, state)).ScriptTypeName));
            case DekiScriptUnary.Op.Length: {
                    DekiScriptLiteral value = state.Pop(expr.Value.VisitWith(this, state));
                    switch(value.ScriptType) {
                    case DekiScriptType.NIL:
                        return state.Push(DekiScriptExpression.Constant(0));
                    case DekiScriptType.LIST:
                        return state.Push(DekiScriptExpression.Constant(((DekiScriptList)value).Value.Count));
                    case DekiScriptType.STR:
                        return state.Push(DekiScriptExpression.Constant(((DekiScriptString)value).Value.Length));
                    case DekiScriptType.MAP:
                        return state.Push(DekiScriptExpression.Constant(((DekiScriptMap)value).Value.Count));
                    case DekiScriptType.XML:
                        return state.Push(DekiScriptExpression.Constant(((DekiScriptXml)value).Value.ListLength));
                    default:
                        return DekiScriptOutputBuffer.Range.Empty;
                    }
                }
            }
            throw new InvalidOperationException("invalid op code:" + expr.OpCode);
        }

        public DekiScriptOutputBuffer.Range Visit(DekiScriptUnknown expr, DekiScriptExpressionEvaluationState state) {
            return state.Push(expr);
        }

        public DekiScriptOutputBuffer.Range Visit(DekiScriptUri expr, DekiScriptExpressionEvaluationState state) {
            return state.Push(expr);
        }

        public DekiScriptOutputBuffer.Range Visit(DekiScriptVar expr, DekiScriptExpressionEvaluationState state) {
            if(expr.Name.EqualsInvariant(DekiScriptRuntime.ENV_ID)) {
                DekiScriptMap vars = new DekiScriptMap();
                vars.AddRange(state.Env.Vars);
                return state.Push(vars);
            }

            // check if variable exists
            DekiScriptLiteral result;
            if(!state.Env.Vars.TryGetValue(expr.Name, out result)) {
                result = state.Runtime.ResolveMissingName(expr.Name);
            }

            if(result == null) {
                throw new DekiScriptUndefinedNameException(expr.Location, expr.Name);
            }
            result = state.Runtime.EvaluateProperty(expr.Location, result, state.Env);
            return state.Push(result);
        }

        public DekiScriptOutputBuffer.Range Visit(DekiScriptXml expr, DekiScriptExpressionEvaluationState state) {
            return state.Push(expr);
        }

        public DekiScriptOutputBuffer.Range Visit(DekiScriptXmlElement expr, DekiScriptExpressionEvaluationState state) {

            // check if any namespaces are being defined
            state.Namespaces.PushScope();
            int marker = state.Buffer.Marker;
            try {
                Dictionary<string, string> namespaces = null;
                List<Tuplet<string, string, string>> attributes = null;
                string ctor = null;
                string id = null;
                string type = null;

                // TODO (steveb): validate that all prefixes are defined!

                // evaluate element name
                string name = XmlConvert.EncodeLocalName(state.Pop(expr.Name.VisitWith(this, state)).AsString());

                // loop over all attributes
                foreach(var attribute in expr.Attributes) {
                    string attrPrefix = attribute.Prefix;
                    string attrName = state.Pop(attribute.Name.VisitWith(this, state)).AsString();
                    if(string.IsNullOrEmpty(attrName)) {
                        continue;
                    }
                    string attrValue = state.Pop(attribute.Value.VisitWith(this, state)).AsString();
                    if(attrValue == null) {
                        continue;
                    }
                    bool isNamespaceDeclaration = string.IsNullOrEmpty(attrPrefix) ? attrName.EqualsInvariant(XMLNS) : attrPrefix.EqualsInvariant(XMLNS);

                    // check if attribute is a namespace declaration
                    if(isNamespaceDeclaration) {

                        // add attribute to namespace declarations
                        namespaces = namespaces ?? new Dictionary<string, string>();

                        // check if the default namespace is being defined
                        if(string.IsNullOrEmpty(attrPrefix)) {
                            namespaces.Add(string.Empty, attrValue);
                        } else {
                            namespaces.Add(attrName, attrValue);
                        }
                    } else {

                        // add attribute to list of attributes
                        attributes = attributes ?? new List<Tuplet<string, string, string>>();
                        attributes.Add(new Tuplet<string, string, string>(attrPrefix, attrName, attrValue));
                        if(string.IsNullOrEmpty(attrPrefix)) {
                            switch(attrName) {
                            case "ctor":
                                ctor = attrValue;
                                break;
                            case "id":
                                id = attrValue;
                                break;
                            case "type":
                                type = attrValue;
                                break;
                            }
                        }
                    }
                }

                // check if current node needs to be replaced entirely
                string jem = null;
                if(string.IsNullOrEmpty(expr.Prefix) && !string.IsNullOrEmpty(type) && name.EqualsInvariant("script") && type.EqualsInvariant("text/jem")) {

                    // NOTE: process <script type="text/jem">

                    // evaluate nested expressions
                    DekiScriptLiteral contents = state.Pop(expr.Node.VisitWith(this, state));
                    if(contents is DekiScriptString) {
                        jem = ((DekiScriptString)contents).Value;
                    } else {
                        jem = contents.ToString();
                    }
                    jem = DekiJemProcessor.Parse(jem, null, state.Env, state.Runtime);
                } else {

                    // check if @ctor is defined without an @id
                    if(!string.IsNullOrEmpty(ctor) && (id == null)) {
                        id = StringUtil.CreateAlphaNumericKey(8);
                        attributes.Add(new Tuplet<string, string, string>(null, "id", id));
                    }

                    // append start xml element to buffer
                    state.Buffer.PushXmlStart(expr.Prefix, name, namespaces, attributes);

                    // evaluate nested expressions
                    expr.Node.VisitWith(this, state);

                    // append end xml element to buffer
                    state.Buffer.PushXmlEnd();

                    // check if the element has a JEM @ctor attribute
                    if(!string.IsNullOrEmpty(ctor)) {

                        // generate JEM code
                        jem = DekiJemProcessor.Parse(ctor, id, state.Env, state.Runtime);
                    }
                }

                // check if JEM code was generated
                if(jem != null) {

                    // create <script> element
                    var scriptAttributes = new List<Tuplet<string, string, string>> { new Tuplet<string, string, string>(null, "type", "text/javascript") };
                    state.Buffer.PushXmlStart(null, "script", null, scriptAttributes);
                    state.Push(DekiScriptExpression.Constant(jem));
                    state.Buffer.PushXmlEnd();
                }
            } catch {
                state.Buffer.Reset(marker);
                throw;
            } finally {

                // restore previous xml namespace definitions
                state.Namespaces.PopScope();
            }
            return state.Buffer.Since(marker);
        }

        internal DekiScriptLiteral Evaluate(DekiScriptAccess expr, DekiScriptExpressionEvaluationState state, bool evaluateProperties) {
            DekiScriptLiteral prefix = state.Pop(expr.Prefix.VisitWith(this, state));
            DekiScriptLiteral index = state.Pop(expr.Index.VisitWith(this, state));
            switch(prefix.ScriptType) {
            case DekiScriptType.MAP: {
                    DekiScriptLiteral result = ((DekiScriptMap)prefix)[index];
                    if(evaluateProperties) {
                        result = state.Runtime.EvaluateProperty(expr.Location, result, state.Env);
                    }
                    return result;
                }
            case DekiScriptType.LIST: {
                    DekiScriptLiteral value = DekiScriptExpression.Constant(index.AsNumber());
                    DekiScriptLiteral result = ((DekiScriptList)prefix)[value];
                    if(evaluateProperties) {
                        result = state.Runtime.EvaluateProperty(expr.Location, result, state.Env);
                    }
                    return result;
                }
            case DekiScriptType.URI: {
                    DekiScriptUri uri = (DekiScriptUri)prefix;

                    // coerce the index type to STR
                    index = DekiScriptExpression.Constant(index.AsString());
                    if(index.ScriptType != DekiScriptType.STR) {
                        throw new DekiScriptBadTypeException(expr.Location, index.ScriptType, new[] { DekiScriptType.STR });
                    }

                    // curry the argument
                    DekiScriptList args;
                    if(!uri.Arguments.IsNil) {

                        // the uri already has curried parameters, make sure they are in LIST format; otherwise fail
                        if(uri.Arguments.ScriptType != DekiScriptType.LIST) {
                            throw new DekiScriptBadTypeException(expr.Location, uri.Arguments.ScriptType, new[] { DekiScriptType.NIL, DekiScriptType.LIST });
                        }
                        args = new DekiScriptList((DekiScriptList)uri.Arguments);
                    } else {
                        args = new DekiScriptList();
                    }
                    args.Add(index);
                    return new DekiScriptUri(uri.Value, args);
                }
            case DekiScriptType.STR: {
                    DekiScriptString text = (DekiScriptString)prefix;

                    // coerce the index type to NUM
                    double? value = index.AsNumber();
                    if(value == null) {
                        throw new DekiScriptBadTypeException(expr.Location, index.ScriptType, new[] { DekiScriptType.NUM });
                    }

                    // retrieve character at given index position
                    int position = (int)value;
                    if((position < 0) || (position >= text.Value.Length)) {

                        // index is out of bounds, return nil
                        return DekiScriptNil.Value;
                    }
                    return DekiScriptExpression.Constant(text.Value[position].ToString());
                }
            case DekiScriptType.XML: {
                    string path = index.AsString();
                    if(path == null) {
                        throw new DekiScriptBadTypeException(expr.Location, index.ScriptType, new[] { DekiScriptType.STR });
                    }
                    XDoc doc = ((DekiScriptXml)prefix).Value[path];
                    if(doc.HasName("html")) {
                        doc = DekiScriptLibrary.CleanseHtmlDocument(doc);
                    }
                    return new DekiScriptXml(doc);
                }
            case DekiScriptType.NIL:
                return DekiScriptNil.Value;
            }
            throw new DekiScriptBadTypeException(expr.Location, prefix.ScriptType, new[] { DekiScriptType.MAP, DekiScriptType.LIST, DekiScriptType.XML, DekiScriptType.STR, DekiScriptType.URI });
        }
    }
}
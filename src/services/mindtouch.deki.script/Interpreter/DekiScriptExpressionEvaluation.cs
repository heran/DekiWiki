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
using MindTouch.Dream;
using MindTouch.Tasking;
using MindTouch.Xml;

namespace MindTouch.Deki.Script.Interpreter {
    internal class DekiScriptExpressionEvaluation : IDekiScriptExpressionVisitor<DekiScriptEnv, DekiScriptLiteral> {

        //--- Types ---

        //--- Class Fields ---
        public static readonly DekiScriptExpressionEvaluation Instance = new DekiScriptExpressionEvaluation();

        //--- Constructors ---
        private DekiScriptExpressionEvaluation() { }

        //--- Methods ---
        public DekiScriptLiteral Visit(DekiScriptAbort expr, DekiScriptEnv env) {
            throw new DekiScriptAbort.Exception(expr.FlowControl);
        }

        public DekiScriptLiteral Visit(DekiScriptAccess expr, DekiScriptEnv env) {
            return Evaluate(expr, env, true);
        }

        public DekiScriptLiteral Visit(DekiScriptAssign expr, DekiScriptEnv env) {
            if(expr.Define) {
                env.Locals.Add(expr.Variable, expr.Value.VisitWith(this, env));
            } else {
                env.Locals[expr.Variable] = expr.Value.VisitWith(this, env);
            }
            return DekiScriptNil.Value;
        }

        public DekiScriptLiteral Visit(DekiScriptBinary expr, DekiScriptEnv env) {
            DekiScriptExpression Left = expr.Left;
            DekiScriptExpression Right = expr.Right;
            switch(expr.OpCode) {
            case DekiScriptBinary.Op.LeftValue:
                return Left.VisitWith(this, env);
            case DekiScriptBinary.Op.IdentityEqual: {
                    DekiScriptLiteral left = Left.VisitWith(this, env);
                    DekiScriptLiteral right = Right.VisitWith(this, env);
                    return DekiScriptBinary.IdentityEqual(left, right);
                }
            case DekiScriptBinary.Op.IdentityNotEqual: {
                    DekiScriptLiteral left = Left.VisitWith(this, env);
                    DekiScriptLiteral right = Right.VisitWith(this, env);
                    return DekiScriptBinary.IdentityNotEqual(left, right);
                }
            case DekiScriptBinary.Op.IsType: {
                    DekiScriptLiteral left = Left.VisitWith(this, env);
                    string type = ((DekiScriptString)Right).Value;
                    return DekiScriptBool.New(StringUtil.EqualsInvariantIgnoreCase(type, "any") || StringUtil.EqualsInvariantIgnoreCase(left.ScriptTypeName, type));
                }
            case DekiScriptBinary.Op.Equal: {
                    DekiScriptLiteral left = Left.VisitWith(this, env);
                    DekiScriptLiteral right = Right.VisitWith(this, env);
                    return DekiScriptBinary.Equal(left, right);
                }
            case DekiScriptBinary.Op.NotEqual: {
                    DekiScriptLiteral left = Left.VisitWith(this, env);
                    DekiScriptLiteral right = Right.VisitWith(this, env);
                    return DekiScriptBinary.NotEqual(left, right);
                }
            case DekiScriptBinary.Op.GreaterOrEqual: {
                    DekiScriptLiteral left = Left.VisitWith(this, env);
                    DekiScriptLiteral right = Right.VisitWith(this, env);
                    if(DekiScriptLiteral.CoerceValuesToSameType(ref left, ref right)) {
                        switch(left.ScriptType) {
                        case DekiScriptType.BOOL:
                        case DekiScriptType.NUM:
                            return DekiScriptBool.New(left.AsNumber() >= right.AsNumber());
                        case DekiScriptType.STR:
                            return DekiScriptBool.New(StringUtil.CompareInvariant(left.AsString(), right.AsString()) >= 0);
                        default:
                            return DekiScriptNil.Value;
                        }
                    } else {
                        return DekiScriptNil.Value;
                    }
                }
            case DekiScriptBinary.Op.GreaterThan: {
                    DekiScriptLiteral left = Left.VisitWith(this, env);
                    DekiScriptLiteral right = Right.VisitWith(this, env);
                    if(DekiScriptLiteral.CoerceValuesToSameType(ref left, ref right)) {
                        switch(left.ScriptType) {
                        case DekiScriptType.BOOL:
                        case DekiScriptType.NUM:
                            return DekiScriptBool.New(left.AsNumber() > right.AsNumber());
                        case DekiScriptType.STR:
                            return DekiScriptBool.New(StringUtil.CompareInvariant(left.AsString(), right.AsString()) > 0);
                        default:
                            return DekiScriptNil.Value;
                        }
                    } else {
                        return DekiScriptNil.Value;
                    }
                }
            case DekiScriptBinary.Op.LessOrEqual: {
                    DekiScriptLiteral left = Left.VisitWith(this, env);
                    DekiScriptLiteral right = Right.VisitWith(this, env);
                    if(DekiScriptLiteral.CoerceValuesToSameType(ref left, ref right)) {
                        switch(left.ScriptType) {
                        case DekiScriptType.BOOL:
                        case DekiScriptType.NUM:
                            return DekiScriptBool.New(left.AsNumber() <= right.AsNumber());
                        case DekiScriptType.STR:
                            return DekiScriptBool.New(StringUtil.CompareInvariant(left.AsString(), right.AsString()) <= 0);
                        default:
                            return DekiScriptNil.Value;
                        }
                    } else {
                        return DekiScriptNil.Value;
                    }
                }
            case DekiScriptBinary.Op.LessThan: {
                    DekiScriptLiteral left = Left.VisitWith(this, env);
                    DekiScriptLiteral right = Right.VisitWith(this, env);
                    if(DekiScriptLiteral.CoerceValuesToSameType(ref left, ref right)) {
                        switch(left.ScriptType) {
                        case DekiScriptType.BOOL:
                        case DekiScriptType.NUM:
                            return DekiScriptBool.New(left.AsNumber() < right.AsNumber());
                        case DekiScriptType.STR:
                            return DekiScriptBool.New(StringUtil.CompareInvariant(left.AsString(), right.AsString()) < 0);
                        default:
                            return DekiScriptNil.Value;
                        }
                    } else {
                        return DekiScriptNil.Value;
                    }
                }
            case DekiScriptBinary.Op.LogicalAnd: {
                    DekiScriptLiteral result = Left.VisitWith(this, env);
                    if(!result.IsNilFalseZero) {
                        result = Right.VisitWith(this, env);
                    }
                    return result;
                }
            case DekiScriptBinary.Op.LogicalOr: {
                    DekiScriptLiteral result = Left.VisitWith(this, env);
                    if(result.IsNilFalseZero) {
                        result = Right.VisitWith(this, env);
                    }
                    return result;
                }
            case DekiScriptBinary.Op.Addition:
                return DekiScriptNumber.New(Left.VisitWith(this, env).AsNumber() + Right.VisitWith(this, env).AsNumber());
            case DekiScriptBinary.Op.Division:
                return DekiScriptNumber.New(Left.VisitWith(this, env).AsNumber() / Right.VisitWith(this, env).AsNumber());
            case DekiScriptBinary.Op.Modulo: {
                    DekiScriptLiteral left = Left.VisitWith(this, env);
                    DekiScriptLiteral right = Right.VisitWith(this, env);
                    if((left is DekiScriptString) && ((right is DekiScriptMap) || (right is DekiScriptList))) {

                        // NOTE (steveb): string.format shorthand notation: "abc = $abc" % { abc: 123 } -OR- "0 = $0" % [ 123 ]
                        return DekiScriptLiteral.FromNativeValue(DekiScriptLibrary.StringFormat(((DekiScriptString)left).Value, right.NativeValue));
                    } else {
                        return DekiScriptNumber.New(left.AsNumber() % right.AsNumber());
                    }
                }
            case DekiScriptBinary.Op.Multiplication:
                return DekiScriptNumber.New(Left.VisitWith(this, env).AsNumber() * Right.VisitWith(this, env).AsNumber());
            case DekiScriptBinary.Op.Subtraction:
                return DekiScriptNumber.New(Left.VisitWith(this, env).AsNumber() - Right.VisitWith(this, env).AsNumber());
            case DekiScriptBinary.Op.NullCoalesce: {
                    DekiScriptLiteral result = Left.VisitWith(this, env);
                    if(result.IsNil) {
                        result = Right.VisitWith(this, env);
                    }
                    return result;
                }
            case DekiScriptBinary.Op.Concat: {
                    DekiScriptLiteral left = Left.VisitWith(this, env);
                    DekiScriptLiteral right = Right.VisitWith(this, env);
                    if(left is DekiScriptNil) {
                        return right;
                    } else if(right is DekiScriptNil) {
                        return left;
                    } else if((left is DekiScriptMap) && (right is DekiScriptMap)) {

                        // left and right expressions are maps, merge them
                        DekiScriptMap result = new DekiScriptMap();
                        result.AddRange((DekiScriptMap)left);
                        result.AddRange((DekiScriptMap)right);
                        return result;
                    } else if((left is DekiScriptList) && (right is DekiScriptList)) {

                        // left and right expressions are lists, concatenate them
                        DekiScriptList result = new DekiScriptList();
                        result.AddRange((DekiScriptList)left);
                        result.AddRange((DekiScriptList)right);
                        return result;
                    } else {

                        // treat left and right expressions as strings
                        string leftText = left.AsString();
                        string rightText = right.AsString();
                        if((leftText != null) && (rightText != null)) {
                            return DekiScriptString.New(leftText + rightText);
                        } else if(leftText != null) {
                            return DekiScriptString.New(leftText);
                        } else if(rightText != null) {
                            return DekiScriptString.New(rightText);
                        } else {
                            return DekiScriptNil.Value;
                        }
                    }
                }
            case DekiScriptBinary.Op.UriAppend: {

                    // TODO (steveb): we should throw an exception when the LHS is not a valid string or uri

                    XUri left = XUri.TryParse(Left.VisitWith(this, env).AsString());
                    string result = null;
                    if(left != null) {
                        DekiScriptLiteral right = Right.VisitWith(this, env);
                        if(right is DekiScriptString) {
                            result = DekiScriptLibrary.UriBuild(left, right.AsString(), null);
                        } else if(right is DekiScriptMap) {
                            result = DekiScriptLibrary.UriBuild(left, null, (Hashtable)right.NativeValue);
                        } else {
                            result = left.ToString();
                        }
                    }
                    return DekiScriptLiteral.FromNativeValue(result);
                }
            case DekiScriptBinary.Op.InCollection: {
                    DekiScriptLiteral left = Left.VisitWith(this, env);
                    DekiScriptLiteral right = Right.VisitWith(this, env);
                    if(right is DekiScriptList) {
                        foreach(DekiScriptLiteral item in ((DekiScriptList)right).Value) {
                            if(!DekiScriptBinary.Equal(left, item).IsNilFalseZero) {
                                return DekiScriptBool.True;
                            }
                        }
                        return DekiScriptBool.False;
                    } else if(right is DekiScriptMap) {
                        foreach(DekiScriptLiteral item in ((DekiScriptMap)right).Value.Values) {
                            if(!DekiScriptBinary.Equal(left, item).IsNilFalseZero) {
                                return DekiScriptBool.True;
                            }
                        }
                        return DekiScriptBool.False;
                    } else {
                        return DekiScriptBool.False;
                    }
                }
            }
            throw new InvalidOperationException("invalid op code:" + expr.OpCode);
        }

        public DekiScriptLiteral Visit(DekiScriptBool expr, DekiScriptEnv env) {
            return expr;
        }

        public DekiScriptLiteral Visit(DekiScriptCall expr, DekiScriptEnv env) {

            // evaluate prefix
            DekiScriptLiteral prefix = expr.Prefix.VisitWith(this, env);
            if(prefix.ScriptType != DekiScriptType.URI) {
                if(prefix.ScriptType == DekiScriptType.NIL) {
                    throw new DekiScriptUndefinedNameException(expr.Line, expr.Column, expr.Prefix.ToString());
                } else {
                    throw new DekiScriptBadTypeException(expr.Line, expr.Column, prefix.ScriptType, new DekiScriptType[] { DekiScriptType.URI });
                }
            }

            // evaluate arguments
            DekiScriptLiteral arguments = expr.Arguments.VisitWith(this, env);
            if((arguments.ScriptType != DekiScriptType.MAP) && (arguments.ScriptType != DekiScriptType.LIST)) {
                throw new DekiScriptBadTypeException(expr.Line, expr.Column, arguments.ScriptType, new DekiScriptType[] { DekiScriptType.MAP, DekiScriptType.LIST });
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
                        throw new DekiScriptBadTypeException(expr.Line, expr.Column, arguments.ScriptType, new DekiScriptType[] { DekiScriptType.MAP });
                    }
                    break;
                default:
                    throw new DekiScriptBadTypeException(expr.Line, expr.Column, arguments.ScriptType, new DekiScriptType[] { DekiScriptType.MAP, DekiScriptType.LIST });
                }
            }

            // check if this is an invocation or curry operation
            if(expr.IsCurryOperation) {
                return new DekiScriptUri(uri.Value, arguments);
            } else {

                // invoke function
                return Coroutine.Invoke(DekiScriptRuntime.Invoke, uri.Value, arguments, env, new Result<DekiScriptLiteral>()).Wait();
            }
        }

        public DekiScriptLiteral Visit(DekiScriptForeach expr, DekiScriptEnv env) {
            bool safe = env.IsSafeMode;
            try {
                DekiScriptEvaluationAccumulator accumulator = new DekiScriptEvaluationAccumulator();
                DekiScriptGeneratorEvaluation.Instance.Generate(expr.Generator, delegate(DekiScriptEnv subEnv) {

                    // iterate over block statements
                    try {
                        accumulator.Add(expr.Block.VisitWith(this, subEnv), safe);
                    } catch(DekiScriptAbort.Exception e) {
                        accumulator.Add(e.AccumulatedState, safe);
                        if(e.FlowControl == DekiScriptAbort.Kind.Break) {
                            throw new DekiScriptAbort.Exception(e.FlowControl, accumulator.Value);
                        }
                    }
                }, env.NewLocalScope());
                return accumulator.Value;
            } catch(DekiScriptAbort.Exception e) {
                return e.AccumulatedState;
            }
        }

        public DekiScriptLiteral Visit(DekiScriptList expr, DekiScriptEnv env) {
            return expr;
        }

        public DekiScriptLiteral Visit(DekiScriptListConstructor expr, DekiScriptEnv env) {
            DekiScriptList result = new DekiScriptList();
            if(expr.Generator == null) {
                foreach(DekiScriptExpression item in expr.Items) {
                    result.Add(item.VisitWith(this, env));
                }
            } else {
                DekiScriptGeneratorEvaluation.Instance.Generate(expr.Generator, delegate(DekiScriptEnv subEnv) {
                    foreach(DekiScriptExpression item in expr.Items) {
                        result.Add(item.VisitWith(this, subEnv));
                    }
                }, env.NewLocalScope());
            }
            return result;
        }

        public DekiScriptLiteral Visit(DekiScriptMagicId expr, DekiScriptEnv env) {
            return env.GetMagicId(expr.Name);
        }

        public DekiScriptLiteral Visit(DekiScriptMap expr, DekiScriptEnv env) {
            return expr;
        }

        public DekiScriptLiteral Visit(DekiScriptMapConstructor expr, DekiScriptEnv env) {
            DekiScriptMap result = new DekiScriptMap();
            if(expr.Generator == null) {
                foreach(DekiScriptMapConstructor.FieldConstructor field in expr.Fields) {
                    DekiScriptLiteral key = field.Key.VisitWith(this, env);

                    // check that key is a simple type
                    string text = key.AsString();
                    if(text != null) {
                        DekiScriptLiteral value = field.Value.VisitWith(this, env);
                        result.Add(text, value);
                    }
                }
            } else {
                DekiScriptGeneratorEvaluation.Instance.Generate(expr.Generator, delegate(DekiScriptEnv subEnv) {
                    foreach(DekiScriptMapConstructor.FieldConstructor field in expr.Fields) {
                        DekiScriptLiteral key = field.Key.VisitWith(this, subEnv);

                        // check that key is a simple type
                        string text = key.AsString();
                        if(text != null) {
                            DekiScriptLiteral value = field.Value.VisitWith(this, subEnv);
                            result.Add(text, value);
                        }
                    }
                }, env.NewLocalScope());
            }
            return result;
        }

        public DekiScriptLiteral Visit(DekiScriptNil expr, DekiScriptEnv env) {
            return expr;
        }

        public DekiScriptLiteral Visit(DekiScriptNumber expr, DekiScriptEnv env) {
            return expr;
        }

        public DekiScriptLiteral Visit(DekiScriptSequence expr, DekiScriptEnv env) {
            if(expr.Kind != DekiScriptSequence.ScopeKind.None) {
                env = env.NewLocalScope();
            }
            bool safe = env.IsSafeMode;
            DekiScriptEvaluationAccumulator accumulator = new DekiScriptEvaluationAccumulator();
            foreach(DekiScriptExpression expression in expr.List) {
                try {
                    accumulator.Add(expression.VisitWith(this, env), safe);
                } catch(DekiScriptAbort.Exception e) {

                    // flow control exception occurred (e.g. break/continue)
                    accumulator.Add(e.AccumulatedState, safe);
                    switch(expr.Kind) {
                    case DekiScriptSequence.ScopeKind.ScopeCatchContinue:
                        if(e.FlowControl == DekiScriptAbort.Kind.Continue) {
                            return accumulator.Value;
                        }
                        break;
                    case DekiScriptSequence.ScopeKind.ScopeCatchBreakAndContinue:
                        return accumulator.Value;
                    }
                    throw new DekiScriptAbort.Exception(e.FlowControl, accumulator.Value);
                }
            }
            return accumulator.Value;
        }

        public DekiScriptLiteral Visit(DekiScriptString expr, DekiScriptEnv env) {
            return expr;
        }

        public DekiScriptLiteral Visit(DekiScriptSwitch expr, DekiScriptEnv env) {
            DekiScriptLiteral value = expr.Value.VisitWith(this, env);
            DekiScriptCaseBlock defaultCase = null;

            // have to use for instead of foreach, since a fallthrough default case needs to be able to look ahead
            for(int i = 0; i < expr.Cases.Length; i++) {
                DekiScriptCaseBlock c = expr.Cases[i];

                // check for default case
                DekiScriptEnv locals = env.NewLocalScope();
                foreach(DekiScriptExpression condition in c.Conditions) {
                    if(condition == null) {

                        // check if this is the first default we've found
                        if(defaultCase == null) {
                            defaultCase = c;
                        }

                        // continue in case loop, since default only gets executed if there is no match
                        continue;
                    }

                    // evaluate test
                    DekiScriptBinary test = new DekiScriptBinary(0, 0, DekiScriptBinary.Op.Equal, value, condition);
                    DekiScriptLiteral caseMatch = test.VisitWith(this, locals);

                    // evaluate body on success
                    if(!caseMatch.IsNilFalseZero) {
                        return EvalBody(locals, c.Body);
                    }
                }
            }

            // haven't found a match yet, so if we have a default, return it
            if(defaultCase != null) {
                return EvalBody(env.NewLocalScope(), defaultCase.Body);
            }
            return DekiScriptNil.Value;
        }

        public DekiScriptLiteral Visit(DekiScriptTernary expr, DekiScriptEnv env) {
            DekiScriptLiteral test = expr.Test.VisitWith(this, env);
            DekiScriptLiteral result;

            // check if a local variable scope needs to be created
            if(expr.IsIfElse) {
                env = env.NewLocalScope();
            }

            // check which branch should be executed
            if(!test.IsNilFalseZero) {
                result = expr.Left.VisitWith(this, env);
            } else {
                result = expr.Right.VisitWith(this, env);
            }
            return result;
        }

        public DekiScriptLiteral Visit(DekiScriptUnary expr, DekiScriptEnv env) {
            switch(expr.OpCode) {
            case DekiScriptUnary.Op.Negate:
                return DekiScriptNumber.New(-expr.Value.VisitWith(this, env).AsNumber());
            case DekiScriptUnary.Op.LogicalNot:
                return DekiScriptBool.New(expr.Value.VisitWith(this, env).IsNilFalseZero);
            case DekiScriptUnary.Op.TypeOf:
                return DekiScriptString.New(expr.Value.VisitWith(this, env).ScriptTypeName);
            case DekiScriptUnary.Op.Length: {
                    DekiScriptLiteral value = expr.Value.VisitWith(this, env);
                    switch(value.ScriptType) {
                    case DekiScriptType.NIL:
                        return DekiScriptNumber.New(0);
                    case DekiScriptType.LIST:
                        return DekiScriptNumber.New(((DekiScriptList)value).Value.Count);
                    case DekiScriptType.STR:
                        return DekiScriptNumber.New(((DekiScriptString)value).Value.Length);
                    case DekiScriptType.MAP:
                        return DekiScriptNumber.New(((DekiScriptMap)value).Value.Count);
                    case DekiScriptType.XML:
                        return DekiScriptNumber.New(((DekiScriptXml)value).Value.ListLength);
                    default:
                        return DekiScriptNil.Value;
                    }
                }
            }
            throw new InvalidOperationException("invalid op code:" + expr.OpCode);
        }

        public DekiScriptLiteral Visit(DekiScriptUnknown expr, DekiScriptEnv env) {
            return expr;
        }

        public DekiScriptLiteral Visit(DekiScriptUri expr, DekiScriptEnv env) {
            return expr;
        }

        public DekiScriptLiteral Visit(DekiScriptVar expr, DekiScriptEnv env) {
            if(expr.Name.EqualsInvariant(DekiScriptRuntime.ENV_ID)) {
                DekiScriptMap vars = new DekiScriptMap();
                vars.AddRange(env.Globals);
                vars.AddRange(env.Locals);
                return vars;
            }

            // check if variable exists
            DekiScriptLiteral result = env[expr.Name];
            if(result == null) {
                throw new DekiScriptUndefinedNameException(expr.Line, expr.Column, expr.Name);
            }
            result = DekiScriptRuntime.EvaluateProperty(result, env);
            return result;
        }

        public DekiScriptLiteral Visit(DekiScriptXml expr, DekiScriptEnv env) {
            return expr;
        }

        public DekiScriptLiteral Visit(DekiScriptXmlConstructor expr, DekiScriptEnv env) {
            XDoc xml = DekiScriptDomEvaluation.Instance.Evaluate(expr.Value, DekiScriptEvalMode.Evaluate, true, env);
            return new DekiScriptXml(xml, env.IsSafeMode);
        }

        private DekiScriptLiteral EvalBody(DekiScriptEnv env, DekiScriptExpression body) {
            try {
                env = env.NewLocalScope();
                return body.VisitWith(this, env);
            } catch(DekiScriptAbort.Exception e) {
                if(e.FlowControl == DekiScriptAbort.Kind.Continue) {
                    throw;
                }
                return e.AccumulatedState;
            }
        }

        internal DekiScriptLiteral Evaluate(DekiScriptAccess expr, DekiScriptEnv env, bool evaluateProperties) {
            DekiScriptLiteral prefix = expr.Prefix.VisitWith(this, env);
            DekiScriptLiteral index = expr.Index.VisitWith(this, env);
            switch(prefix.ScriptType) {
            case DekiScriptType.MAP: {
                    DekiScriptLiteral result = ((DekiScriptMap)prefix)[index];
                    if(evaluateProperties) {
                        result = DekiScriptRuntime.EvaluateProperty(result, env);
                    }
                    return result;
                }
            case DekiScriptType.LIST: {
                    DekiScriptLiteral value = DekiScriptNumber.New(index.AsNumber());
                    DekiScriptLiteral result = ((DekiScriptList)prefix)[value];
                    if(evaluateProperties) {
                        result = DekiScriptRuntime.EvaluateProperty(result, env);
                    }
                    return result;
                }
            case DekiScriptType.URI: {
                    DekiScriptUri uri = (DekiScriptUri)prefix;

                    // coerce the index type to STR
                    index = DekiScriptString.New(index.AsString());
                    if(index.ScriptType != DekiScriptType.STR) {
                        throw new DekiScriptBadTypeException(expr.Line, expr.Column, index.ScriptType, new[] { DekiScriptType.STR });
                    }

                    // curry the argument
                    DekiScriptList args;
                    if(!uri.Arguments.IsNil) {

                        // the uri already has curried parameters, make sure they are in LIST format; otherwise fail
                        if(uri.Arguments.ScriptType != DekiScriptType.LIST) {
                            throw new DekiScriptBadTypeException(expr.Line, expr.Column, uri.Arguments.ScriptType, new[] { DekiScriptType.NIL, DekiScriptType.LIST });
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
                        throw new DekiScriptBadTypeException(expr.Line, expr.Column, index.ScriptType, new[] { DekiScriptType.NUM });
                    }

                    // retrieve character at given index position
                    int position = (int)value;
                    if((position < 0) || (position >= text.Value.Length)) {

                        // index is out of bounds, return nil
                        return DekiScriptNil.Value;
                    }
                    return DekiScriptString.New(text.Value[position].ToString());
                }
            case DekiScriptType.XML: {
                    string path = index.AsString();
                    if(path == null) {
                        throw new DekiScriptBadTypeException(expr.Line, expr.Column, index.ScriptType, new[] { DekiScriptType.STR });
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
            throw new DekiScriptBadTypeException(expr.Line, expr.Column, prefix.ScriptType, new[] { DekiScriptType.MAP, DekiScriptType.LIST, DekiScriptType.XML, DekiScriptType.STR, DekiScriptType.URI });
        }
    }
}

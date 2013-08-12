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
using MindTouch.Deki.Script.Dom;
using MindTouch.Deki.Script.Interpreter;
using MindTouch.Xml;

namespace MindTouch.Deki.Script.Optimizer {
    internal class DekiScriptExpressionOptimizer : IDekiScriptExpressionVisitor<DekiScriptOptimizerState, DekiScriptExpression> {

        //--- Class Methods ---
        public static readonly DekiScriptExpressionOptimizer Instance = new DekiScriptExpressionOptimizer();

        //--- Constructors ---
        private DekiScriptExpressionOptimizer() { }

        //--- Methods ---
        public DekiScriptExpression Visit(DekiScriptAbort expr, DekiScriptOptimizerState state) {
            return expr;
        }

        public DekiScriptExpression Visit(DekiScriptAccess expr, DekiScriptOptimizerState state) {
            DekiScriptExpression prefix = expr.Prefix.VisitWith(this, state);
            DekiScriptExpression index = expr.Index.VisitWith(this, state);
            DekiScriptAccess access = new DekiScriptAccess(expr.Line, expr.Column, prefix, index);
            if((prefix is DekiScriptLiteral) && (index is DekiScriptLiteral)) {

                // BUGBUGBUG (steveb): don't eval properties!
                DekiScriptLiteral result = DekiScriptExpressionEvaluation.Instance.Evaluate(access, state.Env, false);

                // check if result is a property
                if(DekiScriptRuntime.IsProperty(result)) {

                    // retrieve property information
                    DekiScriptFunction function;
                    if(DekiScriptLibrary.Functions.TryGetValue(((DekiScriptUri)result).Value, out function)) {
                        if(function is DekiScriptFunctionNative) {
                            DekiScriptFunctionNative native = (DekiScriptFunctionNative)function;

                            // check if function is idempotent; if so, execute it
                            if(native.IsIdempotent) {

                                // evaluate property, since it never changes
                                return DekiScriptRuntime.EvaluateProperty(result, state.Env);
                            }
                        }
                    }
                    return new DekiScriptCall(expr.Line, expr.Column, result, new DekiScriptList(), false);
                }
                return result;
            }
            return access;
        }

        public DekiScriptExpression Visit(DekiScriptAssign expr, DekiScriptOptimizerState state) {
            DekiScriptExpression result = expr.Value.VisitWith(this, state);
            DekiScriptLiteral value = (result is DekiScriptLiteral) ? (DekiScriptLiteral)result : DekiScriptUnknown.Value;
            if(expr.Define) {
                state.Env.Locals.Add(expr.Variable, value);
            } else {
                state.Env.Locals[expr.Variable] = value;
            }
            if(result is DekiScriptLiteral) {

                // NOTE: variable was assigned a value; it will be substituted into subsequent operations

                return DekiScriptNil.Value;
            }
            return new DekiScriptAssign(expr.Line, expr.Column, expr.Variable, result, expr.Define);
        }

        public DekiScriptExpression Visit(DekiScriptBinary expr, DekiScriptOptimizerState state) {
            DekiScriptExpression left = expr.Left.VisitWith(this, state);
            DekiScriptExpression right = expr.Right.VisitWith(this, state);
            DekiScriptExpression result = new DekiScriptBinary(expr.Line, expr.Column, expr.OpCode, left, right);
            if((left is DekiScriptLiteral) && (right is DekiScriptLiteral)) {
                result = result.Evaluate(state.Env);
            }
            return result;
        }

        public DekiScriptExpression Visit(DekiScriptBool expr, DekiScriptOptimizerState state) {
            return expr;
        }

        public DekiScriptExpression Visit(DekiScriptCall expr, DekiScriptOptimizerState state) {
            DekiScriptExpression prefix = expr.Prefix.VisitWith(this, state);
            DekiScriptExpression arguments = expr.Arguments.VisitWith(this, state);
            DekiScriptExpression result = new DekiScriptCall(expr.Line, expr.Column, prefix, arguments, expr.IsCurryOperation);

            // check if prefix has been resolved to a uri and the arguments have all been resolved
            if((prefix is DekiScriptUri) && (arguments is DekiScriptLiteral)) {
                DekiScriptFunction function;

                // check if the uri can be resolved to a native idempotent function
                if(
                    DekiScriptLibrary.Functions.TryGetValue(((DekiScriptUri)prefix).Value, out function) && 
                    (function is DekiScriptFunctionNative) && 
                    ((DekiScriptFunctionNative)function).IsIdempotent
                ) {

                    // evaluate function, since it never changes
                    return new DekiScriptCall(expr.Line, expr.Column, prefix, arguments, false).Evaluate(state.Env);
                }
            }
            return result;
        }

        public DekiScriptExpression Visit(DekiScriptForeach expr, DekiScriptOptimizerState state) {

            // TODO (steveb): partial evaluation broke with the introduction of generators

            return expr;
            
            //DekiScriptExpression collection = Collection.Optimize(mode, env);

            //// check if we can unroll the loop
            //if(collection is DekiScriptLiteral) {

            //    // retrieve collection
            //    List<DekiScriptLiteral> list;
            //    if(collection is DekiScriptList) {

            //        // loop over list values
            //        list = ((DekiScriptList)collection).Value;
            //    } else if(collection is DekiScriptMap) {

            //        // loop over map key-value pairs
            //        list = new List<DekiScriptLiteral>(((DekiScriptMap)collection).Value.Values);
            //    } else if(collection is DekiScriptXml) {

            //        // loop over xml selection
            //        List<XDoc> selection = ((DekiScriptXml)collection).Value.ToList();
            //        list = new List<DekiScriptLiteral>(selection.Count);
            //        foreach(XDoc doc in selection) {
            //            list.Add(new DekiScriptXml(doc));
            //        }
            //    } else if(collection is DekiScriptNil) {

            //        // nothing to do
            //        list = new List<DekiScriptLiteral>();
            //    } else {
            //        return new DekiScriptForeach(Line, Column, Var, collection, Block);
            //    }

            //    // loop over collection
            //    List<DekiScriptExpression> expressions = new List<DekiScriptExpression>();
            //    int index = 0;
            //    foreach(DekiScriptLiteral value in list) {
            //        DekiScriptEnv subEnv = env.NewLocalScope();

            //        // set the environment variable
            //        subEnv.Locals.Add(Var, value);
            //        subEnv.Locals.Add(DekiScriptRuntime.COUNT_ID, DekiScriptNumber.New(index));
            //        subEnv.Locals.Add(DekiScriptRuntime.INDEX_ID, DekiScriptNumber.New(index));

            //        // NOTE (steveb): we wrap the outcome into a sequence to ensure proper handling of break/continue statements

            //        expressions.Add(DekiScriptSequence.New(DekiScriptSequence.ScopeKind.ScopeCatchContinue, value.Optimize(mode, subEnv)));
            //        ++index;
            //    }
            //    return DekiScriptSequence.New(DekiScriptSequence.ScopeKind.ScopeCatchBreakAndContinue, expressions.ToArray());
            //} else {
            //    DekiScriptEnv subEnv = env.NewLocalScope();

            //    // set the environment variable to unknown
            //    subEnv.Locals.Add(Var, DekiScriptUnknown.Value);
            //    subEnv.Locals.Add(DekiScriptRuntime.COUNT_ID, DekiScriptUnknown.Value);
            //    subEnv.Locals.Add(DekiScriptRuntime.INDEX_ID, DekiScriptUnknown.Value);

            //    // partially evaluate the inner loop
            //    DekiScriptExpression block = Block.Optimize(mode, subEnv);
            //    return new DekiScriptForeach(Line, Column, Var, collection, block);
            //}
        }

        public DekiScriptExpression Visit(DekiScriptList expr, DekiScriptOptimizerState state) {
            return expr;
        }

        public DekiScriptExpression Visit(DekiScriptListConstructor expr, DekiScriptOptimizerState state) {

            // TODO (steveb): need to figure out how to optimize lists with generators
            if(expr.Generator != null) {
                return expr;
            }

            // optimize each item in the list
            DekiScriptExpression[] list = new DekiScriptExpression[expr.Items.Length];
            bool isLiteral = true;
            for(int i = 0; i < expr.Items.Length; i++) {
                DekiScriptExpression item = expr.Items[i].VisitWith(this, state);
                list[i] = item;
                isLiteral = isLiteral && (item is DekiScriptLiteral);
            }
            if(!isLiteral) {
                return new DekiScriptListConstructor(expr.Line, expr.Column, null, list);
            }

            // convert expression to a list
            DekiScriptList result = new DekiScriptList();
            foreach(DekiScriptLiteral literal in list) {
                result.Add(literal);
            }
            return result;
        }

        public DekiScriptExpression Visit(DekiScriptMagicId expr, DekiScriptOptimizerState state) {
            return expr;
        }

        public DekiScriptExpression Visit(DekiScriptMap expr, DekiScriptOptimizerState state) {
            return expr;
        }

        public DekiScriptExpression Visit(DekiScriptMapConstructor expr, DekiScriptOptimizerState state) {

            // TODO (steveb): need to figure out how to optimize maps with generators
            if(expr.Generator != null) {
                return expr;
            }

            // optimize each item in the map
            var fields = new DekiScriptMapConstructor.FieldConstructor[expr.Fields.Length];
            bool isLiteral = true;
            for(int i = 0; i < expr.Fields.Length; i++) {
                var current = expr.Fields[i];
                DekiScriptExpression key = current.Key.VisitWith(this, state);
                DekiScriptExpression value = current.Value.VisitWith(this, state);
                fields[i] = new DekiScriptMapConstructor.FieldConstructor(current.Line, current.Column, key, value);
                isLiteral = isLiteral && (key is DekiScriptLiteral) && (value is DekiScriptLiteral);
            }
            if(!isLiteral) {
                return new DekiScriptMapConstructor(expr.Line, expr.Column, null, fields);
            }

            // convert expression to a map
            DekiScriptMap result = new DekiScriptMap();
            foreach(var field in fields) {
                DekiScriptLiteral key = (DekiScriptLiteral)field.Key;

                // check that key is a simple type
                string text = key.AsString();
                if(text != null) {
                    result.Add(text, (DekiScriptLiteral)field.Value);
                }
            }
            return result;
        }

        public DekiScriptExpression Visit(DekiScriptNil expr, DekiScriptOptimizerState state) {
            return expr;
        }

        public DekiScriptExpression Visit(DekiScriptNumber expr, DekiScriptOptimizerState state) {
            return expr;
        }

        public DekiScriptExpression Visit(DekiScriptSequence expr, DekiScriptOptimizerState state) {
            var mode = state.Mode;
            var env = state.Env;
            if(expr.Kind != DekiScriptSequence.ScopeKind.None) {
                env = env.NewLocalScope();
            }
            bool safe = env.IsSafeMode;

            // loop over all expressions and accumulate as many as possible
            var accumulator = new DekiScriptEvaluationAccumulator();
            List<DekiScriptExpression> list = new List<DekiScriptExpression>(expr.List.Length);
            foreach(DekiScriptExpression expression in expr.List) {
                DekiScriptExpression value = expression.VisitWith(this, new DekiScriptOptimizerState(mode, env));

                // check if we can continue to accumulate the values
                if((accumulator != null) && value is DekiScriptLiteral) {
                    accumulator.Add((DekiScriptLiteral)value, safe);
                } else {

                    // check if the accumulator contains a value to add to the list
                    if(accumulator != null) {
                        list.Add(accumulator.Value);
                        accumulator = null;
                    }

                    // check if value is worthile keeping
                    if(!(value is DekiScriptNil)) {
                        list.Add(value);
                    }
                }
            }
            if(accumulator != null) {
                return accumulator.Value;
            }
            return DekiScriptSequence.New(expr.Kind, list.ToArray());
        }

        public DekiScriptExpression Visit(DekiScriptString expr, DekiScriptOptimizerState state) {
            return expr;
        }

        public DekiScriptExpression Visit(DekiScriptSwitch expr, DekiScriptOptimizerState state) {
            DekiScriptExpression value = expr.Value.VisitWith(this, state);
            List<DekiScriptCaseBlock> cases = new List<DekiScriptCaseBlock>();
            int defaultCaseIndex = -1;
            for(int i = 0; i < expr.Cases.Length; ++i) {
                bool isDefaultCase;
                object outcome =  Optimize(expr.Cases[i], value, state.Mode, state.Env.NewLocalScope(), out isDefaultCase);
                if(outcome is DekiScriptExpression) {
                    return (DekiScriptExpression)outcome;
                }
                if(outcome != null) {
                    cases.Add((DekiScriptCaseBlock)outcome);
                }

                // check if case block contains a default case statement
                if(isDefaultCase && (defaultCaseIndex == -1)) {
                    defaultCaseIndex = cases.Count - 1;
                }
            }

            // check if all matches failed, but we found a default branch
            if((cases.Count == 1) && (defaultCaseIndex == 0) && (cases[0].Conditions.Length == 1)) {
                return cases[0].Body;
            }
            if(cases.Count > 0) {
                return new DekiScriptSwitch(expr.Line, expr.Column, value, cases.ToArray());
            }
            return DekiScriptNil.Value;
        }

        public DekiScriptExpression Visit(DekiScriptTernary expr, DekiScriptOptimizerState state) {
            var mode = state.Mode;
            var env = state.Env;
            DekiScriptExpression test = expr.Test.VisitWith(this, state);
            DekiScriptExpression result;

            // check if a local variable scope needs to be created
            if(expr.IsIfElse) {
                env = env.NewLocalScope();
            }

            // check which branch should be executed
            if(test is DekiScriptLiteral) {
                if(((DekiScriptLiteral)test).IsNilFalseZero) {
                    result = expr.Right.VisitWith(this, new DekiScriptOptimizerState(mode, env));
                } else {
                    result = expr.Left.VisitWith(this, new DekiScriptOptimizerState(mode, env));
                }
            } else {
                result = new DekiScriptTernary(expr.Line, expr.Column, test, expr.Left.VisitWith(this, new DekiScriptOptimizerState(mode, env)), expr.Right.VisitWith(this, new DekiScriptOptimizerState(mode, env)), expr.IsIfElse);
            }
            return result;
        }

        public DekiScriptExpression Visit(DekiScriptUnary expr, DekiScriptOptimizerState state) {
            DekiScriptExpression value = expr.Value.VisitWith(this, state);
            DekiScriptExpression result = new DekiScriptUnary(expr.Line, expr.Column, expr.OpCode, value);
            if(value is DekiScriptLiteral) {
                return result.Evaluate(state.Env);
            }
            return result;
        }

        public DekiScriptExpression Visit(DekiScriptUnknown expr, DekiScriptOptimizerState state) {
            return expr;
        }

        public DekiScriptExpression Visit(DekiScriptUri expr, DekiScriptOptimizerState state) {
            return expr;
        }

        public DekiScriptExpression Visit(DekiScriptVar expr, DekiScriptOptimizerState state) {
            if(expr.Name.EqualsInvariant(DekiScriptRuntime.ENV_ID)) {
                return expr;
            }

            // check if variable exists
            DekiScriptLiteral result;
            if(!state.Env.Locals.TryGetValue(expr.Name, out result) && !state.Env.Globals.TryGetValue(expr.Name, out result)) {
                return expr;
            }

            // check if variable is defined, but the value is unknown
            if(result is DekiScriptUnknown) {
                return expr;
            }
            return result;
        }

        public DekiScriptExpression Visit(DekiScriptXml expr, DekiScriptOptimizerState state) {
            return expr;
        }

        public DekiScriptExpression Visit(DekiScriptXmlConstructor expr, DekiScriptOptimizerState state) {
            var mode = state.Mode;
            var env = state.Env;
            DekiScriptDom value = expr.Value.VisitWith(DekiScriptDomOptimizer.Instance, state);
            if(!value.IsDynamic) {

                // we can simply take the result to an expression
                return new DekiScriptXml(DekiScriptDomEvaluation.Instance.Evaluate(value, mode, true, env));
            }
            return new DekiScriptXmlConstructor(value);
        }

        private object Optimize(DekiScriptCaseBlock expr, DekiScriptExpression value, DekiScriptEvalMode mode, DekiScriptEnv env, out bool isDefaultCase) {
            List<DekiScriptExpression> conditions = new List<DekiScriptExpression>();
            isDefaultCase = false;
            for(int i = 0; i < expr.Conditions.Length; i++) {
                if(expr.Conditions[i] != null) {
                    DekiScriptExpression condition = expr.Conditions[i].VisitWith(this, new DekiScriptOptimizerState(mode, env));

                    // check if condition always succeeds or always fails
                    if((value is DekiScriptLiteral) && (condition is DekiScriptLiteral)) {
                        DekiScriptBinary test = new DekiScriptBinary(0, 0, DekiScriptBinary.Op.Equal, value, condition);
                        if(!test.Evaluate(env).IsNilFalseZero) {

                            // NOTE (steveb): we wrap the outcome into a sequence to ensure proper handling of break/continue statements

                            // condition succeeded, return it
                            return DekiScriptSequence.New(DekiScriptSequence.ScopeKind.ScopeCatchBreakAndContinue, expr.Body.VisitWith(this, new DekiScriptOptimizerState(mode, env)));
                        }
                    } else {
                        conditions.Add(condition);
                    }
                } else {
                    isDefaultCase = true;
                    conditions.Add(null);
                }
            }

            // check if any conditions were true or unknown
            if(conditions.Count == 0) {
                return null;
            }
            DekiScriptExpression body = expr.Body.VisitWith(this, new DekiScriptOptimizerState(mode, env));
            return new DekiScriptCaseBlock(conditions.ToArray(), body, expr.IsBlock);
        }
    }
}

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
using MindTouch.Deki.Script.Expr;
using MindTouch.Deki.Script.Runtime.Library;
using MindTouch.Deki.Script.Runtime.TargetInvocation;

namespace MindTouch.Deki.Script.Compiler {
    internal class DekiScriptExpressionOptimizer : IDekiScriptExpressionVisitor<DekiScriptExpressionEvaluationState, DekiScriptExpression> {

        //--- Class Methods ---
        public static readonly DekiScriptExpressionOptimizer Instance = new DekiScriptExpressionOptimizer();

        //--- Constructors ---
        private DekiScriptExpressionOptimizer() { }

        //--- Methods ---
        public DekiScriptExpression Visit(DekiScriptAbort expr, DekiScriptExpressionEvaluationState state) {
            return expr;
        }

        public DekiScriptExpression Visit(DekiScriptAccess expr, DekiScriptExpressionEvaluationState state) {
            DekiScriptExpression prefix = expr.Prefix.VisitWith(this, state);
            DekiScriptExpression index = expr.Index.VisitWith(this, state);
            DekiScriptAccess access = (DekiScriptAccess)DekiScriptExpression.Access(expr.Location, prefix, index);
            if((prefix is DekiScriptLiteral) && (index is DekiScriptLiteral)) {
                DekiScriptLiteral result = DekiScriptExpressionEvaluation.Instance.Evaluate(access, state, false);

                // check if result is a property
                if(DekiScriptRuntime.IsProperty(result)) {

                    // retrieve property information
                    var descriptor = state.Runtime.ResolveRegisteredFunctionUri(((DekiScriptUri)result).Value);
                    if((descriptor != null) && descriptor.IsIdempotent) {

                        // evaluate property, since it never changes
                        return state.Runtime.EvaluateProperty(expr.Location, result, state.Env);
                    }
                    return DekiScriptExpression.Call(expr.Location, result, new DekiScriptList());
                }
                return result;
            }
            return access;
        }

        public DekiScriptExpression Visit(DekiScriptAssign expr, DekiScriptExpressionEvaluationState state) {
            DekiScriptExpression result = expr.Value.VisitWith(this, state);
            DekiScriptLiteral value = (result is DekiScriptLiteral) ? (DekiScriptLiteral)result : DekiScriptUnknown.Value;
            if(expr.Define) {
                state.Env.Vars.Add(expr.Variable, value);
            } else {
                state.Env.Vars[expr.Variable] = value;
            }
            if(result is DekiScriptLiteral) {

                // NOTE: variable was assigned a value; it will be substituted into subsequent operations

                return DekiScriptNil.Value;
            }
            if(expr.Define) {
                return DekiScriptExpression.VarStatement(expr.Location, DekiScriptExpression.Id(expr.Location, expr.Variable), result);
            }
            return DekiScriptExpression.LetStatement(expr.Location, DekiScriptExpression.Id(expr.Location, expr.Variable), result);
        }

        public DekiScriptExpression Visit(DekiScriptBinary expr, DekiScriptExpressionEvaluationState state) {
            DekiScriptExpression left = expr.Left.VisitWith(this, state);
            DekiScriptExpression right = expr.Right.VisitWith(this, state);
            DekiScriptExpression result = DekiScriptExpression.BinaryOp(expr.Location, expr.OpCode, left, right);
            if((left is DekiScriptLiteral) && (right is DekiScriptLiteral)) {
                result = state.Pop(result.VisitWith(DekiScriptExpressionEvaluation.Instance, state));
            }
            return result;
        }

        public DekiScriptExpression Visit(DekiScriptReturnScope expr, DekiScriptExpressionEvaluationState state) {
            return DekiScriptExpression.ReturnScope(expr.Location, expr.Value.VisitWith(this, state));
        }

        public DekiScriptExpression Visit(DekiScriptBool expr, DekiScriptExpressionEvaluationState state) {
            return expr;
        }

        public DekiScriptExpression Visit(DekiScriptCall expr, DekiScriptExpressionEvaluationState state) {
            DekiScriptExpression prefix = expr.Prefix.VisitWith(this, state);
            DekiScriptExpression arguments = expr.Arguments.VisitWith(this, state);
            DekiScriptExpression result = expr.IsCurryOperation ? DekiScriptExpression.Curry(expr.Location, prefix, arguments) : DekiScriptExpression.Call(expr.Location, prefix, arguments);

            // check if prefix has been resolved to a uri and the arguments have all been resolved
            if((prefix is DekiScriptUri) && (arguments is DekiScriptLiteral)) {

                // check if the uri can be resolved to a native idempotent function
                var descriptor = state.Runtime.ResolveRegisteredFunctionUri(((DekiScriptUri)result).Value);
                if((descriptor != null) && descriptor.IsIdempotent) {

                    // evaluate function, since it never changes
                    return state.Pop(DekiScriptExpression.Call(expr.Location, prefix, arguments).VisitWith(DekiScriptExpressionEvaluation.Instance, state));
                }
            }
            return result;
        }

        public DekiScriptExpression Visit(DekiScriptDiscard expr, DekiScriptExpressionEvaluationState state) {

            // TODO (steveb): missing partial evaluation rule
            return expr;
        }

        public DekiScriptExpression Visit(DekiScriptForeach expr, DekiScriptExpressionEvaluationState state) {

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
            //        return new DekiScriptForeach(Line, Column, Var, collection, Body);
            //    }

            //    // loop over collection
            //    List<DekiScriptExpression> expressions = new List<DekiScriptExpression>();
            //    int index = 0;
            //    foreach(DekiScriptLiteral value in list) {
            //        DekiScriptEnv subEnv = env.NewLocalScope();

            //        // set the environment variable
            //        subEnv.Vars.Add(Var, value);
            //        subEnv.Vars.Add(DekiScriptRuntime.COUNT_ID, DekiScriptExpression.Constant(index));
            //        subEnv.Vars.Add(DekiScriptRuntime.INDEX_ID, DekiScriptExpression.Constant(index));

            //        // NOTE (steveb): we wrap the outcome into a sequence to ensure proper handling of break/continue statements

            //        expressions.Add(DekiScriptSequence.New(DekiScriptSequence.ScopeKind.ScopeCatchContinue, value.Optimize(mode, subEnv)));
            //        ++index;
            //    }
            //    return DekiScriptSequence.New(DekiScriptSequence.ScopeKind.ScopeCatchBreakAndContinue, expressions.ToArray());
            //} else {
            //    DekiScriptEnv subEnv = env.NewLocalScope();

            //    // set the environment variable to unknown
            //    subEnv.Vars.Add(Var, DekiScriptUnknown.Value);
            //    subEnv.Vars.Add(DekiScriptRuntime.COUNT_ID, DekiScriptUnknown.Value);
            //    subEnv.Vars.Add(DekiScriptRuntime.INDEX_ID, DekiScriptUnknown.Value);

            //    // partially evaluate the inner loop
            //    DekiScriptExpression block = Body.Optimize(mode, subEnv);
            //    return new DekiScriptForeach(Line, Column, Var, collection, block);
            //}
        }

        public DekiScriptExpression Visit(DekiScriptList expr, DekiScriptExpressionEvaluationState state) {
            return expr;
        }

        public DekiScriptExpression Visit(DekiScriptListConstructor expr, DekiScriptExpressionEvaluationState state) {

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
                return DekiScriptExpression.List(expr.Location, list);
            }

            // convert expression to a list
            DekiScriptList result = new DekiScriptList();
            foreach(DekiScriptLiteral literal in list) {
                result.Add(literal);
            }
            return result;
        }

        public DekiScriptExpression Visit(DekiScriptMagicId expr, DekiScriptExpressionEvaluationState state) {
            return expr;
        }

        public DekiScriptExpression Visit(DekiScriptMap expr, DekiScriptExpressionEvaluationState state) {
            return expr;
        }

        public DekiScriptExpression Visit(DekiScriptMapConstructor expr, DekiScriptExpressionEvaluationState state) {

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
                fields[i] = new DekiScriptMapConstructor.FieldConstructor(current.Location, key, value);
                isLiteral = isLiteral && (key is DekiScriptLiteral) && (value is DekiScriptLiteral);
            }
            if(!isLiteral) {
                return DekiScriptExpression.Map(expr.Location, fields);
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

        public DekiScriptExpression Visit(DekiScriptNil expr, DekiScriptExpressionEvaluationState state) {
            return expr;
        }

        public DekiScriptExpression Visit(DekiScriptNumber expr, DekiScriptExpressionEvaluationState state) {
            return expr;
        }

        public DekiScriptExpression Visit(DekiScriptReturn expr, DekiScriptExpressionEvaluationState state) {
            return DekiScriptExpression.ReturnStatement(expr.Location, expr.Value.VisitWith(this, state));
        }

        public DekiScriptExpression Visit(DekiScriptSequence expr, DekiScriptExpressionEvaluationState state) {
            return expr;
#if false
            var mode = state.Mode;
            var env = state.Env;
            if(expr.Kind != DekiScriptSequence.ScopeKind.None) {
                env = env.NewLocalScope();
            }
            bool safe = env.IsSafeMode;

            // loop over all expressions and accumulate as many as possible
            var accumulator = new DekiScriptOutputBuffer();
            List<DekiScriptExpression> list = new List<DekiScriptExpression>(expr.List.Length);
            foreach(DekiScriptExpression expression in expr.List) {
                DekiScriptExpression value = expression.VisitWith(this, new DekiScriptExpressionEvaluationState(mode, env));

                // check if we can continue to accumulate the values
                if((accumulator != null) && value is DekiScriptLiteral) {
                    accumulator.Append((DekiScriptLiteral)value);
                } else {

                    // check if the accumulator contains a value to add to the list
                    if(accumulator != null) {
                        list.Add(accumulator.GetResult(safe));
                        accumulator = null;
                    }

                    // check if value is worthile keeping
                    if(!(value is DekiScriptNil)) {
                        list.Add(value);
                    }
                }
            }
            if(accumulator != null) {
                return accumulator.GetResult(safe);
            }
            return DekiScriptSequence.New(expr.Kind, list.ToArray());
#endif
        }

        public DekiScriptExpression Visit(DekiScriptString expr, DekiScriptExpressionEvaluationState state) {
            return expr;
        }

        public DekiScriptExpression Visit(DekiScriptSwitch expr, DekiScriptExpressionEvaluationState state) {
            DekiScriptExpression value = expr.Value.VisitWith(this, state);
            List<DekiScriptSwitch.CaseBlock> cases = new List<DekiScriptSwitch.CaseBlock>();
            int defaultCaseIndex = -1;
            for(int i = 0; i < expr.Cases.Length; ++i) {
                bool isDefaultCase;
                object outcome = Optimize(expr.Cases[i], value, state, out isDefaultCase);
                if(outcome is DekiScriptExpression) {
                    return (DekiScriptExpression)outcome;
                }
                if(outcome != null) {
                    cases.Add((DekiScriptSwitch.CaseBlock)outcome);
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
                return DekiScriptExpression.SwitchStatement(expr.Location, value, cases.ToArray());
            }
            return DekiScriptNil.Value;
        }

        public DekiScriptExpression Visit(DekiScriptTernary expr, DekiScriptExpressionEvaluationState state) {
            DekiScriptExpression test = expr.Test.VisitWith(this, state);
            DekiScriptExpression result;

            // check which branch should be executed
            if(test is DekiScriptLiteral) {
                if(((DekiScriptLiteral)test).IsNilFalseZero) {
                    result = expr.Right.VisitWith(this, state);
                } else {
                    result = expr.Left.VisitWith(this, state);
                }
            } else {
                var left = expr.Left.VisitWith(this, state);
                var right = expr.Right.VisitWith(this, state);
                result = expr.IsIfElse ? DekiScriptExpression.IfElseStatement(expr.Location, test, left, right) : DekiScriptExpression.TernaryOp(expr.Location, test, left, right);
            }
            return result;
        }

        public DekiScriptExpression Visit(DekiScriptTryCatchFinally expr, DekiScriptExpressionEvaluationState state) {
        	
        	// TODO (steveb)
            return expr;
        }

        public DekiScriptExpression Visit(DekiScriptUnary expr, DekiScriptExpressionEvaluationState state) {
            DekiScriptExpression value = expr.Value.VisitWith(this, state);
            DekiScriptExpression result = DekiScriptExpression.UnaryOp(expr.Location, expr.OpCode, value);
            if(value is DekiScriptLiteral) {
                return state.Pop(result.VisitWith(DekiScriptExpressionEvaluation.Instance, state));
            }
            return result;
        }

        public DekiScriptExpression Visit(DekiScriptUnknown expr, DekiScriptExpressionEvaluationState state) {
            return expr;
        }

        public DekiScriptExpression Visit(DekiScriptUri expr, DekiScriptExpressionEvaluationState state) {
            return expr;
        }

        public DekiScriptExpression Visit(DekiScriptVar expr, DekiScriptExpressionEvaluationState state) {
            if(expr.Name.EqualsInvariant(DekiScriptRuntime.ENV_ID)) {
                return expr;
            }

            // check if variable exists
            DekiScriptLiteral result;
            if(!state.Env.Vars.TryGetValue(expr.Name, out result) && !state.Env.Vars.TryGetValue(expr.Name, out result)) {
                return expr;
            }

            // check if variable is defined, but the value is unknown
            if(result is DekiScriptUnknown) {
                return expr;
            }
            return result;
        }

        public DekiScriptExpression Visit(DekiScriptXml expr, DekiScriptExpressionEvaluationState state) {
            return expr;
        }

        public DekiScriptExpression Visit(DekiScriptXmlElement expr, DekiScriptExpressionEvaluationState state) {

            // TODO (steveb): missing code
            return expr;
        }

        private object Optimize(DekiScriptSwitch.CaseBlock expr, DekiScriptExpression value, DekiScriptExpressionEvaluationState state, out bool isDefaultCase) {
            List<DekiScriptExpression> conditions = new List<DekiScriptExpression>();
            isDefaultCase = false;
            for(int i = 0; i < expr.Conditions.Length; i++) {
                if(expr.Conditions[i] != null) {
                    DekiScriptExpression condition = expr.Conditions[i].VisitWith(this, state);

                    // check if condition always succeeds or always fails
                    if((value is DekiScriptLiteral) && (condition is DekiScriptLiteral)) {
                        var test = DekiScriptExpression.BinaryOp(Location.None, DekiScriptBinary.Op.Equal, value, condition);
                        var result = state.Pop(test.VisitWith(DekiScriptExpressionEvaluation.Instance, state));
                        if(!result.IsNilFalseZero) {

                            // NOTE (steveb): we wrap the outcome into a sequence to ensure proper handling of break/continue statements

                            // condition succeeded, return it
                            return DekiScriptExpression.Block(expr.Location, new[] { expr.Body.VisitWith(this, state) });
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
            DekiScriptExpression body = expr.Body.VisitWith(this, state);
            return new DekiScriptSwitch.CaseBlock(expr.Location, conditions.ToArray(), body);
        }
    }
}
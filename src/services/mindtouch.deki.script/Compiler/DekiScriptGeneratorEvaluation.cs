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
using MindTouch.Deki.Script.Runtime;
using MindTouch.Xml;

namespace MindTouch.Deki.Script.Compiler {
    internal delegate void DekiScriptEval(DekiScriptEnv env);

    internal struct DekiScriptGeneratorEvaluationState {

        //--- Fields ---
        public readonly DekiScriptEval Callback;
        public readonly DekiScriptExpressionEvaluationState State;

        //--- Constructors ---
        public DekiScriptGeneratorEvaluationState(DekiScriptEval callback, DekiScriptExpressionEvaluationState state) {
            this.Callback = callback;
            this.State = state;
        }

        //--- Methods ---
        public void ThrowIfTimedout() {
            State.ThrowIfTimedout();
        }
    }

    internal class DekiScriptGeneratorEvaluation : IDekiScriptGeneratorVisitor<DekiScriptGeneratorEvaluationState, Empty> {

        //--- Class Fields ---
        public static readonly DekiScriptGeneratorEvaluation Instance = new DekiScriptGeneratorEvaluation();

        //--- Class Methods ---
        public static void Generate(DekiScriptGenerator expr, DekiScriptEval callback, DekiScriptExpressionEvaluationState state) {

            // set counter variable
            int counter = 0;
            var previousCounter = state.Env.Vars[DekiScriptRuntime.COUNT_ID];
            try {
                state.Env.Vars.Add(DekiScriptRuntime.COUNT_ID, DekiScriptExpression.Constant(counter));
                expr.VisitWith(Instance, new DekiScriptGeneratorEvaluationState(innerEnv => {
                    callback(innerEnv);
                    state.Env.Vars.Add(DekiScriptRuntime.COUNT_ID, DekiScriptExpression.Constant(++counter));
                }, state));
            } finally {
                state.Env.Vars.Add(DekiScriptRuntime.COUNT_ID, previousCounter);
            }
        }

        private static DekiScriptLiteral Eval(DekiScriptExpression expr, DekiScriptGeneratorEvaluationState state) {
            state.ThrowIfTimedout();
            int marker = state.State.Buffer.Marker;
            try {
                return state.State.Pop(expr.VisitWith(DekiScriptExpressionEvaluation.Instance, state.State));
            } finally {
                state.State.Buffer.Reset(marker);
            }
        }

        //--- Constructors ---
        private DekiScriptGeneratorEvaluation() { }

        //--- Methods ---
        public Empty Visit(DekiScriptGeneratorIf expr, DekiScriptGeneratorEvaluationState state) {

            // evaluate the generator condition
            DekiScriptLiteral condition = Eval(expr.Condition, state);

            // check if condition is met
            if(!condition.IsNilFalseZero) {
                EvalNext(expr, state);
            }
            return Empty.Value;
        }

        public Empty Visit(DekiScriptGeneratorVar expr, DekiScriptGeneratorEvaluationState state) {

            // store previous state of variable
            var previousVar = state.State.Env.Vars[expr.Var];
            try {

                // initialize the variable
                state.State.Env.Vars.Add(expr.Var, Eval(expr.Expression, state));

                // generate values
                EvalNext(expr, state);
            } finally {

                // restore the variable
                state.State.Env.Vars.Add(expr.Var, previousVar);
            }
            return Empty.Value;
        }

        public Empty Visit(DekiScriptGeneratorForeachValue expr, DekiScriptGeneratorEvaluationState state) {
            DekiScriptLiteral collection = Eval(expr.Collection, state);

            // retrieve collection
            List<DekiScriptLiteral> list;
            if(collection is DekiScriptList) {

                // loop over list values
                list = ((DekiScriptList)collection).Value;
            } else if(collection is DekiScriptMap) {

                // loop over map key-value pairs
                list = new List<DekiScriptLiteral>(((DekiScriptMap)collection).Value.Values);
            } else if(collection is DekiScriptXml) {

                // loop over xml selection
                List<XDoc> selection = ((DekiScriptXml)collection).Value.ToList();
                list = new List<DekiScriptLiteral>(selection.Count);
                foreach(XDoc doc in selection) {
                    list.Add(new DekiScriptXml(doc));
                }
            } else if(collection is DekiScriptNil) {

                // nothing to do
                list = new List<DekiScriptLiteral>();
            } else {
                throw new DekiScriptBadTypeException(expr.Location, collection.ScriptType, new[] { DekiScriptType.LIST, DekiScriptType.MAP, DekiScriptType.XML, DekiScriptType.NIL });
            }

            // store state of variables
            int index = 0;
            var previousVars = new DekiScriptLiteral[expr.Vars.Length];
            for(int j = 0; j < expr.Vars.Length; ++j) {
                previousVars[j] = state.State.Env.Vars[expr.Vars[j]];
            }
            var previousIndex = state.State.Env.Vars[DekiScriptRuntime.INDEX_ID];
            try {

                // loop over collection
                for(int i = 0; i <= (list.Count - expr.Vars.Length); i += expr.Vars.Length) {

                    // set the environment variable
                    for(int j = 0; j < expr.Vars.Length; ++j) {
                        state.State.Env.Vars.Add(expr.Vars[j], list[i + j]);
                    }
                    state.State.Env.Vars.Add(DekiScriptRuntime.INDEX_ID, DekiScriptExpression.Constant(index));

                    // iterate over block statements
                    EvalNext(expr, state);
                    index += expr.Vars.Length;
                }
            } finally {

                // restore state of variables
                for(int j = 0; j < expr.Vars.Length; ++j) {
                    state.State.Env.Vars.Add(expr.Vars[j], previousVars[j]);
                }
                state.State.Env.Vars.Add(DekiScriptRuntime.INDEX_ID, previousIndex);
            }
            return Empty.Value;
        }

        public Empty Visit(DekiScriptGeneratorForeachKeyValue expr, DekiScriptGeneratorEvaluationState state) {
            DekiScriptLiteral collection = Eval(expr.Collection, state);

            // retrieve collection
            Dictionary<string, DekiScriptLiteral> map;
            if(collection is DekiScriptMap) {

                // loop over map key-value pairs
                map = ((DekiScriptMap)collection).Value;
            } else if(collection is DekiScriptNil) {

                // nothing to do
                map = new Dictionary<string, DekiScriptLiteral>();
            } else {
                throw new DekiScriptBadTypeException(expr.Location, collection.ScriptType, new[] { DekiScriptType.MAP, DekiScriptType.NIL });
            }

            // store state of variables
            var previousKey = state.State.Env.Vars[expr.Key];
            var previousValue = state.State.Env.Vars[expr.Value];
            var previousIndex = state.State.Env.Vars[DekiScriptRuntime.INDEX_ID];
            try {

                // loop over collection
                int index = 0;
                foreach(KeyValuePair<string, DekiScriptLiteral> entry in map) {

                    // set the environment variable
                    state.State.Env.Vars.Add(expr.Key, DekiScriptExpression.Constant(entry.Key));
                    state.State.Env.Vars.Add(expr.Value, entry.Value);
                    state.State.Env.Vars.Add(DekiScriptRuntime.INDEX_ID, DekiScriptExpression.Constant(index));

                    // iterate over block statements
                    EvalNext(expr, state);
                    ++index;
                }
            } finally {
                state.State.Env.Vars.Add(expr.Key, previousKey);
                state.State.Env.Vars.Add(expr.Value, previousValue);
                state.State.Env.Vars.Add(DekiScriptRuntime.INDEX_ID, previousIndex);
            }
            return Empty.Value;
        }

        private void EvalNext(DekiScriptGenerator expr, DekiScriptGeneratorEvaluationState state) {
            DekiScriptEnv env = state.State.Env;

            // check if there is a chained generator
            if(expr.Next != null) {
                expr.Next.VisitWith(this, state);
            } else {

                // call delegate
                state.Callback(env);
            }
        }
    }
}
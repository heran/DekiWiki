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

using System.Collections.Generic;
using MindTouch.Xml;

namespace MindTouch.Deki.Script.Interpreter {
    internal delegate void DekiScriptEval(DekiScriptEnv env);

    internal struct DekiScriptGeneratorEvaluationState {

        //--- Fields ---
        public readonly DekiScriptEval Callback;
        public readonly DekiScriptEnv Env;

        //--- Constructors ---
        public DekiScriptGeneratorEvaluationState(DekiScriptEval callback, DekiScriptEnv env) {
            this.Callback = callback;
            this.Env = env;
        }
    }

    internal class DekiScriptGeneratorEvaluation : IDekiScriptGeneratorVisitor<DekiScriptGeneratorEvaluationState, Empty> {

        //--- Class Fields ---
        public static readonly DekiScriptGeneratorEvaluation Instance = new DekiScriptGeneratorEvaluation();

        //--- Constructors ---
        private DekiScriptGeneratorEvaluation() { }

        //--- Methods ---
        public void Generate(DekiScriptGenerator expr, DekiScriptEval callback, DekiScriptEnv env) {
            expr.VisitWith(this, new DekiScriptGeneratorEvaluationState(callback, env));
        }

        public Empty Visit(DekiScriptGeneratorIf expr, DekiScriptGeneratorEvaluationState state) {
            DekiScriptLiteral condition = expr.Condition.VisitWith(DekiScriptExpressionEvaluation.Instance, state.Env);
            if(!condition.IsNilFalseZero) {
                Generate(expr, state);
            }
            return Empty.Value;
        }

        public Empty Visit(DekiScriptGeneratorVar expr, DekiScriptGeneratorEvaluationState state) {
            state.Env.Locals.Add(expr.Var, expr.Expression.VisitWith(DekiScriptExpressionEvaluation.Instance, state.Env));
            Generate(expr, state);
            return Empty.Value;
        }

        public Empty Visit(DekiScriptGeneratorForeachValue expr, DekiScriptGeneratorEvaluationState state) {
            DekiScriptLiteral collection = expr.Collection.VisitWith(DekiScriptExpressionEvaluation.Instance, state.Env);

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
                throw new DekiScriptBadTypeException(expr.Line, expr.Column, collection.ScriptType, new DekiScriptType[] { DekiScriptType.LIST, DekiScriptType.MAP, DekiScriptType.XML, DekiScriptType.NIL });
            }

            // loop over collection
            int index = 0;
            for(int i = 0; i <= (list.Count - expr.Vars.Length); i += expr.Vars.Length) {

                // set the environment variable
                for(int j = 0; j < expr.Vars.Length; ++j) {
                    state.Env.Locals.Add(expr.Vars[j], list[i + j]);
                }
                state.Env.Locals.Add(DekiScriptRuntime.INDEX_ID, DekiScriptNumber.New(index));

                // iterate over block statements
                Generate(expr, state);
                index += expr.Vars.Length;
            }
            return Empty.Value;
        }

        public Empty Visit(DekiScriptGeneratorForeachKeyValue expr, DekiScriptGeneratorEvaluationState state) {
            DekiScriptLiteral collection = expr.Collection.VisitWith(DekiScriptExpressionEvaluation.Instance, state.Env);

            // retrieve collection
            Dictionary<string, DekiScriptLiteral> map;
            if(collection is DekiScriptMap) {

                // loop over map key-value pairs
                map = ((DekiScriptMap)collection).Value;
            } else if(collection is DekiScriptNil) {

                // nothing to do
                map = new Dictionary<string, DekiScriptLiteral>();
            } else {
                throw new DekiScriptBadTypeException(expr.Line, expr.Column, collection.ScriptType, new DekiScriptType[] { DekiScriptType.MAP, DekiScriptType.NIL });
            }

            // loop over collection
            int index = 0;
            foreach(KeyValuePair<string, DekiScriptLiteral> entry in map) {

                // set the environment variable
                state.Env.Locals.Add(expr.Key, DekiScriptString.New(entry.Key));
                state.Env.Locals.Add(expr.Value, entry.Value);
                state.Env.Locals.Add(DekiScriptRuntime.INDEX_ID, DekiScriptNumber.New(index));

                // iterate over block statements
                Generate(expr, state);
                ++index;
            }
            return Empty.Value;
        }

        private void Generate(DekiScriptGenerator expr, DekiScriptGeneratorEvaluationState state) {
            DekiScriptEnv env = state.Env;

            // check if __count variable is defined
            DekiScriptLiteral count;
            if(!env.Locals.TryGetValue(DekiScriptRuntime.COUNT_ID, out count) || !(count is DekiScriptNumber)) {
                count = DekiScriptNumber.New(0);
                env.Locals.Add(DekiScriptRuntime.COUNT_ID, count);
            }

            // check if there is a chained generator
            if(expr.Next != null) {
                expr.Next.VisitWith(this, state);
            } else {

                // call delegate
                state.Callback(env.NewLocalScope());

                // increase __count variable
                env.Locals.Add(DekiScriptRuntime.COUNT_ID, DekiScriptNumber.New(((DekiScriptNumber)count).Value + 1));
            }
        }
    }
}
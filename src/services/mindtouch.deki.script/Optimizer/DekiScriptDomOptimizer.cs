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

namespace MindTouch.Deki.Script.Optimizer {
    internal struct DekiScriptOptimizerState {
        
        //--- Fields ---
        public readonly DekiScriptEvalMode Mode;
        public readonly DekiScriptEnv Env;

        //--- Constructors ---
        public DekiScriptOptimizerState(DekiScriptEvalMode mode, DekiScriptEnv env) {
            this.Mode = mode;
            this.Env = env;
        }
    }

    internal class DekiScriptDomOptimizer : IDekiScriptDomVisitor<DekiScriptOptimizerState, DekiScriptDom> {

        //--- Class Methods ---
        public static  readonly DekiScriptDomOptimizer Instance = new DekiScriptDomOptimizer();

        //--- Constructors ---
        private DekiScriptDomOptimizer() { }

        //--- Methods ---
        public DekiScriptDom Visit(DekiScriptDomBlock expr, DekiScriptOptimizerState state) {
            state = new DekiScriptOptimizerState(state.Mode, state.Env.NewLocalScope());
            DekiScriptExpression value = expr.Value.VisitWith(DekiScriptExpressionOptimizer.Instance, state);
            DekiScriptDom node = expr.Node.VisitWith(this, state);
            return new DekiScriptDomBlock(expr.Location, value, node);
        }

        public DekiScriptDom Visit(DekiScriptDomCData expr, DekiScriptOptimizerState state) {
            DekiScriptExpression value = expr.Value.VisitWith(DekiScriptExpressionOptimizer.Instance, state);
            return new DekiScriptDomCData(expr.Location, value);
        }

        public DekiScriptDom Visit(DekiScriptDomComment expr, DekiScriptOptimizerState state) {
            DekiScriptExpression value = expr.Value.VisitWith(DekiScriptExpressionOptimizer.Instance, state);
            return new DekiScriptDomComment(expr.Location, value);
        }

        public DekiScriptDom Visit(DekiScriptDomElement expr, DekiScriptOptimizerState state) {
            DekiScriptDomElement.Attribute[] attributes = new DekiScriptDomElement.Attribute[expr.Attributes.Length];
            for(int i = 0; i < expr.Attributes.Length; i++) {
                attributes[i] = Optimize(expr.Attributes[i], state);
            }
            DekiScriptDom node = expr.Node.VisitWith(this, state);
            return new DekiScriptDomElement(expr.Location, expr.Prefix, expr.Name, attributes, node);
        }

        public DekiScriptDom Visit(DekiScriptDomExpr expr, DekiScriptOptimizerState state) {
            DekiScriptExpression value = expr.Value.VisitWith(DekiScriptExpressionOptimizer.Instance, state);

            // TODO: check if expression is literal

            return new DekiScriptDomExpr(expr.Location, value);
        }

        public DekiScriptDom Visit(DekiScriptDomForeach expr, DekiScriptOptimizerState state) {

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

            //        // loop over map values
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
            //        throw new DekiScriptBadTypeException(0, 0, ((DekiScriptLiteral)collection).ScriptType, new DekiScriptType[] { DekiScriptType.LIST, DekiScriptType.MAP, DekiScriptType.XML, DekiScriptType.NIL });
            //    }

            //    // loop over collection
            //    int index = 0;
            //    int count = 0;
            //    List<DekiScriptDom> nodes = new List<DekiScriptDom>();
            //    foreach(DekiScriptLiteral value in list) {

            //        // set the environment variable
            //        DekiScriptEnv locals = env.NewLocalScope();
            //        locals.Locals.Add(Variable, value);
            //        locals.Locals.Add(DekiScriptRuntime.COUNT_ID, DekiScriptNumber.New(count));
            //        locals.Locals.Add(DekiScriptRuntime.INDEX_ID, DekiScriptNumber.New(index));

            //        // check if we should skip this item
            //        ++index;
            //        DekiScriptExpression where = null;
            //        if(Where != null) {
            //            where = Where.Optimize(mode, locals);
            //            if((where is DekiScriptLiteral) && ((DekiScriptLiteral)where).IsNilFalseZero) {
            //                continue;
            //            }
            //        }
            //        ++count;

            //        // evaluate nodes
            //        DekiScriptDom node = (where != null) ? new DekiScriptDomIfElse(Location, new Tuplet<DekiScriptExpression, DekiScriptDom>(where, Node)) : Node;
            //        nodes.Add(node.Optimize(mode, locals));
            //    }
            //    if(nodes.Count == 1) {
            //        return nodes[0];
            //    }
            //    return DekiScriptDomSequence.New(Location, nodes);
            //} else {

            //    // setup locals with unknown values
            //    DekiScriptEnv locals = env.NewLocalScope();
            //    locals.Locals.Add(Variable, DekiScriptUnknown.Value);
            //    locals.Locals.Add(DekiScriptRuntime.COUNT_ID, DekiScriptUnknown.Value);
            //    locals.Locals.Add(DekiScriptRuntime.INDEX_ID, DekiScriptUnknown.Value);

            //    // partially evaluate inner loop
            //    DekiScriptExpression where = (Where != null) ? Where.Optimize(mode, locals) : null;
            //    DekiScriptDom node = Node.Optimize(mode, locals);
            //    return new DekiScriptDomForeach(Location, Variable, collection, where, node);
            //}
        }

        public DekiScriptDom Visit(DekiScriptDomIfElse expr, DekiScriptOptimizerState state) {
            List<Tuplet<DekiScriptExpression, DekiScriptDom>> conditionals = new List<Tuplet<DekiScriptExpression, DekiScriptDom>>();
            for(int i = 0; i < expr.Conditionals.Length; i++) {
                Tuplet<DekiScriptExpression, DekiScriptDom> conditional = expr.Conditionals[i];

                // evaluate current branch
                DekiScriptExpression inner = conditional.Item1.VisitWith(DekiScriptExpressionOptimizer.Instance, state);
                DekiScriptDom node = conditional.Item2.VisitWith(this, state);

                // check if test has a constant outcome
                if(inner is DekiScriptLiteral) {
                    if(((DekiScriptLiteral)inner).IsNilFalseZero) {

                        // NOTE (steveb): this conditional will never be successful; skip it

                    } else {

                        // NOTE (steveb): this conditional will is always successful; make it the "else" branch

                        // check if there are any previous branches
                        if(conditionals.Count == 0) {

                            // just return the inner node structure
                            return node;
                        }

                        // add branch as final "else" branch
                        conditionals.Add(new Tuplet<DekiScriptExpression, DekiScriptDom>(null, node));
                        break;
                    }
                } else {
                    conditionals.Add(new Tuplet<DekiScriptExpression, DekiScriptDom>(inner, node));
                }
            }
            return new DekiScriptDomIfElse(expr.Location, conditionals.ToArray());
        }

        public DekiScriptDom Visit(DekiScriptDomJson expr, DekiScriptOptimizerState state) {
            DekiScriptExpression value = expr.Value.VisitWith(DekiScriptExpressionOptimizer.Instance, state);
            if(value is DekiScriptLiteral) {
                return new DekiScriptDomText(expr.Location, DekiScriptLibrary.JsonEmit(((DekiScriptLiteral)value).NativeValue));
            }
            return new DekiScriptDomJson(expr.Location, value);
        }

        public DekiScriptDom Visit(DekiScriptDomSequence expr, DekiScriptOptimizerState state) {
            DekiScriptDom[] nodes = new DekiScriptDom[expr.Nodes.Length];
            for(int i = 0; i < expr.Nodes.Length; i++) {
                nodes[i] = expr.Nodes[i].VisitWith(this, state);
            }
            return DekiScriptDomSequence.New(expr.Location, nodes);
        }

        public DekiScriptDom Visit(DekiScriptDomText expr, DekiScriptOptimizerState state) {
            return expr;
        }

        private DekiScriptDomElement.Attribute Optimize(DekiScriptDomElement.Attribute expr, DekiScriptOptimizerState state) {
            DekiScriptExpression value = expr.Value.VisitWith(DekiScriptExpressionOptimizer.Instance, state);
            return new DekiScriptDomElement.Attribute(expr.Location, expr.Prefix, expr.Name, value);
        }
    }
}

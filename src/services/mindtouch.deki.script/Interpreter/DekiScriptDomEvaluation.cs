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
using System.Xml;
using MindTouch.Deki.JavaScript;
using MindTouch.Deki.Script.Dom;
using MindTouch.Dream;
using MindTouch.Xml;

namespace MindTouch.Deki.Script.Interpreter {
    internal struct DekiScriptDomEvaluationState {
        
        //--- Fields ---
        public DekiScriptEvalContext Context;
        public XmlNode Parent;
        public DekiScriptEnv Env;

        //--- Constructor ---
        public DekiScriptDomEvaluationState(DekiScriptEvalContext context, XmlNode parent, DekiScriptEnv env) {
            this.Context = context;
            this.Env = env;
            this.Parent = parent;
        }
    }

    internal class DekiScriptDomEvaluation : IDekiScriptDomVisitor<DekiScriptDomEvaluationState, Empty> {

        //--- Class Fields ---
        public static readonly DekiScriptDomEvaluation Instance = new DekiScriptDomEvaluation();

        //--- Constructors ---
        private DekiScriptDomEvaluation() { }

        //--- Methods ---
        public Empty Visit(DekiScriptDomBlock expr, DekiScriptDomEvaluationState state) {
            var context = state.Context;
            var env = state.Env;
            var parent = state.Parent;
            try {
                DekiScriptEnv locals = env.NewLocalScope();

                // check if we need to evaluate the block 'Value'
                if(expr.IsDynamic) {
                    expr.Value.VisitWith(DekiScriptExpressionEvaluation.Instance, locals);
                }
                expr.Node.VisitWith(this, new DekiScriptDomEvaluationState(context, parent, locals));
            } catch(Exception e) {
                EmbedExceptionMessage(expr, env, context, e, parent);
            }
            return Empty.Value;
        }

        public Empty Visit(DekiScriptDomCData expr, DekiScriptDomEvaluationState state) {
            var context = state.Context;
            var env = state.Env;
            var parent = state.Parent;
            try {
                DekiScriptLiteral value = expr.Value.VisitWith(DekiScriptExpressionEvaluation.Instance, env);
                if(!value.IsNil) {
                    XmlNode result = context.CreateCDataSection(value.AsString());
                    parent.AppendChild(result);
                }
            } catch(Exception e) {
                EmbedExceptionMessage(expr, env, context, e, parent);
            }
            return Empty.Value;
        }

        public Empty Visit(DekiScriptDomComment expr, DekiScriptDomEvaluationState state) {
            var context = state.Context;
            var env = state.Env;
            var parent = state.Parent;
            try {
                DekiScriptLiteral value = expr.Value.VisitWith(DekiScriptExpressionEvaluation.Instance, env);
                if(!value.IsNil) {
                    XmlNode result = context.CreateComment(value.AsString());
                    parent.AppendChild(result);
                }
            } catch(Exception e) {
                EmbedExceptionMessage(expr, env, context, e, parent);
            }
            return Empty.Value;
        }

        public Empty Visit(DekiScriptDomElement expr, DekiScriptDomEvaluationState state) {
            var context = state.Context;
            var env = state.Env;
            var parent = state.Parent;
            env = env.NewLocalScope();

            // check if any namespaces are being defined
            try {
                foreach(DekiScriptDomElement.Attribute attribute in expr.Attributes) {
                    EvaluateNamespaceDefinitionAttribute(attribute, context, env);
                }

                // create element
                XmlElement result = context.CreateElement(expr.Prefix, XmlConvert.EncodeLocalName(expr.Name.VisitWith(DekiScriptExpressionEvaluation.Instance, env).AsString()), context.Namespaces.LookupNamespace(expr.Prefix));
                parent.AppendChild(result);

                // add attributes
                foreach(DekiScriptDomElement.Attribute attribute in expr.Attributes) {
                    EvaluateAttribute(attribute, context, result, env);
                }

                // process deki-javascript
                DekiJavaScriptProcessor.ProcessDekiJavaScriptConstructor(context, result, env);

                // add elements
                expr.Node.VisitWith(this, new DekiScriptDomEvaluationState(context, result, env));
            } catch(Exception e) {
                EmbedExceptionMessage(expr, env, context, e, parent);
            }
            return Empty.Value;
        }

        public Empty Visit(DekiScriptDomExpr expr, DekiScriptDomEvaluationState state) {
            var context = state.Context;
            var env = state.Env;
            var parent = state.Parent;
            try {
                DekiScriptLiteral value = expr.Value.VisitWith(DekiScriptExpressionEvaluation.Instance, env.NewLocalScope());
                context.InsertValueBeforeNode(parent, null, value);
            } catch(Exception e) {
                EmbedExceptionMessage(expr, env, context, e, parent);
            }
            return Empty.Value;
        }

        public Empty Visit(DekiScriptDomForeach expr, DekiScriptDomEvaluationState state) {
            var context = state.Context;
            var env = state.Env;
            var parent = state.Parent;
            try {
                DekiScriptGeneratorEvaluation.Instance.Generate(expr.Generator, delegate(DekiScriptEnv subEnv) {

                    // evaluate nodes
                    expr.Node.VisitWith(this, new DekiScriptDomEvaluationState(context, parent, subEnv));
                }, env.NewLocalScope());
            } catch(Exception e) {
                EmbedExceptionMessage(expr, env, context, e, parent);
            }
            return Empty.Value;
        }

        public Empty Visit(DekiScriptDomIfElse expr, DekiScriptDomEvaluationState state) {
            var context = state.Context;
            var env = state.Env;
            var parent = state.Parent;
            try {
                foreach(Tuplet<DekiScriptExpression, DekiScriptDom> conditional in expr.Conditionals) {

                    // check if conditional is false
                    if((conditional.Item1 != null) && conditional.Item1.VisitWith(DekiScriptExpressionEvaluation.Instance, env.NewLocalScope()).IsNilFalseZero) {
                        continue;
                    }

                    // embed conditional branch
                    conditional.Item2.VisitWith(this, new DekiScriptDomEvaluationState(context, parent, env));
                    break;
                }
            } catch(Exception e) {
                EmbedExceptionMessage(expr, env, context, e, parent);
            }
            return Empty.Value;
        }

        public Empty Visit(DekiScriptDomJson expr, DekiScriptDomEvaluationState state) {
            var context = state.Context;
            var env = state.Env;
            var parent = state.Parent;
            try {
                DekiScriptLiteral value = expr.Value.VisitWith(DekiScriptExpressionEvaluation.Instance, env);
                XmlText result = context.CreateTextNode(DekiScriptLibrary.JsonEmit(value.NativeValue));
                parent.AppendChild(result);
            } catch(Exception e) {
                EmbedExceptionMessage(expr, env, context, e, parent);
            }
            return Empty.Value;
        }

        public Empty Visit(DekiScriptDomSequence expr, DekiScriptDomEvaluationState state) {
            var context = state.Context;
            var env = state.Env;
            var parent = state.Parent;
            foreach(DekiScriptDom node in expr.Nodes) {
                node.VisitWith(this, new DekiScriptDomEvaluationState(context, parent, env));
            }
            return Empty.Value;
        }

        public Empty Visit(DekiScriptDomText expr, DekiScriptDomEvaluationState state) {
            var context = state.Context;
            var env = state.Env;
            var parent = state.Parent;
            try {
                XmlNode result = context.CreateTextNode(expr.Value);
                parent.AppendChild(result);
            } catch(Exception e) {
                EmbedExceptionMessage(expr, env, context, e, parent);
            }
            return Empty.Value;
        }

        public XDoc Evaluate(DekiScriptDom expr, DekiScriptEvalMode mode, bool fallthrough, DekiScriptEnv env) {
            DekiScriptEvalContext context = new DekiScriptEvalContext(mode, fallthrough);
            try {
                expr.VisitWith(this, new DekiScriptDomEvaluationState(context, context.Document, env));
            } catch(DekiScriptDocumentTooLargeException) {

                // this exception is thrown to unwind the DOM stack; we can safely ignore it
            }
            context.MergeContextIntoDocument(context.Document);
            return new XDoc(context.Document);
        }

        private void EvaluateAttribute(DekiScriptDomElement.Attribute expr, DekiScriptEvalContext context, XmlElement element, DekiScriptEnv env) {
            if(!expr.IsNamespaceDefinition) {
                try {
                    string name = expr.Name.VisitWith(DekiScriptExpressionEvaluation.Instance, env).AsString();
                    if(name != null) {
                        name = XmlConvert.EncodeLocalName(name);
                        string value = expr.Value.VisitWith(DekiScriptExpressionEvaluation.Instance, env).AsString();
                        if(value != null) {
                            element.SetAttribute(name, context.Namespaces.LookupNamespace(expr.Prefix), value);
                        }
                    }
                } catch(Exception e) {
                    context.InsertExceptionMessageBeforeNode(env, element, null, expr.Location, e);
                }
            }
        }

        private void EvaluateNamespaceDefinitionAttribute(DekiScriptDomElement.Attribute expr, DekiScriptEvalContext context, DekiScriptEnv env) {
            if(expr.IsNamespaceDefinition) {
                DekiScriptLiteral name = expr.Name.VisitWith(DekiScriptExpressionEvaluation.Instance, env);
                DekiScriptLiteral value = expr.Value.VisitWith(DekiScriptExpressionEvaluation.Instance, env);
                if(!value.IsNil) {
                    if(string.IsNullOrEmpty(expr.Prefix)) {
                        context.Namespaces.AddNamespace(string.Empty, value.AsString());
                    } else {
                        context.Namespaces.AddNamespace(XmlConvert.EncodeLocalName(name.AsString() ?? string.Empty), value.AsString());
                    }
                }
            }
        }

        private void EmbedExceptionMessage(DekiScriptDom expr, DekiScriptEnv env, DekiScriptEvalContext context, Exception exception, XmlNode parent) {
            if(exception is DreamRequestFatalException) {
                exception.Rethrow();
            }
            context.InsertExceptionMessageBeforeNode(env, parent, null, expr.Location, exception);
        }
    }
}

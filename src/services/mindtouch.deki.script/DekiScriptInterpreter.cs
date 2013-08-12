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
using System.Xml;
using MindTouch.Deki.Script.Compiler;
using MindTouch.Deki.Script.Expr;
using MindTouch.Deki.Script.Runtime;
using MindTouch.Deki.Script.Runtime.Library;
using MindTouch.Xml;

namespace MindTouch.Deki.Script {
    public static class DekiScriptInterpreterEx {
        
        //--- Extension Methods ---
        public static DekiScriptExpression Optimize(this DekiScriptRuntime runtime, DekiScriptExpression expr, DekiScriptEvalMode mode, DekiScriptEnv env) {
            return expr.VisitWith(DekiScriptExpressionOptimizer.Instance, new DekiScriptExpressionEvaluationState(mode, env, runtime, TimeSpan.MaxValue, int.MaxValue));
        }
    }

    public static class DekiScriptInterpreter {

        //--- Class Methods ---
        public static XmlNode Evaluate(
            XDoc script,
            XmlElement node,
            DekiScriptEvalContext context,
            DekiScriptEnv env,
            DekiScriptRuntime runtime,
            out bool scripted,
            ref bool error
        ) {
            if((context.Mode != DekiScriptEvalMode.Verify) && (context.Mode != DekiScriptEvalMode.EvaluateEditOnly) && (context.Mode != DekiScriptEvalMode.EvaluateSaveOnly)) {
                throw new InvalidOperationException("DekiScript interpreter can only used for save, edit, or verify evaluations.");
            }
            scripted = false;
            XmlNode next = node.NextSibling;
            try {

                // check if element needs to be evaluated
                if(StringUtil.EqualsInvariant(node.NamespaceURI, XDekiScript.ScriptNS)) {
                    scripted = true;

                    // NOTE (steveb): <eval:xyz> nodes are not processed by the interpreter anymore

                } else {
                    XDoc current = script[node];
                    bool hasScriptClass = StringUtil.EqualsInvariant(node.GetAttribute("class"), "script");

                    #region <elem class="script" init="..." if="..." foreach="..." where="..." block="...">
                    // check if element has form <elem class="script" init="..." if="..." foreach="..." where="..." block="...">
                    scripted = node.HasAttribute("init") || node.HasAttribute("if") || node.HasAttribute("foreach") || node.HasAttribute("block");
                    if(context.Mode == DekiScriptEvalMode.Verify) {

                        // check if "block" is present
                        string blockAttr = node.GetAttribute("block");
                        if(!string.IsNullOrEmpty(blockAttr)) {

                            // TODO (steveb): validate script expression

                        }

                        // check if "foreach" is present
                        string foreachAttr = node.GetAttribute("foreach");
                        if(!string.IsNullOrEmpty(foreachAttr)) {

                            // TODO (steveb): validate script expression

                        }

                        // check if "if" is present
                        string ifAttr = node.GetAttribute("if");
                        if(!string.IsNullOrEmpty(ifAttr)) {

                            // TODO (steveb): validate script expression

                        }

                        // check if "init" is present
                        string initAttr = node.GetAttribute("init");
                        if(!string.IsNullOrEmpty(initAttr)) {

                            // TODO (steveb): validate script expression

                        }
                    }
                    #endregion

                    // evaluate child nodes
                    EvaluateChildren(script, node, context, env, runtime, out scripted, ref error);

                    #region evaluate attributes
                    for(int i = 0; i < node.Attributes.Count; ++i) {
                        XmlAttribute attribute = node.Attributes[i];

                        // check if attribute needs to be evaluated
                        if(attribute.NamespaceURI == XDekiScript.ScriptNS) {
                            scripted = true;

                            // NOTE (steveb): eval:xyz="abc" attributes are not processed by the interpreter anymore

                        } else if(StringUtil.StartsWithInvariant(attribute.Value, "{{") && StringUtil.EndsWithInvariant(attribute.Value, "}}")) {
                            scripted = true;

                            // NOTE (steveb): key="{{value}}"
                            string code = attribute.Value.Substring(2, attribute.Value.Length - 4).Trim();

                            // check if script content is substituted
                            bool isPermanentReplacement = false;
                            if(StringUtil.StartsWithInvariantIgnoreCase(code, DekiScriptRuntime.ON_SAVE_PATTERN)) {
                                isPermanentReplacement = (context.Mode == DekiScriptEvalMode.EvaluateSaveOnly);
                                code = code.Substring(DekiScriptRuntime.ON_SAVE_PATTERN.Length);
                            } else if(StringUtil.StartsWithInvariantIgnoreCase(code, DekiScriptRuntime.ON_SUBST_PATTERN)) {
                                isPermanentReplacement = (context.Mode == DekiScriptEvalMode.EvaluateSaveOnly);
                                code = code.Substring(DekiScriptRuntime.ON_SUBST_PATTERN.Length);
                            } else if(StringUtil.StartsWithInvariantIgnoreCase(code, DekiScriptRuntime.ON_EDIT_PATTERN)) {
                                isPermanentReplacement = (context.Mode == DekiScriptEvalMode.EvaluateEditOnly);
                                code = code.Substring(DekiScriptRuntime.ON_EDIT_PATTERN.Length);
                            }

                            // parse expression
                            if((context.Mode == DekiScriptEvalMode.Verify) || isPermanentReplacement) {
                                DekiScriptExpression expression = DekiScriptParser.Parse(ComputeNodeLocation(attribute), code);
                                if(isPermanentReplacement) {
                                    DekiScriptLiteral eval = runtime.Evaluate(expression, DekiScriptEvalMode.EvaluateSafeMode, env);

                                    // determine what the outcome value is
                                    string value = eval.AsString();

                                    // check if we have a value to replace the current attribute with
                                    if((value != null) && !DekiScriptLibrary.ContainsXSSVulnerability(attribute.LocalName, value)) {
                                        attribute.Value = value;
                                    } else {
                                        node.Attributes.RemoveAt(i);
                                        --i;
                                    }
                                }
                            }
                        }
                    }
                    #endregion

                    #region evaluate <span class="script"> or <pre class="script">
                    if(hasScriptClass && (StringUtil.EqualsInvariant(node.LocalName, "pre") || StringUtil.EqualsInvariant(node.LocalName, "span")) && !node.HasAttribute("function")) {

                        // replace the non-breaking space character with space
                        string code = node.InnerText.ReplaceAll("\u00A0", " ", "\u00AD", "").Trim();

                        // check if script content is substituted
                        bool isPermanentReplacement = false;
                        if(StringUtil.StartsWithInvariantIgnoreCase(code, DekiScriptRuntime.ON_SAVE_PATTERN)) {
                            isPermanentReplacement = (context.Mode == DekiScriptEvalMode.EvaluateSaveOnly);
                            code = code.Substring(DekiScriptRuntime.ON_SAVE_PATTERN.Length);
                        } else if(StringUtil.StartsWithInvariantIgnoreCase(code, DekiScriptRuntime.ON_SUBST_PATTERN)) {
                            isPermanentReplacement = (context.Mode == DekiScriptEvalMode.EvaluateSaveOnly);
                            code = code.Substring(DekiScriptRuntime.ON_SUBST_PATTERN.Length);
                        } else if(StringUtil.StartsWithInvariantIgnoreCase(code, DekiScriptRuntime.ON_EDIT_PATTERN)) {
                            isPermanentReplacement = (context.Mode == DekiScriptEvalMode.EvaluateEditOnly);
                            code = code.Substring(DekiScriptRuntime.ON_EDIT_PATTERN.Length);
                        }

                        // parse expression
                        if((context.Mode == DekiScriptEvalMode.Verify) || isPermanentReplacement) {
                            DekiScriptExpression expression = DekiScriptParser.Parse(ComputeNodeLocation(node), code);
                            if(isPermanentReplacement) {
                                DekiScriptLiteral value = runtime.Evaluate(expression, DekiScriptEvalMode.EvaluateSafeMode, env);
                                context.ReplaceNodeWithValue(node, value);
                            }
                            if(!isPermanentReplacement) {
                                scripted = true;
                            }
                        }
                    }
                    #endregion
                }
            } catch(Exception e) {

                // only embed error in verify mode, not in save/edit modes
                if(context.Mode == DekiScriptEvalMode.Verify) {
                    context.InsertExceptionMessageBeforeNode(env, node.ParentNode, node, ComputeNodeLocation(node), e);
                    node.ParentNode.RemoveChild(node);
                }
                error |= true;
            }
            return next;
        }

        private static void EvaluateChildren(
            XDoc script,
            XmlElement node,
            DekiScriptEvalContext context,
            DekiScriptEnv env,
            DekiScriptRuntime runtime,
            out bool scripted,
            ref bool error
        ) {
            scripted = false;

            // recurse first to evaluate nested script content
            XmlNode child = node.FirstChild;
            while(child != null) {
                XmlNode next = child.NextSibling;
                if(child.NodeType == XmlNodeType.Element) {
                    bool childScripted;
                    next = Evaluate(script, (XmlElement)child, context, env, runtime, out childScripted, ref error);
                    scripted = scripted || childScripted;
                }
                child = next;
            }
        }

        private static Location ComputeNodeLocation(XmlNode node) {

            // compute node location
            List<string> pathSegments = new List<string>();
            while((node != null) && !StringUtil.EqualsInvariant(node.Name, "body")) {
                if(node.NodeType == XmlNodeType.Attribute) {
                    pathSegments.Add("@" + node.Name);
                    node = ((XmlAttribute)node).OwnerElement;
                } else {

                    // count how many nodes have the same name
                    int index = 1;
                    XmlNode sibling = node.PreviousSibling;
                    while(sibling != null) {
                        if(StringUtil.EqualsInvariant(sibling.Name, node.Name)) {
                            ++index;
                        }
                        sibling = sibling.PreviousSibling;
                    }

                    // add path segment with or without index
                    if(index > 1) {
                        pathSegments.Add(node.Name + "[" + index + "]");
                    } else {
                        pathSegments.Add(node.Name);
                    }
                    node = node.ParentNode;
                }
            }
            pathSegments.Reverse();
            return new Location(string.Join("/", pathSegments.ToArray()));
        }
    }
}

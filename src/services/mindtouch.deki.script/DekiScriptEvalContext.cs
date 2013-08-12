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
using MindTouch.Deki.Script.Expr;
using MindTouch.Deki.Script.Runtime;
using MindTouch.Xml;

namespace MindTouch.Deki.Script {
    public class DekiScriptEvalContext {

        //--- Fields ---
        public readonly DekiScriptEvalMode Mode;
        public readonly XmlDocument Document;
        public readonly Dictionary<string, object> HeadLookup = new Dictionary<string, object>();
        public readonly List<XmlNode> HeadItems = new List<XmlNode>();
        public readonly Dictionary<string, object> TailLookup = new Dictionary<string, object>();
        public readonly List<XmlNode> TailItems = new List<XmlNode>();
        public readonly Dictionary<string, List<XmlNode>> Bodies = new Dictionary<string, List<XmlNode>>();
        public readonly XmlNamespaceManager Namespaces = new XmlNamespaceManager(XDoc.XmlNameTable);
        public readonly bool Fallthrough;
        private readonly int MaxNodeCount;
        private int _nodeCount;

        //--- Constructors ---
        public DekiScriptEvalContext(XDoc document, DekiScriptEvalMode mode, bool fallthrough, int maxNodeCount) {
            this.Document = document.AsXmlNode.OwnerDocument;
            this.Mode = mode;
            this.Fallthrough = fallthrough;
            this.MaxNodeCount = maxNodeCount;

            // verify that document is below the limit
            _nodeCount = 2*document["body//*"].ListLength + document["body//text()"].ListLength;
            if(_nodeCount >= this.MaxNodeCount) {
                throw new DekiScriptDocumentTooLargeException(MaxNodeCount);
            }
        }

        //--- Methods ---
        public void MergeContextIntoDocument(XmlDocument document) {
            if(document == null) {
                throw new ArgumentNullException("document");
            }
            XmlElement root = document.DocumentElement;
            if(root == null) {
                throw new ArgumentNullException("document", "document is missing root element");
            }

            // check if we have to reorganize the document into an HTML document
            if(((HeadItems.Count > 0) || (TailItems.Count > 0) || (Bodies.Count > 0)) && !StringUtil.EqualsInvariant(root.LocalName, "html") && !StringUtil.EqualsInvariant(root.LocalName, "content")) {
                XmlElement html = document.CreateElement("html");
                XmlElement body = document.CreateElement("body");
                document.RemoveChild(root);
                body.AppendChild(root);
                html.AppendChild(body);
                document.AppendChild(html);
                root = document.DocumentElement;
            }

            // add head elements
            if(HeadItems.Count > 0) {
                XmlElement head = root["head"];
                if(head == null) {
                    head = document.CreateElement("head");
                    root.AppendChild(head);
                }
                foreach(XmlNode item in HeadItems) {
                    head.AppendChild(document.ImportNode(item, true));
                }
            }

            // add targetted bodies
            foreach(KeyValuePair<string, List<XmlNode>> target in Bodies) {
                XmlElement body = document.CreateElement("body");
                root.AppendChild(body);
                body.SetAttribute("target", target.Key);
                foreach(XmlNode item in target.Value) {
                    foreach(XmlNode child in item.ChildNodes) {
                        body.AppendChild(document.ImportNode(child, true));
                    }
                }
            }

            // add tail elements
            if(TailItems.Count > 0) {
                XmlElement tail = root["tail"];
                if(tail == null) {
                    tail = document.CreateElement("tail");
                    root.AppendChild(tail);
                }
                foreach(XmlNode item in TailItems) {
                    tail.AppendChild(document.ImportNode(item, true));
                }
            }
        }

        public void AddHeadElements(XDoc result) {
            XDoc resultHead = result["head"];
            if(!resultHead.IsEmpty) {
                foreach(XmlNode node in resultHead.AsXmlNode.ChildNodes) {

                    // NOTE (steveb): we only allow <!-- comments !-->, <script>, <link>, <meta name="...">, and <style> elements in the head section

                    if((node is XmlComment) ||
                        ((node is XmlElement) && (
                            node.Name.EqualsInvariant("script") ||
                            node.Name.EqualsInvariant("link") ||
                            node.Name.EqualsInvariant("meta") ||
                            node.Name.EqualsInvariant("style"))
                        )
                    ) {
                        string xml = node.OuterXml;
                        if(!HeadLookup.ContainsKey(xml)) {
                            HeadLookup.Add(xml, null);
                            HeadItems.Add(node);
                        }
                    }
                }
            }
        }

        public void AddTailElements(XDoc result) {
            XDoc resultTail = result["tail"];
            if(!resultTail.IsEmpty) {
                foreach(XmlNode node in resultTail.AsXmlNode.ChildNodes) {

                    // NOTE (steveb): we only allow <!-- comments !--> and <script> elements in the tail section

                    if((node is XmlComment) || ((node is XmlElement) && StringUtil.EqualsInvariant(node.Name, "script"))) {

                        // TODO (steveb): we're not properly loading <script src="..." /> elements

                        string xml = node.OuterXml;
                        if(!TailLookup.ContainsKey(xml)) {
                            TailLookup.Add(xml, null);
                            TailItems.Add(node);
                        }
                    }
                }
            }
        }

        internal void ReplaceNodeWithValue(XmlNode node, DekiScriptLiteral value) {
            XmlNode parent = node.ParentNode;
            InsertValueBeforeNode(parent, node, value);
            parent.RemoveChild(node);
        }

        internal void InsertValueBeforeNode(XmlNode parent, XmlNode reference, DekiScriptLiteral value) {
            if((value is DekiScriptXml) || (value is DekiScriptUri)) {
                XDoc xml = value.AsEmbeddableXml(Mode == DekiScriptEvalMode.EvaluateSafeMode);
                if(xml.HasName("html")) {

                    // TODO (steveb): merge XML namespaces

                    // merge <head> and <tail> sections
                    AddHeadElements(xml);
                    AddTailElements(xml);

                    // loop over body elements in response
                    foreach(XDoc body in xml["body"]) {
                        string target = body["@target"].AsText;
                        string conflict = body["@conflict"].AsText ?? "ignore";

                        // check if the main body is targeted or something else
                        if(string.IsNullOrEmpty(target)) {

                            // append body nodes
                            foreach(XmlNode node in body.AsXmlNode.ChildNodes) {
                                parent.InsertBefore(parent.OwnerDocument.ImportNode(node, true), reference);
                            }
                        } else {

                            // check if the targeted body already exists
                            if(Bodies.ContainsKey(target) && !StringUtil.EqualsInvariant(conflict, "replace")) {
                                if(StringUtil.EqualsInvariant(conflict, "append")) {

                                    // append nodes to existing body
                                    Bodies[target].Add(body.AsXmlNode);
                                }
                            } else {

                                // create a new body element
                                List<XmlNode> list = new List<XmlNode>();
                                list.Add(body.AsXmlNode);
                                Bodies[target] = list;
                            }
                        }
                    }
                } else if(!xml.IsEmpty) {

                    // replace the current node with the entire document
                    parent.InsertBefore(parent.OwnerDocument.ImportNode(xml.AsXmlNode, true), reference);
                }
            } else if(value is DekiScriptComplexLiteral) {

                // append text respresentation of value
                parent.InsertBefore(CreateTextNode(value.ToString()), reference);
            } else {

                // append value cast to text
                string text = value.AsString();
                if(!string.IsNullOrEmpty(text)) {
                    parent.InsertBefore(CreateTextNode(text), reference);
                }
            }
        }

        internal void InsertExceptionMessageBeforeNode(DekiScriptEnv env, XmlNode parent, XmlNode reference, Location location, Exception exception) {
            if(Fallthrough) {
                throw exception;
            }
            if(exception is DekiScriptDocumentTooLargeException) {
                DekiScriptDocumentTooLargeException e = (DekiScriptDocumentTooLargeException)exception;

                // check if an error message was already embedded
                if(e.Handled) {
                    throw exception;
                }
                e.Handled = true;
            }

            // check if environment has a __callstack variable
            DekiScriptList callstack = null;
            DekiScriptLiteral callstackVar;
            if(env.Vars.TryGetValue(DekiScriptEnv.CALLSTACK, out callstackVar)) {
                callstack = callstackVar as DekiScriptList;
            }

            XDoc warning = DekiScriptRuntime.CreateWarningFromException(callstack, location, exception);
            parent.InsertBefore(parent.OwnerDocument.ImportNode(warning.AsXmlNode, true), reference);
            if(exception is DekiScriptDocumentTooLargeException) {
                throw exception;
            }
        }

        internal XmlText CreateTextNode(string text) {
            if(++_nodeCount > MaxNodeCount) {
                throw new DekiScriptDocumentTooLargeException(MaxNodeCount);
            }
            return Document.CreateTextNode(text);
        }

        internal XmlElement CreateElement(string prefix, string localName, string namespaceUri) {

            // NOTE (steveb): we count XML elements as two nodes because there is an opening and closing tag
            _nodeCount += 2;
            if(_nodeCount > MaxNodeCount) {
                throw new DekiScriptDocumentTooLargeException(MaxNodeCount);
            }
            return Document.CreateElement(prefix, localName, namespaceUri);
        }
    }
}

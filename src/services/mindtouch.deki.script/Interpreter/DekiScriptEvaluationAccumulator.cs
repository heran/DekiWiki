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
using System.Text;
using System.Xml;

using MindTouch.Xml;

namespace MindTouch.Deki.Script.Interpreter {
    internal class DekiScriptEvaluationAccumulator {

        //--- Fields ---
        private object _value;

        //--- Fields ---
        public DekiScriptLiteral Value {
            get {
                if(_value == null) {
                    return DekiScriptNil.Value;
                }
                if(_value is DekiScriptLiteral) {
                    return (DekiScriptLiteral)_value;
                }
                if(_value is StringBuilder) {
                    return DekiScriptString.New(_value.ToString());
                }
                return new DekiScriptXml((XDoc)_value);
            }
        }

        //--- Methods ---
        public DekiScriptEvaluationAccumulator Add(DekiScriptLiteral literal, bool safe) {
            if(literal == null) {
                throw new ArgumentNullException("literal");
            }
            if(literal is DekiScriptNil) {

                // nothing to do
                return this;
            }

            // check if any value was accumulated
            if(_value == null) {
                if(literal is DekiScriptXml) {
                    _value = ((DekiScriptXml)literal).Value.Clone();
                } else {
                    _value = literal;
                }
                return this;
            }

            // check if we can append a string value
            if(literal is DekiScriptString) {
                AddString(((DekiScriptString)literal).Value, safe);
                return this;
            }
            if(!(literal is DekiScriptUri) && !(literal is DekiScriptXml)) {
                AddString(literal.ToString(), safe);
                return this;
            }

            // check if we need to append an XML document
            XDoc doc = literal.AsEmbeddableXml(safe);
            if(doc.IsEmpty) {
                return this;
            }
            XDoc accumulator = ConvertToXml(safe);

            // build lookup for existing bodies in accumulator
            Dictionary<string, XDoc> bodies = new Dictionary<string, XDoc>();
            foreach(XmlNode node in accumulator.AsXmlNode.ChildNodes) {
                if(node.NodeType == XmlNodeType.Element) {
                    XmlElement element = (XmlElement)node;
                    if(StringUtil.EqualsInvariant(node.LocalName, "body")) {
                        string target = element.GetAttribute("target");
                        bodies[target] = accumulator[node];
                    }
                }
            }

            // loop over all root children in new document
            foreach(XmlNode node in doc.AsXmlNode.ChildNodes) {
                if(node.NodeType == XmlNodeType.Element) {
                    XmlElement element = (XmlElement)node;
                    if(StringUtil.EqualsInvariant(node.LocalName, "body")) {

                        string target = element.GetAttribute("target");
                        XDoc body;
                        if(bodies.TryGetValue(target, out body)) {

                            // body already exists, check how it should be handled
                            string conflict = element.GetAttribute("conflict");
                            if(string.IsNullOrEmpty(conflict)) {

                                // default conflict resolution depends on target: no target (i.e. main body) is append, otherwise it is ignore
                                conflict = string.IsNullOrEmpty(target) ? "append" : "ignore";
                            }
                            switch(conflict) {
                            case "replace":

                                // replace existing body with new one
                                body.RemoveNodes();
                                body.AddNodes(doc[node]);
                                break;
                            case "append":

                                // append nodes to existing body
                                body.AddNodes(doc[node]);
                                break;
                            case "ignore":

                                // ignore new body
                                break;
                            }
                        } else {

                            // target body does not exist, append it
                            accumulator.Start("body");
                            if(!string.IsNullOrEmpty(target)) {
                                accumulator.Attr("target", target);
                            }
                            accumulator.AddNodes(doc[node]);
                            accumulator.End();
                        }
                    } else if(StringUtil.EqualsInvariant(node.LocalName, "head")) {
                        XDoc head = accumulator["head"];
                        foreach(XmlNode child in node.ChildNodes) {
                            head.Add(doc[child]);
                        }
                    } else if(StringUtil.EqualsInvariant(node.LocalName, "tail")) {
                        XDoc head = accumulator["tail"];
                        foreach(XmlNode child in node.ChildNodes) {
                            head.Add(doc[child]);
                        }
                    }
                }
            }
            return this;
        }

        private void AddString(string value, bool safe) {

            // check if value is a literal already; if so, change its type to something more string-accumulation friendly
            if(_value is DekiScriptLiteral) {
                if(_value is DekiScriptString) {

                    // use the native value of the string literal
                    _value = new StringBuilder().Append(((DekiScriptString)_value).Value);
                } else if((_value is DekiScriptUri) || (_value is DekiScriptXml)) {

                    // convert XML/URI literal into an XML document
                    XDoc doc = ((DekiScriptLiteral)_value).AsEmbeddableXml(safe);
                    if(doc["head"].IsEmpty) {
                        doc.Elem("head");
                    }
                    if(doc["tail"].IsEmpty) {
                        doc.Elem("tail");
                    }
                    _value = doc;
                } else {

                    // convert literal to string
                    _value = new StringBuilder().Append(_value.ToString());
                }
            }

            // check state of accumulator
            if(_value is XDoc) {
                XDoc doc = (XDoc)_value;

                // check if XML document needs to be converted to an HTML document
                if(!doc.HasName("html")) {
                    doc = new XDoc("html").Start("body").Add(doc).Value(value).End();
                } else {

                    // check if a main body already exists
                    XDoc body = doc["body[not(@target)]"];
                    if(body.IsEmpty) {

                        // create main body and append string to contents of XML document
                        doc.Start("body").Value(value).End();
                    } else {

                        // append string to contents of XML document
                        body.Value(value);
                    }
                }

                _value = doc;
            } else if(_value is StringBuilder) {

                // append string to string builder
                ((StringBuilder)_value).Append(value);
            }
        }

        private XDoc ConvertToXml(bool safe) {
            if(!(_value is XDoc)) {
                if(_value is DekiScriptString) {

                    // use the native value of the string literal
                    _value = new XDoc("html").Elem("head").Elem("body", ((DekiScriptString)_value).Value).Elem("tail");
                } else if(_value != null) {

                    // use contents of string builder or convert literal to string
                    _value = new XDoc("html").Elem("head").Elem("body", _value.ToString()).Elem("tail");
                } else {

                    // create an empty XML document
                    _value = new XDoc("html").Elem("head").Elem("body").Elem("tail");
                }
            } else {

                // make sure the document has <head> and <tail> elements
                XDoc doc = (XDoc)_value;

                // check if xml has the right form
                if(!doc.HasName("html")) {
                    doc = new XDoc("html").Start("body").Add(doc).End();
                    if(safe) {
                        DekiScriptLibrary.ValidateXHtml(doc["body"], true, true);
                    }
                    _value = doc;
                }

                // check if all the necessary elements are in place
                bool foundHead = false;
                bool foundBody = false;
                bool foundTail = false;
                foreach(XmlNode node in doc.AsXmlNode.ChildNodes) {
                    if(node.NodeType == XmlNodeType.Element) {
                        foundHead = foundHead || StringUtil.EqualsInvariant(node.LocalName, "head");
                        foundBody = foundBody || StringUtil.EqualsInvariant(node.LocalName, "body") && string.IsNullOrEmpty(((XmlElement)node).GetAttribute("target"));
                        foundTail = foundTail || StringUtil.EqualsInvariant(node.LocalName, "tail");
                    }
                    if(foundHead && foundTail && foundBody) {
                        break;
                    }
                }
                if(!foundHead) {
                    doc.Elem("head");
                }
                if(!foundBody) {
                    doc.Elem("body");
                }
                if(!foundTail) {
                    doc.Elem("tail");
                }
            }
            return (XDoc)_value;
        }
    }
}
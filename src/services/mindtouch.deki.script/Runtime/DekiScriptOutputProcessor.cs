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
using System.Linq;
using System.Text;
using System.Xml;
using MindTouch.Deki.Script.Expr;
using MindTouch.Deki.Script.Runtime.Library;
using MindTouch.Dream;
using MindTouch.Xml;

namespace MindTouch.Deki.Script.Runtime {
    internal class DekiScriptOutputProcessor {

        //--- Constants ---
        private const string XMLNS = "xmlns";

        //--- Types ---
        internal enum State {
            EMPTY,
            VALUE,
            COMPOSITE,
            XML,
            HTML
        }

        //--- Class Methods ---
        private static void AppendXmlStart(DekiScriptOutputBuffer buffer, XmlNode element) {
            string prefix = element.Prefix;
            string name = element.LocalName;
            Dictionary<string, string> namespaces = null;
            List<Tuplet<string, string, string>> attributes = null;

            // loop over all attributes
            foreach(XmlAttribute attribute in element.Attributes) {
                string attrPrefix = attribute.Prefix;
                string attrName = attribute.LocalName;
                string attrValue = attribute.Value;
                bool isNamespaceDeclaration = string.IsNullOrEmpty(attrPrefix) ? attrName.EqualsInvariant(XMLNS) : attrPrefix.EqualsInvariant(XMLNS);

                // check if attribute is a namespace declaration
                if(isNamespaceDeclaration) {

                    // add attribute to namespace declarations
                    namespaces = namespaces ?? new Dictionary<string, string>();

                    // check if the default namespace is being defined
                    if(string.IsNullOrEmpty(attrPrefix)) {
                        namespaces.Add(string.Empty, attrValue);
                    } else {
                        namespaces.Add(attrName, attrValue);
                    }
                } else {

                    // add attribute to list of attributes
                    attributes = attributes ?? new List<Tuplet<string, string, string>>();
                    attributes.Add(new Tuplet<string, string, string>(attrPrefix, attrName, attrValue));
                }
            }
            buffer.PushXmlStart(prefix, name, namespaces, attributes);
        }

        private static void SkipNode(DekiScriptOutputBuffer buffer, ref int index) {
            int nesting = 0;
            while(true) {
                var current = buffer[index];
                if(current is DekiScriptOutputBuffer.XmlStart) {
                    ++nesting;
                } else if(current is DekiScriptOutputBuffer.XmlEnd) {
                    if(--nesting < 0) {
                        throw new ShouldNeverHappenException();
                    }
                }
                if(nesting == 0) {
                    break;
                }
                ++index;
            }
        }

        private static void RemoveDuplicateElements(XmlNode context) {
            var dups = new HashSet<string>();
            XmlNode child = context.FirstChild;
            while(child != null) {
                var next = child.NextSibling;

                // check if an identical node had already been found
                if(!dups.Add(child.OuterXml)) {

                    // remove this node
                    context.RemoveChild(child);
                }
                child = next;
            }
        }

        //--- Fields ---
        private State _state = State.EMPTY;
        private XmlDocument _document;
        private XmlAttribute _attribute;
        private XmlNode _html;
        private XmlNode _head;
        private XmlNode _body;
        private XmlNode _tail;
        private Dictionary<string, XmlNode> _bodies;
        private object _value;

        //--- Constructors ---
        public DekiScriptLiteral Process(DekiScriptOutputBuffer buffer, int marker, bool safe) {
            ParseBuffer(buffer, marker, null, safe);

            // update buffer
            buffer.Reset(marker);

            // return computed value as literal
            DekiScriptLiteral result;
            switch(_state) {
            case State.EMPTY:
                result = DekiScriptNil.Value;
                break;
            case State.VALUE:
                result = (DekiScriptLiteral)_value;
                break;
            case State.COMPOSITE:
                result = DekiScriptExpression.Constant(((StringBuilder)_value).ToString());
                break;
            case State.XML:
                result = new DekiScriptXml(new XDoc(_document));
                break;
            case State.HTML:
                foreach(var body in from body in _bodies orderby body.Key select body) {
                    _html.InsertBefore(body.Value, _tail);
                }
                if(_head != null) {
                    RemoveDuplicateElements(_head);
                }
                if(_tail != null) {
                    RemoveDuplicateElements(_tail);
                }
                goto case State.XML;
            default:
                throw new ShouldNeverHappenException();
            }
            Clear();
            return result;
        }

        private void Clear() {
            _state = State.EMPTY;
            _attribute = null;
            _html = null;
            _head = null;
            _body = null;
            _tail = null;
            _bodies = null;
            _value = null;
            _document = null;
        }

        private void ConvertStateToHtml(DekiScriptOutputBuffer.XmlStart start) {
            if(_state != State.HTML) {
                _bodies = new Dictionary<string, XmlNode>();

                // check if we're upconverting from the XML state
                XmlNode xml = null;
                if(_state == State.XML) {

                    // preserve the XML element
                    xml = _document.FirstChild;
                    _document.RemoveChild(xml);
                } else {
                    _document = new XmlDocument(XDoc.XmlNameTable);
                }

                // initialize the HTML fields
                _state = State.HTML;
                if(start != null) {
                    _html = _document.CreateElement(start.Name);
                    if(start.Attributes != null) {
                        foreach(var attribute in start.Attributes) {

                            // TODO (steveb): add support for namespaces
                            var attr = _document.CreateAttribute(attribute.Item2);
                            attr.Value = attribute.Item3;
                            _html.Attributes.Append(attr);
                        }
                    }
                } else {
                    _html = _document.CreateElement("html");                    
                }
                _body = _document.CreateElement("body");
                _document.AppendChild(_html);
                _html.AppendChild(_body);

                // append the preserved XML element
                if(xml != null) {
                    _body.AppendChild(xml);
                }
            }
        }

        private void ParseBuffer(DekiScriptOutputBuffer buffer, int marker, XmlNode contextualbody, bool safe) {
            int end = buffer.Marker;

            // compute value
            for(int i = marker; i < end; ++i) {
                var current = buffer[i];

                // check if value is an XML construct
                if(current is DekiScriptOutputBuffer.XmlStart) {
                    AddNode(contextualbody, buffer, ref i, safe);
                } else if(current is DekiScriptXml) {
                    AddXDoc(contextualbody, ((DekiScriptXml)current).Value);
                } else if(current is DekiScriptUri) {
                    AddUri(contextualbody, (DekiScriptUri)current);
                } else {
                    var literal = (DekiScriptLiteral)current;

                    // check what state the result is in
                    switch(_state) {
                    case State.EMPTY:
                        _state = State.VALUE;
                        _value = current;
                        break;
                    case State.VALUE:
                        _state = State.COMPOSITE;
                        _value = new StringBuilder().AppendLiteral((DekiScriptLiteral)_value).AppendLiteral(literal);
                        break;
                    case State.COMPOSITE:
                        ((StringBuilder)_value).AppendLiteral(literal);
                        break;
                    case State.XML:
                        if(contextualbody == null) {
                            ConvertStateToHtml(null);
                        }
                        goto case State.HTML;
                    case State.HTML:
                        AddText(contextualbody, literal);
                        break;
                    default:
                        throw new ShouldNeverHappenException();
                    }
                }
            }
        }

        private void AddHtml(XmlNode context, DekiScriptOutputBuffer buffer, ref int index, bool safe) {
            while(true) {
                object current = buffer[index];
                if(current is DekiScriptOutputBuffer.XmlStart) {
                    DekiScriptOutputBuffer.XmlStart start = (DekiScriptOutputBuffer.XmlStart)current;
                    switch(start.Name) {
                    case "head":
                        if(safe) {
                            SkipNode(buffer, ref index);
                        } else {
                            AddHead(buffer, ref index);
                        }
                        break;
                    case "body":
                        AddBody(context ?? _body, buffer, ref index, safe);
                        break;
                    case "tail":
                        if(safe) {
                            SkipNode(buffer, ref index);
                        } else {
                            AddTail(buffer, ref index);
                        }
                        break;
                    default:

                        // unexpected node; ignore it
                        SkipNode(buffer, ref index);
                        break;
                    }
                } else if(current is DekiScriptOutputBuffer.XmlEnd) {

                    // we're done
                    break;
                } else {

                    // unexpected node; ignore it
                }
                ++index;
            }
        }

        private void AddHead(DekiScriptOutputBuffer buffer, ref int index) {
            ++index;
            while(true) {

                // TODO (steveb): xml comments are also allowed in the <head> section

                var current = buffer[index];
                if(current is DekiScriptOutputBuffer.XmlStart) {
                    var start = (DekiScriptOutputBuffer.XmlStart)current;
                    switch(start.Name) {
                    case "link":
                    case "meta":
                    case "script":
                    case "style":
                        if(_head == null) {
                            _head = _document.CreateElement("head");
                            _html.InsertBefore(_head, _body);
                        }
                        AddNode(_head, buffer, ref index, false);
                        break;
                    default:

                        // TODO (steveb): log that we ignored something
                        SkipNode(buffer, ref index);
                        break;
                    }
                } else if(current is DekiScriptOutputBuffer.XmlEnd) {

                    // we're done
                    break;
                } else {

                    // unexpected node; ignore it
                }
                ++index;
            }
        }

        private void AddBody(XmlNode contextualbody, DekiScriptOutputBuffer buffer, ref int index, bool safe) {
            var start = (DekiScriptOutputBuffer.XmlStart)buffer[index];

            // check which body is being targeted
            XmlNode body = contextualbody;
            if(start.Attributes != null) {
                string target = (from attribute in start.Attributes where string.IsNullOrEmpty(attribute.Item1) && attribute.Item2.EqualsInvariant("target") select attribute.Item3).FirstOrDefault();
                if(!string.IsNullOrEmpty(target)) {
                    if(safe) {

                        // ignore targeted body
                        body = null;
                    } else {

                        // check how to deal with an existing <body> element
                        string conflict = (from attribute in start.Attributes where string.IsNullOrEmpty(attribute.Item1) && attribute.Item2.EqualsInvariant("conflict") select attribute.Item3).FirstOrDefault() ?? "ignore";
                        switch(conflict) {
                        case "append":

                            // try to find an existing <body> element to append to
                            if(!_bodies.TryGetValue(target, out body)) {
                                XmlElement newbody = _document.CreateElement("body");
                                newbody.SetAttribute("target", target);
                                _bodies[target] = body = newbody;
                            }
                            break;
                        case "ignore":
                            goto default;
                        case "replace":

                            // always create a new <body> element to append to
                            if(true) {
                                XmlElement newbody = _document.CreateElement("body");
                                newbody.SetAttribute("target", target);
                                _bodies[target] = body = newbody;
                            }
                            break;
                        default:

                            // skip this <body> element if one alrady exists, otherwise create one to append to
                            if(!_bodies.ContainsKey(target)) {
                                XmlElement newbody = _document.CreateElement("body");
                                newbody.SetAttribute("target", target);
                                _bodies[target] = body = newbody;
                            } else {

                                // skip all nodes that are part of this subset
                                body = null;
                            }
                            break;
                        }
                    }
                }
            }

            // check if we have a body to append to
            if(body != null) {

                // parse contents into body
                ++index;
                while(!(buffer[index] is DekiScriptOutputBuffer.XmlEnd)) {
                    AddNode(body, buffer, ref index, safe);
                    ++index;
                }
            } else {
                SkipNode(buffer, ref index);
            }
        }

        private void AddTail(DekiScriptOutputBuffer buffer, ref int index) {
            ++index;
            while(true) {

                // TODO (steveb): xml comments are also allowed in the <tail> section

                var current = buffer[index];
                if(current is DekiScriptOutputBuffer.XmlStart) {
                    var start = (DekiScriptOutputBuffer.XmlStart)current;
                    switch(start.Name) {
                    case "script":
                        if(_tail == null) {
                            _tail = _document.CreateElement("tail");
                            _html.InsertAfter(_tail, _body);
                        }
                        AddNode(_tail, buffer, ref index, false);
                        break;
                    default:

                        // TODO (steveb): log that we ignored something
                        SkipNode(buffer, ref index);
                        break;
                    }
                } else if(current is DekiScriptOutputBuffer.XmlEnd) {

                    // we're done
                    break;
                } else {

                    // unexpected node; ignore it
                }
                ++index;
            }
        }

        private void AddNode(XmlNode context, DekiScriptOutputBuffer buffer, ref int index, bool safe) {

            // TODO (steveb); add support for namespaces

            int nesting = 0;
            while(true) {
                var current = buffer[index];
                if(current is DekiScriptOutputBuffer.XmlStart) {
                    var start = (DekiScriptOutputBuffer.XmlStart)current;

                    // check if a <html> or <content> node is being parsed
                    if(start.Name.EqualsInvariant("html") || start.Name.EqualsInvariant("content")) {
                        ConvertStateToHtml(start);
                        ++index;
                        AddHtml(context ?? _body, buffer, ref index, safe);
                    } else if(start.Name.EqualsInvariant("body")) {
                        ConvertStateToHtml(null);
                        AddBody(context ?? _body, buffer, ref index, safe);
                    } else if(!safe || DekiScriptLibrary.XhtmlSafeCop.IsLegalElement(start.Name)) {

                        // check if the current state needs to be upgraded
                        switch(_state) {
                        case State.EMPTY:
                            _state = State.XML;

                            // initialize document
                            _document = new XmlDocument(XDoc.XmlNameTable);
                            break;
                        case State.VALUE:
                            if(_value is DekiScriptUri) {
                                AddUri(context, (DekiScriptUri)_value);
                            } else {
                                AddText(context, (DekiScriptLiteral)_value);
                            }
                            _value = null;
                            break;
                        case State.COMPOSITE:
                            AddText(context, ((StringBuilder)_value).ToString());
                            _value = null;
                            break;
                        case State.XML:
                            if(context == null) {
                                ConvertStateToHtml(null);
                            }
                            break;
                        case State.HTML:

                            // nothing special to do
                            break;
                        default:
                            throw new ShouldNeverHappenException();
                        }

                        // append element to parent node
                        var element = _document.CreateElement(start.Name);
                        (context ?? _body ?? _document).AppendChild(element);

                        // set attributes on element
                        if(start.Attributes != null) {
                            foreach(var attribute in start.Attributes) {
                                if(!safe || DekiScriptLibrary.XhtmlSafeCop.IsLegalAttribute(start.Name, attribute.Item2)) {
                                    element.SetAttribute(attribute.Item2, attribute.Item3);
                                }
                            }
                        }

                        // set new element as context node
                        ++nesting;
                        context = element;
                    } else {
                        SkipNode(buffer, ref index);
                    }
                } else if(current is DekiScriptOutputBuffer.XmlEnd) {

                    // set parent as context node
                    context = context.ParentNode;
                    if(--nesting < 0) {
                        throw new ShouldNeverHappenException();
                    }
                } else if(current is DekiScriptXml) {
                    AddXDoc(context, ((DekiScriptXml)current).Value);
                } else if(current is DekiScriptUri) {
                    AddUri(context, (DekiScriptUri)current);
                } else {
                    AddText(context, (DekiScriptLiteral)current);
                }
                if(nesting == 0) {
                    break;
                }
                ++index;
            }
        }

        private void AddUri(XmlNode context, DekiScriptUri uri) {
            if(context == null) {
                ConvertStateToHtml(null);
            }

            // NOTE (steveb): URIs have special embedding rules; either embed it as a <img> and <a> document base on the URI file extension on the last segment

            XUri url = uri.Value.AsPublicUri();
            MimeType mime = (url.Segments.Length > 0) ? MimeType.FromFileExtension(url.LastSegment ?? string.Empty) : MimeType.BINARY;
            XDoc item = mime.MainType.EqualsInvariant("image") ?
                DekiScriptLibrary.WebImage(url.ToString(), null, null, null) :
                DekiScriptLibrary.WebLink(url.ToString(), null, null, null);
            AddXDoc(context, item);
        }

        private void AddText(XmlNode context, DekiScriptLiteral literal) {
            if(context == null) {
                ConvertStateToHtml(null);
            }

            // TODO (steveb): this is just plain retarded; why do we need to distinguish between string and non-string?!?

            if(literal is DekiScriptString) {
                (context ?? _body).AppendChild(_document.CreateTextNode(((DekiScriptString)literal).Value));
            } else {
                (context ?? _body).AppendChild(_document.CreateTextNode(literal.ToString()));
            }
        }

        private void AddText(XmlNode context, string text) {
            if(context == null) {
                ConvertStateToHtml(null);
            }

            (context ?? _body).AppendChild(_document.CreateTextNode(text));
        }

        private void AddXDoc(XmlNode contextualbody, XDoc doc) {

            // check if this is a no-op
            if(doc.IsEmpty) {
                return;
            }

            // create a sub-buffer for processing
            DekiScriptOutputBuffer buffer = new DekiScriptOutputBuffer(int.MaxValue);

            // push all nodes from the XML document into the buffer
            Stack<XmlNode> stack = new Stack<XmlNode>();

            // check if we're dealing with a simple <html><body> ... </body></html> document
            bool addSiblings = false;
            if((doc.AsXmlNode.NodeType == XmlNodeType.Element) && doc.HasName("html") && doc["head/*"].IsEmpty && doc["tail/*"].IsEmpty && doc["body[@target]"].IsEmpty) {
                var body = doc["body"];
                if(body.IsEmpty || (body.AsXmlNode.ChildNodes.Count == 0)) {

                    // nothing to do
                    return;
                }
                doc = doc[body.AsXmlNode.FirstChild];
                addSiblings = true;
            }

            // loop over nodes
            XmlNode current = doc.AsXmlNode;
            do {
                XmlElement element;
                switch(current.NodeType) {
                case XmlNodeType.Element:
                    element = (XmlElement)current;
                    AppendXmlStart(buffer, element);
                    if(element.HasChildNodes) {
                        stack.Push(addSiblings || (stack.Count > 0) ? current.NextSibling : null);
                        current = element.FirstChild;
                        continue;
                    }
                    buffer.PushXmlEnd();
                    break;
                case XmlNodeType.Attribute:
                    buffer.Push(DekiScriptExpression.Constant((string.IsNullOrEmpty(current.Prefix) ? current.Name : current.Prefix + ":" + current.Name) + "=" + current.Value.QuoteString()));
                    break;
                case XmlNodeType.CDATA:
                case XmlNodeType.SignificantWhitespace:
                case XmlNodeType.Text:
                case XmlNodeType.Whitespace:
                    buffer.Push(DekiScriptExpression.Constant(current.Value));
                    break;
                default:

                    // ignore this node
                    break;
                }

                // move onto next item or resume previous one
                current = addSiblings || (stack.Count > 0) ? current.NextSibling : null;
                while((current == null) && (stack.Count > 0)) {
                    buffer.PushXmlEnd();
                    current = stack.Pop();
                }
            } while(current != null);

            // parse the sub-buffer
            ParseBuffer(buffer, 0, contextualbody, false);
        }
    }
}
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
using System.Collections;
using System.Xml;
using MindTouch.Dream;
using MindTouch.Xml;

namespace MindTouch.Deki.Script.Runtime.Library {
    public static partial class DekiScriptLibrary {

        //--- Class Fields ---
        private static readonly XDocCop _xhtmlSafeCop;
        private static readonly XDocCop _xhtmlCompleteCop;

        //--- Class Properties ---
        public static XDocCop XhtmlSafeCop { get { return _xhtmlSafeCop; } }

        //--- Class Methods ---
        [DekiScriptFunction("xml.text", "Get a text value from an XML document.")]
        public static string XmlText(
            [DekiScriptParam("XML document")] XDoc doc,
            [DekiScriptParam("xpath to value (default: \".\")", true)] string xpath,
            [DekiScriptParam("namespaces (default: none)", true)] Hashtable namespaces,
            [DekiScriptParam("include XML elements (default: false)", true)] bool? xml,
            [DekiScriptParam("include only inner XML (default: false )", true)] bool? inner
        ) {
            bool includeXml = xml ?? false;
            bool asInner = inner ?? false;
            if(doc.IsEmpty) {
                return null;
            }
            XmlNode node = AtXPathNode(doc, xpath, namespaces).AsXmlNode;
            if(node == null) {
                return null;
            }
            if(includeXml) {
                return asInner ? node.InnerXml : node.OuterXml;
            }
            return node.InnerText;
        }

        [DekiScriptFunction("xml.date", "Get a date-time value from an XML document.")]
        public static string XmlDate(
            [DekiScriptParam("XML document")] XDoc doc,
            [DekiScriptParam("xpath to value (default: \".\")", true)] string xpath,
            [DekiScriptParam("namespaces (default: none)", true)] Hashtable namespaces
        ) {
            if(doc.IsEmpty) {
                return null;
            }
            DateTime? result = null;
            try {
                result = AtXPathNode(doc, xpath, namespaces).AsDate;
            } catch { }
            if(result == null) {
                return null;
            }
            return CultureDateTime(result.Value, GetCulture(), 0);
        }

        [DekiScriptFunction("xml.num", "Get a number from an XML document.")]
        public static double? XmlNum(
            [DekiScriptParam("XML document")] XDoc doc,
            [DekiScriptParam("xpath to value (default: \".\")", true)] string xpath,
            [DekiScriptParam("namespaces (default: none)", true)] Hashtable namespaces
        ) {
            if(doc.IsEmpty) {
                return null;
            }
            return AtXPathNode(doc, xpath, namespaces).AsDouble;
        }

        [DekiScriptFunction("xml.list", "Get list of values from an XML document.")]
        public static ArrayList XmlList(
            [DekiScriptParam("XML document")] XDoc doc,
            [DekiScriptParam("xpath to list of values")] string xpath,
            [DekiScriptParam("namespaces (default: none)", true)] Hashtable namespaces,
            [DekiScriptParam("capture enclosing XML element (default: false)", true)] bool? xml
        ) {
            ArrayList result = doc.IsEmpty ? new ArrayList() : AtXPathList(doc, xpath, namespaces, xml ?? false);
            return result;
        }

        [DekiScriptFunction("xml.select", "Create an XML selection using an xpath.")]
        public static XDoc XmlSelect(
            [DekiScriptParam("XML document")] XDoc doc,
            [DekiScriptParam("xpath for selector", true)] string xpath,
            [DekiScriptParam("namespaces (default: none)", true)] Hashtable namespaces
        ) {
            return AtXPathNode(doc, xpath, namespaces);
        }

        [DekiScriptFunction("xml.html", "Render a selection of nodes into an xml document.")]
        public static XDoc XmlHtml(
            [DekiScriptParam("XML document")] XDoc doc,
            [DekiScriptParam("xpath for selector", true)] string xpath
        ) {
            if(doc.Name.EqualsInvariantIgnoreCase("html")) {
                return doc;
            }
            XDoc result = new XDoc("html").Start("body");
            if(string.IsNullOrEmpty(xpath)) {
                result.AddNodes(doc);
            } else {
                foreach(XDoc match in doc[xpath]) {
                    result.AddNodes(match);
                }
            }
            return CleanseHtmlDocument(result);
        }

        [DekiScriptFunction("xml.format", "Render the XML document.")]
        public static string XmlFormat(
            [DekiScriptParam("XML document")] XDoc doc,
            [DekiScriptParam("formatting style (one of \"plain\", \"xhtml\", \"indented\"; default: \"indented\")", true)] string style
        ) {
            style = style ?? "formatted";
            switch(style.ToLowerInvariant()) {
            case "plain":
                return doc.ToString();
            case "xhtml":
                return doc.ToXHtml();
            case "indented":
            default:
                return doc.ToPrettyString();
            }
        }

        [DekiScriptFunction("xml.name", "Get the name of the root node in the XML document.")]
        public static string XmlName(
            [DekiScriptParam("XML document")] XDoc doc
        ) {
            return doc.IsEmpty ? null : doc.Name;
        }

        public static bool VerifyXHtml(XDoc body, bool sanitize) {
            if(sanitize) {

                // remove all <script> nodes entirely (XDocCop will only remove the openeing & closing tags, leaving the script code as plain text... valid, but ugly)
                if(!body[".//_:script"].IsEmpty) {
                    return false;
                }

                // use restricted rules
                if(!_xhtmlSafeCop.Verify(body)) {
                    return false;
                }
                foreach(XDoc attr in body[".//@href | .//@src | .//@poster | .//_:img/@dynsrc | .//_:img/@lowsrc | .//@background | .//@style"]) {
                    if(ContainsXSSVulnerability(attr.Name, attr.Contents)) {
                        return false;
                    }
                }
                return true;
            }

            // use permissive rules, but still remove invalid HTML elements
            return _xhtmlCompleteCop.Verify(body);
        }

        public static void ValidateXHtml(XDoc body, bool sanitize, bool removeIllegalElements) {
            if(sanitize) {

                // remove all <script> nodes entirely (XDocCop will only remove the openeing & closing tags, leaving the script code as plain text... valid, but ugly)
                body[".//_:script"].RemoveAll();

                // use restricted rules
                _xhtmlSafeCop.Enforce(body, removeIllegalElements);
                foreach(XDoc attr in body[".//@href | .//@src | .//@poster | .//_:img/@dynsrc | .//_:img/@lowsrc | .//@background | .//@style"]) {
                    if(ContainsXSSVulnerability(attr.Name, attr.Contents)) {
                        attr.Remove();
                    }
                }
            } else {

                // use permissive rules, but still remove invalid HTML elements
                _xhtmlCompleteCop.Enforce(body, removeIllegalElements);
            }
        }

        public static XDoc CleanseHtmlDocument(XDoc html) {
            if(html.HasName("html")) {
                html = html.Clone();

                // remove <head> and <tail> elements
                html["head"].RemoveAll();
                html["tail"].RemoveAll();

                // make sure there is only one body and validate it
                var mainBody = html["body[not(@target)]"];
                if(mainBody.IsEmpty) {
                    html.Elem("body");
                    mainBody = html["body[not(@target)]"];
                }
                foreach(XDoc body in html["body[@target]"]) {
                    body.Remove();
                }
                ValidateXHtml(mainBody, true, true);
            }
            return html;
        }

        public static bool ContainsXSSVulnerability(string name, string value) {
            try {
                if(name.EqualsInvariantIgnoreCase("style")) {
                    WebCheckStyle(value);
                } else if(name.EqualsInvariantIgnoreCase("href") ||
                    name.EqualsInvariantIgnoreCase("src") ||
                    name.EqualsInvariantIgnoreCase("poster") ||
                    name.EqualsInvariantIgnoreCase("dynsrc") ||
                    name.EqualsInvariantIgnoreCase("lowsrc") ||
                    name.EqualsInvariantIgnoreCase("background")
                ) {
                    WebCheckUri(value);
                }
            } catch {
                return true;
            }
            return false;
        }

        private static XDoc AtXPathNode(XDoc doc, string xpath, Hashtable namespaces) {
            XDoc result = doc;

            // check if xpath selects anything other than the current node
            if(!string.IsNullOrEmpty(xpath) && !xpath.EqualsInvariant(".")) {

                // check if custom namespace mapping was provided
                if(namespaces != null) {

                    // initialize a namespace manager
                    XmlNamespaceManager nsm = new XmlNamespaceManager(SysUtil.NameTable);
                    foreach(DictionaryEntry ns in namespaces) {
                        nsm.AddNamespace((string)ns.Key, SysUtil.ChangeType<string>(ns.Value));
                    }
                    result = doc.AtPath(xpath, nsm);
                } else {

                    // use default namespace manager
                    result = doc[xpath];
                }
            }
            return result;
        }

        private static string AtXPath(XDoc doc, string xpath, Hashtable namespaces, bool asXml) {
            XDoc node = AtXPathNode(doc, xpath, namespaces);
            if(asXml && !node.IsEmpty) {
                if(node.AsXmlNode.NodeType == XmlNodeType.Attribute) {
                    return ((XmlAttribute)node.AsXmlNode).OwnerElement.OuterXml;
                }
                return node.AsXmlNode.OuterXml;
            }
            return node.AsText;
        }

        private static ArrayList AtXPathList(XDoc doc, string xpath, Hashtable namespaces, bool asXml) {
            XDoc node = AtXPathNode(doc, xpath, namespaces);

            // iterate over all matches
            ArrayList result = new ArrayList();
            foreach(XDoc item in node) {
                if(asXml) {
                    if(item.AsXmlNode.NodeType == XmlNodeType.Attribute) {
                        result.Add(((XmlAttribute)item.AsXmlNode).OwnerElement.OuterXml);
                    } else {
                        result.Add(item.AsXmlNode.OuterXml);
                    }
                } else {
                    result.Add(item.AsText);
                }
            }
            return result;
        }
    }
}

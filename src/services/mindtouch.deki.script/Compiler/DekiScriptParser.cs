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
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using MindTouch.Deki.Script.Expr;
using MindTouch.Xml;

namespace MindTouch.Deki.Script.Compiler {
    public class DekiScriptParser {

        //--- Constants ---
        private static readonly Regex ID_REGEX = new Regex(@"^([a-zA-Z][a-zA-Z0-9_]*)$|^\$$", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Singleline);

        //--- Class Methods ---
        public static DekiScriptExpression Parse(Location location, Stream source) {
            if(source == null) {
                throw new ArgumentNullException("source");
            }

            // parse source
            try {
                var scanner = new Scanner(source, location.Origin, location.Line, location.Column);
                var parser = new Parser(scanner);
                parser.Parse();
                return parser.result;
            } catch(FatalError e) {
                throw new DekiScriptParserException(e.Message, Location.None);
            } finally {
                source.Dispose();
            }
        }

        public static DekiScriptExpression Parse(Location location, string source) {
            if(source == null) {
                throw new ArgumentNullException("source");
            }

            // convert unbreakable spaces to regular spaces and trim buffer
            source = source.ReplaceAll("\u00A0", " ", "\u00AD", "").Trim();

            // parse source
            try {
                using(MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(source))) {
                    Scanner scanner = new Scanner(stream, location.Origin, location.Line, location.Column);
                    Parser parser = new Parser(scanner);
                    parser.Parse();
                    return parser.result;
                }
            } catch(FatalError e) {
                throw new DekiScriptParserException(e.Message, Location.None);
            }
        }

        public static DekiScriptExpression TryParse(string code) {
            try {
                return Parse(Location.Start, code);
            } catch { }
            return null;
        }

        public static bool IsIdentifier(string text) {
            return ID_REGEX.IsMatch(text);
        }

        public static bool IsNumber(string text) {
            double value;
            return double.TryParse(text, out value);
        }

        public static DekiScriptExpression Parse(XDoc doc) {
            return Parse(doc, true);
        }

        internal static DekiScriptExpression Parse(XDoc doc, bool scripted) {
            DekiScriptParser parser = new DekiScriptParser();
            var list = new List<DekiScriptExpression>();
            var root = (XmlElement)doc.AsXmlNode;
            parser.PushNode(root);
            parser.Parse(root, list);
            var result = parser.TryCatch(list[0], scripted);
            parser.PopNode();
            if(list.Count != 1) {
                throw new DekiScriptParserException("invalid state: more than one root node in document", Location.None);
            }
            return result;
        }

        private static string StripCode(string code) {
            code = code.ReplaceAll("\u00A0", " ", "\u00AD", "").Trim();
            if(code.StartsWithInvariantIgnoreCase(DekiScriptRuntime.ON_SAVE_PATTERN)) {
                code = code.Substring(DekiScriptRuntime.ON_SAVE_PATTERN.Length).TrimStart();
            } else if(code.StartsWithInvariantIgnoreCase(DekiScriptRuntime.ON_SUBST_PATTERN)) {
                code = code.Substring(DekiScriptRuntime.ON_SUBST_PATTERN.Length).TrimStart();
            } else if(code.StartsWithInvariantIgnoreCase(DekiScriptRuntime.ON_EDIT_PATTERN)) {
                code = code.Substring(DekiScriptRuntime.ON_EDIT_PATTERN.Length).TrimStart();
            }
            return code;
        }

        private static void ConvertBrToNewline(XmlNode current) {

            // convert inner <br/> elements to newlines
            XmlNodeList breaks = current.SelectNodes(".//br");
            if((breaks != null) && (breaks.Count > 0)) {
                List<XmlNode> breakList = new List<XmlNode>(breaks.Count);
                foreach(XmlNode br in breaks) {
                    breakList.Add(br);
                }
                foreach(XmlNode br in breakList) {
                    br.ParentNode.InsertBefore(br.OwnerDocument.CreateTextNode("\n"), br);
                    br.ParentNode.RemoveChild(br);
                }
            }
        }

        //--- Fields ---
        private readonly List<Location> _path = new List<Location>();
        private readonly Dictionary<XmlNode, int> _nodePositionLookup = new Dictionary<XmlNode, int>();
        
        //--- Constructors ---
        private DekiScriptParser() { }

        //--- Properties ---
        public Location Location {
            get {
                return (_path.Count > 0) ? _path[_path.Count - 1] : Location.None;
            }
        }

        //--- Methods ---
        private XmlNode Parse(XmlElement current, List<DekiScriptExpression> list) {
            XmlNode next = current.NextSibling;
            string value;

            // check if element needs to be evaluated
            try {
                if(current.NamespaceURI.EqualsInvariant(XDekiScript.ScriptNS)) {

                    // element has "eval:" prefix
                    switch(current.LocalName) {
                    #region <if test="bool-expr">...</if>{<elseif test="bool-expr">...</elseif>}[<else>...</else>]
                    case "if": {
                        List<Tuplet<DekiScriptExpression, DekiScriptExpression>> conditionals = new List<Tuplet<DekiScriptExpression, DekiScriptExpression>>();

                        // initial "if" statement
                        DekiScriptExpression condition = Parse(SubLocation("/@test"), current.GetAttribute("test"));
                        conditionals.Add(new Tuplet<DekiScriptExpression, DekiScriptExpression>(condition, BuildChildren(current)));

                        // check for subsequent "elseif" and "else" statements
                        while(true) {

                            // move to next node
                            XmlNode originalNext = next;

                            // skip empty text nodes
                            while((next != null) && ((next.NodeType == XmlNodeType.Whitespace) || (next.NodeType == XmlNodeType.SignificantWhitespace) || ((next.NodeType == XmlNodeType.Text) && (next.Value.Trim().Length == 0)))) {
                                next = next.NextSibling;
                            }

                            // check if next node is an alternate branch
                            if((next != null) && next.NamespaceURI.EqualsInvariant(XDekiScript.ScriptNS) && (next.LocalName.EqualsInvariant("elseif") || next.LocalName.EqualsInvariant("else"))) {
                                current = (XmlElement) next;
                                PopNode();
                                PushNode(current);
                                next = current.NextSibling;
                                if(current.LocalName.EqualsInvariant("elseif")) {

                                    // process "elseif" branch
                                    condition = Parse(SubLocation("/@test"), current.GetAttribute("test"));
                                    conditionals.Add(new Tuplet<DekiScriptExpression, DekiScriptExpression>(condition, BuildChildren(current)));
                                } else {

                                    // process "else" branch
                                    conditionals.Add(new Tuplet<DekiScriptExpression, DekiScriptExpression>(null, BuildChildren(current)));
                                    break;
                                }
                            } else {

                                // couln't find an alternatte branch, restore the original next node
                                next = originalNext;
                                break;
                            }
                        }
                        list.Add(DekiScriptExpression.IfElseStatements(Location, conditionals));
                    }
                        break;
                    #endregion

                    #region <foreach [var="id"] in="list-or-map-or-xml-expr" [where|test="bool-expr"]>...</foreach>
                    case "foreach": {
                        string variable = current.HasAttribute("var") ? current.GetAttribute("var").Trim() : DekiScriptRuntime.DEFAULT_ID;
                        string where = current.GetAttribute("where");
                        if(string.IsNullOrEmpty(where)) {
                            where = current.GetAttribute("test");
                        }
                        DekiScriptGenerator generator = null;
                        if(!string.IsNullOrEmpty(where)) {
                            var location = SubLocation("/@where");
                            generator = new DekiScriptGeneratorIf(location, Parse(location, where), null);
                        }
                        generator = new DekiScriptGeneratorForeachValue(Location, new[] { variable }, Parse(Location, current.GetAttribute("in")), generator);
                        list.Add(DekiScriptExpression.ForeachStatement(Location, generator, BuildChildren(current)));
                    }
                        break;
                    #endregion

                    #region <expr value="expr" /> -OR- <expr>expr</expr>
                    case "expr": {
                        string code = current.HasAttribute("value") ? current.GetAttribute("value") : current.InnerText;
                        DekiScriptExpression expr = Parse(Location, code);
                        list.Add(expr);
                    }
                        break;
                    #endregion

                    #region <js value="expr" /> -OR- <js>expr</js>
                    case "js": {
                        string code = current.HasAttribute("value") ? current.GetAttribute("value") : current.InnerText;
                        DekiScriptExpression expr = Parse(Location, code);
                        list.Add(DekiScriptExpression.Call(Location, DekiScriptExpression.Access(Location, DekiScriptExpression.Id(Location, "json"), DekiScriptExpression.Constant("emit")), new DekiScriptListConstructor(null, expr)));
                    }
                        break;
                    #endregion

                    #region <block value="expr">...</block>
                    case "block": {

                        // TODO (steveb): it seems odd we use InnerText here instead of value; what is the motivation?
                        string code = current.HasAttribute("value") ? current.GetAttribute("value") : current.InnerText;
                        list.Add(DekiScriptExpression.BlockWithDeclaration(Location, Parse(Location, code), BuildChildren(current)));
                    }
                        break;
                    #endregion

                    default:
                        throw new DekiScriptParserException(string.Format("{0}, unknown elementn <eval:{1}>", Location, current.LocalName), Location.None);
                    }
                } else {
                    List<DekiScriptExpression> nodes = new List<DekiScriptExpression>();

                    // process "function" attribute
                    if(!string.IsNullOrEmpty(value = current.GetAttribute("function"))) {

                        // NOTE (steveb): process content transform

                        // check if function contains '$' sign, which is a place holder for the main argument
                        DekiScriptExpression evaluation = Parse(SubLocation("/@function"), (value.IndexOf('$') < 0) ? value + "($)" : value);

                        // determine if main argument is a string or an xml document
                        DekiScriptExpression arg;
                        if(current.LocalName.EqualsInvariant("pre")) {
                            ConvertBrToNewline(current);

                            // pass argument in as a string
                            arg = DekiScriptExpression.Constant(StripCode(current.InnerText));
                        } else {

                            // pass argument in as a HTML document
                            List<DekiScriptExpression> inner = new List<DekiScriptExpression>();
                            BuildElement(current, inner);

                            DekiScriptExpression body = DekiScriptExpression.XmlElement(Location, null, DekiScriptExpression.Constant("body"), null, DekiScriptExpression.Block(Location, inner));
                            DekiScriptExpression html = DekiScriptExpression.XmlElement(Location, null, DekiScriptExpression.Constant("html"), null, body);
                            arg = html;
                        }

                        // create DOM expression
                        DekiScriptExpression assign = DekiScriptExpression.VarStatement(Location, DekiScriptExpression.Id(Location, DekiScriptRuntime.DEFAULT_ID), arg);
                        DekiScriptExpression statements = DekiScriptExpression.BlockWithDeclaration(Location, assign, evaluation);
                        nodes.Add(TryCatch(statements, true));
                    } else if(current.LocalName.EqualsInvariant("span") && current.GetAttribute("class").EqualsInvariant("script")) {
                        ConvertBrToNewline(current);

                        // convert <span class="script">...</span> to <eval:expr>...</eval:expr>
                        nodes.Add(TryCatch(Parse(Location, new XmlNodePlainTextReadonlyByteStream(current)), true));
                    } else if(current.LocalName.EqualsInvariant("pre")) {
                        string cls = current.GetAttribute("class");
                        if(cls.EqualsInvariant("script")) {
                            ConvertBrToNewline(current);

                            DekiScriptExpression expr = TryCatch(Parse(Location, new XmlNodePlainTextReadonlyByteStream(current)), true);
                            nodes.Add(expr);
                        } else if(cls.EqualsInvariant("script-jem")) {
                            ConvertBrToNewline(current);

                            // convert <pre class="script-jem">...</pre> to <html><body><script type="text/jem">...</script></body></html>
                            DekiScriptExpression html = Html("body", "script", "text/jem", DekiScriptExpression.Constant(current.InnerText.ReplaceAll("\u00A0", " ", "\00AD", "")));
                            nodes.Add(html);
                        } else if(cls.EqualsInvariant("script-js")) {
                            ConvertBrToNewline(current);

                            // convert <pre class="script-js">...</pre> to <html><body><script type="text/js">...</script></body></html>
                            DekiScriptExpression html = Html("body", "script", "text/javascript", DekiScriptExpression.Constant(current.InnerText.ReplaceAll("\u00A0", " ", "\00AD", "")));
                            nodes.Add(html);
                        } else if(cls.EqualsInvariant("script-css")) {
                            ConvertBrToNewline(current);

                            // convert <pre class="script-css">...</pre> to <html><head><style type="text/css">...</style></head></html>
                            DekiScriptExpression html = Html("head", "style", "text/css", DekiScriptExpression.Constant(current.InnerText.ReplaceAll("\u00A0", " ", "\00AD", "")));
                            nodes.Add(html);
                        } else {
                            BuildElement(current, nodes);
                        }
                    } else {
                        BuildElement(current, nodes);
                    }

                    // process "block" attribute
                    bool scripted = false;
                    if(!string.IsNullOrEmpty(value = current.GetAttribute("block"))) {
                        scripted = true;

                        // attribute "block" is present
                        var location = SubLocation("/@block");
                        DekiScriptExpression blockExpr = Parse(location, value);
                        blockExpr = DekiScriptExpression.BlockWithDeclaration(Location, blockExpr, nodes);
                        nodes.Clear();
                        nodes.Add(blockExpr);
                    }

                    // process "foreach" attribute
                    if(!string.IsNullOrEmpty(value = current.GetAttribute("foreach"))) {
                        scripted = true;

                        // attribute "foreach" is present
                        StringBuilder expression = new StringBuilder();
                        expression.Append(value);
                        string where = current.GetAttribute("where");
                        if(!string.IsNullOrEmpty(where)) {
                            expression.Append(", if ").Append(where);
                        }
                        var location = SubLocation("/@foreach");
                        DekiScriptForeach foreachExpr = (DekiScriptForeach)Parse(location, "foreach(" + expression + "){}");
                        DekiScriptExpression foreachExpr2 = DekiScriptExpression.ForeachStatement(location, foreachExpr.Generator, nodes);
                        nodes.Clear();
                        nodes.Add(foreachExpr2);
                    }

                    // process "if" attribute
                    if(!string.IsNullOrEmpty(value = current.GetAttribute("if"))) {
                        scripted = true;

                        // attribute "if" is present
                        var location = SubLocation("/@if");
                        DekiScriptExpression condition = Parse(location, value);
                        condition = DekiScriptExpression.IfElseStatements(location, new[] { new Tuplet<DekiScriptExpression, DekiScriptExpression>(condition, DekiScriptExpression.Block(Location, nodes)) });
                        nodes.Clear();
                        nodes.Add(condition);
                    }

                    // process "init" attribute
                    if(!string.IsNullOrEmpty(value = current.GetAttribute("init"))) {
                        scripted = true;

                        // attribute "init" is present
                        DekiScriptExpression init = Parse(Location, value);
                        DekiScriptExpression dom = DekiScriptExpression.BlockWithDeclaration(SubLocation("/@init"), init, nodes);
                        nodes.Clear();
                        nodes.Add(dom);
                    }

                    // append inner nodes
                    switch(nodes.Count) {
                    case 0:

                        // nothing to do
                        break;
                    case 1:
                        list.Add(TryCatch(nodes[0], scripted));
                        break;
                    default:
                        list.AddRange(nodes);
                        break;
                    }
                }
            } catch(Exception e) {
                XDoc warning = new XDoc("html").Start("body").Add(DekiScriptRuntime.CreateWarningFromException(null, Location, e)).End();
                list.Add(new DekiScriptXml(warning));
            }
            return next;
        }

        private Location SubLocation(string suffix) {
            return new Location(Location.Origin + suffix);
        }

        private void BuildElement(XmlNode current, ICollection<DekiScriptExpression> list) {

            // create new element
            var attributes = BuildAttributes(current);
            var elem = DekiScriptExpression.XmlElement(Location, current.Prefix, DekiScriptExpression.Constant(current.LocalName), attributes.ToArray(), BuildChildren(current));
            list.Add(elem);
        }

        private List<DekiScriptXmlElement.Attribute> BuildAttributes(XmlNode current) {
            List<DekiScriptXmlElement.Attribute> result = new List<DekiScriptXmlElement.Attribute>(current.Attributes.Count);
            for(int i = 0; i < current.Attributes.Count; ++i) {
                XmlAttribute attribute = current.Attributes[i];
                PushNode(attribute);

                // check if attribute needs to be evaluated
                if(attribute.NamespaceURI == XDekiScript.ScriptNS) {

                    // NOTE (steveb): eval:key="value"
                    DekiScriptXmlElement.Attribute attr = new DekiScriptXmlElement.Attribute(Location, null, DekiScriptExpression.Constant(attribute.LocalName), Parse(Location, attribute.Value));
                    result.Add(attr);
                } else if(attribute.Value.StartsWithInvariant("{{") && attribute.Value.EndsWithInvariant("}}")) {

                    // NOTE (steveb): key="{{value}}"
                    DekiScriptExpression expr = Parse(Location, StripCode(attribute.Value.Substring(2, attribute.Value.Length - 4)));
                    DekiScriptXmlElement.Attribute attr = new DekiScriptXmlElement.Attribute(Location, attribute.Prefix, DekiScriptExpression.Constant(attribute.LocalName), expr);
                    result.Add(attr);
                } else if(!attribute.NamespaceURI.EqualsInvariant("http://www.w3.org/2000/xmlns/") || !attribute.Value.EqualsInvariant("http://mindtouch.com/2007/dekiscript")) {

                    // skip "init", "if", "foreach", "block", "where", "function" since they have already been processed
                    if(!attribute.NamespaceURI.EqualsInvariant(string.Empty) || !(
                        attribute.LocalName.EqualsInvariant("init") ||
                        attribute.LocalName.EqualsInvariant("if") ||
                        attribute.LocalName.EqualsInvariant("foreach") ||
                        attribute.LocalName.EqualsInvariant("where") ||
                        attribute.LocalName.EqualsInvariant("block") ||
                        attribute.LocalName.EqualsInvariant("function")
                    )) {

                        // add static attribute
                        DekiScriptXmlElement.Attribute attr = new DekiScriptXmlElement.Attribute(Location, attribute.Prefix, DekiScriptExpression.Constant(attribute.LocalName), DekiScriptExpression.Constant(attribute.Value));
                        result.Add(attr);
                    }
                }
                PopNode();
            }
            return result;
        }

        private DekiScriptExpression BuildChildren(XmlNode current) {
            List<DekiScriptExpression> result = new List<DekiScriptExpression>(current.ChildNodes.Count);
            for(XmlNode node = current.FirstChild, next; node != null; node = next) {
                PushNode(node);
                next = node.NextSibling;
                switch(node.NodeType) {
                case XmlNodeType.Comment:

                    // TODO (steveb): for now we skip comment, though we MAY want to emit them
                    break;
                case XmlNodeType.Element:
                    next = Parse((XmlElement)node, result);
                    break;
                case XmlNodeType.Text:
                case XmlNodeType.Whitespace:
                case XmlNodeType.SignificantWhitespace:
                    result.Add(DekiScriptExpression.Constant(node.Value));
                    break;
                case XmlNodeType.CDATA:

                    // TODO (steveb): we MAY want to treat CDATA differently than text section
                    result.Add(DekiScriptExpression.Constant(node.Value));
                    break;
                }
                PopNode();
            }
            return DekiScriptExpression.Block(Location, result);
        }

        private void PushNode(XmlNode node) {
            Location location = Location;
            switch(node.NodeType) {
            case XmlNodeType.Attribute:
                location = new Location(location.Origin + "/@" + node.Name);
                break;
            case XmlNodeType.CDATA:
            case XmlNodeType.Comment:
            case XmlNodeType.SignificantWhitespace:
            case XmlNodeType.Text:
            case XmlNodeType.Whitespace:
                location = new Location(location.Origin + "/" + node.Name);
                break;
            case XmlNodeType.Element: {

                    // count how many nodes with the same name precedded the current node
                    int counter = 1;
                    for(XmlNode previous = node.PreviousSibling; previous != null; previous = previous.PreviousSibling) {
                        if(previous.Name.EqualsInvariant(node.Name)) {
                            int previousPosition;
                            if(_nodePositionLookup.TryGetValue(previous, out previousPosition)) {
                                counter += previousPosition;
                                break;
                            }
                            ++counter;
                        }
                    }
                    _nodePositionLookup[node] = counter;

                    // scan forward to see if we should force use of the bracket notation (i.e. [])
                    bool useBrackets = false;
                    if(counter == 1) {
                        for(XmlNode next = node.NextSibling; next != null; next = next.NextSibling) {
                            if(next.Name.EqualsInvariant(node.Name)) {
                                useBrackets = true;
                            }
                        }
                    }

                    // compute new location
                    location = new Location(location.Origin + "/" + node.Name);
                    if((counter > 1) || useBrackets) {
                        location = new Location(location.Origin + "[" + counter + "]");
                    }
                }
                break;
            }
            _path.Add(location);
        }

        private void PopNode() {
            _path.RemoveAt(_path.Count - 1);
        }

        private DekiScriptExpression TryCatch(DekiScriptExpression expr, bool scripted) {
            if(scripted) {
                return DekiScriptExpression.ReturnScope(Location, expr);
            }
            return expr;
        }

        private DekiScriptExpression Html(string container, string tag, string type, DekiScriptExpression expr) {
            DekiScriptExpression style = DekiScriptExpression.XmlElement(Location, null, DekiScriptExpression.Constant(tag), new[] { new DekiScriptXmlElement.Attribute(Location, null, DekiScriptExpression.Constant("type"), DekiScriptExpression.Constant(type)) }, expr);
            DekiScriptExpression head = DekiScriptExpression.XmlElement(Location, null, DekiScriptExpression.Constant(container), null, style);
            DekiScriptExpression html = DekiScriptExpression.XmlElement(Location, null, DekiScriptExpression.Constant("html"), null, head);
            return html;
        }
    }
}
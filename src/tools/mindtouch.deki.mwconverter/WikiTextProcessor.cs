/*
 * MindTouch MediaWiki Converter
 * Copyright (C) 2006-2008 MindTouch Inc.
 * www.mindtouch.com  oss@mindtouch.com
 *
 * For community documentation and downloads visit developer.mindtouch.com;
 * please review the licensing section.
 *
 * This library is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 2.1 of the License, or (at your option) any later version.
 * 
 * This library is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 * 
 * You should have received a copy of the GNU Lesser General Public
 * License along with this library; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
 * http://www.gnu.org/copyleft/lesser.html
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using MindTouch.Deki.Script.Compiler;
using MindTouch.Deki.Script.Expr;
using MindTouch.Dream;
using MindTouch.Tools;
using MindTouch.Xml;

namespace MindTouch.Deki.Converter {
    public static class WikiTextProcessor {

        //--- Types ---
        private enum ParserState {
            Text,           // regular text, no special processing
            External,       // processing [ *external* ]
            Internal
        }

        private enum ContextState {
            None,
            Argument,
            Expression,
            FirstExpressionThenArgument
        }

        public static class ExcludedTags {

            //--- Constants
            private static readonly List<String> _excludedTags = new List<string>(new string[] { "img", "pre", "embed", "script", "style", "applet", "input", "samp", "textarea", "nowiki", "h1", "h2", "h3", "h4", "h5", "h6" });
            private static readonly List<String> _excludedClasses = new List<string>(new string[] { "nowiki", "urlexpansion", "plain", "live", "script", "comment" });

            //--- Class Methods
            public static bool Contains(XmlNode node) {
                return Contains(node, false);
            }

            public static bool Contains(XmlNode node, bool checkParents) {
                if(node == null) {
                    return false;
                }
                string classAttrValue = String.Empty;
                if((null != node.Attributes) && (null != node.Attributes["class"])) {
                    classAttrValue = node.Attributes["class"].Value;
                }
                if(_excludedTags.Contains(node.LocalName.ToLowerInvariant()) || _excludedClasses.Contains(classAttrValue.ToLowerInvariant())) {
                    return true;
                } else {
                    if(checkParents && (null != node.ParentNode)) {
                        return Contains(node.ParentNode, true);
                    } else {
                        return false;
                    }
                }
            }
        }

        //--- Class Fields ---
        private static readonly Regex ID_REGEX = new Regex(@"^([a-zA-Z][a-zA-Z0-9_]*)$|^\$$", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Singleline);
        private static readonly Regex ARG_REGEX = new Regex(@"^([a-zA-Z0-9_]+)$", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Singleline);

        //--- Class Methods ---
        public static void Convert(Site site, XDoc doc, bool isTemplate) {
            XmlNamespaceManager ns = new XmlNamespaceManager(XDoc.XmlNameTable);
            ns.AddNamespace("m", "#mediawiki");
            DetectWikiTextLinks(doc.AsXmlNode);
            Convert(site, doc, isTemplate, doc.AsXmlNode, ContextState.None, ns);
        }

        private static void Convert(Site site, XDoc doc, bool isTemplate, XmlNode node, ContextState state, XmlNamespaceManager ns) {
            List<XmlNode> list = new List<XmlNode>();
            foreach(XmlNode child in node.ChildNodes) {
                list.Add(child);
            }
            ContextState original = state;
            bool firstChild = true;
            foreach(XmlNode child in list) {

                // only first arguments can be expressions
                if(original == ContextState.FirstExpressionThenArgument) {
                    if(!firstChild) {
                        state = ContextState.Argument;
                    } else {
                        state = ContextState.Expression;
                    }
                }
                firstChild = false;
                
                // determine what kinf of child element this is
                XDoc current = doc[child];
                if((child.NodeType == XmlNodeType.Element) && (child.NamespaceURI == "#mediawiki")) {
                    switch(child.LocalName) {
                    case "internal": {

                            // check if this element contains elements that would prevent it from being an internal link
                            if(current.AtPath(".//m:link | .//m:internal | .//m:external", ns).IsEmpty) {
                                Convert(site, doc, isTemplate, child, ContextState.Argument, ns);
                                StringBuilder code = new StringBuilder();
                                code.Append("mediawiki.internal").Append("(");
                                code.Append(AsArgument(child));
                                if(!string.IsNullOrEmpty(site.Language)) {
                                    code.Append(", ").Append(DekiScriptString.QuoteString(site.Language));
                                }
                                code.Append(")");
                                current.Replace(Scripted(code.ToString(), state, true));
                            } else {
                                Convert(site, doc, isTemplate, child, state, ns);
                                if(state == ContextState.Argument) {
                                    current.AddBefore("'[['");
                                    current.AddAfter("']]'");
                                } else {
                                    current.AddBefore("[[");
                                    current.AddAfter("]]");
                                }
                                current.ReplaceWithNodes(current);
                            }
                        }
                        break;
                    case "external": {

                            // check if this element contains elements that would prevent it from being an external link
                            if(current.AtPath(".//m:link | .//m:internal | .//m:external", ns).IsEmpty) {
                                Convert(site, doc, isTemplate, child, ContextState.Argument, ns);
                                StringBuilder code = new StringBuilder();
                                code.Append("mediawiki.external").Append("(");
                                code.Append(AsArgument(child));
                                code.Append(")");
                                current.Replace(Scripted(code.ToString(), state, true));
                            } else {
                                Convert(site, doc, isTemplate, child, state, ns);
                                if(state == ContextState.Argument) {
                                    current.AddBefore("'['");
                                    current.AddAfter("']'");
                                } else {
                                    current.AddBefore("[");
                                    current.AddAfter("]");
                                }
                                current.ReplaceWithNodes(current);
                            }
                        }
                        break;
                    case "link":
                        Convert(site, doc, isTemplate, child, state, ns);
                        switch(current["@type"].AsText) {
                        case "internal":
                            current.Replace(new XDoc("a").Attr("href", current["@href"].AsText).AddNodes(current));
                            break;
                        case "external":
                            current.Replace(new XDoc("a").Attr("class", "external").Attr("href", current["@href"].AsText).AddNodes(current));
                            break;
                        case "external-free":
                            if(state == ContextState.None) {
                                current.Replace(current["@href"].AsText);
                            } else {
                                current.Replace(QuoteString(current["@href"].AsText));
                            }
                            break;
                        case "external-ref":
                            current.Replace(new XDoc("a").Attr("class", "external").Attr("href", current["@href"].AsText));
                            break;
                        default:

                            // no idea what this link is, let's replace with a text version of itself
                            current.Replace(child.OuterXml);
                            break;
                        }
                        break;
                    case "image":
                        current.Replace(new XDoc("img").Attr("src", current["@href"].AsText).Attr("alt", current["@alt"].AsText).Attr("align", current["@align"].AsText));
                        break;
                    case "comment":
                        Convert(site, doc, isTemplate, child, state, ns);
                        current.Replace(new XDoc("span").Attr("class", "comment").AddNodes(current));
                        break;
                    case "nowiki":
                        current.Replace(new XDoc("span").Attr("class", "plain").AddNodes(current));
                        break;
                    case "extension":

                        // TODO: should the behavior be different depending on 'state'?
                        Convert(site, doc, isTemplate, child, state, ns);
                        if (!current["@value"].IsEmpty) {
                            current.Replace("{{" + current["@value"].AsText + "}}");
                        } else {
                            switch (current["@function"].AsText) {
                                case "math":
                                    current.Replace(new XDoc("pre").Attr("class", "script").Attr("function", "math.formula").Value(current.AsText));
                                    break;
                                case "kbd":
                                case "abbr":
                                case "object":
                                    current.Rename(current["@function"].AsText.ToLowerInvariant());
                                    current.RemoveAttr("function");
                                    break;
                                case "rss": {
                                        StringBuilder code = new StringBuilder();
                                        string[] rssParams = current.Contents.Split('|');
                                        code.Append("ajaxrss{");
                                        code.AppendFormat(" feed: '{0}' ", rssParams[0]);
                                        for (int i = 1; i < rssParams.Length; i++) {
                                            string rssParam = rssParams[i].Trim();
                                            int index = rssParam.IndexOf('=');
                                            if (index >= 0) {
                                                code.AppendFormat(", {0}: {1}", rssParam.Substring(0, index), QuoteString(rssParam.Substring(index + 1)));
                                            } else {
                                                code.AppendFormat(", {0}: true", rssParam);
                                            }
                                        }
                                        code.Append(" }");
                                        current.Replace(new XDoc("span").Attr("class", "script").Value(code.ToString()));
                                    }
                                    break;
                                case "title-override":
                                case "breadcrumbs":
                                    current.Remove();
                                    break;
                                default:
                                    current.Replace(child.OuterXml);
                                    break;
                            }
                        }
                        break;
                    case "magic": {
                            string code = null;
                            switch(current["@name"].AsText) {
                            case "CONTENTLANGUAGE":
                                code = "site.language";
                                break;
                            case "CURRENTDAY":
                                code = "date.format(date.now, '%d')";
                                break;
                            case "CURRENTDAY2":
                                code = "date.format(date.now, 'dd')";
                                break;
                            case "CURRENTDAYNAME":
                                code = "date.dayname(date.now)";
                                break;
                            case "CURRENTDOW":
                                code = "date.dayofweek(date.now)";
                                break;
                            case "CURRENTMONTH":
                                code = "date.month(date.now)";
                                break;
                            case "CURRENTMONTHABBREV":
                                code = "date.format(date.now, 'MMM')";
                                break;
                            case "CURRENTMONTHNAME":
                                code = "date.monthname(date.now)";
                                break;
                            case "CURRENTTIME":
                                code = "date.time(date.now)";
                                break;
                            case "CURRENTHOUR":
                                code = "date.format(date.now, 'HH')";
                                break;
                            case "CURRENTWEEK":
                                code = "date.week(date.now)";
                                break;
                            case "CURRENTYEAR":
                                code = "date.year(date.now)";
                                break;
                            case "CURRENTTIMESTAMP":
                                code = "date.format(date.now, 'yyyyMMddHHmmss')";
                                break;
                            case "PAGENAME":
                            case "PAGENAMEE":
                                code = "page.unprefixedpath";
                                break;
                            case "NUMBEROFARTICLES":
                                code = "site.pagecount";
                                break;
                            case "NUMBEROFUSERS":
                                code = "site.usercount";
                                break;
                            case "NAMESPACE":
                                code = "page.namespace";
                                break;
                            case "REVISIONDAY":
                                code = "date.format(page.date, '%d')";
                                break;
                            case "REVISIONDAY2":
                                code = "date.format(page.date, 'dd')";
                                break;
                            case "REVISIONMONTH":
                                code = "date.month(page.date)";
                                break;
                            case "REVISIONYEAR":
                                code = "date.year(page.date)";
                                break;
                            case "REVISIONTIMESTAMP":
                                code = "date.format(page.date, 'yyyyMMddHHmmss')";
                                break;
                            case "SITENAME":
                                code = "site.name";
                                break;
                            case "SERVER":
                                code = "site.uri";
                                break;
                            case "SERVERNAME":
                                code = "site.host";
                                break;
                            default:

                                // unrecognized magic word - use the mediawiki magicword extension
                                code = String.Format("mediawiki.variable('{0}')", DekiScriptString.EscapeString(current["@name"].AsText));
                                break;
                            }
                            current.Replace(Scripted(code, state, true));
                        }
                        break;
                    case "interwiki": {
                            string code = String.Format("mediawiki.interwiki('{0}', '{1}', '{2}')", 
                                DekiScriptString.EscapeString(child.Attributes["prefix"].Value),
                                DekiScriptString.EscapeString(child.Attributes["path"].Value + (((XmlElement)child).HasAttribute("fragment") ? ("#" + child.Attributes["fragment"].Value) : string.Empty)), 
                                DekiScriptString.EscapeString(child.InnerText));
                            current.Replace(Scripted(code, state, true));
                            break;
                    }
                    case "function": {
                            Convert(site, doc, isTemplate, child, ContextState.Argument, ns);
                            StringBuilder code = new StringBuilder();
                            bool withLang = false;
                            switch (current["@name"].AsText) {
                            case "formatnum": {
                                    code.Append("num.format");

                                    // add one more parameter
                                    XmlElement arg = child.OwnerDocument.CreateElement("arg", "#mediawiki");
                                    arg.AppendChild(child.OwnerDocument.CreateTextNode(QuoteString("N")));
                                    child.AppendChild(arg);
                                }
                                break;
                            case "fullurl":
                                code.Append("wiki.uri");
                                break;
                            case "lc":
                                code.Append("string.tolower");
                                break;
                            case "lcfirst":
                                code.Append("string.tolowerfirst");
                                break;
                            case "padleft":
                                code.Append("string.padleft");
                                break;
                            case "padright":
                                code.Append("string.padright");
                                break;
                            case "uc":
                                code.Append("string.toupper");
                                break;
                            case "ucfirst":
                                code.Append("string.toupperfirst");
                                break;
                            case "urlencode":
                                code.Append("web.uriencode");
                                break;
                            case "int":
                                code.Append("wiki.page");
                                break;
                            case "localurl":
                            case "localurle":
                                withLang = true;
                                code.Append(At("mediawiki", current["@name"].AsText));
                                break;
                            default:
                                code.Append(At("mediawiki", current["@name"].AsText));
                                break;
                            }

                            // append parameters
                            code.Append("(");
                            if (withLang) {
                                code.Append(null == site.Language ? "_" : DekiScriptString.QuoteString(site.Language));
                            }
                            bool first = true && !withLang;
                            foreach(XDoc arg in current.AtPath("m:arg", ns)) {
                                if(!first) {
                                    code.Append(", ");
                                }
                                first = false;
                                code.Append(AsArgument(arg.AsXmlNode));
                            }
                            code.Append(")");
                            current.Replace(Scripted(code.ToString(), state, true));
                        }
                        break;
                    case "expression": {
                            StringBuilder code = new StringBuilder();
                            switch(current["@name"].AsText) {
                            case "#expr":
                                Convert(site, doc, isTemplate, child, ContextState.Expression, ns);
                                code.Append(AsExpression(current.AtPath("m:arg", ns).AsXmlNode));
                                break;
                            case "#if": {
                                    Convert(site, doc, isTemplate, child, ContextState.Argument, ns);
                                    code.Append("string.trim(");
                                    code.Append(AsArgument(current.AtPath("m:arg[1]", ns).AsXmlNode));
                                    code.Append(") !== '' ? ");
                                    code.Append(WebHtml(current.AtPath("m:arg[2]", ns).AsXmlNode));
                                    code.Append(" : ");
                                    code.Append(WebHtml(current.AtPath("m:arg[3]", ns).AsXmlNode));
                                }
                                break;
                            case "#ifeq": {
                                    Convert(site, doc, isTemplate, child, ContextState.Argument, ns);
                                    code.Append(AsArgument(current.AtPath("m:arg[1]", ns).AsXmlNode));
                                    code.Append(" == ");
                                    code.Append(AsArgument(current.AtPath("m:arg[2]", ns).AsXmlNode));
                                    code.Append(" ? ");
                                    code.Append(WebHtml(current.AtPath("m:arg[3]", ns).AsXmlNode));
                                    code.Append(" : ");
                                    code.Append(WebHtml(current.AtPath("m:arg[4]", ns).AsXmlNode));
                                }
                                break;
                            case "#ifexpr": {
                                    Convert(site, doc, isTemplate, child, ContextState.FirstExpressionThenArgument, ns);
                                    code.Append(AsExpression(current.AtPath("m:arg[1]", ns).AsXmlNode));
                                    code.Append(" ? ");
                                    code.Append(WebHtml(current.AtPath("m:arg[2]", ns).AsXmlNode));
                                    code.Append(" : ");
                                    code.Append(WebHtml(current.AtPath("m:arg[3]", ns).AsXmlNode));
                                }
                                break;
                            case "#ifexist": {
                                    bool simple;
                                    Convert(site, doc, isTemplate, child, ContextState.Argument, ns);
                                    code.Append("wiki.pageexists(");
                                    string title = AsPathArgument(site, current.AtPath("m:arg[1]", ns), false, out simple);
                                    code.Append(simple ? ("'" + title + "'") : title);
                                    code.Append(") ? ");
                                    code.Append(WebHtml(current.AtPath("m:arg[2]", ns).AsXmlNode));
                                    code.Append(" : ");
                                    code.Append(WebHtml(current.AtPath("m:arg[3]", ns).AsXmlNode));
                                }
                                break;
                            case "#switch":
                            case "#time":
                            case "#rel2abs":
                            case "#titleparts":
                            case "#iferror":

                            // TODO (steveb): missing code, falling through to default case

                            default:
                                code.Append(At("mediawiki", current["@name"].AsText));

                                // append parameters
                                code.Append("(");
                                bool first = true;
                                foreach(XDoc arg in current.AtPath("m:arg", ns)) {
                                    if(!first) {
                                        code.Append(", ");
                                    }
                                    first = false;
                                    code.Append(AsArgument(arg.AsXmlNode));
                                }
                                code.Append(")");
                                break;
                            }
                            current.Replace(Scripted(code.ToString(), state, false));
                        }
                        break;
                    case "template": {
                            Convert(site, doc, isTemplate, child, ContextState.Argument, ns);
                            StringBuilder code = new StringBuilder();

                            // check if we need to decode the page name
                            bool simpleTitle;
                            bool simpleArgs;
                            string title = AsPathArgument(site, current.AtPath("m:name", ns), true, out simpleTitle);
                            XDoc args = current.AtPath("m:arg", ns);
                            string argCode = AppendTemplateArguments(args, out simpleArgs);

                            // append parameters
                            if(simpleTitle && simpleArgs) {
                                code.Append("template.");
                                code.Append(title.Substring(1, title.Length - 2));
                                if(string.IsNullOrEmpty(argCode)) {
                                    code.Append("()");
                                } else if(argCode.StartsWith("[") && argCode.EndsWith("]")) {
                                    code.Append("(" + argCode.Substring(2, argCode.Length - 4) + ")");
                                } else {
                                    code.Append(argCode);
                                }
                            } else {
                                code.Append("wiki.template").Append("(");
                                code.Append(title);
                                if(!string.IsNullOrEmpty(argCode)) {
                                    code.Append(", ");
                                    code.Append(argCode);
                                }
                                code.Append(")");
                            }
                            current.Replace(Scripted(code.ToString(), state, true));
                        }
                        break;
                    case "arg":
                        Convert(site, doc, isTemplate, child, state, ns);
                        break;
                    case "name":
                        Convert(site, doc, isTemplate, child, state, ns);
                        break;
                    case "ref": {
                            if(isTemplate || (state != ContextState.None)) {
                                Convert(site, doc, isTemplate, child, state, ns);
                                string code;
                                int? index = current["@index"].AsInt;
                                if(index != null) {
                                    code = "$" + (index.Value - 1);
                                } else {
                                    code = "$" + current["@name"].AsText;
                                }
                                switch(state) {
                                case ContextState.None:
                                    if(current["@alt"].IsEmpty) {
                                        current.Replace(Scripted("web.html(" + code + ")", state, true));
                                    } else {
                                        current.Replace(Scripted("web.html(" + code + " ?? " + AsArgument(current.AsXmlNode) + ")", state, true));
                                    }
                                    break;
                                default:
                                    if(current["@alt"].IsEmpty) {
                                        current.Replace(Scripted(code, state, true));
                                    } else {
                                        current.Replace(Scripted(code + " ?? " + AsArgument(current.AsXmlNode), state, false));
                                    }
                                    break;
                                }
                            } else {
                                string code = current["@index"].AsText ?? current["@name"].AsText;
                                if(!current["@alt"].IsEmpty) {
                                    code += "|" + current["@alt"].AsText;
                                }
                                current.Replace(new XDoc("span").Attr("class", "plain").Value("{{{" + code + "}}}"));
                            }
                        }
                        break;
                    }
                } else if(child.NodeType == XmlNodeType.Element) {
                    Convert(site, doc, isTemplate, child, state, ns);

                    // loop over attribute nodes
                    foreach(XDoc attribute in current.AtPath("@m:*", ns)) {
                        XDoc code = XDocFactory.From("<code xmlns:mediawiki=\"#mediawiki\">" + attribute.Contents + "</code>", MimeType.XML);
                        Convert(site, code, isTemplate, code.AsXmlNode, ContextState.Argument, ns);
                        attribute.Parent.Attr(attribute.Name, "{{" + AsArgument(code.AsXmlNode) + "}}");
                        attribute.Remove();
                    }
                } else {
                    Convert(site, doc, isTemplate, child, state, ns);
                    if((state == ContextState.Argument) && ((child.NodeType == XmlNodeType.Text) || (child.NodeType == XmlNodeType.Whitespace) || (child.NodeType == XmlNodeType.SignificantWhitespace))) {
                        if(!string.IsNullOrEmpty(child.Value)) {
                            double value;
                            if(double.TryParse(child.Value, out value)) {
                                current.Replace(child.Value);
                            } else {
                                current.Replace(QuoteString(child.Value));
                            }
                        } else {
                            current.Remove();
                        }
                    }
                }
            }
        }

        private static string QuoteString(string text) {
            return "'" + DekiScriptString.EscapeString(text) + "'";
        }

        private static DekiScriptExpression SafeParse(string code) {
            DekiScriptExpression expr = null;
            try {
                expr = DekiScriptParser.Parse(Location.Start, code);
            } catch { }
            return expr;
        }

        private static string WebHtml(XmlNode node) {
            string arg = AsArgument(node);
            DekiScriptExpression expr = SafeParse(arg);
            if((expr is DekiScriptNumber)) {
                return arg;
            } else if((expr is DekiScriptString) && !((DekiScriptString)expr).Value.Contains("&lt;")) {
                return arg;
            } else {
                return "web.html(" + arg + ")";
            }
        }

        private static string AsArgument(XmlNode node) {
            if(node == null) {
                return "''";
            }
            StringBuilder arg = new StringBuilder();
            AsStringArgument(node, arg);
            return arg.ToString().Replace("' .. '", "");
        }

        private static void AsStringArgument(XmlNode node, StringBuilder arg) {
            if(node.ChildNodes.Count > 0) {
                foreach(XmlNode child in node.ChildNodes) {
                    switch(child.NodeType) {
                    case XmlNodeType.Element:
                        if(arg.Length > 0) {
                            arg.Append(" .. ");
                        }
                        arg.Append("'<");
                        arg.Append(DekiScriptString.EscapeString(child.LocalName));
                        foreach(XmlAttribute attribute in ((XmlElement)child).Attributes) {
                            arg.Append(" ");

                            // check if attribute contains dekiscript code
                            if(StringUtil.StartsWithInvariant(attribute.Value, "{{") && StringUtil.EndsWithInvariant(attribute.Value, "}}")) {
                                arg.Append(attribute.LocalName);
                                arg.Append("=\"' .. ");
                                arg.Append(Scripted(attribute.Value.Substring(2, attribute.Value.Length - 4), ContextState.Argument, false));
                                arg.Append(" .. '\"");
                            } else {
                                arg.Append(DekiScriptString.EscapeString(attribute.OuterXml));
                            }
                        }
                        if(child.ChildNodes.Count > 0) {
                            arg.Append(">'");
                            AsStringArgument(child, arg);
                            arg.Append(" .. ");
                            arg.Append("'</");
                            arg.Append(DekiScriptString.EscapeString(child.LocalName));
                            arg.Append(">'");
                        } else {
                            arg.Append("/>'");
                        }
                        break;
                    case XmlNodeType.Text:
                    case XmlNodeType.Whitespace:
                    case XmlNodeType.SignificantWhitespace:
                        if(!string.IsNullOrEmpty(child.Value)) {
                            if(arg.Length > 0) {
                                arg.Append(" .. ");
                            }
                            arg.Append(child.Value);
                        }
                        break;
                    }
                }
            } else {
                arg.Append("''");
            }
        }

        private static string AsExpression(XmlNode node) {
            if(node == null) {
                return "_";
            }
            StringBuilder arg = new StringBuilder();
            AsExpression(node, arg);
            return arg.ToString();
        }

        private static void AsExpression(XmlNode node, StringBuilder arg) {
            if(node.ChildNodes.Count > 0) {
                foreach(XmlNode child in node.ChildNodes) {
                    switch(child.NodeType) {
                    case XmlNodeType.Element:
                        arg.Append("'<");
                        arg.Append(DekiScriptString.EscapeString(child.LocalName));
                        foreach(XmlAttribute attribute in ((XmlElement)child).Attributes) {
                            arg.Append(" ");

                            // check if attribute contains dekiscript code
                            if(StringUtil.StartsWithInvariant(attribute.Value, "{{") && StringUtil.EndsWithInvariant(attribute.Value, "}}")) {
                                arg.Append(attribute.LocalName);
                                arg.Append("=\"' .. ");
                                arg.Append(Scripted(attribute.Value.Substring(2, attribute.Value.Length - 4), ContextState.Argument, false));
                                arg.Append(" .. '\"");
                            } else {
                                arg.Append(DekiScriptString.EscapeString(attribute.OuterXml));
                            }
                        }
                        if(child.ChildNodes.Count > 0) {
                            arg.Append(">'");
                            AsExpression(child, arg);
                            arg.Append(" .. ");
                            arg.Append("'</");
                            arg.Append(DekiScriptString.EscapeString(child.LocalName));
                            arg.Append(">'");
                        } else {
                            arg.Append("/>'");
                        }
                        break;
                    case XmlNodeType.Text:
                    case XmlNodeType.Whitespace:
                    case XmlNodeType.SignificantWhitespace:
                        if(!string.IsNullOrEmpty(child.Value)) {
                            arg.Append(child.Value);
                        }
                        break;
                    }
                }
            } else {
                arg.Append("''");
            }
        }

        private static string Scripted(string code, ContextState state, bool isAtomic) {
            if(state == ContextState.None) {
                code = "{{" + code + "}}";
            } else if(!isAtomic) {
                DekiScriptExpression expr = SafeParse(code);
                if((expr == null) || (expr is DekiScriptBinary) || (expr is DekiScriptTernary)) {
                    code = "(" + code + ")";
                }
            }
            return code;
        }

        private static string At(string container, string key) {
            if(ID_REGEX.IsMatch(key)) {
                return string.Format("{0}.{1}", container, key);
            } else {
                int i;
                if(int.TryParse(key, out i)) {
                    return string.Format("{0}[{1}]", container, key);
                } else {
                    return string.Format("{0}[{1}]", container, QuoteString(key));
                }
            }
        }

        private static void DetectWikiTextLinks(XmlNode node) {
            Stack<XmlNode> stack = new Stack<XmlNode>();
            ParserState state = ParserState.Text;
            XmlNode start = null;
            XmlNode current = node;
            bool first = false;
            while(current != null) {
                if(current is XmlText) {

                    // parse characters
                    string text = current.Value;
                    int startIndex = (first ? ((state == ParserState.Internal) ? 2 : 1) : 0);
                    first = false;
                    for(int i = startIndex; i < text.Length; ++i) {
                        first = false;
                        switch(text[i]) {
                        case '[':
                            switch(state) {
                            case ParserState.Internal:

                                // restart internal link

                            case ParserState.External:

                                // restart external link

                            case ParserState.Text:
                                if((StringAt(text, i + 1) == '[') && (StringAt(text, i + 2) != '[')) {
                                    state = ParserState.Internal;

                                    // split the current text node
                                    current = ((XmlText)current).SplitText(i + 2);
                                    first = true;
                                    start = current;
                                    goto continue_while_loop;
                                } else if(StringAt(text, i + 1) != '[') {
                                    state = ParserState.External;

                                    // split the current text node
                                    current = ((XmlText)current).SplitText(i + 1);
                                    first = true;
                                    start = current;
                                    goto continue_while_loop;
                                } else {

                                    // reset state
                                    state = ParserState.Text;
                                    start = null;
                                }
                                break;
                            }
                            break;
                        case ']':
                            switch(state) {
                            case ParserState.Text:

                                // nothing to do
                                break;
                            case ParserState.Internal:

                                // check if link is structurally sound
                                if(object.ReferenceEquals(current.ParentNode, start.ParentNode)) {
                                    XmlNode next;
                                    bool external;
                                    if(StringAt(text, i + 1) == ']') {

                                        // make an internal link
                                        next = ((XmlText)current).SplitText(i);
                                        external = false;
                                    } else {

                                        // make an external link
                                        next = ((XmlText)current).SplitText(i);
                                        external = true;
                                    }
                                    WrapNodes(external, start, next);

                                    // reset state
                                    state = ParserState.Text;
                                    start = null;
                                    current = next;
                                    goto continue_while_loop;
                                }

                                // reset state
                                state = ParserState.Text;
                                start = null;
                                break;
                            case ParserState.External:

                                // check if the external link is structurally sound
                                if(object.ReferenceEquals(current.ParentNode, start.ParentNode)) {

                                    // make an external link
                                    XmlNode next = ((XmlText)current).SplitText(i);
                                    WrapNodes(true, start, next);

                                    // reset state
                                    state = ParserState.Text;
                                    start = null;
                                    current = next;
                                    goto continue_while_loop;
                                }

                                // reset state
                                state = ParserState.Text;
                                start = null;
                                break;
                            }
                            break;
                        }
                    }
                } else {

                    // check if we are on an element that doesn't require conversion
                    if(!IsExcluded(current) && current.HasChildNodes) {
                        stack.Push(current);
                        current = current.FirstChild;
                        continue;
                    }
                }

                // move to next node
                current = current.NextSibling;
                while((current == null) && (stack.Count > 0)) {
                    current = stack.Pop().NextSibling;
                }
            continue_while_loop:
                continue;
            }
        }

        private static void WrapNodes(bool external, XmlNode start, XmlNode end) {

            // remove '[[' or '[' depending if it's an internal or external linnk
            if(external) {
                start.PreviousSibling.Value = start.PreviousSibling.Value.Substring(0, start.PreviousSibling.Value.Length - 1);
                end.Value = end.Value.Substring(1);
            } else {
                start.PreviousSibling.Value = start.PreviousSibling.Value.Substring(0, start.PreviousSibling.Value.Length - 2);
                end.Value = end.Value.Substring(2);
            }

            // create wrapper node
            XmlNode wrapper = start.OwnerDocument.CreateElement("mediawiki", external ? "external" : "internal", "#mediawiki");
            start.ParentNode.InsertBefore(wrapper, start);

            // move nodes
            for(XmlNode current = start; current != end; ) {
                XmlNode tmp = current;
                current = current.NextSibling;
                wrapper.AppendChild(tmp);
            }
        }

        private static bool IsExcluded(XmlNode node) {
            return (node.NodeType != XmlNodeType.Element) || ExcludedTags.Contains(node);
        }

        private static char StringAt(string text, int index) {
            if(index >= text.Length) {
                return char.MinValue;
            }
            return text[index];
        }

        private static string AsPathArgument(Site site, XDoc arg, bool useTemplateNamespace, out bool simple) {
            simple = false;
            string name = AsArgument(arg.AsXmlNode);
            if((name.Length >= 3) && (SafeParse(name.Substring(1, name.Length - 2)) is DekiScriptVar)) {
                simple = true;
                return name;
            }
            if(SafeParse(name) is DekiScriptString) {
                if(useTemplateNamespace && name.Substring(1).StartsWith("Template:")) {
                    name = "'" + name.Substring(10, name.Length - 11) + "'";
                    if(SafeParse(name.Substring(1, name.Length - 2)) is DekiScriptVar) {
                        simple = true;
                        return name;
                    }
                }
                return name;
            }
            if(useTemplateNamespace) {
                if(string.IsNullOrEmpty(site.Language)) {
                    return "mediawiki.path(" + name + ")";
                } else {
                    return "mediawiki.path(" + name + ", " + DekiScriptString.QuoteString(site.Language) + ")";
                }
            } else {
                return "mediawiki.localurl(" + (string.IsNullOrEmpty(site.Language) ? "_" : DekiScriptString.QuoteString(site.Language)) + ", " + name + ")";
            }
        }

        private static string AppendTemplateArguments(XDoc args, out bool simple) {
            StringBuilder code = new StringBuilder();
            if(args.IsEmpty) {
                simple = true;
            } else {
                List<XDoc> list = args.ToList();
                simple = true;

                // check if arguments are neither strings nor numbers
                foreach(XDoc arg in list) {
                    string value = AsArgument(arg.AsXmlNode);
                    DekiScriptExpression expr = SafeParse(value);
                    if(!(expr is DekiScriptString) && !(expr is DekiScriptNumber)) {
                        simple = false;
                        break;
                    }
                }

                // check if all entries are simple strings
                if(simple) {
                    bool listOnly = true;
                    Hashtable map = new Hashtable();
                    for(int i = 0; i < list.Count; i++) {
                        DekiScriptExpression expr = SafeParse(AsArgument(list[i].AsXmlNode));
                        if(expr is DekiScriptString) {
                            string arg = ((DekiScriptString)expr).Value;
                            int equalIndex = arg.IndexOf("=");
                            if(equalIndex > 0) {
                                string id = arg.Substring(0, equalIndex).Trim();
                                if(ARG_REGEX.IsMatch(id)) {
                                    listOnly = false;
                                    string value = arg.Substring(equalIndex + 1, arg.Length - equalIndex - 1);
                                    double number;
                                    if(double.TryParse(value, out number)) {
                                        map[id] = number;
                                    } else {
                                        map[id] = value;
                                    }
                                } else {
                                    map[i.ToString()] = arg;
                                }
                            } else {
                                map[i.ToString()] = arg;
                            }
                        } else if(expr is DekiScriptNumber) {
                            double arg = ((DekiScriptNumber)expr).Value;
                            map[i.ToString()] = arg;
                        } else {
                            throw new Exception("should never happen");
                        }
                    }

                    // check if all indices are simple numbers
                    if(listOnly) {
                        ArrayList items = new ArrayList();
                        for(int i = 0; i < map.Count; ++i) {
                            items.Add(map[i.ToString()]);
                        }
                        code.Append(new DekiScriptList(items).ToString());
                    } else {
                        code.Append(new DekiScriptMap(map).ToString());
                    }
                } else {
                    code.Append("mediawiki.args([");
                    bool first = true;
                    foreach(XDoc arg in args) {
                        if(!first) {
                            code.Append(", ");
                        }
                        first = false;
                        code.Append(AsArgument(arg.AsXmlNode));
                    }
                    code.Append("])");
                }
            }
            return code.ToString();
        }
    }
}

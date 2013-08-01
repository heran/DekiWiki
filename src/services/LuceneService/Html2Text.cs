/*
 * MindTouch Core - open source enterprise collaborative networking
 * Copyright (c) 2006-2010 MindTouch Inc.
 * www.mindtouch.com  oss@mindtouch.com
 *
 * For community documentation and downloads visit developer.mindtouch.com;
 * please review the licensing section.
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License along
 * with this program; if not, write to the Free Software Foundation, Inc.,
 * 59 Temple Place - Suite 330, Boston, MA 02111-1307, USA.
 * http://www.gnu.org/copyleft/gpl.html
 */
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using MindTouch.Xml;

namespace MindTouch.LuceneService {
    public class Html2Text {

        //--- Types ---
        private class VisitState {

            //--- Fields ---
            private readonly StringBuilder _accumulator = new StringBuilder();
            private bool _linefeed;
            public XmlNode BeingFiltered;

            //--- Methods ---
            public void Break() {
                if(!_linefeed && _accumulator.Length > 0) {
                    _accumulator.AppendLine();
                }
                _linefeed = true;
            }

            public void Append(string text) {
                _linefeed = false;
                _accumulator.Append(text);
            }

            public override string ToString() {
                return _accumulator.ToString();
            }
        }

        //--- Class Fields ---
        private static readonly HashSet<string> _inlineElements = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase) {
            "A",
            "ABBR",
            "B",
            "BASEFONT",
            "BIG",
            "CITE",
            "CODE",
            "DFN",
            "EM",
            "FONT",
            "I",
            "KBD",
            "S",
            "SAMP",
            "SMALL",
            "SPAN",
            "STRIKE",
            "STRONG",
            "TT",
            "U",
            "VAR",
        };
        private static readonly HashSet<string> _removeElements = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase) {
            "SCRIPT",
            "STYLE",
        };

        //--- Methods ---
        public string Convert(XDoc html) {
            if(html == null || html.IsEmpty) {
                return "";
            }
            var state = new VisitState();
            var body = html["body[not(@target)]"];
            foreach(var node in body.VisitOnly(x => IncludeNode(x, state), x => CheckBlock(x, state))) {
                if(CheckBlock(node, state)) {
                    continue;
                }
                switch(node.AsXmlNode.NodeType) {
                case XmlNodeType.CDATA:
                case XmlNodeType.Whitespace:
                case XmlNodeType.SignificantWhitespace:
                case XmlNodeType.Text:
                    state.Append(node.AsText);
                    break;
                }
            }
            return state.ToString();
        }

        private bool CheckBlock(XDoc node, VisitState state) {
            if(!(node.AsXmlNode is XmlElement)) {
                return false;
            }
            if(node.AsXmlNode != state.BeingFiltered    // if the current node is not being filtered
                && !_inlineElements.Contains(node.Name) // and it's not an inline element
            ) {
                state.Break();
            }
            return true;
        }

        private bool IncludeNode(XDoc node, VisitState state) {
            var include = true;
            if(node.AsXmlNode is XmlElement && _removeElements.Contains(node.Name)) {

                // node is a filtered element
                include = false;
            } else {
                var nodeClass = node["@class"].AsText;
                if(nodeClass != null) {

                    // node is an element that might have the noindex class
                    include = !nodeClass.EqualsInvariant("noindex");
                }
            }

            // record the node as filtered depending on the include state
            state.BeingFiltered = include ? null : node.AsXmlNode;
            return include;
        }
    }
}

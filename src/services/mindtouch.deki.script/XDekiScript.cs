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

using MindTouch.Dream;
using MindTouch.Xml;

namespace MindTouch.Deki.Script {
    public class XDekiScript : XDoc {

        //--- Constants ---
        public const string ScriptNS = "http://mindtouch.com/2007/dekiscript";
        public readonly static MimeType DEKISCRIPTXML = new MimeType("application/x.dekiscript+xml", System.Text.Encoding.UTF8);

        //--- Class Methods ---
        public static XDekiScript LoadFrom(string filename) {
            XDoc doc = XDocFactory.LoadFrom(filename, MimeType.XML);
            if(doc == null) {
                return null;
            }
            return new XDekiScript(doc);
        }

        //--- Constructors ---
        public XDekiScript(XDoc doc) : base(doc) {
            if(!doc.IsEmpty) {

                // check if the "eval" prefix is already defined
                if(AsXmlNode.GetNamespaceOfPrefix("eval") == null) {
                    UsePrefix("eval", ScriptNS);
                }
            }
        }

        public XDekiScript() : base("html") {
            Attr("xmlns:eval", ScriptNS);
            UsePrefix("eval", ScriptNS);
            Elem("head");
            Elem("body");
            Elem("tail");
        }

        //---- Properties ---
        public XDoc Head {
            get {
                XDoc result = this["head"];
                if(result.IsEmpty) {
                    Root.Elem("head");
                    result = this["head"];
                }
                return result; 
            }
        }

        public XDoc Body {
            get {
                XDoc result = this["body[not(@target)]"];
                if(result.IsEmpty) {
                    Root.Elem("body");
                    result = this["body[not(@target)]"];
                }
                return result;
            }
        }

        public XDoc Tail {
            get {
                XDoc result = this["tail"];
                if(result.IsEmpty) {
                    Root.Elem("tail");
                    result = this["tail"];
                }
                return result;
            }
        }
    }
}
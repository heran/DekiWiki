/*
 * MindTouch Core - open source enterprise collaborative networking
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

using MindTouch.Xml;

namespace MindTouch.Documentation.Util {
    public static class HtmlXDocExtensions {

        //--- Extension Methods ---
        public static XDoc Section(this XDoc doc, int level, string sectionClass, string heading, XDoc body) {
            if(!body.IsEmpty) {
                doc
                    .StartSection(level, sectionClass, heading)
                    .AddNodes(body)
                    .EndSection();
            }
            return doc;
        }

        public static XDoc StartSection(this XDoc doc, int level, string sectionClass, string heading) {
            return doc
                .Start("div").Attr("class", sectionClass)
                .Heading(level, heading)
                .Start("div").Attr("class", "sectionbody");
        }

        public static XDoc StartSection(this XDoc doc, string sectionClass) {
            return doc
                .Start("div").Attr("class", sectionClass)
                .Start("div").Attr("class", "sectionbody");
        }

        public static XDoc EndSection(this XDoc doc) {
            return doc.End().End();
        }

        public static XDoc Heading(this XDoc doc, int level, string value) {
            return doc.Elem("H" + (level + 1), value);
        }

        public static XDoc Div(this XDoc doc, string divClass, XDoc value) {
            return value.IsEmpty ? doc : doc.Start("div").Attr("class", divClass).AddNodes(value).End();
        }

        public static XDoc Div(this XDoc doc, string divClass, string value) {
            return string.IsNullOrEmpty(value) ? doc : doc.Start("div").Attr("class", divClass).Value(value).End();
        }

        public static XDoc StartSpan(this XDoc doc, string spanClass) {
            return doc.Start("span").Attr("class", spanClass);
        }
        
        public static XDoc EndSpan(this XDoc doc) {
            return doc.End();
        }

        public static XDoc NameValueLine(this XDoc doc, string divClass, string name, string value) {
            return doc.Start("div")
                .Attr("class", divClass)
                .Start("span")
                    .Attr("class", "namevaluetitle")
                    .Elem("b", name + ": ")
                .End()
                .Start("span")
                    .Attr("class", "namevaluecontent")
                    .Value(value)
                .End()
            .End();
        }

        public static XDoc StartNameValueLine(this XDoc doc, string divClass, string name) {
            return doc.Start("div")
                .Attr("class", divClass)
                .Start("span")
                    .Attr("class", "namevaluetitle")
                    .Elem("b", name + ": ")
                .End()
                .Start("span")
                    .Attr("class", "namevaluecontent");
        }

        public static XDoc StartNameValueBlock(this XDoc doc, string divClass, string name) {
            return doc.Start("div")
                .Attr("class", divClass)
                .Start("span")
                    .Attr("class", "namevaluetitle")
                    .Elem("b", name + ": ")
                .End()
                .Start("div")
                    .Attr("class", "namevaluecontent");
        }

        public static XDoc EndNameValue(this XDoc doc) {
            return doc.End().End();
        }

        public static XDoc NameValueLine(this XDoc doc, string name, XDoc value) {
            return doc.Start("div").Elem("b", name + ": ").Add(value).End();
        }

        public static XDoc Link(this XDoc doc, string href, string value) {
            return doc.Start("a").Attr("href.path", href).Value(value).End();
        }

        public static XDoc CSharpBlock(this XDoc doc, string body) {
            return doc
                .Start("div")
                .Attr("class", "sig")
                .Start("pre").Attr("class", "deki-transform").Attr("function", "syntax.CSharp").Value(body).End()
                .End();
        }
    }
}
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
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using MindTouch.Deki.Script.Expr;
using MindTouch.Dream;
using MindTouch.IO;
using MindTouch.Tasking;
using MindTouch.Xml;

namespace MindTouch.Deki.Script.Runtime.Library {
    public static partial class DekiScriptLibrary {

        //--- Constants ---
        public static readonly TimeSpan DEFAULT_WEB_TIMEOUT = TimeSpan.FromSeconds(60);

        // NOTE (steveb): XSS vulnerability check: detect 'expressions', 'url(' and 'http(s)://' links in style attributes
        private static readonly Regex STYLE_XSS_CHECK = new Regex(@"(?:expression|tps*://|url\s*\().*", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

        //--- Class Fields ---
        public static long InsertTextLimit = 512 * 1024;
        public static double MinCacheTtl = 5 * 60;
        private static Sgml.SgmlDtd _htmlEntitiesDtd;
        private static readonly Dictionary<XUri, string> _webTextCache = new Dictionary<XUri, string>();

        //--- Class Methods ---
        [DekiScriptFunction("web.pre", "Insert pre-formatted text.", IsIdempotent = true)]
        public static XDoc WebPre(
            [DekiScriptParam("text to insert")] object text
        ) {
            string value;
            if(text is XDoc) {
                value = XmlFormat((XDoc)text, null);
            } else if(!(text is string)) {
                value = JsonFormat(text);
            } else {
                value = (string)text;
            }
            return new XDoc("html").Start("body").Start("pre").Value(value).End().End();
        }

        [DekiScriptFunction("web.link", "Insert a hyperlink.", IsIdempotent = true)]
        public static XDoc WebLink(
            [DekiScriptParam("link uri")] string uri,
            [DekiScriptParam("link contents; can be text, an image, or another document (default: link uri)", true)] object text,
            [DekiScriptParam("link hover title (default: none)", true)] string title,
            [DekiScriptParam("link target (default: none)", true)] string target
        ) {

            // check __settings.nofollow if links should be followed or not
            bool nofollow = true;
            DreamContext context = DreamContext.CurrentOrNull;
            if(context != null) {
                DekiScriptEnv env = context.GetState<DekiScriptEnv>();
                if(env != null) {

                    // TODO (steveb): this should be stored in the runtime instead!
                    DekiScriptBool setting = env.Vars.GetAt(new[] { DekiScriptEnv.SETTINGS, "nofollow" }) as DekiScriptBool;
                    if(setting != null) {
                        nofollow = setting.AsBool() ?? true;
                    }
                }
            }

            XDoc result = DekiScriptLiteral.FromNativeValue(text ?? uri).AsEmbeddableXml(true);
            XDoc body = result["body[not(@target)]"];
            result.Start("body").Start("a");
            if(nofollow) {
                result.Attr("rel", "custom nofollow");
            } else {
                result.Attr("rel", "custom");
            }
            result.Attr("href", WebCheckUri(uri)).Attr("title", title).Attr("target", target).AddNodes(body).End().End();
            body.Remove();
            return result;
        }

        [DekiScriptFunction("web.image", "Insert an image.", IsIdempotent = true)]
        public static XDoc WebImage(
            [DekiScriptParam("image uri")] string uri,
            [DekiScriptParam("image width", true)] float? width,
            [DekiScriptParam("image height", true)] float? height,
            [DekiScriptParam("image alternative text", true)] string text
        ) {
            XDoc result = new XDoc("html").Start("body");
            if(uri.EndsWithInvariantIgnoreCase(".svg")) {
                result.Start("iframe").Attr("marginwidth", "0").Attr("marginheight", "0").Attr("hspace", "0").Attr("vspace", "0").Attr("frameborder", "0").Attr("scrolling", "no");
            } else {
                result.Start("img");
            }
            result.Attr("src", WebCheckUri(uri));
            result.Attr("width", WebSize(width));
            result.Attr("height", WebSize(height));
            result.Attr("alt", text);
            result.Attr("title", text);
            result.End();
            result.End();
            return result;
        }

        [DekiScriptFunction("web.size", "Convert numeric size to text.", IsIdempotent = true)]
        public static string WebSize(
            [DekiScriptParam("value to convert (0.75 = \"75%\", 0.999 = \"100%\", 1.0 = \"1px\"", true)] float? size
        ) {
            if(!size.HasValue) {
                return null;
            }
            if(size < 1.0f) {
                return ((int)(size.Value * 100 + 0.5f)) + "%";
            }
            return ((int)(size.Value + 0.5f)) + "px";
        }

        [DekiScriptFunction("web.style", "Create text for a 'style' attribute.", IsIdempotent = true)]
        public static string WebStyle(
            [DekiScriptParam("style name")] string name,
            [DekiScriptParam("style value; if missing no style is emitted (default: nil)", true)] string value
        ) {
            if(value == null) {
                return string.Empty;
            }
            return string.Format("{0}:{1};", name, value);
        }

        [DekiScriptFunction("web.uriencode", "Encode text as a URI component. (OBSOLETE: use 'uri.encode' instead)", IsIdempotent = true)]
        internal static string WebUriEncode(
            [DekiScriptParam("text to encode")] string text
        ) {
            return UriEncode(text);
        }

        [DekiScriptFunction("web.text", "Get a text value from a web-service.")]
        public static string WebText(
            [DekiScriptParam("source text or source uri (default: none)", true)] string source,
            [DekiScriptParam("xpath to value (default: none)", true)] string xpath,
            [DekiScriptParam("namespaces (default: none)", true)] Hashtable namespaces,
            [DekiScriptParam("capture enclosing XML element (default: false)", true)] bool? xml,
            [DekiScriptParam("caching duration in seconds (range: 60 - 86400; default: 300)", true)] double? ttl,
            [DekiScriptParam("return nil if source could not be loaded (default: text with error message)", true)] bool? nilIfMissing
        ) {
            XDoc doc;

            // NOTE (steveb): the following cases need to be covered
            // * source is a string and no xpath given -> return source as is
            // * source is a string and xpath given -> convert source to an XML document and apply xpath
            // * source is a uri pointing to text document and xpath given -> fetch source and convert to string (ignore xpath)
            // * source is a uri pointing to xml document and xpath given -> fetch source, convert to XML document, and apply xpath
            // * source is a uri pointing to text document and no xpath given -> fetch source and convert to string
            // * source is a uri pointing to xml document and no xpath given -> fetch source and convert to string
            source = source ?? string.Empty;
            XUri uri = XUri.TryParse(source);
            if(uri == null) {
                if(xpath == null) {

                    // source is a string and no xpath given -> return source as is
                    return source;
                }

                // source is a string and xpath given -> convert sourcwe to an XML document and apply xpath
                doc = XDocFactory.From(source, MimeType.XML);
                if((doc == null) || doc.IsEmpty) {
                    return nilIfMissing.GetValueOrDefault() ? null : "(source is not an xml document)";
                }
                return AtXPath(doc, xpath, namespaces, xml ?? false);
            }

            // we need to fetch an online document
            string response = CachedWebGet(uri, ttl, nilIfMissing);
            if((response == null) || (xpath == null)) {

                // source is a uri pointing to text document and no xpath given -> fetch source and convert to string
                // source is a uri pointing to xml document and no xpath given -> fetch source and convert to string
                return response;
            }
            doc = XDocFactory.From(response, MimeType.XML);
            if(doc.IsEmpty) {
                doc = XDocFactory.From(response, MimeType.HTML);
            }
            if(doc.IsEmpty) {

                // invalid document, return respons instead
                return nilIfMissing.GetValueOrDefault() ? null : response;
            }

            // * source is a uri pointing to xml document and xpath given -> fetch source, convert to XML document, and apply xpath
            return AtXPath(doc, xpath, namespaces, xml ?? false);
        }

        [DekiScriptFunction("web.format", "Convert text into html using a simple formatter")]
        public static XDoc WebFormat(
            [DekiScriptParam("HTML source text or source uri (default: none)", true)] string source,
            [DekiScriptParam("caching duration in seconds (range: 60 - 86400; default: 300)", true)] double? ttl,
            [DekiScriptParam("return nil if source could not be loaded (default: text with error message)", true)] bool? nilIfMissing
        ) {
            string text = WebText(source, null, null, true, ttl, nilIfMissing);
            if(text == null) {
                return null;
            }
            return SimpleHtmlFormatter.Format(text);
        }

        [DekiScriptFunction("web.html", "Convert text to HTML.  The text value can optionally be retrieved from a web-service.")]
        public static XDoc WebHtml(
            [DekiScriptParam("HTML source text or source uri (default: none)", true)] string source,
            [DekiScriptParam("xpath to value (default: none)", true)] string xpath,
            [DekiScriptParam("namespaces (default: none)", true)] Hashtable namespaces,
            [DekiScriptParam("caching duration in seconds (range: 60 - 86400; default: 300)", true)] double? ttl,
            [DekiScriptParam("return nil if source could not be loaded (default: text with error message)", true)] bool? nilIfMissing
        ) {
            string text = WebText(source, xpath, namespaces, true, ttl, nilIfMissing);
            if(text == null) {
                return null;
            }

            // convert text to html without a converter
            XDoc result = XDoc.Empty;
            using(TextReader reader = new StringReader("<html><body>" + text + "</body></html>")) {

                // NOTE (steveb): we create the sgml reader explicitly since we don't want a DTD to be associated with it; the DTD would force a potentially unwanted HTML structure

                // check if HTML entities DTD has already been loaded
                if(_htmlEntitiesDtd == null) {
                    using(StreamReader dtdReader = new StreamReader(Plug.New("resource://mindtouch.deki.script/MindTouch.Deki.Script.Resources.HtmlEntities.dtd").Get().AsStream())) {
                        _htmlEntitiesDtd = Sgml.SgmlDtd.Parse(null, "HTML", dtdReader, null, null, XDoc.XmlNameTable);
                    }
                }

                Sgml.SgmlReader sgmlReader = new Sgml.SgmlReader(XDoc.XmlNameTable);
                sgmlReader.Dtd = _htmlEntitiesDtd;
                sgmlReader.DocType = "HTML";
                sgmlReader.WhitespaceHandling = WhitespaceHandling.All;
                sgmlReader.CaseFolding = Sgml.CaseFolding.ToLower;
                sgmlReader.InputStream = reader;
                try {
                    XmlDocument doc = new XmlDocument(XDoc.XmlNameTable) {
                        PreserveWhitespace = true,
                        XmlResolver = null
                    };
                    doc.Load(sgmlReader);

                    // check if a valid document was created
                    if(doc.DocumentElement != null) {
                        result = new XDoc(doc);
                    }
                } catch {

                    // swallow parsing exceptions
                }
            }
            return CleanseHtmlDocument(result);
        }

        public static XDoc WebHtml(string source, string xpath, Hashtable namespaces, double? ttl) {
            return WebHtml(source, xpath, namespaces, ttl, null);
        }

        [DekiScriptFunction("web.list", "Get list of values from an XML document or web-service.")]
        public static ArrayList WebList(
            [DekiScriptParam("XML source text or source uri")] string source,
            [DekiScriptParam("xpath to list of values")] string xpath,
            [DekiScriptParam("namespaces (default: none)", true)] Hashtable namespaces,
            [DekiScriptParam("capture enclosing XML element (default: false)", true)] bool? xml,
            [DekiScriptParam("caching duration in seconds (range: 60 - 86400; default: 300)", true)] double? ttl,
            [DekiScriptParam("return nil if source could not be loaded (default: text with error message)", true)] bool? nilIfMissing
        ) {
            XUri uri = XUri.TryParse(source);
            ArrayList result;
            if(uri == null) {

                // source is a string -> convert sourcwe to an XML document and apply xpath
                XDoc doc = XDocFactory.From(source, MimeType.XML);
                if((doc == null) || doc.IsEmpty) {
                    result = nilIfMissing.GetValueOrDefault() ? null : new ArrayList();
                } else {
                    result = AtXPathList(doc, xpath, namespaces, xml ?? false);
                }
            } else {

                // we need to fetch an online document
                string response = CachedWebGet(uri, ttl, nilIfMissing);
                if(response == null) {
                    return null;
                }
                XDoc doc = XDocFactory.From(response, MimeType.XML);
                if(doc.IsEmpty) {
                    doc = XDocFactory.From(response, MimeType.HTML);
                }
                if(doc.IsEmpty) {

                    // * source is a uri pointing to text document -> fetch source and convert to string (ignore xpath)
                    result = nilIfMissing.GetValueOrDefault() ? null : new ArrayList { response };
                } else {

                    // * source is a uri pointing to xml document -> fetch source, convert to XML document, and apply xpath
                    result = AtXPathList(doc, xpath, namespaces, xml ?? false);
                }
            }
            return result;
        }

        [DekiScriptFunction("web.xml", "Get an XML document from a web-service.")]
        public static XDoc WebXml(
            [DekiScriptParam("XML source text or source uri")] string source,
            [DekiScriptParam("xpath to value (default: none)", true)] string xpath,
            [DekiScriptParam("namespaces (default: none)", true)] Hashtable namespaces,
            [DekiScriptParam("caching duration in seconds (range: 60 - 86400; default: 300)", true)] double? ttl,
            [DekiScriptParam("return nil if source could not be loaded (default: text with error message)", true)] bool? nilIfMissing
        ) {
            string text = WebText(source, xpath, namespaces, true, ttl, nilIfMissing);
            if(text == null) {
                return null;
            }
            XDoc result = XDocFactory.From(text, MimeType.XML);
            if(result.IsEmpty) {

                // try again assuming the input is HTML
                if(text.TrimStart().StartsWith("<")) {
                    result = XDocFactory.From(text, MimeType.HTML);
                } else {

                    // wrap response into a valid HTML document
                    result = new XDoc("html").Elem("body", text);
                }
            }
            if(result.HasName("html")) {
                result = CleanseHtmlDocument(result);
            }
            return result;
        }

        [DekiScriptFunction("web.json", "Get a JSON value from a web-service.")]
        public static object WebJson(
            [DekiScriptParam("source text or source uri (default: none)", true)] string source,
            [DekiScriptParam("caching duration in seconds (range: 60 - 86400; default: 300)", true)] double? ttl,
            [DekiScriptParam("return nil if source could not be loaded (default: text with error message)", true)] bool? nilIfMissing,
            DekiScriptRuntime runtime
        ) {
            source = source ?? string.Empty;
            XUri uri = XUri.TryParse(source);
            if(uri == null) {
                return JsonParse(source, runtime);
            }

            // we need to fetch an online document
            string response = CachedWebGet(uri, ttl, nilIfMissing);
            if(response == null) {
                return null;
            }
            return JsonParse(response, runtime);
        }


        public static object WebJson(
            [DekiScriptParam("source text or source uri (default: none)", true)] string source,
            [DekiScriptParam("caching duration in seconds (range: 60 - 86400; default: 300)", true)] double? ttl,
            DekiScriptRuntime runtime
        ) {
            return WebJson(source, ttl, null, runtime);
        }


        [DekiScriptFunction("web.checkuri", "Check URI for possible JavaScript code.", IsIdempotent = true)]
        public static string WebCheckUri(
            [DekiScriptParam("uri to check")] string uri
        ) {
            uri = uri.Trim();
            if(uri.StartsWithInvariantIgnoreCase("javascript:")) {
                throw new ArgumentException("javascript is illegal inside the uri", "uri");
            }
            return uri;
        }

        [DekiScriptFunction("web.checkstyle", "Check contents of style attribute for possible XSS vulnerabilities.", IsIdempotent = true)]
        public static string WebCheckStyle(
            [DekiScriptParam("style contents to check")] string style
        ) {
            style = style.Trim().ReplaceWithinDelimiters("/*", "*/", string.Empty, StringComparison.OrdinalIgnoreCase);
            if(STYLE_XSS_CHECK.IsMatch(style)) {
                throw new ArgumentException("style contains potential XSS vulnerability", "style");
            }
            return style;
        }

        [DekiScriptFunction("web.toggle", "Embed a clickable title that toggles the provided document content.", IsIdempotent = true)]
        public static XDoc WebToggle(
            [DekiScriptParam("content to toggle")] XDoc content,
            [DekiScriptParam("title to display for toggle (default: \"Show\")", true)] string title,
            [DekiScriptParam("heading level for title (default: 3)", true)] int? heading,
            [DekiScriptParam("content toggle speed (one of \"slow\", \"normal\", \"fast\" or milliseconds number; default: instantaneous)", true)] string speed,
            [DekiScriptParam("hide content initially (default: true)", true)] bool? hidden
        ) {
            if(!content["body[not(@target)]"].IsEmpty) {
                string id = StringUtil.CreateAlphaNumericKey(8);

                // clone content so we don't modify the original
                content = content.Clone();
                XDoc body = content["body[not(@target)]"];

                // add <style> element
                XDoc head = content["head"];
                if(head.IsEmpty) {
                    content.Elem("head");
                    head = content["head"];
                }
                head.Elem("style",
@"h1.web-expand,
h2.web-expand,
h3.web-expand,
h4.web-expand,
h5.web-expand,
h6.web-expand {
	cursor: pointer;
}
.web-expand span.web-expander { 
    padding-right: 20px;
    background: transparent url('/skins/common/images/nav-parent-open.gif') no-repeat center right;
} 
.web-expanded span.web-expander { 
    background: transparent url('/skins/common/images/nav-parent-docked.gif') no-repeat center right; 
}");

                // set speed
                if(string.IsNullOrEmpty(speed)) {
                    speed = string.Empty;
                } else {
                    int millisec;
                    if(int.TryParse(speed, out millisec)) {
                        speed = millisec.ToString();
                    } else {
                        speed = "'" + speed + "'";
                    }
                }

                // create toggelable content
                bool hide = hidden ?? true;
                content
                    .Start("body")
                        .Start("h" + Math.Max(1, Math.Min(heading ?? 3, 6)))
                            .Attr("class", "web-expand" + (hide ? string.Empty : " web-expanded"))
                            .Attr("onclick", "$(this).toggleClass('web-expanded').next('#" + id + "').toggle(" + speed + ")")
                            .Start("span")
                                .Attr("class", "web-expander")
                                .Value(title ?? "Show")
                            .End()
                        .End()
                        .Start("div")
                            .Attr("id", id)
                            .Attr("style", hide ? "display: none;" : string.Empty)
                            .AddNodes(body)
                        .End()
                    .End();
                body.Remove();
            }
            return content;
        }

        [DekiScriptFunction("web.showerror", "Show error information with debug information.", IsIdempotent = true)]
        public static XDoc WebShowError(
            [DekiScriptParam("error information")] Hashtable error
        ) {
            string description = (string)error["message"];
            string body = (string)error["stacktrace"];

            // check if the script callstack is defined
            ArrayList callstack = (ArrayList)error["callstack"];
            if(callstack != null) {
                StringBuilder stacktrace = new StringBuilder();
                foreach(var entry in callstack) {
                    stacktrace.AppendFormat("    at {0}\n", entry);
                }
                if(stacktrace.Length > 0) {
                    if(body == null) {
                        body = "Callstack:\n" + stacktrace;
                    } else {
                        body = "Callstack:\n" + stacktrace + "\n" + body;
                    }
                }
            }

            // TODO: localize
            const string actiontext = "(click for details)";

            // create embeddable xml
            XDoc result = new XDoc("div");
            result.Start("span").Attr("class", "warning").Value(description).End();
            if(body != null) {
                string id = StringUtil.CreateAlphaNumericKey(8);
                result.Value(" ");
                result.Start("span").Attr("style", "cursor: pointer;").Attr("onclick", string.Format("$('#{0}').toggle()", id)).Value(actiontext).End();
                result.Start("pre").Attr("id", id).Attr("style", "display: none;").Value(body).End();
            }
            return result;
        }

        private static string CachedWebGet(XUri uri, double? ttl, bool? nilIfMissing) {

            // fetch message from cache or from the web
            string result;
            lock(_webTextCache) {
                if(_webTextCache.TryGetValue(uri, out result)) {
                    return result;
                }
            }

            // do the web request
            Result<DreamMessage> response = new Result<DreamMessage>();
            Plug.New(uri).WithTimeout(DEFAULT_WEB_TIMEOUT).InvokeEx("GET", DreamMessage.Ok(), response);
            DreamMessage message = response.Wait();
            try {

                // check message status
                if(!message.IsSuccessful) {
                    if(nilIfMissing.GetValueOrDefault()) {
                        return null;
                    }
                    return message.Status == DreamStatus.UnableToConnect
                        ? string.Format("(unable to fetch text document from uri [status: {0} ({1}), message: \"{2}\"])", (int)message.Status, message.Status, message.ToDocument()["message"].AsText)
                        : string.Format("(unable to fetch text document from uri [status: {0} ({1})])", (int)message.Status, message.Status);
                }

                // check message size
                Result resMemorize = message.Memorize(InsertTextLimit, new Result()).Block();
                if(resMemorize.HasException) {
                    return nilIfMissing.GetValueOrDefault() ? null : "(text document is too large)";
                }

                // detect encoding and decode response
                var stream = message.AsStream();
                var encoding = stream.DetectEncoding() ?? message.ContentType.CharSet;
                result = encoding.GetString(stream.ReadBytes(-1));
            } finally {
                message.Close();
            }

            // start timer to clean-up cached result
            lock(_webTextCache) {
                _webTextCache[uri] = result;
            }
            double timeout = Math.Min(60 * 60 * 24, Math.Max(ttl ?? MinCacheTtl, 60));
            TaskEnv.ExecuteNew(() => TaskTimer.New(TimeSpan.FromSeconds(timeout), timer => {
                lock(_webTextCache) {
                    _webTextCache.Remove((XUri)timer.State);
                }
            }, uri, TaskEnv.None));
            return result;
        }
    }
}

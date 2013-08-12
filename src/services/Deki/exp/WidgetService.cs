/*
 * MindTouch DekiWiki - a commercial grade open source wiki
 * Copyright (C) 2006 MindTouch, Inc.
 * www.mindtouch.com  oss@mindtouch.com
 *
 * For community documentation and downloads visit www.opengarden.org;
 *  please review the licensing section.
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
using System.IO;
using System.Net;
using System.Text;
using System.Xml.Serialization;

using MindTouch.Dream;

namespace MindTouch.Deki {
    [DreamService("MindTouch Dream Widget", "Copyright (c) 2006 MindTouch, Inc.", "https://tech.mindtouch.com/Product/Dream/Service_Widget")]
    public class WidgetService : DreamService {

        //--- Class Fields ---
        private static log4net.ILog _log = LogUtils.CreateLog<WidgetService>();

        //--- Fields ---
        private XUri _wikiRootUri;
        private Plug _widgetstorage;

        //--- Methods ---
        public override void Start(XDoc config) {
            base.Start(config);
            _wikiRootUri = config["deki-root-uri"].AsUri;
            if(_wikiRootUri == null) {
                throw new ArgumentNullException("deki-root-uri is missing or invalid");
            }
            _widgetstorage = Plug.New(Env.RootUri).At("mount", "deki-widgets");
        }

        [DreamFeature("load", "/*", "POST", "Render Widget", "https://tech.mindtouch.com/Product/Dream/Service_Widget")]
        [DreamFeatureParam("mode", "string", "requests widget to be in view or edit mode")]
        [DreamFeatureParam("id", "int", "widget id")]
        public DreamMessage PostLoadHandler(DreamContext context, DreamMessage message) {
            string widget = context.GetSuffix(0, UriPathFormat.Normalized);
            XDoc data = message.Document;
            string mode = context.Uri.GetParam("mode", "view");
            string id = context.Uri.GetParam("id", "-1");
            LogUtils.LogTrace(_log, "POST load", widget, mode, data);
            return DreamMessage.Ok(MimeType.HTML, GetWidgetContent(context, widget, id, mode, data));
        }

        [DreamFeature("create", "/*", "POST", "Create Widget", "https://tech.mindtouch.com/Product/Dream/Service_Widget")]
        [DreamFeatureParam("id", "int", "widget id")]
        public DreamMessage GetCreateHandler(DreamContext context, DreamMessage message) {
            string widget = context.GetSuffix(0, UriPathFormat.Normalized);
            string id = context.Uri.GetParam("id", "-1");
            LogUtils.LogTrace(_log, "GET create", widget, id);
            XDoc data = XDoc.FromXml(GetStorageContent(string.Format("{0}-data.xml", widget)));
            return DreamMessage.Ok(MimeType.HTML, GetWidgetContent(context, widget, id, "edit", data));
        }

        /*
        [DreamFeature("iframe", "/*", "GET", "Prepare Widget", "https://tech.mindtouch.com/Product/Dream/Service_Widget")]
        [DreamFeatureParam("id", "int", "widget id")]
        public void GetLoadHandler(DreamContext context) {
            string widget = context.GetSuffix(0, UriPathFormat.Normalized);
            int id = context.Uri.GetParamAsInt("id", -1);
            LogUtils.LogTrace(_log, "GET iframe", widget, id);
            string template = GetStorageContent("widget-iframe.html");
            string path = "/";
            if (context.Request.UriReferrer == null || context.Request.UriReferrer.Port != this.Env.RootUri.Port)
                path = "/dream" + path;
            XUri uri = context.Request.UriReferrer == null ? _wikiRootUri : new XUri(context.Request.UriReferrer);
            uri = uri.AppendPath(path);
            context.Response.SendHtml(ReplaceVariables(template, new MyDictionary(
                "%%BASEURI%%", uri.ToString(),
                "%%ID%%", id.ToString(), 
                "%%TYPE%%", widget)));
        }
        */

        [DreamFeature("all", "/", "GET", "List all widgets", "https://tech.mindtouch.com/Product/Dream/Service_Widget")]
        public DreamMessage GetAllHandler(DreamContext context, DreamMessage message) {
            return DreamMessage.Ok(GetWidgets());
        }

        /*
        [DreamFeature("tohtml", "/*", "POST", "Convert to HTML", "https://tech.mindtouch.com/Product/Dream/Service_Widget")]
        public void PostToHtmlHandler(DreamContext context) {
            string widget = context.GetSuffix(0, UriPathFormat.Normalized);
            string mode = context.Uri.GetParam("mode", "widget");
            XDoc data = message.Document;
            string xspanData = data.ToXSpan();
            if (mode == "data") {
                context.Response.SendHtml(xspanData);
                return;
            }
            string id = context.Uri.GetParam("id", "");
            context.Response.SendHtml(CreateWidget(widget, GetWidgetContent(context, widget, "", "view", data), id, data, xspanData));
        }
        */

        [DreamFeature("select", "/", "GET", "Editor Select Widget", "https://tech.mindtouch.com/Product/Dream/Service_Widget")]
        public DreamMessage GetEditorSelect(DreamContext context, DreamMessage message) {
            XDoc widgets = GetWidgets();
            string template = GetStorageContent("widget-editor-select.html");
            string widgetHtml = "";
            foreach (XDoc widget in widgets["widget"]) {
                string name = widget.Contents;
                if (name == "dekibizcard")
                    name = "BizCard";
                widgetHtml += "<option value='" + widget.Contents + "'>" + name + "</option>";
            }

            return DreamMessage.Ok(MimeType.HTML, ReplaceVariables(template, new MyDictionary("%%WIDGETS%%", widgetHtml)));
        }

        #region -- Implementation --

        private string GetStorageContent(string file) {

            // TODO: add caching to just download content if not expired
            return _widgetstorage.At(file).Get().Text;
        }

        string GetWidgetContent(DreamContext context, string widgetType, string id, string mode, XDoc data) {
            if (mode.ToLowerInvariant() != "edit") {
                Plug plug = Plug.New(Env.RootUri.At("wiki-data", widgetType, "render"));
                XDoc widgetXhtml = plug.Post(data).Document;
                if (widgetXhtml == null || widgetXhtml.IsEmpty) {
                    LogUtils.LogWarning(_log, string.Format("GetWidgetContent: null/empty document for /wiki-data/{0}/render/ (data)", widgetType), data);
                    throw new DreamAbortException(DreamMessage.BadRequest("no widget data"));
                }
                return widgetXhtml.ToString();
            } else {
                Plug normalizePlug = Plug.New(Env.RootUri.At("wiki-data", widgetType, "normalize"));
                data = normalizePlug.Post(data).Document;

                Plug renderPlug = Plug.New(Env.RootUri.At("wiki-data", widgetType, "render"));
                XDoc renderHtml = renderPlug.Post(data).Document;
                string jsonData = data.ToJson();
                return new XDoc("div")
                    .Attr("class", "widget").Attr("widgetid", id).Attr("widgettype", widgetType).Attr("style", "display:none")
                        .Start("div")
                            .Attr("class", "data")
                            .Value(string.Format("Widget.registerWidget({0},{1})", id, jsonData))
                        .End()
                        .Start("div")
                            .Attr("class", "view")
                            .Add(renderHtml)
                        .End()
                    .ToString();
            }
        }

        public static string ReplaceVariables(string value, IDictionary<string, string> vars) {
            foreach (KeyValuePair<string, string> keyValue in vars)
                value = value.Replace(keyValue.Key, keyValue.Value);
            return value;
        }

        private string CreateWidget(string widget, string widgetContent, string id, XDoc data, string htmlData) {
            string template = GetStorageContent("widget-tohtml.html");
            return ReplaceVariables(template, new MyDictionary(
                "%%WIDGET%%", widgetContent,
                "%%ID%%", id,
                "%%TYPE%%", widget,
                "%%DATA%%", htmlData));
        }

        private XDoc GetWidgets() {
            XDoc files = _widgetstorage.With("pattern", "*-data.xml").Get().Document;
            XDoc ret = new XDoc("widgets");
            foreach(XDoc file in files["file/name"])
                ret.Start("widget").Value(file.Contents.Substring(0, file.Contents.Length - "-data.xml".Length)).End();
            return ret;
        }
        #endregion
    }

    internal class MyDictionary : Dictionary<string, string> {
        internal MyDictionary(params string[] keyValues) {
            for (int i = 0; i < keyValues.Length; i += 2)
                this[keyValues[i]] = keyValues[i + 1];
        }
    }
}

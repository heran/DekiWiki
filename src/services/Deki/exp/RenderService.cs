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
using System.Text;
using System.Xml;

using MindTouch.Dream;

namespace MindTouch.Deki {
    [DreamService("MindTouch DekiWiki - Render Service", "Copyright (c) 2006 MindTouch, Inc.", "http://doc.opengarden.org/Deki_API/Reference/DekiWiki_Dream_API#Render_Service")]
    public class RenderService : DreamService {
        //--- Class Fields ---
        private static log4net.ILog _log = LogUtils.CreateLog<RenderService>();

        #region -- Handlers

        [DreamFeature("tidyXHtml", "/", "POST", "", "http://doc.opengarden.org/Deki_API/Reference/DekiWiki_Dream_API#Render_Service")]
        [DreamFeatureParam("base", "uri", "")]
        [DreamFeatureParam("context", "int", "")]
        [DreamFeatureParam("toEdit", "bool", "")]
        public DreamMessage PostTidyXHmlt(DreamContext context, DreamMessage message) {
            string baseHref = context.Uri.GetParam("base", "");
            string pageID = context.Uri.GetParam("context", "");
            bool toEdit = context.Uri.GetParam<bool>("toEdit", false);
            XDoc doc = XDoc.FromHtml(new StreamReader(message.Stream, message.ContentEncoding));
            XHTMLConverter.Convert(doc, baseHref, pageID, toEdit);
            return DreamMessage.Ok(MimeType.XHTML, doc.Root.ToXHtml());
        }

        /// <summary>
        /// privilege: read
        /// in: page + content
        /// out: content
        /// </summary>
        /// <param name="context"></param>
        [DreamFeature("view", "/", "POST", "returns the converted content including section edit buttons, and resolved server tags", "http://doc.opengarden.org/Deki_API/Reference/DekiWiki_Dream_API#Render_Service")]
        public DreamMessage PostViewHandler(DreamContext context, DreamMessage message) {
            XDoc xhtml = message.ContentType.Contains("/html") 
                ? XDoc.FromHtml(new StreamReader(message.Stream, message.ContentEncoding))
                : message.Document;
            if (xhtml == null || xhtml.IsEmpty) {
                LogUtils.LogWarning(_log, "PostViewHandler: null/empty input document");
                throw new DreamAbortException(DreamMessage.BadRequest("null/empty input document"));
            }
            string pageID = context.Uri.GetParam("context", "");
            /*
            string baseHref = context.Uri.GetParam("baseHref", 0, "http://mos/");
            XHTMLConverter.Convert(xhtml, baseHref, pageID, false);
            */
            ConvertAllWidgets("PostViewHandler", xhtml, "render", pageID);
            return DreamMessage.Ok(MimeType.XHTML, xhtml.ToXHtml());
        }

        private void ConvertAllWidgets(string method, XDoc xhtml, string action, string pageID) {
            foreach (XDoc widget in new List<XDoc>(xhtml["//default:span[@class='widget']"])) {
                string widgetType = widget["@widgettype"].Contents;
                if (widgetType == string.Empty) {
                    LogUtils.LogWarning(_log, method + ": no widgettype attribute", widget);
                    continue;
                }
                XDoc htmlData = widget[string.Format("default:span[@class='{0}']", widgetType)];
                if (htmlData.IsEmpty) {
                    LogUtils.LogWarning(_log, method + ": no widget data with class", widgetType, widget);
                    continue;
                }
                XDoc data = XDoc.FromXSpan(htmlData);
                if (data.IsEmpty) {
                    LogUtils.LogWarning(_log, method + ": no xspan data", htmlData);
                    continue;
                }
                string widgetID = widget["@widgetid"].Contents;
                XUri uri = Env.RootUri.At("wiki-data", widgetType, action).With("id", widgetID);
                if (pageID != "")
                    uri = uri.With("pageid", pageID);
                Plug plug = Plug.New(uri);
                XDoc widgetXhtml = plug.Post(data).Document;
                if (widgetXhtml == null || widgetXhtml.IsEmpty) {
                    LogUtils.LogWarning(_log, method + string.Format(": null/empty document for /wiki-data/{0}/{1}/ (data)", widgetType, action), data);
                    continue;
                }
                LogUtils.LogTrace(_log, method, widgetType, data, widgetXhtml);
                widget.Replace(widgetXhtml);
            }
        }

        /// <summary>
        /// privilege: read
        /// in: page + content
        /// out: content
        /// </summary>
        /// <param name="context"></param>
        [DreamFeature("edit", "/", "POST", "convert into edit mode", "http://doc.opengarden.org/Deki_API/Reference/DekiWiki_Dream_API#Render_Service")]
        public DreamMessage PostEditHandler(DreamContext context, DreamMessage message) {
            XDoc xhtml = message.ContentType.Contains("/html")
                ? XDoc.FromHtml(new StreamReader(message.Stream, message.ContentEncoding))
                : message.Document;
            if (xhtml == null || xhtml.IsEmpty) {
                LogUtils.LogWarning(_log, "PostEditHandler: null/empty input document");
                throw new DreamAbortException(DreamMessage.BadRequest("null/empty input document"));
            }
            string baseHref = context.Uri.GetParam("baseHref", 0, "http://mos/");
            string pageID = context.Uri.GetParam("context", "");
            XHTMLConverter.Convert(xhtml, baseHref, pageID, true);
            ConvertAllWidgets("PostEditHandler", xhtml, "edit", pageID);
            return DreamMessage.Ok(MimeType.XHTML, xhtml.ToXHtml());
        }

        [DreamFeature("paste", "/", "POST", "cleanup paste, and detect microformats", "http://doc.opengarden.org/Deki_API/Reference/DekiWiki_Dream_API#Render_Service")]
        public DreamMessage PostPasteHandler(DreamContext context, DreamMessage message) {
            XDoc xhtml = message.ContentType.Contains("/html")
                ? XDoc.FromHtml(new StreamReader(message.Stream, message.ContentEncoding))
                : message.Document;
            if (xhtml == null || xhtml.IsEmpty) {
                LogUtils.LogWarning(_log, "PostEditHandler: null/empty input document");
                throw new DreamAbortException(DreamMessage.BadRequest("null/empty input document"));
            }
            string baseHref = context.Uri.GetParam("baseHref", 0, "http://mos/");
            string pageID = context.Uri.GetParam("context", "");
            XHTMLConverter.Convert(xhtml, baseHref, pageID, true);

            MindTouch.Dream.Plug plug = MindTouch.Dream.Plug.New(Env.RootUri);
            foreach (XDoc nodeWithClass in xhtml["//*[@class='vcard']"]) {
                XDoc replacement = plug.At("wiki-data", "dekibizcard", "hcardtoedit").Post(nodeWithClass).Document;
                if (replacement != null && !replacement.IsEmpty)
                    nodeWithClass.Replace(replacement);
            }

            bool insertMagic = context.Uri.GetParam<bool>("insertMagic", false);
            if (insertMagic) {
                Plug widgetStorage = Plug.New(Env.RootUri).At("mount", "deki-widgets");
                Plug widgetToEdit = Plug.New(Env.RootUri.At("wiki-data", "dekibizcard", "edit"));
                XDoc files = widgetStorage.With("pattern", "*.vcf").Get().Document;
                foreach (XDoc fileName in files["file/name"]) {
                    XDoc vcard = XDoc.FromVersit(widgetStorage.At(fileName.Contents).Get().Text, "dekibizcard");
                    XDoc widgetXhtml = widgetToEdit.Post(vcard).Document;
                    xhtml["//body"].Add(widgetXhtml);
                }
            }
            return DreamMessage.Ok(MimeType.HTML, xhtml.ToString());
        }

        /// <summary>
        /// privilege: read
        /// in: page + content
        /// out: content
        /// </summary>
        /// <param name="context"></param>
        [DreamFeature("save", "/", "POST", "convert from edit mode into save mode", "http://doc.opengarden.org/Deki_API/Reference/DekiWiki_Dream_API#Render_Service")]
        public DreamMessage PostSaveHandler(DreamContext context, DreamMessage message) {
            return DreamMessage.NotImplemented("missing");
        }

        /// <summary>
        /// privilege: read
        /// in: page + content + mode={html, doc, pdf, sub-pages, static-html-tar} + [scope={page,tree}]
        /// out: content
        /// </summary>
        /// <param name="context"></param>
        [DreamFeature("export", "/", "POST", "returns the converted content as requested", "http://doc.opengarden.org/Deki_API/Reference/DekiWiki_Dream_API#Render_Service")]
        public DreamMessage PostExportHandler(DreamContext context, DreamMessage message) {
            return DreamMessage.NotImplemented("missing");
        }

        /// <summary>
        /// privilege: read
        /// in: page + content
        /// out: content
        /// </summary>
        /// <param name="context"></param>
        [DreamFeature("print", "/", "POST", "clean viewable XHTML (server tags are resolved)", "http://doc.opengarden.org/Deki_API/Reference/DekiWiki_Dream_API#Render_Service")]
        public DreamMessage PostPrintHandler(DreamContext context, DreamMessage message) {
            return DreamMessage.NotImplemented("missing");
        }

        #endregion
    }
}

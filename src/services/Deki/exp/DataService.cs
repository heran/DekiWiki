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
using System.Text;

using MindTouch.Dream;

namespace MindTouch.Deki {
    [DreamService("MindTouch DekiWiki - Data Service", "Copyright (c) 2006 MindTouch, Inc.", Info = "http://doc.opengarden.org/Deki_API/Reference/DekiWiki_Dream_API#Data_Service")]
    public class DataService : DekiWikiServiceBase {
        private static log4net.ILog _log = LogUtils.CreateLog<DataService>();

        #region -- Handlers

        /// <summary>
        /// privilege: read
        /// in: data
        /// out: data-info
        /// </summary>
        /// <param name="context"></param>
        [DreamFeature("info", "/", "GET", "returns the data info for the given data block", "http://doc.opengarden.org/Deki_API/Reference/DekiWiki_Dream_API#Data_Service")]
        public DreamMessage GetInfoHandler(DreamContext context, DreamMessage message) {
            return DreamMessage.NotImplemented("missing");
        }

        /// <summary>
        /// privilege: read
        /// in: data
        /// out: content
        /// </summary>
        /// <param name="context"></param>
        [DreamFeature("content", "/", "GET", "returns a structured component with the given data-id from the given page in the given format", "http://doc.opengarden.org/Deki_API/Reference/DekiWiki_Dream_API#Data_Service")]
        public DreamMessage GetContentHandler(DreamContext context, DreamMessage message) {
            user user = Authenticate(context, message, DekiUserLevel.User);
            page page = Authorize(context, user, DekiAccessLevel.Read, "pageid");
            DekiContext deki = new DekiContext(message, this.DekiConfig);
            bool nofollow = (context.Uri.GetParam("nofollow", 0, null) != null);
            string contents = page.getContent(nofollow);
            string xml = string.Format(DekiWikiService.XHTML_LOOSE, contents);
            XDoc doc = XDoc.FromXml(xml);
            if (doc == null) {
                LogUtils.LogWarning(_log, "GetContentHandler: null document page content", page.PrefixedName, contents);
                throw new DreamAbortException(DreamMessage.BadRequest("null document"));
            }
            XDoc result = new XDoc("list");
            string type = context.Uri.GetParam("type", 0, null);
            string id = context.Uri.GetParam("id", 0, null);
            if (id != null) {
                XDoc widget = doc[string.Format("//default:span[@widgetid={0}]", id)];
                if (widget.IsEmpty) {
                    LogUtils.LogWarning(_log, "GetContentHandler: widget not found for ID", id);
                    return DreamMessage.NotFound("");
                }
                LogUtils.LogTrace(_log, "GetContentHandler: widget by id (id, xspan)", id, widget);
                result.Add(ConvertFromXSpan(widget));
            } else if (type != null) {
                foreach (XDoc widget in doc[string.Format("//default:span[@widgettype='{0}']", type)])
                    result.Add(ConvertFromXSpan(widget));
                LogUtils.LogTrace(_log, "GetContentHandler: widget by type (type, #)", type, result.Count);
            } else {
                foreach (XDoc widget in doc["//default:span[@class='widget']"])
                    result.Add(ConvertFromXSpan(widget));
                LogUtils.LogTrace(_log, "GetContentHandler: all widgets (#)", type, result.Count);
            }
            return DreamMessage.Ok(result);
        }

        public static XDoc ConvertFromXSpan(XDoc xspanWidget) {
            XDoc data = XDoc.FromXSpan(xspanWidget);
            data.Attr("id", xspanWidget["@widgetid"].Contents);
            data.Attr("type", xspanWidget["@widgettype"].Contents);
            return data;
        }

        /// <summary>
        /// privilege: write
        /// in: page + data-xml
        /// out: 
        /// </summary>
        /// <param name="context"></param>
        [DreamFeature("content", "/", "POST", "updates the structured component with the given data-xml within the given page", "http://doc.opengarden.org/Deki_API/Reference/DekiWiki_Dream_API#Data_Service")]
        public DreamMessage PostContentHandler(DreamContext context, DreamMessage message) {
            return DreamMessage.NotImplemented("missing");
        }

        [DreamFeature("convert/xhtml", "/", "POST", "convert xml data into valid XHTML spans", "http://doc.opengarden.org/Deki_API/Reference/DekiWiki_Dream_API#Data_Service")]
        public DreamMessage PostConvertXml2Xhtml(DreamContext context, DreamMessage message) {
            XDoc data = message.Document;
            string xspanData = data.ToXSpan();
            return DreamMessage.Ok(MimeType.HTML, xspanData);
        }

        [DreamFeature("convert/xml", "/", "POST", "convert xml data into valid XHTML spans", "http://doc.opengarden.org/Deki_API/Reference/DekiWiki_Dream_API#Data_Service")]
        public DreamMessage PostConvertXhtml2Xml(DreamContext context, DreamMessage message) {
            XDoc data = message.Document;
            return DreamMessage.Ok(data);
        }

        #endregion
    }
}

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
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Net;
using System.Text;
using System.Xml.Serialization;
using System.Web;

using MindTouch.Dream;

namespace MindTouch.Deki {
    using HttpPostedFile = MindTouch.Dream.Http.HttpPostedFile;

    [DreamService("MindTouch Dream Wiki API", "Copyright (c) 2006 MindTouch, Inc.", "https://tech.mindtouch.com/Product/Dream/Service_WikiAPI")]
    public class WikiApiService : DekiWikiServiceBase {

        //--- Class Fields ---
        private static log4net.ILog _log = LogUtils.CreateLog<WikiApiService>();

        //--- Methods ---
        /// <summary>
        /// functional
        /// </summary>
        /// <param name="context"></param>
        [DreamFeature("users", "/", "GET", "List of users", "https://tech.mindtouch.com/Product/Dream/Service_Wiki-API")]
        public DreamMessage GetUsersHandler(DreamContext context, DreamMessage message) {
            //DekiContext deki = new DekiContext(message, this.DekiConfig);
            //if (!deki.Authenticate()) { //Max: Commented out to allow compilation
            //    return DreamMessage.AccessDenied("DekiWiki", "Authorization Required");
            //}
            XDoc ret = new XDoc("users");
            foreach (user cur in user.GetUsers())
                AddUser(cur, ret);
            return DreamMessage.Ok(ret);
        }

        /// <summary>
        /// mode=raw functional
        /// </summary>
        /// <param name="context"></param>
        [DreamFeature("page", "//*", "GET",
            "Input: " +
            "suffixes == page name, " +
            "optional query/mode [raw|view|print|edit|export|meta] (default: raw), " +
            "optional query/output [json|xml|html] (default: xml), " +
            "optional query/history, " +
            "HTTP basic authentication.  " +
            "Output: page content.  " +
            "Comments: Retrieve the page content of the given page and version in the given format", "https://tech.mindtouch.com/Product/Dream/Service_Wiki-API")]
        public DreamMessage GetPageHandler(DreamContext context, DreamMessage message) {
            page cur = null;//deki.GetCur(true); Max: commented out to allow compilation
            if (cur == null)
                return DreamMessage.BadRequest("can't load page");
            string mode = context.Uri.GetParam("mode", "raw").ToLowerInvariant();
            switch (mode) {
            case "raw":
                return DreamMessage.Ok(MimeType.HTML, cur.Text);
            case "xml":
                string xml = string.Format(DekiWikiService.XHTML_LOOSE, cur.Text);
                XDoc result = XDoc.FromXml(xml);
                return DreamMessage.Ok(MimeType.XHTML, result.ToXHtml());
            case "export":
            case "edit":
            case "print":
            case "view":
                return DreamMessage.Ok(MimeType.HTML, /*deki.Render(cur.Text, mode) Max:Removed to allow compilation*/ "" );
            }
            return DreamMessage.NotImplemented(string.Format("'mode={0}' is not supported"));
        }

        /// <summary>
        /// functional
        /// </summary>
        /// <summary>
        /// usage: 
        ///     http://localhost:8081/wiki-api/nav/Home?max-level=100
        ///     http://localhost:8081/wiki-api/nav/Project_A?max-level=4&column=id&column=children&column=name
        /// </summary>
        /// <param name="context"></param>
        [DreamFeature("nav", "//*", "GET", "Input: suffixes == page name, " +
            "optional query/max-level (default: 1), " +
            "optional query/column[] (default: [name, id, TIP, modified, user]), " +
            "HTTP basic authentication.  " +
            "Output: info doc.  " +
            "Comment: get navigation tree given page context and columns", "https://tech.mindtouch.com/Product/Dream/Service_Wiki-API")]
        public DreamMessage GetNavHandler(DreamContext context, DreamMessage message) {
            DekiContext deki = null; // new DekiContext(message, this.DekiConfig);
            page cur = null; // deki.GetCur(true); Max: Commented out to allow compilation
            if (cur == null)
                return DreamMessage.BadRequest("can't load page");
            int maxLevel = context.Uri.GetParam<int>("max-level", 1);
            bool filterRedirects = context.Uri.GetParam<bool>("redirects", true);
            ICollection<string> columns = context.Uri.GetParams("column");
            string filter = filterRedirects ? " AND page_is_redirect=0" : "";

            XDoc ret = new XDoc("nav");
            AddCur(deki, cur, ret, columns, filter, maxLevel);
            return DreamMessage.Ok(ret);
        }

        /// <summary>
        /// functional
        /// </summary>
        /// <param name="context"></param>
        [DreamFeature("list", "//*", "GET", "Input: suffixes == page name[/filename], query/column[] [files|images|children|siblings|history], optional query/column[] (default: [name, id, TIP, modified, user]), HTTP basic authentication.  Output: info doc", "https://tech.mindtouch.com/Product/Dream/Service_Wiki-API")]
        public DreamMessage GetListHandler(DreamContext context, DreamMessage message) {
            /* Max: Commented out to allow compilation
            DekiContext deki = new DekiContext(context, message, this.DekiConfig);
            cur cur = deki.GetCur(false);
            if (cur == null)
                return DreamMessage.BadRequest("can't load page");
            if(cur.Namespace == NS.ATTACHMENT) {
                attachments attachment = deki.GetAttachment();
                if (attachment == null)
                    return DreamMessage.BadRequest("can't load attachment");
                XDoc file = new XDoc("file");
                AddFile(attachment, file, false);
                return DreamMessage.Ok(file);
            }

            bool filterRedirects = context.Uri.GetParamAsBool("redirects", true);
            ICollection<string> columns = context.Uri.GetParams("column");
            string filter = filterRedirects ? " AND page_is_redirect=0" : "";
            XDoc page = new XDoc("page");
            AddCur(deki, cur, page, columns, filter, 1, true, false);
            return DreamMessage.Ok(page);
             */
            throw new NotImplementedException();
        }

        #region -- Implementation --
        static void AddUser(user user, XDoc doc) {
            doc.Start("user")
                .Attr("id", user.ID.ToString())
                .Start("name").Value(user.Name).End()
                .Start("real-name").Value(user.RealName).End()
                .Start("email").Value(user.Email).End()
                .Start("touched").Value(user.Touched).End()
            .End();
        }

        void AddCur(DekiContext deki, page cur, XDoc doc, ICollection<string> columns, string filter, int level) {
            AddCur(deki, cur, doc, columns, filter, level, false, true);
        }
        void AddCur(DekiContext deki, page cur, XDoc doc, ICollection<string> columns, string filter, int level, bool flat, bool addWrap) {
            if (level < 0)
                return;
            if (addWrap)
                doc.Start("page");
            doc.Attr("cur-id", cur.ID.ToString());
            if (columns.Count == 0 || columns.Contains("parent"))
                doc.Start("parent").Value(cur.ParentID).End();
            if (columns.Count == 0 || columns.Contains("name"))
                doc.Start("name").Value(cur.PrefixedName).End();
            if (columns.Count == 0 || columns.Contains("modified"))
                doc.Start("modified").Value(cur.TimeStamp).End();
            if (columns.Count == 0 || columns.Contains("comment"))
                doc.Start("comment").Value(cur.Comment).End();
            if (columns.Count == 0 || columns.Contains("preview"))
                doc.Start("preview").Value(cur.TIP).End();
            if (columns.Count == 0 || columns.Contains("table-of-contents"))
                doc.Start("table-of-contents").Value(cur.TOC).End();
            if (columns.Count == 0 || columns.Contains("is-redirect"))
                doc.Start("is-redirect").Value(cur.IsRedirect.ToString()).End();
            if (columns.Count == 0 || columns.Contains("from-links"))
                AddCurIDList(cur.GetLinkIDsFrom(), "from-links", doc);
            if (columns.Count == 0 || columns.Contains("to-links"))
                AddCurIDList(cur.GetLinkIDsTo(), "to-links", doc);
            if (columns.Count == 0 || columns.Contains("files")) {
                doc.Start("files");
                foreach (attachments attachment in cur.GetAttachments())
                    AddFile(attachment, doc);
                doc.End();
            }
            if (level < 1) {
                if (addWrap)
                    doc.End();
                return;
            }
            if (columns.Count == 0 || columns.Contains("children")) {
                if (flat)
                    AddCurIDList(cur.GetChildIDs(filter), "children", doc);
                else {
                    doc.Start("children");
                    foreach (page child in cur.LoadChildren(filter))
                        AddCur(deki, child, doc, columns, filter, level - 1);
                    doc.End();
                }
            }
            if (addWrap)
                doc.End();
        }

        static void AddCurIDList(ICollection<ulong> list, string tag, XDoc doc) {
            doc.Start(tag);
            foreach (ulong ID in list)
                doc.Start("id").Value(ID).End();
            doc.End();
        }
        static void AddCurIDList(ICollection<page> list, string tag, XDoc doc) {
            doc.Start(tag);
            foreach (page link in list)
                doc.Start("id").Value(link.ID).End();
            doc.End();
        }

        static void AddFile(attachments attachment, XDoc doc) {
            AddFile(attachment, doc, true);
        }
        static void AddFile(attachments attachment, XDoc doc, bool wrap) {
            if (wrap)
                doc.Start("file");
            doc
                .Attr("id", attachment.ID.ToString())
                .Attr("page", attachment.From.ToString())
                .Start("full-name").Value(attachment.GetFullName()).End()
                .Start("name").Value(attachment.Name).End()
                .Start("extension").Value(attachment.Extension).End()
                .Start("filename").Value(attachment.FileName).End()
                .Start("description").Value(attachment.Description).End()
                .Start("size").Value(attachment.FileSize.ToString()).End()
                .Start("type").Value(attachment.FileType).End()
                .Start("timestamp").Value(attachment.TimeStamp).End()
                .Start("user").Value(attachment.UserText).End();
            if (attachment.Removed != DateTime.MinValue) {
                doc.Start("removed").Value(attachment.Removed).End()
                .Start("removed-by").Value(attachment.RemovedByText).End();
            }
            if (wrap)
                doc.End();
        }

        #endregion
    }

    public enum MKS_STAT {
        EDIT_FULL = 0,
        EDIT_SECTION = 1,
        EDIT_CANCEL = 2,
        EDIT_QUICK_SAVE = 3,
        EDIT_SAVE = 4,
        BREADCRUMB = 5,
        NAV_PARENT = 6,
        NAV_SIBLING = 7,
        NAV_CHILD = 8,
        NAV_NEXT = 9,
        NAV_PREVIOUS = 10,
        CONTENT = 11,
        BACKLINK = 12,
        PRINT = 13,
        SAVE_HTML = 14,
        SAVE_PDF = 15,
        PRINT_CANCEL = 16,
        UPLOAD_FILE = 17,
        UPLOAD_SIZE = 18,
        REVISION = 19,
        MENU_WHATS_NEW = 20,
        MENU_CP = 21,
        CP_ADD_USER = 22,
        CP_DEACTIVATE_USER = 23,
        CP_REINSTALL = 24,
        CP_RESTART = 25,
        CP_SHUTDOWN = 26,
        CP_SUPPORT_ADDED = 27,
        CP_BACKUP_SETTINGS = 28,
        CP_CREATE_BACKUP = 29,
        CP_RESTORE_BACKUP = 30,
        CP_CRLNK_ADDED = 31,
        CP_CRLNK_CHANGED = 32,
        CP_CRLNK_DELETED = 33,
        MENU_SHOW_USERS = 34,
        MENU_ALL_PAGES = 35,
        MENU_POPULAR_PAGES = 36,
        MENU_WANTED_PAGES = 37,
        MENU_ORPHANED_PAGES = 38,
        MENU_DOUBLE_REDIRECTS = 39,
        MY_PREFERENCES = 40,
        MY_CONTRIBUTIONS = 41,
        MY_WATCHLIST = 42,
        CREATE_SUB_PAGE = 43,
        PAGE_RENAMED = 44,
        MENU_WATCH_PAGE = 45,
        MENU_UNWATCH_PAGE = 46,
        VIEWS = 47,
        EDIT_CONFLICT = 48,
        DELETE_PAGE = 49,
        PASSWORD_CHANGED = 50,
        MENU_LOGIN = 51,
        MENU_LOGOUT = 52,
        TOP_MY_PAGE = 53,
        TOP_HOME = 54,
        TOP_COMMUNITY = 55,
        TOP_EVENTS = 56,
        TOP_HELP = 57,
        EDIT_PREVIEW = 58,
        PREVIEW_SHOW_TOC = 59,
        PREVIEW_SHOW_FOOTER = 60,
        PREVIEW_SHOW_LINK_ENDNOTES = 61,
        EDITOR_ADVANCED = 62,
        RESPONSE_COUNT = 63,
        RESPONSE_TIME_MIN = 64,
        RESPONSE_TIME_MAX = 65,
        RESPONSE_TIME_SUM = 66,
        RESPONSE_TIME_STD_DEV = 67,
        RESPONSE_SIZE_MIN = 68,
        RESPONSE_SIZE_MAX = 69,
        MENU_DELETE = 70,
        RESPONSE_SIZE_SUM = 71,
        RESPONSE_SIZE_STD_DEV = 72,
        NAV_CURRENT = 73,
        TOP_CUSTOM = 74,
        TOC = 75,
        UPLOAD_FAILED = 76,
        NEW_PAGE = 77,
        COMPARE_REVISION = 78,
        VIEW_PAGE_AT_GIVEN_REVISION = 79,
        PREVIEW_HIDE_TOC = 80,
        PREVIEW_HIDE_FOOTER = 81,
        PREVIEW_HIDE_LINK_ENDNOTES = 82,
        EDITOR_SIMPLE = 83,
        CP_AUTO_LOGIN_ON = 84,
        CP_AUTO_LOGIN_OFF = 85,
        CP_ANONYMOUS_VIEWING_ON = 86,
        CP_ANONYMOUS_VIEWING_OFF = 87,
        CP_HTTP_AUTHENTICATION_ON = 88,
        CP_HTTP_AUTHENTICATION_OFF = 89,
        MENU_PRINT = 90,
        NET_COLLISIONS = 91,
        NET_PACKETS_SENT = 92,
        NET_PACKETS_RECEIVED = 93,
        NET_BYTES_SENT = 94,
        NET_BYTES_RECEIVED = 95,
    }
}

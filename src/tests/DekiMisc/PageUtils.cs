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
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using MindTouch.Dream;
using MindTouch.Dream.Test;
using MindTouch.Xml;
using MindTouch.Tasking;
using MindTouch.Deki.Logic;

using NUnit.Framework;

namespace MindTouch.Deki.Tests
{
    public static class PageUtils
    {
        public static string GenerateUniquePageName()
        {
            string prefix = "tests/";
            prefix += DateTime.Now.ToString("yyyy-MM-dd/HH-mm", CultureInfo.InvariantCulture);
            prefix += "/p";

            return Utils.GenerateUniqueName(prefix);
        }

        public static DreamMessage CreateRandomPage(Plug p)
        {
            return CreateRandomPage(p, GenerateUniquePageName());
        }

        public static DreamMessage CreateRandomPage(Plug p, string pageTitle)
        {
            return PageUtils.SavePage(p, pageTitle, Utils.GetSmallRandomText());
        }

        public static DreamMessage CreateRandomPage(Plug p, out string id)
        {
            string path = null;
            string title = GenerateUniquePageName();

            return PageUtils.SavePage(p, string.Empty, title, Utils.GetSmallRandomText(), out id, out path);
        }

        public static DreamMessage CreateRandomPage(Plug p, string pageTitle, out string id, out string path)
        {
            return PageUtils.SavePage(p, string.Empty, pageTitle, Utils.GetSmallRandomText(), out id, out path);
        }

        public static DreamMessage CreateRandomPage(Plug p, out string id, out string path)
        {
            string pageTitle = GenerateUniquePageName();
            return PageUtils.SavePage(p, string.Empty, pageTitle, Utils.GetSmallRandomText(), Utils.DateToString(DateTime.MinValue), out id, out path);
        }

        public static DreamMessage SavePage(Plug p, string pageTitle, string content)
        {
            string id = null;
            string path = null;
            return PageUtils.SavePage(p, string.Empty, pageTitle, content, out id, out path);
        }

        public static DreamMessage SavePage(
            Plug p, 
            string parentPath, 
            string pageTitle, 
            string content,
            out string id,
            out string path
        ) {
            string sEditTime = null;
            DreamMessage msg = PageUtils.GetPage(p, parentPath + pageTitle);
            if (msg.Status == DreamStatus.Ok)
            {
                DateTime edittime = msg.ToDocument()["/page/date.edited"].AsDate ?? DateTime.MinValue;
                sEditTime = Utils.DateToString(edittime);
            }

            return SavePage(p, parentPath, pageTitle, content, sEditTime, out id, out path);
        }

        public static DreamMessage SavePage(Plug p, 
            string parentPath, 
            string pageTitle, 
            string content, 
            string edittime, 
            out string id,
            out string path)
        {
            string title = pageTitle;
            if (parentPath != string.Empty)
                title = parentPath + pageTitle;

            title = "=" + XUri.DoubleEncode(title);

            p = p.At("pages", title, "contents");
            if (!string.IsNullOrEmpty(edittime))
                p = p.With("edittime", edittime);

            DreamMessage msg = p.PostAsync(DreamMessage.Ok(MimeType.TEXT_UTF8, content)).Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Page creation failed!");
            id = msg.ToDocument()["page/@id"].AsText;
            path = msg.ToDocument()["page/path"].AsText;
            Assert.IsTrue(!string.IsNullOrEmpty(path), "Page path is null!");
            Assert.IsTrue(!string.IsNullOrEmpty(id), "Page ID is null!");

            return msg;
        }

        public static DreamMessage DeletePageByID(Plug p, string id, bool recurse)
        {
            var msg = p.At("pages", id).WithQuery("recursive=" + recurse.ToString()).DeleteAsync().Wait();
            Assert.IsTrue(msg.IsSuccessful, "Page delete by ID failed!");
            return msg;
        }

        public static DreamMessage DeletePageByName(Plug p, string path, bool recurse)
        {
            path = "=" + XUri.DoubleEncode(path);
            var msg = p.At("pages", path).WithQuery("recursive=" + recurse.ToString()).DeleteAsync().Wait();
            Assert.IsTrue(msg.IsSuccessful, "Page delete by name failed!");
            return msg;
        }

        public static string BuildPageTree(Plug p)
        {
           // A
           // A/B
           // A/B/C
           // A/B/D
           // A/E

            string treeRoot = null;
            string id = null;
            CreateRandomPage(p, out id, out treeRoot);

            string pathToA = treeRoot + "/A";
            PageUtils.SavePage(p, pathToA, string.Empty);
            string pathToB = pathToA + "/B";
            PageUtils.SavePage(p, pathToB, string.Empty);
            string pathToC = pathToB + "/C";
            PageUtils.SavePage(p, pathToC, string.Empty);
            string pathToD = pathToB + "/D";
            PageUtils.SavePage(p, pathToD, string.Empty);
            string pathToE = pathToA + "/E";
            PageUtils.SavePage(p, pathToE, string.Empty);

            return treeRoot;
        }

        public static DreamMessage MovePage(Plug p, string path, string targetPath)
        {
            path = "=" + XUri.DoubleEncode(path);
            DreamMessage msg = p.At("pages", path, "move").With("to", targetPath).Post();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Page move failed!");

            return msg;
        }

        public static DreamMessage GetPage(Plug p, string path) {
            return GetPage(p, path, new PageContentFilterSettings());
        }

        public static DreamMessage GetPage(Plug p, string path, PageContentFilterSettings pageContentFilter) {
            path = "=" + XUri.DoubleEncode(path);
            DreamMessage msg = null;
            var contentFilter = new List<string>();
            if (pageContentFilter.ExcludeOutboundLinks) {
                contentFilter.Add("outbound");
            }
            if (pageContentFilter.ExcludeInboundLinks) {
                contentFilter.Add("inbound");
            }
            var pagesPlug = p.At("pages", path);
            if (contentFilter.Count > 0) {
                pagesPlug = pagesPlug.With("exclude", String.Join(",", contentFilter.ToArray()));
            }
            msg = pagesPlug.GetAsync().Wait();
            Assert.IsTrue(msg.Status == DreamStatus.Ok || msg.Status == DreamStatus.NotFound,
                string.Format("Unexpected status: {0}", msg.Status));
            return msg;
        }

        public static DreamMessage RestrictPage(Plug p, string path, string cascade, string restriction)
        {
            path = "=" + XUri.DoubleEncode(path);

            XDoc securityDoc = new XDoc("security");
            if (!string.IsNullOrEmpty(restriction))
                securityDoc.Start("permissions.page").Start("restriction").Value(restriction).End().End();

            //if (userIdGrantsHash != null && userIdGrantsHash.Keys.Count > 0)
            //{
            //    x.Start("grants");
            //    foreach (KeyValuePair<int, List<string>> grantsForUser in userIdGrantsHash)
            //    {
            //        foreach (string grant in grantsForUser.Value)
            //        {
            //            x.Start("grant").Start("permissions").Start("role").Value(grant).End().End().Start("user").Attr("id", grantsForUser.Key).End().End();
            //        }
            //    }
            //    x.End();
            //}
            DreamMessage msg = p.At("pages", path, "security").WithQuery("cascade=" + cascade).Put(securityDoc);
            Assert.AreEqual(DreamStatus.Ok, msg.Status);

            return msg;
        }

        public static string GetPageName(Plug p, string pageid) // name == final path segment
        {
            DreamMessage msg = p.At("pages", pageid).Get(new Result<DreamMessage>()).Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Page retrieval failed");
            string uri = msg.ToDocument()["uri.ui"].AsText ?? String.Empty;
            int lastSlashIndex = uri.LastIndexOf("/");
            if (lastSlashIndex == -1) {
                return String.Empty;
            }

            string name = XUri.DoubleDecode(uri.Substring(lastSlashIndex + 1));
            return name;
        }

        public static DreamMessage CreateRandomUnlinkedPage(Plug p, out string title, out string id, out string path)
        {
            string pageid = GenerateUniquePageName();
            string content = Utils.GetSmallRandomText();
            title = Utils.GenerateUniqueName();
            DreamMessage msg = p.At("pages", "=" + XUri.DoubleEncode(pageid), "contents")
                .With("title", title).Post(DreamMessage.Ok(MimeType.TEXT_UTF8, content), new Result<DreamMessage>()).Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Unlinked page create failed!");

            id = msg.ToDocument()["page/@id"].AsText ?? String.Empty;
            path = msg.ToDocument()["page/path"].AsText ?? String.Empty;
            Assert.IsTrue(!string.IsNullOrEmpty(path), "Page path is null!");
            Assert.IsTrue(!string.IsNullOrEmpty(id), "Page ID is null!");
            Assert.AreEqual(msg.ToDocument()["page/path/@type"].AsText, "custom", "Path type is not custom!");

            return msg;
        }

        public static DreamMessage MovePage(Plug p) {
            DreamMessage msg = p.Post(new Result<DreamMessage>()).Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Page move failed");
            return msg;
        }

        public static DreamMessage MovePageAndWait(Plug p, string oldpath) {
            DreamMessage msg = MovePage(p);
            Assert.IsTrue(Wait.For(() =>
            {
                msg = PageUtils.GetPage(Utils.BuildPlugForAdmin(), oldpath);
                return (!msg.ToDocument()["path"].IsEmpty && (msg.ToDocument()["path"].AsText != oldpath));
            },
              TimeSpan.FromSeconds(10)),
              "unable to find redirect");
            return msg;
        }

        public static bool IsLinked(DreamMessage pageMsg) {
            if (pageMsg.ToDocument()["path/@type"].IsEmpty) {
                return true;
            }
            return false;
        }

        public static bool IsUnlinked(DreamMessage pageMsg) {
            if ((pageMsg.ToDocument()["path/@type"].AsText ?? String.Empty) == "custom") {
                return true;
            }
            return false;
        }

        public static bool IsFixed(DreamMessage pageMsg) {
            if ((pageMsg.ToDocument()["path/@type"].AsText ?? String.Empty) == "fixed") {
                return true;
            }
            return false;
        }

        public static DreamMessage CreateTalkPage(Plug p, string pagepath, out string talkid, out string talkpath) {
            talkpath = "Talk:" + pagepath;
            DreamMessage msg = p.At("pages", "=" + XUri.DoubleEncode(talkpath), "contents").
                Post(DreamMessage.Ok(MimeType.TEXT_UTF8, "test talk page"), new Result<DreamMessage>()).Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Talk page creation failed");
            talkid = msg.ToDocument()["page/@id"].AsText ?? String.Empty;
            talkpath = msg.ToDocument()["page/path"].AsText ?? String.Empty;
            Assert.IsTrue(!string.IsNullOrEmpty(talkpath), "Page path is null!");
            Assert.IsTrue(!string.IsNullOrEmpty(talkid), "Page ID is null!");
            return msg;
        }

        public static DreamMessage CreatePageWithNamespace(Plug p, string name_space, out string id, out string path) {
            path = name_space + ":" + PageUtils.GenerateUniquePageName();
            DreamMessage msg = p.At("pages", "=" + XUri.DoubleEncode(path), "contents").
                Post(DreamMessage.Ok(MimeType.TEXT_UTF8, "test talk page"), new Result<DreamMessage>()).Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, name_space + " page creation failed");
            id = msg.ToDocument()["page/@id"].AsText ?? String.Empty;
            path = msg.ToDocument()["page/path"].AsText ?? String.Empty;
            Assert.IsTrue(!string.IsNullOrEmpty(path), "Page path is null!");
            Assert.IsTrue(!string.IsNullOrEmpty(id), "Page ID is null!");
            return msg;
        }

        public static DreamMessage CreatePageAndTalkPage(Plug p, out string pageid, out string pagepath, out string talkid, out string talkpath) {
            CreateRandomPage(p, out pageid, out pagepath);
            return CreateTalkPage(p, pagepath, out talkid, out talkpath);
        }

        public static DreamMessage TagPage(Plug p, string pageId, string[] tags) {
            var doc = new XDoc("tags");
            doc = tags.Aggregate(doc, (current, tag) => current.Start("tag").Attr("value", tag).End());
            return p.At("pages", pageId, "tags").Put(doc);
        }
    }
}

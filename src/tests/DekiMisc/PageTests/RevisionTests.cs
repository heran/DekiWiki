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

using NUnit.Framework;

using MindTouch.Dream;
using MindTouch.Xml;
using System;

namespace MindTouch.Deki.Tests.PageTests
{
    [TestFixture]
    public class RevisionTests
    {
        /// <summary>
        ///     Generate a diff of tag-less page content revisions
        /// </summary>        
        /// <feature>
        /// <name>GET:pages/{pageid}/diff</name>
        /// <uri>http://developer.mindtouch.com/Deki/API_Reference/GET%3apages%2f%2f%7bpageid%7d%2f%2fdiff</uri>
        /// </feature>
        /// <expected>Output correct diff</expected>

        [Test]
        public void GetPageDiffWithoutTags()
        {
            // 1. Create a page with some content
            // 2. Retrieve revisions list
            // (3) Assert revisions list is populated
            // 4. Save page with some new content
            // 5. Retrieve revisions list
            // (6) Assret revisions list is populated
            // 7. Perform a diff
            // (8) Assert diff result matches expected diff
            // 9. Delete the page

            // GET:pages/{pageid}/revisions 
            // http://developer.mindtouch.com/Deki/API_Reference/GET%3apages%2f%2f%7bpageid%7d%2f%2frevisions

            Plug p = Utils.BuildPlugForAdmin();

            string content = "This is test content";

            string id = null;
            string path = null;
            DreamMessage msg = PageUtils.SavePage(p, string.Empty,
                PageUtils.GenerateUniquePageName(), content, out id, out path);

            msg = p.At("pages", "=" + XUri.DoubleEncode(path), "revisions").Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Page revisions retrieval failed (1)");
            Assert.IsFalse(msg.ToDocument()["page"].IsEmpty, "Page revisions list is empty?! (1)");

            PageUtils.SavePage(p, path, "New content");

            msg = p.At("pages", id, "revisions").Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Page revisions retrieval failed (2)");
            Assert.IsFalse(msg.ToDocument()["page"].IsEmpty, "Page revisions list is empty?! (2)");

            // GET:pages/{pageid}/diff
            // http://developer.mindtouch.com/Deki/API_Reference/GET%3apages%2f%2f%7bpageid%7d%2f%2fdiff

            msg = p.At("pages", id, "diff").With("revision", "head").With("previous", -1).Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "diff generation failed");
            string diff = "<ins>New</ins><del>This is test</del> content";
            Assert.AreEqual(diff, msg.ToDocument().Contents, "diff result does not match expected result");

            PageUtils.DeletePageByID(p, id, true);
        }

        /// <summary>
        ///     Generate a diff of page content revisions with tags
        /// </summary>        
        /// <feature>
        /// <name>GET:pages/{pageid}/diff</name>
        /// <uri>http://developer.mindtouch.com/Deki/API_Reference/GET%3apages%2f%2f%7bpageid%7d%2f%2fdiff</uri>
        /// </feature>
        /// <expected>Output correct diff</expected>

        [Test]
        public void GetPageDiffWithTags()
        {
            // 1. Create a page with some content with tags
            // 2. Retrieve revisions list
            // (3) Assert revisions list is populated
            // 4. Save page with some new content with tags
            // 5. Retrieve revisions list
            // (6) Assret revisions list is populated
            // 7. Perform a diff
            // (8) Assert diff result matches expected diff
            // 9. Delete the page

            Plug p = Utils.BuildPlugForAdmin();

            string content = "<p>This is test</p><p>content</p>";

            string id = null;
            string path = null;
            DreamMessage msg = PageUtils.SavePage(p, string.Empty,
                PageUtils.GenerateUniquePageName(), content, out id, out path);

            msg = p.At("pages", "=" + XUri.DoubleEncode(path), "revisions").Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Page revisions retrieval failed (1)");
            Assert.IsFalse(msg.ToDocument()["page"].IsEmpty, "Page revisions list is empty?! (1)");

            PageUtils.SavePage(p, path, "<p>New</p><p>content</p>");

            msg = p.At("pages", id, "revisions").Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Page revisions retrieval failed (2)");
            Assert.IsFalse(msg.ToDocument()["page"].IsEmpty, "Page revisions list is empty?! 2)");

            msg = p.At("pages", id, "diff").With("revision", "head").With("previous", -1).Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "diff generation failed");
            string diff = "<p><ins>New</ins><del>This is test</del></p><p>...</p>";
            Assert.AreEqual(diff, msg.ToDocument().Contents, "diff result does not match expected result");

            PageUtils.DeletePageByID(p, id, true);
        }

        /// <summary>
        ///     Performs several 
        /// </summary>        
        /// <feature>
        /// <name>POST:pages/{pageid}/revisions</name>
        /// <uri>http://developer.mindtouch.com/en/ref/MindTouch_API/POST%3apages%2f%2f%7Bpageid%7D%2f%2frevisions</uri>
        /// </feature>
        /// <expected>Output correct diff</expected>

        [Ignore] // Bug 8097
        [Test]
        public void RevisionHideAndUnhide() {

            // 1. Create a page with 3 revisions
            // 2. Create a user with Contributor role
            // (3) Assert unhidden contents can be viewed by user
            // 4. Hide second revision
            // (5) Assert first revision remains unhidden
            // (6) Assert second revision is hidden
            // (7) Assert third revision is unhidden
            // (8) Assert admin can view hidden revision contents
            // (9) Assert Unauthorized HTTP response returned to user attempting to view hidden revision contents
            // (10) Assert Forbidden HTTP response returned to user when attempting to unhide a hidden revision
            // (11) Assert user is allowed to hide a revision
            // (12) Assert Conflict HTTP response returned to user when attempting to revert to hidden revision
            // 13. Delete the page
            // 14. Restore page as admin
            // (15) Assert restored page persists hidden state

            Plug p = Utils.BuildPlugForAdmin();

            string id = null;
            string path = null;
            DreamMessage msg = PageUtils.SavePage(p, string.Empty, PageUtils.GenerateUniquePageName(), "Rev1", out id, out path);
            PageUtils.SavePage(p, path, "Rev2");
            PageUtils.SavePage(p, path, "Rev3");

            string userid;
            string username;
            UserUtils.CreateRandomUser(p, "Contributor", out userid, out username);

            //Check that anon can see contents before hiding revs
            msg = Utils.BuildPlugForUser(username).At("pages", id, "contents").With("revision", 2).GetAsync().Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "reg user can't see contents even before hiding!");

            //Reinit plug to admin
            Utils.BuildPlugForAdmin();

            string comment = "just cuz..";
            XDoc hideRequestXml = new XDoc("revisions").Start("page").Attr("id", id).Attr("hidden", true).Attr("revision", 2).End();
            msg = p.At("pages", id, "revisions").With("comment", comment).PostAsync(hideRequestXml).Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Non 200 status hiding revisions");

            //Ensure correct revisions coming back is visible + hidden
            msg = p.At("pages", id, "revisions").With("revision", 1).GetAsync().Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "pages/{id}/revisions?revision=x returned non 200 status");
            Assert.IsFalse(msg.ToDocument()["/page[@revision = \"1\"]/@hidden"].AsBool ?? false, "Rev 1 is hidden!");

            //validate hidden rev
            msg = p.At("pages", id, "revisions").With("revision", 2).GetAsync().Wait();
            Assert.IsTrue(msg.ToDocument()["/page[@revision = \"2\"]/@hidden"].AsBool ?? false, "Rev 2 is not hidden!");
            Assert.AreEqual(comment, msg.ToDocument()["/page[@revision = \"2\"]/description.hidden"].AsText, "hide comment missing or invalid");
            Assert.IsTrue(!string.IsNullOrEmpty(msg.ToDocument()["/page[@revision = \"2\"]/date.hidden"].AsText), "date.hidden missing");
            Assert.IsNotNull(msg.ToDocument()["/page[@revision = \"2\"]/user.hiddenby/@id"].AsUInt, "user.hiddenby id missing");

            msg = p.At("pages", id, "revisions").With("revision", 3).GetAsync().Wait();
            Assert.IsFalse(msg.ToDocument()["/page[@revision = \"3\"]/@hidden"].AsBool ?? false, "Rev 3 is hidden!");

            //Ensure admin still has rights to see hidden page contents
            msg = p.At("pages", id, "contents").With("revision", 2).GetAsync().Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "admin can't see hidden contents!");

            //Ensure non-admin cannot see hidden page contents
            msg = Utils.BuildPlugForUser(username).At("pages", id, "contents").With("revision", 2).GetAsync().Wait();
            Assert.IsTrue(msg.Status == DreamStatus.Unauthorized || msg.Status == DreamStatus.Forbidden, "reg user can still see contents!");

            //Attempt to unhide a rev by non admin
            hideRequestXml = new XDoc("revisions").Start("page").Attr("id", id).Attr("hidden", false).Attr("revision", 2).End();
            msg = p.At("pages", id, "revisions").With("comment", comment).PostAsync(hideRequestXml).Wait();
            Assert.AreEqual(DreamStatus.Forbidden, msg.Status, "non admin able to unhide rev");

            //Attempt to hide a rev by non admin
            hideRequestXml = new XDoc("revisions").Start("page").Attr("id", id).Attr("hidden", true).Attr("revision", 1).End();
            msg = p.At("pages", id, "revisions").With("comment", comment).PostAsync(hideRequestXml).Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "DELETE holder unable to hide rev");

            //revert hidden rev            
            msg = p.At("pages", id, "revert").With("fromrevision", 2).PostAsync().Wait();
            Assert.AreEqual(DreamStatus.Conflict, msg.Status, "able to revert a hidden rev!");

            //delete page
            msg = p.At("pages", id).DeleteAsync().Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "unable to delete page!");

            //Reinit plug to admin
            Utils.BuildPlugForAdmin();

            //undelete page
            msg = p.At("archive", "pages", id, "restore").PostAsync().Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "unable to restore page!");
                        
            //Ensure correct revisions coming back is visible + hidden
            msg = p.At("pages", id, "revisions").With("revision", 2).GetAsync().Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "pages/{id}/revisions?revision=x returned non 200 status");
            Assert.IsTrue(msg.ToDocument()["/page[@revision = \"2\"]/@hidden"].AsBool ?? false, "Rev 2 is no longer hidden after delete/restore!");
        }

        [Test]
        public void GetPageCreatorInfo() {

            Plug p = Utils.BuildPlugForAdmin();

            string content = "This is test content";

            string id = null;
            string path = null;
            DreamMessage msg = PageUtils.SavePage(p, string.Empty,
                PageUtils.GenerateUniquePageName(), content, out id, out path);

            msg = p.At("users", "current").Get();
            var pageCreatorId = msg.AsDocument()["@id"].AsText;
            PageUtils.SavePage(p, path, "New content");
            var pageDoc = p.At("pages", id).Get().ToDocument();
            Assert.AreEqual(pageCreatorId, pageDoc["user.createdby/@id"].AsText);
            Assert.IsTrue((pageDoc["date.created"].AsDate ?? DateTime.MinValue) > DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(5)), "create date missing.");
            PageUtils.DeletePageByID(p, id, true);

        }
    }
}

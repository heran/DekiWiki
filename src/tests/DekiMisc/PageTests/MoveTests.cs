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
using MindTouch.Dream.Test;

using NUnit.Framework;
using MindTouch.Dream;
using MindTouch.Tasking;

namespace MindTouch.Deki.Tests.PageTests
{
    [TestFixture]
    public class MoveTests
    {

        /// <summary>
        ///     Move a page by ID
        /// </summary>        
        /// <feature>
        /// <name>POST:pages/{pageid}/move</name>
        /// <uri>http://developer.mindtouch.com/Deki/API_Reference/POST%3apages%2f%2f%7bpageid%7d%2f%2fmove</uri>
        /// </feature>
        /// <expected>200 OK HTTP Response</expected>

        [Test]
        public void MovePageByID()
        {
            // 1. Create a parent page1
            // 2. Create page2
            // (3) Assert page2 created successfully
            // 4. Move page2 by ID to subpage of page1
            // (5) Assert parent page1 has a subpage
            // 6. Delete page1 recursively

            Plug p = Utils.BuildPlugForAdmin();

            string parentid = null;
            string parentpath = null;
            DreamMessage msg = PageUtils.CreateRandomPage(p, out parentid, out parentpath);

            string id = null;
            string path = null;
            msg = PageUtils.CreateRandomPage(p, out id, out path);

            msg = p.At("pages", id).Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Page retrieval failed");
            string pageTitle = msg.ToDocument()["title"].AsText;

            msg = p.At("pages", id, "move").With("to", parentpath + "/" + pageTitle).Post();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Page move failed");

            msg = p.At("pages", parentid, "subpages").Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Subpage retrieval failed");

            Assert.IsFalse(msg.ToDocument()[string.Format("page.subpage[@id=\"{0}\"]", id)].IsEmpty, "Parent page has no subpages!");

            PageUtils.DeletePageByID(p, parentid, true);
        }

        /// <summary>
        ///     Move a page by name
        /// </summary>        
        /// <feature>
        /// <name>POST:pages/{pageid}/move</name>
        /// <uri>http://developer.mindtouch.com/Deki/API_Reference/POST%3apages%2f%2f%7bpageid%7d%2f%2fmove</uri>
        /// </feature>
        /// <expected>200 OK HTTP Response</expected>

        public void MovePageByName()
        {
            // 1. Create page1
            // (2) Assert page1 created successfully
            // 3. Create a parent page2
            // 4. Move page1 by name to subpage of page2
            // (5) Assert parent page2 has a subpage
            // 6. Delete page2 recursively

            Plug p = Utils.BuildPlugForAdmin();

            string id = null;
            string path = null;
            DreamMessage msg = PageUtils.CreateRandomPage(p, out id, out path);

            msg = p.At("pages", id).Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Page retrieval failed");
            string pageTitle = msg.ToDocument()["title"].AsText;

            string parentid = null;
            string parentpath = null;
            msg = PageUtils.CreateRandomPage(p, out parentid, out parentpath);
            msg = p.At("pages", "=" + XUri.DoubleEncode(path), "move").With("to", parentpath + "/" + pageTitle).Post();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Page move failed");

            msg = p.At("pages", parentid, "subpages").Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Subpage retrieval failed");

            Assert.IsFalse(msg.ToDocument()[string.Format("page.subpage[@id=\"{0}\"]", id)].IsEmpty, "Parent page has no subpages!");

            PageUtils.DeletePageByID(p, parentid, true);
        }

        /// <summary>
        ///     Move a page to itself
        /// </summary>        
        /// <feature>
        /// <name>POST:pages/{pageid}/move</name>
        /// <uri>http://developer.mindtouch.com/Deki/API_Reference/POST%3apages%2f%2f%7bpageid%7d%2f%2fmove</uri>
        /// </feature>
        /// <expected>200 Ok HTTP Response</expected>

        [Test]
        public void MovePageToItself() {

            // 1. Create page
            // 2. Move it to itself
            // (3) Assert Conflict HTTP response

            Plug p = Utils.BuildPlugForAdmin();

            string id;
            string path;
            DreamMessage msg = PageUtils.CreateRandomPage(p, out id, out path);

            msg = p.At("pages", id, "move").With("to", path).Post();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "failed to move page to itself");
        }

        /// <summary>
        ///     Move a subpage to its parent page
        /// </summary>        
        /// <feature>
        /// <name>POST:pages/{pageid}/move</name>
        /// <uri>http://developer.mindtouch.com/Deki/API_Reference/POST%3apages%2f%2f%7bpageid%7d%2f%2fmove</uri>
        /// </feature>
        /// <expected>200 Ok HTTP Response</expected>

        [Test]
        public void MovePageToSameParent()
        {
            // 1. Create page1
            // 2. Create page2 as child page1
            // 3. Move page2 to page1
            // (4) Assert Conflict HTTP response

            Plug p = Utils.BuildPlugForAdmin();

            string parentid;
            string parentpath;
            DreamMessage msg = PageUtils.CreateRandomPage(p, out parentid, out parentpath);

            string id;
            string path;
            msg = PageUtils.CreateRandomPage(p, parentpath + "/" + Utils.GenerateUniqueName(), out id, out path);

            msg = p.At("pages", id, "move").With("parentid", parentid).Post();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "failed to move page to its parent");
        }

        /// <summary>
        ///     Move a parent page to its subpage
        /// </summary>        
        /// <feature>
        /// <name>POST:pages/{pageid}/move</name>
        /// <uri>http://developer.mindtouch.com/Deki/API_Reference/POST%3apages%2f%2f%7bpageid%7d%2f%2fmove</uri>
        /// </feature>
        /// <expected>409 Conflict HTTP Response</expected>

        [Test]
        public void MovePageToChild()
        {
            // 1. Create page1
            // 2. Create page2 as child page1
            // 3. Move page1 to page2

            Plug p = Utils.BuildPlugForAdmin();

            string parentid = null;
            string parentpath = null;
            DreamMessage msg = PageUtils.CreateRandomPage(p, out parentid, out parentpath);

            string id = null;
            string path = null;
            msg = PageUtils.CreateRandomPage(p, parentpath + "/" + Utils.GenerateUniqueName(), out id, out path);

            try
            {
                msg = p.At("pages", parentid, "move").With("to", path + "/" + parentpath).Post();
                Assert.IsTrue(false, "Move of parent page to child page succeeded?!");
            }
            catch (DreamResponseException ex)
            {
                Assert.IsTrue(ex.Response.Status == DreamStatus.Conflict, "HTTP response other than \"Conflict\" returned");
            }
        }

        /// <summary>
        ///     Move a page to a path where page of the same name already exists
        /// </summary>        
        /// <feature>
        /// <name>POST:pages/{pageid}/move</name>
        /// <uri>http://developer.mindtouch.com/Deki/API_Reference/POST%3apages%2f%2f%7bpageid%7d%2f%2fmove</uri>
        /// </feature>
        /// <expected>409 Conflict HTTP Response</expected>

        [Test]
        public void MovePageWithSameName()
        {
            // 1. Create page1
            // 2. Create parent page2
            // 3. Move page1 to subpage of page2
            // 4. Create page3 with name same as page1
            // 5. Move page3 to subpage of page2
            // (6) Assert Conflict HTTP response

            Plug p = Utils.BuildPlugForAdmin();

            string id1 = null;
            string path1 = null;
            DreamMessage msg = PageUtils.CreateRandomPage(p, out id1, out path1);

            string parentid = null;
            string parentpath = null;
            msg = PageUtils.CreateRandomPage(p, out parentid, out parentpath);

            msg = p.At("pages", id1, "move").With("to", parentpath + "/" + path1).Post();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Page move failed");

            Assert.IsTrue(Wait.For(() =>
            {
                msg = p.At("pages", "=" + XUri.DoubleEncode(path1)).Get(new Result<DreamMessage>()).Wait();
                return (!msg.ToDocument()["path"].IsEmpty && (msg.ToDocument()["path"].AsText != path1));
            },
               TimeSpan.FromSeconds(10)),
               "unable to find redirect");
            
            string id2 = null;
            string path2 = null;
            msg = PageUtils.CreateRandomPage(p, out id2, out path2);

            try
            {
                msg = p.At("pages", id2, "move").With("to", parentpath + "/" + path1).Post();
                Assert.IsTrue(false, "Move to page with same name succeeded?!");
            }
            catch (DreamResponseException ex)
            {
                Assert.IsTrue(ex.Response.Status == DreamStatus.Conflict, "HTTP response other than \"Conflict\" returned");
            }
        }

        /// <summary>
        ///     Rename a page with revisions
        /// </summary>        
        /// <feature>
        /// <name>POST:pages/{pageid}/move</name>
        /// <uri>http://developer.mindtouch.com/Deki/API_Reference/POST%3apages%2f%2f%7bpageid%7d%2f%2fmove</uri>
        /// </feature>
        /// <assumption>Tree pages: A (root), A/B, A/B/C, A/B/D, A/E</assumption>
        /// <expected>Revision number to stay consistent</expected>

        [Test]
        public void TestSimpleRenameWithRevs()
        {
            // 1. Create page tree
            // 2. Edit page A/B/C twice, resulting in 3 revisions
            // 3. Move /A/B/C to /A/B/CAT
            // 4. Retrieve page A/B/C
            // (5) Assert page redirects correctly
            // (6) Assert revision count remains consistent
            //Expected result: 
            // A/B/C moved to A/B/CAT
            // redirect exists at /A/B/C to /A/B/CAT
            // A/B/CAT has 2 revs

            Plug p = Utils.BuildPlugForAdmin();

            string baseTreePath = PageUtils.BuildPageTree(p);

            DreamMessage msg = PageUtils.SavePage(p, baseTreePath + "/A/B/C", "rev 2");
            msg = PageUtils.SavePage(p, baseTreePath + "/A/B/C", "rev 3");

            msg = PageUtils.MovePage(p, baseTreePath + "/A/B/C", baseTreePath + "/A/B/CAT");

            Assert.IsTrue(Wait.For(() =>
            {
                msg = PageUtils.GetPage(p, baseTreePath + "/A/B/C");
                return (!msg.ToDocument()["path"].IsEmpty && (msg.ToDocument()["path"].AsText != baseTreePath + "/A/B/C"));
            },
               TimeSpan.FromSeconds(10)),
               "unable to find redirect");

            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Page retrieval failed (A/B/C).");
            Assert.IsTrue(msg.ToDocument()["page.redirectedfrom/page/title"].Contents == "C", "Page title is inconsistent");
            Assert.IsTrue(msg.ToDocument()["revisions/@count"].AsInt.Value >= 2, "Moved page has less than 2 revisions!");
        }

        /// <summary>
        ///     Create revisions and rename A/B, A/B/C and check redirection/revision consistency
        /// </summary>        
        /// <feature>
        /// <name>POST:pages/{pageid}/move</name>
        /// <uri>http://developer.mindtouch.com/Deki/API_Reference/POST%3apages%2f%2f%7bpageid%7d%2f%2fmove</uri>
        /// </feature>
        /// <assumption>Tree pages: A (root), A/B, A/B/C, A/B/D, A/E</assumption>
        /// <expected>Revision number to stay consistent; redirects to correctly point to moved page</expected>

        [Test]
        public void TestRenameWithChildrenWithRevs()
        {
            //Expected result: 
            // A/B moved to A/BAH
            // redirect exists at /A/B to /A/BAH
            // redirect exists at /A/B/C to /A/BAH/C
            // redirect exists at /A/B/D to /A/BAH/D
            // A/BAH, A/BAH/C have 2 revs
            // parent page stays same

            // Log in as ADMIN
            Plug p = Utils.BuildPlugForAdmin();

            // Create page tree
            string baseTreePath = PageUtils.BuildPageTree(p);

            // Create 3 revisions for /A/B and /A/B/C
            DreamMessage msg = null;
            msg = PageUtils.SavePage(p, baseTreePath + "/A/B", "rev 2");
            msg = PageUtils.SavePage(p, baseTreePath + "/A/B", "rev 3");
            msg = PageUtils.SavePage(p, baseTreePath + "/A/B/C", "rev 2");
            msg = PageUtils.SavePage(p, baseTreePath + "/A/B/C", "rev 3");

            // Move /A/B -> /A/BAH
            msg = PageUtils.GetPage(p, baseTreePath + "/A/B");
            int originalParentPageId = msg.ToDocument()["page.parent/@id"].AsInt.Value;
            msg = PageUtils.MovePage(p, baseTreePath + "/A/B", baseTreePath + "/A/BAH");
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Page move A/B -> A/BAH failed");

            Assert.IsTrue(Wait.For(() =>
            {
                msg = PageUtils.GetPage(p, baseTreePath + "/A/B");
                return (!msg.ToDocument()["path"].IsEmpty && (msg.ToDocument()["path"].AsText != baseTreePath + "/A/B"));
            },
              TimeSpan.FromSeconds(10)),
              "unable to find redirect");

            int newParentPageId = msg.ToDocument()["page.parent/@id"].AsInt ?? 0;

            // Make sure parent is consistent
            Assert.IsTrue(originalParentPageId == newParentPageId, "Page IDs before and after move do not match!");

            // Retrieve /A/BAH and verify >=2 revisions
            msg = PageUtils.GetPage(p, baseTreePath + "/A/BAH");
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Page retrieval A/BAH failed");
            Assert.IsTrue(msg.ToDocument()["revisions/@count"].AsInt.Value >= 2, "A/BAH has less than 2 revisions!");

            // Verify /A/B redirects to /A/BAH, [New move API change: title is "BAH"]
            msg = PageUtils.GetPage(p, baseTreePath + "/A/B");
            Assert.AreEqual(DreamStatus.Ok, msg.Status);
            Assert.IsTrue(msg.ToDocument()["title"].Contents == "BAH", "A/BAH title page is inconsistent");
            Assert.IsTrue(msg.ToDocument()["page.redirectedfrom/page/title"].Contents == "B", "A/BAH redirected title page is inconsistent");

            // Retrieve /A/B/C and verify correct redirection + >=2 revisions
            msg = PageUtils.GetPage(p, baseTreePath + "/A/B/C");
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Page retrieval A/B/C failed");
            Assert.IsTrue(msg.ToDocument()["title"].Contents == "C", "Page title is inconsistent A/B/C");
            Assert.IsTrue(msg.ToDocument()["path"].Contents.EndsWith("A/BAH/C"), "Page path is inconsistent A/BAH/C");
            Assert.IsTrue(msg.ToDocument()["page.redirectedfrom/page/path"].Contents.EndsWith("/A/B/C"), "Redirected from page path is inconsistent A/B/C");
            Assert.IsTrue(msg.ToDocument()["revisions/@count"].AsInt.Value >= 2, "A/BAH/C has less than 2 revisions! (A/B/C)");

            // Retrieve /A/BAH/C and verify 2>= revisions
            msg = PageUtils.GetPage(p, baseTreePath + "/A/BAH/C");
            Assert.IsTrue(msg.ToDocument()["revisions/@count"].AsInt.Value >= 2, "A/BAH/C has less than 2 revisions! (A/BAH/C)");
        }

        /// <summary>
        ///     Move created leaf page (A/E/C) to existing page (A/B)
        /// </summary>        
        /// <feature>
        /// <name>POST:pages/{pageid}/move</name>
        /// <uri>http://developer.mindtouch.com/Deki/API_Reference/POST%3apages%2f%2f%7bpageid%7d%2f%2fmove</uri>
        /// </feature>
        /// <assumption>Tree pages: A (root), A/B, A/B/C, A/B/D, A/E</assumption>
        /// <expected>409 Conflict HTTP response</expected>

        [Test]
        public void TestMoveChildOverExistingPage()
        {
            //Assumptions: 
            // 
            //Actions:
            // 1. Create page tree
            // 2. Create page /A/E/C
            // 3. Move /A/E/C to /A/B
            // (4) Assert Conflict HTTP response
            //Expected result: 
            // conflict error

            Plug p = Utils.BuildPlugForAdmin();

            string baseTreePath = PageUtils.BuildPageTree(p);

            DreamMessage msg = PageUtils.SavePage(p, baseTreePath + "/A/E/C", "sup");
            try
            {
                msg = PageUtils.MovePage(p, baseTreePath + "/A/E/C", baseTreePath + "/A/B");
                Assert.IsTrue(false, "Page move to existing node succeeded?!");
            }
            catch (DreamResponseException ex)
            {
                Assert.IsTrue(ex.Response.Status == DreamStatus.Conflict, "HTTP response other than \"Conflict\" returned");
            }
        }

        /// <summary>
        ///     Move root page (A) to leaf page (A/B/C)
        /// </summary>        
        /// <feature>
        /// <name>POST:pages/{pageid}/move</name>
        /// <uri>http://developer.mindtouch.com/Deki/API_Reference/POST%3apages%2f%2f%7bpageid%7d%2f%2fmove</uri>
        /// </feature>
        /// <assumption>Tree pages: A (root), A/B, A/B/C, A/B/D, A/E</assumption>
        /// <expected>409 Conflict HTTP response</expected>

        [Test]
        public void TestMoveNodeDown()
        {
            //Assumptions: 
            // 
            //Actions:
            // 1. Create page tree
            // 2. Move /A to /A/B/C
            // 3. Assert Conflict HTTP response returned
            //Expected result: 
            // conflict

            Plug p = Utils.BuildPlugForAdmin();

            string baseTreePath = PageUtils.BuildPageTree(p);

            try
            {
                DreamMessage msg = PageUtils.MovePage(p, baseTreePath + "/A", baseTreePath + "/A/B/C");
                Assert.IsTrue(false, "Move root node (A) to leaf node (A/B/C) succeeded?!");
            }
            catch (DreamResponseException ex)
            {
                Assert.IsTrue(ex.Response.Status == DreamStatus.Conflict, "HTTP response other than \"Conflict\" returned");
            }
        }

        /// <summary>
        ///     Move page as user when tree has a private page
        /// </summary>        
        /// <feature>
        /// <name>POST:pages/{pageid}/move</name>
        /// <uri>http://developer.mindtouch.com/Deki/API_Reference/POST%3apages%2f%2f%7bpageid%7d%2f%2fmove</uri>
        /// </feature>
        /// <assumption>Tree pages: A (root), A/B, A/B/C, A/B/D, A/E</assumption>
        /// <assumption>A/B/C set to private</assumption>
        /// <expected>403 Forbidden HTTP response</expected>

        [Test]
        public void TestMoveWithPrivateChildren()
        {
            //Actions:
            // 1. Create page tree
            // 2. Set /A/B/C as private 
            // 3. Create user with Contributor role
            // 4. Move /A to /AWESOMENESS as user
            // (5) Assert Forbidden HTTP response
            //Expected result: 
            // forbidden

            Plug p = Utils.BuildPlugForAdmin();

            string baseTreePath = PageUtils.BuildPageTree(p);

            DreamMessage msg = PageUtils.RestrictPage(p, baseTreePath + "/A/B/C", "absolute", "private");

            string userid = null;
            string username = null;
            msg = UserUtils.CreateRandomUser(p, "Contributor", "password", out userid, out username);

            p = Utils.BuildPlugForUser(username, "password");
            try
            {
                msg = PageUtils.MovePage(p, baseTreePath + "/A", baseTreePath + "/AWESOMENESS");
                Assert.IsTrue(false, "Move of page with private subpages succeeded?!");
            }
            catch (DreamResponseException ex)
            {
                Assert.IsTrue(ex.Response.Status == DreamStatus.Forbidden, "HTTP response other than \"Forbidden\" returned");
            }
        }

        /// <summary>
        ///     Create page /A/A and move /A/A to /A/B and /A to /B  and check redirect correctness
        /// </summary>        
        /// <feature>
        /// <name>POST:pages/{pageid}/move</name>
        /// <uri>http://developer.mindtouch.com/Deki/API_Reference/POST%3apages%2f%2f%7bpageid%7d%2f%2fmove</uri>
        /// </feature>
        /// <expected>/A -> /B; /A/A -> /B/B; /A/B -> /B/B</expected>

        [Test]
        public void Bug0006446_TestMoveSourceWithChildRedirects() {
            //Actions
            // 1. Create page /A/A
            // 2. Move /A/A to /A/B
            // 3. Move /A to /B
            // (4) Assert /A redirects correctly to /B
            // (5) Assert /A/A redirects correctly to B/B
            // (6) Assert /A/B redirects correctly to B/B
            // (7) Assert page /B exists
            // (8) Assert page /B/B exists
            //Expected results
            // A     -> B
            // A/A   -> B/B
            // A/B   -> B/B
            // B
            // B/B

            Plug p = Utils.BuildPlugForAdmin();
            string baseTreePath;
            string id;
            PageUtils.CreateRandomPage(p, out id, out baseTreePath);

            DreamMessage msg = PageUtils.SavePage(p, baseTreePath + "/A/A", "sup");
            msg = PageUtils.MovePage(p, baseTreePath + "/A/A", baseTreePath + "/A/B");
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Failed to move A/A to A/B");

            Assert.IsTrue(Wait.For(() =>
            {
                msg = PageUtils.GetPage(p, baseTreePath + "/A/A");
                return (!msg.ToDocument()["path"].IsEmpty && (msg.ToDocument()["path"].AsText != baseTreePath + "/A/A"));
            },
               TimeSpan.FromSeconds(10)),
               "unable to find redirect");

            msg = PageUtils.MovePage(p, baseTreePath + "/A", baseTreePath + "/B");
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Failed to move A to B");

            Assert.IsTrue(Wait.For(() =>
            {
                msg = PageUtils.GetPage(p, baseTreePath + "/A");
                return (!msg.ToDocument()["path"].IsEmpty && (msg.ToDocument()["path"].AsText != baseTreePath + "/A"));
            },
              TimeSpan.FromSeconds(10)),
              "unable to find redirect");

            //Test redirects
            msg = PageUtils.GetPage(p, baseTreePath + "/A");
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "A doesn't exist");
            Assert.AreEqual(baseTreePath + "/B", msg.ToDocument()["/page/path"].AsText, "A doesn't point to B");

            msg = PageUtils.GetPage(p, baseTreePath + "/A/A");
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "A/A doesn't exist");
            Assert.AreEqual(baseTreePath + "/B/B", msg.ToDocument()["/page/path"].AsText, "A/A doesn't point to B/B");
     
            msg = PageUtils.GetPage(p, baseTreePath + "/A/B");
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "A/B doesn't exist");
            Assert.AreEqual(baseTreePath + "/B/B", msg.ToDocument()["/page/path"].AsText, "A/B doesn't point to B/B");

            //Test pages
            msg = PageUtils.GetPage(p, baseTreePath + "/B");
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "B doesn't exist");
            Assert.AreEqual(baseTreePath + "/B", msg.ToDocument()["/page/path"].AsText, "B doesn't exist");

            msg = PageUtils.GetPage(p, baseTreePath + "/B/B");
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "B/B doesn't exist");
            Assert.AreEqual(baseTreePath + "/B/B", msg.ToDocument()["/page/path"].AsText, "B/B doesn't exist");

        }

        // ***********************
        // New Move Functionality
        // Parameters:
        // 1. name
        // 2. parentid
        // 3. title
        // ***********************
        //
        // SPEC: http://developer.mindtouch.com/en/docs/MindTouch/Specs/Clarified_Page_Title%2f%2fMove_Functionality

        [Test]
        public void Move_Case00a_ToAndLinked_CorrectTitlePath() {
            // Log in as ADMIN
            Plug p = Utils.BuildPlugForAdmin();

            // Create a linked page
            string id;
            string path;
            PageUtils.CreateRandomPage(p, out id, out path);

            // Move page to path-postfix
            string postfix = "-" + System.DateTime.Now.Ticks.ToString();
            PageUtils.MovePage(Utils.BuildPlugForAdmin().At("pages", id, "move").With("to", path + postfix));

            // New path should be path-postfix, title should be title-postfix, page remains linked
            DreamMessage msg = PageUtils.GetPage(p, path + postfix);
            string title = msg.ToDocument()["title"].AsText ?? String.Empty;
            string pathmeta = msg.ToDocument()["path"].AsText ?? String.Empty;

            Assert.AreEqual(path.Substring(path.LastIndexOf("/") + 1) + postfix, title, "Unexpected title");
            Assert.AreEqual(path + postfix, pathmeta, "Unexpected path");
            PageUtils.IsLinked(msg);
        }

        [Test]
        public void Move_Case00b_ToAndUnlinked_CorrectTitlePath() {
            // Log in as ADMIN
            Plug p = Utils.BuildPlugForAdmin();

            // Create an unlinked page
            string id;
            string path;
            string title;
            PageUtils.CreateRandomUnlinkedPage(p, out title, out id, out path);

            // Move page to path-postfix
            string postfix = "-" + System.DateTime.Now.Ticks.ToString();
            PageUtils.MovePage(Utils.BuildPlugForAdmin().At("pages", id, "move").With("to", path + postfix));

            // New path should be path-postfix, title should remain unchanged, page remains unlinked
            DreamMessage msg = PageUtils.GetPage(p, path + postfix);
            string titlemeta = msg.ToDocument()["title"].AsText ?? String.Empty;
            string pathmeta = msg.ToDocument()["path"].AsText ?? String.Empty;

            Assert.AreEqual(titlemeta, title, "Unexpected title");
            Assert.AreEqual(path + postfix, pathmeta, "Unexpected path");
            PageUtils.IsUnlinked(msg);
        }

        [Test]
        public void Move_Case00x_ToAndUnlinked_UriTitleEqualsDisplayTitle_Linked() {
            // Log in as ADMIN
            Plug p = Utils.BuildPlugForAdmin();

            // Create an unlinked page
            string id;
            string path;
            string title;
            PageUtils.CreateRandomUnlinkedPage(p, out title, out id, out path);

            // Move page to .../title
            string newpath = path.Substring(0, path.LastIndexOf("/")+1) + title;
            PageUtils.MovePage(Utils.BuildPlugForAdmin().At("pages", id, "move").With("to", newpath));

            // New path should be .../title, title should remain unchanged, page is LINKED (URI title = display title)
            DreamMessage msg = PageUtils.GetPage(p, newpath);
            string titlemeta = msg.ToDocument()["title"].AsText ?? String.Empty;
            string pathmeta = msg.ToDocument()["path"].AsText ?? String.Empty;

            Assert.AreEqual(titlemeta, title, "Unexpected title");
            Assert.AreEqual(newpath, pathmeta, "Unexpected path");
            PageUtils.IsLinked(msg);
        }

        [Test]
        public void Move_Case01a_ToAndNameParentID_BadRequest()
        {
            // Build ADMIN plug
            Plug p = Utils.BuildPlugForAdmin();

            // Create a random page
            string id;
            string path;
            PageUtils.CreateRandomPage(p, out id, out path);

            // Move with "to" and "name" query parameters
            string newtitle = "fail";
            DreamMessage msg = p.At("pages", "=" + XUri.DoubleEncode(path), "move")
                .With("name", XUri.Encode(newtitle))
                .With("to", XUri.Encode(newtitle)).PostAsync().Wait();
            Assert.AreEqual(msg.Status, DreamStatus.BadRequest, "Combining 'to' and 'name' did not yield a Bad Request response");

            // Move with "to" and "parentid" query parameters
            msg = p.At("pages", "=" + XUri.DoubleEncode(path), "move")
                .With("parentid", id)
                .With("to", XUri.Encode(newtitle)).PostAsync().Wait();
            Assert.AreEqual(msg.Status, DreamStatus.BadRequest, "Combining 'to' and 'parentpageid' did not yield a Bad Request response");

            // Delete the page
            PageUtils.DeletePageByID(p, id, true);
        }

        [Test]
        public void Move_Case01b_ToTitle_CorrectPathTitle()
        {
            // Build ADMIN plug
            Plug p = Utils.BuildPlugForAdmin();

            // Create a random page where name == title
            string id;
            string path;
            PageUtils.CreateRandomPage(p, out id, out path);

            // Move to path-postfix/Z with title="test". Make sure redirect exists to retrieve page from original path
            string postfix = "-" + System.DateTime.Now.Ticks.ToString();
            string newtitle = "test";
            string newpath = path + postfix + "/Z";
            PageUtils.MovePageAndWait(p.At("pages", id, "move")
                .With("to", newpath)
                .With("title", newtitle), path);

            // Retrieve "path" and "title" metadata for page at path-postfix
            DreamMessage msg = PageUtils.GetPage(p, path);
            string pathmeta = msg.ToDocument()["path"].AsText;
            string titlemeta = msg.ToDocument()["title"].AsText;

            // path should be newpath, display title should be newtitle, page should be unlinked
            Assert.AreEqual(newpath, pathmeta, "Page move did not update path correctly!");
            Assert.AreEqual(newtitle, titlemeta, "Page title changed unexpectedly!");
            PageUtils.IsUnlinked(msg);
        }

        [Test]
        public void Move_Case02_Name_CorrectPathTitleParent()
        {
            // Build ADMIN plug
            Plug p = Utils.BuildPlugForAdmin();

            // Create a random page where name == title
            string id;
            string path;
            PageUtils.CreateRandomPage(p, out id, out path);

            // Verify page is linked
            DreamMessage msg = PageUtils.GetPage(p, path);
            PageUtils.IsLinked(msg);

            // Retrieve page title and parent
            string title = msg.ToDocument()["title"].AsText;
            string parentid = msg.ToDocument()["page.parent/@id"].AsText;

            // Move (or rename) to title-postfix
            string postfix = "-" + System.DateTime.Now.Ticks.ToString();
            string newname = title + postfix;
            PageUtils.MovePage(Utils.BuildPlugForAdmin().At("pages", "=" + XUri.DoubleEncode(path), "move")
                .With("name", XUri.Encode(newname)));

            // URI title should be title-postfix, title/parentid should remain unchanged, page is now unlinked
            string despath = path + postfix;
            msg = PageUtils.GetPage(p, despath);
            string pathmeta = msg.ToDocument()["path"].AsText;
            string titlemeta = msg.ToDocument()["title"].AsText;
            string parentidmeta = msg.ToDocument()["page.parent/@id"].AsText;

            Assert.AreEqual(despath, pathmeta, "Unexpected name/path");
            Assert.AreEqual(title, titlemeta, "Title changed unexpectedly!");
            Assert.AreEqual(parentid, parentidmeta, "Page parent ID changed unexpectedly!");
        }

        [Test]
        public void Move_Case03_ParentID_CorrectPathTitleParent() {
            //Assumptions: 
            //      A                A
            //     / \              / 
            //    B   E   ---->    B   
            //   / \              / \ 
            //  C   D            C   D
            //                  /
            //                 E
            //Actions:
            // move E as a child of C
            //Expected result: 
            // E is located at /A/B/C/E

            // Build ADMIN plug and create a page tree
            Plug p = Utils.BuildPlugForAdmin();
            string baseTreePath = PageUtils.BuildPageTree(p);

            //Resolve new parent page of move
            DreamMessage msg = PageUtils.GetPage(p, baseTreePath + "/A/B/C");
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Could not locate /A/B/C");
            uint pageCid = msg.AsDocument()["@id"].AsUInt ?? 0;
            Assert.IsTrue(pageCid > 0, "Could not locate id of /A/B/C");

            //Fetch name and title of page being moved
            msg = PageUtils.GetPage(p, baseTreePath + "/A/E");
            string name = msg.AsDocument()["path"].AsText;
            name = name.Remove(0,  name.LastIndexOf("/")+1);
            string title = msg.AsDocument()["title"].AsText;

            //Perform the move by specifying a new parentpageid
            msg = p.At("pages", "=" + XUri.DoubleEncode(baseTreePath + "/A/E"), "move").With("parentid", pageCid).PostAsync().Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Page move failed!");

            // Retrieve page metadata
            msg = PageUtils.GetPage(p, baseTreePath + "/A/B/C/E");
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Page does not exist at expected path");

            string parentidmeta = msg.ToDocument()["page.parent/@id"].AsText;
            string titlemeta = msg.ToDocument()["title"].AsText;
            string namemeta = msg.ToDocument()["path"].AsText;
            namemeta = namemeta.Remove(0, namemeta.LastIndexOf("/") + 1);

            // Assert page moved correctly and name and title remain consistent.
            Assert.AreEqual(pageCid.ToString(), parentidmeta, "Page parent ID is incorrect!");
            Assert.AreEqual(title, titlemeta, "Page title changed unexpectedly!");
            Assert.AreEqual(name, namemeta, "Page name changed unexpectedly!");

            // Delete page tree
            PageUtils.DeletePageByName(p, baseTreePath, true);
        }

        [Test]
        public void Move_Case04_Title_CorrectPathParentTitle()
        {
            // Build ADMIN plug
            Plug p = Utils.BuildPlugForAdmin();

            // Create a random page
            string id;
            string path;
            PageUtils.CreateRandomPage(p, out id, out path);

            // Verify page is linked
            DreamMessage msg = PageUtils.GetPage(p, path);
            PageUtils.IsLinked(msg);

            // Retrieve page title and parent
            string title = msg.ToDocument()["title"].AsText;
            string parentid = msg.ToDocument()["page.parent/@id"].AsText;

            // Change title to title-postfix
            string postfix = "-" + System.DateTime.Now.Ticks.ToString();
            string newtitle = title + postfix;
            PageUtils.MovePageAndWait(Utils.BuildPlugForAdmin().At("pages", "=" + XUri.DoubleEncode(path), "move")
                .With("title", newtitle), path);

            // Retrieve "name", "title", and "parentid" metadata for moved page
            msg = PageUtils.GetPage(p, path);

            // title should acquire new-title value, URI title (name) should equal new-title, parent should not change, page remains linked
            string namemeta = msg.ToDocument()["path"].AsText;
            namemeta = namemeta.Remove(0, namemeta.LastIndexOf("/") + 1);
            string titlemeta = msg.ToDocument()["title"].AsText;
            string parentidmeta = msg.ToDocument()["page.parent/@id"].AsText;

            Assert.AreEqual(newtitle, namemeta, "Page move did reset name!");
            Assert.AreEqual(newtitle, titlemeta, "Page title changed unexpectedly!");
            Assert.AreEqual(parentid, parentidmeta, "Page parent ID changed unexpectedly!");

            // Delete the Page
            PageUtils.DeletePageByID(p, id, true);
        }

        [Test]
        public void Move_Case05_NameParent_CorrectPathTitleParent()
        {
            //Assumptions: 
            //                       A     
            //     A                 |    
            //    / \                E   
            //   B   E    ----->     |   
            //  / \                  Z  (title: B)
            // C   D                / \   
            //                     C   D                   
            //Actions:
            // move B to a child of E and rename it to Z
            //Expected result: 
            // New hierarchy as follows:
            // A
            // A/E
            // A/E/Z
            // A/E/Z/C
            // A/E/Z/D

            // Build ADMIN plug and create a page tree
            Plug p = Utils.BuildPlugForAdmin();
            string baseTreePath = PageUtils.BuildPageTree(p);

            //Resolve new parent page of move
            DreamMessage msg = PageUtils.GetPage(p, baseTreePath + "/A/E");
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Could not locate /A/E");
            uint pageEid = msg.AsDocument()["@id"].AsUInt ?? 0;
            Assert.IsTrue(pageEid > 0, "Could not locate id of /A/E");

            //Fetch title of page being moved
            msg = PageUtils.GetPage(p, baseTreePath + "/A/B");
            string title = msg.AsDocument()["title"].AsText;

            //Perform the move by specifying a new parentpageid and set the new name
            msg = p.At("pages", "=" + XUri.DoubleEncode(baseTreePath + "/A/B"), "move")
                .With("name", "Z")
                .With("parentid", pageEid).PostAsync().Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Page move failed!");

            
            // Retrieve metadata of moved and renamed page
            msg = PageUtils.GetPage(p, baseTreePath + "/A/E/Z");
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Page does not exist at expected path: /A/E/Z");

            string parentidmeta = msg.ToDocument()["page.parent/@id"].AsText;
            string titlemeta = msg.ToDocument()["title"].AsText;
            string namemeta = msg.ToDocument()["path"].AsText;
            namemeta = namemeta.Remove(0, namemeta.LastIndexOf("/") + 1);

            // Assert page moved and renamed correctly and title remains consistent.
            Assert.AreEqual(pageEid.ToString(), parentidmeta, "Page parent ID is incorrect!");
            Assert.AreEqual("B", titlemeta, "Page title changed unexpectedly!");
            Assert.AreEqual("Z", namemeta, "Page name changed unexpectedly!");

            // Assert child/parent pages also moved correctly
            msg = PageUtils.GetPage(p, baseTreePath + "/A/E");
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Page does not exist at expected path: /A/E");
           
            msg = PageUtils.GetPage(p, baseTreePath + "/A/E/Z/C");
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Page does not exist at expected path: /A/E/Z/C");

            msg = PageUtils.GetPage(p, baseTreePath + "/A/E/Z/D");
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Page does not exist at expected path: /A/E/Z/D");

            // Delete page tree
            PageUtils.DeletePageByName(p, baseTreePath, true);
        }

        [Test]
        public void Move_Case06_NameTitle_CorrectPathTitleParent()
        {
            // Build ADMIN plug
            Plug p = Utils.BuildPlugForAdmin();

            // Create a random page where name == title
            string id;
            string path;
            PageUtils.CreateRandomPage(p, out id, out path);

            // Verify page is linked
            DreamMessage msg = PageUtils.GetPage(p, path);
            PageUtils.IsLinked(msg);

            // Retrieve page title & parent
            string title = msg.ToDocument()["title"].AsText;
            string parentid = msg.ToDocument()["page.parent/@id"].AsText;

            // Change title to title-postfix and name to name-postfix/2
            long postfix = System.DateTime.Now.Ticks;
            string newtitle = title + "-" + postfix.ToString();
            string newname = title + "-" + (postfix / 2).ToString();
            PageUtils.MovePage(Utils.BuildPlugForAdmin().At("pages", "=" + XUri.DoubleEncode(path), "move")
                .With("title", newtitle)
                .With("name", newname));

            // Retrieve "name", "title", and "parentid" metadata for page at name-postfix/2
            string despath = path + "-" + (postfix/2).ToString();
            msg = PageUtils.GetPage(p, despath);

            // name should be name-postfix/2, title should be title-postfix, parentid should remain unchanged, page is unlinked 
            string namemeta = msg.ToDocument()["path"].AsText;
            namemeta = namemeta.Remove(0, namemeta.LastIndexOf("/") + 1);
            string titlemeta = msg.ToDocument()["title"].AsText;
            string parentidmeta = msg.ToDocument()["page.parent/@id"].AsText;

            Assert.AreEqual(newname, namemeta, "Page name segment is incorrect!");
            Assert.AreEqual(newtitle, titlemeta, "Page title is incorrect!");
            Assert.AreEqual(parentid, parentidmeta, "Page parent ID changed unexpectedly!");
            PageUtils.IsUnlinked(msg);
        }

        [Test]
        public void Move_Case07_ParentIDDisplayName_CorrectPathTitleParent()
        {
            //Assumptions: 
            //                  A   
            //     A           / \  
            //    / \         B   E 
            //   B   E        |    
            //  / \           D   
            // C   D          |
            //               foo (title: foo)
            //Actions:
            // Move is called on A/B/C with an empty ?name= , a parentpage id of D, and with ?displayname=foo
            //Expected result: 
            // A
            // A/B
            // A/B/D/foo (displayname=foo)
            // A/E

            // Build ADMIN plug and create a page tree
            Plug p = Utils.BuildPlugForAdmin();
            string root = PageUtils.BuildPageTree(p);

            //Resolve new parent page of move
            DreamMessage msg = PageUtils.GetPage(p, root + "/A/B/D");
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Could not locate /A/B/D");
            uint pageDid = msg.AsDocument()["@id"].AsUInt ?? 0;
            Assert.IsTrue(pageDid > 0, "Could not locate id of /A/B/D");

            //Perform the move
            msg = p.At("pages", "=" + XUri.DoubleEncode(root + "/A/B/C"), "move")
                .With("parentid", pageDid)
                .With("title", "foo").PostAsync().Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Page move failed");

            //Verify move
            msg = PageUtils.GetPage(p, root + "/A/B/D/foo");
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Could not locate /A/B/D/foo");

            string parentidmeta = msg.ToDocument()["page.parent/@id"].AsText;
            string titlemeta = msg.ToDocument()["title"].AsText;
            string namemeta = msg.ToDocument()["path"].AsText;
            namemeta = namemeta.Remove(0, namemeta.LastIndexOf("/") + 1);

            // Assert page moved and renamed correctly and title remains consistent.
            Assert.AreEqual(pageDid.ToString(), parentidmeta, "Page parent ID is incorrect!");
            Assert.AreEqual("foo", msg.AsDocument()["title"].AsText ?? string.Empty, "unexpected title");
            Assert.AreEqual("foo", namemeta, "Page name was not reset!");

            // Assert page is linked
            Assert.AreEqual(msg.ToDocument()["path/@type"].AsText ?? String.Empty, String.Empty, "Page path is unlinked!"); 

            // Delete page tree
            PageUtils.DeletePageByName(p, root, true);
        }

        [Test]
        public void Move_Case08_NameParentIDTitle_CorrectPathTitleParent()
        {
            //      A                A
            //     / \                \
            //    B   E     --->       E
            //   / \                    \
            //  C   D                    Z   Title: ZZZ
            //                          / \
            //                         C   D

            // Build ADMIN plug and create a page tree
            Plug p = Utils.BuildPlugForAdmin();
            string root = PageUtils.BuildPageTree(p);

            //Resolve new parent page of move
            DreamMessage msg = PageUtils.GetPage(p, root + "/A/E");
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Could not locate /A/E");
            uint pageEid = msg.AsDocument()["@id"].AsUInt ?? 0;
            Assert.IsTrue(pageEid > 0, "Could not locate id of /A/E");

            //Perform the move by specifying a new parentpageid and set the new name
            msg = p.At("pages", "=" + XUri.DoubleEncode(root + "/A/B"), "move")
                .With("name", "Z")
                .With("parentid", pageEid)
                .With("title", "ZZZ").PostAsync().Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Page move failed!");

            // Retrieve metadata of moved, renamed, and retitled page
            msg = PageUtils.GetPage(p, root + "/A/E/Z");
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Page does not exist at expected path: /A/E/Z");

            string parentidmeta = msg.ToDocument()["page.parent/@id"].AsText;
            string titlemeta = msg.ToDocument()["title"].AsText;
            string namemeta = msg.ToDocument()["path"].AsText;
            namemeta = namemeta.Remove(0, namemeta.LastIndexOf("/") + 1);

            // Assert page moved and renamed correctly and title remains consistent.
            Assert.AreEqual(pageEid.ToString(), parentidmeta, "Page parent ID is incorrect!");
            Assert.AreEqual("ZZZ", titlemeta, "Page title is incorrect!");
            Assert.AreEqual("Z", namemeta, "Page name is incorrect!");

            // Assert page is now unlinked
            Assert.AreEqual(msg.ToDocument()["path/@type"].AsText ?? String.Empty, "custom", "Page path is linked?!"); 

            // Delete the Page
            PageUtils.DeletePageByName(p, root, true);
        }

        [Test]
        public void Move_Case09_EmptyNameParentIDTitle_BadRequest()
        {
            // Build ADMIN plug
            Plug p = Utils.BuildPlugForAdmin();

            // Create a random page
            string id;
            string path;
            PageUtils.CreateRandomPage(p, out id, out path);

            // Move with empty "name" query parameter
            DreamMessage msg = p.At("pages", "=" + XUri.DoubleEncode(path), "move")
                .With("name", String.Empty).PostAsync().Wait();
            Assert.AreEqual(msg.Status, DreamStatus.BadRequest, "An empty 'name' parameter did not yield a Bad Request response");

            // Move with empty "parentit" query parameter
            msg = p.At("pages", "=" + XUri.DoubleEncode(path), "move")
                .With("parentid", String.Empty).PostAsync().Wait();
            Assert.AreEqual(msg.Status, DreamStatus.BadRequest, "An empty 'parentid' parameter did not yield a Bad Request response");

            // Move with empty "name" query parameter
            msg = p.At("pages", "=" + XUri.DoubleEncode(path), "move")
                .With("title", String.Empty).PostAsync().Wait();
            Assert.AreEqual(msg.Status, DreamStatus.BadRequest, "An empty 'title' parameter did not yield a Bad Request response");

            // Delete the page
            PageUtils.DeletePageByID(p, id, true);
        }

        [Test]
        public void Move_Case10x_NameTitle_LongLinkedTest()
        {
            // Test Procedure:
            //
            // I. Create linked page (name == title)
            //      1. Link -> Unlink (name != title)
            //      2. Unlink -> Link (name omitted)
            // II. Create unlinked page (name != title)
            //      1. Unlink -> Link (name == title)
            //      2. Link -> Unlink (title omitted && name != existing title)

            // Build ADMIN plug
            Plug p = Utils.BuildPlugForAdmin();

            // (I) Create a random page
            string id;
            string path;
            PageUtils.CreateRandomPage(p, out id, out path);

            // Random page creation should have title == name, thus linked
            DreamMessage msg = PageUtils.GetPage(p, path);
            PageUtils.IsLinked(msg);

            // (1) Unlink page (name != title)
            string name = path.Substring(path.LastIndexOf("/")+1);
            string newname = name + "-" + System.DateTime.Now.Ticks.ToString();
            string newtitle = name + "-" + (System.DateTime.Now.Ticks / 2).ToString();
            PageUtils.MovePage(Utils.BuildPlugForAdmin().At("pages", id, "move")
                .With("name", newname)
                .With("title", newtitle));

            // Check to see if page is unlinked
            msg = PageUtils.GetPage(p, path);
            PageUtils.IsUnlinked(msg);

            // (2) Link page back (omit name)
            newtitle = name + "-" + System.DateTime.Now.Ticks.ToString();
            PageUtils.MovePage(Utils.BuildPlugForAdmin().At("pages", id, "move")
                .With("title", newtitle));

            // Check to see if page title and name are linked once more
            msg = PageUtils.GetPage(p, path);
            PageUtils.IsLinked(msg);

            // (II) Create random page where title != path
            string title;
            PageUtils.CreateRandomUnlinkedPage(p, out title, out id, out path);

            // (1) link page (name == title)
            PageUtils.MovePage(Utils.BuildPlugForAdmin().At("pages", id, "move")
                .With("title", title).With("name", title));

            // Verify page is linked
            msg = PageUtils.GetPage(p, path);
            PageUtils.IsLinked(msg);

            // (2) Unlink page one last time (name != title)
            name = path.Substring(path.LastIndexOf("/") + 1);
            newtitle = name + "-" + System.DateTime.Now.Ticks.ToString();
            PageUtils.MovePage(Utils.BuildPlugForAdmin().At("pages", id, "move")
                .With("title", newtitle));

            // Verify page is unlinked and celebrate good times
            msg = PageUtils.GetPage(p, path);
            PageUtils.IsUnlinked(msg); 
        }

        [Test]
        public void Move_Case11_Title_ParsedCorrectly()
        {
            // Test cases <Input Title, Expected Result Title>
            // null == BAD REQUEST
            // FEEL FREE TO ADD TEST CASES
            Dictionary<string, string> d = new Dictionary<string,string>();
            d.Add(String.Empty, null);
            d.Add(" ", null);
            d.Add("       ", null);
            d.Add("   hello", "hello");
            d.Add("hello      ", "hello");
            d.Add("       hello    ", "hello");
            d.Add("  he l l   o   ", "he l l   o");
            d.Add("///", "///");
            d.Add("abcdefghijlmnopqrstuvwxyz   1234567890-=_+)(*&^%$#@!~`/?\\\"><:';{}[]\\|.,", "abcdefghijlmnopqrstuvwxyz   1234567890-=_+)(*&^%$#@!~`/?\\\"><:';{}[]\\|.,");

            // Build ADMIN plug
            Plug p = Utils.BuildPlugForAdmin();

            // Create a random page
            DreamMessage msg;
            string id;
            string path;
            string title;
            PageUtils.CreateRandomPage(p, out id, out path);

            // Save the name so it won't be reset
            string name = path.Substring(path.LastIndexOf("/") + 1);

            // Perform the title change for each value
            foreach(KeyValuePair<string, string> testcase in d)
            {
                msg = p.At("pages", "=" + XUri.DoubleEncode(path), "move")
                .With("name", name)
                .With("title", testcase.Key).PostAsync().Wait();
                if (testcase.Value == null)
                    Assert.AreEqual(DreamStatus.BadRequest, msg.Status, String.Format("Title change succeeded for a bad input! TITLE: '{0}' EXPECTED: '{1}'", testcase.Key, testcase.Value ?? "Bad Request"));
                else
                {
                    Assert.AreEqual(DreamStatus.Ok, msg.Status, String.Format("Title change failed for a good input! TITLE: '{0}' EXPECTED: '{1}'", testcase.Key, testcase.Value ?? "Bad Request"));

                    // Fetch page XML document
                    msg = p.At("pages", id).GetAsync().Wait();
                    Assert.AreEqual(DreamStatus.Ok, msg.Status, "Page retrieval failed!");

                    // Retrieve title and assert it matches expected value
                    title = msg.ToDocument()["title"].AsText;
                    Assert.AreEqual(testcase.Value, title, "Title does not match expected value!");
                }
            }

            // Delete the page
            PageUtils.DeletePageByID(p, id, true);
        }

        [Test]
        public void Move_Case12_Name_ParsedCorrectly()
        {
            // Test cases <Input Name, Expected Result Name>
            // null == BAD REQUEST || CONFLICT
            // FEEL FREE TO ADD TEST CASES
            Dictionary<string, string> d = new Dictionary<string, string>();
            d.Add(String.Empty, null);
            d.Add(" ", null);
            d.Add("       ", null);
            d.Add("   hello", "hello");
            d.Add("hello      ", "hello");
            d.Add("       hello    ", "hello");
            d.Add("  he l l   o   ", "he_l_l___o");
            d.Add("/", null); 
            d.Add("a/b", "a//b");
           // d.Add("/z", "z"); // edge case
            d.Add("x/", "x");
            d.Add("abc//def/g/h", "abc////def//g//h");
            d.Add("abcdefghijklmnopqrstuvwxyz 0123456789 !@#$%^&*()_+\"\\/:;.,<>{}[]-=|", "abcdefghijklmnopqrstuvwxyz_0123456789_!@%23$%25^&*()__\"\\//:;.,%3C%3E%7B%7D%5B%5D-=%7C");
            d.Add("+", null);
            d.Add("_", null);
            d.Add("+abc+", "abc");
            d.Add("+def++efg+++", "def__efg");
            d.Add("_hij_", "hij");
            d.Add("klmnop____qrstu_", "klmnop____qrstu");

            // Build ADMIN plug
            Plug p = Utils.BuildPlugForAdmin();

            // Create a random page
            DreamMessage msg;
            string name;
            string id;
            string path;
            PageUtils.CreateRandomPage(p, out id, out path);

            int finalSegIndex = path.LastIndexOf("/");

            // Perform the title change for each value
            foreach (KeyValuePair<string, string> testcase in d)
            {
                msg = p.At("pages", "=" + XUri.DoubleEncode(path), "move")
                .With("name", testcase.Key).PostAsync().Wait();
                if (testcase.Value == null)
                    Assert.IsTrue(DreamStatus.BadRequest.Equals(msg.Status) || DreamStatus.Conflict.Equals(msg.Status), String.Format("Name change succeeded for a bad input! NAME: '{0}' EXPECTED: '{1}'", testcase.Key, testcase.Value ?? "Bad Request"));
                else
                {
                    Assert.AreEqual(DreamStatus.Ok, msg.Status, String.Format("Name change failed for a good input! NAME: '{0}' EXPECTED: '{1}'", testcase.Key, testcase.Value ?? "Bad Request"));

                    // Fetch page XML document
                    msg = p.At("pages", id).GetAsync().Wait();
                    Assert.AreEqual(DreamStatus.Ok, msg.Status, "Page retrieval failed!");

                    // Retrieve name and assert it matches expected value
                    name = msg.ToDocument()["path"].AsText;
                    name = name.Remove(0, finalSegIndex+1);
                    Assert.AreEqual(testcase.Value, name, "Title does not match expected value!");
                }
            }

            // Delete the page
            PageUtils.DeletePageByID(p, id, true);
        }
        
        [Test]
        public void Move_Case13_NameToTitleConversion_ParsedCorrectly()
        {
            // Test cases <Input Title, Expected Result Name>
            // null == BAD REQUEST
            // FEEL FREE TO ADD TEST CASES
            Dictionary<string, string> d = new Dictionary<string, string>();
            d.Add(String.Empty, null);
            d.Add(" ", null);
            d.Add("       ", null);
            d.Add("   hello", "hello");
            d.Add("hello      ", "hello");
            d.Add("       hello    ", "hello");
            d.Add("  he l l   o   ", "he_l_l___o");
            d.Add("blah", "blah");
            d.Add("title:", "title:");
            // d.Add("a/b/c", "a//b//c"); //edge case, bug 8074
            d.Add("%", "%25");

            // Build ADMIN plug
            Plug p = Utils.BuildPlugForAdmin();

            // Create a random page
            DreamMessage msg;
            string name;
            string id;
            string path;
            PageUtils.CreateRandomPage(p, out id, out path);

            int finalSegIndex = path.LastIndexOf("/");

            // Perform the title change for each value
            foreach (KeyValuePair<string, string> testcase in d)
            {
                msg = p.At("pages", "=" + XUri.DoubleEncode(path), "move")
                .With("title", testcase.Key).PostAsync().Wait();
                if (testcase.Value == null)
                    Assert.AreEqual(DreamStatus.BadRequest, msg.Status, String.Format("Title change and name reset succeeded for a bad input! TITLE: '{0}' EXPECTED: '{1}'", testcase.Key, testcase.Value ?? "Bad Request"));
                else
                {
                    Assert.AreEqual(DreamStatus.Ok, msg.Status, String.Format("Title change and name reset failed for a good input! TITLE: '{0}' EXPECTED: '{1}'", testcase.Key, testcase.Value ?? "Bad Request"));

                    // Fetch page XML document
                    msg = p.At("pages", id).GetAsync().Wait();
                    Assert.AreEqual(DreamStatus.Ok, msg.Status, "Page retrieval failed!");

                    // Retrieve name and assert it matches expected value
                    name = msg.ToDocument()["path"].AsText;
                    name = name.Remove(0, finalSegIndex + 1);
                    Assert.AreEqual(testcase.Value, name, "Title does not match expected value!");
                }
            }

            // Delete the page
            PageUtils.DeletePageByID(p, id, true);
        }

        [Test]
        public void Move_Name_GetARedirectsToB()
        {
            // Build ADMIN plug
            Plug p = Utils.BuildPlugForAdmin();

            // Create a random page
            string id;
            string path;
            PageUtils.CreateRandomPage(p, out id, out path);

            // Retrieve page title
            DreamMessage msg = PageUtils.GetPage(p, path);
            string title = msg.ToDocument()["title"].AsText;

            // Move (or rename) to title-postfix
            string postfix = "-" + System.DateTime.Now.Ticks.ToString();
            string newname = title + postfix;
            PageUtils.MovePageAndWait(Utils.BuildPlugForAdmin().At("pages", "=" + XUri.DoubleEncode(path), "move")
                .With("name", newname), path);
        }

        [Test]
        public void Move_ParentID_MoveToChild_Conflict()
        {
            //      A  ---              
            //     / \    |   
            //    B   E   |   Try to set D as parent page of A             
            //   / \      |           
            //  C   D  <--              

            // Build ADMIN plug and create a page tree
            Plug p = Utils.BuildPlugForAdmin();
            string root = PageUtils.BuildPageTree(p);

            // Resolve new parent page of move
            DreamMessage msg = PageUtils.GetPage(p, root + "/A/B/D");
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Could not locate /A/B/D");
            uint pageDid = msg.AsDocument()["@id"].AsUInt ?? 0;
            Assert.IsTrue(pageDid > 0, "Could not locate id of /A/B/D");

            // Attempt to move A as child of D
            msg = p.At("pages", "=" + XUri.DoubleEncode(root + "/A"), "move")
                .With("parentid", XUri.Encode(pageDid.ToString())).PostAsync().Wait();
            Assert.AreEqual(DreamStatus.Conflict, msg.Status, "Moving a page to its child did not result in Conflict!");

            // Delete the Tree
            PageUtils.DeletePageByName(p, root, true);
        }

        [Test]
        public void Move_NameTitle_RenameRootPages_CorrectTitle()
        {
            Plug p = Utils.BuildPlugForAdmin();

            // Save existing homepage title
            DreamMessage msg = PageUtils.GetPage(p, "");
            string title = msg.ToDocument()["title"].AsText;

            // Change homepage title
            string postfix = "-" + System.DateTime.Now.Ticks.ToString();
            msg = p.At("pages", "home", "move").With("title", title + postfix).PostAsync().Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Home page set new title failed!");

            // Set back to previous title
            msg = p.At("pages", "home", "move").With("title", title).PostAsync().Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Home page set old title failed!");
        }

        [Test]
        public void Move_SpecialToMain_Conflict()
        {
            Plug p = Utils.BuildPlugForAdmin();

            // Create special page
            string specialPage = "Special:test-" + DateTime.Now.Ticks.ToString();
            DreamMessage msg = p.At("pages", "=" + XUri.DoubleEncode(specialPage), "contents")
                .PostAsync(DreamMessage.Ok(MimeType.TEXT_UTF8, "Special Page Move Test")).Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Special page creation failed!");

            // Retrieve main namespace root page ID
            msg = PageUtils.GetPage(p, "");
            int parentid = msg.ToDocument()["@id"].AsInt ?? 0;
            Assert.IsTrue(parentid > 0, "Failed to retrieve root page ID");

            // Move special page to main namespace
            msg = p.At("pages", "=" + XUri.DoubleEncode(specialPage), "move")
                .With("parentid", parentid).PostAsync().Wait();
            Assert.AreEqual(DreamStatus.Conflict, msg.Status, "Moving special page to main namespace succeeded?!");

            // Delete page
            PageUtils.DeletePageByName(p, specialPage, true);
        }

        [Test]
        public void Move_TemplateToMain_Conflict() {
            Plug p = Utils.BuildPlugForAdmin();

            // Create template
            string template = "Template:test-" + DateTime.Now.Ticks.ToString();
            DreamMessage msg = p.At("pages", "=" + XUri.DoubleEncode(template), "contents")
                .PostAsync(DreamMessage.Ok(MimeType.TEXT_UTF8, "Template Page Move Test")).Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Template page creation failed!");

            // Retrieve main namespace root page ID
            msg = PageUtils.GetPage(p, "");
            int parentid = msg.ToDocument()["@id"].AsInt ?? 0;
            Assert.IsTrue(parentid > 0, "Failed to retrieve root page ID");

            // Move template to main namespace
            msg = p.At("pages", "=" + XUri.DoubleEncode(template), "move")
                .With("parentid", parentid).PostAsync().Wait();
            Assert.AreEqual(DreamStatus.Conflict, msg.Status, "Moving template page to main namespace succeeded?!");

            // Delete page
            PageUtils.DeletePageByName(p, template, true);
        }

        [Test]
        public void Move_RootToSomePage_Conflict() {

            // Log in as ADMIN
            Plug p = Utils.BuildPlugForAdmin();

            // Create a page
            string id;
            string path;
            PageUtils.CreateRandomPage(p, out id, out path);

            // Attempt to move root as child of page
            DreamMessage msg = p.At("pages", "home", "move").With("parentid", id).Post(new Result<DreamMessage>()).Wait();
            Assert.AreEqual(DreamStatus.Conflict, msg.Status, "Root page move succeeded?!");
        }

        [Test]
        public void Move_TalkPage_Conflict() {

            // Log in as ADMIN
            Plug p = Utils.BuildPlugForAdmin();

            // Create a page and talk page
            string pageid;
            string pagepath;
            string talkid;
            string talkpath;
            PageUtils.CreatePageAndTalkPage(p, out pageid, out pagepath, out talkid, out talkpath);

            // Attempt to move talk page as child of root
            DreamMessage msg = PageUtils.GetPage(p, "");
            int parentid = msg.ToDocument()["@id"].AsInt ?? 0;
            Assert.IsTrue(parentid > 0, "Failed to retrieve root page ID");
            msg = p.At("pages", talkid, "move").With("parentid", parentid).PostAsync().Wait();
            Assert.AreEqual(DreamStatus.Conflict, msg.Status, "Moving talk page succeeded?!");

            // Try to rename talk page to something else
            msg = p.At("pages", talkid, "move").With("name", "Talk:" + Utils.GenerateUniqueName()).PostAsync().Wait();
            Assert.AreEqual(DreamStatus.BadRequest, msg.Status, "Renaming talk page succeeded?!");
        }

        [Test]
        public void Move_SameTitle_NewName_CorrectNameTitle() {
            // Log in as ADMIN
            Plug p = Utils.BuildPlugForAdmin();

            // Create a linked page (name == title)
            string id;
            string path;
            PageUtils.CreateRandomPage(p, out id, out path);

            // Move page to title=title, name=newname
            string name = path.Substring(path.LastIndexOf("/") + 1);
            string title = name; 
            string postfix = "-" + DateTime.Now.Ticks.ToString();
            string newname = name + postfix;
            DreamMessage msg = p.At("pages", id, "move").With("title", title).With("name", newname).Post(new Result<DreamMessage>()).Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Page move failed");

            // Page should be unlinked, title should not have changed, and name should be the new name
            msg = PageUtils.GetPage(p, path + postfix);
            Assert.AreEqual(title, msg.ToDocument()["title"].AsText ?? String.Empty, "Unexpected title");
            Assert.AreEqual(path + postfix, msg.ToDocument()["path"].AsText ?? String.Empty, "Unexpected path/name");
            Assert.AreEqual("custom", msg.ToDocument()["path/@type"].AsText ?? String.Empty, "Page is not unlinked!");
        }

        [Test]
        public void Move_NewTitle_SameName_CorrectNameTitle() {
            // Log in as ADMIN
            Plug p = Utils.BuildPlugForAdmin();

            // Create a linked page (name == title)
            string id;
            string path;
            PageUtils.CreateRandomPage(p, out id, out path);

            // Move page to title=newtitle, name=name
            string name = path.Substring(path.LastIndexOf("/") + 1);
            string title = name;
            string postfix = "-" + DateTime.Now.Ticks.ToString();
            string newtitle = title + postfix;
            PageUtils.MovePage(p.At("pages", id, "move").With("title", newtitle).With("name", name));

            // Page should be unlinked, title should have changed, and name should not have changed
            DreamMessage msg = PageUtils.GetPage(p, path);
            Assert.AreEqual(newtitle, msg.ToDocument()["title"].AsText ?? String.Empty, "Unexpected title");
            Assert.AreEqual(path, msg.ToDocument()["path"].AsText ?? String.Empty, "Unexpected path/name");
            Assert.AreEqual("custom", msg.ToDocument()["path/@type"].AsText ?? String.Empty, "Page is not unlinked!");
        }

        [Test]
        public void Move_NewTitle_SamePath_CorrectPathTitle() {
            // Log in as ADMIN
            Plug p = Utils.BuildPlugForAdmin();

            // Create a linked page (name == title)
            string id;
            string path;
            PageUtils.CreateRandomPage(p, out id, out path);

            // Move page to title=newtitle, to=path
            string title = "new title";
            PageUtils.MovePage(p.At("pages", id, "move").With("title", title).With("to", path));

            // Page should be unlinked, title should have changed, path should not have changed
            DreamMessage msg = PageUtils.GetPage(p, path);
            Assert.AreEqual(title, msg.ToDocument()["title"].AsText ?? String.Empty, "Unexpected title");
            Assert.AreEqual(path, msg.ToDocument()["path"].AsText ?? String.Empty, "Unexpected path/name");
            PageUtils.IsUnlinked(msg);
        }

        [Test]
        public void Move_SameTitle_NewPath_CorrectPathTitle() {
            // Log in as ADMIN
            Plug p = Utils.BuildPlugForAdmin();

            // Create a linked page (name == title)
            string id;
            string path;
            PageUtils.CreateRandomPage(p, out id, out path);

            // Move page to title=title, to=path-postfix
            string title = path.Substring(path.LastIndexOf("/") + 1);
            string postfix = "-" + DateTime.Now.Ticks.ToString();
            string newpath = path + postfix;
            PageUtils.MovePage(p.At("pages", id, "move").With("title", title).With("to", newpath));

            // Page should be unlinked, title should not have changed, path should have changed
            DreamMessage msg = PageUtils.GetPage(p, newpath);
            Assert.AreEqual(title, msg.ToDocument()["title"].AsText ?? String.Empty, "Unexpected title");
            Assert.AreEqual(newpath, msg.ToDocument()["path"].AsText ?? String.Empty, "Unexpected path/name");
            PageUtils.IsUnlinked(msg);
        }

        [Test]
        public void Move_SpecialPage_Name_CorrectPathTitle() {
        
            // Log in as Admin
            Plug p = Utils.BuildPlugForAdmin();

            // Create a special page
            string contents = "Move special page test";
            string path = "Special:" + Utils.GenerateUniqueName();
            DreamMessage msg = p.At("pages", "=" + XUri.DoubleEncode(path), "contents")
                .Post(DreamMessage.Ok(MimeType.TEXT_UTF8, contents), new Result<DreamMessage>()).Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Special page creation failed");

            // Move from Special:path to Special:newpath
            string newpath = "Special:" + Utils.GenerateUniqueName();
            PageUtils.MovePageAndWait(Utils.BuildPlugForAdmin().At("pages", "=" + XUri.DoubleEncode(path), "move").With("title", newpath), path);

            // Retrieve Special:path
            msg = PageUtils.GetPage(p, path);

            // Sanity check the path is Special:newpath
            string metapath = msg.ToDocument()["path"].AsText ?? String.Empty;
            Assert.AreEqual(newpath, metapath, "Unexpected path");

            // Delete page to preserve special namespace purity
            PageUtils.DeletePageByName(p, newpath, true);
        }

        [Test]
        public void Move_FixedPages_Name_BadRequest() {
            // Log in as ADMIN
            Plug p = Utils.BuildPlugForAdmin();

            // Create a user: page (no need to create user)
            string userpath = "User:" + Utils.GenerateUniqueName();
            DreamMessage msg = PageUtils.SavePage(p, userpath, "User page test");
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Failed to create user: page");

            // Make sure the page is fixed
            msg = PageUtils.GetPage(p, userpath);
            PageUtils.IsFixed(msg);
            
            // Peform moves on fixed pages: home, Special:, Template:, User:. Assert BAD REQUEST returned
            msg = p.At("pages", "home", "move").With("name", "foo").Post(new Result<DreamMessage>()).Wait();
            Assert.AreEqual(DreamStatus.BadRequest, msg.Status, "home URI rename succeeded?!");

           // BUG 8212
           // msg = p.At("pages", "=" + XUri.DoubleEncode(userpath), "move").With("name", "foozzzzzzzzzzz").Post(new Result<DreamMessage>()).Wait();
           // Assert.AreEqual(DreamStatus.BadRequest, msg.Status, "User: URI rename succeeded?!");
            msg = p.At("pages", "=Special:", "move").With("name", "foozzzzzzz").Post(new Result<DreamMessage>()).Wait();
            Assert.AreEqual(DreamStatus.BadRequest, msg.Status, "Special: URI rename succeeded?!");
            
            // BUG 8214
            //msg = p.At("pages", "=Template:", "move").With("name", "foo").Post(new Result<DreamMessage>()).Wait();
            //Assert.AreEqual(DreamStatus.BadRequest, msg.Status, "Template URI rename succeeded?!");
        }
    }
}

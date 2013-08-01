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
using MindTouch.Tasking;
using NUnit.Framework;

using MindTouch.Dream;
using MindTouch.Xml;

namespace MindTouch.Deki.Tests.PageTests
{
    [TestFixture]
    public class TitleTests
    {

        string[] name_space = { "Special:",
                                "User:",
                                "Template:",
                                "Help:",
                                "Project:", };

        /// <summary>
        ///     Create pages with encoded characters (API name) and verify correct path decoding (human-readable name)
        /// </summary>        
        /// <feature>
        /// <name>POST:pages/{pageid}/contents</name>
        /// <uri>http://developer.mindtouch.com/Deki/API_Reference/POST%3apages%2f%2f%7bpageid%7d%2f%2fcontents</uri>
        /// </feature>
        /// <expected>Path (API name) is correctly decoded to human-readable value</expected>

        [Test]
        public void PageCreate_PagePathEncoding_CorrectName()
        {
            // Log in as ADMIN
            Plug p = Utils.BuildPlugForAdmin();

            DreamMessage msg;
            string pathmeta;
            string name;

            // Create parent for all encoding test page creations
            string parentid;
            string parentpath;
            PageUtils.CreateRandomPage(p, out parentid, out parentpath);

            // Test cases <Input encoded name, Expected human-readable name>
            // null == BAD REQUEST
            // FEEL FREE TO ADD TEST CASES
            Dictionary<string, string> d = new Dictionary<string, string>();
            d.Add(String.Empty, null);
            d.Add("%2520", null);
            d.Add("test", "test");
            d.Add("foo%2520bar", "foo_bar");
            // TODO: figure out special/edge cases

            // BUG 8051:
            // d.Add("fooo%252520baar", "fooo%20baar"); 

            // Iterate through every testcase
            foreach (KeyValuePair<string, string> testcase in d)
            {
                // Create page with following path: parentpath/key
                msg = p.At("pages", "=" + XUri.DoubleEncode(parentpath + "/") + testcase.Key, "contents")
                        .PostAsync(DreamMessage.Ok(MimeType.TEXT_UTF8, "Page Encoding Test")).Wait();

                // if value is null, assert bad request returned 
                if (testcase.Value == null)
                    Assert.AreEqual(DreamStatus.BadRequest, msg.Status, String.Format("Page creation succeeded for bogus name?!"));
                else
                {
                    Assert.AreEqual(DreamStatus.Ok, msg.Status, String.Format("Page {0} create failed!", testcase.Key));

                    // Retrieve decoded segment and compare it to expected value
                    pathmeta = msg.ToDocument()["page/path"].AsText;
                    name = pathmeta.Substring(pathmeta.LastIndexOf("/") + 1);

                    Assert.AreEqual(testcase.Value, name, "Path decoded incorrectly");
                }
            }

            // Delete parent page and all children
            PageUtils.DeletePageByID(p, parentid, true);
        }

        /// <summary>
        ///     Create pages with encoded characters (API name) and verify correct name to title conversion following decoding
        /// </summary>        
        /// <feature>
        /// <name>POST:pages/{pageid}/contents</name>
        /// <uri>http://developer.mindtouch.com/Deki/API_Reference/POST%3apages%2f%2f%7bpageid%7d%2f%2fcontents</uri>
        /// </feature>
        /// <expected>Title is correctly translated following the name decoding</expected>

        [Test]
        public void PageCreate_PagePathEncoding_CorrectTitle()
        {
            // Log in as ADMIN
            Plug p = Utils.BuildPlugForAdmin();

            DreamMessage msg;
            string title;

            // Create parent for all encoding test page creations
            string parentid;
            string parentpath;
            PageUtils.CreateRandomPage(p, out parentid, out parentpath);

            // Test cases <Input encoded title, Expected human-readable title>
            // null == BAD REQUEST
            // FEEL FREE TO ADD TEST CASES
            Dictionary<string, string> d = new Dictionary<string, string>();
            d.Add(String.Empty, null);
            d.Add("%2520", null);
            d.Add("test", "test");
            d.Add("foo_bar", "foo bar");
            d.Add("foo%252f%252f%252f%252fbar", "foo//bar");
            d.Add("bar%2525252ffoo", "bar%2ffoo");
            // TODO: figure out special/edge cases

            // Iterate through every testcase
            foreach (KeyValuePair<string, string> testcase in d)
            {
                // Create page with following path: parentpath/key
                msg = p.At("pages", "=" + XUri.DoubleEncode(parentpath + "/") + testcase.Key, "contents")
                        .PostAsync(DreamMessage.Ok(MimeType.TEXT_UTF8, "Page Encoding Test")).Wait();

                // if value is null, assert bad request returned 
                if (testcase.Value == null)
                    Assert.AreEqual(DreamStatus.BadRequest, msg.Status, String.Format("Page creation succeeded for bogus name?!"));
                else
                {
                    Assert.AreEqual(DreamStatus.Ok, msg.Status, String.Format("Page {0} create failed!", testcase.Key));

                    // Retrieve decoded segment and compare it to expected value
                    title = msg.ToDocument()["page/title"].AsText;

                    Assert.AreEqual(testcase.Value, title, "Title translated incorrectly");
                }
            }

            // Delete parent page and all children
            PageUtils.DeletePageByID(p, parentid, true);
        }

        [Test]
        public void PageCreate_NameEqualsTitle_Linked()
        {
            // Log in as ADMIN
            Plug p = Utils.BuildPlugForAdmin();

            // Create a page where name == title
            string path = PageUtils.GenerateUniquePageName();
            DreamMessage msg = p.At("pages", "=" + XUri.DoubleEncode(path), "contents")
                .PostAsync(DreamMessage.Ok(MimeType.TEXT_UTF8, "Linked test")).Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Page creation failed!");

            // Retrieve the page
            msg = p.At("pages", "=" + XUri.DoubleEncode(path)).GetAsync().Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Page retrieval failed!");

            // Check that the page is linked (i.e. no path/@type attribute)
            string type = msg.ToDocument()["path/@type"].AsText;
            Assert.AreEqual(null, type, "Page is unlinked!");

            // Delete the page
            PageUtils.DeletePageByName(p, path, true);
        }

        [Test]
        public void PageCreate_NameDoesNotEqualTitle_Unlinked()
        {
            // Log in as ADMIN
            Plug p = Utils.BuildPlugForAdmin();

            // Create a page where name != title
            string path = PageUtils.GenerateUniquePageName();
            DreamMessage msg = p.At("pages", "=" + XUri.DoubleEncode(path), "contents")
                .With("title", "unique title")
                .PostAsync(DreamMessage.Ok(MimeType.TEXT_UTF8, "Linked test")).Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Page creation failed!");

            // Retrieve the page
            msg = p.At("pages", "=" + XUri.DoubleEncode(path)).GetAsync().Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Page retrieval failed!");

            // Check that the page is unlinked (i.e. path/@type attribute == custom)
            string type = msg.ToDocument()["path/@type"].AsText;
            Assert.AreEqual("custom", type, "Page is unlinked!");

            // Delete the page
            PageUtils.DeletePageByName(p, path, true);
        }

        [Test]
        public void GetRoot_PathType_Fixed() {
            // Build ADMIN plug
            Plug p = Utils.BuildPlugForAdmin();

            // Retrieve home page
            DreamMessage msg = p.At("pages", "home").GetAsync().Wait();
            Assert.AreEqual(msg.Status, DreamStatus.Ok, "Home page retrieval failed!");

            // Check to see if path has "fixed" attribute
            string linked_type = msg.ToDocument()["path/@type"].AsText ?? String.Empty;
            Assert.AreEqual("fixed", linked_type, "Type attribute is not 'fixed'!");

            // TODO: get all "fixed" cases, should only be root pages?
        }

        [Test]
        public void Title_Case01_WithNamespacePrefix_CorrectPathTitle() {
            
            // Case 01
            // Old path:  Namespace:aaa
            // Old title: Namespace:aaa
            //            move?title=Namespace:bbb
            // New path:  Namespace:bbb
            // New title: Namespace:bbb

            // Log in as ADMIN
            Plug p = Utils.BuildPlugForAdmin();

            // Run this test for each namespace
            for (int i = 0; i < name_space.Length; i++) {
                string path = name_space[i] + Utils.GenerateUniqueName();
                PageUtils.SavePage(p, path, name_space[i] + "test");

                // Rename page from Namespace:aaa -> Namespace:bbb using "title"
                string title = Utils.GenerateUniqueName();
                string newpath = name_space[i] + title;
                string newtitle = newpath;
                DreamMessage msg = PageUtils.MovePage(Utils.BuildPlugForAdmin().At("pages", "=" + XUri.DoubleEncode(path), "move").With("title", newtitle));
                Assert.IsTrue(msg.IsSuccessful, "move page failed");
                Assert.AreEqual(1, msg.ToDocument()["@count"].AsInt, "unexpected number of pages moved");
                if(msg.ToDocument()["page/path/@type"].AsText == "fixed") {

                    // fixed path page are not moved, just the title is changed
                    newpath = msg.ToDocument()["page/path"].AsText;
                }

                // Retrieve the page and verify correct path/title
                msg = PageUtils.GetPage(p, newpath);
                string metapath = msg.ToDocument()["path"].AsText ?? String.Empty;
                string metatitle = msg.ToDocument()["title"].AsText ?? String.Empty;

                // path AND title should be Namespace:bbb
                Assert.AreEqual(newpath, metapath, "Unexpected path");
                Assert.AreEqual(newtitle, metatitle, "Unexpected title");

                // Delete page as to not contaminate the namespace
                PageUtils.DeletePageByName(p, newpath, true);
            }
        }

        [Test]
        public void Title_Case02_WithNamespacePrefix_CorrectPathTitle() {

            // Case 02
            // Old path:  Namespace:aaa
            // Old title: Namespace:aaa
            //            move?title=bbb
            // New path:  Namespace:bbb
            // New title: bbb

            // Log in as ADMIN
            Plug p = Utils.BuildPlugForAdmin();

            // Run this test for each namespace
            for (int i = 0; i < name_space.Length; i++) {
                string path = name_space[i] + Utils.GenerateUniqueName();
                PageUtils.SavePage(p, path, name_space[i] + "test");

                // Rename page from Namespace:aaa -> bbb using "title"
                string title = Utils.GenerateUniqueName();
                string newtitle = title;
                string newpath = name_space[i] + title;
                DreamMessage msg = PageUtils.MovePage(Utils.BuildPlugForAdmin().At("pages", "=" + XUri.DoubleEncode(path), "move").With("title", newtitle));
                Assert.IsTrue(msg.IsSuccessful, "move page failed");
                Assert.AreEqual(1, msg.ToDocument()["@count"].AsInt, "unexpected number of pages moved");
                if(msg.ToDocument()["page/path/@type"].AsText == "fixed") {

                    // fixed path page are not moved, just the title is changed
                    newpath = msg.ToDocument()["page/path"].AsText;
                }

                // Retrieve the page and verify correct path/title
                msg = PageUtils.GetPage(p, newpath);
                string metapath = msg.ToDocument()["path"].AsText ?? String.Empty;
                string metatitle = msg.ToDocument()["title"].AsText ?? String.Empty;

                // path should be Namespace:bbb, title should be bbb, page should be linked
                Assert.AreEqual(newpath, metapath, "Unexpected path");
                Assert.AreEqual(newtitle, metatitle, "Unexpected title");
                PageUtils.IsLinked(msg);

                // Delete page as to not contaminate the namespace
                PageUtils.DeletePageByName(p, newpath, true);
            }
        }

        [Test]
        public void Title_Case03_WithNamespacePrefix_CorrectPathTitle() {

            // Case 03
            // Old path:  Namespace:aaa/bbb
            // Old title: Namespace:aaa/bbb
            //            move?title=Namespace:ccc
            // New path:  Namespace:aaa/Namespace:ccc
            // New title: Namespace:aaa/Namespace:ccc

            // Log in as ADMIN
            Plug p = Utils.BuildPlugForAdmin();

            // Run this test for each namespace
            for (int i = 0; i < name_space.Length; i++) {
                string presegment = name_space[i] + Utils.GenerateUniqueName() + "/";
                string path = presegment + Utils.GenerateUniqueName();
                PageUtils.SavePage(p, path, name_space + "test");

                // Rename page from Namespace:aaa/bbb -> Namespace:ccc using "title"
                string title = name_space[i] + Utils.GenerateUniqueName();
                string newpath = presegment + title;
                PageUtils.MovePage(Utils.BuildPlugForAdmin().At("pages", "=" + XUri.DoubleEncode(path), "move").With("title", title));

                // Retrieve the page and verify correct path/title
                DreamMessage msg = PageUtils.GetPage(p, newpath);
                string metapath = msg.ToDocument()["path"].AsText ?? String.Empty;
                string metatitle = msg.ToDocument()["title"].AsText ?? String.Empty;

                // path shouldbe Namespace:aaa/Namespace:ccc title should be Namespace:ccc
                Assert.AreEqual(newpath, metapath, "Unexpected path");
                Assert.AreEqual(title, metatitle, "Unexpected title");

                // Delete page as to not contaminate the namespace
                PageUtils.DeletePageByName(p, newpath, true);
            }
        }

        [Test]
        public void Title_Case04_WithNamespacePrefix_CorrectPathTitle() {

            // Case 04
            // Old path:  Namespace:aaa/bbb
            // Old title: Namespace:aaa/bbb
            //            move?title=ccc
            // New path:  Namespace:aaa/ccc
            // New title: Namespace:aaa/ccc

            // Log in as ADMIN
            Plug p = Utils.BuildPlugForAdmin();

            // Run this test for each namespace
            for (int i = 0; i < name_space.Length; i++) {
                string presegment = name_space[i] + Utils.GenerateUniqueName() + "/";
                string path = presegment + Utils.GenerateUniqueName();
                PageUtils.SavePage(p, path, name_space + "test");

                // Rename page from Namespace:aaa/bbb -> ccc using "title"
                string title = Utils.GenerateUniqueName();
                string newpath = presegment + title;
                PageUtils.MovePage(Utils.BuildPlugForAdmin().At("pages", "=" + XUri.DoubleEncode(path), "move").With("title", title));

                // Retrieve the page and verify correct path/title
                DreamMessage msg = PageUtils.GetPage(p, newpath);
                string metapath = msg.ToDocument()["path"].AsText ?? String.Empty;
                string metatitle = msg.ToDocument()["title"].AsText ?? String.Empty;

                // path shouldbe Namespace:aaa/Namespace:ccc title should be Namespace:ccc
                Assert.AreEqual(newpath, metapath, "Unexpected path");
                Assert.AreEqual(title, metatitle, "Unexpected title");

                // Delete page as to not contaminate the namespace
                PageUtils.DeletePageByName(p, newpath, true);
            }
        }
    }
}
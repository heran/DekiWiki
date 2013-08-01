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
using MindTouch.Dream.Test;
using MindTouch.Deki.Logic;

namespace MindTouch.Deki.Tests.PageTests
{
    [TestFixture]
    public class ContentTests
    {
        /// <summary>
        ///     Create a page
        /// </summary>        
        /// <feature>
        /// <name>POST:pages/{pageid}/contents</name>
        /// <uri>http://developer.mindtouch.com/Deki/API_Reference/POST%3apages%2f%2f%7bpageid%7d%2f%2fcontents</uri>
        /// </feature>
        /// <expected>200 OK HTTP Response</expected>

        [Test]
        public void CreatePage()
        {
            // Login as ADMIN
            Plug p = Utils.BuildPlugForAdmin();

            // Create a page
            string id = null;
            string path = null;
            DreamMessage msg = PageUtils.CreateRandomPage(p, out id, out path);
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Page creation failed");

            // Delete the page
            PageUtils.DeletePageByID(p, id, true);
        }

        /// <summary>
        ///     Retrieve page content
        /// </summary>        
        /// <feature>
        /// <name>GET:pages/{pageid}/contents</name>
        /// <uri>http://developer.mindtouch.com/en/ref/MindTouch_API/GET%3apages%2f%2f%7Bpageid%7D%2f%2fcontents</uri>
        /// </feature>
        /// <expected>Uploaded and retrieved contents are consistent</expected>

        [Test]
        public void GetContent()
        {
            // Login as ADMIN
            Plug p = Utils.BuildPlugForAdmin();

            // Generate small random content
            string content = Utils.GetSmallRandomText();

            // Create a page with random content
            string id = null;
            string path = null;
            DreamMessage msg = PageUtils.SavePage(p, string.Empty,
                PageUtils.GenerateUniquePageName(), content, out id, out path);

            // Retrieve page (by name) and compare contents
            msg = p.At("pages", "=" + XUri.DoubleEncode(path), "contents").With("mode", "view").Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Page retrieval failed (name)");
            XDoc html = XDocFactory.From("<html>" + System.Web.HttpUtility.HtmlDecode(msg.ToDocument()["body"].AsText) + "</html>", MimeType.HTML);
            Assert.AreEqual(html["/html"].AsText, content, "Retrieved and generated contents do not match! (name)");

            // Retrieve page (by ID) and compare contents
            msg = p.At("pages", id, "contents").With("mode", "view").Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Page retrieval failed (ID)");
            html = XDocFactory.From("<html>" + System.Web.HttpUtility.HtmlDecode(msg.ToDocument()["body"].AsText) + "</html>", MimeType.HTML);
            Assert.AreEqual(html["/html"].AsText, content, "Retrieved and generated contents do not match! (ID)");

            // Generate new random contents
            string newContent = Utils.GetSmallRandomText();

            // Replace contents with new content
            msg = PageUtils.SavePage(p, path, newContent);
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Uploading new content failed");

            // Retrieve page (by ID) and compare new contents
            msg = p.At("pages", id, "contents").Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Page retrieval failed (ID, new content)");
            html = XDocFactory.From("<html>" + System.Web.HttpUtility.HtmlDecode(msg.ToDocument()["body"].AsText) + "</html>", MimeType.HTML);
            Assert.AreEqual(html["/html"].AsText, newContent, "Retrieved and generated contents do not match! (ID, new content)");

            // Delete the page
            PageUtils.DeletePageByID(p, id, true);
        }

        /// <summary>
        ///     Retrieve site map
        /// </summary>        
        /// <feature>
        /// <name>GET:pages</name>
        /// <uri>http://developer.mindtouch.com/Deki/API_Reference/GET%3apages</uri>
        /// <parameter>format</parameter>
        /// </feature>
        /// <expected>200 OK HTTP Response</expected>

        [Test]
        public void GetPages()
        {
            // Login as ADMINz
            Plug p = Utils.BuildPlugForAdmin();

            // Retrieve pages in XML format
            DreamMessage msg = p.At("pages").With("format", "xml").Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Site map retrieval failed (xml)");

            // Retrieve pages in HTML format
            msg = p.At("pages").With("format", "html").Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Site map retrieval failed (html)");

            // Retrieve pages in google format
            msg = p.At("pages").With("format", "google").Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Site map retrieval failed (google)");
        }

        /// <summary>
        ///     Retrieve page by ID
        /// </summary>        
        /// <feature>
        /// <name>GET:pages/{pageid}</name>
        /// <uri>http://developer.mindtouch.com/Deki/API_Reference/GET%3apages%2f%2f%7bpageid%7d</uri>
        /// </feature>
        /// <expected>200 OK HTTP Response</expected>

        [Test]
        public void GetPageByID()
        {
            // Log in as ADMIN
            Plug p = Utils.BuildPlugForAdmin();

            // Create a page
            string id = null;
            string path = null;
            DreamMessage msg = PageUtils.CreateRandomPage(p, out id, out path);

            // Retrieve page by ID
            msg = p.At("pages", id).Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Page retrieval failed");
            Assert.IsTrue(msg.ToDocument()["@id"].AsText == id, "Document ID does not match page ID!");

            // Delete the page
            PageUtils.DeletePageByID(p, id, true);
        }

        [Test]
        public void GetPageByIdIncludingContents()
        {
            // Login as ADMIN
            Plug p = Utils.BuildPlugForAdmin();

            // Generate small random content
            string content = Utils.GetSmallRandomText();

            // Create a page with random content
            string id = null;
            string path = null;
            DreamMessage msg = PageUtils.SavePage(p, string.Empty,
                PageUtils.GenerateUniquePageName(), content, out id, out path);

            // Retrieve page (by ID) and compare contents
            msg = p.At("pages", id, "contents").With("mode", "view").With("include", "contents").Get();
            var msgDoc = msg.ToDocument();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Page retrieval by ID failed");
            Assert.AreEqual(content, System.Web.HttpUtility.HtmlDecode(msgDoc["body"].AsText), "The content is incorrect");

            // Delete the page
            PageUtils.DeletePageByID(p, id, true);
        }

        /// <summary>
        ///     Retrieve page by name
        /// </summary>        
        /// <feature>
        /// <name>GET:pages/{pageid}</name>
        /// <uri>http://developer.mindtouch.com/Deki/API_Reference/GET%3apages%2f%2f%7bpageid%7d</uri>
        /// </feature>
        /// <expected>200 OK HTTP Response</expected>

        [Test]
        public void GetPageByName()
        {
            // Log in as ADMIN
            Plug p = Utils.BuildPlugForAdmin();

            // Create a page
            string id = null;
            string path = null;
            DreamMessage msg = PageUtils.CreateRandomPage(p, out id, out path);

            // Retrieve page by name
            msg = p.At("pages", "=" + XUri.DoubleEncode(path)).Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Page retrieval failed");
            Assert.IsTrue(msg.ToDocument()["@id"].AsText == id, "Document ID does not match page ID!");

            // Delete the page
            PageUtils.DeletePageByID(p, id, true);
        }

        /// <summary>
        ///     Convert a page to PDF
        /// </summary>        
        /// <feature>
        /// <name>GET:pages/{pageid}/pdf</name>
        /// <uri>http://developer.mindtouch.com/en/ref/MindTouch_API/GET%3apages%2f%2f%7Bpageid%7D%2f%2fpdf</uri>
        /// </feature>
        /// <expected>200 OK HTTP Response</expected>

        [Test]
        public void GetPdf()
        {
            // Log in as ADMIN
            Plug p = Utils.BuildPlugForAdmin();

            // Create a page
            string id = null;
            string path = null;
            DreamMessage msg = PageUtils.CreateRandomPage(p, out id, out path);

            // Retrieve PDF conversion of page
            msg = p.At("pages", id, "pdf").Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "PDF retrieval failed");

            // Delete the page
            PageUtils.DeletePageByID(p, id, true);
        }

        /// <summary>
        ///     Create a page with big content
        /// </summary>        
        /// <feature>
        /// <name>POST:pages/{pageid}/contents</name>
        /// <uri>http://developer.mindtouch.com/Deki/API_Reference/POST%3apages%2f%2f%7bpageid%7d%2f%2fcontents</uri>
        /// </feature>
        /// <expected>200 OK HTTP Response</expected>

        [Test]
        public void PostBigContent()
        {
            // Log in as ADMIN
            Plug p = Utils.BuildPlugForAdmin();

            // Generate large amount of content and save to a page
            string content = Utils.GetBigRandomText();
            DreamMessage msg = PageUtils.SavePage(p, PageUtils.GenerateUniquePageName(), content);
            string id = msg.ToDocument()["page/@id"].AsText;
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Page retrieval failed");

            // Delete the page
            PageUtils.DeletePageByID(p, id, true);
        }

        /// <summary>
        ///     Test importtime query parameter
        /// </summary>        
        /// <feature>
        /// <name>POST:pages/{pageid}/contents</name>
        /// <uri>http://developer.mindtouch.com/Deki/API_Reference/POST%3apages%2f%2f%7bpageid%7d%2f%2fcontents</uri>
        /// <parameter>importtime</parameter>
        /// </feature>
        /// <expected>importtime property has correct name and value</expected>

        [Test]
        public void ImportTime_query_arg_forces_import_property_creation() 
        {
            // Log in as ADMIN
            Plug p = Utils.BuildPlugForAdmin();

            // Create a page
            string id = null;
            string path = null;
            PageUtils.CreateRandomPage(p, out id, out path);

            // Set importTime property to page
            var importTime = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            p.At("pages", id, "contents")
                .With("edittime", DateTime.MaxValue.ToString("yyyyMMddHHmmss"))
                .With("redirects", "0")
                .With("importtime", importTime)
                .Post(DreamMessage.Ok(MimeType.TEXT_UTF8, "foo"), new Result<DreamMessage>()).Wait();

            // Retrieve property and assert the dates match
            var msg = p.At("pages", id, "properties", XUri.EncodeSegment("mindtouch.import#info")).Get(new Result<DreamMessage>()).Wait();
            Assert.IsTrue(msg.IsSuccessful);
            Assert.AreEqual(importTime, (msg.ToDocument()["date.modified"].AsDate ?? DateTime.MinValue).ToString("yyyyMMddHHmmss"), "Unexpected date");
        }

        /// <summary>
        ///     Retrieve inbound and outbound links to and from a page
        /// </summary>        
        /// <feature>
        /// <name>GET:pages/{pageid}/links</name>
        /// <uri>http://developer.mindtouch.com/Deki/API_Reference/GET%3apages%2f%2f%7bpageid%7d%2f%2flinks</uri>
        /// <parameter>dir</parameter>
        /// </feature>
        /// <expected>200 OK HTTP Response</expected>

        [Test]
        public void GetLinks()
        {
            // Login as ADMIN
            Plug p = Utils.BuildPlugForAdmin();

            // Create a page
            string id = null;
            DreamMessage msg = PageUtils.CreateRandomPage(p, out id);

            // Retrieve a list of outbound links "from" page
            msg = p.At("pages", id, "links").With("dir", "from").Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Page links retrieval failed (from)");

            // Retrieve a list of inbound links "to" page
            msg = p.At("pages", id, "links").With("dir", "to").Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Page links retrieval failed (to)");

            // Delete the page
            PageUtils.DeletePageByID(p, id, true);
        }

        /// <summary>
        ///     Bug #4185 - Inbound/Outbound links are reversed
        /// </summary>        
        /// <feature>
        /// <name>GET:pages/{pageid}/links</name>
        /// <uri>http://developer.mindtouch.com/Deki/API_Reference/GET%3apages%2f%2f%7bpageid%7d%2f%2flinks</uri>
        /// <parameter>dir</parameter>
        /// </feature>
        /// <expected>Documents return correct inbound/outbound links</expected>

        [Test]
        public void Bug0004185_InboundOutboundLinksAreReversed()
        {
            // Log in as ADMIN
            Plug p = Utils.BuildPlugForAdmin();

            // Create page1
            string id = null;
            string path = null;
            DreamMessage msg = PageUtils.CreateRandomPage(p, out id, out path);

            // Create page2 with contents containg a link to page1
            string linkid = null;
            string linkpath = null;
            string content = string.Format(
                "<a href=\"mks://localhost/{0}\" class=\"internal\" title=\"{0}\">asdasdasdasda</a>", path);
            msg = PageUtils.SavePage(p, string.Empty, PageUtils.GenerateUniquePageName(), content, out linkid, out linkpath);

            // Retrieve outbound links of page2. Verify link points to page1.
            msg = p.At("pages", linkid, "links").With("dir", "from").Get();
            Assert.IsTrue(msg.ToDocument()["page/@id"].AsText == id, "Outbound link of page with link does not exist!");

            // Retrieve inbound links to page1. Verify link originates from page2.
            msg = p.At("pages", id, "links").With("dir", "to").Get();
            Assert.IsTrue(msg.ToDocument()["page/@id"].AsText == linkid, "Inbound link of page linked to is not present!");

            // Delete the pages
            PageUtils.DeletePageByID(p, id, true);
            PageUtils.DeletePageByID(p, linkid, true);
        }

        /// <summary>
        ///     Upload a link with a line break as page content and then retrieve page.
        /// </summary>        
        /// <feature>
        /// <name>GET:pages/{pageid}/contents</name>
        /// <uri>http://developer.mindtouch.com/en/ref/MindTouch_API/GET%3apages%2f%2f%7Bpageid%7D%2f%2fcontents</uri>
        /// </feature>
        /// <expected>The link to correctly render</expected>

        [Test]
        public void PageWithLineBreakInLink()
        {
            // Login as ADMIN
            Plug p = Utils.BuildPlugForAdmin();

            // Creates a page with contents containing a link with a line break
            string content = "<p>[[http://www.mindtouch.com/|\n<strong>Link Text</strong>\n]]&nbsp;</p>";
            string id = null;
            string path = null;
            DreamMessage msg = PageUtils.SavePage(p, string.Empty, PageUtils.GenerateUniquePageName(), content, out id, out path);

            // Retrieve page content
            msg = p.At("pages", "=" + XUri.DoubleEncode(path), "contents").With("mode", "view").Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Page contents retrieval failed");
            content = msg.ToDocument()["body"].AsText;

            // Assert link is rendered correctly
            XDoc html = XDocFactory.From("<html>" + System.Web.HttpUtility.HtmlDecode(content) + "</html>", MimeType.HTML);
            Assert.IsTrue(1 == html["//a"].ListLength &&
                          "Link Text" == html["//a/strong"].Contents &&
                          "http://www.mindtouch.com/" == html["//a/@href"].Contents, "Link did not render correctly");

            // Delete the page
            PageUtils.DeletePageByID(p, id, true);
        }

        /// <summary>
        ///     Upload a link with an email address as page content and then retrieve page.
        /// </summary>        
        /// <feature>
        /// <name>GET:pages/{pageid}/contents</name>
        /// <uri>http://developer.mindtouch.com/en/ref/MindTouch_API/GET%3apages%2f%2f%7Bpageid%7D%2f%2fcontents</uri>
        /// </feature>
        /// <expected>The link to be correctly render</expected>

        [Test]
        public void PageWithMailToLink()
        {
            // Log in as ADMIN
            Plug p = Utils.BuildPlugForAdmin();

            // Create a page with contents containing and email address
            string content = "<p>ddd@ddd.dd</p>";
            string id = null;
            string path = null;
            DreamMessage msg = PageUtils.SavePage(p, string.Empty, PageUtils.GenerateUniquePageName(), content, out id, out path);

            // Retrieve page contents
            msg = p.At("pages", "=" + XUri.DoubleEncode(path), "contents").With("mode", "view").Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Page content retrieval failed");

            // Assert email link is rendered correctly
            XDoc html = XDocFactory.From("<html>" +
                System.Web.HttpUtility.HtmlDecode(msg.ToDocument()["body"].AsText) + 
                "</html>", MimeType.HTML);
            Assert.IsTrue("mailto:ddd@ddd.dd" == html["//a/@href"].AsText, "Link did not render correctly");

            // Delete the page
            PageUtils.DeletePageByID(p, id, true);
        }

        /// <summary>
        ///     Retrieve revision contents
        /// </summary>        
        /// <feature>
        /// <name>GET:pages/{pageid}/contents</name>
        /// <uri>http://developer.mindtouch.com/en/ref/MindTouch_API/GET%3apages%2f%2f%7Bpageid%7D%2f%2fcontents</uri>
        /// <parameter>revision</parameter>
        /// </feature>
        /// <expected>Contents are consistent with revision number</expected>

        [Test]
        public void GetContentByRevision()
        {
            // Log in as ADMIN
            Plug p = Utils.BuildPlugForAdmin();

            // Genereate small, random content
            string content = Utils.GetSmallRandomText();

            // Create page with that content
            string id = null;
            string path = null;
            DreamMessage msg = PageUtils.SavePage(p, string.Empty,
                PageUtils.GenerateUniquePageName(), content, out id, out path);

            // Retrieve page contents
            msg = p.At("pages", id, "contents").Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Page content retrieval failed");
            content = msg.ToDocument()["body"].AsText;

            // Save some new, random content to page
            msg = PageUtils.SavePage(p, path, Utils.GetSmallRandomText());
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Failed to update page content");

            // Retrieve new page content and verify it does not match previous content
            msg = p.At("pages", id, "contents").Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Page content retrieval failed (rev 2)");
            Assert.IsFalse(msg.ToDocument()["body"].AsText == content, "Current page content matches previous revision!");

            // Retrieve old page content (previous revision) and verify it matches previous content
            msg = p.At("pages", id, "contents").With("revision", "1").Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Retrieval of page content of previous revision failed");
            Assert.IsTrue(msg.ToDocument()["body"].AsText == content, "Generated content and retrieved content of first revision do not match!");

            // Delete the page
            PageUtils.DeletePageByID(p, id, true);
        }

        /// <summary>
        ///     Post content with H2 HTML tag
        /// </summary>        
        /// <feature>
        /// <name>POST:pages/{pageid}/contents</name>
        /// <uri>http://developer.mindtouch.com/Deki/API_Reference/POST%3apages%2f%2f%7bpageid%7d%2f%2fcontents</uri>
        /// </feature>
        /// <expected>H2 contents render correctly</expected>

        [Test]
        public void PostContentWithH2()
        {
            // Log in as ADMIN
            Plug p = Utils.BuildPlugForAdmin();

            string titlecontent = "TITLE";
            string content = string.Format("<h2>{0}</h2><p>this is content</p>", titlecontent);

            // Create a page with above contents
            string id = null;
            string path = null;
            DreamMessage msg = PageUtils.SavePage(p, string.Empty,
                PageUtils.GenerateUniquePageName(), content, out id, out path);

            // Retrieve page contents
            msg = p.At("pages", id, "contents").Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Page content retrieval failed");

            // Retrieve contents HTML document, isolate <h2> tag (title), and assert it matches the above content
            XDoc html = XDocFactory.From("<html>" + System.Web.HttpUtility.HtmlDecode(msg.ToDocument().Contents) + "</html>", MimeType.HTML);
            Assert.IsTrue(html["//h2"].AsText == titlecontent, "Saved and retrieved H2 content do not match!");

            // Delete the page
            PageUtils.DeletePageByID(p, id, true);
        }

        [Test]
        public void Contents_TestMerge_CorrectContents() {
            // Log in as ADMIN
            Plug p = Utils.BuildPlugForAdmin();

            // Create some content to test merge
            string contentA = "<p>This is some wonderful content</p><p>I love writing tests</p>";
            string contentB = "<p>bing bong ping pong</p><p>I love writing tests</p>";
            string contentC = "<p>This is some wonderful content</p><p>Expect a cool merge right here</p>";
            string contentRes = "<p>bing bong ping pong</p><p>Expect a cool merge right here</p>";

            // Upload contentA to page and save edittime
            string path = PageUtils.GenerateUniquePageName();
            DreamMessage msg = p.At("pages", "=" + XUri.DoubleEncode(path), "contents").Post(DreamMessage.Ok(MimeType.TEXT_UTF8, contentA), new Result<DreamMessage>()).Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Setting page contents to contentA failed");
            msg = PageUtils.GetPage(p, path);
            string edittime = msg.ToDocument()["date.edited"].AsText;
            Assert.IsNotNull(edittime, "No date.edited in page document!");

            // Have it wait a second
            Wait.For(() => { return false; }, TimeSpan.FromSeconds(1));

            // Upload content B as Admin
            msg = p.At("pages", "=" + XUri.DoubleEncode(path), "contents").With("edittime", edittime).Post(DreamMessage.Ok(MimeType.TEXT_UTF8, contentB), new Result<DreamMessage>()).Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Setting page contents to contentB failed");

            // Create a user and upload content C as user
            string userid;
            string username;
            UserUtils.CreateRandomContributor(p, out userid, out username);

            // Upload content C as user
            p = Utils.BuildPlugForUser(username);
            msg = p.At("pages", "=" + XUri.DoubleEncode(path), "contents").With("edittime", edittime).Post(DreamMessage.Ok(MimeType.TEXT_UTF8, contentC), new Result<DreamMessage>()).Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Setting page contents to contentB failed");

            // Retrieve page contents and verify merge
            msg = p.At("pages", "=" + XUri.DoubleEncode(path), "contents").Get(new Result<DreamMessage>()).Wait();
            Assert.AreEqual(contentRes, msg.ToDocument()["body"].AsText ?? String.Empty, "Unexpected contents after merge");
        }

        [Ignore]
        [Test]
        public void Contents_BadTitle_Conflict() {
            // Log in as ADMIN
            Plug p = Utils.BuildPlugForAdmin();

            // Attempt to save a page with a bad title
            string path = PageUtils.GenerateUniquePageName();
            string title = " ";
            DreamMessage msg = p.At("pages", "=" + XUri.DoubleEncode(path), "contents").With("title", title).
                                    Post(DreamMessage.Ok(MimeType.TEXT_UTF8, "test content"), new Result<DreamMessage>()).Wait();
            Assert.AreEqual(DreamStatus.Conflict, msg.Status, "Saving page with invalid title succeeded?!");
        }

        [Test]
        public void Contents_PageWithTalkPage_ManyParameterChanges_CorrectContent() {
            // Log in as ADMIN
            Plug p = Utils.BuildPlugForAdmin();

            // Create a page with some content and save edittime
            string path = PageUtils.GenerateUniquePageName();
            string content = "test content";
            string title = "test title";
            string comment = "new page!";
            MimeType mime = MimeType.TEXT_UTF8;
            DreamMessage msg = p.At("pages", "=" + XUri.DoubleEncode(path), "contents")
                                    .With("title", title)
                                    .With("comment", comment)
                                    .Post(DreamMessage.Ok(mime, content), new Result<DreamMessage>()).Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Creating page failed");
            msg = PageUtils.GetPage(p, path);
            string edittime = msg.ToDocument()["date.edited"].AsText;
            Assert.IsNotNull(edittime, "No date.edited in page document!");

            // Create a talk page
            string talkid;
            string talkpath;
            PageUtils.CreateTalkPage(p, path, out talkid, out talkpath);

            // Now make some changes
            string lang = "de";
            content = "new test content!";
            comment = "edit page!";
            title = "new title!";
            mime = MimeType.TEXT;
            msg = p.At("pages", "=" + XUri.DoubleEncode(path), "contents")
                                   .With("title", title)
                                   .With("comment", comment)
                                   .With("language", lang)
                                   .With("edittime", edittime)
                                   .Post(DreamMessage.Ok(mime, content), new Result<DreamMessage>()).Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Editing page contents failed");

            // Verify changes
            msg = PageUtils.GetPage(p, path);
            Assert.AreEqual(title, msg.ToDocument()["title"].AsText ?? String.Empty, "Unexpected title");
            Assert.AreEqual(content, msg.ToDocument()["summary"].AsText ?? String.Empty, "Unexpected content");
            Assert.AreEqual(lang, msg.ToDocument()["properties/language"].AsText ?? String.Empty, "Unexpected language");
            Assert.AreEqual(comment + "; "
                          + "2 words added; "
                          + "page display name changed to '" + title + "'; "
                          + "page language changed to Deutsch", msg.ToDocument()["description"].AsText ?? String.Empty, "Unexpected description");
        }

        [Test]
        public void Bug_8488_Contents_With_Tags_Inherits_Restrictions() {
            // Log in as ADMIN
            Plug p = Utils.BuildPlugForAdmin();

            // Create a page
            string page_id;
            string page_path;
            var msg = PageUtils.CreateRandomPage(p, out page_id, out page_path);

            // Set page to private
            XDoc secXML = new XDoc("security").Start("permissions.page").Elem("restriction", "Private").End();
            msg = p.At("pages", page_id, "security").Put(secXML, new Result<DreamMessage>()).Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Setting restrictions of parent page to 'Private' failed!");

            // Create a subpage to page with tag contents
            string content = "<p>test page bug 8488</p>";
            string tags = "<p class=\"template:tag-insert\"><em>Tags recommended by the template: </em><a href=\"#\">test:foo</a></p>";
            string subpage_path = page_path + "/" + Utils.GenerateUniqueName();
            msg = p.At("pages", "=" + XUri.DoubleEncode(subpage_path), "contents")
                .Post(DreamMessage.Ok(MimeType.TEXT_UTF8, content + tags), new Result<DreamMessage>()).Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Subpage creation failed!");

            // Verify tag test:foo created
            msg = p.At("pages", "=" + XUri.DoubleEncode(subpage_path), "tags").Get(new Result<DreamMessage>()).Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Tag retrieval failed!");
            Assert.AreEqual("test:foo", msg.ToDocument()["tag/@value"].AsText, "Unexpected tag value");

            // Verify tag data is not part of contents
            msg = p.At("pages", "=" + XUri.DoubleEncode(subpage_path), "contents").Get(new Result<DreamMessage>()).Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Content retrieval failed!");
            Assert.AreEqual(content, msg.ToDocument()["body"].AsText, "Unexpected contents");

            // Retrieve subpage security and verify it is private
            msg = p.At("pages", "=" + XUri.DoubleEncode(subpage_path), "security").Get(new Result<DreamMessage>()).Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Security retrieval failed!");
            Assert.AreEqual("Private", msg.ToDocument()["permissions.page/restriction"].AsText ?? String.Empty, "Subpage did not inherit 'Private' restriction!");
        }

        [Test]
        public void UpdateWithEdittimeNow() {
            // Log in as ADMIN
            Plug p = Utils.BuildPlugForAdmin();

            // Create a page
            string page_id;
            string page_path;
            var msg = PageUtils.CreateRandomPage(p, out page_id, out page_path);

            // update the page
            string content = "<p>just a test</p>";
            msg = p.At("pages", page_id, "contents")
                   .With("edittime", "now")
                   .Post(DreamMessage.Ok(MimeType.TEXT_UTF8, content), new Result<DreamMessage>()).Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "page creation failed!");
        }

        [Test]
        public void Bug_MT8962_PostPageContentsWithFile() {

            Plug adminPlug = Utils.BuildPlugForAdmin();

            // Create random contributor
            string username = null;
            string userid = null;
            DreamMessage msg = UserUtils.CreateRandomUser(adminPlug, "Contributor", "password", out userid, out username);

            // Login as user
            Plug userPlug = Utils.BuildPlugForUser(username);

            // Create a page
            string page_id;
            string page_path;
            msg = PageUtils.CreateRandomPage(userPlug, out page_id, out page_path);

            // Create a file
            string fileName = FileUtils.CreateRamdomFile(null);
            msg = DreamMessage.FromFile(fileName);
            fileName = "foo.jpg";

            // Upload file to page
            msg = userPlug.At("pages", page_id, "files", "=" + fileName).Put(msg);
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "PUT request failed");

            // update the page
            XDoc pageContents = new XDoc("content");
            pageContents.Attr("type", "application/x.deki-text");
            pageContents.Attr("unsafe", "false");
            pageContents.Start("body");
            pageContents.Start("img");
            pageContents.Attr("class", "internal default");
            pageContents.Attr("src.path", "//");
            pageContents.Attr("src.filename", fileName);
            pageContents.End();  //img
            pageContents.End();//body

            msg = userPlug.At("pages", page_id, "contents")
                   .With("edittime", "now")
                   .With("redirects", 0)
                   .With("reltopath", page_path)
                   .Post(pageContents);
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "page creation failed!");

            msg = userPlug.At("pages", page_id, "contents")
                .With("mode", "view")
                .Get();

            string contents = msg.AsDocument()["/content/body"].Contents;
            XDoc imgDoc = XDocFactory.From(contents, MimeType.XML);
            XUri imgSrcUri = XUri.TryParse(imgDoc["@src"].AsText);
            Assert.IsNotNull(imgSrcUri, "img src uri is invalid!");
        }

        [Test]
        public void AdminExcludeInboundLinksTest() {

            // Log in as ADMIN
            Plug p = Utils.BuildPlugForAdmin();

            // Create page1
            string page1id;
            string page1path;
            DreamMessage msg = PageUtils.CreateRandomPage(p, out page1id, out page1path);

            // Create page2 with contents containing a link to page1
            string page2id;
            string page2path;
            string content = string.Format("<a href=\"mks://localhost/{0}\" class=\"internal\" title=\"{0}\">a link to page1</a>", page1path);
            msg = PageUtils.SavePage(p, string.Empty, PageUtils.GenerateUniquePageName(), content, out page2id, out page2path);

            // Retrieve outbound links of page2. Verify link points to page1.
            msg = p.At("pages", page2id, "links").With("dir", "from").Get();
            Assert.IsTrue(msg.ToDocument()["page/@id"].AsText == page1id, "Outbound link of page with link does not exist!");

            // Retrieve inbound links to page1. Verify link originates from page2.
            msg = p.At("pages", page1id, "links").With("dir", "to").Get();
            Assert.IsTrue(msg.ToDocument()["page/@id"].AsText == page2id, "Inbound link of page linked to is not present!");

            // Do not exclude anything
            msg = PageUtils.GetPage(p, page1path, new PageContentFilterSettings());
            var doc = msg.ToDocument();
            Assert.IsTrue(!doc["inbound"].IsEmpty, "[1] Inbound links information was excluded even though it should not have been excluded");
            Assert.IsTrue(!doc["outbound"].IsEmpty, "[2] Outbound links information was excluded even though it should not have been excluded");

            // Exclude both inbound and outbound
            msg = PageUtils.GetPage(p, page1path, new PageContentFilterSettings { ExcludeInboundLinks = true, ExcludeOutboundLinks = true });
            doc = msg.ToDocument();
            Assert.IsTrue(doc["inbound"].IsEmpty, "[3] Inbound links information wasn't excluded");
            Assert.IsTrue(doc["outbound"].IsEmpty, "[4] Outbound links information wasn't excluded");

            // exclude outbound only
            msg = PageUtils.GetPage(p, page1path, new PageContentFilterSettings { ExcludeInboundLinks = false, ExcludeOutboundLinks = true });
            doc = msg.ToDocument();
            Assert.IsTrue(!doc["inbound"].IsEmpty, "[5] Inbound links information was excluded even though it should not be excluded"); 
            Assert.IsTrue(doc["outbound"].IsEmpty, "[6] Outbound links information wasn't excluded");
            
            // Only exclude inbound
            msg = PageUtils.GetPage(p, page1path, new PageContentFilterSettings { ExcludeInboundLinks = true, ExcludeOutboundLinks = false });
            doc = msg.ToDocument();
            Assert.IsTrue(doc["inbound"].IsEmpty, "[7] Inbound links information wasn't excluded");
            Assert.IsTrue(!doc["outbound"].IsEmpty, "[8] Outbound links information was exclude even though it should not be excluded");

            // Delete the pages
            PageUtils.DeletePageByID(p, page1id, true);
            PageUtils.DeletePageByID(p, page2id, true);
        }
    }
}

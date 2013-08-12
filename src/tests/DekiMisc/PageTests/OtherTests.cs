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
using System.Text;

using NUnit.Framework;

using MindTouch.Dream;
using MindTouch.Xml;

namespace MindTouch.Deki.Tests.PageTests
{
    [TestFixture]
    public class OtherTests
    {
        [Test]
        public void GetSubpages()
        {
            // GET:pages/{pageid}/subpages 
            // http://developer.mindtouch.com/Deki/API_Reference/GET%3apages%2f%2f%7bpageid%7d%2f%2fsubpages

            Plug p = Utils.BuildPlugForAdmin();

            string id = null;
            string path = null;
            DreamMessage msg = PageUtils.CreateRandomPage(p, out id, out path);

            msg = p.At("pages", id, "subpages").Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status);
            Assert.IsTrue(msg.ToDocument()["page.subpage"].IsEmpty);

            msg = PageUtils.CreateRandomPage(p, path + "/" + Utils.GenerateUniqueName());
            Assert.AreEqual(DreamStatus.Ok, msg.Status);
            string subid = msg.ToDocument()["page/@id"].AsText;
            Assert.IsTrue(!string.IsNullOrEmpty(subid));

            msg = p.At("pages", "=" + XUri.DoubleEncode(path), "subpages").Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status);
            Assert.IsFalse(msg.ToDocument()[string.Format("page.subpage[@id=\"{0}\"]", subid)].IsEmpty);

            PageUtils.DeletePageByID(p, id, true);
        }

        [Test]
        public void GetPageAliases()
        {
            // GET:pages/{pageid}/aliases
            // http://developer.mindtouch.com/Deki/API_Reference/GET%3apages%2f%2f%7bpageid%7d%2f%2faliases

            Plug p = Utils.BuildPlugForAdmin();

            string id = null;
            string path = null;
            DreamMessage msg = PageUtils.CreateRandomPage(p, out id, out path);

            msg = p.At("pages", id, "aliases").Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status);

            PageUtils.DeletePageByID(p, id, true);
        }

        [Test]
        public void GetPageFiles()
        {
            // GET:pages/{pageid}/files
            // http://developer.mindtouch.com/Deki/API_Reference/GET%3apages%2f%2f%7bpageid%7d%2f%2ffiles

            Plug p = Utils.BuildPlugForAdmin();

            string id = null;
            string path = null;
            DreamMessage msg = PageUtils.CreateRandomPage(p, out id, out path);

            string fileid = null;
            string filename = null;
            FileUtils.UploadRandomFile(p, id, out fileid, out filename);

            msg = p.At("pages", id, "files").Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status);
            Assert.IsFalse(msg.ToDocument()[string.Format("file[@id=\"{0}\"]", fileid)].IsEmpty);

            PageUtils.DeletePageByID(p, id, true);
        }

        [Test]
        public void GetPageFileContent()
        {
            // GET:pages/{pageid}/files/{filename}
            // http://developer.mindtouch.com/Deki/API_Reference/GET%3apages%2f%2f%7bpageid%7d%2f%2ffiles%2f%2f%7bfilename%7d

            Plug p = Utils.BuildPlugForAdmin();

            string id = null;
            string path = null;
            DreamMessage msg = PageUtils.CreateRandomPage(p, out id, out path);

            byte[] content = FileUtils.GenerateRandomContent();
            string fileid = null;
            string filename = null;
            msg = FileUtils.UploadRandomFile(p, id, content, string.Empty, out fileid, out filename);

            msg = p.At("pages", id, "files", "=" + filename).Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status);
            Assert.IsTrue(Utils.ByteArraysAreEqual(content, msg.AsBytes()));

            PageUtils.DeletePageByID(p, id, true);
        }

        [Test]
        public void GetPageFilesAndSubpages()
        {
            // GET:pages/{pageid}/files,subpages
            // http://developer.mindtouch.com/Deki/API_Reference/GET%3apages%2f%2f%7bpageid%7d%2f%2ffiles%2csubpages

            Plug p = Utils.BuildPlugForAdmin();

            string id = null;
            string path = null;
            DreamMessage msg = PageUtils.CreateRandomPage(p, out id, out path);

            string subid = null;
            string subpath = null;
            msg = PageUtils.CreateRandomPage(p, path + "/" + Utils.GenerateUniqueName(), out subid, out subpath);

            string fileid = null;
            string filename = null;
            FileUtils.UploadRandomFile(p, id, out fileid, out filename);

            msg = p.At("pages", id, "files,subpages").Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status);
            Assert.IsFalse(msg.ToDocument()[string.Format("subpages/page.subpage[@id=\"{0}\"]", subid)].IsEmpty);
            Assert.IsFalse(msg.ToDocument()[string.Format("files/file[@id=\"{0}\"]", fileid)].IsEmpty);

            PageUtils.DeletePageByID(p, id, true);
        }

        [Test]
        public void GetPageTree()
        {
            // GET:pages/{pageid}/tree
            // http://developer.mindtouch.com/Deki/API_Reference/GET%3apages%2f%2f%7bpageid%7d%2f%2ftree

            Plug p = Utils.BuildPlugForAdmin();

            string id = null;
            string path = null;
            DreamMessage msg = PageUtils.CreateRandomPage(p, out id, out path);

            string subid = null;
            string subpath = null;
            msg = PageUtils.CreateRandomPage(p, path + "/" + Utils.GenerateUniqueName(), out subid, out subpath);

            msg = p.At("pages", id, "tree").Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status);
            Assert.IsTrue(msg.ToDocument()["page/@id"].AsText == id);
            Assert.IsTrue(msg.ToDocument()["page/subpages/page/@id"].AsText == subid);

            PageUtils.DeletePageByID(p, id, true);
        }

        [Test]
        public void GetPagesPopular()
        {
            // GET:pages/popular
            // http://developer.mindtouch.com/Deki/API_Reference/GET%3apages%2f%2fpopular

            Plug p = Utils.BuildPlugForAdmin();

            DreamMessage msg = p.At("pages", "popular").Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status);
        }

        [Test]
        public void PageIndex()
        {
            // POST:pages/{pageid}/index
            // http://developer.mindtouch.com/Deki/API_Reference/POST%3apages%2f%2f%7bpageid%7d%2f%2findex

            Plug p = Utils.BuildPlugForAdmin();

            string id = null;
            string path = null;
            DreamMessage msg = PageUtils.CreateRandomPage(p, out id, out path);

            msg = p.At("pages", "=" + XUri.DoubleEncode(path), "index").Post();
            Assert.AreEqual(DreamStatus.Ok, msg.Status);

            msg = p.At("pages", id, "index").Post();
            Assert.AreEqual(DreamStatus.Ok, msg.Status);

            msg = p.At("pages", "home", "index").Post();
            Assert.AreEqual(DreamStatus.Ok, msg.Status);

            PageUtils.DeletePageByID(p, id, true);
        }


        
        [Test]
        public void GetPageInfo()
        {
            // GET:pages/{pageid}/info
            // http://developer.mindtouch.com/Deki/API_Reference/GET%3apages%2f%2f%7bpageid%7d%2f%2finfo

            Plug p = Utils.BuildPlugForAdmin();

            string id = null;
            string path = null;
            DreamMessage msg = PageUtils.CreateRandomPage(p, out id, out path);

            msg = p.At("pages", "home", "info").Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status);
            Assert.IsTrue(string.IsNullOrEmpty(msg.ToDocument()["path"].AsText));

            msg = p.At("pages", id, "info").Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status);
            Assert.IsTrue(msg.ToDocument()["path"].AsText == path);

            PageUtils.DeletePageByID(p, id, true);
        }

        [Test]
        public void GetFeeds()
        {
            // GET:pages/{pageid}/feed
            // http://developer.mindtouch.com/Deki/API_Reference/GET%3apages%2f%2f%7bpageid%7d%2f%2ffeed

            Plug p = Utils.BuildPlugForAdmin();

            string id = null;
            string path = null;
            DreamMessage msg = PageUtils.CreateRandomPage(p, out id, out path);

            msg = p.At("pages", id, "feed").Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status);

            PageUtils.DeletePageByID(p, id, true);
        }

        [Test]
        public void PageWithManySubpageLinks()
        {
            //Assumptions: 
            //Actions:
            // Creates a page with many subpage links and then retrieves it
            //Expected result: 
            // The page is successfully created and retrieved

            int countOfLinks = 100;
            StringBuilder pageContent = new StringBuilder("Page with many page links: ");
            Plug p = Utils.BuildPlugForAdmin();

            string id = null;
            string path = null;
            DreamMessage msg = PageUtils.CreateRandomPage(p, out id, out path);
            for (int i = 0; i < countOfLinks; i++)
            {
                PageUtils.SavePage(p, path + "/Page" + i, "Page content for " + i);
                pageContent.Append(String.Format("[[./Page{0}|Page{0}]]", i));
            }
            PageUtils.SavePage(p, path, pageContent.ToString());
            msg = p.At("pages", id, "contents").With("mode", "view").Get();
            XDoc html = XDocFactory.From("<html>" + System.Web.HttpUtility.HtmlDecode(msg.ToDocument().Contents) + "</html>", MimeType.HTML);
            Assert.IsTrue(html["//a[@rel='internal']"].ListLength == countOfLinks);

            msg = p.At("pages", id, "contents").With("mode", "edit").Get();
            html = XDocFactory.From("<html>" + System.Web.HttpUtility.HtmlDecode(msg.ToDocument().Contents) + "</html>", MimeType.HTML);
            Assert.IsTrue(html["//a"].ListLength == countOfLinks);

            msg = p.At("pages", id, "contents").With("mode", "raw").Get();
            html = XDocFactory.From("<html>" + System.Web.HttpUtility.HtmlDecode(msg.ToDocument().Contents) + "</html>", MimeType.HTML);
            Assert.IsTrue(html["//a"].ListLength == countOfLinks);

            msg = p.At("pages", id, "links").With("dir", "from").Get();
            Assert.IsTrue(msg.ToDocument()["@count"].AsInt == countOfLinks);

            List<XDoc> linksList = msg.ToDocument()["//outbound/page"].ToList();
            Assert.IsTrue(linksList.Count == countOfLinks);
            for (int i = 0; i < countOfLinks; i++)
            {
                Assert.IsTrue(linksList[i]["title"].AsText == "Page" + i);
                Assert.IsTrue(linksList[i]["path"].AsText == path + "/Page" + i);
            }

            PageUtils.DeletePageByID(p, id, true);
        }

        [Ignore]
        [Test]
        public void PageWithManyBrokenLinks()
        {
            //Assumptions: 
            //Actions:
            // Creates a page with many broken links and then retrieves it
            //Expected result: 
            // The page is successfully created and retrieved

            int countOfLinks = 100;
            StringBuilder pageContent = new StringBuilder("Page with many broken page links: ");
            Plug p = Utils.BuildPlugForAdmin();

            string id = null;
            string path = null;
            DreamMessage msg = PageUtils.CreateRandomPage(p, out id, out path);
            for (int i = 0; i < countOfLinks; i++)
                pageContent.Append(String.Format("[[./Page{0}|Page{0}]]", i));
            PageUtils.SavePage(p, path, pageContent.ToString());
            msg = p.At("pages", id, "contents").With("mode", "view").Get();
            XDoc html = XDocFactory.From("<html>" + System.Web.HttpUtility.HtmlDecode(msg.ToDocument().Contents) + "</html>", MimeType.HTML);
            Assert.IsTrue(html["//a[@class=\"new\"]"].ListLength == countOfLinks);

            msg = p.At("pages", id, "contents").With("mode", "edit").Get();
            html = XDocFactory.From("<html>" + System.Web.HttpUtility.HtmlDecode(msg.ToDocument().Contents) + "</html>", MimeType.HTML);
            Assert.IsTrue(html["//a"].ListLength == countOfLinks);

            msg = p.At("pages", id, "contents").With("mode", "raw").Get();
            html = XDocFactory.From("<html>" + System.Web.HttpUtility.HtmlDecode(msg.ToDocument().Contents) + "</html>", MimeType.HTML);
            Assert.IsTrue(html["//a"].ListLength == countOfLinks);

            msg = p.At("pages", id, "links").With("dir", "from").Get();
            Assert.IsTrue(msg.ToDocument()["@count"].AsInt == 0);

            PageUtils.DeletePageByID(p, id, true);
        }

        [Test]
        public void PageWithManyExternalLinks()
        {
            //Assumptions: 
            //Actions:
            // Creates a page with many external links and then retrieves it
            //Expected result: 
            // The page is successfully created and retrieved

            int countOfLinks = 100;
            StringBuilder pageContent = new StringBuilder("Page with many external page links: ");
            Plug p = Utils.BuildPlugForAdmin();

            string id = null;
            string path = null;
            DreamMessage msg = PageUtils.CreateRandomPage(p, out id, out path);
            for (int i = 0; i < countOfLinks; i++)
            {
                pageContent.Append(String.Format("[[http://www.mindtouch.com/{0}|http://www.mindtouch.com/{0}]]", i));
            }
            PageUtils.SavePage(p, path, pageContent.ToString());
            msg = p.At("pages", id, "contents").With("mode", "view").Get();
            XDoc html = XDocFactory.From("<html>" + System.Web.HttpUtility.HtmlDecode(msg.ToDocument().Contents) + "</html>", MimeType.HTML);
            Assert.IsTrue(html["//a[@rel='external nofollow']"].ListLength == countOfLinks);

            msg = p.At("pages", id, "contents").With("mode", "edit").Get();
            html = XDocFactory.From("<html>" + System.Web.HttpUtility.HtmlDecode(msg.ToDocument().Contents) + "</html>", MimeType.HTML);
            Assert.IsTrue(html["//a"].ListLength == countOfLinks);

            msg = p.At("pages", id, "contents").With("mode", "raw").Get();
            html = XDocFactory.From("<html>" + System.Web.HttpUtility.HtmlDecode(msg.ToDocument().Contents) + "</html>", MimeType.HTML);
            Assert.IsTrue(html["//a"].ListLength == countOfLinks);

            msg = p.At("pages", id, "links").With("dir", "from").Get();
            Assert.IsTrue(msg.ToDocument()["@count"].AsInt == 0);

            PageUtils.DeletePageByID(p, id, true);
        }
    }
}

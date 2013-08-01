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

namespace MindTouch.Deki.Tests.ArchiveTests
{
    [TestFixture]
    public class PagesTests
    {
        [Test]
        public void GetPages()
        {
            // GET:archive/pages
            // http://developer.mindtouch.com/Deki/API_Reference/GET%3aarchive%2f%2fpages

            // 1. Create a page
            // 2. Delete the page
            // 3. Retrieve selected page from list of archived pages
            // (4a) Assert archived page path is consistent
            // (4b) Assert archived page ID matches deleted page ID

            Plug p = Utils.BuildPlugForAdmin();

            string id = null;
            string path = null;
            DreamMessage msg = PageUtils.CreateRandomPage(p, out id, out path);

            PageUtils.DeletePageByID(p, id, false);

            msg = p.At("archive", "pages").With("title", path).Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Page retrieval failed");
            Assert.AreEqual(path, msg.ToDocument()["//path"].AsText ?? String.Empty, "Page path and archived page path do not match!");
            Assert.AreEqual(id, msg.ToDocument()["page.archive/@id"].AsText ?? String.Empty, "Page ID and archived page ID do not match!");
        }

        [Test]
        public void GetPageByID()
        {
            // GET:archive/pages/{pageid}
            // http://developer.mindtouch.com/Deki/API_Reference/GET%3aarchive%2f%2fpages%2f%2f%7Bpageid%7D

            // 1. Create a page
            // 2. Delete the page
            // (3) Assert archived page path is consistent

            Plug p = Utils.BuildPlugForAdmin();

            string id = null;
            string path = null;
            DreamMessage msg = PageUtils.CreateRandomPage(p, out id, out path);

            PageUtils.DeletePageByID(p, id, false);

            msg = p.At("archive", "pages", id).Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Page retrieval failed");
            Assert.IsTrue((msg.ToDocument()["path"].AsText ?? String.Empty) == path, "Page path and archived page path do not match!");
        }

        [Test]
        public void GetPageInfo()
        {
            // GET:archive/pages/{pageid}/info
            // ...

            // 1. Create a page
            // 2. Delete the page
            // (3a) Assert pageID = page.archiveID
            // (3b) Assert page path is consistent

            Plug p = Utils.BuildPlugForAdmin();

            string id = null;
            string path = null;
            DreamMessage msg = PageUtils.CreateRandomPage(p, out id, out path);

            PageUtils.DeletePageByID(p, id, false);

            msg = p.At("archive", "pages", id, "info").Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Page retrieval failed");
            Assert.IsTrue((msg.ToDocument()["@id"].AsText ?? String.Empty) == id, "Page ID and archived page ID do not match!");
            Assert.AreEqual(path, msg.ToDocument()["path"].AsText ?? String.Empty, "Page path and archived page path do not match!");
        }

        [Test]
        public void GetSubpages()
        {
            // GET:archive/pages/{pageid}/subpages
            // ...

            // 1. Create a page
            // 2. Create subpage to page
            // 3. Delete page (recursively)
            // (4a) Assert subpage archive ID matches deleted subpage ID
            // (4b) Assert subpage path is consistent

            Plug p = Utils.BuildPlugForAdmin();

            string id = null;
            string path = null;
            DreamMessage msg = PageUtils.CreateRandomPage(p, out id, out path);

            string subid = null;
            string subpath = null;
            msg = PageUtils.CreateRandomPage(p, path + "/" + Utils.GenerateUniqueName(), out subid, out subpath);

            PageUtils.DeletePageByID(p, id, true);

            msg = p.At("archive", "pages", id, "subpages").Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Page retrieval failed");
            Assert.IsTrue((msg.ToDocument()["page.archive/@id"].AsText ?? String.Empty) == subid, "Page ID and archived page ID do not match!");
            Assert.AreEqual(subpath, msg.ToDocument()["//path"].AsText ?? String.Empty, "Page path and archived page path do not match!");
        }

        [Test]
        public void GetPageContents()
        {
            // GET:archive/pages/{pageid}/contents
            // ...

            // 1. Create a page
            // 2. Set "This is content for page {id}" as page content
            // 3. Delete the page
            // (4) Assert page contents are consistent 

            Plug p = Utils.BuildPlugForAdmin();

            string id = null;
            string path = null;
            DreamMessage msg = PageUtils.CreateRandomPage(p, out id, out path);

            string contents = "This is content for page " + id;
            PageUtils.SavePage(p, path, contents);

            PageUtils.DeletePageByID(p, id, false);

            msg = p.At("archive", "pages", id, "contents").Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Page retrieval failed");
            Assert.IsTrue((msg.ToDocument()["body"].AsText ?? String.Empty) == contents, "Page contents and archived page contents do not match!");
        }
    }
}

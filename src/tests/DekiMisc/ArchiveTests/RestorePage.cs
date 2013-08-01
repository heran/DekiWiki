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
    public class RestorePage
    {
        [Test]
        public void ResorePageByID()
        {
            // POST:archive/pages/{pageid}/restore
            // ..

            Plug p = Utils.BuildPlugForAdmin();

            string id = null;
            string path = null;
            DreamMessage msg = PageUtils.CreateRandomPage(p, out id, out path);

            PageUtils.DeletePageByID(p, id, false);

            msg = p.At("archive", "pages", id, "restore").Post();
            Assert.AreEqual(DreamStatus.Ok, msg.Status);
            Assert.IsTrue(msg.ToDocument()["page/path"].AsText == path);
            Assert.IsTrue(msg.ToDocument()["page/@id"].AsText == id);
        }

        [Test]
        public void TestRestoreWhenAlreadyExist()
        {
            //Assumptions: 
            // 
            //Actions:
            // create new page
            // delete page
            // create new page with same name
            // restore old page
            //Expected result: 
            // conflict

            Plug p = Utils.BuildPlugForAdmin();

            string id = null;
            string path = null;
            DreamMessage msg = PageUtils.CreateRandomPage(p, out id, out path);

            PageUtils.DeletePageByID(p, id, false);

            msg = PageUtils.CreateRandomPage(p, path);

            try
            {
                msg = p.At("archive", "pages", id, "restore").Post();
                Assert.IsTrue(false);
            }
            catch (DreamResponseException ex)
            {
                Assert.IsTrue(ex.Response.Status == DreamStatus.Conflict);
            }
        }

        [Test]
        public void TestRestoreTwoTimes()
        {
            //Assumptions: 
            // 
            //Actions:
            // create new page
            // delete page
            // restore page
            // restore page second time
            //Expected result: 
            // not found

            Plug p = Utils.BuildPlugForAdmin();

            string id = null;
            DreamMessage msg = PageUtils.CreateRandomPage(p, out id);

            PageUtils.DeletePageByID(p, id, false);

            try
            {
                msg = p.At("archive", "pages", id, "restore").Post();
                Assert.AreEqual(DreamStatus.Ok, msg.Status);

                msg = p.At("archive", "pages", id, "restore").Post();
                Assert.IsTrue(false);
            }
            catch (DreamResponseException ex)
            {
                Assert.IsTrue(ex.Response.Status == DreamStatus.NotFound);
            }
        }

        [Test]
        public void TestRestoreSubpage()
        {
            //Assumptions: 
            // 
            //Actions:
            // create new page (page1)
            // create new subpage (page2)
            // delete page1 recurse
            // restore page1
            //Expected result: 
            // page1 has page2 as subpage

            Plug p = Utils.BuildPlugForAdmin();

            string id1 = null;
            string path1 = null;
            DreamMessage msg = PageUtils.CreateRandomPage(p, out id1, out path1);

            string id2 = null;
            string path2 = null;
            msg = PageUtils.CreateRandomPage(p, path1 + "/" + Utils.GenerateUniqueName(), out id2, out path2);

            msg = p.At("pages", id1, "subpages").Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status);
            Assert.IsFalse(msg.ToDocument()[string.Format("page.subpage[@id=\"{0}\"]", id2)].IsEmpty);

            PageUtils.DeletePageByID(p, id1, true);

            msg = p.At("archive", "pages", id1, "restore").Post();
            Assert.AreEqual(DreamStatus.Ok, msg.Status);

            msg = p.At("pages", id1, "subpages").Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status);
            Assert.IsFalse(msg.ToDocument()[string.Format("page.subpage[@id=\"{0}\"]", id2)].IsEmpty);
        }

        [Test]
        public void TestRestoreSubpage2()
        {
            //Assumptions: 
            // 
            //Actions:
            // create new page (page1)
            // create new subpage (page2)
            // delete page1 no recurse
            // restore page1
            //Expected result: 
            // conflict

            Plug p = Utils.BuildPlugForAdmin();

            string id1 = null;
            string path1 = null;
            DreamMessage msg = PageUtils.CreateRandomPage(p, out id1, out path1);

            string id2 = null;
            string path2 = null;
            msg = PageUtils.CreateRandomPage(p, path1 + "/" + Utils.GenerateUniqueName(), out id2, out path2);

            msg = p.At("pages", id1, "subpages").Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status);
            Assert.IsFalse(msg.ToDocument()[string.Format("page.subpage[@id=\"{0}\"]", id2)].IsEmpty);

            PageUtils.DeletePageByID(p, id1, false);

            try
            {
                msg = p.At("archive", "pages", id1, "restore").Post();
                Assert.IsTrue(false);
            }
            catch (DreamResponseException ex)
            {
                Assert.IsTrue(ex.Response.Status == DreamStatus.Conflict);
            }
        }

        [Test]
        public void TestRestoreAnonymous()
        {
            //Assumptions: 
            // 
            //Actions:
            // connect to server as anonymous
            // try to restore any page
            //Expected result: 
            // forbidden

            Plug p = Utils.BuildPlugForAnonymous();

            try
            {
                DreamMessage msg = p.At("archive", "pages", "1", "restore").Post();
                Assert.IsTrue(false);
            }
            catch (DreamResponseException ex)
            {
                Assert.IsTrue(ex.Response.Status == DreamStatus.Unauthorized);
            }
        }

        [Test]
        public void TestRestoreWithFile()
        {
            //Assumptions: 
            // 
            //Actions:
            // create new page
            // upload random file to the page
            // delete page
            // restore page
            //Expected result: 
            // page has file

            Plug p = Utils.BuildPlugForAdmin();

            string id = null;
            DreamMessage msg = PageUtils.CreateRandomPage(p, out id);

            string fileid = null;
            msg = FileUtils.UploadRandomFile(p, id, out fileid);

            PageUtils.DeletePageByID(p, id, false);

            msg = p.At("archive", "pages", id, "restore").Post();
            Assert.AreEqual(DreamStatus.Ok, msg.Status);

            msg = p.At("pages", id).Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status);
            Assert.IsTrue(msg.ToDocument()["files/file/@id"].AsText == fileid);
        }
    }
}

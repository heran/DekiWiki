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
    public class RestoreFile
    {
        [Test]
        public void RestoreFileByID()
        {
            // POST:archive/files/restore/{fileid}
            // http://developer.mindtouch.com/Deki/API_Reference/POST%3aarchive%2f%2ffiles%2f%2frestore%2f%2f%7bfileid%7d

            Plug p = Utils.BuildPlugForAdmin();

            string id = null;
            DreamMessage msg = PageUtils.CreateRandomPage(p, out id);

            string fileid = null;
            msg = FileUtils.UploadRandomFile(p, id, out fileid);

            msg = FileUtils.DeleteFile(p, fileid);

            msg = p.At("archive", "files").Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status);
            Assert.IsFalse(msg.ToDocument()[string.Format("file.archive[@id=\"{0}\"]", fileid)].IsEmpty);

            msg = p.At("archive", "files", "restore", fileid).Post();
            Assert.AreEqual(DreamStatus.Ok, msg.Status);

            msg = p.At("archive", "files").Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status);
            Assert.IsTrue(msg.ToDocument()[string.Format("file.archive[@id=\"{0}\"]", fileid)].IsEmpty);

            msg = p.At("pages", id, "files").Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status);
            Assert.IsFalse(msg.ToDocument()[string.Format("file[@id=\"{0}\"]", fileid)].IsEmpty);
        }

        [Test]
        public void TestRestoreWhenAlreadyExist()
        {
            //Assumptions: 
            // 
            //Actions:
            // create new page
            // upload file to the page
            // delete file
            // upload file with same name to the page
            // restore old file
            //Expected result: 
            // page has two files

            Plug p = Utils.BuildPlugForAdmin();

            string id = null;
            DreamMessage msg = PageUtils.CreateRandomPage(p, out id);

            string filename = FileUtils.CreateRamdomFile(FileUtils.GenerateRandomContent());
            string fileid = null;
            msg = FileUtils.UploadFile(p, id, string.Empty, out fileid, filename);

            msg = FileUtils.DeleteFile(p, fileid);

            string newfileid = null;
            msg = FileUtils.UploadFile(p, id, string.Empty, out newfileid, filename);

            msg = p.At("archive", "files", "restore", fileid).Post();
            Assert.AreEqual(DreamStatus.Ok, msg.Status);

            msg = p.At("pages", id, "files").Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status);
            Assert.IsTrue(msg.ToDocument()["@count"].AsInt == 2);
            Assert.IsFalse(msg.ToDocument()[string.Format("file[@id=\"{0}\"]", fileid)].IsEmpty);
            Assert.IsFalse(msg.ToDocument()[string.Format("file[@id=\"{0}\"]", newfileid)].IsEmpty);
        }

        [Test]
        public void TestRestoreTwoTimes()
        {
            //Assumptions: 
            // 
            //Actions:
            // create new page
            // upload file to the page
            // delete file
            // restore file
            // restore file second time
            //Expected result: 
            // not found

            Plug p = Utils.BuildPlugForAdmin();

            string id1 = null;
            DreamMessage msg = PageUtils.CreateRandomPage(p, out id1);

            string fileid = null;
            msg = FileUtils.UploadRandomFile(p, id1, out fileid);

            msg = FileUtils.DeleteFile(p, fileid);

            msg = p.At("archive", "files", "restore", fileid).Post();
            Assert.AreEqual(DreamStatus.Ok, msg.Status);

            try
            {
                msg = p.At("archive", "files", "restore", fileid).Post();
                Assert.IsTrue(false);
            }
            catch (DreamResponseException ex)
            {
                Assert.IsTrue(ex.Response.Status == DreamStatus.NotFound);
            }
        }

        [Test]
        public void TestRestoreToOtherPage()
        {
            //Assumptions: 
            // 
            //Actions:
            // create new page1
            // upload file to the page
            // delete file
            // create new page2
            // restore file to page2
            //Expected result: 
            // page1 hasn't any files
            // page2 has file

            Plug p = Utils.BuildPlugForAdmin();

            string id1 = null;
            DreamMessage msg = PageUtils.CreateRandomPage(p, out id1);

            string fileid = null;
            msg = FileUtils.UploadRandomFile(p, id1, out fileid);

            msg = FileUtils.DeleteFile(p, fileid);

            string id2 = null;
            msg = PageUtils.CreateRandomPage(p, out id2);

            msg = p.At("archive", "files", "restore", fileid).With("to", id2).Post();
            Assert.AreEqual(DreamStatus.Ok, msg.Status);

            msg = p.At("pages", id1, "files").Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status);
            Assert.AreEqual(0, msg.ToDocument()["@count"].AsInt);

            msg = p.At("pages", id2, "files").Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status);
            Assert.AreEqual(1, msg.ToDocument()["@count"].AsInt);
            Assert.IsFalse(msg.ToDocument()[string.Format("file[@id=\"{0}\"]", fileid)].IsEmpty);
        }

        [Test]
        public void TestRestoreAnonymous()
        {
            //Assumptions: 
            // 
            //Actions:
            // connect to server as anonymous
            // try to restore any file
            //Expected result: 
            // Unauthorized

            Plug p = Utils.BuildPlugForAnonymous();

            try
            {
                DreamMessage msg = p.At("archive", "files", "restore", "1").Post();
                Assert.IsTrue(false);
            }
            catch (DreamResponseException ex)
            {
                Assert.AreEqual(DreamStatus.Unauthorized, ex.Response.Status);
            }
        }

    }
}

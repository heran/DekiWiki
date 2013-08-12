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
using NUnit.Framework;
using MindTouch.Dream;

namespace MindTouch.Deki.Tests.ArchiveTests {
    [TestFixture]
    public class FilesTests {
        [Test]
        public void GetFiles() {
            // GET:archive/files
            // http://developer.mindtouch.com/Deki/API_Reference/GET%3aarchive%2f%2ffiles

            // 1. Create a page
            // 2. Upload a file attachment to page
            // 3. Delete said attachment
            // (4) Assert file existence in archive: fileID = file.archiveID
            // 5. Delete page

            Plug p = Utils.BuildPlugForAdmin();
            DreamMessage msg;

            string id = null;
            msg = PageUtils.CreateRandomPage(p, out id);

            string fileid = null;
            msg = FileUtils.UploadRandomFile(p, id, out fileid);

            msg = FileUtils.DeleteFile(p, fileid);

            msg = p.At("archive", "files").Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Page retrieval failed");
            //Assert.IsFalse(msg.ToDocument()[string.Format("file.archive[@id=\"{0}\"]", fileid)].IsEmpty, "File is not in archive!");

            //PageUtils.DeletePageByID(p, id, true);
        }

        [Test]
        public void GetFileInfo() {
            // GET:archive/files/{fileid}/info
            // http://developer.mindtouch.com/Deki/API_Reference/GET%3aarchive%2f%2ffiles%2f%2f%7bfileid%7d%2f%2finfo

            // 1. Create a page
            // 2. Upload a file 
            // 3. Delete file
            // (4) Assert fileID matches file.archiveID
            // 5. Delete page

            Plug p = Utils.BuildPlugForAdmin();

            string id = null;
            DreamMessage msg = PageUtils.CreateRandomPage(p, out id);

            string fileid = null;
            msg = FileUtils.UploadRandomFile(p, id, out fileid);

            msg = FileUtils.DeleteFile(p, fileid);

            msg = p.At("archive", "files", fileid, "info").Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Page retrieval failed");
            Assert.IsTrue((msg.ToDocument()["@id"].AsText ?? String.Empty) == fileid, "File archive ID does not match file ID!");

            PageUtils.DeletePageByID(p, id, true);
        }

        [Test]
        public void GetFileContent() {
            // GET:archive/files/{fileid}
            // http://developer.mindtouch.com/Deki/API_Reference/GET%3aarchive%2f%2ffiles%2f%2f%7bfileid%7d

            // 1. Create a page
            // 2. Upload a file
            // 3. Delete the file
            // (4a) Assert that retrieved content matches generated content
            // (4b) Same assertion as 4a, but with the filename appended to path

            Plug p = Utils.BuildPlugForAdmin();

            string id = null;
            DreamMessage msg = PageUtils.CreateRandomPage(p, out id);

            byte[] content = FileUtils.GenerateRandomContent();
            string fileid = null;
            string filename = null;
            msg = FileUtils.UploadRandomFile(p, id, content, string.Empty, out fileid, out filename);

            msg = FileUtils.DeleteFile(p, fileid);

            msg = p.At("archive", "files", fileid).Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Page retrieval failed, GET:archive/files/{fileid}");
            Assert.IsTrue(Utils.ByteArraysAreEqual(content, msg.ToBytes()), "Content of archived file does not match that of original file! GET:archive/files/{fileid}");

            // GET:archive/files/{fileid}/{filename}
            // http://developer.mindtouch.com/Deki/API_Reference/GET%3aarchive%2f%2ffiles%2f%2f%7bfileid%7d%2f%2f%7bfilename%7d

            msg = p.At("archive", "files", fileid, "=" + filename).Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Page retrieval failed, GET:archive/files/{fileid}/{filename}");
            Assert.IsTrue(Utils.ByteArraysAreEqual(content, msg.ToBytes()), "Content of archived file does not match that of original file! GET:archive/files/{fileid}/{filename}");

            PageUtils.DeletePageByID(p, id, true);
        }

        [Test]
        public void DeleteFile() {
            // DELETE:archive/files/{fileid}
            // http://developer.mindtouch.com/Deki/API_Reference/DELETE%3aarchive%2f%2ffiles%2f%2f%7bfileid%7d

            // 1. Create a page
            // 2. Upload a file
            // 3. Delete the file
            // (4) Assert file exists in archive
            // 5. Delete file from archive
            // (6) Assert file does not exist in archive
            // 7. Delete page

            Plug p = Utils.BuildPlugForAdmin();

            string id = null;
            DreamMessage msg = PageUtils.CreateRandomPage(p, out id);

            string fileid = null;
            msg = FileUtils.UploadRandomFile(p, id, out fileid);

            msg = FileUtils.DeleteFile(p, fileid);

            msg = p.At("archive", "files").Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Page retrieval failed (before DELETE)");
            Assert.IsFalse(msg.ToDocument()[string.Format("file.archive[@id=\"{0}\"]", fileid)].IsEmpty, "File is not in archive!");

            msg = p.At("archive", "files", fileid).Delete();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "DELETE failed");

            msg = p.At("archive", "files").Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Page retrieval failed (after DELETE)");
            Assert.IsTrue(msg.ToDocument()[string.Format("file.archive[@id=\"{0}\"]", fileid)].IsEmpty, "File is still in the archive!");

            PageUtils.DeletePageByID(p, id, true);
        }

        [Test]
        public void DeleteFiles() {
            // DELETE:archive/files
            // http://developer.mindtouch.com/Deki/API_Reference/DELETE%3aarchive%2f%2ffiles

            // 1. Create a page
            // 2. Upload a file
            // 3. Delete the file
            // (4) Assert file exists in archive
            // 5. Delete file from archive
            // (6a) Assert file does not exist in archive
            // (6b) Assert file archive is empty
            // 7. Delete page

            Plug p = Utils.BuildPlugForAdmin();

            string id = null;
            DreamMessage msg = PageUtils.CreateRandomPage(p, out id);

            string fileid = null;
            msg = FileUtils.UploadRandomFile(p, id, out fileid);

            msg = FileUtils.DeleteFile(p, fileid);

            msg = p.At("archive", "files").Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Page retrieval failed (before DELETE)");
            Assert.IsFalse(msg.ToDocument()[string.Format("file.archive[@id=\"{0}\"]", fileid)].IsEmpty, "The file archive is empty!");

            msg = p.At("archive", "files").Delete();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "DELETE failed");

            msg = p.At("archive", "files").Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Page retrieval failed (after DELETE)");
            Assert.IsTrue(msg.ToDocument()[string.Format("file.archive[@id=\"{0}\"]", fileid)].IsEmpty, "The archived file still exists!");
            Assert.IsTrue(msg.ToDocument()["file.archive"].IsEmpty, "The file archive still contains files!");

            PageUtils.DeletePageByID(p, id, true);
        }
    }
}

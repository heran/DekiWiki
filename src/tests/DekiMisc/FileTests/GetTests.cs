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
using System.IO;

using NUnit.Framework;
using MindTouch.Dream;
using MindTouch.Xml;

namespace MindTouch.Deki.Tests.FileTests
{
    [TestFixture]
    public class GetTests
    {
        [Test]
        public void GetFiles()
        {
            // GET:files
            // http://developer.mindtouch.com/Deki/API_Reference/GET%3afiles

            Plug p = Utils.BuildPlugForAdmin();

            DreamMessage msg = p.At("files").Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status);
        }

        [Test]
        public void GetFileInfo()
        {
            // GET:pages/{pageid}/files/{filename}/info
            // http://developer.mindtouch.com/Deki/API_Reference/GET%3apages%2f%2f%7bpageid%7d%2f%2ffiles%2f%2f%7bfilename%7d%2f%2finfo

            Plug p = Utils.BuildPlugForAdmin();

            string id = null;
            DreamMessage msg = PageUtils.CreateRandomPage(p, out id);

            string fileid = null;
            string filename = null;
            msg = FileUtils.UploadRandomFile(p, id, out fileid, out filename);

            msg = p.At("pages", id, "files", "=" + filename, "info").Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status);
            Assert.IsTrue(msg.ToDocument()["@id"].AsText == fileid);
            Assert.IsTrue(msg.ToDocument()["page.parent/@id"].AsText == id);

            // GET:files/{fileid}/info
            // http://developer.mindtouch.com/Deki/API_Reference/GET%3afiles%2f%2f%7bfileid%7d%2f%2finfo

            msg = p.At("files", fileid, "info").Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status);
            Assert.IsTrue(msg.ToDocument()["@id"].AsText == fileid);
            Assert.IsTrue(msg.ToDocument()["page.parent/@id"].AsText == id);

            PageUtils.DeletePageByID(p, id, true);
        }

        [Test]
        public void GetFileContent()
        {
            // GET:files/{fileid}
            // http://developer.mindtouch.com/Deki/API_Reference/GET%3afiles%2f%2f%7bfileid%7d

            Plug p = Utils.BuildPlugForAdmin();

            string id = null;
            string path = null;
            DreamMessage msg = PageUtils.CreateRandomPage(p, out id, out path);

            byte[] content = FileUtils.GenerateRandomContent();
            string fileid = null;
            string filename = null;
            msg = FileUtils.UploadRandomFile(p, id, content, string.Empty, out fileid, out filename);

            msg = p.At("files", fileid).Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status);
            Assert.IsTrue(Utils.ByteArraysAreEqual(content, msg.AsBytes()));

            // GET:files/{fileid}/{filename}
            // http://developer.mindtouch.com/Deki/API_Reference/GET%3afiles%2f%2f%7bfileid%7d%2f%2f%7bfilename%7d

            msg = p.At("files", fileid, "=" + filename).Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status);
            Assert.IsTrue(Utils.ByteArraysAreEqual(content, msg.AsBytes()));

            PageUtils.DeletePageByID(p, id, true);
        }

        [Test]
        public void GetFileRevisions()
        {
            // GET:files/{fileid}/revisions
            // http://developer.mindtouch.com/Deki/API_Reference/GET%3afiles%2f%2f%7bfileid%7d%2f%2frevisions

            Plug p = Utils.BuildPlugForAdmin();

            string id = null;
            string path = null;
            DreamMessage msg = PageUtils.CreateRandomPage(p, out id, out path);

            byte[] content = FileUtils.GenerateRandomContent();
            string filename = FileUtils.CreateRamdomFile(content);
            string fileid = null;
            msg = FileUtils.UploadFile(p, id, string.Empty, out fileid, filename);

            msg = p.At("files", fileid, "revisions").Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status);
            Assert.IsTrue(msg.ToDocument()["@count"].AsInt == 1);

            content = FileUtils.GenerateRandomContent();
            using (System.IO.FileStream file = System.IO.File.Create(filename))
                file.Write(content, 0, content.Length);
            msg = FileUtils.UploadFile(p, id, string.Empty, out fileid, filename);
            filename = msg.ToDocument()["filename"].AsText;
            Assert.IsFalse(string.IsNullOrEmpty(filename));

            msg = p.At("files", fileid, "revisions").Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status);
            Assert.IsTrue(msg.ToDocument()["@count"].AsInt == 2);

            // GET:pages/{pageid}/files/{filename}/revisions
            // http://developer.mindtouch.com/Deki/API_Reference/GET%3apages%2f%2f%7bpageid%7d%2f%2ffiles%2f%2f%7bfilename%7d%2f%2frevisions

            msg = p.At("pages", id, "files", "=" + filename, "revisions").Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status);
            Assert.IsTrue(msg.ToDocument()["@count"].AsInt == 2);

            PageUtils.DeletePageByID(p, id, true);
        }

        [Ignore]
        [Test]
        public void RetrieveFileAttachmentContent()
        {
            // HEAD:pages/{pageid}/files/{filename}
            // http://developer.mindtouch.com/Deki/API_Reference/HEAD%3apages%2f%2f%7bpageid%7d%2f%2ffiles%2f%2f%7bfilename%7d

            Plug p = Utils.BuildPlugForAdmin();

            string id = null;
            DreamMessage msg = PageUtils.CreateRandomPage(p, out id);

            string fileid = null;
            string filename = null;
            msg = FileUtils.UploadRandomFile(p, id, out fileid, out filename);

            msg = p.At("pages", id, "files", "=" + filename).Invoke("HEAD", DreamMessage.Ok());
            Assert.AreEqual(DreamStatus.Ok, msg.Status);

            // HEAD:files/{fileid}/{filename}
            // http://developer.mindtouch.com/Deki/API_Reference/HEAD%3afiles%2f%2f%7bfileid%7d%2f%2f%7bfilename%7d

            msg = p.At("files", fileid, "=" + filename).Invoke("HEAD", DreamMessage.Ok());
            Assert.AreEqual(DreamStatus.Ok, msg.Status);

            // HEAD:files/{fileid}
            // http://developer.mindtouch.com/Deki/API_Reference/HEAD%3afiles%2f%2f%7bfileid%7d

            msg = p.At("files", fileid).Invoke("HEAD", DreamMessage.Ok());
            Assert.AreEqual(DreamStatus.Ok, msg.Status);

            // HEAD:files/{name}
            // http://developer.mindtouch.com/Deki/API_Reference/HEAD%3afiles%2f%2f%7bname%7d
            msg = p.At("files", "=" + filename).Invoke("HEAD", DreamMessage.Ok());
            Assert.AreEqual(DreamStatus.Ok, msg.Status);

            PageUtils.DeletePageByID(p, id, true);
        }

        [Test]
        public void GetFileFromAnonymous()
        {
            //Assumptions: 
            // 
            //Actions:
            // create page
            // upload file to page
            // try get file from anonymous account
            //Expected result: 
            // ok

            Plug p = Utils.BuildPlugForAdmin();

            string id = null;
            DreamMessage msg = PageUtils.CreateRandomPage(p, out id);

            byte[] content = FileUtils.GenerateRandomContent();
            string fileid = null;
            string filename = null;
            msg = FileUtils.UploadRandomFile(p, id, content, string.Empty, out fileid, out filename);

            p = Utils.BuildPlugForAnonymous();

            msg = p.At("files", fileid).Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status);
            Assert.IsTrue(Utils.ByteArraysAreEqual(content, msg.AsBytes()));

            p = Utils.BuildPlugForAdmin();
            PageUtils.DeletePageByID(p, id, true);
        }

        [Test]
        public void GetFileSort() {
            Plug p = Utils.BuildPlugForAdmin();

            string id = null;
            DreamMessage msg = PageUtils.CreateRandomPage(p, out id);

            string fileid1, fileid2, fileid3 = null;

            string filename1 = "ac.txt";
            string filename2 = "ab.txt";
            string filename3 = "aa.txt";

            string tmpPath = Path.GetTempPath();
            string filepath1 = Path.Combine(tmpPath, filename1);
            string filepath2 = Path.Combine(tmpPath, filename2);
            string filepath3 = Path.Combine(tmpPath, filename3);
            
            try {
                FileUtils.CreateFile(null, filepath1);
                FileUtils.CreateFile(null, filepath2);
                FileUtils.CreateFile(null, filepath3);

                msg = FileUtils.UploadFile(p, id, "", out fileid1, filepath1);
                msg = p.At("pages", id, "files", "=" + filename1).Invoke("HEAD", DreamMessage.Ok());
                Assert.AreEqual(DreamStatus.Ok, msg.Status);

                msg = FileUtils.UploadFile(p, id, "", out fileid2, filepath2);
                msg = p.At("pages", id, "files", "=" + filename2).Invoke("HEAD", DreamMessage.Ok());
                Assert.AreEqual(DreamStatus.Ok, msg.Status);

                msg = FileUtils.UploadFile(p, id, "", out fileid3, filepath3);
                msg = p.At("pages", id, "files", "=" + filename3).Invoke("HEAD", DreamMessage.Ok());
                Assert.AreEqual(DreamStatus.Ok, msg.Status);

                // check sort order of files via GET:pages/{id}
                msg = p.At("pages", id).Get();
                List<XDoc> files = msg.ToDocument()["files/file"].ToList();
                Assert.IsTrue(1 > StringUtil.CompareInvariant(files[0]["filename"].AsText, files[1]["filename"].AsText));
                Assert.IsTrue(1 > StringUtil.CompareInvariant(files[1]["filename"].AsText, files[2]["filename"].AsText));      

                // check sort order of files via GET:pages/{id}/files
                msg = p.At("pages", id, "files").Get();
                files = msg.ToDocument()["file"].ToList();
                Assert.IsTrue(1 > StringUtil.CompareInvariant(files[0]["filename"].AsText, files[1]["filename"].AsText));
                Assert.IsTrue(1 > StringUtil.CompareInvariant(files[1]["filename"].AsText, files[2]["filename"].AsText));      

            } finally {
                File.Delete(filepath1);
                File.Delete(filepath2);
                File.Delete(filepath3);
            }
            PageUtils.DeletePageByID(p, id, true);
        }
    }
}

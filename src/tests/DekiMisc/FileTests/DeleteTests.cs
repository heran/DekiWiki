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

namespace MindTouch.Deki.Tests.FileTests
{
    [TestFixture]
    public class DeleteTests
    {
        [Test]
        public void DeleteFile()
        {
            // DELETE:files/{fileid}
            // http://developer.mindtouch.com/Deki/API_Reference/DELETE%3afiles%2f%2f%7bfileid%7d

            Plug p = Utils.BuildPlugForAdmin();

            string id = null;
            string path = null;
            DreamMessage msg = PageUtils.CreateRandomPage(p, out id, out path);

            string fileid = null;
            string filename = null;
            FileUtils.UploadRandomFile(p, id, out fileid, out filename);

            msg = p.At("files", fileid).Delete();
            Assert.AreEqual(DreamStatus.Ok, msg.Status);

            msg = p.At("pages", id, "files").Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status);
            Assert.IsTrue(msg.ToDocument()["@count"].AsInt == 0);

            msg = p.At("archive", "files").Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status);
            Assert.IsFalse(msg.ToDocument()[string.Format("file.archive[@id=\"{0}\"]", fileid)].IsEmpty);

            PageUtils.DeletePageByID(p, id, true);
        }

        [Test]
        public void DeleteFileThroughPage()
        {
            // DELETE:pages/{pageid}/files/{filename}
            // http://developer.mindtouch.com/Deki/API_Reference/DELETE%3apages%2f%2f%7bpageid%7d%2f%2ffiles%2f%2f%7bfilename%7d

            Plug p = Utils.BuildPlugForAdmin();

            string id = null;
            DreamMessage msg = PageUtils.CreateRandomPage(p, out id);

            string fileid = null;
            string filename = null;
            FileUtils.UploadRandomFile(p, id, out fileid, out filename);

            msg = p.At("pages", id, "files", "=" + filename).Delete();
            Assert.AreEqual(DreamStatus.Ok, msg.Status);

            msg = p.At("pages", id, "files").Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status);
            Assert.IsTrue(msg.ToDocument()["@count"].AsInt == 0);

            msg = p.At("archive", "files").Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status);
            Assert.IsFalse(msg.ToDocument()[string.Format("file.archive[@id=\"{0}\"]", fileid)].IsEmpty);

            PageUtils.DeletePageByID(p, id, true);
        }
    }
}

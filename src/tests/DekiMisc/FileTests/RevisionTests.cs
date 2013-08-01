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

using System.Collections.Generic;
using System.IO;
using System.Text;

using NUnit.Framework;
using MindTouch.Dream;
using MindTouch.Xml;

namespace MindTouch.Deki.Tests.FileTests
{
    [TestFixture]
    public class RevisionTests
    {
        [Test]
        public void RevisionHideAndUnhide() {
            Plug p = Utils.BuildPlugForAdmin();

            string id = null;
            string path = null;
            string fileid = null;
            string filename = null;
            DreamMessage msg = PageUtils.SavePage(p, string.Empty, PageUtils.GenerateUniquePageName(), "filerevhidetest", out id, out path);
            string filepath = FileUtils.CreateRamdomFile(Encoding.UTF8.GetBytes("My contents."));
            FileUtils.UploadFile(p, id, "test file rev 1", out fileid, filepath);
            FileUtils.UploadFile(p, id, "test file rev 2", out fileid, filepath);
            FileUtils.UploadFile(p, id, "test file rev 3", out fileid, filepath);
                              
            string userid;
            string username;
            UserUtils.CreateRandomUser(p, "Contributor", out userid, out username);

            //Check that anon can see contents before hiding revs
            msg = Utils.BuildPlugForUser(username).At("files", fileid, "contents").With("revision", 2).GetAsync().Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "reg user can't see contents even before hiding!");

            //Reinit plug to admin
            Utils.BuildPlugForAdmin();

            string comment = "just cuz..";
            XDoc hideRequestXml = new XDoc("revisions").Start("file").Attr("id", fileid).Attr("hidden", true).Attr("revision", 2).End();
            msg = p.At("files", fileid, "revisions").With("comment", comment).PostAsync(hideRequestXml).Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Non 200 status hiding revisions");

            //Ensure correct revisions coming back is visible + hidden
            msg = p.At("files", fileid, "info").With("revision", 1).GetAsync().Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "files/{id}/info?revision=x returned non 200 status");
            Assert.IsFalse(msg.ToDocument()["/page[@revision = \"1\"]/@hidden"].AsBool ?? false, "Rev 1 is hidden!");

            //validate hidden rev
            msg = p.At("files", fileid, "info").With("revision", 2).GetAsync().Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "files/{id}/info?revision=x returned non 200 status");
            Assert.IsTrue(msg.ToDocument()["/file[@revision = \"2\"]/@hidden"].AsBool ?? false, "Rev 2 is not hidden!");
            Assert.AreEqual(comment, msg.ToDocument()["/file[@revision = \"2\"]/description.hidden"].AsText, "hide comment missing or invalid");
            Assert.IsTrue(!string.IsNullOrEmpty(msg.ToDocument()["/file[@revision = \"2\"]/date.hidden"].AsText), "date.hidden missing");
            Assert.IsNotNull(msg.ToDocument()["/file[@revision = \"2\"]/user.hiddenby/@id"].AsUInt, "user.hiddenby id missing");

            msg = p.At("files", fileid, "info").With("revision", 3).GetAsync().Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "files/{id}/info?revision=x returned non 200 status");
            Assert.IsFalse(msg.ToDocument()["/file[@revision = \"3\"]/@hidden"].AsBool ?? false, "Rev 3 is hidden!");

            //Ensure admin still has rights to see hidden contents
            msg = p.At("files", fileid).With("revision", 2).GetAsync().Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "admin can't see hidden contents!");

            //Ensure non-admin cannot see hidden contents
            msg = Utils.BuildPlugForUser(username).At("files", fileid).With("revision", 2).GetAsync().Wait();
            Assert.IsTrue(msg.Status == DreamStatus.Unauthorized || msg.Status == DreamStatus.Forbidden, "reg user can still see contents!");

            //Attempt to unhide a rev by non admin
            hideRequestXml = new XDoc("revisions").Start("file").Attr("id", fileid).Attr("hidden", false).Attr("revision", 2).End();
            msg = p.At("files", fileid, "revisions").With("comment", comment).PostAsync(hideRequestXml).Wait();
            Assert.AreEqual(DreamStatus.Forbidden, msg.Status, "non admin able to unhide rev");

            //Attempt to hide a rev by non admin
            hideRequestXml = new XDoc("revisions").Start("file").Attr("id", fileid).Attr("hidden", true).Attr("revision", 1).End();
            msg = p.At("files", fileid, "revisions").With("comment", comment).PostAsync(hideRequestXml).Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "DELETE holder unable to hide rev");

            //Unhide a rev as normal user (fail!)
            hideRequestXml = new XDoc("revisions").Start("file").Attr("id", fileid).Attr("hidden", false).Attr("revision", 1).End();
            msg = p.At("files", fileid, "revisions").With("comment", comment).PostAsync(hideRequestXml).Wait();
            Assert.AreEqual(DreamStatus.Forbidden, msg.Status, "normal user able to unhide!");

            //Reinit plug to admin
            Utils.BuildPlugForAdmin();

            //Unhide a rev as admin
            hideRequestXml = new XDoc("revisions").Start("file").Attr("id", fileid).Attr("hidden", false).Attr("revision", 1).End();
            msg = p.At("files", fileid, "revisions").With("comment", comment).PostAsync(hideRequestXml).Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "admin unable to make rev visible");

            //confirm rev 1 is visible now
            msg = p.At("files", fileid, "info").With("revision", 1).GetAsync().Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "files/{id}/info?revision=x returned non 200 status");
            Assert.IsFalse(msg.ToDocument()["/file[@revision = \"1\"]/@hidden"].AsBool ?? false, "Rev 1 is still hidden!");
        }

        [Test]
        public void GetRevisionSort() {
            Plug p = Utils.BuildPlugForAdmin();
            int count = 5;
            string id = null;
            DreamMessage msg = PageUtils.CreateRandomPage(p, out id);

            string fileid = null;
            string filename = "aa.txt";
            string filepath = Path.Combine(Path.GetTempPath(), filename);
            try {
                FileUtils.CreateFile(null, filepath);
                for(int i = 0; i < count; i++) {
                    msg = FileUtils.UploadFile(p, id, "", out fileid, filepath);
                    msg = p.At("pages", id, "files", "=" + filename).Invoke("HEAD", DreamMessage.Ok());
                    Assert.AreEqual(DreamStatus.Ok, msg.Status);
                }
                msg = p.At("files", fileid, "revisions").Get();
                List<XDoc> files = msg.ToDocument()["file"].ToList();
                for(int i = 0; i < count; i++)
                    Assert.AreEqual(i + 1, files[i]["@revision"].AsInt);

                PageUtils.DeletePageByID(p, id, true);
            } finally {
                File.Delete(filepath);
            }
        }
    }
}

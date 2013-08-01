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
    public class MoveTests
    {
        [Test]
        public void MoveFile()
        {
            // POST:files/{fileid}/move
            // http://developer.mindtouch.com/Deki/API_Reference/POST%3afiles%2f%2f%7bfileid%7d%2f%2fmove

            // 1. Create a source page
            // 2. Create a destination page
            // 3. Upload a file to source page
            // (4) Assert attachment exists in source page
            // 5. Move attachment to destination page
            // (6) Assert source page does not have the attachment
            // (7) Assert destination page has the attachment
            // 8. Delete page

            Plug p = Utils.BuildPlugForAdmin();

            string id = null;
            DreamMessage msg = PageUtils.CreateRandomPage(p, out id);

            string toid = null;
            msg = PageUtils.CreateRandomPage(p, out toid);

            string fileid = null;
            string filename = null;
            msg = FileUtils.UploadRandomFile(p, id, out fileid, out filename);

            msg = p.At("pages", id, "files").Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "GET pages/{id}/files failed. BEFORE move");
            Assert.IsFalse(msg.ToDocument()[string.Format("file[@id=\"{0}\"]", fileid)].IsEmpty, "Source page does not have the attachment! BEFORE move");

            msg = p.At("files", fileid, "move").With("to", toid).Post();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Move request failed!");

            msg = p.At("pages", id, "files").Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "GET pages/{id}/files failed. AFTER move");
            Assert.IsTrue(msg.ToDocument()[string.Format("file[@id=\"{0}\"]", fileid)].IsEmpty, "Source page has the attachment! AFTER move");

            msg = p.At("pages", toid, "files").Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "GET pages/{toid}/files failed. AFTER move");
            Assert.IsFalse(msg.ToDocument()[string.Format("file[@id=\"{0}\"]", fileid)].IsEmpty, "Destination page does not have the attachment! AFTER move");

            PageUtils.DeletePageByID(p, id, true);
        }

        [Test]
        public void MoveFileToDeletedPage()
        {
            //Assumptions: 
            //Actions:
            // 1. Create page1
            // 2. Create page2
            // 3. Delete page2
            // 4. Upload file to page1
            // 5. Attempt to move file from page1 to page2
            // (6) Assert a Not Found response is retruned
            // 7. Delete page1
            //Expected result: 
            // NotFound

            Plug p = Utils.BuildPlugForAdmin();

            string id = null;
            string path = null;
            DreamMessage msg = PageUtils.CreateRandomPage(p, out id, out path);

            string id2 = null;
            string path2 = null;
            msg = PageUtils.CreateRandomPage(p, out id2, out path2);
            PageUtils.DeletePageByID(p, id2, true);

            try
            {
                string fileid = null;
                string filename = null;
                msg = FileUtils.UploadRandomFile(p, id, out fileid, out filename);

                msg = p.At("files", fileid, "move").With("to", id2).Post();
                Assert.IsTrue(false, "File move succeeded?!");
            }
            catch (DreamResponseException ex)
            {
                Assert.AreEqual(DreamStatus.NotFound, ex.Response.Status, "Status other than \"Not Found\" returned: " + ex.Response.Status.ToString());
            }

            PageUtils.DeletePageByID(p, id, true);
        }

        [Test]
        public void MoveFileFromAnonymous()
        {
            //Assumptions: 
            // 
            //Actions:
            // 1. Create page1
            // 2. Create page2
            // 3. Upload file to page1
            // 4. Try to move file to page2 from anonymous account
            // (5) Assert an Unauthorized response is returned
            // 6. Delete page1
            // 7. Delete page2
            //Expected result: 
            // Unauthorized

            Plug p = Utils.BuildPlugForAdmin();

            string id = null;
            DreamMessage msg = PageUtils.CreateRandomPage(p, out id);

            string toid = null;
            msg = PageUtils.CreateRandomPage(p, out toid);

            string fileid = null;
            string filename = null;
            msg = FileUtils.UploadRandomFile(p, id, out fileid, out filename);

            p = Utils.BuildPlugForAnonymous();

            try
            {
                msg = p.At("files", fileid, "move").With("to", toid).Post();
                Assert.IsTrue(false, "File move succeeded?!");
            }
            catch (DreamResponseException ex)
            {
                Assert.AreEqual(DreamStatus.Unauthorized, ex.Response.Status, "Status other than \"Unauthorized\" returned: " + ex.Response.Status.ToString());
            }

            p = Utils.BuildPlugForAdmin();
            PageUtils.DeletePageByID(p, id, true);
            PageUtils.DeletePageByID(p, toid, true);
        }

        [Test]
        public void MoveFileToChildPage()
        {
            //Assumptions: 
            // 
            //Actions:
            // 1. Create A
            // 2. Create B as child of page A
            // 3. Upload file to A
            // (4) Assert file exists on page A
            // 5. Try to move file to page B
            // (6) Assert file does not exist on page A
            // (7) Assert file exists on page B
            // 8. Delete page A recursively
            //Expected result: 
            // file is moved

            Plug p = Utils.BuildPlugForAdmin();

            string baseTreePath = PageUtils.BuildPageTree(p);

            string id = PageUtils.GetPage(p, baseTreePath + "/A").ToDocument()["@id"].AsText;
            string toid = PageUtils.GetPage(p, baseTreePath + "/A/B").ToDocument()["@id"].AsText;

            string fileid = null;
            string filename = null;
            DreamMessage msg = FileUtils.UploadRandomFile(p, id, out fileid, out filename);

            msg = p.At("pages", id, "files").Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "GET pages/{id}/files request failed. BEFORE move");
            Assert.IsFalse(msg.ToDocument()[string.Format("file[@id=\"{0}\"]", fileid)].IsEmpty, "Page does not contain generated file! BEFORE move");

            msg = p.At("files", fileid, "move").With("to", toid).Post();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Move request failed");

            msg = p.At("pages", id, "files").Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "GET pages/{id}/files request failed. AFTER move");
            Assert.IsTrue(msg.ToDocument()[string.Format("file[@id=\"{0}\"]", fileid)].IsEmpty, "Generated file still attached to page! AFTER move");

            msg = p.At("pages", toid, "files").Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "GET pages/{toid}/files request failed. AFTER move");
            Assert.IsFalse(msg.ToDocument()[string.Format("file[@id=\"{0}\"]", fileid)].IsEmpty, "Subpage does not contain generated file! AFTER move");

            PageUtils.DeletePageByID(p, id, true);
        }

        [Test]
        public void RenameFile() {

            // 1. Create a source page
            // 2. Create a destination page (child of source page)
            // 3. Upload a file to source page
            // (4) Assert move request without parameters fails
            // (5) Assert move request without renaming the file and without specifying destination fails
            // (6) Assert move request to same path fails
            // (7) Assert move request to exact same path and filename fails
            // (8) Assert move request to different location and name succeeds
            // (9) Assert a rename to a new name succeeds
            // (10) Assert move request to a different location works
            // (11) Assert the file revisions accurately reflect changes
            // 12. Delete the page

            Plug p = Utils.BuildPlugForAdmin();

            string baseTreePath = PageUtils.BuildPageTree(p);

            string id = PageUtils.GetPage(p, baseTreePath + "/A").ToDocument()["@id"].AsText;
            string toid = PageUtils.GetPage(p, baseTreePath + "/A/B").ToDocument()["@id"].AsText;

            string fileid = null;
            string filename = null;
            DreamMessage msg = FileUtils.UploadRandomFile(p, id, out fileid, out filename);

            //Test no parametrs
            msg = p.At("files", fileid, "move").PostAsync().Wait();
            Assert.AreEqual(DreamStatus.BadRequest, msg.Status, "No parameter test failed");

            //Test no change
            msg = p.At("files", fileid, "move").With("name", filename).PostAsync().Wait();
            Assert.AreEqual(DreamStatus.BadRequest, msg.Status, "No change test failed");

            msg = p.At("files", fileid, "move").With("to", id).PostAsync().Wait();
            Assert.AreEqual(DreamStatus.BadRequest, msg.Status, "No change test failed");

            msg = p.At("files", fileid, "move").With("to", id).With("name", filename).PostAsync().Wait();
            Assert.AreEqual(DreamStatus.BadRequest, msg.Status, "No change test failed");

            string newFileName = "newname.txt";

            //Move and rename
            msg = p.At("files", fileid, "move").With("to", toid).With("name", newFileName).Post();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "move+rename failed");
            Assert.AreEqual(toid, msg.ToDocument()["page.parent/@id"].AsText, "New page id is incorrect");
            Assert.AreEqual(newFileName, msg.ToDocument()["filename"].AsText, "New filename is incorrect");

            //rename
            msg = p.At("files", fileid, "move").With("name", filename).Post();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "rename failed");
            Assert.AreEqual(filename, msg.ToDocument()["filename"].AsText, "New filename is incorrect");

            //move
            msg = p.At("files", fileid, "move").With("to", id).Post();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "move failed");
            Assert.AreEqual(id, msg.ToDocument()["page.parent/@id"].AsText, "New page id is incorrect");

            //verify all revisions
            msg = p.At("files", fileid, "revisions").With("changefilter", "all").Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "get revisions failed");
            string actions = null;
            actions = msg.ToDocument()["file[@revision = '1']/user-action/@type"].AsText ?? string.Empty;
            Assert.IsTrue(actions.Contains("content"), "expected user action missing");
            Assert.IsTrue(actions.Contains("parent"), "expected user action missing");
            Assert.IsTrue(actions.Contains("name"), "expected user action missing");
            actions = msg.ToDocument()["file[@revision = '2']/user-action/@type"].AsText ?? string.Empty;
            Assert.IsTrue(actions.Contains("parent"), "expected user action missing");
            Assert.IsTrue(actions.Contains("name"), "expected user action missing");
            actions = msg.ToDocument()["file[@revision = '3']/user-action/@type"].AsText ?? string.Empty;
            Assert.IsTrue(actions.Contains("name"), "expected user action missing");
            actions = msg.ToDocument()["file[@revision = '4']/user-action/@type"].AsText ?? string.Empty;
            Assert.IsTrue(actions.Contains("parent"), "expected user action missing");            
            Assert.IsTrue(msg.ToDocument()["file[@revision = '5']"].IsEmpty, "5th rev exists!");
            
            PageUtils.DeletePageByID(p, id, true);
        }

        [Test]
        public void UploadRename() {

            // 1. Build a page tree
            // 2. Upload a file to the root of page tree (A)
            // 3. Replace file and rename it
            // (4) Assert replacement succeeded
            // (5) Assert the file revisions accurately reflect changes
            // (6) Assert new file name is consistent
            // 7. Upload a file to page with same name
            // (8) Assert Conflict response is returned
            // 9. Delete the page (recursive)

            Plug p = Utils.BuildPlugForAdmin();

            string baseTreePath = PageUtils.BuildPageTree(p);

            string pageid = PageUtils.GetPage(p, baseTreePath + "/A").ToDocument()["@id"].AsText;

            string fileid = null;
            string filename = null;
            string newfilename = "newfilename";
            DreamMessage msg = FileUtils.UploadRandomFile(p, pageid, out fileid, out filename);

            //Upload a new rev with rename
            msg = p.At("files", fileid, "=" + newfilename).Put(DreamMessage.Ok(MimeType.BINARY, FileUtils.GenerateRandomContent()));
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "upload with rename failed");

            //validate revisions
            msg = p.At("files", fileid, "revisions").With("changefilter", "all").Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "get revisions failed");
            string actions = null;
            actions = msg.ToDocument()["file[@revision = '1']/user-action/@type"].AsText ?? string.Empty;
            Assert.IsTrue(actions.Contains("content"), "expected user action missing");
            Assert.IsTrue(actions.Contains("parent"), "expected user action missing");
            Assert.IsTrue(actions.Contains("name"), "expected user action missing");
            actions = msg.ToDocument()["file[@revision = '2']/user-action/@type"].AsText ?? string.Empty;
            Assert.IsTrue(actions.Contains("content"), "expected user action missing");
            Assert.IsTrue(actions.Contains("name"), "expected user action missing");

            //Confirm new filename
            Assert.AreEqual(newfilename, msg.ToDocument()["file[@revision = '2']/filename"].AsText, "Filenames do not match!");

            string file3name;
            string file3id;
            msg = FileUtils.UploadRandomFile(p, pageid, out file3id, out file3name);

            //Upload new rev of file with rename that conflits with previous rename
            msg = p.At("files", file3id, "=" + newfilename).PutAsync(DreamMessage.Ok(MimeType.BINARY, FileUtils.GenerateRandomContent())).Wait();
            Assert.AreEqual(DreamStatus.Conflict, msg.Status, "upload with conflicting rename check failed");

            PageUtils.DeletePageByID(p, pageid, true);
        }
    }
}

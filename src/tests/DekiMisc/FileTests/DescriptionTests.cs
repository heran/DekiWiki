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
    public class DescriptionTests
    {
        [Test]
        public void CheckDescription()
        {
            // GET:pages/{pageid}/files/{filename}/description
            // http://developer.mindtouch.com/Deki/API_Reference/GET%3apages%2f%2f%7bpageid%7d%2f%2ffiles%2f%2f%7bfilename%7d%2f%2fdescription

            Plug p = Utils.BuildPlugForAdmin();

            string id = null;
            DreamMessage msg = PageUtils.CreateRandomPage(p, out id);

            string description = "File description text";
            string fileid = null;
            string filename = null;
            FileUtils.UploadRandomFile(p, id, null, description, out fileid, out filename);

            msg = p.At("pages", id, "files", "=" + filename, "description").Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status);
            Assert.IsTrue(msg.AsText() == description);

            // GET:files/{fileid}/description
            // http://developer.mindtouch.com/Deki/API_Reference/GET%3afiles%2f%2f%7bfileid%7d%2f%2fdescription

            msg = p.At("files", fileid, "description").Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status);
            Assert.IsTrue(msg.AsText() == description);

            PageUtils.DeletePageByID(p, id, true);
        }

        [Test]
        public void UpdateDescription()
        {
            // PUT:files/{fileid}/description
            // http://developer.mindtouch.com/Deki/API_Reference/PUT%3afiles%2f%2f%7bfileid%7d%2f%2fdescription

            Plug p = Utils.BuildPlugForAdmin();

            string id = null;
            DreamMessage msg = PageUtils.CreateRandomPage(p, out id);

            string description = Utils.GetRandomText(230);
            string fileid = null;
            string filename = null;
            FileUtils.UploadRandomFile(p, id, null, description, out fileid, out filename);

            description = Utils.GetRandomText(230);
            msg = DreamMessage.Ok(MimeType.TEXT_UTF8, description);
            msg = p.At("files", fileid, "description").Put(msg);
            Assert.AreEqual(DreamStatus.Ok, msg.Status);

            msg = p.At("pages", id, "files", "=" + filename, "description").Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status);
            Assert.IsTrue(msg.AsText() == description);

            // PUT:pages/{pageid}/files/{filename}/description
            // http://developer.mindtouch.com/Deki/API_Reference/PUT%3apages%2f%2f%7bpageid%7d%2f%2ffiles%2f%2f%7bfilename%7d%2f%2fdescription

            description = Utils.GetRandomText(230);
            msg = DreamMessage.Ok(MimeType.TEXT_UTF8, description);
            msg = p.At("pages", id, "files", "=" + filename, "description").Put(msg);
            Assert.AreEqual(DreamStatus.Ok, msg.Status);

            msg = p.At("pages", id, "files", "=" + filename, "description").Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status);
            Assert.IsTrue(msg.AsText() == description);

            PageUtils.DeletePageByID(p, id, true);
        }

        [Test]
        public void DeleteDescription()
        {
            // DELETE:files/{fileid}/description
            // http://developer.mindtouch.com/Deki/API_Reference/DELETE%3afiles%2f%2f%7bfileid%7d%2f%2fdescription

            Plug p = Utils.BuildPlugForAdmin();

            string id = null;
            DreamMessage msg = PageUtils.CreateRandomPage(p, out id);

            string description = "File description text";
            string fileid = null;
            string filename = null;
            FileUtils.UploadRandomFile(p, id, null, description, out fileid, out filename);

            msg = p.At("pages", id, "files", "=" + filename, "description").Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status);
            Assert.IsTrue(msg.AsText() == description);

            msg = p.At("files", fileid, "description").Delete();
            Assert.AreEqual(DreamStatus.Ok, msg.Status);

            msg = p.At("pages", id, "files", "=" + filename, "description").Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status);
            Assert.IsTrue(msg.AsText() == string.Empty);

            PageUtils.DeletePageByID(p, id, true);
        }

        [Test]
        public void DeleteDescriptionThroughPage()
        {
            // DELETE:pages/{pageid}/files/{filename}/description
            // http://developer.mindtouch.com/Deki/API_Reference/DELETE%3apages%2f%2f%7bpageid%7d%2f%2ffiles%2f%2f%7bfilename%7d%2f%2fdescription

            Plug p = Utils.BuildPlugForAdmin();

            string id = null;
            DreamMessage msg = PageUtils.CreateRandomPage(p, out id);

            string description = "File description text";
            string fileid = null;
            string filename = null;
            FileUtils.UploadRandomFile(p, id, null, description, out fileid, out filename);

            msg = p.At("pages", id, "files", "=" + filename, "description").Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status);
            Assert.IsTrue(msg.AsText() == description);

            msg = p.At("pages", id, "files", "=" + filename, "description").Delete();
            Assert.AreEqual(DreamStatus.Ok, msg.Status);

            msg = p.At("pages", id, "files", "=" + filename, "description").Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status);
            Assert.IsTrue(msg.AsText() == string.Empty);

            PageUtils.DeletePageByID(p, id, true);
        }

        [Test]
        public void PostBigDescription()
        {
            //Assumptions: 
            //Actions:
            // create page
            // create big description
            // post file with big description to page
            //Expected result: 
            // Ok

            Plug p = Utils.BuildPlugForAdmin();

            string id = null;
            DreamMessage msg = PageUtils.CreateRandomPage(p, out id);

            try
            {
                string description = Utils.GetSmallRandomText();// Utils.GetSmallRandomText();
                string fileid = null;
                string filename = null;
                msg = FileUtils.UploadRandomFile(p, id, null, description, out fileid, out filename);

                msg = p.At("pages", id, "files", "=" + filename, "description").Get();
                Assert.AreEqual(DreamStatus.Ok, msg.Status);
                Assert.IsTrue(msg.AsText() == description);
            }
            catch (DreamResponseException)
            {
                Assert.Fail();
            }

            PageUtils.DeletePageByID(p, id, true);
        }

        [Test]
        public void UpdateDescriptionFromAnonymous()
        {
            //Assumptions: 
            // 
            //Actions:
            // create page
            // upload file to page
            // try to edit file description from anonymous account
            //Expected result: 
            // Unauthorized

            Plug p = Utils.BuildPlugForAdmin();

            string id = null;
            DreamMessage msg = PageUtils.CreateRandomPage(p, out id);

            string description = Utils.GetRandomText(220);
            string fileid = null;
            string filename = null;
            FileUtils.UploadRandomFile(p, id, null, description, out fileid, out filename);

            p = Utils.BuildPlugForAnonymous();

            try
            {
                msg = DreamMessage.Ok(MimeType.TEXT_UTF8, Utils.GetRandomText(220));
                msg = p.At("files", fileid, "description").Put(msg);
                Assert.IsTrue(false);
            }
            catch (DreamResponseException ex)
            {
                Assert.IsTrue(ex.Response.Status == DreamStatus.Unauthorized);
            }

            try
            {
                msg = DreamMessage.Ok(MimeType.TEXT_UTF8, Utils.GetRandomText(220));
                msg = p.At("pages", id, "files", "=" + filename, "description").Put(msg);
                Assert.IsTrue(false);
            }
            catch (DreamResponseException ex)
            {
                Assert.IsTrue(ex.Response.Status == DreamStatus.Unauthorized);
            }

            msg = p.At("pages", id, "files", "=" + filename, "description").Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status);
            Assert.IsTrue(msg.AsText() == description);

            p = Utils.BuildPlugForAdmin();
            PageUtils.DeletePageByID(p, id, true);
        }

        [Test]
        public void DeleteDescriptionFromAnonymous()
        {
            //Assumptions: 
            // 
            //Actions:
            // create page
            // upload file to page
            // try to delete file description from anonymous account
            //Expected result: 
            // Unauthorized

            Plug p = Utils.BuildPlugForAdmin();

            string id = null;
            DreamMessage msg = PageUtils.CreateRandomPage(p, out id);

            string description = "File description text";
            string fileid = null;
            string filename = null;
            FileUtils.UploadRandomFile(p, id, null, description, out fileid, out filename);

            p = Utils.BuildPlugForAnonymous();
            try
            {
                msg = p.At("files", fileid, "description").Delete();
                Assert.IsTrue(false);
            }
            catch (DreamResponseException ex)
            {
                Assert.IsTrue(ex.Response.Status == DreamStatus.Unauthorized);
            }

            try
            {
                msg = p.At("pages", id, "files", "=" + filename, "description").Delete();
                Assert.IsTrue(false);
            }
            catch (DreamResponseException ex)
            {
                Assert.IsTrue(ex.Response.Status == DreamStatus.Unauthorized);
            }

            msg = p.At("pages", id, "files", "=" + filename, "description").Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status);
            Assert.IsTrue(msg.AsText() != string.Empty);

            p = Utils.BuildPlugForAdmin();
            PageUtils.DeletePageByID(p, id, true);
        }


        [Test]
        public void TestAttachmentDescriptionAssociations() {
            DreamMessage msg = null;
            Plug p = Utils.BuildPlugForAdmin();

            string pageId1, pageId2 = null;

            msg = PageUtils.CreateRandomPage(p, out pageId1);
            msg = PageUtils.CreateRandomPage(p, out pageId2);

            string description = null;
            string fileid = null;
            string filename = null;
            string propertyEtag = null;
            string propertyName = "urn:deki.mindtouch.com#description";

            //Create initial file rev
            FileUtils.UploadRandomFile(p, pageId1, null, null, out fileid, out filename);

            //set initial file description
            description = "Content r1 on p1";
            msg = p.At("files", fileid, "properties").WithHeader("Slug", XUri.Encode(propertyName)).PostAsync(DreamMessage.Ok(MimeType.TEXT_UTF8, description)).Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "POST property got non 200");
            Assert.AreEqual(msg.ToDocument()["/property/contents"].AsText, description, "Contents don't match!");
            propertyEtag = msg.ToDocument()["/property/@etag"].AsText;

            //Validate intitial file description
            msg = p.At("files", fileid, "info").GetAsync().Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "GET file info returned non 200:" + msg.ToString());
            Assert.AreEqual(description, msg.ToDocument()["description"].AsText, "Unexpected description");

            //update file description
            description = "Content r1 on p1 updated description 1";
            msg = p.At("files", fileid, "properties", XUri.DoubleEncode(propertyName)).WithHeader(DreamHeaders.ETAG, propertyEtag).PutAsync(DreamMessage.Ok(MimeType.TEXT_UTF8, description)).Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "PUT property got non 200");
            Assert.AreEqual(msg.ToDocument()["/property/contents"].AsText, description, "Contents don't match!");
            propertyEtag = msg.ToDocument()["/property/@etag"].AsText;

            //New file revision
            msg = p.At("pages", pageId1, "files", "=" + filename).PutAsync(DreamMessage.Ok(MimeType.ANY_TEXT, "Some content")).Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "File upload failed: " + msg.ToString());

            //Updated file description
            description = "Content r2 on p1";
            msg = p.At("files", fileid, "properties", XUri.DoubleEncode(propertyName)).WithHeader(DreamHeaders.ETAG, propertyEtag).PutAsync(DreamMessage.Ok(MimeType.TEXT_UTF8, description)).Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "PUT property got non 200");
            Assert.AreEqual(msg.ToDocument()["/property/contents"].AsText, description, "Contents don't match!");
            propertyEtag = msg.ToDocument()["/property/@etag"].AsText;

            //Move file
            msg = p.At("files", fileid, "move").With("to", pageId2).Post();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "File move failed:" + msg.ToString());

            //Update file description
            description = "Content r2 on p2";
            msg = p.At("files", fileid, "properties", XUri.DoubleEncode(propertyName)).WithHeader(DreamHeaders.ETAG, propertyEtag).PutAsync(DreamMessage.Ok(MimeType.TEXT_UTF8, description)).Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "PUT property got non 200");
            Assert.AreEqual(msg.ToDocument()["/property/contents"].AsText, description, "Contents don't match!");
            propertyEtag = msg.ToDocument()["/property/@etag"].AsText;

        }    
    }
}

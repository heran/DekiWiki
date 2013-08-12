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

using NUnit.Framework;

using MindTouch.Dream;
using MindTouch.Xml;

namespace MindTouch.Deki.Tests.PageTests {
    [TestFixture]
    public class CommentTests
    {
        [Test]
        public void CreateComments()
        {
            // POST:pages/{pageid}/comments 
            // http://developer.mindtouch.com/Deki/API_Reference/POST%3apages%2f%2f%7bpageid%7d%2f%2fcomments

            Plug p = Utils.BuildPlugForAdmin();

            string id = null;
            string path = null;
            DreamMessage msg = PageUtils.CreateRandomPage(p, out id, out path);

            string comment = Utils.GetSmallRandomText();
            DreamMessage postMsg = DreamMessage.Ok(MimeType.TEXT_UTF8, comment);
            msg = p.At("pages", id, "comments").Post(postMsg);
            Assert.AreEqual(DreamStatus.Ok, msg.Status);
            string commentId1 = msg.ToDocument()["@id"].AsText;
            Assert.IsFalse(string.IsNullOrEmpty(commentId1));
            Assert.AreEqual(comment, msg.ToDocument()["content"].AsText);

            comment = Utils.GetSmallRandomText();
            postMsg = DreamMessage.Ok(MimeType.TEXT_UTF8, comment);
            msg = p.At("pages", id, "comments").Post(postMsg);
            Assert.AreEqual(DreamStatus.Ok, msg.Status);
            string commentId2 = msg.ToDocument()["@id"].AsText;
            Assert.IsFalse(string.IsNullOrEmpty(commentId2));
            Assert.AreEqual(comment, msg.ToDocument()["content"].AsText);

            // GET:pages/{pageid}/comments 
            // http://developer.mindtouch.com/Deki/API_Reference/GET%3apages%2f%2f%7bpageid%7d%2f%2fcomments

            msg = p.At("pages", "=" + XUri.DoubleEncode(path), "comments").Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status);
            Assert.AreEqual(2, msg.ToDocument()["@count"].AsInt);
            Assert.IsFalse(msg.ToDocument()[string.Format("comment[@id=\"{0}\"]", commentId1)].IsEmpty);
            Assert.IsFalse(msg.ToDocument()[string.Format("comment[@id=\"{0}\"]", commentId2)].IsEmpty);

            PageUtils.DeletePageByID(p, id, true);
        }

        [Test]
        public void GetComment()
        {
            // GET:pages/{pageid}/comments/{commentnumber}/content
            // http://developer.mindtouch.com/Deki/API_Reference/GET%3apages%2f%2f%7bpageid%7d%2f%2fcomments%2f%2f%7bcommentnumber%7d%2f%2fcontent

            Plug p = Utils.BuildPlugForAdmin();

            string id = null;
            DreamMessage msg = PageUtils.CreateRandomPage(p, out id);

            string content = Utils.GetSmallRandomText();
            DreamMessage postMsg = DreamMessage.Ok(MimeType.TEXT_UTF8, content);
            msg = p.At("pages", id, "comments").Post(postMsg);
            Assert.AreEqual(DreamStatus.Ok, msg.Status);
            string commentId = msg.ToDocument()["@id"].AsText;
            Assert.IsFalse(string.IsNullOrEmpty(commentId));
            Assert.IsTrue(msg.ToDocument()["content"].AsText == content);

            msg = p.At("pages", id, "comments", "1", "content").Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status);
            Assert.IsTrue(msg.AsText() == content);

            // GET:pages/{pageid}/comments/{commentnumber}
            // http://developer.mindtouch.com/Deki/API_Reference/GET%3apages%2f%2f%7bpageid%7d%2f%2fcomments%2f%2f%7bcommentnumber%7d

            msg = p.At("pages", id, "comments", "1").Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status);
            Assert.IsTrue(msg.ToDocument()["@id"].AsText == commentId);
            Assert.IsTrue(msg.ToDocument()["content"].AsText == content);

            PageUtils.DeletePageByID(p, id, true);
        }

        [Test]
        public void EditComment()
        {
            // PUT:pages/{pageid}/comments/{commentnumber}/content
            // http://developer.mindtouch.com/Deki/API_Reference/PUT%3apages%2f%2f%7bpageid%7d%2f%2fcomments%2f%2f%7bcommentnumber%7d%2f%2fcontent

            Plug p = Utils.BuildPlugForAdmin();

            string id = null;
            string path = null;
            DreamMessage msg = PageUtils.CreateRandomPage(p, out id, out path);

            string comment = Utils.GetSmallRandomText();
            DreamMessage postMsg = DreamMessage.Ok(MimeType.TEXT_UTF8, comment);
            msg = p.At("pages", id, "comments").Post(postMsg);
            Assert.AreEqual(DreamStatus.Ok, msg.Status);
            string commentId1 = msg.ToDocument()["@id"].AsText;
            Assert.IsFalse(string.IsNullOrEmpty(commentId1));
            Assert.IsTrue(msg.ToDocument()["content"].AsText == comment);

            string newComment = Utils.GetSmallRandomText();
            msg = DreamMessage.Ok(MimeType.TEXT_UTF8, newComment);
            msg = p.At("pages", id, "comments", "1", "content").Put(msg);
            Assert.AreEqual(DreamStatus.Ok, msg.Status);
            Assert.IsTrue(msg.ToDocument()["@id"].AsText == commentId1);
            Assert.IsTrue(msg.ToDocument()["content"].AsText == newComment);

            msg = p.At("pages", "=" + XUri.DoubleEncode(path), "comments").Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status);
            Assert.IsTrue(msg.ToDocument()["@count"].AsInt == 1);
            Assert.IsFalse(msg.ToDocument()[string.Format("comment[@id=\"{0}\"]", commentId1)].IsEmpty);
            Assert.IsTrue(msg.ToDocument()["comment[1]/content"].AsText == newComment);

            PageUtils.DeletePageByID(p, id, true);
        }

        [Test]
        public void EditCommentPermissions()
        {
            // PUT:pages/{pageid}/comments/{commentnumber}/content
            // http://developer.mindtouch.com/Deki/API_Reference/PUT%3apages%2f%2f%7bpageid%7d%2f%2fcomments%2f%2f%7bcommentnumber%7d%2f%2fcontent

            Plug p = Utils.BuildPlugForAdmin();

            string id = null;
            string path = null;
            DreamMessage msg = PageUtils.CreateRandomPage(p, out id, out path);

            // Create Comment with Admin Plug
            string comment = Utils.GetSmallRandomText();
            DreamMessage postMsg = DreamMessage.Ok(MimeType.TEXT_UTF8, comment);
            msg = p.At("pages", id, "comments").Post(postMsg);
            Assert.AreEqual(DreamStatus.Ok, msg.Status);
            string commentId1 = msg.ToDocument()["@id"].AsText;
            Assert.IsFalse(string.IsNullOrEmpty(commentId1));
            Assert.IsTrue(msg.ToDocument()["content"].AsText == comment);
    
            // Create Anonymous PLug and try to edit comment
            string newComment = Utils.GetSmallRandomText();
            msg = DreamMessage.Ok(MimeType.TEXT_UTF8, newComment);
            try
            {
                Plug UserPlug = Utils.BuildPlugForAnonymous();
                msg = UserPlug.At("pages", id, "comments", "1", "content").Put(msg);
            }
            // Edit should fail
            catch (MindTouch.Dream.DreamResponseException ex)
            {
                Assert.IsTrue(ex.Response.Status == DreamStatus.Unauthorized, "Comment should not be overwritten by anonymous user!");
            }

            Utils.BuildPlugForAdmin();
            PageUtils.DeletePageByID(p, id, true);
        }
        
        [Test]
        public void DeleteComment()
        {
            // DELETE:pages/{pageid}/comments/{commentnumber}
            // http://developer.mindtouch.com/Deki/API_Reference/DELETE%3apages%2f%2f%7bpageid%7d%2f%2fcomments%2f%2f%7bcommentnumber%7d

            Plug p = Utils.BuildPlugForAdmin();

            string id = null;
            string path = null;
            DreamMessage msg = PageUtils.CreateRandomPage(p, out id, out path);

            string comment = Utils.GetSmallRandomText();
            DreamMessage postMsg = DreamMessage.Ok(MimeType.TEXT_UTF8, comment);
            msg = p.At("pages", id, "comments").Post(postMsg);
            Assert.AreEqual(DreamStatus.Ok, msg.Status);
            string commentId1 = msg.ToDocument()["@id"].AsText;
            Assert.IsFalse(string.IsNullOrEmpty(commentId1));
            Assert.IsTrue(msg.ToDocument()["content"].AsText == comment);

            msg = p.At("pages", id, "comments", "1").Delete();
            Assert.AreEqual(DreamStatus.Ok, msg.Status);

            msg = p.At("pages", "=" + XUri.DoubleEncode(path), "comments").Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status);
            Assert.IsTrue(msg.ToDocument()["@count"].AsInt == 0);

            PageUtils.DeletePageByID(p, id, true);
        }

        [Test]
        public void TestAnonymousCanPostComment() {
            // Bug http://youtrack.developer.mindtouch.com/issue/MT-9433

            Plug p = Utils.BuildPlugForAnonymous();

            DreamMessage msg = p.At("users", "current").Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status);
            Assert.IsTrue(msg.ToDocument()["permissions.user/operations"].AsText.Contains("READ"), "anon has no read access");

            msg = DreamMessage.Ok(MimeType.TEXT, "This is anonymous comment text");
            msg = p.At("pages", "home", "comments").PostAsync(msg).Wait();
            Assert.AreEqual(DreamStatus.Unauthorized, msg.Status);
        }

        [Test]
        public void CommentsForTreeOfPages()
        {
            //Assumptions:
            //Actions:
            //  Create tree of pages
            //  Add comment to every page
            //  Try to get all comments
            //Expected result: 
            //  All comments received

            Plug p = Utils.BuildPlugForAdmin();

            string baseTreePath = PageUtils.BuildPageTree(p);

            string commentForA = Utils.GetSmallRandomText();
            DreamMessage postMsg = DreamMessage.Ok(MimeType.TEXT_UTF8, commentForA);
            DreamMessage msg = p.At("pages", "=" + XUri.DoubleEncode(baseTreePath + "/A"), "comments").Post(postMsg);
            string commentForAId = msg.ToDocument()["@id"].AsText;

            string commentForB = Utils.GetSmallRandomText();
            postMsg = DreamMessage.Ok(MimeType.TEXT_UTF8, commentForB);
            msg = p.At("pages", "=" + XUri.DoubleEncode(baseTreePath + "/A/B"), "comments").Post(postMsg);
            string commentForBId = msg.ToDocument()["@id"].AsText;

            string commentForC = Utils.GetSmallRandomText();
            postMsg = DreamMessage.Ok(MimeType.TEXT_UTF8, commentForC);
            msg = p.At("pages", "=" + XUri.DoubleEncode(baseTreePath + "/A/B/C"), "comments").Post(postMsg);
            string commentForCId = msg.ToDocument()["@id"].AsText;

            string commentForD = Utils.GetSmallRandomText();
            postMsg = DreamMessage.Ok(MimeType.TEXT_UTF8, commentForD);
            msg = p.At("pages", "=" + XUri.DoubleEncode(baseTreePath + "/A/B/D"), "comments").Post(postMsg);
            string commentForDId = msg.ToDocument()["@id"].AsText;

            string commentForE1 = Utils.GetSmallRandomText();
            postMsg = DreamMessage.Ok(MimeType.TEXT_UTF8, commentForE1);
            msg = p.At("pages", "=" + XUri.DoubleEncode(baseTreePath + "/A/E"), "comments").Post(postMsg);
            string commentForE1Id = msg.ToDocument()["@id"].AsText;

            string commentForE2 = Utils.GetSmallRandomText();
            postMsg = DreamMessage.Ok(MimeType.TEXT_UTF8, commentForE2);
            msg = p.At("pages", "=" + XUri.DoubleEncode(baseTreePath + "/A/E"), "comments").Post(postMsg);
            string commentForE2Id = msg.ToDocument()["@id"].AsText;

            msg = p.At("pages", "=" + XUri.DoubleEncode(baseTreePath), "comments").With("depth", "infinity").Get();
            Assert.IsTrue(msg.ToDocument()["@count"].AsInt == 6);
            Assert.AreEqual(msg.ToDocument()[string.Format("comment[@id='{0}']/content", commentForAId)].AsText, commentForA);
            Assert.AreEqual(msg.ToDocument()[string.Format("comment[@id='{0}']/content", commentForBId)].AsText, commentForB);
            Assert.AreEqual(msg.ToDocument()[string.Format("comment[@id='{0}']/content", commentForCId)].AsText, commentForC);
            Assert.AreEqual(msg.ToDocument()[string.Format("comment[@id='{0}']/content", commentForDId)].AsText, commentForD);
            Assert.AreEqual(msg.ToDocument()[string.Format("comment[@id='{0}']/content", commentForE1Id)].AsText, commentForE1);
            Assert.AreEqual(msg.ToDocument()[string.Format("comment[@id='{0}']/content", commentForE2Id)].AsText, commentForE2);
        }

        [Test]
        public void CommentsForTreeOfPagesWithSecurity()
        {
            //Assumptions:
            //Actions:
            //  Create tree of pages
            //  Add comment to every page
            //  Set private restrictions for E page
            //  Try to get comments from user which doesn't have rights for E page
            //Expected result: 
            //  All comments received except comments for E 

            Plug p = Utils.BuildPlugForAdmin();

            string baseTreePath = PageUtils.BuildPageTree(p);

            string commentForA = Utils.GetSmallRandomText();
            DreamMessage postMsg = DreamMessage.Ok(MimeType.TEXT_UTF8, commentForA);
            DreamMessage msg = p.At("pages", "=" + XUri.DoubleEncode(baseTreePath + "/A"), "comments").Post(postMsg);
            string commentForAId = msg.ToDocument()["@id"].AsText;

            string commentForB = Utils.GetSmallRandomText();
            postMsg = DreamMessage.Ok(MimeType.TEXT_UTF8, commentForB);
            msg = p.At("pages", "=" + XUri.DoubleEncode(baseTreePath + "/A/B"), "comments").Post(postMsg);
            string commentForBId = msg.ToDocument()["@id"].AsText;

            string commentForC = Utils.GetSmallRandomText();
            postMsg = DreamMessage.Ok(MimeType.TEXT_UTF8, commentForC);
            msg = p.At("pages", "=" + XUri.DoubleEncode(baseTreePath + "/A/B/C"), "comments").Post(postMsg);
            string commentForCId = msg.ToDocument()["@id"].AsText;

            string commentForD = Utils.GetSmallRandomText();
            postMsg = DreamMessage.Ok(MimeType.TEXT_UTF8, commentForD);
            msg = p.At("pages", "=" + XUri.DoubleEncode(baseTreePath + "/A/B/D"), "comments").Post(postMsg);
            string commentForDId = msg.ToDocument()["@id"].AsText;

            string commentForE1 = Utils.GetSmallRandomText();
            postMsg = DreamMessage.Ok(MimeType.TEXT_UTF8, commentForE1);
            msg = p.At("pages", "=" + XUri.DoubleEncode(baseTreePath + "/A/E"), "comments").Post(postMsg);
            string commentForE1Id = msg.ToDocument()["@id"].AsText;

            string commentForE2 = Utils.GetSmallRandomText();
            postMsg = DreamMessage.Ok(MimeType.TEXT_UTF8, commentForE2);
            msg = p.At("pages", "=" + XUri.DoubleEncode(baseTreePath + "/A/E"), "comments").Post(postMsg);
            string commentForE2Id = msg.ToDocument()["@id"].AsText;

            XDoc securityDoc = new XDoc("security")
                .Start("permissions.page")
                .Elem("restriction", "Private")
                .End();

            msg = p.At("pages", "=" + XUri.DoubleEncode(baseTreePath + "/A/E"), "security").
                WithQuery("cascade=none").Put(securityDoc);

            string userid = null;
            string username = null;
            msg = UserUtils.CreateRandomContributor(p, out userid, out username);

            p = Utils.BuildPlugForUser(username);

            msg = p.At("pages", "=" + XUri.DoubleEncode(baseTreePath), "comments").With("depth", "infinity").Get();
            Assert.IsTrue(msg.ToDocument()["@count"].AsInt == 4);
            Assert.AreEqual(msg.ToDocument()[string.Format("comment[@id='{0}']/content", commentForAId)].AsText, commentForA);
            Assert.AreEqual(msg.ToDocument()[string.Format("comment[@id='{0}']/content", commentForBId)].AsText, commentForB);
            Assert.AreEqual(msg.ToDocument()[string.Format("comment[@id='{0}']/content", commentForCId)].AsText, commentForC);
            Assert.AreEqual(msg.ToDocument()[string.Format("comment[@id='{0}']/content", commentForDId)].AsText, commentForD);
            Assert.IsTrue(msg.ToDocument()[string.Format("comment[@id='{0}']", commentForE1Id)].IsEmpty);
            Assert.IsTrue(msg.ToDocument()[string.Format("comment[@id='{0}']", commentForE2Id)].IsEmpty);
        }

        [Test]
        public void GetCommentsWithFilter()
        {
            Plug p = Utils.BuildPlugForAdmin();

            string id = null;
            string path = null;
            DreamMessage msg = PageUtils.CreateRandomPage(p, out id, out path);

            string comment = Utils.GetSmallRandomText();
            DreamMessage postMsg = DreamMessage.Ok(MimeType.TEXT_UTF8, comment);
            msg = p.At("pages", id, "comments").Post(postMsg);
            Assert.AreEqual(DreamStatus.Ok, msg.Status);
            string commentId1 = msg.ToDocument()["@id"].AsText;
            Assert.IsFalse(string.IsNullOrEmpty(commentId1));
            Assert.AreEqual(comment, msg.ToDocument()["content"].AsText);

            comment = Utils.GetSmallRandomText();
            postMsg = DreamMessage.Ok(MimeType.TEXT_UTF8, comment);
            msg = p.At("pages", id, "comments").Post(postMsg);
            Assert.AreEqual(DreamStatus.Ok, msg.Status);
            string commentId2 = msg.ToDocument()["@id"].AsText;
            Assert.IsFalse(string.IsNullOrEmpty(commentId2));
            Assert.AreEqual(comment, msg.ToDocument()["content"].AsText);

            string username = null;
            string userid = null;
            msg = UserUtils.CreateRandomContributor(p, out userid, out username);

            p = Utils.BuildPlugForUser(username);

            comment = Utils.GetSmallRandomText();
            postMsg = DreamMessage.Ok(MimeType.TEXT_UTF8, comment);
            msg = p.At("pages", id, "comments").Post(postMsg);
            Assert.AreEqual(DreamStatus.Ok, msg.Status);
            string commentId3 = msg.ToDocument()["@id"].AsText;
            Assert.IsFalse(string.IsNullOrEmpty(commentId3));
            Assert.AreEqual(comment, msg.ToDocument()["content"].AsText);

            msg = p.At("pages", "=" + XUri.DoubleEncode(path), "comments").
                With("postedbyuserid", userid).Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status);
            Assert.AreEqual(1, msg.ToDocument()["@count"].AsInt);
            Assert.IsFalse(msg.ToDocument()[string.Format("comment[@id=\"{0}\"]", commentId3)].IsEmpty);
            Assert.IsTrue(msg.ToDocument()[string.Format("comment[@id=\"{0}\"]", commentId1)].IsEmpty);
            Assert.IsTrue(msg.ToDocument()[string.Format("comment[@id=\"{0}\"]", commentId2)].IsEmpty);

            p = Utils.BuildPlugForAdmin();

            msg = p.At("pages", "=" + XUri.DoubleEncode(path), "comments").
                With("postedbyuserid", UserUtils.GetCurrentUserID(p)).Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status);
            Assert.AreEqual(2, msg.ToDocument()["@count"].AsInt);
            Assert.IsFalse(msg.ToDocument()[string.Format("comment[@id=\"{0}\"]", commentId1)].IsEmpty);
            Assert.IsFalse(msg.ToDocument()[string.Format("comment[@id=\"{0}\"]", commentId2)].IsEmpty);
            Assert.IsTrue(msg.ToDocument()[string.Format("comment[@id=\"{0}\"]", commentId3)].IsEmpty);

            PageUtils.DeletePageByID(p, id, true);
        }
    }
}
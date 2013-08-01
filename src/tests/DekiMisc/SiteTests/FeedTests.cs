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
using System.Text.RegularExpressions;
using MindTouch.Tasking;
using NUnit.Framework;

using MindTouch.Dream.Test;
using MindTouch.Dream;
using MindTouch.Xml;

namespace MindTouch.Deki.Tests.SiteTests
{
    [TestFixture]
    public class FeedTests
    {
        string USERNAME; // aka ADMIN

        enum file_ops { UPLOAD, RENAME, MOVE, DELETE };
        enum tag_ops { ADD, REMOVE };
        enum sec_ops { SET_PRIVATE, ADD_GRANT, REMOVE_GRANT, SET_PUBLIC };

        [TestFixtureSetUp]
        public void setup()
        {
            USERNAME = Utils.Settings.UserName;
        }

        /// <summary>
        ///     Retrieve all feeds
        /// </summary>        
        /// <feature>
        /// <name>GET:/site/feed</name>
        /// <uri>http://developer.mindtouch.com/en/ref/MindTouch_API/GET%3asite%2f%2ffeed</uri>
        /// </feature>
        /// <feature>
        /// <name>GET:/site/feed/new</name>
        /// <uri>http://developer.mindtouch.com/en/ref/MindTouch_API/GET%3asite%2f%2ffeed%2f%2fnew</uri>
        /// </feature>
        /// <feature>
        /// <name>GET:/pages/{pageid}/feed</name>
        /// <uri>http://developer.mindtouch.com/en/ref/MindTouch_API/GET%3apages%2f%2f%7Bpageid%7D%2f%2ffeed</uri>
        /// </feature>
        /// <feature>
        /// <name>GET:/pages/{pageid}/feed/new</name>
        /// <uri>http://developer.mindtouch.com/en/ref/MindTouch_API/GET%3apages%2f%2f%7Bpageid%7D%2f%2ffeed%2f%2fnew</uri>
        /// </feature>
        /// <feature>
        /// <name>GET:/users/{userid}/feed</name>
        /// <uri>http://developer.mindtouch.com/en/ref/MindTouch_API/GET%3ausers%2f%2f%7Buserid%7D%2f%2ffeed</uri>
        /// </feature>
        /// <feature>
        /// <name>GET:users/{userid}/favorites/feed</name>
        /// <uri>http://developer.mindtouch.com/en/ref/MindTouch_API/GET%3ausers%2f%2f%7Buserid%7D%2f%2ffavorites%2f%2ffeed</uri>
        /// </feature>
        /// <expected>All return 200 OK HTTP response</expected>

        [Ignore("trunk.mindtouch.com does not agree with this test")]
        [Test]
        public void AllFeeds_Get_Ok()
        {
            // Log in as ADMIN
            Plug p = Utils.BuildPlugForAdmin();

            // Retrieve all feeds and verify OK status returned
            DreamMessage msg = p.At("pages", "=" + XUri.DoubleEncode("User:" + USERNAME), "feed").GetAsync().Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Failed to retrieve /pages/{pageid}/feed");

            msg = p.At("pages", "=" + XUri.DoubleEncode("User:" + USERNAME), "feed", "new").GetAsync().Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Failed to retrieve //pages/{pageid}/feed/new");

            msg = p.At("site", "feed").GetAsync().Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Failed to retrieve /site/feed");

            msg = p.At("site", "feed", "new").GetAsync().Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Failed to retrieve /site/feed/new");

            msg = p.At("users", "=" + XUri.DoubleEncode(USERNAME), "feed").GetAsync().Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Failed to retrieve /users/{userid}/feed");

            msg = p.At("users", "=" + XUri.DoubleEncode(USERNAME), "favorites", "feed").GetAsync().Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Failed to retrieve /users/{userid}/favorites/feed");
        }

        /// <summary>
        ///    Create a page and verify feed data
        /// </summary>        
        /// <feature>
        /// <name>GET:/site/feed</name>
        /// <uri>http://developer.mindtouch.com/en/ref/MindTouch_API/GET%3asite%2f%2ffeed</uri>
        /// </feature>
        /// <expected>Feed data is correct</expected>
        [Test]
        public void SiteFeed_CreatePage_CorrectFeedData()
        {
            // Log in as ADMIN
            Plug p = Utils.BuildPlugForAdmin();

            // Create a page
            string path = PageUtils.GenerateUniquePageName();
            string contents = "Test feed contents";
            DreamMessage msg = p.At("pages", "=" + XUri.DoubleEncode(path), "contents")
                .PostAsync(DreamMessage.Ok(MimeType.TEXT_UTF8, contents)).Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Page creation failed!");
            int pageid = msg.ToDocument()["page/@id"].AsInt ?? 0;

            // Retrieve most recent created feed entry
            msg = p.At("site", "feed").With("format", "rawdaily").With("limit", 1).GetAsync().Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Feed retrieval failed!");

            // Run checks
            rc_id_check(msg);
            rc_comment_check(msg, "page created, 3 words added");
            rc_cur_id_check(msg, pageid);
            rc_last_oldid_check(msg, 0);
            rc_this_oldid_check(msg, 0);
            rc_namespace_check(msg, 0);
            rc_timestamp_check(msg);
            rc_title_check(msg, path);
            rc_type_check(msg, 1); // NEW = 1
            rc_moved_to_ns_check(msg, 0);
            rc_moved_to_title_check(msg, String.Empty);
            rc_user_name_check(msg, USERNAME);
            rc_full_name_check(msg, String.Empty);
            rc_page_exists_check(msg, 1);
            rc_revision_check(msg, 1);
            cmnt_deleted_check(msg, 0);
            //edit_count_check(msg, 1);
            rc_prev_revision_check(msg, 0);
            rc_summary_check(msg, "Edited once by " + USERNAME);
        }

        /// <summary>
        ///    Edit a page and verify feed data
        /// </summary>        
        /// <feature>
        /// <name>GET:/site/feed</name>
        /// <uri>http://developer.mindtouch.com/en/ref/MindTouch_API/GET%3asite%2f%2ffeed</uri>
        /// </feature>
        /// <expected>Feed data is correct</expected>

        [Test]
        public void SiteFeed_EditPage_CorrectFeedData()
        {
            // Log in as ADMIN
            Plug p = Utils.BuildPlugForAdmin();

            // Create a page
            string id;
            string path;
            PageUtils.CreateRandomPage(p, out id, out path);

            // Edit page
            string contents = "new contents";
            DreamMessage msg = p.At("pages", id, "contents").With("edittime", DateTime.Now.ToString("yyyyMMddHHmmss"))
                .PostAsync(DreamMessage.Ok(MimeType.TEXT_UTF8, contents)).Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Page edit failed!");

            // Retrieve most recent created feed entry
            msg = p.At("site", "feed").With("format", "rawdaily").With("limit", 1).GetAsync().Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Feed retrieval failed!");

            int pageid = System.Convert.ToInt32(id);

            rc_id_check(msg);
            // rc_comment_check(msg, "page created, 3 words added");
            rc_cur_id_check(msg, pageid);
            // rc_last_oldid_check(msg, 0);
            rc_this_oldid_check(msg, 0);
            rc_namespace_check(msg, 0);
            rc_timestamp_check(msg);
            rc_title_check(msg, path);
            rc_type_check(msg, 0); // EDIT = 0
            rc_moved_to_ns_check(msg, 0);
            rc_moved_to_title_check(msg, String.Empty);
            rc_user_name_check(msg, USERNAME);
            rc_full_name_check(msg, String.Empty);
            rc_page_exists_check(msg, 1);
            rc_revision_check(msg, 2);
            cmnt_deleted_check(msg, 0);
            //edit_count_check(msg, 1);
            rc_prev_revision_check(msg, 1);
            rc_summary_check(msg, "Edited once by " + USERNAME);
        }

        /// <summary>
        ///    Move a page and verify feed data
        /// </summary>        
        /// <feature>
        /// <name>GET:/site/feed</name>
        /// <uri>http://developer.mindtouch.com/en/ref/MindTouch_API/GET%3asite%2f%2ffeed</uri>
        /// </feature>
        /// <expected>Feed data is correct</expected>

        [Test]
        public void SiteFeed_MovePage_CorrectFeedData()
        {
            // Log in as ADMIN
            Plug p = Utils.BuildPlugForAdmin();

            // Create a page
            string id;
            string path;
            PageUtils.CreateRandomPage(p, out id, out path);

            string postfix = "-" + DateTime.Now.Ticks.ToString();
            string newpath = path.Substring(0, path.LastIndexOf("/") + 1) + "zzz" + postfix;

            // Move page
            DreamMessage msg = p.At("pages", id, "move").With("name", "zzz" + postfix).PostAsync().Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Page move failed!");

            // Wait for move feed entry to bubble
            Assert.IsTrue(Wait.For(() =>
            {
                msg = p.At("site", "feed").With("format", "rawdaily").With("limit", 1).GetAsync().Wait();
                return((msg.ToDocument()["change/rc_comment"].AsText ?? String.Empty).Contains(path));
            },
                TimeSpan.FromSeconds(10)),
                "Feed for page move could not be retrieved");

            int pageid = System.Convert.ToInt32(id);

            rc_id_check(msg);
            rc_comment_check(msg, path + " moved to " + newpath);
            rc_cur_id_check(msg, pageid);
            rc_last_oldid_check(msg, 0);
            rc_this_oldid_check(msg, 0);
            rc_namespace_check(msg, 0);
            rc_timestamp_check(msg);
            rc_title_check(msg, path);
            rc_type_check(msg, 2); // MOVE = 2
            rc_moved_to_ns_check(msg, 0);
            rc_moved_to_title_check(msg, newpath);
            rc_user_name_check(msg, USERNAME);
            rc_full_name_check(msg, String.Empty);
            rc_page_exists_check(msg, 1);
            rc_revision_check(msg, 1);
            cmnt_deleted_check(msg, 0);
            //edit_count_check(msg, 0);
            //rc_prev_revision_check(msg, 1);
            //rc_summary_check(msg, "Edited once by " + USERNAME);
        }

        /// <summary>
        ///    Delete a page and verify feed data
        /// </summary>        
        /// <feature>
        /// <name>GET:/site/feed</name>
        /// <uri>http://developer.mindtouch.com/en/ref/MindTouch_API/GET%3asite%2f%2ffeed</uri>
        /// </feature>
        /// <expected>Feed data is correct</expected>

        [Test]
        public void SiteFeed_DeletePage_CorrectFeedData()
        {
            // Log in as ADMIN
            Plug p = Utils.BuildPlugForAdmin();

            // Create a page
            string id;
            string path;
            PageUtils.CreateRandomPage(p, out id, out path);

            string newpath = path.Substring(0, path.LastIndexOf("/") + 1) + "zzz";

            // Delete the page
            DreamMessage msg = PageUtils.DeletePageByID(p, id, true);

            // Retrieve feed entry

            msg = p.At("site", "feed").With("format", "rawdaily").With("limit", 1).GetAsync().Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Feed retrieval failed!");

            // Retrieve timestamp

            DreamMessage tsmsg = p.At("archive", "pages", id).GetAsync().Wait();
            Assert.AreEqual(DreamStatus.Ok, tsmsg.Status, "Archived page retrieval failed");
            string timestamp = tsmsg.ToDocument()["date.deleted"].AsText ?? String.Empty;
            timestamp = Regex.Replace(timestamp, "[^0-9]", "");

            // Run checks

            int pageid = System.Convert.ToInt32(id);

            rc_id_check(msg);
            rc_comment_check(msg, "deleted \"" + path + "\"");
            rc_cur_id_check(msg, pageid);
            rc_last_oldid_check(msg, 0);
            rc_this_oldid_check(msg, 0);
            rc_namespace_check(msg, 0);
            rc_timestamp_check(msg);
            rc_title_check(msg, path);
            rc_type_check(msg, 5); // DELETE = 5
            rc_moved_to_ns_check(msg, 0);
            rc_moved_to_title_check(msg, String.Empty);
            rc_user_name_check(msg, USERNAME);
            rc_full_name_check(msg, String.Empty);
            rc_page_exists_check(msg, 0);
            rc_revision_check(msg, 0);
            cmnt_deleted_check(msg, 0);
            //edit_count_check(msg, 0);
            //rc_prev_revision_check(msg, 1);
            //rc_summary_check(msg, "Edited once by " + USERNAME);
        }

        /// <summary>
        ///    Restore a page and verify feed data
        /// </summary>        
        /// <feature>
        /// <name>GET:/site/feed</name>
        /// <uri>http://developer.mindtouch.com/en/ref/MindTouch_API/GET%3asite%2f%2ffeed</uri>
        /// </feature>
        /// <expected>Feed data is correct</expected>

        [Test]
        public void SiteFeed_RestorePage_CorrectFeedData()
        {
            // Log in as ADMIN
            Plug p = Utils.BuildPlugForAdmin();

            // Create a page
            string id;
            string path;
            PageUtils.CreateRandomPage(p, out id, out path);

            string newpath = path.Substring(0, path.LastIndexOf("/") + 1) + "zzz";

            // Delete the page
            DreamMessage msg = PageUtils.DeletePageByID(p, id, true);

            // Restore the page
            msg = p.At("archive", "pages", id, "restore").Post(new Result<DreamMessage>()).Wait();

            // Retrieve feed entry

            msg = p.At("site", "feed").With("format", "rawdaily").With("limit", 1).GetAsync().Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Feed retrieval failed!");

            // Run checks

            int pageid = System.Convert.ToInt32(id);

            rc_id_check(msg);
            rc_comment_check(msg, "restored \"" + path + "\"");
            rc_cur_id_check(msg, pageid);
            rc_last_oldid_check(msg, 0);
            rc_this_oldid_check(msg, 0);
            rc_namespace_check(msg, 0);
            rc_timestamp_check(msg);
            rc_title_check(msg, path);
            rc_type_check(msg, 6); // RESTORE = 6
            rc_moved_to_ns_check(msg, 0);
            rc_moved_to_title_check(msg, String.Empty);
            rc_user_name_check(msg, USERNAME);
            rc_full_name_check(msg, String.Empty);
            rc_page_exists_check(msg, 1);
            rc_revision_check(msg, 1);
            cmnt_deleted_check(msg, 0);
            //edit_count_check(msg, 0);
            //rc_prev_revision_check(msg, 1);
            //rc_summary_check(msg, "Edited once by " + USERNAME);
        }

        /// <summary>
        ///    Post a comment to page and verify feed data
        /// </summary>        
        /// <feature>
        /// <name>GET:/site/feed</name>
        /// <uri>http://developer.mindtouch.com/en/ref/MindTouch_API/GET%3asite%2f%2ffeed</uri>
        /// </feature>
        /// <expected>Feed data is correct</expected>

        [Test]
        public void SiteFeed_CreateComment_CorrectFeedData()
        {
            // Log in as ADMIN
            Plug p = Utils.BuildPlugForAdmin();

            // Create a page
            string id;
            string path;
            PageUtils.CreateRandomPage(p, out id, out path);

            // Post comment to page
            string comment = "this is a comment";
            DreamMessage msg = p.At("pages", id, "comments")
                .Post(DreamMessage.Ok(MimeType.TEXT_UTF8, comment), new Result<DreamMessage>()).Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Posting comment failed!");

            int comment_id = msg.ToDocument()["@id"].AsInt ?? 0;
            int comment_number = msg.ToDocument()["number"].AsInt ?? 0;
            string comment_content_type = msg.ToDocument()["content/@type"].AsText ?? String.Empty;
            string timestamp = msg.ToDocument()["date.posted"].AsText ?? String.Empty;
            timestamp = Regex.Replace(timestamp, "[^0-9]", "");

            // Retrieve feed entry

            msg = p.At("site", "feed").With("format", "rawdaily").With("limit", 1).GetAsync().Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Feed retrieval failed!");

            // Run checks

            int pageid = System.Convert.ToInt32(id);

            rc_id_check(msg);
            rc_comment_check(msg, "comment #1 added");
            rc_cur_id_check(msg, pageid);
            rc_last_oldid_check(msg, 0);
            rc_this_oldid_check(msg, 0);
            rc_namespace_check(msg, 0);
            rc_timestamp_check(msg);
            rc_title_check(msg, path);
            rc_type_check(msg, 40); // COMMENT_CREATION = 40
            rc_moved_to_ns_check(msg, 0);
            rc_moved_to_title_check(msg, String.Empty);
            rc_user_name_check(msg, USERNAME);
            rc_full_name_check(msg, String.Empty);
            rc_page_exists_check(msg, 1);
            rc_revision_check(msg, 0);
            cmnt_id_check(msg, comment_id);
            cmnt_number_check(msg, comment_number);
            cmnt_content_check(msg, comment);
            cmnt_content_mimetype_check(msg, comment_content_type);
            cmnt_deleted_check(msg, 0);
            edit_count_check(msg, 1);
            rc_summary_check(msg, "Edited once by " + USERNAME);
        }

        /// <summary>
        ///    Update a comment and verify feed data
        /// </summary>        
        /// <feature>
        /// <name>GET:/site/feed</name>
        /// <uri>http://developer.mindtouch.com/en/ref/MindTouch_API/GET%3asite%2f%2ffeed</uri>
        /// </feature>
        /// <expected>Feed data is correct</expected>

        [Test]
        public void SiteFeed_UpdateComment_CorrectFeedData()
        {
            // Log in as ADMIN
            Plug p = Utils.BuildPlugForAdmin();

            // Create a page
            string id;
            string path;
            PageUtils.CreateRandomPage(p, out id, out path);

            // Post comment to page
            string comment = "this is a comment";
            DreamMessage msg = p.At("pages", id, "comments")
                .Post(DreamMessage.Ok(MimeType.TEXT_UTF8, comment), new Result<DreamMessage>()).Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Posting comment failed!");

            int comment_number = msg.ToDocument()["number"].AsInt ?? 0;
            
            // Update comment
            comment = "brand new comment";
            msg = p.At("pages", id, "comments", comment_number.ToString(), "content")
                .Put(DreamMessage.Ok(MimeType.TEXT_UTF8, comment), new Result<DreamMessage>()).Wait();

            int comment_id = msg.ToDocument()["@id"].AsInt ?? 0;
            string comment_content_type = msg.ToDocument()["content/@type"].AsText ?? String.Empty;
            string timestamp = msg.ToDocument()["date.posted"].AsText ?? String.Empty;
            timestamp = Regex.Replace(timestamp, "[^0-9]", "");

            // Retrieve feed entry
            msg = p.At("site", "feed").With("format", "rawdaily").With("limit", 1).GetAsync().Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Feed retrieval failed!");

            // Run checks

            int pageid = System.Convert.ToInt32(id);

            rc_id_check(msg);
            rc_comment_check(msg, "comment #" + comment_number.ToString() + " edited");
            rc_cur_id_check(msg, pageid);
            rc_last_oldid_check(msg, 0);
            rc_this_oldid_check(msg, 0);
            rc_namespace_check(msg, 0);
            rc_timestamp_check(msg);
            rc_title_check(msg, path);
            rc_type_check(msg, 41); // COMMENT_UPDATED = 41
            rc_moved_to_ns_check(msg, 0);
            rc_moved_to_title_check(msg, String.Empty);
            rc_user_name_check(msg, USERNAME);
            rc_full_name_check(msg, String.Empty);
            rc_page_exists_check(msg, 1);
            rc_revision_check(msg, 0);
            cmnt_id_check(msg, comment_id);
            cmnt_number_check(msg, comment_number);
            cmnt_content_check(msg, comment);
            cmnt_content_mimetype_check(msg, comment_content_type);
            cmnt_deleted_check(msg, 0);
            edit_count_check(msg, 1);
            rc_summary_check(msg, "Edited once by " + USERNAME);
        }

        /// <summary>
        ///    Delete a comment and verify feed data
        /// </summary>        
        /// <feature>
        /// <name>GET:/site/feed</name>
        /// <uri>http://developer.mindtouch.com/en/ref/MindTouch_API/GET%3asite%2f%2ffeed</uri>
        /// </feature>
        /// <expected>Feed data is correct</expected>

        [Test]
        public void SiteFeed_DeleteComment_CorrectFeedData()
        {
            // Log in as ADMIN
            Plug p = Utils.BuildPlugForAdmin();

            // Create a page
            string id;
            string path;
            PageUtils.CreateRandomPage(p, out id, out path);

            // Post comment to page
            string comment = "this is a comment";
            DreamMessage msg = p.At("pages", id, "comments")
                .Post(DreamMessage.Ok(MimeType.TEXT_UTF8, comment), new Result<DreamMessage>()).Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Posting comment failed!");

            int comment_number = msg.ToDocument()["number"].AsInt ?? 0;
            int comment_id = msg.ToDocument()["@id"].AsInt ?? 0;
            string comment_content_type = msg.ToDocument()["content/@type"].AsText ?? String.Empty;
            string timestamp = msg.ToDocument()["date.posted"].AsText ?? String.Empty;
            timestamp = Regex.Replace(timestamp, "[^0-9]", "");

            // Delete comment
            msg = p.At("pages", id, "comments", comment_number.ToString())
                .Delete(new Result<DreamMessage>()).Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Comment delete failed!");

            // Retrieve feed entry
            msg = p.At("site", "feed").With("format", "rawdaily").With("limit", 1).GetAsync().Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Feed retrieval failed!");

            // Run checks

            int pageid = System.Convert.ToInt32(id);

            rc_id_check(msg);
            rc_comment_check(msg, "comment #" + comment_number.ToString() + " deleted");
            rc_cur_id_check(msg, pageid);
            rc_last_oldid_check(msg, 0);
            rc_this_oldid_check(msg, 0);
            rc_namespace_check(msg, 0);
            rc_timestamp_check(msg);
            rc_title_check(msg, path);
            rc_type_check(msg, 42); // COMMENT_DELETED = 42
            rc_moved_to_ns_check(msg, 0);
            rc_moved_to_title_check(msg, String.Empty);
            rc_user_name_check(msg, USERNAME);
            rc_full_name_check(msg, String.Empty);
            rc_page_exists_check(msg, 1);
            rc_revision_check(msg, 0);
            cmnt_id_check(msg, comment_id);
            cmnt_number_check(msg, comment_number);
            cmnt_content_check(msg, comment);
            cmnt_content_mimetype_check(msg, comment_content_type);
            cmnt_deleted_check(msg, 1);
            old_is_hidden_check(msg, false);
            edit_count_check(msg, 1);
            rc_summary_check(msg, "Edited once by " + USERNAME);
        }

        /// <summary>
        ///    Upload, rename, move, and delete a file; and verify feed data for all cases
        /// </summary>        
        /// <feature>
        /// <name>GET:/site/feed</name>
        /// <uri>http://developer.mindtouch.com/en/ref/MindTouch_API/GET%3asite%2f%2ffeed</uri>
        /// </feature>
        /// <expected>Feed data is correct</expected>

        [Test]
        public void SiteFeed_UploadRenameMoveDeleteFile_CorrectFeedData()
        {
            // Log in as ADMIN
            Plug p = Utils.BuildPlugForAdmin();
            DreamMessage msg;

            // Create a page
            string id;
            string path;
            PageUtils.CreateRandomPage(p, out id, out path);
            
            // Create a page to move file to
            string toid;
            string topath;
            PageUtils.CreateRandomPage(p, out toid, out topath);

            string fileid = String.Empty;
            string filename = String.Empty;
            string tofilename = String.Empty;
            int pageid = 0;
            string comment = String.Empty;
            string title = String.Empty;

            for (int i = 0; i < (int)file_ops.DELETE; i++)
            {
                switch (i)
                {
                    case (int)file_ops.UPLOAD:
                        // Upload file to page
                        FileUtils.UploadRandomFile(p, id, out fileid, out filename);
                        comment = "added '" + filename + "'";
                        pageid = System.Convert.ToInt32(id);
                        title = path;
                        break;

                    case (int)file_ops.RENAME:
                        // Rename file
                        string postfix = "-" + DateTime.Now.Ticks.ToString();
                        tofilename = filename + postfix;
                        msg = p.At("files", fileid, "move").With("name", filename + postfix)
                            .Post(new Result<DreamMessage>()).Wait();
                        Assert.AreEqual(DreamStatus.Ok, msg.Status, "File rename failed!");
                        comment = "renamed file from '" + filename + "' to '" + tofilename + "'";
                        pageid = System.Convert.ToInt32(id);
                        title = path;
                        break;

                    case (int)file_ops.MOVE:
                        // Move file
                        msg = p.At("files", fileid, "move").With("to", toid)
                            .Post(new Result<DreamMessage>()).Wait();
                        Assert.AreEqual(DreamStatus.Ok, msg.Status, "File move failed!");
                        comment = "moved file '" + tofilename + "' from " + path;
                        pageid = System.Convert.ToInt32(toid);
                        title = topath;
                        break;

                    case (int)file_ops.DELETE:
                        // Delete file
                        msg = p.At("files", fileid).Delete(new Result<DreamMessage>()).Wait();
                        Assert.AreEqual(DreamStatus.Ok, msg.Status, "File delete failed!");
                        comment = "removed '" + tofilename + "'";
                        pageid = System.Convert.ToInt32(toid);
                        title = topath;
                        break;

                    default:
                        break;
                }

                // Retrieve feed entry
                msg = p.At("site", "feed").With("format", "rawdaily").With("limit", 1).GetAsync().Wait();
                Assert.AreEqual(DreamStatus.Ok, msg.Status, "Feed retrieval failed!");

                // Run checks

                rc_id_check(msg);
                rc_comment_check(msg, comment);
                rc_cur_id_check(msg, pageid);
                rc_last_oldid_check(msg, 0);
                rc_this_oldid_check(msg, 0);
                rc_namespace_check(msg, 0);
                rc_timestamp_check(msg);
                rc_title_check(msg, title);
                rc_type_check(msg, 50); // FILE = 50
                rc_moved_to_ns_check(msg, 0);
                rc_moved_to_title_check(msg, String.Empty);
                rc_user_name_check(msg, USERNAME);
                rc_full_name_check(msg, String.Empty);
                rc_page_exists_check(msg, 1);
                rc_revision_check(msg, 1);
                cmnt_deleted_check(msg, 0);
                old_is_hidden_check(msg, false);
                rc_prev_revision_check(msg, 1);
                rc_summary_check(msg, "Edited once by " + USERNAME);
            }
        }

        /// <summary>
        ///    Add and remove a tag; and verify feed data for both cases
        /// </summary>        
        /// <feature>
        /// <name>GET:/site/feed</name>
        /// <uri>http://developer.mindtouch.com/en/ref/MindTouch_API/GET%3asite%2f%2ffeed</uri>
        /// </feature>
        /// <expected>Feed data is correct</expected>

        [Test]
        public void SiteFeed_AddRemoveTags_CorrectFeedData()
        {
            // Log in as ADMIN
            Plug p = Utils.BuildPlugForAdmin();
            DreamMessage msg;

            // Create a page
            string id;
            string path;
            PageUtils.CreateRandomPage(p, out id, out path);

            string comment = String.Empty;
            string tag = "this is a tag";

            for (int i = 0; i < (int)tag_ops.REMOVE; i++)
            {
                switch (i)
                {
                    case (int)tag_ops.ADD:
                        // Add a tag
                        XDoc tagXML = new XDoc("tags")
                                        .Start("tag").Attr("value", tag).End();
                        msg = p.At("pages", id, "tags").Put(tagXML, new Result<DreamMessage>()).Wait();
                        comment = "Added tags: " + tag + ".";
                        break;

                    case (int)tag_ops.REMOVE:
                        // Remove the tag (<tags/> XML doc)
                        tagXML = new XDoc("tags");
                        msg = p.At("pages", id, "tags").Put(tagXML, new Result<DreamMessage>()).Wait();
                        comment = "Removed tags: " + tag + ".";
                        break;

                    default:
                        break;
                }

                // Retrieve feed entry
                msg = p.At("site", "feed").With("format", "rawdaily").With("limit", 1).GetAsync().Wait();
                Assert.AreEqual(DreamStatus.Ok, msg.Status, "Feed retrieval failed!");

                // Run checks

                int pageid = System.Convert.ToInt32(id);

                rc_id_check(msg);
                rc_comment_check(msg, comment);
                rc_cur_id_check(msg, pageid);
                rc_last_oldid_check(msg, 0);
                rc_this_oldid_check(msg, 0);
                rc_namespace_check(msg, 0);
                rc_timestamp_check(msg);
                rc_title_check(msg, path);
                rc_type_check(msg, 52); // TAGS = 52
                rc_moved_to_ns_check(msg, 0);
                rc_moved_to_title_check(msg, String.Empty);
                rc_user_name_check(msg, USERNAME);
                rc_full_name_check(msg, String.Empty);
                rc_page_exists_check(msg, 1);
                rc_revision_check(msg, 1);
                cmnt_deleted_check(msg, 0);
                old_is_hidden_check(msg, false);
                rc_prev_revision_check(msg, 1);
                rc_summary_check(msg, "Edited once by " + USERNAME);
            }
        }

        /// <summary>
        ///    Set page to private, add a grant, remove the grant, and set page back to public; and verify feed data for all cases.
        /// </summary>        
        /// <feature>
        /// <name>GET:/site/feed</name>
        /// <uri>http://developer.mindtouch.com/en/ref/MindTouch_API/GET%3asite%2f%2ffeed</uri>
        /// </feature>
        /// <expected>Feed data is correct</expected>

        [Test]
        public void SiteFeed_SetPageToPrivatePublic_AddRemoveGrant_CorrectFeedData()
        {
            // Log in as ADMIN
            Plug p = Utils.BuildPlugForAdmin();
            DreamMessage msg;

            // Create a page
            string id;
            string path;
            PageUtils.CreateRandomPage(p, out id, out path);
            
            // Create a user to give and remove grant
            string userid;
            string username;
            UserUtils.CreateRandomContributor(p, out userid, out username);

            // Grant permissions to give user
            const string GRANT_ROLE = "contributor";

            // Variable security related feed entries
            string comment = String.Empty;
            int type = 0;


            for (int i = 0; i < (int)sec_ops.SET_PUBLIC; i++)
            {
                switch (i)
                {
                    case (int)sec_ops.SET_PRIVATE:
                        // Set page to private
                        XDoc securityDoc = new XDoc("security")
                            .Start("permissions.page")
                                .Elem("restriction", "Private")
                            .End();
                        msg = p.At("pages", id, "security").Post(securityDoc, new Result<DreamMessage>()).Wait();
                        Assert.AreEqual(DreamStatus.Ok, msg.Status, "Setting page to private failed!");
                        comment = "page restriction set to Private";
                        type = 56; // PAGE_RESTRICTION = 56
                        break;

                    case (int)sec_ops.ADD_GRANT:
                        // Add grant to user
                        securityDoc = new XDoc("security")
                        .Start("grants.added")
                            .Start("grant")
                                .Start("permissions")
                                    .Elem("role", GRANT_ROLE)
                                .End()
                                .Start("user")
                                    .Attr("id", userid)
                                .End()
                            .End()
                        .End();
                        msg = p.At("pages", id, "security").Post(securityDoc, new Result<DreamMessage>()).Wait();
                        Assert.AreEqual(DreamStatus.Ok, msg.Status, "Adding user grant failed!");
                        comment = username + " has been added as " + GRANT_ROLE;
                        type = 54; // ADD_GRANT = 54
                        break;

                    case (int)sec_ops.REMOVE_GRANT:
                        // Remove grant from user
                        securityDoc = new XDoc("security")
                        .Start("grants.removed")
                            .Start("grant")
                                .Start("permissions")
                                    .Elem("role", GRANT_ROLE)
                                .End()
                                .Start("user")
                                    .Attr("id", userid)
                                .End()
                            .End()
                        .End();
                        msg = p.At("pages", id, "security").Post(securityDoc, new Result<DreamMessage>()).Wait();
                        Assert.AreEqual(DreamStatus.Ok, msg.Status, "Removing user grant failed!");
                        comment = username + " has been revoked as " + GRANT_ROLE;
                        type = 55; // REMOVE_GRANT = 55
                        break;

                    case (int)sec_ops.SET_PUBLIC:
                        // Set page back to public
                        securityDoc = new XDoc("security")
                            .Start("permissions.page")
                                .Elem("restriction", "Public")
                            .End();
                        msg = p.At("pages", id, "security").Post(securityDoc, new Result<DreamMessage>()).Wait();
                        Assert.AreEqual(DreamStatus.Ok, msg.Status, "Setting page to public failed!");
                        comment = "page restriction set to Public";
                        type = 56; // PAGE_RESTRICTION = 56
                        break;

                    default:
                        break;
                }

                // Retrieve feed entry
                msg = p.At("site", "feed").With("format", "rawdaily").With("limit", 1).GetAsync().Wait();
                Assert.AreEqual(DreamStatus.Ok, msg.Status, "Feed retrieval failed!");

                // Run checks

                int pageid = System.Convert.ToInt32(id);

                rc_id_check(msg);
                rc_comment_check(msg, comment);
                rc_cur_id_check(msg, pageid);
                rc_last_oldid_check(msg, 0);
                rc_this_oldid_check(msg, 0);
                rc_namespace_check(msg, 0);
                rc_timestamp_check(msg);
                rc_title_check(msg, path);
                rc_type_check(msg, type);
                rc_moved_to_ns_check(msg, 0);
                rc_moved_to_title_check(msg, String.Empty);
                rc_user_name_check(msg, USERNAME);
                rc_full_name_check(msg, String.Empty);
                rc_page_exists_check(msg, 1);
                rc_revision_check(msg, 1);
                cmnt_deleted_check(msg, 0);
                old_is_hidden_check(msg, false);
                rc_prev_revision_check(msg, 1);
                rc_summary_check(msg, "Edited once by " + USERNAME);
            }
        }

        private void rc_id_check(DreamMessage msg)
        {
            int rc_id = msg.ToDocument()["change/rc_id"].AsInt ?? 0;
            Assert.IsTrue(rc_id > 0, "Invalid feed rc_id");
        }

        private void rc_comment_check(DreamMessage msg, string comment)
        {
            string commentMeta = msg.ToDocument()["change/rc_comment"].AsText ?? String.Empty;
            Assert.AreEqual(comment, commentMeta, "Unexpected rc_comment");
        }

        private void rc_last_oldid_check(DreamMessage msg, int oldid)
        {
            int oldidMeta = msg.ToDocument()["change/rc_last_oldid"].AsInt ?? 0;
            Assert.AreEqual(oldid, oldidMeta, "Unexpected rc_last_oldid");
        }

        private void rc_this_oldid_check(DreamMessage msg, int oldid)
        {
            int oldidMeta = msg.ToDocument()["change/rc_this_oldid"].AsInt ?? 0;
            Assert.AreEqual(oldid, oldidMeta, "Unexpected rc_last_oldid");
        }

        private void rc_cur_id_check(DreamMessage msg, int id)
        {
            int rc_cur_id = msg.ToDocument()["change/rc_cur_id"].AsInt ?? 0;
            Assert.AreEqual(id, rc_cur_id, "Unexpected rc_cur_id");
        }

        private void rc_namespace_check(DreamMessage msg, int ns)
        {
            int nsMeta = msg.ToDocument()["change/rc_namespace"].AsInt ?? -1;
            Assert.AreEqual(ns, nsMeta, "Unexpected rc_namespace");
        }

        private void rc_timestamp_check(DreamMessage msg)
        {
            string timestamp = msg.ToDocument()["change/rc_timestamp"].AsText ?? String.Empty;
            Assert.IsTrue(timestamp.Length > 0, "rc_timestamp is not present!");
        }

        private void rc_timestamp_check(DreamMessage msg, int pageid)
        {
            Plug p = Utils.BuildPlugForAdmin();

            // Retrieve page date.modified and strip all non-digit characters
            DreamMessage newmsg = p.At("pages", pageid.ToString()).GetAsync().Wait();
            Assert.AreEqual(DreamStatus.Ok, newmsg.Status, "Page retrieval failed");
            string timestamp = newmsg.ToDocument()["date.modified"].AsText ?? String.Empty;
            timestamp = Regex.Replace(timestamp, "[^0-9]", "");

            string timestampMeta = msg.ToDocument()["change/rc_timestamp"].AsText ?? String.Empty;
            Assert.AreEqual(timestamp, timestampMeta, "Unexpected rc_timestamp");
        }

        private void rc_timestamp_check(DreamMessage msg, string timestamp)
        {
            string timestampMeta = msg.ToDocument()["change/rc_timestamp"].AsText ?? String.Empty;
            Assert.AreEqual(timestamp, timestampMeta, "Unexpected rc_timestamp");
        }

        private void rc_title_check(DreamMessage msg, string path)
        {
            string pathMeta = msg.ToDocument()["change/rc_title"].AsText ?? String.Empty;
            Assert.AreEqual(path, pathMeta, "Unexpected rc_title");
        }

        private void rc_type_check(DreamMessage msg, int t)
        {
            int typeMeta = msg.ToDocument()["change/rc_type"].AsInt ?? -1;
            Assert.AreEqual(t, typeMeta, "Unexpected rc_type");
        }

        private void rc_moved_to_ns_check(DreamMessage msg, int ns)
        {
            int nsMeta = msg.ToDocument()["change/rc_moved_to_ns"].AsInt ?? -1;
            Assert.AreEqual(ns, nsMeta, "Unexpected rc_moved_to_ns");
        }

        private void rc_moved_to_title_check(DreamMessage msg, string path)
        {
            string pathMeta = msg.ToDocument()["change/rc_moved_to_title"].AsText ?? String.Empty;
            Assert.AreEqual(path, pathMeta, "Unexpected rc_moved_to_title");
        }

        private void rc_user_name_check(DreamMessage msg, string user)
        {
            string userMeta = msg.ToDocument()["change/rc_user_name"].AsText.ToLower() ?? String.Empty;
            Assert.AreEqual(user.ToLower(), userMeta.ToLower(), "Unexpected rc_user_name");
        }

        private void rc_full_name_check(DreamMessage msg, string name)
        {
            string nameMeta = msg.ToDocument()["change/rc_full_name"].AsText ?? String.Empty;
            Assert.AreEqual(name, nameMeta, "Unexpected rc_full_name");
        }

        private void rc_page_exists_check(DreamMessage msg, int exists)
        {
            int existsMeta = msg.ToDocument()["change/rc_page_exists"].AsInt ?? -1;
            Assert.AreEqual(exists, existsMeta, "Unexpected rc_page_exists");
        }

        private void rc_revision_check(DreamMessage msg, int rev)
        {
            int revMeta = msg.ToDocument()["change/rc_revision"].AsInt ?? 0;
            Assert.AreEqual(rev, revMeta, "Unexpected rc_revision");
        }

        private void edit_count_check(DreamMessage msg, int count)
        {
            int countMeta = msg.ToDocument()["change/edit_count"].AsInt ?? -1;
            Assert.AreEqual(count, countMeta, "Unexpected edit_count");
        }

        private void rc_prev_revision_check(DreamMessage msg, int rev)
        {
            int revMeta = msg.ToDocument()["change/rc_prev_revision"].AsInt ?? -1;
            Assert.AreEqual(rev, revMeta, "Unexpected rc_prev_revision");
        }

        private void rc_summary_check(DreamMessage msg, string summary)
        {
            string sumMeta = msg.ToDocument()["change/rc_summary"].AsText ?? String.Empty;
            Assert.AreEqual(summary.ToLower(), sumMeta.ToLower(), "Unexpected rc_summary");
        }

        private void cmnt_id_check(DreamMessage msg, int cmnt_id)
        {
            int cmnt_id_meta = msg.ToDocument()["change/cmnt_id"].AsInt ?? 0;
            Assert.AreEqual(cmnt_id, cmnt_id_meta, "Unexpected cmnt_id");
        }

        private void cmnt_number_check(DreamMessage msg, int cmnt_number)
        {
            int cmnt_number_meta = msg.ToDocument()["change/cmnt_number"].AsInt ?? 0;
            Assert.AreEqual(cmnt_number, cmnt_number_meta, "Unexpected cmnt_number");
        }

        private void cmnt_content_check(DreamMessage msg, string cmnt_content)
        {
            string cmnt_content_meta = msg.ToDocument()["change/cmnt_content"].AsText ?? String.Empty;
            Assert.AreEqual(cmnt_content, cmnt_content_meta, "Unexpected cmnt_content");
        }

        private void cmnt_content_mimetype_check(DreamMessage msg, string cmnt_content_mimetype)
        {
            string cmnt_content_mimetype_meta = msg.ToDocument()["change/cmnt_content_mimetype"].AsText ?? String.Empty;
            Assert.AreEqual(cmnt_content_mimetype, cmnt_content_mimetype_meta, "Unexpected cmnt_content_mimetype");
        }

        private void cmnt_deleted_check(DreamMessage msg, int deleted)
        {
            int delMeta = msg.ToDocument()["change/cmnt_deleted"].AsInt ?? -1;
            Assert.AreEqual(deleted, delMeta, "Unexpected cmnt_deleted");
        }

        private void old_is_hidden_check(DreamMessage msg, bool old_is_hidden)
        {
            bool? old_is_hidden_meta = msg.ToDocument()["change/old_is_hidden"].AsBool;
            Assert.IsTrue(old_is_hidden == old_is_hidden_meta, "Unexpected old_is_hidden");
        }
    }
}
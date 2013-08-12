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
using MindTouch.Xml;

namespace MindTouch.Deki.Tests.TagTests {
    [TestFixture]
    public class TagTests {

        [Test]
        public void GetPageTags() {
            // PUT:pages/{pageid}/tags
            // http://developer.mindtouch.com/Deki/API_Reference/PUT%3apages%2f%2f%7bpageid%7d%2f%2ftags

            // 1. Create a page
            // 2. Generate an XML document with one of each tag type: date, define, and text
            // (3) Assert tags exist for specified strings
            // 4. Delete page

            Plug p = Utils.BuildPlugForAdmin();

            string id = null;
            string path = null;
            DreamMessage msg = PageUtils.CreateRandomPage(p, out id, out path);

            Guid guid1 = Guid.NewGuid();
            string value1 = "date:2007-08-29";
            string value2 = "define:" + guid1.ToString();
            string value3 = "text tag";

            XDoc tagsDoc = new XDoc("tags")
                .Start("tag").Attr("value", value1).End()
                .Start("tag").Attr("value", value2).End()
                .Start("tag").Attr("value", value3).End();
            msg = p.At("pages", id, "tags").Put(tagsDoc);
            Assert.IsTrue(msg.Status == DreamStatus.Ok, "PUT request failed");

            // GET:pages/{pageid}/tags
            // http://developer.mindtouch.com/Deki/API_Reference/GET%3apages%2f%2f%7bpageid%7d%2f%2ftags

            msg = p.At("pages", id, "tags").Get();
            Assert.IsTrue(msg.Status == DreamStatus.Ok, "GET request failed");
            Assert.IsFalse(msg.ToDocument()[string.Format("tag[@value=\"{0}\"]", value1)].IsEmpty, "Date tag does not exist!");
            Assert.IsFalse(msg.ToDocument()[string.Format("tag[@value=\"{0}\"]", value2)].IsEmpty, "Define tag does not exist!");
            Assert.IsFalse(msg.ToDocument()[string.Format("tag[@value=\"{0}\"]", value3)].IsEmpty, "Text tag does not exist!");

            PageUtils.DeletePageByID(p, id, true);

        }

        // Combine the two? ^v

        [Test]
        public void GetSiteTags() {
            // GET:site/tags

            // 1. Create random page
            // 2. Generate and attach to page a random tag of each type: date, define, and text
            // (3) Assert tags exists for specified strings
            // 4. Delete page

            Plug p = Utils.BuildPlugForAdmin();
            string id = null;
            string path = null;
            DreamMessage msg = PageUtils.CreateRandomPage(p, out id, out path);

            Guid guid1 = Guid.NewGuid();
            Guid guid2 = Guid.NewGuid();
            DateTime now = DateTime.UtcNow;
            string value1 = "date:" + now.ToString("yyyy-MM-dd");
            string value2 = "define:" + guid1.ToString();
            string value3 = guid2.ToString();

            XDoc tagsDoc = new XDoc("tags")
                .Start("tag").Attr("value", value1).End()
                .Start("tag").Attr("value", value2).End()
                .Start("tag").Attr("value", value3).End();

            msg = p.At("pages", id, "tags").Put(tagsDoc);
            Assert.IsTrue(msg.Status == DreamStatus.Ok, "PUT request failed");

            msg = p.At("site", "tags").Get();
            Assert.IsTrue(msg.Status == DreamStatus.Ok, "GET request failed");
            Assert.IsFalse(msg.ToDocument()[string.Format("tag[@value=\"{0}\"]", value1)].IsEmpty, "Date tag does not exist!");
            Assert.IsFalse(msg.ToDocument()[string.Format("tag[@value=\"{0}\"]", value2)].IsEmpty, "Define tag does not exist!");
            Assert.IsFalse(msg.ToDocument()[string.Format("tag[@value=\"{0}\"]", value3)].IsEmpty, "Text tag does not exist!");

            PageUtils.DeletePageByID(p, id, true);
        }

        [Test]
        public void GetSiteTagsWithPages() {

            // 1. Create random page
            // 2. Generate a random tag and attach it to page
            // (3) Assert tag exists with correct tag and page IDs
            // 4. Delete the page

            Plug p = Utils.BuildPlugForAdmin();
            string page_id = null;
            string page_path = null;
            DreamMessage msg = PageUtils.CreateRandomPage(p, out page_id, out page_path);

            Guid guid = Guid.NewGuid();

            XDoc tagsDoc = new XDoc("tags")
                .Start("tag").Attr("value", guid.ToString()).End();

            msg = p.At("pages", page_id, "tags").Put(tagsDoc);
            Assert.IsTrue(msg.Status == DreamStatus.Ok, "PUT request failed");

            uint tag_id = msg.ToDocument()["tag/@id"].AsUInt ?? 0;
            msg = p.At("site", "tags").With("pages", true).Get();

            Assert.IsTrue(msg.Status == DreamStatus.Ok, "GET request failed");
            Assert.IsFalse(msg.ToDocument()[string.Format("tag[@id=\"{0}\"]/pages/page[@id=\"{1}\"]", tag_id, page_id)].IsEmpty, "Tag with specified tag/page ID does not exist!");

            PageUtils.DeletePageByID(p, page_id, true);
        }

        [Test]
        public void GetTextTag() {

            // 1. Create random page
            // 2. Generate random text tag and attach to page
            // (3a) Assert retrived tag value equals to that of generated string
            // (3b) Assert tag exists according to tag ID
            // (4a) Same as 3a, but using the tag value in the path
            // (4b) Same as 3b, but using the tag value in the path
            // 5. Delete page

            Plug p = Utils.BuildPlugForAdmin();
            string id = null;
            string path = null;
            DreamMessage msg = PageUtils.CreateRandomPage(p, out id, out path);

            Guid guid = Guid.NewGuid();

            XDoc tagsDoc = new XDoc("tags")
                .Start("tag").Attr("value", guid.ToString()).End();

            msg = p.At("pages", id, "tags").Put(tagsDoc);
            Assert.IsTrue(msg.Status == DreamStatus.Ok, "PUT request failed");

            // GET:site/tags/{id}
            uint tag_id = msg.ToDocument()["tag/@id"].AsUInt ?? 0;
            msg = p.At("site", "tags", tag_id.ToString()).Get();
            Assert.IsTrue(msg.Status == DreamStatus.Ok, "GET request failed {id}");
            Assert.IsTrue(msg.ToDocument()["@value"].AsText == guid.ToString(), "Tag value does not match generated string! {id}");
            Assert.IsFalse(msg.ToDocument()[string.Format("pages/page[@id=\"{0}\"]", id)].IsEmpty, "Tag with specified ID does not exist! {id}");

            // GET:site/tags/{=tag_value}
            msg = p.At("site", "tags", string.Format("={0}", XUri.Encode(guid.ToString()))).Get();
            Assert.IsTrue(msg.Status == DreamStatus.Ok, "GET request failed {=tag_value}");
            Assert.IsTrue(msg.ToDocument()["@value"].AsText == guid.ToString(), "Tag value does not match generated string! {=tag_value}");
            Assert.IsFalse(msg.ToDocument()[string.Format("pages/page[@id=\"{0}\"]", id)].IsEmpty, "Tag with specified ID does not exist! {=tag_value}");

            PageUtils.DeletePageByID(p, id, true);
        }

        [Test]
        public void GetDefineTag() {

            // 1. Create random page
            // 2. Generate random define tag and attach to page
            // (3a) Assert retrived tag value equals to that of generated string
            // (3b) Assert tag exists according to tag ID
            // (4a) Same as 3a, but using the tag value in the path
            // (4b) Same as 3b, but using the tag value in the path
            // 5. Delete page

            Plug p = Utils.BuildPlugForAdmin();
            string id = null;
            string path = null;
            DreamMessage msg = PageUtils.CreateRandomPage(p, out id, out path);

            Guid guid = Guid.NewGuid();
            string value = "define:" + guid.ToString();
            XDoc tagsDoc = new XDoc("tags")
                .Start("tag").Attr("value", value).End();

            msg = p.At("pages", id, "tags").Put(tagsDoc);
            Assert.IsTrue(msg.Status == DreamStatus.Ok, "PUT request failed");

            // GET:site/tags/{id}
            uint tag_id = msg.ToDocument()["tag/@id"].AsUInt ?? 0;
            msg = p.At("site", "tags", tag_id.ToString()).Get();
            Assert.IsTrue(msg.Status == DreamStatus.Ok, "GET request failed! {id}");
            Assert.IsTrue(msg.ToDocument()["@value"].AsText == value, "Tag value does not match generated string! {id}");
            Assert.IsFalse(msg.ToDocument()[string.Format("pages/page[@id=\"{0}\"]", id)].IsEmpty, "Tag with specified ID does not exist! {id}");

            // GET:site/tags/{=tag_value}
            msg = p.At("site", "tags", string.Format("={0}", XUri.Encode(value))).Get();
            Assert.IsTrue(msg.Status == DreamStatus.Ok, "GET request failed! {=tag_value}");
            Assert.IsTrue(msg.ToDocument()["@value"].AsText == value, "Tag value does not match generated string! {=tag_value}");
            Assert.IsFalse(msg.ToDocument()[string.Format("pages/page[@id=\"{0}\"]", id)].IsEmpty, "Tag with specified ID does not exist! {=tag_value}");

            PageUtils.DeletePageByID(p, id, true);
        }

        [Test]
        public void GetDateTag() {

            // 1. Create random page
            // 2. Generate random date tag and attach to page
            // (3a) Assert retrived tag value equals to that of generated string
            // (3b) Assert tag exists according to tag ID
            // (4a) Same as 3a, but using the tag value in the path
            // (4b) Same as 3b, but using the tag value in the path
            // 5. Delete page

            Plug p = Utils.BuildPlugForAdmin();
            string id = null;
            string path = null;
            DreamMessage msg = PageUtils.CreateRandomPage(p, out id, out path);

            DateTime date = DateTime.UtcNow;
            string value = "date:" + date.ToString("yyyy-MM-dd");

            XDoc tagsDoc = new XDoc("tags")
                .Start("tag").Attr("value", value).End();

            DreamMessage tempmsg = p.At("pages", id, "tags").Put(tagsDoc);
            Assert.IsTrue(tempmsg.Status == DreamStatus.Ok, "PUT request failed");

            // GET:site/tags/{id}
            uint tag_id = tempmsg.ToDocument()["tag/@id"].AsUInt ?? 0;
            msg = p.At("site", "tags", tag_id.ToString()).GetAsync().Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, String.Format("GET request failed! TAG_ID: {0} ||| PUT_MSG: {1} ||| GET_MSG: {2} ||| value: {3}", tag_id, tempmsg.ToString(), msg.ToString(), value));
            Assert.IsTrue(msg.ToDocument()["@value"].AsText == value, "Tag value does not match generated string! {id}");
            Assert.IsFalse(msg.ToDocument()[string.Format("pages/page[@id=\"{0}\"]", id)].IsEmpty, "Tag with specified ID does not exist! {id}");

            // GET:site/tags/{=tag_value}
            msg = p.At("site", "tags", string.Format("={0}", XUri.Encode(value))).Get();
            Assert.IsTrue(msg.Status == DreamStatus.Ok, "GET request failed! {=tag_value}");
            Assert.IsTrue(msg.ToDocument()["@value"].AsText == value, "Tag value does not match generated string! {=tag_value}");
            Assert.IsFalse(msg.ToDocument()[string.Format("pages/page[@id=\"{0}\"]", id)].IsEmpty, "Tag with specified ID does not exist! {=tag_value}");

            PageUtils.DeletePageByID(p, id, true);
        }

        [Test]
        public void GetTagsByDateRange() {

            // 1. Create random page
            // 2. Generate today's date tag and attach to page
            // (3) Assert that the date tag is returned within a period of now to next month (30 days)
            // (4) Assert that the date tag is returned within a period of now to next 2 months (60 days)
            // (5) Assert that the date tag is not returned within a period of tomorrow (1 day) to next month (30 days)
            // 6. Delete page

            Plug p = Utils.BuildPlugForAdmin();
            string page_id = null;
            string path = null;
            DreamMessage msg = PageUtils.CreateRandomPage(p, out page_id, out path);

            DateTime date = DateTime.UtcNow;
            string value = "date:" + date.ToString("yyyy-MM-dd");
            XDoc tagsDoc = new XDoc("tags")
                .Start("tag").Attr("value", value).End();

            msg = p.At("pages", page_id, "tags").Put(tagsDoc);
            Assert.IsTrue(msg.Status == DreamStatus.Ok, "PUT request failed");
            uint tag_id = msg.ToDocument()["tag/@id"].AsUInt ?? 0;

            // get tags within 30 days
            // GET:site/tags/?pages=true&from=now
            msg = p.At("site", "tags").With("pages", true).With("type", "date").With("from", date.ToString("yyyy-MM-dd")).Get();
            Assert.IsTrue(msg.Status == DreamStatus.Ok, "GET request failed. from=now");
            string xpath = string.Format("tag[@id=\"{0}\"]/pages/page[@id=\"{1}\"]", tag_id, page_id);
            Assert.IsFalse(msg.ToDocument()[xpath].IsEmpty, "Tag was not returned! from=now");

            // get tags with explicit to, from (from=now, to=now+60 days)
            msg = p.At("site", "tags").With("pages", true).With("type", "date").With("from", date.ToString("yyyy-MM-dd")).With("to", date.AddDays(60).ToString("yyyy-MM-dd")).Get();
            Assert.IsTrue(msg.Status == DreamStatus.Ok, "GET request failed. from=now to=now+60");
            Assert.IsFalse(msg.ToDocument()[xpath].IsEmpty, "Tag was not returned! from=now to=now+60");

            // get tags past current date (current tag should not be found)
            msg = p.At("site", "tags").With("pages", true).With("type", "date").With("from", date.AddDays(1).ToString("yyyy-MM-dd")).Get();
            Assert.IsTrue(msg.Status == DreamStatus.Ok, "GET request failed. from=now+1");
            Assert.IsTrue(msg.ToDocument()[xpath].IsEmpty, "Tag was returned! from=now+1");

            PageUtils.DeletePageByID(p, page_id, true);

        }

        [Test]
        public void GetTagsByPartialName() {

            // 1. Create random page
            // 2. Generate a random text tag and attach it to page
            // (3) Assert the tag is returned when searching for the first 3 characters of the value
            // 4. Delete the page

            Plug p = Utils.BuildPlugForAdmin();
            string page_id = null;
            string path = null;
            DreamMessage msg = PageUtils.CreateRandomPage(p, out page_id, out path);

            string guid = Guid.NewGuid().ToString();
            XDoc tagsDoc = new XDoc("tags")
                .Start("tag").Attr("value", guid).End();
            msg = p.At("pages", page_id, "tags").Put(tagsDoc);
            Assert.IsTrue(msg.Status == DreamStatus.Ok, "PUT request failed");
            uint tag_id = msg.ToDocument()["tag/@id"].AsUInt ?? 0;

            // GET:site/tags?q=partialName
            msg = p.At("site", "tags").With("pages", true).With("type", "text").With("q", guid.Substring(0, 3)).Get();
            Assert.IsTrue(msg.Status == DreamStatus.Ok, "GET request failed");
            Assert.IsFalse(msg.ToDocument()[string.Format("tag[@id=\"{0}\"]/pages/page[@id=\"{1}\"]", tag_id, page_id)].IsEmpty, "Tag was not returned!");

            PageUtils.DeletePageByID(p, page_id, true);
        }

        [Test]
        public void RenameExistingTagDueToAccentCapitalization() {

            Plug p = Utils.BuildPlugForAdmin();
            string page_id = null;
            string path = null;
            DreamMessage msg = PageUtils.CreateRandomPage(p, out page_id, out path);

            // set lowercase tag
            string t = StringUtil.CreateAlphaNumericKey(10).ToLower();
            XDoc tagsDoc = new XDoc("tags")
                .Start("tag").Attr("value", t).End();
            msg = p.At("pages", page_id, "tags").Put(tagsDoc);
            Assert.IsTrue(msg.Status == DreamStatus.Ok, "PUT request failed");
            uint tag_id = msg.ToDocument()["tag/@id"].AsUInt ?? 0;

            // set uppercase tag on new page
            msg = PageUtils.CreateRandomPage(p, out page_id, out path);
            t = t.ToUpper();
            tagsDoc = new XDoc("tags")
                .Start("tag").Attr("value", t).End();
            msg = p.At("pages", page_id, "tags").Put(tagsDoc);
            Assert.IsTrue(msg.Status == DreamStatus.Ok, "PUT request failed");
            tag_id = msg.ToDocument()["tag/@id"].AsUInt ?? 0;
            string returnedTag = msg.ToDocument()["tag/@value"].AsText;
            Assert.AreEqual(t, returnedTag, "Tag not renamed!");
        }

        [Test]
        public void MT_10206_RemovePageAndTagCleanupTest() {
            /*
                   - Testing: PUT:pages/{pageid}/tags, PUT:site/tags
                   - Grab admin plug
                   - Create user with write privileges
                   - Grab user plug
                   - Create page
                   - Tag page with multiple tags
                   - Assert tag count and value
                   - Tag page with a new set of  tags
                   - Assert tag count and value
                   - Remove page
                   - Assert that the tags that are not being used by any page were removed.
            */

            var startDate = DateTime.Now.Subtract(TimeSpan.FromDays(1)).Date.ToString();
            var tagsToAdd = new[] { Utils.GenerateUniqueName("random_tag"), Utils.GenerateUniqueName("random_tag"), Utils.GenerateUniqueName("random_tag") };
            var retaggingTags = new[] { Utils.GenerateUniqueName("random_tag"), Utils.GenerateUniqueName("random_tag") };
            var adminPlug = Utils.BuildPlugForAdmin();
            string userId, userName;
            UserUtils.CreateRandomContributor(adminPlug, out userId, out userName);
            var userPlug = Utils.BuildPlugForUser(userName);
            string pageId, pagePath;
            PageUtils.CreateRandomPage(userPlug, "Testing tagging page", out pageId, out pagePath);
            var tagPageResponse = PageUtils.TagPage(userPlug, pageId, tagsToAdd);
            Assert.AreEqual(DreamStatus.Ok, tagPageResponse.Status, "[1] Adding tags to a page failed");

            // Fetch the page's tags
            var getPageTagsResponse = userPlug.At("pages", pageId, "tags").Get();
            var doc = getPageTagsResponse.ToDocument();
            Assert.AreEqual(tagsToAdd.Length, doc["@count"].AsInt, "[2] Not all the tags were applied to the page");

            // Assert that the tags were added.
            var getSiteTagsResponse = userPlug.At("site", "tags").With("from", startDate).Get();
            var tagsDoc = getSiteTagsResponse.ToDocument();
            foreach(var tag in tagsToAdd) {
                Assert.AreEqual(false, tagsDoc[String.Format("tag[@value='{0}']", tag)].IsEmpty, "[3] The tag wasn't added to site's pool of tags");
            }

            // Tag again
            tagPageResponse = PageUtils.TagPage(userPlug, pageId, retaggingTags);
            Assert.AreEqual(DreamStatus.Ok, tagPageResponse.Status, "[4] Re-tagging the page failed");
            getPageTagsResponse = userPlug.At("pages", pageId, "tags").Get();
            doc = getPageTagsResponse.ToDocument();
            Assert.AreEqual(retaggingTags.Length, doc["@count"].AsInt, "[5] The number of tags the re-tagged page should have is incorrect");

            // Assert that the new tags were added
            getSiteTagsResponse = userPlug.At("site", "tags").With("from", startDate).Get();
            tagsDoc = getSiteTagsResponse.ToDocument();
            foreach(var tag in retaggingTags) {
                Assert.AreEqual(false, tagsDoc[String.Format("tag[@value='{0}']", tag)].IsEmpty, "[6] The tag wasn't added to site's pool of tags");
            }

            // Delete page
            PageUtils.DeletePageByID(userPlug, pageId, true);

            // Assert that the unused tags were removed from the site
            getSiteTagsResponse = userPlug.At("site", "tags").With("from", startDate).Get();
            Assert.AreEqual(DreamStatus.Ok, getSiteTagsResponse.Status, "[7] Fetching the site's tags failed");
            tagsDoc = getSiteTagsResponse.ToDocument();
            foreach(var tag in retaggingTags) {
                Assert.AreEqual(true, tagsDoc[String.Format("tag[@value='{0}']", tag)].IsEmpty, "[8] The tag wasn't removed even if no page is using it anymore");
            }
        }

        [Test]
        public void Multiple_tags_on_single_page_point_to_their_respective_define_tags() {

            // Arrange
            Plug p = Utils.BuildPlugForAdmin();
            string defineA = null;
            string defineAPath = null;
            DreamMessage msg = PageUtils.CreateRandomPage(p, out defineA, out defineAPath);
            var tagA = "tagA-"+StringUtil.CreateAlphaNumericKey(8);
            msg = p.At("pages", defineA, "tags").Put(new XDoc("tags").Start("tag").Attr("value", "define:" + tagA).End());
            string defineB = null;
            string defineBPath = null;
            msg = PageUtils.CreateRandomPage(p, out defineB, out defineBPath);
            var tagB = "tabB-"+StringUtil.CreateAlphaNumericKey(8);
            msg = p.At("pages", defineB, "tags").Put(new XDoc("tags").Start("tag").Attr("value", "define:" + tagB).End());
            string links = null;
            msg = PageUtils.CreateRandomPage(p, out links);
            msg = p.At("pages", links, "tags").Put(
                new XDoc("tags")
                    .Start("tag").Attr("value", tagA).End()
                    .Start("tag").Attr("value", tagB).End()
            );

            // Act
            msg = p.At("pages", links, "tags").Get();
            var tags = msg.ToDocument();

            // Assert
            Assert.AreEqual("/" + defineAPath, tags[string.Format("tag[@value='{0}']/uri", tagA)].AsUri.Path, "first define tab is pointing at wrong page");
            Assert.AreEqual("/" + defineBPath, tags[string.Format("tag[@value='{0}']/uri", tagB)].AsUri.Path, "second define tab is pointing at wrong page");
        }
    }
}

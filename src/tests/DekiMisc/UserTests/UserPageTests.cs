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

using MindTouch.Tasking;
using NUnit.Framework;
using MindTouch.Dream;
using MindTouch.Xml;
using MindTouch.Dream.Test;

namespace MindTouch.Deki.Tests.UserTests {

    [TestFixture]
    public class DekiWiki_UserPageTest {

        // Existing site settings
        XDoc oldConfig;

        [SetUp]
        public void SaveConfig() {
            oldConfig = SiteUtils.RetrieveConfig(Utils.BuildPlugForAdmin()).ToDocument();
            if (oldConfig == null) {
                Assert.Fail("Bad config");
            }
        }

        [TearDown]
        public void RestoreConfig() {
            if (oldConfig == null) {
                Assert.Fail("Bad config");
            }
            SiteUtils.SaveConfig(Utils.BuildPlugForAdmin(), oldConfig);
        }

        [Test]
        public void ContentNewUser_NoKey_DefaultUserPageContents() {
            // Log in as ADMIN
            Plug p = Utils.BuildPlugForAdmin();

            // Remove content/new-user key
            SiteUtils.RemoveConfigKey(p, "content/new-user");

            // Create a user
            string userid;
            string username;
            UserUtils.CreateRandomContributor(p, out userid, out username);

            // Retrieve a page id. This is to render the template using the "pageid" parameter in the page/{pageid}/contents feature.
            DreamMessage msg = PageUtils.GetPage(p, String.Empty);
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Failed to retrieve home page");
            uint homepageid = msg.ToDocument()["@id"].AsUInt ?? 0;
            Assert.IsTrue(homepageid > 0, "Invalid homepage ID");

            // Retrieve user page contents
            msg = p.At("pages", "=" + XUri.DoubleEncode("User:" + username), "contents").With("pageid", homepageid).Get(new Result<DreamMessage>()).Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Failed to retrieve user page contents");
            string content = msg.ToDocument()["body"].AsText ?? String.Empty;

            // Retrieve default new user page content from resources
            string resource = "MindTouch.Templates.userwelcome.visitor";
            msg = p.At("site", "localization").With("resource", resource).Get(new Result<DreamMessage>()).Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Unable to retrieve resource for string: " + resource);
            string expectedContent = "<p>" + msg.AsText() + "</p>" ?? String.Empty;

            Assert.AreEqual(expectedContent, content, "Unexpected contents");
        }

        [Test]
        public void ContentNewUser_PointsToValidPage_UserPageGetsPageContents() {
            // Log in as ADMIN
            Plug p = Utils.BuildPlugForAdmin();
            DreamMessage msg;

            // Create page content/new-user points to
            string content = "test userpage content";
            string pageid;
            string path;
            PageUtils.SavePage(p, String.Empty, PageUtils.GenerateUniquePageName(), content, out pageid, out path);

            // Add content/new-user key
            SiteUtils.AddConfigKey(p, "content/new-user", path);

            // Create a user
            string userid;
            string username;
            UserUtils.CreateRandomContributor(p, out userid, out username);

            // Retrieve a page id. This is to render the template using the "pageid" parameter in the page/{pageid}/contents feature.
            msg = PageUtils.GetPage(p, String.Empty);
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Failed to retrieve home page");
            uint homepageid = msg.ToDocument()["@id"].AsUInt ?? 0;
            Assert.IsTrue(homepageid > 0, "Invalid homepage ID");

            // Retrieve user page contents and verify it matches content
            msg = p.At("pages", "=" + XUri.DoubleEncode("User:" + username), "contents").With("pageid", homepageid).Get(new Result<DreamMessage>()).Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Failed to retrieve user page contents");
            Assert.AreEqual(content, msg.ToDocument()["body"].AsText ?? String.Empty, "Unexpected contents");
        }

        [Test]
        public void ContentNewUser_PointsToInvalidPage_DefaultUserPageContents() {
            // Log in as ADMIN
            Plug p = Utils.BuildPlugForAdmin();

            // Add content/new-user key to point to some bogus page
            SiteUtils.AddConfigKey(p, "content/new-user", Utils.GenerateUniqueName());

            // Create a user
            string userid;
            string username;
            UserUtils.CreateRandomContributor(p, out userid, out username);

            // Retrieve user page contents
            DreamMessage msg = p.At("pages", "=" + XUri.DoubleEncode("User:" + username), "contents").Get(new Result<DreamMessage>()).Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Failed to retrieve user page contents");
            string content = msg.ToDocument()["body"].AsText ?? String.Empty;

            // Retrieve default new user page content from resources
            string resource = "MindTouch.Templates.userwelcome.visitor";
            msg = p.At("site", "localization").With("resource", resource).Get(new Result<DreamMessage>()).Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Unable to retrieve resource for string: " + resource);
            string expectedContent = "<p>" + msg.AsText() + "</p>" ?? String.Empty;

            Assert.AreEqual(expectedContent, content, "Unexpected contents");
        }

        [Test]
        public void DeleteUserPage_UserPageRecreated() {
            // Log in as ADMIN
            Plug p = Utils.BuildPlugForAdmin();

            // Create a user
            string userid;
            string username;
            UserUtils.CreateRandomContributor(p, out userid, out username);

            // Retrieve user page
            DreamMessage msg = PageUtils.GetPage(p, "User:" + username);
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Could not retrieve user page.");
            uint pageid = msg.ToDocument()["@id"].AsUInt ?? 0;

            // Delete user page
            PageUtils.DeletePageByName(p, "User:" + username, true);

            // Check user page recreated and has new page ID
            msg = PageUtils.GetPage(p, "User:" + username);
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Could not retrieve user page.");
            Assert.AreEqual(pageid + 1, msg.ToDocument()["@id"].AsUInt ?? 0, "Unexpected page ID.");
        }

        [Test]
        public void HomePageGrantRole_NoKey_NoGrant() {
            // Log in as ADMIN
            Plug p = Utils.BuildPlugForAdmin();

            // Remove security/homepage-grant-role key
            SiteUtils.RemoveConfigKey(p, "security/homepage-grant-role");

            // Create a user
            string userid;
            string username;
            UserUtils.CreateRandomContributor(p, out userid, out username);

            // Retrieve user page
            DreamMessage msg = PageUtils.GetPage(p, "User:" + username);
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Could not retrieve user page.");

            // Check that user did not receive grant
            XDoc grant = msg.ToDocument()["security/grants/grant[user/@id=" + userid + "]"];
            Assert.IsTrue(grant.IsEmpty, "User has grant?!");
        }

        [Test]
        public void HomePageGrantRole_ValidKey_UserGetsGrant() {
            // Log in as ADMIN
            Plug p = Utils.BuildPlugForAdmin();

            // Add security/homepage-grant-role key
            string role = "Viewer";
            SiteUtils.AddConfigKey(p, "security/homepage-grant-role", role);

            // Create a user
            string userid;
            string username;
            UserUtils.CreateRandomContributor(p, out userid, out username);

            // Retrieve user page
            DreamMessage msg = PageUtils.GetPage(p, "User:" + username);
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Could not retrieve user page.");

            // Check that user received the grant
            XDoc grant = msg.ToDocument()["security/grants/grant[user/@id=" + userid + "]"];
            Assert.IsFalse(grant.IsEmpty, "User was not given a grant?!");
            Assert.AreEqual(role, grant["permissions/role"].AsText ?? String.Empty, "Unexpected role.");
        }

        [Test]
        public void HomePageGrantRole_InvalidKey_NoGrant() {
            // Log in as ADMIN
            Plug p = Utils.BuildPlugForAdmin();

            // Add security/homepage-grant-role key with some random value
            SiteUtils.AddConfigKey(p, "security/homepage-grant-role", Utils.GenerateUniqueName());

            // Create a user
            string userid;
            string username;
            UserUtils.CreateRandomContributor(p, out userid, out username);

            // Retrieve user page
            DreamMessage msg = PageUtils.GetPage(p, "User:" + username);
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Could not retrieve user page.");

            // Check that user did not receive grant
            XDoc grant = msg.ToDocument()["security/grants/grant[user/@id=" + userid + "]"];
            Assert.IsTrue(grant.IsEmpty, "User has grant?!");
        }
    }
}
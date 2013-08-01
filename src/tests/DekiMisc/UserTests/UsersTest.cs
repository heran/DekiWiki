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
using System.Linq;
using System.Text;
using MindTouch.Tasking;
using MindTouch.Web;
using NUnit.Framework;
using MindTouch.Dream;
using MindTouch.Xml;
using MindTouch.Dream.Test;

namespace MindTouch.Deki.Tests.UserTests {
    [TestFixture]
    public class DekiWiki_UsersTest {

        private static readonly log4net.ILog _log = LogUtils.CreateLog();

        /// <summary>
        ///     Create a user
        /// </summary>        
        /// <feature>
        /// <name>POST:users</name>
        /// <uri>http://developer.mindtouch.com/en/ref/MindTouch_API/POST%3ausers</uri>
        /// </feature>
        /// <expected>User creation successful and POST response metadata matches sent information</expected>

        [Test]
        public void CreateUser() {
            // Build ADMIN plug
            Plug p = Utils.BuildPlugForAdmin();

            try {
                // Define user information and generate user XML document
                string name = Utils.GenerateUniqueName();
                string email = "newuser1@mindtouch.com";
                string fullName = "newuser1's full name";
                string role = "Contributor";

                XDoc usersDoc = new XDoc("user")
                    .Elem("username", name)
                    .Elem("email", email)
                    .Elem("fullname", fullName)
                    .Start("permissions.user")
                        .Elem("role", role)
                    .End();

                // Create the User
                DreamMessage msg = p.At("users").Post(usersDoc);
                Assert.AreEqual(DreamStatus.Ok, msg.Status, "User creation failed");

                // Assert all the information in the returned document is consistent
                Assert.IsTrue(msg.ToDocument()["username"].AsText == name, "Name does not match that in return document");
                Assert.IsTrue(msg.ToDocument()["email"].AsText == email, "Email does not match that in return document");
                Assert.IsTrue(msg.ToDocument()["fullname"].AsText == fullName, "Full name does not match that in return document");
                Assert.IsTrue(msg.ToDocument()["permissions.user/role"].AsText == role, "User permissions does not match that in return document");
            }

            // In the case the username already exists
            catch(MindTouch.Dream.DreamResponseException ex) {
                Assert.IsTrue(ex.Response.Status == DreamStatus.Conflict, "Username already exists, but did not return Conflict?!");
            }
        }

        /// <summary>
        ///     Set a new user password
        /// </summary>        
        /// <feature>
        /// <name>PUT:users/{userid}/password</name>
        /// <uri>http://developer.mindtouch.com/en/ref/MindTouch_API/PUT%3ausers%2f%2f%7Buserid%7D%2f%2fpassword</uri>
        /// </feature>
        /// <expected>Successful authentication through new username/password credentials</expected>

        [Test]
        public void SetUserPassword() {
            // Build ADMIN plug
            Plug p = Utils.BuildPlugForAdmin();

            // Create a random contributor
            string id = null;
            string name = null;
            DreamMessage msg = UserUtils.CreateRandomContributor(p, out id, out name);

            // Set a new password "newpassword" for the user
            msg = DreamMessage.Ok(MimeType.TEXT, "newpassword");
            msg = p.At("users", "=" + name, "password").Put(msg);
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Setting password failed");

            // "Log out" of ADMIN
            p = Utils.BuildPlugForAnonymous();

            // Build a plug for username/newpassword and assert authentication is successful
            msg = p.WithCredentials(name, "newpassword").At("users", "authenticate").Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "User authentication failed");
        }

        /// <summary>
        ///     Set an alternative user password
        /// </summary>        
        /// <feature>
        /// <name>PUT:users/{userid}/password</name>
        /// <uri>http://developer.mindtouch.com/en/ref/MindTouch_API/PUT%3ausers%2f%2f%7Buserid%7D%2f%2fpassword</uri>
        /// <parameter>altpassword</parameter>
        /// </feature>
        /// <expected>Successful authentication through alternative username/password credentials</expected>

        [Test]
        public void SetUserAltPassword() {
            // Build ADMIN plug
            Plug p = Utils.BuildPlugForAdmin();

            // Create a random contributor
            string id = null;
            string name = null;
            DreamMessage msg = UserUtils.CreateRandomContributor(p, out id, out name);

            // Set a new password "newpassword" for the user
            msg = DreamMessage.Ok(MimeType.TEXT, "newpassword");
            msg = p.At("users", "=" + name, "password").Put(msg);
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Password set failed");

            // Set an alternative password "altpassword" for the user
            msg = DreamMessage.Ok(MimeType.TEXT, "newaltpassword");
            msg = p.At("users", "=" + name, "password").With("altpassword", true).Put(msg);
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Alternative password set failed");

            // "Log out" of ADMIN and build plug for the user using the alternative password
            p = Utils.BuildPlugForAnonymous();
            msg = p.WithCredentials(name, "newaltpassword").At("users", "authenticate").Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Authentication with new alternative password failed");

            // "Log out" again and build plug using the new password
            p = Utils.BuildPlugForAnonymous();
            msg = p.WithCredentials(name, "newpassword").At("users", "authenticate").Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Authentication with new password failed");
        }

        /// <summary>
        ///     Retrieve user list
        /// </summary>        
        /// <feature>
        /// <name>GET:users</name>
        /// <uri>http://developer.mindtouch.com/en/ref/MindTouch_API/GET%3ausers</uri>
        /// </feature>
        /// <expected>200 OK HTTP response</expected>

        [Test]
        public void GetUsers() {
            // Build ADMIN plug
            Plug p = Utils.BuildPlugForAdmin();

            // Retrieve user list
            DreamMessage msg = p.At("users").Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "User list retrieval failed");
        }

        /// <summary>
        ///     Retrieve user data by name/ID
        /// </summary>        
        /// <feature>
        /// <name>GET:users/{userid}</name>
        /// <uri>http://developer.mindtouch.com/en/ref/MindTouch_API/GET%3ausers%2f%2f%7buserid%7d</uri>
        /// </feature>
        /// <expected>Generated user information matches subsequent retrieved user metadata</expected>

        [Test]
        public void GetUser() {
            // Build ADMIN plug
            Plug p = Utils.BuildPlugForAdmin();

            // Create a random contributor
            string id = null;
            string name = null;
            DreamMessage msg = UserUtils.CreateRandomContributor(p, out id, out name);

            // Retrieve the user by name and assert that the retrieved document name element matches generated name
            msg = p.At("users", "=" + name).Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "User retrieval by name failed");
            Assert.IsTrue(msg.ToDocument()["username"].AsText == name, "Name in retrieved user (by name) document does not match generated name!");

            // Retrieve the user by ID and perform the same check
            msg = p.At("users", id).Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "User retrieval by ID failed");
            Assert.IsTrue(msg.ToDocument()["username"].AsText == name, "Name in retrieved user (by ID) document does not match generated name!");

            // Retrieve the current user (ADMIN) and assert the retrieved document shows the correct name and role
            msg = p.At("users", "current").Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Retrieval of current user failed! " + msg.ToText());
            Assert.AreEqual(Utils.Settings.UserName.ToLower(), msg.ToDocument()["username"].AsText.ToLower(), "Current user is not 'Admin'?!");
            Assert.AreEqual("Admin", msg.ToDocument()["permissions.user/role"].AsText, "Current user does not have ADMIN permissions?!");
        }

        /// <summary>
        ///     Update user information through POST request
        /// </summary>        
        /// <feature>
        /// <name>POST:users</name>
        /// <uri>http://developer.mindtouch.com/en/ref/MindTouch_API/POST%3ausers</uri>
        /// </feature>
        /// <expected>Updated user information matches POST method response metadata</expected>

        [Test]
        public void ChangeUserThroughPost() {
            // Build ADMIN plug
            Plug p = Utils.BuildPlugForAdmin();

            // Create a random contributor
            string id = null;
            string name = null;
            DreamMessage msg = UserUtils.CreateRandomContributor(p, out id, out name);

            // Define user information and generate user XML document
            string newemail = "newpostusermail@mindtouch.com";
            string newfullName = "new post full name";
            string newrole = "Viewer";
            string newTimezone = "-08:00";

            XDoc usersDoc = new XDoc("user").Attr("id", id)
                .Elem("email", newemail)
                .Elem("fullname", newfullName)
                .Elem("timezone", newTimezone)
                .Start("permissions.user")
                    .Elem("role", newrole)
                .End();

            // Replace random contributor's information with new information
            msg = p.At("users").Post(usersDoc);
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "User update failed");

            // Assert returned POST method document metadata is consistent with the XML request information
            Assert.IsTrue(msg.ToDocument()["username"].AsText == name, "Name does not match that in return document");
            Assert.IsTrue(msg.ToDocument()["email"].AsText == newemail, "Email does not match that in return document");
            Assert.IsTrue(msg.ToDocument()["fullname"].AsText == newfullName, "Full name does not match that in return document");
            Assert.IsTrue(msg.ToDocument()["permissions.user/role"].AsText == newrole, "User permissions does not match that in return document");
            Assert.AreEqual(newTimezone, msg.ToDocument()["timezone"].AsText, "Timezone does not match that in return document");
        }

        /// <summary>
        ///     Update user information through PUT request
        /// </summary>        
        /// <feature>
        /// <name>PUT:users/{userid}</name>
        /// <uri>http://developer.mindtouch.com/en/ref/MindTouch_API/PUT%3ausers%2f%2f%7Buserid%7D</uri>
        /// </feature>
        /// <expected>Updated user information matches PUT method response metadata</expected>

        [Test]
        public void ChangeUserThroughPUT() {
            // Build ADMIN plug
            Plug p = Utils.BuildPlugForAdmin();

            // Create a random contributor
            string id = null;
            string name = null;
            DreamMessage msg = UserUtils.CreateRandomContributor(p, out id, out name);

            // Define user information and generate user XML document
            string newemail = "newusermail@mindtouch.com";
            string newfullName = "new full name";
            string newrole = "Viewer";

            XDoc usersDoc = new XDoc("user")
                .Elem("email", newemail)
                .Elem("fullname", newfullName)
                .Start("permissions.user")
                    .Elem("role", newrole)
                .End();

            // Replace random contributor's information with new information (by name)
            msg = p.At("users", "=" + name).Put(usersDoc);
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "User update failed (by name)");

            // Assert returned PUT method document metadata is consistent with the XML request information
            Assert.IsTrue(msg.ToDocument()["username"].AsText == name, "Name does not match that in return document");
            Assert.IsTrue(msg.ToDocument()["email"].AsText == newemail, "Email does not match that in return document");
            Assert.IsTrue(msg.ToDocument()["fullname"].AsText == newfullName, "Full name does not match that in return document");
            Assert.IsTrue(msg.ToDocument()["permissions.user/role"].AsText == newrole, "User permissions does not match that in return document");

            // Same replacement operation, but by ID
            msg = p.At("users", id).Put(usersDoc);
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "User update failed (by ID)");

            // Same consistency check as above
            Assert.IsTrue(msg.ToDocument()["username"].AsText == name, "Name does not match that in return document");
            Assert.IsTrue(msg.ToDocument()["email"].AsText == newemail, "Email does not match that in return document");
            Assert.IsTrue(msg.ToDocument()["fullname"].AsText == newfullName, "Full name does not match that in return document");
            Assert.IsTrue(msg.ToDocument()["permissions.user/role"].AsText == newrole, "User permissions does not match that in return document");
        }

        /// <summary>
        ///     Authenticate a user via username:password credentials
        /// </summary>        
        /// <feature>
        /// <name>POST:users/authenticate</name>
        /// <uri>http://developer.mindtouch.com/en/ref/MindTouch_API/POST%3ausers%2f%2fauthenticate</uri>
        /// </feature>
        /// <expected>200 OK HTTP response</expected>

        [Test]
        public void PostUsersAuthenticate() {
            // Build plug and authenticate user with username:password credentials
            Plug p = Utils.BuildPlugForAnonymous().WithCredentials(Utils.Settings.UserName, Utils.Settings.Password);

            // Assert succesful authentication and retrieve authtoken for user
            DreamMessage msg = p.At("users", "authenticate").PostAsync().Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Failed to authenticate user");
        }

        /// <summary>
        ///     Authenticate a user by providing API key
        /// </summary>        
        /// <feature>
        /// <name>GET:users/authenticate</name>
        /// <uri>http://developer.mindtouch.com/en/ref/MindTouch_API/GET%3ausers%2f%2fauthenticate</uri>
        /// </feature>
        /// <expected>Impersonated user matches one authenticated</expected>

        [Test]
        public void Can_impersonate_user() {
            // Retrieve authtoken for a given user by only providing the API key
            var msg = Utils.Settings.Server
                .At("users", "authenticate")
                .WithCredentials(Utils.Settings.UserName, null)
                .With("apikey", Utils.Settings.ApiKey)
                .Get(new Result<DreamMessage>()).Wait();

            // Retrieve current logged user metadata
            msg = Utils.Settings.Server.At("users", "current").Get(new Result<DreamMessage>()).Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Failed to retrieve current user data");

            // Assert name of user impersonated matches name returned in current user metadata
            Assert.AreEqual(Utils.Settings.UserName.ToLower(), msg.ToDocument()["username"].AsText.ToLower(), "Current user does not match impersonated user!");
        }

        /// <summary>
        ///     Attempt to authenticate without a password or API key
        /// </summary>        
        /// <feature>
        /// <name>GET:users/authenticate</name>
        /// <uri>http://developer.mindtouch.com/en/ref/MindTouch_API/GET%3ausers%2f%2fauthenticate</uri>
        /// </feature>
        /// <expected>401 Unauthorized HTTP response</expected>

        [Test]
        public void Cannot_authenticate_without_password() {
            // Attempt to authenticate a user without a password or API key
            var msg = Utils.Settings.Server
                .At("users", "authenticate")
                .WithCredentials(Utils.Settings.UserName, null)
                .Get(new Result<DreamMessage>()).Wait();

            // Assert Unauthorized response returned
            Assert.AreEqual(DreamStatus.Unauthorized, msg.Status, "Returned unexpected HTTP response!");
        }

        /// <summary>
        ///     Check allowed contributor READ, LOGIN permissions against a list of pages
        /// </summary>        
        /// <feature>
        /// <name>POST:users/{userid}/allowed</name>
        /// <uri>http://developer.mindtouch.com/en/ref/MindTouch_API/POST%3apages%2f%2f%7Bpageid%7D%2f%2fallowed</uri>
        /// </feature>
        /// <expected>200 OK HTTP response</expected>

        [Test]
        public void PostAllowed() {
            // Build ADMIN plug
            Plug p = Utils.BuildPlugForAdmin();

            // Create a random contributor
            string id = null;
            string name = null;
            DreamMessage msg = UserUtils.CreateRandomContributor(p, out id, out name);

            // Create a random page
            string pageid = null;
            msg = PageUtils.CreateRandomPage(p, out pageid);

            // XML document with a list of pages to run against the 'allowed' feature
            XDoc pagesDoc = new XDoc("pages")
                .Start("page")
                    .Attr("id", pageid)
                .End();

            // Check to see if user has LOGIN, READ permissions for the pages on the list
            msg = p.At("users", id, "allowed")
                .With("operations", "LOGIN,READ")
                .With("verbose", "false")
                .Post(pagesDoc);
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Allowed retrieval failed");
        }

        /// <summary>
        ///     Retrieve a user's feed
        /// </summary>        
        /// <feature>
        /// <name>GET:users/{userid}/feed</name>
        /// <uri>http://developer.mindtouch.com/en/ref/MindTouch_API/GET%3ausers%2f%2f%7Buserid%7D%2f%2ffeed</uri>
        /// </feature>
        /// <expected>200 OK HTTP response</expected>

        [Test]
        public void GetFeed() {
            // Build ADMIN plug
            Plug p = Utils.BuildPlugForAdmin();

            // Create a random contributor
            string id = null;
            string name = null;
            DreamMessage msg = UserUtils.CreateRandomContributor(p, out id, out name);

            // Retrieve the user's feed
            msg = p.At("users", id, "feed").Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "User's feed retrieval failed");
        }

        /// <summary>
        ///     Retrieve a user's favories feed
        /// </summary>        
        /// <feature>
        /// <name>GET:users/{userid}/favorites/feed</name>
        /// <uri>http://developer.mindtouch.com/en/ref/MindTouch_API/GET%3ausers%2f%2f%7buserid%7d%2f%2ffavorites%2f%2ffeed</uri>
        /// </feature>
        /// <expected>200 OK HTTP response</expected>

        [Test]
        public void GetFavoritesFeed() {
            // Build ADMIN plug
            Plug p = Utils.BuildPlugForAdmin();

            // Create random contributor
            string id = null;
            string name = null;
            DreamMessage msg = UserUtils.CreateRandomContributor(p, out id, out name);

            // Retrieve the user's favorites feed
            msg = p.At("users", id, "favorites", "feed").Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "User's favorites feed retrieval failed");
        }

        /// <summary>
        ///     Retrieve a user's favorite pages
        /// </summary>        
        /// <feature>
        /// <name>GET:users/{userid}/favorites</name>
        /// <uri>http://developer.mindtouch.com/en/ref/MindTouch_API/GET%3ausers%2f%2f%7buserid%7d%2f%2ffavorites</uri>
        /// </feature>
        /// <expected>200 OK HTTP response</expected>

        [Test]
        public void GetFavoritesPages() {
            // Build ADMIN plug
            Plug p = Utils.BuildPlugForAdmin();

            // Create random contributor
            string id = null;
            string name = null;
            DreamMessage msg = UserUtils.CreateRandomContributor(p, out id, out name);

            // Retrieve user's favorite pages (should be 0 for a new user)
            msg = p.At("users", id, "favorites").Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "User's favorites retrieval failed");
            Assert.AreEqual(0, msg.ToDocument()["@count"].AsInt, "New user has favorite pages?!");
        }

        /// <summary>
        ///     Update user information through Contributor (non-admin) account
        /// </summary>        
        /// <feature>
        /// <name>POST:users</name>
        /// <uri>http://developer.mindtouch.com/en/ref/MindTouch_API/POST%3ausers</uri>
        /// </feature>
        /// <expected>403 Forbidden HTTP response</expected>

        [Test]
        public void RenameUser() {
            // Build ADMIN plug
            Plug p = Utils.BuildPlugForAdmin();

            // Create a random contributor
            string username = null;
            string userid = null;
            DreamMessage msg = UserUtils.CreateRandomContributor(p, out userid, out username);

            // Define user information and generate user XML document
            string name = Utils.GenerateUniqueName();
            string email = "newuser1@mindtouch.com";
            string fullName = "newuser1's full name";
            string role = "Contributor";

            XDoc usersDoc = new XDoc("user").Attr("id", userid)
                .Elem("username", name)
                .Elem("email", email)
                .Elem("fullname", fullName)
                .Start("permissions.user")
                    .Elem("role", role)
                .End();

            // Update the user information and assert response document returns correct name 
            msg = p.At("users", userid).PutAsync(usersDoc).Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "User update failed");
            Assert.IsTrue(msg.ToDocument()["username"].AsText == name, "Response document name does not match generated name");
            Assert.AreEqual(msg.ToDocument()["page.home/title"].AsText, fullName, "new user homepage displayname does not match");

            // Retrieve user XML document by ID and assert document returns correct name
            msg = p.At("users", userid).Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "User retrieval failed");
            Assert.IsTrue(msg.ToDocument()["username"].AsText == name, "Name of retrieved user does not match generated name");

            // Build plug for user
            p = Utils.BuildPlugForUser(name, "password");

            try {
                // Create user XML document and attempt to update user
                usersDoc = new XDoc("user").Attr("id", userid)
                    .Elem("username", Utils.GenerateUniqueName())
                    .Elem("email", email)
                    .Elem("fullname", fullName)
                    .Start("permissions.user")
                        .Elem("role", role)
                    .End();
                msg = p.At("users").Post(usersDoc);

                // Should not get here
                Assert.IsTrue(false, "User succeeded in updating self?!");
            } catch(MindTouch.Dream.DreamResponseException ex) {
                // Assert 'Forbidden' response returned
                Assert.IsTrue(ex.Response.Status == DreamStatus.Forbidden, "HTTP response other than 'Forbidden' returned!");
            }
        }

        /// <summary>
        ///     Update user with homepage
        /// </summary>        
        /// <feature>
        /// <name>POST:users</name>
        /// <uri>http://developer.mindtouch.com/en/ref/MindTouch_API/POST%3ausers</uri>
        /// </feature>
        /// <expected>User homepage moves to new user name. Old user homepage redirects to new one.</expected>

        [Test]
        public void RenameUserWithHomePage() {
            //Actions:
            // Create User
            // Create a home page for the user
            // Rename user
            //Expected result: 
            // Homepage must change path

            // Build ADMIN plug
            Plug p = Utils.BuildPlugForAdmin();

            // Create a random contributor
            string username = null;
            string userid = null;
            DreamMessage msg = UserUtils.CreateRandomUser(p, "Contributor", "password", out userid, out username);

            // Create homepage by logging in.
            p.At("users", "authenticate").WithCredentials(username, "password").Get();
            string homepageContent = "This is a homepage for " + username;
            PageUtils.SavePage(p, "User:" + username, homepageContent);

            // Retrieve userpage content
            msg = p.At("pages", "=" + XUri.DoubleEncode("User:" + username), "contents").Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Userpage retrieval failed");
            Assert.IsTrue(msg.ToDocument()["body"].AsText == homepageContent, "Retrieved content does not match generated content!");

            // Define user information and generate user XML document
            string newname = Utils.GenerateUniqueName();
            string email = "newuser1@mindtouch.com";
            string fullName = "newuser1's full name";
            string role = "Contributor";

            XDoc usersDoc = new XDoc("user").Attr("id", userid)
                .Elem("username", newname)
                .Elem("email", email)
                .Elem("fullname", fullName)
                .Start("permissions.user")
                    .Elem("role", role)
                .End();

            // Build ADMIN plug
            p = Utils.BuildPlugForAdmin();

            // Update (rename) user
            msg = p.At("users").PostAsync(usersDoc).Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Rename user failed");

            // Validate new page
            msg = p.At("pages", "=" + XUri.DoubleEncode("User:" + newname), "contents").Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Userpage (new) content retrieval failed!");
            Assert.IsTrue(msg.ToDocument()["body"].AsText == homepageContent, "Retrieved userpage (new) content does not match generated content!");

            // Wait for redirect to complete
            Assert.IsTrue(Wait.For(() => {
                msg = PageUtils.GetPage(Utils.BuildPlugForAdmin(), "User:" + username);
                return (msg.IsSuccessful);
            },
             TimeSpan.FromSeconds(10)),
             "unable to find redirect");

            // Validate old page contents
            msg = p.At("pages", "=" + XUri.DoubleEncode("User:" + username), "contents").Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Userpage (old) content retrieval failed!");
            Assert.IsTrue(msg.ToDocument()["body"].AsText == homepageContent, "Retrieved userpage (old) content does not match generated content!");

            // Validate old redirected info
            msg = p.At("pages", "=" + XUri.DoubleEncode("User:" + username)).Get();
            string redirectedfrom = msg.ToDocument()["page.redirectedfrom/page/path"].AsText;
            Assert.IsTrue(redirectedfrom.EndsWith(username), "Old redirected page is invalid");
        }

        /// <summary>
        ///     Rename user to a new name in which User:newname page already exists (despite no user with name newname existing)
        /// </summary>        
        /// <feature>
        /// <name>POST:users</name>
        /// <uri>http://developer.mindtouch.com/en/ref/MindTouch_API/POST%3ausers</uri>
        /// </feature>
        /// <expected>409 Conflict HTTP response</expected>

        [Test]
        public void RenameUserWithHomePageConflict() {
            //Actions:
            // Create User
            // Generate new name for user
            // Create a page with same name as a new name for the user
            // Try to rename user
            //Expected result: 
            // Conflict

            // Build ADMIN plug
            Plug p = Utils.BuildPlugForAdmin();

            // Create a random contributor
            string username = null;
            string userid = null;
            DreamMessage msg = UserUtils.CreateRandomContributor(p, out userid, out username);

            // Create a new name and a userpage for the new name and contributor
            string name = Utils.GenerateUniqueName();
            PageUtils.SavePage(p, "User:" + name, "This is a page");
            PageUtils.SavePage(p, "User:" + username, "This is a homepage");

            // Define user information and generate user XML document
            string email = "newuser1@mindtouch.com";
            string fullName = "newuser1's full name";
            string role = "Contributor";

            XDoc usersDoc = new XDoc("user").Attr("id", userid)
                .Elem("username", name)
                .Elem("email", email)
                .Elem("fullname", fullName)
                .Start("permissions.user")
                    .Elem("role", role)
                .End();
            try {
                // Attempt to update user to name with existing userpage
                msg = p.At("users").Post(usersDoc);

                // Should not get here
                Assert.IsTrue(false, "User update to name with existing userpage succeeded?!");
            } catch(MindTouch.Dream.DreamResponseException ex) {
                Assert.IsTrue(ex.Response.Status == DreamStatus.Conflict, "HTTP response other than 'Conflict' returned");
            }
        }

        /// <summary>
        ///     Create a user, create a userpage, rename user, create a new user with same name as original user, create a userpage for new user
        /// </summary>        
        /// <feature>
        /// <name>POST:users</name>
        /// <uri>http://developer.mindtouch.com/en/ref/MindTouch_API/POST%3ausers</uri>
        /// </feature>
        /// <expected>New user userpage successfully created with correct content</expected>

        [Test]
        public void RenameUserWithHomePageAndCreateNewUserWithSameName() {
            //Actions:
            // Create User
            // Create a home page for the user
            // Rename user
            // Create a new user with name same as an old user
            // Create a home page for the new user
            //Expected result: 
            // Homepage for new user must have correct content

            // Build ADMIN plug
            Plug p = Utils.BuildPlugForAdmin();

            // Create random contributor
            string oldname = null;
            string userid = null;
            DreamMessage msg = UserUtils.CreateRandomUser(p, "Contributor", "password", out userid, out oldname);

            // Create homepage by logging in.
            p = Utils.BuildPlugForUser(oldname, "password");

            // Create content for userpage
            string homepageContent = "This is a homepage for " + oldname;
            PageUtils.SavePage(p, "User:" + oldname, homepageContent);

            // Retrieve userpage contents
            msg = p.At("pages", "=" + XUri.DoubleEncode("User:" + oldname), "contents").Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Userpage retrieval failed!");
            Assert.IsTrue(msg.ToDocument()["body"].AsText == homepageContent, "Retrieved content does not match generated content!");

            // Define new user information and generate user XML document
            string newname = Utils.GenerateUniqueName();
            string email = "newuser1@mindtouch.com";
            string fullName = "newuser1's full name";
            string role = "Contributor";

            XDoc usersDoc = new XDoc("user").Attr("id", userid)
                .Elem("username", newname)
                .Elem("email", email)
                .Elem("fullname", fullName)
                .Start("permissions.user")
                    .Elem("role", role)
                .End();

            // Log in as ADMIN again
            p = Utils.BuildPlugForAdmin();

            // Update the user
            msg = p.At("users").Post(usersDoc);
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "User update failed!");

            // Let move propogate
            Wait.For(() => {
                return false;
            }, TimeSpan.FromMilliseconds(500));

            // Create a user with same name as old user before update
            msg = UserUtils.CreateUser(p, "Contributor", "password", out userid, oldname);

            // Log in as newly created user
            p = Utils.BuildPlugForUser(oldname, "password");

            // Create content for userpage
            homepageContent = "This is a homepage for new user with name " + oldname;
            PageUtils.SavePage(p, "User:" + oldname, homepageContent);

            // Retrieve userpage content
            msg = p.At("pages", "=" + XUri.DoubleEncode("User:" + oldname), "contents").Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Userpage content retrieval failed!");
            Assert.IsTrue(msg.ToDocument()["body"].AsText == homepageContent, "Retrieved userpage content does not match generated content!");
        }

        [Test]
        public void RenameUserAndRenameBack() {
            //Actions:
            // Create User A
            // Rename user to B
            // Rename back to A
            //Expected result: 
            // Success

            // Build ADMIN plug
            Plug p = Utils.BuildPlugForAdmin();

            // Create random contributor
            string oldname = null;
            string userid = null;
            DreamMessage msg = UserUtils.CreateRandomUser(p, "Contributor", "password", out userid, out oldname);

            // rename the user
            XDoc usersDoc = new XDoc("user").Attr("id", userid)
                .Elem("username", Utils.GenerateUniqueName());
            msg = p.At("users", userid).Put(usersDoc);
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "User rename failed!");

            // rename the user back to original name
            usersDoc = new XDoc("user").Attr("id", userid)
                .Elem("username", oldname);
            msg = p.At("users", userid).Put(usersDoc);
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "User rename failed!");
        }


        /// <summary>
        ///     Test user sorting
        /// </summary>        
        /// <feature>
        /// <name>GET:users</name>
        /// <uri>http://developer.mindtouch.com/en/ref/MindTouch_API/GET%3ausers</uri>
        /// <parameter>usernamefilter</parameter>
        /// </feature>
        /// <expected>Users correctly sorted ascending/descending by name, role, email, nick, fullname, last login</expected>

        [Test]
        public void TestGetUsersSorting() {

            // Build ADMIN plug
            Plug p = Utils.BuildPlugForAdmin();

            // Create 2 users with Contributor role
            string id = null;
            string usernameFilter = Utils.GenerateUniqueName();
            UserUtils.CreateUser(p, "Contributor", "password", out id, "+0" + usernameFilter + Utils.GenerateUniqueName());
            UserUtils.CreateUser(p, "Contributor", "password", out id, "-0" + usernameFilter + Utils.GenerateUniqueName());

            // Create 4 more users, each with a unique role
            string uniqueRoleName = null;
            UserUtils.CreateRandomRole(p, out uniqueRoleName);
            UserUtils.CreateUser(p, uniqueRoleName, "password", out id, Utils.GenerateUniqueName() + usernameFilter);
            UserUtils.CreateRandomRole(p, out uniqueRoleName);
            UserUtils.CreateUser(p, uniqueRoleName, "password", out id, Utils.GenerateUniqueName() + usernameFilter);
            UserUtils.CreateRandomRole(p, out uniqueRoleName);
            UserUtils.CreateUser(p, uniqueRoleName, "password", out id, Utils.GenerateUniqueName() + usernameFilter);
            UserUtils.CreateRandomRole(p, out uniqueRoleName);
            UserUtils.CreateUser(p, uniqueRoleName, "password", out id, Utils.GenerateUniqueName() + usernameFilter);

            // Filter the above users from existing users and check for correct ascending/descending sorting
            // as defined by the "TestSortingOfDocByField" method 
            Utils.TestSortingOfDocByField(p.At("users").With("usernamefilter", usernameFilter)
                .With("sortby", "username").Get().ToDocument(), "user", "username", true);
            Utils.TestSortingOfDocByField(p.At("users").With("usernamefilter", usernameFilter)
                .With("sortby", "-username").Get().ToDocument(), "user", "username", false);
            Utils.TestSortingOfDocByField(p.At("users").With("usernamefilter", usernameFilter)
                .With("sortby", "role").Get().ToDocument(), "user", "permissions.user/role", true);
            Utils.TestSortingOfDocByField(p.At("users").With("usernamefilter", usernameFilter)
                .With("sortby", "-role").Get().ToDocument(), "user", "permissions.user/role", false);
            Utils.TestSortingOfDocByField(p.At("users").With("usernamefilter", usernameFilter)
                .With("sortby", "nick").Get().ToDocument(), "user", "nick", true);
            Utils.TestSortingOfDocByField(p.At("users").With("usernamefilter", usernameFilter)
                .With("sortby", "-nick").Get().ToDocument(), "user", "nick", false);
            Utils.TestSortingOfDocByField(p.At("users").With("usernamefilter", usernameFilter)
                .With("sortby", "email").Get().ToDocument(), "user", "email", true);
            Utils.TestSortingOfDocByField(p.At("users").With("usernamefilter", usernameFilter)
                .With("sortby", "-email").Get().ToDocument(), "user", "email", false);
            Utils.TestSortingOfDocByField(p.At("users").With("usernamefilter", usernameFilter)
                .With("sortby", "fullname").Get().ToDocument(), "user", "fullname", true);
            Utils.TestSortingOfDocByField(p.At("users").With("usernamefilter", usernameFilter)
                .With("sortby", "-fullname").Get().ToDocument(), "user", "fullname", false);
            Utils.TestSortingOfDocByField(p.At("users").With("usernamefilter", usernameFilter)
                .With("sortby", "date.lastlogin").Get().ToDocument(), "user", "date.lastlogin", true);
            Utils.TestSortingOfDocByField(p.At("users").With("usernamefilter", usernameFilter)
                .With("sortby", "-date.lastlogin").Get().ToDocument(), "user", "date.lastlogin", false);

            // Sorting by service test moved to XmlAuthTests class

            // Inactivate the last created user
            XDoc usersDoc = new XDoc("user").Attr("id", id)
                .Elem("status", "inactive");
            DreamMessage msg = p.At("users").PostAsync(usersDoc).Wait();
            Assert.AreEqual(msg.Status, DreamStatus.Ok);

            // Perform the sort check once more
            Utils.TestSortingOfDocByField(p.At("users").With("usernamefilter", usernameFilter)
                .With("sortby", "status").Get().ToDocument(), "user", "status", false);
            Utils.TestSortingOfDocByField(p.At("users").With("usernamefilter", usernameFilter)
                .With("sortby", "-status").Get().ToDocument(), "user", "status", true);
        }

        /// <summary>
        ///     Retrieve users through filters
        /// </summary>        
        /// <feature>
        /// <name>GET:users</name>
        /// <uri>http://developer.mindtouch.com/en/ref/MindTouch_API/GET%3ausers</uri>
        /// <parameter>usernamefilter</parameter>
        /// <parameter>usernameemailfilter</parameter>
        /// <parameter>rolefilter</parameter>
        /// </feature>
        /// <expected>Users filtered correctly</expected>

        [Test]
        public void GetUsersWithFilters() {
            //Actions:
            // Create User1 with unique name and email and role
            // Try to get user through username filter
            // Try to get user through usernameemail filter
            // Try to get user through role filter
            //Expected result: 
            // All operations are correct

            // Build ADMIN plug
            Plug p = Utils.BuildPlugForAdmin();

            // Create a unique role
            string uniqueRoleName = null;
            DreamMessage msg = UserUtils.CreateRandomRole(p, out uniqueRoleName);
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Role creation failed!");

            // Create a user with the role
            string id = null;
            string uniqueUserName = Utils.GenerateUniqueName();
            string uniqueUserMail = "mail" + Utils.GenerateUniqueName() + "@mindtouch.com";
            UserUtils.CreateUser(p, uniqueRoleName, "password", out id, uniqueUserName, uniqueUserMail);

            // Filter in user by username 
            msg = p.At("users").With("usernamefilter", uniqueUserName).Get();
            Assert.AreEqual(msg.ToDocument()["user/username"].AsText, uniqueUserName, "User name filtered does not match generated username!");
            Assert.AreEqual(msg.ToDocument()["user/email"].AsText, uniqueUserMail, "User email filtered does not match generated email!");

            // Filter in user by username through different query 
            msg = p.At("users").With("usernameemailfilter", uniqueUserName).Get();
            Assert.AreEqual(msg.ToDocument()["user/username"].AsText, uniqueUserName, "User name filtered does not match generated username!");
            Assert.AreEqual(msg.ToDocument()["user/email"].AsText, uniqueUserMail, "User email filtered does not match generated email!");

            // Filter in user by email
            msg = p.At("users").With("usernameemailfilter", uniqueUserMail).Get();
            Assert.AreEqual(msg.ToDocument()["user/username"].AsText, uniqueUserName, "User name filtered does not match generated username!");
            Assert.AreEqual(msg.ToDocument()["user/email"].AsText, uniqueUserMail, "User email filtered does not match generated email!");

            // Filter in user by role
            msg = p.At("users").With("rolefilter", uniqueRoleName).Get();
            Assert.AreEqual(msg.ToDocument()["user/username"].AsText, uniqueUserName, "User name filtered does not match generated username!");
            Assert.AreEqual(msg.ToDocument()["user/email"].AsText, uniqueUserMail, "User email filtered does not match generated email!");
        }


        [Test]
        public void Can_impersonate_user_via_userid_with_instance_apikey() {
            var p = Utils.BuildPlugForAdmin();
            string userid;
            string username;
            UserUtils.CreateRandomUser(p, "Viewer", out userid, out username);
            var ts = DateTime.UtcNow.ToEpoch();
            var authhash = StringUtil.ComputeHashString(userid + ":" + ts + ":" + Utils.Settings.InstanceApiKey, Encoding.UTF8);
            var authtoken = "imp_" + ts + "_" + authhash + "_" + userid;
            var currentUser = Utils.Settings.Server
                .WithCookieJar(new DreamCookieJar())
                .At("users", "current")
                .WithHeader("X-Authtoken", authtoken)
                .Get().ToDocument();
            Assert.AreEqual(username, currentUser["username"].AsText);
            Assert.AreEqual(userid, currentUser["@id"].AsText);
        }

        [Test]
        public void Can_impersonate_user_via_userid_with_master_apikey() {
            var p = Utils.BuildPlugForAdmin();
            string userid;
            string username;
            UserUtils.CreateRandomUser(p, "Viewer", out userid, out username);
            var ts = DateTime.UtcNow.ToEpoch();
            var authhash = StringUtil.ComputeHashString(userid + ":" + ts + ":" + Utils.Settings.ApiKey, Encoding.UTF8);
            var authtoken = "imp_" + ts + "_" + authhash + "_" + userid;
            var currentUser = Utils.Settings.Server
                .WithCookieJar(new DreamCookieJar())
                .At("users", "current")
                .WithHeader("X-Authtoken", authtoken)
                .Get().ToDocument();
            Assert.AreEqual(username, currentUser["username"].AsText);
            Assert.AreEqual(userid, currentUser["@id"].AsText);
        }

        [Test]
        public void Can_impersonate_user_via_username_with_instance_apikey() {
            var p = Utils.BuildPlugForAdmin();
            string userid;
            string username;
            UserUtils.CreateRandomUser(p, "Viewer", out userid, out username);
            var ts = DateTime.UtcNow.ToEpoch();
            var authhash = StringUtil.ComputeHashString(username + ":" + ts + ":" + Utils.Settings.InstanceApiKey, Encoding.UTF8);
            var authtoken = "imp_" + ts + "_" + authhash + "_=" + username;
            var currentUser = Utils.Settings.Server
                .WithCookieJar(new DreamCookieJar())
                .At("users", "current")
                .WithHeader("X-Authtoken", authtoken)
                .Get().ToDocument();
            Assert.AreEqual(username, currentUser["username"].AsText);
            Assert.AreEqual(userid, currentUser["@id"].AsText);
        }

        [Test]
        public void Can_impersonate_user_via_username_with_master_apikey() {
            var p = Utils.BuildPlugForAdmin();
            string userid;
            string username;
            UserUtils.CreateRandomUser(p, "Viewer", out userid, out username);
            var ts = DateTime.UtcNow.ToEpoch();
            var authhash = StringUtil.ComputeHashString(username + ":" + ts + ":" + Utils.Settings.ApiKey, Encoding.UTF8);
            var authtoken = "imp_" + ts + "_" + authhash + "_=" + username;
            var currentUser = Utils.Settings.Server
                .WithCookieJar(new DreamCookieJar())
                .At("users", "current")
                .WithHeader("X-Authtoken", authtoken)
                .Get().ToDocument();
            Assert.AreEqual(username, currentUser["username"].AsText);
            Assert.AreEqual(userid, currentUser["@id"].AsText);
        }

        [Test]
        public void Can_impersonate_user_via_username_with_master_apikey_users_authenticate() {
            var p = Utils.BuildPlugForAdmin();
            string userid;
            string username;
            UserUtils.CreateRandomUser(p, "Viewer", out userid, out username);
            var ts = DateTime.UtcNow.ToEpoch();
            var authhash = StringUtil.ComputeHashString(username + ":" + ts + ":" + Utils.Settings.ApiKey, Encoding.UTF8);
            var impAuthtoken = "imp_" + ts + "_" + authhash + "_=" + username;
            var authToken = Utils.Settings.Server
                .WithCookieJar(new DreamCookieJar())
                .At("users", "authenticate")
                .With("authtoken", impAuthtoken)
                .Get().AsText();

            var currentUser = Utils.Settings.Server
                .WithCookieJar(new DreamCookieJar())
                .At("users", "current")
                .WithHeader("X-Authtoken", authToken)
                .Get().ToDocument();

            Assert.AreEqual(username, currentUser["username"].AsText);
            Assert.AreEqual(userid, currentUser["@id"].AsText);
        }

        [Test]
        public void Can_impersonate_user_via_username_with_master_apikey_forced_authenticate() {
            var p = Utils.BuildPlugForAdmin();
            string userid;
            string username;
            UserUtils.CreateRandomUser(p, "Viewer", out userid, out username);
            var ts = DateTime.UtcNow.ToEpoch();
            var authhash = StringUtil.ComputeHashString(username + ":" + ts + ":" + Utils.Settings.ApiKey, Encoding.UTF8);
            var impAuthtoken = "imp_" + ts + "_" + authhash + "_=" + username;

            var currentUser = Utils.Settings.Server
                .WithCookieJar(new DreamCookieJar())
                .At("users", "current")
                .With("authenticate", true)
                .WithHeader("X-Authtoken", impAuthtoken)
                .Get().ToDocument();

            Assert.AreEqual(username, currentUser["username"].AsText);
            Assert.AreEqual(userid, currentUser["@id"].AsText);
        }

        [Ignore("http://youtrack.developer.mindtouch.com/issue/MT-9406; Anonymous user is returned instead of expected user")]
        [Test]
        public void Can_impersonate_user_via_username_with_master_apikey_users_authenticate_with_redirect() {
            var p = Utils.BuildPlugForAdmin();
            string userid;
            string username;
            UserUtils.CreateRandomUser(p, "Viewer", out userid, out username);
            var ts = DateTime.UtcNow.ToEpoch();
            var authhash = StringUtil.ComputeHashString(username + ":" + ts + ":" + Utils.Settings.ApiKey, Encoding.UTF8);
            var impAuthtoken = "imp_" + ts + "_" + authhash + "_=" + username;
            var currentUser = Utils.Settings.Server
                .WithCookieJar(new DreamCookieJar())
                .At("users", "authenticate")
                .With("authtoken", impAuthtoken)
                .With("redirect", p.At("users", "current").ToString())
                .Get().ToDocument();

            Assert.AreEqual(username, currentUser["username"].AsText);
            Assert.AreEqual(userid, currentUser["@id"].AsText);
        }

        [Test]
        public void Can_filter_page_ids_for_user() {

            // Build ADMIN plug
            var p = Utils.BuildPlugForAdmin();

            // Create a random contributor
            string id;
            string name;
            UserUtils.CreateRandomContributor(p, out id, out name);

            // Create a random page
            string id1;
            PageUtils.CreateRandomPage(p, out id1);
            string id2;
            PageUtils.CreateRandomPage(p, out id2);
            var securityDoc = new XDoc("security")
                .Start("permissions.page")
                    .Elem("restriction", "Private")
                .End();
            p.At("pages", id2, "security").Post(securityDoc);
            string id3;
            PageUtils.CreateRandomPage(p, out id3);

            // XML document with a list of pages to run against the 'allowed' feature
            var pagesDoc = new XDoc("pages")
                .Start("page").Attr("id", id3).End()
                .Start("page").Attr("id", id2).End()
                .Start("page").Attr("id", id1).End();
            var msg = p.At("users", id, "allowed")
                .With("operations", "READ")
                .With("verbose", "false")
                .Post(pagesDoc, new Result<DreamMessage>()).Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Allowed retrieval failed");
            var filtered = msg.ToDocument();
            _log.DebugFormat("------------ submitted page doc\r\n{0}", pagesDoc.ToPrettyString());
            _log.DebugFormat("------------ received page doc\r\n{0}", filtered.ToPrettyString());
            Assert.AreEqual(new[] { id3, id1 }, filtered["page/@id"].Select(x => x.Contents).ToArray(), "wrong page id's returned");
        }

        [Test]
        public void Can_get_filtered_page_ids_for_user() {

            // Build ADMIN plug
            var p = Utils.BuildPlugForAdmin();

            // Create a random contributor
            string id;
            string name;
            UserUtils.CreateRandomContributor(p, out id, out name);

            // Create a random page
            string id1;
            PageUtils.CreateRandomPage(p, out id1);
            string id2;
            PageUtils.CreateRandomPage(p, out id2);
            var securityDoc = new XDoc("security")
                .Start("permissions.page")
                    .Elem("restriction", "Private")
                .End();
            p.At("pages", id2, "security").Post(securityDoc);
            string id3;
            PageUtils.CreateRandomPage(p, out id3);

            // XML document with a list of pages to run against the 'allowed' feature
            var pagesDoc = new XDoc("pages")
                .Start("page").Attr("id", id3).End()
                .Start("page").Attr("id", id2).End()
                .Start("page").Attr("id", id1).End();
            var msg = p.At("users", id, "allowed")
                .With("operations", "READ")
                .With("invert", "true")
                .Post(pagesDoc, new Result<DreamMessage>()).Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Allowed retrieval failed");
            var filtered = msg.ToDocument();
            _log.DebugFormat("------------ submitted page doc\r\n{0}", pagesDoc.ToPrettyString());
            _log.DebugFormat("------------ received page doc\r\n{0}", filtered.ToPrettyString());
            Assert.AreEqual(new[] { id2 }, filtered["page/@id"].Select(x => x.Contents).ToArray(), "wrong page id's returned");
        }

        [Test]
        public void Filtered_out_pages_filtered_for_user_preserve_order_in_non_verbose_mode() {

            // Build ADMIN plug
            var p = Utils.BuildPlugForAdmin();

            // Create a random contributor
            string id;
            string name;
            UserUtils.CreateRandomContributor(p, out id, out name);

            // Create a random page
            string id1;
            PageUtils.CreateRandomPage(p, out id1);
            var securityDoc = new XDoc("security")
                .Start("permissions.page")
                    .Elem("restriction", "Private")
                .End();
            p.At("pages", id1, "security").Post(securityDoc);
            string id2;
            PageUtils.CreateRandomPage(p, out id2);
            string id3;
            PageUtils.CreateRandomPage(p, out id3);
            p.At("pages", id3, "security").Post(securityDoc);

            // XML document with a list of pages to run against the 'allowed' feature
            var pagesDoc = new XDoc("pages")
                .Start("page").Attr("id", id3).End()
                .Start("page").Attr("id", id2).End()
                .Start("page").Attr("id", id1).End();
            var msg = p.At("users", id, "allowed")
                .With("operations", "READ")
                .With("invert", "true")
                .Post(pagesDoc, new Result<DreamMessage>()).Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Allowed retrieval failed");
            var filtered = msg.ToDocument();
            _log.DebugFormat("------------ submitted page doc\r\n{0}", pagesDoc.ToPrettyString());
            _log.DebugFormat("------------ received page doc\r\n{0}", filtered.ToPrettyString());
            Assert.AreEqual(new[] { id3, id1 }, filtered["page/@id"].Select(x => x.Contents).ToArray(), "wrong page id's returned");
        }

        [Test]
        public void Pages_filtered_for_user_preserve_order_in_non_verbose_mode() {

            // Build ADMIN plug
            var p = Utils.BuildPlugForAdmin();

            // Create a random contributor
            string id;
            string name;
            UserUtils.CreateRandomContributor(p, out id, out name);

            // Create a random page
            string id1;
            PageUtils.CreateRandomPage(p, out id1);
            string id2;
            PageUtils.CreateRandomPage(p, out id2);
            string id3;
            PageUtils.CreateRandomPage(p, out id3);

            // XML document with a list of pages to run against the 'allowed' feature
            var pagesDoc = new XDoc("pages")
                .Start("page").Attr("id", id2).End()
                .Start("page").Attr("id", id3).End()
                .Start("page").Attr("id", id1).End();
            var msg = p.At("users", id, "allowed")
                .With("operations", "READ")
                .With("verbose", "false")
                .Post(pagesDoc, new Result<DreamMessage>()).Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Allowed retrieval failed");
            var filtered = msg.ToDocument();
            _log.DebugFormat("------------ submitted page doc\r\n{0}", pagesDoc.ToPrettyString());
            _log.DebugFormat("------------ received page doc\r\n{0}", filtered.ToPrettyString());
            Assert.AreEqual(new[] { id2, id3, id1 }, filtered["page/@id"].Select(x => x.Contents).ToArray(), "wrong page id's returned");
        }

        [Test]
        public void Pages_filtered_for_user_preserve_order_in_verbose_mode() {

            // Build ADMIN plug
            var p = Utils.BuildPlugForAdmin();

            // Create a random contributor
            string id;
            string name;
            UserUtils.CreateRandomContributor(p, out id, out name);

            // Create a random page
            string id1;
            PageUtils.CreateRandomPage(p, out id1);
            string id2;
            PageUtils.CreateRandomPage(p, out id2);
            string id3;
            PageUtils.CreateRandomPage(p, out id3);

            // XML document with a list of pages to run against the 'allowed' feature
            var pagesDoc = new XDoc("pages")
                .Start("page").Attr("id", id2).End()
                .Start("page").Attr("id", id3).End()
                .Start("page").Attr("id", id1).End();
            var msg = p.At("users", id, "allowed")
                .With("operations", "READ")
                .Post(pagesDoc, new Result<DreamMessage>()).Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Allowed retrieval failed");
            var filtered = msg.ToDocument();
            _log.DebugFormat("------------ submitted page doc\r\n{0}", pagesDoc.ToPrettyString());
            _log.DebugFormat("------------ received page doc\r\n{0}", filtered.ToPrettyString());
            Assert.AreEqual(new[] { id2, id3, id1 }, filtered["page/@id"].Select(x => x.Contents).ToArray(), "wrong page id's returned");
        }

        [Test]
        public void Can_get_verbose_page_data_for_user_filtered_page_list() {

            // Build ADMIN plug
            var p = Utils.BuildPlugForAdmin();

            // Create a random contributor
            string id;
            string name;
            UserUtils.CreateRandomContributor(p, out id, out name);

            // Create a random page
            string id1;
            var pageMsg = PageUtils.CreateRandomPage(p, out id1);

            // XML document with a list of pages to run against the 'allowed' feature
            var pagesDoc = new XDoc("pages").Start("page").Attr("id", id1).End();
            var filterMsg = p.At("users", id, "allowed")
                .With("operations", "READ")
                .Post(pagesDoc, new Result<DreamMessage>()).Wait();
            Assert.AreEqual(DreamStatus.Ok, filterMsg.Status, "Allowed retrieval failed");
            Assert.AreEqual(pageMsg.ToDocument()["page"].ToCompactString(), filterMsg.ToDocument()["page"].ToCompactString());
        }

        [Test]
        public void Can_get_use_comma_delimited_body() {

            // Build ADMIN plug
            var p = Utils.BuildPlugForAdmin();

            // Create a random contributor
            string id;
            string name;
            UserUtils.CreateRandomContributor(p, out id, out name);

            // Create a random page
            string id1;
            PageUtils.CreateRandomPage(p, out id1);
            string id2;
            PageUtils.CreateRandomPage(p, out id2);
            var securityDoc = new XDoc("security")
                .Start("permissions.page")
                    .Elem("restriction", "Private")
                .End();
            p.At("pages", id2, "security").Post(securityDoc);
            string id3;
            PageUtils.CreateRandomPage(p, out id3);
            var msg = p.At("users", id, "allowed")
                .With("verbose","false")
                .With("operations", "READ")
                .Post(DreamMessage.Ok(MimeType.TEXT, string.Format("{0},{1},{2}", id3, id2, id1)), new Result<DreamMessage>()).Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Allowed retrieval failed");
            Assert.AreEqual(string.Format("{0},{1}", id3, id1), msg.ToText(), "wrong page id's returned");
        }

        [Test]
        public void Using_text_body_with_verbose_fails() {
            var p = Utils.BuildPlugForAdmin();
            string id;
            string name;
            UserUtils.CreateRandomContributor(p, out id, out name);
            var msg = p.At("users", id, "allowed")
                .With("operations", "READ")
                .Post(DreamMessage.Ok(MimeType.TEXT, "1,2,3"), new Result<DreamMessage>()).Wait();
            Assert.AreEqual(DreamStatus.BadRequest,msg.Status);
            Assert.AreEqual("Cannot specify the verbose output option when the input is not an Xml document.",msg.ToDocument()["message"].Contents);
        }

    }
}
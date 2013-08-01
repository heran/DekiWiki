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
using MindTouch.Web;
using NUnit.Framework;

using MindTouch.Dream;
using MindTouch.Xml;

namespace MindTouch.Deki.Tests.PageTests {
    [TestFixture]
    public class SecurityTests {
        [Test]
        public void GetPageSecurity() {
            // GET:pages/{pageid}/security
            // http://developer.mindtouch.com/Deki/API_Reference/GET%3apages%2f%2f%7bpageid%7d%2f%2fsecurity

            Plug p = Utils.BuildPlugForAdmin();

            string id = null;
            string path = null;
            DreamMessage msg = PageUtils.CreateRandomPage(p, out id, out path);

            msg = p.At("pages", id, "security").Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status);

            PageUtils.DeletePageByID(p, id, true);
        }

        [Test]
        public void SecurityPagePost() {
            // POST:pages/{pageid}/security
            // http://developer.mindtouch.com/Deki/API_Reference/POST%3apages%2f%2f%7bpageid%7d%2f%2fsecurity

            Plug p = Utils.BuildPlugForAdmin();

            string id = null;
            string path = null;
            DreamMessage msg = PageUtils.CreateRandomPage(p, out id, out path);

            msg = p.At("users", "current").Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status);
            string userid = msg.ToDocument()["@id"].AsText;
            Assert.IsFalse(string.IsNullOrEmpty(userid));

            XDoc securityDoc = new XDoc("security")
                .Start("permissions.page")
                    .Elem("restriction", "Private")
                .End()
                .Start("grants.added")
                    .Start("grant")
                        .Start("permissions")
                            .Elem("role", "Contributor")
                        .End()
                        .Start("user").Attr("id", userid).End()
                        .Elem("date.expires", DateTime.Today.AddYears(1))
                    .End()
                .End();
            msg = p.At("pages", id, "security").Post(securityDoc);
            Assert.AreEqual(DreamStatus.Ok, msg.Status);
            Assert.IsTrue(msg.ToDocument()["grants/grant/permissions/role"].AsText == "Contributor");

            PageUtils.DeletePageByID(p, id, true);
        }

        [Test]
        public void SecurityPagePut() {
            // PUT:pages/{pageid}/security
            // http://developer.mindtouch.com/Deki/API_Reference/PUT%3apages%2f%2f%7bpageid%7d%2f%2fsecurity

            Plug p = Utils.BuildPlugForAdmin();

            string id = null;
            string path = null;
            DreamMessage msg = PageUtils.CreateRandomPage(p, out id, out path);

            msg = p.At("users", "current").Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status);
            string userid = msg.ToDocument()["@id"].AsText;
            Assert.IsFalse(string.IsNullOrEmpty(userid));

            XDoc securityDoc = new XDoc("security")
                .Start("permissions.page")
                    .Elem("restriction", "Private")
                .End()
                .Start("grants")
                    .Start("grant")
                        .Start("permissions")
                            .Elem("role", "Contributor")
                        .End()
                        .Start("user").Attr("id", userid).End()
                        .Elem("date.expires", DateTime.Today.AddYears(1))
                    .End()
                .End();
            msg = p.At("pages", id, "security").Put(securityDoc);
            Assert.AreEqual(DreamStatus.Ok, msg.Status);
            Assert.IsTrue(msg.ToDocument()["grants/grant/permissions/role"].AsText == "Contributor");

            PageUtils.DeletePageByID(p, id, true);
        }

        [Test]
        public void DeleteSecurity() {
            // DELETE:pages/{pageid}/security
            // http://developer.mindtouch.com/Deki/API_Reference/PUT%3apages%2f%2f%7bpageid%7d%2f%2fsecurity

            Plug p = Utils.BuildPlugForAdmin();

            string id = null;
            DreamMessage msg = PageUtils.CreateRandomPage(p, out id);

            msg = p.At("pages", id, "security").DeleteAsync().Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status);

            PageUtils.DeletePageByID(p, id, true);
        }

        [Test]
        public void TestAbsoluteCascading() {
            //Assumptions: 
            //role 'contributor' exists
            //Actions:
            // Create User1 and User2 
            //User1 is contributor on /A/B/* with absolute cascading
            //User2 is contributor on /A* with absolute cascading
            //Expected result: 
            // A/* including A/B does not have user1 as contributor

            Plug p = Utils.BuildPlugForAdmin();

            string baseTreePath = PageUtils.BuildPageTree(p);

            string userid1 = null;
            DreamMessage msg = UserUtils.CreateRandomContributor(p, out userid1);

            string userid2 = null;
            msg = UserUtils.CreateRandomContributor(p, out userid2);

            XDoc securityDoc = new XDoc("security")
                .Start("permissions.page")
                    .Elem("restriction", "Private")
                .End()
                .Start("grants")
                    .Start("grant")
                        .Start("permissions")
                            .Elem("role", "Contributor")
                        .End()
                        .Start("user").Attr("id", userid1).End()
                    .End()
                .End();

            msg = p.At("pages", "=" + XUri.DoubleEncode(baseTreePath + "/A/B"), "security").
                WithQuery("cascade=absolute").Put(securityDoc);
            Assert.AreEqual(DreamStatus.Ok, msg.Status);

            securityDoc = new XDoc("security")
                .Start("permissions.page")
                    .Elem("restriction", "Private")
                .End()
                .Start("grants")
                    .Start("grant")
                        .Start("permissions")
                            .Elem("role", "Contributor")
                        .End()
                        .Start("user").Attr("id", userid2).End()
                    .End()
                .End();

            msg = p.At("pages", "=" + XUri.DoubleEncode(baseTreePath + "/A"), "security").
                WithQuery("cascade=absolute").Put(securityDoc);
            Assert.AreEqual(DreamStatus.Ok, msg.Status);

            msg = p.At("pages", "=" + XUri.DoubleEncode(baseTreePath + "/A/B"), "security").Get();
            Assert.AreEqual(msg.ToDocument()["permissions.page/restriction"].Contents, "Private");
            Assert.AreEqual(msg.ToDocument()["grants/grant[1]/user/@id"].Contents, userid2);
            Assert.IsTrue(msg.ToDocument()["grants/grant[2]"].IsEmpty);
        }

        [Test]
        public void TestPrivateAndPostSecurity() {
            //Actions:
            // Create user with role "Viewer"
            // Admin sets restriction:private on A/* ; grant to user viewer
            // User adds grant for user viewer on A/B/C 
            //Expected result: 
            // User unable to set the grant

            Plug p = Utils.BuildPlugForAdmin();

            string baseTreePath = PageUtils.BuildPageTree(p);

            string username = null;
            string userid = null;
            DreamMessage msg = UserUtils.CreateRandomUser(p, "Viewer", out userid, out username);

            XDoc securityDoc = new XDoc("security")
                .Start("permissions.page")
                    .Elem("restriction", "Private")
                .End()
                .Start("grants")
                    .Start("grant")
                        .Start("permissions")
                            .Elem("role", "Viewer")
                        .End()
                        .Start("user").Attr("id", userid).End()
                    .End()
                .End();

            msg = p.At("pages", "=" + XUri.DoubleEncode(baseTreePath + "/A"), "security").
                WithQuery("cascade=absolute").Put(securityDoc);
            Assert.AreEqual(DreamStatus.Ok, msg.Status);

            p = Utils.BuildPlugForUser(username, "password");

            securityDoc = new XDoc("security")
                .Start("permissions.page")
                    .Elem("restriction", "Private")
                .End()
                .Start("grants")
                    .Start("grant")
                        .Start("permissions")
                            .Elem("role", "Contributor")
                        .End()
                        .Start("user").Attr("id", userid).End()
                    .End()
                .End();

            try {
                msg = p.At("pages", "=" + XUri.DoubleEncode(baseTreePath + "/A/B/C"), "security").
                    WithQuery("cascade=absolute").Post(securityDoc);
                Assert.IsTrue(false);
            } catch(DreamResponseException ex) {
                Assert.IsTrue(ex.Response.Status == DreamStatus.Forbidden);
            }
        }

        [Test]
        public void TestDeltaCascading() {
            //Assumptions: 
            // Role 'Contributor' exists
            // Role 'Viewer' exists
            //Actions:
            // Create User1, User2 and User3
            // User1 is contributor, User2 is contributor on /A/B* with absolute cascading
            // User3 is viewer on /A* with delta cascading
            //Expected result: 
            // /A/B is private
            // User3 is viewer on /A*
            // User1 and user2 are contributors, user3 is a viewer on /A/B*

            Plug p = Utils.BuildPlugForAdmin();

            string baseTreePath = PageUtils.BuildPageTree(p);

            string userid1 = null;
            DreamMessage msg = UserUtils.CreateRandomContributor(p, out userid1);

            string userid2 = null;
            msg = UserUtils.CreateRandomContributor(p, out userid2);

            string userid3 = null;
            msg = UserUtils.CreateRandomContributor(p, out userid3);

            XDoc securityDoc = new XDoc("security")
                .Start("permissions.page")
                    .Elem("restriction", "Private")
                .End()
                .Start("grants")
                    .Start("grant")
                        .Start("permissions")
                            .Elem("role", "Contributor")
                        .End()
                        .Start("user").Attr("id", userid1).End()
                    .End()
                    .Start("grant")
                        .Start("permissions")
                            .Elem("role", "Contributor")
                        .End()
                        .Start("user").Attr("id", userid2).End()
                    .End()
                    .Start("grant")
                        .Start("permissions")
                            .Elem("role", "Viewer")
                        .End()
                        .Start("user").Attr("id", userid3).End()
                    .End()
                .End();

            msg = p.At("pages", "=" + XUri.DoubleEncode(baseTreePath + "/A/B"), "security").
                WithQuery("cascade=absolute").Put(securityDoc);
            Assert.AreEqual(DreamStatus.Ok, msg.Status);

            securityDoc = new XDoc("security")
                .Start("permissions.page")
                    .Elem("restriction", "Private")
                .End()
                .Start("grants")
                    .Start("grant")
                        .Start("permissions")
                            .Elem("role", "Viewer")
                        .End()
                        .Start("user").Attr("id", userid3).End()
                    .End()
                .End();

            msg = p.At("pages", "=" + XUri.DoubleEncode(baseTreePath + "/A"), "security").
                WithQuery("cascade=delta").Put(securityDoc);
            Assert.AreEqual(DreamStatus.Ok, msg.Status);

            msg = p.At("pages", "=" + XUri.DoubleEncode(baseTreePath + "/A"), "security").Get();
            Assert.AreEqual(msg.ToDocument()["permissions.page/restriction"].Contents, "Private");
            Assert.AreEqual(msg.ToDocument()[string.Format("grants/grant[user/@id=\"{0}\"]/permissions/role", userid3)].Contents, "Viewer");

            msg = p.At("pages", "=" + XUri.DoubleEncode(baseTreePath + "/A/B"), "security").Get();
            Assert.AreEqual(msg.ToDocument()[string.Format("grants/grant[user/@id=\"{0}\"]/permissions/role", userid1)].Contents, "Contributor");
            Assert.AreEqual(msg.ToDocument()[string.Format("grants/grant[user/@id=\"{0}\"]/permissions/role", userid2)].Contents, "Contributor");
            Assert.AreEqual(msg.ToDocument()[string.Format("grants/grant[user/@id=\"{0}\"]/permissions/role", userid3)].Contents, "Viewer");
        }

        [Test]
        public void TestEmptyDeltaCascadesNothing() {
            //Assumptions: 
            // Role 'Contributor' exists
            // Role 'Viewer' exists
            //Actions:
            // Create User1
            // User1 is contributor /A/B and Viewer on /A
            //Expected result: 
            // nothing changed

            Plug p = Utils.BuildPlugForAdmin();

            string baseTreePath = PageUtils.BuildPageTree(p);

            string userid1 = null;
            DreamMessage msg = UserUtils.CreateRandomContributor(p, out userid1);

            XDoc securityDoc = new XDoc("security")
                .Start("permissions.page")
                    .Elem("restriction", "Private")
                .End()
                .Start("grants")
                    .Start("grant")
                        .Start("permissions")
                            .Elem("role", "Contributor")
                        .End()
                        .Start("user").Attr("id", userid1).End()
                    .End()
                .End();

            msg = p.At("pages", "=" + XUri.DoubleEncode(baseTreePath + "/A/B"), "security").
                WithQuery("cascade=absolute").Put(securityDoc);
            Assert.AreEqual(DreamStatus.Ok, msg.Status);

            securityDoc = new XDoc("security")
                .Start("permissions.page")
                    .Elem("restriction", "Private")
                .End()
                .Start("grants")
                    .Start("grant")
                        .Start("permissions")
                            .Elem("role", "Viewer")
                        .End()
                        .Start("user").Attr("id", userid1).End()
                    .End()
                .End();

            msg = p.At("pages", "=" + XUri.DoubleEncode(baseTreePath + "/A"), "security").
                WithQuery("cascade=none").Put(securityDoc);
            Assert.AreEqual(DreamStatus.Ok, msg.Status);

            msg = p.At("pages", "=" + XUri.DoubleEncode(baseTreePath + "/A/B"), "security").Get();
            Assert.AreEqual("Contributor", msg.ToDocument()[string.Format("grants/grant[user/@id=\"{0}\"]/permissions/role", userid1)].Contents);

            securityDoc = new XDoc("security")
                .Start("permissions.page")
                    .Elem("restriction", "Private")
                .End()
                .Start("grants")
                    .Start("grant")
                        .Start("permissions")
                            .Elem("role", "Viewer")
                        .End()
                        .Start("user").Attr("id", userid1).End()
                    .End()
                .End();

            msg = p.At("pages", "=" + XUri.DoubleEncode(baseTreePath + "/A"), "security").
                WithQuery("cascade=delta").Put(securityDoc);
            Assert.AreEqual(DreamStatus.Ok, msg.Status);

            msg = p.At("pages", "=" + XUri.DoubleEncode(baseTreePath + "/A"), "security").Get();
            Assert.AreEqual("Viewer", msg.ToDocument()[string.Format("grants/grant[user/@id=\"{0}\"]/permissions/role", userid1)].Contents);

            msg = p.At("pages", "=" + XUri.DoubleEncode(baseTreePath + "/A/B"), "security").Get();
            Assert.AreEqual("Contributor", msg.ToDocument()[string.Format("grants/grant[user/@id=\"{0}\"]/permissions/role", userid1)].Contents);
        }

        [Test]
        public void TestPostDeltas() {
            //Assumptions: 
            // Role 'Contributor' exists
            // Role 'Viewer' exists
            //Actions:
            // Create User1, User2 and User3
            // User1 is contributor, User2 is contributor on /A/B*
            // User3 is a viewer on A* (grant added)
            //Expected result: 
            // User3 is viewer on /A*
            // User1 and user2 are contributors on /A/B*

            Plug p = Utils.BuildPlugForAdmin();

            string baseTreePath = PageUtils.BuildPageTree(p);

            string userid1 = null;
            DreamMessage msg = UserUtils.CreateRandomContributor(p, out userid1);

            string userid2 = null;
            msg = UserUtils.CreateRandomContributor(p, out userid2);

            string userid3 = null;
            msg = UserUtils.CreateRandomContributor(p, out userid3);

            XDoc securityDoc = new XDoc("security")
                .Start("permissions.page")
                    .Elem("restriction", "Private")
                .End()
                .Start("grants")
                    .Start("grant")
                        .Start("permissions")
                            .Elem("role", "Contributor")
                        .End()
                        .Start("user").Attr("id", userid1).End()
                    .End()
                    .Start("grant")
                        .Start("permissions")
                            .Elem("role", "Contributor")
                        .End()
                        .Start("user").Attr("id", userid2).End()
                    .End()
                .End();

            msg = p.At("pages", "=" + XUri.DoubleEncode(baseTreePath + "/A/B"), "security").
                WithQuery("cascade=absolute").Put(securityDoc);
            Assert.AreEqual(DreamStatus.Ok, msg.Status);

            securityDoc = new XDoc("security")
                .Start("permissions.page")
                    .Elem("restriction", "Private")
                .End()
                .Start("grants.added")
                    .Start("grant")
                        .Start("permissions")
                            .Elem("role", "Viewer")
                        .End()
                        .Start("user").Attr("id", userid3).End()
                    .End()
                .End();

            msg = p.At("pages", "=" + XUri.DoubleEncode(baseTreePath + "/A"), "security").
                WithQuery("cascade=delta").Post(securityDoc);
            Assert.AreEqual(DreamStatus.Ok, msg.Status);

            msg = p.At("pages", "=" + XUri.DoubleEncode(baseTreePath + "/A"), "security").Get();
            Assert.AreEqual(msg.ToDocument()[string.Format("grants/grant[user/@id=\"{0}\"]/permissions/role", userid3)].Contents, "Viewer");

            msg = p.At("pages", "=" + XUri.DoubleEncode(baseTreePath + "/A/B/C"), "security").Get();
            Assert.AreEqual(msg.ToDocument()["permissions.page/restriction"].Contents, "Private");
            Assert.AreEqual(msg.ToDocument()[string.Format("grants/grant[user/@id=\"{0}\"]/permissions/role", userid1)].Contents, "Contributor");
            Assert.AreEqual(msg.ToDocument()[string.Format("grants/grant[user/@id=\"{0}\"]/permissions/role", userid2)].Contents, "Contributor");
            Assert.AreEqual(msg.ToDocument()[string.Format("grants/grant[user/@id=\"{0}\"]/permissions/role", userid3)].Contents, "Viewer");
        }

        [Test]
        public void TestCascadingWithSkipIfUnableToSet() {
            //Assumptions:
            //Actions:
            //  Create user with "Contributor" role
            //  Admin sets restriction:private on A/B
            //  User adds grant for self viewer on A/* 
            //  User sets grant for self viewer on A/*
            //Expected result: 
            //  User is viewer on A, A/B/C, A/B/D, A/E but no change on A/B

            Plug p = Utils.BuildPlugForAdmin();

            string baseTreePath = PageUtils.BuildPageTree(p);

            string userid = null;
            string username = null;
            DreamMessage msg = UserUtils.CreateRandomContributor(p, out userid, out username);

            XDoc securityDoc = new XDoc("security")
                .Start("permissions.page")
                    .Elem("restriction", "Private")
                .End();

            msg = p.At("pages", "=" + XUri.DoubleEncode(baseTreePath + "/A/B"), "security").
                WithQuery("cascade=none").Put(securityDoc);
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "restrict /A/B to private");

            p = Utils.BuildPlugForUser(username, "password");

            securityDoc = new XDoc("security")
                .Start("grants.added")
                    .Start("grant")
                        .Start("permissions")
                            .Elem("role", "Viewer")
                        .End()
                        .Start("user").Attr("id", userid).End()
                    .End()
                .End();

            msg = p.At("pages", "=" + XUri.DoubleEncode(baseTreePath + "/A"), "security").
                WithQuery("cascade=delta").Post(securityDoc);
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "grant viewer on /A/*");

            p = Utils.BuildPlugForAdmin(); // relogin as admin

            msg = p.At("pages", "=" + XUri.DoubleEncode(baseTreePath + "/A"), "security").Get();
            Assert.AreEqual(msg.ToDocument()[string.Format("grants/grant[user/@id=\"{0}\"]/permissions/role", userid)].Contents, "Viewer", "confirm viewer grant on /A");

            msg = p.At("pages", "=" + XUri.DoubleEncode(baseTreePath + "/A/B"), "security").Get();
            Assert.AreEqual(msg.ToDocument()["permissions.page/restriction"].Contents, "Private", "confirm private restriction on /A/B");
            Assert.AreEqual(msg.ToDocument()["grants/grant[2]"].IsEmpty, true, "confirm single grant on /A/B");

            msg = p.At("pages", "=" + XUri.DoubleEncode(baseTreePath + "/A/B/C"), "security").Get();
            var doc = msg.ToDocument();
            Assert.AreEqual(doc[string.Format("grants/grant[user/@id=\"{0}\"]/permissions/role", userid)].Contents, "Viewer", "confirm viewer grant on /A/B/C");
            Assert.AreEqual(string.IsNullOrEmpty(doc["permissions.page/operations"].AsText), true, "confirm no available operations on /A/B/C");

            p = Utils.BuildPlugForUser(username, "password"); // relogin as user

            securityDoc = new XDoc("security")
                .Start("permissions.page")
                    .Elem("restriction", "Private")
                .End()
                .Start("grants")
                    .Start("grant")
                        .Start("permissions")
                            .Elem("role", "Contributor")
                        .End()
                        .Start("user").Attr("id", userid).End()
                    .End()
                .End();

            msg = p.At("pages", "=" + XUri.DoubleEncode(baseTreePath + "/A"), "security").
                WithQuery("cascade=absolute").Put(securityDoc);
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "grant contributor on /A/*");

            p = Utils.BuildPlugForAdmin(); // relogin as admin

            msg = p.At("pages", "=" + XUri.DoubleEncode(baseTreePath + "/A"), "security").Get();
            Assert.AreEqual(msg.ToDocument()[string.Format("grants/grant[user/@id=\"{0}\"]/permissions/role", userid)].Contents, "Contributor", "confirm contributor grant on /A");

            msg = p.At("pages", "=" + XUri.DoubleEncode(baseTreePath + "/A/B"), "security").Get();
            Assert.AreEqual(msg.ToDocument()["permissions.page/restriction"].Contents, "Private", "reconfirm private restriction on /A/B");
            Assert.AreEqual(msg.ToDocument()["grants/grant[2]"].IsEmpty, true, "reconfirm single grant on /A/B");

            msg = p.At("pages", "=" + XUri.DoubleEncode(baseTreePath + "/A/B/C"), "security").Get();
            Assert.AreEqual(msg.ToDocument()[string.Format("grants/grant[user/@id=\"{0}\"]/permissions/role", userid)].Contents, "Contributor", "confirm contributor grant on /A/B/C");
            Assert.AreEqual(msg.ToDocument()["permissions.page/restriction"].Contents, "Private", "confirm private restriction on /A/B/C");
        }

        [Ignore]
        [Test]
        public void TestUserGroupGrantInheritence() {
            //Assumptions:
            //  Role 'None', 'Contributor' exist
            //Actions:
            //  Create user u with "None" role
            //  Create group g with "Contributor" role
            //  Mark page p as private with a grant to group g            
            //Expected result: 
            //  User u has contributor access to page p

            Plug p = Utils.BuildPlugForAdmin();
            string userid, username;
            UserUtils.CreateRandomUser(p, "None", "password", out userid, out username);

            string groupid, groupname;
            UserUtils.CreateRandomGroup(p, "Contributor", new string[] { userid }, out groupid, out groupname);

            string pageid, pagepath;
            PageUtils.CreateRandomPage(p, out pageid, out pagepath);

            XDoc securityDoc = new XDoc("security")
                .Start("permissions.page")
                    .Elem("restriction", "Private")
                .End()
                .Start("grants")
                    .Start("grant")
                        .Start("permissions")
                            .Elem("role", "Contributor")
                        .End()
                        .Start("group").Attr("id", groupid).End()
                    .End()
                .End();


            DreamMessage msg = p.At("pages", pageid, "security").Put(securityDoc);

            Assert.IsTrue(msg.IsSuccessful, "Failed to set page to private");

            Plug userP = Utils.BuildPlugForUser(username, "password");

            string roleoperations = p.At("site", "roles", "=Contributor").Get().ToDocument()["operations"].AsText;
            string effectiveOperations = userP.At("pages", pageid, "security").Get().ToDocument()["permissions.effective/operations"].AsText;
            Assert.AreEqual(roleoperations, effectiveOperations, string.Format("Contributor role: {0}\nEffective operations: {1}", roleoperations, effectiveOperations));
        }

        [Ignore]
        [Test]
        public void FailedPermissionChangeWhenPartOfMultipleGroups() {
            //Assumptions:
            //  Role 'Contributor' exist
            //Actions:
            //  Create user user1 with "Contributor" role
            //  Create group group1 with "Contributor" role
            //  Create group group2 with "Contributor" role
            //  Assing user1 with group1 and group2
            //  Create new page
            //  Set page restriction as private
            //  Set grant to page for user1, group1 and group2
            //  Login as user1
            //  Remove group2 from list of grants
            //Expected result: 
            //  List of grants doesn't content group2

            Plug p = Utils.BuildPlugForAdmin();

            string userid = null;
            string username = null;
            DreamMessage msg = UserUtils.CreateRandomContributor(p, out userid, out username);

            string groupid1 = null;
            msg = UserUtils.CreateRandomGroup(p, new string[] { userid }, out groupid1);

            string groupid2 = null;
            msg = UserUtils.CreateRandomGroup(p, new string[] { userid }, out groupid2);

            string pageid = null;
            msg = PageUtils.CreateRandomPage(p, out pageid);

            XDoc securityDoc = new XDoc("security")
                .Start("permissions.page")
                    .Elem("restriction", "Private")
                .End()
                .Start("grants")
                    .Start("grant")
                        .Start("permissions")
                            .Elem("role", "Contributor")
                        .End()
                        .Start("user").Attr("id", userid).End()
                    .End()
                    .Start("grant")
                        .Start("permissions")
                            .Elem("role", "Contributor")
                        .End()
                        .Start("group").Attr("id", groupid1).End()
                    .End()
                    .Start("grant")
                        .Start("permissions")
                            .Elem("role", "Contributor")
                        .End()
                        .Start("group").Attr("id", groupid2).End()
                    .End()
                .End();

            msg = p.At("pages", pageid, "security").Put(securityDoc);
            Assert.IsTrue(msg.IsSuccessful, "Failed to set page to private");

            p = Utils.BuildPlugForUser(username);

            securityDoc = new XDoc("security")
                .Start("permissions.page")
                .Elem("restriction", "Private")
                .End()
                .Start("grants.removed")
                .Start("grant")
                .Start("permissions")
                .Elem("role", "Contributor")
                .End()
                .Start("group").Attr("id", groupid2).End()
                .End()
                .End();

            msg = p.At("pages", pageid, "security").Post(securityDoc);
            Assert.IsTrue(msg.IsSuccessful, "Failed to set page to private");

            Assert.AreEqual(msg.ToDocument()["permissions.page/restriction"].Contents, "Private");
            Assert.AreEqual(msg.ToDocument()[string.Format("grants/grant[user/@id=\"{0}\"]/permissions/role", userid)].Contents, "Contributor");
            Assert.AreEqual(msg.ToDocument()[string.Format("grants/grant[group/@id=\"{0}\"]/permissions/role", groupid1)].Contents, "Contributor");
            Assert.IsTrue(msg.ToDocument()[string.Format("grants/grant[group/@id=\"{0}\"]/permissions/role", groupid2)].IsEmpty);
        }

        [Test]
        public void NewPagesMovedPagesWrongRestrictions() {
            //Assumptions:
            //  Role 'Contributor' exist
            //Actions:
            //  Create user user1 with "Contributor" role
            //  Create user user2 with "Contributor" role
            //  Create page page1
            //  Set page1 restriction as private
            //  Set grant to page1 for user1
            //  Set page2 restriction as private
            //  Set grant to page2 for user2
            //  Move page page2 to page1
            //Expected result: 
            //  List of grants didn't change for page2

            Plug p = Utils.BuildPlugForAdmin();

            string userid1 = null;
            DreamMessage msg = UserUtils.CreateRandomContributor(p, out userid1);

            string pageid1 = null;
            string pagename1 = null;
            msg = PageUtils.CreateRandomPage(p, out pageid1, out pagename1);

            XDoc securityDoc = new XDoc("security")
                .Start("permissions.page")
                    .Elem("restriction", "Private")
                .End()
                .Start("grants")
                    .Start("grant")
                        .Start("permissions")
                            .Elem("role", "Contributor")
                        .End()
                        .Start("user").Attr("id", userid1).End()
                    .End()
                .End();

            msg = p.At("pages", pageid1, "security").Put(securityDoc);
            Assert.IsTrue(msg.IsSuccessful, "Failed to set page to private");

            string userid2 = null;
            msg = UserUtils.CreateRandomContributor(p, out userid2);

            string pageid2 = null;
            string pagename2 = null;
            msg = PageUtils.CreateRandomPage(p, out pageid2, out pagename2);

            securityDoc = new XDoc("security")
                .Start("permissions.page")
                    .Elem("restriction", "Private")
                .End()
                .Start("grants")
                    .Start("grant")
                        .Start("permissions")
                            .Elem("role", "Contributor")
                        .End()
                        .Start("user").Attr("id", userid2).End()
                    .End()
                .End();

            msg = p.At("pages", pageid2, "security").Put(securityDoc);
            Assert.IsTrue(msg.IsSuccessful, "Failed to set page to private");

            msg = PageUtils.MovePage(p, pagename2, pagename1 + "/" + pagename2);

            msg = p.At("pages", pageid2, "security").Get();
            Assert.IsTrue(msg.IsSuccessful);

            Assert.AreEqual(msg.ToDocument()["permissions.page/restriction"].Contents, "Private");
            Assert.AreEqual(msg.ToDocument()[string.Format("grants/grant[user/@id=\"{0}\"]/permissions/role", userid2)].Contents, "Contributor");
            Assert.IsTrue(msg.ToDocument()[string.Format("grants/grant[user/@id=\"{0}\"]/permissions/role", userid1)].IsEmpty);
        }

        [Test]
        public void TestGrantRemovalLockout() {

            //BUG http://bugs.developer.mindtouch.com/view.php?id=6670

            //Assumptions:
            //  Role 'Contributor' exist and it includes changepermission
            //  Private restriction does restrict changepermission
            //Actions:
            //  Create user user1 with "Contributor" role
            //  Create group group1 with "Contributor" role
            //  Add user1 as a member to group1
            //  Create page page1
            //  Set page1 restriction as private
            //  Set grant to page1 for user1 as admin
            //  Set grant to page1 for group1 as admin
            //  Remove grant from page1 for group1
            //Expected result: 
            //  Allowed to remove the grant

            Plug adminPlug = Utils.BuildPlugForAdmin();
            DreamMessage msg;
            XDoc securityDoc;

            string adminId;
            msg = adminPlug.At("users", "current").GetAsync().Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Failed to lookup admin");
            adminId = msg.ToDocument()["@id"].AsText;

            //Create user1
            string user1Id;
            string user1Name;
            msg = UserUtils.CreateRandomUser(adminPlug, "Contributor", "password", out user1Id, out user1Name);
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Failed to create user");

            //Create group1 and set user1 as member
            string group1Id;
            string group1Name;
            UserUtils.CreateRandomGroup(adminPlug, "Contributor", new string[] { user1Id }, out group1Id, out group1Name);
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Failed to create group");

            //Create Page1
            string page1Id = null;
            string page1Name = null;
            msg = PageUtils.CreateRandomPage(adminPlug, out page1Id, out page1Name);
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Failed to create page");

            //Set private restriction with grants to user1 and group1
            securityDoc = new XDoc("security")
    .Start("permissions.page")
        .Elem("restriction", "Private")
    .End()
    .Start("grants")
        .Start("grant")
            .Start("permissions")
                .Elem("role", "Contributor")
            .End()
            .Start("user").Attr("id", user1Id).End()
        .End()
        .Start("grant")
            .Start("permissions")
                .Elem("role", "Contributor")
            .End()
            .Start("group").Attr("id", group1Id).End()
        .End()
        .Start("grant")
            .Start("permissions")
                .Elem("role", "Contributor")
            .End()
            .Start("user").Attr("id", adminId).End()
        .End()
    .End();

            msg = adminPlug.At("pages", page1Id, "security").PutAsync(securityDoc).Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Failed to set initial page perms");

            //Remove grant from group1 and admin
            securityDoc = new XDoc("security")
    .Start("permissions.page")
        .Elem("restriction", "Private")
    .End()
    .Start("grants")
        .Start("grant")
            .Start("permissions")
                .Elem("role", "Contributor")
            .End()
            .Start("user").Attr("id", user1Id).End()
        .End()
    .End();

            Plug userPlug = Utils.BuildPlugForUser(user1Name, "password");

            msg = userPlug.At("pages", page1Id, "security").PutAsync(securityDoc).Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Failed to set initial page perms");

            //Remove the grant for user1 from page1
            securityDoc = new XDoc("security")
        .Start("permissions.page")
            .Elem("restriction", "Private")
        .End()
        .Start("grants")
        .End();

            msg = userPlug.At("pages", page1Id, "security").PutAsync(securityDoc).Wait();
            Assert.AreEqual(DreamStatus.BadRequest, msg.Status, "user got locked out!");


        }
    }
}

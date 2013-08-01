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

using NUnit.Framework;

using MindTouch.Dream;
using MindTouch.Xml;

namespace MindTouch.Deki.Tests.ServiceTests {

    [TestFixture]
    public class XmlAuthTests {
        private const string SID = "sid://mindtouch.com/2008/08/xml-authentication";
        private const string Description = "XmlAuthentication";

        private XDoc CreateDefaultXml() {
            return new XDoc("dekiauth")
                .Start("users")
                    .Start("user").Attr("name", "foo")
                        .Start("password").Attr("type", "md5").Value("9a618248b64db62d15b300a07b00580b").End()
                        .Elem("email", "foo@somewhere.com")
                        .Elem("fullname", "Foo Smith")
                        .Elem("status", "enabled")
                    .End()
                    .Start("user").Attr("name", "joe")
                        .Start("password").Attr("type", "plain").Value("supersecret").End()
                        .Elem("email", "joe@somewhere.com")
                        .Elem("fullname", "Joe Smith")
                        .Elem("status", "enabled")
                    .End()
                    .Start("user").Attr("name", "Mike Lowrey")
                        .Start("password").Attr("type", "plain").Value("supersecret").End()
                        .Elem("email", "mike@somewhere.com")
                        .Elem("fullname", "mike lowrey")
                        .Elem("status", "enabled")
                    .End()
                .End()
                .Start("groups")
                    .Start("group").Attr("name", "sales")
                        .Start("user").Attr("name", "foo").End()
                        .Start("user").Attr("name", "joe").End()
                    .End()
                    .Start("group").Attr("name", "admin")
                        .Start("user").Attr("name", "joe").End()
                    .End()
                .End();
        }

        private string GetServiceIDBySID(Plug p, string sid) {
            DreamMessage msg = p.At("site", "services").With("limit","all").Get();
            foreach(XDoc service in msg.ToDocument()["service"]) {
                if(service["sid"].AsText == sid)
                    return service["@id"].AsText;
            }
            return null;
        }

        private void AddNewUserToXml(Plug p, string serviceid, string username, string password) {
            DreamMessage msg = p.At("site", "services", serviceid).Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status);

            string filename = msg.ToDocument()["config/value[@key='xmlauth-path']"].AsText;

            XDoc xdoc = XDocFactory.LoadFrom(filename, MimeType.XML);
            xdoc["users"]
                    .Start("user").Attr("name", username)
                        .Start("password").Attr("type", "plain").Value(password).End()
                        .Elem("email", username + "@somewhere.com")
                        .Elem("fullname", username)
                        .Elem("status", "enabled")
                    .End();

            xdoc.Save(filename);
        }

        private void AddNewGroupToXml(Plug p, string serviceid, string groupname) {
            DreamMessage msg = p.At("site", "services", serviceid).Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status);

            string filename = msg.ToDocument()["config/value[@key='xmlauth-path']"].AsText;

            XDoc xdoc = XDocFactory.LoadFrom(filename, MimeType.XML);

            xdoc["groups"].Start("group").Attr("name", groupname).End();

            xdoc.Save(filename);
        }

        [TestFixtureSetUp]
        public void Start() {
            XDoc authxdoc = CreateDefaultXml();
            string filename = System.IO.Path.GetTempFileName() + ".xml";
            authxdoc.Save(filename);

            Plug p = Utils.BuildPlugForAdmin();
            string serviceid = GetServiceIDBySID(p, SID);
            DreamMessage msg = null;
            XDoc xdoc = null;

            if(serviceid == null) {
                xdoc = new XDoc("service")
                    .Elem("sid", SID)
                    .Elem("uri", string.Empty)
                    .Elem("type", "auth")
                    .Elem("description", Description)
                    .Elem("status", "enabled")
                    .Elem("local", "true")
                    .Elem("init", "native")
                    .Start("config")
                        .Start("value").Attr("key", "xmlauth-path").Value(filename).End()
                    .End();

                msg = p.At("site", "services").Post(DreamMessage.Ok(xdoc));
                Assert.AreEqual(DreamStatus.Ok, msg.Status);
                serviceid = msg.ToDocument()["@id"].AsText;
                msg = p.At("site", "services", serviceid, "start").Post();
                Assert.AreEqual(DreamStatus.Ok, msg.Status);
            } else {
                msg = p.At("site", "services", serviceid, "stop").Post();
                Assert.AreEqual(DreamStatus.Ok, msg.Status);

                msg = p.At("site", "services", serviceid).Get();
                Assert.AreEqual(DreamStatus.Ok, msg.Status);
                xdoc = msg.ToDocument();
                xdoc["config/value[@key='xmlauth-path']"].ReplaceValue(filename);
                msg = p.At("site", "services", serviceid).Put(xdoc);
                Assert.AreEqual(DreamStatus.Ok, msg.Status);
                msg = p.At("site", "services", serviceid, "start").PostAsync().Wait();
                Assert.AreEqual(DreamStatus.Ok, msg.Status);
            }
        }

        public void StopAndDelete() {
            Plug p = Utils.BuildPlugForAdmin();
            string serviceid = GetServiceIDBySID(p, SID);
            if(!string.IsNullOrEmpty(serviceid)) {
                p.At("site", "services", serviceid).DeleteAsync().Wait();
            }
        }

        [TestFixtureTearDown]
        public void GlobalTearDown() {
            StopAndDelete();
        }

        [Test]
        public void TryToLoginThroughExternalService() {
            //Assumptions: 
            // users joe:supersecret and foo:supersecret exist
            //Actions:
            // try to login with both users credentials
            //Expected result: 
            // ok

            Plug p = Utils.BuildPlugForAdmin();

            string serviceid = GetServiceIDBySID(p, SID);

            DreamMessage msg = null;
            p = Utils.BuildPlugForAnonymous().WithCredentials("joe", "supersecret");

            msg = p.At("users", "authenticate").WithQuery("authprovider=" + serviceid).PostAsync().Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status);

            p = Utils.BuildPlugForAnonymous().WithCredentials("foo", "supersecret");

            msg = p.At("users", "authenticate").WithQuery("authprovider=" + serviceid).PostAsync().Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status);
        }

        [Test]
        public void LoginUserWithSpace() {
            //Assumptions: 
            // users Mike Lowrey:supersecret exists
            //Actions:
            // try to login with user credentials
            //Expected result: 
            // ok

            Plug p = Utils.BuildPlugForAdmin();

            string serviceid = GetServiceIDBySID(p, SID);

            DreamMessage msg = null;
            p = Utils.BuildPlugForAnonymous().WithCredentials("Mike Lowrey", "supersecret");

            msg = p.At("users", "authenticate").WithQuery("authprovider=" + serviceid).PostAsync().Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status);
        }

        [Test]
        public void CreateUser() {
            //Assumptions: 
            // user joe:supersecret exists
            //Actions:
            // try to create user from xml
            // try to create user which doesn't exist in xml
            //Expected result: 
            // ok
            // notfound

            Plug p = Utils.BuildPlugForAdmin();

            string serviceid = GetServiceIDBySID(p, SID);

            string username = Utils.GenerateUniqueName();
            string password = "test";
            AddNewUserToXml(p, serviceid, username, password);

            XDoc userDoc = new XDoc("user")
                .Elem("username", username)
                    .Start("service.authentication").Attr("id", serviceid).End()
                .Start("permissions.user")
                    .Elem("role", "Contributor")
                .End();
            DreamMessage msg = p.At("users").
                With("authusername", "joe").With("authpassword", "supersecret").Post(userDoc);
            Assert.AreEqual(DreamStatus.Ok, msg.Status);

            string userid = msg.ToDocument()["@id"].AsText;
            Assert.IsFalse(string.IsNullOrEmpty(userid));

            try {
                userDoc = new XDoc("user")
                    .Elem("username", Utils.GenerateUniqueName())
                        .Start("service.authentication").Attr("id", serviceid).End()
                    .Start("permissions.user")
                        .Elem("role", "Contributor")
                    .End();
                msg = p.At("users").With("authusername", "joe").
                    With("authpassword", "supersecret").Post(userDoc);
                Assert.Fail();
            } catch(DreamResponseException ex) {
                Assert.AreEqual(DreamStatus.NotFound,ex.Response.Status);
            }
        }

        [Test]
        public void AddNewUserAndLoginThroughPost() {
            //Assumptions: 
            // user joe:supersecret exists
            //Actions:
            // create new user in xml file
            // try to with new user credentials
            //Expected result: 
            // ok

            Plug p = Utils.BuildPlugForAdmin();

            string serviceid = GetServiceIDBySID(p, SID);

            string username = Utils.GenerateUniqueName();
            string password = "test";
            AddNewUserToXml(p, serviceid, username, password);

            p = Utils.BuildPlugForAnonymous().At("users", "authenticate");

            DreamMessage msg = p.WithCredentials(username, password).With("authprovider", serviceid).PostAsync().Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status);
        }

        [Test]
        public void AddNewUserAndLoginThroughGet() {
            //Assumptions: 
            // user joe:supersecret exists
            //Actions:
            // create new user in xml file
            // try to with new user credentials
            //Expected result: 
            // ok

            Plug p = Utils.BuildPlugForAdmin();

            string serviceid = GetServiceIDBySID(p, SID);

            string username = Utils.GenerateUniqueName();
            string password = "test";
            AddNewUserToXml(p, serviceid, username, password);

            p = Utils.BuildPlugForAnonymous().At("users", "authenticate");

            // Because GET:users/authenticate doesn't create users
            DreamMessage msg = p.WithCredentials(username, password).With("authprovider", serviceid).PostAsync().Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status);

            p = Utils.BuildPlugForAnonymous().At("users", "authenticate");

            msg = p.WithCredentials(username, password).With("authprovider", serviceid).GetAsync().Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status);
        }

        [Test]
        public void AddNewGroup() {
            //Assumptions: 
            // user joe:supersecret exists
            //Actions:
            // create new group in xml file
            // try to create group from xml
            // try to receive new group from server
            //Expected result: 
            // new group created in deki

            Plug p = Utils.BuildPlugForAdmin();

            string serviceid = GetServiceIDBySID(p, SID);

            DreamMessage msg = p.At("site", "services", serviceid).Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status);

            string filename = msg.ToDocument()["config/value[@key='xmlauth-path']"].AsText;
            string groupname = Utils.GenerateUniqueName();

            XDoc xdoc = XDocFactory.LoadFrom(filename, MimeType.XML);
            xdoc["groups"]
                    .Start("group").Attr("name", groupname).End();

            xdoc.Save(filename);

            XDoc groupDoc = new XDoc("group")
                .Elem("name", groupname)
                .Start("service.authentication").Attr("id", serviceid).End()
                .Start("permissions.group")
                    .Elem("role", "Contributor")
                .End()
                .Start("users")
                .End();
            msg = p.At("groups").WithQuery("authusername=joe").WithQuery("authpassword=supersecret").Post(groupDoc);
            Assert.AreEqual(DreamStatus.Ok, msg.Status);

            string groupid = msg.ToDocument()["@id"].AsText;
            Assert.IsFalse(string.IsNullOrEmpty(groupid));

            msg = p.At("groups").WithQuery("authprovider=" + serviceid).Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status);
            Assert.IsFalse(msg.ToDocument()[string.Format("group[groupname=\"{0}\"]", groupname)].IsEmpty);

            msg = p.At("groups", groupid).Delete();
            Assert.AreEqual(DreamStatus.Ok, msg.Status);
        }

        [Test]
        public void SortUsersByService() {
            //Assumptions: 
            // user joe:supersecret exists
            //Actions:
            // Create user for local service
            // Create user for xmlauth
            // Sort users by service
            //Expected result: 
            // ok

            Plug p = Utils.BuildPlugForAdmin();

            string id = null;
            string usernameFilter = Utils.GenerateUniqueName();
            DreamMessage msg = null;

            msg = UserUtils.CreateUser(p, "Contributor", "password", out id,
                usernameFilter + Utils.GenerateUniqueName());

            string serviceid = GetServiceIDBySID(p, SID);

            string username = usernameFilter + Utils.GenerateUniqueName();
            AddNewUserToXml(p, serviceid, username, "test");

            XDoc userDoc = new XDoc("user")
                .Elem("username", username)
                    .Start("service.authentication").Attr("id", serviceid).End()
                .Start("permissions.user")
                    .Elem("role", "Contributor")
                .End();
            msg = p.At("users").With("authusername", "joe").With("authpassword", "supersecret").Post(userDoc);
            Assert.AreEqual(DreamStatus.Ok, msg.Status);

            Dictionary<string, string> serviceDescriptions = Utils.GetDictionaryFromDoc(p.At("site", "services").With("limit", "all").Get().ToDocument(), "service", "@id", "description");

            Utils.TestSortingOfDocByField(
                p.At("users").With("sortby", "service").With("usernamefilter", usernameFilter).Get().ToDocument(),
                "user", "service.authentication/@id", true, serviceDescriptions);

            Utils.TestSortingOfDocByField(
                p.At("users").With("sortby", "-service").With("usernamefilter", usernameFilter).Get().ToDocument(),
                "user", "service.authentication/@id", false, serviceDescriptions);
        }

        [Test]
        public void SortGroupsByService() {
            //Assumptions: 
            // user joe:supersecret exists
            //Actions:
            // Create group for local service
            // Create group for xmlauth
            // Sort groups by service
            //Expected result: 
            // ok

            Plug p = Utils.BuildPlugForAdmin();

            string id = null;
            string groupnameFilter = Utils.GenerateUniqueName();
            DreamMessage msg = null;

            msg = UserUtils.CreateGroup(p, "Contributor", null, out id, groupnameFilter + Utils.GenerateUniqueName());

            string serviceid = GetServiceIDBySID(p, SID);

            string groupname = groupnameFilter + Utils.GenerateUniqueName();
            AddNewGroupToXml(p, serviceid, groupname);

            XDoc groupDoc = new XDoc("group")
                .Elem("name", groupname)
                .Start("service.authentication").Attr("id", serviceid).End()
                .Start("permissions.group")
                    .Elem("role", "Contributor")
                .End();

            msg = p.At("groups").With("authusername", "joe").With("authpassword", "supersecret").Post(groupDoc);
            Assert.AreEqual(DreamStatus.Ok, msg.Status);

            Dictionary<string, string> serviceDescriptions = Utils.GetDictionaryFromDoc(p.At("site", "services").With("limit", "all").Get().ToDocument(), "service", "@id", "description");

            Utils.TestSortingOfDocByField(
                p.At("groups").With("sortby", "service").With("groupnamefilter", groupnameFilter).Get().ToDocument(),
                "group", "service.authentication/@id", true, serviceDescriptions);

            Utils.TestSortingOfDocByField(
                p.At("groups").With("sortby", "-service").With("groupnamefilter", groupnameFilter).Get().ToDocument(),
                "group", "service.authentication/@id", false, serviceDescriptions);
        }

        [Test]
        public void DeleteService() {
            //Assumptions: 
            // user joe:supersecret exists
            //Actions:
            // create new group1 in xml file
            // create new user1 in xml file
            // create group1 from xml
            // create user1 from xml
            // delete auth service
            //Expected result: 
            // service.authentication/@id for group1 changed
            // service.authentication/@id for user1 changed

            Plug p = Utils.BuildPlugForAdmin();

            string serviceid = GetServiceIDBySID(p, SID);

            DreamMessage msg = p.At("site", "services", serviceid).Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status);

            string filename = msg.ToDocument()["config/value[@key='xmlauth-path']"].AsText;
            string groupname = "group" + Utils.GenerateUniqueName();

            XDoc xdoc = XDocFactory.LoadFrom(filename, MimeType.XML);
            xdoc["groups"]
                    .Start("group").Attr("name", groupname).End();

            xdoc.Save(filename);

            XDoc groupDoc = new XDoc("group")
                .Elem("name", groupname)
                .Start("service.authentication").Attr("id", serviceid).End()
                .Start("permissions.group")
                    .Elem("role", "Contributor")
                .End()
                .Start("users")
                .End();

            msg = p.At("groups").
                WithQuery("authusername=joe").WithQuery("authpassword=supersecret").PostAsync(groupDoc).Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status);

            string groupid = msg.ToDocument()["@id"].AsText;
            Assert.IsFalse(string.IsNullOrEmpty(groupid));

            string username = Utils.GenerateUniqueName();
            string password = "test";
            AddNewUserToXml(p, serviceid, username, password);

            XDoc userDoc = new XDoc("user")
                .Elem("username", username)
                    .Start("service.authentication").Attr("id", serviceid).End()
                .Start("permissions.user")
                    .Elem("role", "Contributor")
                .End();
            msg = p.At("users").With("authusername", "joe").With("authpassword", "supersecret").Post(userDoc);
            Assert.AreEqual(DreamStatus.Ok, msg.Status);

            string userid = msg.ToDocument()["@id"].AsText;
            Assert.IsFalse(string.IsNullOrEmpty(userid));

            msg = p.At("groups", groupid).Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status);
            Assert.AreEqual(msg.ToDocument()["service.authentication/@id"].AsText, serviceid);

            msg = p.At("users", userid).Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status);
            Assert.AreEqual(msg.ToDocument()["service.authentication/@id"].AsText, serviceid);

            msg = p.At("site", "services", serviceid).DeleteAsync().Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status);

            msg = p.At("groups", groupid).Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status);
            Assert.AreNotEqual(msg.ToDocument()["service.authentication/@id"].AsText, serviceid);

            msg = p.At("groups").WithQuery("authprovider=" + serviceid).Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status);
            Assert.AreEqual(msg.ToDocument()["@count"].AsInt, 0);

            msg = p.At("users", userid).Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status);
            Assert.AreNotEqual(msg.ToDocument()["service.authentication/@id"].AsText, serviceid);

            msg = p.At("users").WithQuery("authprovider=" + serviceid).Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status);
            Assert.AreEqual(msg.ToDocument()["@count"].AsInt, 0);

            msg = p.At("groups", groupid).Delete();
            Assert.AreEqual(DreamStatus.Ok, msg.Status);

            Start(); // For return service
        }
    }
}

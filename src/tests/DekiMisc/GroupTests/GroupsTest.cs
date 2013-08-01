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

namespace MindTouch.Deki.Tests.GroupTests
{
    [TestFixture]
    public class DekiWiki_GroupsTest
    {
        /// <summary>
        ///     Create group with 2 users and with Contributor role
        /// </summary>        
        /// <feature>
        /// <name>POST:groups</name>
        /// <uri>http://developer.mindtouch.com/Deki/API_Reference/POST%3agroups</uri>
        /// </feature>
        /// <assumption>ADMIN permissions</assumption>
        /// <expected>200 OK HTTP Response?</expected>

        [Test]
        public void CreateGroup()
        {
            // 1. Create user1
            // 2. Create user2
            // 3. Generate group XML doc with created users as members and a Contributor role
            // (4) Assert response contains correct group name

            Plug p = Utils.BuildPlugForAdmin();
            try
            {
                string userid1 = null;
                DreamMessage msg = UserUtils.CreateRandomContributor(p, out userid1);

                string userid2 = null;
                msg = UserUtils.CreateRandomContributor(p, out userid2);

                string name = Utils.GenerateUniqueName();

                XDoc groupDoc = new XDoc("group")
                    .Elem("name", name)
                    .Start("permissions.group")
                        .Elem("role", "Contributor")
                    .End()
                    .Start("users")
                        .Start("user").Attr("id", userid1).End()
                        .Start("user").Attr("id", userid2).End()
                    .End();
                msg = p.At("groups").Post(groupDoc);                 
                Assert.AreEqual(DreamStatus.Ok, msg.Status, "Group create failed");

                Assert.IsTrue(msg.ToDocument()["groupname"].AsText == name, "Group name in response does not match generated one!");
            }
            catch (MindTouch.Dream.DreamResponseException ex)
            {
                Assert.IsTrue(ex.Response.Status == DreamStatus.Conflict, "Group already exists!");
            }
        }

        /// <summary>
        ///     Change group permission through POST method
        /// </summary>        
        /// <feature>
        /// <name>POST:groups</name>
        /// <uri>http://developer.mindtouch.com/Deki/API_Reference/POST%3agroups</uri>
        /// </feature>
        /// <assumption>ADMIN permissions</assumption>
        /// <expected>200 OK HTTP Response</expected>

        [Test]
        public void ChangeGroupThroughPost()
        {
            // 1. Create a group
            // 2. Change group role to Contributor
            // (3) Assert OK HTTP response

            Plug p = Utils.BuildPlugForAdmin();

            string id = null;
            DreamMessage msg = UserUtils.CreateRandomGroup(p, out id);

            XDoc groupDoc = new XDoc("group").Attr("id", id)
                .Start("permissions.group")
                    .Elem("role", "Contributor")
                .End();
            msg = p.At("groups").Post(groupDoc);
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "POST request failed");
        }

        /// <summary>
        ///     Change group permission by ID through a PUT request
        /// </summary>        
        /// <feature>
        /// <name>PUT:groups/{groupid}</name>
        /// <uri>http://developer.mindtouch.com/Deki/API_Reference/PUT%3agroups%2f%2f%7bgroupid%7d</uri>
        /// </feature>
        /// <assumption>ADMIN permissions</assumption>
        /// <expected>200 OK HTTP Response</expected>

        [Test]
        public void ChangeGroupByIDThroughPUT()
        {
            // 1. Create a group
            // 2. Change group role to Contributor
            // (3) Assert OK HTTP response

            Plug p = Utils.BuildPlugForAdmin();

            string id = null;
            string name = null;
            DreamMessage msg = UserUtils.CreateRandomGroup(p, out id, out name);

            XDoc groupDoc = new XDoc("group")
                .Start("permissions.group")
                    .Elem("role", "Contributor")
                .End();
            msg = p.At("groups", id).Put(groupDoc);
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "PUT request failed");
        }

        /// <summary>
        ///     Change group permission by name through a PUT request
        /// </summary>        
        /// <feature>
        /// <name>PUT:groups/{groupid}</name>
        /// <uri>http://developer.mindtouch.com/Deki/API_Reference/PUT%3agroups%2f%2f%7bgroupid%7d</uri>
        /// </feature>
        /// <assumption>ADMIN permissions</assumption>
        /// <expected>200 OK HTTP Response</expected>

        [Test]
        public void ChangeGroupByNameThroughPUT()
        {
            // 1. Create a group
            // 2. Change group role to Contributor
            // (3) Assert OK HTTP response

            Plug p = Utils.BuildPlugForAdmin();

            string id = null;
            string name = null;
            DreamMessage msg = UserUtils.CreateRandomGroup(p, out id, out name);

            XDoc groupDoc = new XDoc("group")
                .Start("permissions.group")
                    .Elem("role", "Contributor")
                .End();
            msg = p.At("groups", "=" + name).Put(groupDoc);
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "PUT request failed");
        }

        /// <summary>
        /// Set members for a group
        /// </summary>
        /// <feature>
        /// <name>PUT:groups/{groupid}/users</name>
        /// <URI>http://developer.mindtouch.com/Deki/API_Reference/PUT%3agroups%2f%2f%7bgroupid%7d%2f%2fusers</URI>
        /// </feature>
        /// <assumption>ADMIN permissions</assumption>
        /// <expected>200 OK HTTP Response?</expected>

        [Test]
        public void SetMembersForGroup()
        {
            // 1. Create a group
            // 2. Create user1
            // 3. Create user2
            // 4. Genereate XML doc for a group with created users
            // (5) Assert 200 HTTP response

            Plug p = Utils.BuildPlugForAdmin();

            string id = null;
            DreamMessage msg = UserUtils.CreateRandomGroup(p, out id);

            string userid1 = null;
            msg = UserUtils.CreateRandomContributor(p, out userid1);

            string userid2 = null;
            msg = UserUtils.CreateRandomContributor(p, out userid2);

            XDoc usersDoc = new XDoc("users")
                .Start("user")
                    .Attr("id", userid1)
                .End()
                .Start("user")
                    .Attr("id", userid2)
                .End();
            msg = p.At("groups", id, "users").Put(usersDoc); 
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "PUT request failed");
        }

        /// <summary>
        ///     Retrieve a list of groups
        /// </summary>        
        /// <feature>
        /// <name>GET:groups</name>
        /// <uri>http://developer.mindtouch.com/Deki/API_Reference/GET%3agroups</uri>
        /// </feature>
        /// <assumption>ADMIN permissions</assumption>
        /// <expected>200 OK HTTP Response</expected>

        [Test]
        public void GetGroups()
        {
            // 1. Retrieve groups list
            // (2) Assert 200 HTTP response

            Plug p = Utils.BuildPlugForAdmin();

            DreamMessage msg = p.At("groups").Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Groups retrieval failed");
        }

        /// <summary>
        ///     Retrieve a group by ID
        /// </summary>        
        /// <feature>
        /// <name>GET:groups/{groupid}</name>
        /// <uri>http://developer.mindtouch.com/Deki/API_Reference/GET%3agroups%2f%2f%7bgroupid%7d</uri>
        /// </feature>
        /// <assumption>ADMIN permissions</assumption>
        /// <expected>200 OK HTTP Response</expected>

        [Test]
        public void GetGroupByID()
        {
            // 1. Create a group
            // 2. Retrieve a group by ID
            // (3) Assert group name in response matches generated name

            Plug p = Utils.BuildPlugForAdmin();

            string id = null;
            string name = null;
            DreamMessage msg = UserUtils.CreateRandomGroup(p, out id, out name);

            msg = p.At("groups", id).Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Group retrieval failed");

            Assert.IsTrue(msg.ToDocument()["groupname"].AsText == name, "Group name in response does not match generated group name!");
        }

        /// <summary>
        ///     Retrieve a group by name
        /// </summary>        
        /// <feature>
        /// <name>GET:groups/{groupid}</name>
        /// <uri>http://developer.mindtouch.com/Deki/API_Reference/GET%3agroups%2f%2f%7bgroupid%7d</uri>
        /// </feature>
        /// <assumption>ADMIN permissions</assumption>
        /// <expected>200 OK HTTP Response</expected>

        [Test]
        public void GetGroupByName()
        {
            // 1. Create a group
            // 2. Retrieve a group by name
            // (3) Assert group name in response matches generated name

            Plug p = Utils.BuildPlugForAdmin();

            string id = null;
            string name = null;
            DreamMessage msg = UserUtils.CreateRandomGroup(p, out id, out name);

            msg = p.At("groups", "=" + name).Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Group retrival failed");

            Assert.IsTrue(msg.ToDocument()["groupname"].AsText == name, "Group name in response does not match generated group name!");
        }

        /// <summary>
        ///     Delete a group by ID
        /// </summary>        
        /// <feature>
        /// <name>DELETE:groups/{groupid}</name>
        /// <uri>http://developer.mindtouch.com/Deki/API_Reference/DELETE%3agroups%2f%2f%7bgroupid%7d</uri>
        /// </feature>
        /// <assumption>ADMIN permissions</assumption>
        /// <expected>200 OK HTTP Response</expected>

        [Test]
        public void DeleteGroupByID()
        {
            // 1. Create a group
            // 2. Delete the group by ID
            // (3) Assert 200 HTTP response

            Plug p = Utils.BuildPlugForAdmin();

            string id = null;
            DreamMessage msg = UserUtils.CreateRandomGroup(p, out id);

            msg = p.At("groups", id).Delete();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Delete group by ID failed");
        }

        /// <summary>
        ///     Delete a group by name
        /// </summary>        
        /// <feature>
        /// <name>DELETE:groups/{groupid}</name>
        /// <uri>http://developer.mindtouch.com/Deki/API_Reference/DELETE%3agroups%2f%2f%7bgroupid%7d</uri>
        /// </feature>
        /// <assumption>ADMIN permissions</assumption>
        /// <expected>200 OK HTTP Response</expected>

        [Test]
        public void DeleteGroupByName()
        {
            // 1. Create a group
            // 2. Delete the group by name
            // (3) Assert 200 HTTP response

            Plug p = Utils.BuildPlugForAdmin();

            string name = null;
            string id = null;
            DreamMessage msg = UserUtils.CreateRandomGroup(p, out id, out name);

            msg = p.At("groups", "=" + name).Delete();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Delete group by name failed");
        }

        /// <summary>
        ///     Create a group with 2 users and retrieve the user list
        /// </summary>        
        /// <feature>
        /// <name>GET:groups/{groupid}/users</name>
        /// <uri>http://developer.mindtouch.com/Deki/API_Reference/GET%3agroups%2f%2f%7bgroupid%7d%2f%2fusers</uri>
        /// </feature>
        /// <assumption>ADMIN permissions</assumption>
        /// <expected>count attribute == 2</expected>

        [Test]
        public void GetGroupUsers()
        {
            // 1. Create user1
            // 2. Create user2
            // 3. Generate XML doc for group with created users as members and with Contributor permissions
            // (4) Assert response ID element exists
            // 5. Retrieve list of group users
            // (6) Assert user count equals 2

            Plug p = Utils.BuildPlugForAdmin();

            string userid1 = null;
            DreamMessage msg = UserUtils.CreateRandomContributor(p, out userid1);

            string userid2 = null;
            msg = UserUtils.CreateRandomContributor(p, out userid2);

            string name = Utils.GenerateUniqueName();

            XDoc groupDoc = new XDoc("group")
                .Elem("name", name)
                .Start("permissions.group")
                    .Elem("role", "Contributor")
                .End()
                .Start("users")
                    .Start("user").Attr("id", userid1).End()
                    .Start("user").Attr("id", userid2).End()
                .End();
            msg = p.At("groups").Post(groupDoc);
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "POST request failed");

            string id = msg.ToDocument()["@id"].AsText;
            Assert.IsTrue(!string.IsNullOrEmpty(id), "Group ID does not exist!");

            msg = p.At("groups", id, "users").Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Group user retrieval failed");

            Assert.IsTrue(msg.ToDocument()["@count"].AsInt == 2, "Group does not have exactly 2 members!");
        }

        /// <summary>
        ///     Create a group and retrieve it by assigning the group name to the "groupnamefilter" parameter
        /// </summary>        
        /// <feature>
        /// <name>GET:groups</name>
        /// <uri>http://developer.mindtouch.com/Deki/API_Reference/GET%3agroups</uri>
        /// <parameter>groupnamefilter</parameter>
        /// </feature>
        /// <assumption>ADMIN permissions</assumption>
        /// <expected>Only filtered group XML be present in response</expected>
        
        [Test]
        public void GetGroupWithFilter()
        {
            //Actions:
            // 1. Creates a group
            // 2. Try to get group with filter
            // (3a) Assert generated and retrieved group names match
            // (3b) Assert generated and retrieved group IDs match
            // (3c) Assert one and only one group is filtered
            //Expected result: 
            // OK

            Plug p = Utils.BuildPlugForAdmin();
            string id = null;
            string name = null;
            DreamMessage msg = UserUtils.CreateRandomGroup(p, out id, out name);

            msg = p.At("groups").With("groupnamefilter", name).Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Group with filter retrieval failed");
            Assert.IsTrue(msg.ToDocument()["group/groupname"].AsText == name, "Generated and retrieved group names do not match!");
            Assert.IsTrue(msg.ToDocument()["group/@id"].AsText == id, "Generated and retrieved group IDs do not match!");
            Assert.IsTrue(msg.ToDocument()["@querycount"].AsInt == 1, "More than 1 group returned?!");
        }

        /// <summary>
        ///     Create 5 groups and verify that they are correctly sorted by name/role as specified in the "sortby" parameter
        /// </summary>        
        /// <feature>
        /// <name>GET:groups</name>
        /// <uri>http://developer.mindtouch.com/Deki/API_Reference/GET%3agroups</uri>
        /// <parameter>sortby</parameter>
        /// </feature>
        /// <assumption>ADMIN permissions</assumption>
        /// <expected>Only filtered group XML be present in response</expected>

        [Test]
        public void TestGetGroupsSorting()
        {

            // 1. Create 5 groups
            // (2a) Assert group names are correctly sorted by ascending order
            // (2b) Assert group names are correctly sorted by descending order
            // (3a) Assert group roles are correctly sorted by ascending order
            // (3b) Assert group roles are correctly sorted by descending order

            Plug p = Utils.BuildPlugForAdmin();

            string id = null;
            string groupnameFilter = Utils.GenerateUniqueName();

            string uniqueRoleName = null;
            UserUtils.CreateRandomRole(p, out uniqueRoleName);
            UserUtils.CreateGroup(p, uniqueRoleName, null, out id, Utils.GenerateUniqueName() + groupnameFilter);
            UserUtils.CreateRandomRole(p, out uniqueRoleName);
            UserUtils.CreateGroup(p, uniqueRoleName, null, out id, Utils.GenerateUniqueName() + groupnameFilter);
            UserUtils.CreateRandomRole(p, out uniqueRoleName);
            UserUtils.CreateGroup(p, uniqueRoleName, null, out id, Utils.GenerateUniqueName() + groupnameFilter);
            UserUtils.CreateRandomRole(p, out uniqueRoleName);
            UserUtils.CreateGroup(p, uniqueRoleName, null, out id, Utils.GenerateUniqueName() + groupnameFilter);
            UserUtils.CreateRandomRole(p, out uniqueRoleName);
            UserUtils.CreateGroup(p, uniqueRoleName, null, out id, Utils.GenerateUniqueName() + groupnameFilter);

            Utils.TestSortingOfDocByField(p.At("groups").With("groupnamefilter", groupnameFilter)
                .With("sortby", "name").Get().ToDocument(), "group", "groupname", true);
            Utils.TestSortingOfDocByField(p.At("groups").With("groupnamefilter", groupnameFilter)
                .With("sortby", "-name").Get().ToDocument(), "group", "groupname", false);

            Utils.TestSortingOfDocByField(p.At("groups").With("groupnamefilter", groupnameFilter)
                .With("sortby", "role").Get().ToDocument(), "group", "permissions.group/role", true);
            Utils.TestSortingOfDocByField(p.At("groups").With("groupnamefilter", groupnameFilter)
                .With("sortby", "-role").Get().ToDocument(), "group", "permissions.group/role", false);
        }

        /// <summary>
        ///     Add and remove users from a group
        /// </summary>        
        /// <feature>
        /// <name>GET:groups/{groupid}/users</name>
        /// <uri>http://developer.mindtouch.com/Deki/API_Reference/GET%3agroups%2f%2f%7bgroupid%7d%2f%2fusers</uri>
        /// <parameter>sortby</parameter>
        /// </feature>
        /// <assumption>ADMIN permissions</assumption>
        /// <expected>Group users list reflects changes made</expected>

        [Test]
        public void AddRemoveMembersOfGroup() {

            // 1. Create a group
            // 2. Create Contributor user1
            // 3. Create Contributor user2
            // 4. Generate group users XML doc to add users to group
            // (5a) Assert user1 exists in group
            // (5b) Assert user2 exists in group
            // 6. Remove user1 from group
            // (7a) Assert user1 does not exist in group
            // (7b) Assert user2 still exists in group

            Plug p = Utils.BuildPlugForAdmin();

            string groupId = null;
            DreamMessage msg = UserUtils.CreateRandomGroup(p, out groupId);

            string userid1 = null;
            msg = UserUtils.CreateRandomContributor(p, out userid1);
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Unable to create a user");

            string userid2 = null;
            msg = UserUtils.CreateRandomContributor(p, out userid2);
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Unable to create a user");

            //Add 2 users to the group
            XDoc userAdditionBody = new XDoc("users");
            userAdditionBody.Start("user").Attr("id", userid1).End();
            userAdditionBody.Start("user").Attr("id", userid2).End();
            msg = p.At("groups", groupId, "users").PostAsync(userAdditionBody).Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "error adding user to group");

            //Verify user list
            msg = p.At("groups", groupId, "users").GetAsync().Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "error getting member list");
            Assert.AreEqual(userid1, msg.ToDocument()["user[@id = '" + userid1 + "']/@id"].AsText, "Group does not contain user after addition");
            Assert.AreEqual(userid2, msg.ToDocument()["user[@id = '" + userid2 + "']/@id"].AsText, "Group does not contain user after addition");
        
            //Remove a user from the group
            msg = p.At("groups", groupId, "users", userid1).DeleteAsync().Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "error removing user from group");

            //Verify user list
            msg = p.At("groups", groupId, "users").GetAsync().Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "error getting member list");
            Assert.IsTrue(msg.ToDocument()["user[@id = '" + userid1 + "']/@id"].IsEmpty, "Group still contains user after removal");
            Assert.AreEqual(userid2, msg.ToDocument()["user[@id = '" + userid2 + "']/@id"].AsText, "Group does not contain a user that should have remained");
        }
    }
}

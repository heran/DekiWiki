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

namespace MindTouch.Deki.Tests.SiteTests
{
    [TestFixture]
    public class RolesTests
    {
        [Test]
        public void GetRoles()
        {
            // GET:site/roles
            // http://developer.mindtouch.com/Deki/API_Reference/GET%3asite%2f%2froles

            Plug p = Utils.BuildPlugForAdmin();

            DreamMessage msg = p.At("site", "roles").Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status);
            string id = msg.ToDocument()["permissions/role/@id"].AsText;
            Assert.IsFalse(string.IsNullOrEmpty(id));
            string name = msg.ToDocument()["permissions/role"].AsText;
            Assert.IsFalse(string.IsNullOrEmpty(name));

            // GET:site/roles/{roleid}
            // http://developer.mindtouch.com/Deki/API_Reference/GET%3asite%2f%2froles%2f%2f%7broleid%7d

            msg = p.At("site", "roles", id).Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status);
            Assert.AreEqual(name, msg.ToDocument()["role"].AsText);
        }

        [Test]
        public void PutRoles()
        {
            // PUT:site/roles/{roleid}
            // http://developer.mindtouch.com/Deki/API_Reference/PUT%3asite%2f%2froles%2f%2f%7broleid%7d

            Plug p = Utils.BuildPlugForAdmin();

            XDoc permissionsDoc = new XDoc("permissions")
                .Elem("operations", "READ");
            string name = Utils.GenerateUniqueName();
            DreamMessage msg = p.At("site", "roles", "=" + name).Put(permissionsDoc);
            Assert.AreEqual(DreamStatus.Ok, msg.Status);
            string id = msg.ToDocument()["role/@id"].AsText;
            Assert.IsFalse(string.IsNullOrEmpty(id));
            
            msg = p.At("site", "roles", id).Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status);
            Assert.AreEqual(name, msg.ToDocument()["role"].AsText);
        }

        [Test]
        public void GetOperations()
        {
            // GET:site/operations
            // http://developer.mindtouch.com/Deki/API_Reference/GET%3asite%2f%2foperations

            Plug p = Utils.BuildPlugForAdmin();

            DreamMessage msg = p.At("site", "operations").Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status);
        }

        [Test]
        public void CreateCustomRoleForReadOnly()
        {
            //Assumptions: 
            //Actions:
            // Creates a role1 with LOGIN and READ permissions
            // Create page1
            // Create user1 with role1
            // Try to update page1 from user1
            //Expected result: 
            // MethodNotAllowed

            Plug p = Utils.BuildPlugForAdmin();

            XDoc permissionsDoc = new XDoc("permissions").Elem("operations", "LOGIN,READ");
            string rolename = Utils.GenerateUniqueName();
            DreamMessage msg = p.At("site", "roles", "=" + rolename).Put(permissionsDoc);
            Assert.AreEqual(DreamStatus.Ok, msg.Status);
            string id = msg.ToDocument()["role/@id"].AsText;
            Assert.IsFalse(string.IsNullOrEmpty(id));

            string username = null;
            string userid = null;
            msg = UserUtils.CreateRandomUser(p, rolename, out userid, out username);

            string pageid = null;
            string pagepath = null;
            msg = PageUtils.CreateRandomPage(p, out pageid, out pagepath);
            DateTime edittime = msg.ToDocument()["date.edited"].AsDate ?? DateTime.MinValue;

            p = Utils.BuildPlugForUser(username);

            msg = PageUtils.GetPage(p, pagepath);
            Assert.AreEqual(DreamStatus.Ok, msg.Status);

            msg = p.At("pages", pageid, "contents").
                With("edittime", Utils.DateToString(edittime)).
                PutAsync(DreamMessage.Ok(MimeType.TEXT_UTF8, "New Content")).Wait();
            Assert.AreEqual(DreamStatus.MethodNotAllowed, msg.Status);

            msg = p.At("pages", pageid, "subpages").GetAsync().Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status);

            p = Utils.BuildPlugForAdmin();

            msg = PageUtils.DeletePageByID(p, pageid, true);
            Assert.AreEqual(DreamStatus.Ok, msg.Status);
        }

        [Test]
        public void CreateCustomRoleForReadOnlyAndUpdateForDelete()
        {
            //Assumptions: 
            //Actions:
            // Creates a role with LOGIN and READ permissions
            // Create page1
            // Create user1 with role1
            // Add to role permissions UPDATE and DELETE
            // Try to delete page1 from user1
            //Expected result: 
            // Ok

            Plug p = Utils.BuildPlugForAdmin();

            XDoc permissionsDoc = new XDoc("permissions").Elem("operations", "LOGIN,READ");
            string rolename = Utils.GenerateUniqueName();
            DreamMessage msg = p.At("site", "roles", "=" + rolename).Put(permissionsDoc);
            Assert.AreEqual(DreamStatus.Ok, msg.Status);
            string id = msg.ToDocument()["role/@id"].AsText;
            Assert.IsFalse(string.IsNullOrEmpty(id));

            string username = null;
            string userid = null;
            msg = UserUtils.CreateRandomUser(p, rolename, out userid, out username);

            string pageid = null;
            string pagepath = null;
            msg = PageUtils.CreateRandomPage(p, out pageid, out pagepath);
            DateTime edittime = msg.ToDocument()["date.edited"].AsDate ?? DateTime.MinValue;

            p = Utils.BuildPlugForUser(username);

            msg = PageUtils.GetPage(p, pagepath);
            Assert.AreEqual(DreamStatus.Ok, msg.Status);

            p = Utils.BuildPlugForAdmin();

            permissionsDoc = new XDoc("permissions").Elem("operations", "LOGIN,READ,UPDATE,DELETE");
            msg = p.At("site", "roles", id).Put(permissionsDoc);
            Assert.AreEqual(DreamStatus.Ok, msg.Status);

            msg = p.At("site", "roles", id).Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status);

            p = Utils.BuildPlugForUser(username);

            msg = PageUtils.DeletePageByID(p, pageid, true);
            Assert.AreEqual(DreamStatus.Ok, msg.Status);
        }
    }
}

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

using MindTouch.Dream;
using MindTouch.Xml;

using NUnit.Framework;

namespace MindTouch.Deki.Tests
{
    public static class UserUtils
    {
        public const string DefaultUserPsw = "password";

        public static DreamMessage CreateRandomContributor(Plug p, out string id)
        {
            string name = null;
            return CreateRandomContributor(p, out id, out name);
        }

        public static DreamMessage CreateRandomContributor(Plug p, out string id, out string name)
        {
            return CreateRandomUser(p, "Contributor", out id, out name);
        }

        public static DreamMessage CreateRandomUser(Plug p, string role, out string id, out string name)
        {
            return CreateRandomUser(p, role, DefaultUserPsw, out id, out name);
        }

        public static DreamMessage CreateRandomUser(Plug p, string role, string password, out string id, out string name)
        {
            name = Utils.GenerateUniqueName();
            return CreateUser(p, role, password, out id, name);
        }

        public static DreamMessage CreateUser(Plug p, string role, string password, out string id, string name)
        {
            return CreateUser(p, role, password, out id, name, name + "@mindtouch.com");
        }

        public static DreamMessage CreateUser(Plug p, string role, string password, out string id, string name, string email)
        {
            XDoc usersDoc = new XDoc("user")
                .Elem("username", name)
                .Elem("email", email)
                .Elem("fullname", name + "'s full name")
                .Start("permissions.user")
                    .Elem("role", role)
                .End();

            DreamMessage msg = p.At("users").With("accountpassword", password).PostAsync(usersDoc).Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "POST: users with password returned non 200 status: "+msg.ToString());
            name = msg.ToDocument()["username"].AsText;
            Assert.IsTrue(!string.IsNullOrEmpty(name));
            id = msg.ToDocument()["@id"].AsText;
            Assert.IsTrue(!string.IsNullOrEmpty(id));

            return msg;
        }

        public static DreamMessage CreateRandomGroup(Plug p, string role, string[] memberUserIds, out string id)
        {
            string name = null;
            return CreateRandomGroup(p, role, memberUserIds, out id, out name);
        }

        public static DreamMessage CreateRandomGroup(Plug p, string[] memberUserIds, out string id)
        {
            return CreateRandomGroup(p, "Contributor", memberUserIds, out id);
        }

        public static DreamMessage CreateRandomGroup(Plug p, out string id)
        {
            return CreateRandomGroup(p, "Contributor", new string[] { }, out id);
        }

        public static DreamMessage CreateRandomGroup(Plug p, out string id, out string name)
        {
            return CreateRandomGroup(p, "Contributor", new string[] { }, out id, out name);
        }

        public static DreamMessage CreateRandomGroup(Plug p, string role, string[] memberUserIds, out string id, out string name)
        {
            name = "group_" + Utils.GenerateUniqueName();

            return CreateGroup(p, role, memberUserIds, out id, name);
        }

        public static DreamMessage CreateGroup(Plug p, string role, string[] memberUserIds, out string id, string name)
        {
            XDoc groupDoc = new XDoc("group")
                .Elem("groupname", name)
                .Start("permissions.group")
                    .Elem("role", role)
                .End();

            if (memberUserIds != null && memberUserIds.Length > 0)
            {
                groupDoc.Start("users");
                foreach (string userid in memberUserIds)
                    groupDoc.Start("user").Attr("id", userid).End();
                groupDoc.End();
            }

            DreamMessage msg = p.At("groups").Post(groupDoc);
            id = msg.ToDocument()["@id"].AsText;
            return msg;
        }

        public static DreamMessage SetMembersForGroup(Plug p, string groupid, string[] memberUserIds)
        {
            XDoc usersDoc = new XDoc("users");
            foreach (string  userid in memberUserIds)
	        {
                usersDoc.Start("user")
                    .Attr("id", userid)
                .End();
	        }
            DreamMessage msg = p.At("groups", groupid, "users").Put(usersDoc);
            Assert.AreEqual(DreamStatus.Ok, msg.Status);

            return msg;
        }

        public static DreamMessage CreateRandomRole(Plug p, out string name)
        {
            XDoc permissionsDoc = new XDoc("permissions")
                .Elem("operations", "READ");
            name = Utils.GenerateUniqueName();

            return p.At("site", "roles", "=" + XUri.DoubleEncode(name)).Put(permissionsDoc);
        }

        public static string GetCurrentUserID(Plug p)
        {
            return p.At("users", "current").Get().ToDocument()["@id"].AsText;
        }
    }
}

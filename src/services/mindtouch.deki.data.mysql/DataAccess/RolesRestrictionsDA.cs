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
using System.Data;

namespace MindTouch.Deki.Data.MySql {
    public partial class MySqlDekiDataSession {

        public IList<RoleBE> RolesRestrictions_GetRoles() {
            List<RoleBE> roles = new List<RoleBE>();
            Catalog.NewQuery(@" /* RolesRestrictionsDA::RetrieveRoles  */
select role_id as id, role_name as `name`, role_perm_flags as perm_flags, role_last_edit as last_edit, role_creator_user_id as creator_user_id
from	roles
order by role_id asc;")
            .Execute(delegate(IDataReader dr) {
                while (dr.Read()) {
                    RoleBE role = RolesRestrictions_Populate(dr, RoleType.ROLE);
                    roles.Add(role);
                }
            });
            return roles;
        }

        public IList<RoleBE> RolesRestrictions_GetRestrictions() {
            List<RoleBE> roles = new List<RoleBE>();
            Catalog.NewQuery(@" /* RolesRestrictions_GetRestrictions */
select restriction_id as id, restriction_name as `name`, restriction_perm_flags as perm_flags, restriction_last_edit as last_edit, restriction_creator_user_id as creator_user_id 
from	restrictions
order by restriction_id asc;")
            .Execute(delegate(IDataReader dr) {
                while (dr.Read()) {
                    RoleBE role = RolesRestrictions_Populate(dr, RoleType.RESTRICTION);
                    roles.Add(role);
                }
            });
            return roles;
        }

        public uint RolesRestrictions_InsertRole(RoleBE role) {
            string query = @" /*  RolesRestrictions_InsertRole */
INSERT INTO `roles` (`role_name`, `role_perm_flags`, `role_creator_user_id`, `role_last_edit`)
VALUES (?ROLENAME, ?ROLEFLAGS, ?ROLECREATORID, ?TIMESTAMP);
select LAST_INSERT_ID();";
            uint ret = Catalog.NewQuery(query)
    .With("ROLENAME", role.Name)
    .With("ROLEFLAGS", role.PermissionFlags)
    .With("ROLECREATORID", role.CreatorUserId)
    .With("TIMESTAMP", role.TimeStamp)
    .ReadAsUInt() ?? 0;
            return ret;
        }

        public void RolesRestrictions_UpdateRole(RoleBE role) {
            string query = @" /*  RolesRetrictions_UpdateRole */
UPDATE roles SET
role_perm_flags = ?ROLEFLAGS,
role_creator_user_id = ?ROLECREATORID,
role_last_edit = ?TIMESTAMP
WHERE role_name = ?ROLENAME;";
            Catalog.NewQuery(query)
    .With("ROLENAME", role.Name)
    .With("ROLEFLAGS", role.PermissionFlags)
    .With("ROLECREATORID", role.CreatorUserId)
    .With("TIMESTAMP", role.TimeStamp)
    .Execute();
        }

        private RoleBE RolesRestrictions_Populate(IDataReader dr, RoleType type) {
            RoleBE role = new RoleBE();
            role.CreatorUserId = dr.Read<uint>("creator_user_id");
            role.ID = dr.Read<uint>("id");
            role.Name = dr.Read<string>("name");
            role.PermissionFlags = dr.Read<ulong>("perm_flags");
            role.TimeStamp = dr.Read<DateTime>("last_edit");
            role.Type = type;
            return role;
        }
    }
}

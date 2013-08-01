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
using System.Text;
using System.Data;

using MindTouch.Data;

namespace MindTouch.Deki.Data.MySql {

    public partial class MySqlDekiDataSession {

        private static readonly IDictionary<GroupsSortField, string> GROUPS_SORT_FIELD_MAPPING = new Dictionary<GroupsSortField, string>() 
            { { GroupsSortField.ID, "group_id" },
              { GroupsSortField.NAME, "group_name" },
              { GroupsSortField.ROLE, "roles.role_name" },
              { GroupsSortField.SERVICE, "services.service_description" } };

        public void GroupMembers_UpdateGroupsForUser(uint userId, IList<uint> groupIds) {
            StringBuilder userGroupQuery = null;
            if (!ArrayUtil.IsNullOrEmpty(groupIds)) {
                userGroupQuery = new StringBuilder("insert ignore into user_groups (user_id, group_id) values ");

                for (int i = 0; i < groupIds.Count; i++) {
                    if (i > 0) {
                        userGroupQuery.Append(",");
                    }
                    userGroupQuery.AppendFormat("({0},{1})", userId, groupIds[i]);
                }
            } else {
                userGroupQuery = new StringBuilder();
            }

            Catalog.NewQuery(string.Format(@" /* GroupMembers_UpdateGroupsForUser */
delete from user_groups 
where user_id = ?USERID;
{0}", userGroupQuery.ToString()))
            .With("USERID", userId)
            .Execute();
        }

        public void GroupMembers_UpdateUsersInGroup(uint groupid, IList<uint> userIds, DateTime timestamp) {
            string userIdInClause = userIds.ToCommaDelimitedString();
            string deleteQuery = "delete from user_groups where group_id = ?GROUPID";
            StringBuilder insertQuery = new StringBuilder();
            if (userIds.Count > 0) {
                deleteQuery = string.Format("{0} and user_id not in ({1})", deleteQuery, userIdInClause);
                insertQuery.Append("insert ignore into user_groups (user_id, group_id, last_edit) values ");
                for (int i = 0; i < userIds.Count; i++) {
                    insertQuery.AppendFormat("{0}({1}, ?GROUPID, ?TIMESTAMP)", i > 0 ? "," : "", userIds[i]);
                }
            }

            Catalog.NewQuery(string.Format(@" /* GroupMembers_UpdateUsersInGroup */
{0};
{1};", deleteQuery, insertQuery))
            .With("GROUPID", groupid)
            .With("TIMESTAMP", timestamp)
            .Execute();
        }

        public IList<GroupBE> Groups_GetByIds(IList<uint> groupIds) {
            if(groupIds.Count == 0) {
                return new List<GroupBE>();
            }
            return Groups_GetInternal(string.Format("where groups.group_id in ({0})", groupIds.ToCommaDelimitedString()), "Groups_GetByIds");
        }

        public IList<GroupBE> Groups_GetByNames(IList<string> groupNames) {
            if (ArrayUtil.IsNullOrEmpty(groupNames)) {
                return new List<GroupBE>();
            }
            var groupNamesStr = new StringBuilder();
            for (int i = 0; i < groupNames.Count; i++) {
                if (i > 0) {
                    groupNamesStr.Append(",");
                }
                groupNamesStr.AppendFormat("'{0}'", DataCommand.MakeSqlSafe(groupNames[i]));
            }

            return Groups_GetInternal(string.Format("where groups.group_name in ({0})", groupNamesStr), "Groups_GetByNames");
        }

        public IList<GroupBE> Groups_GetByUser(uint userId) {
            if (userId == 0)
                return null;
            return Groups_GetInternal(string.Format(
@"join user_groups
    on groups.group_id = user_groups.group_id
where user_groups.user_id = {0};", userId), "Groups_GetByUser");

        }

        public IList<GroupBE> Groups_GetByQuery(string groupNameFilter, uint? serviceIdFilter, SortDirection sortDir, GroupsSortField sortField, uint? offset, uint? limit, out uint totalCount, out uint queryCount) {
            List<GroupBE> result = new List<GroupBE>();
            StringBuilder query = new StringBuilder();

            if (groupNameFilter != null) {
                groupNameFilter = "%" + DataCommand.MakeSqlSafe(groupNameFilter) + "%";
            }

            string sortFieldString = null;
            GROUPS_SORT_FIELD_MAPPING.TryGetValue(sortField, out sortFieldString);
            if ((sortFieldString ?? string.Empty).StartsWith("roles.")) {
                    query.Append(@"
left join roles
    on groups.group_role_id = roles.role_id");
                }

            if ((sortFieldString ?? string.Empty).StartsWith("services.")) {
                    query.AppendFormat(@"
left join services
    on groups.group_service_id = services.service_id");
                }
            
            
            if (!string.IsNullOrEmpty(groupNameFilter) || serviceIdFilter != null) {
                query.Append(" where (1=1)");
                if (serviceIdFilter != null) {
                    query.AppendFormat(" AND group_service_id = {0}", serviceIdFilter.Value);
                }

                if (!string.IsNullOrEmpty(groupNameFilter)) {
                    query.AppendFormat(" AND group_name like '{0}'", groupNameFilter);
                }
            }

            if (!string.IsNullOrEmpty(sortFieldString)) {
                query.AppendFormat(" order by {0} ", sortFieldString);
                if (sortDir != SortDirection.UNDEFINED) {
                    query.Append(sortDir.ToString());
                }
            }

            return Groups_GetInternal(query.ToString(), "Groups_GetByQuery", true, limit, offset, out totalCount, out queryCount);
        }

        public uint Groups_Insert(GroupBE group) {
            uint groupId = Catalog.NewQuery(@" /* Groups_Insert */
insert IGNORE into `groups` (`group_name`, `group_role_id`, `group_service_id`, `group_creator_user_id`, `group_last_edit`) 
values (?NAME, ?ROLEID, ?SERVICEID, ?CREATORUSERID, ?TIMESTAMP);
select LAST_INSERT_ID();")
                .With("NAME", group.Name)
                .With("ROLEID", group.RoleId)
                .With("SERVICEID", group.ServiceId)
                .With("CREATORUSERID", group.CreatorUserId)
                .With("TIMESTAMP", group.TimeStamp)
                .ReadAsUInt() ?? 0;
            return groupId;
        }

        public void Groups_Delete(uint groupId) {
            Catalog.NewQuery(@" /* Groups_Delete */
delete from user_groups where group_id = ?GROUPID;
delete from groups where group_id = ?GROUPID;")
                .With("GROUPID", groupId)
                .Execute();
        }


        public void Groups_Update(GroupBE group) {
            Catalog.NewQuery(@" /* Groups_Update */
UPDATE groups set 
group_role_id = ?ROLEID,
group_name = ?NAME
where group_id = ?GROUPID;")
               .With("GROUPID", group.Id)
               .With("ROLEID", group.RoleId)
               .With("NAME", group.Name)
               .Execute();
        }

        public IList<uint> Groups_UpdateServicesToLocal(uint oldServiceId) {
            List<uint> groupIds = new List<uint>();
            string query = string.Format(@"/* Groups_UpdateServicesToLocal */
SELECT group_id 
FROM groups
WHERE group_service_id = ?OLDSERVICEID;

UPDATE groups SET 
group_service_id = 1
WHERE group_service_id = ?OLDSERVICEID;
");

            Catalog.NewQuery(query)
            .With("OLDSERVICEID", oldServiceId)
            .Execute(delegate(IDataReader dr) {
                while(dr.Read()) {
                    groupIds.Add((uint) dr.GetInt32(0));

                }
            });
            return groupIds;
        }

        private IList<GroupBE> Groups_GetInternal(string where, string functionDescription) {
            uint totalCount, queryCount;
            return Groups_GetInternal(where, functionDescription, true, null, null, out totalCount, out queryCount);
        }

        private IList<GroupBE> Groups_GetInternal(string where, string functionDescription, bool lookupCount, uint? limit, uint? offset, out uint totalCount, out uint queryCount) {
            totalCount = queryCount = 0;
            uint totalCountTemp = 0, queryCountTemp = 0;
            List<GroupBE> groups = new List<GroupBE>();
            string totalCountQuery = lookupCount ? "select count(*) as totalcount from groups" : string.Empty;
            string queryCountQuery = lookupCount ? "select count(*) as querycount from groups " + where : string.Empty;
            string limitOffsetQuery = string.Empty;
            if (limit != null || offset != null) {
                limitOffsetQuery = string.Format("limit {0} offset {1}", limit ?? int.MaxValue, offset ?? 0);
            }

            string query = string.Format(@" /* GroupDA::{0} */
SET group_concat_max_len = @@max_allowed_packet;

select groups.*,
	(   select cast(group_concat( user_groups.user_id, '') as char)
		from user_groups
		join users on users.user_id = user_groups.user_id
		where user_groups.group_id = groups.group_id
		group by user_groups.group_id
	) as group_userids
from groups
{1}
{2};
{3};
{4};
", functionDescription, where.TrimEnd(new char[] { ';' }), limitOffsetQuery, totalCountQuery, queryCountQuery);

            Catalog.NewQuery(query)
            .Execute(delegate(IDataReader dr) {
                while (dr.Read()) {
                    GroupBE group = Groups_Populate(dr);
                    groups.Add(group);
                }

                if (dr.NextResult() && dr.Read()) {
                    totalCountTemp = DbUtils.Convert.To<uint>(dr["totalcount"], 0);
                }

                if (dr.NextResult() && dr.Read()) {
                    queryCountTemp = DbUtils.Convert.To<uint>(dr["querycount"], 0);
                }
            });
            totalCount = totalCountTemp;
            queryCount = queryCountTemp;
            return groups;
        }

        private GroupBE Groups_Populate(IDataReader dr) {
            GroupBE group = new GroupBE();
            group.CreatorUserId = dr.Read<uint>("group_creator_user_id");
            group.Id = dr.Read<uint>("group_id");
            group.Name = dr.Read<string>("group_name");
            group.RoleId = dr.Read<uint>("group_role_id");
            group.ServiceId = dr.Read<uint>("group_service_id");
            group.TimeStamp = dr.Read<DateTime>("group_last_edit");
            group.UserIds = dr.Read<string>("group_userids");
            return group;
        }
    }
}

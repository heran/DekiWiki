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
using System.Linq;
using System.Text;

using MindTouch.Data;

namespace MindTouch.Deki.Data.MySql {
    public partial class MySqlDekiDataSession {

        //--- Constants ---
        public const string ANON_USERNAME = "Anonymous";

        private static readonly IDictionary<UsersSortField, string> USERS_SORT_FIELD_MAPPING = new Dictionary<UsersSortField, string>() 
            { { UsersSortField.DATE_CREATED, "users.user_create_timestamp" },
              { UsersSortField.EMAIL, "users.user_email" },
              { UsersSortField.FULLNAME, "users.user_real_name" },
              { UsersSortField.ID, "users.user_id" },
              { UsersSortField.DATE_LASTLOGIN, "users.user_touched" },
              { UsersSortField.NICK, "users.user_name" },
              { UsersSortField.ROLE, "roles.role_name" },
              { UsersSortField.SERVICE, "services.service_description" },
              { UsersSortField.STATUS, "users.user_active" },
              { UsersSortField.USERNAME, "users.user_name" } };


        //--- Methods ---
        public IEnumerable<UserBE> Users_GetByIds(IEnumerable<uint> userIds) {
            if(userIds == null || !userIds.Any()) {
                return new UserBE[0];
            }
            var users = new List<UserBE>();
            var userIdsText = userIds.ToCommaDelimitedString();
            Catalog.NewQuery(
string.Format(@" /* Users_GetByIds */
SELECT  *
FROM    users
WHERE   user_id IN ({0})",
userIdsText))
            .Execute(delegate(IDataReader dr) {
                while(dr.Read()) {
                    var u = Users_Populate(dr);
                    users.Add(u);
                }
            });
            return users;
        }

        public IEnumerable<UserBE> Users_GetByQuery(string usernamefilter, string realnamefilter, string usernameemailfilter, string rolefilter, bool? activatedfilter, uint? groupId, uint? serviceIdFilter, bool? seatFilter, SortDirection sortDir, UsersSortField sortField, uint? offset, uint? limit, out uint totalCount, out uint queryCount) {
            List<UserBE> users = new List<UserBE>();
            uint totalCountTemp = 0;
            uint queryCountTemp = 0;
            StringBuilder joinQuery = new StringBuilder();
            string sortFieldString;
            USERS_SORT_FIELD_MAPPING.TryGetValue(sortField, out sortFieldString);
            if(!string.IsNullOrEmpty(rolefilter) || (sortFieldString ?? string.Empty).StartsWith("roles.")) {
                joinQuery.Append(@"
left join roles
    on users.user_role_id = roles.role_id");
            }
            if((sortFieldString ?? string.Empty).StartsWith("services.")) {
                joinQuery.AppendFormat(@"
left join services
    on users.user_service_id = services.service_id");
            }
            if(groupId != null) {
                joinQuery.AppendFormat(@"
join groups
    on groups.group_id = {0}
join user_groups
    on user_groups.group_id = groups.group_id", groupId.Value);
            }
            StringBuilder whereQuery = new StringBuilder(" where 1=1");
            if(groupId != null) {
                whereQuery.AppendFormat(" AND users.user_id = user_groups.user_id");
            }
            if(!string.IsNullOrEmpty(usernamefilter) && !string.IsNullOrEmpty(realnamefilter)) {
                whereQuery.AppendFormat(" AND (user_name like '{0}%' OR user_real_name like '{1}%')", DataCommand.MakeSqlSafe(usernamefilter), DataCommand.MakeSqlSafe(realnamefilter));
            } else if(!string.IsNullOrEmpty(usernamefilter)) {
                whereQuery.AppendFormat(" AND user_name like '{0}%'", DataCommand.MakeSqlSafe(usernamefilter));
            } else if(!string.IsNullOrEmpty(realnamefilter)) {
                whereQuery.AppendFormat(" AND user_real_name like '{0}%'", DataCommand.MakeSqlSafe(realnamefilter));
            }
            if(!string.IsNullOrEmpty(usernameemailfilter)) {
                whereQuery.AppendFormat(" AND (user_name like '{0}%' OR user_email like '{0}%')", DataCommand.MakeSqlSafe(usernameemailfilter));
            }
            if(activatedfilter != null) {
                whereQuery.AppendFormat(" AND user_active = {0}", activatedfilter.Value ? "1" : "0");
            }
            if(!string.IsNullOrEmpty(rolefilter)) {
                whereQuery.AppendFormat(" AND role_name = '{0}'", DataCommand.MakeSqlSafe(rolefilter));
            }
            if(serviceIdFilter != null) {
                whereQuery.AppendFormat(" AND user_service_id = {0}", serviceIdFilter.Value);
            }
            if(seatFilter != null) {
                whereQuery.AppendFormat(" AND user_seat = {0}", seatFilter.Value ? 1 : 0);
            }

            StringBuilder sortLimitQuery = new StringBuilder();
            if(!string.IsNullOrEmpty(sortFieldString)) {
                sortLimitQuery.AppendFormat(" order by {0} ", sortFieldString);
                if(sortDir != SortDirection.UNDEFINED) {
                    sortLimitQuery.Append(sortDir.ToString());
                }
            }
            if(limit != null || offset != null) {
                sortLimitQuery.AppendFormat(" limit {0} offset {1}", limit ?? int.MaxValue, offset ?? 0);
            }
            string query = string.Format(@" /* Users_GetByQuery */
select *
from users
{0}
{1}
{2};
select count(*) as totalcount from users {0} {3};
select count(*) as querycount from users {0} {1};",
                joinQuery,
                whereQuery,
                sortLimitQuery,
                groupId == null ? string.Empty : "where users.user_id = user_groups.user_id");
            Catalog.NewQuery(query)
            .Execute(delegate(IDataReader dr) {
                while(dr.Read()) {
                    UserBE u = Users_Populate(dr);
                    users.Add(u);
                }
                if(dr.NextResult() && dr.Read()) {
                    totalCountTemp = DbUtils.Convert.To<uint>(dr["totalcount"], 0);
                }
                if(dr.NextResult() && dr.Read()) {
                    queryCountTemp = DbUtils.Convert.To<uint>(dr["querycount"], 0);
                }
            });
            totalCount = totalCountTemp;
            queryCount = queryCountTemp;
            return users;
        }

        public IEnumerable<UserBE> Users_GetActiveUsers() {
            return Users_Get(@" /* Users_GetActiveUsers */
SELECT * FROM users WHERE user_active = 1 ORDER BY users.user_name ASC");
        }

        public IEnumerable<UserBE> Users_GetBySeat(bool seated) {
            var query = seated
                ? @" /* Users_GetBySeat(true) */
SELECT * FROM users WHERE user_seat = 1"
                : @" /* Users_GetBySeat(false) */
SELECT * FROM users WHERE user_active = 1 AND user_seat = 0";
            return Users_Get(query);
        }

        private IEnumerable<UserBE> Users_Get(string query) {
            var users = new List<UserBE>();
            Catalog.NewQuery(query)
            .Execute(delegate(IDataReader dr) {
                while(dr.Read()) {
                    users.Add(Users_Populate(dr));
                }
            });
            return users;
        }

        public UserBE Users_GetByName(string userName) {
            if(string.IsNullOrEmpty(userName))
                return null;
            UserBE user = null;
            Catalog.NewQuery(@" /* Users_GetByName */
SELECT  *
FROM    users
WHERE   user_name = ?USERNAME")
            .With("USERNAME", userName)
            .Execute(delegate(IDataReader dr) {
                if(dr.Read()) {
                    user = Users_Populate(dr);
                }
            });
            return user;
        }

        public UserBE Users_GetByExternalName(string externalUserName, uint serviceId) {
            if(string.IsNullOrEmpty(externalUserName))
                return null;
            UserBE user = null;
            Catalog.NewQuery(@" /* Users_GetByExternalName */
select 	*
from 	users 
where 	user_external_name = ?EXTERNAL_NAME
AND     user_service_id = ?SERVICE_ID;")
                .With("EXTERNAL_NAME", externalUserName)
                .With("SERVICE_ID", serviceId)
                .Execute(delegate(IDataReader dr) {
                if(dr.Read()) {
                    user = Users_Populate(dr);
                }
            });
            return user;
        }

        public uint Users_Insert(UserBE newUser) {
            uint userId = Catalog.NewQuery(@" /* Users_Insert */
insert into users
	(user_role_id,user_name,user_password,user_newpassword,user_touched,user_email,user_service_id,user_active,user_real_name,user_external_name,user_create_timestamp,user_language,user_timezone,user_seat)
values
	(?ROLEID, ?USERNAME, ?USERPASSWORD, ?NEWPASSWORD, ?TOUCHED, ?EMAIL, ?SERVICEID, ?ACTIVE, ?REALNAME, ?EXTERNALNAME, ?USER_CREATE_TIMESTAMP, ?USER_LANGUAGE, ?USER_TIMEZONE, ?USER_SEAT);
select LAST_INSERT_ID();")
                .With("USERPASSWORD", newUser._Password)
                .With("NEWPASSWORD", newUser._NewPassword)
                .With("ROLEID", newUser.RoleId)
                .With("USERNAME", newUser.Name)
                .With("TOUCHED", newUser._Touched)
                .With("EMAIL", newUser.Email)
                .With("SERVICEID", newUser.ServiceId)
                .With("ACTIVE", newUser.UserActive)
                .With("REALNAME", newUser.RealName)
                .With("EXTERNALNAME", newUser.ExternalName)
                .With("USER_CREATE_TIMESTAMP", newUser.CreateTimestamp)
                .With("USER_TIMEZONE", newUser.Timezone)
                .With("USER_LANGUAGE", newUser.Language)
                .With("USER_SEAT", newUser.LicenseSeat ? 1 : 0)
                .ReadAsUInt() ?? 0;

            return userId;
        }

        public void Users_Update(UserBE user) {
            if(user == null || user.ID == 0)
                return;

            Catalog.NewQuery(@" /* Users_Update */
UPDATE users 
SET user_role_id = ?ROLEID,
user_name = ?USERNAME,
user_password = ?USERPASSWORD,
user_newpassword = ?NEWPASSWORD,
user_touched = ?TOUCHED,
user_email = ?EMAIL,
user_service_id = ?SERVICEID,
user_active = ?ACTIVE,
user_real_name = ?REALNAME,
user_external_name = ?EXTERNALNAME,
user_create_timestamp = ?USER_CREATE_TIMESTAMP,
user_timezone = ?USER_TIMEZONE,
user_language = ?USER_LANGUAGE,
user_seat = ?USER_SEAT
WHERE user_id = ?USERID;")
               .With("USERPASSWORD", user._Password)
               .With("NEWPASSWORD", user._NewPassword)
               .With("ROLEID", user.RoleId)
               .With("USERNAME", user.Name)
               .With("TOUCHED", user._Touched)
               .With("EMAIL", user.Email)
               .With("SERVICEID", user.ServiceId)
               .With("ACTIVE", user.UserActive)
               .With("REALNAME", user.RealName)
               .With("USERID", user.ID)
               .With("EXTERNALNAME", user.ExternalName)
               .With("USER_CREATE_TIMESTAMP", user.CreateTimestamp)
               .With("USER_TIMEZONE", user.Timezone)
               .With("USER_LANGUAGE", user.Language)
               .With("USER_SEAT", user.LicenseSeat)
               .Execute();
        }

        public IEnumerable<uint> Users_UpdateServicesToLocal(uint oldServiceId) {

            List<uint> userIds = new List<uint>();
            string query = string.Format(@"/* Users_UpdateServicesToLocal */
SELECT user_id 
FROM users
WHERE user_service_id = ?OLDSERVICEID;

UPDATE users SET 
user_service_id = 1,
user_external_name = null
where user_service_id = ?OLDSERVICEID;
");

            Catalog.NewQuery(query)
            .With("OLDSERVICEID", oldServiceId)
            .Execute(delegate(IDataReader dr) {
                while(dr.Read()) {
                    userIds.Add((uint)dr.GetInt32(0));

                }
            });
            return userIds;
        }

        public uint Users_GetCount() {

            // retrieve the number of users on this wiki
            string query = String.Format("SELECT COUNT(*) as user_count FROM users WHERE user_active=1 AND user_name!='{0}'", ANON_USERNAME);
            return Catalog.NewQuery(query).ReadAsUInt() ?? 0;
        }

        public UserMetrics Users_GetUserMetrics(uint userId) {
            var ret = new UserMetrics();

            ret.CommentPosts = Catalog.NewQuery(
@" /* GetUserMetrics:Comment Posts */
	SELECT COUNT(*)
	FROM comments
	WHERE cmnt_poster_user_id = ?USERID
	AND cmnt_deleter_user_id IS NULL;")
    .With("USERID", userId)
    .ReadAsUInt() ?? 0;

            ret.DownRatings = Catalog.NewQuery(
@" /* GetUserMetrics:down ratings*/
    SELECT COUNT(*)
	FROM ratings
	WHERE rating_reset_timestamp IS NULL
	AND rating_user_id = ?USERID
	AND rating_score = 0;")
    .With("USERID", userId)
    .ReadAsUInt() ?? 0;

            ret.UpRatings = Catalog.NewQuery(
@" /* GetUserMetrics:up ratings */
    SELECT COUNT(*)
	FROM ratings
	WHERE rating_reset_timestamp IS NULL
	AND rating_user_id = ?USERID
	AND rating_score = 1;")
.With("USERID", userId)
.ReadAsUInt() ?? 0;

            ret.PagesCreated = Catalog.NewQuery(
@" /* GetUserMetrics:pages created */
   SELECT (
		SELECT COUNT(*) AS freshpages
		FROM pages
		WHERE page_revision = 1
		AND page_user_id = ?USERID
		) + ( 
		SELECT COUNT(*) AS oldinitialpages
		FROM `old`
		WHERE old_revision = 1
		AND old_user = ?USERID
		) AS pagescreated;")
.With("USERID", userId)
.ReadAsUInt() ?? 0;

            ret.PagesChanged = Catalog.NewQuery(
@" /* GetUserMetrics:pages changed */
	SELECT (
		SELECT COUNT(*) AS freshpages
		FROM pages
		WHERE page_user_id = ?USERID
		) + ( 
		SELECT COUNT(*) AS oldpages 
		FROM (
		SELECT old_page_id
		FROM `old`
		WHERE old_user = ?USERID
		GROUP BY old_page_id) p
		) AS pageschanges;")
.With("USERID", userId)
.ReadAsUInt() ?? 0;

            ret.FilesUploaded = Catalog.NewQuery(
@" /* GetUserMetrics:files uploaded*/
	SELECT COUNT(*)
	FROM resourcerevs
	JOIN resources
		ON res_id = resrev_res_id
	WHERE res_type = 2
	AND resourcerevs.resrev_user_id = ?USERID
	AND resourcerevs.resrev_change_mask & 1 = 1
	ORDER BY res_id;")
.With("USERID", userId)
.ReadAsUInt() ?? 0;

            return ret;
        }

        private UserBE Users_Populate(IDataReader dr) {
            UserBE user = new UserBE();
            user._NewPassword = dr.Read<byte[]>("user_newpassword");
            user._Password = dr.Read<byte[]>("user_password");
            user._Touched = dr.Read<string>("user_touched");
            user.CreateTimestamp = dr.Read<DateTime>("user_create_timestamp");
            user.Email = dr.Read<string>("user_email");
            user.ExternalName = dr.Read<string>("user_external_name");
            user.ID = dr.Read<uint>("user_id");
            user.Language = dr.Read<string>("user_language");
            user.Name = dr.Read<string>("user_name");
            user.RealName = dr.Read<string>("user_real_name");
            user.RoleId = dr.Read<uint>("user_role_id");
            user.ServiceId = dr.Read<uint>("user_service_id");
            user.Timezone = dr.Read<string>("user_timezone");
            user.UserActive = dr.Read<bool>("user_active");
            user.LicenseSeat = dr.Read<bool>("user_seat");
            return user;
        }
    }
}

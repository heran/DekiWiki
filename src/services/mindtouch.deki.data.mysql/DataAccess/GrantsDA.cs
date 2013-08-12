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
using System.Data.Common;
using System.Linq;

using MindTouch.Data;

namespace MindTouch.Deki.Data.MySql {
    public partial class MySqlDekiDataSession {

        //--- Class Methods ---
        public IList<GrantBE> Grants_GetByPage(uint pageId) {
            List<GrantBE> grants = new List<GrantBE>();
            DataSet ds = Catalog.NewQuery(
@" /* Grants_GetByPage */
select 
	?PAGEID as pageid,
	ug.user_grant_id as grantid,
	ug.user_id as userid,
	ug.last_edit as last_edit, 
	ug.creator_user_id as grant_creator_userid, 
	ug.expire_date as grant_expire_date,
	r.role_id as role_id,
	r.role_name as role_name, 
	r.role_perm_flags as role_perm_flags, 
	r.role_last_edit as role_last_edit, 
	r.role_creator_user_id as role_creator_userid
from user_grants ug 
join roles r on
	ug.role_id = r.role_id
where 
	ug.page_id = ?PAGEID;

select 
	?PAGEID as pageid,
	gg.group_grant_id as grantid,
	gg.group_id as groupid,
	gg.last_edit as last_edit, 
	gg.creator_user_id as grant_creator_userid, 
	gg.expire_date as grant_expire_date, 
	r.role_id as role_id,
	r.role_name as role_name, 
	r.role_perm_flags as role_perm_flags, 
	r.role_last_edit as role_last_edit, 
	r.role_creator_user_id as role_creator_userid
from group_grants as gg
join roles r on
	gg.role_id = r.role_id
where
	gg.page_id = ?PAGEID;")
            .With("PAGEID", pageId)
            .ReadAsDataSet();
            IDataReader userGrantsDr = ds.Tables[0].CreateDataReader();
            IDataReader groupGrantsDr = ds.Tables[1].CreateDataReader();

            // populate rows from user given grants from first recordset
            while(userGrantsDr.Read()) {
                grants.Add(Grants_Populate(userGrantsDr, GrantType.USER));
            }

            // populate rows from group associated grants from second recordset
            while(groupGrantsDr.Read()) {
                grants.Add(Grants_Populate(groupGrantsDr, GrantType.GROUP));
            };
            return grants;
        }


        public void Grants_DeleteByPage(IList<ulong> pageIds) {
            if (ArrayUtil.IsNullOrEmpty(pageIds)) {
                return;
            }
            string pageIdsString = pageIds.ToCommaDelimitedString();
            string query = String.Format(@" /* Grants_DeleteByPage */
DELETE FROM group_grants
WHERE page_id in ({0});
DELETE FROM user_grants 
WHERE page_id in ({0});", pageIdsString);
            Catalog.NewQuery(query)
            .Execute();
        }

        public void Grants_Delete(IList<uint> userGrantIds, IList<uint> groupGrantIds) {
            string userGrantIdsString = "NULL";
            string groupGrantIdsString = "NULL";
            if (!ArrayUtil.IsNullOrEmpty(userGrantIds)) {
                userGrantIdsString = userGrantIds.ToCommaDelimitedString();
            }
            if (!ArrayUtil.IsNullOrEmpty(groupGrantIds)) {
                groupGrantIdsString = groupGrantIds.ToCommaDelimitedString();
            }
            Catalog.NewQuery(string.Format(@" /* Grants_Delete */
delete from user_grants where user_grant_id in ({0});
delete from group_grants where group_grant_id in ({1});", userGrantIdsString, groupGrantIdsString))
                    .Execute();
        }

        public uint Grants_Insert(GrantBE grant) {
            DataCommand cmd = null;

            // create the insertion command
            string query = string.Empty;
            if (grant.Type == GrantType.USER) {
                query = @" /* Grants_Insert (user) */
insert into user_grants (page_id, user_id, role_id, creator_user_id, expire_date)
values (?PAGEID, ?USERID, ?ROLEID, ?CREATORID, ?EXPIREDATE);
select LAST_INSERT_ID();";
                cmd = Catalog.NewQuery(query)
                    .With("userid", grant.UserId);

            } else if (grant.Type == GrantType.GROUP) {
                query = @" /* Grants_Insert (group) */
insert into group_grants (page_id, group_id, role_id, creator_user_id, expire_date)
values (?PAGEID, ?GROUPID, ?ROLEID, ?CREATORID, ?EXPIREDATE);
select LAST_INSERT_ID();";
                cmd = Catalog.NewQuery(query)
                    .With("groupid", grant.GroupId);
            }
            if (grant.ExpirationDate == DateTime.MaxValue) {
                cmd.With("expiredate", DBNull.Value);
            } else {
                cmd.With("expiredate", grant.ExpirationDate);
            }
            return cmd.With("pageid", grant.PageId)
                .With("roleid", grant.RoleId)
                .With("creatorid", grant.CreatorUserId)
                .ReadAsUInt() ?? 0;
        }

        public void Grants_CopyToPage(ulong sourcePageId, ulong targetPageId) {
            Catalog.NewQuery(@" /* Grants_CopyToPage */
delete from user_grants where page_id = ?TARGETPAGEID;
insert into user_grants (page_id, user_id, role_id, creator_user_id, expire_date) 
select ?TARGETPAGEID, user_id, role_id, creator_user_id, expire_date from user_grants where page_id = ?SOURCEPAGEID;
 
delete from group_grants where page_id = ?TARGETPAGEID;
insert into group_grants (page_id, group_id, role_id, creator_user_id, expire_date) 
select ?TARGETPAGEID, group_id, role_id, creator_user_id, expire_date from group_grants where page_id = ?SOURCEPAGEID;
 
UPDATE pages SET page_restriction_id =
(SELECT page_restriction_id FROM (
SELECT DISTINCT page_restriction_id FROM pages WHERE page_id = ?SOURCEPAGEID
) as temp) 
where page_id = ?TARGETPAGEID;")
                .With("SOURCEPAGEID", sourcePageId)
                .With("TARGETPAGEID", targetPageId)
                .Execute();
        }

        public Dictionary<ulong, PermissionStruct> Grants_CalculateEffectiveForPages(uint userId, IEnumerable<ulong> pageIds) {
            return Grants_CalculateEffectivePermissions<ulong>(new uint[] { userId }, pageIds, false);
        }

        public Dictionary<uint, PermissionStruct> Grants_CalculateEffectiveForUsers(ulong pageId, IEnumerable<uint> userIds) {
            return Grants_CalculateEffectivePermissions<uint>(userIds, new ulong[] { pageId }, true);
        }

        private Dictionary<T, PermissionStruct> Grants_CalculateEffectivePermissions<T>(IEnumerable<uint> userIds, IEnumerable<ulong> pageIds, bool keyByUser) {
            Dictionary<T, PermissionStruct> ret = new Dictionary<T, PermissionStruct>();
            pageIds = pageIds.Distinct();
            userIds = userIds.Distinct();

            // check if there are any results to return at all
            if ((pageIds.Count() == 0) || (userIds.Count() == 0)) {
                return ret;
            }
            string pageIdsStr = pageIds.ToCommaDelimitedString();
            string userIdsStr = userIds.ToCommaDelimitedString();

            string selectUserPermissions = @"       
        ( (
        SELECT  CAST(r.role_perm_flags AS unsigned)
        FROM    users      AS u
                JOIN roles AS r
                ON      u.user_role_id = r.role_id
        WHERE   u.user_id              =  mainu.user_id
        ) |
        (
        SELECT  CAST(bit_or(r.role_perm_flags) AS unsigned)
        FROM    user_groups AS ug
                JOIN groups AS g
                ON      ug.group_id = g.group_id
                JOIN roles AS r
                ON      g.group_role_id = r.role_id
        WHERE   ug.user_id              =  mainu.user_id
        ) ) AS effective_rights_flags";

            string selectForZeroPageId = string.Format(@"
SELECT  mainp.page_id as pageid, mainu.user_id as userid,
0 AS page_restriction_flags, 
0 AS effective_grant_flags,
{0}
FROM (select 0 as page_id) mainp
JOIN users mainu
WHERE   mainu.user_id IN ({1})", selectUserPermissions, userIdsStr);

            string selectForNonZeroPageIds = string.Format(@"
SELECT  mainp.page_id as pageid, mainu.user_id as userid,
        (SELECT rs.restriction_perm_flags
        FROM    pages p
                LEFT JOIN restrictions rs
                ON      p.page_restriction_id = rs.restriction_id
        WHERE   p.page_id                     = mainp.page_id
        ) AS page_restriction_flags,	
    (select (
	select ifnull((
	SELECT  r.role_perm_flags AS user_grant_flags
        FROM    user_grants u
                JOIN roles r
                ON      u.role_id = r.role_id
        WHERE   u.user_id         = mainu.user_id
            AND u.page_id         = mainp.page_id
	),0)
	) | (        
	select ifnull((
	SELECT  bit_or(r.role_perm_flags) AS group_grant_flags
        FROM    user_groups ug
                JOIN groups g
                ON      ug.group_id = g.group_id
                JOIN group_grants gg
                ON      ug.group_id=gg.group_id
                JOIN roles AS r
                ON      gg.role_id = r.role_id
        WHERE   ug.user_id         = mainu.user_id
            AND gg.page_id         = mainp.page_id
	),0)
    )) as effective_grant_flags,
    {0}
FROM    pages mainp
JOIN    users mainu
WHERE   mainp.page_id IN ({1})
AND     mainu.user_id IN ({2})
", selectUserPermissions, pageIdsStr, userIdsStr);

            string query;
            if (pageIds.Contains<ulong>(0)) {
                if (pageIds.Count() > 1) {
                    query = String.Format("({0}) UNION ({1})", selectForZeroPageId, selectForNonZeroPageIds);
                } else {
                    query = selectForZeroPageId;
                }
            } else {
                query = selectForNonZeroPageIds;
            }

            Catalog.NewQuery(query)
            .Execute(delegate(IDataReader dr) {
                while (dr.Read()) {
                    T pageid = dr.Read<T>("pageid", default(T));
                    T userid = dr.Read<T>("userid", default(T));
                    ulong effective_rights_flags = dr.Read<ulong>("effective_rights_flags", 0);
                    ulong effective_grant_flags = dr.Read<ulong>("effective_grant_flags", 0);
                    ulong page_restriction_flags = dr.Read<ulong>("page_restriction_flags", 0);
                    if (page_restriction_flags == 0)
                        page_restriction_flags = ulong.MaxValue;
                    PermissionStruct flags = new PermissionStruct(effective_rights_flags, page_restriction_flags, effective_grant_flags);
                    if (keyByUser) {
                        ret[userid] = flags;
                    } else {
                        ret[pageid] = flags;
                    }
                }
            });

            return ret;
        }

        private GrantBE Grants_Populate(IDataReader dr, GrantType grantType) {
            GrantBE grant = new GrantBE();
            grant.Id = dr.Read<uint>("grantid");
            grant.PageId = dr.Read<uint>("pageid");
            if (grantType == GrantType.USER)
                grant.UserId = dr.Read<uint>("userid");
            else if (grantType == GrantType.GROUP)
                grant.GroupId = dr.Read<uint>("groupid");
            grant.ExpirationDate = dr.Read<DateTime>("grant_expire_date", DateTime.MaxValue);
            grant.TimeStamp = dr.Read<DateTime>("last_edit");
            grant.CreatorUserId = dr.Read<uint>("grant_creator_userid");
            grant.Role = new RoleBE();
            grant.RoleId = grant.Role.ID = dr.Read<uint>("role_id");
            grant.Role.Name = dr.Read<string>("role_name", string.Empty);
            grant.Role.PermissionFlags = dr.Read<ulong>("role_perm_flags");
            grant.Role.TimeStamp = dr.Read<DateTime>("role_last_edit");
            grant.Role.CreatorUserId = dr.Read<uint>("role_creator_userid");
            grant.Role.Type = RoleType.ROLE;
            grant.Type = grantType;
            return grant;
        }
    }
}

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

        private IList<BanBE> Bans_GetBansInternal(string where) {
            List<BanBE> bans = new List<BanBE>();

            Catalog.NewQuery(string.Format(@" /* BanningDA::GetBans */
select b.*,
	( select group_concat(bi.banip_ipaddress SEPARATOR '\n')
	  from banips bi
	  where bi.banip_ban_id = b.ban_id
	  group by bi.banip_ban_id
	) as ban_addresses,
	( select cast(group_concat(bu.banuser_user_id SEPARATOR '\n') as char)
	  from banusers bu
	  where bu.banuser_ban_id = b.ban_id
	  group by bu.banuser_ban_id
	) as ban_users
from bans b
{0}
order by b.ban_last_edit desc;", where))
            .Execute(delegate(IDataReader dr) {
                while (dr.Read()) {
                    BanBE b = Bans_Populate(dr);
                    bans.Add(b);
                }
            });

            return bans;
        }

        public IList<BanBE> Bans_GetAll() {
            return Bans_GetBansInternal(string.Empty);
        }

        public uint Bans_Insert(BanBE ban) {

            //build banusers insert query
            StringBuilder userIdInsertQuery = new StringBuilder();
            if (ban.BanUserIds != null && ban.BanUserIds.Count > 0) {
                userIdInsertQuery.Append("insert into banusers (banuser_user_id, banuser_ban_id) values ");
                for (int i = 0; i < ban.BanUserIds.Count; i++) {
                    userIdInsertQuery.AppendFormat("{0}({1}, @banid)", i > 0 ? "," : string.Empty, ban.BanUserIds[i]);
                }

                userIdInsertQuery.Append(";");
            }

            //build banips insert query
            StringBuilder addressesInsertQuery = new StringBuilder();
            if (ban.BanAddresses != null && ban.BanAddresses.Count > 0) {
                addressesInsertQuery.Append("insert into banips (banip_ipaddress, banip_ban_id) values ");
                for (int i = 0; i < ban.BanAddresses.Count; i++) {
                    addressesInsertQuery.AppendFormat("{0}('{1}', @banid)", i > 0 ? "," : string.Empty, DataCommand.MakeSqlSafe(ban.BanAddresses[i]));
                }

                addressesInsertQuery.Append(";");
            }

            string query = string.Format(@" /*  Bans_Insert */
insert into bans (ban_by_user_id, ban_expires, ban_reason, ban_revokemask, ban_last_edit)
values(?BAN_BY_USER_ID, ?BAN_EXPIRES, ?BAN_REASON, ?BAN_REVOKEMASK, ?BAN_LAST_EDIT);
select LAST_INSERT_ID();
select LAST_INSERT_ID() into @banid;
{0}
{1}", userIdInsertQuery, addressesInsertQuery);

            return uint.Parse(Catalog.NewQuery(query)
            .With("BAN_BY_USER_ID", ban.ByUserId)
            .With("BAN_EXPIRES", ban.Expires)
            .With("BAN_REASON", ban.Reason)
            .With("BAN_REVOKEMASK", ban.RevokeMask)
            .With("BAN_LAST_EDIT", ban.LastEdit)
            .Read());
        }

        public void Bans_Delete(uint banId) {
            Catalog.NewQuery(@" /* Bans_Delete */
delete from bans where ban_id = ?BAN_ID;
delete from banips where banip_ban_id = ?BAN_ID;
delete from banusers where banuser_ban_id = ?BAN_ID;")
            .With("BAN_ID", banId)
            .Execute();
        }

        public IList<BanBE> Bans_GetByRequest(uint userid, IList<string> ips) {
            StringBuilder sb = new StringBuilder();
            foreach(string ip in ips){
                if( sb.Length > 0){
                    sb.Append(", ");
                }
                sb.AppendFormat("'{0}'", DataCommand.MakeSqlSafe(ip));
            }

            if (sb.Length == 0) {
                sb.Append("NULL");
            }

            //Note: expiration not applied in query to allow for query caching
            string where = string.Format(@" 
where b.ban_id in (
select b.ban_id 
from bans b
left join banips bi on b.ban_id = bi.banip_ban_id
where	bi.banip_ipaddress in ({0})
union 
select b.ban_id 
from bans b
left join banusers bu on b.ban_id = bu.banuser_ban_id
where bu.banuser_user_id = {1})", sb.ToString(), userid);
            return Bans_GetBansInternal(where);
        }

        private BanBE Bans_Populate(IDataReader dr) {
            BanBE ban = new BanBE();
            ban._BanAddresses = dr.Read<string>("ban_addresses");
            ban._BanUserIds = dr.Read<string>("ban_users");
            ban.ByUserId = dr.Read<uint>("ban_by_user_id");
            ban.Expires = dr.Read<DateTime?>("ban_expires", DateTime.MaxValue);
            ban.Id = dr.Read<uint>("ban_id");
            ban.LastEdit = dr.Read<DateTime>("ban_last_edit", DateTime.MinValue);
            ban.Reason = dr.Read<string>("ban_reason");
            ban.RevokeMask = dr.Read<ulong>("ban_revokemask");
            return ban;
        }
    }
}

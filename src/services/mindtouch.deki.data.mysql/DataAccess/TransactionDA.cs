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
using MindTouch.Deki.Data;

namespace MindTouch.Deki.Data.MySql {
    public partial class MySqlDekiDataSession {

        public TransactionBE Transactions_GetById(uint transid) {
            TransactionBE t = null;
            Catalog.NewQuery(@" /* Transactions_GetById */
select *
from transactions
where t_id = ?TID")
                  .With("TID", transid)
                  .Execute(delegate(IDataReader dr) {
                if (dr.Read()) {
                    t = Transactions_Populate(dr);
                }
            });
            return t;
        }

        public uint Transactions_Insert(TransactionBE trans) {
            return Catalog.NewQuery(@" /* Transactions_Insert */
insert into `transactions` (`t_timestamp`,`t_user_id`,`t_page_id`,`t_title`,`t_namespace`,`t_type`) 
values ( ?TS, ?USERID, ?PAGEID, ?TITLE, ?NS, ?TYPE);
select last_insert_id();"
                ).With("TS", trans.TimeStamp)
                 .With("USERID", trans.UserId)
                 .With("PAGEID", trans.PageId)
                 .With("TITLE", trans.Title.AsUnprefixedDbPath())
                 .With("NS", (int) trans.Title.Namespace)
                 .With("TYPE", (int) trans.Type)
                 .ReadAsUInt() ?? 0;
        }

        public void Transactions_Update(TransactionBE trans) {
            string revertuserid = null;
            if (trans.RevertUserId != null)
                revertuserid = trans.RevertUserId.Value.ToString();

            DataCommand cmd = Catalog.NewQuery(@" /* Transactions_Update */
update `transactions` set 
`t_user_id`= ?USERID,
`t_page_id`= ?PAGEID,
`t_title`= ?TITLE,
`t_namespace`= ?NS,
`t_type`= ?TYPE,
`t_reverted`= ?REVERTED,
`t_revert_user_id` = ?REVERT_USERID,
`t_revert_timestamp` = ?REVERT_TS,
`t_revert_reason` = ?REVERT_REASON
where `t_id`= ?TID;"
                ).With("TID", trans.Id)
                 .With("USERID", trans.UserId)
                 .With("PAGEID", trans.PageId)
                 .With("TITLE", trans.Title.AsUnprefixedDbPath())
                 .With("NS", (int) trans.Title.Namespace)
                 .With("TYPE", (int) trans.Type)
                 .With("REVERTED", trans.Reverted)
                 .With("REVERT_REASON", trans.RevertReason);

            //TODO MaxM: This is a workaround for Datacommand.with not taking nullables. 
            if (trans.RevertUserId.HasValue)
                cmd = cmd.With("REVERT_USERID", trans.RevertUserId.Value);
            else
                cmd = cmd.With("REVERT_USERID", DBNull.Value);

            if (trans.RevertTimeStamp.HasValue)
                cmd = cmd.With("REVERT_TS", trans.RevertTimeStamp.Value);
            else
                cmd = cmd.With("REVERT_TS", DBNull.Value);
            cmd.Execute();
        }

        public void Transactions_Delete(uint transId) {
            Catalog.NewQuery(@" /* Transactions_Delete */
delete from transactions where t_id = ?TID"
            ).With("TID", transId)
             .Execute();
        }

        private TransactionBE Transactions_Populate(IDataReader dr) {
            TransactionBE transaction = new TransactionBE();
            transaction._Namespace = dr.Read<ushort>("t_namespace");
            transaction._Title = dr.Read<string>("t_title");
            transaction._Type = dr.Read<uint>("t_type");
            transaction.Id = dr.Read<uint>("t_id");
            transaction.PageId = dr.Read<uint>("t_page_id");
            transaction.Reverted = dr.Read<bool>("t_reverted");
            transaction.RevertReason = dr.Read<string>("t_revert_reason");
            transaction.RevertTimeStamp = dr.Read<DateTime?>("t_revert_timestamp");
            transaction.RevertUserId = dr.Read<uint?>("t_revert_user_id");
            transaction.TimeStamp = dr.Read<DateTime>("t_timestamp");
            transaction.UserId = dr.Read<uint>("t_user_id");
            return transaction;
        }
    }
}

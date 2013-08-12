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

using MindTouch.Data;

namespace MindTouch.Deki.Data.MySql {
    public partial class MySqlDekiDataSession {
        public void Archive_MovePagesTo(IList<ulong> pageIdsToArchive, uint transactionId) {
            if(ArrayUtil.IsNullOrEmpty(pageIdsToArchive)) {
                return;
            }

            var pageIdsStr = pageIdsToArchive.ToCommaDelimitedString();
            Catalog.NewQuery(string.Format(@" /* Archive_MovePagesTo */
 	INSERT INTO archive (ar_namespace, ar_title, ar_text, ar_comment, ar_user, ar_timestamp, ar_minor_edit, ar_last_page_id, ar_old_id, ar_content_type, ar_language, ar_display_name, ar_transaction_id, ar_is_hidden, ar_meta, ar_revision) 
 		select page_namespace, page_title, old_text, old_comment, old_user, old_timestamp, old_minor_edit, old_page_id, old_id, old_content_type, old_language, old_display_name, ?TRANID, old_is_hidden, old_meta, old_revision
		from `old` o 
		join pages p
		on	o.old_page_id = p.page_id
		where p.page_id in ({0});

	INSERT INTO archive (ar_namespace, ar_title, ar_text, ar_comment, ar_user, ar_timestamp, ar_minor_edit, ar_last_page_id, ar_content_type, ar_language, ar_display_name, ar_transaction_id, ar_is_hidden, ar_meta, ar_revision) 
		select page_namespace, page_title, page_text, page_comment, page_user_id, page_timestamp, page_minor_edit, p.page_id, page_content_type, page_language, page_display_name, ?TRANID, page_is_hidden, page_meta, page_revision
		from `pages` p
		where page_is_redirect=0
        and p.page_id in ({0});

    delete from old
    where  old_page_id in ({0});

	delete from pages
	where page_id in ({0});", pageIdsStr))
                 .With("TRANID", transactionId)
                 .Execute();
        }

        public void Archive_Delete(IList<uint> archiveIds) {
            if (ArrayUtil.IsNullOrEmpty(archiveIds)) {
                return;
            }

            var archiveIdsString = archiveIds.ToCommaDelimitedString();
            Catalog.NewQuery(string.Format(@" /* Archive_Delete */
delete from archive
where ar_id in ({0});",
            archiveIdsString)).Execute();
        }

        public IList<KeyValuePair<uint, IList<ArchiveBE>>> Archive_GetPagesByTitleTransactions(Title title, uint? trans_offset, uint? trans_limit, out Dictionary<uint, TransactionBE> transactionsById, out uint? queryTotalTransactionCount) {
            string innerSelectQuery = String.Empty;
            if (title != null) {
                innerSelectQuery = @" 
select transactions.*
from archive
join transactions
	on t_id = ar_transaction_id
where ar_title like ?TITLE
and (?NS = 0 or ar_namespace = ?NS )
and ar_old_id = 0
and t_reverted = 0
group by t_id";
            } else {
                innerSelectQuery = @"
select 	*
from	transactions	
where	t_type = 5
and	    t_reverted = 0
and 	t_page_id is not null";
            }
            
            DataCommand cmd = Catalog.NewQuery(String.Format(@" /* Archive_GetPagesByTitleTransactions */
select * 
from (
	select  t.*, archive.*
	from archive
	join ( 		
		{0}
		order by t_timestamp desc
		limit ?LIMIT offset ?OFFSET
	) t
	on t.t_id = ar_transaction_id
	where ar_old_id = 0
) a
order by a.t_timestamp desc, a.ar_title asc;

select count(*) as queryTotalTransactionCount 
from ( 
{0} 
) a;", innerSelectQuery))
            .With("LIMIT", trans_limit ?? UInt32.MaxValue)
            .With("OFFSET", trans_offset ?? 0);
            if(title != null) {
                cmd.With("TITLE", "%" + title.AsUnprefixedDbPath() + "%")
                   .With("NS", (int) title.Namespace);
            }

            return Archive_PopulateByTransactionQuery(cmd, out transactionsById, out queryTotalTransactionCount);
        }

        private IList<KeyValuePair<uint, IList<ArchiveBE>>> Archive_PopulateByTransactionQuery(DataCommand cmd, out Dictionary<uint, TransactionBE> transactionsById, out uint? queryTotalTransactionCount) {
            Dictionary<uint, TransactionBE> transactionsByIdtemp = new Dictionary<uint, TransactionBE>();
            List<KeyValuePair<uint, IList<ArchiveBE>>> archivedPagesByTransaction = new List<KeyValuePair<uint, IList<ArchiveBE>>>();
            uint? queryTotalTransactionCountTemp = null;

            cmd.Execute(delegate(IDataReader dr) {

                KeyValuePair<uint, IList<ArchiveBE>> currentTrans = new KeyValuePair<uint, IList<ArchiveBE>>();
                while (dr.Read()) {

                    //Populate list of archived pages per trans. (assumes sort by transaction with first archived page being the transaction 'root')
                    uint tranId = DbUtils.Convert.To<uint>(dr["t_id"]) ?? 0;
                    ArchiveBE archivedPage = Archive_Populate(dr);
                    TransactionBE tran = null;
                    if (!transactionsByIdtemp.TryGetValue(tranId, out tran)) {
                        tran = Transactions_Populate(dr);
                        transactionsByIdtemp[tranId] = tran;
                    }

                    if (currentTrans.Key != tranId) {
                        currentTrans = new KeyValuePair<uint, IList<ArchiveBE>>(tranId, new List<ArchiveBE>());
                        archivedPagesByTransaction.Add(currentTrans);
                    }

                    currentTrans.Value.Add(archivedPage);
                }

                if (dr.NextResult() && dr.Read()) {
                    queryTotalTransactionCountTemp = DbUtils.Convert.To<uint>(dr["queryTotalTransactionCount"]);
                }
            });
            transactionsById = transactionsByIdtemp;
            queryTotalTransactionCount = queryTotalTransactionCountTemp;
            return archivedPagesByTransaction;
        }

        public IList<ArchiveBE> Archive_GetPagesInTransaction(ulong pageId, out uint transactionId) {
            uint tempTranId = 0;
            List<ArchiveBE> archivedPagesInTransaction = new List<ArchiveBE>();
            Catalog.NewQuery(@" /* Archive_GetPagesInTransaction */
select  t.*, archive.*
from archive
join ( 		
	select 	*
	from	transactions	
	where	t_type = 5
	and     t_reverted = 0
	and 	t_page_id = ?PAGEID
	order by t_timestamp desc
	limit 1
) t
on t.t_id = ar_transaction_id
where ar_old_id = 0
order by ar_title asc;")
                .With("PAGEID", pageId)
                .Execute(delegate(IDataReader dr) {
                while (dr.Read()) {
                    tempTranId = DbUtils.Convert.To<uint>(dr["t_id"]) ?? 0;
                    //Populate list of archived pages in trans. (assumes sort by transaction with first archived page being the transaction 'root')
                    ArchiveBE archive = Archive_Populate(dr);
                    archivedPagesInTransaction.Add(archive);
                }
            });
            transactionId = tempTranId;
            return archivedPagesInTransaction;
        }

        public Dictionary<ulong, IList<ArchiveBE>> Archive_GetRevisionsByPageIds(IList<ulong> pageIds) {
            Dictionary<ulong, IList<ArchiveBE>> archivesRevsByPageId = new Dictionary<ulong, IList<ArchiveBE>>();
            string pageidsString = pageIds.ToCommaDelimitedString();
            Catalog.NewQuery(string.Format(@" /* Archive_GetRevisionsByPageIds */
select 	*
from	archive
where 	ar_last_page_id in ({0})
order by ar_last_page_id desc, ar_timestamp asc
", pageidsString)).Execute(delegate(IDataReader dr) {
                while (dr.Read()) {

                    IList<ArchiveBE> revisions = null;
                    ArchiveBE p = Archive_Populate(dr);
                    archivesRevsByPageId.TryGetValue(p.LastPageId, out revisions);
                    if (revisions == null)
                        revisions = new List<ArchiveBE>();

                    revisions.Add(p);
                    archivesRevsByPageId[p.LastPageId] = revisions;
                }
            });

            return archivesRevsByPageId;
        }

        public ArchiveBE Archive_GetPageHeadById(ulong pageId) {
            ArchiveBE page = null;

            Catalog.NewQuery(@" /* Archive_GetPageHeadById */
select *
from archive
where ar_last_page_id = ?PAGEID
and ar_old_id = 0;")
                   .With("PAGEID", pageId)
                   .Execute(delegate(IDataReader dr) {
                       if (dr.Read()) {
                           page = Archive_Populate(dr);
                       }
                    });
            return page;
        }

        public uint Archive_GetCountByTitle(Title title) {
            return Catalog.NewQuery(@" /* Archive_GetCountByTitle */
SELECT COUNT(*) 
FROM archive 
WHERE ar_namespace = ?NS and ar_title = ?TITLE;")
                                                .With("TITLE", title.AsUnprefixedDbPath())
                                                .With("NS", (int) title.Namespace)
                                                .ReadAsUInt() ?? 0;
        }

        private ArchiveBE Archive_Populate(IDataReader dr) {
            ArchiveBE archive = new ArchiveBE();
            archive._Comment = dr.Read<byte[]>("ar_comment");
            archive._DisplayName = dr.Read<string>("ar_display_name");
            archive._Namespace = dr.Read<ushort>("ar_namespace");
            archive._Title = dr.Read<string>("ar_title");
            archive._TimeStamp = dr.Read<string>("ar_timestamp");
            archive.ContentType = dr.Read<string>("ar_content_type");
            archive.Id = dr.Read<uint>("ar_id");
            archive.IsHidden = dr.Read<bool>("ar_is_hidden");
            archive.Language = dr.Read<string>("ar_language");
            archive.LastPageId = dr.Read<ulong>("ar_last_page_id");
            archive.Meta = dr.Read<string>("ar_meta");
            archive.MinorEdit = dr.Read<bool>("ar_minor_edit");
            archive.OldId = dr.Read<ulong>("ar_old_id");
            archive.Text = dr.Read<string>("ar_text");
            archive.TransactionId = dr.Read<uint>("ar_transaction_id");
            archive.UserID = dr.Read<uint>("ar_user");
            archive.Revision = dr.Read<uint>("ar_revision");
            return archive;
        }
    }
}

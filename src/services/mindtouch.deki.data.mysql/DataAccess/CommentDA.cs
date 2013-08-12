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
using MySql.Data.MySqlClient;

namespace MindTouch.Deki.Data.MySql {
    public partial class MySqlDekiDataSession {

        public CommentBE Comments_GetById(uint commentId) {
            CommentBE comment = null;
            Catalog.NewQuery(@" /* Comments_GetById */
select  *
from	`comments`
where   `cmnt_id` = ?COMMENTID")
                .With("COMMENTID", commentId)
                .Execute(delegate(IDataReader dr) {
                if(dr.Read()) {
                    comment = Comments_Populate(dr);
                }
            });
            return comment;
        }

        public IList<CommentBE> Comments_GetByPage(PageBE page, CommentFilter searchStatus, bool includePageDescendants, uint? postedByUserId, SortDirection datePostedSortDir, uint? offset, uint? limit, out uint totalComments) {
            List<CommentBE> comments = new List<CommentBE>();
            uint totalCommentsTemp = 0;

            string joins = string.Empty;
            if(includePageDescendants) {
                joins = "JOIN pages \n\tON comments.cmnt_page_id = pages.page_id";
            }

            string whereClauses = "\n 1=1";
            switch(searchStatus) {
                case CommentFilter.DELETED:
                    whereClauses += "\n AND cmnt_delete_date is not null";
                    break;
                case CommentFilter.NONDELETED:
                    whereClauses += "\n AND cmnt_delete_date is null";
                    break;
            }

            if(includePageDescendants) {
                whereClauses += @"
AND( 
(   ((  ?TITLE = '' AND pages.page_title != '') OR (LEFT(pages.page_title, CHAR_LENGTH(?TITLE) + 1) = CONCAT(?TITLE, '/') AND SUBSTRING(pages.page_title, CHAR_LENGTH(?TITLE) + 2, 1) != '/'))
        AND	pages.page_namespace = ?NS
	    AND	pages.page_is_redirect = 0 
    )
OR 	pages.page_id = ?PAGEID)";

            } else {
                whereClauses += "\n AND cmnt_page_id = ?PAGEID";
            }

            if(postedByUserId != null) {
                whereClauses += "\n AND cmnt_poster_user_id = ?POSTERID";
            }

            string orderby = string.Empty;
            if(datePostedSortDir != SortDirection.UNDEFINED) {
                orderby = string.Format("ORDER BY cmnt_create_date {0}, cmnt_id {0}", datePostedSortDir.ToString());
            }

            List<uint> userids = new List<uint>();
            string query = string.Format(@" /* Comments_GetByPage */
SELECT SQL_CALC_FOUND_ROWS comments.*
FROM comments
{0}
WHERE
{1}
{2}
LIMIT ?COUNT OFFSET ?OFFSET;

select found_rows() as totalcomments;"
                , joins, whereClauses, orderby);

            Catalog.NewQuery(query)
            .With("PAGEID", page.ID)
            .With("TITLE", page.Title.AsUnprefixedDbPath())
            .With("NS", (int)page.Title.Namespace)
            .With("POSTERID", postedByUserId)
            .With("COUNT", limit ?? UInt32.MaxValue)
            .With("OFFSET", offset ?? 0)
            .Execute(delegate(IDataReader dr) {
                while(dr.Read()) {
                    CommentBE c = Comments_Populate(dr);
                    if(c.DeleterUserId != null) {
                        userids.Add(c.DeleterUserId ?? 0);
                    }
                    if(c.LastEditUserId != null) {
                        userids.Add(c.LastEditUserId ?? 0);
                    }
                    userids.Add(c.PosterUserId);
                    comments.Add(c);
                }

                if(dr.NextResult() && dr.Read()) {
                    totalCommentsTemp = DbUtils.Convert.To<uint>(dr["totalcomments"]) ?? 0;
                }
            });

            totalComments = totalCommentsTemp;
            return comments;
        }

        public IList<CommentBE> Comments_GetByUser(uint userId) {
            List<CommentBE> ret = new List<CommentBE>();
            Catalog.NewQuery(@" /* Comments_GetByUser */
select  *
from    `comments`
where   `cmnt_poster_user_id` = ?CMNT_POSTER_USER_ID
order by `cmnt_create_date` desc
")
            .With("CMNT_POSTER_USER_ID", userId)
            .Execute(delegate(IDataReader dr) {
                while(dr.Read()) {
                    CommentBE c = Comments_Populate(dr);
                    ret.Add(c);
                }
            });

            return ret;
        }


        public CommentBE Comments_GetByPageIdNumber(ulong pageId, ushort commentNumber) {
            CommentBE comment = null;
            Catalog.NewQuery(@" /* Comments_GetByPageIdNumber */
select  *
from	`comments`
where	`cmnt_page_id` = ?PAGEID
and	    `cmnt_number` = ?NUMBER;")
                    .With("PAGEID", pageId)
                    .With("NUMBER", commentNumber)
                    .Execute(delegate(IDataReader dr) {
                if(dr.Read()) {
                    comment = Comments_Populate(dr);
                }
            });
            return comment;
        }

        public uint Comments_GetCountByPageId(ulong pageId) {
            return Catalog.NewQuery(@" /* Comments_GetCountByPageId */
SELECT COUNT(*) 
FROM comments 
WHERE cmnt_page_id = ?PAGEID 
AND cmnt_delete_date IS NULL;")
                .With("PAGEID", pageId).ReadAsUInt() ?? 0;
        }

        public uint Comments_Insert(CommentBE comment, out ushort commentNumber) {
            uint commentId = 0;
            ushort tempCommentNumber = 0;
            var attempts = 3;
            while(attempts > 0) {
                try {
                    Catalog.NewQuery(@" /* Comments_Insert */
insert into `comments` (`cmnt_page_id`, `cmnt_poster_user_id`, `cmnt_content`, `cmnt_content_mimetype`, `cmnt_title`, `cmnt_create_date`, `cmnt_number`)
select ?PAGE_ID, ?POSTER_USER_ID, ?CONTENT, ?MIMETYPE, ?TITLE, ?TIMESTAMP,
(select ifnull((select max(`cmnt_number`) from 	`comments` where `cmnt_page_id` = ?PAGE_ID), 0)+1);
select cmnt_id, cmnt_number from comments where cmnt_id = LAST_INSERT_ID();")
                        .With("PAGE_ID", comment.PageId)
                        .With("POSTER_USER_ID", comment.PosterUserId)
                        .With("CONTENT", comment.Content)
                        .With("MIMETYPE", comment.ContentMimeType)
                        .With("TITLE", comment.Title)
                        .With("TIMESTAMP", comment.CreateDate)
                        .Execute(delegate(IDataReader dr) {
                        if(dr.Read()) {
                            commentId = dr.Read<uint>("cmnt_id");
                            tempCommentNumber = dr.Read<ushort>("cmnt_number");
                        }
                    });
                } catch(MySqlException e) {

                    // trap for duplicate key (since comment number calculation is a race condition)
                    if(e.Number == 1062) {
                        attempts--;
                        if(attempts > 0) {
                            continue;
                        }
                        throw new CommentConcurrencyException(comment.PageId, e);
                    }
                    throw;
                }
                break;
            }
            commentNumber = tempCommentNumber;
            return commentId;
        }

        public void Comments_Update(CommentBE comment) {
            Catalog.NewQuery(@" /* Comments_Update */
update 	comments
set 	cmnt_last_edit_user_id = ?LAST_EDIT_USER_ID,
		cmnt_last_edit = ?EDIT_TIMESTAMP,
		cmnt_content = ?CONTENT,
		cmnt_content_mimetype = ?MIMETYPE,
        cmnt_deleter_user_id = ?DELETER_USER_ID,
 	    cmnt_delete_date = ?DELETE_TIMESTAMP
where	cmnt_id = ?COMMENTID;")
                    .With("COMMENTID", comment.Id)
                    .With("LAST_EDIT_USER_ID", comment.LastEditUserId)
                    .With("EDIT_TIMESTAMP", comment.LastEditDate)
                    .With("CONTENT", comment.Content)
                    .With("MIMETYPE", comment.ContentMimeType)
                    .With("DELETER_USER_ID", comment.DeleterUserId)
                    .With("DELETE_TIMESTAMP", comment.DeleteDate)
                    .Execute();
        }

        private CommentBE Comments_Populate(IDataReader dr) {
            CommentBE comment = new CommentBE();
            comment.Content = dr.Read<string>("cmnt_content");
            comment.ContentMimeType = dr.Read<string>("cmnt_content_mimetype");
            comment.CreateDate = dr.Read<DateTime>("cmnt_create_date");
            comment.DeleteDate = dr.Read<DateTime?>("cmnt_delete_date");
            comment.DeleterUserId = dr.Read<uint?>("cmnt_deleter_user_id");
            comment.Id = dr.Read<ulong>("cmnt_id");
            comment.LastEditDate = dr.Read<DateTime?>("cmnt_last_edit");
            comment.LastEditUserId = dr.Read<uint?>("cmnt_last_edit_user_id");
            comment.Number = dr.Read<ushort>("cmnt_number");
            comment.PageId = dr.Read<ulong>("cmnt_page_id");
            comment.PosterUserId = dr.Read<uint>("cmnt_poster_user_id");
            comment.Title = dr.Read<string>("cmnt_title");
            return comment;
        }
    }
}

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
using MindTouch.Data;
using MindTouch.Xml;

namespace MindTouch.Deki.Data.MySql {

    public partial class MySqlDekiDataSession {

        //--- Constants ---
        private const int MAXCOMMENTLENGTH = 255;

        //--- Class Methods ---
        public XDoc RecentChanges_GetPageRecentChanges(PageBE page, DateTime since, bool recurse, bool createOnly, uint? limit) {
            string query = @"/* RecentChanges_GetPageRecentChanges */
SELECT 
	rc_id, rc_comment, rc_cur_id, rc_last_oldid, rc_this_oldid, rc_namespace, rc_timestamp, rc_title, rc_type, rc_moved_to_ns, rc_moved_to_title, 
	user_name AS rc_user_name, user_real_name as rc_full_name,
	(page_id IS NOT NULL) AS rc_page_exists, 
	IF(page_id IS NULL, 0, page_revision) AS rc_revision, 
	cmnt_id, cmnt_number, cmnt_content, cmnt_content_mimetype, (cmnt_delete_date IS NOT NULL) as cmnt_deleted,
    old_is_hidden
FROM recentchanges 
LEFT JOIN old ON rc_this_oldid=old_id
LEFT JOIN users ON rc_user=user_id 
LEFT JOIN comments ON ((rc_type=40 AND cmnt_page_id=rc_cur_id AND rc_user=cmnt_poster_user_id AND STR_TO_DATE(rc_timestamp,'%Y%m%e%H%i%s')=cmnt_create_date) OR (rc_type=41 AND cmnt_page_id=rc_cur_id AND rc_user=cmnt_last_edit_user_id AND STR_TO_DATE(rc_timestamp,'%Y%m%e%H%i%s')=cmnt_last_edit) OR (rc_type=42 AND cmnt_page_id=rc_cur_id AND rc_user=cmnt_deleter_user_id AND STR_TO_DATE(rc_timestamp,'%Y%m%e%H%i%s')=cmnt_delete_date))
JOIN (
       ( SELECT page_id, page_namespace, page_title, page_revision
         FROM pages
         WHERE page_id = {0} ) 
       UNION
       ( SELECT page_id, page_namespace, page_title, page_revision
         FROM pages
         where ({4}) AND (page_is_redirect=0 OR page_is_redirect IS NULL) 
       )
)p ON rc_cur_id = p.page_id
{1} {2} 
ORDER BY rc_timestamp DESC, rc_id DESC 
LIMIT {3}";
            query = string.Format(query, page.ID, Nav_GetTimestampQuery(since), Nav_GetChangeTypeLimitQuery(createOnly), limit ?? UInt32.MaxValue, recurse ? string.Format("(page_title like '{1}%') AND (page_namespace={0} AND (('{1}' = '' AND page_title != '') OR (LEFT(page_title, CHAR_LENGTH('{1}') + 1) = CONCAT('{1}', '/') AND SUBSTRING(page_title, CHAR_LENGTH('{1}') + 2, 1) != '/')))", (int)page.Title.Namespace, DataCommand.MakeSqlSafe(page.Title.AsUnprefixedDbPath())) : string.Format("page_id={0}", page.ID));
            return Catalog.NewQuery(query).ReadAsXDoc("table", "change");
        }

        public XDoc RecentChanges_GetSiteRecentChanges(DateTime since, string language, bool createOnly, NS nsFilter, uint? limit, int maxScanSize) {
            string query = @"/* RecentChanges_GetSiteRecentChanges */
SELECT 
	rc_id, rc_comment, rc_cur_id, rc_last_oldid, rc_this_oldid, rc_namespace, rc_timestamp, rc_title, rc_type, rc_moved_to_ns, rc_moved_to_title, 
	user_name AS rc_user_name, user_real_name as rc_full_name,
	(page_id IS NOT NULL) AS rc_page_exists, 
	IF(page_id IS NULL, 0, page_revision) AS rc_revision, 
	cmnt_id, cmnt_number, cmnt_content, cmnt_content_mimetype, (cmnt_delete_date IS NOT NULL) as cmnt_deleted,
    old_is_hidden
FROM recentchanges FORCE INDEX (PRIMARY)
LEFT JOIN old ON rc_this_oldid=old_id
LEFT JOIN users ON rc_user=user_id 
LEFT JOIN comments ON ((rc_type=40 AND cmnt_page_id=rc_cur_id AND rc_user=cmnt_poster_user_id AND STR_TO_DATE(rc_timestamp,'%Y%m%e%H%i%s')=cmnt_create_date) OR (rc_type=41 AND cmnt_page_id=rc_cur_id AND rc_user=cmnt_last_edit_user_id AND STR_TO_DATE(rc_timestamp,'%Y%m%e%H%i%s')=cmnt_last_edit) OR (rc_type=42 AND cmnt_page_id=rc_cur_id AND rc_user=cmnt_deleter_user_id AND STR_TO_DATE(rc_timestamp,'%Y%m%e%H%i%s')=cmnt_delete_date))
LEFT JOIN pages ON rc_cur_id=page_id 
WHERE (page_is_redirect=0 OR page_is_redirect IS NULL)
AND rc_id > ( select count(*) from recentchanges ) - {5}
{0} {1} {2} {3}
ORDER BY rc_timestamp DESC, rc_id DESC 
LIMIT {4}";
            //NOTE (maxm): only scanning at most the top maxScanSize rows of the recentchanges table to look for matching changes
            query = string.Format(query, Nav_GetTimestampQuery(since), Nav_GetChangeTypeLimitQuery(createOnly), Nav_GetLanguageQuery(language), GetNameSpaceFilterQuery(nsFilter), limit, maxScanSize);
            return Catalog.NewQuery(query).ReadAsXDoc("table", "change");
        }

        public XDoc RecentChanges_GetUserContributionsRecentChanges(uint contributorId, DateTime since, uint? limit) {
            string query = @"/* RecentChanges_GetUserContributionsRecentChanges */
SELECT 
	rc_id, rc_comment, rc_cur_id, rc_last_oldid, rc_this_oldid, rc_namespace, rc_timestamp, rc_title, rc_type, rc_moved_to_ns, rc_moved_to_title, 
	user_name AS rc_user_name, user_real_name as rc_full_name,
	(page_id IS NOT NULL) AS rc_page_exists, 
	IF(page_id IS NULL, 0, page_revision) AS rc_revision, 
	cmnt_id, cmnt_number, cmnt_content, cmnt_content_mimetype, (cmnt_delete_date IS NOT NULL) as cmnt_deleted,
    old_is_hidden
FROM recentchanges
LEFT JOIN old ON rc_this_oldid=old_id
LEFT JOIN users ON rc_user=user_id 
LEFT JOIN comments ON ((rc_type=40 AND cmnt_page_id=rc_cur_id AND rc_user=cmnt_poster_user_id AND STR_TO_DATE(rc_timestamp,'%Y%m%e%H%i%s')=cmnt_create_date) OR (rc_type=41 AND cmnt_page_id=rc_cur_id AND rc_user=cmnt_last_edit_user_id AND STR_TO_DATE(rc_timestamp,'%Y%m%e%H%i%s')=cmnt_last_edit) OR (rc_type=42 AND cmnt_page_id=rc_cur_id AND rc_user=cmnt_deleter_user_id AND STR_TO_DATE(rc_timestamp,'%Y%m%e%H%i%s')=cmnt_delete_date))
LEFT JOIN pages ON rc_cur_id=page_id 
WHERE rc_user={0} AND (page_is_redirect=0 OR page_is_redirect IS NULL) 
{1} {2} 
ORDER BY rc_timestamp DESC, rc_id DESC 
LIMIT {3}";
            query = string.Format(query, contributorId, Nav_GetTimestampQuery(since), Nav_GetChangeTypeLimitQuery(false), limit ?? UInt32.MaxValue);
            return Catalog.NewQuery(query).ReadAsXDoc("table", "change");
        }

        public XDoc RecentChanges_GetUserFavoritesRecentChanges(uint favoritesId, DateTime since, uint? limit) {
            string query = @"/* RecentChanges_GetUserFavoritesRecentChanges */
SELECT 
	rc_id, rc_comment, rc_cur_id, rc_last_oldid, rc_this_oldid, rc_namespace, rc_timestamp, rc_title, rc_type, rc_moved_to_ns, rc_moved_to_title, 
	user_name AS rc_user_name, user_real_name as rc_full_name,
	(page_id IS NOT NULL) AS rc_page_exists, 
	IF(page_id IS NULL, 0, page_revision) AS rc_revision, 
	cmnt_id, cmnt_number, cmnt_content, cmnt_content_mimetype, (cmnt_delete_date IS NOT NULL) as cmnt_deleted,
    old_is_hidden
FROM recentchanges 
LEFT JOIN old ON rc_this_oldid=old_id
LEFT JOIN users ON rc_user=user_id 
INNER JOIN watchlist AS favorites ON favorites.wl_user={0} AND favorites.wl_title=rc_title AND favorites.wl_namespace=rc_namespace 
LEFT JOIN comments ON ((rc_type=40 AND cmnt_page_id=rc_cur_id AND rc_user=cmnt_poster_user_id AND STR_TO_DATE(rc_timestamp,'%Y%m%e%H%i%s')=cmnt_create_date) OR (rc_type=41 AND cmnt_page_id=rc_cur_id AND rc_user=cmnt_last_edit_user_id AND STR_TO_DATE(rc_timestamp,'%Y%m%e%H%i%s')=cmnt_last_edit) OR (rc_type=42 AND cmnt_page_id=rc_cur_id AND rc_user=cmnt_deleter_user_id AND STR_TO_DATE(rc_timestamp,'%Y%m%e%H%i%s')=cmnt_delete_date))
LEFT JOIN pages ON rc_cur_id=page_id 
WHERE (page_is_redirect=0 OR page_is_redirect IS NULL) 
{1} {2} 
ORDER BY rc_timestamp DESC, rc_id DESC 
LIMIT {3}";
            query = string.Format(query, favoritesId, Nav_GetTimestampQuery(since), Nav_GetChangeTypeLimitQuery(false), limit ?? UInt32.MaxValue);
            return Catalog.NewQuery(query).ReadAsXDoc("table", "change");
        }

        public void RecentChanges_Insert(DateTime timestamp, PageBE page, UserBE user, string comment, ulong lastoldid, RC type, uint movedToNS, string movedToTitle, bool isMinorChange, uint transactionId) {
            string dbtimestamp = DbUtils.ToString(timestamp);
            string pageTitle = String.Empty;
            NS pageNamespace = NS.MAIN;
            ulong pageID = 0;
            uint userID = 0;
            if (page != null) {
                pageTitle = page.Title.AsUnprefixedDbPath() ;
                pageNamespace = page.Title.Namespace;
                pageID = page.ID;
            }

            if (user != null) {
                userID = user.ID;
            }

            string q = "/* RecentChanges_Insert */";
            if(lastoldid > 0) {
                q += @"
UPDATE recentchanges SET rc_this_oldid = ?LASTOLDID 
WHERE rc_namespace = ?NS 
AND rc_title = ?TITLE 
AND rc_this_oldid=0;";
            }
             q += @"
INSERT INTO recentchanges
(rc_timestamp, rc_cur_time, rc_user, rc_namespace, rc_title, rc_comment, rc_cur_id, rc_this_oldid, rc_last_oldid, rc_type, rc_moved_to_ns, rc_moved_to_title, rc_minor, rc_transaction_id)
VALUES
(?TS, ?TS, ?USER, ?NS, ?TITLE, ?COMMENT, ?CURID, 0, ?LASTOLDID, ?TYPE, ?MOVEDTONS, ?MOVEDTOTITLE, ?MINOR, ?TRANID);
";

            if(!string.IsNullOrEmpty(comment) && comment.Length > MAXCOMMENTLENGTH) {
                string segment1 = comment.Substring(0, MAXCOMMENTLENGTH / 2);
                string segment2 = comment.Substring(comment.Length - MAXCOMMENTLENGTH / 2 + 3);
                comment = string.Format("{0}...{1}", segment1, segment2);
            }
            
            Catalog.NewQuery(q)
                .With("TS", dbtimestamp)
                .With("USER", userID)
                .With("NS", pageNamespace)
                .With("TITLE", pageTitle)
                .With("COMMENT", comment)
                .With("CURID", pageID)
                .With("LASTOLDID", lastoldid)
                .With("TYPE", (uint) type)
                .With("MOVEDTONS", movedToNS)
                .With("MOVEDTOTITLE", movedToTitle)
                .With("MINOR", isMinorChange ? 1 : 0)
                .With("TRANID", transactionId)
                .Execute();
        }

        private string GetNameSpaceFilterQuery(NS ns) {
            if(ns == NS.UNKNOWN) {
                return string.Empty;
            } else {
                return string.Format(" AND rc_namespace = {0}", (int)ns);
            }
        }
    }
}

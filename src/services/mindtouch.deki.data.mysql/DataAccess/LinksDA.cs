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

        public IList<KeyValuePair<ulong, Title>> Links_GetInboundLinks(ulong pageId) {
            List<KeyValuePair<ulong, Title>> links = new List<KeyValuePair<ulong, Title>>();
            Catalog.NewQuery(@" /* Links_GetInboundLinks */
SELECT page_id, page_namespace, page_title, page_display_name 
FROM links
JOIN pages
    on page_id = l_from
WHERE l_to = ?PAGEID
AND page_is_redirect = 0;")
            .With("PAGEID", pageId)
            .Execute(delegate(IDataReader dr) {
                while (dr.Read()) {
                    ulong id = dr.Read<ulong>("page_id");
                    Title title = DbUtils.TitleFromDataReader(dr, "page_namespace", "page_title", "page_display_name");
                    links.Add(new KeyValuePair<ulong,Title>(id, title));
                }
            });
            return links;
        }

        public IList<KeyValuePair<ulong, Title>> Links_GetOutboundLinks(ulong pageId) {
            List<KeyValuePair<ulong, Title>> links = new List<KeyValuePair<ulong, Title>>();
            Catalog.NewQuery(@" /* Links_GetOutboundLinks  */
SELECT page_id, page_namespace, page_title, page_display_name 
FROM links 
JOIN pages 
    ON l_to=page_id 
WHERE l_from = ?PAGEID;")
                .With("PAGEID", pageId)
                .Execute(delegate(IDataReader dr) {
                while (dr.Read()) {
                    ulong id = dr.Read<ulong>("page_id");
                    Title title = DbUtils.TitleFromDataReader(dr, "page_namespace", "page_title", "page_display_name");
                    links.Add(new KeyValuePair<ulong,Title>(id, title));
                }
            });
            return links;
        }

        public IList<string> Links_GetBrokenLinks(ulong pageId) {
            List<string> brokenLinks = new List<string>();
            Catalog.NewQuery(@" /* Links_GetBrokenLinks  */
SELECT bl_to 
FROM brokenlinks 
where bl_from = ?PAGEID;")
                .With("PAGEID", pageId)
                .Execute(delegate(IDataReader dr) {
                while (dr.Read()) {
                    brokenLinks.Add(dr.Read<string>("bl_to"));
                }
            });
            return brokenLinks;
        }

        public void Links_UpdateLinksForPage(PageBE page, IList<ulong> outboundLinks, IList<string> brokenLinks) {

            StringBuilder query = new StringBuilder(@" /* Links_UpdateLinksForPage */
DELETE FROM links 
WHERE l_from = ?PAGEID;
DELETE FROM brokenlinks 
WHERE bl_from = ?PAGEID;
");
            // build page's outbound links query
            if(!ArrayUtil.IsNullOrEmpty(outboundLinks)) {
                query.Append(@"
INSERT IGNORE INTO links (l_from, l_to) VALUES ");
                for(int i = 0; i < outboundLinks.Count; i++) {
                    query.AppendFormat("{0}(?PAGEID, {1})", i > 0 ? "," : "", outboundLinks[i]);
                }
                query.Append(";");
            }

            // build page's broken links query
            if(!ArrayUtil.IsNullOrEmpty(brokenLinks)) {
                query.Append(@"
INSERT IGNORE INTO brokenlinks (bl_from, bl_to) VALUES ");
                for(int i = 0; i < brokenLinks.Count; i++) {
                    query.AppendFormat("{0}(?PAGEID, '{1}')", i > 0 ? "," : "", DataCommand.MakeSqlSafe(brokenLinks[i]));
                }
                query.Append(";");
            }

            // build query to update any broken links that point to this page
            query.Append(@"
INSERT IGNORE INTO links (l_from, l_to) 
    SELECT brokenlinks.bl_from, ?PAGEID 
    FROM brokenlinks 
    WHERE bl_to = ?TITLE;
DELETE FROM brokenlinks 
WHERE bl_to = ?TITLE;"
);

            Catalog.NewQuery(query.ToString())
            .With("PAGEID", page.ID)
            .With("TITLE", page.Title.AsPrefixedDbPath())
            .Execute();
        }

        public void Links_MoveInboundToBrokenLinks(IList<ulong> deletedPageIds) {
            if (ArrayUtil.IsNullOrEmpty(deletedPageIds)) {
                return;
            }
            string pageIdsString = deletedPageIds.ToCommaDelimitedString();
            string query = String.Format(@" /* Links_MoveLinksToBrokenLinks */
insert ignore into brokenlinks(bl_from, bl_to)
	select t.l_from, p.page_title
	from pages p
	join (
		select l_from, l_to 
	        from links
        	where l_to in ({0})) t
	on p.page_id = t.l_to;

DELETE links FROM links
where 	l_from in ({0})
OR	l_to in ({0});", pageIdsString);

            Catalog.NewQuery(query)
                   .Execute();
        }
    }
}

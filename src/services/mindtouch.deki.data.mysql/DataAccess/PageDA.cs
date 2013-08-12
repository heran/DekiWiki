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
        private const string PAGEFIELDS =
      @"p.page_id,
       p.page_restriction_id,
       p.page_parent,
       p.page_is_new,
       p.page_content_type,
       p.page_language, 
       p.page_display_name, 
       p.page_touched,
       p.page_tip,
       p.page_is_redirect,
       p.page_title,
       p.page_id,
       p.page_minor_edit,
       p.page_timestamp,
       char_length(p.page_text) AS page_text_length,
       p.page_revision,
       p.page_user_id,
       p.page_comment,
       p.page_usecache,
       p.page_namespace,
       p.page_is_hidden,
       p.page_meta,
       p.page_etag";

        /// <summary>
        /// Returns the id of the "home" page.
        /// </summary>
        public ulong Pages_HomePageId {
            get {
                uint homepageId = Catalog.NewQuery("SELECT page_id FROM pages WHERE page_parent=0 AND page_namespace=0 AND (page_title=\"\" OR page_title=\"Home\")").ReadAsUInt() ?? 0;
                if(homepageId == 0) {
                    throw new HomePageNotFoundException();
                }
                return homepageId;
            }
        }

        public IList<PageBE> Pages_GetByIds(IEnumerable<ulong> pageIds) {
            if(pageIds.Any()) {
                string pageIdsText = pageIds.ToCommaDelimitedString();
                return Pages_GetInternal(string.Format("WHERE p.page_id in ({0})", pageIdsText), null, null, "Pages_GetByIds");
            }
            return new List<PageBE>();
        }

        private IList<PageBE> Pages_GetInternal(string where, string order, string limitoffset, string function) {
            List<PageBE> pages = new List<PageBE>();
            string query = string.Format(@"/* Pages_GetInternal ({4}) */ SELECT {0} from pages p {1} {2} {3};", PAGEFIELDS, where, order, limitoffset, function);
            Catalog.NewQuery(query)
            .Execute(delegate(IDataReader dr) {
                while(dr.Read()) {
                    PageBE p = Pages_PopulatePage(dr);
                    pages.Add(p);
                }
            });

            return pages;
        }

        public IList<PageBE> Pages_GetChildren(ulong parentPageId, NS nameSpace, bool filterOutRedirects) {
            List<PageBE> pages = new List<PageBE>();

            string q = string.Format(@"
                SET group_concat_max_len = @@max_allowed_packet;
                SELECT {0}, 
	            (	    SELECT cast(group_concat( p1.page_id, '') as char)
		                FROM pages p1
		                WHERE 	p1.page_parent = p.page_id
		                AND	p.page_title  <> ''
		                AND	p1.page_namespace = ?NS
		                AND 	p.page_id <> p1.page_id
                        {2}
		                group by p1.page_parent ) 
	            as page_children,
	            (	    SELECT  count(*)
                        FROM    resources
                        WHERE   res_type = 2
                        AND     res_deleted = 0
                        AND     resrev_parent_page_id = p.page_id
	            ) as page_attachment_count
                FROM 	pages p
                WHERE	p.page_parent = ?PARENTPAGEID
                AND		p.page_namespace = ?NS
                AND		p.page_title <> ''
                {1}
                ORDER BY p.page_title; ",
                                        PAGEFIELDS,
                                        filterOutRedirects ? "AND p.page_is_redirect = 0" : string.Empty,
                                        filterOutRedirects ? "AND p1.page_is_redirect = 0" : string.Empty);


            Catalog.NewQuery(q)
                .With("PARENTPAGEID", parentPageId)
                .With("NS", (int)nameSpace)
                .Execute(delegate(IDataReader dr) {
                while(dr.Read()) {
                    PageBE p = Pages_PopulatePage(dr);

                    p.AttachmentCount = DbUtils.Convert.To<uint>(dr["page_attachment_count"].ToString());
                    p.ChildPageIds = DbUtils.ConvertDelimittedStringToArray<ulong>(',', dr["page_children"].ToString());
                    pages.Add(p);
                }
            });

            return pages;
        }

        public IList<PageBE> Pages_GetByTitles(IList<Title> pageTitles) {
            if(pageTitles == null)
                return null;

            if(pageTitles.Count == 0)
                return new List<PageBE>();

            //TODO MaxM: Split up the query if it's over max query length
            var sb = new StringBuilder();
            for(int i = 0; i < pageTitles.Count; i++) {
                if(0 < i) {
                    sb.Append(" OR ");
                }
                sb.AppendFormat(" (p.page_namespace={0} AND p.page_title='{1}')", (uint)pageTitles[i].Namespace, DataCommand.MakeSqlSafe(pageTitles[i].AsUnprefixedDbPath()));
            }

            return Pages_GetInternal(string.Format("WHERE {0}", sb), null, null, "Pages_GetByTitles");
        }


        public IList<PageBE> Pages_GetPopular(string language, uint? offset, uint? limit) {


            string limitAndOffset = (limit == null && offset == null) ? string.Empty : string.Format("limit {0} offset {1}", limit ?? int.MaxValue, offset ?? 0);

            string query = "INNER JOIN page_viewcount as pv ON p.page_id = pv.page_id WHERE p.page_namespace = 0 AND p.page_is_redirect = 0";

            //Add language filter to query
            if(!string.IsNullOrEmpty(language)) {
                query += string.Format(" AND (p.page_language = '' OR p.page_language = '{0}') ", DataCommand.MakeSqlSafe(language));
            }
            return Pages_GetInternal(query, "ORDER BY pv.page_counter DESC", limitAndOffset, "Pages_GetPopular");
        }

        public IList<PageBE> Pages_GetByNamespaces(IList<NS> namespaces, uint? offset, uint? limit) {
            if(ArrayUtil.IsNullOrEmpty(namespaces)) {
                return new PageBE[] { };
            }

            string limitAndOffset = (limit == null && offset == null) ? string.Empty : string.Format("limit {0} offset {1}", limit ?? int.MaxValue, offset ?? 0);
            string query = null;
            if(namespaces != null && namespaces.Count > 0) {
                query = string.Format("WHERE p.page_namespace in ({0})", namespaces.Select(x => (uint)x).ToCommaDelimitedString());
            }
            return Pages_GetInternal(query, "ORDER BY p.page_id", limitAndOffset, "Pages_GetByNamespaces");
        }

        public void Pages_GetDescendants(PageBE rootPage, string language, bool filterOutRedirects, out IList<PageBE> pages, out Dictionary<ulong, IList<ulong>> childrenInfo, int limit) {
            List<PageBE> tempPages = new List<PageBE>();
            Dictionary<ulong, IList<ulong>> children = new Dictionary<ulong, IList<ulong>>();

            string q = string.Format(@" /* Pages_GetDescendants */
                SET group_concat_max_len = @@max_allowed_packet;

                SELECT {0},
	            (
                    if( p.page_title = '',

            			( /*Find children of root page*/
                            SELECT cast(group_concat( p1.page_id, '') as char)
                            FROM        pages p1
                            WHERE       p1.page_parent = 0
                            AND     	p.page_title  = ''
                            AND     	p1.page_namespace = ?NS
                            AND         p.page_id <> p1.page_id
                            AND (?LANGUAGE IS NULL OR p1.page_language = ?LANGUAGE OR p1.page_language = '')
                            GROUP BY p1.page_parent
                        ),		
						
			            ( /* Find children of non-root pages */
                            SELECT cast(group_concat( p1.page_id, '') as char)
                            FROM        pages p1
                            WHERE       p1.page_parent = p.page_id
                            AND    	    p.page_title  <> ''
                            AND     	p1.page_namespace = ?NS
                            AND         p.page_id <> p1.page_id
                            AND (?LANGUAGE IS NULL OR p1.page_language = ?LANGUAGE OR p1.page_language = '')
                            GROUP BY p1.page_parent
                        )
                    )
                ) AS    page_children
	            FROM 	pages p
	            WHERE 
	            (	
                    ((?TITLE = '' AND p.page_title != '') OR (LEFT(p.page_title, CHAR_LENGTH(?TITLE) + 1) = CONCAT(?TITLE, '/') AND SUBSTRING(p.page_title, CHAR_LENGTH(?TITLE) + 2, 1) != '/'))
		            AND	p.page_namespace = ?NS		            
                    AND (?LANGUAGE IS NULL OR p.page_language = ?LANGUAGE OR p.page_language = '')
                    {1}
	            )
	            OR 	p.page_id = ?PAGEID
                LIMIT ?LIMIT;"
                , PAGEFIELDS, filterOutRedirects ? "AND p.page_is_redirect = 0" : string.Empty);

            Catalog.NewQuery(q)
                .With("PAGEID", rootPage.ID)
                .With("TITLE", rootPage.Title.AsUnprefixedDbPath())
                .With("NS", (int)rootPage.Title.Namespace)
                .With("LANGUAGE", language)
                .With("LIMIT", limit)
                .Execute(delegate(IDataReader dr) {
                while(dr.Read()) {
                    PageBE p = Pages_PopulatePage(dr);
                    tempPages.Add(p);
                    List<ulong> childrenIds = new List<ulong>();
                    string childrenStr = dr["page_children"] as string;
                    if(childrenStr != null) {
                        foreach(string s in childrenStr.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries)) {
                            childrenIds.Add(ulong.Parse(s));
                        }
                    }
                    children[p.ID] = childrenIds;
                }
            });
            if(tempPages.Count >= limit) {
                throw new TooManyResultsException();
            }
            pages = tempPages;
            childrenInfo = children;
        }

        public Dictionary<Title, ulong> Pages_GetIdsByTitles(IList<Title> pageTitle) {
            Dictionary<Title, ulong> ids = new Dictionary<Title, ulong>();
            if(0 < pageTitle.Count) {

                // generate query to retrieve all pages matching the criteria
                // TODO (brigette):  this might break for very long queries.  We need to test on a page with many links
                StringBuilder query = new StringBuilder("SELECT page_id, page_namespace, page_title, page_display_name FROM pages WHERE ");
                for(int i = 0; i < pageTitle.Count; i++) {
                    if(0 < i) {
                        query.Append(" OR ");
                    }
                    query.AppendFormat(" (page_namespace={0} AND page_title='{1}')", (uint)pageTitle[i].Namespace, DataCommand.MakeSqlSafe(pageTitle[i].AsUnprefixedDbPath()));
                }
                query.Append(" GROUP BY page_namespace, page_title");

                // execute the query and read the results into a lookup table
                Catalog.NewQuery(query.ToString()).Execute(delegate(IDataReader dr) {
                    while(dr.Read()) {
                        Title info = DbUtils.TitleFromDataReader(dr, "page_namespace", "page_title", "page_display_name");
                        ids[info] = dr.Read<ulong>("page_id");
                    }
                });
            }
            return ids;
        }

        public uint Pages_GetCount() {

            // retrieve the number of articles on this wiki
            uint count = 0;
            string query = String.Format("SELECT COUNT(*) as page_count FROM pages WHERE page_is_redirect=0 AND page_namespace in ({0}, {1})", (uint)NS.MAIN, (uint)NS.USER);
            Catalog.NewQuery(query).Execute(delegate(IDataReader dr) {
                if(dr.Read()) {
                    count = DbUtils.Convert.To<uint>(dr["page_count"], 0);
                }
            });
            return count;
        }

        public IList<PageBE> Pages_GetFavoritesForUser(uint userid) {
            string query = string.Format(
@"JOIN watchlist wl
    ON wl.wl_user = {0} 
	AND wl.wl_title = p.page_title
	AND wl.wl_namespace = p.page_namespace"
, userid);
            return Pages_GetInternal(query, null, null, "Pages_GetFavoritesForUser");
        }


        public ulong Pages_Insert(PageBE page, ulong restoredPageID) {
            var query = @" /* Pages_Insert */
INSERT INTO pages ({0}page_namespace, page_title, page_text, page_comment, page_user_id, page_timestamp, page_is_redirect, page_minor_edit, page_is_new, page_touched, page_usecache, page_toc, page_tip, page_parent, page_restriction_id, page_content_type, page_language, page_display_name, page_etag, page_revision)
VALUES ({1}?NAMESPACE, ?TITLE, ?PAGETEXT, ?PAGECOMMENT, ?USERID, ?PAGETIMESTAMP, ?ISREDIRECT, ?MINOREDIT, ?ISNEW, ?TOUCHED, ?USECACHE, '', ?TIP, ?PARENT, ?RESTRICTIONID, ?CONTENTTYPE, ?LANGUAGE, ?DISPLAYNAME, ?ETAG, ?REVISION);
{2}
REPLACE INTO page_viewcount (page_id,page_counter) VALUES (@page_id,0);
SELECT @page_id";
            query = restoredPageID != 0
                ? string.Format(query, "page_id,", "?PAGE_ID,", "SELECT @page_id := " + restoredPageID + " as page_id;")
                : string.Format(query, "", "", "SELECT @page_id := LAST_INSERT_ID() as page_id;");

            // create the insertion command
            ulong pageId = 0;
            Catalog.NewQuery(query)
                .With("PAGE_ID", restoredPageID)
                .With("NAMESPACE", page.Title.Namespace)
                .With("TITLE", page.Title.AsUnprefixedDbPath())
                .With("PAGETEXT", page.GetText(this.Head))
                .With("PAGECOMMENT", page.Comment)
                .With("USERID", page.UserID)
                .With("PAGETIMESTAMP", page._TimeStamp)
                .With("ISREDIRECT", page.IsRedirect)
                .With("MINOREDIT", page.MinorEdit)
                .With("ISNEW", page.IsNew)
                .With("TOUCHED", page._Touched)
                .With("USECACHE", page.UseCache)
                .With("TIP", page.TIP)
                .With("PARENT", page.ParentID)
                .With("RESTRICTIONID", page.RestrictionID)
                .With("CONTENTTYPE", page.ContentType)
                .With("LANGUAGE", page.Language)
                .With("DISPLAYNAME", page.Title.DisplayName)
                .With("PAGEID", restoredPageID)
                .With("ETAG", page.Etag)
                .With("REVISION", page.Revision)
                .Execute(delegate(IDataReader dr) {
                if(dr.Read()) {
                    pageId = dr.Read<ulong>("page_id");
                }
            });
            return pageId;
        }

        public void Pages_Update(PageBE page) {

            // Update page text only if it has been set
            string updatePageText = String.Empty;
            if(page.IsTextPopulated) {
                updatePageText = ", page_text = ?PAGETEXT";
            }

            string query = String.Format(@" /* Pages_Update */
update pages SET 
page_namespace      = ?NAMESPACE,
page_title          = ?TITLE,
page_comment        = ?PAGECOMMENT,
page_user_id        = ?USERID,
page_timestamp      = ?PAGETIMESTAMP,
page_is_redirect    = ?ISREDIRECT,
page_minor_edit     = ?MINOREDIT,
page_is_new         = ?ISNEW,
page_touched        = ?TOUCHED,
page_usecache       = ?USECACHE,
page_tip            = ?TIP,
page_parent         = ?PARENT,
page_restriction_id = ?RESTRICTIONID,
page_content_type   = ?CONTENTTYPE,
page_language       = ?LANGUAGE,
page_display_name   = ?DISPLAYNAME,
page_etag           = ?ETAG,
page_revision       = ?REVISION
{0}
where page_id       = ?PAGEID
", updatePageText);
            DataCommand cmd = Catalog.NewQuery(query)
            .With("PAGEID", page.ID)
            .With("NAMESPACE", (int)page.Title.Namespace)
            .With("TITLE", page.Title.AsUnprefixedDbPath())
            .With("PAGECOMMENT", page.Comment)
            .With("USERID", page.UserID)
            .With("PAGETIMESTAMP", page._TimeStamp)
            .With("ISREDIRECT", page.IsRedirect)
            .With("MINOREDIT", page.MinorEdit)
            .With("ISNEW", page.IsNew)
            .With("TOUCHED", page._Touched)
            .With("USECACHE", page.UseCache)
            .With("TIP", page.TIP)
            .With("PARENT", page.ParentID)
            .With("RESTRICTIONID", page.RestrictionID)
            .With("CONTENTTYPE", page.ContentType)
            .With("LANGUAGE", page.Language)
            .With("DISPLAYNAME", page.Title.DisplayName)
            .With("ETAG", page.Etag)
            .With("REVISION", page.Revision);
            if(page.IsTextPopulated) {
                cmd.With("PAGETEXT", page.GetText(this.Head));
            }
            cmd.Execute();
        }

        public uint Pages_GetViewCount(ulong pageId) {
            return Catalog.NewQuery("SELECT page_counter FROM page_viewcount WHERE page_id = ?PAGEID")
                .With("PAGEID", pageId)
                .ReadAsUInt() ?? 0;
        }

        public uint Pages_UpdateViewCount(ulong pageId) {
            uint count = 0;
            var updated = 0;
            Catalog.NewQuery(@" /* Pages_UpdateViewCount */
UPDATE page_viewcount SET page_counter = (@counter := page_counter + 1) WHERE page_id = ?PAGEID;
SELECT CAST(@counter AS UNSIGNED), ROW_COUNT();")
            .With("PAGEID", pageId)
            .Execute(reader => {
                reader.Read();
                count = reader.Read<uint?>(0) ?? 0;
                updated = reader.Read<int?>(1) ?? 0;
            });
            if(updated == 0) {
                Catalog.NewQuery(@" /* Pages_UpdateViewCount */
REPLACE INTO page_viewcount (page_id,page_counter) VALUES (?PAGEID,1);")
                    .With("PAGEID", pageId)
                    .Execute();
                count = 1;
            }
            return count;
        }

        public void Pages_UpdateTitlesForMove(Title currentTitle, ulong newParentId, Title title, DateTime touchedTimestamp) {
            string query = @" /* Pages_UpdateTitlesForMove */
	
	-- Delete redirects at source of move
	delete	from pages
	where	
		page_namespace = ?NS
	AND (
		page_title = ?TITLE
     /* OR util_Page_IsAntecedent(?TITLE, page_title) */
        OR ((?TITLE = '' AND page_title != '') OR (LEFT(page_title, CHAR_LENGTH(?TITLE) + 1) = CONCAT(?TITLE, '/') AND SUBSTRING(page_title, CHAR_LENGTH(?TITLE) + 2, 1) != '/'))
	)AND (page_is_redirect=1);

	update	pages 
	set	page_title = CONCAT(?NEWTITLE, SUBSTRING(page_title, CHAR_LENGTH(?TITLE) + 1)),
        /*page_title = util_String_ReplaceStartOfString(page_title, ?TITLE, ?NEWTITLE),*/
        page_namespace = ?NEWNS,
        page_touched = ?TOUCHED
	where	
		page_namespace = ?NS
	AND (
		page_title = ?TITLE
		OR (
            /*util_Page_IsAntecedent(?TITLE, page_title)*/
            ((?TITLE = '' AND page_title != '') OR (LEFT(page_title, CHAR_LENGTH(?TITLE) + 1) = CONCAT(?TITLE, '/') AND SUBSTRING(page_title, CHAR_LENGTH(?TITLE) + 2, 1) != '/'))
            AND page_title like ?TITLELIKE
        )        
	) AND (
        page_is_redirect = 0
    );

	update pages
		set	page_parent = ?NEWPARENTID,
        page_touched = ?TOUCHED
		where page_namespace = ?NEWNS 
        AND page_title = ?NEWTITLE;
";
            Catalog.NewQuery(query)
            .With("NS", (int)currentTitle.Namespace)
            .With("TITLE", currentTitle.AsUnprefixedDbPath())
            .With("NEWPARENTID", newParentId)
            .With("NEWTITLE", title.AsUnprefixedDbPath())
            .With("NEWNS", (int)title.Namespace)
            .With("TITLELIKE", string.Format("{0}%", currentTitle.AsUnprefixedDbPath()))
            .With("TOUCHED", DbUtils.ToString(touchedTimestamp))
            .Execute();
        }

        public Dictionary<ulong, IList<PageBE>> Pages_GetRedirects(IList<ulong> pageIds) {
            Dictionary<ulong, IList<PageBE>> ret = new Dictionary<ulong, IList<PageBE>>();
            if(pageIds == null || pageIds.Count == 0) {
                return ret;
            }
            string pageIdsText = pageIds.ToCommaDelimitedString();
            string query = string.Format(@" /* PageDA::Pages_GetRedirects */
SELECT l_to, {0}
FROM pages p
JOIN links
	ON p.page_id = l_from
WHERE p.page_is_redirect=1 
AND l_to IN({1});
", PAGEFIELDS, pageIdsText);

            Catalog.NewQuery(query)
                .Execute(delegate(IDataReader dr) {
                while(dr.Read()) {
                    PageBE p = Pages_PopulatePage(dr);
                    ulong to = dr.Read<ulong>("l_to");
                    IList<PageBE> redirectsForId = null;
                    if(!ret.TryGetValue(to, out redirectsForId)) {
                        ret[to] = redirectsForId = new List<PageBE>();
                    }
                    redirectsForId.Add(p);
                }
            });
            return ret;
        }

        public IList<ulong> Pages_GetParentIds(PageBE page) {

            //MaxM: This query returns 10 segments of a hierarchy without using a loop or titles and can be query cached. Please don't submit to the dailywtf. :)
            string query = @" /* Pages_GetParentIds */
select p0.page_id, p0.page_parent, p1.page_parent, p2.page_parent, p3.page_parent, p4.page_parent, p5.page_parent, p6.page_parent, p7.page_parent, p8.page_parent, p9.page_parent
from pages p0
left join pages p1 on p0.page_parent = p1.page_id
left join pages p2 on p1.page_parent = p2.page_id
left join pages p3 on p2.page_parent = p3.page_id
left join pages p4 on p3.page_parent = p4.page_id
left join pages p5 on p4.page_parent = p5.page_id
left join pages p6 on p5.page_parent = p6.page_id
left join pages p7 on p6.page_parent = p7.page_id
left join pages p8 on p7.page_parent = p8.page_id
left join pages p9 on p8.page_parent = p9.page_id
where p0.page_id = ?PAGEID;";

            ulong homepageid = Head.Pages_HomePageId;
            int counter = 0;
            ulong id = page.ID;
            bool first = true;
            Dictionary<ulong, object> dupeChecker = new Dictionary<ulong, object>();
            List<ulong> result = new List<ulong>(16);

            // loop until we've found all the parents
            while(counter++ < 20) {

                // execute query for given page
                bool done = false;
                Catalog.NewQuery(query)
                    .With("PAGEID", id)
                    .Execute(delegate(IDataReader dr) {
                    if(dr.Read()) {
                        for(int i = first ? 0 : 1; i < dr.FieldCount; i++) {
                            ulong parentPageId = DbUtils.Convert.To<ulong>(dr.GetValue(i)) ?? 0;
                            if(parentPageId == 0) {

                                // no valid parent found
                                if(page.Title.Namespace == NS.MAIN) {

                                    // add the homepage for pages in the MAIN namespace
                                    result.Add(homepageid);
                                }
                                done = true;
                                break;
                            } else {
                                if(!dupeChecker.ContainsKey(parentPageId)) {
                                    result.Add(parentPageId);
                                    dupeChecker[parentPageId] = null;
                                } else {

                                    //Cycle detected
                                    done = true;
                                }
                            }
                        }
                    } else {
                        done = true;
                    }
                });

                // check if done
                if(done) {
                    return result;
                }

                // continue with last parent id
                id = result[result.Count - 1];
                first = false;
            }
            throw new Exception(string.Format("GetParentPages failed: too many iterations. Starting id: {0} Parent chain: {1}", page.ID, result.ToCommaDelimitedString()));
        }

        public IList<PageTextContainer> Pages_GetContents(IList<ulong> pageids) {
            List<PageTextContainer> ret = new List<PageTextContainer>();
            if(ArrayUtil.IsNullOrEmpty(pageids)) {
                return ret;
            }

            string pageIdsText = pageids.ToCommaDelimitedString();
            Catalog.NewQuery(
string.Format(
@"/* Pages_GetContents */
SELECT p.page_id, p.page_timestamp, p.page_text
FROM pages p
WHERE p.page_id in ({0})",
pageIdsText))
                .Execute(delegate(IDataReader dr) {
                while(dr.Read()) {
                    PageTextContainer ptc = new PageTextContainer(
                        DbUtils.Convert.To<ulong>(dr["page_id"], 0),
                        DbUtils.Convert.To<string>(dr["page_text"], string.Empty),
                        DbUtils.ToDateTime(dr["page_timestamp"].ToString())
                    );

                    ret.Add(ptc);
                }
            });
            return ret;
        }

        private PageBE Pages_PopulatePage(IDataReader dr) {
            PageBE p = new PageBE();
            p._Comment = dr.Read<byte[]>("page_comment");
            p._DisplayName = dr.Read<string>("page_display_name");
            p._Namespace = dr.Read<ushort>("page_namespace");
            p._TimeStamp = dr.Read<string>("page_timestamp");
            p._Title = dr.Read<string>("page_title");
            p._Touched = dr.Read<string>("page_touched");
            p.ContentType = dr.Read<string>("page_content_type");
            p.ID = dr.Read<ulong>("page_id");
            p.IsNew = dr.Read<bool>("page_is_new");
            p.IsHidden = dr.Read<bool>("page_is_hidden");
            p.IsRedirect = dr.Read<bool>("page_is_redirect");
            p.Language = dr.Read<string>("page_language");
            p.Meta = dr.Read<string>("page_meta");
            p.MinorEdit = dr.Read<bool>("page_minor_edit");
            p.ParentID = dr.Read<ulong>("page_parent");
            p.RestrictionID = dr.Read<uint>("page_restriction_id");
            p.Revision = dr.Read<uint>("page_revision");
            p.TextLength = dr.Read<int>("page_text_length");
            p.TIP = dr.Read<string>("page_tip");
            p.UseCache = dr.Read<bool>("page_usecache");
            p.UserID = dr.Read<uint>("page_user_id");
            p.Etag = dr.Read<string>("page_etag");
            return p;
        }
    }

}

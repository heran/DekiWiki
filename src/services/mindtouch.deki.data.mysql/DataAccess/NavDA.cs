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

using MindTouch.Data;

namespace MindTouch.Deki.Data.MySql {
    public partial class MySqlDekiDataSession {

        //--- Methods ---
        public IList<NavBE> Nav_GetTree(PageBE page, bool includeAllChildren) {
            ulong homepageId = Head.Pages_HomePageId;

            // determine if we need to exclude child pages (if requested page is a child page of Special:)
            bool includeChildren = (page.Title.Namespace != NS.SPECIAL && page.Title.Namespace != NS.SPECIAL_TALK);

            // create queries
            string query = @" /* Nav_GetTree */
                SELECT page_id, page_namespace, page_title, page_display_name, pages.page_parent as page_parent, restriction_perm_flags, 
                ({1}) AS page_children
                FROM pages
                LEFT JOIN restrictions ON page_restriction_id = restriction_id
                WHERE {0}";
            string subquery = includeChildren ? "SELECT count(C.page_id) FROM pages C WHERE C.page_parent = pages.page_id AND C.page_id != ?HOMEPAGEID AND C.page_parent != ?HOMEPAGEID AND C.page_namespace = ?PAGENS AND C.page_is_redirect = 0 GROUP BY C.page_parent" : "0";

            // always include the homepage
            List<string> where = new List<string>();
            where.Add("pages.page_id = ?HOMEPAGEID");

            // determine what pages need to be included
            if(page.ID == homepageId) {

                // include all children of the homepage in the main namespace
                where.Add("(pages.page_is_redirect = 0 AND pages.page_parent = 0 AND pages.page_id != ?HOMEPAGEID AND pages.page_namespace = 0)");
            } else {

                // build list of all parent pages, including the current page, but exclusing the homepage, which needs special consideration
                var parentIdsList = Head.Pages_GetParentIds(page).Where(x => (x != homepageId) && (x != 0))
                    .Union(new[] {page.ID})
                    .ToCommaDelimitedString();
                // include all parents with the requested page
                where.Add(string.Format("pages.page_id IN ({0})", parentIdsList));

                // check if the parent pages should also contain all their children pages
                if(includeAllChildren) {
                    if(includeChildren) {

                        // include the children pages of all parents, including the requested page, but excluding the homepage, which needs to be handled differently
                        where.Add(string.Format("(pages.page_is_redirect = 0 AND pages.page_parent IN ({0}) AND pages.page_namespace = ?PAGENS)", parentIdsList));
                    }

                    // check if for the current namespace, we need to show the children of the homepage
                    if(page.Title.ShowHomepageChildren) {
                        where.Add("(pages.page_is_redirect = 0 AND pages.page_parent = 0 AND pages.page_id != ?HOMEPAGEID AND pages.page_namespace = ?PAGENS)");
                    }
                } else {
                    if(includeChildren) {

                        // include only the children of the requested page in the requested namespace
                        where.Add("(pages.page_is_redirect = 0 AND pages.page_parent = ?PAGEID AND pages.page_namespace = ?PAGENS)");
                    }

                    // check if the requested page is not a child of the homepage; otherwise, we need to be careful about including sibling pages
                    if(page.ParentID != 0) {

                        // include all siblings of the requested page since it's not a child of the homepage
                        where.Add("(pages.page_is_redirect = 0 AND pages.page_parent = ?PAGEPARENT AND pages.page_id != ?PAGEID AND pages.page_namespace = ?PAGENS)");
                    } else if(page.Title.ShowHomepageChildren) {

                        // include all siblings of the requested page since the requested namespace includes all homepage children pages
                        where.Add("(pages.page_is_redirect = 0 AND pages.page_parent = 0 AND pages.page_id != ?HOMEPAGEID AND pages.page_namespace = ?PAGENS)");
                    }
                }
            }

            // compose query and execute it
            query = string.Format(query, string.Join(" OR ", where.ToArray()), subquery);
            DataCommand cmd = Catalog.NewQuery(query)
                .With("HOMEPAGEID", Head.Pages_HomePageId)
                .With("PAGEID", page.ID)
                .With("PAGENS", (int)page.Title.Namespace)
                .With("PAGEPARENT", page.ParentID);
            return Nav_GetInternal(cmd);
        }

        public IList<NavBE> Nav_GetSiblings(PageBE page) {
            string query = @" /* Nav_GetSiblings */
                SELECT page_id, page_namespace, page_title, page_display_name, pages.page_parent as page_parent, restriction_perm_flags,
		        (   	SELECT count(C.page_id) 
		                FROM pages C
			            WHERE 	C.page_parent = pages.page_id 
		                AND	    C.page_id != ?HOMEPAGEID 
		                AND 	C.page_parent != ?HOMEPAGEID
		                AND 	C.page_namespace = ?PAGENS 
		                AND 	C.page_is_redirect = 0
		                GROUP BY C.page_parent 
	            )       AS      page_children
		        FROM    pages
		        LEFT JOIN restrictions ON page_restriction_id = restriction_id
                WHERE   pages.page_is_redirect = 0 
                AND (
                            (   ?HOMEPAGEID != ?PAGEID
                                AND pages.page_parent = ?PAGEPARENT 
                                AND pages.page_id != ?HOMEPAGEID 
                                AND pages.page_namespace = ?PAGENS
                            ) 
                            OR  pages.page_id = ?PAGEID 
                ) 
            ";
            DataCommand cmd = Catalog.NewQuery(query)
                .With("HOMEPAGEID", Head.Pages_HomePageId)
                .With("PAGEID", page.ID)
                .With("PAGENS", (int) page.Title.Namespace)
                .With("PAGEPARENT", page.ParentID);
            return Nav_GetInternal(cmd);
        }

        public IList<NavBE> Nav_GetChildren(PageBE page) {
            string query = @" /* Nav_GetChildren */
                SELECT page_id, page_namespace, page_title, page_display_name, pages.page_parent as page_parent, restriction_perm_flags,
		        (   	SELECT count(C.page_id) 
		                FROM pages C
		                WHERE   C.page_parent = pages.page_id 
		                AND	    C.page_id != ?HOMEPAGEID
		                AND 	C.page_parent != ?HOMEPAGEID
		                AND 	C.page_namespace = ?PAGENS 
		                AND 	C.page_is_redirect = 0
		                GROUP BY C.page_parent 
	            )       AS      page_children
		        FROM pages
		        LEFT JOIN restrictions ON page_restriction_id = restriction_id
		        WHERE   pages.page_is_redirect = 0 
		        AND (
                        (       ?HOMEPAGEID != ?PAGEID 
                                AND pages.page_parent = ?PAGEID
                                AND pages.page_namespace = ?PAGENS
                        ) 
			         OR (       ?HOMEPAGEID = ?PAGEID
                                AND pages.page_parent = 0 
                                AND pages.page_id != ?HOMEPAGEID
                                AND pages.page_namespace = ?PAGENS
                        )
                ) 
            ";
            DataCommand cmd = Catalog.NewQuery(query)
                .With("HOMEPAGEID", Head.Pages_HomePageId)
                .With("PAGEID", page.ID)
                .With("PAGENS", (int) page.Title.Namespace);
            return Nav_GetInternal(cmd);
        }

        public IList<NavBE> Nav_GetSiblingsAndChildren(PageBE page) {
            string query = @" /* Nav_GetSiblingsAndChildren */
                SELECT page_id, page_namespace, page_title, page_display_name, pages.page_parent as page_parent, restriction_perm_flags,
		        (   	SELECT count(C.page_id) 
		                FROM pages C
						WHERE   C.page_parent = pages.page_id 
						AND	    C.page_id != ?HOMEPAGEID
						AND 	C.page_parent != ?HOMEPAGEID
						AND 	C.page_namespace = ?PAGENS 
						AND 	C.page_is_redirect = 0
		                GROUP BY C.page_parent 
	            )       AS      page_children
		        FROM pages
		        LEFT JOIN restrictions ON page_restriction_id = restriction_id
                WHERE ( 
                    (   pages.page_is_redirect = 0 
                        AND ?HOMEPAGEID != ?PAGEID 
                        AND pages.page_parent = ?PAGEPARENT 
                        AND pages.page_namespace = ?PAGENS
                        AND pages.page_id != ?HOMEPAGEID
                    ) 
                    OR 
                    (   pages.page_is_redirect = 0 
                        AND ?HOMEPAGEID = ?PAGEID 
                        AND pages.page_parent = 0 
                        AND pages.page_namespace = ?PAGENS
                    )
                    OR
                    (   ?HOMEPAGEID != ?PAGEID 
                        AND pages.page_parent = ?PAGEID
                        AND pages.page_namespace=?PAGENS
                        AND pages.page_is_redirect=0
                    )
                    OR  pages.page_id = ?PAGEID 
                )
            ";
            DataCommand cmd = Catalog.NewQuery(query)
                .With("HOMEPAGEID", Head.Pages_HomePageId)
                .With("PAGEID", page.ID)
                .With("PAGENS", (int) page.Title.Namespace)
                .With("PAGEPARENT", page.ParentID);
            return Nav_GetInternal(cmd);
        }

        public ulong Nav_GetNearestParent(Title title) {
            string query = @"/* Nav_GetNearestParent */
SELECT page_id FROM pages WHERE page_namespace={0} AND STRCMP(SUBSTRING('{1}', 1, CHAR_LENGTH(page_title) + 1), CONCAT(page_title, '/'))=0 ORDER BY CHAR_LENGTH(page_title) DESC LIMIT 1";
            return Catalog.NewQuery(string.Format(query, (int)title.Namespace, DataCommand.MakeSqlSafe(title.AsUnprefixedDbPath()))).ReadAsULong() ?? Head.Pages_HomePageId;
        }

        private RC[] Nav_RecentChangeFeedExcludeList {
            get {
                string[] list = _settings.GetValue("feed/recent-changes-type-exclude-list", string.Empty).Split(new char[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                List<RC> rcList = new List<RC>();
                foreach (string item in list) {
                    RC type;
                    if(SysUtil.TryParseEnum(item, out type)) {
                        rcList.Add(type);
                    }
                }
                return rcList.ToArray();
            }
        }
       
        private string Nav_GetChangeTypeLimitQuery(bool createOnly) {
            if (createOnly) {
                return "AND rc_type = " + (uint)RC.NEW;
            }
            string query = string.Empty;
            RC[] recentChangeFeedExcludeList = Nav_RecentChangeFeedExcludeList;
            if (recentChangeFeedExcludeList.Length > 0) {
                query = "AND rc_type NOT IN({0})";
                string ids = string.Empty;
                for (int i = 0; i < recentChangeFeedExcludeList.Length; i++) {
                    if (ids != string.Empty)
                        ids += ",";

                    ids += string.Format("{0}", (uint)recentChangeFeedExcludeList[i]);
                }
                query = string.Format(query, ids);
            } 
            return query;
        }

        private string Nav_GetTimestampQuery(DateTime since) {
            string timestampFilter = string.Empty;
            if (since > DateTime.MinValue)
                timestampFilter = string.Format(" AND rc_timestamp > '{0}' ", DbUtils.ToString(since));
            return timestampFilter;
        }

        private string Nav_GetLanguageQuery(string language) {
            string languageFilter = string.Empty;
            if (language != null) {

                //Note MaxM: Since this is getting embedded in the WHERE clause instead of an ON clause, allow for null in case the left join doesn't match a row in pages so the entire row doesn't get removed
                languageFilter = string.Format(" AND (page_language = '{0}' OR page_language = '' OR page_language is null ) ", MindTouch.Data.DataCommand.MakeSqlSafe(language));
            }
            return languageFilter;
        }

        private IList<NavBE> Nav_GetInternal(DataCommand queryCommand) {
            List<NavBE> navPages = new List<NavBE>();
            queryCommand.Execute(delegate(IDataReader dr) {
                while (dr.Read()) {
                    NavBE np = Nav_Populate(dr);
                    navPages.Add(np);
                }
            });
            return navPages;
        }

        private NavBE Nav_Populate(IDataReader dr) {
            return new NavBE {
                ChildCount = dr.Read<int?>("page_children"), 
                DisplayName = dr.Read<string>("page_display_name"), 
                Id = dr.Read<uint>("page_id"), 
                NameSpace = dr.Read<ushort>("page_namespace"), 
                ParentId = dr.Read<uint>("page_parent"), 
                RestrictionFlags = dr.Read<ulong?>("restriction_perm_flags"), 
                Title = dr.Read<string>("page_title")
            };
        }
    }
}

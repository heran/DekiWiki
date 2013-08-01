using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

using MindTouch.Data;
using MindTouch.Deki.Data;
using MindTouch.Dream;

namespace MindTouch.Deki.Data.MySql {
    public partial class MySqlDekiDataSession {

        //--- Methods ---
        public IList<Tuplet<string, uint, string>> Wiki_GetContributors(PageBE page, bool byRecent, string exclude, uint? max) {
            DataCommand command = null;
            string query;

            // build exclusion clauses
            string excludeClause = string.Empty;
            if(StringUtil.ContainsInvariantIgnoreCase(exclude, "all") || StringUtil.ContainsInvariantIgnoreCase(exclude, "inactive")) {
                excludeClause = " AND user_active = 1";
            }
            if(StringUtil.ContainsInvariantIgnoreCase(exclude, "all") || StringUtil.ContainsInvariantIgnoreCase(exclude, "banned")) {
                excludeClause += " AND 0 = (select count(*) from banusers inner join bans on banuser_id = ban_id where banuser_user_id = user_id and (ban_expires is null or ban_expires > now()) )";
            }

            // check if a page was provided
            if(page != null) {

                // find contributors for given page
                if(byRecent) {
                    query = string.Format(@" /* DekiWiki-Functions: wiki.contributors (for a page, from recent changes) */
SELECT user_id, user_name, MAX(rc_timestamp) AS last_edit
FROM recentchanges
JOIN users
 ON rc_user = user_id
WHERE rc_type = 0
AND rc_cur_id = ?PAGEID
{0}
GROUP BY rc_user
ORDER BY last_edit DESC
LIMIT {1};", excludeClause, max ?? UInt32.MaxValue);

                } else {
                    query = string.Format(@" /* DekiWiki-Functions: wiki.contributors (for a page, from revision data) */
SELECT users.user_name, users.user_id, COUNT(*) AS user_edits
FROM 
(
 (SELECT page_user_id AS `user`
  FROM pages
  WHERE page_id = ?PAGEID
 )UNION ALL(
  SELECT old_user AS `user`
  FROM `old`
  WHERE old_page_id = ?PAGEID)
) editcounts
JOIN users
 ON users.user_id = editcounts.user
WHERE TRUE
{0}
GROUP BY editcounts.user
ORDER BY user_edits DESC
LIMIT {1};"
          , excludeClause, max ?? UInt32.MaxValue);
                }
                command = Catalog.NewQuery(query)
                          .With("PAGEID", page.ID);
            } else {

                if(byRecent) {
                    query = string.Format(@" /* DekiWiki-Functions: wiki.contributors (all pages, from recentchanges) */
SELECT user_id, user_name, MAX(rc_timestamp) AS last_edit
FROM recentchanges
JOIN users
 ON rc_user = user_id
WHERE rc_type = 0
{0}
GROUP BY rc_user
ORDER BY last_edit DESC
LIMIT {1};
", excludeClause, max);
                } else {
                    // find contributors for entire site
                    query = string.Format(@" /* DekiWiki-Functions: wiki.contributors (all pages, from revision data) */
SELECT user_id, user_name, 
    (SELECT COUNT(*) 
     FROM pages 
     WHERE user_id = page_user_id {0}) +
    (SELECT COUNT(*) 
     FROM old 
     WHERE user_id = old_user {0}) AS user_edits 
FROM users 
ORDER BY user_edits 
DESC LIMIT {1}", excludeClause, max);
                }
                command = Catalog.NewQuery(query);
            }

            List<Tuplet<string, uint, string>> result = new List<Tuplet<string, uint, string>>();
            command.Execute(delegate(IDataReader reader) {
                while(reader.Read()) {
                    string user = SysUtil.ChangeType<string>(reader["user_name"]);
                    if (user.EqualsInvariant(ANON_USERNAME)) {
                        continue;
                    }
                    uint userid = SysUtil.ChangeType<uint>(reader["user_id"]);
                    string classification;
                    if (byRecent) {
                        classification = SysUtil.ChangeType<string>(reader["last_edit"]);
                    } else {
                        classification = SysUtil.ChangeType<string>(reader["user_edits"]);
                    }
                    result.Add(new Tuplet<string, uint, string>(user, userid, classification));
                } 
            }); 
            
            return result;
        }
    }
}

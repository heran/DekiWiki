using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using MySql.Data.MySqlClient;

namespace MindTouch.Deki.Data.MySql {
    public partial class MySqlDekiDataSession {

        public uint Old_Insert(OldBE old, ulong restoredOldID) {

            string query = @" /* Old_Insert */
INSERT INTO old ({0}old_text, old_comment, old_user, old_timestamp, old_minor_edit, old_content_type, old_language, old_display_name, old_is_hidden, old_meta, old_revision, old_page_id)
VALUES ({1}?OLDTEXT, ?OLDCOMMENT, ?OLDUSER, ?OLDTIMESTAMP, ?MINOREDIT, ?CONTENTTYPE, ?LANGUAGE, ?DISPLAYNAME, ?ISHIDDEN, ?META, ?REVISION, ?PAGEID);
{2}
";
            query = restoredOldID != 0
                ? string.Format(query, "old_id,", "?OLD_ID,", "SELECT " + restoredOldID + " old_id")
                : string.Format(query, "", "", "SELECT LAST_INSERT_ID() old_id;");
            uint oldId = 0;
            try {
                Catalog.NewQuery(query)
                    .With("OLD_ID", restoredOldID)
                    .With("OLDTEXT", old.Text)
                    .With("OLDCOMMENT", old.Comment)
                    .With("OLDUSER", old.UserID)
                    .With("OLDTIMESTAMP", old._TimeStamp)
                    .With("MINOREDIT", old.MinorEdit)
                    .With("CONTENTTYPE", old.ContentType)
                    .With("LANGUAGE", old.Language)
                    .With("DISPLAYNAME", old.DisplayName)
                    .With("ISHIDDEN", old.IsHidden ? 1 : 0)
                    .With("META", old.Meta)
                    .With("REVISION", old.Revision)
                    .With("PAGEID", old.PageID)
                    .Execute(delegate(IDataReader dr) {
                        if(dr.Read()) {
                            oldId = dr.Read<uint>("old_id");
                        }
                    });
            } catch(MySqlException e) {
                switch(e.Number) {
                case 1062:
                    Catalog.NewQuery(@"/* Old_Insert (revision cleanup) */
UPDATE pages
  SET page_revision = (SELECT MAX(old_revision) FROM old WHERE old_page_id = page_id)+1 
  WHERE page_id = ?PAGEID
    AND page_revision <= (SELECT MAX(old_revision) FROM old WHERE old_page_id = page_id)")
                    .With("PAGEID", old.PageID)
                    .Execute();
                    _log.Warn("insert into old failed because of a page revision collision, trying defensive cleanup");
                    throw new PageConcurrencyException(old.PageID, e);
                default:
                    throw;
                }
            }
            return oldId;
        }

        public OldBE Old_GetOldByTimestamp(ulong pageId, DateTime timestamp) {

            // retrieve the old revision most closley matching or preceding the specified timestamp
            OldBE old = null;
            string query = @" /* Old_GetOldByTimestamp */
SELECT old_id, old_text, old_comment, old_user, old_timestamp, old_minor_edit, old_content_type, old_language, old_display_name, old_is_hidden, old_meta, old_revision, old_page_id
FROM old
WHERE old_page_id = ?PAGEID
AND old_timestamp <= ?OLDTIMESTAMP
ORDER BY old_timestamp DESC 
LIMIT 1;";

            Catalog.NewQuery(query)
            .With("PAGEID", pageId)
            .With("OLDTIMESTAMP", DbUtils.ToString(timestamp))
            .Execute(delegate(IDataReader dr) {
                if(dr.Read()) {
                    old = Old_Populate(dr);
                }
            });
            return old;
        }

        public OldBE Old_GetOldByRevision(ulong pageId, ulong revision) {
            OldBE old = null;
            string query = @" /* Old_GetOldByRevision */
SET @ROW = 0; 
SELECT o.old_id, old_text, old_comment, old_user, old_timestamp, old_minor_edit, old_content_type, old_language, old_display_name, old_is_hidden, old_meta, old_revision, old_page_id
FROM `old` o
WHERE o.old_page_id = ?PAGEID
  AND old_revision = ?REVISION;
";
            Catalog.NewQuery(query)
            .With("PAGEID", pageId)
            .With("REVISION", revision)
            .Execute(delegate(IDataReader dr) {
                if(dr.Read()) {
                    old = Old_Populate(dr);
                }
            });
            return old;
        }

        public IList<OldBE> Old_GetOldsByQuery(ulong pageId, bool orderDescendingByRev, uint? offset, uint? limit) {
            List<OldBE> olds = new List<OldBE>();

            // populate a list of page revisions with the given namespace and title include old and cur
            string query = @" /* Old_GetOlds */
SELECT * FROM (
  SELECT old_id, old_text, old_comment, old_user, old_timestamp, old_minor_edit, old_content_type, old_language, old_display_name, old_is_hidden, old_meta, old_revision, old_page_id 
    FROM old
    WHERE old_page_id = ?PAGEID
  UNION
  SELECT 0 as old_id,
        page_text as old_text,
	    page_comment as old_comment, 
	    page_user_id as old_user,  
	    page_timestamp as old_timestamp,
	    page_minor_edit as old_minor_edit,
	    page_content_type as old_content_type,
	    page_language as old_language,
	    page_display_name as old_display_name,
        page_is_hidden as old_is_hidden, 
        page_meta as old_meta,
        page_revision as old_revision,
        page_id as old_page_id
    FROM pages
    WHERE page_id = ?PAGEID
) revision_union
  ORDER BY old_revision {0}
  LIMIT ?LIMIT
  OFFSET ?OFFSET
";
            query = string.Format(query, orderDescendingByRev ? "DESC" : "ASC");
            Catalog.NewQuery(query)
                           .With("PAGEID", pageId)
                           .With("OFFSET", offset ?? 0)
                           .With("LIMIT", limit ?? UInt32.MaxValue)
                           .Execute(delegate(IDataReader dr) {

                while(dr.Read()) {
                    OldBE old = Old_Populate(dr);
                    olds.Add(old);
                }
            });
            return olds;
        }

        // TODO (arnec): There should be no blanket update capability on historical information
        public void Old_Update(OldBE old) {

            string query = @" /* Old_Update */
UPDATE old SET
old_text            = ?OLDTEXT,
old_comment         = ?OLDCOMMENT,
old_user            = ?OLDUSER,
old_timestamp       = ?OLDTIMESTAMP,
old_minor_edit      = ?MINOREDIT,
old_content_type    = ?CONTENTTYPE,
old_language        = ?LANGUAGE,
old_display_name    = ?DISPLAYNAME,
old_is_hidden       = ?ISHIDDEN,
old_meta            = ?META,
old_revision        = ?REVISION,
old_page_id         = ?PAGEID
WHERE old_id        = ?OLDID";
            Catalog.NewQuery(query)
                .With("OLDID", old.ID)
                .With("OLDTEXT", old.Text)
                .With("OLDCOMMENT", old.Comment)
                .With("OLDUSER", old.UserID)
                .With("OLDTIMESTAMP", old._TimeStamp)
                .With("MINOREDIT", old.MinorEdit)
                .With("FLAGS", "utf-8")
                .With("CONTENTTYPE", old.ContentType)
                .With("LANGUAGE", old.Language)
                .With("DISPLAYNAME", old.DisplayName)
                .With("ISHIDDEN", old.IsHidden ? 1 : 0)
                .With("META", old.Meta)
                .With("REVISION", old.Revision)
                .With("PAGEID", old.PageID)
               .Execute();
        }

        private OldBE Old_Populate(IDataReader dr) {
            OldBE old = new OldBE();
            old._Comment = dr.Read<byte[]>("old_comment");
            old.DisplayName = dr.Read<string>("old_display_name");
            old._TimeStamp = dr.Read<string>("old_timestamp");
            old.ContentType = dr.Read<string>("old_content_type");
            old.ID = dr.Read<ulong>("old_id");
            old.IsHidden = dr.Read<bool>("old_is_hidden");
            old.Language = dr.Read<string>("old_language");
            old.Meta = dr.Read<string>("old_meta");
            old.MinorEdit = dr.Read<bool>("old_minor_edit");
            old.Revision = dr.Read<uint>("old_revision");
            old.Text = dr.Read<string>("old_text");
            old.UserID = dr.Read<uint>("old_user");
            old.PageID = dr.Read<ulong>("old_page_id");
            return old;
        }
    }
}

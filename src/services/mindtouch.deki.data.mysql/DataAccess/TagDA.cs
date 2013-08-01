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
using System.Linq;
using System.Text;
using System.Data;

using MindTouch.Data;
using MindTouch.Deki.Data;
using MySql.Data.MySqlClient;

namespace MindTouch.Deki.Data.MySql {

    public partial class MySqlDekiDataSession {

        public IList<ulong> Tags_GetPageIds(uint tagid) {
            List<ulong> ids = new List<ulong>();

            // Note that joining on pages is necessary to ensure that the page hasn't been deleted
            Catalog.NewQuery(@" /* Tags_GetPages */
SELECT tag_map.tagmap_page_id FROM pages
JOIN tag_map
    ON page_id = tagmap_page_id
WHERE tagmap_tag_id = ?TAGID;")
            .With("TAGID", tagid)
            .Execute(delegate(IDataReader dr) {
                while(dr.Read()) {
                    ids.Add(SysUtil.ChangeType<ulong>(dr[0]));
                }
            });
            return ids;
        }

        public IList<TagBE> Tags_GetByPageId(ulong pageid) {

            // retrieve the tags associated with a specified page id
            List<TagBE> tags = new List<TagBE>();
            Catalog.NewQuery(@" /* Tags_GetByPageId */
SELECT `tag_id`, `tag_name`, `tag_type`  
FROM `tag_map`  
JOIN `tags` 
    ON `tag_map`.`tagmap_tag_id`=`tags`.`tag_id`  
WHERE `tagmap_page_id` = ?PAGEID; ")
            .With("PAGEID", (uint)pageid)
            .Execute(delegate(IDataReader dr) {
                while(dr.Read()) {
                    TagBE t = Tags_Populate(dr);
                    if(t != null)
                        tags.Add(t);
                }
            });
            return tags;
        }

        public TagBE Tags_GetById(uint tagid) {
            TagBE tag = null;
            Catalog.NewQuery(@" /* Tags_GetById */
SELECT `tag_id`, `tag_name`, `tag_type` 
FROM `tags` 
WHERE `tag_id` = ?TAGID;")
            .With("TAGID", tagid)
            .Execute(delegate(IDataReader dr) {
                while(dr.Read()) {
                    tag = Tags_Populate(dr);
                }
            });
            return tag;
        }

        public TagBE Tags_GetByNameAndType(string tagName, TagType type) {
            TagBE tag = null;
            Catalog.NewQuery(@" /* Tags_GetByNameAndType */
SELECT `tag_id`, `tag_name`, `tag_type`
FROM `tags`
WHERE `tag_name` = ?TAGNAME 
AND `tag_type` = ?TAGTYPE;")
            .With("TAGNAME", tagName)
            .With("TAGTYPE", type)
            .Execute(delegate(IDataReader dr) {
                while(dr.Read()) {
                    tag = Tags_Populate(dr);
                }
            });
            return tag;
        }

        public bool Tags_ValidateDefineTagMapping(TagBE tag) {
            if(tag == null) {
                throw new ArgumentNullException("tag");
            }
            if(tag.Type != TagType.DEFINE) {
                throw new ArgumentException("Tag has to be of type DEFINE");
            }
            return Catalog.NewQuery(@" /* Tags_ValidateDefineTagMapping */
DELETE FROM tag_map WHERE tagmap_tag_id = ?TAGID AND (SELECT COUNT(*) FROM pages WHERE page_id = tagmap_page_id) = 0;
SELECT COUNT(*) FROM tag_map WHERE tagmap_tag_id = ?TAGID;")
                    .With("TAGID", tag.Id).ReadAsInt() > 0;
        }

        public IList<TagBE> Tags_GetByQuery(string partialName, TagType type, DateTime from, DateTime to) {

            // retrieve the tags associated with a specified page id
            List<TagBE> tags = new List<TagBE>();
            bool hasWhere = false;
            StringBuilder query = new StringBuilder();
            query.Append(@" /* Tags_GetByQuery */
SELECT `tag_id`, `tag_name`, `tag_type` 
FROM tags ");
            if(!string.IsNullOrEmpty(partialName)) {
                query.AppendFormat("WHERE tag_name LIKE '{0}%' ", DataCommand.MakeSqlSafe(partialName));
                hasWhere = true;
            }
            if(type != TagType.ALL) {
                if(hasWhere)
                    query.Append("AND ");
                else
                    query.Append("WHERE ");

                query.AppendFormat(" tag_type={0} ", (int)type);
                hasWhere = true;
            }
            if((type == TagType.DATE) && (from != DateTime.MinValue)) {
                if(hasWhere)
                    query.Append("AND ");
                else
                    query.Append("WHERE ");

                query.AppendFormat("tag_name >= '{0}' ", from.ToString("yyyy-MM-dd"));
                hasWhere = true;
            }
            if((type == TagType.DATE) && (to != DateTime.MaxValue)) {
                if(hasWhere)
                    query.Append("AND ");
                else
                    query.Append("WHERE ");

                query.AppendFormat("tag_name <= '{0}' ", to.ToString("yyyy-MM-dd"));
            }

            Catalog.NewQuery(query.ToString())
            .Execute(delegate(IDataReader dr) {
                while(dr.Read()) {
                    TagBE t = Tags_Populate(dr);
                    tags.Add(t);
                }
            });
            return tags;
        }

        public IDictionary<uint, IEnumerable<ulong>> Tags_GetRelatedPageIds(IEnumerable<uint> tagids) {

            // retrieve a map of tag id to pages
            // each define tag maps to a list of related pages and each text tag maps to its defining page (if one exists) 
            var result = new Dictionary<uint, IEnumerable<ulong>>();
            if(tagids.Any()) {
                uint currentTagId = 0;
                var pageIds = new List<ulong>();
                var tagIdsText = tagids.ToCommaDelimitedString();
                Catalog.NewQuery(string.Format(@" /* Tags_GetRelatedPages */
SELECT requested_tag_id as tag_id, tagmap_page_id as page_id
  FROM tag_map
  JOIN
   (SELECT requestedtags.tag_id as requested_tag_id, tags.tag_id from tags 
    JOIN tags as requestedtags
        ON tags.tag_name = requestedtags.tag_name
    WHERE  (  ((tags.tag_type = 3 AND requestedtags.tag_type = 0) 
            OR (tags.tag_type = 0 AND requestedtags.tag_type = 3)) 
    AND	    requestedtags.tag_id IN ({0}))) relatedtags
ON tagmap_tag_id = tag_id
ORDER by tag_id;"
                    , tagIdsText))
                    .Execute(delegate(IDataReader dr) {
                    while(dr.Read()) {
                        var tagId = DbUtils.Convert.To<UInt32>(dr["tag_id"], 0);
                        var pageId = DbUtils.Convert.To<UInt32>(dr["page_id"], 0);
                        if(currentTagId != 0 && tagId != currentTagId) {
                            result[currentTagId] = pageIds;
                            pageIds = new List<ulong>();
                        }
                        currentTagId = tagId;
                        pageIds.Add(pageId);
                    }
                });
                if(pageIds.Any()) {
                    result[currentTagId] = pageIds;
                }
            }
            return result;
        }

        public uint Tags_Insert(TagBE tag) {
            try {
                return Catalog.NewQuery(@" /* Tags_Insert */
INSERT INTO tags (tag_name, tag_type) VALUES (?TAGNAME, ?TAGTYPE);
SELECT LAST_INSERT_ID();")
                    .With("TAGNAME", tag.Name)
                    .With("TAGTYPE", tag.Type)
                    .ReadAsUInt() ?? 0;
            } catch(MySqlException e) {
                if(e.Number == 1062) {
                    _log.DebugFormat("tag '{0}'({1}) already exists, returning 0",tag.Name,tag.Type);
                    return 0;
                }
                throw;
            }
        }

        public void Tags_Update(TagBE tag) {
            Catalog.NewQuery(@" /* Tags_Update */
UPDATE tags SET 
tag_name = ?TAGNAME,
tag_type = ?TAGTYPE
WHERE tag_id = ?TAGID;
")
            .With("TAGNAME", tag.Name)
            .With("TAGTYPE", tag.Type)
            .With("TAGID", tag.Id)
            .Execute();
            _log.DebugFormat("tag '{0}'({1}) updated", tag.Name, tag.Type);
        }

        public void TagMapping_Delete(ulong pageId, IList<uint> tagids) {

            // deletes the specified page to tag mappings and removes the tag if it is no longer used
            if(0 < tagids.Count) {
                string tagIdsText = tagids.ToCommaDelimitedString();
                Catalog.NewQuery(String.Format(@" /* Tags_Delete */
DELETE FROM tag_map
WHERE tagmap_page_id = ?PAGEID AND tagmap_tag_id in ({0});

DELETE FROM tags
USING tags
LEFT JOIN tag_map tm ON tags.tag_id = tm.tagmap_tag_id
WHERE tag_id in ({0}) AND tm.tagmap_id IS NULL;", tagIdsText))
                            .With("PAGEID", pageId)
                            .Execute();
            }
        }

        public void TagMapping_Insert(ulong pageId, uint tagId) {
            Catalog.NewQuery(@" /* TagMapping_Insert */
REPLACE INTO tag_map (tagmap_page_id, tagmap_tag_id) VALUES (?PAGEID, ?TAGID);")
                .With("PAGEID", pageId)
                .With("TAGID", tagId)
                .Execute();
        }

        private TagBE Tags_Populate(IDataReader dr) {
            TagBE tag = new TagBE();
            tag._Type = dr.Read<uint>("tag_type");
            tag.Id = dr.Read<uint>("tag_id");
            tag.Name = dr.Read<string>("tag_name");
            return tag;
        }
    }
}

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

using System.Collections.Generic;
using System.Data;

namespace MindTouch.Deki.Data.MySql {
    public partial class MySqlDekiDataSession {

        public IList<ResourceIdMapping> ResourceMapping_GetByFileIds(IList<uint> fileIds) {
            List<ResourceIdMapping> ret = new List<ResourceIdMapping>();
            if(fileIds.Count == 0) {
                return ret;
            }
            string query = @" /* ResourceMapping_GetByFileIds */
select resource_id, 'file' as entity_type, file_id as entity_id
from resourcefilemap
where file_id in ({0})
";
            Catalog.NewQuery(string.Format(query, fileIds.ToCommaDelimitedString()))
            .Execute(dr => ret.AddRange(ResourceMapping_Populate(dr)));
            return ret;
        }

        public IList<ResourceIdMapping> ResourceMapping_GetByResourceIds(IList<uint> resourceIds) {
            List<ResourceIdMapping> ret = new List<ResourceIdMapping>();
            if(resourceIds.Count == 0) {
                return ret;
            }
            string query = @" /* ResourceMapping_GetByResourceIds */
select resource_id, 'file' as entity_type, file_id as entity_id
from resourcefilemap
where resource_id in ({0})
";
            Catalog.NewQuery(string.Format(query, resourceIds.ToCommaDelimitedString()))
            .Execute(dr => ret.AddRange(ResourceMapping_Populate(dr)));
            return ret;
        }

        public ResourceIdMapping ResourceMapping_InsertFileMapping(uint? resourceId) {
            ResourceIdMapping ret = null;
            string query = @" /* ResourceMapping_InsertFileMapping */
insert into resourcefilemap (resource_id)
values (?RESOURCEID);

select resource_id, 'file' as entity_type, file_id as entity_id
from resourcefilemap
where file_id = LAST_INSERT_ID();
";
            Catalog.NewQuery(query)
            .With("RESOURCEID", resourceId)
            .Execute(delegate(IDataReader dr) {
                List<ResourceIdMapping> temp = ResourceMapping_Populate(dr);
                if(temp != null && temp.Count > 0) {
                    ret = temp[0];
                }
            });
            return ret;
        }

        public ResourceIdMapping ResourceMapping_UpdateFileMapping(uint fileId, uint? resourceId) {
            ResourceIdMapping ret = null;
            string query = @" /* ResourceMapping_UpdateFileMapping */
update resourcefilemap
set resource_id = ?RESOURCEID
where file_id = ?FILEID;

select resource_id, 'file' as entity_type, file_id as entity_id
from resourcefilemap
where file_id = ?FILEID;
";

            Catalog.NewQuery(query)
            .With("RESOURCEID", resourceId)
            .With("FILEID", fileId)
            .Execute(delegate(IDataReader dr) {
                List<ResourceIdMapping> temp = ResourceMapping_Populate(dr);
                if(temp != null && temp.Count > 0) {
                    ret = temp[0];
                }
            });
            return ret;


        }

        private List<ResourceIdMapping> ResourceMapping_Populate(IDataReader dr) {
            List<ResourceIdMapping> ret = new List<ResourceIdMapping>();

            while(dr.Read()) {
                uint? resId = DbUtils.Convert.To<uint>(dr[0]);
                string entityType = dr[1].ToString();
                uint? entityId = DbUtils.Convert.To<uint>(dr[2]);
                uint? mappingPageId = null;
                uint? mappingFileId = null;
                switch(entityType ?? string.Empty) {
                case "page":
                    mappingPageId = entityId;
                    break;
                case "file":
                    mappingFileId = entityId;
                    break;
                }
                ret.Add(new ResourceIdMapping(resId, mappingFileId, mappingPageId));
            }
            return ret;
        }
    }
}
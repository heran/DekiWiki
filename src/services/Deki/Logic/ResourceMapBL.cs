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

using MindTouch.Deki.Data;

namespace MindTouch.Deki.Logic {
    public class ResourceMapBL {
        public static Dictionary<uint, uint> GetResourceIdsByFileIds(uint[] fileids) {
            Dictionary<uint, uint> ret = new Dictionary<uint, uint>();
            IList<ResourceIdMapping> mappings = DbUtils.CurrentSession.ResourceMapping_GetByFileIds(fileids);
            foreach(ResourceIdMapping mapping in mappings) {
                if(mapping.FileId != null && mapping.ResourceId != null) {
                    ret[mapping.FileId ?? 0] = mapping.ResourceId ?? 0;
                }
            }

            return ret;
        }

        public static uint? GetResourceIdByFileId(uint fileid) {
            uint ret;
            if(GetResourceIdsByFileIds(new uint[] { fileid }).TryGetValue(fileid, out ret)) {
                return ret;
            } else {
                return null;
            }
        }

        public static Dictionary<uint, uint> GetFileIdByResourceIds(uint[] resourceids) {
            Dictionary<uint, uint> ret = new Dictionary<uint, uint>();
            IList<ResourceIdMapping> mappings = DbUtils.CurrentSession.ResourceMapping_GetByFileIds(resourceids);
            foreach(ResourceIdMapping mapping in mappings) {
                if(mapping.FileId != null && mapping.ResourceId != null) {
                    ret[mapping.FileId ?? 0] = mapping.ResourceId ?? 0;
                }
            }
            return ret;
        }

        public static uint? GetFileIdByResourceId(uint resourceid) {
            uint ret;
            if(GetFileIdByResourceIds(new uint[] { resourceid }).TryGetValue(resourceid, out ret)) {
                return ret;
            } else {
                return null;
            }
        }

        public static uint GetNewFileId() {
            return DbUtils.CurrentSession.ResourceMapping_InsertFileMapping(null).FileId.Value;
        }

        public static void UpdateFileIdMapping(uint fileid, uint resourceId) {
            DbUtils.CurrentSession.ResourceMapping_UpdateFileMapping(fileid, resourceId);
        }
    }
}

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
using MindTouch.Dream;

namespace MindTouch.Deki.Data.MySql {
    public partial class MySqlDekiDataSession {

        // -- Types --

        public enum VisibilityFilter {
            ANY = -1,
            VISIBLEONLY = 0,
            HIDDENONLY = 1
        }

        private class ResourceQuery {

            // -- Types --
            private class State {

                // -- Fields --
                public int resourceRevision;
                public DeletionFilter resourceDeletionFilter;
                public IList<uint> resourceIds;
                public IList<string> nameFilter;
                public IList<uint> parentIds;
                public ResourceBE.ParentType? parentType;
                public IList<ResourceBE.Type> resourceTypes;
                public IList<uint> changeSetId;
                public ResourceBE.ChangeOperations changeType;

                public bool populateResourceCountOnly;
                public bool populateContent;
                public bool populateRevisions;
                public ResourceOrderClause[] orderClauses;
                public uint? limit;
                public uint? offset;

                // -- Constructors --
                public State() {
                    resourceRevision = ResourceBE.HEADREVISION;
                    resourceDeletionFilter = DeletionFilter.ANY;

                    changeType = ResourceBE.ChangeOperations.UNDEFINED;
                    populateContent = true;
                }

                public State(State s) {
                    resourceRevision = s.resourceRevision;
                    resourceDeletionFilter = s.resourceDeletionFilter;
                    populateContent = s.populateContent;
                    populateRevisions = s.populateRevisions;
                    populateResourceCountOnly = s.populateResourceCountOnly;
                    parentType = s.parentType;
                    if(!ArrayUtil.IsNullOrEmpty(s.resourceIds)) {
                        resourceIds = new List<uint>(s.resourceIds);
                    }
                    if(!ArrayUtil.IsNullOrEmpty(s.parentIds)) {
                        parentIds = new List<uint>(s.parentIds);
                    }
                    if(!ArrayUtil.IsNullOrEmpty(s.resourceTypes)) {
                        resourceTypes = new List<ResourceBE.Type>(s.resourceTypes);
                    }
                    if(!ArrayUtil.IsNullOrEmpty(s.nameFilter)) {
                        nameFilter = new List<string>(s.nameFilter);
                    }
                    if(!ArrayUtil.IsNullOrEmpty(s.orderClauses)) {
                        orderClauses = (ResourceOrderClause[])s.orderClauses.Clone();
                    }
                    if(!ArrayUtil.IsNullOrEmpty(s.changeSetId)) {
                        changeSetId = new List<uint>(s.changeSetId).ToArray();
                    }
                    changeType = s.changeType;
                    limit = s.limit;
                    offset = s.offset;
                }
            }

            public struct ResourceOrderClause {
                public enum SortTable {
                    UNDEFINED,
                    RESOURCES,
                    RESOURCEREVISIONS
                }

                // -- Fields
                public SortDirection dir;
                public string column;
                public SortTable table;
            }

            // -- Fields --
            private readonly State _state;

            // -- Constructors --
            public ResourceQuery() {
                _state = new State();
            }

            private ResourceQuery(State s) {
                _state = new State(s);
            }

            // -- Methods --
            #region Filter columns
            public ResourceQuery WithResourceId(uint resourceId) {
                return WithResourceId(new[] { resourceId });
            }

            public ResourceQuery WithResourceId(uint[] resourceIds) {
                State s = new State(_state);
                s.resourceIds = resourceIds;
                return new ResourceQuery(s);
            }
            public ResourceQuery WithChangeSetIds(uint changeSetId) {
                return WithChangeSetIds(new[] { changeSetId });
            }

            public ResourceQuery WithChangeSetIds(uint[] changeSetIds) {
                State s = new State(_state);
                s.changeSetId = changeSetIds;
                return new ResourceQuery(s);
            }

            public ResourceQuery WithResourceType(ResourceBE.Type resourceType) {
                return WithResourceType(new[] { resourceType });
            }

            public ResourceQuery WithResourceType(IList<ResourceBE.Type> resourceTypes) {
                State s = new State(_state);
                s.resourceTypes = resourceTypes;
                return new ResourceQuery(s);
            }

            public ResourceQuery WithChangeType(ResourceBE.ChangeOperations changeType) {
                State s = new State(_state);
                s.changeType = changeType;
                return new ResourceQuery(s);
            }

            public ResourceQuery WithRevision(int revision) {
                State s = new State(_state);
                s.resourceRevision = revision;
                s.populateRevisions = revision != ResourceBE.HEADREVISION;
                return new ResourceQuery(s);
            }

            public ResourceQuery WithParent(IList<uint> parentIds, ResourceBE.ParentType? parentType) {
                State s = new State(_state);
                s.parentIds = parentIds;
                s.parentType = parentType;
                return new ResourceQuery(s);
            }

            public ResourceQuery WithDeletionFilter(DeletionFilter deletionFilter) {
                State s = new State(_state);
                s.resourceDeletionFilter = deletionFilter;
                return new ResourceQuery(s);
            }

            public ResourceQuery WithNames(IList<string> names) {
                State s = new State(_state);
                s.nameFilter = names;
                return new ResourceQuery(s);
            }

            #endregion

            public ResourceQuery IncludeRevisions(bool populateRevisions) {
                State s = new State(_state);
                s.populateRevisions = populateRevisions;
                return new ResourceQuery(s);
            }

            protected ResourceQuery IncludeResourceCountOnly(bool countResourcesOnly) {
                State s = new State(_state);
                s.populateResourceCountOnly = countResourcesOnly;
                if(countResourcesOnly) {
                    s.populateContent = false;
                }

                return new ResourceQuery(s);
            }

            public ResourceQuery OrderBy(ResourceOrderClause.SortTable table, string column, SortDirection direction) {
                State s = new State(_state);
                ResourceOrderClause roc = new ResourceOrderClause();
                roc.table = table;
                roc.column = column;
                roc.dir = direction;

                if(s.orderClauses == null) {
                    s.orderClauses = new ResourceOrderClause[] { roc };
                } else {
                    List<ResourceOrderClause> temp = new List<ResourceOrderClause>(s.orderClauses);
                    temp.Add(roc);
                    s.orderClauses = temp.ToArray();
                }
                return new ResourceQuery(s);
            }

            public ResourceQuery Limit(uint? limit) {
                State s = new State(_state);
                s.limit = limit;
                return new ResourceQuery(s);
            }

            public ResourceQuery Offset(uint? offset) {
                State s = new State(_state);
                s.offset = offset;
                return new ResourceQuery(s);
            }

            #region Query building and execution
            public override string ToString() {
                return ToString(string.Empty);
            }

            public string ToString(string methodName) {
                StringBuilder querySb = new StringBuilder();

                const string RESOURCE_ALIAS = "r";
                const string RREVISION_ALIAS = "rr";
                const string RESOURCECONTENTS_ALIAS = "rc";
                string queryColumnAlias = _state.populateRevisions ? RREVISION_ALIAS : RESOURCE_ALIAS;

                //Query Header
                if(!string.IsNullOrEmpty(methodName)) {
                    querySb.AppendFormat(" /* Resources::{0} */", methodName);
                }

                //Columns
                if(!_state.populateResourceCountOnly) {
                    querySb.AppendFormat("\n SELECT {0}", GetColumns(queryColumnAlias));

                    if(_state.populateContent) {
                        querySb.AppendFormat(", {0}.*", RESOURCECONTENTS_ALIAS);
                    }

                } else {
                    querySb.AppendFormat("\n SELECT count(*)");
                }

                //Main table
                querySb.AppendFormat("\n FROM resources {0}", RESOURCE_ALIAS);

                //Joins                
                if(_state.populateRevisions) {
                    querySb.AppendFormat("\n JOIN resourcerevs {0}\n\t ON {1}.res_id = {0}.resrev_res_id", RREVISION_ALIAS, RESOURCE_ALIAS);
                }

                if(_state.populateContent) {
                    querySb.AppendFormat("\n LEFT JOIN resourcecontents {0}\n\t ON {1}.resrev_content_id = {0}.rescontent_id", RESOURCECONTENTS_ALIAS, queryColumnAlias);
                }

                //Where clauses
                querySb.AppendFormat("\n WHERE 1=1");

                if(!ArrayUtil.IsNullOrEmpty(_state.resourceIds)) {
                    string resourceIdsStr = _state.resourceIds.ToCommaDelimitedString();
                    querySb.AppendFormat("\n AND {0}.res_id in ({1})", RESOURCE_ALIAS, resourceIdsStr);
                }

                if(!ArrayUtil.IsNullOrEmpty(_state.changeSetId)) {
                    string transactionIdsStr = _state.changeSetId.ToCommaDelimitedString();
                    querySb.AppendFormat("\n AND {0}.resrev_changeset_id in ({1})", RESOURCE_ALIAS, transactionIdsStr);
                }

                if(_state.resourceDeletionFilter != DeletionFilter.ANY) {
                    querySb.AppendFormat("\n AND {0}.res_deleted = {1}", RESOURCE_ALIAS, (int)_state.resourceDeletionFilter);
                }

                if(!ArrayUtil.IsNullOrEmpty(_state.resourceTypes)) {
                    var resourceTypesStr = _state.resourceTypes.Select(x => (int)x).ToCommaDelimitedString();
                    querySb.AppendFormat("\n AND {0}.res_type in ({1})", RESOURCE_ALIAS, resourceTypesStr);
                }

                if(_state.changeType != ResourceBE.ChangeOperations.UNDEFINED) {
                    querySb.Append("\n AND ( ");
                    bool firstChange = true;
                    foreach(ResourceBE.ChangeOperations c in Enum.GetValues(typeof(ResourceBE.ChangeOperations))) {
                        if((_state.changeType & c) == c && c != ResourceBE.ChangeOperations.UNDEFINED) {
                            if(!firstChange) {
                                querySb.Append(" OR ");
                            }
                            querySb.AppendFormat("{0}.resrev_change_mask & {1} = {1}", queryColumnAlias, (ushort)c);
                            firstChange = false;
                        }
                    }
                    querySb.Append(")");
                }

                if(_state.resourceRevision != ResourceBE.HEADREVISION) {
                    if(_state.resourceRevision > ResourceBE.HEADREVISION) {
                        querySb.AppendFormat("\n AND {0}.resrev_rev = {1}", RREVISION_ALIAS, _state.resourceRevision);
                    } else if(_state.resourceRevision < ResourceBE.HEADREVISION) {
                        querySb.AppendFormat("\n AND {0}.resrev_rev = {1}.res_headrev - {2}", RREVISION_ALIAS, RESOURCE_ALIAS, Math.Abs(_state.resourceRevision));
                    }
                }

                if(_state.parentType.HasValue || !ArrayUtil.IsNullOrEmpty(_state.parentIds)) {
                    string parentIdsStr = (ArrayUtil.IsNullOrEmpty(_state.parentIds) ? "is not null" : "in (" + _state.parentIds.ToCommaDelimitedString() + ")");
                    switch(_state.parentType) {
                    case ResourceBE.ParentType.PAGE:
                        querySb.AppendFormat("\n AND {0}.resrev_parent_page_id {1}", RESOURCE_ALIAS, parentIdsStr);
                        break;
                    case ResourceBE.ParentType.USER:
                        querySb.AppendFormat("\n AND {0}.resrev_parent_user_id {1}", RESOURCE_ALIAS, parentIdsStr);
                        break;
                    case ResourceBE.ParentType.SITE:

                        //SITE resources always have a parent_id=null, parent_user_id=null, parent_page_id=null
                        querySb.AppendFormat("\n AND {0}.resrev_parent_id is null", RESOURCE_ALIAS);
                        querySb.AppendFormat("\n AND {0}.resrev_parent_page_id is null", RESOURCE_ALIAS);
                        querySb.AppendFormat("\n AND {0}.resrev_parent_user_id is null", RESOURCE_ALIAS);
                        break;
                    default:
                        querySb.AppendFormat("\n AND {0}.resrev_parent_id {1}", RESOURCE_ALIAS, parentIdsStr);
                        break;

                    }
                }

                if(!ArrayUtil.IsNullOrEmpty(_state.nameFilter)) {
                    querySb.Append("\n " + BuildNameFilterWhereClause(_state.nameFilter, RESOURCE_ALIAS));
                }

                //Sort
                if(!ArrayUtil.IsNullOrEmpty(_state.orderClauses)) {
                    StringBuilder orderList = new StringBuilder();
                    for(int i = 0; i < _state.orderClauses.Length; i++) {
                        ResourceOrderClause roc = _state.orderClauses[i];
                        if(i > 0) {
                            orderList.Append(",");
                        }
                        string tableAlias = string.Empty;
                        switch(roc.table) {
                        case ResourceOrderClause.SortTable.RESOURCES:
                            tableAlias = RESOURCE_ALIAS + ".";
                            break;
                        case ResourceOrderClause.SortTable.RESOURCEREVISIONS:
                            tableAlias = RREVISION_ALIAS + ".";
                            break;
                        }
                        orderList.AppendFormat("{0}{1} {2}", tableAlias, roc.column, roc.dir.ToString());
                    }

                    querySb.AppendFormat("\n ORDER BY {0}", orderList);
                }

                //Limit+offset
                uint? limit = _state.limit;
                if(limit == null && _state.offset != null) {
                    limit = uint.MaxValue;
                }

                if(limit != null) {
                    querySb.AppendFormat("\n LIMIT {0}", limit);
                }
                if(_state.offset != null) {
                    querySb.AppendFormat("\n OFFSET {0}", _state.offset.Value);
                }
                return querySb.ToString();
            }

            private string GetColumns(string revisionTablePrefix) {
                return string.Format(
    @"res_id,res_headrev,res_type,res_deleted,res_create_timestamp,res_update_timestamp,res_create_user_id,res_update_user_id,
{0}resrev_rev,{0}resrev_user_id,{0}resrev_parent_id,{0}resrev_parent_page_id,{0}resrev_parent_page_id,{0}resrev_parent_user_id,{0}resrev_change_mask,{0}resrev_name,{0}resrev_change_description,{0}resrev_timestamp,{0}resrev_content_id,{0}resrev_deleted,{0}resrev_changeset_id,{0}resrev_size,{0}resrev_mimetype,{0}resrev_language,{0}resrev_is_hidden,{0}resrev_meta",
                    string.IsNullOrEmpty(revisionTablePrefix) ? string.Empty : revisionTablePrefix + ".");
            }


            private static string BuildNameFilterWhereClause(IList<string> names, string tableAlias) {
                /*
                 AND (
                     name in (1,2,3)
                     OR name like %4%
                     OR name like %5%
                 )
                */

                StringBuilder nameSpecificQuery = new StringBuilder();
                StringBuilder nameSubstringQuery = new StringBuilder();
                if(!ArrayUtil.IsNullOrEmpty(names)) {

                    for(int i = 0; i < names.Count; i++) {
                        if(!names[i].Contains('*')) {

                            if(nameSpecificQuery.Length == 0) {
                                nameSpecificQuery.AppendFormat("{0}.resrev_name in (", tableAlias);
                            } else {
                                nameSpecificQuery.Append(i > 0 ? "," : string.Empty);
                            }

                            nameSpecificQuery.AppendFormat("'{0}'", DataCommand.MakeSqlSafe(names[i]));
                        } else {

                            //Change the name into a mysql substring expression
                            string nameExpression = DataCommand.MakeSqlSafe(names[i]);
                            if(nameExpression.StartsWith("*", StringComparison.InvariantCultureIgnoreCase)) {
                                nameExpression = nameExpression.TrimStart('*').Insert(0, "%");
                            }
                            if(nameExpression.EndsWith("*", StringComparison.InvariantCultureIgnoreCase)) {
                                nameExpression = nameExpression.TrimEnd('*') + '%';
                            }

                            if(nameSubstringQuery.Length > 0) {
                                nameSubstringQuery.Append(" OR ");
                            }

                            nameSubstringQuery.AppendFormat(" {0}.resrev_name like \"{1}\"", tableAlias, nameExpression);

                        }
                    }
                    if(nameSpecificQuery.Length > 0) {
                        nameSpecificQuery.Append(")");
                    }
                }

                string ret = string.Empty;
                if(nameSpecificQuery.Length > 0 || nameSubstringQuery.Length > 0) {
                    ret = string.Format(" AND ({0} {1} {2})", nameSpecificQuery, (nameSpecificQuery.Length > 0 && nameSubstringQuery.Length > 0) ? "OR" : string.Empty, nameSubstringQuery);
                }
                return ret;
            }

            public List<ResourceBE> SelectList(DataCatalog dbCatalog, string methodName) {
                List<ResourceBE> ret = new List<ResourceBE>();
                string q = ToString(methodName);
                dbCatalog.NewQuery(q)
                .Execute(delegate(IDataReader dr) {
                    while(dr.Read()) {
                        ret.Add(Resources_Populate(dr));
                    }
                });

                return ret;
            }

            public ResourceBE Select(DataCatalog dbCatalog, string methodName) {
                List<ResourceBE> ret = SelectList(dbCatalog, methodName);
                return ArrayUtil.IsNullOrEmpty(ret) ? null : ret[0];
            }

            public uint? SelectCount(DataCatalog dbCatalog, string methodName) {
                ResourceQuery countRQ = IncludeResourceCountOnly(true);
                string q = countRQ.ToString(methodName + "(count only)");
                return dbCatalog.NewQuery(q).ReadAsUInt();
            }

            #endregion
        }

        // -- Methods --
        #region SELECT Resources

        public IList<ResourceBE> Resources_GetByIds(params uint[] resourceIds) {
            return new ResourceQuery()
                .WithResourceId(resourceIds)
                .SelectList(Catalog, "Resources_GetByIds");
        }

        public ResourceBE Resources_GetByIdAndRevision(uint resourceid, int revision) {
            return new ResourceQuery()
                .WithResourceId(resourceid)
                .WithRevision(revision)
                .Select(Catalog, string.Format("Resources_GetByIdAndRevision (rev: {0})", revision));
        }

        public IList<ResourceBE> Resources_GetByQuery(IList<uint> parentIds, ResourceBE.ParentType? parentType, IList<ResourceBE.Type> resourceTypes, IList<string> names, DeletionFilter deletionStateFilter, bool? populateRevisions, uint? offset, uint? limit) {
            ResourceQuery rq = new ResourceQuery();
            rq = rq.WithParent(parentIds, parentType);
            rq = rq.WithResourceType(resourceTypes);
            rq = rq.WithNames(names);
            rq = rq.WithDeletionFilter(deletionStateFilter);
            rq = rq.IncludeRevisions(populateRevisions ?? false);
            rq = rq.Limit(limit);
            rq = rq.Offset(offset);
            rq = rq.OrderBy(ResourceQuery.ResourceOrderClause.SortTable.RESOURCES, "res_id", SortDirection.ASC);
            return rq.SelectList(Catalog, "Resources_Get");
        }

        public IList<ResourceBE> Resources_GetRevisions(uint resourceId, ResourceBE.ChangeOperations changeTypesFilter, SortDirection sortRevisions, uint? limit) {
            return new ResourceQuery()
            .WithResourceId(resourceId)
            .WithChangeType(changeTypesFilter)
            .IncludeRevisions(true)
            .OrderBy(ResourceQuery.ResourceOrderClause.SortTable.RESOURCEREVISIONS, "resrev_rev", sortRevisions)
            .Limit(limit)
            .SelectList(Catalog, "Resources_GetRevisions");
        }

        public IList<ResourceBE> Resources_GetByChangeSet(uint changeSetId, ResourceBE.Type resourceType) {
            if(changeSetId == 0) {
                return new List<ResourceBE>();
            }

            return new ResourceQuery()
            .WithChangeSetIds(changeSetId)
            .WithResourceType(resourceType)
            .SelectList(Catalog, "Resources_GetByChangeSet");
        }

        public Dictionary<Title, ResourceBE> Resources_GetFileResourcesByTitlesWithMangling(IList<Title> fileTitles) {
            //A specialized query that retrieves file resources from titles. This is only used from DekiXmlParser.ConvertFileLinks

            Dictionary<Title, ResourceBE> ret = new Dictionary<Title, ResourceBE>();

            if(ArrayUtil.IsNullOrEmpty(fileTitles)) {
                return ret;
            }

            string query = @" /* Resources_GetFileResourcesByTitlesWithMangling */
select resources.*, resourcecontents.*, pages.page_id, pages.page_namespace, pages.page_title, pages.page_display_name
from pages
join resources
	on resrev_parent_page_id = page_id
left join resourcecontents
    on resources.resrev_content_id = resourcecontents.rescontent_id
where
        res_type = 2
    AND res_deleted = 0
AND(    
        {0}
)
    ";

            StringBuilder whereQuery = new StringBuilder();
            for(int i = 0; i < fileTitles.Count; i++) {
                if(i > 0) {
                    whereQuery.Append("\n OR ");
                }
                whereQuery.AppendFormat("(pages.page_namespace={0} AND pages.page_title='{1}' AND REPLACE(resources.resrev_name, ' ', '_') = REPLACE('{2}', ' ', '_'))",
                                    (uint)fileTitles[i].Namespace, DataCommand.MakeSqlSafe(fileTitles[i].AsUnprefixedDbPath()), DataCommand.MakeSqlSafe(fileTitles[i].Filename));
            }

            query = string.Format(query, whereQuery);

            Catalog.NewQuery(query).Execute(delegate(IDataReader dr) {
                while(dr.Read()) {
                    Title title = DbUtils.TitleFromDataReader(dr, "page_namespace", "page_title", "page_display_name", "resrev_name");
                    ResourceBE r = Resources_Populate(dr) as ResourceBE;
                    if(r != null) {
                        ret[title] = r;
                    }
                }
            });

            return ret;
        }
        #endregion

        #region Count Resources
        public uint Resources_GetRevisionCount(uint resourceId, ResourceBE.ChangeOperations changeTypesFilter) {
            return new ResourceQuery()
            .WithResourceId(resourceId)
            .WithChangeType(changeTypesFilter)
            .IncludeRevisions(true)
            .SelectCount(Catalog, "Resources_GetRevisionCount") ?? 0;
        }

        #endregion

        public ResourceBE Resources_SaveRevision(ResourceBE resource) {
            string query = string.Empty;
            bool contentUpdated = false;

            if(resource.Content != null && resource.Content.IsNewContent()) {
                contentUpdated = true;
                ResourceContentBE content = Resources_ContentInsert(resource.Content);
                resource.Content = content;
                resource.ContentId = content.ContentId;
            }

            if(resource.IsNewResource()) {
                query = @" /* Resources_SaveRevision (new resource) */
set @resourceid = 0;

insert into resources set
res_headrev =               ?RES_HEADREV, 
res_type =                  ?RES_TYPE, 
res_deleted =               ?RES_DELETED, 
res_create_timestamp =      ?RES_CREATE_TIMESTAMP, 
res_update_timestamp =      ?RES_UPDATE_TIMESTAMP, 
res_create_user_id =        ?RES_CREATE_USER_ID, 
res_update_user_id =        ?RES_UPDATE_USER_ID, 
resrev_rev =                ?RESREV_REV, 
resrev_user_id =            ?RESREV_USER_ID, 
resrev_parent_id =          ?RESREV_PARENT_ID, 
resrev_parent_page_id =     ?RESREV_PARENT_PAGE_ID, 
resrev_parent_user_id =     ?RESREV_PARENT_USER_ID, 
resrev_change_mask =        ?RESREV_CHANGE_MASK, 
resrev_name =               ?RESREV_NAME, 
resrev_change_description = ?RESREV_CHANGE_DESCRIPTION, 
resrev_timestamp =          ?RESREV_TIMESTAMP, 
resrev_content_id =         ?RESREV_CONTENT_ID, 
resrev_deleted =            ?RESREV_DELETED, 
resrev_changeset_id =       ?RESREV_CHANGESET_ID, 
resrev_size =               ?RESREV_SIZE, 
resrev_mimetype =           ?RESREV_MIMETYPE, 
resrev_language =           ?RESREV_LANGUAGE,
resrev_is_hidden =          ?RESREV_IS_HIDDEN, 
resrev_meta =               ?RESREV_META;

select last_insert_id() into @resourceid;

insert into resourcerevs set 
resrev_res_id =             @resourceid,
resrev_rev =                ?RESREV_REV,
resrev_user_id =            ?RESREV_USER_ID,
resrev_parent_id =          ?RESREV_PARENT_ID,
resrev_parent_page_id =     ?RESREV_PARENT_PAGE_ID,
resrev_parent_user_id =     ?RESREV_PARENT_USER_ID,
resrev_change_mask =        ?RESREV_CHANGE_MASK,
resrev_name =               ?RESREV_NAME,
resrev_change_description = ?RESREV_CHANGE_DESCRIPTION,
resrev_timestamp =          ?RESREV_TIMESTAMP,
resrev_content_id =         ?RESREV_CONTENT_ID,
resrev_deleted =            ?RESREV_DELETED,
resrev_changeset_id =       ?RESREV_CHANGESET_ID,
resrev_size =               ?RESREV_SIZE,
resrev_mimetype =           ?RESREV_MIMETYPE,
resrev_language =           ?RESREV_LANGUAGE,
resrev_is_hidden =          ?RESREV_IS_HIDDEN,
resrev_meta =               ?RESREV_META;

update resourcecontents set 
rescontent_res_id =     @resourceid,
rescontent_res_rev =    ?RESREV_REV
where rescontent_id =   ?RESREV_CONTENT_ID;

select *
from resources
left join resourcecontents
    on resources.resrev_content_id = resourcecontents.rescontent_id
where res_id = @resourceid;

/* End of ResourceDA::InsertResourceRevision (new resource) */
";

            } else {

                resource.Revision = ++resource.ResourceHeadRevision;
                if(contentUpdated) {
                    resource.Content.Revision = (uint)resource.Revision;
                }

                query = @" /* Resources_SaveRevision + concurrency check (new revision) */
update resources set 
res_headrev =               ?RES_HEADREV,
res_type =                  ?RES_TYPE,
res_deleted =               ?RES_DELETED,
res_update_timestamp =      ?RESREV_TIMESTAMP,
res_update_user_id =        ?RES_UPDATE_USER_ID,
resrev_rev =                ?RESREV_REV,
resrev_user_id =            ?RESREV_USER_ID,
resrev_parent_id =          ?RESREV_PARENT_ID,
resrev_parent_page_id =     ?RESREV_PARENT_PAGE_ID,
resrev_parent_user_id =     ?RESREV_PARENT_USER_ID,
resrev_change_mask =        ?RESREV_CHANGE_MASK,
resrev_name =               ?RESREV_NAME,
resrev_change_description = ?RESREV_CHANGE_DESCRIPTION,
resrev_timestamp =          ?RESREV_TIMESTAMP,
resrev_content_id =         ?RESREV_CONTENT_ID,
resrev_deleted =            ?RESREV_DELETED,
resrev_changeset_id =       ?RESREV_CHANGESET_ID,
resrev_size =               ?RESREV_SIZE,
resrev_mimetype =           ?RESREV_MIMETYPE,
resrev_language =           ?RESREV_LANGUAGE,
resrev_is_hidden =          ?RESREV_IS_HIDDEN,
resrev_meta =               ?RESREV_META
WHERE res_id =              ?RES_ID
AND res_headrev =           ?RES_HEADREV - 1
AND res_update_timestamp =  ?RES_UPDATE_TIMESTAMP;

select ROW_COUNT() into @affectedRows;

select *
from resources
where @affectedrows > 0
and res_id = ?RES_ID;
";
                if(ArrayUtil.IsNullOrEmpty(Resources_ExecuteInsertUpdateQuery(resource, query))) {

                    //Cleanup content row if resource could not be updated
                    Resources_ContentDelete(resource.ContentId);
                    throw new ResourceConcurrencyException(resource.ResourceId);
                }
                query = @" /* Resources_SaveRevision (new revision) */
replace into resourcerevs set 
resrev_res_id =             ?RES_ID,
resrev_rev =                ?RESREV_REV,
resrev_user_id =            ?RESREV_USER_ID, 
resrev_parent_id =          ?RESREV_PARENT_ID,
resrev_parent_page_id =     ?RESREV_PARENT_PAGE_ID,
resrev_parent_user_id =     ?RESREV_PARENT_USER_ID,
resrev_change_mask =        ?RESREV_CHANGE_MASK,
resrev_name =               ?RESREV_NAME,
resrev_change_description = ?RESREV_CHANGE_DESCRIPTION,
resrev_timestamp =          ?RESREV_TIMESTAMP,
resrev_content_id =         ?RESREV_CONTENT_ID,
resrev_deleted =            ?RESREV_DELETED,
resrev_changeset_id =       ?RESREV_CHANGESET_ID,
resrev_size =               ?RESREV_SIZE,
resrev_mimetype =           ?RESREV_MIMETYPE,
resrev_language =           ?RESREV_LANGUAGE,
resrev_is_hidden =          ?RESREV_IS_HIDDEN,
resrev_meta =               ?RESREV_META;

update resourcecontents set 
rescontent_res_id =     ?RES_ID,
rescontent_res_rev =    ?RESCONTENT_RES_REV
where rescontent_id =   ?RESREV_CONTENT_ID;

select *
from resources
left join resourcecontents
    on resources.resrev_content_id = resourcecontents.rescontent_id
where res_id = ?RES_ID;
";
            }

            ResourceBE[] ret = Resources_ExecuteInsertUpdateQuery(resource, query);
            return (ArrayUtil.IsNullOrEmpty(ret)) ? null : ret[0];
        }

        public ResourceBE Resources_UpdateRevision(ResourceBE resource) {

            //Prepare query for retrieving the resource after updating
            string selectQuery = new ResourceQuery()
                                .WithResourceId(resource.ResourceId)
                                .WithRevision(resource.Revision)
                                .ToString();

            //Note: The resrev_timestamp is not updated in order to preserve the initial timeline of revisions
            string query = string.Format(@" /* Resources_UpdateRevision */
update resourcerevs set 
resrev_rev              = ?RESREV_REV,
resrev_user_id          = ?RESREV_USER_ID,
resrev_parent_id        = ?RESREV_PARENT_ID,
resrev_parent_page_id   = ?RESREV_PARENT_PAGE_ID,
resrev_parent_user_id   = ?RESREV_PARENT_USER_ID,
resrev_change_mask      = ?RESREV_CHANGE_MASK,
resrev_name             = ?RESREV_NAME,
resrev_change_description=?RESREV_CHANGE_DESCRIPTION,
resrev_content_id       = ?RESREV_CONTENT_ID,
resrev_deleted          = ?RESREV_DELETED,
resrev_changeset_id     = ?RESREV_CHANGESET_ID,
resrev_size             = ?RESREV_SIZE,
resrev_mimetype         = ?RESREV_MIMETYPE,
resrev_language         = ?RESREV_LANGUAGE,
resrev_is_hidden        = ?RESREV_IS_HIDDEN,
resrev_meta             = ?RESREV_META
WHERE 
resrev_res_id       = ?RES_ID
AND resrev_rev          = ?RESREV_REV;

{0}
", selectQuery);

            ResourceBE[] ret = Resources_ExecuteInsertUpdateQuery(resource, query);

            if(resource.IsHeadRevision()) {
                Resources_ResetHeadRevision(resource.ResourceId);
            }

            return (ArrayUtil.IsNullOrEmpty(ret)) ? null : ret[0];
        }

        public void Resources_Delete(IList<uint> resourceIds) {
            //TODO MaxM: Remove content as well

            if(resourceIds.Count == 0) {
                return;
            }
            string resourceIdsText = resourceIds.ToCommaDelimitedString();

            Catalog.NewQuery(string.Format(@" /* Resources_Delete */
delete from resources where res_id in ({0});
delete from resourcerevs where resrev_res_id in ({0});", resourceIdsText))
            .Execute();
        }

        public void Resources_DeleteRevision(uint resourceId, int revision) {

            //Wiping a revision is only done internally in the case where content doesn't get saved properly.
            ResourceBE rev = Head.Resources_GetByIdAndRevision(resourceId, revision);
            if(rev == null)
                return;

            //Only one revision exists: Wipe it.
            if(revision == ResourceBE.TAILREVISION && rev.ResourceHeadRevision == revision) {
                Head.Resources_Delete(new List<uint>() { resourceId });
                return;
            }

            //More than one revision exists: Delete the revision

            Catalog.NewQuery(@" /* Resources_DeleteRevision */
delete from resourcerevs 
where resrev_res_id = ?RES_ID
and resrev_rev = ?RESREV_REV;")
            .With("RES_ID", resourceId)
            .With("RESREV_REV", revision)
            .Execute();

            //Head revision was deleted: update head revision to new head.
            if(rev.ResourceHeadRevision == revision) {
                Resources_ResetHeadRevision(resourceId);
            }
        }

        private void Resources_ResetHeadRevision(uint resourceId) {
            //Update the HEAD revision in resources to latest revision in resourcerevs

            Catalog.NewQuery(@" /* Resources_ResetHeadRevision */
update resources f
join resourcerevs fr
join (
	select max(resrev_rev) as resrev_rev
	from resourcerevs
	where resrev_res_id = ?RES_ID
	) maxrev
on fr.resrev_res_id = ?RES_ID
and fr.resrev_rev = maxrev.resrev_rev
set 
f.res_headrev                =fr.resrev_rev,
f.res_update_timestamp       =fr.resrev_timestamp,
f.res_update_user_id         =fr.resrev_user_id,
f.resrev_rev                 =fr.resrev_rev,
f.resrev_user_id             =fr.resrev_user_id,
f.resrev_parent_id           =fr.resrev_parent_id,
f.resrev_parent_page_id      =fr.resrev_parent_page_id,
f.resrev_parent_user_id      =fr.resrev_parent_user_id,
f.resrev_change_mask         =fr.resrev_change_mask,
f.resrev_name                =fr.resrev_name,
f.resrev_change_description  =fr.resrev_change_description,
f.resrev_timestamp           =fr.resrev_timestamp,
f.resrev_content_id          =fr.resrev_content_id,
f.resrev_deleted             =fr.resrev_deleted,
f.resrev_changeset_id        =fr.resrev_changeset_id,
f.resrev_size                =fr.resrev_size,
f.resrev_mimetype            =fr.resrev_mimetype,
f.resrev_language            =fr.resrev_language,
f.resrev_is_hidden           =fr.resrev_is_hidden,
f.resrev_meta          	     =fr.resrev_meta
where 
fr.resrev_res_id = f.res_id;")
            .With("RES_ID", resourceId)
            .Execute();
        }

        private static ResourceBE Resources_Populate(IDataReader dr) {
            ResourceBE.Type resourceType = (ResourceBE.Type)DbUtils.Convert.To<byte>(dr["res_type"]);
            ResourceBE res = new ResourceBE(resourceType);

            MimeType mimeTypeTmp = null;
            for(int i = 0; i < dr.FieldCount; i++) {

                #region ResourceBE and ResourceContentBE entity mapping
                switch(dr.GetName(i).ToLowerInvariant()) {
                case "res_id":
                    res.ResourceId = DbUtils.Convert.To<uint>(dr.GetValue(i)) ?? 0;
                    break;
                case "resrev_res_id":
                    res.ResourceId = DbUtils.Convert.To<uint>(dr.GetValue(i)) ?? 0;
                    break;
                case "resrev_rev":
                    res.Revision = DbUtils.Convert.To<int>(dr.GetValue(i)) ?? 0;
                    break;
                case "resrev_user_id":
                    res.UserId = DbUtils.Convert.To<uint>(dr.GetValue(i)) ?? 0;
                    break;
                case "resrev_change_mask":
                    res.ChangeMask = (ResourceBE.ChangeOperations)(DbUtils.Convert.To<ushort>(dr.GetValue(i)) ?? (ushort)ResourceBE.ChangeOperations.UNDEFINED);
                    break;
                case "resrev_name":
                    res.Name = dr.GetValue(i) as string;
                    break;
                case "resrev_change_description":
                    res.ChangeDescription = dr.GetValue(i) as string;
                    break;
                case "resrev_timestamp":
                    res.Timestamp = new DateTime(dr.GetDateTime(i).Ticks, DateTimeKind.Utc);
                    break;
                case "resrev_content_id":
                    res.ContentId = DbUtils.Convert.To<uint>(dr.GetValue(i), 0);
                    break;
                case "resrev_deleted":
                    res.Deleted = DbUtils.Convert.To<bool>(dr.GetValue(i), false);
                    break;
                case "resrev_size":
                    res.Size = DbUtils.Convert.To<uint>(dr.GetValue(i)) ?? 0;
                    break;
                case "resrev_mimetype":
                    MimeType.TryParse(dr.GetValue(i) as string, out mimeTypeTmp);
                    res.MimeType = mimeTypeTmp;
                    break;
                case "resrev_changeset_id":
                    res.ChangeSetId = DbUtils.Convert.To<uint>(dr.GetValue(i)) ?? 0;
                    break;
                case "resrev_parent_id":
                    res.ParentId = DbUtils.Convert.To<uint>(dr.GetValue(i));
                    break;
                case "resrev_parent_page_id":
                    res.ParentPageId = DbUtils.Convert.To<uint>(dr.GetValue(i));
                    break;
                case "resrev_parent_user_id":
                    res.ParentUserId = DbUtils.Convert.To<uint>(dr.GetValue(i));
                    break;
                case "res_headrev":
                    res.ResourceHeadRevision = DbUtils.Convert.To<int>(dr.GetValue(i)) ?? 0;
                    break;
                case "res_type":

                    // this column was already read
                    break;
                case "res_deleted":
                    res.ResourceIsDeleted = DbUtils.Convert.To<bool>(dr.GetValue(i), false);
                    break;
                case "res_create_timestamp":
                    res.ResourceCreateTimestamp = new DateTime(dr.GetDateTime(i).Ticks, DateTimeKind.Utc);
                    break;
                case "res_update_timestamp":
                    res.ResourceUpdateTimestamp = new DateTime(dr.GetDateTime(i).Ticks, DateTimeKind.Utc);
                    break;
                case "res_create_user_id":
                    res.ResourceCreateUserId = DbUtils.Convert.To<uint>(dr.GetValue(i)) ?? 0;
                    break;
                case "res_update_user_id":
                    res.ResourceUpdateUserId = DbUtils.Convert.To<uint>(dr.GetValue(i)) ?? 0;
                    break;
                case "resrev_language":
                    res.Language = dr.GetValue(i) as string;
                    break;
                case "resrev_is_hidden":
                    res.IsHidden = DbUtils.Convert.To<bool>(dr.GetValue(i), false);
                    break;
                case "resrev_meta":
                    res.Meta = dr.GetValue(i) as string;
                    break;
                case "rescontent_id":
                    res.Content.ContentId = DbUtils.Convert.To<uint>(dr.GetValue(i)) ?? 0;
                    break;
                case "rescontent_res_id":
                    res.Content.ResourceId = DbUtils.Convert.To<uint>(dr.GetValue(i)) ?? 0;
                    break;
                case "rescontent_res_rev":
                    res.Content.Revision = DbUtils.Convert.To<uint>(dr.GetValue(i)) ?? 0;
                    break;
                case "rescontent_value":
                    res.Content.SetData(dr.GetValue(i) as byte[]);
                    break;
                case "rescontent_location":
                    res.Content.Location = dr.GetValue(i) as string;
                    break;
                case "rescontent_mimetype":
                    MimeType.TryParse(dr.GetValue(i) as string, out mimeTypeTmp);
                    res.Content.MimeType = mimeTypeTmp;
                    break;
                case "rescontent_size":
                    res.Content.Size = DbUtils.Convert.To<uint>(dr.GetValue(i)) ?? 0;
                    break;
                default:
                    break;
                }

                #endregion
            }

            //Resource contents may not always be retrieved. Make sure it's null if it's not there.
            if(res.Content != null && res.Content.IsNewContent()) {
                res.Content = null;
            }

            return res;
        }

        private ResourceBE[] Resources_ExecuteInsertUpdateQuery(ResourceBE resource, string query) {
            List<ResourceBE> resources = new List<ResourceBE>();
            Catalog.NewQuery(query)
                .With("RES_ID", resource.ResourceId)
                .With("RES_HEADREV", resource.ResourceHeadRevision)
                .With("RES_TYPE", (uint)resource.ResourceType)
                .With("RES_CREATE_TIMESTAMP", resource.ResourceCreateTimestamp)
                .With("RES_UPDATE_TIMESTAMP", resource.ResourceUpdateTimestamp)
                .With("RES_CREATE_USER_ID", resource.ResourceCreateUserId)
                .With("RES_UPDATE_USER_ID", resource.ResourceUpdateUserId)
                .With("RES_DELETED", resource.ResourceIsDeleted)
                .With("RESREV_REV", resource.Revision)
                .With("RESREV_USER_ID", resource.UserId)
                .With("RESREV_PARENT_ID", resource.ParentId)
                .With("RESREV_PARENT_PAGE_ID", resource.ParentPageId)
                .With("RESREV_PARENT_USER_ID", resource.ParentUserId)
                .With("RESREV_CHANGE_MASK", (ushort)resource.ChangeMask)
                .With("RESREV_NAME", resource.Name)
                .With("RESREV_CHANGE_DESCRIPTION", resource.ChangeDescription)
                .With("RESREV_TIMESTAMP", resource.Timestamp)
                .With("RESREV_DELETED", resource.Deleted)
                .With("RESREV_CHANGESET_ID", resource.ChangeSetId)
                .With("RESREV_SIZE", resource.Size)
                .With("RESREV_MIMETYPE", resource.MimeType != null ? resource.MimeType.ToString() : null)
                .With("RESREV_CONTENT_ID", resource.ContentId)
                .With("RESREV_LANGUAGE", resource.Language)
                .With("RESREV_IS_HIDDEN", resource.IsHidden)
                .With("RESREV_META", resource.Meta)
                .With("RESCONTENT_RES_REV", resource.Content != null ? resource.Content.Revision : null)
                .Execute(delegate(IDataReader dr) {
                while(dr.Read()) {
                    resources.Add(Resources_Populate(dr));
                }
            });
            return resources.ToArray();
        }

        #region ResourceContents methods

        private ResourceContentBE Resources_ContentInsert(ResourceContentBE contents) {

            string query = @" /* ResourceDA::ContentInsert */
insert into resourcecontents (rescontent_res_id, rescontent_res_rev, rescontent_location, rescontent_mimetype, rescontent_size, rescontent_value)
values (?RESCONTENT_RES_ID, ?RESCONTENT_RES_REV, ?RESCONTENT_LOCATION, ?RESCONTENT_MIMETYPE, ?RESCONTENT_SIZE, ?RESCONTENT_VALUE);
select last_insert_id();";

            contents.ContentId = Catalog.NewQuery(query)
            .With("RESCONTENT_RES_ID", contents.ResourceId)
            .With("RESCONTENT_RES_REV", contents.Revision)
            .With("RESCONTENT_LOCATION", contents.Location)
            .With("RESCONTENT_MIMETYPE", contents.MimeType.ToString())
            .With("RESCONTENT_SIZE", contents.Size)
            .With("RESCONTENT_VALUE", contents.IsDbBased ? contents.ToBytes() : null)
            .ReadAsUInt() ?? 0;

            return contents;
        }

        private void Resources_ContentDelete(uint contentId) {
            string query = @" /* ResourceDA::ContentDelete */
delete from resourcecontents where rescontent_id = ?RESCONTENT_ID;";
            Catalog.NewQuery(query)
            .With("RESCONTENT_ID", contentId)
            .Execute();
        }

        #endregion
    }
}
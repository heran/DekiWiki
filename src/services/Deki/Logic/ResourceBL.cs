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

using MindTouch.Deki.Data;
using MindTouch.Deki.Exceptions;
using MindTouch.Dream;

namespace MindTouch.Deki.Logic {
    public class ResourceBL {

        //--- Class Fields ---
        public static readonly ResourceBE.Type[] PROPERTIES = new[] { ResourceBE.Type.PROPERTY };
        public static readonly ResourceBE.Type[] FILES = new[] { ResourceBE.Type.FILE };
        private static readonly ResourceBL _instance = new ResourceBL();

        //--- Class Properties ---
        public static ResourceBL Instance { get { return _instance; } }

        //--- Constructors ---
        private ResourceBL() { }

        //--- Methods ---
        #region ResourceDA query wrappers
        public IList<ResourceBE> GetResources(IList<uint> parentids, ResourceBE.ParentType? parentType, IList<ResourceBE.Type> resourceTypes, IList<string> names, DeletionFilter deletionStateFilter, bool? populateRevisions, uint? limit, uint? offset) {
            return DbUtils.CurrentSession.Resources_GetByQuery(parentids, parentType, resourceTypes, names, deletionStateFilter, populateRevisions, offset, limit);
        }

        public ResourceBE GetResource(uint? parentId, ResourceBE.ParentType? parentType, ResourceBE.Type resourceType, string name, DeletionFilter deletionStateFilter) {
            if(name == null) {
                throw new ArgumentNullException("name");
            }
            uint[] parentids = parentId == null ? new uint[] { } : new[] { parentId.Value };
            return GetResources(parentids, parentType, new[] { resourceType }, new[] { name }, deletionStateFilter, null, null, null).FirstOrDefault();
        }

        public IList<ResourceBE> GetResources(uint? parentId, ResourceBE.ParentType parentType, IList<ResourceBE.Type> resourceTypes, string[] names, DeletionFilter deletionStateFilter) {
            uint[] parentids = parentId == null ? new uint[] { } : new[] { parentId.Value };
            return GetResources(parentids, parentType, resourceTypes, names, deletionStateFilter, null, null, null);
        }

        public IList<ResourceBE> GetResourcesByChangeSet(uint changeSetId, ResourceBE.Type resourceType) {
            return DbUtils.CurrentSession.Resources_GetByChangeSet(changeSetId, resourceType).ToArray();
        }

        public ResourceBE GetResource(uint resourceid) {
            ResourceBE ret = DbUtils.CurrentSession.Resources_GetByIdAndRevision(resourceid, ResourceBE.HEADREVISION);
            return ret;
        }

        public ResourceBE GetResourceRevision(uint resourceid, int revision) {
            ResourceBE ret = DbUtils.CurrentSession.Resources_GetByIdAndRevision(resourceid, revision);
            return ret;
        }

        public IList<ResourceBE> GetResourceRevisions(uint resourceid, ResourceBE.ChangeOperations changeTypesFilter, SortDirection sortRevisions, uint? limit) {
            IList<ResourceBE> ret = DbUtils.CurrentSession.Resources_GetRevisions(resourceid, changeTypesFilter, sortRevisions, limit).ToArray();
            return ret;
        }

        public Dictionary<Title, ResourceBE> GetFileResourcesByTitlesWithMangling(Title[] titles) {
            if(ArrayUtil.IsNullOrEmpty(titles)) {
                return new Dictionary<Title, ResourceBE>();
            }
            return DbUtils.CurrentSession.Resources_GetFileResourcesByTitlesWithMangling(titles);
        }

        public IList<ResourceBE> PopulateChildren(ResourceBE[] resources, ResourceBE.Type[] resourceTypes, bool associateRevisions) {
            if(ArrayUtil.IsNullOrEmpty(resources)) {
                return resources;
            }

            Dictionary<uint, object> resourceIdsHash = new Dictionary<uint, object>();
            foreach(ResourceBE r in resources) {
                resourceIdsHash[r.ResourceId] = null;
            }
            uint[] resourceIds = new List<uint>(resourceIdsHash.Keys).ToArray();

            IList<ResourceBE> children = GetResources(resourceIds, null, resourceTypes, null, DeletionFilter.ACTIVEONLY, associateRevisions, null, null);
            Dictionary<ulong, IList<ResourceBE>> childrenByParentId = GroupByParentId(children, null);
            if(!associateRevisions) {

                //Head revisions of all children of the resource are associated                                 
                foreach(ResourceBE parent in resources) {
                    if(childrenByParentId.TryGetValue(parent.ResourceId, out children)) {
                        parent.ChildResources = children.ToArray();
                    }
                }
            } else {

                //Corresponding revisions of all children of the resource are associated

                foreach(ResourceBE parent in resources) {
                    if(childrenByParentId.TryGetValue(parent.ResourceId, out children)) {
                        List<ResourceBE> childrenAtRevs = new List<ResourceBE>();
                        Dictionary<uint, ResourceBE[]> revsForChild = GroupByResourceId(children);
                        foreach(KeyValuePair<uint, ResourceBE[]> childRevSet in revsForChild) {
                            ResourceBE[] childRevisions = childRevSet.Value;

                            //Ensure that the revisions are sorted by rev# desc. Index 
                            Array.Sort<ResourceBE>(childRevisions, delegate(ResourceBE left, ResourceBE right) {
                                return right.Revision.CompareTo(left.Revision);
                            });

                            if(parent.IsHeadRevision()) {
                                //Head revision of parent should use head revision of children

                                if(childRevisions.Length > 0) {
                                    childrenAtRevs.Add(childRevisions[0]);
                                }

                            } else {
                                //Determine the highest timestamp a child property revision can have
                                DateTime timestampOfNextRev = DateTime.MaxValue;

                                //Revision HEAD - 1 can use the HEAD resources timestamp
                                if(parent.Revision + 1 == parent.ResourceHeadRevision) {
                                    timestampOfNextRev = parent.ResourceUpdateTimestamp;
                                } else {
                                    //Determine timestamp of next revision

                                    ResourceBE nextRev = null;

                                    //Look to see if current list of resources contains the next revision
                                    foreach(ResourceBE r in resources) {
                                        if(parent.ResourceId == r.ResourceId && parent.Revision + 1 == r.Revision) {
                                            nextRev = r;
                                            break;
                                        }
                                    }

                                    //Perform DB call to retrieve next revision
                                    if(nextRev == null) {
                                        nextRev = GetResourceRevision(parent.ResourceId, parent.Revision + 1);
                                    }

                                    if(nextRev != null) {
                                        timestampOfNextRev = nextRev.Timestamp;
                                    }
                                }

                                //Get most recent revision of child before the timestamp of the next revision of parent.
                                for(int i = 0; i < childRevisions.Length; i++) {
                                    if(timestampOfNextRev > childRevisions[i].Timestamp) {
                                        childrenAtRevs.Add(childRevisions[i]);
                                        break;
                                    }
                                }
                            }
                        }
                        parent.ChildResources = childrenAtRevs.ToArray();
                    }
                }
            }

            return resources;
        }
        #endregion

        #region Actions on resources
        public virtual ResourceBE Delete(ResourceBE resource) {
            return Delete(resource, null, 0);
        }

        public virtual ResourceBE Delete(ResourceBE resource, PageBE parentPage, uint changeSetId) {

            //Build the new revision
            ResourceBE res = BuildRevForRemove(resource, DateTime.UtcNow, changeSetId);

            //Update db
            res = SaveResource(res);

            //Update indexes and parent page's timestamp

            //TODO MaxM: Changesink needs to accept a resource
            if(res.ResourceType == ResourceBE.Type.FILE) {
                DekiContext.Current.Instance.EventSink.AttachmentDelete(DekiContext.Current.Now, res, DekiContext.Current.User);

                // Recent changes
                RecentChangeBL.AddFileRecentChange(DekiContext.Current.Now, parentPage, DekiContext.Current.User, DekiResources.FILE_REMOVED(res.Name), changeSetId);
            }

            if(parentPage != null) {
                PageBL.Touch(parentPage, DateTime.UtcNow);
            }

            return res;
        }

        public virtual ResourceBE SaveResource(ResourceBE res) {
            ResourceBE ret = DbUtils.CurrentSession.Resources_SaveRevision(res);
            ReAssociateResourceState(res, ret);
            return ret;
        }

        public virtual ResourceBE UpdateResourceRevision(ResourceBE res) {
            ResourceBE ret = DbUtils.CurrentSession.Resources_UpdateRevision(res);
            ReAssociateResourceState(res, ret);
            return ret;
        }

        public void ReAssociateResourceState(ResourceBE oldRev, ResourceBE newRev) {
            if(oldRev == null || newRev == null) {
                return;
            }
        }
        #endregion

        #region Helper methods

        /// <summary>
        /// Returns a hash by parentid's based on parentType of lists of child resources
        /// </summary>
        /// <param name="resources"></param>
        /// <param name="parentType"></param>
        /// <returns></returns>
        public Dictionary<ulong, IList<ResourceBE>> GroupByParentIdWithCast(IEnumerable<ResourceBE> resources, ResourceBE.ParentType parentType) {

            Dictionary<ulong, List<ResourceBE>> temp = GroupByParentIdInternal(resources, parentType);
            Dictionary<ulong, IList<ResourceBE>> ret = new Dictionary<ulong, IList<ResourceBE>>();

            //Convert lists to arrays
            foreach(KeyValuePair<ulong, List<ResourceBE>> kvp in temp) {
                ret[kvp.Key] = kvp.Value;
            }

            return ret;
        }

        public Dictionary<ulong, IList<ResourceBE>> GroupByParentId(IEnumerable<ResourceBE> resources, ResourceBE.ParentType? parentType) {

            Dictionary<ulong, List<ResourceBE>> temp = GroupByParentIdInternal(resources, parentType);
            Dictionary<ulong, IList<ResourceBE>> ret = new Dictionary<ulong, IList<ResourceBE>>();

            //Convert lists to arrays
            foreach(KeyValuePair<ulong, List<ResourceBE>> kvp in temp) {
                ret[kvp.Key] = kvp.Value;
            }

            return ret;
        }

        private Dictionary<ulong, List<ResourceBE>> GroupByParentIdInternal(IEnumerable<ResourceBE> resources, ResourceBE.ParentType? parentType) {
            Dictionary<ulong, List<ResourceBE>> temp = new Dictionary<ulong, List<ResourceBE>>();
            foreach(ResourceBE res in resources) {
                ulong? id = null;
                switch(parentType) {
                case ResourceBE.ParentType.PAGE:
                    id = res.ParentPageId;
                    break;
                case ResourceBE.ParentType.USER:
                    id = res.ParentUserId;
                    break;
                default:
                    id = res.ParentId;
                    break;
                }

                if((id ?? 0) != 0) {
                    if(!temp.ContainsKey(id.Value)) {
                        temp[id.Value] = new List<ResourceBE>();
                    }

                    temp[id.Value].Add(res);
                }
            }

            return temp;
        }

        public Dictionary<uint, ResourceBE[]> GroupByResourceId(IEnumerable<ResourceBE> resources) {
            Dictionary<uint, List<ResourceBE>> temp = GroupByResourceIdInternal(resources);
            Dictionary<uint, ResourceBE[]> ret = new Dictionary<uint, ResourceBE[]>();

            //Convert lists to arrays
            foreach(KeyValuePair<uint, List<ResourceBE>> kvp in temp) {
                ret[kvp.Key] = kvp.Value.ToArray();
            }

            return ret;
        }

        private Dictionary<uint, List<ResourceBE>> GroupByResourceIdInternal(IEnumerable<ResourceBE> resources) {
            Dictionary<uint, List<ResourceBE>> temp = new Dictionary<uint, List<ResourceBE>>();
            foreach(ResourceBE res in resources) {
                if(!temp.ContainsKey(res.ResourceId)) {
                    temp[res.ResourceId] = new List<ResourceBE>();
                }

                temp[res.ResourceId].Add(res);
            }

            return temp;
        }

        public bool ValidateEtag(string etag, ResourceBE headResource, bool throwException) {
            if(!headResource.IsHeadRevision()) {
                throw new ResourceEtagNotHeadInvalidArgumentException(headResource.ResourceId, headResource.Revision);
            }

            bool isValid = false;
            if(etag != null && StringUtil.EqualsInvariant(etag, headResource.ETag())) {
                isValid = true;
            }

            if(!isValid && throwException) {
                throw new ResourceEtagConflictException(etag ?? string.Empty, headResource.ResourceId);
            }
            return isValid;
        }
        #endregion

        #region Protected resource revision building methods

        public virtual ResourceBE BuildRevForMoveAndRename(ResourceBE currentResource, PageBE targetPage, string name, uint changeSetId) {
            ResourceBE newRev = BuildResourceRev(currentResource);

            //NOTE MaxM: This logic exists here since BuildResourceRev clears out fields preventing chaining of entity building for separate actions on one revision
            if(targetPage != null && (uint)targetPage.ID != newRev.ParentPageId.Value) {
                newRev.ParentPageId = (uint)targetPage.ID;
                newRev.ChangeMask |= ResourceBE.ChangeOperations.PARENT;
            }

            if(name != null && !StringUtil.EqualsInvariant(name, currentResource.Name)) {
                newRev.Name = name;
                newRev.ChangeMask |= ResourceBE.ChangeOperations.NAME;
            }

            newRev.ChangeSetId = changeSetId;
            return newRev;
        }

        public virtual ResourceBE BuildRevForRemove(ResourceBE currentResource, DateTime timestamp, uint changeSetId) {
            ResourceBE newRev = BuildResourceRev(currentResource);
            newRev.ResourceIsDeleted = true;
            newRev.Deleted = true;
            newRev.ChangeSetId = changeSetId;
            newRev.Timestamp = timestamp;
            newRev.ChangeMask = newRev.ChangeMask | ResourceBE.ChangeOperations.DELETEFLAG;
            return newRev;
        }

        public virtual ResourceBE BuildRevForRestore(ResourceBE currentResource, PageBE targetPage, string resourceName, uint changeSetId) {
            ResourceBE newRev = BuildResourceRev(currentResource);
            newRev.ResourceIsDeleted = false;
            newRev.ChangeSetId = changeSetId;
            newRev.ParentPageId = (uint)targetPage.ID;
            newRev.Name = resourceName;
            newRev.ChangeMask = newRev.ChangeMask | ResourceBE.ChangeOperations.DELETEFLAG;
            return newRev;
        }

        public virtual ResourceBE BuildRevForNewResource(uint? parentId, ResourceBE.ParentType parentType, string resourcename, MimeType mimeType, uint size, string description, ResourceBE.Type resourceType, uint userId, ResourceContentBE content) {
            ResourceBE newResource = BuildRevForNewResource(resourcename, mimeType, size, description, resourceType, userId, content);
            switch(parentType) {
            case ResourceBE.ParentType.PAGE:
                newResource.ParentPageId = parentId;
                break;
            case ResourceBE.ParentType.USER:
                newResource.ParentUserId = parentId;
                break;
            default:
                newResource.ParentId = parentId;
                break;
            }
            newResource.ChangeMask = newResource.ChangeMask | ResourceBE.ChangeOperations.PARENT;
            return newResource;
        }

        private ResourceBE BuildRevForNewResource(string resourcename, MimeType mimeType, uint size, string description, ResourceBE.Type resourceType, uint userId, ResourceContentBE content) {
            ResourceBE newResource = new ResourceBE(resourceType);
            newResource.Name = resourcename;
            newResource.ChangeMask = newResource.ChangeMask | ResourceBE.ChangeOperations.NAME;
            newResource.Size = size;
            newResource.MimeType = mimeType;
            newResource.ChangeDescription = description;
            newResource.ResourceCreateUserId = newResource.ResourceUpdateUserId = newResource.UserId = userId;
            newResource.ChangeSetId = 0;
            newResource.Timestamp = newResource.ResourceCreateTimestamp = newResource.ResourceUpdateTimestamp = DateTime.UtcNow;
            newResource.Content = content;
            newResource.ChangeMask = newResource.ChangeMask | ResourceBE.ChangeOperations.CONTENT;
            newResource.ResourceHeadRevision = ResourceBE.TAILREVISION;
            newResource.Revision = ResourceBE.TAILREVISION;
            newResource.IsHidden = false;
            return newResource;
        }

        private ResourceBE BuildRevForExistingResource(ResourceBE currentResource, MimeType mimeType, uint size, string description) {
            ResourceBE newRev = BuildResourceRev(currentResource);
            newRev.MimeType = mimeType;
            newRev.Size = size;
            newRev.ChangeDescription = description;
            newRev.Content = currentResource.Content;
            newRev.ContentId = currentResource.ContentId;
            return newRev;
        }

        public virtual ResourceBE BuildRevForContentUpdate(ResourceBE currentResource, MimeType mimeType, uint size, string description, string name, ResourceContentBE newContent) {
            ResourceBE newRev = BuildRevForExistingResource(currentResource, mimeType, size, description);
            newRev.Content = newContent;
            newRev.ContentId = 0;
            newRev.ChangeMask |= ResourceBE.ChangeOperations.CONTENT;
            if(name != null && !StringUtil.EqualsInvariant(name, newRev.Name)) {
                newRev.ChangeMask |= ResourceBE.ChangeOperations.NAME;
                newRev.Name = name;
            }
            return newRev;
        }

        protected virtual ResourceBE BuildResourceRev(ResourceBE currentRevision) {
            currentRevision.AssertHeadRevision();

            //Clone the resource revision            
            ResourceBE newRev = new ResourceBE(currentRevision) {
                ChangeMask = ResourceBE.ChangeOperations.UNDEFINED,
                Timestamp = DateTime.UtcNow,
                UserId = DekiContext.Current.User.ID,
                ChangeSetId = 0,
                Deleted = false,
                ChangeDescription = null,
                IsHidden = false,
                Meta = currentRevision.Meta
            };

            //Initialize for new revision (clear anything that shouldn't be carried over from current rev)
            return newRev;
        }
        #endregion
    }
}

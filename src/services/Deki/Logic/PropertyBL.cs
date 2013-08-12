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
using MindTouch.Xml;

namespace MindTouch.Deki.Logic {
    public enum AbortEnum {
        Never,
        Exists,
        Modified
    }

    public class PropertyBL {

        //--- Constants ---
        public const uint DEFAULT_CONTENT_CUTOFF = 1024 * 2; //Maximum length of content to be previewed when looking at multiple properties
        public const string PROP_DESC = "urn:deki.mindtouch.com#description";

        //--- Class Fields ---
        private static readonly PropertyBL _instance = new PropertyBL();

        //--- Class Properties ---
        public static PropertyBL Instance { get { return _instance; } }

        //--- Fields ---
        private readonly ResourceBL _resourceBL;

        //--- Constructors ---
        protected PropertyBL() {
            _resourceBL = ResourceBL.Instance;
        }

        //--- Methods ---
        public IList<ResourceBE> FindPropertiesByName(string name) {
            return _resourceBL.GetResources(null, null, ResourceBL.PROPERTIES, new[] { name }, DeletionFilter.ACTIVEONLY, null, null, null);
        }

        public IList<ResourceBE> GetPageProperties(ulong pageId) {
            return _resourceBL.GetResources(new[] { (uint)pageId }, ResourceBE.ParentType.PAGE, ResourceBL.PROPERTIES, null, DeletionFilter.ACTIVEONLY, null, null, null);
        }

        public IList<ResourceBE> GetAttachmentProperties(uint resourceId) {
            return _resourceBL.GetResources(new[] { resourceId }, ResourceBE.ParentType.FILE, ResourceBL.PROPERTIES, null, DeletionFilter.ACTIVEONLY, null, null, null);
        }

        public ResourceBE GetAttachmentProperty(uint resourceId, string name) {
            return _resourceBL.GetResource(resourceId, ResourceBE.ParentType.FILE, ResourceBE.Type.PROPERTY, name, DeletionFilter.ACTIVEONLY);
        }

        public ResourceBE GetAttachmentDescription(uint resourceId) {
            return GetAttachmentProperty(resourceId, PROP_DESC);
        }

        public ResourceBE GetPageProperty(uint resourceId, string name) {
            return _resourceBL.GetResource(resourceId, ResourceBE.ParentType.PAGE, ResourceBE.Type.PROPERTY, name, DeletionFilter.ACTIVEONLY);
        }

        public IList<ResourceBE> GetUserProperties(uint userId) {
            return _resourceBL.GetResources(new[] { userId }, ResourceBE.ParentType.USER, ResourceBL.PROPERTIES, null, DeletionFilter.ACTIVEONLY, null, null, null);
        }

        public ResourceBE UpdatePropertyContent(ResourceBE prop, ResourceContentBE content, string changeDescription, string eTag, AbortEnum abort, XUri parentUri, ResourceBE.ParentType parentType) {
            if(abort == AbortEnum.Modified) {
                _resourceBL.ValidateEtag(eTag, prop, true);
            }
            prop = _resourceBL.BuildRevForContentUpdate(prop, content.MimeType, content.Size, changeDescription, null, content);
            prop = _resourceBL.SaveResource(prop);
            DekiContext.Current.Instance.EventSink.PropertyUpdate(DekiContext.Current.Now, prop, DekiContext.Current.User, parentType, parentUri);
            return prop;
        }

        public ResourceBE CreateProperty(uint? parentId, XUri parentUri, ResourceBE.ParentType parentType, string name, ResourceContentBE content, string description, string etag, AbortEnum abort) {

            //TODO: The parent resource isn't verified when the resource name and the parent info is given
            ResourceBE prop = _resourceBL.GetResource(parentId, parentType, ResourceBE.Type.PROPERTY, name, DeletionFilter.ACTIVEONLY);
            if(prop != null) {
                switch(abort) {
                case AbortEnum.Exists:
                    throw new PropertyExistsConflictException(name);
                case AbortEnum.Modified:
                    _resourceBL.ValidateEtag(etag, prop, true);
                    break;
                }
                prop = _resourceBL.BuildRevForContentUpdate(prop, content.MimeType, content.Size, description, null, content);
                prop = _resourceBL.SaveResource(prop);
                DekiContext.Current.Instance.EventSink.PropertyUpdate(DekiContext.Current.Now, prop, DekiContext.Current.User, parentType, parentUri);
            } else {
                if((abort == AbortEnum.Modified) && !string.IsNullOrEmpty(etag)) {
                    throw new PropertyUnexpectedEtagConflictException();
                }
                prop = _resourceBL.BuildRevForNewResource(parentId, parentType, name, content.MimeType, content.Size, description, ResourceBE.Type.PROPERTY, DekiContext.Current.User.ID, content);
                prop = _resourceBL.SaveResource(prop);
                DekiContext.Current.Instance.EventSink.PropertyCreate(DekiContext.Current.Now, prop, DekiContext.Current.User, parentType, parentUri);
            }
            return prop;
        }

        public ResourceBE[] SaveBatchProperties(uint? parentId, XUri parentUri, ResourceBE.ParentType parentType, XDoc doc, out string[] failedNames, out Dictionary<string, Exception> saveStatusByName) {

            //This is a specialized method that saves a batch of property updates in one request and connects them with a transaction.
            //Successful updates are returned
            //Status/description of each property update is returned as well as a hash of dreammessages by name

            saveStatusByName = new Dictionary<string, Exception>();
            List<string> failedNamesList = new List<string>();
            Dictionary<string, ResourceBE> resourcesByName = new Dictionary<string, ResourceBE>();
            List<ResourceBE> ret = new List<ResourceBE>();

            //Get list of names and perform dupe check
            foreach(XDoc propDoc in doc["/properties/property"]) {
                string name = propDoc["@name"].AsText ?? string.Empty;
                if(resourcesByName.ContainsKey(name)) {
                    throw new PropertyDuplicateInvalidOperationException(name);
                }

                resourcesByName[name] = null;
            }

            //Retrieve current properties with given name
            resourcesByName = _resourceBL.GetResources(parentId, parentType, ResourceBL.PROPERTIES, new List<string>(resourcesByName.Keys).ToArray(), DeletionFilter.ACTIVEONLY).AsHash(e => e.Name);

            //extract property info, build resource revisions, save resource, and maintain statuses for each save
            foreach(XDoc propDoc in doc["/properties/property"]) {
                ResourceBE res = null;
                string content;
                uint contentLength = 0;
                string description = string.Empty;
                string etag = null;
                MimeType mimeType;
                string name = string.Empty;

                try {
                    name = propDoc["@name"].AsText ?? string.Empty;
                    resourcesByName.TryGetValue(name, out res);
                    if(propDoc["contents"].IsEmpty) {
                        if(res == null) {
                            throw new PropertyDeleteDoesNotExistInvalidArgumentException(name);
                        } else {
                            res = DeleteProperty(res, parentType, parentUri);
                        }
                    } else {

                        //parse content from xml
                        etag = propDoc["@etag"].AsText;
                        description = propDoc["description"].AsText;
                        content = propDoc["contents"].Contents;
                        contentLength = (uint)(content ?? string.Empty).Length;
                        string mimeTypeStr = propDoc["contents/@type"].AsText;
                        if(string.IsNullOrEmpty(mimeTypeStr) || !MimeType.TryParse(mimeTypeStr, out mimeType)) {
                            throw new PropertyMimtypeInvalidArgumentException(name, mimeTypeStr);
                        }
                        ResourceContentBE resourceContent = new ResourceContentBE(content, mimeType);

                        if(res == null) {

                            //new property
                            res = CreateProperty(parentId, parentUri, parentType, name, resourceContent, description, etag, AbortEnum.Exists);
                        } else {

                            //new revision
                            res = UpdatePropertyContent(res, resourceContent, description, etag, AbortEnum.Modified, parentUri, parentType);
                        }
                    }

                    ret.Add(res);
                    saveStatusByName[name] = null;
                } catch(ResourcedMindTouchException x) {

                    //Unexpected errors fall through while business logic errors while saving a property continues processing
                    saveStatusByName[name] = x;
                    failedNamesList.Add(name);
                } catch(DreamAbortException x) {

                    // TODO (arnec): remove this once all usage of DreamExceptions is purged from Deki logic

                    //Unexpected errors fall through while business logic errors while saving a property continues processing
                    saveStatusByName[name] = x;
                    failedNamesList.Add(name);
                }
            }

            failedNames = failedNamesList.ToArray();
            return ret.ToArray();
        }

        public ResourceBE DeleteProperty(ResourceBE prop, ResourceBE.ParentType parentType, XUri parentUri) {
            DekiContext.Current.Instance.EventSink.PropertyDelete(DekiContext.Current.Now, prop, DekiContext.Current.User, parentType, parentUri);
            return _resourceBL.Delete(prop);
        }
        
        public XDoc GetPropertyXml(ResourceBE property, XUri parentResourceUri, string propSuffix, uint? contentCutoff) {
            return GetPropertyXml(new ResourceBE[] { property }, parentResourceUri, false, propSuffix, null, contentCutoff, null);
        }

        public XDoc GetPropertyXml(IList<ResourceBE> properties, XUri parentResourceUri, string propSuffix, uint? contentCutoff) {
            return GetPropertyXml(properties, parentResourceUri, true, propSuffix, null, contentCutoff, null);
        }

        public XDoc GetPropertyXml(IList<ResourceBE> properties, XUri parentResourceUri, string propSuffix, uint? contentCutoff, XDoc docToModify) {
            return GetPropertyXml(properties, parentResourceUri, true, propSuffix, null, contentCutoff, docToModify);
        }

        private XDoc GetPropertyXml(IList<ResourceBE> properties, XUri parentResourceUri, bool collection, string propSuffix, bool? explicitRevisionInfo, uint? contentCutoff, XDoc doc) {
            bool requiresEnd = false;
            if(collection) {
                string rootPropertiesNode = string.IsNullOrEmpty(propSuffix) ? "properties" : "properties." + propSuffix;
                if(doc == null) {
                    doc = new XDoc(rootPropertiesNode);
                } else {
                    doc.Start(rootPropertiesNode);
                    requiresEnd = true;
                }

                doc.Attr("count", properties.Count);

                if(parentResourceUri != null) {

                    //Note: this assumes that the property collection of a resource is always accessed by appending "properties" to the parent URI
                    doc.Attr("href", parentResourceUri.At("properties"));
                }

            } else {
                doc = XDoc.Empty;
            }

            //Batch retrieve users for user.modified and user.deleted
            Dictionary<uint, UserBE> usersById = new Dictionary<uint, UserBE>();
            foreach(ResourceBE r in properties) {
                usersById[r.UserId] = null;
            }
            if(!ArrayUtil.IsNullOrEmpty(properties)) {
                usersById = DbUtils.CurrentSession.Users_GetByIds(usersById.Keys.ToArray()).AsHash(e => e.ID);
            }

            foreach(ResourceBE p in properties) {
                doc = AppendPropertyXml(doc, p, parentResourceUri, propSuffix, explicitRevisionInfo, contentCutoff, usersById);
            }

            if(requiresEnd) {
                doc.End();
            }

            return doc;
        }

        private XDoc AppendPropertyXml(XDoc doc, ResourceBE property, XUri parentResourceUri, string propSuffix, bool? explicitRevisionInfo, uint? contentCutoff, Dictionary<uint, UserBE> usersById) {
            bool requiresEnd = false;
            explicitRevisionInfo = explicitRevisionInfo ?? false;
            string propElement = string.IsNullOrEmpty(propSuffix) ? "property" : "property." + propSuffix;
            if(doc == null || doc.IsEmpty) {
                doc = new XDoc(propElement);
            } else {
                doc.Start(propElement);
                requiresEnd = true;
            }

            //Build the base uri to the property

            bool includeContents = property.Size <= (contentCutoff ?? DEFAULT_CONTENT_CUTOFF) &&
                                    (property.MimeType.Match(MimeType.ANY_TEXT)
                                    );

            //TODO: contents null check
            doc.Attr("name", property.Name)
               .Attr("href", /*explicitRevisionInfo.Value ? property.PropertyInfoUri(parentResourceUri, true) : */property.PropertyInfoUri(parentResourceUri));

            if(property.IsHeadRevision()) {
                doc.Attr("etag", property.ETag());
            }

            /* PROPERTY REVISIONS: if(!property.IsHeadRevision() || explicitRevisionInfo.Value) {
                revisions not currently exposed.
                doc.Attr("revision", property.Revision);
            }
            */

            /* PROPERTY REVISIONS: doc.Start("revisions")
               .Attr("count", property.ResourceHeadRevision)
               .Attr("href", property.UriRevisions())
               .End();
            */
            string content = null;
            if(includeContents) {
                content = property.Content.ToText();
            }

            doc.Start("contents")
               .Attr("type", property.MimeType.ToString())
               .Attr("size", property.Size)
               .Attr("href", /*PROPERTY REVISIONS: explicitRevisionInfo.Value ? property.PropertyContentUri(true) : */property.PropertyContentUri(parentResourceUri))
               .Value(content)
               .End();

            doc.Elem("date.modified", property.Timestamp);
            UserBE userModified;
            usersById.TryGetValue(property.UserId, out userModified);
            if(userModified != null) {
                doc.Add(UserBL.GetUserXml(userModified, "modified", Utils.ShowPrivateUserInfo(userModified)));
            }

            doc.Elem("change-description", property.ChangeDescription);

            if(property.Deleted) {
                UserBE userDeleted;
                usersById.TryGetValue(property.UserId, out userDeleted);
                if(userDeleted != null) {
                    doc.Add(UserBL.GetUserXml(userDeleted, "deleted", Utils.ShowPrivateUserInfo(userDeleted)));
                }

                doc.Elem("date.deleted", property.Timestamp);
            }

            if(requiresEnd) {
                doc.End(); //property
            }

            return doc;
        }
    }
}

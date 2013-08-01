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
using MindTouch.Deki.Logic;
using MindTouch.Dream;
using MindTouch.Tasking;
using MindTouch.Xml;

namespace MindTouch.Deki {
    using Yield = IEnumerator<IYield>;

    public partial class DekiWikiService {

        //--- Constants ---
        private const string SLUGHEADER = "Slug";

        //--- Features ---

        #region GET:resource/properties
        [DreamFeature("GET:pages/{pageid}/properties", "Retrieve the properties associated with a page")]
        [DreamFeatureParam("{pageid}", "string", "either an integer page ID, \"home\", or \"=\" followed by a double uri-encoded page title")]
        [DreamFeatureParam("contentcutoff", "int?", "Only show property content shorter than this number of bytes. Default: 2048")]
        [DreamFeatureParam("names", "string?", "Comma separated list of names of properties to return. The entire value should be URI encoded including the commas. Use '*' at the start or end of a name for wildcard matches. Default: all properties")]
        [DreamFeatureParam("redirects", "int?", "If zero, do not follow page redirects.")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        [DreamFeatureStatus(DreamStatus.Ok, "The request completed successfully")]
        [DreamFeatureStatus(DreamStatus.BadRequest, "Invalid input parameter or request body")]
        [DreamFeatureStatus(DreamStatus.Forbidden, "READ access is required")]
        [DreamFeatureStatus(DreamStatus.NotFound, "Requested page could not be found")]
        public Yield GetResourcePropertiesPages(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            return GetResourcePropertiesHelper(context, request, response);
        }

        [DreamFeature("GET:files/{fileid}/properties", "Retrieve the properties associated with a file attachment")]
        [DreamFeatureParam("{fileid}", "int", "identifies a file by ID")]
        [DreamFeatureParam("contentcutoff", "int?", "Only show property content shorter than this number of bytes. Default: 2048")]
        [DreamFeatureParam("names", "string?", "Comma separated list of names of properties to return. The entire value should be URI encoded including the commas. Use '*' at the start or end of a name for wildcard matches. Default: all properties")]
        [DreamFeatureParam("redirects", "int?", "If zero, do not follow page redirects.")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        [DreamFeatureStatus(DreamStatus.Ok, "The request completed successfully")]
        [DreamFeatureStatus(DreamStatus.BadRequest, "Invalid input parameter or request body")]
        [DreamFeatureStatus(DreamStatus.Forbidden, "READ access is required")]
        [DreamFeatureStatus(DreamStatus.NotFound, "Requested file could not be found")]
        public Yield GetResourcePropertiesFiles(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            return GetResourcePropertiesHelper(context, request, response);
        }

        [DreamFeature("GET:users/{userid}/properties", "Retrieve the properties associated with a user")]
        [DreamFeatureParam("{userid}", "string", "either an integer user ID, \"current\", or \"=\" followed by a double uri-encoded user name")]
        [DreamFeatureParam("contentcutoff", "int?", "Only show property content shorter than this number of bytes. Default: 2048")]
        [DreamFeatureParam("names", "string?", "Comma separated list of names of properties to return. The entire value should be URI encoded including the commas. Use '*' at the start or end of a name for wildcard matches. Default: all properties")]
        [DreamFeatureParam("redirects", "int?", "If zero, do not follow page redirects.")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        [DreamFeatureStatus(DreamStatus.Ok, "The request completed successfully")]
        [DreamFeatureStatus(DreamStatus.BadRequest, "Invalid input parameter or request body")]
        [DreamFeatureStatus(DreamStatus.Forbidden, "ADMIN access is required to access other user's properties")]
        [DreamFeatureStatus(DreamStatus.NotFound, "Requested user could not be found")]
        public Yield GetResourcePropertiesUsers(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            return GetResourcePropertiesHelper(context, request, response);
        }

        [DreamFeature("GET:site/properties", "Retrieve the properties associated with the site")]
        [DreamFeatureParam("contentcutoff", "int?", "Only show property content shorter than this number of bytes. Default: 2048")]
        [DreamFeatureParam("names", "string?", "Comma separated list of names of properties to return. The entire value should be URI encoded including the commas. Use '*' at the start or end of a name for wildcard matches. Default: all properties")]
        [DreamFeatureParam("redirects", "int?", "If zero, do not follow page redirects.")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        [DreamFeatureStatus(DreamStatus.Ok, "The request completed successfully")]
        [DreamFeatureStatus(DreamStatus.BadRequest, "Invalid input parameter")]
        [DreamFeatureStatus(DreamStatus.Forbidden, "ADMIN access is required")]
        public Yield GetResourcePropertiesSite(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            return GetResourcePropertiesHelper(context, request, response);
        }

        [DreamFeature("GET:site/search/properties", "Retrieve all pages with a specified property")]
        [DreamFeatureParam("name", "string", "The name of the property to search for")]
        [DreamFeatureParam("verbose", "bool?", "Show verbose page output.  default: true")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        [DreamFeatureStatus(DreamStatus.Ok, "The request completed successfully")]
        [DreamFeatureStatus(DreamStatus.BadRequest, "Invalid input parameter or request body")]
        public Yield GetPagesWithProperty(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            string name = context.GetParam("name");
            bool verbose = context.GetParam("verbose", true);
            IList<ResourceBE> properties = null;
            XDoc ret = new XDoc("pages");
            properties = PropertyBL.Instance.FindPropertiesByName(name);
            if(properties.Count > 0) {
                var pageIds = properties.Select(e => e.ParentPageId ?? 0);
                var filteredPageIds = PermissionsBL.FilterDisallowed(DekiContext.Current.User, pageIds, false, Permissions.BROWSE);
                IList<PageBE> pages = PageBL.GetPagesByIdsPreserveOrder(filteredPageIds);
                foreach(PageBE page in pages) {
                    if(verbose)
                        ret.Add(PageBL.GetPageXmlVerbose(page, null));
                    else
                        ret.Add(PageBL.GetPageXml(page, null));
                }
            }
            response.Return(DreamMessage.Ok(ret));
            yield break;
        }

        private Yield GetResourcePropertiesHelper(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            uint? parentId;
            ResourceBE.ParentType parentType;
            XUri parentUri;
            AuthorizeParent(context, Permissions.READ, out parentId, out parentType, out parentUri);

            string[] names = null;
            string namesStr = context.GetParam("names", null);
            if(!string.IsNullOrEmpty(namesStr)) {
                names = namesStr.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            }
            uint contentCutoff = context.GetParam<uint>("contentcutoff", PropertyBL.DEFAULT_CONTENT_CUTOFF);

            IList<ResourceBE> properties = ResourceBL.Instance.GetResources(parentId, parentType, ResourceBL.PROPERTIES, names, DeletionFilter.ACTIVEONLY);
            XDoc ret = PropertyBL.Instance.GetPropertyXml(properties, parentUri, null, contentCutoff);

            //Backwards compatibility with GET:pages/{pageid}/properties returning the language
            if(parentType == ResourceBE.ParentType.PAGE && parentId != null) {
                PageBE p = PageBL.GetPageById(parentId.Value);
                if(p != null) {
                    ret.Start("language").Attr("deprecated", true).Value(p.Language).End();
                }
            }

            response.Return(DreamMessage.Ok(ret));
            yield break;
        }
        #endregion

        #region GET:resource/properties/{key}
        [DreamFeature("GET:pages/{pageid}/properties/{key}", "Retrieve the content of a page property")]
        [DreamFeatureParam("{pageid}", "string", "either an integer page ID, \"home\", or \"=\" followed by a double uri-encoded page title")]
        [DreamFeatureParam("{key}", "string", "A unique identifier for a property that is obtained through GET:pages/{pageid}/properties")]
        [DreamFeatureParam("redirects", "int?", "If zero, do not follow page redirects.")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        [DreamFeatureStatus(DreamStatus.Ok, "The request completed successfully")]
        [DreamFeatureStatus(DreamStatus.BadRequest, "Invalid input parameter or request body")]
        [DreamFeatureStatus(DreamStatus.Forbidden, "READ access is required")]
        [DreamFeatureStatus(DreamStatus.NotFound, "Requested page and/or property could not be found")]
        public Yield GetResourcePropertyPages(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            return GetResourcePropertyHelper(context, request, response);
        }

        [DreamFeature("GET:files/{fileid}/properties/{key}", "Retrieve the content of an attachment property")]
        [DreamFeatureParam("{fileid}", "int", "identifies a file by ID")]
        [DreamFeatureParam("{key}", "string", "A unique identifier for a property that is obtained through GET:files/{fileid}/properties")]
        [DreamFeatureParam("redirects", "int?", "If zero, do not follow page redirects.")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        [DreamFeatureStatus(DreamStatus.Ok, "The request completed successfully")]
        [DreamFeatureStatus(DreamStatus.BadRequest, "Invalid input parameter or request body")]
        [DreamFeatureStatus(DreamStatus.Forbidden, "READ access is required")]
        [DreamFeatureStatus(DreamStatus.NotFound, "Requested file and/or property could not be found")]
        public Yield GetResourcePropertyFiles(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            return GetResourcePropertyHelper(context, request, response);
        }

        [DreamFeature("GET:users/{userid}/properties/{key}", "Retrieve the content of a user property")]
        [DreamFeatureParam("{userid}", "string", "either an integer user ID, \"current\", or \"=\" followed by a double uri-encoded user name")]
        [DreamFeatureParam("{key}", "string", "A unique identifier for a property that is obtained through GET:users/{userid}/properties")]
        [DreamFeatureParam("redirects", "int?", "If zero, do not follow page redirects.")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        [DreamFeatureStatus(DreamStatus.Ok, "The request completed successfully")]
        [DreamFeatureStatus(DreamStatus.BadRequest, "Invalid input parameter or request body")]
        [DreamFeatureStatus(DreamStatus.Forbidden, "ADMIN access is required to access other user's properties")]
        [DreamFeatureStatus(DreamStatus.NotFound, "Requested user and/or property could not be found")]
        public Yield GetResourcePropertyUsers(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            return GetResourcePropertyHelper(context, request, response);
        }

        [DreamFeature("GET:site/properties/{key}", "Retrieve the content of a site property")]
        [DreamFeatureParam("{key}", "string", "A unique identifier for a property that is obtained through GET:site/properties")]
        [DreamFeatureParam("redirects", "int?", "If zero, do not follow page redirects.")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        [DreamFeatureStatus(DreamStatus.Ok, "The request completed successfully")]
        [DreamFeatureStatus(DreamStatus.BadRequest, "Invalid input parameter or request body")]
        [DreamFeatureStatus(DreamStatus.Forbidden, "ADMIN access is required")]
        [DreamFeatureStatus(DreamStatus.NotFound, "Requested property could not be found")]
        public Yield GetResourcePropertySite(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            return GetResourcePropertyHelper(context, request, response);
        }

        private Yield GetResourcePropertyHelper(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            ResourceBE property = GetPropertyFromRequest(context, Permissions.READ, true);
            var content = ResourceContentBL.Instance.Get(property);

            //TODO (maxm): Consider content-disposition.
            DreamMessage ret = DreamMessage.Ok(content.MimeType, content.ToBytes());
            if(property.IsHeadRevision()) {
                ret.Headers.ETag = property.ETag();
            }
            response.Return(ret);
            yield break;
        }
        #endregion

        #region GET:resource/properties/{key}/info
        [DreamFeature("GET:pages/{pageid}/properties/{key}/info", "Retrieve the metadata about a page property")]
        [DreamFeatureParam("{pageid}", "string", "either an integer page ID, \"home\", or \"=\" followed by a double uri-encoded page title")]
        [DreamFeatureParam("{key}", "string", "A unique identifier for a property that is obtained through GET:pages/{pageid}/properties")]
        [DreamFeatureParam("contentcutoff", "int?", "Only show property content shorter than this number of bytes. Default: 2048")]
        [DreamFeatureParam("redirects", "int?", "If zero, do not follow page redirects.")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        [DreamFeatureStatus(DreamStatus.Ok, "The request completed successfully")]
        [DreamFeatureStatus(DreamStatus.BadRequest, "Invalid input parameter or request body")]
        [DreamFeatureStatus(DreamStatus.Forbidden, "READ access is required")]
        [DreamFeatureStatus(DreamStatus.NotFound, "Requested page and/or property could not be found")]
        public Yield GetResourcePropertyContentsPages(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            return GetResourcePropertyContentsHelper(context, request, response);
        }

        [DreamFeature("GET:files/{fileid}/properties/{key}/info", "Retrieve the metadata about an attachment property")]
        [DreamFeatureParam("{fileid}", "int", "identifies a file by ID")]
        [DreamFeatureParam("{key}", "string", "A unique identifier for a property that is obtained through GET:files/{fileid}/properties")]
        [DreamFeatureParam("contentcutoff", "int?", "Only show property content shorter than this number of bytes. Default: 2048")]
        [DreamFeatureParam("redirects", "int?", "If zero, do not follow page redirects.")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        [DreamFeatureStatus(DreamStatus.Ok, "The request completed successfully")]
        [DreamFeatureStatus(DreamStatus.BadRequest, "Invalid input parameter or request body")]
        [DreamFeatureStatus(DreamStatus.Forbidden, "READ access is required")]
        [DreamFeatureStatus(DreamStatus.NotFound, "Requested file and/or property could not be found")]
        public Yield GetResourcePropertyContentsFiles(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            return GetResourcePropertyContentsHelper(context, request, response);
        }

        [DreamFeature("GET:users/{userid}/properties/{key}/info", "Retrieve the metadata about a user property")]
        [DreamFeatureParam("{userid}", "string", "either an integer user ID, \"current\", or \"=\" followed by a double uri-encoded user name")]
        [DreamFeatureParam("{key}", "string", "A unique identifier for a property that is obtained through GET:users/{userid}/properties")]
        [DreamFeatureParam("contentcutoff", "int?", "Only show property content shorter than this number of bytes. Default: 2048")]
        [DreamFeatureParam("redirects", "int?", "If zero, do not follow page redirects.")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        [DreamFeatureStatus(DreamStatus.Ok, "The request completed successfully")]
        [DreamFeatureStatus(DreamStatus.BadRequest, "Invalid input parameter or request body")]
        [DreamFeatureStatus(DreamStatus.Forbidden, "ADMIN access is required to access other user's properties")]
        [DreamFeatureStatus(DreamStatus.NotFound, "Requested user and/or property could not be found")]
        public Yield GetResourcePropertyContentsUsers(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            return GetResourcePropertyContentsHelper(context, request, response);
        }

        [DreamFeature("GET:site/properties/{key}/info", "Retrieve the metadata about a site property")]
        [DreamFeatureParam("{key}", "string", "A unique identifier for a property that is obtained through GET:site/properties")]
        [DreamFeatureParam("contentcutoff", "int?", "Only show property content shorter than this number of bytes. Default: 2048")]
        [DreamFeatureParam("redirects", "int?", "If zero, do not follow page redirects.")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        [DreamFeatureStatus(DreamStatus.Ok, "The request completed successfully")]
        [DreamFeatureStatus(DreamStatus.BadRequest, "Invalid input parameter or request body")]
        [DreamFeatureStatus(DreamStatus.Forbidden, "ADMIN access is required")]
        [DreamFeatureStatus(DreamStatus.NotFound, "Requested property could not be found")]
        public Yield GetResourcePropertyContentsSite(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            return GetResourcePropertyContentsHelper(context, request, response);
        }

        private Yield GetResourcePropertyContentsHelper(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            uint? parentId;
            ResourceBE.ParentType parentType;
            XUri parentUri;
            string name;

            uint contentCutoff = context.GetParam<uint>("contentcutoff", PropertyBL.DEFAULT_CONTENT_CUTOFF);

            ResourceBE property = GetPropertyFromRequest(context, Permissions.READ, true, true, out name, out parentId, out parentType, out parentUri);
            response.Return(DreamMessage.Ok(PropertyBL.Instance.GetPropertyXml(property, parentUri, null, contentCutoff)));
            yield break;
        }
        #endregion

        #region PUT:resource/properties
        [DreamFeature("PUT:pages/{pageid}/properties", "Perform changes on multiple page properties")]
        [DreamFeatureParam("{pageid}", "string", "either an integer page ID, \"home\", or \"=\" followed by a double uri-encoded page title")]
        [DreamFeatureParam("redirects", "int?", "If zero, do not follow page redirects.")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        [DreamFeatureStatus(DreamStatus.Ok, "The request completed successfully")]
        [DreamFeatureStatus(DreamStatus.BadRequest, "Invalid input parameter or request body")]
        [DreamFeatureStatus(DreamStatus.Forbidden, "UPDATE access is required")]
        [DreamFeatureStatus(DreamStatus.NotFound, "Requested page could not be found")]
        public Yield PutResourcePropertiesPages(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            return PutResourcePropertiesHelper(context, request, response);
        }

        [DreamFeature("PUT:files/{fileid}/properties", "Perform changes on multiple attachment properties")]
        [DreamFeatureParam("{fileid}", "int", "identifies a file by ID")]
        [DreamFeatureParam("redirects", "int?", "If zero, do not follow page redirects.")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        [DreamFeatureStatus(DreamStatus.Ok, "The request completed successfully")]
        [DreamFeatureStatus(DreamStatus.BadRequest, "Invalid input parameter or request body")]
        [DreamFeatureStatus(DreamStatus.Forbidden, "UPDATE access is required")]
        [DreamFeatureStatus(DreamStatus.NotFound, "Requested file could not be found")]
        public Yield PutResourcePropertiesFiles(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            return PutResourcePropertiesHelper(context, request, response);
        }

        [DreamFeature("PUT:users/{userid}/properties", "Perform changes on multiple user properties")]
        [DreamFeatureParam("{userid}", "string", "either an integer user ID, \"current\", or \"=\" followed by a double uri-encoded user name")]
        [DreamFeatureParam("redirects", "int?", "If zero, do not follow page redirects.")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        [DreamFeatureStatus(DreamStatus.Ok, "The request completed successfully")]
        [DreamFeatureStatus(DreamStatus.BadRequest, "Invalid input parameter or request body")]
        [DreamFeatureStatus(DreamStatus.Forbidden, "ADMIN access is required to access other user's properties")]
        [DreamFeatureStatus(DreamStatus.NotFound, "Requested user could not be found")]
        public Yield PutResourcePropertiesUsers(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            return PutResourcePropertiesHelper(context, request, response);
        }


        [DreamFeature("PUT:site/properties", "Perform changes on multiple site properties")]
        [DreamFeatureParam("redirects", "int?", "If zero, do not follow page redirects.")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        [DreamFeatureStatus(DreamStatus.Ok, "The request completed successfully")]
        [DreamFeatureStatus(DreamStatus.BadRequest, "Invalid input parameter or request body")]
        [DreamFeatureStatus(DreamStatus.Forbidden, "ADMIN access is required")]
        public Yield PutResourcePropertiesSite(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            return PutResourcePropertiesHelper(context, request, response);
        }

        private Yield PutResourcePropertiesHelper(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            var resourceMgr = DekiContext.Current.Resources;
            uint? parentId;
            ResourceBE.ParentType parentType;
            XUri parentUri;
            PageBE parentPage;
            AuthorizeParent(context, Permissions.UPDATE, out parentId, out parentType, out parentUri, out parentPage);

            //Backwards compatibility -- set the page language
            if(parentType == ResourceBE.ParentType.PAGE && parentId != null) {
                PageBL.UpdatePropertiesFromXml(PageBL.GetPageById(parentId.Value), request.ToDocument());
            }
            string[] failedNames;
            Dictionary<string, Exception> statusByName;
            ResourceBE[] resources = PropertyBL.Instance.SaveBatchProperties(parentId, parentUri, parentType, request.ToDocument(), out failedNames, out statusByName);
            XDoc ret = PropertyBL.Instance.GetPropertyXml(resources, parentUri, null, null);


            // TODO (arnec): this whole thing needs to be redone, once we figure out how to handle batch operation errors without HTTP assumptions

            //Associate statuses with properties
            foreach(KeyValuePair<string, Exception> kvp in statusByName) {
                string name = kvp.Key;
                if(kvp.Value == null) {

                    //Successful operation: modify existing xml property block with status
                    var propXml = ret["property[@name = '" + name + "']"];
                    if(!propXml.IsEmpty) {
                        propXml.Start("status").Attr("code", (int)DreamStatus.Ok).End();
                    }
                    continue;
                }
                var dreamException = kvp.Value as DreamAbortException;
                if(dreamException == null) {
                    var dekiException = kvp.Value as ResourcedMindTouchException;
                    if(dekiException == null) {
                        _log.WarnFormat("unable to determine status from exception: {0}", kvp.Value);
                        ret.Start("property").Attr("name", name)
                            .Start("status").Attr("code", (int)DreamStatus.InternalError)
                                .Value(kvp.Value.Message)
                            .End()
                        .End();
                    } else {
                        ret.Start("property").Attr("name", name)
                            .Start("status").Attr("code", (int)dekiException.Status)
                                .Value(resourceMgr.Localize(dekiException.Resource))
                            .End()
                        .End();
                    }
                } else {
                    var message = dreamException.Response;
                    if(message.Status == DreamStatus.Ok) {

                        //Successful operation: modify existing xml property block with status
                        var propXml = ret["property[@name = '" + name + "']"];
                        if(!propXml.IsEmpty) {
                            propXml.Start("status").Attr("code", (int)message.Status).End();
                        }
                    } else {
                        ret.Start("property").Attr("name", name)
                            .Start("status").Attr("code", (int)message.Status);
                        if(message.ContentType.IsXml) {
                            ret.Add(message.ToDocument());
                        } else {
                            ret.Value(message.AsText());
                        }
                        ret.End().End();
                    }
                }
            }
            if(ArrayUtil.IsNullOrEmpty(failedNames)) {
                response.Return(DreamMessage.Ok(ret));
            } else {
                response.Return(new DreamMessage(DreamStatus.MultiStatus, null, ret));
            }
            yield break;
        }

        #endregion

        #region POST:resource/properties
        [DreamFeature("POST:pages/{pageid}/properties", "Create a page property")]
        [DreamFeatureParam("{pageid}", "string", "either an integer page ID, \"home\", or \"=\" followed by a double uri-encoded page title")]
        [DreamFeatureParam("description", "string?", "Description of property")]
        [DreamFeatureParam("redirects", "int?", "If zero, do not follow page redirects.")]
        [DreamFeatureParam("abort", "{never, modified, exists}?", "Specifies condition under which to prevent the creation; default is exists.")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        [DreamFeatureStatus(DreamStatus.Ok, "The request completed successfully")]
        [DreamFeatureStatus(DreamStatus.BadRequest, "Invalid input parameter or request body")]
        [DreamFeatureStatus(DreamStatus.Forbidden, "UPDATE access is required")]
        [DreamFeatureStatus(DreamStatus.NotFound, "Requested page could not be found")]
        public Yield PostResourcePropertyPages(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            return PostResourcePropertyHelper(context, request, response);
        }

        [DreamFeature("POST:files/{fileid}/properties", "Create an attachment property")]
        [DreamFeatureParam("{fileid}", "int", "identifies a file by ID")]
        [DreamFeatureParam("description", "string?", "Description of property")]
        [DreamFeatureParam("redirects", "int?", "If zero, do not follow page redirects.")]
        [DreamFeatureParam("abort", "{never, modified, exists}?", "Specifies condition under which to prevent the creation; default is exists.")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        [DreamFeatureStatus(DreamStatus.Ok, "The request completed successfully")]
        [DreamFeatureStatus(DreamStatus.BadRequest, "Invalid input parameter or request body")]
        [DreamFeatureStatus(DreamStatus.Forbidden, "UPDATE access is required")]
        [DreamFeatureStatus(DreamStatus.NotFound, "Requested file could not be found")]
        public Yield PostResourcePropertyFiles(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            return PostResourcePropertyHelper(context, request, response);
        }

        [DreamFeature("POST:users/{userid}/properties", "Create a user property")]
        [DreamFeatureParam("{userid}", "string", "either an integer user ID, \"current\", or \"=\" followed by a double uri-encoded user name")]
        [DreamFeatureParam("description", "string?", "Description of property")]
        [DreamFeatureParam("redirects", "int?", "If zero, do not follow page redirects.")]
        [DreamFeatureParam("abort", "{never, modified, exists}?", "Specifies condition under which to prevent the creation; default is exists.")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        [DreamFeatureStatus(DreamStatus.Ok, "The request completed successfully")]
        [DreamFeatureStatus(DreamStatus.BadRequest, "Invalid input parameter or request body")]
        [DreamFeatureStatus(DreamStatus.Forbidden, "ADMIN access is required to access other user's properties")]
        [DreamFeatureStatus(DreamStatus.NotFound, "Requested user could not be found")]
        public Yield PostResourcePropertyUsers(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            return PostResourcePropertyHelper(context, request, response);
        }


        [DreamFeature("POST:site/properties", "Create a site property")]
        [DreamFeatureParam("description", "string?", "Description of property")]
        [DreamFeatureParam("redirects", "int?", "If zero, do not follow page redirects.")]
        [DreamFeatureParam("abort", "{never, modified, exists}?", "Specifies condition under which to prevent the creation; default is exists.")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        [DreamFeatureStatus(DreamStatus.Ok, "The request completed successfully")]
        [DreamFeatureStatus(DreamStatus.BadRequest, "Invalid input parameter or request body")]
        [DreamFeatureStatus(DreamStatus.Forbidden, "ADMIN access is required")]
        public Yield PostResourcePropertySite(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            return PostResourcePropertyHelper(context, request, response);
        }

        private Yield PostResourcePropertyHelper(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            uint? parentId;
            ResourceBE.ParentType parentType;
            XUri parentUri;
            PageBE parentPage;
            AuthorizeParent(context, Permissions.UPDATE, out parentId, out parentType, out parentUri, out parentPage);
            string etag = request.Headers.ETag ?? context.GetParam("etag", null);
            AbortEnum abort = SysUtil.ChangeType<AbortEnum>(context.GetParam("abort", "exists"));
            string description = context.GetParam("description", string.Empty);
            string name = XUri.Decode(request.Headers[SLUGHEADER]);
            if(string.IsNullOrEmpty(name)) {
                throw new PropertyCreateMissingSlugInvalidOperationException();
            }
            ResourceContentBE resourceContent = ResourceContentBL.Instance.CreateDbSerializedContentFromStream(request.AsStream(), request.ContentLength, request.ContentType);
            ResourceBE property = PropertyBL.Instance.CreateProperty(parentId, parentUri, parentType, name, resourceContent, description, etag, abort);
            XDoc ret = PropertyBL.Instance.GetPropertyXml(property, parentUri, null, null);
            response.Return(DreamMessage.Ok(ret));
            yield break;
        }

        #endregion

        #region PUT:resource/properties/{key}
        [DreamFeature("PUT:pages/{pageid}/properties/{key}", "Update an existing page property")]
        [DreamFeatureParam("{pageid}", "string", "either an integer page ID, \"home\", or \"=\" followed by a double uri-encoded page title")]
        [DreamFeatureParam("{key}", "string", "A unique identifier for a property that is obtained through GET:pages/{pageid}/properties")]
        [DreamFeatureParam("description", "string?", "Description of property")]
        [DreamFeatureParam("etag", "string?", "Etag of the current version of the property. Can alternatively be provided via etag header.")]
        [DreamFeatureParam("redirects", "int?", "If zero, do not follow page redirects.")]
        [DreamFeatureParam("abort", "{never, modified, exists}?", "Specifies condition under which to prevent the update; default is modified.")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        [DreamFeatureStatus(DreamStatus.Ok, "The request completed successfully")]
        [DreamFeatureStatus(DreamStatus.BadRequest, "Invalid input parameter or request body")]
        [DreamFeatureStatus(DreamStatus.Forbidden, "UPDATE access is required")]
        [DreamFeatureStatus(DreamStatus.NotFound, "Requested page and/or property could not be found")]
        public Yield PutResourcePropertyPages(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            return PutResourcePropertyHelper(context, request, response);
        }

        [DreamFeature("PUT:pages/{pageid}/files/{filename}/properties/{key}", "Update an existing attachment property")]
        [DreamFeature("PUT:files/{fileid}/properties/{key}", "Update an existing attachment property")]
        [DreamFeatureParam("{fileid}", "int", "identifies a file by ID")]
        [DreamFeatureParam("{key}", "string", "A unique identifier for a property that is obtained through GET:files/{fileid}/properties")]
        [DreamFeatureParam("description", "string?", "Description of property")]
        [DreamFeatureParam("etag", "string?", "Etag of the current version of the property. Can alternatively be provided via etag header.")]
        [DreamFeatureParam("redirects", "int?", "If zero, do not follow page redirects.")]
        [DreamFeatureParam("abort", "{never, modified, exists}?", "Specifies condition under which to prevent the update; default is modified.")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        [DreamFeatureStatus(DreamStatus.Ok, "The request completed successfully")]
        [DreamFeatureStatus(DreamStatus.BadRequest, "Invalid input parameter or request body")]
        [DreamFeatureStatus(DreamStatus.Forbidden, "UPDATE access is required")]
        [DreamFeatureStatus(DreamStatus.NotFound, "Requested file and/or property could not be found")]
        public Yield PutResourcePropertyFiles(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            return PutResourcePropertyHelper(context, request, response);
        }

        [DreamFeature("PUT:users/{userid}/properties/{key}", "Update an existing user property")]
        [DreamFeatureParam("{userid}", "string", "either an integer user ID, \"current\", or \"=\" followed by a double uri-encoded user name")]
        [DreamFeatureParam("{key}", "string", "A unique identifier for a property that is obtained through GET:users/{userid}/properties")]
        [DreamFeatureParam("description", "string?", "Description of property")]
        [DreamFeatureParam("etag", "string?", "Etag of the current version of the property. Can alternatively be provided via etag header.")]
        [DreamFeatureParam("redirects", "int?", "If zero, do not follow page redirects.")]
        [DreamFeatureParam("abort", "{never, modified, exists}?", "Specifies condition under which to prevent the update; default is modified.")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        [DreamFeatureStatus(DreamStatus.Ok, "The request completed successfully")]
        [DreamFeatureStatus(DreamStatus.BadRequest, "Invalid input parameter or request body")]
        [DreamFeatureStatus(DreamStatus.Forbidden, "ADMIN access is required to access other user's properties")]
        [DreamFeatureStatus(DreamStatus.NotFound, "Requested user and/or property could not be found")]
        public Yield PutResourcePropertyUsers(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            return PutResourcePropertyHelper(context, request, response);
        }

        [DreamFeature("PUT:site/properties/{key}", "Update an existing site property")]
        [DreamFeatureParam("{key}", "string", "A unique identifier for a property that is obtained through GET:site/properties")]
        [DreamFeatureParam("description", "string?", "Description of property")]
        [DreamFeatureParam("etag", "string?", "Etag of the current version of the property. Can alternatively be provided via etag header.")]
        [DreamFeatureParam("redirects", "int?", "If zero, do not follow page redirects.")]
        [DreamFeatureParam("abort", "{never, modified, exists}?", "Specifies condition under which to prevent the update; default is modified.")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        [DreamFeatureStatus(DreamStatus.Ok, "The request completed successfully")]
        [DreamFeatureStatus(DreamStatus.BadRequest, "Invalid input parameter or request body")]
        [DreamFeatureStatus(DreamStatus.Forbidden, "ADMIN access is required")]
        [DreamFeatureStatus(DreamStatus.NotFound, "Requested property could not be found")]
        public Yield PutResourcePropertySite(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            return PutResourcePropertyHelper(context, request, response);
        }

        private Yield PutResourcePropertyHelper(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            uint? parentId;
            ResourceBE.ParentType parentType;
            XUri parentUri;

            string etag = request.Headers.ETag ?? context.GetParam("etag", null);
            string description = context.GetParam("description", string.Empty);
            AbortEnum abort = SysUtil.ChangeType<AbortEnum>(context.GetParam("abort", "modified"));
            string name;

            ResourceBE prop = GetPropertyFromRequest(context, Permissions.UPDATE, false, false, out name, out parentId, out parentType, out parentUri);
            if(prop == null) {

                // abort if the user specified to abort on 'modified' and provided an 'etag'; since no etag exists, they are not the same
                if((abort == AbortEnum.Modified) && !string.IsNullOrEmpty(etag)) {
                    throw new PropertyEditNonexistingConflictException();
                }

                // create the resource
                PageBE parentPage;
                AuthorizeParent(context, Permissions.UPDATE, out parentId, out parentType, out parentUri, out parentPage);
                ResourceContentBE resourceContent = ResourceContentBL.Instance.CreateDbSerializedContentFromRequest(request);
                prop = PropertyBL.Instance.CreateProperty(parentId, parentUri, parentType, name, resourceContent, description, etag, abort);
            } else if(abort == AbortEnum.Exists) {
                throw new PropertyAbortOnExistsConflictException(name);
            } else {
                ResourceContentBE resourceContent = ResourceContentBL.Instance.CreateDbSerializedContentFromRequest(request);
                prop = PropertyBL.Instance.UpdatePropertyContent(prop, resourceContent, description, etag, abort, parentUri, parentType);
            }
            XDoc ret = PropertyBL.Instance.GetPropertyXml(prop, parentUri, null, null);
            response.Return(DreamMessage.Ok(ret));
            yield break;
        }
        #endregion

        #region DELETE:resource/properties/{key}
        [DreamFeature("DELETE:pages/{pageid}/properties/{key}", "Remove a page property")]
        [DreamFeatureParam("{pageid}", "string", "either an integer page ID, \"home\", or \"=\" followed by a double uri-encoded page title")]
        [DreamFeatureParam("{key}", "string", "A unique identifier for a property that is obtained through GET:pages/{pageid}/properties")]
        [DreamFeatureParam("redirects", "int?", "If zero, do not follow page redirects.")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        [DreamFeatureStatus(DreamStatus.Ok, "The request completed successfully")]
        [DreamFeatureStatus(DreamStatus.BadRequest, "Invalid input parameter or request body")]
        [DreamFeatureStatus(DreamStatus.Forbidden, "UPDATE access is required")]
        [DreamFeatureStatus(DreamStatus.NotFound, "Requested page and/or property could not be found")]
        public Yield DeleteResourcePropertyPages(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            return DeleteResourcePropertyHelper(context, request, response);
        }

        [DreamFeature("DELETE:files/{fileid}/properties/{key}", "Remove an attachment property")]
        [DreamFeatureParam("{fileid}", "int", "identifies a file by ID")]
        [DreamFeatureParam("{key}", "string", "A unique identifier for a property that is obtained through GET:files/{fileid}/properties")]
        [DreamFeatureParam("redirects", "int?", "If zero, do not follow page redirects.")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        [DreamFeatureStatus(DreamStatus.Ok, "The request completed successfully")]
        [DreamFeatureStatus(DreamStatus.BadRequest, "Invalid input parameter or request body")]
        [DreamFeatureStatus(DreamStatus.Forbidden, "UPDATE access is required")]
        [DreamFeatureStatus(DreamStatus.NotFound, "Requested file and/or property could not be found")]
        public Yield DeleteResourcePropertyFiles(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            return DeleteResourcePropertyHelper(context, request, response);
        }

        [DreamFeature("DELETE:users/{userid}/properties/{key}", "Remove a user property")]
        [DreamFeatureParam("{userid}", "string", "either an integer user ID, \"current\", or \"=\" followed by a double uri-encoded user name")]
        [DreamFeatureParam("{key}", "string", "A unique identifier for a property that is obtained through GET:users/{userid}/properties")]
        [DreamFeatureParam("redirects", "int?", "If zero, do not follow page redirects.")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        [DreamFeatureStatus(DreamStatus.Ok, "The request completed successfully")]
        [DreamFeatureStatus(DreamStatus.BadRequest, "Invalid input parameter or request body")]
        [DreamFeatureStatus(DreamStatus.Forbidden, "ADMIN access is required to access other user's properties")]
        [DreamFeatureStatus(DreamStatus.NotFound, "Requested user and/or property could not be found")]
        public Yield DeleteResourcePropertyUsers(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            return DeleteResourcePropertyHelper(context, request, response);
        }

        [DreamFeature("DELETE:site/properties/{key}", "Remove a site property")]
        [DreamFeatureParam("{userid}", "string", "either an integer user ID, \"current\", or \"=\" followed by a double uri-encoded user name")]
        [DreamFeatureParam("{key}", "string", "A unique identifier for a property that is obtained through GET:site/properties")]
        [DreamFeatureParam("redirects", "int?", "If zero, do not follow page redirects.")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        [DreamFeatureStatus(DreamStatus.Ok, "The request completed successfully")]
        [DreamFeatureStatus(DreamStatus.BadRequest, "Invalid input parameter or request body")]
        [DreamFeatureStatus(DreamStatus.Forbidden, "ADMIN access is required")]
        [DreamFeatureStatus(DreamStatus.NotFound, "Requested property could not be found")]
        public Yield DeleteResourcePropertySite(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            return DeleteResourcePropertyHelper(context, request, response);
        }

        private Yield DeleteResourcePropertyHelper(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            uint? parentId;
            ResourceBE.ParentType parentType;
            XUri parentUri;
            string name;
            ResourceBE prop = GetPropertyFromRequest(context, Permissions.UPDATE, false, true, out name, out parentId, out parentType, out parentUri);
            PropertyBL.Instance.DeleteProperty(prop, parentType, parentUri);
            response.Return(DreamMessage.Ok());
            yield break;
        }

        #endregion

        private ResourceBE GetPropertyFromRequest(DreamContext context, Permissions access, bool allowRevision) {
            uint? parentId;
            ResourceBE.ParentType parentType;
            XUri parentUri;
            string name;
            return GetPropertyFromRequest(context, access, allowRevision, true, out name, out parentId, out parentType, out parentUri);
        }

        private ResourceBE GetPropertyFromRequest(DreamContext context, Permissions access, bool allowRevision, bool throwException, out string name, out uint? parentId, out ResourceBE.ParentType parentType, out XUri parentUri) {
            PageBE parentPage;
            AuthorizeParent(context, access, out parentId, out parentType, out parentUri, out parentPage);
            name = XUri.Decode(context.GetParam("key"));
            int revision = ResourceBE.HEADREVISION;

            //revisions not currently exposed.
            /*if (allowRevision) {
                string revStr = context.GetParam("revision", "HEAD");
                if (!StringUtil.EqualsInvariantIgnoreCase(revStr, "HEAD")) {
                    if (!uint.TryParse(revStr, out revision)) {
                        throw new DreamAbortException(DreamMessage.BadRequest(DekiResources.REVISION_HEAD_OR_INT()));
                    }
                }
            }*/

            ResourceBE property = null;
            property = ResourceBL.Instance.GetResource(parentId, parentType, ResourceBE.Type.PROPERTY, name, DeletionFilter.ACTIVEONLY);
            if((property != null) && (revision != ResourceBE.HEADREVISION)) {
                property = ResourceBL.Instance.GetResourceRevision(property.ResourceId, revision);
            }

            if(property == null) {
                if(throwException) {
                    throw new DreamAbortException(DreamMessage.NotFound(string.Format("Property '{0}' not found", name)));
                }
            }
            return property;
        }

        private void AuthorizeParent(DreamContext context, Permissions access, out uint? parentId, out ResourceBE.ParentType parentType, out XUri parentUri) {
            PageBE parentPage;
            AuthorizeParent(context, access, out parentId, out parentType, out parentUri, out parentPage);
        }

        private void AuthorizeParent(DreamContext context, Permissions access, out uint? parentId, out ResourceBE.ParentType parentType, out XUri parentUri, out PageBE parentPage) {
            parentPage = null;
            parentUri = null;

            //Determine parent resource type from uri. 
            switch(context.Uri.GetSegment(1, UriPathFormat.Normalized)) {
            case "pages":
                if(null != context.GetParam("filename", null)) {
                    goto case "files";
                } else {
                    parentType = ResourceBE.ParentType.PAGE;
                    parentPage = PageBL_GetPageFromUrl(context, true);
                    PageBL.AuthorizePage(DekiContext.Current.User, access, parentPage, false);
                    parentId = (uint)parentPage.ID;
                    parentUri = PageBL.GetUri(parentPage);
                    break;
                }
            case "files":
                parentType = ResourceBE.ParentType.FILE;
                ResourceBE attachment = GetAttachmentFromUrl(context, true, out parentPage, false, false);
                PageBL.AuthorizePage(DekiContext.Current.User, access, parentPage, false);
                parentId = attachment.ResourceId;
                parentUri = AttachmentBL.Instance.GetUri(attachment);
                break;
            case "users":
                parentType = ResourceBE.ParentType.USER;
                UserBE user = GetUserFromUrlMustExist();

                //TODO: consider anonymous users here!

                //Users may modify their own properties or if admin then other users' properties
                if(user.ID != DekiContext.Current.User.ID) {
                    PermissionsBL.CheckUserAllowed(DekiContext.Current.User, Permissions.ADMIN);
                }

                parentId = user.ID;
                parentUri = UserBL.GetUri(user);
                break;
            case "site":
                parentType = ResourceBE.ParentType.SITE;
                parentId = null;
                parentUri = DekiContext.Current.ApiUri.At("site");

                //Accessing the site properties requires admin rights
                PermissionsBL.CheckUserAllowed(DekiContext.Current.User, Permissions.ADMIN);
                break;
            default:
                throw new DreamInternalErrorException("Unexpected parent resource type in URI: " + context.Uri.Segments[1]);
            }
        }
    }
}

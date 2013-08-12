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

using MindTouch.Deki.Data;
using MindTouch.Deki.Exceptions;
using MindTouch.Deki.Logic;
using MindTouch.Dream;
using MindTouch.Tasking;
using MindTouch.Xml;
using System.Linq;

namespace MindTouch.Deki {
    using Yield = IEnumerator<IYield>;

    public partial class DekiWikiService {

        //--- Features ---
        [DreamFeature("GET:site/services", "Retrieve list of services.")]
        [DreamFeatureParam("type", "{auth, ext}?", "Return only these types of services. Default: Return all")]
        [DreamFeatureParam("limit", "string?", "Maximum number of items to retrieve. Must be a positive number or 'all' to retrieve all items. (default: 100)")]
        [DreamFeatureParam("offset", "int?", "Number of items to skip. Must be a positive number or 0 to not skip any. (default: 0)")]
        [DreamFeatureParam("sortby", "{description, type, init, sid, uri}?", "Sort field. Prefix value with '-' to sort descending. default: No sorting")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        [DreamFeatureStatus(DreamStatus.Ok, "The request completed successfully")]
        [DreamFeatureStatus(DreamStatus.BadRequest, "Invalid input parameter or request body")]
        [DreamFeatureStatus(DreamStatus.Forbidden, "MindTouch API key or Administrator access is required.")]
        public Yield GetServices(DreamContext context, DreamMessage request, Result<DreamMessage> response) {

            bool privateDetails = PermissionsBL.IsUserAllowed(DekiContext.Current.User, Permissions.ADMIN);

            //Private feature requires api-key
            uint totalCount;
            uint queryCount;
            IList<ServiceBE> services = ServiceBL.GetServicesByQuery(context, out totalCount, out queryCount);
            XDoc result = new XDoc("services");
            result.Attr("count", services.Count);
            result.Attr("querycount", queryCount);
            result.Attr("totalcount", totalCount);
            result.Attr("href", DekiContext.Current.ApiUri.At("site", "services"));
            foreach(ServiceBE s in services) {
                result.Add(ServiceBL.GetServiceXmlVerbose(DekiContext.Current.Instance, s, null, privateDetails));
            }
            response.Return(DreamMessage.Ok(result));
            yield break;
        }

        [DreamFeature("GET:site/services/{id}", "Retrieve service.")]
        [DreamFeatureParam("{id}", "string", "identifies a service by ID or ={namespace}")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        [DreamFeatureStatus(DreamStatus.Ok, "The request completed successfully")]
        [DreamFeatureStatus(DreamStatus.BadRequest, "Invalid input parameter or request body")]
        [DreamFeatureStatus(DreamStatus.Forbidden, "MindTouch API key or Administrator access is required.")]
        [DreamFeatureStatus(DreamStatus.NotFound, "Requested service could not be found")]
        public Yield GetServiceById(DreamContext context, DreamMessage request, Result<DreamMessage> response) {

            bool privateDetails = PermissionsBL.IsUserAllowed(DekiContext.Current.User, Permissions.ADMIN);

            //Private feature requires api-key
            var identifier = context.GetParam("id");
            uint serviceId = 0;
            if(identifier.StartsWith("=")) {
                var serviceInfo = DekiContext.Current.Instance.RunningServices[XUri.Decode(identifier.Substring(1))];
                if(serviceInfo != null) {
                    serviceId = serviceInfo.ServiceId;
                }
            } else {
                if(!uint.TryParse(identifier, out serviceId)) {
                    throw new DreamBadRequestException(string.Format("Invalid id '{0}'", identifier));
                }
            }
            ServiceBE service = ServiceBL.GetServiceById(serviceId);
            if(service == null) {
                throw new ServiceNotFoundException(identifier);
            }
            response.Return(DreamMessage.Ok(ServiceBL.GetServiceXmlVerbose(DekiContext.Current.Instance, service, null, privateDetails)));
            yield break;
        }


        [DreamFeature("*:site/services/{id}/proxy//*", "Proxy requets to a service.")]
        [DreamFeatureParam("{id}", "string", "identifies a service by ID or ={namespace}")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        [DreamFeatureStatus(DreamStatus.Ok, "The request completed successfully")]
        [DreamFeatureStatus(DreamStatus.BadRequest, "Invalid input parameter or request body")]
        [DreamFeatureStatus(DreamStatus.Forbidden, "MindTouch API key or Administrator access is required.")]
        [DreamFeatureStatus(DreamStatus.NotFound, "Requested service could not be found")]
        public Yield ProxyToService(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            PermissionsBL.IsUserAllowed(DekiContext.Current.User, Permissions.ADMIN);

            //Private feature requires api-key
            var identifier = context.GetParam("id");
            ServiceRepository.IServiceInfo serviceInfo = null;
            if(identifier.StartsWith("=")) {
                serviceInfo = DekiContext.Current.Instance.RunningServices[XUri.Decode(identifier.Substring(1))];
            } else {
                uint serviceId;
                if(uint.TryParse(identifier, out serviceId)) {
                    serviceInfo = DekiContext.Current.Instance.RunningServices[serviceId];
                } else {
                    throw new DreamBadRequestException(string.Format("Invalid id '{0}'", identifier));
                }
            }
            if(serviceInfo == null) {
                throw new ServiceNotFoundException(identifier);
            }
            var proxyUri = serviceInfo.ServiceUri.At(context.GetSuffixes(UriPathFormat.Original).Skip(1).ToArray());

            yield return context.Relay(Plug.New(proxyUri), request, response);
        }

        [DreamFeature("POST:site/services/{id}", "Restart a service (backwards compatibility)")]
        [DreamFeature("PUT:site/services/{id}", "Update a service")]
        [DreamFeatureParam("{id}", "int", "identifies a service by ID")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        [DreamFeatureStatus(DreamStatus.Ok, "The request completed successfully")]
        [DreamFeatureStatus(DreamStatus.BadRequest, "Invalid input parameter or request body")]
        [DreamFeatureStatus(DreamStatus.Forbidden, "MindTouch API key or Administrator access is required.")]
        [DreamFeatureStatus(DreamStatus.NotFound, "Requested service could not be found")]
        internal Yield PostServicesId(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            ServiceBL.EnsureServiceAdministrationAllowed();
            uint id = context.GetParam<uint>("id");
            ServiceBE service = DbUtils.CurrentSession.Services_GetById(id);
            if(service == null) {
                throw new ServiceNotFoundException(id);
            }
            if(context.Verb.EqualsInvariantIgnoreCase("PUT")) {

                //Modify a service (only with PUT)
                service = ServiceBL.PostServiceFromXml(request.ToDocument(), service);
                response.Return(DreamMessage.Ok(ServiceBL.GetServiceXmlVerbose(DekiContext.Current.Instance, service, null)));
            } else {

                //Backward compatibility: posting an empty document restarts the service
                service = ServiceBL.StartService(service, true, true);
                if(service.ServiceEnabled && (service.Uri == null)) {
                    throw new ServiceSettingsInvalidArgumentException();
                }
                response.Return(DreamMessage.Ok(ServiceBL.GetServiceXmlVerbose(DekiContext.Current.Instance, service, null)));
            }
            yield break;
        }

        [DreamFeature("POST:site/services", "Add a service (backward compatibility: empty body will restart all services)")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        [DreamFeatureStatus(DreamStatus.Ok, "The request completed successfully")]
        [DreamFeatureStatus(DreamStatus.Forbidden, "MindTouch API key or Administrator access is required.")]
        internal Yield PostServices(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            ServiceBL.EnsureServiceAdministrationAllowed();
            if(request.HasDocument && !request.ToDocument().IsEmpty) {

                //Add or Modify a service
                ServiceBE service = ServiceBL.PostServiceFromXml(request.ToDocument(), null);
                response.Return(DreamMessage.Ok(ServiceBL.GetServiceXmlVerbose(DekiContext.Current.Instance, service, null)));
            } else {

                //Backward compatibility: posting an empty document restarts all local services
                XDoc ret = new XDoc("services");
                foreach(ServiceBE service in ServiceBL.RestartServices()) {
                    ret.Add(ServiceBL.GetServiceXml(service, null));
                }
                response.Return(DreamMessage.Ok(ret));
            }
            yield break;
        }

        [DreamFeature("DELETE:site/services/{id}", "Delete a service")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        [DreamFeatureStatus(DreamStatus.Ok, "The request completed successfully")]
        [DreamFeatureStatus(DreamStatus.Forbidden, "MindTouch API key or Administrator access is required.")]
        internal Yield DeleteSiteService(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            ServiceBL.EnsureServiceAdministrationAllowed();

            uint id = context.GetParam<uint>("id");
            ServiceBE service = DbUtils.CurrentSession.Services_GetById(id);
            if(service == null) {
                throw new ServiceNotFoundException(id);
            }

            ServiceBL.DeleteService(service);
            response.Return(DreamMessage.Ok());
            yield break;
        }

        [DreamFeature("POST:site/services/{id}/start", "Start or restart a service")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        [DreamFeatureStatus(DreamStatus.Ok, "The request completed successfully")]
        [DreamFeatureStatus(DreamStatus.Forbidden, "MindTouch API key or Administrator access is required.")]
        internal Yield PostServiceIdStart(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            ServiceBL.EnsureServiceAdministrationAllowed();
            uint id = context.GetParam<uint>("id");
            ServiceBE service = DbUtils.CurrentSession.Services_GetById(id);
            if(service == null) {
                throw new ServiceNotFoundException(id);
            }
            service = ServiceBL.StartService(service, true, true);
            response.Return(DreamMessage.Ok(ServiceBL.GetServiceXmlVerbose(DekiContext.Current.Instance, service, null)));
            yield break;
        }

        [DreamFeature("POST:site/services/{id}/stop", "Stop a service")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        [DreamFeatureStatus(DreamStatus.Ok, "The request completed successfully")]
        [DreamFeatureStatus(DreamStatus.Forbidden, "MindTouch API key or Administrator access is required.")]
        internal Yield PostServiceIdStop(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            ServiceBL.EnsureServiceAdministrationAllowed();
            uint id = context.GetParam<uint>("id");

            ServiceBE service = ServiceBL.StopService(id, false);
            if(service == null) {
                throw new ServiceNotFoundException(id);
            }
            response.Return(DreamMessage.Ok(ServiceBL.GetServiceXmlVerbose(DekiContext.Current.Instance, service, null)));
            yield break;
        }
    }
}

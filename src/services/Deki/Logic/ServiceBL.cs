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
using System.Collections.Specialized;
using System.Diagnostics;
using log4net;
using MindTouch.Collections;
using MindTouch.Deki.Data;
using MindTouch.Deki.Exceptions;
using MindTouch.Dream;
using MindTouch.Tasking;
using MindTouch.Xml;

namespace MindTouch.Deki.Logic {
    public static class ServiceBL {

        //--- Types ---
        private enum ServiceStopType {
            Restart,
            Shutdown,
            Disable
        }

        //--- Constants ---
        public const int BUILT_IN_AUTH_SERVICE_ID = 1;

        // TODO (steveb): we have to remove this constant!
        public const string SID_FOR_LOCAL = "http://services.mindtouch.com/deki/draft/2006/11/dekiwiki";

        //--- Static Fields ---
        private static readonly ILog _log = DekiLogManager.CreateLog();

        //--- Class Methods ---
        public static ServiceBE StartService(ServiceBE service, bool forceRefresh, bool disableOnFailure) {

            // create subordinate request id for service start
            var dreamContext = DreamContext.Current;
            var requestId = dreamContext.GetState<string>(DreamHeaders.DREAM_REQUEST_ID);
            dreamContext.SetState(DreamHeaders.DREAM_REQUEST_ID, requestId + "-service_" + service.Id);

            try {
                var stopwatch = Stopwatch.StartNew();
                service.ServiceLastStatus = string.Empty;
                StopService(service.Id, service, ServiceStopType.Restart);
                DekiContext context = DekiContext.Current;
                bool dirtyServiceEntity = false;
                XUri location;
                ServiceRepository.IServiceInfo serviceInfo = null;
                try {

                    // check if service is local
                    if(service.ServiceLocal) {
                        if(string.IsNullOrEmpty(service.SID)) {
                            throw new Exception("missing SID");
                        }

                        // start service
                        if(IsLocalAuthService(service)) {

                            // this service is the built-in authentication provider; no need to start it
                            location = context.Deki.Self;
                        } else {

                            // convert local service configuration into an xdoc
                            XDoc config = new XDoc("config");
                            foreach(KeyValuePair<string, string> configEntry in ArrayUtil.AllKeyValues(service.Config)) {
                                config.InsertValueAt(configEntry.Key, configEntry.Value);
                            }

                            // if no apikey was provided, create a random one so that CreateService doesn't inject the parent one
                            if(config["apikey"].IsEmpty) {
                                config.Elem("apikey", StringUtil.CreateAlphaNumericKey(16));
                            }

                            // add information for service to callback into deki
                            if(config["uri.deki"].IsEmpty) {
                                config.Elem("uri.deki", context.Deki.Self);
                                config.Elem("wikiid.deki", context.Instance.Id);

                                // Providing master apikey to service for setups that don't use per instance keys
                                config.Elem("apikey.deki", context.Instance.ApiKey.IfNullOrEmpty(context.Deki.MasterApiKey));
                            }

                            // the service location must use the service ID and the instance ID
                            string servicePath = string.Format("services/{0}/{1}", context.Instance.Id, service.Id);
                            _log.DebugFormat("starting service '{0}' at path {1} w/ namespace {2}", service.SID, servicePath,service.Preferences["namespace"]);
                            serviceInfo = context.Instance.CreateLocalService(service, servicePath, config);
                            location = serviceInfo.ServiceUri;
                        }

                        // check if the service uri has changed since last invocation (happens when service is started for the first time or server GUID has changed)
                        if(!service.Uri.EqualsInvariantIgnoreCase(location.ToString())) {
                            dirtyServiceEntity = true;
                            service.Uri = location.ToString();
                        }
                    } else {
                        _log.DebugFormat("registering remote service '{0}'", service.SID);
                        if(string.IsNullOrEmpty(service.Uri)) {
                            throw new Exception("missing URI");
                        }
                        location = new XUri(service.Uri);
                        serviceInfo = context.Instance.RegisterRemoteService(service, location);
                    }

                    // check if service is an Extension service
                    if(service.Type == ServiceType.EXT) {
                        if(service.ServiceLocal) {
                            _log.DebugFormat("registering service '{0}' as extension", service.SID);
                        }
                        ExtensionBL.StartExtensionService(context, service, serviceInfo, forceRefresh);
                    }

                    //Successfully starting a service enables it.
                    if(!service.ServiceEnabled) {
                        dirtyServiceEntity = true;
                        service.ServiceEnabled = true;
                    }
                } catch(Exception e) {
                    dirtyServiceEntity = true;
                    DreamMessage dm = null;
                    if(e is DreamResponseException) {
                        dm = ((DreamResponseException)e).Response;
                        string message = dm.HasDocument ? dm.ToDocument()[".//message"].AsText.IfNullOrEmpty(e.Message) : dm.ToText();
                        service.ServiceLastStatus = string.Format("unable to initialize service ({0})", message);
                    } else {
                        service.ServiceLastStatus = e.GetCoroutineStackTrace();
                    }
                    if(serviceInfo != null) {
                        try {
                            context.Instance.DeregisterService(service.Id);
                        } catch { }
                    }

                    // A service that fails to start becomes disabled if it's started explicitly (not during deki startup)
                    if(disableOnFailure) {
                        service.ServiceEnabled = false;
                    }
                    _log.ErrorExceptionMethodCall(e, "StartService", string.Format("Unable to start local service id '{0}' with SID '{1}' Error: '{2}'", service.Id, service.SID, service.ServiceLastStatus));
                    if(dm != null) {
                        throw new ExternalServiceResponseException(dm);
                    } else {
                        throw;
                    }
                } finally {

                    // don't update remote services that haven't changed
                    if(dirtyServiceEntity) {
                        service = UpdateService(service);
                    }
                }
                stopwatch.Stop();
                _log.InfoFormat("Service '{0}' ({1}) started in {2}ms", service.Description, service.SID, stopwatch.ElapsedMilliseconds);
                return service;
            } finally {

                // restore the request id
                dreamContext.SetState(DreamHeaders.DREAM_REQUEST_ID, requestId);
            }
        }

        public static ServiceBE StopService(uint servicedId, bool dekiShutdown) {
            var service = DbUtils.CurrentSession.Services_GetById(servicedId);
            return StopService(servicedId, service, dekiShutdown ? ServiceStopType.Shutdown : ServiceStopType.Disable);
        }

        public static ServiceBE StopService(ServiceBE service) {
            return StopService(service.Id, service, ServiceStopType.Disable);
        }

        private static ServiceBE StopService(uint serviceId, ServiceBE service, ServiceStopType stopType) {
            if(IsLocalAuthService(serviceId)) {
                return service;
            }
            DekiContext context = DekiContext.Current;
            bool saveBe = false;
            if(service != null && service.ServiceLocal) {

                // local services should have their uri cleared out
                _log.DebugFormat("stopping service '{0}'", service.SID);
                service.Uri = null;
                saveBe = true;
            }
            if(service != null && stopType == ServiceStopType.Disable) {

                // wipe out last error on disable
                service.ServiceLastStatus = null;
                saveBe = true;
            }
            var serviceInfo = context.Instance.RunningServices[serviceId];

            if(serviceInfo == null) {
                if(saveBe) {
                    service = UpdateService(service) ?? service;
                }

                // service is not registered as running, we're done here
                return service;
            }

            try {
                context.Instance.DeregisterService(serviceId);
            } catch(Exception e) {

                // log the error, but ignore it otherwise
                if(service == null) {
                    context.Instance.Log.WarnExceptionMethodCall(e, "StopService", string.Format("Unable to stop {0} service id '{1}'", serviceInfo.IsLocal ? "local" : "remote", serviceId));
                } else {
                    context.Instance.Log.WarnExceptionMethodCall(e, "StopService", string.Format("Unable to stop {0} service id '{1}' with SID '{2}'", serviceInfo.IsLocal ? "local" : "remote", serviceId, service.SID));
                }
            }

            if(service != null && (stopType == ServiceStopType.Disable || saveBe)) {
                service.ServiceEnabled = (stopType != ServiceStopType.Disable);
                service = UpdateService(service) ?? service;
            }
            return service;
        }

        public static void StartServices() {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            IBlockingQueue<ServiceBE> services = new BlockingQueue<ServiceBE>();
            List<ServiceBE> servicesToStart = new List<ServiceBE>(DbUtils.CurrentSession.Services_GetAll());

            // extract all auth services and start them synchronously first
            List<ServiceBE> authServices = servicesToStart.FindAll(service => service.Type == ServiceType.AUTH);
            servicesToStart.RemoveAll(service => service.Type == ServiceType.AUTH);
            foreach(ServiceBE authService in authServices) {
                try {
                    StartService(authService, false, false);
                } catch {
                    //Services started on deki startup do not get disabled if they fail to start
                }
            }

            // start remaining services in parallel
            foreach(ServiceBE service in servicesToStart) {
                if(service.ServiceEnabled) {
                    services.Enqueue(service);
                }
            }
            services.Close();
            List<Result> workers = new List<Result>();
            for(int i = 0; i < 10; i++) {
                workers.Add(Async.ForkThread(() => StartServices_Helper(services), new Result()));
            }
            workers.Join(new Result()).Wait();
            _log.InfoFormat("Services started for instance '{0}' in {1}ms", DekiContext.Current.Instance.Id, stopwatch.ElapsedMilliseconds);
        }

        private static void StartServices_Helper(IBlockingQueue<ServiceBE> services) {
            foreach(ServiceBE service in services) {
                try {
                    StartService(service, false, false);
                } catch {
                    //Services started on deki startup do not get disabled if they fail to start
                }
            }
        }

        public static IEnumerable<ServiceBE> RestartServices() {
            _log.Debug("restarting services");
            var result = new List<ServiceBE>();

            // Note (arnec): we fork in blocking fashion to execute the restart in its own dreamcontext. This is related to canonical uri and cookie issues
            // and should get some more love to determine proper behavior on the Dream side of things.
            Async.Fork(() => {
                foreach(ServiceBE service in DbUtils.CurrentSession.Services_GetAll()) {
                    _log.DebugFormat("restarting '{0}'", service.SID);
                    if(service.ServiceEnabled) {
                        try {
                            result.Add(StartService(service, true, false));
                        } catch {
                            //Services started on deki startup do not get disabled if they fail to start                        
                        }
                    }

                }
            }, new Result()).Wait();
            return result;
        }

        public static void StopServices() {
            foreach(var serviceInfo in DekiContext.Current.Instance.RunningServices) {
                try {
                    _log.DebugFormat("shutting down service '{0}'", serviceInfo.ServiceUri);
                    StopService(serviceInfo.ServiceId, true);
                } catch(Exception e) {
                    _log.DebugFormat("Shutdown of service at '{0}' failed:\r\n{1}", serviceInfo.ServiceUri, e);

                    // try to delete the service in case the stop failed before it could clean up the local uri
                    if(serviceInfo.IsLocal) {
                        var location = Plug.New(serviceInfo.ServiceUri);
                        if(location != null) {
                            location.DeleteAsync().Block();
                        }
                    }
                }
            }
        }

        public static ServiceBE GetServiceById(uint serviceid) {
            return DbUtils.CurrentSession.Services_GetById(serviceid);
        }

        public static IList<ServiceBE> GetServicesByQuery(DreamContext context, out uint totalCount, out uint queryCount) {
            ServiceType filterType = context.GetParam("type", ServiceType.UNDEFINED);
            uint limit, offset;
            SortDirection sortDir;
            string sortFieldString;
            Utils.GetOffsetAndCountFromRequest(context, 100, out limit, out offset, out sortDir, out sortFieldString);

            // Attempt to read the sort field.  If a parsing error occurs, default to undefined.
            ServicesSortField sortField = ServicesSortField.UNDEFINED;
            if(!String.IsNullOrEmpty(sortFieldString)) {
                try { sortField = SysUtil.ChangeType<ServicesSortField>(sortFieldString); } catch { }
            }
            return DbUtils.CurrentSession.Services_GetByQuery(filterType == ServiceType.UNDEFINED ? null : filterType.ToString(), sortDir, sortField, offset, limit, out totalCount, out queryCount);
        }

        public static ServiceBE RetrieveLocalAuthService() {

            //TODO (MaxM): This can be fixed by changing the service_type for the local auth service to uniquely find it.

            ServiceBE service = GetServiceById(BUILT_IN_AUTH_SERVICE_ID);
            if(!StringUtil.EqualsInvariantIgnoreCase(service.SID.Trim(), SID_FOR_LOCAL)) {
                throw new ServiceSIDExpectedFatalException(SID_FOR_LOCAL);
            }
            return service;
        }

        public static bool IsLocalAuthService(ServiceBE service) {
            if(service == null)
                return false;

            return service.Id == BUILT_IN_AUTH_SERVICE_ID;
        }

        public static bool IsLocalAuthService(uint serviceId) {
            return serviceId == BUILT_IN_AUTH_SERVICE_ID;
        }

        public static void EnsureServiceAdministrationAllowed() {
            if(DekiContext.Current.Instance.LimitedAdminPermissions) {
                throw new ServiceAdministrationNotImplementedExceptionException();
            }
        }

        public static ServiceBE UpdateService(ServiceBE service) {
            DbUtils.CurrentSession.Services_Delete(service.Id);
            uint serviceId = DbUtils.CurrentSession.Services_Insert(service);

            // reload the service
            return DbUtils.CurrentSession.Services_GetById(serviceId);
        }

        public static void DeleteService(ServiceBE service) {
            if(IsLocalAuthService(service)) {
                throw new ServiceCannotDeleteAuthInvalidOperationException();
            }

            service = StopService(service);
            DbUtils.CurrentSession.Services_Delete(service.Id);
            DbUtils.CurrentSession.Users_UpdateServicesToLocal(service.Id);
            DbUtils.CurrentSession.Groups_UpdateServicesToLocal(service.Id);
        }

        public static ServiceBE PostServiceFromXml(XDoc serviceDoc, ServiceBE serviceToProcess) {
            uint? serviceId;
            string SID, description, uri;
            bool? statusEnabled;
            bool? localInit;
            ServiceType? type;
            NameValueCollection config, preferences;

            ParseServiceXml(serviceDoc, out serviceId, out SID, out description, out statusEnabled, out localInit, out type, out uri, out config, out preferences);

            //new service
            if(serviceToProcess == null && (serviceId == null || serviceId == 0)) {

                //convert XML input to a service object
                serviceToProcess = NewServiceFromXml(serviceDoc, SID, description, statusEnabled, localInit, type, uri, config, preferences);

                //insert the service
                serviceId = DbUtils.CurrentSession.Services_Insert(serviceToProcess);

                // reload the service
                serviceToProcess = DbUtils.CurrentSession.Services_GetById(serviceId.Value);

            } else {

                //Validate logic of given xml
                if(ServiceBL.IsLocalAuthService(serviceToProcess)) {
                    throw new ServiceCannotModifyBuiltInAuthInvalidOperationException();
                }

                if(((uri ?? string.Empty) != string.Empty) && (localInit ?? false)) {
                    throw new ServiceCannotSetLocalUriInvalidOperationException();
                }

                //Stop the service before making any changes
                serviceToProcess = StopService(serviceToProcess);

                //convert XML input to a service object. 
                serviceToProcess = UpdateServiceFromParsedXml(serviceToProcess, SID, description, statusEnabled, localInit, type, uri, config, preferences);

                //Update existing service
                serviceToProcess = UpdateService(serviceToProcess);
            }

            return serviceToProcess;
        }

        private static ServiceBE UpdateServiceFromParsedXml(ServiceBE service, string SID, string description, bool? statusEnabled, bool? localInit,
                                                            ServiceType? type, string uri, NameValueCollection config, NameValueCollection preferences) {

            //Modify only changed (non-null) values

            if(SID != null) {
                service.SID = SID;
            }

            if(service.Description != null) {
                service.Description = description;
            }

            if(statusEnabled != null) {
                service.ServiceEnabled = statusEnabled.Value;
            }

            if(localInit != null) {
                service.ServiceLocal = localInit.Value;
            }

            if(type != null && type.Value != ServiceType.UNDEFINED) {
                service.Type = type.Value;
            }

            if(uri != null) {
                service.Uri = uri;
            }

            if(config != null) {
                service.Config = config;
            }

            if(preferences != null) {
                service.Preferences = preferences;
            }

            return service;
        }

        private static ServiceBE NewServiceFromXml(XDoc serviceDoc, string SID, string description, bool? statusEnabled,
                                            bool? localInit, ServiceType? type, string uri, NameValueCollection config, NameValueCollection preferences) {

            ServiceBE service = new ServiceBE();

            if(string.IsNullOrEmpty(SID) && (localInit ?? true)) {
                throw new ServiceMissingCreateSIDInvalidOperationException();
            }

            if(type == null || type == ServiceType.UNDEFINED) {
                throw new ServiceMissingCreateTypeInvalidArgumentException();
            }

            service.SID = SID ?? string.Empty;
            service.Description = description ?? string.Empty;
            service.ServiceEnabled = statusEnabled ?? true;
            service.ServiceLocal = localInit ?? true;
            service.Type = type.Value;
            service.Uri = uri ?? string.Empty;
            service.Preferences = preferences ?? new NameValueCollection();
            service.Config = config ?? new NameValueCollection();
            service.ServiceLastEdit = DateTime.UtcNow;

            return service;
        }

        private static void ParseServiceXml(XDoc serviceDoc, out uint? serviceId, out string SID, out string description, out bool? serviceEnabled,
                                            out bool? localInit, out ServiceType? type, out string uri,
                                            out NameValueCollection config, out NameValueCollection preferences) {

            /*
             <service id=x>
                <sid/>
                <type/> // authentication(auth) or extension (ext)
                <description/>
                <status/>
                <init/> // native or remote
                <uri/> // for remote
                <config>
                    <value key="foo">bar</value>
                </config>
             */

            config = null;
            preferences = null;
            serviceId = null;
            type = ServiceType.UNDEFINED;
            localInit = null;
            uri = null;

            if(!serviceDoc["@id"].IsEmpty) {
                serviceId = serviceDoc["@id"].AsUInt;
            }

            string typeStr = serviceDoc["type"].AsText ?? string.Empty;
            switch(typeStr.ToLowerInvariant()) {
            case "auth":
            case "authentication":
                type = ServiceType.AUTH;
                break;

            case "ext":
            case "extension":
                type = ServiceType.EXT;
                break;

            default:
                throw new ServiceInvalidUpdateTypeInvalidArgumentException();
            }

            string initStr = serviceDoc["init"].AsText ?? serviceDoc["initialization"].AsText ?? string.Empty;
            switch(initStr.ToLowerInvariant()) {
            case "native":
                localInit = true;
                break;

            case "remote":
                localInit = false;
                break;
            default:
                throw new ServiceUnexpectedInitInvalidOperationException();
            }

            SID = serviceDoc["sid"].AsText;
            description = serviceDoc["description"].AsText;
            if(string.IsNullOrEmpty(description)) {
                throw new ServiceMissingDescriptionInvalidArgumentException();
            }

            switch((serviceDoc["status"].AsText ?? string.Empty).ToLowerInvariant()) {
            case "enabled":
                serviceEnabled = true;
                break;
            case "disabled":
                serviceEnabled = false;
                break;
            case "":
                serviceEnabled = null;
                break;
            default:
                throw new ServiceInvalidStatusInvalidArgumentException();
            }

            if(!string.IsNullOrEmpty(serviceDoc["uri"].AsText)) {
                XUri tempUri = serviceDoc["uri"].AsUri;
                if(tempUri != null) {
                    uri = tempUri.ToString();
                }
            }

            if(!serviceDoc["preferences"].IsEmpty) {
                preferences = new NameValueCollection();

                foreach(XDoc pref in serviceDoc["preferences/value"]) {
                    string key = pref["@key"].AsText ?? string.Empty;
                    string val = pref.AsText ?? string.Empty;

                    if(!string.IsNullOrEmpty(key)) {
                        preferences.Add(key, val);
                    }
                }
            }

            if(!serviceDoc["config"].IsEmpty) {
                config = new NameValueCollection();

                foreach(XDoc cfg in serviceDoc["config/value"]) {
                    string key = cfg["@key"].AsText ?? string.Empty;
                    string val = cfg.AsText ?? string.Empty;

                    if(!string.IsNullOrEmpty(key)) {
                        config.Add(key, val);
                    }
                }
            }
        }

        #region XML Helpers
        public static XDoc GetServiceXml(ServiceBE service, string relation) {
            XDoc serviceXml = new XDoc(string.IsNullOrEmpty(relation) ? "service" : "service." + relation);
            serviceXml.Attr("id", service.Id);
            serviceXml.Attr("href", DekiContext.Current.ApiUri.At("site", "services", service.Id.ToString()));
            return serviceXml;
        }

        public static XDoc GetServiceXmlVerbose(DekiInstance instance, ServiceBE service, string relation) {
            return GetServiceXmlVerbose(DekiContext.Current.Instance, service, relation, true);
        }

        public static XDoc GetServiceXmlVerbose(DekiInstance instance, ServiceBE service, string relation, bool privateDetails) {
            XDoc serviceXml = GetServiceXml(service, relation);

            serviceXml.Start("sid").Value(service.SID ?? string.Empty).End();

            serviceXml.Start("uri").Value(XUri.TryParse(service.Uri)).End();

            serviceXml.Start("type").Value(service.Type.ToString().ToLowerInvariant()).End();
            serviceXml.Start("description").Value(service.Description ?? "").End();

            serviceXml.Elem("date.modified", service.ServiceLastEdit);
            serviceXml.Elem("status", service.ServiceEnabled ? "enabled" : "disabled");

            serviceXml.Start("local").Value(service.ServiceLocal).Attr("deprecated", true).End();
            serviceXml.Elem("init", service.ServiceLocal ? "native" : "remote");
            var serviceInfo = instance.RunningServices[service.Id];
            if(serviceInfo != null && !string.IsNullOrEmpty(serviceInfo.Namespace)) {
                serviceXml.Elem("namespace", serviceInfo.Namespace);
            }
            if(privateDetails) {
                serviceXml.Elem("lasterror", service.ServiceLastStatus ?? "");

                serviceXml.Start("config");

                foreach(string key in service.Config.AllKeys)
                    serviceXml.Start("value").Attr("key", key).Value(service.Config[key]).End();
                serviceXml.End();

                serviceXml.Start("preferences");
                foreach(string key in service.Preferences.AllKeys)
                    serviceXml.Start("value").Attr("key", key).Value(service.Preferences[key]).End();
                serviceXml.End();
            }
            return serviceXml;
        }
        #endregion
    }
}

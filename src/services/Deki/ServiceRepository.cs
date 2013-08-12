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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using log4net;
using MindTouch.Deki.Data;
using MindTouch.Dream;
using MindTouch.Xml;

namespace MindTouch.Deki {
    public class ServiceRepository : IEnumerable<ServiceRepository.IServiceInfo> {

        //--- Types ---

        // Note (arnec): This is an interface so that ServiceInfo can be private and allow private access to its members by the repository
        public interface IServiceInfo {

            //--- Properties ---
            uint ServiceId { get; }
            XUri ServiceUri { get; }
            bool IsLocal { get; }
            bool IsExtensionService { get; }
            ExtensionInfo Extension { get; }
            string Namespace { get; }
        }

        private class ServiceInfo : IServiceInfo {

            //--- Fields ---
            private ExtensionInfo _extension;

            //--- Constructors ---
            public ServiceInfo(ServiceRepository owner, uint serviceId, XUri serviceUri, bool isLocal) {
                Owner = owner;
                ServiceId = serviceId;
                ServiceUri = serviceUri;
                IsLocal = isLocal;
            }

            //--- Properties ---
            public ServiceRepository Owner { get; private set; }
            public bool IsExtensionService { get { return Extension != null; } }
            public string Namespace { get; set; }
            public uint ServiceId { get; set; }
            public XUri ServiceUri { get; set; }
            public bool IsLocal { get; set; }

            public ExtensionInfo Extension {
                get {
                    if(_extension == null) {
                        lock(Owner._servicesById) {
                            if(_extension == null) {
                                var extension = new ExtensionInfo();
                                Owner._extensionServices.Add(this);
                                _extension = extension;
                            }
                        }
                    }
                    return _extension;
                }
            }
        }

        public class ExtensionInfo {

            //--- Fields ---
            private readonly Dictionary<string, string> _preferences = new Dictionary<string, string>();
            public IEnumerable<ExtensionFunctionInfo> Functions;
            public XDoc Manifest;
            public bool IsProtected;

            //--- Methods ---
            public void SetPreference(string key, string value) {
                if(string.IsNullOrEmpty(value)) {
                    return;
                }
                lock(_preferences) {
                    _preferences[key] = value;
                }
            }

            public string GetPreference(string key) {
                string value;
                _preferences.TryGetValue(key, out value);
                return value;
            }
        }

        public class ExtensionFunctionInfo {

            //--- Fields ---
            public readonly string Name;
            public readonly XUri Uri;

            //--- Constructors ---
            public ExtensionFunctionInfo(string name, XUri uri) {
                Name = name;
                Uri = uri;
            }
        }

        //--- Static Fields ---
        private static readonly ILog _log = LogUtils.CreateLog();

        //--- Fields ---

        // Note (arnec): Always lock on _servicesById before acccessing any of the lookups
        private readonly Dictionary<uint, ServiceInfo> _servicesById = new Dictionary<uint, ServiceInfo>();
        private readonly Dictionary<XUri, ServiceInfo> _servicesByUri = new Dictionary<XUri, ServiceInfo>();

        // Note (arnec): We store all services with the same namespace in the lookup, so that removal of one
        // automatically promotes the next as the one that can be looked up by name
        private readonly Dictionary<string, List<ServiceInfo>> _servicesByNamespace = new Dictionary<string, List<ServiceInfo>>();
        private readonly HashSet<ServiceInfo> _extensionServices = new HashSet<ServiceInfo>();

        // --- Properties ---
        public IServiceInfo this[string serviceNamespace] {
            get {
                lock(_servicesById) {
                    var info = GetNamespaceServices(serviceNamespace);
                    return info != null ? info.FirstOrDefault() : null;
                }
            }
        }

        public IServiceInfo this[uint serviceId] {
            get {
                lock(_servicesById) {
                    ServiceInfo info;
                    _servicesById.TryGetValue(serviceId, out info);
                    return info;
                }
            }
        }

        public IServiceInfo this[XUri serviceUri] {
            get {
                lock(_servicesById) {
                    ServiceInfo info;
                    _servicesByUri.TryGetValue(serviceUri, out info);
                    return info;
                }
            }
        }

        public IEnumerable<IServiceInfo> ExtensionServices {
            get {
                lock(_servicesById) {
                    return _extensionServices.ToArray();
                }
            }
        }

        //--- Methods ---
        public IServiceInfo RegisterService(ServiceBE service, XUri serviceUri, bool isLocal) {
            var serviceInfo = new ServiceInfo(this, service.Id, serviceUri, isLocal);
            lock(_servicesById) {
                _servicesById[service.Id] = serviceInfo;
                _servicesByUri[serviceUri] = serviceInfo;
                RegisterNamespace(serviceInfo, service.Preferences["namespace"]);
            }
            return serviceInfo;
        }

        public void RegisterNamespace(IServiceInfo serviceInfo, string serviceNamespace) {
            if(string.IsNullOrEmpty(serviceNamespace)) {
                return;
            }
            serviceNamespace = serviceNamespace.Trim();
            if(string.IsNullOrEmpty(serviceNamespace)) {
                return;
            }
            if(serviceNamespace.EqualsInvariant(serviceInfo.Namespace)) {
                return;
            }
            var info = serviceInfo as ServiceInfo;
            if(info == null || info.Owner != this) {
                _log.WarnFormat("The service {0} does not belong to this repository", serviceInfo);
                return;
            }
            lock(_servicesById) {
                if(!string.IsNullOrEmpty(info.Namespace)) {
                    var oldRegistration = GetNamespaceServices(info.Namespace);
                    if(oldRegistration != null) {
                        oldRegistration.Remove(info);
                        if(!oldRegistration.Any()) {
                            _servicesByNamespace.Remove(info.Namespace);
                        }
                    }
                }
                var newRegistration = GetNamespaceServices(serviceNamespace);
                if(newRegistration == null) {
                    newRegistration = new List<ServiceInfo>();
                    _servicesByNamespace[serviceNamespace] = newRegistration;
                }
                newRegistration.Add(info);
                newRegistration.Sort((x, y) => (int)((long)x.ServiceId - (long)y.ServiceId));
                info.Namespace = serviceNamespace;
            }
        }

        private List<ServiceInfo> GetNamespaceServices(string serviceNamespace) {

            // Note (arnec): assumes that method is always called in locked context
            List<ServiceInfo> info;
            _servicesByNamespace.TryGetValue(serviceNamespace, out info);
            return info;
        }

        public void DeregisterService(IServiceInfo serviceInfo) {
            lock(_servicesById) {
                _servicesById.Remove(serviceInfo.ServiceId);
                _servicesByUri.Remove(serviceInfo.ServiceUri);
                if(!string.IsNullOrEmpty(serviceInfo.Namespace)) {
                    var registration = GetNamespaceServices(serviceInfo.Namespace);
                    registration.Remove(serviceInfo as ServiceInfo);
                    if(!registration.Any()) {
                        _servicesByNamespace.Remove(serviceInfo.Namespace);
                    }
                }
                var info = serviceInfo as ServiceInfo;
                if(info != null) {
                    _extensionServices.Remove(info);
                }
            }
        }

        public void Clear() {
            lock(_servicesById) {
                _servicesById.Clear();
                _servicesByNamespace.Clear();
                _servicesByUri.Clear();
                _extensionServices.Clear();
            }
        }

        public IEnumerator<IServiceInfo> GetEnumerator() {
            lock(_servicesById) {
                return _servicesById.Values.Select(x => (IServiceInfo)x).ToList().GetEnumerator();
            }
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }
    }
}
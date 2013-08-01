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
using System.IO;
using MindTouch.Dream;
using MindTouch.Tasking;
using MindTouch.Xml;

namespace MindTouch.Deki.WikiManagement {
    internal class RemoteInstanceManager : InstanceManager {

        //--- Constants ---
        private const int HOST_WIKIID_TIMEOUT = 5 * 60;
        private static readonly TimeSpan TIMEOUT = TimeSpan.FromSeconds(1);

        //--- Fields ---
        private readonly Plug _directory;
        private readonly Dictionary<string, Tuplet<string, DateTime>> _hostsToWikiIds = new Dictionary<string, Tuplet<string, DateTime>>();
        private readonly string _tempPath;

        //--- Constructors ---
        public RemoteInstanceManager(DekiWikiService dekiService, TaskTimerFactory timerFactory, XUri directoryUri, string tempPath) : base(dekiService, timerFactory) {

            // validate temp folder
            _tempPath = tempPath;
            if(!Directory.Exists(_tempPath)) {
                throw new ArgumentException("temp folder does not exist", "tempPath");
            }

            // check remote directory
            _directory = Plug.New(directoryUri);
            var testMsg = _directory.GetAsync().Wait();
            if(!testMsg.IsSuccessful) {
                _log.WarnFormat("Error validating remote deki portal service at '{0}'", directoryUri);
            }
        }

        //--- Properties ---
        public override bool IsCloudManager { get { return true; } }

        //--- Methods ---
        public override XDoc GetGlobalConfig() {
            return _dekiService.Config["wikis/globalconfig"];
        }

        protected override XDoc GetConfigForWikiId(string wikiId) {
            if(string.IsNullOrEmpty(wikiId)) {
                return null;
            }
            var p = DirectoryGetConfigForWikiId(wikiId);
            if(p.IsSuccessful && p.ContentType.IsXml) {
                var configDoc = XDoc.Empty;
                var wikiDoc = p.ToDocument();
                configDoc = wikiDoc["config"];
                foreach(XDoc hostDoc in configDoc["host"]) {
                    string host = hostDoc.AsText;
                    lock(_hostsToWikiIds) {
                        _hostsToWikiIds[host] = new Tuplet<string, DateTime>(wikiId, DateTime.UtcNow);
                    }
                }
                var updated = wikiDoc["date.updated"].AsDate ?? DateTime.MinValue;
                if(updated != DateTime.MinValue) {
                    configDoc.Attr("updated", updated);
                }
                return configDoc;
            }
            _log.WarnFormat("Unable to lookup config for site '{0}'. Return status: '{1}'", wikiId, p.Status);
            return null;
        }

        protected override string GetWikiIdByHostname(string hostname) {
            Tuplet<string, DateTime> wikiId;
            lock(_hostsToWikiIds) {
                _hostsToWikiIds.TryGetValue(hostname, out wikiId);

                // Associations between a hostname and a wiki id should timeout at least every 5 minutes to allow hostnames to be switched.
                if(wikiId != null) {
                    TimeSpan timeSpanSinceLastCheck = DateTime.UtcNow - wikiId.Item2;
                    if(timeSpanSinceLastCheck > InactiveInstanceTimeOut || timeSpanSinceLastCheck > TimeSpan.FromSeconds(HOST_WIKIID_TIMEOUT)) {
                        _hostsToWikiIds.Remove(hostname);
                        wikiId = null;
                    }
                }
            }
            if(wikiId == null) {
                DreamMessage p = DirectoryGetWikiIdByHostname(hostname);
                if(p.IsSuccessful) {
                    XDoc wikiDoc = p.ToDocument();
                    wikiId = new Tuplet<string, DateTime>(wikiDoc["@id"].AsText, DateTime.UtcNow);
                    lock(_hostsToWikiIds) {
                        _hostsToWikiIds[hostname] = wikiId;
                    }
                } else {
                    _log.WarnFormat("unable find a wikiid for hostname '{0}'", hostname);
                }
            }
            return wikiId == null ? null : wikiId.Item1;
        }

        protected override void ShutdownInstance(string wikiid, DekiInstance instance) {
            lock(instance) {

                // On instance shutdown, remove host to wikiId association.
                lock(_hostsToWikiIds) {
                    foreach(var hostDoc in instance.Config["host"]) {
                        var host = hostDoc.AsText;
                        _hostsToWikiIds.Remove(host);
                    }
                }
                base.ShutdownInstance(wikiid, instance);
            }
        }

        protected override ILicenseController GetLicenseController(string wikiId, Plug licenseStoragePlug) {
            return new RemoteLicenseController(wikiId, licenseStoragePlug, DirectoryGetLicense, _loggerRepositories[wikiId].Get<RemoteLicenseController>());
        }

        private DreamMessage DirectoryGetLicense(string wikiId) {
            return CachedRequest(_directory.At(wikiId).At("license"), wikiId + "-license.xml");
        }

        private DreamMessage DirectoryGetConfigForWikiId(string wikiId) {
            return CachedRequest(_directory.At(wikiId), wikiId + "-config.xml");
        }

        private DreamMessage DirectoryGetWikiIdByHostname(string hostname) {
            return CachedRequest(_directory.At("=" + hostname), XUri.EncodeSegment(hostname) + "-wikiid.xml");
        }

        // CLEANUP (CL-442): pull this out into its own interface, but for that RemoteInstanceManager must be autofac controlled
        private DreamMessage CachedRequest(Plug plug, string filename) {
            var fullpath = Path.Combine(_tempPath, filename);

            // attempt to fecth a response from the portal
            var response = plug.WithTimeout(TIMEOUT).GetAsync().Wait();

            // check if we got a positive response
            if(response.IsSuccessful) {

                // cache response on disk
                try {
                    response.ToDocument().Save(fullpath);
                } catch(Exception e) {
                    _log.Warn(string.Format("Unable to write file '{0}'.", fullpath), e);

                    // just in case, let's remove the file
                    try {
                        File.Delete(fullpath);
                    } catch { }
                }
                return response;
            }

            // check if we got a negative response
            if(response.Status == DreamStatus.Gone) {

                // cache response on disk
                try {
                    File.Delete(fullpath);
                } catch { }
                return response;
            }

            // no response, let's check if we have a cached response on disk
            try {
                var xml = XDocFactory.LoadFrom(fullpath, MimeType.XML);
                _log.WarnFormat("Portal did not respond. Using previously cached response instead from {0}", filename);
                return DreamMessage.Ok(xml);
            } catch { }
            return response;
        }
    }
}

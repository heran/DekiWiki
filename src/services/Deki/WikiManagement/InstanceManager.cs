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
using log4net;
using MindTouch.Deki.Util;
using MindTouch.Dream;
using MindTouch.Tasking;
using MindTouch.Web;
using MindTouch.Xml;
using MindTouch.Extensions.Time;

namespace MindTouch.Deki.WikiManagement {

    // Note (arnec): InstanceManager is internal because it directly affects license management and since we rely on its subclassing to 
    // provide local and cloud behaviors, it needs to be protected from subclassing in unsigned assemblies
    internal abstract class InstanceManager {

        //--- Constants ---
        public const string DEFAULT_WIKI_ID = "default";

        //--- Class fields ---
        protected static readonly ILog _log = LogUtils.CreateLog();

        //--- Class methods ---
        public static InstanceManager New(DekiWikiService dekiService, TaskTimerFactory timerFactory) {
            InstanceManager mgr;
            var srcString = dekiService.Config["wikis/@src"].AsText;
            if(!string.IsNullOrEmpty(srcString)) {
                XUri remoteDirUri;
                if(!XUri.TryParse(srcString, out remoteDirUri)) {

                    //TODO: build a specialized exception out of this
                    throw new ApplicationException(string.Format("Configuration is not valid. wikis/@src ({0})is not a valid url!", srcString));
                }
                mgr = new RemoteInstanceManager(dekiService, timerFactory, remoteDirUri, dekiService.TempPath);
            } else {
                mgr = new LocalInstanceManager(dekiService, timerFactory);
            }

            mgr._maxInstances = dekiService.Config["wikis/@max"].AsInt ?? int.MaxValue;
            var timeoutSecs = dekiService.Config["wikis/@ttl"].AsDouble;
            if(timeoutSecs == null || timeoutSecs == 0) {
                mgr._inactiveInstanceTimeOut = TimeSpan.MaxValue;
            } else {
                mgr._inactiveInstanceTimeOut = TimeSpan.FromSeconds(timeoutSecs.Value);
            }
            var retryInterval = dekiService.Config["wikis/@retry-interval"].AsDouble ?? 10;
            mgr._abandonedInstanceRetryInterval = TimeSpan.FromSeconds(retryInterval);
            mgr._minInstanceIdletime = TimeSpan.FromSeconds(dekiService.Config["wikis/@idletime"].AsDouble ?? 60);
            return mgr;
        }

        //--- Fields ---
        private readonly Dictionary<string, DekiInstance> _instances = new Dictionary<string, DekiInstance>();
        private readonly Dictionary<string, TaskTimer> _instanceExpireTimers = new Dictionary<string, TaskTimer>();
        protected readonly Dictionary<string, ILoggerRepository> _loggerRepositories = new Dictionary<string, ILoggerRepository>();
        protected readonly DekiWikiService _dekiService;
        protected readonly TaskTimerFactory _timerFactory;
        private int _maxInstances = int.MaxValue;
        private TimeSpan _inactiveInstanceTimeOut;
        private TimeSpan _abandonedInstanceRetryInterval;
        private TimeSpan _minInstanceIdletime;

        //--- Constructors ---
        protected InstanceManager(DekiWikiService dekiService, TaskTimerFactory timerFactory) {
            _dekiService = dekiService;
            _timerFactory = timerFactory;
        }

        //--- Abstract methods ---
        public abstract XDoc GetGlobalConfig();
        protected abstract XDoc GetConfigForWikiId(string wikiId);
        protected abstract string GetWikiIdByHostname(string hostname);
        protected abstract ILicenseController GetLicenseController(string wikiId, Plug licenseStoragePlug);

        //--- Properties ---
        protected TimeSpan InactiveInstanceTimeOut { get { return _inactiveInstanceTimeOut; } }
        public virtual bool IsCloudManager { get { return false; } }
        public uint InstancesRunning {
            get {
                lock(_instances) {
                    return (uint)_instances.Count;
                }
            }
        }

        public IEnumerable<KeyValuePair<string, string>> InstanceStatuses {
            get {
                lock(_instances) {
                    return (from kvp in _instances
                            let status = kvp.Value != null ? kvp.Value.Status.ToString().ToLower() : "notrunning"
                            select new KeyValuePair<string, string>(kvp.Key, status)).ToArray();
                }
            }
        }

        //--- Methods ---
        public IEnumerable<KeyValuePair<string, string>> GetGlobalServices() {
            var config = new List<KeyValuePair<string, string>> {
                new KeyValuePair<string, string>("services/mailer", _dekiService.Mailer.Uri.AsPublicUri().ToString()), 
                new KeyValuePair<string, string>("services/luceneindex", _dekiService.LuceneIndex.Uri.AsPublicUri().ToString()), 
                new KeyValuePair<string, string>("services/pagesubscription", _dekiService.PageSubscription.Uri.AsPublicUri().ToString()),
                new KeyValuePair<string, string>("services/packageupdater", _dekiService.PackageUpdater.Uri.AsPublicUri().ToString())
            };
            return config;
        }

        public DekiInstance GetWikiInstance(DreamMessage request, bool startIfNotRunning) {
            if(request == null) {
                return null;
            }
            string hostname = string.Empty;
            bool wikiIdFromHeader = true;
            string wikiId = null;
            string publicUriFromHeader = null;
            var wikiIdentityHeader = HttpUtil.ParseNameValuePairs(request.Headers[DekiWikiService.WIKI_IDENTITY_HEADERNAME] ?? "");
            wikiIdentityHeader.TryGetValue("id", out wikiId);
            wikiIdentityHeader.TryGetValue("uri", out publicUriFromHeader);
            if(string.IsNullOrEmpty(wikiId)) {

                // get requested hostname and normalize
                wikiIdFromHeader = false;
                hostname = request.Headers.Host;
                if(!string.IsNullOrEmpty(hostname)) {
                    if(hostname.EndsWith(":80")) {
                        hostname = hostname.Substring(0, hostname.Length - 3);
                    }
                    hostname = hostname.Trim().ToLowerInvariant();
                }
                wikiId = GetWikiIdByHostname(hostname);
            }
            var dekiInstance = GetWikiInstance(wikiId);
            if(!startIfNotRunning && dekiInstance == null) {
                return null;
            }

            //If an instance doesn't exist or if it was last updated over x minutes ago, refetch the instance config to check for status changes
            if(dekiInstance == null || (InactiveInstanceTimeOut != TimeSpan.MaxValue && dekiInstance.InstanceLastUpdateTime.Add(InactiveInstanceTimeOut) < DateTime.UtcNow)) {
                _log.DebugFormat("trying to start instance '{0}'", wikiId);
                XDoc instanceConfig = XDoc.Empty;
                try {
                    instanceConfig = GetConfigForWikiId(wikiId);
                    if((instanceConfig == null || instanceConfig.IsEmpty) && dekiInstance == null) {

                        // NOTE (maxm): these exceptions don't come from resources and are not mapped because no DekiContext exists.
                        if(wikiIdFromHeader) {
                            throw new DreamAbortException(new DreamMessage(DreamStatus.Gone, null, MimeType.TEXT, string.Format("No wiki exists for provided wikiId '{0}'", wikiId)));
                        }
                        throw new DreamAbortException(new DreamMessage(DreamStatus.Gone, null, MimeType.TEXT, string.Format("No wiki exists at host '{0}'", hostname)));
                    }
                } catch(DreamAbortException e) {
                    if(e.Response.Status == DreamStatus.Gone) {
                        ShutdownInstance(wikiId);
                        throw;
                    }
                    if(dekiInstance == null) {
                        throw;
                    }
                } catch {
                    if(dekiInstance == null) {
                        throw;
                    }
                } finally {
                    if(dekiInstance != null)
                        dekiInstance.InstanceLastUpdateTime = DateTime.UtcNow;
                }

                //If a wiki already exists, shut it down if it was updated since it was last created
                if(dekiInstance != null && dekiInstance.InstanceCreationTime < (instanceConfig["@updated"].AsDate ?? DateTime.MinValue)) {
                    ShutdownInstance(wikiId);
                    dekiInstance = null;
                }

                // create instance if none exists
                if(dekiInstance == null) {
                    dekiInstance = CreateWikiInstance(wikiId, instanceConfig);
                }
            }
            if(InactiveInstanceTimeOut != TimeSpan.MaxValue) {
                lock(_instances) {
                    TaskTimer timer;
                    if(_instanceExpireTimers.TryGetValue(wikiId, out timer)) {
                        timer.Change(InactiveInstanceTimeOut, TaskEnv.None);
                    }
                }
            }
            if(wikiIdFromHeader) {

                // the request host does not represent a valid host for public uri generation, so we need to alter the context
                XUri publicUriOverride;
                if(string.IsNullOrEmpty(publicUriFromHeader) || !XUri.TryParse(publicUriFromHeader, out publicUriOverride)) {
                    _log.DebugFormat("no public uri provided in wiki header, using canonical uri");
                    publicUriOverride = dekiInstance.CanonicalUri;
                }

                // Note (arnec): Propagating a much hard-coded assumption, i.e. that the Api for any Deki instance can be accessed
                // at the instances' canonical uri plus @api
                publicUriOverride = publicUriOverride.At("@api");
                _log.DebugFormat("switching public uri from {0} to {1} for request", DreamContext.Current.PublicUri, publicUriOverride);
                DreamContext.Current.SetPublicUriOverride(publicUriOverride);
            }
            dekiInstance.InstanceLastAccessedTime = DateTime.UtcNow;
            return dekiInstance;
        }

        public bool ShutdownCurrentInstance() {
            return ShutdownInstance(DekiContext.Current.Instance.Id);
        }

        public virtual void Shutdown() {
            lock(_instances) {
                foreach(string wikiId in new List<string>(_instances.Keys)) {
                    ShutdownInstance(wikiId);
                }
            }
            DreamContext.Current.SetState<DekiContext>(null);
            _instances.Clear();
        }

        protected DekiInstance GetWikiInstance(string wikiId) {
            if(string.IsNullOrEmpty(wikiId)) {
                return null;
            }
            DekiInstance di;
            lock(_instances) {
                _log.DebugFormat("retrieving instance for wiki id '{0}'", wikiId);
                if(_instances.TryGetValue(wikiId, out di) && di.Status == DekiInstanceStatus.ABANDONED) {
                    var retryTime = di.InstanceCreationTime.Add(_abandonedInstanceRetryInterval);
                    if(retryTime < DateTime.UtcNow) {
                        di.Log.DebugFormat("instance '{0}' has been abandoned for more than {1:0} seconds, preparing to retry creation", di.Id, _abandonedInstanceRetryInterval.TotalSeconds);
                        di = null;
                        _instances.Remove(wikiId);
                    } else {
                        di.Log.DebugFormat("instance '{0}' was abandoned recently, not allowing retry until {1}", di.Id, retryTime);
                    }
                }
            }
            return di;
        }

        protected DekiInstance CreateWikiInstance(string wikiId, XDoc instanceConfig) {
            List<DekiInstance> instances = null;
            DekiInstance instance;

            lock(_instances) {
                instance = GetWikiInstance(wikiId);
                if(instance == null) {
                    var licenseStoragePlug = _dekiService.Storage;
                    _loggerRepositories[wikiId] = new ContextLoggerRepository("[" + wikiId + "] ");
                    var licenseController = GetLicenseController(wikiId, licenseStoragePlug);
                    _instances[wikiId] = instance = new DekiInstance(_dekiService, wikiId, instanceConfig, licenseController);
                }

                // Schedule new instance for shutdown if inactive-instance-timeout enabled.
                if(InactiveInstanceTimeOut != TimeSpan.MaxValue) {
                    var timer = _timerFactory.New(OnInstanceExpireTimer, wikiId);
                    _instanceExpireTimers[wikiId] = timer;
                }
                if(_instances.Count > _maxInstances) {
                    instances = _instances.Values.ToList();
                }
            }

            // Hit the instance number limit? Look for least recently accessed wiki and shut it down.
            if(instances != null) {
                Async.Fork(() => {
                    _log.DebugFormat("looking for excess instances to shut down");
                    var excessInstances = (from candidate in instances
                                           where DateTime.UtcNow - candidate.InstanceLastUpdateTime >= _minInstanceIdletime
                                           orderby candidate.InstanceLastUpdateTime
                                           select candidate.Id)
                        .Take(instances.Count - _maxInstances);
                    foreach(var shutdownId in excessInstances) {
                        _log.DebugFormat("shutting down instance '{0}'", shutdownId);
                        OutOfContextShutdown(shutdownId);
                    }
                });
            }
            return instance;
        }

        public bool ShutdownInstance(string wikiId) {
            if(string.IsNullOrEmpty(wikiId)) {
                _log.Warn("Cannot call shutdown with null or empty wikiId");
                return false;
            }
            _log.DebugFormat("trying to shut down instance '{0}'", wikiId);
            DekiInstance instance;
            TaskTimer timer;
            lock(_instances) {
                if(!_instances.TryGetValue(wikiId, out instance)) {
                    return false;
                }

                // Make sure only one thread tries to shut the instance down
                lock(instance) {
                    if(instance.Status == DekiInstanceStatus.ABANDONED) {
                        instance.ShutdownAbandoned();
                        _instances.Remove(wikiId);
                        if(_instanceExpireTimers.TryGetValue(wikiId, out timer)) {
                            _instanceExpireTimers.Remove(wikiId);
                            timer.Cancel();
                        }
                        return true;
                    }
                    if(!instance.BeginShutdown()) {
                        return false;
                    }
                }
            }
            try {
                ShutdownInstance(wikiId, instance);
            } catch(Exception e) {
                _log.DebugFormat("instance '{0}' shutdown had some errors: {1}\r\n{2}", wikiId, e.Message, e);
            } finally {
                lock(_instances) {
                    if(_instances.TryGetValue(wikiId, out instance)) {
                        _instances.Remove(wikiId);
                    }
                    if(_instanceExpireTimers.TryGetValue(wikiId, out timer)) {
                        _instanceExpireTimers.Remove(wikiId);
                        timer.Cancel();
                    }
                }
            }
            return true;
        }

        protected virtual void ShutdownInstance(string wikiid, DekiInstance instance) {
            lock(instance) {
                _log.DebugFormat("shutting down instance '{0}'", wikiid);
                var context = DreamContext.Current;
                var currentDekiContext = context.GetState<DekiContext>();
                DekiContext tempDekiContext = null;
                try {
                    if(currentDekiContext == null || currentDekiContext.Instance.Id != wikiid) {
                        _log.DebugFormat("creating temp deki context for shutdown of instance '{0}'", wikiid);

                        // Note (arnec): the host header is only used for logging in the Epilogue which will never be invoked
                        // by this temporary dekicontext
                        var hostheader = wikiid + "-notused";
                        tempDekiContext = new DekiContext(_dekiService, instance, hostheader, context.StartTime, DekiWikiService.ResourceManager);
                        context.SetState(tempDekiContext);
                    }
                    instance.EndShutdown();
                } finally {
                    if(tempDekiContext != null) {
                        tempDekiContext.Dispose();
                    }
                    context.SetState(currentDekiContext);
                }
            }
        }

        protected void OnInstanceExpireTimer(TaskTimer timer) {
            var wikiId = (string)timer.State;
            _log.DebugFormat("instance '{0}' expired", wikiId);
            OutOfContextShutdown(wikiId);
        }

        private void OutOfContextShutdown(string wikiId) {
            OutOfContextShutdown(wikiId, 0);
        }

        private void OutOfContextShutdown(string wikiId, int attempt) {
            ++attempt;
            _dekiService.Self.At("host", "stop", wikiId)
                .WithCookieJar(_dekiService.Cookies)
                .Post(new Result<DreamMessage>()).WhenDone(
                    m => {
                        if(m.Status != DreamStatus.ServiceUnavailable || attempt > 3) {
                            return;
                        }
                        _log.DebugFormat("stop endpoint was unavailable, attempt {0}", attempt);
                        Async.Sleep(2.Seconds()).WhenDone(r => OutOfContextShutdown(wikiId, attempt));
                    },
                    e => { }
                );
        }
    }
}

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
using log4net;
using MindTouch.Collections;
using MindTouch.Dream;
using MindTouch.Tasking;
using MindTouch.Web;
using MindTouch.Xml;

namespace MindTouch.Deki.PackageUpdate {

    [DreamService("MindTouch Package Update Service", "Copyright (c) 2010 MindTouch Inc.",
        Info = "http://developer.mindtouch.com/en/docs/MindTouch/Specs/Template_Updater",
        SID = new[] { "sid://mindtouch.com/2010/04/packageupdater" }
    )]
    public class PackageUpdaterService : DekiExtService {

        //--- Class Fields ---
        private static readonly ILog _log = LogUtils.CreateLog();

        //--- Fields ---
        private string _packagePath;
        private ExpiringDictionary<string, PackageUpdater> _instances;
        private TimeSpan _instanceTtl;
        private string _apikey;
        private XUri _subscriptionLocation;
        private ProcessingQueue<XDoc> _processingQueue;

        //--- Feature ---
        [DreamFeature("POST:update", "Invoke Package updater")]
        internal IEnumerator<IYield> PostUpdateTemplates(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            var doc = request.ToDocument();
            var wikiId = doc["@wikiid"].AsText;
            var apiUri = doc["uri"].AsText;
            var force = doc["@force"].AsBool ?? false;
            var init = doc["@init"].AsBool ?? false;
            var apiPlug = CreateApiPlug(apiUri, wikiId);
            _log.DebugFormat("received manual tickle from '{0}' @ '{1}' with force={2} and init={3}", wikiId, apiUri, force, init);
            XDoc importReport = null;
            yield return GetInstance(wikiId, true).UpdatePackages(apiPlug, wikiId, _apikey, force, init, new Result<XDoc>()).Set(x => importReport = x);
            response.Return(DreamMessage.Ok(importReport));
            yield break;
        }

        [DreamFeature("GET:status", "Invoke Package updater")]
        [DreamFeatureParam("wikiid", "string", "WikiId for which to check the import status")]
        internal IEnumerator<IYield> GetImportStatus(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            var wikiId = context.GetParam("wikiid");
            _log.DebugFormat("checking status on instance '{0}'", wikiId);
            var instance = GetInstance(wikiId, false);
            var status = instance == null ? "none" : instance.Status.ToString().ToLower();
            response.Return(DreamMessage.Ok(new XDoc("package-updater").Attr("wikiid", wikiId).Attr("status", status)));
            yield break;
        }

        [DreamFeature("POST:queue", "Invoke Package updater asynchronously")]
        internal IEnumerator<IYield> QueueUpdateTemplates(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            var doc = request.ToDocument();
            var wikiId = doc["@wikiid"].AsText;
            var channel = doc["channel"].AsText;
            _log.DebugFormat("received event '{0}' from '{1}'", channel, wikiId);
            if(!_processingQueue.TryEnqueue(doc)) {
                throw new InvalidOperationException("Enqueue of update event failed.");
            }
            response.Return(DreamMessage.Ok());
            yield break;
        }

        //--- Methods ---
        protected override IEnumerator<IYield> Start(XDoc config, Result result) {
            yield return Coroutine.Invoke(base.Start, config, new Result());
            _processingQueue = new ProcessingQueue<XDoc>(CallUpdate);
            _instances = new ExpiringDictionary<string, PackageUpdater>(TimerFactory, true);
            _apikey = config["apikey"].AsText;
            _instanceTtl = TimeSpan.FromSeconds(config["instance-ttl"].AsInt ?? 10 * 60);
            _packagePath = config["package-path"].AsText;
            if(string.IsNullOrEmpty(_packagePath)) {
                throw new ArgumentException("No value was provided for configuration key 'package-path'");
            }
            try {
                _packagePath = PhpUtil.ConvertToFormatString(_packagePath);

                // Note (arnec): _packagePath may contain a {0} for injecting wikiid, so we want to make sure the
                // path string can be formatted without an exception
                string.Format(_packagePath, "dummy");
            } catch {
                throw new ArgumentException(string.Format("The package path '{0}' contains an illegal formmating directive", _packagePath));
            }

            // set up subscription for pubsub
            yield return Coroutine.Invoke(SubscribeInstanceEvents, PubSub, new Result());
            result.Return();
        }

        protected override IEnumerator<IYield> Stop(Result result) {
            _packagePath = null;
            _subscriptionLocation = null;
            foreach(var instance in _instances) {
                instance.Value.Dispose();
            }
            _instances.Dispose();
            _instances = null;
            yield return Coroutine.Invoke(base.Stop, new Result());
            result.Return();
        }

        private IEnumerator<IYield> SubscribeInstanceEvents(Plug pubsubPlug, Result result) {
            _log.DebugFormat("subscribing to pubsub {0}", pubsubPlug.Uri.ToString());
            var subscriptionSet = new XDoc("subscription-set")
                .Elem("uri.owner", Self.Uri)
                .Start("subscription")
                .Add(DreamCookie.NewSetCookie("service-key", InternalAccessKey, Self.Uri).AsSetCookieDocument)
                .Elem("channel", "event://*/deki/site/started")
                .Start("recipient")
                .Attr("authtoken", _apikey)
                .Elem("uri", Self.Uri.At("queue"))
                .End()
                .End();
            DreamMessage subscription = null;
            yield return pubsubPlug.At("subscribers").PostAsync(subscriptionSet).Set(x => subscription = x);

            // only care about created responses, but log other failures
            if(subscription.Status == DreamStatus.Created) {
                string accessKey = subscription.ToDocument()["access-key"].AsText;
                XUri location = subscription.Headers.Location;
                Cookies.Update(DreamCookie.NewSetCookie("access-key", accessKey, location), null);
                _subscriptionLocation = location.AsLocalUri().WithoutQuery();
                _log.DebugFormat("subscribed indexer for events at {0}", _subscriptionLocation);
            } else if(subscription.Status == DreamStatus.Conflict) {
                _log.DebugFormat("didn't subscribe, since we already had a subscription in place");
            } else {
                _log.WarnFormat("subscribe to {0} failed: {1}", pubsubPlug.Uri.ToString(), subscription.Status);
            }
            result.Return();
        }

        private void CallUpdate(XDoc doc, Action completionCallback) {
            var wikiId = doc["@wikiid"].AsText;
            var apiUri = doc["uri"].AsText;
            var apiPlug = CreateApiPlug(apiUri, wikiId);
            var channel = doc["channel"].AsText;
            _log.DebugFormat("processing event '{0}' from '{1}' @ '{2}'", channel, wikiId, apiUri);
            try {
                GetInstance(wikiId, true).UpdatePackages(apiPlug, wikiId, _apikey, false, false, new Result<XDoc>()).WhenDone(r => {
                    try {
                        if(r.HasException) {
                            _log.Warn(string.Format("package update for '{0}' failed", wikiId), r.Exception);
                        }
                    } finally {
                        completionCallback();
                    }
                });
            } catch(Exception e) {
                _log.Warn(string.Format("unable to get updater instance for '{0}'.", wikiId), e);
            }
        }

        private Plug CreateApiPlug(string uri, string wikiId) {
            return Plug.New(uri).WithHeader("X-Deki-Site", "id=" + wikiId);
        }

        private PackageUpdater GetInstance(string wikiId, bool create) {
            PackageUpdater instance;
            lock(_instances) {
                var entry = _instances[wikiId];
                if(entry == null) {
                    if(!create) {
                        return null;
                    }
                    var templatePath = string.Format(_packagePath, wikiId);
                    if(!Directory.Exists(templatePath)) {
                        throw new ArgumentException(string.Format("package path '{0}' does not exist", templatePath));
                    }
                    instance = new PackageUpdater(templatePath);
                    _instances.Set(wikiId, instance, _instanceTtl);
                } else {
                    instance = entry.Value;
                }
            }
            return instance;
        }
    }
}
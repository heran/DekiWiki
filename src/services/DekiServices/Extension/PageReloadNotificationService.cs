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

using log4net;
using MindTouch.Dream;
using MindTouch.Tasking;
using MindTouch.Web;
using MindTouch.Xml;

namespace MindTouch.Deki.Services.Extension {
    using Yield = IEnumerator<IYield>;

    [DreamService("MindTouch Page Reload Notification Extension", "Copyright (c) 2010 MindTouch Inc.",
       Info = "http://developer.mindtouch.com/App_Catalog/InstantCommentNotification",
        SID = new string[] { "sid://mindtouch.com/2009/03/extension/pagereloadnotification" }
    )]
    [DreamServiceConfig("apikey", "string", "Apikey for accessing deki")]
    [DreamServiceConfig("poll-interval", "int?", "Seconds to wait between service polls (60 seconds default")]
    [DreamServiceBlueprint("deki/service-type", "extension")]
    [DekiExtLibrary(
        Label = "Page Reload Notification",
        Namespace = "pagenotification",
        Description = "This extension contains functionality for notifying a user that the current page has changed in real time"
    )]
    public class PageReloadNotificationService : DekiExtService {

        //--- Types ---
        public class Subscription {
            private DateTime _lastTouched;
            public readonly Dictionary<string, bool> Subscribers = new Dictionary<string, bool>();

            public DateTime LastTouched { get { return _lastTouched; } }

            public bool HasChanged(string subscriber) {
                _lastTouched = DateTime.UtcNow;
                lock(Subscribers) {
                    bool delivered;
                    if(!Subscribers.TryGetValue(subscriber, out delivered)) {

                        // subscriber doesn't yet exist, so has changed is false
                        delivered = false;
                    }
                    Subscribers[subscriber] = false;
                    return delivered;
                }
            }

            public void Changed(string user) {
                lock(Subscribers) {
                    string[] keys = new string[Subscribers.Count];
                    Subscribers.Keys.CopyTo(keys, 0);
                    foreach(string subscriber in keys) {

                        // consider the page unchanged if the changing user is the same as the subscriber
                        if(!StringUtil.EqualsInvariantIgnoreCase(user, subscriber)) {
                            Subscribers[subscriber] = true;
                        }
                    }
                }
            }
        }

        //--- Class Fields ---
        private new static readonly ILog _log = LogUtils.CreateLog();

        //--- Fields ---
        private Plug _subscriptionLocation;
        private string _apikey;
        private readonly Dictionary<uint, Subscription> _subscriptions = new Dictionary<uint, Subscription>();
        private readonly Dictionary<string, Tuplet<string, DateTime>> _userCache = new Dictionary<string, Tuplet<string, DateTime>>();
        private TimeSpan _pollInterval;
        private Plug _deki;

        //--- Functions ---
        [DekiExtFunction("init", "Set up the notification section for page reloads")]
        public XDoc ReloadNotification(
            [DekiExtParam("Page id", false)] string id
        ) {
            string containerId = "pn_" + StringUtil.CreateAlphaNumericKey(4);
            XUri self = Self.Uri.AsPublicUri();
            XDoc doc = new XDoc("html")
                .Start("body").Start("div")
                    .Attr("id", containerId)
                .End().End()
                .Start("tail")
                    .Start("script")
                        .Attr("type", "text/javascript")
                        .Value(string.Format("Deki.Api.Poll({0},'{1}','{2}');", _pollInterval.TotalMilliseconds, containerId, self.At("changed", id)))
                    .End()
                .End();
            doc.EndAll();
            return doc;
        }

        //--- Features ---
        [DreamFeature("GET:changed/{pageid}", "Get notification body if the page has changed")]
        public Yield GetQueuedItem(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            uint pageId = context.GetParam<uint>("pageid", 0);
            string containerId = context.GetParam("containerId", null);
            string authtoken = null;
            foreach(DreamCookie cookie in request.Cookies) {
                if(StringUtil.EqualsInvariantIgnoreCase("authtoken", cookie.Name)) {
                    authtoken = cookie.Value;
                    break;
                }
            }
            if(pageId == 0) {
                _log.WarnFormat("Bad pageId");
                response.Return(DreamMessage.BadRequest("Bad pageId"));
                yield break;
            }
            if(string.IsNullOrEmpty(containerId)) {
                _log.WarnFormat("Missing containerId");
                response.Return(DreamMessage.BadRequest("Missing containerId"));
                yield break;
            }
            if(string.IsNullOrEmpty(authtoken)) {
                _log.WarnFormat("Unable to retrieve subscriber credentials from cookie");
                response.Return(DreamMessage.BadRequest("Unable to retrieve subscriber credentials from cookie"));
                yield break;
            }
            Tuplet<string, DateTime> userCache;
            if(!_userCache.TryGetValue(authtoken, out userCache)) {

                Result<DreamMessage> userResult;
                yield return userResult = _deki.At("users", "current").WithHeader("X-Authtoken", authtoken).GetAsync();
                if(!userResult.Value.IsSuccessful) {
                    _log.WarnFormat("Unable to retrieve user info for provided credentials");
                    response.Return(DreamMessage.BadRequest("Unable to retrieve user info for provided credentials"));
                    yield break;
                }
                XDoc userDoc = userResult.Value.ToDocument();
                _log.DebugFormat("caching user info for '{0}': {1}", userDoc["username"].AsText, userDoc["@href"].AsUri.AsPublicUri());
                userCache = new Tuplet<string, DateTime>(userDoc["@href"].AsUri.AsPublicUri().Path, DateTime.UtcNow);
                lock(_userCache) {
                    _userCache[authtoken] = userCache;
                }
            }
            string subscriber = userCache.Item1;
            lock(_subscriptions) {
                Subscription subscription;
                if(!_subscriptions.TryGetValue(pageId, out subscription)) {
                    subscription = new Subscription();
                    _subscriptions[pageId] = subscription;
                    _log.DebugFormat("created subscription for {0}", pageId);
                }
                _log.DebugFormat("checking subscription for {0}", subscriber);
                if(!subscription.HasChanged(subscriber)) {
                    response.Return(DreamMessage.Ok());
                    yield break;
                }
            }
            XDoc doc = new XDoc("div")
                .Attr("class", "systemmsg")
                .Start("div")
                    .Attr("class", "inner")
                    .Value("The page has changed. Click ")
                    .Start("a")
                        .Attr("rel", "custom")
                        .Attr("href", "")
                        .Value("here")
                    .End()
                    .Value(" to reload.")
                .End()
                .Start("script")
                    .Attr("type", "text/javascript")
                    .Value(string.Format("$('#{0}').slideDown('slow');", containerId))
                .End();
            _log.DebugFormat("page {0} changed deliverd", pageId);
            response.Return(DreamMessage.Ok(doc));
            yield break;
        }

        [DreamFeature("POST:notify", "receive a page notifications")]
        internal Yield Notify(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            XDoc doc = request.ToDocument();
            yield return Async.Fork(() => {
                uint pageId = doc["pageid"].AsUInt ?? 0;
                string user = doc["user/uri"].AsUri.Path;
                Subscription subscription;
                lock(_subscriptions) {
                    if(_subscriptions.TryGetValue(pageId, out subscription)) {
                        subscription.Changed(user);
                        _log.DebugFormat("page {0} changed by {1}", pageId, user);
                    }
                }
            }, new Result()).Catch();
            response.Return(DreamMessage.Ok());
        }

        //--- Methods ---
        protected override Yield Start(XDoc config, Result result) {
            yield return Coroutine.Invoke(base.Start, config, new Result());

            // get the apikey, which we will need as a subscription auth token for subscriptions not done on behalf of a user
            _apikey = config["apikey"].AsText;

            // capture the wikiId of the wiki starting us
            string wikiId = config["wikiid"].AsText ?? "default";

            // set up plug deki, so we can validate users
            XUri dekiUri = config["uri.deki"].AsUri ?? new XUri("http://localhost:8081/deki");
            _deki = Plug.New(dekiUri).With("apikey", _apikey).WithHeader("X-Deki-Site", "id=" + wikiId);

            // get ajax polling interval
            _pollInterval = TimeSpan.FromSeconds(config["poll-interval"].AsDouble ?? 60);

            // set up subscription reaper
            TaskTimer.New(TimeSpan.FromSeconds(60), timer => {
                lock(_subscriptions) {
                    var staleSubs = new List<uint>();
                    foreach(KeyValuePair<uint, Subscription> pair in _subscriptions) {
                        if(pair.Value.LastTouched.Add(_pollInterval).Add(TimeSpan.FromSeconds(10)) < DateTime.UtcNow) {
                            staleSubs.Add(pair.Key);
                        }
                    }
                    foreach(uint pageId in staleSubs) {
                        _log.DebugFormat("removing subscription for {0}", pageId);
                        _subscriptions.Remove(pageId);
                    }
                }
                timer.Change(TimeSpan.FromSeconds(60), TaskEnv.None);
            }, null, TaskEnv.None);


            // set up subscription for pubsub
            XDoc subscription = new XDoc("subscription-set")
                .Elem("uri.owner", Self.Uri.AsServerUri().ToString())
                .Start("subscription")
                    .Elem("channel", string.Format("event://{0}/deki/pages/update", wikiId))
                    .Elem("channel", string.Format("event://{0}/deki/pages/revert", wikiId))
                    .Elem("channel", string.Format("event://{0}/deki/pages/tags/update", wikiId))
                    .Elem("channel", string.Format("event://{0}/deki/pages/dependentschanged/comments/create", wikiId))
                    .Elem("channel", string.Format("event://{0}/deki/pages/dependentschanged/comments/update", wikiId))
                    .Elem("channel", string.Format("event://{0}/deki/pages/dependentschanged/comments/delete", wikiId))
                    .Elem("channel", string.Format("event://{0}/deki/pages/dependentschanged/files/create", wikiId))
                    .Elem("channel", string.Format("event://{0}/deki/pages/dependentschanged/files/update", wikiId))
                    .Elem("channel", string.Format("event://{0}/deki/pages/dependentschanged/files/delete", wikiId))
                    .Elem("channel", string.Format("event://{0}/deki/pages/dependentschanged/files/properties/*", wikiId))
                    .Elem("channel", string.Format("event://{0}/deki/pages/dependentschanged/files/restore", wikiId))
                    .Add(DreamCookie.NewSetCookie("service-key", InternalAccessKey, Self.Uri).AsSetCookieDocument)
                    .Start("recipient")
                        .Attr("authtoken", _apikey)
                        .Elem("uri", Self.Uri.AsServerUri().At("notify").ToString())
                    .End()
                .End();
            Result<DreamMessage> subscribe;
            yield return subscribe = PubSub.At("subscribers").PostAsync(subscription);
            string accessKey = subscribe.Value.ToDocument()["access-key"].AsText;
            XUri location = subscribe.Value.Headers.Location;
            Cookies.Update(DreamCookie.NewSetCookie("access-key", accessKey, location), null);
            _subscriptionLocation = Plug.New(location.AsLocalUri().WithoutQuery());
            _log.DebugFormat("set up initial subscription location at {0}", _subscriptionLocation.Uri);
            result.Return();
        }

        protected override Yield Stop(Result result) {
            yield return _subscriptionLocation.DeleteAsync().Catch();
            _subscriptions.Clear();
            yield return Coroutine.Invoke(base.Stop, new Result());
            result.Return();
        }
    }
}

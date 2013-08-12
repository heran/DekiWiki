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

using log4net;
using MindTouch.Deki.Script.Expr;
using MindTouch.Dream;
using MindTouch.Tasking;
using MindTouch.Web;
using MindTouch.Xml;

namespace MindTouch.Deki.Services.Extension {
    using Yield = IEnumerator<IYield>;

    [DreamService("MindTouch Real-time Information Extension", "Copyright (c) 2010 MindTouch Inc.",
       Info = "http://developer.mindtouch.com/App_Catalog/RealTimeInfo",
        SID = new string[] { "sid://mindtouch.com/2009/02/extension/realtimeinfo" }
    )]
    [DreamServiceConfig("news-ttl", "int?", "Max seconds a news event stays around")]
    [DreamServiceConfig("apikey", "string", "Apikey for accessing deki")]
    [DreamServiceConfig("ignore", "string", "Comma separated list of page paths to ignore for stats purposes")]
    [DreamServiceBlueprint("deki/service-type", "extension")]
    [DekiExtLibrary(
        Label = "Real Time Info",
        Namespace = "realtime",
        Description = "This extension contains functions for retrieving real time site view information"
    )]
    public class RealTimeInfoService : DekiExtService {

        //--- Types ---
        public class View {
            public readonly uint PageId;
            public readonly XUri PageUri;
            public readonly string PagePath;
            public readonly XUri UserUri;
            public readonly DateTime Time = DateTime.UtcNow;

            public View(uint id, XUri pageUri, string pagePath, XUri userUri) {
                PageId = id;
                PageUri = pageUri;
                PagePath = pagePath;
                UserUri = userUri;
            }
        }

        //--- Class Fields ---
        private new static readonly ILog _log = LogUtils.CreateLog();

        //--- Fields ---
        private TimeSpan _ttl;
        private Plug _subscriptionLocation;
        private readonly Queue<View> _pageViews = new Queue<View>();
        private readonly Dictionary<string, object> _ignore = new Dictionary<string, object>();
        private TimeSpan _checkInterval;
        private string _apikey;
        private XUri _anonymousUser;

        //--- Functions ---
        [DekiExtFunction("popular", "Get a map of recent page view information")]
        public Hashtable PopularPages(
            [DekiExtParam("max results (default: 10)", true)] int? max,
            [DekiExtParam("poll interval (only for js format, default: 30)", true)] int? interval
        ) {
            int maxResults = max ?? 10;
            int resultCount = 0;
            DekiScriptMap env = DreamContext.Current.GetState<DekiScriptMap>();
            DekiScriptLiteral uriLiteral = env.GetAt("site.uri");
            XUri deki = new XUri(uriLiteral.NativeValue.ToString()).At("@api", "deki");
            Hashtable map = new Hashtable(StringComparer.OrdinalIgnoreCase);
            map.Add("interval", _ttl.TotalSeconds);
            ArrayList pages = new ArrayList();
            map.Add("pages", pages);
            int total = 0;
            Dictionary<uint, int> rankLookup = new Dictionary<uint, int>();
            lock(_pageViews) {
                foreach(View view in _pageViews) {
                    if(rankLookup.ContainsKey(view.PageId)) {
                        rankLookup[view.PageId]++;
                    } else {
                        rankLookup[view.PageId] = 1;
                    }
                    total++;
                }
            }
            List<Tuplet<uint, int>> rank = new List<Tuplet<uint, int>>();
            foreach(KeyValuePair<uint, int> kvp in rankLookup) {
                rank.Add(new Tuplet<uint, int>(kvp.Key, kvp.Value));
            }
            rank.Sort(delegate(Tuplet<uint, int> a, Tuplet<uint, int> b) {
                return b.Item2.CompareTo(a.Item2);
            });
            map.Add("total", total);
            foreach(Tuplet<uint, int> page in rank) {
                Hashtable pageMap = new Hashtable(StringComparer.OrdinalIgnoreCase);
                pages.Add(pageMap);

                // BUGBUGBUG (arnec): the AsLocalUri should not be required after bug #5964 is resolved
                pageMap.Add("page", DekiScriptExpression.Constant(deki.At("$page").AsLocalUri(), new[] { DekiScriptExpression.Constant(page.Item1), DekiScriptExpression.Constant(true) }));
                pageMap.Add("views", page.Item2);
                resultCount++;
                if(resultCount >= maxResults) {
                    break;
                }
            }
            return map;
        }

        [DekiExtFunction("pageactivity", "Get recent activity related to a page")]
        public XDoc PageActivity(
             [DekiExtParam("path to page", true)] string path,
             [DekiExtParam("format: html, xml (default: html)", true)] string format
        ) {
            if(string.IsNullOrEmpty(path)) {
                return null;
            }
            View[] activity;
            lock(_pageViews) {
                activity = _pageViews.ToArray();
            }

            // Note (arnec): Since _pageViews is a queue, activity is in time sorted order and we just reverse it to get the most recent first
            Array.Reverse(activity);

            // find all views for the page
            List<View> pageViews = new List<View>();
            foreach(View view in activity) {
                if(StringUtil.EqualsInvariantIgnoreCase(path, view.PagePath)) {
                    pageViews.Add(view);
                }
            }

            // find the most recent user that has looked at another page since
            XDoc nextDestination = null;
            foreach(View user in pageViews) {
                if(user.UserUri == _anonymousUser) {
                    continue;
                }
                foreach(View view in activity) {
                    if(user.UserUri != view.UserUri) {
                        continue;
                    }
                    if(!StringUtil.EqualsInvariantIgnoreCase(view.PagePath, user.PagePath)) {
                        DreamMessage pageMessage = Plug.New(view.PageUri).With("apikey", _apikey).GetAsync().Wait();
                        nextDestination = pageMessage.ToDocument();
                    }
                    break;
                }
            }
            XDoc doc;
            switch(format) {
            case "xml":
                doc = new XDoc("activity").Attr("interval", _ttl.TotalSeconds);
                break;
            default:
                doc = new XDoc("html").Start("body").Start("div").Attr("class", "popular-list")
                    .Elem("p", string.Format("{0} views in last {1:0} minutes", pageViews.Count, (int)_ttl.TotalMinutes));
                if(nextDestination != null) {
                    doc.Start("p")
                        .Value("The last visitor to this page went to ")
                        .Start("a").Attr("href", nextDestination["uri.ui"].AsText).Value(nextDestination["title"].AsText).End()
                        .Value(" next.")
                    .End();
                }
                break;
            }
            doc.EndAll();
            return doc;
        }

        //--- Features ---
        [DreamFeature("POST:notify/view", "receive a page view event")]
        internal Yield NotifyViews(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            XDoc doc = request.ToDocument();
            yield return Async.Fork(() => {
                XUri userUri = doc["user/uri"].AsUri;
                if(_anonymousUser == null && (doc["user/@anonymous"].AsBool ?? false)) {
                    _anonymousUser = userUri;
                }
                string path = doc["path"].AsText;
                if(!_ignore.ContainsKey(path)) {
                    lock(_pageViews) {
                        _pageViews.Enqueue(new View(doc["pageid"].AsUInt ?? 0, doc["uri"].AsUri, path, userUri));
                    }
                }
            }, new Result()).Catch();
            response.Return(DreamMessage.Ok());
        }

        //--- Methods ---
        protected override Yield Start(XDoc config, Result result) {
            yield return Coroutine.Invoke(base.Start, config, new Result());

            // set up cache reaper
            _ttl = TimeSpan.FromSeconds(Config["news-ttl"].AsDouble ?? 60 * 60);
            double? checkInterval = Config["check-interval"].AsDouble;
            if(checkInterval.HasValue) {
                _checkInterval = TimeSpan.FromSeconds(checkInterval.Value);
            } else {
                double checkInterval2 = _ttl.TotalSeconds / 10;
                if(_ttl.TotalSeconds < 30 || checkInterval2 < 30) {
                    checkInterval2 = 30;
                }
                _checkInterval = TimeSpan.FromSeconds(checkInterval2);
            }
            TaskTimer.New(_ttl, delegate(TaskTimer timer) {
                lock(_pageViews) {
                    View next;
                    do {
                        if(_pageViews.Count == 0) {
                            break;
                        }
                        next = _pageViews.Peek();
                        if(next != null) {
                            if(next.Time.Add(_ttl) < DateTime.UtcNow) {
                                _pageViews.Dequeue();
                            } else {
                                break;
                            }
                        }
                    } while(next != null);
                }
                timer.Change(_checkInterval, TaskEnv.None);
            }, null, TaskEnv.None);

            // get the apikey, which we will need as a subscription auth token for subscriptions not done on behalf of a user
            _apikey = config["apikey"].AsText;

            // build ignore list
            string ignore = config["ignore"].AsText;
            if(!string.IsNullOrEmpty(ignore)) {
                foreach(string page in ignore.Split(new char[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries)) {
                    _ignore[page] = null;
                }
            }
            // set up subscription for page views
            XDoc subscription = new XDoc("subscription-set")
                .Elem("uri.owner", Self.Uri.AsServerUri().ToString())
                .Start("subscription")
                    .Elem("channel", "event://*/deki/pages/view")
                    .Add(DreamCookie.NewSetCookie("service-key", InternalAccessKey, Self.Uri).AsSetCookieDocument)
                    .Start("recipient")
                        .Attr("authtoken", _apikey)
                        .Elem("uri", Self.Uri.AsServerUri().At("notify", "view").ToString())
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
            _pageViews.Clear();
            yield return Coroutine.Invoke(base.Stop, new Result());
            result.Return();
        }
    }
}

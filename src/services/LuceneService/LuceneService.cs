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
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using log4net;

using Lucene.Net.Analysis;
using Lucene.Net.Index;
using Lucene.Net.Documents;
using Lucene.Net.Search;
using Lucene.Net.QueryParsers;
using MindTouch.Collections;
using MindTouch.Dream;
using MindTouch.Extensions.Time;
using MindTouch.IO;
using MindTouch.Tasking;
using MindTouch.Web;
using MindTouch.Xml;

namespace MindTouch.LuceneService {
    using Yield = IEnumerator<IYield>;

    [DreamService("MindTouch Lucene Service", "Copyright (c) 2006-2010 MindTouch Inc.",
       Info = "http://developer.mindtouch.com/Deki/API/LuceneService",
        SID = new[] { 
            "sid://mindtouch.com/2007/06/luceneindex",
            "http://services.mindtouch.com/deki/draft/2007/06/luceneindex"
        }
    )]
    [DreamServiceConfig("path.store", "string", "Path to the lucene index on disk")]
    public class LuceneService : DreamService {

        //--- Constants ---
        private static readonly Regex _wordcountRegex = new Regex(@"\w+", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        private const string CUSTOM_PROPERTY_NAMESPACE = "urn:custom.mindtouch.com#";

        //--- Class Fields ---
        private static readonly ILog _log = LogUtils.CreateLog();

        //--- Fields ---
        private readonly Html2Text _htmlConverter = new Html2Text();
        private Analyzer _defaultAnalyzer;
        private int _previewLength;
        private string[] _indexNamespaceWhitelist;
        private Dictionary<string, SearchFilter> _searchFilters;
        private string _apikey;
        private XUri _subscriptionLocation;
        private Dictionary<string, float> _namespaceBoost;
        private string _queuePathBase;
        private TimeSpan _filterTimeout;
        private string _indexPath;
        private TimeSpan _instanceTtl;
        private TimeSpan _indexAccumulationTime;
        private ExpiringDictionary<string, SearchInstanceData> _instances;
        private int _indexerParallelism;
        private int _indexerMaxRetry;
        private TimeSpan _indexerRetrySleep;
        private float _searchScoreThreshhold;
        private bool _allowLeadingWildCard;
        private TimeSpan _instanceCommitInterval;
        private LuceneProfilerFactory _profilerFactory;
        private int _subscriptionMaxFailureDuration;
        private bool _usePersistentSubscription;

        //--- Features ---
        [DreamFeature("DELETE:clear", "clear and reinitialize the index")]
        [DreamFeatureParam("wikiid", "string?", "id of the wiki instance (default: \"default\")")]
        internal Yield ClearIndex(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            string wikiid = context.GetParam("wikiid", "default");
            GetInstance(wikiid).Clear();
            response.Return(DreamMessage.Ok());
            yield break;
        }

        [DreamFeature("GET:stats", "Return index statistics")]
        [DreamFeatureParam("wikiid", "string?", "id of the wiki instance (default: \"default\")")]
        internal Yield GetStats(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            string wikiid = context.GetParam("wikiid", "default");
            response.Return(DreamMessage.Ok(GetInstance(wikiid).GetStats()));
            yield break;
        }

        [DreamFeature("GET:queue/size", "Returns current size of the unindexed item queue")]
        [DreamFeatureParam("wikiid", "string?", "id of the wiki instance (if omitted, shows queue size for entire service)")]
        internal Yield GetQueueSize(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            var wikiid = context.GetParam("wikiid", null);
            if(string.IsNullOrEmpty(wikiid)) {
                int totalSize = 0;
                foreach(var instance in GetInstances()) {
                    try {
                        totalSize += instance.QueueSize;
                    } catch(ObjectDisposedException) { } // ignoring ObjectDisposed
                }
                response.Return(DreamMessage.Ok(new XDoc("queue").Elem("size", totalSize)));
            } else {
                var instance = GetInstance(wikiid);
                var size = 0;
                if(instance != null) {
                    try {
                        size = instance.QueueSize;
                    } catch(ObjectDisposedException) { } // ignoring ObjectDisposed
                }
                response.Return(DreamMessage.Ok(new XDoc("queue").Attr("wikiid", wikiid).Elem("size", size)));
            }
            yield break;
        }

        [DreamFeature("POST:subscriptions", "Subscribe the lucene service to a specific pubsub service")]
        internal Yield SubscribeServiceToAdditionalPubSub(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            var pubsub = request.ToDocument();
            var pubsubPlug = Plug.New(pubsub["@href"].AsUri);
            foreach(var header in pubsub["header"]) {
                pubsubPlug.WithHeader(header["name"].AsText, header["value"].AsText);
            }
            var setCookies = DreamCookie.ParseAllSetCookieNodes(pubsub["set-cookie"]);
            if(setCookies.Count > 0) {
                pubsubPlug.CookieJar.Update(setCookies, null);
            }
            yield return Coroutine.Invoke(SubscribeIndexer, pubsubPlug, new Result());
            response.Return(DreamMessage.Ok());
            yield break;
        }

        [DreamFeature("POST:queue", "Queue a document for addition or replacement in the index (allows change accumulation)")]
        internal Yield QueueDocument(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            XDoc doc = request.ToDocument();
            if(_log.IsDebugEnabled) {
                XUri channel = doc["channel"].AsUri;
                string type = channel.Segments[1];
                string action = channel.Segments[2];
                string uri = doc["uri"].AsText;
                _log.DebugFormat("queueing action '{0}' for resource type '{1}': {2}", action, type, uri);
            }
            var wikiid = doc["@wikiid"].AsText;
            GetInstance(wikiid).Enqueue(doc);
            response.Return(DreamMessage.Ok());
            yield break;
        }

        [DreamFeature("GET:initstate", "Check on instance indexer state")]
        [DreamFeatureParam("wikiid", "string?", "id of the wiki instance (if omitted, shows queue size for entire service)")]
        internal Yield GetInitState(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            var wikiid = context.GetParam("wikiid", "default");
            var exists = CheckForInstance(wikiid);
            response.Return(DreamMessage.Ok(new XDoc("instance").Attr("wikiid", wikiid).Attr("exists", exists)));
            yield break;
        }

        [DreamFeature("GET:", "Search the index")]
        [DreamFeatureParam("q", "string", "search query")]
        [DreamFeatureParam("max", "string?", "maximum number of results to return, use 'all' for unlimited results (default: 100)")]
        [DreamFeatureParam("threshhold", "double?", "minimum score a result must beat to be included in set (default: 0.01 or service configuration)")]
        [DreamFeatureParam("offset", "int?", "offset in the result set, ignored if max is 'all' (default: 0)")]
        [DreamFeatureParam("wikiid", "string?", "id of the wiki instance (default: \"default\")")]
        [DreamFeatureParam("userid", "uint?", "Optional id of user for whose permissions the results should be filtered (requires apiuri)")]
        [DreamFeatureParam("apiuri", "uint?", "Uri to api to use for filtering")]
        [DreamFeatureParam("explain", "bool?", "Include query details (default: false)")]
        [DreamFeatureParam("sortby", "{score, title, date, size, wordcount, user.username, user.fullname, user.date.lastlogin}?", "Sort field. Prefix value with '-' to sort descending (default: -score)")]
        internal Yield Search(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            var explain = !context.Uri.GetParam("explain", "false").EqualsInvariantIgnoreCase("false");
            XDoc ret = new XDoc("documents");
            string q = context.GetParam("q");
            var max = context.GetParam("max", "100");
            int limit = int.MaxValue;
            int offset = 0;
            if(!max.StartsWithInvariantIgnoreCase("all")) {
                limit = (int)Math.Min(long.Parse(max), int.MaxValue);
                offset = context.GetParam("offset", 0);
            }
            var authPlug = GetAuthPlug(context, request);
            var threshold = context.GetParam("threshhold", _searchScoreThreshhold);
            var wikiid = context.GetParam("wikiid", "default");
            var sortDesc = false;
            SortField sortField = null;
            string sortBy = context.GetParam("sortby", "-score");
            if(sortBy.StartsWith("-")) {
                sortDesc = true;
                sortBy = sortBy.Substring(1);
            }
            switch(sortBy) {
            case "title":
                sortField = new SortField("title.sort", SortField.STRING, sortDesc);
                break;
            case "date":
                sortField = new SortField("date.edited", SortField.STRING, sortDesc);
                break;
            case "size":
                sortField = new SortField("size", SortField.AUTO, sortDesc);
                break;
            case "wordcount":
                sortField = new SortField("wordcount", SortField.AUTO, sortDesc);
                break;
            case "user.username":
                sortField = new SortField("username", SortField.STRING, sortDesc);
                break;
            case "user.fullname":
                sortField = new SortField("fullname.sort", SortField.STRING, sortDesc);
                break;
            case "user.date.lastlogin":
                sortField = new SortField("date.lastlogin", SortField.STRING, sortDesc);
                break;
            case "rating.score":
                sortField = new SortField("rating.score", SortField.FLOAT, sortDesc);
                break;
            case "rating.count":
                sortField = new SortField("rating.count", SortField.INT, sortDesc);
                break;
            case "score":
                if(!sortDesc) {
                    sortField = new SortField(SortField.FIELD_SCORE.GetField(), SortField.SCORE, true);
                }
                break;
            }
            Query query;
            var instance = GetInstance(wikiid);
            using(var profile = _profilerFactory.Start("standard", wikiid)) {
                try {
                    BooleanQuery.SetMaxClauseCount(ushort.MaxValue);
                    var parser = new QueryParser("content", _defaultAnalyzer);
                    parser.SetAllowLeadingWildcard(_allowLeadingWildCard);
                    using(profile.ProfileParse(q)) {
                        query = parser.Parse(q);
                    }
                } catch(ParseException) {
                    response.Return(DreamMessage.BadRequest(string.Format("Error parsing search query: {0}", q)));
                    yield break;
                }
                ret.Elem("parsedQuery", query.ToString());
                IList<LuceneResult> resultSet = null;
                using(profile.ProfileQuery()) {
                    resultSet = instance.Search(query, sortField, limit, offset, threshold, profile, authPlug);
                }
                using(profile.ProfilePostProcess(resultSet.Count)) {
                    foreach(var result in resultSet) {
                        ConvertToXDoc(ret, result);
                    }
                }
                if(explain) {
                    var beginning = ret[0];
                    beginning.AddAllBefore(profile.GetProfile());
                }
            }
            response.Return(DreamMessage.Ok(ret));
            yield break;
        }

        [DreamFeature("GET:compact", "Search the index")]
        [DreamFeatureParam("q", "string", "search query")]
        [DreamFeatureParam("wikiid", "string?", "id of the wiki instance (default: \"default\")")]
        [DreamFeatureParam("threshhold", "double?", "minimum score a result must beat to be included in set (default: 0.01 or service configuration)")]
        [DreamFeatureParam("userid", "uint?", "Optional id of user for whose permissions the results should be filtered (requires apiuri)")]
        [DreamFeatureParam("apiuri", "uint?", "Uri to api to use for filtering")]
        [DreamFeatureParam("explain", "bool?", "Include query details (default: false)")]
        internal Yield SearchCompact(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            var explain = !context.Uri.GetParam("explain", "false").EqualsInvariantIgnoreCase("false");
            var ret = new XDoc("documents");
            var q = context.GetParam("q");
            var wikiid = context.GetParam("wikiid", "default");
            var threshold = context.GetParam("threshhold", _searchScoreThreshhold);
            var authPlug = GetAuthPlug(context, request);
            Query query;
            var instance = GetInstance(wikiid);
            using(var profile = _profilerFactory.Start("compact", wikiid)) {
                try {
                    BooleanQuery.SetMaxClauseCount(ushort.MaxValue);
                    var parser = new QueryParser("content", _defaultAnalyzer);
                    parser.SetAllowLeadingWildcard(_allowLeadingWildCard);
                    using(profile.ProfileParse(q)) {
                        query = parser.Parse(q);
                    }
                } catch(ParseException) {
                    response.Return(DreamMessage.BadRequest(string.Format("Error parsing search query: {0}", q)));
                    yield break;
                }
                ret.Elem("parsedQuery", query.ToString());
                IList<LuceneResult> resultSet;
                using(profile.ProfileQuery()) {
                    resultSet = instance.Search(query, null, int.MaxValue, 0, threshold, profile, authPlug);
                }
                using(profile.ProfilePostProcess(resultSet.Count)) {
                    foreach(var result in resultSet) {
                        ConvertToCompactXDoc(ret, result);
                    }
                }
                if(explain) {
                    var beginning = ret[0];
                    beginning.AddAllBefore(profile.GetProfile());
                }
            }
            response.Return(DreamMessage.Ok(ret));
            yield break;
        }

        //--- Methods ---
        protected override Yield Start(XDoc config, Result result) {
            yield return Coroutine.Invoke(base.Start, config, new Result());
            _instances = new ExpiringDictionary<string, SearchInstanceData>(TimerFactory, true);
            _instances.EntryExpired += OnEntryExpired;
            _profilerFactory = new LuceneProfilerFactory(TimeSpan.FromSeconds(config["profiler-slow-query-threshold"].AsDouble ?? 1));
            _apikey = config["apikey"].AsText;
            _filterTimeout = TimeSpan.FromSeconds(config["filter-timeout"].AsInt ?? 30);
            _instanceTtl = TimeSpan.FromSeconds(config["instance-ttl"].AsInt ?? 10 * 60);
            _instanceCommitInterval = TimeSpan.FromSeconds(config["instance-commit-interval"].AsInt ?? 30);
            _indexAccumulationTime = TimeSpan.FromSeconds(config["accumulation-time"].AsInt ?? 10);
            _searchScoreThreshhold = config["score-threshhold"].AsFloat ?? 0.001f;
            _log.DebugFormat("filter timeout: {0}", _filterTimeout);
            _indexerParallelism = config["indexer-parallelism"].AsInt ?? 10;
            _log.DebugFormat("indexer parallelism: {0}", _indexerParallelism);
            _indexerMaxRetry = config["indexer-retry"].AsInt ?? 3;
            _indexerRetrySleep = (config["indexer-retry-sleep"].AsDouble ?? 10).Seconds();
            _log.DebugFormat("indexer retries: {0}", _indexerMaxRetry);
            _allowLeadingWildCard = config["allow-leading-wildcard"].AsBool ?? false;
            _subscriptionMaxFailureDuration = config["max-subscription-failure-duration"].AsInt ?? 48 * 60 * 60; // 48 hour default failure window
            _usePersistentSubscription = config["use-persistent-subscription"].AsBool ?? false;
            if(_usePersistentSubscription) {
                if(string.IsNullOrEmpty(config["internal-service-key"].AsText)) {
                    throw new ArgumentException("Cannot enable persistent subscriptions, without also specifying a custom internal-access-key");
                }
            } else {

                // set up subscription for pubsub
                yield return Coroutine.Invoke(SubscribeIndexer, PubSub, new Result());
            }
            if(_defaultAnalyzer == null) {
                _defaultAnalyzer = new PerFieldAnalyzer();
            }
            int previewLength;
            if(int.TryParse(config["preview-length"].AsText, out previewLength)) {
                _previewLength = previewLength;
            } else {
                _previewLength = 1024;
            }
            _searchFilters = new Dictionary<string, SearchFilter>();
            foreach(XDoc filter in config["filter-path"]) {
                _searchFilters.Add("." + filter["@extension"].AsText, new SearchFilter(filter.AsText ?? "", filter["@arguments"].AsText ?? ""));
            }
            if(string.IsNullOrEmpty(config["namespace-whitelist"].AsText)) {
                _indexNamespaceWhitelist = new[] { "main", "project", "user", "template", "help", "main_talk", "project_talk", "user_talk", "template_talk", "help_talk" };
            } else {
                _indexNamespaceWhitelist = config["namespace-whitelist"].AsText.Split(',').Select(item => item.Trim()).ToArray();
            }

            // set up index path and make sure it's valid
            _indexPath = Config["path.store"].AsText;
            try {
                _indexPath = PhpUtil.ConvertToFormatString(_indexPath);

                // Note (arnec): _indexPath may contain a {0} for injecting wikiid, so we want to make sure the
                // path string can be formatted without an exception
                string.Format(_indexPath, "dummy");
            } catch {
                throw new ArgumentException(string.Format("The storage base path '{0}' contains an illegal formmating directive", _indexPath));
            }

            // set up queue path base string & make sure it's valid
            var queueBasePath = config["path.queue"].AsText ?? Config["path.store"].AsText.TrimEnd(Path.DirectorySeparatorChar, '/') + "-queue";
            try {
                _queuePathBase = PhpUtil.ConvertToFormatString(queueBasePath);

                // Note (arnec): _queuePathBase may contain a {0} for injecting wikiid, so we want to make sure the
                // path string can be formatted without an exception
                string.Format(_queuePathBase, "dummy");
            } catch {
                throw new ArgumentException(string.Format("The queue base path '{0}' contains an illegal formmating directive", queueBasePath));
            }

            // setup boost values for namespaces
            _namespaceBoost = new Dictionary<string, float> {
                {"main", 4F},
                {"main_talk", 1F}, 
                {"user", 2F}, 
                {"user_talk", .5F}, 
                {"project", 4F},
                {"project_talk", 1F},
                {"template", .5F},
                {"template_talk", .5F},
                {"help", 1F},
                {"help_talk", .5F}
            };
            if(!Config["namespace-boost"].IsEmpty) {
                foreach(XDoc ns in Config["namespace-boost/namespace"]) {
                    if(_namespaceBoost.ContainsKey(ns["@name"].AsText))
                        _namespaceBoost[ns["@name"].AsText] = ns["@value"].AsFloat ?? 1F;
                }
            }

            result.Return();
        }

        protected override Yield Stop(Result result) {
            _instances.EntryExpired -= OnEntryExpired;
            foreach(var instance in GetInstances()) {
                instance.Dispose();
            }
            _instances.Dispose();
            _instances = null;
            _searchFilters = null;
            _indexNamespaceWhitelist = null;
            _namespaceBoost = null;
            yield return Plug.New(_subscriptionLocation).DeleteAsync().Catch();
            yield return Coroutine.Invoke(base.Stop, new Result());
            result.Return();
        }

        public override DreamFeatureStage[] Prologues {
            get {
                return new[] { 
                    new DreamFeatureStage("start-stats", this.PrologueStats, DreamAccess.Public),
                };
            }
        }

        public override DreamFeatureStage[] Epilogues {
            get {
                return new[] {                    
                    new DreamFeatureStage("end-stats", this.EpilogueStats, DreamAccess.Public), 
                };
            }
        }

        private Yield PrologueStats(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            var sw = Stopwatch.StartNew();
            context.SetState("stats-stopwatch", sw);
            response.Return(request);
            yield break;
        }

        private Yield EpilogueStats(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            var sw = context.GetState<Stopwatch>("stats-stopwatch");
            sw.Stop();
            _log.InfoFormat("Finished [{0}:{1}] [{2}] {3:0}ms", context.Verb, context.Uri.Path, request.Status.ToString(), sw.ElapsedMilliseconds);
            response.Return(request);
            yield break;
        }

        private Plug GetAuthPlug(DreamContext context, DreamMessage request) {
            var userIdString = context.GetParam("userid", null);
            if(string.IsNullOrEmpty(userIdString)) {
                return null;
            }
            var userId = uint.Parse(userIdString);
            var wikiid = context.GetParam("wikiid", "default");
            var plug = Plug.New(context.GetParam("apiuri"))
                .At("users", userId.ToString(), "allowed")
                .WithHeader("X-Deki-Site", "id=" + wikiid)
                .With("operations", "READ,BROWSE")
                .With("verbose", "false")
                .With("invert", "true");
            return plug;
        }

        private void OnEntryExpired(object sender, ExpirationArgs<string, SearchInstanceData> e) {
            _log.DebugFormat("expiring instance '{0}'", e.Entry.Key);
            e.Entry.Value.Dispose();
        }

        private Yield SubscribeIndexer(Plug pubsubPlug, Result result) {
            _log.DebugFormat("subscribing indexer to pubsub {0}", pubsubPlug.Uri.ToString());
            var subscriptionSet = new XDoc("subscription-set")
                 .Elem("uri.owner", Self.Uri)
                 .Start("subscription")
                     .Add(DreamCookie.NewSetCookie("service-key", InternalAccessKey, Self.Uri).AsSetCookieDocument)
                     .Elem("channel", "event://*/deki/pages/noop")
                     .Elem("channel", "event://*/deki/pages/create")
                     .Elem("channel", "event://*/deki/pages/move")
                     .Elem("channel", "event://*/deki/pages/update")
                     .Elem("channel", "event://*/deki/pages/delete")
                     .Elem("channel", "event://*/deki/pages/revert")
                     .Elem("channel", "event://*/deki/pages/rated")
                     .Elem("channel", "event://*/deki/pages/createalias")
                     .Elem("channel", "event://*/deki/pages/tags/update")
                     .Elem("channel", "event://*/deki/pages/dependentschanged/properties/*")
                     .Elem("channel", "event://*/deki/files/noop")
                     .Elem("channel", "event://*/deki/files/create")
                     .Elem("channel", "event://*/deki/files/update")
                     .Elem("channel", "event://*/deki/files/delete")
                     .Elem("channel", "event://*/deki/files/move")
                     .Elem("channel", "event://*/deki/files/restore")
                     .Elem("channel", "event://*/deki/comments/noop")
                     .Elem("channel", "event://*/deki/comments/create")
                     .Elem("channel", "event://*/deki/comments/update")
                     .Elem("channel", "event://*/deki/comments/delete")
                     .Elem("channel", "event://*/deki/users/dependentschanged/properties/*")
                     .Elem("channel", "event://*/deki/users/create")
                     .Elem("channel", "event://*/deki/users/update")
                     .Elem("channel", "event://*/deki/users/login")
                     .Start("recipient")
                         .Attr("authtoken", _apikey)
                         .Elem("uri", Self.Uri.At("queue"))
                     .End()
                 .End();
            if(_usePersistentSubscription) {
                var locationKey = StringUtil.ComputeHashString(pubsubPlug.Uri.AsPublicUri().WithoutQuery() + InternalAccessKey);
                var accessKey = StringUtil.ComputeHashString(InternalAccessKey);
                subscriptionSet.Attr("max-failure-duration", _subscriptionMaxFailureDuration);
                pubsubPlug = pubsubPlug.WithHeader("X-Set-Access-Key", accessKey).WithHeader("X-Set-Location-Key", locationKey);
            }
            DreamMessage subscription = null;
            yield return pubsubPlug.At("subscribers").PostAsync(subscriptionSet).Set(x => subscription = x);

            // only care about created responses, but log other failures
            switch(subscription.Status) {
            case DreamStatus.Created: {
                    var accessKey = subscription.ToDocument()["access-key"].AsText;
                    var location = subscription.Headers.Location;
                    Cookies.Update(DreamCookie.NewSetCookie("access-key", accessKey, location), null);
                    _subscriptionLocation = location.AsLocalUri().WithoutQuery();
                    _log.DebugFormat("subscribed indexer for events at {0}", _subscriptionLocation);
                    break;
                }
            case DreamStatus.Conflict:
                _log.DebugFormat("didn't subscribe, since we already had a subscription in place");
                break;
            default:
                _log.WarnFormat("subscribe to {0} failed: {1}", pubsubPlug.Uri.ToString(), subscription.Status);
                break;
            }
            result.Return();
        }

        private Yield OnQueueExpire(UpdateRecord data, Result result) {
            _log.DebugFormat("indexing '{0}'", data.Id);
            XUri docId = data.Id.WithHost("localhost").WithPort(80);
            string wikiid = data.Id.Host;
            if(string.IsNullOrEmpty(wikiid)) {
                wikiid = "default";
            }
            XDoc revision = null;
            XUri revisionUri = null;
            XUri channel = data.Meta["channel"].AsUri;
            string type = channel.Segments[1];
            string action = channel.Segments[2];
            string contentUri = string.Empty;
            _log.DebugFormat("processing action '{0}' for resource type '{1}' and id '{2}'", action, type, data.Id);
            Term deleteTerm;
            // if this is an Add we need to validate the data before we get to a possible delete
            string oldDocUri = docId.ToString().ToLowerInvariant();
            switch(type) {
            case "pages":
                if(oldDocUri.Contains("@api/deki/archive/")) {
                    oldDocUri = oldDocUri.Replace("@api/deki/archive/", "@api/deki/");
                }
                deleteTerm = new Term("uri", oldDocUri);
                break;
            case "users":
                var userId = data.Meta["userid"].AsText;
                deleteTerm = new Term("id.user", userId);
                break;
            default:
                deleteTerm = new Term("uri", oldDocUri);
                break;
            }
            if(data.ActionStack.IsAdd) {
                if(data.Meta.IsEmpty) {
                    throw new DreamBadRequestException("document is empty");
                }
                switch(type) {
                case "files":
                    revisionUri = data.Meta["revision.uri"].AsUri;
                    contentUri = data.Meta["content.uri"].AsText;
                    if(string.IsNullOrEmpty(contentUri)) {
                        throw new DreamBadRequestException(string.Format("missing content uri for '{0}'", data.Id));
                    }
                    break;
                case "pages":
                    revisionUri = data.Meta["revision.uri"].AsUri;
                    contentUri = data.Meta["content.uri[@type='application/xml']"].AsText;
                    if(string.IsNullOrEmpty(contentUri)) {
                        throw new DreamBadRequestException(string.Format("missing xml content uri for '{0}'", data.Id));
                    }
                    break;
                case "comments":
                    revisionUri = data.Meta["uri"].AsUri;
                    break;
                case "users":
                    revisionUri = data.Meta["uri"].AsUri;
                    break;
                }
                if(revisionUri == null) {
                    throw new DreamBadRequestException(string.Format("missing revision uri for '{0}'", data.Id));
                }
                Result<DreamMessage> revisionResult;
                _log.DebugFormat("fetching revision for {1} from {0}", data.Id, revisionUri);
                yield return revisionResult = Plug.New(revisionUri).With("apikey", _apikey).GetAsync();
                if(!revisionResult.Value.IsSuccessful) {
                    throw BadRequestException(revisionResult.Value, "unable to fetch revision info from '{0}' (status: {1})", data.Meta["revision.uri"].AsText, revisionResult.Value.Status);
                }
                revision = revisionResult.Value.ToDocument();
            }
            _log.DebugFormat("deleting '{0}' from index using uri {1}", data.Id, oldDocUri);
            GetInstance(wikiid).DeleteDocuments(deleteTerm);

            // build new document
            string text = string.Empty;
            if(data.ActionStack.IsAdd) {
                _log.DebugFormat("adding '{0}' to index", data.Id);
                var d = new Document();
                d.Add(new Field("uri", docId.ToString().ToLowerInvariant(), Field.Store.YES, Field.Index.UN_TOKENIZED));
                d.Add(new Field("mime", revision["contents/@type"].AsText ?? "", Field.Store.YES, Field.Index.TOKENIZED));
                DateTime editDate;
                string editDateStringFromDoc = (type == "files") ? revision["date.created"].AsText : revision["date.edited"].AsText;
                DateTime.TryParse(editDateStringFromDoc, out editDate);
                if(type == "comments" && editDate == DateTime.MinValue) {

                    // if editDate is still min, we didn't find an edit date and need to use post date
                    DateTime.TryParse(revision["date.posted"].AsText, out editDate);
                }
                if(editDate != DateTime.MinValue) {
                    var editDateString = editDate.ToUniversalTime().ToString("yyyyMMddHHmmss", System.Globalization.CultureInfo.InvariantCulture.DateTimeFormat);
                    d.Add(new Field("date.edited", editDateString, Field.Store.YES, Field.Index.UN_TOKENIZED));
                }
                string language = null;
                switch(type) {
                case "pages": {

                        // filter what we actually index
                        var ns = revision["namespace"].AsText;
                        if(Array.IndexOf(_indexNamespaceWhitelist, ns) < 0) {
                            _log.DebugFormat("not indexing '{0}', namespace '{1}' is not in whitelist", data.Id, ns);
                            result.Return();
                            yield break;
                        }
                        string path = revision["path"].AsText ?? string.Empty;
                        d.Add(new Field("path", path, Field.Store.YES, Field.Index.TOKENIZED));
                        d.Add(new Field("id.page", revision["@id"].AsText ?? "0", Field.Store.YES, Field.Index.UN_TOKENIZED));
                        d.Add(new Field("title", revision["title"].AsText ?? string.Empty, Field.Store.YES, Field.Index.TOKENIZED));
                        d.Add(new Field("title.sort", revision["title"].AsText ?? string.Empty, Field.Store.NO, Field.Index.UN_TOKENIZED));
                        d.Add(new Field("namespace", ns ?? string.Empty, Field.Store.NO, Field.Index.UN_TOKENIZED));
                        d.Add(new Field("type", "wiki", Field.Store.YES, Field.Index.UN_TOKENIZED));
                        d.Add(new Field("author", revision["user.author/username"].AsText ?? string.Empty, Field.Store.YES, Field.Index.TOKENIZED));
                        d.Add(new Field("author.sort", revision["user.author/username"].AsText ?? string.Empty, Field.Store.NO, Field.Index.UN_TOKENIZED));

                        // store the original page title in case display title was set
                        int index = path.LastIndexOf('/');
                        if(index > 0) {
                            path = path.Substring(index + 1);
                        }
                        d.Add(new Field("path.title", path, Field.Store.YES, Field.Index.TOKENIZED));

                        var pageUri = data.Meta["uri"].AsUri;
                        _log.DebugFormat("fetching page info: {0}", pageUri);
                        Result<DreamMessage> pageResult;
                        yield return pageResult = Plug.New(pageUri).With("apikey", _apikey).GetAsync();
                        DreamMessage page = pageResult.Value;
                        if(!page.IsSuccessful) {
                            throw BadRequestException(page, "unable to fetch page data from '{0}' for '{1}'", contentUri, data.Id);
                        }
                        XDoc pageDoc = page.ToDocument();
                        var score = pageDoc["rating/@score"].AsText;
                        if(!string.IsNullOrEmpty(score)) {
                            d.Add(new Field("rating.score", score, Field.Store.YES, Field.Index.UN_TOKENIZED));
                        }
                        d.Add(new Field("creator", pageDoc["user.createdby/username"].AsText ?? string.Empty, Field.Store.YES, Field.Index.UN_TOKENIZED));
                        d.Add(new Field("date.created", DateTimeToString(pageDoc["date.created"].AsDate), Field.Store.YES, Field.Index.UN_TOKENIZED));
                        d.Add(new Field("rating.count", pageDoc["rating/@count"].AsText ?? "0", Field.Store.YES, Field.Index.UN_TOKENIZED));
                        d.Add(new Field("title.parent", pageDoc["page.parent/title"].AsText ?? "", Field.Store.YES, Field.Index.UN_TOKENIZED));
                        d.Add(new Field("path.parent", pageDoc["page.parent/path"].AsText ?? "", Field.Store.YES, Field.Index.UN_TOKENIZED));
                        foreach(var ancestor in pageDoc["//page.parent/path"]) {
                            var ancestorPath = ancestor.AsText;
                            if(string.IsNullOrEmpty(ancestorPath)) {
                                continue;
                            }
                            d.Add(new Field("path.ancestor", ancestorPath, Field.Store.YES, Field.Index.UN_TOKENIZED));
                        }
                        var parentId = pageDoc["page.parent/@id"].AsUInt;
                        if(parentId.HasValue) {
                            d.Add(new Field("id.parent", parentId.Value.ToString(), Field.Store.YES, Field.Index.UN_TOKENIZED));
                        }

                        // check if this is a redirect
                        if(!pageDoc["page.redirectedto"].IsEmpty) {

                            // redirect
                            if(!(Config["index-redirects"].AsBool ?? false)) {
                                _log.DebugFormat("indexing of redirects is disabled, not indexing '{0}'", data.Id);
                                result.Return();
                                yield break;
                            }
                            _log.DebugFormat("indexing redirect, leave content empty");
                            d.Add(new Field("size", "0", Field.Store.YES, Field.Index.UN_TOKENIZED));
                        } else {
                            language = pageDoc["language"].AsText;

                            // fetch the page
                            _log.DebugFormat("fetching page content: {0}", contentUri);
                            DreamMessage content = null;
                            yield return Plug.New(contentUri).With("apikey", _apikey).WithTimeout(TimeSpan.FromMinutes(10))
                                .Get(new Result<DreamMessage>())
                                .Set(x => content = x);
                            if(!content.IsSuccessful) {
                                throw BadRequestException(content, "unable to fetch content from '{0}' for '{1}'", contentUri, data.Id);
                            }
                            text = _htmlConverter.Convert(content.ToDocument());
                            d.Add(new Field("size", content.ContentLength.ToString(), Field.Store.YES, Field.Index.UN_TOKENIZED));
                        }

                        // process tags, if they exist
                        if(!data.Meta["tags.uri"].IsEmpty) {
                            Result<DreamMessage> tagsResult;
                            yield return tagsResult = Plug.New(data.Meta["tags.uri"].AsUri).With("apikey", _apikey).GetAsync();
                            if(!tagsResult.Value.IsSuccessful) {
                                throw BadRequestException(tagsResult.Value, "unable to fetch tags from '{0}' for '{1}'", data.Meta["tags.uri"].AsText, data.Id);
                            }
                            XDoc tags = tagsResult.Value.ToDocument();
                            StringBuilder sb = new StringBuilder();
                            foreach(XDoc v in tags["tag/@value"]) {
                                sb.AppendFormat("{0}\n", v.AsText);
                            }
                            d.Add(new Field("tag", sb.ToString(), Field.Store.YES, Field.Index.TOKENIZED));
                        }

                        //Save page properties
                        yield return Coroutine.Invoke(AddPropertiesToDocument, d, pageDoc["properties"], new Result());

                        // set docuemnt boost based on namespace
                        d.SetBoost(GetNamespaceBoost(revision["namespace"].AsText));
                        break;
                    }
                case "files": {
                        var ns = revision["page.parent/namespace"].AsText;
                        if(Array.IndexOf(_indexNamespaceWhitelist, ns) < 0) {
                            _log.DebugFormat("not indexing '{0}', namespace '{1}' is not in whitelist", data.Id, ns);
                            result.Return();
                            yield break;
                        }
                        d.Add(new Field("namespace", ns ?? string.Empty, Field.Store.NO, Field.Index.UN_TOKENIZED));
                        var filename = revision["filename"].AsText;
                        string extension = Path.GetExtension(filename);
                        d.Add(new Field("path", revision["page.parent/path"].AsText ?? string.Empty, Field.Store.YES, Field.Index.TOKENIZED));
                        d.Add(new Field("title.page", revision["page.parent/title"].AsText ?? string.Empty, Field.Store.YES, Field.Index.TOKENIZED));
                        d.Add(new Field("id.page", revision["page.parent/@id"].AsText ?? "0", Field.Store.YES, Field.Index.UN_TOKENIZED));
                        d.Add(new Field("id.file", revision["@id"].AsText ?? "0", Field.Store.YES, Field.Index.UN_TOKENIZED));
                        d.Add(new Field("extension", extension ?? string.Empty, Field.Store.NO, Field.Index.TOKENIZED));
                        d.Add(new Field("filename", filename ?? string.Empty, Field.Store.NO, Field.Index.TOKENIZED));
                        d.Add(new Field("title", filename ?? string.Empty, Field.Store.YES, Field.Index.TOKENIZED));
                        d.Add(new Field("title.sort", filename ?? string.Empty, Field.Store.NO, Field.Index.UN_TOKENIZED));
                        d.Add(new Field("author", revision["user.createdby/username"].AsText ?? string.Empty, Field.Store.YES, Field.Index.TOKENIZED));
                        d.Add(new Field("author.sort", revision["user.createdby/username"].AsText ?? string.Empty, Field.Store.NO, Field.Index.UN_TOKENIZED));
                        d.Add(new Field("description", revision["description"].AsText ?? string.Empty, Field.Store.YES, Field.Index.TOKENIZED));
                        d.Add(new Field("type", GetDocumentType(extension), Field.Store.YES, Field.Index.UN_TOKENIZED));

                        // convert binary types to text
                        Result<Tuplet<string, int>> contentResult;
                        yield return contentResult = Coroutine.Invoke(ConvertToText, extension, new XUri(contentUri), new Result<Tuplet<string, int>>());
                        Tuplet<string, int> content = contentResult.Value;
                        text = content.Item1;
                        var size = content.Item2;
                        if(size == 0) {

                            // since ConvertToText only gets the byte size if there is a converter for the filetype,
                            // we fall back to the size in the document if it comes back as zero
                            size = revision["contents/@size"].AsInt ?? 0;
                        }
                        d.Add(new Field("size", size.ToString(), Field.Store.YES, Field.Index.UN_TOKENIZED));

                        break;
                    }
                case "comments": {
                        var ns = revision["page.parent/namespace"].AsText;
                        if(Array.IndexOf(_indexNamespaceWhitelist, ns) < 0) {
                            _log.DebugFormat("not indexing '{0}', namespace '{1}' is not in whitelist", data.Id, ns);
                            result.Return();
                            yield break;
                        }
                        d.Add(new Field("namespace", ns ?? string.Empty, Field.Store.NO, Field.Index.UN_TOKENIZED));
                        text = revision["content"].AsText ?? string.Empty;
                        d.Add(new Field("comments", text, Field.Store.YES, Field.Index.TOKENIZED));
                        d.Add(new Field("type", "comment", Field.Store.YES, Field.Index.UN_TOKENIZED));
                        d.Add(new Field("path", revision["page.parent/path"].AsText ?? string.Empty, Field.Store.YES, Field.Index.TOKENIZED));
                        d.Add(new Field("id.page", revision["page.parent/@id"].AsText ?? "0", Field.Store.YES, Field.Index.UN_TOKENIZED));
                        d.Add(new Field("title.page", revision["page.parent/title"].AsText ?? string.Empty, Field.Store.YES, Field.Index.UN_TOKENIZED));
                        d.Add(new Field("id.comment", revision["@id"].AsText ?? "0", Field.Store.YES, Field.Index.UN_TOKENIZED));
                        string title = "Comment #" + revision["number"].AsInt;
                        d.Add(new Field("title", title, Field.Store.YES, Field.Index.TOKENIZED));
                        d.Add(new Field("title.sort", title, Field.Store.NO, Field.Index.UN_TOKENIZED));
                        string author = revision["user.editedby/username"].AsText ?? revision["user.createdby/username"].AsText ?? "";
                        d.Add(new Field("author", author, Field.Store.YES, Field.Index.TOKENIZED));
                        d.Add(new Field("author.sort", author, Field.Store.NO, Field.Index.UN_TOKENIZED));
                        break;
                    }

                case "users": {
                        d.Add(new Field("type", "user", Field.Store.YES, Field.Index.UN_TOKENIZED));
                        d.Add(new Field("id.user", revision["@id"].AsText, Field.Store.YES, Field.Index.UN_TOKENIZED));
                        d.Add(new Field("username", revision["username"].AsText, Field.Store.YES, Field.Index.UN_TOKENIZED));
                        d.Add(new Field("email", revision["email"].AsText ?? string.Empty, Field.Store.NO, Field.Index.UN_TOKENIZED));
                        var fullname = revision["fullname"].AsText ?? string.Empty;
                        d.Add(new Field("fullname", fullname, Field.Store.YES, Field.Index.ANALYZED));
                        d.Add(new Field("fullname.sort", fullname, Field.Store.NO, Field.Index.NOT_ANALYZED));
                        d.Add(new Field("date.lastlogin", DateTimeToString(revision["date.lastlogin"].AsDate), Field.Store.NO, Field.Index.UN_TOKENIZED));
                        d.Add(new Field("date.created", DateTimeToString(revision["date.created"].AsDate), Field.Store.YES, Field.Index.UN_TOKENIZED));
                        d.Add(new Field("language", revision["language"].AsText ?? string.Empty, Field.Store.NO, Field.Index.UN_TOKENIZED));
                        d.Add(new Field("service.authentication.id", revision["service.authentication/@id"].AsText ?? string.Empty, Field.Store.NO, Field.Index.UN_TOKENIZED));

                        foreach(XDoc group in revision["groups/group"]) {
                            d.Add(new Field("group.id", group["@id"].AsText ?? string.Empty, Field.Store.NO, Field.Index.UN_TOKENIZED));
                            d.Add(new Field("group", group["groupname"].AsText ?? string.Empty, Field.Store.NO, Field.Index.UN_TOKENIZED));
                        }

                        // NOTE (MaxM): User properties are only automatically included for current user so they need to be retrieved.
                        Result<DreamMessage> propertyResult;
                        yield return propertyResult = Plug.New(revisionUri).At("properties").With("apikey", _apikey).GetAsync();
                        if(!propertyResult.Value.IsSuccessful) {
                            throw BadRequestException(propertyResult.Value, "unable to fetch properties for user id '{0}' for '{1}'", revision["@id"].AsText, data.Id);
                        }
                        XDoc propertiesDoc = propertyResult.Value.ToDocument();

                        // Save user properties
                        yield return Coroutine.Invoke(AddPropertiesToDocument, d, propertiesDoc, new Result());

                        break;
                    }
                }// switch(type)
                string preview = text;
                if(preview.Length > _previewLength) {
                    preview = preview.Substring(0, _previewLength);
                }
                d.Add(new Field("content", text, Field.Store.NO, Field.Index.TOKENIZED));
                d.Add(new Field("preview", preview, Field.Store.YES, Field.Index.TOKENIZED));
                d.Add(new Field("wordcount", _wordcountRegex.Matches(text).Count.ToString(), Field.Store.YES, Field.Index.UN_TOKENIZED));

                if(type == "files" || type == "comments") {

                    // fetch parent page for language
                    string parentUri = revision["page.parent/@href"].AsText;
                    if(!string.IsNullOrEmpty(parentUri)) {
                        Result<DreamMessage> parentResult;
                        yield return parentResult = Plug.New(parentUri).With("apikey", _apikey).GetAsync();
                        if(!parentResult.Value.IsSuccessful) {
                            throw new DreamBadRequestException(string.Format("unable to fetch parent from '{0}' for '{1}'", contentUri, data.Id));
                        }
                        XDoc parent = parentResult.Value.ToDocument();
                        language = parent["language"].AsText;
                    }
                }
                if(string.IsNullOrEmpty(language)) {
                    language = "neutral";
                }
                d.Add(new Field("language", language, Field.Store.YES, Field.Index.UN_TOKENIZED));
                _log.DebugFormat("Adding document for '{0}' to index", data.Id);
                GetInstance(wikiid).AddDocument(d);
            }
            _log.DebugFormat("completed indexing '{0}'", data.Id);
            result.Return();
        }

        private Exception BadRequestException(DreamMessage message, string msg, params object[] args) {
            if(_log.IsDebugEnabled && message.HasDocument) {
                _log.DebugFormat(msg + ":\r\n" + message.ToDocument(), args);
            }
            return new DreamBadRequestException(string.Format(msg, args));
        }

        private float GetNamespaceBoost(string ns) {
            if(_namespaceBoost.ContainsKey(ns))
                return _namespaceBoost[ns];
            return 1F;
        }

        // BUGBUGBUG: this is a total hack...
        private string GetDocumentType(string extension) {
            string type = "";
            switch(extension.ToLowerInvariant()) {
            case ".pdf":
            case ".doc":
            case ".docx":
            case ".odp":
            case ".ppt":
            case ".pptx":
            case ".xls":
            case ".txt":
            case ".csv":
                type = "document";
                break;
            case ".jpg":
            case ".png":
            case ".gif":
            case ".svg":
            case ".bmp":
                type = "image";
                break;
            default:
                type = "binary";
                break;
            }
            return type;
        }

        private Yield ConvertToText(string extension, XUri contentUri, Result<Tuplet<string, int>> result) {
            Tuplet<string, int> value = new Tuplet<string, int>(string.Empty, 0);
            SearchFilter filter;

            _searchFilters.TryGetValue(extension, out filter);
            if(filter == null) {

                // see if a wildcard was defined
                _searchFilters.TryGetValue(".*", out filter);
            }
            if(filter != null) {

                // fetch content from source
                Result<DreamMessage> contentResult;
                yield return contentResult = Plug.New(contentUri).With("apikey", _apikey).InvokeEx("GET", DreamMessage.Ok(), new Result<DreamMessage>());
                DreamMessage content = contentResult.Value;
                if(!content.IsSuccessful) {
                    content.Close();
                    throw new DreamBadRequestException(string.Format("unable to fetch content from '{0}", contentUri));
                }
                value.Item2 = (int)content.ContentLength;

                // check filter type
                if(filter.FileName == string.Empty) {

                    // file is already in text format
                    value.Item1 = content.AsText();
                } else {

                    // convert source document to text
                    Stream output = null;
                    Stream error = null;

                    // invoke converter
                    string processArgs = string.Format(PhpUtil.ConvertToFormatString(filter.Arguments), extension);
                    _log.DebugFormat("executing: {0} {1}", filter.FileName, processArgs);
                    Result<Tuplet<int, Stream, Stream>> exitResult;

                    // TODO (steveb): use WithCleanup() to dispose of resources in case of failure
                    yield return exitResult = Async.ExecuteProcess(filter.FileName, processArgs, content.AsStream(), new Result<Tuplet<int, Stream, Stream>>(_filterTimeout)).Catch();
                    content.Close();
                    if(exitResult.HasException) {
                        result.Throw(exitResult.Exception);
                        yield break;
                    }
                    try {
                        Tuplet<int, Stream, Stream> exitValues = exitResult.Value;
                        int status = exitValues.Item1;
                        output = exitValues.Item2;
                        error = exitValues.Item3;

                        // check if converter was successful
                        if(status == 0) {

                            // capture converter output as text for indexing
                            using(StreamReader sr = new StreamReader(output)) {
                                value.Item1 = sr.ReadToEnd();
                            }
                        } else {

                            // log convert error
                            string stderr = string.Empty;
                            try {
                                using(StreamReader sr = new StreamReader(error)) {
                                    stderr = sr.ReadToEnd();
                                }
                            } catch {
                                stderr = "(unabled to read stderr from converter)";
                            }
                            _log.WarnFormat("error converting content at '{0}', exitCode: {1}, stderr: {2}", contentUri, status, stderr);
                        }
                    } finally {

                        // make sure the output stream gets closed
                        try {
                            if(output != null) {
                                output.Close();
                            }
                        } catch { }

                        // make sure the error stream gets closed
                        try {
                            if(error != null) {
                                error.Close();
                            }
                        } catch { }
                    }
                }
            }
            result.Return(value);
            yield break;
        }

        private void ConvertToXDoc(XDoc doc, LuceneResult result) {
            var d = result.Document;
            doc.Start("document");
            foreach(Field field in d.GetFields()) {
                if(field.IsStored()) {
                    doc.Elem(System.Xml.XmlConvert.EncodeLocalName(field.Name()), field.StringValue());
                }
            }
            doc.Elem("score", result.Score);
            doc.End();
        }

        private void ConvertToCompactXDoc(XDoc doc, LuceneResult result) {
            var d = result.Document;
            doc.Start("document");
            AddField(doc, d, "id.page");
            AddField(doc, d, "id.file");
            AddField(doc, d, "id.comment");
            AddField(doc, d, "id.user");
            AddField(doc, d, "title");
            AddField(doc, d, "date.edited");
            AddField(doc, d, "rating.score");
            AddField(doc, d, "rating.count");
            doc.Elem("score", result.Score);
            doc.End();
        }

        private void AddField(XDoc xdoc, Document document, string fieldName) {
            var field = document.GetField(fieldName);
            if(field == null) {
                return;
            }
            xdoc.Elem(System.Xml.XmlConvert.EncodeLocalName(field.Name()), field.StringValue());
        }

        private bool CheckForInstance(string wikiid) {
            _log.DebugFormat("checking for index for '{0}'", wikiid);
            lock(_instances) {
                var entry = _instances[wikiid];
                if(entry != null) {
                    _log.DebugFormat("index for '{0}' already initialized", wikiid);
                    return true;
                }
                var indexPath = string.Format(_indexPath, wikiid);
                var exists = Directory.Exists(indexPath) && Directory.GetFiles(indexPath).Length > 0;
                _log.DebugFormat("initializing index for '{0}'", wikiid);
                GetInstance(wikiid);
                if(exists) {
                    _log.DebugFormat("index for '{0}' existed", wikiid);
                    return true;
                }
                _log.DebugFormat("no index for '{0}' existed", wikiid);
                return false;
            }
        }

        private SearchInstanceData GetInstance(string wikiid) {
            SearchInstanceData instance;
            lock(_instances) {
                var entry = _instances[wikiid];
                if(entry == null) {
                    var indexPath = string.Format(_indexPath, wikiid);
                    var queuePath = string.Format(_queuePathBase, wikiid);
                    var persistentQueue = new TransactionalQueue<XDoc>(new MultiFileQueueStream(queuePath), new XDocQueueItemSerializer());
                    var queue = new UpdateDelayQueue(_indexAccumulationTime, new UpdateRecordDispatcher(OnQueueExpire, _indexerParallelism, _indexerMaxRetry, _indexerRetrySleep), persistentQueue);
                    instance = new SearchInstanceData(indexPath, _defaultAnalyzer, queue, _instanceCommitInterval, TimerFactory);
                    _instances.Set(wikiid, instance, _instanceTtl);
                    _log.DebugFormat("created instance '{0}'", wikiid);
                } else {
                    instance = entry.Value;
                }
            }
            return instance;
        }

        private IEnumerable<SearchInstanceData> GetInstances() {
            lock(_instances) {
                return (from entries in _instances select entries.Value).ToArray();
            }
        }

        private Yield AddPropertiesToDocument(Document d, XDoc propertiesDoc, Result result) {
            if(propertiesDoc != null && !propertiesDoc.IsEmpty) {
                foreach(XDoc pagePropDoc in propertiesDoc["property"]) {
                    string propName = pagePropDoc["@name"].AsText;
                    string content = string.Empty;

                    if(!string.IsNullOrEmpty(propName)) {
                        if(propName.StartsWithInvariantIgnoreCase(CUSTOM_PROPERTY_NAMESPACE)) {

                            // Custom properties are indexed as #foo
                            string[] nameSegments = propName.Split(new char[] { '#' }, StringSplitOptions.RemoveEmptyEntries);
                            propName = '#' + nameSegments[nameSegments.Length - 1];
                        }
                        propName = propName.Replace(' ', '_');

                        // Retrieve property content
                        XDoc contentsDoc = pagePropDoc["contents"];
                        content = contentsDoc.AsText;
                        if(string.IsNullOrEmpty(content)
                            && (contentsDoc["@size"].AsLong ?? 0) > 0
                            && !contentsDoc["@type"].IsEmpty
                            && new MimeType(contentsDoc["@type"].AsText).Match(MimeType.ANY_TEXT)) {

                            // Retrieve the property contents if it's text and has a length but wasn't included in the property summary document
                            XUri contentsUri = contentsDoc["@href"].AsUri;
                            Result<DreamMessage> propContentsResult;
                            yield return propContentsResult = Plug.New(contentsUri).With("apikey", _apikey).GetAsync();
                            if(!propContentsResult.Value.IsSuccessful) {
                                throw new DreamBadRequestException(string.Format("unable to fetch property contents from '{0}", contentsUri));
                            }
                            content = propContentsResult.Value.AsText();
                        }
                        if(!string.IsNullOrEmpty(content)) {
                            d.Add(new Field(propName, content, Field.Store.NO, Field.Index.TOKENIZED));
                        }

                        // Add each propertyname to the 'property' field for tag-like behavior.
                        d.Add(new Field("property", propName, Field.Store.YES, Field.Index.UN_TOKENIZED));
                    }
                }
            }
            result.Return();
        }

        private string DateTimeToString(DateTime? time) {
            if(time == null || time == DateTime.MinValue) {
                return string.Empty;
            }
            return time.Value.ToUniversalTime().ToString("yyyyMMddHHmmss", System.Globalization.CultureInfo.InvariantCulture.DateTimeFormat);
        }
    }
}

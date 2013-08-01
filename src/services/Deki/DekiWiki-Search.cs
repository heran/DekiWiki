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
using System.Text;
using System.IO;
using System.Threading;
using MindTouch.Deki.Data;
using MindTouch.Deki.Exceptions;
using MindTouch.Deki.Logic;
using MindTouch.Deki.Search;
using MindTouch.Dream;
using MindTouch.Tasking;
using MindTouch.Xml;
using MindTouch.Extensions.Time;

namespace MindTouch.Deki {
    using Yield = IEnumerator<IYield>;

    public partial class DekiWikiService {

        //--- Features ---
        [DreamFeature("GET:site/opensearch/description", "Get the OpenSearch Description document")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        public Yield GetSearchDescription(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            var resources = DekiContext.Current.Resources;
            XDoc description = new XDoc("OpenSearchDescription", "http://a9.com/-/spec/opensearch/1.1/");
            description.Elem("ShortName", resources.Localize(DekiResources.OPENSEARCH_SHORTNAME(DekiContext.Current.Instance.SiteName)))
                       .Elem("Description", resources.Localize(DekiResources.OPENSEARCH_DESCRIPTION()))
                       .Start("Query")
                            .Attr("role", "example")
                            .Attr("searchTerms", "Wiki")
                       .End();

            // HACK HACK HACK: we can't use XUri because it encodes the "{}" characters
            string uri = DekiContext.Current.ApiUri.At("site", "opensearch").ToString();
            uri += "?q={searchTerms}&offset={startIndex}&limit={count?}&";

            description.Start("Url")
                 .Attr("type", "text/html")
                 .Attr("indexOffset", 0)
                 .Attr("template", DekiContext.Current.UiUri.At("Special:Search").ToString() + "?search={searchTerms}&offset=0&limit={count?}&format=html")
            .End()
            .Start("Url")
                 .Attr("type", "application/atom+xml")
                 .Attr("indexOffset", 0)
                 .Attr("template", uri + "format=atom")
            .End()
            .Start("Url")
                 .Attr("type", "application/rss+xml")
                 .Attr("indexOffset", 0)
                 .Attr("template", uri + "format=rss")
            .End()
            .Start("Url")
                 .Attr("type", "application/x-suggestions+json")
                 .Attr("template", DekiContext.Current.ApiUri.At("site", "opensearch", "suggestions").ToString() + "?q={searchTerms}")
             .End();
            response.Return(DreamMessage.Ok(description));
            yield break;
        }

        [DreamFeature("GET:site/opensearch", "Search the site index")]
        [DreamFeatureParam("format", "{rss | atom}?", "search output format (rss | atom) default: atom")]
        [DreamFeatureParam("q", "string", "lucene search string")]
        [DreamFeatureParam("limit", "string?", "Maximum number of items to retrieve. Must be a positive number or 'all' to retrieve all items. (default: 100)")]
        [DreamFeatureParam("offset", "int?", "Number of items to skip. Must be a positive number or 0 to not skip any. (default: 0)")]
        [DreamFeatureParam("sortby", SearchBL.SORT_FIELDS_DESCRIPTION, "Sort field. Prefix value with '-' to sort descending. default: -score")]
        [DreamFeatureParam("constraint", "string?", "Additional search constraint (ex: language:en-us AND type:wiki) default: none")]
        [DreamFeatureParam("parser", "{bestguess|term|filename|lucene}?", "The parser to use for the query  (default: bestguess)")]
        [DreamFeatureParam("nocache", "bool?", "Use caching search path (better for paging results)  (default: false)")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        [DreamFeatureStatus(DreamStatus.Ok, "The request completed successfully")]
        [DreamFeatureStatus(DreamStatus.BadRequest, "Invalid input parameter or request body")]
        public Yield OpenSearch(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            bool useCache = context.Uri.GetParam("nocache", "false").EqualsInvariantIgnoreCase("false");
            string format = context.GetParam("format", "atom");
            string query = context.GetParam("q");
            string constraint = context.GetParam("constraint", string.Empty);
            var parser = context.GetParam("parser", SearchQueryParserType.BestGuess);
            var discriminator = Utils.GetSetDiscriminatorFromRequest(context, 100, "-score");

            // get search results
            Result<XDoc> res;
            yield return res = Coroutine.Invoke(Search, query, discriminator, constraint, useCache, parser, new Result<XDoc>());
            XDoc luceneResults = res.Value;
            if(!luceneResults["@error"].IsEmpty) {
                throw new DreamBadRequestException(luceneResults["@error"].AsText);
            }
            XDoc results = null;
            switch(format) {
            case "rss":
                results = ConvertSearchResultsToOpenSearchRss(luceneResults, query, discriminator.Limit, discriminator.Offset);
                break;
            default:
                results = ConvertSearchResultsToOpenSearchAtom(luceneResults, query, discriminator.Limit, discriminator.Offset, format);
                break;
            }
            response.Return(DreamMessage.Ok(results));
            yield break;
        }

        [DreamFeature("GET:site/opensearch/suggestions", "Search the site index")]
        [DreamFeatureParam("q", "string", "lucene search string")]
        [DreamFeatureParam("sortby", SearchBL.SORT_FIELDS_DESCRIPTION, "Sort field. Prefix value with '-' to sort descending. default: -score")]
        [DreamFeatureParam("constraint", "string?", "Additional search constraint (ex: language:en-us AND type:wiki) default: none")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        [DreamFeatureStatus(DreamStatus.Ok, "The request completed successfully")]
        [DreamFeatureStatus(DreamStatus.BadRequest, "Invalid input parameter or request body")]
        public Yield OpenSearchSuggest(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            string query = context.GetParam("q");
            string sortBy = context.GetParam("sortBy", string.Empty);
            string constraint = context.GetParam("constraint", string.Empty);
            // get search results
            Result<XDoc> res;
            yield return res = Coroutine.Invoke(Search, "title:\"" + StringUtil.EscapeString(query) + "\"", (uint)100, (uint)0, sortBy, constraint, new Result<XDoc>());
            XDoc luceneResults = res.Value;
            if(!luceneResults["@error"].IsEmpty) {
                throw new DreamBadRequestException(luceneResults["@error"].AsText);
            }

            StringWriter result = new StringWriter();
            result.Write("[\"{0}\", [", StringUtil.EscapeString(query));
            bool firstResult = true;
            foreach(XDoc title in luceneResults["document/title"]) {
                if(!firstResult)
                    result.Write(", ");
                firstResult = false;
                result.Write("\"{0}\"", StringUtil.EscapeString(title.AsText));

            }
            result.Write("]]");
            response.Return(DreamMessage.Ok(MimeType.JSON, result.ToString()));
            yield break;
        }

        [DreamFeature("GET:site/search", "Search the site index")]
        [DreamFeatureParam("format", "{xml | search}?", "search output format (xml | search) default: xml")]
        [DreamFeatureParam("q", "string", "lucene search string")]
        [DreamFeatureParam("limit", "string?", "Maximum number of items to retrieve. Must be a positive number or 'all' to retrieve all items. (default: 100)")]
        [DreamFeatureParam("offset", "int?", "Number of items to skip. Must be a positive number or 0 to not skip any. (default: 0)")]
        [DreamFeatureParam("sortby", SearchBL.SORT_FIELDS_DESCRIPTION, "Sort field. Prefix value with '-' to sort descending. default: -score")]
        [DreamFeatureParam("constraint", "string?", "Additional search constraints (ex: language:en-us AND type:wiki) default: none")]
        [DreamFeatureParam("verbose", "bool?", "show verbose page xml. default: true")]
        [DreamFeatureParam("parser", "{bestguess|term|filename|lucene}?", "The parser to use for the query  (default: bestguess)")]
        [DreamFeatureParam("nocache", "bool?", "Use caching search path (better for paging results)  (default: false)")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        [DreamFeatureStatus(DreamStatus.Ok, "The request completed successfully")]
        [DreamFeatureStatus(DreamStatus.BadRequest, "Invalid input parameter or request body")]
        public Yield SearchIndex(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            bool useCache = context.Uri.GetParam("nocache", "false").EqualsInvariantIgnoreCase("false");
            string format = context.GetParam("format", "xml");
            string query = context.GetParam("q");
            string constraint = context.GetParam("constraint", string.Empty);
            bool verbose = context.GetParam("verbose", true);
            var parser = context.GetParam("parser", SearchQueryParserType.BestGuess);
            var discriminator = Utils.GetSetDiscriminatorFromRequest(context, 100, "-score");
            // get search results
            Result<XDoc> res;
            yield return res = Coroutine.Invoke(Search, query, discriminator, constraint, useCache, parser, new Result<XDoc>());
            XDoc luceneResults = res.Value;
            if(!luceneResults["@error"].IsEmpty) {
                throw new DreamBadRequestException(luceneResults["@error"].AsText);
            }
            XDoc results = null;
            switch(format.ToLowerInvariant()) {
            case "search":
                results = ConvertSearchResultsToSearchFormat(luceneResults);
                break;
            default:
                results = ConvertSearchResultsToXmlFormat(luceneResults, verbose);
                break;
            }
            response.Return(DreamMessage.Ok(results));
            yield break;
        }

        [DreamFeature("GET:site/query", "Search the site index with analytical tracking. Primarily for user facing search.")]
        [DreamFeatureParam("q", "string", "lucene search string")]
        [DreamFeatureParam("parser", "{bestguess|term|lucene}?", "The parser to use for the query  (default: bestguess)")]
        [DreamFeatureParam("queryid", "ulong?", "Query tracking id returned by original query result (used for paging/changing sort order)")]
        [DreamFeatureParam("previousqueryid", "ulong?", "Query tracking id of previous query, if this is a follow-up query")]
        [DreamFeatureParam("limit", "int?", "Maximum number of items to retrieve. Must be a positive number (default: 25)")]
        [DreamFeatureParam("offset", "int?", "Number of items to skip. Must be a positive number or 0 to not skip any. (default: 0)")]
        [DreamFeatureParam("sortby", "{rank, title, modified}?", "Sort field. Prefix value with '-' to sort descending. (default: -rank)")]
        [DreamFeatureParam("constraint", "string?", "Additional search constraints (ex: language:en-us AND type:wiki) default: none")]
        [DreamFeatureParam("explain", "bool?", "Include ranking details (default: false)")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        [DreamFeatureStatus(DreamStatus.Ok, "The request completed successfully")]
        [DreamFeatureStatus(DreamStatus.BadRequest, "Invalid input parameter or request body")]
        public Yield GetRankedSearch(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            bool explain = !context.Uri.GetParam("explain", "false").EqualsInvariantIgnoreCase("false");
            if(explain) {
                explain = DetermineAccess(context, request) == DreamAccess.Internal;
            }
            string queryString = context.GetParam("q");

            // Note (arnec): BUG 7972: want date.created support, which can't happen until lucene indexes it, which can't happen until the 
            // API exposes it, which can't happen until we actuallly can get at it simply for pages (i.e. Resource model)
            var discriminator = Utils.GetSetDiscriminatorFromRequest(context, 25, "-rank");
            var queryId = context.GetParam<ulong>("queryid", 0);
            var previousQueryId = context.GetParam<ulong>("previousqueryid", 0);
            string constraint = context.GetParam("constraint", string.Empty);
            var trackingInfo = new TrackingInfo() {
                QueryId = queryId == 0 ? null : (ulong?)queryId,
                PreviousQueryId = previousQueryId == 0 ? null : (ulong?)previousQueryId
            };
            var parser = context.GetParam("parser", SearchQueryParserType.BestGuess);
            // TODO (arnec): throw on unsupported type

            // get search results
            XDoc results = null;
            var search = Resolve<ISearchBL>(context);
            var query = search.BuildQuery(queryString, constraint, parser, DekiContext.Current.IsValidApiKeyInRequest);
            yield return Coroutine.Invoke(CachedSearch, search, query, discriminator, explain, trackingInfo, new Result<XDoc>())
                .Set(x => results = x);
            response.Return(DreamMessage.Ok(results));
            yield break;
        }
#if DEBUG
        [DreamFeature("GET:site/query/{queryid}", "Register a tracked search result selection.")]
#endif
        [DreamFeature("POST:site/query/{queryid}", "Register a tracked search result selection.")]
        [DreamFeatureParam("position", "ushort?", "Search result position")]
        [DreamFeatureParam("rank", "double", "Search result rank")]
        [DreamFeatureParam("pageid", "uint", "Page id where the result occured")]
        [DreamFeatureParam("type", "{page,file,comment}", "Type of result")]
        [DreamFeatureParam("typeid", "uint?", "Id of type (if not 'page'")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        [DreamFeatureStatus(DreamStatus.Ok, "The request completed successfully")]
        [DreamFeatureStatus(DreamStatus.BadRequest, "Invalid input parameter or request body")]
        public Yield PostSearchResultAnalytics(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            var queryId = context.GetParam<ulong>("queryid");
            var position = context.GetParam<ushort>("position", 0);
            var rank = context.GetParam<double>("rank");
            var pageId = context.GetParam<uint>("pageid");
            var typeString = context.GetParam("type", null);
            var type = SysUtil.ParseEnum<SearchResultType>(typeString);
            var typeId = context.GetParam<uint>("typeid", 0);
            var searchAnalytics = Resolve<ISearchBL>(context);
            searchAnalytics.TrackQueryResultPick(queryId, rank, position, pageId, type, typeId == 0 ? null : (uint?)typeId);
            response.Return(DreamMessage.Ok());
            yield break;
        }

        [DreamFeature("GET:site/query/log", "Get tracked search data.")]
        [DreamFeatureParam("query", "string?", "Find searches using the given query term string (overrides 'term')")]
        [DreamFeatureParam("term", "string?", "Find searches containing a specific term.")]
        [DreamFeatureParam("groupby", "{query}?", "Aggregate by query")]
        [DreamFeatureParam("lowquality", "bool?", "Find searches that have 0 or more than 3 result")]
        [DreamFeatureParam("since", "string?", "Start date for result set.  Date is provided in 'yyyyMMddHHmmss' format (default: one month ago).")]
        [DreamFeatureParam("before", "string?", "End date for result set.  Date is provided in 'yyyyMMddHHmmss' format (default: now).")]
        [DreamFeatureParam("limit", "int?", "Maximum number of items to retrieve. Must be a positive number (default: 25)")]
        [DreamFeatureParam("offset", "int?", "Number of items to skip. Must be a positive number or 0 to not skip any. (default: 0)")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        [DreamFeatureStatus(DreamStatus.Ok, "The request completed successfully")]
        [DreamFeatureStatus(DreamStatus.BadRequest, "Invalid input parameter or request body")]
        internal Yield GetTrackedSearches(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            uint limit, offset;
            Utils.GetOffsetAndCountFromRequest(context, 25, out limit, out offset);
            var since = DbUtils.ToDateTime(context.GetParam("since", DbUtils.ToString(DekiContext.Current.Now.AddMonths(-1))));
            var before = DbUtils.ToDateTime(context.GetParam("before", DbUtils.ToString(DekiContext.Current.Now)));
            var query = context.GetParam("query", null);
            var term = context.GetParam("term", null);
            var grouped = context.GetParam("groupby", "").EndsWithInvariantIgnoreCase("query");
            var lowQuality = context.GetParam("lowquality", false);
            var searchAnalytics = Resolve<ISearchBL>(context);
            var type = SearchAnalyticsQueryType.All;
            if(query != null) {
                type = SearchAnalyticsQueryType.QueryString;
            } else if(!string.IsNullOrEmpty(term)) {
                type = SearchAnalyticsQueryType.Term;
                query = term;
            }
            if(grouped) {
                response.Return(DreamMessage.Ok(searchAnalytics.GetAggregatedQueriesXml(query, type, lowQuality, since, before, limit, offset)));
            } else {
                response.Return(DreamMessage.Ok(searchAnalytics.GetQueriesXml(query, type, lowQuality, since, before, limit, offset)));
            }
            yield break;
        }

        [DreamFeature("GET:site/query/log/terms", "Get tracked search terms")]
        [DreamFeatureParam("lowquality", "bool?", "Find searches that have 0 or more than 1 result")]
        [DreamFeatureParam("since", "string?", "Start date for result set.  Date is provided in 'yyyyMMddHHmmss' format (default: one month ago).")]
        [DreamFeatureParam("before", "string?", "End date for result set.  Date is provided in 'yyyyMMddHHmmss' format (default: now).")]
        [DreamFeatureParam("limit", "int?", "Maximum number of items to retrieve. Must be a positive number (default: 25)")]
        [DreamFeatureParam("offset", "int?", "Number of items to skip. Must be a positive number or 0 to not skip any. (default: 0)")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        [DreamFeatureStatus(DreamStatus.Ok, "The request completed successfully")]
        [DreamFeatureStatus(DreamStatus.BadRequest, "Invalid input parameter or request body")]
        internal Yield GetTrackedSearchTerms(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            uint limit, offset;
            Utils.GetOffsetAndCountFromRequest(context, 25, out limit, out offset);
            var since = DbUtils.ToDateTime(context.GetParam("since", DbUtils.ToString(DekiContext.Current.Now.AddMonths(-1))));
            var before = DbUtils.ToDateTime(context.GetParam("before", DbUtils.ToString(DekiContext.Current.Now)));
            var lowQuality = context.GetParam("lowquality", false);
            var searchAnalytics = Resolve<ISearchBL>(context);
            response.Return(DreamMessage.Ok(searchAnalytics.GetTermsXml(lowQuality, since, before, limit, offset)));
            yield break;
        }

        [DreamFeature("GET:site/query/log/{queryid}", "Get tracked search detail")]
        [DreamFeatureParam("queryid", "string", "Either the unique query id, or the aggregated, sorted querystring prefixed with an =")]
        [DreamFeatureParam("since", "string?", "Start date for querystring based aggregation.  Date is provided in 'yyyyMMddHHmmss' format (default: one month ago).")]
        [DreamFeatureParam("before", "string?", "End date for querystring based aggregation.  Date is provided in 'yyyyMMddHHmmss' format (default: now).")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        [DreamFeatureStatus(DreamStatus.Ok, "The request completed successfully")]
        [DreamFeatureStatus(DreamStatus.BadRequest, "Invalid input parameter or request body")]
        internal Yield GetTrackedSearchDetail(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            var query = context.GetParam("queryid");
            var searchAnalytics = Resolve<ISearchBL>(context);
            if(query.StartsWith("=")) {
                var since = DbUtils.ToDateTime(context.GetParam("since", DbUtils.ToString(DekiContext.Current.Now.AddMonths(-1))));
                var before = DbUtils.ToDateTime(context.GetParam("before", DbUtils.ToString(DekiContext.Current.Now)));
                var querystring = query.Substring(1);
                response.Return(DreamMessage.Ok(searchAnalytics.GetAggregateQueryXml(querystring, since, before)));
            } else {
                var queryId = SysUtil.ChangeType<ulong>(query);
                response.Return(DreamMessage.Ok(searchAnalytics.GetQueryXml(queryId)));
            }
            yield break;
        }

        [DreamFeature("GET:site/search/rebuild", "Return rebuild information")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        [DreamFeatureStatus(DreamStatus.Ok, "The request completed successfully")]
        [DreamFeatureStatus(DreamStatus.BadRequest, "Invalid input parameter or request body")]
        public Yield GetRebuildInformation(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            Result<DreamMessage> q;
            yield return q = DekiContext.Current.Deki.LuceneIndex.At("queue", "size").With("wikiid", DekiContext.Current.Instance.Id).GetAsync();
            var queueSize = 0;
            if(q.Value.IsSuccessful) {
                queueSize += q.Value.ToDocument()["size"].AsInt ?? 0;
            }
            response.Return(DreamMessage.Ok(new XDoc("search").Elem("pending", queueSize)));
            yield break;
        }

        [DreamFeature("POST:site/search/rebuild", "Rebuild the site index")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        [DreamFeatureStatus(DreamStatus.Ok, "The request completed successfully")]
        [DreamFeatureStatus(DreamStatus.Forbidden, "MindTouch API key or Administrator access is required.")]
        internal Yield RebuildIndex(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            DreamMessage resp = null;
            yield return LuceneIndex.At("clear").With("wikiid", DekiContext.Current.Instance.Id).Delete(new Result<DreamMessage>()).Set(x => resp = x);
            if(!resp.IsSuccessful) {
                throw new SearchIndexDeleteFatalException(resp.AsText());
            }
            var searchAnalytics = Resolve<ISearchBL>(context);
            searchAnalytics.ClearCache();
            Async.Fork(RebuildIndex_Helper);
            response.Return(DreamMessage.Ok());
        }

        [DreamFeature("POST:site/search/repair", "Repair the site index")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        [DreamFeatureParam("checkonly", "bool?", "Report on entities that need to be repaired, but do not execute repair (default: false)")]
        [DreamFeatureParam("verbose", "bool?", "Report on all entities considered, whether or not repair was required (default: false)")]
        [DreamFeatureStatus(DreamStatus.Ok, "The request completed successfully")]
        [DreamFeatureStatus(DreamStatus.Forbidden, "MindTouch API key or Administrator access is required.")]
        internal Yield RepairIndex(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            var indexrepair = Resolve<IIndexRebuilder>(context);
            var checkonly = context.GetParam("checkonly", false);
            var verbose = context.GetParam("verbose", false);
            Result<XDoc> report;
            yield return report = indexrepair.Repair(checkonly, verbose, new Result<XDoc>());
            response.Return(DreamMessage.Ok(report.Value));
        }

        //--- Methods ---
        private void RebuildIndex_Helper() {

            // Note (arnec): Thread.Sleep calls are required to throttle the re-index enqueing and stop it from making the site unusable
            try {
                Thread.Sleep(1.Seconds());
                _log.DebugFormat("starting re-index event queueing");
                var rebuilder = Resolve<IIndexRebuilder>(DreamContext.Current);
                rebuilder.Rebuild();
            } catch(Exception e) {
                _log.WarnExceptionMethodCall(e, "RebuildIndex_Helper", "rebuild died");
            }
        }

        private Yield Search(string queryString, uint max, uint offset, string sortBy, string constraint, Result<XDoc> result) {
            var discriminator = new SetDiscriminator() {
                Limit = max,
                Offset = offset,
            };
            discriminator.SetSortInfo(sortBy, "-score");
            return Search(queryString, discriminator, constraint, false, SearchQueryParserType.BestGuess, result);
        }

        private Yield Search(string queryString, SetDiscriminator discriminator, string constraint, bool useCache, SearchQueryParserType parser, Result<XDoc> result) {
            if(parser == SearchQueryParserType.Filename) {
                useCache = false;
            }
            switch(discriminator.SortField) {
            case "date":
                if(useCache) {
                    discriminator.SortField = "modified";
                }
                break;
            case "score":
                if(useCache) {
                    discriminator.SortField = "rank";
                }
                break;
            case "wordcount":
            case "size":
                useCache = false;
                break;
            }
            var search = Resolve<ISearchBL>(DreamContext.Current);
            var query = search.BuildQuery(queryString, constraint, parser, DekiContext.Current.IsValidApiKeyInRequest);
            Result<XDoc> docResult;
            if(useCache && query.Cacheable) {
                yield return docResult = Coroutine.Invoke(CachedSearch, search, query, discriminator, false, (TrackingInfo)null, new Result<XDoc>());
            } else {
                yield return docResult = Coroutine.Invoke(UnCachedSearch, query, discriminator, new Result<XDoc>());
            }
            result.Return(RewriteRedirectsAndUris(docResult.Value));
        }


        private Yield UnCachedSearch(SearchQuery query, SetDiscriminator discriminator, Result<XDoc> result) {
            if(query.IsEmpty) {
                result.Return(new XDoc("documents"));
                yield break;
            }

            // get search results
            DreamMessage msg = null;
            var context = DekiContext.Current;
            var searchPlug = context.Deki.LuceneIndex
                .With("q", query.LuceneQuery)
                .With("max", discriminator.Limit)
                .With("offset", discriminator.Offset)
                .With("sortBy", discriminator.SortBy ?? "")
                .With("wikiid", context.Instance.Id);
            if(!DekiContext.Current.IsValidApiKeyInRequest) {
                searchPlug = searchPlug
                    .With("userid", context.User.ID)
                    .With("apiuri", Self.Uri.AsPublicUri().ToString());
            }
            yield return searchPlug
                .Get(new Result<DreamMessage>())
                .Set(x => msg = x);
            if(!msg.IsSuccessful) {
                var resources = context.Resources;
                result.Return(new XDoc("documents").Attr("error", resources.Localize(DekiResources.ERROR_QUERYING_SEARCH_INDEX(query))));
                yield break;
            }
            result.Return(msg.ToDocument());
        }

        private Yield CachedSearch(ISearchBL search, SearchQuery query, SetDiscriminator discriminator, bool explain, TrackingInfo trackingInfo, Result<XDoc> result) {
            if(query.IsEmpty) {
                yield return search.FormatResultSet(new SearchResult(), discriminator, false, null, new Result<XDoc>())
                    .Set(result.Return);
                yield break;
            }
            var searchResultSet = search.GetCachedQuery(query);
            if(!explain && searchResultSet != null && (trackingInfo == null || trackingInfo.QueryId.HasValue)) {

                // we can only use the cached result set if we either don't have trackingInfo or the trackInfo has a queryId. Otherwise we have to re-query.
                yield return search.FormatResultSet(searchResultSet, discriminator, false, trackingInfo, new Result<XDoc>()).Set(result.Return);
                yield break;
            }

            // get search results
            DreamMessage msg = null;
            var context = DekiContext.Current;
            var searchPlug = context.Deki.LuceneIndex
                .At("compact")
                .With("q", query.LuceneQuery)
                .With("wikiid", context.Instance.Id);
            if(!DekiContext.Current.IsValidApiKeyInRequest) {
                searchPlug = searchPlug
                    .With("userid", context.User.ID)
                    .With("apiuri", Self.Uri.AsPublicUri().ToString());
            }
            yield return searchPlug
                .Get(new Result<DreamMessage>())
                .Set(x => msg = x);
            if(!msg.IsSuccessful) {
                var resources = context.Resources;
                result.Return(new XDoc("documents").Attr("error", resources.Localize(DekiResources.ERROR_QUERYING_SEARCH_INDEX(query))));
                yield break;
            }
            searchResultSet = search.CacheQuery(msg.ToDocument(), query, trackingInfo);
            yield return search.FormatResultSet(searchResultSet, discriminator, explain, trackingInfo, new Result<XDoc>())
                .Set(result.Return);
        }

        private XDoc RewriteRedirectsAndUris(XDoc searchResults) {
            DekiContext context = DekiContext.Current;

            // setup proper URI for file attachments and users
            foreach(XDoc node in searchResults["document"]) {
                try {
                    if(node["type"].AsText.EqualsInvariant("wiki")) {
                        node["uri"].ReplaceValue(Utils.AsPublicUiUri(Title.FromUriPath(node["path"].AsText)));
                    } else if(node["type"].AsText.EqualsInvariant("comment")) {
                        XUri commentUri = node["uri"].AsUri;
                        XUri publicCommentUri = new XUri(Utils.AsPublicUiUri(Title.FromUriPath(node["path"].AsText))).WithFragment(commentUri.Fragment);
                        node["uri"].ReplaceValue(publicCommentUri.ToString());
                    } else if(!node["id.file"].IsEmpty) {
                        node["uri"].ReplaceValue(context.ApiUri.At("files", node["id.file"].AsText, Title.AsApiParam(node["title"].AsText)));
                    } else if(!node["id.user"].IsEmpty) {
                        node["uri"].ReplaceValue(new XUri(Utils.AsPublicUiUri(Title.FromUIUsername(node["username"].AsText))));
                    }
                } catch(Exception e) {
                    _log.Warn(string.Format("Unable to generate UI uri for API Uri '{0}'", node["uri"].AsText), e);
                }
            }
            return searchResults;
        }

        private XDoc ConvertSearchResultsToXmlFormat(XDoc luceneResults, bool verbose) {
            XDoc results = new XDoc("search");
            results.Add(luceneResults["parsedQuery"]);

            // go over each document and extract id's that can be looked up in batch
            var pagesById = new Dictionary<ulong, PageBE>();
            var usersById = new Dictionary<uint, UserBE>();
            foreach(XDoc document in luceneResults["document"]) {
                if(!document["id.page"].IsEmpty) {
                    pagesById[document["id.page"].AsULong ?? 0] = null;

                } else if(!document["id.user"].IsEmpty) {
                    usersById[document["id.user"].AsUInt ?? 0] = null;
                }
            }
            if(pagesById.Keys.Count > 0) {
                pagesById = PageBL.GetPagesByIdsPreserveOrder(pagesById.Keys).ToDictionary(e => e.ID);
            }
            if(usersById.Keys.Count > 0) {
                usersById = DbUtils.CurrentSession.Users_GetByIds(usersById.Keys.ToList()).ToDictionary(e => e.ID);
            }

            foreach(XDoc document in luceneResults["document"]) {

                // if we have a <id.file> attribute we're an attachment
                if(!document["id.file"].IsEmpty) {
                    uint fileId = document["id.file"].AsUInt ?? 0;
                    ResourceBE file = ResourceBL.Instance.GetResource(fileId);
                    if(null != file) {
                        XDoc fileXml = AttachmentBL.Instance.GetFileXml(file, verbose, null, null);
                        fileXml.Attr("score", document["score"].AsFloat ?? 0);
                        results.Add(fileXml);
                    }
                } else if(!document["id.comment"].IsEmpty) {
                    uint commentId = document["id.comment"].AsUInt ?? 0;
                    CommentBE comment = CommentBL.GetComment(commentId);
                    if(null != comment) {
                        XDoc commentXml = CommentBL.GetCommentXml(comment, null);
                        commentXml.Attr("score", document["score"].AsFloat ?? 0);
                        results.Add(commentXml);
                    }
                } else if(!document["id.page"].IsEmpty) {
                    ulong pageId = document["id.page"].AsULong ?? 0;
                    PageBE page = null;
                    if(pagesById.TryGetValue(pageId, out page) && page != null) {
                        XDoc pageXml;
                        if(verbose) {
                            pageXml = PageBL.GetPageXmlVerbose(page, null);
                        } else {
                            pageXml = PageBL.GetPageXml(page, null);
                        }
                        pageXml.Attr("score", document["score"].AsFloat ?? 0);
                        results.Add(pageXml);
                    }
                } else if(!document["id.user"].IsEmpty) {
                    uint userId = document["id.user"].AsUInt ?? 0;
                    UserBE u = null;
                    if(usersById.TryGetValue(userId, out u) && u != null) {
                        XDoc userXml;
                        if(verbose) {
                            userXml = UserBL.GetUserXmlVerbose(u, null, Utils.ShowPrivateUserInfo(u), true, true);
                        } else {
                            userXml = UserBL.GetUserXml(u, null, Utils.ShowPrivateUserInfo(u));
                        }
                        userXml.Attr("score", document["score"].AsFloat ?? 0);
                        results.Add(userXml);
                    }
                } else {
                    // BUGBUGBUG:  we need to determine the schema for "external" documents
                    results.Add(document);
                }
            }
            return results;
        }

        private XDoc ConvertSearchResultsToSearchFormat(XDoc luceneResults) {
            XDoc results = new XDoc("search");
            results.Add(luceneResults["parsedQuery"]);
            foreach(XDoc document in luceneResults["document"]) {
                results.Start("result").AddAll(document.Elements).End();
            }
            return results;
        }

        private XAtomFeed ConvertSearchResultsToOpenSearchAtom(XDoc luceneResults, string query, uint limit, uint offset, string format) {
            string luceneNamespace = "dekilucene";
            int totalResults = 100000;
            bool firstPage = offset < limit;
            bool lastPage = luceneResults["document"].ToList().Count == 0 ? true : false;
            XUri searchUri = DekiContext.Current.ApiUri.At("site", "opensearch").With("q", query).With("format", format);
            XUri self = searchUri.With("offset", Convert.ToString(offset)).With("limit", Convert.ToString(limit));
            XAtomFeed feed = new XAtomFeed("MindTouch Search", self, DateTime.Now);
            feed.UsePrefix("opensearch", "http://a9.com/-/spec/opensearch/1.1/");
            feed.UsePrefix(luceneNamespace, "http://services.mindtouch.com/deki/draft/2007/06/luceneindex");
            feed.UsePrefix("relevance", "http://a9.com/-/opensearch/extensions/relevance/1.0/");
            feed.AddAuthor("MindTouch Core", null, string.Empty);
            feed.Id = self;
            feed.Elem("dekilucene:parsedQuery", luceneResults["parsedQuery"].AsText);

            // HACKHACKHACK show a fake <totalResults> until we run out
            if(!lastPage) {
                feed.Elem("opensearch:totalResults", totalResults);
            }

            if(offset >= limit)
                feed.Elem("opensearch:startIndex", offset);

            feed.Elem("opensearch:itemsPerPage", limit);
            feed.Start("opensearch:Query")
                .Attr("role", "request")
                .Attr("searchTerms", XUri.Encode(query))
                .Attr("startPage", "1")
            .End();
            feed.Start("link")
                .Attr("rel", "alternate")
                .Attr("type", MimeType.HTML.ToString())
                .Attr("href", DekiContext.Current.UiUri.At("Special:Search").With("search", query).With("search", query).With("format", "html").With("limit", limit).With("offset", offset))
            .End();
            feed.Start("link")
                .Attr("rel", "search")
                .Attr("type", "application/opensearchdescription+xml")
                .Attr("href", DekiContext.Current.ApiUri.At("site", "opensearch", "description"))
            .End();
            feed.Start("link")
                .Attr("rel", "first")
                .Attr("href", searchUri.With("offset", Convert.ToString(0)).With("limit", Convert.ToString(limit)))
                .Attr("type", MimeType.ATOM.ToString())
            .End();
            if(!firstPage) {
                feed.Start("link")
                    .Attr("rel", "previous")
                    .Attr("href", searchUri.With("offset", Convert.ToString(offset - limit)).With("limit", Convert.ToString(limit)))
                    .Attr("type", MimeType.ATOM.ToString())
                .End();
            }
            if(!lastPage) {
                feed.Start("link")
                    .Attr("rel", "next")
                    .Attr("href", searchUri.With("offset", Convert.ToString(offset + limit)).With("limit", Convert.ToString(limit)))
                    .Attr("type", MimeType.ATOM.ToString())
                .End();
            }
            if(!lastPage) {
                feed.Start("link")
                    .Attr("rel", "last")
                    .Attr("href", searchUri.With("offset", Convert.ToString(totalResults - limit)).With("limit", Convert.ToString(limit)))
                    .Attr("type", MimeType.ATOM.ToString())
                .End();
            }
            var homepageId = DekiContext.Current.Instance.HomePageId;
            foreach(XDoc document in luceneResults["document"]) {
                var currentNode = feed.AsXmlNode;
                try {
                    bool isPageChild = false;
                    DateTime edited = DbUtils.ToDateTime(document["date.edited"].AsText);
                    XAtomEntry entry = feed.StartEntry(document["title"].AsText, edited, edited);
                    entry.Start("link")
                        .Attr("href", document["uri"].AsUri)
                    .End();
                    entry.Id = document["uri"].AsUri;
                    entry.AddContent(StringUtil.EncodeHtmlEntities(document["preview"].AsText, Encoding.ASCII, false));
                    entry.Elem("relevance:score", document["score"].AsText);
                    entry.Elem("dekilucene:size", document["size"].AsText);
                    entry.Elem("dekilucene:wordcount", document["wordcount"].AsText);
                    entry.Elem("dekilucene:path", document["path"].AsText);
                    if(!document["id.file"].IsEmpty) {
                        entry.Elem("dekilucene:id.file", document["id.file"].AsText);
                        isPageChild = true;
                    } else if(!document["id.comment"].IsEmpty) {
                        entry.Elem("dekilucene:id.comment", document["id.comment"].AsText);
                        isPageChild = true;
                    }
                    var pageId = document["id.page"].AsUInt ?? 0;
                    if(!isPageChild) {
                        entry.Elem("dekilucene:id.page", pageId);
                    }

                    if(pageId != homepageId) {
                        uint parentPageId;
                        string parentPath;
                        string parentTitle;
                        if(isPageChild) {
                            parentPageId = pageId;
                            parentPath = document["path"].AsText;
                            parentTitle = document["title.page"].AsText;
                        } else {
                            parentPageId = document["id.parent"].AsUInt ?? 0;
                            parentPath = document["path.parent"].AsText;
                            parentTitle = document["title.parent"].AsText;
                        }
                        if(parentPath != null && parentTitle != null) {
                            var title = Title.FromPrefixedDbPath(parentPath, parentTitle);
                            entry.Start("dekilucene:page.parent")
                                .Attr("id", parentPageId)
                                .Attr("path", title.AsPrefixedDbPath())
                                .Attr("title", title.AsUserFriendlyName())
                                .Attr("href", DekiContext.Current.ApiUri.At("pages", parentPageId.ToString()))
                                .End();
                        }
                    }
                } catch(Exception e) {
                    _log.Warn("found invalid data in search result. Likely a sign of a corrupted index. Skipping record", e);
                } finally {
                    feed.End(currentNode);
                }
            }
            return feed;
        }

        private XDoc ConvertSearchResultsToOpenSearchRss(XDoc luceneResults, string query, uint limit, uint offset) {
            string luceneNamespace = "dekilucene";
            int totalResults = 100000;
            bool firstPage = offset < limit;
            bool lastPage = luceneResults["document"].ToList().Count == 0 ? true : false;
            XUri searchUri = DekiContext.Current.ApiUri.At("site", "opensearch").With("q", query).With("format", "rss");
            XUri self = searchUri.With("offset", Convert.ToString(offset)).With("limit", Convert.ToString(limit));
            XDoc feed = new XDoc("rss");
            feed.UsePrefix("opensearch", "http://a9.com/-/spec/opensearch/1.1/");
            feed.UsePrefix("atom", "http://www.w3.org/2005/Atom");
            feed.UsePrefix(luceneNamespace, "http://services.mindtouch.com/deki/draft/2007/06/luceneindex");
            feed.UsePrefix("relevance", "http://a9.com/-/opensearch/extensions/relevance/1.0/");
            feed.Attr("version", "2.0");
            feed.Elem("dekilucene:parsedQuery", luceneResults["parsedQuery"].AsText);
            feed.Start("channel")
                .Elem("title", "MindTouch Search")
                .Elem("link", self)
                .Elem("description", "MindTouch Search");

            // HACKHACKHACK show a fake <totalResults> until we run out
            if(!lastPage) {
                feed.Elem("opensearch:totalResults", totalResults);
            }

            if(offset >= limit)
                feed.Elem("opensearch:startIndex", offset);

            feed.Elem("opensearch:itemsPerPage", limit);
            feed.Start("opensearch:Query")
                .Attr("role", "request")
                .Attr("searchTerms", XUri.Encode(query))
                .Attr("startPage", "1")
            .End();
            feed.Start("atom:link")
                .Attr("rel", "alternate")
                .Attr("type", MimeType.HTML.ToString())
                .Attr("href", DekiContext.Current.UiUri.At("Special:Search").With("search", query).With("limit", limit).With("offset", offset))
            .End();
            feed.Start("atom:link")
                .Attr("rel", "search")
                .Attr("type", "application/opensearchdescription+xml")
                .Attr("href", DekiContext.Current.ApiUri.At("site", "opensearch", "description"))
            .End();
            feed.Start("atom:link")
                .Attr("rel", "first")
                .Attr("href", searchUri.With("offset", Convert.ToString(0)).With("limit", Convert.ToString(limit)))
                .Attr("type", MimeType.ATOM.ToString())
            .End();
            if(!firstPage) {
                feed.Start("atom:link")
                    .Attr("rel", "previous")
                    .Attr("href", searchUri.With("offset", Convert.ToString(offset - limit)).With("limit", Convert.ToString(limit)))
                    .Attr("type", MimeType.ATOM.ToString())
                .End();
            }
            if(!lastPage) {
                feed.Start("atom:link")
                    .Attr("rel", "next")
                    .Attr("href", searchUri.With("offset", Convert.ToString(offset + limit)).With("limit", Convert.ToString(limit)))
                    .Attr("type", MimeType.ATOM.ToString())
                .End();
            }
            if(!lastPage) {
                feed.Start("atom:link")
                    .Attr("rel", "last")
                    .Attr("href", searchUri.With("offset", Convert.ToString(totalResults - limit)).With("limit", Convert.ToString(limit)))
                    .Attr("type", MimeType.ATOM.ToString())
                .End();
            }

            var homepageId = DekiContext.Current.Instance.HomePageId;
            foreach(XDoc document in luceneResults["document"]) {
                var currentNode = feed.AsXmlNode;
                try {
                    bool isPageChild = false;
                    feed.Start("item")
                        .Elem("title", document["title"].AsText)
                        .Elem("link", document["uri"].AsUri)
                        .Elem("description", StringUtil.EncodeHtmlEntities(document["preview"].AsText, Encoding.ASCII, false))
                        .Elem("relevance:score", document["score"].AsText)
                        .Elem("dekilucene:size", document["size"].AsText)
                        .Elem("dekilucene:wordcount", document["wordcount"].AsText)
                        .Elem("dekilucene:path", document["path"].AsText);
                    if(!document["id.file"].IsEmpty) {
                        feed.Elem("dekilucene:id.file", document["id.file"].AsText);
                        isPageChild = true;
                    } else if(!document["id.comment"].IsEmpty) {
                        feed.Elem("dekilucene:id.comment", document["id.comment"].AsText);
                        isPageChild = true;
                    }
                    var pageId = document["id.page"].AsUInt ?? 0;
                    if(!isPageChild) {
                        feed.Elem("dekilucene:id.page", pageId);
                    }

                    if(pageId != homepageId) {
                        uint parentPageId;
                        string parentPath;
                        string parentTitle;
                        if(isPageChild) {
                            parentPageId = pageId;
                            parentPath = document["path"].AsText;
                            parentTitle = document["title.page"].AsText;
                        } else {
                            parentPageId = document["id.parent"].AsUInt ?? 0;
                            parentPath = document["path.parent"].AsText;
                            parentTitle = document["title.parent"].AsText;
                        }
                        if(parentPath != null && parentTitle != null) {
                            var title = Title.FromPrefixedDbPath(parentPath, parentTitle);
                            feed.Start("dekilucene:page.parent")
                                .Attr("id", parentPageId)
                                .Attr("path", title.AsPrefixedDbPath())
                                .Attr("title", title.AsUserFriendlyName())
                                .Attr("href", DekiContext.Current.ApiUri.At("pages", parentPageId.ToString()))
                                .End();
                        }
                    }
                } catch(Exception e) {
                    _log.Warn("found invalid data in search result. Likely a sign of a corrupted index. Skipping record", e);
                } finally {
                    feed.End(currentNode);
                }
            }
            feed.End();
            return feed;
        }
    }
}

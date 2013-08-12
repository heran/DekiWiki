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
using System.Text;
using log4net;
using MindTouch.Cache;
using MindTouch.Deki.Data;
using MindTouch.Deki.Exceptions;
using MindTouch.Deki.Search;
using MindTouch.Dream;
using MindTouch.Tasking;
using MindTouch.Xml;
using MindTouch.Deki.Util;
using System.Linq;

namespace MindTouch.Deki.Logic {
    using Yield = IEnumerator<IYield>;

    public class SearchBL : ISearchBL {

        //--- Types ---
        private class PickAggregate {

            //--- Fields ---
            private int _sum;
            private int _count;
            private int _min = int.MaxValue;

            //--- Properties ---
            public int Max { get; private set; }
            public double Average { get { return _count == 0 ? 0 : (double)_sum / _count; } }
            public int Min { get { return _count == 0 ? 0 : _min; } }

            //--- Methods ---
            public PickAggregate Aggregate(int value) {
                if(value == 0) {
                    return this;
                }
                Max = Math.Max(Max, value);
                _min = Math.Min(_min, value);
                _sum += value;
                _count++;
                return this;
            }
        }

        //--- Constants ---
        private const int MAX_LUCENE_QUERY_ITEMS = 40000;
        private const int RATING_PROMOTE_BOOST = 5;
        private const int RATING_DEMOTE_BOOST = 0;
        private const int RATING_COUNT_THRESHOLD = 1000;
        private const double RATING_RANK_MIDPOINT = 0.2;
        private const int SEARCH_POPULARITY_BOOST = 5;
        private const int SEARCH_POPULARITY_THRESHOLD = 100;
        private const string TERM_QUERY = "content:{0} title:{0}^10 path.title:{0}^4 description:{0}^3 tag:{0}^2 comments:{0}";
        public const string SORT_FIELDS_DESCRIPTION = "{score, title, date, size, wordcount, rating.score, rating.count}?";
        public const string SORT_FIELDS_DESCRIPTION_DEKISCRIPT = "sort field (one of \"score\", \"title\", \"date\", \"size\", \"wordcount\", \"rating.score\", \"rating.count\"; use \"-title\" for reverse order; default: \"score\")";

        //--- Class Fields ---
        private static readonly Random _random = new Random();

        //--- Fields ---
        private readonly IDekiDataSession _session;
        private readonly UserBE _user;
        private readonly IInstanceSettings _settings;
        private readonly SearchQueryParser _parser;
        private readonly Func<bool> _adaptiveSearchEnabled;
        private readonly ILog _log;
        private readonly IKeyValueCache _cache;
        private readonly string _wikiid;
        private readonly XUri _apiUri;
        private readonly Plug _searchPlug;

        //--- Constructors ---
        public SearchBL(IDekiDataSession session, IKeyValueCache cache, string wikiid, XUri apiUri, Plug searchPlug, UserBE user, IInstanceSettings settings, SearchQueryParser parser, Func<bool> adaptiveSearchEnabled, ILog log) {
            _log = log;
            _session = session;
            _cache = cache;
            _wikiid = wikiid;
            _apiUri = apiUri;
            _searchPlug = searchPlug;
            _user = user;
            _settings = settings;
            _parser = parser;
            _adaptiveSearchEnabled = adaptiveSearchEnabled;
        }

        //--- Properties ---
        private bool IsAdaptiveSearchEnabled {
            get { return _adaptiveSearchEnabled(); }
        }

        //--- Methods ---
        public SearchQuery BuildQuery(string queryString, string constraint, SearchQueryParserType parserType, bool haveApiKey) {
            var nullConstraint = string.IsNullOrEmpty(constraint);
            var constraintBuilder = new LuceneClauseBuilder();

            // must have at a queryString or a constraint to proceed
            if(string.IsNullOrEmpty(queryString) && nullConstraint) {
                return SearchQuery.CreateEmpty();
            }

            // If an apikey isn't provided filter out user results by requiring results to be one of the other types
            if(parserType != SearchQueryParserType.Filename && !haveApiKey) {
                constraintBuilder.And("type:(wiki document image comment binary)");
                constraintBuilder.And(constraint);
            } else {
                constraintBuilder.And(constraint);
            }
            SearchQuery query = null;
            switch(parserType) {
            case SearchQueryParserType.BestGuess:
            case SearchQueryParserType.Term:
                var terms = _parser.GetQueryTerms(queryString, parserType != SearchQueryParserType.Term);
                if(terms == null) {
                    query = BuildLuceneQuery(constraintBuilder, queryString);
                    break;
                }
                var unprefixed = (from t in terms where !t.HasFieldPrefix select t.Escaped).ToArray();
                var prefixed = (from t in terms where t.HasFieldPrefix select t.Escaped).ToArray();
                var queryBuilder = new StringBuilder();
                if(unprefixed.Any()) {
                    var termString = "(" + string.Join(" ", unprefixed) + ")";
                    var termQuery = _settings.GetValue("search/termquery", TERM_QUERY);
                    try {
                        queryBuilder.AppendFormat(termQuery, termString);
                    } catch(FormatException e) {
                        throw new FormatException("query format provided in 'search/termquery' is invalid", e);
                    }
                    queryBuilder.Append(" ");
                }
                if(prefixed.Any()) {
                    foreach(var p in prefixed) {
                        queryBuilder.Append(p);
                        queryBuilder.Append(" ");
                    }
                }
                if(!haveApiKey) {
                    constraintBuilder.And("-namespace:\"template_talk\" -namespace:\"help\" -namespace:\"help_talk\"");
                }
                query = new SearchQuery(queryString, queryBuilder.ToString(), constraintBuilder, terms);
                break;
            case SearchQueryParserType.Filename:
                if(string.IsNullOrEmpty(queryString)) {
                    query = SearchQuery.CreateEmpty();
                    break;
                }
                var term = _parser.CreateEscapedTerm(queryString);
                var fileQuery = string.Format("filename:{0}* extension:.{0} description:{0}", term);
                constraintBuilder = new LuceneClauseBuilder();
                constraintBuilder.And("type:(document image binary)");
                constraintBuilder.And(constraint);
                query = new SearchQuery(queryString, fileQuery, constraintBuilder, null);
                break;
            case SearchQueryParserType.Lucene:
                query = BuildLuceneQuery(constraintBuilder, queryString);
                break;
            }
            return query;
        }

        public SearchResult GetCachedQuery(SearchQuery query) {
            SearchResult result;
            if(_cache.TryGet(GetQueryKey(query.LuceneQuery), out result)) {
                _log.DebugFormat("got query from cache: {0}", query.LuceneQuery);
            } else {
                _log.DebugFormat("query was not in cache: {0}", query.LuceneQuery);
            }
            return result;
        }

        public void TrackQueryResultPick(ulong queryId, double rank, ushort position, uint pageId, SearchResultType type, uint? typeId) {
            if(type == SearchResultType.Page) {
                typeId = pageId;
            }
            _session.SearchAnalytics_LogQueryPick(queryId, rank, position, pageId, type, typeId ?? 0);
            _session.SearchAnalytics_UpdateQueryPopularityAggregate(queryId);
        }

        public Result<XDoc> FormatResultSet(SearchResult searchResultSet, SetDiscriminator discriminator, bool explain, TrackingInfo trackingInfo, Result<XDoc> result) {
            return Coroutine.Invoke(FormatResultSet_Helper, searchResultSet, discriminator, explain, trackingInfo, result);
        }

        public SearchResult CacheQuery(XDoc searchDoc, SearchQuery query, TrackingInfo trackingInfo) {
            var key = GetQueryKey(query.LuceneQuery);
            var documents = searchDoc["document"];
            if(trackingInfo != null) {
                if(!trackingInfo.QueryId.HasValue) {

                    // new tracked query
                    _log.Debug("creating new tracked query");
                    trackingInfo.QueryId = _session.SearchAnalytics_LogQuery(query, searchDoc["parsedQuery"].AsText, _user.ID, (uint)documents.ListLength, trackingInfo.PreviousQueryId);
                } else {
                    _log.Debug("recreating tracked query that fell out of cache");
                }
            } else {
                _log.Debug("untracked query");
            }
            var results = new SearchResultRankCalculator(
                _settings.GetValue("search/rating-promote-boost", RATING_PROMOTE_BOOST),
                _settings.GetValue("search/rating-demote-boost", RATING_DEMOTE_BOOST),
                _settings.GetValue("search/rating-count-threshold", RATING_COUNT_THRESHOLD),
                _settings.GetValue("search/rating-rank-midpoint", RATING_RANK_MIDPOINT),
                _settings.GetValue("search/search-popularity-boost", SEARCH_POPULARITY_BOOST),
                _settings.GetValue("search/search-popularity-threshold", SEARCH_POPULARITY_THRESHOLD)
            );
            _log.Debug("building rank calculator set");
            foreach(var entry in documents) {
                try {
                    SearchResultType type;
                    var typeId = entry["id.file"].AsUInt;
                    if(typeId.HasValue) {
                        type = SearchResultType.File;
                    } else {
                        typeId = entry["id.comment"].AsUInt;
                        if(typeId.HasValue) {
                            type = SearchResultType.Comment;
                        } else {
                            typeId = entry["id.user"].AsUInt;
                            if(typeId.HasValue) {
                                type = SearchResultType.User;
                            } else {
                                typeId = entry["id.page"].AsUInt;
                                if(typeId.HasValue) {
                                    type = SearchResultType.Page;
                                } else {
                                    _log.WarnFormat("dropping unsupported result item {0}", entry["type"]);
                                    continue;
                                }
                            }
                        }
                    }
                    results.Add(
                        typeId.Value,
                        type,
                        entry["title"].AsText,
                        entry["score"].AsDouble ?? 0,
                        DbUtils.ToDateTime(entry["date.edited"].AsText),
                        entry["rating.score"].AsDouble,
                        entry["rating.count"].AsInt ?? 0
                    );
                } catch(Exception e) {

                    // skip any item we cannot process, but log debug to see if there is a bad value pattern that's not related to stale index data
                    _log.DebugFormat("unable to parse lucene result value because of '{0} ({1})' from: {2}", e.GetType(), e.Message, entry.ToString());
                }
            }
            if(IsAdaptiveSearchEnabled) {
                var popularity = _session.SearchAnalytics_GetPopularityRanking(query.GetOrderedTermsHash());
                results.ComputeRank(popularity);
            } else {
                _log.Debug("ranking disabled in non-commercial version");
            }
            var searchResults = new SearchResult(searchDoc["parsedQuery"].AsText, results.ToArray());
            _log.DebugFormat("putting results into cache with key: {0}", key);
            _cache.Set(key, searchResults, TimeSpan.FromSeconds(_settings.GetValue("search/set-cache-time", 120d)));
            return searchResults;
        }

        public XDoc GetQueriesXml(string queryString, SearchAnalyticsQueryType type, bool lowQuality, DateTime since, DateTime before, uint limit, uint offset) {
            ThrowOnInvalidLicense();
            var queryDoc = new XDoc("queries");
            var queries = _session.SearchAnalytics_GetTrackedQueries(BuildAnalyticsQuery(queryString, type), type, since, before, limit, offset);
            if(lowQuality) {
                queries = FilterForLowQuality(queries);
            }
            var count = 0;
            foreach(var query in queries) {
                queryDoc.Start("query");
                BuildQueryXml(queryDoc, query);
                queryDoc.End();
                count++;
            }
            queryDoc.Attr("count", count);
            return queryDoc;
        }

        public XDoc GetQueryXml(ulong queryId) {
            ThrowOnInvalidLicense();
            var queryDoc = new XDoc("query");
            BuildDetailedQueryXml(queryDoc, queryId);
            return queryDoc;
        }

        public XDoc GetAggregateQueryXml(string queryString, DateTime since, DateTime before) {
            ThrowOnInvalidLicense();
            var query = BuildQuery(queryString, null, SearchQueryParserType.BestGuess, true);
            var sortedTerms = query.GetOrderedNormalizedTermString();
            var queries = _session.SearchAnalytics_GetTrackedQueries(sortedTerms ?? "", SearchAnalyticsQueryType.QueryString, since, before, null, null);
            if(!queries.Any()) {
                return XDoc.Empty;
            }
            var last = queries.Max(x => x.Created);
            var queryDoc = new XDoc("query")
                .Attr("href", BuildLogHref(sortedTerms, since, before))
                .Elem("sorted-terms", sortedTerms)
                .Elem("date.searched", last)
                .Start("queries")
                    .Attr("totalcount", queries.Count());
            var agg = (from q in queries
                       group q by q.RawQuery into raw
                       let count = raw.Count()
                       orderby count descending
                       select new { Raw = raw.Key, Count = count });
            foreach(var raw in agg) {
                queryDoc.Start("raw")
                    .Attr("count", raw.Count)
                    .Value(raw.Raw)
                .End();
            }
            queryDoc.End();
            var relatedIds = (from q in queries where q.PreviousId.HasValue select q.PreviousId.Value).ToArray();
            var previous = _session.SearchAnalytics_GetPreviousSortedQueryTermsRecursively(relatedIds);
            if(previous.Any()) {
                queryDoc.Start("previous")
                    .Attr("totalcount", previous.Count());
                var previousAgg = (from p in previous
                                   group p by p into related
                                   let count = related.Count()
                                   orderby count descending
                                   select new { SortedTerms = related.Key, Count = count });
                foreach(var related in previousAgg) {
                    queryDoc.Start("query")
                        .Attr("count", related.Count)
                        .Attr("href", BuildLogHref(related.SortedTerms, since, before))
                        .Elem("sorted-terms", related.SortedTerms)
                    .End();
                }
                queryDoc.End();
            }
            GetSelectedResultXml(queryDoc, queries, null);
            return queryDoc;
        }

        public XDoc GetAggregatedQueriesXml(string queryString, SearchAnalyticsQueryType type, bool lowQuality, DateTime since, DateTime before, uint limit, uint offset) {
            ThrowOnInvalidLicense();
            var queries = _session.SearchAnalytics_GetTrackedQueries(BuildAnalyticsQuery(queryString, type), type, since, before, null, null);
            if(lowQuality) {
                queries = FilterForLowQuality(queries);
            }

            // aggregate the queries by their sorted terms string and apply the limit and offset on the aggregated set
            var aggregate = (from entity in queries
                             group entity by entity.SortedTerms into sortedTerms
                             orderby sortedTerms.Count() descending
                             select new {
                                 SortedTerms = sortedTerms.Key,
                                 Queries = sortedTerms.ToArray()
                             })
                .Skip(offset.ToInt())
                .Take(limit.ToInt());

            // aggregate and collapse results and build Xml document
            var doc = new XDoc("queries");
            var count = 0;
            foreach(var agg in aggregate) {

                // find most recent query
                var last = agg.Queries.Max(x => x.Created);
                doc.Start("query")
                    .Attr("count", agg.Queries.Length)
                    .Attr("href", BuildLogHref(agg.SortedTerms, since, before))
                    .Elem("sorted-terms", agg.SortedTerms)
                    .Elem("date.searched", last);
                GetSelectedResultXml(doc, agg.Queries, 5);
                doc.End();
                count++;
            }
            doc.Attr("count", count);
            return doc;
        }

        public XDoc GetTermsXml(bool lowQuality, DateTime since, DateTime before, uint limit, uint offset) {
            ThrowOnInvalidLicense();
            var terms = _session.SearchAnalytics_GetTerms(lowQuality, since, before, limit, offset);
            var doc = new XDoc("terms").Attr("count", terms.Count());
            foreach(var term in terms.OrderByDescending(x => x.Count)) {
                doc.Start("term")
                    .Attr("count", term.Count)
                    .Value(term.Term)
                    .End();
            }
            return doc;
        }

        public void ClearCache() {
            _cache.Clear();
        }

        public Result<IEnumerable<SearchResultItem>> GetItems(IEnumerable<ulong> pageIds, SearchResultType type, Result<IEnumerable<SearchResultItem>> result) {
            return Coroutine.Invoke(GetItems_Helper, pageIds, type, result);
        }

        private Yield GetItems_Helper(IEnumerable<ulong> ids, SearchResultType type, Result<IEnumerable<SearchResultItem>> result) {
            string query;
            Func<XDoc, uint> getTypeId;
            switch(type) {
            case SearchResultType.User:
                getTypeId = entry => entry["id.user"].AsUInt.Value;
                query = string.Format("id.user:({0}) ", string.Join(" ", ids.Select(x => x.ToString()).ToArray()));
                break;
            case SearchResultType.File:
                getTypeId = entry => entry["id.file"].AsUInt.Value;
                query = string.Format("id.file:({0}) ", string.Join(" ", ids.Select(x => x.ToString()).ToArray()));
                break;
            case SearchResultType.Comment:
                getTypeId = entry => entry["id.comment"].AsUInt.Value;
                query = string.Format("id.comment:({0}) ", string.Join(" ", ids.Select(x => x.ToString()).ToArray()));
                break;
            case SearchResultType.Page:
                getTypeId = entry => entry["id.page"].AsUInt.Value;
                query = string.Format("(id.page:({0}) AND type:wiki)", string.Join(" ", ids.Select(x => x.ToString()).ToArray()));
                break;
            default:
                result.Return(new SearchResultItem[0]);
                yield break;
            }
            DreamMessage luceneResult = null;
            yield return _searchPlug.At("compact").With("wikiid", _wikiid).With("q", query).Get(new Result<DreamMessage>())
                .Set(x => luceneResult = x);
            if(!luceneResult.IsSuccessful) {
                throw new Exception("unable to query lucene for details");
            }
            var documents = luceneResult.ToDocument()["document"];
            var results = new List<SearchResultItem>();
            foreach(var entry in documents) {
                try {
                    results.Add(new SearchResultItem(
                      getTypeId(entry),
                      type,
                      entry["title"].AsText,
                      entry["score"].AsDouble ?? 0,
                      DbUtils.ToDateTime(entry["date.edited"].AsText)
                  ));
                } catch(Exception e) {

                    // skip any item we cannot process, but log debug to see if there is a bad value pattern that's not related to stale index data
                    _log.DebugFormat("unable to parse lucene result value because of '{0} ({1})' from: {2}", e.GetType(), e.Message, entry);
                }
            }
            result.Return(results.Distinct(x => x.TypeId));
        }

        private void GetSelectedResultXml(XDoc doc, IEnumerable<LoggedSearchBE> queries, int? take) {

            // collapse and collect last selected result per query
            var lastPicks = new List<LoggedSearchResultBE>();
            foreach(var entity in queries) {
                if(!entity.SelectedResults.Any()) {
                    continue;
                }
                lastPicks.Add(entity.SelectedResults.OrderByDescending(x => x.Created).First());
            }

            // aggregate last selected results
            var pickQuery = (from pick in lastPicks
                             orderby pick.Created descending
                             let key = pick.PageId + ":" + pick.Type + ":" + pick.TypeId
                             group pick by key into aggPick
                             let Result = aggPick.First()
                             let PositionStats = aggPick.Aggregate(new PickAggregate(), (agg, pick) => agg.Aggregate(pick.Position))
                             orderby aggPick.Count() descending
                             select new {
                                 Count = aggPick.Count(),
                                 Result,
                                 PositionStats
                             });
            if(take.HasValue) {
                pickQuery = pickQuery.Take(take.Value);
            }
            var picks = pickQuery.ToArray();
            if(!picks.Any()) {
                return;
            }
            doc.Start("selected-results");
            var totalCount = 0;
            foreach(var pick in picks) {
                totalCount += pick.Count;
                doc.Start("result")
                    .Attr("count", pick.Count)
                    .Start("position")
                        .Attr("avg", pick.PositionStats.Average)
                        .Attr("min", pick.PositionStats.Min)
                        .Attr("max", pick.PositionStats.Max)
                    .End();
                BuildResultXmlFragment(doc, pick.Result);
                doc.End();
            }
            doc.Attr("totalcount", totalCount);
            doc.End();
        }

        private IEnumerable<LoggedSearchBE> FilterForLowQuality(IEnumerable<LoggedSearchBE> queries) {
            foreach(var query in queries) {
                if(query.SelectedResults.Count > 0 && query.SelectedResults.Count < 3) {
                    continue;
                }
                yield return query;
            }
        }

        private SearchQuery BuildLuceneQuery(LuceneClauseBuilder constraint, string queryString) {
            SearchQuery query;
            query = new SearchQuery(queryString, queryString, constraint, null);
            return query;
        }

        private string BuildAnalyticsQuery(string queryString, SearchAnalyticsQueryType type) {
            switch(type) {
            case SearchAnalyticsQueryType.QueryString:
                var query = BuildQuery(queryString, null, SearchQueryParserType.BestGuess, true);
                return query.GetOrderedNormalizedTermString() ?? "";
            case SearchAnalyticsQueryType.Term:
                return queryString;
            }
            return null;
        }

        private void ThrowOnInvalidLicense() {
            if(!IsAdaptiveSearchEnabled) {
                throw new MindTouchLicenseInvalidOperationForbiddenException("Adaptive Search Log API");
            }
        }

        private XUri BuildLogHref(string sortedTerms, DateTime since, DateTime before) {
            var href = _apiUri.At("site", "query", "log", "=" + XUri.EncodeSegment(sortedTerms))
                .With("since", DbUtils.ToString(since))
                .With("before", DbUtils.ToString(before));
            return href;
        }

        private void BuildDetailedQueryXml(XDoc queryDoc, ulong queryId) {
            var query = _session.SearchAnalytics_GetTrackedQuery(queryId);
            BuildQueryXml(queryDoc, query);
            if(query.PreviousId.HasValue) {

                // Note (arnec): this method builds the xml using recursive DA calls, which is generally dangerous,
                // but the chain for queries should always be non-existent or very short
                BuildDetailedQueryXml(queryDoc["previous"], query.PreviousId.Value);
            }
        }

        private void BuildQueryXml(XDoc queryDoc, LoggedSearchBE query) {
            queryDoc
                .Attr("id", query.Id)
                .Attr("href", _apiUri.At("site", "query", "log", query.Id.ToString()).AsPublicUri());
            if(query.PreviousId.HasValue) {
                queryDoc.Start("previous")
                    .Attr("id", query.PreviousId.Value)
                    .Attr("href", _apiUri.At("site", "query", "log", query.Id.ToString()).AsPublicUri())
                    .End();
            }
            queryDoc
                .Elem("date.searched", query.Created)
                .Elem("sorted-terms", query.SortedTerms)
                .Elem("raw", query.RawQuery)
                .Elem("processed", query.ParsedQuery)
                .Elem("count.results", query.ResultCount);

            // terms block
            queryDoc.Start("terms");
            foreach(var term in query.Terms) {
                queryDoc.Elem("term", term);
            }
            queryDoc.End();

            // selected result block
            queryDoc.Start("selected-results").Attr("count", query.SelectedResults.Count);
            foreach(var pick in query.SelectedResults) {
                queryDoc.Start("result")
                    .Elem("position", pick.Position)
                    .Elem("rank", pick.Rank);
                BuildResultXmlFragment(queryDoc, pick);
                queryDoc.End();
            }
            queryDoc.End();
        }

        private void BuildResultXmlFragment(XDoc doc, LoggedSearchResultBE pick) {
            doc.Attr("type", pick.Type.ToString().ToLower())
                .Elem("date.selected", pick.Created)
                .Start("page")
                .Attr("id", pick.PageId)
                .Attr("href", _apiUri.At("pages", pick.PageId.ToString()).AsPublicUri())
            .End();
            switch(pick.Type) {
            case SearchResultType.File:
                doc.Start("file")
                    .Attr("id", pick.TypeId)
                    .Attr("href", _apiUri.At("files", pick.TypeId.ToString(), "info").AsPublicUri())
                .End();
                break;
            case SearchResultType.Comment:
                doc.Start("comment")
                    .Attr("id", pick.TypeId)
                    .Attr("href", _apiUri.At("comments", pick.TypeId.ToString()).AsPublicUri())
                .End();
                break;
            }
        }

        private Yield FormatResultSet_Helper(SearchResult searchResultSet, SetDiscriminator discriminator, bool explain, TrackingInfo trackingInfo, Result<XDoc> result) {
            _log.Debug("formatting result set");
            var searchDoc = new XDoc("search")
                .Attr("querycount", searchResultSet.Count)
                .Attr("ranking", IsAdaptiveSearchEnabled ? "adaptive" : "simple")
                .Elem("parsedQuery", searchResultSet.ExecutedQuery);
            ulong queryId = 0;
            if(trackingInfo != null) {
                queryId = trackingInfo.QueryId.Value;
                searchDoc.Attr("queryid", queryId);

                // TODO (arnec): Keep or remove this? It does expose admin visibility data to non-admins
                if(explain) {
                    searchDoc.Start("settings").Start("search")
                        .Elem("rating-promote-boost", _settings.GetValue("search/rating-promote-boost", RATING_PROMOTE_BOOST))
                        .Elem("rating-demote-boost", _settings.GetValue("search/rating-demote-boost", RATING_DEMOTE_BOOST))
                        .Elem("rating-count-threshold", _settings.GetValue("search/rating-count-threshold", RATING_COUNT_THRESHOLD))
                        .Elem("rating-rank-midpoint", _settings.GetValue("search/rating-rank-midpoint", RATING_RANK_MIDPOINT))
                        .Elem("search-popularity-boost", _settings.GetValue("search/search-popularity-boost", SEARCH_POPULARITY_BOOST))
                        .Elem("search-popularity-threshold", _settings.GetValue("search/search-popularity-threshold", SEARCH_POPULARITY_THRESHOLD))
                    .End().End();
                }
            }
            var query = searchResultSet as IEnumerable<SearchResultItem>;
            switch(discriminator.SortField) {
            case "title":
                query = OrderBy(query, item => item.Title, discriminator.Ascending);
                break;
            case "modified":
                query = OrderBy(query, item => item.Modified, discriminator.Ascending);
                break;
            default:
                query = OrderBy(query, item => item.Rank, discriminator.Ascending);
                break;
            }
            if(discriminator.Offset > 0) {
                query = query.Skip(discriminator.Offset.ToInt());
            }
            if(discriminator.Limit > 0 && discriminator.Limit != uint.MaxValue) {
                query = query.Take(discriminator.Limit.ToInt());
            }
            var items = query.ToList();
            yield return Coroutine.Invoke(PopulateDetail, items, new Result());

            // position starts at 1 not 0, since 0 is the value used when position isn't tracked
            var position = discriminator.Offset + 1;
            var count = 0;
            foreach(var item in query) {
                if(item.Detail == null) {
                    continue;
                }
                try {
                    if(trackingInfo != null) {
                        var detail = item.Detail;

                        // Note (arnec): this assumes that any item in a tracked result has an id.page
                        var trackUri = _apiUri.At("site", "query", queryId.ToString())
                            .With("pageid", item.Detail["id.page"] ?? "0")
                            .With("rank", item.Rank.ToString())
                            .With("type", item.Type.ToString().ToLower());
                        if(discriminator.SortField.EqualsInvariantIgnoreCase("rank")) {
                            trackUri = trackUri.With("position", position.ToString());
                        }
                        var uri = new XUri(detail["uri"]);
                        var path = detail["path"];
                        var title = detail["title"];
                        var pageTitle = title;
                        try {
                            switch(item.Type) {
                            case SearchResultType.User:
                                continue;
                            case SearchResultType.File:
                                trackUri = trackUri.With("typeid", item.TypeId.ToString());
                                uri = _apiUri.At("files", item.TypeId.ToString(), Title.AsApiParam(title));
                                pageTitle = detail["title.page"];
                                break;
                            case SearchResultType.Comment:
                                trackUri = trackUri.With("typeid", item.TypeId.ToString());
                                uri = new XUri(Utils.AsPublicUiUri(Title.FromUriPath(path))).WithFragment(uri.Fragment);
                                pageTitle = detail["title.page"];
                                break;
                            default:
                                uri = new XUri(Utils.AsPublicUiUri(Title.FromUriPath(path)));
                                break;
                            }
                        } catch(Exception e) {

                            // Note (arnec): not being able to derive the Ui Uri is not enough reason to skip the item
                            _log.Warn("unable to derive UI uri for item", e);
                        }
                        searchDoc.Start("result")
                            .Elem("id", item.TypeId);
                        if(explain) {
                            var rankable = item as RankableSearchResultItem;
                            if(rankable != null) {
                                searchDoc.Start("explain")
                                    .Elem("rank.normalized", rankable.Rank)
                                    .Elem("rank.raw", rankable.RawRank)
                                    .Elem("lucene-score", rankable.LuceneScore)
                                    .Elem("lucene-position", rankable.Position)
                                    .Elem("normalized-rating", rankable.Rating)
                                    .Elem("rating-count", rankable.RatingCount)
                                    .Elem("rating-boost", rankable.RatingBoost)
                                    .Elem("search-popularity", rankable.SearchPopularity)
                                    .Elem("search-popularity-boost", rankable.SearchPopularityBoost)
                                .End();
                            }
                        }
                        searchDoc.Elem("uri", uri)
                            .Elem("uri.track", trackUri)
                            .Elem("rank", item.Rank)
                            .Elem("title", title)
                            .Start("page")
                                .Start("rating").Attr("score", detail["rating.score"]).Attr("count", detail["rating.count"]).End()
                                .Elem("title", pageTitle)
                                .Elem("path", path)
                            .End()
                            .Elem("author", detail["author"])
                            .Elem("date.modified", item.Modified)
                            .Elem("content", detail["preview"])
                            .Elem("type", item.Type.ToString().ToLower())
                            .Elem("mime", detail["mime"])
                            .Elem("tag", detail["tag"])
                            .Elem("size", detail["size"])
                            .Elem("wordcount", detail["wordcount"])
                        .End();
                        position++;
                    } else {
                        searchDoc.Start("document").Elem("score", item.Rank);
                        foreach(var kvp in item.Detail) {
                            if(kvp.Key.EqualsInvariant("score")) {
                                continue;
                            }
                            searchDoc.Elem(kvp.Key, kvp.Value);

                        }
                        searchDoc.End();
                    }
                } catch(Exception e) {

                    // skip any item we cannot process
                    _log.Warn("unable to process search data for item", e);

                    // Note (arnec): skipping an item throws off total and querycount and messes with offset. It's an outlier in the first place
                    // so probably not worth correcting for.
                }
                count++;
            }
            searchDoc.Attr("count", count);
            result.Return(searchDoc);
            yield break;
        }

        private static IEnumerable<SearchResultItem> OrderBy<TSortField>(IEnumerable<SearchResultItem> query, Func<SearchResultItem, TSortField> sortFunction, bool ascending) {
            var ordered = ascending ? query.OrderBy(sortFunction) : query.OrderByDescending(sortFunction);
            return ordered.ThenBy(x => x.TypeId);
        }

        private Yield PopulateDetail(List<SearchResultItem> items, Result result) {
            var pages = new List<SearchResultItem>();
            var files = new List<SearchResultItem>();
            var comments = new List<SearchResultItem>();
            var users = new List<SearchResultItem>();
            var hits = 0;
            foreach(var item in items) {
                SearchResultDetail detail;
                if(_cache.TryGet(item.DetailKey, out detail)) {
                    _log.TraceFormat("got {0} from cache", item.DetailKey);
                }
                if(detail == null) {

                    // cache miss
                    switch(item.Type) {
                    case SearchResultType.User:
                        users.Add(item);
                        break;
                    case SearchResultType.File:
                        files.Add(item);
                        break;
                    case SearchResultType.Comment:
                        comments.Add(item);
                        break;
                    default:
                        pages.Add(item);
                        break;
                    }
                } else {
                    hits++;
                    item.Detail = detail;
                }
            }
            _log.DebugFormat("got {0}/{1} items from cache", hits, items.Count);
            if(pages.Any() || files.Any() || comments.Any()) {

                // query lucene for meta data
                var itemLookup = new Dictionary<string, SearchResultItem>();
                var queries = new List<String>();
                if(pages.Any()) {
                    BuildLookupQueries(pages, itemLookup, queries, "(id.page:(", ") AND type:wiki) ");
                }
                if(files.Any()) {
                    BuildLookupQueries(files, itemLookup, queries, "(id.file:(", ")) ");
                }
                if(comments.Any()) {
                    BuildLookupQueries(comments, itemLookup, queries, "(id.comment:(", ")) ");
                }
                if(users.Any()) {
                    BuildLookupQueries(users, itemLookup, queries, "(id.user:(", ")) ");
                }
                var count = 0;
                foreach(var query in queries) {
                    count++;
                    _log.DebugFormat("querying lucene for details ({0}/{1})", count, queries.Count);
                    DreamMessage luceneResult = null;
                    yield return _searchPlug.With("wikiid", _wikiid).With("q", query).With("max", "all").GetAsync().Set(x => luceneResult = x);
                    if(!luceneResult.IsSuccessful) {

                        // TODO (arnec): need error handling story here
                        throw new Exception("unable to query lucene for details");
                    }
                    var details = luceneResult.ToDocument()["document"];
                    _log.DebugFormat("retrieved {0} detail records", details.ListLength);
                    foreach(var detailDoc in details) {
                        var detail = SearchResultDetail.FromXDoc(detailDoc);
                        SearchResultItem item;
                        if(detail["id.file"] != null) {
                            itemLookup.TryGetValue(SearchResultType.File + ":" + detail["id.file"], out item);
                        } else if(detail["id.comment"] != null) {
                            itemLookup.TryGetValue(SearchResultType.Comment + ":" + detail["id.comment"], out item);
                        } else if(detail["id.user"] != null) {
                            itemLookup.TryGetValue(SearchResultType.User + ":" + detail["id.user"], out item);
                        } else {
                            itemLookup.TryGetValue(SearchResultType.Page + ":" + detail["id.page"], out item);
                        }
                        if(item != null) {
                            _log.TraceFormat("got {0} from lucene", item.DetailKey);
                            _cache.Set(item.DetailKey, detail, TimeSpan.FromSeconds(_settings.GetValue("search/date-cache-time", 60d)));
                            item.Detail = detail;
                        }
                    }
                }
                _log.Debug("finished populating and caching detail records");
            }
            result.Return();
        }

        private void BuildLookupQueries(List<SearchResultItem> items, Dictionary<string, SearchResultItem> itemLookup, List<string> queries, string prefix, string postfix) {
            var count = 0;
            StringBuilder builder = null;
            foreach(var item in items) {
                if(builder == null) {
                    builder = new StringBuilder();
                    builder.Append(prefix);
                }
                var key = item.Type + ":" + item.TypeId;
                if(itemLookup.ContainsKey(key)) {
                    continue;
                }
                itemLookup[key] = item;
                builder.Append(item.TypeId);
                builder.Append(" ");
                count++;
                if(count > MAX_LUCENE_QUERY_ITEMS) {
                    builder.Append(postfix);
                    queries.Add(builder.ToString());
                    builder = null;
                    count = 0;
                }
            }
            if(count > 0) {
                builder.Append(postfix);
                queries.Add(builder.ToString());
            }
        }

        private string GetQueryKey(string query) {
            return "query:" + _user.ID + ":" + query;
        }
    }
}

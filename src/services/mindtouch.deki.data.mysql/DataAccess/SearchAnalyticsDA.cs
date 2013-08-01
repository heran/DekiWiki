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
using System.Data;
using System.Linq;
using System.Text;
using MindTouch.Data;
using MindTouch.Deki.Search;

namespace MindTouch.Deki.Data.MySql {

    public partial class MySqlDekiDataSession {

        //--- Methods ---
        public ulong SearchAnalytics_LogQuery(SearchQuery query, string parsedQuery, uint userId, uint resultCount, ulong? previousQueryId) {
            var sorted = query.GetOrderedNormalizedTermString();
            var hash = query.GetOrderedTermsHash();
            var queryId = Catalog.NewQuery(@"/* SearchAnalytics_LogQuery */
INSERT INTO query_log (raw, sorted_terms, sorted_terms_hash, parsed, created, user_id, ref_query_id, result_count)
  VALUES (?QUERY, ?SORTED, ?HASH, ?PARSED, ?CREATED, ?USERID, ?REFID, ?RESULTCOUNT);
SELECT LAST_INSERT_ID();")
                .With("QUERY", query.Raw)
                .With("SORTED", sorted)
                .With("HASH", hash)
                .With("PARSED", parsedQuery)
                .With("USERID", userId)
                .With("CREATED", DateTime.UtcNow)
                .With("REFID", previousQueryId)
                .With("RESULTCOUNT", resultCount)
                .ReadAsULong().Value;
            var terms = query.GetNormalizedTerms();
            if(terms.Any()) {
                var quotedTerms = terms.Select(x => "'" + DataCommand.MakeSqlSafe(x) + "'").ToArray();
                Catalog.NewQuery(string.Format(@"/* SearchAnalytics_LogQuery */
INSERT IGNORE INTO query_terms (query_term) values
{0};", string.Join(",", quotedTerms.Select(x => "(" + x + ")").ToArray()))).Execute();
                var termIds = new List<uint>();
                Catalog.NewQuery(string.Format(@"/* SearchAnalytics_LogQuery */
SELECT query_term_id from query_terms where query_term IN({0})",
                    string.Join(",", quotedTerms))).Execute(r => {
                        while(r.Read()) {
                            termIds.Add(r.Read<uint>(0));
                        }
                    });
                Catalog.NewQuery(string.Format(@"/* SearchAnalytics_LogQuery */
INSERT IGNORE INTO query_term_map (query_term_id,query_id) values
{0};", string.Join(",", termIds.Select(x => "(" + x + "," + queryId + ")").ToArray()))).Execute();
            }
            return queryId;
        }

        public void SearchAnalytics_LogQueryPick(ulong queryId, double rank, ushort position, uint pageId, SearchResultType type, uint typeId) {
            Catalog.NewQuery(@"/* SearchAnalytics_LogQueryPick */
INSERT INTO query_result_log 
    (query_id, created, result_position, result_rank, page_id, type, type_id) 
  VALUES
    (?QUERYID, ?CREATED, ?POSITION, ?RANK, ?PAGEID, ?TYPE, ?TYPEID);
UPDATE query_log SET last_result_id = LAST_INSERT_ID() WHERE query_id = ?QUERYID")
                .With("QUERYID", queryId)
                .With("CREATED", DateTime.UtcNow)
                .With("POSITION", position)
                .With("RANK", rank)
                .With("PAGEID", pageId)
                .With("TYPE", type)
                .With("TYPEID", typeId)
                .Execute();

        }

        public void SearchAnalytics_UpdateQueryPopularityAggregate(ulong queryId) {

            // Note (arnec): The DELETE/INSERT isn't atomic, but since we're doing a REPLACE INTO, it will only result in extra work, not
            // extra records (i.e. it always aggregates everything by the sorted_terms_hash, so another entry of hte same call would only
            // overwrite with more recent data)
            Catalog.NewQuery(@"/* SearchAnalytics_UpdateQueryPopularityAggregate */
SELECT @hash := sorted_terms_hash FROM query_log WHERE query_id = ?QUERYID;
DELETE FROM query_result_popularity WHERE sorted_terms_hash = @hash;
REPLACE into query_result_popularity (sorted_terms_hash,type,type_id,selection_count)
  SELECT sorted_terms_hash, type, type_id,count(*) 
    FROM query_log ql
      INNER JOIN query_result_log qrl ON ql.last_result_id = qrl.query_result_id
    WHERE sorted_terms_hash = @hash
    GROUP BY sorted_terms_hash, page_id,type_id;")
                            .With("QUERYID", queryId)
                            .Execute();
        }

        public IEnumerable<ResultPopularityBE> SearchAnalytics_GetPopularityRanking(string termhash) {
            var results = new List<ResultPopularityBE>();
            if(!string.IsNullOrEmpty(termhash)) {
                Catalog.NewQuery(@"/* SearchAnalytics_GetPopularityRanking */
SELECT * FROM query_result_popularity WHERE sorted_terms_hash = ?HASH")
                    .With("HASH", termhash)
                    .Execute(r => {
                        while(r.Read()) {
                            results.Add(new ResultPopularityBE() {
                                TypeId = r.Read<uint>("type_id"),
                                Type = r.Read<SearchResultType>("type"),
                                Count = r.Read<uint>("selection_count")
                            });
                        }
                    });
            }
            return results;
        }

        public LoggedSearchBE SearchAnalytics_GetTrackedQuery(ulong queryId) {
            LoggedSearchBE query = null;
            Catalog.NewQuery(@"/* SearchAnalytics_GetTrackedQuery */
SELECT * FROM query_log ql WHERE ql.query_id = ?QUERYID;
SELECT qt.query_term
  FROM query_term_map qtm
    INNER JOIN query_terms qt ON qtm.query_term_id = qt.query_term_id
  WHERE qtm.query_id = ?QUERYID;")
                .With("QUERYID", queryId)
                .Execute(r => {
                    while(r.Read()) {
                        query = SearchAnalytics_PopulateTrackQueryBE(r);
                    }
                    r.NextResult();
                    while(r.Read()) {
                        query.Terms.Add(r.Read<string>("query_term"));
                    }
                });
            if(query != null) {
                query.SelectedResults = new List<LoggedSearchResultBE>();
                Catalog.NewQuery(@"/* SearchAnalytics_GetTrackedQuery */
SELECT * FROM query_result_log qrl WHERE qrl.query_id = ?QUERYID;")
                    .With("QUERYID", query.Id)
                    .Execute(r => {
                        while(r.Read()) {
                            query.SelectedResults.Add(SearchAnalytics_PopulateSearchResultBE(r));
                        }
                    });
            }
            return query;
        }

        public IEnumerable<LoggedSearchBE> SearchAnalytics_GetTrackedQueries(string querystring, SearchAnalyticsQueryType type, DateTime since, DateTime before, uint? limit, uint? offset) {
            var whereClause = new StringBuilder();
            SearchAnalytics_BuildDateRangeClause(whereClause);
            string joinClause = null;
            switch(type) {
            case SearchAnalyticsQueryType.Term:
                if(string.IsNullOrEmpty(querystring)) {
                    return new LoggedSearchBE[0];
                }
                joinClause = @"INNER JOIN query_term_map qtm ON ql.query_id = qtm.query_id
INNER JOIN query_terms qt ON qtm.query_term_id = qt.query_term_id";
                whereClause.Append(" AND qt.query_term = ?KEY");
                break;
            case SearchAnalyticsQueryType.QueryString:
                if(querystring == null) {
                    return new LoggedSearchBE[0];
                }
                whereClause.Append(" AND ql.sorted_terms = ?KEY");
                break;
            }
            whereClause.Append(" ORDER BY ql.created DESC");
            SearchAnalytics_BuildLimitOffsetClause(whereClause, limit, offset);
            var queries = new Dictionary<ulong, LoggedSearchBE>();
            Catalog.NewQuery(string.Format(@"/* SearchAnalytics_GetTrackedQueries */
SELECT * FROM query_log ql
{0}
{1}", joinClause, whereClause))
                    .With("SINCE", since)
                    .With("BEFORE", before)
                    .With("LIMIT", limit)
                    .With("OFFSET", offset)
                    .With("KEY", querystring)
                    .Execute(r => {
                        while(r.Read()) {
                            var entity = SearchAnalytics_PopulateTrackQueryBE(r);
                            queries.Add(entity.Id, entity);
                        }
                    });
            if(queries.Any()) {
                var idClause = queries.Keys.ToCommaDelimitedString();

                // populate terms && results
                Catalog.NewQuery(string.Format(@"/* SearchAnalytics_GetTrackedQueries */
SELECT qtm.query_id, qt.query_term
  FROM query_term_map qtm
    INNER JOIN query_terms qt ON qtm.query_term_id = qt.query_term_id
  WHERE qtm.query_id IN ({0});
SELECT *
  FROM query_result_log qrl
  WHERE qrl.query_id IN ({0});", idClause))
                    .Execute(r => {
                        while(r.Read()) {
                            var id = r.Read<ulong>("query_id");
                            LoggedSearchBE entity = queries[id];
                            entity.Terms.Add(r.Read<string>("query_term"));
                        }
                        r.NextResult();
                        while(r.Read()) {
                            var id = r.Read<ulong>("query_id");
                            LoggedSearchBE entity = queries[id];
                            entity.SelectedResults.Add(SearchAnalytics_PopulateSearchResultBE(r));
                        }
                    });
            }
            return queries.Values;
        }

        public IEnumerable<string> SearchAnalytics_GetPreviousSortedQueryTermsRecursively(IEnumerable<ulong> queryIds) {
            var previousTerms = new List<String>();
            var iterationsLeft = 3;
            while(queryIds.Any()) {
                var idClause = queryIds.ToCommaDelimitedString();
                var recursiveQueryIds = new List<ulong>();
                Catalog.NewQuery(string.Format(@"/* SearchAnalytics_GetPreviousSortedQueryTermsRecursively */
SELECT sorted_terms, ref_query_id FROM query_log as ql WHERE query_id IN ({0})", idClause))
                    .Execute(r => {
                        while(r.Read()) {
                            previousTerms.Add(r.Read<string>("sorted_terms"));
                            var previous = r.Read<ulong?>("ref_query_id");
                            if(previous.HasValue) {
                                recursiveQueryIds.Add(previous.Value);
                            }
                        }
                    });
                queryIds = recursiveQueryIds;
                iterationsLeft--;
                if(iterationsLeft <= 0) {
                    break;
                }
            }
            return previousTerms;
        }

        public IEnumerable<TermAggregateBE> SearchAnalytics_GetTerms(bool lowQuality, DateTime since, DateTime before, uint limit, uint offset) {
            var whereClause = new StringBuilder();
            var querySet = "query_log";
            if(lowQuality) {

                // Note (arnec): prepending space to force AND instead of WHERE in whereclause building
                whereClause.Append(" ");
                SearchAnalytics_BuildDateRangeClause(whereClause);
                querySet = string.Format(@"
(
    SELECT ql.query_id 
      FROM query_log ql
        LEFT JOIN query_result_log qrl ON ql.query_id = qrl.query_id
      WHERE qrl.query_id IS NOT NULL{0}
      GROUP BY ql.query_id HAVING count(*) > 3
    UNION
    SELECT ql.query_id
      FROM query_log ql
        LEFT JOIN query_result_log qrl ON ql.query_id = qrl.query_id
      WHERE qrl.query_id IS NULL{0}
)", whereClause);
                whereClause.Length = 0;
            } else {
                SearchAnalytics_BuildDateRangeClause(whereClause);

            }
            whereClause.Append(" GROUP BY qt.query_term ORDER BY count DESC");
            SearchAnalytics_BuildLimitOffsetClause(whereClause, limit, offset);
            var terms = new List<TermAggregateBE>();
            Catalog.NewQuery(string.Format(@"/* SearchAnalytics_GetTerms */
SELECT count(*) as count, qt.query_term
  FROM {0} as ql
    INNER JOIN query_term_map qtm ON ql.query_id = qtm.query_id
    INNER JOIN query_terms qt ON qtm.query_term_id = qt.query_term_id
{1}", querySet, whereClause))
                .With("SINCE", since)
                .With("BEFORE", before)
                .With("LIMIT", limit)
                .With("OFFSET", offset)
                .Execute(r => {
                    while(r.Read()) {
                        terms.Add(new TermAggregateBE() {
                            Term = r.Read<string>("query_term"),
                            Count = r.Read<int>("count")
                        });
                    }
                });
            return terms;
        }

        private LoggedSearchBE SearchAnalytics_PopulateTrackQueryBE(IDataReader reader) {
            return new LoggedSearchBE() {
                Id = reader.Read<ulong>("query_id"),
                Created = reader.Read<DateTime>("created"),
                PreviousId = reader.Read<ulong?>("ref_query_id"),
                RawQuery = reader.Read<string>("raw"),
                ParsedQuery = reader.Read<string>("parsed"),
                SortedTerms = reader.Read<string>("sorted_terms"),
                UserId = reader.Read<uint>("user_id"),
                Terms = new List<string>(),
                ResultCount = reader.Read<int>("result_count"),
                SelectedResults = new List<LoggedSearchResultBE>()
            };
        }

        private LoggedSearchResultBE SearchAnalytics_PopulateSearchResultBE(IDataReader r) {
            return new LoggedSearchResultBE() {
                Id = r.Read<ulong>("query_result_id"),
                QueryId = r.Read<ulong>("query_id"),
                Created = r.Read<DateTime>("created"),
                Position = r.Read<ushort>("result_position"),
                Rank = r.Read<double>("result_rank"),
                PageId = r.Read<uint>("page_id"),
                Type = r.Read<SearchResultType>("type"),
                TypeId = r.Read<uint>("type_id")
            };
        }

        private void SearchAnalytics_BuildDateRangeClause(StringBuilder whereClause) {
            whereClause.Append(whereClause.Length > 0 ? " AND " : " WHERE ");
            whereClause.Append("ql.created >= ?SINCE AND ql.created < ?BEFORE");
        }

        private void SearchAnalytics_BuildLimitOffsetClause(StringBuilder whereClause, uint? limit, uint? offset) {
            if(!limit.HasValue) {
                return;
            }
            whereClause.Append(" LIMIT ?LIMIT");
            if(!offset.HasValue) {
                return;
            }
            whereClause.Append(" OFFSET ?OFFSET");
        }

    }
}

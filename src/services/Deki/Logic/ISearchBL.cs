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
using MindTouch.Deki.Data;
using MindTouch.Deki.Search;
using MindTouch.Tasking;
using MindTouch.Xml;

namespace MindTouch.Deki.Logic {
    public interface ISearchBL {

        //--- Methods ---
        SearchQuery BuildQuery(string queryString, string constraint, SearchQueryParserType parserType, bool haveApiKey);
        SearchResult GetCachedQuery(SearchQuery query);
        void TrackQueryResultPick(ulong queryId, double rank, ushort position, uint pageId, SearchResultType type, uint? typeId);
        Result<XDoc> FormatResultSet(SearchResult searchResultSet, SetDiscriminator discriminator, bool explain, TrackingInfo trackingInfo, Result<XDoc> result);
        SearchResult CacheQuery(XDoc searchDoc, SearchQuery query, TrackingInfo trackingInfo);
        XDoc GetQueriesXml(string queryString, SearchAnalyticsQueryType type, bool lowQuality, DateTime since, DateTime before, uint limit, uint offset);
        XDoc GetQueryXml(ulong queryId);
        XDoc GetAggregateQueryXml(string queryString, DateTime since, DateTime before);
        XDoc GetAggregatedQueriesXml(string queryString, SearchAnalyticsQueryType type, bool lowQuality, DateTime since, DateTime before, uint limit, uint offset);
        XDoc GetTermsXml(bool lowQuality, DateTime since, DateTime before, uint limit, uint offset);
        void ClearCache();
        Result<IEnumerable<SearchResultItem>> GetItems(IEnumerable<ulong> ids, SearchResultType type, Result<IEnumerable<SearchResultItem>> result);
    }
}
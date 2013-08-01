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
using System.Threading;
using Autofac.Builder;
using MindTouch.Deki.Data;
using MindTouch.Deki.Logic;
using MindTouch.Deki.Search;
using MindTouch.Dream;
using MindTouch.Dream.Test;
using MindTouch.Extensions.Time;
using MindTouch.Tasking;
using MindTouch.Xml;
using Moq;
using NUnit.Framework;
using DreamTimes = MindTouch.Dream.Test.Mock.Times;
using System.Linq;

namespace MindTouch.Deki.Tests.SiteTests {

    // Note (arnec): These tests are separate from SearchTests, since they need to fire a up a new Deki instance with a custom autofac configuration
    [TestFixture]
    public class SearchTestsWithSearchBLMock {
        private DreamHostInfo _hostInfo;
        private Func<ISearchBL> _searchBLFactory;
        private Func<IPageBL> _pageBLFactory;
        private DreamServiceInfo _deki;
        private Plug _search;
        private IndexRebuilderMock _indexRebuilderMock;

        [TestFixtureSetUp]
        public void FixtureSetup() {
            _indexRebuilderMock = new IndexRebuilderMock();
            var builder = new ContainerBuilder();
            builder.Register(c => _searchBLFactory()).As<ISearchBL>().RequestScoped();
            builder.Register(c => _pageBLFactory()).As<IPageBL>().RequestScoped();
            builder.Register(_indexRebuilderMock).As<IIndexRebuilder>();
            _hostInfo = DreamTestHelper.CreateRandomPortHost(new XDoc("config").Elem("apikey", Utils.Settings.ApiKey), builder.Build());
            _hostInfo.Host.Self.At("load").With("name", "mindtouch.deki").Post(DreamMessage.Ok());
            _hostInfo.Host.Self.At("load").With("name", "mindtouch.deki.services").Post(DreamMessage.Ok());
            _hostInfo.Host.Self.At("load").With("name", "mindtouch.indexservice").Post(DreamMessage.Ok());
            _deki = DreamTestHelper.CreateService(_hostInfo, Utils.Settings.DekiConfig);
            _search = _deki.AtLocalHost.At("site", "query");
        }

        [TestFixtureTearDown]
        public void FixtureTeardown() {
            _hostInfo.Dispose();
        }

        [SetUp]
        public void Setup() {
            MockPlug.DeregisterAll();
        }

        [Test]
        public void Caching_search_with_apikey_does_not_pass_auth_info_to_lucene() {
            var searchMock = CreateMocks().Item1;
            var responseXml = new XDoc("response");
            var luceneXml = new XDoc("lucene");
            var searchQuery = new SearchQuery("raw", "processed", new LuceneClauseBuilder(), null);
            var searchResult = new SearchResult();
            searchMock.Setup(x => x.BuildQuery("foo", "", SearchQueryParserType.BestGuess, true)).Returns(searchQuery).AtMostOnce().Verifiable();
            searchMock.Setup(x => x.GetCachedQuery(searchQuery)).Returns((SearchResult)null).AtMostOnce().Verifiable();
            XUri luceneUriCalled = null;
            MockPlug.Register(Utils.Settings.LuceneMockUri, (p, v, u, req, res) => {
                luceneUriCalled = u;
                res.Return(DreamMessage.Ok(luceneXml));
            });
            searchMock.Setup(x => x.CacheQuery(It.Is<XDoc>(v => v == luceneXml), It.IsAny<SearchQuery>(), It.IsAny<TrackingInfo>()))
                .Returns(searchResult);
            searchMock.Setup(x => x.FormatResultSet(
                    searchResult,
                    It.IsAny<SetDiscriminator>(),
                    false,
                    It.IsAny<TrackingInfo>(),
                    It.IsAny<Result<XDoc>>()
                ))
                .Returns(new Result<XDoc>().WithReturn(responseXml));
            var response = _search
                .With("q", "foo")
                .With("apikey", Utils.Settings.ApiKey)
                .Get(new Result<DreamMessage>()).Wait();
            Assert.IsTrue(response.IsSuccessful, response.ToErrorString());
            Assert.IsNotNull(luceneUriCalled, "lucene was not called");
            Assert.AreEqual(Utils.Settings.LuceneMockUri.At("compact"), luceneUriCalled.WithoutQuery(), "lucene was called at wrong uri");
            Assert.IsNull(luceneUriCalled.GetParam("apiuri"), "lucene request contained an apiuri parameter");
            Assert.IsNull(luceneUriCalled.GetParam("userid"), "lucene request contained a userid parameter");
            Assert.AreEqual(searchQuery.LuceneQuery, luceneUriCalled.GetParam("q"), "lucene request had incorrect q parameter");
        }

        [Test]
        public void Caching_search_without_apikey_passes_auth_info_to_lucene() {
            var searchMock = CreateMocks().Item1;
            var responseXml = new XDoc("response");
            var luceneXml = new XDoc("lucene");
            var searchQuery = new SearchQuery("raw", "processed", new LuceneClauseBuilder(), null);
            var searchResult = new SearchResult();
            searchMock.Setup(x => x.BuildQuery("foo", "", SearchQueryParserType.BestGuess, false)).Returns(searchQuery).AtMostOnce().Verifiable();
            searchMock.Setup(x => x.GetCachedQuery(searchQuery)).Returns((SearchResult)null).AtMostOnce().Verifiable();
            XUri luceneUriCalled = null;
            MockPlug.Register(Utils.Settings.LuceneMockUri, (p, v, u, req, res) => {
                luceneUriCalled = u;
                res.Return(DreamMessage.Ok(luceneXml));
            });
            searchMock.Setup(x => x.CacheQuery(It.Is<XDoc>(v => v == luceneXml), It.IsAny<SearchQuery>(), It.IsAny<TrackingInfo>()))
                .Returns(searchResult);
            searchMock.Setup(x => x.FormatResultSet(
                    searchResult,
                    It.IsAny<SetDiscriminator>(),
                    false,
                    It.IsAny<TrackingInfo>(),
                    It.IsAny<Result<XDoc>>()
                ))
                .Returns(new Result<XDoc>().WithReturn(responseXml));
            var response = _search
                .With("q", "foo")
                .Get(new Result<DreamMessage>()).Wait();
            Assert.IsTrue(response.IsSuccessful, response.ToErrorString());
            Assert.IsNotNull(luceneUriCalled, "lucene was not called");
            Assert.AreEqual(Utils.Settings.LuceneMockUri.At("compact"), luceneUriCalled.WithoutQuery(), "lucene was called at wrong uri");
            Assert.IsNotNull(luceneUriCalled.GetParam("apiuri"), "lucene request did not contain an apiuri parameter");
            Assert.IsNotNull(luceneUriCalled.GetParam("userid"), "lucene request did not contain a userid parameter");
            Assert.AreEqual(searchQuery.LuceneQuery, luceneUriCalled.GetParam("q"), "lucene request had incorrect q parameter");
        }

        [Test]
        public void NonCaching_search_with_apikey_does_not_pass_auth_info_to_lucene() {
            var search = _deki.AtLocalHost.At("site", "search");
            var searchMock = CreateMocks().Item1;
            var luceneXml = new XDoc("lucene");
            var searchQuery = new SearchQuery("raw", "processed", new LuceneClauseBuilder(), null);
            searchMock.Setup(x => x.BuildQuery("foo", "", SearchQueryParserType.BestGuess, true)).Returns(searchQuery).AtMostOnce().Verifiable();
            XUri luceneUriCalled = null;
            MockPlug.Register(Utils.Settings.LuceneMockUri, (p, v, u, req, res) => {
                luceneUriCalled = u;
                res.Return(DreamMessage.Ok(luceneXml));
            });
            var response = search
                .With("q", "foo")
                .With("nocache", true)
                .With("apikey", Utils.Settings.ApiKey)
                .Get(new Result<DreamMessage>()).Wait();
            Assert.IsTrue(response.IsSuccessful, response.ToErrorString());
            Assert.IsNotNull(luceneUriCalled, "lucene was not called");
            Assert.AreEqual(Utils.Settings.LuceneMockUri, luceneUriCalled.WithoutQuery(), "lucene was called at wrong uri");
            Assert.IsNull(luceneUriCalled.GetParam("apiuri"), "lucene request contained an apiuri parameter");
            Assert.IsNull(luceneUriCalled.GetParam("userid"), "lucene request contained a userid parameter");
            Assert.AreEqual(searchQuery.LuceneQuery, luceneUriCalled.GetParam("q"), "lucene request had incorrect q parameter");
        }

        [Test]
        public void NonCaching_search_without_apikey_passes_auth_info_to_lucene() {
            var search = _deki.AtLocalHost.At("site", "search");
            var searchMock = CreateMocks().Item1;
            var luceneXml = new XDoc("lucene");
            var searchQuery = new SearchQuery("raw", "processed", new LuceneClauseBuilder(), null);
            searchMock.Setup(x => x.BuildQuery("foo", "", SearchQueryParserType.BestGuess, false)).Returns(searchQuery).AtMostOnce().Verifiable();
            XUri luceneUriCalled = null;
            MockPlug.Register(Utils.Settings.LuceneMockUri, (p, v, u, req, res) => {
                luceneUriCalled = u;
                res.Return(DreamMessage.Ok(luceneXml));
            });
            var response = search
                .With("q", "foo")
                .With("nocache", true)
                .Get(new Result<DreamMessage>()).Wait();
            Assert.IsTrue(response.IsSuccessful, response.ToErrorString());
            Assert.IsNotNull(luceneUriCalled, "lucene was not called");
            Assert.AreEqual(Utils.Settings.LuceneMockUri, luceneUriCalled.WithoutQuery(), "lucene was called at wrong uri");
            Assert.IsNotNull(luceneUriCalled.GetParam("apiuri"), "lucene request did not contain an apiuri parameter");
            Assert.IsNotNull(luceneUriCalled.GetParam("userid"), "lucene request did not contain a userid parameter");
            Assert.AreEqual(searchQuery.LuceneQuery, luceneUriCalled.GetParam("q"), "lucene request had incorrect q parameter");
        }

        [Test]
        public void Ranked_search_hits_lucene_and_caches_result() {
            var searchMock = CreateMocks().Item1;
            var luceneMock = MockPlug.Setup(Utils.Settings.LuceneMockUri);
            var before = DateTime.UtcNow.AddDays(-2).WithoutMilliseconds();
            var since = DateTime.UtcNow.AddDays(-1).WithoutMilliseconds();
            uint limit = 10;
            uint offset = 20;
            var responseXml = new XDoc("response");
            var luceneXml = new XDoc("lucene");
            var searchQuery = new SearchQuery("raw", "processed", new LuceneClauseBuilder(), null);
            var searchResult = new SearchResult();
            searchMock.Setup(x => x.BuildQuery("foo", "", SearchQueryParserType.BestGuess, false)).Returns(searchQuery).AtMostOnce().Verifiable();
            searchMock.Setup(x => x.GetCachedQuery(searchQuery)).Returns((SearchResult)null).AtMostOnce().Verifiable();
            luceneMock
                .Verb("GET")
                .At("compact")
                .With("wikiid", "default")
                .With("q", searchQuery.LuceneQuery)
                .Returns(luceneXml)
                .ExpectCalls(DreamTimes.Once());
            searchMock.Setup(x => x.CacheQuery(It.Is<XDoc>(v => v == luceneXml), searchQuery, It.Is<TrackingInfo>(t => !t.QueryId.HasValue)))
                .Returns(searchResult)
                .AtMostOnce()
                .Verifiable();
            Predicate<SetDiscriminator> discriminator = s => {
                return s.Limit == limit && s.Offset == offset && s.SortField == "rank" && !s.Ascending;
            };
            searchMock.Setup(x => x.FormatResultSet(
                    searchResult,
                    It.Is<SetDiscriminator>(s => discriminator(s)),
                    false,
                    It.Is<TrackingInfo>(t => !t.QueryId.HasValue),
                    It.IsAny<Result<XDoc>>()
                ))
                .Returns(new Result<XDoc>().WithReturn(responseXml))
                .AtMostOnce()
                .Verifiable();
            var response = _search
                .With("q", "foo")
                .With("since", since)
                .With("before", before)
                .With("limit", limit)
                .With("offset", offset)
                .Get(new Result<DreamMessage>()).Wait();
            Assert.IsTrue(response.IsSuccessful, response.ToErrorString());
            searchMock.VerifyAll();
            luceneMock.Verify();
            Assert.AreEqual(responseXml, response.ToDocument());
        }

        [Test]
        public void Can_get_ranked_search_from_cache_and_does_not_hit_lucene() {
            var searchMock = CreateMocks().Item1;
            var luceneMock = MockPlug.Setup(Utils.Settings.LuceneMockUri);
            luceneMock
                .Verb("GET")
                .At("compact")
                .ExpectCalls(DreamTimes.Never());
            var before = DateTime.UtcNow.AddDays(-2).WithoutMilliseconds();
            var since = DateTime.UtcNow.AddDays(-1).WithoutMilliseconds();
            uint limit = 10;
            uint offset = 20;
            ulong queryId = 42;
            var responseXml = new XDoc("response");
            var searchResult = new SearchResult();
            var searchQuery = new SearchQuery("raw", "processed", new LuceneClauseBuilder(), null);
            searchMock.Setup(x => x.BuildQuery("foo", "", SearchQueryParserType.BestGuess, false)).Returns(searchQuery).AtMostOnce().Verifiable();
            searchMock.Setup(x => x.GetCachedQuery(searchQuery)).Returns(searchResult).AtMostOnce().Verifiable();
            Predicate<SetDiscriminator> discriminator = s => {
                return s.Limit == limit && s.Offset == offset && s.SortField == "rank" && !s.Ascending;
            };
            searchMock.Setup(x => x.FormatResultSet(
                    searchResult,
                    It.Is<SetDiscriminator>(s => discriminator(s)),
                    false,
                    It.Is<TrackingInfo>(t => t.QueryId == queryId),
                    It.IsAny<Result<XDoc>>()
                ))
                .Returns(new Result<XDoc>().WithReturn(responseXml))
                .AtMostOnce()
                .Verifiable();
            var response = _search
                .With("q", "foo")
                .With("queryid", queryId.ToString())
                .With("since", since)
                .With("before", before)
                .With("limit", limit)
                .With("offset", offset)
                .Get(new Result<DreamMessage>()).Wait();
            Assert.IsTrue(response.IsSuccessful, response.ToErrorString());
            searchMock.VerifyAll();
            luceneMock.Verify();
            Assert.AreEqual(responseXml, response.ToDocument());
        }

        [Test]
        public void Ranked_search_tracks_previous_queryid() {
            var searchMock = CreateMocks().Item1;
            var luceneMock = MockPlug.Setup(Utils.Settings.LuceneMockUri);
            var before = DateTime.UtcNow.AddDays(-2).WithoutMilliseconds();
            var since = DateTime.UtcNow.AddDays(-1).WithoutMilliseconds();
            uint limit = 10;
            uint offset = 20;
            ulong previousqueryid = 43;
            var responseXml = new XDoc("response");
            var luceneXml = new XDoc("lucene");
            var searchQuery = new SearchQuery("raw", "processed", new LuceneClauseBuilder(), null);
            var searchResult = new SearchResult();
            searchMock.Setup(x => x.BuildQuery("foo", "", SearchQueryParserType.BestGuess, false)).Returns(searchQuery).AtMostOnce().Verifiable();
            searchMock.Setup(x => x.GetCachedQuery(searchQuery)).Returns((SearchResult)null).AtMostOnce().Verifiable();
            luceneMock
                .Verb("GET")
                .At("compact")
                .With("wikiid", "default")
                .With("q", searchQuery.LuceneQuery)
                .Returns(luceneXml)
                .ExpectCalls(DreamTimes.Once());
            searchMock.Setup(x => x.CacheQuery(It.IsAny<XDoc>(), searchQuery, It.Is<TrackingInfo>(t => t.PreviousQueryId == previousqueryid)))
                .Returns(searchResult)
                .AtMostOnce()
                .Verifiable();
            searchMock.Setup(x => x.FormatResultSet(
                    searchResult,
                    It.IsAny<SetDiscriminator>(),
                    false,
                    It.IsAny<TrackingInfo>(),
                    It.IsAny<Result<XDoc>>())
                )
                .Returns(new Result<XDoc>().WithReturn(responseXml))
                .AtMostOnce()
                .Verifiable();
            var response = _search
                .With("q", "foo")
                .With("previousqueryid", previousqueryid.ToString())
                .With("since", since)
                .With("before", before)
                .With("limit", limit)
                .With("offset", offset)
                .Get(new Result<DreamMessage>()).Wait();
            Assert.IsTrue(response.IsSuccessful, response.ToErrorString());
            searchMock.VerifyAll();
            luceneMock.Verify();
            Assert.AreEqual(responseXml, response.ToDocument());
        }

        [Test]
        public void Can_track_search_picks() {
            var mock = CreateMocks().Item1;
            ulong queryId = 42;
            double rank = 0.5;
            ushort position = 10;
            uint pageId = 33;
            SearchResultType type = SearchResultType.File;
            uint? typeId = 34;
            mock.Setup(x => x.TrackQueryResultPick(queryId, rank, position, pageId, type, typeId)).AtMostOnce().Verifiable();
            var response = _search.At(queryId.ToString())
                .With("rank", rank)
                .With("position", position)
                .With("pageid", pageId)
                .With("type", type.ToString())
                .With("typeid", typeId.ToString())
                .Post(DreamMessage.Ok(), new Result<DreamMessage>()).Wait();
            Assert.IsTrue(response.IsSuccessful, response.ToErrorString());
            mock.VerifyAll();
        }

        [Test]
        public void Can_get_query_log_item() {
            var mock = CreateMocks().Item1;
            ulong queryId = 42;
            var responseXml = new XDoc("response");
            mock.Setup(x => x.GetQueryXml(queryId)).Returns(responseXml).AtMostOnce().Verifiable();
            var response = _search.At("log", queryId.ToString())
                .With("apikey", Utils.Settings.ApiKey)
                .Get(new Result<DreamMessage>()).Wait();
            Assert.IsTrue(response.IsSuccessful, response.ToErrorString());
            mock.VerifyAll();
            Assert.AreEqual(responseXml, response.ToDocument());
        }

        [Test]
        public void Can_get_query_aggregate() {
            var mock = CreateMocks().Item1;
            var before = DateTime.UtcNow.AddDays(-2).WithoutMilliseconds();
            var since = DateTime.UtcNow.AddDays(-1).WithoutMilliseconds();
            var responseXml = new XDoc("response");
            mock.Setup(x => x.GetAggregateQueryXml("foo", since, before)).Returns(responseXml).AtMostOnce().Verifiable();
            var response = _search.At("log", "=foo")
                .With("since", since)
                .With("before", before)
                .With("apikey", Utils.Settings.ApiKey)
                .Get(new Result<DreamMessage>()).Wait();
            Assert.IsTrue(response.IsSuccessful, response.ToErrorString());
            mock.VerifyAll();
            Assert.AreEqual(responseXml, response.ToDocument());
        }

        [Test]
        public void Can_get_query_log() {
            var mock = CreateMocks().Item1;
            var before = DateTime.UtcNow.AddDays(-2).WithoutMilliseconds();
            var since = DateTime.UtcNow.AddDays(-1).WithoutMilliseconds();
            uint limit = 10;
            uint offset = 20;
            var responseXml = new XDoc("response");
            mock.Setup(x => x.GetQueriesXml(null, SearchAnalyticsQueryType.All, false, since, before, limit, offset)).Returns(responseXml)
                .AtMostOnce()
                .Verifiable();
            var response = _search.At("log")
                .With("since", since)
                .With("before", before)
                .With("limit", limit)
                .With("offset", offset)
                .With("apikey", Utils.Settings.ApiKey)
                .Get(new Result<DreamMessage>()).Wait();
            Assert.IsTrue(response.IsSuccessful, response.ToErrorString());
            mock.VerifyAll();
            Assert.AreEqual(responseXml, response.ToDocument());
        }

        [Test]
        public void Can_get_query_log_for_empty_query() {
            var mock = CreateMocks().Item1;
            var before = DateTime.UtcNow.AddDays(-2).WithoutMilliseconds();
            var since = DateTime.UtcNow.AddDays(-1).WithoutMilliseconds();
            uint limit = 10;
            uint offset = 20;
            var responseXml = new XDoc("response");
            mock.Setup(x => x.GetQueriesXml("", SearchAnalyticsQueryType.QueryString, false, since, before, limit, offset)).Returns(responseXml)
                .AtMostOnce()
                .Verifiable();
            var response = _search.At("log")
                .With("query", "")
                .With("since", since)
                .With("before", before)
                .With("limit", limit)
                .With("offset", offset)
                .With("apikey", Utils.Settings.ApiKey)
                .Get(new Result<DreamMessage>()).Wait();
            Assert.IsTrue(response.IsSuccessful, response.ToErrorString());
            mock.VerifyAll();
            Assert.AreEqual(responseXml, response.ToDocument());
        }

        [Test]
        public void Can_get_query_log_by_querystring() {
            var mock = CreateMocks().Item1;
            var before = DateTime.UtcNow.AddDays(-2).WithoutMilliseconds();
            var since = DateTime.UtcNow.AddDays(-1).WithoutMilliseconds();
            uint limit = 10;
            uint offset = 20;
            var responseXml = new XDoc("response");
            mock.Setup(x => x.GetQueriesXml("foo", SearchAnalyticsQueryType.QueryString, false, since, before, limit, offset)).Returns(responseXml)
                .AtMostOnce()
                .Verifiable();
            var response = _search.At("log")
                .With("query", "foo")
                .With("since", since)
                .With("before", before)
                .With("limit", limit)
                .With("offset", offset)
                .With("apikey", Utils.Settings.ApiKey)
                .Get(new Result<DreamMessage>()).Wait();
            Assert.IsTrue(response.IsSuccessful, response.ToErrorString());
            mock.VerifyAll();
            Assert.AreEqual(responseXml, response.ToDocument());
        }

        [Test]
        public void Can_get_query_log_by_term() {
            var mock = CreateMocks().Item1;
            var before = DateTime.UtcNow.AddDays(-2).WithoutMilliseconds();
            var since = DateTime.UtcNow.AddDays(-1).WithoutMilliseconds();
            uint limit = 10;
            uint offset = 20;
            var responseXml = new XDoc("response");
            mock.Setup(x => x.GetQueriesXml("foo", SearchAnalyticsQueryType.Term, false, since, before, limit, offset)).Returns(responseXml)
                .AtMostOnce()
                .Verifiable();
            var response = _search.At("log")
                .With("term", "foo")
                .With("since", since)
                .With("before", before)
                .With("limit", limit)
                .With("offset", offset)
                .With("apikey", Utils.Settings.ApiKey)
                .Get(new Result<DreamMessage>()).Wait();
            Assert.IsTrue(response.IsSuccessful, response.ToErrorString());
            mock.VerifyAll();
            Assert.AreEqual(responseXml, response.ToDocument());
        }

        [Test]
        public void Can_get_aggregated_query_log_by() {
            var mock = CreateMocks().Item1;
            var before = DateTime.UtcNow.AddDays(-2).WithoutMilliseconds();
            var since = DateTime.UtcNow.AddDays(-1).WithoutMilliseconds();
            uint limit = 10;
            uint offset = 20;
            var responseXml = new XDoc("response");
            mock.Setup(x => x.GetAggregatedQueriesXml(null, SearchAnalyticsQueryType.All, false, since, before, limit, offset)).Returns(responseXml)
                .AtMostOnce()
                .Verifiable();
            var response = _search.At("log")
                .With("groupby", "query")
                .With("since", since)
                .With("before", before)
                .With("limit", limit)
                .With("offset", offset)
                .With("apikey", Utils.Settings.ApiKey)
                .Get(new Result<DreamMessage>()).Wait();
            Assert.IsTrue(response.IsSuccessful, response.ToErrorString());
            mock.VerifyAll();
            Assert.AreEqual(responseXml, response.ToDocument());
        }

        [Test]
        public void Can_get_aggregated_query_log_by_querystring() {
            var mock = CreateMocks().Item1;
            var before = DateTime.UtcNow.AddDays(-2).WithoutMilliseconds();
            var since = DateTime.UtcNow.AddDays(-1).WithoutMilliseconds();
            uint limit = 10;
            uint offset = 20;
            var responseXml = new XDoc("response");
            mock.Setup(x => x.GetAggregatedQueriesXml("foo", SearchAnalyticsQueryType.QueryString, false, since, before, limit, offset)).Returns(responseXml)
                .AtMostOnce()
                .Verifiable();
            var response = _search.At("log")
                .With("query", "foo")
                .With("groupby", "query")
                .With("since", since)
                .With("before", before)
                .With("limit", limit)
                .With("offset", offset)
                .With("apikey", Utils.Settings.ApiKey)
                .Get(new Result<DreamMessage>()).Wait();
            Assert.IsTrue(response.IsSuccessful, response.ToErrorString());
            mock.VerifyAll();
            Assert.AreEqual(responseXml, response.ToDocument());
        }

        [Test]
        public void Can_get_aggregated_query_log_by_term() {
            var mock = CreateMocks().Item1;
            var before = DateTime.UtcNow.AddDays(-2).WithoutMilliseconds();
            var since = DateTime.UtcNow.AddDays(-1).WithoutMilliseconds();
            uint limit = 10;
            uint offset = 20;
            var responseXml = new XDoc("response");
            mock.Setup(x => x.GetAggregatedQueriesXml("foo", SearchAnalyticsQueryType.Term, false, since, before, limit, offset)).Returns(responseXml)
                .AtMostOnce()
                .Verifiable();
            var response = _search.At("log")
                .With("term", "foo")
                .With("groupby", "query")
                .With("since", since)
                .With("before", before)
                .With("limit", limit)
                .With("offset", offset)
                .With("apikey", Utils.Settings.ApiKey)
                .Get(new Result<DreamMessage>()).Wait();
            Assert.IsTrue(response.IsSuccessful, response.ToErrorString());
            mock.VerifyAll();
            Assert.AreEqual(responseXml, response.ToDocument());
        }

        [Test]
        public void Can_get_Terms() {
            var mock = CreateMocks().Item1;
            var before = DateTime.UtcNow.AddDays(-2).WithoutMilliseconds();
            var since = DateTime.UtcNow.AddDays(-1).WithoutMilliseconds();
            uint limit = 10;
            uint offset = 20;
            var termsXml = new XDoc("results");
            mock.Setup(x => x.GetTermsXml(false, since, before, limit, offset)).Returns(termsXml).AtMostOnce().Verifiable();
            var response = _search.At("log", "terms")
                .With("since", since)
                .With("before", before)
                .With("limit", limit)
                .With("offset", offset)
                .With("apikey", Utils.Settings.ApiKey)
                .Get(new Result<DreamMessage>()).Wait();
            Assert.IsTrue(response.IsSuccessful, response.ToErrorString());
            mock.VerifyAll();
            Assert.AreEqual(termsXml, response.ToDocument());
        }

        [Test]
        public void Reindex_clears_search_cache() {
            var luceneMock = MockPlug.Setup(Utils.Settings.LuceneMockUri);
            luceneMock
                .Verb("DELETE")
                .At("clear")
                .With("wikiid", "default")
                .Returns(DreamMessage.Ok())
                .ExpectCalls(DreamTimes.Once());
            var mock = CreateMocks().Item1;
            mock.Setup(x => x.ClearCache()).AtMostOnce().Verifiable();
            var response = _deki.AtLocalHost.At("site", "search", "rebuild")
                .With("apikey", Utils.Settings.ApiKey)
                .Post(new Result<DreamMessage>()).Wait();
            Assert.IsTrue(response.IsSuccessful, response.ToErrorString());
            mock.VerifyAll();
            luceneMock.Verify();
            Assert.IsTrue(_indexRebuilderMock.WaitHandle.WaitOne(10.Seconds()), "index rebuilder wasn't triggered");
        }

        [Test]
        public void Can_repair_index() {
            _indexRebuilderMock.RepairCalls.Clear();
            var response = _deki.AtLocalHost.At("site", "search", "repair")
                .With("apikey", Utils.Settings.ApiKey)
                .Post(new Result<DreamMessage>()).Wait();
            Assert.IsTrue(response.IsSuccessful, response.ToErrorString());
            Assert.AreEqual(1, _indexRebuilderMock.RepairCalls.Count);
            var call = _indexRebuilderMock.RepairCalls.First();
            Assert.IsFalse(call.CheckOnly, "checkonly was set");
            Assert.IsFalse(call.Verbose, "verbose was set");
        }

        [Test]
        public void Can_do_a_repair_dry_run() {
            _indexRebuilderMock.RepairCalls.Clear();
            var response = _deki.AtLocalHost.At("site", "search", "repair")
                .With("checkonly", true)
                .With("apikey", Utils.Settings.ApiKey)
                .Post(new Result<DreamMessage>()).Wait();
            Assert.IsTrue(response.IsSuccessful, response.ToErrorString());
            Assert.AreEqual(1, _indexRebuilderMock.RepairCalls.Count);
            var call = _indexRebuilderMock.RepairCalls.First();
            Assert.IsTrue(call.CheckOnly, "checkonly was not set");
            Assert.IsFalse(call.Verbose, "verbose was set");
        }

        [Test]
        public void Can_get_verbose_repair_reporting() {
            _indexRebuilderMock.RepairCalls.Clear();
            var response = _deki.AtLocalHost.At("site", "search", "repair")
                .With("verbose", true)
                .With("apikey", Utils.Settings.ApiKey)
                .Post(new Result<DreamMessage>()).Wait();
            Assert.IsTrue(response.IsSuccessful, response.ToErrorString());
            Assert.AreEqual(1, _indexRebuilderMock.RepairCalls.Count);
            var call = _indexRebuilderMock.RepairCalls.First();
            Assert.IsFalse(call.CheckOnly, "checkonly was set");
            Assert.IsTrue(call.Verbose, "verbose was not set");
        }

        private Tuplet<Mock<ISearchBL>, Mock<IPageBL>> CreateMocks() {
            var searchMock = new Mock<ISearchBL>();
            var pageMock = new Mock<IPageBL>();

            // Note (arnec): we have to wrap the mocks so that the type seen by Deki comes from a Deki signed assembly
            _searchBLFactory = () => new SearchBLMock(searchMock.Object);
            _pageBLFactory = () => new PageBLMock(pageMock.Object);
            return new Tuplet<Mock<ISearchBL>, Mock<IPageBL>>(searchMock, pageMock);
        }

        private class IndexRebuilderMock : IIndexRebuilder {
            public readonly ManualResetEvent WaitHandle = new ManualResetEvent(false);
            public void Rebuild() {
                WaitHandle.Set();
            }

            public class RepairCall {
                public bool CheckOnly;
                public bool Verbose;
            }
            public List<RepairCall> RepairCalls = new List<RepairCall>();
            public Result<XDoc> Repair(bool checkOnly, bool verbose, Result<XDoc> result) {
                RepairCalls.Add(new RepairCall { CheckOnly = checkOnly, Verbose = verbose });
                result.Return(new XDoc("report"));
                return result;
            }
        }

        private class SearchBLMock : ISearchBL {
            private readonly ISearchBL _instance;

            public SearchBLMock(ISearchBL instance) {
                _instance = instance;
            }

            #region ISearchBL Members

            public SearchQuery BuildQuery(string queryString, string constraint, SearchQueryParserType parserType, bool haveApiKey) {
                return _instance.BuildQuery(queryString, constraint, parserType, haveApiKey);
            }

            public SearchResult GetCachedQuery(SearchQuery query) {
                return _instance.GetCachedQuery(query);
            }

            public void TrackQueryResultPick(ulong queryId, double rank, ushort position, uint pageId, SearchResultType type, uint? typeId) {
                _instance.TrackQueryResultPick(queryId, rank, position, pageId, type, typeId);
            }

            public Result<XDoc> FormatResultSet(SearchResult searchResultSet, SetDiscriminator discriminator, bool explain, TrackingInfo trackingInfo, Result<XDoc> result) {
                return _instance.FormatResultSet(searchResultSet, discriminator, explain, trackingInfo, result);
            }

            public SearchResult CacheQuery(XDoc searchDoc, SearchQuery query, TrackingInfo trackingInfo) {
                return _instance.CacheQuery(searchDoc, query, trackingInfo);
            }

            public XDoc GetQueriesXml(string queryString, SearchAnalyticsQueryType type, bool lowQuality, DateTime since, DateTime before, uint limit, uint offset) {
                return _instance.GetQueriesXml(queryString, type, lowQuality, since, before, limit, offset);
            }

            public XDoc GetQueryXml(ulong queryId) {
                return _instance.GetQueryXml(queryId);
            }

            public XDoc GetAggregateQueryXml(string queryString, DateTime since, DateTime before) {
                return _instance.GetAggregateQueryXml(queryString, since, before);
            }

            public XDoc GetAggregatedQueriesXml(string queryString, SearchAnalyticsQueryType type, bool lowQuality, DateTime since, DateTime before, uint limit, uint offset) {
                return _instance.GetAggregatedQueriesXml(queryString, type, lowQuality, since, before, limit, offset);
            }

            public XDoc GetTermsXml(bool lowQuality, DateTime since, DateTime before, uint limit, uint offset) {
                return _instance.GetTermsXml(lowQuality, since, before, limit, offset);
            }

            public void ClearCache() {
                _instance.ClearCache();
            }

            public Result<IEnumerable<SearchResultItem>> GetItems(IEnumerable<ulong> ids, SearchResultType type, Result<IEnumerable<SearchResultItem>> result) {
                throw new NotImplementedException();
            }
            #endregion
        }

        private class PageBLMock : IPageBL {
            private readonly IPageBL _instance;

            public PageBLMock(IPageBL instance) {
                _instance = instance;
            }
            public PageBE GetPageById(ulong id) {
                return _instance.GetPageById(id);
            }

            public IEnumerable<PageBE> GetAllPagesChunked(NS[] whitelist) {
                throw new NotImplementedException();
            }

            public XUri GetUriCanonical(PageBE page) {
                throw new NotImplementedException();
            }
        }
    }
}

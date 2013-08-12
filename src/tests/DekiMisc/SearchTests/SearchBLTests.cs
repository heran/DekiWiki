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
using MindTouch.Cache;
using MindTouch.Deki.Data;
using MindTouch.Deki.Exceptions;
using MindTouch.Deki.Logic;
using MindTouch.Deki.Search;
using MindTouch.Deki.Util;
using MindTouch.Dream;
using MindTouch.Dream.Test;
using MindTouch.Dream.Test.Mock;
using MindTouch.Tasking;
using MindTouch.Xml;
using Moq;
using NUnit.Framework;
using Times = Moq.Times;
using DreamTimes = MindTouch.Dream.Test.Mock.Times;

namespace MindTouch.Deki.Tests.SearchTests {

    [TestFixture]
    public class SearchBLTests {

        //--- Class Fields ---
        private static readonly ILog _log = LogUtils.CreateLog();

        //--- Fields ---
        private SearchBL _searchBL;
        private Mock<IInstanceSettings> _settings;
        private Mock<IDekiDataSession> _session;
        private Mock<IKeyValueCache> _cache;
        private readonly UserBE _user = new UserBE { ID = 1234 };
        private readonly string _wikiId = "default";
        private readonly XUri _apiUri = new XUri("mock://api/");
        private readonly Plug _searchPlug = Plug.New("mock://searchbl/lucene");

        [SetUp]
        public void Setup() {
            MockPlug.DeregisterAll();
            _searchBL = null;
            _settings = new Mock<IInstanceSettings>();
            _settings.Setup(x => x.GetValue("search/termquery", It.IsAny<string>())).Returns((string key, string def) => def);
            _session = new Mock<IDekiDataSession>();
            _cache = new Mock<IKeyValueCache>();
            var cacheFactory = new InMemoryKeyValueCacheFactory(TaskTimerFactory.Current);
            var searchSerializer = new SearchSerializer();
            cacheFactory.SetSerializer<SearchResult>(searchSerializer);
            cacheFactory.SetSerializer<SearchResultDetail>(searchSerializer);
        }

        [Test]
        [ExpectedException(typeof(FormatException))]
        public void Bad_termquery_string_in_settings_throws_FormatException_on_BestGuess() {

            // Arrange
            _settings.Setup(x => x.GetValue("search/termquery", It.IsAny<string>())).Returns("{0}{1}");

            // Act
            Search.BuildQuery("query", "constraint", SearchQueryParserType.BestGuess, true);
        }

        [Test]
        public void Can_build_default_BestGuess_term_query_without_constraints_or_apikey() {

            // Act
            var query = Search.BuildQuery("foo bar", null, SearchQueryParserType.BestGuess, false);

            // Assert
            Assert.AreEqual("foo bar", query.Raw);
            Assert.AreEqual(
                "+(content:(foo bar) title:(foo bar)^10 path.title:(foo bar)^4 description:(foo bar)^3 tag:(foo bar)^2 comments:(foo bar) ) +type:(wiki document image comment binary) -namespace:\"template_talk\" -namespace:\"help\" -namespace:\"help_talk\"",
                query.LuceneQuery);
        }

        [Test]
        public void Can_build_default_BestGuess_term_query_without_constraints_but_with_apikey() {

            // Act
            var query = Search.BuildQuery("foo bar", null, SearchQueryParserType.BestGuess, true);

            // Assert
            Assert.AreEqual("foo bar", query.Raw);
            Assert.AreEqual(
                "content:(foo bar) title:(foo bar)^10 path.title:(foo bar)^4 description:(foo bar)^3 tag:(foo bar)^2 comments:(foo bar) ",
                query.LuceneQuery);
        }

        [Test]
        public void Can_build_default_BestGuess_term_query_with_prefixed_terms() {

            // Act
            var query = Search.BuildQuery("tag:bar foo", null, SearchQueryParserType.BestGuess, true);

            // Assert
            Assert.AreEqual("tag:bar foo", query.Raw);
            Assert.AreEqual(
                "content:(foo) title:(foo)^10 path.title:(foo)^4 description:(foo)^3 tag:(foo)^2 comments:(foo) tag:bar ",
                query.LuceneQuery);
        }

        [Test]
        public void Can_build_default_BestGuess_term_query_with_constraints_and_apikey() {

            // Act
            var query = Search.BuildQuery("foo bar", "constraint", SearchQueryParserType.BestGuess, true);

            // Assert
            Assert.AreEqual("foo bar", query.Raw);
            Assert.AreEqual(
                "+(content:(foo bar) title:(foo bar)^10 path.title:(foo bar)^4 description:(foo bar)^3 tag:(foo bar)^2 comments:(foo bar) ) +constraint",
                query.LuceneQuery);
        }

        [Test]
        public void Can_build_default_BestGuess_term_query_with_constraints_and_no_apikey() {

            // Act
            var query = Search.BuildQuery("foo bar", "constraint", SearchQueryParserType.BestGuess, false);

            // Assert
            Assert.AreEqual("foo bar", query.Raw);
            Assert.AreEqual(
                "+(content:(foo bar) title:(foo bar)^10 path.title:(foo bar)^4 description:(foo bar)^3 tag:(foo bar)^2 comments:(foo bar) ) +type:(wiki document image comment binary) +constraint -namespace:\"template_talk\" -namespace:\"help\" -namespace:\"help_talk\"",
                query.LuceneQuery);
        }

        [Test]
        public void Can_build_custom_BestGuess_term_query_with_constraints_and_apikey() {

            // Arrange
            _settings.Setup(x => x.GetValue("search/termquery", It.IsAny<string>())).Returns("custom:{0}");

            // Act
            var query = Search.BuildQuery("foo bar", "constraint", SearchQueryParserType.BestGuess, true);

            // Assert
            Assert.AreEqual("foo bar", query.Raw);
            Assert.AreEqual(
                "+(custom:(foo bar) ) +constraint",
                query.LuceneQuery);
        }

        [Test]
        public void Can_build_BestGuess_lucene_query_with_constraints_and_apikey() {

            // Arrange
            _settings.Setup(x => x.GetValue("search/termquery", It.IsAny<string>())).Returns("custom:{0}");

            // Act
            var query = Search.BuildQuery("tag:foo^4", "constraint", SearchQueryParserType.BestGuess, true);

            // Assert
            Assert.AreEqual("tag:foo^4", query.Raw);
            Assert.AreEqual(
                "+(tag:foo^4) +constraint",
                query.LuceneQuery);
        }

        [Test]
        public void Can_build_constraint_only_query_that_limits_type() {

            // Act
            var query = Search.BuildQuery("", "type:wiki", SearchQueryParserType.BestGuess, false);

            // Assert
            Assert.AreEqual("", query.Raw);
            Assert.AreEqual(
                "+type:(wiki document image comment binary) +type:wiki",
                query.LuceneQuery);
        }

        [Test]
        public void Can_build_BestGuess_lucene_query_without_constraints_or_apikey() {

            // Arrange
            _settings.Setup(x => x.GetValue("search/termquery", It.IsAny<string>())).Returns("custom:{0}");

            // Act
            var query = Search.BuildQuery("tag:foo^4", null, SearchQueryParserType.BestGuess, false);

            // Assert
            Assert.AreEqual("tag:foo^4", query.Raw);
            Assert.AreEqual(
                "+(tag:foo^4) +type:(wiki document image comment binary)",
                query.LuceneQuery);
        }

        [Test]
        public void Can_build_Lucene_query() {

            // Arrange
            _settings.Setup(x => x.GetValue("search/termquery", It.IsAny<string>())).Returns("custom:{0}");

            // Act
            var query = Search.BuildQuery("tag:foo^4", null, SearchQueryParserType.Lucene, false);

            // Assert
            Assert.AreEqual("tag:foo^4", query.Raw);
            Assert.AreEqual(
                "+(tag:foo^4) +type:(wiki document image comment binary)",
                query.LuceneQuery);
        }

        [Test]
        public void Building_BestGuess_query_without_query_string_produces_empty_query() {

            // Act
            var query = Search.BuildQuery("", null, SearchQueryParserType.BestGuess, false);

            // Assert
            Assert.IsTrue(query.IsEmpty);
        }

        [Test]
        public void Building_Filename_query_without_querystring_but_constraint_creates_empty_query() {

            // Act
            var query = Search.BuildQuery("", "constraint", SearchQueryParserType.Filename, false);

            // Assert
            Assert.IsTrue(query.IsEmpty);
        }

        [Test]
        public void Can_build_Filename_query_with_dash_in_filename() {

            // Act
            var query = Search.BuildQuery("foo-bar", null, SearchQueryParserType.Filename, false);

            // Assert
            Assert.AreEqual("+(filename:foo\\-bar* extension:.foo\\-bar description:foo\\-bar) +type:(document image binary)", query.LuceneQuery);
        }

        [Test]
        public void Can_build_Filename_query_with_no_filename() {

            // Act
            var query = Search.BuildQuery(null, null, SearchQueryParserType.Filename, false);

            // Assert
            Assert.AreEqual("", query.LuceneQuery);
        }

        [Test]
        public void Can_build_Filename_query_with_space_in_filename() {

            // Act
            var query = Search.BuildQuery("foo bar", null, SearchQueryParserType.Filename, false);

            // Assert
            Assert.AreEqual("+(filename:foo\\ bar* extension:.foo\\ bar description:foo\\ bar) +type:(document image binary)", query.LuceneQuery);
        }

        [Test]
        public void Can_use_custom_constraint_on_Filename_query() {

            // Act
            var query = Search.BuildQuery("foo", "custom", SearchQueryParserType.Filename, false);

            // Assert
            Assert.AreEqual("+(filename:foo* extension:.foo description:foo) +type:(document image binary) +custom", query.LuceneQuery);
        }

        [Test]
        public void Plan_query_can_use_caching_path() {

            // Act
            var query = Search.BuildQuery("foo", "custom", SearchQueryParserType.BestGuess, false);

            // Assert
            Assert.IsTrue(query.Cacheable);
        }

        [Test]
        public void Mention_of_user_outside_the_context_of_type_does_not_disable_caching_path() {

            // Act
            var query = Search.BuildQuery("type:document user", "custom", SearchQueryParserType.BestGuess, false);

            // Assert
            Assert.IsTrue(query.Cacheable);
        }

        [Test]
        public void Unprefixed_mention_of_user_outside_the_context_of_type_does_not_disable_caching_path() {

            // Act
            var query = Search.BuildQuery("type:document name:user", "custom", SearchQueryParserType.BestGuess, false);

            // Assert
            Assert.IsTrue(query.Cacheable);
        }

        [Test]
        public void User_type_in_query_disables_caching_path() {

            // Act
            var query = Search.BuildQuery("type:user", "custom", SearchQueryParserType.BestGuess, false);

            // Assert
            Assert.IsFalse(query.Cacheable);
        }

        [Test]
        public void User_type_in_constraint_disables_caching_path() {

            // Act
            var query = Search.BuildQuery("foo", "type:user", SearchQueryParserType.BestGuess, false);

            // Assert
            Assert.IsFalse(query.Cacheable);
        }

        [Test]
        public void User_type_as_part_of_type_list_disables_caching_path() {

            // Act
            var query = Search.BuildQuery("foo", "type:(document user)", SearchQueryParserType.BestGuess, false);

            // Assert
            Assert.IsFalse(query.Cacheable);
        }

        [Test]
        public void User_type_as_only_member_of_a_type_list_disables_caching_path() {

            // Act
            var query = Search.BuildQuery("foo", "type:(user)", SearchQueryParserType.BestGuess, false);

            // Assert
            Assert.IsFalse(query.Cacheable);
        }

        [Test]
        public void User_type_as_only_member_of_a_type_list_disables_caching_path_2() {

            // Act
            var query = Search.BuildQuery("foo", "type:( user)", SearchQueryParserType.BestGuess, false);

            // Assert
            Assert.IsFalse(query.Cacheable);
        }

        [Test]
        public void Cache_miss_returns_null() {

            // Arrange
            var q = new SearchQuery("raw", "processed", new LuceneClauseBuilder(), null);
            SearchResult result;
            _cache.Setup(x => x.TryGet(GetKey(q), out result)).Returns(false).AtMostOnce().Verifiable();

            // Act/Assert
            Assert.IsNull(Search.GetCachedQuery(q));
            _cache.VerifyAll();
        }

        [Test]
        public void Cache_hit_returns_SearchResult() {

            // Arrange
            var q = new SearchQuery("raw", "processed", new LuceneClauseBuilder(), null);
            var result = new SearchResult();
            _cache.Setup(x => x.TryGet(GetKey(q), out result)).Returns(false).AtMostOnce().Verifiable();

            // Act/Assert
            Assert.AreSame(result, Search.GetCachedQuery(q));
            _cache.VerifyAll();
        }

        [Test]
        public void CacheQuery_converts_xml_to_search_results_and_caches_the_data() {

            // Arrange
            InitWithoutAdaptiveSearch();
            var searchDoc = XDocFactory.From(@"
<documents>
    <parsedQuery>content:foo</parsedQuery>
    <document>
        <id.page>123</id.page>
        <id.file>1234</id.file>
        <title>file</title>
        <date.edited>20100525231800</date.edited>
        <score>1</score>
    </document>
    <document>
        <id.page>456</id.page>
        <title>page</title>
        <date.edited>20100429160114</date.edited>
        <rating.count>0</rating.count>
        <score>0.75</score>
    </document>
    <document>
        <id.page>36932</id.page>
        <id.comment>789</id.comment>
        <title>comment</title>
        <date.edited>20100429160323</date.edited>
        <rating.count>0</rating.count>
        <score>0.5</score>
    </document>
    <document>
        <id.page>36932</id.page>
        <id.user>432</id.user>
        <title>user</title>
        <date.edited>20100429160323</date.edited>
        <rating.count>0</rating.count>
        <score>0.5</score>
    </document>
    <document>
        <title>dropped item</title>
        <date.edited>20100429160323</date.edited>
        <rating.count>0</rating.count>
        <score>0.5</score>
    </document>
</documents>", MimeType.TEXT_XML);
            var q = new SearchQuery("raw", "processed", new LuceneClauseBuilder(), null);
            _cache.Setup(x => x.Set(GetKey(q), It.IsAny<SearchResult>(), It.IsAny<TimeSpan>())).AtMostOnce().Verifiable();

            // Act
            var result = Search.CacheQuery(searchDoc, q, null);

            // Assert
            _session.Verify(x => x.SearchAnalytics_GetPopularityRanking(It.IsAny<string>()), Times.Never());
            _cache.VerifyAll();
            Assert.AreEqual(4, result.Count);
            Assert.AreEqual("content:foo", result.ExecutedQuery);
            Assert.IsTrue(result.Where(x => x.Type == SearchResultType.File && x.TypeId == 1234).Any());
            Assert.IsTrue(result.Where(x => x.Type == SearchResultType.Page && x.TypeId == 456).Any());
            Assert.IsTrue(result.Where(x => x.Type == SearchResultType.Comment && x.TypeId == 789).Any());
            Assert.IsTrue(result.Where(x => x.Type == SearchResultType.User && x.TypeId == 432).Any());
        }

        [Test]
        public void CacheQuery_can_track_query() {

            // Arrange
            InitWithoutAdaptiveSearch();
            var searchDoc = XDocFactory.From(@"
<documents>
    <parsedQuery>content:foo</parsedQuery>
    <document>
        <id.page>123</id.page>
        <id.file>1234</id.file>
        <title>file</title>
        <date.edited>20100525231800</date.edited>
        <score>1</score>
    </document>
</documents>", MimeType.TEXT_XML);
            var trackingInfo = new TrackingInfo() { PreviousQueryId = 123 };
            var q = new SearchQuery("raw", "processed", new LuceneClauseBuilder(), null);
            _cache.Setup(x => x.Set(GetKey(q), It.IsAny<SearchResult>(), It.IsAny<TimeSpan>())).AtMostOnce().Verifiable();
            _session.Setup(x => x.SearchAnalytics_LogQuery(q, "content:foo", _user.ID, 1, 123)).Returns(456).AtMostOnce().Verifiable();

            // Act
            Search.CacheQuery(searchDoc, q, trackingInfo);

            // Assert
            _session.Verify(x => x.SearchAnalytics_GetPopularityRanking(It.IsAny<string>()), Times.Never());
            _session.VerifyAll();
            _cache.VerifyAll();
            Assert.AreEqual(456, trackingInfo.QueryId);
        }

        [Test]
        public void CacheQuery_with_adaptive_search_fetches_popularity_data() {

            // Arrange
            var q = new SearchQuery("raw", "processed", new LuceneClauseBuilder(), null);
            _cache.Setup(x => x.Set(GetKey(q), It.IsAny<SearchResult>(), It.IsAny<TimeSpan>())).AtMostOnce().Verifiable();
            _session.Setup(x => x.SearchAnalytics_GetPopularityRanking(q.GetOrderedTermsHash()))
                .Returns((IEnumerable<ResultPopularityBE>)null)
                .AtMostOnce()
                .Verifiable();

            // Act
            Search.CacheQuery(new XDoc("foo"), q, null);

            // Assert
            _session.VerifyAll();
            _cache.VerifyAll();
        }

        [Test]
        public void FormatResults_hits_cache_before_lucene_for_detail() {

            // Arrange
            var items = new[] {
                  new SearchResultItem(1,SearchResultType.Page,"page",1,DateTime.UtcNow),
                  new SearchResultItem(2,SearchResultType.File,"page",3,DateTime.UtcNow),
                  new SearchResultItem(3,SearchResultType.Comment,"page",2,DateTime.UtcNow),
                  new SearchResultItem(4,SearchResultType.User,"page",4,DateTime.UtcNow),
            };
            var details = new[] {
                new SearchResultDetail(Pairs("id", items[0].TypeId)),
                new SearchResultDetail(Pairs("id", items[1].TypeId)),
                new SearchResultDetail(Pairs("id", items[2].TypeId)),
                new SearchResultDetail(Pairs("id", items[3].TypeId)),
            };
            var set = new SearchResult("parsed", items);
            var discriminator = new SetDiscriminator() { Ascending = true, Limit = 100, Offset = 0, SortField = "rank" };
            var explain = false;
            TrackingInfo trackingInfo = null;
            _cache.Setup(x => x.TryGet(items[0].DetailKey, out details[0])).Returns(true).AtMostOnce().Verifiable();
            _cache.Setup(x => x.TryGet(items[1].DetailKey, out details[1])).Returns(true).AtMostOnce().Verifiable();
            _cache.Setup(x => x.TryGet(items[2].DetailKey, out details[2])).Returns(true).AtMostOnce().Verifiable();
            _cache.Setup(x => x.TryGet(items[3].DetailKey, out details[3])).Returns(true).AtMostOnce().Verifiable();

            // Act
            var xml = Search.FormatResultSet(set, discriminator, explain, trackingInfo, new Result<XDoc>()).Wait();

            // Assert
            _cache.VerifyAll();
            var expectedXml = XDocFactory.From(@"
<search querycount=""4"" ranking=""adaptive"" count=""4"">
  <parsedQuery>parsed</parsedQuery>
  <document>
    <score>1</score>
    <id>1</id>
  </document>
  <document>
    <score>2</score>
    <id>3</id>
  </document>
  <document>
    <score>3</score>
    <id>2</id>
  </document>
  <document>
    <score>4</score>
    <id>4</id>
  </document>
</search>", MimeType.TEXT_XML);
            Assert.AreEqual(expectedXml.ToCompactString(), xml.ToCompactString());
        }

        [Test]
        public void FormatResults_hits_lucene_for_detail_records_on_cache_miss() {

            // Arrange
            var items = new[] {
                  new SearchResultItem(1,SearchResultType.Page,"page",1,DateTime.UtcNow),
                  new SearchResultItem(2,SearchResultType.File,"file",3,DateTime.UtcNow),
                  new SearchResultItem(3,SearchResultType.Comment,"comment",2,DateTime.UtcNow),
                  new SearchResultItem(4,SearchResultType.User,"user",4,DateTime.UtcNow),
            };
            var set = new SearchResult("parsed", items);
            var discriminator = new SetDiscriminator() { Ascending = true, Limit = 100, Offset = 0, SortField = "rank" };
            var explain = false;
            TrackingInfo trackingInfo = null;
            MockPlug.Setup(_searchPlug)
                 .Verb("GET")
                 .With("wikiid", "default")
                 .With("q", v => v.Contains("page:(1"))
                 .With("max", "all")
                 .Returns(new XDoc("documents").Start("document").Elem("id.page", 1))
                 .ExpectCalls(DreamTimes.Once());
            MockPlug.Setup(_searchPlug)
                .Verb("GET")
                .With("wikiid", "default")
                .With("q", v => v.Contains("file:(2"))
                .With("max", "all")
                .Returns(new XDoc("documents").Start("document").Elem("id.file", 2))
                .ExpectCalls(DreamTimes.Once());
            MockPlug.Setup(_searchPlug)
                .Verb("GET")
                .With("wikiid", "default")
                .With("q", v => v.Contains("comment:(3"))
                .With("max", "all")
                .Returns(new XDoc("documents").Start("document").Elem("id.comment", 3))
                .ExpectCalls(DreamTimes.Once());
            MockPlug.Setup(_searchPlug)
                .Verb("GET")
                .With("wikiid", "default")
                .With("q", v => v.Contains("user:(4"))
                .With("max", "all")
                .Returns(new XDoc("documents").Start("document").Elem("id.user", 4))
                .ExpectCalls(DreamTimes.Once());
            SearchResultDetail detail = null;
            _cache.Setup(x => x.TryGet(It.IsAny<string>(), out detail)).Returns(false).AtMost(4).Verifiable();
            _cache.Setup(x => x.Set(items[0].DetailKey, It.Is<SearchResultDetail>(d => d["id.page"] == "1"), It.IsAny<TimeSpan>()))
                .AtMostOnce().Verifiable();
            _cache.Setup(x => x.Set(items[1].DetailKey, It.Is<SearchResultDetail>(d => d["id.file"] == "2"), It.IsAny<TimeSpan>()))
                .AtMostOnce().Verifiable();
            _cache.Setup(x => x.Set(items[2].DetailKey, It.Is<SearchResultDetail>(d => d["id.comment"] == "3"), It.IsAny<TimeSpan>()))
                .AtMostOnce().Verifiable();
            _cache.Setup(x => x.Set(items[3].DetailKey, It.Is<SearchResultDetail>(d => d["id.user"] == "4"), It.IsAny<TimeSpan>()))
                .AtMostOnce().Verifiable();

            // Act
            var xml = Search.FormatResultSet(set, discriminator, explain, trackingInfo, new Result<XDoc>()).Wait();

            // Assert
            _cache.VerifyAll();
            MockPlug.VerifyAll();
            var expectedXml = XDocFactory.From(@"
<search querycount=""4"" ranking=""adaptive"" count=""4"">
  <parsedQuery>parsed</parsedQuery>
  <document>
    <score>1</score>
    <id.page>1</id.page>
  </document>
  <document>
    <score>2</score>
    <id.comment>3</id.comment>
  </document>
  <document>
    <score>3</score>
    <id.file>2</id.file>
  </document>
  <document>
    <score>4</score>
    <id.user>4</id.user>
  </document>
</search>", MimeType.TEXT_XML);
            Assert.AreEqual(expectedXml.ToCompactString(), xml.ToCompactString());
        }

        [Test]
        public void FormatResultSet_generates_special_output_for_tracked_searches() {

            // Arrange
            var item = new SearchResultItem(1, SearchResultType.Page, "page", 1,  DateTime.Parse("2010/10/10").ToSafeUniversalTime());
            var detail = new SearchResultDetail(Pairs(
                "id.page", 1,
                "path","path",
                "uri","http://uri",
                "title", "foo",
                "author", "bob",
                "preview", "preview"
            ));
            var set = new SearchResult("parsed", new[] { item });
            var discriminator = new SetDiscriminator() { Ascending = true, Limit = 100, Offset = 0, SortField = "rank" };
            var explain = false;
            TrackingInfo trackingInfo = new TrackingInfo() { QueryId = 123, PreviousQueryId = 456 };
            _cache.Setup(x => x.TryGet(item.DetailKey, out detail)).Returns(true).AtMostOnce().Verifiable();

            // Act
            var xml = Search.FormatResultSet(set, discriminator, explain, trackingInfo, new Result<XDoc>()).Wait();

            // Assert
            _cache.VerifyAll();
            var expectedXml = XDocFactory.From(@"
<search querycount=""1"" ranking=""adaptive"" queryid=""123"" count=""1"">
  <parsedQuery>parsed</parsedQuery>
  <result>
    <id>1</id>
    <uri>http://uri</uri>
    <uri.track>mock://api/site/query/123?pageid=1&amp;rank=1&amp;type=page&amp;position=1</uri.track>
    <rank>1</rank>
    <title>foo</title>
    <page>
      <rating />
      <title>foo</title>
      <path>path</path>
    </page>
    <author>bob</author>
    <date.modified>2010-10-10T00:00:00Z</date.modified>
    <content>preview</content>
    <type>page</type>
  </result>
</search>", MimeType.TEXT_XML);
            Assert.AreEqual(expectedXml.ToCompactString(), xml.ToCompactString());
        }

        [Test]
        public void File_result_pick_tracks_file_id_as_type_id() {

            // Arrange
            _session.Setup(s => s.SearchAnalytics_LogQueryPick(123, 42, 10, 20, SearchResultType.File, 456)).AtMostOnce().Verifiable();
            _session.Setup(s => s.SearchAnalytics_UpdateQueryPopularityAggregate(123)).AtMostOnce().Verifiable();

            // Act
            Search.TrackQueryResultPick(123, 42, 10, 20, SearchResultType.File, 456);

            // Assert
            _session.VerifyAll();
        }


        [Test]
        public void Page_result_pick_tracks_page_id_as_type_id() {

            // Arrange
            _session.Setup(s => s.SearchAnalytics_LogQueryPick(123, 42, 10, 20, SearchResultType.Page, 20)).AtMostOnce().Verifiable();
            _session.Setup(s => s.SearchAnalytics_UpdateQueryPopularityAggregate(123)).AtMostOnce().Verifiable();

            // Act
            Search.TrackQueryResultPick(123, 42, 10, 20, SearchResultType.Page, null);

            // Assert
            _session.VerifyAll();
        }

        [Test]
        public void Can_get_single_query_detail() {

            // Arrange
            var created = DateTime.UtcNow.AddDays(-2).WithoutMilliseconds();
            _session.Setup(x => x.SearchAnalytics_GetTrackedQuery(123))
                .Returns(new LoggedSearchBE() {
                    Id = 123,
                    Created = created,
                    ParsedQuery = "foo bar",
                    RawQuery = "FOO BAR",
                    ResultCount = 2,
                    SortedTerms = "bar foo",
                    SelectedResults = new List<LoggedSearchResultBE> { PageResultBE(7, 11, created), },
                    Terms = new List<string> { "foo", "bar" },
                    PreviousId = 456,
                })
                .AtMostOnce()
                .Verifiable();
            _session.Setup(x => x.SearchAnalytics_GetTrackedQuery(456))
                .Returns(new LoggedSearchBE() {
                    Id = 456,
                    SortedTerms = "blah blah",
                    SelectedResults = new List<LoggedSearchResultBE>(),
                    Terms = new List<string>(),
                })
                .AtMostOnce()
                .Verifiable();

            // Act
            var xml = Search.GetQueryXml(123);

            // Assert
            _session.VerifyAll();
            Assert.AreEqual(123, xml["@id"].AsInt);
            Assert.AreEqual(created, xml["date.searched"].AsDate);
            Assert.AreEqual("foo bar", xml["processed"].AsText);
            Assert.AreEqual("FOO BAR", xml["raw"].AsText);
            Assert.AreEqual("bar foo", xml["sorted-terms"].AsText);
            Assert.AreEqual(2, xml["count.results"].AsInt);
            Assert.AreEqual(new[] { "bar", "foo" }, (from term in xml["terms/term"] let t = term.AsText orderby t select t).ToArray());
            var r = xml["selected-results/result"];
            Assert.AreEqual(1, r.ListLength);
            Assert.AreEqual(7, r["page/@id"].AsInt);
            Assert.AreEqual(11, r["position"].AsInt);
            Assert.AreEqual(created, r["date.selected"].AsDate);
            var previous = xml["previous"];
            Assert.AreEqual(456, previous["@id"].AsInt);
            Assert.AreEqual("blah blah", previous["sorted-terms"].AsText);
        }

        [Test]
        public void Can_get_full_query_log() {

            // Arrange
            var before = DateTime.UtcNow.AddDays(-2);
            var since = DateTime.UtcNow.AddDays(-1);
            var d1 = DateTime.UtcNow.WithoutMilliseconds();
            var d2 = d1.AddDays(-1);
            var d3 = d1.AddDays(-2);
            var d4 = d1.AddDays(-3);
            var d5 = d1.AddDays(-4);
            var d6 = d1.AddDays(-5);
            _session.Setup(x => x.SearchAnalytics_GetTrackedQueries(null, SearchAnalyticsQueryType.All, since, before, 0, 0))
               .Returns(new[] {
                    new LoggedSearchBE {Id = 1, RawQuery = "a", Created = d1, SortedTerms = "Foo bar",
                        Terms = new List<string>(),
                        SelectedResults = new List<LoggedSearchResultBE>() {
                            PageResultBE(3,1,d2),
                            FileResultBE(2,2,1,d3),
                            PageResultBE(1,1,d4),
                    }},           
                    new LoggedSearchBE {Id = 2, RawQuery = "b", Created = d5, SortedTerms = "Foo bar baz",
                        Terms = new List<string> {"foo","bar"},
                        SelectedResults = new List<LoggedSearchResultBE>() {
                            PageResultBE(1,1,d6),
                    }},           
                })
                .AtMostOnce()
                .Verifiable();

            // Act
            var xml = Search.GetQueriesXml(null, SearchAnalyticsQueryType.All, false, since, before, 0, 0);

            // Assert
            _session.VerifyAll();
            Assert.AreEqual(2, xml["@count"].AsInt);
            var q1 = xml["query[@id='1']"];
            Assert.AreEqual("Foo bar", q1["sorted-terms"].AsText);
            Assert.AreEqual("a", q1["raw"].AsText);
            Assert.AreEqual(d1, q1["date.searched"].AsDate);
            var results = q1["selected-results/result"];
            Assert.AreEqual(
                new[] { "page", "file", "page" },
                (from r in results orderby r["date.selected"].AsDate descending select r["@type"].AsText).ToArray());
            Assert.AreEqual(
                new[] { 3, 2, 1 },
                (from r in results orderby r["date.selected"].AsDate descending select r["page/@id"].AsInt ?? 0).ToArray());
            var q2 = xml["query[@id='2']"];
            Assert.AreEqual("Foo bar baz", q2["sorted-terms"].AsText);
            Assert.AreEqual("b", q2["raw"].AsText);
            Assert.AreEqual(d5, q2["date.searched"].AsDate);
            Assert.AreEqual(new[] { "bar", "foo" }, (from term in q2["terms/term"] let t = term.AsText orderby t select t).ToArray());
        }

        [Test]
        public void Can_get_query_log_by_querystring() {

            // Arrange
            var before = DateTime.UtcNow.AddDays(-2);
            var since = DateTime.UtcNow.AddDays(-1);
            _session.Setup(x => x.SearchAnalytics_GetTrackedQueries("bar foo", SearchAnalyticsQueryType.QueryString, since, before, 0, 0))
               .Returns(new[] {
                    new LoggedSearchBE {SortedTerms = "1",SelectedResults = new List<LoggedSearchResultBE>(), Terms = new List<string>()},           
                    new LoggedSearchBE {SortedTerms = "2",SelectedResults = new List<LoggedSearchResultBE>(), Terms = new List<string>()},           
                    new LoggedSearchBE {SortedTerms = "1",SelectedResults = new List<LoggedSearchResultBE>(), Terms = new List<string>()},           
                })
                .AtMostOnce()
                .Verifiable();

            // Act
            var xml = Search.GetQueriesXml("foo bar", SearchAnalyticsQueryType.QueryString, false, since, before, 0, 0);

            // Assert
            _session.VerifyAll();
            Assert.AreEqual(new[] { 1, 2, 1 }, xml["query/sorted-terms"].Select(x => x.AsInt).ToArray());
        }

        [Test]
        public void Can_get_query_log_by_term() {

            // Arrange
            var before = DateTime.UtcNow.AddDays(-2);
            var since = DateTime.UtcNow.AddDays(-1);
            _session.Setup(x => x.SearchAnalytics_GetTrackedQueries("foo bar", SearchAnalyticsQueryType.Term, since, before, 0, 0))
               .Returns(new[] {
                    new LoggedSearchBE {SortedTerms = "1",SelectedResults = new List<LoggedSearchResultBE>(), Terms = new List<string>()},           
                    new LoggedSearchBE {SortedTerms = "2",SelectedResults = new List<LoggedSearchResultBE>(), Terms = new List<string>()},           
                    new LoggedSearchBE {SortedTerms = "1",SelectedResults = new List<LoggedSearchResultBE>(), Terms = new List<string>()},           
                })
                .AtMostOnce()
                .Verifiable();

            // Act
            var xml = Search.GetQueriesXml("foo bar", SearchAnalyticsQueryType.Term, false, since, before, 0, 0);

            // Assert
            _session.VerifyAll();
            Assert.AreEqual(new[] { 1, 2, 1 }, xml["query/sorted-terms"].Select(x => x.AsInt).ToArray());
        }

        [Test]
        public void Query_log_passes_offset_and_limit_to_DA() {

            // Arrange
            var before = DateTime.UtcNow.AddDays(-2);
            var since = DateTime.UtcNow.AddDays(-1);
            _session.Setup(x => x.SearchAnalytics_GetTrackedQueries(null, SearchAnalyticsQueryType.All, since, before, 100, 10))
               .Returns(new[] {
                    new LoggedSearchBE() { SortedTerms = "1", SelectedResults = new List<LoggedSearchResultBE>(), Terms = new List<string>() }
                })
                .AtMostOnce()
                .Verifiable();

            // Act
            Search.GetQueriesXml(null, SearchAnalyticsQueryType.All, false, since, before, 100, 10);

            // Assert
            _session.VerifyAll();
        }

        [Test]
        public void Aggregated_queries_applies_limit_and_after_aggregation() {

            // Arrange
            var before = DateTime.UtcNow.AddDays(-2);
            var since = DateTime.UtcNow.AddDays(-1);
            _session.Setup(x => x.SearchAnalytics_GetTrackedQueries("x", SearchAnalyticsQueryType.QueryString, since, before, null, null))
                .Returns(new[] {
                    new LoggedSearchBE {SortedTerms = "1",SelectedResults = new List<LoggedSearchResultBE>()},           
                    new LoggedSearchBE {SortedTerms = "2",SelectedResults = new List<LoggedSearchResultBE>()},           
                    new LoggedSearchBE {SortedTerms = "3",SelectedResults = new List<LoggedSearchResultBE>()},           
                    new LoggedSearchBE {SortedTerms = "4",SelectedResults = new List<LoggedSearchResultBE>()},           
                    new LoggedSearchBE {SortedTerms = "5",SelectedResults = new List<LoggedSearchResultBE>()},           
                    new LoggedSearchBE {SortedTerms = "6",SelectedResults = new List<LoggedSearchResultBE>()},           
                    new LoggedSearchBE {SortedTerms = "7",SelectedResults = new List<LoggedSearchResultBE>()},           
                    new LoggedSearchBE {SortedTerms = "8",SelectedResults = new List<LoggedSearchResultBE>()},           
                })
                .AtMostOnce()
                .Verifiable();

            // Act
            var aggXml = Search.GetAggregatedQueriesXml("x", SearchAnalyticsQueryType.QueryString, false, since, before, 5, 2);

            // Assert
            Assert.AreEqual(new[] { 3, 4, 5, 6, 7 }, aggXml["query/sorted-terms"].Select(x => x.AsInt).ToArray());
            _session.VerifyAll();
        }

        [Test]
        public void Aggregated_queries_xml_aggregates_results_as_well() {

            // Arrange
            var before = DateTime.UtcNow.AddDays(-2);
            var since = DateTime.UtcNow.AddDays(-1);
            var last = DateTime.UtcNow.WithoutMilliseconds();
            var older = last.AddDays(-1);
            _session.Setup(x => x.SearchAnalytics_GetTrackedQueries("bar foo", SearchAnalyticsQueryType.QueryString, since, before, null, null))
                .Returns(new[] {
                    new LoggedSearchBE {RawQuery = "a", Created = older, SortedTerms = "Foo bar",
                        SelectedResults = new List<LoggedSearchResultBE> {
                            PageResultBE(1,1,older),
                            FileResultBE(1,2,1,last),
                            PageResultBE(2,1,older),
                    }},           
                    new LoggedSearchBE {RawQuery = "b", Created = last, SortedTerms = "Foo bar",
                        SelectedResults = new List<LoggedSearchResultBE> {
                            PageResultBE(1,1,older),
                    }},           
                    new LoggedSearchBE {RawQuery = "c", Created = older, SortedTerms = "Foo bar",
                        SelectedResults = new List<LoggedSearchResultBE> {
                            FileResultBE(1,2,1,older),
                    }},           
                    new LoggedSearchBE {RawQuery = "x", Created = older, SortedTerms = "foo bar",
                        SelectedResults = new List<LoggedSearchResultBE> {
                            new LoggedSearchResultBE(),                                                                                             
                    }},           
                })
                .AtMostOnce()
                .Verifiable();

            // Act
            var aggXml = Search.GetAggregatedQueriesXml("foo bar", SearchAnalyticsQueryType.QueryString, false, since, before, 100, 0);

            // Assert
            _session.VerifyAll();
            Assert.AreEqual(2, aggXml["@count"].AsInt);
            var query = aggXml["query[sorted-terms='Foo bar']"];
            Assert.AreEqual(3, query["@count"].AsInt);
            Assert.AreEqual(last, query["date.searched"].AsDate);
            Assert.AreEqual(3, query["selected-results/@totalcount"].AsInt);
            var page = query["selected-results/result[@type='page']"];
            Assert.AreEqual(1, page["@count"].AsInt);
            Assert.AreEqual("page", page["@type"].AsText);
            Assert.AreEqual(older, page["date.selected"].AsDate);
            var file = query["selected-results/result[@type='file']"];
            Assert.AreEqual(2, file["@count"].AsInt);
            Assert.AreEqual("file", file["@type"].AsText);
            Assert.AreEqual(last, file["date.selected"].AsDate);
            Assert.AreEqual(1, file["page/@id"].AsInt);
            Assert.AreEqual(2, file["file/@id"].AsInt);
        }

        [Test]
        public void Aggregated_queries_computes_position_stats() {

            // Arrange
            var before = DateTime.UtcNow.AddDays(-2);
            var since = DateTime.UtcNow.AddDays(-1);
            _session.Setup(x => x.SearchAnalytics_GetTrackedQueries("foo", SearchAnalyticsQueryType.QueryString, since, before, null, null))
                .Returns(new[] {
                    new LoggedSearchBE {RawQuery = "a",SelectedResults = new List<LoggedSearchResultBE> {PageResultBE(1,10,since),}},           
                    new LoggedSearchBE {RawQuery = "a",SelectedResults = new List<LoggedSearchResultBE> {PageResultBE(1,30,since),}},           
                    new LoggedSearchBE {RawQuery = "a",SelectedResults = new List<LoggedSearchResultBE> {PageResultBE(1,40,since),}},           
                    new LoggedSearchBE {RawQuery = "a",SelectedResults = new List<LoggedSearchResultBE> {PageResultBE(1,20,since),}},           
                    new LoggedSearchBE {RawQuery = "a",SelectedResults = new List<LoggedSearchResultBE> {PageResultBE(1,50,since),}},           
                })
                .AtMostOnce()
                .Verifiable();

            // Act
            var aggXml = Search.GetAggregatedQueriesXml("foo", SearchAnalyticsQueryType.QueryString, false, since, before, 100, 0);

            // Assert
            _session.VerifyAll();
            var result = aggXml["query/selected-results/result/position"];
            Assert.AreEqual(1, result.ListLength);
            Assert.AreEqual(10, result["@min"].AsInt);
            Assert.AreEqual(50, result["@max"].AsInt);
            Assert.AreEqual(30, result["@avg"].AsInt);
        }

        [Test]
        public void Can_aggregate_terms_query() {

            // Arrange
            var before = DateTime.UtcNow.AddDays(-2);
            var since = DateTime.UtcNow.AddDays(-1);
            _session.Setup(x => x.SearchAnalytics_GetTrackedQueries("x", SearchAnalyticsQueryType.Term, since, before, null, null))
                .Returns(new[] {
                    new LoggedSearchBE() {SortedTerms = "1",SelectedResults = new List<LoggedSearchResultBE>()},           
                    new LoggedSearchBE() {SortedTerms = "2",SelectedResults = new List<LoggedSearchResultBE>()},           
                    new LoggedSearchBE() {SortedTerms = "1",SelectedResults = new List<LoggedSearchResultBE>()},           
                })
                .AtMostOnce()
                .Verifiable();

            // Act
            var aggXml = Search.GetAggregatedQueriesXml("x", SearchAnalyticsQueryType.Term, false, since, before, 100, 0);

            // Assert
            Assert.AreEqual(new[] { 1, 2 }, aggXml["query/sorted-terms"].Select(x => x.AsInt).ToArray());
            _session.VerifyAll();
        }

        [Test]
        public void Can_aggregate_query_results_with_all_query_type() {

            // Arrange
            var before = DateTime.UtcNow.AddDays(-2);
            var since = DateTime.UtcNow.AddDays(-1);
            _session.Setup(x => x.SearchAnalytics_GetTrackedQueries(null, SearchAnalyticsQueryType.All, since, before, null, null))
                .Returns(new[] {
                    new LoggedSearchBE() {SortedTerms = "1",SelectedResults = new List<LoggedSearchResultBE>()},           
                    new LoggedSearchBE() {SortedTerms = "2",SelectedResults = new List<LoggedSearchResultBE>()},           
                    new LoggedSearchBE() {SortedTerms = "1",SelectedResults = new List<LoggedSearchResultBE>()},           
                })
                .AtMostOnce()
                .Verifiable();

            // Act
            var aggXml = Search.GetAggregatedQueriesXml(null, SearchAnalyticsQueryType.All, false, since, before, 100, 0);

            // Assert
            Assert.AreEqual(new[] { 1, 2 }, aggXml["query/sorted-terms"].Select(x => x.AsInt).ToArray());
            _session.VerifyAll();
        }

        [Test]
        public void Can_get_aggregated_query_xml() {

            // Arrange
            var before = DateTime.UtcNow.AddDays(-2);
            var since = DateTime.UtcNow.AddDays(-1);
            var last = DateTime.UtcNow.WithoutMilliseconds();
            var older = last.AddDays(-1);
            _session.Setup(x => x.SearchAnalytics_GetTrackedQueries("foo", SearchAnalyticsQueryType.QueryString, since, before, null, null))
                .Returns(new[] {
                    new LoggedSearchBE() {RawQuery = "a", SelectedResults = new List<LoggedSearchResultBE>(), Created = older},           
                    new LoggedSearchBE() {RawQuery = "a", SelectedResults = new List<LoggedSearchResultBE>(), Created = older},           
                    new LoggedSearchBE() {RawQuery = "a", SelectedResults = new List<LoggedSearchResultBE>(), Created = older},           
                    new LoggedSearchBE() {RawQuery = "b", SelectedResults = new List<LoggedSearchResultBE>(), Created = last},           
                    new LoggedSearchBE() {RawQuery = "b", SelectedResults = new List<LoggedSearchResultBE>(), Created = older},           
                    new LoggedSearchBE() {RawQuery = "b", SelectedResults = new List<LoggedSearchResultBE>(), Created = older},           
                    new LoggedSearchBE() {RawQuery = "b", SelectedResults = new List<LoggedSearchResultBE>(), Created = older},           
                    new LoggedSearchBE() {RawQuery = "b", SelectedResults = new List<LoggedSearchResultBE>(), Created = older},           
                })
                .AtMostOnce()
                .Verifiable();

            // Act
            var aggXml = Search.GetAggregateQueryXml("foo", since, before);

            // Assert
            _session.VerifyAll();
            Assert.AreEqual(last, aggXml["date.searched"].AsDate);
            Assert.AreEqual("foo", aggXml["sorted-terms"].AsText);
            Assert.AreEqual(8, aggXml["queries/@totalcount"].AsInt);
            var queries = aggXml["queries/raw"];
            Assert.AreEqual(2, queries.ListLength);
            Assert.AreEqual(3, (from q in queries where q.Contents == "a" select q["@count"].AsInt).FirstOrDefault());
            Assert.AreEqual(5, (from q in queries where q.Contents == "b" select q["@count"].AsInt).FirstOrDefault());
        }

        [Test]
        public void Can_get_terms_xml() {

            // Arrange
            var before = DateTime.UtcNow.AddDays(-2);
            var since = DateTime.UtcNow.AddDays(-1);
            uint limit = 100;
            uint offset = 0;
            _session.Setup(x => x.SearchAnalytics_GetTerms(false, since, before, limit, offset))
                .Returns(new[] {
                    new TermAggregateBE() { Count = 1, Term = "foo"},
                    new TermAggregateBE() { Count = 5, Term = "bar"}
                })
                .AtMostOnce()
                .Verifiable();

            // Act
            var termsXml = Search.GetTermsXml(false, since, before, limit, offset);

            // Assert
            _session.VerifyAll();
            Assert.AreEqual(2, termsXml["*"].ListLength);
            Assert.AreEqual(2, termsXml["@count"].AsInt ?? 0);
            Assert.AreEqual(1, termsXml["term"].Where(x => x.Contents == "foo").First()["@count"].AsInt ?? 0);
            Assert.AreEqual(5, termsXml["term"].Where(x => x.Contents == "bar").First()["@count"].AsInt ?? 0);
        }

        [Test]
        [ExpectedException(typeof(MindTouchLicenseInvalidOperationForbiddenException))]
        public void GetQueriesXml_requires_adaptive_search() {

            // Arrange
            InitWithoutAdaptiveSearch();

            // Act
            Search.GetQueriesXml("foo", SearchAnalyticsQueryType.All, false, DateTime.MinValue, DateTime.MinValue, 0, 0);
        }

        [Test]
        [ExpectedException(typeof(MindTouchLicenseInvalidOperationForbiddenException))]
        public void GetAggregatedQueriesXml_requires_adaptive_search() {

            // Arrange
            InitWithoutAdaptiveSearch();

            // Act
            Search.GetAggregatedQueriesXml("foo", SearchAnalyticsQueryType.All, false, DateTime.MinValue, DateTime.MinValue, 0, 0);
        }

        [Test]
        [ExpectedException(typeof(MindTouchLicenseInvalidOperationForbiddenException))]
        public void GetAggregateQueryXml_requires_adaptive_search() {

            // Arrange
            InitWithoutAdaptiveSearch();

            // Act
            Search.GetAggregateQueryXml("foo", DateTime.MinValue, DateTime.MinValue);
        }

        [Test]
        [ExpectedException(typeof(MindTouchLicenseInvalidOperationForbiddenException))]
        public void GetQueryXml_requires_adaptive_search() {

            // Arrange
            InitWithoutAdaptiveSearch();

            // Act
            Search.GetQueryXml(42);
        }

        [Test]
        [ExpectedException(typeof(MindTouchLicenseInvalidOperationForbiddenException))]
        public void GetTermsXml_requires_adaptive_search() {

            // Arrange
            InitWithoutAdaptiveSearch();

            // Act
            Search.GetTermsXml(false, DateTime.MinValue, DateTime.MinValue, 0, 0);
        }

        private string GetKey(SearchQuery query) {
            return "query:" + _user.ID + ":" + query.LuceneQuery;
        }

        private IEnumerable<KeyValuePair<string, string>> Pairs(params object[] values) {
            for(var i = 0; i < values.Length; i += 2) {
                yield return new KeyValuePair<string, string>(values[i].ToString(), values[i + 1].ToString());
            }
        }

        private LoggedSearchResultBE PageResultBE(uint pageId, ushort position, DateTime created) {
            return new LoggedSearchResultBE() {
                Created = created,
                Type = SearchResultType.Page,
                TypeId = pageId,
                PageId = pageId,
                Position = position,
            };
        }

        private LoggedSearchResultBE FileResultBE(uint pageId, uint typeId, ushort position, DateTime created) {
            return new LoggedSearchResultBE() {
                Created = created,
                Type = SearchResultType.File,
                TypeId = typeId,
                PageId = pageId,
                Position = position,
            };
        }

        private SearchBL Search {
            get {
                if(_searchBL == null) {
                    _searchBL = new SearchBL(
                        _session.Object,
                        _cache.Object,
                        _wikiId,
                        _apiUri,
                        _searchPlug,
                        _user,
                        _settings.Object,
                        new SearchQueryParser(),
                        () => true,
                        LogUtils.CreateLog<SearchBL>()
                    );
                }
                return _searchBL;
            }
        }

        private void InitWithoutAdaptiveSearch() {
            _searchBL = new SearchBL(
                 _session.Object,
                 _cache.Object,
                 _wikiId,
                 _apiUri,
                 _searchPlug,
                 _user,
                 _settings.Object,
                 new SearchQueryParser(),
                 () => false,
                 LogUtils.CreateLog<SearchBL>());
        }
    }
}

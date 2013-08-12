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
using MindTouch.Deki.Data;
using MindTouch.Deki.Search;
using NUnit.Framework;

namespace MindTouch.Deki.Tests.SearchTests {

    [TestFixture]
    public class SearchResultRankCalculatorTests {

        private int _ratingPromotion;
        private int _ratingDemotion;
        private int _ratingCount;
        private double _ratingMidpoint;
        private int _popularityBoost;
        private int _popularityThreshold;
        private uint _id;

        [SetUp]
        public void Setup() {
            _ratingPromotion = 10;
            _ratingDemotion = 10;
            _ratingCount = 100;
            _ratingMidpoint = 0.5;
            _popularityBoost = 10;
            _popularityThreshold = 10;
            _id = 0;
        }

        [Test]
        public void Rating_of_1_stays_1() {
            var item = NewItemFromSet(0, 1, 0);
            Assert.AreEqual(1, item.Rating);
        }

        [Test]
        public void Rating_of_0_becomes_minus_1() {
            var item = NewItemFromSet(0, 0, 0);
            Assert.AreEqual(-1, item.Rating);
        }

        [Test]
        public void Rating_of_null_becomes_0() {
            var item = NewItemFromSet(0, null, 0);
            Assert.AreEqual(0, item.Rating);
        }

        [Test]
        public void Can_skew_rating_midpoint_to_point_25() {
            Assert.AreEqual(-1, NewItemFromSet(0, 0, 0, 0.25).Rating, "incorrect rating for 0");
            Assert.AreEqual(0, NewItemFromSet(0, 0.25, 0, 0.25).Rating, "incorrect rating for 0.25");
            Assert.AreEqual(0.25 / 0.75, NewItemFromSet(0, 0.5, 0, 0.25).Rating, "incorrect rating for 0.5");
            Assert.AreEqual(1, NewItemFromSet(0, 1, 0, 0.25).Rating, "incorrect rating for 1");
        }

        [Test]
        public void Can_skew_rating_midpoint_to_point_05() {
            Assert.AreEqual(-1, NewItemFromSet(0, 0, 0, 0.05).Rating, "incorrect rating for 0");
            Assert.AreEqual(-0.04 / 0.05, NewItemFromSet(0, 0.01, 0, 0.05).Rating, "incorrect rating for 0.01");
            Assert.AreEqual(0.45 / 0.95, NewItemFromSet(0, 0.5, 0, 0.05).Rating, "incorrect rating for 0.5");
            Assert.AreEqual(1, NewItemFromSet(0, 1, 0, 0.05).Rating, "incorrect rating for 1");
        }

        [Test]
        public void Can_skew_rating_midpoint_to_point_pos_1() {
            Assert.AreEqual(-1, NewItemFromSet(0, 0, 0, 1).Rating, "incorrect rating for 0");
            Assert.AreEqual(-0.75 / 1, NewItemFromSet(0, 0.25, 0, 1).Rating, "incorrect rating for 0.25");
            Assert.AreEqual(-0.5 / 1, NewItemFromSet(0, 0.5, 0, 1).Rating, "incorrect rating for 0.5");
            Assert.AreEqual(0, NewItemFromSet(0, 1, 0, 1).Rating, "incorrect rating for 1");
        }

        [Test]
        public void Can_skew_rating_midpoint_to_point_neg_1() {
            Assert.AreEqual(0, NewItemFromSet(0, 0, 0, 0).Rating, "incorrect rating for 0");
            Assert.AreEqual(0.25, NewItemFromSet(0, 0.25, 0, 0).Rating, "incorrect rating for 0.25");
            Assert.AreEqual(0.5, NewItemFromSet(0, 0.5, 0, 0).Rating, "incorrect rating for 0.5");
            Assert.AreEqual(1, NewItemFromSet(0, 1, 0, 0.25).Rating, "incorrect rating for 1");
        }

        [Test]
        public void No_computation_means_lucene_equals_rank() {
            var r = CreateRankCalculator();
            var item1 = AddNewItem(r, 1);
            var item2 = AddNewItem(r, 0.5);
            var item3 = AddNewItem(r, 0.2);
            Assert.AreEqual(item1.LuceneScore, item1.Rank);
            Assert.AreEqual(item2.LuceneScore, item2.Rank);
            Assert.AreEqual(item3.LuceneScore, item3.Rank);
        }

        [Test]
        public void Without_other_sources_uses_lucene_for_order_evenly_spaced() {
            var r = CreateRankCalculator();
            var item1 = AddNewItem(r, 1);
            var item2 = AddNewItem(r, 0.2);
            var item3 = AddNewItem(r, 0.5);
            r.ComputeRank(null);
            Assert.AreEqual(1, item1.Rank);
            Assert.AreEqual(0.5, item3.Rank);
            Assert.AreEqual(0, item2.Rank);
        }

        [Test]
        public void Highest_rank_is_always_1() {
            var r = CreateRankCalculator();
            var item1 = AddNewItem(r, 5);
            var item2 = AddNewItem(r, 2);
            var item3 = AddNewItem(r, 1);
            r.ComputeRank(null);
            Assert.AreEqual(1, item1.Rank);
        }

        [Test]
        public void Lowest_rank_is_always_0() {
            var r = CreateRankCalculator();
            var item1 = AddNewItem(r, 5);
            var item2 = AddNewItem(r, 2);
            var item3 = AddNewItem(r, 1);
            r.ComputeRank(null);
            Assert.AreEqual(0, item3.Rank);
        }

        [Test]
        public void Rating_count_influences_rating_boost() {
            var r = CreateRankCalculator();
            var item1 = AddNewItem(r, 1, 1, 10);
            var item2 = AddNewItem(r, 1, 1, 100);
            var item3 = AddNewItem(r, 1, 1, 50);
            r.ComputeRank(null);
            Assert.AreEqual(
                new[] { item2.RatingBoost, item3.RatingBoost, item1.RatingBoost },
                (from x in r let y = (RankableSearchResultItem)x orderby y.RatingBoost descending select y.RatingBoost).ToArray()
            );
        }

        [Test]
        public void Rating_affects_rank() {
            var r = CreateRankCalculator();
            var item1 = AddNewItem(r, 3, 0.5, _ratingCount);
            var item2 = AddNewItem(r, 2, 0.7, _ratingCount);
            var item3 = AddNewItem(r, 1, 0.2, _ratingCount);
            r.ComputeRank(null);
            Assert.AreEqual(new[] { item2.Rank, item1.Rank, item3.Rank }, r.OrderByDescending(x => x.Rank).Select(x => x.Rank).ToArray());
        }

        [Test]
        public void Rating_below_midpoint_causes_demotion() {
            var r = CreateRankCalculator();
            var item1 = AddNewItem(r, 0, 0.2, 10);
            r.ComputeRank(null);
            Assert.Less(item1.RatingBoost, 0);
        }

        [Test]
        public void RatingBoost_is_capped() {
            var r = CreateRankCalculator();
            var item1 = AddNewItem(r, 0, 1, _ratingCount);
            var item2 = AddNewItem(r, 0, 1, _ratingCount * 2);
            r.ComputeRank(null);
            Assert.AreEqual((double)_ratingPromotion, item2.RatingBoost);
            Assert.AreEqual(item1.RatingBoost, item2.RatingBoost);
        }

        [Test]
        public void RatingBoost_demotion_is_capped() {
            var r = CreateRankCalculator();
            var item1 = AddNewItem(r, 0, 0, _ratingCount);
            var item2 = AddNewItem(r, 0, 0, _ratingCount * 2);
            r.ComputeRank(null);
            Assert.AreEqual((double)-1*_ratingDemotion, item2.RatingBoost);
            Assert.AreEqual(item1.RatingBoost, item2.RatingBoost);
        }

        [Test]
        public void Results_are_matched_to_their_popularity_rank() {
            var r = CreateRankCalculator();
            var item1 = AddNewItem(r, 0);
            var item2 = AddNewItem(r, 0);
            var pop = new List<ResultPopularityBE> {
                NewPopItem(item1.TypeId, 2), 
                NewPopItem(item2.TypeId, 1)
            };
            r.ComputeRank(pop);
            Assert.AreEqual(CalcPopularityBoost(2), item1.SearchPopularityBoost);
            Assert.AreEqual(CalcPopularityBoost(1), item2.SearchPopularityBoost);
        }

        [Test]
        public void PopularityBoost_is_capped() {
            var r = CreateRankCalculator();
            var item1 = AddNewItem(r, 0);
            var item2 = AddNewItem(r, 0);
            var pop = new List<ResultPopularityBE> {
                NewPopItem(item1.TypeId, _popularityThreshold), 
                NewPopItem(item2.TypeId, _popularityThreshold*2)
            };
            r.ComputeRank(pop);
            Assert.AreEqual((double)_popularityBoost, item2.SearchPopularityBoost);
            Assert.AreEqual(item1.SearchPopularityBoost, item2.SearchPopularityBoost);
        }

        [Test]
        public void Popularity_affects_rank() {
            var r = CreateRankCalculator();
            var item1 = AddNewItem(r, 3);
            var item2 = AddNewItem(r, 2);
            var item3 = AddNewItem(r, 1);
            var pop = new List<ResultPopularityBE> {
                NewPopItem(item1.TypeId, 7), 
                NewPopItem(item2.TypeId, 10),
                NewPopItem(item3.TypeId, 2)
            };
            r.ComputeRank(pop);
            Assert.AreEqual(new[] { item2.Rank, item1.Rank, item3.Rank }, r.OrderByDescending(x => x.Rank).Select(x => x.Rank).ToArray());
        }

        private ResultPopularityBE NewPopItem(uint id, int count) {
            return new ResultPopularityBE() {
                Type = SearchResultType.Page,
                TypeId = id,
                Count = (uint)count
            };
        }

        private SearchResultRankCalculator CreateRankCalculator() {
            return new SearchResultRankCalculator(
                _ratingPromotion,
                _ratingDemotion,
                _ratingCount,
                _ratingMidpoint,
                _popularityBoost,
                _popularityThreshold);
        }

        private RankableSearchResultItem AddNewItem(SearchResultRankCalculator rankCalculator, double lucene, double? rating, int ratingCount) {
            return rankCalculator.Add(_id++, SearchResultType.Page, "", lucene, DateTime.MinValue, rating, ratingCount);
        }

        private RankableSearchResultItem AddNewItem(SearchResultRankCalculator rankCalculator, double lucene) {
            return rankCalculator.Add(_id++, SearchResultType.Page, "", lucene, DateTime.MinValue, null, 0);
        }

        private RankableSearchResultItem NewItemFromSet(double lucene, double? rating, int ratingCount) {
            return NewItemFromSet(lucene, rating, ratingCount, 0.5);
        }

        private RankableSearchResultItem NewItemFromSet(double lucene, double? rating, int ratingCount, double ratingMidpoint) {
            var c = new SearchResultRankCalculator(0, 0, 0, ratingMidpoint, 0, 0);
            return c.Add(_id++, SearchResultType.Page, "", lucene, DateTime.MinValue, rating, ratingCount);
        }

        private double CalcRatingBoost(RankableSearchResultItem item) {

            // this does not try to do the capping that the real formula does. That is tested separately
            return _ratingPromotion * item.Rating / _ratingCount;
        }

        private double CalcPopularityBoost(int popularity) {

            // this does not try to do the capping that the real formula does. That is tested separately
            return _popularityBoost * (double)popularity / _popularityThreshold;
        }


    }
}

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
using System.Linq;
using log4net;
using MindTouch.Deki.Data;

namespace MindTouch.Deki.Search {
    public class SearchResultRankCalculator : IEnumerable<SearchResultItem> {

        //--- Class Fields ---
        private static readonly ILog _log = LogUtils.CreateLog();

        //--- Fields ---
        private readonly HashSet<string> _dedup = new HashSet<string>();
        private readonly List<RankableSearchResultItem> _items = new List<RankableSearchResultItem>();
        private readonly int _ratingsPromotionBoost;
        private readonly int _ratingsDemotionBoost;
        private readonly int _maxRatingCount;
        private readonly int _popularityBoost;
        private readonly int _popularityThreshold;
        private readonly double _ratingMidpoint;

        //--- Constructors ---
        public SearchResultRankCalculator(
            int ratingsPromotionBoost,
            int ratingsDemotionBoost,
            int maxRatingCount,
            double ratingMidpoint,
            int searchPopularityBoost,
            int popularityThreshold
        ) {
            _ratingsPromotionBoost = ratingsPromotionBoost;
            _ratingsDemotionBoost = ratingsDemotionBoost;
            _maxRatingCount = maxRatingCount;
            ratingMidpoint = Math.Max(0, ratingMidpoint);
            ratingMidpoint = Math.Min(ratingMidpoint, 1);
            _ratingMidpoint = ratingMidpoint;
            _popularityBoost = searchPopularityBoost;
            _popularityThreshold = popularityThreshold;
        }

        //--- Methods ---
        public RankableSearchResultItem Add(
            uint typeId,
            SearchResultType type,
            string title,
            double score,
            DateTime modified,
            double? rating,
            int ratingCount
        ) {
            if(_ratingMidpoint == 0) {
                rating = rating ?? 0;
            } else if(rating.HasValue) {
                var r = rating.Value - _ratingMidpoint;
                rating = r > 0 ? r / (1 - _ratingMidpoint) : r / _ratingMidpoint;
            }
            var item = new RankableSearchResultItem(typeId, type, title, score, modified, rating ?? 0, ratingCount);
            var key = item.DetailKey;
            if(_dedup.Contains(key)) {
                _log.WarnFormat("Found duplicate entry for {0}:{1} in search results. The index is most likely corrupted and should be rebuilt", item.Type, item.TypeId);
                return null;
            }
            _items.Add(item);
            _dedup.Add(key);
            return item;
        }

        public void ComputeRank(IEnumerable<ResultPopularityBE> searchPopularityRank) {
            _log.DebugFormat("computing rank for set of {0} items", _items.Count);

            // build popularity rank information, if provided
            Dictionary<ulong, uint> popularityRank = null;
            if(searchPopularityRank != null) {
                popularityRank = new Dictionary<ulong, uint>();
                foreach(var pop in searchPopularityRank) {
                    popularityRank[GetResultKey(pop.Type, pop.TypeId)] = pop.Count;
                }
            }

            // normalize rank sources and compute rank
            double maxComputedRank = double.MinValue;
            double minComputedRank = double.MaxValue;
            int position = _items.Count;
            foreach(var item in _items.OrderByDescending(x => x.LuceneScore)) {
                item.Position = position;
                if(popularityRank != null && _popularityThreshold != 0) {
                    item.SearchPopularity = GetPopularity(popularityRank, item);
                    item.SearchPopularityBoost = (double)_popularityBoost * Math.Min(_popularityThreshold, item.SearchPopularity) / _popularityThreshold;
                }
                var weightedRating = item.Rating * Math.Min(_maxRatingCount, item.RatingCount) / _maxRatingCount;
                item.RatingBoost = weightedRating > 0
                    ? _ratingsPromotionBoost * weightedRating
                    : _ratingsDemotionBoost * weightedRating;
                item.RawRank = item.Position + item.RatingBoost + item.SearchPopularityBoost;
                maxComputedRank = Math.Max(maxComputedRank, item.RawRank);
                minComputedRank = Math.Min(minComputedRank, item.RawRank);
                position--;
            }

            // normalize rank
            foreach(var item in _items) {
                item.SetRank((item.RawRank - minComputedRank) / (maxComputedRank - minComputedRank));
                _log.TraceFormat("{0}({1}):{2},rank:{3:0.000},lucene:{4:0.000},position:{5},ratingboost:{6:0.00},popboost:{7:0.00}",
                   item.Type, item.TypeId, item.Title, item.Rank, item.LuceneScore, item.Position, item.RatingBoost, item.SearchPopularityBoost);
            }
        }

        private int GetPopularity(Dictionary<ulong, uint> lookup, RankableSearchResultItem item) {
            uint count;
            lookup.TryGetValue(GetResultKey(item.Type, item.TypeId), out count);
            return count.ToInt();
        }

        private ulong GetResultKey(SearchResultType type, uint id) {
            return (ulong)type << 32 | id;
        }

        #region Implementation of IEnumerable
        public IEnumerator<SearchResultItem> GetEnumerator() {
            return _items.Cast<SearchResultItem>().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }
        #endregion
    }
}
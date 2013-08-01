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
using MindTouch.Deki.Data;

namespace MindTouch.Deki.Search {
    public class RankableSearchResultItem : SearchResultItem {

        //--- Fields ----
        public readonly double Rating;
        public readonly int RatingCount;
        public readonly double LuceneScore;
        public int Position;
        public double RatingBoost;
        public double SearchPopularityBoost;
        public int SearchPopularity;
        public double RawRank;

        //--- Constructors ---
        public RankableSearchResultItem(
            uint typeId,
            SearchResultType type,
            string title,
            double score,
            DateTime modified,
            double rating,
            int ratingCount
        ) : base(typeId, type, title, score, modified) {
            Rating = rating;
            RatingCount = ratingCount;
            LuceneScore = score;
        }

        //--- Methods ---
        public void SetRank(double rank) {
            _rank = rank;
        }
    }
}
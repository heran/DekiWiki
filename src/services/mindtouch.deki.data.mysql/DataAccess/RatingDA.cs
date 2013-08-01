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
using System.Data;

namespace MindTouch.Deki.Data.MySql {
    public partial class MySqlDekiDataSession {

        //--- Constants ---
        private const int RATING_TREND_SIZE = 10; // trend score includes this many recent ratings

        public void Rating_Insert(RatingBE rating) {

            // Reset previous rating
            Rating_ResetUserResourceRating(rating.UserId, rating.ResourceId, rating.ResourceType, rating.Timestamp);

            // Insert new rating
            Catalog.NewQuery(@" /* RatingDA::Rating_Insert */
INSERT INTO ratings (rating_user_id, rating_resource_id, rating_resource_type, rating_resource_revision, rating_score, rating_timestamp, rating_reset_timestamp)
VALUES (?RATING_USER_ID, ?RATING_RESOURCE_ID, ?RATING_RESOURCE_TYPE, ?RATING_RESOURCE_REVISION, ?RATING_SCORE, ?RATING_TIMESTAMP, ?RATING_RESET_TIMESTAMP);
")
           .With("RATING_RESOURCE_ID", rating.ResourceId)
           .With("RATING_RESOURCE_TYPE", (byte)rating.ResourceType)
           .With("RATING_USER_ID", rating.UserId)
           .With("RATING_RESOURCE_REVISION", rating.ResourceRevision)
           .With("RATING_SCORE", rating.Score)
           .With("RATING_TIMESTAMP", rating.Timestamp)
           .With("RATING_RESET_TIMESTAMP", rating.TimestampReset)
           .Execute();

            // Compute new rating and trend
            Rating_Compute(rating.ResourceId, rating.ResourceType, rating.Timestamp);
        }

        public void Rating_ResetUserResourceRating(uint userId, ulong resourceId, RatingBE.Type resourceType, DateTime resetTimestamp) {
            Catalog.NewQuery(@" /* RatingDA::Rating_ResetUserResourceRating */
UPDATE  ratings
SET     rating_reset_timestamp = ?RATING_RESET_TIMESTAMP
WHERE   rating_resource_id = ?RATING_RESOURCE_ID
AND     rating_resource_type = ?RATING_RESOURCE_TYPE
AND     rating_user_id = ?RATING_USER_ID
AND     rating_reset_timestamp is NULL;")
           .With("RATING_RESOURCE_ID", resourceId)
           .With("RATING_RESOURCE_TYPE", (byte)resourceType)
           .With("RATING_USER_ID", userId)
           .With("RATING_RESET_TIMESTAMP", resetTimestamp)
           .Execute();

            Rating_Compute(resourceId, resourceType, resetTimestamp);
        }

        public RatingComputedBE Rating_GetResourceRating(ulong resourceId, RatingBE.Type resourceType) {
            RatingComputedBE rc = null;
            string query = @" /* RatingDA::Rating_GetResourceRating */
SELECT *
FROM    ratingscomputed
WHERE   ratingscomputed_resource_id = ?RATINGSCOMPUTED_RESOURCE_ID
AND     ratingscomputed_resource_type = ?RATINGSCOMPUTED_RESOURCE_TYPE;
";
            Catalog.NewQuery(query)
                .With("RATINGSCOMPUTED_RESOURCE_ID", resourceId)
                .With("RATINGSCOMPUTED_RESOURCE_TYPE", (byte)resourceType)
            .Execute(delegate(IDataReader dr) {
                if(dr.Read()) {
                    rc = Rating_PopulateRatingComputed(dr);
                }
            });
            return rc;
        }

        public RatingBE Rating_GetUserResourceRating(uint userId, ulong resourceId, RatingBE.Type resourceType) {
            RatingBE r = null;
            string query = @" /* RatingDA::Rating_GetUserResourceRating */
SELECT *
FROM    ratings
WHERE   rating_resource_id = ?RATING_RESOURCE_ID
AND     rating_resource_type = ?RATING_RESOURCE_TYPE
AND     rating_user_id = ?RATING_USER_ID
AND     rating_reset_timestamp is NULL;
";
            Catalog.NewQuery(query)
                .With("RATING_RESOURCE_ID", resourceId)
                .With("RATING_RESOURCE_TYPE", (byte)resourceType)
                .With("RATING_USER_ID", userId)
            .Execute(delegate(IDataReader dr) {
                if(dr.Read()) {
                    r = Rating_PopulateRating(dr);
                }
            });
            return r;
        }

        private void Rating_Compute(ulong resourceId, RatingBE.Type resourceType, DateTime timestamp) {

            int sumRatingScore = 0;
            int ratingsCount = 0;
            int trendSumRatingScore = 0;
            int trendRatingsCount = 0;

            Catalog.NewQuery(@" /* RatingDA::Rating_Compute: determine score */
SELECT  CAST(SUM(rating_score) as UNSIGNED INTEGER) as sum_rating_score, COUNT(*) as count_ratings
FROM    ratings
WHERE   rating_resource_id = ?RATING_RESOURCE_ID
AND     rating_resource_type = ?RATING_RESOURCE_TYPE
AND     rating_reset_timestamp IS NULL;

SELECT  CAST(SUM(rating_score) as UNSIGNED INTEGER) as trend_sum_rating_score, COUNT(*) as trend_count_ratings
FROM    ratings
WHERE   rating_resource_id = ?RATING_RESOURCE_ID
AND     rating_resource_type = ?RATING_RESOURCE_TYPE
AND     rating_reset_timestamp IS NULL
ORDER BY rating_timestamp DESC
LIMIT ?TREND_SIZE;
")
            .With("RATING_RESOURCE_ID", resourceId)
            .With("RATING_RESOURCE_TYPE", (byte)resourceType)
            .With("TREND_SIZE", RATING_TREND_SIZE)
            .Execute(delegate(IDataReader dr) {
                if(dr.Read()) {
                    sumRatingScore = dr.Read<int>("sum_rating_score", 0);
                    ratingsCount = dr.Read<int>("count_ratings", 0);
                }
                if(dr.NextResult() && dr.Read()) {
                    trendSumRatingScore = dr.Read<int>("trend_sum_rating_score", 0);
                    trendRatingsCount = dr.Read<int>("trend_count_ratings", 0);
                }
            });

            if(ratingsCount > 0 && trendRatingsCount > 0) {
                float score = (float)sumRatingScore / (float)ratingsCount;
                float scoreTrend = (float)trendSumRatingScore / (float)trendRatingsCount;

                Catalog.NewQuery(@" /* RatingDA::Rating_Compute: set computed rating */
REPLACE INTO ratingscomputed
SET     ratingscomputed_resource_id = ?RATING_RESOURCE_ID,
        ratingscomputed_resource_type = ?RATING_RESOURCE_TYPE,
        ratingscomputed_timestamp = ?RATING_TIMESTAMP,
        ratingscomputed_score = ?RATING_SCORE,
        ratingscomputed_count = ?RATING_COUNT,
        ratingscomputed_score_trend = ?RATING_SCORE_TREND;
")
                .With("RATING_RESOURCE_ID", resourceId)
                .With("RATING_RESOURCE_TYPE", (byte)resourceType)
                .With("RATING_TIMESTAMP", timestamp)
                .With("RATING_SCORE", score)
                .With("RATING_COUNT", ratingsCount)
                .With("RATING_SCORE_TREND", scoreTrend)
                .Execute();
            } else {

                // if no active ratings exist for a page, remove the computed rating
                Catalog.NewQuery(@" /* RatingDA::Rating_Compute: delete computed rating */
DELETE FROM ratingscomputed
WHERE   ratingscomputed_resource_id = ?RATING_RESOURCE_ID
AND     ratingscomputed_resource_type = ?RATING_RESOURCE_TYPE;
")
                .With("RATING_RESOURCE_ID", resourceId)
                .With("RATING_RESOURCE_TYPE", (byte)resourceType)
                .Execute();
            }
        }

        private RatingBE Rating_PopulateRating(IDataReader dr) {
            RatingBE r = new RatingBE {
                Id = dr.Read<uint>("rating_id"),
                UserId = dr.Read<uint>("rating_user_id"),
                ResourceId = dr.Read<uint>("rating_resource_id"),
                ResourceType = (RatingBE.Type)dr.Read<byte>("rating_resource_type"),
                ResourceRevision = dr.Read<ulong?>("rating_resource_revision", null),
                Score = dr.Read<float>("rating_score"),
                Timestamp = dr.Read<DateTime>("rating_timestamp"),
                TimestampReset = dr.Read<DateTime?>("rating_reset_timestamp", null)
            };
            return r;
        }

        private RatingComputedBE Rating_PopulateRatingComputed(IDataReader dr) {
            RatingComputedBE rc = new RatingComputedBE {
                Id = dr.Read<uint>("ratingscomputed_id"),
                ResourceId = dr.Read<uint>("ratingscomputed_resource_id"),
                ResourceType = (RatingBE.Type)dr.Read<byte>("ratingscomputed_resource_type"),
                Score = dr.Read<float>("ratingscomputed_score"),
                ScoreTrend = dr.Read<float>("ratingscomputed_score_trend"),
                Count = dr.Read<uint>("ratingscomputed_count"),
                Timestamp = dr.Read<DateTime>("ratingscomputed_timestamp")
            };
            return rc;
        }
    }
}

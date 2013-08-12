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
using MindTouch.Deki.Exceptions;
using MindTouch.Xml;

namespace MindTouch.Deki.Logic {
    public static class RatingBL {

        //--- Properties ---
        private static bool IsRatingsEnabled {
            get {
                var license = DekiContext.Current.LicenseManager;
                var licenseState = DekiContext.Current.LicenseManager.LicenseState;
                return (licenseState == LicenseStateType.TRIAL || licenseState == LicenseStateType.COMMERCIAL)
                    && ((license.GetCapability(LicenseBL.CONTENTRATING) ?? string.Empty).EqualsInvariantIgnoreCase(LicenseBL.CONTENTRATING_ENABLED));
            }
        }
        
        //--- Methods ---
        public static void SetRating(PageBE page, UserBE user, float? score) {
            ThrowOnInvalidLicense();
            RatingBE currentRating = DbUtils.CurrentSession.Rating_GetUserResourceRating(user.ID, page.ID, RatingBE.Type.PAGE);

            if(score == null) {
                if(currentRating == null) {

                    // no rating exists currently: noop
                    return;
                }

                // reset a user ratings for a page
                DbUtils.CurrentSession.Rating_ResetUserResourceRating(user.ID, page.ID, RatingBE.Type.PAGE, DekiContext.Current.Now);
            } else {

                // set or update a page rating

                // Valid score is limited to 0 and 1.
                if(score != 0 && score != 1) {
                    throw new RatingInvalidArgumentException();
                }

                if(currentRating != null && currentRating.Score == score) {

                    // an equal score already exists: noop
                    return;
                }

                RatingBE rating = new RatingBE();
                rating.ResourceId = page.ID;
                rating.ResourceType = RatingBE.Type.PAGE;
                rating.ResourceRevision = page.Revision;
                rating.Timestamp = DekiContext.Current.Now;
                rating.TimestampReset = null;
                rating.UserId = user.ID;
                rating.Score = score.Value;

                // Set a new rating
                DbUtils.CurrentSession.Rating_Insert(rating);
            }
            // Trigger a notification
            DekiContext.Current.Instance.EventSink.PageRated(DekiContext.Current.Now, page, user);
        }

        public static XDoc GetRatingXml(PageBE page, UserBE user) {
            return AppendRatingXml(null, page, user);
        }

        public static XDoc AppendRatingXml(XDoc doc, PageBE page, UserBE user) {
            bool endDoc = false;
            if(doc == null) {
                doc = new XDoc("rating");
            } else {
                doc.Start("rating");
                endDoc = true;
            }

            RatingComputedBE ratingEffective = DbUtils.CurrentSession.Rating_GetResourceRating(page.ID, RatingBE.Type.PAGE);
            RatingBE userRating = null;
            if(ratingEffective != null) {

                doc.Attr("score", ratingEffective.Score)
                   .Attr("score.trend", ratingEffective.ScoreTrend)
                   .Attr("count", ratingEffective.Count)
                   .Attr("date", ratingEffective.Timestamp);

                userRating = DbUtils.CurrentSession.Rating_GetUserResourceRating(user.ID, page.ID, RatingBE.Type.PAGE);
                if(userRating != null) {
                    doc.Start("user.ratedby")
                        .Attr("id", user.ID)
                        .Attr("score", userRating.Score.ToString())
                        .Attr("date", userRating.Timestamp)
                        .Attr("href", UserBL.GetUri(user))
                        .End();
                }
            } else {

                // No ratings exist for page: Output placeholder
                doc.Attr("score", string.Empty)
                    .Attr("count", 0);          
            }

            if(endDoc) {
                doc.End(); // rating
            }

            return doc;
        }

        private static void ThrowOnInvalidLicense() {
            if(!IsRatingsEnabled) {
                throw new MindTouchLicenseInvalidOperationForbiddenException("Content Rating API");
            }
        }
    }
}
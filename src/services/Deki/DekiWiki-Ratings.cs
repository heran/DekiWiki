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
using System.IO;

using MindTouch.Deki.Data;
using MindTouch.Deki.Exceptions;
using MindTouch.Deki.Logic;
using MindTouch.Dream;
using MindTouch.Tasking;
using MindTouch.Xml;

namespace MindTouch.Deki {
    using Yield = IEnumerator<IYield>;

    public partial class DekiWikiService {

        //--- Features ---
        [DreamFeature("GET:pages/{pageid}/ratings", "Retrieve the page rating")]
        [DreamFeatureParam("redirects", "int?", "If zero, do not follow page redirects.")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        public Yield GetPageRating(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            UserBE user = DekiContext.Current.User;
            PageBE page = PageBL_AuthorizePage(context, user, Permissions.READ, false);
            XDoc ret = RatingBL.GetRatingXml(page, user);
            response.Return(DreamMessage.Ok(ret));
            yield break;
        }

        [DreamFeature("POST:pages/{pageid}/ratings", "Rate the quality of a page")]
        [DreamFeatureParam("{pageid}", "string", "either an integer page ID, \"home\", or \"=\" followed by a double uri-encoded page title")]
        [DreamFeatureParam("score", "int", "A '0' or '1' respectively indicating a poor or good rating. A value of '' will reset a rating.")]
        [DreamFeatureParam("redirects", "int?", "If zero, do not follow page redirects.")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        [DreamFeatureStatus(DreamStatus.Ok, "The request completed successfully")]
        [DreamFeatureStatus(DreamStatus.BadRequest, "Invalid input parameter")]
        [DreamFeatureStatus(DreamStatus.Forbidden, "READ access to the page is required")]
        [DreamFeatureStatus(DreamStatus.NotFound, "Requested page could not be found")]
        public Yield PostPageRating(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            UserBE user = DekiContext.Current.User;
            if(UserBL.IsAnonymous(user)) {
                throw new RatingForAnonymousDeniedException(AUTHREALM, string.Empty);
            }
            PageBE page = PageBL_AuthorizePage(context, user, Permissions.READ, false);
            string scoreStr = context.GetParam("score");
            float? score = null;
            if(!string.IsNullOrEmpty(scoreStr)) {
                float tempScore;
                if(!float.TryParse(scoreStr, out tempScore)) {
                    throw new RatingInvalidArgumentException();
                }
                score = tempScore;                
            }
            RatingBL.SetRating(page, user, score);
            XDoc ret = RatingBL.GetRatingXml(page, user);
            response.Return(DreamMessage.Ok(ret));
            yield break;
        }
    }
}
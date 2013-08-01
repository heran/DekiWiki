/*
 * MindTouch Core - open source enterprise collaborative networking
 * Copyright (c) 2006-2009 MindTouch Inc.
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
using MindTouch.Deki.Logic;
using MindTouch.Dream;
using MindTouch.Tasking;
using MindTouch.Xml;

namespace MindTouch.Deki {
    using Yield = IEnumerator<IYield>;

    public partial class DekiWikiService {

        [DreamFeature("GET:pages/{pageid}/tags", "Retrieve the tags on a page.")]
        [DreamFeatureParam("{pageid}", "string", "either an integer page ID, \"home\", or \"=\" followed by a double uri-encoded page title")]
        [DreamFeatureParam("redirects", "int?", "If zero, do not follow page redirects.")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        [DreamFeatureStatus(DreamStatus.Ok, "The request completed successfully")]
        [DreamFeatureStatus(DreamStatus.BadRequest, "Invalid input parameter or request body")]
        [DreamFeatureStatus(DreamStatus.Forbidden, "Read access to the page is required")]
        [DreamFeatureStatus(DreamStatus.NotFound, "The requested page could not be found")]
        public Yield GetPageTags(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            PageBE page = PageBL_AuthorizePage(context, null, Permissions.READ, false);
            XUri href = DekiContext.Current.ApiUri.At("pages", page.ID.ToString(), "tags");
            var tagBL = new TagBL();
            XDoc doc = tagBL.GetTagListXml(tagBL.GetTagsForPage(page), "tags", href, false);
            response.Return(DreamMessage.Ok(doc));
            yield break;
        } 

        [DreamFeature("PUT:pages/{pageid}/tags", "Sets the tags on a page.")]
        [DreamFeatureParam("{pageid}", "string", "either an integer page ID, \"home\", or \"=\" followed by a double uri-encoded page title")]
        [DreamFeatureParam("redirects", "int?", "If zero, do not follow page redirects.")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        [DreamFeatureStatus(DreamStatus.Ok, "The request completed successfully")]
        [DreamFeatureStatus(DreamStatus.BadRequest, "Invalid input parameter or request body")]
        [DreamFeatureStatus(DreamStatus.Forbidden, "Update access to the page is required")]
        [DreamFeatureStatus(DreamStatus.NotFound, "The requested page could not be found")]
        public Yield SetPageTags(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            PageBE page = PageBL_AuthorizePage(context, null, Permissions.UPDATE, false);
            XUri href = DekiContext.Current.ApiUri.At("pages", page.ID.ToString(), "tags");
            var tagBL = new TagBL();
            tagBL.PutTagsFromXml(page, request.ToDocument());
            XDoc doc = tagBL.GetTagListXml(tagBL.GetTagsForPage(page), "tags", href, false);
            response.Return(DreamMessage.Ok(doc));
            yield break;
        }

        [DreamFeature("GET:site/tags", "Retrieve all tags")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        [DreamFeatureParam("type", "string?", "type of the tag (text | date | user | define) (default: all types)")]
        [DreamFeatureParam("from", "string?", "start date for type=date (ex: 2008-01-30) (default: now)")]
        [DreamFeatureParam("to", "string?", "end date for type=date (ex: 2008-12-30) (default: now + 30 days)")]
        [DreamFeatureParam("pages", "bool?", "show pages with each tag (default: false)")]
        [DreamFeatureParam("q", "string?", "partial tag name to match (ex: tagprefix) (default none)")]
        [DreamFeatureStatus(DreamStatus.Ok, "The request completed successfully")]
        [DreamFeatureStatus(DreamStatus.BadRequest, "Invalid input parameter or request body")]
        [DreamFeatureStatus(DreamStatus.Forbidden, "Read access to the page is required")]
        [DreamFeatureStatus(DreamStatus.NotFound, "The requested page could not be found")]
        public Yield GetTags(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            string type = DreamContext.Current.GetParam("type", "");
            string fromStr = DreamContext.Current.GetParam("from", "");
            string toStr = DreamContext.Current.GetParam("to", "");
            bool showPages = DreamContext.Current.GetParam("pages", false);
            string partialName = DreamContext.Current.GetParam("q", "");

            // parse type
            TagType tagType = TagType.ALL;
            if(!string.IsNullOrEmpty(type) && !SysUtil.TryParseEnum(type, out tagType)) {
                throw new DreamBadRequestException("Invalid type parameter");
            }

            // check and validate from date
            DateTime from = (tagType == TagType.DATE) ? DateTime.Now : DateTime.MinValue;
            if(!string.IsNullOrEmpty(fromStr) && !DateTime.TryParse(fromStr, out from)) {
                throw new DreamBadRequestException("Invalid from date parameter");
            }

            // check and validate to date
            DateTime to = (tagType == TagType.DATE) ? from.AddDays(30) : DateTime.MaxValue;
            if(!string.IsNullOrEmpty(toStr) && !DateTime.TryParse(toStr, out to)) {
                throw new DreamBadRequestException("Invalid to date parameter");
            }

            // execute query
            var tagBL = new TagBL();
            var tags = tagBL.GetTags(partialName, tagType, from, to);
            XDoc doc = tagBL.GetTagListXml(tags, "tags", null, showPages);
            response.Return(DreamMessage.Ok(doc));
            yield break;
        }

        [DreamFeature("GET:site/tags/{tagid}", "Retrieve pages with tag")]
        [DreamFeatureParam("{tagid}", "string", "either an integer tag ID or \"=\" followed by a double uri-encoded tag name")]
        [DreamFeatureParam("language", "string?", "filter pages by language (default: all languages)")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        [DreamFeatureStatus(DreamStatus.Ok, "The request completed successfully")]
        [DreamFeatureStatus(DreamStatus.BadRequest, "Invalid input parameter or request body")]
        [DreamFeatureStatus(DreamStatus.Forbidden, "Read access to the page is required")]
        [DreamFeatureStatus(DreamStatus.NotFound, "The requested page could not be found")]
        public Yield GetTaggedPages(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            string language = DreamContext.Current.GetParam("language", null);
            var tagBL = new TagBL();
            XDoc doc = tagBL.GetTagXml(tagBL.GetTagFromUrl(context), true, language);
            response.Return(DreamMessage.Ok(doc));
            yield break;
        } 
    }
}

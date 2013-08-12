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

using System.Collections.Generic;

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
        [DreamFeature("GET:comments/{commentid}", "Retrieve a comment and metadata")]
        [DreamFeature("GET:pages/{pageid}/comments/{commentnumber}", "Retrieve a comment and metadata")]
        [DreamFeatureParam("{pageid}", "string?", "either an integer page ID, \"home\", or \"=\" followed by a double uri-encoded page title")]
        [DreamFeatureParam("{commentnumber}", "int?", "identifies the comment on the page")]
        [DreamFeatureParam("{commentid}", "int?", "identifies the comment by its unique id")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        [DreamFeatureStatus(DreamStatus.Ok, "Request completed successfully")]
        [DreamFeatureStatus(DreamStatus.BadRequest, "Invalid input parameter or request body")]
        [DreamFeatureStatus(DreamStatus.Forbidden, "Read access to the page is required")]
        [DreamFeatureStatus(DreamStatus.NotFound, "Requested comment could not be found")]
        public Yield GetComment(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            PageBE page = null;
            CommentBE comment = null;
            var commentId = context.GetParam<uint>("commentid", 0);
            if(commentId > 0) {
                comment = CommentBL.GetComment(commentId);
            } else {
                GetCommentFromRequest(context, Permissions.READ, out page, out comment);
            }
            response.Return(DreamMessage.Ok(CommentBL.GetCommentXml(comment, null)));
            yield break;
        }

        [DreamFeature("GET:pages/{pageid}/comments/{commentnumber}/content", "Retrieve the comment text only")]
        [DreamFeatureParam("{pageid}", "string", "either an integer page ID, \"home\", or \"=\" followed by a double uri-encoded page title")]
        [DreamFeatureParam("{commentnumber}", "int", "identifies the comment on the page")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        [DreamFeatureStatus(DreamStatus.Ok, "The request completed successfully")]
        [DreamFeatureStatus(DreamStatus.BadRequest, "Invalid input parameter or request body")]
        [DreamFeatureStatus(DreamStatus.Forbidden, "Read access to the page is required")]
        [DreamFeatureStatus(DreamStatus.NotFound, "The requested comment could not be found")]
        public Yield GetCommentContent(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            PageBE page = null;
            CommentBE comment = null;
            GetCommentFromRequest(context, Permissions.READ, out page, out comment);
            response.Return(DreamMessage.Ok(new MimeType(comment.ContentMimeType), comment.Content));
            yield break;
        }

        [DreamFeature("GET:pages/{pageid}/comments", "Retrieve the comments on a page")]
        [DreamFeatureParam("{pageid}", "string", "either an integer page ID, \"home\", or \"=\" followed by a double uri-encoded page title")]
        [DreamFeatureParam("format", "{xml, atom}?", "Output format (default: xml).")]
        [DreamFeatureParam("filter", "string?", "Comments to return: any, nondeleted. default: nondeleted")]
        [DreamFeatureParam("limit", "string?", "Maximum number of items to retrieve. Must be a positive number or 'all' to retrieve all items. (default: 100)")]
        [DreamFeatureParam("offset", "int?", "Number of items to skip. Must be a positive number or 0 to not skip any. (default: 0)")]
        [DreamFeatureParam("sortby", "{date.posted}?", "Sort field. Prefix value with '-' to sort descending. (default: date.posted)")]
        [DreamFeatureParam("depth", "string?", "Use 'infinity' to return comments from all descendant pages. (default: 0)")]
        [DreamFeatureParam("postedbyuserid", "int?", "Only return comments posted by the id of a user")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        [DreamFeatureStatus(DreamStatus.Ok, "The request completed successfully")]
        [DreamFeatureStatus(DreamStatus.BadRequest, "Invalid input parameter or request body")]
        [DreamFeatureStatus(DreamStatus.Forbidden, "Read access to the page is required")]
        [DreamFeatureStatus(DreamStatus.NotFound, "The requested page could not be found")]
        public Yield GetPageComments(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            CheckResponseCache(context, false);

            PageBE page = PageBL_AuthorizePage(context, null, Permissions.READ, false);
            uint count, offset;

            string filterStr = context.GetParam("filter", "nondeleted");
            CommentFilter filter;
            switch(filterStr.ToLowerInvariant()) {
            case "any":
                filter = CommentFilter.ANY;
                break;
            case "nondeleted":
                filter = CommentFilter.NONDELETED;
                break;
            default:
                throw new CommentFilterInvalidArgumentException();
            }
            bool includeDescendantPages = false;
            string depth = context.GetParam("depth", "0");
            switch(depth.ToLowerInvariant()) {
            case "0":
                includeDescendantPages = false;
                break;
            case "infinity":
                includeDescendantPages = true;
                break;
            default:
                throw new DreamBadRequestException(string.Format("Invalid depth value '{0}'. Supported values are '0' and 'infinity'.", depth));
            }

            uint postedByUserIdtmp = context.GetParam<uint>("postedbyuserid", 0);
            uint? postedByUserId = null;
            if(postedByUserIdtmp > 0) {
                postedByUserId = postedByUserIdtmp;
            }

            SortDirection sortDir;
            string sortField;
            Utils.GetOffsetAndCountFromRequest(context, 100, out count, out offset, out sortDir, out sortField);
            sortDir = sortDir == SortDirection.UNDEFINED ? SortDirection.ASC : sortDir; // default sort is ascending by timestamp
            uint totalCount;
            XUri commentsUri = DekiContext.Current.ApiUri.At("pages", page.ID.ToString(), "comments");
            IList<CommentBE> comments = CommentBL.RetrieveCommentsForPage(page, filter, includeDescendantPages, postedByUserId, sortDir, offset, count, out totalCount);

            XDoc ret = null;
            MimeType mimetype;
            switch(context.GetParam("format", "xml").ToLowerInvariant()) {
            case "xml":
                ret = CommentBL.GetCommentXml(comments, true, null, includeDescendantPages, offset, sortDir, null, commentsUri, totalCount);
                mimetype = MimeType.XML;
                break;
            case "atom":
                ret = CommentBL.GetCommentXmlAsAtom(comments, context.Uri, page);
                mimetype = MimeType.ATOM;
                break;
            default:
                throw new DreamBadRequestException("Invalid format. Valid formats are 'xml' and 'atom'.");
            }

            response.Return(DreamMessage.Ok(mimetype, ret));
            yield break;
        }

        [DreamFeature("PUT:pages/{pageid}/comments/{commentnumber}/content", "Edit the specified comment")]
        [DreamFeatureParam("{pageid}", "string", "either an integer page ID, \"home\", or \"=\" followed by a double uri-encoded page title")]
        [DreamFeatureParam("{commentnumber}", "int", "identifies the comment on the page")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        [DreamFeatureStatus(DreamStatus.Ok, "The request completed successfully")]
        [DreamFeatureStatus(DreamStatus.BadRequest, "Invalid input parameter or request body (must be text MIME type)")]
        [DreamFeatureStatus(DreamStatus.Forbidden, "Administrator access or comment author is required")]
        [DreamFeatureStatus(DreamStatus.NotFound, "The requested comment could not be found")]
        public Yield PutCommentContent(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            PageBE page = null;
            CommentBE comment = null;
            GetCommentFromRequest(context, Permissions.READ, out page, out comment);
            if(UserBL.IsAnonymous(DekiContext.Current.User)) {
                throw new CommentPostForAnonymousDeniedException(AUTHREALM, string.Empty);
            }
            comment = CommentBL.EditExistingComment(page, comment, request, context);
            if(comment != null) {
                DekiContext.Current.Instance.EventSink.CommentUpdate(DekiContext.Current.Now, comment, page, DekiContext.Current.User);
                response.Return(DreamMessage.Ok(CommentBL.GetCommentXml(comment, null)));
            } else {
                throw new CommentFailedEditFatalException();
            }
            yield break;
        }

        [DreamFeature("POST:pages/{pageid}/comments", "Post a new comment to a page")]
        [DreamFeatureParam("{pageid}", "string", "either an integer page ID, \"home\", or \"=\" followed by a double uri-encoded page title")]
        [DreamFeatureParam("title", "string?", "Title for comment")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        [DreamFeatureStatus(DreamStatus.Ok, "The request completed successfully")]
        [DreamFeatureStatus(DreamStatus.BadRequest, "Invalid input parameter or request body (must be text MIME type)")]
        [DreamFeatureStatus(DreamStatus.Forbidden, "Update access to the page is required")]
        [DreamFeatureStatus(DreamStatus.NotFound, "The requested page could not be found")]
        public Yield PostPageComment(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            PageBE page = PageBL_AuthorizePage(context, null, Permissions.READ, false);
            if(UserBL.IsAnonymous(DekiContext.Current.User)) {
                throw new CommentPostForAnonymousDeniedException(AUTHREALM, string.Empty);
            }
            CommentBE comment = CommentBL.PostNewComment(page, request, context);
            if(comment != null) {
                DekiContext.Current.Instance.EventSink.CommentCreate(DekiContext.Current.Now, comment, page, DekiContext.Current.User);
                response.Return(DreamMessage.Ok(CommentBL.GetCommentXml(comment, null)));
            } else {
                throw new CommentFailedPostFatalException();
            }
            yield break;
        }

        [DreamFeature("DELETE:pages/{pageid}/comments/{commentnumber}", "Mark a comment as being deleted. This hides comment content from non admins")]
        [DreamFeatureParam("{pageid}", "string", "either an integer page ID, \"home\", or \"=\" followed by a double uri-encoded page title")]
        [DreamFeatureParam("{commentnumber}", "int", "identifies the comment on the page")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        [DreamFeatureStatus(DreamStatus.Ok, "The request completed successfully")]
        [DreamFeatureStatus(DreamStatus.BadRequest, "Invalid input parameter or request body")]
        [DreamFeatureStatus(DreamStatus.Forbidden, "Administrator access or comment author is required")]
        [DreamFeatureStatus(DreamStatus.NotFound, "The requested comment could not be found")]
        public Yield DeleteComment(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            PageBE page = null;
            CommentBE comment = null;
            GetCommentFromRequest(context, Permissions.ADMIN, out page, out comment);
            CommentBL.DeleteComment(page, comment);
            response.Return(DreamMessage.Ok());
            yield break;
        }


        private void GetCommentFromRequest(DreamContext context, Permissions access, out PageBE page, out CommentBE comment) {
            page = null;
            comment = null;

            ushort commentNumber = context.GetParam<ushort>("commentnumber");

            if(commentNumber != 0) {
                page = PageBL_AuthorizePage(context, null, Permissions.READ, false);
                comment = DbUtils.CurrentSession.Comments_GetByPageIdNumber(page.ID, commentNumber);
            }

            if(comment == null) {
                throw new CommentNotFoundException();
            }
        }
    }
}

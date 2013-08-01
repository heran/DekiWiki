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
using MindTouch.Deki.Exceptions;
using MindTouch.Dream;
using MindTouch.Xml;

namespace MindTouch.Deki.Logic {

    public interface ICommentBL {

        //--- Methods ---
        IList<CommentBE> RetrieveCommentsForPage(PageBE page, CommentFilter filter, bool includePageDescendants, uint? postedByUserId, SortDirection sortDir, uint offset, uint limit, out uint totalComments);
        XUri GetUri(CommentBE comment);
    }

    public class CommentBLAdapter : ICommentBL {

        //--- Methods ---
        public IList<CommentBE> RetrieveCommentsForPage(PageBE page, CommentFilter filter, bool includePageDescendants, uint? postedByUserId, SortDirection sortDir, uint offset, uint limit, out uint totalComments) {
            return CommentBL.RetrieveCommentsForPage(page, filter, includePageDescendants, postedByUserId, sortDir, offset, limit, out totalComments);
        }

        public XUri GetUri(CommentBE comment) {
            return CommentBL.GetUri(comment);
        }
    }

    public static class CommentBL {

        public static CommentBE GetComment(uint commentId) {
            return DbUtils.CurrentSession.Comments_GetById(commentId);
        }

        public static IList<CommentBE> RetrieveCommentsForPage(PageBE page, CommentFilter filter, bool includePageDescendants, uint? postedByUserId, SortDirection sortDir, uint offset, uint limit, out uint totalComments) {
            IList<CommentBE> commentsForPage = DbUtils.CurrentSession.Comments_GetByPage(page, filter, includePageDescendants, postedByUserId, sortDir, offset, limit, out totalComments);

            if(includePageDescendants) {

                //Filter out comments from pages with no user permissions
                //NOTE: this will mess up limits/offsets without a way to apply permissions at the db layer.
                commentsForPage = ApplyPermissionFilter(commentsForPage);
            }

            return commentsForPage;
        }

        public static IList<CommentBE> RetrieveCommentsForUser(UserBE user) {
            IList<CommentBE> comments = DbUtils.CurrentSession.Comments_GetByUser(user.ID);
            comments = ApplyPermissionFilter(comments);
            return comments;
        }

        public static CommentBE PostNewComment(PageBE page, DreamMessage request, DreamContext context) {
            ValidateCommentText(request.ContentType, request.AsText());

            CommentBE comment = new CommentBE();
            comment.Title = context.GetParam("title", string.Empty);
            comment.PageId = page.ID;
            comment.Content = request.AsText();
            comment.ContentMimeType = request.ContentType.ToString();
            comment.PosterUserId = DekiContext.Current.User.ID;
            comment.CreateDate = DateTime.UtcNow;

            //Note (MaxM): Replytoid/replies not yet exposed
            //ulong replyToId = context.GetParam<ulong>("replyto", 0);
            //if (replyToId == 0)
            //    newComment.ReplyToId = null;
            //else
            //    newComment.ReplyToId = replyToId;

            ushort commentNumber;
            uint commentId = DbUtils.CurrentSession.Comments_Insert(comment, out commentNumber);
            if (commentId == 0) {
                return null;
            } else {
                comment.Id = commentId;
                comment.Number = commentNumber;
                PageBL.Touch(page, comment.CreateDate);
                RecentChangeBL.AddCommentCreateRecentChange(comment.CreateDate, page, DekiContext.Current.User, DekiResources.COMMENT_ADDED(comment.Number), comment);
                return comment;
            } 
        }

        public static void DeleteComment(PageBE page, CommentBE comment) {
            if(comment.PosterUserId != DekiContext.Current.User.ID) {
                PermissionsBL.CheckUserAllowed(DekiContext.Current.User, Permissions.ADMIN);
            }
            if (!comment.IsCommentMarkedAsDeleted) {
                comment.DeleteDate = DateTime.UtcNow;
                comment.DeleterUserId = DekiContext.Current.User.ID;
                DbUtils.CurrentSession.Comments_Update(comment);
                PageBL.Touch(page, comment.DeleteDate.Value);
                RecentChangeBL.AddCommentDeleteRecentChange(comment.DeleteDate.Value, page, DekiContext.Current.User, DekiResources.COMMENT_DELETED(comment.Number), comment);
                DekiContext.Current.Instance.EventSink.CommentDelete(DekiContext.Current.Now, comment, page, DekiContext.Current.User);
            }
        }

        public static CommentBE EditExistingComment(PageBE page, CommentBE comment, DreamMessage request, DreamContext context) {
            if (comment.PosterUserId != DekiContext.Current.User.ID) {
                PermissionsBL.CheckUserAllowed(DekiContext.Current.User, Permissions.ADMIN);
            }
            ValidateCommentText(request.ContentType, request.AsText());
            comment.LastEditDate = DateTime.UtcNow;
            comment.LastEditUserId = DekiContext.Current.User.ID;
            comment.Content = request.AsText();
            comment.ContentMimeType = request.ContentType.ToString();

            DbUtils.CurrentSession.Comments_Update(comment);
            PageBL.Touch(page, comment.LastEditDate.Value);
            RecentChangeBL.AddCommentUpdateRecentChange(comment.LastEditDate.Value, page, DekiContext.Current.User, DekiResources.COMMENT_EDITED(comment.Number), comment);
            return comment;
        }

        private static void ValidateCommentText(MimeType mimetype, string content) {
            if (!MimeType.TEXT.Match(mimetype))
                throw new CommentMimetypeUnsupportedInvalidArgumentException(mimetype);
        }

        private static IList<CommentBE> ApplyPermissionFilter(IList<CommentBE> comments) {
            var ret = new List<CommentBE>();
            
            //Collect page ids
            var pageids = comments.Select(e => e.PageId);

            //Determine pages to filter out due to perms
            IEnumerable<ulong> filteredOutPages;
            PermissionsBL.FilterDisallowed(DekiContext.Current.User, pageids, false, out filteredOutPages, Permissions.READ);
            
            //Hash filtered out pageids
            var filteredOutPagesHash = new Dictionary<ulong, object>();
            foreach(ulong f in filteredOutPages) {
                filteredOutPagesHash[f] = null;
            }

            //Remove filtered out comments
            foreach(CommentBE c in comments) {
                if(!filteredOutPagesHash.ContainsKey(c.PageId)) {
                    ret.Add(c);
                }
            }

            return ret;
        }

        #region Uris
        public static XUri GetUri(CommentBE comment) {
            return DekiContext.Current.ApiUri.At("pages", comment.PageId.ToString(), "comments", comment.Number.ToString());
        }
        #endregion

        #region XML Helpers

        public static XDoc GetCommentXmlAsAtom(IList<CommentBE> comments, XUri feedUri, PageBE page) {
            var resources = DekiContext.Current.Resources;
            string title = resources.Localize(DekiResources.COMMENT_FOR(page.Title.AsUserFriendlyName()));
            XAtomFeed feed = new XAtomFeed(title, feedUri, DateTime.UtcNow);
            feed.AddLink(PageBL.GetUriUi(page), XAtomBase.LinkRelation.Alternate, MimeType.XHTML, null, page.Title.AsUserFriendlyName());
            feed.Id = feedUri;

            foreach(CommentBE c in comments) {
                UserBE posterUser = UserBL.GetUserById(c.PosterUserId);
                title = c.Title;
                if(string.IsNullOrEmpty(title)) {
                    title = resources.Localize(DekiResources.COMMENT_BY_TO(posterUser.Name, page.Title.AsUserFriendlyName()));
                }
                XAtomEntry entry = feed.StartEntry(title, c.CreateDate, (c.LastEditDate == null || c.LastEditDate == DateTime.MinValue) ? c.CreateDate : c.LastEditDate.Value);
                entry.Id = GetUri(c);
                entry.AddAuthor(posterUser.Name, UserBL.GetUriUiHomePage(posterUser), posterUser.Email);
                MimeType commentMimetype;
                MimeType.TryParse(c.ContentMimeType, out commentMimetype);
                entry.AddContent(c.Content);

                XUri entryLink = PageBL.GetUriUi(page).WithFragment("comment" + c.Number);
                entry.AddLink(entryLink, XAtomBase.LinkRelation.Alternate, null, null, null);
                entry.AddLink(GetUri(c).At("content"), XAtomBase.LinkRelation.Enclosure, commentMimetype, c.Content.Length, "content");
                feed.End();
            }

            return feed;
        }

        public static XDoc GetCommentXml(CommentBE comment, string suffix) {
            return GetCommentXml(new List<CommentBE>() {comment}, false, suffix, true, null, null, null, null, null);
        }

        public static XDoc GetCommentXml(IList<CommentBE> comments, bool collection, string suffix, bool? includeParentInfo, uint? offset, SortDirection? sortDir, XDoc doc, XUri commentCollectionUri, uint? totalCount) {

            bool requiresEnd = false;
            if(collection) {
                string rootCommentsNode = string.IsNullOrEmpty(suffix) ? "comments" : "comments." + suffix;
                if(doc == null) {
                    doc = new XDoc(rootCommentsNode);
                } else {
                    doc.Start(rootCommentsNode);
                    requiresEnd = true;
                }

                if((offset ?? 0) > 0) {
                    doc.Attr("offset", offset.Value);
                }

                if((sortDir ?? SortDirection.UNDEFINED) != SortDirection.UNDEFINED) {
                    doc.Attr("sort", sortDir.ToString().ToLowerInvariant());
                }
                doc.Attr("count", comments.Count);
                if(totalCount != null) {
                    doc.Attr("totalcount", totalCount.Value);
                }
            } else {
                doc = XDoc.Empty;
            }

            if(commentCollectionUri != null) {
                doc.Attr("href", commentCollectionUri);
            }

            foreach(CommentBE c in comments) {
                doc = AppendCommentXml(doc, c, suffix, includeParentInfo);
            }

            if(requiresEnd) {
                doc.End();
            }

            return doc;
        }

        private static XDoc AppendCommentXml(XDoc doc, CommentBE comment, string suffix, bool? includeParentInfo) {

            bool requiresEnd = false;
            string commentElement = string.IsNullOrEmpty(suffix) ? "comment" : "comment." + suffix;
            if(doc == null || doc.IsEmpty) {
                doc = new XDoc(commentElement);
            } else {
                doc.Start(commentElement);
                requiresEnd = true;
            }

            doc.Attr("id", comment.Id).Attr("href", CommentBL.GetUri(comment));

            //include parentinfo by default if the parent page is populated
            PageBE page = PageBL.GetPageById(comment.PageId);
            if(page != null && (includeParentInfo ?? true)) {
                doc.Add(PageBL.GetPageXml(page, "parent"));
            }

            UserBE posterUser = UserBL.GetUserById(comment.PosterUserId);
            if (posterUser != null) {
                doc.Add(UserBL.GetUserXml(posterUser, "createdby", Utils.ShowPrivateUserInfo(posterUser)));
            }
            doc.Start("date.posted").Value(comment.CreateDate).End();
            doc.Start("title").Value(comment.Title).End();
            doc.Start("number").Value(comment.Number).End();

            //Note (MaxM): Replytoid/replies not yet exposed
            //if (ReplyToId.HasValue) {
            //    doc.Start("comment.replyto")
            //        .Attr("number", ReplyToId.Value.ToString())
            //        .Attr("href", DekiContext.Current.ApiUri.At("pages", PageId.ToString(), "comments", ReplyToId.Value.ToString())).End();
            //}

            bool displayContent = true;

            //Only display content for nondeleted comments or for admins
            if(comment.IsCommentMarkedAsDeleted) {
                displayContent = PermissionsBL.IsUserAllowed(DekiContext.Current.User, Permissions.ADMIN);
            }

            if(displayContent) {
                doc.Start("content")
                    .Attr("type", comment.ContentMimeType)
                    .Attr("href", CommentBL.GetUri(comment).At("content"))
                    .Value(comment.Content).End();
            }

            if (comment.LastEditUserId.HasValue) {
                UserBE lastEditUser = UserBL.GetUserById(comment.LastEditUserId.Value);
                if (null != lastEditUser) {
                    doc.Add(UserBL.GetUserXml(lastEditUser, "editedby", Utils.ShowPrivateUserInfo(lastEditUser)));
                    doc.Start("date.edited").Value(comment.LastEditDate).End();
                }
            }

            if(comment.IsCommentMarkedAsDeleted && comment.DeleterUserId.HasValue) {
                UserBE deleteUser = UserBL.GetUserById(comment.DeleterUserId.Value);
                if (null != deleteUser) {
                    doc.Add(UserBL.GetUserXml(deleteUser, "deletedby", Utils.ShowPrivateUserInfo(deleteUser)));
                    doc.Start("date.deleted").Value(comment.DeleteDate).End();
                }
            }

            if(requiresEnd) {
                doc.End(); //comment
            }

            return doc;
        }

        #endregion
    }
}

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
using MindTouch.Deki.Data;
using MindTouch.Xml;

namespace MindTouch.Deki.Logic {
    public class RecentChangeEntry {

        //--- Fields ---
        protected ulong _id;
        protected string _comment;
        protected ulong _curId;
        protected ulong _lastOldId;
        protected ulong _thisOldId;
        protected NS _namespace;
        protected DateTime _timestamp;
        protected string _title;
        protected RC _type;
        protected NS _movedToNs;
        protected string _movedToTitle;
        protected string _username;
        protected string _fullname;
        protected bool _pageExists;
        protected int _revision;
        protected ulong? _cmntId;
        protected int? _cmntNumber;
        protected string _cmntContent;
        protected string _cmntMimetype;
        protected bool _cmntDeleted;
        protected bool _oldIsHidden;
        protected int? _editCount;
        protected int? _previousRevision;
        protected string _summary;
        protected int _currentRevision;
        public List<Tuplet<string/*username*/, string/*fullname*/, string/*comment*/>> ExtraComments;
        public List<KeyValuePair<string/*username*/, string/*fullname*/>> SortedAuthors;

        //--- Properties ---
        public virtual ulong Id {
            get { return _id; }
            set { _id = value; }
        }

        public virtual string Comment {
            get { return _comment; }
            set { _comment = value; }
        }

        public virtual ulong CurId {
            get { return _curId; }
            set { _curId = value; }
        }

        public virtual ulong LastOldId {
            get { return _lastOldId; }
            set { _lastOldId = value; }
        }

        public virtual ulong ThisOldId {
            get { return _thisOldId; }
            set { _thisOldId = value; }
        }

        public virtual NS Namespace {
            get { return _namespace; }
            set { _namespace = value; }
        }

        public virtual DateTime Timestamp {
            get { return _timestamp; }
            set { _timestamp = value; }
        }

        public virtual string Title {
            get { return _title; }
            set { _title = value; }
        }

        public virtual RC Type {
            get { return _type; }
            set { _type = value; }
        }

        public virtual NS MovedToNs {
            get { return _movedToNs; }
            set { _movedToNs = value; }
        }

        public virtual string MovedToTitle {
            get { return _movedToTitle; }
            set { _movedToTitle = value; }
        }

        public virtual string Username {
            get { return _username; }
            set { _username = value; }
        }

        public virtual string Fullname {
            get { return _fullname; }
            set { _fullname = value; }
        }

        public virtual bool PageExists {
            get { return _pageExists; }
            set { _pageExists = value; }
        }

        public virtual int Revision {
            get { return _revision; }
            set { _revision = value; }
        }

        public virtual ulong? CmntId {
            get { return _cmntId; }
            set { _cmntId = value; }
        }

        public virtual int? CmntNumber {
            get { return _cmntNumber; }
            set { _cmntNumber = value; }
        }

        public virtual string CmntContent {
            get { return _cmntContent; }
            set { _cmntContent = value; }
        }

        public virtual string CmntMimetype {
            get { return _cmntMimetype; }
            set { _cmntMimetype = value; }
        }

        public virtual bool CmntDeleted {
            get { return _cmntDeleted; }
            set { _cmntDeleted = value; }
        }

        public virtual bool OldIsHidden {
            get { return _oldIsHidden; }
            set { _oldIsHidden = value; }
        }

        public virtual int? EditCount {
            get { return _editCount; }
            set { _editCount = value; }
        }

        public virtual int? PreviousRevision {
            get { return _previousRevision; }
            set { _previousRevision = value; }
        }

        public virtual string Summary {
            get { return _summary; }
            set { _summary = value; }
        }

        public virtual int CurrentRevision {
            get { return _currentRevision; }
            set { _currentRevision = value; }
        }
    }

    public static class RecentChangeBL {

        //--- Class Methods ---
        public static void AppendXml(this RecentChangeEntry change, XDoc doc) {
            doc.Start("change");
            doc.Elem("rc_id", change.Id);
            if(change.ExtraComments != null) {
                foreach(var comment in change.ExtraComments) {
                    doc.Start("rc_comment")
                        .Attr("author", comment.Item1)
                        .Attr("fullname", comment.Item2)
                        .Value(comment.Item3)
                    .End();
                }
            } else {
                doc.Elem("rc_comment", change.Comment);
            }
            doc.Elem("rc_cur_id", change.CurId);
            doc.Elem("rc_last_oldid", change.LastOldId);
            doc.Elem("rc_this_oldid", change.ThisOldId);
            doc.Elem("rc_namespace", (int)change.Namespace);
            doc.Elem("rc_timestamp", DbUtils.ToString(change.Timestamp));
            doc.Elem("rc_title", change.Title);
            doc.Elem("rc_type", (int)change.Type);
            doc.Elem("rc_moved_to_ns", (int)change.MovedToNs);
            doc.Elem("rc_moved_to_title", change.MovedToTitle);
            if((change.SortedAuthors != null) && (change.SortedAuthors.Count > 1)) {
                foreach(var author in change.SortedAuthors) {
                    doc.Elem("rc_user_name", author.Key);
                    doc.Elem("rc_full_name", author.Value);
                }
            } else {
                doc.Elem("rc_user_name", change.Username);
                doc.Elem("rc_full_name", change.Fullname);
            }
            doc.Elem("rc_page_exists", change.PageExists ? 1 : 0);
            doc.Elem("rc_revision", change.Revision);
            doc.Elem("cmnt_id", change.CmntId);
            doc.Elem("cmnt_number", change.CmntNumber);
            doc.Elem("cmnt_content", change.CmntContent);
            doc.Elem("cmnt_content_mimetype", change.CmntMimetype);
            doc.Elem("cmnt_deleted", change.CmntDeleted ? 1 : 0);
            doc.Elem("old_is_hidden", change.OldIsHidden);
            doc.Elem("edit_count", change.EditCount);
            doc.Elem("rc_prev_revision", change.PreviousRevision);
            doc.Elem("rc_summary", change.Summary);
            doc.End();
        }

        public static RecentChangeEntry FromXml(XDoc doc) {
            var result = new RecentChangeEntry();
            result.CmntContent = doc["cmnt_content"].AsText;
            result.CmntDeleted = (doc["cmnt_deleted"].AsInt.GetValueOrDefault() != 0);
            result.CmntId = doc["cmnt_id"].AsULong;
            result.CmntMimetype = doc["cmnt_content_mimetype"].AsText;
            result.CmntNumber = doc["cmnt_number"].AsInt;
            result.Comment = doc["rc_comment"].AsText;
            result.CurId = doc["rc_cur_id"].AsULong.GetValueOrDefault();
            result.Fullname = doc["rc_full_name"].AsText;
            result.Id = doc["rc_id"].AsULong.GetValueOrDefault();
            result.LastOldId = doc["rc_last_oldid"].AsULong.Value;
            result.MovedToNs = (NS)doc["rc_moved_to_ns"].AsInt.Value;
            result.MovedToTitle = doc["rc_moved_to_title"].AsText;
            result.Namespace = (NS)doc["rc_namespace"].AsInt.Value;
            result.OldIsHidden = (doc["old_is_hidden"].AsInt.GetValueOrDefault() != 0);
            result.PageExists = doc["rc_page_exists"].AsInt.GetValueOrDefault() != 0;
            result.Revision = doc["rc_revision"].AsInt ?? 0;
            result.ThisOldId = doc["rc_this_oldid"].AsULong.Value;
            result.Timestamp = DbUtils.ToDateTime(doc["rc_timestamp"].AsText);
            result.Title = doc["rc_title"].AsText;
            result.Type = (RC)(doc["rc_type"].AsInt ?? 0);
            result.Username = doc["rc_user_name"].AsText ?? string.Empty;
            return result;
        }

        public static void AddPageMetaRecentChange(DateTime timestamp, PageBE title, UserBE user, DekiResource comment) {
            var resources = DekiContext.Current.Resources;
            DbUtils.CurrentSession.RecentChanges_Insert(timestamp, title, user, resources.Localize(comment), 0, RC.PAGEMETA, 0, String.Empty, false, 0);
        }

        public static void AddTagsRecentChange(DateTime timestamp, PageBE title, UserBE user, DekiResourceBuilder comment) {
            var resources = DekiContext.Current.Resources;
            DbUtils.CurrentSession.RecentChanges_Insert(timestamp, title, user, comment.Localize(resources), 0, RC.TAGS, 0, String.Empty, false, 0);
        }

        public static void AddGrantsAddedRecentChange(DateTime timestamp, PageBE title, UserBE user, DekiResourceBuilder comment) {
            var resources = DekiContext.Current.Resources;
            DbUtils.CurrentSession.RecentChanges_Insert(timestamp, title, user, comment.Localize(resources), 0, RC.GRANTS_ADDED, 0, String.Empty, false, 0);
        }

        public static void AddGrantsRemovedRecentChange(DateTime timestamp, PageBE title, UserBE user, DekiResourceBuilder comment) {
            var resources = DekiContext.Current.Resources;
            DbUtils.CurrentSession.RecentChanges_Insert(timestamp, title, user, comment.Localize(resources), 0, RC.GRANTS_REMOVED, 0, String.Empty, false, 0);
        }

        public static void AddGrantsRemovedRecentChange(DateTime timestamp, PageBE title, UserBE user, DekiResource comment) {
            var resources = DekiContext.Current.Resources;
            DbUtils.CurrentSession.RecentChanges_Insert(timestamp, title, user, resources.Localize(comment), 0, RC.GRANTS_REMOVED, 0, String.Empty, false, 0);
        }

        public static void AddRestrictionUpdatedChange(DateTime timestamp, PageBE title, UserBE user, DekiResource comment) {
            var resources = DekiContext.Current.Resources;
            DbUtils.CurrentSession.RecentChanges_Insert(timestamp, title, user, resources.Localize(comment), 0, RC.RESTRICTION_UPDATED, 0, String.Empty, false, 0);
        }

        public static void AddUserCreatedRecentChange(DateTime timestamp, PageBE title, UserBE user, DekiResource comment) {
            var resources = DekiContext.Current.Resources;
            DbUtils.CurrentSession.RecentChanges_Insert(timestamp, title, user, resources.Localize(comment), 0, RC.USER_CREATED, 0, String.Empty, false, 0);
        }

        public static void AddFileRecentChange(DateTime timestamp, PageBE title, UserBE user, DekiResource comment, uint transactionId) {
            var resources = DekiContext.Current.Resources;
            DbUtils.CurrentSession.RecentChanges_Insert(timestamp, title, user, resources.Localize(comment), 0, RC.FILE, 0, String.Empty, false, transactionId);
        }

        public static void AddNewPageRecentChange(DateTime timestamp, PageBE title, UserBE user, DekiResourceBuilder comment) {
            var resources = DekiContext.Current.Resources;
            DbUtils.CurrentSession.RecentChanges_Insert(timestamp, title, user, comment.Localize(resources), 0, RC.NEW, 0, String.Empty, false, 0);
        }

        public static void AddEditPageRecentChange(DateTime timestamp, PageBE title, UserBE user, DekiResourceBuilder comment, OldBE old) {
            var resources = DekiContext.Current.Resources;
            DbUtils.CurrentSession.RecentChanges_Insert(timestamp, title, user, comment.Localize(resources), old.ID, RC.EDIT, 0, String.Empty, false, 0);
        }

        public static void AddMovePageRecentChange(DateTime timestamp, PageBE oldTitle, Title newTitle, UserBE user, DekiResource comment, bool minorChange, uint transactionId) {
            var resources = DekiContext.Current.Resources;
            DbUtils.CurrentSession.RecentChanges_Insert(timestamp, oldTitle, user, resources.Localize(comment), 0, RC.MOVE, (uint)newTitle.Namespace, newTitle.AsUnprefixedDbPath(), minorChange, transactionId);
        }

        public static void AddDeletePageRecentChange(DateTime timestamp, PageBE page, UserBE user, DekiResource comment, bool minorChange, uint transactionId) {
            var resources = DekiContext.Current.Resources;
            DbUtils.CurrentSession.RecentChanges_Insert(timestamp, page, user, resources.Localize(comment), 0, RC.PAGEDELETED, 0, string.Empty, minorChange, transactionId);
        }

        public static void AddRestorePageRecentChange(DateTime timestamp, PageBE page, UserBE user, DekiResource comment, bool minorChange, uint transactionId) {
            var resources = DekiContext.Current.Resources;
            DbUtils.CurrentSession.RecentChanges_Insert(timestamp, page, user, resources.Localize(comment), 0, RC.PAGERESTORED, 0, string.Empty, minorChange, transactionId);
        }

        public static void AddCommentCreateRecentChange(DateTime timestamp, PageBE page, UserBE user, DekiResource summary, CommentBE comment) {
            AddCommentRecentChange(timestamp, page, user, summary, comment, RC.COMMENT_CREATE);
        }

        public static void AddCommentDeleteRecentChange(DateTime timestamp, PageBE page, UserBE user, DekiResource summary, CommentBE comment) {
            AddCommentRecentChange(timestamp, page, user, summary, comment, RC.COMMENT_DELETE);
        }

        public static void AddCommentUpdateRecentChange(DateTime timestamp, PageBE page, UserBE user, DekiResource summary, CommentBE comment) {
            AddCommentRecentChange(timestamp, page, user, summary, comment, RC.COMMENT_UPDATE);
        }

        private static void AddCommentRecentChange(DateTime timestamp, PageBE page, UserBE user, DekiResource summary, CommentBE comment, RC rcType) {
            var resources = DekiContext.Current.Resources;

            //TODO MaxM: Consider truncating summary
            DbUtils.CurrentSession.RecentChanges_Insert(timestamp, page, user, resources.Localize(summary), 0, rcType, 0, string.Empty, false, 0);
        }
    }
}

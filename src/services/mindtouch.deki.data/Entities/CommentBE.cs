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
using System.Text;
using MindTouch.Dream;

namespace MindTouch.Deki.Data {
    public class CommentBE {

        //--- Fields ---
        protected ulong _id;
        protected ulong _pageId;
        protected ushort _number;
        protected uint _posterUserId;
        protected DateTime _createDate;
        protected DateTime? _lastEditDate;
        protected uint? _lastEditUserId;
        protected string _content;
        protected string _contentMimeType;
        protected string _title;
        protected DateTime? _deleteDate;
        protected uint? _deleterUserId;

        //--- Properties ---
        public virtual ulong Id {
            get { return _id; }
            set { _id = value; }
        }

        public virtual ulong PageId {
            get { return _pageId; }
            set { _pageId = value; }
        }

        public virtual ushort Number {
            get { return _number; }
            set { _number = value; }
        }

        public virtual uint PosterUserId {
            get { return _posterUserId; }
            set { _posterUserId = value; }
        }

        public virtual DateTime CreateDate {
            get { return _createDate; }
            set { _createDate = new DateTime(value.Ticks, DateTimeKind.Utc); }
        }

        public virtual DateTime? LastEditDate {
            get { return _lastEditDate; }
            set { _lastEditDate = value.HasValue ? new DateTime(value.Value.Ticks, DateTimeKind.Utc) : (DateTime?)null; }
        }

        public virtual uint? LastEditUserId {
            get { return _lastEditUserId; }
            set { _lastEditUserId = value; }
        }

        public virtual string Content {
            get { return _content; }
            set { _content = value; }
        }
        public virtual string ContentMimeType {
            get { return _contentMimeType; }
            set { _contentMimeType = value; }
        }

        public virtual string Title {
            get { return _title; }
            set { _title = value; }
        }

        //Note (MaxM): Replytoid/replies not yet exposed        
        //        protected ulong? _replyToId;
        //        [DatabaseField(Name = "cmnt_replyto_id")]
        //        public ulong? ReplyToId {
        //            get { return _replyToId; }
        //            set { _replyToId = value; }
        //        }

        public virtual DateTime? DeleteDate {
            get { return _deleteDate; }
            set { _deleteDate = value.HasValue ? new DateTime(value.Value.Ticks, DateTimeKind.Utc) : (DateTime?)null; }
        }
        public virtual uint? DeleterUserId {
            get { return _deleterUserId; }
            set { _deleterUserId = value; }
        }

        public virtual bool IsCommentMarkedAsDeleted {
            get {
                return DeleteDate.GetValueOrDefault(DateTime.MinValue) > DateTime.MinValue || _deleterUserId.GetValueOrDefault(0) > 0;
            }
        }

        //--- Methods ---
        public virtual CommentBE Copy() {
            CommentBE comment = new CommentBE();
            comment.Content = Content;
            comment.ContentMimeType = ContentMimeType;
            comment.CreateDate = CreateDate;
            comment.DeleteDate = DeleteDate;
            comment.DeleterUserId = DeleterUserId;
            comment.Id = Id;
            comment.LastEditDate = LastEditDate;
            comment.LastEditUserId = LastEditUserId;
            comment.Number = Number;
            comment.PageId = PageId;
            comment.PosterUserId = PosterUserId;
            comment.Title = Title;
            return comment;
        }
    }
}

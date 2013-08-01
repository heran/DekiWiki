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

using MindTouch.Dream;
using MindTouch.Xml;

namespace MindTouch.Deki.Data {
    [Serializable]
    public class OldBE {

        //--- Constants ---
        private const string META_REVHIDE_USERID = "rev-hidden-user-id";
        private const string META_REVHIDE_TS = "rev-hidden-ts";
        private const string META_REVHIDE_COMMENT = "rev-hidden-comment";

        //--- Fields ---
        protected ulong _id;
        protected ulong _pageId;
        protected string _text;
        protected string _comment;
        protected uint _userId;
        protected DateTime _timestamp;
        protected bool _minorEdit;        
        protected string _contentType;
        protected ulong _rev;
        protected string _language;
        protected bool _hidden;
        protected string _metaStr;
        protected string _displayName;

        [NonSerialized]
        protected MetaXml _metaTempDoc;

        //--- Properties ---
        public virtual ulong ID {
            get { return _id; }
            set { _id = value; }
        }

        public virtual ulong PageID {
            get { return _pageId; }
            set { _pageId = value; }
        }

        public virtual string DisplayName {
            get { return _displayName; }
            set { _displayName = value; }
        }

        public virtual string Text {
            get { return _text; }
            set { _text = value; }
        }

        public virtual byte[] _Comment {
            get { return DbUtils.ToBlob(_comment); }
            set { _comment = DbUtils.ToString(value); }
        }
        public virtual string Comment {
            get { return _comment; }
            set { _comment = value; }
        }

        public virtual uint UserID {
            get { return _userId; }
            set { _userId = value; }
        }

        public virtual string _TimeStamp {
            get { return DbUtils.ToString(_timestamp); }
            set { _timestamp = DbUtils.ToDateTime(value); }
        }
        public virtual DateTime TimeStamp {
            get { return _timestamp; }
            set { _timestamp = value; }
        }

        public virtual bool MinorEdit {
            get { return _minorEdit; }
            set { _minorEdit = value; }
        }

        public virtual string ContentType {
            get { return _contentType; }
            set { _contentType = value ?? DekiMimeType.DEKI_TEXT; }
        }

        public virtual ulong Revision {
            get { return _rev; }
            set { _rev = value; }
        }

        public virtual string Language {
            get { return _language; }
            set { _language = value ?? DekiMimeType.DEKI_TEXT; }
        }

        public virtual bool IsHidden {
            get { return _hidden; }
            set { _hidden = value; }
        }

        public virtual string Meta {
            get {
                if(_metaTempDoc != null) {
                    _metaStr = _metaTempDoc.ToString();
                }
                return _metaStr;
            }
            set {
                _metaStr = value;
                _metaTempDoc = null;
            }
        }

        public virtual MetaXml MetaXml {
            get {
                if(_metaTempDoc == null) {
                    if(string.IsNullOrEmpty(_metaStr)) {
                        _metaTempDoc = new MetaXml();
                    } else {
                        _metaTempDoc = new MetaXml(XDocFactory.From(_metaStr, MimeType.XML));
                    }
                }
                return _metaTempDoc;
            }
        }

        //--- Methods ---
        public virtual OldBE Copy() {
            OldBE old = new OldBE();
            old._Comment = _Comment;
            old.DisplayName = DisplayName;
            old._TimeStamp = _TimeStamp;
            old._pageId = _pageId;
            old.ContentType = ContentType;
            old.ID = ID;
            old.IsHidden = IsHidden;
            old.Language = Language;
            old.Meta = Meta;
            old.MinorEdit = MinorEdit;
            old.Revision = Revision;
            old.Text = Text;
            old.UserID = UserID;
            return old;
        }
    }
}

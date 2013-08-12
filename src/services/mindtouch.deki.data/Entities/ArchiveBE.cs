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

    public class ArchiveBE {

        //--- Fields ---
        protected uint _id;
        protected Title _title = null;
        protected string _text;
        protected string _comment;
        protected uint _userId;
        protected DateTime _timestamp;
        protected bool _minorEdit;
        protected ulong _lastPageId;
        protected ulong _oldId;
        protected string _contentType;
        protected string _language;
        protected uint _transactionId;
        protected bool _isHidden;
        protected string _metaStr;
        protected ulong _revision;
        protected MetaXml _metaTempDoc;

        //--- Properties ---
        public virtual uint Id {
            get { return _id; }
            set { _id = value; }
        }

        public virtual ushort _Namespace {
            get { return (ushort)Title.Namespace; }
            set { Title.Namespace = (NS)value; }
        }

        public virtual string _Title {
            get { return Title.AsUnprefixedDbPath(); }
            set { Title.Path = value; }
        }

        public virtual string _DisplayName {
            get { return Title.DisplayName; }
            set { Title.DisplayName = value; }
        }

        public virtual Title Title {
            get {
                if (null == _title) {
                    _title = Title.FromDbPath(NS.UNKNOWN, String.Empty, null);
                }
                return _title;
            }
            set {
                _title = value;
            }
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

        public virtual ulong LastPageId {
            get { return _lastPageId; }
            set { _lastPageId = value; }
        }

        public virtual ulong OldId {
            get { return _oldId; }
            set { _oldId = value; }
        }

        public virtual string ContentType {
            get { return _contentType ?? DekiMimeType.DEKI_TEXT; }
            set { _contentType = value; }
        }

        public virtual string Language {
            get { return _language; }
            set { _language = value; }
        }

        public virtual uint TransactionId {
            get { return _transactionId; }
            set { _transactionId = value; }
        }

        public virtual bool IsHidden {
            get { return _isHidden; }
            set { _isHidden = value; }
        }

        public virtual ulong Revision {
            get { return _revision; }
            set { _revision = value; }
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
        public virtual ArchiveBE Copy() {
            ArchiveBE archive = new ArchiveBE();
            archive._Comment = _Comment;
            archive._DisplayName = _DisplayName;
            archive._Namespace = _Namespace;
            archive._Title = _Title;
            archive._TimeStamp = _TimeStamp;
            archive.ContentType = ContentType;
            archive.Id = Id;
            archive.IsHidden = IsHidden;
            archive.Language = Language;
            archive.LastPageId = LastPageId;
            archive.Meta = Meta;
            archive.MinorEdit = MinorEdit;
            archive.OldId = OldId;
            archive.Text = Text;
            archive.TransactionId = TransactionId;
            archive.UserID = UserID;
            archive.Revision = Revision;
            return archive;
        }
    }
}
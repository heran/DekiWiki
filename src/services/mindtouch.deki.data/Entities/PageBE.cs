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
using System.Text.RegularExpressions;

using MindTouch.Dream;
using MindTouch.Xml;

namespace MindTouch.Deki.Data {

    [Serializable]
    public class PageBE {

        //--- Fields ---
        protected ulong _id;
        protected Title _title;
        protected string _text;
        protected int _textLength = -1;
        protected string _comment;
        protected uint _userId;
        protected DateTime _timestamp;
        protected bool _isRedirect;
        protected PageBE _redirectedFrom;
        protected bool _minorEdit;
        protected bool _isNew;
        protected DateTime _touched;
        protected bool _useCache;
        protected string _tip;
        protected ulong _parent;
        protected uint _restrictionId;
        protected string _contentType;
        protected string _language;
        protected bool _isHidden;
        protected string _metaStr;
        protected string _etag;
        protected PageBE _parentPage;
        protected uint? _attachmentCount;
        protected ulong[] _childPageIds;
        protected PageBE[] _childPages;
        protected ulong _revision;

        [NonSerialized]
        protected MetaXml _metaTempDoc;

        //--- Properties ---
        public virtual ulong ID {
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
                if(null == _title) {
                    _title = Title.FromDbPath(NS.UNKNOWN, String.Empty, null);
                }
                return _title;
            }
            set {
                _title = value;
            }
        }

        // this is used to set the page title programmatically when a page is rendered
        public string CustomTitle;

        public virtual int TextLength {
            get { return _textLength; }
            set { _textLength = value; }
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

        public virtual bool IsRedirect {
            get { return _isRedirect; }
            set { _isRedirect = value; }
        }

        public virtual PageBE RedirectedFrom {
            get { return _redirectedFrom; }
            set { _redirectedFrom = value; }
        }

        public virtual bool MinorEdit {
            get { return _minorEdit; }
            set { _minorEdit = value; }
        }

        public virtual bool IsNew {
            get { return _isNew; }
            set { _isNew = value; }
        }

        public virtual string _Touched {
            get { return DbUtils.ToString(_touched); }
            set { _touched = DbUtils.ToDateTime(value); }
        }
        public virtual DateTime Touched {
            get { return _touched; }
            set { _touched = value; }
        }

        public virtual bool UseCache {
            get { return _useCache; }
            set { _useCache = value; }
        }

        public virtual string TIP {
            get { return _tip; }
            set { _tip = value; }
        }

        public virtual ulong ParentID {
            get { return _parent; }
            set { _parent = value; }
        }

        public virtual uint RestrictionID {
            get { return _restrictionId; }
            set { _restrictionId = value; }
        }

        public virtual string ContentType {
            get { return _contentType; }
            set { _contentType = value; }
        }

        public virtual string Language {
            get { return _language; }
            set { _language = value; }
        }

        public virtual bool IsHidden {
            get { return _isHidden; }
            set { _isHidden = value; }
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

        // TODO (brigettek) : This is a delay populated field
        public virtual uint? AttachmentCount {
            get { return _attachmentCount; }
            set { _attachmentCount = value; }
        }

        // TODO (brigettek) : This is a delay populated field
        public virtual ulong[] ChildPageIds {
            get { return _childPageIds; }
            set { _childPageIds = value; }
        }

        // TODO (brigettek) : This is a delay populated field
        public virtual PageBE[] ChildPages {
            get { return _childPages; }
            set { _childPages = value; }
        }

        public virtual ulong Revision {
            get { return _revision; }
            set { _revision = value; }
        }

        public virtual bool IsTextPopulated {
            get { return _text != null; }
        }

        public virtual string Etag {
            get { return _etag; }
            set { _etag = value; }
        }

        //--- Methods ---

        // TODO (brigettek): page text needs to be stored separately from the page
        public string GetText(IDekiDataSession session) {

            //page_text is lazy loaded if it doesn't exist.
            if (_text == null) {
                if (ID != 0) {
                    PageTextContainer pageTextContainer = session.Pages_GetContents(new List<ulong>() {ID}).FirstOrDefault();
                    if (null != pageTextContainer) {

                        // TODO (brigettek): Do we still need to go to the old table?
                        if (pageTextContainer.TimeStamp == TimeStamp) {
                            _text = pageTextContainer.Text;
                        } else {
                            OldBE oldPage = session.Old_GetOldByTimestamp(ID, TimeStamp);
                            if (oldPage != null)
                                _text = oldPage.Text;
                            else
                                throw new OldIdNotFoundException(ID, TimeStamp);
                        }
                    }
                } else {
                    _text = string.Empty;
                }
            }
            return _text;
        }

        public void SetText(string text) {
            _text = text;
            _textLength = text == null ? 0 : text.Length;;
            if (_text != null) {
                _text = _text.Replace("<!-- Tidy found serious XHTML errors:  -->", "");
            }
        }

        public virtual PageBE Copy(PageBE to) {            
            to._Comment = _Comment;
            to._DisplayName = _DisplayName;
            to._Namespace = _Namespace;
            to._TimeStamp = _TimeStamp;
            to._Title = _Title;
            to._Touched = _Touched;
            to.ContentType = ContentType;
            to.ID = ID;
            to.IsNew = IsNew;
            to.IsHidden = IsHidden;
            to.IsRedirect = IsRedirect;
            to.Language = Language;
            to.Meta = Meta;
            to.MinorEdit = MinorEdit;
            to.ParentID = ParentID;
            to.RestrictionID = RestrictionID;
            to.Revision = Revision;
            to.TextLength = TextLength;
            to.TIP = TIP;
            to.UseCache = UseCache;
            to.UserID = UserID;
            to.Etag = Etag;
            return to;
        }

        public virtual PageBE Copy() {
            return Copy(new PageBE());
        }
    }
}

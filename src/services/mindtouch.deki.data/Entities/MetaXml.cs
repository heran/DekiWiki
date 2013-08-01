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
using MindTouch.Xml;

namespace MindTouch.Deki.Data {
    public class MetaXml : XDoc {

        //--- Constants ---
        private const string ATTRIBUTE_ROOT = "attr";
        private const string META_REVHIDE_USERID = "rev-hidden-user-id";
        private const string META_REVHIDE_TS = "rev-hidden-ts";
        private const string META_REVHIDE_COMMENT = "rev-hidden-comment";
        private const string META_IMAGEWIDTH = "width";
        private const string META_IMAGEHEIGHT = "height";
        private const string META_IMAGEFRAMES = "frames";
        private const string META_FILEID = "fileid";
        
        //--- Constructors --
        public MetaXml() : base(ATTRIBUTE_ROOT) { }

        public MetaXml(XDoc doc) : base(doc) { }

        //--- Properties ---
        public int? ImageWidth {
            get {
                return this[META_IMAGEWIDTH].AsInt;
            }
            set {
                SetMetaValue(META_IMAGEWIDTH, value);
            }
        }

        public int? ImageHeight {
            get {
                return this[META_IMAGEHEIGHT].AsInt;
            }
            set {
                SetMetaValue(META_IMAGEHEIGHT, value);
            }
        }

        public int? ImageFrames {
            get {
                return this[META_IMAGEFRAMES].AsInt;
            }
            set {
                SetMetaValue(META_IMAGEFRAMES, value);
            }
        }

        public uint? FileId {
            get {
                return this[META_FILEID].AsUInt;
            }
            set {
                SetMetaValue(META_FILEID, value);
            }
        }

        public uint? RevisionHiddenUserId {
            get {
                return this[META_REVHIDE_USERID].AsUInt;
            }
            set {
                SetMetaValue(META_REVHIDE_USERID, value);
            }
        }

        public string RevisionHiddenComment {
            get {
                return this[META_REVHIDE_COMMENT].AsText;
            }
            set {
                SetMetaValue(META_REVHIDE_COMMENT, value);
            }
        }

        public DateTime? RevisionHiddenTimestamp {
            get {
                return this [META_REVHIDE_TS].AsDate;
            }
            set {
                SetMetaValue(META_REVHIDE_TS, value);
            }
        }

        //--- Methods ---
        private void SetMetaValue<T>(string name, T? value) where T : struct {
            var entry = this[name];
            if(value == null) {
                entry.Remove();
            } else {
                if(entry.IsEmpty) {
                    this.Elem(name, value.Value);
                } else {
                    entry.ReplaceValue(value.Value);
                }
            }
        }

        private void SetMetaValue(string name, string value) {
            var entry = this[name];
            if(value == null) {
                entry.Remove();
            } else {
                if(entry.IsEmpty) {
                    this.Elem(name, value);
                } else {
                    entry.ReplaceValue(value);
                }
            }
        }
    }
}
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
    public class ResourceBE {

        //--- Constants ---
        public const int HEADREVISION = 0; //Used for lookups for latest revision of a file
        public const int TAILREVISION = 1; //Starting revision number for a file

        //--- Types ---
        [Flags]
        public enum ChangeOperations : ushort {
            UNDEFINED = 0,
            CONTENT = 1,    //The content was created or updated
            NAME = 2,       //The name of the resource was changed
            LANGUAGE = 4,   //Language got set or updated
            META = 8,       //The meta attributes of the resource were updates
            DELETEFLAG = 16,//The resource delete flag was changed. Resource is either deleted or restored
            PARENT = 32     //The resource moved to a different parent
        }

        public enum ParentType : byte {
            PAGE = 1,
            FILE = 2,
            USER = 3,
            SITE = 5
        }

        public enum Type : byte {
            FILE = 2,
            PROPERTY = 4
        };

        //--- Fields ---
        private string _metaStr;
        private MetaXml _metaTempDoc;

        //--- Constructors ---
        public ResourceBE(Type type) {
            ResourceType = type;
            Content = new ResourceContentBE(true);
        }

        public ResourceBE(ResourceBE sourceRev) : this(sourceRev.ResourceType) {
            ResourceId = sourceRev.ResourceId;
            Revision = sourceRev.Revision;
            ChangeDescription = sourceRev.ChangeDescription;
            Name = sourceRev.Name;
            Timestamp = sourceRev.Timestamp;
            ChangeMask = sourceRev.ChangeMask;
            UserId = sourceRev.UserId;
            ChangeSetId = sourceRev.ChangeSetId;
            Deleted = sourceRev.Deleted;
            ParentId = sourceRev.ParentId;
            ParentPageId = sourceRev.ParentPageId;
            ParentUserId = sourceRev.ParentUserId;
            ChildResources = sourceRev.ChildResources;
            
            // revision contents
            Size = sourceRev.Size;
            MimeType = sourceRev.MimeType;
            Content = sourceRev.Content;
            ContentId = sourceRev.ContentId;
            Meta = sourceRev.Meta;

            // resource info
            ResourceIsDeleted = sourceRev.ResourceIsDeleted;
            ResourceUpdateUserId = sourceRev.ResourceUpdateUserId;
            ResourceUpdateTimestamp = sourceRev.ResourceUpdateTimestamp;
            ResourceHeadRevision = sourceRev.ResourceHeadRevision;
            ResourceCreateUserId = sourceRev.ResourceCreateUserId;
            ResourceCreateTimestamp = sourceRev.ResourceCreateTimestamp;
        }

        //--- Properties ---

        // Resource Properties
        public Type ResourceType { get; private set; }
        public uint ResourceId { get; set; }
        public int ResourceHeadRevision { get; set; }
        public bool ResourceIsDeleted { get; set; }
        public uint ResourceCreateUserId { get; set; }
        public uint ResourceUpdateUserId { get; set; }
        public DateTime ResourceCreateTimestamp { get; set; }
        public DateTime ResourceUpdateTimestamp { get; set; }

        // Resource Revision Properies
        public int Revision { get; set; }
        public uint UserId { get; set; }
        public ChangeOperations ChangeMask { get; set; }
        public string Name { get; set; }
        public string ChangeDescription { get; set; }
        public DateTime Timestamp { get; set; }
        public uint? ChangeSetId { get; set; }
        public bool Deleted { get; set; }
        public uint Size { get; set; }
        public MimeType MimeType { get; set; }
        public string Language { get; set; }
        public bool IsHidden { get; set; }
        public uint ContentId { get; set; }
        public ResourceContentBE Content { get; set; }
        public uint? ParentId { get; set; }
        public ulong? ParentPageId { get; set; }
        public uint? ParentUserId { get; set; }
        public ResourceBE[] ChildResources { get; set; }

        public string Meta {
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

        public MetaXml MetaXml {
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
        public bool IsHeadRevision() {
            if(Revision > ResourceHeadRevision) {
                throw new ResourceRevisionOutOfRangeException(ToString());
            }
            return ResourceHeadRevision == Revision;
        }

        public void AssertHeadRevision() {
            if(!IsHeadRevision()) {
                throw new ResourceExpectedHeadException(ResourceHeadRevision, Revision);
            }
        }

        public bool IsNewResource() {
            return ResourceId == 0;
        }

        public virtual string ETag() {
            string etag = string.Format("{0}.r{1}_ts{2}", ResourceId, Revision, Timestamp.ToString(XDoc.RFC_DATETIME_FORMAT));
            return etag;
        }

        public override string ToString() {
            string s = string.Format("Res ID:{0} Rev:{1} HeadRev:{2} Type:{3} Name:{4} MimeType:{5} Size:{6}", ResourceId, Revision, ResourceHeadRevision, ResourceType, Name, MimeType, Size);
            return s;
        }

        public string FilenameExtension {
            get {
                if(ResourceType != Type.FILE) {
                    throw new InvalidOperationException("invalid operation for resource type");
                }
                return System.IO.Path.GetExtension(Name).TrimStart('.');
            }
        }

        public string FilenameWithoutExtension {
            get {
                if(ResourceType != Type.FILE) {
                    throw new InvalidOperationException("invalid operation for resource type");
                }
                return FilenameExtension.Length == 0 ? Name : Name.Substring(0, Name.Length - FilenameExtension.Length - 1);
            }
        }

        public XUri PropertyUri(XUri parentUri) {
            if(ResourceType != Type.PROPERTY) {
                throw new InvalidOperationException("invalid operation for resource type");
            }
            return parentUri.At("properties", XUri.DoubleEncodeSegment(Name));
        }

        public XUri PropertyInfoUri(XUri parentUri) {
            if(ResourceType != Type.PROPERTY) {
                throw new InvalidOperationException("invalid operation for resource type");
            }
            return PropertyUri(parentUri).At("info");
        }

        public XUri PropertyContentUri(XUri parentUri) {
            if(ResourceType != Type.PROPERTY) {
                throw new InvalidOperationException("invalid operation for resource type");
            }
            return PropertyUri(parentUri);
        }
    }
}

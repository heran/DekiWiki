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
using System.IO;

using MindTouch.Dream;

namespace MindTouch.Deki {

    public enum TagType { ALL = -1, TEXT = 0, DATE = 1, USER = 2, DEFINE = 3 };

    public enum DekiInstanceStatus : byte {

        /// <summary>
        /// instance has been created but not yet initialized
        /// </summary>
        CREATED,

        /// <summary>
        /// instance is initializing
        /// </summary>
        INITIALIZING,

        /// <summary>
        /// instance is serving requests, but some services may not be ready yet
        /// </summary>
        STARTING_SERVICES,

        /// <summary>
        /// instance has been initialized and is ready to serve requests
        /// </summary>
        RUNNING,

        /// <summary>
        /// instance is in the process of being shut down
        /// </summary>
        STOPPING,

        /// <summary>
        /// instance has been shut down
        /// </summary>
        STOPPED,

        /// <summary>
        /// instance has failed to initialize and will reject requests
        /// </summary>
        ABANDONED
    }

    public enum RC : uint {

        // TODO (steveb): add rc_type for file restore, upload, delete, move, description, wipe

        // page related changes
        EDIT = 0,
        NEW = 1,
        MOVE = 2,
        LOG = 3,                // NOTE (steveb): not used, maintained for backwards compatibility
        MOVE_OVER_REDIRECT = 4, // NOTE (steveb): not used, maintained for backwards compatibility
        PAGEDELETED = 5,
        PAGERESTORED = 6,

        // 4x : comment related changes
        COMMENT_CREATE = 40,
        COMMENT_UPDATE = 41,
        COMMENT_DELETE = 42,

        // 5x: misc. changes
        FILE = 50,
        PAGEMETA = 51,          // NOTE (steveb): only used to track page language changes since 9.02
        TAGS = 52,
        GRANTS_ADDED = 54,
        GRANTS_REMOVED = 55,
        RESTRICTION_UPDATED = 56,

        // 6x : user related changes
        USER_CREATED = 60,
    }

    public enum FeedFormat {
        ATOM_DAILY,
        RAW,
        RAW_DAILY,
        ATOM_ALL,
        DAILY_ACTIVITY
    }

    public enum RatioType {
        UNDEFINED,
        FIXED,
        VARIABLE
    };

    public enum SizeType {
        UNDEFINED,
        ORIGINAL,
        THUMB,
        WEBVIEW,
        BESTFIT,
        CUSTOM
    };

    public enum FormatType {
        UNDEFINED,
        JPG,
        PNG,
        BMP,
        GIF
    };

    public enum GrantType : byte {
        GROUP,
        USER,
        UNDEFINED
    };

    public enum RoleType : byte {
        ROLE,
        RESTRICTION,
        UNDEFINED
    };

    public enum ServiceType {
        AUTH,
        EXT,
        UNDEFINED
    };

    public enum ParserMode {
        EDIT,
        RAW,
        VIEW,
        VIEW_NO_EXECUTE,
        SAVE
    }

    [Flags]
    public enum Permissions : ulong {
        NONE = 0,
        LOGIN = 1,        // able to log in
        BROWSE = 2,        // can see page title in navigation
        READ = 4,        // Can see page and attachment contents
        SUBSCRIBE = 8,        // subscribe to page changes/rss
        UPDATE = 16,       // Can edit an existing page and work with attachments
        CREATE = 32,       // create new page
        DELETE = 256,      // delete a file or page
        CHANGEPERMISSIONS = 1024,     // change page permissions/grants
        CONTROLPANEL = 2048,     // can access the control panel
        UNSAFECONTENT = 4096,     // can write unsafe content (e.g. <script>, <embed>, <form>, etc.)
        ADMIN = 0x8000000000000000UL
    }

    public static class PermissionSets {

        //--- Constants ---
        public const Permissions PAGE_INDEPENDENT = Permissions.ADMIN | Permissions.CONTROLPANEL | Permissions.LOGIN;
        public const Permissions INVALID_LICENSE_REVOKE_LIST = Permissions.LOGIN;
        public const Permissions MINIMAL_ANONYMOUS_PERMISSIONS = Permissions.LOGIN;
        public const Permissions ALL = (Permissions)0xFFFFFFFFFFFFFFFFUL;
    }

    [Serializable]
    public struct PermissionStruct {
        public PermissionStruct(Permissions userPermissions, Permissions pageRestrictionsMask, Permissions pageGrantPermissions) {
            UserPermissions = userPermissions;
            PageRestrictionsMask = pageRestrictionsMask;
            PageGrantPermissions = pageGrantPermissions;
        }

        public PermissionStruct(ulong userPermissions, ulong pageRestrictionsMask, ulong pageGrantPermissions) {
            UserPermissions = (Permissions)userPermissions;
            PageRestrictionsMask = (Permissions)pageRestrictionsMask;
            PageGrantPermissions = (Permissions)pageGrantPermissions;
        }

        public Permissions UserPermissions;
        public Permissions PageRestrictionsMask;
        public Permissions PageGrantPermissions;
    }

    public enum CascadeType : byte {
        NONE,   //Permissions are not cascaded to child pages
        DELTA, //Changes between given page's security and proposed security cascaded to child nodes
        ABSOLUTE //Proposed security is set on child pages
    }

    public class DekiMimeType {

        //--- Constants ---
        public const string MEDIAWIKI_TEXT = "text/x.mediawiki";
        public const string DEKI_TEXT = "application/x.deki-text";
        public const string DEKI_XML0702 = "application/x.deki0702+xml";
        public const string DEKI_XML0805 = "application/x.deki0805+xml";
        public const string HTML_TEXT = "text/html";
    }

    public class TagPrefix {

        //--- Constants ---
        public static readonly string USER = "@";
        public static readonly string DEFINE = "define:";
        public static readonly string DATE = "date:";
        public static readonly string TEXT = String.Empty;
    }

    public class Role {

        // TODO (MaxM): These names should never be hardcoded
        // BUGBUGBUG (steveb): roles must be localizable
        public const string CONTRIBUTOR = "Contributor";
    }

    public class ConfigValue {

        //--- Fields ---
        public readonly string Value;
        public bool IsReadOnly;
        public bool IsHidden;

        //--- Constructors --- 
        public ConfigValue(string value) : this(value, false, false) { }

        public ConfigValue(string value, bool readOnly, bool hidden) {
            this.Value = value;
            this.IsReadOnly = readOnly;
            this.IsHidden = hidden;
        }

        //--- Methods ---
        public override string ToString() {
            return Value;
        }
    }

    public enum CommentFilter {
        ANY = 0,
        DELETED = 1,
        NONDELETED = 2
    };

    public enum DeletionFilter : int {
        ANY = -1,
        DELETEDONLY = 1,
        ACTIVEONLY = 0
    };

    public enum SortDirection : byte {
        UNDEFINED,
        ASC,
        DESC
    };

    public enum GroupsSortField {
        UNDEFINED,
        ID,
        NAME,
        ROLE,
        SERVICE
    }

    public enum ServicesSortField {
        UNDEFINED,
        DESCRIPTION,
        ID,
        INIT,
        LOCAL,
        SID,
        TYPE,
        URI
    }

    public enum UsersSortField {
        UNDEFINED,
        DATE_CREATED,
        EMAIL,
        FULLNAME,
        ID,
        DATE_LASTLOGIN,
        NICK,
        ROLE,
        SERVICE,
        STATUS,
        USERNAME
    }

    public class SetDiscriminator {

        //--- Fields ---
        public uint Offset;
        public uint Limit;
        public bool Ascending = true;
        public string SortField;

        //--- Properties ---
        public string SortBy {
            get {
                return Ascending || string.IsNullOrEmpty(SortField) ? SortField : "-" + SortField;
            }
        }

        //--- Methods ---
        public void SetSortInfo(string sortBy, string defaultSortBy) {
            if(string.IsNullOrEmpty(defaultSortBy)) {
                throw new ArgumentNullException("defaultSortBy");
            }
            sortBy = sortBy.IfNullOrEmpty(defaultSortBy);
            if(sortBy.StartsWith("-")) {
                Ascending = false;
                SortField = sortBy.Substring(1);
            } else {
                Ascending = true;
                SortField = sortBy;
            }
        }
    }

    [Serializable]
    public class ResourceIdMapping {
        public readonly uint? ResourceId;
        public readonly uint? FileId;
        public readonly uint? PageId;

        public ResourceIdMapping(uint? resourceId, uint? fileId, uint? pageId) {
            this.ResourceId = resourceId;
            this.FileId = fileId;
            this.PageId = pageId;
        }

        public ResourceIdMapping Copy() {
            return new ResourceIdMapping(ResourceId, FileId, PageId);
        }
    }

    public class StreamInfo : IDisposable {

        //--- Fields ---
        public readonly Stream Stream;
        public readonly long Length;
        public readonly MimeType Type;
        public readonly DateTime? Modified;
        public readonly XUri Uri;

        //--- Constructors ---
        public StreamInfo(Stream stream, long size) : this(stream, size, null, null) { }
        public StreamInfo(Stream stream, long size, MimeType type) : this(stream, size, type, null) { }

        public StreamInfo(Stream stream, long size, MimeType type, DateTime? modified) {
            if(stream == null) {
                throw new ArgumentNullException("stream");
            }
            this.Stream = stream;
            this.Length = size;
            this.Type = type ?? MimeType.BINARY;
            this.Modified = modified;
        }

        public StreamInfo(XUri uri) {
            this.Uri = uri;
        }

        //--- Methods ---
        public void Close() {
            if(Stream != null) {
                Stream.Close();
            }
        }

        public void Dispose() {
            Close();
        }
    }

    [Serializable]
    public class PageTextContainer {

        //--- Fields ---
        public readonly ulong PageId;
        public readonly string Text;
        public readonly DateTime TimeStamp;

        //--- Constructors ---
        public PageTextContainer(ulong pageId, string text, DateTime timestamp) {
            this.PageId = pageId;
            this.Text = text;
            this.TimeStamp = timestamp;
        }
    }

    [Serializable]
    public class UserPagePermissionContainer {

        //--- Fields ---
        public readonly PermissionStruct Permission;
        public readonly uint UserId;
        public readonly ulong PageId;

        //--- Constructors ---

        public UserPagePermissionContainer(uint userId, ulong pageId) {
            this.UserId = userId;
            this.PageId = pageId;
        }

        public UserPagePermissionContainer(uint userId, ulong pageId, PermissionStruct Permission) {
            this.UserId = userId;
            this.PageId = pageId;
            this.Permission = Permission;
        }
    }

    [Serializable]
    public class UserMetrics {
        public uint CommentPosts { get; set; }
        public uint UpRatings { get; set; }
        public uint DownRatings { get; set; }
        public uint PagesCreated { get; set; }
        public uint PagesChanged { get; set; }
        public uint FilesUploaded { get; set; }
    }
}

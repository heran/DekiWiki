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
using System.Text.RegularExpressions;

using MindTouch.Dream;
using MindTouch.Xml;

namespace MindTouch.Deki {

    /// <summary>
    ///  Identifies a title by namespace, topic name, and filename
    /// </summary>
    [Serializable]
    public class Title : IEquatable<Title>, IComparable<Title> {

        //--- Class fields ---
        public const String INDEX_PHP_TITLE = "index.php?title=";
        private static readonly Regex INVALID_TITLE_REGEX = new Regex(@"^\/|^\.\.$|^\.$|^\./|^\.\./|/\./|/\.\./|/\.$|/\..$|\/$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        private static readonly Regex SEPARATOR_REGEX = new Regex(@"(?<=[^/])/(?=[^/])", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        private static readonly Regex INDEX_PHP_REGEX = new Regex(@"/?index\.php\?title=", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        private static readonly char[] INDEX_PHP_CHARS = new[] { '%', '&', '+' };
        private static readonly IDictionary<NS, string> _namespaceNames;
        private static readonly IDictionary<string, NS> _namespaceValues;
        private static readonly IDictionary<NS, NS> _frontToTalkMap;
        private static readonly IDictionary<NS, NS> _talkToFrontMap;
        private static readonly char[] TRIM_CHARS = new[] { '/', '_', ' ', '\t', '\r', '\n', '\f', '\v', '\x00A0' };

        //--- Class constructor ---
        static Title() {

            // populate the namespace type to name mapping
            _namespaceNames = new Dictionary<NS, string>();
            _namespaceNames[NS.MAIN] = String.Empty;
            _namespaceNames[NS.MAIN_TALK] = "Talk";
            _namespaceNames[NS.USER] = "User";
            _namespaceNames[NS.USER_TALK] = "User_talk";
            _namespaceNames[NS.PROJECT] = "Project";
            _namespaceNames[NS.PROJECT_TALK] = "Project_talk";
            _namespaceNames[NS.TEMPLATE] = "Template";
            _namespaceNames[NS.TEMPLATE_TALK] = "Template_talk";
            _namespaceNames[NS.HELP] = "Help";
            _namespaceNames[NS.HELP_TALK] = "Help_talk";
            _namespaceNames[NS.ATTACHMENT] = "File";
            _namespaceNames[NS.SPECIAL] = "Special";
            _namespaceNames[NS.SPECIAL_TALK] = "Special_talk";
            _namespaceNames[NS.ADMIN] = "Admin";

            // populate the namespace name to type mapping
            _namespaceValues = new Dictionary<string, NS>();
            foreach(KeyValuePair<NS, string> ns in _namespaceNames) {
                _namespaceValues[ns.Value.ToLowerInvariant()] = ns.Key;
            }

            // populate front to talk mapping
            _frontToTalkMap = new Dictionary<NS, NS>();
            _frontToTalkMap[NS.MAIN] = NS.MAIN_TALK;
            _frontToTalkMap[NS.USER] = NS.USER_TALK;
            _frontToTalkMap[NS.PROJECT] = NS.PROJECT_TALK;
            _frontToTalkMap[NS.TEMPLATE] = NS.TEMPLATE_TALK;
            _frontToTalkMap[NS.HELP] = NS.HELP_TALK;
            _frontToTalkMap[NS.SPECIAL] = NS.SPECIAL_TALK;

            // populate talk to front mapping
            _talkToFrontMap = new Dictionary<NS, NS>();
            foreach(KeyValuePair<NS, NS> ns in _frontToTalkMap) {
                _talkToFrontMap[ns.Value] = ns.Key;
            }
        }


        //--- Class methods ---

        /// <summary>
        /// Constructs a title object from a page namespace and database path
        /// </summary>
        public static Title FromDbPath(NS ns, string dbPath, string displayName) {
            return FromDbPath(ns, dbPath, displayName, String.Empty);
        }

        /// <summary>
        /// Constructs a title object from a page namespace, database path, and filename
        /// </summary>
        public static Title FromDbPath(NS ns, string dbPath, string displayName, string filename) {
            return new Title(ns, dbPath, displayName, filename, null, null);
        }

        /// <summary>
        /// Constructs a title object from a page namespace, database path, filename, anchor, and query
        /// </summary>
        public static Title FromDbPath(NS ns, string dbPath, string displayName, string filename, string anchor, string query) {
            return new Title(ns, dbPath, displayName, filename, anchor, query);
        }

        /// <summary>
        /// Contructs a title object from a prefixed database path
        /// Ex.  User:Admin/MyPage
        /// </summary>
        public static Title FromPrefixedDbPath(string prefixedDbPath, string displayName) {
            NS ns;
            string path;
            StringToNSAndPath(null, prefixedDbPath, out ns, out path);
            return FromDbPath(ns, path, displayName, String.Empty);
        }

        /// <summary>
        /// Constructs a title object from a xdoc
        /// </summary>
        public static Title FromXDoc(XDoc node, string nsFieldName, string titleFieldName, string displayFieldName) {
            return FromDbPath((NS)(node[nsFieldName].AsInt ?? 0), string.IsNullOrEmpty(titleFieldName) ? null : node[titleFieldName].AsText, string.IsNullOrEmpty(displayFieldName) ? null : node[displayFieldName].AsText);
        }

        /// <summary>
        /// Constructs a title from the {pageid} parameter of a given API call.  The api param is assumed to be double url encoded.
        /// Ex.  =User:Admin%252fMyPage
        /// </summary>
        public static Title FromApiParam(string pageid) {
            if(pageid.StartsWith("=")) {
                pageid = pageid.Substring(1);
                string path = DbEncodePath(XUri.Decode(pageid));
                return FromPrefixedDbPath(path, null);
            }
            if(pageid.EqualsInvariantIgnoreCase("home")) {
                return FromDbPath(NS.MAIN, String.Empty, null);
            }
            return null;
        }

        public static Title FromUriPath(string path) {
            return FromUIUri(null, path, false);
        }

        public static Title FromUriPath(Title baseTitle, string path) {
            return FromUIUri(baseTitle, path, false);
        }

        public static Title FromUIUri(Title baseTitle, string uiUri) {
            return FromUIUri(baseTitle, uiUri, true);
        }

        /// <summary>
        /// Constructs a title from a ui uri segment (ui urls are items inserted by the editor or the user).  The ui uri is assumed to be url encoded.
        /// Ex. "index.php?title=User:Admin/MyPage", "User:Admin/MyPage", "File:User:Admin/MyPage/MyFile.jpg"
        /// </summary>
        public static Title FromUIUri(Title baseTitle, string uiUri, bool parseUri) {
            NS ns;
            string filename = String.Empty;
            string query = null;
            string anchor = null;
            if(parseUri) {
                Match indexPhpMatch = INDEX_PHP_REGEX.Match(uiUri);
                if(indexPhpMatch.Success) {
                    uiUri = uiUri.Substring(indexPhpMatch.Length);
                    int amp = uiUri.IndexOf('&');
                    if(amp >= 0) {
                        uiUri = uiUri.Substring(0, amp) + "?" + uiUri.Substring(amp + 1);
                    }
                }
                int anchorIndex = uiUri.IndexOf('#');
                if(0 <= anchorIndex && (anchorIndex + 1 <= uiUri.Length)) {
                    anchor = uiUri.Substring(anchorIndex + 1);
                    uiUri = uiUri.Substring(0, anchorIndex);
                    if(String.IsNullOrEmpty(uiUri.Trim())) {
                        uiUri = "./";
                    }
                }
                int queryIndex = uiUri.IndexOf('?');
                if(0 <= queryIndex && (queryIndex + 1 < uiUri.Length)) {
                    query = uiUri.Substring(queryIndex + 1);
                    uiUri = uiUri.Substring(0, queryIndex);
                }
            }
            string path = DbEncodePath(uiUri);
            StringToNSAndPath(baseTitle, path, out ns, out path);

            // if the url points to a file, extract the filename from it
            if(ns == NS.ATTACHMENT) {
                int filenameIndex = path.LastIndexOf('/');
                if(0 <= filenameIndex) {
                    filename = path.Substring(filenameIndex, path.Length - filenameIndex);
                    path = path.Substring(0, filenameIndex);
                } else {
                    filename = path;
                    path = String.Empty;
                }
                filename = XUri.Decode(filename.Trim('/'));
                StringToNSAndPath(baseTitle, path, out ns, out path);
            }
            return FromDbPath(ns, path, null, filename, anchor, query);
        }

        /// <summary>
        /// Convert a username to a title representation. I.e., the homepage
        /// </summary>
        /// <param name="username"></param>
        /// <returns></returns>
        public static Title FromUIUsername(string username) {

            //Don't create hierarchies even if the username has /
            username = username.Replace("/", "//");
            string path = DbEncodePath(username);
            if(string.IsNullOrEmpty(path)) {
                throw new ArgumentException("username is empty", "username");
            }
            return FromDbPath(NS.USER, path, null);
        }

        public static Title FromRelativePath(Title root, string path) {
            Title result;
            bool isTalk = false;

            // Check for relative talk page format
            if(path.StartsWith("TALK://")) {
                isTalk = true;
                path = path.Substring(5);
            }

            // Check for relative page format
            if(path.StartsWith("//")) {
                NS ns;
                path = '.' + path.Substring(1);
                StringToNSAndPath(root, path, out ns, out path);
                result = FromDbPath(ns, path, null);
                if(isTalk) {
                    result = result.AsTalk();
                }
            } else {
                result = FromPrefixedDbPath(path, null);
            }
            return result;
        }

        /// <summary>
        /// Retrieve the string corresponding to the namespace type.
        /// </summary>
        public static String NSToString(NS ns) {
            String nsString;
            if(_namespaceNames.TryGetValue(ns, out nsString)) {
                return nsString;
            }
            return String.Empty;
        }

        /// <summary>
        /// Retrieve the type corresponding to the namespace name.
        /// </summary>
        public static NS StringToNS(String nsString) {
            NS ns;
            if(_namespaceValues.TryGetValue(nsString.ToLowerInvariant(), out ns)) {
                return ns;
            }
            return NS.UNKNOWN;
        }

        /// <summary>
        /// Retrieves the specified name as an parameter for an API call.  The output is double uri encoded.
        /// Ex.  =my%252fname
        /// </summary>
        public static string AsApiParam(string name) {

            // returns the name in an api consumable form
            return "=" + XUri.DoubleEncode(name.Replace(" ", "_"));
        }

        public static string AnchorEncode(string section) {
            return XUri.Encode(section.Trim().Replace(' ', '_')).ReplaceAll(
                "%3A", ":",
                "%", "."
            );
        }

        /// <summary>
        /// Helper to resolve a relative page path
        /// </summary>
        private static string ResolvePath(Title baseTitle, string prefixedRelativePath) {

            bool useBasePath = false;
            int basePathSegmentsToRemove = 0;
            string savedTitle = prefixedRelativePath;
            prefixedRelativePath = prefixedRelativePath.Trim();

            // check if the title is a relative path (relative paths are only supported at the beginning of the title)
            if(null != baseTitle) {
                while(prefixedRelativePath.StartsWith("./") || prefixedRelativePath.StartsWith("../")) {
                    useBasePath = true;
                    if(prefixedRelativePath.StartsWith("./")) {
                        prefixedRelativePath = prefixedRelativePath.Substring(2);
                    } else {
                        prefixedRelativePath = prefixedRelativePath.Substring(3);
                        basePathSegmentsToRemove++;
                    }
                }
                if((prefixedRelativePath == ".") || (prefixedRelativePath == "..")) {
                    useBasePath = true;
                    if(prefixedRelativePath == "..") {
                        basePathSegmentsToRemove++;
                    }
                    prefixedRelativePath = String.Empty;
                }
            }

            // if the title is a relative path, resolve it
            if(useBasePath) {
                string basePath = baseTitle.AsPrefixedDbPath();
                if(basePathSegmentsToRemove > 0) {
                    MatchCollection segmentSeparators = SEPARATOR_REGEX.Matches(basePath);

                    // if the relative path specified more segments than are available, do not resolve the link.
                    // otherwise, strip the specified number of segments from the base path
                    if(segmentSeparators.Count + 1 < basePathSegmentsToRemove) {
                        basePath = String.Empty;
                        prefixedRelativePath = savedTitle;
                    } else if(segmentSeparators.Count + 1 == basePathSegmentsToRemove) {
                        int baseNsPos = basePath.IndexOf(':');
                        basePath = (baseNsPos < 0 ? String.Empty : basePath.Substring(0, baseNsPos + 1));
                    } else {
                        basePath = (basePath.Substring(0, (segmentSeparators[segmentSeparators.Count - basePathSegmentsToRemove].Index)));
                    }
                }
                prefixedRelativePath = (basePath + "/" + prefixedRelativePath).Trim('/');
            }

            return prefixedRelativePath;
        }

        /// <summary>
        /// Helper to retrieve the namespace and path from a relative page path string
        /// </summary>
        private static void StringToNSAndPath(Title baseTitle, string prefixedRelativePath, out NS ns, out string path) {

            ns = NS.MAIN;
            path = prefixedRelativePath.Trim(new[] { '/' });

            // handle relative paths in the title
            path = ResolvePath(baseTitle, path);

            // if there is a namespace, retrieve it
            int nsPos = path.IndexOf(':');
            if(nsPos > 1) {
                ns = StringToNS(path.Substring(0, nsPos));

                // if the namespace is not found, default to using the main namespace 
                // if found, extract the namespace from the title text
                if(NS.UNKNOWN == ns) {
                    ns = NS.MAIN;
                } else {
                    path = path.Substring(nsPos + 1);
                }
            }

            // handle relative paths in the title
            path = ResolvePath(baseTitle, path);

            // if we didn't previously have a namespace, check if we have a namespace after resolving relative paths
            if((nsPos < 0) && (null != baseTitle)) {
                StringToNSAndPath(null, path, out ns, out path);
            }
            path = path.Trim(new[] { '/' });
        }

        /// <summary>
        /// Return a title of the form namespace:text, or text if in the main namespace.
        /// </summary>
        private static String NSAndPathToString(NS ns, string path) {
            if(NS.MAIN == ns) {
                return path;
            }
            return NSToString(ns) + ":" + path;
        }

        /// <summary>
        /// Helper method to encode a given path in a database-friendly format
        /// </summary>
        public static string DbEncodePath(string path) {

            // encodes the specified path using the same format as the database
            path = XUri.Decode(path);
            path = path.ReplaceAll(
                "%", "%25",
                "[", "%5B",
                "]", "%5D",
                "{", "%7B",
                "}", "%7D",
                "|", "%7C",
                "+", "%2B",
                "<", "%3C",
                ">", "%3E",
                "#", "%23",
                "//", "%2F",
                " ", "_"
            );
            path = path.Trim(TRIM_CHARS).Replace("%2F", "//");
            return path;
        }

        public static string DbEncodeDisplayName(string displayName) {
            displayName = displayName.ReplaceAll(
                "%", "%25",
                "[", "%5B",
                "]", "%5D",
                "{", "%7B",
                "}", "%7D",
                "|", "%7C",
                "+", "%2B",
                "<", "%3C",
                ">", "%3E",
                "#", "%23",
                " ", "_",
                "/", "//"
            );
            displayName = displayName.Trim(TRIM_CHARS);
            return displayName;
        }

        public static bool IsValidDisplayName(string displayName) {
            return !string.IsNullOrEmpty(displayName);
        }

        public static bool operator ==(Title o1, Title o2) {
            return (Object)o1 == null ? (Object)o2 == null : o1.Equals(o2);
        }

        public static bool operator !=(Title o1, Title o2) {
            return (Object)o1 == null ? (Object)o2 != null : !o1.Equals(o2);
        }

        //--- Fields --- 

        private NS _namespace;
        private string _path;
        private string _displayName;
        private string _filename;
        private string _anchor;
        private string _query;

        //--- Constructors ---
        public Title(Title title) {
            _namespace = title._namespace;
            _path = title._path;
            _displayName = title._displayName;
            _filename = title._filename;
            _anchor = title._anchor;
            _query = title._query;
        }

        private Title(NS ns, string path, string displayName, string filename, string anchor, string query) {
            _namespace = ns;
            _path = path;
            _displayName = displayName;
            _filename = filename;
            _anchor = anchor;
            _query = query;
        }

        //--- Properties ---

        public NS Namespace {
            get { return _namespace; }
            set { _namespace = value; }
        }

        public string Path {
            get { return _path ?? String.Empty; }
            set { _path = value; }
        }

        public string DisplayName {
            get { return _displayName; }
            set { _displayName = value; }
        }

        public string Filename {
            get { return _filename ?? String.Empty; }
            set { _filename = value; }
        }

        public string Anchor {
            get { return _anchor; }
            set { _anchor = value; }
        }

        public string Query {
            get { return _query; }
            set { _query = value; }
        }

        public string Extension { get { return System.IO.Path.GetExtension(Filename).TrimStart('.').ToLowerInvariant(); } }
        public bool IsHomepage { get { return IsRoot && IsMain; } }
        public bool IsRoot { get { return Path == String.Empty; } }
        public bool IsMain { get { return NS.MAIN == Namespace; } }
        public bool IsTemplate { get { return NS.TEMPLATE == Namespace; } }
        public bool IsTemplateTalk { get { return NS.TEMPLATE_TALK == Namespace; } }
        public bool IsUser { get { return NS.USER == Namespace; } }
        public bool IsUserTalk { get { return NS.USER_TALK == Namespace; } }
        public bool IsSpecial { get { return NS.SPECIAL == Namespace; } }
        public bool IsSpecialTalk { get { return NS.SPECIAL_TALK == Namespace; } }
        public bool IsFile { get { return !String.IsNullOrEmpty(Filename); } }
        public bool HasAnchor { get { return null != Anchor; } }
        public bool HasQuery { get { return null != Query; } }

        public bool ShowHomepageChildren {
            get {
                switch(Namespace) {
                case NS.USER:
                case NS.USER_TALK:
                case NS.TEMPLATE:
                case NS.TEMPLATE_TALK:
                case NS.SPECIAL:
                case NS.SPECIAL_TALK:
                    return false;
                }
                return true;
            }
        }

        public bool IsValid {
            get {
                if(DisplayName != null && !IsValidDisplayName(DisplayName)) {
                    return false;
                }
                foreach(var segment in AsUnprefixedDbSegments()) {
                    if(INVALID_TITLE_REGEX.IsMatch(segment)) {
                        return false;
                    }
                }
                return AsUnprefixedDbPath().Length <= 255;
            }
        }

        public bool IsEditable {
            get {
                switch(Namespace) {
                case NS.MAIN:
                case NS.MAIN_TALK:
                case NS.PROJECT:
                case NS.PROJECT_TALK:
                case NS.HELP:
                case NS.HELP_TALK:
                case NS.SPECIAL:
                case NS.SPECIAL_TALK:
                    return true;
                case NS.USER:
                case NS.USER_TALK:
                case NS.TEMPLATE:
                case NS.TEMPLATE_TALK:
                    return !IsRoot;
                }
                return false;
            }
        }

        public bool IsTalk {
            get {
                switch(Namespace) {
                case NS.MAIN_TALK:
                case NS.USER_TALK:
                case NS.PROJECT_TALK:
                case NS.TEMPLATE_TALK:
                case NS.HELP_TALK:
                case NS.SPECIAL_TALK:
                    return true;
                default:
                    return false;
                }
            }
        }

        //--- Methods ---

        /// <summary>
        /// Helper method to include the filename query and anchor with the path
        /// </summary>
        public string AppendFilenameQueryAnchor(string path, bool containsIndexPhp) {
            if(IsFile) {
                if(!path.EndsWith("/")) {
                    path = path + "/";
                }
                path = "File:" + path + Filename.Replace("?", "%3f").Replace("#", "%23").Replace("&", "%26");
            }
            if(HasQuery) {
                if(path.LastIndexOf('?') >= 0 || containsIndexPhp) {
                    path += "&" + Query;
                } else {
                    path += "?" + Query;
                }
            }
            if(HasAnchor) {
                path += "#" + Anchor;
            }
            return path;
        }

        /// <summary>
        /// Returns true if this title is a parent of the specified title
        /// </summary>
        public bool IsParentOf(Title title) {
            string parent = AsPrefixedDbPath();
            string child = title.AsPrefixedDbPath();
            return child.StartsWith(parent + "/", true, null) && ((child.Length < parent.Length + 2) || (child[parent.Length + 2 - 1] != '/'));
        }

        /// <summary>
        /// Retrieves the parent title
        /// </summary>
        public Title GetParent() {
            string[] parents = AsUnprefixedDbSegments();

            // parent of the home page is null
            if(0 == parents.Length) {
                return null;
            }

            // parent of pages with only one segment is the main page
            if(1 == parents.Length) {
                // Special:XXX pages should use Special: as their root, instead of the homepage
                if(!IsRoot && IsSpecial) {
                    return FromDbPath(NS.SPECIAL, String.Empty, null);
                }
                return FromDbPath(NS.MAIN, String.Empty, null);
            }

            // parent of a page with multiple segments is obtained by combining the segments
            if(1 < parents.Length) {
                return FromDbPath(_namespace, String.Join("/", parents, 0, parents.Length - 1), null);
            }

            throw new InvalidOperationException("invalid title state");
        }

        /// <summary>
        /// Retrieves the parent title
        /// </summary>
        /// <param name="forceNamespace">Force the namespace of the parent Title to be same as the current one</param>
        /// <returns>Parent Title instance.</returns>
        public Title GetParent(bool forceNamespace) {
            if(!forceNamespace) {
                return GetParent();
            }
            var parentTitle = IsFile ? FromDbPath(Namespace, Path, null) : GetParent();
            if((null != parentTitle) && (!IsRoot)) {
                parentTitle.Namespace = Namespace;
            }
            return parentTitle;
        }

        /// <summary>
        /// Determine if this Title is the parent of another Title
        /// </summary>
        /// <param name="forceNamespace">Force the namespace of the child Title to be same as the current one</param>
        /// <param name="title">Candidate child Title</param>
        /// <returns><see langword="True"/> if the provided Title is a child of the current Title</returns>
        public bool IsParentOf(bool forceNamespace, Title title) {
            var parent = title;
            if(Namespace == title.Namespace) {
                while(null != (parent = parent.GetParent(forceNamespace))) {
                    if(parent == this) {
                        return true;
                    }
                }
            }
            return false;
        }

        public override string ToString() {
            return AppendFilenameQueryAnchor(AsPrefixedDbPath(), false);
        }

        /// <summary>
        /// Retrieves the database encoded title
        /// </summary>
        public string AsUnprefixedDbPath() {
            return Path;
        }

        /// <summary>
        /// Retrieves the database encoded title prefixed with the namespace
        /// </summary>
        public string AsPrefixedDbPath() {
            return NSAndPathToString(_namespace, Path);
        }

        /// <summary>
        /// Retrieves the database encoded title as segments
        /// </summary>
        public string[] AsUnprefixedDbSegments() {
            return AsDbSegments(AsUnprefixedDbPath());
        }

        public string AsSegmentName() {
            string[] segments = AsUnprefixedDbSegments();
            if(segments.Length > 0) {
                return segments[segments.Length - 1];
            }
            return string.Empty;
        }



        /// <summary>
        /// Retrieves an array of database encoded title segments
        /// </summary>
        private string[] AsDbSegments(string path) {

            // replace '//' with '%2f' ('//' was used as encoding for '/' in titles)
            path = path.Replace("//", "%2f");

            // split path into segments and encode each segment individually
            string[] parts = path.Split('/');
            if((parts.Length == 1) && (parts[0].Length == 0) && IsMain) {

                // the db-segments for the homepage is an empty array (so that it has length less than its child pages)
                return StringUtil.EmptyArray;
            }
            string[] result = Array.ConvertAll(parts, delegate(string segment) {

                // undo addition of capture avoiding symbol
                segment = segment.Replace("%2f", "//");
                return segment;
            });
            return result;
        }

        /// <summary>
        /// Retrieves the title as the {pageid} parameter for an API call.  The output is double uri encoded.
        /// Ex.  =User:Admin%252fMyPage
        /// </summary>
        public string AsApiParam() {

            // returns the title in an {pageid} api consumable form
            if(IsHomepage) {
                return "home";
            }
            return "=" + NSAndPathToString(_namespace, XUri.DoubleEncode(AsUnprefixedDbPath()));
        }

        /// <summary>
        /// Retrieves the title as a ui uri segment (ui urls can be used to access pages by pre-pending the ui host).  The output is db encoded.
        /// Ex. "index.php?title=User:Admin/MyPage", "User:Admin/MyPage"
        /// </summary>
        public string AsUiUriPath() {
            return AsUiUriPath(false);
        }

        /// <summary>
        /// Retrieves the title as a ui uri segment (ui urls can be used to access pages by pre-pending the ui host).  The output is db encoded.
        /// Ex. "index.php?title=User:Admin/MyPage", "User:Admin/MyPage"
        /// </summary>
        public string AsUiUriPath(bool forceUseIndexPhp) {
            string path;
            string dbPath = XUri.Decode(AsUnprefixedDbPath());
            if(forceUseIndexPhp) {
                path = INDEX_PHP_TITLE + AppendFilenameQueryAnchor(NSAndPathToString(Namespace, XUri.EncodeQuery(dbPath).Replace("+", "%2b")), true);
            } else {

                // returns the title in a wiki ui consumable form
                StringBuilder pathBuilder = new StringBuilder();
                string[] segments = AsDbSegments(dbPath);
                bool useIndexPhp = false;
                foreach(string segment in segments) {
                    if(-1 < segment.IndexOfAny(INDEX_PHP_CHARS)) {
                        useIndexPhp = true;
                        break;
                    }
                    if(0 < pathBuilder.Length) {
                        pathBuilder.Append("/");
                    }
                    pathBuilder.Append(XUri.EncodeSegment(segment));
                }
                if(useIndexPhp) {
                    path = INDEX_PHP_TITLE + AppendFilenameQueryAnchor(NSAndPathToString(Namespace, XUri.EncodeQuery(dbPath).Replace("+", "%2b")), true);
                } else {
                    path = AppendFilenameQueryAnchor(NSAndPathToString(Namespace, pathBuilder.ToString().Replace("?", "%3f")), false);
                }
            }

            // URL encode ?'s so they don't get confused for the query and add the filename, anchor and query 
            return path;
        }

        public string AsRelativePath(Title root) {
            string dbPath;
            Title activeRoot = IsTalk ? root.AsTalk() : root.AsFront();

            // Check if the current title is under the root, and if so, make it relative
            if((activeRoot.IsParentOf(this) || (activeRoot.Namespace == Namespace && activeRoot.IsRoot) || (activeRoot.AsPrefixedDbPath() == AsPrefixedDbPath()))) {
                dbPath = "//" + (AsPrefixedDbPath().Substring(activeRoot.AsPrefixedDbPath().Length).Trim(new[] { '/' }));
                if(IsTalk) {
                    dbPath = "TALK:" + dbPath;
                }
            } else {
                dbPath = AsPrefixedDbPath();
            }
            return dbPath;
        }

        /// <summary>
        /// Retrieves the talk title associated with this title
        /// </summary>
        public Title AsTalk() {
            Title associatedTalkTitle = FromDbPath(Namespace, Path, DisplayName);
            if(!IsTalk) {
                NS talkNamespace;
                if(_frontToTalkMap.TryGetValue(Namespace, out talkNamespace)) {
                    associatedTalkTitle.Namespace = talkNamespace;
                } else {
                    associatedTalkTitle = null;
                }
            }
            return associatedTalkTitle;
        }

        /// <summary>
        /// Retrieves the non-talk title associated with this title
        /// </summary>
        public Title AsFront() {
            Title associatedFrontTitle = FromDbPath(Namespace, Path, DisplayName);
            if(IsTalk) {
                NS frontNamespace;
                if(_talkToFrontMap.TryGetValue(Namespace, out frontNamespace)) {
                    associatedFrontTitle.Namespace = frontNamespace;
                } else {
                    associatedFrontTitle = null;
                }
            }
            return associatedFrontTitle;
        }

        public Title WithUserFriendlyName(string name) {
            name = name.Replace("/", "//");
            Title result = this;

            // check if current title represents the home page
            if(IsHomepage) {

                // if appended name is a prefix, change namespaces when appropriate
                NS ns;
                StringToNSAndPath(null, name, out ns, out name);
                result = FromDbPath(ns, Path, null);
            }

            // construct new path
            string path = string.IsNullOrEmpty(result.Path) ? DbEncodePath(name) : result.Path + "/" + DbEncodePath(name);

            // initialize new title object
            result = new Title(result.Namespace, path, result.DisplayName, result.Filename, result.Anchor, result.Query);

            // trim any dangling '/' characters
            result.Path = result.Path.TrimEnd(new[] { '/' });

            // validate new title object
            if(!result.IsValid) {
                throw new ArgumentException(string.Format("resulting title object is invalid: {0}", result.AsPrefixedDbPath()), "name");
            }
            return result;
        }

        public Title WithUserFriendlyName(string name, string displayName) {
            Title result = WithUserFriendlyName(name);
            result.DisplayName = displayName;
            return result;
        }

        /// <summary>
        /// Change the last segment of a page path and the displayname.
        /// * Requires at least a displayname or name to be provided (not empty/null)
        /// * displayname provided but null name: name is generated from title
        /// * name is provided but null displayname: displayname is unaffected
        /// </summary>
        /// <param name="name"></param>
        /// <param name="displayName"></param>
        /// <returns></returns>
        public Title Rename(string name, string displayName) {
            if(string.IsNullOrEmpty(name) && string.IsNullOrEmpty(displayName)) {
                throw new ArgumentException("neither 'name' nor 'displayName' were provided");
            }

            // NOTE (steveb): mimic logic of PageBL.GetPathType() for FIXED

            // ensure page is not a root page
            if(!IsRoot && !(IsUser && GetParent().IsHomepage) && !IsTalk) {

                // check if a name must be generated for the page based on the display name
                if(string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(displayName)) {

                    // name comes from display name
                    name = displayName; // name gets encoded later by WithUserFriendlyName
                }
            } else if(!string.IsNullOrEmpty(name)) {

                // root pages cannot be renamed
                throw new ArgumentException("root pages cannot be renamed");
            }

            // check if a name was supplied (implies current page is not root)
            Title ret;
            if(!string.IsNullOrEmpty(name)) {
                Title parent = GetParent(true);

                // renaming a top level page in a namespace (e.g. special:foo)
                if(parent.IsRoot) {

                    // strip out namespace: prefix if it's same as current namespace
                    string prefix = _namespaceNames[_namespace] + ":";
                    if(name.StartsWithInvariantIgnoreCase(prefix)) {
                        name = name.Substring(prefix.Length);
                    }
                    ret = FromDbPath(_namespace, string.Empty, null).WithUserFriendlyName(name);
                } else {
                    ret = parent.WithUserFriendlyName(name);
                }
            } else {
                ret = new Title(this);
            }

            // "foo" renamed with "template:bar"
            if(_namespace != ret._namespace) {
                throw new ArgumentException("rename may not change namespaces");
            }

            // page title modified?
            ret.DisplayName = !string.IsNullOrEmpty(displayName) ? displayName : _displayName;
            if(!ret.IsValid) {
                throw new ArgumentException("renamed title is invalid");
            }
            return ret;
        }

        public override bool Equals(object obj) {
            return ((IEquatable<Title>)this).Equals(obj as Title);
        }

        public override int GetHashCode() {
            int result = StringComparer.OrdinalIgnoreCase.GetHashCode(_namespace) ^ StringComparer.OrdinalIgnoreCase.GetHashCode(Path) ^ StringComparer.OrdinalIgnoreCase.GetHashCode(Filename);
            if(null != Query) {
                result = result ^ StringComparer.OrdinalIgnoreCase.GetHashCode(Query);
            }
            if(null != Anchor) {
                result = result ^ StringComparer.OrdinalIgnoreCase.GetHashCode(Anchor);
            }
            return result;
        }

        //--- IEquatable<Title> Members ---
        bool IEquatable<Title>.Equals(Title info) {
            if(null == info) {
                return false;
            }
            return (Namespace == info.Namespace) && AsUnprefixedDbPath().EqualsInvariantIgnoreCase(info.AsUnprefixedDbPath()) && Filename.EqualsInvariantIgnoreCase(info.Filename) && Query.EqualsInvariantIgnoreCase(info.Query) && Anchor.EqualsInvariantIgnoreCase(info.Anchor);
        }

        //--- IComparable<Title> Members ---
        public int CompareTo(Title other) {
            int compare = (int)this.Namespace - (int)other.Namespace;
            if(compare != 0) {
                return compare;
            }
            return this.AsUnprefixedDbPath().Replace("//", "\uFFEF").CompareInvariantIgnoreCase(other.AsUnprefixedDbPath().Replace("//", "\uFFEF"));
        }
    }
}

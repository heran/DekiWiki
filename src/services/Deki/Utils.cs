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
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using MindTouch.Deki.Exceptions;
using MindTouch.Dream;
using MindTouch.Deki.Data;
using MindTouch.Deki.Logic;
using MindTouch.Xml;

namespace MindTouch.Deki {
    public static class Utils {


        //--- Constants ---
        internal const string MKS_PATH = "mks://localhost/";
        internal const char DOUBLE_ARROW = (char)8658; // rightwards double arrow: &rArr;

        //--- Class Methods ---
        internal static string PathCombine(string first, params string[] paths) {
            string result = first;
            foreach(string path in paths) {
                result = Path.Combine(result, path);
            }
            return result;
        }

        internal static bool EnsureDirectoryExists(string dirName) {
            if(!Directory.Exists(dirName)) {
                try {
                    if(!Directory.CreateDirectory(dirName).Exists) {
                        return false;
                    }
                } catch {
                    return false;
                }
            }
            return true;
        }

        internal static XDoc GetPageDiff(XDoc before, XDoc after, bool compact, int maxDelta, out XDoc invisibleDiff, out DekiResource summary, out XDoc beforeHighlightedChanges, out XDoc afterHighlightedChanges) {
            var resources = DekiContext.Current.Resources;
            XDoc result = XDoc.Empty;
            beforeHighlightedChanges = before;
            afterHighlightedChanges = after;
            invisibleDiff = XDoc.Empty;

            // compute diff between the two documents
            Tuplet<ArrayDiffKind, XDocDiff.Token>[] diff = XDocDiff.Diff(before, after, maxDelta);
            if(diff == null) {
                summary = DekiResources.PAGE_DIFF_TOO_LARGE();
                return result;
            }

            // create change summary
            int added;
            int removed;
            int attributes;
            int structural;
            summary = GetChangeSummary(diff, out added, out removed, out attributes, out structural);

            // create document with highlighted changes
            if((added > 0) || (removed > 0) || (attributes > 0)) {
                try {
                    List<Tuplet<string, string, string>> invisibleChanges;
                    XDocDiff.Highlight(diff, out result, out invisibleChanges, out beforeHighlightedChanges, out afterHighlightedChanges);

                    // check if we should only keep top-level elements that contain changes to cut down on the noise
                    if(compact) {
                        if((added > 0) || (removed > 0)) {
                            CompactDiff(result);
                            CompactDiff(beforeHighlightedChanges);
                            CompactDiff(afterHighlightedChanges);
                        } else {

                            // no changes were made, remove all nodes
                            result = XDoc.Empty;
                        }
                    }

                    // add invisible changes
                    if(invisibleChanges.Count > 0) {
                        invisibleDiff = new XDoc("ol");
                        foreach(Tuplet<string, string, string> change in invisibleChanges) {
                            invisibleDiff.Start("li");
                            invisibleDiff.Value(string.Format("{0}: ", change.Item1));
                            if(change.Item2 != null) {
                                invisibleDiff.Elem("del", change.Item2.QuoteString());
                            } else {
                                invisibleDiff.Value(resources.Localize(DekiResources.PAGE_DIFF_NOTHING()));
                            }
                            invisibleDiff.Value(" " + DOUBLE_ARROW + " ");
                            if(change.Item3 != null) {
                                invisibleDiff.Elem("ins", change.Item3.QuoteString());
                            } else {
                                invisibleDiff.Value(resources.Localize(DekiResources.PAGE_DIFF_NOTHING()));
                            }
                            invisibleDiff.End();
                        }
                    }
                } catch(Exception e) {
                    summary = DekiResources.PAGE_DIFF_ERROR("MindTouch", e);
                }
            }
            return result;
        }

        private static void CompactDiff(XDoc doc) {
            XDoc dotdotdot = new XDoc("p").Value("...");
            foreach(XDoc skip in doc["child::node()[not(.//ins | .//del)]"]) {

                // make sure we only operate on XML element nodes
                if((skip.AsXmlNode.NodeType == XmlNodeType.Element) && !skip.HasName("ins") && !skip.HasName("del") && skip.AsXmlNode.HasChildNodes) {

                    // check if the previous element is already a <p>...</p> node
                    XmlNode previous = skip.AsXmlNode.PreviousSibling;
                    while((previous != null) && (previous.NodeType != XmlNodeType.Element) && ((previous.NodeType != XmlNodeType.Text) || (previous.Value.Trim().Length == 0))) {
                        previous = previous.PreviousSibling;
                    }
                    if((previous == null) || !StringUtil.EqualsInvariant(previous.LocalName, "p") || !StringUtil.EqualsInvariant(previous.InnerXml, "...")) {

                        // replace node with <p>...</p>
                        skip.Replace(dotdotdot);
                    } else {

                        // remove node
                        skip.Remove();
                    }
                }
            }
        }

        internal static DekiResource GetPageDiffSummary(PageBE page, string before, string beforeMime, string after, string afterMime, int maxDelta) {
            ParserResult beforeDoc = DekiXmlParser.Parse(page, beforeMime, page.Language, before, ParserMode.EDIT, false, -1, null, null);
            ParserResult afterDoc = DekiXmlParser.Parse(page, afterMime, page.Language, after, ParserMode.EDIT, false, -1, null, null);
            Tuplet<ArrayDiffKind, XDocDiff.Token>[] diff = XDocDiff.Diff(beforeDoc.MainBody, afterDoc.MainBody, maxDelta);
            if(diff == null) {
                return DekiResources.PAGE_DIFF_TOO_LARGE();
            }

            // create change summary
            int added;
            int removed;
            int attributes;
            int structural;
            return GetChangeSummary(diff, out added, out removed, out attributes, out structural);
        }

        internal static int GetPageWordCount(XDoc doc) {
            XDocWord[] words = XDocWord.ConvertToWordList(doc);
            int result = 0;
            for(int i = 0; i < words.Length; ++i) {
                if(words[i].IsWord) {
                    ++result;
                }
            }
            return result;
        }

        public static void RemoveEmptyNodes(XDoc doc) {
            foreach(XDoc node in doc.VisitAll().Reverse().ToList()) {
                var xNode = node.AsXmlNode;
                if(xNode.NodeType == XmlNodeType.Element && !xNode.HasChildNodes && xNode.Attributes.Count == 0) {
                    node.Remove();
                }
            }
        }

        internal static string AsPublicUiUri(Title title) {
            return AsPublicUiUri(title, false);
        }

        internal static string AsPublicUiUri(Title title, bool forceUseIndexPhp) {
            DreamContext context = DreamContext.Current;
            string baseUri = context.GetParam("baseuri", null);
            if(baseUri != null) {
                return baseUri + title.AsUiUriPath(true).Substring(Title.INDEX_PHP_TITLE.Length);
            } else {
                DekiContext deki = DekiContext.Current;
                XUri global = context.AsPublicUri(deki.UiUri.Uri.WithoutPathQueryFragment().WithoutCredentials());
                return global.SchemeHostPort + "/" + title.AsUiUriPath(forceUseIndexPhp);
            }
        }

        internal static XUri AsPublicApiUri(string resource, ulong id) {
            return DreamContext.Current.AsPublicUri(DekiContext.Current.ApiUri.At(resource, id.ToString()));
        }

        internal static void GetOffsetAndCountFromRequest(DreamContext context, uint defaultCount, out uint count, out uint offset) {
            string sortField = string.Empty;
            SortDirection sortDirection;
            GetOffsetAndCountFromRequest(context, defaultCount, out count, out offset, out sortDirection, out sortField);
        }

        internal static void GetOffsetAndCountFromRequest(DreamContext context, uint defaultCount, out uint count, out uint offset, out SortDirection sortDirection, out string sortField) {

            offset = context.GetParam<uint>("offset", 0);
            count = defaultCount;
            sortDirection = SortDirection.UNDEFINED;

            string countStr = context.GetParam("limit", null) ?? context.GetParam("count", null) ?? context.GetParam("max", count.ToString());
            if(countStr.EqualsInvariantIgnoreCase("all")) {
                count = uint.MaxValue;
            } else if(!uint.TryParse(countStr, out count)) {
                throw new LimitParameterInvalidArgumentException();
            }

            sortField = context.GetParam("sortby", null);
            if(sortField == string.Empty)
                sortField = null;

            if(!string.IsNullOrEmpty(sortField)) {
                if(sortField.StartsWith("-")) {
                    sortDirection = SortDirection.DESC;
                    sortField = sortField.Substring(1);
                } else {
                    sortDirection = SortDirection.ASC;
                }
            }
        }

        internal static SetDiscriminator GetSetDiscriminatorFromRequest(DreamContext context, uint defaultCount, string defaultSortBy) {
            var discriminator = new SetDiscriminator();
            discriminator.Offset = context.GetParam<uint>("offset", 0);
            discriminator.Limit = defaultCount;
            var limit = context.GetParam("limit", null) ?? context.GetParam("count", null) ?? context.GetParam("max", discriminator.Limit.ToString());
            if(limit.EqualsInvariantIgnoreCase("all")) {
                discriminator.Limit = uint.MaxValue;
            } else if(!uint.TryParse(limit, out discriminator.Limit)) {
                throw new LimitParameterInvalidArgumentException();
            }
            discriminator.SetSortInfo(context.GetParam("sortby", ""), defaultSortBy);
            return discriminator;
        }

        internal static bool ShowPrivateUserInfo(UserBE u) {
            DekiContext context = DekiContext.Current;

            //Requesting user is able to see private user details if looking up self, setting privacy/expose-user-email is true, or an apikey is provided, or admin
            return (context.Instance.PrivacyExposeUserEmail) || (context.User != null && context.User.ID == u.ID) || (context.Deki.DetermineAccess(DreamContext.Current, DreamContext.Current.Request) == DreamAccess.Internal);
        }

        internal static bool IsPageModification(RC type) {

            // list operations that should be grouped together for daily change digest
            return (type == RC.EDIT) || (type == RC.PAGEMETA) || (type == RC.NEW) || (type == RC.FILE) || (type == RC.GRANTS_ADDED) || (type == RC.GRANTS_REMOVED) || (type == RC.TAGS) || (type == RC.RESTRICTION_UPDATED);
        }

        internal static bool IsPageEdit(RC type) {

            // list operations that cause a new revision number
            return (type == RC.EDIT) || (type == RC.NEW);
        }

        internal static bool IsPageComment(RC type) {

            // list operation that apply to page comments
            return (type == RC.COMMENT_CREATE) || (type == RC.COMMENT_DELETE) || (type == RC.COMMENT_UPDATE);
        }

        internal static bool IsPageHiddenOperation(RC type) {

            // list operations that should be ignored when generating the change feed
            return (type == RC.LOG) || (type == RC.MOVE_OVER_REDIRECT) || (type == RC.USER_CREATED);
        }

        internal static string LinguisticJoin(IEnumerable<string> authors, string conjunctive) {
            StringBuilder result = new StringBuilder();
            int count = authors.Count();
            int i = 0;
            foreach(string author in authors) {
                if(i == 0) {
                    result.Append(author);
                } else if(i == (count - 1)) {
                    if(count > 2) {
                        result.Append(", ");
                    } else {
                        result.Append(" ");
                    }
                    result.Append(conjunctive);
                    result.Append(" ");
                    result.Append(author);
                } else {
                    result.Append(", ");
                    result.Append(author);
                }
                ++i;
            }
            return result.ToString();
        }

        internal static string EncodeUriCharacters(string text) {
            StringBuilder result = null;
            int start = 0;
            for(int i = 0; i < text.Length; ++i) {
                if(text[i] >= 127) {
                    if(result == null) {
                        result = new StringBuilder();
                    }
                    result.Append(text, start, i - start);
                    result.Append(XUri.Encode(text[i].ToString()));
                    start = i + 1;
                }
            }
            if(result != null) {
                result.Append(text, start, text.Length - start);
                return result.ToString();
            }
            return text;
        }

        internal static Title GetRelToTitleFromUrl(DreamContext context) {
            Title relToTitle = null;
            uint rootId = context.GetParam<uint>("relto", 0);
            if(0 == rootId) {
                string path = context.GetParam("reltopath", null);
                if(null != path) {
                    relToTitle = Title.FromPrefixedDbPath(path, null);
                }
            } else {
                PageBE rootPage = PageBL.GetPageById(rootId);
                if((null == rootPage) || (0 == rootPage.ID)) {
                    throw new PageIdInvalidArgumentException();
                } else {
                    relToTitle = rootPage.Title;
                }
            }
            if((null != relToTitle) && relToTitle.IsTalk) {
                throw new PageReltoTalkInvalidOperationException();
            }
            return relToTitle;
        }

        /// <summary>
        /// Retrieves the user-friendly name of this title
        /// </summary>
        public static string AsUserFriendlyName(this Title title) {
            if(title.IsFile) {
                return title.AppendFilenameQueryAnchor(String.Empty, false);
            } else {
                string result = title.AsPrefixedDbPath().Replace('_', ' ');
                if(!title.HasAnchor && !title.HasQuery) {

                    // Check if there is a translated name for the page
                    string resourceResult = title.GetTranslatedName();
                    if(resourceResult != null) {
                        return resourceResult;
                    }

                    // check if page has a display name (always the case for new pages after Olympic)
                    if(!string.IsNullOrEmpty(title.DisplayName)) {
                        return title.DisplayName;
                    }
                }

                // replace '//' with '%2f' ('//' was used as encoding for '/' in titles)
                result = result.Trim('/').Replace("//", "%2f");

                // split path into segments 
                string[] segments = result.Split('/');
                result = segments[segments.Length - 1];
                return title.AppendFilenameQueryAnchor(XUri.Decode(result.Replace("%2f", "/")), false);
            }
        }

        /// <summary>
        /// Retrieves the user-friendly display name of this title.
        /// Used to always build a display name, either from localized resources or the last segment in the path.
        /// </summary>
        public static string AsUserFriendlyDisplayName(this Title title) {
            string result = title.AsPrefixedDbPath().Replace('_', ' ');

            // Check if there is a translated name for the page
            string resourceResult = title.GetTranslatedName();
            if(resourceResult != null) {
                return resourceResult;
            }

            // replace '//' with '%2f' ('//' was used as encoding for '/' in titles)
            result = result.Trim('/').Replace("//", "%2f");

            // split path into segments 
            string[] segments = result.Split('/');
            result = segments[segments.Length - 1];
            return title.AppendFilenameQueryAnchor(XUri.Decode(result.Replace("%2f", "/")), false);
        }


        public static string AsPrefixedUserFriendlyPath(this Title title) {
            if(!title.IsFile && !title.HasAnchor && !title.HasQuery) {

                // this is a special/admin page, we check if there is a translated name for the page
                string resourceResult = title.GetTranslatedName();
                if(resourceResult != null) {
                    return resourceResult;
                }
            }
            return title.AppendFilenameQueryAnchor(XUri.Decode(title.AsPrefixedDbPath()), false);
        }

        /// <summary>
        /// Retrieves the title as a path consumable by the editor.  The relative path is used if possible.
        /// Ex. "User:Admin/MyPage/"
        /// </summary>
        public static string AsEditorUriPath(this Title title) {

            // URL encode ?'s so they don't get confused for the query and add the filename, anchor and query 
            return Utils.MKS_PATH + title.AppendFilenameQueryAnchor(title.AsPrefixedDbPath().Replace("?", "%3f"), false);
        }

        public static string ToString(this Title title) {
            return title.AsPrefixedUserFriendlyPath();
        }

        private static string GetTranslatedName(this Title title) {

            // this is the homepage, return the site name
            if(title.IsHomepage) {
                return string.IsNullOrEmpty(title.DisplayName) ? DekiContext.Current.Instance.SiteName : null;
            }

            string result = null;

            // TODO (arnec): should these strings be DekiResource instances?
            var resources = DekiContext.Current.Resources;
            switch(title.Namespace) {
            case NS.USER:
                if(title.IsRoot) {
                    result = resources.LocalizeOrNull("Page.ListUsers.page-title");
                }
                break;
            case NS.TEMPLATE:
                if(title.IsRoot) {
                    result = resources.LocalizeOrNull("Page.ListTemplates.page-title");
                }
                break;
            case NS.ADMIN:
                if(title.IsRoot) {
                    result = resources.LocalizeOrNull("Admin.ControlPanel.page-title");
                } else {
                    result = resources.LocalizeOrNull("Admin.ControlPanel." + title.Path + ".page-title");
                }
                break;
            case NS.SPECIAL:
                if(title.IsRoot) {
                    result = resources.LocalizeOrNull("Page.SpecialPages.page-title");
                } else {
                    result = resources.LocalizeOrNull("Page.SpecialPages." + title.Path + ".page-title");
                }
                break;
            }
            return string.IsNullOrEmpty(result) ? null : result;
        }

        private static DekiResource GetChangeSummary(Tuplet<ArrayDiffKind, XDocDiff.Token>[] diff, out int added, out int removed, out int attributes, out int structural) {
            added = 0;
            removed = 0;
            attributes = 0;
            structural = 0;

            // count changes
            for(int i = 0; i < diff.Length; ++i) {
                switch(diff[i].Item1) {
                case ArrayDiffKind.Added:
                    if((diff[i].Item2.Type == XmlNodeType.Text) && (diff[i].Item2.Value.Length > 0) && !char.IsWhiteSpace(diff[i].Item2.Value[0])) {
                        ++added;
                    } else if(diff[i].Item2.Type == XmlNodeType.Attribute) {
                        ++attributes;
                    } else if((diff[i].Item2.Type == XmlNodeType.Element) || (diff[i].Item2.Type == XmlNodeType.None)) {
                        ++structural;
                    }
                    break;
                case ArrayDiffKind.Removed:
                    if((diff[i].Item2.Type == XmlNodeType.Text) && (diff[i].Item2.Value.Length > 0) && !char.IsWhiteSpace(diff[i].Item2.Value[0])) {
                        ++removed;
                    } else if(diff[i].Item2.Type == XmlNodeType.Attribute) {
                        ++attributes;
                    } else if((diff[i].Item2.Type == XmlNodeType.Element) || (diff[i].Item2.Type == XmlNodeType.None)) {
                        ++structural;
                    }
                    break;
                }
            }

            // compute summary
            DekiResource result;
            if((added > 0) && (removed > 0)) {
                result = DekiResources.PAGE_DIFF_SUMMARY(added, removed);
            } else if(added > 0) {
                result = DekiResources.PAGE_DIFF_SUMMARY_ADDED(added);
            } else if(removed > 0) {
                result = DekiResources.PAGE_DIFF_SUMMARY_REMOVED(removed);
            } else if((attributes > 0) || (structural > 0)) {
                result = DekiResources.PAGE_DIFF_SUMMARY_NOT_VISIBLE();
            } else {
                result = DekiResources.PAGE_DIFF_SUMMARY_NOTHING();
            }
            return result;
        }
    }
}

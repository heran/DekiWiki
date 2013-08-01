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
using System.Globalization;
using System.Linq;
using MindTouch.Deki.Data;
using MindTouch.Dream;
using MindTouch.Web;
using MindTouch.Xml;

namespace MindTouch.Deki.Logic {
    public static class PageSiteMapBL {

        //--- Types ---
        internal class CompareInfoComparer : Comparer<string> {

            // TODO (steveb): move this class into Dream

            //--- Fields ---
            private CompareInfo _compare;

            //--- Constructors ---
            internal CompareInfoComparer(CompareInfo compare) {
                if(compare == null) {
                    throw new ArgumentNullException("compare");
                }
                _compare = compare;
            }

            //--- Methods ---
            public override int Compare(string left, string right) {
                return _compare.Compare(left, right);
            }
        }

        #region XML

        public static XDoc BuildXmlSiteMap(PageBE rootPage, string language) {
            Dictionary<ulong, PageBE> pagesById = null;
            rootPage = PageBL.PopulateDescendants(rootPage, null, out pagesById, ConfigBL.GetInstanceSettingsValueAs<int>(ConfigBL.MAX_SITEMAP_SIZE_KEY, ConfigBL.MAX_SITEMAP_SIZE));

            PageBE[] allowedPages = PermissionsBL.FilterDisallowed(DekiContext.Current.User, new List<PageBE>(pagesById.Values).ToArray(), false, new Permissions[] { Permissions.BROWSE });
            Dictionary<ulong, PageBE> allowedPagesById = allowedPages.AsHash(e => e.ID);
            Dictionary<ulong, PageBE> addedPagesById = null;
            if(!string.IsNullOrEmpty(language)) {
                List<ulong> pagesToRemove = new List<ulong>();
                foreach(KeyValuePair<ulong, PageBE> page in allowedPagesById) {
                    if(!string.IsNullOrEmpty(page.Value.Language) && !StringUtil.EqualsInvariantIgnoreCase(page.Value.Language, language)) {
                        pagesToRemove.Add(page.Key);
                    }
                }
                foreach(ulong pageId in pagesToRemove) {
                    allowedPagesById.Remove(pageId);
                }
            }
            PageBL.AddParentsOfAllowedChildren(rootPage, allowedPagesById, addedPagesById);

            return BuildXmlSiteMap(rootPage, new XDoc("pages"), allowedPagesById);
        }

        private static XDoc BuildXmlSiteMap(PageBE current, XDoc doc, Dictionary<ulong, PageBE> allowedPagesById) {
            doc.Add(PageBL.GetPageXml(current, null));
            XDoc y = doc[doc.AsXmlNode.LastChild].Start("subpages");
            
            if (!ArrayUtil.IsNullOrEmpty(current.ChildPages)) {
                PageBE[] visibleChildren = Array.FindAll(current.ChildPages, delegate(PageBE child) {
                    return allowedPagesById.ContainsKey(child.ID);
                });
                foreach (PageBE child in visibleChildren) {
                    BuildXmlSiteMap(child, y, allowedPagesById);
                }
            }

            y.End();

            return doc;
        }

        #endregion

        /// <summary>
        /// Builds an HTML view of the site (with permissions enforced)
        /// </summary>
        /// <param name="rootPage"></param>
        /// <returns></returns>
        public static XDoc BuildHtmlSiteMap(PageBE rootPage, string language, int depth, bool reverse) {
            if (depth <= 0) {
                return XDoc.Empty;
            }
            Dictionary<ulong, PageBE> pagesById = null;
            if (depth == 1) {
                rootPage.ChildPages = PageBL.GetChildren(rootPage, true).ToArray();
                pagesById = rootPage.ChildPages.ToDictionary(p => p.ID, true);
            } else {
                rootPage = PageBL.PopulateDescendants(rootPage, null, out pagesById, ConfigBL.GetInstanceSettingsValueAs<int>(ConfigBL.MAX_SITEMAP_SIZE_KEY, ConfigBL.MAX_SITEMAP_SIZE));
            }
            PageBE[] filteredPages = null;
            PageBE[] allowedPages = PermissionsBL.FilterDisallowed(DekiContext.Current.User, new List<PageBE>(pagesById.Values).ToArray(), false, out filteredPages, new Permissions[] { Permissions.BROWSE });
            Dictionary<ulong, PageBE> allowedPagesById = allowedPages.AsHash(e => e.ID);
            Dictionary<ulong, PageBE> filteredPagesById = filteredPages.AsHash(e => e.ID);
            if(!string.IsNullOrEmpty(language)) {
                List<ulong> pagesToRemove = new List<ulong>();
                foreach (KeyValuePair<ulong, PageBE> page in allowedPagesById) {
                    if (!string.IsNullOrEmpty(page.Value.Language) && !StringUtil.EqualsInvariantIgnoreCase(page.Value.Language, language)) {
                        pagesToRemove.Add(page.Key);
                    }
                }
                foreach (ulong pageId in pagesToRemove) {
                    allowedPagesById.Remove(pageId);
                }
            }
            Dictionary<ulong, PageBE> addedPagesById = null;
            PageBL.AddParentsOfAllowedChildren(rootPage, allowedPagesById, addedPagesById);

            XDoc result = new XDoc("ul");
            result = BuildHtmlSiteMap(rootPage, result, allowedPagesById, filteredPagesById, depth, reverse);
            return result;
        }

        private static XDoc BuildHtmlSiteMap(PageBE current, XDoc doc, Dictionary<ulong, PageBE> allowedPagesById, Dictionary<ulong, PageBE> filteredPagesById, int depth, bool reverse) {
            doc.Start("li");
            string uri = Utils.AsPublicUiUri(current.Title);
            List<string> classValues = new List<string>();
            if (filteredPagesById.ContainsKey(current.ID))
                classValues.Add("statusRestricted");
            if (current.ID == DekiContext.Current.Instance.HomePageId)
                classValues.Add("statusHome");

            doc.Start("a").Attr("rel", "internal").Attr("href", uri).Attr("title", current.Title.AsPrefixedUserFriendlyPath()).Attr("pageid", current.ID);
            if(classValues.Count > 0) {
                doc.Attr("class", string.Join(" ", classValues.ToArray()));
            }
            doc.Value(current.Title.AsUserFriendlyName()).End();

            if(depth > 0 && !ArrayUtil.IsNullOrEmpty(current.ChildPages)) {
                PageBE[] visibleChildren = Array.FindAll(current.ChildPages, delegate(PageBE child) {
                    return allowedPagesById.ContainsKey(child.ID);
                });
                DreamContext context = DreamContext.CurrentOrNull;
                if(context != null) {
                    DekiContext deki = DekiContext.CurrentOrNull;
                    CultureInfo culture = HttpUtil.GetCultureInfoFromHeader(current.Language ?? ((deki != null) ? deki.Instance.SiteLanguage : string.Empty), context.Culture);
                    string[] paths = new string[visibleChildren.Length];
                    for(int i = 0; i < visibleChildren.Length; ++i) {
                        paths[i] = XUri.Decode(visibleChildren[i].Title.AsPrefixedDbPath()).Replace("//", "\uFFEF").Replace("/", "\t");
                    }
                    Array.Sort(paths, visibleChildren, new CompareInfoComparer(culture.CompareInfo));
                }
                if(visibleChildren.Length > 0) {
                    doc.Start("ul");
                    if (reverse) {
                        Array.Reverse(visibleChildren);
                    }
                    foreach (PageBE p in visibleChildren) {
                        doc = BuildHtmlSiteMap(p, doc, allowedPagesById, filteredPagesById, depth - 1, reverse);
                    }
                    doc.End();
                }
            }
            doc.End();
            return doc;
        }

        /// <summary>
        /// Builds a http://sitemaps.org compliant sitemap as used by google (https://www.google.com/webmasters/tools/docs/en/protocol.html)
        /// </summary>
        /// <param name="rootPage"></param>
        /// <returns></returns>
        public static XDoc BuildGoogleSiteMap(PageBE rootPage, string language) {

            IList<PageBE> pages = null;
            Dictionary<ulong, IList<ulong>> childrenInfo = null;
            DbUtils.CurrentSession.Pages_GetDescendants(rootPage, null, true, out pages, out childrenInfo, ConfigBL.GetInstanceSettingsValueAs<int>(ConfigBL.MAX_SITEMAP_SIZE_KEY, ConfigBL.MAX_SITEMAP_SIZE));

            PageBE[] allowedPages = PermissionsBL.FilterDisallowed(DekiContext.Current.User, pages, false, new Permissions[] { Permissions.BROWSE });
            Dictionary<ulong, PageBE> allowedPagesById = allowedPages.AsHash(e => e.ID);
            Dictionary<ulong, PageBE> addedPagesById = null;
            if(!string.IsNullOrEmpty(language)) {
                List<ulong> pagesToRemove = new List<ulong>();
                foreach(KeyValuePair<ulong, PageBE> page in allowedPagesById) {
                    if(!string.IsNullOrEmpty(page.Value.Language) && !StringUtil.EqualsInvariantIgnoreCase(page.Value.Language, language)) {
                        pagesToRemove.Add(page.Key);
                    }
                }
                foreach(ulong pageId in pagesToRemove) {
                    allowedPagesById.Remove(pageId);
                }
            }
            PageBL.AddParentsOfAllowedChildren(rootPage, allowedPagesById, addedPagesById);

            XDoc x = new XDoc("urlset", "http://www.google.com/schemas/sitemap/0.84");
            foreach (PageBE p in allowedPagesById.Values) {
                x.Start("url");
                x.Elem("loc", Utils.AsPublicUiUri(p.Title));
                x.Start("lastmod").Value(p.TimeStamp.ToString("yyyy-MM-dd")).End();
                x.End();
            }

            return x;
        }
    }
}

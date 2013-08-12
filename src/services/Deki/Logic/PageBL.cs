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
using System.Text;
using MindTouch.Deki.Data;
using MindTouch.Deki.Exceptions;
using MindTouch.Deki.Script.Runtime.Library;
using MindTouch.Dream;
using MindTouch.Xml;
using MindTouch.Tasking;
using MindTouch.Deki.Script;

namespace MindTouch.Deki.Logic {

    public class PageContentFilterSettings {

        //--- Fields ---
        private ParserMode parserMode = ParserMode.VIEW;

        //--- Properties ---
        public bool ExcludeInboundLinks { get; set; }
        public bool ExcludeOutboundLinks { get; set; }
        public bool IncludeContents { get; set; }
        public ParserMode ContentsMode {
            get { return parserMode; }
            set { parserMode = value; }
        }
        public string Revision { get; set; }
        public bool Xhtml { get; set; }
    }

    // Note (arnec): IPageBL will eventually be the interface to a non-static PageBL. In the meantime it
    // mirrors only the methods required by those not using PageBL directly and is implemented via PageBLAdapter
    public interface IPageBL {
        PageBE GetPageById(ulong id);
        IEnumerable<PageBE> GetAllPagesChunked(NS[] whitelist);
        XUri GetUriCanonical(PageBE page);
    }

    public class PageBLAdapter : IPageBL {
        public PageBE GetPageById(ulong id) {
            return PageBL.GetPageById(id);
        }

        public IEnumerable<PageBE> GetAllPagesChunked(NS[] whitelist) {
            return PageBL.GetAllPagesChunked(whitelist);
        }

        public XUri GetUriCanonical(PageBE page) {
            return PageBL.GetUriCanonical(page);
        }
    }

    public static class PageBL {

        //--- Class Fields ---
        private static readonly log4net.ILog _log = DekiLogManager.CreateLog();

        public static XDoc RetrievePageXDoc(PageBE page, uint pageId, ParserMode mode, string language, bool isInclude, int section, Title relToTitle, bool xhtml, out ParserResult parserResult) {
            uint contextPageId = pageId;
            if((mode == ParserMode.VIEW) && (contextPageId != uint.MaxValue) && page.Title.IsTemplate) {

                // NOTE (steveb): page being rendered is a template and a contextual page was specified; this means we're rendering a global template page
                PageBE contextPage = GetPageById(contextPageId);
                if(contextPage == null) {
                    parserResult = new ParserResult();
                    return null;
                }
                parserResult = DekiXmlParser.ParseGlobalTemplate(contextPage, page);
            } else {
                parserResult = DekiXmlParser.Parse(page, page.ContentType, language ?? page.Language, page.GetText(DbUtils.CurrentSession), mode, isInclude, section, null, relToTitle);
            }
            if(page.Title.IsTemplate && isInclude) {
                DekiXmlParser.PostProcessTemplateInsertBody(parserResult, page);
            }

            // post process tail element
            DekiXmlParser.PostProcessParserResults(parserResult);

            // BUGBUGBUG (steveb): we cannot properly restore an old title unless it had a display title set

            // wrap the result in a content tag and return it to the user
            XDoc result = new XDoc("content")
                    .Attr("type", parserResult.ContentType)
                    .Attr("etag", page.Etag)
                    .Attr("title", page.CustomTitle ?? page.Title.AsUserFriendlyName());

            // check if page contains unsafe content
            if(mode == ParserMode.EDIT) {
                result.Attr("unsafe", !DekiScriptLibrary.VerifyXHtml(parserResult.MainBody, true));
            }

            if(xhtml) {
                result.AddNodes(parserResult.Content);
            } else {

                // encode the result as nodes of text
                foreach(XDoc entry in parserResult.Content.Elements) {
                    if(entry.HasName("body")) {
                        result.Start("body").Attr("target", entry["@target"].AsText).Value(entry.ToInnerXHtml()).End();
                    } else {
                        result.Elem(entry.Name, entry.ToInnerXHtml());
                    }
                }
            }
            return result;
        }

        //--- Class Methods ---

        /// <summary>
        /// Retrieves the homepage object
        /// </summary>
        /// <returns></returns>
        public static PageBE GetHomePage() {
            return GetPageById(DekiContext.Current.Instance.HomePageId);
        }

        public static PageBE EnsureParent(bool isRedirect, Title title) {
            var resources = DekiContext.Current.Resources;
            PageBE parentPage = null;

            // Talk pages do not have a parent
            if(!title.IsTalk) {
                Title parentTitle = title.GetParent();

                // Attempt to retrieve the parent and create it if not found
                if(null != parentTitle) {
                    parentPage = GetPageByTitle(parentTitle);
                    if((parentPage.ID == 0) || (!isRedirect && parentPage.IsRedirect)) {
                        Save(parentPage, resources.Localize(DekiResources.EMPTY_PARENT_ARTICLE_TEXT()), DekiMimeType.DEKI_TEXT, null);
                    }
                }
            }
            return parentPage;
        }

        public static void ImportTemplatePages(PageBE page, Title[] templates) {

            // for each template sub page, obtain its content and create a new subpage with it under the current page
            foreach(Title template in templates) {
                PageBE templatePage = GetPageByTitle(template);
                if((0 != templatePage.ID) && (PermissionsBL.IsUserAllowed(DekiContext.Current.User, templatePage, Permissions.READ))) {
                    string[] segments = template.AsUnprefixedDbSegments();
                    segments[0] = page.Title.AsPrefixedDbPath();
                    Title newTitle = Title.FromPrefixedDbPath(String.Join("/", segments).Trim('/'), null);
                    PageBE newPage = GetPageByTitle(newTitle);
                    if(0 == newPage.ID) {
                        Save(newPage, null, templatePage.GetText(DbUtils.CurrentSession), templatePage.ContentType, templatePage.Title.DisplayName, templatePage.Language);
                    }
                }
            }
        }

        public static void UpdateLinks(PageBE page, Title[] links) {
            if(links == null) {

                // if there are no links, clear the stored link data for this page
                DbUtils.CurrentSession.Links_UpdateLinksForPage(page, null, null);
            } else {

                // attempt to obtain the page ID's of all internal links
                Dictionary<Title, ulong> ids = DbUtils.CurrentSession.Pages_GetIdsByTitles(links);
                List<Title> brokenLinks = new List<Title>();
                for(int i = 0; i < links.Length; i++) {
                    if(!ids.ContainsKey(links[i])) {
                        brokenLinks.Add(links[i]);
                    }
                }
                DbUtils.CurrentSession.Links_UpdateLinksForPage(page, ids.Values.ToList(), brokenLinks.Distinct().Select(e => e.AsPrefixedDbPath()).ToList());
            }
        }

        public static IList<PageBE> GetTalkPages(ICollection<PageBE> pages) {
            Title[] pageTitles = pages.Select(e => e.Title.AsTalk()).ToArray();
            return DbUtils.CurrentSession.Pages_GetByTitles(pageTitles);
        }

        public static XDoc GetPopularPagesXml(DreamContext context) {
            uint limit, offset;
            bool? sortAsc;

            Utils.GetOffsetAndCountFromRequest(context, 50, out limit, out offset);

            // extract the filter language
            string language = context.GetParam("language", null);
            if(null != language) {
                ValidatePageLanguage(language);
            }

            // retrieve popular pages
            IList<PageBE> pages = DbUtils.CurrentSession.Pages_GetPopular(language, offset, limit);

            // filter based on the user's permissions
            pages = PermissionsBL.FilterDisallowed(DekiContext.Current.User, pages, false, Permissions.BROWSE);

            // construct xml page list and inject view information into it
            XUri href = DekiContext.Current.ApiUri.At("pages", "popular");
            XDoc result = GetPageListXml(pages, "pages.popular", href);
            int i = 0;
            foreach(XDoc pageDoc in result[".//page"]) {
                pageDoc.Add(GetMetricsXml(pages[i], false));
                i++;
            }
            return result;
        }

        public static XDoc GetFavoritePagesForUser(UserBE user) {

            //retrieve favorite pages
            IList<PageBE> favorites = DbUtils.CurrentSession.Pages_GetFavoritesForUser(user.ID);

            // filter based on the user's permissions
            favorites = PermissionsBL.FilterDisallowed(DekiContext.Current.User, favorites, false, Permissions.BROWSE);

            //build xml
            return PageBL.GetPageListXml(favorites, "pages", DekiContext.Current.ApiUri.At("users", user.ID.ToString(), "favorites"));
        }

        public static PageBE GetPageById(ulong id) {
            return DbUtils.CurrentSession.Pages_GetByIds(new[] { id }).FirstOrDefault();
        }

        public static IList<PageBE> GetPagesByIdsPreserveOrder(IEnumerable<ulong> pageIds) {
            Dictionary<ulong, PageBE> pagesById = DbUtils.CurrentSession.Pages_GetByIds(pageIds).AsHash(e => e.ID);
            List<PageBE> pages = new List<PageBE>();

            //Get pages back in the order requested.
            foreach(uint pageid in pageIds) {
                PageBE p = null;
                if(pagesById.TryGetValue(pageid, out p)) {
                    pages.Add(p);
                }
            }
            return pages;
        }

        public static PageBE GetPageByTitle(Title title) {
            PageBE page;
            IList<PageBE> pages = DbUtils.CurrentSession.Pages_GetByTitles(new List<Title>() { title });
            if(ArrayUtil.IsNullOrEmpty(pages)) {

                //TODO (Max): I really wanna get rid of this dummy page creation and return null!
                page = new PageBE();
                page.Title = title;
            } else {
                page = pages[0];
            }
            return page;
        }

        public static IEnumerable<PageBE> GetAllPagesChunked(NS[] whitelist) {
            uint limit = 1000;
            uint offset = 0;
            while(true) {
                var chunk = DbUtils.CurrentSession.Pages_GetByNamespaces(whitelist, offset, limit);
                _log.DebugFormat("got chunk of {0} pages", chunk.Count);
                foreach(var page in chunk) {
                    yield return page;
                }
                if(chunk.Count < limit) {
                    yield break;
                }
                offset += limit;
            }
        }

        public static IList<PageBE> GetParents(PageBE page) {

            // check for trivial case where the base page is the homepage
            if(page.Title.IsHomepage) {
                return new List<PageBE>() { page };
            }

            // parent id of talk pages is 0, but the parent chain is the same as the front page
            if(page.Title.IsTalk) {
                PageBE frontPage = GetPageByTitle(page.Title.AsFront());
                if(frontPage != null && frontPage.ID != 0) {

                    //Lookup the ancestor chain of the associated front page
                    IList<ulong> frontPageParentIds = DbUtils.CurrentSession.Pages_GetParentIds(frontPage);

                    //replace the starting page of the front page's ancestors chain with the talk page
                    if(frontPageParentIds != null && frontPageParentIds.Count > 1 && frontPageParentIds[0] == frontPage.ID) {
                        frontPageParentIds[0] = page.ID;
                        return GetPagesByIdsPreserveOrder(frontPageParentIds);
                    }
                }
            }
            return GetPagesByIdsPreserveOrder(DbUtils.CurrentSession.Pages_GetParentIds(page));
        }

        public static ICollection<PageBE> GetChildren(PageBE parentPage, bool filterRedirects) {
            if(parentPage != null) {
                uint parentId = parentPage.Title.IsRoot ? 0 : (uint)parentPage.ID;
                return new List<PageBE>(DbUtils.CurrentSession.Pages_GetChildren(parentId, parentPage.Title.Namespace, filterRedirects));
            }
            return null;
        }

        public static IList<PageBE> GetDescendants(PageBE rootPage, bool filterOutRedirects) {
            IList<PageBE> pages;
            Dictionary<ulong, IList<ulong>> childrenInfo;
            DbUtils.CurrentSession.Pages_GetDescendants(rootPage, null, filterOutRedirects, out pages, out childrenInfo, ConfigBL.GetInstanceSettingsValueAs<int>(ConfigBL.MAX_SITEMAP_SIZE_KEY, ConfigBL.MAX_SITEMAP_SIZE));
            return pages;
        }

        public static XDoc GetSubpageXml(PageBE page, uint limit, uint offset) {
            ICollection<PageBE> subpages = GetChildren(page, true) ?? new List<PageBE>(0);
            PageBE[] browseablePages = PermissionsBL.FilterDisallowed(DekiContext.Current.User, new List<PageBE>(subpages), false, Permissions.BROWSE);

            // sort by title and perform offset+limit
            browseablePages = SortPagesByTitle(browseablePages);
            int totalcount = browseablePages.Length;
            browseablePages = browseablePages.Skip(offset.ToInt()).Take(limit.ToInt()).ToArray();
            XDoc ret = new XDoc("subpages")
                .Attr("totalcount", totalcount)
                .Attr("count", browseablePages.Length)
                .Attr("href", DekiContext.Current.ApiUri.At("pages", page.ID.ToString(), "subpages"));

            // loop over all sub-pages
            foreach(PageBE subpage in browseablePages) {

                // a terminalNode is a childpage that contains no files and no children.
                bool terminalNode = ((subpage.AttachmentCount ?? 0) == 0) && ArrayUtil.IsNullOrEmpty(subpage.ChildPageIds);

                // add sub-page document
                XDoc subPageXml = GetPageXml(subpage, "subpage");
                subPageXml.Attr("terminal", terminalNode);
                subPageXml.Attr("subpages", !ArrayUtil.IsNullOrEmpty(subpage.ChildPageIds));
                ret.Add(subPageXml);
            }
            return ret;
        }

        public static PageBE PopulateDescendants(PageBE rootPage) {
            Dictionary<ulong, PageBE> pagesById = null;
            return PopulateDescendants(rootPage, null, out pagesById, ConfigBL.GetInstanceSettingsValueAs<int>(ConfigBL.MAX_SITEMAP_SIZE_KEY, ConfigBL.MAX_SITEMAP_SIZE));
        }

        /// <summary>
        /// Given a page, this will populate every descendant's ChildPages
        /// </summary>
        /// <param name="rootPage"></param>
        public static PageBE PopulateDescendants(PageBE rootPage, string language, out Dictionary<ulong, PageBE> pagesById, int limit) {
            IList<PageBE> pages;
            Dictionary<ulong, IList<ulong>> childrenInfo = null;
            DbUtils.CurrentSession.Pages_GetDescendants(rootPage, language, true, out pages, out childrenInfo, limit);
            pagesById = pages.AsHash(e => e.ID);
            foreach(PageBE currentPage in pagesById.Values) {
                IList<ulong> childrenForPage = null;
                if(childrenInfo.TryGetValue(currentPage.ID, out childrenForPage)) {
                    List<PageBE> childPages = new List<PageBE>();
                    foreach(ulong childPageId in childrenForPage) {
                        PageBE childPage = null;

                        if(pagesById.TryGetValue(childPageId, out childPage)) {
                            childPages.Add(childPage);
                        } else {
                            //This shouldn't happen.
                        }
                    }
                    currentPage.ChildPages = SortPagesByTitle(childPages.ToArray());
                } else {
                    // This shouldn't happen.
                }
            }

            return pagesById[rootPage.ID];
        }

        public static PageBE CreateUserHomePage(UserBE user) {
            var resources = DekiContext.Current.Resources;
            if(user == null)
                return null;

            PageBE userHomePage = UserBL.GetHomePage(user);
            if(userHomePage.ID == 0 || userHomePage.IsRedirect) {

                try {

                    // get contents for new page
                    var homepageContent = resources.Localize(DekiResources.NEWUSERPAGETEXT());
                    string homepageContentType = DekiMimeType.DEKI_TEXT;

                    //Content/new-user behavior for new homepages.
                    if(!string.IsNullOrEmpty(DekiContext.Current.Instance.ContentNewUser)) {

                        Title contentNewUserTitle = Title.FromPrefixedDbPath(DekiContext.Current.Instance.ContentNewUser, null);
                        PageBE contents = GetPageByTitle(contentNewUserTitle);
                        if(contents != null && contents.ID != 0) {
                            contents = ResolveRedirects(contents);
                        }

                        //Found page pointed to by content/new-user
                        if(contents != null && contents.ID != 0) {

                            //Save the executed version of the template. Run this as the owner of the homepage
                            PermissionsBL.ImpersonationBegin(user);
                            ParserResult p = DekiXmlParser.Parse(contents, ParserMode.EDIT, -1, true);
                            homepageContentType = p.ContentType;
                            homepageContent = p.BodyText;
                            PermissionsBL.ImpersonationEnd();
                        } else {
                            _log.WarnFormat("Failed to set the contents of a user's homepage for user '{0}' from page '{1}'", user.Name, contentNewUserTitle);
                        }
                    }

                    //Need to impersonate an admin user if no context is set or performing anonymous user creation
                    if(DekiContext.Current.User == null || UserBL.IsAnonymous(DekiContext.Current.User) || !PermissionsBL.IsUserAllowed(DekiContext.Current.User, Permissions.ADMIN)) {
                        PermissionsBL.ImpersonationBeginOfAdmin();
                    }

                    Save(userHomePage, null, homepageContent, homepageContentType, !string.IsNullOrEmpty(user.RealName) ? user.RealName : null, null);

                    //Set a grant for the new user on their homepage
                    if(!string.IsNullOrEmpty(DekiContext.Current.Instance.HomePageGrantRole)) {
                        RoleBE roleForGrant = PermissionsBL.GetRoleByName(DekiContext.Current.Instance.HomePageGrantRole);

                        if(roleForGrant != null) {
                            GrantBE userHomePageGrant = new GrantBE();
                            userHomePageGrant.Role = roleForGrant;
                            userHomePageGrant.RoleId = roleForGrant.ID;
                            userHomePageGrant.UserId = user.ID;
                            userHomePageGrant.Type = GrantType.USER;
                            userHomePageGrant.TimeStamp = DateTime.UtcNow;
                            userHomePageGrant.PageId = (uint)userHomePage.ID;
                            userHomePageGrant.CreatorUserId = DekiContext.Current.User.ID;
                            try {
                                PermissionsBL.ApplyDeltaPagePermissions(userHomePage, null, new List<GrantBE>(new GrantBE[] { userHomePageGrant }), new List<GrantBE>(), false);
                            } catch(Exception x) {
                                _log.WarnExceptionFormat(x, "Failed to apply a grant with role '{0}' to {1}'s homepage.", DekiContext.Current.Instance.HomePageGrantRole, user.Name);
                            }
                        }
                    }
                } finally {
                    PermissionsBL.ImpersonationEnd();
                }
            }
            return userHomePage;
        }

        public static PageBE GetTargetForRedirectPage(PageBE redirectPage) {
            PageBE targetPage = null;

            if(redirectPage == null || !redirectPage.IsRedirect)
                return null;

            ParserResult parserResult = DekiXmlParser.Parse(redirectPage, ParserMode.VIEW_NO_EXECUTE);
            if(null != parserResult.RedirectsToTitle) {
                targetPage = GetPageByTitle(parserResult.RedirectsToTitle);
            } else {
                targetPage = null;
            }

            return targetPage;
        }


        public static XUri ResolveRedirectUri(PageBE startPage) {
            if(!startPage.IsRedirect) {
                return null;
            }
            PageBE finalRedirect = ResolveRedirects(startPage);
            if(!finalRedirect.IsRedirect) {
                return null;
            }
            ParserResult parserResult = DekiXmlParser.Parse(finalRedirect, ParserMode.VIEW_NO_EXECUTE);
            return parserResult.RedirectsToUri;
        }

        public static IList<PageBE> GetRedirectsApplyPermissions(PageBE aliasesForPage) {
            IList<PageBE> redirects = Pages_GetRedirects(aliasesForPage.ID);
            if(redirects != null)
                redirects = PermissionsBL.FilterDisallowed(DekiContext.Current.User, redirects, false, Permissions.BROWSE);
            return redirects;
        }

        public static IList<PageBE> Pages_GetRedirects(ulong pageId) {
            IList<PageBE> ret = null;
            DbUtils.CurrentSession.Pages_GetRedirects(new[] { pageId }.ToList()).TryGetValue(pageId, out ret);
            return ret;
        }

        public static string ValidatePageLanguage(string language) {
            try {
                CultureInfo ci = new CultureInfo(language);
                return ci.NativeName;
            } catch(ArgumentException) {
                throw new PageLanguageInvalidArgumentException();
            }
        }

        private static PageBE SetPageLanguage(PageBE page, string language, bool force) {
            if(!page.Language.EqualsInvariantIgnoreCase(language)) {

                if(page.Title.IsTalk && !force) {
                    throw new TalkPageLanguageCannotBeSetConflictException();
                }

                string nativeName = ValidatePageLanguage(language);
                page.Language = language;
                page.Touched = DekiContext.Current.Now;
                DbUtils.CurrentSession.Pages_Update(page);
                RecentChangeBL.AddPageMetaRecentChange(page.Touched, page, DekiContext.Current.User, DekiResources.PAGE_LANGUAGE_CHANGED(nativeName));
                DekiContext.Current.Instance.EventSink.PageUpdate(DekiContext.Current.Now, page, DekiContext.Current.User);

                //Set the talk page language as well
                if(!page.Title.IsTalk) {
                    PageBE talkPage = PageBL.GetPageByTitle(page.Title.AsTalk());
                    if(talkPage != null && talkPage.ID != 0) {
                        SetPageLanguage(talkPage, language, true);
                    }
                }
            }
            return page;
        }

        public static OldBE InsertOld(PageBE oldPage, ulong restoredOldId) {
            OldBE old = new OldBE();
            CopyPageToOld(oldPage, old);
            try {
                uint oldId = DbUtils.CurrentSession.Old_Insert(old, restoredOldId);
                if(oldId == 0) {
                    old = null;
                } else {
                    old.ID = oldId;
                }
            } catch(PageConcurrencyException e) {

                // TODO (arnec): Remove this once OldBE contains the page Id and the OldDA can throw this exception properly
                e.PageId = oldPage.ID;
                throw;
            }
            return old;
        }

        public static void Touch(PageBE page, DateTime timestamp) {
            page.Touched = timestamp;
            DbUtils.CurrentSession.Pages_Update(page);
        }

        public static void UpdatePropertiesFromXml(PageBE page, XDoc settings) {

            // extract the page language and check if it has changed         
            string language = settings["language"].Contents;
            SetPageLanguage(page, language, false);
        }

        public static OldBE Save(PageBE page, string text, string contentType, string language) {
            return Save(page, null, text, contentType, null, language);
        }

        public static OldBE Save(PageBE page, string userComment, string text, string contentType, string displayName, string language) {
            bool conflict;
            return Save(page, null, userComment, text, contentType, displayName, language, -1, null, DateTime.UtcNow, 0, true, false, null, false, out conflict);
        }

        public static OldBE Save(PageBE page, OldBE previous, string userComment, string text, string contentType, string displayName, string language, int section, string xpath, DateTime timeStamp, ulong restoredPageId, bool loggingEnabled, bool removeIllegalElements, Title relToTitle, bool overwrite, out bool conflict) {
            return Save(page, previous, userComment, text, contentType, displayName, language, section, xpath, timeStamp, restoredPageId, loggingEnabled, removeIllegalElements, relToTitle, overwrite, DekiContext.Current.User.ID, out conflict);
        }

        public static OldBE Save(PageBE page, OldBE previous, string userComment, string text, string contentType, string displayName, string language, int section, string xpath, DateTime timeStamp, ulong restoredPageId, bool loggingEnabled, bool removeIllegalElements, Title relToTitle, bool overwrite, uint authorId, out bool conflict) {

            // NOTE (steveb):
            //  page: most recent page about to be overwritten
            //  previous: (optional) possible earlier page on which the current edit is based upon

            conflict = false;
            bool isNewForEventContext = page.ID == 0 || page.IsRedirect;

            // check save permissions
            IsAccessAllowed(page, 0 == page.ID ? Permissions.CREATE : Permissions.UPDATE, false);

            // validate the save
            if((0 == page.ID) && ((-1 != section) || (null != xpath))) {
                throw new PageEditExistingSectionInvalidOperationException();
            }

            // displaynames entered by user are trimmed
            if(displayName != null) {
                displayName = displayName.Trim();
            }

            if(!Title.FromDbPath(page.Title.Namespace, page.Title.AsUnprefixedDbPath(), displayName).IsValid) {
                throw new PageInvalidTitleConflictException();
            }

            // load old contents into current page when a section is edited
            ParserResult alternate = new ParserResult();
            ParserResult original = new ParserResult();
            if(previous != null) {

                // parse most recent version as alternate
                alternate = DekiXmlParser.Parse(page, ParserMode.RAW);

                // parse base version for three way diff
                string pageContentType = page.ContentType;
                string pageText = page.GetText(DbUtils.CurrentSession);
                page.ContentType = previous.ContentType;
                page.SetText(previous.Text);
                original = DekiXmlParser.Parse(page, ParserMode.RAW);
                page.ContentType = pageContentType;
                page.SetText(pageText);
            }

            // ensure the parent exists
            PageBE parent = EnsureParent(DekiXmlParser.REDIRECT_REGEX.IsMatch(text), page.Title);
            if(null != parent) {
                page.ParentID = parent.Title.IsRoot ? 0 : parent.ID;
            }

            // Explicitly setting the language of a talk page is not valid
            if(page.Title.IsTalk && !string.IsNullOrEmpty(language)) {
                throw new TalkPageLanguageCannotBeSetConflictException();
            }

            // Language is set in this order: explicitly given, already set, language of parent
            language = language ?? page.Language ?? (null != parent ? parent.Language : String.Empty);

            // talk pages always get their language from their corresponding front page
            if(page.Title.IsTalk) {
                PageBE frontPage = PageBL.GetPageByTitle(page.Title.AsFront());
                if(frontPage != null && frontPage.ID != 0) {
                    language = frontPage.Language;
                }
            }

            string nativeName = ValidatePageLanguage(language);

            // parse the content
            ParserResult parserResult = DekiXmlParser.ParseSave(page, contentType, language, text, section, xpath, removeIllegalElements, relToTitle);
            OldBE old = null;
            var comment = new DekiResourceBuilder(userComment ?? string.Empty);

            // check if this is a new page
            if(0 == page.ID) {
                _log.DebugFormat("saving new page {0}", page.Title);
                AuthorizePage(DekiContext.Current.User, Permissions.CREATE, parent, false);

                if(0 == restoredPageId) {
                    if(!comment.IsEmpty) {
                        comment.Append("; ");
                    }
                    comment.Append(DekiResources.PAGE_CREATED());
                    if((null == parserResult.RedirectsToTitle) && (null == parserResult.RedirectsToUri)) {
                        comment.Append(", ");
                        comment.Append(DekiResources.PAGE_DIFF_SUMMARY_ADDED(Utils.GetPageWordCount(parserResult.MainBody)));
                    }
                    page.MinorEdit = false;
                    page.IsNew = true;
                }
            }

            // if this is an existing page, ensure the content has changed and save the current page information 
            else {
                _log.DebugFormat("saving existing page {0}", page.ID);

                // prevent creating a redirect on a page that has non-redirect children
                if((null != parserResult.RedirectsToTitle) || (null != parserResult.RedirectsToUri)) {
                    IList<PageBE> children = DbUtils.CurrentSession.Pages_GetChildren((uint)page.ID, page.Title.Namespace, true);
                    if(0 < children.Count) {
                        throw new PageInvalidRedirectConflictException();
                    }
                }

                bool displayNameChanged = page.Title.DisplayName != displayName && displayName != null;
                bool languageChanged = !page.Language.EqualsInvariant(language);
                if(parserResult.ContentType == page.ContentType) {
                    if(parserResult.BodyText.EqualsInvariant(page.GetText(DbUtils.CurrentSession))) {
                        if(!displayNameChanged && !languageChanged && !overwrite) {
                            return null;
                        }
                    } else {
                        int maxDelta = DekiContext.Current.Instance.MaxDiffSize;

                        // merge changes
                        if(previous != null) {
                            conflict = true;
                            try {
                                XDoc mergeBody = XDocDiff.Merge(original.MainBody, alternate.MainBody, parserResult.MainBody, maxDelta, ArrayMergeDiffPriority.Right, out conflict);
                                if(mergeBody == null) {

                                    // NOTE (steveb): for some reason, Merge() thinks that returning null is not a conflict.
                                    conflict = true;

                                    // merge failed, use the submitted body instead
                                    mergeBody = parserResult.MainBody;
                                }
                                parserResult = DekiXmlParser.ParseSave(page, page.ContentType, page.Language, mergeBody.ToInnerXHtml(), -1, null, removeIllegalElements, relToTitle);
                            } catch(Exception e) {
                                _log.Error("Save", e);
                            }
                        }
                        if(!comment.IsEmpty) {
                            comment.Append("; ");
                        }
                        if(page.IsRedirect) {
                            comment.Append(DekiResources.PAGE_DIFF_SUMMARY_ADDED(Utils.GetPageWordCount(parserResult.MainBody)));
                        } else {
                            comment.Append(Utils.GetPageDiffSummary(page, page.GetText(DbUtils.CurrentSession), page.ContentType, parserResult.BodyText, parserResult.ContentType, maxDelta));
                        }
                    }
                } else {
                    if(!comment.IsEmpty) {
                        comment.Append("; ");
                    }
                    comment.Append(DekiResources.PAGE_CONTENTTYPE_CHANGED(parserResult.ContentType));
                }

                if(displayNameChanged) {
                    if(!comment.IsEmpty) {
                        comment.Append("; ");
                    }
                    comment.Append(DekiResources.PAGE_DISPLAYNAME_CHANGED(displayName));

                }

                if(languageChanged) {
                    if(!comment.IsEmpty) {
                        comment.Append("; ");
                    }
                    comment.Append(DekiResources.PAGE_LANGUAGE_CHANGED(nativeName));

                    // set the language on the talk page as well.
                    if(!page.Title.IsTalk) {
                        PageBE talkPage = PageBL.GetPageByTitle(page.Title.AsTalk());
                        if(talkPage != null && talkPage.ID != 0) {
                            SetPageLanguage(talkPage, language, true);
                        }
                    }
                }
                old = InsertOld(page, 0);
                page.MinorEdit = (page.UserID == DekiContext.Current.User.ID) && (DateTime.Now < page.TimeStamp.AddMinutes(15));
                page.IsNew = false;
            }

            // update the page information to reflect the new content
            var resources = DekiContext.Current.Resources;
            page.Comment = comment.Localize(resources);
            page.ContentType = parserResult.ContentType;
            page.Language = language;
            page.UserID = authorId;
            var bodyText = string.Empty;
            if(parserResult.RedirectsToTitle != null) {
                page.IsRedirect = true;
                bodyText = "#REDIRECT [[" + parserResult.RedirectsToTitle.AsPrefixedDbPath() + "]]";
            } else if(parserResult.RedirectsToUri != null) {
                page.IsRedirect = true;
                bodyText = "#REDIRECT [[" + parserResult.RedirectsToUri + "]]";
            } else {
                page.IsRedirect = false;
                bodyText = parserResult.BodyText;
            }
            page.SetText(bodyText);
            page.UseCache = !parserResult.HasScriptContent;
            page.TIP = parserResult.Summary;
            page.Touched = page.TimeStamp = timeStamp;

            // nametype and displayname logic
            if(string.IsNullOrEmpty(displayName) && (page.ID == 0)) {

                // new page created without a title: title comes from the name
                page.Title.DisplayName = page.Title.AsUserFriendlyDisplayName();
            } else if(!string.IsNullOrEmpty(displayName) || page.Title.IsHomepage) {

                // title is provided: title set from provided value
                page.Title.DisplayName = displayName;
            } else {
                // preserve the display name of the page
            }

            // Note (arnec): Using Encoding.UTF8 because the default is Encoding.Unicode which produces a different md5sum than expected from ascii
            page.Etag = StringUtil.ComputeHashString(bodyText, Encoding.UTF8);

            // insert or update the page
            if(0 == page.ID) {
                if(restoredPageId == 0) {

                    // only set the revision if this isn't a restore.
                    page.Revision = 1;
                }
                ulong pageId = DbUtils.CurrentSession.Pages_Insert(page, restoredPageId);
                if(pageId != 0) {

                    // the ID is set on the passed in page object
                    page.ID = pageId;
                    if(loggingEnabled) {
                        RecentChangeBL.AddNewPageRecentChange(page.TimeStamp, page, DekiContext.Current.User, comment);
                    }

                    // Copy permissions from parent if the page is a child of homepage or Special:
                    ulong parentPageId = page.ParentID;
                    if((parentPageId == 0) && (parent != null) && (page.Title.IsMain || page.Title.IsSpecial)) {
                        parentPageId = parent.ID;
                    }
                    if(parentPageId > 0) {
                        DbUtils.CurrentSession.Grants_CopyToPage(parentPageId, page.ID);

                        // refresh the entity since the restriction may have changed and copy the state to passed in page object
                        PageBE temp = GetPageById(page.ID);
                        temp.Copy(page);
                    }

                    // never log creation of userhomepages to recentchanges
                    if(loggingEnabled && page.Title.IsUser && page.Title.GetParent().IsHomepage) {
                        loggingEnabled = false;
                    }
                }
            } else {
                page.Revision++;
                DbUtils.CurrentSession.Pages_Update(page);
                if(loggingEnabled) {
                    RecentChangeBL.AddEditPageRecentChange(page.TimeStamp, page, DekiContext.Current.User, comment, old);
                }
            }
            try {
                if(null != parserResult.Templates) {
                    ImportTemplatePages(page, parserResult.Templates.ToArray());
                }
                if(null != parserResult.Tags) {
                    ImportPageTags(page, parserResult.Tags.ToArray());
                }
                if(null != parserResult.Links) {
                    UpdateLinks(page, parserResult.Links.ToArray());
                }
            } finally {
                if(isNewForEventContext) {
                    DekiContext.Current.Instance.EventSink.PageCreate(DekiContext.Current.Now, page, DekiContext.Current.User);
                } else {
                    DekiContext.Current.Instance.EventSink.PageUpdate(DekiContext.Current.Now, page, DekiContext.Current.User);
                }
            }
            return old;
        }

        public static void IncrementViewCount(PageBE page) {
            DbUtils.CurrentSession.Pages_UpdateViewCount(page.ID);
            if(DekiContext.Current.Instance.PageViewEventsEnabled) {
                DekiContext.Current.Instance.EventSink.PageViewed(DekiContext.Current.Now, page, DekiContext.Current.User);
            }
        }

        public static uint GetViewCount(ulong pageId) {
            return DbUtils.CurrentSession.Pages_GetViewCount(pageId);
        }

        private static void ImportPageTags(PageBE page, string[] tags) {
            var tagBL = new TagBL();
            tagBL.PutTags(page, tags);
        }

        public static void RevertPageFromRevision(PageBE pageToRevert, int sourceRevisionNumber) {
            var resources = DekiContext.Current.Resources;

            // nothing to do if the revert revision is the same as the current revision 
            if(sourceRevisionNumber == (int)pageToRevert.Revision) {
                return;
            }

            OldBE oldRev = GetOldRevisionForPage(pageToRevert, sourceRevisionNumber);
            if(oldRev.IsHidden) {

                // BUGBUGBUG (steveb): why do we have a hard-coded english string here?!?
                throw new PageHiddenRevisionMustBeUnhiddenConflictException();
            }
            if(oldRev.ID != 0) {

                // generate comment string for operation
                var comment = resources.Localize(DekiResources.REVERTED(pageToRevert.Title.AsPrefixedUserFriendlyPath(), oldRev.Revision, oldRev.TimeStamp));

                // get the old displayname ...
                var oldDisplayName = oldRev.DisplayName;
                if(string.IsNullOrEmpty(oldDisplayName) && pageToRevert.ID == DekiContext.Current.Instance.HomePageId) {

                    // ... and if it's the home page and the title was null, use the site name
                    oldDisplayName = ConfigBL.GetInstanceSettingsValue("ui/sitename", pageToRevert.Title.DisplayName);
                }

                // (bug 7056) set author of page to author of old page since we're reverting
                bool conflict;
                Save(pageToRevert, null, comment, oldRev.Text, oldRev.ContentType, oldDisplayName, oldRev.Language, -1, null, DateTime.UtcNow, 0, true, false, null, true, oldRev.UserID, out conflict);
            } else {
                throw new PageWithRevisionNotFoundException();
            }
        }

        public static XDoc BuildParentPageXmlTree(PageBE page, bool verbose) {
            XDoc ret = XDoc.Empty;
            if(page.ID == DekiContext.Current.Instance.HomePageId) {
                return ret;
            }

            IList<PageBE> parentPages = GetParents(page);

            if(parentPages.Count > 1) {
                for(int i = 1; i < parentPages.Count; i++) {
                    XDoc pageSummary = verbose ? GetPageXmlVerbose(parentPages[i], "parent") : GetPageXml(parentPages[i], "parent");
                    if(ret.IsEmpty) {
                        ret = pageSummary;
                    } else {
                        XDoc cursor = XDoc.Empty;
                        if(ret.HasName("page.parent")) {
                            cursor = ret;
                        }
                        foreach(XDoc p in ret[".//page.parent"]) {
                            cursor = p;
                        }
                        cursor.Add(pageSummary);
                    }
                }
            }
            return ret;
        }

        public static OldBE[] ModifyRevisionVisibility(PageBE page, XDoc request, string comment) {
            List<OldBE> revisionsToHide = new List<OldBE>();
            List<OldBE> revisionsToUnhide = new List<OldBE>();
            ulong? id = null;
            ulong? revNum = null;
            bool? hide = null;
            uint currentUserId = DekiContext.Current.User.ID;
            DateTime currentTs = DateTime.UtcNow;

            foreach(XDoc pageDoc in request["/revisions/page"]) {
                id = pageDoc["@id"].AsULong;

                //Provided id of all page revisions must match the page id
                if(id != null && id.Value != page.ID) {
                    throw new MismatchedIdInvalidArgumentException();
                }

                revNum = pageDoc["@revision"].AsULong;
                if((revNum ?? 0) <= 0) {
                    throw new RatingInvalidArgumentException();
                }

                //Hiding the head revision is not allowed. Reasons include:
                //* Behavior of search indexing undefined
                //* Behavior of accessing HEAD revision is undefined
                if(revNum.Value == page.Revision) {
                    throw new HideHeadInvalidOperationException();
                }

                hide = pageDoc["@hidden"].AsBool;
                if(hide == null) {
                    throw new HiddenAttributeInvalidArgumentException();
                }

                OldBE rev = GetOldRevisionForPage(page, (int)revNum.Value);
                if(rev == null) {
                    throw new RevisionNotFoundInvalidArgumentException();
                }

                if(hide.Value != rev.IsHidden) {
                    if(hide.Value) {
                        revisionsToHide.Add(rev);
                    } else {
                        revisionsToUnhide.Add(rev);
                    }
                }
            }

            if(revisionsToUnhide.Count == 0 && revisionsToHide.Count == 0) {
                throw new NoRevisionToHideUnHideInvalidOperationException();
            }

            foreach(OldBE revToHide in revisionsToHide) {
                revToHide.IsHidden = true;
                revToHide.MetaXml.RevisionHiddenUserId = currentUserId;
                revToHide.MetaXml.RevisionHiddenTimestamp = currentTs;
                revToHide.MetaXml.RevisionHiddenComment = comment;
                DbUtils.CurrentSession.Old_Update(revToHide);
            }

            if(revisionsToUnhide.Count > 0) {
                PermissionsBL.CheckUserAllowed(DekiContext.Current.User, Permissions.ADMIN);
            }

            foreach(OldBE revToUnhide in revisionsToUnhide) {
                revToUnhide.IsHidden = false;
                revToUnhide.MetaXml.RevisionHiddenUserId = null;
                revToUnhide.MetaXml.RevisionHiddenTimestamp = null;
                revToUnhide.MetaXml.RevisionHiddenComment = null;
                DbUtils.CurrentSession.Old_Update(revToUnhide);
            }

            revisionsToHide.AddRange(revisionsToUnhide);
            return revisionsToHide.ToArray();
        }

        public static PageBE SetPageDisplayTitle(PageBE page, string displayName) {
            if(StringUtil.EqualsInvariant(page.Title.DisplayName, displayName)) {
                return page;
            }

            page.Title.DisplayName = displayName;
            DekiResource comment = null == displayName ? DekiResources.PAGE_DISPLAYNAME_RESET() : DekiResources.PAGE_DISPLAYNAME_CHANGED(displayName);
            var resources = DekiContext.Current.Resources;

            page.Comment = resources.Localize(comment);
            DbUtils.CurrentSession.Pages_Update(page);
            RecentChangeBL.AddPageMetaRecentChange(DekiContext.Current.Now, page, DekiContext.Current.User, comment);
            DekiContext.Current.Instance.EventSink.PageUpdate(DekiContext.Current.Now, page, DekiContext.Current.User);
            return page;
        }

        #region Move page methods

        public static IList<PageBE> MovePage(PageBE sourcePage, PageBE newParentPage, string newPageName, string newPageTitle) {
            Title moveTitle = new Title(sourcePage.Title);

            // parent page provided?
            if(newParentPage != null) {
                moveTitle = newParentPage.Title.WithUserFriendlyName(sourcePage.Title.AsSegmentName(), sourcePage.Title.DisplayName);
            }

            // maintain old page title (which was derived from name) if a displayname doesn't already exist and one not provided
            if(string.IsNullOrEmpty(newPageTitle) && string.IsNullOrEmpty(sourcePage.Title.DisplayName)) {
                newPageTitle = sourcePage.Title.AsUserFriendlyDisplayName();
            }

            // compute renamed title if name or title are provided
            if((newPageName != null) || (newPageTitle != null)) {
                try {
                    moveTitle = moveTitle.Rename(newPageName, newPageTitle);
                } catch(ArgumentException x) {
                    throw new TitleRenameNameInvalidArgumentException(x.Message);
                }
            }
            return MovePage(sourcePage, moveTitle, false);
        }

        public static IList<PageBE> MovePage(PageBE sourcePage, string to, string displayName) {
            Title t = Title.FromUIUri(null, to, false);
            if(string.IsNullOrEmpty(displayName)) {
                if(GetPathType(sourcePage) == PagePathType.LINKED) {

                    // if page is linked and no displayname is provided, rebuild displayname from path
                    t.DisplayName = t.AsUserFriendlyDisplayName();
                } else {

                    // preserve existing displayname if not linked
                    t.DisplayName = sourcePage.Title.DisplayName;
                }
            } else {

                // a displayname is provided
                t.DisplayName = displayName;
            }
            return MovePage(sourcePage, t, false);
        }

        public static IList<PageBE> MovePage(PageBE sourcePage, Title title, bool allowFixedPathRename) {

            //Get a list of pages (including child pages)
            //Can all pages be moved (Authorization/Namespace check/Homepage)
            //Does page exist at proposed title(s)? (including all child titles)
            //  IsRedirect: Delete it!
            //  Not a redirect: Exit with error.
            //Update page (title/ns/parent_id) and children (title/ns)
            //Update Olds, Archive (title + ns)
            //Write to RC
            //Update links

            // check if this page cannot be moved, but only the display name can be changed
            if(!allowFixedPathRename && (GetPathType(sourcePage) == PagePathType.FIXED)) {
                if(title != sourcePage.Title) {
                    throw new PageMoveTitleConflictException();
                }
            }

            // check if there's nothing to do
            if((sourcePage.Title == title) && title.DisplayName.EqualsInvariant(sourcePage.Title.DisplayName)) {
                return new List<PageBE>();
            }
            IList<PageBE> ret;

            // page path did not change but display name is changed: just change the displayname
            if((sourcePage.Title == title) && !title.DisplayName.EqualsInvariant(sourcePage.Title.DisplayName)) {

                // Path isn't being modified but only the displayname: Change the displayname and return.
                sourcePage = SetPageDisplayTitle(sourcePage, title.DisplayName);
                List<PageBE> r = new List<PageBE>();
                r.Add(sourcePage);
                return r;
            }

            //Validation of move based on source page + destination title/ns
            ValidatePageMoveForRootNode(sourcePage, title);

            //Retrieve all child pages of source page and associated talk pages, including source page
            List<PageBE> pagesToMoveList = new List<PageBE>();

            List<PageBE> childRedirectsToRecreate = new List<PageBE>();

            //Retrieve all descendant pages including redirects. Separate out the redirects to be deleted and readded later.
            foreach(PageBE p in GetDescendants(sourcePage, false)) {
                if(p.IsRedirect) {
                    childRedirectsToRecreate.Add(p);
                } else {
                    pagesToMoveList.Add(p);
                }
            }

            // Ensure that redirect text is populated since the page is deleted before it would be lazy loaded
            foreach(PageBE redirect in childRedirectsToRecreate) {
                redirect.GetText(DbUtils.CurrentSession);
            }

            pagesToMoveList.AddRange(GetTalkPages(pagesToMoveList));
            PageBE[] pagesToMove = pagesToMoveList.ToArray();

            //Determine new page titles for every moved page
            Dictionary<ulong, Title> newTitlesByPageId = BuildNewTitlesForMovedPages(sourcePage.Title, pagesToMove, title);

            //Will throw an unauthorized exception
            PermissionsBL.FilterDisallowed(DekiContext.Current.User, pagesToMove, true, Permissions.UPDATE);

            Title[] newTitles = new Title[newTitlesByPageId.Count];
            newTitlesByPageId.Values.CopyTo(newTitles, 0);

            TransactionBE newTrans = new TransactionBE();
            newTrans.UserId = DekiContext.Current.User.ID;
            newTrans.PageId = sourcePage.ID;
            newTrans.Title = sourcePage.Title;
            newTrans.Type = RC.MOVE;
            newTrans.TimeStamp = DateTime.UtcNow;
            uint transId = DbUtils.CurrentSession.Transactions_Insert(newTrans);
            try {

                //Target titles can only be redirects or talk pages (which are deleted before the move)
                EnsureTargetTitlesAvailableForMove(newTitles);

                //Retrieve or create the new parent page of the root node
                PageBE newParentPage = EnsureParent(sourcePage.IsRedirect, title);

                //Ensure user has CREATE access on the proposed new parent page 
                PermissionsBL.CheckUserAllowed(DekiContext.Current.User, newParentPage, Permissions.CREATE);

                //Perform update on titles in pages and olds. parent id in pages is updated as well.
                DbUtils.CurrentSession.Pages_UpdateTitlesForMove(sourcePage.Title, newParentPage.Title.IsRoot ? 0 : newParentPage.ID, title, DekiContext.Current.Now);
                DbUtils.CurrentSession.Pages_UpdateTitlesForMove(sourcePage.Title.AsTalk(), 0, title.AsTalk(), DekiContext.Current.Now);

                // Perform displayname update to page after the location change
                if(!StringUtil.EqualsInvariant(title.DisplayName, sourcePage.Title.DisplayName)) {
                    PageBE updatedRootPage = GetPageById(sourcePage.ID);
                    if(updatedRootPage != null) {
                        SetPageDisplayTitle(updatedRootPage, title.DisplayName);
                    }
                }

                // Build return value from current state in the db
                List<PageBE> movedPages =
                    DbUtils.CurrentSession.Pages_GetByIds(from p in pagesToMove select p.ID).ToList();

                ret = new List<PageBE>(from p in movedPages select p.Copy());

                // All pages have now been moved. Process recent changes, redirects, and events on another thread
                Async.Fork(() => {

                    // Create redirects from previous titles to new titles
                    try {
                        MovePageProcessRedirects(pagesToMoveList, newTitlesByPageId, childRedirectsToRecreate);
                    } catch(Exception x) {
                        _log.WarnExceptionFormat(x, "Move page failure: redirect processing");
                    }

                    // Add to recent changes and trigger notification
                    try {
                        MovePageProcessRecentChanges(pagesToMoveList, movedPages, newTitlesByPageId, transId);
                    } catch(Exception x) {
                        _log.WarnExceptionFormat(x, "Move page failure: recentchange processing");
                    }
                }, new Result());

            } catch {
                DbUtils.CurrentSession.Transactions_Delete(transId);
                throw;
            }
            return ret;
        }

        private static void MovePageProcessRecentChanges(List<PageBE> pagesToMove, List<PageBE> movedPages, Dictionary<ulong, Title> newTitlesByPageId, uint transactionId) {

            Dictionary<ulong, PageBE> movedPagesHash = movedPages.ToDictionary(e => e.ID);

            bool minorchange = false;
            foreach(PageBE page in pagesToMove) {

                PageBE movedPage = null;
                if(movedPagesHash.TryGetValue(page.ID, out movedPage)) {

                    //Write to recent changes
                    Title sourceTitle = page.Title;
                    Title targetTitle = newTitlesByPageId[page.ID];
                    var rcComment = DekiResources.ONE_MOVED_TO_TWO(sourceTitle.AsPrefixedUserFriendlyPath(), targetTitle.AsPrefixedDbPath());
                    RecentChangeBL.AddMovePageRecentChange(DekiContext.Current.Now, page, targetTitle, DekiContext.Current.User, rcComment, minorchange, transactionId);
                    minorchange = true;
                    DekiContext.Current.Instance.EventSink.PageMove(DekiContext.Current.Now, page, movedPage, DekiContext.Current.User);
                }
            }
        }

        private static void MovePageProcessRedirects(List<PageBE> pagesToMove, Dictionary<ulong, Title> newTitlesByPageId, List<PageBE> childRedirectsToRecreate) {

            //Update redirects pointing to the old locations to point to the new
            Dictionary<ulong, IList<PageBE>> redirectsByPages =
                DbUtils.CurrentSession.Pages_GetRedirects(new List<ulong>(from p in pagesToMove select p.ID));

            foreach(PageBE pageToMove in pagesToMove) {
                IList<PageBE> redirects = null;
                if(redirectsByPages.TryGetValue(pageToMove.ID, out redirects)) {
                    foreach(PageBE redirect in redirects) {
                        redirect.SetText("#REDIRECT [[" + newTitlesByPageId[pageToMove.ID].AsPrefixedDbPath() + "]]");
                        DbUtils.CurrentSession.Pages_Update(redirect);
                    }
                }
            }

            // redirects that are children of the moved root page are deleted by Pages_UpdateTitlesForMove and need to be recreated
            pagesToMove.AddRange(childRedirectsToRecreate);

            //NOTE: Pages are sorted before creating redirects to intermediate parent pages arent automatically created
            pagesToMove = pagesToMove.OrderBy(p => p.Title).ToList();

            foreach(PageBE movedPage in pagesToMove) {
                string text = string.Empty;
                if(movedPage.IsRedirect) {
                    //This redirect is simply recreated any may be a direct link or in the case when it's pointing to a page that was just moved,
                    //it will point to the previous title for which there's a new redirect.
                    text = movedPage.GetText(DbUtils.CurrentSession);
                } else {
                    text = "#REDIRECT [[" + newTitlesByPageId[movedPage.ID].AsPrefixedDbPath() + "]]";
                }
                PageBE oldPageRedirect = new PageBE();
                oldPageRedirect.Title = movedPage.Title;
                oldPageRedirect.SetText(text);
                Save(oldPageRedirect, oldPageRedirect.GetText(DbUtils.CurrentSession), DekiMimeType.DEKI_TEXT, null);
                DbUtils.CurrentSession.Grants_CopyToPage(movedPage.ID, oldPageRedirect.ID);
                DekiContext.Current.Instance.EventSink.PageAliasCreate(DekiContext.Current.Now, oldPageRedirect, DekiContext.Current.User);
            }
        }

        private static void EnsureTargetTitlesAvailableForMove(Title[] newTitles) {
            IList<PageBE> existingPagesWithNewTitles = DbUtils.CurrentSession.Pages_GetByTitles(newTitles);

            List<Title> newTitlesList = new List<Title>(newTitles);
            List<PageBE> discardTitles = new List<PageBE>();
            List<Title> nonDiscardTitles = new List<Title>();
            foreach(PageBE existingPage in existingPagesWithNewTitles) {
                if(existingPage.IsRedirect || existingPage.Title.IsTalk) {
                    discardTitles.Add(existingPage);
                } else if(newTitlesList.Contains(existingPage.Title)) {
                    throw new PageMoveExistingTitleConflictException(existingPage.Title.AsPrefixedUserFriendlyPath());
                } else {
                    nonDiscardTitles.Add(existingPage.Title);
                }
            }

            if(nonDiscardTitles.Count > 0) {
                throw new PageAlreadyExistsConflictException();
            }

            //TODO: Move needs to create a transaction
            if(discardTitles.Count > 0) {
                DeletePages(discardTitles.ToArray(), DateTime.UtcNow, 0, false);
            }
        }

        private static Dictionary<ulong, Title> BuildNewTitlesForMovedPages(Title rootTitle, PageBE[] pagesToMove, Title newTitleForRootPage) {
            Dictionary<ulong, Title> newTitlesByPageId = new Dictionary<ulong, Title>();
            foreach(PageBE pageToMove in pagesToMove) {
                string titleRelativeToRootNode = pageToMove.Title.AsPrefixedDbPath().Substring((pageToMove.Title.IsTalk ? rootTitle.AsTalk() : rootTitle).AsPrefixedDbPath().Length);
                newTitlesByPageId[pageToMove.ID] = Title.FromPrefixedDbPath((pageToMove.Title.IsTalk ? newTitleForRootPage.AsTalk() : newTitleForRootPage).AsPrefixedDbPath() + titleRelativeToRootNode, pageToMove.Title.DisplayName);
            }

            return newTitlesByPageId;
        }

        private static void ValidatePageMoveForRootNode(PageBE sourcePage, Title title) {
            if(title.IsHomepage) {
                throw new PageMoveTitleConflictException();
            }
            if((sourcePage.Title.IsTemplate || title.IsTemplate) && (sourcePage.Title.Namespace != title.Namespace)) {
                throw new PageMoveTemplateConflictException();
            }
            if((sourcePage.Title.IsSpecial || title.IsSpecial) && (sourcePage.Title.Namespace != title.Namespace)) {
                throw new PageMoveSpecialConflictException();
            }
            if(!title.IsEditable || title.IsTalk) {
                throw new PageMoveTitleEditeConflictException(title.Namespace);
            }
            if(!title.IsValid) {
                throw new PageInvalidTitleConflictException();
            }
            if(sourcePage.ID == DekiContext.Current.Instance.HomePageId) {
                throw new PageMoveHomepageMoveConflictException();
            }
            if((sourcePage.Title.IsUser && sourcePage.ParentID == 0) && !PermissionsBL.IsUserAllowed(DekiContext.Current.User, Permissions.ADMIN)) {
                throw new PageMoveRootUserConflictException();
            }
            if(!sourcePage.Title.IsEditable) {
                throw new PageMoveSourceNamespaceConflictException(title.Namespace);
            }
            if(sourcePage.Title.IsParentOf(title)) {
                throw new PageMoveDescendantConflictException(sourcePage.Title.Path, title.Path);
            }
            if(sourcePage.Title.IsTalk) {
                throw new PageModifyTalkConflictException();
            }
        }

        private static PagePathType GetPathType(PageBE page) {
            string name = page.Title.AsSegmentName();
            string title = page.Title.DisplayName ?? string.Empty;

            // NOTE (steveb): Title.Rename() has behavior based on FIXED paths; keep them consistent

            // Determine 'fixed' status first
            if(page.Title.IsRoot) { // top location of a namespace
                return PagePathType.FIXED;
            }
            if(page.Title.IsUser && page.Title.GetParent().IsHomepage) { // user homepage
                return PagePathType.FIXED;
            }
            if(page.Title.IsTalk) { // talk pages
                return PagePathType.FIXED;
            }

            // Determine 'linked' status
            if(name.EqualsInvariantIgnoreCase(title)) {
                return PagePathType.LINKED;
            }
            if(name.EqualsInvariantIgnoreCase(Title.DbEncodeDisplayName(title))) {
                return PagePathType.LINKED;
            }
            if(title.EqualsInvariantIgnoreCase(page.Title.AsUserFriendlyDisplayName())) {
                return PagePathType.LINKED;
            }
            return PagePathType.CUSTOM;
        }

        #endregion

        #region Authorization and page helper methods

        public static void IsAccessAllowed(PageBE page, Permissions access, bool ignoreHomepageException) {
            if(page == null)
                return;

            // TODO (steveb): implementation should be streamlined for readability

            // prevent creating, modifying, and deleting special pages
            if(!page.Title.IsEditable) {
                if((Permissions.CREATE == (Permissions.CREATE & access)) ||
                    (Permissions.UPDATE == (Permissions.UPDATE & access)) ||
                    (Permissions.DELETE == (Permissions.DELETE & access))) {
                    throw new PageModifySpecialConflictException();
                }
            }

            // prevent deleting the home page
            if(!ignoreHomepageException && (Permissions.DELETE == (Permissions.DELETE & access)) && (DekiContext.Current.Instance.HomePageId == page.ID)) {
                throw new PageDeleteHomepageConflictException();
            }

            // prevent talk pages from being modified independantly from their associated page
            if(page.Title.IsTalk) {
                if(Permissions.DELETE == (Permissions.DELETE & access)) {
                    throw new PageModifyTalkConflictException();
                }
                if(Permissions.CREATE == (Permissions.CREATE & access)) {
                    PageBE talkAssociate = GetPageByTitle(page.Title.AsFront());
                    if(null == talkAssociate || 0 == talkAssociate.ID) {
                        throw new PageCreateTalkConflictException();
                    }
                }
            }
        }

        public static PageBE AuthorizePage(UserBE user, Permissions access, PageBE page, bool ignoreHomepageException) {

            //Check whether access on this page is overriden
            IsAccessAllowed(page, access, ignoreHomepageException);

            //Perform authorization lookup
            PermissionsBL.CheckUserAllowed(user, page, access);
            return page;
        }

        public static PageBE ResolvePageRev(PageBE page, string revStr) {
            revStr = revStr.Trim();

            // determine the requested revision
            if(!revStr.EqualsInvariantIgnoreCase("head") && !revStr.EqualsInvariantIgnoreCase("0")) {
                int revision;
                if(int.TryParse(revStr, out revision)) {
                    OldBE oldRev = GetOldRevisionForPage(page, revision);
                    if(oldRev == null) {
                        throw new PageWithRevisionNotFoundException();
                    } else {
                        CopyOldToPage(oldRev, page, page.Title);
                    }

                } else {
                    throw new RevisionHeadOrIntInvalidArgumentException();
                }
            }
            return page;
        }

        public static OldBE GetOldRevisionForPage(PageBE page, int revision) {
            OldBE ret = null;
            ulong revisionToLookup;

            if(revision < 0) {
                if((int)page.Revision + revision <= 0) {
                    return null;
                }
                revisionToLookup = (page.Revision + (ulong)revision);
            } else if(revision > 0) {
                revisionToLookup = (ulong)revision;
            } else {
                revisionToLookup = page.Revision;
            }

            if(revisionToLookup == page.Revision) {
                ret = new OldBE();
                CopyPageToOld(page, ret);
            } else {
                ret = DbUtils.CurrentSession.Old_GetOldByRevision(page.ID, revisionToLookup);
            }
            return ret;
        }

        public static void CopyOldToPage(OldBE old, PageBE page, Title pageTitle) {
            page.SetText(old.Text);
            page.ContentType = old.ContentType;
            page.UserID = old.UserID;
            page.TimeStamp = old.TimeStamp;
            page.MinorEdit = old.MinorEdit;
            page.Comment = old.Comment;
            page.Language = old.Language;
            page.IsHidden = old.IsHidden;
            page.Meta = old.Meta;
            page.Revision = old.Revision;
            page.ID = old.PageID;
            page.Title = pageTitle;
        }

        public static void CopyPageToOld(PageBE page, OldBE old) {
            old.Text = page.GetText(DbUtils.CurrentSession);
            old.ContentType = page.ContentType;
            old.UserID = page.UserID;
            old.TimeStamp = page.TimeStamp;
            old.MinorEdit = page.MinorEdit;
            old.Comment = page.Comment;
            old.Language = page.Language;
            old.IsHidden = page.IsHidden;
            old.Meta = page.Meta;
            old.Revision = page.Revision;
            old.PageID = page.ID;
            old.DisplayName = page.Title.DisplayName;
        }

        public static PageBE ResolveRedirects(PageBE startPage) {
            return ResolveRedirects(startPage, int.MaxValue);
        }

        public static PageBE ResolveRedirects(PageBE startPage, int redirects) {
            PageBE result = startPage;
            if(redirects > 0) {
                PageBE currentRedirect = startPage;
                Dictionary<string, bool> redirectedFrom = new Dictionary<string, bool>();

                // process the next entry in the redirect chain
                while(redirects > 0 && currentRedirect != null && !redirectedFrom.ContainsKey(currentRedirect.Title.AsPrefixedDbPath()) && 0 != currentRedirect.ID) {
                    --redirects;
                    redirectedFrom[currentRedirect.Title.AsPrefixedDbPath()] = true;
                    result = currentRedirect;

                    if(currentRedirect.IsRedirect) {
                        currentRedirect = PageBL.GetTargetForRedirectPage(currentRedirect);
                    }
                }

                // if a redirect was found, update the redirected from page information
                if(result.ID != startPage.ID) {
                    result.RedirectedFrom = startPage;
                }
            }
            return result;
        }
        #endregion

        #region Page deletion methods
        public static PageBE[] DeletePage(PageBE startNode, bool deleteChildren) {

            // BUGBUGBUG (steveb): if deleteChildren is false; don't fetch the children (duh!)

            Dictionary<ulong, PageBE> pagesById = null;
            startNode = PopulateDescendants(startNode, null, out pagesById, int.MaxValue);

            //Apply permissions to all descendant pages and their corresponding talk pages
            PageBE[] allowedPages = PermissionsBL.FilterDisallowed(DekiContext.Current.User, pagesById.Values, false, new[] { Permissions.DELETE });
            IList<PageBE> allTalkPages = GetTalkPages(allowedPages);
            PageBE[] allowedTalkPages = PermissionsBL.FilterDisallowed(DekiContext.Current.User, allTalkPages, false, new[] { Permissions.DELETE });

            //Determine reset/delete list
            Dictionary<ulong, PageBE> allowedPagesById = allowedPages.AsHash(e => e.ID);
            Dictionary<ulong, PageBE> pagesToResetHash = new Dictionary<ulong, PageBE>();
            Dictionary<Title, PageBE> allTalkPagesByTitle = allTalkPages.AsHash(e => e.Title);
            Dictionary<Title, PageBE> allowedTalkPagesByTitle = allowedTalkPages.AsHash(e => e.Title);
            RemoveParentsOfDisallowedChildrenAndTalk(startNode, allTalkPagesByTitle, allowedTalkPagesByTitle, allowedPagesById, pagesToResetHash);
            PageBE[] pagesToDelete = null;
            PageBE[] pagesToReset = null;
            PageBE[] talkPagesToDelete = null;

            List<PageBE> talkPagesToDeleteList = new List<PageBE>();

            //If children exist and are deleteable but not asked to delete children, reset the startnode instead
            if(!deleteChildren && startNode.ChildPages != null && startNode.ChildPages.Length > 0) {
                pagesToResetHash[startNode.ID] = startNode;
                allowedPagesById.Remove(startNode.ID);
            }

            if(deleteChildren) {
                pagesToDelete = allowedPagesById.Values.ToArray();
                pagesToReset = pagesToResetHash.Values.ToArray();
                talkPagesToDelete = allowedTalkPagesByTitle.Values.ToArray();
            } else {
                if(allowedPagesById.ContainsKey(startNode.ID)) {
                    pagesToDelete = new[] { startNode };
                }
                if(pagesToResetHash.ContainsKey(startNode.ID)) {
                    pagesToReset = new[] { startNode };
                }
                if(allowedTalkPagesByTitle.ContainsKey(startNode.Title.AsTalk())) {
                    talkPagesToDelete = new[] { startNode };
                }
            }

            //Perform deletes and resets
            List<PageBE> deletedPages = new List<PageBE>();

            TransactionBE newTrans = new TransactionBE();
            newTrans.UserId = DekiContext.Current.User.ID;
            newTrans.PageId = startNode.ID;
            newTrans.Title = startNode.Title;
            newTrans.Type = RC.PAGEDELETED;
            newTrans.TimeStamp = DateTime.UtcNow;
            uint transId = DbUtils.CurrentSession.Transactions_Insert(newTrans);
            try {
                pagesToDelete = SortPagesByTitle(pagesToDelete);
                pagesToReset = SortPagesByTitle(pagesToReset);

                //Perform deletes and resets
                if(pagesToDelete != null && pagesToDelete.Length > 0) {
                    DeletePages(pagesToDelete, newTrans.TimeStamp, transId, true);
                    deletedPages.AddRange(pagesToDelete);
                }
                if(pagesToReset != null && pagesToReset.Length > 0) {
                    DeleteByResettingPages(pagesToReset, newTrans.TimeStamp, transId);
                    deletedPages.AddRange(pagesToReset);
                }

                //Talk pages get deleted in same transaction
                if(talkPagesToDelete != null && talkPagesToDelete.Length > 0) {
                    DeletePages(allowedTalkPages, newTrans.TimeStamp, transId, true);
                    deletedPages.AddRange(allowedTalkPages);
                }

            } catch {
                DbUtils.CurrentSession.Transactions_Delete(transId);
                throw;
            }

            // Deleted user homepages are recreated
            foreach(PageBE p in deletedPages) {
                if(p.Title.IsUser && p.Title.GetParent().IsHomepage) {

                    // NOTE (maxm): This is determining the owner of the page by the title which
                    // may not always be reliable.
                    UserBE pageOwner = UserBL.GetUserByName(p.Title.AsSegmentName());
                    if(pageOwner != null) {
                        CreateUserHomePage(pageOwner);
                    }
                }
            }
            return deletedPages.ToArray();
        }

        public static void DeletePages(PageBE[] pagesToDelete, DateTime timeStampUtc, uint transactionId, bool showInRecentChanges) {
            AttachmentBL.Instance.RemoveAttachmentsFromPages(pagesToDelete, timeStampUtc, transactionId);
            IList<ulong> pageIds = pagesToDelete.Select(e => e.ID).ToList();
            DbUtils.CurrentSession.Links_MoveInboundToBrokenLinks(pageIds);
            DbUtils.CurrentSession.Archive_MovePagesTo(pageIds, transactionId);
            bool minorChange = false;
            foreach(PageBE p in pagesToDelete) {
                if(showInRecentChanges) {
                    RecentChangeBL.AddDeletePageRecentChange(timeStampUtc, p, DekiContext.Current.User, DekiResources.DELETED_ARTICLE(p.Title.AsPrefixedUserFriendlyPath()), minorChange, transactionId);
                    minorChange = true;
                }
                DekiContext.Current.Instance.EventSink.PageDelete(DekiContext.Current.Now, p, DekiContext.Current.User);
            }
        }

        private static void DeleteByResettingPages(PageBE[] pagesToReset, DateTime timeStampUtc, uint transactionId) {
            var resources = DekiContext.Current.Resources;

            // TODO (Max): There's a little room for optimization here. Pages that are reset can be deleted with the initial batch and then
            // this logic can be applied to the pages that need recreating
            foreach(PageBE p in pagesToReset) {

                ICollection<PageBE> childrenOfOriginal = PageBL.GetChildren(p, true);
                DeletePages(new PageBE[] { p }, timeStampUtc, transactionId, false);

                //Create a new page with same title as 'reset' page
                PageBE newPage = new PageBE();
                newPage.Title = p.Title;
                Save(newPage, resources.Localize(DekiResources.DEL_WITH_CHILDREN_PLACEHOLDER()), DekiMimeType.DEKI_TEXT, null);

                //Change parent ids of children of reset page to point to new page.
                foreach(PageBE childOfOriginal in childrenOfOriginal) {
                    childOfOriginal.ParentID = newPage.ID;
                    DbUtils.CurrentSession.Pages_Update(childOfOriginal);
                }
            }
        }
        #endregion

        #region Page utility methods

        public static bool AddParentsOfAllowedChildren(PageBE page, Dictionary<ulong, PageBE> allowedPagesById, Dictionary<ulong, PageBE> addedPagesById) {
            if(addedPagesById == null)
                addedPagesById = new Dictionary<ulong, PageBE>();

            bool isAllowed = allowedPagesById.ContainsKey(page.ID);

            if(page.ChildPages == null || page.ChildPages.Length == 0)
                return isAllowed;

            bool hasAllowedChild = false;
            foreach(PageBE p in page.ChildPages) {
                if(AddParentsOfAllowedChildren(p, allowedPagesById, addedPagesById)) {
                    hasAllowedChild = true;
                }
            }

            if(!isAllowed && hasAllowedChild) {
                allowedPagesById[page.ID] = addedPagesById[page.ID] = page;
                return true;
            }
            return isAllowed;
        }

        public static bool RemoveParentsOfDisallowedChildrenAndTalk(PageBE page, Dictionary<Title, PageBE> allTalkByTitle, Dictionary<Title, PageBE> allowedTalkByTitle, Dictionary<ulong, PageBE> allowedPagesById, Dictionary<ulong, PageBE> removedPagesById) {

            // NOTE: method returns 'false' if no constraints exist for the current page to be removed (i.e. it can be deleted)

            if(removedPagesById == null) {
                throw new ArgumentNullException("removedPagesById");
            }

            bool isDisallowed = !allowedPagesById.ContainsKey(page.ID);
            bool isDisallowedTalk = allTalkByTitle.ContainsKey(page.Title.AsTalk()) && !allowedTalkByTitle.ContainsKey(page.Title.AsTalk());

            bool hasDisallowedChild = false;
            if(page.ChildPages != null) {
                foreach(PageBE p in page.ChildPages) {
                    if(RemoveParentsOfDisallowedChildrenAndTalk(p, allTalkByTitle, allowedTalkByTitle, allowedPagesById, removedPagesById)) {
                        hasDisallowedChild = true;
                    }
                }
            }
            if(!isDisallowed && (hasDisallowedChild || isDisallowedTalk)) {

                // mark current page to be reset
                allowedPagesById.Remove(page.ID);
                removedPagesById[page.ID] = page;
                return true;
            } else if(isDisallowed) {

                // mark current page to be skipped
                allowedPagesById.Remove(page.ID);
                return true;
            }

            // page and child-pages can be deleted
            return false;
        }

        public static PageBE[] SortPagesByTitle(PageBE[] pages) {
            if(ArrayUtil.IsNullOrEmpty(pages)) {
                return pages;
            }
            var sortedPages = new PageBE[pages.Length];
            Array.Copy(pages, sortedPages, pages.Length);
            Array.Sort(sortedPages, (p1, p2) => p1.Title.CompareTo(p2.Title));
            return sortedPages;
        }

        #endregion

        #region Uris
        public static XUri GetUriContentsCanonical(PageBE page) {
            return GetUriCanonical(page).At("contents").With("revision", page.Revision.ToString());
        }
        public static XUri GetUriRevisionCanonical(PageBE page) {
            return GetUriCanonical(page).At("revisions").With("revision", page.Revision.ToString());
        }
        public static XUri GetUriCanonical(PageBE page) {
            return GetUri(page).With("redirects", "0");
        }
        public static XUri GetUri(PageBE page) {
            return DekiContext.Current.ApiUri.At("pages", page.ID.ToString());
        }
        public static XUri GetUriProperties(PageBE page) {
            return GetUri(page).At("properties");
        }
        public static XUri GetUriUi(PageBE page) {
            return XUri.TryParse(Utils.AsPublicUiUri(page.Title));
        }
        #endregion

        #region XML Helpers
        public static XDoc GetPropertiesXml(PageBE page) {
            return new XDoc("properties").Attr("href", DekiContext.Current.ApiUri.At("pages", page.ID.ToString(), "properties")).Elem("language", page.Language);
        }

        public static XDoc GetMetricsXml(PageBE page, bool includeCharCount) {
            XDoc ret = new XDoc("metrics")
                 .Start("metric.views").Value(GetViewCount(page.ID)).End();
            if(includeCharCount) {
                ret.Start("metric.charcount").Value(page.TextLength).End();
            }
            return ret;
        }

        public static XDoc GetSecurityXml(PageBE page) {
            XDoc ret = new XDoc("security");
            ret.Attr("href", DekiContext.Current.ApiUri.At("pages", page.ID.ToString(), "security"));

            ret.Add(PermissionsBL.GetPermissionXml(PermissionsBL.CalculateEffectivePageRights(page, DekiContext.Current.User), "effective"));
            RoleBE targetPageRestriction = PermissionsBL.GetRestrictionById(page.RestrictionID);
            ret.Add(PermissionsBL.GetRoleXml(targetPageRestriction, "page"));
            IList<GrantBE> grants = DbUtils.CurrentSession.Grants_GetByPage((uint)page.ID);
            ret.Add(PermissionsBL.GetGrantListXml(grants));
            ret.Add(PermissionsBL.GetPermissionsRevokedXml(DekiContext.Current.User));
            return ret;
        }

        public static XDoc GetLinksXml(IList<KeyValuePair<ulong, Title>> links, string listName) {

            // Populate the minimum information needed to retrieve the page list xml of the links
            var linkPages = from link in links select new PageBE() { ID = link.Key, Title = link.Value };
            return PageBL.GetPageListXml(linkPages.ToList(), listName);
        }

        public static XDoc GetPageXml(PageBE page, string relation) {
            return GetPageXml(page, relation, false);
        }

        public static XDoc GetPageXml(PageBE page, string relation, bool suppressRedirects) {
            XDoc pageXml = new XDoc(string.IsNullOrEmpty(relation) ? "page" : "page." + relation);

            pageXml.Attr("id", page.ID)
                   .Attr("revision", page.Revision);
            XUri uri = GetUriCanonical(page);
            if(suppressRedirects && page.IsRedirect) {
                uri = uri.With("redirects", "0");
            }
            if(page.IsHidden) {
                pageXml.Attr("hidden", true);
            }

            pageXml.Attr("href", uri);
            pageXml.Elem("uri.ui", DekiContext.Current.UiUri.AtPath(page.Title.AsUiUriPath()));

            // check for the special case where the title is null - this indicates the home page where the uri host is used as the title
            // set page title

            //Don't show displayname if rev is hidden && non admin

            if(!page.IsHidden || PermissionsBL.IsUserAllowed(DekiContext.Current.User, Permissions.ADMIN)) {
                pageXml.Start("title").Value(page.Title.AsUserFriendlyName()).End();
            } else {
                Title tempTitle = Title.FromPrefixedDbPath(page.Title.AsPrefixedDbPath(), null);
                pageXml.Start("title").Value(tempTitle.AsUserFriendlyName()).Attr("hidden", true).End();
            }

            pageXml.Start("path").Value(page.Title.AsPrefixedDbPath());
            switch(GetPathType(page)) {
            case PagePathType.CUSTOM:
                pageXml.Attr("type", "custom");
                break;
            case PagePathType.FIXED:
                pageXml.Attr("type", "fixed");
                break;
            case PagePathType.LINKED:
                break; // type is omitted
            }
            pageXml.End(); // path/@type

            pageXml.Elem("namespace", ((NS)page._Namespace).ToString().ToLowerInvariant());
            return pageXml;
        }

        public static XDoc GetPageXmlVerbose(PageBE page, string relation) {
            return GetPageXmlVerbose(page, relation, new PageContentFilterSettings());
        }

        public static XDoc GetPageXmlVerbose(PageBE page, string relation, PageContentFilterSettings contentFilterSettings) {
            XDoc pageXml = GetPageXml(page, relation, true);
            pageXml.Add(GetMetricsXml(page, true));
            pageXml.Start("summary").Value(page.TIP).End();

            //Pages that are not editable dont allow permission changes. If permission changes aren't allowed, don't display the security info.
            if(page.Title.IsEditable) {
                pageXml.Add(GetSecurityXml(page));
            }

            // HACKHACKHACK (MaxM): Page Create timestamp and creator need to be tracked in the PageBE entity rather than queried
            DateTime? created = null;
            uint? creatorUserId = null;
            if(page.Revision == 1) {
                created = page.TimeStamp;
                creatorUserId = page.UserID;
            } else {
                var tailRev = GetOldRevisionForPage(page, 1);
                if(tailRev != null) {
                    created = tailRev.TimeStamp;
                    creatorUserId = tailRev.UserID;
                }
            }
            UserBE creator = null;
            if(creatorUserId != null) {
                creator = UserBL.GetUserById(creatorUserId.Value);
            }
            if(creator != null) {
                pageXml.Add(UserBL.GetUserXml(creator, "createdby", Utils.ShowPrivateUserInfo(creator)));
            }
            if(created != null) {
                pageXml.Start("date.created").Value(created.Value).End();
            }
            pageXml.Start("date.edited").Value(page.TimeStamp).End();
            pageXml.Start("date.modified").Value(page.Touched).End();
            UserBE author;
            if(page.Revision == 1) {
                author = creator;
            } else {
                author = UserBL.GetUserById(page.UserID);
            }
            if(author != null) {
                pageXml.Add(UserBL.GetUserXml(author, "author", Utils.ShowPrivateUserInfo(author)));
            }
            pageXml.Start("description").Value(page.Comment).End();
            pageXml.Elem("language", page.Language);
            pageXml.Add(BuildParentPageXmlTree(page, false));

            // page redirection information
            pageXml.Start("page.redirectedfrom");
            if(null != page.RedirectedFrom) {
                pageXml.Add(GetPageXml(page.RedirectedFrom, null, true));
            }
            pageXml.End();

            PageBE redirectedTo = GetTargetForRedirectPage(page);
            if(page.IsRedirect && redirectedTo != null) {
                pageXml.Add(GetPageXml(redirectedTo, "redirectedto", true));
            }

            // Embed page rating
            RatingBL.AppendRatingXml(pageXml, page, DekiContext.Current.User);
            pageXml.Start("subpages").Attr("href", DekiContext.Current.ApiUri.At("pages", page.ID.ToString(), "subpages")).End();

            // TODO (steveb): why is this commented out?
            //pageXml.Add(GetPageListXml("subpages", PageBL.GetChildren(this, true), DekiContext.Current.ApiUri.At("pages", ID.ToString(), "subpages")));

            if(!contentFilterSettings.ExcludeOutboundLinks) {
                pageXml.Add(GetLinksXml(DbUtils.CurrentSession.Links_GetOutboundLinks(page.ID), "outbound"));
            }
            if(!contentFilterSettings.ExcludeInboundLinks) {
                pageXml.Add(GetLinksXml(DbUtils.CurrentSession.Links_GetInboundLinks(page.ID), "inbound"));
            }
            pageXml.Start("aliases")
                //TODO (Max): .Attr("count", ????)
                .Attr("href", DekiContext.Current.ApiUri.At("pages", page.ID.ToString(), "aliases"))
            .End();
            pageXml.Start("revisions")
                .Attr("count", page.Revision)
                .Attr("href", DekiContext.Current.ApiUri.At("pages", page.ID.ToString(), "revisions"))
           .End();

            // Emit archive revision info
            if(PermissionsBL.IsUserAllowed(DekiContext.Current.User, Permissions.ADMIN)) {
                pageXml.Start("revisions.archive")
                    .Attr("count", DbUtils.CurrentSession.Archive_GetCountByTitle(page.Title))
                    .Attr("href", DekiContext.Current.ApiUri.At("archive", "pages", page.Title.AsApiParam(), "revisions"))
                .End();
            }

            // Emit comment info
            pageXml.Start("comments")
                .Attr("count", DbUtils.CurrentSession.Comments_GetCountByPageId(page.ID).ToString())
                .Attr("href", DekiContext.Current.ApiUri.At("pages", page.ID.ToString(), "comments"))
            .End();

            //Embed properties for the attachment
            IList<ResourceBE> props = PropertyBL.Instance.GetPageProperties(page.ID);
            pageXml = PropertyBL.Instance.GetPropertyXml(props, GetUri(page), null, null, pageXml);

            //Backwards compatibility: return the language within properties
            pageXml["/page/properties"].Start("language").Attr("deprecated", true).Value(page.Language).End();

            // Emit tags list
            XUri tagHref = DekiContext.Current.ApiUri.At("pages", page.ID.ToString(), "tags");
            var tagBL = new TagBL();
            pageXml.Add(tagBL.GetTagListXml(tagBL.GetTagsForPage(page), "tags", tagHref, false));

            //Page file attachments
            pageXml.Add(AttachmentBL.Instance.GetFileXml(AttachmentBL.Instance.GetPageAttachments(page.ID), false, null, null, null));
            if(contentFilterSettings.IncludeContents) {
                ParserResult parserResult;
                ResolvePageRev(page, contentFilterSettings.Revision);
                pageXml.Add(RetrievePageXDoc(page, (uint)page.ID, contentFilterSettings.ContentsMode, page.Language, false, -1, page.Title, contentFilterSettings.Xhtml, out parserResult)); // CESAR: finish
            } else {
                pageXml.Start("contents")
                    .Attr("type", page.ContentType)
                    .Attr("href", DekiContext.Current.ApiUri.At("pages", page.ID.ToString(), "contents"))
                    .Attr("etag", page.Etag)
                    .End();
            if(DekiContext.Current.Deki.PrinceXmlPath != string.Empty)
                pageXml.Start("contents.alt").Attr("type", MimeType.PDF.FullType).Attr("href", DekiContext.Current.ApiUri.At("pages", page.ID.ToString(), "pdf")).End();
            }
            return pageXml;
        }

        public static XDoc GetPageListXml(IEnumerable<PageBE> listItems, string listName) {
            return GetPageListXml(listItems, listName, null);
        }

        public static XDoc GetPageListXml(IEnumerable<PageBE> listItems, string listName, XUri href) {
            XDoc ret = null;
            if(!string.IsNullOrEmpty(listName))
                ret = new XDoc(listName);
            else
                ret = new XDoc("list");

            // TODO (Max): Consider passing in a list or a ICollection
            int count = 0;
            if(listItems != null)
                count = new List<PageBE>(listItems).Count;

            ret.Attr("count", count);

            if(href != null) {
                ret.Attr("href", href);
            }

            if(listItems != null) {
                foreach(PageBE page in listItems)
                    ret.Add(PageBL.GetPageXml(page, string.Empty));
            }
            return ret;
        }

        internal static XDoc GetOldXml(PageBE pageCurrent, OldBE old, string relation) {

            // retrieve the page xml summary
            // NOTE (maxm): The old to page copying is done for revision hiding
            PageBE pageFromOld = new PageBE();
            CopyOldToPage(old, pageFromOld, pageCurrent.Title);
            XDoc oldXml = PageBL.GetPageXml(pageFromOld, relation);

            // add revision-specific information to it
            oldXml.Attr("revision", old.Revision.ToString());
            oldXml.Attr("href", DekiContext.Current.ApiUri.At("pages", pageCurrent.ID.ToString(), "revisions").With("revision", old.Revision.ToString()).With("redirects", "0"));
            oldXml.Start("date.edited").Value(old.TimeStamp).End();
            UserBE author = UserBL.GetUserById(old.UserID);
            if(author != null)
                oldXml.Add(UserBL.GetUserXml(author, "author", Utils.ShowPrivateUserInfo(author)));


            if(!old.IsHidden || PermissionsBL.IsUserAllowed(DekiContext.Current.User, Permissions.ADMIN)) {
                oldXml.Start("description").Value(old.Comment).End();
            } else {

                //if rev is hidden && non admin, dont show comment and add @hidden=true
                oldXml.Start("description").Attr("hidden", true).End();
            }

            oldXml.Start("contents").Attr("type", old.ContentType).Attr("href", DekiContext.Current.ApiUri.At("pages", pageCurrent.ID.ToString(), "contents").With("revision", old.Revision.ToString()).With("redirects", "0")).End();

            if(old.IsHidden) {
                uint? userIdHiddenBy = old.MetaXml.RevisionHiddenUserId;
                if(userIdHiddenBy != null) {
                    UserBE userHiddenBy = UserBL.GetUserById(userIdHiddenBy.Value);
                    if(userHiddenBy != null) {
                        oldXml.Add(UserBL.GetUserXml(userHiddenBy, "hiddenby", Utils.ShowPrivateUserInfo(userHiddenBy)));
                    }
                }
                oldXml.Elem("date.hidden", old.MetaXml.RevisionHiddenTimestamp ?? DateTime.MinValue);
                oldXml.Elem("description.hidden", old.MetaXml.RevisionHiddenComment ?? string.Empty);
            }
            return oldXml;
        }

        internal static XDoc GetOldListXml(PageBE pageCurrent, IEnumerable<OldBE> listItems, string listName) {

            // create a list of revision information 
            XDoc ret = null;
            if(!string.IsNullOrEmpty(listName))
                ret = new XDoc(listName);
            else
                ret = new XDoc("list");
            if(listItems != null) {
                foreach(OldBE old in listItems)
                    ret.Add(PageBL.GetOldXml(pageCurrent, old, string.Empty));
            }
            return ret;
        }
        #endregion
    }
}

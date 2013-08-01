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
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Xsl;

using MindTouch.Deki.Data;
using MindTouch.Deki.Logic;
using MindTouch.Deki.Script.Expr;
using MindTouch.Deki.Script.Runtime;
using MindTouch.Deki.Script.Runtime.Library;
using MindTouch.Dream;
using MindTouch.Tasking;
using MindTouch.Xml;

namespace MindTouch.Deki {
    public partial class DekiWikiService {

        //--- Constants ---
        public const string CUSTOM_PROPERTY_NAMESPACE = "urn:custom.mindtouch.com#";
        public const string PUBLIC_PROPERTY_NAMESPACE = "mindtouch.deki.public#";
        public const string API_PROPERTY_NAMESPACE = "mindtouch.deki.api#";
        public const string USER_PROPERTY_NAMESPACE = "mindtouch.deki.user.{0}#";

        //--- Functions ---

        #region --- Wiki Functions ---
        [DekiExtFunction("wiki.usercount", "Get number of wiki users. (OBSOLETE: use site.usercount)")]
        internal int WikiUserCount() {
            return (int)DbUtils.CurrentSession.Users_GetCount();
        }

        [DekiExtFunction("wiki.pagecount", "Get number of wiki pages. (OBSOLETE: use site.pagecount)")]
        internal int WikiPageCount() {
            return (int)DbUtils.CurrentSession.Pages_GetCount();
        }

        [DekiExtFunction("wiki.page", "Include an entire or part of another wiki page.")]
        public XDoc WikiPage(
            [DekiExtParam("wiki page path")] string path,
            [DekiExtParam("section to include on wiki page (default: full page)", true)] string section,
            [DekiExtParam("page revision to use: 0 is the head revision, > 0 stands for a specific revision (e.g. 1 is the first revision), < 0 is revision relative to head revision (e.g. -1 is the previous revision) (default: head revision)", true)] int? revision,
            [DekiExtParam("show page/section title of included page (default: no)", true)] bool? show,
            [DekiExtParam("set page/section title heading and adjust all sub-headings accordingly; the adjustment is applied even when the page/section heading is not shown (range: 0-5, where 0 is 'Title' style, 1 is 'Heading 1', and so forth; default: keep original headings)", true)] int? heading,
            [DekiExtParam("return nil if page/section is not found (default: return link to missing page/section)", true)] bool? nilIfMissing
        ) {
            path = ResolvePath(path);
            XDoc body = DekiXmlParser.Include(path, section, show ?? false, ++heading, null, revision ?? 0, true, nilIfMissing.GetValueOrDefault(), true);
            return body.IsEmpty ? null : body;
        }

        [DekiExtFunction("wiki.text", "Retrieve the text contents for an entire or part of another wiki page.")]
        public string WikiText(
            [DekiExtParam("wiki page path")] string path,
            [DekiExtParam("section to include on wiki page (default: nil)", true)] string section,
            [DekiExtParam("page revision to use: 0 is the head revision, > 0 stands for a specific revision (e.g. 1 is the first revision), < 0 is revision relative to head revision (e.g. -1 is the previous revision) (default: head revision)", true)] int? revision,
            [DekiExtParam("return nil if page/section is not found (default: return name of missing page/section)", true)] bool? nilIfMissing
        ) {
            return GetWikiText(path, section, revision, true, nilIfMissing.GetValueOrDefault());
        }

        private string GetWikiText(string path, string section, int? revision, bool followRedirects, bool nilIfMissing) {
            path = ResolvePath(path);
            XDoc body = DekiXmlParser.Include(path, section, false, null, null, revision ?? 0, followRedirects, nilIfMissing, false);
            if(body.IsEmpty) {
                return null;
            }
            body = body["body"];
            if(body.IsEmpty) {
                return string.Empty;
            }

            // TODO (steveb): we must use the HTML->TEXT converter here (see http://youtrack.developer.mindtouch.com/issue/MT-9630)

            // remove non-rendered HTML elements
            body["//script | //style | //object | //noscript | //applet"].RemoveAll();

            // retrieve only text
            return body.AsXmlNode.InnerText;
        }

        [DekiExtFunction("wiki.anypage", "Include or link to a random page from a list of pages.")]
        public XDoc WikiAnyPage(
            [DekiExtParam("page query")] string query,
            [DekiExtParam("interval in seconds for selecing another page (default: 3600)", true)] int? seconds,
            [DekiExtParam("max query results (default: 100)", true)] int? max,
            [DekiExtParam("link to the page (default: false)", true)] bool? linkonly,
            [DekiExtParam("style for page title (default: \"display: none;\")", true)] string titlestyle
        ) {

            // perform content search
            XDoc search = Coroutine.Invoke(DekiContext.Current.Deki.Search, query, (uint)(max ?? 100), (uint)0, string.Empty, "type:wiki", new Result<XDoc>()).Wait();
            search = search["document"];
            int count = search.ListLength;
            if(count == 0) {
                return new XDoc("html").Elem("body");
            }

            // determine random index to display
            int index = (int)DateTime.UtcNow.Subtract(new DateTime(2000, 1, 1)).TotalSeconds / (seconds ?? 3600);
            XDoc page = search.AtPosition(index % count);

            // load and render page
            Title title = Title.FromPrefixedDbPath(page["path"].AsText ?? string.Empty, page["title"].AsText);
            if(linkonly ?? false) {
                return new XDoc("html").Start("body").Start("a").Attr("href", Utils.AsPublicUiUri(title)).Attr("rel", "internal").Value(title.AsUserFriendlyName()).End().End();
            }
            XDoc result = DekiXmlParser.Include(page["path"].AsText, null, false, null, null, 0, true, false, false);
            result["body"].AddNodesInFront(new XDoc("title").Start("a").Attr("style", titlestyle ?? "display:none;").Attr("href", Utils.AsPublicUiUri(title)).Attr("rel", "internal").Value(title.AsUserFriendlyName()).Elem("br").End());
            return result;
        }

        [DekiExtFunction("wiki.toc", "Show the table of contents of a wiki page.")]
        public XDoc WikiToc(
            [DekiExtParam("wiki page path", true)] string path,
            [DekiExtParam("heading depth for table of contents (default: all)", true)] int? depth
        ) {
            XDoc toc;
            PageBE page = (path == null) ? DreamContext.Current.GetState<PageBE>("CurrentPage") : GetPage(path, Permissions.READ, true);

            // if the page was not found, create an empty table of contents
            if(page == null || page.ID == 0) {
                return new XDoc("html").Elem("body", "page not found");
            }

            // check if we're including the toc for the current page
            if((path == null) || page.Title.AsPrefixedDbPath().EqualsInvariantIgnoreCase(path)) {

                // we're embedding the toc for the current page, just embed the place holder
                toc = ExtensionBL.TOC;

                // check if we need to limit the depth
                if(depth != null) {
                    toc = toc.Clone();
                    toc["body/span"].Attr("depth", depth.ToString());
                }
                return toc;
            }

            // parse the page
            ParserResult result = DekiXmlParser.Parse(page, ParserMode.VIEW_NO_EXECUTE);
            string uri = Utils.AsPublicUiUri(page.Title);
            toc = new XDoc("html").Start("body").Start("div").Attr("class", "wiki-toc").AddNodes(DekiXmlParser.TruncateTocDepth(result.Content["body[@target='toc']"], depth)).End().End();

            // convert relative links to absolute links
            foreach(XDoc link in toc[".//@href"]) {
                string anchor = link.AsText;
                if(anchor.StartsWithInvariant("#")) {
                    link.ReplaceValue(uri + anchor);
                }
            }
            return toc;
        }

        [DekiExtFunction("wiki.tree", "Show hierarchy of pages starting at a wiki page.")]
        public XDoc WikiTree(
            [DekiExtParam("wiki page path", true)] string path,
            [DekiExtParam("nesting depth for retrieving child pages (default: all)", true)] int? depth,
            [DekiExtParam("reverse order of child pages", true)] bool? reverse
        ) {
            PageBE page = (path == null) ? DreamContext.Current.GetState<PageBE>("CurrentPage") : GetPage(path, Permissions.BROWSE, true);
            try {
                XDoc tree = PageSiteMapBL.BuildHtmlSiteMap(page, null, depth ?? int.MaxValue, reverse ?? false);
                return new XDoc("html").Start("body").Start("div").Attr("class", "wiki-tree").Add(tree["li/ul"]).End().End();
            } catch(KeyNotFoundException) {
                return XDoc.Empty;
            }
        }

        [DekiExtFunction("wiki.popular", "Show list of popular pages")]
        public XDoc WikiPopular(
            [DekiExtParam("max number of pages (default: 10)", true)] int? max
        ) {
            var popularPagesResponse = WikiApi(DekiContext.Current.Deki.Self.At("pages", "popular").With("limit", max ?? 10), null);
            XDoc popular = new XDoc("html").Start("body").Start("div").Start("ol");
            foreach(XDoc page in popularPagesResponse["page"]) {
                popular.Start("li")
                    .Start("a")
                        .Attr("href", page["uri.ui"].AsText)
                        .Value(page["title"].AsText)
                    .End()
                    .Value(string.Format(" ({0} views)", page["metrics/metric.views"].AsText))
                .End();
            }
            popular.EndAll();
            return popular;
        }

        [DekiExtFunction("wiki.search", "Find wiki pages and files.")]
        public XDoc WikiSearch(
            [DekiExtParam("search query")] string query,
            [DekiExtParam("max results (default: 10)", true)] uint? max,
            [DekiExtParam(SearchBL.SORT_FIELDS_DESCRIPTION_DEKISCRIPT, true)] string sortBy,
            [DekiExtParam("additional search constraint (default: \"\")", true)] string constraint,
            [DekiExtParam("number of results to skip (default: 0)", true)] uint? offset
        ) {
            XDoc search = Coroutine.Invoke(DekiContext.Current.Deki.Search, query, max ?? 10, offset ?? 0, sortBy, constraint, new Result<XDoc>()).Wait();
            XDoc result = new XDoc("html").Start("body").Start("div").Attr("class", "wiki-search").Start("ul");
            foreach(XDoc document in search["document"]) {
                result.Start("li").Start("a").Attr("rel", "internal").Attr("href", document["uri"].AsText).Value(document["title"].AsText).End().End();
            }
            result.End().End().End();
            return result;
        }

        [DekiExtFunction("wiki.uri", "Retrieve the full uri of a given wiki page.")]
        public string WikiUri(
            [DekiExtParam("wiki page path")] string path,
            [DekiExtParam("query", true)] string query
        ) {
            PageBE page = DreamContext.Current.GetState<PageBE>("CurrentPage");
            Title title = Title.FromUriPath(page.Title, path);
            if(query != null) {
                title.Query += query;
            }
            return Utils.AsPublicUiUri(title);
        }

        [DekiExtFunction("wiki.directory", "Show directory of wiki pages matching the search query.")]
        public XDoc WikiDirectory(
            [DekiExtParam("search query (default: all pages in main namespace)", true)] string query
        ) {
            XDoc search = Coroutine.Invoke(DekiContext.Current.Deki.Search, query ?? "namespace:main", (uint)1000, (uint)0, (string)null, "type:wiki", new Result<XDoc>()).Wait();
            CultureInfo culture = DreamContext.Current.Culture;
            search.Sort(delegate(XDoc left, XDoc right) {
                string leftTitle = left["title"].Contents;
                string rightTitle = right["title"].Contents;
                if(string.IsNullOrEmpty(leftTitle) && string.IsNullOrEmpty(rightTitle)) {
                    return 0;
                }
                if(string.IsNullOrEmpty(leftTitle)) {
                    return -1;
                }
                if(string.IsNullOrEmpty(rightTitle)) {
                    return 1;
                }
                if((char.IsLetter(leftTitle[0]) && char.IsLetter(rightTitle[0])) || (!char.IsLetter(leftTitle[0]) && !char.IsLetter(rightTitle[0]))) {
                    return string.Compare(leftTitle, rightTitle, true, culture);
                }
                if(char.IsLetter(leftTitle[0])) {
                    return 1;
                }
                return -1;
            });
            char lastLetter = '\0';
            XDoc result = new XDoc("html").Start("body").Start("div").Attr("class", "wiki-directory");
            foreach(XDoc document in search["document"]) {
                string title = document["title"].AsText;
                if(!string.IsNullOrEmpty(title)) {
                    char letter = char.ToUpperInvariant(title[0]);
                    if(char.IsLetter(letter)) {

                        // check if we're starting a new letter group
                        if(lastLetter != letter) {
                            if(lastLetter != '\0') {
                                result.End();
                            }
                            lastLetter = letter;
                            result.Elem("strong", letter.ToString()).Elem("br");
                            result.Start("ul");
                        }
                        result.Start("li").Start("a").Attr("rel", "internal").Attr("href", document["uri"].AsText).Value(title).End().End();
                    } else {

                        // special symbol case
                        if(lastLetter != '#') {
                            lastLetter = '#';
                            result.Elem("strong", "#").Elem("br");
                            result.Start("ul");
                        }
                        result.Start("li").Start("a").Attr("rel", "internal").Attr("href", document["uri"].AsText).Value(title).End().End();
                    }
                }
            }
            if(lastLetter != '\0') {
                result.End();
            }
            result.End().End();
            return result;
        }

        [DekiExtFunction("wiki.template", "Invoke a template page.")]
        public XDoc WikiTemplate(
            [DekiExtParam("template path")] string path,
            [DekiExtParam("template arguments (default: nil)", true)] object args,
            [DekiExtParam("alternative body target (default: nil)", true)] string target,
            [DekiExtParam("conflict resolution if target already exists (one of \"ignore\", \"replace\", or \"append\"; default: \"ignore\")", true)] string conflict
        ) {
            Title title = Title.FromUriPath(path);

            // Validate arguments
            if((args != null) && !(args is ArrayList) && !(args is Hashtable)) {
                throw new ArgumentException("Template arguments must a list, map, or nil.", "args");
            }

            // If the page title is prefixed with :, do not assume the template namespace
            if(title.Path.StartsWith(":")) {
                title.Path = title.Path.Substring(1);
            } else if(title.IsMain) {
                title.Namespace = NS.TEMPLATE;
            }
            XDoc result = DekiXmlParser.Include(title.AsPrefixedDbPath(), null, false, null, DekiScriptLiteral.FromNativeValue(args), 0, true, false, false);

            // check if an alternate target was provided and the page was authored with unsafecontent
            if(!string.IsNullOrEmpty(target) && DekiXmlParser.PageAuthorCanExecute()) {
                XDoc body = result["body[not(@target)]"];
                if(!body.IsEmpty) {
                    body.Attr("target", target);
                    if(!string.IsNullOrEmpty(conflict)) {
                        body.Attr("conflict", conflict);
                    }
                }
            }
            return result;
        }

        [DekiExtFunction("template", "Invoke a template page.")]
        public XDoc Template(
            [DekiExtParam("template path")] string path,
            [DekiExtParam("template arguments (default: nil)", true)] object args,
            [DekiExtParam("alternative body target (default: nil)", true)] string target,
            [DekiExtParam("conflict resolution if target already exists (one of \"ignore\", \"replace\", or \"append\"; default: \"ignore\")", true)] string conflict
        ) {
            return WikiTemplate(path, args, target, conflict);
        }

        [DekiExtFunction("wiki.contributors", "Show most active contributors for the site or a page.")]
        public XDoc WikiContributors(
            [DekiExtParam("wiki page path (default: nil)", true)] string path,
            [DekiExtParam("max results (default: 10)", true)] uint? max,
            [DekiExtParam("order by most recent (default: false)", true)] bool? recent,
            [DekiExtParam("exclude banned or inactive users (one of: none, banned, inactive, all; default: none)", true)] string exclude
        ) {

            // TODO (steveb): add 'since' parameter to limit contributors to a date range

            PageBE page = null;
            if(path != null) {

                // retrieve the page requested
                page = GetPage(path, Permissions.BROWSE, true);

                // If the page was not found, create an empty contributors list
                if(page == null || page.ID == 0) {
                    return new XDoc("html").Elem("body", "page not found");
                }
            }
            bool byRecent = recent.GetValueOrDefault();
            if(string.IsNullOrEmpty(exclude)) {
                exclude = "none";
            }

            // Retrieve contributors in a list of the form <user, userid, classification>
            IList<Tuplet<string, uint, string>> contributorInfos = DbUtils.CurrentSession.Wiki_GetContributors(page, byRecent, exclude, max ?? 10);

            // extract result into a bulleted list
            XDoc result = new XDoc("html").Start("body").Start("div").Attr("class", "wiki-contributors").Start("ul");

            foreach(Tuplet<string, uint, string> contributorInfo in contributorInfos) {
                Title title = Title.FromDbPath(NS.USER, contributorInfo.Item1, contributorInfo.Item1);
                string classification;
                if(byRecent) {
                    DateTime lastEdit = DbUtils.ToDateTime(contributorInfo.Item3);
                    if(lastEdit == DateTime.MinValue) {
                        continue;
                    }
                    classification = string.Format(" ({0})", lastEdit.ToString(DreamContext.Current.Culture.DateTimeFormat.ShortDatePattern));
                } else {
                    int edits = SysUtil.ChangeType<int>(contributorInfo.Item3);
                    if(edits == 0) {
                        continue;
                    }
                    classification = string.Format(" ({0} edits)", edits);
                }
                result.Start("li")
                          .Start("a")
                              .Attr("class", "link-user").Attr("href", Utils.AsPublicUiUri(title)).Attr("userid", contributorInfo.Item2).Value(title.AsUserFriendlyName())
                          .End()
                          .Value(classification)
                      .End();
            }
            result.End().End().End();
            return result;
        }

        [DekiExtFunction("wiki.localize", "Retrieve a localized resource string.")]
        public XDoc WikiLocalize(
            [DekiExtParam("wiki resource name", false)] string resourceName,
            [DekiExtParam("wiki resource parameters (default: nil)", true)] ArrayList resourceParameters
        ) {
            var parameters = resourceParameters == null ? null : resourceParameters.ToArray();
            var resourceValue = DekiContext.Current.Resources.Localize(resourceName, parameters);
            return DekiScriptLibrary.WebHtml(resourceValue, null, null, null);
        }

        [DekiExtFunction("wiki.languages", "Displays a page's versions written in other languages.")]
        public XDoc WikiLanugages(
            [DekiExtParam("Language to title map of all page versions written in other languages")] Hashtable languages
        ) {

            // Extract the language link information and sort
            List<Tuplet<string, string>> languageLinks = new List<Tuplet<string, string>>(languages.Count);
            foreach(DictionaryEntry entry in languages) {
                CultureInfo ci = new CultureInfo((string)entry.Key);
                Tuplet<string, string> languageLink = new Tuplet<string, string> {
                    Item1 = ci.TextInfo.ToTitleCase(ci.NativeName),
                    Item2 = Utils.AsPublicUiUri(Title.FromUriPath((string)entry.Value))
                };
                languageLinks.Add(languageLink);
            }
            languageLinks.Sort((left, right) => string.Compare(left.Item1, right.Item1, StringComparison.InvariantCulture));

            // Generate an HTML list of the language data
            XDoc result = new XDoc("html").Start("body").Attr("target", "languages");
            result.Start("ul");
            for(int i = 0; i < languageLinks.Count; i++) {
                result.Start("li").Start("a").Attr("href", languageLinks[i].Item2).Value(languageLinks[i].Item1).End().End();
            }
            return result.End().End();
        }

        [DekiExtFunction("wiki.pageexists", "Check if the given wiki page exists.")]
        public bool WikiPageExists(
            [DekiExtParam("wiki page path")] string path
        ) {
            PageBE page = GetPage(path, Permissions.BROWSE, true);
            return (page != null) && (page.ID != 0);
        }

        [DekiExtFunction("wiki.pagepermissions", "Get the effective permissions for a user and page.")]
        public Hashtable WikiPagePermissions(
            [DekiExtParam("wiki page path or id (default: current page)", true)] object page,
            [DekiExtParam("user name or id (default: current user)", true)] object user
        ) {
            UserBE userBe = null;
            PageBE pageBe = null;
            if(page == null) {
                pageBe = DreamContext.Current.GetState<PageBE>("CurrentPage");
            } else if(page is double) {
                pageBe = GetPage(SysUtil.ChangeType<uint>(page), Permissions.READ, false);
            } else if(page is string) {
                pageBe = GetPage((string)page, Permissions.READ, false);
            }
            if(user == null) {
                userBe = DekiContext.Current.User;
            } else if(user is double) {
                userBe = UserBL.GetUserById(SysUtil.ChangeType<uint>(user));
            } else if(user is string) {
                userBe = DbUtils.CurrentSession.Users_GetByName((string)user);
            }
            if(pageBe == null || userBe == null) {
                return null;
            }
            Hashtable permissions = new Hashtable(StringComparer.OrdinalIgnoreCase);
            foreach(string grant in PermissionsBL.PermissionsToArray(PermissionsBL.CalculateEffectivePageRights(pageBe, userBe))) {
                permissions.Add(grant.ToLowerInvariant(), true);
            }
            return permissions;
        }

        [DekiExtFunction("wiki.appendpath", "Append a title to a page path.")]
        public string WikiAppendPath(
            [DekiExtParam("wiki page path")] string path,
            [DekiExtParam("single page title or list of titles to append")] object title
        ) {
            PageBE page = DreamContext.Current.GetState<PageBE>("CurrentPage");
            Title pageTitle = Title.FromUriPath(page.Title, path);
            ArrayList titleList = (title as ArrayList) ?? new ArrayList { title };
            foreach(object t in titleList) {
                pageTitle = pageTitle.WithUserFriendlyName(t.ToString());
            }
            return pageTitle.AsPrefixedDbPath();
        }

        [DekiExtFunction("wiki.comments", "Get most recent comments for a page path.")]
        public ArrayList WikiComments(
            [DekiExtParam("wiki page path (default: current page)", true)] string path,
            [DekiExtParam("retrieve comments from descendant pages as well (default: false)", true)] bool? recurse,
            [DekiExtParam("filter by poster user id or name (default: all)", true)] object user,
            [DekiExtParam("maximum number of comments to return (default: 100)", true)] uint? max,
            [DekiExtParam("number of comments to skip (default: 0)", true)] uint? offset
        ) {
            uint? useridFilter = null;
            if(user != null) {

                //If a user is specified ensure that the name or id is a valid user otherwise return empty list.
                UserBE u = null;
                if(user is double) {
                    u = UserBL.GetUserById(SysUtil.ChangeType<uint>(user));
                } else if(user is string) {
                    u = DbUtils.CurrentSession.Users_GetByName((string)user);
                }
                if(u != null) {
                    useridFilter = u.ID;
                } else {
                    return new ArrayList();
                }
            }

            PageBE page = (path == null) ? DreamContext.Current.GetState<PageBE>("CurrentPage") : GetPage(path, Permissions.NONE, true);
            uint totalComments;
            IList<CommentBE> comments = CommentBL.RetrieveCommentsForPage(page, CommentFilter.NONDELETED, recurse ?? false, useridFilter, SortDirection.DESC, offset ?? 0, max ?? 100, out totalComments);
            ArrayList result = new ArrayList();
            foreach(CommentBE c in comments) {
                Hashtable entry = MakeCommentObject(c);
                result.Add(entry);
            }
            return result;
        }

        [DekiExtFunction("wiki.create", "Insert a link or button to create a new page.")]
        public XDoc WikiCreate(
            [DekiExtParam("label for edit link or button (default: \"New page\")", true)] string label,
            [DekiExtParam("path to parent page for new page (default: current page)", true)] string path,
            [DekiExtParam("template to use to populate new page (default: system default)", true)] string template,
            [DekiExtParam("show as button (default: true)", true)] bool? button,
            [DekiExtParam("new title for page (default: \"Page Title\")", true)] string title,
            [DekiExtParam("request arguments to pass to new page (default: nil)", true)] Hashtable args
        ) {

            // check if use has create access to current page
            PageBE page;
            Title pageTitle;
            if(path != null) {
                pageTitle = Title.FromUriPath(path);
                page = PageBL.GetPageByTitle(pageTitle);
            } else {
                page = DreamContext.Current.GetState<PageBE>("CurrentPage");
                pageTitle = new Title(page.Title);
            }

            // check if user provided a title for the new page
            if(!string.IsNullOrEmpty(title)) {
                pageTitle.Query = "action=edit";

                // generate fresh title and check if title is unique
                Title newPageTitle = pageTitle.WithUserFriendlyName(title);
                int suffix = 1;
                while(WikiPageExists(newPageTitle.AsPrefixedDbPath())) {

                    // TODO (steveb): we could use an exponential, followed by a binary search algorithm here instead of linear
                    newPageTitle = pageTitle.WithUserFriendlyName(string.Format("{0} ({1})", title, suffix++));
                }
                pageTitle = newPageTitle;
            } else {
                pageTitle.Query = "action=addsubpage";
            }
            bool enabled = (page != null) && PermissionsBL.IsUserAllowed(DekiContext.Current.User, page, Permissions.CREATE);

            // check if a template page is requested
            if(!string.IsNullOrEmpty(template)) {
                pageTitle.Query += "&template=" + XUri.EncodeQuery(template);
            }

            // check if request arguments are provided
            if((args != null) && (args.Count > 0)) {
                StringBuilder requestArgs = new StringBuilder();
                foreach(DictionaryEntry entry in args) {
                    if(entry.Value != null) {
                        requestArgs.AppendFormat("&{0}={1}", XUri.EncodeQuery(entry.Key.ToString()), XUri.EncodeQuery(entry.Value.ToString()));
                    } else {
                        requestArgs.AppendFormat("&{0}", XUri.EncodeQuery(entry.Key.ToString()));
                    }
                }
                pageTitle.Query += requestArgs.ToString();
            }

            // determine required on-click code
            string onClick = string.Format("window.location='/{0}'", pageTitle.AsUiUriPath(true).EscapeString());

            // determine label
            var resources = DekiContext.Current.Resources;
            label = label ?? resources.Localize(DekiResources.NEW_PAGE());

            // create link text
            XDoc result = new XDoc("html").Start("body");
            if(button ?? true) {
                result.Start("input");
                result.Attr("type", "button").Attr("value", label);
                if(enabled) {
                    result.Attr("onclick", onClick).Attr("value", label);
                } else {
                    result.Attr("class", "disabled").Attr("disabled", "disabled");
                }
                result.End();
            } else {
                result.Start("a");
                if(enabled) {
                    result.Attr("href", "#").Attr("onclick", onClick);
                } else {
                    result.Attr("class", "disabled");
                }
                result.Value(label).End();
            }
            result.End();
            return result;
        }

        [DekiExtFunction("wiki.edit", "Insert a link or button to open the editor.")]
        public XDoc WikiEdit(
            [DekiExtParam("label for edit link or button (default: \"Edit page\")", true)] string label,
            [DekiExtParam("path of page to edit (default: current page)", true)] string path,
            [DekiExtParam("name of section to edit (default: edit entire page)", true)] string section,
            [DekiExtParam("show as button (default: true)", true)] bool? button,
            [DekiExtParam("template to use to populate new page (default: system default)", true)] string template,
            [DekiExtParam("request arguments to pass to page (default: nil)", true)] Hashtable args
        ) {

            // check if use has edit access to current page
            PageBE page;
            Title title = null;
            if(path != null) {
                title = Title.FromUriPath(path);
                page = PageBL.GetPageByTitle(title);
            } else {
                page = DreamContext.Current.GetState<PageBE>("CurrentPage");
            }
            bool enabled = (page != null) && PermissionsBL.IsUserAllowed(DekiContext.Current.User, page, Permissions.UPDATE);

            // determine required on-click code
            string onClick;
            if(path != null) {

                // TODO (steveb): we don't support editing a section on another page

                title.Query = "action=edit";

                // check if a template page is requested
                if(!string.IsNullOrEmpty(template)) {
                    title.Query += "&template=" + XUri.EncodeQuery(template);
                }

                // check if request arguments are provided
                if((args != null) && (args.Count > 0)) {
                    StringBuilder requestArgs = new StringBuilder();
                    foreach(DictionaryEntry entry in args) {
                        if(entry.Value != null) {
                            requestArgs.AppendFormat("&{0}={1}", XUri.EncodeQuery((string)entry.Key), XUri.EncodeQuery((string)entry.Value));
                        } else {
                            requestArgs.AppendFormat("&{0}", XUri.EncodeQuery((string)entry.Key));
                        }
                    }
                    title.Query += requestArgs.ToString();
                }

                // set onClick handler
                onClick = string.Format("window.location='{0}'", Utils.AsPublicUiUri(title).EscapeString());
            } else {
                onClick = string.IsNullOrEmpty(section) ? "doLoadEditor();return false;" : string.Format("$('#{0} + :header > div.editIcon > a:first').click()", Title.AnchorEncode(section));
            }

            // determine label
            var resources = DekiContext.Current.Resources;
            label = label ?? resources.Localize(DekiResources.EDIT_PAGE());

            // create link text
            XDoc result = new XDoc("html").Start("body");
            if(button ?? true) {
                result.Start("input");
                result.Attr("type", "button").Attr("value", label);
                if(enabled) {
                    result.Attr("onclick", onClick).Attr("value", label);
                } else {
                    result.Attr("class", "disabled").Attr("disabled", "disabled");
                }
                result.End();
            } else {
                result.Start("a");
                if(enabled) {
                    result.Attr("href", "#").Attr("onclick", onClick);
                } else {
                    result.Attr("class", "disabled");
                }
                result.Value(label).End();
            }
            result.End();
            return result;
        }

        [DekiExtFunction("wiki.getpage", "Get page object at wiki page path or id.")]
        public Hashtable WikiGetPage(
            [DekiExtParam("wiki page path or id")] object page,
            [DekiExtParam("follow redirects (default: true)", true)] bool? redirect
        ) {
            PageBE result = null;
            bool followRedirects = redirect ?? true;
            if(page is double) {
                result = GetPage(SysUtil.ChangeType<uint>(page), Permissions.READ, followRedirects);
            } else if(page is string) {
                result = GetPage((string)page, Permissions.READ, followRedirects);
            }
            return MakePageObject(result, true, followRedirects);
        }

        [DekiExtFunction("wiki.getfile", "Get file object for a given file id.")]
        public Hashtable WikiGetFile(
            [DekiExtParam("file id")] uint fileid
        ) {
            return FileProperty(fileid, false);
        }

        [DekiExtFunction("wiki.getuser", "Get user object by user name or id.")]
        public Hashtable WikiGetUser(
            [DekiExtParam("user name or id")] object user
        ) {
            UserBE result = null;
            if(user is double) {
                result = UserBL.GetUserById(SysUtil.ChangeType<uint>(user));
            } else if(user is string) {
                string username = (string)user;
                result = DbUtils.CurrentSession.Users_GetByName(username);

                // check if no user was found
                if(result == null) {

                    // check if the username included '_'
                    string altUsername = username.Replace("_", " ");
                    if(!altUsername.EqualsInvariant(username)) {

                        // try again but use ' ' instead of '_'
                        result = DbUtils.CurrentSession.Users_GetByName(altUsername);
                    }
                }
            }
            return MakeUserObject(result);
        }

        [DekiExtFunction("wiki.getsearch", "Get list of found page and file objects.")]
        public ArrayList WikiGetSearch(
            [DekiExtParam("search query")] string query,
            [DekiExtParam("max results (default: 10)", true)] uint? max,
            [DekiExtParam(SearchBL.SORT_FIELDS_DESCRIPTION_DEKISCRIPT, true)] string sortBy,
            [DekiExtParam("additional search constraint (default: \"\")", true)] string constraint,
            [DekiExtParam("number of results to skip (default: 0)", true)] uint? offset
        ) {
            XDoc search = Coroutine.Invoke(DekiContext.Current.Deki.Search, query, max ?? 10, offset ?? 0, sortBy, constraint, new Result<XDoc>()).Wait();
            ArrayList result = new ArrayList();
            foreach(XDoc document in search["document"]) {
                if(document["type"].AsText.EqualsInvariant("wiki")) {
                    result.Add(PageProperty(document["id.page"].AsUInt ?? 0, true));
                } else if(document["type"].AsText.EqualsInvariant("comment")) {
                    CommentBE comment = CommentBL.GetComment(document["id.comment"].AsUInt ?? 0);
                    result.Add(MakeCommentObject(comment));
                } else {
                    result.Add(FileProperty(document["id.file"].AsULong ?? 0, false));
                }
            }
            return result;
        }

        [DekiExtFunction("wiki.gettag", "Get specified tag.")]
        public Hashtable WikiGeTag(
            [DekiExtParam("tag name")] string tag
        ) {
            var tagBL = new TagBL();
            TagBE result = tagBL.ParseTag(tag);
            result = DbUtils.CurrentSession.Tags_GetByNameAndType(result.Name, result.Type);
            if(result != null) {
                return MakeTagObject(result);
            }
            return null;
        }

        [DekiExtFunction("wiki.inclusions", "Get list of pages that form the current inclusion chain.  The first item is the outermost page that is being loaded.  The last item is the current page, unless the current page is a template and templates are excluded (see parameters below).")]
        public ArrayList WikiInclusions(
            [DekiExtParam("list template pages as well (default: true)", true)] bool? templates
        ) {
            ParserState parseState = DekiXmlParser.GetParseState();
            ArrayList result = new ArrayList();
            foreach(PageBE page in parseState.ProcessingStack) {
                if(templates.GetValueOrDefault(true) || !page.Title.IsTemplate) {
                    result.Add(MakePageObject(page, true, true));
                }
            }
            return result;
        }

        [DekiExtFunction("wiki.version", "Get the current version number.", IsProperty = true)]
        public Hashtable WikiVersion() {
            Assembly deki = this.GetType().Assembly;
            Version v = deki.GetName().Version;
            Hashtable version = new Hashtable(StringComparer.OrdinalIgnoreCase) {
                { "major", v.Major }, 
                { "minor", v.Minor }, 
                { "build", v.Build }, 
                { "revision", v.Revision }, 
                { "text", v.ToString() }, 
                { "date", deki.GetBuildDate() }
            };
            var svnRevision = deki.GetAttribute<SvnRevisionAttribute>();
            if(svnRevision != null) {
                version.Add("svnrevision", svnRevision.Revision);
            }
            var svnBranch = deki.GetAttribute<SvnBranchAttribute>();
            if(svnBranch != null) {
                version.Add("svnbranch", svnBranch.Branch);
            }
            var gitRevision = deki.GetAttribute<GitRevisionAttribute>();
            if(gitRevision != null) {
                version.Add("gitrevision", gitRevision.Revision);
            }
            var gitBranch = deki.GetAttribute<GitBranchAttribute>();
            if(gitBranch != null) {
                version.Add("gitbranch", gitBranch.Branch);
            }
            var gitUri = deki.GetAttribute<GitUriAttribute>();
            if(gitUri != null) {
                version.Add("gituri", gitUri.Uri);
            }
            return version;
        }

        [DekiExtFunction("wiki.api", "Get an XML document from the API using the current user's credentials.")]
        public XDoc WikiApi(
            [DekiExtParam("XML source uri")] XUri source,
            [DekiExtParam("return nil when an error occurs", true)] bool? nilOnError
        ) {
            try {

                // make sure the uri doesn't point to another site
                source = source.AsLocalUri();
                if(!source.Scheme.EqualsInvariant("local")) {
                    throw new ArgumentException("the source uri must point to the current deki server", "source");
                }

                // check if we need to add the user's authtoken
                Plug api = Plug.New(source).WithTimeout(DekiScriptLibrary.DEFAULT_WEB_TIMEOUT);
                if(!UserBL.IsAnonymous(DekiContext.Current.User) && !string.IsNullOrEmpty(DekiContext.Current.AuthToken)) {
                    api = api.WithHeader(AUTHTOKEN_HEADERNAME, DekiContext.Current.AuthToken);
                }
                DreamMessage message = api.GetAsync().Wait();

                // check the response status
                if(!message.IsSuccessful) {
                    throw new Exception(message.Status == DreamStatus.UnableToConnect
                                            ? string.Format("(unable to fetch text document from uri [status: {0} ({1}), message: \"{2}\"])", (int)message.Status, message.Status, message.ToDocument()["message"].AsText)
                                            : string.Format("(unable to fetch text document from uri [status: {0} ({1})])", (int)message.Status, message.Status));
                }
                return message.ToDocument();
            } catch {
                if(!(nilOnError ?? false)) {
                    throw;
                }
            }
            return null;
        }

        [DekiExtFunction("wiki.language", "Get the effective language for a page given the page language, user language, and site language.")]
        public string WikiLanguage(
            [DekiExtParam("wiki page path", true)] string path
        ) {
            PageBE page = (path == null) ? DreamContext.Current.GetState<PageBE>("CurrentPage") : GetPage(path, Permissions.BROWSE, true);

            // first check if the page has a language preference, if so return it
            if(!string.IsNullOrEmpty(page.Language)) {
                return page.Language;
            }

            // then check if the user has a language preference, if so return it
            UserBE user = DekiContext.Current.User;
            if(!string.IsNullOrEmpty(user.Language)) {
                return user.Language;
            }

            // otherwise, default to site language
            return DekiContext.Current.Instance.SiteLanguage;
        }

        [DekiExtFunction("wiki.link", "Insert a hyperlink to a page.")]
        public XDoc WikiLink(
            [DekiExtParam("wiki page path or id")] object page,
            [DekiExtParam("link contents; can be text, an image, or another document (default: page title)", true)] object text,
            [DekiExtParam("link hover title (default: none)", true)] string title,
            [DekiExtParam("link target (default: none)", true)] string target,
            [DekiExtParam("follow redirects (default: true)", true)] bool? redirect
        ) {
            PageBE result;
            try {
                bool followRedirects = redirect ?? true;
                if(page is double) {
                    result = GetPage(SysUtil.ChangeType<uint>(page), Permissions.READ, followRedirects);
                } else if(page is string) {
                    result = GetPage((string)page, Permissions.READ, followRedirects);
                    if(result.ID == 0) {

                        // page doesn't exist
                        return null;
                    }
                } else {
                    throw new ArgumentException("parameter is neither a page path, nor page id", "page");
                }
                return DekiScriptLibrary.WebLink(Utils.AsPublicUiUri(result.Title), text ?? result.Title.DisplayName, title, target);
            } catch {
                return null;
            }
        }

        [DekiExtFunction("wiki.recentchangestimestamp", "Compute a timestamp based on recent changes in a page hierarchy.")]
        public string WikiTimestamp(
            [DekiExtParam("wiki page path", true)] string path,
            [DekiExtParam("include page changes (default: true)", true)] bool? pages,
            [DekiExtParam("include tag changes (default: true)", true)] bool? tags,
            [DekiExtParam("include comment changes (default: true)", true)] bool? comments,
            [DekiExtParam("include file changes (default: true)", true)] bool? files
        ) {
            PageBE page = (path == null) ? DreamContext.Current.GetState<PageBE>("CurrentPage") : GetPage(path, Permissions.BROWSE, true);
            return DekiContext.Current.Deki.GetRecursiveRecentChangesTimestamp(page, pages ?? true, files ?? true, comments ?? true, tags ?? true);
        }
        #endregion

        [DekiExtFunction("extension.describe", "Embed the description for an extension.")]
        internal XDoc ExtDescribe(
            [DekiExtParam("URI to a running extension or script")] XUri extension
        ) {
            XDoc doc = Plug.New(extension).Get().ToDocument();
            if(_extensionRenderXslt == null) {

                // lazy load XSLT for rendering extensions
                XDoc extensionRenderDoc = Plug.New("resource://mindtouch.deki/MindTouch.Deki.Resources.ExtensionRender.xslt").With(DreamOutParam.TYPE, MimeType.XML.FullType).Get().ToDocument();
                XslCompiledTransform extensionRenderXslt = new XslCompiledTransform();
                extensionRenderXslt.Load(new XmlNodeReader(extensionRenderDoc.AsXmlNode), null, null);
                _extensionRenderXslt = extensionRenderXslt;
            }
            return doc.TransformAsXml(_extensionRenderXslt);
        }

        #region --- Wiki Properties ---
        [DekiExtFunction("siteusercount", IsProperty = true)]
        internal int SiteUserCount() {
            return WikiUserCount();
        }

        [DekiExtFunction("sitepagecount", IsProperty = true)]
        internal int SitePageCount() {
            return WikiPageCount();
        }

        [DekiExtFunction("sitetags", IsProperty = true)]
        internal Hashtable SiteTags() {
            Hashtable result = new Hashtable(StringComparer.OrdinalIgnoreCase);
            var tagBL = new TagBL();
            foreach(TagBE tag in tagBL.GetTags(null, TagType.ALL, DateTime.MinValue, DateTime.MaxValue)) {
                result[tag.PrefixedName] = MakeTagObject(tag);
            }
            return result;
        }

        [DekiExtFunction("siteusers", IsProperty = true)]
        internal ArrayList SiteUsers() {
            var users = DbUtils.CurrentSession.Users_GetActiveUsers();
            var result = new ArrayList();
            foreach(var user in users) {
                result.Add(MakeUserObject(user));
            }
            return result;
        }

        [DekiExtFunction("user", IsProperty = true)]
        internal Hashtable UserProperty(
            [DekiExtParam("user name or id")] object user
        ) {
            return WikiGetUser(user);
        }

        [DekiExtFunction("page", IsProperty = true)]
        internal Hashtable PageProperty(
            [DekiExtParam("page id")] uint pageid,
            [DekiExtParam("follow redirects (default: false)", true)] bool? followRedirects
        ) {
            return MakePageObject(GetPage(pageid, Permissions.READ, followRedirects ?? false), true, followRedirects ?? false);
        }

        [DekiExtFunction("commenturi", IsProperty = true)]
        internal string CommentUriProperty(
            [DekiExtParam("page id")] uint pageId,
            [DekiExtParam("comment number")] uint commentNumber
        ) {
            PageBE page = PageBL.GetPageById(pageId);
            return new XUri(Utils.AsPublicUiUri(page.Title)).WithFragment("comment" + commentNumber).ToString();
        }

        [DekiExtFunction("redirect", IsProperty = true)]
        internal Hashtable PageRedirectProperty(
            [DekiExtParam("page id")] uint pageid
        ) {
            PageBE page = GetPage(pageid, Permissions.READ, false);
            if(!page.IsRedirect) {
                return null;
            }
            Hashtable result = new Hashtable(StringComparer.OrdinalIgnoreCase);
            XUri externalRedirect = PageBL.ResolveRedirectUri(page);
            if(null != externalRedirect) {
                result.Add("type", "external");
                result.Add("page", null);
                result.Add("uri", externalRedirect);
            } else {
                PageBE redirectPage = PageBL.ResolveRedirects(page);
                result.Add("page", PropertyAt("$page", redirectPage.ID, false));
                result.Add("uri", Utils.AsPublicUiUri(redirectPage.Title));
                result.Add("type", "internal");
            }
            return result;
        }

        [DekiExtFunction("pagetoc", IsProperty = true)]
        internal XDoc PageTocProperty(string path) {
            return WikiToc(path, null);
        }

        [DekiExtFunction("pagexml", IsProperty = true)]
        internal XDoc PageXmlProperty(uint pageid) {
            XDoc result = XDoc.Empty;
            PageBE page = GetPage(pageid, Permissions.READ, false);
            if(page != null) {
                result = DekiXmlParser.CreateParserDocument(page, ParserMode.RAW);
            }
            return result;
        }

        [DekiExtFunction("pagetext", IsProperty = true)]
        internal string PageTextProperty(string path) {
            return GetWikiText(path, null, 0, false, false);
        }

        [DekiExtFunction("pagecontents", IsProperty = true)]
        internal XDoc PageContentsProperty(string path) {
            return DekiXmlParser.Include(path, null, false, null, null, 0, false, false, false);
        }

        [DekiExtFunction("pageviewcount", IsProperty = true)]
        internal ulong PageViewCount(
            [DekiExtParam("page id")] uint pageId
        ) {
            return PageBL.GetViewCount(pageId);
        }

        [DekiExtFunction("rating", IsProperty = true)]
        internal Hashtable PageRating(uint pageid) {
            Hashtable ret = null;
            PageBE p = GetPage(pageid, Permissions.READ, false);
            if(p != null) {
                ret = MakeRatingObject(DbUtils.CurrentSession.Rating_GetResourceRating(pageid, RatingBE.Type.PAGE));
            }
            return ret;
        }

        [DekiExtFunction("parents", IsProperty = true)]
        internal ArrayList PageParents(
            [DekiExtParam("page id")] uint pageid,
            [DekiExtParam("follow redirects (default: false)", true)] bool? followRedirects
        ) {
            ArrayList result = new ArrayList(16);
            PageBE page = GetPage(pageid, Permissions.BROWSE, followRedirects ?? false);
            if(page != null) {
                foreach(PageBE parent in PageBL.GetParents(page)) {
                    result.Add(MakePageObject(parent, true, followRedirects ?? false));
                }
                result.Reverse();
            }
            return result;
        }

        [DekiExtFunction("subpages", IsProperty = true)]
        internal Hashtable SubPageProperty(uint pageid) {
            Hashtable result = new Hashtable(StringComparer.OrdinalIgnoreCase);
            PageBE page = GetPage(pageid, Permissions.BROWSE, false);
            if(page != null) {
                foreach(PageBE subpage in PermissionsBL.FilterDisallowed(DekiContext.Current.User, PageBL.GetChildren(page, true), false, Permissions.BROWSE)) {
                    result[subpage.Title.AsSegmentName()] = PropertyAt("$page", subpage.ID, true);
                }
            }
            return result;
        }

        [DekiExtFunction("pagecomment", IsProperty = true)]
        internal Hashtable PageCommentProperty(uint commentId) {
            var comment = CommentBL.GetComment(commentId);
            return MakeCommentObject(comment);
        }

        [DekiExtFunction("pagecomments", IsProperty = true)]
        internal ArrayList PageCommentsProperty(uint pageid) {
            ArrayList result = new ArrayList();
            PageBE page = GetPage(pageid, Permissions.READ, false);
            if(page != null) {
                uint count;
                IList<CommentBE> comments = DbUtils.CurrentSession.Comments_GetByPage(page, CommentFilter.NONDELETED, false, null, SortDirection.ASC, 0, uint.MaxValue, out count);
                foreach(CommentBE comment in comments) {
                    Hashtable entry = MakeCommentObject(comment);
                    result.Add(entry);
                }
            }
            return result;
        }

        [DekiExtFunction("pagetags", IsProperty = true)]
        internal Hashtable TagsProperty(uint pageid) {
            Hashtable result = new Hashtable(StringComparer.OrdinalIgnoreCase);
            PageBE page = GetPage(pageid, Permissions.READ, false);
            if(page != null) {
                var tagBL = new TagBL();
                foreach(TagBE tag in tagBL.GetTagsForPage(page)) {
                    result[tag.PrefixedName] = MakeTagObject(tag);
                }
            }
            return result;
        }

        [DekiExtFunction("tagged", IsProperty = true)]
        internal ArrayList TaggedProperty(uint tagid) {
            ArrayList result = new ArrayList();
            IList<ulong> pageIds = DbUtils.CurrentSession.Tags_GetPageIds(tagid);
            if(!ArrayUtil.IsNullOrEmpty(pageIds)) {
                var pages = PageBL.GetPagesByIdsPreserveOrder(pageIds);
                var filteredPages = PermissionsBL.FilterDisallowed(DekiContext.Current.User, pages, false, Permissions.BROWSE);
                filteredPages = PageBL.SortPagesByTitle(filteredPages);
                foreach(PageBE page in filteredPages) {
                    result.Add(PropertyAt("$page", page.ID));
                }
            }
            return result;
        }

        [DekiExtFunction("files", IsProperty = true)]
        internal Hashtable FilesProperty(uint pageid) {
            Hashtable result = new Hashtable(StringComparer.OrdinalIgnoreCase);
            PageBE page = GetPage(pageid, Permissions.READ, false);
            if(page != null) {
                foreach(ResourceBE file in AttachmentBL.Instance.GetPageAttachments(page.ID)) {
                    result[file.Name] = PropertyAt("$file", file.ResourceId, true);
                }
            }
            return result;
        }

        [DekiExtFunction("revisions", IsProperty = true)]
        internal ArrayList RevisionsProperty(uint pageid) {
            ArrayList result = new ArrayList();
            PageBE page = GetPage(pageid, Permissions.READ, false);
            if(page != null) {
                int count = (int)page.Revision;
                for(int i = 0; i < count; ++i) {
                    result.Add(PropertyAt("$revision", pageid, i));
                }
            }
            return result;
        }

        [DekiExtFunction("revision", IsProperty = true)]
        internal Hashtable RevisionProperty(uint pageid, ulong revision) {
            PageBE page = GetPage(pageid, Permissions.READ, false);
            if(page != null) {
                OldBE old = PageBL.GetOldRevisionForPage(page, (int)revision + 1);
                if(old != null) {
                    PageBL.CopyOldToPage(old, page, page.Title);
                    return MakePageObject(page, true, true);
                }
            }
            return null;
        }

        [DekiExtFunction("file", IsProperty = true)]
        internal Hashtable FileProperty(ulong id, bool isResourceId) {
            if(!isResourceId) {
                id = ResourceMapBL.GetResourceIdByFileId((uint)id) ?? 0;
            }
            ResourceBE file = ResourceBL.Instance.GetResource((uint)id);
            return MakeFileObject(file);
        }

        [DekiExtFunction("filedesc", IsProperty = true)]
        internal string FileDescriptionProperty(uint resourceId) {
            ResourceBE description = PropertyBL.Instance.GetAttachmentDescription(resourceId);

            // TODO (steveb): make sure we're allowed to read this property

            if((description != null) && description.MimeType.Match(MimeType.ANY_TEXT)) {
                return ResourceContentBL.Instance.Get(description).ToText();
            }
            return null;
        }

        [DekiExtFunction("pageprops", IsProperty = true)]
        internal Hashtable PagePropertiesProperty(uint pageid) {
            Hashtable result = new Hashtable(StringComparer.OrdinalIgnoreCase);
            PageBE page = GetPage(pageid, Permissions.READ, false);
            if(page != null) {
                foreach(ResourceBE property in PropertyBL.Instance.GetPageProperties(pageid)) {
                    Hashtable value = MakePropertyObject(property);
                    if((value != null) && (value.Count > 0)) {

                        // custom (urn:custom.mindtouch.com#) properties are stored using only the key following the namespace
                        if(property.Name.StartsWithInvariant(CUSTOM_PROPERTY_NAMESPACE)) {
                            result[property.Name.Substring(CUSTOM_PROPERTY_NAMESPACE.Length)] = value;
                        } else {
                            result[property.Name] = value;
                        }
                    }
                }
            }
            return result;
        }

        [DekiExtFunction("usercomments", IsProperty = true)]
        internal ArrayList UserCommentsProperty(uint userid) {
            ArrayList result = new ArrayList();
            UserBE u = UserBL.GetUserById(userid);
            if(u == null) {
                return new ArrayList();
            }
            IList<CommentBE> comments = CommentBL.RetrieveCommentsForUser(u);
            foreach(CommentBE comment in comments) {
                Hashtable entry = MakeCommentObject(comment);
                result.Add(entry);
            }
            return result;
        }

        [DekiExtFunction("userprops", IsProperty = true)]
        internal Hashtable UserPropertiesProperty(uint userid) {
            Hashtable result = new Hashtable(StringComparer.OrdinalIgnoreCase);
            foreach(ResourceBE property in PropertyBL.Instance.GetUserProperties(userid)) {
                Hashtable value = MakePropertyObject(property);
                if((value != null) && (value.Count > 0)) {

                    // custom (urn:custom.mindtouch.com#) properties are stored using only the key following the namespace
                    if(property.Name.StartsWithInvariant(CUSTOM_PROPERTY_NAMESPACE)) {
                        result[property.Name.Substring(CUSTOM_PROPERTY_NAMESPACE.Length)] = value;
                    } else {
                        result[property.Name] = value;
                    }
                }
            }
            return result;
        }

        [DekiExtFunction("usermetrics", IsProperty = true)]
        internal Hashtable UserMetrics(uint userid) {
            var metrics = DbUtils.CurrentSession.Users_GetUserMetrics(userid);
            return MakeUserMetricObject(metrics);
        }

        [DekiExtFunction("usergroups", IsProperty = true)]
        internal Hashtable UserGroupsProperty(uint userid) {
            var result = new Hashtable(StringComparer.OrdinalIgnoreCase);
            IList<GroupBE> usergroups = DbUtils.CurrentSession.Groups_GetByUser(userid);
            if(null != usergroups) {
                foreach(GroupBE group in usergroups) {
                    result.Add(group.Name, MakeGroupObject(group));
                }
            }           
            return result;
        }

        [DekiExtFunction("fileprops", IsProperty = true)]
        internal Hashtable FilePropertiesProperty(uint resourceId) {
            Hashtable result = new Hashtable(StringComparer.OrdinalIgnoreCase);
            foreach(ResourceBE property in PropertyBL.Instance.GetAttachmentProperties(resourceId)) {
                Hashtable value = MakePropertyObject(property);
                if((value != null) && (value.Count > 0)) {

                    // custom (urn:custom.mindtouch.com#) properties are stored using only the key following the namespace
                    if(property.Name.StartsWithInvariant(CUSTOM_PROPERTY_NAMESPACE)) {
                        result[property.Name.Substring(CUSTOM_PROPERTY_NAMESPACE.Length)] = value;
                    } else {
                        result[property.Name] = value;
                    }
                }
            }
            return result;
        }

        [DekiExtFunction("property", IsProperty = true)]
        internal Hashtable PropertyProperty(uint resourceid, int revision) {
            ResourceBE property = ResourceBL.Instance.GetResourceRevision(resourceid, revision);
            return MakePropertyObject(property);
        }

        [DekiExtFunction("proprevisions", IsProperty = true)]
        internal ArrayList PropertyRevisionsProperty(uint resourceid) {
            ArrayList result = new ArrayList();
            ResourceBE headRevision = ResourceBL.Instance.GetResource(resourceid);

            // TODO (steveb): make sure we're allowed to read this property

            for(int i = 1; i <= headRevision.Revision; ++i) {
                result.Add(PropertyAt("$property", headRevision.ResourceId, i));
            }
            return result;
        }
        #endregion

        #region --- Meta Functions ---
        [DekiExtFunction("meta.description", "Add a plain language description to the page.")]
        public XDoc MetaDescription(
            [DekiExtParam("description text")] string content
        ) {
            return DekiXmlParser.PageAuthorCanExecute() ? new XDoc("html").Start("head").Start("meta").Attr("name", "description").Attr("content", content).End().End() : XDoc.Empty;
        }

        [DekiExtFunction("meta.keywords", "Add keywords for search engines to the page.")]
        public XDoc MetaKeywords(
            [DekiExtParam("comma separated keywords")] string content
        ) {
            return DekiXmlParser.PageAuthorCanExecute() ? new XDoc("html").Start("head").Start("meta").Attr("name", "keywords").Attr("content", content).End().End() : XDoc.Empty;
        }

        [DekiExtFunction("meta.author", "Add the author's name to the page.")]
        public XDoc MetaAuthor(
            [DekiExtParam("author name")] string content
        ) {
            return DekiXmlParser.PageAuthorCanExecute() ? new XDoc("html").Start("head").Start("meta").Attr("name", "author").Attr("content", content).End().End() : XDoc.Empty;
        }

        [DekiExtFunction("meta.robots", "Add control statements for search engines to the page.")]
        public XDoc MetaRobots(
            [DekiExtParam("search engine control statements")] string content
        ) {
            return DekiXmlParser.PageAuthorCanExecute() ? new XDoc("html").Start("head").Start("meta").Attr("name", "robots").Attr("content", content).End().End() : XDoc.Empty;
        }

        [DekiExtFunction("meta.copyright", "Add a copyright statement to the page.")]
        public XDoc MetaCopyright(
            [DekiExtParam("copyright text")] string content
        ) {
            return DekiXmlParser.PageAuthorCanExecute() ? new XDoc("html").Start("head").Start("meta").Attr("name", "copyright").Attr("content", content).End().End() : XDoc.Empty;
        }

        [DekiExtFunction("meta.rating", "Add simple content rating to the page.")]
        public XDoc MetaRating(
            [DekiExtParam("content rating")] string content
        ) {
            return DekiXmlParser.PageAuthorCanExecute() ? new XDoc("html").Start("head").Start("meta").Attr("name", "rating").Attr("content", content).End().End() : XDoc.Empty;
        }

        [DekiExtFunction("meta.googlebot", "Add control statements for Google's search engine to the page.")]
        public XDoc MetaGoogleBot(
            [DekiExtParam("google search engine control statements")] string content
        ) {
            return DekiXmlParser.PageAuthorCanExecute() ? new XDoc("html").Start("head").Start("meta").Attr("name", "googlebot").Attr("content", content).End().End() : XDoc.Empty;
        }

        [DekiExtFunction("meta.custom", "Add a custom meta tag to the page.")]
        public XDoc MetaCustom(
            [DekiExtParam("meta property name")] string name,
            [DekiExtParam("meta property value")] string content
        ) {
            return DekiXmlParser.PageAuthorCanExecute() ? new XDoc("html").Start("head").Start("meta").Attr("name", name).Attr("content", content).End().End() : XDoc.Empty;
        }

        [DekiExtFunction("meta.refresh", "Add an automatice reload/redirect to the page.")]
        public XDoc MetaRefresh(
            [DekiExtParam("delay in seconds")] double delay,
            [DekiExtParam("redirect URI (default: current page)", true)] XUri redirect
        ) {

            // don't emit the refresh tag if the page is loaded with redirects=0
            if(DreamContext.Current.Uri.GetParam("redirects", string.Empty) == "0") {
                return XDoc.Empty;
            }

            // compose the 'content' parameter depending if a redirect uri was specified
            string content = (redirect != null) ? string.Format("{0};url={1}", delay, redirect) : delay.ToString();
            return DekiXmlParser.PageAuthorCanExecute() ? new XDoc("html").Start("head").Start("meta").Attr("http-equiv", "refresh").Attr("content", content).End().End() : XDoc.Empty;
        }
        #endregion

        //--- Methods ---
        internal DekiScriptLiteral PropertyAt(string name) {
            return DekiScriptExpression.Constant(Functions.At(name));
        }

        internal DekiScriptLiteral PropertyAt(string name, ulong id) {
            return DekiScriptExpression.Constant(Functions.At(name), new[] { DekiScriptExpression.Constant(id) });
        }

        internal DekiScriptLiteral PropertyAt(string name, ulong id1, ulong id2) {
            return DekiScriptExpression.Constant(Functions.At(name), new[] { DekiScriptExpression.Constant(id1), DekiScriptExpression.Constant(id2) });
        }

        internal DekiScriptLiteral PropertyAt(string name, ulong id, int rev) {
            return DekiScriptExpression.Constant(Functions.At(name), new[] { DekiScriptExpression.Constant(id), DekiScriptExpression.Constant(rev) });
        }

        internal DekiScriptLiteral PropertyAt(string name, ulong id, bool flag) {
            return DekiScriptExpression.Constant(Functions.At(name), new[] { DekiScriptExpression.Constant(id), DekiScriptExpression.Constant(flag) });
        }

        internal DekiScriptLiteral PropertyAt(string name, string text) {
            return DekiScriptExpression.Constant(Functions.At(name), new[] { DekiScriptExpression.Constant(text) });
        }

        internal Hashtable MakePageObject(PageBE page, bool skipPermissionCheck, bool followRedirects) {
            Hashtable result = new Hashtable(StringComparer.OrdinalIgnoreCase);
            if((page != null) && (page.ID != 0) && (skipPermissionCheck || PermissionsBL.IsUserAllowed(DekiContext.Current.User, page, Permissions.READ))) {
                PageBE current = DreamContext.Current.GetState<PageBE>("CurrentPage");
                if((current != null) && (current.ID == page.ID)) {
                    result.Add("toc", ExtensionBL.TOC);
                } else {
                    result.Add("toc", PropertyAt("$pagetoc", page.Title.AsPrefixedDbPath()));
                }
                XUri apiUri = Utils.AsPublicApiUri("pages", page.ID);
                if(!followRedirects) {
                    apiUri = apiUri.With("redirects", "0");
                }
                if(page.Title.IsUser || page.Title.IsUserTalk) {
                    string namespaceUsername = page.Title.AsUnprefixedDbSegments()[0];
                    result.Add("namespaceuser", PropertyAt("$user", namespaceUsername));
                }

                // add page properties
                result.Add("id", page.ID);
                result.Add("name", page.Title.AsSegmentName());
                result.Add("title", page.Title.AsUserFriendlyName());
                result.Add("path", page.Title.AsPrefixedDbPath());
                result.Add("unprefixedpath", page.Title.AsUnprefixedDbPath());
                result.Add("namespace", Title.NSToString(page.Title.Namespace));
                result.Add("revision", page.Revision);
                result.Add("language", string.IsNullOrEmpty(page.Language) ? null : page.Language);
                result.Add("uri", Utils.AsPublicUiUri(page.Title));
                result.Add("api", apiUri.ToString());
                result.Add("date", DekiScriptLibrary.CultureDateTime(page.TimeStamp));
                result.Add("viewcount", PropertyAt("$pageviewcount", page.ID));
                result.Add("mime", page.ContentType);
                result.Add("editsummary", page.Comment);
                result.Add("author", PropertyAt("$user", page.UserID));
                result.Add("subpages", PropertyAt("$subpages", page.ID));
                result.Add("comments", PropertyAt("$pagecomments", page.ID));
                result.Add("files", PropertyAt("$files", page.ID));
                result.Add("tags", PropertyAt("$pagetags", page.ID));
                result.Add("xml", PropertyAt("$pagexml", page.ID));
                result.Add("text", PropertyAt("$pagetext", page.Title.AsPrefixedDbPath()));
                result.Add("feed", apiUri.At("feed").ToString());
                result.Add("revisions", PropertyAt("$revisions", page.ID));
                result.Add("fronturi", Utils.AsPublicUiUri(page.Title.AsFront()));
                result.Add("talkuri", Utils.AsPublicUiUri(page.Title.AsTalk()));
                result.Add("properties", PropertyAt("$pageprops", page.ID));
                result.Add("rating", PropertyAt("$rating", page.ID));
                result.Add("ishidden", page.IsHidden);
                result.Add("contents", PropertyAt("$pagecontents", page.Title.AsPrefixedDbPath()));

                // check whether this page is a redirect
                if(page.IsRedirect) {
                    result.Add("redirect", PropertyAt("$redirect", page.ID));
                } else {
                    result.Add("redirect", null);
                }

                // compute parent ID
                DekiInstance instance = DekiContext.Current.Instance;
                ulong parentID = page.ParentID;
                if(parentID == 0) {
                    parentID = ((page.ID == instance.HomePageId) ? 0 : instance.HomePageId);
                }
                result.Add("parent", PropertyAt("$page", parentID, followRedirects));
                result.Add("parents", PropertyAt("$parents", page.ID, followRedirects));

                // add additional meta-data if page revision is hidden
                if(page.IsHidden) {
                    var hidden = new Hashtable(StringComparer.OrdinalIgnoreCase);
                    DateTime? hiddenTs = page.MetaXml.RevisionHiddenTimestamp;
                    if(hiddenTs != null) {
                        hidden.Add("date", DekiScriptLibrary.CultureDateTime(hiddenTs.Value));
                    }
                    hidden.Add("description", page.MetaXml.RevisionHiddenComment ?? string.Empty);
                    hidden.Add("user", PropertyAt("$user", page.MetaXml.RevisionHiddenUserId ?? 0));
                    result.Add("hidden", hidden);
                }
            }
            return result;
        }

        internal Hashtable MakeUserObject(UserBE user) {

            // initialize gravatar link
            DekiInstance deki = DekiContext.Current.Instance;
            XUri gravatar = new XUri("http://www.gravatar.com/avatar");

            // add size, if any
            string size = deki.GravatarSize;
            if(size != null) {
                gravatar = gravatar.With("s", size);
            }

            // add rating, if any
            string rating = deki.GravatarRating;
            if(rating != null) {
                gravatar = gravatar.With("r", rating);
            }

            // add default icon, if any
            string def = deki.GravatarDefault;
            if(def != null) {
                gravatar = gravatar.With("d", def);
            }

            // initialize user object
            Hashtable result = new Hashtable(StringComparer.OrdinalIgnoreCase);
            string hash = string.Empty;
            if(user != null) {
                var env = DreamContext.Current.GetState<DekiScriptEnv>();
                string secure = deki.GravatarSalt ?? string.Empty;
                if(!secure.EqualsInvariantIgnoreCase("hidden") && !string.IsNullOrEmpty(user.Email)) {
                    hash = StringUtil.ComputeHashString(secure + (user.Email ?? string.Empty).Trim().ToLowerInvariant(), Encoding.UTF8);
                }
                PageBE homePage = UserBL.GetHomePage(user);
                result.Add("id", user.ID);
                result.Add("name", user.Name);
                result.Add("fullname", !string.IsNullOrEmpty(user.RealName) ? user.RealName : null);
                result.Add("uri", Utils.AsPublicUiUri(homePage.Title));
                result.Add("api", Utils.AsPublicApiUri("users", user.ID).ToString());
                result.Add("homepage", PropertyAt("$page", homePage.ID, true));
                result.Add("anonymous", UserBL.IsAnonymous(user));
                result.Add("admin", PermissionsBL.IsUserAllowed(user, Permissions.ADMIN));
                result.Add("feed", Utils.AsPublicApiUri("users", user.ID).At("feed").ToString());
                result.Add("authtoken", (!env.IsSafeMode && DekiContext.Current.User.ID == user.ID) ? DekiContext.Current.AuthToken : null);
                result.Add("properties", PropertyAt("$userprops", user.ID));
                result.Add("comments", PropertyAt("$usercomments", user.ID));
                result.Add("timezone", DekiScriptLibrary.RenderTimeZone(DekiScriptLibrary.ParseTimeZone(user.Timezone)));
                result.Add("language", DekiScriptExpression.Constant(string.IsNullOrEmpty(user.Language) ? null : user.Language));
                result.Add("metrics", PropertyAt("$usermetrics", user.ID));
                if(Utils.ShowPrivateUserInfo(user)) {
                    result.Add("email", user.Email);
                }
                result.Add("groups", PropertyAt("$usergroups", user.ID));
            } else {
                result.Add("id", 0);
                result.Add("name", null);
                result.Add("fullname", null);
                result.Add("uri", null);
                result.Add("api", null);
                result.Add("homepage", null);
                result.Add("anonymous", true);
                result.Add("admin", false);
                result.Add("feed", null);
                result.Add("authtoken", null);
                result.Add("properties", new Hashtable(StringComparer.OrdinalIgnoreCase));
                result.Add("comments", new ArrayList());
                result.Add("timezone", "GMT");
                result.Add("language", DekiScriptNil.Value);
                result.Add("metrics", new Hashtable(StringComparer.OrdinalIgnoreCase));
                result.Add("groups", new Hashtable(StringComparer.OrdinalIgnoreCase));
            }

            // set the emailhash and gravatar values
            if(!string.IsNullOrEmpty(hash)) {
                result.Add("emailhash", hash);
                result.Add("gravatar", gravatar.At(hash + ".png"));
            } else {
                result.Add("emailhash", string.Empty);
                result.Add("gravatar", gravatar.At("no-email.png"));
            }
            return result;
        }

        internal Hashtable MakeGroupObject(GroupBE group) {
            var result = new Hashtable(StringComparer.OrdinalIgnoreCase);
            if(group != null) {
                result.Add("name", group.Name);
                result.Add("id", group.Id);
            }
            return result;
        }

        internal Hashtable MakeFileObject(ResourceBE file) {
            Hashtable result = new Hashtable(StringComparer.OrdinalIgnoreCase);
            if((file != null) && PermissionsBL.IsUserAllowed(DekiContext.Current.User, PageBL.GetPageById(file.ParentPageId.Value), Permissions.READ)) {
                result.Add("editsummary", file.ChangeDescription);
                result.Add("description", PropertyAt("$filedesc", file.ResourceId));
                result.Add("name", file.Name);
                result.Add("id", file.MetaXml.FileId);
                result.Add("size", file.Size);
                result.Add("page", PropertyAt("$page", file.ParentPageId.Value));
                result.Add("mime", file.MimeType.FullType);
                result.Add("date", DekiScriptLibrary.CultureDateTime(file.Timestamp));
                result.Add("author", PropertyAt("$user", file.UserId));
                result.Add("api", Utils.AsPublicApiUri("files", file.MetaXml.FileId ?? 0).ToString());
                result.Add("uri", Utils.AsPublicApiUri("files", file.MetaXml.FileId ?? 0).At("=" + XUri.DoubleEncodeSegment(file.Name)).ToString());
                int? width = file.MetaXml.ImageWidth;
                result.Add("imagewidth", width);
                result.Add("imageheight", file.MetaXml.ImageHeight);
                result.Add("imageframes", width.HasValue ? (object)(file.MetaXml.ImageFrames ?? 1) : null);
                result.Add("thumburi", width.HasValue ? AttachmentBL.Instance.GetUriContent(file).With("size", "thumb") : null);
                result.Add("webviewuri", width.HasValue ? AttachmentBL.Instance.GetUriContent(file).With("size", "webview") : null);
                result.Add("properties", PropertyAt("$fileprops", file.ResourceId));
            }
            return result;
        }

        internal Hashtable MakeRatingObject(RatingComputedBE rating) {
            Hashtable result = new Hashtable(StringComparer.OrdinalIgnoreCase);
            if(rating != null) {
                result.Add("count", rating.Count);
                result.Add("score", rating.Score);
                result.Add("trendscore", rating.ScoreTrend);
                result.Add("date", DekiScriptLibrary.CultureDateTime(rating.Timestamp));
            } else {
                result.Add("count", 0);
            }
            return result;
        }

        internal Hashtable MakeUserMetricObject(UserMetrics userMetrics) {
            Hashtable result = new Hashtable(StringComparer.OrdinalIgnoreCase);
            if(userMetrics != null) {
                result.Add("comments", userMetrics.CommentPosts);
                result.Add("pages-created", userMetrics.PagesCreated);
                result.Add("pages-edited", userMetrics.PagesChanged);
                result.Add("files-added", userMetrics.FilesUploaded);
                result.Add("ratings-up", userMetrics.UpRatings);
                result.Add("ratings-down", userMetrics.DownRatings);
            } else {
                result.Add("comments", 0);
                result.Add("pages-created", 0);
                result.Add("pages-edited", 0);
                result.Add("files-added", 0);
                result.Add("ratings-up", 0);
                result.Add("ratings-down", 0);
            }
            return result;
        }

        internal Hashtable MakeTagObject(TagBE tag) {
            Hashtable result = new Hashtable(StringComparer.OrdinalIgnoreCase) {
                { "name", tag.PrefixedName }, 
                { "prefix", tag.Prefix }, 
                { "value", tag.Name }, 
                { "type", tag.Type.ToString().ToLowerInvariant() }
            };

            // add related pages
            if(tag.RelatedPages != null) {
                var sortedPages = PageBL.SortPagesByTitle(tag.RelatedPages);
                ArrayList pages = new ArrayList();
                foreach(PageBE related in sortedPages) {
                    pages.Add(PropertyAt("$page", related.ID));
                }
                result.Add("pages", pages);
            } else {
                result.Add("pages", PropertyAt("$tagged", tag.Id));
            }

            // add page
            if(tag.DefinedTo != null) {
                result.Add("definition", PropertyAt("$page", tag.DefinedTo.ID));
            } else {
                result.Add("definition", null);
            }
            return result;
        }

        internal Hashtable MakePropertyObject(ResourceBE property) {
            if(property == null) {
                return null;
            }
            Hashtable result = new Hashtable(StringComparer.OrdinalIgnoreCase);

            // check permission & set parent reference
            if(property.ParentPageId != null) {
                if(!PermissionsBL.IsUserAllowed(DekiContext.Current.User, PageBL.GetPageById(property.ParentPageId.Value), Permissions.READ)) {
                    return result;
                }

                // set parent
                result.Add("page", PropertyAt("$page", property.ParentPageId.Value));

                // check if property is head revision (API doesn't support access to older revisions)
                if(property.IsHeadRevision()) {
                    result.Add("api", property.PropertyUri(Self.At("pages", property.ParentPageId.Value.ToString())));
                }
            } else if(property.ParentUserId != null) {

                // check that the current user can access the properties of the specified user
                if((property.ParentUserId != DekiContext.Current.User.ID) && !PermissionsBL.IsUserAllowed(DekiContext.Current.User, Permissions.ADMIN)) {
                    return result;
                }

                // set parent
                result.Add("user", PropertyAt("$user", property.ParentUserId.Value));

                // check if property is head revision (API doesn't support access to older revisions)
                if(property.IsHeadRevision()) {
                    result.Add("api", property.PropertyUri(Self.At("users", property.ParentUserId.Value.ToString())));
                }
            } else if(property.ParentId != null) {

                // parent must be a file resource, retrieve the parent and check it
                ResourceBE parent = ResourceBL.Instance.GetResource(property.ParentId.Value);

                // the parent must have a parent page id, o/w something isn't what we expect it to be
                if(parent == null) {
                    return result;
                }
                PageBE page = PageBL.GetPageById(parent.ParentPageId.Value);
                if(!PermissionsBL.IsUserAllowed(DekiContext.Current.User, page, Permissions.READ)) {
                    return result;
                }

                // set parent
                uint? fileId = ResourceMapBL.GetFileIdByResourceId(property.ParentId.Value);
                if(fileId != null) {
                    result.Add("file", PropertyAt("$file", fileId.Value));

                    // check if property is head revision (API doesn't support access to older revisions)
                    if(property.IsHeadRevision()) {
                        result.Add("api", property.PropertyUri(Self.At("files", fileId.Value.ToString())));
                    }
                } else {
                    _log.WarnFormat("Could not find parent of property {0} (expected parent to be a file)", property.ResourceId);
                }
            } else {

                // must be a site property 
                if(!PermissionsBL.IsUserAllowed(DekiContext.Current.User, Permissions.ADMIN)) {
                    return result;
                }
            }

            // set property information fields
            result.Add("editsummary", property.ChangeDescription);
            result.Add("language", property.Language);
            result.Add("mime", property.MimeType.FullType);

            // split name into namespace-name pair
            int index = property.Name.IndexOf('#');
            if(index >= 0) {
                result.Add("namespace", property.Name.Substring(0, index));
                result.Add("name", property.Name.Substring(index + 1));
            } else {
                result.Add("namespace", string.Empty);
                result.Add("name", property.Name);
            }

            // get authorship information
            result.Add("size", property.Size);
            result.Add("date", property.Timestamp);
            result.Add("author", PropertyAt("$user", property.UserId));
            result.Add("revision", property.Revision);
            result.Add("revisions", PropertyAt("$proprevisions", property.ResourceId));

            // set content values
            var content = ResourceContentBL.Instance.Get(property);
            if(property.MimeType.Match(MimeType.ANY_TEXT)) {
                result.Add("text", content.ToText());
                result.Add("xml", null);
            } else if(property.MimeType.IsXml) {
                string text = content.ToText();
                result.Add("text", text);
                result.Add("xml", DekiScriptExpression.Constant(XDocFactory.From(text, MimeType.XML)));
            } else {

                // TODO (steveb): binary types are not yet supported
                result.Add("text", null);
                result.Add("xml", null);
            }
            return result;
        }

        internal Hashtable MakeCommentObject(CommentBE comment) {
            Hashtable entry = new Hashtable(StringComparer.OrdinalIgnoreCase) {
                { "text", comment.Content }, 
                { "mime", comment.ContentMimeType }, 
                { "date", DekiScriptLibrary.CultureDateTime(comment.CreateDate) }, 
                { "author", PropertyAt("$user", comment.PosterUserId) }, 
                { "page", PropertyAt("$page", comment.PageId) }, 
                { "uri", PropertyAt("$commenturi", comment.PageId, comment.Number) }, 
                { "number", comment.Number }
            };
            return entry;
        }

        private PageBE GetPage(uint pageid, Permissions permissions, bool followRedirects) {
            return ResolvePage(PageBL.GetPageById(pageid), permissions, followRedirects);
        }

        private PageBE GetPage(string path, Permissions permissions, bool followRedirects) {

            // compute target page title based on current page (only used by relative paths)
            PageBE page = DreamContext.Current.GetState<PageBE>("CurrentPage");
            Title title = Title.FromUriPath(page.Title, path);

            // load page
            page = PageBL.GetPageByTitle(title);
            return ResolvePage(page, permissions, followRedirects);
        }

        private PageBE ResolvePage(PageBE page, Permissions permissions, bool followRedirects) {
            if(page != null) {
                if(followRedirects) {
                    page = PageBL.ResolveRedirects(page);
                }
                if((page != null) && !PermissionsBL.IsUserAllowed(DekiContext.Current.User, page, permissions)) {
                    page = null;
                }
            }
            return page;
        }

        private string ResolvePath(string path) {
            PageBE page = DreamContext.Current.GetState<PageBE>("CurrentPage");
            Title title = Title.FromUriPath(page.Title, path);
            return title.AsPrefixedDbPath();
        }
    }
}

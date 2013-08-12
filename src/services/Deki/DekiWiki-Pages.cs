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
using System.Diagnostics;
using System.IO;
using MindTouch.Deki.Data;
using MindTouch.Deki.Exceptions;
using MindTouch.Deki.Export;
using MindTouch.Deki.Logic;
using MindTouch.Deki.Profiler;
using MindTouch.Deki.Script.Runtime;
using MindTouch.Deki.Script.Runtime.Library;
using MindTouch.Dream;
using MindTouch.Tasking;
using MindTouch.Xml;

namespace MindTouch.Deki {
    using Yield = IEnumerator<IYield>;

    public partial class DekiWikiService {

        //--- Class Methods ---
        private static PageBE PageBL_GetPageFromUrl(DreamContext context, bool mustExist) {

            // TODO (steveb): replace all PageBL_GetPageFromUrl() calls

            var pageid = context.GetParam(PARAM_PAGEID);
            var redirects = DreamContext.Current.GetParam(PARAM_REDIRECTS, int.MaxValue);
            return PageBL_GetPage(pageid, redirects, mustExist);
        }

        private static PageBE PageBL_GetPage(string pageid, int? redirects, bool mustExist) {
            PageBE result = PageBL_GetPageFromPathSegment(mustExist, pageid);
            if(null != result) {
                result = PageBL.ResolveRedirects(result, redirects ?? int.MaxValue);
            }
            return result;
        }

        private ParserMode ExtractParserMode(string modeStr) {
            ParserMode mode = ParserMode.VIEW;
            switch(modeStr) {
                case "edit":
                    mode = ParserMode.EDIT;
                    break;
                case "raw":
                    mode = ParserMode.RAW;
                    break;
                case "view":
                    mode = ParserMode.VIEW;
                    break;
                case "viewnoexecute":
                    mode = ParserMode.VIEW_NO_EXECUTE;
                    break;
            }
            return mode;
        }

        public static PageBE PageBL_AuthorizePage(DreamContext context, UserBE user, Permissions access, bool ignoreHomepageException) {
            if(user == null) {
                user = DekiContext.GetContext(context).User;
            }
            return PageBL.AuthorizePage(user, access, PageBL_GetPageFromUrl(context, true), ignoreHomepageException);
        }

        public static PageBE PageBL_GetPageFromPathSegment(bool mustExist, string pageid) {
            PageBE result;

            // check the format of the pageid
            ulong id;
            if(ulong.TryParse(pageid, out id)) {
                result = PageBL.GetPageById(id);
            } else {
                Title title = Title.FromApiParam(pageid);
                if(null == title) {
                    throw new PageIdParameterInvalidArgumentException();
                }
                result = PageBL.GetPageByTitle(title);
            }

            // if an ID was specified and the page was not found or if a name was specified and the caller requires it to exist,
            // fail immediately.
            if((result == null) || (result.ID == 0) && mustExist) {
                throw new PageNotFoundException();
            }
            return result;
        }

        //--- Features ---
        [DreamFeature("GET:pages/{pageid}", "Retrieve aggregate page information including attachments")]
        [DreamFeatureParam("{pageid}", "string", "either an integer page ID, \"home\", or \"=\" followed by a double uri-encoded page title")]
        [DreamFeatureParam("redirects", "int?", "If zero, do not follow page redirects.")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        [DreamFeatureParam("exclude", "string?", "Elements to exclude from response document (choice of \"inbound\", \"outbound\"; default: exclude nothing)")]
        [DreamFeatureParam("include", "string?", "Extra elements to include (choice of \"contents\"; default: include nothing extra)")]
        [DreamFeatureParam("mode", "{edit, raw, view}", "render content for different uses; default is 'view'")]
        [DreamFeatureParam("revision", "string?", "Page revision to retrieve. 'head' by default will retrieve latest revision. positive integer will retrieve specific revision")]
        [DreamFeatureParam("format", "{html, xhtml}?", "Result format (default: html)")]
        [DreamFeatureStatus(DreamStatus.Ok, "The request completed successfully")]
        [DreamFeatureStatus(DreamStatus.BadRequest, "Invalid input parameter or request body")]
        [DreamFeatureStatus(DreamStatus.Forbidden, "Read access to the page is required")]
        [DreamFeatureStatus(DreamStatus.NotFound, "Requested page could not be found")]
        public XDoc GetPage(DreamContext context, string pageid, int? redirects) {
            CheckResponseCache(context, false);
            PageBE page = PageBL_GetPage(pageid, redirects, true);
            page = PageBL.AuthorizePage(DekiContext.Current.User, Permissions.READ, page, false);
            var pageContentFilterSettings = new PageContentFilterSettings {
                ExcludeInboundLinks = context.GetParam("exclude", "").Contains("inbound"),
                ExcludeOutboundLinks = context.GetParam("exclude", "").Contains("outbound"),
                IncludeContents = context.GetParam("include", "").Contains("contents"),
                ContentsMode = ExtractParserMode(context.GetParam("mode", "view").ToLowerInvariant()),
                Revision = context.GetParam("revision", "HEAD"),
                Xhtml = context.GetParam("format", "html").EqualsInvariant("xhtml")
            };

            // track page view count
            if((pageContentFilterSettings.ContentsMode == ParserMode.VIEW || pageContentFilterSettings.ContentsMode == ParserMode.VIEW_NO_EXECUTE)
                && pageContentFilterSettings.IncludeContents
                && DekiContext.Current.Instance.StatsPageHitCounter) {
                PageBL.IncrementViewCount(page);
            }
            return PageBL.GetPageXmlVerbose(page, null, pageContentFilterSettings);
        }

        [DreamFeature("POST:pages/{pageid}/allowed", "Filter a list of user ids based on access to the page")]
        [DreamFeatureParam("{pageid}", "int", "integer page ID")]
        [DreamFeatureParam("permissions", "string?", "A comma separated list of permissions that must be satisfied (e.g read, etc.). Defaults to read, if not provided")]
        [DreamFeatureParam("filterdisabled", "bool?", "Consider disabled users to be disallowed, regardless of permissions (default: false)")]
        [DreamFeatureStatus(DreamStatus.Ok, "The request completed successfully")]
        [DreamFeatureStatus(DreamStatus.BadRequest, "Invalid input parameter or request body")]
        [DreamFeatureStatus(DreamStatus.Forbidden, "Read access to the page is required")]
        [DreamFeatureStatus(DreamStatus.NotFound, "Requested page could not be found")]
        public Yield GetPageAllowedUsers(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            List<uint> userids = new List<uint>();
            if(request.HasDocument) {
                foreach(XDoc userid in request.ToDocument()["user/@id"]) {
                    uint? id = userid.AsUInt;
                    if(id.HasValue) {
                        userids.Add(id.Value);
                    } else {
                        throw new DreamBadRequestException(string.Format("'{0}' is not a valid userid", userid.AsText));
                    }
                }
            }
            if(userids.Count == 0) {
                throw new DreamBadRequestException("must provide at least one userid");
            }
            string permissionsList = context.GetParam("permissions");
            bool filterDisabled = context.GetParam("filterdisabled", false);
            if(filterDisabled) {
                List<uint> activeUsers = new List<uint>();
                foreach(UserBE user in DbUtils.CurrentSession.Users_GetByIds(userids)) {
                    if(user.UserActive) {
                        activeUsers.Add(user.ID);
                    }
                }
                userids = activeUsers;
                if(userids.Count == 0) {
                    response.Return(DreamMessage.Ok(new XDoc("users")));
                    yield break;
                }
            }
            Permissions permissions = Permissions.READ;
            if(!string.IsNullOrEmpty(permissionsList)) {
                bool first = true;
                foreach(string perm in permissionsList.Split(',')) {
                    Permissions p;
                    if(!SysUtil.TryParseEnum(perm, out p)) {
                        throw new DreamBadRequestException(string.Format("'{0}' is not a valid permission value", perm));
                    }
                    if(first) {
                        permissions = p;
                    } else {
                        permissions |= p;
                    }
                    first = false;
                }
            }
            uint[] filteredIds = PermissionsBL.FilterDisallowed(userids.ToArray(), context.GetParam<uint>("pageid"), false, permissions);
            XDoc msg = new XDoc("users");
            foreach(int userid in filteredIds) {
                msg.Start("user").Attr("id", userid).End();
            }
            response.Return(DreamMessage.Ok(msg));
            yield break;
        }

        [DreamFeature("GET:pages/{pageid}/diff", "Show changes between revisions")]
        [DreamFeatureParam("{pageid}", "string", "either an integer page ID, \"home\", or \"=\" followed by a double uri-encoded page title")]
        [DreamFeatureParam("previous", "string?", "Previous page revision to retrieve. 'head' by default will retrieve latest revision. Positive integer will retrieve specific revision")]
        [DreamFeatureParam("redirects", "int?", "If zero, do not follow page redirects.")]
        [DreamFeatureParam("revision", "string?", "Page revision to retrieve. 'head' by default will retrieve latest revision. Positive integer will retrieve specific revision")]
        [DreamFeatureParam("mode", "{edit, raw, view}?", "which rendering mode to use when diffing; default is 'edit'")]
        [DreamFeatureParam("diff", "{combined, all}?", "Result format; 'combined' shows changes to the page contents, 'all' shows in addition the before and after versions of the page with highlighted changes; default is 'combined'")]
        [DreamFeatureParam("format", "{html, xhtml}?", "Result format (default: html)")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        [DreamFeatureStatus(DreamStatus.Ok, "The request completed successfully")]
        [DreamFeatureStatus(DreamStatus.BadRequest, "Invalid input parameter or request body")]
        [DreamFeatureStatus(DreamStatus.Forbidden, "Read access to the page is required")]
        [DreamFeatureStatus(DreamStatus.NotFound, "Requested page could not be found")]
        public Yield GetPageDiff(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            var resources = DekiContext.Current.Resources;
            string afterRev = DreamContext.Current.GetParam("revision", "head");
            string beforeRev = DreamContext.Current.GetParam("previous");
            ParserMode mode = ParserMode.EDIT;
            try {
                mode = SysUtil.ChangeType<ParserMode>(context.GetParam("mode", "edit").ToUpperInvariant());
            } catch { }

            PageBE page = PageBL_AuthorizePage(context, null, Permissions.READ, false);
            PageBL.ResolvePageRev(page, afterRev);
            ParserResult parserResult = DekiXmlParser.Parse(page, mode);

            // check if the same revision is being requested
            bool xhtml = StringUtil.EqualsInvariantIgnoreCase(context.GetParam("format", "html"), "xhtml");
            XDoc result = new XDoc("content");
            result.Attr("type", parserResult.ContentType);
            if(afterRev.EqualsInvariant(beforeRev)) {
                if(!xhtml) {
                    result.Value(parserResult.BodyText);
                } else {
                    result.AddNodes(parserResult.MainBody);
                }
            } else {
                PageBE previous = PageBL_AuthorizePage(context, null, Permissions.READ, false);
                PageBL.ResolvePageRev(previous, beforeRev);
                ParserResult previousParserResult = DekiXmlParser.Parse(previous, mode);
                DekiResource summary; // not used
                XDoc invisibleDiff;
                XDoc beforeChanges;
                XDoc afterChanges;
                XDoc combinedChanges = Utils.GetPageDiff(previousParserResult.MainBody, parserResult.MainBody, true, DekiContext.Current.Instance.MaxDiffSize, out invisibleDiff, out summary, out beforeChanges, out afterChanges);
                if(combinedChanges.IsEmpty) {

                    // if there are no visible changes and we requested the compact form, we will receive an empty document, which breaks subsequent code
                    combinedChanges = new XDoc("body");
                }
                if(!invisibleDiff.IsEmpty) {
                    combinedChanges.Start("p").Elem("strong", resources.Localize(DekiResources.PAGE_DIFF_OTHER_CHANGES())).End();
                    combinedChanges.Add(invisibleDiff);
                }
                switch(context.GetParam("diff", "combined").ToLowerInvariant()) {
                case "all":
                    if(!xhtml) {
                        result.Elem("before", beforeChanges.ToInnerXHtml());
                        result.Elem("combined", combinedChanges.ToInnerXHtml());
                        result.Elem("after", afterChanges.ToInnerXHtml());
                    } else {
                        result.Start("before").AddNodes(beforeChanges).End();
                        result.Start("combined").AddNodes(combinedChanges).End();
                        result.Start("after").AddNodes(afterChanges).End();
                    }
                    break;
                default:
                    if(!xhtml) {
                        result.Value(combinedChanges.ToInnerXHtml());
                    } else {
                        result.AddNodes(combinedChanges);
                    }
                    break;
                }
            }
            response.Return(DreamMessage.Ok(result));
            yield break;
        }

        [DreamFeature("GET:pages/{pageid}/info", "Retrieve page information")]
        [DreamFeatureParam("{pageid}", "string", "either an integer page ID, \"home\", or \"=\" followed by a double uri-encoded page title")]
        [DreamFeatureParam("redirects", "int?", "If zero, do not follow page redirects.")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        [DreamFeatureStatus(DreamStatus.Ok, "The request completed successfully")]
        [DreamFeatureStatus(DreamStatus.BadRequest, "Invalid input parameter or request body")]
        [DreamFeatureStatus(DreamStatus.Forbidden, "Read access to the page is required")]
        [DreamFeatureStatus(DreamStatus.NotFound, "Requested page could not be found")]
        public Yield GetPageInfo(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            PageBE page = PageBL_AuthorizePage(context, null, Permissions.READ, false);
            response.Return(DreamMessage.Ok(PageBL.GetPageXml(page, string.Empty)));
            yield break;
        }

        [DreamFeature("GET:pages/{pageid}/pdf", "Export a page to PDF")]
        [DreamFeatureParam("{pageid}", "string", "either an integer page ID, \"home\", or \"=\" followed by a double uri-encoded page title")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        [DreamFeatureParam("format", "string", "format to export: pdf | html (default: pdf)")]
        [DreamFeatureParam("sytlesheet", "string", "name of custom stylesheet to apply (stored in site/properties with namespace 'mindtouch.prince.stylesheet#")]
        [DreamFeatureStatus(DreamStatus.Ok, "The request completed successfully")]
        [DreamFeatureStatus(DreamStatus.BadRequest, "Invalid input parameter or request body")]
        [DreamFeatureStatus(DreamStatus.Forbidden, "Read access to the page is required")]
        [DreamFeatureStatus(DreamStatus.NotFound, "Requested page could not be found")]
        public Yield GetPageExportPDF(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            PageBE page = PageBL_AuthorizePage(context, null, Permissions.READ, false);
            string format = context.GetParam("format", "pdf");
            string stylesheetName = context.GetParam("stylesheet", null);
            string propertyName = XUri.EncodeQuery("mindtouch.prince.stylesheet#" + stylesheetName);
            string css = null;
            if(!string.IsNullOrEmpty(stylesheetName)) {

                // load the CSS from site/properties namespace: mindtouch.prince.stylesheet#mystylesheet
                yield return DekiContext.Current.ApiPlug.At("site", "properties", propertyName).With("apikey", DekiContext.Current.Deki.MasterApiKey).Get(new Result<DreamMessage>()).Set(v => css = v.ToText());
            }

            // prepare content
            string title = page.Title.AsUserFriendlyName();
            ParserResult parser = DekiXmlParser.Parse(page, ParserMode.VIEW);
            XDoc ret = new XDoc("html")
                .Start("head")
                    .Elem("title", title)
                    .AddNodes(parser.Head);

            if(!string.IsNullOrEmpty(css)) {
                ret.Start("style").Attr("type", "text/css").Value(css).End();
            }

            ret.End()
            .Start("body")
                .Elem("h1", title)
                .AddNodes(parser.MainBody)
            .End();
            // convert document
            DreamMessage responseMessage;
            if(format.EqualsInvariantIgnoreCase("html")) {
                string html = Export.PDFExport.ExportToHtml(ret);
                response.Return(DreamMessage.Ok(MimeType.HTML, html));
            } else {
                Stream stream = Export.PDFExport.ExportToPDF(ret);
                if(stream == null) {
                    throw new PagePrinceExportErrorFatalException(page.ID);
                }
                responseMessage = new DreamMessage(DreamStatus.Ok, null, MimeType.PDF, stream.Length, stream);
                responseMessage.Headers.ContentDisposition = new ContentDisposition(true, page.TimeStamp, null, null, string.Format("{0}.pdf", page.Title.AsUserFriendlyName()), null, request.Headers.UserAgent);
                response.Return(responseMessage);
            }
            yield break;
        }

        [DreamFeature("GET:pages/{pageid}/links", "Retrieve list of inbound or outbound page links")]
        [DreamFeatureParam("{pageid}", "string", "either an integer page ID, \"home\", or \"=\" followed by a double uri-encoded page title")]
        [DreamFeatureParam("dir", "{from, to}", "links pointing to a page or from a page")]
        [DreamFeatureParam("redirects", "int?", "If zero, do not follow page redirects.")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        [DreamFeatureStatus(DreamStatus.Ok, "The request completed successfully")]
        [DreamFeatureStatus(DreamStatus.BadRequest, "Invalid input parameter or request body")]
        [DreamFeatureStatus(DreamStatus.Forbidden, "Read access to the page is required")]
        [DreamFeatureStatus(DreamStatus.NotFound, "Requested page could not be found")]
        public Yield GetPageLinks(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            PageBE page = PageBL_AuthorizePage(context, null, Permissions.READ, false);

            // build response
            XDoc result;

            // get links
            switch(context.Uri.GetParam("dir", "").ToLowerInvariant()) {
            case "from":
            case "out":
                result = PageBL.GetLinksXml(DbUtils.CurrentSession.Links_GetOutboundLinks(page.ID), "outbound");
                break;
            case "to":
            case "in":
                result = PageBL.GetLinksXml(DbUtils.CurrentSession.Links_GetInboundLinks(page.ID), "inbound");
                break;
            default:
                throw new PageDirectoryInvalidArgumentException(context.Uri.GetParam("dir", ""));
            }

            response.Return(DreamMessage.Ok(result));
            yield break;
        }

        [DreamFeature("GET:pages/{pageid}/subpages", "Retrieve list of sub-pages")]
        [DreamFeatureParam("{pageid}", "string", "either an integer page ID, \"home\", or \"=\" followed by a double uri-encoded page title")]
        [DreamFeatureParam("limit", "string?", "Maximum number of items to retrieve. Must be a positive number or 'all' to retrieve all items. (default: 100)")]
        [DreamFeatureParam("offset", "int?", "Number of items to skip. Must be a positive number or 0 to not skip any. (default: 0)")]
        [DreamFeatureParam("redirects", "int?", "If zero, do not follow page redirects.")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        [DreamFeatureStatus(DreamStatus.Ok, "The request completed successfully")]
        [DreamFeatureStatus(DreamStatus.BadRequest, "Invalid input parameter or request body")]
        [DreamFeatureStatus(DreamStatus.Forbidden, "Read access to the page is required")]
        [DreamFeatureStatus(DreamStatus.NotFound, "Requested page could not be found")]
        public Yield GetPageSubpages(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            PageBE page = PageBL_AuthorizePage(context, null, Permissions.READ, false);
            uint offset;
            uint limit;
            Utils.GetOffsetAndCountFromRequest(context, uint.MaxValue, out limit, out offset);

            // build response
            XDoc result = PageBL.GetSubpageXml(page, limit, offset);
            response.Return(DreamMessage.Ok(result));
            yield break;
        }

        [DreamFeature("GET:pages/{pageid}/aliases", "Retrieve list of page aliases")]
        [DreamFeatureParam("{pageid}", "string", "either an integer page ID, \"home\", or \"=\" followed by a double uri-encoded page title")]
        [DreamFeatureParam("redirects", "int?", "If zero, do not follow page redirects.")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        [DreamFeatureStatus(DreamStatus.Ok, "The request completed successfully")]
        [DreamFeatureStatus(DreamStatus.BadRequest, "Invalid input parameter or request body")]
        [DreamFeatureStatus(DreamStatus.Forbidden, "Read access to the page is required")]
        [DreamFeatureStatus(DreamStatus.NotFound, "Requested page could not be found")]
        public Yield GetPageAliases(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            PageBE page = PageBL_AuthorizePage(context, null, Permissions.READ, false);
            XUri href = DekiContext.Current.ApiUri.At("pages", page.ID.ToString(), "aliases");
            XDoc doc = PageBL.GetPageListXml(PageBL.GetRedirectsApplyPermissions(page), "aliases", href);
            response.Return(DreamMessage.Ok(doc));
            yield break;
        }

        [DreamFeature("GET:pages/{pageid}/revisions", "Retrieve revision history of a given title")]
        [DreamFeatureParam("{pageid}", "string", "either an integer page ID, \"home\", or \"=\" followed by a double uri-encoded page title")]
        [DreamFeatureParam("limit", "string?", "Maximum number of items to retrieve. Must be a positive number or 'all' to retrieve all items. (default: 50)")]
        [DreamFeatureParam("offset", "int?", "Number of items to skip. Must be a positive number or 0 to not skip any. (default: 0)")]
        [DreamFeatureParam("redirects", "int?", "If zero, do not follow page redirects.")]
        [DreamFeatureParam("revision", "int?", "Page revision to retrieve")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        [DreamFeatureStatus(DreamStatus.Ok, "The request completed successfully")]
        [DreamFeatureStatus(DreamStatus.BadRequest, "Invalid input parameter or request body")]
        [DreamFeatureStatus(DreamStatus.Forbidden, "Read access to the page is required")]
        [DreamFeatureStatus(DreamStatus.NotFound, "Requested page could not be found")]
        public Yield GetPageRevisions(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            PageBE page = PageBL_AuthorizePage(context, null, Permissions.READ, false);
            XDoc result = null;

            // extract parameters
            int rev = context.GetParam<int>("revision", int.MinValue);
            if(rev != int.MinValue) {
                OldBE oldRev = PageBL.GetOldRevisionForPage(page, rev);

                if(oldRev != null) {
                    result = PageBL.GetOldXml(page, oldRev, null);
                } else {
                    throw new PageRevisionNotFoundException(rev, page.ID);
                }
            } else {
                uint max, offset;
                Utils.GetOffsetAndCountFromRequest(context, 50, out max, out offset);
                result = PageBL.GetOldListXml(page, DbUtils.CurrentSession.Old_GetOldsByQuery(page.ID, true, offset, max), "pages");
            }

            response.Return(DreamMessage.Ok(result));
            yield break;
        }

        [DreamFeature("GET:pages/{pageid}/files", "Retrieves a list of files for a given page")]
        [DreamFeatureParam("{pageid}", "string", "either an integer page ID, \"home\", or \"=\" followed by a double uri-encoded page title")]
        [DreamFeatureParam("redirects", "int?", "If zero, do not follow page redirects.")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        [DreamFeatureStatus(DreamStatus.Ok, "The request completed successfully")]
        [DreamFeatureStatus(DreamStatus.BadRequest, "Invalid input parameter or request body")]
        [DreamFeatureStatus(DreamStatus.Forbidden, "Read access to the page is required")]
        [DreamFeatureStatus(DreamStatus.NotFound, "Requested page could not be found")]
        public Yield GetPageFiles(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            PageBE page = PageBL_AuthorizePage(context, null, Permissions.READ, false);
            IList<ResourceBE> files = AttachmentBL.Instance.GetPageAttachments(page.ID);
            XUri href = DekiContext.Current.ApiUri.At("pages", page.ID.ToString(), "files");
            response.Return(DreamMessage.Ok(AttachmentBL.Instance.GetAttachmentRevisionListXml(files, href)));
            yield break;
        }

        [DreamFeature("GET:pages/{pageid}/files,subpages", "Retrieves a list of files and subpages for a given page")]
        [DreamFeatureParam("{pageid}", "string", "either an integer page ID, \"home\", or \"=\" followed by a double uri-encoded page title")]
        [DreamFeatureParam("redirects", "int?", "If zero, do not follow page redirects.")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        [DreamFeatureStatus(DreamStatus.Ok, "The request completed successfully")]
        [DreamFeatureStatus(DreamStatus.BadRequest, "Invalid input parameter or request body")]
        [DreamFeatureStatus(DreamStatus.Forbidden, "Read access to the page is required")]
        [DreamFeatureStatus(DreamStatus.NotFound, "Requested page could not be found")]
        public Yield GetPageSubpagesAndFiles(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            PageBE page = PageBL_AuthorizePage(context, null, Permissions.READ, false);
            IList<ResourceBE> files = AttachmentBL.Instance.GetPageAttachments(page.ID);
            XDoc ret = PageBL.GetPageXml(page, null);
            ret.Add(PageBL.GetSubpageXml(page, uint.MaxValue, 0));
            ret.Add(AttachmentBL.Instance.GetAttachmentRevisionListXml(files));
            response.Return(DreamMessage.Ok(ret));
            yield break;
        }

        [DreamFeature("POST:pages/{pageid}/revert", "Revert page to an earlier revision")]
        [DreamFeatureParam("{pageid}", "string", "either an integer page ID, \"home\", or \"=\" followed by a double uri-encoded page title")]
        [DreamFeatureParam("redirects", "int?", "If zero, do not follow page redirects.")]
        [DreamFeatureParam("fromrevision", "int", "Revision number of page that will become the new head revision")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        [DreamFeatureStatus(DreamStatus.Ok, "The request completed successfully")]
        [DreamFeatureStatus(DreamStatus.BadRequest, "Invalid input parameter or request body")]
        [DreamFeatureStatus(DreamStatus.Forbidden, "Update access to the page is required")]
        [DreamFeatureStatus(DreamStatus.NotFound, "Requested page could not be found")]
        public Yield PostPageRevert(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            PageBE page = PageBL_AuthorizePage(context, null, Permissions.UPDATE, false);

            // Note (arnec): has to be an int instead of ulong, since negative numbers have special meaning a number of layers down
            int rev = context.GetParam<int>("fromrevision");
            PageBL.RevertPageFromRevision(page, rev);
            DekiContext.Current.Instance.EventSink.PageRevert(DekiContext.Current.Now, page, DekiContext.Current.User, rev);
            response.Return(DreamMessage.Ok());
            yield break;
        }

        [DreamFeature("POST:pages/{pageid}/move", "Move page to a new location")]
        [DreamFeatureParam("{pageid}", "string", "either an integer page ID, \"home\", or \"=\" followed by a double uri-encoded page title")]
        [DreamFeatureParam("to", "string?", "new page location including the path and name of the page")]
        [DreamFeatureParam("name", "string?", "Move the page to the given name while keeping it under the same parent page")]
        [DreamFeatureParam("parentid", "int?", "Relocate the page under a given parent page")]
        [DreamFeatureParam("title", "string?", "Set the title of the page. The name of a page is also modified unless it's provided")]
        [DreamFeatureParam("redirects", "int?", "If zero, do not follow page redirects.")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        [DreamFeatureStatus(DreamStatus.Ok, "The request completed successfully")]
        [DreamFeatureStatus(DreamStatus.BadRequest, "Invalid input parameter or request body")]
        [DreamFeatureStatus(DreamStatus.Forbidden, "Update access to the page is required")]
        [DreamFeatureStatus(DreamStatus.NotFound, "Requested page could not be found")]
        [DreamFeatureStatus(DreamStatus.Conflict, "Page move would conflict with an existing page")]
        public Yield PostPageMove(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            PageBE page = PageBL_AuthorizePage(context, null, Permissions.UPDATE, false);

            string to = context.GetParam("to", null);
            string name = context.GetParam("name", null);
            string title = context.GetParam("title", null);
            ulong parentId = context.GetParam<ulong>("parentid", 0);
            IList<PageBE> movedPages = null;

            // validate display title
            if(title != null && !Title.IsValidDisplayName(title = title.Trim())) {
                throw new DreamBadRequestException("Title parameter is invalid");
            }

            // check which type of move is being requested
            if(!string.IsNullOrEmpty(to)) {

                // ensure that neither name, nor parent id are defined
                if(!string.IsNullOrEmpty(name) || (parentId != 0)) {
                    throw new DreamBadRequestException("To parameter cannot be combined with name, title, or parentid");
                }
                movedPages = PageBL.MovePage(page, to, title);
            } else {

                // validate parent id
                PageBE parentPage = null;
                if(parentId > 0) {
                    parentPage = PageBL.GetPageById(parentId);
                    if(parentPage == null) {
                        throw new DreamBadRequestException("Page given by parentid does not exist");
                    }
                }
                movedPages = PageBL.MovePage(page, parentPage, name, title);
            }
            XDoc ret = PageBL.GetPageListXml(movedPages, "pages.moved");
            response.Return(DreamMessage.Ok(ret));
            yield break;
        }

        [DreamFeature("GET:pages/{pageid}/contents", "Retrieve the contents of a page.")]
        [DreamFeature("GET:pages/{pageid}/contents/explain", "Explain how contents of a page are rendered.")]
        [DreamFeatureParam("{pageid}", "string", "either an integer page ID, \"home\", or \"=\" followed by a double uri-encoded page title")]
        [DreamFeatureParam("redirects", "int?", "If zero, do not follow page redirects.")]
        [DreamFeatureParam("mode", "{edit, raw, view}", "render content for different uses; default is 'view'")]
        [DreamFeatureParam("revision", "string?", "Page revision to retrieve. 'head' by default will retrieve latest revision. positive integer will retrieve specific revision")]
        [DreamFeatureParam("highlight", "string?", "Comma separated list of terms to highlight (default: empty)")]
        [DreamFeatureParam("format", "{html, xhtml}?", "Result format (default: html)")]
        [DreamFeatureParam("section", "int?", "The section number (default: none)")]
        [DreamFeatureParam("include", "bool?", "Treat page as an include (default: false)")]
        [DreamFeatureParam("pageid", "int?", "For template pages, use specified page ID as context for template invocation (default: none)")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        [DreamFeatureParam("relto", "int?", "Page used for path normalization (default: none)")]
        [DreamFeatureParam("reltopath", "string?", "Page used for path normalization. Ignored if relto parameter is defined. (default: none)")]
        [DreamFeatureStatus(DreamStatus.Ok, "The request completed successfully")]
        [DreamFeatureStatus(DreamStatus.BadRequest, "Invalid input parameter or request body")]
        [DreamFeatureStatus(DreamStatus.Forbidden, "Update access to the page is required")]
        [DreamFeatureStatus(DreamStatus.NotFound, "Requested page could not be found")]
        [DreamFeatureStatus(DreamStatus.NonAuthoritativeInformation, "Page contents could not be parsed in its native format and was returned in an alternative format instead")]
        public Yield GetPageContents(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            CheckResponseCache(context, false);
            var sw = Stopwatch.StartNew();
            bool explain = context.Uri.LastSegment.EqualsInvariantIgnoreCase("explain");

            PageBE page = PageBL_GetPageFromUrl(context, true);
            var deki = DekiContext.Current;
            page = PageBL.AuthorizePage(deki.User, Permissions.READ, page, false);

            // retrieve the title used for path normalization (if any)
            Title relToTitle = Utils.GetRelToTitleFromUrl(context);

            // determine what page revision is requested
            PageBL.ResolvePageRev(page, DreamContext.Current.GetParam("revision", "HEAD"));
            if(page.IsHidden) {
                PermissionsBL.CheckUserAllowed(deki.User, Permissions.ADMIN);
            }

            int section = context.GetParam<int>("section", -1);
            if((0 == section) || (section < -1)) {
                throw new SectionParamInvalidArgumentException();
            }

            // check if page should be processed as an include (used by the 'Template Dialog')
            bool isInclude = !context.GetParam("include", "false").EqualsInvariantIgnoreCase("false");
            string language = context.GetParam("lang", null);
            var mode = ExtractParserMode(context.GetParam("mode", "view").ToLowerInvariant());
            var xhtml = context.GetParam("format", "html").EqualsInvariant("xhtml");
            ParserResult parserResult;
            var contextPageId = context.Uri.GetParam("pageid", uint.MaxValue);
            var result = PageBL.RetrievePageXDoc(page, contextPageId, mode, language, isInclude, section, relToTitle, xhtml, out parserResult);
            if (result == null) {
                response.Return(DreamMessage.BadRequest(string.Format("no page exists for pageid={0}", contextPageId)));
                yield break;
            }

            // track page view count
            if((mode == ParserMode.VIEW|| mode == ParserMode.VIEW_NO_EXECUTE) && !isInclude && deki.Instance.StatsPageHitCounter) {
                PageBL.IncrementViewCount(page);
            }

            // check if we hit a snag, which is indicated by a plain-text response
            sw.Stop();
            DreamMessage msg;
            if(explain) {
                msg = DreamMessage.Ok(MimeType.XML, DekiPageProfiler.CreateProfilerDoc(sw.Elapsed, page, deki, DbUtils.CurrentSession));
            } else {

                // check if the page render too slowly, if so log it
                var renderLimit = deki.Instance.SlowPageRenderAlert;
                if((renderLimit > 0) && (sw.Elapsed.TotalSeconds >= renderLimit)) {
                    DekiPageProfiler.Log(sw.Elapsed, page, deki, DbUtils.CurrentSession);
                }

                // check the kind of render response we need to send back
                if((parserResult.ContentType == MimeType.TEXT.FullType) && (page.ContentType != MimeType.TEXT.FullType)) {

                    // something happened during parsing
                    msg = new DreamMessage(DreamStatus.NonAuthoritativeInformation, null, result);
                } else if(xhtml) {
                    msg = DreamMessage.Ok(MimeType.XHTML, result.ToXHtml(false));
                } else {
                    msg = DreamMessage.Ok(result);
                }
            }
            response.Return(msg);
            yield break;
        }

        [DreamFeature("POST:pages/{pageid}/contents", "Update contents of a page")]
        [DreamFeatureParam("{pageid}", "string", "either an integer page ID, \"home\", or \"=\" followed by a double uri-encoded page title")]
        [DreamFeatureParam("edittime", "string", "the previous revision's edit timestamp (yyyyMMddHHmmss or yyyy-MM-ddTHH:mm:ssZ) or \"now\" to bypass concurrent edit check")]
        [DreamFeatureParam("comment", "string?", "the edit comment")]
        [DreamFeatureParam("language", "string?", "the page language (default: determine culture from parent)")]
        [DreamFeatureParam("title", "string?", "the display title (default: use existing title or determine from page path.)")]
        [DreamFeatureParam("section", "int?", "the section number.  If zero, append as a new section")]
        [DreamFeatureParam("xpath", "string?", "identifies the portion of the page to update; this parameter is ignored if section is specified")]
        [DreamFeatureParam("abort", "{never, modified, exists}?", "specifies condition under which to prevent the save; default is never")]
        [DreamFeatureParam("redirects", "int?", "If zero, do not follow page redirects.")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        [DreamFeatureParam("tidy", "{remove, convert}?", "Determines if invalid content is converted to text or removed (default: 'convert')")]
        [DreamFeatureParam("relto", "int?", "Page used for path normalization (default: none)")]
        [DreamFeatureParam("reltopath", "string?", "Page used for path normalization. Ignored if relto parameter is defined. (default: none)")]
        [DreamFeatureParam("overwrite", "bool?", "New page revision is created when no changes are detected when overwrite is true (default: false)")]
        [DreamFeatureParam("importtime", "string?", "If this is an import, the edit timestamp of the imported content (yyyyMMddHHmmss or yyyy-MM-ddTHH:mm:ssZ)")]
        [DreamFeatureStatus(DreamStatus.Ok, "The request completed successfully")]
        [DreamFeatureStatus(DreamStatus.BadRequest, "Invalid input parameter or request body")]
        [DreamFeatureStatus(DreamStatus.Forbidden, "Update access to the page is required")]
        [DreamFeatureStatus(DreamStatus.NotFound, "Requested page could not be found")]
        public Yield PostPageContents(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            PageBE cur = PageBL_GetPageFromUrl(context, false);

            // load page contents based on mime type
            string contents;
            MimeType mimeType = request.ContentType;
            if(mimeType.IsXml) {
                XDoc contentsDoc = request.ToDocument();
                if(contentsDoc == null || contentsDoc.IsEmpty || !contentsDoc.HasName("content")) {
                    throw new PostedDocumentInvalidArgumentException("content");
                }
                contents = contentsDoc["body"].ToInnerXHtml();
            } else if(MimeType.TEXT.Match(mimeType) || MimeType.FORM_URLENCODED.Match(mimeType)) {
                contents = request.AsText();
            } else {
                throw new UnsupportedContentTypeInvalidArgumentException(mimeType);
            }

            bool isExistingPage = cur.ID != 0 && !cur.IsRedirect;
            string abort = context.GetParam("abort", "never").ToLowerInvariant();
            if(isExistingPage && "exists" == abort) {
                throw new PageExistsConflictException();
            }

            // Retrieve the title used for path normalization (if any)
            Title relToTitle = Utils.GetRelToTitleFromUrl(context);
            string editTimeStr = context.GetParam("edittime", null);
            DateTime editTime = DateTime.MinValue;
            if(!string.IsNullOrEmpty(editTimeStr)) {
                editTime = editTimeStr.EqualsInvariantIgnoreCase("now") ? DateTime.UtcNow : DbUtils.ToDateTime(editTimeStr);
            }
            string comment = context.GetParam("comment", String.Empty);
            string language = context.GetParam("language", null);
            string displayName = context.GetParam("title", null);
            int section = context.GetParam<int>("section", -1);
            if((section < -1) || ((!isExistingPage) && (0 < section))) {
                throw new SectionParamInvalidArgumentException();
            }

            // determin how unsafe/invalid content should be handled
            bool removeIllegalElements = StringUtil.EqualsInvariantIgnoreCase(context.GetParam("tidy", "convert"), "remove");

            // a new revision is created when no changes are detected when overwrite is enabled
            bool overwrite = context.GetParam<bool>("overwrite", false);

            // check whether the page exists and is not a redirect
            DateTime pageLastEditTime = cur.TimeStamp;
            OldBE baseOld = null;
            OldBE overwrittenOld = null;
            if(isExistingPage) {
                PageBL.AuthorizePage(DekiContext.Current.User, Permissions.UPDATE, cur, false);

                // ensure that 'edittime' is set
                if(DateTime.MinValue == editTime) {
                    throw new PageEditTimeInvalidArgumentException();
                }

                // check if page was modified since since the specified time
                if(pageLastEditTime > editTime) {

                    // ensure we're allowed to save a modified page
                    if("modified" == abort) {
                        throw new PageModifiedConflictException();
                    }

                    // if an edit has occurred since the specified edit time, retrieve the revision upon which it is based
                    // NOTE: this can be null if someone created the page after the specified edit time (ie. no common base revision)
                    baseOld = DbUtils.CurrentSession.Old_GetOldByTimestamp(cur.ID, editTime);

                    // if editing a particular section, use the page upon which the section edits were based.
                    if(0 < section && null == baseOld) {
                        throw new PageHeadingInvalidArgumentException();
                    }
                }
            }

            // save page
            bool conflict;
            try {
                overwrittenOld = PageBL.Save(cur, baseOld, comment, contents, DekiMimeType.DEKI_TEXT, displayName, language, section, context.GetParam("xpath", null), DateTime.UtcNow, 0, true, removeIllegalElements, relToTitle, overwrite, out conflict);
            } catch(DekiScriptDocumentTooLargeException e) {
                response.Return(DreamMessage.Forbidden(string.Format(e.Message)));
                yield break;
            }

            // check if this post is part of an import action
            var importTimeStr = context.GetParam("importtime", null);
            if(!string.IsNullOrEmpty(importTimeStr)) {
                var dateModified = DbUtils.ToDateTime(importTimeStr);
                var lastImport = PropertyBL.Instance.GetPageProperty((uint)cur.ID, SiteImportBuilder.LAST_IMPORT);
                var lastImportDoc = new XDoc("last-import").Elem("etag", cur.Etag).Elem("date.modified", dateModified);
                var content = new ResourceContentBE(lastImportDoc);
                if(lastImport == null) {
                    PropertyBL.Instance.CreateProperty((uint)cur.ID, PageBL.GetUri(cur), ResourceBE.ParentType.PAGE, SiteImportBuilder.LAST_IMPORT, content, string.Format("import at revision {0}", cur.Revision), content.ComputeHashString(), AbortEnum.Never);
                } else {
                    PropertyBL.Instance.UpdatePropertyContent(lastImport, content, string.Format("updated import at revision {0}", cur.Revision), content.ComputeHashString(), AbortEnum.Never, PageBL.GetUri(cur), ResourceBE.ParentType.PAGE);
                }
            }

            // generate xml output
            XDoc editXml = new XDoc("edit") { PageBL.GetPageXml(cur, String.Empty) };

            // if a non-redirect was overwritten, report it
            if((overwrittenOld != null) && (pageLastEditTime != editTime) && isExistingPage && conflict) {
                editXml.Attr("status", "conflict");
                editXml.Add(baseOld == null ? new XDoc("page.base") : PageBL.GetOldXml(cur, baseOld, "base"));
                editXml.Add(PageBL.GetOldXml(cur, overwrittenOld, "overwritten"));
            } else {
                editXml.Attr("status", "success");
            }

            response.Return(DreamMessage.Ok(editXml));
            yield break;
        }

        [DreamFeature("POST:pages/{pageid}/revisions", "Performs operations such as hide/unhide for revisions of pages")]
        [DreamFeatureParam("{pageid}", "string", "either an integer page ID, \"home\", or \"=\" followed by a double uri-encoded page title")]
        [DreamFeatureParam("comment", "string?", "Reason for hiding revisions")]
        [DreamFeatureParam("redirects", "int?", "If zero, do not follow page redirects.")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        [DreamFeatureStatus(DreamStatus.Ok, "The request completed successfully")]
        [DreamFeatureStatus(DreamStatus.BadRequest, "Invalid input parameter or request body")]
        [DreamFeatureStatus(DreamStatus.Forbidden, "DELETE access is required to hide a revision and ADMIN access to unhide")]
        [DreamFeatureStatus(DreamStatus.NotFound, "Requested page could not be found")]
        public Yield PostPageRevisions(DreamContext context, DreamMessage request, Result<DreamMessage> response) {

            // Note (arnec):Page revision hiding requires DELETE permission which by default is never authorized on the home page,
            // so we need to pass in a flag to allow the Homepage authorization exception to be ignored.
            PageBE page = PageBL_AuthorizePage(context, null, Permissions.DELETE, true);

            OldBE[] olds = PageBL.ModifyRevisionVisibility(page, request.ToDocument(), context.GetParam("comment", string.Empty));
            XDoc ret = PageBL.GetOldListXml(page, olds, "pages");
            response.Return(DreamMessage.Ok(ret));
            yield break;
        }

        [DreamFeature("POST:pages/{pageid}/index", "re-index a page and it's attributes")]
        [DreamFeatureParam("{pageid}", "string", "either an integer page ID, \"home\", or \"=\" followed by a double uri-encoded page title")]
        [DreamFeatureParam("redirects", "int?", "If zero, do not follow page redirects.")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        [DreamFeatureStatus(DreamStatus.Ok, "The request completed successfully")]
        [DreamFeatureStatus(DreamStatus.BadRequest, "Invalid input parameter or request body")]
        [DreamFeatureStatus(DreamStatus.Forbidden, "MindTouch API key or Administrator access is required.")]
        [DreamFeatureStatus(DreamStatus.NotFound, "Requested page could not be found")]
        internal Yield IndexPage(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            PageBE page = PageBL_AuthorizePage(context, null, Permissions.NONE, false);
            DekiContext.Current.Instance.EventSink.PagePoke(DekiContext.Current.Now, page, DekiContext.Current.User);
            uint commentCount;
            foreach(CommentBE comment in DbUtils.CurrentSession.Comments_GetByPage(page, CommentFilter.NONDELETED, false, null, SortDirection.UNDEFINED, 0, uint.MaxValue, out commentCount)) {
                DekiContext.Current.Instance.EventSink.CommentPoke(DekiContext.Current.Now, comment, page, DekiContext.Current.User);
            }
            response.Return(DreamMessage.Ok());
            yield break;
        }

        [DreamFeature("DELETE:pages/{pageid}", "Deletes a page and optionally descendant pages by moving them to the archive")]
        [DreamFeatureParam("{pageid}", "string", "either an integer page ID, \"home\", or \"=\" followed by a double uri-encoded page title")]
        [DreamFeatureParam("recursive", "bool?", "only delete page or delete page and descendants. Default: false")]
        [DreamFeatureParam("redirects", "int?", "If zero, do not follow page redirects.")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        [DreamFeatureStatus(DreamStatus.Ok, "Request completed successfully")]
        [DreamFeatureStatus(DreamStatus.BadRequest, "Invalid input parameter or request body")]
        [DreamFeatureStatus(DreamStatus.Forbidden, "Update/delete access to the page is required")]
        [DreamFeatureStatus(DreamStatus.NotFound, "Requested page could not be found")]
        public Yield DeletePage(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            PageBE pageToDelete = PageBL_AuthorizePage(context, null, Permissions.UPDATE | Permissions.DELETE, false);
            bool recurse = context.GetParam<bool>("recursive", false);
            PageBE[] deletedPages = PageBL.DeletePage(pageToDelete, recurse);

            XDoc responseDoc = PageBL.GetPageListXml(deletedPages, "deletedpages");
            response.Return(DreamMessage.Ok(responseDoc));
            yield break;
        }

        [DreamFeature("GET:pages", "Builds a site map starting from 'home' page.")]
        [DreamFeature("GET:pages/{pageid}/tree", "Builds a site map starting from a given page.")]
        [DreamFeatureParam("{pageid}", "string", "either an integer page ID, \"home\", or \"=\" followed by a double uri-encoded page title")]
        [DreamFeatureParam("format", "{xml, html, google}?", "Result format (default: xml)")]
        [DreamFeatureParam("startpage", "bool?", "For HTML sitemap, indicates if the start page should be included (default: true)")]
        [DreamFeatureParam("language", "string?", "Filter results by language (default: all languages)")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        [DreamFeatureStatus(DreamStatus.Ok, "The request completed successfully")]
        [DreamFeatureStatus(DreamStatus.BadRequest, "Invalid input parameter or request body")]
        [DreamFeatureStatus(DreamStatus.Forbidden, "Browse access to the page is required")]
        [DreamFeatureStatus(DreamStatus.NotFound, "Requested page could not be found")]
        public Yield GetPages(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            CheckResponseCache(context, true);

            PageBE page = null;
            string pageParam = context.GetParam("pageid", string.Empty);
            if(pageParam == string.Empty) {
                page = PageBL.GetHomePage();
            } else {
                page = PageBL_GetPageFromUrl(context, true);
            }
            PageBL.AuthorizePage(DekiContext.Current.User, Permissions.BROWSE, page, false);
            string format = context.GetParam("format", "xml");
            bool includeStartPage = context.GetParam<bool>("startpage", true);

            // extract the filter language
            string language = context.GetParam("language", null);
            if(null != language) {
                PageBL.ValidatePageLanguage(language);
            }

            XDoc retXml = null;
            try {
                switch(format.ToLowerInvariant()) {
                case "sitemap":
                case "sitemap.xml":
                case "google":
                    retXml = PageSiteMapBL.BuildGoogleSiteMap(page, language);
                    response.Return(DreamMessage.Ok(retXml));
                    break;
                case "html":
                    retXml = PageSiteMapBL.BuildHtmlSiteMap(page, language, int.MaxValue, false);
                    XDoc html = includeStartPage ? retXml : retXml["li/ul"];
                    response.Return(DreamMessage.Ok(MimeType.HTML, html.ToXHtml()));
                    break;
                case "xml":
                    retXml = PageSiteMapBL.BuildXmlSiteMap(page, language);
                    response.Return(DreamMessage.Ok(retXml));
                    break;
                default:
                    throw new PageFormatInvalidArgumentException();
                }
            } catch(TooManyResultsException e){
                _log.Warn("Refused to build sitemap for the system's stability sake", e);
                response.Return(DekiExceptionMapper.Map(e, DekiContext.Current.Resources));
            }
            yield break;
        }

        [DreamFeature("GET:pages/popular", "Retrieves a list of popular pages.")]
        [DreamFeatureParam("limit", "string?", "Maximum number of items to retrieve. Must be a positive number or 'all' to retrieve all items. (default: 50)")]
        [DreamFeatureParam("offset", "int?", "Number of items to skip. Must be a positive number or 0 to not skip any. (default: 0)")]
        [DreamFeatureParam("language", "string?", "Filter results by language (default: all languages)")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        [DreamFeatureStatus(DreamStatus.Ok, "The request completed successfully")]
        public Yield GetPopularPages(DreamContext context, DreamMessage request, Result<DreamMessage> response) {

            //Language filtering done in GetPopularPagesXml
            XDoc result = PageBL.GetPopularPagesXml(context);
            response.Return(DreamMessage.Ok(result));
            yield break;
        }

        #region security

        [DreamFeature("GET:pages/{pageid}/security", "Retrieve page security info")]
        [DreamFeatureParam("{pageid}", "string", "either an integer page ID, \"home\", or \"=\" followed by a double uri-encoded page title")]
        [DreamFeatureParam("redirects", "int?", "If zero, do not follow page redirects.")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        [DreamFeatureStatus(DreamStatus.Ok, "The request completed successfully")]
        [DreamFeatureStatus(DreamStatus.BadRequest, "Invalid input parameter or request body")]
        [DreamFeatureStatus(DreamStatus.Forbidden, "Browse access to the page is required")]
        [DreamFeatureStatus(DreamStatus.NotFound, "Requested page could not be found")]
        public Yield GetPageSecurity(DreamContext context, DreamMessage request, Result<DreamMessage> response) {

            //NOTE MaxM: This features requires either BROWSE or READ access but not both. Permissions don't currently support this sort of query so two checks are performed instead.
            // First user is checked for having READ access and if not then BROWSE access. The second call will throw an exception. The exception message is misleading since it only describes
            // that BROWSE is required.
            PageBE page = PageBL_GetPageFromUrl(context, true);
            UserBE user = DekiContext.Current.User;
            if(!PermissionsBL.IsUserAllowed(user, page, Permissions.READ)) {
                PermissionsBL.CheckUserAllowed(user, page, Permissions.BROWSE);
            }

            XDoc result = PageBL.GetSecurityXml(page);
            response.Return(DreamMessage.Ok(result));
            yield break;
        }

        [DreamFeature("DELETE:pages/{pageid}/security", "Reset page restricts and grants")]
        [DreamFeatureParam("{pageid}", "string", "either an integer page ID, \"home\", or \"=\" followed by a double uri-encoded page title")]
        [DreamFeatureParam("redirects", "int?", "If zero, do not follow page redirects.")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        [DreamFeatureStatus(DreamStatus.Ok, "The request completed successfully")]
        [DreamFeatureStatus(DreamStatus.BadRequest, "Invalid input parameter or request body")]
        [DreamFeatureStatus(DreamStatus.Forbidden, "Change permissions access to the page is required")]
        [DreamFeatureStatus(DreamStatus.NotFound, "The requested page could not be found")]
        public Yield DeletePageSecurity(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            PageBE page = PageBL_AuthorizePage(context, null, Permissions.CHANGEPERMISSIONS, false);
            PermissionsBL.DeleteAllGrantsForPage(page);

            //Clear page restriction.
            page.RestrictionID = 0;
            DbUtils.CurrentSession.Pages_Update(page);

            DekiContext.Current.Instance.EventSink.PageSecurityDelete(DekiContext.Current.Now, page);
            response.Return(DreamMessage.Ok());
            yield break;
        }

        [DreamFeature("PUT:pages/{pageid}/security", "Set page security info")]
        [DreamFeatureParam("{pageid}", "string", "either an integer page ID, \"home\", or \"=\" followed by a double uri-encoded page title")]
        [DreamFeatureParam("redirects", "int?", "If zero, do not follow page redirects.")]
        [DreamFeatureParam("cascade", "{none,delta,absolute}?", "none: Permissions are not cascaded to child pages; deltas: Changes between given page's security and proposed security cascaded to child nodes; absolute: Proposed security is set on child pages. Default: none")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        [DreamFeatureStatus(DreamStatus.Ok, "The request completed successfully")]
        [DreamFeatureStatus(DreamStatus.BadRequest, "Invalid input parameter or request body")]
        [DreamFeatureStatus(DreamStatus.Forbidden, "Change permissions access to the page is required")]
        [DreamFeatureStatus(DreamStatus.NotFound, "Requested page could not be found")]
        public Yield PutPageSecurity(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            PageBE page = PageBL_AuthorizePage(context, null, Permissions.CHANGEPERMISSIONS, false);
            CascadeType cascade;
            switch(context.GetParam("cascade", "none").ToLowerInvariant().Trim()) {
            case "delta":
                cascade = CascadeType.DELTA;
                break;
            case "absolute":
                cascade = CascadeType.ABSOLUTE;
                break;
            case "none":
                cascade = CascadeType.NONE;
                break;
            default:
                throw new CascadeParameterInvalidArgumentException();
            }

            // parse out the page restriction attribute
            string pageRestrictionStr = request.ToDocument()["permissions.page/restriction"].Contents;
            if(string.IsNullOrEmpty(pageRestrictionStr)) {
                throw new PageRestrictionInfoMissingInvalidArgumentException();
            }

            // retrieve role from the db.
            RoleBE restriction = PermissionsBL.GetRestrictionByName(pageRestrictionStr);
            if(restriction == null) {
                throw new PageRestrictionNotFoundInvalidArgumentException(pageRestrictionStr);
            }

            // check if only changes should be propagated
            if(cascade == CascadeType.DELTA) {

                // check if restriction has changed
                var currentRestriction = PermissionsBL.GetPageRestriction(page);
                if(currentRestriction.ID == restriction.ID) {
                    restriction = null;
                }
            }

            // parse given xml to GrantBE's. This throws a bad request exception on parse problems.
            List<GrantBE> grants = PermissionsBL.ReadGrantsXml(request.ToDocument()["grants"], page, false);

            // apply validation logic and add grants
            PermissionsBL.ReplacePagePermissions(page, restriction, grants, cascade);

            DekiContext.Current.Instance.EventSink.PageSecuritySet(DekiContext.Current.Now, page, cascade);

            // return the parsed grants to client
            response.Return(DreamMessage.Ok(PageBL.GetSecurityXml(page)));
            yield break;
        }

        [DreamFeature("POST:pages/{pageid}/security", "Modify page security by adding and removing grants")]
        [DreamFeatureParam("{pageid}", "string", "either an integer page ID, \"home\", or \"=\" followed by a double uri-encoded page title")]
        [DreamFeatureParam("redirects", "int?", "If zero, do not follow page redirects.")]
        [DreamFeatureParam("cascade", "{none, delta}", "Apply proposed security to child pages. default: none")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        [DreamFeatureStatus(DreamStatus.Ok, "The request completed successfully")]
        [DreamFeatureStatus(DreamStatus.BadRequest, "Invalid input parameter or request body")]
        [DreamFeatureStatus(DreamStatus.Forbidden, "Change permissions access to the page is required")]
        [DreamFeatureStatus(DreamStatus.NotFound, "Requested page could not be found")]
        public Yield PostPageSecurity(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            PageBE page = PageBL_AuthorizePage(context, null, Permissions.CHANGEPERMISSIONS, false);
            CascadeType cascade;
            switch(context.GetParam("cascade", "none").ToLowerInvariant().Trim()) {
            case "delta":
                cascade = CascadeType.DELTA;
                break;
            case "none":
                cascade = CascadeType.NONE;
                break;
            default:
                throw new CascadeParameterInvalidArgumentException();
            }
            RoleBE restriction = null;

            // parse out the OPTIONAL page restriction attribute
            string pageRestrictionStr = request.ToDocument()["permissions.page/restriction"].Contents;

            // retrieve role from the db
            if(!string.IsNullOrEmpty(pageRestrictionStr)) {
                restriction = PermissionsBL.GetRestrictionByName(pageRestrictionStr);
                if(restriction == null) {
                    throw new PageRestrictionNotFoundInvalidArgumentException(pageRestrictionStr);
                }

                // check if only changes should be propagated
                if(cascade == CascadeType.DELTA) {

                    // check if restriction has changed
                    var currentRestriction = PermissionsBL.GetPageRestriction(page);
                    if(currentRestriction.ID == restriction.ID) {
                        restriction = null;
                    }
                }
            }

            // parse given xml to GrantBE's. This throws a bad request exception on parse problems.
            List<GrantBE> grantsAdded = PermissionsBL.ReadGrantsXml(request.ToDocument()["grants.added"], page, false);
            List<GrantBE> grantsRemoved = PermissionsBL.ReadGrantsXml(request.ToDocument()["grants.removed"], page, false);
            if((grantsAdded.Count != 0) || (grantsRemoved.Count != 0) || (restriction != null)) {

                // apply validation logic and add grants.
                PermissionsBL.ApplyDeltaPagePermissions(page, restriction, grantsAdded, grantsRemoved, cascade == CascadeType.DELTA);
            }
            DekiContext.Current.Instance.EventSink.PageSecurityUpdated(DekiContext.Current.Now, page, cascade);

            // return the parsed grants to client
            response.Return(DreamMessage.Ok(PageBL.GetSecurityXml(page)));
            yield break;
        }

        [DreamFeature("POST:pages/{pageid}/message/*//*", "Post a custom page event into the pubsub bus (limited to 128KB)")]
        [DreamFeatureParam("{pageid}", "string", "either an integer page ID, \"home\", or \"=\" followed by a double uri-encoded page title")]
        [DreamFeatureStatus(DreamStatus.Ok, "The request completed successfully")]
        [DreamFeatureStatus(DreamStatus.BadRequest, "Invalid input parameter or request body")]
        [DreamFeatureStatus(DreamStatus.Forbidden, "A logged-in user is required")]
        public Yield PostPageMessage(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            if(UserBL.IsAnonymous(DekiContext.Current.User)) {
                response.Return(DreamMessage.Forbidden("A logged-in user is required"));
                yield break;
            }
            if(request.ContentLength > 128 * 1024) {
                response.Return(DreamMessage.BadRequest("Content-length cannot exceed 128KB)"));
                yield break;
            }
            PageBE page = PageBL_AuthorizePage(context, null, Permissions.READ, false);
            XDoc body = new XDoc("body");
            switch(request.ContentType.FullType) {
            case "text/plain":
                body.Attr("content-type", request.ContentType.ToString())
                    .Value(request.AsText());
                break;
            default:
                body.Attr("content-type", request.ContentType.ToString())
                    .Add(request.ToDocument());
                break;
            }
            string[] path = context.GetSuffixes(UriPathFormat.Original);
            path = ArrayUtil.SubArray(path, 1);
            DekiContext.Current.Instance.EventSink.PageMessage(DekiContext.Current.Now, page, DekiContext.Current.User, body, path);
            response.Return(DreamMessage.Ok());
            yield break;
        }

        #endregion grants

    }
}

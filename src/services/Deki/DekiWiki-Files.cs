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
using System.Text.RegularExpressions;

using MindTouch.Deki.Data;
using MindTouch.Deki.Exceptions;
using MindTouch.Deki.Logic;
using MindTouch.Dream;
using MindTouch.Tasking;
using MindTouch.Xml;

namespace MindTouch.Deki {
    using Yield = IEnumerator<IYield>;

    public partial class DekiWikiService {

        //--- Constants ---
        private static readonly Regex MSIE_USER_AGENT_REGEX = new Regex("MSIE[^;8]+(;|\\))", RegexOptions.Compiled);
        private static readonly Regex MSIE6_USER_AGENT_REGEX = new Regex("MSIE 6.0(|b)(;|\\))", RegexOptions.Compiled);
        private static readonly Regex MSWEBDAV_USER_AGENT_REGEX = new Regex("^(Microsoft-WebDAV-MiniRedir|Microsoft Data Access Internet Publishing Provider DAV|Microsoft Office Core Storage Infrastructure)", RegexOptions.Compiled);

        //--- Features ---
        [DreamFeature("GET:files", "Retrieve information for all attached files")]
        [DreamFeatureParam("skip", "int?", "Number of files to skip. Default: 0")]
        [DreamFeatureParam("numfiles", "int?", "Number of files to retrieve. 'ALL' for no limit. Default: 100")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        [DreamFeatureStatus(DreamStatus.Ok, "The request completed successfully")]
        [DreamFeatureStatus(DreamStatus.BadRequest, "Invalid input parameter or request body")]
        [DreamFeatureStatus(DreamStatus.Forbidden, "Read access to the page sis required")]
        public Yield GetFiles(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            PermissionsBL.CheckUserAllowed(DekiContext.Current.User, Permissions.READ);
            uint skip = context.GetParam<uint>("skip", 0);
            uint numfiles = 100;
            string numfilesStr = context.GetParam("numfiles", numfiles.ToString());
            if(StringUtil.EqualsInvariantIgnoreCase(numfilesStr, "ALL")) {
                numfiles = uint.MaxValue;
            } else {
                if(!uint.TryParse(numfilesStr, out numfiles))
                    throw new AttachmentCannotParseNumFilesInvalidArgumentException();
            }

            IList<ResourceBE> files = AttachmentBL.Instance.RetrieveAttachments(skip, numfiles);
            XDoc ret = AttachmentBL.Instance.GetFileXml(files, false, null, null, null);
            response.Return(DreamMessage.Ok(ret));
            yield break;
        }

        [DreamFeature("GET:pages/{pageid}/files/{filename}/info", "Retrieve file attachment information")]
        [DreamFeature("GET:files/{fileid}/info", "Retrieve file attachment information")]
        [DreamFeatureParam("{pageid}", "string", "either an integer page ID, \"home\", or \"=\" followed by a double uri-encoded page title")]
        [DreamFeatureParam("{filename}", "string", "\"=\" followed by a double uri-encoded file name")]
        [DreamFeatureParam("{fileid}", "int", "identifies a file by ID")]
        [DreamFeatureParam("revision", "string?", "File revision to retrieve. 'head' by default will retrieve latest revision. positive integer will retrieve specific revision")]
        [DreamFeatureParam("redirects", "int?", "If zero, do not follow page redirects (only applies when {pageid} is present).")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        [DreamFeatureStatus(DreamStatus.Ok, "The request completed successfully")]
        [DreamFeatureStatus(DreamStatus.BadRequest, "Invalid input parameter or request body")]
        [DreamFeatureStatus(DreamStatus.Forbidden, "Read access to the page is required")]
        [DreamFeatureStatus(DreamStatus.NotFound, "Requested file could not be found")]
        public Yield GetFileInfo(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            PageBE parentPage = null;
            ResourceBE fileRevision = GetAttachment(context, request, Permissions.READ, true, false, out parentPage);

            // found matching attachments. Put into the response XDoc
            bool? revisionInfo = null;
            if(!StringUtil.EqualsInvariantIgnoreCase(DreamContext.Current.GetParam("revision", "HEAD"), "HEAD")) {
                revisionInfo = true;
            }
            response.Return(DreamMessage.Ok(AttachmentBL.Instance.GetFileXml(fileRevision, true, null, revisionInfo)));
            yield break;
        }

        // Note (arnec): OPTIONS,LOCK,UNLOCK,PROPFIND exists to fake WebDAV editing of Office documents via IE. They do not
        // represent a proper WEBDAV implementation
        [DreamFeature("OPTIONS:pages/{pageid}/files/{filename}", "Retrieve available HTTP options")]
        [DreamFeature("OPTIONS:files/{fileid}", "Retrieve available HTTP options")]
        [DreamFeature("OPTIONS:files/{fileid}/{filename}", "Retrieve available HTTP options")]
        public Yield GetOptions(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            DreamMessage responseMsg = DreamMessage.Ok();
            responseMsg.Headers.Add("DAV", "1");             // The WebDAV DAV header specifies whether the resource supports the WebDAV schema and protocol.
            responseMsg.Headers.Add("MS-Author-Via", "DAV"); //suggests to certain authoring applications the protocol mechanism to author with
            responseMsg.Headers.Add("Allow", "DELETE, GET, HEAD, LOCK, UNLOCK, PUT, PROPFIND");
            response.Return(responseMsg);
            yield break;
        }

        [DreamFeature("UNLOCK:pages/{pageid}/files/{filename}", "Unlock file (always returns 204)")]
        [DreamFeature("UNLOCK:files/{fileid}", "Unlock file (always returns 204)")]
        [DreamFeature("UNLOCK:files/{fileid}/{filename}", "Unlock file (always returns 204)")]
        public Yield UnLockFile(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            response.Return(new DreamMessage(DreamStatus.NoContent, null));
            yield break;
        }

        [DreamFeature("LOCK:pages/{pageid}/files/{filename}", "Lock file (always returns 200, mirroring the request document)")]
        [DreamFeature("LOCK:files/{fileid}", "Lock file (always returns 200, mirroring the request document)")]
        [DreamFeature("LOCK:files/{fileid}/{filename}", "Lock file (always returns 200, mirroring the request document)")]
        public Yield LockFile(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            if(!string.IsNullOrEmpty(request.Headers["If"])) {
                response.Return(new DreamMessage(DreamStatus.NoContent, null));
                yield break;
            }
            var lockinfo = request.ToDocument().UsePrefix("d", "DAV:");
            var prop = new XDoc("d", "prop", "DAV:")
                .Start("d:lockdiscovery")
                    .Start("d:activelock")
                        .AddAll(lockinfo["*"])
                        .Elem("d:depth", "infinity")
                        .Elem("d:timeout", "Second-3600")
                        .Start("d:locktoken").Elem("d:href", "opaquelocktoken:" + Guid.NewGuid())
                    .End()
                .End();
            response.Return(new DreamMessage(DreamStatus.Ok, null, prop));
            yield break;
        }

        [DreamFeature("PROPFIND:pages/{pageid}/files/{filename}", "Lock file (always returns 412 - Precondition Failed)")]
        [DreamFeature("PROPFIND:files/{fileid}", "Lock file (always returns 412 - Precondition Failed)")]
        [DreamFeature("PROPFIND:files/{fileid}/{filename}", "Lock file (always returns 412 - Precondition Failed)")]
        public Yield PropfindFile(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            var multistatus = new XDoc("d", "multistatus", "DAV:")
                .Start("d:response")
                    .Elem("d:href", context.Uri.AsPublicUri())
                    .Start("d:propstat")
                        .Start("d:prop")
                            .Elem("d:resourcetype")
                            .Elem("d:iscollection", "0")
                        .End()
                        .Elem("d:status", "HTTP/1.1 200 OK")
                    .End()
                .End();
            response.Return(new DreamMessage(DreamStatus.MultiStatus, null, multistatus));
            yield break;
        }

        [DreamFeature("PROPFIND:files", "Lock file (always returns 412 - Precondition Failed)")]
        public Yield PropfindDirectory(DreamContext context, DreamMessage request, Result<DreamMessage> response) {

            // Note (arnec): Win7 WebDAV client probes up the path until it finds a parent that reports back as being a collection,
            // so /files reports being a collection
            var multistatus = new XDoc("d", "multistatus", "DAV:")
                .Start("d:response")
                    .Elem("d:href", context.Uri.AsPublicUri())
                    .Start("d:propstat")
                        .Start("d:prop")
                            .Start("d:resourcetype").Elem("d:collection").End()
                            .Elem("d:displayname", "files")
                            .Elem("d:iscollection", "1")
                        .End()
                        .Elem("d:status", "HTTP/1.1 200 OK")
                    .End()
                .End();
            response.Return(new DreamMessage(DreamStatus.MultiStatus, null, multistatus));
            yield break;
        }

        [DreamFeature("GET:pages/{pageid}/files/{filename}", "Retrieve file attachment content")]
        [DreamFeature("GET:files/{fileid}", "Retrieve file attachment content")]
        [DreamFeature("GET:files/{fileid}/{filename}", "Retrieve file attachment content")]
        [DreamFeature("HEAD:pages/{pageid}/files/{filename}", "Retrieve file attachment content (Note: image manipulation arguments are ignored for HEAD requests)")]
        [DreamFeature("HEAD:files/{fileid}", "Retrieve file attachment content (Note: image manipulation arguments are ignored for HEAD requests)")]
        [DreamFeature("HEAD:files/{fileid}/{filename}", "Retrieve file attachment content (Note: image manipulation arguments are ignored for HEAD requests)")]
        [DreamFeatureParam("{pageid}", "string", "either an integer page ID, \"home\", or \"=\" followed by a double uri-encoded page title")]
        [DreamFeatureParam("{filename}", "string", "\"=\" followed by a double uri-encoded file name")]
        [DreamFeatureParam("{fileid}", "int", "identifies a file by ID")]
        [DreamFeatureParam("height", "int?", "Height of the image")]
        [DreamFeatureParam("width", "int?", "Width of the image")]
        [DreamFeatureParam("ratio", "{fixed, var}?", "Fixed preserves aspect ratio by applying height and width as bounding maximums rather than absolute values. Variable will use the width and height given. Default: fixed")]
        [DreamFeatureParam("format", "{jpg, png, bmp, gif}?", "Convert output to given type. Default is to use original type.")]
        [DreamFeatureParam("size", "{original, thumb, webview, bestfit, custom}?", "Return a resized image from one of the preset cached sizes. Use 'thumb' or 'webview' to return a smaller scaled image. Use 'bestfit' along with height/width to return one of the known sizes being at least the size given. Default: original")]
        [DreamFeatureParam("revision", "string?", "File revision to retrieve. 'head' by default will retrieve latest revision. positive integer will retrieve specific revision")]
        [DreamFeatureParam("redirects", "int?", "If zero, do not follow page redirects (only applies when {pageid} is present).")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        [DreamFeatureStatus(DreamStatus.Ok, "The request completed successfully")]
        [DreamFeatureStatus(DreamStatus.BadRequest, "Invalid input parameter or request body")]
        [DreamFeatureStatus(DreamStatus.Forbidden, "Read access to the page is required")]
        [DreamFeatureStatus(DreamStatus.NotFound, "Requested file could not be found")]
        [DreamFeatureStatus(DreamStatus.NotImplemented, "Requested operation is not currently supported")]
        public Yield GetFile(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            PageBE parentPage = null;
            DreamMessage responseMsg = null;
            ResourceBE fileRevision = GetAttachment(context, request, Permissions.READ, true, false, out parentPage);

            if(fileRevision.IsHidden) {
                PermissionsBL.CheckUserAllowed(DekiContext.Current.User, Permissions.ADMIN);
            }

            // check if only file information is requested
            if(context.Verb == Verb.HEAD) {
                response.Return(new DreamMessage(DreamStatus.Ok, null, fileRevision.MimeType, (long)fileRevision.Size, Stream.Null));
                yield break;
            }
            try {
                if(request.CheckCacheRevalidation(fileRevision.Timestamp)) {
                    responseMsg = DreamMessage.NotModified();
                }
                if(responseMsg == null) {

                    #region Preview related parameter parsing
                    string sFormat = context.GetParam("format", string.Empty);
                    string sRatio = context.GetParam("ratio", string.Empty);
                    uint height = context.GetParam<uint>("height", 0);
                    uint width = context.GetParam<uint>("width", 0);
                    string cachedSize = context.GetParam("size", string.Empty);

                    // check 'ratio' parameter
                    RatioType ratio = RatioType.UNDEFINED;
                    if(!string.IsNullOrEmpty(sRatio)) {
                        switch(sRatio.ToLowerInvariant().Trim()) {
                        case "var":
                        case "variable":
                            ratio = RatioType.VARIABLE;
                            break;
                        case "fixed":
                            ratio = RatioType.FIXED;
                            break;
                        default:
                            throw new AttachmentFileRatioInvalidArgumentException();
                        }
                    }

                    // check 'size' parameter
                    SizeType size = SizeType.UNDEFINED;
                    if(!string.IsNullOrEmpty(cachedSize) && !SysUtil.TryParseEnum(cachedSize.Trim(), out size)) {
                        throw new AttachmentFilesizeInvalidArgumentException();
                    }

                    // check 'format' parameter
                    FormatType format = FormatType.UNDEFINED;
                    if(!string.IsNullOrEmpty(sFormat) && !SysUtil.TryParseEnum(sFormat.Trim(), out format)) {
                        throw new AttachmentFileFormatInvalidArgumentException();
                    }
                    #endregion

                    //if any preview related parameters are set, do preview logic. Otherwise return the file
                    StreamInfo file = null;
                    if((size != SizeType.UNDEFINED && size != SizeType.ORIGINAL) ||
                        ratio != RatioType.UNDEFINED ||
                        format != FormatType.UNDEFINED ||
                        height != 0 ||
                        width != 0
                    ) {
                        file = AttachmentPreviewBL.RetrievePreview(fileRevision, height, width, ratio, size, format);
                    } else {
                        var isMSWebDAV = MSWEBDAV_USER_AGENT_REGEX.IsMatch(request.Headers.UserAgent ?? string.Empty);
                        file = DekiContext.Current.Instance.Storage.GetFile(fileRevision, SizeType.ORIGINAL, !isMSWebDAV);
                    }

                    // prepare response
                    if(file == null) {
                        throw new AttachmentDoesNotExistFatalException(fileRevision.ResourceId, fileRevision.Revision);
                    }

                    if(file.Uri != null) {
                        responseMsg = DreamMessage.Redirect(file.Uri);
                    } else {
                        bool inline = fileRevision.MetaXml.ImageHeight.HasValue;

                        // see if we can use the MimeType map for allowing inlining
                        if(!inline) {

                            // if IE inline security is not disabled
                            bool isIE = false;
                            if(!DekiContext.Current.Instance.EnableUnsafeIEContentInlining) {

                                // check the user agent to see if we're dealing with IE
                                isIE = MSIE_USER_AGENT_REGEX.IsMatch(request.Headers.UserAgent ?? string.Empty);
                            }

                            // see if the mime-type could allow inlining
                            inline = DekiContext.Current.Instance.MimeTypeCanBeInlined(fileRevision.MimeType);
                            if(inline && isIE) {

                                // check whether the creator of the file had unsafecontent permission, to override IE security
                                IList<ResourceBE> revisions = ResourceBL.Instance.GetResourceRevisions(fileRevision.ResourceId, ResourceBE.ChangeOperations.CONTENT, SortDirection.DESC, 1);
                                UserBE lastContentEditor = UserBL.GetUserById(revisions[0].UserId);
                                inline = PermissionsBL.IsUserAllowed(lastContentEditor, parentPage, Permissions.UNSAFECONTENT);
                            }
                        }
                        responseMsg = DreamMessage.Ok(fileRevision.MimeType, file.Length, file.Stream);
                        responseMsg.Headers["X-Content-Type-Options"] = "nosniff";
                        responseMsg.Headers.ContentDisposition = new ContentDisposition(inline, fileRevision.Timestamp, null, null, fileRevision.Name, file.Length, request.Headers.UserAgent);

                        // MSIE6 will delete a downloaded file before the helper app trying to use it can get to it so we
                        //have to do custom cache control headers for MSIE6 so that the file can actually be opened
                        if(MSIE6_USER_AGENT_REGEX.IsMatch(request.Headers.UserAgent ?? string.Empty)) {
                            responseMsg.Headers["Expires"] = "0";
                            responseMsg.Headers.Pragma = "cache";
                            responseMsg.Headers.CacheControl = "private";
                        } else {
                            responseMsg.SetCacheMustRevalidate(fileRevision.Timestamp);
                        }
                    }
                }
            } catch {
                if(responseMsg != null) {
                    responseMsg.Close();
                }
                throw;
            }
            response.Return(responseMsg);
            yield break;
        }

        [DreamFeature("GET:pages/{pageid}/files/{filename}/revisions", "Retrieve file revision info")]
        [DreamFeature("GET:files/{fileid}/revisions", "Retrieve file revision info")]
        [DreamFeatureParam("{pageid}", "string", "either an integer page ID, \"home\", or \"=\" followed by a double uri-encoded page title")]
        [DreamFeatureParam("{filename}", "string", "\"=\" followed by a double uri-encoded file name")]
        [DreamFeatureParam("{fileid}", "int", "identifies a file by ID")]
        [DreamFeatureParam("changefilter", "string?", "Only show revisions having a user-action listed in this comma delimited list. Valid actions are: CONTENT, NAME, LANGUAGE, META, DELETEFLAG, PARENT (default: all actions)")]
        [DreamFeatureParam("redirects", "int?", "If zero, do not follow page redirects (only applies when {pageid} is present).")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        [DreamFeatureStatus(DreamStatus.Ok, "The request completed successfully")]
        [DreamFeatureStatus(DreamStatus.BadRequest, "Invalid input parameter or request body")]
        [DreamFeatureStatus(DreamStatus.Forbidden, "Read access to the page is required")]
        [DreamFeatureStatus(DreamStatus.NotFound, "Requested file could not be found")]
        public Yield GetFileRevisions(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            CheckResponseCache(context, false);

            //Default change filter is CONTENT changes to preserve backwards compat
            string changeFilterStr = context.GetParam("changefilter", AttachmentBL.DEFAULT_REVISION_FILTER.ToString());

            ResourceBE.ChangeOperations changeFilter = ResourceBE.ChangeOperations.UNDEFINED;
            if(!string.IsNullOrEmpty(changeFilterStr)) {
                if(StringUtil.EqualsInvariantIgnoreCase("all", changeFilterStr)) {
                    changeFilter = ResourceBE.ChangeOperations.UNDEFINED;
                } else if(!SysUtil.TryParseEnum(changeFilterStr, out changeFilter)) {
                    throw new DreamBadRequestException("changefilter value is invalid. Possible values are ALL, " + string.Join(",", Enum.GetNames(typeof(ResourceBE.ChangeOperations))));
                }
            }

            PageBE parentPage = null;
            ResourceBE fileRevision = GetAttachment(context, request, Permissions.READ, false, false, out parentPage);
            XUri listUri = AttachmentBL.Instance.GetUri(fileRevision).At("revisions").With("changefilter", changeFilterStr.ToLowerInvariant());
            XDoc ret = AttachmentBL.Instance.GetFileRevisionsXml(fileRevision, changeFilter, listUri, fileRevision.Revision);
            response.Return(DreamMessage.Ok(ret));
            yield break;
        }

        [DreamFeature("POST:pages/{pageid}/files/{filename}/revisions", "Performs operations such as hide/unhide for revisions of files")]
        [DreamFeature("POST:files/{fileid}/revisions", "Performs operations such as hide/unhide for revisions of files")]
        [DreamFeatureParam("{pageid}", "string", "either an integer page ID, \"home\", or \"=\" followed by a double uri-encoded page title")]
        [DreamFeatureParam("{filename}", "string", "\"=\" followed by a double uri-encoded file name")]
        [DreamFeatureParam("{fileid}", "int", "identifies a file by ID")]
        [DreamFeatureParam("comment", "string?", "Reason for hiding revisions")]
        [DreamFeatureParam("redirects", "int?", "If zero, do not follow page redirects (only applies when {pageid} is present).")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        [DreamFeatureStatus(DreamStatus.Ok, "The request completed successfully")]
        [DreamFeatureStatus(DreamStatus.BadRequest, "Invalid input parameter or request body")]
        [DreamFeatureStatus(DreamStatus.Forbidden, "DELETE access is required to hide a revision and ADMIN access to unhide")]
        [DreamFeatureStatus(DreamStatus.NotFound, "Requested file could not be found")]
        public Yield PostFileRevisions(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            ResourceBE file = GetAttachment(context, request, Permissions.DELETE);
            ResourceBE[] modifiedRevs = AttachmentBL.Instance.ModifyRevisionVisibility(file, request.ToDocument(), context.GetParam("comment", string.Empty));
            XDoc ret = AttachmentBL.Instance.GetAttachmentRevisionListXml(modifiedRevs);
            response.Return(DreamMessage.Ok(ret));
            yield break;
        }


        [DreamFeature("PUT:pages/{pageid}/files/{filename}", "Replace an existing attachment with a new version or create a new attachment")]
        [DreamFeature("PUT:files/{fileid}", "Replace an existing attachment with a new version")]
        [DreamFeature("PUT:files/{fileid}/{filename}", "Replace an existing attachment with a new version")]
        [DreamFeatureParam("{pageid}", "string", "either an integer page ID, \"home\", or \"=\" followed by a double uri-encoded page title")]
        [DreamFeatureParam("{filename}", "string", "\"=\" followed by a double uri-encoded file name")]
        [DreamFeatureParam("description", "string?", "file attachment description")]
        [DreamFeatureParam("redirects", "int?", "If zero, do not follow page redirects.")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        [DreamFeatureStatus(DreamStatus.Ok, "The request completed successfully")]
        [DreamFeatureStatus(DreamStatus.BadRequest, "Invalid input parameter or request body")]
        [DreamFeatureStatus(DreamStatus.Forbidden, "Update access to the page is required")]
        [DreamFeatureStatus(DreamStatus.NotFound, "Requested page could not be found")]
        public Yield PutFile(DreamContext context, DreamMessage request, Result<DreamMessage> response) {

            // Retrieve the file
            PageBE page;
            string userFileName;
            ResourceBE file = GetAttachmentFromUrl(context, false, out page, false, false);

            // If the file does not exist, attempt to retrieve the page
            if(null == file) {
                if(null == page) {
                    if(null != DreamContext.Current.GetParam<string>("fileid")) {
                        throw new AttachmentNotFoundException();
                    }
                    page = PageBL_GetPageFromUrl(context, true);
                }
                userFileName = GetFilenameFromPathSegment(DreamContext.Current.GetParam<string>("filename"));
            } else {
                string fileNameParam = DreamContext.Current.GetParam("filename", null);
                if(fileNameParam == null) {
                    userFileName = file.Name;
                } else {
                    userFileName = GetFilenameFromPathSegment(fileNameParam);
                }
            }

            // Retrieve the file description
            string userDescription = context.GetParam("description", string.Empty);
            if(userDescription.Length > AttachmentBL.MAX_DESCRIPTION_LENGTH) {
                userDescription = userDescription.Substring(0, AttachmentBL.MAX_DESCRIPTION_LENGTH);
            }

            // Validate the page
            PageBL.AuthorizePage(DekiContext.Current.User, Permissions.UPDATE, page, false);

            // Get entire stream so it can be reused
            var isMSWebDAV = MSWEBDAV_USER_AGENT_REGEX.IsMatch(request.Headers.UserAgent ?? string.Empty);
            ResourceBE savedFileRevision = AttachmentBL.Instance.AddAttachment(file, request.AsStream(), request.ContentLength, request.ContentType, page, userDescription, userFileName, isMSWebDAV);

            // report an error on failure, and don't redirect
            if(savedFileRevision == null) {
                throw new AttachmentUploadSaveFatalException();
            }
            response.Return(DreamMessage.Ok(AttachmentBL.Instance.GetFileXml(savedFileRevision, true, null, null)));
            yield break;
        }

        [DreamFeature("POST:files/{fileid}/move", "Move an attachment from one page to another and/or change the filename")]
        [DreamFeatureParam("{fileid}", "int", "identifies a file by ID")]
        [DreamFeatureParam("to", "string?", "page id of target page")]
        [DreamFeatureParam("name", "string?", "new filename")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        [DreamFeatureStatus(DreamStatus.Ok, "The request completed successfully")]
        [DreamFeatureStatus(DreamStatus.BadRequest, "Invalid input parameter or request body")]
        [DreamFeatureStatus(DreamStatus.Forbidden, "Update access to the page is required")]
        [DreamFeatureStatus(DreamStatus.NotFound, "Requested file could not be found")]
        public Yield PostFileMove(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            PageBE sourcePage = null;
            PageBE destPage = null;
            ResourceBE fileToMove = GetAttachmentFromUrl(context, true, out sourcePage, false, false);

            // parameter parsing
            string name = context.GetParam("name", null);
            string to = context.GetParam("to", null);

            if(string.IsNullOrEmpty(name) && string.IsNullOrEmpty(to)) {
                throw new AttachmentMoveInvalidArgumentException();
            }
            if(name == null) {
                name = fileToMove.Name;
            }
            destPage = to != null ? PageBL_GetPageFromPathSegment(true, to) : sourcePage;

            //Check if we're actually doing anything            
            if(sourcePage.ID == destPage.ID && fileToMove.Name.EqualsInvariant(name)) {
                throw new AttachmentNotChangedInvalidOperationException(fileToMove.Name, destPage.Title.AsUserFriendlyName());
            }

            //Ensure write access to source and destination pages.
            IList<PageBE> pList = PermissionsBL.FilterDisallowed(DekiContext.Current.User, new PageBE[] { sourcePage, destPage }, true, Permissions.UPDATE);

            // perform the move
            ResourceBE ret = AttachmentBL.Instance.MoveAttachment(fileToMove, sourcePage, destPage, name, true);

            response.Return(DreamMessage.Ok(AttachmentBL.Instance.GetFileXml(ret, true, null, false)));
            yield break;
        }

        [DreamFeature("DELETE:pages/{pageid}/files/{filename}", "Delete file attachment")]
        [DreamFeature("DELETE:files/{fileid}", "Delete file attachment")]
        [DreamFeature("DELETE:files/{fileid}/{filename}", "Delete file attachment")]
        [DreamFeatureParam("{pageid}", "string", "either an integer page ID, \"home\", or \"=\" followed by a double uri-encoded page title")]
        [DreamFeatureParam("{filename}", "string", "\"=\" followed by a double uri-encoded file name")]
        [DreamFeatureParam("{fileid}", "int", "identifies a file by ID")]
        [DreamFeatureParam("redirects", "int?", "If zero, do not follow page redirects (only applies when {pageid} is present).")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        [DreamFeatureStatus(DreamStatus.Ok, "The request completed successfully")]
        [DreamFeatureStatus(DreamStatus.BadRequest, "Invalid input parameter or request body")]
        [DreamFeatureStatus(DreamStatus.Forbidden, "Update access to the page is required")]
        [DreamFeatureStatus(DreamStatus.NotFound, "The requested file could not be found")]
        public Yield DeleteFile(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            ResourceBE fileRevision = GetAttachment(context, request, Permissions.UPDATE);

            // check if anything needs to be done
            if(!fileRevision.ResourceIsDeleted) {
                AttachmentBL.Instance.RemoveAttachments(new[] { fileRevision });
                response.Return(DreamMessage.Ok());
            } else {
                throw new AttachmentAlreadyMovedNotFoundException();
            }
            yield break;
        }

        [DreamFeature("GET:pages/{pageid}/files/{filename}/description", "retrieves a file description")]
        [DreamFeature("GET:files/{fileid}/description", "retrieves a file description")]
        [DreamFeatureParam("{pageid}", "string", "either an integer page ID, \"home\", or \"=\" followed by a double uri-encoded page title")]
        [DreamFeatureParam("{filename}", "string", "\"=\" followed by a double uri-encoded file name")]
        [DreamFeatureParam("{fileid}", "int", "identifies a file by ID")]
        [DreamFeatureParam("revision", "string?", "File revision to retrieve. 'head' by default will retrieve latest revision. positive integer will retrieve specific revision")]
        [DreamFeatureParam("redirects", "int?", "If zero, do not follow page redirects (only applies when {pageid} is present).")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        [DreamFeatureStatus(DreamStatus.Ok, "The request completed successfully")]
        [DreamFeatureStatus(DreamStatus.BadRequest, "Invalid input parameter or request body")]
        [DreamFeatureStatus(DreamStatus.Forbidden, "Read access to the page is required")]
        [DreamFeatureStatus(DreamStatus.NotFound, "Requested file could not be found")]
        public Yield GetFileDescription(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            PageBE parentPage = null;
            ResourceBE file = GetAttachment(context, request, Permissions.READ, true, false, out parentPage);
            ResourceBE descriptionProperty = PropertyBL.Instance.GetAttachmentDescription(file.ResourceId);
            if(descriptionProperty == null) {
                // reply with no data
                response.Return(DreamMessage.Ok());
            } else {

                // reply with description
                var content = ResourceContentBL.Instance.Get(descriptionProperty);
                response.Return(DreamMessage.Ok(content.MimeType, content.ToText()));
            }
            yield break;
        }

        [DreamFeature("PUT:pages/{pageid}/files/{filename}/description", "Update attachment description")]
        [DreamFeature("PUT:files/{fileid}/description", "Update attachment description")]
        [DreamFeature("DELETE:files/{fileid}/description", "Reset the file description")]
        [DreamFeature("DELETE:pages/{pageid}/files/{filename}/description", "Reset the file description")]
        [DreamFeatureParam("{pageid}", "string", "either an integer page ID, \"home\", or \"=\" followed by a double uri-encoded page title")]
        [DreamFeatureParam("{filename}", "string", "\"=\" followed by a double uri-encoded file name")]
        [DreamFeatureParam("{fileid}", "int", "identifies a file by ID")]
        [DreamFeatureParam("redirects", "int?", "If zero, do not follow page redirects (only applies when {pageid} is present).")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        [DreamFeatureStatus(DreamStatus.Ok, "Request completed successfully")]
        [DreamFeatureStatus(DreamStatus.BadRequest, "Invalid input parameter or request body")]
        [DreamFeatureStatus(DreamStatus.Forbidden, "Update access to the page is required")]
        [DreamFeatureStatus(DreamStatus.NotFound, "Requested file could not be found")]
        public Yield PutFileDescription(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            PageBE parentPage;
            ResourceBE file = GetAttachment(context, request, Permissions.UPDATE, false, false, out parentPage);

            // determine if description needs to be set or cleared
            string description = StringUtil.EqualsInvariant(context.Verb, "PUT") ? request.AsText() : string.Empty;
            file = AttachmentBL.Instance.SetDescription(file, description);
            response.Return(DreamMessage.Ok(AttachmentBL.Instance.GetFileXml(file, true, null, null)));
            yield break;
        }

        [DreamFeature("POST:files/{fileid}/index", "re-index an attachment")]
        [DreamFeatureParam("{fileid}", "int", "identifies a file by ID")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        [DreamFeatureStatus(DreamStatus.Ok, "Request completed successfully")]
        [DreamFeatureStatus(DreamStatus.Forbidden, "MindTouch API key or Administrator access is required.")]
        internal Yield IndexFile(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            ResourceBE file = GetAttachment(context, request, Permissions.NONE);
            DekiContext.Current.Instance.EventSink.AttachmentPoke(DekiContext.Current.Now, file);
            response.Return(DreamMessage.Ok());
            yield break;
        }

        //--- Methods ---
        private ResourceBE GetAttachment(DreamContext context, DreamMessage request, Permissions access) {
            PageBE p;
            return GetAttachment(context, request, access, false, false, out p);
        }

        private ResourceBE GetAttachment(DreamContext context, DreamMessage request, Permissions access, bool allowRevs, bool allowDeleted, out PageBE parentPage) {
            ResourceBE file = GetAttachmentFromUrl(context, true, out parentPage, allowRevs, allowDeleted);
            PageBL.AuthorizePage(DekiContext.Current.User, access, parentPage, false);
            DreamContext.Current.SetState<UserBE>(DekiContext.Current.User);
            DreamContext.Current.SetState<PageBE>(parentPage);

            //Identify images for upgrades
            List<ResourceBE> fileList = new List<ResourceBE>();
            fileList.Add(file);
            AttachmentBL.Instance.IdentifyUnknownImages(fileList);

            return file;
        }

        private ResourceBE GetAttachmentFromUrl(DreamContext context, bool mustExist, out PageBE page, bool allowRevs, bool allowDeleted) {
            ResourceBE file = null;
            int revision = ResourceBE.HEADREVISION;
            page = null;
            string revStr = DreamContext.Current.GetParam("revision", "head").Trim();
            if(allowRevs) {
                if(!StringUtil.EqualsInvariantIgnoreCase(revStr, "head") && !StringUtil.EqualsInvariantIgnoreCase(revStr, "0"))
                    if(!int.TryParse(revStr, out revision)) {
                        throw new RevisionHeadOrIntInvalidArgumentException();
                    }
            } else if(!StringUtil.EqualsInvariantIgnoreCase(revStr, "head") && !StringUtil.EqualsInvariantIgnoreCase(revStr, "0")) {
                throw new AttachmentUnsupportedRevisionInvalidArgumentException();
            }

            uint fileId = DreamContext.Current.GetParam<uint>("fileid", 0);
            if(fileId != 0) {

                uint resourceId = ResourceMapBL.GetResourceIdByFileId(fileId) ?? 0;
                if(resourceId > 0) {

                    // use resourceid to retrieve attachment
                    file = ResourceBL.Instance.GetResourceRevision(resourceId, revision);
                }
                if(file != null) {
                    page = PageBL.GetPageById(file.ParentPageId.Value);
                }
            } else {
                // use filename to retrieve attachment
                string fileName = GetFilenameFromPathSegment(DreamContext.Current.GetParam<string>("filename"));
                page = PageBL_GetPageFromUrl(context, true);
                DeletionFilter deleteFilter = allowDeleted ? DeletionFilter.ANY : DeletionFilter.ACTIVEONLY;
                file = AttachmentBL.Instance.GetPageAttachment(page.ID, fileName, deleteFilter);
                if(revision != ResourceBE.HEADREVISION) {
                    file = ResourceBL.Instance.GetResourceRevision(file.ResourceId, revision);
                }
            }
            if(file == null) {
                if(mustExist) {
                    throw new AttachmentNotFoundException();
                }
            } else {

                if(!allowDeleted) {
                    if(file.ResourceIsDeleted || page == null) {

                        // TODO (maxm): Throw a 404 status if file is not marked as deleted but the parent page cannot be found.
                        // This may be caused by an unfinished page delete operation that didn't mark the file as deleted.
                        throw new AttachmentRemovedNotFoundException();
                    }
                }
            }

            return file;
        }

        public static string GetFilenameFromPathSegment(string filePathSegment) {
            string filename = null;
            if(filePathSegment.StartsWith("=")) {
                filename = XUri.Decode(filePathSegment.Substring(1));
            }
            if(string.IsNullOrEmpty(filename)) {
                throw new AttachmentMissingFilenameInvalidArgumentException();
            }
            return filename;
        }
    }
}

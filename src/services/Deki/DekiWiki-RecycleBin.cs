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

using MindTouch.Deki.Data;
using MindTouch.Deki.Exceptions;
using MindTouch.Deki.Logic;
using MindTouch.Dream;
using MindTouch.Dream.Http;
using MindTouch.Tasking;
using MindTouch.Xml;

namespace MindTouch.Deki {
    using Yield = IEnumerator<IYield>;

    public partial class DekiWikiService {

        //--- Features ---

        [DreamFeature("GET:archive", "Retrieves a summary of available archive information")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        [DreamFeatureStatus(DreamStatus.Ok, "The request completed successfully")]
        [DreamFeatureStatus(DreamStatus.Forbidden, "Administrator access is required")]
        public Yield GetArchive(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            PermissionsBL.CheckUserAllowed(DekiContext.Current.User, Permissions.ADMIN);
            XDoc ret = new XDoc("archive");
            ret.Start("pages.archive").Attr("href", DekiContext.Current.ApiUri.At("archive", "pages")).End();
            ret.Start("files.archive").Attr("href", DekiContext.Current.ApiUri.At("archive", "files")).End();
            response.Return(DreamMessage.Ok(ret));
            yield break;
        }


        [DreamFeature("GET:archive/files", "Retrieves file info for all deleted files")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        [DreamFeatureStatus(DreamStatus.Ok, "The request completed successfully")]
        [DreamFeatureStatus(DreamStatus.Forbidden, "Administrator access is required")]
        public Yield GetArchiveFiles(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            PermissionsBL.CheckUserAllowed(DekiContext.Current.User, Permissions.ADMIN);
            IList<ResourceBE> removedFiles = AttachmentBL.Instance.GetDeletedAttachments(null, null);
            XDoc responseXml = AttachmentBL.Instance.GetFileXml(removedFiles, true, "archive", null, null);
            response.Return(DreamMessage.Ok(responseXml));
            yield break;
        }

        [DreamFeature("GET:archive/files/{fileid}", "Retrieves file content for a deleted file")]
        [DreamFeature("GET:archive/files/{fileid}/{filename}", "Retrieve file attachment content")]
        [DreamFeatureParam("{fileid}", "int", "identifies a file by ID")]
        [DreamFeatureParam("{filename}", "string", "\"=\" followed by a double uri-encoded file name")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        [DreamFeatureStatus(DreamStatus.Ok, "The request completed successfully")]
        [DreamFeatureStatus(DreamStatus.BadRequest, "Invalid input parameter or request body")]
        [DreamFeatureStatus(DreamStatus.Forbidden, "Administrator access is required")]
        [DreamFeatureStatus(DreamStatus.NotFound, "Requested file could not be found in the archive")]
        public Yield GetArchiveFile(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            PermissionsBL.CheckUserAllowed(DekiContext.Current.User, Permissions.ADMIN);
            PageBE parentPage = null;
            ResourceBE removedFile = GetAttachment(context, request, Permissions.NONE, true, true, out parentPage);
            if(!removedFile.ResourceIsDeleted) {
                throw new AttachmentArchiveFileNotDeletedNotFoundException();
            }
            StreamInfo file = DekiContext.Current.Instance.Storage.GetFile(removedFile, SizeType.ORIGINAL, false);
            if(file == null) {
                throw new AttachmentDoesNotExistFatalException(removedFile.ResourceId, removedFile.Revision);
            }
            var responseMsg = DreamMessage.Ok(file.Type, file.Length, file.Stream);
            responseMsg.Headers.ContentDisposition = new ContentDisposition(true, removedFile.Timestamp, null, null, removedFile.Name, file.Length);

            response.Return(responseMsg);
            yield break;
        }

        [DreamFeature("GET:archive/files/{fileid}/info", "Retrieves file info for a deleted file")]
        [DreamFeatureParam("{fileid}", "int", "identifies a file by ID")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        [DreamFeatureStatus(DreamStatus.Ok, "The request completed successfully")]
        [DreamFeatureStatus(DreamStatus.BadRequest, "Invalid input parameter or request body")]
        [DreamFeatureStatus(DreamStatus.Forbidden, "Administrator access is required")]
        [DreamFeatureStatus(DreamStatus.NotFound, "Requested file could not be found in the archive")]
        public Yield GetArchiveFileInfo(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            PermissionsBL.CheckUserAllowed(DekiContext.Current.User, Permissions.ADMIN);
            PageBE parentPage = null;
            ResourceBE removedFile = GetAttachment(context, request, Permissions.NONE, true, true, out parentPage);
            if(!removedFile.ResourceIsDeleted) {
                throw new AttachmentArchiveFileNotDeletedNotFoundException();
            }
            response.Return(DreamMessage.Ok(AttachmentBL.Instance.GetFileXml(removedFile, true, "archive", null)));
            yield break;
        }

        [DreamFeature("DELETE:archive/files/{fileid}", "Remove a file from the archive (wipe)")]
        [DreamFeatureParam("{fileid}", "int", "identifies a file by ID")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        [DreamFeatureStatus(DreamStatus.Ok, "Request completed successfully")]
        [DreamFeatureStatus(DreamStatus.BadRequest, "Invalid input parameter or request body")]
        [DreamFeatureStatus(DreamStatus.Forbidden, "Administrator access is required")]
        [DreamFeatureStatus(DreamStatus.NotFound, "Requested file could not be found in the archive")]
        public Yield DeleteArchiveFile(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            PermissionsBL.CheckUserAllowed(DekiContext.Current.User, Permissions.ADMIN);
            PageBE parentPage = null;
            ResourceBE removedFile = GetAttachment(context, request, Permissions.NONE, true, true, out parentPage);
            if(!removedFile.ResourceIsDeleted) {
                throw new AttachmentArchiveFileNotDeletedNotFoundException();
            }
            AttachmentBL.Instance.WipeAttachments(new[] { removedFile });
            response.Return(DreamMessage.Ok());
            yield break;
        }

        [DreamFeature("DELETE:archive/files", "Removes all files from the archive (wipe)")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        [DreamFeatureStatus(DreamStatus.Ok, "The request completed successfully")]
        [DreamFeatureStatus(DreamStatus.Forbidden, "Administrator access is required")]
        public Yield DeleteArchiveFiles(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            PermissionsBL.CheckUserAllowed(DekiContext.Current.User, Permissions.ADMIN);
            IList<ResourceBE> removedFiles = AttachmentBL.Instance.GetDeletedAttachments(null, null);
            AttachmentBL.Instance.WipeAttachments(removedFiles);
            response.Return(DreamMessage.Ok());
            yield break;
        }

        [DreamFeature("POST:archive/files/restore/{fileid}", "Restores a deleted file back to its page")]
        [DreamFeatureParam("{fileid}", "int", "identifies a file by ID")]
        [DreamFeatureParam("to", "string?", "Optional restore-to page to override a removed file's original parent id")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        [DreamFeatureStatus(DreamStatus.Ok, "The request completed successfully")]
        [DreamFeatureStatus(DreamStatus.BadRequest, "Invalid input parameter or request body")]
        [DreamFeatureStatus(DreamStatus.Forbidden, "Administrator access is required")]
        [DreamFeatureStatus(DreamStatus.NotFound, "Requested file could not be found in the archive")]
        public Yield PostArchiveFilesRestore(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            PermissionsBL.CheckUserAllowed(DekiContext.Current.User, Permissions.ADMIN);

            // parameter parsing
            PageBE destPage = null;
            string to = context.GetParam("to", string.Empty);
            if(to != string.Empty) {
                destPage = PageBL_GetPageFromPathSegment(false, to);
            }
            PageBE parentPage;
            ResourceBE removedFile = GetAttachment(context, request, Permissions.NONE, true, true, out parentPage);
            if(!removedFile.ResourceIsDeleted) {
                throw new AttachmentArchiveFileNotDeletedNotFoundException();
            }

            //Optionally move the restored file to the given page
            if(null == destPage) {
                destPage = parentPage;
            }
            AttachmentBL.Instance.RestoreAttachment(removedFile, destPage, DateTime.UtcNow, 0);
            response.Return(DreamMessage.Ok());
            yield break;
        }

        [DreamFeature("GET:archive/pages", "Retrieve the pages that can be potentially restored from deletion.")]
        [DreamFeatureParam("title", "string?", "Show deleted pages matching the given title. (default: all pages)")]
        [DreamFeatureParam("limit", "string?", "Maximum number of items to retrieve. Must be a positive number or 'all' to retrieve all items. (default: 100)")]
        [DreamFeatureParam("offset", "int?", "Number of items to skip. Must be a positive number or 0 to not skip any. (default: 0)")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        [DreamFeatureStatus(DreamStatus.Ok, "The request completed successfully")]
        [DreamFeatureStatus(DreamStatus.Forbidden, "Administrator access is required")]
        public Yield GetArchivePages(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            PermissionsBL.CheckUserAllowed(DekiContext.Current.User, Permissions.ADMIN);
            uint limit, offset;
            SortDirection sortDir;
            string sortField;
            Utils.GetOffsetAndCountFromRequest(context, 100, out limit, out offset, out sortDir, out sortField);
            Title filterTitle = null;
            string titleStr = context.GetParam("title", null);
            if(!string.IsNullOrEmpty(titleStr)) {
                filterTitle = Title.FromUIUri(null, titleStr, false);
            }
            XDoc responseXml = PageArchiveBL.GetArchivedPagesXml(limit, offset, filterTitle);
            response.Return(DreamMessage.Ok(responseXml));
            yield break;
        }

        [DreamFeature("GET:archive/pages/{pageid}/info", "Retrieve basic page information for the deleted page")]
        [DreamFeature("GET:archive/pages/{pageid}", "Retrieve basic page information for the deleted page")]
        [DreamFeatureParam("{pageid}", "string", "An integer page ID of a deleted page")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        [DreamFeatureStatus(DreamStatus.Ok, "The request completed successfully")]
        [DreamFeatureStatus(DreamStatus.Forbidden, "Administrator access is required")]
        public Yield GetArchivePagesInfo(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            PermissionsBL.CheckUserAllowed(DekiContext.Current.User, Permissions.ADMIN);
            XDoc responseXml = PageArchiveBL.GetArchivePageXml(context.GetParam<uint>("pageid"));
            response.Return(DreamMessage.Ok(responseXml));
            yield break;
        }

        [DreamFeature("GET:archive/pages/{pageid}/subpages", "Retrieve the child pages that were deleted as well from deleting the given page")]
        [DreamFeatureParam("{pageid}", "string", "An integer page ID of a deleted page")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        [DreamFeatureStatus(DreamStatus.Ok, "The request completed successfully")]
        [DreamFeatureStatus(DreamStatus.Forbidden, "Administrator access is required")]
        public Yield GetArchivePageSubpages(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            PermissionsBL.CheckUserAllowed(DekiContext.Current.User, Permissions.ADMIN);
            XDoc responseXml = PageArchiveBL.GetArchivedSubPagesXml(context.GetParam<uint>("pageid"));
            response.Return(DreamMessage.Ok(responseXml));
            yield break;
        }

        [DreamFeature("GET:archive/pages/{pageid}/contents", "Retrieve the contents of a deleted page for previewing")]
        [DreamFeatureParam("{pageid}", "string", "An integer page ID of a deleted page")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        [DreamFeatureStatus(DreamStatus.Ok, "The request completed successfully")]
        [DreamFeatureStatus(DreamStatus.Forbidden, "Administrator access is required")]
        public Yield GetArchivePageContents(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            PermissionsBL.CheckUserAllowed(DekiContext.Current.User, Permissions.ADMIN);
            DreamMessage ret = PageArchiveBL.BuildDeletedPageContents(context.GetParam<uint>("pageid"));
            response.Return(ret);
            yield break;
        }


        [DreamFeature("POST:archive/pages/{pageid}/restore", "Restore all revisions of a given page")]
        [DreamFeatureParam("{pageid}", "int", "An integer page ID from GET: archive/pages")]
        [DreamFeatureParam("to", "string", "new page title")]
        [DreamFeatureParam("reason", "string?", "Reason for reverting")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        [DreamFeatureStatus(DreamStatus.Ok, "The request completed successfully")]
        [DreamFeatureStatus(DreamStatus.NotFound, "Unable to find the page to delete")]
        [DreamFeatureStatus(DreamStatus.Conflict, "A title with the same path already exists. To restoring to a different path with '?to='")]
        [DreamFeatureStatus(DreamStatus.Forbidden, "Administrator access is required")]
        public Yield PostArchivePagesPageIdRestore(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            PermissionsBL.CheckUserAllowed(DekiContext.Current.User, Permissions.ADMIN);
            uint pageid = context.GetParam<uint>("pageid");
            string targetPathStr = context.GetParam("to", string.Empty);
            string reason = context.GetParam("reason", string.Empty);
            Title targetPath = null;
            if(!string.IsNullOrEmpty(targetPathStr)) {
                targetPath = Title.FromUIUri(null, context.GetParam("to"), false);
            }
            XDoc responseXml = PageArchiveBL.RestoreDeletedPage(pageid, targetPath, reason);
            response.Return(DreamMessage.Ok(responseXml));
            yield break;
        }


    }
}

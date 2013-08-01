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
using MindTouch.Deki.Data;
using MindTouch.Deki.Exceptions;
using MindTouch.Dream;
using MindTouch.Xml;

namespace MindTouch.Deki.Logic {

    // Note (arnec): IAttachmentBL will eventually be the interface to a per request AttachmentBL without static dependencies. In the meantime it
    // mirrors only the methods required by those not using AttachmentBL directly.
    public interface IAttachmentBL {

        //--- Methods ---
        IEnumerable<ResourceBE> GetAllAttachementsChunked();
        XUri GetUri(ResourceBE file);
    }

    public class AttachmentBL : IAttachmentBL {

        //--- Constants ---
        public const ResourceBE.ChangeOperations DEFAULT_REVISION_FILTER = ResourceBE.ChangeOperations.CONTENT;
        public const int MAX_FILENAME_LENGTH = 255;
        public const int MAX_DESCRIPTION_LENGTH = 1024;

        //--- Class Fields ---
        private static readonly log4net.ILog _log = DekiLogManager.CreateLog();
        private static readonly Dictionary<char, char> _charReplaceMap = new Dictionary<char, char>();

        //--- Class Constructor ---
        static AttachmentBL() {
            foreach(char c in Path.GetInvalidFileNameChars()) {
                _charReplaceMap.Add(c, ' ');
            }
        }

        //--- Class Properties ---
        public static AttachmentBL Instance {
            get {
                return new AttachmentBL();
            }
        }

        //--- Fields ---
        private readonly DekiContext _dekiContext;
        private readonly IDekiDataSession _session;
        private readonly DekiResources _resources;
        private readonly ResourceBL _resourceBL;

        //--- Constructors ---
        protected AttachmentBL() {
            _dekiContext = DekiContext.Current;
            _session = DbUtils.CurrentSession;
            _resources = _dekiContext.Resources;
            _resourceBL = ResourceBL.Instance;
        }

        //--- Methods ---
        public bool IsAllowedForImageMagickPreview(ResourceBE file) {

            // TODO (steveb): recognize mime-types as well
            if(Array.IndexOf(_dekiContext.Instance.ImageMagickExtensions, file.FilenameExtension.ToLowerInvariant()) == -1) {
                return false;
            }
            return _dekiContext.Instance.MaxImageSize >= file.Size;
        }

        // methods for page resources
        public IList<ResourceBE> GetPageAttachments(ulong pageId) {
            return _resourceBL.GetResources(new[] { (uint)pageId }, ResourceBE.ParentType.PAGE, ResourceBL.FILES, null, DeletionFilter.ACTIVEONLY, null, null, null);
        }

        public ResourceBE GetPageAttachment(ulong pageId, string filename) {
            return _resourceBL.GetResource((uint)pageId, ResourceBE.ParentType.PAGE, ResourceBE.Type.FILE, filename, DeletionFilter.ACTIVEONLY);
        }

        public ResourceBE GetPageAttachment(ulong pageId, string filename, DeletionFilter deletionFilter) {
            return _resourceBL.GetResource((uint)pageId, ResourceBE.ParentType.PAGE, ResourceBE.Type.FILE, filename, deletionFilter);
        }

        public IList<ResourceBE> GetDeletedAttachments(uint? limit, uint? offset) {
            return _resourceBL.GetResources(null, ResourceBE.ParentType.PAGE, ResourceBL.FILES, null, DeletionFilter.DELETEDONLY, null, limit, offset);
        }

        public ResourceBE AddAttachment(ResourceBE existingRevision, Stream filestream, long filesize, MimeType mimeType, PageBE targetPage, string userDescription, string fileName, bool isMsWebDav) {
            if(_dekiContext.Instance.MaxFileSize < filesize) {
                throw new AttachmentMaxFileSizeAllowedInvalidArgumentException(_dekiContext.Instance.MaxFileSize);
            }
            var saveFileName = ValidateFileName(fileName);
            if(existingRevision != null) {
                if(!saveFileName.EqualsInvariant(existingRevision.Name)) {

                    // An existing file is getting renamed. Make sure no file exists with the new name
                    var existingAttachment = GetPageAttachment(targetPage.ID, saveFileName);
                    if(existingAttachment != null) {
                        throw new AttachmentExistsOnPageConflictException(saveFileName, targetPage.Title.AsUserFriendlyName());
                    }
                }
            }

            // If file is found but has been deleted, create a new file.
            if(existingRevision != null && existingRevision.ResourceIsDeleted) {
                existingRevision = null;
            }
            if(isMsWebDav) {
                _log.DebugFormat("Upload client is MD WebDAV, provided mimetype is: {0}", mimeType);
                var extensionFileType = MimeType.FromFileExtension(Path.GetExtension(saveFileName));
                if(!extensionFileType.Match(mimeType) || extensionFileType == MimeType.DefaultMimeType) {
                    mimeType = existingRevision == null ? extensionFileType : existingRevision.MimeType;
                    _log.DebugFormat("using mimetype '{0}' instead", mimeType);
                }
            }
            ResourceBE attachment;
            var resourceContents = new ResourceContentBE((uint)filesize, mimeType);
            var isUpdate = false;
            if(existingRevision == null) {
                attachment = _resourceBL.BuildRevForNewResource((uint)targetPage.ID, ResourceBE.ParentType.PAGE, saveFileName, mimeType, (uint)filesize, null, ResourceBE.Type.FILE, _dekiContext.User.ID, resourceContents);
            } else {
                isUpdate = true;
                attachment = BuildRevForContentUpdate(existingRevision, mimeType, (uint)filesize, null, saveFileName, resourceContents);
            }
            
            // rewrite mimetype to text/plain for certain extensions
            string extension = attachment.FilenameExtension;
            if(_dekiContext.Instance.FileExtensionForceAsTextList.Any(forcedExtensions => extension == forcedExtensions)) {
                attachment.MimeType = MimeType.TEXT;
            }

            // Insert the attachment into the DB
            attachment = SaveResource(attachment);
            try {

                // Save file to storage provider
                _dekiContext.Instance.Storage.PutFile(attachment, SizeType.ORIGINAL, new StreamInfo(filestream, filesize, mimeType));
            } catch(Exception x) {
                _dekiContext.Instance.Log.WarnExceptionFormat(x, "Failed to save attachment to storage provider");

                // Upon save failure, delete the record from the db.
                _session.Resources_DeleteRevision(attachment.ResourceId, attachment.Revision);
                throw;
            }

            // Set description property
            if(!string.IsNullOrEmpty(userDescription)) {
                attachment = SetDescription(attachment, userDescription);
            }

            // For images resolve width/height (if not in imagemagick's blacklist)
            attachment = IdentifyUnknownImage(attachment);

            // Pre render thumbnails of images 
            AttachmentPreviewBL.PreSaveAllPreviews(attachment);
            PageBL.Touch(targetPage, DateTime.UtcNow);

            //TODO MaxM: Connect with transaction
            RecentChangeBL.AddFileRecentChange(targetPage.Touched, targetPage, _dekiContext.User, DekiResources.FILE_ADDED(attachment.Name), 0);
            if(isUpdate) {
                _dekiContext.Instance.EventSink.AttachmentUpdate(_dekiContext.Now, attachment, _dekiContext.User);
            } else {
                _dekiContext.Instance.EventSink.AttachmentCreate(_dekiContext.Now, attachment, _dekiContext.User);
            }
            return attachment;
        }

        public IList<ResourceBE> RetrieveAttachments(uint? offset, uint? limit) {
            IList<ResourceBE> files = GetDeletedAttachments(limit, offset);
            List<ResourceBE> ret;

            //Apply permissions
            if(!ArrayUtil.IsNullOrEmpty(files)) {
                var distinctPageIds = files.Select(e => e.ParentPageId.Value).Distinct().ToArray();
                var allowedIds = PermissionsBL.FilterDisallowed(_dekiContext.User, distinctPageIds, false, Permissions.READ);
                
                //NOTE: this does a .contains on a list of unique pageids which *should* be pretty small
                ret = files.Where(f => allowedIds.Contains(f.ParentPageId.Value)).ToList();
            } else {
                ret = new List<ResourceBE>();
            }
            return ret;
        }

        public ResourceBE SetDescription(ResourceBE file, string description) {

            //Description of files use properties
            ResourceBE currentDesc = PropertyBL.Instance.GetAttachmentDescription(file.ResourceId);
            if(currentDesc != null) {
                PropertyBL.Instance.UpdatePropertyContent(currentDesc, new ResourceContentBE(description, MimeType.TEXT_UTF8), null, currentDesc.ETag(), AbortEnum.Modified, GetUri(file), ResourceBE.ParentType.FILE);
            } else {
                PropertyBL.Instance.CreateProperty(file.ResourceId, GetUri(file), ResourceBE.ParentType.FILE, PropertyBL.PROP_DESC, new ResourceContentBE(description, MimeType.TEXT_UTF8), null, null, AbortEnum.Exists);
            }
            return file;
        }

        public void RemoveAttachments(ResourceBE[] attachmentToRemove) {
            RemoveAttachments(attachmentToRemove, DateTime.UtcNow, 0);
        }

        public void RemoveAttachmentsFromPages(PageBE[] pages, DateTime timestamp, uint transactionId) {
            //This method used from page deletion

            IList<ResourceBE> files = _resourceBL.GetResources(pages.Select(e => (uint)e.ID).ToArray(), ResourceBE.ParentType.PAGE, ResourceBL.FILES, null, DeletionFilter.ACTIVEONLY, null, null, null);
            RemoveAttachments(files, timestamp, transactionId);
        }

        public void RemoveAttachments(IList<ResourceBE> attachmentToRemove, DateTime timestamp, uint transactionId) {

            //TODO MaxM: This batch remove exists solely to mark all files as deleted when a parent page is deleted..
            List<ulong> pageIds = new List<ulong>();
            foreach(ResourceBE file in attachmentToRemove) {
                file.AssertHeadRevision();
                pageIds.Add(file.ParentPageId.Value);
            }
            Dictionary<ulong, PageBE> pagesById = PageBL.GetPagesByIdsPreserveOrder(pageIds).AsHash(e => e.ID);

            foreach(ResourceBE file in attachmentToRemove) {
                PageBE parentPage;
                if(pagesById.TryGetValue(file.ParentPageId.Value, out parentPage)) {
                    _resourceBL.Delete(file, parentPage, transactionId);
                }
            }
        }

        public void RestoreAttachment(ResourceBE attachmentToRestore, PageBE toPage, DateTime timestamp, uint transactionId) {

            if(toPage == null || toPage.ID == 0) {
                ArchiveBE archivesMatchingPageId = _session.Archive_GetPageHeadById(attachmentToRestore.ParentPageId.Value);
                if(archivesMatchingPageId == null) {
                    throw new AttachmentRestoreFailedNoParentFatalException();
                } else {
                    toPage = PageBL.GetPageByTitle(archivesMatchingPageId.Title);
                    if(0 == toPage.ID) {
                        PageBL.Save(toPage, _resources.Localize(DekiResources.RESTORE_ATTACHMENT_NEW_PAGE_TEXT()), DekiMimeType.DEKI_TEXT, null);
                    }
                }
            }

            string filename = attachmentToRestore.Name;

            //Check for name conflicts on target page
            ResourceBE conflictingFile = GetPageAttachment(toPage.ID, filename);
            if(conflictingFile != null) {

                //rename the restored file
                filename = string.Format("{0}(restored {1}){2}", attachmentToRestore.FilenameWithoutExtension, DateTime.Now.ToString("g"), string.IsNullOrEmpty(attachmentToRestore.FilenameExtension) ? string.Empty : "." + attachmentToRestore.FilenameExtension);
                conflictingFile = GetPageAttachment(toPage.ID, filename);
                if(conflictingFile != null) {
                    throw new AttachmentRestoreNameConflictException();
                }
            }

            //Build new revision for restored file
            attachmentToRestore = BuildRevForRestore(attachmentToRestore, toPage, filename, transactionId);

            //Insert new revision into DB
            attachmentToRestore = SaveResource(attachmentToRestore);

            //Recent Changes
            RecentChangeBL.AddFileRecentChange(_dekiContext.Now, toPage, _dekiContext.User, DekiResources.FILE_RESTORED(attachmentToRestore.Name), transactionId);
            _dekiContext.Instance.EventSink.AttachmentRestore(_dekiContext.Now, attachmentToRestore, _dekiContext.User);
        }

        /// <summary>
        /// Move this attachment to the target page. 
        /// </summary>
        /// <remarks>
        /// This will fail if destination page has a file with the same name.
        /// </remarks>
        /// <param name="sourcePage">Current file location</param>
        /// <param name="targetPage">Target file location. May be same as sourcepage for rename</param>
        /// <param name="name">New filename or null for no change</param>
        /// <returns></returns>
        public ResourceBE MoveAttachment(ResourceBE attachment, PageBE sourcePage, PageBE targetPage, string name, bool loggingEnabled) {

            //TODO MaxM: Connect with a changeset
            uint changeSetId = 0;
            attachment.AssertHeadRevision();

            bool move = targetPage != null && targetPage.ID != sourcePage.ID;
            bool rename = name != null && !name.EqualsInvariant(attachment.Name);

            //Just return the current revision if no change is being made
            if(!move && !rename) {
                return attachment;
            }

            //validate filename
            if(rename) {
                name = ValidateFileName(name);
            }

            //Check the resource exists on the target (may be same as source page) with new name (if given) or current name
            ResourceBE existingAttachment = GetPageAttachment((targetPage ?? sourcePage).ID, name ?? attachment.Name);
            if(existingAttachment != null) {
                throw new AttachmentExistsOnPageConflictException(name ?? attachment.Name, (targetPage ?? sourcePage).Title.AsUserFriendlyName());
            }

            //Performing a move?
            if(move) {
                _dekiContext.Instance.Storage.MoveFile(attachment, targetPage);  //Perform the IStorage move (should be a no-op)
            }

            //Build the new revision
            ResourceBE newRevision = BuildRevForMoveAndRename(attachment, targetPage, name, changeSetId);

            //Insert new revision into DB
            try {
                newRevision = SaveResource(newRevision);
            } catch {

                //failed to save the revision, undo the file move with the IStorage. (Should be a no-op)
                if(move) {
                    _dekiContext.Instance.Storage.MoveFile(attachment, sourcePage);
                }
                throw;
                //NOTE MaxM: file rename does not even touch IStorage. No need to undo it
            }

            //Notification for file move
            if(loggingEnabled) {

                if(move) {
                    RecentChangeBL.AddFileRecentChange(_dekiContext.Now, sourcePage, _dekiContext.User, DekiResources.FILE_MOVED_TO(attachment.Name, targetPage.Title.AsPrefixedUserFriendlyPath()), changeSetId);
                    RecentChangeBL.AddFileRecentChange(_dekiContext.Now, targetPage, _dekiContext.User, DekiResources.FILE_MOVED_FROM(attachment.Name, sourcePage.Title.AsPrefixedUserFriendlyPath()), changeSetId);
                }
                if(rename) {
                    RecentChangeBL.AddFileRecentChange(_dekiContext.Now, sourcePage, _dekiContext.User, DekiResources.FILE_RENAMED_TO(attachment.Name, name), changeSetId);
                }
            }

            //Notification for file rename and move use same event
            _dekiContext.Instance.EventSink.AttachmentMove(_dekiContext.Now, attachment, sourcePage, _dekiContext.User);

            return newRevision;
        }

        public void WipeAttachments(IList<ResourceBE> attachments) {
            if(attachments == null) {
                return;
            }

            List<uint> fileids = new List<uint>();
            foreach(ResourceBE file in attachments) {
                try {

                    // ensure that attachment to be wiped is head revision
                    _dekiContext.Instance.Storage.DeleteFile(file, SizeType.ORIGINAL);
                    _dekiContext.Instance.Storage.DeleteFile(file, SizeType.THUMB);
                    _dekiContext.Instance.Storage.DeleteFile(file, SizeType.WEBVIEW);
                    fileids.Add(file.ResourceId);
                } catch(Exception e) {
                    _dekiContext.Instance.Log.WarnExceptionMethodCall(e, "WipeAttachments: delete file failed", file.ResourceId, file.Revision);
                }
            }

            _session.Resources_Delete(fileids);
        }

        public ResourceBE[] IdentifyUnknownImages(IEnumerable<ResourceBE> attachments) {
            List<ResourceBE> ret = new List<ResourceBE>();
            foreach(ResourceBE file in attachments) {
                ResourceBE updatedFile = file;
                if(file != null && IsAllowedForImageMagickPreview(file) && (file.MetaXml.ImageHeight == null || file.MetaXml.ImageWidth == null)) {
                    StreamInfo fileInfo = _dekiContext.Instance.Storage.GetFile(file, SizeType.ORIGINAL, false);
                    if(file != null) {
                        int width;
                        int height;
                        int frames;
                        if(AttachmentPreviewBL.RetrieveImageDimensions(fileInfo, out width, out height, out frames)) {
                            file.MetaXml.ImageWidth = width;
                            file.MetaXml.ImageHeight = height;

                            // check if we need to store the number of frames (default is 1)
                            if(frames > 1) {
                                file.MetaXml.ImageFrames = frames;
                            }
                            updatedFile = _resourceBL.UpdateResourceRevision(file);
                        }
                    }
                }
                ret.Add(updatedFile);
            }
            return ret.ToArray();
        }

        public ResourceBE IdentifyUnknownImage(ResourceBE image) {
            return IdentifyUnknownImages(new ResourceBE[] { image })[0];
        }

        public XDoc GetAttachmentRevisionListXml(IList<ResourceBE> fileList) {
            return GetAttachmentRevisionListXml(fileList, null);
        }

        public XDoc GetAttachmentRevisionListXml(IList<ResourceBE> fileList, XUri href) {
            XDoc attachmentListDoc = null;

            attachmentListDoc = new XDoc("files");
            attachmentListDoc.Attr("count", fileList == null ? 0 : fileList.Count);
            if(href != null)
                attachmentListDoc.Attr("href", href);

            if(fileList != null) {
                List<ResourceBE> sortedFiles = new List<ResourceBE>(fileList);
                sortedFiles = SortFileListByNameAndRevision(sortedFiles);

                //HACK: Convenient place to ensure all images that haven't been identified are looked at
                IdentifyUnknownImages(sortedFiles);

                //Add attachment info to list wrapper xml
                foreach(ResourceBE att in sortedFiles) {
                    attachmentListDoc.Add(GetFileXml(att, true, null, null));
                }
            }

            return attachmentListDoc;
        }

        public ResourceBE[] ModifyRevisionVisibility(ResourceBE res, XDoc request, string comment) {
            List<ResourceBE> revisionsToHide = new List<ResourceBE>();
            List<ResourceBE> revisionsToUnhide = new List<ResourceBE>();
            List<ResourceBE> ret = new List<ResourceBE>();

            foreach(XDoc fileDoc in request["/revisions/file"]) {
                ulong? id = fileDoc["@id"].AsULong;

                //Provided id of all file revision must match the file id
                if(id != null && id.Value != res.MetaXml.FileId) {
                    throw new MismatchedIdInvalidArgumentException();
                }

                int? revNum = fileDoc["@revision"].AsInt;
                if((revNum ?? 0) <= 0) {
                    throw new RevisionInvalidArgumentException();
                }

                //Hiding the head revision is not allowed. Reasons include:
                //* Behavior of search indexing undefined
                //* Behavior of accessing HEAD revision is undefined
                if(revNum == res.ResourceHeadRevision) {
                    throw new HideHeadInvalidOperationException();
                }

                bool? hide = fileDoc["@hidden"].AsBool;
                if(hide == null) {
                    throw new HiddenAttributeInvalidArgumentException();
                }

                ResourceBE rev = _resourceBL.GetResourceRevision(res.ResourceId, revNum.Value);
                if(rev == null) {
                    throw new RevisionNotFoundInvalidArgumentException();
                }

                //Only allow hiding revisions with content changes
                if((rev.ChangeMask & ResourceBE.ChangeOperations.CONTENT) != ResourceBE.ChangeOperations.CONTENT) {
                    throw new RevisionCannotBeHiddenConflictException();
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

            uint currentUserId = _dekiContext.User.ID;
            DateTime currentTs = DateTime.UtcNow;

            foreach(ResourceBE rev in revisionsToHide) {
                rev.IsHidden = true;
                rev.MetaXml.RevisionHiddenUserId = currentUserId;
                rev.MetaXml.RevisionHiddenTimestamp = currentTs;
                rev.MetaXml.RevisionHiddenComment = comment;
                ret.Add(_resourceBL.UpdateResourceRevision(rev));
            }

            if(revisionsToUnhide.Count > 0) {
                PermissionsBL.CheckUserAllowed(_dekiContext.User, Permissions.ADMIN);
            }

            foreach(ResourceBE rev in revisionsToUnhide) {
                rev.IsHidden = false;
                rev.MetaXml.RevisionHiddenUserId = null;
                rev.MetaXml.RevisionHiddenTimestamp = null;
                rev.MetaXml.RevisionHiddenComment = null;
                ret.Add(_resourceBL.UpdateResourceRevision(rev));
            }

            return ret.ToArray();
        }

        protected ResourceBE SaveResource(ResourceBE res) {
            ResourceBE ret = null;
            if(res.IsNewResource()) {

                //New attachments get a legacy fileid mapping.
                uint fileId = ResourceMapBL.GetNewFileId();
                res.MetaXml.FileId = fileId;
                ret = _resourceBL.SaveResource(res);
                ResourceMapBL.UpdateFileIdMapping(fileId, ret.ResourceId);
            } else {
                ret = _resourceBL.SaveResource(res);
            }
            return ret;
        }

        protected ResourceBE BuildRevForContentUpdate(ResourceBE currentResource, MimeType mimeType, uint size, string description, string name, ResourceContentBE newContent) {
            ResourceBE newRev = _resourceBL.BuildRevForContentUpdate(currentResource, mimeType, size, description, name, newContent);
            newRev.MetaXml.FileId = currentResource.MetaXml.FileId;
            return newRev;
        }

        protected ResourceBE BuildRevForMoveAndRename(ResourceBE currentResource, PageBE targetPage, string name, uint changeSetId) {
            ResourceBE newRev = _resourceBL.BuildRevForMoveAndRename(currentResource, targetPage, name, changeSetId);
            newRev.MetaXml.FileId = currentResource.MetaXml.FileId;
            return newRev;
        }

        protected ResourceBE BuildRevForRemove(ResourceBE currentResource, DateTime timestamp, uint changeSetId) {
            ResourceBE newRev = _resourceBL.BuildRevForRemove(currentResource, timestamp, changeSetId);
            newRev.MetaXml.FileId = currentResource.MetaXml.FileId;
            return newRev;
        }

        protected ResourceBE BuildRevForRestore(ResourceBE currentResource, PageBE targetPage, string resourceName, uint changeSetId) {
            ResourceBE newRev = _resourceBL.BuildRevForRestore(currentResource, targetPage, resourceName, changeSetId);
            newRev.MetaXml.FileId = currentResource.MetaXml.FileId;
            return newRev;
        }

        public IEnumerable<ResourceBE> GetAllAttachementsChunked() {
            const uint limit = 1000;
            uint offset = 0;
            while(true) {
                var chunk = _resourceBL.GetResources(null, null, ResourceBL.FILES, null, DeletionFilter.ACTIVEONLY, null, limit, offset);
                _log.DebugFormat("got chunk of {0} attachments", chunk.Count);
                foreach(var attachment in chunk) {
                    yield return attachment;
                }
                if(chunk.Count < limit) {
                    yield break;
                }
                offset += limit;
            }
        }

        #region Uris
        public XUri GetUri(ResourceBE file) {
            XUri uri = null;
            if(file.ResourceIsDeleted) {
                uri = _dekiContext.ApiUri.At("archive", "files", (file.MetaXml.FileId ?? 0).ToString());
            } else {
                uri = _dekiContext.ApiUri.At("files", (file.MetaXml.FileId ?? 0).ToString());
            }
            return uri;
        }

        public XUri GetUriContent(ResourceBE file) {
            return GetUriContent(file, !file.IsHeadRevision());
        }

        public XUri GetUriContent(ResourceBE file, bool? includeRevision) {
            if(includeRevision == null) {
                includeRevision = !file.IsHeadRevision();
            }

            XUri uri = GetUri(file);

            if(includeRevision.Value) {
                uri = uri.With("revision", file.Revision.ToString());
            }

            uri = uri.At(Title.AsApiParam(file.Name));
            return uri;
        }

        public XUri GetUriInfo(ResourceBE file) {
            return GetUriInfo(file, !file.IsHeadRevision());
        }

        public XUri GetUriInfo(ResourceBE file, bool? includeRevision) {
            if(includeRevision == null) {
                includeRevision = !file.IsHeadRevision();
            }

            if(includeRevision.Value) {
                return GetUri(file).At("info").With("revision", file.Revision.ToString());
            } else {
                return GetUri(file).At("info");
            }
        }

        #endregion

        #region XML Helpers

        public XDoc GetFileRevisionsXml(ResourceBE resource, ResourceBE.ChangeOperations changeFilter, XUri listUri, int? totalcount) {
            IList<ResourceBE> revisions = _resourceBL.GetResourceRevisions(resource.ResourceId, changeFilter, SortDirection.ASC, null);
            return GetFileXml(revisions, true, true, null, true, totalcount, listUri);
        }

        public XDoc GetFileXml(ResourceBE file, bool verbose, string fileSuffix, bool? explicitRevisionInfo) {
            return GetFileXml(new List<ResourceBE>() { file }, verbose, false, fileSuffix, explicitRevisionInfo, null, null);
        }

        public XDoc GetFileXml(IList<ResourceBE> files, bool verbose, string fileSuffix, bool? explicitRevisionInfo, XUri listUri) {
            return GetFileXml(files, verbose, true, fileSuffix, explicitRevisionInfo, null, listUri);
        }

        private XDoc GetFileXml(IList<ResourceBE> files, bool verbose, bool list, string fileSuffix, bool? explicitRevisionInfo, int? totalCount, XUri listUri) {
            Dictionary<uint, UserBE> users = new Dictionary<uint, UserBE>();
            Dictionary<ulong, PageBE> pages = new Dictionary<ulong, PageBE>();
            List<uint> parentIds = new List<uint>();

            //Collect related entity id's 
            foreach(ResourceBE f in files) {
                users[f.UserId] = null;
                pages[f.ParentPageId.Value] = null;
                parentIds.Add(f.ResourceId);
            }

            //Perform batch lookups of related entities
            users = _session.Users_GetByIds(users.Keys.ToArray()).AsHash(e => e.ID);
            if(verbose) {
                pages = PageBL.GetPagesByIdsPreserveOrder(pages.Keys.ToArray()).AsHash(e => e.ID);
            }

            //Associate properties with the given attachments
            files = _resourceBL.PopulateChildren(files.ToArray(), new[] { ResourceBE.Type.PROPERTY }, explicitRevisionInfo ?? false);

            XDoc ret = XDoc.Empty;
            if(list) {
                List<ResourceBE> sortedFiles = new List<ResourceBE>(files);
                files = SortFileListByNameAndRevision(sortedFiles).ToArray();
                ret = new XDoc(string.IsNullOrEmpty(fileSuffix) ? "files" : "files." + fileSuffix);
                ret.Attr("count", files.Count);
                if(totalCount != null) {
                    ret.Attr("totalcount", totalCount.Value);
                }
                if(listUri != null) {
                    ret.Attr("href", listUri);
                }
            }
            foreach(ResourceBE f in files) {
                UserBE updatedByUser;
                PageBE parentPage;
                users.TryGetValue(f.UserId, out updatedByUser);
                pages.TryGetValue(f.ParentPageId.Value, out parentPage);
                ret = AppendFileXml(ret, f, fileSuffix, explicitRevisionInfo, updatedByUser, parentPage);
            }

            return ret;
        }

        private XDoc AppendFileXml(XDoc doc, ResourceBE file, string fileSuffix, bool? explicitRevisionInfo, UserBE updatedByUser, PageBE parentPage) {
            bool requiresEnd = false;
            string fileElement = string.IsNullOrEmpty(fileSuffix) ? "file" : "file." + fileSuffix;
            if(doc == null || doc.IsEmpty) {
                doc = new XDoc(fileElement);
            } else {
                doc.Start(fileElement);
                requiresEnd = true;
            }
            doc.Attr("id", file.MetaXml.FileId ?? 0);
            doc.Attr("revision", file.Revision);
            doc.Attr("res-id", file.ResourceId);
            if(file.IsHidden) {
                doc.Attr("hidden", true);
            }
            doc.Attr("href", GetUriInfo(file, explicitRevisionInfo));
            doc.Start("filename").Value(file.Name).End();

            //Description comes from a property
            string description = string.Empty;
            if(!ArrayUtil.IsNullOrEmpty(file.ChildResources)) {
                ResourceBE descProp = Array.Find(file.ChildResources, p => p != null && p.ResourceType == ResourceBE.Type.PROPERTY && p.Name.EqualsInvariantIgnoreCase(PropertyBL.PROP_DESC));
                if(descProp != null) {
                    description = descProp.Content.ToText();
                }
            }
            doc.Start("description").Value(description).End();
            doc.Start("contents")
                .Attr("type", file.MimeType == null ? null : file.MimeType.ToString())
                .Attr("size", file.Size);
            if((file.MetaXml.ImageHeight ?? 0) > 0 && (file.MetaXml.ImageWidth ?? 0) > 0) {
                doc.Attr("width", file.MetaXml.ImageWidth.Value);
                doc.Attr("height", file.MetaXml.ImageHeight.Value);
            }
            doc.Attr("href", GetUriContent(file, explicitRevisionInfo));
            doc.End(); //contents
            if((file.MetaXml.ImageWidth ?? 0) > 0 && (file.MetaXml.ImageHeight ?? 0) > 0) {
                string previewMime = AttachmentPreviewBL.ResolvePreviewMime(file.MimeType).ToString();
                doc.Start("contents.preview")
                    .Attr("rel", "thumb")
                    .Attr("type", previewMime)
                    .Attr("maxwidth", _dekiContext.Instance.ImageThumbPixels)
                    .Attr("maxheight", _dekiContext.Instance.ImageThumbPixels)
                    .Attr("href", GetUriContent(file, explicitRevisionInfo).With("size", "thumb"));
                if(!file.IsHeadRevision() || (explicitRevisionInfo ?? false)) {
                    doc.Attr("revision", file.Revision);
                }
                doc.End(); //contents.preview: thumb
                doc.Start("contents.preview")
                    .Attr("rel", "webview")
                    .Attr("type", previewMime)
                    .Attr("maxwidth", _dekiContext.Instance.ImageWebviewPixels)
                    .Attr("maxheight", _dekiContext.Instance.ImageWebviewPixels)
                    .Attr("href", GetUriContent(file, explicitRevisionInfo).With("size", "webview"));
                if(!file.IsHeadRevision() || (explicitRevisionInfo ?? false)) {
                    doc.Attr("revision", file.Revision);
                }
                doc.End(); //contents.preview: webview
            }
            doc.Start("date.created").Value(file.Timestamp).End();
            if(updatedByUser != null) {
                doc.Add(UserBL.GetUserXml(updatedByUser, "createdby", Utils.ShowPrivateUserInfo(updatedByUser)));
            }
            if(file.ResourceIsDeleted && ((file.ChangeMask & ResourceBE.ChangeOperations.DELETEFLAG) == ResourceBE.ChangeOperations.DELETEFLAG)) {
                if(updatedByUser != null) {
                    doc.Add(UserBL.GetUserXml(updatedByUser, "deletedby", Utils.ShowPrivateUserInfo(updatedByUser)));
                }
                doc.Start("date.deleted").Value(file.Timestamp).End();
            }
            if(file.IsHeadRevision() && !(explicitRevisionInfo ?? false) && !file.ResourceIsDeleted) {
                uint filteredCount = _session.Resources_GetRevisionCount(file.ResourceId, DEFAULT_REVISION_FILTER);
                doc.Start("revisions");
                doc.Attr("count", filteredCount);
                doc.Attr("totalcount", file.Revision);
                doc.Attr("href", GetUri(file).At("revisions"));
                doc.End();
            } else {
                if(file.ChangeMask != ResourceBE.ChangeOperations.UNDEFINED) {
                    doc.Start("user-action").Attr("type", file.ChangeMask.ToString().ToLowerInvariant()).End();
                }
            }

            //parent page is passed in for verbose output only
            if(parentPage != null) {
                doc.Add(PageBL.GetPageXml(parentPage, "parent"));
            }
            if(file.ChildResources != null) {
                List<ResourceBE> properties = new List<ResourceBE>();
                foreach(ResourceBE p in file.ChildResources) {
                    properties.Add(p);
                }
                doc = PropertyBL.Instance.GetPropertyXml(properties.ToArray(), GetUri(file), null, null, doc);
            }
            if(file.IsHidden) {
                uint? userIdHiddenBy = file.MetaXml.RevisionHiddenUserId;
                if(userIdHiddenBy != null) {
                    UserBE userHiddenBy = UserBL.GetUserById(userIdHiddenBy.Value);
                    if(userHiddenBy != null) {
                        doc.Add(UserBL.GetUserXml(userHiddenBy, "hiddenby", Utils.ShowPrivateUserInfo(userHiddenBy)));
                    }
                }
                doc.Elem("date.hidden", file.MetaXml.RevisionHiddenTimestamp ?? DateTime.MinValue);
                doc.Elem("description.hidden", file.MetaXml.RevisionHiddenComment ?? string.Empty);
            }
            if(requiresEnd) {
                doc.End(); //file
            }
            return doc;
        }

        #endregion

        #region Private Helper Methods

        private bool IsBlockedFileExtension(string extension) {
            bool ret = false;
            string[] blocklist = _dekiContext.Instance.FileExtensionBlockList;
            if(blocklist != null && blocklist.Length > 0) {
                if(new List<string>(blocklist).Contains(extension.ToLowerInvariant().Trim()))
                    ret = true;
            }

            return ret;
        }

        private string CleanFileName(string rawfilename) {
            if(rawfilename == null)
                return null;

            StringBuilder sb = new StringBuilder();
            foreach(char c in rawfilename) {
                if(_charReplaceMap.ContainsKey(c))
                    sb.Append(_charReplaceMap[c]);
                else
                    sb.Append(c);
            }

            return sb.ToString();
        }

        private string ValidateFileName(string filename) {
            string ret = CleanFileName(filename).Trim();

            if(Path.GetFileNameWithoutExtension(ret).Length == 0 || filename.Length > MAX_FILENAME_LENGTH) {
                throw new AttachmentFilenameInvalidArgumentException();
            }

            string extension = Path.GetExtension(ret).TrimStart('.');

            if(IsBlockedFileExtension(extension)) {
                _dekiContext.Instance.Log.WarnMethodCall("attach file not allowed: extension", extension);
                throw new AttachmentFiletypeNotAllowedInvalidArgumentException(extension);
            }

            return ret;
        }

        private List<ResourceBE> SortFileListByNameAndRevision(List<ResourceBE> files) {

            files.Sort(delegate(ResourceBE file1, ResourceBE file2) {
                int compare = 0;

                //Perform a sort on name if the resource id's are different. Multiple revs of same file should not sort on name.
                if(file1.ResourceId != file2.ResourceId) {
                    compare = StringUtil.CompareInvariant(file1.Name, file2.Name);
                }
                if(compare != 0)
                    return compare;
                else {
                    // compare on revision number
                    return file1.Revision.CompareTo(file2.Revision);
                }
            });
            return files;
        }

        #endregion
    }
}

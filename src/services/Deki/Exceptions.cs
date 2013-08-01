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
using MindTouch.Deki.Logic;
using MindTouch.Dream;

namespace MindTouch.Deki.Exceptions {

    #region Base exceptions
    public abstract class MindTouchException : Exception {

        //--- Fields ---

        // TODO (arnec): there should be no explicit exception to HTTP status mapping. This only exists because of how errors are currently
        // reported on Property batch operations
        public DreamStatus Status = DreamStatus.BadRequest;

        //--- Constructors ---
        protected MindTouchException() { }
        protected MindTouchException(string message, Exception inner) : base(message, inner) { }
    }

    public abstract class ResourcedMindTouchException : MindTouchException {

        //--- Fields ----
        public readonly DekiResource Resource;

        //--- Constructors ---
        protected ResourcedMindTouchException(DekiResource resource) {
            Resource = resource;
        }

        //--- Properties ---
        public override string Message {
            get { return Resource == null ? null : Resource.ToString(); }
        }
    }

    public abstract class MindTouchInvalidCallException : ResourcedMindTouchException {
        protected MindTouchInvalidCallException(DekiResource resource) : base(resource) { }
    }

    public abstract class MindTouchArgumentException : MindTouchInvalidCallException {
        protected MindTouchArgumentException(DekiResource resource) : base(resource) { }
    }

    public abstract class MindTouchFatalCallException : ResourcedMindTouchException {
        protected MindTouchFatalCallException(DekiResource resource) : base(resource) {
            Status = DreamStatus.InternalError;
        }
    }

    public abstract class MindTouchConflictException : ResourcedMindTouchException {
        protected MindTouchConflictException(DekiResource resource) : base(resource) {
            Status = DreamStatus.Conflict;
        }
    }

    public abstract class MindTouchNotFoundException : ResourcedMindTouchException {
        protected MindTouchNotFoundException(DekiResource resource) : base(resource) {
            Status = DreamStatus.NotFound;
        }
    }

    public abstract class MindTouchForbiddenException : ResourcedMindTouchException {
        protected MindTouchForbiddenException(DekiResource resource) : base(resource) {
            Status = DreamStatus.Forbidden;

        }
    }

    public abstract class MindTouchAccessDeniedException : ResourcedMindTouchException {

        //--- Fields ---
        public readonly string AuthRealm;

        //--- Constructors ---
        protected MindTouchAccessDeniedException(string authRealm, DekiResource resource) : base(resource) {
            AuthRealm = authRealm;
            Status = DreamStatus.Unauthorized;
        }
    }

    public abstract class MindTouchNotImplementedException : ResourcedMindTouchException {
        protected MindTouchNotImplementedException(DekiResource resource) : base(resource) {
            Status = DreamStatus.NotImplemented;
        }
    }
    #endregion

    #region resource-less exceptions
    public class MindTouchInvalidOperationException : MindTouchException {
        public MindTouchInvalidOperationException(string message, Exception inner) : base(message, inner) { }
    }
    #endregion

    #region shared exceptions
    public class NoSuchInstanceInvalidArgumentException : MindTouchInvalidCallException {
        public NoSuchInstanceInvalidArgumentException() : base(DekiResources.NO_INSTANCE_FOR_HOSTNAME()) { }
    }

    public class OutputParameterInvalidArgumentException : MindTouchInvalidCallException {
        public OutputParameterInvalidArgumentException() : base(DekiResources.OUTPUT_PARAM_INVALID()) { }
    }

    public class MaxParameterInvalidArgumentException : MindTouchInvalidCallException {
        public MaxParameterInvalidArgumentException() : base(DekiResources.MAX_PARAM_INVALID()) { }
    }

    public class OffsetParameterInvalidArgumentException : MindTouchInvalidCallException {
        public OffsetParameterInvalidArgumentException() : base(DekiResources.OFFSET_PARAM_INVALID()) { }
    }

    public class FormatParameterInvalidArgumentException : MindTouchInvalidCallException {
        public FormatParameterInvalidArgumentException() : base(DekiResources.FORMAT_PARAM_INVALID()) { }
    }

    public class LimitParameterInvalidArgumentException : MindTouchInvalidCallException {
        public LimitParameterInvalidArgumentException() : base(DekiResources.LIMIT_PARAM_INVALID()) { }
    }

    public class CascadeParameterInvalidArgumentException : MindTouchInvalidCallException {
        public CascadeParameterInvalidArgumentException() : base(DekiResources.CASCADE_PARAM_INVALID()) { }
    }

    public class PostedDocumentInvalidArgumentException : MindTouchInvalidCallException {
        public PostedDocumentInvalidArgumentException(string documentroot) : base(DekiResources.INVALID_POSTED_DOCUMENT_1(documentroot)) { }
    }

    public class UnsupportedContentTypeInvalidArgumentException : MindTouchInvalidCallException {
        public UnsupportedContentTypeInvalidArgumentException(MimeType unsupported) : base(DekiResources.CONTENT_TYPE_NOT_SUPPORTED(unsupported)) { }
    }

    public class SectionParamInvalidArgumentException : MindTouchInvalidCallException {
        public SectionParamInvalidArgumentException() : base(DekiResources.SECTION_PARAM_INVALID()) { }
    }

    public class TitleRenameNameInvalidArgumentException : MindTouchInvalidCallException {
        public TitleRenameNameInvalidArgumentException(string message) : base(DekiResources.TITLE_RENAME_FAILURE(message)) { }
    }

    public class TalkPageLanguageCannotBeSetConflictException : MindTouchConflictException {
        public TalkPageLanguageCannotBeSetConflictException() : base(DekiResources.LANGUAGE_SET_TALK()) { }
    }

    public class ServiceAuthIdAttrInvalidArgumentException : MindTouchInvalidCallException {
        public ServiceAuthIdAttrInvalidArgumentException() : base(DekiResources.SERVICE_AUTH_ID_ATTR_INVALID()) { }
    }

    public class ServiceDoesNotExistInvalidArgumentException : MindTouchInvalidCallException {
        public ServiceDoesNotExistInvalidArgumentException(uint serviceId) : base(DekiResources.SERVICE_DOES_NOT_EXIST(serviceId)) { }
    }

    public class RoleDoesNotExistInvalidArgumentException : MindTouchInvalidCallException {
        public RoleDoesNotExistInvalidArgumentException(string role) : base(DekiResources.ROLE_DOES_NOT_EXIST(role)) { }
    }

    public class UserIdAttrInvalidArgumentException : MindTouchInvalidCallException {
        public UserIdAttrInvalidArgumentException() : base(DekiResources.USER_ID_ATTR_INVALID()) { }
    }

    public class RevisionHeadOrIntInvalidArgumentException : MindTouchInvalidCallException {
        public RevisionHeadOrIntInvalidArgumentException() : base(DekiResources.REVISION_HEAD_OR_INT()) { }
    }

    public class MismatchedIdInvalidArgumentException : MindTouchInvalidCallException {
        public MismatchedIdInvalidArgumentException() : base(DekiResources.MISMATCHED_ID()) { }
    }

    public class RevisionInvalidArgumentException : MindTouchInvalidCallException {
        public RevisionInvalidArgumentException() : base(DekiResources.INVALID_REVISION()) { }
    }

    public class HideHeadInvalidOperationException : MindTouchInvalidCallException {
        public HideHeadInvalidOperationException() : base(DekiResources.CANNOT_HIDE_HEAD()) { }
    }

    public class HiddenAttributeInvalidArgumentException : MindTouchInvalidCallException {
        public HiddenAttributeInvalidArgumentException() : base(DekiResources.HIDDEN_ATTRIBUTE()) { }
    }

    public class RevisionNotFoundInvalidArgumentException : MindTouchInvalidCallException {
        public RevisionNotFoundInvalidArgumentException() : base(DekiResources.REVISION_NOT_FOUND()) { }
    }

    public class NoRevisionToHideUnHideInvalidOperationException : MindTouchInvalidCallException {
        public NoRevisionToHideUnHideInvalidOperationException() : base(DekiResources.NO_REVISION_TO_HIDE_UNHIDE()) { }
    }

    public class RevisionCannotBeHiddenConflictException : MindTouchConflictException {
        public RevisionCannotBeHiddenConflictException() : base(DekiResources.REVISION_CANNOT_BE_HIDDEN()) { }
    }
    #endregion

    #region AttachmentBL exceptions
    public class AttachmentMaxFileSizeAllowedInvalidArgumentException : MindTouchInvalidCallException {
        public AttachmentMaxFileSizeAllowedInvalidArgumentException(long maxfilesize) : base(DekiResources.MAX_FILE_SIZE_ALLOWED(maxfilesize)) { }
    }

    public class AttachmentExistsOnPageConflictException : MindTouchConflictException {
        public AttachmentExistsOnPageConflictException(string fileName, string pagePath)
            : base(DekiResources.ATTACHMENT_EXISTS_ON_PAGE(fileName, pagePath)) {
        }
    }

    public class AttachmentNotChangedInvalidOperationException : MindTouchInvalidCallException {
        public AttachmentNotChangedInvalidOperationException(string fileName, string pagePath)
            : base(DekiResources.ATTACHMENT_EXISTS_ON_PAGE(fileName, pagePath)) {
        }
    }

    public class AttachmentRestoreFailedNoParentFatalException : MindTouchFatalCallException {
        public AttachmentRestoreFailedNoParentFatalException() : base(DekiResources.RESTORE_FILE_FAILED_NO_PARENT()) { }
    }

    public class AttachmentRestoreNameConflictException : MindTouchConflictException {
        public AttachmentRestoreNameConflictException() : base(DekiResources.FILE_RESTORE_NAME_CONFLICT()) { }
    }

    public class AttachmentFilenameInvalidArgumentException : MindTouchInvalidCallException {
        public AttachmentFilenameInvalidArgumentException() : base(DekiResources.FILENAME_IS_INVALID()) { }
    }

    public class AttachmentFiletypeNotAllowedInvalidArgumentException : MindTouchInvalidCallException {
        public AttachmentFiletypeNotAllowedInvalidArgumentException(string extension) : base(DekiResources.FILE_TYPE_NOT_ALLOWED(extension)) { }
    }

    public class AttachmentCannotParseNumFilesInvalidArgumentException : MindTouchInvalidCallException {
        public AttachmentCannotParseNumFilesInvalidArgumentException() : base(DekiResources.CANNOT_PARSE_NUMFILES()) { }
    }

    public class AttachmentFileRatioInvalidArgumentException : MindTouchInvalidCallException {
        public AttachmentFileRatioInvalidArgumentException() : base(DekiResources.INVALID_FILE_RATIO()) { }
    }

    public class AttachmentFilesizeInvalidArgumentException : MindTouchInvalidCallException {
        public AttachmentFilesizeInvalidArgumentException() : base(DekiResources.INVALID_FILE_SIZE()) { }
    }

    public class AttachmentFileFormatInvalidArgumentException : MindTouchInvalidCallException {
        public AttachmentFileFormatInvalidArgumentException() : base(DekiResources.INVALID_FILE_FORMAT()) { }
    }

    public class AttachmentDoesNotExistFatalException : MindTouchFatalCallException {
        public AttachmentDoesNotExistFatalException(uint fileResourceId, int revision) : base(DekiResources.COULD_NOT_RETRIEVE_FILE(fileResourceId, revision)) { }
    }

    public class AttachmentNotFoundException : MindTouchNotFoundException {
        public AttachmentNotFoundException() : base(DekiResources.COULD_NOT_FIND_FILE()) { }
    }

    public class AttachmentRemovedNotFoundException : MindTouchNotFoundException {
        public AttachmentRemovedNotFoundException() : base(DekiResources.FILE_HAS_BEEN_REMOVED()) { }
    }

    public class AttachmentUploadSaveFatalException : MindTouchFatalCallException {
        public AttachmentUploadSaveFatalException() : base(DekiResources.FAILED_TO_SAVE_UPLOAD()) { }
    }

    public class AttachmentMoveInvalidArgumentException : MindTouchInvalidCallException {
        public AttachmentMoveInvalidArgumentException() : base(DekiResources.ATTACHMENT_MOVE_INVALID_PARAM()) { }
    }

    public class AttachmentAlreadyMovedNotFoundException : MindTouchNotFoundException {
        public AttachmentAlreadyMovedNotFoundException() : base(DekiResources.FILE_ALREADY_REMOVED()) { }
    }

    public class AttachmentUnsupportedRevisionInvalidArgumentException : MindTouchInvalidCallException {
        public AttachmentUnsupportedRevisionInvalidArgumentException() : base(DekiResources.REVISION_NOT_SUPPORTED()) { }
    }

    public class AttachmentMissingFilenameInvalidArgumentException : MindTouchInvalidCallException {
        public AttachmentMissingFilenameInvalidArgumentException() : base(DekiResources.MISSING_FILENAME()) { }
    }

    public class AttachmentArchiveFileNotDeletedNotFoundException : MindTouchNotFoundException {
        public AttachmentArchiveFileNotDeletedNotFoundException() : base(DekiResources.FILE_NOT_DELETED()) { }
    }
    #endregion

    #region AttachmentPreviewBL exceptions
    public class ImagePreviewOversizedInvalidArgumentException : MindTouchInvalidCallException {
        public ImagePreviewOversizedInvalidArgumentException() : base(DekiResources.IMAGE_REQUEST_TOO_LARGE()) { }
    }

    public class AttachmentPreviewFailedWithMimeTypeNotImplementedException : MindTouchNotImplementedException {
        public AttachmentPreviewFailedWithMimeTypeNotImplementedException(MimeType mimeType) : base(DekiResources.FAILED_WITH_MIME_TYPE(mimeType)) { }
    }

    public class AttachmentPreviewFormatConversionWithSizeNotImplementedException : MindTouchNotImplementedException {
        public AttachmentPreviewFormatConversionWithSizeNotImplementedException() : base(DekiResources.FORMAT_CONVERSION_WITH_SIZE_UNSUPPORTED()) { }
    }

    public class AttachmentPreviewBadImageFatalException : MindTouchException {

        //--- Fields ---
        public readonly MimeType PreviewMimeType;
        public readonly byte[] PreviewImage;

        //--- Constructors ---
        public AttachmentPreviewBadImageFatalException(MimeType previewMimeType, byte[] previewImage) {
            PreviewMimeType = previewMimeType;
            PreviewImage = previewImage;
        }
    }

    public class AttachmentPreviewNoImageFatalException : MindTouchFatalCallException {
        public AttachmentPreviewNoImageFatalException() : base(DekiResources.CANNOT_CREATE_THUMBNAIL()) { }
    }
    #endregion

    #region AuthBL exceptions
    public class AuthServiceIdInvalidArgumentException : MindTouchInvalidCallException {
        public AuthServiceIdInvalidArgumentException(uint serviceId) : base(DekiResources.INVALID_SERVICE_ID(serviceId)) { }
    }

    public class AuthNotAnAuthServiceInvalidArgumentException : MindTouchInvalidCallException {
        public AuthNotAnAuthServiceInvalidArgumentException(uint serviceId) : base(DekiResources.NOT_AUTH_SERVICE(serviceId)) { }
    }

    public class AuthLoginExternalUserConflictException : MindTouchConflictException {
        public AuthLoginExternalUserConflictException(string authserviceDescription)
            : base(DekiResources.LOGIN_EXTERNAL_USER_CONFLICT(authserviceDescription)) {
        }
    }

    public class LoginExternalUserUnknownConflictException : MindTouchConflictException {
        public LoginExternalUserUnknownConflictException() : base(DekiResources.LOGIN_EXTERNAL_USER_CONFLICT_UNKNOWN()) { }
    }

    public class AuthUserDisabledForbiddenException : MindTouchForbiddenException {
        public AuthUserDisabledForbiddenException(string username) : base(DekiResources.USER_DISABLED(username)) { }
    }

    public class AuthFailedDeniedException : MindTouchAccessDeniedException {
        public AuthFailedDeniedException(string authRealm) : base(authRealm, DekiResources.AUTHENTICATION_FAILED()) { }
    }
    #endregion

    #region BanningBL exceptions
    public class BanEmptyInvalidArgumentException : MindTouchInvalidCallException {
        public BanEmptyInvalidArgumentException() : base(DekiResources.BANNING_EMPTY_BAN()) { }
    }

    public class BanNoPermsInvalidArgumentException : MindTouchInvalidCallException {
        public BanNoPermsInvalidArgumentException() : base(DekiResources.BANNING_NO_PERMS()) { }
    }

    public class BanIdNotFoundException : MindTouchNotFoundException {
        public BanIdNotFoundException(uint banId) : base(DekiResources.BANNING_NOT_FOUND_ID(banId)) { }
    }

    public class BanningOwnerConflict : MindTouchConflictException {
        public BanningOwnerConflict() : base(DekiResources.BANNING_OWNER_CONFLICT()) { }
    }
    #endregion

    #region CommentBL exceptions
    public class CommentMimetypeUnsupportedInvalidArgumentException : MindTouchInvalidCallException {
        public CommentMimetypeUnsupportedInvalidArgumentException(MimeType mimeType) : base(DekiResources.COMMENT_MIMETYPE_UNSUPPORTED(mimeType)) { }
    }

    public class CommentFilterInvalidArgumentException : MindTouchInvalidCallException {
        public CommentFilterInvalidArgumentException() : base(DekiResources.FILTER_PARAM_INVALID()) { }
    }

    public class CommentFailedEditFatalException : MindTouchFatalCallException {
        public CommentFailedEditFatalException() : base(DekiResources.FAILED_EDIT_COMMENT()) { }
    }

    public class CommentFailedPostFatalException : MindTouchFatalCallException {
        public CommentFailedPostFatalException() : base(DekiResources.FAILED_POST_COMMENT()) { }
    }

    public class CommentNotFoundException : MindTouchNotFoundException {
        public CommentNotFoundException() : base(DekiResources.COMMENT_NOT_FOUND()) { }
    }

    public class CommentPostForAnonymousDeniedException : MindTouchAccessDeniedException {
        public CommentPostForAnonymousDeniedException(string authRealm, string message) : base(authRealm, DekiResources.OPERATION_DENIED_FOR_ANONYMOUS(message)) { }
    }

    #endregion

    #region ConfigBL exceptions
    public class ConfigMissingRequiredKeyInvalidArgumentException : MindTouchInvalidCallException {
        public ConfigMissingRequiredKeyInvalidArgumentException(string key) : base(DekiResources.MISSING_REQUIRED_CONFIG_KEY(key)) { }
    }

    public class ConfigUpdateConfigSettingsInvalidArgumentException : MindTouchInvalidCallException {
        public ConfigUpdateConfigSettingsInvalidArgumentException() : base(DekiResources.ERROR_UPDATE_CONFIG_SETTINGS()) { }
    }

    public class ConfigUpdateDupeSettingException : MindTouchInvalidCallException {
        public ConfigUpdateDupeSettingException(string key) : base(DekiResources.ERROR_DUPE_CONFIG_SETTINGS(key)) { }
    }    
    #endregion

    #region DekiXmlParser exceptions
    public class DekiXmlParserInvalidContentException : MindTouchInvalidCallException {
        public DekiXmlParserInvalidContentException() : base(DekiResources.CONTENT_CANNOT_BE_PARSED()) { }
    }

    public class DekiXmlParserInvalidXPathException : MindTouchInvalidCallException {
        public DekiXmlParserInvalidXPathException() : base(DekiResources.XPATH_PARAM_INVALID()) { }
    }

    public class DekiXmlParserInfinitePageInclusionInvalidScriptException : MindTouchInvalidCallException {
        public DekiXmlParserInfinitePageInclusionInvalidScriptException() : base(DekiResources.INFINITE_PAGE_INCLUSION()) { }
    }

    public class DekiXmlParserInvalidPageFormatException : MindTouchInvalidCallException {
        public DekiXmlParserInvalidPageFormatException() : base(DekiResources.PAGE_FORMAT_INVALID()) { }
    }
    #endregion

    #region External Service Adapter exceptions
    public class ExternalServiceNotStartedFatalException : MindTouchFatalCallException {
        public ExternalServiceNotStartedFatalException(ServiceType type, string sid) : base(DekiResources.SERVICE_NOT_STARTED(type, sid)) { }
    }

    public class ExternalServiceResponseException : ResourcedMindTouchException {

        //--- Fields ---
        public readonly DreamMessage ExternalServiceResponse;

        //--- Constructors ---
        public ExternalServiceResponseException(DekiResource message, DreamMessage externalServiceResponse)
            : base(message) {
            ExternalServiceResponse = externalServiceResponse;
        }
        public ExternalServiceResponseException(DreamMessage externalServiceResponse)
            : base(null) {
            ExternalServiceResponse = externalServiceResponse;
        }
    }

    public class ExternalServiceAuthenticationDeniedException : MindTouchAccessDeniedException {
        public ExternalServiceAuthenticationDeniedException(string authRealm, string serviceDescription)
            : base(authRealm, DekiResources.AUTHENTICATION_FAILED_FOR(serviceDescription)) {
        }
    }

    public class ExternalServiceUnexpecteUsernameFatalException : MindTouchFatalCallException {
        public ExternalServiceUnexpecteUsernameFatalException(string username, string builtUsername) : base(DekiResources.UNEXPECTED_EXTERNAL_USERNAME(username, builtUsername)) { }
    }

    public class ExternalAuthResponseFatalException : MindTouchFatalCallException {
        public ExternalAuthResponseFatalException() : base(DekiResources.EXTERNAL_AUTH_BAD_RESPONSE()) { }
    }
    #endregion

    #region ExtensionBL exceptions
    public class ExtensionRemoveServiceInvalidOperationException : MindTouchInvalidCallException {
        public ExtensionRemoveServiceInvalidOperationException(XUri uri) : base(DekiResources.EXTENSION_INVALID_REMOTE_SERVICE(uri)) { }
    }

    public class ExtensionNamespaceInvalidArgumentException : MindTouchInvalidCallException {
        public ExtensionNamespaceInvalidArgumentException(string ns) : base(DekiResources.EXTENSION_INVALID_NAMESPACE(ns)) { }
    }
    #endregion

    #region FSStorage exceptions
    public class StoragePathConfigMissingInvalidArgumentException : MindTouchInvalidCallException {
        public StoragePathConfigMissingInvalidArgumentException() : base(DekiResources.PATH_CONFIG_MISSING()) { }
    }

    public class StorageNonHeadRevisionDeleteFatalException : MindTouchFatalCallException {
        public StorageNonHeadRevisionDeleteFatalException() : base(DekiResources.CAN_ONLY_DELETE_HEAD_REVISION()) { }
    }

    public class StorageDirectoryCreationFatalException : MindTouchFatalCallException {
        public StorageDirectoryCreationFatalException(string directory) : base(DekiResources.CANNOT_CREATE_FILE_DIRECTORY(directory)) { }
    }

    public class StorageFileSaveFatalException : MindTouchFatalCallException {
        public StorageFileSaveFatalException(string filename, string message) : base(DekiResources.CANNOT_SAVE_FILE_TO(filename, message)) { }
    }
    #endregion

    #region GroupBL exceptions
    public class GroupNotFoundException : MindTouchNotFoundException {
        public GroupNotFoundException(string group) : base(DekiResources.GROUP_NOT_FOUND(group)) { }
    }

    public class GroupIdNotFoundException : MindTouchNotFoundException {
        public GroupIdNotFoundException(uint? groupId) : base(DekiResources.GROUP_ID_NOT_FOUND(groupId)) { }
    }

    public class GroupIdInvalidArgumentException : MindTouchInvalidCallException {
        public GroupIdInvalidArgumentException() : base(DekiResources.GROUPID_PARAM_INVALID()) { }
    }

    public class ExternalGroupNotFoundException : MindTouchNotFoundException {
        public ExternalGroupNotFoundException(string groupName) : base(DekiResources.EXTERNAL_GROUP_NOT_FOUND(groupName)) { }
    }

    public class GroupExistsWithServiceConflictException : MindTouchConflictException {
        public GroupExistsWithServiceConflictException(string groupName, uint serviceId)
            : base(DekiResources.GROUP_EXISTS_WITH_SERVICE(groupName, serviceId)) {
        }
    }

    public class ExternalGroupRenameNotImplementedException : MindTouchNotImplementedException {
        public ExternalGroupRenameNotImplementedException() : base(DekiResources.EXTERNAL_GROUP_RENAME_NOT_ALLOWED()) { }
    }

    public class GroupCreateUpdateFatalException : MindTouchFatalCallException {
        public GroupCreateUpdateFatalException() : base(DekiResources.GROUP_CREATE_UPDATE_FAILED()) { }
    }

    public class GroupExpectedUserRootNodeInvalidArgumentException : MindTouchInvalidCallException {
        public GroupExpectedUserRootNodeInvalidArgumentException() : base(DekiResources.EXPECTED_ROOT_NODE_USERS()) { }
    }

    public class GroupServiceNotFoundFatalException : MindTouchFatalCallException {
        public GroupServiceNotFoundFatalException(uint serviceId, string groupname) : base(DekiResources.GROUP_SERVICE_NOT_FOUND(serviceId, groupname)) { }
    }

    public class ExternalGroupMemberInvalidOperationException : MindTouchInvalidCallException {
        public ExternalGroupMemberInvalidOperationException() : base(DekiResources.GROUP_EXTERNAL_CHANGE_MEMBERS()) { }
    }

    public class GroupMembersRequireSameAuthInvalidOperationException : MindTouchInvalidCallException {
        public GroupMembersRequireSameAuthInvalidOperationException() : base(DekiResources.GROUP_MEMBERS_REQUIRE_SAME_AUTH()) { }
    }

    public class GroupIdAttributeInvalidArgumentException : MindTouchInvalidCallException {
        public GroupIdAttributeInvalidArgumentException() : base(DekiResources.GROUP_ID_ATTR_INVALID()) { }
    }

    public class GroupCouldNotFindUserInvalidArgumentException : MindTouchInvalidCallException {
        public GroupCouldNotFindUserInvalidArgumentException(uint userId) : base(DekiResources.COULD_NOT_FIND_USER(userId)) { }
    }
    #endregion

    #region PageBL exceptions
    public class PageExistsConflictException : MindTouchConflictException {
        public PageExistsConflictException() : base(DekiResources.PAGE_ALREADY_EXISTS()) { }
    }

    public class PageModifiedConflictException : MindTouchConflictException {
        public PageModifiedConflictException() : base(DekiResources.PAGE_WAS_MODIFIED()) { }
    }

    public class PageEditTimeInvalidArgumentException : MindTouchInvalidCallException {
        public PageEditTimeInvalidArgumentException() : base(DekiResources.EDITTIME_PARAM_INVALID()) { }
    }

    public class PageHeadingInvalidArgumentException : MindTouchInvalidCallException {
        public PageHeadingInvalidArgumentException() : base(DekiResources.HEADING_PARAM_INVALID()) { }
    }

    public class PageFormatInvalidArgumentException : MindTouchInvalidCallException {
        public PageFormatInvalidArgumentException() : base(DekiResources.INVALID_FORMAT_GIVEN()) { }
    }

    public class PageRestrictionInfoMissingInvalidArgumentException : MindTouchInvalidCallException {
        public PageRestrictionInfoMissingInvalidArgumentException() : base(DekiResources.RESTRICTION_INFO_MISSING()) { }
    }

    public class PageRestrictionNotFoundInvalidArgumentException : MindTouchInvalidCallException {
        public PageRestrictionNotFoundInvalidArgumentException(string restriction) : base(DekiResources.RESTRICTION_NOT_FOUND(restriction)) { }
    }

    public class PageRevisionNotFoundException : MindTouchNotFoundException {
        public PageRevisionNotFoundException(int revision, ulong pageId) : base(DekiResources.COULD_NOT_FIND_REVISION(revision, pageId)) { }
    }

    public class PagePrinceExportErrorFatalException : MindTouchFatalCallException {
        public PagePrinceExportErrorFatalException(ulong pageId) : base(DekiResources.UNABLE_TO_EXPORT_PAGE_PRINCE_ERROR(pageId)) { }
    }

    public class PageDirectoryInvalidArgumentException : MindTouchInvalidCallException {
        public PageDirectoryInvalidArgumentException(string directory) : base(DekiResources.DIR_IS_NOT_VALID(directory)) { }
    }

    public class PageLanguageInvalidArgumentException : MindTouchInvalidCallException {
        public PageLanguageInvalidArgumentException() : base(DekiResources.LANGUAGE_PARAM_INVALID()) { }
    }

    public class PageEditExistingSectionInvalidOperationException : MindTouchInvalidCallException {
        public PageEditExistingSectionInvalidOperationException() : base(DekiResources.SECTION_EDIT_EXISTING_PAGES_ONLY()) { }
    }

    public class PageInvalidTitleConflictException : MindTouchConflictException {
        public PageInvalidTitleConflictException() : base(DekiResources.INVALID_TITLE()) { }
    }

    public class PageInvalidRedirectConflictException : MindTouchConflictException {
        public PageInvalidRedirectConflictException() : base(DekiResources.INVALID_REDIRECT()) { }
    }

    public class PageWithRevisionNotFoundException : MindTouchNotFoundException {
        public PageWithRevisionNotFoundException() : base(DekiResources.CANNOT_FIND_PAGE_WITH_REVISION()) { }
    }

    public class PageMoveExistingTitleConflictException : MindTouchConflictException {
        public PageMoveExistingTitleConflictException(string pagePath) : base(DekiResources.PAGE_MOVE_CONFLICT_EXISTING_TITLE(pagePath)) { }
    }

    public class PageAlreadyExistsConflictException : MindTouchConflictException {
        public PageAlreadyExistsConflictException() : base(DekiResources.PAGES_ALREADY_EXIST_AT_DEST()) { }
    }

    public class PageMoveHomepageConflictException : MindTouchConflictException {
        public PageMoveHomepageConflictException() : base(DekiResources.PAGE_MOVE_CONFLICT_HOMEPAGE()) { }
    }

    public class PageMoveTitleConflictException : MindTouchConflictException {
        public PageMoveTitleConflictException() : base(DekiResources.PAGE_MOVE_CONFLICT_TITLE()) { }
    }

    public class PageMoveTemplateConflictException : MindTouchConflictException {
        public PageMoveTemplateConflictException() : base(DekiResources.PAGE_MOVE_CONFLICT_TEMPLATE()) { }
    }

    public class PageMoveSpecialConflictException : MindTouchConflictException {
        public PageMoveSpecialConflictException() : base(DekiResources.PAGE_MOVE_CONFLICT_SPECIAL()) { }
    }

    public class PageMoveTitleEditeConflictException : MindTouchConflictException {
        public PageMoveTitleEditeConflictException(NS @namespace) : base(DekiResources.PAGE_MOVE_CONFLICT_TITLE_NOT_EDITABLE(@namespace)) { }
    }

    public class PageMoveHomepageMoveConflictException : MindTouchConflictException {
        public PageMoveHomepageMoveConflictException() : base(DekiResources.PAGE_MOVE_CONFLICT_MOVE_HOMEPAGE()) { }
    }

    public class PageMoveRootUserConflictException : MindTouchConflictException {
        public PageMoveRootUserConflictException() : base(DekiResources.PAGE_MOVE_CONFLICT_MOVE_ROOTUSER()) { }
    }

    public class PageMoveSourceNamespaceConflictException : MindTouchConflictException {
        public PageMoveSourceNamespaceConflictException(NS @namespace) : base(DekiResources.PAGE_MOVE_CONFLICT_SOURCE_NAMESPACE(@namespace)) { }
    }

    public class PageMoveDescendantConflictException : MindTouchConflictException {
        public PageMoveDescendantConflictException(string parentPagePath, string childPathPath)
            : base(DekiResources.PAGE_MOVE_CONFLICT_MOVE_TO_DESCENDANT(parentPagePath, childPathPath)) {
        }
    }

    public class PageModifyTalkConflictException : MindTouchConflictException {
        public PageModifyTalkConflictException() : base(DekiResources.CANNOT_MODIFY_TALK()) { }
    }

    public class PageModifySpecialConflictException : MindTouchConflictException {
        public PageModifySpecialConflictException() : base(DekiResources.CANNOT_MODIFY_SPECIAL_PAGES()) { }
    }

    public class PageDeleteHomepageConflictException : MindTouchConflictException {
        public PageDeleteHomepageConflictException() : base(DekiResources.HOMEPAGE_CANNOT_BE_DELETED()) { }
    }

    public class PageCreateTalkConflictException : MindTouchConflictException {
        public PageCreateTalkConflictException() : base(DekiResources.CANNOT_CREATE_TALK()) { }
    }

    public class PageHiddenRevisionMustBeUnhiddenConflictException : MindTouchConflictException {
        public PageHiddenRevisionMustBeUnhiddenConflictException() : base(DekiResources.PAGE_HIDDEN_REVISION_MUST_BE_UNHIDDEN()) { }
    }

    public class PageIdParameterInvalidArgumentException : MindTouchInvalidCallException {
        public PageIdParameterInvalidArgumentException() : base(DekiResources.PAGE_ID_PARAM_INVALID()) { }
    }

    public class PageNotFoundException : MindTouchNotFoundException {
        public PageNotFoundException() : base(DekiResources.CANNOT_FIND_REQUESTED_PAGE()) { }
    }

    public class PageInvalidDocumentException : MindTouchInvalidCallException {
        public PageInvalidDocumentException() : base(DekiResources.INVALID_POSTED_DOCUMENT()) { }
    }

    public class PageIdInvalidArgumentException : MindTouchInvalidCallException {
        public PageIdInvalidArgumentException() : base(DekiResources.INVALID_PAGE_ID()) { }
    }

    public class PageReltoTalkInvalidOperationException : MindTouchInvalidCallException {
        public PageReltoTalkInvalidOperationException() : base(DekiResources.CANNOT_RELTO_TALK()) { }
    }

    public class PageXmlParseInvalidDocumentException : MindTouchInvalidCallException {
        public PageXmlParseInvalidDocumentException(string message) : base(DekiResources.UNABLE_TO_PARSE_PAGES_FROM_XML(message)) { }
    }
    #endregion

    #region PageArchiveBL exceptions
    public class PageArchiveLogicNotFoundException : MindTouchNotFoundException {
        public PageArchiveLogicNotFoundException(uint pageId) : base(DekiResources.RESTORE_PAGE_ID_NOT_FOUND(pageId)) { }
    }

    public class PageArchiveRestoreNamedPageConflictException : MindTouchConflictException {
        public PageArchiveRestoreNamedPageConflictException(string conflictTitles) : base(DekiResources.CANNOT_RESTORE_PAGE_NAMED(conflictTitles)) { }
    }

    public class PageArchiveBadTransactionFatalException : MindTouchFatalCallException {
        public PageArchiveBadTransactionFatalException(uint transactionId, uint pageId) : base(DekiResources.PAGEARCHIVE_BAD_TRANSACTION(transactionId, pageId)) { }
    }
    #endregion

    #region PermissionsBL exceptions
    public class PermissionsNotAllowedForbiddenException : MindTouchForbiddenException {
        public PermissionsNotAllowedForbiddenException(string pagePath, NS pageNamespace) : base(DekiResources.PERMISSIONS_NOT_ALLOWED_ON(pagePath, pageNamespace)) { }
    }

    public class PermissionsNoAdminForImpersonationFatalException : MindTouchFatalCallException {
        public PermissionsNoAdminForImpersonationFatalException(string users) : base(DekiResources.PERMISSIONS_NO_ADMIN_FOR_IMPERSONATION(users)) { }
    }

    public class PermissionsUserWouldBeLockedOutOfPageInvalidOperationException : MindTouchInvalidCallException {
        public PermissionsUserWouldBeLockedOutOfPageInvalidOperationException() : base(DekiResources.USER_WOULD_BE_LOCKED_OUT_OF_PAGE()) { }
    }

    public class PermissionsRetrieveRoleInvalidArgumentException : MindTouchInvalidCallException {
        public PermissionsRetrieveRoleInvalidArgumentException(string role) : base(DekiResources.CANNOT_RETRIEVE_REQUIRED_ROLE(role)) { }
    }

    public class PermissionsDuplicateRoleInvalidArgumentException : MindTouchInvalidCallException {
        public PermissionsDuplicateRoleInvalidArgumentException() : base(DekiResources.DUPLICATE_ROLE()) { }
    }

    public class PermissionsDuplicateGrantInvalidArgumentException : MindTouchInvalidCallException {
        public PermissionsDuplicateGrantInvalidArgumentException() : base(DekiResources.DUPLICATE_GRANT_FOR_USER_GROUP()) { }
    }

    public class PermissionsNoUserWithIdInvalidArgumentException : MindTouchInvalidCallException {
        public PermissionsNoUserWithIdInvalidArgumentException(uint userId) : base(DekiResources.CANNOT_FIND_USER_WITH_ID(userId)) { }
    }

    public class PermissionsNoGroupWithIdInvalidArgumentException : MindTouchInvalidCallException {
        public PermissionsNoGroupWithIdInvalidArgumentException(uint groupId) : base(DekiResources.CANNOT_FIND_GROUP_WITH_ID(groupId)) { }
    }

    public class PermissionsForbiddenException : MindTouchForbiddenException {
        public PermissionsForbiddenException(DekiResource error) : base(error) { }
    }
    public class PermissionsDeniedException : MindTouchAccessDeniedException {
        public PermissionsDeniedException(string authRealm, DekiResource error)
            : base(authRealm, DekiResources.OPERATION_DENIED_FOR_ANONYMOUS(error)) {
        }
    }

    public class PermissionsInvalidRoleNameInvalidArgumentException : MindTouchInvalidCallException {
        public PermissionsInvalidRoleNameInvalidArgumentException() : base(DekiResources.ROLE_NAME_PARAM_INVALID()) { }
    }

    public class PermissionsGrantParseInvalidArgumentException : MindTouchInvalidCallException {
        public PermissionsGrantParseInvalidArgumentException(string message) : base(DekiResources.CANNOT_PARSE_GRANTS(message)) { }
    }

    public class PermissionsUserOrGroupIDNotGivenInvalidArgumentException : MindTouchArgumentException {
        public PermissionsUserOrGroupIDNotGivenInvalidArgumentException() : base(DekiResources.USER_OR_GROUP_ID_NOT_GIVEN()) { }
    }

    public class PermissionsRoleNotGivenInvalidArgumentException : MindTouchArgumentException {
        public PermissionsRoleNotGivenInvalidArgumentException() : base(DekiResources.ROLE_NOT_GIVEN()) { }
    }

    public class PermissionsUnrecognizedRoleInvalidArgumentRoleException : MindTouchArgumentException {
        public PermissionsUnrecognizedRoleInvalidArgumentRoleException() : base(DekiResources.ROLE_UNRECOGNIZED()) { }
    }

    public class PermissionsExpiryParseInvalidArgumentException : MindTouchArgumentException {
        public PermissionsExpiryParseInvalidArgumentException() : base(DekiResources.CANNOT_PARSE_EXPIRY()) { }
    }
    #endregion

    #region PropertyBL exceptions
    public class PropertyEditNonexistingConflictException : MindTouchConflictException {
        public PropertyEditNonexistingConflictException() : base(DekiResources.PROPERTY_EDIT_NONEXISTING_CONFLICT()) { }
    }

    public class PropertyAbortOnExistsConflictException : MindTouchConflictException {
        public PropertyAbortOnExistsConflictException(string property) : base(DekiResources.PROPERTY_EXISTS_CONFLICT(property)) { }
    }

    public class PropertyCreateMissingSlugInvalidOperationException : MindTouchInvalidCallException {
        public PropertyCreateMissingSlugInvalidOperationException() : base(DekiResources.PROPERTY_CREATE_MISSING_SLUG()) { }
    }

    public class PropertyExistsConflictException : MindTouchConflictException {
        public PropertyExistsConflictException(string property) : base(DekiResources.PROPERTY_ALREADY_EXISTS(property)) { }
    }

    public class PropertyDuplicateInvalidOperationException : MindTouchInvalidCallException {
        public PropertyDuplicateInvalidOperationException(string property) : base(DekiResources.PROPERTY_DUPE_EXCEPTION(property)) { }
    }

    public class PropertyDeleteDoesNotExistInvalidArgumentException : MindTouchInvalidCallException {
        public PropertyDeleteDoesNotExistInvalidArgumentException(string property) : base(DekiResources.PROPERTY_DOESNT_EXIST_DELETE(property)) { }
    }

    public class PropertyUnexpectedEtagConflictException : MindTouchConflictException {
        public PropertyUnexpectedEtagConflictException() : base(DekiResources.PROPERTY_UNEXPECTED_ETAG()) { }
    }

    public class PropertyMimtypeInvalidArgumentException : MindTouchInvalidCallException {
        public PropertyMimtypeInvalidArgumentException(string property, string mimetype) : base(DekiResources.PROPERTY_INVALID_MIMETYPE(property, mimetype)) { }
    }
    #endregion

    #region RatingBL exceptions
    public class RatingInvalidArgumentException : MindTouchInvalidCallException {
        public RatingInvalidArgumentException() : base(DekiResources.RATING_INVALID_SCORE()) { }
    }

    public class RatingForAnonymousDeniedException : MindTouchAccessDeniedException {
        public RatingForAnonymousDeniedException(string authRealm, string message) : base(authRealm, DekiResources.OPERATION_DENIED_FOR_ANONYMOUS(message)) { }
    }
    #endregion

    #region ResourceBL exceptions
    public class ResourceEtagConflictException : MindTouchConflictException {
        public ResourceEtagConflictException(string etag, uint resourceId) : base(DekiResources.RESOURCE_ETAG_CONFLICT(etag, resourceId)) { }
    }

    public class ResourceEtagNotHeadInvalidArgumentException : MindTouchInvalidCallException {
        public ResourceEtagNotHeadInvalidArgumentException(uint resourceId, int revision) : base(DekiResources.RESOURCE_ETAG_NOT_HEAD(resourceId, revision)) { }
    }
    #endregion

    #region SearchBL exceptions
    public class SearchIndexDeleteFatalException : MindTouchFatalCallException {
        public SearchIndexDeleteFatalException(string errorMessage) : base(DekiResources.ERROR_DELETING_INDEX(errorMessage)) { }
    }
    #endregion

    #region ServiceBL exceptions
    public class ServiceNotFoundException : MindTouchNotFoundException {
        public ServiceNotFoundException(string identifier) : base(DekiResources.SERVICE_NOT_FOUND(identifier)) { }
        public ServiceNotFoundException(uint serviceId) : base(DekiResources.SERVICE_NOT_FOUND(serviceId)) { }
    }

    public class ServiceSettingsInvalidArgumentException : MindTouchInvalidCallException {
        public ServiceSettingsInvalidArgumentException() : base(DekiResources.SERVICE_CHECK_SETTINGS()) { }
    }

    public class ServiceSIDExpectedFatalException : MindTouchFatalCallException {
        public ServiceSIDExpectedFatalException(string sid) : base(DekiResources.EXPECTED_SERVICE_TO_HAVE_SID(sid)) { }
    }

    public class ServiceAdministrationNotImplementedExceptionException : MindTouchNotImplementedException {
        public ServiceAdministrationNotImplementedExceptionException() : base(DekiResources.SERVICE_ADMINISTRATION_DISABLED()) { }
    }

    public class ServiceMissingCreateSIDInvalidOperationException : MindTouchInvalidCallException {
        public ServiceMissingCreateSIDInvalidOperationException() : base(DekiResources.SERVICE_CREATE_SID_MISSING()) { }
    }

    public class ServiceMissingCreateTypeInvalidArgumentException : MindTouchInvalidCallException {
        public ServiceMissingCreateTypeInvalidArgumentException() : base(DekiResources.SERVICE_CREATE_TYPE_MISSING()) { }
    }

    public class ServiceInvalidUpdateTypeInvalidArgumentException : MindTouchInvalidCallException {
        public ServiceInvalidUpdateTypeInvalidArgumentException() : base(DekiResources.SERVICE_UPDATE_TYPE_INVALID()) { }
    }

    public class ServiceUnexpectedInitInvalidOperationException : MindTouchInvalidCallException {
        public ServiceUnexpectedInitInvalidOperationException() : base(DekiResources.SERVICE_UNEXPECTED_INIT()) { }
    }

    public class ServiceMissingDescriptionInvalidArgumentException : MindTouchInvalidCallException {
        public ServiceMissingDescriptionInvalidArgumentException() : base(DekiResources.SERVICE_MISSING_DESCRIPTION()) { }
    }

    public class ServiceInvalidStatusInvalidArgumentException : MindTouchInvalidCallException {
        public ServiceInvalidStatusInvalidArgumentException() : base(DekiResources.SERVICE_INVALID_STATUS()) { }
    }

    public class ServiceCannotDeleteAuthInvalidOperationException : MindTouchInvalidCallException {
        public ServiceCannotDeleteAuthInvalidOperationException() : base(DekiResources.SERVICE_CANNOT_DELETE_AUTH()) { }
    }

    public class ServiceCannotModifyBuiltInAuthInvalidOperationException : MindTouchInvalidCallException {
        public ServiceCannotModifyBuiltInAuthInvalidOperationException() : base(DekiResources.SERVICE_CANNOT_MODIFY_AUTH()) { }
    }

    public class ServiceCannotSetLocalUriInvalidOperationException : MindTouchInvalidCallException {
        public ServiceCannotSetLocalUriInvalidOperationException() : base(DekiResources.SERVICE_CANNOT_SET_LOCAL_URI()) { }
    }
    #endregion

    #region SiteBL exceptions
    public class SiteNoAdminFatalException : MindTouchFatalCallException {
        public SiteNoAdminFatalException() : base(DekiResources.CANNOT_RETRIEVE_ADMIN_ACCOUNT()) { }
    }

    public class SiteConflictException : MindTouchConflictException {
        public SiteConflictException(DekiResource resource) : base(resource) { }
    }

    public class SiteMustBeLoggedInForbiddenException : MindTouchForbiddenException {
        public SiteMustBeLoggedInForbiddenException() : base(DekiResources.MUST_BE_LOGGED_IN()) { }
    }

    public class SiteNoSuchLocalizationResourceNotFoundException : MindTouchNotFoundException {
        public SiteNoSuchLocalizationResourceNotFoundException(string resource) : base(DekiResources.ERROR_NO_SUCH_RESOURCE(resource)) { }
    }

    public class SiteExpectedXmlContentTypeInvalidArgumentException : MindTouchInvalidCallException {
        public SiteExpectedXmlContentTypeInvalidArgumentException() : base(DekiResources.EXPECTED_XML_CONTENT_TYPE()) { }
    }

    public class SiteImageMimetypeInvalidArgumentException : MindTouchInvalidCallException {
        public SiteImageMimetypeInvalidArgumentException() : base(DekiResources.EXPECTED_IMAGE_MIMETYPE()) { }
    }

    public class SiteUnableToProcessLogoInvalidOperationException : MindTouchInvalidCallException {
        public SiteUnableToProcessLogoInvalidOperationException() : base(DekiResources.CANNOT_PROCESS_LOGO_IMAGE()) { }
    }

    public class SiteRoleNameNotFoundException : MindTouchNotFoundException {
        public SiteRoleNameNotFoundException(string role) : base(DekiResources.ROLE_NAME_NOT_FOUND(role)) { }
    }

    public class SiteRoleIdInvalidArgumentException : MindTouchInvalidCallException {
        public SiteRoleIdInvalidArgumentException() : base(DekiResources.ROLEID_PARAM_INVALID()) { }
    }

    public class SiteRoleIdNotFoundException : MindTouchNotFoundException {
        public SiteRoleIdNotFoundException(uint roleId) : base(DekiResources.ROLE_ID_NOT_FOUND(roleId)) { }
    }

    public class SiteExportRedirectInvalidOperationException : MindTouchInvalidCallException {
        public SiteExportRedirectInvalidOperationException() : base(DekiResources.INVALID_REDIRECT_OPERATION()) { }
    }

    public class SiteImportUndefinedNameInvalidArgumentException : MindTouchInvalidCallException {
        public SiteImportUndefinedNameInvalidArgumentException(string name) : base(DekiResources.UNDEFINED_NAME(name)) { }
    }
    #endregion

    #region TagBL exceptions
    public class TagNotFoundException : MindTouchNotFoundException {
        public TagNotFoundException() : base(DekiResources.CANNOT_FIND_REQUESTED_TAG()) { }
    }

    public class TagInvalidArgumentException : MindTouchInvalidCallException {
        public TagInvalidArgumentException(string name) : base(DekiResources.TAG_INVALID(name)) { }
    }
    #endregion

    #region LicenseBL exceptions
    public class MindTouchLicenseUpdateInvalidArgumentException : MindTouchInvalidCallException {
        public MindTouchLicenseUpdateInvalidArgumentException() : base(DekiResources.LICENSE_UPDATE_INVALID()) { }
    }

    public class MindTouchLicenseUserCreationForbiddenException : MindTouchForbiddenException {
        public MindTouchLicenseUserCreationForbiddenException() : base(DekiResources.LICENSE_LIMIT_USER_CREATION()) { }
    }

    public class MindTouchLicenseExpiredInvalidOperationException : MindTouchInvalidCallException {
        public MindTouchLicenseExpiredInvalidOperationException(DateTime expiration) : base(DekiResources.LICENSE_UPDATE_EXPIRED(expiration)) { }
    }

    public class MindTouchLicenseInvalidOperationForbiddenException : MindTouchForbiddenException {
        public MindTouchLicenseInvalidOperationForbiddenException(string operation) : base(DekiResources.LICENSE_OPERATION_NOT_ALLOWED(operation)) { }
    }

    public class MindTouchLicenseTransitionForbiddenLicenseTransitionException : MindTouchForbiddenException {
        public MindTouchLicenseTransitionForbiddenLicenseTransitionException(LicenseStateType currentState, LicenseStateType proposedState)
            : base(DekiResources.LICENSE_TRANSITION_INVALID(currentState, proposedState)) {
        }
    }

    public class MindTouchLicenseTooManyUsersForbiddenException : MindTouchForbiddenException {
        public MindTouchLicenseTooManyUsersForbiddenException(uint currentActiveUsers, uint maxUsers, uint userDelta)
            : base(DekiResources.LICENSE_LIMIT_TOO_MANY_USERS(currentActiveUsers, maxUsers, userDelta)) {
        }
    }

    public class MindTouchLicenseNoNewUserForbiddenException : MindTouchForbiddenException {
        public MindTouchLicenseNoNewUserForbiddenException(LicenseStateType currentLicenseState)
            : base(DekiResources.LICENSE_NO_NEW_USER_CREATION(currentLicenseState)) {
        }
    }

    public class MindTouchLicenseInsufficientSeatsException : MindTouchConflictException {
        public MindTouchLicenseInsufficientSeatsException(int seatsAllowed)
            : base(DekiResources.LICENSE_INSUFFICIENT_SEATS(seatsAllowed)) {
        }
    }

    public class MindTouchLicenseAnonymousSeat : MindTouchConflictException {
        public MindTouchLicenseAnonymousSeat()
            : base(DekiResources.LICENSE_ANONYMOUS_SEAT()) {
        }
    }

    public class MindTouchLicenseRemovalFromSiteOwnerException : MindTouchConflictException {
        public MindTouchLicenseRemovalFromSiteOwnerException()
            : base(DekiResources.LICENSE_SEAT_REMOVAL_FROM_OWNER()) {
        }
    }

    public class MindTouchLicenseNoSiteOwnerDefinedException : MindTouchConflictException {
        public MindTouchLicenseNoSiteOwnerDefinedException()
            : base(DekiResources.LICENSE_NO_SITE_OWNER_DEFINED()) {
        }
    }

    public class MindTouchLicenseUploadByNonOwnerException : MindTouchConflictException {
        
        //--- Fields ---
        public readonly string Username;
        public readonly ulong Userid;

        //--- Constructors ---
        public MindTouchLicenseUploadByNonOwnerException(string username, ulong userid)
            : base(DekiResources.LICENSE_UPLOAD_BY_NON_OWNER(username, userid)) {
            Username = username;
            Userid = userid;
        }
    }

    public class MindTouchLicenseSeatLicensingNotInUseException : MindTouchConflictException {
        public MindTouchLicenseSeatLicensingNotInUseException()
            : base(DekiResources.LICENSE_SEAT_LICENSING_NOT_IN_USE()) {
        }
    }

    public class MindTouchRemoteLicenseFailedException : Exception {
        public MindTouchRemoteLicenseFailedException() : base("Remote license failed to validate, preventing instance from running") {}
    }

    public class MindTouchInvalidLicenseStateException : Exception {
        public MindTouchInvalidLicenseStateException() : base("Unable to determine license state at startup, preventing instance from running") {}
    }

    #endregion

    #region Remote Instance License exceptions
    public class MindTouchRemoteLicenseModificationException : MindTouchForbiddenException {
        public MindTouchRemoteLicenseModificationException() : base(DekiResources.REMOTE_LICENSE_MODIFICATION_FORBIDDEN()) { }
    }

    public class MindTouchRemoteLicenseInvalidException : MindTouchFatalCallException {
        public MindTouchRemoteLicenseInvalidException(): base(DekiResources.REMOTE_LICENSE_MISSING()) { }
    }
    #endregion

    #region UserBL exceptions
    public class NoSuchUserUsePostNotFoundException : MindTouchNotFoundException {
        public NoSuchUserUsePostNotFoundException() : base(DekiResources.GIVEN_USER_NOT_FOUND_USE_POST()) { }
    }

    public class UserNotFoundException : MindTouchNotFoundException {
        public UserNotFoundException() : base(DekiResources.GIVEN_USER_NOT_FOUND()) { }
    }

    public class UserOperationListInvalidArgumentException : MindTouchInvalidCallException {
        public UserOperationListInvalidArgumentException() : base(DekiResources.INVALID_OPERATION_LIST()) { }
    }

    public class UserExpectedRootNodePagesInvalidDocumentException : MindTouchInvalidCallException {
        public UserExpectedRootNodePagesInvalidDocumentException() : base(DekiResources.EXPECTED_ROOT_NODE_PAGES()) { }
    }

    public class UserNewPasswordNotProvidedInvalidArgumentException : MindTouchInvalidCallException {
        public UserNewPasswordNotProvidedInvalidArgumentException() : base(DekiResources.NEW_PASSWORD_NOT_PROVIDED()) { }
    }

    public class UserNewPasswordTooShortInvalidArgumentException : MindTouchInvalidCallException {
        public UserNewPasswordTooShortInvalidArgumentException() : base(DekiResources.NEW_PASSWORD_TOO_SHORT()) { }
    }

    public class UserCanOnlyChangeLocalUserPasswordInvalidOperationException : MindTouchInvalidCallException {
        public UserCanOnlyChangeLocalUserPasswordInvalidOperationException() : base(DekiResources.PASSWORD_CHANGE_LOCAL_ONLY()) { }
    }

    public class UserCannotChangeAnonPasswordInvalidOperationException : MindTouchInvalidCallException {
        public UserCannotChangeAnonPasswordInvalidOperationException() : base(DekiResources.CANNOT_CHANGE_ANON_PASSWORD()) { }
    }

    public class UserCurrentPasswordIncorrectForbiddenException : MindTouchForbiddenException {
        public UserCurrentPasswordIncorrectForbiddenException() : base(DekiResources.CURRENTPASSWORD_DOES_NOT_MATCH()) { }
    }

    public class UserMustBeTargetOrAdminForbiddenException : MindTouchForbiddenException {
        public UserMustBeTargetOrAdminForbiddenException() : base(DekiResources.MUST_BE_TARGET_USER_OR_ADMIN()) { }
    }

    public class UserCannotChangeOwnAltPasswordInvalidOperationException : MindTouchInvalidCallException {
        public UserCannotChangeOwnAltPasswordInvalidOperationException() : base(DekiResources.CANNOT_CHANGE_OWN_ALT_PASSWORD()) { }
    }

    public class UserIdInvalidArgumentException : MindTouchInvalidCallException {
        public UserIdInvalidArgumentException() : base(DekiResources.USERID_PARAM_INVALID()) { }
    }

    public class UserValidationInvalidOperationException : MindTouchInvalidCallException {
        public UserValidationInvalidOperationException(string username) : base(DekiResources.USER_VALIDATION_FAILED(username)) { }
    }

    public class UserPageFilterVerboseNotAllowedException : MindTouchInvalidCallException {
        public UserPageFilterVerboseNotAllowedException() : base(DekiResources.USER_PAGE_FILTER_VERBOSE_NOT_ALLOWED()) { }
    }

    public class UserPageFilterInvalidInputException : MindTouchInvalidCallException {
        public UserPageFilterInvalidInputException() : base(DekiResources.USER_PAGE_FILTER_INVALID_INPUT()) { }
    }

    public class ExternalUserPasswordInvalidOperationException : MindTouchInvalidCallException {
        public ExternalUserPasswordInvalidOperationException() : base(DekiResources.CANNOT_SET_EXTERNAL_ACCOUNT_PASSWORD()) { }
    }

    public class ExternalUserNotFoundException : MindTouchNotFoundException {
        public ExternalUserNotFoundException(string username) : base(DekiResources.EXTERNAL_USER_NOT_FOUND(username)) { }
    }

    public class ExternalUserExistsConflictException : MindTouchConflictException {
        public ExternalUserExistsConflictException(string externalAccount, string user, uint serviceId) : base(DekiResources.USER_EXISTS_WITH_EXTERNAL_NAME(externalAccount, user, serviceId)) { }
    }

    public class UserWithIdExistsConflictException : MindTouchConflictException {
        public UserWithIdExistsConflictException(string user, uint userId) : base(DekiResources.USER_EXISTS_WITH_ID(user, userId)) { }
    }

    public class UserIdNotFoundException : MindTouchNotFoundException {
        public UserIdNotFoundException(uint userId) : base(DekiResources.USER_ID_NOT_FOUND(userId)) { }
    }

    public class UserUsePutToChangePasswordInvalidOperationException : MindTouchInvalidCallException {
        public UserUsePutToChangePasswordInvalidOperationException() : base(DekiResources.USE_PUT_TO_CHANGE_PASSWORDS()) { }
    }

    public class UserAnonymousEditInvalidOperationException : MindTouchInvalidCallException {
        public UserAnonymousEditInvalidOperationException() : base(DekiResources.ANONYMOUS_USER_EDIT()) { }
    }

    public class UserOwnerDeactivationConflict : MindTouchConflictException {
        public UserOwnerDeactivationConflict() : base(DekiResources.USER_OWNER_DEACTIVATION_CONFLICT()) { } 
    }

    public class UserAnonymousDeactivationInvalidOperationException : MindTouchInvalidCallException {
        public UserAnonymousDeactivationInvalidOperationException() : base(DekiResources.DEACTIVATE_ANONYMOUS_NOT_ALLOWED()) { }
    }

    public class UserAuthChangeFatalException : MindTouchFatalCallException {
        public UserAuthChangeFatalException() : base(DekiResources.USER_AUTHSERVICE_CHANGE_FAIL()) { }
    }

    public class ExternalUserRenameNotImplementedExceptionException : MindTouchNotImplementedException {
        public ExternalUserRenameNotImplementedExceptionException() : base(DekiResources.EXTERNAL_USER_RENAME_NOT_ALLOWED()) { }
    }

    public class UserHomepageRenameConflictException : MindTouchConflictException {
        public UserHomepageRenameConflictException() : base(DekiResources.USER_RENAME_HOMEPAGE_CONFLICT()) { }
    }

    public class UserParameterInvalidArgumentException : MindTouchInvalidCallException {
        public UserParameterInvalidArgumentException() : base(DekiResources.USERNAME_PARAM_INVALID()) { }
    }

    public class UserStatusAttrInvalidArgumentException : MindTouchInvalidCallException {
        public UserStatusAttrInvalidArgumentException() : base(DekiResources.USER_STATUS_ATTR_INVALID()) { }
    }

    public class UserTimezoneInvalidArgumentException : MindTouchInvalidCallException {
        public UserTimezoneInvalidArgumentException() : base(DekiResources.INVALID_TIMEZONE_VALUE()) { }
    }

    public class UserInvalidLanguageException : MindTouchInvalidCallException {
        public UserInvalidLanguageException() : base(DekiResources.INVALID_LANGUAGE_VALUE()) { }
    }

    public class InvalidIPAddressException : MindTouchInvalidCallException {
        public InvalidIPAddressException(string ipAddress) : base(DekiResources.BANNING_INVALID_IP_ADDRESS(ipAddress)) { }
    }

    public class LoopbackIPAddressException : MindTouchForbiddenException {
        public LoopbackIPAddressException(string ipAddress) : base(DekiResources.BANNING_LOOPBACK_IP_ADDRESS(ipAddress)) { }
    }
    #endregion
}

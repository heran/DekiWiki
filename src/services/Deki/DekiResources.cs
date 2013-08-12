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
using System.Globalization;
using MindTouch.Deki.Logic;
using MindTouch.Deki.Search;
using MindTouch.Dream;
using System.Linq;

namespace MindTouch.Deki {

    // ReSharper disable InconsistentNaming
    public sealed class DekiResources {

        // --- Constants ---
        public static DekiResource DEFAULT_SITE_NAME() { return new DekiResource("System.API.default-site-name"); }
        public static DekiResource DEL_WITH_CHILDREN_PLACEHOLDER() { return new DekiResource("System.API.DekiScript.page-placeholder-for-children"); }
        public static DekiResource EMPTY_PARENT_ARTICLE_TEXT() { return new DekiResource("System.API.page-generated-for-subpage"); }
        public static DekiResource FILE_ADDED(string filename) { return new DekiResource("System.API.added-file", filename); }
        public static DekiResource FILE_MOVED_FROM(string filename, string pagePath) { return new DekiResource("System.API.moved-file-from", filename, pagePath); }
        public static DekiResource FILE_MOVED_TO(string filename, string pagePath) { return new DekiResource("System.API.moved-file-to", filename, pagePath); }
        public static DekiResource FILE_RENAMED_TO(string sourcename, string targetname) { return new DekiResource("System.API.renamed-file-to", sourcename, targetname); }
        public static DekiResource FILE_REMOVED(string filename) { return new DekiResource("System.API.removed-file", filename); }
        public static DekiResource FILE_RESTORED(string filename) { return new DekiResource("System.API.restored-file", filename); }
        //public static DekiResource FILE_DESCRIPTION_CHANGED() { return new DekiResource("System.API.user-changed-file-description"); }
        public static DekiResource GRANT_ADDED(string entityname, string rolename) { return new DekiResource("System.API.user-grant-added", entityname, rolename.ToLowerInvariant()); }
        public static DekiResource GRANT_REMOVED(string entityname, string rolename) { return new DekiResource("System.API.user-grant-removed", entityname, rolename.ToLowerInvariant()); }
        public static DekiResource GRANT_REMOVED_ALL() { return new DekiResource("System.API.user-grant-removed-all"); }
        public static DekiResource MAIN_PAGE() { return new DekiResource("System.API.main-page"); }
        //public static DekiResource MISSING_ARTICLE() { return new DekiResource("System.API.Error.missing-article"); }
        //public static DekiResource NEW_ARTICLE_TEXT() { return new DekiResource("System.API.new-article-text"); }
        public static DekiResource NO_ARTICLE_TEXT() { return new DekiResource("System.API.no-text-in-page"); }
        public static DekiResource NO_HEADINGS() { return new DekiResource("System.API.no-headers"); }
        public static DekiResource NO_TOC_HERE() { return new DekiResource("System.API.page-has-no-toc"); }
        public static DekiResource ONE_MOVED_TO_TWO(string sourcePath, string targetTitle) { return new DekiResource("System.API.one-moved-to-two", sourcePath, targetTitle); }
        public static DekiResource REVERTED(string pagePath, ulong revision, DateTime timestamp) { return new DekiResource("System.API.reverted-to-earlier-version", pagePath, revision, timestamp); }
        public static DekiResource USER_ADDED(string username) { return new DekiResource("System.API.user-added", username); }
        //public static DekiResource USER_INVITED() { return new DekiResource("System.API.user-invited-for-days"); }
        public static DekiResource TABLE_OF_CONTENTS() { return new DekiResource("System.API.table-of-contents"); }
        public static DekiResource AND() { return new DekiResource("System.API.and"); }
        public static DekiResource EDIT_SUMMARY_ONE(string authorlist, int editTotal) { return new DekiResource("System.API.edited-once-by", authorlist, editTotal); }
        public static DekiResource EDIT_SUMMARY_TWO(string authorlist, int editTotal) { return new DekiResource("System.API.edited-twice-by", authorlist, editTotal); }
        public static DekiResource EDIT_SUMMARY_MANY(string authorlist, int editTotal) { return new DekiResource("System.API.edited-times-by", authorlist, editTotal); }
        public static DekiResource EDIT_MULTIPLE() { return new DekiResource("System.API.edited-multiple"); }
        public static DekiResource NEW_PAGE() { return new DekiResource("System.API.new-page"); }
        public static DekiResource REMOVE_FROM_WATCHLIST() { return new DekiResource("System.API.remove-from-watchlist"); }
        public static DekiResource ADD_TO_WATCHLIST() { return new DekiResource("System.API.add-to-watchlist"); }
        public static DekiResource WHATS_NEW(string sitename) { return new DekiResource("System.API.whats-new", sitename); }
        public static DekiResource PAGE_NEWS(string pagename) { return new DekiResource("System.API.page-changes", pagename); }
        public static DekiResource USER_NEWS(string username) { return new DekiResource("System.API.user-contributions", username); }
        public static DekiResource USER_FAVORITES(string username) { return new DekiResource("System.API.user-favorites", username); }
        public static DekiResource PAGES_EQUAL() { return new DekiResource("System.API.page-versions-identical"); }
        public static DekiResource PAGE_DIFF_ERROR(string name, Exception e) { return new DekiResource("System.API.Error.page-diff", name, e); }
        public static DekiResource PAGE_DIFF_TOO_LARGE() { return new DekiResource("System.API.Error.page-diff-too-large"); }
        public static DekiResource MORE_DOT_DOT_DOT() { return new DekiResource("System.API.more-dot-dot-dot"); }
        public static DekiResource REDIRECTED_TO(string link) { return new DekiResource("System.API.page-content-located-at", link); }
        public static DekiResource REDIRECTED_TO_BROKEN(string link) { return new DekiResource("System.API.page-redirect-no-longer-exists", link); }
        public static DekiResource PAGE_DIFF_SUMMARY(int added, int removed) { return new DekiResource("System.API.page-diff-added-removed", added, removed); }
        public static DekiResource PAGE_DIFF_SUMMARY_ADDED(int added) { return new DekiResource("System.API.page-diff-added", added); }
        public static DekiResource PAGE_DIFF_SUMMARY_NOT_VISIBLE() { return new DekiResource("System.API.page-diff-no-visible-changes"); }
        public static DekiResource PAGE_DIFF_SUMMARY_NOTHING() { return new DekiResource("System.API.page-diff-no-changes"); }
        public static DekiResource PAGE_DIFF_SUMMARY_REMOVED(int removed) { return new DekiResource("System.API.page-diff-words-removed", removed); }
        public static DekiResource PAGE_DIFF_OTHER_CHANGES() { return new DekiResource("System.API.page-diff-other-changes"); }
        public static DekiResource PAGE_DIFF_NOTHING() { return new DekiResource("System.API.page-diff-nothing"); }
        public static DekiResource PAGE_DISPLAYNAME_CHANGED(string displayname) { return new DekiResource("System.API.page-displayname-changed", displayname); }
        public static DekiResource PAGE_DISPLAYNAME_RESET() { return new DekiResource("System.API.page-displayname-reset"); }
        public static DekiResource PAGE_CONTENTTYPE_CHANGED(string contentType) { return new DekiResource("System.API.page-content-type-changed", contentType); }
        public static DekiResource PAGE_CREATED() { return new DekiResource("System.API.page-created"); }
        public static DekiResource PAGE_LANGUAGE_CHANGED(string nativeName) { return new DekiResource("System.API.page-language-changed", nativeName); }
        public static DekiResource LOGIN_EXTERNAL_USER_CONFLICT(string authserviceDescription) { return new DekiResource("System.API.Error.user-name-exists-provider", authserviceDescription); }
        public static DekiResource LOGIN_EXTERNAL_USER_CONFLICT_UNKNOWN() { return new DekiResource("System.API.Error.user-name-exists"); }
        //public static DekiResource UNSUPPORTED_TYPE() { return new DekiResource("System.API.Error.unsupported-type"); }
        //public static DekiResource BAD_TYPE() { return new DekiResource("System.API.Error.bad-type"); }
        //public static DekiResource PARSER_ERROR() { return new DekiResource("System.API.Error.parser-details"); }
        public static DekiResource UNDEFINED_NAME(string name) { return new DekiResource("System.API.Error.reference-to-undefined-name", name); }
        //public static DekiResource INVOKE_ERROR() { return new DekiResource("System.API.Error.function-failed"); }
        public static DekiResource DELETED_ARTICLE(string pagePath) { return new DekiResource("System.API.deleted-article", pagePath); }
        public static DekiResource SERVICE_CHECK_SETTINGS() { return new DekiResource("System.API.check-service-settings"); }
        public static DekiResource UNDELETED_ARTICLE(string pagePath) { return new DekiResource("System.API.restored-article", pagePath); }

        // Note (arnec): the two args were in the original code. Legacy requirement for some localized versions?
        public static DekiResource RESTORE_ATTACHMENT_NEW_PAGE_TEXT() { return new DekiResource("System.API.page-created-restored-attachment", DekiMimeType.DEKI_TEXT, null); }
        public static DekiResource OR() { return new DekiResource("System.API.or"); }
        public static DekiResource RESTRICT_MESSAGE() { return new DekiResource("System.API.page-is-restricted"); }
        public static DekiResource PAGE_MOVE_CONFLICT_HOMEPAGE() { return new DekiResource("System.API.Error.cannot-move-to-home-page"); }
        public static DekiResource PAGE_MOVE_CONFLICT_EXISTING_TITLE(string pagePath) { return new DekiResource("System.API.Error.title-conflicts-existing-title", pagePath); }
        public static DekiResource PAGE_MOVE_CONFLICT_TITLE() { return new DekiResource("System.API.Error.title-same-as-current"); }
        public static DekiResource PAGE_MOVE_CONFLICT_TEMPLATE() { return new DekiResource("System.API.Error.cannot-move-in-out-templates"); }
        public static DekiResource PAGE_MOVE_CONFLICT_SPECIAL() { return new DekiResource("System.API.Error.cannot-move-in-out-special"); }
        public static DekiResource PAGE_MOVE_CONFLICT_TITLE_NOT_EDITABLE(NS @namespace) { return new DekiResource("System.API.Error.cannot-move-to-namespace", @namespace); }
        public static DekiResource PAGE_MOVE_CONFLICT_MOVE_HOMEPAGE() { return new DekiResource("System.API.Error.cannot-move-home-page"); }
        public static DekiResource PAGE_MOVE_CONFLICT_MOVE_ROOTUSER() { return new DekiResource("System.API.Error.cannot-move-user-page"); }
        public static DekiResource PAGE_MOVE_CONFLICT_SOURCE_NAMESPACE(NS @namespace) { return new DekiResource("System.API.Error.cannot-move-from-namespace", @namespace); }
        public static DekiResource PAGE_MOVE_CONFLICT_MOVE_TO_DESCENDANT(string parentPagePath, string childPathPath) { return new DekiResource("System.API.Error.cannot-move-page-to-child", parentPagePath, childPathPath); }
        public static DekiResource COMMENT_ADDED(int commentNumber) { return new DekiResource("System.API.comment-added", commentNumber); }
        public static DekiResource COMMENT_EDITED(int commentNumber) { return new DekiResource("System.API.comment-edited", commentNumber); }
        public static DekiResource COMMENT_DELETED(int commentNumber) { return new DekiResource("System.API.comment-deleted", commentNumber); }
        public static DekiResource NEWUSERPAGETEXT() { return new DekiResource("System.API.new-user-page-content"); }
        public static DekiResource INVALID_TITLE() { return new DekiResource("System.API.Error.invalid-title"); }
        public static DekiResource INVALID_REDIRECT() { return new DekiResource("System.API.Error.invalid-redirect"); }
        public static DekiResource INVALID_REDIRECT_OPERATION() { return new DekiResource("System.API.Error.invalid-redirect-operation"); }
        public static DekiResource INTERNAL_ERROR() { return new DekiResource("System.API.Error.internal-error"); }
        public static DekiResource OPENSEARCH_SHORTNAME(string sitename) { return new DekiResource("System.API.opensearch-shortname", sitename); }
        public static DekiResource OPENSEARCH_DESCRIPTION() { return new DekiResource("System.API.opensearch-description"); }
        public static DekiResource EDIT_PAGE() { return new DekiResource("Skin.Common.edit-page"); }
        public static DekiResource CREATE_PAGE() { return new DekiResource("Skin.Common.new-page"); }
        public static DekiResource VIEW_PAGE() { return new DekiResource("System.API.page-diff-view-page"); }
        public static DekiResource VIEW_PAGE_DIFF() { return new DekiResource("System.API.page-diff-view-page-diff"); }
        public static DekiResource VIEW_PAGE_HISTORY() { return new DekiResource("System.API.page-diff-view-page-history"); }
        public static DekiResource BAN_USER(string username) { return new DekiResource("System.API.page-diff-ban-user", username); }
        public static DekiResource PAGE_NOT_AVAILABLE() { return new DekiResource("System.API.page-diff-page-not-available"); }
        public static DekiResource COMMENT_NOT_AVAILABLE() { return new DekiResource("System.API.page-diff-comment-not-available"); }
        public static DekiResource RESTRICTION_CHANGED(string restriction) { return new DekiResource("System.API.restriction-changed", restriction); }
        public static DekiResource PROTECTED() { return new DekiResource("System.API.protected"); }
        public static DekiResource MISMATCHED_ID() { return new DekiResource("System.API.Error.mismatched_id"); }
        public static DekiResource INVALID_REVISION() { return new DekiResource("System.API.Error.invalid_revision"); }
        public static DekiResource CANNOT_HIDE_HEAD() { return new DekiResource("System.API.Error.cannot_hide_head"); }
        public static DekiResource HIDDEN_ATTRIBUTE() { return new DekiResource("System.API.Error.hidden_attribute"); }
        public static DekiResource REVISION_NOT_FOUND() { return new DekiResource("System.API.Error.rev_not_found"); }
        public static DekiResource REVISION_CANNOT_BE_HIDDEN() { return new DekiResource("System.API.Error.rev_cannot_be_hidden"); }
        public static DekiResource NO_REVISION_TO_HIDE_UNHIDE() { return new DekiResource("System.API.Error.no_rev_to_hide_unhide"); }

        #region DekiWiki-Comments
        public static DekiResource FAILED_EDIT_COMMENT() { return new DekiResource("System.API.Error.failed_edit_comment"); }
        public static DekiResource FAILED_POST_COMMENT() { return new DekiResource("System.API.Error.failed_post_comment"); }
        public static DekiResource COMMENT_NOT_FOUND() { return new DekiResource("System.API.Error.comment_not_found"); }
        public static DekiResource FILTER_PARAM_INVALID() { return new DekiResource("System.API.Error.filter_param_invalid"); }
        #endregion

        #region DekiWiki-Files
        public static DekiResource CANNOT_PARSE_NUMFILES() { return new DekiResource("System.API.Error.cannot_parse_numfiles"); }
        public static DekiResource INVALID_FILE_RATIO() { return new DekiResource("System.API.Error.invalid_file_ratio"); }
        public static DekiResource INVALID_FILE_SIZE() { return new DekiResource("System.API.Error.invalid_file_size"); }
        public static DekiResource INVALID_FILE_FORMAT() { return new DekiResource("System.API.Error.invalid_file_format"); }
        public static DekiResource COULD_NOT_RETRIEVE_FILE(uint fileResourceId, int revision) { return new DekiResource("System.API.Error.could_not_retrieve_file", fileResourceId, revision); }
        public static DekiResource CANNOT_UPLOAD_TO_TEMPLATE() { return new DekiResource("System.API.Error.cannot_upload_to_template"); }
        public static DekiResource FAILED_TO_SAVE_UPLOAD() { return new DekiResource("System.API.Error.failed_to_save_upload"); }
        //public static string ATTACHMENT_EXISTS_ON_PAGE() { return new DekiResource("Attachment already exists on target page"); }
        public static DekiResource FILE_ALREADY_REMOVED() { return new DekiResource("System.API.Error.file_already_removed"); }
        public static DekiResource REVISION_HEAD_OR_INT() { return new DekiResource("System.API.Error.revision_head_or_int"); }
        public static DekiResource REVISION_NOT_SUPPORTED() { return new DekiResource("System.API.Error.revision_not_supported"); }
        //public static DekiResource MAX_REVISIONS_ALLOWED() { return new DekiResource("System.API.Error.max_revisions_allowed"); }
        public static DekiResource COULD_NOT_FIND_FILE() { return new DekiResource("System.API.Error.could_not_find_file"); }
        public static DekiResource FILE_HAS_BEEN_REMOVED() { return new DekiResource("System.API.Error.file_has_been_removed"); }
        public static DekiResource MISSING_FILENAME() { return new DekiResource("System.API.Error.missing_filename"); }
        #endregion

        #region DekiWiki-Groups
        public static DekiResource GROUPID_PARAM_INVALID() { return new DekiResource("System.API.Error.groupid_param_invalid"); }
        public static DekiResource GROUP_NOT_FOUND(string group) { return new DekiResource("System.API.Error.group_not_found", group); }
        //public static string GROUP_ID_NOT_FOUND() { return new DekiResource("Group id '{0}' not found"); }
        #endregion

        #region DekiWiki-Nav
        public static DekiResource OUTPUT_PARAM_INVALID() { return new DekiResource("System.API.Error.output_param_invalid"); }
        #endregion

        #region DekiWiki-News
        public static DekiResource GIVEN_USER_NOT_FOUND() { return new DekiResource("System.API.Error.given_user_not_found"); }
        public static DekiResource SINCE_PARAM_INVALID() { return new DekiResource("System.API.Error.since_param_invalid"); }
        public static DekiResource MAX_PARAM_INVALID() { return new DekiResource("System.API.Error.max_param_invalid"); }
        public static DekiResource OFFSET_PARAM_INVALID() { return new DekiResource("System.API.Error.offset_param_invalid"); }
        public static DekiResource FORMAT_PARAM_INVALID() { return new DekiResource("System.API.Error.format_param_invalid"); }
        #endregion

        #region DekiWiki-Pages
        public static DekiResource FORMAT_PARAM_MUST_BE() { return new DekiResource("System.API.Error.format_param_must_be"); }
        //public static DekiResource UNABLE_TO_EXPORT_PAGEID() { return new DekiResource("System.API.Error.unable_to_export_pageid"); }
        public static DekiResource UNABLE_TO_EXPORT_PAGE_PRINCE_ERROR(ulong pageId) { return new DekiResource("System.API.Error.unable_to_export_page_prince_error", pageId); }
        public static DekiResource DIR_IS_NOT_VALID(string directory) { return new DekiResource("System.API.Error.dir_is_not_valid", directory); }
        public static DekiResource COULD_NOT_FIND_REVISION(int revision, ulong pageId) { return new DekiResource("System.API.Error.could_not_find_revision", revision, pageId); }
        //public static string MAX_PARAM_INVALID() { return new DekiResource("'max' parameter is not valid"); }
        //public static string OFFSET_PARAM_INVALID() { return new DekiResource("'offset' parameter is not valid"); }
        public static DekiResource MISSING_FUNCTIONALITY() { return new DekiResource("System.API.Error.missing_functionality"); }
        public static DekiResource SECTION_PARAM_INVALID() { return new DekiResource("System.API.Error.section_param_invalid"); }
        public static DekiResource CONTENT_TYPE_NOT_SUPPORTED(MimeType unsupported) { return new DekiResource("System.API.Error.content_type_not_supported", unsupported, MimeType.TEXT, MimeType.FORM_URLENCODED); }
        public static DekiResource PAGE_ALREADY_EXISTS() { return new DekiResource("System.API.Error.page_already_exists"); }
        public static DekiResource EDITTIME_PARAM_INVALID() { return new DekiResource("System.API.Error.edittime_param_invalid"); }
        public static DekiResource PAGE_WAS_MODIFIED() { return new DekiResource("System.API.Error.page_was_modified"); }
        public static DekiResource HEADING_PARAM_INVALID() { return new DekiResource("System.API.Error.heading_param_invalid"); }
        public static DekiResource INVALID_FORMAT_GIVEN() { return new DekiResource("System.API.Error.invalid_format_given"); }
        public static DekiResource RESTRICTION_INFO_MISSING() { return new DekiResource("System.API.Error.restriction_info_missing"); }
        public static DekiResource RESTRICTION_NOT_FOUND(string restriction) { return new DekiResource("System.API.Error.restriciton_not_found", restriction); }
        public static DekiResource CASCADE_PARAM_INVALID() { return new DekiResource("System.API.Error.cascade_param_invalid"); }
        public static DekiResource CANNOT_MODIFY_TALK() { return new DekiResource("System.API.Error.cannot_modify_talk"); }
        public static DekiResource CANNOT_CREATE_TALK() { return new DekiResource("System.API.Error.cannot_create_talk"); }
        public static DekiResource CANNOT_RELTO_TALK() { return new DekiResource("System.API.Error.cannot_relto_talk"); }
        public static DekiResource TITLE_RENAME_FAILURE(string message) { return new DekiResource("System.API.Error.title_rename_failure", message); }
        #endregion

        #region DekiWiki-Ratings
        public static DekiResource RATING_INVALID_SCORE() { return new DekiResource("System.API.Error.rating_invalid_score"); }
        #endregion

        #region DekiWiki-RecycleBin
        public static DekiResource FILE_NOT_DELETED() { return new DekiResource("System.API.Error.file_not_deleted"); }
        //public static string COULD_NOT_RETRIEVE_FILE() { return new DekiResource("Could not retrieve attachment fileid {0} rev {1}"); }
        public static DekiResource LIMIT_PARAM_INVALID() { return new DekiResource("System.API.Error.limit_param_invalid"); }
        public static DekiResource TITLE_PARAM_INVALID() { return new DekiResource("System.API.Error.title_param_invalid"); }
        #endregion

        #region DekiWiki-Services
        public static DekiResource SERVICE_NOT_FOUND(uint serviceId) { return new DekiResource("System.API.Error.service_not_found", serviceId); }
        public static DekiResource SERVICE_NOT_FOUND(string serviceName) { return new DekiResource("System.API.Error.service_not_found", serviceName); }
        #endregion

        #region DekiWiki-Site
        public static DekiResource ERROR_DELETING_INDEX(string errorMessage) { return new DekiResource("System.API.Error.error_deleting_index", errorMessage); }
        public static DekiResource MUST_BE_LOGGED_IN() { return new DekiResource("System.API.Error.must_be_logged_in"); }
        //public static DekiResource ERROR_PARSING_SEARCH_QUERY() { return new DekiResource("System.API.Error.error_parsing_search_query"); }
        public static DekiResource ERROR_QUERYING_SEARCH_INDEX(SearchQuery query) { return new DekiResource("System.API.Error.error_querying_search_index", query.Raw.EncodeHtmlEntities()); }
        public static DekiResource EXPECTED_IMAGE_MIMETYPE() { return new DekiResource("System.API.Error.expected_image_mimetype"); }
        public static DekiResource EXPECTED_XML_CONTENT_TYPE() { return new DekiResource("System.API.Error.expected_xml_content_type"); }
        public static DekiResource CANNOT_PROCESS_LOGO_IMAGE() { return new DekiResource("System.API.Error.cannot_process_logo_image"); }
        public static DekiResource ERROR_NO_SUCH_RESOURCE(string resource) { return new DekiResource("System.API.Error.no_such_resource", resource); }
        public static DekiResource SITE_TOO_BIG_TO_GENERATE_SITEMAP() { return new DekiResource("System.API.Error.site_too_big_to_generate_sitemap"); }
        #endregion

        #region DekiWiki-SiteRoles
        public static DekiResource ROLEID_PARAM_INVALID() { return new DekiResource("System.API.Error.roleid_param_invalid"); }
        public static DekiResource ROLE_NAME_NOT_FOUND(string role) { return new DekiResource("System.API.Error.role_name_not_found", role); }
        public static DekiResource ROLE_ID_NOT_FOUND(uint roleId) { return new DekiResource("System.API.Error.role_id_not_found", roleId); }
        #endregion

        #region DekiWiki-Users
        //public static string GIVEN_USER_NOT_FOUND() { return new DekiResource("Given user was not found"); }
        public static DekiResource ACCOUNTPASSWORD_PARAM_INVALID() { return new DekiResource("System.API.Error.accountpassword_param_invalid"); }
        public static DekiResource GIVEN_USER_NOT_FOUND_USE_POST() { return new DekiResource("System.API.Error.given_user_not_found_use_post"); }
        public static DekiResource INVALID_OPERATION_LIST() { return new DekiResource("System.API.Error.invalid_operation_list"); }
        public static DekiResource EXPECTED_ROOT_NODE_PAGES() { return new DekiResource("System.API.Error.expected_root_node_pages"); }
        public static DekiResource NEW_PASSWORD_NOT_PROVIDED() { return new DekiResource("System.API.Error.new_password_not_provided"); }
        public static DekiResource NEW_PASSWORD_TOO_SHORT() { return new DekiResource("System.API.Error.new_password_too_short"); }
        public static DekiResource UNABLE_TO_FIND_USER() { return new DekiResource("System.API.Error.unable_to_find_user"); }
        public static DekiResource PASSWORD_CHANGE_LOCAL_ONLY() { return new DekiResource("System.API.Error.password_change_local_only"); }
        public static DekiResource CANNOT_CHANGE_ANON_PASSWORD() { return new DekiResource("System.API.Error.cannot_change_anon_password"); }
        public static DekiResource CURRENTPASSWORD_DOES_NOT_MATCH() { return new DekiResource("System.API.Error.currentpassword_does_not_match"); }
        public static DekiResource MUST_BE_TARGET_USER_OR_ADMIN() { return new DekiResource("System.API.Error.must_be_target_user_or_admin"); }
        public static DekiResource CANNOT_CHANGE_OWN_ALT_PASSWORD() { return new DekiResource("System.API.Error.cannot_change_own_alt_password"); }
        public static DekiResource USERID_PARAM_INVALID() { return new DekiResource("System.API.Error.userid_param_invalid"); }
        #endregion

        #region DekiXmlParser
        public static DekiResource CONTENT_CANNOT_BE_PARSED() { return new DekiResource("System.API.Error.content_cannot_be_parsed"); }
        public static DekiResource XPATH_PARAM_INVALID() { return new DekiResource("System.API.Error.xpath_param_invalid"); }
        //public static string SECTION_PARAM_INVALID() { return new DekiResource("'section' parameter is not valid"); }
        public static DekiResource INFINITE_PAGE_INCLUSION() { return new DekiResource("System.API.Error.infinite_page_inclusion"); }
        public static DekiResource PAGE_FORMAT_INVALID() { return new DekiResource("System.API.Error.page_format_invalid"); }
        public static DekiResource MISSING_FILE(string pagePath) { return new DekiResource("System.API.Error.missing_file", pagePath); }
        #endregion

        #region GrantBE
        public static DekiResource CANNOT_PARSE_GRANTS(string message) { return new DekiResource("System.API.Error.cannot_parse_grants", message); }
        public static DekiResource USER_OR_GROUP_ID_NOT_GIVEN() { return new DekiResource("System.API.Error.user_or_group_id_not_given"); }
        public static DekiResource ROLE_NOT_GIVEN() { return new DekiResource("System.API.Error.role_not_given"); }
        public static DekiResource ROLE_UNRECOGNIZED() { return new DekiResource("System.API.Error.role_unrecognized"); }
        public static DekiResource CANNOT_PARSE_EXPIRY() { return new DekiResource("System.API.Error.cannot_parse_expiry"); }
        #endregion

        #region AttachmentBL
        public static DekiResource MAX_FILE_SIZE_ALLOWED(long maxfilesize) { return new DekiResource("System.API.Error.max_file_size_allowed", maxfilesize); }
        public static DekiResource FILENAME_IS_INVALID() { return new DekiResource("System.API.Error.filename_is_invalid"); }
        public static DekiResource FILE_TYPE_NOT_ALLOWED(string extension) { return new DekiResource("System.API.Error.file_type_not_allowed", extension); }
        // public static DekiResource MAX_REVISIONS_ALLOWED() { return new DekiResource("A maximum of {0} revisions is allowed per file"); }
        public static DekiResource RESTORE_FILE_FAILED_NO_PARENT() { return new DekiResource("System.API.Error.restore_file_failed_no_parent"); }
        public static DekiResource ATTACHMENT_EXISTS_ON_PAGE(string fileName, string pagePath) { return new DekiResource("System.API.Error.attachment_exists_on_page", fileName, pagePath); }
        public static DekiResource FILE_RESTORE_NAME_CONFLICT() { return new DekiResource("System.API.Error.file_restore_name_conflict"); }
        public static DekiResource ATTACHMENT_MOVE_INVALID_PARAM() { return new DekiResource("System.API.Error.attachment_move_invalid_param"); }
        #endregion

        #region AttachmentPreviewBL
        public static DekiResource FAILED_WITH_MIME_TYPE(MimeType mimeType) { return new DekiResource("System.API.Error.failed_with_mime_type", mimeType); }
        public static DekiResource FORMAT_CONVERSION_WITH_SIZE_UNSUPPORTED() { return new DekiResource("System.API.Error.format_conversion_with_size_unsupported"); }
        public static DekiResource IMAGE_REQUEST_TOO_LARGE() { return new DekiResource("System.API.Error.image_request_too_large"); }
        public static DekiResource CANNOT_CREATE_THUMBNAIL() { return new DekiResource("System.API.Error.cannot_create_thumbnail"); }
        #endregion

        #region AuthBL
        public static DekiResource INVALID_SERVICE_ID(uint serviceId) { return new DekiResource("System.API.Error.invalid_service_id", serviceId); }
        public static DekiResource NOT_AUTH_SERVICE(uint serviceId) { return new DekiResource("System.API.Error.not_auth_service", serviceId); }
        public static DekiResource AUTHENTICATION_FAILED() { return new DekiResource("System.API.Error.authentication_failed"); }
        public static DekiResource USER_DISABLED(string username) { return new DekiResource("System.API.Error.user_disabled", username); }
        //public static DekiResource CANNOT_RETRIEVE_USER_FOR_TOKEN() { return new DekiResource("System.API.Error.cannot_retrieve_user_for_token"); }
        #endregion

        #region CommentBL
        public static DekiResource COMMENT_MIMETYPE_UNSUPPORTED(MimeType unsupported) { return new DekiResource("System.API.Error.comment_mimetype_unsupported", unsupported, MimeType.HTML, MimeType.TEXT); }
        public static DekiResource COMMENT_FOR(string pageTitle) { return new DekiResource("System.API.comment_for", pageTitle); }
        public static DekiResource COMMENT_BY_TO(string user, string pageTitle) { return new DekiResource("System.API.comment_by_to", user, pageTitle); }
        public static DekiResource COMMENT_CONCURRENCY_ERROR(ulong pageId) { return new DekiResource("System.API.Error.comment_concurrency_error", pageId); }
        #endregion

        #region ConfigBL
        public static DekiResource MISSING_REQUIRED_CONFIG_KEY(string key) { return new DekiResource("System.API.Error.missing_required_config_key", key); }
        public static DekiResource ERROR_UPDATE_CONFIG_SETTINGS() { return new DekiResource("System.API.Error.error_update_config_settings"); }
        public static DekiResource ERROR_DUPE_CONFIG_SETTINGS(string key) { return new DekiResource("System.API.Error.error_dupe_config_settings", key); }        
        #endregion

        #region ExternalServicesSA
        public static DekiResource UNABLE_TO_AUTH_WITH_SERVICE(ServiceType serviceInfo, string sid, string uri) { return new DekiResource("System.API.Error.unable_to_auth_with_service", serviceInfo, sid, uri); }
        public static DekiResource SERVICE_NOT_STARTED(ServiceType type, string sid) { return new DekiResource("System.API.Error.service_not_started", type, sid); }
        public static DekiResource UNEXPECTED_EXTERNAL_USERNAME(string username, string builtUsername) { return new DekiResource("System.API.Error.unexpected_external_username", username, builtUsername); }
        public static DekiResource AUTHENTICATION_FAILED_FOR(string serviceDescription) { return new DekiResource("System.API.Error.authentication_failed_for", serviceDescription); }
        public static DekiResource GROUP_DETAILS_LOOKUP_FAILED(string group) { return new DekiResource("System.API.Error.group_details_lookup_failed", group); }
        //public static DekiResource SERVICE_INFO_LOOKUP_FAILED() { return new DekiResource("System.API.Error.service_info_lookup_failed"); }
        public static DekiResource EXTERNAL_AUTH_BAD_RESPONSE() { return new DekiResource("System.API.Error.external_auth_bad_response"); }
        #endregion

        #region ExtensionBL
        public static DekiResource EXTENSION_INVALID_REMOTE_SERVICE(XUri uri) { return new DekiResource("System.API.Error.extension_invalid_remote_service", uri); }
        public static DekiResource EXTENSION_INVALID_NAMESPACE(string ns) { return new DekiResource("System.API.Error.extension_invalid_namespace", ns); }
        #endregion

        #region GroupBL
        public static DekiResource EXTERNAL_GROUP_NOT_FOUND(string groupName) { return new DekiResource("System.API.Error.external_group_not_found", groupName); }
        public static DekiResource GROUP_EXISTS_WITH_SERVICE(string groupName, uint serviceId) { return new DekiResource("System.API.Error.group_exists_with_service", groupName, serviceId); }
        public static DekiResource GROUP_ID_NOT_FOUND(uint? groupId) { return new DekiResource("System.API.Error.group_id_not_found", (object)groupId ?? -1); }
        public static DekiResource GROUP_CREATE_UPDATE_FAILED() { return new DekiResource("System.API.Error.group_create_update_failed"); }
        public static DekiResource EXPECTED_ROOT_NODE_USERS() { return new DekiResource("System.API.Error.expected_root_node_users"); }
        public static DekiResource GROUP_MEMBERS_REQUIRE_SAME_AUTH() { return new DekiResource("System.API.Error.group_members_require_same_auth"); }
        public static DekiResource GROUP_ID_ATTR_INVALID() { return new DekiResource("System.API.Error.group_id_attr_invalid"); }
        public static DekiResource SERVICE_AUTH_ID_ATTR_INVALID() { return new DekiResource("System.API.Error.service_auth_id_attr_invalid"); }
        public static DekiResource SERVICE_DOES_NOT_EXIST(uint serviceId) { return new DekiResource("System.API.Error.service_does_not_exist", serviceId); }
        public static DekiResource ROLE_DOES_NOT_EXIST(string role) { return new DekiResource("System.API.Error.role_does_not_exist", role); }
        //public static string USER_ID_ATTR_INVALID() { return new DekiResource("/user/@id not specified or invalid"); }
        public static DekiResource COULD_NOT_FIND_USER(uint userId) { return new DekiResource("System.API.Error.could_not_find_user", userId); }
        public static DekiResource EXTERNAL_GROUP_RENAME_NOT_ALLOWED() { return new DekiResource("System.API.Error.group_external_rename_not_allowed"); }
        public static DekiResource GROUP_EXTERNAL_CHANGE_MEMBERS() { return new DekiResource("System.API.Error.group_external_change_members"); }
        public static DekiResource GROUP_SERVICE_NOT_FOUND(uint serviceId, string groupname) { return new DekiResource("System.API.Error.group_service_not_found", serviceId, groupname); }
        #endregion

        #region PageArchiveBL
        public static DekiResource CANNOT_RESTORE_PAGE_NAMED(string conflictTitles) { return new DekiResource("System.API.Error.cannot_restore_page_named", conflictTitles); }
        public static DekiResource RESTORE_PAGE_ID_NOT_FOUND(uint id) { return new DekiResource("System.API.Error.restore_page_id_not_found", id); }
        public static DekiResource PAGEARCHIVE_BAD_TRANSACTION(uint transactionId, uint pageId) { return new DekiResource("System.API.Error.pagearchive_bad_transaction", transactionId, pageId); }

        #endregion

        #region PageBL
        public static DekiResource INVALID_POSTED_DOCUMENT() { return new DekiResource("System.API.Error.invalid_posted_document"); }
        public static DekiResource INVALID_POSTED_DOCUMENT_1(string documentroot) { return new DekiResource("System.API.Error.invalid_posted_document_1", documentroot); }
        public static DekiResource INVALID_PAGE_ID() { return new DekiResource("System.API.Error.invalid_page_id"); }
        public static DekiResource UNABLE_TO_PARSE_PAGES_FROM_XML(string message) { return new DekiResource("System.API.Error.unable_to_parse_pages_from_xml", message); }
        public static DekiResource UNABLE_TO_FIND_HOME_PAGE() { return new DekiResource("System.API.Error.unable_to_find_home_page"); }
        public static DekiResource UNABLE_TO_FIND_OLD_PAGE_FOR_ID(ulong pageId, DateTime timestamp) { return new DekiResource("System.API.Error.unable_to_find_old_page_for_id", pageId, timestamp); }
        public static DekiResource UNABLE_TO_RETRIEVE_PAGE_FOR_ID(ulong pageId) { return new DekiResource("System.API.Error.unable_to_retrieve_page_for_id", pageId); }
        public static DekiResource SECTION_EDIT_EXISTING_PAGES_ONLY() { return new DekiResource("System.API.Error.section_edit_existing_pages_only"); }
        public static DekiResource CANNOT_FIND_PAGE_WITH_REVISION() { return new DekiResource("System.API.Error.cannot_find_page_with_revision"); }
        public static DekiResource PAGES_ALREADY_EXIST_AT_DEST() { return new DekiResource("System.API.Error.pages_already_exist_at_dest"); }
        public static DekiResource CANNOT_MODIFY_SPECIAL_PAGES() { return new DekiResource("System.API.Error.cannot_modify_special_pages"); }
        public static DekiResource HOMEPAGE_CANNOT_BE_DELETED() { return new DekiResource("System.API.Error.homepage_cannot_be_deleted"); }
        //public static string REVISION_HEAD_OR_INT() { return new DekiResource("Revision may be HEAD or a positive integer"); }
        public static DekiResource LANGUAGE_PARAM_INVALID() { return new DekiResource("System.API.Error.language_param_invalid"); }
        public static DekiResource LANGUAGE_SET_TALK() { return new DekiResource("System.API.Error.language_set_talk"); }
        public static DekiResource PAGE_ID_PARAM_INVALID() { return new DekiResource("System.API.Error.page_id_param_invalid"); }
        public static DekiResource CANNOT_FIND_REQUESTED_PAGE() { return new DekiResource("System.API.Error.cannot_find_requested_page"); }
        public static DekiResource PAGE_HIDDEN_REVISION_MUST_BE_UNHIDDEN() { return new DekiResource("System.API.Error.page_hidden_revision_must_be_unhidden"); }
        public static DekiResource PAGE_CONCURRENCY_ERROR(ulong pageId) { return new DekiResource("System.API.Error.page_concurrency_error", pageId); }
        #endregion

        #region PersmissionBL
        public static DekiResource PERMISSIONS_NOT_ALLOWED_ON(string pagePath, NS pageNamespace) { return new DekiResource("System.API.Error.permissions_not_allowed_on", pagePath, pageNamespace); }
        public static DekiResource USER_WOULD_BE_LOCKED_OUT_OF_PAGE() { return new DekiResource("System.API.Error.user_would_be_locked_out_of_page"); }
        public static DekiResource CANNOT_RETRIEVE_REQUIRED_ROLE(string role) { return new DekiResource("System.API.Error.cannot_retrieve_required_role", role); }
        public static DekiResource DUPLICATE_ROLE() { return new DekiResource("System.API.Error.duplicate_role"); }
        public static DekiResource DUPLICATE_GRANT_FOR_USER_GROUP() { return new DekiResource("System.API.Error.duplicate_grant_for_user_group"); }
        public static DekiResource CANNOT_FIND_USER_WITH_ID(uint userId) { return new DekiResource("System.API.Error.cannot_find_user_with_id", userId); }
        public static DekiResource CANNOT_FIND_GROUP_WITH_ID(uint groupId) { return new DekiResource("System.API.Error.cannot_find_group_with_id", groupId); }
        public static DekiResource ACCESS_DENIED_TO(string userName, string actions, string mask) { return new DekiResource("System.API.Error.access_denied_to", userName, actions, mask); }
        public static DekiResource ACCESS_DENIED_TO_FOR_PAGE(string userName, string actions, string mask, ulong pageId) { return new DekiResource("System.API.Error.access_denied_to_for_page", userName, actions, mask, pageId); }
        public static DekiResource OPERATION_DENIED_FOR_ANONYMOUS(string message) { return new DekiResource("System.API.Error.operation_denied_for_anonymous", message); }
        public static DekiResource PERMISSIONS_NO_ADMIN_FOR_IMPERSONATION(string users) { return new DekiResource("System.API.Error.permissions_no_admin_for_impersonation", users); }
        public static DekiResource OPERATION_DENIED_FOR_ANONYMOUS(DekiResource message) { return new DekiResource("System.API.Error.operation_denied_for_anonymous", message); }
        public static DekiResource ROLE_NAME_PARAM_INVALID() { return new DekiResource("System.API.Error.rold_name_param_invalid"); }
        public static DekiResource ANONYMOUS_USER_EDIT() { return new DekiResource("System.API.Error.anonymous_user_edit"); }
        #endregion

        #region ResourceBL
        public static DekiResource RESOURCE_ETAG_CONFLICT(string etag, uint resourceId) { return new DekiResource("System.API.Error.resource_etag_conflict", etag, resourceId); }
        public static DekiResource RESOURCE_ETAG_NOT_HEAD(uint resourceId, int revision) { return new DekiResource("System.API.Error.resource_etag_not_head", resourceId, revision); }
        public static DekiResource RESOURCE_EXPECTED_HEAD_REVISION(int headRevision, int revision) { return new DekiResource("System.API.Error.resource_expected_head_revision", headRevision, revision); }
        public static DekiResource RESOURCE_REVISION_OUT_OF_RANGE(string resource) { return new DekiResource("System.API.Error.resource_revision_out_of_range", resource); }
        #endregion

        #region ServiceBL
        public static DekiResource EXPECTED_SERVICE_TO_HAVE_SID(string sid) { return new DekiResource("System.API.Error.expected_service_to_have_sid", sid); }
        public static DekiResource SERVICE_ADMINISTRATION_DISABLED() { return new DekiResource("System.API.Error.service_administration_disabled"); }
        public static DekiResource SERVICE_CREATE_SID_MISSING() { return new DekiResource("System.API.Error.service_create_sid_missing"); }
        public static DekiResource SERVICE_CREATE_TYPE_MISSING() { return new DekiResource("System.API.Error.service_create_type_missing"); }
        public static DekiResource SERVICE_UPDATE_TYPE_INVALID() { return new DekiResource("System.API.Error.service_update_type_invalid"); }
        public static DekiResource SERVICE_UNEXPECTED_INIT() { return new DekiResource("System.API.Error.service_unexpected_init"); }
        public static DekiResource SERVICE_MISSING_DESCRIPTION() { return new DekiResource("System.API.Error.service_missing_description"); }
        public static DekiResource SERVICE_INVALID_STATUS() { return new DekiResource("System.API.Error.service_invalid_status"); }
        public static DekiResource SERVICE_CANNOT_DELETE_AUTH() { return new DekiResource("System.API.Error.service_cannot_delete_auth"); }
        public static DekiResource SERVICE_CANNOT_MODIFY_AUTH() { return new DekiResource("System.API.Error.service_cannot_mod_auth"); }
        public static DekiResource SERVICE_CANNOT_SET_LOCAL_URI() { return new DekiResource("System.API.Error.service_cannot_set_local_uri"); }
        #endregion

        #region SiteBL
        public static DekiResource CANNOT_RETRIEVE_ADMIN_ACCOUNT() { return new DekiResource("System.API.Error.cannot_retrieve_admin_account"); }
        public static DekiResource SMTP_SERVER_NOT_CONFIGURED() { return new DekiResource("System.API.Error.smtp_server_not_configured"); }
        public static DekiResource ADMIN_EMAIL_NOT_SET() { return new DekiResource("System.API.Error.admin_email_not_set"); }
        #endregion

        #region UserBL
        public static DekiResource USER_VALIDATION_FAILED(string username) { return new DekiResource("System.API.Error.user_validation_failed", username); }
        public static DekiResource CANNOT_SET_EXTERNAL_ACCOUNT_PASSWORD() { return new DekiResource("System.API.Error.cannot_set_external_account_password"); }
        public static DekiResource EXTERNAL_USER_NOT_FOUND(string username) { return new DekiResource("System.API.Error.external_user_not_found", username); }
        public static DekiResource USER_EXISTS_WITH_EXTERNAL_NAME(string externalAccount, string user, uint serviceId) { return new DekiResource("System.API.Error.user_exists_with_external_name", externalAccount, user, serviceId); }
        public static DekiResource USER_EXISTS_WITH_ID(string user, uint userId) { return new DekiResource("System.API.Error.user_exists_with_id", user, userId); }
        public static DekiResource USER_ID_ATTR_INVALID() { return new DekiResource("System.API.Error.user_id_attr_invalid"); }
        public static DekiResource USER_ID_NOT_FOUND(uint userId) { return new DekiResource("System.API.Error.user_id_not_found", userId); }
        public static DekiResource USE_PUT_TO_CHANGE_PASSWORDS() { return new DekiResource("System.API.Error.use_put_to_change_passwords"); }
        public static DekiResource UPDATE_USER_AUTH_SERVICE_NOT_ALLOWED() { return new DekiResource("System.API.Error.update_user_auth_service_not_allowed"); }
        public static DekiResource DEACTIVATE_ANONYMOUS_NOT_ALLOWED() { return new DekiResource("System.API.Error.deactivate_anonymous_not_allowed"); }
        public static DekiResource USERNAME_PARAM_INVALID() { return new DekiResource("System.API.Error.username_param_invalid"); }
        //public static string SERVICE_AUTH_ID_ATTR_INVALID() { return new DekiResource("'/user/service.authentication/@id' not provided or invalid"); }
        //public static string SERVICE_DOES_NOT_EXIST() { return new DekiResource("service {0} does not exist"); }
        //public static string ROLE_DOES_NOT_EXIST() { return new DekiResource("role '{0}' does not exist"); }
        public static DekiResource USER_STATUS_ATTR_INVALID() { return new DekiResource("System.API.Error.user_status_attr_invalid"); }
        public static DekiResource NO_REGISTRATION_FOUND() { return new DekiResource("System.API.Error.no_registration_found"); }
        public static DekiResource REGISTRATION_EXPIRED() { return new DekiResource("System.API.Error.registration_expired"); }
        public static DekiResource USER_ALREADY_EXISTS() { return new DekiResource("System.API.Error.user_already_exists"); }
        public static DekiResource EXTERNAL_USER_RENAME_NOT_ALLOWED() { return new DekiResource("System.API.Error.user_external_rename_not_allowed"); }
        public static DekiResource USER_RENAME_HOMEPAGE_CONFLICT() { return new DekiResource("System.API.Error.user_rename_homepage_conflict"); }
        public static DekiResource INVALID_TIMEZONE_VALUE() { return new DekiResource("System.API.Error.invalid_timezone_value"); }
        public static DekiResource INVALID_LANGUAGE_VALUE() { return new DekiResource("System.API.Error.invalid_language_value"); }
        public static DekiResource USER_AUTHSERVICE_CHANGE_FAIL() { return new DekiResource("System.API.Error.user_authservice_change_fail"); }
        public static DekiResource USER_OWNER_DEACTIVATION_CONFLICT() { return new DekiResource("System.API.Error.user_owner_deactivation_conflict"); }

        public static DekiResource USER_PAGE_FILTER_VERBOSE_NOT_ALLOWED() { return new DekiResource("System.API.Error.user_page_filter_verbose_not_allowed"); }
        public static DekiResource USER_PAGE_FILTER_INVALID_INPUT() { return new DekiResource("System.API.Error.user_page_filter_invalid_input"); }
        #endregion

        #region FSStorage
        public static DekiResource PATH_CONFIG_MISSING() { return new DekiResource("System.API.Error.path_config_missing"); }
        public static DekiResource CAN_ONLY_MOVE_HEAD_REVISION() { return new DekiResource("System.API.Error.can_only_move_head_revision"); }
        public static DekiResource DEST_PAGE_HAS_FILE_WITH_SAME_NAME() { return new DekiResource("System.API.Error.dest_page_has_file_with_same_name"); }
        public static DekiResource CANNOT_CREATE_FILE_DIRECTORY(string directory) { return new DekiResource("System.API.Error.cannot_create_file_directory", directory); }
        public static DekiResource CANNOT_MOVE_FILE_DELETED_EXISTS() { return new DekiResource("System.API.Error.cannot_move_file_deleted_exists"); }
        //public static DekiResource ERROR_MOVING_FILE_REVISIONS(int expectedRevisions, int actualRevisions) { return new DekiResource("System.API.Error.error_moving_file_revisions", expectedRevisions, actualRevisions); }
        public static DekiResource CAN_ONLY_DELETE_HEAD_REVISION() { return new DekiResource("System.API.Error.can_only_delete_head_revision"); }
        public static DekiResource CANNOT_SAVE_FILE_TO(string filename, string message) { return new DekiResource("System.API.Error.cannot_save_file_to", filename, message); }
        //public static DekiResource CANNOT_SET_PERMISSIONS_ON_FILE() { return new DekiResource("System.API.Error.cannot_set_permissions_on_file"); }
        //public static DekiResource SOURCE_FILES_MISSING() { return new DekiResource("System.API.Error.source_files_missing"); }
        #endregion

        #region HttpCacheHelpers
        //public static DekiResource UNABLE_TO_PARSE_VAL_HEADER_VAL() { return new DekiResource("System.API.Error.unable_to_parse_val_header_val"); }
        #endregion

        #region DekiContext
        public static DekiResource NO_INSTANCE_FOR_HOSTNAME() { return new DekiResource("System.API.Error.no_instance_for_hostname"); }
        #endregion

        #region LicenseBL
        public static DekiResource LICENSE_NO_NEW_USER_CREATION(LicenseStateType currentLicenseState) { return new DekiResource("System.API.Error.license_no_new_user_creation", currentLicenseState); }
        public static DekiResource LICENSE_LIMIT_USER_CREATION() { return new DekiResource("System.API.Error.license_limit_user_creation"); }
        public static DekiResource LICENSE_LIMIT_TOO_MANY_USERS(uint currentActiveUsers, uint maxUsers, uint userDelta) { return new DekiResource("System.API.Error.license_limit_too_many_users", currentActiveUsers, maxUsers, userDelta); }
        public static DekiResource LICENSE_UPDATE_INVALID() { return new DekiResource("System.API.Error.license_update_invalid"); }
        public static DekiResource LICENSE_UPDATE_EXPIRED(DateTime expiration) { return new DekiResource("System.API.Error.license_update_expired", expiration); }
        public static DekiResource LICENSE_TRANSITION_INVALID(LicenseStateType currentState, LicenseStateType proposedState) { return new DekiResource("System.API.Error.license_transition_invalid", currentState, proposedState); }
        public static DekiResource LICENSE_UPDATE_PRODUCTKEY_INVALID() { return new DekiResource("System.API.Error.license_update_productkey_invalid"); }
        public static DekiResource LICENSE_OPERATION_NOT_ALLOWED(string operation) { return new DekiResource("System.API.Error.license_operation_not_allowed", operation); }
        public static DekiResource LICENSE_INSUFFICIENT_SEATS(int seatsAllowed) { return new DekiResource("System.API.Error.license_insufficient_seats", seatsAllowed); }
        public static DekiResource LICENSE_ANONYMOUS_SEAT() { return new DekiResource("System.API.Error.license_anonymous_seat"); }

        public static DekiResource LICENSE_SEAT_REMOVAL_FROM_OWNER() { return new DekiResource("System.API.Error.license_seat_removal_from_owner"); }
        public static DekiResource LICENSE_NO_SITE_OWNER_DEFINED() { return new DekiResource("System.API.Error.license_no_site_owner_defined"); }
        public static DekiResource LICENSE_UPLOAD_BY_NON_OWNER(string username, ulong userid) { return new DekiResource("System.API.Error.license_upload_by_non_owner", username, userid); }
        public static DekiResource LICENSE_SEAT_LICENSING_NOT_IN_USE() { return new DekiResource("System.API.Error.license_seat_licensing_not_in_use"); }        
        
        #endregion

        #region PropertyBL
        public static DekiResource PROPERTY_DUPE_EXCEPTION(string property) { return new DekiResource("System.API.Error.property_dupe_exception", property); }
        public static DekiResource PROPERTY_UNEXPECTED_ETAG() { return new DekiResource("System.API.Error.property_unexpected_etag"); }
        public static DekiResource PROPERTY_ALREADY_EXISTS(string property) { return new DekiResource("System.API.Error.property_already_exists", property); }
        public static DekiResource PROPERTY_DOESNT_EXIST_DELETE(string property) { return new DekiResource("System.API.Error.property_doesnt_exist_delete", property); }
        public static DekiResource PROPERTY_INVALID_MIMETYPE(string property, string mimetype) { return new DekiResource("System.API.Error.property_invalid_mimetype", property, mimetype); }
        public static DekiResource PROPERTY_CREATE_MISSING_SLUG() { return new DekiResource("System.API.Error.property_create_missing_slug"); }
        public static DekiResource PROPERTY_EDIT_NONEXISTING_CONFLICT() { return new DekiResource("System.API.Error.property_edit_nonexisting_conflict"); }
        public static DekiResource PROPERTY_EXISTS_CONFLICT(string property) { return new DekiResource("System.API.Error.property_exists_conflict", property); }
        public static DekiResource PROPERTY_CONCURRENCY_ERROR(uint resourceId) { return new DekiResource("System.API.Error.property_concurrency_error", resourceId); }
        #endregion

        #region Banning
        public static DekiResource BANNING_NOT_FOUND_ID(uint banId) { return new DekiResource("System.API.Error.banning_not_found_id", banId); }
        public static DekiResource BANNING_EMPTY_BAN() { return new DekiResource("System.API.Error.banning_empty_ban"); }
        public static DekiResource BANNING_NO_PERMS() { return new DekiResource("System.API.Error.banning_no_perms"); }
        public static DekiResource BANNING_OWNER_CONFLICT() { return new DekiResource("System.API.Error.banning_owner_conflict"); }
        public static DekiResource BANNING_INVALID_IP_ADDRESS(string ipAddress) { return new DekiResource("System.API.Error.banning_invalid_ip_address", ipAddress); }
        public static DekiResource BANNING_LOOPBACK_IP_ADDRESS(string ipAddress) { return new DekiResource("System.API.Error.banning_loopback_ip_address", ipAddress); }
        #endregion

        #region TagBL
        public static DekiResource TAG_ADDED(string added) { return new DekiResource("System.API.added-tags", added); }
        public static DekiResource TAG_REMOVED(string removed) { return new DekiResource("System.API.removed-tags", removed); }
        public static DekiResource CANNOT_FIND_REQUESTED_TAG() { return new DekiResource("System.API.Error.cannot_find_requested_tag"); }
        public static DekiResource TAG_INVALID(string name) { return new DekiResource("System.API.Error.invalid-tag", name); }
        #endregion

        #region RemoteLicenseManager
        public static DekiResource REMOTE_LICENSE_MODIFICATION_FORBIDDEN() { return new DekiResource("System.API.Error.remote_license_modification_forbidden"); }
        public static DekiResource REMOTE_LICENSE_MISSING() { return new DekiResource("System.API.Error.remote_license_missing"); }
        #endregion

        //--- Fields ---
        private readonly IPlainTextResourceManager _resourceManager;
        public readonly CultureInfo Culture;

        //--- Constructors ---
        public DekiResources(IPlainTextResourceManager resourceManager, CultureInfo culture) {
            _resourceManager = resourceManager;
            Culture = culture;
        }

        //--- Methods ---
        public string Localize(DekiResource resource) {
            return Localize(resource.LocalizationKey, resource.Args);
        }

        public string LocalizeOrNull(string resourceKey, params object[] args) {
            return Localize(resourceKey, true, args);
        }

        public string Localize(string resourceKey, params object[] args) {
            return Localize(resourceKey, false, args);
        }

        private string Localize(string resourceKey, bool acceptNull, object[] args) {
            if(args == null) {
                args = new object[0];
            }
            var format = GetLocalizedFormat(resourceKey, acceptNull);
            if(format == null) {
                return null;
            }

            // TODO (arnec): fix the resource reader to escape { }
            // Note (arnec): while we enforce args via DekiResource, someone calling Localize directory and not providing args
            // could get a string with placeholders not substituted. Should really make the resource reader escape all  { } on the php conversion
            if(!args.Any()) {
                return format;
            }
            for(var i = 0; i < args.Length; i++) {
                var v = args[i];
                if(v is DateTime) {
                    args[i] = ((DateTime)v).ToString(Culture);
                }
                var r = args[i] as DekiResource;
                if(r == null) {
                    continue;
                }
                args[i] = Localize(r);
            }
            return string.Format(format, args);
        }

        private string GetLocalizedFormat(string resourceKey, bool acceptNull) {

            // NOTE (royk): it's possible to pass in a key with embedded javascript from $_GET, which 
            //              creates a XSS vulnerability, so let's not pass back this information
            var format = _resourceManager.GetString(resourceKey, Culture, null);
            if(format != null || acceptNull) {
                return format;
            }
            return string.Format("[MISSING: {0}]", resourceKey.ReplaceAll("&", "&amp;", "<", "&lt;", ">", "&gt;")).EscapeString();
        }
    }
    // ReSharper restore InconsistentNaming
}

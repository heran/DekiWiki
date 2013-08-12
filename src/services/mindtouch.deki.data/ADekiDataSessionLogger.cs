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
using MindTouch.Deki.Search;
using MindTouch.Dream;
using MindTouch.Xml;

namespace MindTouch.Deki.Data {
    public abstract class ADekiDataSessionLogger : IDekiDataSession, IDekiDataStats {

        //--- Constants ---
        private const string CATEGORY_PAGE = "PageDA";
        private const string CATEGORY_USER = "UserDA";
        private const string CATEGORY_LINKS = "LinksDA";
        private const string CATEGORY_ROLES_RESTRICTIONS = "RolesRestrictionsDA";
        private const string CATEGORY_RESOURCE = "ResourceDA";
        private const string CATEGORY_BANS = "BansDA";
        private const string CATEGORY_GRANTS = "GrantsDA";
        private const string CATEGORY_GROUPS = "GroupsDA";
        private const string CATEGORY_SERVICES = "ServiceDA";
        private const string CATEGORY_TAGS = "TagsDA";
        private const string CATEGORY_RC = "RecentChangesDA";
        private const string CATEGORY_NAV = "NavDA";
        private const string CATEGORY_OLD = "OldDA";
        private const string CATEGORY_COMMENTS = "CommentsDA";
        private const string CATEGORY_ARCHIVE = "ArchiveDA";
        private const string CATEGORY_TRANSACTIONS = "TransactionsDA";
        private const string CATEGORY_RATINGS = "RatingsDA";
        private const string CATEGORY_MISC = "MISC";

        //--- Fields ---
        private readonly IDekiDataSession _next;
        private IDekiDataSession _head;

        //--- Constructors ---
        protected ADekiDataSessionLogger(IDekiDataSession nextSession) {
            if(nextSession == null) {
                throw new ArgumentNullException("nextSession");
            }
            _next = nextSession;
            Head = this;
        }

        //--- Properties ---
        public IDekiDataSession Head {
            get {
                return _head;
            }
            set {
                _head = value;
                _next.Head = value;
            }
        }

        public IDekiDataSession Next {
            get {
                return _next;
            }
        }

        //--- Abstract Methods ---
        protected abstract void LogQuery(string category, string function, Stopwatch sw, params object[] parameters);

        //--- Methods ---
        public void Dispose() {
            _next.Dispose();
        }

        #region IDekiDataSession methods

        #region archive
        public void Archive_Delete(IList<uint> archiveIds) {
            _next.Archive_Delete(archiveIds);
        }

        public uint Archive_GetCountByTitle(Title title) {
            Stopwatch sw = Stopwatch.StartNew();
            var ret = _next.Archive_GetCountByTitle(title);
            LogQuery(CATEGORY_ARCHIVE, "Archive_GetCountByTitle", sw, "title", title);
            return ret;
        }

        public ArchiveBE Archive_GetPageHeadById(ulong pageId) {
            Stopwatch sw = Stopwatch.StartNew();
            var ret = _next.Archive_GetPageHeadById(pageId);
            LogQuery(CATEGORY_ARCHIVE, "Archive_GetPageHeadById", sw, "pageId", pageId);
            return ret;
        }

        public IList<KeyValuePair<uint, IList<ArchiveBE>>> Archive_GetPagesByTitleTransactions(Title title, uint? offset, uint? limit, out Dictionary<uint, TransactionBE> transactionsById, out uint? queryTotalTransactionCount) {
            Stopwatch sw = Stopwatch.StartNew();
            var ret = _next.Archive_GetPagesByTitleTransactions(title, offset, limit, out transactionsById, out queryTotalTransactionCount);
            LogQuery(CATEGORY_ARCHIVE, "Archive_GetPagesByTitleTransactions", sw, "title", title, "offset", offset, "limit", limit);
            return ret;
        }

        public IList<ArchiveBE> Archive_GetPagesInTransaction(ulong pageId, out uint transactionId) {
            Stopwatch sw = Stopwatch.StartNew();
            var ret = _next.Archive_GetPagesInTransaction(pageId, out transactionId);
            LogQuery(CATEGORY_ARCHIVE, "Archive_GetPagesInTransaction", sw, "pageId", pageId);
            return ret;
        }

        public Dictionary<ulong, IList<ArchiveBE>> Archive_GetRevisionsByPageIds(IList<ulong> pageIds) {
            Stopwatch sw = Stopwatch.StartNew();
            var ret = _next.Archive_GetRevisionsByPageIds(pageIds);
            LogQuery(CATEGORY_ARCHIVE, "Archive_GetRevisionsByPageIds", sw, "pageIds", pageIds);
            return ret;
        }

        public void Archive_MovePagesTo(IList<ulong> pageIdsToArchive, uint transactionId) {
            Stopwatch sw = Stopwatch.StartNew();
            _next.Archive_MovePagesTo(pageIdsToArchive, transactionId);
            LogQuery(CATEGORY_ARCHIVE, "Archive_MovePagesTo", sw, "pageIdsToArchive", pageIdsToArchive, "transactionId", transactionId);
        }

        #endregion
        #region bans
        public void Bans_Delete(uint banid) {
            Stopwatch sw = Stopwatch.StartNew();
            _next.Bans_Delete(banid);
            LogQuery(CATEGORY_BANS, "Bans_Delete", sw, "banid", banid);
        }

        public IList<BanBE> Bans_GetAll() {
            Stopwatch sw = Stopwatch.StartNew();
            var ret = _next.Bans_GetAll();
            LogQuery(CATEGORY_BANS, "Bans_GetAll", sw);
            return ret;
        }

        public IList<BanBE> Bans_GetByRequest(uint userid, IList<string> ips) {
            Stopwatch sw = Stopwatch.StartNew();
            var ret = _next.Bans_GetByRequest(userid, ips);
            LogQuery(CATEGORY_BANS, "Bans_GetByRequest", sw, "userid", userid, "ips", ips);
            return ret;
        }

        public uint Bans_Insert(BanBE ban) {
            Stopwatch sw = Stopwatch.StartNew();
            var ret = _next.Bans_Insert(ban);
            LogQuery(CATEGORY_BANS, "Bans_Insert", sw, "ban", ban);
            return ret;
        }
        #endregion bans
        #region comments

        public CommentBE Comments_GetById(uint commentid) {
            Stopwatch sw = Stopwatch.StartNew();
            var ret = _next.Comments_GetById(commentid);
            LogQuery(CATEGORY_COMMENTS, "Comments_GetById", sw, "commentid", commentid);
            return ret;
        }

        public IList<CommentBE> Comments_GetByPage(PageBE page, CommentFilter searchStatus, bool includePageDescendants, uint? postedByUserId, SortDirection datePostedSortDir, uint? offset, uint? limit, out uint totalComments) {
            Stopwatch sw = Stopwatch.StartNew();
            var ret = _next.Comments_GetByPage(page, searchStatus, includePageDescendants, postedByUserId, datePostedSortDir, offset, limit, out totalComments);
            LogQuery(CATEGORY_COMMENTS, "Comments_GetByPage", sw, "page", page, "searchStatus", searchStatus, "includePageDescendants", includePageDescendants, "postedByUserId", postedByUserId, "datePostedSortDir", datePostedSortDir, "offset", offset, "limit", limit);
            return ret;
        }

        public CommentBE Comments_GetByPageIdNumber(ulong pageid, ushort commentNumber) {
            Stopwatch sw = Stopwatch.StartNew();
            var ret = _next.Comments_GetByPageIdNumber(pageid, commentNumber);
            LogQuery(CATEGORY_COMMENTS, "Comments_GetByPageIdNumber", sw, "pageid", pageid, "commentNumber", commentNumber);
            return ret;
        }

        public IList<CommentBE> Comments_GetByUser(uint userId) {
            Stopwatch sw = Stopwatch.StartNew();
            var ret = _next.Comments_GetByUser(userId);
            LogQuery(CATEGORY_COMMENTS, "Comments_GetByUser", sw, "userId", userId);
            return ret;
        }

        public uint Comments_GetCountByPageId(ulong pageid) {
            Stopwatch sw = Stopwatch.StartNew();
            var ret = _next.Comments_GetCountByPageId(pageid);
            LogQuery(CATEGORY_COMMENTS, "Comments_GetCountByPageId", sw, "pageid", pageid);
            return ret;
        }

        public uint Comments_Insert(CommentBE comment, out ushort commentNumber) {
            Stopwatch sw = Stopwatch.StartNew();
            var ret = _next.Comments_Insert(comment, out commentNumber);
            LogQuery(CATEGORY_COMMENTS, "Comments_Insert", sw, "comment", comment);
            return ret;
        }

        public void Comments_Update(CommentBE comment) {
            Stopwatch sw = Stopwatch.StartNew();
            _next.Comments_Update(comment);
            LogQuery(CATEGORY_COMMENTS, "Comments_Update", sw, "comment", comment);
        }
        #endregion
        #region config
        public IList<KeyValuePair<string, ConfigValue>> Config_ReadInstanceSettings() {
            Stopwatch sw = Stopwatch.StartNew();
            var ret = _next.Config_ReadInstanceSettings();
            LogQuery(CATEGORY_MISC, "Config_ReadInstanceSettings", sw);
            return ret;
        }

        public void Config_WriteInstanceSettings(IList<KeyValuePair<string, string>> keyValues) {
            Stopwatch sw = Stopwatch.StartNew();
            _next.Config_WriteInstanceSettings(keyValues);
            LogQuery(CATEGORY_MISC, "Config_WriteInstanceSettings", sw);
        }
        #endregion
        #region grants
        public Dictionary<ulong, PermissionStruct> Grants_CalculateEffectiveForPages(uint userId, IEnumerable<ulong> pageIds) {
            Stopwatch sw = Stopwatch.StartNew();
            var ret = _next.Grants_CalculateEffectiveForPages(userId, pageIds);
            LogQuery(CATEGORY_GRANTS, "Grants_CalculateEffectiveForPages", sw, "userId", userId, "pageIds", pageIds);
            return ret;
        }

        public Dictionary<uint, PermissionStruct> Grants_CalculateEffectiveForUsers(ulong pageId, IEnumerable<uint> userIds) {
            Stopwatch sw = Stopwatch.StartNew();
            var ret = _next.Grants_CalculateEffectiveForUsers(pageId, userIds);
            LogQuery(CATEGORY_GRANTS, "Grants_CalculateEffectiveForUsers", sw, "pageId", pageId, "userIds", userIds);
            return ret;
        }
        public void Grants_CopyToPage(ulong sourcePage, ulong targetPage) {
            Stopwatch sw = Stopwatch.StartNew();
            _next.Grants_CopyToPage(sourcePage, targetPage);
            LogQuery(CATEGORY_GRANTS, "Grants_CopyToPages", sw, "sourcePage", sourcePage, "targetPage", targetPage);
        }

        public IList<GrantBE> Grants_GetByPage(uint pageid) {
            Stopwatch sw = Stopwatch.StartNew();
            var ret = _next.Grants_GetByPage(pageid);
            LogQuery(CATEGORY_GRANTS, "Grants_GetByPage", sw, "pageid", pageid);
            return ret;
        }

        public void Grants_Delete(IList<uint> userGrantIds, IList<uint> groupGrantIds) {
            Stopwatch sw = Stopwatch.StartNew();
            _next.Grants_Delete(userGrantIds, groupGrantIds);
            LogQuery(CATEGORY_GRANTS, "Grants_Delete", sw, "userGrantIds", userGrantIds, "groupGrantIds", groupGrantIds);
        }

        public void Grants_DeleteByPage(IList<ulong> pageIds) {
            Stopwatch sw = Stopwatch.StartNew();
            _next.Grants_DeleteByPage(pageIds);
            LogQuery(CATEGORY_GRANTS, "Grants_DeleteByPage", sw, "pageIds", pageIds);
        }

        public uint Grants_Insert(GrantBE grant) {
            Stopwatch sw = Stopwatch.StartNew();
            var ret = _next.Grants_Insert(grant);
            LogQuery(CATEGORY_GRANTS, "Grants_Insert", sw, "grant", grant);
            return ret;
        }

        #endregion
        #region groups
        public void Groups_Delete(uint groupId) {
            Stopwatch sw = Stopwatch.StartNew();
            _next.Groups_Delete(groupId);
            LogQuery(CATEGORY_GROUPS, "Groups_Delete", sw, "groupId", groupId);
        }

        public IList<GroupBE> Groups_GetByIds(IList<uint> groupIds) {
            Stopwatch sw = Stopwatch.StartNew();
            var ret = _next.Groups_GetByIds(groupIds);
            LogQuery(CATEGORY_GROUPS, "Groups_GetByIds", sw, "groupIds", groupIds);
            return ret;

        }

        public IList<GroupBE> Groups_GetByNames(IList<string> groupNames) {
            Stopwatch sw = Stopwatch.StartNew();
            var ret = _next.Groups_GetByNames(groupNames);
            LogQuery(CATEGORY_GROUPS, "Groups_GetByNames", sw, "groupNames", groupNames);
            return ret;

        }

        public IList<GroupBE> Groups_GetByQuery(string groupNameFilter, uint? serviceIdFilter, SortDirection sortDir, GroupsSortField sortField, uint? offset, uint? limit, out uint totalCount, out uint queryCount) {
            Stopwatch sw = Stopwatch.StartNew();
            var ret = _next.Groups_GetByQuery(groupNameFilter, serviceIdFilter, sortDir, sortField, offset, limit, out totalCount, out queryCount);
            LogQuery(CATEGORY_GROUPS, "Groups_GetByQuery", sw, "groupnamefilter", groupNameFilter, "serviceIdFilter", serviceIdFilter, "sortDir", sortDir, "sortField", sortField, "offset", offset, "limit", limit);
            return ret;
        }

        public IList<GroupBE> Groups_GetByUser(uint userId) {
            Stopwatch sw = Stopwatch.StartNew();
            var ret = _next.Groups_GetByUser(userId);
            LogQuery(CATEGORY_GROUPS, "Groups_GetByUser", sw, "userId", userId);
            return ret;
        }

        public uint Groups_Insert(GroupBE group) {
            Stopwatch sw = Stopwatch.StartNew();
            var ret = _next.Groups_Insert(group);
            LogQuery(CATEGORY_GROUPS, "Groups_Insert", sw, "group", group);
            return ret;
        }

        public void GroupMembers_UpdateGroupsForUser(uint userId, IList<uint> groupIds) {
            Stopwatch sw = Stopwatch.StartNew();
            _next.GroupMembers_UpdateGroupsForUser(userId, groupIds);
            LogQuery(CATEGORY_GROUPS, "GroupMembers_UpdateGroupsForUser", sw, "userId", userId, "groupIds", groupIds);
        }

        public void GroupMembers_UpdateUsersInGroup(uint groupId, IList<uint> userIds, DateTime timestamp) {
            Stopwatch sw = Stopwatch.StartNew();
            _next.GroupMembers_UpdateUsersInGroup(groupId, userIds, timestamp);
            LogQuery(CATEGORY_GROUPS, "GroupMembers_UpdateUsersInGroup", sw, "groupId", groupId, "userIds", userIds, "timestamp", timestamp);
        }

        public void Groups_Update(GroupBE group) {
            Stopwatch sw = Stopwatch.StartNew();
            _next.Groups_Update(group);
            LogQuery(CATEGORY_GROUPS, "Groups_Update", sw, "group", group);
        }

        public IList<uint> Groups_UpdateServicesToLocal(uint oldServiceId) {
            Stopwatch sw = Stopwatch.StartNew();
            var ret = _next.Groups_UpdateServicesToLocal(oldServiceId);
            LogQuery(CATEGORY_GROUPS, "Groups_UpdateServicesToLocal", sw, "oldServiceId", oldServiceId);
            return ret;
        }

        #endregion
        #region links

        public IList<string> Links_GetBrokenLinks(ulong pageId) {
            Stopwatch sw = Stopwatch.StartNew();
            var ret = _next.Links_GetBrokenLinks(pageId);
            LogQuery(CATEGORY_LINKS, "Links_GetBrokenLinks", sw, "pageId", pageId);
            return ret;
        }

        public IList<KeyValuePair<ulong, Title>> Links_GetInboundLinks(ulong pageId) {
            Stopwatch sw = Stopwatch.StartNew();
            var ret = _next.Links_GetInboundLinks(pageId);
            LogQuery(CATEGORY_LINKS, "Links_GetInboundLinks", sw, "pageId", pageId);
            return ret;
        }

        public IList<KeyValuePair<ulong, Title>> Links_GetOutboundLinks(ulong pageId) {
            Stopwatch sw = Stopwatch.StartNew();
            var ret = _next.Links_GetOutboundLinks(pageId);
            LogQuery(CATEGORY_LINKS, "Links_GetOutboundLinks", sw, "pageId", pageId);
            return ret;
        }

        public void Links_MoveInboundToBrokenLinks(IList<ulong> deletedPageIds) {
            Stopwatch sw = Stopwatch.StartNew();
            _next.Links_MoveInboundToBrokenLinks(deletedPageIds);
            LogQuery(CATEGORY_LINKS, "Links_MoveInboundToBrokenLinks", sw, "deletedPageIds", deletedPageIds);
        }

        public void Links_UpdateLinksForPage(PageBE page, IList<ulong> outboundLinks, IList<string> brokenLinks) {
            Stopwatch sw = Stopwatch.StartNew();
            _next.Links_UpdateLinksForPage(page, outboundLinks, brokenLinks);
            LogQuery(CATEGORY_LINKS, "Links_UpdateLinksForPage", sw, "page", page, "outboundLinks", outboundLinks, "brokenLinks", brokenLinks);
        }

        #endregion
        #region nav
        public IList<NavBE> Nav_GetChildren(PageBE page) {
            Stopwatch sw = Stopwatch.StartNew();
            var ret = _next.Nav_GetChildren(page);
            LogQuery(CATEGORY_NAV, "Nav_GetChildren", sw, "page", page);
            return ret;
        }

        public ulong Nav_GetNearestParent(Title title) {
            Stopwatch sw = Stopwatch.StartNew();
            var ret = _next.Nav_GetNearestParent(title);
            LogQuery(CATEGORY_NAV, "Nav_GetNearestParent", sw, "title", title);
            return ret;
        }

        public IList<NavBE> Nav_GetSiblings(PageBE page) {
            Stopwatch sw = Stopwatch.StartNew();
            var ret = _next.Nav_GetSiblings(page);
            LogQuery(CATEGORY_NAV, "Nav_GetSiblings", sw, "page", page);
            return ret;
        }

        public IList<NavBE> Nav_GetSiblingsAndChildren(PageBE page) {
            Stopwatch sw = Stopwatch.StartNew();
            var ret = _next.Nav_GetSiblingsAndChildren(page);
            LogQuery(CATEGORY_NAV, "Nav_GetSiblingsAndChildren", sw, "page", page);
            return ret;
        }

        public IList<NavBE> Nav_GetTree(PageBE page, bool includeAllChildren) {
            Stopwatch sw = Stopwatch.StartNew();
            var ret = _next.Nav_GetTree(page, includeAllChildren);
            LogQuery(CATEGORY_NAV, "Nav_GetTree", sw, "page", page, "includeAllChildren", includeAllChildren);
            return ret;
        }
        #endregion
        #region old
        public OldBE Old_GetOldByTimestamp(ulong pageId, DateTime timestamp) {
            Stopwatch sw = Stopwatch.StartNew();
            var ret = _next.Old_GetOldByTimestamp(pageId, timestamp);
            LogQuery(CATEGORY_OLD, "Old_GetOldByTimestamp", sw, "pageId", pageId, "timestamp", timestamp);
            return ret;
        }

        public OldBE Old_GetOldByRevision(ulong pageId, ulong revision) {
            Stopwatch sw = Stopwatch.StartNew();
            var ret = _next.Old_GetOldByRevision(pageId, revision);
            LogQuery(CATEGORY_OLD, "Old_GetOldByRevision", sw, "pageId", pageId, "revision", revision);
            return ret;
        }

        public IList<OldBE> Old_GetOldsByQuery(ulong pageId, bool orderDescendingByRev, uint? offset, uint? limit) {
            Stopwatch sw = Stopwatch.StartNew();
            var ret = _next.Old_GetOldsByQuery(pageId, orderDescendingByRev, offset, limit);
            LogQuery(CATEGORY_OLD, "Old_GetOldsByQuery", sw, "pageId", pageId, "orderDescendingByRev", orderDescendingByRev, "offset", offset, "limit", limit);
            return ret;
        }

        public uint Old_Insert(OldBE oldPage, ulong restoredOldID) {
            Stopwatch sw = Stopwatch.StartNew();
            var ret = _next.Old_Insert(oldPage, restoredOldID);
            LogQuery(CATEGORY_OLD, "Old_Insert", sw, "oldPage", oldPage, "restoredOldID", restoredOldID);
            return ret;
        }

        public void Old_Update(OldBE oldPage) {
            Stopwatch sw = Stopwatch.StartNew();
            _next.Old_Update(oldPage);
            LogQuery(CATEGORY_OLD, "Old_Update", sw, "oldPage", oldPage);
        }
        #endregion
        #region pages

        public IList<PageBE> Pages_GetByIds(IEnumerable<ulong> pageIds) {
            Stopwatch sw = Stopwatch.StartNew();
            var ret = _next.Pages_GetByIds(pageIds);
            LogQuery(CATEGORY_PAGE, "Pages_GetByIds", sw, "pageIds", pageIds);
            return ret;
        }

        public IList<PageBE> Pages_GetByNamespaces(IList<NS> namespaces, uint? offset, uint? limit) {
            Stopwatch sw = Stopwatch.StartNew();
            var ret = _next.Pages_GetByNamespaces(namespaces, offset, limit);
            LogQuery(CATEGORY_PAGE, "Pages_GetByNamespaces", sw, "namespaces", namespaces, "offset", offset, "limit", limit);
            return ret;
        }

        public IList<PageBE> Pages_GetByTitles(IList<Title> pageTitles) {
            Stopwatch sw = Stopwatch.StartNew();
            var ret = _next.Pages_GetByTitles(pageTitles);
            LogQuery(CATEGORY_PAGE, "Pages_GetByTitles", sw, "pageTitles", pageTitles);
            return ret;
        }

        public IList<PageBE> Pages_GetChildren(ulong parentPageId, NS nameSpace, bool filterOutRedirects) {
            Stopwatch sw = Stopwatch.StartNew();
            var ret = _next.Pages_GetChildren(parentPageId, nameSpace, filterOutRedirects);
            LogQuery(CATEGORY_PAGE, "Pages_GetChildren", sw, "parentPageId", parentPageId, "nameSpace", nameSpace, "filterOutRedirects", filterOutRedirects);
            return ret;
        }

        public uint Pages_GetCount() {
            Stopwatch sw = Stopwatch.StartNew();
            var ret = _next.Pages_GetCount();
            LogQuery(CATEGORY_PAGE, "Pages_GetCount", sw);
            return ret;
        }

        public void Pages_GetDescendants(PageBE rootPage, string language, bool filterOutRedirects, out IList<PageBE> pages, out Dictionary<ulong, IList<ulong>> childrenInfo, int limit) {
            Stopwatch sw = Stopwatch.StartNew();
            _next.Pages_GetDescendants(rootPage, language, filterOutRedirects, out pages, out childrenInfo, limit);
            LogQuery(CATEGORY_PAGE, "Pages_GetDescendants", sw, "rootPage", rootPage, "language", language, "filterOutRedirects", filterOutRedirects, "limit", limit);
        }

        public IList<PageBE> Pages_GetFavoritesForUser(uint userid) {
            Stopwatch sw = Stopwatch.StartNew();
            var ret = _next.Pages_GetFavoritesForUser(userid);
            LogQuery(CATEGORY_PAGE, "Pages_GetFavoritesForUser", sw, "userid", userid);
            return ret;
        }

        public Dictionary<Title, ulong> Pages_GetIdsByTitles(IList<Title> pageTitle) {
            Stopwatch sw = Stopwatch.StartNew();
            var ret = _next.Pages_GetIdsByTitles(pageTitle);
            LogQuery(CATEGORY_PAGE, "Pages_GetIdsByTitles", sw, "pageTitles", pageTitle);
            return ret;
        }

        public IList<ulong> Pages_GetParentIds(PageBE page) {
            Stopwatch sw = Stopwatch.StartNew();
            var ret = _next.Pages_GetParentIds(page);
            LogQuery(CATEGORY_PAGE, "Pages_GetParentIds", sw, "page", page);
            return ret;
        }

        public IList<PageBE> Pages_GetPopular(string language, uint? offset, uint? limit) {
            Stopwatch sw = Stopwatch.StartNew();
            var ret = _next.Pages_GetPopular(language, offset, limit);
            LogQuery(CATEGORY_PAGE, "Pages_GetPopular", sw, "language", language, "offset", offset, "limit", limit);
            return ret;
        }

        public Dictionary<ulong, IList<PageBE>> Pages_GetRedirects(IList<ulong> pageIds) {
            Stopwatch sw = Stopwatch.StartNew();
            var ret = _next.Pages_GetRedirects(pageIds);
            LogQuery(CATEGORY_PAGE, "Pages_GetRedirects", sw, "pageIds", pageIds);
            return ret;
        }

        public ulong Pages_HomePageId {
            get {
                Stopwatch sw = Stopwatch.StartNew();
                var ret = _next.Pages_HomePageId;
                LogQuery(CATEGORY_PAGE, "Pages_HomePageId", sw);
                return ret;
            }
        }

        public uint Pages_GetViewCount(ulong pageId) {
            Stopwatch sw = Stopwatch.StartNew();
            var ret = _next.Pages_GetViewCount(pageId);
            LogQuery(CATEGORY_PAGE, "Pages_GetViewCount", sw, "pageId", pageId);
            return ret;
        }

        public uint Pages_UpdateViewCount(ulong pageId) {
            Stopwatch sw = Stopwatch.StartNew();
            var ret = _next.Pages_UpdateViewCount(pageId);
            LogQuery(CATEGORY_PAGE, "Pages_UpdateViewCount", sw, "pageId", pageId);
            return ret;
        }

        public ulong Pages_Insert(PageBE page, ulong restoredPageID) {
            Stopwatch sw = Stopwatch.StartNew();
            var ret = _next.Pages_Insert(page, restoredPageID);
            LogQuery(CATEGORY_PAGE, "Pages_Insert", sw, "page", page, "restoredPageID", restoredPageID);
            return ret;
        }

        public void Pages_Update(PageBE page) {
            Stopwatch sw = Stopwatch.StartNew();
            _next.Pages_Update(page);
            LogQuery(CATEGORY_PAGE, "Pages_Update", sw, "page", page);
        }

        public void Pages_UpdateTitlesForMove(Title currentTitle, ulong newParentId, Title title, DateTime touchedTimestamp) {
            Stopwatch sw = Stopwatch.StartNew();
            _next.Pages_UpdateTitlesForMove(currentTitle, newParentId, title, touchedTimestamp);
            LogQuery(CATEGORY_PAGE, "Pages_UpdateTitlesForMove", sw, "currentTitle", currentTitle, "newParentId", newParentId, "title", title, "touchedTimestamp", touchedTimestamp);
        }

        public IList<PageTextContainer> Pages_GetContents(IList<ulong> pageIds) {
            Stopwatch sw = Stopwatch.StartNew();
            var ret = _next.Pages_GetContents(pageIds);
            LogQuery(CATEGORY_PAGE, "Pages_GetContents", sw, "pageIds", pageIds);
            return ret;
        }
        #endregion
        #region ratings
        public void Rating_Insert(RatingBE rating) {
            Stopwatch sw = Stopwatch.StartNew();
            _next.Rating_Insert(rating);
            LogQuery(CATEGORY_RATINGS, "Rating_Insert", sw, "rating", rating);
        }

        public RatingComputedBE Rating_GetResourceRating(ulong resourceId, RatingBE.Type resourceType) {
            Stopwatch sw = Stopwatch.StartNew();
            var ret = _next.Rating_GetResourceRating(resourceId, resourceType);
            LogQuery(CATEGORY_RATINGS, "Rating_GetResourceRating", sw, "resourceId", resourceId, "resourceType", resourceType);
            return ret;
        }

        public RatingBE Rating_GetUserResourceRating(uint userId, ulong resourceId, RatingBE.Type resourceType) {
            Stopwatch sw = Stopwatch.StartNew();
            var ret = _next.Rating_GetUserResourceRating(userId, resourceId, resourceType);
            LogQuery(CATEGORY_RATINGS, "Rating_GetUserResourceRating", sw, "userId", userId, "resourceId", resourceId, "resourceType", resourceType);
            return ret;
        }

        public void Rating_ResetUserResourceRating(uint userId, ulong resourceId, RatingBE.Type resourceType, DateTime resetTimestamp) {
            Stopwatch sw = Stopwatch.StartNew();
            _next.Rating_ResetUserResourceRating(userId, resourceId, resourceType, resetTimestamp);
            LogQuery(CATEGORY_RATINGS, "Rating_ResetUserResourceRating", sw, "userId", userId, "resourceId", resourceId, "resourceType", resourceType, "resetTimestamp", resetTimestamp);
        }
        #endregion
        #region recentchanges
        public XDoc RecentChanges_GetPageRecentChanges(PageBE page, DateTime since, bool recurse, bool createOnly, uint? limit) {
            Stopwatch sw = Stopwatch.StartNew();
            var ret = _next.RecentChanges_GetPageRecentChanges(page, since, recurse, createOnly, limit);
            LogQuery(CATEGORY_RC, "RecentChanges_GetPageRecentChanges", sw, "page", page, "since", since, "recurse", recurse, "createOnly", createOnly, "limit", limit);
            return ret;
        }

        public XDoc RecentChanges_GetSiteRecentChanges(DateTime since, string language, bool createOnly, NS nsFilter, uint? limit, int maxScanSize) {
            Stopwatch sw = Stopwatch.StartNew();
            var ret = _next.RecentChanges_GetSiteRecentChanges(since, language, createOnly, nsFilter, limit, maxScanSize);
            LogQuery(CATEGORY_RC, "RecentChanges_GetSiteRecentChanges", sw, "since", since, "language", language, "createOnly", createOnly, "nsFilter", nsFilter, "limit", limit, "maxScanSize", maxScanSize);
            return ret;
        }

        public XDoc RecentChanges_GetUserContributionsRecentChanges(uint contributorId, DateTime since, uint? limit) {
            Stopwatch sw = Stopwatch.StartNew();
            var ret = _next.RecentChanges_GetUserContributionsRecentChanges(contributorId, since, limit);
            LogQuery(CATEGORY_RC, "RecentChanges_GetUserContributionsRecentChanges", sw, "contributorId", contributorId, "since", since, "limit", limit);
            return ret;
        }

        public XDoc RecentChanges_GetUserFavoritesRecentChanges(uint favoritesId, DateTime since, uint? limit) {
            Stopwatch sw = Stopwatch.StartNew();
            var ret = _next.RecentChanges_GetUserFavoritesRecentChanges(favoritesId, since, limit);
            LogQuery(CATEGORY_RC, "RecentChanges_GetUserFavoritesRecentChanges", sw, "favoritesId", favoritesId, "since", since, "limit", limit);
            return ret;
        }

        public void RecentChanges_Insert(DateTime timestamp, PageBE page, UserBE user, string comment, ulong lastoldid, RC type, uint movedToNS, string movedToTitle, bool isMinorChange, uint transactionId) {
            Stopwatch sw = Stopwatch.StartNew();
            _next.RecentChanges_Insert(timestamp, page, user, comment, lastoldid, type, movedToNS, movedToTitle, isMinorChange, transactionId);
            LogQuery(CATEGORY_RC, "RecentChanges_Insert", sw, "timestamp", timestamp, "page", page, "user", user, "comment", comment, "lastoldid", lastoldid, "type", type, "movedToNS", movedToNS, "movedToTitle", movedToTitle, "isMinorChange", isMinorChange, "transactionId", transactionId);
        }
        #endregion
        #region resources
        public void Resources_Delete(IList<uint> resourceIds) {
            Stopwatch sw = Stopwatch.StartNew();
            _next.Resources_Delete(resourceIds);
            LogQuery(CATEGORY_RESOURCE, "Resources_Delete", sw, "resourceIds", resourceIds);
        }

        public void Resources_DeleteRevision(uint resourceId, int revision) {
            Stopwatch sw = Stopwatch.StartNew();
            _next.Resources_DeleteRevision(resourceId, revision);
            LogQuery(CATEGORY_RESOURCE, "Resources_DeleteRevision", sw, "resourceId", resourceId, "revision", revision);
        }

        public IList<ResourceBE> Resources_GetByQuery(IList<uint> parentIds, ResourceBE.ParentType? parentType, IList<ResourceBE.Type> resourceTypes, IList<string> names, DeletionFilter deletionStateFilter, bool? populateRevisions, uint? offset, uint? limit) {
            Stopwatch sw = Stopwatch.StartNew();
            var ret = _next.Resources_GetByQuery(parentIds, parentType, resourceTypes, names, deletionStateFilter, populateRevisions, offset, limit);
            LogQuery(CATEGORY_RESOURCE, "Resources_GetByQuery", sw, "parentIds", parentIds, "parentType", parentType, "resourceTypes", resourceTypes, "names", names, "deletionStateFilter", deletionStateFilter, "populateRevisions", populateRevisions, "offset", offset, "limit", limit);
            return ret;
        }

        public IList<ResourceBE> Resources_GetByChangeSet(uint changeSetId, ResourceBE.Type resourceType) {
            Stopwatch sw = Stopwatch.StartNew();
            var ret = _next.Resources_GetByChangeSet(changeSetId, resourceType);
            LogQuery(CATEGORY_RESOURCE, "Resources_GetByChangeSet", sw, "changeSetId", changeSetId, "resourceType", resourceType);
            return ret;
        }

        public ResourceBE Resources_GetByIdAndRevision(uint resourceid, int revision) {
            Stopwatch sw = Stopwatch.StartNew();
            var ret = _next.Resources_GetByIdAndRevision(resourceid, revision);
            LogQuery(CATEGORY_RESOURCE, "Resources_GetByIdAndRevision", sw, "resourceid", resourceid, "revision", revision);
            return ret;
        }

        public Dictionary<Title, ResourceBE> Resources_GetFileResourcesByTitlesWithMangling(IList<Title> fileTitles) {
            Stopwatch sw = Stopwatch.StartNew();
            var ret = _next.Resources_GetFileResourcesByTitlesWithMangling(fileTitles);
            LogQuery(CATEGORY_RESOURCE, "Resources_GetFileResourcesByTitlesWithMangling", sw, "fileTitles", fileTitles);
            return ret;
        }

        public IList<ResourceBE> Resources_GetRevisions(uint resourceId, ResourceBE.ChangeOperations changeTypesFilter, SortDirection sortRevisions, uint? limit) {
            Stopwatch sw = Stopwatch.StartNew();
            var ret = _next.Resources_GetRevisions(resourceId, changeTypesFilter, sortRevisions, limit);
            LogQuery(CATEGORY_RESOURCE, "Resources_GetRevisions", sw, "resourceId", resourceId, "changeTypesFilter", changeTypesFilter, "sortRevisions", sortRevisions, "limit", limit);
            return ret;
        }

        public uint Resources_GetRevisionCount(uint resourceId, ResourceBE.ChangeOperations changeTypesFilter) {
            Stopwatch sw = Stopwatch.StartNew();
            var ret = _next.Resources_GetRevisionCount(resourceId, changeTypesFilter);
            LogQuery(CATEGORY_RESOURCE, "Resources_GetRevisionCount", sw, "resourceId", resourceId, "changeTypesFilter", changeTypesFilter);
            return ret;
        }

        public ResourceBE Resources_SaveRevision(ResourceBE resource) {
            Stopwatch sw = Stopwatch.StartNew();
            var ret = _next.Resources_SaveRevision(resource);
            LogQuery(CATEGORY_RESOURCE, "Resources_SaveRevision", sw, "resource", resource);
            return ret;
        }

        public ResourceBE Resources_UpdateRevision(ResourceBE resource) {
            Stopwatch sw = Stopwatch.StartNew();
            var ret = _next.Resources_UpdateRevision(resource);
            LogQuery(CATEGORY_RESOURCE, "Resources_UpdateRevision", sw, "resource", resource);
            return ret;
        }

        public IList<ResourceIdMapping> ResourceMapping_GetByFileIds(IList<uint> fileIds) {
            Stopwatch sw = Stopwatch.StartNew();
            var ret = _next.ResourceMapping_GetByFileIds(fileIds);
            LogQuery(CATEGORY_RESOURCE, "ResourceMapping_GetByFileIds", sw, "fileIds", fileIds);
            return ret;
        }

        public IList<ResourceIdMapping> ResourceMapping_GetByResourceIds(IList<uint> resourceIds) {
            Stopwatch sw = Stopwatch.StartNew();
            var ret = _next.ResourceMapping_GetByResourceIds(resourceIds);
            LogQuery(CATEGORY_RESOURCE, "ResourceMapping_GetByResourceIds", sw, "resourceIds", resourceIds);
            return ret;
        }

        public ResourceIdMapping ResourceMapping_InsertFileMapping(uint? resourceId) {
            Stopwatch sw = Stopwatch.StartNew();
            var ret = _next.ResourceMapping_InsertFileMapping(resourceId);
            LogQuery(CATEGORY_RESOURCE, "ResourceMapping_InsertFileMapping", sw, "resourceId", resourceId);
            return ret;
        }

        public ResourceIdMapping ResourceMapping_UpdateFileMapping(uint fileId, uint? resourceId) {
            Stopwatch sw = Stopwatch.StartNew();
            var ret = _next.ResourceMapping_UpdateFileMapping(fileId, resourceId);
            LogQuery(CATEGORY_RESOURCE, "ResourceMapping_UpdateFileMapping", sw, "fileId", fileId, "resourceId", resourceId);
            return ret;
        }
        #endregion
        #region rolesrestrictions
        public IList<RoleBE> RolesRestrictions_GetRestrictions() {
            Stopwatch sw = Stopwatch.StartNew();
            var ret = _next.RolesRestrictions_GetRestrictions();
            LogQuery(CATEGORY_ROLES_RESTRICTIONS, "RolesRestrictions_GetRestrictions", sw);
            return ret;
        }

        public IList<RoleBE> RolesRestrictions_GetRoles() {
            Stopwatch sw = Stopwatch.StartNew();
            var ret = _next.RolesRestrictions_GetRoles();
            LogQuery(CATEGORY_ROLES_RESTRICTIONS, "RolesRestrictions_GetRoles", sw);
            return ret;
        }
        public uint RolesRestrictions_InsertRole(RoleBE role) {
            Stopwatch sw = Stopwatch.StartNew();
            var ret = _next.RolesRestrictions_InsertRole(role);
            LogQuery(CATEGORY_ROLES_RESTRICTIONS, "RolesRestrictions_InsertRole", sw, "role", role);
            return ret;
        }

        public void RolesRestrictions_UpdateRole(RoleBE role) {
            Stopwatch sw = Stopwatch.StartNew();
            _next.RolesRestrictions_UpdateRole(role);
            LogQuery(CATEGORY_ROLES_RESTRICTIONS, "RolesRestrictions_UpdateRole", sw, "role", role);
        }
        #endregion
        #region services
        public void Services_Delete(uint serviceId) {
            Stopwatch sw = Stopwatch.StartNew();
            _next.Services_Delete(serviceId);
            LogQuery(CATEGORY_SERVICES, "Services_Delete", sw, "serviceId", serviceId);
        }

        public IList<ServiceBE> Services_GetAll() {
            Stopwatch sw = Stopwatch.StartNew();
            var ret = _next.Services_GetAll();
            LogQuery(CATEGORY_SERVICES, "Services_GetAll", sw);
            return ret;
        }

        public ServiceBE Services_GetById(uint serviceid) {
            Stopwatch sw = Stopwatch.StartNew();
            var ret = _next.Services_GetById(serviceid);
            LogQuery(CATEGORY_SERVICES, "Services_GetById", sw, "serviceid", serviceid);
            return ret;
        }

        public IList<ServiceBE> Services_GetByQuery(string serviceType, SortDirection sortDir, ServicesSortField sortField, uint? offset, uint? limit, out uint totalCount, out uint queryCount) {
            Stopwatch sw = Stopwatch.StartNew();
            var ret = _next.Services_GetByQuery(serviceType, sortDir, sortField, offset, limit, out totalCount, out queryCount);
            LogQuery(CATEGORY_SERVICES, "Services_GetByQuery", sw, "serviceType", serviceType, "sortDir", sortDir, "sortField", sortField, "offset", offset, "limit", limit);
            return ret;
        }

        public uint Services_Insert(ServiceBE service) {
            Stopwatch sw = Stopwatch.StartNew();
            var ret = _next.Services_Insert(service);
            LogQuery(CATEGORY_SERVICES, "Services_Insert", sw, "service", service);
            return ret;
        }

        #endregion
        #region tags

        public IList<TagBE> Tags_GetByQuery(string partialName, TagType type, DateTime from, DateTime to) {
            Stopwatch sw = Stopwatch.StartNew();
            var ret = _next.Tags_GetByQuery(partialName, type, from, to);
            LogQuery(CATEGORY_TAGS, "Tags_GetByQuery", sw, "partialName", partialName, "type", type, "from", from, "to", to);
            return ret;
        }

        public TagBE Tags_GetById(uint tagid) {
            Stopwatch sw = Stopwatch.StartNew();
            var ret = _next.Tags_GetById(tagid);
            LogQuery(CATEGORY_TAGS, "Tags_GetById", sw, "tagid", tagid);
            return ret;
        }

        public TagBE Tags_GetByNameAndType(string tagName, TagType type) {
            Stopwatch sw = Stopwatch.StartNew();
            var ret = _next.Tags_GetByNameAndType(tagName, type);
            LogQuery(CATEGORY_TAGS, "Tags_GetByNameAndType", sw, "tagName", tagName, "type", type);
            return ret;
        }

        public bool Tags_ValidateDefineTagMapping(TagBE tag) {
            Stopwatch sw = Stopwatch.StartNew();
            var ret = _next.Tags_ValidateDefineTagMapping(tag);
            LogQuery(CATEGORY_TAGS, "Tags_ValidateDefineTagMapping", sw, "tag", tag);
            return ret;
        }

        public IList<ulong> Tags_GetPageIds(uint tagid) {
            Stopwatch sw = Stopwatch.StartNew();
            var ret = _next.Tags_GetPageIds(tagid);
            LogQuery(CATEGORY_TAGS, "Tags_GetPageIds", sw, "tagid", tagid);
            return ret;
        }

        public IList<TagBE> Tags_GetByPageId(ulong pageid) {
            Stopwatch sw = Stopwatch.StartNew();
            var ret = _next.Tags_GetByPageId(pageid);
            LogQuery(CATEGORY_TAGS, "Tags_GetByPageId", sw, "pageid", pageid);
            return ret;
        }

        public IDictionary<uint, IEnumerable<ulong>> Tags_GetRelatedPageIds(IEnumerable<uint> tagids) {
            Stopwatch sw = Stopwatch.StartNew();
            var ret = _next.Tags_GetRelatedPageIds(tagids);
            LogQuery(CATEGORY_TAGS, "Tags_GetRelatedPages", sw, "tagids", tagids);
            return ret;
        }

        public uint Tags_Insert(TagBE tag) {
            Stopwatch sw = Stopwatch.StartNew();
            var ret = _next.Tags_Insert(tag);
            LogQuery(CATEGORY_TAGS, "Tags_Insert", sw, "tag", tag);
            return ret;
        }

        public void Tags_Update(TagBE tag) {
            Stopwatch sw = Stopwatch.StartNew();
            _next.Tags_Update(tag);
            LogQuery(CATEGORY_TAGS, "Tags_Update", sw, "tag", tag);
        }
        #endregion

        #region TagMapping
        public void TagMapping_Delete(ulong pageId, IList<uint> tagids) {
            Stopwatch sw = Stopwatch.StartNew();
            _next.TagMapping_Delete(pageId, tagids);
            LogQuery(CATEGORY_TAGS, "TagMapping_Delete", sw, "pageId", pageId, "tagids", tagids);
        }

        public void TagMapping_Insert(ulong pageId, uint tagId) {
            Stopwatch sw = Stopwatch.StartNew();
            _next.TagMapping_Insert(pageId, tagId);
            LogQuery(CATEGORY_TAGS, "TagMapping_Insert", sw, "pageId", pageId, "tagId", tagId);
        }
        #endregion

        #region transactions
        public void Transactions_Delete(uint transId) {
            Stopwatch sw = Stopwatch.StartNew();
            _next.Transactions_Delete(transId);
            LogQuery(CATEGORY_TRANSACTIONS, "Transactions_Delete", sw, "transId", transId);
        }

        public TransactionBE Transactions_GetById(uint transid) {
            Stopwatch sw = Stopwatch.StartNew();
            var ret = _next.Transactions_GetById(transid);
            LogQuery(CATEGORY_TRANSACTIONS, "Transactions_GetById", sw, "transid", transid);
            return ret;
        }

        public uint Transactions_Insert(TransactionBE trans) {
            Stopwatch sw = Stopwatch.StartNew();
            var ret = _next.Transactions_Insert(trans);
            LogQuery(CATEGORY_TRANSACTIONS, "Transactions_Insert", sw, "trans", trans);
            return ret;
        }

        public void Transactions_Update(TransactionBE trans) {
            Stopwatch sw = Stopwatch.StartNew();
            _next.Transactions_Update(trans);
            LogQuery(CATEGORY_TRANSACTIONS, "Transactions_Update", sw, "trans", trans);
        }
        #endregion
        #region users

        public UserBE Users_GetByExternalName(string externalUserName, uint serviceId) {
            Stopwatch sw = Stopwatch.StartNew();
            var ret = _next.Users_GetByExternalName(externalUserName, serviceId);
            LogQuery(CATEGORY_USER, "Users_GetByExternalName", sw, "externalUserName", externalUserName, "serviceId", serviceId);
            return ret;
        }

        public IEnumerable<UserBE> Users_GetByIds(IEnumerable<uint> userIds) {
            Stopwatch sw = Stopwatch.StartNew();
            var ret = _next.Users_GetByIds(userIds);
            LogQuery(CATEGORY_USER, "Users_GetByIds", sw, "userIds", userIds);
            return ret;
        }

        public UserBE Users_GetByName(string userName) {
            Stopwatch sw = Stopwatch.StartNew();
            var ret = _next.Users_GetByName(userName);
            LogQuery(CATEGORY_USER, "Users_GetByName", sw, "userName", userName);
            return ret;
        }

        public IEnumerable<UserBE> Users_GetByQuery(string userNameFilter, string realNameFilter, string userNameEmailFilter, string roleFilter, bool? activatedFilter, uint? groupId, uint? serviceIdFilter, bool? seatFilter, SortDirection sortDir, UsersSortField sortField, uint? offset, uint? limit, out uint totalCount, out uint queryCount) {
            Stopwatch sw = Stopwatch.StartNew();
            var ret = _next.Users_GetByQuery(userNameFilter, realNameFilter, userNameEmailFilter, roleFilter, activatedFilter, groupId, serviceIdFilter, seatFilter, sortDir, sortField, offset, limit, out totalCount, out queryCount);
            LogQuery(CATEGORY_USER, "Users_GetByQuery", sw, "userNameFilter", userNameFilter, "realNameFilter", realNameFilter, "userNameEmailFilter", userNameEmailFilter, "roleFilter", roleFilter, "activatedFilter", activatedFilter, "groupId", groupId, "serviceIdFilter", serviceIdFilter, "seatFilter", seatFilter, "sortDir", sortDir, "sortField", sortField, "offset", offset, "limit", limit);
            return ret;
        }

        public IEnumerable<UserBE> Users_GetActiveUsers() {
            var sw = Stopwatch.StartNew();
            var ret = _next.Users_GetActiveUsers();
            LogQuery(CATEGORY_USER, "Users_GetActiveUsers", sw);
            return ret;
        }

        public IEnumerable<UserBE> Users_GetBySeat(bool seated) {
            var sw = Stopwatch.StartNew();
            var ret = _next.Users_GetBySeat(seated);
            LogQuery(CATEGORY_USER, "Users_GetBySeat", sw, "seated", seated);
            return ret;
        }

        public uint Users_GetCount() {
            Stopwatch sw = Stopwatch.StartNew();
            var ret = _next.Users_GetCount();
            LogQuery(CATEGORY_USER, "Users_GetCount", sw);
            return ret;
        }

        public uint Users_Insert(UserBE newUser) {
            Stopwatch sw = Stopwatch.StartNew();
            var ret = _next.Users_Insert(newUser);
            LogQuery(CATEGORY_USER, "Users_Insert", sw, "newUser", newUser);
            return ret;
        }

        public void Users_Update(UserBE user) {
            Stopwatch sw = Stopwatch.StartNew();
            _next.Users_Update(user);
            LogQuery(CATEGORY_USER, "Users_Update", sw, "user", user);
        }

        public IEnumerable<uint> Users_UpdateServicesToLocal(uint oldServiceId) {
            Stopwatch sw = Stopwatch.StartNew();
            var ret = _next.Users_UpdateServicesToLocal(oldServiceId);
            LogQuery(CATEGORY_USER, "Users_UpdateServicesToLocal", sw, "oldServiceId", oldServiceId);
            return ret;
        }

        public UserMetrics Users_GetUserMetrics(uint userId) {
            Stopwatch sw = Stopwatch.StartNew();
            var ret = _next.Users_GetUserMetrics(userId);
            LogQuery(CATEGORY_USER, "Users_GetUserMetrics", sw, "userId", userId);
            return ret;
        }

        #endregion
        #region misc
        public void RequestLog_Insert(XUri requestUri, string requestVerb, string requestHostHeader, string origin, string serviceHost, string serviceFeature, DreamStatus responseStatus, string username, uint executionTime, string response) {
            _next.RequestLog_Insert(requestUri, requestVerb, requestHostHeader, origin, serviceHost, serviceFeature, responseStatus, username, executionTime, response);
        }
        public IList<Tuplet<string, uint, string>> Wiki_GetContributors(PageBE page, bool byRecent, string exclude, uint? limit) {
            Stopwatch sw = Stopwatch.StartNew();
            var ret = _next.Wiki_GetContributors(page, byRecent, exclude, limit);
            LogQuery(CATEGORY_USER, "Wiki_GetContributors", sw, "page", page, "byRecent", byRecent, "exclude", exclude, "limit", limit);
            return ret;
        }

        #endregion
        #region SearchAnalytics
        public ulong SearchAnalytics_LogQuery(SearchQuery query, string parsedQuery, uint userId, uint resultCount, ulong? previousQueryId) {
            return _next.SearchAnalytics_LogQuery(query, parsedQuery, userId, resultCount, previousQueryId);
        }

        public void SearchAnalytics_LogQueryPick(ulong queryId, double rank, ushort position, uint pageId, SearchResultType type, uint typeId) {
            _next.SearchAnalytics_LogQueryPick(queryId, rank, position, pageId, type, typeId);
        }

        public void SearchAnalytics_UpdateQueryPopularityAggregate(ulong queryId) {
            _next.SearchAnalytics_UpdateQueryPopularityAggregate(queryId);
        }

        public IEnumerable<ResultPopularityBE> SearchAnalytics_GetPopularityRanking(string termHash) {
            return _next.SearchAnalytics_GetPopularityRanking(termHash);
        }

        public IEnumerable<LoggedSearchBE> SearchAnalytics_GetTrackedQueries(string querystring, SearchAnalyticsQueryType type, DateTime since, DateTime before, uint? limit, uint? offset) {
            return _next.SearchAnalytics_GetTrackedQueries(querystring, type, since, before, limit, offset);
        }

        public LoggedSearchBE SearchAnalytics_GetTrackedQuery(ulong queryId) {
            return _next.SearchAnalytics_GetTrackedQuery(queryId);
        }

        public IEnumerable<TermAggregateBE> SearchAnalytics_GetTerms(bool lowQuality, DateTime since, DateTime before, uint limit, uint offset) {
            return _next.SearchAnalytics_GetTerms(lowQuality, since, before, limit, offset);
        }

        public IEnumerable<string> SearchAnalytics_GetPreviousSortedQueryTermsRecursively(IEnumerable<ulong> queryIds) {
            return _next.SearchAnalytics_GetPreviousSortedQueryTermsRecursively(queryIds);
        }
        #endregion
        #endregion

        #region --- IDekiDataStats Members ---
        Dictionary<string, string> IDekiDataStats.GetStats() {
            IDekiDataStats statSession = _next as IDekiDataStats;
            Dictionary<string, string> ret = (statSession == null) ? new Dictionary<string, string>() : statSession.GetStats();
            return ret;
        }
        #endregion
    }
}

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
using MindTouch.Deki.Search;
using MindTouch.Dream;
using MindTouch.Xml;

namespace MindTouch.Deki.Data {
    public interface IDekiDataSession : IDisposable {

        //--- Properties ---
        IDekiDataSession Head { get; set; }
        IDekiDataSession Next { get; }

        #region Archive
        void Archive_Delete(IList<uint> archiveIds);
        uint Archive_GetCountByTitle(Title title);
        ArchiveBE Archive_GetPageHeadById(ulong pageId);
        IList<KeyValuePair<uint, IList<ArchiveBE>>> Archive_GetPagesByTitleTransactions(Title title, uint? offset, uint? limit, out Dictionary<uint, TransactionBE> transactionsById, out uint? queryTotalTransactionCount);
        IList<ArchiveBE> Archive_GetPagesInTransaction(ulong pageId, out uint transactionId);
        Dictionary<ulong, IList<ArchiveBE>> Archive_GetRevisionsByPageIds(IList<ulong> pageIds);
        void Archive_MovePagesTo(IList<ulong> pageIdsToArchive, uint transactionId);
        #endregion

        #region Bans
        void Bans_Delete(uint banId);
        IList<BanBE> Bans_GetAll();
        IList<BanBE> Bans_GetByRequest(uint userid, IList<string> ips);
        uint Bans_Insert(BanBE ban);
        #endregion

        #region Comments
        CommentBE Comments_GetById(uint commentId);
        IList<CommentBE> Comments_GetByPage(PageBE page, CommentFilter searchStatus, bool includePageDescendants, uint? postedByUserId, SortDirection datePostedSortDir, uint? offset, uint? limit, out uint totalComments);
        CommentBE Comments_GetByPageIdNumber(ulong pageId, ushort commentNumber);
        IList<CommentBE> Comments_GetByUser(uint userId);
        uint Comments_GetCountByPageId(ulong pageId);
        uint Comments_Insert(CommentBE comment, out ushort commentNumber);
        void Comments_Update(CommentBE comment);
        #endregion

        #region Config
        IList<KeyValuePair<string, ConfigValue>> Config_ReadInstanceSettings();
        void Config_WriteInstanceSettings(IList<KeyValuePair<string, string>> keyValues);
        #endregion

        #region Grants
        Dictionary<ulong, PermissionStruct> Grants_CalculateEffectiveForPages(uint userId, IEnumerable<ulong> pageIds);
        Dictionary<uint, PermissionStruct> Grants_CalculateEffectiveForUsers(ulong pageId, IEnumerable<uint> userIds);
        void Grants_CopyToPage(ulong sourcePageId, ulong targetPageId);
        void Grants_Delete(IList<uint> userGrantIds, IList<uint> groupGrantIds);
        void Grants_DeleteByPage(IList<ulong> pageIds);
        IList<GrantBE> Grants_GetByPage(uint pageid);
        uint Grants_Insert(GrantBE grant);
        #endregion

        #region GroupMembers
        void GroupMembers_UpdateGroupsForUser(uint userId, IList<uint> groupIds);
        void GroupMembers_UpdateUsersInGroup(uint groupId, IList<uint> userIds, DateTime timestamp);
        #endregion

        #region Groups
        void Groups_Delete(uint groupId);
        IList<GroupBE> Groups_GetByIds(IList<uint> groupIds);
        IList<GroupBE> Groups_GetByNames(IList<string> groupNames);
        IList<GroupBE> Groups_GetByQuery(string groupNameFilter, uint? serviceIdFilter, SortDirection sortDir, GroupsSortField sortField, uint? offset, uint? limit, out uint totalCount, out uint queryCount);
        IList<GroupBE> Groups_GetByUser(uint userId);
        uint Groups_Insert(GroupBE group);
        void Groups_Update(GroupBE group);
        IList<uint> Groups_UpdateServicesToLocal(uint oldServiceId);
        #endregion

        #region Links
        IList<string> Links_GetBrokenLinks(ulong pageId);
        IList<KeyValuePair<ulong, Title>> Links_GetInboundLinks(ulong pageId);
        IList<KeyValuePair<ulong, Title>> Links_GetOutboundLinks(ulong pageId);
        void Links_MoveInboundToBrokenLinks(IList<ulong> deletedPageIds);
        void Links_UpdateLinksForPage(PageBE page, IList<ulong> outboundLinks, IList<string> brokenLinks);
        #endregion

        #region Nav
        IList<NavBE> Nav_GetChildren(PageBE page);
        ulong Nav_GetNearestParent(Title title);
        IList<NavBE> Nav_GetSiblings(PageBE page);
        IList<NavBE> Nav_GetSiblingsAndChildren(PageBE page);
        IList<NavBE> Nav_GetTree(PageBE page, bool includeAllChildren);
        #endregion

        #region Old
        OldBE Old_GetOldByTimestamp(ulong pageId, DateTime timestamp);
        IList<OldBE> Old_GetOldsByQuery(ulong pageId, bool orderDescendingByRev, uint? offset, uint? limit);
        OldBE Old_GetOldByRevision(ulong pageId, ulong revision);
        uint Old_Insert(OldBE oldPage, ulong restoredOldID);
        void Old_Update(OldBE oldPage);
        #endregion

        #region Pages
        IList<PageBE> Pages_GetByIds(IEnumerable<ulong> pageIds);
        IList<PageBE> Pages_GetByNamespaces(IList<NS> namespaces, uint? offset, uint? limit);
        IList<PageBE> Pages_GetByTitles(IList<Title> pageTitles);
        IList<PageBE> Pages_GetChildren(ulong parentPageId, NS nameSpace, bool filterOutRedirects);
        IList<PageTextContainer> Pages_GetContents(IList<ulong> pageIds);
        uint Pages_GetCount();
        void Pages_GetDescendants(PageBE rootPage, string language, bool filterOutRedirects, out IList<PageBE> pages, out Dictionary<ulong, IList<ulong>> childrenInfo, int limit);
        IList<PageBE> Pages_GetFavoritesForUser(uint userid);
        Dictionary<Title, ulong> Pages_GetIdsByTitles(IList<Title> pageTitle);
        IList<ulong> Pages_GetParentIds(PageBE page);
        IList<PageBE> Pages_GetPopular(string language, uint? offset, uint? limit);
        Dictionary<ulong, IList<PageBE>> Pages_GetRedirects(IList<ulong> pageIds);
        ulong Pages_HomePageId { get; }
        ulong Pages_Insert(PageBE page, ulong restoredPageID);
        void Pages_Update(PageBE page);
        void Pages_UpdateTitlesForMove(Title currentTitle, ulong newParentId, Title title, DateTime touchedTimestamp);
        uint Pages_GetViewCount(ulong pageId);
        uint Pages_UpdateViewCount(ulong pageId);
        #endregion

        #region Ratings
        void Rating_Insert(RatingBE rating);
        RatingComputedBE Rating_GetResourceRating(ulong resourceId, RatingBE.Type resourceType);
        RatingBE Rating_GetUserResourceRating(uint userId, ulong resourceId, RatingBE.Type resourceType);
        void Rating_ResetUserResourceRating(uint userId, ulong resourceId, RatingBE.Type resourceType, DateTime resetTimestamp);
        #endregion

        #region RecentChanges
        XDoc RecentChanges_GetPageRecentChanges(PageBE page, DateTime since, bool recurse, bool createOnly, uint? limit);
        XDoc RecentChanges_GetSiteRecentChanges(DateTime since, string language, bool createOnly, NS nsFilter, uint? limit, int maxScanSize);
        XDoc RecentChanges_GetUserContributionsRecentChanges(uint contributorId, DateTime since, uint? limit);
        XDoc RecentChanges_GetUserFavoritesRecentChanges(uint favoritesId, DateTime since, uint? limit);
        void RecentChanges_Insert(DateTime timestamp, PageBE page, UserBE user, string comment, ulong lastoldid, RC type, uint movedToNS, string movedToTitle, bool isMinorChange, uint transactionId);
        #endregion

        #region RequestLog
        void RequestLog_Insert(XUri requestUri, string requestVerb, string requestHostHeader, string origin, string serviceHost, string serviceFeature, DreamStatus responseStatus, string username, uint executionTime, string response);
        #endregion

        #region Resources
        void Resources_Delete(IList<uint> resourceIds);
        void Resources_DeleteRevision(uint resourceId, int revision);
        IList<ResourceBE> Resources_GetByChangeSet(uint changeSetId, ResourceBE.Type resourceType);
        ResourceBE Resources_GetByIdAndRevision(uint resourceid, int revision);
        IList<ResourceBE> Resources_GetByQuery(IList<uint> parentIds, ResourceBE.ParentType? parentType, IList<ResourceBE.Type> resourceTypes, IList<string> names, DeletionFilter deletionStateFilter, bool? populateRevisions, uint? offset, uint? limit);
        Dictionary<Title, ResourceBE> Resources_GetFileResourcesByTitlesWithMangling(IList<Title> fileTitles);
        IList<ResourceBE> Resources_GetRevisions(uint resourceId, ResourceBE.ChangeOperations changeTypesFilter, SortDirection sortRevisions, uint? limit);
        uint Resources_GetRevisionCount(uint resourceId, ResourceBE.ChangeOperations changeTypesFilter);

        // TODO (brigettek): Make these return types consistent with other insert/updates
        ResourceBE Resources_SaveRevision(ResourceBE resource);
        ResourceBE Resources_UpdateRevision(ResourceBE resource);
        #endregion

        #region ResourceMapping
        IList<ResourceIdMapping> ResourceMapping_GetByFileIds(IList<uint> fileIds);
        IList<ResourceIdMapping> ResourceMapping_GetByResourceIds(IList<uint> resourceIds);

        // TODO (brigettek): Make these return types consistent with other insert/updates
        ResourceIdMapping ResourceMapping_InsertFileMapping(uint? resourceId);
        ResourceIdMapping ResourceMapping_UpdateFileMapping(uint fileId, uint? resourceId);
        #endregion

        #region RolesRestrictions
        IList<RoleBE> RolesRestrictions_GetRestrictions();
        IList<RoleBE> RolesRestrictions_GetRoles();
        uint RolesRestrictions_InsertRole(RoleBE role);
        void RolesRestrictions_UpdateRole(RoleBE role);
        #endregion

        #region Services
        void Services_Delete(uint serviceId);
        IList<ServiceBE> Services_GetAll();
        ServiceBE Services_GetById(uint serviceid);
        IList<ServiceBE> Services_GetByQuery(string serviceType, SortDirection sortDir, ServicesSortField sortField, uint? offset, uint? limit, out uint totalCount, out uint queryCount);
        uint Services_Insert(ServiceBE service);
        #endregion

        #region Tags
        TagBE Tags_GetById(uint tagid);
        TagBE Tags_GetByNameAndType(string tagName, TagType type);
        bool Tags_ValidateDefineTagMapping(TagBE tag);
        IList<TagBE> Tags_GetByPageId(ulong pageid);
        IList<TagBE> Tags_GetByQuery(string partialName, TagType type, DateTime from, DateTime to);
        IList<ulong> Tags_GetPageIds(uint tagid);
        void Tags_Update(TagBE tag);
        IDictionary<uint, IEnumerable<ulong>> Tags_GetRelatedPageIds(IEnumerable<uint> tagids);
        uint Tags_Insert(TagBE tag);
        #endregion

        #region TagMapping
        void TagMapping_Delete(ulong pageId, IList<uint> tagids);
        void TagMapping_Insert(ulong pageId, uint tagId);
        #endregion

        #region Transactions
        void Transactions_Delete(uint transId);
        TransactionBE Transactions_GetById(uint transid);
        uint Transactions_Insert(TransactionBE trans);
        void Transactions_Update(TransactionBE trans);
        #endregion

        #region Users
        UserBE Users_GetByExternalName(string externalUserName, uint serviceId);
        IEnumerable<UserBE> Users_GetByIds(IEnumerable<uint> userIds);
        UserBE Users_GetByName(string userName);
        IEnumerable<UserBE> Users_GetByQuery(string userNameFilter, string realNameFilter, string userNameEmailFilter, string roleFilter, bool? activatedFilter, uint? groupId, uint? serviceIdFilter, bool? seatFilter, SortDirection sortDir, UsersSortField sortField, uint? offset, uint? limit, out uint totalCount, out uint queryCount);
        IEnumerable<UserBE> Users_GetActiveUsers();
        IEnumerable<UserBE> Users_GetBySeat(bool seated);
        uint Users_GetCount();
        uint Users_Insert(UserBE newUser);
        void Users_Update(UserBE user);
        IEnumerable<uint> Users_UpdateServicesToLocal(uint oldServiceId);
        UserMetrics Users_GetUserMetrics(uint userId);
        #endregion     

        #region Wiki

        // Tuple consists of the following:  user name, user id, last edit timestamp/user edit count
        IList<Tuplet<string, uint, string>> Wiki_GetContributors(PageBE page, bool byRecent, string exclude, uint? limit);
        #endregion

        #region SearchAnalytics
        ulong SearchAnalytics_LogQuery(SearchQuery query, string parsedQuery, uint userId, uint resultCount, ulong? previousQueryId);
        void SearchAnalytics_LogQueryPick(ulong queryId, double rank, ushort position, uint pageId, SearchResultType type, uint typeId);
        void SearchAnalytics_UpdateQueryPopularityAggregate(ulong queryId);
        IEnumerable<ResultPopularityBE> SearchAnalytics_GetPopularityRanking(string termHash);
        IEnumerable<LoggedSearchBE> SearchAnalytics_GetTrackedQueries(string querystring, SearchAnalyticsQueryType type, DateTime since, DateTime before, uint? limit, uint? offset);
        LoggedSearchBE SearchAnalytics_GetTrackedQuery(ulong queryId);
        IEnumerable<TermAggregateBE> SearchAnalytics_GetTerms(bool lowQuality, DateTime since, DateTime before, uint limit, uint offset);
        IEnumerable<string> SearchAnalytics_GetPreviousSortedQueryTermsRecursively(IEnumerable<ulong> queryIds);
        #endregion
    }
}

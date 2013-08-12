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
using System.Linq;

using MindTouch.Deki.Data;
using MindTouch.Deki.Exceptions;
using MindTouch.Deki.Logic;
using MindTouch.Deki.Util;
using MindTouch.Dream;
using MindTouch.Tasking;
using MindTouch.Xml;

namespace MindTouch.Deki {
    using Yield = IEnumerator<IYield>;

    public partial class DekiWikiService {

        //--- Constants ---
        private const int MAX_RECENT_CHANGES = 500;

        //--- Types ---
        internal class DigestLookupEntry {

            //--- Fields ---
            public DateTime Timestamp;
            public int Index;
            public RC Type;

            //--- Constructors ---
            public DigestLookupEntry(DateTime timestamp, int index, RC type) {
                this.Timestamp = timestamp;
                this.Index = index;
                this.Type = type;
            }
        }

        //--- Class Methods ---
        private static XDoc GetParsedPageRevision(ulong pageId, int revision) {
            try {
                PageBE page = PageBL.GetPageById(pageId);
                PageBL.ResolvePageRev(page, revision.ToString());
                var parserResult = DekiXmlParser.Parse(page, page.ContentType, page.Language, page.GetText(DbUtils.CurrentSession), ParserMode.EDIT, false, -1, null, null);
                return parserResult.Content;
            } catch(Exception e) {
                _log.WarnExceptionMethodCall(e, "QueryPageVersions", string.Format("unable to retrieve page {0} revision {1}", pageId, revision));
                return null;
            }
        }

        private static XDoc QueryPageVersions(ulong pageId, int? afterRevision, int? beforeRevision, IDictionary<string, XDoc> cache) {
            XDoc doc = new XDoc("diff");

            // retrieve 'after' version
            if(afterRevision.HasValue) {
                XDoc contents;
                string key = string.Format("{0}-{1}", pageId, afterRevision);

                // chek if we have a cached version
                if(!cache.TryGetValue(key, out contents)) {

                    // store response (doesn't matter if it was successful or not)
                    cache[key] = contents = GetParsedPageRevision(pageId, afterRevision.Value);
                }
                if(contents != null) {
                    doc.Start("after").Start("body");
                    doc.Elem("h1", contents["@title"].AsText);
                    doc.AddNodes(contents["body"]);
                    doc.End().End();
                }
            }

            // check if 'before' version is expected to be different
            if(beforeRevision.HasValue && (afterRevision != beforeRevision) && (beforeRevision > 0)) {
                XDoc contents;
                string key = string.Format("{0}-{1}", pageId, beforeRevision);

                // chek if we have a cached version
                if(!cache.TryGetValue(key, out contents)) {

                    // store response (doesn't matter if it was successful or not)
                    cache[key] = contents = GetParsedPageRevision(pageId, beforeRevision.Value);
                }
                if(contents != null) {
                    doc.Start("before").Start("body");
                    doc.Elem("h1", contents["@title"].AsText);
                    doc.AddNodes(contents["body"]);
                    doc.End().End();
                }
            }
            return doc;
        }

        private static IEnumerable<RecentChangeEntry> ConvertAndFilterXmlToRecentChanges(XDoc doc, int offset, int count, bool filterByPermissions) {

            // convert xml to a list of recent change entities
            IEnumerable<RecentChangeEntry> changes = (from change in doc["change"]
                                                      let item = RecentChangeBL.FromXml(change)
                                                      where !IsUserPageCreation(item)
                                                      select item).ToList();

            // allow adminstrators access to recent changes from all pages, otherwise filter based on permissions
            if(filterByPermissions && !PermissionsBL.IsUserAllowed(DekiContext.Current.User, Permissions.ADMIN)) {

                // check which pages are visible
                var pages = PageBL.GetPagesByIdsPreserveOrder((from change in changes select change.CurId).Distinct());
                var authorizedPages = PermissionsBL.FilterDisallowed(DekiContext.Current.User, pages, false, Permissions.READ | Permissions.BROWSE);
                var authorizedPagesHash = authorizedPages.ToDictionary(e => e.ID);

                //HACK: deleted pages are only visible by administrators because their restrictions are not preserved as they page from
                //page to archive allowing previously hidden page titles to become visible to anonyone.
                // http://bugs.developer.mindtouch.com/view.php?id=4855

                // if page was authorized or did not exist in the original page list (could have been deleted).
                changes = from change in changes where authorizedPagesHash.ContainsKey(change.CurId) select change;
            }

            // TODO (steveb): we can remove this code if we read the page revision number from the db
            AddRevisionOffsets(changes);

            return (from change in changes where !(change.OldIsHidden && Utils.IsPageEdit(change.Type)) select change).Skip(offset).Take(count);
        }

        private static bool IsUserPageCreation(RecentChangeEntry item) {
            if(!(item.Namespace == NS.USER && item.Type == RC.NEW)  ) {
                return false;
            }
            var title = Title.FromDbPath(item.Namespace, item.Title, null);
            var parent = title.GetParent();
            return parent != null && parent.IsRoot;
        }

        private static void AddRevisionOffsets(IEnumerable<RecentChangeEntry> changes) {
            var pageRevisionOffset = new Dictionary<ulong, int>();
            foreach(var change in changes) {

                // check if page still exists
                if(change.PageExists) {
                    int counter;
                    change.CurrentRevision = change.Revision;

                    // check if we have seen a page-edit operation for this page before
                    if(pageRevisionOffset.TryGetValue(change.CurId, out counter)) {

                        // update revision number to absolute offset
                        change.Revision = change.Revision - counter;
                    }

                    // check if current change was a page edit operation, if so update the revision counter
                    if(Utils.IsPageEdit(change.Type)) {
                        ++counter;
                        pageRevisionOffset[change.CurId] = counter;
                    }
                } else {

                    // mark page as gone
                    change.Revision = 0;
                    change.CurrentRevision = 0;
                }
            }
        }

        private static void ExtractRecentChangesParameters(DreamContext context, out DateTime since, out int limit, out int offset, out FeedFormat format, out string language, out NS ns, ref List<string> feedNameSuffixes) {

            // extract 'since' parameter
            since = DbUtils.ToDateTime(context.GetParam("since", DbUtils.ToString(DateTime.MinValue)));

            // extract 'limit' parameter
            limit = context.GetParam("limit", 100);
            if((limit <= 0) || (limit > MAX_RECENT_CHANGES)) {
                throw new MaxParameterInvalidArgumentException();
            }

            // extract 'offset' parameter
            offset = context.GetParam("offset", 0);
            if(offset < 0) {
                throw new OffsetParameterInvalidArgumentException();
            }

            // extract 'format' parameter
            switch(context.GetParam("format", "daily")) {
            case "raw":
                format = FeedFormat.RAW;
                break;
            case "rawdaily":
            case "dailyraw":
            case "digest":
                format = FeedFormat.RAW_DAILY;
                break;
            case "daily":
            case "atom":
                format = FeedFormat.ATOM_DAILY;
                break;
            case "all":
                format = FeedFormat.ATOM_ALL;
                break;
            default:
                throw new FormatParameterInvalidArgumentException();
            }

            // extract 'language' parameter
            language = context.GetParam("language", null);
            if(null != language) {
                PageBL.ValidatePageLanguage(language);
            }

            // extract 'namespace' parameter
            ns = context.GetParam<NS>("namespace", NS.UNKNOWN);

            // determine feed suffix
            if(feedNameSuffixes == null) {
                feedNameSuffixes = new List<string>();
            }
            if(context.GetParam("format", null) != null) {
                feedNameSuffixes.Add(string.Format("format={0}", context.GetParam("format", null)));
            }
            if(context.GetParam("since", null) != null) {
                feedNameSuffixes.Add(string.Format("since={0}", DbUtils.ToString(since)));
            }
            if(context.GetParam("limit", null) != null) {
                feedNameSuffixes.Add(string.Format("limit={0}", limit));
            }
            if(context.GetParam("offset", null) != null) {
                feedNameSuffixes.Add(string.Format("offset={0}", offset));
            }
            if(context.GetParam("language", null) != null) {
                feedNameSuffixes.Add(string.Format("lang={0}", language));
            }
            if(ns != NS.UNKNOWN) {
                feedNameSuffixes.Add(string.Format("namespace={0}", ns));
            }
        }

        //--- Features ---
        [DreamFeature("GET:site/feed/new", "Retrieve feed of new page creations")] // TODO (steveb): hide this feature signature
        [DreamFeature("GET:site/feed", "Retrieve feed of site changes")]
        [DreamFeatureParam("since", "string?", "Start date for changes.  Date is provided in 'yyyyMMddHHmmss' format (default: ignored).")]
        [DreamFeatureParam("limit", "int?", "Number of changes to retrieve (default: 100)")]
        [DreamFeatureParam("offset", "int?", "Skipped changes (default: 0)")]
        [DreamFeatureParam("format", "{all, daily, raw, rawdaily}?", "Format for feed (default: daily)")]
        [DreamFeatureParam("language", "string?", "Filter results by language (default: all languages)")]
        [DreamFeatureParam("namespace", "string?", "Filter results by namespace (default: all namespace)")]
        [DreamFeatureParam("filter", "string?", "use \"new\" to include only newly created pages in the feed (default: all)")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        [DreamFeatureStatus(DreamStatus.Ok, "The request completed successfully")]
        [DreamFeatureStatus(DreamStatus.BadRequest, "Invalid input parameter or request body")]
        [DreamFeatureStatus(DreamStatus.Forbidden, "Suscribe access to the page is required")]
        public Yield GetSiteChanges(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            var resources = DekiContext.Current.Resources;
            CheckResponseCache(context, true);

            // Note (arnec): the /new signature was introduced as resolution for BUG 5346
            bool createOnly = context.Uri.Path.EndsWith("/new") || context.GetParam("filter", string.Empty).EqualsInvariant("new");
            DateTime since;
            int limit;
            int offset;
            string language;
            NS ns;
            FeedFormat format;
            List<string> feedNameSuffixes = null;
            PermissionsBL.CheckUserAllowed(DekiContext.Current.User, Permissions.SUBSCRIBE);
            ExtractRecentChangesParameters(context, out since, out limit, out offset, out format, out language, out ns, ref feedNameSuffixes);
            var res = MakeNewsFeedCached(() => QuerySiteRecentChanges(since, offset, limit, language, createOnly, ns, true), context.Uri.WithoutCredentials(), resources.Localize(DekiResources.WHATS_NEW(DekiContext.Current.Instance.SiteName)), createOnly ? "site-feed-new" : "site-feed", feedNameSuffixes, format, since);
            response.Return(DreamMessage.Ok(res.Item1, res.Item2));
            yield break;
        }

        [DreamFeature("GET:site/activity", "Retrieve report on site activities")]
        [DreamFeatureParam("since", "string?", "Start date for report.  Date is provided in 'yyyyMMddHHmmss' format (default: last 14 days).")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        [DreamFeatureStatus(DreamStatus.Ok, "The request completed successfully")]
        [DreamFeatureStatus(DreamStatus.BadRequest, "Invalid input parameter or request body")]
        [DreamFeatureStatus(DreamStatus.Forbidden, "Suscribe access to the page is required")]
        public Yield GetSiteActivities(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            var resources = DekiContext.Current.Resources;
            CheckResponseCache(context, true);
            DateTime since = DbUtils.ToDateTime(context.GetParam("since", DbUtils.ToString(DateTime.UtcNow.Date.AddDays(-14))));
            PermissionsBL.CheckUserAllowed(DekiContext.Current.User, Permissions.SUBSCRIBE);
            var res = MakeNewsFeedCached(() => QuerySiteRecentChanges(since, 0, MAX_RECENT_CHANGES, null, false, NS.UNKNOWN, false), context.Uri.WithoutCredentials(), resources.Localize(DekiResources.WHATS_NEW(DekiContext.Current.Instance.SiteName)), "site-activiites", null, FeedFormat.DAILY_ACTIVITY, since);
            response.Return(DreamMessage.Ok(res.Item1, res.Item2));
            yield break;
        }

        [DreamFeature("GET:pages/{pageid}/feed/new", "Retrieve feed of new page creations")] // TODO (steveb): hide this feature signature
        [DreamFeature("GET:pages/{pageid}/feed", "Retrieve feed of page changes")]
        [DreamFeatureParam("{pageid}", "string", "either an integer page ID, \"home\", or \"=\" followed by a double uri-encoded page title")]
        [DreamFeatureParam("since", "string?", "Start date for changes.  Date is provided in 'yyyyMMddHHmmss' format (default: ignored).")]
        [DreamFeatureParam("limit", "int?", "Number of changes to retrieve (default: 100)")]
        [DreamFeatureParam("offset", "int?", "Skipped changes (default: 0)")]
        [DreamFeatureParam("format", "{all, daily, raw, rawdaily}?", "Format for feed (default: daily)")]
        [DreamFeatureParam("depth", "string?", "How deep into the sub-tree changes should be included. 0 for the current page only, 'infinity' for entire sub-tree (default: 0)")]
        [DreamFeatureParam("filter", "string?", "use \"new\" to include only newly created pages in the feed (default: all)")]
        [DreamFeatureParam("redirects", "int?", "If zero, do not follow page redirects.")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        [DreamFeatureStatus(DreamStatus.Ok, "The request completed successfully")]
        [DreamFeatureStatus(DreamStatus.BadRequest, "Invalid input parameter or request body")]
        [DreamFeatureStatus(DreamStatus.Forbidden, "Read/suscribe access to the page is required")]
        [DreamFeatureStatus(DreamStatus.NotFound, "Requested page could not be found")]
        public Yield GetPageChanges(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            var resources = DekiContext.Current.Resources;
            CheckResponseCache(context, true);

            // Note (arnec): the /new signature was introduced as resolution for BUG 5346
            bool createOnly = context.Uri.Path.EndsWith("/new") || context.GetParam("filter", string.Empty).EqualsInvariant("new");
            bool recurse = createOnly || context.GetParam("depth", "0").EqualsInvariant("infinity");
            DateTime since;
            int limit;
            int offset;
            FeedFormat format;
            string language;
            NS ns;
            List<string> feedNameSuffixes = new List<string>();
            PageBE page = PageBL_AuthorizePage(context, null, Permissions.READ | Permissions.SUBSCRIBE, false);
            feedNameSuffixes.Add("page=" + page.ID);
            if(recurse) {
                feedNameSuffixes.Add("depth=infinity");
            }
            ExtractRecentChangesParameters(context, out since, out limit, out offset, out format, out language, out ns, ref feedNameSuffixes);
            var res = MakeNewsFeedCached(() => QueryPageRecentChanges(page, since, offset, limit, recurse, createOnly), context.Uri.WithoutCredentials(), resources.Localize(DekiResources.PAGE_NEWS(page.Title.AsUserFriendlyName())), createOnly ? "page-feed-new" : "page-feed", feedNameSuffixes, format, since);
            response.Return(DreamMessage.Ok(res.Item1, res.Item2));
            yield break;
        }

        [DreamFeature("GET:users/{userid}/feed", "Retrieve feed of user contributions")]
        [DreamFeatureParam("{userid}", "string", "either an integer user ID, \"current\", or \"=\" followed by a double uri-encoded user name")]
        [DreamFeatureParam("since", "string?", "Start date for changes.  Date is provided in 'yyyyMMddHHmmss' format (default: ignored).")]
        [DreamFeatureParam("limit", "int?", "Number of changes to retrieve (default: 100)")]
        [DreamFeatureParam("offset", "int?", "Skipped changes (default: 0)")]
        [DreamFeatureParam("format", "{all, daily, raw, rawdaily}?", "Format for feed (default: daily)")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        [DreamFeatureStatus(DreamStatus.Ok, "The request completed successfully")]
        [DreamFeatureStatus(DreamStatus.BadRequest, "Invalid input parameter or request body")]
        [DreamFeatureStatus(DreamStatus.Forbidden, "Suscribe access is required")]
        [DreamFeatureStatus(DreamStatus.NotFound, "Requested user could not be found")]
        public Yield GetUserContributions(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            var resources = DekiContext.Current.Resources;
            CheckResponseCache(context, true);
            PermissionsBL.CheckUserAllowed(DekiContext.Current.User, Permissions.SUBSCRIBE);
            DateTime since;
            int limit;
            int offset;
            FeedFormat format;
            string language;
            NS ns;
            List<string> feedNameSuffixes = new List<string>();
            UserBE contributor = GetUserFromUrlMustExist();
            feedNameSuffixes.Add("user=" + contributor.ID);

            // BUGBUGBUG (steveb): doing daily digests for users might not be appropriate, because it combines edit operations, but skips other contributors

            ExtractRecentChangesParameters(context, out since, out limit, out offset, out format, out language, out ns, ref feedNameSuffixes);
            var res = MakeNewsFeedCached(() => QueryUserContributionsRecentChanges(contributor.ID, since, offset, limit), context.Uri.WithoutCredentials(), resources.Localize(DekiResources.USER_NEWS(string.IsNullOrEmpty(contributor.RealName) ? contributor.Name : contributor.RealName)), "user-feed", feedNameSuffixes, format, since);
            response.Return(DreamMessage.Ok(res.Item1, res.Item2));
            yield break;
        }

        [DreamFeature("GET:users/{userid}/favorites/feed", "Retrieve feed of user favorites changes")]
        [DreamFeatureParam("{userid}", "string", "either an integer user ID, \"current\", or \"=\" followed by a double uri-encoded user name")]
        [DreamFeatureParam("since", "string?", "Start date for changes.  Date is provided in 'yyyyMMddHHmmss' format (default: ignored).")]
        [DreamFeatureParam("limit", "int?", "Number of changes to retrieve (default: 100)")]
        [DreamFeatureParam("offset", "int?", "Skipped changes (default: 0)")]
        [DreamFeatureParam("format", "{all, daily, raw, rawdaily}?", "Format for feed (default: daily)")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        [DreamFeatureStatus(DreamStatus.Ok, "The request completed successfully")]
        [DreamFeatureStatus(DreamStatus.BadRequest, "Invalid input parameter or request body")]
        [DreamFeatureStatus(DreamStatus.Forbidden, "Suscribe access to the page is required")]
        [DreamFeatureStatus(DreamStatus.NotFound, "Requested user could not be found")]
        public Yield GetUserFavoritesChanges(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            var resources = DekiContext.Current.Resources;
            CheckResponseCache(context, true);
            PermissionsBL.CheckUserAllowed(DekiContext.Current.User, Permissions.SUBSCRIBE);
            DateTime since;
            int limit;
            int offset;
            FeedFormat format;
            string language;
            NS ns;
            List<string> feedNameSuffixes = new List<string>();
            UserBE contributor = GetUserFromUrlMustExist();
            feedNameSuffixes.Add("user=" + contributor.ID);
            ExtractRecentChangesParameters(context, out since, out limit, out offset, out format, out language, out ns, ref feedNameSuffixes);
            var res = MakeNewsFeedCached(() => QueryUserFavoritesRecentChanges(contributor.ID, since, offset, limit), context.Uri.WithoutCredentials(), resources.Localize(DekiResources.USER_FAVORITES(string.IsNullOrEmpty(contributor.RealName) ? contributor.Name : contributor.RealName)), "favorites-feed", feedNameSuffixes, format, since);
            response.Return(DreamMessage.Ok(res.Item1, res.Item2));
            yield break;
        }

        //--- Methods ---
        private string GetRecursiveRecentChangesTimestamp(PageBE page, bool includePages, bool includeFiles, bool includeComments, bool includeTags) {
            RecentChangeEntry entry = null;
            if(includePages && includeFiles && includeComments && includeTags) {
                entry = QueryPageRecentChanges(page, DateTime.MinValue, 0, 1, true, false).First();
            } else {

                // TODO (steveb): we need to change this so that we _only_ query for the changes 
                //                we actually need rather than fetch a whole bunch and discard them
                var changes = QueryPageRecentChanges(page, DateTime.MinValue, 0, 100, true, false);
                foreach(var change in changes) {
                    bool changed = false;
                    if(includePages) {
                        changed = changed
                            || (change.Type == RC.EDIT)
                            || (change.Type == RC.NEW)
                            || (change.Type == RC.MOVE)
                            || (change.Type == RC.MOVE_OVER_REDIRECT)
                            || (change.Type == RC.PAGEDELETED)
                            || (change.Type == RC.PAGERESTORED)
                            || (change.Type == RC.PAGEMETA)
                            || (change.Type == RC.GRANTS_ADDED)
                            || (change.Type == RC.GRANTS_REMOVED)
                            || (change.Type == RC.RESTRICTION_UPDATED);
                    }
                    if(includeFiles) {
                        changed = changed
                            || (change.Type == RC.FILE);
                    }
                    if(includeComments) {
                        changed = changed
                            || (change.Type == RC.COMMENT_CREATE)
                            || (change.Type == RC.COMMENT_UPDATE)
                            || (change.Type == RC.COMMENT_DELETE);
                    }
                    if(includeTags) {
                        changed = changed
                            || (change.Type == RC.MOVE)
                            || (change.Type == RC.MOVE_OVER_REDIRECT)
                            || (change.Type == RC.TAGS);
                    }
                    if(changed) {
                        entry = change;
                        break;
                    }
                }

                // if no match was found, keep the timestamp of the oldest one
                if(entry == null) {
                    entry = changes.Last();
                }
            }
            return entry.Timestamp.ToString("yyyyMMddHHmmss");
        }

        private IEnumerable<RecentChangeEntry> QuerySiteRecentChanges(DateTime since, int offset, int count, string language, bool createOnly, NS nsFilter, bool filterByPermissions) {
            return ConvertAndFilterXmlToRecentChanges(DbUtils.CurrentSession.RecentChanges_GetSiteRecentChanges(since, language, createOnly, nsFilter, MAX_RECENT_CHANGES, DekiContext.Current.Instance.RecentChangesScanSize), offset, count, filterByPermissions);
        }

        private IEnumerable<RecentChangeEntry> QueryPageRecentChanges(PageBE page, DateTime since, int offset, int count, bool recurse, bool createOnly) {
            return ConvertAndFilterXmlToRecentChanges(DbUtils.CurrentSession.RecentChanges_GetPageRecentChanges(page, since, recurse, createOnly, MAX_RECENT_CHANGES), offset, count, true);
        }

        private IEnumerable<RecentChangeEntry> QueryUserContributionsRecentChanges(uint contributorId, DateTime since, int offset, int count) {
            return ConvertAndFilterXmlToRecentChanges(DbUtils.CurrentSession.RecentChanges_GetUserContributionsRecentChanges(contributorId, since, MAX_RECENT_CHANGES), offset, count, true);
        }

        private IEnumerable<RecentChangeEntry> QueryUserFavoritesRecentChanges(uint favoritesId, DateTime since, int offset, int count) {
            return ConvertAndFilterXmlToRecentChanges(DbUtils.CurrentSession.RecentChanges_GetUserFavoritesRecentChanges(favoritesId, since, MAX_RECENT_CHANGES), offset, count, true);
        }

        private Tuplet<MimeType, XDoc> MakeNewsFeedCached(Func<IEnumerable<RecentChangeEntry>> recentchanges, XUri feedUri, string feedTitle, string feedName, List<string> feedNameSuffixes, FeedFormat format, DateTime since) {
            DekiContext deki = DekiContext.Current;
            TimeSpan feedCacheTtl = deki.Instance.RecentChangesFeedCachingTtl;

            // cache the feed if caching is enabled, if an ATOM format is requested, and the user is not logged in
            if((feedCacheTtl > TimeSpan.Zero) && ((format == FeedFormat.ATOM_ALL) || (format == FeedFormat.ATOM_DAILY)) && UserBL.IsAnonymous(deki.User)) {

                // compute complete feed name
                if(feedNameSuffixes.Count > 0) {
                    feedName += "(" + string.Join(",", feedNameSuffixes.ToArray()) + ")";
                }
                feedName += ".xml";

                // check if there is a cached version of the feed
                Plug store = Storage.At("site_" + XUri.EncodeSegment(DekiContext.Current.Instance.Id), DreamContext.Current.Culture.Name, "users", string.Format("user_{0}", DekiContext.Current.User.ID), feedName);
                var v = store.Get(new Result<DreamMessage>(TimeSpan.MaxValue)).Wait();
                XDoc cachedFeed = (v.IsSuccessful && v.HasDocument) ? v.ToDocument() : null;
                if(cachedFeed != null) {

                    // let's validate the timestamp on the feed as well (just in case the cache storage didn't remove the item)
                    DateTime now = DateTime.UtcNow;
                    DateTime updated = cachedFeed["_:updated"].AsDate ?? now;
                    if(now.Subtract(updated) < feedCacheTtl) {
                        return new Tuplet<MimeType, XDoc>(MimeType.ATOM, cachedFeed);
                    }
                }
                var result = MakeNewsFeed(recentchanges(), feedUri, feedTitle, format, since);
                if(!result.Item2.IsEmpty) {
                    store.With("ttl", feedCacheTtl.TotalSeconds).Put(result.Item2, new Result<DreamMessage>(TimeSpan.MaxValue)).Block();
                }
                return result;
            }
            return MakeNewsFeed(recentchanges(), feedUri, feedTitle, format, since);
        }

        private Tuplet<MimeType, XDoc> MakeNewsFeed(IEnumerable<RecentChangeEntry> recentchanges, XUri feedUri, string feedTitle, FeedFormat format, DateTime since) {
            var resources = DekiContext.Current.Resources;
            var changes = new List<RecentChangeEntry>();
            DekiContext deki = DekiContext.Current;
            bool diffCacheEnabled = deki.Instance.RecentChangesDiffCaching;

            // check if we need to merge change entries
            MimeType mime = MimeType.XML;
            if((format == FeedFormat.ATOM_DAILY) || (format == FeedFormat.RAW_DAILY)) {

                // combine changes that occurred on the same day
                Dictionary<string, DigestLookupEntry> pageLookup = new Dictionary<string, DigestLookupEntry>();
                Dictionary<string, DigestLookupEntry> commentLookup = new Dictionary<string, DigestLookupEntry>();
                Dictionary<string, ulong> commentDescriptToCommentLookup = new Dictionary<string, ulong>();
                List<Dictionary<string, KeyValuePair<string, int>>> authors = new List<Dictionary<string, KeyValuePair<string, int>>>();
                int index = 0;
                foreach(var change in recentchanges) {
                    ulong pageId = change.CurId;
                    if(pageId == 0) {

                        // should never happen, but if it does, just ignore this entry
                        continue;
                    }
                    DateTime timestamp = change.Timestamp;
                    NS ns = change.Namespace;
                    RC type = change.Type;
                    string author = change.Username;
                    string fullname = change.Fullname ?? change.Username;

                    // check if we processing a comment or page change
                    if(Utils.IsPageComment(type)) {
                        ulong commentId = change.CmntId ?? 0;
                        string comment = change.Comment;
                        if(commentId == 0) {

                            // NOTE (steveb): because the recentchanges table is brain dead, we sometimes cannot associate a comment change with the comment that affected it;
                            //                luckily, when that happens, there is a good chance that the description for the change is the same as an earlier one;
                            //                so all we need to do is to lookup the previous change using the current change description.

                            if(!commentDescriptToCommentLookup.TryGetValue(comment ?? string.Empty, out commentId)) {
                                continue;
                            }
                        } else if(comment != null) {
                            commentDescriptToCommentLookup[comment] = commentId;
                        }

                        // remove revision number (not applicable)
                        change.Revision = 0;

                        // check if we need to merge this change with a previous one
                        DigestLookupEntry entry;
                        string key = string.Format("{0}-{1}", commentId, timestamp.DayOfYear);
                        if(commentLookup.TryGetValue(key, out entry)) {
                            var item = changes[entry.Index];
                            ++item.EditCount;

                            // append the change comments
                            if(item.ExtraComments == null) {
                                item.ExtraComments = new List<Tuplet<string, string, string>>();

                                // first add the existing comment to the list
                                item.ExtraComments.Add(new Tuplet<string, string, string>(item.Username, item.Fullname, item.Comment));
                            }
                            item.ExtraComments.Add(new Tuplet<string, string, string>(change.Username, change.Fullname, change.Comment));

                            // updated edit count for author
                            KeyValuePair<string, int> authorEdits;
                            authors[entry.Index].TryGetValue(author, out authorEdits);
                            authors[entry.Index][author] = new KeyValuePair<string, int>(fullname, authorEdits.Value + 1);
                        } else {
                            change.EditCount = 1;

                            // NOTE (steveb): we always create the lookup to create a discontinuity with previous changes on the same page;
                            //                this causes ungroupable changes (e.g. MOVE) to split groupable changes; thus avoiding
                            //                that these groupable changes get inproperly grouped since they aren't continuous.

                            // create a new entry, either because this page has no existing entry yet, or the change cannot be grouped with other changes
                            commentLookup[key] = new DigestLookupEntry(timestamp, index, type);
                            authors.Add(new Dictionary<string, KeyValuePair<string, int>>());
                            authors[authors.Count - 1].Add(author, new KeyValuePair<string, int>(fullname, 1));

                            changes.Add(change);
                            ++index;
                        }
                    } else {

                        // add a default edit count
                        if(change.EditCount == 0) {
                            change.EditCount = Utils.IsPageEdit(type) ? 1 : 0;
                        }

                        // check if we need to merge this change with a previous one
                        DigestLookupEntry entry;
                        string key = string.Format("{0}-{1}-{2}", ns, pageId, timestamp.DayOfYear);
                        if(pageLookup.TryGetValue(key, out entry) && Utils.IsPageModification(type) && Utils.IsPageModification(entry.Type)) {
                            var item = changes[entry.Index];

                            // update 'rc_last_oldid' to reflect the older page id of the combined records
                            if(Utils.IsPageEdit(type)) {
                                item.LastOldId = change.LastOldId;
                                item.EditCount = item.EditCount + 1;
                                if(change.Revision != 0) {
                                    item.PreviousRevision = change.Revision - 1;
                                }
                            }

                            // append the change comments
                            if(item.ExtraComments == null) {
                                item.ExtraComments = new List<Tuplet<string, string, string>>();

                                // first add the existing comment to the list
                                item.ExtraComments.Add(new Tuplet<string, string, string>(item.Username, item.Fullname, item.Comment));
                            }
                            item.ExtraComments.Add(new Tuplet<string, string, string>(change.Username, change.Fullname, change.Comment));

                            // updated edit count for author
                            KeyValuePair<string, int> authorEdits;
                            authors[entry.Index].TryGetValue(author, out authorEdits);
                            authors[entry.Index][author] = new KeyValuePair<string, int>(fullname, authorEdits.Value + 1);
                        } else {

                            // NOTE (steveb): we always create the lookup to create a discontinuity with previous changes on the same page;
                            //                this causes ungroupable changes (e.g. MOVE) to split groupable changes; thus avoiding
                            //                that these groupable changes get inproperly grouped since they aren't continuous.

                            // create a new entry, either because this page has no existing entry yet, or the change cannot be grouped with other changes
                            pageLookup[key] = new DigestLookupEntry(timestamp, index, type);
                            authors.Add(new Dictionary<string, KeyValuePair<string, int>>());
                            authors[authors.Count - 1].Add(author, new KeyValuePair<string, int>(fullname, 1));

                            // check if page was changed
                            if(Utils.IsPageEdit(type)) {

                                // update previous revision number
                                change.PreviousRevision = change.Revision - 1;
                            } else if(Utils.IsPageModification(type)) {

                                // set previous revision number
                                change.PreviousRevision = change.Revision;
                            }
                            changes.Add(change);
                            ++index;
                        }
                    }
                }

                // create list of authors as comment line
                for(int i = 0; i < changes.Count; ++i) {
                    var change = changes[i];

                    // create an array of (fullname, username) author names
                    var sortedAuthors = (from author in authors[i] select new KeyValuePair<string, string>(author.Key, author.Value.Key)).ToList();
                    sortedAuthors.Sort((x, y) => StringComparer.OrdinalIgnoreCase.Compare(x.Value, y.Value));
                    string authorList = Utils.LinguisticJoin(from author in sortedAuthors select (string.IsNullOrEmpty(author.Value) ? author.Key : author.Value), resources.Localize(DekiResources.AND()));

                    // add-up all edit operations
                    int editTotal = 0;
                    foreach(KeyValuePair<string, int> edits in authors[i].Values) {
                        editTotal += edits.Value;
                    }

                    // reset comment for standard edits
                    RC type = change.Type;
                    if(Utils.IsPageModification(type) || Utils.IsPageComment(type)) {
                        string summary = null;
                        switch(editTotal) {
                        case 2:
                            summary = resources.Localize(DekiResources.EDIT_SUMMARY_TWO(authorList, editTotal));
                            break;
                        case 1:
                            summary = resources.Localize(DekiResources.EDIT_SUMMARY_ONE(authorList, editTotal));
                            break;
                        case 0:
                            break;
                        default:
                            summary = resources.Localize(DekiResources.EDIT_SUMMARY_MANY(authorList, editTotal));
                            break;
                        }
                        change.Summary = summary;
                    }

                    // reflect that multiple authors edited article, if appropriate
                    change.SortedAuthors = sortedAuthors;
                }

                // check if only the digest format was requested
                if(format == FeedFormat.RAW_DAILY) {
                    XDoc digest = new XDoc("digest");
                    foreach(var change in changes) {
                        change.AppendXml(digest);
                    }
                    return new Tuplet<MimeType, XDoc>(mime, digest);
                }
            } else if(format == FeedFormat.ATOM_ALL) {

                // keep all changes
                foreach(var change in recentchanges) {
                    if(Utils.IsPageEdit(change.Type)) {
                        change.PreviousRevision = change.Revision - 1;
                    } else {
                        change.Revision = 0;
                    }
                    changes.Add(change);
                }
            } else if(format == FeedFormat.DAILY_ACTIVITY) {

                // need to establish how many pages and users exist in total
                var pagesTotal = (int)DbUtils.CurrentSession.Pages_GetCount();
                var usersTotal = (int)DbUtils.CurrentSession.Users_GetCount();

                // daily activity format
                XDoc table = new XDoc("activity").Attr("type", "daily");
                DateTime missing = DateTime.UtcNow.Date;
                foreach(var change in from recentchange in recentchanges
                                      where (recentchange.Namespace == NS.MAIN) || (recentchange.Namespace == NS.USER)
                                      group recentchange by recentchange.Timestamp.Date into recentchangesByDate
                                      select new {
                                          Date = recentchangesByDate.Key,

                                          // count as edited pages, pages that were not created or deleted the same day
                                          PagesEdited = recentchangesByDate.Where(rc => (rc.Type == RC.EDIT) && !recentchangesByDate.Any(rc2 => (rc.CurId == rc2.CurId) && ((rc2.Type == RC.NEW) || (rc2.Type == RC.PAGERESTORED) || (rc.Type == RC.PAGEDELETED)))).Distinct(rc => rc.CurId).Count(),

                                          // count as created pages, pages that were not deleted later the same day
                                          PagesCreated = recentchangesByDate.Count(rc => ((rc.Type == RC.NEW) || (rc.Type == RC.PAGERESTORED)) && !recentchangesByDate.Any(rc2 => (rc2.CurId == rc.CurId) && (rc2.Id < rc.Id) && (rc.Type == RC.PAGEDELETED))),

                                          // count as deleted pages, pages that were not created or restored earlier the same day
                                          PagesDeleted = recentchangesByDate.Count(rc => (rc.Type == RC.PAGEDELETED) && !recentchangesByDate.Any(rc2 => (rc.CurId == rc2.CurId) && (rc2.Id > rc.Id) && ((rc2.Type == RC.NEW) || (rc2.Type == RC.PAGERESTORED)))),

                                          // simple counting of created users
                                          UsersCreated = recentchangesByDate.Count(rc => rc.Type == RC.USER_CREATED)
                                      }
                ) {

                    // check if we need to add empty entries for missing days
                    for(; missing > change.Date; missing = missing.AddDays(-1)) {
                        table.Start("entry").Attr("date", missing)
                            .Elem("pages.total", pagesTotal)
                            .Elem("pages.created", 0)
                            .Elem("pages.edited", 0)
                            .Elem("pages.deleted", 0)
                            .Elem("users.total", usersTotal)
                            .Elem("users.created", 0)
                        .End();
                    }

                    // add this day's entry
                    table.Start("entry").Attr("date", change.Date)
                        .Elem("pages.total", pagesTotal)
                        .Elem("pages.created", change.PagesCreated)
                        .Elem("pages.edited", change.PagesEdited)
                        .Elem("pages.deleted", change.PagesDeleted)
                        .Elem("users.total", usersTotal)
                        .Elem("users.created", change.UsersCreated)
                    .End();
                    
                    // NOTE (steveb): pages total might become negative if user created didn't actually create a user page
                    pagesTotal -= change.PagesCreated - change.PagesDeleted + change.UsersCreated;
                    usersTotal -= change.UsersCreated;

                    // indicate that current is *not* missing
                    missing = change.Date.AddDays(-1);
                }

                // pad with missing records
                for(; missing >= since; missing = missing.AddDays(-1)) {
                    table.Start("entry").Attr("date", missing)
                        .Elem("pages.total", pagesTotal)
                        .Elem("pages.created", 0)
                        .Elem("pages.edited", 0)
                        .Elem("pages.deleted", 0)
                        .Elem("users.total", usersTotal)
                        .Elem("users.created", 0)
                    .End();
                }
                return new Tuplet<MimeType, XDoc>(mime, table);
            } else {

                // unknown or RAW format
                XDoc table = new XDoc("table");
                foreach(var change in recentchanges) {
                    change.AppendXml(table);
                }
                return new Tuplet<MimeType, XDoc>(mime, table);
            }

            // compose feed document
            mime = MimeType.ATOM;
            XAtomFeed feed = new XAtomFeed(feedTitle, feedUri, DateTime.UtcNow) { Language = deki.Instance.SiteLanguage, Id = feedUri };
            Dictionary<string, XDoc> cache = new Dictionary<string, XDoc>();
            foreach(var change in changes) {
                RC type = change.Type;
                if(Utils.IsPageHiddenOperation(type)) {

                    // no real content to produce; let's skip it
                    continue;
                }

                // build feed content
                Title title = Title.FromDbPath(change.Namespace, change.Title, null);
                XDoc description = new XDoc("div");
                AppendDiff(diffCacheEnabled, description, change, type, title, cache);

                // add item to feed
                try {
                    DateTime timestamp = change.Timestamp;
                    XAtomEntry entry = feed.StartEntry(title.AsPrefixedUserFriendlyPath(), timestamp, timestamp);
                    XUri id = XUri.TryParse(Utils.AsPublicUiUri(title));
                    if(id != null) {
                        if(id.Segments.Length == 0) {
                            id = id.WithTrailingSlash();
                        }
                        entry.Id = id.WithFragment(DbUtils.ToString(change.Timestamp));
                    }
                    entry.AddAuthor(((change.SortedAuthors == null) || (change.SortedAuthors.Count == 1)) ? (string.IsNullOrEmpty(change.Fullname) ? change.Username : change.Fullname) : resources.Localize(DekiResources.EDIT_MULTIPLE()), null, null);
                    entry.AddLink(new XUri(Utils.AsPublicUiUri(title)), XAtomBase.LinkRelation.Alternate, null, null, null);
                    entry.AddSummary(MimeType.XHTML, description);
                    feed.End();
                } catch(Exception e) {
                    _log.ErrorExceptionMethodCall(e, "MakeNewsFeed", title.AsPrefixedDbPath());
                }
            }

            // insert <ins> styles
            foreach(XDoc ins in feed[".//ins"]) {
                ins.Attr("style", "color: #009900;background-color: #ccffcc;text-decoration: none;");
            }

            // insert <del> styles
            foreach(XDoc del in feed[".//del"]) {
                del.Attr("style", "color: #990000;background-color: #ffcccc;text-decoration: none;");
            }
            return new Tuplet<MimeType, XDoc>(mime, feed);
        }

        private void AppendDiff(bool diffCacheEnabled, XDoc body, RecentChangeEntry change, RC type, Title title, IDictionary<string, XDoc> cache) {
            var resources = DekiContext.Current.Resources;
            ulong pageid = change.CurId;
            int? after = (change.Revision > 0) ? (int?)change.Revision : null;
            int? before = change.PreviousRevision;

            // append edit summary, if any
            body.Elem("p", change.Summary);

            // append comment(s)
            int count = (change.ExtraComments == null) ? (string.IsNullOrEmpty(change.Comment) ? 0 : 1) : change.ExtraComments.Count;
            switch(count) {
            case 0:

                // nothing to do
                break;
            case 1:
                body.Elem("p", (change.ExtraComments != null) ? change.ExtraComments[0].Item3 : change.Comment);
                break;
            default:
                body.Start("ol");
                foreach(var comment in ((IEnumerable<Tuplet<string, string, string>>)change.ExtraComments).Reverse()) {
                    string author = string.IsNullOrEmpty(comment.Item2) ? comment.Item1 : comment.Item2;
                    body.Elem("li", string.IsNullOrEmpty(author) ? comment.Item3 : string.Format("{0} ({1})", comment.Item3, author));
                }
                body.End();
                break;
            }

            // check if page was modified
            if(after.HasValue && before.HasValue && (after != before)) {

                // check if we have a cached version of this page diff
                XDoc diffXml = null;
                Plug store = Storage.At("site_" + XUri.EncodeSegment(DekiContext.Current.Instance.Id), DreamContext.Current.Culture.Name, "feeds", string.Format("page_{0}", pageid), string.Format("diff_{0}-{1}.xml", before, after));
                if(diffCacheEnabled) {
                    var v = store.Get(new Result<DreamMessage>(TimeSpan.MaxValue)).Wait();
                    diffXml = (v.IsSuccessful && v.HasDocument) ? v.ToDocument() : null;

                    if(diffXml != null) {

                        // TODO (steveb): this problem only exists b/c we can't determine the actual revision number that we should use for diffing (see bug 7824)

                        // check if either revision has been hidden since we computed the diff
                        var session = DbUtils.CurrentSession;
                        if(after.Value != change.CurrentRevision) {
                            OldBE afterRevision = session.Old_GetOldByRevision(pageid, (ulong)after.Value);
                            if((afterRevision == null) || afterRevision.IsHidden) {
                                diffXml = null;
                            }
                        }
                        if((diffXml != null) && (before.Value != change.CurrentRevision) && (before.Value > 0)) {
                            OldBE beforeRevision = session.Old_GetOldByRevision(pageid, (ulong)before.Value);
                            if((beforeRevision == null) || beforeRevision.IsHidden) {
                                diffXml = null;
                            }
                        }
                    }
                }
                if(diffXml == null) {
                    diffXml = new XDoc("diff");

                    // retrieve page versions
                    XDoc res = QueryPageVersions(pageid, after, before, cache);
                    XDoc beforeDoc = res["before/body"];
                    XDoc afterDoc = res["after/body"];

                    // check if either both versions or only one version were retrieved
                    XDoc diff = XDoc.Empty;
                    XDoc invisibleDiff = XDoc.Empty;
                    string summary = null;
                    if(!beforeDoc.IsEmpty && !afterDoc.IsEmpty) {
                        XDoc beforeChanges;
                        XDoc afterChanges;
                        DekiResource summaryResource = null;

                        // compute differences between 'before' and 'after' versions
                        diff = Utils.GetPageDiff(beforeDoc, afterDoc, true, DekiContext.Current.Instance.MaxDiffSize, out invisibleDiff, out summaryResource, out beforeChanges, out afterChanges);

                        // TODO (arnec): why are we using ToLower here at all and without a culture?
                        summary = resources.Localize(summaryResource).ToLower();
                    } else if(!afterDoc.IsEmpty) {

                        // since we don't have a 'before' version, just show the entire 'after' version (can happen for new pages or hidden revisions)
                        diff = afterDoc;
                    } else if(!beforeDoc.IsEmpty) {

                        // since we don't have a 'after' version, just show the entire 'before' version (can happen for hidden revisions)
                        diff = beforeDoc;
                    }

                    // add change summary
                    diffXml.Start("blockquote");
                    diffXml.Start("p").Elem("strong", summary).End();

                    // check if a diff was computed
                    if(!diff.IsEmpty) {
                        diffXml.Start("hr").Attr("width", "100%").Attr("size", "2").End();
                        diffXml.AddNodes(diff);
                        diffXml.Start("hr").Attr("width", "100%").Attr("size", "2").End();

                        // check if there are invisible changes as well to show
                        if(!invisibleDiff.IsEmpty) {
                            diffXml.Start("p").Elem("strong", resources.Localize(DekiResources.PAGE_DIFF_OTHER_CHANGES())).End();
                            diffXml.Add(invisibleDiff);
                        }
                    } else if(!invisibleDiff.IsEmpty) {

                        // only show invisible changes
                        diffXml.Start("hr").Attr("width", "100%").Attr("size", "2").End();
                        diffXml.Start("p").Elem("strong", resources.Localize(DekiResources.PAGE_DIFF_OTHER_CHANGES())).End();
                        diffXml.Add(invisibleDiff);
                    } else if(beforeDoc.IsEmpty && afterDoc.IsEmpty) {

                        // show message that page contents were not available anymore
                        diffXml.Elem("p", resources.Localize(DekiResources.PAGE_NOT_AVAILABLE()));
                    }
                    diffXml.End();

                    // store diff in cache
                    if(diffCacheEnabled && !afterDoc.IsEmpty) {
                        store.With("ttl", TimeSpan.FromDays(30).TotalSeconds).Put(diffXml, new Result<DreamMessage>(TimeSpan.MaxValue)).Block();
                    }
                }
                body.AddNodes(diffXml);
            }

            // check if we have a comment text
            if(Utils.IsPageComment(type)) {
                string text = change.CmntContent;
                if(!string.IsNullOrEmpty(text) && !change.CmntDeleted) {
                    MimeType mime = new MimeType(change.CmntMimetype ?? MimeType.TEXT_UTF8.ToString());
                    if(mime.Match(MimeType.HTML)) {
                        XDoc html = XDocFactory.From(string.Format("<html><body>{0}</body></html>", text), MimeType.HTML);
                        body.Start("blockquote").AddNodes(html["body"]).End();
                    } else {

                        // anything else should be consider to be text
                        body.Start("blockquote").Elem("p", text).End();
                    }
                } else {

                    // anything else should be consider to be text
                    body.Start("blockquote").Elem("p", resources.Localize(DekiResources.COMMENT_NOT_AVAILABLE())).End();
                }
            }

            // adds links
            body.Start("table").Attr("border", 0).Attr("padding", "5").Attr("width", "80%").Start("tr");

            // add link for viewing the page
            if(change.PageExists) {
                Title view = new Title(title);
                body.Start("td").Start("a").Attr("href", Utils.AsPublicUiUri(view, true)).Value(resources.Localize(DekiResources.VIEW_PAGE())).End().End();
            }

            // check if we need to add link for editing the page
            if(after.HasValue && before.HasValue && (after != before)) {
                Title edit = new Title(title) { Query = "action=edit" };
                body.Start("td").Start("a").Attr("href", Utils.AsPublicUiUri(edit)).Value(resources.Localize(DekiResources.EDIT_PAGE())).End().End();
            }

            // check if we need to add link for viewing the complete diff
            if(after.HasValue && before.HasValue && (after != before)) {
                Title show = new Title(title) { Query = string.Format("diff={0}&revision={1}", after.Value, before.Value) };
                body.Start("td").Start("a").Attr("href", Utils.AsPublicUiUri(show, true)).Value(resources.Localize(DekiResources.VIEW_PAGE_DIFF())).End().End();
            }

            // check if we need to add link for seeing full page history
            if(after.HasValue && before.HasValue && (after != before)) {
                Title history = new Title(title) { Query = "action=history" };
                body.Start("td").Start("a").Attr("href", Utils.AsPublicUiUri(history)).Value(resources.Localize(DekiResources.VIEW_PAGE_HISTORY())).End().End();
            }

            // check if we need to add link for banning the user
            List<KeyValuePair<string, string>> authors = change.SortedAuthors;
            if((authors == null) || (authors.Count == 0)) {
                authors = new List<KeyValuePair<string, string>> { new KeyValuePair<string, string>(change.Username, change.Fullname) };
            }
            for(int i = 0; i < authors.Count; ++i) {
                string username = authors[i].Key;
                string fullname = authors[i].Value;
                if(!string.IsNullOrEmpty(username)) {

                    // don't put up ban link for admins.
                    UserBE user = DbUtils.CurrentSession.Users_GetByName(username);
                    if(!UserBL.IsAnonymous(user) && !PermissionsBL.IsUserAllowed(user, Permissions.ADMIN)) {
                        Title ban = Title.FromUIUri(null, "Special:Userban");
                        ban.Query += string.Format("username={0}", username);
                        body.Start("td").Start("a").Attr("href", Utils.AsPublicUiUri(ban)).Value(resources.Localize(DekiResources.BAN_USER(string.IsNullOrEmpty(fullname) ? username : fullname))).End().End();
                    }
                }
            }

            // close HTML
            body.End().End();
        }
    }
}

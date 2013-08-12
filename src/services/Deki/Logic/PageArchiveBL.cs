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
using System.Text;

using MindTouch.Deki.Data;
using MindTouch.Deki.Exceptions;
using MindTouch.Dream;
using MindTouch.Xml;

namespace MindTouch.Deki.Logic {

    public static class PageArchiveBL {

        public static DreamMessage BuildDeletedPageContents(uint pageid) {
            ArchiveBE page = DbUtils.CurrentSession.Archive_GetPageHeadById(pageid);
            if (page == null) {
                throw new PageArchiveLogicNotFoundException(pageid);
            }

            //HACKHACKHACK MaxM: Copy data to a PageBE object since parser will not work on an ArchiveBE. ArchiveBE needs to go away.
            PageBE tempP = new PageBE();
            tempP.Title = page.Title;
            tempP.SetText(page.Text);
            tempP.ContentType = page.ContentType;

            ParserResult parserResult = DekiXmlParser.Parse(tempP, ParserMode.VIEW_NO_EXECUTE);

            // TODO (steveb): this code is almost identical to the one in "GET:pages/{pageid}/contents"; consider merging

            // post process tail
            DekiXmlParser.PostProcessParserResults(parserResult);

            // wrap the result in a content tag and return it to the user
            XDoc result = new XDoc("content").Attr("type", parserResult.ContentType);
            foreach (XDoc entry in parserResult.Content.Elements) {
                if (entry.HasName("body")) {
                    result.Start("body").Attr("target", entry["@target"].AsText).Value(entry.ToInnerXHtml()).End();
                } else {
                    result.Elem(entry.Name, entry.ToInnerXHtml());
                }
            }
            // check if we hit a snag, which is indicated by a plain-text response
            if ((parserResult.ContentType == MimeType.TEXT.FullType) && (page.ContentType != MimeType.TEXT.FullType)) {

                // something happened during parsing
                return new DreamMessage(DreamStatus.NonAuthoritativeInformation, null, result);
            } else {
                return DreamMessage.Ok(result);
            }
        }
 
        private static Title BuildNewTitlesForMovedPage(Title rootTitle, Title currentTitle, Title newTitleForRootPage) {
            string titleRelativeToRootNode = currentTitle.AsPrefixedDbPath().Substring((currentTitle.IsTalk ? rootTitle.AsTalk() : rootTitle).AsPrefixedDbPath().Length);
            return Title.FromPrefixedDbPath((currentTitle.IsTalk ? newTitleForRootPage.AsTalk() : newTitleForRootPage).AsPrefixedDbPath() + titleRelativeToRootNode, currentTitle.DisplayName);
        }

        public static XDoc RestoreDeletedPage(uint pageid, Title newRootPath, string revertReason) {

            //Retrieve initial revisions of pages to restore
            //First item in the list is the page that initiated the transaction.
            //Talk pages are included.
            uint initialDeleteTranId = 0;
            IList<ArchiveBE> pagesToRestore = DbUtils.CurrentSession.Archive_GetPagesInTransaction(pageid, out initialDeleteTranId);
            TransactionBE initialDeleteTrans = DbUtils.CurrentSession.Transactions_GetById(initialDeleteTranId);

            //Validate deleted page + transaction
            if (pagesToRestore.Count == 0) {
                throw new PageArchiveLogicNotFoundException(pageid);
            }

            if (initialDeleteTrans == null) {
                throw new PageArchiveBadTransactionFatalException(initialDeleteTranId, pageid);
            }

            //TODO MaxM: move the above gathering of what pages to restore to another method. Make this private.

            //Look for title conflicts
            List<Title> titles = new List<Title>();
            List<ulong> pageidsToRestore = new List<ulong>();
            Dictionary<ulong, PageBE> restoredPagesById = null;
            DateTime utcTimestamp = DateTime.UtcNow;

            foreach (ArchiveBE p in pagesToRestore) {

                Title t = p.Title;
                if (newRootPath != null) {
                    t = BuildNewTitlesForMovedPage(pagesToRestore[0].Title, p.Title, newRootPath);
                }
                titles.Add(t);

                pageidsToRestore.Add(p.LastPageId);
            }

            IList<PageBE> currentPages = DbUtils.CurrentSession.Pages_GetByTitles(titles.ToArray());
            if (currentPages.Count > 0) {

                //Remove all conflicting redirect pages from target of restore if all conflicting pages are redirects
                List<PageBE> conflictingRedirects = new List<PageBE>();
                foreach (PageBE p in currentPages) {
                    if (p.IsRedirect) {
                        conflictingRedirects.Add(p);
                    }
                }

                if (currentPages.Count == conflictingRedirects.Count && conflictingRedirects.Count > 0) {
                
                    //Remove existing redirects and refresh the conflicting pages list
                    PageBL.DeletePages(conflictingRedirects.ToArray(), utcTimestamp, 0, false);
                    currentPages = DbUtils.CurrentSession.Pages_GetByTitles(titles.ToArray());
                }
            }

            if (currentPages.Count > 0) {

                //return the name(s) of the conflicting page title(s)
                StringBuilder conflictTitles = new StringBuilder();
                foreach (PageBE p in currentPages) {
                    if (conflictTitles.Length > 0)
                        conflictTitles.Append(", ");

                    conflictTitles.Append(p.Title.AsPrefixedUserFriendlyPath());
                }

                throw new PageArchiveRestoreNamedPageConflictException(conflictTitles.ToString());
            }

            //Gather revisions for all pages to be restored.
            //Revisions are sorted by timestamp: oldest first.
            Dictionary<ulong, IList<ArchiveBE>> revisionsByPageId = DbUtils.CurrentSession.Archive_GetRevisionsByPageIds(pageidsToRestore);

            uint restoredPageTranId = 0;
            try {
                TransactionBE newTrans = new TransactionBE();
                newTrans.UserId = DekiContext.Current.User.ID;
                newTrans.PageId = pageid;
                newTrans.Title = pagesToRestore[0].Title;
                newTrans.Type = RC.PAGERESTORED;
                newTrans.TimeStamp = DateTime.UtcNow;
                restoredPageTranId = DbUtils.CurrentSession.Transactions_Insert(newTrans);

                //Pages must be restored in correct order (alphabetical ensures parent pages are restored before children). 
                //pagesToRestore must be in alphabetical title order

                bool minorChange = false;
                foreach (ArchiveBE pageToRestore in pagesToRestore) {

                    IList<ArchiveBE> revisions = null;
                    if (revisionsByPageId.TryGetValue(pageToRestore.LastPageId, out revisions)) {
                        
                        //Optionally restore page to different title
                        Title restoreToTitle = pageToRestore.Title;
                        if (newRootPath != null) {
                            restoreToTitle = BuildNewTitlesForMovedPage(pagesToRestore[0].Title, pageToRestore.Title, newRootPath);
                        }

                        RestorePageRevisionsForPage(revisions.ToArray(), restoreToTitle, restoredPageTranId, minorChange, utcTimestamp);
                        DbUtils.CurrentSession.Archive_Delete(revisions.Select(e => e.Id).ToList());
                    }
                    minorChange = true;
                }

                //Retrieve the restored pages
                restoredPagesById = PageBL.GetPagesByIdsPreserveOrder(pageidsToRestore).AsHash(e => e.ID);

                // Restore attachments
                IList<ResourceBE> attachmentsToRestore = ResourceBL.Instance.GetResourcesByChangeSet(initialDeleteTrans.Id, ResourceBE.Type.FILE);
                foreach (ResourceBE at in attachmentsToRestore) {
                    PageBE restoredPage;
                    if(restoredPagesById.TryGetValue(at.ParentPageId.Value, out restoredPage)) {
                        AttachmentBL.Instance.RestoreAttachment(at, restoredPage, utcTimestamp, restoredPageTranId);
                    }
                }

                //Update the old transaction as reverted
                initialDeleteTrans.Reverted = true;
                initialDeleteTrans.RevertTimeStamp = utcTimestamp;
                initialDeleteTrans.RevertUserId = DekiContext.Current.User.ID;
                initialDeleteTrans.RevertReason = revertReason;
                DbUtils.CurrentSession.Transactions_Update(initialDeleteTrans);

            } catch (Exception) {
                DbUtils.CurrentSession.Transactions_Delete(restoredPageTranId);
                throw;
            }

            //Build restore summary
            XDoc ret = new XDoc("pages.restored");
            foreach (ulong restoredPageId in pageidsToRestore) {
                PageBE restoredPage = null;
                if (restoredPagesById.TryGetValue((uint) restoredPageId, out restoredPage)) {
                    ret.Add(PageBL.GetPageXml(restoredPage, string.Empty));
                }
            }

            return ret;
        }

        private static void RestorePageRevisionsForPage(ArchiveBE[] archivedRevs, Title newTitle, uint transactionId, bool minorChange, DateTime utcTimestamp) {
            // add the most recent archive entry to the pages table
            // NOTE:  this will preserve the page id if it was saved with the archive or create a new page id if it is not available
            ArchiveBE mostRecentArchiveRev = archivedRevs[archivedRevs.Length - 1];
            PageBE restoredPage = null;
            if (0 < archivedRevs.Length) {
                restoredPage = new PageBE();
                restoredPage.Title = newTitle;
                restoredPage.Revision = mostRecentArchiveRev.Revision;
                restoredPage.MinorEdit = mostRecentArchiveRev.MinorEdit;
                bool conflict;
                PageBL.Save(restoredPage, null, mostRecentArchiveRev.Comment, mostRecentArchiveRev.Text, mostRecentArchiveRev.ContentType, mostRecentArchiveRev.Title.DisplayName, mostRecentArchiveRev.Language, -1, null, mostRecentArchiveRev.TimeStamp, mostRecentArchiveRev.LastPageId, false, false, null, false, out conflict);
                RecentChangeBL.AddRestorePageRecentChange(utcTimestamp, restoredPage, DekiContext.Current.User, DekiResources.UNDELETED_ARTICLE(restoredPage.Title.AsPrefixedUserFriendlyPath()), minorChange, transactionId);
            }

            // add all other archive entries to the old table
            // NOTE:  this will preserve the old ids if they were saved with the archive or create new old ids if not available
            for (int i = 0; i < archivedRevs.Length - 1; i++) {
                ArchiveBE archivedRev = archivedRevs[i];
                PageBE currentPage = new PageBE();
                currentPage.Title = newTitle;
                if (i < archivedRevs.Length - 1) {
                    ParserResult parserResult = DekiXmlParser.ParseSave(currentPage, archivedRev.ContentType, currentPage.Language, archivedRev.Text, -1, null, false, null);
                    currentPage.SetText(parserResult.BodyText);
                    currentPage.ContentType = parserResult.ContentType;
                    currentPage.UserID = archivedRev.UserID;
                    currentPage.TimeStamp = archivedRev.TimeStamp;
                    currentPage.MinorEdit = archivedRev.MinorEdit;
                    currentPage.Comment = archivedRev.Comment;
                    currentPage.Language = archivedRev.Language;
                    currentPage.IsHidden = archivedRev.IsHidden;
                    currentPage.Revision = archivedRev.Revision;
                    currentPage.ID = restoredPage.ID;
                    PageBL.InsertOld(currentPage, archivedRev.OldId);
                }
            }
        }

        #region XML Helpers
        public static XDoc GetArchivedPagesXml(uint trans_limit, uint trans_offset, Title filterTitle) {
            XDoc ret = new XDoc("pages.archive");
            Dictionary<uint, TransactionBE> transactionsById = null;
            Dictionary<uint, UserBE> usersById = new Dictionary<uint, UserBE>();
            uint? queryTotalTransactionCount;

            //Lookup archived pages grouped by transactions
            IList<KeyValuePair<uint, IList<ArchiveBE>>> pagesByTransactions = DbUtils.CurrentSession.Archive_GetPagesByTitleTransactions(filterTitle, trans_offset, trans_limit, out transactionsById, out queryTotalTransactionCount);
            if (queryTotalTransactionCount != null)
                ret.Attr("querycount", queryTotalTransactionCount.Value);

            //Retrieve users that performed the delete 
            foreach (TransactionBE t in transactionsById.Values) {
                usersById[t.UserId] = null;
            }
            usersById = DbUtils.CurrentSession.Users_GetByIds(usersById.Keys.ToArray()).AsHash(e => e.ID);

            //Build xml with the info
            foreach (KeyValuePair<uint, IList<ArchiveBE>> trans in pagesByTransactions) {
                IList<ArchiveBE> pages = trans.Value;
                TransactionBE tran = null;
                UserBE user = null;
                DateTime ts = DateTime.MinValue;

                //Retrieve user and timestamp based on transaction
                if (transactionsById.TryGetValue(trans.Key, out tran)) {
                    ts = tran.TimeStamp;
                    usersById.TryGetValue(tran.UserId, out user);
                }

                //Display the page where the delete started
                GetArchivePageXml(ret, pages[0], user, ts);

                //Display href to see the subpages
                ret[ret.AsXmlNode.LastChild].Start("subpages").Attr("count", pages.Count - 1).Attr("href", DekiContext.Current.ApiUri.At("archive", "pages", pages[0].LastPageId.ToString(), "subpages")).End();
            }

            return ret;
        }

        public static XDoc GetArchivedSubPagesXml(uint pageid) {
            XDoc ret = new XDoc("pages.archive");
            uint tranId = 0;

            //Lookup archived pages grouped by transactions
            IList<ArchiveBE> removedPages = DbUtils.CurrentSession.Archive_GetPagesInTransaction(pageid, out tranId);
            if (ArrayUtil.IsNullOrEmpty(removedPages)) {
                throw new PageArchiveLogicNotFoundException(pageid);
            }

            for (int i = 1; i < removedPages.Count; i++) {
                GetArchivePageXml(ret, removedPages[i], null, DateTime.MinValue);
            }

            return ret;
        }

        public static XDoc GetArchivePageXml(uint pageid) {
            XDoc ret = XDoc.Empty;
            ArchiveBE page = DbUtils.CurrentSession.Archive_GetPageHeadById(pageid);
            if (page == null) {
                throw new PageArchiveLogicNotFoundException(pageid);
            }

            //Retrieve metadata about the deletion to populate page info
            TransactionBE t = DbUtils.CurrentSession.Transactions_GetById(page.TransactionId);
            DateTime deleteTs = DateTime.MinValue;
            UserBE deletedBy = null;
            if (t != null) {
                deletedBy = UserBL.GetUserById(t.UserId);
                deleteTs = t.TimeStamp;
            }

            return GetArchivePageXml(ret, page, deletedBy, deleteTs);
        }

        private static XDoc GetArchivePageXml(XDoc doc, ArchiveBE archivedPage, UserBE user, DateTime ts) {
            XDoc ret = doc;
            bool singlePage = false;
            if (ret == null || ret.IsEmpty) {
                ret = new XDoc("page.archive");
                singlePage = true;
            } else {
                ret = ret.Start("page.archive");
            }

            ret = ret.Attr("id", archivedPage.LastPageId).Attr("href", DekiContext.Current.ApiUri.At("archive", "pages", archivedPage.LastPageId.ToString(), "info"))
            .Start("title").Value(archivedPage.Title.AsUserFriendlyName()).End()
            .Start("path").Value(archivedPage.Title.AsPrefixedDbPath()).End()
            .Start("contents").Attr("type", archivedPage.ContentType).Attr("href", DekiContext.Current.ApiUri.At("archive", "pages", archivedPage.LastPageId.ToString(), "contents")).End();

            if (user != null) {
                ret.Add(UserBL.GetUserXml(user, "deleted", false));
            }

            if (ts != DateTime.MinValue) {
                ret.Elem("date.deleted", ts);
            }
            if (!singlePage)
                ret.End();
            return ret;
        }

        #endregion
    }
}

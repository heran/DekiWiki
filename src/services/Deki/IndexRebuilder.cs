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
using System.Threading;
using log4net;
using MindTouch.Deki.Data;
using MindTouch.Deki.Logic;
using MindTouch.Deki.Search;
using MindTouch.Tasking;
using MindTouch.Xml;

namespace MindTouch.Deki {
    using Yield = IEnumerator<IYield>;

    public interface IIndexRebuilder {

        //--- Methods ---
        void Rebuild();
        Result<XDoc> Repair(bool checkOnly, bool verbose, Result<XDoc> result);
    }

    public class IndexRebuilder : IIndexRebuilder {

        //--- Types ---
        private class Report {
            public readonly bool CheckOnly;
            public readonly bool Verbose;
            public readonly XDoc Document;
            public int Total;
            public int Missing;
            public int Stale;

            public Report(string reportName, bool checkOnly, bool verbose) {
                CheckOnly = checkOnly;
                Verbose = verbose;
                Document = new XDoc(reportName);
            }

            public XDoc BuildReport(params Report[] reports) {
                foreach(var r in reports) {
                    r.AddStatsToDocument();
                    Total += r.Total;
                    Missing += r.Missing;
                    Stale += r.Stale;
                    Document.Add(r.Document);
                }
                AddStatsToDocument();
                return Document;
            }

            protected void AddStatsToDocument() {
                Document
                    .Attr("total", Total)
                    .Attr("repairing", CheckOnly ? 0 : Missing + Stale)
                    .Attr("missing", Missing)
                    .Attr("stale", Stale)
                    .Attr("checkonly", CheckOnly);
            }
        }

        //-- Class Fields ---
        private static readonly ILog _log = LogUtils.CreateLog();

        //--- Fields ---
        private readonly IDekiChangeSink _eventSink;
        private readonly UserBE _currentUser;
        private readonly ISearchBL _searchBL;
        private readonly IPageBL _pageBL;
        private readonly ICommentBL _commentBL;
        private readonly IAttachmentBL _attachmentBL;
        private readonly IUserBL _userBL;
        private readonly NS[] _indexNameSpaceWhitelist;
        private readonly DateTime _now;

        //--- Cosntructors ---
        public IndexRebuilder(
            IDekiChangeSink eventSink,
            UserBE currentUser,
            ISearchBL searchBL,
            IPageBL pageBL,
            ICommentBL commentBL,
            IAttachmentBL attachmentBL,
            IUserBL userBL,
            NS[] indexNameSpaceWhitelist,
            DateTime now
        ) {
            _eventSink = eventSink;
            _currentUser = currentUser;
            _searchBL = searchBL;
            _pageBL = pageBL;
            _commentBL = commentBL;
            _attachmentBL = attachmentBL;
            _userBL = userBL;
            _indexNameSpaceWhitelist = indexNameSpaceWhitelist;
            _now = now;
        }

        //--- Methods ---
        public void Rebuild() {
            _eventSink.IndexRebuildStart(_now);
            var count = 0;
            Action throttle = () => {
                if(++count % 100 == 0) {
                    Thread.Sleep(200);
                }
            };
            var pages = new HashSet<ulong>();
            foreach(var page in _pageBL.GetAllPagesChunked(_indexNameSpaceWhitelist)) {
                throttle();
                _eventSink.PagePoke(_now, page, _currentUser);
                pages.Add(page.ID);
                uint commentCount;
                foreach(var comment in _commentBL.RetrieveCommentsForPage(page, CommentFilter.NONDELETED, false, null, SortDirection.UNDEFINED, 0, uint.MaxValue, out commentCount)) {
                    throttle();
                    _eventSink.CommentPoke(_now, comment, page, _currentUser);
                }
            }
            _log.DebugFormat("page queueing completed");
            foreach(var att in _attachmentBL.GetAllAttachementsChunked()) {
                if(!pages.Contains(att.ParentPageId.GetValueOrDefault())) {
                    continue;
                }
                throttle();
                _eventSink.AttachmentPoke(_now, att);
            }
            _log.DebugFormat("file queueing completed");
            foreach(var user in _userBL.GetAllUsers()) {
                throttle();
                _eventSink.UserUpdate(_now, user);
            }
            _log.DebugFormat("completed re-index event queueing");
        }

        public Result<XDoc> Repair(bool checkOnly, bool verbose, Result<XDoc> result) {
            return Coroutine.Invoke(Repair_Helper, checkOnly, verbose, result);
        }

        private Yield Repair_Helper(bool checkOnly, bool verbose, Result<XDoc> result) {
            var count = 0;
            Action throttle = () => {
                if(++count % 100 == 0) {
                    Thread.Sleep(100);
                }
            };

            // check pages and their comments
            var pages = new HashSet<ulong>();
            var pageUnitOfWork = new List<PageBE>();
            var pageReport = new Report("pages", checkOnly, verbose);
            var commentUnitOfWork = new List<Tuplet<CommentBE, PageBE>>();
            var commentReport = new Report("comments", checkOnly, verbose);
            foreach(var page in _pageBL.GetAllPagesChunked(_indexNameSpaceWhitelist)) {
                throttle();
                pages.Add(page.ID);
                pageUnitOfWork.Add(page);
                uint commentCount;
                foreach(var comment in _commentBL.RetrieveCommentsForPage(page, CommentFilter.NONDELETED, false, null, SortDirection.UNDEFINED, 0, uint.MaxValue, out commentCount)) {
                    throttle();
                    commentUnitOfWork.Add(new Tuplet<CommentBE, PageBE>(comment, page));
                    if(commentUnitOfWork.Count != 1000) {
                        continue;
                    }
                    yield return Coroutine.Invoke(ProcessComments, commentUnitOfWork, commentReport, new Result());
                    commentUnitOfWork.Clear();
                }
                if(pageUnitOfWork.Count != 1000) {
                    continue;
                }
                yield return Coroutine.Invoke(ProcessPages, pageUnitOfWork, pageReport, new Result());
                pageUnitOfWork.Clear();
            }
            yield return Coroutine.Invoke(ProcessPages, pageUnitOfWork, pageReport, new Result());
            yield return Coroutine.Invoke(ProcessComments, commentUnitOfWork, commentReport, new Result());

            // check files
            var fileUnitOfWork = new List<ResourceBE>();
            var fileReport = new Report("files", checkOnly, verbose);
            foreach(var att in _attachmentBL.GetAllAttachementsChunked()) {
                if(!pages.Contains(att.ParentPageId.GetValueOrDefault())) {
                    continue;
                }
                throttle();
                fileUnitOfWork.Add(att);
                if(fileUnitOfWork.Count != 1000) {
                    continue;
                }
                yield return Coroutine.Invoke(ProcessFiles, fileUnitOfWork, fileReport, new Result());
                fileUnitOfWork.Clear();
            }

            // check users
            var userUnitOfWork = new List<UserBE>();
            var userReport = new Report("users", checkOnly, verbose);
            foreach(var user in _userBL.GetAllUsers()) {
                throttle();
                userUnitOfWork.Add(user);
                if(userUnitOfWork.Count != 1000) {
                    continue;
                }
                yield return Coroutine.Invoke(ProcessUsers, userUnitOfWork, userReport, new Result());
                userUnitOfWork.Clear();
            }
            yield return Coroutine.Invoke(ProcessUsers, userUnitOfWork, userReport, new Result());
            var aggregate = new Report("report", checkOnly, verbose);
            result.Return(aggregate.BuildReport(pageReport, commentReport, fileReport, userReport));
        }

        private Yield ProcessUsers(List<UserBE> unitOfWork, Report report, Result result) {
            if(!unitOfWork.Any()) {
                yield break;
            }
            Dictionary<uint, DateTime> indexContents = null;
            yield return _searchBL.GetItems(unitOfWork.Select(x => (ulong)x.ID), SearchResultType.User, new Result<IEnumerable<SearchResultItem>>())
                .Set(x => indexContents = x.ToDictionary(kvp => kvp.TypeId, kvp => kvp.Modified));
            _log.DebugFormat("processing {0}({1}) users", unitOfWork.Count, indexContents.Count);
            foreach(var user in unitOfWork) {
                report.Total++;
                DateTime modified;
                var missing = true;
                if(indexContents.TryGetValue((uint)user.ID, out modified)) {
                    missing = false;
                }
                if(report.Verbose || missing) {
                    report.Document.Start("user").Attr("id", user.ID);
                    if(missing) {
                        report.Missing++;
                        _log.DebugFormat("missing user: {0} ({1})", user.Name, user.ID);
                        report.Document.Attr("error-type", "missing");
                    }
                    report.Document.Elem("name", user.Name);
                    var uri = _userBL.GetUri(user);
                    report.Document.Attr("href", uri);
                    report.Document.End();
                }
                if(report.CheckOnly || !missing) {
                    continue;
                }
                _eventSink.UserUpdate(_now, user);
            }
            result.Return();
        }

        private Yield ProcessPages(List<PageBE> unitOfWork, Report report, Result result) {
            if(!unitOfWork.Any()) {
                yield break;
            }
            Dictionary<uint, DateTime> indexContents = null;
            yield return _searchBL.GetItems(unitOfWork.Select(x => x.ID), SearchResultType.Page, new Result<IEnumerable<SearchResultItem>>())
                .Set(x => indexContents = x.ToDictionary(kvp => kvp.TypeId, kvp => kvp.Modified));
            _log.DebugFormat("processing {0}({1}) pages", unitOfWork.Count, indexContents.Count);
            foreach(var page in unitOfWork) {
                report.Total++;
                DateTime modified;
                var missing = true;
                var stale = true;
                if(indexContents.TryGetValue((uint)page.ID, out modified)) {
                    if(modified.WithoutMilliseconds() >= page.TimeStamp.WithoutMilliseconds()) {
                        stale = false;
                        missing = false;
                    } else {
                        missing = false;
                    }
                }
                if(report.Verbose || missing || stale) {
                    report.Document.Start("page")
                        .Attr("id", page.ID)
                        .Attr("revision", page.Revision);
                    if(missing) {
                        report.Missing++;
                        _log.DebugFormat("missing page: {0} ({1})", page.Title.Path, page.ID);
                        report.Document.Attr("error-type", "missing");
                    } else if(stale) {
                        report.Stale++;
                        _log.DebugFormat("stale page:   {0} ({1})", page.Title.Path, page.ID);
                        report.Document.Attr("error-type", "stale");
                    }
                    if(!missing) {
                        report.Document.Elem("date.index", modified);
                    }
                    report.Document
                        .Elem("date.edited", page.TimeStamp)
                        .Elem("title", page.Title);
                    var uri = _pageBL.GetUriCanonical(page);
                    report.Document.Attr("href", uri);
                    report.Document.Elem("namespace", ((NS)page._Namespace).ToString().ToLowerInvariant());
                    report.Document.End();
                }
                if(report.CheckOnly || (!missing && !stale)) {
                    continue;
                }
                _eventSink.PagePoke(_now, page, _currentUser);
            }
            result.Return();
        }

        private Yield ProcessComments(List<Tuplet<CommentBE, PageBE>> unitOfWork, Report report, Result result) {
            if(!unitOfWork.Any()) {
                yield break;
            }
            Dictionary<uint, DateTime> indexContents = null;
            yield return _searchBL.GetItems(unitOfWork.Select(x => x.Item1.Id), SearchResultType.Comment, new Result<IEnumerable<SearchResultItem>>())
                .Set(x => indexContents = x.ToDictionary(kvp => kvp.TypeId, kvp => kvp.Modified));
            _log.DebugFormat("processing {0}({1}) comments", unitOfWork.Count, indexContents.Count);
            foreach(var tuple in unitOfWork) {
                var comment = tuple.Item1;
                var page = tuple.Item2;
                var commentModified = (comment.LastEditDate ?? comment.CreateDate).WithoutMilliseconds();
                report.Total++;
                DateTime indexModified;
                var missing = true;
                var stale = true;
                if(indexContents.TryGetValue((uint)comment.Id, out indexModified)) {
                    if(indexModified.WithoutMilliseconds() >= commentModified) {
                        stale = false;
                        missing = false;
                    } else {
                        missing = false;
                    }
                }
                if(report.Verbose || missing || stale) {
                    report.Document.Start("file").Attr("id", comment.Id);
                    if(missing) {
                        report.Missing++;
                        _log.DebugFormat("missing comment {0} on page {1}", comment.Id, comment.PageId);
                        report.Document.Attr("error-type", "missing");
                    } else if(stale) {
                        report.Stale++;
                        _log.DebugFormat("stale comment {0} on page {1}", comment.Id, comment.PageId);
                        report.Document.Attr("error-type", "stale");
                    }
                    if(!missing) {
                        report.Document.Elem("date.index", indexModified);
                    }
                    report.Document.Elem("date.edited", commentModified);
                    var uri = _commentBL.GetUri(comment);
                    report.Document.Attr("href", uri);
                    report.Document.End();
                }
                if(report.CheckOnly || (!missing && !stale)) {
                    continue;
                }
                _eventSink.CommentPoke(_now, comment, page, _currentUser);
            }
            result.Return();
        }

        private Yield ProcessFiles(List<ResourceBE> unitOfWork, Report report, Result result) {
            if(!unitOfWork.Any()) {
                yield break;
            }
            Dictionary<uint, DateTime> indexContents = null;
            yield return _searchBL.GetItems(unitOfWork.Select(x => (ulong)x.MetaXml.FileId), SearchResultType.File, new Result<IEnumerable<SearchResultItem>>())
                .Set(x => indexContents = x.ToDictionary(kvp => kvp.TypeId, kvp => kvp.Modified));
            _log.DebugFormat("processing {0}({1}) files", unitOfWork.Count, indexContents.Count);
            foreach(var file in unitOfWork) {
                report.Total++;
                DateTime modified;
                var missing = true;
                var stale = true;
                if(indexContents.TryGetValue(file.MetaXml.FileId.GetValueOrDefault(), out modified)) {
                    if(modified.WithoutMilliseconds() >= file.ResourceUpdateTimestamp.WithoutMilliseconds()) {
                        stale = false;
                        missing = false;
                    } else {
                        missing = false;
                    }
                }
                if(report.Verbose || missing || stale) {
                    report.Document.Start("file")
                        .Attr("id", file.MetaXml.FileId.GetValueOrDefault())
                        .Attr("revision", file.Revision);
                    if(missing) {
                        report.Missing++;
                        _log.DebugFormat("missing file: {0} ({1})", file.Name, file.MetaXml.FileId);
                        report.Document.Attr("error-type", "missing");
                    } else if(stale) {
                        report.Stale++;
                        _log.DebugFormat("stale file:   {0} ({1})", file.Name, file.MetaXml.FileId);
                        report.Document.Attr("error-type", "stale");
                    }
                    if(!missing) {
                        report.Document.Elem("date.index", modified);
                    }
                    report.Document
                        .Elem("date.edited", file.ResourceUpdateTimestamp)
                        .Elem("name", file.Name);
                    var uri = _attachmentBL.GetUri(file);
                    report.Document.Attr("href", uri);
                    report.Document.End();
                }
                if(report.CheckOnly || (!missing && !stale)) {
                    continue;
                }
                _eventSink.AttachmentPoke(_now, file);
            }
            result.Return();
        }

    }
}

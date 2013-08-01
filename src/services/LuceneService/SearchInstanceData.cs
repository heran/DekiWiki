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
using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using MindTouch.Dream;
using MindTouch.Extensions.Time;
using MindTouch.Tasking;
using MindTouch.Threading;
using MindTouch.Xml;

namespace MindTouch.LuceneService {
    using Yield = IEnumerator<IYield>;

    public class SearchInstanceData : IDisposable {

        //--- Class Fields ---
        private static readonly ILog _log = LogUtils.CreateLog();

        //--- Fields ---
        private readonly UpdateDelayQueue _queue;
        private readonly TimeSpan _commitInterval;
        private readonly TaskTimerFactory _taskTimerFactory;
        private readonly FSDirectory _directory;
        private readonly object _updateSyncroot = new object();
        private readonly ReaderWriterLockSlim _disposalLock = new ReaderWriterLockSlim();
        private readonly Analyzer _analyzer;
        private readonly TaskTimer _commitTimer;
        private IndexWriter _writer;
        private IndexReader _reader;
        private IndexSearcher _searcher;
        private bool _searcherIsStale;
        private bool _hasUncommittedData;
        private bool _disposed;

        //--- Constructors ---
        public SearchInstanceData(string indexPath, Analyzer analyzer, UpdateDelayQueue queue, TimeSpan commitInterval, TaskTimerFactory taskTimerFactory) {
            _analyzer = analyzer;
            _directory = FSDirectory.GetDirectory(indexPath);

            // Note (arnec): Needed with SimpleFSLock, since a hard shutdown will have left the lock dangling
            IndexWriter.Unlock(_directory);
            try {
                _writer = new IndexWriter(_directory, _analyzer, IndexWriter.MaxFieldLength.UNLIMITED);
            } catch(CorruptIndexException e) {
                _log.WarnFormat("The Search index at {0} is corrupt. You must repair or delete it before restarting the service. If you delete it, you must rebuild your index after service restart.", indexPath);
                if(e.Message.StartsWith("Unknown format version")) {
                    _log.Warn("The index is considered corrupt because it's an unknown version. Did you accidentally downgrade your install?");
                }
                throw;
            }
            _reader = IndexReader.Open(_directory);
            _searcher = new IndexSearcher(_reader);
            _queue = queue;
            _commitInterval = commitInterval;
            _taskTimerFactory = taskTimerFactory;
            if(_commitInterval != TimeSpan.Zero) {
                _commitTimer = _taskTimerFactory.New(_commitInterval, Commit, null, TaskEnv.None);
            }
        }

        //--- Properties ---
        public int QueueSize {
            get {
                return _disposalLock.ExecuteWithReadLock(() => {
                    EnsureInstanceNotDisposed();
                    return _queue.QueueSize;
                });
            }
        }

        //--- Methods ---
        public void Enqueue(XDoc doc) {
            _disposalLock.ExecuteWithReadLock(() => {
                EnsureInstanceNotDisposed();
                _queue.Enqueue(doc);
            });
        }

        public XDoc GetStats() {
            return _disposalLock.ExecuteWithReadLock(() => {
                EnsureInstanceNotDisposed();
                var ret = new XDoc("stats");
                ret.Elem("numDocs", GetSearcher().GetIndexReader().NumDocs());
                return ret;
            });
        }

        public void Dispose() {
            if(_disposed) {
                return;
            }
            _disposalLock.ExecuteWithWriteLock(() => {
                _disposed = true;
                _writer.Close();
                _searcher.Close();
                _reader.Close();
                _queue.Dispose();
                if(_commitTimer != null) {
                    _commitTimer.Cancel();
                }
            });
        }

        public void Clear() {
            _disposalLock.ExecuteWithWriteLock(() => {
                EnsureInstanceNotDisposed();
                _writer.DeleteAll();
                _queue.Clear();
                _writer.Commit();
                _writer.Close();
                _searcher.Close();
                _reader.Close();
                _writer = new IndexWriter(_directory, _analyzer, true, IndexWriter.MaxFieldLength.UNLIMITED);
                _reader = IndexReader.Open(_directory);
                _searcher = new IndexSearcher(_reader);
            });
        }

        public void AddDocument(Document document) {
            _disposalLock.ExecuteWithReadLock(() => {
                EnsureInstanceNotDisposed();
                lock(_updateSyncroot) {
                    _writer.AddDocument(document);
                    if(_commitInterval == TimeSpan.Zero) {
                        _writer.Commit();
                        _searcherIsStale = true;
                    } else {
                        _hasUncommittedData = true;
                    }
                }
            });
        }

        public void DeleteDocuments(Term term) {
            _disposalLock.ExecuteWithReadLock(() => {
                EnsureInstanceNotDisposed();
                lock(_updateSyncroot) {
                    _writer.DeleteDocuments(term);
                    if(_commitInterval == TimeSpan.Zero) {
                        _writer.Commit();
                        _searcherIsStale = true;
                    } else {
                        _hasUncommittedData = true;
                    }
                }
            });
        }

        public IList<LuceneResult> Search(Query query, SortField sortField, int limit, int offset, float threshold, LuceneProfiler profiler, Plug authPlug) {
            return _disposalLock.ExecuteWithReadLock(() => {
                using(profiler.ProfileQueryInternals()) {
                    EnsureInstanceNotDisposed();
                    var searcher = GetSearcher();
                    int numHits;
                    if(authPlug == null) {
                        numHits = Math.Min(searcher.MaxDoc(), limit == int.MaxValue ? int.MaxValue : limit + offset);
                    } else {
                        var setSizeGuessLong = ((long)offset + limit) * 5;
                        setSizeGuessLong = Math.Max(1000, setSizeGuessLong);
                        var setSizeGuess = setSizeGuessLong.ToInt();
                        numHits = Math.Min(searcher.MaxDoc(), setSizeGuess);
                    }
                    var collector = TopFieldCollector.create(
                        sortField == null ? new Sort() : new Sort(sortField),
                        numHits,
                        false, // fillFields, not required
                        true, // trackDocScores, required
                        true, // trackMaxScore, related to trackDocScores
                        sortField == null // should docs be in docId order?
                    );
                    searcher.Search(query, collector);
                    var docs = collector.TopDocs();
                    var maxscore = docs.GetMaxScore();

                    // Note: cheap way to avoid div/zero
                    if(maxscore == 0) {
                        maxscore = 1;
                    }
                    var items = from hit in docs.scoreDocs
                                let score = hit.score / maxscore
                                where score >= threshold
                                select new LuceneResult(searcher.Doc(hit.doc), score);
                    IList<LuceneResult> resultSet;
                    if(authPlug == null) {
                        if(offset > 0 || limit != int.MaxValue) {
                            items = items.Skip(offset).Take(limit);
                        }
                        resultSet = items.ToList();
                    } else {
                        resultSet = LuceneResultFilter.Filter(authPlug, items, offset, limit, new Result<IList<LuceneResult>>()).Wait();
                    }
                    return resultSet;
                }
            });
        }

        private IndexSearcher GetSearcher() {
            if(_searcherIsStale) {
                lock(_updateSyncroot) {
                    if(_searcherIsStale) {
                        var reader = _reader.Reopen();
                        if(reader != _reader) {
                            _log.DebugFormat("re-opening searcher for {0}", _directory.ToString());
                            _reader.Close();
                            _searcher.Close();
                            _reader = reader;
                            _searcher = new IndexSearcher(_reader);
                        }
                        _searcherIsStale = false;
                    }
                }
            }
            return _searcher;
        }

        private void EnsureInstanceNotDisposed() {
            if(_disposed) {
                throw new ObjectDisposedException("The search instance has been disposed");
            }
        }

        private void Commit(TaskTimer tt) {
            if(_hasUncommittedData) {
                _disposalLock.ExecuteWithReadLock(() => {
                    EnsureInstanceNotDisposed();
                    lock(_updateSyncroot) {
                        _writer.Commit();
                        _searcherIsStale = true;
                        _hasUncommittedData = false;
                    }
                });
            }
            tt.Change(_commitInterval, TaskEnv.None);
        }
    }
}

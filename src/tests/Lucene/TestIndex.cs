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
using System.Collections.Generic;
using System.Linq;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers;
using Lucene.Net.Search;
using Lucene.Net.Store;
using MindTouch.LuceneService;

namespace MindTouch.Lucene.Tests {
    public class TestIndex {

        //--- Fields ---
        public readonly string Default;
        private readonly IndexWriter _writer;
        private readonly QueryParser _parser;
        private readonly RAMDirectory _rd;

        //--- Constructors ---
        public TestIndex() : this("content", new StandardAnalyzer()) { }
        public TestIndex(Analyzer analyzer) : this("content", analyzer) { }

        public TestIndex(string def, Analyzer analyzer) {
            Default = def;
            _rd = new RAMDirectory();
            _writer = new IndexWriter(_rd, analyzer, true, IndexWriter.MaxFieldLength.LIMITED);
            _parser = new QueryParser(def, analyzer);
        }

        //--- Methods ---
        public void Add(Document d) {
            _writer.AddDocument(d);
            _writer.Commit();
        }

        public void Update(Term t, Document d) {
            _writer.UpdateDocument(t, d);
            _writer.Commit();
        }

        public Query Parse(string query) {
            return _parser.Parse(query);
        }

        public List<LuceneResult> Search(string query) {
            return Search(_parser.Parse(query));
        }

        public List<LuceneResult> Search(Query query) {
            return Search(query, null);
        }

        public List<LuceneResult> Search(string query, Sort sort) {
            return Search(_parser.Parse(query), sort);
        }

        public List<LuceneResult> Search(Query query, Sort sort) {
            var searcher = new IndexSearcher(_rd);
            var collector = TopFieldCollector.create(sort ?? new Sort(), searcher.MaxDoc(), false, true, true, sort == null);
            searcher.Search(query, collector);
            var docs = collector.TopDocs();
            var maxscore = docs.GetMaxScore();

            // Note: cheap way to avoid div/zero
            if(maxscore == 0) {
                maxscore = 1;
            }
            return (from hit in docs.scoreDocs
                    let score = hit.score / maxscore
                    where score >= 0.001f
                    select new LuceneResult(searcher.Doc(hit.doc), score)).ToList();
        }
    }

}
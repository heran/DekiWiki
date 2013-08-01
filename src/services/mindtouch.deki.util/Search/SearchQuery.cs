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
using System.Text.RegularExpressions;

namespace MindTouch.Deki.Search {
    public class SearchQuery {

        //--- Class Fields ---
        private static readonly Regex _userTypeRegex = new Regex(@"type:""*user|type:\([^)]*user", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        //--- Class Methods ---
        public static SearchQuery CreateEmpty() {
            return new SearchQuery(null, null, new LuceneClauseBuilder(), null);
        }

        //--- Fields ---
        public readonly string Raw;
        public readonly string LuceneQuery;
        public readonly QueryTerm[] Terms;
        public readonly bool Cacheable;
        private string _orderedTermString;

        //--- Constructors ---
        public SearchQuery(string raw, string processed, LuceneClauseBuilder constraint, IEnumerable<QueryTerm> terms) {
            if(constraint == null) {
                throw new ArgumentException("constraint");
            }
            Raw = raw;
            if(string.IsNullOrEmpty(processed)) {
                LuceneQuery = constraint.Clause;
            } else if(constraint.IsEmpty) {
                LuceneQuery = processed;
            } else {
                var query = new LuceneClauseBuilder();
                query.And("(" + processed + ")");
                query.And(constraint.Clause);
                LuceneQuery = query.Clause;
            }
            Terms = terms == null ? new QueryTerm[0] : terms.ToArray();
            Cacheable = !_userTypeRegex.IsMatch(LuceneQuery);
        }

        //--- Properties ---
        public bool IsEmpty { get { return string.IsNullOrEmpty(LuceneQuery); } }

        //--- Methods ---
        public string GetOrderedNormalizedTermString() {
            if(_orderedTermString != null) {
                return _orderedTermString;
            }
            if(!Terms.Any()) {
                _orderedTermString = Raw;
                return _orderedTermString;
            }
            var q = from t in Terms orderby t.Normalized select t.SafeNormalized;
            return _orderedTermString = Terms.Any() ? string.Join(" ", q.ToArray()) : null;
        }

        public string[] GetNormalizedTerms() {
            if(!Terms.Any()) {
                return new string[0];
            }
            return Terms.Select(t => t.Normalized).ToArray();
        }

        public string GetOrderedTermsHash() {
            var termString = GetOrderedNormalizedTermString();
            return termString == null ? null : StringUtil.ComputeHashString(termString, Encoding.UTF8);
        }


    }
}

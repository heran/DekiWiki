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
using System.Text;

namespace MindTouch.Deki.Search {
    public class QueryTerm : IComparable<QueryTerm> {

        //--- Fields ---
        public readonly string Escaped;
        public readonly string Normalized;
        public readonly bool HasFieldPrefix;
        private readonly bool _hasWhiteSpace;

        //--- Constructors ---
        public QueryTerm(string escaped, string normalized, bool hasFieldPrefix, bool hasWhiteSpace) {
            Escaped = escaped;
            Normalized = normalized.ToLowerInvariant();
            HasFieldPrefix = hasFieldPrefix;
            _hasWhiteSpace = hasWhiteSpace;
        }

        //--- Properties ---
        public string SafeNormalized { get { return _hasWhiteSpace ? "\"" + Normalized + "\"" : Normalized;}}

        //--- Methods ---
        public int CompareTo(QueryTerm other) {
            return Escaped.CompareInvariantIgnoreCase(other.Escaped);
        }

        public override bool Equals(object obj) {
            var right = obj as QueryTerm;
            return right != null && Escaped.EqualsInvariantIgnoreCase(right.Escaped);
        }

        public override int GetHashCode() {
            return Escaped.ToLowerInvariant().GetHashCode();
        }
    }
}
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
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace MindTouch.Deki.Search {
    public class SearchResult : IEnumerable<SearchResultItem> {

        //--- Fields ---
        private readonly IEnumerable<SearchResultItem> _items;

        //--- Constructors ---
        public SearchResult() {
            _items = new SearchResultItem[0];    
        }

        // Note (arnec): This constructor assumes that the provided enumerable is cheap to call .Count()
        // i.e. it shouldn't be lazily computed at access.
        public SearchResult(string parsedQuery, IEnumerable<SearchResultItem> items) {
            _items = items ?? new SearchResultItem[0];
            ExecutedQuery = parsedQuery;
        }

        //--- Properties ---
        public string ExecutedQuery { get; private set; }
        public int Count { get { return _items.Count(); } }

        //--- Methods ---
        public IEnumerator<SearchResultItem> GetEnumerator() {
            return _items.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }
    }
}

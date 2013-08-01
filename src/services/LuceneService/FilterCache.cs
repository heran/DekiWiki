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

namespace MindTouch.LuceneService {
    public class FilterCache : IEnumerable<KeyValuePair<ulong, bool>> {

        //--- Fields ---
        private readonly Dictionary<ulong, bool> _items = new Dictionary<ulong, bool>();

        //--- Methods ---
        public IEnumerable<ulong> NeedsFilterCheck(IEnumerable<ulong?> candidates) {
            var validCandidates = (from id in candidates
                                   where id.HasValue && !_items.ContainsKey(id.Value)
                                   select id.Value).Distinct();
            var needFiltering = new List<ulong>(validCandidates);
            foreach(var id in needFiltering) {
                _items[id] = false;
            }
            return needFiltering;
        }

        public void MarkAsFiltered(IEnumerable<ulong> filtered) {
            foreach(var id in filtered) {
                _items[id] = true;
            }
        }

        public bool IsFiltered(ulong? candidate) {
            if(!candidate.HasValue) {
                return false;
            }
            bool filtered;
            _items.TryGetValue(candidate.Value, out filtered);
            return filtered;
        }

        public IEnumerator<KeyValuePair<ulong, bool>> GetEnumerator() {
            return _items.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }
    }

}

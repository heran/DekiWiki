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
using System.Collections;
using System.Collections.Generic;
using MindTouch.Xml;

namespace MindTouch.Deki.Search {
   
    public class SearchResultDetail : IEnumerable<KeyValuePair<string,string>> {

        //--- Class Methods ---
        public static SearchResultDetail FromXDoc(XDoc detailDoc) {
            var detail = new SearchResultDetail();
            foreach(var elem in detailDoc["*"]) {
                detail[elem.Name] = elem.AsText;
            }

            return detail;
        }

        //--- Fields ---
        private  readonly Dictionary<string, string> _fields = new Dictionary<string, string>();

        //--- Constructors ---
        public SearchResultDetail() {}

        public SearchResultDetail(IEnumerable<KeyValuePair<string, string>> fields) {
            foreach(var field in fields) {
                _fields[field.Key] = field.Value;
            }
        }

        //--- Properties ---
        public string this[string key] {
            get {
                string value;
                _fields.TryGetValue(key, out value);
                return value;
            }
            set {
                _fields[key] = value;
            }
        }

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator() {
            return _fields.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }
    }
}
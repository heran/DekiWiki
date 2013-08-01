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
using MindTouch.Xml;

namespace MindTouch.Deki.UserSubscription {

    // Note: this class is not thread-safe. It is assumed that the using code (NotificationDelayQueue) handles the appropriate
    //       thread locks when manipulating it
    public class NotificationUpdateRecord {

        //--- Class Methods ---
        public static NotificationUpdateRecord FromDocument(XDoc doc) {
            var record = new NotificationUpdateRecord(doc["@wikiid"].AsText, doc["@userid"].AsUInt.Value);
            foreach(var page in doc["page"]) {
                record.Add(page["@id"].AsUInt.Value, page["@modified"].AsDate.Value);
            }
            return record;
        }

        //--- Fields ---
        public readonly string WikiId;
        public readonly uint UserId;
        private readonly Dictionary<uint, DateTime> _pages = new Dictionary<uint, DateTime>();

        //--- Constructors ---
        public NotificationUpdateRecord(string wikiId, uint userId) {
            WikiId = wikiId;
            UserId = userId;
        }

        //--- Properties ---
        public IEnumerable<Tuplet<uint, DateTime>> Pages {
            get {
                foreach(var kvp in _pages) {
                    yield return new Tuplet<uint, DateTime>(kvp.Key, kvp.Value);
                }
            }
        }

        //--- Methods ---
        public void Add(uint pageId, DateTime modificationDate) {
            if(_pages.ContainsKey(pageId)) {
                return;
            }
            _pages[pageId] = modificationDate;
        }

        public XDoc ToDocument() {
            var doc = new XDoc("updateRecord")
                .Attr("wikiid", WikiId)
                .Attr("userid", UserId);
            foreach(var kvp in _pages) {
                doc.Start("page").Attr("id", kvp.Key).Attr("modified", kvp.Value).End();
            }
            return doc;
        }
    }
}

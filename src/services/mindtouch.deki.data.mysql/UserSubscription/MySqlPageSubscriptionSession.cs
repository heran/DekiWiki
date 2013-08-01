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
using System.Data;
using System.Linq;
using MindTouch.Data;
using MindTouch.Deki.Data.UserSubscription;

namespace MindTouch.Deki.Data.MySql.UserSubscription {
    public class MySqlPageSubscriptionSession : IPageSubscriptionDataSession {

        //--- Fields ---
        private readonly DataCatalog _catalog;

        //--- Constructors ---
        public MySqlPageSubscriptionSession(DataCatalog catalog) {
            _catalog = catalog;
        }

        //--- Methods ---
        public void Subscribe(uint userId, uint pageId, bool subscribeChildPages) {
            _catalog.NewQuery("REPLACE INTO pagesub VALUES (?PAGEID, ?USERID, ?SUBCHILD)")
                .With("PAGEID", pageId)
                .With("USERID", userId)
                .With("SUBCHILD", subscribeChildPages ? 1 : 0)
                .Execute();
        }

        public void UnsubscribeUser(uint userId, uint? pageId) {
            if(pageId.HasValue) {
                _catalog.NewQuery("DELETE FROM pagesub WHERE pagesub_page_id = ?PAGEID AND pagesub_user_id = ?USERID")
                    .With("PAGEID", pageId.Value)
                    .With("USERID", userId)
                    .Execute();
            } else {
                _catalog.NewQuery("DELETE FROM pagesub WHERE pagesub_user_id = ?USERID")
                    .With("USERID", userId)
                    .Execute();
            }
        }

        public void UnsubscribePage(uint pageId) {
            _catalog.NewQuery("DELETE FROM pagesub WHERE pagesub_page_id = ?PAGEID")
                .With("PAGEID", pageId)
                .Execute();
        }

        public IEnumerable<PageSubscriptionBE> GetSubscriptionsForUser(uint userId, IEnumerable<uint> pageIds) {
            var subscriptions = new List<PageSubscriptionBE>();
            var pageClause = "";
            if(pageIds != null && pageIds.Any()) {
                pageClause = " AND pagesub_page_id in (" + pageIds.ToCommaDelimitedString() + ")";
            }
            _catalog.NewQuery("SELECT pagesub_user_id, pagesub_page_id, pagesub_child_pages FROM pagesub WHERE pagesub_user_id = ?USERID" + pageClause)
                .With("USERID", userId)
                .Execute(r => {
                    while(r.Read()) {
                        subscriptions.Add(GetSubscription(r));
                    }
                });
            return subscriptions;
        }

        public IEnumerable<PageSubscriptionBE> GetSubscriptionsForPages(IEnumerable<uint> pageIds) {
            var subscriptions = new List<PageSubscriptionBE>();
            var pageClause = " pagesub_page_id in (" + pageIds.ToCommaDelimitedString() + ")";
            _catalog.NewQuery("SELECT pagesub_user_id, pagesub_page_id, pagesub_child_pages FROM pagesub WHERE" + pageClause)
                .Execute(r => {
                    while(r.Read()) {
                        subscriptions.Add(GetSubscription(r));
                    }
                });
            return subscriptions;
        }

        public object Clone() {
            return this;
        }

        public void Dispose() { }

        private PageSubscriptionBE GetSubscription(IDataReader r) {
            return new PageSubscriptionBE() {
                UserId = r.Read<uint>("pagesub_user_id"),
                PageId = r.Read<uint>("pagesub_page_id"),
                IncludeChildPages = r.Read<bool>("pagesub_child_pages")
            };
        }
    }
}

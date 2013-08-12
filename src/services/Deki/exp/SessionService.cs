/*
 * MindTouch DekiWiki - a commercial grade open source wiki
 * Copyright (C) 2006 MindTouch, Inc.
 * www.mindtouch.com  oss@mindtouch.com
 *
 * For community documentation and downloads visit www.opengarden.org;
 *  please review the licensing section.
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
using System.Text;

using MindTouch.Dream;

namespace MindTouch.Deki {
    [DreamService("MindTouch DekiWiki - Session Service", "Copyright (c) 2006 MindTouch, Inc.", "http://doc.opengarden.org/Deki_API/Reference/DekiWiki_Dream_API#Session_Service")]
    public class SessionService : DreamService {

        #region -- Handlers

        /// <summary>
        /// privilege: session-id
        /// in: session-id
        /// out: list of page-info
        /// </summary>
        /// <param name="context"></param>
        [DreamFeature("list-breadcrumbs", "/", "GET", "returns the list of current breadcrumbs", "http://doc.opengarden.org/Deki_API/Reference/DekiWiki_Dream_API#Session_Service")]
        public void GetListBreadcrumbsHandler(DreamContext context) {
        }

        /// <summary>
        /// privilege: session-id
        /// in: session-id, cur-id
        /// out: 
        /// </summary>
        /// <param name="context"></param>
        [DreamFeature("visit", "/", "POST", "adds the given page to the breadcrumbs", "http://doc.opengarden.org/Deki_API/Reference/DekiWiki_Dream_API#Session_Service")]
        public void PostVisitHandler(DreamContext context) {
        }

        /// <summary>
        /// privilege: http-authentication
        /// in: HTTP authentication
        /// out: session-id
        /// </summary>
        /// <param name="context"></param>
        [DreamFeature("login", "/", "POST", "creates a new session-id", "http://doc.opengarden.org/Deki_API/Reference/DekiWiki_Dream_API#Session_Service")]
        public void PostLoginHandler(DreamContext context) {
        }

        /// <summary>
        /// privilege: session-id
        /// in: session-id
        /// out: 
        /// </summary>
        /// <param name="context"></param>
        [DreamFeature("logout", "/", "POST", "deletes the given session-id", "http://doc.opengarden.org/Deki_API/Reference/DekiWiki_Dream_API#Session_Service")]
        public void PostLogoutHandler(DreamContext context) {
        }

        /// <summary>
        /// privilege: admin
        /// in: HTTP authentication
        /// out: list of session-info
        /// </summary>
        /// <param name="context"></param>
        [DreamFeature("sessions", "/", "GET", "as administrator retrieve the list of all sessions.  <br/>a session contains: client browser info, IP, user-id, current page, last actions", "http://doc.opengarden.org/Deki_API/Reference/DekiWiki_Dream_API#Session_Service")]
        public void GetSessionsHandler(DreamContext context) {
        }
        #endregion
    }
}

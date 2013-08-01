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
    [DreamService("MindTouch DekiWiki - Site Service", "Copyright (c) 2006 MindTouch, Inc.", "http://doc.opengarden.org/Deki_API/Reference/DekiWiki_Dream_API#Site_Service")]
    public class SiteService : DreamService {

        #region -- Handlers

        /// <summary>
        /// privilege: read
        /// in: mode={watch, contributions, popular, double redirects, unused redirects, all, ns/page pattern}  [output-mode=rss]
        /// out: list of page-info
        /// </summary>
        /// <param name="context"></param>
        [DreamFeature("list", "/", "GET", "return s page-info list given the mode", "http://doc.opengarden.org/Deki_API/Reference/DekiWiki_Dream_API#Site_Service")]
        public void GetListHandler(DreamContext context) {
        }

        /// <summary>
        /// privilege: read
        /// in: keywords + filter={all,page,images,files,archive} + [start] + [limit] + [output-mode=rss]
        /// out: list of page-info and file-info
        /// </summary>
        /// <param name="context"></param>
        [DreamFeature("search", "/", "GET", "returns a list of page-info and file-infos respecting the given filter", "http://doc.opengarden.org/Deki_API/Reference/DekiWiki_Dream_API#Site_Service")]
        public void GetSearchHandler(DreamContext context) {
        }

        /// <summary>
        /// privilege: none
        /// in: 
        /// out: list of links
        /// </summary>
        /// <param name="context"></param>
        [DreamFeature("top-links", "/", "GET", "returns the list of links", "http://doc.opengarden.org/Deki_API/Reference/DekiWiki_Dream_API#Site_Service")]
        public void GetTopLinksHandler(DreamContext context) {
        }

        /// <summary>
        /// privilege: admin
        /// in: backup, restart, shutdown, reset-cache, factory reset, initialize demo, save demo, restore demo
        /// out: 
        /// </summary>
        /// <param name="context"></param>
        [DreamFeature("action", "/", "POST", "performs specified action", "http://doc.opengarden.org/Deki_API/Reference/DekiWiki_Dream_API#Site_Service")]
        public void PostActionHandler(DreamContext context) {
        }

        /// <summary>
        /// privilege: admin
        /// in: 
        /// out: site-info
        /// </summary>
        /// <param name="context"></param>
        [DreamFeature("site", "/", "GET", "returns the current site settings", "http://doc.opengarden.org/Deki_API/Reference/DekiWiki_Dream_API#Site_Service")]
        public void GetSiteHandler(DreamContext context) {
        }

        /// <summary>
        /// privilege: admin
        /// in: site-info
        /// out: 
        /// </summary>
        /// <param name="context"></param>
        [DreamFeature("site", "/", "POST", "applies given site settings to the wiki", "http://doc.opengarden.org/Deki_API/Reference/DekiWiki_Dream_API#Site_Service")]
        public void PostSiteHandler(DreamContext context) {
        }

        /// <summary>
        /// privilege: admin
        /// in: 
        /// out: network-info
        /// </summary>
        /// <param name="context"></param>
        [DreamFeature("network", "/", "GET", "returns the current network settings", "http://doc.opengarden.org/Deki_API/Reference/DekiWiki_Dream_API#Site_Service")]
        public void GetNetworkHandler(DreamContext context) {
        }

        /// <summary>
        /// privilege: admin
        /// in: network-info
        /// out: 
        /// </summary>
        /// <param name="context"></param>
        [DreamFeature("network", "/", "POST", "changes the current network settings", "http://doc.opengarden.org/Deki_API/Reference/DekiWiki_Dream_API#Site_Service")]
        public void PostNetworkHandler(DreamContext context) {
        }

        /// <summary>
        /// privilege: admin
        /// in: 
        /// out: access-info
        /// </summary>
        /// <param name="context"></param>
        [DreamFeature("access", "/", "GET", "returns the current access modes", "http://doc.opengarden.org/Deki_API/Reference/DekiWiki_Dream_API#Site_Service")]
        public void GetAccessAccessHandler(DreamContext context) {
        }

        /// <summary>
        /// privilege: admin
        /// in: access-info
        /// out: 
        /// </summary>
        /// <param name="context"></param>
        [DreamFeature("access", "/", "POST", "changes the current access modes", "http://doc.opengarden.org/Deki_API/Reference/DekiWiki_Dream_API#Site_Service")]
        public void PostAccessHandler(DreamContext context) {
        }

        /// <summary>
        /// privilege: none
        /// in: locale, id, args
        /// out: string
        /// </summary>
        /// <param name="context"></param>
        [DreamFeature("string", "/", "GET", "returns a localized string given its id, locale, and additional arguments", "http://doc.opengarden.org/Deki_API/Reference/DekiWiki_Dream_API#Site_Service")]
        public void GetStringHandler(DreamContext context) {
        }

        #endregion
    }
}

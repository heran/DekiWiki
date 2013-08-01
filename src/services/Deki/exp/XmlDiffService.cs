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
    [DreamService("MindTouch DekiWiki - XmlDiff Service", "Copyright (c) 2006 MindTouch, Inc.", "http://doc.opengarden.org/Deki_API/Reference/DekiWiki_Dream_API#XmlDiff_Service")]
    public class XmlDiffService : DreamService {

        #region -- Handlers

        /// <summary>
        /// privilege: n/a
        /// in: base + new
        /// out: xml-diffgram
        /// </summary>
        /// <param name="context"></param>
        [DreamFeature("diffgram", "/", "POST", "returns the xml-diffgram between 2 versions", "http://doc.opengarden.org/Deki_API/Reference/DekiWiki_Dream_API#XmlDiff_Service")]
        public void PostDiffgramHandler(DreamContext context) {
        }

        /// <summary>
        /// privilege: n/a
        /// in: base + new
        /// out: viewable xml-diff
        /// </summary>
        /// <param name="context"></param>
        [DreamFeature("view", "/", "POST", "returns the viewable xml-diff between 2 versions", "http://doc.opengarden.org/Deki_API/Reference/DekiWiki_Dream_API#XmlDiff_Service")]
        public void PostViewHandler(DreamContext context) {
        }

        /// <summary>
        /// privilege: n/a
        /// in: base + diffgram
        /// out: new
        /// </summary>
        /// <param name="context"></param>
        [DreamFeature("apply", "/", "POST", "returns the new XDoc with diffgram applied", "http://doc.opengarden.org/Deki_API/Reference/DekiWiki_Dream_API#XmlDiff_Service")]
        public void PostApplyHandler(DreamContext context) {
        }

        #endregion

    }
}

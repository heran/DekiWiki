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

using MindTouch.Dream;
using MindTouch.Xml;

namespace MindTouch.Deki {
    public partial class DekiWikiService {

        //--- Features ---
#if DEBUG
        [DreamFeature("GET:host/stop", "Stop the request associate tenant, if running")]
#endif
        [DreamFeature("POST:host/stop", "Stop  the request associate tenant, if running")]
        internal DreamMessage PostHostInstanceStop(DreamContext context, DreamMessage request) {
            var dekiContext = DekiContext.CurrentOrNull;
            if(dekiContext != null) {
                if(Instancemanager.ShutdownCurrentInstance()) {
                    return DreamMessage.Ok(new XDoc("tenant").Attr("wikiid", dekiContext.Instance.Id).Attr("status", "stopped"));
                }
                return new DreamMessage(DreamStatus.ServiceUnavailable, null);
            }
            return DreamMessage.Ok(new XDoc("tenant").Attr("status", "notrunning"));
        }

        [DreamFeature("POST:host/stop/{wikiid}", "Stop the requested tenant, if running")]
        internal DreamMessage PostHostInstanceStopByWikiId(DreamContext context, DreamMessage request) {
            var wikiId = context.GetParam("wikiid", null);
            if(!string.IsNullOrEmpty(wikiId)) {
                if(Instancemanager.ShutdownInstance(wikiId)) {
                    return DreamMessage.Ok(new XDoc("tenant").Attr("wikiid", wikiId).Attr("status", "stopped"));
                }
                return new DreamMessage(DreamStatus.ServiceUnavailable, null);
            }
            return DreamMessage.Ok(new XDoc("tenant").Attr("wikiid", wikiId).Attr("status", "notrunning"));
        }

        [DreamFeature("GET:host", "Get all currently running tenants")]
        internal DreamMessage GetHostInstances(DreamContext context, DreamMessage request) {
            var response = new XDoc("tenants");
            foreach(var status in Instancemanager.InstanceStatuses) {
                response.Start("tenant")
                    .Attr("wikiid", status.Key)
                    .Attr("status", status.Value)
                .End();
            }
            return DreamMessage.Ok(response);
        }
    }
}
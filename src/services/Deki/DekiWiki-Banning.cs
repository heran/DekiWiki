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

using System.Collections.Generic;

using MindTouch.Deki.Data;
using MindTouch.Deki.Exceptions;
using MindTouch.Deki.Logic;
using MindTouch.Dream;
using MindTouch.Tasking;
using MindTouch.Xml;

namespace MindTouch.Deki {
    using Yield = IEnumerator<IYield>;

    public partial class DekiWikiService {

        //--- Features ---
        [DreamFeature("GET:site/bans", "Get a list of all IP and user bans")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        [DreamFeatureStatus(DreamStatus.Ok, "Request completed successfully")]
        [DreamFeatureStatus(DreamStatus.Forbidden, "ADMIN access is required")]        
        public Yield GetBans(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            PermissionsBL.CheckUserAllowed(DekiContext.Current.User, Permissions.ADMIN);
            response.Return(DreamMessage.Ok(BanningBL.RetrieveBans()));
            yield break;
        }

        [DreamFeature("GET:site/bans/{banid}", "See a specific ban entry")]
        [DreamFeatureParam("{banid}", "int", "Identifies a ban by ID")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        [DreamFeatureStatus(DreamStatus.Ok, "Request completed successfully")]
        [DreamFeatureStatus(DreamStatus.Forbidden, "ADMIN access is required")]
        [DreamFeatureStatus(DreamStatus.NotFound, "Ban ID does not exist")]      
        public Yield GetBan(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            PermissionsBL.CheckUserAllowed(DekiContext.Current.User, Permissions.ADMIN);
            BanBE ban = GetBanFromRequest(context, context.GetParam<uint>("banid"));
            response.Return(DreamMessage.Ok(BanningBL.GetBanXml(ban)));
            yield break;
        }

        [DreamFeature("POST:site/bans", "Create a ban entry")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        [DreamFeatureStatus(DreamStatus.Ok, "Request completed successfully")]
        [DreamFeatureStatus(DreamStatus.BadRequest, "Invalid request body")]
        [DreamFeatureStatus(DreamStatus.Forbidden, "ADMIN access is required")]
        public Yield PostBans(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            PermissionsBL.CheckUserAllowed(DekiContext.Current.User, Permissions.ADMIN);
            BanBE ban = BanningBL.SaveBan(request.ToDocument());
            DekiContext.Current.Instance.EventSink.BanCreated(DekiContext.Current.Now, ban);
            response.Return(DreamMessage.Ok(BanningBL.GetBanXml(ban)));
            yield break;
        }

        [DreamFeature("DELETE:site/bans/{banid}", "Remove a ban entry")]
        [DreamFeatureParam("{banid}", "int", "Identifies a ban by ID")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        [DreamFeatureStatus(DreamStatus.Ok, "Request completed successfully")]
        [DreamFeatureStatus(DreamStatus.Forbidden, "ADMIN access is required")]
        [DreamFeatureStatus(DreamStatus.NotFound, "Ban ID does not exist")]    
        public Yield DeleteBan(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            PermissionsBL.CheckUserAllowed(DekiContext.Current.User, Permissions.ADMIN);
            BanBE ban = GetBanFromRequest(context, context.GetParam<uint>("banid"));
            BanningBL.DeleteBan(ban);
            DekiContext.Current.Instance.EventSink.BanRemoved(DekiContext.Current.Now, ban);
            response.Return(DreamMessage.Ok());
            yield break;
        }

        private BanBE GetBanFromRequest(DreamContext context, uint banid) {
            BanBE ban = BanningBL.GetById(banid);
            if (ban == null) {
                throw new BanIdNotFoundException(banid);
            }
            return ban;        
        }    
    }
}

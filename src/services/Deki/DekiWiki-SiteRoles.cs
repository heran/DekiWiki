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
        //--- Constants ---
        //--- Class Methods ---
        //--- Features ---

        [DreamFeature("GET:site/roles", "Retrieve list of defined roles")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        [DreamFeatureStatus(DreamStatus.Ok, "The request completed successfully")]
        public Yield GetSiteRoles(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            IList<RoleBE> roles = DbUtils.CurrentSession.RolesRestrictions_GetRoles();
            XDoc ret = new XDoc("roles");
            ret.Attr("href", DekiContext.Current.ApiUri.At("site", "roles"));
            if(roles != null) {
                foreach(RoleBE r in roles) {
                    ret.Add(PermissionsBL.GetRoleXml(r, null));
                }
            }
            response.Return(DreamMessage.Ok(ret));
            yield break;
        }

        [DreamFeature("GET:site/roles/{roleid}", "Retrieve a role")]
        [DreamFeatureParam("{roleid}", "string", "either an integer role ID or \"=\" followed by a double uri-encoded role name")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        [DreamFeatureStatus(DreamStatus.Ok, "The request completed successfully")]
        [DreamFeatureStatus(DreamStatus.BadRequest, "Invalid input parameter or request body")]
        [DreamFeatureStatus(DreamStatus.NotFound, "Requested role could not be found")]
        public Yield GetSiteRole(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            RoleBE role = GetRoleFromUrl();
            XDoc ret = PermissionsBL.GetRoleXml(role, null);
            response.Return(DreamMessage.Ok(ret));
            yield break;
        }

        [DreamFeature("PUT:site/roles/{roleid}", "Modify or add a role")]
        [DreamFeatureParam("{roleid}", "string", "either an integer role ID or \"=\" followed by a double uri-encoded role name")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        [DreamFeatureStatus(DreamStatus.Ok, "The request completed successfully")]
        [DreamFeatureStatus(DreamStatus.BadRequest, "Invalid input parameter or request body")]
        [DreamFeatureStatus(DreamStatus.Forbidden, "Administrator access is required")]
        [DreamFeatureStatus(DreamStatus.NotFound, "Requested role could not be found")]
        public Yield PutSiteRole(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            PermissionsBL.CheckUserAllowed(DekiContext.Current.User, Permissions.ADMIN);

            RoleBE role = GetRoleFromUrl(false);
            role = PermissionsBL.PutRole(role, request, context);
            response.Return(DreamMessage.Ok(PermissionsBL.GetRoleXml(role, null)));
            yield break;
        }

        [DreamFeature("GET:site/operations", "Retrieve all known security operations")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        [DreamFeatureStatus(DreamStatus.Ok, "The request completed successfully")]
        public Yield GetSiteOperations(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            XDoc ret = new XDoc("operations");
            ret.Attr("href", DekiContext.Current.ApiUri.At("site", "operations"));
            ret.Value(string.Join(",", PermissionsBL.PermissionsToArray(ulong.MaxValue)));
            response.Return(DreamMessage.Ok(ret));
            yield break;
        }

        #region Helper methods

        private RoleBE GetRoleFromUrl() {
            return GetRoleFromUrl(true);
        }

        private RoleBE GetRoleFromUrl(bool mustExist) {
            RoleBE r;
            string roleid = DreamContext.Current.GetParam("roleid");

            // Double decoding of name is done to work around a mod_proxy issue that strips out slashes
            roleid = XUri.Decode(roleid);
            if(roleid.StartsWith("=")) {
                string name = roleid.Substring(1);
                r = PermissionsBL.GetRoleByName(name);
                if(r == null && mustExist) {
                    throw new SiteRoleNameNotFoundException(name);
                }
            } else {
                uint roleIdInt;
                if(!uint.TryParse(roleid, out roleIdInt)) {
                    throw new SiteRoleIdInvalidArgumentException();
                }
                r = PermissionsBL.GetRoleById(roleIdInt);
                if(r == null && mustExist) {
                    throw new SiteRoleIdNotFoundException(roleIdInt);
                }
            }
            return r;
        }
        #endregion

    }
}

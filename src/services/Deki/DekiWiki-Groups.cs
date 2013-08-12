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
using System.Linq;

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

        [DreamFeature("GET:groups", "Retrieve list of groups.")]
        [DreamFeatureParam("groupnamefilter", "string?", "Search for groups by name or part of a name")]
        [DreamFeatureParam("authprovider", "int?", "Return groups belonging to given authentication service id")]
        [DreamFeatureParam("limit", "string?", "Maximum number of items to retrieve. Must be a positive number or 'all' to retrieve all items. (default: 100)")]
        [DreamFeatureParam("offset", "int?", "Number of items to skip. Must be a positive number or 0 to not skip any. (default: 0)")]
        [DreamFeatureParam("sortby", "{id, name, role, service}?", "Sort field. Prefix value with '-' to sort descending. default: No sorting")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        [DreamFeatureStatus(DreamStatus.Ok, "Request completed successfully")]
        [DreamFeatureStatus(DreamStatus.BadRequest, "Invalid input parameter or request body")]
        [DreamFeatureStatus(DreamStatus.Forbidden, "Read access is required")]
        public Yield GetGroups(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            PermissionsBL.CheckUserAllowed(DekiContext.Current.User, Permissions.READ);
            uint totalCount, queryCount;
            IList<GroupBE> groups = GroupBL.GetGroupsByQuery(context, out totalCount, out queryCount);

            XDoc result = new XDoc("groups");
            result.Attr("count", groups.Count);
            result.Attr("querycount", queryCount);
            result.Attr("totalcount", totalCount);
            result.Attr("href", DekiContext.Current.ApiUri.At("groups"));

            foreach(GroupBE g in groups) {
                result.Add(GroupBL.GetGroupXmlVerbose(g, null));
            }

            response.Return(DreamMessage.Ok(result));
            yield break;
        }

        [DreamFeature("GET:groups/{groupid}", "Retrieve group information")]
        [DreamFeatureParam("{groupid}", "string", "either an integer group ID or \"=\" followed by a double uri-encoded group name")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        [DreamFeatureStatus(DreamStatus.Ok, "Request completed successfully")]
        [DreamFeatureStatus(DreamStatus.BadRequest, "Invalid input parameter or request body")]
        [DreamFeatureStatus(DreamStatus.Forbidden, "Read access is required")]
        [DreamFeatureStatus(DreamStatus.NotFound, "Requested group could not be found")]
        public Yield GetGroup(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            PermissionsBL.CheckUserAllowed(DekiContext.Current.User, Permissions.READ);
            GroupBE group = GetGroupFromUrl();
            DreamMessage responseMsg = DreamMessage.Ok(GroupBL.GetGroupXmlVerbose(group, null));
            response.Return(responseMsg);
            yield break;
        }

        [DreamFeature("GET:groups/{groupid}/users", "Return a list of users in a group")]
        [DreamFeatureParam("{groupid}", "string", "either an integer group ID or \"=\" followed by a double uri-encoded group name")]
        [DreamFeatureParam("usernamefilter", "string?", "Search for users by name or part of a name")]
        [DreamFeatureParam("rolefilter", "string?", "Search for users by a role name")]
        [DreamFeatureParam("activatedfilter", "bool?", "Search for users by their active status")]
        [DreamFeatureParam("limit", "string?", "Maximum number of items to retrieve. Must be a positive number or 'all' to retrieve all items. (default: 100)")]
        [DreamFeatureParam("offset", "int?", "Number of items to skip. Must be a positive number or 0 to not skip any. (default: 0)")]
        [DreamFeatureParam("sortby", "{id, username, nick, email, fullname, date.lastlogin, status, role, service}?", "Sort field. Prefix value with '-' to sort descending. default: No sorting")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        [DreamFeatureStatus(DreamStatus.Ok, "Request completed successfully")]
        [DreamFeatureStatus(DreamStatus.BadRequest, "Invalid input parameter or request body")]
        [DreamFeatureStatus(DreamStatus.Forbidden, "Read access is required")]
        [DreamFeatureStatus(DreamStatus.NotFound, "Requested group could not be found")]
        public Yield GetGroupUsers(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            PermissionsBL.CheckUserAllowed(DekiContext.Current.User, Permissions.READ);
            GroupBE group = GetGroupFromUrl();
            DreamMessage responseMsg = null;

            uint totalCount;
            uint queryCount;
            var usersInGroup = UserBL.GetUsersByQuery(context, group.Id, out totalCount, out queryCount);

            XDoc ret = new XDoc("users");
            ret.Attr("count", usersInGroup.Count());
            ret.Attr("querycount", queryCount);
            ret.Attr("totalcount", totalCount);
            ret.Attr("href", DekiContext.Current.ApiUri.At("groups", group.Id.ToString(), "users"));

            foreach(UserBE member in usersInGroup)
                ret.Add(UserBL.GetUserXml(member, null, Utils.ShowPrivateUserInfo(member)));

            responseMsg = DreamMessage.Ok(ret);

            response.Return(responseMsg);
            yield break;
        }

        [DreamFeature("POST:groups", "Add or modify a group")]
        [DreamFeatureParam("authusername", "string?", "Username to use for verification with external authentication service")]
        [DreamFeatureParam("authpassword", "string?", "Password to use for verification with external authentication service")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        [DreamFeatureStatus(DreamStatus.Ok, "Request completed successfully")]
        [DreamFeatureStatus(DreamStatus.BadRequest, "Invalid input parameter or request body")]
        [DreamFeatureStatus(DreamStatus.Forbidden, "Administrator access is required")]
        [DreamFeatureStatus(DreamStatus.NotFound, "Requested group could not be found")]
        [DreamFeatureStatus(DreamStatus.Conflict, "Group already exists")]
        public Yield PostGroup(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            PermissionsBL.CheckUserAllowed(DekiContext.Current.User, Permissions.ADMIN);
            DreamMessage responseMsg = null;
            GroupBE group = GroupBL.PostGroupFromXml(request.ToDocument(), null, context.GetParam("authusername", null), context.GetParam("authpassword", null));
            responseMsg = DreamMessage.Ok(GroupBL.GetGroupXmlVerbose(group, null));
            response.Return(responseMsg);
            yield break;
        }

        [DreamFeature("PUT:groups/{groupid}", "Modify an existing group")]
        [DreamFeatureParam("{groupid}", "string", "either an integer group ID or \"=\" followed by a double uri-encoded group name")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        [DreamFeatureStatus(DreamStatus.Ok, "Request completed successfully")]
        [DreamFeatureStatus(DreamStatus.BadRequest, "Invalid input parameter or request body")]
        [DreamFeatureStatus(DreamStatus.Forbidden, "Administrator access is required")]
        [DreamFeatureStatus(DreamStatus.NotFound, "Requested group could not be found")]
        public Yield PutGroup(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            PermissionsBL.CheckUserAllowed(DekiContext.Current.User, Permissions.ADMIN);
            GroupBE group = GetGroupFromUrl();
            group = GroupBL.PostGroupFromXml(request.ToDocument(), group, null, null);
            response.Return(DreamMessage.Ok(GroupBL.GetGroupXmlVerbose(group, null)));
            yield break;
        }

        [DreamFeature("DELETE:groups/{groupid}", "Remove a group")]
        [DreamFeatureParam("{groupid}", "string", "either an integer group ID or \"=\" followed by a double uri-encoded group name")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        [DreamFeatureStatus(DreamStatus.Ok, "Request completed successfully")]
        [DreamFeatureStatus(DreamStatus.BadRequest, "Invalid input parameter or request body")]
        [DreamFeatureStatus(DreamStatus.Forbidden, "Administrator access is required")]
        [DreamFeatureStatus(DreamStatus.NotFound, "Requested group could not be found")]
        public Yield DeleteGroup(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            PermissionsBL.CheckUserAllowed(DekiContext.Current.User, Permissions.ADMIN);
            GroupBE group = GetGroupFromUrl();
            DbUtils.CurrentSession.Groups_Delete(group.Id);
            response.Return(DreamMessage.Ok());
            yield break;
        }

        [DreamFeature("PUT:groups/{groupid}/users", "Set the members for a group")]
        [DreamFeatureParam("{groupid}", "string", "either an integer group ID or \"=\" followed by a double uri-encoded group name")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        [DreamFeatureStatus(DreamStatus.Ok, "Request completed successfully")]
        [DreamFeatureStatus(DreamStatus.BadRequest, "Invalid input parameter or request body")]
        [DreamFeatureStatus(DreamStatus.Forbidden, "Administrator access is required")]
        [DreamFeatureStatus(DreamStatus.NotFound, "Requested group could not be found")]
        public Yield PutGroupUsers(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            PermissionsBL.CheckUserAllowed(DekiContext.Current.User, Permissions.ADMIN);
            GroupBE group = GetGroupFromUrl();
            group = GroupBL.SetGroupMembers(group, request.ToDocument());
            response.Return(DreamMessage.Ok(GroupBL.GetGroupXmlVerbose(group, null)));
            yield break;
        }

        [DreamFeature("POST:groups/{groupid}/users", "Add members to a group")]
        [DreamFeatureParam("{groupid}", "string", "either an integer group ID or \"=\" followed by a double uri-encoded group name")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        [DreamFeatureStatus(DreamStatus.Ok, "Request completed successfully")]
        [DreamFeatureStatus(DreamStatus.BadRequest, "Invalid input parameter or request body")]
        [DreamFeatureStatus(DreamStatus.Forbidden, "Administrator access is required")]
        [DreamFeatureStatus(DreamStatus.NotFound, "Requested group could not be found")]
        public Yield PostGroupUsers(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            PermissionsBL.CheckUserAllowed(DekiContext.Current.User, Permissions.ADMIN);
            GroupBE group = GetGroupFromUrl();
            group = GroupBL.AddGroupMembers(group, request.ToDocument());
            response.Return(DreamMessage.Ok(GroupBL.GetGroupXmlVerbose(group, null)));
            yield break;
        }

        [DreamFeature("DELETE:groups/{groupid}/users/{userid}", "Remove given member from a group")]
        [DreamFeatureParam("{groupid}", "string", "either an integer group ID or \"=\" followed by a double uri-encoded group name")]
        [DreamFeatureParam("{userid}", "string", "either an integer user ID, \"current\", or \"=\" followed by a double uri-encoded user name")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        [DreamFeatureStatus(DreamStatus.Ok, "Request completed successfully")]
        [DreamFeatureStatus(DreamStatus.BadRequest, "Invalid input parameter")]
        [DreamFeatureStatus(DreamStatus.Forbidden, "Administrator access is required")]
        [DreamFeatureStatus(DreamStatus.NotFound, "Requested group could not be found")]
        public Yield DeleteGroupUser(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            PermissionsBL.CheckUserAllowed(DekiContext.Current.User, Permissions.ADMIN);
            GroupBE group = GetGroupFromUrl();
            UserBE user = GetUserFromUrlMustExist();
            group = GroupBL.RemoveGroupMember(group, user);
            response.Return(DreamMessage.Ok(GroupBL.GetGroupXmlVerbose(group, null)));
            yield break;
        }


        #region Helper methods

        private GroupBE GetGroupFromUrl() {
            GroupBE g;
            string groupid = DreamContext.Current.GetParam("groupid");

            //Double decoding of name is done to work around a mod_proxy issue that strips out slashes
            groupid = XUri.Decode(groupid);
            if(groupid.StartsWith("=")) {
                string name = groupid.Substring(1);
                g = GroupBL.GetGroupByName(name);
                if(g == null) {
                    throw new GroupNotFoundException(name);
                }
            } else {
                uint groupIdInt;
                if(!uint.TryParse(groupid, out groupIdInt)) {
                    throw new GroupIdInvalidArgumentException();
                }
                g = GroupBL.GetGroupById(groupIdInt);
                if(g == null) {
                    throw new GroupIdNotFoundException(groupIdInt);
                }
            }
            return g;
        }
        #endregion
    }
}

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
using System.Linq;

using MindTouch.Deki.Data;
using MindTouch.Deki.Exceptions;
using MindTouch.Dream;
using MindTouch.Xml;

namespace MindTouch.Deki.Logic {
    public class GroupBL {

        public static GroupBE GetGroupByName(string groupName) {
            return DbUtils.CurrentSession.Groups_GetByNames(new List<string>() { groupName }).FirstOrDefault();
        }

        public static GroupBE GetGroupById(uint groupId) {
            return DbUtils.CurrentSession.Groups_GetByIds(new List<uint>() { groupId }).FirstOrDefault();
        }

        public static IList<GroupBE> GetGroupsByQuery(DreamContext context, out uint totalCount, out uint queryCount) {
            uint limit, offset;
            SortDirection sortDir;
            string sortFieldString;
            Utils.GetOffsetAndCountFromRequest(context, 100, out limit, out offset, out sortDir, out sortFieldString);

            // Attempt to read the sort field.  If a parsing error occurs, default to undefined.
            GroupsSortField sortField = GroupsSortField.UNDEFINED;
            if(!String.IsNullOrEmpty(sortFieldString)) {
                try { sortField = SysUtil.ChangeType<GroupsSortField>(sortFieldString); } catch { }
            }
            string groupnamefilter = context.GetParam("groupnamefilter", null);
            uint? serviceid = context.GetParam<uint>("authprovider", 0);
            if((serviceid ?? 0) == 0) {
                serviceid = null;
            }
            return DbUtils.CurrentSession.Groups_GetByQuery(groupnamefilter, serviceid, sortDir, sortField, offset, limit, out totalCount, out queryCount);
        }

        public static GroupBE PostGroupFromXml(XDoc groupDoc, GroupBE groupToProcess, string externalusername, string externalpassword) {
            GroupBE group = null;
            string groupName = string.Empty;
            ServiceBE groupService = null;
            RoleBE groupRole = null;
            UserBE[] groupMembers = null;
            uint? groupId = null;

            ParseGroupXml(groupDoc, out groupId, out groupName, out groupService, out groupRole, out groupMembers);

            //Create new group
            if(groupToProcess == null && (groupId == null || groupId == 0)) {

                if(groupService == null)
                    groupService = ServiceBL.RetrieveLocalAuthService();

                //External groups should be confirmed with the auth provider
                if(groupService != null && !ServiceBL.IsLocalAuthService(groupService)) {

                    //username+password from request query params are used here
                    group = ExternalServiceSA.BuildGroupFromAuthService(groupService, groupToProcess, groupName, externalusername, externalpassword);

                    if(group == null) {
                        throw new ExternalGroupNotFoundException(groupName);
                    }
                }

                //Does this group already exist?
                GroupBE tempGroup = GetGroupByName(groupName);
                if(tempGroup != null) {
                    throw new GroupExistsWithServiceConflictException(groupName, tempGroup.ServiceId);
                }

                ValidateGroupMemberList(groupService, groupMembers);

                // Insert the group
                GroupBE newGroup = new GroupBE();
                newGroup.Name = groupName;
                newGroup.RoleId = groupRole.ID;
                newGroup.ServiceId = groupService.Id;
                newGroup.CreatorUserId = DekiContext.Current.User.ID;
                newGroup.TimeStamp = DateTime.UtcNow;
                uint newGroupId = DbUtils.CurrentSession.Groups_Insert(newGroup);
                if(newGroupId == 0) {
                    group = null;
                } else {
                    DbUtils.CurrentSession.GroupMembers_UpdateUsersInGroup(newGroupId, groupMembers.Select(e => e.ID).ToList(), newGroup.TimeStamp);

                    // reload the group to ensure group members are set
                    group = GetGroupById(newGroupId);
                }
            }
                //Edit existing group
            else {
                if(groupId != null) {
                    groupToProcess = GetGroupById(groupId.Value);
                }

                if(groupToProcess == null) {
                    throw new GroupIdNotFoundException(groupId);
                }

                group = groupToProcess;

                //Change the role?
                if(group.RoleId != groupRole.ID) {
                    group.RoleId = groupRole.ID;
                }

                //Rename the group?
                if(group.Name != groupName && !string.IsNullOrEmpty(groupName)) {

                    GroupBE tempGroup = GetGroupByName(groupName);

                    if(tempGroup != null) {
                        throw new GroupExistsWithServiceConflictException(groupName, tempGroup.ServiceId);
                    }

                    if(!ServiceBL.IsLocalAuthService(group.ServiceId)) {

                        //TODO MaxM: allow renaming of external groups
                        throw new ExternalGroupRenameNotImplementedException();
                    }

                    //Set the new name of the group.
                    group.Name = groupName;
                }

                DbUtils.CurrentSession.Groups_Update(group);
                //TODO (MaxM): Update group list as well?
                group = GetGroupById(group.Id);

            }

            if(group == null) {
                throw new GroupCreateUpdateFatalException();
            }

            return group;
        }

        public static GroupBE SetGroupMembers(GroupBE group, XDoc userList) {
            UserBE[] members = ProcessGroupMemberInput(group, userList);
            DbUtils.CurrentSession.GroupMembers_UpdateUsersInGroup(group.Id, members.Select(e => e.ID).ToList(), DateTime.UtcNow);

            // reload the group to ensure group members are set
            return GetGroupById(group.Id);
        }

        private static IEnumerable<UserBE> GetMemberUsers(GroupBE group) {
            return (group.UserIdsList == null || !group.UserIdsList.Any())
                ? new UserBE[0]
                : DbUtils.CurrentSession.Users_GetByIds(group.UserIdsList);
        }

        public static GroupBE AddGroupMembers(GroupBE group, XDoc userList) {
            UserBE[] newMembers = ProcessGroupMemberInput(group, userList);
            if(ArrayUtil.IsNullOrEmpty(newMembers)) {
                return group;
            }

            var members = GetMemberUsers(group).ToList();
            members.AddRange(newMembers);

            DbUtils.CurrentSession.GroupMembers_UpdateUsersInGroup(group.Id, members.Select(e => e.ID).ToList(), DateTime.UtcNow);
            return GetGroupById(group.Id);
        }

        public static GroupBE RemoveGroupMember(GroupBE group, UserBE user) {
            List<UserBE> members = new List<UserBE>();
            foreach(UserBE u in GetMemberUsers(group)) {
                if(u.ID != user.ID) {
                    members.Add(u);
                }
            }

            DbUtils.CurrentSession.GroupMembers_UpdateUsersInGroup(group.Id, members.Select(e => e.ID).ToList(), DateTime.UtcNow);
            return GetGroupById(group.Id);
        }

        private static UserBE[] ProcessGroupMemberInput(GroupBE group, XDoc userList) {
            if(!userList.HasName("users")) {
                throw new GroupExpectedUserRootNodeInvalidArgumentException();
            }

            ServiceBE service = ServiceBL.GetServiceById(group.ServiceId);
            if(service == null) {
                throw new GroupServiceNotFoundFatalException(group.ServiceId, group.Name);
            }

            //Changing members of an external group is not supported. You may modify members in the external provider instead.
            if(!ServiceBL.IsLocalAuthService(service)) {
                throw new ExternalGroupMemberInvalidOperationException();
            }

            UserBE[] members = ReadUserListXml(userList);
            ValidateGroupMemberList(service, members);
            return members;
        }

        private static void ValidateGroupMemberList(ServiceBE groupService, UserBE[] potentialMembers) {

            //Groups belonging to built-in auth service are allowed to contain users from remote services
            if(!ServiceBL.IsLocalAuthService(groupService)) {
                foreach(UserBE u in potentialMembers) {
                    if(u.ServiceId != groupService.Id)
                        throw new GroupMembersRequireSameAuthInvalidOperationException();
                }
            }
        }

        #region XML Helpers

        private static void ParseGroupXml(XDoc groupDoc, out uint? id, out string name, out ServiceBE authService, out RoleBE role, out UserBE[] userList) {

            name = groupDoc["groupname"].AsText ?? groupDoc["name"].AsText;
            string authserviceidstr = groupDoc["service.authentication/@id"].AsText;
            string rolestr = groupDoc["permissions.group/role"].AsText;
            authService = null;
            role = null;
            id = null;


            if(!groupDoc["@id"].IsEmpty) {
                uint id_temp;
                if(!uint.TryParse(groupDoc["@id"].Contents, out id_temp))
                    throw new GroupIdAttributeInvalidArgumentException();
                id = id_temp;
            }

            if(!string.IsNullOrEmpty(authserviceidstr)) {
                uint serviceid;
                if(!uint.TryParse(authserviceidstr, out serviceid))
                    throw new ServiceAuthIdAttrInvalidArgumentException();

                authService = ServiceBL.GetServiceById(serviceid);
                if(authService == null)
                    throw new ServiceDoesNotExistInvalidArgumentException(serviceid);
            }

            if(!string.IsNullOrEmpty(rolestr)) {
                role = PermissionsBL.GetRoleByName(rolestr);
                if(role == null)
                    throw new RoleDoesNotExistInvalidArgumentException(rolestr);
            } else {
                role = PermissionsBL.RetrieveDefaultRoleForNewAccounts();
            }
            if(!groupDoc["users"].IsEmpty) {
                userList = ReadUserListXml(groupDoc["users"]);
            } else
                userList = new UserBE[] { };
        }

        private static UserBE[] ReadUserListXml(XDoc usersList) {
            UserBE[] ret = null;
            List<uint> userIds = new List<uint>();
            foreach(XDoc userXml in usersList["user/@id"]) {
                uint? id = userXml.AsUInt;
                if(id == null)
                    throw new UserIdAttrInvalidArgumentException();

                userIds.Add(id.Value);
            }

            if(userIds.Count > 0) {
                Dictionary<uint, UserBE> userHash = DbUtils.CurrentSession.Users_GetByIds(userIds).AsHash(e => e.ID);

                foreach(uint id in userIds) {
                    if(!userHash.ContainsKey(id))
                        throw new GroupCouldNotFindUserInvalidArgumentException(id);
                }

                ret = new List<UserBE>(userHash.Values).ToArray();
            } else {
                ret = new UserBE[] { };
            }

            return ret;
        }

        public static XDoc GetGroupXml(GroupBE group, string relation) {
            XDoc groupXml = new XDoc(string.IsNullOrEmpty(relation) ? "group" : "group." + relation);

            groupXml.Attr("id", group.Id);
            groupXml.Attr("href", DekiContext.Current.ApiUri.At("groups", group.Id.ToString()));
            groupXml.Start("groupname").Value(group.Name).End();

            return groupXml;
        }

        public static XDoc GetGroupXmlVerbose(GroupBE group, string relation) {

            XDoc groupXml = GetGroupXml(group, relation);

            ServiceBE authService = ServiceBL.GetServiceById(group.ServiceId);
            if(authService != null)
                groupXml.Add(ServiceBL.GetServiceXml(authService, "authentication"));

            groupXml.Start("users");
            if(group.UserIdsList != null) {
                groupXml.Attr("count", group.UserIdsList.Length);
            }

            groupXml.Attr("href", DekiContext.Current.ApiUri.At("groups", group.Id.ToString(), "users"));
            groupXml.End();

            //Permissions for the group
            RoleBE role = PermissionsBL.GetRoleById(group.RoleId);
            groupXml.Add(PermissionsBL.GetRoleXml(role, "group"));
            return groupXml;
        }
        #endregion
    }
}

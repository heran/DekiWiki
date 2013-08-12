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

#define DISABLE_REAL_NAME_SYNCHRONIZATION

using System;
using System.Collections.Generic;

using MindTouch.Deki.Data;
using MindTouch.Deki.Exceptions;
using MindTouch.Dream;
using MindTouch.Xml;

namespace MindTouch.Deki.Logic {
    public static class ExternalServiceSA { // SA = Service Adapter

        private const string USER_INFO = "users";
        private const string GROUP_INFO = "groups";
        private const string AUTHENTICATE_PATH = "authenticate";

        public static UserBE BuildUserFromAuthService(ServiceBE serviceInfo, UserBE knownUser, string usernameToBuild, bool bypassAuthentication, string authusername, string password, out List<GroupBE> externalGroups) {
            externalGroups = null;
            if(serviceInfo == null || string.IsNullOrEmpty(usernameToBuild))
                return null;

            //Dont perform external lookup for disabled users
            if(knownUser != null && !knownUser.UserActive)
                return knownUser;

            var errMsg = DekiResources.UNABLE_TO_AUTH_WITH_SERVICE(serviceInfo.Type, serviceInfo.SID, serviceInfo.Uri);

            if(knownUser != null && !string.IsNullOrEmpty(knownUser.ExternalName)) {
                usernameToBuild = knownUser.ExternalName;
            }

            UserBE ret = null;
            DreamMessage response = null;
            if(serviceInfo.Uri == null) {
                throw new ExternalServiceNotStartedFatalException(serviceInfo.Type, serviceInfo.SID);
            }
            try {
                Plug dekiExternalAuthPlug;

                //bypassAuthentication is used when you only need user details but not to necessarily authenticate
                if(bypassAuthentication) {

                    //An external auth service's GET: user/{username} does not necessarily require authentication to lookup users but it may. It's up to the service
                    //to decide if anon requests are allowed.
                    dekiExternalAuthPlug = Plug.New(serviceInfo.Uri).At(USER_INFO).At(XUri.Encode(usernameToBuild));
                } else {

                    //Credentials are always needed for GET: authenticate. The user details of the auth'd user is returned with same format as GET: user/{username}
                    dekiExternalAuthPlug = Plug.New(serviceInfo.Uri).At(AUTHENTICATE_PATH);
                }

                //Always include credentials with the request if they're supplied
                if(!string.IsNullOrEmpty(authusername)) {
                    dekiExternalAuthPlug = dekiExternalAuthPlug.WithCredentials(authusername, password ?? string.Empty);
                }

                response = dekiExternalAuthPlug.GetAsync().Wait();
            } catch(Exception x) {
                throw new ExternalServiceResponseException(errMsg, DreamMessage.InternalError(x));
            }

            if(response.IsSuccessful) {
                XDoc userXml = response.ToDocument();

                if(userXml == null || userXml.IsEmpty) {
                    throw new ExternalAuthResponseFatalException();
                }

                string nameFromAuthProvider = userXml["@name"].Contents;
                if(!nameFromAuthProvider.EqualsInvariantIgnoreCase(usernameToBuild)) {
                    throw new ExternalServiceUnexpecteUsernameFatalException(userXml["@name"].AsText, usernameToBuild);
                }
                ret = knownUser ?? new UserBE();
                ret.Email = string.IsNullOrEmpty(userXml["email"].AsText) ? (ret.Email ?? string.Empty) : userXml["email"].AsText;

                //Build the realname (exposed as 'fullname' in user xml) by saving it as '{firstname} {lastname}'
                string externalFirstName = userXml["firstname"].AsText ?? string.Empty;
                string externalLastName = userXml["lastname"].AsText ?? string.Empty;
                string separator = externalLastName.Length > 0 && externalFirstName.Length > 0 ? ", " : string.Empty;

                // NOTE (maxm): Fullname sync is disabled for now. Refer to bug 7855
#if !DISABLE_REAL_NAME_SYNCHRONIZATION
                ret.RealName = string.Format("{0}{1}{2}", externalLastName, separator, externalFirstName);
#endif

                ret.ServiceId = serviceInfo.Id;
                ret.Touched = DateTime.UtcNow;

                ret.ExternalName = string.IsNullOrEmpty(ret.ExternalName) ? nameFromAuthProvider : ret.ExternalName;
                ret.Name = string.IsNullOrEmpty(ret.Name) ? nameFromAuthProvider : ret.Name;

                //For new users, the name must be normalized and unique
                if(ret.ID == 0) {

                    string nameFromExternalName = string.Empty;

                    //Allow using a displayname from an external provider only for new accounts
                    if(!userXml["@displayname"].IsEmpty) {
                        nameFromExternalName = userXml["@displayname"].AsText;
                    } else {
                        nameFromExternalName = ret.ExternalName;
                    }

                    ret.Name = UserBL.NormalizeExternalNameToWikiUsername(nameFromExternalName);
                }

                //Build group objects out of the user's group membership list
                externalGroups = new List<GroupBE>();
                IList<GroupBE> userGroups = DbUtils.CurrentSession.Groups_GetByUser(ret.ID);

                //Preserve local groups for existing users
                if(ret.ID != 0 && userGroups != null) {
                    foreach(GroupBE g in userGroups) {
                        if(ServiceBL.IsLocalAuthService(g.ServiceId)) {
                            externalGroups.Add(g);
                        }
                    }
                }

                foreach(XDoc group in userXml["groups/group"]) {
                    GroupBE g = new GroupBE();
                    g.Name = group["@name"].AsText;
                    g.ServiceId = serviceInfo.Id;
                    if(!string.IsNullOrEmpty(g.Name))
                        externalGroups.Add(g);
                }
            } else {
                switch(response.Status) {
                case DreamStatus.Unauthorized:
                    if(bypassAuthentication) {
                        DekiContext.Current.Instance.Log.Warn(string.Format("Attempted to lookup user info on auth provider '{0}' but failed since it required credentials", serviceInfo.Id));
                    }

                    throw new ExternalServiceAuthenticationDeniedException(DekiWikiService.AUTHREALM, serviceInfo.Description);
                default:
                    throw new ExternalServiceResponseException(errMsg, response);
                }
            }

            return ret;
        }

        public static GroupBE BuildGroupFromAuthService(ServiceBE serviceInfo, GroupBE knownGroup, string groupNameToBuild, string authusername, string password) {
            if(serviceInfo == null || string.IsNullOrEmpty(groupNameToBuild))
                return null;

            GroupBE ret = null;
            DreamMessage response = null;
            if(serviceInfo.Uri == null) {
                throw new ExternalServiceNotStartedFatalException(serviceInfo.Type, serviceInfo.SID);
            }
            var errMsg = DekiResources.GROUP_DETAILS_LOOKUP_FAILED(groupNameToBuild);
            try {
                Plug dekiExternalAuthPlug = Plug.New(serviceInfo.Uri).At(GROUP_INFO).At(XUri.Encode(groupNameToBuild));

                //Always include credentials with the request if they're supplied
                if(!string.IsNullOrEmpty(authusername)) {
                    dekiExternalAuthPlug = dekiExternalAuthPlug.WithCredentials(authusername, password ?? string.Empty);
                }

                response = dekiExternalAuthPlug.GetAsync().Wait();
            } catch(Exception x) {
                throw new ExternalServiceResponseException(errMsg, DreamMessage.InternalError(x));
            }

            if(response.IsSuccessful) {
                XDoc groupXml = response.ToDocument();
                if(groupXml.HasName("group") && groupXml["@name"].Contents.EqualsInvariant(groupNameToBuild)) {
                    if(knownGroup == null)
                        ret = new GroupBE();
                    else
                        ret = knownGroup;

                    ret.Name = string.IsNullOrEmpty(ret.Name) ? groupNameToBuild : ret.Name;
                    ret.ServiceId = serviceInfo.Id;
                }

                //TODO (MaxM): Consider looking up existing wiki users and associating them here.
            } else {
                switch(response.Status) {
                case DreamStatus.Unauthorized:
                    throw new ExternalServiceAuthenticationDeniedException(DekiWikiService.AUTHREALM, serviceInfo.Description);
                case DreamStatus.InternalError:
                case DreamStatus.Forbidden:
                default:
                    throw new ExternalServiceResponseException(errMsg, response);
                }
            }

            return ret;
        }
    }
}

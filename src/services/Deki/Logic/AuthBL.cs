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
using System.Text;
using System.Text.RegularExpressions;
using System.Security.Cryptography;

using MindTouch.Deki.Data;
using MindTouch.Deki.Exceptions;
using MindTouch.Dream;
using MindTouch.Extensions.Time;
using MindTouch.Web;
using MindTouch.Xml;

namespace MindTouch.Deki.Logic {
    public static class AuthBL {

        //--- Types ---
        private class Token {

            //--- Fields ---
            public uint UserId;
            public string Username;
            public DateTime Timestamp;
            public string Hash;
            public bool IsImpersonationToken;
        }


        //--- Constants ---
        private const string authTokenPattern = @"^(?<id>([\d])+)_(?<ts>([\d]){18})_(?<hash>.+)$";
        private static readonly Regex authTokenRegex = new Regex(authTokenPattern, RegexOptions.Singleline | RegexOptions.Compiled | RegexOptions.CultureInvariant);
        private const string impersonationPattern = @"^imp_(?<epoch>([\d])+)_(?<hash>[^_]{32})_(?<user>.+)$";
        private static readonly Regex impersonationRegex = new Regex(impersonationPattern, RegexOptions.Singleline | RegexOptions.Compiled | RegexOptions.CultureInvariant);

        //--- Class Methods ---
        public static UserBE Authenticate(DreamContext context, DreamMessage request, uint serviceId, bool autoCreateExternalUser, bool allowAnon, out bool altPassword) {
            UserBE user = null;
            altPassword = false;

            // Case 1: username/fullname, password, provider (login window)
            //      1. Validate & retrieve fullname using credentials
            //          Failed -> return null
            //      2. Populate user object
            //          A. Populates user info
            //          B. Populates group info in user object
            //      3. Does fullname exist? 
            //          Yes -> Update user (email, fullname, ...)
            //          No  -> Create user
            //      4. Update Group information
            //      5. return user object
            //
            // Case 2: fullname, password (http, api, ...)
            //      1. Lookup full name, exist?
            //          Yes -> return user
            //          No -> return null
            //
            // Case 3: auth-token (header auth)
            //      0. Valid auth token?
            //          No -> return null
            //      1. Lookup user by name
            //          Found -> return user
            //          Else  -> return null

            string userName = null;
            string password = null;
            UserBE userFromToken = null;
            ServiceBE authService = null;

            // Extract authtoken and impersonation authtoken from request. 
            // AllowAnon is false when in GET/POST: users/authenticate or when ?authenticate=true. 
            // Standard user authtokens are ignored when AllowAnon=false but impersonation tokens are accepted.
            bool impersonationOnly = !allowAnon;
            userFromToken = UserFromAuthTokenInRequest(context, impersonationOnly);
            
            if(userFromToken == null) {
                HttpUtil.GetAuthentication(context.Uri.ToUri(), request.Headers, out userName, out password);
            }

            // check if we need to retrieve authentication service information
            if(serviceId > 0) {
                authService = ServiceBL.GetServiceById(serviceId);
                if(authService == null) {
                    throw new AuthServiceIdInvalidArgumentException(serviceId);
                }
                if(authService.Type != ServiceType.AUTH) {
                    throw new AuthNotAnAuthServiceInvalidArgumentException(serviceId);
                }
            }

            // check if a username was provided
            if(!string.IsNullOrEmpty(userName)) {

                //Case 2: Given username + password
                if(authService == null) {

                    //Assuming local user or existing external account
                    user = DbUtils.CurrentSession.Users_GetByName(userName);
                    if(user != null) {
                        serviceId = user.ServiceId;
                        authService = ServiceBL.GetServiceById(serviceId);
                    } else {
                        LoginAccessDenied(context, request, userName, null, password);
                    }
                }
                if(authService == null) {
                    throw new AuthServiceIdInvalidArgumentException(serviceId);
                }
                if(authService.Type != ServiceType.AUTH) {
                    throw new AuthNotAnAuthServiceInvalidArgumentException(serviceId);
                }
                if(user == null) {

                    //Performing auth on local account
                    if(ServiceBL.IsLocalAuthService(authService)) {
                        user = DbUtils.CurrentSession.Users_GetByName(userName);
                    } else {

                        //Performing external auth. Lookup by external user name
                        user = DbUtils.CurrentSession.Users_GetByExternalName(userName, authService.Id);
                    }
                    if(user != null && user.ServiceId != authService.Id) {
                        ServiceBE currentUsersAuthService = ServiceBL.GetServiceById(user.ServiceId);
                        if(currentUsersAuthService != null) {
                            throw new AuthLoginExternalUserConflictException(currentUsersAuthService.Description);
                        }
                        throw new LoginExternalUserUnknownConflictException();
                    }
                }

                //Local account in the db.
                if(user != null && ServiceBL.IsLocalAuthService(authService)) {

                    //Validate password for local account or validate the apikey
                    if(!IsValidAuthenticationForLocalUser(user, password, out altPassword)) {

                        // try impersonation using the ApiKey
                        if(string.IsNullOrEmpty(password) && PermissionsBL.ValidateRequestApiKey()) {
                            DekiContext.Current.Instance.Log.InfoFormat("user '{0}' authenticated via apikey impersonation", userName);
                        } else {
                            LoginAccessDenied(context, request, userName, user.ID, password);
                        }
                    }
                }

                // User was not found in the db and not being asked to create it.
                if(user == null && !autoCreateExternalUser) {
                    LoginAccessDenied(context, request, userName, null, password);
                }

                // Creating local account if apikey checks out and our authservice is local
                if(user == null && string.IsNullOrEmpty(password) && PermissionsBL.ValidateRequestApiKey() && ServiceBL.IsLocalAuthService(authService)) {
                    XDoc newUserDoc = new XDoc("user")
                        .Elem("username", userName);
                    DreamMessage newUserResponse = DekiContext.Current.ApiPlug.At("users")
                        .With("apikey", DreamContext.Current.GetParam("apikey", string.Empty))
                        .Post(newUserDoc);
                    user = UserBL.GetUserById(newUserResponse.ToDocument()["/user/@id"].AsUInt ?? 0);
                    if(user != null && !string.IsNullOrEmpty(password)) {
                        user = UserBL.SetPassword(user, password, false);
                    }
                }

                // Got an external account
                // Passing in the user object from db if it was found.
                List<GroupBE> externalGroups = null;
                if(!ServiceBL.IsLocalAuthService(authService)) {
                    bool bypassAuthentication = false;
                    string externalName;
                    if(user == null || string.IsNullOrEmpty(user.ExternalName))
                        externalName = userName;
                    else
                        externalName = user.ExternalName;

                    // If apikey is valid, try to bypass auth with the external provider
                    // and only lookup user/group details.
                    if(string.IsNullOrEmpty(password) && PermissionsBL.ValidateRequestApiKey()) {
                        DekiContext.Current.Instance.Log.InfoFormat("user '{0}' authenticating being bypassed via apikey impersonation", userName);
                        bypassAuthentication = true;
                    }

                    user = ExternalServiceSA.BuildUserFromAuthService(authService, user, userName, bypassAuthentication, externalName, password, out externalGroups);
                }

                // User was not found or did not authenticate with external provider
                if(user == null) {
                    LoginAccessDenied(context, request, userName, null, password);
                } else {

                    //New user creation from external provider
                    if(user.ID == 0) {
                        if(!autoCreateExternalUser) {
                            LoginAccessDenied(context, request, userName, null, password);
                        }
                    } else { 
                        
                        //user exists
                        // TODO (steveb): ???
                    }
                    if(user.UserActive) {
                        user = UserBL.CreateOrUpdateUser(user);
                        if(externalGroups != null) {
                            UserBL.UpdateUsersGroups(user, externalGroups.ToArray());
                        }
                    }
                }
            } else if(userFromToken != null) {

                // valid token exists that resolved to a user
                user = userFromToken;
            } else if(allowAnon) {

                // Anonymous user
                user = DbUtils.CurrentSession.Users_GetByName(DekiWikiService.ANON_USERNAME);
            }
            if(user == null) {

                //No credentials. Or token not provided or is invalid.
                LoginAccessDenied(context, request, null, null, password);
            } else if(!user.UserActive && !PermissionsBL.ValidateRequestApiKey()) {

                //If a valid api key is provided, override the disabled account flag
                throw new AuthUserDisabledForbiddenException(user.Name);
            }
            return user;
        }

        public static string EncryptPassword(UserBE user, string pwd) {
            string md5Pwd = GetMD5(pwd);
            return GetMD5(user.ID + "-" + md5Pwd);
        }

        private static string GetMD5(string pass) {
            MD5 MD5 = MD5CryptoServiceProvider.Create();
            StringBuilder sb = new StringBuilder();
            foreach(byte ch in MD5.ComputeHash(Encoding.Default.GetBytes(pass))) {
                sb.AppendFormat("{0:x2}", ch);
            }
            return sb.ToString();
        }

        /// <summary>
        /// Get a user out of an authtoken from a request if it's valid.
        /// </summary>
        /// <returns></returns>
        private static UserBE UserFromAuthTokenInRequest(DreamContext context, bool impersonationOnly) {
            DreamMessage request = context.Request;
            string authToken = context.Uri.GetParam(DekiWikiService.AUTHTOKEN_URIPARAM, null);
            UserBE user = null;

            // Check if auth token is in a cookie
            if((authToken == null) && request.HasCookies) {
                DreamCookie authCookie = DreamCookie.GetCookie(request.Cookies, DekiWikiService.AUTHTOKEN_COOKIENAME);
                if((authCookie != null) && (!authCookie.Expired)) {
                    authToken = authCookie.Value;
                }
            }

            // Check if auth token is in a header or passed in as query parameter
            authToken = authToken ?? request.Headers[DekiWikiService.AUTHTOKEN_HEADERNAME];

            // Extract user name from auth token if it's valid
            if(authToken != null) {
                user = ValidateAuthToken(authToken, impersonationOnly);

                // check whether licensestate prevents user from being authenticated
                var license = DekiContext.Current.LicenseManager;
                LicenseStateType licensestate = license.LicenseState;
                if((licensestate == LicenseStateType.EXPIRED || licensestate == LicenseStateType.INVALID || licensestate == LicenseStateType.INACTIVE) &&
                    !PermissionsBL.IsUserAllowed(user, Permissions.ADMIN)
                ) {
                    if(DekiContext.Current.Instance.Log.IsWarnEnabled) {
                        switch(licensestate) {
                        case LicenseStateType.EXPIRED:
                            DekiContext.Current.Instance.Log.WarnFormat("UserFromAuthTokenInRequest: Expired license {0}, reverting non-admin user to anonymous", license.LicenseExpiration);
                            break;
                        case LicenseStateType.INVALID:
                            DekiContext.Current.Instance.Log.WarnFormat("UserFromAuthTokenInRequest: Invalid license, reverting non-admin user to anonymous");
                            break;
                        case LicenseStateType.INACTIVE:
                            DekiContext.Current.Instance.Log.WarnFormat("UserFromAuthTokenInRequest: Inactive license, reverting non-admin user to anonymous");
                            break;
                        }
                    }
                    user = null;
                } else {
                    DekiContext.Current.AuthToken = authToken;
                }
            }
            if(PermissionsBL.ValidateRequestApiKey()) {
                uint userIdOverride = 0;
                if(uint.TryParse(context.GetParam(DekiWikiService.IMPERSONATE_USER_QUERYNAME, null), out userIdOverride)) {
                    UserBE userOverride = UserBL.GetUserById(userIdOverride);
                    if(userOverride != null) {
                        user = userOverride;
                        DekiContext.Current.Instance.Log.InfoFormat("APIKEY provided. Impersonating user id '{0}': {1}", user.ID, user.Name);
                    }
                }
            }
            return user;
        }

        public static string CreateAuthTokenForUser(UserBE user) {
            return CreateAuthTokenForUser(user, DateTime.Now);
        }

        private static string CreateAuthTokenForUser(UserBE user, DateTime timestamp) {
            string ret = string.Empty;
            string tokenContent = string.Format("{0}_{1}", user.ID.ToString(), timestamp.ToUniversalTime().Ticks);

            //Include the users current password as part of validation to invalidate token upon pw change.
            string contentToValidate = string.Format("{0}.{1}.{2}", tokenContent, user.Password ?? string.Empty, DekiContext.Current.Instance.AuthTokenSalt);
            System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create();

            string hash = new Guid(md5.ComputeHash(Encoding.Default.GetBytes(contentToValidate))).ToString("N");
            ret = tokenContent + "_" + hash;
            return ret;
        }

        private static UserBE ValidateAuthToken(string authToken, bool impersonationOnly) {
            var context = DekiContext.Current;
            var token = ParseToken(authToken);
            var instance = context.Instance;
            if(token == null) {
                return null;
            }
            UserBE user;
            if(token.IsImpersonationToken) {
                if(ValidateImpersonationToken(context, token)) {
                    user = string.IsNullOrEmpty(token.Username) ? UserBL.GetUserById(token.UserId) : UserBL.GetUserByName(token.Username);
                    instance.Log.InfoFormat("APIKEY impersonation token provided. Impersonating user '{0}' ({1})", user.Name, user.ID);
                    return user;
                }
                return null;
            }

            // only allowing impersonation tokens?
            if(impersonationOnly) {
                return null;
            }

            // check timestamp
            if(token.Timestamp < DateTime.UtcNow.Subtract(instance.AuthCookieExpirationTime) && instance.AuthCookieExpirationTime.TotalSeconds > 0) {
                return null;
            }

            // retrieve associated user object
            user = UserBL.GetUserById(token.UserId);
            if(user == null) {
                return null;
            }

            // TODO Max: Consider logging this as an intrusion attempt.
            return authToken == CreateAuthTokenForUser(user, token.Timestamp) ? user : null;
        }

        private static bool ValidateImpersonationToken(DekiContext context, Token token) {
            if(token.Timestamp < DateTime.UtcNow.Subtract(1.Minutes())) {
                return false;
            }
            var userToken = string.IsNullOrEmpty(token.Username) ? token.UserId.ToString() : token.Username;
            var valid = false;
            if(!string.IsNullOrEmpty(context.Instance.ApiKey)) {
                valid = token.Hash.Equals(StringUtil.ComputeHashString(userToken + ":" + token.Timestamp.ToEpoch() + ":" + context.Instance.ApiKey, Encoding.UTF8));
            }
            if(!valid) {
                valid = token.Hash.Equals(StringUtil.ComputeHashString(userToken + ":" + token.Timestamp.ToEpoch() + ":" + context.Deki.MasterApiKey, Encoding.UTF8));
            }
            return valid;
        }

        public static bool IsValidAuthenticationForLocalUser(UserBE user, string password) {
            bool altPassword;
            return IsValidAuthenticationForLocalUser(user, password, out altPassword);
        }

        public static bool IsValidAuthenticationForLocalUser(UserBE user, string password, out bool altPassword) {
            bool isValid = false;
            altPassword = false;
            string encrypted = EncryptPassword(user, password);
            if(string.CompareOrdinal(encrypted, user.Password) == 0) {

                //On login if a user has a temp password but logs in with original password, clear out the temp password.
                if(!string.IsNullOrEmpty(user.NewPassword)) {
                    user.NewPassword = string.Empty;
                    DbUtils.CurrentSession.Users_Update(user);
                }
                isValid = true;
            } else if(!string.IsNullOrEmpty(user.NewPassword) && string.CompareOrdinal(encrypted, user.NewPassword) == 0) {
                isValid = true;
                altPassword = true;
            }
            return isValid;
        }

        private static void LoginAccessDenied(DreamContext context, DreamMessage request, string username, uint? userid, string password) {
            string apiKey = context.GetParam("apikey", null);
            if(!string.IsNullOrEmpty(username) || !string.IsNullOrEmpty(password) || !string.IsNullOrEmpty(apiKey)) {
                DekiContext.Current.Instance.Log.WarnMethodCall("Authenticate: user password not correct or invalid auth token",
                "username:" + username,
                "userid:" + userid,
                "usingpassword?:" + !string.IsNullOrEmpty(password),
                "usingapikey?:" + !string.IsNullOrEmpty(apiKey),
                "origin:" + request.Headers[DreamHeaders.DREAM_CLIENTIP]);
            }
            throw new AuthFailedDeniedException(DekiWikiService.AUTHREALM);
        }

        private static Token ParseToken(string authToken) {
            if(string.IsNullOrEmpty(authToken)) {
                return null;
            }
            Regex regex;
            var token = new Token();
            if(authToken.StartsWith("imp_")) {
                regex = impersonationRegex;
                token.IsImpersonationToken = true;
            } else {
                regex = authTokenRegex;
            }
            var m = regex.Match(authToken);
            if(!m.Success) {
                return null;
            }
            if(token.IsImpersonationToken) {
                var user = m.Groups["user"].Value;
                if(string.IsNullOrEmpty(user)) {
                    return null;
                }
                if(user.StartsWith("=")) {
                    token.Username = user.Substring(1);
                } else if(!uint.TryParse(user, out token.UserId)) {
                    return null;
                }
                uint epoch;
                if(!uint.TryParse(m.Groups["epoch"].Value, out epoch)) {
                    return null;
                }
                token.Timestamp = DateTimeUtil.FromEpoch(epoch);
            } else {
                if(!uint.TryParse(m.Groups["id"].Value, out token.UserId)) {
                    return null;
                }
                long tsValue;
                if(!long.TryParse(m.Groups["ts"].Value, out tsValue)) {
                    return null;
                }
                token.Timestamp = new DateTime(tsValue, DateTimeKind.Utc);
            }
            token.Hash = m.Groups["hash"].Value;
            return token;
        }
    }
}

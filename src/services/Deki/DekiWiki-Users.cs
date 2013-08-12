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
using MindTouch.Tasking;
using MindTouch.Web;
using MindTouch.Xml;

namespace MindTouch.Deki {
    using Yield = IEnumerator<IYield>;
    using MindTouch.Deki.Logic;

    public partial class DekiWikiService {

        //--- Features ---
        [DreamFeature("POST:users/authenticate", "Authenticate a user given http header Credentials or an auth token. When using external authentication, this will automatically create an account and synchronize groups. Response status 200 implies valid credentials and contains a new auth token.")]
        [DreamFeature("GET:users/authenticate", "Authenticate a user given http header Credentials or an auth token. Response status 200 implies valid credentials and contains a new auth token.")]
        [DreamFeatureParam("redirect", "uri?", "Redirect to the given URI upon authentication")]
        [DreamFeatureParam("authprovider", "int?", "Identifier for the external service to use for authentication.")]
        [DreamFeatureStatus(DreamStatus.Ok, "The request completed successfully")]
        [DreamFeatureStatus(DreamStatus.BadRequest, "Invalid input parameter or request body")]
        [DreamFeatureStatus(DreamStatus.Forbidden, "Login access is required")]
        [DreamFeatureStatus(DreamStatus.Conflict, "Username conflicts with an existing username")]
        [DreamFeatureStatus(DreamStatus.Unauthorized, "Authentication failed")]
        public Yield PostUserAuth(DreamContext context, DreamMessage request, Result<DreamMessage> response) {

            uint serviceId = context.GetParam<uint>("authprovider", 0);
            bool altPassword;

            //This will internally fail with a 501 response if credentials are invalid.
            //Anonymous accounts (no credentials/authtoken) are not allowed -> 401
            UserBE u = SetContextAndAuthenticate(request, serviceId, context.Verb == Verb.POST, false, true, out altPassword);
            PermissionsBL.CheckUserAllowed(u, Permissions.LOGIN);


            string token = AuthBL.CreateAuthTokenForUser(u);

            try {
                PageBL.CreateUserHomePage(DekiContext.Current.User);
            } catch { }
            XUri redirectUri = XUri.TryParse(context.GetParam("redirect", null));
            DreamMessage ret = BuildSetAuthTokenResponse(token, redirectUri);
            DekiContext.Current.Instance.EventSink.UserLogin(DekiContext.Current.Now, DekiContext.Current.User);

            //TODO Max: Set a response header or status to indicate that an alt password was used.
            response.Return(ret);
            yield break;
        }

        [DreamFeature("GET:users", "Retrieve list of users.")]
        [DreamFeatureParam("usernamefilter", "string?", "Search for users name starting with supplied text")]
        [DreamFeatureParam("fullnamefilter", "string?", "Search for users full name starting with supplied text")]
        [DreamFeatureParam("usernameemailfilter", "string?", "Search for users by name and email or part of a name and email")]
        [DreamFeatureParam("authprovider", "int?", "Return users belonging to given authentication service id")]
        [DreamFeatureParam("rolefilter", "string?", "Search for users by a role name")]
        [DreamFeatureParam("activatedfilter", "bool?", "Search for users by their active status")]
        [DreamFeatureParam("seatfilter", "string?", "Search for users with or without seats (one of \"seated\", \"unseated\", or \"recommended\"; default: none)")]
        [DreamFeatureParam("limit", "string?", "Maximum number of items to retrieve. Must be a positive number or 'all' to retrieve all items. (default: 100)")]
        [DreamFeatureParam("offset", "int?", "Number of items to skip. Must be a positive number or 0 to not skip any. (default: 0)")]
        [DreamFeatureParam("sortby", "{id, username, nick, email, fullname, date.lastlogin, status, role, service, date.created}?", "Sort field. Prefix value with '-' to sort descending. default: No sorting")]
        [DreamFeatureParam("verbose", "bool?", "Return detailed user information. (default: true)")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        [DreamFeatureStatus(DreamStatus.Ok, "The request completed successfully")]
        [DreamFeatureStatus(DreamStatus.Forbidden, "Read access is required")]
        public Yield GetUsers(DreamContext context, DreamMessage request, Result<DreamMessage> response) {

            // TODO (steveb): add 'emailfilter' and use it to obsolete 'usernameemailfilter'; 'usernamefilter', 'fullnamefilter', and 'emailfilter' 
            //                should be OR'ed together when they are present.

            PermissionsBL.CheckUserAllowed(DekiContext.Current.User, Permissions.READ);
            uint totalCount;
            uint queryCount;
            var users = UserBL.GetUsersByQuery(context, null, out totalCount, out queryCount);
            XDoc result = new XDoc("users");
            result.Attr("count", users.Count());
            result.Attr("querycount", queryCount);
            result.Attr("totalcount", totalCount);
            result.Attr("href", DekiContext.Current.ApiUri.At("users"));
            bool verbose = context.GetParam<bool>("verbose", true);
            foreach(UserBE u in users) {
                if(verbose) {
                    result.Add(UserBL.GetUserXmlVerbose(u, null, Utils.ShowPrivateUserInfo(u), true, true));
                } else {
                    result.Add(UserBL.GetUserXml(u, null, Utils.ShowPrivateUserInfo(u)));
                }
            }
            response.Return(DreamMessage.Ok(result));
            yield break;
        }

        [DreamFeature("GET:users/{userid}", "Retrieve information about a user.")]
        [DreamFeatureParam("{userid}", "string", "Either an integer user ID, \"current\", or \"=\" followed by a double uri-encoded user name")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        [DreamFeatureParam("exclude", "string?", "Elements to exclude from response document (choice of \"groups\", \"properties\"; default: exclude nothing)")]
        [DreamFeatureStatus(DreamStatus.Ok, "The request completed successfully")]
        [DreamFeatureStatus(DreamStatus.BadRequest, "Invalid input parameter or request body")]
        [DreamFeatureStatus(DreamStatus.Forbidden, "Read access is required")]
        [DreamFeatureStatus(DreamStatus.NotFound, "Requested user could not be found")]
        public Yield GetUser(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            UserBE u = GetUserFromUrlMustExist();

            //Perform permission check if not looking yourself up
            if(u.ID != DekiContext.Current.User.ID) {
                PermissionsBL.CheckUserAllowed(DekiContext.Current.User, Permissions.READ);
            }
            var showGroups = !context.GetParam("exclude", "").Contains("groups");
            var showProperties = !context.GetParam("exclude", "").Contains("properties");
            response.Return(DreamMessage.Ok(UserBL.GetUserXmlVerbose(u, null, Utils.ShowPrivateUserInfo(u), showGroups, showProperties)));
            yield break;
        }

        [DreamFeature("POST:users", "Add or modify a user")]
        [DreamFeatureParam("accountpassword", "string?", "Account password to set (default: do not set/change password)")]
        [DreamFeatureParam("authusername", "string?", "Username to use for verification with external authentication service")]
        [DreamFeatureParam("authpassword", "string?", "Password to use for verification with external authentication service")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        [DreamFeatureStatus(DreamStatus.Ok, "The request completed successfully")]
        [DreamFeatureStatus(DreamStatus.BadRequest, "Invalid input parameter or request body")]
        [DreamFeatureStatus(DreamStatus.Forbidden, "Administrator access, apikey, or account owner is required")]
        [DreamFeatureStatus(DreamStatus.NotFound, "Requested user could not be found")]
        [DreamFeatureStatus(DreamStatus.Conflict, "Username conflicts with an existing username")]
        public Yield PostUser(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            if(!PermissionsBL.IsUserAllowed(DekiContext.Current.User, Permissions.ADMIN)) {
                throw new DreamForbiddenException("Must provide an apikey or admin authtoken to create a user");
            }

            // authorization is performed later.
            string accountPassword = context.GetParam("accountpassword", null);

            //standard user creation/editing
            UserBE user = UserBL.PostUserFromXml(request.ToDocument(), null, accountPassword, context.GetParam("authusername", null), context.GetParam("authpassword", null));
            response.Return(DreamMessage.Ok(UserBL.GetUserXmlVerbose(user, null, Utils.ShowPrivateUserInfo(user), true, true)));
            yield break;
        }

        [DreamFeature("PUT:users/{userid}", "Modify an existing user")]
        [DreamFeatureParam("{userid}", "string", "either an integer user ID, \"current\", or \"=\" followed by a double uri-encoded user name")]
        [DreamFeatureParam("authusername", "string?", "Username to use for verification with external authentication service")]
        [DreamFeatureParam("authpassword", "string?", "Password to use for verification with external authentication service")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        [DreamFeatureStatus(DreamStatus.Ok, "The request completed successfully")]
        [DreamFeatureStatus(DreamStatus.BadRequest, "Invalid input parameter or request body")]
        [DreamFeatureStatus(DreamStatus.Forbidden, "Administrator access or account owner is required")]
        [DreamFeatureStatus(DreamStatus.NotFound, "Requested user could not be found")]
        [DreamFeatureStatus(DreamStatus.Conflict, "Username conflicts with an existing username")]
        public Yield PutUser(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            UserBE user = GetUserFromUrl();

            //Authorization is performed later.
            if(user == null) {
                throw new NoSuchUserUsePostNotFoundException();
            }
            string accountPassword = context.GetParam("accountpassword", null);
            user = UserBL.PostUserFromXml(request.ToDocument(), user, accountPassword, context.GetParam("authusername", null), context.GetParam("authpassword", null));
            response.Return(DreamMessage.Ok(UserBL.GetUserXmlVerbose(user, null, Utils.ShowPrivateUserInfo(user), true, true)));
            yield break;
        }

        [DreamFeature("POST:users/{userid}/allowed", "Check one or more resources if given operation is allowed.")]
        [DreamFeatureParam("{userid}", "string", "either an integer user ID, \"current\", or \"=\" followed by a double uri-encoded user name")]
        [DreamFeatureParam("mask", "long?", "Permission bit mask required for the pages")]
        [DreamFeatureParam("operations", "string?", "Comma separated list of operations to verify")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        [DreamFeatureParam("verbose", "bool?", "Return verbose information on permitted pages (default: true")]
        [DreamFeatureParam("invert", "bool?", "Return filtered instead of allowed pages. Sets verbose to false (default: false")]
        [DreamFeatureStatus(DreamStatus.Ok, "The request completed successfully")]
        [DreamFeatureStatus(DreamStatus.BadRequest, "Invalid input parameter or request body")]
        [DreamFeatureStatus(DreamStatus.NotFound, "Requested user could not be found")]
        public Yield PostUsersAllowed(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            var permissionMask = context.GetParam<ulong>("mask", 0);
            var operationList = context.GetParam("operations", "");
            var user = GetUserFromUrlMustExist();
            var verbose = context.GetParam("verbose", true);
            var invert = context.GetParam("invert", false);

            // Use comma separated permission list or permissionmask from request.
            var permissions = Permissions.NONE;
            if(permissionMask != 0) {
                permissions = (Permissions)permissionMask;
            }

            // Convert operation list to mask combined with provided mask
            if(!string.IsNullOrEmpty(operationList)) {
                try {
                    permissions |= (Permissions)PermissionsBL.MaskFromPermissionList(PermissionsBL.PermissionListFromString(operationList));
                } catch {
                    throw new UserOperationListInvalidArgumentException();
                }
            }
            IEnumerable<ulong> pageIds;
            var textOutput = false;
            if(request.HasDocument) {
                if(!request.ToDocument().HasName("pages")) {
                    throw new UserExpectedRootNodePagesInvalidDocumentException();
                }
                pageIds = from pageIdXml in request.ToDocument()["page/@id"]
                          let pageId = pageIdXml.AsULong
                          where pageId.HasValue
                          select pageId.Value;
            } else if(verbose) {
                throw new UserPageFilterVerboseNotAllowedException();
            } else if(!request.ContentType.Match(MimeType.TEXT)) {
                throw new UserPageFilterInvalidInputException();
            } else {
                textOutput = true;
                pageIds = request.ToText().CommaDelimitedToULong();
            }
            IEnumerable<ulong> filtered;
            var allowedPages = PermissionsBL.FilterDisallowed(user, pageIds, false, out filtered, permissions);
            if(textOutput) {
                var output = invert
                    ? filtered.ToCommaDelimitedString()
                    : allowedPages.ToCommaDelimitedString();
                response.Return(DreamMessage.Ok(MimeType.TEXT, output ?? string.Empty));
            } else {
                var responseDoc = new XDoc("pages");
                if(invert) {
                    foreach(var pageId in filtered) {
                        responseDoc.Start("page").Attr("id", pageId).End();
                    }
                } else if(allowedPages.Any()) {
                    if(verbose) {
                        foreach(var page in PageBL.GetPagesByIdsPreserveOrder(allowedPages)) {
                            responseDoc.Add(PageBL.GetPageXml(page, null));
                        }
                    } else {
                        foreach(var pageId in allowedPages) {
                            responseDoc.Start("page").Attr("id", pageId).End();
                        }
                    }
                }
                response.Return(DreamMessage.Ok(responseDoc));
            }
            yield break;
        }

        [DreamFeature("PUT:users/{userid}/seat", "Give a user a license seat.")]
        [DreamFeatureParam("{userid}", "string", "either an integer user ID, \"current\", or \"=\" followed by a double uri-encoded user name")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        [DreamFeatureStatus(DreamStatus.Ok, "The request completed successfully")]
        [DreamFeatureStatus(DreamStatus.Conflict, "User cannot be given a seat or seats depleted")]
        [DreamFeatureStatus(DreamStatus.Forbidden, "Administrator access required")]
        [DreamFeatureStatus(DreamStatus.NotFound, "Requested user could not be found")]
        internal Yield PutUserSeat(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            UserBE targetUser = GetUserFromUrlMustExist();
            DekiContext.Current.LicenseManager.SetUserSeat(targetUser);
            response.Return(DreamMessage.Ok(UserBL.GetUserXmlVerbose(UserBL.GetUserById(targetUser.ID), string.Empty, true, true, true)));
            yield break;
        }

        [DreamFeature("DELETE:users/{userid}/seat", "Remove a license seat from a user.")]
        [DreamFeatureParam("{userid}", "string", "either an integer user ID, \"current\", or \"=\" followed by a double uri-encoded user name")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        [DreamFeatureStatus(DreamStatus.Ok, "The request completed successfully")]
        [DreamFeatureStatus(DreamStatus.BadRequest, "Invalid input parameter or request body")]
        [DreamFeatureStatus(DreamStatus.Forbidden, "Administrator access required")]
        [DreamFeatureStatus(DreamStatus.NotFound, "Requested user could not be found")]
        internal Yield DeleteUserSeat(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            UserBE targetUser = GetUserFromUrlMustExist();
            DekiContext.Current.LicenseManager.RemoveSeatFromUser(targetUser);
            response.Return(DreamMessage.Ok(UserBL.GetUserXmlVerbose(UserBL.GetUserById(targetUser.ID), string.Empty, true, true, true)));
            yield break;
        }

        [DreamFeature("PUT:users/{userid}/password", "Set password for a given user.")]
        [DreamFeatureParam("{userid}", "string", "either an integer user ID, \"current\", or \"=\" followed by a double uri-encoded user name")]
        [DreamFeatureParam("currentpassword", "string?", "Current password needed when changing your own password (without admin rights)")]
        [DreamFeatureParam("altpassword", "bool?", "If true, the given password sets a secondary password that can be used for login. The main password is not overwritten. (default: false)")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        [DreamFeatureStatus(DreamStatus.Ok, "The request completed successfully")]
        [DreamFeatureStatus(DreamStatus.BadRequest, "Invalid input parameter or request body")]
        [DreamFeatureStatus(DreamStatus.Forbidden, "Administrator access or account owner is required")]
        [DreamFeatureStatus(DreamStatus.NotFound, "Requested user could not be found")]
        public Yield PutPasswordChange(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            UserBE targetUser = GetUserFromUrlMustExist();
            string password = request.AsText();
            if(string.IsNullOrEmpty(password)) {
                throw new UserNewPasswordNotProvidedInvalidArgumentException();
            }
            if(password.Length < 4) {
                throw new UserNewPasswordTooShortInvalidArgumentException();
            }

            // Ensure that the password is being set only on local accounts
            ServiceBE s = ServiceBL.GetServiceById(targetUser.ServiceId);
            if(s != null && !ServiceBL.IsLocalAuthService(s)) {
                throw new UserCanOnlyChangeLocalUserPasswordInvalidOperationException();
            }
            if(UserBL.IsAnonymous(targetUser)) {
                throw new UserCannotChangeAnonPasswordInvalidOperationException();
            }

            // Admins can always change anyones password.
            if(PermissionsBL.IsUserAllowed(DekiContext.Current.User, Permissions.ADMIN)) {

                //For admins a currentpassword is option but if given then it should be validated
                string currentPwd = context.GetParam("currentpassword", string.Empty);
                if(!string.IsNullOrEmpty(currentPwd)) {
                    if(!AuthBL.IsValidAuthenticationForLocalUser(targetUser, currentPwd)) {
                        throw new UserCurrentPasswordIncorrectForbiddenException();
                    }
                }
            } else if(DekiContext.Current.User.ID == targetUser.ID) {
                if(context.GetParam("altpassword", false)) {
                    throw new UserCannotChangeOwnAltPasswordInvalidOperationException();
                }

                // User changing their own password requires knowledge of their current password
                string currentPwd = context.GetParam("currentpassword");
                if(!AuthBL.IsValidAuthenticationForLocalUser(DekiContext.Current.User, currentPwd)) {
                    throw new UserCurrentPasswordIncorrectForbiddenException();
                }
            } else {
                throw new UserMustBeTargetOrAdminForbiddenException();
            }
            bool altPassword = context.GetParam<bool>("altpassword", false);
            targetUser = UserBL.SetPassword(targetUser, password, altPassword);
            if(DekiContext.Current.User.ID == targetUser.ID) {
                response.Return(BuildSetAuthTokenResponse(AuthBL.CreateAuthTokenForUser(targetUser), null));
            } else {
                response.Return(DreamMessage.Ok());
            }
            yield break;
        }

        [DreamFeature("GET:users/{userid}/favorites", "Retrieves a list of favorite pages for a user.")]
        [DreamFeatureParam("{userid}", "string", "either an integer user ID, \"current\", or \"=\" followed by a double uri-encoded user name")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        [DreamFeatureStatus(DreamStatus.Ok, "The request completed successfully")]
        [DreamFeatureStatus(DreamStatus.Forbidden, "BROWSE access is required")]
        [DreamFeatureStatus(DreamStatus.NotFound, "Requested user could not be found")]
        public Yield GetFavoritePagesForUser(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            XDoc result = PageBL.GetFavoritePagesForUser(GetUserFromUrlMustExist());
            response.Return(DreamMessage.Ok(result));
            yield break;
        }

        [DreamFeature("GET:users/{userid}/metrics", "Retrieve usage metrics for a user")]
        [DreamFeatureParam("{userid}", "string", "either an integer user ID, \"current\", or \"=\" followed by a double uri-encoded user name")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        [DreamFeatureStatus(DreamStatus.Ok, "The request completed successfully")]
        [DreamFeatureStatus(DreamStatus.NotFound, "Requested user could not be found")]
        public Yield GetMetricsForUser(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            UserBE targetUser = GetUserFromUrlMustExist();
            XDoc metricsDoc = UserBL.GetUserMetricsXml(targetUser);
            response.Return(DreamMessage.Ok(metricsDoc));
            yield break;
        }

        private UserBE GetUserFromUrl() {
            UserBE u = null;
            string userid = DreamContext.Current.GetParam("userid");

            //Double decoding of name is done to work around a mod_proxy issue that strips out slashes
            userid = XUri.Decode(userid);
            if(StringUtil.EqualsInvariantIgnoreCase(userid.Trim(), "current")) {
                u = DekiContext.Current.User;
            } else if(userid.StartsWith("=")) {
                string name = userid.Substring(1);
                u = DbUtils.CurrentSession.Users_GetByName(name);
            } else {
                uint userIdInt;
                if(!uint.TryParse(userid, out userIdInt)) {
                    throw new UserIdInvalidArgumentException();
                }
                u = UserBL.GetUserById(userIdInt);
            }
            return u;
        }

        private UserBE GetUserFromUrlMustExist() {
            UserBE u = GetUserFromUrl();
            if(u == null) {
                throw new UserNotFoundException();
            }
            return u;
        }

        private DreamMessage BuildSetAuthTokenResponse(string authToken, XUri redirect) {
            DreamMessage responseMsg;
            if(redirect == null) {
                responseMsg = DreamMessage.Ok(MimeType.TEXT_UTF8, authToken);
            } else {
                responseMsg = DreamMessage.Redirect(redirect);
            }
            // set expiration time, if one is provided
            DateTime expires = DateTime.MinValue; // Default to no expiration attribute (session cookie)
            TimeSpan authCookieExpirationTime = DekiContext.Current.Instance.AuthCookieExpirationTime;
            if(authCookieExpirationTime.TotalSeconds > 0) {
                expires = DateTime.UtcNow.Add(authCookieExpirationTime);
            }

            // add 'Set-Cookie' header
            responseMsg.Cookies.Add(DreamCookie.NewSetCookie(AUTHTOKEN_COOKIENAME, authToken, Self.Uri.AsPublicUri().WithoutPathQueryFragment(), expires));

            // add 'P3P' header for IE
            responseMsg.Headers["P3P"] = "CP=\"IDC DSP COR ADM DEVi TAIi PSA PSD IVAi IVDi CONi HIS OUR IND CNT\"";
            return responseMsg;
        }
    }
}

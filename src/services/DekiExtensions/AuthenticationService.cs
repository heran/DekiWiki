/*
 * MindTouch Core - open source enterprise collaborative networking
 * Copyright (c) 2006-2010 MindTouch Inc.
 * www.mindtouch.com  oss@mindtouch.com
 *
 * For community documentation and downloads visit developer.mindtouch.com;
 * please review the licensing section.
 *
 * This library is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 2.1 of the License, or (at your option) any later version.
 * 
 * This library is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 * 
 * You should have received a copy of the GNU Lesser General Public
 * License along with this library; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
 * http://www.gnu.org/copyleft/lesser.html
 */

using System;
using System.Collections.Specialized;
using System.Collections.Generic;

using MindTouch.Dream;
using MindTouch.Tasking;
using MindTouch.Web;
using MindTouch.Xml;

namespace MindTouch.Deki {
    using Yield = IEnumerator<IYield>;

    public abstract class DekiAuthenticationService : DreamService {

        //--- Types ---
        public class Group {

            //--- Fields ---
            public readonly string Name;
            public readonly DateTime Created;
            private NameValueCollection _custom;

            //--- Constructors ---

            /// <summary>
            /// Creates a group object.
            /// </summary>
            /// <param name="name">Group name</param>
            public Group(string name) {
                if(name == null) {
                    throw new ArgumentNullException("name");
                }
                this.Name = name;
            }

            //--- Properties ---
            public bool HasCustom { get { return _custom != null; } }

            public NameValueCollection Custom {
                get {
                    if(_custom == null) {
                        _custom = new NameValueCollection();
                    }
                    return _custom;
                }
            }

            //--- Methods ---
            public XDoc ToXml() {
                XDoc result = new XDoc("group");
                result.Attr("name", Name);
                if(HasCustom) {
                    foreach(KeyValuePair<string, string> custom in ArrayUtil.AllKeyValues(Custom)) {
                        result.Elem(custom.Key, custom.Value);
                    }
                }
                return result;
            }
        }

        public class User {

            //--- Fields ---
            public readonly string Name;
            public readonly string Email;
            public readonly Group[] Groups;
            private NameValueCollection _custom;

            //--- Fields ---

            /// <summary>
            /// Create a user object.
            /// </summary>
            /// <param name="name">User name</param>
            /// <param name="groups">List of groups the user is member of (must contain at least one group)</param>
            public User(string name, string email, Group[] groups) {
                if(name == null) {
                    throw new ArgumentNullException("name");
                }
                if(email == null) {
                    throw new ArgumentNullException("email");
                }
                if(groups == null) {
                    throw new ArgumentNullException("groups");
                }
                if((groups.Length == 0) || (groups[0] == null)) {
                    throw new ArgumentException("groups");
                }
                this.Name = name;
                this.Email = email;
                this.Groups = groups;
            }

            //--- Properties ---
            public bool HasCustom { get { return _custom != null; } }

            public NameValueCollection Custom {
                get {
                    if(_custom == null) {
                        _custom = new NameValueCollection();
                    }
                    return _custom;
                }
            }

            //--- Methods ---
            public XDoc ToXml() {
                XDoc result = new XDoc("user");
                result.Attr("name", Name);
                if(Email != null) {
                    result.Elem("email", Email);
                }
                if(HasCustom) {
                    foreach(KeyValuePair<string, string> custom in ArrayUtil.AllKeyValues(Custom)) {
                        result.Elem(custom.Key, custom.Value);
                    }
                }
                result.Start("groups");
                foreach(Group group in Groups) {
                    result.Add(group.ToXml());
                }
                result.End();
                return result;
            }
        }

        //--- Class Fields ---
        public readonly Group DefaultGroup = new Group("default");

        //--- Features ---
        [DreamFeature("GET:authenticate", "Authenticate a user.")]
        [DreamFeatureStatus(DreamStatus.Ok, "The request completed successfully")]
        [DreamFeatureStatus(DreamStatus.Unauthorized, "Authentication failed")]
        public Yield FeatureGetAuthenticate(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            string user = Authenticate(context, request);
            
            string password;
            HttpUtil.GetAuthentication(context.Uri.ToUri(), request.Headers, out user, out password);
            yield return context.Relay(Self.At("users", XUri.Encode(user)).WithCredentials(user, password), request, response);
        }

        [DreamFeature("GET:groups", "Retrieve a list of all groups.")]
        [DreamFeatureStatus(DreamStatus.Ok, "The request completed successfully")]
        [DreamFeatureStatus(DreamStatus.Unauthorized, "Authentication failed")]
        [DreamFeatureStatus(DreamStatus.NotFound, "No groups found")]
        public Yield FeatureGetGroups(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            Authenticate(context, request);
            Group[] groups = GetGroups();
            if(groups == null) {
                response.Return(DreamMessage.NotFound("No groups found"));
            } else {
                XDoc result = new XDoc("groups");
                foreach(Group group in groups) {
                    result.Add(group.ToXml());
                }
                response.Return(DreamMessage.Ok(result));
            }
            yield break;
        }

        [DreamFeature("GET:groups/{group}", "Retrieve information about a group.")]
        [DreamFeatureParam("{group}", "string", "identifies the group to retrieve")]
        [DreamFeatureStatus(DreamStatus.Ok, "The request completed successfully")]
        [DreamFeatureStatus(DreamStatus.Unauthorized, "Authentication failed")]
        [DreamFeatureStatus(DreamStatus.NotFound, "Requested group could not be found")]
        public Yield FeatureGetGroupInfo(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            Authenticate(context, request);
            string group = context.GetParam("group");
            Group result = GetGroup(group);
            if(result == null) {
                response.Return(DreamMessage.NotFound(string.Format("Group '{0}' not found", group)));
            } else {
                response.Return(DreamMessage.Ok(result.ToXml()));
            }
            yield break;
        }

        [DreamFeature("GET:users/{user}", "Retrieve information about a user.")]
        [DreamFeatureParam("{user}", "string", "identifies the user to retrieve")]
        [DreamFeatureStatus(DreamStatus.Ok, "The request completed successfully")]
        [DreamFeatureStatus(DreamStatus.Unauthorized, "Authentication failed")]
        [DreamFeatureStatus(DreamStatus.NotFound, "Requested user could not be found")]
        public Yield FeatureGetUserInfo(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            Authenticate(context, request);
            string user = context.GetParam("user");
            User result = GetUser(user);
            if(result == null) {
                response.Return(DreamMessage.NotFound(string.Format("User '{0}' not found", user)));
            } else {
                response.Return(DreamMessage.Ok(result.ToXml()));
            }
            yield break;
        }

        //--- Abstract Methods ---

        /// <summary>
        /// Check if password matches for user name.
        /// </summary>
        /// <param name="user">User name</param>
        /// <param name="password">Password</param>
        /// <returns>True if user name and password match</returns>
        public abstract bool CheckUserPassword(string user, string password);

        /// <summary>
        /// Returns information about a user.
        /// </summary>
        /// <param name="user">User name</param>
        /// <returns>User object if user name exists, otherwise null</returns>
        public abstract User GetUser(string user);

        //--- Methods ---

        /// <summary>
        /// Authenticate the current request.
        /// </summary>
        /// <param name="context">Context of current request</param>
        /// <param name="request">Current request</param>
        /// <returns>Authenticatd user name</returns>
        /// <exception cref="DreamAbortException">Throws a DreamAbortException with Access Denied response if authentication fails</exception>
        public virtual string Authenticate(DreamContext context, DreamMessage request) {
            string user;
            string password;
            HttpUtil.GetAuthentication(context.Uri.ToUri(), request.Headers, out user, out password);
            if(string.IsNullOrEmpty(user) || string.IsNullOrEmpty(password) || !CheckUserPassword(user, password)) {
                throw new DreamAbortException(DreamMessage.AccessDenied(AuthenticationRealm, "Missing or invalid credentials"));
            }
            return user;
        }

        /// <summary>
        /// Returns a list of all groups.
        /// </summary>
        /// <returns>List of groups</returns>
        public virtual Group[] GetGroups() {
            return new Group[] { DefaultGroup };
        }

        /// <summary>
        /// Returns information about a group.
        /// </summary>
        /// <param name="group">Group name</param>
        /// <returns>Group object if group name exists, otherwise null</returns>
        public virtual Group GetGroup(string group) {
            if(!StringUtil.EqualsInvariantIgnoreCase(group, "default")) {
                return null;
            }
            return DefaultGroup;
        }
    }
}

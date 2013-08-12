/*
 * MindTouch Deki Wiki - a commercial grade open source wiki
 * Copyright (C) 2006, 2007 MindTouch, Inc.
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
using log4net;

namespace MindTouch.Deki.Services {
    using Yield = IEnumerator<IYield>;


    [DreamService("MindTouch LDAP Authentication Service", "MindTouch, Inc. 2007",
       Info = "http://doc.opengarden.org/Deki_API/Reference/LdapAuthentication",
        SID = new string[] { 
            "sid://mindtouch.com/2007/05/BETA/ldap-authentication"
        }
    )]
    [DreamServiceConfig("hostname", "string", "hostname or ip of domain controller or ldap server. Port 389 used by default. Port 636 used by default with SSL enabled.")]
    [DreamServiceConfig("ssl", "bool?", "Use LDAPS mode. This requires your LDAP server to be running with SSL and for the certificate to be recognized on the machine running this LDAP service. (default: false)")]
    [DreamServiceConfig("ssl-ignore-cert-errors", "bool?", "Allows you to use self signed or expired certificates. This should only be used for testing. (default: false)")]
    [DreamServiceConfig("searchbase", "string", "The distinguished name (DN) of the domain. For example: 'DC=sd,DC=mindtouch,DC=com'")]
    [DreamServiceConfig("bindingdn", "string", "The DN to use for binding to LDAP. Use $1 to substitute with user name. ActiveDirectory example: $1@sd.mindtouch.com OpenLdap example: CN=$1,DC=sd,DC=mindtouch,DC=com")]
    [DreamServiceConfig("bindingpw", "string?", "Optional password for binding. Combined with a valid bindingdn account, queries to this service can be done without credentials")]
    [DreamServiceConfig("userquery", "string", "The search query to use for looking up users. Use $1 to substitute with user name. ActiveDirectory example: samAccountName=$1 OpenLdap example: cn=$1 Novell eDirectory example: uid=$1")]
    [DreamServiceConfig("timeout", "int?", "Timeout for directory operations in milliseconds")]
    [DreamServiceConfig("displayname-pattern", "string?", "Returns a friendlier name that can be customized by ldap attributes. Example: {sn}, {givenname}")]
    [DreamServiceConfig("groupquery", "string?", "LDAP query for group lookup by name. $1 is replaced by username. Default: (&(objectCategory=group)(cn=$1))")]
    [DreamServiceConfig("groupqueryall", "string?", "LDAP query for looking up all groups. Default: (objectCategory=group)")]
    [DreamServiceConfig("groupmembersattribute", "string?", "LDAP attribute for looking up members of a group. Default: memberof (works for AD). Use groupmembership for eDirectory")]
    [DreamServiceConfig("groupmembershipquery", "string?", "TODO")]
    [DreamServiceConfig("usernameattribute", "string?", "LDAP attribute for retrieving a users account name. Provide an attribute to always use rather then trying a series of common attributes. Default: attempts to use sAMAccountName -> uid -> name -> cn.")]
    [DreamServiceConfig("groupnameattribute", "string?", "LDAP attribute for retrieving a group name. Provide an attribute to always use rather then trying a series of common attributes. Default: attempts to use sAMAccountName -> uid -> name -> cn.")]
    [DreamServiceConfig("verboselogging", "bool?", "Will output more details to the log as TRACE level. Warning: usernames+passwords are included as well. Default: false")]
    [DreamServiceBlueprint("dekiwiki/service-type", "authentication")]
    public class LdapAuthenticationService : DreamService {

        //--- Constants ---
        public const int DEFAULT_TIMEOUT = 5000;
        public const int MIN_TIMEOUT = 500;

        //--- Types ---
        public class LdapConfig {

            //--- Fields ---
            public string LdapHostname = string.Empty;
            public bool SSL = false;
            public bool SSLIgnoreCertErrors = false;
            public string LdapSearchBase = string.Empty;
            public string BindingDn = string.Empty;
            public string BindingPw = string.Empty;
            public string UserQuery = string.Empty;
            public string GroupQuery = string.Empty;
            public string GroupQueryAll = string.Empty;
            public string GroupMembersAttribute = string.Empty;
            public string GroupMembershipQuery = string.Empty;
            public string UserNameAttribute = string.Empty;
            public string GroupNameAttribute = string.Empty;            
            public string DisplayNamePattern = string.Empty;
            public int LdapTimeOut = 0;
            public bool VerboseLogging = false;
        }

        //--- Fields ---
        private LdapConfig _config;

        //--- Properties ---
        public override string AuthenticationRealm {
            get {
                return "LDAP: " + _config.LdapSearchBase;
            }
        }

        //--- Features ---
        [DreamFeature("GET:groups/{groupname}", "Retrieve verbose information about a given group.")]
        [DreamFeatureParam("output", "string?", "'verbose' will retrieve group member information. 'brief' will not. Default: 'brief'")]
        [DreamFeatureParam("timelimit", "int?", "Timelimit in milliseconds for lookup. Default = 5000")]
        public Yield GetGroupInfo(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            string groupname = context.GetParam("groupname", null);
            string output = context.GetParam("output", "brief").Trim();
            LdapClient ldap = GetLdapClient(context, request, false);
            XDoc groupXml = ldap.GetGroupInfo(StringUtil.EqualsInvariant(output, "verbose"), 3, groupname);
            if( groupXml == null)
                response.Return(DreamMessage.NotFound(string.Format("Group '{0}' not found", groupname)));
            else
                response.Return(DreamMessage.Ok(groupXml));
            yield break;
        }

        [DreamFeature("GET:groups/", "Retrieve all groups found in the directory.")]
        [DreamFeatureParam("output", "string?", "'verbose' will retrieve group member information. 'brief' will not. Default: 'brief'")]
        [DreamFeatureParam("timelimit", "int?", "Timelimit in milliseconds for lookup. Default = 5000")]
        public Yield GetGroups(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            string output = context.GetParam("output", "brief").Trim();
            LdapClient ldap = GetLdapClient(context, request, false);
            XDoc groupXml = ldap.GetGroupInfo(StringUtil.EqualsInvariant(output, "verbose"), 3, null);
            if (groupXml == null)
                response.Return(DreamMessage.NotFound("No groups found"));
            else
                response.Return(DreamMessage.Ok(groupXml));

            yield break;
        }

        [DreamFeature("GET:users/{username}", "Retrieve information about a given user.")]
        [DreamFeatureParam("timelimit", "int?", "Timelimit in milliseconds for lookup. Default = 5000")]
        public Yield GetUserInfo(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            string username = context.GetParam("username");
            LdapClient ldap = GetLdapClient(context, request, false);

            XDoc userXml = ldap.GetUserInfo(true, 3, username);

            if (userXml == null)
                response.Return(DreamMessage.NotFound(string.Format("User '{0}' not found. Search query used: '{1}'", username, ldap.BuildUserSearchQuery(username))));
            else
                response.Return(DreamMessage.Ok(userXml));

            yield break;
        }

        [DreamFeature("GET:authenticate", "Authenticate a user with the directory.")]
        [DreamFeatureParam("timelimit", "int?", "Timelimit in milliseconds for lookup. Default = 5000")]
        public Yield UserLogin(DreamContext context, DreamMessage request, Result<DreamMessage> response) {

            //This will attempt to bind to ldap with credentials from http header.
            //Non authentication exceptions will be returned to user.
            //Authentication failure will result in a DreamMessage.AccessDenied response
            LdapClient ldapClient = GetLdapClient(context, request, true);

            XDoc userXml = ldapClient.GetUserInfo(true, 3, ldapClient.UserName);
            
            if (userXml == null)
                response.Return(DreamMessage.NotFound(string.Format("User '{0}' not found. Search query used: '{1}'", ldapClient.UserName, ldapClient.BuildUserSearchQuery(ldapClient.UserName))));
            else
                response.Return(DreamMessage.Ok(userXml));

            yield break;
        }

        //--- Methods ---
        public override Yield Start(XDoc config, Result result) {
            yield return Coroutine.Invoke(base.Start, config, new Result()).Catch(result);

            // TODO MaxM: Validate config.
            _config = new LdapConfig();
            _config.LdapHostname = config["hostname"].AsText ?? config["ldaphostname"].AsText;
            _config.LdapSearchBase = config["searchbase"].AsText ?? config["ldapsearchbase"].AsText;
            _config.BindingDn = config["bindingdn"].AsText ?? config["ldapbindingdn"].AsText;
            _config.BindingPw = config["bindingpw"].AsText ?? config["ldapbindingpw"].AsText;
            _config.UserQuery = config["userquery"].AsText ?? config["ldapuserquery"].AsText;
            _config.DisplayNamePattern = config["displayname-pattern"].AsText;
            _config.GroupQuery = config["groupquery"].AsText ?? "(&(objectClass=group)(cn=$1))";
            _config.GroupQueryAll = config["groupqueryall"].AsText ?? "(objectClass=group)";
            _config.GroupMembersAttribute = config["groupmembersattribute"].AsText ?? "memberof";
            _config.GroupMembershipQuery = config["groupmembershipquery"].AsText;
            _config.UserNameAttribute = config["usernameattribute"].AsText;
            _config.GroupNameAttribute = config["groupnameattribute"].AsText;
            _config.SSL = config["ssl"].AsBool ?? false;
            _config.SSLIgnoreCertErrors = config["ssl-ignore-cert-errors"].AsBool ?? false;

            _config.LdapTimeOut = Math.Max(MIN_TIMEOUT, config["timeout"].AsInt ?? config["ldaptimeout"].AsInt ?? DEFAULT_TIMEOUT);
            _config.VerboseLogging = config["verboselogging"].AsBool ?? false;
                       
            try {
                string ARGERROR = "LDAP Service config parameter not provided";

                // validate configuration
                if (string.IsNullOrEmpty(_config.LdapHostname)) {
                    throw new ArgumentException(ARGERROR, "hostname");
                }
                if (string.IsNullOrEmpty(_config.LdapSearchBase)) {
                    throw new ArgumentException(ARGERROR, "searchbase");
                }

                if (string.IsNullOrEmpty(_config.BindingDn)) {
                    throw new ArgumentException(ARGERROR, "bindingdn");
                }

                if (string.IsNullOrEmpty(_config.UserQuery)) {
                    throw new ArgumentException(ARGERROR, "userquery");
                }
            }
            catch (ArgumentException ae) {
                throw new DreamBadRequestException(ae.Message);
            }

            result.Return();
        }

        /// <summary>
        /// Returns a connected/authenticated ldapclient. Authentication info either comes from
        /// the standard authentication header or from local configuration.
        /// Default scope and domain controller IP/Host must be in service configuration.
        /// </summary>
        /// <param name="requireHeaderAuth">Will only accept authenticate from request header</param>
        /// <returns></returns>
        private LdapClient GetLdapClient(DreamContext context, DreamMessage request, bool requireAuth) {
            string authuser = string.Empty;
            string authpassword = string.Empty;

            HttpUtil.GetAuthentication(context.Uri.ToUri(), request.Headers, out authuser, out authpassword);
            if (_config.VerboseLogging) {
                LogUtils.LogTrace(_log, context.Feature.VerbSignature, string.Format("Performing LDAP lookup uri: '{0}' username: '{1}' pw: '{2}'", context.Feature.VerbSignature, authuser, authpassword));
            }

            if (string.IsNullOrEmpty(authuser) && requireAuth)
                throw new DreamAbortException(DreamMessage.AccessDenied(AuthenticationRealm, "Provide credentials to authenticate with ldap"));

            LdapClient ldap = new LdapClient(_config, authuser, authpassword, _log);
            ldap.TimeLimit = context.GetParam<int>("timelimit", _config.LdapTimeOut);
            if (requireAuth) {
                bool authenticated = ldap.Authenticate();

                if (!authenticated) {
                    string msg = string.Format("Invalid LDAP username or password. Login DN used: '{0}'", ldap.BuildBindDn(authuser));
                    throw new DreamAbortException(DreamMessage.AccessDenied(AuthenticationRealm, msg));
                }
            }
            return ldap;
        }
    }
}

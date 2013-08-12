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
using System.Security.Cryptography.X509Certificates;

using Novell.Directory.Ldap;
using MindTouch;
using MindTouch.Dream;
using log4net;
using System.Text.RegularExpressions;

namespace MindTouch.Deki.Services {
    public class LdapClient {

        //--- Fields ---
        private string _username;
        private string _password;
        private ILog _log;
        private int _timeLimit = 5000;
        private const int LDAP_PORT = 389;
        private const int LDAPS_PORT = 636;

        private LdapAuthenticationService.LdapConfig _config;

        //--- Constructors ---
        public LdapClient(LdapAuthenticationService.LdapConfig config, string username, string password, ILog logger) {
            _config = config;
            _log = logger;
            _username = username;
            _password = password;
        }

        //--- Methods ---
        private LdapConnection GetLdapConnectionFromBindingDN(string server, string bindingdn, string password) {
            LdapConnection conn = null;
            try {
                conn = new LdapConnection();
                conn.SecureSocketLayer = _config.SSL;
                int port = _config.SSL ? LDAPS_PORT : LDAP_PORT;
                conn.UserDefinedServerCertValidationDelegate += new CertificateValidationCallback(ValidateCert);

                //if server has a port number specified, it's used instead.
                conn.Connect(server, port);

                if (!string.IsNullOrEmpty(bindingdn)) {
                    conn.Bind(bindingdn, password);
                }

            } catch (Exception x) {
                UnBind(conn);

                LogUtils.LogWarning(_log, x, "GetLdapConnection", string.Format("Failed to bind to LDAP server: '{0}' with bindingdn: '{1}'. Password provided? {2}. Exception: {3}", server, bindingdn, string.IsNullOrEmpty(password).ToString(), x.ToString()));
                throw;
            }
            return conn;
        }

        /// <summary>
        /// Authenticates by creating a bind.
        /// Connection exceptions will be thrown but invalid credential will return false
        /// </summary>
        /// <returns></returns>
        public bool Authenticate() {
            bool ret = false;
            LdapConnection conn = null;
            LdapConnection queryConn = null;
            try {

                //When using the proxy bind, authentications requires a user lookup and another bind.
                if (!string.IsNullOrEmpty(_config.BindingPw)) {
                    LdapSearchResults authUser = LookupLdapUser(false, _username, out queryConn);
                    if (authUser.hasMore()) {
                        LdapEntry entry = authUser.next();
                        conn = Bind(_config.LdapHostname, entry.DN, _password);
                    } else {
                        _log.WarnFormat("No users matched search creteria for username '{0}'", _username);
                        ret = false;
                    }
                } else {
                    conn = Bind();
                }

                if (conn != null) {
                    ret = conn.Bound;
                }

            } catch (LdapException x) {
                ret = false;
                if (x.ResultCode != LdapException.INVALID_CREDENTIALS) {
                    throw;
                }
            } finally {
                UnBind(queryConn);
                UnBind(conn);
            }

            return ret;
        }

        private LdapConnection Bind() {

            string hostname = _config.LdapHostname;
            string bindDN = string.Empty;
            string bindPW = string.Empty;

            //Determine if a query account is configured or not with BindingDN and BindingPW
            if (!string.IsNullOrEmpty(_config.BindingPw)) {

                bindDN = _config.BindingDn;
                bindPW = _config.BindingPw;
            } else {

                //No preconfigured account exists: need to establish a bind with provided credentials.
                bindDN = BuildBindDn(_username);
                bindPW = _password;
            }

            return Bind(hostname, bindDN, bindPW);
        }

        private LdapConnection Bind(string hostname, string bindDN, string bindPW) {
            LdapConnection conn = null;
            try {

                //Establish ldap bind
                conn = GetLdapConnectionFromBindingDN(hostname, bindDN, bindPW);

                if (!conn.Bound) {
                    UnBind(conn);
                    conn = null;

                    //Sometimes it doesn't throw an exception but ramains unbound. 
                    throw new DreamAbortException(DreamMessage.AccessDenied("Deki LDAP Service", string.Format("An LDAP bind was not established. Server: '{0}'. BindingDN: '{1}'. Password provided? '{2}'", hostname, bindDN, string.IsNullOrEmpty(bindPW) ? "No." : "Yes.")));
                }
            } catch (LdapException x) {
                UnBind(conn);

                if (x.ResultCode == LdapException.INVALID_CREDENTIALS) {
                    throw new DreamAbortException(DreamMessage.AccessDenied("Deki LDAP Service", string.Format("Invalid LDAP credentials. Server: '{0}'. BindingDN: '{1}'. Password provided? '{2}'", hostname, bindDN, string.IsNullOrEmpty(bindPW) ? "No." : "Yes.")));
                } else {
                    throw;
                }
            }
            return conn;
        }

        private void UnBind(LdapConnection conn) {

            if (conn != null && conn.Connected) {
                try {
                    conn.Disconnect();
                } catch { }
            }
        }

        private LdapSearchResults LookupLdapUser(bool retrieveGroupMembership, string username, out LdapConnection conn) {

            conn = Bind();

            username = EscapeLdapString(username);

            //search filter is built based on passed userQuery with username substitution
            string searchFilter = this.BuildUserSearchQuery(username);

            //Build interesting attribute list
            List<string> attrs = new List<string>();
            attrs.AddRange(new string[] { "sAMAccountName", "uid", "cn", "userAccountControl", "whenCreated", "name", "givenname", "sn", "telephonenumber", "mail", "description" });
            if (retrieveGroupMembership) {
                attrs.Add(_config.GroupMembersAttribute);
            }

            if (!string.IsNullOrEmpty(_config.UserNameAttribute) && !attrs.Contains(_config.UserNameAttribute)) {
                attrs.Add(_config.UserNameAttribute);
            }

            //add more attributes to lookup if using a displayname-pattern
            string[] patternAttributes = RetrieveAttributesFromPattern(_config.DisplayNamePattern);
            if (patternAttributes != null) {
                foreach (string patternAttribute in patternAttributes) {
                    if (!attrs.Contains(patternAttribute))
                        attrs.Add(patternAttribute);
                }
            }

            LdapSearchConstraints cons = new LdapSearchConstraints(new LdapConstraints(_timeLimit, true, null, 0));
            cons.BatchSize = 0;

            LdapSearchResults results = conn.Search(_config.LdapSearchBase,
                            LdapConnection.SCOPE_SUB,
                             searchFilter,
                             attrs.ToArray(),
                             false,
                             cons);

            return results;
        }

        /// <summary>
        /// Authenticates by creating a bind and returning info about the user.
        /// This either returns a /users/user xml block or an exception xml if unable to connect or authenticate.
        /// In case of exception, invalid credentials is noted as /exception/message = "invalid credentials"
        /// </summary>
        /// <returns></returns>
        public XDoc AuthenticateXml() {
            return GetUserInfo(false, _username);
        }

        #region overloads for retrial handling
        public XDoc GetUserInfo(bool retrieveGroupMembership, uint retries, string username) {
            do {
                try {
                    return GetUserInfo(retrieveGroupMembership, username);
                } catch (TimeoutException) { }
            } while (retries-- > 0);

            throw new TimeoutException();
        }

        public XDoc GetGroupInfo(bool retrieveGroupMembers, uint retries, string optionalGroupName) {
            do {
                try {
                    return GetGroupInfo(retrieveGroupMembers, optionalGroupName);
                } catch (TimeoutException) { }
            } while (retries-- > 0);

            throw new TimeoutException();
        }

        #endregion

        /// <summary>
        /// Retrieve information about one or more users
        /// </summary>
        /// <param name="retrieveGroupMembership">retrieving list of groups for each user will take longer</param>
        /// <param name="username">Username to lookup</param>
        /// <returns></returns>
        public XDoc GetUserInfo(bool retrieveGroupMembership, string username) {

            XDoc resultXml = null;
            LdapConnection conn = null;

            try {
                LdapSearchResults results = LookupLdapUser(retrieveGroupMembership, username, out conn);

                if (results.hasMore()) {
                    LdapEntry nextEntry = null;
                    try {
                        nextEntry = results.next();
                    } catch (LdapException x) {
                        HandleLdapException(x);
                    }

                    if (nextEntry == null)
                        throw new ArgumentNullException("nextEntry");

                    //Create xml from search entry
                    resultXml = new XDoc("user");

                    string name = string.Empty;

                    //If a usernameattribute is configured, use that. Otherwise try the common ones.
                    if (!string.IsNullOrEmpty(_config.UserNameAttribute)) {
                        name = GetAttributeSafe(nextEntry, _config.UserNameAttribute);
                    } else {
                        name = GetAttributeSafe(nextEntry, "sAMAccountName"); //MS Active Directory
                        if (string.IsNullOrEmpty(name))
                            name = GetAttributeSafe(nextEntry, "uid"); //OpenLDAP
                        if (string.IsNullOrEmpty(name))
                            name = GetAttributeSafe(nextEntry, "name"); //OpenLDAP
                        if (string.IsNullOrEmpty(name))
                            name = GetAttributeSafe(nextEntry, "cn"); //Novell eDirectory
                    }

                    string displayName = BuildDisplayNameFromPattern(_config.DisplayNamePattern, nextEntry);

                    resultXml.Attr("name", name);
                    if (!string.IsNullOrEmpty(displayName))
                        resultXml.Attr("displayname", displayName);

                    resultXml.Start("ldap-dn").Value(nextEntry.DN).End();
                    resultXml.Start("date.created").Value(ldapStringToDate(GetAttributeSafe(nextEntry, "whenCreated"))).End();
                    resultXml.Start("firstname").Value(GetAttributeSafe(nextEntry, "givenname")).End();
                    resultXml.Start("lastname").Value(GetAttributeSafe(nextEntry, "sn")).End();
                    resultXml.Start("phonenumber").Value(GetAttributeSafe(nextEntry, "telephonenumber")).End();
                    resultXml.Start("email").Value(GetAttributeSafe(nextEntry, "mail")).End();
                    resultXml.Start("description").Value(GetAttributeSafe(nextEntry, "description")).End();

                    //Retrieve group memberships

                    if (string.IsNullOrEmpty(_config.GroupMembershipQuery)) {
                        LdapAttributeSet memberAttrSet = nextEntry.getAttributeSet();

                        LdapAttribute memberAttr = null;
                        if (memberAttrSet != null)
                            memberAttr = memberAttrSet.getAttribute(_config.GroupMembersAttribute);

                        if (memberAttr != null) {
                            resultXml.Start("groups");
                            foreach (string member in memberAttr.StringValueArray) {
                                resultXml.Start("group");
                                resultXml.Attr("name", GetNameFromDn(member));
                                resultXml.Start("ldap-dn").Value(member).End();
                                resultXml.End();
                            }
                            resultXml.End();
                        }
                    } else {

                        //Perform custom query to determine groups of a user
                        PopulateGroupsForUserWithQuery(resultXml, username, conn);

                    }
                }
            } finally {
                UnBind(conn);
            }

            return resultXml;
        }

        private void PopulateGroupsForUserWithQuery(XDoc doc, string username, LdapConnection conn) {
            doc.Start("groups");

            string searchFilter = string.Format(Dream.PhpUtil.ConvertToFormatString(_config.GroupMembershipQuery), username);

            //Build interesting attribute list
            List<string> attrs = new List<string>();
            attrs.AddRange(new string[] { "whenCreated", "name", "sAMAccountName", "cn" });

            LdapSearchConstraints cons = new LdapSearchConstraints(new LdapConstraints(_timeLimit, true, null, 0));
            cons.BatchSize = 0;

            LdapSearchResults results = conn.Search(_config.LdapSearchBase,
                            LdapConnection.SCOPE_SUB,
                             searchFilter,
                             attrs.ToArray(),
                             false,
                             cons);

            while (results.hasMore()) {
                LdapEntry nextEntry = null;
                try {
                    nextEntry = results.next();
                } catch (LdapException x) {
                    HandleLdapException(x);
                }

                if (nextEntry == null)
                    throw new ArgumentNullException("nextEntry");

                //Create xml from search entry
                doc.Start("group").Attr("name", GetNameFromDn(nextEntry.DN)).Start("ldap-dn").Value(nextEntry.DN).End().End();
            }

            doc.End(); //groups
        }

        /// <summary>
        /// Retrieves group information from ldap
        /// </summary>
        /// <param name="retrieveGroupMembers">true to return users in each group. This may hurt performance</param>
        /// <param name="optionalGroupName">Group to lookup by name. Null for all groups</param>
        /// <returns></returns>
        public XDoc GetGroupInfo(bool retrieveGroupMembers, string optionalGroupName) {
            LdapConnection conn = null;
            XDoc resultXml = null;
            try {

                //Confirm a query bind has been established
                conn = Bind();

                string searchFilter;

                //Build the searchfilter based on if a group name is given.
                if (!string.IsNullOrEmpty(optionalGroupName)) {

                    optionalGroupName = EscapeLdapString(optionalGroupName);

                    //Looking up group by name
                    searchFilter = string.Format(PhpUtil.ConvertToFormatString(_config.GroupQuery), optionalGroupName);
                } else {

                    //Looking up all groups
                    searchFilter = _config.GroupQueryAll;
                }

                //Build interesting attribute list
                List<string> attrs = new List<string>();
                attrs.AddRange(new string[] { "whenCreated", "name", "sAMAccountName", "cn" });
                if (retrieveGroupMembers) {
                    attrs.Add("member");
                }

                if (!string.IsNullOrEmpty(_config.GroupNameAttribute) && !attrs.Contains(_config.GroupNameAttribute)) {
                    attrs.Add(_config.GroupNameAttribute);
                }

                LdapSearchConstraints cons = new LdapSearchConstraints(new LdapConstraints(_timeLimit, true, null, 0));
                cons.BatchSize = 0;

                LdapSearchResults results = conn.Search(_config.LdapSearchBase,
                               LdapConnection.SCOPE_SUB,
                                searchFilter,
                                attrs.ToArray(),
                                false,
                                cons);

                //Create outer groups collection if multiple groups are being looked up or none provided
                if (string.IsNullOrEmpty(optionalGroupName))
                    resultXml = new XDoc("groups");

                while (results.hasMore()) {
                    LdapEntry nextEntry = null;
                    try {
                        nextEntry = results.next();
                    } catch (LdapException x) {
                        HandleLdapException(x);
                        continue;
                    }

                    //Create xml from search entry
                    if (resultXml == null)
                        resultXml = new XDoc("group");
                    else
                        resultXml.Start("group");

                    string name = string.Empty;

                    //If a groupnameattribute is configured, use that. Otherwise try the common ones.
                    if (!string.IsNullOrEmpty(_config.GroupNameAttribute)) {
                        name = GetAttributeSafe(nextEntry, _config.GroupNameAttribute);
                    } else {
                        name = GetAttributeSafe(nextEntry, "sAMAccountName"); //MS Active Directory
                        if (string.IsNullOrEmpty(name))
                            name = GetAttributeSafe(nextEntry, "uid"); //OpenLDAP
                        if (string.IsNullOrEmpty(name))
                            name = GetAttributeSafe(nextEntry, "name"); //OpenLDAP
                        if (string.IsNullOrEmpty(name))
                            name = GetAttributeSafe(nextEntry, "cn"); //Novell eDirectory
                    }

                    resultXml.Attr("name", name);
                    resultXml.Start("ldap-dn").Value(nextEntry.DN).End();
                    resultXml.Start("date.created").Value(ldapStringToDate(GetAttributeSafe(nextEntry, "whenCreated"))).End();

                    //Retrieve and write group membership to xml
                    LdapAttributeSet memberAttrSet = nextEntry.getAttributeSet();
                    LdapAttribute memberAttr = memberAttrSet.getAttribute("member");

                    // TODO MaxM: This currently does not differentiate between user and group
                    // members. 

                    if (memberAttr != null) {
                        foreach (string member in memberAttr.StringValueArray) {
                            resultXml.Start("member");
                            resultXml.Attr("name", GetNameFromDn(member));
                            resultXml.Start("ldap-dn").Value(member).End();
                            resultXml.End();
                        }
                    }
                    if (string.IsNullOrEmpty(optionalGroupName))
                        resultXml.End();

                }
            } finally {
                UnBind(conn);
            }

            return resultXml;
        }

        #region Properties

        /// <summary>
        /// In milliseconds. Default = 5000
        /// </summary>
        public int TimeLimit {
            get { return _timeLimit; }
            set { _timeLimit = value; }
        }

        public string UserName {
            get { return _username; }
        }

        #endregion

        public string BuildUserSearchQuery(string username) {
            return string.Format(Dream.PhpUtil.ConvertToFormatString(_config.UserQuery), username);
        }

        public string BuildBindDn(string username) {
            if (string.IsNullOrEmpty(username))
                return null;
            return
                string.Format(PhpUtil.ConvertToFormatString(_config.BindingDn), username);
        }

        #region Helper methods

        private string GetNameFromDn(string dn) {
            string name;
            name = dn.Split(',')[0].Split('=')[1];
            name = UnEscapeLdapString(name);
            return name;
        }

        private DateTime ldapStringToDate(string ldapDate) {
            if (string.IsNullOrEmpty(ldapDate))
                return DateTime.MinValue;
            else {
                DateTime result;
                return DateTime.TryParseExact(ldapDate, "yyyyMMddHHmmss.0Z", System.Globalization.DateTimeFormatInfo.CurrentInfo, System.Globalization.DateTimeStyles.AssumeUniversal, out result) ? result : DateTime.MinValue;
            }
        }

        private string GetAttributeSafe(LdapEntry entry, string attributeName) {
            string ret = string.Empty;

            if (entry != null && !String.IsNullOrEmpty(attributeName)) {
                LdapAttribute attr = entry.getAttribute(attributeName);
                if (attr != null)
                    ret = attr.StringValue;
            }

            return ret;
        }

        private static readonly Regex _displayNamePatternRegex = new Regex("{(?<attribute>[^}]*)}", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private string BuildDisplayNameFromPattern(string displayNamePattern, LdapEntry entry) {
            if (string.IsNullOrEmpty(displayNamePattern))
                return string.Empty;

            string displayName = displayNamePattern;
            string[] attributes = RetrieveAttributesFromPattern(displayNamePattern);
            if (attributes != null) {
                foreach (string attribute in attributes) {
                    displayName = displayName.Replace("{" + attribute + "}", GetAttributeSafe(entry, attribute));
                }
            }

            return displayName;
        }

        private string[] RetrieveAttributesFromPattern(string displayNamePattern) {
            if (string.IsNullOrEmpty(displayNamePattern))
                return null;

            List<string> attributes = null;
            try {
                MatchCollection mc = _displayNamePatternRegex.Matches(displayNamePattern);
                attributes = new List<string>();
                foreach (Match m in mc) {
                    attributes.Add(m.Groups["attribute"].Value);
                }
            } catch (Exception x) {
                _log.Warn(string.Format("Could not parse the displayname-pattern '{0}'", displayNamePattern), x);
                attributes = null;
            }

            if (attributes == null)
                return null;
            else
                return attributes.ToArray();
        }

        private void HandleLdapException(LdapException x) {
            switch (x.ResultCode) {
            case LdapException.Ldap_TIMEOUT:
                throw new TimeoutException("Ldap lookup timed out", x);
            case LdapException.OPERATIONS_ERROR:
            case LdapException.INVALID_DN_SYNTAX:
                if (x.ResultCode == 1 && x.LdapErrorMessage.Contains("DSID-0C090627"))
                    throw new DreamAbortException(DreamMessage.Forbidden(string.Format("Account '{0}' is disabled", this._username)));

                throw new ArgumentException(string.Format("The search base '{0}' may have invalid format (Example: 'DC=sales,DC=acme,DC=com') or the account used for binding may be disabled. Error returned from LDAP: {1}", _config.LdapSearchBase, x.LdapErrorMessage), x);
            default:
                throw x;
            }
        }

        private string UnEscapeLdapString(string original) {
            if (string.IsNullOrEmpty(original))
                return original;

            return original.Replace("\\", "");
        }


        private string EscapeLdapString(string original) {

            //Per http://www.ietf.org/rfc/rfc2253.txt section 2.4

            if (string.IsNullOrEmpty(original))
                return original;

            StringBuilder sb = new StringBuilder();
            foreach (char c in original) {

                if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || (c == ' ')) {
                    sb.Append(c);
                } else {
                    sb.Append(@"\" + Convert.ToString((int) c, 16));
                }
            }

            return sb.ToString();
        }

        #endregion

        private bool ValidateCert(X509Certificate certificate, int[] certificateErrors) {
            if (certificateErrors.Length == 0) {
                return true;
            }

            string errors = string.Join(",", Array.ConvertAll<int, string>(certificateErrors, new Converter<int, string>(delegate(int value) { return value.ToString(); })));
            _log.WarnFormat("Got error# {0} from LDAPS certificate: {1}", errors, certificate.ToString(true));
            return _config.SSLIgnoreCertErrors;
        }
    }

    public class AccountDisabledException : Exception {
        public AccountDisabledException() { }
    }
}

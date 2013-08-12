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
using System.Security.Cryptography;
using System.Text;

using MindTouch.Data;
using MindTouch.Dream;
using MindTouch.Tasking;
using MindTouch.Xml;

namespace MindTouch.Deki.Services {
    using Yield = IEnumerator<IYield>;

    [DreamService("MindTouch Joomla Authentication Service", "Copyright (c) 2006-2010 MindTouch Inc.",
       Info = "http://doc.opengarden.org/Deki_Wiki/Authentication/JoomlaAuthentication",
        SID = new string[] { 
            "sid://mindtouch.com/2007/07/joomla-authentication",
            "http://services.mindtouch.com/deki/draft/2007/07/joomla-authentication", 
        }
    )]
    [DreamServiceConfig("db-server", "string?", "Database host name (default: localhost).")]
    [DreamServiceConfig("db-port", "int?", "Database port (default: 3306)")]
    [DreamServiceConfig("db-catalog", "string", "Database name")]
    [DreamServiceConfig("db-user", "string", "Database user name")]
    [DreamServiceConfig("db-password", "string", "Password for database user")]
    [DreamServiceConfig("db-options", "string?", "Connection string parameters (default: \"\")")]
    [DreamServiceConfig("db-tableprefix", "string?", "Prefix for table names (default: \"jos_\")")]
    [DreamServiceConfig("joomla-version", "string", "Version of Joomla (default: 1.6)")]
    [DreamServiceBlueprint("deki/service-type", "authentication")]
    public class JoomlaAuthenticationService : DekiAuthenticationService {

        //--- Class Fields ---
        private static log4net.ILog _log = LogUtils.CreateLog();

        //--- Fields ---
        private DataFactory _factory;
        private DataCatalog _catalog;
        private string _prefix;
        private string _version;
       
        //--- Properties ---
        public override string AuthenticationRealm {
            get {
                return "Joomla";
            }
        }

        protected override Yield Start(XDoc config, Result result) {
            yield return Coroutine.Invoke(base.Start, config, new Result());

            _factory = new DataFactory(MySql.Data.MySqlClient.MySqlClientFactory.Instance, "?");
            _catalog = new DataCatalog(_factory, config);
           
            _prefix = config["db-tableprefix"].AsText ?? "jos_";
            _version = config["joomla-version"].AsText;
            if (_version == null)
                _version = "1.6";
            else if (_version != "1.0" && _version != "1.5" && _version != "1.6")
            {
                _log.Warn("joomla-version is set incorrectly: " + _version + ". Should be one of the following: 1.0, 1.5, 1.6");
                _version = "1.6";
            }

            try {
                _catalog.TestConnection();
            } catch(Exception) {
                throw new Exception(string.Format("Unable to connect to joomla instance with connectionString: {0}", _catalog.ConnectionString));
            }
            result.Return();
        }

        public override bool CheckUserPassword(string user, string password) {
            DataCommand cmd = _catalog.NewQuery(string.Format("SELECT password FROM {1}users WHERE username='{0}' AND block=0 LIMIT 1", DataCommand.MakeSqlSafe(user), _prefix));
            string remotePassword = cmd.Read();
            if (remotePassword == null)
                return false;
            if (remotePassword == password)
                return true;
            
            if (remotePassword.Contains(":"))
            {
                // Extract Salt password
                string[] split = remotePassword.Split(':');
                // Encode in MD5
                byte[] textBytes = Encoding.UTF8.GetBytes(password + split[1]);
                byte[] hash = MD5.Create().ComputeHash(textBytes);
                // Create hash to Hex string
                StringBuilder s = new StringBuilder();
                foreach (byte a in hash)
                    s.Append(a.ToString("x2").ToLower());
                
                if(String.Compare(split[0], s.ToString(), true) == 0)
                    return true;  
            }
            return false;

        }

        public override User GetUser(string user) {
            string name = "", email = "";
            DataCommand cmd = _catalog.NewQuery(string.Format("SELECT username, email, usertype FROM {1}users WHERE username='{0}' AND block=0 LIMIT 1", DataCommand.MakeSqlSafe(user), _prefix));
            XDoc result = cmd.ReadAsXDoc("users", "user");

            if(result["user"].IsEmpty)
                return null;

            name = result["user/username"].AsText;
            email = result["user/email"].AsText;
            Group group = new Group(result["user/usertype"].AsText);

            return new User(name, email, new Group[] { group });
        }

        public override Group[] GetGroups() {
            IList<Group> groupList = new List<Group>();

            DataCommand cmd;
            XDoc groupsXDoc;
            // Compatability with 1.0 version
            switch(_version.Substring(0,3))
            {
                case "1.0":
                    cmd = _catalog.NewQuery(string.Format("SELECT name FROM {0}usertypes", _prefix));
                    groupsXDoc = cmd.ReadAsXDoc("usertypes", "usertype");
                    foreach (XDoc group in groupsXDoc["usertype/name"])
                        groupList.Add(new Group(group.AsText));
                    break;
                default:
                    cmd = _catalog.NewQuery(string.Format("SELECT name FROM {0}core_acl_aro_groups", _prefix));
                    groupsXDoc = cmd.ReadAsXDoc("groups", "group");
                    foreach (XDoc group in groupsXDoc["group/name"])
                        groupList.Add(new Group(group.AsText));
                    break;
            }

            Group[] groups = new Group[groupList.Count];
            groupList.CopyTo(groups, 0);
            return groups;
        }

        public override Group GetGroup(string group) {
            DataCommand cmd;
            // Compatability with 1.0 version

            switch (_version.Substring(0,3))
            {
                case "1.0":
                    cmd = _catalog.NewQuery(string.Format("SELECT name FROM {1}usertypes where name='{0}' LIMIT 1", DataCommand.MakeSqlSafe(group), _prefix));
                    break;
                default:
                    cmd = _catalog.NewQuery(string.Format("SELECT name FROM {1}core_acl_aro_groups where name='{0}' LIMIT 1", DataCommand.MakeSqlSafe(group), _prefix));
                    break;
            }
            XDoc groupXDoc = cmd.ReadAsXDoc("groups", "group");
            if(groupXDoc["group/name"].IsEmpty)
                return null;

            return new Group(groupXDoc["group/name"].AsText);
        }
    }
}

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

using MindTouch.Data;
using MindTouch.Dream;
using MindTouch.Tasking;
using MindTouch.Xml;

namespace MindTouch.Deki.Services {
    using Yield = IEnumerator<IYield>;

    [DreamService("MindTouch Drupal Authentication Service", "Copyright (c) 2006-2010 MindTouch Inc.",
       Info = "http://doc.opengarden.org/Deki_Wiki/Authentication/DrupalAuthentication",
        SID = new string[] { 
            "sid://mindtouch.com/2007/05/drupal-authentication",
            "http://services.mindtouch.com/deki/draft/2007/05/drupal-authentication" ,
            "http://services.mindtouch.com/deki/draft/2007/05/drupal"
        }
    )]
    [DreamServiceConfig("db-server", "string?", "Database host name (default: localhost).")]
    [DreamServiceConfig("db-port", "int?", "Database port (default: 3306)")]
    [DreamServiceConfig("db-catalog", "string", "Database table name")]
    [DreamServiceConfig("db-user", "string", "Database user name")]
    [DreamServiceConfig("db-password", "string", "Password for database user")]
    [DreamServiceConfig("db-options", "string?", "Connection string parameters (default: \"\")")]
    [DreamServiceConfig("db-tableprefix", "string?", "Prefix for table names (default: \"\")")]
    [DreamServiceBlueprint("deki/service-type", "authentication")]
    public class DrupalAuthenticationService : DekiAuthenticationService {

        //--- Fields ---
        private DataFactory _factory;
        private DataCatalog _catalog;
        private string _prefix;
       
        //--- Properties ---
        public override string AuthenticationRealm {
            get {
                return "Drupal";
            }
        }

        //--- Features ---
        protected override Yield Start(XDoc config, Result result) {
            yield return Coroutine.Invoke(base.Start, config, new Result());

            _factory = new DataFactory(MySql.Data.MySqlClient.MySqlClientFactory.Instance, "?");
            _catalog = new DataCatalog(_factory, config);
            _prefix = config["db-tableprefix"].AsText ?? string.Empty;
            
            try {
                _catalog.TestConnection();
            }
            catch(Exception) {
                throw new Exception(string.Format("Unable to connect to drupal instance with connectionString: {0}", _catalog.ConnectionString));
            }
            result.Return();
        }

        //--- Methods ---
        public override bool CheckUserPassword(string user, string password) {
            DataCommand cmd = _catalog.NewQuery(string.Format("SELECT count(*) FROM {2}users WHERE name='{0}' AND pass=md5('{1}') LIMIT 1", DataCommand.MakeSqlSafe(user), DataCommand.MakeSqlSafe(password), _prefix));
            long count = cmd.ReadAsLong() ?? 0;
            if(count > 0)
                return true;

            return false;
        }

        public override User GetUser(string user) {
            string name = "", email = "";
            Group group = new Group("anonymous user");
            DataCommand cmd = _catalog.NewQuery(string.Format("SELECT u.name, u.mail AS email, r.name AS group_name FROM {1}users u LEFT OUTER JOIN {1}users_roles ur ON u.uid=ur.uid LEFT OUTER JOIN {1}role r ON ur.rid=r.rid WHERE u.name='{0}' LIMIT 1", DataCommand.MakeSqlSafe(user), _prefix));
            XDoc result = cmd.ReadAsXDoc("users","user");
           
            if(result["user"].IsEmpty)
                return null;

            name = result["user/name"].AsText;
            email = result["user/email"].AsText;
            string groupName = result["user/group_name"].AsText;
            if(!string.IsNullOrEmpty(groupName))
                group = new Group(groupName);
           
            return new User(name, email, new Group[] { group });
        }

        public override Group[] GetGroups() {
            IList<Group> groupList = new List<Group>();
            DataCommand cmd = _catalog.NewQuery(string.Format("SELECT name AS role_name FROM {0}role", _prefix));
            XDoc roles = cmd.ReadAsXDoc("roles", "role");
            foreach(XDoc role in roles["role/role_name"])
                groupList.Add(new Group(role.AsText));
             
            Group[] groups = new Group[groupList.Count];
            groupList.CopyTo(groups,0);
            return groups;
        }

        public override Group GetGroup(string group) {
            DataCommand cmd = _catalog.NewQuery(string.Format("SELECT name AS role_name FROM {1}role WHERE name='{0}' LIMIT 1", DataCommand.MakeSqlSafe(group), _prefix));
            XDoc role = cmd.ReadAsXDoc("roles", "role");
            if(role["role/role_name"].IsEmpty)
                return null;

            return new Group(role["role/role_name"].AsText);
        }
    }
}

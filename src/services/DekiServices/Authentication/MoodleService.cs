/*
 * MindTouch Core - open source enterprise collaborative networking
 * Copyright (C) 2006-2008 MindTouch Inc.
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
using System.Data;

using MindTouch.Data;
using MindTouch.Dream;
using MindTouch.Tasking;
using MindTouch.Xml;

namespace MindTouch.Deki.Services {
    using Yield = IEnumerator<IYield>;

    [DreamService("MindTouch Moodle Authentication Service", "Copyright (c) 2009-2010 MindTouch Inc.",
        Info = "http://developer.mindtouch.com/App_Catalog/Moodle",
        SID = new string[] { 
            "sid://mindtouch.com/2009/02/moodle-authentication" 
        }
    )]
    [DreamServiceConfig("db-server", "string?", "Database host name (default: localhost).")]
    [DreamServiceConfig("db-port", "int?", "Database port (default: 3306)")]
    [DreamServiceConfig("db-catalog", "string", "Database table name")]
    [DreamServiceConfig("db-user", "string", "Database user name")]
    [DreamServiceConfig("db-password", "string", "Password for database user")]
    [DreamServiceConfig("db-options", "string?", "Connection string parameters (default: \"\")")]
    [DreamServiceConfig("db-tableprefix", "string?", "Prefix for table names. (default: no prefix)")]
    public class MoodleAuthenticationService : DekiAuthenticationService {

        //--- Fields ---
        private DataFactory _factory;
        private DataCatalog _catalog;
        private string _tablePrefix;
        private Group _defaultGroup = new Group("default");

        //--- Properties ---
        public override string AuthenticationRealm { get { return "Moodle"; } }

        //--- Methods ---
        protected override Yield Start(XDoc config, Result result) {
            yield return Coroutine.Invoke(base.Start, config, new Result());

            // read configuration settings
            _factory = new DataFactory(MySql.Data.MySqlClient.MySqlClientFactory.Instance, "?");
            _catalog = new DataCatalog(_factory, config);

            _tablePrefix = config["db-tableprefix"].AsText ?? string.Empty;
            
            // test database connection
            try {
                _catalog.TestConnection();
            } catch(Exception) {
                throw new Exception(string.Format("Unable to connect to Moodle instance with connection string: {0}", _catalog.ConnectionString));
            }
            result.Return();
        }

        public override bool CheckUserPassword(string user, string password) {
            DataCommand cmd = _catalog.NewQuery(string.Format("SELECT count(*) FROM {2}user WHERE {2}user.username = '{0}' AND {2}user.password = MD5('{1}')", DataCommand.MakeSqlSafe(user.ToLowerInvariant()), DataCommand.MakeSqlSafe(password), _tablePrefix));
            long count = cmd.ReadAsLong() ?? 0;
            return count > 0;
        }

        public override User GetUser(string user) {
            DataCommand user_cmd = _catalog.NewQuery(string.Format("SELECT {1}user.username FROM {1}user WHERE username = '{0}'", DataCommand.MakeSqlSafe(user.ToLowerInvariant()), _tablePrefix));
            string user_result = user_cmd.Read();
            if(user_result == null ) {
                return null;
            }
            List<Group> group_result = new List<Group>();
            _catalog.NewQuery(string.Format("SELECT {1}groups.name FROM {1}groups WHERE {1}groups.id IN (SELECT {1}groups_members.groupid FROM {1}groups_members WHERE {1}groups_members.userid = (SELECT {1}user.id FROM {1}user WHERE {1}user.username ='{0}'))", DataCommand.MakeSqlSafe(user.ToLowerInvariant()), _tablePrefix)).Execute(delegate(IDataReader reader) {
                while(reader.Read()) {
                    group_result.Add(new Group(SysUtil.ChangeType<string>(reader[0])));
                }
            });
            if (group_result.Count == 0)
            {
                return new User(user, string.Empty, new Group[] { _defaultGroup });
            } 
            return new User(user, string.Empty, group_result.ToArray());
        }

        public override Group GetGroup(string group) {
            string title = _catalog.NewQuery(string.Format("SELECT {1}groups.name FROM {1}groups WHERE {1}groups.name = '{0}'", DataCommand.MakeSqlSafe(group), _tablePrefix)).Read();
            if(title != null) {
                return new Group(title);
            }
            return null;
        }

        public override Group[] GetGroups() {
            List<Group> groups = new List<Group>();
            _catalog.NewQuery(string.Format("SELECT name FROM {0}groups", _tablePrefix)).Execute(delegate(IDataReader reader) {
                while(reader.Read()) {
                    groups.Add(new Group(SysUtil.ChangeType<string>(reader[0])));
                }
            });
            return groups.ToArray();
        }
    }
}

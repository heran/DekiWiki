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
using System.Data;

using MindTouch.Data;
using MindTouch.Dream;
using MindTouch.Tasking;
using MindTouch.Xml;

namespace MindTouch.Deki.Services {
    using Yield = IEnumerator<IYield>;

    [DreamService("MindTouch vBulletin Authentication Service", "Copyright (c) 2006-2010 MindTouch Inc.",
        Info = "http://doc.opengarden.org/Deki_API/Reference/vBulletinAuthentication",
        SID = new string[] { 
            "sid://mindtouch.com/2007/12/vbulletin-authentication",
            "http://services.mindtouch.com/deki/draft/2007/12/vbulletin-authentication" 
        }
    )]
    [DreamServiceConfig("db-server", "string?", "Database host name (default: localhost).")]
    [DreamServiceConfig("db-port", "int?", "Database port (default: 3306)")]
    [DreamServiceConfig("db-catalog", "string", "Database table name")]
    [DreamServiceConfig("db-user", "string", "Database user name")]
    [DreamServiceConfig("db-password", "string", "Password for database user")]
    [DreamServiceConfig("db-options", "string?", "Connection string parameters (default: \"\")")]
    [DreamServiceConfig("db-tableprefix", "string?", "Prefix for table names. (default: no prefix)")]
    public class VBulletinAuthenticationService : DekiAuthenticationService {

        //--- Fields ---
        private DataFactory _factory;
        private DataCatalog _catalog;
        private string _tablePrefix;

        //--- Properties ---
        public override string AuthenticationRealm { get { return "vBulletin"; } }

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
                throw new Exception(string.Format("Unable to connect to vBulletin instance with connection string: {0}", _catalog.ConnectionString));
            }
            result.Return();
        }

        public override bool CheckUserPassword(string user, string password) {
            DataCommand cmd = _catalog.NewQuery(string.Format("SELECT count(*) FROM {2}user LEFT JOIN {2}usergroup ON {2}user.usergroupid={2}usergroup.usergroupid WHERE {2}user.username='{0}' AND {2}user.password=MD5(CONCAT(MD5('{1}'), salt)) AND {2}usergroup.forumpermissions!=0", DataCommand.MakeSqlSafe(user.ToLowerInvariant()), DataCommand.MakeSqlSafe(password), _tablePrefix));
            long count = cmd.ReadAsLong() ?? 0;
            return count > 0;
        }

        public override User GetUser(string user) {
            DataCommand cmd = _catalog.NewQuery(string.Format("SELECT {1}usergroup.title FROM {1}user LEFT JOIN {1}usergroup ON {1}user.usergroupid={1}usergroup.usergroupid WHERE {1}user.username='{0}' AND {1}usergroup.forumpermissions!=0", DataCommand.MakeSqlSafe(user.ToLowerInvariant()), _tablePrefix));
            string title = cmd.Read();
            if(title == null) {
                return null;
            }
            return new User(user, string.Empty, new Group[] { new Group(title) });
        }

        public override Group GetGroup(string group) {
            string title = _catalog.NewQuery(string.Format("SELECT title FROM {1}usergroup WHERE {1}usergroup.title='{0}' AND {1}usergroup.forumpermissions!=0", DataCommand.MakeSqlSafe(group), _tablePrefix)).Read();
            if(title != null) {
                return new Group(title);
            }
            return null;
        }

        public override Group[] GetGroups() {
            List<Group> groups = new List<Group>();
            _catalog.NewQuery(string.Format("SELECT title FROM {0}usergroup WHERE {0}usergroup.forumpermissions!=0", _tablePrefix)).Execute(delegate(IDataReader reader) {
                while(reader.Read()) {
                    groups.Add(new Group(SysUtil.ChangeType<string>(reader[0])));
                }
            });
            return groups.ToArray();
        }
    }
}

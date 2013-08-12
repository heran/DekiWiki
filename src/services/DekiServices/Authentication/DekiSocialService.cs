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

    [DreamService("MindTouch Deki Social Authentication Service", "Copyright (c) 2006-2010 MindTouch Inc.",
       Info = "http://doc.opengarden.org/Deki_API/Reference/DrupalAuthentication",
        SID = new string[] { 
            "sid://mindtouch.com/2007/11/dekisocial-authentication",
            "http://services.mindtouch.com/deki/draft/2007/11/dekisocial-authentication" 
        }
    )]
    [DreamServiceConfig("db-server", "string?", "Database host name (default: localhost).")]
    [DreamServiceConfig("db-port", "int?", "Database port (default: 3306)")]
    [DreamServiceConfig("db-catalog", "string", "Database table name")]
    [DreamServiceConfig("db-user", "string", "Database user name")]
    [DreamServiceConfig("db-password", "string", "Password for database user")]
    [DreamServiceConfig("db-options", "string?", "Connection string parameters (default: \"\")")]
    [DreamServiceConfig("openid-suffix", "string?", "OpenID domain suffix for internal accounts (default: \"\")")]
    [DreamServiceBlueprint("deki/service-type", "authentication")]
    public class DekiSocialAuthenticationService : DekiAuthenticationService {

        //--- Fiels ---
        private DataFactory _factory;
        private DataCatalog _catalog;
        private string _domain;

        //--- Properties ---
        public override string AuthenticationRealm { get { return "DekiSocial"; } }

        //--- Methods ---
        protected override Yield Start(XDoc config, Result result) {
            yield return Coroutine.Invoke(base.Start, config, new Result());

            // read configuration settings
            _factory = new DataFactory(MySql.Data.MySqlClient.MySqlClientFactory.Instance, "?");
            _catalog = new DataCatalog(_factory, config);
            _domain = (config["openid-suffix"].AsText ?? string.Empty).ToLowerInvariant();

            // test database connection
            try {
                _catalog.TestConnection();
            } catch(Exception) {
                throw new Exception(string.Format("Unable to connect to Deki Social instance with connectionString: {0}", _catalog.ConnectionString));
            }
            result.Return();
        }

        public override bool CheckUserPassword(string user, string password) {
            DataCommand cmd = _catalog.NewQuery(string.Format("SELECT COUNT(*) FROM openIDToUser LEFT JOIN users ON openIDToUser.userID = users.userID WHERE ((users.internal = 'Y' AND openIDToUser.openID = CONCAT('{0}', '{2}') AND users.password = MD5(CONCAT('{1}', 'WL758ek0', salt))) OR (users.internal = 'N' AND  openIDToUser.openID = '{0}' AND users.password = MD5('{1}'))) AND users.active = 'Y' LIMIT 1", DataCommand.MakeSqlSafe(user.ToLowerInvariant()), DataCommand.MakeSqlSafe(password), DataCommand.MakeSqlSafe(_domain)));
            long count = cmd.ReadAsLong() ?? 0;
            if(count > 0)
                return true;

            return false;
        }

        public override User GetUser(string user) {
            User u = null;
            _catalog.NewQuery(
@"SELECT users.userid, openIDToUser.openid, users.email 
FROM openIDToUser 
LEFT JOIN users 
    ON openIDToUser.userID = users.userID 
WHERE ( ( users.internal = 'Y' AND openIDToUser.openID = CONCAT(?USERNAME, ?DOMAIN)) 
OR (users.internal = 'N' AND openIDToUser.openID = ?USERNAME) ) 
AND users.active = 'Y' 
LIMIT 1")
            .With("USERNAME", user)
            .With("DOMAIN", _domain)
            .Execute(delegate(IDataReader dr) {
                if (dr.Read()) {
                    u = new User(user, dr["email"] as string, new Group[] { DefaultGroup });
                }
            });
            return u;
        }
    }
}

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
using System.Security.Cryptography;

using MindTouch.Data;
using MindTouch.Dream;
using MindTouch.Tasking;
using MindTouch.Xml;

namespace MindTouch.Deki.Services {
    using Yield = IEnumerator<IYield>;

    [DreamService("MindTouch Wordpress Authentication Service", "Copyright (c) 2006-2010 MindTouch Inc.",
       Info = "http://doc.opengarden.org/Deki_Wiki/Authentication/WordPressAuthentication",
        SID = new string[] { 
            "sid://mindtouch.com/2007/07/wordpress-authentication",
            "http://services.mindtouch.com/deki/draft/2007/07/wordpress-authentication", 
        }
    )]
    [DreamServiceConfig("db-server", "string?", "Database host name (default: localhost).")]
    [DreamServiceConfig("db-port", "int?", "Database port (default: 3306)")]
    [DreamServiceConfig("db-catalog", "string", "Database name")]
    [DreamServiceConfig("db-user", "string", "Database user name")]
    [DreamServiceConfig("db-password", "string", "Password for database user")]
    [DreamServiceConfig("db-options", "string?", "Connection string parameters (default: \"\")")]
    [DreamServiceConfig("db-tableprefix", "string?", "Prefix for table names (default: \"wp_\")")]
    [DreamServiceBlueprint("deki/service-type", "authentication")]
    public class WordPressAuthenticationService : DekiAuthenticationService {

        //--- Fields ---
        private DataFactory _factory;
        private DataCatalog _catalog;
        private Group _defaultGroup = new Group("default");
        private string _prefix;
        string itoa64 = "./0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";

        //--- Properties ---
        public override string AuthenticationRealm {
            get {
                return "Wordpress";
            }
        }

        protected override Yield Start(XDoc config, Result result) {
            yield return Coroutine.Invoke(base.Start, config, new Result());

            _factory = new DataFactory(MySql.Data.MySqlClient.MySqlClientFactory.Instance, "?");
            _catalog = new DataCatalog(_factory, config);
            _prefix = config["db-tableprefix"].AsText ?? "wp_";

            try {
                _catalog.TestConnection();
            } catch(Exception) {
                throw new Exception(string.Format("Unable to connect to wordpress instance with connectionString: {0}", _catalog.ConnectionString));
            }
            result.Return();
        }

        public override bool CheckUserPassword(string user, string password) {
            DataCommand cmd = _catalog.NewQuery(string.Format("SELECT user_pass FROM {1}users WHERE user_login='{0}' AND user_status=0 LIMIT 1", DataCommand.MakeSqlSafe(user), _prefix));
            XDoc result = cmd.ReadAsXDoc("users", "user");
            if(result["user"].IsEmpty)
                return false;

            string hashed = result["user/user_pass"].AsText;
            return CheckPassword(password, hashed);
        }

        public override User GetUser(string user) {
            string name = "", email = "";
            DataCommand cmd = _catalog.NewQuery(string.Format("SELECT user_login, user_email FROM {1}users WHERE user_login='{0}' AND user_status=0 LIMIT 1", DataCommand.MakeSqlSafe(user), _prefix));
            XDoc result = cmd.ReadAsXDoc("users", "user");

            if(result["user"].IsEmpty)
                return null;

            name = result["user/user_login"].AsText;
            email = result["user/user_email"].AsText;

            // TODO, group should be determined by deserializing the PHP value in wp_usermeta meta_key='wp_capabilities'

            return new User(name, email, new Group[] { _defaultGroup });
        }

        public override Group[] GetGroups() {
            return new Group[] {_defaultGroup };
        }

        public override Group GetGroup(string group) {
            return _defaultGroup;
        }

        private bool CheckPassword(string password, string hash) {
            // wordpress >= 2.5 uses new hashing algorithm so check old (md5) and new (PHPass) hash
            return CheckMD5HashedPassword(password, hash) || CheckPHPassPassword(password, hash);
        }

        private bool CheckMD5HashedPassword(string password, string hash) {
            return hash == StringUtil.ComputeHashString(password, Encoding.UTF8);
        }
        
        // port of PHPass crypt_private function (http://www.openwall.com/phpass/)
        private bool CheckPHPassPassword(string password, string hash) {
            MD5 md5 = MD5.Create();
            Encoding encoding = Encoding.UTF8;

            int count_log2 = itoa64.IndexOf(hash[3]);
            int count = 1 << count_log2;
            string salt = hash.Substring(4, 8);
            if(salt.Length != 8)
                return false;

            byte[] tmpHash = md5.ComputeHash(encoding.GetBytes(salt + password));
            byte[] password_bytes = encoding.GetBytes(password);
            do {
                tmpHash = md5.ComputeHash(ArrayUtil.Concat<byte>(tmpHash, password_bytes));
            } while(--count > 0);
            string output = hash.Substring(0,12);
            output += encode64(tmpHash, 16);
            return hash == output;
        }

        // port of PHPass encode64 function
        private string encode64(byte[] input, int count) {
            string output = string.Empty;
            int i = 0;
            do {
                int value = (int)(input[i++]);
                output += itoa64[value & 0x3f];
                if(i < count)
                    value |= (int)(input[i]) << 8;
                output += itoa64[(value >> 6) & 0x3f];
                if(i++ >= count)
                    break;
                if(i < count)
                    value |= (int)(input[i]) << 16;
                output += itoa64[(value >> 12) & 0x3f];
                if(i++ >= count)
                    break;
                output += itoa64[(value >> 18) & 0x3f];
            } while (i < count);
            return output;
        }
    }
}

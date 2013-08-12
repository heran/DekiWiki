/*
 * MindTouch DekiWiki - a commercial grade open source wiki
 * Copyright (C) 2006 MindTouch, Inc.
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

namespace MindTouch.Deki {
    public abstract class DekiWikiServiceBase : DreamService {

        //--- Class Fields ---
        private static log4net.ILog _log = LogUtils.CreateLog<DekiWikiServiceBase>();

        //--- Fields ---
        private DekiConfig _dekiConfig = new DekiConfig();

        //--- Properties ---
        public override string AuthenticationRealm { get { return "DekiWiki"; } }
        protected internal DekiConfig DekiConfig { get { return _dekiConfig; } }

        //--- Methods ---
        public override void Start(XDoc config) {
            base.Start(config);
            ReadWikiSettings(config);
            _dekiConfig.DbServer = config["deki-dbserver"].AsText ?? "localhost";
            _dekiConfig.DbPort = config["deki-dbport"].AsText ?? "3306";
            _dekiConfig.DbName = config["deki-dbname"].AsText ?? "wikidb";
            _dekiConfig.DbUser = config["deki-dbuser"].AsText ?? "wikiuser";
            _dekiConfig.DbPassword = config["deki-dbpassword"].Contents;
            _dekiConfig.ProxyKey = config["deki-proxykey"].Contents;
            _dekiConfig.IP = config["deki-path"].AsText ?? "/var/www/mks-wiki";
            _dekiConfig.SiteName = config["deki-sitename"].AsText ?? Language.Text("sitename");
            _dekiConfig.AdminDbUser = config["admin-db-user"].AsText ?? "root";
            _dekiConfig.AdminDbPassword = config["admin-db-password"].Contents;

            DatabaseAccess.DatabaseObject.SetConnectionSettings(
                "MySql.Data, Version=1.0.7.30072, Culture=neutral, PublicKeyToken=c5687fc88969c44d",
                "MySql.Data.MySqlClient.MySqlConnection",
                "MySql.Data.MySqlClient.MySqlDataAdapter",
                "MySql.Data.MySqlClient.MySqlParameter",
                "MySql.Data.MySqlClient.MySqlCommand",
                "?"
            );
        }

        protected user Authenticate(DreamContext context, DreamMessage request, DekiUserLevel level) {
            user result = null;

            // get username and password
            string user;
            string password;
            if (!DreamUtil.GetAuthentication(context, request, out user, out password)) {

                // anonymous access is always granted
                if (level == DekiUserLevel.Anonymous) {

                    // TODO (steveb): missing code
                    throw new NotImplementedException("return anonymous user");
                } else {
                    throw new DreamAbortException(DreamMessage.AccessDenied(AuthenticationRealm, "authentication failed"));
                }
            }

            // validate username and password
            result = MindTouch.Deki.user.GetUserByName(user);
            if (result == null) {
                throw new DreamAbortException(DreamMessage.AccessDenied(AuthenticationRealm, "authentication failed"));
            }
            if (!result.checkPassword(password)) {
                throw new DreamAbortException(DreamMessage.AccessDenied(AuthenticationRealm, "authentication failed"));
            }
            if ((level == DekiUserLevel.Admin) && !result.isSysop()) {
                throw new DreamAbortException(DreamMessage.AccessDenied(AuthenticationRealm, "authentication failed"));
            }
            return result;
        }
        
        protected page Authorize(DreamContext context, user user, DekiAccessLevel access, string pageIdName) {
            page result;
            long id = context.Uri.GetParam<long>(pageIdName, 0);
            if (id == 0) {
                string title = context.Uri.GetParam("title", "");
                if (string.IsNullOrEmpty(title)) {
                    throw new DreamAbortException(DreamMessage.NotFound(""));
                }

                result = page.GetCurByTitle(title);
            } else {
                result = page.GetCurByID((ulong)id);
            }

            // check that page was found
            if ((result == null) || (result.ID <= 0)) {
                throw new DreamAbortException(DreamMessage.NotFound(""));
            }

            // check if action is allowed
            string action;
            switch (access) {
            case DekiAccessLevel.Read:
                action = DekiWikiConstants.ACTION_READ;
                break;
            case DekiAccessLevel.Write:
                action = DekiWikiConstants.ACTION_EDIT;
                break;
            case DekiAccessLevel.Destroy:
                action = DekiWikiConstants.ACTION_DELETE;
                break;
            default:
                throw new DreamAbortException(DreamMessage.BadRequest(string.Format("unknown action {0}", access)));
            }
            if (!result.userCan(action, user)) {
                throw new DreamAbortException(DreamMessage.AccessDenied(DekiWikiService.AUTHREALM, ""));
            }

            // return page
            return result;
        }

        private static void ReadWikiSettings(XDoc wikiInit) {
            string dbName = string.Empty, user = string.Empty, pwd = string.Empty, proxyKey = string.Empty, server = string.Empty;
            string serverPort = string.Empty, sitename = string.Empty, adminDbUser = string.Empty, adminDbPassword = string.Empty;

            string baseDir = string.Empty, fullName = string.Empty;
            const string fileName = "LocalSettings.php";
            const string adminFileName = "AdminSettings.php";

            // BUGBUGBUG (steveb): why is this hard coded?
            baseDir = wikiInit["deki-path"].AsText ?? (Environment.OSVersion.Platform != PlatformID.Unix ? @"C:\wiki\root" : @"/var/www/mks-wiki");

            fullName = System.IO.Path.Combine(baseDir, fileName);
            if (!System.IO.File.Exists(fullName)) {
                LogUtils.LogWarning(_log, "ReadWikiSettings: File not found", fullName);
                return;
            }
            foreach (string line in DreamUtil.ReadAllLines(fullName, System.Text.Encoding.Default)) {
                if (line.StartsWith("$IP"))
                    baseDir = GetValueFromConfigLine(line);
                if (line.StartsWith("$wgDBname"))
                    dbName = GetValueFromConfigLine(line);
                if (line.StartsWith("$wgDBuser"))
                    user = GetValueFromConfigLine(line);
                if (line.StartsWith("$wgDBpassword"))
                    pwd = GetValueFromConfigLine(line);
                if (line.StartsWith("$wgProxyKey"))
                    proxyKey = GetValueFromConfigLine(line);
                if (line.StartsWith("$wgDBserver"))
                    server = GetValueFromConfigLine(line);
                if (line.StartsWith("$wgSitename"))
                    sitename = GetValueFromConfigLine(line);
            }
            string fullAdminFileName = System.IO.Path.Combine(baseDir, adminFileName);
            if (System.IO.File.Exists(fullAdminFileName)) {
                foreach (string line in DreamUtil.ReadAllLines(fullAdminFileName, System.Text.Encoding.Default)) {
                    if (line.StartsWith("$wgDBadminuser"))
                        adminDbUser = GetValueFromConfigLine(line);
                    if (line.StartsWith("$wgDBadminpassword"))
                        adminDbPassword = GetValueFromConfigLine(line);
                }
            }
            if (wikiInit["deki-path"].IsEmpty)
                wikiInit.Start("deki-path").Value(baseDir).End();
            else
                wikiInit["deki-path"].Value(baseDir);
            wikiInit
                .Start("deki-dbserver").Value(server).End()
                .Start("deki-dbport").Value(serverPort).End()
                .Start("deki-dbname").Value(dbName).End()
                .Start("deki-dbuser").Value(user).End()
                .Start("deki-dbpassword").Attr("hidden", "true").Value(pwd).End()
                .Start("deki-proxykey").Value(proxyKey).End()
                .Start("deki-sitename").Value(sitename).End()

                .Start("admin-db-user").Attr("hidden", "true").Value(adminDbUser).End()
                .Start("admin-db-password").Attr("hidden", "true").Value(adminDbPassword).End()
                ;
        }

        private static string GetValueFromConfigLine(string line) {
            string[] keyValue = line.Split(new char[] { '=' }, 2);
            if (keyValue.Length != 2)
                return string.Empty;
            return keyValue[1].Trim('"', ';', ' ').Replace("\\\\", "\\").Replace("\\'", "'").Replace("\\\"", "\"");
        }
    }
}

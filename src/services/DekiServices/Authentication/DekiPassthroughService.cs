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

using MindTouch.Dream;
using MindTouch.Tasking;
using MindTouch.Xml;

namespace MindTouch.Deki.Services.Authentication {
    using Yield = IEnumerator<IYield>;

    [DreamService("MindTouch Remote Site Authentication Service", "Copyright (c) 2006-2010 MindTouch Inc.",
      Info = "http://developer.mindtouch.com/App_Catalog/RemoteDekiAuthentication",
        SID = new string[] { 
            "sid://mindtouch.com/2007/12/remote-deki-authentication",
            "http://services.mindtouch.com/deki/draft/2007/12/remote-deki-authentication" 
        }
    )]
    [DreamServiceConfig("deki-master-uri", "string", "The URI to the master deki-api. For example: http://some.hostname/@api/deki or http://192.168.0.10:8081/deki")]
    [DreamServiceConfig("timeout", "int?", "Timeout in seconds for requests to deki-master-uri")]
    [DreamServiceBlueprint("deki/service-type", "authentication")]
    public class DekiPassthroughService : DekiAuthenticationService {
        
        //--- Constants ---
        const string REMOTEDEKIUSERS = "remotedekiusers";

        //--- Fields ---
        private Plug _dekiPlug = null;

        public override bool CheckUserPassword(string user, string password) {
            DreamMessage response = _dekiPlug.At("users", "authenticate").WithCredentials(user, password).GetAsync().Wait();
            if (response.IsSuccessful) {
                return true;
            } else {
                if (response.Status == DreamStatus.Unauthorized)
                    return false;
                else
                    throw new DreamAbortException(response);
            }
        }

        public override Group GetGroup(string group) {
            Group g = null;
            DreamMessage response = _dekiPlug.At("groups", "=" + XUri.Encode(group)).GetAsync().Wait();
            if (response.IsSuccessful) {
                XDoc groupDoc = response.ToDocument();
                string groupName = groupDoc["groupname"].AsText;
                if (!string.IsNullOrEmpty(groupName))
                    g = new Group(groupName);
            } else {
                throw new DreamAbortException(response);
            }

            return g;
        }

        public override Group[] GetGroups() {
            List<Group> groups = new List<Group>();

            DreamMessage response = _dekiPlug.At("groups").GetAsync().Wait();
            if (response.IsSuccessful) {

                groups.Add(new Group(REMOTEDEKIUSERS));
                foreach (XDoc groupDoc in response.ToDocument()["group"]) {
                    string groupName = groupDoc["groupname"].AsText;
                    if (!string.IsNullOrEmpty(groupName))
                        groups.Add(new Group(groupName));
                }
            } else {
                throw new DreamAbortException(response);
            }

            return groups.ToArray();
        }

        public override User GetUser(string user) {
            User u = null;
            DreamMessage response = _dekiPlug.At("users", "=" + XUri.Encode(user)).GetAsync().Wait();

            if (response.IsSuccessful) {
                XDoc userDoc = response.ToDocument();
                string email = userDoc["email"].AsText;
                List<Group> groups = new List<Group>();

                groups.Add(new Group(REMOTEDEKIUSERS));
                foreach (XDoc groupDoc in userDoc["groups/group"]) {
                    string groupName = groupDoc["groupname"].AsText;
                    if (!string.IsNullOrEmpty(groupName))
                        groups.Add(new Group(groupName));
                }

                u = new User(user, email, groups.ToArray());
            } else {
                throw new DreamAbortException(response);
            }


            return u;
        }

        protected override Yield Start(XDoc config, Result result) {
            yield return Coroutine.Invoke(base.Start, config, new Result());

            //This assumes a uri like http://hostname/@api/deki
            XUri uri = config["deki-master-uri"].AsUri;
            //TODO: Consider supporting a uri like http://hostname

            if (uri == null) {
                throw new ArgumentNullException("deki-master-uri");
            }

            _dekiPlug = Plug.New(uri, TimeSpan.FromSeconds(config["timeout"].AsDouble ?? Plug.DEFAULT_TIMEOUT.TotalSeconds));
            result.Return();
        }
    }
}

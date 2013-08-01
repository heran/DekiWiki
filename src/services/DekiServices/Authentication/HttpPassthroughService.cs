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
using MindTouch.Web;
using MindTouch.Xml;

namespace MindTouch.Deki.Services {
    using Yield = IEnumerator<IYield>;

    [DreamService("MindTouch HTTP Authentication Service", "Copyright (c) 2006-2010 MindTouch Inc.",
       Info = "http://developer.mindtouch.com/Deki/Authentication/HttpAuthentication",
        SID = new string[] { 
            "sid://mindtouch.com/2007/07/http-authentication",
            "http://services.mindtouch.com/deki/draft/2007/07/http-authentication" 
        }
    )]
    [DreamServiceConfig("authentication-uri", "string", "The full URL that is setup to authenticate requests")]
    [DreamServiceConfig("timeout", "int?", "Timeout in seconds for request to the authentication-url")]
    [DreamServiceBlueprint("deki/service-type", "authentication")]
    public class HttpPassthroughService : DekiAuthenticationService {

        //--- Constants ---
        private const string DEFGROUP = "default";

        //--- Fields ---
        private XUri _authUri = null;
        private int _timeoutSecs = 10;

        //--- Methods ---
        public override bool CheckUserPassword(string user, string password) {
            DreamMessage response = null;
            Plug p = Plug.New(_authUri, TimeSpan.FromSeconds(_timeoutSecs)).WithCredentials(user, password);
            try {
                response = p.Get();
            }
            catch (DreamResponseException x) {
                throw new DreamAbortException(x.Response);
            }
            if (response == null)
                return false;
            else
                return response.Status == DreamStatus.Ok;
        }


        public override User GetUser(string user) {
            DreamMessage request = DreamContext.Current.Request;
            string loginUser, password;
            HttpUtil.GetAuthentication(DreamContext.Current.Uri, request.Headers, out loginUser, out password);

            if (loginUser == user)
                return new User(user, string.Empty, new Group[] { new Group(DEFGROUP) });
            else
                throw new DreamAbortException(DreamMessage.NotImplemented("HTTPPassThroughService will only return info for a user matching the given credentials. (Manually adding users via user management screen unsupported)"));
        }

        protected override Yield Start(XDoc config, Result result) {
            yield return Coroutine.Invoke(base.Start, config, new Result());
            _timeoutSecs = config["timeout"].AsInt ?? _timeoutSecs;
            _authUri = config["authentication-uri"].AsUri;
            if(_authUri == null) {
                throw new ArgumentNullException("authentication-uri");
            }
            result.Return();
        }
    }
}

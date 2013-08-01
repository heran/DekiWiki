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

using MindTouch.Dream;

namespace MindTouch.Deki.Data.MySql {
    public partial class MySqlDekiDataSession {

        //--- Class Methods ---
        public void RequestLog_Insert(XUri requestUri, string requestVerb, string requestHostHeader, string origin, string serviceHost, string serviceFeature, DreamStatus responseStatus, string username, uint executionTime, string response) {
            string host = requestUri.HostPort;
            if( !host.Contains(":"))
                host = host + ":80";

            //Schema for request log in "trunk/product/deki/web/maintenance/apirequestlog.sql"
            Catalog.NewQuery(@" /* RequestLog_Insert */
insert delayed into requestlog (
 	`rl_requesthost`, `rl_requesthostheader`, `rl_requestpath`, `rl_requestparams`, `rl_requestverb`, 
 	`rl_dekiuser`, `rl_origin`, `rl_servicehost`, `rl_servicefeature`, `rl_responsestatus`, `rl_executiontime`, 
 	`rl_response`
 ) values (
 	?REQUESTHOST, ?REQUESTHOSTHEADER, ?REQUESTPATH, ?REQUESTPARAMS, ?REQUESTVERB, 
 	?DEKIUSER, ?ORIGIN, ?SERVICEHOST, ?SERVICEFEATURE, ?RESPONSESTATUS, ?EXECUTIONTIME, 
 	?RESPONSE
 );")
                .With("REQUESTHOST", host)
                .With("REQUESTHOSTHEADER", requestHostHeader)
                .With("REQUESTPATH", requestUri.Path)
                .With("REQUESTPARAMS", requestUri.Query)
                .With("REQUESTVERB", requestVerb)
                .With("DEKIUSER", username)
                .With("ORIGIN", origin == null ? String.Empty : origin.ToLowerInvariant())
                .With("SERVICEHOST", serviceHost)
                .With("SERVICEFEATURE", serviceFeature)
                .With("RESPONSESTATUS", (int)responseStatus)
                .With("EXECUTIONTIME", executionTime)
                .With("RESPONSE", response)
                .Execute();
        }
    }
}

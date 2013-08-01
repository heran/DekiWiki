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
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Authentication;
using MindTouch.Deki.Script.Runtime.Library;
using MindTouch.Dream;
using MindTouch.Tasking;
using MindTouch.Xml;

namespace MindTouch.Deki.Services.Extension {
    using Yield = IEnumerator<IYield>;

    [DreamService("YouTrack Extension", "Copyright (c) 2006-2010 MindTouch Inc.",
        Info = "http://developer.mindtouch.com/App_Catalog/YouTrack",
        SID = new[] { 
            "sid://mindtouch.com/2010/11/youtrack"
        }
    )]
    [DreamServiceConfig("server", "string", "URI to your YouTrack installation.")]
    [DreamServiceConfig("username", "string?", "A YouTrack username that has view access")]
    [DreamServiceConfig("password", "string?", "Password for the YouTrack account")]
    [DreamServiceBlueprint("deki/service-type", "extension")]
    [DekiExtLibrary(
        Label = "YouTrack",
        Namespace = "youtrack",
        Description = "This extension contains functions for integrating with JetBrain's YouTrack."
    )]
    public class YouTrackService : DekiExtService {

        //--- Constants ---
        private const string MISSING_FIELD_ERROR = "YouTrack extension: missing configuration setting";

        //--- Class Methods ---
        private static string ConvertDate(XDoc date) {
            long? epoch = date.AsLong;
            if(!epoch.HasValue) {
                return null;
            }
            var datetime = DateTimeUtil.FromEpoch((uint)(epoch.Value / 1000));
            return DekiScriptLibrary.CultureDateTime(datetime);
        }

        private static object ConvertValue(XDoc item) {
            string value = item.AsText;

            // check if we should return nil
            if(value == null) {
                return null;
            }

            // check if we should return a boolean
            bool boolValue;
            if(bool.TryParse(value, out boolValue)) {
                return boolValue;
            }

            // check if we should return a number
            double doubleValue;
            if(double.TryParse(value, out doubleValue)) {
                return doubleValue;
            }

            // return the value
            return value;
        }

        //--- Fields ---
        private string _username;
        private string _password;
        private Plug _server;

        //--- Functions ---
        [DekiExtFunction(Description = "Retrieve list of matching issues from YouTrack server.")]
        public ArrayList Issues(
            [DekiExtParam("query expression (default: all issues)", true)] string query,
            [DekiExtParam("number of items to skip (default: 0)", true)] int? offset,
            [DekiExtParam("maximum number of items to return (default: 100)", true)] int? limit,
            [DekiExtParam("get only issues updated after specified date.", true)] string since
        ) {
            Login();
            var request = _server.At("rest", "project", "issues");

            // check if a query was provided
            if(!string.IsNullOrEmpty(query)) {
                request = request.With("filter", query);
            }

            // check if a limit and offset were provided
            if(offset.HasValue) {
                request = request.With("after", offset.Value);
            }
            request = request.With("max", limit ?? 100);

            // check if a date limit was provide
            if(!string.IsNullOrEmpty(since)) {
                DreamContext context = DreamContext.CurrentOrNull;
                CultureInfo culture = (context == null) ? CultureInfo.CurrentCulture : context.Culture;
                double dummy;
                var date = DekiScriptLibrary.CultureDateTimeParse(since, culture, out dummy);
                request = request.With("updatedAfter", date.ToEpoch());
            }
            var doc = request.Get().ToDocument();

            var result = new ArrayList();
            foreach(var issue in doc["issue"]) {
                result.Add(ConvertIssue(issue));
            }
            return result;
        }

        [DekiExtFunction(Description = "Create a hyperlink to a YouTrack query.")]
        public XDoc QueryLink(
            [DekiExtParam("query expression to link to")] string query,
            [DekiExtParam("link contents; can be text, an image, or another document (default: link uri)", true)] object text,
            [DekiExtParam("link hover title (default: link title)", true)] string title,
            [DekiExtParam("link target (default: nil)", true)] string target
        ) {
            return DekiScriptLibrary.WebLink(_server.At("issues").With("q", query).ToString(), text, title, target);
        }

        [DekiExtFunction(Description = "Create a hyperlink to a YouTrack issue.")]
        public XDoc IssueLink(
            [DekiExtParam("YouTrack ID to link to (default: all issues)")] string id,
            [DekiExtParam("link contents; can be text, an image, or another document (default: link uri)", true)] object text,
            [DekiExtParam("link hover title (default: link title)", true)] string title,
            [DekiExtParam("link target (default: nil)", true)] string target
        ) {
            return DekiScriptLibrary.WebLink(_server.At("issue", id).ToString(), text, title, target);
        }

        //--- Methods ---
        protected override Yield Start(XDoc config, Result result) {
            yield return Coroutine.Invoke(base.Start, config, new Result());

            // read configuration settings
            _username = config["username"].AsText;
            _password = config["password"].AsText;
            var uri = config["server"].AsUri;
            if(uri == null) {
                throw new ArgumentException(MISSING_FIELD_ERROR, "server");
            }
            _server = Plug.New(uri);
            result.Return();
        }

        protected override Yield Stop(Result result) {

            // clear settings
            _server = null;
            _password = null;
            _username = null;
            yield return Coroutine.Invoke(base.Stop, new Result());
            result.Return();
        }

        private void Login() {
            if(!string.IsNullOrEmpty(_username) && !string.IsNullOrEmpty(_password)) {
                var result = _server.At("rest", "user", "login").With("login", _username).With("password", _password).PostAsForm(new Result<DreamMessage>()).Wait();
                if(!result.IsSuccessful) {
                    throw new AuthenticationException("invalid username/password combination");
                }
            }
        }

        private Hashtable ConvertIssue(XDoc issue) {
            var item = new Hashtable(StringComparer.OrdinalIgnoreCase);

            // TODO (steveb): non-translated data
            //<issue 
            //  affectsVersion="9.02 (Lyons)"
            //  fixedVersion="10.0.4" 
            //  projectShortName="MT" 
            //  fixedInBuild="Next build" 
            //  commentsCount="1" 
            //  numberInProject="7565" 
            //>
            //<links>
            //  <issueLink typeInward="is duplicated by" typeOutward="duplicates" typeName="Duplicate" target="MT-8859" source="MT-9121" />
            //  <issueLink typeInward="caused by" typeOutward="fix caused" typeName="Regression" target="MT-8859" source="MT-8506" />
            //</links>
            //<attachments>
            //  <fileUrl url="/_persistent/2010-10-04_1110.png?file=47-386&amp;v=0&amp;c=false" name="2010-10-04_1110.png" />
            //</attachments>

            item["priority"] = issue["@priority"].AsInt ?? 0;
            item["type"] = issue["@type"].AsText;
            var state = issue["@state"].AsText;
            item["state"] = state;
            item["subsystem"] = issue["@subsystem"].AsText;
            item["id"] = issue["@id"].AsText;
            item["assignee"] = issue["@assigneeName"].AsText;
            item["reporter"] = issue["@reporterName"].AsText;
            item["summary"] = issue["@summary"].AsText;
            item["description"] = issue["@description"].AsText;
            item["created"] = ConvertDate(issue["@created"]);
            item["updated"] = ConvertDate(issue["@updated"]);
            //item["historyUpdated"] = ConvertDate(issue["@historyUpdated"]);
            item["resolved"] = ConvertDate(issue["@resolved"]);
            item["votes"] = issue["@votes"].AsInt ?? 0;

            // add custom fields
            var fields = new Hashtable(StringComparer.OrdinalIgnoreCase);
            foreach(var field in issue["field"]) {
                var name = field["@name"].AsText ?? "N/A";
                var values = field["value"];
                if(values.ListLength > 1) {
                    var list = new ArrayList();
                    foreach(var value in values) {
                        list.Add(ConvertValue(value));
                    }
                    fields[name] = list;
                } else {
                    fields[name] = ConvertValue(values);
                }
            }
            item["fields"] = fields;
            return item;
        }
    }
}

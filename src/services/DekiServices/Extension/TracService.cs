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

using CookComputing.XmlRpc;

using MindTouch.Dream;
using MindTouch.Tasking;
using MindTouch.Xml;

namespace MindTouch.Deki.Services.Extension {
    using Yield = IEnumerator<IYield>;

    [DreamService("MindTouch Trac Bug Tracker Extension", "Copyright (c) 2006-2010 MindTouch Inc.",
        Info = "http://developer.mindtouch.com/App_Catalog/Trac",
        SID = new string[] { 
            "sid://mindtouch.com/2008/02/trac",
            "http://services.mindtouch.com/deki/draft/2008/02/trac" 
        }
    )]
    [DreamServiceConfig("trac-uri", "string", "URI to your Trac installation. This URI + /login/xmlrpc needs to exist. Be sure that the XMLRPC plugin is installed.")]
    [DreamServiceConfig("username", "string", "A Trac username that has view access")]
    [DreamServiceConfig("password", "string", "Password for the Trac account")]
    [DreamServiceBlueprint("deki/service-type", "extension")]
    [DekiExtLibrary(
        Label = "Trac",
        Namespace = "trac",
        Description = "This extension contains functions for integrating with Trac bug tracker."
    )]
    [DekiExtLibraryFiles(Prefix = "MindTouch.Deki.Services.Resources", Filenames = new string[] { 
        "trac.css", "sorttable.js"
    })]

    public class TracService : DekiExtService {

        //--- Constants ---
        private const string MISSING_FIELD_ERROR = "Trac extension: missing configuration setting";
        private static readonly string[] STRIKE_THROUGH_STATUSES = new string[] { "closed", "resolved" };

        //--- Fields ---
        string _username;
        string _password;
        XUri _uri;
        Trac _trac;

        //--- Functions ---
        [DekiExtFunction(Description = "Create a link with description to a bug.")]
        public XDoc Link(
            [DekiExtParam("bug id")] int id
        ) {
            object[] ticket = _trac.TicketGet(id);
            
            XDoc result = new XDoc("html");
            result.Start("body");
            BuildBugLink((XmlRpcStruct) ticket[3], result, id);
            result.End();
            return result;
        }

        [DekiExtFunction(Description = "Create a table listing of bugs for a given query (Example: status!=closed )")]
        public XDoc Table(
            [DekiExtParam("query")] string query,
            [DekiExtParam("count", true)] int? count,
            [DekiExtParam("skip", true)] int? skip
            ) {

            int[] ticketIds = _trac.TicketQuery(query);
            List<Signature> sigs = new List<Signature>();
            List<int> ids = new List<int>();
            for (int i = skip ?? 0; i < Math.Min(ticketIds.Length, (count ?? 100) + (skip ?? 0)); i++) {
                sigs.Add(new Signature("ticket.get", new object[] { ticketIds[i] }));
                ids.Add(ticketIds[i]);
            }

            List<XmlRpcStruct> tickets = new List<XmlRpcStruct>();
            foreach (object o in _trac.SystemMultiCall(sigs.ToArray())) {
                object temp = ((object[]) o)[0];
                tickets.Add((XmlRpcStruct) ((object[]) temp)[3]);
            }

            XDoc bugList = BuildBugListHTMLTable(tickets.ToArray(), ids.ToArray());
            XDoc ret = new XDoc("html");
            ret.Start("head")
                .Start("script").Attr("type", "text/javascript").Attr("src", Files.At("sorttable.js")).End()
                .Start("link").Attr("type", "text/css").Attr("rel", "stylesheet").Attr("href", Files.At("trac.css")).End()
                .Start("style").Attr("type", "text/css").Value(@".feedtable {
    border:1px solid #999;
    line-height:1.5em;
    overflow:hidden;
    width:100%;
}
.feedtable th {
    background-color:#ddd;
    border-bottom:1px solid #999;
    font-size:14px;
}
.feedtable tr {
    background-color:#FFFFFF;
}
.feedtable tr.feedroweven td {
    background-color:#ededed;
}").End()
            .End();
            ret.Start("body").Add(bugList).End();
            return ret;
        }

        [DekiExtFunction(Description = "Get the number of bugs returned by a query")]
        public int Count(
            [DekiExtParam("query")] string query
            ) {

            int[] ticketIds = _trac.TicketQuery(query);
            return ticketIds.Length;
        }

        //--- Methods ---
        private XDoc BuildBugLink(XmlRpcStruct bug, XDoc doc, int bugid) {
            string status = bug["status"].ToString();
            string reporter = bug["reporter"].ToString();

            // The severity-param is not submitted by trac if no severity-entry is defined
            string severity = string.Empty;
            if(bug["severity"] != null) {
                severity = bug["severity"].ToString();
            }

            string owner = bug["owner"].ToString();
            string summary = bug["summary"].ToString();

            bool strikeThrough = !string.IsNullOrEmpty(status) && Array.Exists<string>(STRIKE_THROUGH_STATUSES, delegate(string s) {
                return StringUtil.EqualsInvariantIgnoreCase(status.ToLowerInvariant(), s);
            });

            string title = string.Format("{0} (Reporter: {1} Owner: {2})",
                summary,
                reporter,
                owner);

            doc.Start("a").Attr("href", _uri.At("ticket", bugid.ToString())).Attr("title", title);
            if (strikeThrough) {
                doc.Start("del").Value(bugid).End();
            } else {
                doc.Value(bugid);
            }
            doc.End();
            return doc;
        }

        private XDoc BuildBugListHTMLTable(XmlRpcStruct[] bugs, int[] ticketIds) {
            XDoc ret = new XDoc("div").Attr("class", "DW-table Trac-table table");
            ret.Start("table").Attr("border", 0).Attr("cellspacing", 0).Attr("cellpadding", 0).Attr("class", "table feedtable sortable");

            // header
            ret.Start("tr")
                .Elem("th", "Bug#")
                .Elem("th", "Summary")
                .Elem("th", "Status")
                .Elem("th", "Severity")
                .Elem("th", "Opened By")
                .Elem("th", "Assigned To")

            .End();

            for(int i=0; i < bugs.Length; i++){
                XmlRpcStruct bug = bugs[i];
                string status = bug["status"].ToString();
                string reporter = bug["reporter"].ToString();

                // The severity-param is not submitted by trac if no severity-entry is defined
                string severity = string.Empty;
                if(bug["severity"] != null) {
                    severity = bug["severity"].ToString();
                }
                string owner = bug["owner"].ToString();
                string summary = bug["summary"].ToString();

                string trClass = string.Format("{0} {1}", (i % 2 == 0) ? "even" : "odd", status);
                ret.Start("tr").Attr("class", trClass);
                ret.Start("td");
                ret = BuildBugLink(bug, ret, ticketIds[i]);
                ret.End(); //td;
                ret.Elem("td", summary);
                ret.Start("td").Value(status).End();
                ret.Start("td").Value(severity).End();
                ret.Elem("td", reporter);
                ret.Elem("td", owner);

                ret.End();//tr
            }
            ret.End(); // table
            return ret;
        }

        protected override Yield Start(XDoc config, Result result) {
            yield return Coroutine.Invoke(base.Start, config, new Result());

            // read configuration settings
            _username = config["username"].AsText;
            if (string.IsNullOrEmpty(_username)) {
                throw new ArgumentException(MISSING_FIELD_ERROR, "username");
            }
            _password = config["password"].AsText;
            if (string.IsNullOrEmpty(_password)) {
                throw new ArgumentException(MISSING_FIELD_ERROR, "password");
            }
            _uri = config["trac-uri"].AsUri;
            if (_uri == null) {
                throw new ArgumentException(MISSING_FIELD_ERROR, "trac-uri");
            }

            // initialize web-service
            _trac = XmlRpcProxyGen.Create<Trac>();
            _trac.Url = _uri.At("login", "xmlrpc").ToString();
            if (!string.IsNullOrEmpty(_username)) {
                _trac.Credentials = new System.Net.NetworkCredential(_username, _password);
            }

            result.Return();
        }

        protected override Yield Stop(Result result) {

            // clear settings
            _password = null;
            _username = null;
            _trac = null;
            yield return Coroutine.Invoke(base.Stop, new Result());
            result.Return();
        }

        public interface Trac : IXmlRpcProxy {
            [XmlRpcMethod("ticket.status.getAll")]
            string[] TicketStatusGetAll();

            [XmlRpcMethod("ticket.get")]
            object[] TicketGet(int id);

            [XmlRpcMethod("ticket.query")]
            int[] TicketQuery(string qstr);

            [XmlRpcMethod("system.multicall")]
            object[] SystemMultiCall(Signature[] signatures);
        }

        public struct Signature {
            public Signature(string m, object[] p) {
                this.methodName = m;
                this.@params = p;
            }
            [XmlRpcMember("methodName")]
            public string methodName;

            [XmlRpcMember("params")]
            public object[] @params;
        }
    }
}

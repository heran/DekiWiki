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

using MindTouch.Deki.Services.JiraWebServices;
using MindTouch.Dream;
using MindTouch.Tasking;
using MindTouch.Xml;

namespace MindTouch.Deki.Services.Extension {
    using Yield = IEnumerator<IYield>;

    [DreamService("MindTouch Atlassian Jira Bug Tracker Extension", "Copyright (c) 2006-2010 MindTouch Inc.",
        Info = "http://developer.mindtouch.com/App_Catalog/Jira",
        SID = new string[] { 
            "sid://mindtouch.com/2008/02/jira",
            "http://services.mindtouch.com/deki/draft/2008/02/jira" 
        }
    )]
    [DreamServiceConfig("username", "string", "A Jira username that has view access to your projects of interest")]
    [DreamServiceConfig("password", "string", "Password for the jira account")]
    [DreamServiceConfig("jira-uri", "string", "URI to your Atlassian Jira installation. This URI + /rpc/soap/jirasoapservice-v2 needs to exist. Be sure that the RPC + SOAP plugin is enabled.")]
    [DreamServiceConfig("jira-session-timeout-mins", "int?", "Number of minutes that a Jira login token is valid. Default: 30. Refer to http://confluence.atlassian.com/display/JIRA/Changing+the+default+session+timeout")]
    [DreamServiceBlueprint("deki/service-type", "extension")]
    [DekiExtLibrary(
        Label = "Jira",
        Namespace = "jira",
        Description = "This extension contains functions for integrating with Atlassian Jira bug tracker."
    )]
    [DekiExtLibraryFiles(Prefix = "MindTouch.Deki.Services.Resources", Filenames = new string[] { 
        "Jira.css", "sorttable.js"
    })]

    public class JiraService : DekiExtService {

        //--- Constants ---
        private const string MISSING_FIELD_ERROR = "Jira extension: missing configuration setting";
        private static readonly string[] STRIKE_THROUGH_STATUSES = new string[] { "closed", "resolved" };
        private const int DEFAULT_LOGIN_TTL = 30;

        //--- Fields ---
        string _username;
        string _password;
        XUri _uri;
        Dictionary<string, RemoteStatus> _statuses = null;
        Dictionary<string, RemoteFilter> _filters = null;
        Dictionary<string, RemotePriority> _priorities = null;

        // http://docs.atlassian.com/software/jira/docs/api/rpc-jira-plugin/latest/com/atlassian/jira/rpc/soap/JiraSoapService.html
        JiraSoapServiceService _jira = null;
        string _jiraToken;
        DateTime _jiraTokenTimestamp = DateTime.MinValue;
        TimeSpan _jiraTokenDuration;

        //--- Functions ---
        [DekiExtFunction(Description = "Create a link with description to a bug.")]
        public XDoc Link(
            [DekiExtParam("bug id")] string id
        ) {
            InitializeService();
            InitializeStatuses();
            RemoteIssue issue = _jira.getIssue(_jiraToken, id);

            XDoc result = new XDoc("html");
            result.Start("body");
            BuildBugLink(issue, result);
            result.End();
            return result;
        }

        [DekiExtFunction(Description = "Create a table listing of bugs for a given filter")]
        public XDoc Table(
            [DekiExtParam("filter name")] string filter
            ) {

            InitializeService();
            InitializeStatuses();
            InitializeFilters();
            InitializePriorities();

            RemoteFilter f = RetrieveFilter(filter);
            RemoteIssue[] issues = _jira.getIssuesFromFilter(_jiraToken, f.id);

            XDoc bugList = BuildBugListHTMLTable(issues);
            XDoc ret = new XDoc("html");
            ret.Start("head")
                .Start("script").Attr("type", "text/javascript").Attr("src", Files.At("sorttable.js")).End()
                .Start("link").Attr("type", "text/css").Attr("rel", "stylesheet").Attr("href", Files.At("Jira.css")).End()
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

        [DekiExtFunction(Description = "Create a table listing of bugs for given search terms")]
        public XDoc TableSearch(
            [DekiExtParam("search query")] string query
            ) {

            InitializeService();
            InitializeStatuses();
            InitializeFilters();
            InitializePriorities();

            RemoteIssue[] issues = _jira.getIssuesFromTextSearch(_jiraToken, query);

            XDoc bugList = BuildBugListHTMLTable(issues);
            XDoc ret = new XDoc("html");
            ret.Start("head")
                .Start("script").Attr("type", "text/javascript").Attr("src", Files.At("sorttable.js")).End()
                .Start("link").Attr("type", "text/css").Attr("rel", "stylesheet").Attr("href", Files.At("Jira.css")).End()
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

        [DekiExtFunction(Description = "Get the number of bugs returned by a filter")]
        public int Count(
            [DekiExtParam("filter name")] string filter
            ) {

            InitializeService();
            InitializeFilters();

            RemoteFilter f = RetrieveFilter(filter);
            return (int) _jira.getIssueCountForFilter(_jiraToken, f.id);
        }

        //--- Methods ---
        private XDoc BuildBugLink(RemoteIssue bug, XDoc doc) {

            RemoteStatus status = null;
            if (!string.IsNullOrEmpty(bug.status)) {
                _statuses.TryGetValue(bug.status, out status);
            }

            bool strikeThrough = status != null && Array.Exists<string>(STRIKE_THROUGH_STATUSES, delegate(string s) {
                return StringUtil.EqualsInvariantIgnoreCase(status.name.ToLowerInvariant(), s);
            });

            string title = string.Format("{0} (Reporter: {1} Assigned To: {2})",
                bug.summary,
                bug.reporter,
                bug.assignee);

            doc.Start("a").Attr("href", _uri.At("browse").At(bug.key)).Attr("title", title);
            if (strikeThrough) {
                doc.Start("del").Value(bug.key).End();
            } else {
                doc.Value(bug.key);
            }
            doc.End();
            return doc;
        }

        private XDoc BuildBugListHTMLTable(RemoteIssue[] issues) {
            XDoc ret = new XDoc("div").Attr("class", "DW-table Jira-table table");
            ret.Start("table").Attr("border", 0).Attr("cellspacing", 0).Attr("cellpadding", 0).Attr("class", "table feedtable sortable");

            // header
            ret.Start("tr")
                .Elem("th", "Bug#")
                .Elem("th", "Summary")
                .Elem("th", "Status")
                .Elem("th", "Priority")
                .Elem("th", "Opened By")
                .Elem("th", "Assigned To")

            .End();


            int count = 0;
            foreach (RemoteIssue bug in issues) {
                count++;
                RemoteStatus status = null;
                if (!string.IsNullOrEmpty(bug.status)) {
                    _statuses.TryGetValue(bug.status, out status);
                }

                RemotePriority priority = null;
                if (!string.IsNullOrEmpty(bug.priority)) {
                    _priorities.TryGetValue(bug.priority, out priority);
                }

                string trClass = string.Format("{0} {1}", (count % 2 == 0) ? "even" : "odd", status == null ? string.Empty : status.name);
                ret.Start("tr").Attr("class", trClass);
                ret.Start("td");
                ret = BuildBugLink(bug, ret);
                ret.End(); //td;
                ret.Elem("td", bug.summary);
                if (status == null) {
                    ret.Elem("td", "");
                } else {
                    ret.Start("td").Start("img").Attr("src", status.icon).Attr("alt", status.name).Attr("title", status.description).End().Value(status.name).End();
                }

                if (priority == null) {
                    ret.Elem("td");
                } else {
                    ret.Start("td").Start("img").Attr("src", priority.icon).Attr("alt", priority.name).Attr("title", priority.description).End().Value(priority.name).End();
                }

                ret.Elem("td", bug.reporter ?? string.Empty);
                ret.Elem("td", bug.assignee ?? string.Empty);

                ret.End();//tr
            }
            ret.End(); // table
            return ret;
        }

        private RemoteFilter RetrieveFilter(string filterName) {
            RemoteFilter f = null;
            _filters.TryGetValue(filterName.ToLowerInvariant(), out f);
            if (f == null) {

                //If a filter wasn't found, try refetching them from the server while ignoring cache
                InitializeFilters(true);
                _filters.TryGetValue(filterName.ToLowerInvariant(), out f);
                if (f == null) {
                    throw new ArgumentException(string.Format("Unable to find filter '{0}'", filterName), "filterName");
                }
            }

            return f;
        }

        private void InitializeService() {
            if (string.IsNullOrEmpty(_jiraToken) || new TimeSpan(DateTime.Now.Ticks - _jiraTokenTimestamp.Ticks) > _jiraTokenDuration) {
                _jiraToken = _jira.login(_username, _password);
                _jiraTokenTimestamp = DateTime.Now;
            }
        }

        private void InitializeStatuses() {

            //TODO Caching.
            if (_statuses == null) {
                _statuses = new Dictionary<string, RemoteStatus>();
                foreach (RemoteStatus s in _jira.getStatuses(_jiraToken)) {
                    _statuses[s.id] = s;
                }
            }
        }

        private void InitializeFilters() {
            InitializeFilters(false);
        }

        private void InitializeFilters(bool ignoreCache) {
            //TODO caching.
            if (_filters == null || ignoreCache) {
                _filters = new Dictionary<string, RemoteFilter>();
                foreach (RemoteFilter f in _jira.getSavedFilters(_jiraToken)) {
                    _filters[f.name.ToLowerInvariant()] = f;
                }
            }
        }

        private void InitializePriorities() {
            //TODO caching.
            if (_priorities == null) {
                _priorities = new Dictionary<string, RemotePriority>();
                foreach (RemotePriority p in _jira.getPriorities(_jiraToken)) {
                    _priorities[p.id] = p;
                }
            }
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
            _uri = config["jira-uri"].AsUri;
            if (_uri == null) {
                throw new ArgumentException(MISSING_FIELD_ERROR, "jira-uri");
            }

            _jiraTokenDuration = TimeSpan.FromMinutes(config["jira-session-timeout-mins"].AsInt ?? DEFAULT_LOGIN_TTL);

            // initialize web-service
            _jira = new JiraSoapServiceService();
            _jira.Url = _uri.At("rpc", "soap", "jirasoapservice-v2").ToString();
            result.Return();
        }

        protected override Yield Stop(Result result) {

            // clear settings
            _password = null;
            _username = null;
            _jira = null;
            yield return Coroutine.Invoke(base.Stop, new Result());
            result.Return();
        }
    }
}

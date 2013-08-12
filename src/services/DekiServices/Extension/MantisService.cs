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
using MindTouch.Threading;
using MindTouch.Deki.Services.MantisWebServices;
using MindTouch.Xml;

namespace MindTouch.Deki.Services.Extension {
    using Yield = IEnumerator<IYield>;

    [DreamService("MindTouch Mantis Bug Tracker Extension", "Copyright (c) 2006-2010 MindTouch Inc.",
        Info = "http://developer.mindtouch.com/App_Catalog/Mantis",
        SID = new string[] { 
            "sid://mindtouch.com/2008/01/mantis",
            "http://services.mindtouch.com/deki/draft/2008/01/mantis" 
        }
    )]
    [DreamServiceConfig("username", "string", "A Mantis username that has view access to your projects of interest")]
    [DreamServiceConfig("password", "string", "Password for the mantis account")]
    [DreamServiceConfig("mantis-uri", "string", "URI to your mantis installation. This URI + /api/soap/mantisconnect.php needs to exist")]
    [DreamServiceConfig("default-project", "string?", "Name of the default project to use")]
    [DreamServiceBlueprint("deki/service-type", "extension")]
    [DekiExtLibrary(
        Label = "Mantis",
        Namespace = "mantis",
        Description = "This extension contains functions for integrating with Mantis bug tracker versions 1.1.1+."
    )]
    [DekiExtLibraryFiles(Prefix = "MindTouch.Deki.Services.Resources", Filenames = new string[] { 
        "Mantis.css", "sorttable.js"
    })]
    public class MantisService : DekiExtService {
        
        //--- Constants ---
        private const int MAX_ISSUES_IN_REQUEST = 20;
        private const string MISSING_FIELD_ERROR = "Mantis extension: missing configuration setting";
        private static readonly string[] STRIKE_THROUGH_STATUSES = new string[] { "closed", "resolved" };

        //--- Fields ---
        private string _username;
        private string _password;
        private XUri _uri;
        private MantisConnect _service;
        
        //--- Functions ---
        [DekiExtFunction(Description = "Create a link with description to a bug.")]
        public XDoc Link(
            [DekiExtParam("bug id")] int id
        ) {
            IssueData bug = _service.mc_issue_get(_username, _password, id.ToString());
            XDoc result = new XDoc("html");
            result.Start("body");
            BuildBugLink(bug, result);
            result.End();
            return result;
        }

        [DekiExtFunction(Description = "Create a bulleted list of bugs for a given project")]
        public XDoc List(
            [DekiExtParam("project name (default: default-project)", true)] string project,
            [DekiExtParam("filter name (note: the filter must have been saved previously in Mantis) (default: none)", true)] string filter,
            [DekiExtParam("bugs per page (default: 25))", true)] int? count,
            [DekiExtParam("page number (default: first page)", true)] int? page
        ) {
            int pageNumber = page ?? 0;
            int numberPerPage = count ?? 25;
            ProjectData projectData = GetProjectByName(project ?? Config["default-project"].AsText);
            if(projectData == null) {
                throw new ArgumentException(string.Format("Project '{0}' not found", project ?? Config["default-project"].AsText), "project");
            }
            string filterId = string.Empty;
            if (!string.IsNullOrEmpty(filter)) {
                FilterData filterData = GetFilterByName(filter, projectData.id);
                if(filterData == null) {
                    throw new ArgumentException(string.Format("Filter '{0}' not found", filter), "filter");
                }
                filterId = filterData.id;
            }
            IssueData[] issues = RetrieveIssueData(_username, _password, projectData.id, filterId, pageNumber, numberPerPage);
            XDoc bugList = BuildBugListHTML(issues);
            XDoc ret = new XDoc("html");
            ret.Start("head")
                .Start("link").Attr("type", "text/css").Attr("rel", "stylesheet").Attr("href", Files.At("Mantis.css")).End()
            .End();
            ret.Start("body").Add(bugList).End();
            return ret;
        }


        [DekiExtFunction(Description = "Create a table listing of bugs for a given project")]
        public XDoc Table(
            [DekiExtParam("project name (default: default-project)", true)] string project,
            [DekiExtParam("filter name (note: the filter must have been saved previously in Mantis) (default: none)", true)] string filter,
            [DekiExtParam("bugs per page (default: 25))", true)] int? count,
            [DekiExtParam("page number (default: first page)", true)] int? page
        ) {
            int pageNumber = page ?? 1;
            int numberPerPage = count ?? MAX_ISSUES_IN_REQUEST;
            ProjectData projectData = GetProjectByName(project ?? Config["default-project"].AsText);
            if(projectData == null) {
                throw new ArgumentException(string.Format("Project '{0}' not found", project ?? Config["default-project"].AsText), "project");
            }
            string filterId = string.Empty;
            if (!string.IsNullOrEmpty(filter)) {
                FilterData filterData = GetFilterByName(filter, projectData.id);
                if(filterData == null) {
                    throw new ArgumentException(string.Format("Filter '{0}' not found", filter), "filter");
                }
                filterId = filterData.id;
            }
            IssueData[] issues = RetrieveIssueData(_username, _password, projectData.id, filterId, pageNumber, numberPerPage);
            XDoc bugList = BuildBugListHTMLTable(issues);
            XDoc ret = new XDoc("html");
            ret.Start("head")
                .Start("link").Attr("type", "text/css").Attr("rel", "stylesheet").Attr("href", Files.At("Mantis.css")).End()
                .Start("script").Attr("type", "text/javascript").Attr("src", Files.At("sorttable.js")).End()
            .End();
            ret.Start("body").Add(bugList).End();
            return ret;
        }

        [DekiExtFunction(Description = "Get the number of bugs returned by a filter")]
        public int Count(
            [DekiExtParam("project name (default: default-project)", true)] string project,
            [DekiExtParam("filter name (note: the filter must have been saved previously in Mantis) (default: none)", true)] string filter
            ) {

            ProjectData projectData = GetProjectByName(project ?? Config["default-project"].AsText);
            if (projectData == null) {
                throw new ArgumentException(string.Format("Project '{0}' not found", project ?? Config["default-project"].AsText), "project");
            }
            string filterId = string.Empty;
            if (!string.IsNullOrEmpty(filter)) {
                FilterData filterData = GetFilterByName(filter, projectData.id);
                if (filterData == null) {
                    throw new ArgumentException(string.Format("Filter '{0}' not found", filter), "filter");
                }
                filterId = filterData.id;
            }

            IssueHeaderData[] issues = _service.mc_filter_get_issue_headers(_username, _password, projectData.id, filterId, "0", "100000");
            return issues.Length;
        }


        //--- Methods ---
        private XDoc BuildBugLink(IssueData issue, XDoc doc) {
            bool strikeThrough = issue.status != null && Array.Exists<string>(STRIKE_THROUGH_STATUSES, delegate(string s) {
                return StringUtil.EqualsInvariantIgnoreCase(issue.status.name, s);
            });

            string title = string.Format("{0} (Reporter: {1} AssignedTo: {2})",
                issue.summary,
                issue.reporter != null ? issue.reporter.name : string.Empty,
                issue.handler != null ? issue.handler.name : string.Empty);


            doc.Start("a").Attr("href", _uri.At("view.php").With("id", issue.id)).Attr("title", title);
            if(strikeThrough) {
                doc.Start("del").Value("#" + issue.id).End();
            } else {
                doc.Value("#" + issue.id);
            }
            doc.End();
            return doc;
        }

        private XDoc BuildBugListHTML(IssueData[] issues) {
            XUri view = _uri.At("view.php");
            XDoc ret = new XDoc("div").Attr("class", "mantis-list");
            ret.Start("ul");
            foreach (IssueData bug in issues) {
                ret.Start("li")
                    .Start("a").Attr("href", view.With("id", bug.id)).Attr("target", "blank")
                        .Value(string.Format("{0}: {1} ({2})", bug.id, bug.summary, (bug.status != null) ? bug.status.name : string.Empty))
                    .End() // a
                .End(); // li
            }
            ret.End();
            return ret;
        }

        private XDoc BuildBugListHTMLTable(IssueData[] issues) {
            XDoc ret = new XDoc("div").Attr("class", "DW-table Mantis-table table");
            ret.Start("table").Attr("border", 0).Attr("cellspacing", 0).Attr("cellpadding", 0).Attr("class", "table sortable ").Attr("id", "mantis-table");

            // header
            ret.Start("tr")
                .Elem("th", "Bug#")
                .Elem("th", "Summary")
                .Elem("th", "Status")
                .Elem("th", "Opened By")
                .Elem("th", "Assigned To")
                .Elem("th", "Severity")
            .End();

            // loop over rows, if any
            if(issues != null) {
                int count = 0;
                foreach (IssueData bug in issues) {
                    count++;
                    string status = (bug.status != null) ? bug.status.name : string.Empty;
                    string severity = (bug.severity != null) ? bug.severity.name : string.Empty;
                    string tdClass = string.Format("{0} {1} {2}", (count % 2 == 0) ? "bg1" : "bg2", status, severity);
                    ret.Start("tr").Attr("class", tdClass);
                    ret.Start("td").Attr("class", tdClass);
                    ret = BuildBugLink(bug, ret);
                    ret.End(); //td;
                    ret.Start("td").Attr("class", tdClass).Value(bug.summary).End()
                         .Start("td").Attr("class", tdClass).Value(status).End()
                         .Start("td").Attr("class", tdClass).Value((bug.reporter != null) ? bug.reporter.name : string.Empty).End()
                         .Start("td").Attr("class", tdClass).Value((bug.handler != null) ? bug.handler.name : string.Empty).End()
                         .Start("td").Attr("class", tdClass + " severity").Value(severity).End()
                     .End();
                }
            }
            ret.End(); // table
            return ret;
        }

        private ProjectData GetProjectByName(string projectName) {
            if(!string.IsNullOrEmpty(projectName)) {
                ProjectData[] projects = _service.mc_projects_get_user_accessible(_username, _password);
                foreach(ProjectData p in projects) {
                    if(StringUtil.EqualsInvariantIgnoreCase(p.name.Trim(), projectName.Trim())) {
                        return p;
                    }
                }
            }
            return null;
        }

        private FilterData GetFilterByName(string filterName, string projectId) {
            if(!string.IsNullOrEmpty(filterName)) {
                FilterData[] filters = _service.mc_filter_get(_username, _password, projectId);
                foreach(FilterData f in filters) {
                    if(StringUtil.EqualsInvariantIgnoreCase(f.name.Trim(), filterName.Trim())) {
                        return f;
                    }
                }
            }
            return null;
        }

        protected override Yield Start(XDoc config, Result result) {
            yield return Coroutine.Invoke(base.Start, config, new Result());

            // read configuration settings
            _username = config["username"].AsText;
            if(string.IsNullOrEmpty(_username)) {
                throw new ArgumentException(MISSING_FIELD_ERROR, "username");
            }
            _password = config["password"].AsText;
            if(string.IsNullOrEmpty(_password)) {
                throw new ArgumentException(MISSING_FIELD_ERROR, "password");
            }
            _uri = config["mantis-uri"].AsUri;
            if(_uri == null) {
                throw new ArgumentException(MISSING_FIELD_ERROR, "mantis-uri");
            }

            // initialize web-service
            _service = new MantisWebServices.MantisConnect();
            _service.Url = _uri.At("api", "soap", "mantisconnect.php").ToString();
            result.Return();
        }

        protected override Yield Stop(Result result) {

            // clear settings
            _uri = null;
            _password = null;
            _username = null;
            _service = null;
            yield return Coroutine.Invoke(base.Stop, new Result());
            result.Return();
        }

        private IssueData[] RetrieveIssueData(string username, string password, string projectId, string filterId, int pageNumber, int numberPerPage) {
            using(ElasticThreadPool pool = new ElasticThreadPool(0, 2)) {
                List<IssueData> result = new List<IssueData>();
                List<Result<IssueData[]>> results = new List<Result<IssueData[]>>();
                Tuplet<bool> canceled = new Tuplet<bool>(false);
                for(int issuesRemaining = numberPerPage; issuesRemaining > 0; issuesRemaining -= MAX_ISSUES_IN_REQUEST, ++pageNumber) {
                    int issuesInBatch = Math.Min(issuesRemaining, MAX_ISSUES_IN_REQUEST);
                    results.Add(ProcessIssueBatch(pool, projectId, filterId, pageNumber, issuesInBatch, canceled, new Result<IssueData[]>(TimeSpan.FromSeconds(30))));
                }
                Dictionary<string, IssueData> tempHash = new Dictionary<string, IssueData>();
                foreach(Result<IssueData[]> r in results) {
                    IssueData[] batch = r.Wait();

                    //HACK: Workaround for Mantis's broken paging: Asking for a batch at a page number that doesnt exist
                    // will return the first page's results.
                    // This takes care of the case when the #of tickets is evenly divisible by the batch size. (i.e 100 tix, 20/page)
                    foreach(IssueData bug in batch) {
                        if(!tempHash.ContainsKey(bug.id)) {
                            tempHash[bug.id] = bug;
                            result.Add(bug);
                        }
                    }
                    if(batch.Length < MAX_ISSUES_IN_REQUEST) {

                        //the current batch did not fill up, don't go to the next batch
                        canceled.Item1 = true;
                        break;
                    }
                }
                return result.ToArray();
            }
        }

        private Result<IssueData[]> ProcessIssueBatch(ElasticThreadPool pool,  string projectId, string filterId, int pageNumber, int issuesInBatch, Tuplet<bool> canceled, Result<IssueData[]> result) {
            pool.QueueWorkItem(HandlerUtil.WithEnv(delegate {

                // TODO (steveb): use result.IsCanceled instead of shared tuple once cancellation is supported on the result object

                // check if request has been canceled
                if(!canceled.Item1) {
                    IssueData[] issuesForBatch;
                    if(!string.IsNullOrEmpty(filterId)) {
                        issuesForBatch = _service.mc_filter_get_issues(_username, _password, projectId, filterId, pageNumber.ToString(), issuesInBatch.ToString());
                    } else {
                        issuesForBatch = _service.mc_project_get_issues(_username, _password, projectId, pageNumber.ToString(), issuesInBatch.ToString());
                    }
                    result.Return(issuesForBatch);
                } else {
                	
                	// TODO (steveb): throw a more specific exception
                    result.Throw(new Exception("unspecified error"));
                }
            },TaskEnv.Clone()));
            return result;
        } 
    }
}

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

using MindTouch.Dream;
using MindTouch.Tasking;
using MindTouch.Xml;

namespace MindTouch.Deki.Services {
    using Yield = IEnumerator<IYield>;

    [DreamService("MindTouch ADO.NET Data Services Extension", "Copyright (c) 2006-2010 MindTouch Inc.",
       Info = "http://developer.mindtouch.com/App_Catalog/ADONET",
       SID = new string[] { 
            "sid://mindtouch.com/2008/02/ado.net",
            "http://services.mindtouch.com/deki/draft/2008/02/ado.net" 
       }
    )]
    [DreamServiceConfig("service-uri", "string", "URI to an ADO.NET web service endpoint. (Example: http://astoria.sandbox.live.com/northwind/northwind.rse)")]
    [DreamServiceConfig("username", "string?", "Username for ADO.NET data service")]
    [DreamServiceConfig("password", "string?", "Password for ADO.NET data service")]
    [DreamServiceBlueprint("deki/service-type", "extension")]
    [DekiExtLibrary(
        Label = "ADO.NET",
        Namespace = "adonet",
        Description = "This extension contains functions for displaying data from ADO.NET data services."
    )]
    [DekiExtLibraryFiles(Prefix = "MindTouch.Deki.Services.Resources", Filenames = new string[] { "sorttable.js" })]
    public class AdoDotNetService : DekiExtService {

        //--- Constants ---
        private const string MISSING_FIELD_ERROR = "ADO.NET extension: missing configuration setting";
        private const string METADATA_PATH = "$sys_GetEdmSchema";
        private const double CACHE_TTL = 30;

        //--- Fields ---
        Plug _adoNetPlug = null;
        Dictionary<string, XDoc> _cache = new Dictionary<string, XDoc>();

        //--- Functions ---
        [DekiExtFunction(Description = "Show results from an ADO.NET query as a table.", Transform = "pre")]
        public XDoc Table(
            [DekiExtParam("resource name with optional query logic. Example: Products[ProductName eq 'Chai']")] string resource,
            [DekiExtParam("nested resources to expand (default: none)", true)] string expand,
            [DekiExtParam("filter records by (default: return all records)", true)] string filter,
            [DekiExtParam("order records by (default: none)", true)] string orderby,
            [DekiExtParam("number of records to skip (default: 0)", true)] int? skip,
            [DekiExtParam("number of records to return (default: 100)", true)] int? top,
            [DekiExtParam("record columns to table columns mapping ({ Customer: { LastName: 'Last', FirstName: 'First' }, Order: { OrderID: 'ID'} }; default: all columns)", true)] Hashtable columns,
            [DekiExtParam("URI to ADO.NET data service", true)] XUri dataservice
        ) {
            XDoc schema;
            XDoc adoNetRet = PerformQuery(dataservice, resource, expand, filter, orderby, skip ?? 0, top ?? 100, true, out schema);

            XDoc result = new XDoc("html")
                .Start("head")
                    .Start("script").Attr("type", "text/javascript").Attr("src", Files.At("sorttable.js")).End()
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
                .End()
                .Start("body");

            if(adoNetRet.HasName("DataService")) {
                foreach(XDoc resourceSet in adoNetRet.Elements) {
                    RenderOutput(result, resourceSet, schema, columns);
                }
            }
            result.End();
            return result;
        }

        [DekiExtFunction(Description = "Show single value from a query for a given column")]
        public string Value(
            [DekiExtParam("resource name with optional query logic. Example: Products[ProductName eq 'Chai']")] string resource,
            [DekiExtParam("filter records by (default: return all records)", true)] string filter,
            [DekiExtParam("record column")] string column,
            [DekiExtParam("URI to ADO.NET data service", true)] XUri dataservice
        ) {

            XDoc schema;
            XDoc adoNetRet = PerformQuery(dataservice, resource, string.Empty, filter, string.Empty, 0, null, false, out schema);
            return adoNetRet[string.Format(".//{0}", column)].AsText;
        }

        [DekiExtFunction(Description = "Show the values of a given column from a list of records")]
        public ArrayList List(
            [DekiExtParam("resource name with optional query logic. Example: Products[ProductName eq 'Chai']")] string resource,
            [DekiExtParam("filter records by (default: return all records)", true)] string filter,
            [DekiExtParam("order records by (default: none)", true)] string orderby,
            [DekiExtParam("number of records to skip (default: 0)", true)] int? skip,
            [DekiExtParam("number of records to return (default: 100)", true)] int? top,
            [DekiExtParam("record column")] string column,
            [DekiExtParam("URI to ADO.NET data service", true)] XUri dataservice
        ) {
            XDoc schema;
            XDoc adoNetRet = PerformQuery(dataservice, resource, string.Empty, filter, orderby, skip ?? 0, top ?? 100, false, out schema);
            ArrayList ret = new ArrayList();
            foreach (XDoc x in adoNetRet[string.Format(".//{0}", column)]) {
                ret.Add(x.AsText);
            }
            return ret;
        }

        //--- Methods ---

        private XDoc PerformQuery(XUri dataservice, string resource, string expand, string filter, string orderby, int? skip, int? top, bool fetchSchema, out XDoc schema) {
            Plug p = _adoNetPlug;

            if (dataservice != null) {
                p = Plug.New(dataservice);
            } else if (p == null) {
                throw new ArgumentException("Missing field", "dataservice");
            }

            if (fetchSchema) {
                schema = FetchSchema(p);
            } else {
                schema = null;
            }

            if (!string.IsNullOrEmpty(resource)) {
                
                //HACKHACKHACK: +'s aren't treated the same way as '%20' when uri is decoded on the server side
                string s = XUri.Encode(resource).Replace("+", "%20");
                p = p.At(s);
            }

            if (!string.IsNullOrEmpty(expand)) {
                p = p.With("$expand", expand);
            }
            if (!string.IsNullOrEmpty(filter)) {
                p = p.With("$filter", filter);
            }

            if (!string.IsNullOrEmpty(orderby)) {
                p = p.With("$orderby", orderby);
            }

            if (skip != null) {
                p = p.With("$skip", skip.Value);
            }

            if (top != null) {
                p = p.With("$top", top.Value);
            }

            XDoc ret = null;
            string key = p.ToString();
            lock (_cache) {
                _cache.TryGetValue(key, out ret);
            }

            if (ret == null) {
                ret = p.Get().ToDocument();

                // add result to cache and start a clean-up timer
                lock (_cache) {
                    _cache[key] = ret;
                }

                TaskTimer.New(TimeSpan.FromSeconds(CACHE_TTL), RemoveCachedEntry, key, TaskEnv.None);
            }
            return ret;
        }

        private XDoc RenderOutput(XDoc output, XDoc adoNetInput, XDoc schema, Hashtable columnFilter) {
            KeyValuePair<string, string>[] columns;
            List<string> refColumns;
            string entityType = string.Empty;

            //Discover entity type from collection
            if (!adoNetInput.Elements.IsEmpty)
                entityType = adoNetInput.Elements.Name;

            if (!string.IsNullOrEmpty(entityType)) {
                GetColumnsForEntityType(schema, entityType, out columns, out refColumns, columnFilter);
                output = BuildTableHeader(output, columns);

                foreach (XDoc resource in adoNetInput[entityType]) {
                    output.Start("tr");
                    foreach (KeyValuePair<string, string> c in columns) {
                        output.Start("td");
                        if (refColumns.Contains(c.Key)) {
                            //foreign key column

                            if (resource[c.Key]["@href"].IsEmpty) {
                                output = RenderOutput(output, resource[c.Key], schema, columnFilter);
                            } else {

                                // TODO (maxm): need to build a complete uri
                                output.Value(resource[c.Key]["@href"].AsText);
                            }
                        } else {
                            output.Value(resource[c.Key].AsText);
                        }
                        output.End();
                    }

                    output.End();//tr
                }
                output.End(); //table
            }
            return output;
        }

        private XDoc FetchSchema(Plug adoNetPlug) {
            XDoc ret = null;
            string key = adoNetPlug.At(METADATA_PATH).ToString();
            lock (_cache) {
                _cache.TryGetValue(key, out ret);
            }

            if (ret == null) {
                string temp = adoNetPlug.At(METADATA_PATH).Get().AsTextReader().ReadToEnd();
              
                //HACKHACKHACK to workaround ns issue
                temp = temp.Replace("xmlns=\"http://schemas.microsoft.com/ado/2006/04/edm\"", "");
                ret = XDocFactory.From(temp, MimeType.XML);

                // add result to cache and start a clean-up timer
                lock (_cache) {
                    _cache[key] = ret;
                }

                TaskTimer.New(TimeSpan.FromSeconds(CACHE_TTL), RemoveCachedEntry, key, TaskEnv.None);
            }

            //TODO: throw exception if schema is invalid somehow (or if the schema changed)
            return ret;          
        }

        private void GetColumnsForEntityType(XDoc schema, string entityType, out KeyValuePair<string, string>[] columns, out List<string> refColumns, Hashtable columnFilter) {

            // Example: http://astoria.sandbox.live.com/northwind/northwind.rse/$sys_GetEdmSchema

            XDoc entity = schema[string.Format("EntityType[@Name='{0}']", entityType)];
            if (entity.IsEmpty) {
                throw new ArgumentOutOfRangeException(string.Format("Could not resolve the EntityType for '{0}'", entityType));
            }

            Hashtable columnMapping = null;
            if (columnFilter != null) {
                columnMapping = columnFilter[entityType] as Hashtable;
            }
            List<KeyValuePair<string, string>> columnsList = new List<KeyValuePair<string, string>>();
            refColumns = new List<string>();

            foreach (XDoc column in entity["Property/@Name"]) {
                string name = column.AsText;
                string newName = name;

                if (columnMapping != null) {
                    newName = columnMapping[name] as string;
                }

                if (name != null && newName != null) {
                    columnsList.Add(new KeyValuePair<string, string>(name, newName));
                }

            }

            foreach (XDoc columnRef in entity["NavigationProperty/@Name"]) {
                string name = columnRef.AsText;
                string newName = name;

                refColumns.Add(name);

                if (columnMapping != null) {
                    newName = columnMapping[name] as string;
                }

                if (name != null && newName != null) {
                    columnsList.Add(new KeyValuePair<string, string>(name, newName));
                }
            }

            columns = columnsList.ToArray();
        }

        private XDoc BuildTableHeader(XDoc doc, KeyValuePair<string, string>[] columns) {
            doc.Start("table").Attr("border", 1).Attr("cellpadding", 0).Attr("cellspacing", 0).Attr("class", "feedtable sortable");

            doc.Start("tr");
            foreach (KeyValuePair<string, string> c in columns) {
                doc.Start("th").Value(c.Value).End();//th
            }

            doc.End();//tr

            return doc;
        }

        private void RemoveCachedEntry(TaskTimer timer) {
            lock (_cache) {
                _cache.Remove((string) timer.State);
            }
        }

        protected override Yield Start(XDoc config, Result result) {
            yield return Coroutine.Invoke(base.Start, config, new Result());
            
            _adoNetPlug = Plug.New(config["service-uri"].AsUri);

            if (_adoNetPlug != null) {
                string u = config["username"].AsText;
                string p = config["password"].AsText;
                if (!string.IsNullOrEmpty(u) || !string.IsNullOrEmpty(p))
                    _adoNetPlug = _adoNetPlug.WithCredentials(u, p);
            }

            result.Return();
        }
    }
}

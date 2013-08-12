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
using System.Data;
using System.Text;

using MindTouch.Data;
using MindTouch.Dream;
using MindTouch.Tasking;
using MindTouch.Xml;

namespace MindTouch.Deki.Services {
    using Yield = IEnumerator<IYield>;

    [DreamService("PostgreSql Extension", "Copyright (c) 2006-2010 MindTouch Inc.",
        Info = "http://developer.mindtouch.com/App_Catalog/Postgresql",
        SID = new string[] { 
            "sid://mindtouch.com/2009/05/pgsql",
            "http://services.mindtouch.com/deki/draft/2009/05/pgsql" 
        }
    )]
    [DreamServiceConfig("db-server", "string?", "Database host name. (default: localhost)")]
    [DreamServiceConfig("db-port", "int?", "Database port. (default: 5432)")]
    [DreamServiceConfig("db-catalog", "string", "Database table name.")]
    [DreamServiceConfig("db-user", "string", "Database user name.")]
    [DreamServiceConfig("db-password", "string", "Password for database user.")]
    [DreamServiceConfig("db-options", "string?", "Optional connection string parameters. (default: none)")]
    [DreamServiceBlueprint("deki/service-type", "extension")]
    [DekiExtLibrary(
        Label = "PgSql", 
        Namespace = "pgsql", 
        Description = "This extension contains functions for displaying data from PostgreSQL databases.",
        Logo = "$files/pgsql-logo.png"
    )]
    [DekiExtLibraryFiles(Prefix = "MindTouch.Deki.Services.Resources", Filenames = new string[] { "sorttable.js", "pgsql-logo.png" })]
    public class PostgreSqlService : DekiExtService {

        //--- Class Fields ---
        private static DataFactory _factory = new DataFactory("Npgsql", "?");

        //--- Fields ---
        private DataCatalog _catalog;

        //--- Functions ---
        [DekiExtFunction(Description = "Show results from a SELECT query as a table.", Transform = "pre")]
        public XDoc Table(
            [DekiExtParam("SELECT query")] string query
        ) {
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
            result.Start("table").Attr("border", 0).Attr("cellpadding", 0).Attr("cellspacing", 0).Attr("class", "feedtable sortable");
            _catalog.NewQuery(query).Execute(delegate(IDataReader reader) {

                // capture row columns
                result.Start("thead").Start("tr");
                int count = reader.FieldCount;
                for(int i = 0; i < count; ++i) {
                    result.Start("th").Elem("strong", reader.GetName(i)).End();
                }
                result.End().End();

                // read records
                int rowcount = 0;
                result.Start("tbody");
                while(reader.Read()) {
                    result.Start("tr");
                    result.Attr("class", ((rowcount++ & 1) == 0) ? "feedroweven" : "feedrowodd");
                    for (int i = 0; i < count; ++i) {
                        string val = string.Empty;

                        try {
                            if (!reader.IsDBNull(i)) {
                                val = reader.GetValue(i).ToString();
                            }
                        } catch { }
                     
                        result.Elem("td", val);
                    }
                    result.End();
                }
                result.End();
            });
            result.End().End();
            return result;
        }

        [DekiExtFunction(Description = "Get single value from a SELECT query.")]
        public string Value(
            [DekiExtParam("SELECT query")] string query
        ) {
            return _catalog.NewQuery(query).Read();
        }

        [DekiExtFunction(Description = "Collect rows as a list from a SELECT query.")]
        public ArrayList List(
            [DekiExtParam("SELECT query")] string query,
            [DekiExtParam("column name (default: first column)", true)] string column
        ) {
            ArrayList result = new ArrayList();
            _catalog.NewQuery(query).Execute(delegate(IDataReader reader) {
                int index = (column != null) ? reader.GetOrdinal(column) : 0;
                while(reader.Read()) {
                    result.Add(reader[index]);
                }
            });
            return result;
        }

        [DekiExtFunction(Description = "Collect all columns from a SELECT query.")]
        public Hashtable Record(
            [DekiExtParam("SELECT query")] string query
        ) {
            Hashtable result = new Hashtable(StringComparer.OrdinalIgnoreCase);
            _catalog.NewQuery(query).Execute(delegate(IDataReader reader) {
                if(reader.Read()) {
                    for(int i = 0; i < reader.FieldCount; ++i) {
                        result[reader.GetName(i)] = reader[i];
                    }
                }
            });
            return result;
        }

        [DekiExtFunction(Description = "Collect all columns and all rows from a SELECT query.")]
        public ArrayList RecordList(
            [DekiExtParam("SELECT query")] string query
        ) {
            ArrayList result = new ArrayList();
            _catalog.NewQuery(query).Execute(delegate(IDataReader reader) {
                string[] columns = null;
                while(reader.Read()) {

                    // read result column names
                    if(columns == null) {
                        columns = new string[reader.FieldCount];
                        for(int i = 0; i < columns.Length; ++i) {
                            columns[i] = reader.GetName(i);
                        }
                    }

                    // read row
                    Hashtable row = new Hashtable(StringComparer.OrdinalIgnoreCase);
                    for(int i = 0; i < reader.FieldCount; ++i) {
                        row[columns[i]] = reader[i];
                    }
                    result.Add(row);
                }
            });
            return result;
        }

        //--- Methods ---
        protected override Yield Start(XDoc config, Result result) {
            yield return Coroutine.Invoke(base.Start, config, new Result());
            StringBuilder connectionString = new StringBuilder();
            string server = config["db-server"].AsText ?? "localhost";
            connectionString.AppendFormat("Server={0};", server);
            int? port = config["db-port"].AsInt;
            if( port.HasValue) {
                connectionString.AppendFormat("Port={0};", port.Value);
            }
            string catalog = config["db-catalog"].AsText;
            if(string.IsNullOrEmpty(catalog)) {
                throw new ArgumentNullException("config/catalog");
            }
            connectionString.AppendFormat("Database={0};", catalog);
            string user = config["db-user"].AsText;
            if(!string.IsNullOrEmpty(user)) {
                connectionString.AppendFormat("User Id={0};", user);
            }
            string password = config["db-password"].AsText;
            if(!string.IsNullOrEmpty(password)) {
                connectionString.AppendFormat("Password={0};", password);
            }
            string options = config["db-options"].AsText;
            if(!string.IsNullOrEmpty(options)) {
                connectionString.Append(options);
            }
            _catalog = new DataCatalog(_factory, connectionString.ToString());
            result.Return();
        }

        protected override Yield Stop(Result result) {
            _catalog = null;
            yield return Coroutine.Invoke(base.Stop, new Result());
            result.Return();
        }
    }
}

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

    [DreamService("MindTouch Dapper Extension", "Copyright (c) 2006-2010 MindTouch Inc.",
        Info = "http://developer.mindtouch.com/App_Catalog/Dapper",
        SID = new string[] { 
            "sid://mindtouch.com/2007/12/dapper",
            "http://services.mindtouch.com/deki/draft/2007/12/dapper" 
        }
    )]
    [DreamServiceBlueprint("deki/service-type", "extension")]
    [DekiExtLibrary(
        Label = "Dapper",
        Namespace = "dapp",
        Description = "This extension contains functions for embedding Dapps from Dapper.net.",
        Logo = "$files/dapper-logo.png"
    )]
    [DekiExtLibraryFiles(Prefix = "MindTouch.Deki.Services.Resources", Filenames = new string[] { "sorttable.js", "dapper-logo.png" })]
    public class DapperService : DekiExtService {

        //--- Constants ---
        public const double CACHE_TTL = 30;
        public const string VARIABLE_PREFIX = "variableArg_";

        //--- Fields ---
        private Dictionary<string, XDoc> _cache = new Dictionary<string, XDoc>();

        //--- Functions ---
        [DekiExtFunction(Description = "Retrieve the XML for a Dapp.")]
        public Yield Xml(
            [DekiExtParam("name of Dapp to invoke")] string name,
            [DekiExtParam("input uri (default: as defined by the Dapp)", true)] XUri input,
            [DekiExtParam("dapp arguments (default: none)", true)] Hashtable args,
            Result<XDoc> response
        ) {
            Result<XDoc> res;
            yield return res = Coroutine.Invoke(FetchResult, name, input, args, new Result<XDoc>());
            response.Return(res.Value);
        }

        [DekiExtFunction(Description = "Show a single value from a Dapp.")]
        public Yield Value(
            [DekiExtParam("name of Dapp to invoke")] string name,
            [DekiExtParam("xpath to select value (default: first field in Dapp)", true)] string xpath,
            [DekiExtParam("input uri (default: as defined by the Dapp)", true)] XUri input,
            [DekiExtParam("dapp arguments (default: none)", true)] Hashtable args,
            Result<object> response
        ) {
            Result<XDoc> res;
            yield return res = Coroutine.Invoke(FetchResult, name, input, args, new Result<XDoc>());
            XDoc doc = res.Value;

            // fetch value from document
            XDoc value = doc[xpath ?? ".//*[@type='field']"];
            response.Return(ConvertDocToValue(value, false));
            yield break;
        }

        [DekiExtFunction(Description = "Show a single HTML value from a Dapp.")]
        public Yield Html(
            [DekiExtParam("name of Dapp to invoke")] string name,
            [DekiExtParam("xpath to select value (default: first field in Dapp)", true)] string xpath,
            [DekiExtParam("input uri (default: as defined by the Dapp)", true)] XUri input,
            [DekiExtParam("dapp arguments (default: none)", true)] Hashtable args,
            Result<object> response
        ) {
            Result<XDoc> res;
            yield return res = Coroutine.Invoke(FetchResult, name, input, args, new Result<XDoc>());
            XDoc doc = res.Value;

            // fetch value from document
            XDoc value = doc[xpath ?? ".//*[@type='field']"];
            response.Return(ConvertDocToValue(value, true));
            yield break;
        }

        [DekiExtFunction(Description = "Collect values as a list from a Dapp.")]
        public Yield List(
            [DekiExtParam("name of Dapp to invoke")] string name,
            [DekiExtParam("xpath for collecting values (default: all groups in Dapp)", true)] string xpath,
            [DekiExtParam("input uri (default: as defined by the Dapp)", true)] XUri input,
            [DekiExtParam("dapp arguments (default: none)", true)] Hashtable args,
            Result<ArrayList> response
        ) {
            Result<XDoc> res;
            yield return res = Coroutine.Invoke(FetchResult, name, input, args, new Result<XDoc>());
            XDoc doc = res.Value;

            // fetch values from document
            ArrayList result = new ArrayList();
            foreach(XDoc value in doc[xpath ?? ".//*[@type='group']"]) {
                result.Add(ConvertDocToValue(value, false));
            }
            response.Return(result);
            yield break;
        }

        [DekiExtFunction(Description = "Show results from a Dapp as a table.")]
        public Yield Table(
            [DekiExtParam("name of Dapp to invoke")] string name,
            [DekiExtParam("xpath for collecting values (default: all groups in Dapp)", true)] string xpath,
            [DekiExtParam("input uri (default: as defined by the Dapp)", true)] XUri input,
            [DekiExtParam("dapp arguments (default: none)", true)] Hashtable args,
            Result<XDoc> response
        ) {
            Result<XDoc> res;
            yield return res = Coroutine.Invoke(FetchResult, name, input, args, new Result<XDoc>());
            XDoc doc = res.Value;

            // fetch value from document
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
            XDoc rows = doc[xpath ?? ".//*[@type='group']"];

            // create header row
            result.Start("thead").Start("tr");
            foreach(XDoc cell in rows.Elements) {
                result.Start("th").Elem("strong", cell.Name).End();
            }
            result.End().End();

            // create data rows
            int rowcount = 0;
            result.Start("tbody");
            foreach(XDoc row in rows) {
                result.Start("tr");
                result.Attr("class", ((rowcount++ & 1) == 0) ? "feedroweven" : "feedrowodd");
                foreach(XDoc cell in row.Elements) {
                    result.Elem("td", ConvertDocToValue(cell, true));
                }
                result.End();
            }
            result.End();

            // close table & body tags
            result.End().End();
            response.Return(result);
            yield break;
        }

        [DekiExtFunction(Description = "Run a Dapp and publish its results.")]
        [DekiExtFunctionScript("MindTouch.Deki.Services.Resources", "dapper-run.xml")]
        public XDoc Run(
            [DekiExtParam("name of Dapp to invoke")] string name,
            [DekiExtParam("xpath for collecting values (default: all groups in Dapp)", true)] string xpath,
            [DekiExtParam("input uri (default: as defined by the Dapp)", true)] XUri input,
            [DekiExtParam("dapp arguments (default: none)", true)] Hashtable args,
            [DekiExtParam("publish on channel (default: \"default\")", true)] string publish,
            [DekiExtParam("subscribe to channel (default: nil)", true)] string subscribe
        ) {
            throw new InvalidOperationException("this function is implemented as a script");
        }

        //--- Features ---
        [DreamFeature("POST:proxy/run", "Run Dapp service.")]
        public Yield PostRun(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            XDoc doc = request.ToDocument();
            string name = doc["dappName"].AsText;
            XUri input = doc["applyToUrl"].AsUri;
            string xpath = doc["xpath"].AsText;

            // convert args
            Hashtable args = new Hashtable();
            foreach(XDoc item in doc.Elements) {
                if(StringUtil.StartsWithInvariant(item.Name, VARIABLE_PREFIX)) {
                    args[item.Name.Substring(VARIABLE_PREFIX.Length)] = item.AsText;
                }
            }

            // invoke dapper
            Result<XDoc> res;
            yield return res = Coroutine.Invoke(FetchResult, name, input, args, new Result<XDoc>());

            // create result document
            XDoc rows = res.Value[xpath ?? ".//*[@type='group']"];
            XDoc result = new XDoc("results");
            foreach(XDoc row in rows) {
                result.Start("result");
                foreach(XDoc cell in row.Elements) {
                    result.Elem(cell.Name, cell.AsText);
                }
                result.End();
            }
            string json = JsonUtil.ToJson(result);
            response.Return(DreamMessage.Ok(MimeType.JSON, json));
            yield break;
        }

        //--- Methods ---
        private Yield FetchResult(string name, XUri input, Hashtable args, Result<XDoc> response) {

            // build uri
            XUri uri = new XUri("http://www.dapper.net/RunDapp?v=1").With("dappName", name);
            if(input != null) {
                uri = uri.With("applyToUrl", input.ToString());
            }
            if(args != null) {
                foreach(DictionaryEntry entry in args) {
                    uri = uri.With(VARIABLE_PREFIX + SysUtil.ChangeType<string>(entry.Key), SysUtil.ChangeType<string>(entry.Value));
                }
            }

            // check if we have a cached result
            XDoc result;
            string key = uri.ToString();
            lock(_cache) {
                if(_cache.TryGetValue(key, out result)) {
                    response.Return(result);
                    yield break;
                }
            }

            // fetch result
            Result<DreamMessage> res;
            yield return res = Plug.New(uri).GetAsync();
            if(!res.Value.IsSuccessful) {
                throw new DreamInternalErrorException(string.Format("Unable to process Dapp: ", input));
            }
            if(!res.Value.HasDocument) {
                throw new DreamInternalErrorException(string.Format("Dapp response is not XML: ", input));
            }

            // add result to cache and start a clean-up timer
            lock(_cache) {
                _cache[key] = res.Value.ToDocument();
            }
            TaskTimer.New(TimeSpan.FromSeconds(CACHE_TTL), RemoveCachedEntry, key, TaskEnv.None);
            response.Return(res.Value.ToDocument());
            yield break;
        }

        private object ConvertDocToValue(XDoc value, bool allowxml) {
            if(value.IsEmpty) {

                // no value for element
                return null;
            } else if(StringUtil.EqualsInvariant(value["@type"].AsText, "group")) {

                // value is a group
                Hashtable result = new Hashtable();
                foreach(XDoc child in value.Elements) {
                    result[child.Name] = ConvertDocToValue(child, allowxml);
                }
                return result;
            } else {

                // value is field, determine if the field needs special treatment
                switch(value["@originalElement"].AsText) {
                case "a":
                    if(allowxml) {
                        return new XDoc("html").Start("body").Start("a").Attr("href", value["@href"].AsText).Value(value.AsText).End().End();
                    } else {
                        return value.AsText;
                    }
                case "img":
                    if(allowxml) {
                        return new XDoc("html").Start("body").Start("img").Attr("src", value["@src"].AsText).Attr("href", value["@href"].AsText).End().End();
                    } else {
                        return value["@src"].AsText;
                    }
                default:
                    return value.AsText;
                }
            }
        }

        private void RemoveCachedEntry(TaskTimer timer) {
            lock(_cache) {
                _cache.Remove((string)timer.State);
            }
        }
    }
}

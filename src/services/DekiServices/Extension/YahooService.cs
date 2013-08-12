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
using System.Text;

using MindTouch.Dream;
using MindTouch.Tasking;
using MindTouch.Xml;

namespace MindTouch.Deki.Services {
    using Yield = IEnumerator<IYield>;

    [DreamService("MindTouch Yahoo! Extension", "Copyright (c) 2006-2010 MindTouch Inc.",
        Info = "http://developer.mindtouch.com/App_Catalog/Yahoo%21",
        SID = new string[] { 
            "sid://mindtouch.com/2007/06/yahoo",
            "http://services.mindtouch.com/deki/draft/2007/06/yahoo" 
        }
    )]
    [DreamServiceConfig("finance-app-id", "string", "Yahoo! Finance Application ID")]
    [DreamServiceConfig("finance-sig", "string", "Yahoo! Finance Signature")]
    [DreamServiceConfig("yahoo-app-id", "string?", "Yahoo! Application ID (default: \"YahooDemo\")")]
    [DreamServiceBlueprint("deki/service-type", "extension")]
    [DekiExtLibrary(
        Label = "Yahoo!", 
        Namespace = "yahoo", 
        Description = "This extension contains functions for embedding Yahoo! widgets.",
        Logo = "$files/yahoo-logo.png"
    )]
    [DekiExtLibraryFiles(Prefix = "MindTouch.Deki.Services.Resources", Filenames = new string[] { "yahoo-logo.png" })]
    public class YahooService : DekiExtService {

        //--- Functions ---
        [DekiExtFunction(Description = "Embed Yahoo! finance stock quote widget")]
        public XDoc StockQuote(
            [DekiExtParam("stock ticker symbol")] string symbol
        ) {

            // check keys
            string app = Config["finance-app-id"].AsText;
            string sig = Config["finance-sig"].AsText;
            if(string.IsNullOrEmpty(app) || string.IsNullOrEmpty(sig)) {
                return new XDoc("html").Start("body").Start("span").Attr("style", "color:red;font-weight:bold;").Value("The Yahoo! Finance Application ID or Signature are missing").End().End();
            }

            // create control
            symbol = symbol.ToUpperInvariant();
            XDoc result = new XDoc("html").Start("body").Start("iframe")
                .Attr("allowtransparency", "true")
                .Attr("marginwidth", "0")
                .Attr("marginheight", "0")
                .Attr("hspace", "0")
                .Attr("vspace", "0")
                .Attr("frameborder", "0")
                .Attr("scrolling", "no")
                .Attr("src", string.Format("http://api.finance.yahoo.com/instrument/1.0/{0}/badge;quote/HTML?AppID={1}&sig={2}", XUri.EncodeSegment(symbol), XUri.EncodeQuery(app), XUri.EncodeQuery(sig)))
                .Attr("width", "200px")
                .Attr("height", "250px")
                .Start("a").Attr("href", "http://finance.yahoo.com").Value("Yahoo! Finance").End()
                .Elem("br")
                .Start("a").Attr("href", string.Format("http://finance.yahoo.com/q?s={0}", XUri.EncodeQuery(symbol))).Value(string.Format("Quote for {0}", symbol)).End()
            .End().End();
            return result;
        }

        [DekiExtFunction(Description = "Embed Yahoo! finance stock chart widget")]
        public XDoc StockChart(
            [DekiExtParam("stock ticker symbol")] string symbol
        ) {

            // check keys
            string app = Config["finance-app-id"].AsText;
            string sig = Config["finance-sig"].AsText;
            if(string.IsNullOrEmpty(app) || string.IsNullOrEmpty(sig)) {
                return new XDoc("html").Start("body").Start("span").Attr("style", "color:red;font-weight:bold;").Value("The Yahoo! Finance Application ID or Signature are missing").End().End();
            }

            // create control
            symbol = symbol.ToUpperInvariant();
            XDoc result = new XDoc("html").Start("body").Start("iframe")
                .Attr("allowtransparency", "true")
                .Attr("marginwidth", "0")
                .Attr("marginheight", "0")
                .Attr("hspace", "0")
                .Attr("vspace", "0")
                .Attr("frameborder", "0")
                .Attr("scrolling", "no")
                .Attr("src", string.Format("http://api.finance.yahoo.com/instrument/1.0/{0}/badge;chart=1y;quote/HTML?AppID={1}&sig={2}", XUri.EncodeSegment(symbol), XUri.EncodeQuery(app), XUri.EncodeQuery(sig)))
                .Attr("width", "200px")
                .Attr("height", "390px")
                .Start("a").Attr("href", "http://finance.yahoo.com").Value("Yahoo! Finance").End()
                .Elem("br")
                .Start("a").Attr("href", "http://finance.yahoo.com/q?s=^GSPC/").Value("Quote for ^GSPC").End()
            .End().End();
            return result;
        }

        [DekiExtFunction(Description = "Publish terms extracted from content")]
        public XDoc ExtractTerms(
            [DekiExtParam("content to extract terms from (default: nil)", true)] string content, 
            [DekiExtParam("max number of terms to extract (default: 3)", true)] int? max, 
            [DekiExtParam("publish on channel (default: \"default\")", true)] string publish,
            [DekiExtParam("subscribe to channel (default: nil)", true)] string subscribe
        ) {
            XDoc result = new XDoc("html");

            // head
            string id = StringUtil.CreateAlphaNumericKey(8);
            StringBuilder javascript = new StringBuilder();
            javascript.AppendLine(
@"var data = { service: '{service}', max: {max}, channel: '{publish}' };
Deki.subscribe('{id}', null, yahoo_analyzecontent_data, data);"
.Replace("{id}", id)
.Replace("{publish}", StringUtil.EscapeString(publish ?? "default"))
.Replace("{service}", DreamContext.Current.AsPublicUri(Self.At("proxy").At("extractterms").With("dream.out.format", "json")).ToString())
.Replace("{max}", (max ?? 3).ToString())
            );
            if(subscribe != null) {
                javascript.AppendLine(@"Deki.subscribe('{subscribe}', null, yahoo_analyzecontent_data, data);".Replace("{subscribe}", StringUtil.EscapeString(subscribe)));
            }
            result.Start("head")
                .Start("script").Attr("type", "text/javascript").Value(
@"function yahoo_analyzecontent_data(c, m, d) {
    $.post(d.service, { context: m.text }, function(r) {
        var response = YAHOO.lang.JSON.parse(r);
        if(typeof(response.Result) == 'string') {
            Deki.publish(d.channel, { text: response.Result });
        } else if(typeof(response.Result) == 'object') {
            Deki.publish(d.channel, { text: response.Result.slice(0, d.max).join(', ') });
        } else {
            Deki.publish('debug', { text: 'term extraction failed for: ' + m.text });
        }
    });
}"
                ).End()
                .Start("script").Attr("type", "text/javascript").Value(javascript.ToString()).End()
            .End();

            // tail
            if(!string.IsNullOrEmpty(content)) {
                result.Start("tail")
                    .Start("script").Attr("type", "text/javascript").Value("Deki.publish('{id}', { text: '{content}' });".Replace("{id}", id).Replace("{content}", StringUtil.EscapeString(content))).End()
                .End();
            }
            return result;
        }

        [DekiExtFunction(Description = "Get geocode information from a location.")]
        public Hashtable GeoLocation(
            [DekiExtParam("full address to geocode")] string location
        ) {
            return GetGeoCode(location, null, null, null, null);
        }

        [DekiExtFunction(Description = "Get geocode information from an address")]
        public Hashtable GeoCode(
            [DekiExtParam("street name with optional number", true)] string street,
            [DekiExtParam("city name", true)] string city,
            [DekiExtParam("state (US only)", true)] string state,
            [DekiExtParam("zipcode", true)] string zip
        ) {
            return GetGeoCode(null, street, city, state, zip);
        }

        //--- Features ---
        [DreamFeature("POST:proxy/extractterms", "The 'Term Extraction Web Service' provides a list of significant words or phrases extracted from a larger content.")]
        [DreamFeatureParam("content", "string", "content to extract terms from (utf-8 encoded)")]
        [DreamFeatureParam("query", "string?", "optional query to help with the extraction process")]
        public Yield PostAnalyzeContent(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            XDoc doc = request.ToDocument();

            // proxy to analysis service
            Result<DreamMessage> res;
            yield return res = Plug.New("http://search.yahooapis.com/ContentAnalysisService/V1/termExtraction").With("appid", Config["yahoo-app-id"].AsText ?? "YahooDemom").With("context", doc["context"].Contents).With("query", doc["query"].Contents).PostAsync();
            response.Return(res.Value);
        }

        //--- Methods ---
        private Hashtable GetGeoCode(string location, string street, string city, string state, string zip) {

            // fetch geocode information
            Plug plug = Plug.New("http://local.yahooapis.com/MapsService/V1/geocode").With("appid", Config["yahoo-app-id"].AsText ?? "YahooDemom")
                .With("location", location)
                .With("street", street)
                .With("city", city)
                .With("state", state)
                .With("zip", zip);
            DreamMessage response = plug.GetAsync().Wait();

            // convert result
            Hashtable result = null;
            if(response.IsSuccessful) {
                XDoc geo = response.ToDocument()["_:Result"];
                result = new Hashtable(StringComparer.CurrentCultureIgnoreCase);
                result.Add("latitude", geo["Latitude"].AsDouble);
                result.Add("longitude", geo["Longitude"].AsDouble);
                result.Add("address", geo["Address"].AsText);
                result.Add("city", geo["City"].AsText);
                result.Add("state", geo["State"].AsText);
                result.Add("zip", geo["Zip"].AsText);
                result.Add("country", geo["Country"].AsText);
            }
            return result;
        }
    }
}

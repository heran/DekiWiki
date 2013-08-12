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

    [DreamService("MindTouch Google Extension", "Copyright (c) 2006-2010 MindTouch Inc.",
        Info = "http://developer.mindtouch.com/App_Catalog/Google",
        SID = new string[] { 
            "sid://mindtouch.com/2007/06/google",
            "http://services.mindtouch.com/deki/draft/2007/06/google" 
        }
    )]
    [DreamServiceConfig("api-key", "string", "API Key for Google")]
    [DreamServiceBlueprint("deki/service-type", "extension")]
    [DekiExtLibrary(
        Label = "Google", 
        Namespace = "google", 
        Description = "This extension contains functions for embedding Google maps, charts, and widgets.",
        Logo = "$files/google-logo.png"
    )]
    [DekiExtLibraryFiles(Prefix = "MindTouch.Deki.Services.Resources", Filenames = new string[] { "google-logo.png" })]
    public class GoogleService : DekiExtService {

        //--- Properties ---
        public string GoogleApiKey { get { return Config["api-key"].AsText ?? Config["maps-api-key"].AsText; }}

        //--- Functions ---
        [DekiExtFunction(Description = "Embed calendar")]
        public XDoc Calendar(
            [DekiExtParam("Google calendar feed uri ")] XUri uri,
            [DekiExtParam("starting date (default: today)", true)] string startDate,
            [DekiExtParam("ending date (default: 7 days from today)", true)] string endDate,
            [DekiExtParam("calendar width (default: 800)", true)] float? width,
            [DekiExtParam("calendar height (default: 800)", true)] float? height
        ) {
            XDoc result = new XDoc("html");

            // try reading supplied dates
            DateTime start;
            DateTime end;
            if(!DateTime.TryParse(startDate, out start)) {
                start = DateTime.UtcNow;
            }
            if(!DateTime.TryParse(endDate, out end)) {
                end = DateTime.UtcNow.AddDays(7);
            }

            Plug calPlug = Plug.New(uri).With("start-min", start.ToString("s")).With("start-max", end.ToString("s"));
            DreamMessage response = calPlug.GetAsync().Wait();
            if(response.IsSuccessful) {
                XDoc doc = response.ToDocument();
                if(doc.HasName("feed")) {
                    XAtomFeed calFeed = new XAtomFeed(doc);
                    calFeed.UsePrefix("atom", "http://www.w3.org/2005/Atom");
                    XUri embedUri = calFeed["atom:link[@rel='alternate']/@href"].AsUri;
                    result = NewIFrame(embedUri, width ?? 800, height ?? 800);
                } else {

                    // BUGBUGBUG (steveb): user provided an embeddable representation; we won't be able to use the start and end date parameters then.

                    result = NewIFrame(uri, width ?? 800, height ?? 800);
                }
            }
            return result;
        }

        [DekiExtFunction(Description = "Embed a control to show an Atom/RSS feed")]
        [DekiExtFunctionScript("MindTouch.Deki.Services.Resources", "google-feeds.xml")]
        public XDoc Feed(
            [DekiExtParam("feed uri (Atom or RSS)")] XUri feed,
            [DekiExtParam("max number of entries to return (default: 4)", true)] int? max,
            [DekiExtParam("feed label (default: \"\")", true)] string label
        ) {
            throw new InvalidOperationException("this function is implemented as a script");
        }

        [DekiExtFunction(Description = "Embed a control to show Atom/RSS feeds")]
        [DekiExtFunctionScript("MindTouch.Deki.Services.Resources", "google-feeds.xml")]
        public XDoc Feeds(
            [DekiExtParam("list of feed objects (Atom or RSS) (e.g. [{ uri : \"http://feeds.feedburner.com/Mindtouch\", label: \"MindTouch\" }, ...]; default: [])", true)] ArrayList feeds,
            [DekiExtParam("max number of entries to return (default: 4)", true)] int? max,
            [DekiExtParam("tabbed presentation (default: false)", true)] bool? tabbed,
            [DekiExtParam("subscribe to channel (default: nil)", true)] string subscribe
        ) {
            throw new InvalidOperationException("this function is implemented as a script");
        }

        [DekiExtFunction(Description = "Publish entries of a RSS/Atom feed")]
        public XDoc FeedEntries(
            [DekiExtParam("feed uri (Atom or RSS) (default: nil)", true)] XUri feed,
            [DekiExtParam("max number of entries to read (default: 20)", true)] int? max,
            [DekiExtParam("publish on channel (default: \"default\")", true)] string publish,
            [DekiExtParam("subscribe to channel (default: nil)", true)] string subscribe
        ) {

            // check key
            string key = GoogleApiKey;
            if(string.IsNullOrEmpty(key)) {
                return new XDoc("html").Start("body").Start("span").Attr("style", "color:red;font-weight:bold;").Value("The Google API key is missing").End().End();
            }

            // initialize feed loader
            string id = StringUtil.CreateAlphaNumericKey(8);
            XDoc result = new XDoc("html");
            result.Start("head");
            result.Start("script").Attr("type", "text/javascript").Attr("src", new XUri("http://www.google.com/jsapi").With("key", key)).End();
            result.Start("script").Attr("type", "text/javascript").Value("google.load('feeds', '1');").End();
            result.Start("script").Attr("type", "text/javascript").Value(
@"function google_feed_data(c, m, d) {
    if((typeof(m.uri) != 'undefined') && (m.uri != null) && (m.uri != '')) {
        var feed = new google.feeds.Feed(m.uri);
        feed.setNumEntries(d.max);
        feed.load(function(r) {
            if(!r.error) {
                for(var i = 0; i < r.feed.entries.length; ++i) {
                    var entry = r.feed.entries[i];
                    Deki.publish(d.channel, { label: entry.title, uri: entry.link, html: entry.content, text: entry.contentSnippet, date: entry.publishedDate, name: r.feed.author });
                }
            } else {
                Deki.publish('debug', { text: 'An error occurred while retrieving the feed for uri = ' + uri + '; code = ' + r.error.code + '; message = ' + r.error.message });
            }
        });
    }
}"
            ).End();
            if(subscribe != null) {
                result.Start("script").Attr("type", "text/javascript").Value("Deki.subscribe('{subscribe}', null, google_feed_data, { max: {max}, channel: '{channel}' });".Replace("{subscribe}", StringUtil.EscapeString(subscribe)).Replace("{max}", (max ?? 20).ToString()).Replace("{channel}", StringUtil.EscapeString(publish ?? "default"))).End();
            }
            result.Start("script").Attr("type", "text/javascript").Value("Deki.subscribe('{id}', null, google_feed_data, { max: {max}, channel: '{channel}' });".Replace("{id}", id).Replace("{max}", (max ?? 20).ToString()).Replace("{channel}", StringUtil.EscapeString(publish ?? "default"))).End();
            result.End();
            if(feed != null) {
                result.Start("tail")
                    .Start("script").Attr("type", "text/javascript").Value("Deki.publish('{id}', { uri: '{feed}' });".Replace("{id}", id).Replace("{feed}", feed.ToString())).End()
                .End();
            }
            return result;
        }

        [DekiExtFunction(Description = "Publish list of RSS/Atom feeds that mach search terms")]
        [DekiExtFunctionScript("MindTouch.Deki.Services.Resources", "google-findfeeds.xml")]
        public XDoc FindFeeds(
            [DekiExtParam("search term (default: nil)", true)] string search,
            [DekiExtParam("max number of feeds to find (default: 4)", true)] int? max,
            [DekiExtParam("publish on channel (default: \"default\")", true)] string publish,
            [DekiExtParam("subscribe to channel (default: nil)", true)] string subscribe
        ) {
            throw new InvalidOperationException("this function is implemented as a script");
        }

        [DekiExtFunction(Description = "Embed map with marker")]
        [DekiExtFunctionScript("MindTouch.Deki.Services.Resources", "google-map.xml")]
        public XDoc Map(
            [DekiExtParam("street address (default: nil)", true)] string address,
            [DekiExtParam("map zoom level (1-17; default: 14)", true)] int? zoom,
            [DekiExtParam("map width (default: 450)", true)] float? width,
            [DekiExtParam("map height (default: 300)", true)] float? height,
            [DekiExtParam("title for map marker (default: nil)", true)] string title,
            [DekiExtParam("subscribe to channel (default: nil)", true)] string subscribe
        ) {
            throw new InvalidOperationException("this function is implemented as a script");
        }

        [DekiExtFunction(Description = "Embed results for web, images, videos, news, blogs, and books search")]
        [DekiExtFunctionScript("MindTouch.Deki.Services.Resources", "google-search.xml")]
        public XDoc Search(
            [DekiExtParam("search terms (default: nil)", true)] string search,
            [DekiExtParam("search options (default: { web: true, local: true, images: true, videos: true, news: true, blogs: true, books: true })", true)] Hashtable options,
            [DekiExtParam("tabbed presentation (default: false)", true)] bool? tabbed,
            [DekiExtParam("publish on channel (default: nil)", true)] string publish,
            [DekiExtParam("subscribe to channel (default: nil)", true)] string subscribe
        ) {
            throw new InvalidOperationException("this function is implemented as a script");
        }

        [DekiExtFunction(Description = "Embed blog search results")]
        public XDoc SearchBlogs(
            [DekiExtParam("search terms (default: nil)", true)] string search,
            [DekiExtParam("horizontal layout (default: false)", true)] bool? horizontal,
            [DekiExtParam("search title (default: nil)", true)] string title,
            [DekiExtParam("subscribe to channel (default: nil)", true)] string subscribe
        ) {
            // check key
            string key = GoogleApiKey;
            if(string.IsNullOrEmpty(key)) {
                return new XDoc("html").Start("body").Start("span").Attr("style", "color:red;font-weight:bold;").Value("The Google API key is missing").End().End();
            }

            // initialize blogs control
            string id = StringUtil.CreateAlphaNumericKey(8);
            string resultId = id + "_result";
            XDoc result = new XDoc("html")
                .Start("head")
                    .Start("script").Attr("type", "text/javascript").Attr("src", new XUri("http://www.google.com/uds/api?file=uds.js&v=1.0").With("key", key)).End()
                    .Start("link").Attr("type", "text/css").Attr("rel", "stylesheet").Attr("href", "http://www.google.com/uds/css/gsearch.css").End()
                    .Start("script").Attr("type", "text/javascript").Attr("src", "http://www.google.com/uds/solutions/blogbar/gsblogbar.js").End()
                    .Start("link").Attr("type", "text/css").Attr("rel", "stylesheet").Attr("href", "http://www.google.com/uds/solutions/blogbar/gsblogbar.css").End()
                    .Start("script").Attr("type", "text/javascript").Value(@"function google_ctrlexec_data(c, m, d) { if((typeof(m.text) != 'undefined') && (m.text != null) && (m.text != '')) d.ctrl.execute(m.text); }").End()
                .End();

            // body
            result.Start("body")
                .Start("div").Attr("id", id).Attr("class", "googleNewsBar").End()
                .Start("div").Attr("id", resultId).Attr("style", "display: none").End()
            .End();

            // tail
            StringBuilder javsscript = new StringBuilder();
            javsscript.AppendLine(
@"var result = document.getElementById('{resultid}');
var gsctrl = new GSblogBar(document.getElementById('{id}'), { resultStyle : GSblogBar.RESULT_STYLE_EXPANDED, largeResultSet : true, horizontal : {horizontal}, title: '{title}', currentResult: result, autoExecuteList : { cycleTime : {horizontal} ? GSblogBar.CYCLE_TIME_MEDIUM : GSblogBar.CYCLE_TIME_MANUAL, executeList: '{search}'.split(',') } });"
.Replace("{id}", id)
.Replace("{resultid}", resultId)
.Replace("{search}", StringUtil.EscapeString(search ?? string.Empty))
.Replace("{horizontal}", (horizontal ?? false).ToString().ToLowerInvariant())
.Replace("{title}", StringUtil.EscapeString(title ?? string.Empty))
            );
            if(subscribe != null) {
                javsscript.AppendLine("Deki.subscribe('{subscribe}', null, google_ctrlexec_data, { ctrl: gsctrl });".Replace("{subscribe}", StringUtil.EscapeString(subscribe)));
            }
            result.Start("tail")
                .Start("script").Attr("type", "text/javascript").Value(javsscript.ToString()).End()
            .End();
            return result;
        }

        [DekiExtFunction(Description = "Embed news search results")]
        public XDoc SearchNews(
            [DekiExtParam("search terms (default: nil)", true)] string search,
            [DekiExtParam("horizontal layout (default: false)", true)] bool? horizontal,
            [DekiExtParam("search title (default: nil)", true)] string title,
            [DekiExtParam("subscribe to channel (default: nil)", true)] string subscribe
        ) {
            // check key
            string key = GoogleApiKey;
            if(string.IsNullOrEmpty(key)) {
                return new XDoc("html").Start("body").Start("span").Attr("style", "color:red;font-weight:bold;").Value("The Google API key is missing").End().End();
            }

            // initialize news control
            string id = StringUtil.CreateAlphaNumericKey(8);
            string resultId = id + "_result";
            XDoc result = new XDoc("html")
                .Start("head")
                    .Start("script").Attr("type", "text/javascript").Attr("src", new XUri("http://www.google.com/uds/api?file=uds.js&v=1.0").With("key", key)).End()
                    .Start("link").Attr("type", "text/css").Attr("rel", "stylesheet").Attr("href", "http://www.google.com/uds/css/gsearch.css").End()
                    .Start("script").Attr("type", "text/javascript").Attr("src", "http://www.google.com/uds/solutions/newsbar/gsnewsbar.js").End()
                    .Start("link").Attr("type", "text/css").Attr("rel", "stylesheet").Attr("href", "http://www.google.com/uds/solutions/newsbar/gsnewsbar.css").End()
                    .Start("script").Attr("type", "text/javascript").Value(@"function google_ctrlexec_data(c, m, d) { if((typeof(m.text) != 'undefined') && (m.text != null) && (m.text != '')) d.ctrl.execute(m.text); }").End()
                .End();

            // body
            result.Start("body")
                .Start("div").Attr("id", id).Attr("class", "googleNewsBar").End()
                .Start("div").Attr("id", resultId).Attr("style", "display: none").End()
            .End();

            // tail
            StringBuilder javascript = new StringBuilder();
            javascript.AppendLine(
@"var result = document.getElementById('{resultid}');
var gsctrl = new GSnewsBar(document.getElementById('{id}'), { resultStyle : GSnewsBar.RESULT_STYLE_EXPANDED, largeResultSet : true, horizontal : {horizontal}, title: '{title}', currentResult: result, autoExecuteList : { cycleTime : {horizontal} ? GSnewsBar.CYCLE_TIME_MEDIUM : GSnewsBar.CYCLE_TIME_MANUAL, executeList: '{search}'.split(',') } });"
.Replace("{id}", id)
.Replace("{resultid}", resultId)
.Replace("{search}", StringUtil.EscapeString(search ?? string.Empty))
.Replace("{horizontal}", (horizontal ?? false).ToString().ToLowerInvariant())
.Replace("{title}", StringUtil.EscapeString(title ?? string.Empty))
            );
            if(subscribe != null) {
                javascript.AppendLine("Deki.subscribe('{subscribe}', null, google_ctrlexec_data, { ctrl: gsctrl });".Replace("{subscribe}", StringUtil.EscapeString(subscribe)));
            }
            result.Start("tail")
                .Start("script").Attr("type", "text/javascript").Value(javascript.ToString()).End()
            .End();
            return result;
        }

        [DekiExtFunction(Description = "Embed video search results")]
        public XDoc SearchVideos(
            [DekiExtParam("search terms (default: nil)", true)] string search,
            [DekiExtParam("video search width (default: 260)", true)] float? width,
            [DekiExtParam("video search height (default: nil)", true)] float? height,
            [DekiExtParam("subscribe to channel (default: nil)", true)] string subscribe
        ) {
            // check key
            string key = GoogleApiKey;
            if(string.IsNullOrEmpty(key)) {
                return new XDoc("html").Start("body").Start("span").Attr("style", "color:red;font-weight:bold;").Value("The Google API key is missing").End().End();
            }

            // initialize video search control
            string id = StringUtil.CreateAlphaNumericKey(8);
            XDoc result = new XDoc("html")
                .Start("head")
                    .Start("script").Attr("type", "text/javascript").Attr("src", new XUri("http://www.google.com/uds/api?file=uds.js&v=1.0").With("key", key)).End()
                    .Start("link").Attr("type", "text/css").Attr("rel", "stylesheet").Attr("href", "http://www.google.com/uds/css/gsearch.css").End()
                    .Start("script").Attr("type", "text/javascript").Attr("src", "http://www.google.com/uds/solutions/videosearch/gsvideosearch.js").End()
                    .Start("link").Attr("type", "text/css").Attr("rel", "stylesheet").Attr("href", "http://www.google.com/uds/solutions/videosearch/gsvideosearch.css").End()
                    .Start("script").Attr("type", "text/javascript").Value(@"function google_ctrlexec_data(c, m, d) { if((typeof(m.text) != 'undefined') && (m.text != null) && (m.text != '')) d.ctrl.execute(m.text); }").End()
                .End();

            // body
            StringBuilder style = new StringBuilder();
            style.AppendFormat("width:{0};", AsSize(width ?? 260));
            if(height.HasValue) {
                style.AppendFormat("height:{0};", AsSize(height));
            }
            result.Start("body")
                .Start("div").Attr("id", id).Attr("class", "googleVideoSearchCtrl").Attr("style", style.ToString()).End()
            .End();

            // tail
            StringBuilder javascript = new StringBuilder();
            javascript.AppendLine(
@"var gsctrl = new GSvideoSearchControl(document.getElementById('{id}'),[{ query: '' }],null,null,{ largeResultSet: true });
Deki.subscribe('{id}', null, google_ctrlexec_data, { ctrl: gsctrl });"
.Replace("{id}", id)
            );
            if(subscribe != null) {
                javascript.AppendLine("Deki.subscribe('{subscribe}', null, google_ctrlexec_data, { ctrl: gsctrl });".Replace("{subscribe}", StringUtil.EscapeString(subscribe)));
            }
            if(!string.IsNullOrEmpty(search)) {
                javascript.AppendLine("Deki.publish('{id}', { text: '{search}' });".Replace("{id}", id).Replace("{search}", StringUtil.EscapeString(search ?? string.Empty)));
            }
            result.Start("tail")
                .Start("script").Attr("type", "text/javascript").Value(javascript.ToString()).End()
            .End();
            return result;
        }

        [DekiExtFunction(Description = "Embed spreadsheet")]
        public XDoc Spreadsheet(
            [DekiExtParam("Google spreadsheet uri")] XUri uri,
            [DekiExtParam("spreadsheet width (default: 800)", true)] float? width,
            [DekiExtParam("spreadsheet height (default: 800)", true)] float? height
        ) {
            return NewIFrame(uri, width ?? 800, height ?? 800);
        }

        [DekiExtFunction(Description = "Embed gadget script")]
        public XDoc GadgetScript(
            [DekiExtParam("Google gadget script")] string script
        ) {

            // validate xml
            XDoc xml = XDocFactory.From("<html><body>" + script + "</body></html>", MimeType.HTML)["//script"];
            if((xml == null) || !xml.HasName("script")) {
                throw new ArgumentException("Google gadget must contained in a <script> tag");
            }

            // validate uri
            XUri uri = xml["@src"].AsUri ?? XUri.Localhost;
            if(!uri.Host.EqualsInvariantIgnoreCase("gmodules.com") && !uri.Host.EqualsInvariantIgnoreCase("www.gmodules.com")) {
                throw new ArgumentException("Google gadget must be originating from gmodules.com");
            }

            // remove .js output option
            uri = uri.WithoutParams("output");

            // create <iframe> in which we'll load the gadget
            return NewIFrame(uri, SysUtil.ChangeType<float?>(uri.GetParam("w")), SysUtil.ChangeType<float>(uri.GetParam("h")));
            
        }

        // TODO (steveb): missing chart types

        // LineXYChart
        // VennDiagram
        // ScatterPlot

        [DekiExtFunction(Description = "Embed line chart")]
        public XDoc LineChart(
            [DekiExtParam("chart width")] int width,
            [DekiExtParam("chart height")] int height,
            [DekiExtParam("chart values (e.g. [ 1, 2, 3 ])")] ArrayList values,
            [DekiExtParam("chart legends (e.g. [ \"first\", \"secong\", \"third\" ]; default: nil)", true)] ArrayList legends,
            [DekiExtParam("chart colors (e.g. [ \"ff0000\", \"00ff00\", \"0000ff\" ]; default: nil)", true)] ArrayList colors,
            [DekiExtParam("chart x-axis labels (e.g. [ \"first\", \"second\", \"third\" ]; default: [ 1, 2, 3, ... ])", true)] ArrayList xaxis,
            [DekiExtParam("chart y-axis labels (e.g. [ 0, 50, 100 ]; default: [ 0, max/2, max ])", true)] ArrayList yaxis
        ) {
            int count;
            double max;
            XUri uri = new XUri("http://chart.apis.google.com/chart")
                .With("chs", string.Format("{0}x{1}", width, height))
                .With("cht", "lc")
                .With("chd", MakeChartData(values, false, out count, out max));

            // check if we need to create a default X-axis
            if(xaxis == null) {
                xaxis = new ArrayList(count);
                for(int i = 1; i <= count; ++i) {
                    xaxis.Add(i.ToString());
                }
            }

            // check if we need to create a default Y-axis
            if(yaxis == null) {
                yaxis = new ArrayList(3);
                yaxis.Add("0");
                yaxis.Add((max / 2).ToString("#,##0.##"));
                yaxis.Add(max.ToString("#,##0.##"));
            }

            // create axis labels
            if((xaxis != null) && (yaxis != null)) {
                uri = uri.With("chxt", "x,y").With("chxl", string.Format("0:|{0}|1:|{1}", string.Join("|", AsStrings(xaxis)), string.Join("|", AsStrings(yaxis))));
            } else if(xaxis != null) {
                uri = uri.With("chxt", "x").With("chxl", string.Format("0:|{0}", string.Join("|", AsStrings(xaxis))));
            } else if(yaxis != null) {
                uri = uri.With("chxt", "y").With("chxl", string.Format("0:|{0}", string.Join("|", AsStrings(yaxis))));
            }
            if(legends != null) {
                uri = uri.With("chdl", string.Join("|", AsStrings(legends)));
            }
            if(colors != null) {
                uri = uri.With("chco", string.Join(",", AsStrings(colors)));
            }
            return new XDoc("html").Start("body").Start("img").Attr("src", uri).End().End();
        }

        [DekiExtFunction(Description = "Embed bar chart")]
        public XDoc BarChart(
            [DekiExtParam("chart width")] int width,
            [DekiExtParam("chart height")] int height,
            [DekiExtParam("chart values (e.g. [ 1, 2, 3 ] for single series, or [ [ 1, 2, 3 ], [ 4, 5, 6 ] ] for multi-series)")] ArrayList values,
            [DekiExtParam("chart legends (e.g. [ \"first\", \"second\", \"third\" ]; default: nil)", true)] ArrayList legends,
            [DekiExtParam("chart colors (e.g. [ \"ff0000\", \"00ff00\", \"0000ff\" ]; default: nil)", true)] ArrayList colors,
            [DekiExtParam("draw bars vertically (default: true)", true)] bool? vertical,
            [DekiExtParam("draw bars stacked (default: false)", true)] bool? stacked,
            [DekiExtParam("chart x-axis labels (e.g. [ \"first\", \"second\", \"third\" ]; default: [ 1, 2, 3, ... ])", true)] ArrayList xaxis,
            [DekiExtParam("chart y-axis labels (e.g. [ 0, 50, 100 ]; default: [ 0, max/2, max ])", true)] ArrayList yaxis
        ) {
            int count;
            double max;
            XUri uri = new XUri("http://chart.apis.google.com/chart")
                .With("chs", string.Format("{0}x{1}", width, height))
                .With("cht", vertical.GetValueOrDefault(true) ? (stacked.GetValueOrDefault(false) ? "bvs" : "bvg") : (stacked.GetValueOrDefault(false) ? "bhs" : "bhg"))
                .With("chd", MakeChartData(values, stacked ?? false, out count, out max));

            // check if we need to create a default X-axis
            if(xaxis == null) {
                xaxis = new ArrayList(count);
                for(int i = 1; i <= count; ++i) {
                    xaxis.Add(i.ToString());
                }
            }

            // check if we need to create a default Y-axis
            if(yaxis == null) {
                yaxis = new ArrayList(3);
                yaxis.Add("0");
                yaxis.Add((max / 2).ToString("#,##0.##"));
                yaxis.Add(max.ToString("#,##0.##"));
            }
            
            // create axis labels
            if((xaxis.Count > 0 ) && (yaxis.Count > 0)) {
                uri = uri.With("chxt", "x,y").With("chxl", string.Format("0:|{0}|1:|{1}", string.Join("|", AsStrings(xaxis)), string.Join("|", AsStrings(yaxis))));
            } else if(xaxis.Count > 0) {
                uri = uri.With("chxt", "x").With("chxl", string.Format("0:|{0}", string.Join("|", AsStrings(xaxis))));
            } else if(yaxis.Count > 0) {
                uri = uri.With("chxt", "y").With("chxl", string.Format("0:|{0}", string.Join("|", AsStrings(yaxis))));
            }
            if(legends != null) {
                uri = uri.With("chdl", string.Join("|", AsStrings(legends)));
            }
            if(colors != null) {
                uri = uri.With("chco", string.Join(",", AsStrings(colors)));
            }
            return new XDoc("html").Start("body").Start("img").Attr("src", uri).End().End();
        }

        [DekiExtFunction(Description = "Embed pie chart")]
        public XDoc PieChart(
            [DekiExtParam("chart width")] int width,
            [DekiExtParam("chart height")] int height,
            [DekiExtParam("chart values (e.g. [ 1, 2, 3 ])")] ArrayList values,
            [DekiExtParam("chart labels (e.g. [ \"first\", \"secong\", \"third\" ]; default: nil)", true)] ArrayList labels,
            [DekiExtParam("chart colors (e.g. [ \"ff0000\", \"00ff00\", \"0000ff\" ]; default: nil)", true)] ArrayList colors,
            [DekiExtParam("draw 3D chart (default: true)", true)] bool? threeD
        ) {
            int count;
            double max;
            XUri uri = new XUri("http://chart.apis.google.com/chart")
                .With("chs", string.Format("{0}x{1}", width, height))
                .With("cht", threeD.GetValueOrDefault(true) ? "p3" : "p")
                .With("chd", MakeChartData(values, false, out count, out max));
            if(labels != null) {
                uri = uri.With("chl", string.Join("|", AsStrings(labels)));
            }
            if(colors != null) {
                uri = uri.With("chco", string.Join(",", AsStrings(colors)));
            }
            return new XDoc("html").Start("body").Start("img").Attr("src", uri).End().End();
        }

        [DekiExtFunction(Description = "Embed Google-o-meter")]
        public XDoc Meter(
            [DekiExtParam("meter width")] int width,
            [DekiExtParam("meter height")] int height,
            [DekiExtParam("meter position (between 0 and 100)")] int value,
            [DekiExtParam("meter label (default: none)", true)] string label,
            [DekiExtParam("meter colors (e.g. [ \"ff0000\", \"00ff00\", \"0000ff\" ]; default: nil)", true)] ArrayList colors
        ) {
            XUri uri = new XUri("http://chart.apis.google.com/chart")
                .With("chs", string.Format("{0}x{1}", width, height))
                .With("cht", "gom")
                .With("chd", "t:" + Math.Max(0, Math.Min(value, 100)));
            if(!string.IsNullOrEmpty(label)) {
                uri = uri.With("chl", label);
            }
            if(colors != null) {
                uri = uri.With("chco", string.Join(",", AsStrings(colors)));
            }
            return new XDoc("html").Start("body").Start("img").Attr("src", uri).End().End();
        }

        private string MakeChartData(ArrayList values, bool stacked, out int count, out double max) {
            StringBuilder data = new StringBuilder();
            count = 0;
            max = 1.0;
            if(values.Count > 0) {

                // check if we have multiple data series
                if(values[0] is ArrayList) {

                    // find max value
                    max = 0;
                    int maxSeries = 0;
                    for(int i = 0; i < values.Count; ++i) {
                        ArrayList inner = values[i] as ArrayList;
                        if(inner != null) {
                            maxSeries = Math.Max(maxSeries, inner.Count);
                            if(stacked) {
                                double sum = 0.0;
                                for(int j = 0; j < inner.Count; ++j) {
                                    try {
                                        double value = SysUtil.ChangeType<double>(inner[j]);
                                        sum += value;
                                        inner[j] = value;
                                    } catch {
                                        inner[j] = null;
                                    }
                                }
                                max = Math.Max(max, sum);
                            } else {
                                for(int j = 0; j < inner.Count; ++j) {
                                    try {
                                        double value = SysUtil.ChangeType<double>(inner[j]);
                                        max = Math.Max(max, value);
                                        inner[j] = value;
                                    } catch {
                                        inner[j] = null;
                                    }
                                }
                            }
                        }
                    }

                    // just in case, let's set some max value
                    if(max == 0) {
                        max = 1;
                    }

                    // normalize values
                    for(int i = 0; i < maxSeries; ++i) {
                        bool first = true;
                        if(data.Length != 0) {
                            data.Append("|");
                        }
                        for(int j = 0; j < values.Count; ++j) {
                            if(!first) {
                                data.Append(",");
                            }
                            first = false;
                            ArrayList inner = values[j] as ArrayList;
                            if((inner != null) && (i < inner.Count) && (inner[i] != null)) {
                                data.Append(((double)inner[i] / max * 100).ToString("##0.##"));
                            } else {
                                data.Append("-1");
                            }
                        }
                    }
                } else {

                    // find max value
                    max = 0;
                    count = values.Count;
                    for(int i = 0; i < values.Count; ++i) {
                        try {
                            double value = SysUtil.ChangeType<double>(values[i]);
                            max = Math.Max(max, value);
                            values[i] = value;
                        } catch {
                            values[i] = null;
                        }
                    }

                    // just in case, let's set some max value
                    if(max == 0) {
                        max = 1;
                    }

                    // normalize values
                    for(int i = 0; i < values.Count; ++i) {
                        if(data.Length != 0) {
                            data.Append(",");
                        }
                        if(values[i] != null) {
                            data.Append(((double)values[i] / max * 100).ToString("##0.##"));
                        } else {
                            data.Append("-1");
                        }
                    }
                }
            }
            return "t:" + data.ToString();
        }

        private string[] AsStrings(ArrayList values) {
            string[] result = new string[values.Count];
            for(int i = 0; i < values.Count; ++i) {
                result[i] = SysUtil.ChangeType<string>(values[i]);
            }
            return result;
        }
    }
}

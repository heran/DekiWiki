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
using System.IO;
using System.Xml;
using System.Xml.Xsl;

using MindTouch.Dream;
using MindTouch.Tasking;
using MindTouch.Xml;

namespace MindTouch.Deki.Services {
    using Yield = IEnumerator<IYield>;

    [DreamService("MindTouch Atom/RSS Feed Extension", "Copyright (c) 2006-2010 MindTouch Inc.",
        Info = "http://developer.mindtouch.com/App_Catalog/Feed",
        SID = new string[] { 
            "sid://mindtouch.com/2007/06/feed",
            "http://services.mindtouch.com/deki/draft/2007/06/feed" 
        }
    )]
    [DreamServiceConfig("timeout", "double?", "Timeout for fetching a feed (default: 60secs)")]
    [DreamServiceConfig("cached", "double?", "Cache duration for feed (default: 0secs)")]
    [DreamServiceBlueprint("deki/service-type", "extension")]
    [DekiExtLibrary(
        Label = "Atom/RSS Feeds", 
        Namespace = "feed", 
        Description = "This extension contains functions for using Atom and RSS feeds.",
        Logo = "$files/feed-logo.png"
    )]
    [DekiExtLibraryFiles(Prefix = "MindTouch.Deki.Services.Resources", Filenames = new string[] { "feed-logo.png" })]
    public class FeedService : DekiExtService {

        //--- Class Methods ---
        private static XDoc AddTableStyles(XDoc doc) {
            XDoc head = doc["head"];
            if(head.IsEmpty) {
                doc.Elem("head");
                head = doc["head"];
            }
            head.Start("style").Attr("type", "text/css").Value(@".feedtable {
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
}").End();
            return doc;
        }

        //--- Fields ---
        private XslCompiledTransform _xslt;
        private Dictionary<XUri, XDoc> _feeds;
        private double _cached;

        //--- Functions ---
        [DekiExtFunction(Description = "Show Atom/RSS feed as a table")]
        public Yield Table(
            [DekiExtParam("feed uri (Atom or RSS)")] XUri feed,
            [DekiExtParam("max items to display (default: nil)", true)] int? max,
            Result<XDoc> result
        ) {
            Result<DreamMessage> res;
            yield return res = FetchFeed(feed).Catch();
            result.Return(GetFeed(feed, "table", res, max));
        }

        [DekiExtFunction(Description = "Show Atom/RSS feed as a list")]
        public Yield List(
            [DekiExtParam("feed uri (Atom or RSS)")] XUri feed,
            [DekiExtParam("max items to display (default: nil)", true)] int? max,
            Result<XDoc> result
        ) {
            Result<DreamMessage> res;
            yield return res = FetchFeed(feed).Catch();
            result.Return(GetFeed(feed, "list", res, max));
        }

        //--- Methods ---
        protected override Yield Start(XDoc config, Result result) {
            yield return Coroutine.Invoke(base.Start, config, new Result());

            // pre-load xsl
            XDoc doc = Plug.New("resource://mindtouch.deki.services/MindTouch.Deki.Services.Resources.rss2html.xslt").With(DreamOutParam.TYPE, MimeType.XML.FullType).Get().ToDocument();
            _xslt = new XslCompiledTransform();
            _xslt.Load(new XmlNodeReader(doc.AsXmlNode), null, null);
            _feeds = new Dictionary<XUri, XDoc>();
            _cached = config["cached"].AsDouble ?? 0.0;
            result.Return();
        }

        protected override Yield Stop(Result result) {
            _feeds = null;
            return base.Stop(result);
        }

        private Result<DreamMessage> FetchFeed(XUri uri) {

            // replace the invalid feed:// scheme with http://
            if(uri.Scheme.EqualsInvariantIgnoreCase("feed")) {
                uri = uri.WithScheme("http");
            }
            lock(_feeds) {

                // check if we have a cached result
                XDoc feed;
                if(_feeds.TryGetValue(uri, out feed)) {
                    Result<DreamMessage> result = new Result<DreamMessage>();
                    result.Return(DreamMessage.Ok(feed));
                    return result;
                }
            }
            Plug plug = Plug.New(uri, TimeSpan.FromSeconds(Config["timeout"].AsDouble ?? Plug.DEFAULT_TIMEOUT.TotalSeconds));
            return plug.GetAsync();
        }

        private XDoc GetFeed(XUri uri, string format, Result<DreamMessage> result, int? max) {
            if(result.HasException) {
                return new XDoc("html").Start("body").Value(string.Format("An error occurred while retrieving the feed ({0}).", result.Exception.Message)).End();
            }
            XDoc rss;
            if(result.Value.ContentType.IsXml) {
                rss = result.Value.ToDocument();
            } else {
                
                // NOTE (steveb): some servers return the wrong content-type (e.g. text/html or text/plain); ignore the content-type then and attempt to parse the document
                try {
                    rss = XDocFactory.From(result.Value.AsTextReader(), MimeType.XML);
                } catch(Exception e) {
                    return new XDoc("html").Start("body").Value("An error occurred while retrieving the feed (invalid xml).").End();
                }
            }
            if(rss.IsEmpty) {
                return new XDoc("html").Start("body").Value("An error occurred while retrieving the feed (no results returned).").End();
            }

            // check if result needs to be cached
            if(_cached > 0) {
                lock(_feeds) {
                    if(!_feeds.ContainsKey(uri)) {
                        _feeds[uri] = rss;
                        TaskTimer.New(TimeSpan.FromSeconds(_cached), OnTimeout, uri, TaskEnv.None);
                    }
                }
            }

            // Create format style parameter
            XsltArgumentList xslArg = new XsltArgumentList();
            xslArg.AddParam("format", String.Empty, format);
            if (max.HasValue) {
                xslArg.AddParam("max", String.Empty, max.ToString());
            }
            StringWriter writer = new StringWriter();
            _xslt.Transform(rss.AsXmlNode, xslArg, writer);
            return AddTableStyles(XDocFactory.From("<html><body>" + writer.ToString() + "</body></html>", MimeType.HTML));
        }

        private void OnTimeout(TaskTimer timer) {
            XUri uri = (XUri)timer.State;
            lock(_feeds) {
                _feeds.Remove(uri);
            }
        }
    }
}
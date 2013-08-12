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
using System.Text;

using MindTouch.Dream;
using MindTouch.IO;
using MindTouch.Tasking;
using MindTouch.Xml;

namespace MindTouch.Deki.Services {
    using Yield = IEnumerator<IYield>;

    [DreamService("MindTouch Graphviz Extension", "Copyright (c) 2006-2010 MindTouch Inc.", 
        Info = "http://developer.mindtouch.com/App_Catalog/Graphviz",
        SID = new string[] { 
            "sid://mindtouch.com/2007/06/graphviz",
            "http://services.mindtouch.com/deki/draft/2007/06/graphviz" 
        }
    )]
    [DreamServiceConfig("dot-path", "string", "Path to the 'dot' application")]
    [DreamServiceConfig("neato-path", "string", "Path to the 'neato' application")]
    [DreamServiceConfig("twopi-path", "string", "Path to the 'twopi' application")]
    [DreamServiceConfig("circo-path", "string", "Path to the 'circo' application")]
    [DreamServiceBlueprint("deki/service-type", "extension")]
    [DreamServiceBlueprint("setup/private-storage")]
    [DekiExtLibrary(
        Label = "Graphviz", 
        Namespace = "graphviz", 
        Description = "This extension contains functions for generating graphs."
    )]
    public class GraphvizService : DekiExtService {

        //--- Class Fields ---
        private static new log4net.ILog _log = LogUtils.CreateLog();

        //--- Fields ---
        private string _dot;
        private string _neato;
        private string _twopi;
        private string _circo;

        //--- Functions ---
        [DekiExtFunction(Description = "Generate graph using DOT layout", Transform = "pre")]
        public object Dot(
            [DekiExtParam("graph description")] string graph,
            [DekiExtParam("output format (one of \"png\", \"png+map\", \"svg\", or \"xdot\"; default: \"png\")", true)] string format
        ) {
            return ProcessGraph(graph, _dot, format ?? "png");
        }

        [DekiExtFunction(Description = "Generate graph using NEATO layout", Transform = "pre")]
        public object Neato(
            [DekiExtParam("graph description")] string graph,
            [DekiExtParam("output format (one of \"png\", \"png+map\", \"svg\", or \"xdot\"; default: \"png\")", true)] string format
        ) {
            return ProcessGraph(graph, _neato, format ?? "png");
        }

        [DekiExtFunction(Description = "Generate graph using TWOPI layout", Transform = "pre")]
        public object Twopi(
            [DekiExtParam("graph description")] string graph,
            [DekiExtParam("output format (one of \"png\", \"png+map\", \"svg\", or \"xdot\"; default: \"png\")", true)] string format
        ) {
            return ProcessGraph(graph, _twopi, format ?? "png");
        }

        [DekiExtFunction(Description = "Generate graph using CIRCO layout", Transform = "pre")]
        public object Circo(
            [DekiExtParam("graph description")] string graph,
            [DekiExtParam("output format (one of \"png\", \"png+map\", \"svg\", or \"xdot\"; default: \"png\")", true)] string format
        ) {
            return ProcessGraph(graph, _circo, format ?? "png");
        }

        //--- Features ---
        [DreamFeature("GET:images/{name}", "Retrieve image")]
        public Yield GetImage(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            yield return context.Relay(Storage.At(context.GetParam("name")), request, response);
        }

        //--- Methods ---
        protected override Yield Start(XDoc config, Result result) {
            yield return Coroutine.Invoke(base.Start, config, new Result());
            _dot = config["dot-path"].AsText;
            _neato = config["neato-path"].AsText;
            _twopi = config["twopi-path"].AsText;
            _circo = config["circo-path"].AsText;
            result.Return();
        }

        protected override Yield Stop(Result result) {
            _dot = null;
            _neato = null;
            _twopi = null;
            _circo = null;
            yield return Coroutine.Invoke(base.Stop, new Result());
            result.Return();
        }

        private object ProcessGraph(string graph, string app, string format) {
            if(string.IsNullOrEmpty(graph)) {
                throw new DreamBadRequestException("invalid graph");
            }
            if(string.IsNullOrEmpty(app) || !File.Exists(app)) {
                throw new DreamBadRequestException("application misconfigured or missing");
            }
            if(format == null) {
                throw new ArgumentNullException("format");
            }

            // determined the output format
            switch(format.ToLowerInvariant()) {
            case "png":
                return GetResoucePlug(graph, app, format, MimeType.PNG).Uri;
            case "png+map": {
                    string id = StringUtil.CreateAlphaNumericKey(8);

                    // TODO (steveb): dot -Tcmapx -oTEMPFILE.map -Tgif -oTEMPFILE.png < source

                    // generate map file and replace attributes
                    XDoc map = XDocFactory.From(GetResoucePlug(graph, app, "cmapx", MimeType.XML).Get().AsText(), MimeType.XML);
                    map["@id"].ReplaceValue(id);
                    map["@name"].ReplaceValue(id);

                    // create composite result
                    return new XDoc("html")
                        .Start("body")
                            .Start("img")
                                .Attr("src", GetResoucePlug(graph, app, "png", MimeType.PNG).Uri)
                                .Attr("usemap", "#" + id)
                            .End()
                            .Add(map)
                        .End();
                }
            case "svg":
                return new XDoc("html")
                    .Start("body")
                        .Start("embed")
                            .Attr("src", GetResoucePlug(graph, app, format, MimeType.SVG))
                            .Attr("type", MimeType.SVG.FullType)
                            .Attr("pluginspage", "http://www.adobe.com/svg/viewer/install/")
                        .End()
                    .End();
            case "xdot":
                return GetResoucePlug(graph, app, format, MimeType.TEXT).Get().AsText();
            default:
                throw new DreamBadRequestException(string.Format("unknown format: {0}",  format));
            }
        }

        private Plug GetResoucePlug(string graph, string app, string format, MimeType mime) {
            string hash = StringUtil.ComputeHashString(graph);
            string key = string.Format("{0}.{1}.{2}", hash, Path.GetFileNameWithoutExtension(app), format);
            Plug location = Storage.At(key);

            // check if the target resource needs to be generated
            if(!location.InvokeAsync("HEAD", DreamMessage.Ok()).Wait().IsSuccessful) {
                Stream data;
                using(Stream input = new MemoryStream(Encoding.UTF8.GetBytes(graph))) {

                    // execute process
                    Stream output;
                    Stream error;
                    Tuplet<int, Stream, Stream> exitValues = Async.ExecuteProcess(app, "-T" + format, input, new Result<Tuplet<int, Stream, Stream>>(TimeSpan.FromSeconds(30))).Wait();
                    int status = exitValues.Item1;
                    using(output = exitValues.Item2) {
                        using(error = exitValues.Item3) {
                            if(status != 0) {
                                string message;
                                using(StreamReader reader = new StreamReader(error, Encoding.ASCII)) {
                                    message = reader.ReadToEnd();
                                }
                                throw new DreamInternalErrorException(string.Format("graphviz failed with status {0}:\n{1}", status, message));
                            }
                            data = output.ToChunkedMemoryStream(-1, new Result<ChunkedMemoryStream>()).Wait();
                        }
                    }
                }

                // create result
                location.With("ttl", TimeSpan.FromDays(7).TotalSeconds).Put(DreamMessage.Ok(mime, data.Length, data));
            }
            return Self.At("images", key);
        }
    }
}
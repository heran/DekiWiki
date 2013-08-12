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
using MindTouch.Tasking;
using MindTouch.Xml;

namespace MindTouch.Deki.Services {
    using Yield = IEnumerator<IYield>;

    [DreamService("MindTouch Math Extension", "Copyright (c) 2006-2010 MindTouch Inc.",
        Info = "http://developer.mindtouch.com/App_Catalog/Math",
        SID = new string[] { 
            "sid://mindtouch.com/2007/06/math",
            "http://services.mindtouch.com/deki/draft/2007/06/math" 
        }
    )]
    [DreamServiceConfig("latex-path", "string", "Path to latex application")]
    [DreamServiceConfig("imagemagick-path", "string", "Path to imagemagick application")]
    [DreamServiceConfig("dvips-path", "string", "Path to dvips application")]
    [DreamServiceBlueprint("deki/service-type", "extension")]
    [DreamServiceBlueprint("setup/private-storage")]
    [DekiExtLibrary(
        Label = "Math", 
        Namespace = "math", 
        Description = "This extension contains functions for rendering and computing mathematical expressions."
    )]
    public class MathService : DekiExtService {

        //--- Class Fields ---
        private static string[] _blacklist = new string[] {
            "include",
            "def",
            "command",
            "loop",
            "repeat",
            "open",
            "toks",
            "output",
            "input",
            "catcode",
            "name",
            "^^",
            "\\every",
            "\\errhelp",
            "\\errorstopmode",
            "\\scrollmode",
            "\\nonstopmode",
            "\\batchmode",
            "\\read",
            "\\write",
            "csname",
            "\\newhelp",
            "\\uppercase", 
            "\\lowercase",
            "\\relax",
            "\\aftergroup",
            "\\afterassignment",
            "\\expandafter",
            "\\noexpand",
            "\\special"
        };
        

        //--- Fields ---
        private string _latex;
        private string _imagemagick;
        private string _dvips;

        //--- Functions ---
        [DekiExtFunction(Description = "Render math formula", Transform = "pre")]
        public XUri Formula(
            [DekiExtParam("formula in Latex-AMS notation")] string formula
        ) {
            if(string.IsNullOrEmpty(_latex) || !File.Exists(_latex)) {
                throw new DreamBadRequestException("latex misconfigured or missing");
            }
            if(string.IsNullOrEmpty(_imagemagick) || !File.Exists(_imagemagick)) {
                throw new DreamBadRequestException("imagemagick misconfigured or missing");
            }
            if(string.IsNullOrEmpty(_dvips) || !File.Exists(_dvips)) {
                throw new DreamBadRequestException("dvips misconfigured or missing");
            }

            // check if equation is too long
            if(formula.Length >= 1024) {
                throw new DreamBadRequestException("formula is too long");
            }

            // check if we already have a response
            string key = new Guid(StringUtil.ComputeHash(formula)).ToString();
            string filekey = key + ".png";
            Plug location = Storage.At(filekey);
            if(location.InvokeAsync("HEAD", DreamMessage.Ok()).Wait().IsSuccessful) {
                return Self.At("images", filekey);
            }

            // check if equation contains blacklisted words
            foreach(string word in _blacklist) {
                if(StringUtil.IndexOfInvariantIgnoreCase(formula, word) >= 0) {
                    throw new DreamBadRequestException(string.Format("illegal content in formula ({0})", word));
                }
            }

            string texFile = Path.Combine(Path.GetTempPath(), key + ".tex");
            string dviFile = Path.Combine(Path.GetTempPath(), key + ".dvi");
            string psFile = Path.Combine(Path.GetTempPath(), key + ".ps");
            string pngFile = Path.Combine(Path.GetTempPath(), key + ".png");
            try {
                // create tex file
                File.WriteAllText(texFile, WrapInLatex(formula));

                // convert latex to dvi
                Process("latex", _latex, string.Format("--interaction=nonstopmode \"--aux-directory={1}\" \"--output-directory={1}\" \"{0}\"", texFile, Path.GetTempPath().TrimEnd('/', '\\')));

                // convert dvi to ps
                Process("dvips", _dvips, string.Format("-q -E \"{0}\" -o \"{1}\"", dviFile, psFile));

                // convert ps to image
                Process("imagemagick", _imagemagick, string.Format("-density 120 -trim -transparent \"#FFFFFF\" \"{0}\" \"{1}\"", psFile, pngFile));

                // store resulting image
                location.With("ttl", TimeSpan.FromDays(7).TotalSeconds).Put(DreamMessage.Ok(MimeType.PNG, File.ReadAllBytes(pngFile)));

                // keep copy of produced output
                return Self.At("images", filekey);
            } finally {
                File.Delete(texFile);
                File.Delete(dviFile);
                File.Delete(psFile);
                File.Delete(pngFile);
                File.Delete(Path.Combine(Path.GetTempPath(), key + ".aux"));
                File.Delete(Path.Combine(Path.GetTempPath(), key + ".log"));
            }
        }

        [DekiExtFunction(Description="Embed an InstaCalc sheet")]
        public XDoc Sheet(
            [DekiExtParam("uri to InstaCalc sheet")] XUri sheet, 
            [DekiExtParam("width of sheet (default: 425)", true)] float? width,
            [DekiExtParam("height of sheet (default: 300)", true)] float? height
        ) {
            return NewIFrame(new XUri("http://instacalc.com/embed").WithParamsFrom(sheet), width ?? 425,height ?? 300);
        }

        //--- Features ---
        [DreamFeature("GET:images/{name}", "Retrieve image")]
        public Yield GetImage(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            string image = context.GetParam("name");
            if(Path.GetExtension(image) != ".png") {
                response.Return(DreamMessage.NotFound("image not found"));
            } else {
                yield return context.Relay(Storage.At(image), request, response);
            }
        }

        //--- Methods ---
        protected override Yield Start(XDoc config, Result result) {
            yield return Coroutine.Invoke(base.Start, config, new Result());
            _latex = config["latex-path"].AsText;
            _imagemagick = config["imagemagick-path"].AsText;
            _dvips = config["dvips-path"].AsText;
            result.Return();
        }

        protected override Yield Stop(Result result) {
            _latex = null;
            _imagemagick = null;
            _dvips = null;
            yield return Coroutine.Invoke(base.Stop, new Result());
            result.Return();
        }

        private string WrapInLatex(string formula) {
            StringBuilder result = new StringBuilder();
            result.Append("\\documentclass{article}\n");
            result.Append("\\usepackage{amsmath}\n");
            result.Append("\\usepackage{amsfonts}\n");
            result.Append("\\usepackage{amssymb}\n");
            result.Append("\\pagestyle{empty}\n");
            result.Append("\\begin{document}\n");
            result.Append("$" + formula + "$\n");
            result.Append("\\end{document}\n");
            return result.ToString();
        }

        private void Process(string hint, string application, string cmdline) {
            Tuplet<int, Stream, Stream> exitValues = Async.ExecuteProcess(application, cmdline, Stream.Null, new Result<Tuplet<int,Stream,Stream>>(TimeSpan.FromSeconds(30))).Wait();
            int status = exitValues.Item1;
            Stream output = exitValues.Item2;
            Stream error = exitValues.Item3;
            if((status < 0) || (status >= 2)) {
                string message;
                using(StreamReader reader = new StreamReader(error, Encoding.ASCII)) {
                    message = reader.ReadToEnd();
                }
                throw new DreamAbortException(DreamMessage.InternalError(string.Format("{0} failed with status {1}:\n{2}", hint, status, message)));
            }
        }
    }
}

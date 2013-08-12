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

using MindTouch.Dream;
using MindTouch.Tasking;
using MindTouch.Xml;

namespace MindTouch.Deki.Services {
    using Yield = IEnumerator<IYield>;

    [DreamService("MindTouch Silverlight Extension", "Copyright (c) 2006-2010 MindTouch Inc.",
        Info = "http://developer.mindtouch.com/App_Catalog/Silverlight",
        SID = new string[] { 
            "sid://mindtouch.com/2008/02/silverlight",
            "http://services.mindtouch.com/deki/draft/2008/02/silverlight" 
        }
    )]
    [DreamServiceBlueprint("deki/service-type", "extension")]
    [DreamServiceBlueprint("setup/private-storage")]
    [DekiExtLibrary(
        Label = "Silverlight",
        Namespace = "silverlight",
        Description = "This extension contains functions for embedding Microsoft Silverlight content.",
        Logo = "$files/silverlight-logo.png"
    )]
    [DekiExtLibraryFiles(Prefix = "MindTouch.Deki.Services.Resources", Filenames = new string[] { "silverlight-logo.png", "Silverlight.js" })]
    public class SilverlightService : DekiExtService {

        //--- Constants ---
        private static readonly MimeType XAML = new MimeType("application/xaml");

        //--- Functions ---
        [DekiExtFunction(Description = "Embed a Silverlight control from source text or source uri.", Transform = "pre")]
        public XDoc Embed(
            [DekiExtParam("Silverlight source code or source URI")] string source,
            [DekiExtParam("control width (default: 100%)", true)] float? width,
            [DekiExtParam("control height (default: 250)", true)] float? height,
            [DekiExtParam("Silverlight version (default: '1.0')", true)] string version,
            [DekiExtParam("control background color (default: body background color)", true)] string backgroundcolor,
            [DekiExtParam("window-less mode (default: true)", true)] bool? windowless
        ) {
            XUri uri = XUri.TryParse(source);

            // check if we need to convert the text into an attachment
            if(uri == null) {
                string filename = new Guid(StringUtil.ComputeHash(source)).ToString() + ".xaml";
                Plug location = Storage.At(filename);
                if(!location.InvokeAsync("HEAD", DreamMessage.Ok()).Wait().IsSuccessful) {

                    // validate document
                    XDoc doc = XDocFactory.From(source, MimeType.XML);
                    if((doc == null) || doc.IsEmpty) {
                        throw new ArgumentException("source is not an XML document", "source");
                    }
                    location.With("ttl", TimeSpan.FromDays(7).TotalSeconds).Put(DreamMessage.Ok(XAML, source));
                }
                uri = DreamContext.Current.AsPublicUri(Self.At("xaml", filename));
            }

            // create the embedded silverlight control
            string div = StringUtil.CreateAlphaNumericKey(8);
            string id = StringUtil.CreateAlphaNumericKey(8);
            XDoc result = new XDoc("html")
                .Start("head")
                    .Start("script").Attr("type", "text/javascript").Attr("src", Files.At("Silverlight.js")).End()
                .End()
                .Start("body")
                    .Start("div").Attr("id", div)
                        .Start("script").Attr("type", "text/javascript").Value(string.Format(@"Silverlight.createObject('{0}', document.getElementById('{1}'), '{2}', {{ width: '{3}', height: '{4}', version: '{5}', background: {6}, isWindowless: '{7}' }}, {{}});", uri, div, id, AsSize(width ?? 0.999f), AsSize(height ?? 250), version ?? "1.0", (backgroundcolor != null) ? StringUtil.QuoteString(backgroundcolor) : "document.body.style.backgroundColor", windowless ?? true)).End()
                    .End()
                .End();
            return result;
        }

        //--- Features ---
        [DreamFeature("GET:xaml/{name}", "Retrieve XAML.")]
        public Yield GetImage(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            string image = context.GetParam("name");
            if(Path.GetExtension(image) != ".xaml") {
                response.Return(DreamMessage.NotFound("xaml not found"));
            } else {
                yield return context.Relay(Storage.At(image), request, response);
            }
        }
    }
}

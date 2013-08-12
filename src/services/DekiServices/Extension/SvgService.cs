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

    [DreamService("MindTouch SVG Extension", "Copyright (c) 2006-2010 MindTouch Inc.",
        Info = "http://developer.mindtouch.com/App_Catalog/Svg",
        SID = new string[] { 
            "sid://mindtouch.com/2008/05/svg"
        }
    )]
    [DreamServiceBlueprint("deki/service-type", "extension")]
    [DreamServiceBlueprint("setup/private-storage")]
    [DekiExtLibrary(
        Label = "SVG",
        Description = "This extension embeds SVG images.",
        Logo = "$files/svg-logo.png"
    )]
    [DekiExtLibraryFiles(Prefix = "MindTouch.Deki.Services.Resources", Filenames = new string[] { "svg-logo.png" })]
    public class SvgService : DekiExtService {

        //--- Functions ---
        [DekiExtFunction(Description = "Embed a SVG image from source text or source uri.", Transform = "pre")]
        public XDoc Svg(
            [DekiExtParam("SVG source code or source URI")] string source,
            [DekiExtParam("image width (default: 100%)", true)] float? width,
            [DekiExtParam("image height (default: 250)", true)] float? height,
            [DekiExtParam("embedding style (one of \"embed\", \"iframe\"; default: \"iframe\")", true)] string embedding
        ) {
            XUri uri = XUri.TryParse(source);

            // check if we need to convert the text into an attachment
            if(uri == null) {
                string filename = new Guid(StringUtil.ComputeHash(source)).ToString() + ".svg";
                Plug location = Storage.At(filename);
                if(!location.InvokeAsync("HEAD", DreamMessage.Ok()).Wait().IsSuccessful) {

                    // validate document
                    XDoc doc = XDocFactory.From(source, MimeType.XML);
                    if((doc == null) || doc.IsEmpty) {
                        throw new ArgumentException("source is not an XML document", "source");
                    }
                    location.With("ttl", TimeSpan.FromDays(7).TotalSeconds).Put(DreamMessage.Ok(MimeType.SVG, source));
                }
                uri = DreamContext.Current.AsPublicUri(Self.At("images", filename));
            }

            // create the embedded svg control
            XDoc result;
            switch((embedding ?? "iframe").ToLowerInvariant()) {
            case "iframe":
                result = NewIFrame(uri, width ?? 0.999f, height ?? 250);
                break;
            case "embed":
            default:
                result = new XDoc("html")
                    .Start("body")
                        .Start("embed")
                            .Attr("src", uri)
                            .Attr("width", AsSize(width ?? 0.999f))
                            .Attr("height", AsSize(height ?? 250))
                            .Attr("type", MimeType.SVG.FullType)
                            .Attr("pluginspage", "http://www.adobe.com/svg/viewer/install/")
                        .End()
                    .End();
                break;
            }
            return result;
        }

        //--- Features ---
        [DreamFeature("GET:images/{name}", "Retrieve SVG file.")]
        public Yield GetImage(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            string image = context.GetParam("name");
            if(Path.GetExtension(image) != ".svg") {
                response.Return(DreamMessage.NotFound("svg not found"));
            } else {
                yield return context.Relay(Storage.At(image), request, response);
            }
        }
    }
}

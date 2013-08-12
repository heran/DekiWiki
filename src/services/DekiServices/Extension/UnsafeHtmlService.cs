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

using System.Collections.Generic;

using MindTouch.Dream;
using MindTouch.Tasking;
using MindTouch.Xml;

namespace MindTouch.Deki.Services {
    using Yield = IEnumerator<IYield>;

    [DreamService("MindTouch Unsafe HTML Extension", "Copyright (c) 2006-2010 MindTouch Inc.",
        Info = "http://developer.mindtouch.com/App_Catalog/UnsafeHtml",
        SID = new string[] { 
            "sid://mindtouch.com/2007/06/html.unsafe",
            "http://services.mindtouch.com/deki/draft/2007/06/html.unsafe" 
        }
    )]
    [DreamServiceBlueprint("deki/service-type", "extension")]
    [DekiExtLibrary(
        Label = "Unsafe HTML", 
        Namespace = "unsafe", 
        Description = "This extension contains functions for embedding unsafe HTML."
    )]
    public class UnsafeHtmlService : DekiExtService {

        // TODO (steveb): add support for  <applet>, <embed>, <object>

        //--- Functions ---
        [DekiExtFunction(Description = "Embed custom HTML code", Transform = "pre")]
        public XDoc Html(
            [DekiExtParam("an inner html fragment")] string innerHtml
        ) {
            return XDocFactory.From("<html><body>" + innerHtml + "</body></html>", MimeType.HTML);
        }

        [DekiExtFunction(Description = "Embed iframe element")]
        public XDoc IFrame(
            [DekiExtParam("source uri")] XUri uri,
            [DekiExtParam("width (default: 425)", true)] float? width,
            [DekiExtParam("height (default: 350)", true)] float? height,
            [DekiExtParam("border (default: false)", true)] bool? border,
            [DekiExtParam("marginwidth (default: 0)", true)] int? marginwidth,
            [DekiExtParam("marginheight (default: 0)", true)] int? marginheight,
            [DekiExtParam("scrolling (default: false)", true)] bool? scrolling
        ) {
            return new XDoc("html").Start("body").Start("iframe")
                .Attr("width", AsSize(width ?? 425))
                .Attr("height", AsSize(height ?? 350))
                .Attr("frameborder", (border ?? false) ? 1 : 0)
                .Attr("marginwidth", marginwidth ?? 0)
                .Attr("marginheight", marginheight ?? 0)
                .Attr("scrolling", (scrolling ?? false) ? "yes" : "no")
                .Attr("src", uri)
            .End().End();
        }

        [DekiExtFunction(Description = "Embed script", Transform = "pre")]
        public XDoc Script(
            [DekiExtParam("script code or uri")] string codeOrUri,
            [DekiExtParam("script type (default: 'text/javascript')", true)] string type
        ) {
            XUri source = XUri.TryParse(codeOrUri);
            XDoc result;
            if(source != null) {
                result = new XDoc("html")
                    .Start("body")
                        .Start("script").Attr("type", type ?? "text/javascript").Attr("src", source).End()
                    .End();
            } else {
                result = new XDoc("html")
                    .Start("body")
                        .Start("script").Attr("type", type ?? "text/javascript").Value(codeOrUri).End()
                    .End();
            }
            return result;
        }
    }
}

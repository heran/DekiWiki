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
using System.IO;
using System.Text;

using MindTouch.Dream;
using MindTouch.Tasking;
using MindTouch.Xml;

namespace MindTouch.Deki.Services {
    using Yield = IEnumerator<IYield>;

    [DreamService("MindTouch ImageMagick Extension", "Copyright (c) 2006-2010 MindTouch Inc.",
        Info = "http://developer.mindtouch.com/App_Catalog/ImageMagick",
        SID = new string[] { 
            "sid://mindtouch.com/2007/06/imagemagick",
            "http://services.mindtouch.com/deki/draft/2007/06/imagemagick" 
        }
    )]
    [DreamServiceConfig("imagemagick-path", "string", "Path to imagemagick application")]
    [DreamServiceBlueprint("deki/service-type", "extension")]
    [DreamServiceBlueprint("setup/private-storage")]
    [DekiExtLibrary(
        Label = "ImageMagick", 
        Namespace = "image", 
        Description = "This extension contains functions for manipulating images."
    )]
    [DekiExtLibraryFiles(Prefix = "MindTouch.Deki.Services.Resources", Filenames = new string[] { "slideshow.css", "slideshow.js" })]
    public class ImageMagickService : DekiExtService {

        //--- Properties ---
        public string ImageMagick { get { return Config["imagemagick-path"].AsText; } }

        //--- Functions ---
        [DekiExtFunction(Description = "Generate image with white border and optional sub-title")]
        public XUri Polaroid(
            [DekiExtParam("image uri")] XUri image, 
            [DekiExtParam("rotation angle (default: 3)", true)] int? angle,
            [DekiExtParam("caption (default: nil)", true)] string caption
        ) {
            if(caption != null) {
                return Process(image, string.Format("-caption {0} -size 500x500 - -gravity center -background gray20 -polaroid {1}", StringUtil.QuoteString(caption), angle ?? 3));
            } else {
                return Process(image, string.Format("- -background gray20 -polaroid {0}", angle ?? 3));
            }
        }

        [DekiExtFunction(Description = "Resize image to arbitrary dimensions")]
        public XUri Resize(
            [DekiExtParam("image uri")] XUri image,
            [DekiExtParam("width (max: 500)", true)] int width,
            [DekiExtParam("height (max: 500)", true)] int height
        ) {
            return Process(image, string.Format("-size 500x500 - -resize {0}x{1}!", Math.Min(width, 500), Math.Min(height, 500)));
        }

        [DekiExtFunction(Description = "Fit image to dimensions and preserve its aspect ratio")]
        public XUri FitToSize(
            [DekiExtParam("image uri")] XUri image,
            [DekiExtParam("width (max: 500)", true)] int width,
            [DekiExtParam("height (max: 500)", true)] int height
        ) {
            return Process(image, string.Format("-size 500x500 - -resize {0}x{1}", Math.Min(width, 500), Math.Min(height, 500)));
        }

        [DekiExtFunction(Description = "Generate image with a soft oval border")]
        public XUri Vignette(
            [DekiExtParam("image uri")] XUri image,
            [DekiExtParam("radius (default: 0)", true)] double? radius,
            [DekiExtParam("sigma (default: 4)", true)] double? sigma
        ) {
            return Process(image, string.Format("-size 500x500 - -matte -background none -vignette {0}x{1}", radius ?? 0, sigma ?? 4));
        }

        [DekiExtFunction(Description = "Generate image with a soft rectangular border")]
        public XUri Blend(
            [DekiExtParam("image uri")] XUri image
        ) {
            return Process(image, "-size 500x500 - -matte -virtual-pixel transparent -channel A -blur 0x8 -evaluate subtract 50% -evaluate multiply 2.001");
        }

        [DekiExtFunction(Description = "Generate image with a raised border")]
        public XUri Raise(
            [DekiExtParam("image uri")] XUri image,
            [DekiExtParam("border width (default: 8)", true)] int? width
        ) {
            return Process(image, string.Format("-size 500x500 - -raise {0}x{0}", width ?? 8));
        }

        [DekiExtFunction(Description = "Generate image with a sunken border")]
        public XUri Sink(
            [DekiExtParam("image uri")] XUri image,
            [DekiExtParam("border width (default: 8)", true)] int? width
        ) {
            return Process(image, string.Format("-size 500x500 - +raise {0}x{0}", width ?? 8));
        }

        [DekiExtFunction(Description = "Flip image vertically")]
        public XUri Flip(
            [DekiExtParam("image uri")] XUri image
        ) {
            return Process(image, "-size 500x500 - -flip");
        }

        [DekiExtFunction(Description = "Flip image horizontally")]
        public XUri Flop(
            [DekiExtParam("image uri")] XUri image
        ) {
            return Process(image, "-size 500x500 - -flop");
        }

        [DekiExtFunction(Description = "Rotate image")]
        public XUri Rotate(
            [DekiExtParam("image uri")] XUri image,
            [DekiExtParam("rotation angle", true)] double angle
        ) {
            if(angle == 0.0) {
                return new XUri(image);
            }
            return Process(image, string.Format("-size 500x500 - -matte -background none -rotate {0}", angle));
        }

        [DekiExtFunction(Description = "Generate image with a wave")]
        public XUri Wave(
            [DekiExtParam("image uri")] XUri image,
            [DekiExtParam("wave amplitude (default: 5)", true)] int? amplitude,
            [DekiExtParam("wave length (default: 50)", true)] int? length
        ) {
            return Process(image, string.Format("-size 500x500 - -matte -background none -wave {0}x{1}", amplitude ?? 5, length ?? 50));
        }

        [DekiExtFunction(Description = "Twist the image from the center outward")]
        public XUri Swirl(
            [DekiExtParam("image uri")] XUri image,
            [DekiExtParam("swirl angle (default: 45)", true)] double? angle
        ) {
            return Process(image, string.Format("-size 500x500 - -swirl {0}", angle ?? 45.0));
        }

        [DekiExtFunction(Description = "Blur image")]
        public XUri Blur(
            [DekiExtParam("image uri")] XUri image,
            [DekiExtParam("radius (default: 0)", true)] double? radius,
            [DekiExtParam("sigma (default: 1)", true)] double? sigma
        ) {
            return Process(image, string.Format("-size 500x500 - -blur {0}x{1}", radius ?? 0, sigma ?? 1));
        }

        [DekiExtFunction(Description = "Generate embossed image")]
        public XUri Emboss(
            [DekiExtParam("image uri")] XUri image,
            [DekiExtParam("radius (default: 1)", true)] double? radius
        ) {
            return Process(image, string.Format("-size 500x500 - -emboss {0}", radius ?? 1));
        }

        [DekiExtFunction(Description = "Simulate image painted with color blobs")]
        public XUri Paint(
            [DekiExtParam("image uri")] XUri image,
            [DekiExtParam("radius (default: 1)", true)] double? radius
        ) {
            return Process(image, string.Format("-size 500x500 - -paint {0}", radius ?? 1));
        }

        [DekiExtFunction(Description = "Simulate image painted with charcoal")]
        public XUri Charcoal(
            [DekiExtParam("image uri")] XUri image,
            [DekiExtParam("kernel size, must be an odd number (default: 3)", true)] int? kernel
        ) {
            return Process(image, string.Format("-size 500x500 - -charcoal {0}", kernel ?? 3));
        }

        [DekiExtFunction(Description = "Generate outline of image")]
        public XUri Outline(
            [DekiExtParam("image uri")] XUri image
        ) {
            return Process(image, "-size 500x500 - -background white -flatten -colorspace Gray -negate -edge 1 -negate -normalize -threshold 50% -despeckle -blur 0x.5 -contrast-stretch 0x50%");
        }

        [DekiExtFunction(Description = "Embed a slideshow of images")]
        public XDoc SlideShow(
            [DekiExtParam("list of image URIs")] ArrayList uris,
            [DekiExtParam("slideshow width (default: 300px)", true)] float? width,
            [DekiExtParam("slideshow height (default: 300px)", true)] float? height,
            [DekiExtParam("interval in seconds (default: 5.0)", true)] double? interval,
            [DekiExtParam("slideshow effect; one of {slideright, slideleft, slideup, squeezeleft, squeezeright, squeezeup, squeezedown, fadeout} (default: 'fadeout')", true)] string effect
        ) {

            // convert URIs to XUri objects
            XUri[] xuris = new XUri[uris.Count];
            for(int i = 0; i < uris.Count; ++i) {
                xuris[i] = SysUtil.ChangeType<XUri>(uris[i]);
                if(xuris[i] == null) {
                    throw new ArgumentNullException(string.Format("entry {0} is null", i));
                }
            }

            // create response document
            string id = "_" + StringUtil.CreateAlphaNumericKey(8);
            XDoc result = new XDoc("html")
                .Start("head")
                    .Start("link").Attr("type", "text/css").Attr("rel", "stylesheet").Attr("href", Files.At("slideshow.css")).End()
                    .Start("script").Attr("type", "text/javascript").Attr("src", Files.At("slideshow.js")).End()
                .End();
            result.Start("body")
                .Start("div").Attr("id", id).Attr("class", "yui-sldshw-displayer").Attr("style", string.Format("width:{0};height:{1};", AsSize(width ?? 300), AsSize(height ?? 300)));

            // add each image
            int counter = 0;
            foreach(XUri uri in xuris) {
                ++counter;
                result.Start("img");
                result.Attr("id", id + counter);
                if(counter == 1) {
                    result.Attr("class", "yui-sldshw-active yui-sldshw-frame");
                } else {
                    result.Attr("class", "yui-sldshw-cached yui-sldshw-frame");
                }
                result.Attr("src", uri).Attr("onclick", string.Format("{0}.transition();", id)).End();
            }
            result.End().End();

            // add code to kick star the slideshow
            result.Start("tail")
                .Start("script").Attr("type", "text/javascript")
                    .Value(string.Format("var {0} = new YAHOO.myowndb.slideshow('{0}', {{ interval: {1}, effect: YAHOO.myowndb.slideshow.effects.{2} }}); {0}.loop();", id, (int)((interval ?? 5.0) * 1000), (effect ?? "fadeOut").ToLowerInvariant()))
                .End();
            result.End();
            return result;
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
        private XUri Process(XUri image, string options) {
            return Coroutine.Invoke(Process, image, options, new Result<XUri>()).Wait();
        }

        private Yield Process(XUri image, string options, Result<XUri> result) {

            // TODO (steveb):
            //  1) check if the fetched image has caching information
            //  2) if it does, check if it changed since last operation
            //  3) generate MD5 from image (not from uri)
            //  4) respond with new uri

            if(image == null) {
                throw new DreamBadRequestException("invalid image uri");
            }

            // check if we have a cached response
            string key = new Guid(StringUtil.ComputeHash(image.ToString() + " " + options)).ToString() + ".png";
            Plug location = Storage.At(key);
            Result<DreamMessage> res;
            yield return res = location.InvokeAsync("HEAD", DreamMessage.Ok());
            if(res.Value.IsSuccessful) {
                result.Return(Self.At("images", key));
                yield break;
            }

            // check if image-magick is properly configured
            if(string.IsNullOrEmpty(ImageMagick) || !File.Exists(ImageMagick)) {
                throw new DreamBadRequestException("imagemagick misconfigured or missing");
            }

            // fetch image
            res = new Result<DreamMessage>();
            Plug.New(image).InvokeEx("GET", DreamMessage.Ok(), res);
            yield return res;
            if(!res.Value.IsSuccessful) {
                res.Value.Close();
                throw new DreamAbortException(DreamMessage.NotFound("could not retrieve image"));
            }

            // invoke image conversion
            Result<Tuplet<int, Stream, Stream>> exitRes = new Result<Tuplet<int, Stream, Stream>>(TimeSpan.FromSeconds(30));
            using(Stream input = res.Value.AsStream()) {
                yield return Async.ExecuteProcess(ImageMagick, string.Format("{0} {1}:-", options, "png"), input, exitRes);
            }

            // check outcome
            int status = exitRes.Value.Item1;
            Stream output = exitRes.Value.Item2;
            Stream error = exitRes.Value.Item3;
            if(status != 0) {
                string message;
                using(StreamReader reader = new StreamReader(error, Encoding.ASCII)) {
                    message = reader.ReadToEnd();
                }
                throw new DreamAbortException(DreamMessage.InternalError(string.Format("operation failed with status {0}:\n{1}", status, message)));
            }

            // create result
            yield return res = location.With("ttl", TimeSpan.FromDays(7).TotalSeconds).PutAsync(DreamMessage.Ok(MimeType.PNG, output.Length, output));
            if(!res.Value.IsSuccessful) {
                throw new DreamAbortException(new DreamMessage(res.Value.Status, null, res.Value.ContentType, res.Value.AsBytes()));
            }
            result.Return(Self.At("images", key));
        }
    }
}

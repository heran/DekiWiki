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
using log4net;
using MindTouch.Deki.Script.Expr;
using MindTouch.Dream;
using MindTouch.Tasking;
using MindTouch.Xml;

namespace MindTouch.Deki.Services {
    using Yield = IEnumerator<IYield>;

    [DreamService("MindTouch Media Extension", "Copyright (c) 2006-2010 MindTouch Inc.",
        Info = "http://developer.mindtouch.com/App_Catalog/Media",
        SID = new[] { 
            "sid://mindtouch.com/2007/06/media",
            "http://services.mindtouch.com/deki/draft/2007/06/media" 
        }
    )]
    [DreamServiceConfig("kaltura/partner-id", "string?", "Kaltura Partner ID. Kaltura Video embedding is disabled without this value.")]
    [DreamServiceConfig("kaltura/uiconf/player-mix", "string", "Kaltura UICONF value for remix player. Kaltura Video embedding is disabled without this value.")]
    [DreamServiceConfig("kaltura/uiconf/player-nomix", "string", "Kaltura UICONF value for original player. Kaltura Video embedding is disabled without this value.")]
    [DreamServiceConfig("kaltura/server-uri", "string?", "Kaltura Server URI. Location of the Kaltura Video server. (default: 'http://www.kaltura.com')")]
    [DreamServiceConfig("kaltura/seo-links", "string?", "Embed Kaltura SEO links. (one of 'enabled' or 'disabled'; default: 'enabled')")]
    [DreamServiceBlueprint("deki/service-type", "extension")]
    [DekiExtLibrary(
        Label = "Media", 
        Namespace = "", 
        Description = "This extension contains functions for embedding various media types.",
        Logo = "$files/media-logo.png"
    )]
    [DekiExtLibraryFiles(Prefix = "MindTouch.Deki.Services.Resources", Filenames = new[] { "media-logo.png" })]
    public class MediaService : DekiExtService {

        //--- Types ---
        internal abstract class AMedia {

            //--- Fields ---
            internal XUri Uri;
            internal bool AutoPlay;
            internal float? Width;
            internal float? Height;

            //--- Constructors ---
            internal AMedia(XUri uri) {
                this.Uri = uri;
            }

            //--- Properties ---
            protected abstract string AutoPlayText { get; }

            //--- Methods ---
            internal abstract XDoc AsXDoc();

            protected XDoc AsSwfObjectEmbed(string callback) {

                // NOTE (steveb): see SWFObject documentation at http://code.google.com/p/swfobject/wiki/documentation

                string id = StringUtil.CreateAlphaNumericKey(8);
                string javascript = string.Format(
                    @"swfobject.embedSWF('{1}', '{0}', '{2}', '{3}', '9.0.0', '/skins/common/expressInstall.swf', {{ {5} }}, {{ allowScriptAccess: 'always', allowNetworking: 'all', allowFullScreen: 'true', bgcolor: '#000000', movie: '{1}', wmode: 'opaque' }}, {{ id: '{0}', name: '{0}' }}, {4});",
                    id,
                    Uri.ToString().EscapeString(),
                    AsSize(Width ?? 425),
                    AsSize(Height ?? 400),
                    callback ?? "null",
                    AutoPlayText
                );
                XDoc result = new XDoc("html")
                    .Start("head")
                        .Start("script").Attr("type", "text/javascript").Attr("src", "/skins/common/swfobject.js").End()
                        .Start("script").Attr("type", "text/javascript").Value(javascript).End()
                    .End()
                    .Start("body")
                        .Start("div").Attr("id", id)
                            .Start("p")
                                .Start("a").Attr("href", "http://www.adobe.com/go/getflashplayer")
                                    .Start("img").Attr("src", "http://www.adobe.com/images/shared/download_buttons/get_flash_player.gif").Attr("alt", "Get Adobe Flash player").End()
                                .End()
                            .End()
                        .End()
                    .End();
                return result;
            }
        }

        internal class YouTubeVideo : AMedia {

            //--- Class Methods ---
            internal static AMedia New(XUri uri) {

                // check if the uri is a youtube video
                if(
                    uri.Host.EndsWithInvariantIgnoreCase(".youtube.com") || 
                    uri.Host.EqualsInvariantIgnoreCase("youtube.com") ||
                    uri.Host.EndsWithInvariantIgnoreCase(".youtube-nocookie.com") ||
                    uri.Host.EqualsInvariantIgnoreCase("youtube-nocookie.com")
                ) {
                    if(uri.GetParam("v") != null) {
                        return new YouTubeVideo(uri.WithoutPathQueryFragment().At("v", uri.GetParam("v")));
                    }
                    if(!ArrayUtil.IsNullOrEmpty(uri.Segments) && (uri.Segments.Length == 2) && (uri.Segments[0].EqualsInvariantIgnoreCase("v"))) {
                        return new YouTubeVideo(uri);
                    }
                }
                return null;
            }

            //--- Constructors ---
            private YouTubeVideo(XUri uri) : base(uri) { }

            //--- Properties ---
            protected override string AutoPlayText {
                get { return null;}
            }

            //--- Methods ---
            internal override XDoc AsXDoc() {
                
                // check if autoplay is enabled and append value to uri
                if(AutoPlay) {
                    Uri = Uri.With("autoplay", "1");
                }
                return AsSwfObjectEmbed(null);
            }
        }

        internal class GoogleVideo : AMedia {

            //--- Class Methods ---
            internal static AMedia New(XUri uri) {

                // check if the uri is a google video
                if(uri.Host.EqualsInvariantIgnoreCase("video.google.com")) {
                    if((null != uri.Params) && (!String.IsNullOrEmpty(uri.GetParam("docid")))) {
                        return new GoogleVideo(new XUri("http://video.google.com/googleplayer.swf?docId=" + uri.GetParam("docid")));
                    }
                }
                return null;
            }

            //--- Properties ---
            protected override string AutoPlayText {
                get { return AutoPlay ? "autoPlay: 'true'" : null; }
            }

            //--- Constructors  ---
            private GoogleVideo(XUri uri) : base(uri) { }

            //--- Methods ---
            internal override XDoc AsXDoc() {
                return AsSwfObjectEmbed(null);
            }
        }

        internal class ViddlerVideo : AMedia {

            //--- Class Methods ---
            internal static AMedia New(XUri uri) {

                // check if the uri is a viddler video
                if(uri.Host.EqualsInvariantIgnoreCase("www.viddler.com") || uri.Host.EqualsInvariantIgnoreCase("viddler.com")) {
                    if(!ArrayUtil.IsNullOrEmpty(uri.Segments)) {
                        if((uri.Segments[0].EqualsInvariant("player") || uri.Segments[0].EqualsInvariant("simple")) && (uri.Segments.Length == 2)) {

                            // this is a link directly to the video
                            return new ViddlerVideo(uri);
                        }
                        if(uri.Segments[0].EqualsInvariant("explore") || ((uri.Segments.Length == 3) && uri.Segments[1].EqualsInvariant("videos"))) {

                            // this is a link to the web-page; fetch the page
                            DreamMessage response = Plug.New(uri).GetAsync().Wait();
                            if(response.IsSuccessful && response.ContentType.SubType.EqualsInvariant("html")) {

                                // parse html
                                try {
                                    XDoc page = response.ToDocument();
                                    uri = page["head/link[@rel='video_src']/@href"].AsUri;
                                    if(uri != null) {
                                        return new ViddlerVideo(uri);
                                    }
                                } catch(Exception e) {
                                    _log.WarnExceptionFormat(e, "unable to parse viddler.com page at {0}", uri);
                                }
                            }
                        }
                    }
                }
                return null;
            }

            //--- Constructors ---
            private ViddlerVideo(XUri uri) : base(uri) { }

            //--- Properties ---
            protected override string AutoPlayText {
                get { return AutoPlay ? "autoplay: 't'" : null; }
            }

            //--- Methods ---
            internal override XDoc AsXDoc() {
                return AsSwfObjectEmbed(null);
            }
        }

        internal class VeohVideo : AMedia {

            // sample: <embed src="http://www.veoh.com/videodetails2.swf?permalinkId=v4537349f45TeCH&id=anonymous&player=videodetailsembedded&videoAutoPlay=0" allowFullScreen="true" width="540" height="438" bgcolor="#000000" type="application/x-shockwave-flash" pluginspage="http://www.macromedia.com/go/getflashplayer"></embed>

            //--- Class Methods ---
            internal static AMedia New(XUri uri) {

                // check if the uri is a viddler video
                if(uri.Host.EqualsInvariantIgnoreCase("www.veoh.com") || uri.Host.EqualsInvariantIgnoreCase("veoh.com")) {
                    if(!string.IsNullOrEmpty(uri.Fragment) && uri.Fragment.StartsWithInvariantIgnoreCase("watch=")) {
                        uri = new XUri("http://www.veoh.com/static/swf/webplayer/WebPlayer.swf?version=AFrontend.5.4.5.1008&player=videodetailsembedded&id=anonymous").With("permalinkId", uri.Fragment.Substring(6));
                        return new VeohVideo(uri);
                    }
                    if((uri.LastSegment ?? string.Empty).EndsWithInvariantIgnoreCase("WebPlayer.swf") && !string.IsNullOrEmpty(uri.GetParam("permalinkId"))) {
                        uri = new XUri("http://www.veoh.com/static/swf/webplayer/WebPlayer.swf?version=AFrontend.5.4.5.1008&player=videodetailsembedded&id=anonymous").With("permalinkId", uri.GetParam("permalinkId"));
                        return new VeohVideo(uri);
                    }
                }
                return null;
            }

            //--- Constructors ---
            private VeohVideo(XUri uri) : base(uri) { }

            //--- Properties ---
            protected override string AutoPlayText {
                get { return null; }
            }

            //--- Methods ---
            internal override XDoc AsXDoc() {
                Uri = Uri.With("videoAutoPlay", AutoPlay ? "1": "0");
                return AsSwfObjectEmbed(null);
            }
        }

        internal class UStreamVideo : AMedia {

            //--- Class Methods ---
            internal static AMedia New(XUri uri) {

                // check if the uri is a viddler video
                if(uri.Host.EqualsInvariantIgnoreCase("www.ustream.tv") || uri.Host.EqualsInvariantIgnoreCase("ustream.tv")) {
                    if(!ArrayUtil.IsNullOrEmpty(uri.Segments)) {
                        if((uri.Segments.Length == 1) || ((uri.Segments.Length >= 3)  && uri.Segments[0].EqualsInvariantIgnoreCase("flash"))) {

                            // this is a link directly to the video
                            return new UStreamVideo(uri);
                        }
                        if(uri.Segments[0].EqualsInvariant("channel") || (uri.Segments.Length == 2)) {

                            // this is a link to the web-page; fetch the page
                            DreamMessage response = Plug.New(uri).GetAsync().Wait();
                            if(response.IsSuccessful && response.ContentType.SubType.EqualsInvariant("html")) {

                                // parse html
                                try {
                                    XDoc page = response.ToDocument();
                                    string id = page["//_:div[@id='ircChat']/@rel"].AsText;
                                    if(!string.IsNullOrEmpty(id)) {
                                        return new UStreamVideo(new XUri("http://www.ustream.tv/flash/live/1/").At(id));
                                    }
                                    return null;
                                } catch(Exception e) {
                                    _log.WarnExceptionFormat(e, "unable to parse Ustream.tv page at {0}", uri);
                                }
                            }
                        }
                    }
                }
                return null;
            }

            //--- Constructors ---
            private UStreamVideo(XUri uri) : base(uri) { }

            //--- Properties ---
            protected override string AutoPlayText {
                get { return AutoPlay ? null : "autoplay: 'false'"; }
            }

            //--- Methods ---
            internal override XDoc AsXDoc() {
                return AsSwfObjectEmbed(null);
            }
        }

        internal class KalturaVideo : AMedia {

            //--- Class Methods ---
            internal static AMedia New(XUri uri, XDoc config) {

                // check if the uri is a viddler video
                if(uri.Scheme.EqualsInvariantIgnoreCase("kaltura")) {
                    if(uri.Segments.Length >= 1) {
                        string entryID = uri.Segments[0];
                        string partnerID = config["kaltura/partner-id"].AsText;

                        // check if extension is configured for kaltura integration
                        if(!string.IsNullOrEmpty(partnerID)) {
                            bool remix = !(uri.GetParam("edit", null) ?? uri.GetParam("remix", "no")).EqualsInvariantIgnoreCase("no");

                            // verify that user has permission to remix content on current page
                            if(remix) {
                                Plug dekiApi = GetDekiApi(config);
                                if(dekiApi != null) {
                                    try {
                                        DekiScriptMap env = DreamContext.Current.GetState<DekiScriptMap>();
                                        string pageid = env.GetAt("page.id").AsString();
                                        string userid = env.GetAt("user.id").AsString();
                                        XDoc users = dekiApi.At("pages", pageid, "allowed").With("permissions", "UPDATE").Post(new XDoc("users").Start("user").Attr("id", userid).End()).ToDocument();
                                        remix = !users[string.Format(".//user[@id='{0}']", userid)].IsEmpty;
                                    } catch(Exception e) {
                                        _log.Error("unable to verify user permission on page", e);
                                    }
                                }
                            }

                            // check if SEO links are explicitly disabled
                            bool seo = !(config["kaltura/seo-links"].AsText ?? "enabled").EqualsInvariantIgnoreCase("disabled");

                            // determin which UI configuration to use based on user's permissions and embed settings for video
                            string uiConfID = remix ? config["kaltura/uiconf/player-mix"].AsText : config["kaltura/uiconf/player-nomix"].AsText;
                            if(!string.IsNullOrEmpty(uiConfID)) {
                                uri = config["kaltura/server-uri"].AsUri ?? new XUri("http://www.kaltura.com");
                                uri = uri.At("index.php", "kwidget", "wid", "_" + partnerID, "uiconf_id", uiConfID, "entry_id", entryID);
                                return new KalturaVideo(uri, remix, seo);
                            }
                        }
                    }
                }
                return null;
            }

            //--- Fields ---
            private readonly bool _remix;
            private readonly bool _seo;

            //--- Constructors ---
            private KalturaVideo(XUri uri, bool remix, bool seo) : base(uri) {
                _remix = remix;
                _seo = seo;
            }

            //--- Properties ---
            protected override string AutoPlayText {
                get { return null; }
            }

            //--- Methods ---
            internal override XDoc AsXDoc() {
                XDoc result = AsSwfObjectEmbed(_seo ? "embed_kaltura_links" : null);

                // define editor callback function
                if(_remix || _seo) {
                    XDoc head = new XDoc("head");

                    // check if editing of content is allowed
                    if(_remix) {
                        head.Start("script").Attr("type", "text/javascript").Value(@"var onPlayerEditClick = function(arg){doPopupKalturaEditor(arg);}; var gotoEditorWindow = onPlayerEditClick;").End();
                    }

                    // define function for embedding SEO links
                    if(_seo) {

                        // NOTE: using $(res.ref) did not work on IE; now we use $(res.id) which seems to work everywhere
                        head.Start("script").Attr("type", "text/javascript").Value(@"function embed_kaltura_links(res) { if(typeof res.ref != 'undefined') $(res.id).append('<a href=""http://corp.kaltura.com"">video platform</a>').append('<a href=""http://corp.kaltura.com/technology/video_management"">video management</a>').append('<a href=""http://corp.kaltura.com/solutions/overview"">video solutions</a>').append('<a href=""http://corp.kaltura.com/technology/video_player"">free video player</a>'); }").End();
                    }

                    // prepend new nodes
                    result["head"].AddNodesInFront(head);
                }
                return result;
            }
        }

        internal class WindowsMedia : AMedia {

            //--- Class Methods ---
            internal static AMedia New(XUri uri) {
                if((uri.LastSegment ?? string.Empty).EndsWithInvariantIgnoreCase(".wmv")) {
                    return new WindowsMedia(uri);
                }
                return null;
            }

            //--- Constructors ---
            private WindowsMedia(XUri uri) : base(uri) { }

            //--- Properties ---
            protected override string AutoPlayText {
                get { return null; }
            }

            //--- Methods ---
            internal override XDoc AsXDoc() {
                return new XDoc("html").Start("body").Start("span")
                    .Start("object")
                        .Attr("width", AsSize(Width))
                        .Attr("height", AsSize(Height))
                        .Attr("classid", "CLSID:22D6F312-B0F6-11D0-94AB-0080C74C7E95")
                        .Attr("standby", "Loading Windows Media Player components...")
                        .Attr("type", "application/x-oleobject")
                        .Start("param").Attr("name", "FileName").Attr("value", Uri).End()
                        .Start("param").Attr("name", "autostart").Attr("value", AutoPlay ? "true" : "false").End()
                        .Start("param").Attr("name", "ShowControls").Attr("value", "true").End()
                        .Start("param").Attr("name", "ShowStatusBar").Attr("value", "true").End()
                        .Start("param").Attr("name", "ShowDisplay").Attr("value", "false").End()
                        .Start("embed")
                            .Attr("width", AsSize(Width))
                            .Attr("height", AsSize(Height))
                            .Attr("src", Uri)
                            .Attr("ShowControls", "1")
                            .Attr("ShowStatusBar", "1")
                            .Attr("ShowDisplay", "0")
                            .Attr("autostart", AutoPlay ? "1" : "0")
                            .Attr("scale", "tofit")
                            .Attr("wmode", "opaque")
                        .End()
                    .End()
                .End().End();
            }
        }

        internal class UnknownMedia : AMedia {

            //--- Class Methods ---
            internal static AMedia New(XUri uri) {
                return new UnknownMedia(uri);
            }

            //--- Constructors ---
            private UnknownMedia(XUri uri) : base(uri) { }

            //--- Properties ---
            protected override string AutoPlayText {
                get { return null; }
            }

            //--- Methods ---
            internal override XDoc AsXDoc() {
                return new XDoc("html").Start("body").Start("span").Start("embed")
                    .Attr("width", AsSize(Width))
                    .Attr("height", AsSize(Height))
                    .Attr("hidden", ((Width == 0) && (Height == 0)) ? "true" : null)
                    .Attr("src", Uri.With("autoStart", Convert.ToInt32(AutoPlay).ToString()))
                    .Attr("autoplay", AutoPlay.ToString())
                    .Attr("autostart", AutoPlay.ToString())
                    .Attr("scale", "tofit")
                    .Attr("wmode", "opaque")
                    .Attr("allowFullScreen", "true")
                .End().End().End();
            }
        }

        // --- Class Fields ---
        private static readonly ILog _log = LogUtils.CreateLog();

        //--- Class Methods ---
        internal static Plug GetDekiApi(XDoc config) {
            Plug result = null;
            XUri dekiUri = config["uri.deki"].AsUri;
            string dekiApiKey = config["apikey.deki"].AsText;
            if((dekiUri != null) && !string.IsNullOrEmpty(dekiApiKey)) {
                result = Plug.New(dekiUri).With("apikey", dekiApiKey);
            }
            return result;
        }

        //--- Functions ---
        [DekiExtFunction(Description = "Embed a media source (audio/video)")]
        public XDoc Media(
            [DekiExtParam("media uri")] XUri source,
            [DekiExtParam("media width (default: player dependent)", true)] float? width,
            [DekiExtParam("media height (default: player dependent)", true)] float? height,
            [DekiExtParam("auto-start media on load (default: false)", true)] bool? start
        ) {

            // determine the media type
            AMedia media = null;
            media = media ?? YouTubeVideo.New(source);
            media = media ?? GoogleVideo.New(source);
            media = media ?? ViddlerVideo.New(source);
            media = media ?? VeohVideo.New(source);
            media = media ?? UStreamVideo.New(source);
            media = media ?? KalturaVideo.New(source, Config);
            media = media ?? WindowsMedia.New(source);
            media = media ?? UnknownMedia.New(source);
            if(media != null) {
                media.Width = width;
                media.Height = height;
                media.AutoPlay = start ?? false;
                return media.AsXDoc();
            }
            return XDoc.Empty;
        }

        //--- Features ---
        protected override Yield Start(XDoc config, Result result) {
            yield return Coroutine.Invoke(base.Start, config, new Result());

            // import all kaltura/* configuration keys
            Plug dekiApi = GetDekiApi(config);
            if(dekiApi != null) {
                Result<DreamMessage> res;
                yield return res = dekiApi.At("site", "settings").GetAsync();
                try {
                    XDoc dekiConfig = res.Value.ToDocument();
                    Config.AddAll(dekiConfig["kaltura"]);
                } catch(Exception e) {
                    _log.Warn("unable to retrieve site settings from host", e);
                }
            }
            result.Return();
        }
    }
}

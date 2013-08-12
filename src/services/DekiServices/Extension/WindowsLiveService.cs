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
using System.Text;

using MindTouch.Dream;
using MindTouch.Tasking;
using MindTouch.Xml;

namespace MindTouch.Deki.Services {
    using Yield = IEnumerator<IYield>;

    [DreamService("MindTouch Windows Live Extension", "Copyright (c) 2006-2010 MindTouch Inc.",
        Info = "http://developer.mindtouch.com/App_Catalog/Windows_Live",
        SID = new string[] { 
            "sid://mindtouch.com/2007/07/windows.live",
            "http://services.mindtouch.com/deki/draft/2007/07/windows.live" 
        }
    )]
    [DreamServiceBlueprint("deki/service-type", "extension")]
    [DekiExtLibrary(
        Label = "Windows Live", 
        Namespace = "live", 
        Description = "This extension contains functions for embedding Microsoft Windows Live Controls.",
        Logo = "$files/windowslive-logo.png"
    )]
    [DekiExtLibraryFiles(Prefix = "MindTouch.Deki.Services.Resources", Filenames = new string[] { "livechannel.html", "liveprivacy.html", "windowslive-logo.png" })]
    public class WindowsLiveService : DekiExtService {

        //--- Functions ---
        [DekiExtFunction(Description = "Embed Virtual Earth map with optional description")]
        public XDoc Map(
            [DekiExtParam("street address to center map on (default: nil)", true)] string address,
            [DekiExtParam("map zoom level (1-19; default: 14)", true)] int? zoom,
            [DekiExtParam("map width (default: 450)", true)] float? width,
            [DekiExtParam("map height (default: 300)", true)] float? height,
            [DekiExtParam("map marker description (default: nil)", true)] string description,
            [DekiExtParam("map marker title (default: \"Address\")", true)] string title,
            [DekiExtParam("map kind (either \"aerial\", \"road\", or \"hybrid\"; default: nil)", true)] string kind,
            [DekiExtParam("publish on channel (default: \"default\")", true)] string publish,
            [DekiExtParam("subscribe to channel (default: nil)", true)] string subscribe
        ) {
            return GenerateMap(width ?? 450, height ?? 300, null, 1, address, description, title, zoom ?? 14, kind, null, null, subscribe, publish);
        }

        [DekiExtFunction(Description = "Embed Virtual Earth map in road-only mode with optional description")]
        public XDoc RoadMap(
            [DekiExtParam("street address to center map on (default: nil)", true)] string address,
            [DekiExtParam("map zoom level (1-19; default: 14)", true)] int? zoom,
            [DekiExtParam("map width (default: 450)", true)] float? width,
            [DekiExtParam("map height (default: 300)", true)] float? height,
            [DekiExtParam("map marker description (default: nil)", true)] string description,
            [DekiExtParam("map marker title (default: \"Address\")", true)] string title,
            [DekiExtParam("publish on channel (default: \"default\")", true)] string publish,
            [DekiExtParam("subscribe to channel (default: nil)", true)] string subscribe
        ) {
            return GenerateMap(width ?? 450, height ?? 300, null, 1, address, description, title, zoom ?? 14, "road", null, null, subscribe, publish);
        }

        [DekiExtFunction(Description = "Embed Virtual Earth map in aerial-only mode with optional description")]
        public XDoc AerialMap(
            [DekiExtParam("street address to center map on (default: nil)", true)] string address,
            [DekiExtParam("map zoom level (1-19; default: 14)", true)] int? zoom,
            [DekiExtParam("map width (default: 450)", true)] float? width,
            [DekiExtParam("map height (default: 300)", true)] float? height,
            [DekiExtParam("map marker description (default: nil)", true)] string description,
            [DekiExtParam("map marker title (default: \"Address\")", true)] string title,
            [DekiExtParam("publish on channel (default: \"default\")", true)] string publish,
            [DekiExtParam("subscribe to channel (default: nil)", true)] string subscribe
        ) {
            return GenerateMap(width ?? 450, height ?? 300, null, 1, address, description, title, zoom ?? 14, "aerial", null, null, subscribe, publish);
        }


        [DekiExtFunction(Description = "Embed Virtual Earth map in hybrid-only mode with optional description")]
        public XDoc HybridMap(
            [DekiExtParam("street address to center map on (default: nil)", true)] string address,
            [DekiExtParam("map zoom level (1-19; default: 14)", true)] int? zoom,
            [DekiExtParam("map width (default: 450)", true)] float? width,
            [DekiExtParam("map height (default: 300)", true)] float? height,
            [DekiExtParam("map marker description (default: nil)", true)] string description,
            [DekiExtParam("map marker title (default: \"Address\")", true)] string title,
            [DekiExtParam("publish on channel (default: \"default\")", true)] string publish,
            [DekiExtParam("subscribe to channel (default: nil)", true)] string subscribe
        ) {
            return GenerateMap(width ?? 450, height ?? 300, null, 1, address, description, title, zoom ?? 14, "hybrid", null, null, subscribe, publish);
        }

        [DekiExtFunction(Description = "Embed Virtual Earth map showing directions from one address to another")]
        public XDoc Directions(
            [DekiExtParam("from address (default: nil)", true)] string from,
            [DekiExtParam("to address (default: nil)", true)] string to,
            [DekiExtParam("map width (default: 450)", true)] float? width,
            [DekiExtParam("map height (default: 300)", true)] float? height,
            [DekiExtParam("map kind (either \"aerial\", \"road\", or \"hybrid\"; default: nil)", true)] string kind,
            [DekiExtParam("publish on channel (default: \"default\")", true)] string publish,
            [DekiExtParam("subscribe to channel (default: nil)", true)] string subscribe
        ) {
            return GenerateMap(width ?? 450, height ?? 300, null, 1, null, null, null, 14, kind, from, to, subscribe, publish);
        }

        [DekiExtFunction(Description = "Embed Virtual Earth map showing places of interest near an address")]
        public XDoc FindOnMap(
            [DekiExtParam("street address to center map on")] string where,
            [DekiExtParam("what to find")] string what,
            [DekiExtParam("map zoom level (1-19; default: 14)", true)] int? zoom,
            [DekiExtParam("map width (default: 450)", true)] float? width,
            [DekiExtParam("map height (default: 300)", true)] float? height,
            [DekiExtParam("max results to display (1-20; default: 10)", true)] int? max,
            [DekiExtParam("map kind (either \"aerial\", \"road\", or \"hybrid\"; default: nil)", true)] string kind,
            [DekiExtParam("publish on channel (default: \"default\")", true)] string publish,
            [DekiExtParam("subscribe to channel (default: nil)", true)] string subscribe
        ) {
            return GenerateMap(width ?? 450, height ?? 300, what, max ?? 10, where, null, null, zoom ?? 14, kind, null, null, subscribe, publish);
        }

        [DekiExtFunction(Description = "Embed Windows Live Contacts control")]
        public XDoc Contacts(
            [DekiExtParam("control width (default: 250)", true)] float? width,
            [DekiExtParam("control height (default: 350)", true)] float? height,
            [DekiExtParam("control border style (default: \"solid 1px black\")", true)] string border,
            [DekiExtParam("control language (default: \"en\")", true)] string language,
            [DekiExtParam("control location (one of \"left\", \"right\", or \"none\"; default: \"right\")", true)] string location,
            [DekiExtParam("publish on channel (default: \"default\")", true)] string publish
        ) {

            // set language code
            string market = language ?? "en";
            int index = market.IndexOf('-');
            if(index >= 0) {
                market = market.Substring(0, index);
            }

            // create custom javascript
            string id = StringUtil.CreateAlphaNumericKey(8);
            string javascript = 
@"function livecontacts_{id}_onError() { Deki.publish('debug', { text: 'An error occurred during the initialization of the Live Contacts control with id = {id}' }); }
function livecontacts_{id}_onData(c) { 
    for (var i = 0; i < c.length; i++) Deki.publish('{publish}', { name: c[i].name, email: c[i].email, phone: c[i].phone, address: (c[i].personalStreet + ', ' + c[i].personalCity + ', ' + c[i].personalState + ' ' + c[i].personalPostalCode + ', ' + c[i].personalCountry), uri: c[i].spacesRssUrl }); 
}".Replace("{id}", id).Replace("{publish}", StringUtil.EscapeString(publish ?? "default"));

            // create control document
            XDoc result = new XDoc("html");
            result.UsePrefix("devlive", "http://dev.live.com");
            result.Start("head");
            result.Start("script").Attr("type", "text/javascript").Value(@"if(document.namespaces) document.namespaces.add('devlive', 'http://dev.live.com'); else document.documentElement.setAttribute('xmlns:devlive', 'http://dev.live.com');").End();
            result.Start("script").Attr("type", "text/javascript").Attr("src", "http://controls.services.live.com/scripts/base/v0.3/live.js").End();
            result.Start("script").Attr("type", "text/javascript").Attr("src", "http://controls.services.live.com/scripts/base/v0.3/controls.js").End();
            result.Start("script").Attr("type", "text/javascript").Value(javascript).End();
            result.End();

            // append page body
            result.Start("body");
            result.Start("noscript").Value("This control requires JavaScript").End();
            result.Start("div").Attr("xmlns:devlive", "http://dev.live.com").Start("devlive:contactscontrol")
                .Attr("id", id)
                .Attr("style", string.Format("width:{0}px;height:{1}px;border:{2};float:{3};", width ?? 250, height ?? 350, border ?? "solid 1px black", location ?? "right"))
                .Attr("devlive:channelEndpointURL", Files.At("livechannel.html"))
                .Attr("devlive:privacyStatementURL", Files.At("liveprivacy.html"))
                .Attr("devlive:dataDesired", "name,email,phone,personalstreet,personalcity,personalstate,personalcountry,personalpostalcode,spacesrssurl")
                .Attr("devlive:market", market)
                .Attr("devlive:onError", "livecontacts_{id}_onError".Replace("{id}", id))
                .Attr("devlive:onData", "livecontacts_{id}_onData".Replace("{id}", id))
            .End().End();
            result.End();
            return result;
        }

        // TODO: http://settings.messenger.live.com/applications/CreateHtml.aspx
        [DekiExtFunction(Description = "Embed Windows Live Messenger control")]
        public XDoc Messenger(
            [DekiExtParam("the user being messaged (e.g. \"12BACD345678@apps.messenger.live.com\")")] string invitee,
            [DekiExtParam("control width (default: 250)", true)] float? width,
            [DekiExtParam("control height (default: 350)", true)] float? height,
            [DekiExtParam("control border style (default: \"solid 1px black\")", true)] string border,
            [DekiExtParam("control language (default: \"en-US\")", true)] string language,
            [DekiExtParam("control location (one of \"left\", \"right\", or \"none\"; default: \"right\")", true)] string location
        ) {
            return new XDoc("html")
                .Start("body")
                    .Start("iframe")
                        .Attr("src", new XUri("http://settings.messenger.live.com/Conversation/IMMe.aspx").With("invitee", invitee).With("mkt", language ?? "en-US"))
                        .Attr("width", AsSize(width))
                        .Attr("height", AsSize(height))
                        .Attr("style", string.Format("width:{0}px;height:{1}px;border:{2};float:{3};", width ?? 250, height ?? 350, border ?? "solid 1px black", location ?? "right"))
                        .Attr("frameborder", 0)
                    .End()
                .End();
        }

        [DekiExtFunction(Description = "Embed Windows Live Messenger presence icon")]
        public XDoc Presence(
            [DekiExtParam("the user being messaged (e.g. \"12BACD345678@apps.messenger.live.com\")")] string invitee,
            [DekiExtParam("control language (default: \"en-US\")", true)] string language
        ) {
            return new XDoc("html")
                .Start("body")
                    .Start("span").Attr("class", "plain")
                        .Start("a")
                            .Attr("target", "_blank")
                            .Attr("href", new XUri("http://settings.messenger.live.com/Conversation/IMMe.aspx").With("invitee", invitee).With("mkt", language ?? "en-US"))
                            .Start("img")
                                .Attr("style", "border-style: none;")
                                .Attr("src", new XUri("http://messenger.services.live.com/users/").At(invitee, "presenceimage").With("mkt", language ?? "en-US"))
                                .Attr("width", 16)
                                .Attr("height", 16)
                            .End()
                        .End()
                    .End()
                .End();
        }

        //[DekiExtFunction(Description = "Embed Windows Live Messenger Sign-In control")]
        public XDoc MessengerSignIn(
            [DekiExtParam("sign-in control width (default: 250)", true)] float? width,
            [DekiExtParam("sign-in control height (default: 350)", true)] float? height,
            [DekiExtParam("sign-in control border style (default: \"solid 1px black\")", true)] string border,
            [DekiExtParam("sign-in control language (default: \"en-US\")", true)] string language,
            [DekiExtParam("control location (one of \"left\", \"right\", or \"none\"; default: \"right\")", true)] string location,
            [DekiExtParam("publish on channel (default: \"default\")", true)] string publish,
            [DekiExtParam("subscribe to channel (default: nil)", true)] string subscribe
        ) {
            string id = StringUtil.CreateAlphaNumericKey(8);
            string common =
@"function livemessenger_publish_update(channel, kind, address, presence) {
    Deki.publish(channel, {
        kind: kind,
        name: presence.get_displayName() || address.get_address(),
        email: address.get_address(),
        status: Enum.toString(Microsoft.Live.Messenger.PresenceStatus, presence.get_status())
    });
}
function livemessenger_send(c, m, d) {
    if(Deki.hasValue(m.text) && Deki.hasValue(m.name)) {
        var conversation = d.conversations[m.name];
        if(!conversation) {
            var enum1 = d.user.get_contacts().getEnumerator();
            var found = null;
            while(enum1.moveNext()) {
                var address = enum1.get_current().get_currentAddress();
                if((m.name == address.get_address()) || (m.name == address.get_presence().get_displayName())) {
                    found = address;
                    break;
                }
            }
            if(!found) {
                return;
            }
            conversation = d.user.get_conversations().create(found);
            conversation.add_messageReceived(Delegate.create(null, function(sender, e) {
                var message = e.get_message();
                Deki.publish('{publish}', {
                    kind: 'message',
                    name: message.get_sender().get_presence().get_displayName() || message.get_sender().get_address(),
                    email: message.get_sender().get_address(),
                    text: message.get_text(),
                    date: get_timestamp()
                });
            }));
            d.conversations[m.name] = conversation;
        }
        Deki.publish(d.channel, {
            kind: 'message',
            name: d.user.get_presence().get_displayName() || d.user.get_address().get_address(),
            email: d.user.get_address().get_address(),
            text: m.text,
            date: Date()
        });
        var messenger = new Microsoft.Live.Messenger.TextMessage(m.text, null);
        conversation.sendMessage(messenger, null);
    }
}";

            string init =
@"var signin = new Microsoft.Live.Messenger.UI.SignInControl('{id}', '{privacy}', '{channel}', '{language}');
signin.add_authenticationCompleted(Delegate.create(null, function(sender, e) {
    var user = new Microsoft.Live.Messenger.User(e.get_identity());
    user.add_signInCompleted(Delegate.create(null, function(sender, e) {
        if (e.get_resultCode() === Microsoft.Live.Messenger.SignInResultCode.success) {
            var data = { conversations: { }, user: user, channel: '{publish}' };
            if(Deki.hasValue('{subscribe}')) {
                Deki.subscribe('{subscribe}', null, livemessenger_send, data);
            }
            user.get_presence().add_propertyChanged(Delegate.create(null, function(sender, e) {
                livemessenger_publish_update('{publish}', 'user', user.get_address(), user.get_presence());
            }));
            livemessenger_publish_update('{publish}', 'user', user.get_address(), user.get_presence());
            var enum1 = user.get_contacts().getEnumerator();
            while(enum1.moveNext()) {
                var contact = enum1.get_current();
                var address = contact.get_currentAddress();
                address.get_presence().add_propertyChanged(Delegate.create(null, function(sender, e) {
                    livemessenger_publish_update('{publish}', 'contact', contact.get_currentAddress(), address.get_presence());
                }));
            }
            user.get_conversations().add_collectionChanged(Delegate.create(null, function(sender, e) {
                for(var i = 0; i < e.get_newItems().length; ++i) {
                    var conversation = e.get_newItems().get_item(i);
                    var address = conversation.get_roster()[0];
                    data.conversations[address.get_address()] = conversation;
                    conversation.add_messageReceived(Delegate.create(null, function(sender, e) {
                        var message = e.get_message();
                        Deki.publish('{publish}', {
                            kind: 'message',
                            name: message.get_sender().get_presence().get_displayName() || message.get_sender().get_address(),
                            email: message.get_sender().get_address(),
                            text: message.get_text(),
                            date: get_timestamp()
                        });
                    }));
                }
            }));
        }
    }));
    user.signIn(null);
}));"
    .Replace("{id}", id)
    .Replace("{channel}", DreamContext.Current.AsPublicUri(Files.At("livechannel.html")).ToString())
    .Replace("{privacy}", DreamContext.Current.AsPublicUri(Files.At("liveprivacy.html")).ToString())
    .Replace("{language}", StringUtil.EscapeString(language ?? "en-US"))
    .Replace("{publish}", StringUtil.EscapeString(publish ?? "default"))
    .Replace("{subscribe}", StringUtil.EscapeString(subscribe ?? string.Empty));

            // head
            XDoc result = new XDoc("html");
            result.Start("head");
            result.Start("script").Attr("type", "text/javascript").Attr("src", "http://settings.messenger.live.com/api/1.0/messenger.js").End();
            result.Start("script").Attr("type", "text/javascript").Value(common).End();
            result.End();

            // body
            result.Start("body")
                .Start("div")
                    .Attr("id", id)
                    .Attr("style", string.Format("width:{0}px;height:{1}px;border:{2};float:{3};", width ?? 200, height ?? 90, border ?? "solid 1px black", location ?? "none"))
                .End()
            .End();

            result.Start("tail");
            result.Start("script").Attr("type", "text/javascript").Value(init).End();
            result.End();
            return result;
        }

        //--- Methods ---
        private XDoc GenerateMap(float width, float height, string find, int max, string address, string description, string title, int zoom, string kind, string from, string to, string recv, string publish) {
            publish = publish ?? "default";
            string id = StringUtil.CreateAlphaNumericKey(8);
            XDoc result = new XDoc("html");

            // add head
            result.Start("head");
            result.Start("script").Attr("type", "text/javascript").Attr("src", "http://dev.virtualearth.net/mapcontrol/mapcontrol.ashx?v=6").End();
            result.Start("script").Attr("type", "text/javascript").Value(
@"function windows_live_virtual_earth_data(c, m, d) {
    if(Deki.hasValue(m.from)) d.from = m.from;
    if(Deki.hasValue(m.to)) d.to = m.to;
    if(Deki.hasValue(d.from) && Deki.hasValue(d.to)) {
        d.map.GetRoute(d.from, d.to, null, null, function(route) {
            d.from = '';
            d.to = '';
            for(var i = 0; i < route.Itinerary.Segments.length; ++i) {
            var entry = { text: route.Itinerary.Segments[i].Instruction };
            if(route.Itinerary.Segments[i].Distance) entry.info = route.Itinerary.Segments[i].Distance + route.Itinerary.DistanceUnit;
                Deki.publish(d.send, entry);
            }
            Deki.publish(d.send, { text: 'TOTAL DISTANCE', info: route.Itinerary.Distance + route.Itinerary.DistanceUnit });
        });
    } else if(Deki.hasValue(m.address)) {
        var text = null;
        if(Deki.hasValue(m.text)) {
            text = m.text;
        }
        d.map.Find(text, m.address, null, null, 0, text ? d.max : 1, null, null, null, null, function(shapelayer, results, places, more) {
            if(places) {
                d.map.SetCenterAndZoom(places[0].LatLong, d.zoom);
                var label = null;
                if(Deki.hasValue(m.label)) {
                    label = m.label;
                }
                if(!label && Deki.hasValue(m.name)) {
                    label = m.name;
                }
                if(label || (Deki.hasValue(m.info))) {
                    var shape = new VEShape(VEShapeType.Pushpin, places[0].LatLong);
                    if(label) {
                        shape.SetTitle(label);
                    }
                    if(Deki.hasValue(m.info)) {
                        shape.SetDescription(m.info);
                    }
                    d.map.AddShape(shape);
                }
            } else {
                Deki.publish('debug', { text: 'unable to find address for: ' + m.address });
            }
        });
    }
}"
            ).End();
            result.End();


            // add body
            result.Start("body").Start("div").Attr("id", id).Attr("style", string.Format("position:relative; width:{0}; height:{1};", AsSize(width), AsSize(height))).End().End();

            // add tail
            StringBuilder javascript = new StringBuilder();
            javascript.AppendFormat("var map = new VEMap('{0}');", id).AppendLine();
            switch(kind) {
            case "road":
                javascript.AppendLine("map.SetDashboardSize(VEDashboardSize.Tiny);");
                javascript.AppendLine("map.LoadMap(null, 3, 'r', null, null, true);");
                break;
            case "aerial":
                javascript.AppendLine("map.SetDashboardSize(VEDashboardSize.Tiny);");
                javascript.AppendLine("map.LoadMap(null, 3, 'a', null, null, true);");
                break;
            case "hybrid":
                javascript.AppendLine("map.SetDashboardSize(VEDashboardSize.Tiny);");
                javascript.AppendLine("map.LoadMap(null, 3, 'h', null, null, true);");
                break;
            default:
                javascript.AppendLine("map.SetDashboardSize(VEDashboardSize.Small);");
                javascript.AppendLine("map.LoadMap(null, 3, 'r', null, null, false);");
                break;
            }
            javascript.AppendLine("map.AttachEvent('onmousewheel', function(e) { window.scrollBy(0, -5*e.mouseWheelChange); return true; });");
            javascript.AppendLine("var mapdata = { map: map, max: {max}, zoom: {zoom}, from: '', to: '', send: '{publish}' };".Replace("{max}", max.ToString()).Replace("{zoom}", zoom.ToString()).Replace("{publish}", publish));
            javascript.AppendLine("Deki.subscribe('{id}', null, windows_live_virtual_earth_data, mapdata);".Replace("{id}", id));
            if(recv != null) {
                javascript.AppendLine("Deki.subscribe('{subscribe}', null, windows_live_virtual_earth_data, mapdata);".Replace("{subscribe}", StringUtil.EscapeString(recv)));
            }
            if((from != null) || (to != null)) {
                javascript.AppendLine(@"Deki.publish('{id}', { from: '{from}', to: '{to}' });".Replace("{id}", id).Replace("{from}", StringUtil.EscapeString(from ?? string.Empty)).Replace("{to}", StringUtil.EscapeString(to ?? string.Empty)));
            } else if(find != null) {
                javascript.AppendLine(@"Deki.publish('{id}', { address: '{address}', text: '{find}' });".Replace("{id}", id).Replace("{address}", StringUtil.EscapeString(address)).Replace("{find}", StringUtil.EscapeString(find)));
            } else if(address != null) {
                javascript.AppendLine(@"Deki.publish('{id}', { address: '{address}', label: '{label}', info: '{info}' });".Replace("{id}", id).Replace("{address}", StringUtil.EscapeString(address)).Replace("{label}", StringUtil.EscapeString(title ?? "Address")).Replace("{info}", StringUtil.EscapeString(description ?? address)));
            }
            result.Start("tail");
            result.Start("script").Attr("type", "text/javascript").Value(javascript).End();
            result.End();
            return result;
        }
    }
}

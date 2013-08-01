/*
 * MindTouch Core - open source enterprise collaborative networking
 * Copyright (C) 2006-2008 MindTouch Inc.
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
using System.Text.RegularExpressions;
using log4net;

using MindTouch.Deki.Varnish;
using MindTouch.Dream;
using MindTouch.Tasking;
using MindTouch.Web;
using MindTouch.Xml;

namespace MindTouch.Deki {
    using Yield = IEnumerator<IYield>;

    [DreamService("MindTouch Varnish Purge Service", "MindTouch Inc. 2006",
       Info = "http://wiki.developer.mindtouch.com/Deki_Wiki/API/VarnishPurgeService",
        SID = new string[] { "sid://mindtouch.com/2009/01/varnish" }
    )]
    [DreamServiceConfig("uri.varnish", "string", "Uri to varnish cache")]
    public class VarnishPurgeService : DreamService {
        //--- Class Fields ---
        private static readonly ILog _log = LogUtils.CreateLog();

        //--- Fields ---
        private string _apikey;
        private XUri _subscriptionLocation;
        private Plug _varnish;
        private Plug _deki;
        private TimeSpan _delayPurgeTimespan;
        private UpdateDelayQueue _updateDelayQueue;

        //--- Features ---
        [DreamFeature("POST:queue", "Queue a purge operation")]
        internal Yield QueuePurge(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            XDoc doc = request.ToDocument();
            XUri channel = doc["channel"].AsUri;
            string action = channel.Segments[2];

            // there are certain sub-events we don't use to trigger on
            if(action != "view") {
                _updateDelayQueue.Enqueue(doc);
            }
            response.Return(DreamMessage.Ok());
            yield break;
        }

        //--- Methods ---
        protected override Yield Start(XDoc config, Result result) {
            yield return Coroutine.Invoke(base.Start, config, new Result());
            _varnish = Plug.New(Config["uri.varnish"].AsUri);
            _deki = Plug.New(Config["uri.deki"].AsUri);
            _apikey = Config["apikey"].AsText;
            _delayPurgeTimespan = TimeSpan.FromSeconds(config["varnish-purge-delay"].AsInt ?? 10);
            var dispatcher = new UpdateRecordDispatcher(OnQueueExpire);
            _updateDelayQueue = new UpdateDelayQueue(_delayPurgeTimespan, dispatcher);

            // set up subscription for pubsub
            XDoc subscriptionSet = new XDoc("subscription-set")
                .Elem("uri.owner", Self.Uri)
                .Start("subscription")
                    .Add(DreamCookie.NewSetCookie("service-key", InternalAccessKey, Self.Uri).AsSetCookieDocument)
                    .Elem("channel", "event://*/deki/pages/create")
                    .Elem("channel", "event://*/deki/pages/move")
                    .Elem("channel", "event://*/deki/pages/update")
                    .Elem("channel", "event://*/deki/pages/delete")
                    .Elem("channel", "event://*/deki/pages/revert")
                    .Elem("channel", "event://*/deki/pages/createalias")
                    .Elem("channel", "event://*/deki/pages/tags/update")
                    .Elem("channel", "event://*/deki/pages/dependentschanged/comments/create")
                    .Elem("channel", "event://*/deki/pages/dependentschanged/comments/update")
                    .Elem("channel", "event://*/deki/pages/dependentschanged/comments/delete")
                    .Elem("channel", "event://*/deki/pages/dependentschanged/files/create")
                    .Elem("channel", "event://*/deki/pages/dependentschanged/files/update")
                    .Elem("channel", "event://*/deki/pages/dependentschanged/files/delete")
                    .Elem("channel", "event://*/deki/pages/dependentschanged/files/move")
                    .Elem("channel", "event://*/deki/pages/dependentschanged/files/restore")
                    .Elem("channel", "event://*/deki/files/create")
                    .Elem("channel", "event://*/deki/files/update")
                    .Elem("channel", "event://*/deki/files/delete")
                    .Elem("channel", "event://*/deki/files/move")
                    .Elem("channel", "event://*/deki/files/restore")
                    .Start("recipient")
                        .Attr("authtoken", _apikey)
                        .Elem("uri", Self.Uri.At("queue"))
                    .End()
                .End();
            Result<DreamMessage> subscriptionResult;
            yield return subscriptionResult = PubSub.At("subscribers").PostAsync(subscriptionSet);
            string accessKey = subscriptionResult.Value.ToDocument()["access-key"].AsText;
            XUri location = subscriptionResult.Value.Headers.Location;
            Cookies.Update(DreamCookie.NewSetCookie("access-key", accessKey, location), null);
            _subscriptionLocation = location.AsLocalUri().WithoutQuery();
            _log.DebugFormat("subscribed VarnishPurgeService for events at {0}", _subscriptionLocation);
            result.Return();
        }

        protected override Yield Stop(Result result) {
            _updateDelayQueue.Cleanup();
            _updateDelayQueue = null;
            yield return Plug.New(_subscriptionLocation).DeleteAsync().Catch();
            yield return Coroutine.Invoke(base.Stop, new Result());
            result.Return();
        }

        private Yield OnQueueExpire(UpdateRecord data, Result result) {
            string regex;

            switch(data.Type) {
            case RecordType.Page: {
                    if(string.IsNullOrEmpty(data.Path)) {
                        Result<DreamMessage> pageResult;
                        yield return pageResult = _deki.At("pages", data.Id.ToString()).GetAsync();
                        DreamMessage pageInfo = pageResult.Value;
                        if(!pageInfo.IsSuccessful) {
                            throw new DreamBadRequestException(string.Format("unable to fetch page for '{0}' from '{1}'", data.Id, data.WikiId));
                        }
                        data.Path = pageInfo.ToDocument()["path"].AsText;
                    }
                    string pathIndexPhp = Title.FromUriPath(data.Path).AsUiUriPath(true);

                    // need to purge url's like:  
                    //    1) index.php?title=Some/Page
                    //    2) /Some/Page
                    //    3) /@api/deki/pages/{id}/.*
                    regex = string.Format(@"^/(({0}|{1})[\?&]?|@api/deki/pages/{2}/?).*$", Regex.Escape(data.Path), Regex.Escape(pathIndexPhp), data.Id);
                    break;
                }
            case RecordType.File: {

                    // need to purge url's like:
                    //    1) /@api/deki/files/1234
                    //    2) /@api/deki/files/1234/=test.png
                    //    3) /@api/deki/files/1234/=test.png?size=webview
                    regex = string.Format(@"^/@api/deki/files/{0}/?.*$", data.Id);
                    break;
                }
            default:
                result.Return();
                yield break;
            }

            DreamMessage msg = new DreamMessage(DreamStatus.Ok, null, MimeType.TEXT, "dummy"); // mono requires some data, hence the "dummy"
            msg.Headers.Add("X-Purge-Url", regex);
            Result<DreamMessage> response;
            yield return response = _varnish.InvokeAsync("PURGE", msg);
            if(!response.Value.IsSuccessful) {
                _log.DebugFormat("failure purging: {0}", regex);
            } else {
                _log.DebugFormat("purged: {0}", regex);
            }
            result.Return();
            yield break;
        }
    }



}

/*
 * MindTouch DekiWiki - a commercial grade open source wiki
 * Copyright (C) 2006 MindTouch, Inc.
 * www.mindtouch.com  oss@mindtouch.com
 *
 * For community documentation and downloads visit www.opengarden.org;
 *  please review the licensing section.
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
using System.Xml.Serialization;
using MindTouch.Dream;

namespace MindTouch.Deki {
    [DreamService("MindTouch Dream WikiEvents", "MindTouch, Inc. 2006", "http://tech.mindtouch.com/Product/Dream/Service_WikiEvents")]
    public class WikiEventsService : DreamService {

        //--- Class Fields ---
        private static log4net.ILog _log = LogUtils.CreateLog<WikiEventsService>();

        [DreamFeature("event", "/", "PUT", "Post Wiki Event", "http://www.mindtouch.com")]
        public DreamMessage PutEvent(DreamContext context, DreamMessage message) {
            XDoc xmlEvent = message.Document;
            LogUtils.LogTrace(_log, "PutEvent", xmlEvent);
            EventRecord record = new EventRecord();
            record.Who = xmlEvent["who"].Contents;
            record.When = xmlEvent["when"].AsDate ?? DateTime.UtcNow;
            record.Channel = xmlEvent["channel"].Contents;
            record.Action = xmlEvent["action"].Contents;
            record.ActionDetail = xmlEvent["detail"].Contents;
            record.Context = xmlEvent["context"].AsUri;
            record.Target = xmlEvent["target"].AsUri;
            if(record.ActionDetail != string.Empty) {
                Async(delegate() { Plug.New(context.Uri).At("stats", "record", record.ActionDetail).Put(DreamMessage.Ok(MimeType.TEXT, "1")); });
            }
            if(record.Context != null) {
                Async(delegate() { Plug.New(context.Uri).At("events", "channels", record.Channel).Put(record.ToXDoc()); });
            }
            return DreamMessage.Ok();
        }
    }

    [XmlRoot("Event")]
    public class EventRecord {
        public string Who;
        public DateTime When = DateTime.UtcNow;
        public XUri Context;
        public string Channel;
        public string Action;
        public string ActionDetail;
        public XUri Target;

        public XDoc ToXDoc() {
            return new XDoc("event")
                .Start("who").Value(Who).End()
                .Start("when").Value(When).End()
                .Start("context").Value(Context).End()
                .Start("channel").Value(Channel).End()
                .Start("action").Value(Action).End()
                .Start("detail").Value(ActionDetail).End()
                .Start("target").Value(Target).End();
        }
    }
}

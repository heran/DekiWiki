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

using MindTouch.Dream;
using MindTouch.Tasking;
using MindTouch.Xml;

namespace MindTouch.Deki.Export {
    using Yield = IEnumerator<IYield>;

    public class Exporter {
        private readonly Plug _dekiApi;

        //--- Class Fields ---
        private static readonly ILog _log = LogUtils.CreateLog();

        //--- Class Methods ---
        public static Result<Exporter> CreateAsync(Plug dekiApi, XDoc exports, int relto, Result<Exporter> result) {
            return Coroutine.Invoke(Create_Helper, dekiApi, exports, relto, (string)null, result);
        }

        public static Result<Exporter> CreateAsync(Plug dekiApi, XDoc exports, string reltopath, Result<Exporter> result) {
            return Coroutine.Invoke(Create_Helper, dekiApi, exports, 0, reltopath, result);
        }

        private static Yield Create_Helper(Plug dekiApi, XDoc exports, int relto, string reltopath, Result<Exporter> result) {
            Exporter exporter = new Exporter(dekiApi);
            yield return Coroutine.Invoke(exporter.Init, exports, relto, reltopath, new Result());
            result.Return(exporter);
        }

        //--- Fields ---
        private XDoc _exportDocument;

        //--- Constructors ---
        private Exporter(Plug dekiApi) {
            _dekiApi = dekiApi;
        }

        //--- Properties ---
        public XDoc Manifest { get { return _exportDocument["manifest"]; } }

        public string[] DataIds {
            get {
                List<string> dataIds = new List<string>();
                foreach(XDoc doc in _exportDocument["requests/request/@dataid"]) {
                    dataIds.Add(doc.AsText);
                }
                return dataIds.ToArray();
            }
        }

        //--- Methods ---
        private Yield Init(XDoc exports, int relto, string reltopath, Result result) {
            Plug exportPlug = _dekiApi.At("site", "export");
            if(string.IsNullOrEmpty(reltopath)) {
                exportPlug = exportPlug.With("relto", relto.ToString());
            } else {
                exportPlug = exportPlug.With("reltopath", reltopath);
            }
            Result<DreamMessage> initResult;
            yield return initResult = exportPlug.Post(exports, new Result<DreamMessage>(TimeSpan.MaxValue));
            DreamMessage response = initResult.Value;
            if(!response.IsSuccessful) {
                throw new DreamResponseException(response, string.Format("Request failed with {0} for {1}", response.Status, exportPlug.Uri));
            }
            _exportDocument = response.ToDocument();
            result.Return();
            yield break;
        }

        public Result<ExportItem> GetItemAsync(string dataId, Result<ExportItem> result) {
            return Coroutine.Invoke(GetItem_Helper, dataId, result);
        }

        private Yield GetItem_Helper(string dataId, Result<ExportItem> result) {
            XDoc request = _exportDocument[string.Format("requests/request[@dataid='{0}']", dataId)];
            Plug itemPlug = Plug.New(request["@href"].AsUri)
                .WithHeaders(_dekiApi.Headers)
                .WithCredentials(_dekiApi.Credentials)
                .WithCookieJar(_dekiApi.CookieJar)
                .WithTimeout(TimeSpan.FromMinutes(30));
            foreach(XDoc header in request["header"]) {
                itemPlug = itemPlug.WithHeader(header["@name"].AsText, header["@value"].AsText);
            }
            string verb = request["@method"].AsText;
            Result<DreamMessage> invokeResult;
            yield return invokeResult = itemPlug.InvokeEx(verb, DreamMessage.Ok(), new Result<DreamMessage>());
            DreamMessage response = invokeResult.Value;
            if(!response.IsSuccessful) {
                throw new DreamResponseException(response, string.Format("Request failed with {0} for {1}", response.Status, itemPlug.Uri));
            }
            XDoc manifestItem = _exportDocument[string.Format("manifest/*[@dataid='{0}']", dataId)].Clone();
            ExportItem item = new ExportItem(dataId, response.ToStream(), response.ContentLength, manifestItem);
            result.Return(item);
        }
    }
}

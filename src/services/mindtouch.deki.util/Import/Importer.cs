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
using log4net;

using MindTouch.Dream;
using MindTouch.Tasking;
using MindTouch.Xml;

namespace MindTouch.Deki.Import {
    using Yield = IEnumerator<IYield>;

    public class Importer {

        //--- Class Fields ---
        private static readonly ILog _log = LogUtils.CreateLog();

        //--- Class Methods ---
        public static Result<Importer> CreateAsync(Plug dekiApi, XDoc manifest, int relto, Result<Importer> result) {
            return Coroutine.Invoke(Create_Helper, dekiApi, manifest, relto, (string)null, false, result);
        }
        public static Result<Importer> CreateAsync(Plug dekiApi, XDoc manifest, string reltopath, Result<Importer> result) {
            return Coroutine.Invoke(Create_Helper, dekiApi, manifest, 0, reltopath, false, result);
        }

        public static Result<Importer> CreateAsync(Plug dekiApi, XDoc manifest, int relto, bool forceOverwrite, Result<Importer> result) {
            return Coroutine.Invoke(Create_Helper, dekiApi, manifest, relto, (string)null, forceOverwrite, result);
        }
        public static Result<Importer> CreateAsync(Plug dekiApi, XDoc manifest, string reltopath, bool forceOverwrite, Result<Importer> result) {
            return Coroutine.Invoke(Create_Helper, dekiApi, manifest, 0, reltopath, forceOverwrite, result);
        }

        private static Yield Create_Helper(Plug dekiApi, XDoc manifest, int relto, string reltopath, bool forceOverwrite, Result<Importer> result) {
            Importer importer = new Importer(dekiApi, manifest);
            yield return Coroutine.Invoke(importer.Init, relto, reltopath, forceOverwrite, new Result());
            result.Return(importer);
        }

        //--- Fields ---
        private readonly Plug _dekiApi;
        private readonly XDoc _manifest;
        private List<ImportItem> _items;
        private XDoc _requests;

        //--- Constructors ---
        private Importer(Plug dekiApi, XDoc manifest) {
            _dekiApi = dekiApi;
            _manifest = manifest;
        }

        //--- Properties
        public XDoc Manifest { get { return _manifest; } }
        public List<XDoc> Warnings { get { return _requests["warning"].ToList(); } }

        public IEnumerable<ImportItem> Items {
            get {
                lock(_manifest) {
                    if(_items == null) {
                        _items = new List<ImportItem>();
                        foreach(XDoc request in _requests["request"]) {
                            var dataId = request["@dataid"].AsText;
                            var manifest = _manifest[string.Format(".//*[@dataid='{0}']", dataId)];
                            _items.Add(new ImportItem(dataId, request, manifest));
                        }
                    }
                }
                return _items;
            }
        }

        //--- Methods ---
        private Yield Init(int relto, string reltopath, bool forceOverwrite, Result result) {
            Result<DreamMessage> initResult;
            Plug importPlug = _dekiApi.At("site", "import");
            if(string.IsNullOrEmpty(reltopath)) {
                importPlug = importPlug.With("relto", relto);
            } else {
                importPlug = importPlug.With("reltopath", reltopath);
            }
            if(forceOverwrite) {
                importPlug = importPlug.With("forceoverwrite", "true");
            }
            yield return initResult = importPlug.Post(_manifest, new Result<DreamMessage>(TimeSpan.MaxValue));
            DreamMessage response = initResult.Value;
            if(!response.IsSuccessful) {
                throw new DreamResponseException(response, string.Format("Request failed with {0} for {1}", response.Status, importPlug.Uri));
            }
            _requests = response.ToDocument();
            result.Return();
        }

        public Result WriteDataAsync(ImportItem item, Result result) {
            return Coroutine.Invoke(WriteData_Helper, item, result);
        }

        private Yield WriteData_Helper(ImportItem item, Result result) {
            var mimeType = MimeType.New(item.Request["@type"].AsText ?? MimeType.TEXT_XML.FullType);
            DreamMessage itemMessage;
            if(item.Data != null) {
                itemMessage = DreamMessage.Ok(mimeType, item.DataLength, item.Data);
            } else {
                var body = item.Request["body"];
                itemMessage = !body.IsEmpty ? DreamMessage.Ok(body.Elements) : DreamMessage.Ok();
            }
            foreach(var header in item.Request["header"]) {
                itemMessage.Headers[header["@name"].AsText] = header["@value"].AsText;
            }
            var verb = item.Request["@method"].AsText;
            var itemPlug = Plug.New(item.Request["@href"].AsUri)
                .WithHeaders(_dekiApi.Headers)
                .WithCredentials(_dekiApi.Credentials)
                .WithCookieJar(_dekiApi.CookieJar)
                .WithTimeout(TimeSpan.FromMinutes(30));
            DreamMessage response = null;
            yield return itemPlug.Invoke(verb, itemMessage, new Result<DreamMessage>(TimeSpan.MaxValue)).Set(x => response = x);
            if(!response.IsSuccessful) {
                throw new DreamResponseException(response, string.Format("Request failed with {0} for {1}", response.Status, itemPlug.Uri));
            }
            result.Return();
            yield break;
        }
    }
}

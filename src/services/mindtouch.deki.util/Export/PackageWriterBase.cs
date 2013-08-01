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
using System.Linq;
using System.Text;
using log4net;
using MindTouch.Dream;
using MindTouch.Tasking;
using MindTouch.Xml;

namespace MindTouch.Deki.Export {
    using Yield = IEnumerator<IYield>;

    public abstract class PackageWriterBase : IPackageWriter {

        //--- Class Fields ---
        private static readonly ILog _log = LogUtils.CreateLog();

        //--- Class Methods ---
        private static string Encode(string segment) {
            return XUri.EncodeSegment(segment);
        }

        //--- Fields ---
        private readonly XDoc _dataMap;
        private XDoc _manifest;
        private bool _closed;
        private bool _disposed;

        //--- Constructors ---
        protected PackageWriterBase() {
            _dataMap = new XDoc("map");
        }

        //--- Methods ---
        public void Dispose() {
            if(_disposed) {
                throw new ObjectDisposedException("Object already disposed");
            }
            if(!_closed) {
                Close();
            }
            _disposed = true;
        }

        public virtual Result WriteDataAsync(ExportItem item, Result result) {
            return Coroutine.Invoke(WriteData_Helper, item, result);
        }

        protected abstract Yield WriteData_Helper(ExportItem item, Result result);

        protected virtual string GetFilePath(ExportItem item) {
            var path = item.ItemManifest["path"].AsText;
            var segments = new List<string>();
            if(path.StartsWith("//")) {
                segments.Add("relative");
            } else {
                segments.Add("absolute");
            }
            segments.AddRange(path.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries));
            switch(item.ItemManifest.Name) {
            case "page":
                segments.Add("page.xml");
                break;
            case "file":
                segments.Add(Encode(item.ItemManifest["filename"].AsText));
                break;
            case "property":
                segments.Add(Encode(item.ItemManifest["name"].AsText) + ".dat");
                break;
            default:
                segments.Add(item.ItemManifest.Name + ".dat");
                break;
            }
            return string.Join("/", segments.Select(x => Encode(x)).ToArray());
        }

        protected void AddFileMap(string dataId, string filepath) {
            lock(_dataMap) {
                _dataMap.Start("item").Attr("dataid", dataId).Attr("path", filepath).End();
            }
        }

        public virtual Result WriteManifest(XDoc manifest, Result result) {
            _manifest = manifest;
            result.Return();
            return result;
        }

        public void Close() {
            CloseAsync(new Result()).Wait();
        }

        public virtual Result CloseAsync(Result result) {
            if(_closed) {
                throw new InvalidOperationException("Object is already closed");
            }
            _closed = true;
            return Coroutine.Invoke(CloseAsync_Helper, result);
        }

        private Yield CloseAsync_Helper(Result result) {
            XDoc package = new XDoc("package")
               .Add(_manifest)
               .Add(_dataMap);
            yield return Coroutine.Invoke(WritePackageDoc_Helper, package, new Result());
            result.Return();
            yield break;
        }

        protected abstract Yield WritePackageDoc_Helper(XDoc package, Result result);
    }
}

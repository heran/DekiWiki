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
using System.Threading;
using log4net;
using MindTouch.Dream;

using MindTouch.Tasking;
using MindTouch.Xml;

namespace MindTouch.Deki.Export {
    using Yield = IEnumerator<IYield>;
    public class ExportManager {

        //--- Constants ---
        private const int DEFAULT_RETRIES = 3;

        //--- Class Fields ---
        private static readonly ILog _log = LogUtils.CreateLog();

        //--- Class Methods ---
        public static Result<ExportManager> CreateArchiveExportManagerAsync(Plug dekiApi, XDoc exports, int relto, string archiveFilename, Result<ExportManager> result) {
            ArchivePackageWriter filePackager = new ArchivePackageWriter(archiveFilename);
            return CreateAsync(dekiApi, exports, relto, filePackager, result);
        }

        public static Result<ExportManager> CreateArchiveExportManagerAsync(Plug dekiApi, XDoc exports, string reltopath, string archiveFilename, Result<ExportManager> result) {
            ArchivePackageWriter filePackager = new ArchivePackageWriter(archiveFilename);
            return CreateAsync(dekiApi, exports, reltopath, filePackager, result);
        }

        public static Result<ExportManager> CreateFileExportManagerAsync(Plug dekiApi, XDoc exports, int relto, string packageDirectory, Result<ExportManager> result) {
            FilePackageWriter filePackager = new FilePackageWriter(packageDirectory);
            return CreateAsync(dekiApi, exports, relto, filePackager, result);
        }

        public static Result<ExportManager> CreateFileExportManagerAsync(Plug dekiApi, XDoc exports, string reltopath, string packageDirectory, Result<ExportManager> result) {
            FilePackageWriter filePackager = new FilePackageWriter(packageDirectory);
            return CreateAsync(dekiApi, exports, reltopath, filePackager, result);
        }

        public static Result<ExportManager> CreateAsync(Plug dekiApi, XDoc exports, int relto, IPackageWriter packager, Result<ExportManager> result) {
            return Coroutine.Invoke(Create_Helper, dekiApi, exports, relto, (string)null, packager, result);
        }
        public static Result<ExportManager> CreateAsync(Plug dekiApi, XDoc exports, string reltopath, IPackageWriter packager, Result<ExportManager> result) {
            return Coroutine.Invoke(Create_Helper, dekiApi, exports, 0, reltopath, packager, result);
        }

        private static Yield Create_Helper(Plug dekiApi, XDoc exports, int relto, string reltopath, IPackageWriter packager, Result<ExportManager> result) {
            Result<Exporter> exporterResult;
            if(string.IsNullOrEmpty(reltopath)) {
                yield return exporterResult = Exporter.CreateAsync(dekiApi, exports, relto, new Result<Exporter>());
            } else {
                yield return exporterResult = Exporter.CreateAsync(dekiApi, exports, reltopath, new Result<Exporter>());
            }
            result.Return(new ExportManager(exporterResult.Value, packager));
            yield break;
        }

        //--- Fields ---
        private readonly Exporter _exporter;
        private readonly IPackageWriter _packager;
        private int _completed = 0;

        //--- Constructors ---
        private ExportManager(Exporter exporter, IPackageWriter packager) {
            MaxRetries = DEFAULT_RETRIES;
            _exporter = exporter;
            _packager = packager;
        }

        //--- Properties ---
        public int TotalItems { get { return _exporter.DataIds.Length; } }
        public int CompletedItems { get { return _completed; } }
        public int MaxRetries { get; set; }

        //--- Methods ---
        public Result ExportAsync(Result result) {
            return Coroutine.Invoke(Export_Helper, (Action<XDoc>)null, result);
        }

        public Result ExportAsync(Action<XDoc> manifestCallback, Result result) {
            return Coroutine.Invoke(Export_Helper, manifestCallback, result);
        }

        private Yield Export_Helper(Action<XDoc> manifestCallback, Result result) {
            foreach(var dataId in _exporter.DataIds) {
                var retry = 0;
                while(true) {
                    Result<ExportItem> itemResult;
                    yield return itemResult = _exporter.GetItemAsync(dataId, new Result<ExportItem>()).Catch();
                    if(itemResult.HasException) {
                        retry++;
                        _log.DebugFormat("failed to retrieve item {0} on try {1}", (CompletedItems + 1),retry);
                        if(retry < MaxRetries) {
                            continue;
                        }
                        XDoc manifestItem = _exporter.Manifest[string.Format(".//*[@dataid='{0}']", dataId)];
                        throw new ExportException(manifestItem, itemResult.Exception);
                    }
                    Result writeResult;
                    yield return writeResult = _packager.WriteDataAsync(itemResult.Value, new Result()).Catch();
                    itemResult.Value.Dispose();
                    if(writeResult.HasException) {
                        retry++;
                        _log.DebugFormat("failed to write item {0} on try {1}", (CompletedItems + 1), retry);
                        if(retry < MaxRetries) {
                            continue;
                        }
                        throw new ExportException(itemResult.Value.ItemManifest, writeResult.Exception);
                    }
                    break;
                }
                Interlocked.Increment(ref _completed);
            }
            if(manifestCallback != null) {
                manifestCallback(_exporter.Manifest);
            }
            yield return _packager.WriteManifest(_exporter.Manifest, new Result());
            _packager.Dispose();
            result.Return();
        }
    }
}

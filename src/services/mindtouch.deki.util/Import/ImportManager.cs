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
using System.Linq;
using System.Threading;
using log4net;
using MindTouch.Dream;
using MindTouch.Tasking;
using MindTouch.Xml;

namespace MindTouch.Deki.Import {
    using Yield = IEnumerator<IYield>;

    public class ImportManager {

        //--- Constants ---
        private const int DEFAULT_RETRIES = 3;

        //--- Class Fields ---
        private static readonly ILog _log = LogUtils.CreateLog();

        //--- Class Methods ---
        public static Result<ImportManager> CreateArchiveImportManagerAsync(Plug dekiApi, int relto, string archiveFilename, Result<ImportManager> result) {
            var archivePackageReader = new ArchivePackageReader(archiveFilename);
            return CreateAsync(dekiApi, relto, archivePackageReader, result);
        }

        public static Result<ImportManager> CreateArchiveImportManagerAsync(Plug dekiApi, string reltopath, string archiveFilename, Result<ImportManager> result) {
            var archivePackageReader = new ArchivePackageReader(archiveFilename);
            return CreateAsync(dekiApi, reltopath, archivePackageReader, result);
        }

        public static Result<ImportManager> CreateFileImportManagerAsync(Plug dekiApi, int relto, string packageDirectory, Result<ImportManager> result) {
            var filePackager = new FilePackageReader(packageDirectory);
            return CreateAsync(dekiApi, relto, filePackager, result);
        }

        public static Result<ImportManager> CreateFileImportManagerAsync(Plug dekiApi, string reltopath, string packageDirectory, Result<ImportManager> result) {
            var filePackager = new FilePackageReader(packageDirectory);
            return CreateAsync(dekiApi, reltopath, filePackager, result);
        }

        public static Result<ImportManager> CreateAsync(Plug dekiApi, int relto, IPackageReader packager, Result<ImportManager> result) {
            return Coroutine.Invoke(Create_Helper, dekiApi, relto, (string)null, packager, result);
        }

        public static Result<ImportManager> CreateAsync(Plug dekiApi, string reltopath, IPackageReader packager, Result<ImportManager> result) {
            return Coroutine.Invoke(Create_Helper, dekiApi, 0, reltopath, packager, result);
        }

        private static Yield Create_Helper(Plug dekiApi, int relto, string reltopath, IPackageReader packager, Result<ImportManager> result) {
            Result<XDoc> manifestResult;
            yield return manifestResult = packager.ReadManifest(new Result<XDoc>());
            Result<Importer> importerResult;
            if(string.IsNullOrEmpty(reltopath)) {
                yield return importerResult = Importer.CreateAsync(dekiApi, manifestResult.Value, relto, new Result<Importer>());
            } else {
                yield return importerResult = Importer.CreateAsync(dekiApi, manifestResult.Value, reltopath, new Result<Importer>());
            }
            result.Return(new ImportManager(importerResult.Value, packager));
            yield break;
        }

        //--- Fields ---
        private readonly Importer _importer;
        private readonly IPackageReader _packager;
        private int _completed = 0;

        //--- Constructors ---
        public ImportManager(Importer importer, IPackageReader packager) {
            MaxRetries = DEFAULT_RETRIES;
            _importer = importer;
            _packager = packager;
        }

        //--- Properties ---
        public int TotalItems { get { return _importer.Items.Count(); } }
        public int CompletedItems { get { return _completed; } }
        public int MaxRetries { get; set; }

        //--- Methods ---
        public Result ImportAsync(Result result) {
            return Coroutine.Invoke(Import_Helper, result);
        }

        private Yield Import_Helper(Result result) {
            foreach(var importItem in _importer.Items) {
                var retry = 0;
                while(true) {

                    // capturing item since we may modify its slot
                    var item = importItem;
                    if(item.NeedsData) {
                        Result<ImportItem> readResult;
                        yield return readResult = _packager.ReadData(item, new Result<ImportItem>()).Catch();
                        if(readResult.HasException) {
                            retry++;
                            _log.DebugFormat("failed to read data item {0} on try {1}", (CompletedItems + 1), retry);
                            if(retry < MaxRetries) {
                                continue;
                            }
                            throw new ImportException(item.Manifest, readResult.Exception);
                        }

                        item = readResult.Value;
                    }
                    Result writeResult;
                    yield return writeResult = _importer.WriteDataAsync(item, new Result()).Catch();
                    if(writeResult.HasException) {
                        retry++;
                        _log.DebugFormat("failed to write item {0} on try {1}", (CompletedItems + 1), retry);
                        if(retry < MaxRetries) {
                            continue;
                        }
                        throw new ImportException(item.Manifest, writeResult.Exception);
                    }
                    break;
                }
                Interlocked.Increment(ref _completed);
            }
            _packager.Dispose();
            result.Return();
        }
    }
}

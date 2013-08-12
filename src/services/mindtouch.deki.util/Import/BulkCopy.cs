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

using MindTouch.Deki.Export;
using MindTouch.Dream;
using MindTouch.Tasking;
using MindTouch.Xml;

namespace MindTouch.Deki.Import {
    using Yield = IEnumerator<IYield>;

    public class BulkCopy {

        public delegate Yield CopyInterceptDelegate(BulkCopy copier, ExportItem item, Result<ExportItem> result);

        //--- Class Fields ---
        private static readonly ILog _log = LogUtils.CreateLog();

        public static Result<BulkCopy> CreateAsync(Plug dekiApi, XDoc exports, string exportReltoPath, string importReltoPath, Result<BulkCopy> result) {
            return Coroutine.Invoke(Create_Helper, dekiApi, exports, 0, exportReltoPath, 0, importReltoPath, false, result);
        }

        public static Result<BulkCopy> CreateAsync(Plug dekiApi, XDoc exports, int exportRelto, string importReltoPath, Result<BulkCopy> result) {
            return Coroutine.Invoke(Create_Helper, dekiApi, exports, exportRelto, (string)null, 0, importReltoPath, false, result);
        }

        public static Result<BulkCopy> CreateAsync(Plug dekiApi, XDoc exports, string exportReltoPath, int importRelto, Result<BulkCopy> result) {
            return Coroutine.Invoke(Create_Helper, dekiApi, exports, 0, exportReltoPath, importRelto, (string)null, false, result);
        }

        public static Result<BulkCopy> CreateAsync(Plug dekiApi, XDoc exports, int exportRelto, int importRelto, Result<BulkCopy> result) {
            return Coroutine.Invoke(Create_Helper, dekiApi, exports, exportRelto, (string)null, importRelto, (string)null, false, result);
        }

        public static Result<BulkCopy> CreateAsync(Plug dekiApi, XDoc exports, string exportReltoPath, string importReltoPath, bool forceOverwrite, Result<BulkCopy> result) {
            return Coroutine.Invoke(Create_Helper, dekiApi, exports, 0, exportReltoPath, 0, importReltoPath, forceOverwrite, result);
        }

        public static Result<BulkCopy> CreateAsync(Plug dekiApi, XDoc exports, int exportRelto, string importReltoPath, bool forceOverwrite, Result<BulkCopy> result) {
            return Coroutine.Invoke(Create_Helper, dekiApi, exports, exportRelto, (string)null, 0, importReltoPath, forceOverwrite, result);
        }

        public static Result<BulkCopy> CreateAsync(Plug dekiApi, XDoc exports, string exportReltoPath, int importRelto, bool forceOverwrite, Result<BulkCopy> result) {
            return Coroutine.Invoke(Create_Helper, dekiApi, exports, 0, exportReltoPath, importRelto, (string)null, forceOverwrite, result);
        }

        public static Result<BulkCopy> CreateAsync(Plug dekiApi, XDoc exports, int exportRelto, int importRelto, bool forceOverwrite, Result<BulkCopy> result) {
            return Coroutine.Invoke(Create_Helper, dekiApi, exports, exportRelto, (string)null, importRelto, (string)null, forceOverwrite, result);
        }

        private static Yield Create_Helper(Plug dekiApi, XDoc exports, int exportRelto, string exportReltoPath, int importRelto, string importReltoPath, bool forceOverwrite, Result<BulkCopy> result) {
            Result<Exporter> exporterResult;
            if(string.IsNullOrEmpty(exportReltoPath)) {
                yield return exporterResult = Exporter.CreateAsync(dekiApi, exports, exportRelto, new Result<Exporter>());
            } else {
                yield return exporterResult = Exporter.CreateAsync(dekiApi, exports, exportReltoPath, new Result<Exporter>());
            }
            Exporter exporter = exporterResult.Value;
            Result<Importer> importerResult;
            if(string.IsNullOrEmpty(importReltoPath)) {
                yield return importerResult = Importer.CreateAsync(dekiApi, exporter.Manifest, importRelto, forceOverwrite, new Result<Importer>());
            } else {
                yield return importerResult = Importer.CreateAsync(dekiApi, exporter.Manifest, importReltoPath, forceOverwrite, new Result<Importer>());
            }
            BulkCopy bulkCopy = new BulkCopy(exporter, importerResult.Value);
            result.Return(bulkCopy);
        }

        private readonly Exporter _exporter;
        private readonly Importer _importer;
        private int _completed;
        private CoroutineHandler<BulkCopy, ExportItem, Result<ExportItem>> _copyIntercept;

        private BulkCopy(Exporter exporter, Importer importer) {
            _exporter = exporter;
            _importer = importer;
        }

        public XDoc Manifest { get { return _importer.Manifest; } }
        public int TotalItems { get { return _importer.Items.Count(); } }
        public int CompletedItems { get { return _completed; } }

        public void RegisterCopyIntercept(CopyInterceptDelegate intercept) {
            _copyIntercept = delegate(BulkCopy copier, ExportItem exportItem, Result<ExportItem> result) {
                return intercept(copier, exportItem, result);
            };
        }

        public Result<BulkCopy> CopyAsync(Result<BulkCopy> result) {
            return Coroutine.Invoke(Copy_Helper, result);
        }

        private Yield Copy_Helper(Result<BulkCopy> result) {
            foreach(var importItem in _importer.Items) {
                Result<ExportItem> exportResult;
                yield return exportResult = _exporter.GetItemAsync(importItem.DataId, new Result<ExportItem>());
                ExportItem exportItem = exportResult.Value;
                if(_copyIntercept != null) {
                    Result<ExportItem> interceptResult;
                    yield return interceptResult = Coroutine.Invoke(_copyIntercept, this, exportItem, new Result<ExportItem>());
                    if(interceptResult.Value == null) {
                        exportItem.Dispose();
                        continue;
                    }
                    exportItem = interceptResult.Value;
                }
                yield return _importer.WriteDataAsync(importItem.WithData(exportItem.Data, exportItem.DataLength), new Result());
                Interlocked.Increment(ref _completed);
            }
            result.Return(this);
            yield break;
        }
    }
}

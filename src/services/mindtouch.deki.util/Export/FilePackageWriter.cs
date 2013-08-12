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
using System.IO;
using log4net;

using MindTouch.IO;
using MindTouch.Tasking;
using MindTouch.Xml;

namespace MindTouch.Deki.Export {
    using Yield = IEnumerator<IYield>;

    public class FilePackageWriter : PackageWriterBase {

        //--- Class Fields ---
        private static readonly ILog _log = LogUtils.CreateLog();

        //--- Class Methods ---
        private readonly string _packageDirectory;

        public FilePackageWriter(string packageDirectory) {
            _packageDirectory = packageDirectory;
        }

        protected override Yield WriteData_Helper(ExportItem item, Result result) {
            string file = GetFilePath(item);
            string filepath = Path.Combine(_packageDirectory, file);
            string path = Path.GetDirectoryName(filepath);
            if(!Directory.Exists(path)) {
                _log.DebugFormat("creating directory: {0}", path);
                Directory.CreateDirectory(path);
            }
            FileStream fileStream = File.Create(filepath);
            Result<long> copyResult;
            yield return copyResult = item.Data.CopyTo(fileStream, item.DataLength, new Result<long>()).Catch();
            item.Data.Close();
            fileStream.Close();
            if(copyResult.HasException) {
                result.Throw(copyResult.Exception);
                yield break;
            }
            if(item.DataLength != copyResult.Value) {
                throw new IOException(string.Format("tried to write {0} bytes, but wrote {1} instead for {2}", item.DataLength, copyResult.Value, filepath));
            }
            _log.DebugFormat("saved: {0}", filepath);
            AddFileMap(item.DataId, file);
            item.Data.Close();
            result.Return();
            yield break;
        }

        protected override Yield WritePackageDoc_Helper(XDoc package, Result result) {
            package.Save(Path.Combine(_packageDirectory, "package.xml"), true);
            result.Return();
            yield break;
        }
    }
}

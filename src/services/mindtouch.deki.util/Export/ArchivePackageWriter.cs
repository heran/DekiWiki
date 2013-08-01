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
using ICSharpCode.SharpZipLib.Zip;
using log4net;
using MindTouch.IO;
using MindTouch.Tasking;
using MindTouch.Xml;

namespace MindTouch.Deki.Export {
    using Yield = IEnumerator<IYield>;

    public class ArchivePackageWriter : PackageWriterBase {

        //--- Class Fields ---
        private static readonly ILog _log = LogUtils.CreateLog();

        //--- Fields ---
        private readonly Stream _outputStream;
        private readonly ZipOutputStream _archiveStream;

        //--- Constructors ---
        public ArchivePackageWriter(string archiveFilePath) : this(File.Create(archiveFilePath)) { }

        public ArchivePackageWriter(Stream archiveStream) {
            _outputStream = archiveStream;
            _archiveStream = new ZipOutputStream(archiveStream);
            _archiveStream.SetLevel(9);
        }

        //--- Methods ---
        protected override Yield WriteData_Helper(ExportItem item, Result result) {
            var tempfilename = Path.GetTempFileName();
            var file = GetFilePath(item);
            using(var fileStream = File.Create(tempfilename)) {
                Result<long> copyResult;
                yield return copyResult = item.Data.CopyTo(fileStream, item.DataLength, new Result<long>()).Catch();
                item.Data.Close();
                if(copyResult.HasException) {
                    result.Throw(copyResult.Exception);
                    yield break;
                }
                if(item.DataLength != copyResult.Value) {
                    throw new IOException(string.Format("tried to write {0} bytes, but wrote {1} instead for {2}", item.DataLength, copyResult.Value, tempfilename));
                }
                fileStream.Seek(0, SeekOrigin.Begin);
                yield return WriteZipStream(file, fileStream, item.DataLength, new Result());
            }
            File.Delete(tempfilename);
            _log.DebugFormat("saved: {0}", file);
            AddFileMap(item.DataId, file);
            item.Data.Close();
            result.Return();
            yield break;
        }

        private Result WriteZipStream(string filepath, Stream data, long length, Result result) {
            return Async.Fork(delegate() {
                _archiveStream.PutNextEntry(new ZipEntry(filepath));
                int totalRead = 0;
                const int bufferSize = 4096;
                byte[] buffer = new byte[bufferSize];
                while(totalRead < length) {
                    int read = data.Read(buffer, 0, bufferSize);
                    _archiveStream.Write(buffer, 0, read);
                    totalRead += read;
                }
                data.Close();
            }, result);
        }

        protected override Yield WritePackageDoc_Helper(XDoc package, Result result) {
            MemoryStream stream = new MemoryStream(package.ToBytes());
            yield return WriteZipStream("package.xml", stream, stream.Length, new Result());
            _archiveStream.Finish();
            _archiveStream.Close();
            _outputStream.Close();
            result.Return();
            yield break;
        }
    }
}

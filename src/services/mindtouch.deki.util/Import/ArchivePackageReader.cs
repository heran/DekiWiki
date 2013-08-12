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

using MindTouch.Dream;
using MindTouch.Tasking;
using MindTouch.Xml;

namespace MindTouch.Deki.Import {
    using Yield = IEnumerator<IYield>;

    public class ArchivePackageReader : IPackageReader {
        private readonly Stream _archiveStream;
        private readonly ZipFile _zipFile;
        private XDoc _package;

        public ArchivePackageReader(string archiveFilePath) : this(File.OpenRead(archiveFilePath)) { }

        public ArchivePackageReader(Stream archiveStream) {
            _archiveStream = archiveStream;
            _zipFile = new ZipFile(archiveStream);
        }

        public Result<XDoc> ReadManifest(Result<XDoc> result) {
            return Coroutine.Invoke(ReadManifest_Helper, new Result<XDoc>());
        }

        private Yield ReadManifest_Helper(Result<XDoc> result) {
            if(_package == null) {
                yield return Coroutine.Invoke(ReadPackage_Helper, new Result());
            }
            result.Return(_package["manifest"]);
            yield break;
        }

        private Yield ReadPackage_Helper(Result result) {
            ZipEntry manifestEntry = _zipFile.GetEntry("package.xml");
            Result<MemoryStream> readResult;
            yield return readResult = ReadZipStream(manifestEntry, new Result<MemoryStream>());
            using(TextReader reader = new StreamReader(readResult.Value)) {
                _package = XDocFactory.From(reader, MimeType.TEXT_XML);
            }
            result.Return();
        }

        private Result<MemoryStream> ReadZipStream(ZipEntry entry, Result<MemoryStream> result) {
            return Async.Fork(delegate() {
                MemoryStream outputStream = new MemoryStream((int)entry.Size);
                Stream entryStream = _zipFile.GetInputStream(entry);
                int totalRead = 0;
                const int bufferSize = 4096;
                byte[] buffer = new byte[bufferSize];
                while(totalRead < entry.Size) {
                    int read = entryStream.Read(buffer, 0, bufferSize);
                    outputStream.Write(buffer, 0, read);
                    totalRead += read;
                }
                entryStream.Close();
                outputStream.Position = 0;
                return outputStream;
            }, result);
        }

        public Result<ImportItem> ReadData(ImportItem item, Result<ImportItem> result) {
            return Coroutine.Invoke(ReadData_Helper, item, result);
        }

        private Yield ReadData_Helper(ImportItem item, Result<ImportItem> result) {
            if(_package == null) {
                yield return Coroutine.Invoke(ReadPackage_Helper, new Result());
            }
            string file = _package[string.Format("map/item[@dataid='{0}']/@path", item.DataId)].AsText;
            ZipEntry entry = _zipFile.GetEntry(file);
            result.Return(item.WithData(_zipFile.GetInputStream(entry), entry.Size));
            yield break;
        }

        public Result CloseAsync(Result result) {
            _zipFile.Close();
            _archiveStream.Close();
            result.Return();
            return result;
        }

        public void Close() {
            CloseAsync(new Result()).Wait();
        }

        public void Dispose() {
            Close();
        }
    }
}

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

using MindTouch.Dream;
using MindTouch.Tasking;
using MindTouch.Xml;

namespace MindTouch.Deki.Import {
    using Yield = IEnumerator<IYield>;

    public class FilePackageReader : IPackageReader {
        private readonly string _packageDirectory;
        private readonly XDoc _package;

        public FilePackageReader(string packageDirectory) {
            _packageDirectory = packageDirectory;
            string path = Path.Combine(_packageDirectory, "package.xml");
            if(!File.Exists(path)) {
                throw new FileNotFoundException("Unable to locate manifest", path);
            }
            _package = XDocFactory.LoadFrom(path, MimeType.TEXT_XML);
        }

        public Result<XDoc> ReadManifest(Result<XDoc> result) {
            result.Return(_package["manifest"]);
            return result;
        }

        public Result<ImportItem> ReadData(ImportItem item, Result<ImportItem> result) {
            string file = _package[string.Format("map/item[@dataid='{0}']/@path", item.DataId)].AsText;
            string path = Path.Combine(_packageDirectory, file);
            if(!File.Exists(path)) {
                throw new FileNotFoundException(string.Format("Unable to locate file for dataid '{0}'", item.DataId), path);
            }
            FileStream fileStream = File.OpenRead(path);
            result.Return(item.WithData(fileStream, fileStream.Length));
            return result;
        }

        public Result CloseAsync(Result result) {
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

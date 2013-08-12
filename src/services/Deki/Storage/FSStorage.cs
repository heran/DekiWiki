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
using System.IO;
using log4net;
using MindTouch.Deki.Exceptions;
using MindTouch.Dream;
using MindTouch.Deki.Logic;
using MindTouch.Deki.Data;
using MindTouch.IO;
using MindTouch.Tasking;
using MindTouch.Xml;

namespace MindTouch.Deki.Storage {
    public class FSStorage : IStorageProvider{

        //--- Constants ---

        //--- Class Fields ---
        private static readonly ILog _log = LogUtils.CreateLog();

        //--- Fields ---
        private string _path;
        private string _cache;

        //--- Constructors ---
        public FSStorage(XDoc configuration) {           
            _path = configuration["path"].AsText;
            if(string.IsNullOrEmpty(_path)) {
                throw new StoragePathConfigMissingInvalidArgumentException();
            }

            _cache = configuration["cache-path"].AsText ?? Path.Combine(_path, ".cache");
        }

        //--- Methods ---
        public StreamInfo GetFile(ResourceBE attachment, SizeType size, bool allowFileLink) {
            MimeType mime;

            switch (size) {
            case SizeType.THUMB:
            case SizeType.WEBVIEW:
                mime = AttachmentPreviewBL.ResolvePreviewMime(attachment.MimeType);
                break;
            default:
                mime = attachment.MimeType;
                break;
            }

            return GetFileInternal(FilePath(attachment, size), mime);
        }

        public void PutFile(ResourceBE attachment, SizeType size, StreamInfo file) {
            PutFileInternal(FilePath(attachment, size), file, false);
        }

        public void MoveFile(ResourceBE attachment, PageBE targetPage) {
            //do nothing.
        }

        public void DeleteFile(ResourceBE attachment, SizeType size) {
            if (!attachment.IsHeadRevision()) {
                throw new StorageNonHeadRevisionDeleteFatalException();
            }
            for (int i = 1; i <= attachment.Content.Revision; i++) {
                string filename = FilePath(attachment, size);
                try {
                    File.Delete(filename);                    
                } catch { }
            }
        }

        public void PutSiteFile(string label, StreamInfo file) {
            PutFileInternal(SiteFilePath(label), file, true);
        }

        public DateTime GetSiteFileTimestamp(string label){
            return File.GetLastWriteTimeUtc(SiteFilePath(label));
        }

        public StreamInfo GetSiteFile(string label, bool allowFileLink) {
            return GetFileInternal(SiteFilePath(label), MimeType.FromFileExtension(label));
        }

        public void DeleteSiteFile(string label) {
            try {
                File.Delete(SiteFilePath(label));
            } catch { }
        }

        public void Dispose() { }

        private string FilePath(uint fileid, uint revision, SizeType size) {
            string path;
            switch(size) {
            case SizeType.THUMB:
                path = Utils.PathCombine(_path, NumToPath(fileid, ".res"), NumToPath(revision, ".thumb"));
                break;
            case SizeType.WEBVIEW:
                path = Utils.PathCombine(_path, NumToPath(fileid, ".res"), NumToPath(revision, ".webview"));
                break;
            default:
                path = Utils.PathCombine(_path, NumToPath(fileid, ".res"), NumToPath(revision, ".bin"));
                break;
            }
            return path;
        }

        private string NumToPath(uint num, string suffix) {
            string id = num.ToString();
            string path = "";
            for(int i = id.Length; i > 3; i -= 3) {
                path += id.Substring(0, 3) + "/";
                id = id.Substring(3);
            }
            return path + id + suffix;
        }

        private string FilePath(ResourceBE file, SizeType size) {
            return FilePath(file.ResourceId, file.Content.Revision ?? 0, size);
        }

        private string SiteFilePath(string label) {
            return Utils.PathCombine(_path, "SITE", label);
        }

        private StreamInfo GetFileInternal(string path, MimeType type) {
            StreamInfo result = null;
            if(File.Exists(path)) {
                DateTime modified = File.GetLastWriteTimeUtc(path);
                Stream stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                result = new StreamInfo(stream, stream.Length, type, modified);
            } else {
                if(!Directory.Exists(_path)) {
                    _log.WarnMethodCall("file storage directory not found", _path);
                } else {
                    _log.WarnMethodCall("file not found", path);
                }
            }
            return result;
        }

        private void PutFileInternal(string filename, StreamInfo file, bool overwrite) {
            using(file) {

                // TODO (MaxM): A disk space check should be performed here.         

                // make sure destination location exists
                string directory = Path.GetDirectoryName(filename);
                if(!Utils.EnsureDirectoryExists(directory)) {
                    _log.WarnMethodCall("cannot create directory", directory);
                    throw new StorageDirectoryCreationFatalException(directory);
                }

                // copy stream to destination file. Error out if file already exists if not overwriting
                try {
                    FileMode overWriteMode = overwrite ? FileMode.Create : FileMode.CreateNew;
                    using(Stream stream = File.Open(filename, overWriteMode, FileAccess.Write)) {

                        file.Stream.CopyTo(stream, file.Length, new Result<long>(TimeSpan.MaxValue)).Wait();
                    }
                } catch(Exception e) {
                    _log.WarnExceptionMethodCall(e, "Unhandled exception while saving attachment", filename);
                    throw new StorageFileSaveFatalException(filename, e.Message);
                }
            }
        }
    }
}

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
using System.Security.Cryptography;
using System.Text;
using log4net;
using MindTouch.Deki.Data;
using MindTouch.Dream;
using MindTouch.IO;
using MindTouch.Tasking;
using MindTouch.Xml;
using MindTouch.Extensions.Time;

namespace MindTouch.Deki.Storage {
    public class S3Storage : IStorageProvider {

        //--- Constants ---
        private const string AWS_DATE = "X-Amz-Date";
        private const double DEFAUTL_S3_TIMEOUT = 30;

        //--- Fields ---
        private readonly TaskTimerFactory _timerFactory;
        private readonly ILog _log;
        private readonly string _publicKey;
        private readonly string _privateKey;
        private readonly string _bucket;
        private readonly Plug _s3;
        private readonly string _prefix;
        private readonly bool _allowRedirects;
        private readonly TimeSpan _redirectTimeout;
        private readonly string _tempDirectory;
        private readonly TimeSpan _cacheTtl;
        private readonly Dictionary<string, Tuplet<string, TaskTimer, DateTime?>> _cache = new Dictionary<string, Tuplet<string, TaskTimer, DateTime?>>();
        private bool _disposed;

        //--- Constructors ---
        public S3Storage(XDoc configuration, ILog log) {
            _timerFactory = TaskTimerFactory.Create(this);
            _log = log;
            _publicKey = configuration["publickey"].AsText;
            _privateKey = configuration["privatekey"].AsText;
            _bucket = configuration["bucket"].AsText;
            _prefix = configuration["prefix"].AsText;
            if(string.IsNullOrEmpty(_publicKey)) {
                throw new ArgumentException("Invalid Amazon S3 publickey");
            }
            if(string.IsNullOrEmpty(_privateKey)) {
                throw new ArgumentException("Invalid Amazon S3 privatekey");
            }
            if(string.IsNullOrEmpty(_bucket)) {
                throw new ArgumentException("Invalid Amazon S3 bucket");
            }
            if(string.IsNullOrEmpty(_prefix)) {
                throw new ArgumentException("Invalid Amazon S3 prefix");
            }
            _tempDirectory = Path.Combine(Path.GetTempPath(), "s3_cache_" + XUri.EncodeSegment(_prefix));
            if(Directory.Exists(_tempDirectory)) {
                Directory.Delete(_tempDirectory, true);
            }
            Directory.CreateDirectory(_tempDirectory);
            _allowRedirects = configuration["allowredirects"].AsBool ?? false;
            _redirectTimeout = TimeSpan.FromSeconds(configuration["redirecttimeout"].AsInt ?? 60);
            _cacheTtl = (configuration["cachetimeout"].AsInt ?? 60 * 60).Seconds();

            // initialize S3 plug
            _s3 = Plug.New("http://s3.amazonaws.com", TimeSpan.FromSeconds(configuration["timeout"].AsDouble ?? DEFAUTL_S3_TIMEOUT)).WithPreHandler(S3AuthenticationHeader).At(_bucket);
        }

        //--- Methods ---
        public StreamInfo GetFile(ResourceBE attachment, SizeType size, bool allowFileLink) {
            MimeType mime;

            switch(size) {
            case SizeType.THUMB:
            case SizeType.WEBVIEW:
                mime = Logic.AttachmentPreviewBL.ResolvePreviewMime(attachment.MimeType);
                break;
            default:
                mime = attachment.MimeType;
                break;
            }

            return GetFileInternal(BuildS3Filename(attachment, size), mime, allowFileLink);
        }

        public void PutFile(ResourceBE attachment, SizeType size, StreamInfo file) {
            CheckDisposed();
            PutFileInternal(BuildS3Filename(attachment, size), attachment.Name, file);
        }

        public void MoveFile(ResourceBE attachment, PageBE targetPage) {
            CheckDisposed();
            //Nothing to do here.
        }

        public void DeleteFile(ResourceBE attachment, SizeType size) {
            CheckDisposed();
            DeleteFileInternal(BuildS3Filename(attachment, size));
        }

        public void PutSiteFile(string label, StreamInfo file) {
            CheckDisposed();
            PutFileInternal(BuildS3SiteFilename(label), string.Empty, file);
        }

        public DateTime GetSiteFileTimestamp(string label) {
            CheckDisposed();
            return GetFileTimeStampInternal(BuildS3SiteFilename(label));
        }

        public StreamInfo GetSiteFile(string label, bool allowFileLink) {
            CheckDisposed();
            return GetFileInternal(BuildS3SiteFilename(label), MimeType.FromFileExtension(label), allowFileLink);
        }

        public void DeleteSiteFile(string label) {
            CheckDisposed();
            DeleteFileInternal(BuildS3SiteFilename(label));
        }

        public void Dispose() {
            _disposed = true;
            _timerFactory.Dispose();
            if(!Directory.Exists(_tempDirectory)) {
                return;
            }
            try {
                Directory.Delete(_tempDirectory, true);
            } catch(Exception e) {
                _log.Warn(string.Format("unable to delete cache directory '{0}': {1}", _tempDirectory, e.Message), e);
            }
        }

        private void CheckDisposed() {
            if(_disposed) {
                throw new ObjectDisposedException(GetType().Name, string.Format("S3Storage for for prefix '{0}'", _prefix));
            }
        }

        private StreamInfo GetFileInternal(string filename, MimeType type, bool allowFileLink) {

            if(allowFileLink && _allowRedirects) {
                return new StreamInfo(BuildS3Uri(Verb.GET, _s3.AtPath(filename), _redirectTimeout));
            }

            // check if file is cached
            var entry = GetCachedEntry(filename);
            if(entry != null) {
                Stream filestream = File.Open(entry.Item1, FileMode.Open, FileAccess.Read, FileShare.Read);
                return new StreamInfo(filestream, filestream.Length, type, entry.Item3);
            }

            // get file from S3
            var result = new Result<DreamMessage>();
            _s3.AtPath(filename).InvokeEx(Verb.GET, DreamMessage.Ok(), result);
            var response = result.Wait();
            try {
                if(response.IsSuccessful) {
                    return new StreamInfo(response.AsStream(), response.ContentLength, response.ContentType, GetLastModifiedTimestampFromResponse(response));
                }
                if(response.Status == DreamStatus.NotFound) {
                    response.Close();
                    return null;
                }
                throw new DreamInternalErrorException(string.Format("S3 unable to fetch file (status {0}, message {1})", response.Status, response.AsText()));
            } catch {
                if(response != null) {
                    response.Close();
                }
                throw;
            }
        }

        private void PutFileInternal(string s3Filename, string filename, StreamInfo file) {
            var tmpfile = Path.Combine(_tempDirectory, Guid.NewGuid() + ".cache");
            try {
                using(file) {
                    Tuplet<string, TaskTimer, DateTime?> entry = null;

                    // create tmp file
                    try {

                        // copy stream to tmp file
                        using(Stream stream = File.Create(tmpfile)) {
                            file.Stream.CopyTo(stream, file.Length, new Result<long>(TimeSpan.MaxValue)).Wait();
                        }

                        // create cached entry
                        if(_cacheTtl != TimeSpan.Zero) {
                            lock(_cache) {
                                if(_cache.TryGetValue(s3Filename, out entry)) {
                                    entry.Item2.Change(_cacheTtl, TaskEnv.None);
                                    entry.Item3 = file.Modified;
                                } else {
                                    var timer = _timerFactory.New(_cacheTtl, OnTimer, s3Filename, TaskEnv.None);
                                    _cache[s3Filename] = entry = new Tuplet<string, TaskTimer, DateTime?>(tmpfile, timer, file.Modified);
                                }
                            }
                        }
                    } catch(Exception e) {
                        try {

                            // delete tmp file and clear out timer and cache, if any exist
                            SafeFileDelete(tmpfile);
                            if(entry != null) {
                                lock(_cache) {
                                    entry.Item2.Cancel();
                                    _cache.Remove(s3Filename);
                                }
                            }
                        } catch(Exception e2) {
                            _log.WarnFormat("Failed cleaned-up post tmp file creation failure for attachment {0}: {1}", s3Filename, e2.Message);
                        }
                        throw new DreamInternalErrorException(string.Format("Unable to cache file attachment to '{0}' ({1})", s3Filename, e.Message));
                    }
                }

                // forward cached file to S3
                Stream filestream = File.Open(tmpfile, FileMode.Open, FileAccess.Read, FileShare.Read);
                file = new StreamInfo(filestream, file.Length, file.Type);
                var s3Msg = DreamMessage.Ok(file.Type, file.Length, file.Stream);
                s3Msg.Headers.ContentDisposition = new ContentDisposition(true, DateTime.UtcNow, null, null, filename, file.Length);

                // Note (arnec): The timeout is just a workaround Plug not having some kind of heartbeat on progress. Ideally 30 seconds of inactivity
                // should be perfectly fine, as long as we track uploads that are proceeding as active
                _s3.AtPath(s3Filename).WithTimeout(TimeSpan.FromMinutes(30)).Put(s3Msg);
            } finally {
                if(_cacheTtl == TimeSpan.Zero) {
                    SafeFileDelete(tmpfile);
                }
            }
        }

        private void DeleteFileInternal(string filename) {
            RemoveCachedEntry(filename);
            _s3.AtPath(filename).DeleteAsync().Wait();
        }

        private DateTime GetFileTimeStampInternal(string filename) {

            // check cache
            var entry = GetCachedEntry(filename);
            if(entry != null) {
                return entry.Item3 ?? DateTime.MinValue;
            }

            // get file information from S3
            DreamMessage response = _s3.AtPath(filename).InvokeAsync("HEAD", DreamMessage.Ok()).Wait();
            return GetLastModifiedTimestampFromResponse(response);
        }

        private DateTime GetLastModifiedTimestampFromResponse(DreamMessage response) {
            if(response.IsSuccessful) {
                return response.Headers.LastModified ?? DateTime.MinValue;
            }
            return DateTime.MinValue;
        }

        private string BuildS3Filename(ResourceBE attachment, SizeType size) {

            // Legacy pre-Lyons S3 paths are based on fileid. If the fileid is present in the resource then use it otherwise base on resourceid.
            var id = (attachment.MetaXml.FileId == null) ? string.Format("r{0}", attachment.ResourceId) : attachment.MetaXml.FileId.ToString();
            switch(size) {
            case SizeType.THUMB:
            case SizeType.WEBVIEW:
                return string.Format("{0}/{1}/{2}/{3}", _prefix, id, attachment.Content.Revision - 1, size.ToString().ToLowerInvariant());
            default:
                return string.Format("{0}/{1}/{2}", _prefix, id, attachment.Content.Revision - 1);
            }
        }

        private string BuildS3SiteFilename(string label) {
            return string.Format("{0}/{1}/{2}", _prefix, "SITE", label);
        }

        private DreamMessage S3AuthenticationHeader(string verb, XUri uri, XUri normalizedUri, DreamMessage message) {

            // add amazon date header
            var date = DateTime.UtcNow.ToString("r");
            message.Headers[AWS_DATE] = date;

            // add authorization header
            var result = string.Format("{0}\n{1}\n{2}\n\n{3}:{4}\n{5}", verb, message.Headers[DreamHeaders.CONTENT_MD5], message.ContentType, AWS_DATE.ToLowerInvariant(), date, normalizedUri.Path);
            var hmac = new HMACSHA1(Encoding.UTF8.GetBytes(_privateKey));
            var signature = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(result)));
            message.Headers.Authorization = string.Format("AWS {0}:{1}", _publicKey, signature);
            message.Headers.ContentType = message.ContentType;
            return message;
        }

        private XUri BuildS3Uri(string verb, XUri uri, TimeSpan expireTime) {
            var expireTimeSeconds = ((long)(new TimeSpan(DateTime.UtcNow.Add(expireTime).Subtract(DateTimeUtil.Epoch).Ticks).TotalSeconds)).ToString();
            var result = string.Format("{0}\n{1}\n{2}\n{3}\n{4}", verb, string.Empty, string.Empty, expireTimeSeconds, uri.Path);
            var hmac = new HMACSHA1(Encoding.UTF8.GetBytes(_privateKey));
            var signature = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(result)));
            return uri.With("AWSAccessKeyId", _publicKey).With("Signature", signature).With("Expires", expireTimeSeconds);
        }

        private void OnTimer(TaskTimer timer) {
            if(_disposed) {
                return;
            }
            RemoveCachedEntry((string)timer.State);
        }

        private Tuplet<string, TaskTimer, DateTime?> GetCachedEntry(string filename) {
            if(_cacheTtl == TimeSpan.Zero) {
                return null;
            }
            Tuplet<string, TaskTimer, DateTime?> result;
            lock(_cache) {
                if(_cache.TryGetValue(filename, out result)) {
                    if(File.Exists(result.Item1)) {
                        result.Item2.Change(_cacheTtl, TaskEnv.None);
                    } else {
                        result.Item2.Cancel();
                        _cache.Remove(filename);
                        return null;
                    }
                }
            }
            return result;
        }

        private void RemoveCachedEntry(string filename) {
            Tuplet<string, TaskTimer, DateTime?> entry;
            lock(_cache) {
                if(_cache.TryGetValue(filename, out entry)) {
                    _cache.Remove(filename);
                }
            }
            if(entry != null) {
                SafeFileDelete(entry.Item1);
            }
        }

        private void SafeFileDelete(string filename) {
            if(string.IsNullOrEmpty(filename) || !File.Exists(filename)) {
                return;
            }
            try {
                File.Delete(filename);
            } catch(Exception e) {
                _log.Warn(string.Format("unable to delete cache file '{0}': {1}", filename, e.Message), e);
            }
        }
    }
}

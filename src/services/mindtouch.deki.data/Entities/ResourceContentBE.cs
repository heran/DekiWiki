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

using MindTouch.Dream;
using MindTouch.IO;
using MindTouch.Xml;

namespace MindTouch.Deki.Data {
    public class ResourceContentBE {

        // -- Constructors --
        public ResourceContentBE(bool dbBased) {
            _dbBased = dbBased;
        }

        public ResourceContentBE(uint size, MimeType mimeType)
            : this(false) {
            _size = size;
            _mimeType = mimeType;
        }

        public ResourceContentBE(string value, MimeType mimeType)
            : this(true) {
            if(value == null) {
                throw new ArgumentNullException("value");
            }
            if(mimeType == null) {
                throw new ArgumentNullException("mimeType");
            }
            _mimeType = mimeType;
            _stream = new ChunkedMemoryStream();
            _stream.Write(mimeType.CharSet, value);
            _size = (uint)_stream.Length;
        }

        public ResourceContentBE(XDoc doc)
            : this(true) {
            if(doc == null) {
                throw new ArgumentNullException("doc");
            }
            _stream = new ChunkedMemoryStream();
            doc.WriteTo(_stream);
            _mimeType = MimeType.TEXT_XML;
            _size = (uint)_stream.Length;
        }

        public ResourceContentBE(Stream stream, MimeType mimeType)
            : this(true) {
            if(stream == null) {
                throw new ArgumentNullException("stream");
            }
            if(mimeType == null) {
                throw new ArgumentNullException("mimeType");
            }
            if(!stream.IsStreamMemorized()) {
                throw new ArgumentException("The provided stream must be a supported memory stream type");
            }
            _stream = stream;
            _size = (uint)stream.Length;
            _mimeType = mimeType;
        }

        // -- Fields --
        private readonly bool _dbBased;
        private uint _contentId;
        private uint? _resourceId;
        private uint? _revision;
        private Stream _stream;
        private MimeType _mimeType;
        private string _location;
        private uint _size;

        // -- Properties --
        public uint ContentId {
            get { return _contentId; }
            set { _contentId = value; }
        }

        public uint? ResourceId {
            get { return _resourceId; }
            set { _resourceId = value; }
        }

        public uint? Revision {
            get { return _revision; }
            set { _revision = value; }
        }

        public MimeType MimeType {
            get { return _mimeType; }
            set { _mimeType = value; }
        }

        public string Location {
            get { return _location; }
            set { _location = value; }
        }

        public uint Size {
            get { return _size; }
            set { _size = value; }
        }

        public bool IsDbBased {
            get { return _dbBased; }
        }

        //--- Methods ---
        public string ComputeHashString() {
            if(!IsDbBased) {
                throw new InvalidOperationException("Cannot compute the hash for content that is not db based");
            }
            _stream.Position = 0;
            return _stream.ComputeHashString();
        }

        public Stream ToStream() {
            if(!IsDbBased) {
                throw new InvalidOperationException("Cannot get a stream for content that is not db based");
            }
            var stream = new ChunkedMemoryStream();
            _stream.Position = 0;
            _stream.CopyTo(stream, _size);
            stream.Position = 0;
            return stream;
        }

        public void SetData(byte[] value) {
            if(!IsDbBased) {
                throw new InvalidOperationException("Cannot set the content for a resource that is not db based");
            }
            if(value == null) {
                return;
            }
            _stream = new ChunkedMemoryStream(value);
            _size = (uint)_stream.Length;
        }

        public byte[] ToBytes() {
            if(!IsDbBased) {
                throw new InvalidOperationException("Cannot get the content for a resource that is not db based");
            }
            _stream.Position = 0;
            return _stream.ReadBytes(_size);
        }

        public string ToText() {
            if(!IsDbBased) {
                throw new InvalidOperationException("Cannot get the content for a resource that is not db based");
            }
            _stream.Position = 0;
            var reader = new StreamReader(_stream, MimeType.CharSet);
            return reader.ReadToEnd();
        }

        public bool IsNewContent() {
            return ContentId == 0;
        }
    }
}

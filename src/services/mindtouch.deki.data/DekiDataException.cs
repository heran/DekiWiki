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
using System.Text;

namespace MindTouch.Deki.Data {

    public class DekiDataException : Exception {

        public DekiDataException() { }
        public DekiDataException(string message, Exception inner) : base(message, inner) { }
    }

    public class ResourceRevisionOutOfRangeException : DekiDataException {

        //--- Fields ---
        public readonly string Resource;

        //--- Constructors ---
        public ResourceRevisionOutOfRangeException(string resource) {
            Resource = resource;
        }
    }

    public class ResourceExpectedHeadException : DekiDataException {

        //--- Fields ---
        public readonly int HeadRevision;
        public readonly int Revision;

        //--- Constructors ---
        public ResourceExpectedHeadException(int headRevision, int revision) {
            HeadRevision = headRevision;
            Revision = revision;
        }
    }

    public class ResourceConcurrencyException : DekiDataException {

        private uint _resourceId;

        public uint ResourceId {
            get {
                return _resourceId;
            }
        }

        public ResourceConcurrencyException(uint resourceId) {
            _resourceId = resourceId;
        }
    }

    public class PageConcurrencyException : DekiDataException {

        //--- Constructors ---
        public PageConcurrencyException(ulong pageId, Exception inner)
            : base("A concurrency problem occured persisting page data", inner) {
            PageId = pageId;
        }

        //--- Properties ---

        // TODO (arnec): setter should be switched to private once the whole OldBE/page_id business is done
        public ulong PageId { get; set; }
    }

    public class CommentConcurrencyException : DekiDataException {

        //--- Constructors ---
        public CommentConcurrencyException(ulong pageId, Exception inner)
            : base("A concurrency problem occured persisting a comment", inner) {
            PageId = pageId;
        }

        //--- Properties ---

        public ulong PageId { get; private set; }
    }

    public class OldIdNotFoundException : DekiDataException {

        private ulong _oldId;
        private DateTime _timeStamp;

        public ulong OldId {
            get {
                return _oldId;
            }
        }

        public DateTime TimeStamp {
            get {
                return _timeStamp;
            }
        }

        public OldIdNotFoundException(ulong oldId, DateTime timeStamp) {
            _oldId = oldId;
            _timeStamp = timeStamp;
        }
    }

    public class PageIdNotFoundException : DekiDataException {

        private ulong _pageId;

        public ulong PageId {
            get {
                return _pageId;
            }
        }

        public PageIdNotFoundException(ulong pageId) {
            _pageId = pageId;
        }
    }

    public class HomePageNotFoundException : DekiDataException { }
    public class TooManyResultsException : DekiDataException { }
}

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

namespace MindTouch.Deki.Data {

    [Serializable]
    public class RatingBE {

        //--- Types ---
        public enum Type : byte {
            PAGE = 1
        };

        // -- Members --
        uint _id;
        uint _userId;
        ulong _resourceId;
        Type _resourceType;
        ulong? _resourceRevision;
        float _score;
        DateTime _timestamp;
        DateTime? _timestampReset;

        // -- Properties --
        public uint Id {
            get { return _id; }
            set { _id = value; }
        }

        public uint UserId {
            get { return _userId; }
            set { _userId = value; }
        }

        public ulong ResourceId {
            get { return _resourceId; }
            set { _resourceId = value; }
        }

        public Type ResourceType {
            get { return _resourceType; }
            set { _resourceType = value; }
        }

        public ulong? ResourceRevision {
            get { return _resourceRevision; }
            set { _resourceRevision = value; }
        }

        public float Score {
            get { return _score; }
            set { _score = value; }
        }

        public DateTime Timestamp {
            get { return _timestamp; }
            set { _timestamp = value; }
        }

        public DateTime? TimestampReset {
            get { return _timestampReset; }
            set { _timestampReset = value; }
        }

        //--- Methods ---
        public virtual RatingBE Copy() {
            RatingBE r = new RatingBE();
            r.Id = Id;
            r.ResourceId = ResourceId;
            r.ResourceType = ResourceType;
            r.Score = Score;
            r.Timestamp = Timestamp;
            r.ResourceRevision = ResourceRevision;
            r.TimestampReset = TimestampReset;
            r.UserId = UserId;
            return r;
        }
    }
}
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
    public class TransactionBE {

        //--- Fields ---
        protected uint _id;
        protected Title _title = null;
        protected ulong _pageId;
        protected uint _userId;
        protected RC _type;
        protected DateTime _timeStamp;
        protected bool _reverted;
        protected uint? _revertUserId;
        protected DateTime? _revertTimeStamp;
        protected string _revertReason;

        //--- Properties ---
        public virtual uint Id {
            get { return _id; }
            set { _id = value; }
        }

        public virtual ushort _Namespace {
            get { return (ushort) Title.Namespace; }
            set { Title.Namespace = (NS) value; }
        }

        public virtual string _Title {
            get { return Title.AsUnprefixedDbPath(); }
            set { Title.Path = value; }
        }

        public virtual Title Title {
            get {
                if (null == _title) {
                    _title = Title.FromDbPath(NS.UNKNOWN, String.Empty, null);
                }
                return _title;
            }
            set {
                _title = value;
            }
        }

        public virtual ulong PageId {
            get { return _pageId; }
            set { _pageId = value; }
        }

        public virtual uint UserId {
            get { return _userId; }
            set { _userId = value; }
        }

        public virtual RC Type {
            get { return _type; }
            set { _type = value; }
        }

        public virtual uint _Type {
            get { return (uint)_type; }
            set { _type = (RC) value; }
        }

        public virtual DateTime TimeStamp {
            get { return _timeStamp; }
            set {
                _timeStamp = value;
                if (_timeStamp.Kind == DateTimeKind.Unspecified)
                    _timeStamp = DateTime.SpecifyKind(value, DateTimeKind.Utc);
            }
        }

        public virtual bool Reverted {
            get { return _reverted; }
            set { _reverted = value; }
        }

        public virtual uint? RevertUserId {
            get { return _revertUserId; }
            set { _revertUserId = value; }
        }

        public virtual DateTime? RevertTimeStamp {
            get { return _revertTimeStamp; }
            set {
                _revertTimeStamp = value;
                if (value != null && _revertTimeStamp.Value.Kind == DateTimeKind.Unspecified)
                    _revertTimeStamp = DateTime.SpecifyKind(value.Value, DateTimeKind.Utc);
            }
        }

        public virtual string RevertReason {
            get { return _revertReason; }
            set { _revertReason = value; }
        }

        //--- Methods ---
        public virtual TransactionBE Copy() {
            TransactionBE transaction = new TransactionBE();
            transaction._Namespace = _Namespace;
            transaction._Title = _Title;
            transaction._Type = _Type;
            transaction.Id = Id;
            transaction.PageId = PageId;
            transaction.Reverted = Reverted;
            transaction.RevertReason = RevertReason;
            transaction.RevertTimeStamp = RevertTimeStamp;
            transaction.RevertUserId = RevertUserId;
            transaction.TimeStamp = TimeStamp;
            transaction.UserId = UserId;
            return transaction;
        }
    }
}

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
    public class BanBE {

        //--- Fields ---
        protected uint _banId;
        protected uint _banByUserId;
        protected DateTime? _banExpires;
        protected string _banReason;
        protected ulong _banRevokeMask;
        protected DateTime _banLastEdit;
        protected List<string> _banAddresses;
        protected List<uint> _banUserIds;

        //--- Properties ---
        public virtual uint Id {
            get { return _banId; }
            set { _banId = value; }
        }

        public virtual uint ByUserId {
            get { return _banByUserId; }
            set { _banByUserId = value; }
        }

        public virtual DateTime? Expires {
            get { return _banExpires; }
            set { _banExpires = value; }
        }

        public virtual string Reason {
            get { return _banReason; }
            set { _banReason = value; }
        }

        public virtual ulong RevokeMask {
            get { return _banRevokeMask; }
            set { _banRevokeMask = value; }
        }

        public virtual DateTime LastEdit {
            get { return _banLastEdit; }
            set { _banLastEdit = value; }
        }

        public virtual List<string> BanAddresses {
            get { return _banAddresses; }
            set { _banAddresses = value; }
        }

        public virtual List<uint> BanUserIds {
            get { return _banUserIds; }
            set { _banUserIds = value; }
        }

        public virtual string _BanUserIds {
            set { _banUserIds = new List<uint>(DbUtils.ConvertDelimittedStringToArray<uint>('\n', value ?? string.Empty)); }
        }

        public virtual string _BanAddresses {
            set { _banAddresses = new List<string>((value ?? string.Empty).Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries)); }
        }

        //--- Methods ---
        public virtual BanBE Copy() {
            BanBE ban = new BanBE();
            ban.BanAddresses = new List<string>(BanAddresses);
            ban.BanUserIds = new List<uint>(BanUserIds);
            ban.ByUserId = ByUserId;
            ban.Expires = Expires;
            ban.Id = Id;
            ban.LastEdit = LastEdit;
            ban.Reason = Reason;
            ban.RevokeMask = RevokeMask;
            return ban;
        }
    }
}

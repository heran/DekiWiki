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

using MindTouch.Dream;

namespace MindTouch.Deki.Data {

    public class GroupBE {

        //--- Fields ---
        protected uint _id;
        protected string _name;
        protected uint _roleId;
        protected uint _serviceId;
        protected DateTime _timeStamp;
        protected uint _creatorUserId;
        protected string _userIds;

        //--- Properties ---
        public virtual uint Id {
            get { return _id; }
            set { _id = value; }
        }

        public virtual string Name {
            get { return _name; }
            set { _name = value; }
        }

        public virtual uint RoleId {
            get { return _roleId; }
            set { _roleId = value; }
        }

        public virtual uint ServiceId {
            get { return _serviceId; }
            set { _serviceId = value; }
        }

        public virtual DateTime TimeStamp {
            get { return _timeStamp; }
            set { _timeStamp = value; }
        }

        public virtual uint CreatorUserId {
            get { return _creatorUserId; }
            set { _creatorUserId = value; }
        }

        public virtual string UserIds {
            get { return _userIds; }
            set { _userIds = value; }
        }

        public virtual uint[] UserIdsList {
            get {
                if (string.IsNullOrEmpty(UserIds))
                    return null;
                else
                    return DbUtils.ConvertDelimittedStringToArray<uint>(',', UserIds);
            }
        }

        //--- Methods ---
        public virtual GroupBE Copy() {
            GroupBE group = new GroupBE();
            group.CreatorUserId = CreatorUserId;
            group.Id = Id;
            group.Name = Name;
            group.RoleId = RoleId;
            group.ServiceId = ServiceId;
            group.TimeStamp = TimeStamp;
            group.UserIds = UserIds;
            return group;
        }
    }
}

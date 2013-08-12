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
using System.Data.SqlClient;
using System.Data;
using MindTouch.Dream;

namespace MindTouch.Deki.Data {

    [Serializable]    
    public class GrantBE {

        //--- Fields ---
        protected uint _id;
        protected DateTime _expirationDate = DateTime.MaxValue;
        protected DateTime _timeStamp = DateTime.MinValue;
        protected GrantType _grantType = GrantType.UNDEFINED;
        protected uint _userId;
        protected uint _groupId;
        protected uint _pageId;
        protected uint _roleId;
        protected RoleBE _roleBE;
        protected uint _creatorUserId;

        //--- Properties ---
        public virtual uint Id {
            get { return _id; }
            set { _id = value; }
        }

        public virtual DateTime ExpirationDate {
            get { return _expirationDate; }
            set { _expirationDate = value; }
        }

        public virtual DateTime TimeStamp {
            get { return _timeStamp; }
            set { _timeStamp = value; }
        }

        public virtual GrantType Type {
            get { return _grantType; }
            set { _grantType = value; }
        }

        public virtual uint UserId {
            get { return _userId; }
            set { _userId = value; }
        }

        public virtual uint GroupId {
            get { return _groupId; }
            set { _groupId = value; }
        }

        public virtual uint PageId {
            get { return _pageId; }
            set { _pageId = value; }
        }

        public virtual uint RoleId {
            get { return _roleId; }
            set { _roleId = value; }
        }

        public virtual RoleBE Role {
            get { return _roleBE; }
            set { _roleBE = value; }
        }

        public virtual uint CreatorUserId {
            get { return _creatorUserId; }
            set { _creatorUserId = value; }
        }

        //--- Methods ---
        public virtual GrantBE Copy() {
            GrantBE grant = new GrantBE();
            grant.Id = Id;
            grant.PageId = PageId;
            grant.UserId = UserId;
            grant.GroupId = GroupId;
            grant.ExpirationDate = ExpirationDate;
            grant.TimeStamp = TimeStamp;
            grant.CreatorUserId = CreatorUserId;
            grant.RoleId = RoleId;
            grant.Role = Role.Copy();
            grant.Type = Type;
            return grant;
        }
    }
}

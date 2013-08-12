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
    public class UserBE  {

        //--- Fields ---
        protected uint _id;
        protected string _name;
        protected string _realName;
        protected string _password;
        protected string _newPassword;
        protected string _email;
        protected DateTime _touched;
        protected uint _roleId;
        protected bool _active;
        protected string _externalName;
        protected uint _serviceId = 0;
        protected DateTime _createTimestamp;
        protected string _language;
        protected string _timezone;
        protected bool _licenseSeat;
        
        //--- Constructors ---
        public UserBE() {
            this.ID = 0;
            this.RealName = string.Empty;
            this.Email = string.Empty;
            this.Password = string.Empty;
            this.NewPassword = string.Empty;
            this.Touched = DateTime.UtcNow;
            this._active = true;
        }

        //--- Properties ---
        public virtual uint ID {
            get { return _id; }
            set { _id = value; }
        }

        public virtual string Name {
            get { return _name; }
            set { _name = value; }
        }

        public virtual string RealName {
            get { return _realName; }
            set { _realName = value; }
        }

        public virtual byte[] _Password {
            get { return DbUtils.ToBlob(_password); }
            set { _password = DbUtils.ToString(value); }
        }
        public virtual string Password {
            get { return _password; }
            set { _password = value; }
        }

        public virtual byte[] _NewPassword {
            get { return DbUtils.ToBlob(_newPassword); }
            set { _newPassword = DbUtils.ToString(value); }
        }
        public virtual string NewPassword {
            get { return _newPassword; }
            set { _newPassword = value; }
        }

        public virtual string Email {
            get { return _email; }
            set { _email = value; }
        }

        public virtual string _Touched {
            get { return DbUtils.ToString(_touched); }
            set { _touched = DbUtils.ToDateTime(value); }
        }
        public virtual DateTime Touched {
            get { return _touched; }
            set { _touched = value; }
        }

        public virtual uint RoleId {
            get { return _roleId; }
            set { _roleId = value;  }
        }

        public virtual bool UserActive {
            get { return _active; }
            set { _active = value; }
        }

        public virtual string ExternalName {
            get { return _externalName; }
            set { _externalName = value; }
        }

        public virtual uint ServiceId {
            get { return _serviceId; }
            set { _serviceId = value; }
        }

        public virtual DateTime CreateTimestamp {
            get { return _createTimestamp; }
            set {
                _createTimestamp = value;
                if(_createTimestamp.Kind == DateTimeKind.Unspecified) {
                    _createTimestamp = DateTime.SpecifyKind(_createTimestamp, DateTimeKind.Utc);
                }
            }
        }

        public virtual string Language {
            get { return _language; }
            set { _language = value; }
        }

        public virtual string Timezone {
            get { return _timezone; }
            set { _timezone = value; }
        }

        public virtual bool LicenseSeat {
            get { return _licenseSeat; }
            set { _licenseSeat = value; }
        }

        //--- Methods ---
        public virtual UserBE Copy() {
            UserBE user = new UserBE();
            user._NewPassword = _NewPassword;
            user._Password = _Password;
            user._Touched = _Touched;
            user.CreateTimestamp = CreateTimestamp;
            user.Email = Email;
            user.ExternalName = ExternalName;
            user.ID = ID;
            user.Language = Language;
            user.Name = Name;
            user.RealName = RealName;
            user.RoleId = RoleId;
            user.ServiceId = ServiceId;
            user.Timezone = Timezone;
            user.UserActive = UserActive;
            user.LicenseSeat = LicenseSeat;
            return user;
        }
    }
}

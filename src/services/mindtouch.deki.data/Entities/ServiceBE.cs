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
using System.Collections.Specialized;

namespace MindTouch.Deki.Data {

    [Serializable]
    public class ServiceBE {

        //--- Fields ---
        protected uint _id;
        protected ServiceType _type = ServiceType.UNDEFINED;
        protected string _serviceSID;
        protected string _description;
        protected string _uri;
        protected NameValueCollection _serviceConfig = new NameValueCollection();
        protected NameValueCollection _servicePrefs = new NameValueCollection();
        protected string _service_last_status;
        protected byte _service_enabled;
        protected byte _service_local;
        protected DateTime _service_last_edit;

        //--- Properites ---
        public virtual uint Id {
            get { return _id; }
            set { _id = value; }
        }

        public virtual ServiceType Type {
            get { return _type; }
            set { _type = value; }
        }

        public virtual string SID {
            get { return _serviceSID; }
            set { _serviceSID = value; }
        }

        public virtual string Description {
            get { return _description; }
            set { _description = value; }
        }

        public virtual string Uri {
            get { return _uri; }
            set { _uri = value; }
        }

        /// <summary>
        /// Used for starting a service
        /// </summary>
        public virtual NameValueCollection Config {
            get{ return _serviceConfig;}
            set{ _serviceConfig = value;}
        }

        /// <summary>
        /// Preferences for calling a service
        /// </summary>
        public virtual NameValueCollection Preferences {
            get { return _servicePrefs; }
            set { _servicePrefs = value; }
        }

        public virtual string ServiceLastStatus {
            get { return _service_last_status; }
            set { _service_last_status = value; }
        }

        public virtual byte _ServiceEnabled {
            get { return _service_enabled; }
            set { _service_enabled = value; }
        }

         public virtual byte _ServiceLocal {
            get { return _service_local; }
            set { _service_local = value; }        
        }

        public virtual DateTime ServiceLastEdit {
            get { return _service_last_edit; }
            set { _service_last_edit = value; }
        }

        public virtual bool ServiceEnabled {
            get {
                return _ServiceEnabled != 0 ? true : false;
            }
            set {
                _ServiceEnabled = (byte) (value ? 1 : 0);
            }
        }

        public virtual bool ServiceLocal {
            get {
                return _ServiceLocal != 0 ? true : false;
            }
            set {
                _ServiceLocal = (byte) (value ? 1 : 0);
            }
        }

        //--- Methods ---
        public virtual ServiceBE Copy() {
            ServiceBE s = new ServiceBE();
            s._ServiceEnabled = _ServiceEnabled;
            s._ServiceLocal = _ServiceLocal;
            s.Description = Description;
            s.Id = Id;
            s.ServiceLastEdit = ServiceLastEdit;
            s.ServiceLastStatus = ServiceLastStatus;
            s.SID = SID;
            s.Type = Type;
            s.Uri = Uri;
            s.Config = new NameValueCollection(Config);
            s.Preferences = new NameValueCollection(Preferences);
            return s;
        }
    }
}

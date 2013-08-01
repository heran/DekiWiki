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
using MindTouch.Deki.Logic;
using MindTouch.Xml;

namespace MindTouch.Deki.WikiManagement {
    public class LicenseData {

        //--- Types ---
        private struct Data {

            //--- Fields ---

            // Note (arnec): Since licensedocs are checksummed it is assumed that they will never be modified and therefore a
            // copy of Data is considered a full clone.
            public XDoc LicenseDoc;
            public LicenseStateType LicenseState;
            public DateTime LicenseStateChecked;
            public DateTime LicenseExpiration;
            public Permissions AnonymousPermissions;
            public ulong? SiteOwnerUserId;
        }

        //--- Fields ---
        private Data _data;

        //--- Constructors ---
        public LicenseData() {
            _data.LicenseStateChecked = DateTime.MinValue;
        }

        private LicenseData(Data source) {
            _data = source;
        }

        //--- Properties ---
        public LicenseStateType LicenseState { get { return _data.LicenseState; } }
        public DateTime LicenseStateChecked { get { return _data.LicenseStateChecked; } }
        public DateTime LicenseExpiration { get { return _data.LicenseExpiration; } }
        public Permissions AnonymousPermissions { get { return _data.AnonymousPermissions; } }
        public ulong? SiteOwnerUserId { get { return _data.SiteOwnerUserId; } }
        public XDoc LicenseDoc { get { return _data.LicenseDoc ?? XDoc.Empty; } }

        //--- Methods ---
        public void Update(LicenseData licenseData) {
            _data = licenseData._data;
        }

        public LicenseData WithLicenseDocument(XDoc licenseDoc) {
            var clone = _data;
            clone.LicenseDoc = licenseDoc;
            return new LicenseData(clone);
        }

        public LicenseData WithSiteOwnerUserId(ulong? siteOwnerUserId) {
            var clone = _data;
            clone.SiteOwnerUserId = siteOwnerUserId;
            return new LicenseData(clone);
        }

        public LicenseData WithState(LicenseStateType state) {
            var clone = _data;
            clone.LicenseState = state;
            return new LicenseData(clone);
        }

        public LicenseData WithPermissions(Permissions permissions) {
            var clone = _data;
            clone.AnonymousPermissions = permissions;
            return new LicenseData(clone);
        }

        public LicenseData Checked(DateTime checkedDate) {
            var clone = _data;
            clone.LicenseStateChecked = checkedDate;
            return new LicenseData(clone);
        }

        public LicenseData WithExpiration(DateTime expiration) {
            var clone = _data;
            clone.LicenseExpiration = expiration;
            return new LicenseData(clone);
        }

        public LicenseData Clone() {
            return new LicenseData(_data);
        }
    }
}
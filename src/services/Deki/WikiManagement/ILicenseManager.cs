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
using MindTouch.Deki.Data;
using MindTouch.Deki.Logic;
using MindTouch.Xml;

namespace MindTouch.Deki.WikiManagement {

    // Note (arnec): This interface is internal so that it cannot be subtyped to subvert license management outside the signed assembly
    // the implementer itself is not internal so it can still be tested in isolation.
    internal interface ILicenseManager {
    
        //--- Properties ---
        LicenseStateType LicenseState { get; }
        DateTime LicenseExpiration { get; }
        XDoc LicenseDocument { get; }
        XDoc LicenseDocumentOrEmpty { get; }

        //--- Methods ---
        XDoc GetLicenseDocument(bool publicLicense);
        string GetCapability(string name);
        bool IsUserCreationAllowed(bool throwException);
        ulong LicensePermissionRevokeMask();
        ulong AnonymousUserMask(UserBE user);
        bool IsSeatLicensingEnabled();
        void SetUserSeat(UserBE user);
        void UpdateLicense(XDoc currentLicense, XDoc newLicense);
        void RemoveSeatFromUser(UserBE user);
        ulong GetUnseatedUserMask();
        ulong? GetSiteOwnerUserId();
        IEnumerable<UserBE> GetSeatRecommendations(uint? offset, uint? limit, out uint totalCount, out uint queryCount);
    }
}
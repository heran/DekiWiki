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
using MindTouch.Deki.Exceptions;
using MindTouch.Deki.Logic;
using MindTouch.Xml;

namespace MindTouch.Deki.WikiManagement {
    public class LicenseManager : ILicenseManager {

        /* ----------------------------------------------------------------------- *
         * A word about the classes used for license management in MindTouch:
         * 
         * LicenseData
         *   the per instance shared license container. Shared instance is
         *   updated atomically
         * 
         * LicenseBL
         *   business logic for operating on license documents without concept
         *   of current license state
         * 
         * LicenseManager
         *   the per request encapsulation of the shared LicenseData for license
         *   query and manipulation
         *   
         * LicenseController
         *   contains logic for operating on LicenseData in an immutable fashion.
         *   It does not contain license state itself, but always is passed in
         *   LicenseData and if it returns LicenseData, it is a modified clone. It
         *   never modifies the LicenseData directly
         *   
         * RemoteLicenseController
         *   specialized controller used by the RemoteInstanceManager to modify
         *   license transition behavior for remotely managed instances
         *   
         * DekiLicense
         *   contains logic for manipulating licenses shared outside of the
         *   Mindtouch API
         * 
         * ----------------------------------------------------------------------- */


        //--- Constants ---
        public const int LICENSE_CHECK_INTERVAL = 300; //seconds

        //--- Class Fields ---
        public static int LicenseCheckInterval = LICENSE_CHECK_INTERVAL;
        private static readonly log4net.ILog _log = LogUtils.CreateLog();
        private static readonly object _sync = new object();

        //--- Fields ---
        protected readonly ILicenseBL _licenseBL;
        private readonly IInstanceSettings _settings;
        private readonly ISeatingBL _seatingBL;
        private readonly LicenseStateTransitionCallback _licenseStateTransitionCallback;
        private readonly ILicenseController _licenseController;
        private readonly IUserBL _userBL;

        // Note (arnec): Always access via LicenseData, so the proper license checks can be performed
        private readonly LicenseData _licenseData;
        private bool _inLicenseCheck;

        //--- Constructors ---
        public LicenseManager(ILicenseController licenseController, IUserBL userBL, LicenseData licenseData, ILicenseBL licenseBL, IInstanceSettings settings, ISeatingBL seatingBL, LicenseStateTransitionCallback licenseStateTransitionCallback) {
            _licenseController = licenseController;
            _userBL = userBL;
            _licenseData = licenseData;
            _licenseBL = licenseBL;
            _settings = settings;
            _seatingBL = seatingBL;
            _licenseStateTransitionCallback = licenseStateTransitionCallback;
        }

        //--- Properties ---
        public LicenseStateType LicenseState { get { return LicenseData.LicenseState; } }
        public DateTime LicenseExpiration { get { return LicenseData.LicenseExpiration; } }
        public XDoc LicenseDocument { get { return LicenseData.LicenseDoc; } }

        public XDoc LicenseDocumentOrEmpty {
            get {
                try {
                    var doc = LicenseDocument;
                    return doc;
                } catch { }
                return XDoc.Empty;
            }
        }

        private LicenseData LicenseData {
            get {
                if(_licenseData.LicenseState == LicenseStateType.UNDEFINED || ((DateTime.UtcNow - _licenseData.LicenseStateChecked).TotalSeconds > LicenseCheckInterval)) {
                    if(!_inLicenseCheck) {

                        // Re-evaluate license after interval or if it's undefined
                        lock(_sync) {
                            if(!_inLicenseCheck) {
                                try {
                                    _inLicenseCheck = true;
                                    if(_licenseData.LicenseState == LicenseStateType.UNDEFINED || ((DateTime.UtcNow - _licenseData.LicenseStateChecked).TotalSeconds > LicenseCheckInterval)) {
                                        var state = _licenseData.LicenseState;
                                        _log.DebugFormat("verifying license of state {0}, last checked on {1}", _licenseData.LicenseState, _licenseData.LicenseStateChecked);
                                        var licenseData = _licenseController.VerifyLicenseData(_licenseData, _licenseBL, _seatingBL);
                                        if(licenseData.LicenseState != LicenseStateType.UNDEFINED) {
                                            licenseData = licenseData.Checked(DateTime.UtcNow);
                                        }
                                        _licenseData.Update(licenseData);
                                        if(state != _licenseData.LicenseState) {
                                            _settings.ClearConfigCache();
                                        }
                                    }
                                } finally {
                                    _inLicenseCheck = false;
                                }
                            }
                        }
                    } else {
                        _log.Warn("trying to re-enter license check, returning current state instead.");
                    }
                }
                return _licenseData;
            }
        }

        //--- Methods ---
        public XDoc GetLicenseDocument(bool publicLicense) {
            return publicLicense ? LicenseDocument["license.public"] : LicenseDocument;
        }

        public string GetCapability(string name) {
            return _licenseBL.GetCapability(LicenseDocument, name);
        }

        public bool IsUserCreationAllowed(bool throwException) {
            if(!IsLicenseValid()) {
                if(throwException) {
                    throw new MindTouchLicenseNoNewUserForbiddenException(LicenseState);
                }
                return false;
            }

            // Active-users not defined implies unlimited users
            var maxUsers = LicenseData.LicenseDoc["/license.private/grants/active-users"].AsUInt ?? UInt32.MaxValue;
            var currentUsers = _userBL.GetUserCount();
            if(currentUsers >= (maxUsers)) {
                if(throwException) {
                    throw new MindTouchLicenseUserCreationForbiddenException();
                }
                return false;
            }
            return true;
        }

        public ulong LicensePermissionRevokeMask() {
            if(IsLicenseValid()) {
                return ~0UL;
            }
            return ~(ulong)PermissionSets.INVALID_LICENSE_REVOKE_LIST;
        }

        public ulong AnonymousUserMask(UserBE user) {
            if(!_userBL.IsAnonymous(user)) {
                return ~0UL;
            }
            return (ulong)LicenseData.AnonymousPermissions;
        }

        public bool IsSeatLicensingEnabled() {
            return _seatingBL.IsSeatLicensingEnabled(LicenseData);
        }

        public void SetUserSeat(UserBE user) {
            _seatingBL.SetUserSeat(user, LicenseData);
        }

        public virtual void UpdateLicense(XDoc currentLicense, XDoc newLicense) {
            LicenseData currentLicenseData;
            if(currentLicense == null) {
                currentLicenseData = LicenseData.Clone();
            } else {
                var tempLicense = new LicenseData().WithLicenseDocument(currentLicense);
                currentLicenseData = _licenseBL.DetermineLicenseState(tempLicense, false, _seatingBL.IsSeatLicensingEnabled(tempLicense));
            }
            lock(_sync) {
                var updated = _licenseController.UpdateLicense(newLicense, currentLicenseData, _licenseBL, _seatingBL).Checked(DateTime.UtcNow);
                _settings.ClearConfigCache();
                _licenseData.Update(updated);
                _licenseStateTransitionCallback(currentLicenseData, updated);
            }
            _log.DebugFormat("license updated to state {0} and last checked date {1}", _licenseData.LicenseState, _licenseData.LicenseStateChecked);
        }

        public void RemoveSeatFromUser(UserBE user) {
            _seatingBL.RemoveSeatFromUser(user, LicenseData);
        }

        public ulong GetUnseatedUserMask() {
            return _seatingBL.GetUnseatedUserMask(LicenseData.LicenseDoc);
        }

        public ulong? GetSiteOwnerUserId() {
            return _seatingBL.GetSiteOwnerUserId(LicenseData);
        }

        public IEnumerable<UserBE> GetSeatRecommendations(uint? offset, uint? limit, out uint totalCount, out uint queryCount) {
            return _seatingBL.GetSeatRecommendations(LicenseDocument, offset, limit, out totalCount, out queryCount);
        }

        private bool IsLicenseValid() {
            switch(LicenseState) {
            case LicenseStateType.INVALID:
            case LicenseStateType.EXPIRED:
            case LicenseStateType.INACTIVE:
                return false;
            default:
                return true;
            }
        }
    }

    public delegate void LicenseStateTransitionCallback(LicenseData previousState, LicenseData newState);
}
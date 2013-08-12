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
using System.Linq;
using System.Text;
using MindTouch.Deki.Data;
using MindTouch.Deki.Exceptions;
using MindTouch.Deki.Util;
using MindTouch.Deki.WikiManagement;
using MindTouch.Dream;
using MindTouch.Security.Cryptography;
using MindTouch.Xml;

namespace MindTouch.Deki.Logic {
    public enum LicenseStateType : byte {
        UNDEFINED,
        INVALID,
        INACTIVE,
        COMMUNITY,
        TRIAL,
        COMMERCIAL,
        EXPIRED
    };

    public interface ILicenseBL {

        //--- Methods ---
        string GetCapability(XDoc licenseDoc, string name);
        LicenseData DetermineLicenseState(LicenseData license, bool verifyProductKey, bool seatLicensingEnabled);
        LicenseData BuildLicenseData(XDoc license, bool verifyProductKey, bool seatLicensingEnabled);
        LicenseData ValidateNewLicenseTransition(LicenseData newLicense, LicenseData currentLicense);
        int GetSeatsLicensed(XDoc licenseDoc);
        string BuildProductKey();
        void Validate(XDoc license);
        ulong? GetSiteOwnerUserId(XDoc licenseDoc);
    }

    public class LicenseBL : ILicenseBL {

        //--- Constants ---
        private const int GRACE_PERIOD = 14; // days
        public const string CONTENTRATING = "content-rating";
        public const string CONTENTRATING_ENABLED = "enabled";

        //--- Class Fields ---
        private static readonly log4net.ILog _log = DekiLogManager.CreateLog();

        //--- Class Properties ---

        /// <summary>
        /// This accessor only exists for backward compatibility for static BLs. Any instance based recipient should use injection
        /// </summary>
        public static ILicenseBL Instance { get { return DreamContext.Current.Container.Resolve<ILicenseBL>(); } }

        //--- Fields ---
        private readonly string _masterApiKey;
        private readonly string _instanceApiKey;

        //--- Constructors ---
        public LicenseBL(string masterApiKey, string instanceApiKey) {
            _masterApiKey = masterApiKey;
            _instanceApiKey = instanceApiKey;
        }

        //--- Methods ---
        public string GetCapability(XDoc licenseDoc, string name) {
            return DekiLicense.GetCapability(licenseDoc, name);
        }

        public LicenseData DetermineLicenseState(LicenseData licenseData, bool verifyProductKey, bool seatLicensingEnabled) {
            if(licenseData == null) {
                throw new ArgumentNullException("licenseData");
            }
            _log.DebugFormat("verifying license state with initial state: {0}", licenseData.LicenseState);
            return BuildLicenseData(licenseData.LicenseDoc, verifyProductKey, seatLicensingEnabled);
        }

        public LicenseData BuildLicenseData(XDoc license, bool verifyProductKey, bool seatLicensingEnabled) {
            var builtLicense = new LicenseData().WithLicenseDocument(license);

            // check if a valid license was passed in
            if(license.IsEmpty) {
                _log.Debug("license document was empty");
                return builtLicense;
            }

            // check if the deki assembly is signed
            var assembly = typeof(DekiWikiService).Assembly;
            if(ArrayUtil.IsNullOrEmpty(assembly.GetName().GetPublicKey())) {

                // no signature, default to community
                _log.Warn("Unable to validate signature of license since the MindTouch Core service was not signed by MindTouch. Reverting to community edition.");
                return builtLicense.WithState(LicenseStateType.COMMUNITY).WithPermissions(PermissionSets.ALL);
            }

            // assembly is signed: validate xml signature
            var rsa = RSAUtil.ProviderFrom(assembly);
            if((rsa == null) || !license.HasValidSignature(rsa)) {
                _log.Warn("License failed XML validation");
                return builtLicense.WithState(LicenseStateType.INVALID);
            }

            // check license matched product key
            var productKey = license["licensee/product-key"].AsText;

            // license product key may be generated based on either the instance or master apikeys
            if(verifyProductKey && !IsValidProductKey(productKey, _instanceApiKey) && !IsValidProductKey(productKey, _masterApiKey)) {
                _log.Warn("Invalid product-key in license");
                return builtLicense.WithState(LicenseStateType.INVALID);
            }

            // determine license type
            switch(license["@type"].AsText ?? "inactive") {
            case "trial":
                builtLicense = builtLicense.WithState(LicenseStateType.TRIAL);
                break;
            case "inactive":
                builtLicense = builtLicense.WithState(LicenseStateType.INACTIVE);
                break;
            case "community":
                builtLicense = builtLicense.WithState(LicenseStateType.COMMUNITY);
                break;
            case "commercial":
                builtLicense = builtLicense.WithState(LicenseStateType.COMMERCIAL);
                break;
            default:
                _log.Warn("Unknown license type");
                builtLicense = builtLicense.WithState(LicenseStateType.INVALID);
                break;
            }

            // check expiration
            builtLicense = builtLicense.WithExpiration(license["date.expiration"].AsDate ?? DateTime.MaxValue);
            if(builtLicense.LicenseState == LicenseStateType.COMMERCIAL) {

                // check if license is passed grace period
                if(builtLicense.LicenseExpiration <= DateTime.UtcNow.AddDays(-GRACE_PERIOD)) {
                    _log.DebugFormat("commercial license has expired and is past grace period: {0}", builtLicense.LicenseExpiration);
                    return builtLicense.WithState(LicenseStateType.EXPIRED);
                }
                _log.DebugFormat("commercial license has not expired or is at least not past grace period: {0}", builtLicense.LicenseExpiration);
            } else if(builtLicense.LicenseExpiration <= DateTime.UtcNow) {
                _log.DebugFormat("non-commercial license has expired: {0}", builtLicense.LicenseExpiration);
                return builtLicense.WithState(LicenseStateType.EXPIRED);
            }

            // check version
            var licenseVersion = (license["version"].AsText ?? "*").Split('.');
            var assemblyVersion = typeof(LicenseBL).Assembly.GetName().Version;
            var appVersion = new[] { assemblyVersion.Major, assemblyVersion.Minor, assemblyVersion.Revision, assemblyVersion.Build };
            for(var i = 0; (i < licenseVersion.Length) && (i < appVersion.Length); ++i) {
                var pattern = licenseVersion[i];
                int value;
                if(pattern.Equals("*") || (int.TryParse(pattern, out value) && (value >= appVersion[i]))) {
                    continue;
                }
                return builtLicense.WithState(LicenseStateType.EXPIRED);
            }

            // determine permissions for anonymous user
            builtLicense = builtLicense.WithPermissions(PermissionsBL.MaskFromString(DekiLicense.GetCapability(license, "anonymous-permissions")) | PermissionSets.MINIMAL_ANONYMOUS_PERMISSIONS);

            // retrieve the site owner from the license
            var siteOwnerUserId = GetSiteOwnerUserId(license);
            if(seatLicensingEnabled && (siteOwnerUserId ?? 0) == 0) {
                throw new MindTouchLicenseNoSiteOwnerDefinedException();
            }
            return builtLicense.WithSiteOwnerUserId(siteOwnerUserId);

        }

        public LicenseData ValidateNewLicenseTransition(LicenseData newLicense, LicenseData currentLicense) {

            var currentState = currentLicense.LicenseState;

            /*
            * State transitions:
            *   community   -> community
            *   community   -> commercial
            *   trial       -> commercial
            *   trial       -> trial
            *   commercial  -> commercial
            *   expired     -> trial
            *   expired     -> commercial
            *   inactive    -> trial
            *   inactive    -> commercial
            *   inactive    -> community
            *   invalid     -> *
            */

            // retrieve desired license state

            var exception = true;

            // check if the license transition is valid
            if(newLicense.LicenseState == LicenseStateType.INVALID) {

                // cannot switch to an invalid license             
                throw new MindTouchLicenseUpdateInvalidArgumentException();
            }
            if(newLicense.LicenseState == LicenseStateType.EXPIRED) {

                // cannot switch to an expired license
                throw new MindTouchLicenseExpiredInvalidOperationException(newLicense.LicenseExpiration);
            }
            switch(currentState) {
            case LicenseStateType.COMMUNITY:
                switch(newLicense.LicenseState) {
                case LicenseStateType.COMMUNITY:
                case LicenseStateType.COMMERCIAL:
                    exception = false;
                    break;
                }
                break;
            case LicenseStateType.INACTIVE:
                switch(newLicense.LicenseState) {
                case LicenseStateType.COMMUNITY:
                case LicenseStateType.COMMERCIAL:
                case LicenseStateType.TRIAL:
                    exception = false;
                    break;
                }
                break;
            case LicenseStateType.TRIAL:
                switch(newLicense.LicenseState) {
                case LicenseStateType.COMMERCIAL:
                case LicenseStateType.TRIAL:
                    exception = false;
                    break;
                }
                break;
            case LicenseStateType.COMMERCIAL:
                switch(newLicense.LicenseState) {
                case LicenseStateType.COMMERCIAL:
                    exception = false;
                    break;
                }
                break;
            case LicenseStateType.EXPIRED:
                switch(newLicense.LicenseState) {
                case LicenseStateType.TRIAL:
                case LicenseStateType.COMMERCIAL:
                    exception = false;
                    break;
                }
                break;
            case LicenseStateType.INVALID:
                exception = false;
                break;
            default:
                throw new MindTouchLicenseUpdateInvalidArgumentException();
            }

            // verify that this license of for this installations
            if(exception) {
                throw new MindTouchLicenseTransitionForbiddenLicenseTransitionException(currentState, newLicense.LicenseState);
            }

            // Extra validation when updating or transitioning to commerical license
            if(newLicense.LicenseState == LicenseStateType.COMMERCIAL) {

                //user count
                var maxUsers = newLicense.LicenseDoc["/license.private/grants/active-users"].AsUInt;
                if(maxUsers != null) {

                    //Reject license if its user limit is lower than current number of users
                    var currentActiveUsers = DbUtils.CurrentSession.Users_GetCount();
                    if(currentActiveUsers > maxUsers.Value) {
                        var userDelta = currentActiveUsers - maxUsers.Value;
                        throw new MindTouchLicenseTooManyUsersForbiddenException(currentActiveUsers, maxUsers.Value, userDelta);
                    }
                }
            }

            return newLicense;
        }

        public int GetSeatsLicensed(XDoc licenseDoc) {
            int maxSeats;
            if(!int.TryParse(GetCapability(licenseDoc, "active-seats"), out maxSeats)) {
                return int.MaxValue;
            }
            return maxSeats;
        }

        public string BuildProductKey() {
            var apiKey = _instanceApiKey.IfNullOrEmpty(_masterApiKey);
            return StringUtil.ComputeHashString(apiKey, Encoding.UTF8).ToUpperInvariant();
        }

        public void Validate(XDoc license) {
            DekiLicense.Validate(license);
        }

        public ulong? GetSiteOwnerUserId(XDoc licenseDoc) {
            return licenseDoc["licensee/id.owner"].AsULong;
        }

        private static bool IsValidProductKey(string productKey, string apikey) {
            if(productKey == null) {
                return true;
            }
            if(string.IsNullOrEmpty(apikey) || string.IsNullOrEmpty(productKey)) {
                return false;
            }
            string computedProductKey = StringUtil.ComputeHashString(apikey, Encoding.UTF8);
            return productKey.EqualsInvariantIgnoreCase(computedProductKey);
        }
    }
}

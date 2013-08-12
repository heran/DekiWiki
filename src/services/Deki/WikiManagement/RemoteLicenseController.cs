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
using System.Linq;
using log4net;
using MindTouch.Deki.Exceptions;
using MindTouch.Deki.Logic;
using MindTouch.Dream;
using MindTouch.Xml;

namespace MindTouch.Deki.WikiManagement {

    public class RemoteLicenseController : LicenseController {

        //--- Fields ---
        private readonly Func<string, DreamMessage> _getRemoteLicense;
        private readonly ILog _log;

        //--- Constructors ---
        public RemoteLicenseController(string wikiId, Plug licenseStoragePlug, Func<string, DreamMessage> getRemoteLicense, ILog log)
            : base(wikiId, licenseStoragePlug, log) {
            _log = log;
            _getRemoteLicense = getRemoteLicense;
        }

        //--- Properties ---
        public override XDoc LicenseDoc {
            get {
                var remoteLicenseMsg = _getRemoteLicense(_wikiId);
                if(!remoteLicenseMsg.IsSuccessful) {
                    throw new ShouldNeverHappenException("portal response is invalid");
                }
                try {
                    var license = remoteLicenseMsg.ToDocument() ?? XDoc.Empty;
                    if(license.IsEmpty) {
                        throw new ShouldNeverHappenException("unable to read portal response as document");
                    }
                    return license;
                } catch(ShouldNeverHappenException) {
                    throw;
                } catch {
                    throw new MindTouchRemoteLicenseInvalidException();
                }
            }
        }

        //--- Methods ---
        public override LicenseData UpdateLicense(XDoc license, LicenseData currentLicense, ILicenseBL licenseBL, ISeatingBL seatingBL) {
            _log.DebugFormat("updating license from '{1}' to '{0}'", license["@type"].Contents, currentLicense.LicenseState);
            licenseBL.Validate(license);
            return HandleLicenseTransition(license, currentLicense, seatingBL, licenseBL);
        }

        public override LicenseData VerifyLicenseData(LicenseData licenseData, ILicenseBL licenseBL, ISeatingBL seatingBL) {
            _log.Debug("verifying license: " + licenseData.LicenseState);

            // load authoritative license from remote
            var license = LicenseDoc;
            var verified = licenseBL.DetermineLicenseState(new LicenseData().WithLicenseDocument(license), true, seatingBL.IsSeatLicensingEnabled(licenseData));
            if(!(new[] { LicenseStateType.COMMERCIAL, LicenseStateType.TRIAL, LicenseStateType.INACTIVE, LicenseStateType.EXPIRED }).Contains(verified.LicenseState)) {
                _log.DebugFormat("license state '{0}' is not allowed for remotely managed instances", verified.LicenseState);
                throw new MindTouchRemoteLicenseFailedException();
            }
            _log.Debug("verified license: " + verified.LicenseState);
            return verified;
        }

        protected override void HandleExcessSeats(LicenseData license, ISeatingBL seatingBL, SeatAssignmentInfo seats) {
            _log.WarnFormat("site '{0}' has {1} assigned seats, but is only licensed for {2} (not revoking)", _wikiId, seats.Assigned, seats.Allowed);
        }
    }
}
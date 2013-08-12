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
using log4net;
using MindTouch.Deki.Logic;
using MindTouch.Dream;
using MindTouch.Xml;

namespace MindTouch.Deki.WikiManagement {

    public class LicenseController : ILicenseController {

        //--- Constants ---
        protected const string LICENSE_FILE = "license.xml";

        //--- Fields ---
        protected readonly Plug _licenseStoragePlug;
        private readonly ILog _log;
        protected readonly string _wikiId;

        //--- Constructors ---
        public LicenseController(string wikiId, Plug licenseStoragePlug, ILog log) {
            _log = log;
            _licenseStoragePlug = licenseStoragePlug;
            _wikiId = wikiId;
        }

        //--- Properties ---
        public virtual XDoc LicenseDoc {
            get {
                var license = XDoc.Empty;

                // load instance license 
                var msg = _licenseStoragePlug.At(_wikiId, LICENSE_FILE).GetAsync().Wait();
                if(msg.IsSuccessful) {
                    try {
                        license = msg.ToDocument();
                    } catch(Exception x) {
                        _log.WarnExceptionFormat(x, "The commercial license for the instance could not be loaded");
                        license = XDoc.Empty;
                    }
                }

                // load shared license if instance license wasn't found
                if(license.IsEmpty) {
                    msg = _licenseStoragePlug.At(LICENSE_FILE).GetAsync().Wait();
                    if(msg.IsSuccessful) {
                        try {
                            license = msg.ToDocument();
                        } catch(Exception x) {
                            _log.WarnExceptionFormat(x, "The shared commercial license could not be loaded");
                            license = XDoc.Empty;
                        }
                    }
                }

                // check if a license was found
                if(license.IsEmpty) {
                    msg = Plug.New("resource://mindtouch.deki/MindTouch.Deki.Resources.license-community.xml").With(DreamOutParam.TYPE, MimeType.XML.ToString()).GetAsync().Wait();
                    if(msg.IsSuccessful) {
                        try {
                            license = msg.ToDocument();
                        } catch(Exception x) {
                            _log.WarnExceptionFormat(x, "The community license could not be loaded");
                            license = XDoc.Empty;
                        }

                    } else {

                        // unable to retrieve the license
                        _log.Warn("unable to retrieve the built in community license");
                    }
                }
                return license;
            }
        }

        //--- Methods ---
        public virtual LicenseData UpdateLicense(XDoc license, LicenseData currentLicense, ILicenseBL licenseBL, ISeatingBL seatingBL) {
            licenseBL.Validate(license);

            // Only site owner of a license can upload a license that has seat licensing enabled.
            seatingBL.ValidateLicenseUpdateUser(license);
            var newLicense = HandleLicenseTransition(license, currentLicense, seatingBL, licenseBL);
            _licenseStoragePlug.At(_wikiId, LICENSE_FILE).Put(license);
            return newLicense;
        }

        public virtual LicenseData VerifyLicenseData(LicenseData licenseData, ILicenseBL licenseBL, ISeatingBL seatingBL) {
            if(licenseData.LicenseDoc.IsEmpty) {
                licenseData = licenseData.WithLicenseDocument(LicenseDoc);
            }
            return licenseBL.DetermineLicenseState(licenseData, true, seatingBL.IsSeatLicensingEnabled(licenseData));
        }

        protected LicenseData HandleLicenseTransition(XDoc licenseDoc, LicenseData currentLicense, ISeatingBL seatingBL, ILicenseBL licenseBL) {
            var seatingEnabledInNewLicense = seatingBL.IsSeatLicensingEnabled(licenseDoc);
            var tempLicense = licenseBL.BuildLicenseData(licenseDoc, true, seatingEnabledInNewLicense);
            var newLicense = licenseBL.ValidateNewLicenseTransition(tempLicense, currentLicense);
            _log.DebugFormat("new license with state '{0}' has passed validation and will be accepted", newLicense.LicenseState);

            // Ensure that all seats are cleared if seat licensing is disabled. 
            // This will allow a known clean seat state if it becomes enabled.
            if(!seatingBL.IsSeatLicensingEnabled(currentLicense)) {

                _log.Debug("old license did not have seat licensing");
                seatingBL.RevokeSeats(currentLicense);
            }

            // Seat licensing
            if(seatingEnabledInNewLicense) {
                var seats = seatingBL.HandleSeatTransition(licenseDoc);
                if(seats.Assigned > seats.Allowed) {
                    HandleExcessSeats(currentLicense, seatingBL, seats);
                }

                // set a seat for the owner
                seatingBL.SetOwnerUserSeat(newLicense);
            } else {
                _log.Debug("new license does not contain seat licensing");

                // Clear the seat state when going to non-seat license
                seatingBL.RevokeSeats(newLicense);
            }
            _log.DebugFormat("transitioned license from '{0}' to '{1}'", currentLicense.LicenseState, newLicense.LicenseState);
            return newLicense;
        }

        protected virtual void HandleExcessSeats(LicenseData license, ISeatingBL seatingBL, SeatAssignmentInfo seats) {
            _log.WarnFormat("site has {0} assigned seats, but is only licensed for {1} (revoking seats)", seats.Assigned, seats.Allowed);
            seatingBL.RevokeSeats(license);
        }
    }
}
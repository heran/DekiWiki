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
using log4net;
using MindTouch.Deki.Data;
using MindTouch.Deki.Exceptions;
using MindTouch.Deki.Logic;
using MindTouch.Xml;

namespace MindTouch.Deki.WikiManagement {

    public class SeatingBL : ISeatingBL {

        //--- Constants ---
        private const ushort POINTS_FOR_GROUP_MEMBERSHIP = 1; // points for each group membership that would lead to revoked operations
        private const ushort POINTS_FOR_USER_ROLE = 2;        // points for having direct revoked operations
        private const ushort POINTS_FOR_RECOMMENDATION = 1;   // number of points for a user to get on the recommended list

        //--- Fields ---
        private readonly IUserBL _userBL;
        private readonly ILicenseBL _licenseBL;
        private readonly ILog _log;
        private readonly ICurrentUserContext _userContext;
        private readonly IDekiDataSession _dataSession;

        //--- Constructors ---
        public SeatingBL(IUserBL userBL, ILicenseBL licenseBL, ICurrentUserContext userContext, IDekiDataSession dataSession, ILog log) {
            _userBL = userBL;
            _licenseBL = licenseBL;
            _log = log;
            _userContext = userContext;
            _dataSession = dataSession;
        }

        //--- Methods ---
        public bool IsSeatLicensingEnabled(XDoc licenseDoc) {
            return GetUnseatedUserMask(licenseDoc) != ulong.MaxValue;
        }

        public bool IsSeatLicensingEnabled(LicenseData license) {
            return IsSeatLicensingEnabled(license.LicenseDoc);
        }

        // Note (arnec): GetSeatRecommendations cannot be unit tested because it calls into PermissionsBL.GetRoles(), which calls into
        // DekiContext
        public IEnumerable<UserBE> GetSeatRecommendations(XDoc license, uint? offset, uint? limit, out uint totalCount, out uint queryCount) {
            if(!IsSeatLicensingEnabled(license)) {
                throw new MindTouchLicenseSeatLicensingNotInUseException();
            }
            var pointsByUserId = new Dictionary<ulong, ushort>();

            // return all enabled users
            var usersById = _dataSession.Users_GetBySeat(false).ToDictionary(u => u.ID, true);

            // return all groups (and their member users)
            var groups = _dataSession.Groups_GetByQuery(null, null, SortDirection.UNDEFINED, GroupsSortField.UNDEFINED, null, null, out totalCount, out queryCount);

            ulong unseatedMask = GetUnseatedUserMask(license);

            // get all roles and filter by those that have revoked operations
            var roles = from r in PermissionsBL.GetRoles()
                        where (r.PermissionFlags & unseatedMask) != r.PermissionFlags
                        select r.ID;

            // add up points for membership to group that has a selected role 
            foreach(var group in groups) {
                if(ArrayUtil.IsNullOrEmpty(group.UserIdsList)) {
                    continue;
                }
                if(roles.Contains(group.RoleId)) {

                    // group has one of the preselected roles.
                    foreach(uint userId in group.UserIdsList) {
                        if(pointsByUserId.ContainsKey(userId)) {
                            pointsByUserId[userId] += POINTS_FOR_GROUP_MEMBERSHIP;
                        } else {
                            pointsByUserId[userId] = POINTS_FOR_GROUP_MEMBERSHIP;
                        }
                    }
                }
            }

            // points from having a revoked role directly
            foreach(UserBE user in usersById.Values) {
                if(roles.Contains(user.RoleId)) {
                    if(pointsByUserId.ContainsKey(user.ID)) {
                        pointsByUserId[user.ID] += POINTS_FOR_USER_ROLE;
                    } else {
                        pointsByUserId[user.ID] = POINTS_FOR_USER_ROLE;

                    }
                }
            }

            // filter out users without points; sort by points; map back to user objects
            var ret = (from up in pointsByUserId
                       where usersById.ContainsKey((uint)up.Key) && up.Value >= POINTS_FOR_RECOMMENDATION
                       orderby up.Value descending, usersById[(uint)up.Key].Name
                       select usersById[(uint)up.Key])
                .ToArray();

            queryCount = totalCount = (uint)ret.Count();

            // apply offset and limit and recalculate querycount
            if(offset != null || limit != null) {
                ret = ret.Skip((int)(offset ?? 0)).Take((int)(limit ?? Int32.MaxValue)).ToArray();
                queryCount = (uint)ret.Count();
            }
            return ret;
        }

        public ulong GetUnseatedUserMask(XDoc licenseDoc) {
            var unseatedPermsStr = _licenseBL.GetCapability(licenseDoc, "unseated-permissions");
            if(string.IsNullOrEmpty(unseatedPermsStr)) {
                return ulong.MaxValue;
            }
            return (ulong)(PermissionsBL.MaskFromString(unseatedPermsStr) | PermissionSets.MINIMAL_ANONYMOUS_PERMISSIONS);
        }

        public void SetUserSeat(UserBE user, LicenseData license) {
            if(!IsSeatLicensingEnabled(license)) {
                throw new MindTouchLicenseSeatLicensingNotInUseException();
            }
            if(_userBL.IsAnonymous(user)) {
                throw new MindTouchLicenseAnonymousSeat();
            }
            if(user.LicenseSeat) {

                // giving a seat to a seated user is not an error
                _log.DebugFormat("user {0} already has a seat license", user.ID);
                return;
            }
            if(AreSeatsExceedingLicense(license)) {
                throw new MindTouchLicenseInsufficientSeatsException(GetSeatsLicensed(license));
            }
            _log.DebugFormat("settings licenseSeat to true for user {0}", user.ID);
            user.LicenseSeat = true;
            _userBL.UpdateUser(user);

            // ensure that another server or thread hasn't concurrently granted a seat thus going over the limit.
            // Both seats may potentially be revoked.
            if(AreSeatsExceedingLicense(license)) {
                RemoveSeatFromUserInternal(user, license);
                throw new MindTouchLicenseInsufficientSeatsException(GetSeatsLicensed(license));
            }
            _log.InfoFormat("Seat licensing: Granted a seat to '{0}'", user.Name);
        }

        public ulong? GetSiteOwnerUserId(LicenseData license) {
            if(IsSeatLicensingEnabled(license)) {
                if(license.SiteOwnerUserId == null) {
                    _log.Warn("Seat licensing is enabled but no site owner is defined.");
                }
                return license.SiteOwnerUserId;
            }
            return null;
        }

        public void RemoveSeatFromUser(UserBE user, LicenseData licenseData) {
            if(!IsSeatLicensingEnabled(licenseData)) {
                throw new MindTouchLicenseSeatLicensingNotInUseException();
            }
            RemoveSeatFromUserInternal(user, licenseData);
        }

        public void RevokeSeats(LicenseData license) {
            var ownerUserId = GetSiteOwnerUserId(license) ?? 0;
            var seatedUsers = GetSeatedUsers().Where(u => u.ID != ownerUserId).ToArray();
            foreach(var u in seatedUsers) {
                RemoveSeatFromUserInternal(u, license);
            }
            if(seatedUsers.Any()) {
                _log.Warn(String.Format("Seat licensing: Revoked {0} seats from users", seatedUsers.Count()));
            }
        }

        public void SetOwnerUserSeat(LicenseData checkedLicense) {

            // Give a seat to the site owner if they don't already have one (the owner id has already been validated)
            var ownerUserId = GetSiteOwnerUserId(checkedLicense) ?? 0;
            if(ownerUserId == 0) {
                _log.Warn("Unable to assign a seat to the owner since it was not defined in the license");
                return;
            }
            var siteOwner = _userBL.GetUserById((uint)ownerUserId);
            if(siteOwner == null) {
                _log.Warn(String.Format("Unable to look up site owner user id '{0}'", ownerUserId));
                return;
            }
            SetUserSeat(siteOwner, checkedLicense);

            // reset current user to reflect seat status of owner
            if(_userContext.User != null && _userContext.User.ID == siteOwner.ID) {
                _userContext.User = siteOwner;
            }
        }

        public void ValidateLicenseUpdateUser(XDoc license) {
            if(!IsSeatLicensingEnabled(license)) {
                return;
            }
            var siteOwnerUserId = _licenseBL.GetSiteOwnerUserId(license);
            if(siteOwnerUserId == null) {
                throw new MindTouchLicenseNoSiteOwnerDefinedException();
            }
            if(_userContext.User == null) {
                throw new ShouldNeverHappenException("Tried to update a license without having a user defined in request context");
            }
            if(siteOwnerUserId.Value == _userContext.User.ID) {
                return;
            }
            var requiredCurrentUser = _userBL.GetUserById((uint)siteOwnerUserId.Value);
            var username = requiredCurrentUser != null ? requiredCurrentUser.Name : "(unavailable)";
            throw new MindTouchLicenseUploadByNonOwnerException(username, siteOwnerUserId.Value);
        }

        public SeatAssignmentInfo HandleSeatTransition(XDoc license) {
            _log.Debug("new license contains seat licensing");

            // (seat licensing -> seat licensing)
            // If more seats are assigned than allowed by the new license: revoke all seats.

            // determine site owner
            var siteOwnerUserId = _licenseBL.GetSiteOwnerUserId(license);
            if(siteOwnerUserId == null) {
                throw new MindTouchLicenseNoSiteOwnerDefinedException();
            }

            // License update is refused if seats were downgraded to less than the number of current assigned seats
            var seatsAllowed = _licenseBL.GetSeatsLicensed(license);
            var seatsAssigned = GetSeatedUsers().Count();

            // optimized way of getting the owner because only the owner can upload a license
            var owner = (_userContext.User != null && siteOwnerUserId.Value == _userContext.User.ID) ? _userContext.User : _userBL.GetUserById((uint)siteOwnerUserId.Value);
            if(owner != null && !owner.LicenseSeat) {

                // A seat for the site owner needs to be available
                seatsAssigned++;
            }
            return new SeatAssignmentInfo(seatsAssigned, seatsAllowed);
        }

        private IEnumerable<UserBE> GetSeatedUsers() {
            return _dataSession.Users_GetBySeat(true);
        }

        private bool AreSeatsExceedingLicense(LicenseData license) {

            // enforce seat licensing limits
            var seatsAssigned = GetSeatedUsers().Count();
            var seatsAllowed = GetSeatsLicensed(license);

            // ensure a seat is available
            if(seatsAllowed < seatsAssigned) {
                _log.Warn(String.Format("Seat licensing: Number of seats used is exceeding allowed. Seats allowed: {0}; Seats assigned:{1}", seatsAllowed, seatsAssigned));
                return true;
            }
            return false;
        }

        private void RemoveSeatFromUserInternal(UserBE user, LicenseData license) {
            if(!user.LicenseSeat) {
                return;
            }

            // Site owner may not have their license removed
            var siteOwnerUserId = GetSiteOwnerUserId(license) ?? 0;
            if(user.ID == siteOwnerUserId) {
                throw new MindTouchLicenseRemovalFromSiteOwnerException();
            }
            user.LicenseSeat = false;
            _userBL.UpdateUser(user);
            _log.InfoFormat("Seat licensing: Removed a seat from '{0}'", user.Name);
        }

        private int GetSeatsLicensed(LicenseData license) {
            return _licenseBL.GetSeatsLicensed(license.LicenseDoc);
        }
    }
}
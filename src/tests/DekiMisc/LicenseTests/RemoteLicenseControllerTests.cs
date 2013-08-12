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
using MindTouch.Deki.Data;
using MindTouch.Deki.Exceptions;
using MindTouch.Deki.Logic;
using MindTouch.Deki.WikiManagement;
using MindTouch.Dream;
using MindTouch.Dream.Test;
using MindTouch.Xml;
using Moq;
using NUnit.Framework;
using MindTouch.Deki.Util;

namespace MindTouch.Deki.Tests.LicenseTests {

    [TestFixture]
    public class RemoteLicenseControllerTests {

        //--- Class Methods ---
        private static bool InactiveLicense(XDoc license) {
            return license["@type"].AsText.EqualsInvariant("inactive");
        }

        //--- Fields ---
        private Plug _storagePlug = Plug.New("mock://storage/");
        private Mock<ILicenseBL> _licenseBLMock;
        private Mock<ISeatingBL> _seatingBLMock;
        private MockUserContext _userContext;
        private Mock<IInstanceSettings> _settingsMock;
        private Mock<IUserBL> _userMock;
        private RemoteLicenseController _licenseController;
        private XDoc _inactiveLicense;
        private string _wikiId;
        private string _getRemoteLicense_WikiId;
        private XDoc _getRemoteLicense_License;
        private int _getRemoteLicense_OkCounter;
        private int _getRemoteLicense_ErrorCounter;

        //--- Methods ---
        [SetUp]
        public void Setup() {
            _inactiveLicense = Plug.New("resource://mindtouch.deki/MindTouch.Deki.Resources.license-inactive.xml").With(DreamOutParam.TYPE, MimeType.XML.ToString()).Get().ToDocument();

            MockPlug.DeregisterAll();
            _userMock = new Mock<IUserBL>();
            _settingsMock = new Mock<IInstanceSettings>();
            _licenseBLMock = new Mock<ILicenseBL>();
            _seatingBLMock = new Mock<ISeatingBL>();
            _userContext = new MockUserContext();
            _wikiId = "bob";
            _licenseController = new RemoteLicenseController(_wikiId, _storagePlug, GetRemoteLicense, LogUtils.CreateLog<RemoteLicenseController>());
        }

        [Test]
        public void Can_update_license_seat_licensing() {

            // Arrange
            var currentLicense = new XDoc("current");
            var newLicense = new XDoc("new");
            var newExpiration = DateTime.UtcNow.AddDays(1);
            var newPermissions = Permissions.BROWSE;
            ulong? newOwnerId = 1;
            var currentLicenseData = new LicenseData()
                .WithLicenseDocument(currentLicense)
                .WithState(LicenseStateType.COMMERCIAL)
                .WithExpiration(DateTime.UtcNow)
                .WithPermissions(Permissions.NONE)
                .WithSiteOwnerUserId(1)
                .Checked(DateTime.UtcNow);
            var newLicenseData = new LicenseData().WithLicenseDocument(newLicense)
                .WithState(LicenseStateType.COMMERCIAL)
                .WithExpiration(newExpiration)
                .WithPermissions(newPermissions)
                .WithSiteOwnerUserId(newOwnerId)
                .Checked(DateTime.UtcNow);
            _licenseBLMock.Setup(x => x.GetSiteOwnerUserId(newLicense)).Returns(newOwnerId);
            var tempLicenseNewLicenseData = new LicenseData().WithLicenseDocument(newLicense);
            _licenseBLMock.Setup(x => x.BuildLicenseData(newLicense, true, true)).Returns(tempLicenseNewLicenseData).Verifiable();
            _licenseBLMock.Setup(x => x.ValidateNewLicenseTransition(It.Is<LicenseData>(l => l.AreSame(tempLicenseNewLicenseData)), It.Is<LicenseData>(l => l.AreSame(currentLicenseData))))
                .Returns(newLicenseData)
                .Verifiable();
            _seatingBLMock.Setup(x => x.IsSeatLicensingEnabled(currentLicenseData)).Returns(true);
            _seatingBLMock.Setup(x => x.IsSeatLicensingEnabled(newLicense)).Returns(true);
            _seatingBLMock.Setup(x => x.HandleSeatTransition(newLicense)).Returns(new SeatAssignmentInfo(2, 5));

            // Act
            var updatedLicense = _licenseController.UpdateLicense(newLicense, currentLicenseData, _licenseBLMock.Object, _seatingBLMock.Object);

            // Assert
            _licenseBLMock.Verify(x => x.Validate(newLicense), Times.Once());
            _licenseBLMock.Verify(x => x.ValidateNewLicenseTransition(It.Is<LicenseData>(l => l.AreSame(tempLicenseNewLicenseData)), It.Is<LicenseData>(l => l.AreSame(currentLicenseData))), Times.Once());
            _seatingBLMock.Verify(x => x.IsSeatLicensingEnabled(currentLicenseData), Times.Once());
            _seatingBLMock.Verify(x => x.IsSeatLicensingEnabled(newLicense), Times.Once());
            _seatingBLMock.Verify(x => x.HandleSeatTransition(newLicense), Times.Once());
            _seatingBLMock.Verify(x => x.SetOwnerUserSeat(newLicenseData), Times.Once());
            Assert.AreSame(newLicenseData, updatedLicense);
            Assert.AreEqual(newLicense, newLicenseData.LicenseDoc);
            Assert.AreEqual(newExpiration, newLicenseData.LicenseExpiration);
            Assert.AreEqual(newOwnerId, newLicenseData.SiteOwnerUserId);
            Assert.AreEqual(newPermissions, newLicenseData.AnonymousPermissions);
        }

        [Test]
        public void Can_update_license_without_seat_licensing() {

            // Arrange
            var currentLicense = new XDoc("current");
            var newLicense = new XDoc("new");
            var newExpiration = DateTime.UtcNow.AddDays(1);
            var newPermissions = Permissions.BROWSE;
            ulong? newOwnerId = 1;
            var currentLicenseData = new LicenseData()
                .WithLicenseDocument(currentLicense)
                .WithState(LicenseStateType.COMMERCIAL)
                .WithExpiration(DateTime.UtcNow)
                .WithPermissions(Permissions.NONE)
                .WithSiteOwnerUserId(1)
                .Checked(DateTime.UtcNow);
            var newLicenseData = new LicenseData().WithLicenseDocument(newLicense)
                .WithState(LicenseStateType.COMMERCIAL)
                .WithExpiration(newExpiration)
                .WithPermissions(newPermissions)
                .WithSiteOwnerUserId(newOwnerId)
                .Checked(DateTime.UtcNow);
            var tempLicenseNewLicenseData = new LicenseData().WithLicenseDocument(newLicense);
            _licenseBLMock.Setup(x => x.BuildLicenseData(newLicense, true, false)).Returns(tempLicenseNewLicenseData);
            _licenseBLMock.Setup(x => x.ValidateNewLicenseTransition(It.Is<LicenseData>(l => l.AreSame(tempLicenseNewLicenseData)), It.Is<LicenseData>(l => l.AreSame(currentLicenseData))))
                .Returns(newLicenseData);
            _seatingBLMock.Setup(x => x.IsSeatLicensingEnabled(currentLicenseData)).Returns(false);
            _seatingBLMock.Setup(x => x.IsSeatLicensingEnabled(newLicense)).Returns(false);

            // Act
            var updatedLicense = _licenseController.UpdateLicense(newLicense, currentLicenseData, _licenseBLMock.Object, _seatingBLMock.Object);

            // Assert
            _licenseBLMock.Verify(x => x.Validate(newLicense), Times.Once());
            _licenseBLMock.Verify(x => x.ValidateNewLicenseTransition(It.Is<LicenseData>(l => l.AreSame(tempLicenseNewLicenseData)), It.Is<LicenseData>(l => l.AreSame(currentLicenseData))), Times.Once());
            _seatingBLMock.Verify(x => x.IsSeatLicensingEnabled(currentLicenseData), Times.AtLeastOnce());
            _seatingBLMock.Verify(x => x.IsSeatLicensingEnabled(newLicense), Times.AtLeastOnce());
            _seatingBLMock.Verify(x => x.RevokeSeats(currentLicenseData), Times.Once());
            _seatingBLMock.Verify(x => x.RevokeSeats(newLicenseData), Times.Once());
            Assert.AreSame(newLicenseData, updatedLicense);
            Assert.AreEqual(newLicense, updatedLicense.LicenseDoc);
            Assert.AreEqual(newExpiration, updatedLicense.LicenseExpiration);
            Assert.AreEqual(newOwnerId, updatedLicense.SiteOwnerUserId);
            Assert.AreEqual(newPermissions, updatedLicense.AnonymousPermissions);
        }

        [Test]
        public void License_check_always_fetches_from_remote() {

            // Arrange
            var license = new XDoc("license");
            var newLicenseState = LicenseStateType.COMMERCIAL;
            var newExpiration = DateTime.UtcNow.AddDays(1);
            var newPermissions = Permissions.BROWSE;
            ulong? newSiteOwnerId = 1;
            var checkedDate = DateTime.UtcNow;
            SetupGetRemoteLicense(_wikiId, license);
            var licenseData = new LicenseData();
            var licenseDataWithLicense = new LicenseData().WithLicenseDocument(license);
            _licenseBLMock.Setup(x => x.DetermineLicenseState(It.Is<LicenseData>(l => l.AreSame(licenseDataWithLicense)), true, false))
                .Returns(new LicenseData().WithLicenseDocument(license)
                             .WithState(newLicenseState)
                             .WithExpiration(newExpiration)
                             .WithPermissions(newPermissions)
                             .WithSiteOwnerUserId(newSiteOwnerId)
                             .Checked(checkedDate)
                );
            _seatingBLMock.Setup(x => x.IsSeatLicensingEnabled(license)).Returns(false);

            // Act
            var checkedLicense = _licenseController.VerifyLicenseData(licenseData, _licenseBLMock.Object, _seatingBLMock.Object);

            // Assert
            VerifyGetRemoteLicense();
            _licenseBLMock.Verify(x => x.DetermineLicenseState(It.Is<LicenseData>(l => l.AreSame(licenseDataWithLicense)), true, false), Times.Once());
            Assert.AreEqual(license, checkedLicense.LicenseDoc);
            Assert.AreEqual(newLicenseState, checkedLicense.LicenseState);
            Assert.AreEqual(newExpiration, checkedLicense.LicenseExpiration);
            Assert.AreEqual(newSiteOwnerId, checkedLicense.SiteOwnerUserId);
            Assert.AreEqual(newPermissions, checkedLicense.AnonymousPermissions);
            Assert.AreEqual(checkedDate, checkedLicense.LicenseStateChecked);
        }

        private void SetupGetRemoteLicense(string wikiId, XDoc license) {
            _getRemoteLicense_ErrorCounter = 0;
            _getRemoteLicense_OkCounter = 0;
            _getRemoteLicense_WikiId = wikiId;
            _getRemoteLicense_License = license;
        }

        private DreamMessage GetRemoteLicense(string wikiId) {
            if((_getRemoteLicense_WikiId == null) || _getRemoteLicense_WikiId.EqualsInvariantIgnoreCase(wikiId)) {
                ++_getRemoteLicense_OkCounter;
                return DreamMessage.Ok(_getRemoteLicense_License);
            }
            ++_getRemoteLicense_ErrorCounter;
            return DreamMessage.NotFound("error");
        }

        private void VerifyGetRemoteLicense() {
            Assert.GreaterOrEqual(1, _getRemoteLicense_OkCounter);
            Assert.AreEqual(0, _getRemoteLicense_ErrorCounter);
            _getRemoteLicense_ErrorCounter = 0;
            _getRemoteLicense_OkCounter = 0;
            _getRemoteLicense_WikiId = null;
            _getRemoteLicense_License = null;
        }
    }
}

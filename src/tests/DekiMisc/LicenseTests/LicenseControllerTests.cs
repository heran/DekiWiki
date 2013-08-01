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
using MindTouch.Deki.Util;
using MindTouch.Deki.WikiManagement;
using MindTouch.Dream;
using MindTouch.Dream.Test;
using MindTouch.Xml;
using Moq;
using NUnit.Framework;

namespace MindTouch.Deki.Tests.LicenseTests {
    [TestFixture]
    public class LicenseControllerTests {
        private Plug _storagePlug = Plug.New("mock://storage/");
        private Mock<ILicenseBL> _licenseBLMock;
        private Mock<ISeatingBL> _seatingBLMock;
        private LicenseController _licenseController;
        private string _wikiId;

        [SetUp]
        public void Setup() {
            MockPlug.DeregisterAll();
            _licenseBLMock = new Mock<ILicenseBL>();
            _seatingBLMock = new Mock<ISeatingBL>();
            _wikiId = "bob";
            _licenseController = new LicenseController(_wikiId, _storagePlug, LogUtils.CreateLog<LicenseController>());
        }

        [Test]
        public void SeatingBL_candidate_check_makes_UpdateLicense_throw() {

            // Arrange
            var newLicense = new XDoc("new");
            var licenseData = new LicenseData();
            var testException = new Utils.TestException();
            _seatingBLMock.Setup(x => x.ValidateLicenseUpdateUser(newLicense)).Throws(testException);

            // Act
            try {
                _licenseController.UpdateLicense(newLicense, licenseData, _licenseBLMock.Object, _seatingBLMock.Object);

                // Assert
                Assert.Fail("wow, no throw");
            } catch(Utils.TestException e) {
                Assert.AreSame(testException, e);
            }
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
            var tempLicenseNewLicenseData = new LicenseData().WithLicenseDocument(newLicense);
            _licenseBLMock.Setup(x => x.BuildLicenseData(newLicense, true, true)).Returns(tempLicenseNewLicenseData).Verifiable();
            _licenseBLMock.Setup(x => x.ValidateNewLicenseTransition(It.Is<LicenseData>(l => l.AreSame(tempLicenseNewLicenseData)), It.Is<LicenseData>(l => l.AreSame(currentLicenseData))))
                .Returns(newLicenseData)
                .Verifiable();
            _seatingBLMock.Setup(x => x.IsSeatLicensingEnabled(currentLicenseData)).Returns(true);
            _seatingBLMock.Setup(x => x.IsSeatLicensingEnabled(newLicense)).Returns(true);
            _seatingBLMock.Setup(x => x.HandleSeatTransition(newLicense)).Returns(new SeatAssignmentInfo(2, 5));
            MockPlug.Setup(_storagePlug).At(_wikiId, "license.xml").Verb("Put").WithBody(newLicense).ExpectAtLeastOneCall();

            // Act
            var updatedLicense = _licenseController.UpdateLicense(newLicense, currentLicenseData, _licenseBLMock.Object, _seatingBLMock.Object);

            // Assert
            MockPlug.VerifyAll();
            _licenseBLMock.Verify(x => x.Validate(newLicense), Times.Once());
            _licenseBLMock.Verify(x => x.ValidateNewLicenseTransition(It.Is<LicenseData>(l => l.AreSame(tempLicenseNewLicenseData)), It.Is<LicenseData>(l => l.AreSame(currentLicenseData))), Times.Once());
            _seatingBLMock.Verify(x => x.IsSeatLicensingEnabled(currentLicenseData), Times.Once());
            _seatingBLMock.Verify(x => x.IsSeatLicensingEnabled(newLicense), Times.Once());
            _seatingBLMock.Verify(x => x.ValidateLicenseUpdateUser(newLicense), Times.Once());
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
            MockPlug.Setup(_storagePlug).At(_wikiId, "license.xml").Verb("Put").WithBody(newLicense).ExpectAtLeastOneCall();

            // Act
            var updatedLicense = _licenseController.UpdateLicense(newLicense, currentLicenseData, _licenseBLMock.Object, _seatingBLMock.Object);

            // Assert
            MockPlug.VerifyAll();
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
        public void License_check_fetches_license_if_not_present() {

            // Arrange
            var license = new XDoc("license");
            var newLicenseState = LicenseStateType.COMMERCIAL;
            var newExpiration = DateTime.UtcNow.AddDays(1);
            var newPermissions = Permissions.BROWSE;
            ulong? newSiteOwnerId = 1;
            var checkedDate = DateTime.UtcNow;
            var licenseData = new LicenseData();
            var licenseDataWithLicense = new LicenseData().WithLicenseDocument(license);
            MockPlug.Setup(_storagePlug).At(_wikiId, "license.xml").Verb("GET").Returns(license).ExpectAtLeastOneCall();
            _licenseBLMock.Setup(x => x.DetermineLicenseState(It.Is<LicenseData>(l => l.AreSame(licenseDataWithLicense)), true,false))
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
            MockPlug.VerifyAll();
            _licenseBLMock.Verify(x => x.DetermineLicenseState(It.Is<LicenseData>(l => l.AreSame(licenseDataWithLicense)), true,false), Times.Once());
            Assert.AreEqual(license, checkedLicense.LicenseDoc);
            Assert.AreEqual(newLicenseState, checkedLicense.LicenseState);
            Assert.AreEqual(newExpiration, checkedLicense.LicenseExpiration);
            Assert.AreEqual(newSiteOwnerId, checkedLicense.SiteOwnerUserId);
            Assert.AreEqual(newPermissions, checkedLicense.AnonymousPermissions);
            Assert.AreEqual(checkedDate, checkedLicense.LicenseStateChecked);
        }

        [Test]
        public void Lacking_per_instance_license_License_check_fetches_shared_license() {

            // Arrange
            var license = new XDoc("license");
            var newLicenseState = LicenseStateType.COMMERCIAL;
            var newExpiration = DateTime.UtcNow.AddDays(1);
            var newPermissions = Permissions.BROWSE;
            ulong? newSiteOwnerId = 1;
            var checkedDate = DateTime.UtcNow;
            var licenseData = new LicenseData();
            var licenseDataWithLicense = new LicenseData().WithLicenseDocument(license);
            MockPlug.Setup(_storagePlug).At(_wikiId, "license.xml").Verb("GET").Returns(DreamMessage.NotFound("this isn't the license you are looking for")).ExpectAtLeastOneCall();
            MockPlug.Setup(_storagePlug).At("license.xml").Verb("GET").Returns(license).ExpectAtLeastOneCall();
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
            MockPlug.VerifyAll();
            _licenseBLMock.Verify(x => x.DetermineLicenseState(It.Is<LicenseData>(l => l.AreSame(licenseDataWithLicense)), true, false), Times.Once());
            Assert.AreEqual(license, checkedLicense.LicenseDoc);
            Assert.AreEqual(newLicenseState, checkedLicense.LicenseState);
            Assert.AreEqual(newExpiration, checkedLicense.LicenseExpiration);
            Assert.AreEqual(newSiteOwnerId, checkedLicense.SiteOwnerUserId);
            Assert.AreEqual(newPermissions, checkedLicense.AnonymousPermissions);
            Assert.AreEqual(checkedDate, checkedLicense.LicenseStateChecked);
        }

        [Test]
        public void License_check_returns_checked_clone() {

            // Arrange
            var license = new XDoc("license");
            var newLicenseState = LicenseStateType.COMMERCIAL;
            var newExpiration = DateTime.UtcNow.AddDays(1);
            var newPermissions = Permissions.BROWSE;
            ulong? newSiteOwnerId = 1;
            var checkedDate = DateTime.UtcNow;
            var licenseData = new LicenseData()
                .WithLicenseDocument(license)
                .WithState(LicenseStateType.COMMERCIAL)
                .WithExpiration(DateTime.UtcNow)
                .WithPermissions(Permissions.NONE)
                .WithSiteOwnerUserId(1)
                .Checked(DateTime.MinValue);
            _licenseBLMock.Setup(x => x.DetermineLicenseState(It.Is<LicenseData>(l => l.AreSame(licenseData)), true,false))
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
            _licenseBLMock.Verify(x => x.DetermineLicenseState(It.Is<LicenseData>(l => l.AreSame(licenseData)), true, false), Times.Once());
            Assert.AreEqual(license, checkedLicense.LicenseDoc);
            Assert.AreEqual(newLicenseState, checkedLicense.LicenseState);
            Assert.AreEqual(newExpiration, checkedLicense.LicenseExpiration);
            Assert.AreEqual(newSiteOwnerId, checkedLicense.SiteOwnerUserId);
            Assert.AreEqual(newPermissions, checkedLicense.AnonymousPermissions);
            Assert.AreEqual(checkedDate, checkedLicense.LicenseStateChecked);
        }
    }

}

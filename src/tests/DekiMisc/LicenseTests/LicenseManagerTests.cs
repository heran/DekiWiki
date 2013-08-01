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
using MindTouch.Deki.Logic;
using MindTouch.Deki.WikiManagement;
using MindTouch.Xml;
using Moq;
using NUnit.Framework;

namespace MindTouch.Deki.Tests.LicenseTests {

    [TestFixture]
    public class LicenseManagerTests {
        private LicenseData _licenseData;
        private LicenseManager _manager;
        private Mock<ILicenseBL> _licenseBLMock;
        private Mock<IUserBL> _userMock;
        private Mock<ILicenseController> _licenseControllerMock;
        private Mock<IInstanceSettings> _instanceSettingsMock;
        private Mock<ISeatingBL> _seatingBLMock;
        private LicenseStateTransitionMock _licenseStateTransitionMock;

        [SetUp]
        public void Setup() {
            _userMock = new Mock<IUserBL>();
            _licenseData = new LicenseData();
            _licenseBLMock = new Mock<ILicenseBL>();
            _licenseControllerMock = new Mock<ILicenseController>();
            _instanceSettingsMock = new Mock<IInstanceSettings>();
            _seatingBLMock = new Mock<ISeatingBL>();
            _licenseStateTransitionMock = new LicenseStateTransitionMock();
            _manager = new LicenseManager(_licenseControllerMock.Object, _userMock.Object, _licenseData, _licenseBLMock.Object, _instanceSettingsMock.Object, _seatingBLMock.Object, _licenseStateTransitionMock.Callback);
        }

        [Test]
        public void Can_update_license() {

            // Arrange
            var currentLicense = new XDoc("current");
            var newLicense = new XDoc("new");
            var newExpiration = DateTime.UtcNow.AddDays(2);
            var newPermissions = Permissions.BROWSE;
            ulong? newOwnerId = 1;
            var checkedDate = DateTime.UtcNow;
            var inputData = new LicenseData()
                .WithLicenseDocument(currentLicense)
                .WithState(LicenseStateType.COMMERCIAL)
                .WithExpiration(DateTime.UtcNow.AddDays(1))
                .WithPermissions(Permissions.NONE)
                .WithSiteOwnerUserId(1)
                .Checked(DateTime.UtcNow);
            _licenseData.Update(inputData);
            var expected = _licenseData.Clone();
            _licenseControllerMock.Setup(x => x.UpdateLicense(newLicense, It.Is<LicenseData>(l => l.AreSame(expected)), _licenseBLMock.Object, _seatingBLMock.Object))
                .Returns(new LicenseData()
                    .WithLicenseDocument(newLicense)
                    .WithState(LicenseStateType.COMMERCIAL)
                    .WithExpiration(newExpiration)
                    .WithPermissions(newPermissions)
                    .WithSiteOwnerUserId(newOwnerId)
                    .Checked(checkedDate)
                );

            // Act
            _manager.UpdateLicense(null, newLicense);

            // Assert
            _licenseControllerMock.Verify(x => x.VerifyLicenseData(It.IsAny<LicenseData>(), _licenseBLMock.Object, _seatingBLMock.Object), Times.Never());
            _licenseControllerMock.Verify(x => x.UpdateLicense(newLicense, It.Is<LicenseData>(l => l.AreSame(expected)), _licenseBLMock.Object, _seatingBLMock.Object), Times.Once());
            _licenseStateTransitionMock.Verify(LicenseStateType.COMMERCIAL, LicenseStateType.COMMERCIAL, 1);
            Assert.AreEqual(LicenseStateType.COMMERCIAL, _licenseData.LicenseState);
            Assert.AreEqual(newLicense, _licenseData.LicenseDoc);
            Assert.AreEqual(newExpiration, _licenseData.LicenseExpiration);
            Assert.AreEqual(newOwnerId, _licenseData.SiteOwnerUserId);
            Assert.AreEqual(newPermissions, _licenseData.AnonymousPermissions);
            Assert.GreaterOrEqual(_licenseData.LicenseStateChecked, checkedDate);
        }

        [Test]
        public void Can_transition_license() {

            // Arrange
            var currentLicense = new XDoc("current");
            var newLicense = new XDoc("new");
            var newExpiration = DateTime.UtcNow.AddDays(2);
            var newPermissions = Permissions.BROWSE;
            ulong? newOwnerId = 1;
            var checkedDate = DateTime.UtcNow;
            var inputData = new LicenseData()
                .WithLicenseDocument(currentLicense)
                .WithState(LicenseStateType.INACTIVE)
                .WithExpiration(DateTime.UtcNow.AddDays(1))
                .WithPermissions(Permissions.NONE)
                .WithSiteOwnerUserId(1)
                .Checked(DateTime.UtcNow);
            _licenseData.Update(inputData);
            var expected = _licenseData.Clone();
            _licenseControllerMock.Setup(x => x.UpdateLicense(newLicense, It.Is<LicenseData>(l => l.AreSame(expected)), _licenseBLMock.Object, _seatingBLMock.Object))
                .Returns(new LicenseData()
                    .WithLicenseDocument(newLicense)
                    .WithState(LicenseStateType.COMMERCIAL)
                    .WithExpiration(newExpiration)
                    .WithPermissions(newPermissions)
                    .WithSiteOwnerUserId(newOwnerId)
                    .Checked(checkedDate)
                );

            // Act
            _manager.UpdateLicense(null, newLicense);

            // Assert
            _licenseControllerMock.Verify(x => x.VerifyLicenseData(It.IsAny<LicenseData>(), _licenseBLMock.Object, _seatingBLMock.Object), Times.Never());
            _licenseControllerMock.Verify(x => x.UpdateLicense(newLicense, It.Is<LicenseData>(l => l.AreSame(expected)), _licenseBLMock.Object, _seatingBLMock.Object), Times.Once());
            _licenseStateTransitionMock.Verify(LicenseStateType.INACTIVE, LicenseStateType.COMMERCIAL, 1);
            Assert.AreEqual(LicenseStateType.COMMERCIAL, _licenseData.LicenseState);
            Assert.AreEqual(newLicense, _licenseData.LicenseDoc);
            Assert.AreEqual(newExpiration, _licenseData.LicenseExpiration);
            Assert.AreEqual(newOwnerId, _licenseData.SiteOwnerUserId);
            Assert.AreEqual(newPermissions, _licenseData.AnonymousPermissions);
            Assert.GreaterOrEqual(_licenseData.LicenseStateChecked, checkedDate);
        }

        [Test]
        public void First_license_access_triggers_license_check() {

            // Arrange
            var license = new XDoc("license");
            var newState = LicenseStateType.COMMERCIAL;
            var newExpiration = DateTime.UtcNow.AddDays(1);
            var newPermissions = Permissions.BROWSE;
            ulong? newOwnerId = 1;
            var checkedDate = DateTime.UtcNow;
            _licenseControllerMock.Setup(x => x.VerifyLicenseData(_licenseData, _licenseBLMock.Object, _seatingBLMock.Object))
                .Returns(new LicenseData()
                    .WithLicenseDocument(license)
                    .WithState(newState)
                    .WithExpiration(newExpiration)
                    .WithPermissions(newPermissions)
                    .WithSiteOwnerUserId(newOwnerId)
                    .Checked(checkedDate)
                );

            // Act
            var state = _manager.LicenseState;

            // Assert
            _licenseControllerMock.Verify(x => x.VerifyLicenseData(_licenseData, _licenseBLMock.Object, _seatingBLMock.Object));
            _instanceSettingsMock.Verify(x => x.ClearConfigCache());
            Assert.AreEqual(newState, state);
            Assert.AreEqual(license, _licenseData.LicenseDoc);
            Assert.AreEqual(newState, _licenseData.LicenseState);
            Assert.AreEqual(newExpiration, _licenseData.LicenseExpiration);
            Assert.AreEqual(newOwnerId, _licenseData.SiteOwnerUserId);
            Assert.AreEqual(newPermissions, _licenseData.AnonymousPermissions);
            Assert.GreaterOrEqual(_licenseData.LicenseStateChecked, checkedDate);
        }

        [Test]
        public void License_access_of_validated_license_does_not_trigger_license_check() {

            // Arrange
            var license = new XDoc("license");
            var expiration = DateTime.UtcNow.AddDays(1);
            _licenseData.Update(_licenseData
                .WithLicenseDocument(license)
                .WithState(LicenseStateType.COMMERCIAL)
                .WithExpiration(expiration)
                .WithPermissions(Permissions.LOGIN)
                .WithSiteOwnerUserId(42)
                .Checked(DateTime.UtcNow));

            // Act
            var state = _manager.LicenseState;

            // Assert
            _licenseControllerMock.Verify(x => x.VerifyLicenseData(It.IsAny<LicenseData>(), _licenseBLMock.Object, _seatingBLMock.Object), Times.Never());
            Assert.AreEqual(LicenseStateType.COMMERCIAL, state);
            Assert.AreEqual(license, _manager.LicenseDocument);
            Assert.AreEqual(expiration, _manager.LicenseExpiration);
        }

        [Test]
        public void Only_first_license_access_triggers_license_check() {

            // Arrange
            var license = new XDoc("license");
            var newState = LicenseStateType.COMMERCIAL;
            var newExpiration = DateTime.UtcNow.AddDays(1);
            var newPermissions = Permissions.BROWSE;
            ulong? newOwnerId = 1;
            var checkedDate = DateTime.UtcNow;
            _licenseControllerMock.Setup(x => x.VerifyLicenseData(_licenseData, _licenseBLMock.Object, _seatingBLMock.Object))
                .Returns(new LicenseData()
                    .WithLicenseDocument(license)
                    .WithState(newState)
                    .WithExpiration(newExpiration)
                    .WithPermissions(newPermissions)
                    .WithSiteOwnerUserId(newOwnerId)
                    .Checked(checkedDate)
                );

            // Act
            var state1 = _manager.LicenseState;
            var state2 = _manager.LicenseState;

            // Assert
            _licenseControllerMock.Verify(x => x.VerifyLicenseData(_licenseData, _licenseBLMock.Object, _seatingBLMock.Object), Times.Once());
            Assert.AreEqual(newState, state1);
            Assert.AreEqual(newState, state2);
            Assert.AreEqual(license, _licenseData.LicenseDoc);
            Assert.AreEqual(newState, _licenseData.LicenseState);
            Assert.AreEqual(newExpiration, _licenseData.LicenseExpiration);
            Assert.AreEqual(newOwnerId, _licenseData.SiteOwnerUserId);
            Assert.AreEqual(newPermissions, _licenseData.AnonymousPermissions);
            Assert.GreaterOrEqual(_licenseData.LicenseStateChecked, checkedDate);
        }

        [Test]
        public void License_check_happens_periodically_on_known_good() {

            // Arrange
            var currentLicense = new XDoc("license");
            var newLicense = new XDoc("new");
            var newState = LicenseStateType.COMMERCIAL;
            var newExpiration = DateTime.UtcNow.AddDays(1);
            var newPermissions = Permissions.BROWSE;
            ulong? newOwnerId = 1;
            var oldCheckedDate = DateTime.UtcNow.AddHours(-1);
            var checkedDate = DateTime.UtcNow;
            var inputData = new LicenseData()
                .WithLicenseDocument(currentLicense)
                .WithState(LicenseStateType.COMMERCIAL)
                .WithExpiration(DateTime.UtcNow)
                .WithPermissions(Permissions.NONE)
                .WithSiteOwnerUserId(1)
                .Checked(oldCheckedDate);
            _licenseData.Update(inputData);
            _licenseControllerMock.Setup(x => x.VerifyLicenseData(_licenseData, _licenseBLMock.Object, _seatingBLMock.Object))
                .Returns(new LicenseData()
                    .WithLicenseDocument(newLicense)
                    .WithState(newState)
                    .WithExpiration(newExpiration)
                    .WithPermissions(newPermissions)
                    .WithSiteOwnerUserId(newOwnerId)
                    .Checked(checkedDate)
                );

            // Act
            var state = _manager.LicenseState;

            // Assert
            _licenseControllerMock.Verify(x => x.VerifyLicenseData(_licenseData, _licenseBLMock.Object, _seatingBLMock.Object), Times.Once());
            Assert.AreEqual(newState, state);
            Assert.AreEqual(newLicense, _licenseData.LicenseDoc);
            Assert.AreEqual(newState, _licenseData.LicenseState);
            Assert.AreEqual(newExpiration, _licenseData.LicenseExpiration);
            Assert.AreEqual(newOwnerId, _licenseData.SiteOwnerUserId);
            Assert.AreEqual(newPermissions, _licenseData.AnonymousPermissions);
            Assert.GreaterOrEqual(_licenseData.LicenseStateChecked, checkedDate);
        }

    }
}

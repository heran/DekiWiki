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
using MindTouch.Deki.Data;
using MindTouch.Deki.Exceptions;
using MindTouch.Deki.Logic;
using MindTouch.Deki.WikiManagement;
using MindTouch.Xml;
using Moq;
using NUnit.Framework;

namespace MindTouch.Deki.Tests.LicenseTests {

    [TestFixture]
    public class SeatingBLTests {
        private Mock<ILicenseBL> _licenseBLMock;
        private MockUserContext _userContext;
        private Mock<IUserBL> _userMock;
        private Mock<IDekiDataSession> _session;
        private ISeatingBL _seatingBL;

        [SetUp]
        public void Setup() {
            _userMock = new Mock<IUserBL>();
            _licenseBLMock = new Mock<ILicenseBL>();
            _userContext = new MockUserContext();
            _session = new Mock<IDekiDataSession>();
            _seatingBL = new SeatingBL(_userMock.Object, _licenseBLMock.Object, _userContext, _session.Object, LogUtils.CreateLog<SeatingBL>());
        }

        [Test]
        public void Can_validate_candidate_when_seat_licensing_is_disabled() {

            // Arrange
            var license = new XDoc("license");
            _licenseBLMock.Setup(x => x.GetCapability(license, "unseated-permissions")).Returns((string)null).Verifiable();

            // Act
            _seatingBL.ValidateLicenseUpdateUser(license);

            // Assert
            _licenseBLMock.VerifyAll();
        }

        [Test]
        public void Can_validate_candidate_when_seat_licensing_is_enabled() {

            // Arrange
            var license = new XDoc("license");
            _licenseBLMock.Setup(x => x.GetCapability(license, "unseated-permissions")).Returns(PermissionSets.MINIMAL_ANONYMOUS_PERMISSIONS.ToString()).Verifiable();
            _licenseBLMock.Setup(x => x.GetSiteOwnerUserId(license)).Returns(_userContext.User.ID).Verifiable();

            // Act
            _seatingBL.ValidateLicenseUpdateUser(license);

            // Assert
            _licenseBLMock.VerifyAll();
        }

        [Test]
        [ExpectedException(typeof(MindTouchLicenseUploadByNonOwnerException))]
        public void Non_owner_is_invalid_candidate() {

            // Arrange
            var license = new XDoc("license");
            _licenseBLMock.Setup(x => x.GetCapability(license, "unseated-permissions")).Returns(PermissionSets.MINIMAL_ANONYMOUS_PERMISSIONS.ToString());
            _licenseBLMock.Setup(x => x.GetSiteOwnerUserId(license)).Returns(_userContext.User.ID + 100);

            // Act
            _seatingBL.ValidateLicenseUpdateUser(license);
        }

        [Test]
        [ExpectedException(typeof(MindTouchLicenseNoSiteOwnerDefinedException))]
        public void License_without_owner_throws() {

            // Arrange
            var license = new XDoc("license");
            _licenseBLMock.Setup(x => x.GetCapability(license, "unseated-permissions")).Returns(PermissionSets.MINIMAL_ANONYMOUS_PERMISSIONS.ToString());

            // Act
            _seatingBL.ValidateLicenseUpdateUser(license);

        }
    }
}
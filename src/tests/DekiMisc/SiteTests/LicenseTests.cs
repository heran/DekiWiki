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
using System.IO;
using System.Collections.Generic;
using NUnit.Framework;

using MindTouch.Dream;
using MindTouch.Dream.Test;
using MindTouch.Tasking;
using MindTouch.Xml;

namespace MindTouch.Deki.Tests.SiteTests {
    [TestFixture]
    public class LicenseTests {
        private static readonly string[] _commercialLicenseArgs = new[] {
            "type=commercial",
            "sign=" + Utils.Settings.SnKeyPath,
            "id=123",
            "productkey=" + Utils.Settings.ProductKey,
            "licensee=Acme",
            "address=123",
            "hosts=" + Utils.Settings.HostAddress,
            "name=foo",
            "phone=123-456-7890",
            "email=foo@mindtouch.com",
            "users=infinite",
            "sites=infinite",
        };
        private static readonly string[] _communityLicenseArgs = new[] {
           "type=community",
           "sign=" + Utils.Settings.SnKeyPath,
        };
        private static readonly string[] _trialLicenseArgs = new[] {
            "type=trial",
            "sign=" + Utils.Settings.SnKeyPath,
            "productkey=" +Utils.Settings.ProductKey,
            "name=foo",
            "email=foo@mindtouch.com",
        };
        private static readonly string[] _expiredLicenseArgs = new[] {
             "type=trial",
             "sign=" + Utils.Settings.SnKeyPath,
             "productkey=" + Utils.Settings.ProductKey,
             "name=foo",
             "email=foo@mindtouch.com",
             "expiration=now+1",
        };

        [TestFixtureTearDown]
        public void FixtureTearDown() {
            ShutDownInstance();
            LicenseUtil.TestLicense.Save(Utils.Settings.LicensePath);
        }

        [SetUp]
        public void SetUp() {
            // Set clean license state prior to every test
            DeleteLicense();
            ShutDownInstance();
        }

        [Test]
        public void NoLicense_RevertsToCommunity() {
            // Retrieve license
            Plug p = Utils.BuildPlugForAdmin();
            DreamMessage msg = GetLicenseXML(p);
            Assert.AreEqual("community", msg.ToDocument()["@type"].AsText ?? String.Empty, "License did not revert back to community.");
        }

        [Test]
        public void RetriveLicenseAsAnonymousAndAdmin() {
            // Assure both private and public licenses are output
            Plug p = Utils.BuildPlugForAdmin();
            DreamMessage msg = GetLicenseXML(p);
            Assert.IsTrue(!msg.ToDocument()["//license.public"].IsEmpty, "license.public element is not present!");
            Assert.IsTrue(!msg.ToDocument()["//license.private"].IsEmpty, "license.private element is not present!");

            // Only public license should be output
            p = Utils.BuildPlugForAnonymous();
            msg = GetLicenseXML(p);
            Assert.IsTrue(!msg.ToDocument()["//license.public"].IsEmpty, "license.public element is not present!");
            Assert.IsTrue(msg.ToDocument()["//license.private"].IsEmpty, "license.private element is present!");
        }

        [Test]
        public void SaveCommunityLicense() {

            // Generate a community license
            XDoc license = LicenseUtil.GenerateLicense(_communityLicenseArgs);

            // Save community license and start deki service
            SaveLicense(license);
            Plug p = Utils.BuildPlugForAdmin();

            // Retrieve license
            DreamMessage msg = GetLicenseXML(p);
            Assert.AreEqual("community", msg.ToDocument()["@type"].AsText ?? String.Empty, "Unexpected license type.");
        }

        [Test]
        public void SaveTrialLicense() {
            // Generate a trial license
            XDoc license = LicenseUtil.GenerateLicense(_trialLicenseArgs);

            // Save trial license and start deki service
            SaveLicense(license);
            Plug p = Utils.BuildPlugForAdmin();

            // Retrieve license
            DreamMessage msg = GetLicenseXML(p);
            Assert.AreEqual("trial", msg.ToDocument()["@type"].AsText ?? String.Empty, "Unexpected license type.");
        }

        [Test]
        public void PutCommercialLicense() {
            // Generate the ultimate commercial license
            string[] args = new string[] { "type=commercial",
                                           "sign=" + Utils.Settings.SnKeyPath,
                                           "id=123",
                                           "productkey=" + Utils.Settings.ProductKey,
                                           "licensee=Acme",
                                           "address=123",
                                           "hosts=" + Utils.Settings.HostAddress,
                                           "name=foo",
                                           "phone=123-456-7890",
                                           "email=foo@mindtouch.com",
                                           "users=infinite",
                                           "sites=infinite",
                                           "expiration=never",
                                           "sid=sid://mindtouch.com/ent",
                                           "sid=sid://mindtouch.com/std/",
                                           "sidexpiration=never",
                                           "sid=sid://mindtouch.com/ext/2009/12/anychart",
                                           "sidexpiration=now+600",
                                           "sid=sid://mindtouch.com/ext/2009/12/anygantt",
                                           "sidexpiration=90",
                                           "sid=sid://mindtouch.com/ext/2010/06/analytics.content",
                                           "sidexpiration=" + String.Format("{0:yyyyMMdd}", DateTime.Now.AddDays(90)),
                                           "sid=sid://mindtouch.com/ext/2010/06/analytics.search",
                                           "capability:shared-cache-provider=memcache",
                                           "capabilityexpiration=" + String.Format("{0:yyyyMMdd}", DateTime.Now.AddDays(180)),
                                           "capability:anonymous-permissions=ALL",
                                           "capabilityexpiration=60",
                                           "capability:search-engine=adaptive",
                                           "capabilityexpiration=never",
                                           "capability:content-rating=enabled",
                                           "capabilityexpiration=now+600" };
            XDoc license = LicenseUtil.GenerateLicense(args);

            // Upload community license via API
            Plug p = Utils.BuildPlugForAdmin();
            DreamMessage msg = p.At("license").Put(license, new Result<DreamMessage>()).Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Uploading commercial license failed!");
        }

        [Test]
        public void PutCommercialLicenseWithUserLimitLessThanAlreadyExist() {
            // Log in as ADMIN
            Plug p = Utils.BuildPlugForAdmin();

            // Retrieve # of active users
            DreamMessage msg = p.At("users").With("activatedfilter", "true").Get(new Result<DreamMessage>()).Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Users retrieval failed");
            int usercount = msg.ToDocument()["@querycount"].AsInt ?? 0;
            Assert.IsTrue(usercount > 0, "No active users?");

            // Generate a commercial license
            usercount -= 2;
            string[] args = new string[] { "type=commercial",
                                           "sign=" + Utils.Settings.SnKeyPath,
                                           "id=123",
                                           "productkey=" + Utils.Settings.ProductKey,
                                           "licensee=Acme",
                                           "address=123",
                                           "hosts=" + Utils.Settings.HostAddress,
                                           "name=foo",
                                           "phone=123-456-7890",
                                           "email=foo@mindtouch.com",
                                           "users=" + usercount,
                                           "sites=infinite" };
            XDoc license = LicenseUtil.GenerateLicense(args);

            // Upload community license via API
            msg = p.At("license").Put(license, new Result<DreamMessage>()).Wait();
            Assert.AreEqual(DreamStatus.Forbidden, msg.Status, "Uploading commercial license with less users than currently exist succeeded?!");
        }

        [Test]
        public void CreateMoreUsersThanLicenseAllows() {
            // Log in as ADMIN
            Plug p = Utils.BuildPlugForAdmin();

            // Retrieve # of active users
            DreamMessage msg = p.At("users").With("activatedfilter", "true").Get(new Result<DreamMessage>()).Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Users retrieval failed");
            int usercount = msg.ToDocument()["@querycount"].AsInt ?? 0;
            Assert.IsTrue(usercount > 0, "No active users?");

            // Create and retrieve commercial license for active users
            string[] args = new string[] { "type=commercial",
                                           "sign=" + Utils.Settings.SnKeyPath,
                                           "id=123",
                                           "productkey=" + Utils.Settings.ProductKey,
                                           "licensee=Acme",
                                           "address=123",
                                           "hosts=" + Utils.Settings.HostAddress,
                                           "name=foo",
                                           "phone=123-456-7890",
                                           "email=foo@mindtouch.com",
                                           "users=" + usercount,
                                           "sites=infinite" };
            XDoc license = LicenseUtil.GenerateLicense(args);

            // Upload license
            msg = p.At("license").Put(license, new Result<DreamMessage>()).Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Uploading commercial license failed!");

            // Create a user (should succeed)
            msg = CreateUser(p);
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Creating a user within license limit failed?!");

            // Create another user (should fail)
            msg = CreateUser(p);
            Assert.AreEqual(DreamStatus.Forbidden, msg.Status, "Creating more users than allowed by license succeeded?!");
        }

        [Test]
        public void PutTrialLicenseToExpire() {
            // Generate a trial license to expire in 2 seconds
            string[] args = new string[] { "type=trial",
                                           "sign=" + Utils.Settings.SnKeyPath,
                                           "productkey=" + Utils.Settings.ProductKey,
                                           "name=foo",
                                           "email=foo@mindtouch.com",
                                           "expiration=now+2" };
            XDoc license = LicenseUtil.GenerateLicense(args);

            // Save trial license
            SaveLicense(license);

            // Wait for 2 seconds
            WaitFor(2);

            // Start the deki service
            Plug p = Utils.BuildPlugForAdmin();

            // Retrieve license
            DreamMessage msg = GetLicenseXML(p);
            Assert.AreEqual("trial", msg.ToDocument()["@type"].AsText ?? String.Empty, "Unexpected license type.");

            // Retrieve settings and verify that license/state = EXPIRED
            msg = p.At("site", "settings").Get(new Result<DreamMessage>()).Wait();
            Assert.AreEqual("EXPIRED", msg.ToDocument()["license/state"].AsText ?? String.Empty, "Unexpected license state in site/settings");
        }

        [Test]
        public void PutCommercialLicense_AnonymousAccessCapabilityToExpire() {
            const int CAPABILITY_EXPIRATION = 10;

            // Log in as ADMIN
            Plug p = Utils.BuildPlugForAdmin();

            // Create a commercial license where anonymous access expires
            string[] args = new string[] { "type=commercial",
                                           "sign=" + Utils.Settings.SnKeyPath,
                                           "id=123",
                                           "productkey=" + Utils.Settings.ProductKey,
                                           "licensee=Acme",
                                           "address=123",
                                           "hosts=" + Utils.Settings.HostAddress,
                                           "name=foo",
                                           "phone=123-456-7890",
                                           "email=foo@mindtouch.com",
                                           "users=infinite",
                                           "sites=infinite",
                                           "capability:anonymous-permissions=ALL",
                                           "capabilityexpiration=now+" + CAPABILITY_EXPIRATION };
            XDoc license = LicenseUtil.GenerateLicense(args);

            // Upload license
            DreamMessage msg = p.At("license").Put(license, new Result<DreamMessage>()).Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Uploading commercial license failed!");

            // Retrieve home page as anonymous
            msg = p.At("pages", "home").Get(new Result<DreamMessage>()).Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Anonymous page retrieval failed?!");

            // Wait CAPABILITY_EXPIRATION seconds
            WaitFor(CAPABILITY_EXPIRATION);

            // Restart service
            ShutDownInstance();
            p = Utils.BuildPlugForAnonymous();

            // Retrieve home page as anonymous again, should be unauthorized
            msg = p.At("pages", "home").Get(new Result<DreamMessage>()).Wait();
            Assert.AreEqual(DreamStatus.Unauthorized, msg.Status, "Anonymous still has READ permissions?!");
        }

        [Test]
        public void PutCommercialLicense_AnonymousPermissions() {
            const int CAPABILITY_EXPIRATION = 10;

            // Log in as ADMIN
            Plug p = Utils.BuildPlugForAdmin();

            // Create a commercial license where anonymous access expires
            string[] args = new string[] { "type=commercial",
                                           "sign=" + Utils.Settings.SnKeyPath,
                                           "id=123",
                                           "productkey=" + Utils.Settings.ProductKey,
                                           "licensee=Acme",
                                           "address=123",
                                           "hosts=" + Utils.Settings.HostAddress,
                                           "name=foo",
                                           "phone=123-456-7890",
                                           "email=foo@mindtouch.com",
                                           "users=infinite",
                                           "sites=infinite",
                                           "capability:anonymous-permissions=LOGIN,BROWSE"};
            XDoc license = LicenseUtil.GenerateLicense(args);

            // Upload license
            DreamMessage msg = p.At("license").Put(license, new Result<DreamMessage>()).Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Uploading commercial license failed!");

            msg = Utils.BuildPlugForAnonymous().At("users", "current").GetAsync().Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "get:users/current failed");

            // ensure that unseated user has permissions revoked other than those listed by "capability:unseated-permissions"
            Assert.IsNotEmpty(msg.AsDocument()["permissions.revoked/permissions.license.anonymous/operations"].AsText ?? string.Empty);
            Assert.IsFalse(msg.AsDocument()["permissions.revoked/permissions.license.anonymous/operations"].AsText.ContainsInvariantIgnoreCase("LOGIN"));
            Assert.IsFalse(msg.AsDocument()["permissions.revoked/permissions.license.anonymous/operations"].AsText.ContainsInvariantIgnoreCase("BROWSE"));
        }

        [Test]
        public void Can_transition_from_built_in_community_to_commercial() {
            var p = Utils.BuildPlugForAdmin();
            var msg = p.At("license").Put(LicenseUtil.TestLicense, new Result<DreamMessage>()).Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Community -> Commercial transition failed");
        }


        [Test]
        public void Can_transition_from_community_to_commercial() {
            var license = LicenseUtil.GenerateLicense(_communityLicenseArgs);
            SaveLicense(license);
            var p = Utils.BuildPlugForAdmin();
            var msg = p.At("license").Put(LicenseUtil.TestLicense, new Result<DreamMessage>()).Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Community -> Commercial transition failed");
        }

        [Test]
        public void Can_transition_from_trial_to_commercial() {
            var license = LicenseUtil.GenerateLicense(_trialLicenseArgs);
            SaveLicense(license);
            var p = Utils.BuildPlugForAdmin();
            var msg = p.At("license").Put(LicenseUtil.TestLicense, new Result<DreamMessage>()).Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Trial -> Commercial transition failed");
        }

        [Test]
        public void Can_transition_from_commercial_to_commercial() {
            var license = LicenseUtil.GenerateLicense(_commercialLicenseArgs);
            SaveLicense(license);
            var p = Utils.BuildPlugForAdmin();
            var msg = p.At("license").Put(LicenseUtil.TestLicense, new Result<DreamMessage>()).Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Commercial -> Commercial transition failed");
        }

        [Test]
        public void Can_transition_from_expired_to_commercial() {
            var license = LicenseUtil.GenerateLicense(_expiredLicenseArgs);
            SaveLicense(license);
            var p = Utils.BuildPlugForAdmin();
            var msg = p.At("license").Put(LicenseUtil.TestLicense, new Result<DreamMessage>()).Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Expired -> Commercial transition failed");
        }

        // TODO: inspect nondeterminsitic response
        [Test]
        public void Transition_from_commercial_to_expired_fails() {
            var license = LicenseUtil.GenerateLicense(_commercialLicenseArgs);
            SaveLicense(license);
            license = LicenseUtil.GenerateLicense(_expiredLicenseArgs);
            var p = Utils.BuildPlugForAdmin();
            var msg = p.At("license").Put(license, new Result<DreamMessage>()).Wait();
            Assert.IsTrue((DreamStatus.Forbidden ==  msg.Status) || (DreamStatus.BadRequest == msg.Status), "Transition to expired license succeeded?!");
        }

        [Test]
        public void Transition_from_community_to_trial_fails() {
            var license = LicenseUtil.GenerateLicense(_communityLicenseArgs);
            SaveLicense(license);
            license = LicenseUtil.GenerateLicense(_trialLicenseArgs);
            var p = Utils.BuildPlugForAdmin();
            var msg = p.At("license").Put(license, new Result<DreamMessage>()).Wait();
            Assert.AreEqual(DreamStatus.Forbidden, msg.Status, "Community -> Trial transition succeeded?!");
        }

        [Test]
        public void Can_transition_to_community() {
            var p = Utils.BuildPlugForAdmin();
            var license = LicenseUtil.GenerateLicense(_communityLicenseArgs);
            var msg = p.At("license").Put(license, new Result<DreamMessage>()).Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Failed to transition to community license");
        }

        [Test]
        public void CommunityToCommercialWithBadProductKey_BadRequest() {
            // Commercial license to transition to
            string[] args = new string[] { "type=commercial",
                                           "sign=" + Utils.Settings.SnKeyPath,
                                           "id=123",
                                           "productkey=00000000000000000000000000000000",
                                           "licensee=Acme",
                                           "address=123",
                                           "hosts=" + Utils.Settings.HostAddress,
                                           "name=foo",
                                           "phone=123-456-7890",
                                           "email=foo@mindtouch.com",
                                           "users=infinite",
                                           "sites=infinite" };
            XDoc commercial_license = LicenseUtil.GenerateLicense(args);

            // Community -> Commercial
            Plug p = Utils.BuildPlugForAdmin();
            DreamMessage msg = p.At("license").Put(commercial_license, new Result<DreamMessage>()).Wait();
            Assert.AreEqual(DreamStatus.BadRequest, msg.Status, "Community -> Commercial transition with bad product key succeeded?");
        }

        [Test]
        public void Upload_Save_BadLicense_AndCreateUser() {
            // Generate a commercial license
            XDoc license = LicenseUtil.GenerateLicense(_commercialLicenseArgs);

            // Tamper with the XML to invalidate it
            license["@type"].ReplaceValue("invalid");

            // Attempt to upload tampered license
            Plug p = Utils.BuildPlugForAdmin();
            DreamMessage msg = p.At("license").Put(license, new Result<DreamMessage>()).Wait();
            Assert.AreEqual(DreamStatus.BadRequest, msg.Status, "Upload of tampered license succeeded?!");

            // Sidestep API by saving license and restarting service
            DeleteLicense();
            SaveLicense(license);
            ShutDownInstance();

            // Check that the license state is invalid
            p = Utils.BuildPlugForAdmin();
            msg = p.At("site", "settings").Get(new Result<DreamMessage>()).Wait();
            Assert.AreEqual("INVALID", msg.ToDocument()["license/state"].AsText ?? String.Empty, "Unexpected license state in site/settings");

            // Try to create a user
            msg = CreateUser(p);
            Assert.AreEqual(DreamStatus.Forbidden, msg.Status, "User creation with invalid license succeeded?!");
        }

        [Test]
        public void SaveExpiredCommercialLicenseWithinAndBeyondGracePeriod() {
            // Hard coded in LicenseBL.cs
            const int GRACE = 14;

            // Within grace (GRACE/2)
            string[] args = new string[] { "type=commercial",
                                           "sign=" + Utils.Settings.SnKeyPath,
                                           "id=123",
                                           "productkey=" + Utils.Settings.ProductKey,
                                           "licensee=Acme",
                                           "address=123",
                                           "hosts=" + Utils.Settings.HostAddress,
                                           "name=foo",
                                           "phone=123-456-7890",
                                           "email=foo@mindtouch.com",
                                           "users=infinite",
                                           "sites=infinite",
                                           "expiration=" + String.Format("{0:yyyyMMdd}", DateTime.Now.AddDays(-GRACE/2)) };
            XDoc license = LicenseUtil.GenerateLicense(args);

            // Need to sidestep API since uploading expired license is not allowed
            SaveLicense(license);

            // Check that license has not yet expired
            Plug p = Utils.BuildPlugForAdmin();
            DreamMessage msg = p.At("site", "settings").Get(new Result<DreamMessage>()).Wait();
            Assert.AreEqual("COMMERCIAL", msg.ToDocument()["license/state"].AsText ?? String.Empty, "Unexpected license state in site/settings");

            // Generate license outside of grace (GRACE*2)
            args = new string[] { "type=commercial",
                                           "sign=" + Utils.Settings.SnKeyPath,
                                           "id=123",
                                           "productkey=" + Utils.Settings.ProductKey,
                                           "licensee=Acme",
                                           "address=123",
                                           "hosts=" + Utils.Settings.HostAddress,
                                           "name=foo",
                                           "phone=123-456-7890",
                                           "email=foo@mindtouch.com",
                                           "users=infinite",
                                           "sites=infinite",
                                           "expiration=" + String.Format("{0:yyyyMMdd}", DateTime.Now.AddDays(-GRACE*2)) };
            license = LicenseUtil.GenerateLicense(args);

            // Need to sidestep API since uploading expired license is not allowed
            DeleteLicense();
            SaveLicense(license);
            ShutDownInstance();

            // Check that license is expired
            p = Utils.BuildPlugForAdmin();
            msg = p.At("site", "settings").Get(new Result<DreamMessage>()).Wait();
            Assert.AreEqual("EXPIRED", msg.ToDocument()["license/state"].AsText ?? String.Empty, "Unexpected license state in site/settings");
        }

        [Test]
        public void CommercialSaveOldVersion_IsExpired() {
            // Save version 8
            string[] args = new string[] { "type=commercial",
                                           "sign=" + Utils.Settings.SnKeyPath,
                                           "id=123",
                                           "productkey=" + Utils.Settings.ProductKey,
                                           "licensee=Acme",
                                           "address=123",
                                           "hosts=" + Utils.Settings.HostAddress,
                                           "name=foo",
                                           "phone=123-456-7890",
                                           "email=foo@mindtouch.com",
                                           "users=infinite",
                                           "sites=infinite",
                                           "expiration=never",
                                           "version=8" };
            XDoc license = LicenseUtil.GenerateLicense(args);

            // Need to sidestep API since uploading expired license is not allowed
            SaveLicense(license);

            // Check that license is expired
            Plug p = Utils.BuildPlugForAdmin();
            DreamMessage msg = p.At("site", "settings").Get(new Result<DreamMessage>()).Wait();
            Assert.AreEqual("EXPIRED", msg.ToDocument()["license/state"].AsText ?? String.Empty, "Unexpected license state in site/settings");
        }

        [Test]
        public void TestLicenseGenerator() {
            // This test method invokes CallLicenseGenerator() which runs the license generator
            // tool. Command line arguments are passed as an array of strings.
            // The purpose of this method tests only the license generator and checks
            // whether a license was successfully generated or not. 

            // No args - returns error
            string[] args = new string[] { };
            Tuplet<int, Stream, Stream> exitValues = LicenseUtil.CallLicenseGenerator(args);
            Assert.AreNotEqual(0, exitValues.Item1, "Unexpected return code");

            // Community
            exitValues = LicenseUtil.CallLicenseGenerator(_communityLicenseArgs);
            Assert.AreEqual(0, exitValues.Item1, "Unexpected return code\n" + GetErrorMsg(exitValues.Item2) + GetErrorMsg(exitValues.Item3));

            // Trial
            exitValues = LicenseUtil.CallLicenseGenerator(_trialLicenseArgs);
            Assert.AreEqual(0, exitValues.Item1, "Unexpected return code\n" + GetErrorMsg(exitValues.Item2) + GetErrorMsg(exitValues.Item3));

            // Commercial
            exitValues = LicenseUtil.CallLicenseGenerator(_commercialLicenseArgs);
            Assert.AreEqual(0, exitValues.Item1, "Unexpected return code\n" + GetErrorMsg(exitValues.Item2) + GetErrorMsg(exitValues.Item3));
        }
        
        // Stop instance to allow subsequent license retrieval upon plug instantiation
        private void ShutDownInstance() {
            Plug p = Utils.BuildPlugForAdmin();
            var msg = p.At("host", "stop").With("apikey", Utils.Settings.ApiKey).Post(new Result<DreamMessage>()).Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Failed to shutdown instance!");
        }

        [Test]
        public void SeatLicensing_UploadLicenseWithoutOwner() {

            // Log in as ADMIN
            Plug p = Utils.BuildPlugForAdmin();

            // Retrieve # of active users
            DreamMessage msg = p.At("users").With("seatfilter", "true").Get(new Result<DreamMessage>()).Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Users retrieval failed");
            int seatCount = msg.ToDocument()["@querycount"].AsInt ?? 0;

            // Create and retrieve commercial license for active users
            string[] args = new string[] { "type=commercial",
                                           "sign=" + Utils.Settings.SnKeyPath,
                                           "id=123",
                                           "productkey=" + Utils.Settings.ProductKey,
                                           "licensee=Acme",
                                           "address=123",
                                           "hosts=" + Utils.Settings.HostAddress,
                                           "name=foo",
                                           "phone=123-456-7890",
                                           "email=foo@mindtouch.com",
                                           "seats=" + (seatCount + 1),
                                           "users=infinite",
                                           "sites=infinite",
                                           "capability:unseated-permissions=LOGIN,READ,BROWSE"
                                            };
            XDoc license = LicenseUtil.GenerateLicense(args);

            // Upload license
            msg = p.At("license").Put(license, new Result<DreamMessage>()).Wait();
            Assert.AreEqual(DreamStatus.Conflict, msg.Status, "Uploaded seat license enabled license without ownerid!");
        }

        [Test]
        public void SeatLicensing_UploadLicenseAsNonOwner() {

            // Log in as ADMIN
            Plug p = Utils.BuildPlugForAdmin();

            // Retrieve # of active users
            DreamMessage msg = p.At("users").With("seatfilter", "true").Get(new Result<DreamMessage>()).Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Users retrieval failed");
            int seatCount = msg.ToDocument()["@querycount"].AsInt ?? 0;

            // Create and retrieve commercial license for active users
            string[] args = new string[] { "type=commercial",
                                           "sign=" + Utils.Settings.SnKeyPath,
                                           "id=123",
                                           "ownerid=123456789",
                                           "productkey=" + Utils.Settings.ProductKey,
                                           "licensee=Acme",
                                           "address=123",
                                           "hosts=" + Utils.Settings.HostAddress,
                                           "name=foo",
                                           "phone=123-456-7890",
                                           "email=foo@mindtouch.com",
                                           "seats=" + (seatCount + 1),
                                           "users=infinite",
                                           "sites=infinite",
                                           "capability:unseated-permissions=LOGIN,READ,BROWSE"
                                            };
            XDoc license = LicenseUtil.GenerateLicense(args);

            // Upload license
            msg = p.At("license").Put(license, new Result<DreamMessage>()).Wait();
            Assert.AreEqual(DreamStatus.Conflict, msg.Status, "Uploaded seat license enabled license without ownerid!");
        }

        [Test]
        public void SeatLicensing_TransferSiteOwner() {

            // Log in as ADMIN
            Plug p = Utils.BuildPlugForAdmin();

            // Determine admin userid
            DreamMessage msg = p.At("users", "current").GetAsync().Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "GET: users/current failed for admin");
            string ownerUserId = msg.AsDocument()["/user/@id"].AsText;

            // Retrieve # of active users
            msg = p.At("users").With("seatfilter", "true").Get(new Result<DreamMessage>()).Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Users retrieval failed");
            int seatCount = msg.ToDocument()["@querycount"].AsInt ?? 0;

            // Create and retrieve commercial license for active users
            string[] args = new string[] { "type=commercial",
                                           "sign=" + Utils.Settings.SnKeyPath,
                                           "id=123",
                                           "ownerid="+ownerUserId,
                                           "productkey=" + Utils.Settings.ProductKey,
                                           "licensee=Acme",
                                           "address=123",
                                           "hosts=" + Utils.Settings.HostAddress,
                                           "name=foo",
                                           "phone=123-456-7890",
                                           "email=foo@mindtouch.com",
                                           "seats=" + (seatCount + 1),
                                           "users=infinite",
                                           "sites=infinite",
                                           "capability:unseated-permissions=LOGIN,READ,BROWSE"
                                            };
            XDoc license = LicenseUtil.GenerateLicense(args);

            // Upload license
            msg = p.At("license").Put(license, new Result<DreamMessage>()).Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Uploaded seat license enabled license without ownerid!");

            // create another admin user
            string admin2userid;
            string admin2username;
            UserUtils.CreateRandomUser(p, "ADMIN", out admin2userid, out admin2username);

            // Create and retrieve commercial license for active users
            args = new string[] { "type=commercial",
                                           "sign=" + Utils.Settings.SnKeyPath,
                                           "id=123",
                                           "ownerid="+admin2userid,
                                           "productkey=" + Utils.Settings.ProductKey,
                                           "licensee=Acme",
                                           "address=123",
                                           "hosts=" + Utils.Settings.HostAddress,
                                           "name=foo",
                                           "phone=123-456-7890",
                                           "email=foo@mindtouch.com",
                                           "seats=" + (seatCount + 2),
                                           "users=infinite",
                                           "sites=infinite",
                                           "capability:unseated-permissions=LOGIN,READ,BROWSE"
                                            };
            license = LicenseUtil.GenerateLicense(args);
        
            // Set a seat for admin2
            msg = Utils.BuildPlugForAdmin().At("users", admin2userid, "seat").PutAsync(DreamMessage.Ok(MimeType.TEXT_UTF8, string.Empty)).Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Setting a seat failed");

            // Upload license
            msg = Utils.BuildPlugForUser(admin2username).At("license").Put(license, new Result<DreamMessage>()).Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "uploading license failed!");        
        }

        [Test]
        public void SeatLicensing_UploaderHasSeat() {

            // Log in as ADMIN
            Plug p = Utils.BuildPlugForAdmin();

            // Determine admin userid
            DreamMessage msg = p.At("users", "current").GetAsync().Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "GET: users/current failed for admin");
            Assert.AreEqual(null, msg.AsDocument()["license.seat"].AsText, "User has seat before test started!");
            string ownerUserId = msg.AsDocument()["/user/@id"].AsText;

            // Retrieve # of active users
            msg = p.At("users").With("seatfilter", "true").Get(new Result<DreamMessage>()).Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Users retrieval failed");
            int seatCount = msg.ToDocument()["@querycount"].AsInt ?? 0;

            // Create and retrieve commercial license for active users
            string[] args = new string[] { "type=commercial",
                                           "sign=" + Utils.Settings.SnKeyPath,
                                           "id=123",
                                           "ownerid="+ownerUserId,
                                           "productkey=" + Utils.Settings.ProductKey,
                                           "licensee=Acme",
                                           "address=123",
                                           "hosts=" + Utils.Settings.HostAddress,
                                           "name=foo",
                                           "phone=123-456-7890",
                                           "email=foo@mindtouch.com",
                                           "seats=" + (seatCount + 1),
                                           "users=infinite",
                                           "sites=infinite",
                                           "capability:unseated-permissions=LOGIN,READ,BROWSE"
                                            };
            XDoc license = LicenseUtil.GenerateLicense(args);

            // Upload license
            msg = p.At("license").Put(license, new Result<DreamMessage>()).Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Uploaded seat license enabled license without ownerid!");

            // admin has seat?
            msg = p.At("users", "current").GetAsync().Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "GET: users/current failed for admin");
            Assert.AreEqual(true, msg.AsDocument()["license.seat"].AsBool, "User has no seat!");
            Assert.AreEqual(true, msg.AsDocument()["license.seat/@owner"].AsBool.Value);
        }

        [Test]
        public void SeatLicensing_SetSeatLicenseForUser() {
            // Log in as ADMIN
            Plug p = Utils.BuildPlugForAdmin();

            // Upload non seat license to clear seats
            string[] args = new string[] { "type=commercial",
                                           "sign=" + Utils.Settings.SnKeyPath,
                                           "id=123",
                                           "productkey=" + Utils.Settings.ProductKey,
                                           "licensee=Acme",
                                           "address=123",
                                           "hosts=" + Utils.Settings.HostAddress,
                                           "name=foo",
                                           "phone=123-456-7890",
                                           "email=foo@mindtouch.com",
                                           "users=infinite",
                                           "sites=infinite" };
            XDoc license = LicenseUtil.GenerateLicense(args);

            // Upload license
            DreamMessage msg = p.At("license").Put(license, new Result<DreamMessage>()).Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Uploading commercial license failed!");

            // Determine admin userid
            msg = p.At("users", "current").GetAsync().Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "GET: users/current failed for admin");
            Assert.AreEqual(null, msg.AsDocument()["license.seat"].AsText, "User has seat before test started!");
            string ownerUserId = msg.AsDocument()["/user/@id"].AsText;

            // Create and retrieve commercial license for active users
            args = new string[] { "type=commercial",
                                           "sign=" + Utils.Settings.SnKeyPath,
                                           "id=123",
                                           "productkey=" + Utils.Settings.ProductKey,
                                           "licensee=Acme",
                                           "address=123",
                                           "hosts=" + Utils.Settings.HostAddress,
                                           "name=foo",
                                           "phone=123-456-7890",
                                           "email=foo@mindtouch.com",
                                           "ownerid="+ownerUserId,
                                           "seats=" + (2),
                                           "capability:unseated-permissions=LOGIN,READ,BROWSE",
                                           "users=infinite",
                                           "sites=infinite" };
            license = LicenseUtil.GenerateLicense(args);

            // Upload license
            msg = p.At("license").Put(license, new Result<DreamMessage>()).Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Uploading commercial license failed!");

            // Create a user
            string userId;
            string userName;
            msg = UserUtils.CreateRandomUser(p, "Contributor", out userId, out userName);
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Creating a user failed?!");

            // set the seat for the user
            msg = p.At("users", userId, "seat").PutAsync(DreamMessage.Ok(MimeType.TEXT_UTF8, string.Empty)).Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Setting a seat failed");

            // Create another user 
            msg = UserUtils.CreateRandomUser(p, "Contributor", out userId, out userName);
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Creating a user failed?!");

            // Set seat when seats should already be depleted
            msg = p.At("users", userId, "seat").PutAsync(DreamMessage.Ok(MimeType.TEXT_UTF8, string.Empty)).Wait();
            Assert.AreEqual(DreamStatus.Conflict, msg.Status, "Seat given when open seats depleted!");
        }


        [Test]
        public void SeatLicensing_SetSeatLicenseForAnonymousUser() {
            // Log in as ADMIN
            Plug p = Utils.BuildPlugForAdmin();

            // Determine admin userid
            DreamMessage msg = p.At("users", "current").GetAsync().Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "GET: users/current failed for admin");
            Assert.AreEqual(null, msg.AsDocument()["license.seat"].AsText, "User has seat before test started!");
            string ownerUserId = msg.AsDocument()["/user/@id"].AsText;

            // Retrieve # of active users
            msg = p.At("users").With("seatfilter", "true").Get(new Result<DreamMessage>()).Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Users retrieval failed");
            int seatCount = msg.ToDocument()["@querycount"].AsInt ?? 0;

            // Create and retrieve commercial license for active users
            string[] args = new string[] { "type=commercial",
                                           "sign=" + Utils.Settings.SnKeyPath,
                                           "id=123",
                                           "productkey=" + Utils.Settings.ProductKey,
                                           "licensee=Acme",
                                           "address=123",
                                           "hosts=" + Utils.Settings.HostAddress,
                                           "name=foo",
                                           "phone=123-456-7890",
                                           "email=foo@mindtouch.com",
                                           "seats=" + (seatCount + 2),
                                           "capability:unseated-permissions=LOGIN,READ,BROWSE",
                                           "ownerid="+ownerUserId,
                                           "users=infinite",
                                           "sites=infinite" };
            XDoc license = LicenseUtil.GenerateLicense(args);

            // Upload license
            msg = p.At("license").Put(license, new Result<DreamMessage>()).Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Uploading commercial license failed!");

            // set the seat for the anonymous user
            msg = p.At("users", "=anonymous", "seat").PutAsync(DreamMessage.Ok(MimeType.TEXT_UTF8, string.Empty)).Wait();
            Assert.AreEqual(DreamStatus.Conflict, msg.Status, "Was able to set a seat for anonymous user!");
        }

        [Test]
        public void SeatLicensing_RemoveSeatLicenseForUser() {
            // Log in as ADMIN
            Plug p = Utils.BuildPlugForAdmin();

            // Determine admin userid
            DreamMessage msg = p.At("users", "current").GetAsync().Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "GET: users/current failed for admin");
            Assert.AreEqual(null, msg.AsDocument()["license.seat"].AsText, "User has seat before test started!");
            string ownerUserId = msg.AsDocument()["/user/@id"].AsText;

            // Retrieve # of active users
            msg = p.At("users").With("seatfilter", "true").Get(new Result<DreamMessage>()).Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Users retrieval failed");
            int seatCount = msg.ToDocument()["@querycount"].AsInt ?? 0;

            // Create and retrieve commercial license for active users
            string[] args = new string[] { "type=commercial",
                                           "sign=" + Utils.Settings.SnKeyPath,
                                           "id=123",
                                           "productkey=" + Utils.Settings.ProductKey,
                                           "licensee=Acme",
                                           "address=123",
                                           "hosts=" + Utils.Settings.HostAddress,
                                           "name=foo",
                                           "phone=123-456-7890",
                                           "email=foo@mindtouch.com",
                                           "seats=" + (seatCount + 2),
                                           "capability:unseated-permissions=LOGIN,READ,BROWSE",
                                           "ownerid="+ownerUserId,
                                           "users=infinite",
                                           "sites=infinite" };
            XDoc license = LicenseUtil.GenerateLicense(args);

            // Upload license
            msg = p.At("license").Put(license, new Result<DreamMessage>()).Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Uploading commercial license failed!");

            // Create a user
            string userId;
            string userName;
            msg = UserUtils.CreateRandomUser(p, "Contributor", out userId, out userName);
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Creating a user failed?!");

            // remove seat for user that has no seat
            msg = p.At("users", userId, "seat").DeleteAsync().Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "remove seat from seatless user should have been an OK response");

            // give a seat to the user
            msg = p.At("users", userId, "seat").PutAsync(DreamMessage.Ok(MimeType.TEXT_UTF8, string.Empty)).Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "giving a seat failed");

            // remove seat for seated user
            msg = p.At("users", userId, "seat").DeleteAsync().Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "was unable to remove a seat");

            // remove seat for unseated user
            msg = p.At("users", userId, "seat").DeleteAsync().Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "was unable to remove a seat");
        }

        [Test]
        public void SeatLicensing_UnseatedPermissions() {
            // Log in as ADMIN
            Plug p = Utils.BuildPlugForAdmin();

            // Determine admin userid
            DreamMessage msg = p.At("users", "current").GetAsync().Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "GET: users/current failed for admin");
            Assert.AreEqual(null, msg.AsDocument()["license.seat"].AsText, "User has seat before test started!");
            string ownerUserId = msg.AsDocument()["/user/@id"].AsText;

            // Retrieve # of active users
            msg = p.At("users").With("seatfilter", "true").Get(new Result<DreamMessage>()).Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Users retrieval failed");
            int seatCount = msg.ToDocument()["@querycount"].AsInt ?? 0;

            // Create and retrieve commercial license for active users
            string[] args = new string[] { "type=commercial",
                                           "sign=" + Utils.Settings.SnKeyPath,
                                           "id=123",
                                           "productkey=" + Utils.Settings.ProductKey,
                                           "licensee=Acme",
                                           "address=123",
                                           "hosts=" + Utils.Settings.HostAddress,
                                           "name=foo",
                                           "phone=123-456-7890",
                                           "email=foo@mindtouch.com",
                                           "seats=" + (seatCount + 1),
                                           "users=infinite",
                                           "sites=infinite",
                                           "ownerid="+ownerUserId,
                                           "capability:unseated-permissions=LOGIN,READ,BROWSE"
                                            };
            XDoc license = LicenseUtil.GenerateLicense(args);

            // Upload license
            msg = p.At("license").Put(license, new Result<DreamMessage>()).Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Uploading commercial license failed!");

            // Create a user
            string userId;
            string userName;
            msg = UserUtils.CreateRandomUser(p, "Contributor", out userId, out userName);
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Creating a user failed?!");

            Assert.AreEqual(false, msg.AsDocument()["license.seat"].AsBool, "User shouldn't have a seat!");

            // ensure that unseated user has permissions revoked other than those listed by "capability:unseated-permissions"
            Assert.IsFalse(msg.AsDocument()["permissions.revoked/permissions.license.unseated/operations"].AsText.ContainsInvariantIgnoreCase("LOGIN"));
            Assert.IsFalse(msg.AsDocument()["permissions.revoked/permissions.license.unseated/operations"].AsText.ContainsInvariantIgnoreCase("READ"));
            Assert.IsFalse(msg.AsDocument()["permissions.revoked/permissions.license.unseated/operations"].AsText.ContainsInvariantIgnoreCase("BROWSE"));

            // ensure that unseated user has effective permissions that at least include those listed by "capability:unseated-permissions"
            Assert.IsTrue(msg.AsDocument()["permissions.effective/operations"].AsText.ContainsInvariantIgnoreCase("LOGIN"));
            Assert.IsTrue(msg.AsDocument()["permissions.effective/operations"].AsText.ContainsInvariantIgnoreCase("READ"));
            Assert.IsTrue(msg.AsDocument()["permissions.effective/operations"].AsText.ContainsInvariantIgnoreCase("BROWSE"));

            // give a seat to the user
            msg = p.At("users", userId, "seat").PutAsync(DreamMessage.Ok(MimeType.TEXT_UTF8, string.Empty)).Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "giving a seat failed");            
            Assert.AreEqual(true, msg.AsDocument()["license.seat"].AsBool, "User should have a seat!");

            // ensure that permissions are no longer revoked due to seats
            Assert.IsNull(msg.AsDocument()["permissions.revoked/permissions.license.lackingseat/operations"].AsText, "perms being revoked for seated user!");
        }

        [Test]
        [Ignore]
        public void SeatLicensing_UnexpectedLicenseClearSeats() {

            int _seats = 2;

            // Log in as ADMIN
            Plug p = Utils.BuildPlugForAdmin();

            // Determine admin userid
            DreamMessage msg = p.At("users", "current").GetAsync().Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "GET: users/current failed for admin");
            string ownerUserId = msg.AsDocument()["/user/@id"].AsText;

            // Create and retrieve commercial license for active users
            string[] args = new string[] { "type=commercial",
                                           "sign=" + Utils.Settings.SnKeyPath,
                                           "id=123",
                                           "ownerid="+ownerUserId,
                                           "productkey=" + Utils.Settings.ProductKey,
                                           "licensee=Acme",
                                           "address=123",
                                           "hosts=" + Utils.Settings.HostAddress,
                                           "name=foo",
                                           "phone=123-456-7890",
                                           "email=foo@mindtouch.com",
                                           "seats=" + (_seats+1), // _seats + site owner
                                           "users=infinite",
                                           "sites=infinite",
                                           "capability:unseated-permissions=LOGIN,READ,BROWSE"
                                            };
            XDoc license = LicenseUtil.GenerateLicense(args);

            // Upload license
            msg = p.At("license").Put(license, new Result<DreamMessage>()).Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Failed to upload license");

            // Create _seats users and give them all seats
            string userid;
            for (int i = 0; i < _seats; i++) {
                msg = UserUtils.CreateRandomContributor(p, out userid);
                msg = p.At("users", userid, "seat").PutAsync(DreamMessage.Ok(MimeType.TEXT_UTF8, string.Empty)).Wait();
                Assert.AreEqual(DreamStatus.Ok, msg.Status, "giving a seat failed");
                Assert.AreEqual(true, msg.ToDocument()["license.seat"].AsBool, "User should have a seat!");
            }

            // Make sure there _seats+1 users with seats
            msg = p.At("users").With("seatfilter", "true").Get(new Result<DreamMessage>()).Wait();
            Assert.AreEqual(_seats + 1, msg.ToDocument()["@querycount"].AsInt ?? 0, "Unexpected number of seats taken!");

            // Shutdown instance and sneak in a new license
            ShutDownInstance();
            DeleteLicense();
            args[11] = "seats=1";
            license = LicenseUtil.GenerateLicense(args);
            SaveLicense(license);

            // Start instance and verify that all seats were cleared
            p = Utils.BuildPlugForAdmin();
            msg = p.At("license").Get(new Result<DreamMessage>()).Wait();
            Assert.AreEqual(1, msg.ToDocument()["grants/seats"].AsInt ?? 0, "Unexpected user seats in sneaked license.");
            msg = p.At("users").With("seatfilter", "true").Get(new Result<DreamMessage>()).Wait();
            Assert.AreEqual(1, msg.ToDocument()["@querycount"].AsInt ?? 0, "Seats were not cleared!");
            
            // Verify remaining user is owner
            Assert.AreEqual(ownerUserId, msg.ToDocument()["user/@id"].AsText ?? String.Empty, "Remaining user is not the owner!");
        }

        [Test]
        public void SeatLicensing_UploadLicenseWithLessSeatsThanAlreadyTaken() {

            int _seats = 4;

            // Log in as ADMIN
            Plug p = Utils.BuildPlugForAdmin();

            // Determine admin userid
            DreamMessage msg = p.At("users", "current").GetAsync().Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "GET: users/current failed for admin");
            string ownerUserId = msg.AsDocument()["/user/@id"].AsText;

            // Create and retrieve commercial license for active users
            string[] args = new string[] { "type=commercial",
                                           "sign=" + Utils.Settings.SnKeyPath,
                                           "id=123",
                                           "ownerid="+ownerUserId,
                                           "productkey=" + Utils.Settings.ProductKey,
                                           "licensee=Acme",
                                           "address=123",
                                           "hosts=" + Utils.Settings.HostAddress,
                                           "name=foo",
                                           "phone=123-456-7890",
                                           "email=foo@mindtouch.com",
                                           "seats=" + _seats+1, // _seats + site owner
                                           "users=infinite",
                                           "sites=infinite",
                                           "capability:unseated-permissions=LOGIN,READ,BROWSE"
                                            };
            XDoc license = LicenseUtil.GenerateLicense(args);

            // Upload license
            msg = p.At("license").Put(license, new Result<DreamMessage>()).Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Failed to upload license");

            // Create _seats users and give them all seats
            string userid;
            var userIds = new List<string>();
            for (int i = 0; i < _seats; i++) {
                msg = UserUtils.CreateRandomContributor(p, out userid);
                userIds.Add(userid);
                msg = p.At("users", userid, "seat").PutAsync(DreamMessage.Ok(MimeType.TEXT_UTF8, string.Empty)).Wait();
                Assert.AreEqual(DreamStatus.Ok, msg.Status, "giving a seat failed");
                Assert.AreEqual(true, msg.ToDocument()["license.seat"].AsBool, "User should have a seat!");
            }

            // Try to upload license with less seats than are taken
            args[11] = "seats=" + _seats;
            license = LicenseUtil.GenerateLicense(args);

            msg = p.At("license").Put(license, new Result<DreamMessage>()).Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "failed in uploading license with less seats than are already taken!");

            foreach(string useridToCheck in userIds) {
                msg = p.At("users", useridToCheck).Get();
                Assert.AreEqual(DreamStatus.Ok, msg.Status, "user lookup failed");
                Assert.AreEqual(false, msg.ToDocument()["license.seat"].AsBool, "User should not have a seat!");
            }


        }

        [Test]
        public void SeatLicensing_RemoveSeatFromSiteOwner() {

            // Log in as ADMIN
            Plug p = Utils.BuildPlugForAdmin();

            // Determine admin userid
            DreamMessage msg = p.At("users", "current").GetAsync().Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "GET: users/current failed for admin");
            string ownerUserId = msg.AsDocument()["/user/@id"].AsText;

            // Create and retrieve commercial license for active users
            string[] args = new string[] { "type=commercial",
                                           "sign=" + Utils.Settings.SnKeyPath,
                                           "id=123",
                                           "ownerid="+ownerUserId,
                                           "productkey=" + Utils.Settings.ProductKey,
                                           "licensee=Acme",
                                           "address=123",
                                           "hosts=" + Utils.Settings.HostAddress,
                                           "name=foo",
                                           "phone=123-456-7890",
                                           "email=foo@mindtouch.com",
                                           "seats=5", 
                                           "users=infinite",
                                           "sites=infinite",
                                           "capability:unseated-permissions=LOGIN,READ,BROWSE"
                                            };
            XDoc license = LicenseUtil.GenerateLicense(args);

            // Upload license
            msg = p.At("license").Put(license, new Result<DreamMessage>()).Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Failed to upload license");

            msg = p.At("users", ownerUserId).Get(new Result<DreamMessage>()).Wait();
            Assert.IsTrue(msg.ToDocument()["license.seat"].AsBool ?? false, "Site owner does not have seat?!");
            Assert.IsTrue(msg.ToDocument()["license.seat/@owner"].AsBool ?? false, "Site owner attribute missing!");

            // Try to remove site owner seat
            msg = p.At("users", ownerUserId, "seat").Delete(new Result<DreamMessage>()).Wait();
            Assert.AreEqual(DreamStatus.Conflict, msg.Status, "Site owner seat removal succeeded?!");
        }

        [Test]
        public void SeatLicensing_UserSeatsUndefined() {

            // Log in as ADMIN
            Plug p = Utils.BuildPlugForAdmin();

            // Determine admin userid
            DreamMessage msg = p.At("users", "current").GetAsync().Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "GET: users/current failed for admin");
            string ownerUserId = msg.AsDocument()["/user/@id"].AsText;

            // Create and retrieve commercial license for active users
            string[] args = new string[] { "type=commercial",
                                           "sign=" + Utils.Settings.SnKeyPath,
                                           "id=123",
                                           "ownerid="+ownerUserId,
                                           "productkey=" + Utils.Settings.ProductKey,
                                           "licensee=Acme",
                                           "address=123",
                                           "hosts=" + Utils.Settings.HostAddress,
                                           "name=foo",
                                           "phone=123-456-7890",
                                           "email=foo@mindtouch.com",
                                           //"seats=5", 
                                           "users=infinite",
                                           "sites=infinite",
                                           "capability:unseated-permissions=LOGIN,READ,BROWSE"
                                            };
            XDoc license = LicenseUtil.GenerateLicense(args);

            // Upload license
            msg = p.At("license").Put(license, new Result<DreamMessage>()).Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Failed to upload license");

            // Give a user a seat
            string userid;
            msg = UserUtils.CreateRandomContributor(p, out userid);
            msg = p.At("users", userid, "seat").PutAsync(DreamMessage.Ok(MimeType.TEXT_UTF8, string.Empty)).Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "giving a seat failed");
            Assert.AreEqual(true, msg.ToDocument()["license.seat"].AsBool, "User should have a seat!");
        }

        [Test]
        public void SeatLicensing_AddRemoveRecommendSeatWithoutSeatLicensing() {
        
            // Login as ADMIN
            Plug p = Utils.BuildPlugForAdmin();

            // Default license has seat licensing disabled, proceed
            // Add seat
            string userid;
            var msg = UserUtils.CreateRandomContributor(p, out userid);
            msg = p.At("users", userid, "seat").PutAsync(DreamMessage.Ok(MimeType.TEXT_UTF8, string.Empty)).Wait();
            Assert.AreEqual(DreamStatus.Conflict, msg.Status, "Giving a seat succeeded?!");
            Assert.IsTrue(msg.ToDocument()["license.seat"].IsEmpty, "Unexpected license.seat element!");

            // Remove seat
            msg = p.At("users", userid, "seat").Delete(new Result<DreamMessage>()).Wait();
            Assert.AreEqual(DreamStatus.Conflict, msg.Status, "Removing a seat succeeded?!");
            Assert.IsTrue(msg.ToDocument()["license.seat"].IsEmpty, "Unexpected license.seat element!");

            // Get Recommendation
            msg = p.At("users").With("seatfilter", "recommended").Get(new Result<DreamMessage>()).Wait();
            Assert.AreEqual(DreamStatus.Conflict, msg.Status, "Seat recommendation succeeded?!");
        }

        [Ignore]
        [Test]
        public void SeatLicensing_GetRecommendation() { 

            // Login as ADMIN
            Plug p = Utils.BuildPlugForAdmin();

            // Determine admin userid
            DreamMessage msg = p.At("users", "current").GetAsync().Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "GET: users/current failed for admin");
            string ownerUserId = msg.AsDocument()["/user/@id"].AsText;

            // Create and retrieve commercial license for active users
            string[] args = new string[] { "type=commercial",
                                           "sign=" + Utils.Settings.SnKeyPath,
                                           "id=123",
                                           "ownerid="+ownerUserId,
                                           "productkey=" + Utils.Settings.ProductKey,
                                           "licensee=Acme",
                                           "address=123",
                                           "hosts=" + Utils.Settings.HostAddress,
                                           "name=foo",
                                           "phone=123-456-7890",
                                           "email=foo@mindtouch.com",
                                           //"seats=5", 
                                           "users=infinite",
                                           "sites=infinite",
                                           "capability:unseated-permissions=LOGIN,READ,BROWSE"
                                            };
            XDoc license = LicenseUtil.GenerateLicense(args);

            // Upload license
            msg = p.At("license").Put(license, new Result<DreamMessage>()).Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Failed to upload license");

            // Create Contributor
            string contrib_id;
            UserUtils.CreateRandomContributor(p, out contrib_id);

            // Create User with only GUEST permissions
            string guest_id;
            string guest_name;
            UserUtils.CreateRandomUser(p, "Guest", out guest_id, out guest_name);

            // Check that contributor received recommendation and guest did not
            // Limit=all + seatfilter doesn't work
            msg = p.At("users").With("seatfilter", "recommended").With("sortby","-date.created").Get(new Result<DreamMessage>()).Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Retrieving seat recommendations failed!");
            Assert.IsFalse(msg.ToDocument()["user[@id=" + contrib_id + "]"].IsEmpty, "Contributor should be recommended!");
            Assert.IsTrue(msg.ToDocument()["user[@id=" + guest_id + "]"].IsEmpty, "Guest shouldn't be recommended!");
        }

        [Test]
        public void SeatLicensing_DisableSiteOwner() {

            // Log in as ADMIN
            Plug p = Utils.BuildPlugForAdmin();

            // Determine admin userid
            DreamMessage msg = p.At("users", "current").GetAsync().Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "GET: users/current failed for admin");
            string ownerUserId = msg.AsDocument()["/user/@id"].AsText;

            // Create and retrieve commercial license for active users
            string[] args = new string[] { "type=commercial",
                                           "sign=" + Utils.Settings.SnKeyPath,
                                           "id=123",
                                           "ownerid="+ownerUserId,
                                           "productkey=" + Utils.Settings.ProductKey,
                                           "licensee=Acme",
                                           "address=123",
                                           "hosts=" + Utils.Settings.HostAddress,
                                           "name=foo",
                                           "phone=123-456-7890",
                                           "email=foo@mindtouch.com",
                                           "seats=5", 
                                           "users=infinite",
                                           "sites=infinite",
                                           "capability:unseated-permissions=LOGIN,READ,BROWSE"
                                            };
            XDoc license = LicenseUtil.GenerateLicense(args);

            // Upload license
            msg = p.At("license").Put(license, new Result<DreamMessage>()).Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Failed to upload license");

            msg = p.At("users", ownerUserId).Get(new Result<DreamMessage>()).Wait();
            Assert.IsTrue(msg.ToDocument()["license.seat"].AsBool ?? false, "Site owner does not have seat?!");
            Assert.IsTrue(msg.ToDocument()["license.seat/@owner"].AsBool ?? false, "Site owner attribute missing!");

            // Try to disable site owner
            XDoc userXml = new XDoc("user").Attr("id", ownerUserId).Elem("status", "inactive");

            msg = p.At("users", ownerUserId).Put(DreamMessage.Ok(userXml), new Result<DreamMessage>()).Wait();
            Assert.AreEqual(DreamStatus.Conflict, msg.Status, "Site owner disabling succeeded?!");
        }

        [Test]
        public void SeatLicensing_DisableUserRemovesSeat() {

            // Log in as ADMIN
            Plug p = Utils.BuildPlugForAdmin();

            // Determine admin userid
            DreamMessage msg = p.At("users", "current").GetAsync().Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "GET: users/current failed for admin");
            string ownerUserId = msg.AsDocument()["/user/@id"].AsText;

            // Create and retrieve commercial license for active users
            string[] args = new string[] { "type=commercial",
                                           "sign=" + Utils.Settings.SnKeyPath,
                                           "id=123",
                                           "ownerid="+ownerUserId,
                                           "productkey=" + Utils.Settings.ProductKey,
                                           "licensee=Acme",
                                           "address=123",
                                           "hosts=" + Utils.Settings.HostAddress,
                                           "name=foo",
                                           "phone=123-456-7890",
                                           "email=foo@mindtouch.com",
                                           "seats=5", 
                                           "users=infinite",
                                           "sites=infinite",
                                           "capability:unseated-permissions=LOGIN,READ,BROWSE"
                                            };
            XDoc license = LicenseUtil.GenerateLicense(args);

            // Upload license
            msg = p.At("license").Put(license, new Result<DreamMessage>()).Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Failed to upload license");

            msg = p.At("users", ownerUserId).Get(new Result<DreamMessage>()).Wait();
            Assert.IsTrue(msg.ToDocument()["license.seat"].AsBool ?? false, "Site owner does not have seat?!");
            Assert.IsTrue(msg.ToDocument()["license.seat/@owner"].AsBool ?? false, "Site owner attribute missing!");

            // Give a user a seat
            string userid;
            msg = UserUtils.CreateRandomContributor(p, out userid);
            msg = p.At("users", userid, "seat").PutAsync(DreamMessage.Ok(MimeType.TEXT_UTF8, string.Empty)).Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "giving a seat failed");
            Assert.AreEqual(true, msg.ToDocument()["license.seat"].AsBool, "User should have a seat!");

            // disable the new user
            XDoc userXml = new XDoc("user").Attr("id", userid).Elem("status", "inactive");
            msg = p.At("users", userid).Put(DreamMessage.Ok(userXml), new Result<DreamMessage>()).Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Site owner disabling succeeded?!");

            // confirm user is now unseated
            msg = p.At("users", userid).Get(new Result<DreamMessage>()).Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Could not lookup user");
            Assert.AreEqual(false, msg.AsDocument()["license.seat"].AsBool, "user should be seatless!");
        }

        // Save license at license storage path
        private void SaveLicense(XDoc license_doc) {
            FileStream fs = new FileStream(Utils.Settings.LicensePath, FileMode.Create);
            StreamWriter sw = new StreamWriter(fs);
            sw.Write(license_doc.ToString());
            sw.Close();
        }

        // Delete license from license storage path
        private void DeleteLicense() {
            if(File.Exists(Utils.Settings.LicensePath)) {
                File.Delete(Utils.Settings.LicensePath);
            }
        }

        // Error stream -> error string
        private string GetErrorMsg(Stream error) {
            StreamReader sr = new StreamReader(error);
            return sr.ReadToEnd();
        }

        // GET:license request with success check
        private DreamMessage GetLicenseXML(Plug p) {
            DreamMessage msg = p.At("license").Get(new Result<DreamMessage>()).Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "License retrieval failed");
            return msg;
        }

        // Create a user

        private DreamMessage CreateUser(Plug p) {
            // User XML
            string name = Utils.GenerateUniqueName();
            string role = "Contributor";
            string email = "licensetest@mindtouch.com";
            string password = "password";
            XDoc usersDoc = new XDoc("user")
                .Elem("username", name)
                .Elem("email", email)
                .Elem("fullname", name + "'s full name")
                .Start("permissions.user")
                    .Elem("role", role)
                .End();

            // Send request to create user
            return p.At("users").With("accountpassword", password).Post(usersDoc, new Result<DreamMessage>()).Wait();
        }

        // Wait for N seconds
        private void WaitFor(int seconds) {
            Wait.For(() => {
                return false;
            }, TimeSpan.FromSeconds(seconds));
        }

        /// <summary>
        ///     Retrieve site license
        /// </summary>        
        /// <feature>
        /// <name>GET:license</name>
        /// <uri>http://developer.mindtouch.com/en/ref/MindTouch_API/GET%3alicense</uri>
        /// </feature>
        /// <expected>200 OK HTTP response</expected>

        [Test]
        public void GetLicense() {
            // Build ADMIN plug
            Plug p = Utils.BuildPlugForAdmin();

            // Retrieve license in XML format
            DreamMessage msg = p.At("license").With("format", "xml").Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "License retrieval in XML format failed");

            // Retrieve license in HTML format
            msg = p.At("license").With("format", "html").Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "License retrieval in HTML format failed");
        }
    }
}

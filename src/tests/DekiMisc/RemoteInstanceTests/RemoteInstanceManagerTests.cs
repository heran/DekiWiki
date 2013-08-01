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
using System.Text;
using System.Threading;
using MindTouch.Deki.WikiManagement;
using MindTouch.Dream;
using MindTouch.Dream.Test;
using MindTouch.Dream.Test.Mock;
using MindTouch.Tasking;
using MindTouch.Xml;
using NUnit.Framework;
using MindTouch.Extensions.Time;

namespace MindTouch.Deki.Tests.RemoteInstanceTests {

    [TestFixture]
    public class RemoteInstanceManagerTests {

        //--- Class Fields ---
        private static readonly log4net.ILog _log = LogUtils.CreateLog();
        private static readonly string _expiration = String.Format("{0:yyyyMMdd}", DateTime.Now.AddDays(2));
        private static readonly string[] _oldApikeyLicenseArgs = new[] {
            "type=commercial",
            "sign=" + Utils.Settings.SnKeyPath,
            "id=123",
            "productkey=" + StringUtil.ComputeHashString("foobar", Encoding.UTF8),
            "licensee=Acme",
            "address=123",
            "hosts=" + Utils.Settings.HostAddress,
            "name=foo",
            "phone=123-456-7890",
            "email=foo@mindtouch.com",
            "users=infinite",
            "sites=infinite",
            "sid=sid://mindtouch.com/ent",
            "sid=sid://mindtouch.com/std/",
            "sidexpiration=" + _expiration,
            "sid=sid://mindtouch.com/ext/2009/12/anychart",
            "sidexpiration=" + _expiration,
            "sid=sid://mindtouch.com/ext/2009/12/anygantt",
            "sidexpiration=" + _expiration,
            "sid=sid://mindtouch.com/ext/2010/06/analytics.content",
            "sidexpiration=" + _expiration,
            "sid=sid://mindtouch.com/ext/2010/06/analytics.search",
            "capability:anonymous-permissions=ALL",
            "capabilityexpiration=" + _expiration,
            "capability:search-engine=adaptive",
            "capabilityexpiration=" + _expiration,
            "capability:content-rating=enabled",
            "capabilityexpiration=" + _expiration,
        };
        private static readonly string[] _expiredLicenseArgs = new[] {
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
            "expiration="+DateTime.UtcNow.AddDays(-30).ToString("yyyyMMdd"),
        };
        private static readonly string[] _communityLicenseArgs = new[] {
           "type=community",
           "sign=" + Utils.Settings.SnKeyPath,
        };

        [SetUp]
        public void Setup() {
            MockPlug.DeregisterAll();
            Utils.Settings.SetupAsRemoteInstance();
        }

        [TearDown]
        public void Teardown() {
            Utils.Settings.SetupAsLocalInstance();
        }

        [Test]
        public void Can_load_site_via_remote_instance_manager() {
            var p = Utils.BuildPlugForAdmin();
            var instances = p.At("host").With("apikey", Utils.Settings.ApiKey).Get(new Result<DreamMessage>()).Wait();
            Assert.AreEqual(DreamStatus.Ok, instances.Status, "Failed to retrieve running instances");
            Assert.AreEqual("default", instances.ToDocument()["tenant/@wikiid"].AsText ?? String.Empty, "Unexpected wiki id");
            Assert.AreEqual("running", instances.ToDocument()["tenant/@status"].AsText ?? String.Empty, "Unexpected status");
            var settings = p.At("site", "settings").With("apikey", Utils.Settings.ApiKey).Get();
            Assert.AreEqual(DreamStatus.Ok, settings.Status, "unable to retrieve settings");
            Assert.AreEqual("cloud", settings.ToDocument()["instance/mode"].AsText);
        }

        [Test]
        public void Can_start_with_inactive_license() {
            RemoteInstanceService.Instance.LicenseOverride = _ => LicenseUtil.InactiveLicense;
            var p = Utils.BuildPlugForAdmin();
            var instances = p.At("host").With("apikey", Utils.Settings.ApiKey).Get(new Result<DreamMessage>()).Wait();
            Assert.AreEqual(DreamStatus.Ok, instances.Status, "Failed to retrieve running instances");
            Assert.AreEqual("default", instances.ToDocument()["tenant/@wikiid"].AsText ?? String.Empty, "Unexpected wiki id");
            Assert.AreEqual("running", instances.ToDocument()["tenant/@status"].AsText ?? String.Empty, "Unexpected status");
            var settings = p.At("site", "settings").With("apikey", Utils.Settings.ApiKey).Get();
            Assert.AreEqual(DreamStatus.Ok, settings.Status, "unable to retrieve settings");
            Assert.AreEqual("cloud", settings.ToDocument()["instance/mode"].AsText);
        }

        [Test]
        public void Can_push_inactive_to_active_transition_via_license_update() {
            var eventSubscriber = new XUri("mock://event/subscriber");
            var subForAuthorized = new XDoc("subscription-set")
                 .Elem("uri.owner", eventSubscriber)
                 .Start("subscription")
                     .Attr("id", "1")
                     .Elem("channel", "event://default/deki/site/license/update")
                     .Start("recipient")
                        .Attr("authtoken", Utils.Settings.ApiKey)
                        .Elem("uri", eventSubscriber)
                    .End()
                 .End();
            var result = Utils.Settings.Server.At("pubsub", "subscribers").With("apikey", Utils.Settings.ApiKey).PostAsync(subForAuthorized).Wait();
            Assert.IsTrue(result.IsSuccessful);
            MockPlug.Setup(eventSubscriber).Verb("POST").WithMessage(msg => {
                var doc = msg.ToDocument();
                return "INACTIVE".EqualsInvariantIgnoreCase(doc["previous-license/@state"].AsText)
                    && "COMMERCIAL".EqualsInvariantIgnoreCase(doc["new-license/@state"].AsText);
            }).ExpectCalls(Times.Once());
            var inactiveLicenseCalls = 0;
            RemoteInstanceService.Instance.LicenseOverride = _ => {
                inactiveLicenseCalls++;
                _log.DebugFormat("pre update call {0} to remote license endpoint, returning inactive", inactiveLicenseCalls);
                return LicenseUtil.InactiveLicense;
            };
            var licenses = new XDoc("licenses")
                .Start("old-license").Add(LicenseUtil.InactiveLicense).End()
                .Start("new-license").Add(LicenseUtil.TestLicense);
            _log.Debug("updating license");
            var response = Utils.Settings.Server.At("license")
                .With("apikey", Utils.Settings.ApiKey)
                .Put(licenses, new Result<DreamMessage>())
                .Wait();
            _log.Debug("updated license");
            var commercialLicenseCalls = 0;
            RemoteInstanceService.Instance.LicenseOverride = _ => {
                commercialLicenseCalls++;
                _log.DebugFormat("post update call {0} to remote license endpoint, returning test license", commercialLicenseCalls);
                return LicenseUtil.TestLicense;
            };
            Assert.AreEqual(DreamStatus.Ok, response.Status, "unable to update license");
            MockPlug.VerifyAll(10.Seconds());
            response = Utils.Settings.Server.At("license")
                .With("apikey", Utils.Settings.ApiKey)
                .Get(new Result<DreamMessage>())
                .Wait();
            Assert.AreEqual(DreamStatus.Ok, response.Status, "unable to fetch license");
            var license = response.ToDocument();
            Assert.AreEqual("commercial", license["@type"].AsText, "bad license:\r\n" + license.ToPrettyString());
        }

        [Test]
        public void Can_push_transition_from_expired_license_via_update_license() {
            var expired = LicenseUtil.GenerateLicense(_expiredLicenseArgs);
            var expiration = expired["date.expiration"].AsDate ?? DateTime.MinValue;
            var when = expiration - DateTime.UtcNow;
            _log.InfoFormat("--- license expired expired {0} days ago -- {1}", -when.TotalDays, expiration);
            var eventSubscriber = new XUri("mock://event/subscriber");
            var subForAuthorized = new XDoc("subscription-set")
                 .Elem("uri.owner", eventSubscriber)
                 .Start("subscription")
                     .Attr("id", "1")
                     .Elem("channel", "event://default/deki/site/license/update")
                     .Start("recipient")
                        .Attr("authtoken", Utils.Settings.ApiKey)
                        .Elem("uri", eventSubscriber)
                    .End()
                 .End();
            var result = Utils.Settings.Server.At("pubsub", "subscribers").With("apikey", Utils.Settings.ApiKey).PostAsync(subForAuthorized).Wait();
            Assert.IsTrue(result.IsSuccessful);
            MockPlug.Setup(eventSubscriber).Verb("POST").WithMessage(msg => {
                var doc = msg.ToDocument();
                _log.Debug("transition doc:\r\n" + doc.ToPrettyString());
                return "EXPIRED".EqualsInvariantIgnoreCase(doc["previous-license/@state"].AsText)
                    && "COMMERCIAL".EqualsInvariantIgnoreCase(doc["new-license/@state"].AsText);
            }).ExpectCalls(Times.Once());
            var licenses = new XDoc("licenses")
                .Start("old-license").Add(expired).End()
                .Start("new-license").Add(LicenseUtil.TestLicense);
            var response = Utils.Settings.Server.At("license")
                .With("apikey", Utils.Settings.ApiKey)
                .Put(licenses, new Result<DreamMessage>())
                .Wait();
            Assert.AreEqual(DreamStatus.Ok, response.Status, "unable to update license");
            MockPlug.VerifyAll(10.Seconds());
        }

        [Test]
        public void Can_push_transition_to_license_with_new_apikey_via_update_license() {
            var old = LicenseUtil.GenerateLicense(_oldApikeyLicenseArgs);
            var eventSubscriber = new XUri("mock://event/subscriber");
            var subForAuthorized = new XDoc("subscription-set")
                 .Elem("uri.owner", eventSubscriber)
                 .Start("subscription")
                     .Attr("id", "1")
                     .Elem("channel", "event://default/deki/site/license/update")
                     .Start("recipient")
                        .Attr("authtoken", Utils.Settings.ApiKey)
                        .Elem("uri", eventSubscriber)
                    .End()
                 .End();
            var result = Utils.Settings.Server.At("pubsub", "subscribers").With("apikey", Utils.Settings.ApiKey).PostAsync(subForAuthorized).Wait();
            Assert.IsTrue(result.IsSuccessful);
            MockPlug.Setup(eventSubscriber).Verb("POST").WithMessage(msg => {
                var doc = msg.ToDocument();
                _log.Debug("transition doc:\r\n" + doc.ToPrettyString());
                return "COMMERCIAL".EqualsInvariantIgnoreCase(doc["previous-license/@state"].AsText)
                    && "COMMERCIAL".EqualsInvariantIgnoreCase(doc["new-license/@state"].AsText);
            }).ExpectCalls(Times.Once());
            var licenses = new XDoc("licenses")
                .Start("old-license").Add(old).End()
                .Start("new-license").Add(LicenseUtil.TestLicense);
            var response = Utils.Settings.Server.At("license")
                .With("apikey", Utils.Settings.ApiKey)
                .Put(licenses, new Result<DreamMessage>())
                .Wait();
            Assert.AreEqual(DreamStatus.Ok, response.Status, "unable to update license");
            MockPlug.VerifyAll(10.Seconds());
        }

        [Test]
        public void Instance_is_accessible_on_receiving_transition_push_via_update_license() {
            var eventSubscriber = new XUri("mock://event/subscriber");
            var subForAuthorized = new XDoc("subscription-set")
                 .Elem("uri.owner", eventSubscriber)
                 .Start("subscription")
                     .Attr("id", "1")
                     .Elem("channel", "event://default/deki/site/license/update")
                     .Start("recipient")
                        .Attr("authtoken", Utils.Settings.ApiKey)
                        .Elem("uri", eventSubscriber)
                    .End()
                 .End();
            var result = Utils.Settings.Server.At("pubsub", "subscribers").With("apikey", Utils.Settings.ApiKey).PostAsync(subForAuthorized).Wait();
            Assert.IsTrue(result.IsSuccessful);
            var mre = new ManualResetEvent(false);
            DreamMessage settingsResult = null;
            MockPlug.Register(eventSubscriber, (p, v, u, r, r2) => {
                r2.Return(DreamMessage.Ok());
                _log.DebugFormat("got transition event, trying to fetch license");
                settingsResult = Utils.Settings.Server.At("license")
                    .With("apikey", Utils.Settings.ApiKey)
                    .WithHeader("X-Deki-Site", "id=default")
                    .Get(new Result<DreamMessage>()).Wait();
                mre.Set();
            });
            var licenses = new XDoc("licenses")
                .Start("old-license").Add(LicenseUtil.InactiveLicense).End()
                .Start("new-license").Add(LicenseUtil.TestLicense);
            var response = Utils.Settings.Server.At("license")
                .With("apikey", Utils.Settings.ApiKey)
                .Put(licenses, new Result<DreamMessage>())
                .Wait();
            Assert.AreEqual(DreamStatus.Ok, response.Status, "unable to update license");
            Assert.IsTrue(mre.WaitOne(10.Seconds()));
            Assert.IsTrue(settingsResult.IsSuccessful);
            var settings = settingsResult.ToDocument();
            Assert.AreEqual("commercial", settings["@type"].AsText.ToLower());
        }

        [Test]
        public void Bad_license_prevents_instance_from_starting_up() {
            Assert.AreEqual(0, Utils.Settings.Server.At("host").With("apikey", Utils.Settings.ApiKey).Get().ToDocument()["tenant"].ListLength);
            File.Delete(Utils.Settings.LicensePath);
            var community = LicenseUtil.GenerateLicense(_communityLicenseArgs);
            community.Save(Utils.Settings.LicensePath);
            _log.InfoFormat("--- wrote community license to {0}", Utils.Settings.LicensePath);
            RemoteInstanceService.Instance.LicenseOverride = _ => community;
            Assert.IsFalse(Utils.Settings.Server.At("site", "settings").Get(new Result<DreamMessage>()).Wait().IsSuccessful);
            var tenants = Utils.Settings.Server.At("host").With("apikey", Utils.Settings.ApiKey).Get().ToDocument();
            _log.Info("tenants:\r\n" + tenants.ToPrettyString());
            var tenant = tenants["tenant"];
            Assert.AreEqual(1, tenant.ListLength);
            Assert.AreEqual("abandoned", tenant["@status"].AsText);
        }

        [Test]
        public void Local_license_expiring_after_startup_affects_license_state_in_settings() {
            var p = Utils.BuildPlugForAdmin();
            var tenants = Utils.Settings.Server.At("host").With("apikey", Utils.Settings.ApiKey).Get().ToDocument();
            _log.Info("tenants:\r\n" + tenants.ToPrettyString());
            Assert.AreEqual(1, tenants["tenant[@wikiid='default']"].ListLength);
            try {
                var expired = LicenseUtil.GenerateLicense(_expiredLicenseArgs);
                RemoteInstanceService.Instance.LicenseOverride = _ => expired;
                LicenseManager.LicenseCheckInterval = 0;
                File.Delete(Utils.Settings.LicensePath);
                expired.Save(Utils.Settings.LicensePath);
                _log.Debug("---- forcing license check");
                var settings = p.At("site", "settings").With("apikey", Utils.Settings.ApiKey).Get();
                Assert.AreEqual(DreamStatus.Ok, settings.Status, "unable to retrieve settings");
                Assert.AreEqual("EXPIRED", settings.ToDocument()["license/state"].AsText);
            } finally {
                LicenseManager.LicenseCheckInterval = LicenseManager.LICENSE_CHECK_INTERVAL;
            }
        }
    }
}

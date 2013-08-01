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
using System.IO;
using System.Linq;
using System.Threading;
using MindTouch.Dream;
using MindTouch.Dream.Test;
using MindTouch.Dream.Test.Mock;
using MindTouch.Tasking;
using MindTouch.Xml;

using NUnit.Framework;
using MindTouch.Extensions.Time;

namespace MindTouch.Deki.Tests {

    [TestFixture]
    public class DekiInstanceTests {

        //--- Class Fields ---
        private static readonly log4net.ILog _log = LogUtils.CreateLog();

        [SetUp]
        public void Setup() {
            MockPlug.DeregisterAll();
            Utils.Settings.ShutdownHost();
        }

        [Ignore("Stress test")]
        [Test]
        public void Multiple_simultaneous_requests() {
            var signal = new ManualResetEvent(false);
            var apihits = new List<Result<Result<DreamMessage>>>();
            var n = 50;
            using(var server = CreateTestServer()) {
                var core = server.CreateCoreService();
                for(var i = 0; i < n; i++) {
                    apihits.Add(Async.ForkThread(() => {
                        signal.WaitOne();
                        return core.AtLocalHost.At("pages", "1").GetAsync();
                    },
                                                 new Result<Result<DreamMessage>>()));
                }
                signal.Set();
                foreach(Result<Result<DreamMessage>> r in apihits) {
                    r.Wait();
                    r.Value.Wait();
                    Assert.IsTrue(r.Value.Value.IsSuccessful, r.Value.Value.ToString());
                }
                var services = core.AtLocalHost.At("site", "services").With("limit", "all").With("apikey", Utils.Settings.ApiKey).GetAsync().Wait();
                Assert.IsTrue(services.IsSuccessful, services.ToText());
                foreach(var service in services.ToDocument()["service[status='disabled']"]) {
                    var error = service["lasterror"].Contents;
                    if(string.IsNullOrEmpty(error)) {
                        continue;
                    }
                    Console.WriteLine(service.ToPrettyString());
                }
            }
        }

        [Ignore("Stress test")]
        [Test]
        public void Start_Stop_services_concurrently() {
            using(var server = CreateTestServer()) {
                var core = server.CreateCoreService();
                int retry = 10;
                while(true) {
                    var status = core.AtLocalHost.At("site", "status").With("apikey", Utils.Settings.ApiKey).GetAsync().Wait();
                    if(status.IsSuccessful && status.ToDocument()["state"].Contents == "RUNNING") {
                        break;
                    }
                    retry--;
                    if(retry < 0) {
                        throw new Exception("host never stared up");
                    }
                    Thread.Sleep(1000);
                }
                var services = core.AtLocalHost.At("site", "services").With("limit", "all").With("apikey", Utils.Settings.ApiKey).GetAsync().Wait();
                Assert.IsTrue(services.IsSuccessful, services.ToText());
                var servicesToRestart = from service in services.ToDocument()["service[status='enabled']"]
                                        let id = service["@id"].AsInt ?? 0
                                        where !string.IsNullOrEmpty(service["uri"].Contents) && id != 1
                                        select service;
                var signal = new ManualResetEvent(false);
                var apihits = new List<Result<DreamMessage>>();
                foreach(var service in servicesToRestart) {
                    var localId = service["@id"].AsInt ?? 0;
                    apihits.Add(Async.ForkThread(() => {
                        signal.WaitOne();
                        DreamMessage response = null;
                        for(var k = 0; k < 10; k++) {
                            response = core.AtLocalHost.At("site", "services", localId.ToString(), "stop").With("apikey", Utils.Settings.ApiKey).PostAsync().Wait();
                            if(response.IsSuccessful) {
                                response = core.AtLocalHost.At("site", "services", localId.ToString(), "start").With("apikey", Utils.Settings.ApiKey).PostAsync().Wait();
                            }
                        }
                        return response;
                    },
                                                 new Result<DreamMessage>()));
                }
                signal.Set();
                foreach(var r in apihits) {
                    r.Wait();
                    Assert.IsTrue(r.Value.IsSuccessful, r.Value.ToString());
                }
            }
        }

        [Ignore("Stress test")]
        [Test]
        public void Multi_instance_service_start_stop() {
            using(var server1 = CreateTestServer()) {
                using(var server2 = CreateTestServer()) {
                    var core1 = server1.CreateCoreService();
                    var core2 = server2.CreateCoreService();
                    int retry = 10;
                    while(true) {
                        var status1 = core1.AtLocalHost.At("site", "status").With("apikey", Utils.Settings.ApiKey).GetAsync().Wait();
                        var status2 = core2.AtLocalHost.At("site", "status").With("apikey", Utils.Settings.ApiKey).GetAsync().Wait();
                        if(status1.IsSuccessful && status1.ToDocument()["state"].Contents == "RUNNING" &&
                           status2.IsSuccessful && status2.ToDocument()["state"].Contents == "RUNNING") {
                            break;
                        }
                        retry--;
                        if(retry < 0) {
                            throw new Exception("hosts never stared up");
                        }
                        Thread.Sleep(1000);
                    }
                    var services = core1.AtLocalHost.At("site", "services").With("limit", "all").With("apikey", Utils.Settings.ApiKey).GetAsync().Wait();
                    Assert.IsTrue(services.IsSuccessful, services.ToText());
                    var serviceToRestart = (from service in services.ToDocument()["service[status='enabled']"]
                                            let id = service["@id"].AsInt ?? 0
                                            where !string.IsNullOrEmpty(service["uri"].Contents) && id != 1
                                            select service).FirstOrDefault();
                    var signal = new ManualResetEvent(false);
                    var apihits = new List<Result<DreamMessage>>();
                    foreach(var core in new[] { core1, core2 }) {
                        var localCore = core;
                        apihits.Add(Async.ForkThread(() => {
                            var id = serviceToRestart["@id"].AsInt ?? 0;
                            signal.WaitOne();
                            DreamMessage response = null;
                            for(var k = 0; k < 20; k++) {
                                DreamMessage stopResponse = localCore.AtLocalHost.At("site", "services", id.ToString(), "stop").With("apikey", Utils.Settings.ApiKey).PostAsync().Wait();
                                if(stopResponse.IsSuccessful) {
                                    int retryStart = 3;
                                    while(retryStart > 0) {
                                        response = localCore.AtLocalHost.At("site", "services", id.ToString(), "start").With("apikey", Utils.Settings.ApiKey).PostAsync().Wait();
                                        if(response.IsSuccessful) {
                                            break;
                                        }
                                        retryStart--;
                                    }
                                }
                            }
                            return response;
                        },
                        new Result<DreamMessage>()));
                    }
                    signal.Set();
                    foreach(var r in apihits) {
                        r.Wait();
                        Assert.IsTrue(r.Value.IsSuccessful, r.Value.ToString());
                    }
                }
            }
        }

        [Test]
        public void Start_Stop_named_Instance() {
            Assert.AreEqual(0, Utils.Settings.Server.At("host").With("apikey", Utils.Settings.ApiKey).Get().ToDocument()["tenant"].ListLength);

            // Start instance
            var p = Utils.BuildPlugForAdmin();
            _log.Info("started default instance");

            var msg = Utils.Settings.Server.At("host").With("apikey", Utils.Settings.ApiKey).Get(new Result<DreamMessage>()).Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Failed to retrieve running instances");
            var tenants = msg.ToDocument();
            var tenant = tenants["tenant"];
            Assert.AreEqual(1, tenant.ListLength, "Unexpected instance(s):\r\n" + tenants.ToPrettyString());
            Assert.AreEqual(Utils.Settings.WikiId, tenant["@wikiid"].AsText ?? String.Empty, "Unexpected wiki id");
            Assert.AreEqual("running", tenant["@status"].AsText ?? String.Empty, "Unexpected status");

            // Shut down instance
            _log.Info("shutting down instance");
            msg = Utils.Settings.Server.At("host", "stop", Utils.Settings.WikiId).With("apikey", Utils.Settings.ApiKey).Post(new Result<DreamMessage>()).Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Failed to shutdown instance!");
            var doc = msg.ToDocument();
            Assert.AreEqual(Utils.Settings.WikiId, doc["@wikiid"].AsText ?? String.Empty, "Missing wikiId:\r\n" + doc.ToPrettyString());

            _log.Info("instance should be shut down");
            msg = p.At("host").With("apikey", Utils.Settings.ApiKey).Get(new Result<DreamMessage>()).Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Failed to retrieve running instances");
            tenants = msg.ToDocument();
            tenant = tenants["tenant"];
            Assert.AreEqual(0, tenant.ListLength, "Unexpected instance(s):\r\n" + tenants.ToPrettyString());
        }

        [Test]
        public void Stopping_instance_by_name_shuts_down_its_child_services() {
            Assert.AreEqual(0, Utils.Settings.Server.At("host").With("apikey", Utils.Settings.ApiKey).Get().ToDocument()["tenant"].ListLength);

            // Start instance
            var p = Utils.BuildPlugForAdmin();
            _log.Info("started default instance");

            var msg = Utils.Settings.Server.At("host").With("apikey", Utils.Settings.ApiKey).Get(new Result<DreamMessage>()).Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Failed to retrieve running instances");
            var tenants = msg.ToDocument();
            var tenant = tenants["tenant"];
            Assert.AreEqual(1, tenant.ListLength, "Unexpected instance(s):\r\n" + tenants.ToPrettyString());
            Assert.AreEqual(Utils.Settings.WikiId, tenant["@wikiid"].AsText ?? String.Empty, "Unexpected wiki id");
            Assert.AreEqual("running", tenant["@status"].AsText ?? String.Empty, "Unexpected status");
            var dekiServices = p.At("site", "services").With("type", "ext").Get().ToDocument();
            var allServices = Utils.Settings.HostInfo.LocalHost.At("host", "services").With("apikey", Utils.Settings.ApiKey).Get().ToDocument()["service"];
            var serviceUris = new HashSet<string>();
            foreach(var service in dekiServices["service"]) {
                var sid = service["sid"].AsText;
                var uri = service["uri"].AsText;
                if(allServices.Where(x => x["uri"].AsText == sid && x["sid"].AsText == uri).Any()) {
                    serviceUris.Add(uri);
                }
            }

            // Shut down instance
            _log.Info("shutting down instance");
            msg = Utils.Settings.Server.At("host", "stop", Utils.Settings.WikiId).With("apikey", Utils.Settings.ApiKey).Post(new Result<DreamMessage>()).Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Failed to shutdown instance!");
            var doc = msg.ToDocument();
            Assert.AreEqual(Utils.Settings.WikiId, doc["@wikiid"].AsText ?? String.Empty, "Missing wikiId:\r\n" + doc.ToPrettyString());

            _log.Info("instance should be shut down");
            msg = p.At("host").With("apikey", Utils.Settings.ApiKey).Get(new Result<DreamMessage>()).Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Failed to retrieve running instances");
            tenants = msg.ToDocument();
            tenant = tenants["tenant"];
            Assert.AreEqual(0, tenant.ListLength, "Unexpected instance(s):\r\n" + tenants.ToPrettyString());
            allServices = Utils.Settings.HostInfo.LocalHost.At("host", "services").With("apikey", Utils.Settings.ApiKey).Get().ToDocument()["service"];
            var stillRunning = (from service in allServices
                                let uri = service["uri"].AsText
                                where serviceUris.Contains(uri)
                                select uri)
                .ToArray();
            Assert.IsFalse(stillRunning.Any(), "child services still running: " + string.Join(", ", stillRunning));
            foreach(var service in dekiServices["service"]) {
                var sid = service["sid"].AsText;
                var uri = service["uri"].AsText;
                if(allServices.Where(x => x["uri"].AsText == sid && x["sid"].AsText == uri).Any()) {
                    serviceUris.Add(uri);
                }
            }
        }


        [Test]
        public void Start_Stop_Current_Instance() {
            Assert.AreEqual(0, Utils.Settings.Server.At("host").With("apikey", Utils.Settings.ApiKey).Get().ToDocument()["tenant"].ListLength);

            // Start instance
            var p = Utils.BuildPlugForAdmin();
            _log.Info("started default instance");
            var msg = p.At("host").With("apikey", Utils.Settings.ApiKey).Get(new Result<DreamMessage>()).Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Failed to retrieve running instances");
            var tenants = msg.ToDocument();
            var tenant = tenants["tenant"];
            Assert.AreEqual(1, tenant.ListLength, "Unexpected instance(s):\r\n" + tenants.ToPrettyString());
            Assert.AreEqual(Utils.Settings.WikiId, tenant["@wikiid"].AsText ?? String.Empty, "Unexpected wiki id");
            Assert.AreEqual("running", tenant["@status"].AsText ?? String.Empty, "Unexpected status");

            // Shut down instance
            _log.Info("shutting down instance");
            msg = p.At("host", "stop").Post(new Result<DreamMessage>()).Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Failed to shutdown instance!");
            var doc = msg.ToDocument();
            Assert.AreEqual(Utils.Settings.WikiId, doc["@wikiid"].AsText ?? String.Empty, "Missing wikiId:\r\n" + doc.ToPrettyString());

            _log.Info("instance should be shut down");
            msg = p.At("host").With("apikey", Utils.Settings.ApiKey).Get(new Result<DreamMessage>()).Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Failed to retrieve running instances");
            tenants = msg.ToDocument();
            tenant = tenants["tenant"];
            Assert.AreEqual(0, tenant.ListLength, "Unexpected instance(s):\r\n" + tenants.ToPrettyString());
        }

        [Test]
        public void Start_Stop_Current_Instance_without_instance_api_key() {
            Utils.Settings.SetupWithoutInstanceApiKey();
            Assert.AreEqual(0, Utils.Settings.Server.At("host").With("apikey", Utils.Settings.ApiKey).Get().ToDocument()["tenant"].ListLength);

            // Start instance
            var p = Utils.BuildPlugForAdmin();
            _log.Info("started default instance");
            var msg = p.At("host").With("apikey", Utils.Settings.ApiKey).Get(new Result<DreamMessage>()).Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Failed to retrieve running instances");
            var tenants = msg.ToDocument();
            var tenant = tenants["tenant"];
            Assert.AreEqual(1, tenant.ListLength, "Unexpected instance(s):\r\n" + tenants.ToPrettyString());
            Assert.AreEqual(Utils.Settings.WikiId, tenant["@wikiid"].AsText ?? String.Empty, "Unexpected wiki id");
            Assert.AreEqual("running", tenant["@status"].AsText ?? String.Empty, "Unexpected status");

            // Shut down instance
            _log.Info("shutting down instance");
            msg = p.At("host", "stop").Post(new Result<DreamMessage>()).Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Failed to shutdown instance!");
            var doc = msg.ToDocument();
            Assert.AreEqual(Utils.Settings.WikiId, doc["@wikiid"].AsText ?? String.Empty, "Missing wikiId:\r\n" + doc.ToPrettyString());

            _log.Info("instance should be shut down");
            msg = p.At("host").With("apikey", Utils.Settings.ApiKey).Get(new Result<DreamMessage>()).Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Failed to retrieve running instances");
            tenants = msg.ToDocument();
            tenant = tenants["tenant"];
            Assert.AreEqual(0, tenant.ListLength, "Unexpected instance(s):\r\n" + tenants.ToPrettyString());
            Utils.Settings.ShutdownHost();
        }

        [Test]
        public void Instance_expires() {
            using(var server = CreateTestServer()) {
                _log.Debug("--- creating multitenant service");
                var host = server.CreateMultiTenantService(5, 10);
                var p = host.WithInternalKey().AtLocalHost;
                var eventSubscriber = new XUri("mock://event/subscriber");
                var subForAuthorized = new XDoc("subscription-set")
                     .Elem("uri.owner", eventSubscriber)
                     .Start("subscription")
                         .Attr("id", "1")
                         .Elem("channel", "event://a/deki/site/stop")
                         .Start("recipient")
                            .Attr("authtoken", Utils.Settings.ApiKey)
                            .Elem("uri", eventSubscriber)
                        .End()
                     .End();
                _log.Debug("--- subscribing to instance shutdown event");
                var result = p.At("pubsub", "subscribers").With("apikey", server.HostInfo.ApiKey).PostAsync(subForAuthorized).Wait();
                Assert.IsTrue(result.IsSuccessful);
                MockPlug.Setup(eventSubscriber).Verb("POST").ExpectCalls(Times.Once());
                Assert.AreEqual(0, p.At("host").Get().ToDocument()["tenant"].ListLength);
                _log.Debug("--- gettings settings/forcing site init");
                Assert.IsTrue(p.At("site", "settings").WithHeader("X-Deki-Site", "id=a").Get().IsSuccessful);
                var msg = p.At("host").Get(new Result<DreamMessage>()).Wait();
                Assert.AreEqual(DreamStatus.Ok, msg.Status, "Failed to retrieve running instances");
                var tenants = msg.ToDocument();
                var tenant = tenants["tenant"];
                Assert.AreEqual(1, tenant.ListLength, "Unexpected instance(s):\r\n" + tenants.ToPrettyString());
                Assert.AreEqual("a", tenant["@wikiid"].AsText ?? String.Empty, "Unexpected wiki id");
                var status = tenant["@status"].AsText ?? "";
                Assert.Contains(status, new[] { "running", "starting_services" }, "Unexpected status: " + status);
                _log.Debug("---- wait for expire");
                MockPlug.VerifyAll(30.Seconds());
                Assert.IsTrue(
                    p.At("host").Get(new Result<DreamMessage>()).Wait().ToDocument()["tenant[@wikiid='a']"].IsEmpty,
                    "instance didn't time out"
                );
                _log.Debug("----- instance has shut down");
            }
        }

        [Test]
        public void Expired_instance_can_be_restarted() {
            using(var server = CreateTestServer()) {
                _log.Debug("--- creating multitenant service");
                var host = server.CreateMultiTenantService(5, 10);
                var hostPlug = host.WithInternalKey().AtLocalHost;
                Assert.AreEqual(0, hostPlug.At("host").Get().ToDocument()["tenant"].ListLength);
                Assert.IsTrue(hostPlug.At("site", "settings").WithHeader("X-Deki-Site", "id=a").Get().IsSuccessful);
                Assert.IsTrue(
                    Wait.For(() => hostPlug.At("host").Get(new Result<DreamMessage>()).Wait().ToDocument()["tenant[@wikiid='a']"].IsEmpty, 20.Seconds()),
                    "instance didn't time out");
                Assert.IsTrue(hostPlug.At("site", "settings").WithHeader("X-Deki-Site", "id=a").Get().IsSuccessful);
                var msg = hostPlug.At("host").Get(new Result<DreamMessage>()).Wait();
                Assert.AreEqual(DreamStatus.Ok, msg.Status, "Failed to retrieve running instances");
                var tenants = msg.ToDocument();
                var tenant = tenants["tenant"];
                Assert.AreEqual(1, tenant.ListLength, "Unexpected instance(s):\r\n" + tenants.ToPrettyString());
                Assert.AreEqual("a", tenant["@wikiid"].AsText ?? String.Empty, "Unexpected wiki id");
                var status = tenant["@status"].AsText ?? "";
                Assert.Contains(status, new[] { "running", "starting_services" }, "Unexpected status: " + status);
            }
        }

        [Test]
        public void Instance_limit_kicks_running_instance_in_favor_of_new() {
            using(var server = CreateTestServer()) {
                var host = server.CreateMultiTenantService(300, 2);
                var hostPlug = host.WithInternalKey().AtLocalHost;
                var eventSubscriber = new XUri("mock://event/subscriber");
                var subForAuthorized = new XDoc("subscription-set")
                     .Elem("uri.owner", eventSubscriber)
                     .Start("subscription")
                         .Attr("id", "1")
                         .Elem("channel", "event://a/deki/site/stop")
                         .Start("recipient")
                            .Attr("authtoken", Utils.Settings.ApiKey)
                            .Elem("uri", eventSubscriber)
                        .End()
                     .End();
                _log.Debug("--- subscribing to instance shutdown event");
                var result = hostPlug.At("pubsub", "subscribers").With("apikey", server.HostInfo.ApiKey).PostAsync(subForAuthorized).Wait();
                Assert.IsTrue(result.IsSuccessful);
                var resetEvent = new ManualResetEvent(false);
                MockPlug.Register(eventSubscriber, (p, v, u, r, r2) => {
                    resetEvent.Set();
                    r2.Return(DreamMessage.Ok());
                });
                Assert.AreEqual(0, hostPlug.At("host").Get().ToDocument()["tenant"].ListLength);
                _log.Debug("--- starting instance a");
                Assert.IsTrue(hostPlug.At("site", "settings").WithHeader("X-Deki-Site", "id=a").Get().IsSuccessful);
                _log.Debug("--- starting instance b");
                Assert.IsTrue(hostPlug.At("site", "settings").WithHeader("X-Deki-Site", "id=b").Get().IsSuccessful);
                _log.Debug("--- checking running instances");
                var msg = hostPlug.At("host").Get(new Result<DreamMessage>()).Wait();
                Assert.AreEqual(DreamStatus.Ok, msg.Status, "Failed to retrieve running instances");
                var tenants = msg.ToDocument();
                var tenant = tenants["tenant"];
                Assert.AreEqual(2, tenant.ListLength, "Unexpected instance(s):\r\n" + tenants.ToPrettyString());
                Assert.IsFalse(tenants["tenant[@wikiid='a']"].IsEmpty, "couldn't find instance 'a'");
                Assert.IsFalse(tenants["tenant[@wikiid='b']"].IsEmpty, "couldn't find instance 'b'");
                _log.Debug("--- starting instance c");
                msg = hostPlug.At("site", "settings").WithHeader("X-Deki-Site", "id=c").Get(new Result<DreamMessage>()).Wait();
                Assert.IsTrue(msg.IsSuccessful, msg.ToErrorString());
                _log.Debug("--- waiting for some instance to shut down");
                Assert.IsTrue(resetEvent.WaitOne(10.Seconds()), "no instance was shut down");
                msg = hostPlug.At("host").Get(new Result<DreamMessage>()).Wait();
                Assert.AreEqual(DreamStatus.Ok, msg.Status, "Failed to retrieve running instances");
                tenants = msg.ToDocument();
                tenant = tenants["tenant"];
                Assert.AreEqual(2, tenant.ListLength, "Unexpected instance(s):\r\n" + tenants.ToPrettyString());
                Assert.IsFalse(tenants["tenant[@wikiid='c']"].IsEmpty, "couldn't find instance 'c'");
            }
        }

        private static InstantTestHost CreateTestServer() {
            return new InstantTestHost();
        }

        public class InstantTestHost : IDisposable {
            public readonly DreamHostInfo HostInfo;

            public InstantTestHost() {
                HostInfo = DreamTestHelper.CreateRandomPortHost(new XDoc("config").Elem("apikey", Utils.Settings.ApiKey));
                HostInfo.Host.Self.At("load").With("name", "mindtouch.deki").Post(DreamMessage.Ok());
                HostInfo.Host.Self.At("load").With("name", "mindtouch.deki.services").Post(DreamMessage.Ok());
                HostInfo.Host.Self.At("load").With("name", "mindtouch.indexservice").Post(DreamMessage.Ok());
            }

            public void Dispose() {
                HostInfo.Dispose();
            }
            public DreamServiceInfo CreateCoreService() {
                var lucenePath = Path.Combine(Path.Combine(Path.GetTempPath(), StringUtil.CreateAlphaNumericKey(4)), "luceneindex");
                Directory.CreateDirectory(lucenePath);
                var dekiConfig = new XDoc("config")
                    .Elem("apikey", Utils.Settings.ApiKey)
                    .Elem("path", "deki")
                    .Elem("sid", "http://services.mindtouch.com/deki/draft/2006/11/dekiwiki")
                    .Elem("deki-path", Utils.Settings.DekiPath)
                    .Elem("imagemagick-convert-path", Utils.Settings.ImageMagickConvertPath)
                    .Elem("imagemagick-identify-path", Utils.Settings.ImageMagickIdentifyPath)
                    .Elem("princexml-path", Utils.Settings.PrinceXmlPath)
                    .Elem("deki-resources-path", Utils.Settings.DekiResourcesPath)
                    .Start("page-subscription")
                        .Elem("accumulation-time", "0")
                    .End()
                    .Start("packageupdater").Attr("uri", Utils.Settings.PackageUpdaterMockUri).End()
                    .Start("indexer").Attr("src", Utils.Settings.LuceneMockUri).End()
                    .Elem("uri.page-subscription", Utils.Settings.PageSubscriptionMockUri)
                    .Start("wikis")
                        .Start("config")
                            .Attr("id", "default")
                            .Elem("host", "*")
                            .Start("page-subscription")
                                .Elem("from-address", "foo@bar.com")
                            .End()
                            .Elem("db-server", Utils.Settings.DbServer)
                            .Elem("db-port", "3306")
                            .Elem("db-catalog", Utils.Settings.DbCatalog)
                            .Elem("db-user", Utils.Settings.DbUser)
                            .Start("db-password")
                                .Attr("hidden", "true").Value(Utils.Settings.DbPassword)
                            .End()
                            .Elem("db-options", "pooling=true; Connection Timeout=5; Protocol=socket; Min Pool Size=2; Max Pool Size=50; Connection Reset=false;character set=utf8;ProcedureCacheSize=25;Use Procedure Bodies=true;")
                        .End()
                    .End()
                    .Start("indexer")
                        .Elem("path.store", lucenePath)
                        .Elem("namespace-whitelist", "main, main_talk, user, user_talk")
                    .End();
                return DreamTestHelper.CreateService(HostInfo, dekiConfig);
            }

            public DreamServiceInfo CreateMultiTenantService(double instanceTimeout, int maxInstances) {
                var lucenePath = Path.Combine(Path.Combine(Path.GetTempPath(), StringUtil.CreateAlphaNumericKey(4)), "luceneindex");
                Directory.CreateDirectory(lucenePath);
                var dekiConfig = new XDoc("config")
                    .Elem("apikey", Utils.Settings.ApiKey)
                    .Elem("path", "deki")
                    .Elem("sid", "http://services.mindtouch.com/deki/draft/2006/11/dekiwiki")
                    .Elem("deki-path", Utils.Settings.DekiPath)
                    .Elem("imagemagick-convert-path", Utils.Settings.ImageMagickConvertPath)
                    .Elem("imagemagick-identify-path", Utils.Settings.ImageMagickIdentifyPath)
                    .Elem("princexml-path", Utils.Settings.PrinceXmlPath)
                    .Elem("deki-resources-path", Utils.Settings.DekiResourcesPath)
                    .Start("page-subscription")
                        .Elem("accumulation-time", "0")
                    .End()
                    .Start("packageupdater").Attr("uri", Utils.Settings.PackageUpdaterMockUri).End()
                    .Start("indexer").Attr("src", Utils.Settings.LuceneMockUri).End()
                    .Elem("uri.page-subscription", Utils.Settings.PageSubscriptionMockUri)
                    .Start("wikis")
                        .Attr("ttl", instanceTimeout)
                        .Attr("max", maxInstances)
                        .Attr("idletime", 0)
                        .Start("config")
                            .Attr("id", "a")
                            .Elem("host", "a")
                            .Start("page-subscription")
                                .Elem("from-address", "foo@bar.com")
                            .End()
                            .Elem("db-server", Utils.Settings.DbServer)
                            .Elem("db-port", "3306")
                            .Elem("db-catalog", Utils.Settings.DbCatalog)
                            .Elem("db-user", Utils.Settings.DbUser)
                            .Start("db-password").Attr("hidden", "true").Value(Utils.Settings.DbPassword).End()
                            .Elem("db-options", "pooling=true; Connection Timeout=5; Protocol=socket; Min Pool Size=2; Max Pool Size=50; Connection Reset=false;character set=utf8;ProcedureCacheSize=25;Use Procedure Bodies=true;")
                        .End()
                        .Start("config")
                            .Attr("id", "b")
                            .Elem("host", "b")
                            .Start("page-subscription")
                                .Elem("from-address", "foo@bar.com")
                            .End()
                            .Elem("db-server", Utils.Settings.DbServer)
                            .Elem("db-port", "3306")
                            .Elem("db-catalog", Utils.Settings.DbCatalog)
                            .Elem("db-user", Utils.Settings.DbUser)
                            .Start("db-password").Attr("hidden", "true").Value(Utils.Settings.DbPassword).End()
                            .Elem("db-options", "pooling=true; Connection Timeout=5; Protocol=socket; Min Pool Size=2; Max Pool Size=50; Connection Reset=false;character set=utf8;ProcedureCacheSize=25;Use Procedure Bodies=true;")
                        .End()
                        .Start("config")
                            .Attr("id", "c")
                            .Elem("host", "c")
                            .Start("page-subscription")
                                .Elem("from-address", "foo@bar.com")
                            .End()
                            .Elem("db-server", Utils.Settings.DbServer)
                            .Elem("db-port", "3306")
                            .Elem("db-catalog", Utils.Settings.DbCatalog)
                            .Elem("db-user", Utils.Settings.DbUser)
                            .Start("db-password").Attr("hidden", "true").Value(Utils.Settings.DbPassword).End()
                            .Elem("db-options", "pooling=true; Connection Timeout=5; Protocol=socket; Min Pool Size=2; Max Pool Size=50; Connection Reset=false;character set=utf8;ProcedureCacheSize=25;Use Procedure Bodies=true;")
                        .End()
                        .Start("indexer")
                            .Elem("path.store", lucenePath)
                            .Elem("namespace-whitelist", "main, main_talk, user, user_talk")
                        .End()
                    .End();
                return DreamTestHelper.CreateService(HostInfo, dekiConfig);
            }
        }
    }
}

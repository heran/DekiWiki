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
using System.Threading;
using Autofac.Builder;
using MindTouch.Deki.Data.UserSubscription;
using MindTouch.Deki.UserSubscription;
using MindTouch.Dream;
using MindTouch.Dream.Test;
using MindTouch.Extensions.Time;
using MindTouch.Tasking;
using MindTouch.Xml;
using Moq;
using PlugTimes = MindTouch.Dream.Test.Mock.Times;
using NUnit.Framework;

namespace MindTouch.Deki.Tests.ChangeSubscriptionTests {

    [TestFixture]
    public class DekiChangeSubscriptionTests {

        private static readonly log4net.ILog _log = LogUtils.CreateLog();

        private DreamHostInfo _hostInfo;
        private Mock<IPageSubscriptionDataSession> _sessionMock;
        private Mock<IPageSubscriptionInstance> _instanceMock;
        private string _apikey = "abc";
        private XUri _deki = new XUri("http://mock/deki");
        private XUri _email = new XUri("http://mock/email").With("apikey", "123");
        private XUri _subscribe = new XUri("http://mock/sub");
        private XUri _storage = new XUri("http://mock/store");


        [SetUp]
        public void PerTestSetup() {
            _instanceMock = new Mock<IPageSubscriptionInstance>();
            InitSession();
            var builder = new ContainerBuilder();
            builder.Register(_instanceMock.Object).As<IPageSubscriptionInstance>();
            _hostInfo = DreamTestHelper.CreateRandomPortHost(new XDoc("config").Elem("apikey", Utils.Settings.ApiKey), builder.Build());
        }

        private void InitSession() {
            _sessionMock = new Mock<IPageSubscriptionDataSession>();
        }

        [TearDown]
        public void PerTestCleanup() {
            MockPlug.DeregisterAll();
            _hostInfo = null;
            _log.Debug("cleaned up");
        }

        [Test]
        public void Initialize_service_with_persisted_subscriptions() {
            XUri email = new XUri("http://mock/email");
            XUri deki = new XUri("http://mock/deki");
            MockPlug.Register(deki, delegate(Plug p, string v, XUri u, DreamMessage r, Result<DreamMessage> r2) {
                _log.DebugFormat("deki: {0}", u);
                DreamMessage msg = DreamMessage.Ok(new XDoc("user")
                    .Attr("id", "1")
                    .Elem("email", "a@b.com")
                    .Start("permissions.user")
                        .Elem("operations", "READ,SUBSCRIBE,LOGIN")
                    .End());
                msg.Headers.Add("X-Deki-Site", "id=wicked");
                r2.Return(msg);
            });
            XUri subscribe = new XUri("http://mock/sub");
            int subscribeCalled = 0;
            MockPlug.Register(subscribe, delegate(Plug p, string v, XUri u, DreamMessage r, Result<DreamMessage> r2) {
                _log.DebugFormat("subscribe: {0}", u);
                subscribeCalled++;
                DreamMessage msg = DreamMessage.Ok(new XDoc("foo"));
                msg.Headers.Location = subscribe;
                r2.Return(msg);
            });
            XUri storage = new XUri("http://mock/store");
            var serviceInfo = DreamTestHelper.CreateService(
                _hostInfo,
                typeof(DekiChangeSubscriptionService),
                "email",
                new XDoc("config")
                    .Elem("uri.emailer", email)
                    .Elem("uri.deki", deki)
                    .Elem("uri.pubsub", subscribe)
                    .Elem("uri.storage", storage)
                );
            Assert.AreEqual(1, subscribeCalled);
            _sessionMock.Verify(x => x.GetSubscriptionsForUser(It.IsAny<uint>(), It.IsAny<List<uint>>()), Times.Never());
            _sessionMock.Verify(x => x.Subscribe(It.IsAny<uint>(), It.IsAny<uint>(), It.IsAny<bool>()), Times.Never());
            _sessionMock.Verify(x => x.UnsubscribeUser(It.IsAny<uint>(), It.IsAny<uint?>()), Times.Never());
            InitSession();
            _log.Debug("get all subscriptions");
            _instanceMock.Setup(x => x.GetUserInfo(1)).Returns(new PageSubscriptionUser(1));
            _instanceMock.Setup(x => x.CreateDataSession()).Returns(_sessionMock.Object);
            _sessionMock.Setup(x => x.GetSubscriptionsForUser(1, It.Is<IEnumerable<uint>>(y => y.Count() == 0)))
                .Returns(new List<PageSubscriptionBE> {
                    new PageSubscriptionBE() {PageId = 10, UserId = 1, IncludeChildPages = false},
                    new PageSubscriptionBE() {PageId = 20, UserId = 1, IncludeChildPages = true}
                }).AtMostOnce().Verifiable();
            var storageMock = MockPlug.Setup(storage)
                .Verb("GET")
                .At("subscriptions", "wicked")
                .ExpectCalls(PlugTimes.Once());
            var response = serviceInfo.AtLocalHost
                .At("subscriptions")
                .WithHeader("X-Deki-Site", "id=wicked")
                .GetAsync()
                .Wait();
            Assert.IsTrue(response.IsSuccessful);
            _instanceMock.Verify(x => x.CreateDataSession(), Times.Once());
            _sessionMock.VerifyAll();
            storageMock.Verify();
            var subscriptions = response.ToDocument();
            var sub = subscriptions["subscription.page"];
            Assert.AreEqual(2, sub.ListLength);
            Assert.AreEqual("0", sub.Where(x => x["@id"].AsText == "10").Select(x => x["@depth"].AsText).First());
            Assert.AreEqual("infinity", sub.Where(x => x["@id"].AsText == "20").Select(x => x["@depth"].AsText).First());
        }

        [Test]
        public void Initialize_service_with_legacy_subscriptions_migrates_to_db() {
            XUri email = new XUri("http://mock/email");
            XUri deki = new XUri("http://mock/deki");
            MockPlug.Register(deki, delegate(Plug p, string v, XUri u, DreamMessage r, Result<DreamMessage> r2) {
                _log.DebugFormat("deki: {0}", u);
                DreamMessage msg = DreamMessage.Ok(new XDoc("user")
                    .Attr("id", "1")
                    .Elem("email", "a@b.com")
                    .Start("permissions.user")
                        .Elem("operations", "READ,SUBSCRIBE,LOGIN")
                    .End());
                msg.Headers.Add("X-Deki-Site", "id=wicked");
                r2.Return(msg);
            });
            XUri subscribe = new XUri("http://mock/sub");
            int subscribeCalled = 0;
            MockPlug.Register(subscribe, delegate(Plug p, string v, XUri u, DreamMessage r, Result<DreamMessage> r2) {
                _log.DebugFormat("subscribe: {0}", u);
                subscribeCalled++;
                DreamMessage msg = DreamMessage.Ok(new XDoc("foo"));
                msg.Headers.Location = subscribe;
                r2.Return(msg);
            });
            XUri storage = new XUri("http://mock/store");
            var serviceInfo = DreamTestHelper.CreateService(
                _hostInfo,
                typeof(DekiChangeSubscriptionService),
                "email",
                new XDoc("config")
                    .Elem("uri.emailer", email)
                    .Elem("uri.deki", deki)
                    .Elem("uri.pubsub", subscribe)
                    .Elem("uri.storage", storage)
                    .Start("components")
                        .Start("component")
                            .Attr("scope", "service")
                            .Attr("type", "MindTouch.Deki.Data.MySql.UserSubscription.MySqlPageSubscriptionSessionFactory, mindtouch.deki.data.mysql")
                        .End()
                    .End()
                );
            Assert.AreEqual(1, subscribeCalled);
            _sessionMock.Verify(x => x.GetSubscriptionsForUser(It.IsAny<uint>(), It.IsAny<List<uint>>()), Times.Never());
            _sessionMock.Verify(x => x.Subscribe(It.IsAny<uint>(), It.IsAny<uint>(), It.IsAny<bool>()), Times.Never());
            _sessionMock.Verify(x => x.UnsubscribeUser(It.IsAny<uint>(), It.IsAny<uint?>()), Times.Never());
            _log.Debug("get all subscriptions");
            InitSession();
            _instanceMock.Setup(x => x.GetUserInfo(1)).Returns(new PageSubscriptionUser(1));
            _instanceMock.Setup(x => x.CreateDataSession()).Returns(_sessionMock.Object);
            _sessionMock.Setup(x => x.Subscribe(1, 10, false)).AtMostOnce().Verifiable();
            _sessionMock.Setup(x => x.Subscribe(1, 20, true)).AtMostOnce().Verifiable();
            _sessionMock.Setup(x => x.GetSubscriptionsForUser(1, It.Is<IEnumerable<uint>>(y => y.Count() == 0)))
                .Returns(new List<PageSubscriptionBE> {
                    new PageSubscriptionBE() {PageId = 10, UserId = 1, IncludeChildPages = false},
                    new PageSubscriptionBE() {PageId = 20, UserId = 1, IncludeChildPages = true}
                }).AtMostOnce().Verifiable();
            MockPlug.Setup(storage)
                .Verb("GET")
                .At("subscriptions", "wicked")
                .Returns(new XDoc("files")
                    .Start("file").Elem("name", "user_1.xml").End()
                    .Start("file").Elem("name", "bar.txt").End())
                .ExpectCalls(PlugTimes.Once());
            MockPlug.Setup(storage)
                .Verb("GET")
                .At("subscriptions", "wicked", "user_1.xml")
                .Returns(new XDoc("user")
                    .Attr("userid", 1)
                    .Elem("email", "foo")
                    .Start("subscription.page").Attr("id", 10).Attr("depth", 0).End()
                    .Start("subscription.page").Attr("id", 20).Attr("depth", "infinity").End())
                .ExpectCalls(PlugTimes.Once());
            MockPlug.Setup(storage)
              .Verb("DELETE")
              .At("subscriptions", "wicked")
              .ExpectCalls(PlugTimes.Once());
            var response = serviceInfo.AtLocalHost
                .At("subscriptions")
                .WithHeader("X-Deki-Site", "id=wicked")
                .GetAsync()
                .Wait();
            Assert.IsTrue(response.IsSuccessful);
            _instanceMock.Verify(x => x.CreateDataSession(), Times.Once());
            _sessionMock.VerifyAll();
            MockPlug.VerifyAll();
            var subscriptions = response.ToDocument();
            var sub = subscriptions["subscription.page"];
            Assert.AreEqual(2, sub.ListLength);
            Assert.AreEqual("0", sub.Where(x => x["@id"].AsText == "10").Select(x => x["@depth"].AsText).First());
            Assert.AreEqual("infinity", sub.Where(x => x["@id"].AsText == "20").Select(x => x["@depth"].AsText).First());
        }

        [Test]
        public void Request_without_valid_user_headers_results_in_not_authorized() {

            // set up mocks for all the support service calls
            XUri deki = new XUri("http://mock/deki");
            MockPlug.Register(deki, delegate(Plug p, string v, XUri u, DreamMessage r, Result<DreamMessage> r2) {
                _log.DebugFormat("deki: {0}", u);
                r2.Return(DreamMessage.AccessDenied("deki", "bad puppy"));
            });
            XUri subscribe = new XUri("http://mock/sub");
            MockPlug.Register(subscribe, delegate(Plug p, string v, XUri u, DreamMessage r, Result<DreamMessage> r2) {
                if(u == subscribe.At("subscribers")) {
                    _log.Debug("creating subscription");
                    DreamMessage msg = DreamMessage.Ok(new XDoc("x"));
                    msg.Headers.Location = subscribe.At("testsub");
                    r2.Return(msg);
                } else {
                    _log.Debug("updating subscription");
                    r2.Return(DreamMessage.Ok());
                }
            });
            XUri storage = new XUri("http://mock/store");
            MockPlug.Register(storage, delegate(Plug p, string v, XUri u, DreamMessage r, Result<DreamMessage> r2) {
                _log.DebugFormat("storage: {0}", u);
                r2.Return(DreamMessage.Ok(new XDoc("foo")));
            });

            // set up service
            _log.Debug("set up service");
            DreamServiceInfo serviceInfo = DreamTestHelper.CreateService(
                _hostInfo,
                typeof(DekiChangeSubscriptionService),
                "email",
                new XDoc("config")
                    .Elem("uri.emailer", new XUri("http://mock/email"))
                    .Elem("uri.deki", deki)
                    .Elem("uri.pubsub", subscribe)
                    .Elem("uri.storage", storage)
                );
            Plug service = serviceInfo.WithInternalKey().AtLocalHost;

            // post a subscription
            _log.Debug("post page 10 subscription");
            InitSession();
            _instanceMock.Setup(x => x.GetUserInfo(1)).Returns(new PageSubscriptionUser(1));
            _instanceMock.Setup(x => x.CreateDataSession()).Returns(_sessionMock.Object);
            var response = service
                .At("pages", "10")
                .With("depth", "infinity")
                .WithHeader("X-Deki-Site", "id=wicked")
                .PostAsync()
                .Wait();
            Assert.IsFalse(response.IsSuccessful);
            Assert.AreEqual(DreamStatus.Unauthorized, response.Status);
        }

        [Test]
        public void Request_for_user_without_email_results_in_special_bad_request() {

            // set up mocks for all the support service calls
            XUri deki = new XUri("http://mock/deki");
            MockPlug.Register(deki, delegate(Plug p, string v, XUri u, DreamMessage r, Result<DreamMessage> r2) {
                _log.DebugFormat("deki: {0}", u);
                DreamMessage msg = DreamMessage.Ok(new XDoc("user")
                    .Attr("id", "1")
                    .Elem("email", "")
                    .Start("permissions.user")
                        .Elem("operations", "READ,SUBSCRIBE,LOGIN")
                    .End());
                msg.Headers.Add("X-Deki-Site", "id=wicked");
                r2.Return(msg);
            });
            XUri subscribe = new XUri("http://mock/sub");
            MockPlug.Register(subscribe, delegate(Plug p, string v, XUri u, DreamMessage r, Result<DreamMessage> r2) {
                if(u == subscribe.At("subscribers")) {
                    _log.Debug("creating subscription");
                    DreamMessage msg = DreamMessage.Ok(new XDoc("x"));
                    msg.Headers.Location = subscribe.At("testsub");
                    r2.Return(msg);
                } else {
                    _log.Debug("updating subscription");
                    r2.Return(DreamMessage.Ok());
                }
            });
            XUri storage = new XUri("http://mock/store");
            MockPlug.Register(storage, delegate(Plug p, string v, XUri u, DreamMessage r, Result<DreamMessage> r2) {
                _log.DebugFormat("storage: {0}", u);
                r2.Return(DreamMessage.Ok(new XDoc("foo")));
            });

            // set up service
            _log.Debug("set up service");
            _instanceMock.Setup(x => x.GetUserInfo(1)).Returns(new PageSubscriptionUser(1));
            DreamServiceInfo serviceInfo = DreamTestHelper.CreateService(
                _hostInfo,
                typeof(DekiChangeSubscriptionService),
                "email",
                new XDoc("config")
                    .Elem("uri.emailer", new XUri("http://mock/email"))
                    .Elem("uri.deki", deki)
                    .Elem("uri.pubsub", subscribe)
                    .Elem("uri.storage", storage)
                );
            Plug service = serviceInfo.WithInternalKey().AtLocalHost;

            // post a subscription
            _log.Debug("post page 10 subscription");
            DreamMessage response = service
                .At("pages", "10")
                .With("depth", "infinity")
                .WithHeader("X-Deki-Site", "id=wicked")
                .PostAsync()
                .Wait();
            Assert.IsFalse(response.IsSuccessful);
            Assert.AreEqual(DreamStatus.BadRequest, response.Status);
            Assert.AreEqual("no email for user", response.ToDocument()["message"].AsText);
        }

        [Test]
        public void Subscribe_for_page_without_subscribe_effective_permissions_results_in_forbidden() {
            uint userId = 15;
            uint pageId = 10;

            // set up service
            var service = SetupService();
            _log.Debug("get some subscriptions");
            _instanceMock.Setup(x => x.GetUserInfo(1)).Returns(new PageSubscriptionUser(1)).Verifiable();

            InitSession();
            _instanceMock.Setup(x => x.GetUserInfo(userId)).Returns(new PageSubscriptionUser(userId));
            _instanceMock.SetupGet(x => x.WikiId).Returns("wicked");
            ExpectCurrentUserQuery(userId);
            MockPlug.Setup(_deki)
                .At("pages", pageId.ToString(), "allowed")
                .With("permissions", "read,subscribe")
                .WithHeader("X-Deki-Site", "id=wicked")
                .Returns(new XDoc("users").Start("user").Attr("id", 43).End())
                .ExpectAtLeastOneCall();
            InstanceExpectations();
            var response = service
                .At("pages", pageId.ToString())
                .With("depth", "infinity")
                .WithHeader("X-Deki-Site", "id=wicked")
                .PostAsync()
                .Wait();
            Assert.IsFalse(response.IsSuccessful);
            Assert.AreEqual(DreamStatus.Forbidden, response.Status);
            _instanceMock.Verify(x => x.CreateDataSession(), Times.Never());
            MockPlug.VerifyAll(10.Seconds());
            MockPlug.DeregisterAll();
        }

        [Test]
        public void Username_is_used_in_email_address() {
            var service = SetupService();
            PostSubscription(service, 1, 10, true);

            // post a page event
            _log.Debug("posting a page event");
            string channel = "event://wicked/deki/pages/update";
            MockPlug.Setup(_deki)
                .Verb("GET")
                .At("pages", "10")
                .With("apikey", _apikey)
                .WithHeader("X-Deki-Site", "id=wicked")
                .Returns(new XDoc("page")
                    .Elem("uri.ui", "http://foo.com/@api/deki/pages/10")
                    .Elem("title", "foo")
                    .Elem("path", "foo/bar")
                    .Start("page.parent")
                        .Attr("id", 8)
                    .End())
                .ExpectCalls(PlugTimes.Exactly(2));
            MockPlug.Setup(_deki)
                .Verb("GET")
                .At("pages", "10", "feed")
                .With("redirects", "0")
                .With("format", "raw")
                .WithHeader("X-Deki-Site", "id=wicked")
                .Returns(new XDoc("table")
                    .Start("change")
                        .Elem("rc_summary", "Two edits")
                        .Elem("rc_comment", "edit 1")
                        .Elem("rc_comment", "edit 2")
                    .End())
                .ExpectAtLeastOneCall();
            MockPlug.Setup(_email)
                .WithMessage(m => m.ToDocument()["to"].AsText.EqualsInvariant("\"bob\" <a@b.com>"))
                .ExpectAtLeastOneCall();
            MockPlug.Setup(_deki)
                .Verb("GET")
                .At("users", "1")
                .WithHeader("X-Deki-Site", "id=wicked")
                .Returns(new XDoc("user")
                    .Attr("id", "1")
                    .Elem("email", "a@b.com")
                    .Elem("username", "bob")
                    .Elem("language", "en")
                    .Start("permissions.user")
                        .Elem("operations", "READ,SUBSCRIBE,LOGIN")
                    .End())
                .ExpectAtLeastOneCall();
            MockPlug.Setup(_deki)
                .Verb("POST")
                 .At("pages", "10", "allowed")
                 .With("permissions", "read,subscribe")
                 .Returns(new XDoc("users").Start("user").Attr("id", 1).End())
                 .ExpectAtLeastOneCall();
            InitSession();
            _instanceMock.Setup(x => x.GetUserInfo(1)).Returns(new PageSubscriptionUser(1));
            _instanceMock.Setup(x => x.CreateDataSession()).Returns(_sessionMock.Object);
            _instanceMock.SetupGet(x => x.WikiId).Returns("wicked");
            _instanceMock.SetupGet(x => x.IsValid).Returns(true);
            _sessionMock.Setup(x => x.GetSubscriptionsForPages(It.Is<IEnumerable<uint>>(y => AreEqual(y, new uint[] { 10, 8 }))))
                .Returns(new List<PageSubscriptionBE> {
                    new PageSubscriptionBE() {PageId = 10, UserId = 1, IncludeChildPages = false},
                }).AtMostOnce().Verifiable();

            var response = service.At("notify")
                .WithHeader(DreamHeaders.DREAM_EVENT_CHANNEL, channel)
                .PostAsync(CreateDekiEvent().Elem("channel", channel).Elem("pageid", 10))
                .Wait();
            Assert.IsTrue(response.IsSuccessful);

            // expect:
            // - email service is called for user
            _sessionMock.VerifyAll();
            MockPlug.VerifyAll(10.Seconds());
        }

        [Test]
        public void Username_can_be_surpressed_in_email_address() {
            var service = SetupService();
            PostSubscription(service, 1, 10, true);

            // post a page event
            _log.Debug("posting a page event");
            string channel = "event://wicked/deki/pages/update";
            MockPlug.Setup(_deki)
                .Verb("GET")
                .At("pages", "10")
                .WithHeader("X-Deki-Site", "id=wicked")
                .Returns(new XDoc("page")
                    .Elem("uri.ui", "http://foo.com/@api/deki/pages/10")
                    .Elem("title", "foo")
                    .Elem("path", "foo/bar")
                    .Start("page.parent")
                        .Attr("id", 8)
                    .End())
                .ExpectCalls(PlugTimes.Exactly(2));
            MockPlug.Setup(_deki)
                .Verb("GET")
                .At("pages", "10", "feed")
                .With("redirects", "0")
                .With("format", "raw")
                .WithHeader("X-Deki-Site", "id=wicked")
                .Returns(new XDoc("table")
                    .Start("change")
                        .Elem("rc_summary", "Two edits")
                        .Elem("rc_comment", "edit 1")
                        .Elem("rc_comment", "edit 2")
                    .End())
                .ExpectAtLeastOneCall();
            MockPlug.Setup(_email)
                .WithMessage(m => m.ToDocument()["to"].AsText.EqualsInvariant("a@b.com"))
                .ExpectAtLeastOneCall();
            MockPlug.Setup(_deki)
                .Verb("GET")
                .At("users", "1")
                .WithHeader("X-Deki-Site", "id=wicked")
                .Returns(new XDoc("user")
                    .Attr("id", "1")
                    .Elem("email", "a@b.com")
                    .Elem("username", "bob")
                    .Elem("language", "en")
                    .Start("permissions.user")
                        .Elem("operations", "READ,SUBSCRIBE,LOGIN")
                    .End())
                .ExpectAtLeastOneCall();
            MockPlug.Setup(_deki)
                .Verb("POST")
                 .At("pages", "10", "allowed")
                 .With("permissions", "read,subscribe")
                 .Returns(new XDoc("users").Start("user").Attr("id", 1).End())
                 .ExpectAtLeastOneCall();
            InitSession();
            _instanceMock.Setup(x => x.GetUserInfo(1)).Returns(new PageSubscriptionUser(1));
            _instanceMock.Setup(x => x.CreateDataSession()).Returns(_sessionMock.Object);
            _instanceMock.SetupGet(x => x.WikiId).Returns("wicked");
            _instanceMock.SetupGet(x => x.IsValid).Returns(true);
            _instanceMock.SetupGet(x => x.UseShortEmailAddress).Returns(true);
            _sessionMock.Setup(x => x.GetSubscriptionsForPages(It.Is<IEnumerable<uint>>(y => AreEqual(y, new uint[] { 10, 8 }))))
                .Returns(new List<PageSubscriptionBE> {
                    new PageSubscriptionBE() {PageId = 10, UserId = 1, IncludeChildPages = false},
                }).AtMostOnce().Verifiable();

            var response = service.At("notify")
                .WithHeader(DreamHeaders.DREAM_EVENT_CHANNEL, channel)
                .PostAsync(CreateDekiEvent().Elem("channel", channel).Elem("pageid", 10))
                .Wait();
            Assert.IsTrue(response.IsSuccessful);

            // expect:
            // - email service is called for user
            _sessionMock.VerifyAll();
            MockPlug.VerifyAll(10.Seconds());
        }

        [Test]
        public void Dispatch_to_multiple_emails() {
            var service = SetupService();
            PostSubscription(service, 1, 10, true);
            PostSubscription(service, 2, 10, true);

            // post a page event
            string channel = "event://wicked/deki/pages/update";
            MockPlug.Setup(_deki)
                .Verb("GET")
                .At("pages", "10")
                .WithHeader("X-Deki-Site", "id=wicked")
                .Returns(new XDoc("page")
                    .Elem("uri.ui", "http://foo.com/@api/deki/pages/10")
                    .Elem("title", "foo")
                    .Elem("path", "foo/bar")
                    .Start("page.parent")
                        .Attr("id", 8)
                    .End())
                .ExpectCalls(PlugTimes.Exactly(2));
            MockPlug.Setup(_deki)
                .Verb("GET")
                .At("pages", "10", "feed")
                .With("redirects", "0")
                .With("format", "raw")
                .WithHeader("X-Deki-Site", "id=wicked")
                .Returns(new XDoc("table")
                             .Start("change")
                             .Elem("rc_summary", "Two edits")
                             .Elem("rc_comment", "edit 1")
                             .Elem("rc_comment", "edit 2")
                             .End())
                .ExpectAtLeastOneCall();
            var emails = new Dictionary<string, XDoc>();
            MockPlug.Setup(_email)
                .WithMessage(m => {
                    var doc = m.ToDocument();
                    emails[doc["to"].AsText] = doc;
                    return true;
                })
                .ExpectCalls(PlugTimes.Exactly(2));
            MockPlug.Setup(_deki)
                .Verb("POST")
                 .At("pages", "10", "allowed")
                 .With("permissions", "read,subscribe")
                 .WithMessage(m => m.ToDocument()["user/@id"].ListLength == 2)
                 .Returns(new XDoc("users").Start("user").Attr("id", 1).End().Start("user").Attr("id", 2).End())
                 .ExpectAtLeastOneCall();
            InitSession();
            _instanceMock.Setup(x => x.GetUserInfo(1)).Returns(new PageSubscriptionUser(1) { Email = "email1" });
            _instanceMock.Setup(x => x.GetUserInfo(2)).Returns(new PageSubscriptionUser(2) { Email = "email2" });
            _instanceMock.Setup(x => x.CreateDataSession()).Returns(_sessionMock.Object);
            _instanceMock.SetupGet(x => x.WikiId).Returns("wicked");
            _instanceMock.SetupGet(x => x.Sitename).Returns("Test Site");
            _instanceMock.SetupGet(x => x.IsValid).Returns(true);
            _instanceMock.SetupGet(x => x.Culture).Returns(CultureUtil.GetNonNeutralCulture("en-us"));
            _sessionMock.Setup(x => x.GetSubscriptionsForPages(It.Is<IEnumerable<uint>>(y => AreEqual(y, new uint[] { 10, 8 }))))
                .Returns(new List<PageSubscriptionBE> {
                    new PageSubscriptionBE() {PageId = 10, UserId = 1, IncludeChildPages = true},
                    new PageSubscriptionBE() {PageId = 10, UserId = 2, IncludeChildPages = true},
                }).AtMostOnce().Verifiable();

            var response = service.At("notify")
                .WithHeader(DreamHeaders.DREAM_EVENT_CHANNEL, channel)
                .PostAsync(CreateDekiEvent().Elem("channel", channel).Elem("pageid", 10))
                .Wait();
            Assert.IsTrue(response.IsSuccessful);

            // expect:
            // - email service is called for both users
            _sessionMock.VerifyAll();
            MockPlug.VerifyAll(10.Seconds());

            Assert.IsTrue(emails.ContainsKey("email1"), "no email1");
            Assert.IsTrue(emails.ContainsKey("email2"), "no email2");
            var emailDoc = emails["email1"];
            var para = emailDoc["body[@html='true']/p"];
            Assert.AreEqual("http://foo.com/@api/deki/pages/10", para["b/a/@href"].AsText);
            para = para.Next;
            Assert.AreEqual("<li>edit 1 ( <a href=\"http://foo.com/@api/deki/pages/10?revision\">Mon, 01 Jan 0001 00:00:00 GMT</a> by <a href=\"http://foo.com/User%3A\" /> )</li>", para["ol/li"].ToString());
        }

        [Test]
        public void Dispatch_indirect_subscriptions() {
            var service = SetupService();
            PostSubscription(service, 1, 10, true);
            PostSubscription(service, 2, 12, true);
            PostSubscription(service, 3, 14, false);

            // post a page event
            string channel = "event://wicked/deki/pages/update";
            MockPlug.Setup(_deki)
                .Verb("GET")
                .At("pages", "15")
                .WithHeader("X-Deki-Site", "id=wicked")
                .With("apikey",_apikey)
                .Returns(new XDoc("page")
                    .Elem("uri.ui", "http://foo.com/@api/deki/pages/15")
                    .Elem("title", "foo")
                    .Elem("path", "foo/bar")
                    .Start("page.parent")
                        .Attr("id", 14)
                        .Start("page.parent")
                            .Attr("id", 12)
                            .Start("page.parent")
                                .Attr("id", 10)
                            .End()
                        .End()
                    .End())
                .ExpectCalls(PlugTimes.Exactly(2));
            MockPlug.Setup(_deki)
                .Verb("GET")
                .At("pages", "15", "feed")
                .With("redirects", "0")
                .With("format", "raw")
                .WithHeader("X-Deki-Site", "id=wicked")
                .Returns(new XDoc("table")
                             .Start("change")
                             .Elem("rc_summary", "Two edits")
                             .Elem("rc_comment", "edit 1")
                             .Elem("rc_comment", "edit 2")
                             .End())
                .ExpectAtLeastOneCall();
            var emails = new Dictionary<string, XDoc>();
            MockPlug.Setup(_email)
                .WithMessage(m => {
                    var doc = m.ToDocument();
                    emails[doc["to"].AsText] = doc;
                    return true;
                })
                .ExpectCalls(PlugTimes.Exactly(2));
            MockPlug.Setup(_deki)
                .Verb("POST")
                .At("pages", "15", "allowed")
                .With("permissions", "read,subscribe")
                .With("apikey",_apikey)
                .WithHeader("X-Deki-Site", "id=wicked")
                .WithMessage(m => m.ToDocument()["user/@id"].ListLength == 2)
                .Returns(new XDoc("users").Start("user").Attr("id", 1).End().Start("user").Attr("id", 2).End())
                .ExpectAtLeastOneCall();
            InitSession();
            _instanceMock.Setup(x => x.GetUserInfo(1)).Returns(new PageSubscriptionUser(1) { Email = "email1" });
            _instanceMock.Setup(x => x.GetUserInfo(2)).Returns(new PageSubscriptionUser(2) { Email = "email2" });
            _instanceMock.Setup(x => x.GetUserInfo(3)).Returns(new PageSubscriptionUser(3) { Email = "email3" });
            _instanceMock.Setup(x => x.CreateDataSession()).Returns(_sessionMock.Object);
            _instanceMock.SetupGet(x => x.WikiId).Returns("wicked");
            _instanceMock.SetupGet(x => x.Sitename).Returns("Test Site");
            _instanceMock.SetupGet(x => x.IsValid).Returns(true);
            _instanceMock.SetupGet(x => x.Culture).Returns(CultureUtil.GetNonNeutralCulture("en-us"));
            _sessionMock.Setup(x => x.GetSubscriptionsForPages(It.Is<IEnumerable<uint>>(y => AreEqual(y, new uint[] { 15, 14, 12, 10 }))))
                .Returns(new List<PageSubscriptionBE> {
                    new PageSubscriptionBE() {PageId = 10, UserId = 1, IncludeChildPages = true},
                    new PageSubscriptionBE() {PageId = 12, UserId = 2, IncludeChildPages = true},
                    new PageSubscriptionBE() {PageId = 14, UserId = 3, IncludeChildPages = false},
                }).AtMostOnce().Verifiable();

            var response = service.At("notify")
                .WithHeader(DreamHeaders.DREAM_EVENT_CHANNEL, channel)
                .PostAsync(CreateDekiEvent().Elem("channel", channel).Elem("pageid", 15))
                .Wait();
            Assert.IsTrue(response.IsSuccessful);

            // expect:
            // - email service is called for both users
            _sessionMock.VerifyAll();
            MockPlug.VerifyAll(10.Seconds());

            Assert.IsTrue(emails.ContainsKey("email1"), "no email1");
            Assert.IsTrue(emails.ContainsKey("email2"), "no email2");
            var emailDoc = emails["email1"];
            var para = emailDoc["body[@html='true']/p"];
            Assert.AreEqual("http://foo.com/@api/deki/pages/15", para["b/a/@href"].AsText);
            para = para.Next;
            Assert.AreEqual("<li>edit 1 ( <a href=\"http://foo.com/@api/deki/pages/15?revision\">Mon, 01 Jan 0001 00:00:00 GMT</a> by <a href=\"http://foo.com/User%3A\" /> )</li>", para["ol/li"].ToString());
        }

        [Test]
        public void User_update_invalidates_user() {
            var service = SetupService();
            _log.Debug("posting a user update event");
            var user = new PageSubscriptionUser(1) { Email = "email1" };
            Assert.IsTrue(user.IsValid);
            _instanceMock.Setup(x => x.GetUserInfo(1)).Returns(user).Verifiable();
            InstanceExpectations();
            var channel = "event://wicked/deki/users/update";
            var response = service.At("updateuser")
                .WithHeader(DreamHeaders.DREAM_EVENT_CHANNEL, channel)
                .PostAsync(CreateDekiEvent().Elem("channel", channel).Elem("userid", 1))
                .Wait();
            Assert.IsTrue(response.IsSuccessful);
            _instanceMock.VerifyAll();
            Assert.IsFalse(user.IsValid);
        }

        [Test]
        public void Can_remove_subscription() {
            var service = SetupService();
            _log.Debug("remove a subscription");
            _instanceMock.Setup(x => x.GetUserInfo(1)).Returns(new PageSubscriptionUser(1)).Verifiable();
            _instanceMock.Setup(x => x.CreateDataSession()).Returns(_sessionMock.Object).Verifiable();
            _sessionMock.Setup(x => x.UnsubscribeUser(1, 10)).Verifiable();
            InstanceExpectations();
            ExpectCurrentUserQuery(1);
            var response = service.At("pages", "10")
                .WithHeader("X-Deki-Site", "id=wicked")
                .DeleteAsync()
                .Wait();
            Assert.IsTrue(response.IsSuccessful);
            _instanceMock.VerifyAll();
            _sessionMock.VerifyAll();
            MockPlug.VerifyAll(10.Seconds());
        }

        [Test]
        public void Can_retrieve_subscription_for_specific_page() {
            var service = SetupService();
            _log.Debug("get some subscriptions");
            _instanceMock.Setup(x => x.GetUserInfo(1)).Returns(new PageSubscriptionUser(1)).Verifiable();
            _instanceMock.Setup(x => x.CreateDataSession()).Returns(_sessionMock.Object).Verifiable();
            _sessionMock.Setup(x => x.GetSubscriptionsForUser(1, It.Is<IEnumerable<uint>>(y => AreEqual(y, new uint[] { 16, 10, 12, 14 }))))
                .Returns(new List<PageSubscriptionBE> {
                    new PageSubscriptionBE {PageId = 10, UserId = 1, IncludeChildPages = true},
                    new PageSubscriptionBE {PageId = 12, UserId = 1, IncludeChildPages = false},
                    new PageSubscriptionBE {PageId = 13, UserId = 1, IncludeChildPages = true},
                    new PageSubscriptionBE {PageId = 16, UserId = 1, IncludeChildPages = false},
                })
                .AtMostOnce()
                .Verifiable();
            InstanceExpectations();
            ExpectCurrentUserQuery(1);
            MockPlug.Setup(_deki)
                .At("pages", "16")
                .WithHeader("X-Deki-Site", "id=wicked")
                .Returns(new XDoc("page")
                    .Start("security").Start("permissions.effective").Elem("operations", "SUBSCRIBE").End().End()
                    .Start("page.parent")
                        .Attr("id", 10)
                        .Start("page.parent")
                            .Attr("id", 12)
                            .Start("page.parent")
                                .Attr("id", 14)
                    .EndAll())
                .ExpectAtLeastOneCall();

            var response = service
                .At("subscriptions", "16")
                .With("page", "16")
                .WithHeader("X-Deki-Site", "id=wicked")
                .GetAsync()
                .Wait();
            Assert.IsTrue(response.IsSuccessful);
            Assert.AreEqual(
                new[] {"10:infinity", "13:infinity", "16:0"},
                (from doc in response.ToDocument()["subscription.page"]
                     let id = doc["@id"].AsUInt
                     let depth = doc["@depth"].AsText
                     orderby id
                     select id + ":" + depth).ToArray());
            _instanceMock.VerifyAll();
            _sessionMock.VerifyAll();
            MockPlug.VerifyAll(1.Seconds());
        }

        [Test]
        public void Retrieve_subscription_for_specific_page_without_subscription_returns_doc_with_nodes() {
            var service = SetupService();
            _log.Debug("get some subscriptions");
            _instanceMock.Setup(x => x.GetUserInfo(1)).Returns(new PageSubscriptionUser(1)).Verifiable();
            _instanceMock.Setup(x => x.CreateDataSession()).Returns(_sessionMock.Object).Verifiable();
            _sessionMock.Setup(x => x.GetSubscriptionsForUser(1, It.Is<IEnumerable<uint>>(y => AreEqual(y, new uint[] { 16, 10, 12, 14 }))))
                .Returns(new List<PageSubscriptionBE> {
                    new PageSubscriptionBE {PageId = 10, UserId = 1, IncludeChildPages = false},
                    new PageSubscriptionBE {PageId = 12, UserId = 1, IncludeChildPages = false},
                })
                .AtMostOnce()
                .Verifiable();
            InstanceExpectations();
            ExpectCurrentUserQuery(1);
            MockPlug.Setup(_deki)
                .At("pages", "16")
                .WithHeader("X-Deki-Site", "id=wicked")
                .Returns(new XDoc("page")
                    .Start("security").Start("permissions.effective").Elem("operations", "SUBSCRIBE").End().End()
                    .Start("page.parent")
                        .Attr("id", 10)
                        .Start("page.parent")
                            .Attr("id", 12)
                            .Start("page.parent")
                                .Attr("id", 14)
                    .EndAll())
                .ExpectAtLeastOneCall();

            var response = service
                .At("subscriptions", "16")
                .With("page", "16")
                .WithHeader("X-Deki-Site", "id=wicked")
                .GetAsync()
                .Wait();
            Assert.IsTrue(response.IsSuccessful);
            Assert.IsTrue(response.ToDocument()["subscription.page"].IsEmpty);
            _instanceMock.VerifyAll();
            _sessionMock.VerifyAll();
            MockPlug.VerifyAll(1.Seconds());
        }

        [Test]
        public void Retrieve_subscription_for_page_with_effective_subscribe_permission_is_forbidden() {
            var service = SetupService();
            _log.Debug("get some subscriptions");
            _instanceMock.Setup(x => x.GetUserInfo(1)).Returns(new PageSubscriptionUser(1)).Verifiable();
            InstanceExpectations();
            ExpectCurrentUserQuery(1);
            MockPlug.Setup(_deki)
                .At("pages", "16")
                .WithHeader("X-Deki-Site", "id=wicked")
                .Returns(new XDoc("page")
                    .Start("security").Start("permissions.effective").Elem("operations", "READ").End().End()
                    .Start("page.parent")
                        .Attr("id", 10)
                        .Start("page.parent")
                            .Attr("id", 12)
                            .Start("page.parent")
                                .Attr("id", 14)
                    .EndAll())
                .ExpectAtLeastOneCall();

            var response = service
                .At("subscriptions", "16")
                .With("page", "16")
                .WithHeader("X-Deki-Site", "id=wicked")
                .GetAsync()
                .Wait();
            Assert.AreEqual(DreamStatus.Forbidden, response.Status);
            _instanceMock.Verify(x => x.CreateDataSession(), Times.Never());
            _instanceMock.VerifyAll();
            MockPlug.VerifyAll(1.Seconds());
        }

        [Test]
        public void Retrieve_subscription_for_page_range() {
            var service = SetupService();
            _log.Debug("get some subscriptions");
            _instanceMock.Setup(x => x.GetUserInfo(1)).Returns(new PageSubscriptionUser(1)).Verifiable();
            _instanceMock.Setup(x => x.CreateDataSession()).Returns(_sessionMock.Object).Verifiable();
            _sessionMock.Setup(x => x.GetSubscriptionsForUser(1, It.Is<IEnumerable<uint>>(y => AreEqual(y, new uint[] { 10, 12, 14, 16 }))))
                .Returns(new List<PageSubscriptionBE> {
                    new PageSubscriptionBE {PageId = 10, UserId = 1, IncludeChildPages = true},
                    new PageSubscriptionBE {PageId = 12, UserId = 1, IncludeChildPages = true},
                    new PageSubscriptionBE {PageId = 16, UserId = 1, IncludeChildPages = true},
                })
                .AtMostOnce()
                .Verifiable();
            InstanceExpectations();
            ExpectCurrentUserQuery(1);
            var response = service
                .At("subscriptions")
                .With("pages", "10,12,14,16")
                .WithHeader("X-Deki-Site", "id=wicked")
                .GetAsync()
                .Wait();
            Assert.IsTrue(response.IsSuccessful);
            XDoc subscriptions = response.ToDocument();
            _instanceMock.VerifyAll();
            _sessionMock.VerifyAll();
            MockPlug.VerifyAll(10.Seconds());
            Assert.AreEqual(new[] { 10, 12, 16 }, subscriptions["subscription.page/@id"].Select(x => x.AsInt ?? 0).ToArray());
        }

        [Test]
        public void Retrieve_subscription_all_pages() {
            var service = SetupService();
            _log.Debug("get some subscriptions");
            _instanceMock.Setup(x => x.GetUserInfo(1)).Returns(new PageSubscriptionUser(1)).Verifiable();
            _instanceMock.Setup(x => x.CreateDataSession()).Returns(_sessionMock.Object).Verifiable();
            _sessionMock.Setup(x => x.GetSubscriptionsForUser(1, It.Is<IEnumerable<uint>>(y => !y.Any())))
                .Returns(new List<PageSubscriptionBE> {
                    new PageSubscriptionBE {PageId = 10, UserId = 1, IncludeChildPages = true},
                    new PageSubscriptionBE {PageId = 12, UserId = 1, IncludeChildPages = true},
                    new PageSubscriptionBE {PageId = 16, UserId = 1, IncludeChildPages = true},
                })
                .AtMostOnce()
                .Verifiable();
            InstanceExpectations();
            ExpectCurrentUserQuery(1);
            var response = service
                .At("subscriptions")
                .WithHeader("X-Deki-Site", "id=wicked")
                .GetAsync()
                .Wait();
            Assert.IsTrue(response.IsSuccessful);
            XDoc subscriptions = response.ToDocument();
            _instanceMock.VerifyAll();
            _sessionMock.VerifyAll();
            MockPlug.VerifyAll(10.Seconds());
            Assert.AreEqual(new[] { 10, 12, 16 }, subscriptions["subscription.page/@id"].Select(x => x.AsInt ?? 0).ToArray());
        }

        [Test]
        public void Retrieve_subscription_for_page_range_by_user_id() {
            var service = SetupService();
            _log.Debug("get some subscriptions");
            _instanceMock.Setup(x => x.GetUserInfo(1)).Returns(new PageSubscriptionUser(1)).Verifiable();
            _instanceMock.Setup(x => x.CreateDataSession()).Returns(_sessionMock.Object).Verifiable();
            _sessionMock.Setup(x => x.GetSubscriptionsForUser(1, It.Is<IEnumerable<uint>>(y => AreEqual(y, new uint[] { 10, 12, 14, 16 }))))
                .Returns(new List<PageSubscriptionBE> {
                    new PageSubscriptionBE {PageId = 10, UserId = 1, IncludeChildPages = true},
                    new PageSubscriptionBE {PageId = 12, UserId = 1, IncludeChildPages = true},
                    new PageSubscriptionBE {PageId = 16, UserId = 1, IncludeChildPages = true},
                })
                .AtMostOnce()
                .Verifiable();
            InstanceExpectations();
            ExpectUserQuery(1);
            var response = service
                .At("subscribers", "1")
                .With("pages", "10,12,14,16")
                .WithHeader("X-Deki-Site", "id=wicked")
                .GetAsync()
                .Wait();
            Assert.IsTrue(response.IsSuccessful);
            XDoc subscriptions = response.ToDocument();
            _instanceMock.VerifyAll();
            _sessionMock.VerifyAll();
            MockPlug.VerifyAll(10.Seconds());
            Assert.AreEqual(new[] { 10, 12, 16 }, subscriptions["subscription.page/@id"].Select(x => x.AsInt ?? 0).ToArray());
        }

        [Test]
        public void Retrieve_subscription_all_pages_by_user_id() {
            var service = SetupService();
            _log.Debug("get some subscriptions");
            _instanceMock.Setup(x => x.GetUserInfo(1)).Returns(new PageSubscriptionUser(1)).Verifiable();
            _instanceMock.Setup(x => x.CreateDataSession()).Returns(_sessionMock.Object).Verifiable();
            _sessionMock.Setup(x => x.GetSubscriptionsForUser(1, It.Is<IEnumerable<uint>>(y => !y.Any())))
                .Returns(new List<PageSubscriptionBE> {
                    new PageSubscriptionBE {PageId = 10, UserId = 1, IncludeChildPages = true},
                    new PageSubscriptionBE {PageId = 12, UserId = 1, IncludeChildPages = true},
                    new PageSubscriptionBE {PageId = 16, UserId = 1, IncludeChildPages = true},
                })
                .AtMostOnce()
                .Verifiable();
            InstanceExpectations();
            ExpectUserQuery(1);
            var response = service
                .At("subscribers", "1")
                .WithHeader("X-Deki-Site", "id=wicked")
                .GetAsync()
                .Wait();
            Assert.IsTrue(response.IsSuccessful);
            XDoc subscriptions = response.ToDocument();
            _instanceMock.VerifyAll();
            _sessionMock.VerifyAll();
            MockPlug.VerifyAll(10.Seconds());
            Assert.AreEqual(new[] { 10, 12, 16 }, subscriptions["subscription.page/@id"].Select(x => x.AsInt ?? 0).ToArray());
        }

        [Test]
        public void Retrieve_subscription_matches_current_user_subscription_by_current() {
             var service = SetupService();
            _log.Debug("get some subscriptions");
            _instanceMock.Setup(x => x.GetUserInfo(1)).Returns(new PageSubscriptionUser(1)).Verifiable();
            _instanceMock.Setup(x => x.CreateDataSession()).Returns(_sessionMock.Object).Verifiable();
            _sessionMock.Setup(x => x.GetSubscriptionsForUser(1, It.Is<IEnumerable<uint>>(y => !y.Any())))
                .Returns(new List<PageSubscriptionBE> {
                    new PageSubscriptionBE {PageId = 10, UserId = 1, IncludeChildPages = true},
                    new PageSubscriptionBE {PageId = 12, UserId = 1, IncludeChildPages = true},
                    new PageSubscriptionBE {PageId = 16, UserId = 1, IncludeChildPages = true},
                })
                .Verifiable();
            InstanceExpectations();
            ExpectCurrentUserQuery(1);

            _log.Debug("get current request user subscriptions");
            var response = service
                .At("subscriptions")
                .WithHeader("X-Deki-Site", "id=wicked")
                .GetAsync()
                .Wait();
            Assert.IsTrue(response.IsSuccessful);
            XDoc currentSubscriptions = response.ToDocument();

            _log.Debug("get user subscriptions by current");
            response = service
                .At("subscribers", "current")
                .WithHeader("X-Deki-Site", "id=wicked")
                .GetAsync()
                .Wait();
            Assert.IsTrue(response.IsSuccessful);
            XDoc userSubscriptions = response.ToDocument();
            Assert.AreEqual(currentSubscriptions, userSubscriptions);
            _instanceMock.VerifyAll();
            _sessionMock.VerifyAll();
            MockPlug.VerifyAll(10.Seconds());
        }

        [Test]
        public void Retrieve_subscription_matches_current_user_subscription_by_id() {
             var service = SetupService();
            _log.Debug("get some subscriptions");
            _instanceMock.Setup(x => x.GetUserInfo(1)).Returns(new PageSubscriptionUser(1)).Verifiable();
            _instanceMock.Setup(x => x.CreateDataSession()).Returns(_sessionMock.Object).Verifiable();
            _sessionMock.Setup(x => x.GetSubscriptionsForUser(1, It.Is<IEnumerable<uint>>(y => !y.Any())))
                .Returns(new List<PageSubscriptionBE> {
                    new PageSubscriptionBE {PageId = 10, UserId = 1, IncludeChildPages = true},
                    new PageSubscriptionBE {PageId = 12, UserId = 1, IncludeChildPages = true},
                    new PageSubscriptionBE {PageId = 16, UserId = 1, IncludeChildPages = true},
                })
                .Verifiable();
            InstanceExpectations();

            _log.Debug("get current request user subscriptions");
            ExpectCurrentUserQuery(1);
            var response = service
                .At("subscriptions")
                .WithHeader("X-Deki-Site", "id=wicked")
                .GetAsync()
                .Wait();
            Assert.IsTrue(response.IsSuccessful);
            XDoc currentSubscriptions = response.ToDocument();

            _log.Debug("get user subscriptions by user id 1");
            ExpectUserQuery(1);
            response = service
                .At("subscribers", "1")
                .WithHeader("X-Deki-Site", "id=wicked")
                .GetAsync()
                .Wait();
            Assert.IsTrue(response.IsSuccessful);
            XDoc userSubscriptions = response.ToDocument();
            Assert.AreEqual(currentSubscriptions, userSubscriptions);
            _instanceMock.VerifyAll();
            _sessionMock.VerifyAll();
            MockPlug.VerifyAll(10.Seconds());
        }

        [Test]
        public void Siteid_query_arg_gets_translated_to_wikiid_header() {
            var service = SetupService();
            _log.Debug("get some subscriptions");
            _instanceMock.Setup(x => x.GetUserInfo(1)).Returns(new PageSubscriptionUser(1)).Verifiable();
            _instanceMock.Setup(x => x.CreateDataSession()).Returns(_sessionMock.Object).Verifiable();
            _sessionMock.Setup(x => x.GetSubscriptionsForUser(1, It.Is<IEnumerable<uint>>(y => !y.Any())))
                .Returns(new List<PageSubscriptionBE> {
                    new PageSubscriptionBE {PageId = 10, UserId = 1, IncludeChildPages = true},
                })
                .AtMostOnce()
                .Verifiable();
            InstanceExpectations();
            ExpectCurrentUserQuery(1);
            var response = service
                .At("subscriptions")
                .With("siteid", "wicked")
                .GetAsync()
                .Wait();
            Assert.IsTrue(response.IsSuccessful);
            _instanceMock.VerifyAll();
            _sessionMock.VerifyAll();
            MockPlug.VerifyAll(10.Seconds());
        }

        [Test]
        public void User_without_proper_page_permissions_gets_forbidden_on_subscribe_attempt() {
            var service = SetupService();
            _log.Debug("post page 10 subscription");
            _instanceMock.Setup(x => x.GetUserInfo(1)).Returns(new PageSubscriptionUser(1)).Verifiable();
            InstanceExpectations();
            MockPlug.Setup(_deki)
                .At("pages", "10", "allowed")
                .With("permissions", "read,subscribe")
                .WithHeader("X-Deki-Site", "id=wicked")
                .Returns(new XDoc("users").Start("user").Attr("id", 5).End())
                .ExpectAtLeastOneCall();
            ExpectCurrentUserQuery(1);
            var response = service
                .At("pages", "10")
                .With("depth", "infinity")
                .WithHeader("X-Deki-Site", "id=wicked")
                .PostAsync()
                .Wait();
            Assert.IsFalse(response.IsSuccessful);
            Assert.AreEqual(DreamStatus.Forbidden, response.Status);
            _instanceMock.VerifyAll();
            MockPlug.VerifyAll(10.Seconds());
        }

        [Test]
        public void User_delete_event_wipes_subscriptions() {
            var service = SetupService();
            _log.Debug("posting a user delete event");
            _instanceMock.Setup(x => x.GetUserInfo(1)).Returns(new PageSubscriptionUser(1)).AtMostOnce().Verifiable();
            _instanceMock.Setup(x => x.CreateDataSession()).Returns(_sessionMock.Object).Verifiable();
            _sessionMock.Setup(x => x.UnsubscribeUser(1, null)).AtMostOnce().Verifiable();
            InstanceExpectations();
            string channel = "event://wicked/deki/users/delete";
            var response = service.At("updateuser")
                .WithHeader(DreamHeaders.DREAM_EVENT_CHANNEL, channel)
                .PostAsync(CreateDekiEvent().Elem("channel", channel).Elem("userid", 1))
                .Wait();
            Assert.IsTrue(response.IsSuccessful);
            _instanceMock.VerifyAll();
            _sessionMock.VerifyAll();
            MockPlug.VerifyAll(10.Seconds());
        }

        [Test]
        public void Deleted_page_event_wipes_subscriptions_for_page() {
            var service = SetupService();
            _log.Debug("posting a page delete event");
            _instanceMock.Setup(x => x.CreateDataSession()).Returns(_sessionMock.Object).Verifiable();
            _sessionMock.Setup(x => x.UnsubscribePage(10)).AtMostOnce().Verifiable();
            string channel = "event://wicked/deki/pages/delete";

            MockPlug.Setup(_deki)
                .At("pages", "10")
                .WithHeader("X-Deki-Site", "id=wicked")
                .Returns(DreamMessage.NotFound("no longer here")).ExpectCalls(PlugTimes.Never());
            InstanceExpectations();
            var response = service.At("notify")
                .WithHeader(DreamHeaders.DREAM_EVENT_CHANNEL, channel)
                .PostAsync(CreateDekiEvent().Elem("channel", channel).Elem("pageid", 10))
                .Wait();
            Assert.IsTrue(response.IsSuccessful);
            _instanceMock.VerifyAll();
            _sessionMock.VerifyAll();
            MockPlug.VerifyAll(5.Seconds());
        }

        /// <summary>
        ///     Subscribe user to a page and check that he was subscribed
        /// </summary>        
        /// <feature>
        /// <name>PUT: /@api/deki/pagesubservice/pages/{pageid}/subscribers/{userid}?siteid={siteid}</name>
        /// </feature>
        /// <expected>200 OK HTTP Response</expected>
        [Test]
        public void SubscribeUserAsAdmin() {
            // 1) Create user
            // 2) Create page
            // 3) Subscripe user to page
            // 4) Check user is subscribed

            Plug p = Utils.BuildPlugForAdmin();
            DreamMessage msg;

            string pageId;
            string path;
            string userId;
            string userName;

            // create random user
            msg = UserUtils.CreateRandomUser(p, "Contributor", out userId, out userName);
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Creating random user failed.");

            // create random page
            msg = PageUtils.CreateRandomPage(p, out pageId, out path);
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Creating random page failed.");

            // subscribe the user
            msg = p.At("pagesubservice", "pages", pageId, "subscribers", userId).With("siteid", "default").Put(DreamMessage.Ok());
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Creating page subscribtion failed");

            // Check subscribtion
            msg = p.At("pagesubservice", "pages", pageId, "subscribers").With("siteid", "default").Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Getting page subscriptions failed");
            string subscriberID = msg.ToDocument()["//subscriber/@id"].AsText;
            Assert.AreEqual(subscriberID, userId, "User was not subscribed properly.");
        }

        /// <summary>
        ///     Subscribe user as non admin to a page. This should fail.
        /// </summary>        
        /// <feature>
        /// <name>PUT: /@api/deki/pagesubservice/pages/{pageid}/subscribers/{userid}?siteid={siteid}</name>
        /// </feature>
        /// <expected>403 Forbidden HTTP Response</expected>
        [Test]
        public void SubscribeUserAsNonAdmin() {
            // 1) Create 2 user
            // 2) Create page
            // 3) Attempt to subscribe user2 to page using user 1 (should fail)
            // 4) Attempt to subscribe user2 to page using Anonymous (should fail)

            Plug p = Utils.BuildPlugForAdmin();
            DreamMessage msg;
            string pageId;
            string path;
            string userNameOne;
            string userNameTwo;
            string userIdOne;
            string userIdTwo;

            // create users
            msg = UserUtils.CreateRandomUser(p, "Contributor", out userIdOne, out userNameOne);
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Failed to create random user.");
            msg = UserUtils.CreateRandomUser(p, "Contributor", out userIdTwo, out userNameTwo);
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Failed to create random user.");

            // create random page
            msg = PageUtils.CreateRandomPage(p, out pageId, out path);
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Creating random page failed.");
            p = Utils.BuildPlugForUser(userNameOne);

            // subscribe the user
            try {
                msg = p.At("pagesubservice", "pages", pageId, "subscribers", userIdTwo).With("siteid", "default").Put(DreamMessage.Ok());
            } catch(MindTouch.Dream.DreamResponseException ex) {
                Assert.IsTrue(ex.Response.Status == DreamStatus.Forbidden, "Non-Admin should not be allowed to subscribe user");
            }
            p = Utils.BuildPlugForAnonymous();

            // subscribe the user
            try {
                msg = p.At("pagesubservice", "pages", pageId, "subscribers", userIdTwo).With("siteid", "default").Put(DreamMessage.Ok());
            } catch(MindTouch.Dream.DreamResponseException ex) {
                Assert.IsTrue(ex.Response.Status == DreamStatus.BadRequest, "Anonymous user should not be allowed to subscribe user");
            }
        }

        /// <summary>
        ///     Subscribe yourself to a page.
        /// </summary>        
        /// <feature>
        /// <name>PUT: /@api/deki/pagesubservice/pages/{pageid}/subscribers/{userid}?siteid={siteid}</name>
        /// </feature>
        /// <expected>200 Success HTTP Response</expected>
        [Test]
        public void SubscribeYourself() {
            // 1) Create user
            // 2) Create page
            // 3) Attempt to subscribe yourself to that page

            var p = Utils.BuildPlugForAdmin();
            DreamMessage msg;
            string pageId;
            string path;
            string userName;
            string userId;

            // create users
            msg = UserUtils.CreateRandomUser(p, "Contributor", out userId, out userName);
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Failed to create random user.");
          

            // create random page
            msg = PageUtils.CreateRandomPage(p, out pageId, out path);
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Creating random page failed.");
            p = Utils.BuildPlugForUser(userName);

            // subscribe the user
            try {
                msg = p.At("pagesubservice", "pages", pageId, "subscribers", userId).With("siteid", "default").Put(DreamMessage.Ok());
            } catch(MindTouch.Dream.DreamResponseException ex) {
                Assert.IsTrue(ex.Response.Status == DreamStatus.Ok, "Failed to subscribe onself to a page.");
            }
        }

        /// <summary>
        ///     Subscribe an anonymous user. This should fail.
        /// </summary>        
        /// <feature>
        /// <name>PUT: /@api/deki/pagesubservice/pages/{pageid}/subscribers/{userid}?siteid={siteid}</name>
        /// </feature>
        /// <expected>400 BadRequest HTTP Response</expected>
        /// 
        [Test]
        public void SubscribeAnonymous() {
            // 1) Create page
            // 2) Attempt to subscribe Anonymous to page (should fail)

            Plug p = Utils.BuildPlugForAdmin();
            DreamMessage msg;
            string pageId;
            string path;

            // create random page
            msg = PageUtils.CreateRandomPage(p, out pageId, out path);
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Creating random page failed.");

            // attempt to subscribe anonymous
            try {
                msg = p.At("pagesubservice", "pages", pageId, "subscribers", "2").With("siteid", "default").Put(DreamMessage.Ok());
            } catch(MindTouch.Dream.DreamResponseException ex) {
                Assert.IsTrue(ex.Response.Status == DreamStatus.BadRequest, "Should not be able to subscribe anonymous user");
            }
        }

        /// <summary>
        ///     Subscribe a user to a non existant page. This should fail.
        /// </summary>        
        /// <feature>
        /// <name>PUT: /@api/deki/pagesubservice/pages/{pageid}/subscribers/{userid}?siteid={siteid}</name>
        /// </feature>
        /// <expected>403 Forbidden HTTP Response</expected>
        [Test]
        public void SubscribeUserToNonexistantPage() {
            // 1) Create Page
            // 2) Delete it
            // 3) Create user
            // 4) Attempt to subscribe user to it. (should fail)

            DreamMessage msg;
            string pageId;
            string path;
            string userId;
            string userName;
            Plug p = Utils.BuildPlugForAdmin();

            // create random page and delete it
            msg = PageUtils.CreateRandomPage(p, out pageId, out path);
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Creating random page failed.");
            msg = PageUtils.DeletePageByID(p, pageId, true);
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Deleting page failed");

            // create random user
            msg = UserUtils.CreateRandomUser(p, "Contributor", out userId, out userName);
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Failed to create random user.");
            try {
                msg = p.At("pagesubservice", "pages", pageId, "subscribers", userId).With("siteid", "default").Put(DreamMessage.Ok());
            } catch(MindTouch.Dream.DreamResponseException ex) {
                Assert.AreEqual(ex.Response.Status, DreamStatus.Forbidden, "Should not be able to subscribe non-existant page.");
            }
        }

        /// <summary>
        ///     Subscribe a non existant user to a page. This should fail.
        /// </summary>        
        /// <feature>
        /// <name>PUT: /@api/deki/pagesubservice/pages/{pageid}/subscribers/{userid}?siteid={siteid}</name>
        /// </feature>
        /// <expected>404 Not Found HTTP Response</expected>
        /// 
        [Test]
        public void SubscribeNonExistantUser() {
            // 1) Create Page
            // 2) Create user
            // 3) Attempt to subscribe user with id (userid + 1) to it. (should fail)

            DreamMessage msg;
            string pageId;
            string path;
            string userId;
            string userName;
            Plug p = Utils.BuildPlugForAdmin();

            // create random page 
            msg = PageUtils.CreateRandomPage(p, out pageId, out path);
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Creating random page failed.");

            // create random user and delete him
            msg = UserUtils.CreateRandomUser(p, "Contributor", out userId, out userName);
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Failed to create random user.");

            // add 1 to userid so that the user doesn't exist
            userId = (int.Parse(userId) + 1).ToString();
            try {
                msg = p.At("pagesubservice", "pages", pageId, "subscribers", "1000").With("siteid", "default").Put(DreamMessage.Ok());
            } catch(MindTouch.Dream.DreamResponseException ex) {
                Assert.AreEqual(ex.Response.Status, DreamStatus.NotFound, "Should not be able to subscribe non-existant user.");
            }
        }

        private XDoc CreateDekiEvent() {
            return new XDoc("deki-event").Attr("wikiid", "wicked").Attr("event-time", DateTime.Parse("2009/01/01 12:00:00"));
        }

        private Plug SetupService() {
            _log.Debug("set up service");
            MockPlug.Setup(_subscribe)
                .Verb("POST")
                .At("subscribers")
                .Returns(m => {
                    DreamMessage msg = DreamMessage.Ok(new XDoc("x"));
                    msg.Headers.Location = _subscribe.At("testsub");
                    return msg;
                })
                .ExpectAtLeastOneCall();
            DreamServiceInfo serviceInfo = DreamTestHelper.CreateService(
                _hostInfo,
                typeof(DekiChangeSubscriptionService),
                "email",
                new XDoc("config")
                    .Elem("uri.emailer", _email)
                    .Elem("uri.deki", _deki)
                    .Elem("uri.pubsub", _subscribe)
                    .Elem("uri.storage", _storage)
                    .Elem("accumulation-time", 0)
                    .Elem("from-address", "foo@bar.com")
                    .Elem("apikey", _apikey)
                );
            var service = serviceInfo.WithInternalKey().AtLocalHost;
            MockPlug.VerifyAll(10.Seconds());
            MockPlug.DeregisterAll();
            return service;
        }

        private void PostSubscription(Plug service, uint userId, uint pageId, bool childPages) {
            _log.DebugFormat("post a subscription for user {0} on page {1}, recurse: {2}", userId, pageId, childPages);
            InitSession();
            _instanceMock.Setup(x => x.GetUserInfo(userId)).Returns(new PageSubscriptionUser(userId));
            _instanceMock.SetupGet(x => x.WikiId).Returns("wicked");
            _instanceMock.Setup(x => x.CreateDataSession()).Returns(_sessionMock.Object);
            _sessionMock.Setup(x => x.Subscribe(userId, pageId, childPages)).AtMostOnce().Verifiable();
            ExpectCurrentUserQuery(userId);
            MockPlug.Setup(_deki)
                .At("pages", pageId.ToString(), "allowed")
                .With("permissions", "read,subscribe")
                .WithHeader("X-Deki-Site", "id=wicked")
                .Returns(new XDoc("users").Start("user").Attr("id", userId).End())
                .ExpectAtLeastOneCall();
            InstanceExpectations();
            DreamMessage response = service
                .At("pages", pageId.ToString())
                .With("depth", childPages ? "infinity" : "0")
                .WithHeader("X-Deki-Site", "id=wicked")
                .PostAsync()
                .Wait();
            Assert.IsTrue(response.IsSuccessful, response.AsText());
            _sessionMock.VerifyAll();
            MockPlug.VerifyAll(10.Seconds());
            MockPlug.DeregisterAll();
        }

        private void ExpectCurrentUserQuery(uint userId) {
            MockPlug.Setup(_deki)
                .Verb("GET")
                .At("users", "current")
                .WithHeader("X-Deki-Site", "id=wicked")
                .Returns(new XDoc("user")
                             .Attr("id", userId)
                             .Elem("email", userId + "@b.com")
                             .Elem("username", userId + "-bob")
                             .Elem("language", "en")
                             .Start("permissions.user")
                             .End())
                .ExpectAtLeastOneCall();
        }

        private void ExpectUserQuery(uint userId) {
             MockPlug.Setup(_deki)
                .Verb("GET")
                .At("users", userId.ToString())
                .WithHeader("X-Deki-Site", "id=wicked")
                .Returns(new XDoc("user")
                             .Attr("id", userId)
                             .Elem("email", userId + "@b.com")
                             .Elem("username", userId + "-bob")
                             .Elem("language", "en")
                             .Start("permissions.user")
                             .End())
                .ExpectAtLeastOneCall();
        }

        private void InstanceExpectations() {
            MockPlug.Setup(_storage).At("subscriptions", "wicked").Verb("GET");
            MockPlug.Setup(_deki)
                .Verb("GET")
                .At("site", "settings")
                .WithHeader("X-Deki-Site", "id=wicked")
                .Returns(new XDoc("config").Value("content don't matter, since it's all exposed by the mock"));
        }

        private static bool AreEqual<T>(IEnumerable<T> a, IEnumerable<T> b) {
            return
                string.Join(".", a.Select(z => z.ToString()).ToArray()) ==
                string.Join(".", b.Select(z => z.ToString()).ToArray());
        }
    }
}
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
using MindTouch.Deki.PubSub;
using MindTouch.Dream;
using MindTouch.Dream.Services.PubSub;
using MindTouch.Dream.Test;
using MindTouch.Tasking;
using MindTouch.Web;
using MindTouch.Xml;
using Moq;
using NUnit.Framework;
using MindTouch.Extensions.Time;
using Times = MindTouch.Dream.Test.Mock.Times;

namespace MindTouch.Deki.Tests {

    [TestFixture]
    public class DekiPubSubTests {

        private static readonly log4net.ILog _log = LogUtils.CreateLog();

        private DreamHostInfo _hostInfo;
        private Mock<IPubSubDispatchQueueRepository> _mockRepository;

        [TestFixtureSetUp]
        public void GlobalInit() {
            _hostInfo = DreamTestHelper.CreateRandomPortHost();
        }

        [TearDown]
        public void PerTestCleanup() {
            MockPlug.DeregisterAll();
        }

        [SetUp]
        public void Setup() {
            _mockRepository = new Mock<IPubSubDispatchQueueRepository>();
        }

        [Test]
        public void Authtoken_recipients_always_get_what_they_want() {
            XUri mockDeki = new XUri("http://mock/deki");
            int dekiCalled = 0;
            MockPlug.Register(mockDeki, delegate(Plug p, string v, XUri u, DreamMessage r, Result<DreamMessage> r2) {
                _log.DebugFormat("deki called at {0}", u);
                dekiCalled++;
                r2.Return(DreamMessage.Ok());
            });
            XUri mockAuthorized = new XUri("http://mock/authorized");
            int authorizedCalled = 0;
            XDoc received = null;
            var authorizedResetEvent = new ManualResetEvent(false);
            MockPlug.Register(mockAuthorized, delegate(Plug p, string v, XUri u, DreamMessage r, Result<DreamMessage> r2) {
                authorizedCalled++;
                received = r.ToDocument();
                authorizedResetEvent.Set();
                r2.Return(DreamMessage.Ok());
            });
            XUri mockNotAuthorized = new XUri("http://mock/notauthorized");
            int notAuthorizedCalled = 0;
            MockPlug.Register(mockNotAuthorized, delegate(Plug p, string v, XUri u, DreamMessage r, Result<DreamMessage> r2) {
                notAuthorizedCalled++;
                r2.Return(DreamMessage.Ok());
            });
            DreamServiceInfo serviceInfo = DreamTestHelper.CreateService(
                _hostInfo,
                "sid://mindtouch.com/dream/2008/10/pubsub",
                "authorization",
                new XDoc("config")
                    .Elem("authtoken", "abc")
                    .Elem("uri.deki", mockDeki)
                    .Start("components")
                        .Attr("context", "service")
                        .Start("component")
                            .Attr("implementation", typeof(DekiDispatcher).AssemblyQualifiedName)
                            .Attr("type", typeof(IPubSubDispatcher).AssemblyQualifiedName)
                        .End()
                    .End()
            );
            XDoc subForAuthorized = new XDoc("subscription-set")
                .Elem("uri.owner", mockAuthorized)
                .Start("subscription")
                    .Attr("id", "1")
                    .Elem("uri.resource", "http://mock/resource/*")
                    .Elem("channel", "channel:///foo/*")
                    .Start("recipient").Attr("authtoken", "abc").Elem("uri", mockAuthorized).End()
                .End();
            DreamMessage result = serviceInfo.WithInternalKey().AtLocalHost.At("subscribers").PostAsync(subForAuthorized).Wait();
            Assert.IsTrue(result.IsSuccessful);

            XDoc subForNotAuthorized = new XDoc("subscription-set")
                .Elem("uri.owner", mockNotAuthorized)
                .Start("subscription")
                    .Attr("id", "1")
                    .Elem("uri.resource", "http://mock/resource/*")
                    .Elem("channel", "channel:///foo/*")
                    .Start("recipient").Elem("uri", mockNotAuthorized).End()
                .End();
            result = serviceInfo.WithInternalKey().AtLocalHost.At("subscribers").PostAsync(subForNotAuthorized).Wait();
            Assert.IsTrue(result.IsSuccessful);
            XDoc msg = new XDoc("foop");
            result = serviceInfo.WithInternalKey()
                .AtLocalHost
                .At("publish")
                .WithHeader(DreamHeaders.DREAM_EVENT_CHANNEL, "channel:///foo/bar")
                .WithHeader(DreamHeaders.DREAM_EVENT_ORIGIN, mockDeki.ToString())
                .WithHeader(DreamHeaders.DREAM_EVENT_RESOURCE, "http://mock/resource/bar")
                .PostAsync(msg).Wait();
            Assert.IsTrue(result.IsSuccessful);
            Wait.For(() => authorizedResetEvent.WaitOne(100, true), TimeSpan.FromSeconds(10));
            Assert.AreEqual(0, dekiCalled);
            Assert.AreEqual(0, notAuthorizedCalled);
            Assert.AreEqual(1, authorizedCalled);
            Assert.AreEqual(msg, received);
        }

        [Test]
        public void InstanceKey_authtoken_gets_verified_against_deki() {
            var mockDeki = new XUri("http://mock/deki");
            var dispatcherUri = new XUri("http://mock/dispatcher");
            var authorizedRecipient = new XUri("http://mock/authorized");
            var dispatcher = new DekiDispatcher(
                new DispatcherConfig {
                    ServiceUri = dispatcherUri,
                    ServiceAccessCookie = new DreamCookie("service-key", "foo", dispatcherUri),
                    ServiceConfig = new XDoc("config").Elem("uri.deki", mockDeki).Elem("authtoken", "abc")
                },
                _mockRepository.Object
            );
            var sub = new XDoc("subscription-set")
                .Elem("uri.owner", authorizedRecipient)
                    .Start("subscription")
                    .Attr("id", "1")
                    .Elem("channel", "event://default/deki/pages/*")
                    .Start("recipient").Attr("authtoken", "def").Elem("uri", authorizedRecipient).End()
                .End();
            _log.DebugFormat("registering sub set");
            dispatcher.RegisterSet("abc", sub, "def");
            Assert.IsTrue(Wait.For(
                () => dispatcher.CombinedSet.Subscriptions
                    .Where(s =>
                        s.Channels.Where(c => c.ToString() == "event://default/deki/pages/*").Any()
                        && !s.Resources.Any()
                    ).Any(),
                10.Seconds()));
            MockPlug.Setup(mockDeki)
                .Verb("GET")
                .At("site", "settings")
                .With("apikey", "abc")
                .WithHeader("X-Deki-Site", "id=default")
                .Returns(new XDoc("config").Start("security").Elem("api-key", "def").End())
                .ExpectCalls(Times.Once());
            MockPlug.Setup(authorizedRecipient)
                .Verb("POST")
                .WithMessage(r => {
                    if(!r.HasDocument) {
                        return false;
                    }
                    var doc = r.ToDocument();
                    _log.Debug(doc.ToPrettyString());
                    return doc["channel"].AsText == "event://default/deki/pages/update"
                        && doc["pageid"].AsText == "10";
                })
                .ExpectCalls(Times.Once());
            var evDoc = new XDoc("deki-event")
                 .Attr("wikiid", "default")
                 .Elem("channel", "event://default/deki/pages/update")
                 .Elem("uri", "deki://default/pages/10")
                 .Elem("pageid", "10");
            var ev = new DispatcherEvent(evDoc, new XUri("event://default/deki/pages/update"), new XUri("deki://default/pages/10"), new XUri("http://default/deki/pages/10"));
            _log.DebugFormat("ready to dispatch event");
            dispatcher.Dispatch(ev);
            MockPlug.VerifyAll(TimeSpan.FromSeconds(10));
        }

        [Test]
        public void Page_events_uses_deki_to_check_parent_wildcard_resource_matches() {
            var dekiPageAuthEvent = new ManualResetEvent(false);
            var authorizedRecipientCalledEv = new ManualResetEvent(false);
            var mockMatchCalledEv = new ManualResetEvent(false);
            var mockWildcardCalledEv = new ManualResetEvent(false);
            XUri mockDeki = new XUri("http://mock/deki");
            int dekiPageAuthCalled = 0;
            int dekiPageCalled = 0;
            MockPlug.Register(mockDeki, delegate(Plug p, string v, XUri u, DreamMessage r, Result<DreamMessage> r2) {
                XDoc rDoc = null;
                _log.DebugFormat("mockDeki called at: {0}", u);
                if(u.Segments.Length == 3) {
                    _log.DebugFormat("getting page xml");
                    if(u.LastSegment == "10") {
                        dekiPageCalled++;
                    }
                    rDoc = new XDoc("page")
                        .Attr("id", "10")
                        .Start("page.parent")
                            .Attr("id", "9")
                            .Start("page.parent")
                                .Attr("id", "8")
                                .Start("page.parent")
                                    .Attr("id", "7")
                        .EndAll();
                } else {
                    _log.DebugFormat("getting users for page: {0}", u.LastSegment);
                    dekiPageAuthCalled++;
                    rDoc = new XDoc("users");
                    foreach(XDoc user in r.ToDocument()["user/@id"]) {
                        rDoc.Start("user").Attr("id", user.AsText).End();
                    }
                    if(dekiPageAuthCalled == 2) {
                        dekiPageAuthEvent.Set();
                    }
                }
                r2.Return(DreamMessage.Ok(rDoc));
            });
            XUri authorizedRecipient = new XUri("http://mock/authorized2");
            int authorizedRecipientCalled = 0;
            XDoc authorizedReceived = null;
            MockPlug.Register(authorizedRecipient, delegate(Plug p, string v, XUri u, DreamMessage r, Result<DreamMessage> r2) {
                _log.DebugFormat("authorizedRecipient called at: {0}", u);
                authorizedRecipientCalled++;
                authorizedReceived = r.ToDocument();
                authorizedRecipientCalledEv.Set();
                r2.Return(DreamMessage.Ok());
            });
            XUri mockRecipient = new XUri("http://mock/r1");
            int mockRecipientCalled = 0;
            int mockMatchCalled = 0;
            int mockWildcardCalled = 0;
            MockPlug.Register(mockRecipient, delegate(Plug p, string v, XUri u, DreamMessage r, Result<DreamMessage> r2) {
                _log.DebugFormat("mockRecipient called at: {0}", u);
                mockRecipientCalled++;
                if(u == mockRecipient.At("match")) {
                    mockMatchCalled++;
                    mockMatchCalledEv.Set();
                } else if(u == mockRecipient.At("wildcard")) {
                    mockWildcardCalled++;
                    mockWildcardCalledEv.Set();
                }
                r2.Return(DreamMessage.Ok());
            });
            XUri dispatcherUri = new XUri("http://mock/dispatcher");
            DekiDispatcher dispatcher = new DekiDispatcher(
                new DispatcherConfig() {
                    ServiceUri = dispatcherUri,
                    ServiceAccessCookie = new DreamCookie("service-key", "foo", dispatcherUri),
                    ServiceConfig = new XDoc("config").Elem("uri.deki", mockDeki).Elem("authtoken", "abc")
                },
                _mockRepository.Object
            );
            XDoc sub1 = new XDoc("subscription-set")
                .Elem("uri.owner", authorizedRecipient)
                    .Start("subscription")
                    .Attr("id", "1")
                    .Elem("channel", "event://default/deki/pages/*")
                    .Start("recipient").Attr("authtoken", "abc").Elem("uri", authorizedRecipient).End()
                .End();
            _log.DebugFormat("registering sub set 1");
            dispatcher.RegisterSet("location1", sub1, "def");
            Assert.IsTrue(Wait.For(() =>
                dispatcher.CombinedSet.Subscriptions
                    .Where(s =>
                           s.Channels.Where(c => c.ToString() == "event://default/deki/pages/*").Any()
                           && !s.Resources.Any()
                    ).Any(),
                10.Seconds()));

            XDoc sub2 = new XDoc("subscription-set")
                .Elem("uri.owner", mockRecipient.At("1"))
                .Start("subscription")
                    .Attr("id", "1")
                    .Elem("uri.resource", "deki://default/pages/10")
                    .Elem("channel", "event://default/deki/pages/*")
                    .Start("recipient").Attr("userid", "1").Elem("uri", "http://mock/r1/match").End()
                .End();
            _log.DebugFormat("registering sub set 2");
            dispatcher.RegisterSet("location2", sub2, "def");
            Assert.IsTrue(Wait.For(() =>
                dispatcher.CombinedSet.Subscriptions
                    .Where(s =>
                        s.Channels.Where(c => c.ToString() == "event://default/deki/pages/*").Any()
                        && s.Resources.Where(r => r.ToString() == "deki://default/pages/10").Any()
                    ).Any(),
                10.Seconds()));

            XDoc sub3 = new XDoc("subscription-set")
                .Elem("uri.owner", mockRecipient.At("2"))
                .Start("subscription")
                    .Attr("id", "2")
                    .Elem("uri.resource", "deki://default/pages/11")
                    .Elem("channel", "event://default/deki/pages/*")
                    .Start("recipient").Attr("userid", "1").Elem("uri", "http://mock/r1/miss").End()
                .End();
            _log.DebugFormat("registering sub set 3");
            dispatcher.RegisterSet("location3", sub3, "def");
            Assert.IsTrue(Wait.For(() =>
                dispatcher.CombinedSet.Subscriptions
                    .Where(s =>
                           s.Channels.Where(c => c.ToString() == "event://default/deki/pages/*").Any()
                           && s.Resources.Where(r => r.ToString() == "deki://default/pages/11").Any()
                    ).Any(),
                10.Seconds()));

            XDoc sub4 = new XDoc("subscription-set")
                .Elem("uri.owner", mockRecipient.At("3"))
                .Start("subscription")
                    .Attr("id", "3")
                    .Elem("uri.resource", "deki://default/pages/8#depth=infinity")
                    .Elem("channel", "event://default/deki/pages/*")
                    .Start("recipient").Attr("userid", "1").Elem("uri", "http://mock/r1/wildcard").End()
                .End();
            _log.DebugFormat("registering sub set 4");
            dispatcher.RegisterSet("location4", sub4, "def");
            Assert.IsTrue(Wait.For(() =>
                dispatcher.CombinedSet.Subscriptions
                    .Where(s =>
                           s.Channels.Where(c => c.ToString() == "event://default/deki/pages/*").Any()
                           && s.Resources.Where(r => r.ToString() == "deki://default/pages/8#depth%3Dinfinity").Any()
                    ).Any(),
                10.Seconds()));

            XDoc evDoc = new XDoc("deki-event")
                .Attr("wikiid", "default")
                .Elem("channel", "event://default/deki/pages/update")
                .Elem("uri", "deki://default/pages/10")
                .Elem("pageid", "10");
            var ev = new DispatcherEvent(evDoc, new XUri("event://default/deki/pages/update"), new XUri("deki://default/pages/10"), new XUri("http://default/deki/pages/10"));

            _log.DebugFormat("ready to dispatch event");
            dispatcher.Dispatch(ev);
            Assert.IsTrue(dekiPageAuthEvent.WaitOne(5000, true));
            Assert.IsTrue(authorizedRecipientCalledEv.WaitOne(5000, true));
            Assert.IsTrue(mockMatchCalledEv.WaitOne(5000, true));
            Assert.IsTrue(mockWildcardCalledEv.WaitOne(5000, true));
            Assert.AreEqual(1, dekiPageCalled);
            Assert.AreEqual(2, dekiPageAuthCalled);
            Assert.AreEqual(1, authorizedRecipientCalled);
            Assert.AreEqual(2, mockRecipientCalled);
            Assert.AreEqual(1, mockMatchCalled);
            Assert.AreEqual(1, mockWildcardCalled);
            Assert.AreEqual(evDoc, authorizedReceived);
        }

        [Test]
        public void Page_delete_events_skip_page_auth() {
            XUri mockDeki = new XUri("http://mock/deki");
            int dekiCalled = 0;
            MockPlug.Register(mockDeki, delegate(Plug p, string v, XUri u, DreamMessage r, Result<DreamMessage> r2) {
                _log.DebugFormat("mockDeki called at: {0}", u);
                dekiCalled++;
                r2.Return(DreamMessage.BadRequest("shouldn't have called deki"));
            });

            XUri authorizedRecipient = new XUri("http://mock/authorized2");
            AutoResetEvent authorizedResetEvent = new AutoResetEvent(false);
            XDoc authorizedReceived = null;
            MockPlug.Register(authorizedRecipient, delegate(Plug p, string v, XUri u, DreamMessage r, Result<DreamMessage> r2) {
                _log.DebugFormat("authorizedRecipient called at: {0}", u);
                authorizedReceived = r.ToDocument();
                authorizedResetEvent.Set();
                r2.Return(DreamMessage.Ok());
            });

            XUri mockRecipient = new XUri("http://mock/r1");
            int mockRecipientCalled = 0;
            MockPlug.Register(mockRecipient, delegate(Plug p, string v, XUri u, DreamMessage r, Result<DreamMessage> r2) {
                _log.DebugFormat("mockRecipient called at: {0}", u);
                mockRecipientCalled++;
                r2.Return(DreamMessage.Ok());
            });

            XUri dispatcherUri = new XUri("http://mock/dispatcher");
            DekiDispatcher dispatcher = new DekiDispatcher(
                new DispatcherConfig {
                    ServiceUri = dispatcherUri,
                    ServiceAccessCookie = new DreamCookie("service-key", "foo", dispatcherUri),
                    ServiceConfig = new XDoc("config").Elem("uri.deki", mockDeki).Elem("authtoken", "abc")
                },
                _mockRepository.Object
            );

            XDoc sub1 = new XDoc("subscription-set")
                .Elem("uri.owner", authorizedRecipient)
                .Start("subscription")
                    .Attr("id", "1")
                    .Elem("channel", "event://default/deki/pages/*")
                    .Start("recipient").Attr("authtoken", "abc").Elem("uri", authorizedRecipient).End()
                .End();

            Thread.Sleep(100);
            _log.DebugFormat("registering sub set 1");
            dispatcher.RegisterSet("abc", sub1, "def");
            XDoc sub2 = new XDoc("subscription-set")
                .Elem("uri.owner", mockRecipient.At("1"))
                .Start("subscription")
                    .Attr("id", "1")
                    .Elem("uri.resource", "deki://default/pages/10")
                    .Elem("channel", "event://default/deki/pages/*")
                    .Start("recipient").Attr("userid", "1").Elem("uri", "http://mock/r1/match").End()
                .End();

            Thread.Sleep(100);
            _log.DebugFormat("registering sub set 2");
            dispatcher.RegisterSet("abc", sub2, "def");

            XDoc evDoc = new XDoc("deki-event")
               .Attr("wikiid", "default")
               .Elem("channel", "event://default/deki/pages/delete")
               .Elem("uri", "deki://default/pages/10")
               .Elem("pageid", "10");
            var ev = new DispatcherEvent(evDoc, new XUri("event://default/deki/pages/delete"), new XUri("deki://default/pages/10"), new XUri("http://default/deki/pages/10"));
            _log.DebugFormat("ready to dispatch event");

            // Meh. Testing multithreaded code is wonky. This 1000ms sleep is required, otherwise the event below never fires
            Thread.Sleep(1000);
            dispatcher.Dispatch(ev);

            // since we're waiting for more than one dekiPageAuthEvent, we give it a chance to finish after the first triggers
            Assert.IsTrue(authorizedResetEvent.WaitOne(5000, false));
            Assert.AreEqual(0, dekiCalled);
            Assert.AreEqual(0, mockRecipientCalled);
            Assert.AreEqual(evDoc, authorizedReceived);
        }

        [Test]
        public void Uses_deki_to_prune_recipients() {
            XUri mockDeki = new XUri("http://mock/deki");
            int dekiCalled = 0;
            int dekipage42authCalled = 0;
            bool dekiArgsGood = false;
            int dekipage43authCalled = 0;
            MockPlug.Register(mockDeki, delegate(Plug p, string v, XUri u, DreamMessage r, Result<DreamMessage> r2) {
                _log.DebugFormat("mockDeki called at: {0}", u);
                dekiCalled++;
                dekiArgsGood = false;
                List<int> users = new List<int>();
                foreach(XDoc user in r.ToDocument()["user/@id"]) {
                    users.Add(user.AsInt.Value);
                }
                if(users.Count == 4 && users.Contains(1) && users.Contains(2) && users.Contains(3) && users.Contains(4)) {
                    dekiArgsGood = true;
                }
                DreamMessage msg = DreamMessage.Ok();
                if(u.WithoutQuery() == mockDeki.At("pages", "42", "allowed")) {
                    dekipage42authCalled++;
                    msg = DreamMessage.Ok(new XDoc("users")
                        .Start("user").Attr("id", 1).End()
                        .Start("user").Attr("id", 2).End());
                } else if(u.WithoutQuery() == mockDeki.At("pages", "43", "allowed")) {
                    dekipage43authCalled++;
                    msg = DreamMessage.Ok(new XDoc("users")
                        .Start("user").Attr("id", 3).End()
                        .Start("user").Attr("id", 4).End());
                }
                r2.Return(msg);
            });
            XUri mockRecipient = new XUri("http://mock/r1");
            int mockRecipientCalled = 0;
            XDoc received = null;
            List<string> recipients = new List<string>();
            AutoResetEvent are = new AutoResetEvent(false);
            MockPlug.Register(mockRecipient, delegate(Plug p, string v, XUri u, DreamMessage r, Result<DreamMessage> r2) {
                _log.DebugFormat("mockRecipient called at: {0}", u);
                mockRecipientCalled++;
                received = r.ToDocument();
                recipients.Clear();
                recipients.AddRange(r.Headers.DreamEventRecipients);
                are.Set();
                r2.Return(DreamMessage.Ok());
            });

            DreamServiceInfo serviceInfo = DreamTestHelper.CreateService(
                _hostInfo,
                "sid://mindtouch.com/dream/2008/10/pubsub",
                "whitelist",
                new XDoc("config")
                .Elem("uri.deki", mockDeki)
                .Start("components")
                    .Attr("context", "service")
                    .Start("component")
                        .Attr("implementation", typeof(DekiDispatcher).AssemblyQualifiedName)
                        .Attr("type", typeof(IPubSubDispatcher).AssemblyQualifiedName)
                    .End()
                .End()
            );
            XDoc sub = new XDoc("subscription-set")
                .Elem("uri.owner", mockRecipient)
                .Start("subscription")
                    .Attr("id", "1")
                    .Elem("uri.resource", "http://mock/resource/x")
                    .Elem("channel", "channel:///foo/*")
                    .Elem("uri.proxy", mockRecipient)
                    .Start("recipient").Attr("userid", "1").Elem("uri", "http://recipient/a").End()
                    .Start("recipient").Attr("userid", "2").Elem("uri", "http://recipient/b").End()
                    .Start("recipient").Attr("userid", "3").Elem("uri", "http://recipient/c").End()
                    .Start("recipient").Attr("userid", "4").Elem("uri", "http://recipient/d").End()
                .End()
                .Start("subscription")
                    .Attr("id", "2")
                    .Elem("uri.resource", "http://mock/resource/y")
                    .Elem("channel", "channel:///foo/*")
                    .Elem("uri.proxy", mockRecipient)
                    .Start("recipient").Attr("userid", "1").Elem("uri", "http://recipient/a").End()
                    .Start("recipient").Attr("userid", "2").Elem("uri", "http://recipient/b").End()
                    .Start("recipient").Attr("userid", "3").Elem("uri", "http://recipient/c").End()
                    .Start("recipient").Attr("userid", "4").Elem("uri", "http://recipient/d").End()
                .End();
            DreamMessage result = serviceInfo.WithInternalKey().AtLocalHost.At("subscribers").PostAsync(sub).Wait();
            Assert.IsTrue(result.IsSuccessful);
            XDoc ev = new XDoc("event").Elem("pageid", 42);
            result = serviceInfo.WithInternalKey()
                 .AtLocalHost
                 .At("publish")
                 .WithHeader(DreamHeaders.DREAM_EVENT_CHANNEL, "channel:///foo/bar")
                 .WithHeader(DreamHeaders.DREAM_EVENT_ORIGIN, mockDeki.ToString())
                 .WithHeader(DreamHeaders.DREAM_EVENT_RESOURCE, "http://mock/resource/x")
                 .PostAsync(ev).Wait();
            Assert.IsTrue(result.IsSuccessful);
            Assert.IsTrue(are.WaitOne(500, true));
            Assert.AreEqual(1, dekiCalled);
            Assert.AreEqual(1, dekipage42authCalled);
            Assert.IsTrue(dekiArgsGood);
            Assert.AreEqual(ev, received);
            Assert.AreEqual(2, recipients.Count);
            Assert.Contains("http://recipient/a", recipients);
            Assert.Contains("http://recipient/b", recipients);
            ev = new XDoc("event").Elem("pageid", 43);
            result = serviceInfo.WithInternalKey()
                 .AtLocalHost
                 .At("publish")
                 .WithHeader(DreamHeaders.DREAM_EVENT_CHANNEL, "channel:///foo/bar")
                 .WithHeader(DreamHeaders.DREAM_EVENT_ORIGIN, mockDeki.ToString())
                 .WithHeader(DreamHeaders.DREAM_EVENT_RESOURCE, "http://mock/resource/y")
                 .PostAsync(ev).Wait();
            Assert.IsTrue(result.IsSuccessful);
            Assert.IsTrue(are.WaitOne(5000, true));
            Assert.AreEqual(2, dekiCalled);
            Assert.AreEqual(1, dekipage42authCalled);
            Assert.IsTrue(dekiArgsGood);
            Assert.AreEqual(ev, received);
            Assert.AreEqual(2, recipients.Count);
            Assert.Contains("http://recipient/c", recipients);
            Assert.Contains("http://recipient/d", recipients);
        }

        [Test]
        public void Subscribers_to_a_single_page_do_not_get_escalated_by_a_subscriber_with_infinite_depth_on_same_page() {
            var mockDekiUri = new XUri("http://mock/deki");
            var mockDeki = MockPlug.Register(mockDekiUri);
            mockDeki.Expect().Verb("GET").Uri(mockDekiUri.At("pages", "10")).Response(DreamMessage.Ok(new XDoc("page")
                    .Attr("id", "10")
                    .Start("page.parent")
                        .Attr("id", "9")
                        .Start("page.parent")
                            .Attr("id", "8")
                            .Start("page.parent")
                                .Attr("id", "7")
                    .EndAll()));
            var dispatcherUri = new XUri("http://mock/dispatcher");
            var dispatcher = new DekiDispatcher(
                new DispatcherConfig {
                    ServiceUri = dispatcherUri,
                    ServiceAccessCookie = new DreamCookie("service-key", "foo", dispatcherUri),
                    ServiceConfig = new XDoc("config").Elem("uri.deki", mockDekiUri).Elem("authtoken", "abc")
                },
                _mockRepository.Object
            );
            var combinedSetUpdated = 0;
            dispatcher.CombinedSetUpdated += (o, e) => {
                combinedSetUpdated++;
            };

            // subscribe to page 7 and all children
            var mockHierarchyRecipientUri = new XUri("http://mock/recipient/hierarchy");
            var mockHierarchyRecipient = MockPlug.Register(mockHierarchyRecipientUri);
            mockHierarchyRecipient.Expect().Verb("POST");
            dispatcher.RegisterSet(
                "location1",
                    new XDoc("subscription-set")
                    .Elem("uri.owner", mockHierarchyRecipientUri)
                    .Start("subscription")
                        .Attr("id", "3")
                        .Elem("uri.resource", "deki://default/pages/7#depth=infinity")
                        .Elem("channel", "event://default/deki/pages/*")
                        .Start("recipient").Attr("authtoken", "abc").Elem("uri", mockHierarchyRecipientUri).End()
                    .End(),
                "def"
            );

            // subscribe to only page 7
            var mockPageonlyRecipientUri = new XUri("http://mock/recipient/pageonly");
            var mockPageonlyRecipientCalled = 0;
            MockPlug.Register(mockPageonlyRecipientUri, delegate(Plug p, string v, XUri u, DreamMessage r, Result<DreamMessage> r2) {
                _log.DebugFormat("mockPageonlyRecipient called at: {0}", u);
                mockPageonlyRecipientCalled++;
                r2.Return(DreamMessage.Ok());
            });
            dispatcher.RegisterSet(
                "location2",
                new XDoc("subscription-set")
                    .Elem("uri.owner", mockPageonlyRecipientUri)
                    .Start("subscription")
                        .Attr("id", "3")
                        .Elem("uri.resource", "deki://default/pages/7")
                        .Elem("channel", "event://default/deki/pages/*")
                        .Start("recipient").Attr("authtoken", "abc").Elem("uri", mockPageonlyRecipientUri).End()
                    .End(),
                "def"
            );

            // wait for subscriptions to be set up
            _log.DebugFormat("wait for subscriptions to be updated");
            Assert.IsTrue(
                Wait.For(() => combinedSetUpdated == 2, TimeSpan.FromSeconds(10)),
                string.Format("timeout waiting for subscriptions, expected 2, got {0} dispatches", combinedSetUpdated));

            // fire page change for page 10 (sub-child of page 7)
            XDoc evDoc = new XDoc("deki-event")
               .Attr("wikiid", "default")
               .Elem("channel", "event://default/deki/pages/update")
               .Elem("uri", "deki://default/pages/10")
               .Elem("pageid", "10");
            var ev = new DispatcherEvent(evDoc, new XUri("event://default/deki/pages/update"), new XUri("deki://default/pages/10"), new XUri("http://default/deki/pages/10"));
            _log.DebugFormat("ready to dispatch event");
            dispatcher.Dispatch(ev);

            // wait for deki to have been called
            _log.DebugFormat("wait for deki call");
            Assert.IsTrue(mockDeki.WaitAndVerify(TimeSpan.FromSeconds(10)), mockDeki.VerificationFailure);

            // wait for recipients to be notified
            _log.DebugFormat("wait for hierarchy notification");
            Assert.IsTrue(mockHierarchyRecipient.WaitAndVerify(TimeSpan.FromSeconds(10)), mockHierarchyRecipient.VerificationFailure);

            // only 'hierarchy' subscriber should have been notified
            _log.DebugFormat("make sure page only doesn't get called");
            Assert.IsFalse(Wait.For(() => mockPageonlyRecipientCalled > 0, TimeSpan.FromSeconds(5)));
        }

    }
}

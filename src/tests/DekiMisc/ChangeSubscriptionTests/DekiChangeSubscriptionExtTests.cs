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

using MindTouch.Dream;
using MindTouch.Dream.Test;
using MindTouch.Extensions.Time;
using MindTouch.Tasking;
using MindTouch.Xml;

using NUnit.Framework;

namespace MindTouch.Deki.Tests.ChangeSubscriptionTests {

    [TestFixture]
    public class DekiChangeSubscriptionExtTests {

        private static readonly log4net.ILog _log = LogUtils.CreateLog();
        private Plug _pageSub;
        private Plug _adminPlug;
        private Plug _pageSubscriberPlug;
        private string _userId;
        private string _userName;

        [TestFixtureSetUp]
        public void GlobalInit() {
            Utils.Settings.ShutdownHost();
            _adminPlug = Utils.BuildPlugForAdmin();
            _log.DebugFormat("admin plug: {0}", _adminPlug.Uri.ToString());
            _userId = null;
            _userName = null;
            UserUtils.CreateRandomContributor(_adminPlug, out _userId, out _userName);
            _log.DebugFormat("created contributor {0} ({1})", _userName, _userId);
            _pageSubscriberPlug = Utils.BuildPlugForUser(_userName);
            _log.DebugFormat("subscriber plug: {0}", _pageSubscriberPlug.Uri.ToString());
            _pageSub = _pageSubscriberPlug.At("pagesubservice");
            DreamMessage response = PageUtils.CreateRandomPage(_adminPlug);
            Assert.IsTrue(response.IsSuccessful);
        }

        [TearDown]
        public void PerTestTearDown() {
            MockPlug.DeregisterAll();
        }

        [Test]
        public void Create_view_remove_subscription() {
            string id;
            string path;
            DreamMessage response = PageUtils.CreateRandomPage(_adminPlug, out id, out path);
            Assert.IsTrue(response.IsSuccessful);

            // let the page create event bubble through
            Thread.Sleep(1000);
            _log.DebugFormat("post single page subscription: {0}", id);
            response = _pageSub.At("pages", id).WithHeader("X-Deki-Site", "id=default").PostAsync().Wait();
            Assert.IsTrue(response.IsSuccessful);

            _log.Debug("get subscription");
            response = _pageSub.At("subscriptions").With("pages", id).WithHeader("X-Deki-Site", "id=default").GetAsync().Wait();
            Assert.IsTrue(response.IsSuccessful);
            XDoc subscription = response.ToDocument()["subscription.page"];
            Assert.AreEqual(1, subscription.ListLength);
            Assert.AreEqual("0", subscription["@depth"].AsText);

            _log.Debug("post page tree subscription");
            response = _pageSub.At("pages", id).With("depth", "infinity").WithHeader("X-Deki-Site", "id=default").PostAsync().Wait();
            Assert.IsTrue(response.IsSuccessful);

            _log.Debug("get subscription");
            response = _pageSub.At("subscriptions").With("pages", id).WithHeader("X-Deki-Site", "id=default").GetAsync().Wait();
            Assert.IsTrue(response.IsSuccessful);
            subscription = response.ToDocument()["subscription.page"];
            Assert.AreEqual(1, subscription.ListLength);
            Assert.AreEqual("infinity", subscription["@depth"].AsText);

            _log.Debug("remove subscription");
            response = _pageSub.At("pages", id).WithHeader("X-Deki-Site", "id=default").DeleteAsync().Wait();
            Assert.IsTrue(response.IsSuccessful);

            _log.Debug("get subscription");
            response = _pageSub.At("subscriptions").With("pages", id).WithHeader("X-Deki-Site", "id=default").GetAsync().Wait();
            Assert.IsTrue(response.IsSuccessful);
            Assert.IsTrue(response.ToDocument()["subscription.page"].IsEmpty, response.AsText());
        }

        [Test]
        public void Subscribe_and_see_page_change_dispatched() {
            XDoc set = null, sub = null;

            // make sure we have a pagesubservice subscription to start with
            Assert.IsTrue(Wait.For(() => {
                set = Utils.Settings.HostInfo.LocalHost.At("deki", "pubsub", "subscribers").With("apikey", Utils.Settings.HostInfo.ApiKey).Get(new Result<XDoc>()).Wait();
                return set["subscription[channel='event://*/deki/pages/update']"]
                    .Where(x => x["recipient/uri"].AsText.EndsWithInvariant("deki/pagesubservice/notify")).Any();
            }, 10.Seconds()), set.ToPrettyString());

            _log.Debug("start: Subscribe_and_see_page_change_dispatched");
            var emailResetEvent = new ManualResetEvent(false);

            // subscribe to dekipubsub, so we can verify page creation event has passed
            XUri coreSubscriber = new XUri("http://mock/dekisubscriber");
            var createdPages = new HashSet<string>();
            var modifiedPages = new HashSet<string>();
            MockPlug.Register(coreSubscriber, delegate(Plug p, string v, XUri u, DreamMessage r, Result<DreamMessage> r2) {
                var doc = r.ToDocument();
                var channel = doc["channel"].AsUri;
                var pId = doc["pageid"].AsText;
                _log.DebugFormat("dekisubscriber called called with channel '{0}' for page {1}", channel, pId);
                if(channel.LastSegment == "create") {
                    lock(createdPages) {
                        createdPages.Add(pId);
                    }
                } else if(channel.LastSegment == "update") {
                    lock(modifiedPages) {
                        modifiedPages.Add(pId);
                    }
                } else {
                    _log.Info("wrong channel!");
                }
                r2.Return(DreamMessage.Ok());
            });
            XDoc subscriptionSet = new XDoc("subscription-set")
                .Elem("uri.owner", coreSubscriber)
                .Start("subscription")
                    .Elem("channel", "event://*/deki/pages/*")
                    .Start("recipient")
                        .Attr("authtoken", Utils.Settings.HostInfo.ApiKey)
                        .Elem("uri", coreSubscriber)
                    .End()
                .End();

            var subscriptionResult = Utils.Settings.HostInfo.LocalHost.At("deki", "pubsub", "subscribers").With("apikey", Utils.Settings.HostInfo.ApiKey).PostAsync(subscriptionSet).Wait();
            Assert.IsTrue(subscriptionResult.IsSuccessful);
            _log.DebugFormat("check for subscription in combined set");
            Assert.IsTrue(Wait.For(() => {
                set = Utils.Settings.HostInfo.LocalHost.At("deki", "pubsub", "subscribers").With("apikey", Utils.Settings.HostInfo.ApiKey).Get(new Result<XDoc>()).Wait();
                sub = set["subscription[channel='event://*/deki/pages/*']"];
                return sub["recipient/uri"].Contents == coreSubscriber.ToString();
            }, 10.Seconds()));
            int emailerCalled = 0;
            XDoc emailDoc = null;
            DreamMessage response;
            string pageId;
            string pagePath;

            // create a page
            _log.DebugFormat("creating page");
            response = PageUtils.CreateRandomPage(_adminPlug, out pageId, out pagePath);
            Assert.IsTrue(response.IsSuccessful);

            //give the page creation event a chance to bubble through
            _log.DebugFormat("checking for page {0} creation event", pageId);
            Assert.IsTrue(Wait.For(() => {
                Thread.Sleep(100);
                lock(createdPages) {
                    return createdPages.Contains(pageId);
                }
            }, 10.Seconds()));
            _log.DebugFormat("got create event");

            _log.DebugFormat("post page subscription for page {0}", pageId);
            response = _pageSub.At("pages", pageId).With("depth", "0").WithHeader("X-Deki-Site", "id=default").PostAsFormAsync().Wait();
            Assert.IsTrue(response.IsSuccessful);

            XUri emailerEndpoint = Utils.Settings.HostInfo.Host.LocalMachineUri.At("deki", "mailer");
            MockPlug.Register(emailerEndpoint, delegate(Plug p, string v, XUri u, DreamMessage r, Result<DreamMessage> r2) {
                var doc = r.ToDocument();
                var to = doc["to"].AsText;
                _log.DebugFormat("emailer called for: {0}", to);
                var fragment = _userName + "@";
                if(to.StartsWith(fragment) || to.Contains(fragment)) {
                    emailDoc = doc;
                    emailerCalled++;
                    emailResetEvent.Set();
                }
                r2.Return(DreamMessage.Ok());
            });

            _log.Debug("mod page");
            modifiedPages.Clear();

            response = PageUtils.SavePage(Utils.BuildPlugForAdmin(), pagePath, "foo");
            Assert.IsTrue(response.IsSuccessful);

            _log.DebugFormat("checking for page {0} update event", pageId);
            Assert.IsTrue(Wait.For(() => {
                Thread.Sleep(100);
                lock(modifiedPages) {
                    return modifiedPages.Contains(pageId);
                }
            }, 10.Seconds()));
            _log.DebugFormat("got update event");

            _log.Debug("waiting on email post");
            Assert.IsTrue(Wait.For(() => emailResetEvent.WaitOne(100, true), 10.Seconds()));
            _log.Debug("email fired");
            Assert.IsFalse(emailDoc.IsEmpty);
            Assert.AreEqual(pageId, emailDoc["pages/pageid"].AsText);
            Thread.Sleep(200);
            Assert.AreEqual(1, emailerCalled);
        }
    }
}
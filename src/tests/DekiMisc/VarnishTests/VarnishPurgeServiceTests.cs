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
using System.Threading;

using MindTouch.Dream;
using MindTouch.Dream.Test;
using MindTouch.Extensions.Time;
using MindTouch.Tasking;
using MindTouch.Xml;

using NUnit.Framework;

namespace MindTouch.Deki.Tests.VarnishTests {

    [TestFixture]
    public class VarnishPurgeServiceTests {

        private static readonly log4net.ILog _log = LogUtils.CreateLog();

        private DreamHostInfo _hostInfo;

        [SetUp]
        public void PerTestSetup() {
            _hostInfo = DreamTestHelper.CreateRandomPortHost();
        }

        [TearDown]
        public void PerTestCleanup() {
            MockPlug.DeregisterAll();
            _hostInfo = null;
            _log.Debug("cleaned up");
        }

        [Test]
        public void Service_end_to_end_no_deki() {

            // set up mocks for all the support service calls
            string apikey = "abc";
            XUri deki = new XUri("http://mock/deki");
            int dekiCalled = 0;
            MockPlug.Register(deki, delegate(Plug p, string v, XUri u, DreamMessage r, Result<DreamMessage> r2) {
                _log.DebugFormat("deki: {0}:{1}", v, u);
                dekiCalled++;
                r2.Return(DreamMessage.Ok());
            });

            XUri varnish = new XUri("http://mock/varnish");
            AutoResetEvent varnishResetEvent = new AutoResetEvent(false);
            string varnishHeader = "";
            int varnishCalled = 0;
            MockPlug.Register(varnish, delegate(Plug p, string v, XUri u, DreamMessage r, Result<DreamMessage> r2) {
                _log.DebugFormat("varnish: {0}:{1}", v, u);
                if(v == "PURGE") {
                    varnishHeader = r.Headers["X-Purge-Url"];
                    varnishCalled++;
                    varnishResetEvent.Set();
                }
                r2.Return(DreamMessage.Ok());
            });

            XUri subscribe = new XUri("http://mock/sub");
            XUri subscriptionLocation = subscribe.At("testsub");
            AutoResetEvent subscribeResetEvent = new AutoResetEvent(false);
            XUri subscribeCalledUri = null;
            XDoc subscribePosted = null;
            MockPlug.Register(subscribe, delegate(Plug p, string v, XUri u, DreamMessage r, Result<DreamMessage> r2) {
                subscribeCalledUri = u;
                if(u == subscribe.At("subscribers")) {
                    _log.Debug("creating subscription");
                    DreamMessage msg = DreamMessage.Ok(new XDoc("x"));
                    msg.Headers.Location = subscriptionLocation;
                    subscribeResetEvent.Set();
                    r2.Return(msg);
                } else {
                    _log.Debug("updating subscription");
                    subscribePosted = r.ToDocument();
                    subscribeResetEvent.Set();
                    r2.Return(DreamMessage.Ok());
                }
            });

            // set up service
            _log.Debug("set up service");
            DreamServiceInfo serviceInfo = DreamTestHelper.CreateService(
                _hostInfo,
                typeof(VarnishPurgeService),
                "varnish",
                new XDoc("config")
                    .Elem("uri.deki", deki)
                    .Elem("uri.varnish", varnish)
                    .Elem("uri.pubsub", subscribe)
                    .Elem("varnish-purge-delay", 1)
                    .Elem("apikey", apikey)
                );
            Plug service = serviceInfo.WithInternalKey().AtLocalHost;

            // expect:
            // - storage was queried
            // - subscription was created on subscribe
            Assert.IsTrue(subscribeResetEvent.WaitOne(100, true));
            Assert.AreEqual(subscribe.At("subscribers"), subscribeCalledUri);

            // post page varnish event
            service.At("queue").Post(
                new XDoc("deki-event")
                    .Attr("wikiid", "abc")
                    .Elem("channel", "event://abc/deki/pages/create")
                    .Elem("pageid", "1")
                    .Elem("path", "x/y/z"));
            Assert.IsTrue(varnishResetEvent.WaitOne(5000, false));
            Assert.AreEqual(0, dekiCalled);
            Assert.AreEqual(1, varnishCalled);
            Assert.AreEqual("^/((x/y/z|index\\.php\\?title=x/y/z)[\\?&]?|@api/deki/pages/1/?).*$", varnishHeader);
            Assert.IsTrue(Wait.For(() => varnishCalled == 1, 10.Seconds()),"varnish wasn't called");
        }

        [Test]
        public void Service_end_to_end_with_deki() {

            // set up mocks for all the support service calls
            string apikey = "abc";
            XUri deki = new XUri("http://mock/deki");
            int dekiCalled = 0;
            MockPlug.Register(deki, delegate(Plug p, string v, XUri u, DreamMessage r, Result<DreamMessage> r2) {
                _log.DebugFormat("deki: {0}:{1}", v, u);
                dekiCalled++;
                r2.Return(DreamMessage.Ok(new XDoc("page").Elem("path", "x/y/z")));
            });

            XUri varnish = new XUri("http://mock/varnish");
            AutoResetEvent varnishResetEvent = new AutoResetEvent(false);
            string varnishHeader = "";
            int varnishCalled = 0;
            MockPlug.Register(varnish, delegate(Plug p, string v, XUri u, DreamMessage r, Result<DreamMessage> r2) {
                _log.DebugFormat("varnish: {0}:{1}", v, u);
                if(v == "PURGE") {
                    varnishHeader = r.Headers["X-Purge-Url"];
                    varnishCalled++;
                    varnishResetEvent.Set();
                }
                r2.Return(DreamMessage.Ok());
            });

            XUri subscribe = new XUri("http://mock/sub");
            XUri subscriptionLocation = subscribe.At("testsub");
            AutoResetEvent subscribeResetEvent = new AutoResetEvent(false);
            XUri subscribeCalledUri = null;
            XDoc subscribePosted = null;
            MockPlug.Register(subscribe, delegate(Plug p, string v, XUri u, DreamMessage r, Result<DreamMessage> r2) {
                subscribeCalledUri = u;
                if(u == subscribe.At("subscribers")) {
                    _log.Debug("creating subscription");
                    DreamMessage msg = DreamMessage.Ok(new XDoc("x"));
                    msg.Headers.Location = subscriptionLocation;
                    subscribeResetEvent.Set();
                    r2.Return(msg);
                } else {
                    _log.Debug("updating subscription");
                    subscribePosted = r.ToDocument();
                    subscribeResetEvent.Set();
                    r2.Return(DreamMessage.Ok());
                }
            });

            // set up service
            _log.Debug("set up service");
            DreamServiceInfo serviceInfo = DreamTestHelper.CreateService(
                _hostInfo,
                typeof(VarnishPurgeService),
                "varnish",
                new XDoc("config")
                    .Elem("uri.deki", deki)
                    .Elem("uri.varnish", varnish)
                    .Elem("uri.pubsub", subscribe)
                    .Elem("varnish-purge-delay", 1)
                    .Elem("apikey", apikey)
                );
            Plug service = serviceInfo.WithInternalKey().AtLocalHost;

            // expect:
            // - storage was queried
            // - subscription was created on subscribe
            Assert.IsTrue(subscribeResetEvent.WaitOne(100, true));
            Assert.AreEqual(subscribe.At("subscribers"), subscribeCalledUri);

            // post page varnish event
            service.At("queue").Post(
                new XDoc("deki-event")
                    .Attr("wikiid", "abc")
                    .Elem("channel", "event://abc/deki/pages/create")
                    .Elem("pageid", "1"));
            Assert.IsTrue(varnishResetEvent.WaitOne(5000, false));
            Assert.AreEqual(1, dekiCalled);
            Assert.AreEqual(1, varnishCalled);
            Assert.AreEqual("^/((x/y/z|index\\.php\\?title=x/y/z)[\\?&]?|@api/deki/pages/1/?).*$", varnishHeader);
            Thread.Sleep(1000);
            Assert.AreEqual(1, varnishCalled);
            Assert.AreEqual(1, dekiCalled);
        }
    }
}

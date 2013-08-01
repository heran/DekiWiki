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
using System.Globalization;
using System.Threading;

using MindTouch.Deki.UserSubscription;
using MindTouch.Dream;
using MindTouch.Dream.Test;
using MindTouch.Tasking;
using MindTouch.Xml;

using NUnit.Framework;
using MindTouch.Extensions.Time;

namespace MindTouch.Deki.Tests.ChangeSubscriptionTests {
    using Yield = IEnumerator<IYield>;

    [TestFixture]
    public class PageChangeCacheTests {

        private static readonly log4net.ILog _log = LogUtils.CreateLog();

        [TearDown]
        public void PerTestCleanup() {
            MockPlug.DeregisterAll();
        }

        [Test]
        public void Get_page_data() {
            Plug deki = Plug.New("http://mock/deki");
            AutoMockPlug autoMock = MockPlug.Register(deki.Uri);
            PageChangeCache cache = new PageChangeCache(deki, (key, trigger) => { });
            DateTime timestamp = DateTime.Parse("2009/02/01 10:10:00");
            XUri pageUri = deki.Uri.At("pages", "10").With("redirects", "0");
            XDoc pageResponse = new XDoc("page")
                .Elem("uri.ui", "http://foo.com/@api/deki/pages/10")
                .Elem("title", "foo")
                .Elem("path", "foo/bar");
            XUri feedUri = deki.Uri
                .At("pages", "10", "feed")
                .With("redirects", "0")
                .With("format", "raw")
                .With("since", timestamp.Subtract(TimeSpan.FromSeconds(10)).ToString("yyyyMMddHHmmss"));
            XDoc changeResponse = new XDoc("table")
                .Start("change")
                .Elem("rc_summary", "Two edits")
                .Elem("rc_comment", "edit 1")
                .Elem("rc_comment", "edit 2")
                .Elem("rc_timestamp", "20090201101000")
                .End();

            _log.Debug("first get");
            autoMock.Expect("GET", pageUri, (XDoc)null, DreamMessage.Ok(pageResponse));
            autoMock.Expect("GET", feedUri, (XDoc)null, DreamMessage.Ok(changeResponse));
            PageChangeData data = Coroutine.Invoke(cache.GetPageData, (uint)10, "foo", timestamp, CultureInfo.InvariantCulture, (string)null, new Result<PageChangeData>()).Wait();
            Assert.IsTrue(autoMock.WaitAndVerify(TimeSpan.FromSeconds(10)));

            //string plainbody = "foo\r\n[ http://foo.com/@api/deki/pages/10 ]\r\n\r\n - edit 1 (Sun, 01 Feb 2009 10:10:00 GMT)\r\n   [ http://foo.com/@api/deki/pages/10?revision ]\r\n\r\n";
            XDoc htmlBody = XDocFactory.From("<html><p><b><a href=\"http://foo.com/@api/deki/pages/10\">foo</a></b> ( Last edited by <a href=\"http://foo.com/User%3A\" /> )<br /><small><a href=\"http://foo.com/@api/deki/pages/10\">http://foo.com/@api/deki/pages/10</a></small><br /><small><a href=\"http://foo.com/index.php?title=Special%3APageAlerts&amp;id=10\">Unsubscribe</a></small></p><p><ol><li>edit 1 ( <a href=\"http://foo.com/@api/deki/pages/10?revision\">Sun, 01 Feb 2009 10:10:00 GMT</a> by <a href=\"http://foo.com/User%3A\" /> )</li></ol></p><br /></html>", MimeType.TEXT_XML);
            Assert.AreEqual(htmlBody.ToString(), data.HtmlBody.ToString());
        }

        [Test]
        public void Get_page_data_in_user_timezone() {
            Plug deki = Plug.New("http://mock/deki");
            AutoMockPlug autoMock = MockPlug.Register(deki.Uri);
            PageChangeCache cache = new PageChangeCache(deki, (key, trigger) => { });
            DateTime timestamp = DateTime.Parse("2009/02/01 10:10:00");
            XUri pageUri = deki.Uri.At("pages", "10").With("redirects", "0");
            XDoc pageResponse = new XDoc("page")
                .Elem("uri.ui", "http://foo.com/@api/deki/pages/10")
                .Elem("title", "foo")
                .Elem("path", "foo/bar");
            XUri feedUri = deki.Uri
                .At("pages", "10", "feed")
                .With("redirects", "0")
                .With("format", "raw")
                .With("since", timestamp.Subtract(TimeSpan.FromSeconds(10)).ToString("yyyyMMddHHmmss"));
            XDoc changeResponse = new XDoc("table")
                .Start("change")
                .Elem("rc_summary", "Two edits")
                .Elem("rc_comment", "edit 1")
                .Elem("rc_comment", "edit 2")
                .Elem("rc_timestamp", "20090201101000")
                .End();

            _log.Debug("first get");
            autoMock.Expect("GET", pageUri, (XDoc)null, DreamMessage.Ok(pageResponse));
            autoMock.Expect("GET", feedUri, (XDoc)null, DreamMessage.Ok(changeResponse));
            PageChangeData data = Coroutine.Invoke(cache.GetPageData, (uint)10, "foo", timestamp, CultureInfo.InvariantCulture, "-09:00", new Result<PageChangeData>()).Wait();
            Assert.IsTrue(autoMock.WaitAndVerify(TimeSpan.FromSeconds(10)));

            //string plainbody = "foo\r\n[ http://foo.com/@api/deki/pages/10 ]\r\n\r\n - edit 1 (Sun, 01 Feb 2009 01:10:00 -09:00)\r\n   [ http://foo.com/@api/deki/pages/10?revision ]\r\n\r\n";
            XDoc htmlBody = XDocFactory.From("<html><p><b><a href=\"http://foo.com/@api/deki/pages/10\">foo</a></b> ( Last edited by <a href=\"http://foo.com/User%3A\" /> )<br /><small><a href=\"http://foo.com/@api/deki/pages/10\">http://foo.com/@api/deki/pages/10</a></small><br /><small><a href=\"http://foo.com/index.php?title=Special%3APageAlerts&amp;id=10\">Unsubscribe</a></small></p><p><ol><li>edit 1 ( <a href=\"http://foo.com/@api/deki/pages/10?revision\">Sun, 01 Feb 2009 01:10:00 -09:00</a> by <a href=\"http://foo.com/User%3A\" /> )</li></ol></p><br /></html>", MimeType.TEXT_XML);
            Assert.AreEqual(htmlBody.ToString(), data.HtmlBody.ToString());
        }

        [Test]
        public void Cache_hit_and_expire() {
            Plug deki = Plug.New("http://mock/deki");
            AutoMockPlug autoMock = MockPlug.Register(deki.Uri);
            Action expire = null;
            PageChangeCache cache = new PageChangeCache(deki, (key, trigger) => expire = trigger);
            DateTime timestamp = DateTime.UtcNow;
            XUri pageUri = deki.Uri.At("pages", "10").With("redirects", "0");
            XDoc pageResponse = new XDoc("page")
                .Elem("uri.ui", "http://foo.com/@api/deki/pages/10")
                .Elem("title", "foo")
                .Elem("path", "foo/bar");
            XUri feedUri = deki.Uri
                .At("pages", "10", "feed")
                .With("redirects", "0")
                .With("format", "raw")
                .With("since", timestamp.Subtract(TimeSpan.FromSeconds(10)).ToString("yyyyMMddHHmmss"));
            XDoc changeResponse = new XDoc("table")
                .Start("change")
                .Elem("rc_summary", "Two edits")
                .Elem("rc_comment", "edit 1")
                .Elem("rc_comment", "edit 2")
                .End();

            _log.Debug("first get");
            autoMock.Expect("GET", pageUri, (XDoc)null, DreamMessage.Ok(pageResponse));
            autoMock.Expect("GET", feedUri, (XDoc)null, DreamMessage.Ok(changeResponse));
            Assert.IsNotNull(Coroutine.Invoke(cache.GetPageData, (uint)10, "foo", timestamp, CultureInfo.InvariantCulture, (string)null, new Result<PageChangeData>()).Wait());
            Assert.IsTrue(autoMock.WaitAndVerify(10.Seconds()));

            _log.Debug("second get, cache hit");
            autoMock.Reset();
            Assert.IsNotNull(Coroutine.Invoke(cache.GetPageData, (uint)10, "foo", timestamp, CultureInfo.InvariantCulture, (string)null, new Result<PageChangeData>()).Wait());
            Assert.IsTrue(autoMock.WaitAndVerify(2.Seconds()));

            _log.Debug("third get, cache miss");
            autoMock.Reset();
            autoMock.Expect("GET", pageUri, (XDoc)null, DreamMessage.Ok(pageResponse));
            autoMock.Expect("GET", feedUri, (XDoc)null, DreamMessage.Ok(changeResponse));
            expire();
            Assert.IsNotNull(Coroutine.Invoke(cache.GetPageData, (uint)10, "foo", timestamp, CultureInfo.InvariantCulture, (string)null, new Result<PageChangeData>()).Wait());
            Assert.IsTrue(autoMock.WaitAndVerify(10.Seconds()));
        }

        [Test]
        public void Cache_hit_if_in_same_minute() {
            Plug deki = Plug.New("http://mock/deki");
            AutoMockPlug autoMock = MockPlug.Register(deki.Uri);
            PageChangeCache cache = new PageChangeCache(deki, (key, trigger) => { });
            DateTime t1 = DateTime.Parse("2009/02/01 10:10:00");
            DateTime t2 = DateTime.Parse("2009/02/01 10:10:30");
            DateTime t3 = DateTime.Parse("2009/02/01 10:11:00");
            XUri pageUri = deki.Uri.At("pages", "10").With("redirects", "0");
            XDoc pageResponse = new XDoc("page")
                .Elem("uri.ui", "http://foo.com/@api/deki/pages/10")
                .Elem("title", "foo")
                .Elem("path", "foo/bar");
            XUri feedUri = deki.Uri
                .At("pages", "10", "feed")
                .With("redirects", "0")
                .With("format", "raw");
            XDoc changeResponse = new XDoc("table")
                .Start("change")
                .Elem("rc_summary", "Two edits")
                .Elem("rc_comment", "edit 1")
                .Elem("rc_comment", "edit 2")
                .End();

            _log.Debug("first get");
            autoMock.Expect("GET", pageUri, (XDoc)null, DreamMessage.Ok(pageResponse));
            autoMock.Expect("GET", feedUri.With("since", t1.Subtract(TimeSpan.FromSeconds(10)).ToString("yyyyMMddHHmmss")), (XDoc)null, DreamMessage.Ok(changeResponse));
            Assert.IsNotNull(Coroutine.Invoke(cache.GetPageData, (uint)10, "foo", t1, CultureInfo.InvariantCulture, (string)null, new Result<PageChangeData>()).Wait());
            Assert.IsTrue(autoMock.WaitAndVerify(10.Seconds()));

            _log.Debug("second get, cache hit");
            autoMock.Reset();
            Assert.IsNotNull(Coroutine.Invoke(cache.GetPageData, (uint)10, "foo", t2, CultureInfo.InvariantCulture, (string)null, new Result<PageChangeData>()).Wait());
            Assert.IsTrue(autoMock.WaitAndVerify(2.Seconds()));

            _log.Debug("third get, cache miss");
            autoMock.Reset();
            autoMock.Expect("GET", pageUri, (XDoc)null, DreamMessage.Ok(pageResponse));
            autoMock.Expect("GET", feedUri.With("since", t3.Subtract(TimeSpan.FromSeconds(10)).ToString("yyyyMMddHHmmss")), (XDoc)null, DreamMessage.Ok(changeResponse));
            Assert.IsNotNull(Coroutine.Invoke(cache.GetPageData, (uint)10, "foo", t3, CultureInfo.InvariantCulture, (string)null, new Result<PageChangeData>()).Wait());
            Assert.IsTrue(autoMock.WaitAndVerify(10.Seconds()));
        }
    }
}

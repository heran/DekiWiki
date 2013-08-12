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

using System.Diagnostics;
using System.Threading;
using log4net;
using MindTouch.Deki.Script.Expr;
using NUnit.Framework;
using MindTouch.Deki.Services.Extension;
using MindTouch.Dream;
using MindTouch.Dream.Test;
using MindTouch.Xml;

namespace MindTouch.Deki.Tests.ServiceTests {

    [TestFixture]
    public class WebCacheTests {

        //--- Class Fields ---
        private static readonly ILog _log = LogUtils.CreateLog();
        private static readonly int MAX_ITERATIONS = 100;
        private const string CONTENT = "this is a test";

        //--- Fields ---
        private DreamHostInfo _hostInfo;
       
        //--- Methods ---
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
        public void Create_entries_in_cache() {
            DekiScriptLiteral result;

            // create web-cahce extension service
            var webcache = DreamTestHelper.CreateService(
                _hostInfo,
                new XDoc("config")
                    .Elem("class", typeof(WebCacheService).FullName)
                    .Elem("path", "webcache")
            );

            // extract entry points for web-cache functions
            var manifest = webcache.AtLocalHost.Get().AsDocument();
            var fetch = Plug.New(manifest["function[name/text()='fetch']/uri"].AsUri);
            var store = Plug.New(manifest["function[name/text()='store']/uri"].AsUri);
            var clear = Plug.New(manifest["function[name/text()='clear']/uri"].AsUri);

            // create MAX_ITERATIONS entries in web-cache
            var sw = Stopwatch.StartNew();
            for(int i = 1; i <= MAX_ITERATIONS; ++i) {
                var key = "key" + i;
                var list = new DekiScriptList()
                    .Add(DekiScriptExpression.Constant(key))
                    .Add(DekiScriptExpression.Constant(CONTENT));
                var response = store.Post(list.ToXml());
                var doc = response.ToDocument();
                result = DekiScriptLiteral.FromXml(doc);
                _log.DebugFormat("webcache.store('{0}') -> {1}", key, result);
            }
            sw.Stop();
            _log.DebugFormat("webcache.store() all took {0:#,##0} seconds", sw.Elapsed.TotalSeconds);

            // shutdown web-cache service
            webcache.WithPrivateKey().AtLocalHost.Delete();

            // re-create web-cache extension service
            webcache = DreamTestHelper.CreateService(
                _hostInfo,
                new XDoc("config")
                    .Elem("class", typeof(WebCacheService).FullName)
                    .Elem("path", "webcache")
            );

            // re-extract entry points for web-cache functions
            manifest = webcache.AtLocalHost.Get().AsDocument();
            fetch = Plug.New(manifest["function[name/text()='fetch']/uri"].AsUri);
            store = Plug.New(manifest["function[name/text()='store']/uri"].AsUri);
            clear = Plug.New(manifest["function[name/text()='clear']/uri"].AsUri);

            // loop over all entries in web-cache and fetch them
            sw = Stopwatch.StartNew();
            for(int i = 1; i <= MAX_ITERATIONS; ++i) {
                int count = 0;
                var key = "key" + i;
                do {
                    result = DekiScriptLiteral.FromXml(fetch.Post(new DekiScriptList().Add(DekiScriptExpression.Constant(key)).ToXml()).ToDocument());
                    var text = ((DekiScriptList)result)[0].AsString();
                    if(text == CONTENT) {
                        break;
                    }
                    Thread.Sleep(50);
                } while(++count < 100);
                if(count >= 100) {
                    Assert.Fail("too many attempts to load " + key);
                    return;
                }
            }
            sw.Stop();
            _log.DebugFormat("webcache.fetch() all took {0:#,##0} seconds", sw.Elapsed.TotalSeconds);

            // loop over all entries in web-cache and clear them out
            sw = Stopwatch.StartNew();
            for(int i = 1; i <= MAX_ITERATIONS; ++i) {
                var key = "key" + i;
                var list = new DekiScriptList()
                    .Add(DekiScriptExpression.Constant(key));
                var response = clear.Post(list.ToXml());
                var doc = response.ToDocument();
                result = DekiScriptLiteral.FromXml(doc);
                _log.DebugFormat("webcache.clear('{0}') -> {1}", key, result);
            }
            sw.Stop();
            _log.DebugFormat("webcache.clear() all took {0:#,##0} seconds", sw.Elapsed.TotalSeconds);

            // loop over all entries in web-cache and fetch them
            sw = Stopwatch.StartNew();
            for(int i = 1; i <= MAX_ITERATIONS; ++i) {
                var key = "key" + i;
                result = DekiScriptLiteral.FromXml(fetch.Post(new DekiScriptList().Add(DekiScriptExpression.Constant(key)).ToXml()).ToDocument());
                var text = ((DekiScriptList)result)[0].AsString();
                Assert.AreEqual(null, text, "entry " + key + " was not deleted");
            }
            sw.Stop();
            _log.DebugFormat("webcache.fetch() all took {0:#,##0} seconds", sw.Elapsed.TotalSeconds);

            // shutdown web-cache service again
            webcache.WithPrivateKey().AtLocalHost.Delete();
        }
    }
}

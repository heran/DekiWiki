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
using System.Text;
using MindTouch.Dream.Test;
using MindTouch.Dream.Test.Mock;
using MindTouch.Tasking;
using MindTouch.Xml;
using NUnit.Framework;
using MindTouch.Dream;

namespace MindTouch.Deki.Tests.SiteTests {
    [TestFixture]
    public class SearchTests {

        [SetUp]
        public void Setup() {
            Utils.PingServer();
            MockPlug.DeregisterAll();
        }

        [Test]
        public void GetSearch() {
            // GET:site/search
            // http://developer.mindtouch.com/Deki/API_Reference/GET%3asite%2f%2fsearch

            var p = Utils.BuildPlugForAdmin();
            var mock = MockPlug.Setup(Utils.Settings.LuceneMockUri)
                .Verb("GET")
                .At(new[] { "compact" })
                .With("wikiid", "default")
                .With("q", "+(title:\"GetSearch\"^2) +type:(wiki document image comment binary)")
                .Returns(DreamMessage.Ok(new XDoc("documents")))
                .ExpectCalls(Times.Once());
            var msg = p.At("site", "search").With("q", string.Format("title:\"GetSearch\"^2")).With("format", "xml").Get(new Result<DreamMessage>()).Wait();
            Assert.IsTrue(msg.IsSuccessful);
            mock.Verify();
        }

        [Test]
        public void GetOpensearch() {
            // GET:site/opensearch
            // ...

            var p = Utils.BuildPlugForAdmin();
            var mock = MockPlug.Setup(Utils.Settings.LuceneMockUri)
                .Verb("GET")
                .At(new[] { "compact" })
                .With("wikiid", "default")
                .With("q", "+(title:\"GetOpensearch\"^2) +type:(wiki document image comment binary)")
                .Returns(DreamMessage.Ok(new XDoc("documents")))
                .ExpectCalls(Times.Once());
            var msg = p.At("site", "opensearch").With("q", string.Format("title:\"GetOpensearch\"^2")).With("format", "xml").Get(new Result<DreamMessage>()).Wait();
            Assert.IsTrue(msg.IsSuccessful);
            mock.Verify();
        }

        [Test]
        public void GetOpensearchSuggestions() {
            // GET:site/opensearch/suggestions
            // ...

            var p = Utils.BuildPlugForAdmin();
            var mock = MockPlug.Setup(Utils.Settings.LuceneMockUri)
                .Verb("GET")
                .With("wikiid", "default")
                .With("q", "+(title:/foo/bar ) +type:(wiki document image comment binary) -namespace:\"template_talk\" -namespace:\"help\" -namespace:\"help_talk\"")
                .With("max", "100")
                .With("offset", "0")
                .With("sortBy", "-score")
                .Returns(DreamMessage.Ok(new XDoc("documents")))
                .ExpectCalls(Times.Once());
            var msg = p.At("site", "opensearch", "suggestions").With("q", "/foo/bar").Get(new Result<DreamMessage>()).Wait();
            Assert.IsTrue(msg.IsSuccessful);
            mock.Verify();
        }

        [Test]
        public void GetOpensearchDescription() {
            // GET:site/opensearch/description
            // ...

            var p = Utils.BuildPlugForAdmin();

            var msg = p.At("site", "opensearch", "description").Get();
            Assert.IsTrue(msg.IsSuccessful);
        }
    }
}

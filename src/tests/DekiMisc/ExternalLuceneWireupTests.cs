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
using MindTouch.Dream;
using MindTouch.Dream.Test;
using MindTouch.Web;
using MindTouch.Xml;
using NUnit.Framework;

namespace MindTouch.Deki.Tests {

    [TestFixture]
    public class ExternalLuceneWireupTests {

        [Test]
        public void Providing_externalLuceneUri_posts_dekipubsub_plug_info_on_that_service() {
            var mockLuceneUri = new XUri("http://mock/lucene");
            var mockLucene = MockPlug.Register(mockLuceneUri);
            XDoc pubsubPlugInfo = null;
            mockLucene.Expect()
                .Verb("POST")
                .Uri(mockLuceneUri.At("subscriptions")).RequestDocument(x => { pubsubPlugInfo = x; return true; })
                .Response(DreamMessage.Ok());
            var dekiConfig = new XDoc("config")
                    .Elem("apikey", "123")
                    .Elem("path", "deki")
                    .Elem("sid", "http://services.mindtouch.com/deki/draft/2006/11/dekiwiki")
                    .Elem("deki-path", Utils.Settings.DekiPath)
                    .Elem("deki-resources-path", Utils.Settings.DekiResourcesPath)
                    .Elem("imagemagick-convert-path", Utils.Settings.ImageMagickConvertPath)
                    .Elem("imagemagick-identify-path", Utils.Settings.ImageMagickIdentifyPath)
                    .Elem("princexml-path", Utils.Settings.PrinceXmlPath)
                    .Start("indexer").Attr("src", mockLuceneUri).End()
                    .Start("page-subscription")
                        .Elem("accumulation-time", "0")
                    .End()
                    .Start("wikis")
                        .Start("config")
                            .Attr("id", "default")
                            .Elem("host", "*")
                            .Start("page-subscription")
                                .Elem("from-address", "foo@bar.com")
                            .End()
                            .Elem("db-server", "na")
                            .Elem("db-port", "3306")
                            .Elem("db-catalog", "wikidb")
                            .Elem("db-user", "wikiuser")
                            .Start("db-password").Attr("hidden", "true").Value("password").End()
                            .Elem("db-options", "pooling=true; Connection Timeout=5; Protocol=socket; Min Pool Size=2; Max Pool Size=50; Connection Reset=false;character set=utf8;ProcedureCacheSize=25;Use Procedure Bodies=true;")
                        .End()
                    .End();
            var apikey = dekiConfig["apikey"].AsText;
            var hostInfo = DreamTestHelper.CreateRandomPortHost(new XDoc("config").Elem("apikey", apikey));
            hostInfo.Host.Self.At("load").With("name", "mindtouch.deki").Post(DreamMessage.Ok());
            hostInfo.Host.Self.At("load").With("name", "mindtouch.deki.services").Post(DreamMessage.Ok());
            var deki = DreamTestHelper.CreateService(hostInfo, dekiConfig);
            Assert.IsTrue(mockLucene.WaitAndVerify(TimeSpan.FromSeconds(10)), mockLucene.VerificationFailure);
            var pubsubPlug = Plug.New(pubsubPlugInfo["@href"].AsUri);
            foreach(var header in pubsubPlugInfo["header"]) {
                pubsubPlug.WithHeader(header["name"].AsText, header["value"].AsText);
            }
            var setCookies = DreamCookie.ParseAllSetCookieNodes(pubsubPlugInfo["set-cookie"]);
            if(setCookies.Count > 0) {
                pubsubPlug.CookieJar.Update(setCookies, null);
            }
            var subscriptionSet = new XDoc("subscription-set")
                 .Elem("uri.owner", mockLuceneUri)
                 .Start("subscription")
                     .Elem("channel", "event://*/foo")
                     .Start("recipient")
                         .Attr("authtoken", apikey)
                         .Elem("uri", mockLuceneUri)
                     .End()
                 .End();
            var subscription = pubsubPlug.At("subscribers").Post(subscriptionSet);
            Assert.AreEqual(DreamStatus.Created, subscription.Status);
        }
    }
}

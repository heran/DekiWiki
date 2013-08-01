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

using NUnit.Framework;

using MindTouch.Dream;
using MindTouch.Xml;

namespace MindTouch.Deki.Tests.SiteTests
{
    [TestFixture]
    public class ServicesTests
    {

        /* TODO: tests to add:
         * editing/deleting/stopping/starting built in service (id 1)
         * not enough access to edit service (requires admin)
         * 
         */
        

        const string TEST_SERVICE_SID = "sid://mindtouch.com/test";

        [Test]
        public void GetServices()
        {
            // GET:site/services
            // http://developer.mindtouch.com/Deki/API_Reference/GET%3asite%2f%2fservices

            Plug p = Utils.BuildPlugForAdmin();

            DreamMessage msg = p.At("site", "services").With("limit", "all").Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status);
        }

        [Test]
        [Ignore]
        public void PostServices()
        {
            // POST:site/services
            // http://developer.mindtouch.com/Deki/API_Reference/POST%3asite%2f%2fservices

            Plug p = Utils.BuildPlugForAdmin();

            DreamMessage msg = p.At("site", "services").PostAsync().Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status);
        }

        [Test]
        public void TestFullServiceLifetime() {
            Plug p = Utils.BuildPlugForAdmin();

            string desc = "test service";

            XDoc serviceXml = new XDoc("service");
            serviceXml.Elem("sid", TEST_SERVICE_SID);
            serviceXml.Elem("type", "ext");
            serviceXml.Elem("description", desc);
            serviceXml.Elem("init", "native");

            //create the service
            DreamMessage msg = p.At("site", "services").PostAsync(serviceXml).Wait();
            Assert.IsTrue(msg.IsSuccessful, "service creation failed");
            uint service_id = msg.ToDocument()["@id"].AsUInt ?? 0;
            Assert.IsTrue(service_id > 0);
            
            //todo: validate the service

            //start the service
            msg = p.At("site", "services", service_id.ToString(), "start").PostAsync().Wait();
            Assert.IsTrue(msg.IsSuccessful, "service startup failed");
            XUri uri = msg.ToDocument()["uri"].AsUri;
            Assert.IsNotNull(uri);
            Assert.IsTrue(!string.IsNullOrEmpty(uri.ToString()));

            //stop the service
            msg = p.At("site", "services", service_id.ToString(), "stop").PostAsync().Wait();
            Assert.IsTrue(msg.IsSuccessful, "service stopping failed");
            Assert.IsTrue(string.IsNullOrEmpty(msg.ToDocument()["uri"].AsText));
            msg = p.At("site", "services", service_id.ToString()).GetAsync().Wait();
            Assert.IsTrue(msg.IsSuccessful);
            serviceXml = msg.ToDocument();

            //start the service
            msg = p.At("site", "services", service_id.ToString(), "start").PostAsync().Wait();
            Assert.IsTrue(msg.IsSuccessful, "service startup failed");
            uri = msg.ToDocument()["uri"].AsUri;
            Assert.IsNotNull(uri);
            Assert.IsTrue(!string.IsNullOrEmpty(uri.ToString()));

            //start the service
            msg = p.At("site", "services", service_id.ToString(), "start").PostAsync().Wait();
            Assert.IsTrue(msg.IsSuccessful, "service refresh failed");
            uri = msg.ToDocument()["uri"].AsUri;
            Assert.IsNotNull(uri);
            Assert.IsTrue(!string.IsNullOrEmpty(uri.ToString()));
        
            //delete the service
            msg = p.At("site", "services", service_id.ToString()).DeleteAsync().Wait();
            Assert.IsTrue(msg.IsSuccessful, "service deletion failed");        
            msg = p.At("site", "services", service_id.ToString()).GetAsync().Wait();
            Assert.AreEqual(DreamStatus.NotFound, msg.Status, "service still exists after deletion");
        }

        [Test]
        public void TestPutSiteServiceId() {
            Plug p = Utils.BuildPlugForAdmin();
            XDoc serviceXml = new XDoc("service");
            serviceXml.Elem("sid", TEST_SERVICE_SID);
            serviceXml.Elem("type", "ext");
            serviceXml.Elem("description", "test1");
            serviceXml.Elem("init", "native");
            serviceXml.Start("config");
            serviceXml.Start("value").Attr("key", "keyfoo1").Value("valbar1").End();
            serviceXml.Start("value").Attr("key", "keyfoo2").Value("valbar2").End();
            serviceXml.End();

            //create the service
            DreamMessage msg = p.At("site", "services").PostAsync(serviceXml).Wait();
            Assert.IsTrue(msg.IsSuccessful, "service creation failed");
            uint service_id = msg.ToDocument()["@id"].AsUInt ?? 0;
            Assert.IsTrue(service_id > 0);
            serviceXml = msg.ToDocument();
            Assert.IsTrue(msg.ToDocument()["description"].AsText == "test1");
            Assert.IsTrue(msg.ToDocument()["config/value[@key = 'keyfoo1']"].AsText == "valbar1");
            Assert.IsTrue(msg.ToDocument()["config/value[@key = 'keyfoo2']"].AsText == "valbar2");

            //edit the service
            serviceXml["description"].Remove();
            serviceXml["config/value[@key = 'keyfoo2']"].Remove();
            serviceXml["config/value[@key = 'keyfoo1']"].Remove();
            serviceXml["config"].Start("value").Attr("key", "keyfoo1new").Value("valbar1new").End();
            serviceXml["config"].Start("value").Attr("key", "keyfoo2new").Value("valbar2new").End();
            serviceXml.Elem("description", "test2");
            msg = p.At("site", "services", service_id.ToString()).PutAsync(serviceXml).Wait();
            Assert.IsTrue(msg.IsSuccessful, "service editing failed");

            //validate edit
            msg = p.At("site", "services", service_id.ToString()).GetAsync().Wait();
            Assert.IsTrue(msg.IsSuccessful);
            Assert.IsTrue(msg.ToDocument()["description"].AsText == "test2");
            Assert.IsTrue(msg.ToDocument()["config/value[@key = 'keyfoo1new']"].AsText == "valbar1new");
            Assert.IsTrue(msg.ToDocument()["config/value[@key = 'keyfoo2new']"].AsText == "valbar2new");
            Assert.IsTrue(msg.ToDocument()["config/value[@key = 'keyfoo1']"].AsText == null);
            Assert.IsTrue(msg.ToDocument()["config/value[@key = 'keyfoo2']"].AsText == null);

            //delete the service
            msg = p.At("site", "services", service_id.ToString()).DeleteAsync().Wait();
            Assert.IsTrue(msg.IsSuccessful, "service deletion failed");
            msg = p.At("site", "services", service_id.ToString()).GetAsync().Wait();
            Assert.IsTrue(msg.Status == DreamStatus.NotFound, "service still exists after deletion");
        }

        [Test]
        public void PostServicesByID()
        {
            // POST:site/services/{id}
            // http://developer.mindtouch.com/Deki/API_Reference/POST%3asite%2f%2fservices%2f%2f%7bid%7d

            Plug p = Utils.BuildPlugForAdmin();

            DreamMessage msg = p.At("site", "services", "1").Post();
            Assert.AreEqual(DreamStatus.Ok, msg.Status);
        }

        [Test]
        public void GetServiceByID()
        {
            // GET:site/services/{id}
            // http://developer.mindtouch.com/Deki/API_Reference/GET%3asite%2f%2fservices%2f%2f%7bid%7d

            Plug p = Utils.BuildPlugForAdmin();

            DreamMessage msg = p.At("site", "services", "1").Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status);
        }
    }
}

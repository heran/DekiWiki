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

namespace MindTouch.Deki.Tests.PropertyTests {

    [TestFixture]
    public class PropertyTests {

        [Test]
        public void SaveGetPageProperty() {
            Plug p = Utils.BuildPlugForAdmin();

            string id = null;
            string path = null;
            DreamMessage msg = PageUtils.CreateRandomPage(p, out id, out path);

            string content = Utils.GetSmallRandomText();

            //NOTE: PUT: resource/{properties}/{key} is allowing property creation. Creating properties this way is undocumented and not recommended but was added for import/export
            //Test property creation via PUT
            //msg = p.At("pages", id, "properties", "foo").PutAsync(DreamMessage.Ok(MimeType.TEXT, content)).Wait();
            //Assert.AreEqual(DreamStatus.Conflict, msg.Status);

            msg = p.At("pages", id, "properties").WithHeader("Slug", XUri.Encode("foo")).PostAsync(DreamMessage.Ok(MimeType.TEXT_UTF8, content)).Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "post properties returned non 200 status: " + msg.ToString());

            //TODO: validate response XML for 200's
            msg = p.At("pages", id, "properties", "foo", "info").GetAsync().Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "get property returned non 200 status: " + msg.ToString());
            Assert.AreEqual(content, msg.ToDocument()["/property[@name= 'foo']/contents"].AsText, "Contents don't match!");

            XUri contentsUri = msg.ToDocument()["/property[@name = 'foo']/contents/@href"].AsUri;
            Assert.IsTrue(contentsUri != null, "Couldn't find content href");
            Plug p2 = Plug.New(contentsUri).WithTimeout(p.Timeout).WithHeaders(p.Headers).WithCredentials(p.Credentials);
            msg = p2.GetAsync().Wait();
            Assert.IsTrue(msg.Status == DreamStatus.Ok, "get property contents by uri didnt return status 200: " + msg.ToString());
            Assert.IsTrue(msg.ContentType.Match(MimeType.TEXT), "get property content type didnt match: " + msg.ToString());
            Assert.IsTrue(msg.AsText() == content, "get property content didnt match: " + msg.ToString());
        }

        [Test]
        public void FileUploadAndPropertyUpdate() {
            Plug p = Utils.BuildPlugForAdmin();

            string id = null;
            string path = null;
            DreamMessage msg = PageUtils.CreateRandomPage(p, out id, out path);
            string filepath = FileUtils.CreateRamdomFile(null);
            string fileName = System.IO.Path.GetFileName(filepath);
            fileName = "properties";

            //Upload file via PUT: pages/{id}/files/{filename}
            msg = p.At("pages", id, "files", "=" + XUri.DoubleEncode(fileName)).PutAsync(DreamMessage.FromFile(filepath)).Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Initial upload failed");

            string fileid = msg.ToDocument()["@id"].AsText;

            //Upload another rev of file via PUT:files/{fileid}/{filename}            
            msg = p.At("files", fileid, "=" + XUri.DoubleEncode(fileName)).PutAsync(DreamMessage.FromFile(filepath)).Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "upload via PUT:files/{fileid}/{filename} failed");
            Assert.AreEqual("file", msg.ToDocument().Name, "File upload did not return a file xml");

            //Create a property 'foo' on the file
            msg = p.At("files", fileid, "properties").WithHeader("slug", "foo").PostAsync(DreamMessage.Ok(MimeType.TEXT_UTF8, "foo content")).Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Property foo set via POST:files/{id}/properties failed");
            Assert.AreEqual("property", msg.ToDocument().Name, "property upload did not return property xml");
            string propEtag = msg.ToDocument()["@etag"].AsText;

            //Update property 'foo' using the batch feature
            string newPropertyContent = "Some new content";
            XDoc propDoc = new XDoc("properties")
                .Start("property").Attr("name", "foo").Attr("etag", propEtag)
                    .Start("contents").Attr("type", MimeType.TEXT_UTF8.ToString()).Value(newPropertyContent).End()
                .End();
            msg = p.At("files", fileid, "properties").PutAsync(propDoc).Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "batch property call failed");
            Assert.AreEqual("properties", msg.ToDocument().Name, "batch property upload did not return properties xml");
        }

        [Test]
        [Ignore]
        public void RevisionPageProperty() {
            //Create n revisions. 
            //Validate number of revisions created and content at head revisions
            //Validate content of each revision

            //TODO: description tests
            int REVS = 10;
            Plug p = Utils.BuildPlugForAdmin();

            string id = null;
            string path = null;
            DreamMessage msg = PageUtils.CreateRandomPage(p, out id, out path);

            for (int i = 1; i <= REVS; i++) {
                XDoc content = new XDoc("revtest");
                content.Start("revision").Value(i).End();
                string etag = string.Empty;

                msg = p.At("pages", id, "properties", "foo").WithHeader(DreamHeaders.ETAG, etag).PutAsync(DreamMessage.Ok(content)).Wait();
                Assert.IsTrue(msg.Status == DreamStatus.Ok, "put property returned non 200 status: " + msg.ToString());
                etag = msg.ToDocument()["/property/@etag"].AsText;

                msg = p.At("pages", id, "properties", "foo", "info").GetAsync().Wait();
                Assert.IsTrue(msg.Status == DreamStatus.Ok, "get property returned non 200 status: " + msg.ToString());
                string c = XDocFactory.From(msg.ToDocument()["/property[@name= 'foo']/contents"].AsText, MimeType.New(msg.ToDocument()["/property[@name='foo']/contents/@type"].AsText)).ToPrettyString();
                Assert.IsTrue(msg.Status == DreamStatus.Ok, "get property returned non 200 status: " + msg.ToString());
                Assert.AreEqual(content.ToPrettyString(), c, "Contents don't match!");
                Assert.AreEqual((msg.ToDocument()["/property[@name= 'foo']/revisions/@count"].AsInt ?? 0), i);
            }

            msg = p.At("pages", id, "properties", "foo", "revisions").GetAsync().Wait();
            Assert.IsTrue(msg.Status == DreamStatus.Ok, "get property revisions returned non 200 status: " + msg.ToString());
            Assert.AreEqual((msg.ToDocument()["/properties/@count"].AsInt ?? 0), REVS);

            for (int i = 1; i <= REVS; i++) {
                XDoc content = new XDoc("revtest");
                content.Start("revision").Value(i).End();

                msg = p.At("pages", id, "properties", "foo", "info").With("revision", i).GetAsync().Wait();
                Assert.IsTrue(msg.Status == DreamStatus.Ok, "get property returned non 200 status: " + msg.ToString());
                string c = XDocFactory.From(msg.ToDocument()["/property[@name= 'foo']/contents"].AsText, MimeType.New(msg.ToDocument()["/property[@name='foo']/contents/@type"].AsText)).ToPrettyString();
                Assert.IsTrue(msg.Status == DreamStatus.Ok, "get property returned non 200 status: " + msg.ToString());
                Assert.AreEqual(content.ToPrettyString(), c, "Contents don't match!");
                Assert.AreEqual((msg.ToDocument()["/property[@name= 'foo']/revisions/@count"].AsInt ?? 0), REVS);
            }
        }

        [Test]
        public void DeleteAndOverwritePageProperty() {
            //Set a property foo with content c1
            //Delete the property foo
            //Validate that foo doesn't exist
            //Set property to content c2
            //Validate property content of c2

            string C1 = "C1";
            string C2 = "C2";
            Plug p = Utils.BuildPlugForAdmin();
            string id = null;
            string path = null;
            XDoc content = null;
            DreamMessage msg = PageUtils.CreateRandomPage(p, out id, out path);

            //Set foo with c1
            content = new XDoc("deltest").Start("somevalue").Value(C1).End();
            msg = p.At("pages", id, "properties").WithHeader("Slug", XUri.Encode("foo")).PostAsync(DreamMessage.Ok(content)).Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "put property returned non 200 status: " + msg.ToString());
            msg = p.At("pages", id, "properties", "foo", "info").GetAsync().Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "get property returned non 200 status: " + msg.ToString());
            msg = p.At("pages", id, "properties", "foo").GetAsync().Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "get property contents returned non 200 status: " + msg.ToString());
            Assert.AreEqual(content.ToPrettyString(), msg.ToDocument().ToPrettyString(), "Contents don't match!");

            //Delete foo
            msg = p.At("pages", id, "properties", "foo").DeleteAsync(DreamMessage.Ok()).Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "delete property returned non 200 status: " + msg.ToString());

            //Validate that foo doesn't exist
            msg = p.At("pages", id, "properties", "foo", "info").GetAsync(DreamMessage.Ok()).Wait();
            Assert.AreEqual(DreamStatus.NotFound, msg.Status, "get deleted property returned non 404 status: " + msg.ToString());

            //Set property to content c2
            content = new XDoc("deltest").Start("somevalue").Value(C2).End();
            msg = p.At("pages", id, "properties").WithHeader("Slug", XUri.Encode("foo")).PostAsync(DreamMessage.Ok(content)).Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "put property returned non 200 status: " + msg.ToString());
            msg = p.At("pages", id, "properties", "foo", "info").GetAsync().Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "get property returned non 200 status: " + msg.ToString());

            //Validate property content of c2
            msg = p.At("pages", id, "properties", "foo").GetAsync().Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "get property contents returned non 200 status: " + msg.ToString());
            Assert.AreEqual(content.ToPrettyString(), msg.ToDocument().ToPrettyString(), "Contents don't match!");

        }

        [Test]
        public void MultiplePropertiesInPage() {
            //Save multiple properties in a page
            //Retrieve them

            Plug p = Utils.BuildPlugForAdmin();
            string id = null;
            string path = null;
            int NUMPROPS = 5;
            DreamMessage msg = PageUtils.CreateRandomPage(p, out id, out path);

            for(int i = 1; i <= NUMPROPS; i++) {
                string propname = string.Format("testprop_{0}", i);
                XDoc content = new XDoc("proptest");
                content.Start("name").Value(propname).End();

                msg = p.At("pages", id, "properties").WithHeader("Slug", XUri.Encode(propname)).PostAsync(DreamMessage.Ok(content)).Wait();
                Assert.AreEqual(DreamStatus.Ok, msg.Status, "put property returned non 200 status: " + msg.ToString());

                msg = p.At("pages", id, "properties", propname, "info").GetAsync().Wait();
                Assert.AreEqual(DreamStatus.Ok, msg.Status, "get property returned non 200 status: " + msg.ToString());
                Assert.AreEqual(MimeType.XML.ToString(), MimeType.New(msg.ToDocument()["/property[@name='" + propname + "']/contents/@type"].AsText).ToString(), "Content types dont match");

                msg = p.At("pages", id, "properties", propname).GetAsync().Wait();
                Assert.AreEqual(DreamStatus.Ok, msg.Status, "get property content returned non 200 status: " + msg.ToString());
                Assert.AreEqual(content.ToPrettyString(), msg.ToDocument().ToPrettyString(), "Contents don't match!");

                //revisions not yet supported.
                //Assert.AreEqual((msg.ToDocument()["/property[@name= '" + propname + "']/revisions/@count"].AsInt ?? 0), 1);

            }

            msg = p.At("pages", id, "properties").GetAsync(DreamMessage.Ok()).Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "get properties returned non 200 status: " + msg.ToString());
            Assert.AreEqual(msg.ToDocument()["/properties/@count"].AsInt, NUMPROPS, "Wrong property count!");
        }

        //[Test]
        public void FileProperties() {
            //TODO!
        }

        //[Test]
        public void UserProperties() {
            //TODO!
        }

        //[Test]
        public void PropPermissions() {
            //TODO!
        }

        /*TODO: batch tests:
         * duplicate name in put
         * invalid mimetype
         * Etags
         */

        [Test]
        public void PropPutBatchPositive() {
            Plug p = Utils.BuildPlugForAdmin();
            string id = null;
            string path = null;
            DreamMessage msg = PageUtils.CreateRandomPage(p, out id, out path);
            string name;
            const int NUMPROPS = 5;
            XDoc doc = new XDoc("properties");
            string nameTemplate = "prop_{0}";
            string contentTemplate = "Some value {0}";
            string descTemplate = "Soem description {0}";
            string[] etags = new string[NUMPROPS];
           
            //Create 5 new properties
            for(int i = 0; i < NUMPROPS; i++) {
                doc.Start("property").Attr("name", string.Format(nameTemplate, i))
                    .Start("contents").Attr("type", MimeType.TEXT.ToString())
                        .Value(string.Format(contentTemplate, i))
                        .End()
                    .Elem("description", string.Format(descTemplate, i))
                    .End();

            }

            msg = p.At("pages", id, "properties").PutAsync(DreamMessage.Ok(doc)).Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "put batch property returned non 200 status: " + msg.ToString());

            Assert.AreEqual(msg.ToDocument()["/properties/@count"].AsInt, NUMPROPS, "count is wrong");
            for(int i = 0; i < NUMPROPS; i++) {
                name = string.Format(nameTemplate, i);
                string etag = msg.ToDocument()["/properties/property[@name = '" + name + "']/@etag"].AsText;
                Assert.IsTrue(!string.IsNullOrEmpty(etag), string.Format("Property {0} etag empty", name));
                etags[i] = etag;
                Assert.AreEqual(msg.ToDocument()["/properties/property[@name = '" + name + "']/status/@code"].AsInt ?? 0, 200, string.Format("Property {0} status", name));
                Assert.AreEqual(msg.ToDocument()["/properties/property[@name = '" + name + "']/contents"].AsText, string.Format(contentTemplate, i), string.Format("Property {0} content", name));
                Assert.AreEqual(msg.ToDocument()["/properties/property[@name = '" + name + "']/contents/@type"].AsText, MimeType.TEXT.ToString(), string.Format("Property {0} content type", name));
                Assert.AreEqual(msg.ToDocument()["/properties/property[@name = '" + name + "']/change-description"].AsText, string.Format(descTemplate, i), string.Format("Property {0} description", name));
                
            }

            //prop 0 is getting deleted (no content)
            //prop 1 is getting updated
            //prop 2 is getting updated with same content

            string PROP1CONTENT = "some new content";
            doc = new XDoc("properties")
                .Start("property").Attr("name", string.Format(nameTemplate, 0)).Attr("etag", etags[0]).End()
                .Start("property").Attr("name", string.Format(nameTemplate, 1)).Attr("etag", etags[1])
                    .Start("contents").Attr("type", MimeType.TEXT.ToString()).Value(PROP1CONTENT).End()
                .End()
                .Start("property").Attr("name", string.Format(nameTemplate, 2)).Attr("etag", etags[2])
                    .Start("contents").Attr("type", MimeType.TEXT.ToString()).Value(string.Format(contentTemplate, 2)).End()
                .End();

            msg = p.At("pages", id, "properties").PutAsync(DreamMessage.Ok(doc)).Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "put batch property request #2 returned non 200 status: " + msg.ToString());

            //prop 0: deleted
            name = string.Format(nameTemplate, 0);
            Assert.AreEqual(msg.ToDocument()["/properties/property[@name = '" + name + "']/status/@code"].AsInt ?? 0, 200, string.Format("Property {0} status after delete", name));
            Assert.IsTrue((msg.ToDocument()["/properties/property[@name = '" + name + "']/user.deleted/@id"].AsInt ?? 0) > 0, string.Format("Property {0} delete user", name));
            Assert.IsTrue(msg.ToDocument()["/properties/property[@name = '" + name + "']/date.deleted"].AsDate != null, string.Format("Property {0} delete date", name));

            //prop 1: updated
            name = string.Format(nameTemplate, 1);
            Assert.AreEqual(msg.ToDocument()["/properties/property[@name = '" + name + "']/status/@code"].AsInt ?? 0, 200, string.Format("Property {0} status after update", name));
            Assert.AreEqual(msg.ToDocument()["/properties/property[@name = '" + name + "']/contents"].AsText, PROP1CONTENT, string.Format("Property {0} content after update", name));

            //TODO test for prop2 when content isn't updated.

        }

        [Test]
        public void PropGetContentPreviewsPositive() {
            Plug p = Utils.BuildPlugForAdmin();
            string id = null;
            string path = null;
            DreamMessage msg = PageUtils.CreateRandomPage(p, out id, out path);
            int NUMPROPS = 5;
            XDoc doc = new XDoc("properties");
            string contentLong = Utils.GetRandomTextByAlphabet(5000);
            string contentShort = "Some content";
            string descTemplate = "Soem description {0}";

                
            //Short content
                doc.Start("property").Attr("name", "prop_short")
                    .Start("contents").Attr("type", MimeType.TEXT.ToString())
                        .Value(contentShort)
                        .End()
                    .End();

            //Long content
                doc.Start("property").Attr("name", "prop_long")
                    .Start("contents").Attr("type", MimeType.TEXT.ToString())
                        .Value(contentLong)
                        .End()
                    .End();                    

            msg = p.At("pages", id, "properties").PutAsync(DreamMessage.Ok(doc)).Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "put batch property returned non 200 status: " + msg.ToString());

            msg = p.At("pages", id, "properties").GetAsync(DreamMessage.Ok()).Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "get properties returned non 200 status: " + msg.ToString());
        }

        [Ignore("PUT: resource/{properties} is allowing property creation. Creating properties this way is undocumented and not recommended but was added for import/export.")]
        [Test]
        public void NewPropertyWithEtagNegative() {
            Plug p = Utils.BuildPlugForAdmin();
            string id = null;
            string path = null;
            DreamMessage msg = PageUtils.CreateRandomPage(p, out id, out path);

            
            XDoc doc = new XDoc("properties");
            string contentShort = "Some content";

            doc.Start("property").Attr("name", "prop_short").Attr("etag", "some-unexpected-etag")
                .Start("contents").Attr("type", MimeType.TEXT.ToString())
                    .Value(contentShort)
                    .End()
                .End();

            msg = p.At("pages", id, "properties").PutAsync(DreamMessage.Ok(doc)).Wait();
            Assert.AreEqual(DreamStatus.MultiStatus, msg.Status, "put batch property returned 200 status but expected MultiStatus");

            Assert.AreEqual("409", msg.ToDocument()["/properties/property[@name='prop_short']/status/@code"].AsText);
            msg = p.At("pages", id, "properties").WithHeader("Slug", XUri.Encode("foo")).WithHeader(DreamHeaders.ETAG, "some-etag").PostAsync(DreamMessage.Ok(MimeType.TEXT, "blah")).Wait();
            Assert.AreEqual(DreamStatus.Conflict, msg.Status, "put batch property returned 200 status but expected : " + msg.ToString());
        }

        [Test]
        public void GetPageProperties()
        {
            Plug p = Utils.BuildPlugForAdmin();

            string id = null;
            string path = null;
            DreamMessage msg = PageUtils.CreateRandomPage(p, out id, out path);

            msg = p.At("pages", id, "properties").GetAsync().Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status);

            msg = p.At("pages", "=" + XUri.DoubleEncode(path), "properties").GetAsync().Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status);

            PageUtils.DeletePageByID(p, id, true);
        }

        [Test]
        public void PutPageProperties()
        {
            Plug p = Utils.BuildPlugForAdmin();

            string id = null;
            string path = null;
            DreamMessage msg = PageUtils.CreateRandomPage(p, out id, out path);

            string propertyContent = Utils.GetSmallRandomText();
            string propertyName = Utils.GenerateUniqueName();

            msg = p.At("pages", id, "properties").WithHeader("Slug", XUri.Encode(propertyName)).PostAsync(DreamMessage.Ok(MimeType.TEXT_UTF8, propertyContent)).Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status);
            Assert.AreEqual(msg.ToDocument()["/property/contents"].AsText, propertyContent, "Contents don't match!");

            msg = p.At("pages", id, "properties", propertyName).GetAsync().Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status);
            Assert.AreEqual(propertyContent, msg.AsText(), "Contents don't match!");

            PageUtils.DeletePageByID(p, id, true);
        }

        [Test]
        public void PutPagePropertiesXml() {
            Plug p = Utils.BuildPlugForAdmin();

            string id = null;
            string path = null;
            DreamMessage msg = PageUtils.CreateRandomPage(p, out id, out path);

            var propertyContent = new XDoc("foo").Start("someelement").Attr("justanattribute", true).End();
            var propXml = new XDoc("properties")
  .Start("property").Attr("name", "foo")
  .Start("contents").Attr("type", MimeType.XML.ToString())
      .Value(propertyContent) // the xml content is not escaped!
      .End()
  .Elem("description", "whattup")
  .End();

            msg = p.At("pages", id, "properties").PutAsync(DreamMessage.Ok(propXml)).Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status);
            msg = p.At("pages", id, "properties", "foo").GetAsync().Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status);
            Assert.AreEqual(propertyContent.ToPrettyString(), msg.ToDocument().ToPrettyString(), "Contents don't match!");

            PageUtils.DeletePageByID(p, id, true);
        }

        [Test]
        public void GetFileProperties()
        {
            Plug p = Utils.BuildPlugForAdmin();

            string id = null;
            string path = null;
            DreamMessage msg = PageUtils.CreateRandomPage(p, out id, out path);

            string fileid = null;
            msg = FileUtils.UploadRandomFile(p, id, out fileid);

            msg = p.At("files", fileid, "properties").GetAsync().Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status);

            PageUtils.DeletePageByID(p, id, true);
        }

        [Test]
        public void PutFileProperties()
        {
            Plug p = Utils.BuildPlugForAdmin();

            string id = null;
            string path = null;
            DreamMessage msg = PageUtils.CreateRandomPage(p, out id, out path);

            string propertyContent = Utils.GetSmallRandomText();
            string propertyName = Utils.GenerateUniqueName();

            string fileid = null;
            msg = FileUtils.UploadRandomFile(p, id, out fileid);

            msg = p.At("files", fileid, "properties").WithHeader("Slug", XUri.Encode(propertyName)).PostAsync(DreamMessage.Ok(MimeType.TEXT_UTF8, propertyContent)).Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status);
            Assert.AreEqual(msg.ToDocument()["/property/contents"].AsText, propertyContent, "Contents don't match!");

            msg = p.At("files", fileid, "properties", propertyName).GetAsync().Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Non 200 status on get:property content: "+msg.ToString());
            Assert.AreEqual(propertyContent, msg.AsText(), "Contents don't match!");

            PageUtils.DeletePageByID(p, id, true);
        }

        [Test]
        public void GetUserProperties()
        {
            Plug p = Utils.BuildPlugForAdmin();

            string id = null;
            DreamMessage msg = UserUtils.CreateRandomContributor(p, out id);

            msg = p.At("users", id, "properties").GetAsync().Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status);
        }

        [Test]
        public void PostUserProperties()
        {
            Plug p = Utils.BuildPlugForAdmin();

            string id = null;
            DreamMessage msg = UserUtils.CreateRandomContributor(p, out id);

            string propertyContent = Utils.GetSmallRandomText();
            string propertyName = Utils.GenerateUniqueName();

            msg = p.At("users", id, "properties").WithHeader("Slug", XUri.Encode(propertyName)).PostAsync(DreamMessage.Ok(MimeType.TEXT_UTF8, propertyContent)).Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status);
            Assert.AreEqual(msg.ToDocument()["/property/contents"].AsText, propertyContent, "Contents don't match!");

            msg = p.At("users", id, "properties", propertyName).GetAsync().Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status);
            Assert.AreEqual(propertyContent, msg.AsText(), "Contents don't match!");
        }

        [Test]
        public void PostPutDeleteSiteProperties() {
            Plug p = Utils.BuildPlugForAdmin();
            string propertyContent = Utils.GetSmallRandomText();
            string propertyName = Utils.GenerateUniqueName();
            
            DreamMessage msg = null;
            try {
                msg = p.At("site", "properties").WithHeader("Slug", XUri.Encode(propertyName)).PostAsync(DreamMessage.Ok(MimeType.TEXT_UTF8, propertyContent)).Wait();
                Assert.AreEqual(DreamStatus.Ok, msg.Status, "POST property got non 200");
                Assert.AreEqual(msg.ToDocument()["/property/contents"].AsText, propertyContent, "Contents don't match!");
                
                msg = p.At("site", "properties", propertyName).GetAsync().Wait();
                Assert.AreEqual(DreamStatus.Ok, msg.Status, "GET property returned non 200");
                Assert.AreEqual(propertyContent, msg.AsText(), "Contents don't match!");

                propertyContent = Utils.GetSmallRandomText();
                msg = p.At("site", "properties", propertyName).WithHeader(DreamHeaders.ETAG, msg.Headers.ETag).PutAsync(DreamMessage.Ok(MimeType.TEXT_UTF8, propertyContent)).Wait();
                Assert.AreEqual(DreamStatus.Ok, msg.Status, "PUT property returned non 200");

                msg = p.At("site", "properties", propertyName).GetAsync().Wait();
                Assert.AreEqual(DreamStatus.Ok, msg.Status, "GET property returned non 200");
                Assert.AreEqual(propertyContent, msg.AsText(), "Contents don't match on second rev!");
            
            } finally {
                msg = p.At("site", "properties", propertyName).DeleteAsync().Wait();
                Assert.AreEqual(DreamStatus.Ok, msg.Status, "Delete status non 200");
            }
            msg = p.At("site", "properties", propertyName).GetAsync().Wait();
            Assert.AreEqual(DreamStatus.NotFound, msg.Status, "Deleted property get status non 404");

        }

    }
}

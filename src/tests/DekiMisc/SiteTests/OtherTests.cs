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
using log4net;
using log4net.Config;
using NUnit.Framework;
using MindTouch.Dream;

namespace MindTouch.Deki.Tests.SiteTests
{
    [TestFixture]
    public class OtherTests
    {

        /// <summary>
        ///     Retrieve list of site functions
        /// </summary>        
        /// <feature>
        /// <name>GET:site/functions</name>
        /// <uri>http://developer.mindtouch.com/Deki/API_Reference/GET%3asite%2f%2ffunctions</uri>
        /// </feature>
        /// <expected>200 OK HTTP response</expected>

        [Test]
        public void GetFunctions()
        {
            // 1. Retrieve site functions list
            // (2) Assert the list retrieved successfully (200 OK HTTP repsonse)

            Plug p = Utils.BuildPlugForAdmin();

            DreamMessage msg = p.At("site", "functions").Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Page retrieval failed");
        }

        /// <summary>
        ///     Retrieve site feed
        /// </summary>        
        /// <feature>
        /// <name>GET:site/feed</name>
        /// <uri>http://developer.mindtouch.com/Deki/API_Reference/GET%3asite%2f%2ffeed</uri>
        /// </feature>
        /// <expected>200 OK HTTP response</expected>

        [Test]
        public void GetFeed()
        {
            // 1. Retrieve site feed
            // (2) Assert the feed retrieved successfully (200 OK HTTP repsonse)

            Plug p = Utils.BuildPlugForAdmin();

            DreamMessage msg = p.At("site", "feed").Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Page retrieval failed");
        }

        /// <summary>
        ///     Test site logo features (GET, DELETE, PUT)
        /// </summary>        
        /// <feature>
        /// <name>GET:logo.png</name>
        /// <uri>http://developer.mindtouch.com/en/ref/MindTouch_API/GET%3asite%2f%2flogo.png</uri>
        /// </feature>
        /// <expected>200 OK HTTP response</expected>

        [Ignore] // Need to take account for an existing logo
        [Test]
        public void LogoTest() // TODO: split
        {
            // GET:logo.png
            // ...

            // 1. Retrieve logo
            // (2) Assert logo retrieved successfully (200 OK HTTP response)
            // 3. Generate a PNG image
            // 4. Upload as logo
            // (5) Assert logo uploaded successfully (200 OK HTTP response)
            // 6. Retrieve logo once again
            // (7) Assert retrieved logo matches uploaded logo
            // 8. Delete logo
            // (9) Assert logo delete was successful (200 OK HTTP response)
            // 10. Retrieve logo once more
            // (11) Assert 404 Not Found HTTP response

            Plug p = Utils.BuildPlugForAdmin();

            DreamMessage msg = p.At("site", "logo.png").Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Logo retrieval failed");

            // PUT:site/logo
            // ...

            byte[] imageData = null;

            System.Drawing.Bitmap pic = new System.Drawing.Bitmap(100, 100);
            using (System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(pic))
                g.DrawRectangle(System.Drawing.Pens.Blue, 10, 10, 80, 80);
            System.IO.MemoryStream stream = new System.IO.MemoryStream();
            pic.Save(stream, System.Drawing.Imaging.ImageFormat.Png);

            imageData = stream.ToArray();
            msg = p.At("site", "logo").Put(DreamMessage.Ok(MimeType.PNG, imageData));
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Logo upload (PUT) failed");

            // GET:site/logo
            // ...
            
            msg = p.At("site", "logo").Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status);

            Utils.ByteArraysAreEqual(msg.AsBytes(), imageData);

            // GET:site/logo.png
            // ...

            msg = p.At("site", "logo.png").Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Logo retrieval failed");

            Utils.ByteArraysAreEqual(msg.AsBytes(), imageData);

            // DELETE:site/logo
            // ...

            msg = p.At("site", "logo").Delete();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Logo deletion failed");

            msg = p.At("site", "logo").GetAsync().Wait();
            Assert.AreEqual(DreamStatus.NotFound, msg.Status, "Logo successfully retrieved after deletion?!");
        }
    }
}

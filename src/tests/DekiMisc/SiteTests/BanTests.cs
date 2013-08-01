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
using MindTouch.Tasking;
using NUnit.Framework;

using MindTouch.Dream;
using MindTouch.Xml;

namespace MindTouch.Deki.Tests.SiteTests
{
    [TestFixture]
    public class BanTests
    {
        const string banRevokemask = "9223372036854779902";

        [Test]
        public void GetBans()
        {
            // GET:site/bans
            // ...

            Plug p = Utils.BuildPlugForAdmin();

            DreamMessage msg = p.At("site", "bans").Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status);
        }

        [Test]
        public void CreatingPagesByBanned()
        {
            //Assumptions: 
            // 
            //Actions:
            // Create user1 as contributor
            // Ban user1
            // Try to create page from user1
            //Expected result: 
            // Forbidden

            Plug p = Utils.BuildPlugForAdmin();

            string userId = null;
            string userName = null;
            DreamMessage msg = UserUtils.CreateRandomContributor(p, out userId, out userName);


            XDoc ban = new XDoc("bans")
                .Elem("description", Utils.GetSmallRandomText())
                .Elem("date.expires", DateTime.Now.AddDays(10))
                .Start("permissions.revoked")
                    .Elem("operations", banRevokemask)
                .End()
                .Start("ban.users")
                    .Start("user").Attr("id", userId).End()
                .End();

            msg = p.At("site", "bans").Post(ban);
            Assert.AreEqual(DreamStatus.Ok, msg.Status);

            p = Utils.BuildPlugForUser(userName, UserUtils.DefaultUserPsw);
            
            string pageTitle = "=" + XUri.DoubleEncode(PageUtils.GenerateUniquePageName());
            string content = Utils.GetSmallRandomText();

            msg = p.At("pages", pageTitle, "contents").With("edittime", Utils.DateToString(DateTime.MinValue)).
                PostAsync(DreamMessage.Ok(MimeType.TEXT_UTF8, content)).Wait();
            Assert.AreEqual(DreamStatus.Forbidden, msg.Status);
        }

        [Test]
        public void PostBanByIP()
        {
            // POST:site/bans
            // ...

            Plug p = Utils.BuildPlugForAdmin();

            XDoc ban = new XDoc("bans")
                .Elem("description", Utils.GetSmallRandomText())
                .Elem("date.expires", DateTime.Now.AddDays(10))
                .Start("permissions.revoked")
                    .Elem("operations", banRevokemask)
                .End()
                .Start("ban.addresses")
                    .Elem("address", "192.168.0.1")
                .End();

            DreamMessage msg = p.At("site", "bans").Post(ban);
            Assert.AreEqual(DreamStatus.Ok, msg.Status);

            string banid = msg.ToDocument()["@id"].AsText;
            Assert.IsTrue(!string.IsNullOrEmpty(banid));

            msg = p.At("site", "bans").Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status);
            Assert.IsFalse(msg.ToDocument()[string.Format("ban[@id=\"{0}\"]", banid)].IsEmpty);

            msg = p.At("site", "bans", banid).Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status);
            Assert.AreEqual(banid, msg.ToDocument()["@id"].AsText);
        }

        [Test]
        public void PostBanToLoopbackIPAddress()
        {
            // POST:site/bans

            Plug p = Utils.BuildPlugForAdmin();

            XDoc ban = new XDoc("bans")
                .Elem("description", Utils.GetSmallRandomText())
                .Elem("date.expires", DateTime.Now.AddDays(10))
                .Start("permissions.revoked")
                    .Elem("operations", "READ,BROWSE")
                .End()
                .Start("ban.addresses")
                    .Elem("address", "127.0.0.1")
                .End();

            DreamMessage response = p.At("site", "bans").Post(ban, new Result<DreamMessage>()).Wait();
            Assert.AreEqual(DreamStatus.Forbidden, response.Status, "Failed to prevent banning loopback ip address");
        }

        [Test]
        public void PostBanByUserID()
        {
            // POST:site/bans
            // ...

            Plug p = Utils.BuildPlugForAdmin();

            string userid = null;
            DreamMessage msg = UserUtils.CreateRandomContributor(p, out userid);

            XDoc ban = new XDoc("bans")
                .Elem("description", Utils.GetSmallRandomText())
                .Elem("date.expires", DateTime.Now.AddDays(10))
                .Start("permissions.revoked")
                    .Elem("operations", banRevokemask)
                .End()
                .Start("ban.users")
                    .Start("user").Attr("id", userid).End()
                .End();

            msg = p.At("site", "bans").Post(ban);
            Assert.AreEqual(DreamStatus.Ok, msg.Status);

            string banid = msg.ToDocument()["@id"].AsText;
            Assert.IsTrue(!string.IsNullOrEmpty(banid));

            msg = p.At("site", "bans").Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status);
            Assert.IsFalse(msg.ToDocument()[string.Format("ban[@id=\"{0}\"]", banid)].IsEmpty);

            msg = p.At("site", "bans", banid).Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status);
            Assert.AreEqual(banid, msg.ToDocument()["@id"].AsText);
        }

        [Test]
        public void DeleteBan()
        {
            // DELETE:site/bans
            // ...

            Plug p = Utils.BuildPlugForAdmin();

            XDoc ban = new XDoc("bans")
                .Elem("description", Utils.GetSmallRandomText())
                .Elem("date.expires", DateTime.Now.AddDays(10))
                .Start("permissions.revoked")
                    .Elem("operations", banRevokemask)
                .End()
                .Start("ban.addresses")
                    .Elem("address", "192.168.0.1")
                .End();

            DreamMessage msg = p.At("site", "bans").Post(ban);
            Assert.AreEqual(DreamStatus.Ok, msg.Status);

            string banid = msg.ToDocument()["@id"].AsText;
            Assert.IsTrue(!string.IsNullOrEmpty(banid));

            msg = p.At("site", "bans", banid).Delete();
            Assert.AreEqual(DreamStatus.Ok, msg.Status);

            msg = p.At("site", "bans").Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status);
            Assert.IsTrue(msg.ToDocument()[string.Format("ban[@id=\"{0}\"]", banid)].IsEmpty);

            msg = p.At("site", "bans", banid).GetAsync().Wait();
            Assert.AreEqual(DreamStatus.NotFound, msg.Status);
        }

        [Test]
        public void CreatingPageByUnbanned()
        {
            //Assumptions: 
            // 
            //Actions:
            // Create user as contributor
            // Ban user
            // Unban user
            // Try to create page from user
            //Expected result: 
            // Ok

            Plug p = Utils.BuildPlugForAdmin();

            string userId = null;
            string userName = null;
            DreamMessage msg = UserUtils.CreateRandomContributor(p, out userId, out userName);


            XDoc ban = new XDoc("bans")
                .Elem("description", Utils.GetSmallRandomText())
                .Elem("date.expires", DateTime.Now.AddDays(10))
                .Start("permissions.revoked")
                    .Elem("operations", banRevokemask)
                .End()
                .Start("ban.users")
                    .Start("user").Attr("id", userId).End()
                .End();

            msg = p.At("site", "bans").Post(ban);
            Assert.AreEqual(DreamStatus.Ok, msg.Status);


            string banid = msg.ToDocument()["@id"].AsText;
            Assert.IsTrue(!string.IsNullOrEmpty(banid));

            msg = p.At("site", "bans", banid).Delete();
            Assert.AreEqual(DreamStatus.Ok, msg.Status);

            p = Utils.BuildPlugForUser(userName, UserUtils.DefaultUserPsw);

            string pageTitle = "=" + XUri.DoubleEncode(PageUtils.GenerateUniquePageName());
            string content = Utils.GetSmallRandomText();
            msg = p.At("pages", pageTitle, "contents").With("edittime", Utils.DateToString(DateTime.MinValue)).
                PostAsync(DreamMessage.Ok(MimeType.TEXT_UTF8, content)).Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status);


            string pageId = msg.ToDocument()["page/@id"].AsText;
            p = Utils.BuildPlugForAdmin();
            PageUtils.DeletePageByID(p, pageId, true);
        }

        [Test]
        public void AddFileFromBanned()
        {
            //Assumptions: 
            // 
            //Actions:
            // Create page
            // Create user as contributor
            // Ban user
            // Try to upload file to page from banned user
            //Expected result:
            // Forbidden

            Plug p = Utils.BuildPlugForAdmin();

            string pageId = null;
            DreamMessage msg = PageUtils.CreateRandomPage(p, out pageId);

            string userId = null;
            string userName = null;
            msg = UserUtils.CreateRandomContributor(p, out userId, out userName);

            XDoc ban = new XDoc("bans")
                .Elem("description", Utils.GetSmallRandomText())
                .Elem("date.expires", DateTime.Now.AddDays(10))
                .Start("permissions.revoked")
                    .Elem("operations", banRevokemask)
                .End()
                .Start("ban.users")
                    .Start("user").Attr("id", userId).End()
                .End();

            msg = p.At("site", "bans").Post(ban);
            Assert.AreEqual(DreamStatus.Ok, msg.Status);

            p = Utils.BuildPlugForUser(userName, UserUtils.DefaultUserPsw);

            try
            {
                string fileid = null;
                string filename = null;
                msg = FileUtils.UploadRandomFile(p, pageId, out fileid, out filename);
                Assert.IsTrue(false);
            }
            catch (DreamResponseException ex)
            {
                Assert.AreEqual(DreamStatus.Forbidden, ex.Response.Status);
            }


            p = Utils.BuildPlugForAdmin();
            PageUtils.DeletePageByID(p, pageId, true);
        }

        [Test]
        public void AddFileFromUnbanned()
        {
            //Assumptions: 
            // 
            //Actions:
            // Create page
            // Create user as contributor
            // Ban user
            // Unban user
            // Try to upload file to page from banned user
            //Expected result:
            // Ok

            Plug p = Utils.BuildPlugForAdmin();

            string pageId = null;
            DreamMessage msg = PageUtils.CreateRandomPage(p, out pageId);


            string userId = null;
            string userName = null;
            msg = UserUtils.CreateRandomContributor(p, out userId, out userName);

            XDoc ban = new XDoc("bans")
                .Elem("description", Utils.GetSmallRandomText())
                .Elem("date.expires", DateTime.Now.AddDays(10))
                .Start("permissions.revoked")
                    .Elem("operations", banRevokemask)
                .End()
                .Start("ban.users")
                    .Start("user").Attr("id", userId).End()
                .End();

            msg = p.At("site", "bans").Post(ban);
            Assert.AreEqual(DreamStatus.Ok, msg.Status);


            string banid = msg.ToDocument()["@id"].AsText;
            Assert.IsTrue(!string.IsNullOrEmpty(banid));

            msg = p.At("site", "bans", banid).Delete();
            Assert.AreEqual(DreamStatus.Ok, msg.Status);

            p = Utils.BuildPlugForUser(userName, UserUtils.DefaultUserPsw);

            string fileid = null;
            string filename = null;
            msg = FileUtils.UploadRandomFile(p, pageId, out fileid, out filename);
            Assert.AreEqual(DreamStatus.Ok, msg.Status);


            p = Utils.BuildPlugForAdmin();
            PageUtils.DeletePageByID(p, pageId, true);
        }

        [Test]
        public void CheckBannedUsersPerms() {
            //Assumptions: 
            // 
            //Actions:
            // Create user1 as contributor
            // Ban user1
            // create public page p1
            // Call POST: pages/{p1}/allowed with user1
            //Expected result: 
            // user1 should not have access to p1

            Plug p = Utils.BuildPlugForAdmin();

            string userId = null;
            string userName = null;
            DreamMessage msg = UserUtils.CreateRandomContributor(p, out userId, out userName);

            // confirm user has perms
            msg = p.At("users", userId).Get();
            Assert.IsTrue((msg.AsDocument()["permissions.effective/operations"].AsText ?? string.Empty).ContainsInvariantIgnoreCase("UPDATE"), "user doesnt have expected perms");

            // ban the user
            XDoc ban = new XDoc("bans")
                .Elem("description", Utils.GetSmallRandomText())
                .Elem("date.expires", DateTime.Now.AddDays(10))
                .Start("permissions.revoked")
                    .Elem("operations", banRevokemask)
                .End()
                .Start("ban.users")
                    .Start("user").Attr("id", userId).End()
                .End();
            msg = p.At("site", "bans").Post(ban);
            Assert.AreEqual(DreamStatus.Ok, msg.Status);

            // confirm user has perms
            msg = p.At("users", userId).Get();
            Assert.IsFalse((msg.AsDocument()["permissions.effective/operations"].AsText ?? string.Empty).ContainsInvariantIgnoreCase("UPDATE"), "user doesnt have expected perms");            
        }
    }
}

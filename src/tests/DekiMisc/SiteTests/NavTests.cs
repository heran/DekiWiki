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

using NUnit.Framework;
using MindTouch.Dream;

namespace MindTouch.Deki.Tests.SiteTests
{
    [TestFixture]
    public class NavTests
    {
        [Test]
        public void GetFull()
        {
            // GET:site/nav/{pageid}/full
            // http://developer.mindtouch.com/Deki/API_Reference/GET%3asite%2f%2fnav%2f%2f%7bpageid%7d%2f%2ffull

            Plug p = Utils.BuildPlugForAdmin();

            string id = null;
            DreamMessage msg = PageUtils.CreateRandomPage(p, out id);

            msg = p.At("site", "nav", id, "full").Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status);

            PageUtils.DeletePageByID(p, id, true);
        }

        [Test]
        public void GetSiblings()
        {
            // GET:site/nav/{pageid}/siblings
            // http://developer.mindtouch.com/Deki/API_Reference/GET%3asite%2f%2fnav%2f%2f%7bpageid%7d%2f%2fsiblings

            Plug p = Utils.BuildPlugForAdmin();

            string id = null;
            DreamMessage msg = PageUtils.CreateRandomPage(p, out id);

            msg = p.At("site", "nav", id, "siblings").Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status);

            PageUtils.DeletePageByID(p, id, true);
        }

        [Test]
        public void GetChildren()
        {
            // GET:site/nav/{pageid}/children
            // http://developer.mindtouch.com/Deki/API_Reference/GET%3asite%2f%2fnav%2f%2f%7bpageid%7d%2f%2fchildren

            Plug p = Utils.BuildPlugForAdmin();

            string id = null;
            DreamMessage msg = PageUtils.CreateRandomPage(p, out id);

            msg = p.At("site", "nav", id, "children").Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status);

            PageUtils.DeletePageByID(p, id, true);
        }

        [Test]
        public void GetChildrenSiblings()
        {
            // GET:site/nav/{pageid}/children,siblings
            // http://developer.mindtouch.com/Deki/API_Reference/GET%3asite%2f%2fnav%2f%2f%7bpageid%7d%2f%2fchildren%2csiblings

            Plug p = Utils.BuildPlugForAdmin();

            string id = null;
            DreamMessage msg = PageUtils.CreateRandomPage(p, out id);

            msg = p.At("site", "nav", id, "children,siblings").Get();
            Assert.AreEqual(DreamStatus.Ok, msg.Status);

            PageUtils.DeletePageByID(p, id, true);
        }
    }
}

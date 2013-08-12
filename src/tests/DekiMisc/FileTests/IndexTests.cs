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

namespace MindTouch.Deki.Tests.FileTests
{
    [TestFixture]
    public class IndexTests
    {
        [Test]
        public void IndexFile()
        {
            // POST:files/{fileid}/index
            // http://developer.mindtouch.com/Deki/API_Reference/POST%3afiles%2f%2f%7bfileid%7d%2f%2findex

            Plug p = Utils.BuildPlugForAdmin();

            string id = null;
            DreamMessage msg = PageUtils.CreateRandomPage(p, out id);

            string fileid = null;
            string filename = null;
            msg = FileUtils.UploadRandomFile(p, id, out fileid, out filename);

            msg = p.At("files", fileid, "index").Post();
            Assert.AreEqual(DreamStatus.Ok, msg.Status);

            PageUtils.DeletePageByID(p, id, true);
        }
    }
}

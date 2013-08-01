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
using MindTouch.Deki.Export;
using MindTouch.Tasking;
using MindTouch.Xml;
using NUnit.Framework;

namespace MindTouch.Deki.Util.Tests.Packaging {
    
    [TestFixture]
    public class PackagerBaseTests {

        [Test]
        public void Conflicting_pages_get_numbered() {
            var packager = new MockPackager();
            var item = new ExportItem(null, null, 0, new XDoc("page").Elem("path", "//foo"));
            Assert.AreEqual("relative/foo/page.xml", packager.GetFilename(item));
            Assert.AreEqual("relative/foo/page.xml", packager.GetFilename(item));
        }

        [Test]
        public void Conflicting_unknown_types_get_numbered() {
            var packager = new MockPackager();
            var item = new ExportItem(null, null, 0, new XDoc("widget").Elem("path", "//foo"));
            Assert.AreEqual("relative/foo/widget.dat", packager.GetFilename(item));
            Assert.AreEqual("relative/foo/widget.dat", packager.GetFilename(item));
        }

        [Test]
        public void Generates_relative_path_for_double_slash() {
            var packager = new MockPackager();
            var item = new ExportItem(null, null, 0, new XDoc("page").Elem("path", "//foo"));
            Assert.AreEqual("relative/foo/page.xml", packager.GetFilename(item));
        }

        [Test]
        public void Generates_absolute_path_without_double_slash() {
            var packager = new MockPackager();
            var item = new ExportItem(null, null, 0, new XDoc("page").Elem("path", "foo"));
            Assert.AreEqual("absolute/foo/page.xml", packager.GetFilename(item));
        }
    }

    public class MockPackager : PackageWriterBase {

        public string GetFilename(ExportItem item) {
            return GetFilePath(item);
        }

        protected override IEnumerator<IYield> WriteData_Helper(ExportItem item, Result result) {
            throw new NotImplementedException();
        }

        protected override IEnumerator<IYield> WritePackageDoc_Helper(XDoc package, Result result) {
            throw new NotImplementedException();
        }
    }
}

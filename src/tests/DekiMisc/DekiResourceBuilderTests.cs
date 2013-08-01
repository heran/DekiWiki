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
using System.Globalization;
using System.Linq;
using System.Text;
using Moq;
using NUnit.Framework;

namespace MindTouch.Deki.Tests {

    [TestFixture]
    public class DekiResourceBuilderTests {

        [Test]
        public void New_builders_is_empty() {
            Assert.IsTrue(new DekiResourceBuilder().IsEmpty);
        }

        [Test]
        public void Adding_empty_string_keeps_builder_empty() {
            var b = new DekiResourceBuilder();
            b.Append("");
            Assert.IsTrue(b.IsEmpty);
        }

        [Test]
        public void Adding_null_string_keeps_builder_empty() {
            var b = new DekiResourceBuilder();
            b.Append((string)null);
            Assert.IsTrue(b.IsEmpty);
        }

        [Test]
        public void Creating_builder_with_empty_string_keeps_builder_empty() {
            var b = new DekiResourceBuilder("");
            Assert.IsTrue(b.IsEmpty);
        }

        [Test]
        public void Creating_builder_with_null_string_keeps_builder_empty() {
            var b = new DekiResourceBuilder((string)null);
            Assert.IsTrue(b.IsEmpty);
        }

        [Test]
        public void Appending_string_sets_builder_non_empty() {
            var b = new DekiResourceBuilder();
            b.Append("bar");
            Assert.IsFalse(b.IsEmpty);
        }

        [Test]
        public void Creating_builder_with_string_sets_builder_non_empty() {
            var b = new DekiResourceBuilder("x");
            Assert.IsFalse(b.IsEmpty);
        }

        [Test]
        public void Appending_resource_sets_builder_non_empty() {
            var b = new DekiResourceBuilder();
            b.Append(new DekiResource("x"));
            Assert.IsFalse(b.IsEmpty);
        }

        [Test]
        public void Creating_builder_with_resource_sets_builder_non_empty() {
            var b = new DekiResourceBuilder(new DekiResource("x"));
            Assert.IsFalse(b.IsEmpty);
        }

        [Test]
        public void Can_localize_string_resource_mixed_builder() {
            var resourceManagerMock = new Mock<IPlainTextResourceManager>();
            var resources = new DekiResources(resourceManagerMock.Object, CultureInfo.InvariantCulture);
            resourceManagerMock.Setup(x => x.GetString("x", CultureInfo.InvariantCulture, null))
                .Returns("abc").AtMostOnce().Verifiable();
            resourceManagerMock.Setup(x => x.GetString("y", CultureInfo.InvariantCulture, null))
                .Returns("xyz").AtMostOnce().Verifiable();
            var b = new DekiResourceBuilder();
            b.Append(new DekiResource("x"));
            b.Append("+");
            b.Append(new DekiResource("y"));
            b.Append("-");
            Assert.AreEqual("abc+xyz-",b.Localize(resources));
            resourceManagerMock.VerifyAll();
        }

    }
}

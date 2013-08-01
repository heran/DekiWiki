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
using System.IO;
using System.Linq;
using System.Text;
using MindTouch.Deki.Data;
using MindTouch.Dream;
using MindTouch.Xml;
using NUnit.Framework;
using MindTouch.IO;

namespace MindTouch.Deki.Tests.EntityTests {

    [TestFixture]
    public class ResourceContentBETests {

        [Test]
        public void Can_create_BE_from_XDoc() {
            var doc = new XDoc("test").Elem("foo", StringUtil.CreateAlphaNumericKey(6));
            var be = new ResourceContentBE(doc);

            var doc2 = XDocFactory.From(be.ToStream(), be.MimeType);
            Assert.AreEqual(doc,doc2);
        }

        [Test]
        public void Can_write_bytes_to_blank_BE() {
            var be = new ResourceContentBE(true);
            var v = 42;
            be.SetData(BitConverter.GetBytes(v));
            Assert.AreEqual(v, BitConverter.ToInt32(be.ToBytes(), 0));
        }

        [Test]
        public void Can_create_BE_from_Stream() {
            var stream = new MemoryStream();
            var bytes = Encoding.UTF8.GetBytes("foo");
            stream.Write(bytes, 0, bytes.Length);
            stream.Position = 0;
            var be = new ResourceContentBE(stream,MimeType.TEXT_UTF8);
            Assert.AreEqual(MimeType.TEXT_UTF8,be.MimeType);
            Assert.AreEqual(stream.Length, be.Size);
            Assert.AreEqual(stream.Length, be.ToBytes().Length);
        }

        [Test]
        public void BE_hashes_stream() {
            var value = StringUtil.CreateAlphaNumericKey(20);
            var be = new ResourceContentBE(value,MimeType.TEXT_UTF8);
            Assert.AreEqual(StringUtil.ComputeHashString(value,Encoding.UTF8), be.ComputeHashString());
        }

        [Test]
        public void Can_roundtrip_text_through_BE() {
            var text = StringUtil.CreateAlphaNumericKey(20);
            var be = new ResourceContentBE(text, MimeType.TEXT_UTF8);
            Assert.AreEqual(text,be.ToText());
        }
    }
}

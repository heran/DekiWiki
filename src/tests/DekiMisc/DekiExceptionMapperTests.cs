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
using System.Globalization;
using MindTouch.Deki.Exceptions;
using MindTouch.Dream;
using Moq;
using NUnit.Framework;

namespace MindTouch.Deki.Tests {

    [TestFixture]
    public class DekiExceptionMapperTests {
        private Mock<IPlainTextResourceManager> _resourceManagerMock;
        private DekiResources _resources;

        [SetUp]
        public void Setup() {
            _resourceManagerMock = new Mock<IPlainTextResourceManager>();
            _resources = new DekiResources(_resourceManagerMock.Object, CultureInfo.InvariantCulture);
        }

        [Test]
        public void Subclass_of_DekiBadCallException_call_its_handler() {
            _resourceManagerMock.Setup(x => x.GetString("System.API.Error.language_set_talk", CultureInfo.InvariantCulture, null))
                .Returns("foo").AtMostOnce().Verifiable();
            var message = DekiExceptionMapper.Map(new BadCallSubClass(), _resources);
            Assert.AreEqual(DreamStatus.BadRequest, message.Status);
            Assert.AreEqual("foo", message.ToDocument()["message"].AsText);
        }

        [Test]
        public void Subclass_of_DekiFatalCallException_call_its_handler() {
            _resourceManagerMock.Setup(x => x.GetString("System.API.Error.language_set_talk", CultureInfo.InvariantCulture, null))
                .Returns("foo").AtMostOnce().Verifiable();
            var message = DekiExceptionMapper.Map(new FatalSubClass(), _resources);
            Assert.AreEqual(DreamStatus.InternalError, message.Status);
            Assert.AreEqual("foo", message.ToDocument()["message"].AsText);
        }

        [Test]
        public void Subclass_of_DekiConflictException_call_its_handler() {
            _resourceManagerMock.Setup(x => x.GetString("System.API.Error.language_set_talk", CultureInfo.InvariantCulture, null))
                .Returns("foo").AtMostOnce().Verifiable();
            var message = DekiExceptionMapper.Map(new ConflictSubClass(), _resources);
            Assert.AreEqual(DreamStatus.Conflict, message.Status);
            Assert.AreEqual("foo", message.ToDocument()["message"].AsText);
        }


        [Test]
        public void Subclass_of_DekiMissingException_call_its_handler() {
            _resourceManagerMock.Setup(x => x.GetString("System.API.Error.language_set_talk", CultureInfo.InvariantCulture, null))
                .Returns("foo").AtMostOnce().Verifiable();
            var message = DekiExceptionMapper.Map(new MissingSubClass(), _resources);
            Assert.AreEqual(DreamStatus.NotFound, message.Status);
            Assert.AreEqual("foo", message.ToDocument()["message"].AsText);
        }

        public class BadCallSubClass : MindTouchInvalidCallException {
            public BadCallSubClass() : base(DekiResources.LANGUAGE_SET_TALK()) { }
        }

        public class FatalSubClass : MindTouchFatalCallException {
            public FatalSubClass() : base(DekiResources.LANGUAGE_SET_TALK()) { }
        }

        public class ConflictSubClass : MindTouchConflictException {
            public ConflictSubClass() : base(DekiResources.LANGUAGE_SET_TALK()) { }
        }
        public class MissingSubClass : MindTouchNotFoundException {
            public MissingSubClass() : base(DekiResources.LANGUAGE_SET_TALK()) { }
        }
    }
}

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
using Autofac.Builder;
using log4net;
using MindTouch.Deki.Util;
using Moq;
using NUnit.Framework;

namespace MindTouch.Deki.Tests {
    
    [TestFixture]
    public class Log4NetInjectionModuleTests {

        [Test]
        public void Can_inject_logger() {
            var mockLoggerRepository = new Mock<ILoggerRepository>();
            var mockLogger = new Mock<ILog>();
            mockLoggerRepository.Setup(x => x.Get(typeof(LoggerInjected))).Returns(mockLogger.Object).AtMostOnce().Verifiable();
            var builder = new ContainerBuilder();
            builder.RegisterModule(new Log4NetInjectionModule());
            builder.Register(mockLoggerRepository.Object).As<ILoggerRepository>();
            builder.Register<LoggerInjected>().As<ILoggerInjected>();
            var container = builder.Build();
            var injected = container.Resolve<ILoggerInjected>();
            mockLoggerRepository.VerifyAll();
            Assert.AreEqual(mockLogger.Object, injected.Log);
        }

        public interface ILoggerInjected {
            ILog Log { get; }
        }

        public class LoggerInjected : ILoggerInjected {
            private readonly ILog _log;

            public LoggerInjected(ILog log) {
                _log = log;
            }

            public ILog Log {
                get { return _log; }
            }
        }

        public class MockLoggerRepository : ILoggerRepository {
            #region Implementation of IDisposable
            public void Dispose() {
                throw new NotImplementedException();
            }
            #endregion

            #region Implementation of ILoggerRepository
            public ILog Get<T>() {
                throw new NotImplementedException();
            }

            public ILog Get(Type type) {
                throw new NotImplementedException();
            }
            #endregion
        }
    }
}

using System;
using log4net;
using log4net.Appender;
using log4net.Config;
using log4net.Core;
using log4net.Repository.Hierarchy;
using NUnit.Framework;

namespace MindTouch.Deki.Util.Tests {

    [TestFixture]
    public class ContextLoggerRepositoryTests {
        private static readonly ILog log = LogUtils.CreateLog();

        [Test]
        public void Can_get_logger() {
            var repository = new ContextLoggerRepository("foo");
            var logger = repository.Get<A>();
            Assert.IsNotNull(logger,"could not get a logger from repository");
            var contextLogger = logger as ContextLogger;
            Assert.IsNotNull(contextLogger,"logger could not be cast to context logger");
        }

        [Test]
        public void Same_type_receives_same_logger_instance() {
            var repository = new ContextLoggerRepository("foo");
            var logger = repository.Get<A>();
            Assert.AreSame(logger, repository.Get<A>(),"logger instances were not the same");
        }

        [Test]
        public void Loggers_from_same_repository_share_context() {
            var repository = new ContextLoggerRepository("foo");
            var loggerA = repository.Get<A>() as ContextLogger;
            var loggerB = repository.Get<B>() as ContextLogger;
            Assert.AreEqual(loggerA.Context,loggerB.Context,"loggers had different contexts from same repository");
        }

        [Test]
        public void Loggers_for_same_class_but_different_context_are_not_the_same() {
            var r1 = new ContextLoggerRepository("foo");
            var r2 = new ContextLoggerRepository("bar");
            var logger1 = r1.Get<A>() as ContextLogger;
            var logger2 = r2.Get<A>() as ContextLogger;
            Assert.AreNotSame(logger1, logger2);
        }
        [Test]
        public void Loggers_for_same_class_but_different_context_share_same_base() {
            var r1 = new ContextLoggerRepository("foo");
            var r2 = new ContextLoggerRepository("bar");
            var logger1 = r1.Get<A>() as ContextLogger;
            var logger2 = r2.Get<A>() as ContextLogger;
            Assert.AreSame(logger1.Logger, logger2.Logger);
        }

        public class A {}
        public class B {}
    }
}

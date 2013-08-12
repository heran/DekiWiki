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
using log4net;
using log4net.Appender;
using log4net.Core;
using log4net.Repository.Hierarchy;
using NUnit.Framework;

namespace MindTouch.Deki.Util.Tests {

    [TestFixture]
    public class ContextLoggerTests {
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        [Test]
        public void Test() {
            // Get the default hierarchy
            var h = LogManager.GetLoggerRepository() as Hierarchy;
            var m = new MemoryAppender();
            h.Root.AddAppender(m);
            log.Debug("test");
            var l1a = new L1("a");
            var l1b = new L1("b");
            l1a.L1CallL2();
            l1b.L1CallL2();
            var events = m.GetEvents();
            CheckEvent(0, events, typeof(ContextLoggerTests), "Test", "test");
            CheckEvent(1, events, typeof(L1), ".ctor", "[a] L1 logger created");
            CheckEvent(2, events, typeof(L1), ".ctor", "[b] L1 logger created");
            CheckEvent(3, events, typeof(L1), "L1CallL2", "[a] L1 calling L2");
            CheckEvent(4, events, typeof(L2), ".ctor", "[a] L2 logger created");
            CheckEvent(5, events, typeof(L2), "L2Call", "[a] L2 call");
            CheckEvent(6, events, typeof(L1), "L1CallL2", "[b] L1 calling L2");
            CheckEvent(7, events, typeof(L2), ".ctor", "[b] L2 logger created");
            CheckEvent(8, events, typeof(L2), "L2Call", "[b] L2 call");
            h.Root.RemoveAppender(m);
       }

        private void CheckEvent(int index, LoggingEvent[] events, Type type, string method, string message) {
            Assert.AreEqual(type.ToString(), events[index].LoggerName, string.Format("wrong logger for event {0}", index));
            Assert.AreEqual(method, events[index].LocationInformation.MethodName, string.Format("wrong method for event {0}", index));
            Assert.AreEqual(message, events[index].MessageObject.ToString(), string.Format("wrong message for event {0}", index));
        }
    }

    class L1 {
        private readonly string _context;
        private static readonly ILog logBase = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private ContextLogger log;

        public L1(string context) {
            _context = context;
            log = new ContextLogger(logBase, "[" + context + "] ");
            log.DebugFormat("L1 logger created");
        }

        public void L1CallL2() {
            log.DebugFormat("L1 calling L2");
            var l2 = new L2(_context);
            l2.L2Call();
        }
    }

    class L2 {
        private readonly string _context;
        private static readonly ILog logBase = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private ContextLogger log;

        public L2(string context) {
            _context = context;
            log = new ContextLogger(logBase, "[" + context + "] ");
            log.DebugFormat("L2 logger created");
        }

        public void L2Call() {
            log.DebugFormat("L2 call");

        }
    }
}

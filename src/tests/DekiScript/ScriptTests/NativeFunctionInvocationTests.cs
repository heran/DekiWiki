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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using MindTouch.Deki.Script.Expr;
using MindTouch.Deki.Script.Runtime;
using MindTouch.Deki.Script.Runtime.TargetInvocation;
using MindTouch.Deki.Script.Tests.Util;
using MindTouch.Dream;
using MindTouch.Dream.Test;
using NUnit.Framework;

namespace MindTouch.Deki.Script.Tests.ScriptTests {

    [TestFixture]
    public class NativeFunctionInvocationTests {

        //--- Fields ---
        private DekiScriptTester _t;
        
        [SetUp]
        public void Setup() {
            _t = new DekiScriptTester();
            MockPlug.DeregisterAll();
        }

        [Test]
        public void Can_call_native_function_without_args() {
            _t.Runtime.RegisterFunction("test.hello", GetType().GetMethod("TestHello"), new DekiScriptNativeInvocationTarget.Parameter[0]);
            _t.Test(
                "test.hello();",
                "hello",
                typeof(DekiScriptString),
                false
                );
        }

        [Test]
        public void Can_call_native_function_with_args() {
            _t.Runtime.RegisterFunction("test.echo",  GetType().GetMethod("TestEcho"), new[] {
                new DekiScriptNativeInvocationTarget.Parameter("input",false), 
            });
            _t.Test(
                "test.echo(\"foo\");",
                "foo",
                typeof(DekiScriptString),
                false
                );
        }

        public static string TestHello() {
            return "hello";
        }

        public static string TestEcho(string input) {
            return input;
        }
    }
}

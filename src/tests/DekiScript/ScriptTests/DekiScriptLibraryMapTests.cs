/*
 * MindTouch DekiScript - embeddable web-oriented scripting runtime
 * Copyright (c) 2006-2010 MindTouch Inc.
 * www.mindtouch.com  oss@mindtouch.com
 *
 * For community documentation and downloads visit developer.mindtouch.com;
 * please review the licensing section.
 *
 * This library is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 2.1 of the License, or (at your option) any later version.
 * 
 * This library is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 * 
 * You should have received a copy of the GNU Lesser General Public
 * License along with this library; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
 * http://www.gnu.org/copyleft/lesser.html
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using MindTouch.Deki.Script.Runtime.Library;
using MindTouch.Deki.Script.Tests.Util;
using MindTouch.Deki.Script.Expr;
using NUnit.Framework;

namespace MindTouch.Deki.Script.Tests.ScriptTests {

    [TestFixture]
    public class DekiScriptLibraryMapTests {

        //--- Fields ---
        private DekiScriptTester _t;

        [SetUp]
        public void Setup() {
            _t = new DekiScriptTester();
        }

        [Test]
        public void Apply() {
            _t.Test(
                @"var x = { ""a"":""b"", 1:4 }; map.apply(x, ""$.value+5"");",
                @"{ 1 : 9, a : nil }",
                typeof(DekiScriptMap)
            );
        }

        [Test]
        public void Contains_True() {
            _t.Test(
                @"var x = { ""a"":""b"", 1:4 }; map.contains(x, ""a"");",
                @"true",
                typeof(DekiScriptBool)
            );
        }

        [Test]
        public void Contains_False() {
            _t.Test(
                @"var x = { ""a"":""b"", 1:4 }; map.contains(x, ""omega"");",
                @"false",
                typeof(DekiScriptBool)
            );
        }

        [Test]
        public void Keys() {
            _t.Test(
                @"map.keys({ ""a"":""b"", 1:4, true:false });",
                @"[ ""a"", ""1"", ""true"" ]",
                typeof(DekiScriptList)
            );
        }

        [Test]
        public void KeyValues() {
            _t.Test(
                @"map.keyvalues({ ""a"":""b"", 1:4, true:false });",
                @"[ { key : ""a"", value : ""b"" }, { key : ""1"", value : 4 }, { key : ""true"", value : False } ]",
                typeof(DekiScriptList)
            );
        }

        [Test]
        public void Select() {
            _t.Test(
                @"map.select({ ""a"":""b"", 1:4, c:10, e:7 }, ""$.value%2==0"");",
                @"{ 1 : 4, c : 10 }",
                typeof(DekiScriptMap)
            );
        }

        [Test]
        public void Remove() {
            _t.Test(
                @"map.remove({ ""a"":""b"", 1:4, c:10, e:7, true:false }, ""e"");",
                @"{ 1 : 4, a : ""b"", c : 10, true : False }",
                typeof(DekiScriptMap)
            );
        }

        [Test]
        public void Values() {
            _t.Test(
                @"map.values({ ""a"":""b"", 1:4, c:10, e:7, true:false });",
                @"[ 10, ""b"", 4, 7, False ]",
                typeof(DekiScriptList)
            );
        }
    }
}
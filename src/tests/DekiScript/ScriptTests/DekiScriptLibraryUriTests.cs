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
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using MindTouch.Dream;
using MindTouch.Deki.Script.Runtime.Library;
using MindTouch.Deki.Script.Expr;
using MindTouch.Deki.Script.Tests.Util;
using NUnit.Framework;

namespace MindTouch.Deki.Script.Tests.ScriptTests {

    [TestFixture]
    public class DekiScriptLibraryUriTests {

        //--- Fields ---
        private DekiScriptTester _t;

        [SetUp]
        public void Setup() {
            _t = new DekiScriptTester();
        }

        [Test]
        public void AppendPath_String() {
            _t.Test(
                @"uri.appendpath(""http://www.mindtouch.com"", ""download"")",
                @"http://www.mindtouch.com/download",
                typeof(DekiScriptString)
            );
        }


        [Test]
        public void AppendPath_List() {
            _t.Test(
                @"uri.appendpath(""http://www.mindtouch.com"", [ ""download"", ""b"", ""c"", ""z"" ])",
                @"http://www.mindtouch.com/download/b/c/z",
                typeof(DekiScriptString)
            );
        }

        [Test]
        public void AppendQuery() {
            _t.Test(
                @"uri.appendquery(""http://www.mindtouch.com"", { a : ""b"", 7 : true, format : ""html"" })",
                @"http://www.mindtouch.com?a=b&7=True&format=html",
                typeof(DekiScriptString)
            );
        }

        [Test]
        public void Build_String() {
            _t.Test(
                @"uri.build(""http://www.mindtouch.com"", ""download"", { a : ""b"", 7 : true, format : ""html"" })",
                @"http://www.mindtouch.com/download?a=b&7=True&format=html",
                typeof(DekiScriptString)
            );
        }

        [Test]
        public void Build_List() {
            _t.Test(
                @"uri.build(""http://www.mindtouch.com"", [ ""download"", ""b"", ""c"", ""z"" ], { a : [ ""b"", ""c"", ""z"" ], 7 : true, format : ""html"" })",
                @"http://www.mindtouch.com/download/b/c/z?a=b&a=c&a=z&7=True&format=html",
                typeof(DekiScriptString)
            );
        }

        [Test]
        public void Decode() {
            _t.Test(
                @"uri.decode(""http://www.mindtouch.com/a%20b%21c%22d%23e%24f%25"")",
                @"http://www.mindtouch.com/a b!c""d#e$f%",
                typeof(DekiScriptString)
            );
        }

        [Test]
        public void Encode() {
            _t.Test(
                @"uri.encode(""a b!c\""d#e$f%"")",
                @"a+b!c%22d%23e%24f%25",
                typeof(DekiScriptString)
            );
        }

        [Test]
        public void IsValid() {
            _t.Test(
                @"uri.isvalid(""http://www.mindtouch.com"")",
                @"true",
                typeof(DekiScriptBool)
            );
        }

        [Test]
        public void IsValid_Null() {
            _t.Test(
                @"uri.isvalid(nil)",
                @"false",
                typeof(DekiScriptBool)
            );
        }

        [Test]
        public void IsValid_String() {
            _t.Test(
                @"uri.isvalid(""www.mindtouch.com"")",
                @"false",
                typeof(DekiScriptBool)
            );
        }

        [Test]
        public void IsValid_Number() {
            _t.Test(
                @"uri.isvalid(1000)",
                @"false",
                typeof(DekiScriptBool)
            );
        }

        [Test]
        public void IsValid_XUri() {
             Assert.IsTrue(DekiScriptLibrary.UriIsValid(new XUri("http://www.mindtouch.com")));
        }

        [Test]
        public void Parts() {
            _t.Test(
                @"uri.parts(""http://user:password@www.mindtouch.com/a/b?c=1&d=e"")",
                @"{ fragment : nil, host : ""www.mindtouch.com"", password : ""password"", path : [ ""a"", ""b"" ], query : { c : ""1"", d : ""e"" }, scheme : ""http"", user : ""user"" }",
                typeof(DekiScriptMap)
            );
        }

        [Test]
        public void Parse() { //deprecated
            _t.Test(
                @"uri.parse(""http://user:password@www.mindtouch.com/a/b?c=1&d=e"")",
                @"{ fragment : nil, host : ""www.mindtouch.com"", password : ""password"", path : [ ""a"", ""b"" ], query : { c : ""1"", d : ""e"" }, scheme : ""http"", user : ""user"" }",
                typeof(DekiScriptMap)
            );
        }

        [Ignore] // bug 8573
        [Test]
        public void Parts_WithFragment() {
            _t.Test(
                @"uri.parts(""http://user:password@www.mindtouch.com/a/b#search?c=1&d=e"")",
                @"{ fragment : ""search"", host : ""www.mindtouch.com"", password : ""password"", path : [ ""a"", ""b"" ], query : { c : ""1"", d : ""e"" }, scheme : ""http"", user : ""user"" }",
                typeof(DekiScriptMap)
            );
        }
    }
}
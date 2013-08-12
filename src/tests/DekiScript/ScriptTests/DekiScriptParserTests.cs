/*
 * MindTouch DekiScript - embeddable web-oriented scripting runtime
 * Copyright (c) 2006-2010 MindTouch Inc.
 * www.mindtouch.com  oss@mindtouch.com
 *
 * For community documentation and downloads visit wiki.developer.mindtouch.com;
 * please review the licensing section.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 *     http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System.IO;
using System.Text;
using MindTouch.Deki.Script.Compiler;
using MindTouch.Deki.Script.Expr;
using NUnit.Framework;

namespace MindTouch.Deki.Script.Tests.ScriptTests {
    
    [TestFixture]
    public class DekiScriptParserTests {

        [Test]
        public void Can_parse_string() {
            var code = "(var x = 5; if(x > 5) { x })";
            var expr = DekiScriptParser.Parse(Location.Start, code);
            Assert.AreEqual(code, expr.ToString());
        }

        [Test]
        public void Can_parse_stream() {
            var code = "(var x = 5; if(x > 5) { x })";
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(code));
            var expr = DekiScriptParser.Parse(Location.Start, stream);
            Assert.AreEqual(code, expr.ToString());
        }
    }
}

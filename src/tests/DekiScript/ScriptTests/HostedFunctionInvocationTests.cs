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
using MindTouch.Deki.Script.Expr;
using MindTouch.Deki.Script.Runtime;
using MindTouch.Deki.Script.Tests.Util;
using MindTouch.Dream;
using MindTouch.Dream.Test;
using MindTouch.Xml;
using NUnit.Framework;
using System.Linq;

namespace MindTouch.Deki.Script.Tests.ScriptTests {
    [TestFixture]
    public class HostedFunctionInvocationTests {

        private DreamHostInfo _hostInfo;
        private ITestScriptService _scriptService;

        [TestFixtureSetUp]
        public void FixtureSetup() {
            _hostInfo = DreamTestHelper.CreateRandomPortHost(new XDoc("config").Elem("apikey", "123"));
            _hostInfo.Host.Self.At("load").With("name", "mindtouch.deki.services").Post(DreamMessage.Ok());
        }

        [SetUp]
        public void Setup() {
            MockPlug.DeregisterAll();
            _scriptService = new TestScriptService(_hostInfo);
        }

        [Test]
        public void Test_default_parameters() {
            AddFunctionAsXml(@"
  <function>
    <name>test_default_parameters</name>
    <param name=""value"" type=""any"" default=""123"">Don't provide a value.</param>
    <return type=""xml"">
      <html xmlns:eval=""http://mindtouch.com/2007/dekiscript"">
        <body>
          <eval:if test=""$value == 123"">default parameter test worked</eval:if>
          <eval:else>default parameter test failed</eval:else>
        </body>
      </html>
    </return>
  </function>
                ");
            Execute("test_default_parameters()")
                .VerifyXml((doc) => Assert.IsTrue(doc["body"].AsText.Contains("default parameter test worked")));
        }

        [Test]
        public void Head_style() {
            AddFunctionAsXml(@"
  <function>
    <name>head_style</name>
    <return type=""xml"">
      <html xmlns:eval=""http://mindtouch.com/2007/dekiscript"">
        <head>
          <style type=""text/css"">
            body {background-color: yellow}
            h1 {background-color: #00ff00}
            h2 {background-color: transparent}
            p {background-color: rgb(250,0,255)}
          </style>
        </head>
      </html>
    </return>
  </function>
                ");
            Execute("head_style()")
                .VerifyXml((doc) => Assert.IsTrue(doc["head/style"].AsText.Contains("body {background-color: yellow}")));
        }

        [Test]
        public void Head_meta() {
            AddFunctionAsXml(@"
  <function>
    <name>head_meta</name>
    <return type=""xml"">
      <html xmlns:eval=""http://mindtouch.com/2007/dekiscript"">
        <head>
          <meta name=""description"" content=""Free Web tutorials on HTML, CSS, XML, and XHTML"" />
          <meta name=""keywords"" content=""HTML, DHTML, CSS, XML, XHTML, JavaScript, VBScript"" />
        </head>
      </html>
    </return>
  </function>
            ");
            Execute("head_meta()")
                .VerifyXml((doc) => {
                    var meta = doc["head/meta"].ToList();
                    Assert.AreEqual(2, meta.Count);
                    Assert.AreEqual(new[] { "description", "keywords" }, meta.Select(x => x["@name"].AsText).ToArray());
                });
        }

        [Test]
        public void Merging_multiple_bodies() {
            AddFunctionAsXml(@"
  <function>
    <name>multiple_bodies</name>
    <return type=""xml"">
      <html xmlns:eval=""http://mindtouch.com/2007/dekiscript"">
        <body target=""first"">First Body</body>
        <body>MainBody</body>
        <body target=""last"">Last Body</body>
      </html>
    </return>
  </function>
            ");
            AddFunctionAsXml(@"
  <function>
    <name>multiple_bodies_merged</name>
    <return type=""xml"">
      <html xmlns:eval=""http://mindtouch.com/2007/dekiscript"">
        <body>Before <eval:expr value=""multiple_bodies()""/> After</body>
      </html>
    </return>
  </function>
        ");
            Execute("multiple_bodies_merged()")
                .VerifyXml((doc) => {
                    var mainbody = doc["body[not(@target)]"];
                    Assert.IsFalse(mainbody.IsEmpty);
                    Assert.AreEqual("Before MainBody After", mainbody.Contents);
                    var first = doc["body[@target='first']"];
                    Assert.IsFalse(first.IsEmpty);
                    Assert.AreEqual("First Body", first.Contents);
                    var last = doc["body[@target='last']"];
                    Assert.IsFalse(last.IsEmpty);
                    Assert.AreEqual("Last Body", last.Contents);
                });
        }

        [Test]
        public void Eval_expr_with_attribute() {
            AddFunctionAsXml(@"
 <function>
    <name>eval_expr_with_attribute</name>
    <param name=""value"" type=""str"" default=""'Hello World!'"">Don't provide a value.</param>
    <return type=""xml"">
      <html xmlns:eval=""http://mindtouch.com/2007/dekiscript"">
        <body>
          <eval:expr value=""$value"" />
        </body>
      </html>
    </return>
  </function>
            ");
            Execute("eval_expr_with_attribute()")
                .VerifyXml((doc) => Assert.AreEqual("Hello World!", doc["body"].Contents.Trim()));
        }

        [Test]
        public void Eval_expr_as_text() {
            AddFunctionAsXml(@"
  <function>
    <name>eval_expr_as_text</name>
    <param name=""value"" type=""str"" default=""'Hello World!'"">Don't provide a value.</param>
    <return type=""xml"">
      <html xmlns:eval=""http://mindtouch.com/2007/dekiscript"">
        <body>
          <eval:expr>$value</eval:expr>
        </body>
      </html>
    </return>
  </function>
            ");
            Execute("eval_expr_as_text()")
                .VerifyXml((doc) => Assert.AreEqual("Hello World!", doc["body"].Contents.Trim()));
        }

        [Test]
        public void Eval_expr_with_attribute_with_error() {
            AddFunctionAsXml(@"
  <function>
    <name>eval_expr_with_attribute_with_error</name>
    <return type=""xml"">
      <html xmlns:eval=""http://mindtouch.com/2007/dekiscript"">
        <body>
          <eval:expr value=""num.abs(_)"" />
        </body>
      </html>
    </return>
  </function>
            ");
            Execute("eval_expr_with_attribute_with_error()")
                .VerifyXml((doc) => {
                    Assert.AreEqual("warning", doc["span/@class"].AsText);
                    var message = doc["span"].AsText;
                    Assert.IsTrue(message.StartsWith("missing value for parameter 'number' (index 0)"), message);
                });
        }

        [Test]
        public void Eval_expr_as_text_with_error() {
            AddFunctionAsXml(@"
   <function>
    <name>eval_expr_as_text_with_error</name>
    <return type=""xml"">
      <html xmlns:eval=""http://mindtouch.com/2007/dekiscript"">
        <body>
          <eval:expr>num.abs(_)</eval:expr>
        </body>
      </html>
    </return>
  </function>
           ");
            Execute("eval_expr_as_text_with_error()")
                .VerifyXml((doc) => {
                    Assert.AreEqual("warning", doc["span/@class"].AsText);
                    var message = doc["span"].AsText;
                    Assert.IsTrue(message.StartsWith("missing value for parameter 'number' (index 0)"), message);
                });
        }

        [Test]
        public void Eval_js_with_attribute() {
            AddFunctionAsXml(@"
   <function>
    <name>eval_js_with_attribute</name>
    <param name=""value"" type=""str"" default=""'Hello World!'"">Don't provide a value.</param>
    <return type=""xml"">
      <html xmlns:eval=""http://mindtouch.com/2007/dekiscript"">
        <body>
          <eval:js value=""$value"" />
        </body>
      </html>
    </return>
  </function>
           ");
            Execute("eval_js_with_attribute()")
                .VerifyXml((doc) => Assert.AreEqual(@"""Hello World!""", doc["body"].Contents.Trim()));
        }

        [Test]
        public void Eval_js_as_text() {
            AddFunctionAsXml(@"
  <function>
    <name>eval_js_as_text</name>
    <param name=""value"" type=""str"" default=""'Hello World!'"">Don't provide a value.</param>
    <return type=""xml"">
      <html xmlns:eval=""http://mindtouch.com/2007/dekiscript"">
        <body>
          <eval:js>$value</eval:js>
        </body>
      </html>
    </return>
  </function>
            ");
            Execute("eval_js_as_text()")
                .VerifyXml((doc) => Assert.AreEqual(@"""Hello World!""", doc["body"].Contents.Trim()));
        }

        [Test]
        public void Eval_js_with_attribute_with_error() {
            AddFunctionAsXml(@"
  <function>
    <name>eval_js_with_attribute_with_error</name>
    <return type=""xml"">
      <html xmlns:eval=""http://mindtouch.com/2007/dekiscript"">
        <body>
          <eval:js value=""num.abs(_)"" />
        </body>
      </html>
    </return>
  </function>
            ");
            Execute("eval_js_with_attribute_with_error()")
                .VerifyXml((doc) => {
                    Assert.AreEqual("warning", doc["span/@class"].AsText);
                    var message = doc["span"].AsText;
                    Assert.IsTrue(message.StartsWith("missing value for parameter 'number' (index 0)"), message);
                });
        }

        [Test]
        public void Eval_js_as_text_with_error() {
            AddFunctionAsXml(@"
  <function>
    <name>eval_js_as_text_with_error</name>
    <return type=""xml"">
      <html xmlns:eval=""http://mindtouch.com/2007/dekiscript"">
        <body>
          <eval:js>num.abs(_)</eval:js>
        </body>
      </html>
    </return>
  </function>
            ");
            Execute("eval_js_as_text_with_error()")
                .VerifyXml((doc) => {
                    Assert.AreEqual("warning", doc["span/@class"].AsText);
                    var message = doc["span"].AsText;
                    Assert.IsTrue(message.StartsWith("missing value for parameter 'number' (index 0)"), message);
                });
        }

        [Test]
        public void Eval_if_elseif_else() {
            AddFunctionAsXml(@"
  <function>
    <name>eval_if_elseif_else</name>
    <param name=""value"" type=""num""/>
    <return type=""xml"">
      <html xmlns:eval=""http://mindtouch.com/2007/dekiscript"">
        <body>
          <eval:if test=""$value == 1"">If</eval:if>
          <eval:elseif test=""$value == 2"">ElseIf</eval:elseif>
          <eval:else>Else</eval:else>
        </body>
      </html>
    </return>
  </function>
            ");
            Execute("eval_if_elseif_else(1)")
                .VerifyXml((doc) => Assert.AreEqual(@"If", doc["body"].Contents.Trim()));
            Execute("eval_if_elseif_else(2)")
                .VerifyXml((doc) => Assert.AreEqual(@"ElseIf", doc["body"].Contents.Trim()));
            Execute("eval_if_elseif_else(3)")
                .VerifyXml((doc) => Assert.AreEqual(@"Else", doc["body"].Contents.Trim()));
        }

        [Test]
        public void Eval_if_elseif_else_with_error_in_if() {
            AddFunctionAsXml(@"
  <function>
    <name>eval_if_elseif_else</name>
    <param name=""value"" type=""num""/>
    <return type=""xml"">
      <html xmlns:eval=""http://mindtouch.com/2007/dekiscript"">
        <body>
          <eval:if test=""num.abs(_)"">If</eval:if>
          <eval:elseif test=""$value == 2"">ElseIf</eval:elseif>
          <eval:else>Else</eval:else>
        </body>
      </html>
    </return>
  </function>
            ");
            Execute("eval_if_elseif_else(1)")
                .VerifyXml((doc) => {
                    Assert.AreEqual("warning", doc["span/@class"].AsText);
                    var message = doc["span"].AsText;
                    Assert.IsTrue(message.StartsWith("missing value for parameter 'number' (index 0)"), message);
                });
        }

        [Test]
        public void Eval_if_elseif_else_with_error_in_elseif() {
            AddFunctionAsXml(@"
  <function>
    <name>eval_if_elseif_else</name>
    <param name=""value"" type=""num""/>
    <return type=""xml"">
      <html xmlns:eval=""http://mindtouch.com/2007/dekiscript"">
        <body>
          <eval:if test=""$value == 1"">If</eval:if>
          <eval:elseif test=""num.abs(_)"">ElseIf</eval:elseif>
          <eval:else>Else</eval:else>
        </body>
      </html>
    </return>
  </function>
            ");
            Execute("eval_if_elseif_else(2)")
                .VerifyXml((doc) => {
                    Assert.AreEqual("warning", doc["span/@class"].AsText);
                    var message = doc["span"].AsText;
                    Assert.IsTrue(message.StartsWith("missing value for parameter 'number' (index 0)"), message);
                });
        }

        [Test]
        public void Eval_if_elseif_else_with_error_in_else() {
            AddFunctionAsXml(@"
  <function>
    <name>eval_if_elseif_else</name>
    <param name=""value"" type=""num""/>
    <return type=""xml"">
      <html xmlns:eval=""http://mindtouch.com/2007/dekiscript"">
        <body>
          <eval:if test=""$value == 1"">If</eval:if>
          <eval:elseif test=""$value == 2"">ElseIf</eval:elseif>
          <eval:else>
            <eval:expr>num.abs(_)</eval:expr>
          </eval:else>
        </body>
      </html>
    </return>
  </function>
            ");
            Execute("eval_if_elseif_else(3)")
                .VerifyXml((doc) => {
                    Assert.AreEqual("warning", doc["span/@class"].AsText);
                    var message = doc["span"].AsText;
                    Assert.IsTrue(message.StartsWith("missing value for parameter 'number' (index 0)"), message);
                });
        }

        [Test]
        public void Eval_block_with_definition() {
            AddFunctionAsXml(@"
  <function>
    <name>eval_block_with_definition</name>
    <return type=""xml"">
      <html xmlns:eval=""http://mindtouch.com/2007/dekiscript"">
        <body>
          <eval:block value=""var t = 1 + 2"">
            <eval:expr value=""t"" />
          </eval:block>
        </body>
      </html>
    </return>
  </function>

            ");
            Execute("eval_block_with_definition()")
                .VerifyXml((doc) => Assert.AreEqual(@"3", doc["body"].Contents.Trim()));
        }

        [Test]
        public void Eval_block_with_with_emitting_definition() {
            AddFunctionAsXml(@"
  <function>
    <name>eval_block_with_with_emitting_definition</name>
    <return type=""xml"">
      <html xmlns:eval=""http://mindtouch.com/2007/dekiscript"">
        <body>
          <eval:block value=""456"">
            <eval:expr value=""123"" />
          </eval:block>
        </body>
      </html>
    </return>
  </function>
            ");
            Execute("eval_block_with_with_emitting_definition()")
                .VerifyXml((doc) => Assert.AreEqual(@"123", doc["body"].Contents.Trim()));
        }

        [Test]
        public void Eval_block_with_with_error() {
            AddFunctionAsXml(@"
   <function>
    <name>eval_block_with_with_error</name>
    <return type=""xml"">
      <html xmlns:eval=""http://mindtouch.com/2007/dekiscript"">
        <body>
          <eval:block value=""num.abs(_)"">
            <eval:expr value=""123"" />
          </eval:block>
        </body>
      </html>
    </return>
  </function>
           ");
            Execute("eval_block_with_with_error()")
                .VerifyXml((doc) => {
                    Assert.AreEqual("warning", doc["span/@class"].AsText);
                    var message = doc["span"].AsText;
                    Assert.IsTrue(message.StartsWith("missing value for parameter 'number' (index 0)"), message);
                });
        }

        [Test]
        public void Eval_foreach_in_list() {
            AddFunctionAsXml(@"
  <function>
    <name>eval_foreach_in_list</name>
    <param name=""value"" type=""list"" />
    <return type=""xml"">
      <html xmlns:eval=""http://mindtouch.com/2007/dekiscript"">
        <body>
          <eval:foreach var=""x"" in=""$value"">
            <i><eval:expr value=""x"" /></i>
          </eval:foreach>
        </body>
      </html>
    </return>
  </function>
            ");
            Execute("eval_foreach_in_list([1,2,3])")
                .VerifyXml((doc) => Assert.AreEqual(new[] { 1, 2, 3 }, doc["body/i"].Select(x => x.AsInt).ToArray()));
        }

        [Test]
        public void Eval_foreach_in_list_without_id() {
            AddFunctionAsXml(@"
  <function>
    <name>eval_foreach_in_list_without_id</name>
    <param name=""value"" type=""list"" />
    <return type=""xml"">
      <html xmlns:eval=""http://mindtouch.com/2007/dekiscript"">
        <body>
          <eval:foreach in=""$value"">
            <i><eval:expr value=""$"" /></i>
          </eval:foreach>
        </body>
      </html>
    </return>
  </function>
            ");
            Execute("eval_foreach_in_list_without_id([1,2,3])")
                .VerifyXml((doc) => Assert.AreEqual(new[] { 1, 2, 3 }, doc["body/i"].Select(x => x.AsInt).ToArray()));
        }

        [Test]
        public void Eval_foreach_in_map() {
            AddFunctionAsXml(@"
  <function>
    <name>eval_foreach_in_map</name>
    <param name=""value"" type=""map"" />
    <return type=""xml"">
      <html xmlns:eval=""http://mindtouch.com/2007/dekiscript"">
        <body>
          <eval:foreach in=""$value"">
            <i><eval:expr value=""$"" /></i>
          </eval:foreach>
        </body>
      </html>
    </return>
  </function>
            ");
            Execute("eval_foreach_in_map({ 1: 'a', 2: 'b', 3: 'c' })")
                .VerifyXml((doc) => Assert.AreEqual(new[] { "a", "b", "c" }, doc["body/i"].Select(x => x.AsText).ToArray()));
        }

        [Test]
        public void Eval_foreach_in_list_with_test_clause() {
            AddFunctionAsXml(@"
  <function>
    <name>eval_foreach_in_list_with_test_clause</name>
    <param name=""value"" type=""list"" />
    <return type=""xml"">
      <html xmlns:eval=""http://mindtouch.com/2007/dekiscript"">
        <body>
          <eval:foreach var=""x"" in=""$value"" test=""x % 2 == 1"">
            <i><eval:expr value=""x"" /></i>
          </eval:foreach>
        </body>
      </html>
    </return>
  </function>
            ");
            Execute("eval_foreach_in_list_with_test_clause([1,2,3])")
                .VerifyXml((doc) => Assert.AreEqual(new[] { 1, 3 }, doc["body/i"].Select(x => x.AsInt).ToArray()));
        }

        [Test]
        public void Eval_foreach_in_list_with_where_clause() {
            AddFunctionAsXml(@"
  <function>
    <name>eval_foreach_in_list_with_where_clause</name>
    <param name=""value"" type=""list"" />
    <return type=""xml"">
      <html xmlns:eval=""http://mindtouch.com/2007/dekiscript"">
          <body>
            <eval:foreach var=""x"" in=""$value"" where=""x % 2 == 1"">
              <i><eval:expr value=""x"" /></i>
            </eval:foreach>
          </body>
      </html>
    </return>
  </function>
            ");
            Execute("eval_foreach_in_list_with_where_clause([1,2,3])")
                .VerifyXml((doc) => Assert.AreEqual(new[] { 1, 3 }, doc["body/i"].Select(x => x.AsInt).ToArray()));
        }

        [Test]
        public void Eval_foreach_with_error_in_list() {
            AddFunctionAsXml(@"
  <function>
    <name>eval_foreach_with_error_in_list</name>
    <return type=""xml"">
      <html xmlns:eval=""http://mindtouch.com/2007/dekiscript"">
        <body>
          <eval:foreach var=""x"" in=""num.abs(_)"">
            <eval:expr value=""x"" />
            <br />
          </eval:foreach>
        </body>
      </html>
    </return>
  </function>
            ");
            Execute("eval_foreach_with_error_in_list()")
                .VerifyXml((doc) => {
                    Assert.AreEqual("warning", doc["span/@class"].AsText);
                    var message = doc["span"].AsText;
                    Assert.IsTrue(message.StartsWith("missing value for parameter 'number' (index 0)"), message);
                });
        }

        [Test]
        public void Eval_foreach_with_error_in_where_clause() {
            AddFunctionAsXml(@"
  <function>
    <name>eval_foreach_with_error_in_where_clause</name>
    <param name=""value"" type=""list"" />
    <return type=""xml"">
      <html xmlns:eval=""http://mindtouch.com/2007/dekiscript"">
          <body>
            <eval:foreach var=""x"" in=""$value"" where=""num.abs(_)"">
              <eval:expr value=""x"" />
              <br />
            </eval:foreach>
          </body>
      </html>
    </return>
  </function>
            ");
            Execute("eval_foreach_with_error_in_where_clause([1,2,3])")
                .VerifyXml((doc) => {
                    Assert.AreEqual("warning", doc["span/@class"].AsText);
                    var message = doc["span"].AsText;
                    Assert.IsTrue(message.StartsWith("missing value for parameter 'number' (index 0)"), message);
                });
        }

        [Test]
        public void Eval_if_attribute() {
            AddFunctionAsXml(@"
  <function>
    <name>eval_if_attribute</name>
    <param name=""value"" type=""num""/>
    <return type=""xml"">
      <html xmlns:eval=""http://mindtouch.com/2007/dekiscript"">
        <body>
          <span if=""$value == 1"">If</span>
        </body>
      </html>
    </return>
  </function>
            ");
            Execute("eval_if_attribute(1)")
                .VerifyXml((doc) => Assert.AreEqual("If", doc["body/span"].AsText, doc.ToPrettyString()));
            Execute("eval_if_attribute(2)")
                .VerifyXml((doc) => Assert.IsTrue(doc["body/span"].IsEmpty, doc.ToPrettyString()));
        }

        [Test]
        public void Eval_if_attribute_with_error() {
            AddFunctionAsXml(@"
   <function>
    <name>eval_if_attribute_with_error</name>
    <return type=""xml"">
      <html xmlns:eval=""http://mindtouch.com/2007/dekiscript"">
        <body>
          <span if=""num.abs(_)"">If</span>
        </body>
      </html>
    </return>
  </function>
           ");
            Execute("eval_if_attribute_with_error()")
                .VerifyXml((doc) => {
                    Assert.AreEqual("warning", doc["body/div/span/@class"].AsText, doc.ToPrettyString());
                    var message = doc["body/div/span"].AsText;
                    Assert.IsTrue(message.StartsWith("missing value for parameter 'number' (index 0)"), message);
                });
        }

        [Test]
        public void Eval_block_attribute_with_definition() {
            AddFunctionAsXml(@"
  <function>
    <name>eval_block_attribute_with_definition</name>
    <return type=""xml"">
      <html xmlns:eval=""http://mindtouch.com/2007/dekiscript"">
        <body>
          <span block=""var t = 1 + 2"">
            <eval:expr value=""t"" />
          </span>
        </body>
      </html>
    </return>
  </function>
            ");
            Execute("eval_block_attribute_with_definition()")
                .VerifyXml((doc) => Assert.AreEqual(3, doc["body/span"].AsInt, doc.ToPrettyString()));
        }

        [Test]
        public void Eval_block_attribute_with_with_emitting_definition() {
            AddFunctionAsXml(@"
  <function>
    <name>eval_block_attribute_with_with_emitting_definition</name>
    <return type=""xml"">
      <html xmlns:eval=""http://mindtouch.com/2007/dekiscript"">
        <body>
          <span block=""456"">
            <eval:expr value=""123"" />
          </span>
        </body>
      </html>
    </return>
  </function>
            ");
            Execute("eval_block_attribute_with_with_emitting_definition()")
                .VerifyXml((doc) => Assert.AreEqual(123, doc["body/span"].AsInt, doc.ToPrettyString()));
        }

        [Test]
        public void Eval_block_attribute_with_error() {
            AddFunctionAsXml(@"
  <function>
    <name>eval_block_attribute_with_error</name>
    <return type=""xml"">
      <html xmlns:eval=""http://mindtouch.com/2007/dekiscript"">
        <body>
          <span block=""num.abs(_)"">
            <eval:expr value=""123"" />
          </span>
        </body>
      </html>
    </return>
  </function>
            ");
            Execute("eval_block_attribute_with_error()")
                .VerifyXml((doc) => {
                    Assert.AreEqual("warning", doc["body/div/span/@class"].AsText, doc.ToPrettyString());
                    var message = doc["body/div/span"].AsText;
                    Assert.IsTrue(message.StartsWith("missing value for parameter 'number' (index 0)"), message);
                });
        }

        [Test]
        public void Eval_init_attribute_with_definition() {
            AddFunctionAsXml(@"
  <function>
    <name>eval_init_attribute_with_definition</name>
    <return type=""xml"">
      <html xmlns:eval=""http://mindtouch.com/2007/dekiscript"">
        <body>
          <span init=""var t = 1 + 2"">
            <eval:expr value=""t"" />
          </span>
        </body>
      </html>
    </return>
  </function>
            ");
            Execute("eval_init_attribute_with_definition()")
                .VerifyXml((doc) => Assert.AreEqual(3, doc["body/span"].AsInt, doc.ToPrettyString()));
        }

        [Test]
        public void Eval_init_attribute_with_with_emitting_definition() {
            AddFunctionAsXml(@"
  <function>
    <name>eval_init_attribute_with_with_emitting_definition</name>
    <return type=""xml"">
      <html xmlns:eval=""http://mindtouch.com/2007/dekiscript"">
        <body>
          <span init=""456"">
            <eval:expr value=""123"" />
          </span>
        </body>
      </html>
    </return>
  </function>
            ");
            Execute("eval_init_attribute_with_with_emitting_definition()")
                .VerifyXml((doc) => Assert.AreEqual(123, doc["body/span"].AsInt, doc.ToPrettyString()));
        }

        [Test]
        public void Eval_init_attribute_with_error() {
            AddFunctionAsXml(@"
  <function>
    <name>eval_init_attribute_with_error</name>
    <return type=""xml"">
      <html xmlns:eval=""http://mindtouch.com/2007/dekiscript"">
        <body>
          <span init=""num.abs(_)"">
            <eval:expr value=""123"" />
          </span>
        </body>
      </html>
    </return>
  </function>
            ");
            Execute("eval_init_attribute_with_error()")
                .VerifyXml((doc) => {
                    Assert.AreEqual("warning", doc["body/div/span/@class"].AsText, doc.ToPrettyString());
                    var message = doc["body/div/span"].AsText;
                    Assert.IsTrue(message.StartsWith("missing value for parameter 'number' (index 0)"), message);
                });
        }

        [Test]
        public void Eval_foreach_attribute_in_list() {
            AddFunctionAsXml(@"
  <function>
    <name>eval_foreach_attribute_in_list</name>
    <param name=""value"" type=""list"" />
    <return type=""xml"">
      <html xmlns:eval=""http://mindtouch.com/2007/dekiscript"">
        <body>
          <div foreach=""var x in $value"">
            <span><eval:expr value=""x"" /></span>
          </div>
        </body>
      </html>
    </return>
  </function>
            ");
            Execute("eval_foreach_attribute_in_list([1,2,3])")
                .VerifyXml((doc) => Assert.AreEqual(new[] { 1, 2, 3 }, doc["body/div/span"].Select(x => x.AsInt).ToArray(), doc.ToPrettyString()));
        }

        [Test]
        public void Eval_foreach_attribute_in_map() {
            AddFunctionAsXml(@"
  <function>
    <name>eval_foreach_attribute_in_map</name>
    <param name=""value"" type=""map"" />
    <return type=""xml"">
      <html xmlns:eval=""http://mindtouch.com/2007/dekiscript"">
        <body>
          <div foreach=""var x in $value"">
            <span><eval:expr value=""x"" /></span>
          </div>
        </body>
      </html>
    </return>
  </function>
            ");
            Execute("eval_foreach_attribute_in_map({ 1: 'a', 2: 'b', 3: 'c' })")
                .VerifyXml((doc) => Assert.AreEqual(new[] { "a", "b", "c" }, doc["body/div/span"].Select(x => x.AsText).ToArray(), doc.ToPrettyString()));
        }

        [Test]
        public void Eval_foreach_attribute_in_list_with_where_clause() {
            AddFunctionAsXml(@"
  <function>
    <name>eval_foreach_attribute_in_list_with_where_clause</name>
    <param name=""value"" type=""list"" />
    <return type=""xml"">
      <html xmlns:eval=""http://mindtouch.com/2007/dekiscript"">
          <body>
            <div foreach=""var x in $value where x % 2 == 1"">
              <span><eval:expr value=""x"" /></span>
            </div>
        </body>
      </html>
    </return>
  </function>
            ");
            Execute("eval_foreach_attribute_in_list_with_where_clause([1,2,3])")
                .VerifyXml((doc) => Assert.AreEqual(new[] { 1, 3 }, doc["body/div/span"].Select(x => x.AsInt).ToArray(), doc.ToPrettyString()));
        }

        [Test]
        public void Eval_foreach_attribute_in_list_with_where_attribute() {
            AddFunctionAsXml(@"
  <function>
    <name>eval_foreach_attribute_in_list_with_where_attribute</name>
    <param name=""value"" type=""list"" />
    <return type=""xml"">
      <html xmlns:eval=""http://mindtouch.com/2007/dekiscript"">
          <body>
            <div foreach=""var x in $value"" where=""x % 2 == 1"">
              <span><eval:expr value=""x"" /></span>
            </div>
          </body>
      </html>
    </return>
  </function>
            ");
            Execute("eval_foreach_attribute_in_list_with_where_attribute([1,2,3])")
                .VerifyXml((doc) => Assert.AreEqual(new[] { 1, 3 }, doc["body/div/span"].Select(x => x.AsInt).ToArray(), doc.ToPrettyString()));
        }

        [Test]
        public void Eval_foreach_attribute_in_list_with_if_attribute() {
            AddFunctionAsXml(@"
  <function>
    <name>eval_foreach_attribute_in_list_with_if_attribute</name>
    <param name=""value"" type=""list"" />
    <param name=""condition"" type=""bool"" />
    <return type=""xml"">
      <html xmlns:eval=""http://mindtouch.com/2007/dekiscript"">
          <body>
            <div if=""$condition"" foreach=""var x in $value"">
              <span><eval:expr value=""x"" /></span>
            </div>
          </body>
      </html>
    </return>
  </function>
            ");
            Execute("eval_foreach_attribute_in_list_with_if_attribute([1,2,3],false)")
                .VerifyXml((doc) => Assert.IsTrue(doc["body/div/span"].IsEmpty, doc.ToPrettyString()));
            Execute("eval_foreach_attribute_in_list_with_if_attribute([1,2,3],true)")
                .VerifyXml((doc) => Assert.AreEqual(new[] { 1, 2, 3 }, doc["body/div/span"].Select(x => x.AsInt).ToArray(), doc.ToPrettyString()));
        }

        [Test]
        public void Eval_foreach_attribute_in_list_with_init_and_block() {
            AddFunctionAsXml(@"
  <function>
    <name>eval_foreach_attribute_in_list_with_init_and_block</name>
    <param name=""value"" type=""list"" />
    <return type=""xml"">
      <html xmlns:eval=""http://mindtouch.com/2007/dekiscript"">
          <body>
            <div init=""var value = $value"" foreach=""var x in value"" block=""let x = 2 * x"">
              <span><eval:expr value=""x"" /></span>
            </div>
          </body>
      </html>
    </return>
  </function>
            ");
            Execute("eval_foreach_attribute_in_list_with_init_and_block([1,2,3])")
                .VerifyXml((doc) => Assert.AreEqual(new[] { 2, 4, 6 }, doc["body/div/span"].Select(x => x.AsInt).ToArray(), doc.ToPrettyString()));
        }

        [Test]
        public void Eval_foreach_attribute_with_error_in_list() {
            AddFunctionAsXml(@"
  <function>
    <name>eval_foreach_attribute_with_error_in_list</name>
    <return type=""xml"">
      <html xmlns:eval=""http://mindtouch.com/2007/dekiscript"">
        <body>
          <div foreach=""var x in num.abs(_)"">
            <eval:expr value=""x"" />
            <br />
          </div>
        </body>
      </html>
    </return>
  </function>
            ");
            Execute("eval_foreach_attribute_with_error_in_list()")
                .VerifyXml((doc) => {
                    Assert.AreEqual("warning", doc["body/div/span/@class"].AsText, doc.ToPrettyString());
                    var message = doc["body/div/span"].AsText;
                    Assert.IsTrue(message.StartsWith("missing value for parameter 'number' (index 0)"), message);
                });
        }

        [Test]
        public void Eval_foreach_attribute_with_error_in_where_clause() {
            AddFunctionAsXml(@"
  <function>
    <name>eval_foreach_attribute_with_error_in_where_clause</name>
    <param name=""value"" type=""list"" />
    <return type=""xml"">
      <html xmlns:eval=""http://mindtouch.com/2007/dekiscript"">
          <body>
            <div foreach=""var x in $value where num.abs(_)"">
              <eval:expr value=""x"" />
              <br />
            </div>
          </body>
      </html>
    </return>
  </function>
            ");
            Execute("eval_foreach_attribute_with_error_in_where_clause([1,2,3])")
                .VerifyXml((doc) => {
                    Assert.AreEqual("warning", doc["body/div/span/@class"].AsText, doc.ToPrettyString());
                    var message = doc["body/div/span"].AsText;
                    Assert.IsTrue(message.StartsWith("missing value for parameter 'number' (index 0)"), message);
                });
        }

        [Test]
        public void Eval_foreach_attribute_with_error_in_where_attribute() {
            AddFunctionAsXml(@"
  <function>
    <name>eval_foreach_attribute_with_error_in_where_attribute</name>
    <param name=""value"" type=""list"" />
    <return type=""xml"">
      <html xmlns:eval=""http://mindtouch.com/2007/dekiscript"">
          <body>
            <div foreach=""var x in $value"" where=""num.abs(_)"">
              <eval:expr value=""x"" />
              <br />
            </div>
          </body>
      </html>
    </return>
  </function>
            ");
            Execute("eval_foreach_attribute_with_error_in_where_attribute([1,2,3])")
                .VerifyXml((doc) => {
                    Assert.AreEqual("warning", doc["body/div/span/@class"].AsText, doc.ToPrettyString());
                    var message = doc["body/div/span"].AsText;
                    Assert.IsTrue(message.StartsWith("missing value for parameter 'number' (index 0)"), message);
                });
        }

        [Test]
        public void Eval_attribute_using_prefix() {
            AddFunctionAsXml(@"
  <function>
    <name>eval_attribute_using_prefix</name>
    <param name=""value"" type=""xml"" />
    <param name=""float"" type=""str"" />
    <return type=""xml"">
      <html xmlns:eval=""http://mindtouch.com/2007/dekiscript"">
        <body>
          <div eval:style=""'float:' .. $float .. ';'""><eval:expr value=""$value"" /></div>
        </body>
      </html>
    </return>
  </function>
            ");
            Execute(@"eval_attribute_using_prefix(<div>""Hello World""</div>,'right')")
                .VerifyXml((doc) => {
                    Assert.AreEqual("float:right;", doc["body/div/@style"].AsText, doc.ToPrettyString());
                    Assert.AreEqual("Hello World", doc["body/div/div"].AsText, doc.ToPrettyString());
                });
        }

        [Test]
        public void Eval_attribute_using_braces() {
            AddFunctionAsXml(@"
  <function>
    <name>eval_attribute_using_braces</name>
    <param name=""value"" type=""xml"" />
    <param name=""float"" type=""str"" />
    <return type=""xml"">
      <html xmlns:eval=""http://mindtouch.com/2007/dekiscript"">
        <body>
          <div style=""{{ 'float:' .. ($float ?? 'right') .. ';' }}""><eval:expr value=""$value"" /></div>
        </body>
      </html>
    </return>
  </function>
            ");
            Execute(@"eval_attribute_using_braces(<div>""Hello World""</div>,'right')")
                .VerifyXml((doc) => {
                    Assert.AreEqual("float:right;", doc["body/div/@style"].AsText, doc.ToPrettyString());
                    Assert.AreEqual("Hello World", doc["body/div/div"].AsText, doc.ToPrettyString());
                });
        }

        [Test]
        public void Eval_attribute_with_error() {
            AddFunctionAsXml(@"
  <function>
    <name>eval_attribute_with_error</name>
    <return type=""xml"">
      <html xmlns:eval=""http://mindtouch.com/2007/dekiscript"">
        <body>
          <div eval:style=""num.abs(_)"">
            <eval:expr value=""$value"" />
          </div>
        </body>
      </html>
    </return>
  </function>
            ");
            Execute("eval_attribute_with_error()")
                .VerifyXml((doc) => {
                    Assert.AreEqual("warning", doc["span/@class"].AsText, doc.ToPrettyString());
                    var message = doc["span"].AsText;
                    Assert.IsTrue(message.StartsWith("missing value for parameter 'number' (index 0)"), message);
                });
        }

        [Test]
        public void Return_xml_from_html() {
            AddFunctionAsXml(@"
   <function>
    <name>return_xml_from_html</name>
    <return type=""xml"">
      <html xmlns:eval=""http://mindtouch.com/2007/dekiscript"">
        <body>
          <strong>Hello World!</strong>
        </body>
      </html>
    </return>
  </function>
           ");
            Execute("return_xml_from_html()")
                .VerifyXml((doc) => Assert.AreEqual("Hello World!", doc["body/strong"].AsText, doc.ToPrettyString()));
        }

        [Test]
        public void Return_xml_from_code() {
            AddFunctionAsXml(@"
  <function>
    <name>return_xml_from_code</name>
    <return type=""xml"">
      &lt;html&gt;
        &lt;body&gt;
          &lt;strong&gt; 'Hello World!' &lt;/strong&gt;
        &lt;/body&gt;
      &lt;/html&gt;
    </return>
  </function>
           ");
            Execute("return_xml_from_code()")
                .VerifyXml((doc) => Assert.AreEqual("Hello World!", doc["body/strong"].AsText, doc.ToPrettyString()));
        }

        [Test]
        public void Can_return_nil() {
            AddFunction("return_nil")
                .Body("nil")
                .ReturnsNil();
            Execute("return_nil()")
                .Verify("nil", typeof(DekiScriptNil));
        }

        [Test]
        public void Can_return_bool() {
            AddFunction("return_bool")
                .Body("true")
                .ReturnsBool();
            Execute("return_bool()")
                .Verify("true", typeof(DekiScriptBool));
        }

        [Test]
        public void Can_return_num() {
            AddFunction("return_num")
                .Body("123")
                .ReturnsNum();
            Execute("return_num()")
                .Verify("123", typeof(DekiScriptNumber));
        }

        [Test]
        public void Can_return_num_from_str() {
            AddFunction("return_num_from_str")
                .Body("'123'")
                .ReturnsNum();
            Execute("return_num_from_str()")
                .Verify("123", typeof(DekiScriptNumber));
        }

        [Test]
        public void Can_return_str() {
            AddFunction("return_str")
                .Body("'Hello World!'")
                .ReturnsStr();
            Execute("return_str()")
                .Verify("Hello World!", typeof(DekiScriptString));
        }

        [Test]
        public void Can_return_str_from_num() {
            AddFunction("return_str_from_num")
                .Body("123")
                .ReturnsStr();
            Execute("return_str_from_num()")
                .Verify("123", typeof(DekiScriptString));
        }

        [Test]
        public void Calling_built_in_function() {
            AddFunction("foo")
                .Param("value", "num", "123")
                .Body("num.abs($.value)")
                .ReturnsNum();
            Execute("foo(-1)")
                .Verify("1", typeof(DekiScriptNumber));
        }

        [Test]
        public void Calling_recursive_function() {
            AddFunction("recurse")
                .Param("times", "num")
                .Param("count", "num", "0")
                .Body(@"
if($times > 0) {
  var times = $times - 1;
  var count = $count + 1;
  return recurse(times,count);
} else {
  return $count;
}
                ")
                .ReturnsNum();
            Execute("recurse(5)")
                .Verify("5", typeof(DekiScriptNumber));
        }

        [Test]
        public void Input_environment_is_not_affected_by_other_nested_function_calls() {
            AddFunction("entry")
                .Param("a", "str")
                .Param("b", "str")
                .Body(@"
var a = $a;
var b = $b;
return inner('xyz','uvw') && a == $a && b == $b;            
                ")
                .ReturnsBool();
            AddFunction("inner")
                .Param("a", "str")
                .Param("b", "str")
                .Body(@"
return $a == 'xyz' && $b == 'uvw';
                ")
                .ReturnsBool();
            Execute("entry('abc','def')")
                .Verify("true", typeof(DekiScriptBool));
        }

        [Ignore("this doesn't seem right")]
        [Test]
        public void Cannot_call_private_function_from_outside() {
            AddFunction("private_function")
                .Private()
                .Body("'foo'")
                .ReturnsStr();
            Execute("private_function()")
                .Verify("foo", typeof(DekiScriptString));
            Assert.Fail("hey!");
        }

        [Ignore("this doesn't seem right")]
        [Test]
        public void Cannot_call_internal_function_from_outside() {
            AddFunction("internal_function")
                .Internal()
                .Body("'foo'")
                .ReturnsStr();
            Execute("internal_function()")
                .Verify("foo", typeof(DekiScriptString));
            Assert.Fail("hey!");
        }

        [Test]
        public void Extensions_can_call_their_own_internal_functions() {
            AddFunction("internal_function")
                .Internal()
                .Body("'foo'")
                .ReturnsStr();
            AddFunction("call_internal_function")
                .Body("internal_function()")
                .ReturnsStr();
            Execute("call_internal_function()")
                .Verify("foo", typeof(DekiScriptString));
        }

        [Test]
        public void Raw_string_with_mismatched_quote_has_quote_escaped() {
            AddFunction("raw_string_with_mismatched_quote")
                .Body("\"\"\"\"hello\"\"\"")
                .ReturnsAny();
            Execute("raw_string_with_mismatched_quote()")
                .Verify("\"hello", typeof(DekiScriptString));
        }

        [Test]
        public void Simple_exception() {
            AddFunction("simple_exception")
                .Body("num.abs(_)")
                .ReturnsAny();
            Execute("simple_exception()")
                .VerifyXml((doc) => {
                    Assert.AreEqual("warning", doc["span/@class"].AsText, doc.ToPrettyString());
                    var message = doc["span"].AsText;
                    Assert.IsTrue(message.StartsWith("missing value for parameter 'number' (index 0)"), message);
                });
        }

        [Test]
        public void Simple_exception_handling() {
            AddFunction("simple_exception_handling")
                .Body("num.abs(_) !! \"oops!\"")
                .ReturnsAny();
            Execute("simple_exception_handling()")
                .Verify("oops!", typeof(DekiScriptString));
        }

        [Test]
        public void Can_access_full_environment() {
            AddFunction("return_env")
                .Body("__env")
                .ReturnsAny();
            Execute("return_env()")
                .Verify<DekiScriptMap>(value => {
                    Assert.IsFalse(value.IsEmpty, value.ToString());
                });
        }

        [Test]
        public void Return_invalid_type() {
            AddFunction("return_invalid_type")
                .Body("'Hello'")
                .ReturnsNum();
            Execute("return_invalid_type()")
                .VerifyException(e => {
                    Assert.AreEqual(typeof(DekiScriptInvokeException), e.GetType());
                    Assert.AreEqual(typeof(DekiScriptRemoteException), e.InnerException.GetType());
                    Assert.AreEqual("return value could not convert from 'str' to 'num'", e.InnerException.Message);
                });
        }

        [Ignore("should this really bubble up?")]
        [Test]
        public void Eval_with_bad_type_param_error() {
            AddFunction("eval_with_bad_type_param_error")
                .Body("num.abs(\"x\")")
                .ReturnsAny();
            Execute("eval_with_bad_type_param_error()")
                .VerifyException(e => {
                    Assert.AreEqual(typeof(DekiScriptInvokeException), e.GetType());
                    Assert.AreEqual(typeof(DekiScriptInvalidCastException), e.InnerException.GetType());
                });
            Assert.Fail();
        }

        [Ignore("or should this one indeed bubble up instead of being xml?")]
        [Test]
        public void Return_automatic_conversion_in_nested_call() {
            AddFunction("return_automatic_conversion")
                .Body("\"123\"")
                .ReturnsNum();
            AddFunction("return_automatic_conversion_in_nested_call")
                .Body("return_automatic_conversion()")
                .ReturnsAny();
            Execute("return_automatic_conversion_in_nested_call()")
                .Verify("123", typeof(DekiScriptNumber));
        }

        //--- Helpers ---
        private IFunctionDefinition AddFunction(string function) {
            return _scriptService.AddFunction(function);
        }
        private void AddFunctionAsXml(string functionDoc) {
            _scriptService.AddFunctionAsXml(functionDoc);
        }

        private void AddFunctionAsXml(XDoc functionDoc) {
            _scriptService.AddFunctionAsXml(functionDoc);
        }

        private IExecutionPlan Execute(string expr) {
            return _scriptService.Execute(expr);
        }
    }
}
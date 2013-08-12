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
using System.Threading;
using MindTouch.Deki.Script.Compiler;
using MindTouch.Deki.Script.Expr;
using MindTouch.Deki.Script.Runtime;
using MindTouch.Deki.Script.Runtime.TargetInvocation;
using MindTouch.Deki.Script.Tests.Util;
using MindTouch.Dream;
using MindTouch.Extensions.Time;
using MindTouch.Xml;

using NUnit.Framework;

namespace MindTouch.Deki.Script.Tests.ScriptTests {

    [TestFixture]
    public class ExecutionTests {

        //--- Fields ---
        private DekiScriptTester _dekiScriptTester;

        [SetUp]
        public void Setup() {
            _dekiScriptTester = new DekiScriptTester();
        }

        //--- Methods ---

        [Test]
        public void Parse_failure_should_throw() {
            try {
                _dekiScriptTester.Parse("if(if(false))");
                Assert.Fail("shouldn't have passed parse");
            } catch(DekiScriptParserException e) {
                Assert.AreEqual("invalid Primary: line 1, column 4", e.Message);
            } catch(Exception e) {
                Assert.Fail(string.Format("Caught wrong exception: {0}", e));
            }
        }

        [Test]
        public void DateNow_call() {
            DekiScriptExpression expr = _dekiScriptTester.Parse("date.now");
            DekiScriptEnv env = _dekiScriptTester.Runtime.CreateEnv();
            DekiScriptExpression result = _dekiScriptTester.Runtime.Evaluate(expr, DekiScriptEvalMode.Evaluate, env);
            Assert.IsAssignableFrom(typeof(DekiScriptString), result);
        }

        [Test]
        public void Definition_Assignment() {
            _dekiScriptTester.Test("var x = 5; let x = 10; x;", "10", typeof(DekiScriptNumber));
        }

        [Test]
        public void Defintion_without_assignment_followed_by_Assignment_should_not_throw() {
            _dekiScriptTester.Test("var x; let x = 10; x;", "10", typeof(DekiScriptNumber));
        }

        [Test]
        public void Assignment_without_definition_should_not_throw() {
            _dekiScriptTester.Test("let x = 5; 5;", "5", typeof(DekiScriptNumber));
        }

        [Test]
        public void Block_IfElse() {
            _dekiScriptTester.Test("if(5 > 10) { true } else { false }", "false", typeof(DekiScriptBool));
        }

        [Test]
        public void Blockless_IfElse() {
            _dekiScriptTester.Test("if(true) 5+5; else 10-5;", "10", typeof(DekiScriptNumber));
        }

        [Test]
        public void Block_nested_IfElseif() {
            _dekiScriptTester.Test("if(false) { 1 } else { if(true) { 2 } else { 3 } }", "2", typeof(DekiScriptNumber));
        }

        [Test]
        public void Blockless_nested_IfElseIf() {
            _dekiScriptTester.Test("if(false) 1; else if(true) 2; else 3;", "2", typeof(DekiScriptNumber));
        }

        [Test]
        public void Blockless_IfElse_with_Assignment() {
            _dekiScriptTester.Test("var x = 5; if(true) let x = 10; else let x = 15; x;", "10", typeof(DekiScriptNumber));
        }

        [Test]
        public void Block_IfElse_with_Statements() {
            _dekiScriptTester.Test("if(true) { 5; 6 } else { 7 }", "56", typeof(DekiScriptString));
        }

        [Test]
        public void Blockless_IfElse_with_Statements_should_throw() {
            try {
                _dekiScriptTester.Parse("if(true) 5; 6 else 7");
                Assert.Fail("shouldn't have passed parse");
            } catch(DekiScriptParserException e) {
                Assert.AreEqual("EOF expected: line 1, column 15", e.Message);
            } catch(Exception e) {
                Assert.Fail(string.Format("Caught wrong exception: {0}", e));
            }
        }

        [Test]
        public void Blockless_IfElse_with_Definition() {
            _dekiScriptTester.Parse("if(true) var x = 5; else 7;");
        }

        [Test]
        public void Blockless_IfElse_with_Foreach_body() {
            _dekiScriptTester.Test("if(false) 5; else foreach(var x in [ 1, 2, 3 ]) x;", "123", typeof(DekiScriptString));
        }

        [Test]
        public void Blockless_Foreach_with_IfElse_body() {
            _dekiScriptTester.Test("foreach(var x in [ 1, 2, 3 ]) if(x==1) \"a\"; else if(x==2) \"b\"; else \"c\";", "abc", typeof(DekiScriptString));
        }

        [Test]
        public void Block_Foreach() {
            _dekiScriptTester.Test("foreach(var x in [ 1, 2, 3 ]) { x }", "123", typeof(DekiScriptString));
        }

        [Test]
        public void Blockless_Foreach() {
            _dekiScriptTester.Test("foreach(var x in [ 1, 2, 3 ])  x;", "123", typeof(DekiScriptString));
        }

        [Test]
        public void Foreach_with_multiple_expression_body() {
            _dekiScriptTester.Test("foreach(var y in [1,2,3]) { y+1; y-1; }", "203142", typeof(DekiScriptString));

        }
        [Test]
        public void Switch_without_cases() {
            _dekiScriptTester.Test("switch(2) {  } 5;", "5", typeof(DekiScriptNumber));
        }

        [Test]
        public void Switch_with_single_statement_cases() {
            _dekiScriptTester.Test("switch(2) { case 1: \"a\"; case 2: \"b\"; }", "b", typeof(DekiScriptString));
        }

        [Test]
        public void Switch_with_multi_statement_cases() {
            _dekiScriptTester.Test("switch(2) { case 1: \"a\"; 1; case 2: \"b\"; 2; }", "b2", typeof(DekiScriptString));
        }

        [Test]
        public void Switch_with_block_statement_cases() {
            _dekiScriptTester.Test("switch(2) { case 1: { \"a\"; 1; } case 2: { \"b\"; 2; } }", "b2", typeof(DekiScriptString));
        }

        [Test]
        public void Switch_with_case_fallthrough() {
            _dekiScriptTester.Test("switch(1) { case 0: \"x\" case 1: case 2: \"b\"; case 3: \"c\" }", "b", typeof(DekiScriptString));
        }

        [Test]
        public void Switch_with_default() {
            _dekiScriptTester.Test("switch(10) { case 1: 10; default: 15; case 2: 20; }", "15", typeof(DekiScriptNumber));
        }

        [Test]
        public void Switch_with_default_that_is_not_triggered() {
            _dekiScriptTester.Test("switch(2) { case 1: 10; default: 15; case 2: 20; }", "20", typeof(DekiScriptNumber));
        }

        [Test]
        public void Switch_with_default_as_fallthrough() {
            _dekiScriptTester.Test("switch(10) { case 1: 10; default: case 2: 20; }", "20", typeof(DekiScriptNumber));
        }

        [Test]
        public void Switch_with_default_as_fallthrough_that_is_not_triggered() {
            _dekiScriptTester.Test("switch(3) { case 1: 10; default: case 2: 20; case 3: 30}", "30", typeof(DekiScriptNumber));
        }

        [Test]
        public void Switch_redefining_var_in_local_scope_should_affect_outer_scope() {
            _dekiScriptTester.Test("var x = 5, y = 1; switch(x) { case 5: { var x = 10; let y = 2; } }; x; y;", "102", typeof(DekiScriptString));
        }

        [Test]
        public void Single_variable_definition() {
            _dekiScriptTester.Test("var x = 5; x", "5", typeof(DekiScriptNumber));
        }

        [Test]
        public void Single_variable_definition_without_assigment() {
            _dekiScriptTester.Test("var x; let x = 5; x", "5", typeof(DekiScriptNumber));
        }

        [Test]
        public void Multiple_variable_definition() {
            _dekiScriptTester.Test("var x = 5, y = 10; x;y;", "510", typeof(DekiScriptString));
        }

        [Test]
        public void Multiple_variable_definition_with_some_assignment() {
            _dekiScriptTester.Test("var x, y = 5, z; let x = 1; let z = 20; x; y; z;", "1520", typeof(DekiScriptString));
        }

        [Test]
        public void Break_should_throw_on_eval() {
            DekiScriptExpression expr = _dekiScriptTester.Parse("break");
            DekiScriptEnv env = _dekiScriptTester.Runtime.CreateEnv();
            try {
                _dekiScriptTester.Runtime.Evaluate(expr, DekiScriptEvalMode.Evaluate, env);
                Assert.Fail("shouldn't have passed eval");
            } catch(DekiScriptBreakException e) {

                // nothing to do
            } catch(Exception e) {
                Assert.Fail(string.Format("Caught wrong exception: {0}", e));
            }
        }

        [Test]
        public void Continue_should_throw_on_eval() {
            DekiScriptExpression expr = _dekiScriptTester.Parse("continue");
            DekiScriptEnv env = _dekiScriptTester.Runtime.CreateEnv();
            try {
                _dekiScriptTester.Runtime.Evaluate(expr, DekiScriptEvalMode.Evaluate, env);
                Assert.Fail("shouldn't have passed eval");
            } catch(DekiScriptContinueException e) {

                // nothing to do
            } catch(Exception e) {
                Assert.Fail(string.Format("Caught wrong exception: {0}", e));
            }
        }

        [Test]
        public void Switch_with_continue_in_case_should_throw() {
            DekiScriptExpression expr = _dekiScriptTester.Parse("var x; switch(1) { case 1: let x = 5; continue; let x = 10; }");
            DekiScriptEnv env = _dekiScriptTester.Runtime.CreateEnv();
            try {
                _dekiScriptTester.Runtime.Evaluate(expr, DekiScriptEvalMode.Evaluate, env);
                Assert.Fail("shouldn't have passed eval");
            } catch(DekiScriptContinueException e) {

                // nothing to do
            } catch(Exception e) {
                Assert.Fail(string.Format("Caught wrong exception: {0}", e));
            }
        }

        [Test]
        public void Switch_with_break_in_case_should_exit_case() {
            _dekiScriptTester.Test("var x; switch(1) { case 1: let x = 5; break; let x = 10; }; x;", "5", typeof(DekiScriptNumber));
        }

        [Test]
        public void Break_in_foreach_should_exit_loop() {
            _dekiScriptTester.Test("foreach (var y in [1,2,3,4]) { if( y == 3 ) break; y; }", "12", typeof(DekiScriptString));
        }

        [Test]
        public void Break_in_foreach_inside_foreach_should_exit_inner_loop_only() {
            _dekiScriptTester.Test(
                "foreach (var x in [1,2])" +
                "  foreach (var y in [4,5,6])" +
                "  {" +
                "     x; if( y == 5 ) break; y;" +
                "  }",
                "141242", typeof(DekiScriptString));
        }

        [Test]
        public void Break_in_compound_statement_in_foreach_should_accumulate_state_up_to_break() {
            _dekiScriptTester.Test(
                "foreach (var x in [1,2,3,4])" +
                "{" +
                "  x; x+1; if( x == 2 ) { x-2; break; } x-1;" +
                "}",
                "120230", typeof(DekiScriptString));
        }

        [Test]
        public void Continue_in_foreach_should_skip_current_iteration() {
            _dekiScriptTester.Test("foreach (var y in [1,2,3,4]) { if( y == 2 ) continue; y; }", "134", typeof(DekiScriptString));
        }

        [Test]
        public void Continue_in_foreach_inside_foreach_should_skip_in_inner_loop_only() {
            _dekiScriptTester.Test(
                "foreach(var x in [1,2])" +
                "  foreach(var y in [4,5,6])" +
                "  {" +
                "     x; if( y == 5 ) continue; y;" +
                "  }",
                "1411624226", typeof(DekiScriptString));
        }

        [Test]
        public void Continue_in_compound_statement_in_foreach_should_accumulate_state_up_to_continue() {
            _dekiScriptTester.Test(
                "foreach (var x in [1,2,3,4])" +
                "{" +
                "  x; x+1; if( x < 3 ) { x-2; continue; } x-1;" +
                "}",
                "12-1230342453", typeof(DekiScriptString));
        }

        [Test]
        public void Left_associativity_of_AND_OR_operators() {
            _dekiScriptTester.TestAST("1 > 2 && 2 > 5 || 6 > 7 && 2 == 3", "((1 > 2) && (2 > 5)) || ((6 > 7) && (2 == 3))");
            _dekiScriptTester.TestAST("1 > 2 && 2 > 5 && 6 > 7 && 2 == 3", "(((1 > 2) && (2 > 5)) && (6 > 7)) && (2 == 3)");
        }

        [Test]
        public void Left_associativity_of_null_coalescing() {
            _dekiScriptTester.TestAST("5 ?? 6 ?? 7", "(5 ?? 6) ?? 7");
        }

        [Test]
        public void Left_associativity_in_relative_comparison() {
            _dekiScriptTester.TestAST("5 > 4", "5 > 4");
            _dekiScriptTester.TestAST("2 > 4 > 2", "(2 > 4) > 2");
        }

        [Test]
        public void Left_associativity_in_uri_append() {
            _dekiScriptTester.TestAST("\"http://localhost\" & \"foo\" & { q: 1 } & \"bar\" & { p: 2 }", "(((\"http://localhost\" & \"foo\") & { q : 1 }) & \"bar\") & { p : 2 }");
        }

        [Test]
        public void Left_associativity_in_add_subtract_multiply_divide() {
            _dekiScriptTester.Test("6 / 3 / 2", "1", typeof(DekiScriptNumber));
            _dekiScriptTester.Test("1 + 2 - (3 + 1)", "-1", typeof(DekiScriptNumber));
            _dekiScriptTester.Test("1 + 2 - 3 + 1", "1", typeof(DekiScriptNumber));
            _dekiScriptTester.Test("9 + 5 * 2 - 2 + 3", "20", typeof(DekiScriptNumber));
            _dekiScriptTester.Test("4 + 3 * 2 - 1", "9", typeof(DekiScriptNumber));
            _dekiScriptTester.Test("10 - 3 * 2 + 1", "5", typeof(DekiScriptNumber));
            _dekiScriptTester.Test("8 - 1 + 1", "8", typeof(DekiScriptNumber));
        }

        [Test]
        public void Expression_lists_with_nil_values() {
            _dekiScriptTester.TestAST("(1; 2; var x = 3; nil; x)", "(1; 2; var x = 3; x)");
        }

        [Test]
        public void Expression_lists_in_parentheses() {
            _dekiScriptTester.Test("(123)", "123", typeof(DekiScriptNumber));
            _dekiScriptTester.Test("(var x = 123; x)", "123", typeof(DekiScriptNumber));
            _dekiScriptTester.Test("var y = (var x = 123; x); y + 1", "124", typeof(DekiScriptNumber));
            _dekiScriptTester.Test("(1; 2; 3;)", "123", typeof(DekiScriptString));
        }

        [Test]
        public void Expressions_evaluating_to_nil() {
            _dekiScriptTester.Test("", "nil", typeof(DekiScriptNil));
            _dekiScriptTester.Test("_", "nil", typeof(DekiScriptNil));
            _dekiScriptTester.Test("nil", "nil", typeof(DekiScriptNil));
            _dekiScriptTester.Test("null", "nil", typeof(DekiScriptNil));
            _dekiScriptTester.Test("_ + 1", "nil", typeof(DekiScriptNil));
            _dekiScriptTester.Test("true && _", "nil", typeof(DekiScriptNil));
            _dekiScriptTester.Test("_ || true", "true", typeof(DekiScriptBool));
        }

        [Test]
        public void Scoped_expressions() {
            _dekiScriptTester.Test("var x = 1; var x = 2; x", "2", typeof(DekiScriptNumber));
            _dekiScriptTester.Test("var x = 1; (var x = 2); x", "2", typeof(DekiScriptNumber));
            _dekiScriptTester.Test("var x = 1; (let x = 2); x", "2", typeof(DekiScriptNumber));
        }

        [Test]
        public void Xml_empty_element() {
            _dekiScriptTester.Test("<br/>", "<br />", typeof(DekiScriptXml));
        }

        [Test]
        public void Xml_simple_element() {
            _dekiScriptTester.Test("<foo>'test'</foo>", "<foo>test</foo>", typeof(DekiScriptXml));
        }

        [Test]
        public void Xml_dynamic_element() {
            _dekiScriptTester.Test("var x = 1; <foo a='abc' b=(1+2) >x + 1</>", "<foo a=\"abc\" b=\"3\">2</foo>", typeof(DekiScriptXml));
        }

        [Test]
        public void Xml_improperly_closed_element() {
            try {
                _dekiScriptTester.Parse("<foo>'test'</bar>");
                Assert.Fail("shouldn't have passed parse");
            } catch(DekiScriptParserException e) {
                Assert.AreEqual("closing tag mismatch, found </bar>, expected </foo>: line 1, column 14", e.Message);
            } catch(Exception e) {
                Assert.Fail(string.Format("Caught wrong exception: {0}", e));
            }
        }

        [Test]
        public void Xml_expressions_separated_by_semicolon() {
            _dekiScriptTester.Test(
                @"<html><head><script type=""text/javascript"" src=""http://foo""/>;
                <script type=""text/javascript"" src=""http://bar""/>;
                <script type=""text/javascript"" src=""http://baz""/>;</head><body /></html>",
                @"<html><head><script type=""text/javascript"" src=""http://foo"" /><script type=""text/javascript"" src=""http://bar"" /><script type=""text/javascript"" src=""http://baz"" /></head><body /></html>",
                typeof(DekiScriptXml));
        }

        [Test]
        public void Xml_expressions_without_semicolon_safemode() {
            _dekiScriptTester.Test(
                @"let __safe = false;
                <html>
                    <head>
                        <script type=""text/javascript"" src=""http://foo""/>;
                        <script type=""text/javascript"" src=""http://bar""/>;
                        <script type=""text/javascript"" src=""http://baz""/>;
                    </head>
                    <body />
                </html>",
                @"<html><body /></html>",
                typeof(DekiScriptXml), true);
        }

        [Test]
        public void Xml_expressions_without_semicolons() {
            _dekiScriptTester.Test(
                @"<html><head><script type=""text/javascript"" src=""http://foo""/>
                <script type=""text/javascript"" src=""http://bar""/>
                <script type=""text/javascript"" src=""http://baz""/></head><body /></html>",
                @"<html><head><script type=""text/javascript"" src=""http://foo"" /><script type=""text/javascript"" src=""http://bar"" /><script type=""text/javascript"" src=""http://baz"" /></head><body /></html>",
                typeof(DekiScriptXml));
        }

        [Test]
        public void Multiple_xml_expressions() {
            _dekiScriptTester.Test(
                @"<foo/><bar/>",
                @"<html><body><foo /><bar /></body></html>",
                typeof(DekiScriptXml));
        }

        [Test]
        public void OutputBuffer_single_value_followed_by_xml() {
            _dekiScriptTester.Test(
                @"1;<bar/>",
                @"<html><body>1<bar /></body></html>",
                typeof(DekiScriptXml));
        }

        [Test]
        public void OutputBuffer_multiple_values_followed_by_xml() {
            _dekiScriptTester.Test(
                @"1; 2; <bar/>",
                @"<html><body>12<bar /></body></html>",
                typeof(DekiScriptXml));
        }

        [Test]
        public void OutputBuffer_xml_followed_by_a_value() {
            _dekiScriptTester.Test(
                @"<bar/>; 1",
                @"<html><body><bar />1</body></html>",
                typeof(DekiScriptXml)
            );
        }

        [Test]
        public void OutputBuffer_html_followed_by_a_value() {
            _dekiScriptTester.Test(
                @"<html><body><bar /></body></html>; 1",
                @"<html><body><bar />1</body></html>",
                typeof(DekiScriptXml)
            );
        }

        [Test]
        public void OutputBuffer_nil() {
            _dekiScriptTester.Test(
                @"_",
                @"nil",
                typeof(DekiScriptNil)
            );
        }

        [Test]
        public void OutputBuffer_single_value() {
            _dekiScriptTester.Test(
                @"1",
                @"1",
                typeof(DekiScriptNumber)
            );
        }

        [Test]
        public void OutputBuffer_unsafe_html_with_head_and_tail() {
            _dekiScriptTester.Test(
                @"<html><head><style>'css'</style></head><body /><tail><script>'code'</script></tail></html>",
                @"<html><head><style>css</style></head><body /><tail><script>code</script></tail></html>",
                typeof(DekiScriptXml), false
            );
        }

        [Test]
        public void OutputBuffer_safe_html_with_head_and_tail() {
            _dekiScriptTester.Test(
                @"<html><head><style>'css'</style></head><body /><tail><script>'code'</script></tail></html>",
                @"<html><body /></html>",
                typeof(DekiScriptXml), true
            );
        }

        [Test]
        public void OutputBuffer_nested_body() {
            _dekiScriptTester.Test(
                @"<body>1</body>",
                @"<html><body>1</body></html>",
                typeof(DekiScriptXml)
            );
        }

        [Test]
        public void OutputBuffer_html_with_invalid_element() {
            _dekiScriptTester.Test(
                @"<html><body>1</body><invalid>789</invalid></html>",
                @"<html><body>1</body></html>",
                typeof(DekiScriptXml)
            );
        }

        [Test]
        public void OutputBuffer_html_head_with_invalid_element() {
            _dekiScriptTester.Test(
                @"<html><head><invalid>789</invalid></head><body>1</body></html>",
                @"<html><body>1</body></html>",
                typeof(DekiScriptXml)
            );
        }

        [Test]
        public void OutputBuffer_html_tail_with_invalid_element() {
            _dekiScriptTester.Test(
                @"<html><body>1</body><tail><invalid>789</invalid></tail></html>",
                @"<html><body>1</body></html>",
                typeof(DekiScriptXml)
            );
        }

        [Test]
        public void OutputBuffer_html_with_multiple_bodies_using_append() {
            _dekiScriptTester.Test(
                @"<html><body target=""foo"" conflict=""append"">1</body></html>; <html><body target=""foo"" conflict=""append"">2</body></html>; ",
                @"<html><body /><body target=""foo"">12</body></html>",
                typeof(DekiScriptXml)
            );
        }

        [Test]
        public void OutputBuffer_html_with_multiple_distinct_bodies() {
            _dekiScriptTester.Test(
                @"<html><body target=""foo"">1</body></html>; <html><body target=""bar"">2</body></html>; ",
                @"<html><body /><body target=""bar"">2</body><body target=""foo"">1</body></html>",
                typeof(DekiScriptXml)
            );
        }

        [Test]
        public void OutputBuffer_html_with_duplicate_head_elements() {
            _dekiScriptTester.Test(
                @"<html><head><script type=""text/javascript"" src=""foo"" /><script type=""text/javascript"" src=""bar"" /><script type=""text/javascript"" src=""foo"" /><script type=""text/javascript"" src=""abc"" /></head><body>1</body></html>; ",
                @"<html><head><script type=""text/javascript"" src=""foo"" /><script type=""text/javascript"" src=""bar"" /><script type=""text/javascript"" src=""abc"" /></head><body>1</body></html>",
                typeof(DekiScriptXml)
            );
        }

        [Test]
        public void OutputBuffer_html_with_duplicate_tail_elements() {
            _dekiScriptTester.Test(
                @"<html><body>1</body><tail><script type=""text/javascript"">""foo""</script><script type=""text/javascript"">""bar""</script><script type=""text/javascript"">""foo""</script><script type=""text/javascript"">""abc""</script></tail></html>; ",
                @"<html><body>1</body><tail><script type=""text/javascript"">foo</script><script type=""text/javascript"">bar</script><script type=""text/javascript"">abc</script></tail></html>",
                typeof(DekiScriptXml)
            );
        }

        [Test]
        public void OutputBuffer_web_uri_followed_by_html() {
            var expr = DekiScriptExpression.Block(Location.None, new[] { DekiScriptExpression.Constant(new XUri("http://foo/index.html")), DekiScriptExpression.Constant(new XDoc("html").Elem("body", "test")) });
            _dekiScriptTester.Test(
                expr,
                @"<html><body><a rel=""custom nofollow"" href=""http://foo/index.html"">http://foo/index.html</a>test</body></html>",
                typeof(DekiScriptXml), false
            );
        }

        [Test]
        public void OutputBuffer_image_uri_followed_by_html() {
            var expr = DekiScriptExpression.Block(Location.None, new[] { DekiScriptExpression.Constant(new XUri("http://foo/index.png")), DekiScriptExpression.Constant(new XDoc("html").Elem("body", "test")) });
            _dekiScriptTester.Test(
                expr,
                @"<html><body><img src=""http://foo/index.png"" />test</body></html>",
                typeof(DekiScriptXml), false
            );
        }

        [Test]
        public void OutputBuffer_web_uri_inside_xml() {
            var expr = DekiScriptExpression.XmlElement(Location.None, null, DekiScriptExpression.Constant("doc"), null, DekiScriptExpression.Constant(new XUri("http://foo/index.html")));
            _dekiScriptTester.Test(
                expr,
                @"<doc><a rel=""custom nofollow"" href=""http://foo/index.html"">http://foo/index.html</a></doc>",
                typeof(DekiScriptXml), false
            );
        }

        [Test]
        public void OutputBuffer_single_attribute() {
            _dekiScriptTester.Test(
                @"var x = <doc><node id='1'>'a'</node></doc>; x['//@id']",
                @"id=""1""",
                typeof(DekiScriptXml)
            );
        }

        [Test]
        public void OutputBuffer_single_attribute_followed_by_html() {
            _dekiScriptTester.Test(
                @"var x = <doc><node id='1'>'a'</node></doc>; x['//@id']; <bar/>",
                @"<html><body>id=""1""<bar /></body></html>",
                typeof(DekiScriptXml)
            );
        }

        [Test]
        public void OutputBuffer_single_attribute_followed_by_a_value() {
            _dekiScriptTester.Test(
                @"var x = <doc><node id='1'>'a'</node></doc>; x['//@id']; 'hi'",
                @"id=""1""hi",
                typeof(DekiScriptString)
            );
        }

        [Test]
        public void OutputBuffer_multiple_attributes() {
            _dekiScriptTester.Test(
                @"var x = <doc><node id='1'>'a'</node><node id='2'>'b'</node><node id='3'>'c'</node></doc>; x['//@id']",
                @"id=""1""",
                typeof(DekiScriptXml)
            );
        }

        [Test]
        public void OutputBuffer_multiple_attributes_as_list() {
            _dekiScriptTester.Test(
                @"var x = <doc><node id='1'>'a'</node><node id='2'>'b'</node><node id='3'>'c'</node></doc>; [ y foreach var y in x['//@id'] ]",
                @"[ id=""1"", id=""2"", id=""3"" ]",
                typeof(DekiScriptList)
            );
        }

        [Test]
        public void OutputBuffer_nested_text_in_html() {
            _dekiScriptTester.Test(
                @"var x = <html><body>""hello world""</body></html>; <foo><bar> x </bar></foo>;",
                @"<foo><bar>hello world</bar></foo>",
                typeof(DekiScriptXml)
            );
        }

        [Test]
        public void OutputBuffer_nested_formatted_text_in_html() {
            _dekiScriptTester.Test(
                @"var x = <html><body><strong>""hello"";</strong><strong>""world""</strong></body></html>; <foo><bar> x </bar></foo>;",
                @"<foo><bar><strong>hello</strong><strong>world</strong></bar></foo>",
                typeof(DekiScriptXml)
            );
        }

        [Test]
        public void OutputBuffer_nested_html_document() {
            _dekiScriptTester.Test(
                @"var x = <html><head><script src=""test.xml"" /></head><body><strong>""hello"";</strong><strong>""world""</strong> </body></html>; <foo><bar> x </bar></foo>;",
                @"<html><head><script src=""test.xml"" /></head><body><foo><bar><strong>hello</strong><strong>world</strong></bar></foo></body></html>",
                typeof(DekiScriptXml)
            );
        }

        [Test]
        public void OutputBuffer_nested_xml_in_xml() {
            _dekiScriptTester.Test(
                @"var x = <xyz>""hello"";</xyz>; <foo><bar> x </bar></foo>;",
                @"<foo><bar><xyz>hello</xyz></bar></foo>",
                typeof(DekiScriptXml)
            );
        }

        [Test]
        public void Dom_evaluation_with_static_content() {
            XDoc doc = new XDoc("html").UsePrefix("eval", "http://mindtouch.com/2007/dekiscript")
                .Start("body")
                    .Start("div").Attr("block", "var x = string.toupper('foo')")
                        .Elem("eval:expr", "x .. 3; string.nbsp")
                    .End()
                .End();

            // parse node
            DekiScriptExpression node = DekiScriptParser.Parse(doc);
            Assert.AreEqual("<html><body>(discard (var x = string.toupper(\"foo\")); <div>(x .. 3; string.nbsp)</div>) !! web.showerror(__error)</body></html> !! web.showerror(__error)", node.ToString());

            // TODO (steveb): disabled the partial evaluation test for now

            // partial evaluation
            //node = node.Optimize(DekiScriptEvalMode.Evaluate, DekiScriptEnv.Create());
            //Assert.AreEqual("<html><body><div>\"FOO3 \"; </div></body></html>", node.ToString());

            // full evaluation
            //XDoc value = node.Evaluate(DekiScriptEvalMode.Evaluate, DekiScriptEnv.Create()).AsEmbeddableXml(false);
            //Assert.AreEqual("<html><body><div>FOO3&nbsp;</div></body></html>", value.ToXHtml());
        }

        [Test]
        public void Dom_evaluation_with_dynamic_content() {
            XDoc doc = new XDoc("html").UsePrefix("eval", "http://mindtouch.com/2007/dekiscript")
                .Start("body")
                    .Start("div").Attr("block", "var x = string.toupper(date.now)")
                        .Elem("eval:expr", "x .. 3; string.nbsp")
                    .End()
                .End();

            // parse node
            DekiScriptExpression node = DekiScriptParser.Parse(doc);
            Assert.AreEqual("<html><body>(discard (var x = string.toupper(date.now)); <div>(x .. 3; string.nbsp)</div>) !! web.showerror(__error)</body></html> !! web.showerror(__error)", node.ToString());

            // TODO (steveb): disabled the partial evaluation test for now

            // partial evaluation
            //node = node.Optimize(DekiScriptEvalMode.Evaluate, DekiScriptEnv.Create());
            //Assert.AreEqual("<html><body>discard (var x = \"native:///string.toupper\"( \"native:///$date.now\"())); <div>x .. 3; \" \"; </div>; </body></html>", node.ToString());

            // full evaluation
            //string now = DekiScriptLibrary.DateNow();
            //XDoc value = node.Evaluate(DekiScriptEvalMode.Evaluate, DekiScriptEnv.Create()).AsEmbeddableXml(false);
            //Assert.AreEqual("<html><body><div>" + now.ToUpperInvariant() + "3&nbsp;</div></body></html>", value.ToXHtml());
        }

        // TODO (steveb): 
        //  <eval:if test="true"></eval:if>
        //  <eval:if test="false"></eval:if><eval:elseif test="x"></eval:elseif><eval:else></eval:else>
        //  <eval:if test="x"></eval:if><eval:elseif test="true"></eval:elseif><eval:else></eval:else>
        //  <div function="test"><p>hello</p></div>

        [Ignore("partial evaluation broke with the introduction of generators")]
        [Test]
        public void Dom_partial_evaluation_for_foreach1() {
            const string source = "<html xmlns:eval='http://mindtouch.com/2007/dekiscript'><body init='var x = date.now'><div foreach='var y in num.series(1, 5)' where='y != x'><eval:expr>y</eval:expr></div></body></html>";
            XDoc doc = XDocFactory.From(source, MimeType.XML);

            DekiScriptExpression node = DekiScriptParser.Parse(doc);
            node = _dekiScriptTester.Runtime.Optimize(node, DekiScriptEvalMode.Evaluate, _dekiScriptTester.Runtime.CreateEnv());
            Assert.AreEqual("<html>/* discard */ (var x = \"native:///$date.now\"()); <body>if((1 != x)) { <div>1; </div>}if((2 != x)) { <div>2; </div>}if((3 != x)) { <div>3; </div>}if((4 != x)) { <div>4; </div>}if((5 != x)) { <div>5; </div>}</body></html>", node.ToString());
        }

        [Ignore("partial evaluation broke with the introduction of generators")]
        [Test]
        public void Dom_partial_evaluation_for_foreach2() {
            const string source = "<html xmlns:eval='http://mindtouch.com/2007/dekiscript'><body init='var x = 2'><div foreach='var y in num.series(1, 5)' where='y != x'><eval:expr>y</eval:expr></div></body></html>";
            XDoc doc = XDocFactory.From(source, MimeType.XML);

            DekiScriptExpression node = DekiScriptParser.Parse(doc);
            node = _dekiScriptTester.Runtime.Optimize(node, DekiScriptEvalMode.Evaluate, _dekiScriptTester.Runtime.CreateEnv());
            Assert.AreEqual("<html><body><div>1; </div><div>3; </div><div>4; </div><div>5; </div></body></html>", node.ToString());
        }

        [Ignore("partial evaluation broke with the refactoring of the output buffer")]
        [Test]
        public void Partial_evaluation_for_assign_and_access() {
            _dekiScriptTester.TestPartialEval("var x = 123; let x = 456; x", "456");
            _dekiScriptTester.TestPartialEval("var x = 1 + date.now; x", "var x = 1 + \"native:///$date.now\"(); x");
        }

        [Test]
        public void Partial_evaluation_for_ifelse() {
            _dekiScriptTester.TestPartialEval("if(1 + 1 >= 2) 123; else 456;", "123");
            _dekiScriptTester.TestPartialEval("if(1 + 1 < 2) 123; else 456;", "456");
            _dekiScriptTester.TestPartialEval("if(1 + 1 < 2) 123; else if(true) 456; else 789;", "456");
            _dekiScriptTester.TestPartialEval("if(1 + 1 < 2) 123; else if(false) 456; else 789;", "789");
            _dekiScriptTester.TestPartialEval("if(date.now) 12 .. 3; else 4 .. 5 .. 6;", "if(\"native:///$date.now\"()) { \"123\" } else { \"456\" }");
        }

        [Test]
        public void Partial_evaluation_for_binary_operations() {

            // TODO (steveb): short-circuiting for partial evaluation

            _dekiScriptTester.TestPartialEval("1 + 2", "3");
            _dekiScriptTester.TestPartialEval("1 + date.now", "1 + \"native:///$date.now\"()");
        }

        [Ignore("partial evaluation broke with the refactoring of the output buffer")]
        [Test]
        public void Partial_evaluation_for_properties_and_calls() {
            _dekiScriptTester.TestPartialEval("date.month(date.now)", "\"native:///date.month\"(\"native:///$date.now\"())");
            _dekiScriptTester.TestPartialEval("string.toupper('hello')", "\"HELLO\"");
        }

        [Test]
        public void Partial_evaluation_for_switch() {
            _dekiScriptTester.TestPartialEval("switch(1) { case 1: 1 + 2 + 3; case 2: 345; }", "6");
            _dekiScriptTester.TestPartialEval("switch(3) { case 1: 123; case 2: 345; }", "nil");
            _dekiScriptTester.TestPartialEval("switch(3) { case 1: 123; case 2: 345; default: 789; }", "789");

            // TODO (steveb): handle 'break' statements
            //_dekiScriptTester.TestPartialEval("switch(3) { case 1: 123; case 2: 345; default: 789; break; }", "789");

            _dekiScriptTester.TestPartialEval("switch(date.now) { case 1: 123; case 2: 345; default: 789; }", "switch(\"native:///$date.now\"()) { case 1: 123; case 2: 345; default: 789; }");
            _dekiScriptTester.TestPartialEval("switch(3) { case date.now: 123; case 2: 345; default: 789; }", "switch(3) { case \"native:///$date.now\"(): 123; default: 789; }");
        }

        [Test]
        public void Partial_evaluation_for_foreach() {

            // TODO (steveb): handle loop unrolling, incl. break & continue
            //_dekiScriptTester.TestPartialEval("foreach(var x in num.series(1, 5)) x;", "\"12345\"");
        }

        [Test]
        public void Partial_evaluation_for_magicid() {
            _dekiScriptTester.TestPartialEval("@id", "@id");
        }

        [Ignore("partial evaluation broke with the refactoring of the output buffer")]
        [Test]
        public void Partial_evaluation_for_expression_lists() {
            _dekiScriptTester.TestPartialEval("(1; 2; 3)", "\"123\"");
            _dekiScriptTester.TestPartialEval("(1; 2; var x = 3; x)", "\"123\"");
            _dekiScriptTester.TestPartialEval("(1; 2; var x = 3; 1 + nil; x)", "\"123\"");
            _dekiScriptTester.TestPartialEval("(var x = 1; x + 2)", "3");
        }

        [Test]
        public void ListForeach_parse() {
            _dekiScriptTester.TestAST("[ x foreach var x in [1, 2, 3]]", "[ x foreach var x in [ 1, 2, 3 ] ]");
            _dekiScriptTester.TestAST("[ x, y foreach var x in [1, 2, 3] where x % 2 == 1]", "[ x, y foreach var x in [ 1, 2, 3 ], if (x % 2) == 1 ]");
            _dekiScriptTester.TestAST("[ x, y foreach var x in [1, 2, 3] where x % 2 == 1, var y = x + 1 ]", "[ x, y foreach var x in [ 1, 2, 3 ], if (x % 2) == 1, var y = x + 1 ]");
            _dekiScriptTester.TestAST("[ k..v foreach var k : v in { a : 1, b : 2, c : 3 } ]", "[ k .. v foreach var k : v in { a : 1, b : 2, c : 3 } ]");
            _dekiScriptTester.TestAST("[ x, y foreach var x, y in [1, 2, 3] ]", "[ x, y foreach var x, y in [ 1, 2, 3 ] ]");
        }

        [Test]
        public void ListForeach_eval() {
            _dekiScriptTester.Test("[ x foreach var x in [1, 2, 3]]", "[ 1, 2, 3 ]", typeof(DekiScriptList));
            _dekiScriptTester.Test("[ x foreach var x in [1, 2, 3] where x % 2 == 1]", "[ 1, 3 ]", typeof(DekiScriptList));
            _dekiScriptTester.Test("[ x, y foreach var x in [1, 2, 3] where x % 2 == 1, var y = x + 1 ]", "[ 1, 2, 3, 4 ]", typeof(DekiScriptList));
            _dekiScriptTester.Test("[ k..v foreach var k : v in { a : 1, b : 2, c : 3 } ]", "[ \"a1\", \"b2\", \"c3\" ]", typeof(DekiScriptList));
            _dekiScriptTester.Test("[ x, y foreach var x, y in [1, 2, 3] ]", "[ 1, 2 ]", typeof(DekiScriptList));
            _dekiScriptTester.Test("[ __count foreach var x in [7, 8, 9]]", "[ 0, 1, 2 ]", typeof(DekiScriptList));
        }

        [Test]
        public void MapForeach_parse() {
            _dekiScriptTester.TestAST("{ (x) : 2*x foreach var x in num.series(1, 10) }", "{ (x) : 2 * x foreach var x in num.series(1, 10) }");
        }

        [Test]
        public void MapForeach_eval() {
            _dekiScriptTester.Test("{ (x) : 2*x foreach var x in num.series(1, 3) }", "{ 1 : 2, 2 : 4, 3 : 6 }", typeof(DekiScriptMap));
        }

        [Test]
        public void ListForeach_primes() {
            _dekiScriptTester.Test("var noprimes = [ j foreach var i in num.series(2, 8), var j in num.series(i * 2, 50, i) ]; [ x foreach var x in num.series(2, 50) where !list.contains(noprimes, x) ];", "[ 2, 3, 5, 7, 11, 13, 17, 19, 23, 29, 31, 37, 41, 43, 47 ]", typeof(DekiScriptList));
        }

        [Test]
        public void IsNot_parse() {
            _dekiScriptTester.TestAST("a is not list", "!(a is list)");
        }

        [Test]
        public void IsNot_eval() {
            _dekiScriptTester.Test("123 is not list", "true", typeof(DekiScriptBool));
        }

        [Test]
        public void In_parse() {
            _dekiScriptTester.TestAST("2 in [ 1, 2, 3 ]", "2 in [ 1, 2, 3 ]");
            _dekiScriptTester.TestAST("4 not in [ 1, 2, 3 ]", "!(4 in [ 1, 2, 3 ])");
        }

        [Test]
        public void In_eval() {
            _dekiScriptTester.Test("2 in [ 1, 2, 3 ]", "true", typeof(DekiScriptBool));
            _dekiScriptTester.Test("4 not in [ 1, 2, 3 ]", "true", typeof(DekiScriptBool));
            _dekiScriptTester.Test("2 in { a:1, b:2, c:3 }", "true", typeof(DekiScriptBool));
            _dekiScriptTester.Test("4 not in { a:1, b:2, c:3 }", "true", typeof(DekiScriptBool));
        }

        [Test]
        public void RawStrings_empty() {
            _dekiScriptTester.Test(@"''''''", "", typeof(DekiScriptString));
        }

        [Test]
        public void RawStrings_simple() {
            _dekiScriptTester.Test(@"'''test'''", "test", typeof(DekiScriptString));
        }

        [Test]
        public void RawStrings_with_quote() {
            _dekiScriptTester.Test(@"'''abc'xyz'''", "abc'xyz", typeof(DekiScriptString));
        }

        [Test]
        public void RawStrings_with_double_quote() {
            _dekiScriptTester.Test(@"'''abc""xyz'''", "abc\"xyz", typeof(DekiScriptString));
        }

        [Test]
        public void RawStrings_with_quotes() {
            _dekiScriptTester.Test(@"'''abc''xyz'''", "abc''xyz", typeof(DekiScriptString));
        }

        [Test, Ignore]
        public void RawStrings_multiple() {
            _dekiScriptTester.Test(@"'''abc''' .. '''xyz'''", "abcxyz", typeof(DekiScriptString));
        }

        [Test]
        public void BodyTargeting1() {
            _dekiScriptTester.Test(@"<html><body> 'first' </body></html>", @"<html><body>first</body></html>", typeof(DekiScriptXml));
        }

        [Test]
        public void BodyTargeting2() {
            _dekiScriptTester.Test(@"<html><body> 'first' </body></html>; <html><body> 'second' </body></html>;", @"<html><body>firstsecond</body></html>", typeof(DekiScriptXml));
        }

        [Test]
        public void BodyTargeting3() {
            _dekiScriptTester.Test(@"<html><body> 'first' </body></html>; <html><body target=""other""> 'second' </body></html>;", @"<html><body>first</body><body target=""other"">second</body></html>", typeof(DekiScriptXml));
        }

        [Test]
        public void BodyTargeting4() {
            _dekiScriptTester.Test(
                @"<html><body> 'first' </body></html>; <html><body target=""other""> 'second' </body></html>; <html><body target=""other""> 'third' </body></html>;",
                @"<html><body>first</body><body target=""other"">second</body></html>",
                typeof(DekiScriptXml)
            );
        }

        [Test]
        public void BodyTargeting5() {
            _dekiScriptTester.Test(
                @"<html><body> 'first' </body></html>; <html><body target=""other""> 'second' </body></html>; <html><body target=""other"" conflict=""replace""> 'third' </body></html>;",
                @"<html><body>first</body><body target=""other"">third</body></html>",
                typeof(DekiScriptXml)
            );
        }

        [Test]
        public void BodyTargeting6() {
            _dekiScriptTester.Test(
                @"<html><body> 'first' </body></html>; <html><body target=""other""> 'second' </body></html>; <html><body target=""other"" conflict=""append""> 'third' </body></html>;",
                @"<html><body>first</body><body target=""other"">secondthird</body></html>",
                typeof(DekiScriptXml)
            );
        }

        [Test]
        public void MapRemove() {
            _dekiScriptTester.Test(
                @"var x = {""a"": 1,""b"":2}; map.remove(x,""b"");x;",
                @"{ a : 1 }{ a : 1, b : 2 }",
                typeof(DekiScriptString)
            );
        }

        [Test]
        public void MapRemove_is_idempotent() {
            _dekiScriptTester.Test(
                @"var x = {""a"": 1,""b"":2}; var y = map.remove(x,""b"");map.remove(y,""b"");y;x;",
                @"{ a : 1 }{ a : 1 }{ a : 1, b : 2 }",
                typeof(DekiScriptString));
        }

        [Test]
        public void Map_access_of_key_in_variable() {
            _dekiScriptTester.Test(
                @"var k = ""a""; var m = {""a"": ""foo""}; m[k];",
                "foo",
                typeof(DekiScriptString)
            );
        }

        [Test]
        public void ListSort_no_args() {
            _dekiScriptTester.Test(
                @"var x = [ ""four"", ""two"", ""one"", ""three"" ]; x; list.sort(x);",
                @"[ ""four"", ""two"", ""one"", ""three"" ][ ""four"", ""one"", ""three"", ""two"" ]",
                typeof(DekiScriptString)
            );
        }

        [Test]
        public void ListSort_by_key() {
            _dekiScriptTester.Test(
                @"var x = [ {""val"": 2, ""text"": ""two""}, {""val"": 1, ""text"": ""one""},{""val"": 3, ""text"": ""three""}  ]; x; list.sort(x,""val"");",
                @"[ { text : ""two"", val : 2 }, { text : ""one"", val : 1 }, { text : ""three"", val : 3 } ][ { text : ""one"", val : 1 }, { text : ""two"", val : 2 }, { text : ""three"", val : 3 } ]",
                typeof(DekiScriptString)
            );
        }

        [Test]
        public void ListSort_with_list_as_key() {
            _dekiScriptTester.Test(
                @"var x = [ ""four"", ""two"", ""one"", ""three"" ]; x; list.sort(x,[4,2,1,3]);",
                @"[ ""four"", ""two"", ""one"", ""three"" ][ ""one"", ""two"", ""three"", ""four"" ]",
                typeof(DekiScriptString)
            );
        }

        [Test]
        public void Map_concatenation() {
            _dekiScriptTester.Test(
                "{ a: 1 } .. { b: 2 }",
                "{ a : 1, b : 2 }",
                typeof(DekiScriptMap)
            );
        }

        [Test]
        public void List_concatenation() {
            _dekiScriptTester.Test(
                "[ 1 ] .. [ 2 ]",
                "[ 1, 2 ]",
                typeof(DekiScriptList)
            );
        }

        [Test]
        public void Xml_xpath() {
            _dekiScriptTester.Test(
                @"var x = <doc><node id='1'>'a'</node><node id='2'>'b'</node><node id='3'>'c'</node></doc>; [ y foreach var y in x['node[@id]'] ]",
                @"[ <node id=""1"">""a""</node>, <node id=""2"">""b""</node>, <node id=""3"">""c""</node> ]",
                typeof(DekiScriptList)
            );
        }

        [Test]
        public void Foreach_counter() {
            _dekiScriptTester.Test(
                "var a=[]; foreach(var i in [4,5,6]) let a ..= [ __count ]; a",
                "[ 0, 1, 2 ]",
                typeof(DekiScriptList)
            );
        }

        [Test]
        public void Foreach_sequential_counter() {
            _dekiScriptTester.Test(
                "var a=[]; foreach(var i in [4,5,6]) let a ..= [ __count ]; foreach(var i in [7,8,9]) let a ..= [ __count ]; a",
                "[ 0, 1, 2, 0, 1, 2 ]",
                typeof(DekiScriptList)
            );
        }

        [Test]
        public void Foreach_nested_counter() {
            _dekiScriptTester.Test(
                @"var a=[]; foreach(var i in [4,5,6]) { let a ..= [ 'a' .. __count ]; foreach(var i in [7,8]) let a ..= [ 'b' .. __count ]; } a",
                @"[ ""a0"", ""b0"", ""b1"", ""a1"", ""b0"", ""b1"", ""a2"", ""b0"", ""b1"" ]",
                typeof(DekiScriptList)
            );
        }

        [Test]
        public void Foreach_condition_on_counter() {
            _dekiScriptTester.Test(
                @"var a=[]; foreach(var i in [1,2,3,4,5,6] where __count < 3) let a ..= [ __count ]; a",
                @"[ 0, 1, 2 ]",
                typeof(DekiScriptList)
            );
        }

        [Test]
        public void Can_timeout_execution() {
            _dekiScriptTester.Runtime.SetTimeout(1.Seconds());
            _dekiScriptTester.Runtime.RegisterFunction("waste.time", GetType().GetMethod("WasteTime"), new DekiScriptNativeInvocationTarget.Parameter[0]);
            try {
                _dekiScriptTester.Test(
                    "foreach(var x in [1,2,3,4,5,6,7,8,9,10]) { waste.time(); };",
                    "..........",
                    typeof(DekiScriptString),
                    false
                    );
                Assert.Fail("didn't time out");
            }catch(TimeoutException) {
                
            }
        }

        public static string WasteTime() {
            Thread.Sleep(500);
            return ".";
        }
    }
}

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
using MindTouch.Deki.Script.Compiler;
using MindTouch.Deki.Script.Expr;
using NUnit.Framework;

namespace MindTouch.Deki.Script.Tests.ScriptTests {

    [TestFixture]
    public class ExpressionSerializationTests {
        [Test]
        public void Switch_without_cases() {
            Test("switch(2) { }");
        }

        [Test]
        public void Switch_with_single_statement_cases() {
            Test("switch(2) { case 1: \"a\"; case 2: \"b\"; }");
        }

        [Test]
        public void Switch_with_multi_statement_cases() {
            Test("switch(2) { case 1: (\"a\"; 1); case 2: (\"b\"; 2); }");
        }

        [Test]
        public void Switch_with_block_statement_cases() {
            Test("switch(2) { case 1: (\"a\"; 1); case 2: (\"b\"; 2); }");
        }

        [Test]
        public void Switch_with_case_fallthrough() {
            Test("switch(2) { case 1: case 2: (\"b\"; 2); }");
        }

        [Test]
        public void Switch_with_default() {
            Test("switch(10) { case 1: 10; default: 15; case 2: 20; }");
        }

        [Test]
        public void Single_variable_definition() {
            Test("var x = 5");
        }

        [Test]
        public void Single_variable_definition_without_assigment() {
            Test("var x");
        }

        [Test]
        public void Multiple_variable_definition() {
            Test("var x = 5, y = 10", "(var x = 5; var y = 10)");
        }

        [Test]
        public void Multiple_variable_definition_without_assignment() {
            Test("var x, y", "(var x; var y)");
        }

        [Test]
        public void Multiple_variable_definition_with_some_assignment() {
            Test("var x, y = 5, z", "(var x; var y = 5; var z)");
        }

        [Test]
        public void List_assignment()
        {
            Test("var x = [ 1, 2 ]");
        }

        [Test]
        public void Break_flow_control_keyword() {
            Test("break");
        }

        [Test]
        public void Continue_flow_control_keyword() {
            Test("continue");
        }

        private void Test(string expression, string deserialized) {
            DekiScriptExpression expr = DekiScriptParser.Parse(Location.Start, expression);
            Assert.AreEqual(deserialized, expr.ToString());
        }

        private void Test(string expression) {
            DekiScriptExpression expr = DekiScriptParser.Parse(Location.Start, expression);
            Assert.AreEqual(expression, expr.ToString());
        }
    }
}
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
using MindTouch.Deki.Script.Runtime;
using NUnit.Framework;

namespace MindTouch.Deki.Script.Tests.Util {
    public class DekiScriptTester {

        //--- Class Fields ---
        public static readonly DekiScriptTester Default = new DekiScriptTester();

        //--- Fields ---
        public readonly DekiScriptTestRuntime Runtime = new DekiScriptTestRuntime();

        //--- Methods ---
        public DekiScriptExpression Parse(string code) {
            return DekiScriptParser.Parse(Location.Start, code);
        }

        public void Test(string expression, string resultValue, Type expectedType) {
            Test(expression, resultValue, expectedType, false);
        }

        public void Test(string expression, string resultValue, Type expectedType, bool safe) {
            var expr = Parse(expression);
            Test(expr, resultValue, expectedType, safe);
        }

        public void Test(DekiScriptExpression expr, string resultValue, Type expectedType, bool safe) {
            var env = Runtime.CreateEnv();
            env.Vars.Add(DekiScriptEnv.SAFEMODE, DekiScriptExpression.Constant(safe));
            DekiScriptExpression result = Runtime.Evaluate(expr, safe ? DekiScriptEvalMode.EvaluateSafeMode : DekiScriptEvalMode.Evaluate, env);
            Assert.IsAssignableFrom(expectedType, result);
            string value;
            if(result is DekiScriptString) {
                value = ((DekiScriptString)result).Value;
            } else if(result is DekiScriptXml) {
                value = ((DekiScriptXml)result).Value.ToString();
            } else {
                value = result.ToString();
            }
            Assert.AreEqual(resultValue, value);
        }

        public void TestAST(string expression, string ast) {
            var expr = Parse(expression);
            Assert.AreEqual(ast, expr.ToString());
        }

        public void TestPartialEval(string expression, string ast) {
            var expr = Parse(expression);
            expr = Runtime.Optimize(expr, DekiScriptEvalMode.Evaluate, Runtime.CreateEnv());
            Assert.AreEqual(ast, expr.ToString(), expression);
        }
    }
}
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
using MindTouch.Dream;
using MindTouch.Dream.Test;
using MindTouch.Xml;
using NUnit.Framework;

namespace MindTouch.Deki.Script.Tests.Util {

    internal class TestScriptService : ITestScriptService {

        //--- Fields ---
        private readonly DreamHostInfo _hostinfo;
        private readonly DekiScriptTester _tester = new DekiScriptTester();
        private readonly XDoc _manifest;
        private readonly XUri _manifestUri = new XUri("mock://manifest/" + StringUtil.CreateAlphaNumericKey(8));

        //--- Constructors ---
        public TestScriptService(DreamHostInfo hostinfo) {
            _hostinfo = hostinfo;
            MockPlug.Register(_manifestUri, (p, v, u, r, r2) => r2.Return(DreamMessage.Ok(_manifest)));
            _manifest = new XDoc("extension")
                .Elem("title", "Function Test Extension")
                .Elem("label", "test")
                .Elem("namespace", "test")
                .Start("requires").Attr("host", "MindTouch Core 10.0").End();
        }

        //--- Methods ---
        public IFunctionDefinition AddFunction(string name) {
            return new FunctionDefinition(this, name);
        }

        public ITestScriptService AddFunctionAsXml(string function) {
            var functionDocument = XDocFactory.From(function, MimeType.TEXT_XML);
            Assert.IsFalse(functionDocument.IsEmpty, string.Format("unable to parse function: {0}", function));
            return AddFunctionAsXml(functionDocument);
        }

        public ITestScriptService AddFunctionAsXml(XDoc functionDocument) {
            _manifest.AddAll(functionDocument);
            return this;
        }

        public void Add(FunctionDefinition function) {
            function.ToXml(_manifest);
        }

        public void Execute(ExecutionPlan plan) {
            var service = DreamTestHelper.CreateService(
                _hostinfo,
                "sid://mindtouch.com/2007/12/dekiscript",
                "dekiscript",
                new XDoc("config").Elem("manifest", _manifestUri)
            );
            foreach(var functionName in _manifest["function/name"]) {
                var name = functionName.AsText;
                _tester.Runtime.RegisterFunction(name, service.AtLocalHost.At(name));
            }
            try {
                var expr = _tester.Parse(plan.Expr);
                var env = _tester.Runtime.CreateEnv();
                env.Vars.Add(DekiScriptEnv.SAFEMODE, DekiScriptExpression.Constant(plan.Safe));
                DekiScriptExpression result = _tester.Runtime.Evaluate(expr, plan.Safe ? DekiScriptEvalMode.EvaluateSafeMode : DekiScriptEvalMode.Evaluate, env);
                if(plan.TypedVerification != null) {
                    Assert.AreEqual(plan.ExpectedType,result.GetType());
                    plan.TypedVerification(result);
                } else if(plan.DocVerification != null) {
                    if(!(result is DekiScriptXml)) {
                        Assert.Fail(string.Format("return type was '{0}' not DekiScriptXml", result.GetType()));
                    }
                    var doc = ((DekiScriptXml)result).Value;
                    plan.DocVerification(doc);
                } else if(plan.StringVerification != null) {
                    string value;
                    if(result is DekiScriptString) {
                        value = ((DekiScriptString)result).Value;
                    } else if(result is DekiScriptXml) {
                        value = ((DekiScriptXml)result).Value.ToString();
                    } else {
                        value = result.ToString();
                    }
                    plan.StringVerification(value, result.GetType());
                } else {
                    Assert.Fail("Execution completed without exception");
                }
            } catch(Exception e) {
                if(plan.ExceptionVerification != null) {
                    plan.ExceptionVerification(e);
                } else {
                    throw;
                }
            } finally {
                service.WithPrivateKey().AtLocalHost.Delete();
            }
        }

        public IExecutionPlan Execute(string expr) {
            return new ExecutionPlan(this, expr);
        }
    }
}

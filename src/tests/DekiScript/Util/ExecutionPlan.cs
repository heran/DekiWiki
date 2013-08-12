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
using MindTouch.Xml;
using NUnit.Framework;

namespace MindTouch.Deki.Script.Tests.Util {
    internal class ExecutionPlan : IExecutionPlan {

        //--- Fields ---
        public readonly string Expr;
        public Action<string,Type> StringVerification;
        public Action<XDoc> DocVerification;
        public Action<object> TypedVerification;
        public Type ExpectedType;
        public bool Safe;

        private readonly TestScriptService _scriptService;
        public Action<Exception> ExceptionVerification;

        //--- Constructors ---
        public ExecutionPlan(TestScriptService scriptService, string expr) {
            _scriptService = scriptService;
            Expr = expr;
        }

        //--- Methods ---
        public IExecutionPlan UsingSafeMode() {
            Safe = true;
            return this;
        }

        public void Verify<T>(Action<T> expectation) {
            ExpectedType = typeof(T);
            TypedVerification = o => {
                var v = (T)o;
                expectation(v);
            };
            Verify();
        }

        public void VerifyXml(Action<XDoc> expectation) {
            DocVerification = expectation;
            Verify();
        }

        public void Verify(Action<string, Type> expectation) {
            StringVerification = expectation;
            Verify();
        }

        public void Verify(Action<string> expectation) {
            StringVerification = (doc, type) => expectation(doc);
            Verify();
        }

        public void Verify(string expectedValue, Type expectedType) {
            StringVerification = (value, type) => {
                Assert.AreEqual(expectedType, type);
                Assert.AreEqual(expectedValue, value);
            };
            Verify();
        }

        public void VerifyException(Type expectedException) {
            ExceptionVerification = e => Assert.AreEqual(expectedException, e.GetType());
            Verify();
        }

        public void VerifyException(Action<Exception> expectation) {
            ExceptionVerification = expectation;
            Verify();
        }

        private void Verify() {
            _scriptService.Execute(this);
        }
    }
}
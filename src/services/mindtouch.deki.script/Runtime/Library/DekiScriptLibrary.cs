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

using System;
using MindTouch.Deki.Script.Expr;
using MindTouch.Dream;
using MindTouch.Tasking;
using MindTouch.Xml;

namespace MindTouch.Deki.Script.Runtime.Library {
    public static partial class DekiScriptLibrary {

        //--- Class Constructor ---
        static DekiScriptLibrary() {

            // initialize safe XHTML validator
            string rules = Plug.New("resource://mindtouch.deki.script/MindTouch.Deki.Script.Resources.xhtml-safe.txt").Get().AsText();
            _xhtmlSafeCop = new XDocCop(rules.Split('\n', '\r'));

            // initialize complete XHTML validator
            rules = Plug.New("resource://mindtouch.deki.script/MindTouch.Deki.Script.Resources.xhtml-unsafe.txt").Get().AsText();
            _xhtmlCompleteCop = new XDocCop(rules.Split('\n', '\r'));
        }

        //--- Class Methods ---
        public static DekiScriptMap MakeErrorObject(Exception e, DekiScriptEnv env) {
            DekiScriptMap exception = new DekiScriptMap();

            // add call stack if one is available
            DekiScriptList callstack = null;
            if(env != null) {
                DekiScriptLiteral callstackVar;
                if(env.Vars.TryGetValue(DekiScriptEnv.CALLSTACK, out callstackVar)) {
                    callstack = callstackVar as DekiScriptList;
                }
            }
            while(e is DekiScriptInvokeException) {
                var ex = (DekiScriptInvokeException)e;
                if(callstack == null) {
                    callstack = new DekiScriptList();
                }
                callstack.AddNativeValue(ex.FunctionName);
                e = ex.InnerException;
            }
            if(callstack != null) {
                exception.Add("callstack", callstack);
            }

            // add exception text
            exception.Add("message", DekiScriptExpression.Constant(e.Message));
            exception.Add("stacktrace", DekiScriptExpression.Constant(e.GetCoroutineStackTrace()));
            if(e.InnerException != null) {
                exception.Add("inner", MakeErrorObject(e.InnerException, null));
            }
            if(e is DekiScriptException) {
                exception.Add("source", DekiScriptExpression.Constant(((DekiScriptException)e).Location.Origin));
            }
            return exception;
        }

        private static object Eval(object value, DekiScriptRuntime runtime) {
            if(value is XUri) {
                DekiScriptLiteral uri = DekiScriptExpression.Constant((XUri)value);
                if(DekiScriptRuntime.IsProperty(uri)) {
                    value = runtime.EvaluateProperty(Location.None, uri, runtime.CreateEnv()).NativeValue;
                }
            }
            return value;
        }
    }
}

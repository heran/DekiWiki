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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using log4net;
using MindTouch.Deki.Script.Compiler;
using MindTouch.Deki.Script.Expr;
using MindTouch.Deki.Script.Runtime;
using MindTouch.Deki.Script.Runtime.Library;
using MindTouch.Deki.Script.Runtime.TargetInvocation;
using MindTouch.Dream;
using MindTouch.Tasking;
using MindTouch.Xml;

namespace MindTouch.Deki.Script {
    public class DekiScriptRuntime {

        //--- Constants ---
        public const string DEFAULT_ID = "$";
        public const string ARGS_ID = "args";
        public const string ENV_ID = "__env";
        public const string COUNT_ID = "__count";
        public const string INDEX_ID = "__index";
        public const string ON_SAVE_PATTERN = "save:";
        public const string ON_SUBST_PATTERN = "subst:";
        public const string ON_EDIT_PATTERN = "edit:";
        public const int MAX_OUTPUT_SIZE = 100000;

        //--- Class Fields ---
        private static readonly ILog _log = LogUtils.CreateLog();
        private static readonly DekiScriptList _emptyList = new DekiScriptList();
        private static readonly Dictionary<XUri, DekiScriptInvocationTargetDescriptor> _commonFunctions;
        private static readonly DekiScriptEnv _commonEnv;

        //--- Class Constructor ---
        static DekiScriptRuntime() {

            // register built-in functions
            _commonFunctions = new Dictionary<XUri, DekiScriptInvocationTargetDescriptor>();
            foreach(MethodInfo method in typeof(DekiScriptLibrary).GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy)) {

                // check if it has the DekiScriptFunction attribute
                DekiScriptFunctionAttribute functionAttribute = (DekiScriptFunctionAttribute)Attribute.GetCustomAttribute(method, typeof(DekiScriptFunctionAttribute));
                if(functionAttribute != null) {
                    var parameters = from param in method.GetParameters()
                                     let attr = (DekiScriptParamAttribute[])param.GetCustomAttributes(typeof(DekiScriptParamAttribute), false)
                                     select ((attr != null) && (attr.Length > 0)) ? new DekiScriptNativeInvocationTarget.Parameter(attr[0].Hint, attr[0].Optional) : null;
                    var target = new DekiScriptNativeInvocationTarget(null, method, parameters.ToArray());
                    var function = new DekiScriptInvocationTargetDescriptor(target.Access, functionAttribute.IsProperty, functionAttribute.IsIdempotent, functionAttribute.Name ?? method.Name, target.Parameters, target.ReturnType, functionAttribute.Description, functionAttribute.Transform, target);
                    _commonFunctions[new XUri("native:///").At(function.SystemName)] = function;
                }
            }

            // build common env
            DekiScriptMap common = new DekiScriptMap();

            // add global constants
            common.AddNativeValueAt("num.e", Math.E);
            common.AddNativeValueAt("num.pi", Math.PI);
            common.AddNativeValueAt("num.epsilon", double.Epsilon);
            common.AddNativeValueAt("num.positiveinfinity", double.PositiveInfinity);
            common.AddNativeValueAt("num.negativeinfinity", double.NegativeInfinity);
            common.AddNativeValueAt("num.nan", double.NaN);

            // add global functions & properties
            foreach(var function in _commonFunctions) {
                common.AddNativeValueAt(function.Value.Name, function.Key);
            }
            _commonEnv = new DekiScriptEnv(common);
        }

        //--- Class Methods ---
        public static bool IsProperty(DekiScriptLiteral value) {
            if(value.ScriptType == DekiScriptType.URI) {
                return (((DekiScriptUri)value).Value.LastSegment ?? string.Empty).StartsWithInvariant("$");
            }
            return false;
        }

        public static Exception UnwrapAsyncException(XUri uri, Exception exception) {

            // unroll target and async invocation exceptions
            while(((exception is TargetInvocationException)) && (exception.InnerException != null)) {
                exception = exception.InnerException;
            }
            return exception;
        }

        internal static XDoc CreateWarningFromException(DekiScriptList callstack, Location location, Exception exception) {

            // unwrap nested async exception
            exception = UnwrapAsyncException(null, exception);

            // determine exception
            XDoc result;
            if(exception is DreamAbortException) {
                DreamAbortException e = (DreamAbortException)exception;
                if(e.Response.ContentType.IsXml) {
                    result = CreateWarningElement(callstack, string.Format("{1}, a web exception was thrown (status: {0})", (int)e.Response.Status, location), new XMessage(e.Response).ToPrettyString());
                } else {
                    result = CreateWarningElement(callstack, string.Format("{1}, a web exception was thrown (status: {0})", (int)e.Response.Status, location), e.Response.AsText());
                }
            } else if(exception is DekiScriptException) {
                result = CreateWarningElement(callstack, exception.Message, exception.GetCoroutineStackTrace());
            } else {
                result = CreateWarningElement(callstack, string.Format("{0}: {1}", exception.Message, location), exception.GetCoroutineStackTrace());
            }
            return result;
        }

        public static XDoc CreateWarningElement(DekiScriptList callstack, String description, string message) {
            if(callstack != null) {
                StringBuilder stacktrace = new StringBuilder();
                foreach(var entry in from item in callstack.Value let entry = item.AsString() where entry != null select entry) {
                    stacktrace.AppendFormat("    at {0}\n", entry);
                }
                if(stacktrace.Length > 0) {
                    if(message == null) {
                        message = "Callstack:\n" + stacktrace;
                    } else {
                        message = "Callstack:\n" + stacktrace + "\n" + message;
                    }
                }
            }

            XDoc result = new XDoc("div");
            result.Start("span").Attr("class", "warning").Value(description).End();
            if(message != null) {
                string id = StringUtil.CreateAlphaNumericKey(8);
                result.Value(" ");
                result.Start("span").Attr("style", "cursor: pointer;").Attr("onclick", string.Format("$('#{0}').toggle()", id)).Value("(click for details)").End();
                result.Start("pre").Attr("id", id).Attr("style", "display: none;").Value(message).End();
            }
            return result;
        }

        //--- Fields ---
        private readonly Dictionary<XUri, DekiScriptInvocationTargetDescriptor> _functions;

        //--- Constructors ---
        public DekiScriptRuntime() {
            _functions = new Dictionary<XUri, DekiScriptInvocationTargetDescriptor>(_commonFunctions);
        }

        //--- Properties ---
        public Dictionary<XUri, DekiScriptInvocationTargetDescriptor> Functions { get { return _functions; } }
        protected virtual TimeSpan EvaluationTimeout { get { return TimeSpan.MaxValue; } }
        protected virtual ILog Log { get { return _log; } }

        //--- Methdos ---
        public virtual void RegisterExtensionFunctions(Dictionary<XUri, DekiScriptInvocationTargetDescriptor> functions) {
            lock(_functions) {
                foreach(var entry in functions) {
                    _functions[entry.Key] = entry.Value;
                }
            }
        }

        public virtual DekiScriptInvocationTargetDescriptor ResolveRegisteredFunctionUri(XUri uri) {
            DekiScriptInvocationTargetDescriptor descriptor;

            // check registered functions
            if(_functions.TryGetValue(uri, out descriptor)) {
                return descriptor;
            }
            return null;
        }

        public virtual DekiScriptEnv CreateEnv() {
            return _commonEnv.NewScope();
        }

        public virtual DekiScriptLiteral Evaluate(DekiScriptExpression expr, DekiScriptEvalMode mode, DekiScriptEnv env) {
            DekiScriptExpressionEvaluationState state = new DekiScriptExpressionEvaluationState(mode, env, this, EvaluationTimeout, GetMaxOutputSize(mode));
            try {
                return state.Pop(expr.VisitWith(DekiScriptExpressionEvaluation.Instance, state));
            } catch(DekiScriptReturnException e) {
                state.Push(e.Value);
                return state.PopAll();
            }
        }

        public virtual DekiScriptLiteral EvaluateProperty(Location location, DekiScriptLiteral value, DekiScriptEnv env) {
            if(IsProperty(value)) {
                DekiScriptUri uri = (DekiScriptUri)value;
                try {
                    value = Invoke(location, uri.Value, uri.Arguments.IsNil ? _emptyList : uri.Arguments, env);
                } catch(DekiScriptFatalException) {
                    throw;
                } catch(Exception e) {
                    var descriptor = ResolveRegisteredFunctionUri(uri.Value);
                    throw new DekiScriptInvokeException(location, uri.Value, (descriptor != null) ? descriptor.Name : uri.Value.ToString(), e);
                }
            }
            return value;
        }

        public virtual DekiScriptLiteral Invoke(Location location, XUri uri, DekiScriptLiteral args, DekiScriptEnv env) {
            var sw = Stopwatch.StartNew();
            DekiScriptInvocationTargetDescriptor descriptor;
            var target = _functions.TryGetValue(uri, out descriptor) ? descriptor.Target : FindTarget(uri);
            try {

                // invoke function directly
                return target.Invoke(this, args);
            } catch(Exception e) {
                throw UnwrapAsyncException(uri, e).Rethrow();
            } finally {
                sw.Stop();
                bool property = (uri.LastSegment ?? string.Empty).StartsWithInvariant("$");
                env.AddFunctionProfile(location, (descriptor != null) ? (property ? "$" : "") + descriptor.Name : uri.ToString(), sw.Elapsed);
            }
        }

        public virtual Plug PreparePlug(Plug plug) {
            return plug;
        }

        public virtual DekiScriptLiteral ResolveMissingName(string name) {
            return null;
        }

        public virtual void LogExceptionInOutput(DekiScriptLiteral error) {
            Log.Info("exception in dekiscript output: " +  DekiScriptLibrary.JsonFormat(error.NativeValue));
        }

        public virtual int GetMaxOutputSize(DekiScriptEvalMode mode) {
            return MAX_OUTPUT_SIZE;
        }

        protected virtual IDekiScriptInvocationTarget FindTarget(XUri uri) {
            string last_segment = uri.LastSegment ?? string.Empty;
            if(last_segment.EndsWithInvariant(".rpc")) {
                string methodName = last_segment.Substring(0, last_segment.Length - 4);
                return new DekiScriptXmlRpcInvocationTarget(uri.WithoutLastSegment(), methodName);
            }
            if(last_segment.EndsWithInvariant(".jsp")) {
                return new DekiScriptHttpGetInvocationTarget(uri);
            }
            return new DekiScriptRemoteInvocationTarget(uri);
        }
    }
}

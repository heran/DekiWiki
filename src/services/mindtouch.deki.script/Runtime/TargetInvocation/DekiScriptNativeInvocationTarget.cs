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
using System.Collections.Generic;
using System.Reflection;
using MindTouch.Deki.Script.Expr;
using MindTouch.Dream;
using MindTouch.Extensions;
using MindTouch.Tasking;
using MindTouch.Xml;

namespace MindTouch.Deki.Script.Runtime.TargetInvocation {
    public class DekiScriptNativeInvocationTarget : ADekiScriptInvocationTarget {

        //--- Types ---
        public class Parameter {

            //--- Class Fields ---
            public static readonly Parameter Default = new Parameter(null, false);

            //--- Fields ---
            public readonly string Hint;
            public readonly bool Optional;

            //--- Constructor ---
            public Parameter(string hint, bool optional) {
                this.Hint = hint;
                this.Optional = optional;
            }
        }

        //--- Fields ---
        private readonly Func<DekiScriptRuntime, object[], object> _invoke;

        //--- Constructors ---
        public DekiScriptNativeInvocationTarget(object subject, MethodInfo method, Parameter[] parameters) {
            this.Method = method;

            // check if method is a coroutine
            Type nativeReturnType;
            ConstructorInfo resultTypeConstructor = null;
            ParameterInfo[] methodParams = method.GetParameters();
            bool isCoroutine = (method.ReturnType == typeof(IEnumerator<IYield>));
            if(isCoroutine) {

                // check if enough parameters are present
                if(methodParams.Length == 0) {
                    throw new ArgumentException("handler is missing Result<T> parameter");
                }

                // check that the last parameter is of type Result<T>
                Type lastParam = methodParams[methodParams.Length - 1].ParameterType;
                if(!lastParam.IsGenericType || (lastParam.GetGenericTypeDefinition() != typeof(Result<>))) {
                    throw new ArgumentException(string.Format("handler last parameter must be generic type Result<T>, but is {0}", lastParam.FullName));
                }
                resultTypeConstructor = lastParam.GetConstructor(Type.EmptyTypes);
                nativeReturnType = lastParam.GetGenericArguments()[0];

                // remove last parameter from array since it represents the return type
                methodParams = ArrayUtil.Resize(methodParams, methodParams.Length - 1);
            } else {
                nativeReturnType = method.ReturnType;
            }
            ReturnType = DekiScriptLiteral.AsScriptType(nativeReturnType);

            // check if first parameter is a DekiScriptRuntime
            bool usesRuntime = false;
            if((methodParams.Length > 0) && (methodParams[methodParams.Length - 1].ParameterType.IsA<DekiScriptRuntime>())) {
                usesRuntime = true;
                methodParams = ArrayUtil.Resize(methodParams, methodParams.Length - 1);
            }

            // retrieve method parameters and their attributes
            Parameters = new DekiScriptParameter[methodParams.Length];
            for(int i = 0; i < methodParams.Length; ++i) {
                ParameterInfo param = methodParams[i];
                Parameter details = parameters[i] ?? Parameter.Default;

                // add hint parameter
                Parameters[i] = new DekiScriptParameter(param.Name, DekiScriptLiteral.AsScriptType(param.ParameterType), details.Optional, details.Hint, param.ParameterType, DekiScriptNil.Value);
            }

            // determine access rights
            if(method.IsPrivate || method.IsFamily) {
                this.Access = DreamAccess.Private;
            } else if(method.IsAssembly) {
                this.Access = DreamAccess.Internal;
            } else {
                this.Access = DreamAccess.Public;
            }

            // create invocation callback
            if(resultTypeConstructor != null) {
                if(usesRuntime) {

                    // invoke coroutine with runtime
                    _invoke = (runtime, args) => {
                        var arguments = new object[args.Length + 2];
                        AResult result = (AResult)resultTypeConstructor.Invoke(null);
                        arguments[arguments.Length - 1] = result;
                        arguments[arguments.Length - 2] = runtime;
                        Array.Copy(args, arguments, args.Length);
                        new Coroutine(method, result).Invoke(() => (IEnumerator<IYield>)method.InvokeWithRethrow(subject, arguments));
                        result.Block();
                        return result.UntypedValue;
                    };
                } else {

                    // invoke coroutine without runtime
                    _invoke = (runtime, args) => {
                        var arguments = new object[args.Length + 1];
                        AResult result = (AResult)resultTypeConstructor.Invoke(null);
                        arguments[arguments.Length - 1] = result;
                        Array.Copy(args, arguments, args.Length);
                        new Coroutine(method, result).Invoke(() => (IEnumerator<IYield>)method.InvokeWithRethrow(subject, arguments));
                        result.Block();
                        return result.UntypedValue;
                    };
                }
            } else {
                if(usesRuntime) {

                    // invoke method with runtime
                    _invoke = (runtime, args) => {
                        var arguments = new object[args.Length + 1];
                        arguments[arguments.Length - 1] = runtime;
                        Array.Copy(args, arguments, args.Length);
                        return method.InvokeWithRethrow(subject, arguments);
                    };
                } else {

                    // invoke method without runtime
                    _invoke = (runtime, args) => method.InvokeWithRethrow(subject, args);
                }
            }
        }

        //--- Properties ---
        public DreamAccess Access { get; private set; }
        public DekiScriptParameter[] Parameters { get; private set; }
        public MethodInfo Method { get; private set; }
        public DekiScriptType ReturnType { get; private set; }

        //--- Methods ---
        public override DekiScriptLiteral InvokeList(DekiScriptRuntime runtime, DekiScriptList args) {
            return InvokeHelper(runtime, DekiScriptParameter.ValidateToList(Parameters, args));
        }

        public override DekiScriptLiteral InvokeMap(DekiScriptRuntime runtime, DekiScriptMap args) {
            return InvokeHelper(runtime, DekiScriptParameter.ValidateToList(Parameters, args));
        }

        private DekiScriptLiteral InvokeHelper(DekiScriptRuntime runtime, DekiScriptList args) {

            // convert passed in arguments
            object[] arguments = new object[Parameters.Length];
            int i = 0;
            try {
                for(; i < Parameters.Length; ++i) {
                    var value = args[i].NativeValue;

                    // check if we need to convert the value
                    if((value != null) && (Parameters[i].NativeType != typeof(object))) {

                        // check for the special case where we cast from XML to STR
                        if((value is XDoc) && (Parameters[i].NativeType == typeof(string))) {
                            XDoc xml = (XDoc)value;
                            if(xml.HasName("html")) {
                                value = xml["body[not(@target)]"].Contents;
                            } else {
                                value = xml.ToString();
                            }
                        } else {

                            // rely on the default type conversion rules
                            value = SysUtil.ChangeType(value, Parameters[i].NativeType);
                        }
                    }
                    arguments[i] = value;
                }
            } catch {
                throw new ArgumentException(string.Format("could not convert parameter '{0}' (index {1}) from {2} to {3}", Parameters[i].Name, i, args[i].ScriptTypeName, DekiScriptLiteral.AsScriptTypeName(Parameters[i].ScriptType)));
            }

            // invoke method
            var result = _invoke(runtime, arguments);

            // check if result is a URI
            if(result is XUri) {

                // normalize URI if possible
                DreamContext context = DreamContext.CurrentOrNull;
                if(context != null) {
                    result = context.AsPublicUri((XUri)result);
                }
            }
            var literal = DekiScriptLiteral.FromNativeValue(result);
            try {
                return literal.Convert(ReturnType);
            } catch(DekiScriptInvalidCastException) {
                throw new DekiScriptInvalidReturnCastException(Location.None, literal.ScriptType, ReturnType);
            }
        }
    }
}
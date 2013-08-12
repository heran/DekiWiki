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

namespace MindTouch.Deki.Script.Runtime.TargetInvocation {
    public class DekiScriptExpressionInvocationTarget : ADekiScriptInvocationTarget {

        //--- Fields ---
        private readonly DekiScriptEnv _env;

        //--- Constructors ---
        public DekiScriptExpressionInvocationTarget(DreamAccess access, DekiScriptParameter[] parameters, DekiScriptExpression expr) : this(access, parameters, expr, null) { }

        public DekiScriptExpressionInvocationTarget(DreamAccess access, DekiScriptParameter[] parameters, DekiScriptExpression expr, DekiScriptEnv env) {
            if(parameters == null) {
                throw new ArgumentNullException("parameters");
            }
            if(expr == null) {
                throw new ArgumentNullException("expr");
            }
            this.Access = access;
            this.Parameters = parameters;
            this.Expression = expr;
            _env = env;
        }

        //--- Properties ---
        public DreamAccess Access { get; private set; }
        public DekiScriptParameter[] Parameters { get; private set; }
        public DekiScriptExpression Expression { get; private set; }

        //--- Methods ---
        public override DekiScriptLiteral InvokeList(DekiScriptRuntime runtime, DekiScriptList args) {
            return InvokeHelper(runtime, DekiScriptParameter.ValidateToMap(Parameters, args));
        }

        public override DekiScriptLiteral InvokeMap(DekiScriptRuntime runtime, DekiScriptMap args) {
            return InvokeHelper(runtime, DekiScriptParameter.ValidateToMap(Parameters, args));
        }

        private DekiScriptLiteral InvokeHelper(DekiScriptRuntime runtime, DekiScriptMap args) {

            // invoke script
            DekiScriptEnv env = (_env != null) ? _env.NewScope() : runtime.CreateEnv();
            env.Vars.Add("args", args);
            env.Vars.Add("$", args);
            return runtime.Evaluate(Expression, DekiScriptEvalMode.Evaluate, env);
        }
    }
}
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
using System.Diagnostics;
using System.Xml;
using MindTouch.Deki.Script.Expr;
using MindTouch.Deki.Script.Runtime;
using MindTouch.Deki.Script.Runtime.Library;
using MindTouch.Xml;

namespace MindTouch.Deki.Script.Compiler {
    internal struct DekiScriptExpressionEvaluationState {

        //--- Types ---
        private class SharedState {

            //--- Fields ---
            public Stopwatch EvaluationTimer;
            public TimeSpan EvaluationTimeout;
            public DekiScriptXml EvaluationError;
            public bool Safe;
        }

        //--- Fields ---
        public readonly DekiScriptEnv Env;
        public readonly XmlNamespaceManager Namespaces;
        public readonly DekiScriptOutputBuffer Buffer;
        public readonly DekiScriptRuntime Runtime;
        private readonly SharedState _sharedState;

        //--- Constructor ---
        public DekiScriptExpressionEvaluationState(DekiScriptEvalMode mode, DekiScriptEnv env, DekiScriptRuntime runtime, TimeSpan evaluationTimeout, int maxOutputBufferSize) {
            this.Env = env;
            this.Namespaces = new XmlNamespaceManager(XDoc.XmlNameTable);
            this.Buffer = new DekiScriptOutputBuffer(maxOutputBufferSize);
            this.Runtime = runtime;
            _sharedState = new SharedState();
            _sharedState.Safe = (mode == DekiScriptEvalMode.EvaluateSafeMode);
            if(evaluationTimeout == TimeSpan.MaxValue) {
                return;
            }
            _sharedState.EvaluationTimeout = evaluationTimeout;
            _sharedState.EvaluationTimer = Stopwatch.StartNew();
        }

        private DekiScriptExpressionEvaluationState(DekiScriptEnv env, DekiScriptRuntime runtime, XmlNamespaceManager namespaces, DekiScriptOutputBuffer buffer, SharedState sharedState) {
            this.Env = env;
            this.Namespaces = namespaces;
            this.Buffer = buffer;
            this.Runtime = runtime;
            _sharedState = sharedState;
        }

        //--- Properties ---
        public bool SafeMode { get { return _sharedState.Safe; } }
        public DekiScriptXml FatalEvaluationError { get { return _sharedState.EvaluationError; } }

        //--- Methods ---
        public DekiScriptExpressionEvaluationState With(DekiScriptEnv env) {
            return new DekiScriptExpressionEvaluationState(env, Runtime, Namespaces, Buffer, _sharedState);
        }

        public DekiScriptOutputBuffer.Range Push(DekiScriptLiteral value) {
            return Buffer.Push(value);
        }

        public DekiScriptLiteral Pop(DekiScriptOutputBuffer.Range range) {
            return Buffer.Pop(range, _sharedState.Safe);
        }

        public DekiScriptLiteral PopAll() {
            return Buffer.Pop(new DekiScriptOutputBuffer.Range(0, Buffer.Marker), _sharedState.Safe);
        }

        public void ThrowIfTimedout() {
            if(_sharedState.EvaluationTimer == null) {
                return;
            }
            if(_sharedState.EvaluationTimer.Elapsed <= _sharedState.EvaluationTimeout) {
                return;
            }
            _sharedState.EvaluationTimer.Stop();
            var e = new TimeoutException("script execution has timed out");
            var error = DekiScriptLibrary.MakeErrorObject(e, Env);
            _sharedState.EvaluationError = new DekiScriptXml(DekiScriptLibrary.WebShowError((Hashtable)error.NativeValue));
            throw e;
        }
    }
}
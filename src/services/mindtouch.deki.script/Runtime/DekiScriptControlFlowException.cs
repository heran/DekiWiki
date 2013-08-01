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

namespace MindTouch.Deki.Script.Runtime {
    public abstract class DekiScriptControlFlowException : DekiScriptException {

        //--- Constructors ---
        protected DekiScriptControlFlowException(Location location, string message) : base(location, message) { }
    }

    public class DekiScriptBreakException : DekiScriptControlFlowException {

        //--- Constructors ---
        public DekiScriptBreakException(Location location) : base(location, "Unhandled 'break' statement") { }
    }

    public class DekiScriptContinueException : DekiScriptControlFlowException {

        //--- Constructors ---
        public DekiScriptContinueException(Location location) : base(location, "Unhandled 'continue' statement") { }
    }

    public class DekiScriptReturnException : DekiScriptControlFlowException {

        //--- Fields ---
        public readonly DekiScriptLiteral Value;

        //--- Constructors ---
        public DekiScriptReturnException(Location location, DekiScriptLiteral value) : base(location, "Unhandled 'return' statement") {
            if(value == null) {
                throw new ArgumentNullException("value");
            }
            this.Value = value;
        }
    }
}
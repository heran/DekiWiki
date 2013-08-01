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

using MindTouch.Tasking;

namespace MindTouch.Deki {
    using Yield = IEnumerator<IYield>;

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    internal class DekiScriptFunctionAttribute : Attribute {

        //--- Fields ---
        public string Name;
        public string Description;
        public string Transform;
        public bool IsProperty;
        public bool IsIdempotent;

        //--- Constructors ---
        internal DekiScriptFunctionAttribute() { }

        internal DekiScriptFunctionAttribute(string name) {
            this.Name = name;
        }

        internal DekiScriptFunctionAttribute(string name, string description) {
            this.Name = name;
            this.Description = description;
        }
    }

    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    internal class DekiScriptParamAttribute : Attribute {

        //--- Fields ---
        public readonly string Hint;
        public readonly bool Optional;

        //--- Constructors ---
        internal DekiScriptParamAttribute(string hint) : this(hint, false) { }

        internal DekiScriptParamAttribute(string hint, bool optional) {
            this.Hint = hint;
            this.Optional = optional;
        }
    }
}

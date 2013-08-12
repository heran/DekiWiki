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

using MindTouch.Deki.Script.Expr;

namespace MindTouch.Deki.Script.Runtime {
    public abstract class DekiScriptFatalException : DekiScriptException {

        //--- Constructors ---
        protected DekiScriptFatalException(Location location) : base(location) { }
        protected DekiScriptFatalException(Location location, string message) : base(location, message) { }
    }

    public class DekiScriptUnsupportedTypeException : DekiScriptFatalException {

        //--- Fields ---
        public readonly string Type;

        //--- Constructors ---
        public DekiScriptUnsupportedTypeException(Location location, string type) : base(location) {
            this.Type = type;
        }
    }

    public class DekiScriptUndefinedNameException : DekiScriptFatalException {

        //--- Fields ---
        public readonly string Name;

        //--- Constructors ---
        public DekiScriptUndefinedNameException(Location location, string name) : base(location) {
            this.Name = name;
        }

        //--- Properties ---
        public override string Message {
            get {
                return string.Format("reference to undefined name '{0}' {1}", Name, base.Message);
            }
        }
    }

    public class DekiScriptDocumentTooLargeException : DekiScriptFatalException {

        //--- Fields ---
        public bool Handled;

        //--- Constructors ---
        public DekiScriptDocumentTooLargeException(int limit) : base(Location.None, string.Format("Document exceeded {0:#,##0} nodes.", limit)) { }
    }
}
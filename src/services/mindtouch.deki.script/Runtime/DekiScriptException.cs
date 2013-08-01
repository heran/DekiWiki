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

namespace MindTouch.Deki.Script.Runtime {
    public abstract class DekiScriptException : Exception {

        //--- Fields ---
        public Location Location;

        //--- Constructors ---
        protected DekiScriptException(Location location) {
            this.Location = location;
        }

        protected DekiScriptException(Location location, string message) : base(message) {
            this.Location = location;
        }

        protected DekiScriptException(Location location, string message, Exception inner) : base(message, inner) {
            this.Location = location;
        }

        //--- Properties ---
        public override string Message {
            get {
                if(Location.HasValue) {
                    return base.Message + ": " + Location;
                }
                return base.Message;
            }
        }
    }

    public class DekiScriptInvokeException : DekiScriptException {

        //--- Class Methods ---
        private static string MakeMessage(string functionName) {
            return string.Format("function '{0}' failed", functionName);
        }

        //--- Fields ---
        public readonly XUri Uri;
        public readonly string Error;
        public readonly string FunctionName;

        //--- Constructors ---
        public DekiScriptInvokeException(Location location, XUri uri, string functionName, string error) : base(location, MakeMessage(functionName)) {
            this.Uri = uri;
            this.Error = error;
            this.FunctionName = functionName;
        }

        public DekiScriptInvokeException(Location location, XUri uri, string functionName, Exception inner) : base(location, MakeMessage(functionName), inner) {
            this.Uri = uri;
            this.FunctionName = functionName;
        }

        //--- Methods ---
        public override string StackTrace {
            get {
                if(!string.IsNullOrEmpty(Error)) {
                    return Error + "\n" + base.StackTrace;
                }
                return base.StackTrace;
            }
        }
    }

    public class DekiScriptBadTypeException : DekiScriptException {

        //--- Class Methods ---
        private static string MakeMessage(DekiScriptType badType, DekiScriptType[] expectedTypes) {
            string[] types = Array.ConvertAll(expectedTypes, type => type.ToString().ToLowerInvariant());
            return string.Format("{0} is not valid; expected {1}", badType.ToString().ToLowerInvariant(), string.Join(" or ", types));
        }

        //--- Fields ---
        public readonly DekiScriptType BadType;
        public readonly DekiScriptType[] ExpectedTypes;

        //--- Constructors ---
        public DekiScriptBadTypeException(Location location, DekiScriptType badType, DekiScriptType[] expectedTypes) : base(location, MakeMessage(badType, expectedTypes)) {
            this.BadType = badType;
            this.ExpectedTypes = expectedTypes;
        }
    }

    public class DekiScriptInvalidCastException : DekiScriptException {

        //--- Class Methods ---
        protected static string MakeMessage(DekiScriptType currentType, DekiScriptType newType) {
            return string.Format("cannot convert from '{0}' to '{1}'", currentType.ToString().ToLowerInvariant(), newType.ToString().ToLowerInvariant());
        }

        //--- Fields ---
        public readonly DekiScriptType CurrentType;
        public readonly DekiScriptType NewType;

        //--- Constructors ---
        public DekiScriptInvalidCastException(Location location, DekiScriptType currentType, DekiScriptType newType) : base(location, MakeMessage(currentType, newType)) {
            this.CurrentType = currentType;
            this.NewType = newType;
        }
    }

    public class DekiScriptInvalidParameterCastException : DekiScriptException {

        //--- Class Methods ---
        protected static string MakeMessage(DekiScriptParameter parameter, DekiScriptType sourceType) {
            return string.Format("parameter '{2}' could not convert from '{0}' to '{1}'", sourceType.ToString().ToLowerInvariant(), parameter.ScriptType.ToString().ToLowerInvariant(), parameter.Name);
        }

        //--- Fields ---
        public readonly DekiScriptParameter Parameter;
        public readonly DekiScriptType SourceType;

        //--- Constructors ---
        public DekiScriptInvalidParameterCastException(Location location, DekiScriptParameter parameter, DekiScriptType sourceType) : base(location, MakeMessage(parameter, sourceType)) {
            this.Parameter = parameter;
            this.SourceType = sourceType;
        }
    }

    public class DekiScriptInvalidReturnCastException : DekiScriptException {

        //--- Class Methods ---
        protected static string MakeMessage(DekiScriptType currentType, DekiScriptType newType) {
            return string.Format("return value could not convert from '{0}' to '{1}'", currentType.ToString().ToLowerInvariant(), newType.ToString().ToLowerInvariant());
        }

        //--- Fields ---
        public readonly DekiScriptType CurrentType;
        public readonly DekiScriptType NewType;

        //--- Constructors ---
        public DekiScriptInvalidReturnCastException(Location location, DekiScriptType currentType, DekiScriptType newType)
            : base(location, MakeMessage(currentType, newType)) {
            this.CurrentType = currentType;
            this.NewType = newType;
        }
    }

    public class DekiScriptBadReturnTypeException : DekiScriptException {

        //--- Class Methods ---
        private static string MakeMessage(DekiScriptType badType, DekiScriptType[] expectedTypes) {
            string[] types = Array.ConvertAll(expectedTypes, type => type.ToString().ToLowerInvariant());
            return string.Format("{0} is not valid return type; expected {1}", badType.ToString().ToLowerInvariant(), string.Join(" or ", types));
        }

        //--- Fields ---
        public readonly DekiScriptType BadType;
        public readonly DekiScriptType[] ExpectedTypes;

        //--- Constructors ---
        public DekiScriptBadReturnTypeException(Location location, DekiScriptType badType, DekiScriptType[] expectedTypes) : base(location, MakeMessage(badType, expectedTypes)) {
            this.BadType = badType;
            this.ExpectedTypes = expectedTypes;
        }
    }

    public class DekiScriptRemoteException : DekiScriptException {
        
        //--- Constructors ---
        public DekiScriptRemoteException(Location location, string message) : base(location, message) { }
    }
}
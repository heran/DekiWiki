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
using MindTouch.Xml;

namespace MindTouch.Deki.Script.Runtime {
    public class DekiScriptParameter {

        //--- Class Methods ---
        public static DekiScriptList ValidateToList(DekiScriptParameter[] parameters, DekiScriptList args) {
            DekiScriptList result = new DekiScriptList();

            // check passed in arguments
            int i = 0;
            var count = Math.Min(args.Value.Count, parameters.Length);
            for(; i < count; ++i) {
                var value = args[i];
                if(value.IsNil) {
                    if((parameters[i].ScriptType != DekiScriptType.ANY) && !parameters[i].Optional) {
                        throw new ArgumentException(string.Format("missing value for parameter '{0}' (index {1})", parameters[i].Name, i));
                    }

                    // use default value for this parameter
                    result.Add(parameters[i].Default);
                } else {
                    result.Add(parameters[i].Convert(value));
                }
            }

            // check that missing arguments are optional
            for(; i < parameters.Length; ++i) {
                if((parameters[i].ScriptType != DekiScriptType.ANY) && !parameters[i].Optional) {
                    throw new ArgumentException(string.Format("missing value for parameter '{0}' (index {1})", parameters[i].Name, i));
                }
                result.Add(parameters[i].Default);
            }
            return result;
        }

        public static DekiScriptMap ValidateToMap(DekiScriptParameter[] parameters, DekiScriptList args) {
            DekiScriptMap result = new DekiScriptMap();

            // check passed in arguments
            int i = 0;
            var count = Math.Min(args.Value.Count, parameters.Length);
            for(; i < count; ++i) {
                var value = args[i];
                if(value.IsNil) {
                    if((parameters[i].ScriptType != DekiScriptType.ANY) && !parameters[i].Optional) {
                        throw new ArgumentException(string.Format("missing value for parameter '{0}' (index {1})", parameters[i].Name, i));
                    }

                    // set default value for this parameter
                    result.Add(parameters[i].Name, parameters[i].Default);
                } else {
                    result.Add(parameters[i].Name, parameters[i].Convert(value));
                }
            }

            // check that missing arguments are optional
            for(; i < parameters.Length; ++i) {
                if((parameters[i].ScriptType != DekiScriptType.ANY) && !parameters[i].Optional) {
                    throw new ArgumentException(string.Format("missing value for parameter '{0}' (index {1})", parameters[i].Name, i));
                }
                result.Add(parameters[i].Name, parameters[i].Default);
            }
            return result;
        }

        public static DekiScriptMap ValidateToMap(DekiScriptParameter[] parameters, DekiScriptMap args) {
            DekiScriptMap result = new DekiScriptMap();

            // check passed in arguments
            for(int i = 0; i < parameters.Length; ++i) {
                var value = args[parameters[i].Name];
                if(value.IsNil) {
                    if((parameters[i].ScriptType != DekiScriptType.ANY) && !parameters[i].Optional) {
                        throw new ArgumentException(string.Format("missing value for parameter '{0}' (index {1})", parameters[i].Name, i));
                    }

                    // set default value for this parameter
                    result.Add(parameters[i].Name, parameters[i].Default);
                } else {
                    result.Add(parameters[i].Name, parameters[i].Convert(value));
                }
            }
            return result;
        }

        public static DekiScriptList ValidateToList(DekiScriptParameter[] parameters, DekiScriptMap args) {
            DekiScriptList result = new DekiScriptList();

            // check passed in arguments
            for(int i = 0; i < parameters.Length; ++i) {
                var value = args[parameters[i].Name];
                if(value.IsNil) {
                    if((parameters[i].ScriptType != DekiScriptType.ANY) && !parameters[i].Optional) {
                        throw new ArgumentException(string.Format("missing value for parameter '{0}' (index {1})", parameters[i].Name, i));
                    }

                    // set default value for this parameter
                    result.Add(parameters[i].Default);
                } else {
                    result.Add(parameters[i].Convert(value));
                }
            }
            return result;
        }

        //--- Fields ---
        public readonly string Name;
        public readonly DekiScriptType ScriptType;
        public readonly bool Optional;
        public readonly string Hint;
        public readonly Type NativeType;
        public readonly DekiScriptLiteral Default;

        //--- Contructors ---
        public DekiScriptParameter(string name, DekiScriptType type, bool optional, string hint) {
            if(string.IsNullOrEmpty(name)) {
                throw new NullReferenceException("name");
            }
            this.Name = name;
            this.ScriptType = type;
            this.Optional = optional;
            this.Hint = hint;
            this.NativeType = typeof(object);
            this.Default = DekiScriptNil.Value;
        }

        public DekiScriptParameter(string name, DekiScriptType type, bool optional, string hint, Type nativeType, DekiScriptLiteral @default) {
            if(string.IsNullOrEmpty(name)) {
                throw new NullReferenceException("name");
            }
            if(nativeType == null) {
                throw new NullReferenceException("nativeType");
            }
            if(@default == null) {
                throw new NullReferenceException("default");
            }
            this.Name = name;
            this.ScriptType = type;
            this.Optional = optional;
            this.Hint = hint;
            this.NativeType = nativeType;
            this.Default = @default;
        }

        //--- Methods ---
        public DekiScriptLiteral Convert(DekiScriptLiteral value) {
            try {
                return value.Convert(ScriptType);
            } catch(DekiScriptInvalidCastException) {
                throw new DekiScriptInvalidParameterCastException(Location.None, this, value.ScriptType);
            }
        }

        public void AppendXml(XDoc doc) {
            doc.Start("param").Attr("name", Name).Attr("type", DekiScriptLiteral.AsScriptTypeName(ScriptType));
            if(Default.IsNil) {
                doc.Attr("optional", Optional ? "true" : null);
            } else {
                doc.Attr("default", Default.ToString());
            }
            doc.Value(Hint);
            doc.End();
        }
    }
}
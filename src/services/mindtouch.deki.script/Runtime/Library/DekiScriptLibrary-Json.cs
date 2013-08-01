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
using System.CodeDom.Compiler;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using MindTouch.Dream;
using MindTouch.Xml;

namespace MindTouch.Deki.Script.Runtime.Library {
    public static partial class DekiScriptLibrary {

        //--- Class Methods ---
        [DekiScriptFunction("json.emit", "Convert DekiScript value to JavaScript Object Notation (JSON) value.", IsIdempotent = true)]
        public static string JsonEmit(
            [DekiScriptParam("value to convert", true)] object value
        ) {
            StringBuilder result = new StringBuilder();
            JsonEmit(value, result);
            return result.ToString();
        }

        [DekiScriptFunction("json.format", "Render JavaScript Object Notation (JSON) value as a pretty string.", IsIdempotent = true)]
        public static string JsonFormat(
            [DekiScriptParam("value to convert", true)] object value
        ) {
            using(StringWriter writer = new StringWriter()) {
                JsonFormat(value, new IndentedTextWriter(writer));
                return writer.ToString();
            }
        }

        [DekiScriptFunction("json.parse", "Convert JavaScript Object Notation (JSON) value to a DekiScript value.", IsIdempotent = true)]
        public static object JsonParse(
            [DekiScriptParam("value to convert", true)] string value,
            DekiScriptRuntime runtime
        ) {

            // TODO (steveb): 'json.parse' based on spec at http://json.org/

            return StringDeserialize(value, runtime);
        }

        private static void JsonEmit(object value, StringBuilder result) {
            if(value == null) {
                result.Append("null");
            } else if((value is string) || (value is XUri) || (value is XDoc)) {
                result.Append(value.ToString().QuoteString());
            } else if(value is ArrayList) {
                bool first = true;
                result.Append("[");
                foreach(object entry in (ArrayList)value) {
                    if(!first) {
                        result.Append(", ");
                    }
                    first = false;
                    JsonEmit(entry, result);
                }
                result.Append("]");
            } else if(value is Hashtable) {
                bool first = true;
                result.Append("{");
                foreach(DictionaryEntry entry in (Hashtable)value) {
                    if(!first) {
                        result.Append(", ");
                    }
                    first = false;
                    result.Append(((string)entry.Key).QuoteString()).Append(": ");
                    JsonEmit(entry.Value, result);
                }
                result.Append("}");
            } else if(value is bool) {
                result.Append((bool)value ? "true" : "false");
            } else {
                result.Append(value.ToString());
            }
        }

        private static void JsonFormat(object value, IndentedTextWriter writer) {
            if(value == null) {
                writer.Write("null");
            } else if((value is string) || (value is XUri) || (value is XDoc)) {
                writer.Write(value.ToString().QuoteString());
            } else if(value is ArrayList) {
                bool first = true;
                writer.Write("[ ");
                writer.Indent += 1;
                foreach(object entry in (ArrayList)value) {
                    if(!first) {
                        writer.Write(", ");
                    }
                    writer.WriteLine();
                    first = false;
                    JsonFormat(entry, writer);
                }
                writer.Indent -= 1;
                if(!first) {
                    writer.WriteLine();
                }
                writer.Write("]");
            } else if(value is Hashtable) {
                bool first = true;
                writer.Write("{ ");
                writer.Indent += 1;
                foreach(var entry in from DictionaryEntry pair in (Hashtable)value orderby ((string)pair.Key).ToLowerInvariant() select pair) {
                    if(!first) {
                        writer.Write(", ");
                    }
                    writer.WriteLine();
                    first = false;
                    writer.Write(((string)entry.Key).QuoteString());
                    writer.Write(": ");
                    JsonFormat(entry.Value, writer);
                }
                writer.Indent -= 1;
                if(!first) {
                    writer.WriteLine();
                }
                writer.Write("}");
            } else if(value is bool) {
                writer.Write((bool)value ? "true" : "false");
            } else {
                writer.Write(value.ToString());
            }
        }
    }
}

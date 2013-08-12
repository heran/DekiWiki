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
using MindTouch.Dream;

namespace MindTouch.Deki.Script.Runtime.Library {
    public static partial class DekiScriptLibrary {

        //--- Class Methods ---
        [DekiScriptFunction("uri.encode", "Encode text as a URI component.", IsIdempotent = true)]
        public static string UriEncode(
            [DekiScriptParam("text to encode")] string text
        ) {
            return XUri.Encode(text);
        }

        [DekiScriptFunction("uri.decode", "Decode text as a URI component.", IsIdempotent = true)]
        public static string UriDecode(
            [DekiScriptParam("text to decode")] string text
        ) {
            return XUri.Decode(text);
        }

        [DekiScriptFunction("uri.parse", "Parse a URI into its component parts. (OBSOLETE: use uri.parts instead)", IsIdempotent = true)]
        internal static Hashtable UriParse(
            [DekiScriptParam("uri to parse")] XUri uri
        ) {
            return UriParts(uri);
        }

        [DekiScriptFunction("uri.parts", "Decompose a URI into its component parts.", IsIdempotent = true)]
        public static Hashtable UriParts(
            [DekiScriptParam("uri to decompose")] XUri uri
        ) {
            Hashtable result = new Hashtable(StringComparer.OrdinalIgnoreCase);
            result.Add("scheme", uri.Scheme);
            result.Add("user", uri.User);
            result.Add("password", uri.Password);
            result.Add("host", uri.HostPort);
            result.Add("path", new ArrayList(uri.Segments));
            Hashtable query = new Hashtable(StringComparer.OrdinalIgnoreCase);
            if(uri.Params != null) {
                foreach(KeyValuePair<string, string> entry in uri.Params) {
                    query.Add(entry.Key, entry.Value);
                }
            }
            result.Add("query", query);
            result.Add("fragment", uri.Fragment);
            return result;
        }

        [DekiScriptFunction("uri.appendquery", "Append query parameters to a URI.", IsIdempotent = true)]
        public static string UriAppendQuery(
            [DekiScriptParam("base uri")] XUri uri,
            [DekiScriptParam("query parameters to append")] Hashtable args
        ) {
            foreach(DictionaryEntry entry in args) {
                string value = SysUtil.ChangeType<string>(entry.Value);
                if(value != null) {
                    uri = uri.With((string)entry.Key, value);
                }
            }
            return uri.ToString();
        }

        [DekiScriptFunction("uri.appendpath", "Append path segments to a URI.", IsIdempotent = true)]
        public static string UriAppendPath(
            [DekiScriptParam("base uri")] XUri uri,
            [DekiScriptParam("path segments to append (must a string or list of strings)", true)] object path
        ) {
            if(path is string) {
                uri = uri.At((string)path);
            } else if(path is ArrayList) {
                foreach(string segment in (ArrayList)path) {
                    uri = uri.At(segment);
                }
            }
            return uri.ToString();
        }

        [DekiScriptFunction("uri.build", "Build a new URI with path and query parameters.", IsIdempotent = true)]
        public static string UriBuild(
            [DekiScriptParam("base uri")] XUri uri,
            [DekiScriptParam("path segments to append (must a string or list of strings)", true)] object path,
            [DekiScriptParam("query parameters to append", true)] Hashtable args
        ) {
            if(path is string) {
                uri = uri.AtPath((string)path);
            } else if(path is ArrayList) {
                foreach(string segment in (ArrayList)path) {
                    uri = uri.At(XUri.EncodeSegment(segment));
                }
            }
            if(args != null) {
                foreach(DictionaryEntry entry in args) {
                    string key = (string)entry.Key;

                    // remove existing parameter
                    uri = uri.WithoutParams(key);

                    // check if entry is a list of values
                    if(entry.Value is ArrayList) {
                        foreach(var value in (ArrayList)entry.Value) {
                            uri = uri.With(key, SysUtil.ChangeType<string>(value));
                        }
                    } else if(entry.Value != null) {
                        uri = uri.With(key, SysUtil.ChangeType<string>(entry.Value));
                    }
                }
            }
            return uri.ToString();
        }

        [DekiScriptFunction("uri.isvalid", "Check if a value is a valid URI.", IsIdempotent = true)]
        public static bool UriIsValid(
            [DekiScriptParam("uri to validate", true)] object value
        ) {
            if(value == null) {
                return false;
            }
            if(value is XUri) {
                return true;
            }
            if(value is string) {
                return XUri.TryParse((string)value) != null;
            }
            return false;
        }
    }
}

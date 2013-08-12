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
using MindTouch.Deki.Script.Compiler;
using MindTouch.Deki.Script.Expr;

namespace MindTouch.Deki.Script.Runtime.Library {
    public static partial class DekiScriptLibrary {

        //--- Class Methods ---
        [DekiScriptFunction("map.values", "Get the list of values of a map.", IsIdempotent = true)]
        public static ArrayList MapValues(
            [DekiScriptParam("map value")] Hashtable map,
            DekiScriptRuntime runtime
        ) {
            ArrayList result = new ArrayList(map.Values);
            for(int i = 0; i < result.Count; ++i) {
                result[i] = Eval(result[i], runtime);
            }
            return result;
        }

        [DekiScriptFunction("map.keys", "Get the list of keys of a map.", IsIdempotent = true)]
        public static ArrayList MapKeys(
            [DekiScriptParam("map value")] Hashtable map
        ) {
            return new ArrayList(map.Keys);
        }

        [DekiScriptFunction("map.select", "Create a map that only contains key-value pairs for which the condition succeeds.")]
        public static Hashtable MapFilter(
            [DekiScriptParam("map value")] Hashtable map,
            [DekiScriptParam("condition to execute for each item (use '$' to refer to the key-value pair)")] string condition,
            DekiScriptRuntime runtime
        ) {
            DekiScriptExpression expr = DekiScriptParser.Parse(new Location("map.select(condition)"), condition);
            Hashtable result = new Hashtable(StringComparer.OrdinalIgnoreCase);
            foreach(DictionaryEntry entry in map) {
                DekiScriptMap keyvalue = new DekiScriptMap();
                object value = Eval(entry.Value, runtime);
                keyvalue.Add("key", DekiScriptLiteral.FromNativeValue(entry.Key));
                keyvalue.Add("value", DekiScriptLiteral.FromNativeValue(value));
                DekiScriptEnv env = runtime.CreateEnv();
                env.Vars.Add(DekiScriptRuntime.DEFAULT_ID, keyvalue);
                DekiScriptLiteral test = runtime.Evaluate(expr, DekiScriptEvalMode.EvaluateSafeMode, env);
                if(!test.IsNilFalseZero) {
                    result.Add(entry.Key, value);
                }
            }
            return result;
        }

        [DekiScriptFunction("map.apply", "Create a new map by applying the expression to each key value pair.")]
        public static Hashtable MapApply(
            [DekiScriptParam("map value")] Hashtable map,
            [DekiScriptParam("expression to apply (use '$' to refer to the item)")] string expression,
            DekiScriptRuntime runtime
        ) {
            DekiScriptExpression expr = DekiScriptParser.Parse(new Location("map.apply(expression)"), expression);
            Hashtable result = new Hashtable(StringComparer.OrdinalIgnoreCase);
            foreach(DictionaryEntry entry in map) {
                DekiScriptMap keyvalue = new DekiScriptMap();
                keyvalue.Add("key", DekiScriptLiteral.FromNativeValue(entry.Key));
                keyvalue.Add("value", DekiScriptLiteral.FromNativeValue(Eval(entry.Value, runtime)));
                DekiScriptEnv env = runtime.CreateEnv();
                env.Vars.Add(DekiScriptRuntime.DEFAULT_ID, keyvalue);
                result.Add(entry.Key, runtime.Evaluate(expr, DekiScriptEvalMode.EvaluateSafeMode, env));
            }
            return result;
        }

        [DekiScriptFunction("map.keyvalues", "Create a list of the key-value pairs.", IsIdempotent = true)]
        public static ArrayList MapKeyValues(
            [DekiScriptParam("map value")] Hashtable map,
            DekiScriptRuntime runtime
        ) {
            ArrayList result = new ArrayList(map.Count);
            foreach(DictionaryEntry entry in map) {
                Hashtable keyvalue = new Hashtable();
                keyvalue.Add("key", entry.Key);
                keyvalue.Add("value", Eval(entry.Value, runtime));
                result.Add(keyvalue);
            }
            return result;
        }

        [DekiScriptFunction("map.contains", "Check if the key is contained in a map.", IsIdempotent = true)]
        public static bool MapContains(
            [DekiScriptParam("map value")] Hashtable map,
            [DekiScriptParam("key value")] string key
        ) {
            return map.ContainsKey(key);
        }

        [DekiScriptFunction("map.remove", "Remove the key from a map and return the modified map.", IsIdempotent = true)]
        public static Hashtable MapRemove(
            [DekiScriptParam("map value")] Hashtable map,
            [DekiScriptParam("key value")] string key
        ) {
            Hashtable newmap = new Hashtable(map);
            newmap.Remove(key);
            return newmap;
        }
    }
}

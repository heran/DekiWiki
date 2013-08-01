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
using System.Text;
using MindTouch.Deki.Script.Compiler;
using MindTouch.Deki.Script.Expr;
using MindTouch.Dream;

namespace MindTouch.Deki.Script.Runtime.Library {
    public static partial class DekiScriptLibrary {

        //--- Types ---
        private class DekiScriptComparer : IComparer {

            //--- Fields ---
            private readonly DekiScriptRuntime _runtime;
            private readonly DekiScriptExpression _compare;
            private readonly DekiScriptEnv _env;
            private readonly DekiScriptMap _values;

            //--- Constructors ---
            public DekiScriptComparer(DekiScriptRuntime runtime, DekiScriptExpression compare) {
                if(compare == null) {
                    throw new ArgumentNullException("compare");
                }
                _runtime = runtime;
                _compare = compare;
                _values = new DekiScriptMap();
                _env = runtime.CreateEnv();
                _env.Vars.Add(DekiScriptRuntime.DEFAULT_ID, _values);
            }

            //--- Methods ---
            public int Compare(object left, object right) {
                _values.Add("left", DekiScriptLiteral.FromNativeValue(left));
                _values.Add("right", DekiScriptLiteral.FromNativeValue(right));
                _env.Vars.Add(DekiScriptRuntime.DEFAULT_ID, _values);
                DekiScriptLiteral test = _runtime.Evaluate(_compare, DekiScriptEvalMode.EvaluateSafeMode, _env);
                return (int)(test.AsNumber() ?? 0.0);
            }
        }

        //--- Class Methods ---
        [DekiScriptFunction("list.reverse", "Reverse the items in a list.", IsIdempotent = true)]
        public static ArrayList ListReverse(
            [DekiScriptParam("list value")] ArrayList list
        ) {
            list.Reverse();
            return list;
        }

        [DekiScriptFunction("list.sort", "Sort the items in a list.")]
        public static ArrayList ListSort(
            [DekiScriptParam("list of values")] ArrayList list,
            [DekiScriptParam("key for value if list contains maps (default: nil)", true)] object key,
            [DekiScriptParam("sort in reverse order (default: false)", true)] bool? reverse,
            [DekiScriptParam("compare two items (return -1 if left item less than the right item, 0 if they are equal, and +1 if the left item is greater than the right item; use '$left' and '$right' to refer to the left and right items respectively)", true)] string compare,
            DekiScriptRuntime runtime
       ) {

            // prepare custom comparer
            IComparer comparer = null;
            if(compare != null) {
                comparer = new DekiScriptComparer(runtime, DekiScriptParser.Parse(new Location("list.sort(compare)"), compare));
            }

            // sort list
            if(key == null || (key as string != null && string.IsNullOrEmpty(key as string))) {
                list.Sort(comparer);
            } else {
                Array keys = (key is ArrayList) ? ((ArrayList)key).ToArray() : ListCollect(list, key.ToString(), runtime).ToArray();
                Array values = list.ToArray();
                Array.Sort(keys, values, comparer);
                list = new ArrayList(values);
            }

            // check if results need to be reveresed
            if(reverse ?? false) {
                list.Reverse();
            }
            return list;
        }

        [DekiScriptFunction("list.collect", "Collect all values from each map inside the list.")]
        public static ArrayList ListCollect(
            [DekiScriptParam("list of maps")] ArrayList list,
            [DekiScriptParam("key for value to collect")] string key,
            DekiScriptRuntime runtime
        ) {
            ArrayList result = new ArrayList();
            foreach(object entry in list) {
                Hashtable map = entry as Hashtable;
                if((map != null) && map.ContainsKey(key)) {
                    object value = map[key];
                    result.Add(Eval(value, runtime));
                }
            }
            return result;
        }

        [DekiScriptFunction("list.select", "Create a list that only contains values for which the condition succeeds.")]
        public static ArrayList ListSelect(
            [DekiScriptParam("list value")] ArrayList list,
            [DekiScriptParam("condition to execute for each item (use '$' to refer to the item)")] string condition,
            DekiScriptRuntime runtime
        ) {
            DekiScriptExpression expr = DekiScriptParser.Parse(new Location("list.select(condition)"), condition);
            ArrayList result = new ArrayList();
            foreach(object entry in list) {
                DekiScriptEnv env = runtime.CreateEnv();
                env.Vars.Add(DekiScriptRuntime.DEFAULT_ID, DekiScriptLiteral.FromNativeValue(entry));
                DekiScriptLiteral test = runtime.Evaluate(expr, DekiScriptEvalMode.EvaluateSafeMode, env);
                if(!test.IsNilFalseZero) {
                    result.Add(entry);
                }
            }
            return result;
        }

        [DekiScriptFunction("list.apply", "Create a new list by applying the expression to each item.")]
        public static ArrayList ListApply(
            [DekiScriptParam("list value")] ArrayList list,
            [DekiScriptParam("expression to apply (use '$' to refer to the current item)")] string expression,
            DekiScriptRuntime runtime
        ) {
            DekiScriptExpression expr = DekiScriptParser.Parse(new Location("list.apply(expression)"), expression);
            ArrayList result = new ArrayList();
            foreach(object entry in list) {
                DekiScriptEnv env = runtime.CreateEnv();
                env.Vars.Add(DekiScriptRuntime.DEFAULT_ID, DekiScriptLiteral.FromNativeValue(entry));
                result.Add(runtime.Evaluate(expr, DekiScriptEvalMode.EvaluateSafeMode, env).NativeValue);
            }
            return result;
        }

        [DekiScriptFunction("list.splice", "Split a list and insert new items into it.", IsIdempotent = true)]
        public static ArrayList ListSplice(
            [DekiScriptParam("list value")] ArrayList list,
            [DekiScriptParam("copy all values before the start offset; if negative, offset is relative to end of list")] int start,
            [DekiScriptParam("number of elements to skip; if negative, relative to length of list (default: all)", true)] int? length,
            [DekiScriptParam("values to append after start offset (default: none)", true)] ArrayList values
        ) {

            // compute offset position
            if(start < 0) {
                start = Math.Max(0, list.Count + start);
            }
            start = Math.Min(list.Count, start);

            // compute length
            int count = length.GetValueOrDefault(list.Count);
            if(count < 0) {
                count = Math.Max(0, (list.Count + count) - start);
            }

            // copy beginning
            ArrayList result = new ArrayList();
            for(int i = 0; i < start; ++i) {
                result.Add(list[i]);
            }

            // copy new values (if any)
            if(values != null) {
                result.AddRange(values);
            }

            // copy end
            for(int i = start + count; i < list.Count; ++i) {
                result.Add(list[i]);
            }
            return result;
        }

        [DekiScriptFunction("list.contains", "Check if list contains the given value.", IsIdempotent = true)]
        public static bool ListContains(
            [DekiScriptParam("list value")] ArrayList list,
            [DekiScriptParam("value to check for")] object value,
            [DekiScriptParam("ignore case (default: false)", true)] bool? ignorecase
        ) {
            if((ignorecase ?? false) && (value is string)) {
                string text = (string)value;
                foreach(var item in list) {
                    if((item is string) && text.EqualsInvariantIgnoreCase((string) item)) {
                        return true;
                    }
                }
                return false;
            }
            return list.Contains(value);
        }

        [DekiScriptFunction("list.indexof", "Find the index position of the given value.", IsIdempotent = true)]
        public static int ListIndexOf(
            [DekiScriptParam("list value")] ArrayList list,
            [DekiScriptParam("value to check for")] object value
        ) {
            return list.IndexOf(value);
        }

        [DekiScriptFunction("list.lastindexof", "Find the last index position of the given value.", IsIdempotent = true)]
        public static int ListLastIndexOf(
            [DekiScriptParam("list value")] ArrayList list,
            [DekiScriptParam("value to check for")] object value
        ) {
            return list.LastIndexOf(value);
        }

        [DekiScriptFunction("list.indexesof", "Find all index positions of the given value.", IsIdempotent = true)]
        public static ArrayList ListIndexesOf(
            [DekiScriptParam("list value")] ArrayList list,
            [DekiScriptParam("value to check for")] object value
        ) {
            ArrayList result = new ArrayList();
            int start = 0;
            while(start < list.Count) {
                int next = list.IndexOf(value, start);
                if(next < 0) {
                    break;
                }
                result.Add(next);
                start = next + 1;
            }
            return result;
        }

        [DekiScriptFunction("list.sum", "Get sum of values in list.", IsIdempotent = true)]
        public static double ListSum(
            [DekiScriptParam("list value")] ArrayList list,
            [DekiScriptParam("expression to fetch value (use '$' to refer to the current item; default: $)", true)] string expression,
            DekiScriptRuntime runtime
        ) {
            if(expression != null) {
                list = ListApply(list, expression, runtime);
            }
            double result = 0.0;
            foreach(object entry in list) {
                try {
                    result += SysUtil.ChangeType<double>(entry);
                } catch { }
            }
            return result;
        }

        [DekiScriptFunction("list.min", "Get smallest value in list.", IsIdempotent = true)]
        public static double ListMin(
            [DekiScriptParam("list value")] ArrayList list,
            [DekiScriptParam("expression to fetch value (use '$' to refer to the current item; default: $)", true)] string expression,
            DekiScriptRuntime runtime
        ) {
            if(expression != null) {
                list = ListApply(list, expression, runtime);
            }
            double result = double.MaxValue;
            foreach(object entry in list) {
                try {
                    result = Math.Min(result, SysUtil.ChangeType<double>(entry));
                } catch { }
            }
            return result;
        }

        [DekiScriptFunction("list.max", "Get largest value in list.", IsIdempotent = true)]
        public static double ListMax(
            [DekiScriptParam("list value")] ArrayList list,
            [DekiScriptParam("expression to fetch value (use '$' to refer to the current item; default: $)", true)] string expression,
            DekiScriptRuntime runtime
        ) {
            if(expression != null) {
                list = ListApply(list, expression, runtime);
            }
            double result = double.MinValue;
            foreach(object entry in list) {
                try {
                    result = Math.Max(result, SysUtil.ChangeType<double>(entry));
                } catch { }
            }
            return result;
        }

        [DekiScriptFunction("list.average", "Get average of values in list.", IsIdempotent = true)]
        public static double ListAverage(
            [DekiScriptParam("list value")] ArrayList list,
            [DekiScriptParam("expression to fetch value (use '$' to refer to the current item; default: $)", true)] string expression,
            DekiScriptRuntime runtime
        ) {
            if(expression != null) {
                list = ListApply(list, expression, runtime);
            }
            double result = 0.0;
            int count = 0;
            foreach(object entry in list) {
                try {
                    result += SysUtil.ChangeType<double>(entry);
                    ++count;
                } catch { }
            }
            return (count > 0) ? (result / count) : 0.0;
        }

        [DekiScriptFunction("list.random", "Get a random item from the list.")]
        public static object ListRandom(
            [DekiScriptParam("list value")] ArrayList list
        ) {
            if(list.Count == 0) {
                return null;
            }
            int index = RANDOM.Next(list.Count);
            return list[index];
        }

        [DekiScriptFunction("list.reduce", "Combine all values in list into a single value using the supplied expression.")]
        public static object ListReduce(
            [DekiScriptParam("list value")] ArrayList list,
            [DekiScriptParam("expression to compute combined value (use '$value' and '$item' to refer to the current value and item, respectively)")] string expression,
            [DekiScriptParam("starting value (default: nil)", true)] object value,
            DekiScriptRuntime runtime
        ) {
            DekiScriptExpression expr = DekiScriptParser.Parse(new Location("list.reduce(expression)"), expression);
            foreach(object entry in list) {
                DekiScriptEnv env = runtime.CreateEnv();
                DekiScriptMap values = new DekiScriptMap();
                values.Add("value", DekiScriptLiteral.FromNativeValue(value));
                values.Add("item", DekiScriptLiteral.FromNativeValue(entry));
                env.Vars.Add(DekiScriptRuntime.DEFAULT_ID, values);
                value = runtime.Evaluate(expr, DekiScriptEvalMode.EvaluateSafeMode, env).NativeValue;
            }
            return value;
        }

        [DekiScriptFunction("list.orderby", "Sort items in list using key names.")]
        public static ArrayList ListOrderBy(
            [DekiScriptParam("list value")] ArrayList list,
            [DekiScriptParam("key name or list of key names; sort direction is controlled by appendending \" ascending\" or \" descending\" to the key(s); when omitted, the direction is asending by default")] object keys,
            DekiScriptRuntime runtime
        ) {

            // check for trivial sort case
            if(keys is string) {
                string key = ((string)keys).Trim();

                // the key cannot contain access operators
                if(key.IndexOfAny(new[] { '.', '[', ']' }) < 0) {
                    return ListOrderBy_IsReversed(ref key) ? ListSort(list, key, true, null, runtime) : ListSort(list, key, false, null, runtime);
                }

                // let's change 'keys' into an array list for processing convenience
                ArrayList temp = new ArrayList { keys };
                keys = temp;
            }

            // check that 'keys' has a valid type
            if(!(keys is ArrayList)) {
                throw new DekiScriptBadTypeException(Location.None, DekiScriptLiteral.AsScriptType(keys.GetType()), new[] { DekiScriptType.STR, DekiScriptType.LIST });
            }

            // build comparison expression
            StringBuilder compare = new StringBuilder();
            foreach(string key in (ArrayList)keys) {
                if(compare.Length > 0) {
                    compare.Append(" || ");
                }

                // process sort field
                string field = key;
                if(ListOrderBy_IsReversed(ref field)) {
                    compare.AppendFormat("(if($left.{0} < $right.{0}) 1; else if($left.{0} > $right.{0}) -1; else 0;)", field);
                } else {
                    compare.AppendFormat("(if($left.{0} < $right.{0}) -1; else if($left.{0} > $right.{0}) 1; else 0;)", field);
                }
            }

            // sort list
            list.Sort(new DekiScriptComparer(runtime, DekiScriptParser.Parse(new Location("list.orderby(compare)"), compare.ToString())));
            return list;
        }

        private static bool ListOrderBy_IsReversed(ref string key) {

            // check if the key has a sorting order
            if(key.EndsWithInvariant(" ascending")) {
                key = key.Substring(0, key.Length - 10 /* " ascending" */);
            } else if(key.EndsWithInvariant(" descending")) {
                key = key.Substring(0, key.Length - 11 /* " descending" */);
                return true;
            }
            return false;
        }

        [DekiScriptFunction("list.groupby", "Group items in list by common expression value.")]
        public static Hashtable ListGroupBy(
            [DekiScriptParam("list value")] ArrayList list,
            [DekiScriptParam("expression to apply for grouping (use '$' to refer to the current item)")] string expression,
            DekiScriptRuntime runtime
        ) {
            Hashtable result = new Hashtable(StringComparer.OrdinalIgnoreCase);
            DekiScriptExpression expr = DekiScriptParser.Parse(new Location("list.groupby(expression)"), expression);

            // loop over all items in list
            foreach(object entry in list) {
                DekiScriptEnv env = runtime.CreateEnv();
                env.Vars.Add(DekiScriptRuntime.DEFAULT_ID, DekiScriptLiteral.FromNativeValue(entry));

                // evalute grouping expression
                string key = runtime.Evaluate(expr, DekiScriptEvalMode.EvaluateSafeMode, env).AsString();
                if(key != null) {

                    // check if an accumulation list already exists; otherwise, create one
                    ArrayList accumulator = (ArrayList)result[key];
                    if(accumulator == null) {
                        accumulator = new ArrayList();
                        result[key] = accumulator;
                    }
                    accumulator.Add(entry);
                }
            }
            return result;
        }

        [DekiScriptFunction("list.intersect", "Compute the list of values that are common to the first and second list.", IsIdempotent = true)]
        public static ArrayList ListIntersect(
            [DekiScriptParam("first list")] ArrayList first,
            [DekiScriptParam("second list")] ArrayList second,
            [DekiScriptParam("expression to determine if item from the first list should be included in final list (return true to include, false to exclude; use '$left' and '$right' to refer to the current item from the first and seconds lists respectively; default: equality condition)", true)] string condition,
            DekiScriptRuntime runtime
        ) {
            ArrayList result = new ArrayList();
            if(condition != null) {

                // loop over both lists and keep items from the first list based on the outcome of the condition
                DekiScriptExpression expr = DekiScriptParser.Parse(new Location("list.intersect(condition)"), condition);
                foreach(object left in first) {
                    foreach(object right in second) {
                        DekiScriptEnv env = runtime.CreateEnv();
                        DekiScriptMap keyvalue = new DekiScriptMap();
                        keyvalue.Add("left", DekiScriptLiteral.FromNativeValue(left));
                        keyvalue.Add("right", DekiScriptLiteral.FromNativeValue(right));
                        env.Vars.Add(DekiScriptRuntime.DEFAULT_ID, keyvalue);
                        DekiScriptLiteral test = runtime.Evaluate(expr, DekiScriptEvalMode.EvaluateSafeMode, env);
                        if(!test.IsNilFalseZero) {
                            result.Add(left);
                            break;
                        }
                    }
                }
            } else {

                // using simple containment check to keep items
                foreach(object item in first) {
                    if(second.Contains(item)) {
                        result.Add(item);
                    }
                }
            }
            return result;
        }

        [DekiScriptFunction("list.combine", "Combine two lists into a map.", IsIdempotent = true)]
        public static Hashtable ListCombine(
            [DekiScriptParam("list of keys")] ArrayList keys,
            [DekiScriptParam("list of values")] ArrayList values
        ) {
            Hashtable result = new Hashtable(StringComparer.OrdinalIgnoreCase);
            for(int i = 0; i < Math.Min(keys.Count, values.Count); ++i) {
                try {
                    string key = SysUtil.ChangeType<string>(keys[i]);
                    if(!string.IsNullOrEmpty(key)) {
                        result[key] = values[i];
                    }
                } catch { }
            }
            return result;
        }

        [DekiScriptFunction("list.new", "Create a list of a given size.", IsIdempotent = true)]
        public static ArrayList ListNew(
            [DekiScriptParam("size of the list")] int size,
            [DekiScriptParam("intial value for list entry (default: nil)", true)] object value
        ) {
            if((size < 0) || (size > 1000000)) {
                return null;
            }
            ArrayList result = new ArrayList(size);
            for(int i = 0; i < size; ++i) {
                result.Add(value);
            }
            return result;
        }

        [DekiScriptFunction("list.count", "Count the number of items for which the condition succeeds.")]
        public static int ListCount(
            [DekiScriptParam("list value")] ArrayList list,
            [DekiScriptParam("condition to execute for each item (use '$' to refer to the item)")] string condition,
            DekiScriptRuntime runtime
        ) {
            DekiScriptExpression expr = DekiScriptParser.Parse(new Location("list.count(condition)"), condition);
            int count = 0;
            foreach(object entry in list) {
                DekiScriptEnv env = runtime.CreateEnv();
                env.Vars.Add(DekiScriptRuntime.DEFAULT_ID, DekiScriptLiteral.FromNativeValue(entry));
                DekiScriptLiteral test = runtime.Evaluate(expr, DekiScriptEvalMode.EvaluateSafeMode, env);
                if(!test.IsNilFalseZero) {
                    ++count;
                }
            }
            return count;
        }
    }
}

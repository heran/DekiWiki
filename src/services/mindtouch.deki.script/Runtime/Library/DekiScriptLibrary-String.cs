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
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.Collections.Generic;
using MindTouch.Deki.Script.Compiler;
using MindTouch.Deki.Script.Expr;
using MindTouch.Xml;

namespace MindTouch.Deki.Script.Runtime.Library {
    public static partial class DekiScriptLibrary {

        //--- Constants ---
        private static readonly Regex LUCENE_ESCAPE = new Regex("[+\\-!(){}\\[\\]^\\\"~*?:\\\\]", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        //--- Class Methods ---
        [DekiScriptFunction("string.quote", "Quote string and escape characters.", IsIdempotent = true)]
        public static string StringQuote(
            [DekiScriptParam("string value")] string text
        ) {
            return text.QuoteString();
        }

        [DekiScriptFunction("string.tolower", "Convert string to lowercase characters.", IsIdempotent = true)]
        public static string StringToLower(
            [DekiScriptParam("string value")] string text
        ) {
            CultureInfo culture = GetCulture();
            return text.ToLower(culture);
        }

        [DekiScriptFunction("string.tolowerfirst", "Convert first character in string to lowercase.", IsIdempotent = true)]
        public static string StringToLowerFirst(
            [DekiScriptParam("string value")] string text
        ) {
            CultureInfo culture = GetCulture();
            if(text.Length == 0) {
                return text;
            }
            return char.ToLower(text[0], culture) + text.Substring(1);
        }

        [DekiScriptFunction("string.toupper", "Convert string to uppercase characters.", IsIdempotent = true)]
        public static string StringToUpper(
            [DekiScriptParam("string value")] string text
        ) {
            CultureInfo culture = GetCulture();
            return text.ToUpper(culture);
        }

        [DekiScriptFunction("string.toupperfirst", "Convert first character in string to uppercase.", IsIdempotent = true)]
        public static string StringToUpperFirst(
            [DekiScriptParam("string value")] string text
        ) {
            CultureInfo culture = GetCulture();
            if(text.Length == 0) {
                return text;
            }
            return char.ToUpper(text[0], culture) + text.Substring(1);
        }

        [DekiScriptFunction("string.tocamelcase", "Convert first charater of each word in string to uppercase.", IsIdempotent = true)]
        public static string StringToCamelCase(
            [DekiScriptParam("string value")] string text
        ) {
            CultureInfo culture = GetCulture();
            return culture.TextInfo.ToTitleCase(text);
        }

        [DekiScriptFunction("string.substr", "Extract a substring.", IsIdempotent = true)]
        public static string StringSubStr(
            [DekiScriptParam("string value")] string text,
            [DekiScriptParam("sub-string start index")] int start,
            [DekiScriptParam("sub-string length (default: remainder of string)", true)] int? length
        ) {

            // determine start offset
            if(start < 0) {
                start = text.Length + start;
            }
            start = Math.Max(0, Math.Min(start, text.Length));

            // determine length
            int len = length ?? text.Length;
            if(len < 0) {
                len = (text.Length - start) + len;
            }
            len = Math.Max(0, Math.Min(len, text.Length - start));

            // extract string
            return text.Substring(start, len);
        }

        [DekiScriptFunction("string.replace", "Replaces all occurrences of a string with another string.", IsIdempotent = true)]
        public static string StringReplace(
            [DekiScriptParam("string value")] string text,
            [DekiScriptParam("old value")] string before,
            [DekiScriptParam("new value")] string after,
            [DekiScriptParam("ignore case (default: false)", true)] bool? ignorecase
        ) {

            // check for the degenarate case
            if(before.Length == 0) {

                // nothing to do
                return text;
            }

            // check if we're doing a case-insensitve replace
            if(ignorecase ?? false) {
                StringBuilder result = new StringBuilder();

                // use culture specific comparer
                CompareInfo compare = GetCulture().CompareInfo;
                int start = 0;
                while(true) {
                    int index = compare.IndexOf(text, before, start, CompareOptions.IgnoreCase);
                    if(index < 0) {

                        // append the end of the string and we're done
                        result.Append(text, start, text.Length - start);
                        break;
                    }

                    // append the part we skipped over, then the replacement
                    result.Append(text, start, index - start);
                    result.Append(after);
                    start = index + before.Length;
                }
                return result.ToString();
            }
            return text.Replace(before, after);
        }

        [DekiScriptFunction("string.padleft", "Pad string with characters on the left until it reaches the specified width.", IsIdempotent = true)]
        public static string StringPadLeft(
            [DekiScriptParam("string value")] string text,
            [DekiScriptParam("total width (negative: relative to current length; positive: absolute length)")] int width,
            [DekiScriptParam("padding character (default: \" \")", true)] string padding
        ) {
            return text.PadLeft((width <= 0) ? (text.Length - width) : width, string.IsNullOrEmpty(padding) ? ' ' : padding[0]);
        }

        [DekiScriptFunction("string.padright", "Pad string with characters on the right until it reaches the specified width.", IsIdempotent = true)]
        public static string StringPadRight(
            [DekiScriptParam("string value")] string text,
            [DekiScriptParam("total width (negative: relative to current length; positive: absolute length)")] int width,
            [DekiScriptParam("padding character (default: \" \")", true)] string padding
        ) {
            return text.PadRight((width <= 0) ? (text.Length - width) : width, string.IsNullOrEmpty(padding) ? ' ' : padding[0]);
        }

        [DekiScriptFunction("string.length", "Get the length of the string.", IsIdempotent = true)]
        public static int StringLength(
            [DekiScriptParam("string value", true)] string text
        ) {
            return (text ?? string.Empty).Length;
        }

        [DekiScriptFunction("string.equals", "Determine equality of two strings", IsIdempotent = true)]
        public static bool StringEquals(
            [DekiScriptParam("first string value")] string first,
            [DekiScriptParam("second string value")] string second,
            [DekiScriptParam("ignore case (default: false)", true)] bool? ignorecase
            ) {
            return StringCompare(first, second, ignorecase) == 0;
        }

        [DekiScriptFunction("string.compare", "Compare two strings.", IsIdempotent = true)]
        public static int StringCompare(
            [DekiScriptParam("first string value")] string first,
            [DekiScriptParam("second string value")] string second,
            [DekiScriptParam("ignore case (default: false)", true)] bool? ignorecase
        ) {
            CultureInfo culture = GetCulture();
            if(ignorecase ?? false) {
                return culture.CompareInfo.Compare(first, second, CompareOptions.IgnoreCase);
            }
            return culture.CompareInfo.Compare(first, second);
        }

        [DekiScriptFunction("string.icompare", "Compare two strings ignoring case (OBSOLETE: use string.compare(first, second, true) instead).", IsIdempotent = true)]
        internal static int StringICompare(
            [DekiScriptParam("first string value")] string first,
            [DekiScriptParam("second string value")] string second
        ) {
            CultureInfo culture = GetCulture();
            return culture.CompareInfo.Compare(first, second, CompareOptions.IgnoreCase);
        }

        [DekiScriptFunction("string.match", "Match a string against a regular expression pattern and return the first match.", IsIdempotent = true)]
        public static object StringMatch(
            [DekiScriptParam("string value")] string text,
            [DekiScriptParam("regular expression pattern")] string pattern,
            [DekiScriptParam("ignore case (default: false)", true)] bool? ignorecase,
            [DekiScriptParam("return text with index (default: false)", true)] bool? withIndex
        ) {
            Match m = Regex.Match(text, pattern, (ignorecase ?? false) ? RegexOptions.IgnoreCase : RegexOptions.None);
            if(!m.Success) {
                return null;
            }
            if(withIndex.GetValueOrDefault()) {
                var info = new Hashtable(StringComparer.OrdinalIgnoreCase);
                info["text"] = m.Value;
                info["index"] = m.Index;
                return info;
            }
            return m.Value;
        }

        [DekiScriptFunction("string.matches", "Match a string against a regular expression pattern and return all matches.", IsIdempotent = true)]
        public static ArrayList StringMatches(
            [DekiScriptParam("string value")] string text,
            [DekiScriptParam("regular expression pattern")] string pattern,
            [DekiScriptParam("ignore case (default: false)", true)] bool? ignorecase,
            [DekiScriptParam("return text with index (default: false)", true)] bool? withIndex
        ) {
            ArrayList result = new ArrayList();
            for(var m = Regex.Match(text, pattern, (ignorecase ?? false) ? RegexOptions.IgnoreCase : RegexOptions.None); m.Success; m = m.NextMatch()) {
                if(withIndex.GetValueOrDefault()) {
                    var info = new Hashtable(StringComparer.OrdinalIgnoreCase);
                    info["text"] = m.Value;
                    info["index"] = m.Index;
                    result.Add(info);
                } else {
                    result.Add(m.Value);
                }
            }
            return result;
        }

        [DekiScriptFunction("string.matchreplace", "Replace every match the regular expression pattern with replacement value.", IsIdempotent = true)]
        public static string StringMatchReplace(
            [DekiScriptParam("string value")] string text,
            [DekiScriptParam("regular expression pattern")] string pattern,
            [DekiScriptParam("replacement value")] string replacement,
            [DekiScriptParam("ignore case (default: false)", true)] bool? ignorecase
        ) {
            Regex regex = new Regex(pattern, (ignorecase ?? false) ? RegexOptions.IgnoreCase : RegexOptions.None);
            return regex.Replace(text, replacement);
        }

        [DekiScriptFunction("string.join", "Join strings in a list using a specified separator.", IsIdempotent = true)]
        public static string StringJoin(
            [DekiScriptParam("list of strings")] ArrayList list,
            [DekiScriptParam("separator used between strings (default: \", \")", true)] string separator
        ) {
            return string.Join(separator ?? ", ", Array.ConvertAll(list.ToArray(), value => SysUtil.ChangeType<string>(value) ?? string.Empty));
        }

        [DekiScriptFunction("string.split", "Split string at each occurrence of the separator up to the specified limit.", IsIdempotent = true)]
        public static ArrayList StringSplit(
            [DekiScriptParam("string value")] string text,
            [DekiScriptParam("string separator")] string separator,
            [DekiScriptParam("maximum results (default: no limit)", true)] int? max
        ) {
            ArrayList result = new ArrayList();

            // check for degenerate case
            if(max.HasValue && (max.Value <= 1)) {
                result.Add(text);
                return result;
            }
            result.AddRange(text.Split(new[] { separator }, max ?? int.MaxValue, StringSplitOptions.None));
            return result;
        }

        [DekiScriptFunction("string.splitcsv", "Convert string into a collection of rows and columns.", IsIdempotent = true)]
        public static Hashtable StringSplitCsv(
            [DekiScriptParam("comma separated values")] string text
        ) {
            ArrayList rows = new ArrayList();
            int columncount = 0;
            CsvStream csv = new CsvStream(new StringReader(text));

            string[] row = null;
            do {
                row = csv.GetNextRow();
                if(row != null) {
                    rows.Add(new ArrayList(row));
                    columncount = Math.Max(columncount, row.Length);
                }
            } while(row != null);


            // ensure that each row has columncount values
            for(int i = 0; i < rows.Count; ++i) {
                ArrayList r = (ArrayList)rows[i];
                while(r.Count < columncount) {
                    r.Add(string.Empty);
                }
            }

            // set values in result object
            Hashtable result = new Hashtable(StringComparer.OrdinalIgnoreCase);
            result.Add("rowcount", rows.Count);
            result.Add("columncount", columncount);
            result.Add("values", rows);
            return result;
        }

        [DekiScriptFunction("string.trim", "Remove whitespace from the beginning and end of the string.", IsIdempotent = true)]
        public static string StringTrim(
            [DekiScriptParam("string value")] string text
        ) {
            return text.Trim();
        }

        [DekiScriptFunction("string.trimstart", "Remove whitespace from the beginning of the string.", IsIdempotent = true)]
        public static string StringTrimStart(
            [DekiScriptParam("string value")] string text
        ) {
            return text.TrimStart();
        }

        [DekiScriptFunction("string.trimend", "Remove whitespace from the end of the string.", IsIdempotent = true)]
        public static string StringTrimEnd(
            [DekiScriptParam("string value")] string text
        ) {
            return text.TrimEnd();
        }

        [DekiScriptFunction("string.contains", "Check if the first string contains the second string.", IsIdempotent = true)]
        public static bool StringContains(
            [DekiScriptParam("first string value")] string first,
            [DekiScriptParam("second string value")] string second,
            [DekiScriptParam("ignore case (default: false)", true)] bool? ignorecase
        ) {
            return StringIndexOf(first, second, ignorecase) >= 0;
        }

        [DekiScriptFunction("string.indexof", "Get index of the second string in the first string.", IsIdempotent = true)]
        public static int StringIndexOf(
            [DekiScriptParam("first string value")] string first,
            [DekiScriptParam("second string value")] string second,
            [DekiScriptParam("ignore case (default: false)", true)] bool? ignorecase
        ) {
            CultureInfo culture = GetCulture();
            return culture.CompareInfo.IndexOf(first, second, (ignorecase ?? false) ? CompareOptions.IgnoreCase : CompareOptions.None);
        }

        [DekiScriptFunction("string.lastindexof", "Get last index of the second string in the first string.", IsIdempotent = true)]
        public static int StringLastIndexOf(
            [DekiScriptParam("first string value")] string first,
            [DekiScriptParam("second string value")] string second,
            [DekiScriptParam("ignore case (default: false)", true)] bool? ignorecase
        ) {
            CultureInfo culture = GetCulture();
            return culture.CompareInfo.LastIndexOf(first, second, (ignorecase ?? false) ? CompareOptions.IgnoreCase : CompareOptions.None);
        }

        [DekiScriptFunction("string.indexesof", "Get all indexes of the second string in the first string.", IsIdempotent = true)]
        public static ArrayList StringIndexesOf(
            [DekiScriptParam("first string value")] string first,
            [DekiScriptParam("second string value")] string second,
            [DekiScriptParam("ignore case (default: false)", true)] bool? ignorecase
        ) {
            CultureInfo culture = GetCulture();
            ArrayList result = new ArrayList();
            if(!string.IsNullOrEmpty(first) && !string.IsNullOrEmpty(second)) {
                int position = 0;
                int index = culture.CompareInfo.IndexOf(first, second, position, (ignorecase ?? false) ? CompareOptions.IgnoreCase : CompareOptions.None);
                while(index >= 0) {
                    result.Add(index);
                    position = index + second.Length;
                    if(position >= first.Length) {
                        break;
                    }
                    index = first.IndexOf(second, position);
                }
            }
            return result;
        }

        [DekiScriptFunction("string.startswith", "Check if the first string starts with the second string.", IsIdempotent = true)]
        public static bool StringStartsWith(
            [DekiScriptParam("first string value")] string first,
            [DekiScriptParam("second string value")] string second,
            [DekiScriptParam("ignore case (default: false)", true)] bool? ignorecase
        ) {
            CultureInfo culture = GetCulture();
            return first.StartsWith(second, ignorecase ?? false, culture);
        }

        [DekiScriptFunction("string.endswith", "Check if the first string ends with the second string.", IsIdempotent = true)]
        public static bool StringEndsWith(
            [DekiScriptParam("first string value")] string first,
            [DekiScriptParam("second string value")] string second,
            [DekiScriptParam("ignore case (default: false)", true)] bool? ignorecase
        ) {
            CultureInfo culture = GetCulture();
            return first.EndsWith(second, ignorecase ?? false, culture);
        }

        [DekiScriptFunction("string.insert", "Insert the second string into the first string at the specified index.", IsIdempotent = true)]
        public static string StringInsert(
            [DekiScriptParam("first string value")] string first,
            [DekiScriptParam("second string value")] string second,
            [DekiScriptParam("insert position")] int index
        ) {
            return first.Insert(index, second);
        }

        [DekiScriptFunction("string.remove", "Remove characters from a string the specified index.", IsIdempotent = true)]
        public static string StringRemove(
            [DekiScriptParam("string value")] string text,
            [DekiScriptParam("index to remove characters at")] int index,
            [DekiScriptParam("number of characters to remove (default: all)", true)] int? count
        ) {
            if(count.HasValue) {
                return text.Remove(index, count.Value);
            }
            return text.Remove(index);
        }

        [DekiScriptFunction("string.eval", "Evaluate a dekiscript expression.", IsIdempotent = true)]
        public static object StringEval(
            [DekiScriptParam("dekiscript expression")] string code,
            [DekiScriptParam("ignore errors during evaluation", true)] bool? ignoreErrors,
            DekiScriptRuntime runtime
        ) {
            try {
                return runtime.Evaluate(DekiScriptParser.Parse(new Location("string.eval(code)"), code), DekiScriptEvalMode.EvaluateSafeMode, runtime.CreateEnv()).NativeValue;
            } catch(Exception e) {
                if(ignoreErrors ?? false) {
                    return e.ToString();
                } else {
                    throw;
                }
            }
        }

        [DekiScriptFunction("string.serialize", "Convert a value into a string.", IsIdempotent = true)]
        public static string StringSerialize(
            [DekiScriptParam("value", true)] object value
        ) {
            return DekiScriptLiteral.FromNativeValue(value).ToString();
        }

        [DekiScriptFunction("string.deserialize", "Convert a string into a value.", IsIdempotent = true)]
        public static object StringDeserialize(
            [DekiScriptParam("string value")] string value,
            DekiScriptRuntime runtime
        ) {
            return runtime.Evaluate(DekiScriptParser.Parse(new Location("string.deserialize(value)"), value), DekiScriptEvalMode.EvaluateSafeMode, new DekiScriptEnv()).NativeValue;
        }

        [DekiScriptFunction("string.escape", "Escape special characters in string.", IsIdempotent = true)]
        public static string StringEscape(
            [DekiScriptParam("string value")] string value
        ) {
            return value.EscapeString();
        }

        [DekiScriptFunction("string.searchescape", "Escape special characters in string.", IsIdempotent = true)]
        public static string StringSearchEscape(
            [DekiScriptParam("string value")] string value
        ) {
            if(string.IsNullOrEmpty(value)) {
                return string.Empty;
            }
            return LUCENE_ESCAPE.Replace(value, x => "\\" + x.Value);
        }

        [DekiScriptFunction("string.sqlescape", "Escape special SQL characters in string.", IsIdempotent = true)]
        public static string StringSqlEscape(
            [DekiScriptParam("string value")] string value
        ) {
            return Data.DataCommand.MakeSqlSafe(value);
        }

        [DekiScriptFunction("string.format", "Replaces placeholders with values in a string.", IsIdempotent = true)]
        public static string StringFormat(
            [DekiScriptParam("string with placeholders")] string text,
            [DekiScriptParam("values to insert into string", true)] object values
        ) {
            if(values == null) {
                return text;
            }

            // check if values is either a map or a list
            Hashtable map = values as Hashtable;
            ArrayList list = values as ArrayList;
            if((map == null) && (list == null)) {
                throw new DekiScriptBadTypeException(Location.None, DekiScriptLiteral.AsScriptType(values.GetType()), new[] { DekiScriptType.MAP, DekiScriptType.LIST });
            }

            // loop over string and fetch values from map/list
            StringBuilder result = new StringBuilder();
            int start = 0;
            int end = text.IndexOf('$');
            while(end >= 0) {

                // append characters skipped
                if(start != end) {
                    result.Append(text.Substring(start, end - start));
                }
                ++end;
                start = end;

                // check if we reached the end of the text or if the current position is followed by a '$' sign
                if((end < text.Length) && (text[end] != '$')) {

                    // parse the identifier
                    while((end < text.Length) && ((text[end] == '_') || char.IsLetterOrDigit(text[end]))) {
                        ++end;
                    }
                    string id = text.Substring(start, end - start);

                    // fetch value for identifier and insert it if found
                    if(id.Length > 0) {
                        string value = null;
                        if(map != null) {
                            value = SysUtil.ChangeType<string>(map[id]);
                        } else {
                            int index;
                            if(int.TryParse(id, out index) && (index >= 0) && (index < list.Count)) {
                                value = SysUtil.ChangeType<string>(list[index]);
                            }
                        }
                        if(!string.IsNullOrEmpty(value)) {
                            result.Append(value);
                        }
                    }
                } else {
                    result.Append("$");
                    ++end;
                }

                // find next '$' sign
                start = end;
                end = (end < text.Length) ? text.IndexOf('$', end) : -1;
            }

            // append rest of the string
            if(start < text.Length) {
                result.Append(text.Substring(start));
            }
            return result.ToString();
        }

        [DekiScriptFunction("string.hash", "Compute MD5 hash of string.", IsIdempotent = true)]
        public static string StringHash(
            [DekiScriptParam("string value")] string value
        ) {
            return StringUtil.ComputeHashString(value, Encoding.UTF8);
        }

        [DekiScriptFunction("string.cast", "Cast value to a string or nil if not possible.", IsIdempotent = true)]
        public static string StringCast(
            [DekiScriptParam("value to cast")] object value
        ) {
            if((value != null) && !(value is Hashtable) && !(value is ArrayList) && !(value is XDoc)) {
                return value.ToString();
            }
            return null;
        }

        [DekiScriptFunction("string.iscontrol", "Check if first character in string is a control character.", IsIdempotent = true)]
        public static bool StringIsControl(
            [DekiScriptParam("string value")] string value
        ) {
            return (value.Length > 0) ? char.IsControl(value[0]) : false;
        }

        [DekiScriptFunction("string.isdigit", "Check if first character in string is a decimal digit.", IsIdempotent = true)]
        public static bool StringIsDigit(
            [DekiScriptParam("string value")] string value
        ) {
            return (value.Length > 0) ? char.IsDigit(value[0]) : false;
        }

        [DekiScriptFunction("string.ishighsurrogate", "Check if first character in string is a high surrogate (UTF-32).", IsIdempotent = true)]
        public static bool StringIsHighSurrogate(
            [DekiScriptParam("string value")] string value
        ) {
            return (value.Length > 0) ? char.IsHighSurrogate(value[0]) : false;
        }

        [DekiScriptFunction("string.isletter", "Check if first character in string is an alphabetic character.", IsIdempotent = true)]
        public static bool StringIsLetter(
            [DekiScriptParam("string value")] string value
        ) {
            return (value.Length > 0) ? char.IsLetter(value[0]) : false;
        }

        [DekiScriptFunction("string.isletterordigit", "Check if first character in string is an alphabetic character or decimal digit.", IsIdempotent = true)]
        public static bool StringIsLetterOrDigit(
            [DekiScriptParam("string value")] string value
        ) {
            return (value.Length > 0) ? char.IsLetterOrDigit(value[0]) : false;
        }

        [DekiScriptFunction("string.islower", "Check if first character in string is a lowercase letter.", IsIdempotent = true)]
        public static bool StringIsLower(
            [DekiScriptParam("string value")] string value
        ) {
            return (value.Length > 0) ? char.IsLower(value[0]) : false;
        }

        [DekiScriptFunction("string.islowsurrogate", "Check if first character in string is a low surrogate (UTF-32).", IsIdempotent = true)]
        public static bool StringIsLowSurrogate(
            [DekiScriptParam("string value")] string value
        ) {
            return (value.Length > 0) ? char.IsLowSurrogate(value[0]) : false;
        }

        [DekiScriptFunction("string.isnumber", "Check if first character in string is a number.", IsIdempotent = true)]
        public static bool StringIsNumber(
            [DekiScriptParam("string value")] string value
        ) {
            return (value.Length > 0) ? char.IsNumber(value[0]) : false;
        }

        [DekiScriptFunction("string.ispunctuation", "Check if first character in string is a punctation mark.", IsIdempotent = true)]
        public static bool StringIsPunctuation(
            [DekiScriptParam("string value")] string value
        ) {
            return (value.Length > 0) ? char.IsPunctuation(value[0]) : false;
        }

        [DekiScriptFunction("string.isseparator", "Check if first character in string is a separator character.", IsIdempotent = true)]
        public static bool StringIsSeparator(
            [DekiScriptParam("string value")] string value
        ) {
            return (value.Length > 0) ? char.IsSeparator(value[0]) : false;
        }

        [DekiScriptFunction("string.issurrogate", "Check if first character in string is a surrogate character (UTF-32).", IsIdempotent = true)]
        public static bool StringIsSurrogate(
            [DekiScriptParam("string value")] string value
        ) {
            return (value.Length > 0) ? char.IsSurrogate(value[0]) : false;
        }

        [DekiScriptFunction("string.issymbol", "Check if first character in string is a symbol character.", IsIdempotent = true)]
        public static bool StringIsSymbol(
            [DekiScriptParam("string value")] string value
        ) {
            return (value.Length > 0) ? char.IsSymbol(value[0]) : false;
        }

        [DekiScriptFunction("string.isupper", "Check if first character in string is an uppercase letter.", IsIdempotent = true)]
        public static bool StringIsUpper(
            [DekiScriptParam("string value")] string value
        ) {
            return (value.Length > 0) ? char.IsUpper(value[0]) : false;
        }

        [DekiScriptFunction("string.iswhitespace", "Check if first character in string is white space.", IsIdempotent = true)]
        public static bool StringIsWhitespace(
            [DekiScriptParam("string value")] string value
        ) {
            return (value.Length > 0) ? char.IsWhiteSpace(value[0]) : false;
        }

        [DekiScriptFunction("string.nbsp", "Non-breakable space.", IsProperty = true, IsIdempotent = true)]
        public static string StringNbsp() {
            return "\u00a0";
        }
    }
}

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
using System.Text;
using System.Text.RegularExpressions;
using MindTouch.Deki.Script.Expr;
using MindTouch.Deki.Script.Runtime;
using MindTouch.Deki.Script.Runtime.Library;
using MindTouch.Tasking;
using MindTouch.Xml;

namespace MindTouch.Deki.Script.Compiler {
    public static class DekiJemProcessor {

        private enum ParseMode {
            ALL,
            STATEMENT,
            EXPRESSION
        }

        //--- Constants ---
        private const string WHEN = "when";
        private static readonly char[] NEWLINE_CHARS = new[] { '\r', '\n' };
        private static readonly Regex EVENT_PATTERN = new Regex(@"((?<sink>(\$this|#[a-zA-Z0-9_]+))\.(?<event>[a-zA-Z_][a-zA-Z_0-9]*))(?<tail>\s*(\(|\.|\[))?", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        //--- Class Methods ---
        public static string Parse(string code, string id, DekiScriptEnv env, DekiScriptRuntime runtime) {
            StringBuilder result = new StringBuilder();

            // check if code is attached to an id'ed element
            if(!string.IsNullOrEmpty(id)) {
                result.AppendLine("$(\"#" + id + "\").each(function() {");
                result.AppendLine("var $this = $(this);");
            }

            // convert JEM code into regular javascript
            int i = 0;
            result.Append(ParseExpression(code, id, ParseMode.ALL, false, env, runtime, new Dictionary<string, string>(), ref i));

            // check if code is attached to an id'ed element
            if(!string.IsNullOrEmpty(id)) {
                result.AppendLine("});");
            }
            return result.ToString();
        }

        private static bool IsAlpha(char c) {
            return ((c >= 'a') && (c <= 'z')) || ((c >= 'A') && (c <= 'Z')) || (c == '_') || (c == '$');
        }

        private static bool IsAlphaNum(char c) {
            return IsAlpha(c) || ((c >= '0') && (c <= '9'));
        }

        private static void ScanString(string code, char endChar, ref int i) {
            ++i;
            for(; (i < code.Length); ++i) {
                char c = code[i];
                if(c == '\\') {
                    ++i;
                } else if(c == endChar) {
                    ++i;
                    break;
                }
            }
        }

        private static bool TryScanComment(string code, ref int i) {
            char next = ((i + 1) < code.Length) ? code[i + 1] : (char)0;
            if(next == '/') {

                // NOTE (steveb): this is a comment line (i.e. //)
                i = code.IndexOfAny(NEWLINE_CHARS, i + 2);
                if(i < 0) {
                    i = code.Length;
                } else {
                    next = ((i + 1) < code.Length) ? code[i + 1] : (char)0;
                    if((code[i] == '\r') && (next == '\n')) {
                        ++i;
                    }
                }
                return true;
            }
            if(next == '*') {

                // NOTE (steveb): this is a comment line (i.e. /* */)
                i = code.IndexOf("*/", i + 2);
                if(i < 0) {
                    i = code.Length;
                } else {
                    i += 2;
                }
                return true;
            }
            return false;
        }

        private static void ScanId(string code, ref int i) {
            ++i;
            for(; (i < code.Length) && IsAlphaNum(code[i]); ++i) { }
        }

        private static void ScanWhitespace(string code, ref int i) {
            while((i < code.Length) && char.IsWhiteSpace(code[i])) {
                ++i;
            }
        }

        private static string ParseExpression(string code, string id, ParseMode mode, bool whenCondition, DekiScriptEnv env, DekiScriptRuntime runtime, Dictionary<string, string> channels, ref int i) {
            StringBuilder result = new StringBuilder();
            int nesting = 0;
            for(; i < code.Length; ++i) {
                int start;
                switch(code[i]) {
                case '"':
                case '\'':

                    // process strings
                    start = i;
                    ScanString(code, code[i], ref i);
                    result.Append(code, start, i - start);
                    --i;
                    break;
                case '/':

                    // check if / denotes the beginning of a comment, if so process it
                    start = i;
                    if(TryScanComment(code, ref i)) {

                        // NOTE: remove comments in when-condition

                        if(!whenCondition) {
                            result.Append(code, start, i - start);
                            result.Append("\n");
                        }
                        --i;
                    } else {
                        result.Append(code[i]);
                    }
                    break;
                case '\\':

                    // backslash (\) always appends the next character
                    result.Append(code[i++]);
                    if(i < code.Length) {
                        result.Append(code[i]);
                    }
                    break;
                case '(':

                    // increase nesting level
                    result.Append(code[i]);
                    ++nesting;
                    break;
                case '{':

                    // check if this is the beginning of a dekiscript block {{ }}
                    if(((i + 1) < code.Length) && (code[i + 1] == '{')) {
                        ++i;
                        string value;
                        start = i;
                        if(TryParseDekiScriptExpression(code, env, runtime, ref i, out value)) {
                            result.Append(value);
                        } else {
                            ++nesting;
                            result.Append('{');
                            result.Append(code, start, i - start);
                            --i;
                        }
                    } else {
                        ++nesting;
                        result.Append(code[i]);
                    }
                    break;
                case ')':
                case '}':

                    // decrease nesting level and check if this is the end of the sougth expression
                    result.Append(code[i]);
                    --nesting;

                    // NOTE: only exit if
                    // 1) we don't have to read all of the code
                    // 2) there are no open parentheses or cruly braces
                    // 3) we don't on a complete statement or the current characteris a closing curly brace

                    if((mode != ParseMode.ALL) && (nesting <= 0) && ((mode != ParseMode.STATEMENT) || (code[i] == '}'))) {

                        // found the end of the expression
                        ++i;
                        return result.ToString();
                    }
                    break;
                case ';':

                    // check if the statement is the end of the sougth expression
                    result.Append(code[i]);

                    // NOTE: only exit if
                    // 1) we don't have to read all of the code
                    // 2) there are no open parentheses or cruly braces
                    // 3) we stop on a complete statement

                    if((nesting <= 0) && (mode == ParseMode.STATEMENT)) {

                        // found the end of the expression
                        ++i;
                        return result.ToString();
                    }
                    break;
                case '@':

                    // channel name
                    if(channels != null) {
                        ++i;
                        start = i;
                        string channel;
                        string name;
                        if((i < code.Length) && ((code[i] == '"') || (code[i] == '\''))) {

                            // process: @"channel_name" or @'channel_name'
                            ScanString(code, code[i], ref i);
                            channel = code.Substring(start, i - start);
                            name = channel.Substring(1, channel.Length - 2).UnescapeString();
                        } else {

                            // process: @channel_magic_id
                            ScanId(code, ref i);
                            name = code.Substring(start, i - start);
                            if(!channels.TryGetValue(name, out channel)) {
                                channel = env.GetMagicId(name).ToString();
                            }
                        }
                        start = i;
                        ScanWhitespace(code, ref i);
                        if((i < code.Length) && (code[i] == '(')) {

                            // process: @channel ( ... )
                            string message = ParseExpression(code, id, ParseMode.EXPRESSION, false, env, runtime, channels, ref i);
                            message = message.Substring(1, message.Length - 2).Trim();
                            if(message.Length == 0) {
                                result.AppendFormat("Deki.publish({0})", channel);
                            } else {
                                result.AppendFormat("Deki.publish({0}, {1})", channel, message);
                            }
                        } else {

                            // channel is used for reading; add it to the channel set to read on activation
                            channels[name] = channel;

                            // convert channel name and add whitespace
                            result.AppendFormat("$channels[{0}]", name.QuoteString());
                            result.Append(code, start, i - start);
                        }
                        --i;
                    } else {
                        result.Append(code[i]);
                    }
                    break;
                case '#':

                    // NOTE: don't process #id in the when-condition

                    // element name
                    if(!whenCondition && (channels != null)) {
                        ++i;
                        start = i;

                        // process: #id
                        ScanId(code, ref i);
                        string name = code.Substring(start, i - start);
                        result.Append("$(\"#" + name + "\")");
                        --i;
                    } else {
                        result.Append(code[i]);
                    }
                    break;
                default:

                    // NOTE: don't process when() in the when-condition

                    // check if this is the beginning of an identifier
                    if(!whenCondition && IsAlpha(code[i])) {
                        start = i;
                        ScanId(code, ref i);
                        int j = i;
                        ScanWhitespace(code, ref j);

                        // check if scanned identifier is the keyword 'when'
                        if(((i - start) == WHEN.Length) && (string.Compare(code, start, WHEN, 0, WHEN.Length, StringComparison.Ordinal) == 0) && (j < code.Length) && (code[j] == '(')) {
                            i = j;
                            Dictionary<string, string> subChannels = new Dictionary<string, string>();

                            // parse the condition of the 'when()' statement
                            string condition = ParseExpression(code, id, ParseMode.EXPRESSION, true, env, runtime, subChannels, ref i);

                            // parse the body of the 'when()' expression
                            string body = ParseExpression(code, id, ParseMode.STATEMENT, false, env, runtime, subChannels, ref i);
                            BuildWhenStatement(condition.Trim(), id, body.Trim(), result, env, subChannels);
                        } else {
                            result.Append(code, start, i - start);
                        }
                        --i;
                    } else {
                        result.Append(code[i]);
                    }
                    break;
                }
            }
            return result.ToString();
        }

        private static bool TryParseDekiScriptExpression(string ctor, DekiScriptEnv env, DekiScriptRuntime runtime, ref int i, out string value) {
            string source = ParseExpression(ctor, null, ParseMode.EXPRESSION, false, env, runtime, null, ref i);
            if((i >= ctor.Length) || (ctor[i] != '}')) {
                value = null;
                return false;
            }

            // try to parse and execute the dekiscript fragment
            try {
                source = source.Substring(1, source.Length - 2);
                DekiScriptExpression dekiscript = DekiScriptParser.Parse(new Location("jem"),  source);
                DekiScriptLiteral result = runtime.Evaluate(dekiscript, DekiScriptEvalMode.EvaluateSafeMode, env);
                value = DekiScriptLibrary.JsonEmit(result.NativeValue);
            } catch(Exception e) {

                // execution failed; convert exception into a javascript comment
                value = "alert(\"ERROR in DekiScript expression:\\n---------------------------\\n\\n\" + " + e.GetCoroutineStackTrace().QuoteString() + ")";
            }
            return true;
        }

        private static void BuildWhenStatement(string expr, string id, string body, StringBuilder head, DekiScriptEnv env, Dictionary<string, string> channels) {

            // remove the optional outer () and {}; the expression is already scoped, so we don't need them anymore
            if(expr.StartsWith("(") && expr.EndsWith(")")) {
                expr = expr.Substring(1, expr.Length - 2).Trim();
            }

            // gather all events from 'when()' condition expression into a map { sink1: [ event1, event2, ... ], sink2: [ event1, event2, ... ], ... }
            Dictionary<string, Dictionary<string, string>> sinks = new Dictionary<string, Dictionary<string, string>>();
            string condition = EVENT_PATTERN.Replace(expr, delegate(Match match) {

                // check if a tail element was matched, which disqualifies this match
                Group tail = match.Groups["tail"];
                if(!tail.Success) {

                    // check for event match
                    Group group = match.Groups["event"];
                    if(group.Success) {
                        string sink = match.Groups["sink"].Value;
                        Dictionary<string, string> events;
                        if(!sinks.TryGetValue(sink, out events)) {
                            events = new Dictionary<string, string>();
                            sinks[sink] = events;
                        }
                        events[group.Value] = group.Value;

                        // TODO (steveb): we should also check that the event source is what we expect it to be

                        return string.Format("($event.type == {0})", StringUtil.QuoteString(group.Value));
                    }
                } else {
                    string sink = match.Groups["sink"].Value;
                    if(sink.StartsWith("#")) {
                        return string.Format("$(\"{0}\").{1}{2}", sink, match.Groups["event"].Value, tail.Value);
                    }
                }
                return match.Value;
            });

            // create stub function; $event is only set when invoked for events, not for messages
            string function = "_" + StringUtil.CreateAlphaNumericKey(8);
            if(sinks.Count > 0) {
                head.Append("var " + function + " = function($event) { $event = $event||{}; ");
            } else {
                head.Append("var " + function + " = function() { ");
            }

            // read channel state for all channels that a read from; including 'when()' condition and body
            if(channels.Count > 0) {
                bool first = true;
                head.Append("var $channels = {");
                foreach(KeyValuePair<string, string> channel in channels) {
                    if(!first) {
                        head.Append(", ");
                    }
                    first = false;
                    head.AppendFormat("{0}: Deki.query({1})||{{}}", channel.Key.QuoteString(), channel.Value);
                }
                head.Append(" }; ");
            }

            // add optional condition
            if(!string.IsNullOrEmpty(condition)) {
                head.Append("if(" + condition + ") ");
            }

            // append body of 'when()' statement and close the function
            if(string.IsNullOrEmpty(body)) {
                head.Append(";");
            } else {
                head.Append(body);
            }
            head.AppendLine(" };");

            // register function for event handlers
            if(sinks.Count > 0) {
                foreach(KeyValuePair<string, Dictionary<string, string>> events in sinks) {
                    string bind = string.Join(" ", new List<string>(events.Value.Values).ToArray()).QuoteString();
                    if(events.Key.EqualsInvariant("$this")) {
                        head.AppendLine("$this.bind(" + bind + ", " + function + ");");
                    } else {
                        head.AppendLine("$(" + events.Key.QuoteString() + ").bind(" + bind + ", " + function + ");");
                    }
                }
            }

            // register function for message handlers
            foreach(KeyValuePair<string, string> channel in channels) {
                head.AppendLine("Deki.subscribe(" + channel.Value + ", " + (string.IsNullOrEmpty(id) ? "null" : "this") + ", " + function + ");");
            }
        }
    }
}
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
using MindTouch.Deki.Script.Expr;

namespace MindTouch.Deki.Script.Runtime {
    internal static class DekiScriptOutputBufferEx {

        //--- Extension Methods ---
        public static StringBuilder AppendLiteral(this StringBuilder builder, DekiScriptLiteral literal) {
            if(literal is DekiScriptString) {
                builder.Append(((DekiScriptString)literal).Value);
            } else {
                builder.Append(literal.ToString());
            }
            return builder;
        }
    }

    internal class DekiScriptOutputBuffer {

        //--- Types ---
        internal struct Range {

            //--- Class Fields ---
            public static readonly Range Empty = new Range(0, 0);

            //--- Fields ---
            public readonly int Start;
            public readonly int End;

            //--- Constructors ---
            public Range(int start, int end) {
                this.Start = start;
                this.End = end;
            }

            //--- Properties ---
            public int Count { get { return End - Start; } }
            public bool IsEmpty { get { return Start == End; } }
        }

        internal class XmlStart {

            //--- Fields ---
            public readonly string Prefix;
            public readonly string Name;
            public readonly Dictionary<string, string> Namespaces;
            public readonly List<Tuplet<string/*Prefix*/, string/*Name*/, string/*Value*/>> Attributes;

            //--- Constructors ---
            public XmlStart(string prefix, string name, Dictionary<string, string> namespaces, List<Tuplet<string, string, string>> attributes) {
                this.Prefix = prefix;
                this.Name = name;
                this.Namespaces = namespaces;
                this.Attributes = attributes;
            }

            //--- Methods ---
            public override string ToString() {
                var result = new StringBuilder();
                result.Append("<");
                result.Append(Name);
                if((Attributes != null) && (Attributes.Count > 0)) {
                    foreach(var attribute in Attributes) {
                        result.Append(" ");
                        result.Append(attribute.Item2);
                        result.Append("=");
                        result.Append(attribute.Item3.QuoteString());
                    }
                }
                result.Append(">");
                return result.ToString();
            }
        }

        internal class XmlEnd {

            //--- Class Fields ---
            public static readonly XmlEnd Value = new XmlEnd();

            //--- Constructors ---
            private XmlEnd() { }

            //--- Methods ---
            public override string ToString() {
                return "</>";
            }
        }

        //--- Fields ---
        private readonly List<object> _buffer = new List<object>();
        private readonly int _limit;

        //--- Constructors ---
        public DekiScriptOutputBuffer(int limit) {
            _limit = limit;
        }

        //--- Properties ---
        public object this[int index] { get { return _buffer[index]; } }
        public int Marker { get { return _buffer.Count; } }

        //--- Methods ---
        public DekiScriptLiteral Pop(Range range, bool safe) {

            // check if range is empty
            if(range.IsEmpty) {
                return DekiScriptNil.Value;
            }

            // make sure we're not trying to process elements in the middle of the buffer
            if(range.End != _buffer.Count) {
                throw new ArgumentException("range is not valid");
            }

            // optimize for the common case where we need the last added item
            if(range.Count == 1) {
                var result = (DekiScriptLiteral)_buffer[range.Start];
                _buffer.RemoveAt(range.Start);
                return result;
            }

            // process contents of output buffer
            return new DekiScriptOutputProcessor().Process(this, range.Start, safe);
        }

        public Range Since(int marker) {
            return new Range(marker, _buffer.Count);
        }

        public void Reset(int marker) {
            _buffer.RemoveRange(marker, _buffer.Count - marker);
        }

        public Range Push(DekiScriptLiteral literal) {
            if(literal.IsNil) {
                return Range.Empty;
            }

            // append element
            int start = _buffer.Count;
            CheckBufferLimits();
            _buffer.Add(literal);
            return new Range(start, _buffer.Count);
        }

        internal void PushXmlStart(string prefix, string name, Dictionary<string, string> namespaces, List<Tuplet<string, string, string>> attributes) {
            CheckBufferLimits();
            _buffer.Add(new XmlStart(prefix, name, namespaces, attributes));
        }

        internal void PushXmlEnd() {
            CheckBufferLimits();
            _buffer.Add(XmlEnd.Value);
        }

        private void CheckBufferLimits() {
            if(_buffer.Count >= _limit) {
                throw new DekiScriptDocumentTooLargeException(_buffer.Count);
            }
        }
    }
}
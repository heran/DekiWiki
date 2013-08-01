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
using System.Text;
using System.IO;
using System.Collections;

namespace MindTouch.Deki.Script {

    // based on http://knab.ws/blog/index.php?/archives/10-CSV-file-parser-and-writer-in-C-Part-2.html
    internal class CsvStream {

        //--- Fields ---
        private readonly TextReader _stream;
        private bool _endOfStream;
        private bool _endOfLine;
        private readonly char[] _buffer = new char[4096];
        private int _pos;
        private int _length;

        //--- Constructors ---
        public CsvStream(TextReader stream) {
            if(stream == null) {
                throw new NullReferenceException("stream");
            }
            _stream = stream;
        }

        //--- Methods ---
        public string[] GetNextRow() {
            var row = new ArrayList();
            while(true) {
                var item = GetNextItem();
                if(item == null) {
                    return row.Count == 0 ? null : (string[])row.ToArray(typeof(string));
                }
                row.Add(item);
            }
        }

        private string GetNextItem() {
            if(_endOfLine) {

                // previous item was last in line, start new line
                _endOfLine = false;
                return null;
            }

            bool quoted = false;
            bool predata = true;
            bool postdata = false;
            var item = new StringBuilder();
            while(true) {
                var c = GetNextChar(true);
                if(_endOfStream) {
                    return item.Length > 0 ? item.ToString() : null;
                }
                if((postdata || !quoted) && c == ',') {

                    // end of item, return
                    return item.ToString();
                }
                if((predata || postdata || !quoted) && (c == '\n' || c == '\r')) {

                    // we are at the end of the line, eat newline characters and exit
                    _endOfLine = true;
                    if(c == '\r' && GetNextChar(false) == '\n') {
                        GetNextChar(true);
                    }
                    return item.ToString();
                }
                if(predata && c == ' ') {

                    // whitespace preceeding data, discard
                    continue;
                }
                if(predata && c == '"') {

                    // quoted data is starting
                    quoted = true;
                    predata = false;
                    continue;
                }
                if(predata) {

                    // data is starting without quotes
                    predata = false;
                    item.Append(c);
                    continue;
                }
                if(c == '"' && quoted) {
                    if(GetNextChar(false) == '"') {

                        // double quotes within quoted string means add a quote       
                        item.Append(GetNextChar(true));
                    } else {

                        // end-quote reached
                        postdata = true;
                    }
                    continue;
                }

                // all cases covered, character must be data
                item.Append(c);
            }
        }

        private char GetNextChar(bool eat) {
            if(_pos >= _length) {
                _length = _stream.ReadBlock(_buffer, 0, _buffer.Length);
                if(_length == 0) {
                    _endOfStream = true;
                    return '\0';
                }
                _pos = 0;
            }
            if(eat) {
                return _buffer[_pos++];
            }
            return _buffer[_pos];
        }
    }
}

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
using System.IO;
using System.Text;
using System.Xml;
using MindTouch.IO;

namespace MindTouch.Deki.Script {
    public class XmlNodePlainTextReadonlyByteStream : Stream {

        //--- Fields ---
        private readonly IEnumerator<string> _cursor;
        private string _text;
        private int _textPosition;
        private byte[] _internalBuffer;
        private int _position;

        //--- Constructors ---
        public XmlNodePlainTextReadonlyByteStream(XmlNode element) {
            _cursor = FindText(element).GetEnumerator();

            // prime the buffer
            FillByteBuffer();
        }

        //--- Methods ---
        private bool FillByteBuffer() {
            while(true) {
                if(!CheckForTextToConvert()) {
                    return false;
                }
                int charCount;
                var chars = GetCharacterBufferFromText(out charCount);
                if(chars == null) {
                    continue;
                }
                _internalBuffer = Encoding.UTF8.GetBytes(chars, 0, charCount);
                _position = 0;
                return true;
            }
        }

        private bool CheckForTextToConvert() {
            while(_text == null || _textPosition == _text.Length) {
                if(!_cursor.MoveNext()) {
                    return false;
                }
                _text = _cursor.Current;
                if(string.IsNullOrEmpty(_text)) {
                    _text = null;
                    continue;
                }
                _textPosition = 0;
            }
            return true;
        }

        private char[] GetCharacterBufferFromText(out int charCount) {
            charCount = 0;
            if(_internalBuffer == null) {
                SeekPastLeadingWhiteSpace();
                SeekPastLeadingPattern();
                SeekPastLeadingWhiteSpace();
            }
            if(_text.Length == _textPosition) {
                return null;
            }
            var maxChars = Math.Min(_text.Length - _textPosition, StreamUtil.BUFFER_SIZE / sizeof(char));
            var chars = new char[maxChars];
            for(; _textPosition < _text.Length && charCount < maxChars; _textPosition++) {
                var c = _text[_textPosition];
                switch(c) {
                case '\u00A0':

                    // non-breaking whitespace becomes whitespace
                    c = ' ';
                    break;
                case '\u00AD':

                    // soft-hypens are removed
                    continue;
                }
                chars[charCount++] = c;
            }
            return charCount == 0 ? null : chars;
        }

        private void SeekPastLeadingWhiteSpace() {
            for(; _textPosition < _text.Length; _textPosition++) {
                var c = _text[_textPosition];
                if(c == '\u00A0' || c == ' ' || char.IsWhiteSpace(c)) {
                    continue;
                }
                return;
            }
        }

        private void SeekPastLeadingPattern() {
            foreach(var pattern in new[] { DekiScriptRuntime.ON_SAVE_PATTERN, DekiScriptRuntime.ON_SUBST_PATTERN, DekiScriptRuntime.ON_EDIT_PATTERN }) {
                if(_text.Length - _textPosition < pattern.Length || !pattern.EqualsInvariantIgnoreCase(_text.Substring(_textPosition, pattern.Length))) {
                    continue;
                }
                _textPosition += pattern.Length;
                return;
            }
        }

        public override int Read(byte[] buffer, int offset, int count) {
            if(offset < 0 || count < 0 || offset + count > buffer.Length) {
                throw new ArgumentException("offset or count are nor valid for provided buffer");
            }
            var read = 0;
            while(count > 0) {
                if((_internalBuffer == null) || (_position == _internalBuffer.Length)) {
                    if(!FillByteBuffer()) {
                        return read;
                    }
                }
                var copyCount = Math.Min(count, _internalBuffer.Length - _position);
                Array.Copy(_internalBuffer, _position, buffer, offset, copyCount);
                read += copyCount;
                _position += copyCount;
                count -= copyCount;
                offset += copyCount;
            }
            return read;
        }

        private IEnumerable<string> FindText(XmlNode parent) {
            XmlNode textNode = null;
            switch(parent.NodeType) {
            case XmlNodeType.Whitespace:
            case XmlNodeType.SignificantWhitespace:
            case XmlNodeType.Text:
            case XmlNodeType.CDATA:
                textNode = parent;
                break;
            }
            if(textNode != null) {
                if(!string.IsNullOrEmpty(textNode.Value)) {
                    yield return textNode.Value;
                }
                yield break;
            }
            foreach(XmlNode node in parent) {
                foreach(var child in FindText(node)) {
                    yield return child;
                }
            }
        }

        //--- Unimplemented Stream Properties
        public override bool CanRead { get { return true; } }
        public override bool CanSeek { get { return false; } }
        public override bool CanWrite { get { return false; } }
        public override long Length { get { return -1; } }

        public override long Position {
            get { throw new InvalidOperationException(); }
            set { throw new InvalidOperationException(); }
        }

        //--- Unimplemented Stream Methods

        public override void Flush() { }

        public override long Seek(long offset, SeekOrigin origin) {
            throw new InvalidOperationException();
        }

        public override void SetLength(long value) {
            throw new InvalidOperationException();
        }

        public override void Write(byte[] buffer, int offset, int count) {
            throw new InvalidOperationException();
        }
    }
}

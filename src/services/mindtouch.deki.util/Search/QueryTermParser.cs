/*
 * MindTouch Core - open source enterprise collaborative networking
 * Copyright (c) 2006-2010 MindTouch Inc.
 * www.mindtouch.com  oss@mindtouch.com
 *
 * For community documentation and downloads visit developer.mindtouch.com;
 * please review the licensing section.
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License along
 * with this program; if not, write to the Free Software Foundation, Inc.,
 * 59 Temple Place - Suite 330, Boston, MA 02111-1307, USA.
 * http://www.gnu.org/copyleft/gpl.html
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MindTouch.Deki.Search {
    internal class QueryTermParser {

        //--- Class Fields ---
        private static readonly HashSet<string> _knownFields = new HashSet<string> {
            "author",
            "comments",
            "content",
            "description",
            "filename",
            "id.file",
            "id.page",
            "id.parent",
            "namespace",
            "path",
            "path.parent",
            "path.title",
            "property",
            "rating.count",
            "rating.score",
            "tag",
            "title",
            "title.page",
            "title.parent",
            "type",
        };
        private static readonly HashSet<string> _reservedWords = new HashSet<string>() {
            "AND",
            "NOT",
            "OR"
        };

        //--- Fields ---
        private readonly HashSet<QueryTerm> _terms = new HashSet<QueryTerm>();
        private readonly StringBuilder _escapedTerm = new StringBuilder();
        private readonly StringBuilder _normalizedTerm = new StringBuilder();
        private string _escapedField;
        private string _normalizedField;
        private bool _abortOnLuceneConstruct;
        private bool _inQuote;
        private bool _escaping;
        private char _leadingOperator;

        //--- Properties ---
        private bool IsTermStart { get { return _escapedTerm.Length == 0; } }
        private bool HasFieldPrefix { get { return !string.IsNullOrEmpty(_escapedField); } }

        //--- Methods ---

        // Note (arnec): this call is not threadsafe, since QueryTermParser keeps state in the instance, which is why
        // it is not used by itself but embedded in SearchQueryParser
        public IEnumerable<QueryTerm> GetQueryTerms(string rawQuery, bool abortOnLuceneConstruct) {
            _abortOnLuceneConstruct = abortOnLuceneConstruct;
            _inQuote = _escaping = false;
            _escapedTerm.Length = 0;
            _normalizedTerm.Length = 0;
            _leadingOperator = char.MinValue;
            _terms.Clear();
            for(var i = 0; i < rawQuery.Length; i++) {
                var c = rawQuery[i];
                if(_escaping) {
                    _escaping = false;
                } else {
                    switch(c) {

                    // quote
                    case '"':
                        if(!_inQuote) {
                            if(IsTermStart) {

                                // start of quoted term
                                _inQuote = true;
                                continue;
                            }

                            // quote in middle of unquoted term, throw
                            throw new FormatException(string.Format("Quote in middle of unquoted term: {0}", _escapedTerm));
                        }
                        if(i < rawQuery.Length - 1) {
                            i++;
                            var c2 = rawQuery[i];
                            if(!char.IsWhiteSpace(c2)) {
                                if(_abortOnLuceneConstruct) {

                                    // might be legal in lucene, i.e. term followed by range, boost, etc.
                                    return null;
                                }

                                // badly ended quoted term
                                throw new FormatException(string.Format("Unescaped quote in middle of quoted term: \"{0}", _escapedTerm));
                            }
                        }
                        if(!BuildTerm()) {
                            return null;
                        }
                        continue;

                    // possible inclusion or exclusion operator
                    case '+':
                    case '-':
                        if(_inQuote) {
                            EscapeCharacter();
                            break;
                        }
                        if(_leadingOperator == char.MinValue && IsTermStart && !HasFieldPrefix) {

                            // capture operator
                            _leadingOperator = c;
                            continue;
                        }
                        EscapeCharacter();
                        break;

                    // special characters
                    case '!':
                    case '(':
                    case ')':
                    case '{':
                    case '}':
                    case '[':
                    case ']':
                    case '^':
                    case '~':
                        if(!_inQuote && abortOnLuceneConstruct) {
                            return null;
                        }
                        EscapeCharacter();
                        break;

                    // wild cards
                    case '*':
                    case '?':
                        if(_inQuote) {
                            EscapeCharacter();
                        }
                        break;

                    // possible field separator
                    case ':':
                        if(_inQuote) {
                            EscapeCharacter();
                        } else if(HasFieldPrefix) {
                            EscapeCharacter();
                        } else if(IsTermStart) {
                            if(abortOnLuceneConstruct) {
                                return null;
                            }
                            EscapeCharacter();
                        } else {
                            _escapedField = _escapedTerm.ToString();
                            _normalizedField = _normalizedTerm.ToString();
                            if(_knownFields.Contains(_escapedField) || _escapedField.Contains("#")) {
                                _escapedTerm.Length = 0;
                                _normalizedTerm.Length = 0;
                                continue;
                            }
                            _escapedField = _normalizedField = null;
                            EscapeCharacter();
                        }
                        break;

                    // possible escaped sequence
                    case '\\':
                        _escapedTerm.Append('\\');
                        _escaping = true;
                        continue;

                    // everything else
                    default:
                        if(!_inQuote && char.IsWhiteSpace(c)) {
                            if(!BuildTerm()) {
                                return null;
                            }
                            continue;
                        }
                        if(_inQuote && char.IsWhiteSpace(c)) {
                            EscapeCharacter();
                        }

                        break;
                    }
                }
                _escapedTerm.Append(c);
                _normalizedTerm.Append(c);
            }
            if(_inQuote) {
                throw new FormatException(string.Format("Unclosed quote on quoted term term: \"{0}", _escapedTerm));
            }
            return !BuildTerm() ? null : _terms.ToArray();
        }

        private void EscapeCharacter() {
            _escapedTerm.Append('\\');
        }

        private bool BuildTerm() {
            if(_escapedTerm.Length != 0) {
                if(_escaping) {
                    throw new FormatException(string.Format("Incomplete escape sequence at end of term: {0}", _escapedTerm));
                }
                var escaped = HasFieldPrefix ? _escapedField + ":" + _escapedTerm : _escapedTerm.ToString();
                var normalized = HasFieldPrefix ? _normalizedField + ":" + _normalizedTerm : _normalizedTerm.ToString();
                if(!_inQuote && _reservedWords.Contains(escaped)) {
                    if(_abortOnLuceneConstruct) {
                        return false;
                    }
                    escaped = "\"" + escaped + "\"";
                }
                switch(_leadingOperator) {
                case '-':
                    escaped = "-" + escaped;
                    normalized = "-" + normalized;
                    break;
                case '+':
                    escaped = "+" + escaped;
                    break;
                }
                _terms.Add(new QueryTerm(escaped, normalized, HasFieldPrefix, _inQuote));
            }
            _escapedField = _normalizedField = null;
            _normalizedTerm.Length = 0;
            _escapedTerm.Length = 0;
            _inQuote = false;
            _leadingOperator = char.MinValue;
            return true;
        }
    }
}
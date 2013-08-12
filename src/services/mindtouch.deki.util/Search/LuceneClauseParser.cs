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
using System.Text;

namespace MindTouch.Deki.Search {
    public class LuceneClauseParser {

        //--- Types ---
        public class Clauses {

            //--- Fields ---
            public readonly IEnumerable<string> Unprefixed;
            public readonly IEnumerable<string> PlusPrefixed;
            public readonly IEnumerable<string> MinusPrefixed;

            //--- Constructors ---
            public Clauses(IEnumerable<string> unprefixed, IEnumerable<string> plusPrefixed, IEnumerable<string> minusPrefixed) {
                Unprefixed = unprefixed;
                PlusPrefixed = plusPrefixed;
                MinusPrefixed = minusPrefixed;
            }
        }

        //--- Class Fields ---
        private static readonly HashSet<string> _reservedWords = new HashSet<string>() {
            "AND",
            "NOT",
            "OR"
        };

        //--- Class Methods ---
        public static Clauses Parse(string clause) {
            var c = new LuceneClauseParser(clause);
            c.Parse();
            return new Clauses(c._unprefixed, c._plusPrefixed, c._minusPrefixed);
        }

        //--- Fields ---
        private readonly List<string> _unprefixed = new List<string>();
        private readonly List<string> _plusPrefixed = new List<string>();
        private readonly List<string> _minusPrefixed = new List<string>();
        private readonly string _clause;
        private readonly StringBuilder _current = new StringBuilder();
        private int _parentheseCount;
        private char _prefix = char.MinValue;

        //--- Constructors ---
        private LuceneClauseParser(string clause) {
            _clause = clause;
        }

        //--- Properties ---
        private bool IsTermStart { get { return _current.Length == 0; } }

        //--- Methods ---
        private void Parse() {
            try {
                for(var i = 0; i < _clause.Length; i++) {
                    var c = _clause[i];
                    switch(c) {

                    // quote
                    case '"':
                        ParseQuotedString(ref i);
                        continue;

                    // escape
                    case '\\':
                        ParseEscapeSequence(ref i);
                        continue;

                    // open parentheses
                    case '(':
                        _parentheseCount++;
                        break;

                    // close parentheses
                    case ')':
                        _parentheseCount--;
                        if(_parentheseCount < 0) {
                            throw new Exception();
                        }
                        break;

                    // possible inclusion or exclusion operator
                    case '+':
                    case '-':
                        if(IsTermStart) {
                            _prefix = c;
                        }
                        break;

                    // everything else
                    default:
                        if(_parentheseCount == 0 && char.IsWhiteSpace(c)) {
                            CaptureTerm();
                            continue;
                        }
                        break;
                    }
                    _current.Append(c);
                }
                if(_parentheseCount != 0) {
                    throw new Exception();
                }
                CaptureTerm();
            } catch {

                // something unparsable, treat it all as one block
                _unprefixed.Clear();
                _plusPrefixed.Clear();
                _minusPrefixed.Clear();
                _unprefixed.Add("(" + _clause + ")");
            }
        }

        private void ParseEscapeSequence(ref int i) {
            if(i + 1 == _clause.Length) {

                // we've run out of characters, bail
                throw new Exception();
            }
            _current.Append(_clause[i]);
            i++;
            _current.Append(_clause[i]);
        }

        private void ParseQuotedString(ref int i) {
            var c = _clause[i];
            _current.Append(c);
            i++;
            for(; i < _clause.Length; i++) {
                c = _clause[i];
                if(c == '"') {
                    _current.Append(c);
                    return;
                }
                if(c == '\\') {
                    ParseEscapeSequence(ref i);
                    continue;
                }
                _current.Append(c);
            }

            // end of clause before end of quote
            throw new Exception();
        }

        private void CaptureTerm() {
            if(_current.Length > 0) {
                var term = _current.ToString();
                _current.Length = 0;
                if(_reservedWords.Contains(term)) {
                    throw new Exception();
                }
                switch(_prefix) {
                case '+':
                    _plusPrefixed.Add(term);
                    break;
                case '-':
                    _minusPrefixed.Add(term);
                    break;
                default:
                    _unprefixed.Add(term);
                    break;
                }
                _prefix = char.MinValue;
            }
        }
    }
}
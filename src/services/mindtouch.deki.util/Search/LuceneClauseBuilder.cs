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
    public class LuceneClauseBuilder {

        //--- Fields ---
        private readonly StringBuilder _builder = new StringBuilder();

        //--- Properties ---
        public bool IsEmpty { get { return _builder.Length == 0; } }
        public string Clause { get { return _builder.ToString(); } }

        //--- Methods ---
        public void And(string constraint) {
            if(string.IsNullOrEmpty(constraint)) {
                return;
            }
            var clauses = LuceneClauseParser.Parse(constraint);
            var unprefixedClause = new StringBuilder();
            var clauseCount = 0;
            foreach(var unprefixed in clauses.Unprefixed) {
                if(clauseCount > 0) {
                    unprefixedClause.Append(" ");
                }
                unprefixedClause.Append(unprefixed);
                clauseCount++;
            }
            if(clauseCount > 0) {
                if(clauseCount > 1) {
                    PadAndAppend("+(" + unprefixedClause + ")");
                } else {
                    PadAndAppend("+" + unprefixedClause);
                }
            }
            AppendClauses(clauses.PlusPrefixed);
            AppendClauses(clauses.MinusPrefixed);
        }

        private void AppendClauses(IEnumerable<string> clauses) {
            foreach(var clause in clauses) {
                PadAndAppend(clause);
            }
        }

        private void PadAndAppend(string clause) {
            if(_builder.Length > 0) {
                _builder.Append(" ");
            }
            _builder.Append(clause);
        }
    }
}

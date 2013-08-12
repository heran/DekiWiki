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
using System.IO;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;

namespace MindTouch.LuceneService {

    public class SearchFilter {
        private string _fileName;
        private string _arguments;

        public SearchFilter(string fileName, string arguments) {
            _fileName = fileName;
            _arguments = arguments;
        }
        public string FileName { get { return _fileName; } }
        public string Arguments { get { return _arguments; } }
    }

    public class PropertyAnalyzer : Analyzer {
        public override TokenStream TokenStream(string fieldName, TextReader reader) {
            return new LowerCaseFilter(new PropertyTokenizer(reader));
        }
    }

    /// <summary>
    /// This analyzer is case-insensitive and treats the input as a single term.
    /// </summary>
    public class UntokenizedAnalyzer : Analyzer {
        public override TokenStream TokenStream(string fieldName, TextReader reader) {
            return new LowerCaseFilter(new UnTokenizer(reader));
        }
    }

    public class UnTokenizer : CharTokenizer {
        public UnTokenizer(System.IO.TextReader reader)
            : base(reader) {
        }

        protected override bool IsTokenChar(char c) {
            return true;
        }
    }

    /// <summary>
    /// This analyzer behaves simliar to <see cref="UntokenizedAnalyzer"/> but also treats whitespace, dashes and underscores as the same character.
    /// </summary>
    public class FilenameAnalyzer : Analyzer {
        public override TokenStream TokenStream(string fieldName, TextReader reader) {
            return new LowerCaseFilter(new FilenameTokenizer(reader));
        }
    }

    public class FilenameTokenizer : UnTokenizer {
        public FilenameTokenizer(TextReader input)
            : base(input) {
        }

        protected override char Normalize(char c) {
            if(char.IsWhiteSpace(c) || c == '-') {
                return '_';
            }
            return base.Normalize(c);
        }
    }

    // A tokenizer used for properties which splits tokens at whitespace
    // or a comma
    public class PropertyTokenizer : CharTokenizer {
        public PropertyTokenizer(System.IO.TextReader reader)
            : base(reader) {
        }

        protected override bool IsTokenChar(char c) {
            return c != ',' && !System.Char.IsWhiteSpace(c);
        }
    }

    public class LetterDigitTokenizer : CharTokenizer {
        /// <summary>Construct a new LetterTokenizer. </summary>
        public LetterDigitTokenizer(System.IO.TextReader reader)
            : base(reader) {
        }

        protected override bool IsTokenChar(char c) {
            return System.Char.IsLetterOrDigit(c);
        }
    }
    /// <summary>A PathAnalyzer is an Analyzer that handles path segnments in URI's properly.  We don't want
    /// paths to be tokenized but we can't use KeyWordAnalyzer/Tokenizer since wildcard searches don't work with them.
    /// </summary>
    public class PathAnalyzer : Analyzer {
        public override TokenStream TokenStream(System.String fieldName, System.IO.TextReader reader) {
            return new LowerCaseFilter(new PathTokenizer(reader));
        }
    }
    /// <summary>A PathTokenizer is a tokenizer that handles path segnments in URI's properly.  We don't want
    /// paths to be tokenized but we can't use KeyWordAnalyzer/Tokenizer since wildcard searches don't work with them.
    /// </summary>
    public class PathTokenizer : CharTokenizer {
        public PathTokenizer(System.IO.TextReader reader)
            : base(reader) {
        }

        protected override bool IsTokenChar(char c) {
            return (System.Char.IsLetterOrDigit(c) || System.Char.IsPunctuation(c) || System.Char.IsSeparator(c));
        }
    }
    /// <summary>TagAnalyzer/TagTokenizer breaks tokens at line feeds since tags are delimited by '\n'
    /// </summary>
    public class TagAnalyzer : Analyzer {
        public override TokenStream TokenStream(string fieldName, TextReader reader) {
            return new LowerCaseFilter(new TagTokenizer(reader));
        }
    }
    public class TagTokenizer : CharTokenizer {
        public TagTokenizer(System.IO.TextReader reader)
            : base(reader) {
        }
        protected override bool IsTokenChar(char c) {
            return c != '\n';
        }
    }
    public class LowerCaseKeywordAnalyzer : Analyzer {
        public override TokenStream TokenStream(System.String fieldName, System.IO.TextReader reader) {
            return new LowerCaseFilter(new KeywordTokenizer(reader));
        }
    }
    public class TitleAnalyzer : Analyzer {
        //--- Methods ---
        public override TokenStream TokenStream(string fieldName, TextReader reader) {
            return new PorterStemFilter(new LowerCaseFilter(new LetterDigitTokenizer(reader)));
        }
    }
    public class EnglishAnalyzer : Analyzer {
        //--- Methods ---
        public override TokenStream TokenStream(string fieldName, TextReader reader) {
            return new PorterStemFilter(new LowerCaseFilter(new StandardTokenizer(reader)));
        }
    }
    public class PerFieldAnalyzer : PerFieldAnalyzerWrapper {
        //--- Constructors ---
        public PerFieldAnalyzer()
            : base(new PropertyAnalyzer()) {
            base.AddAnalyzer("mime", new LowerCaseKeywordAnalyzer());
            base.AddAnalyzer("date.edited", new LowerCaseKeywordAnalyzer());
            base.AddAnalyzer("content", new EnglishAnalyzer());
            base.AddAnalyzer("content.edit", new EnglishAnalyzer());
            base.AddAnalyzer("preview", new EnglishAnalyzer());
            base.AddAnalyzer("author", new EnglishAnalyzer());
            base.AddAnalyzer("description", new EnglishAnalyzer());
            base.AddAnalyzer("comments", new EnglishAnalyzer());
            base.AddAnalyzer("title", new TitleAnalyzer());
            base.AddAnalyzer("path.title", new TitleAnalyzer());
            base.AddAnalyzer("path", new PathAnalyzer());
            base.AddAnalyzer("uri", new LowerCaseKeywordAnalyzer());
            base.AddAnalyzer("tag", new TagAnalyzer());
            base.AddAnalyzer("type", new LowerCaseKeywordAnalyzer());
            base.AddAnalyzer("namespace", new LowerCaseKeywordAnalyzer());
            base.AddAnalyzer("language", new KeywordAnalyzer());
            base.AddAnalyzer("extension", new UntokenizedAnalyzer());

            // Note (arnec): can't use FilenameAnalyzer because wildcard queries do no use the tokenizer
            base.AddAnalyzer("filename", new UntokenizedAnalyzer());
        }
    }
}

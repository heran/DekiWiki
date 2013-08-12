/*
 * MindTouch DekiScript - embeddable web-oriented scripting runtime
 * Copyright (c) 2006-2010 MindTouch Inc.
 * www.mindtouch.com  oss@mindtouch.com
 *
 * For community documentation and downloads visit developer.mindtouch.com;
 * please review the licensing section.
 *
 * This library is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 2.1 of the License, or (at your option) any later version.
 * 
 * This library is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 * 
 * You should have received a copy of the GNU Lesser General Public
 * License along with this library; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
 * http://www.gnu.org/copyleft/lesser.html
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using MindTouch.Deki.Script.Runtime.Library;
using MindTouch.Deki.Script.Expr;
using MindTouch.Deki.Script.Tests.Util;
using NUnit.Framework;

namespace MindTouch.Deki.Script.Tests.ScriptTests {

    [TestFixture]
    public class DekiScriptLibraryScriptTests {

        //--- Fields ---
        private DekiScriptTester _t;

        [SetUp]
        public void Setup() {
            _t = new DekiScriptTester();
        }

        [Test]
        public void Cast() {
            _t.Test(
                @"String.cast(42);",
                @"42",
                typeof(DekiScriptString)
            );
        }

        [Test]
        public void Cast_NonCastableType() {
            _t.Test(
                @"String.cast([ 1, 2, 3 ]);",
                @"nil",
                typeof(DekiScriptNil)
            );
        }

        [Test]
        public void Compare_LessThanZero() {
            _t.Test(
                @"String.compare(""a"", ""b"");",
                @"-1",
                typeof(DekiScriptNumber)
            );
        }

        [Test]
        public void Compare_EqualsZero_IgnoreCase() {
            _t.Test(
                @"String.compare(""c"", ""C"", true);",
                @"0",
                typeof(DekiScriptNumber)
            );
        }

        [Test]
        public void Compare_GreaterThanZero() {
            _t.Test(
                @"String.compare(""D"", ""d"");",
                @"1",
                typeof(DekiScriptNumber)
            );
        }

        [Test]
        public void Contains() {
            _t.Test(
                @"String.contains(""I was born to lead, not to read."", ""LEAD"");",
                @"false",
                typeof(DekiScriptBool)
            );
        }

        [Test]
        public void Contains_IgnoreCase() {
            _t.Test(
                @"String.contains(""I was born to lead, not to read."", ""READ"", true);",
                @"true",
                typeof(DekiScriptBool)
            );
        }

        [Test]
        public void Deserialize_ToNumber() {
            _t.Test(
                @"String.deserialize(""42"");",
                @"42",
                typeof(DekiScriptNumber)
            );
        }

        [Test]
        public void Deserialize_ToList() {
            _t.Test(
                @"String.deserialize(""[ 1, true, 'three' ]"");",
                @"[ 1, True, ""three"" ]",
                typeof(DekiScriptList)
            );
        }

        [Test]
        public void EndsWith() {
            _t.Test(
                @"String.endswith(""I was elected to lead, not to read."", ""NOT TO READ."");",
                @"false",
                typeof(DekiScriptBool)
            );
        }

        [Test]
        public void EndsWith_IgnoreCase() {
            _t.Test(
                @"String.endswith(""I was elected to lead, not to read."", ""NOT TO READ."", true);",
                @"true",
                typeof(DekiScriptBool)
            );
        }

        [Test]
        public void Equals() {
            _t.Test(
                @"String.equals(""I was elected to lead, not to read."", ""I was elected to lead, NOT TO READ."");",
                @"false",
                typeof(DekiScriptBool)
            );
        }

        [Test]
        public void Equals_IgnoreCase() {
            _t.Test(
                @"String.equals(""I was elected to lead, not to read."", ""I was elected to lead, NOT TO READ."", true);",
                @"true",
                typeof(DekiScriptBool)
            );
        }

        [Test]
        public void Escape() {
            _t.Test(
                @"String.escape(""'doh'"");",
                @"\'doh\'",
                typeof(DekiScriptString)
            );
        }

        [Test]
        public void Eval() {
            _t.Test(
                @"String.eval(""1 + 5 - 2 * 9 / 3"");",
                @"0",
                typeof(DekiScriptNumber)
            );
        }
        
        [Test]
        [ExpectedException(typeof(Runtime.DekiScriptInvokeException))]
        public void Eval_BadExpression() {
            _t.Test(
                @"String.eval(""1 + 5 - 2 * 9 / 3 + "");",
                @"--should never happen--",
                typeof(DekiScriptNil)
            );
        }

        [Test]
        public void Eval_BadExpression_IgnoreError() {
            _t.Test(
                // Long exception string. If it contains the exception, then it's all good.
                @"String.contains(String.eval(""1 + 5 - 2 * 9 / 3 + "", true), ""MindTouch.Deki.Script.Compiler.DekiScriptParserException"");",
                @"true",
                typeof(DekiScriptBool)
            );
        }

        [Test]
        public void Format_List() {
            _t.Test(
                @"String.Format(""$0 $1 $2 $3 $4 $5 some text"", [ ""hey"", 2, 7.7, true ])",
                @"hey 2 7.7 True   some text",
                typeof(DekiScriptString)
            );
        }

        [Test]
        public void Format_Map() {
            _t.Test(
                @"String.Format(""$0 $1 $0"", { 0:""b"", 1:true, 2:7 })",
                @"b True b",
                typeof(DekiScriptString)
            );
        }

        [Test]
        public void Format_NoValues() {
            _t.Test(
                @"String.Format(""$0 $1 hey"", _)",
                @"$0 $1 hey",
                typeof(DekiScriptString)
            );
        }

        [Test]
        public void Format_TextHasDollarSign() {
            _t.Test(
                @"String.Format(""$0 $1 gimme tha $$$$$$"", [ 1 , 2 ])",
                @"1 2 gimme tha $$$",
                typeof(DekiScriptString)
            );
        }

        [Test]
        [ExpectedException(typeof(Runtime.DekiScriptInvokeException))]
        public void Format_ValuesNotInListOrMap() {
            _t.Test(
                @"String.Format(""$0 $1 hey"", ""test"")",
                @"--should never happen--",
                typeof(DekiScriptString)
            );
        }


        [Test]
        public void Hash() {
            _t.Test(
                @"String.hash(""melder"");",
                @"0a74c3381f19f3b4f726e7e2bc3bcc70",
                typeof(DekiScriptString)
            );
        }

        [Test]
        public void ICompare() { // obsolete
            _t.Test(
                @"String.icompare(""c"", ""C"");",
                @"0",
                typeof(DekiScriptNumber)
            );
        }

        [Test]
        public void IndexesOf() {
            _t.Test(
                @"String.indexesof(""I was elected to lead, not to read."", "" "");",
                @"[ 1, 5, 13, 16, 22, 26, 29 ]",
                typeof(DekiScriptList)
            );
        }

        [Test]
        public void IndexOf() {
            _t.Test(
                @"String.indexof(""I was elected to lead, not to read."", "" "");",
                @"1",
                typeof(DekiScriptNumber)
            );
        }

        [Test]
        public void Insert() {
            _t.Test(
                @"String.insert(""I was elected to read."", ""to lead, not "", 14);",
                @"I was elected to lead, not to read.",
                typeof(DekiScriptString)
            );
        }

        [Test]
        public void IsControl() {
            _t.Test(
                @"String.iscontrol(""\r"");",
                @"true",
                typeof(DekiScriptBool)
            );
        }

        [Test]
        public void IsDigit() {
            _t.Test(
                @"String.isdigit(""3.14"");",
                @"true",
                typeof(DekiScriptBool)
            );
        }

        [Test]
        public void IsHighSurrogate() {
            _t.Test(
                @"String.ishighsurrogate(""\uD801abcdefg"");",
                @"true",
                typeof(DekiScriptBool)
            );
        }

        [Test]
        public void IsLetter() {
            _t.Test(
                @"String.isletter(""3.14"");",
                @"false",
                typeof(DekiScriptBool)
            );
        }

        [Test]
        public void IsLetterOrDigit() {
            _t.Test(
                @"String.isletterordigit(""3.14"");",
                @"true",
                typeof(DekiScriptBool)
            );
        }

        [Test]
        public void IsLowSurrogate() {
            _t.Test(
                @"String.islowsurrogate(""\uDCCCabcdefg"");",
                @"true",
                typeof(DekiScriptBool)
            );
        }

        [Test]
        public void IsLower() {
            _t.Test(
                @"String.islower(""I was elected to lead, not to read."");",
                @"false",
                typeof(DekiScriptBool)
            );
        }

        [Test]
        public void IsNumber() {
            _t.Test(
                @"String.isnumber(""I was elected to lead, not to read."");",
                @"false",
                typeof(DekiScriptBool)
            );
        }

        [Test]
        public void IsPunctuation() {
            _t.Test(
                @"String.ispunctuation("",14"");",
                @"true",
                typeof(DekiScriptBool)
            );
        }

        [Test]
        public void IsSeparator() {
            _t.Test(
                @"String.isseparator(""\ 14"");",
                @"true",
                typeof(DekiScriptBool)
            );
        }

        [Test]
        public void IsSurrogate() {
            _t.Test(
                @"String.issurrogate(""3.14"");",
                @"false",
                typeof(DekiScriptBool)
            );
        }

        [Test]
        public void IsSymbol() {
            _t.Test(
                @"String.issymbol(""<14"");",
                @"true",
                typeof(DekiScriptBool)
            );
        }

        [Test]
        public void IsUpper() {
            _t.Test(
                @"String.isupper(""I was elected to lead, not to read."");",
                @"true",
                typeof(DekiScriptBool)
            );
        }

        [Test]
        public void IsWhitespace() {
            _t.Test(
                @"String.iswhitespace(""   ... doh"");",
                @"true",
                typeof(DekiScriptBool)
            );
        }

        [Test]
        public void Join() {
            _t.Test(
                @"String.join([ ""I"", ""was"", ""elected"", ""to"", ""lead,"", ""not"", ""to"", ""read."" ], "" "");",
                @"I was elected to lead, not to read.",
                typeof(DekiScriptString)
            );
        }

        [Test]
        public void LastIndexOf() {
            _t.Test(
                @"String.lastindexof(""I was elected to lead, not to read."", "" "");",
                @"29",
                typeof(DekiScriptNumber)
            );
        }

        [Test]
        public void Length() {
            _t.Test(
                @"String.length(""I was elected to lead, not to read."");",
                @"35",
                typeof(DekiScriptNumber)
            );
        }

        [Test] 
        public void Match_Success() {
            _t.Test(
                @"String.match(""acobjai3189764fjkasja13470831kaslndlancl"", ""([0-9]+)"" );",
                @"3189764",
                typeof(DekiScriptString)
            );
        }

        [Test]
        public void Match_NoMatches() {
            _t.Test(
                @"String.match(""acobjai3189764fjkasja13470831kaslndlancl"", ""(z)+"" );",
                @"nil",
                typeof(DekiScriptNil)
            );
        }

        [Test]
        public void Matches_SingleCaptureGroup() {
            _t.Test(
                @"String.matches(""acobjai3189764fjkasja13470831kaslndlancl"", ""([0-9])+"" );",
                @"[ ""3189764"", ""13470831"" ]",
                typeof(DekiScriptList)
            );
        }

        [Test]
        public void Matches_MultipleCaptureGroups() {
            _t.Test(
                @"String.matches(""acobjai3189764fjkasja13470831kaslndlancl"", ""([a-z]+([0-9]+))"" );",
                @"[ ""acobjai3189764"", ""fjkasja13470831"" ]",
                typeof(DekiScriptList)
            );
        }

        [Test]
        public void Matches_NoMatches() {
            _t.Test(
                @"String.matches(""acobjai3189764fjkasja13470831kaslndlancl"", ""(z)+"" );",
                @"[]",
                typeof(DekiScriptList)
            );
        }

        [Test]
        public void Nbsp() {
            _t.Test(
                @"String.nbsp;",
                @" ",
                typeof(DekiScriptString)
            );
        }

        [Test]
        public void Padleft() {
            _t.Test(
                @"String.padleft(""abcd"", 10, ""*"" );",
                @"******abcd",
                typeof(DekiScriptString)
            );
        }

        [Test]
        public void PadRight() {
            _t.Test(
                @"String.padright(""abcd"", 10, ""*"" );",
                @"abcd******",
                typeof(DekiScriptString)
            );
        }

        [Test]
        public void Quote() {
            _t.Test(
                @"String.quote(""I was elected to lead, not to read."");",
                @"""I was elected to lead, not to read.""",
                typeof(DekiScriptString)
            );
        }

        [Test]
        public void Remove() {
            _t.Test(
                @"String.remove(""I was elected to lead, not to read."", 13, 13);",
                @"I was elected to read.",
                typeof(DekiScriptString)
            );
        }

        [Test]
        public void Remove_All() {
            _t.Test(
                @"String.remove(""I was elected to lead, not to read."", 13);",
                @"I was elected",
                typeof(DekiScriptString)
            );
        }

        [Test]
        public void Replace() {
            _t.Test(
                @"String.replace(""I was elected to lead."", ""lead"", ""read"");",
                @"I was elected to read.",
                typeof(DekiScriptString)
            );
        }

        [Test]
        public void Replace_EmptyStrings() {
            _t.Test(
                @"String.replace(""I was elected to lead."", """", """");",
                @"I was elected to lead.",
                typeof(DekiScriptString)
            );
        }

        [Test]
        public void Replace_IgnoreCase() {
            _t.Test(
                @"String.replace(""I was elected to lead."", ""LEAD"", ""read"", true);",
                @"I was elected to read.",
                typeof(DekiScriptString)
            );
        }

        [Test]
        public void SearchEscape() {
            _t.Test(
                @"String.searchescape(""- + ! ( ) { } [ ] ^ ~ ? *"");",
                @"\- \+ \! \( \) \{ \} \[ \] \^ \~ \? \*",
                typeof(DekiScriptString)
            );
        }

        [Test]
        public void SearchEscape_EmptyString() {
            _t.Test(
                @"String.searchescape("""");",
                @"",
                typeof(DekiScriptString)
            );
        }

        [Test]
        public void Serialize() {
            _t.Test(
                @"String.serialize(""3.14"");",
                @"""3.14""",
                typeof(DekiScriptString)
            );
        }

        [Test]
        public void Split() {
            _t.Test(
                @"String.split(""I was elected to lead."", "" "", 4);",
                @"[ ""I"", ""was"", ""elected"", ""to lead."" ]",
                typeof(DekiScriptList)
            );
        }

        [Test]
        public void SplitCSV() {
            _t.Test(
                @"String.splitcsv(""I, was, elected, to lead.\n Not, to, read."");",
                @"{ columncount : 4, rowcount : 2, values : [ [ ""I"", ""was"", ""elected"", ""to lead."" ], [ ""Not"", ""to"", ""read."", """" ] ] }",
                typeof(DekiScriptMap)
            );
        }

        [Test]
        public void SplitCSV_WithQuote() {
            _t.Test(
                @"String.splitcsv(""I, was, elected, \""\""\""\"", to lead."");",
                @"{ columncount : 5, rowcount : 1, values : [ [ ""I"", ""was"", ""elected"", ""\"""", ""to lead."" ] ] }",
                typeof(DekiScriptMap)
            );
        }

        [Test]
        public void SplitCSV_CommaInQuotes() {
            _t.Test(
                @"String.splitcsv(""I, was, elected, \""to, lead.\"""");",
                @"{ columncount : 4, rowcount : 1, values : [ [ ""I"", ""was"", ""elected"", ""to, lead."" ] ] }",
                typeof(DekiScriptMap)
            );
        }

        [Test]
        public void SplitCSV_ItemsWithNewLines() {
            _t.Test(
                @"String.splitcsv(""I, was, elected,\""to,\nlead.\"""");",
                @"{ columncount : 4, rowcount : 1, values : [ [ ""I"", ""was"", ""elected"", ""to,\nlead."" ] ] }",
                typeof(DekiScriptMap)
            );
        }

        [Test]
        public void SplitCSV_ItemsWithNewLines2() {
            _t.Test(
                @"String.splitcsv(""I, was, elected,\""to,\nlead.\""\nNot, to, read."");",
                @"{ columncount : 4, rowcount : 2, values : [ [ ""I"", ""was"", ""elected"", ""to,\nlead."" ], [ ""Not"", ""to"", ""read."", """" ] ] }",
                typeof(DekiScriptMap)
            );
        }

        [Test]
        public void SQLEscape() {
            _t.Test(
                @"String.sqlescape(""' \"""");",
                @"\' \""",
                typeof(DekiScriptString)
            );
        }

        [Test]
        public void StartsWith() {
            _t.Test(
                @"String.startswith(""I was elected to lead, not to read."", ""I was elected"");",
                @"true",
                typeof(DekiScriptBool)
            );
        }

        [Test]
        public void SubStr() {
            _t.Test(
                @"String.substr(""I was elected to lead, not to read."", 0, 21);",
                @"I was elected to lead",
                typeof(DekiScriptString)
            );
        }

        [Test]
        public void SubStr_NegativeParameters() {
            _t.Test(
                @"String.substr(""I was elected to lead, not to read."", -12, -1);",
                @"not to read",
                typeof(DekiScriptString)
            );
        }

        [Test]
        public void ToCamelCase() {
            _t.Test(
                @"String.tocamelcase(""I was elected to lead, not to read."");",
                @"I Was Elected To Lead, Not To Read.",
                typeof(DekiScriptString)
            );
        }

        [Test]
        public void ToLower() {
            _t.Test(
                @"String.tolower(""I Was electEd tO leaD, not To rEad."");",
                @"i was elected to lead, not to read.",
                typeof(DekiScriptString)
            );
        }

        [Test]
        public void ToLowerFirst() {
            _t.Test(
                @"String.tolowerfirst(""I Was electEd tO leaD, not To rEad."");",
                @"i Was electEd tO leaD, not To rEad.",
                typeof(DekiScriptString)
            );
        }

        [Test]
        public void ToLowerFirst_EmptyString() {
            _t.Test(
                @"String.tolowerfirst("""");",
                @"",
                typeof(DekiScriptString)
            );
        }

        [Test]
        public void ToUpper() {
            _t.Test(
                @"String.toupper(""I Was electEd tO leaD, not To rEad."");",
                @"I WAS ELECTED TO LEAD, NOT TO READ.",
                typeof(DekiScriptString)
            );
        }

        [Test]
        public void ToUpperFirst() {
            _t.Test(
                @"String.toupperfirst(""I Was electEd tO leaD, not To rEad."");",
                @"I Was electEd tO leaD, not To rEad.",
                typeof(DekiScriptString)
            );
        }

        [Test]
        public void ToUpperFirst_EmptyString() {
            _t.Test(
                @"String.toupperfirst("""");",
                @"",
                typeof(DekiScriptString)
            );
        }

        [Test]
        public void Trim() {
            _t.Test(
                @"String.trim(""       I        Was electEd   tO  leaD, not To     rEad.   "");",
                @"I        Was electEd   tO  leaD, not To     rEad.",
                typeof(DekiScriptString)
            );
        }

        [Test]
        public void TrimEnd() {
            _t.Test(
                @"String.trimend(""       I        Was electEd   tO  leaD, not To     rEad.   "");",
                @"       I        Was electEd   tO  leaD, not To     rEad.",
                typeof(DekiScriptString)
            );
        }

        [Test]
        public void TrimStart() {
            _t.Test(
                @"String.trimstart(""       I        Was electEd   tO  leaD, not To     rEad.   "");",
                @"I        Was electEd   tO  leaD, not To     rEad.   ",
                typeof(DekiScriptString)
            );
        }
    }
}
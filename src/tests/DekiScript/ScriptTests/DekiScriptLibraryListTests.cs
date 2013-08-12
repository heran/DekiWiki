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
using MindTouch.Deki.Script.Expr;
using MindTouch.Deki.Script.Runtime.TargetInvocation;
using MindTouch.Deki.Script.Tests.Util;
using MindTouch.Dream.Test;
using NUnit.Framework;


namespace MindTouch.Deki.Script.Tests.ScriptTests {
    
    [TestFixture]
    public class DekiScriptLibraryListTests {

        //--- Fields ---
        private DekiScriptTester _t;

        [SetUp]
        public void Setup() {
            _t = new DekiScriptTester();
        }
        
        [Test]
        public void Can_call_ListApply() {
            _t.Test(
                "list.apply([1,2,3,4],\"$+$\")",
                "[ 2, 4, 6, 8 ]",
                typeof(DekiScriptList));
        }

        [Test]
        public void Can_call_ListApply_with_custom_function() {
            _t.Runtime.RegisterFunction("test.double", GetType().GetMethod("TestDouble"), new[] {
                new DekiScriptNativeInvocationTarget.Parameter("i",false),
            });
            _t.Test(
                "list.apply([1,2,3,4],\"test.double($)\")",
                "[ 2, 4, 6, 8 ]",
                typeof(DekiScriptList));
        }

        [Test]
        public void Can_call_ListSort() {
            _t.Test(
                "list.sort{ list: ['2009-12-01','2009-11-01', '2009-11-02', '2009-11-03'], compare: \"date.compare($left, $right)\" };",
                "[ \"2009-11-01\", \"2009-11-02\", \"2009-11-03\", \"2009-12-01\" ]",
                typeof(DekiScriptList));
        }

        [Test]
        public void Can_call_ListSort_with_custom_function() {
            _t.Runtime.RegisterFunction("test.compare", GetType().GetMethod("TestCompare"), new[] {
                new DekiScriptNativeInvocationTarget.Parameter("left",false),
                new DekiScriptNativeInvocationTarget.Parameter("right",false),
            });
           _t.Test(
                "list.sort{list: [1,2,3,4], compare: \"test.compare($left, $right)\"};",
                "[ 4, 3, 2, 1 ]",
                typeof(DekiScriptList));
            
        }

        [Test]
        public void Apply() {
            _t.Test(
                @"var x = [ 1, 2, 3.54 ]; list.apply(x, ""$+5"");",
                @"[ 6, 7, 8.54 ]",
                typeof(DekiScriptList)
            );
        }

        [Test]
        [ExpectedException(typeof(Runtime.DekiScriptInvokeException))]
        public void Apply_BadExpression() {
            _t.Test(
                @"var x = [ 1, 2, 3.54 ]; list.apply(x, ""$foo"");",
                @"--should never happen--",
                typeof(DekiScriptList)
            );
        }

        [Test]
        public void Apply_NonNumericList() {
            _t.Test(
                @"var x = [ 1, ""two"", [ 3, ""four""], false, true ]; list.apply(x, ""$+2"");",
                @"[ 3, nil, nil, 2, 3 ]",
                typeof(DekiScriptList)
            );
        }

        [Test]
        public void Average() {
            _t.Test(
                @"var x = [ 1, ""two"", [ 3, ""four""], 1.5, 4.5, 4 ]; list.average(x);",
                @"2.75",
                typeof(DekiScriptNumber)
            );
        }

        [Test]
        public void Collect() {
            _t.Test(
                @"var x = [ 1, ""two"", { ""a"": 1 }, 3.5, { ""b"": 2 }, { ""a"": 1 }, { ""A"": 17 } ]; list.collect(x, 'a');",
                @"[ 1, 1, 17 ]",
                typeof(DekiScriptList)
            );
        }

        [Test]
        public void Combine() {
            _t.Test(
                @"var x = [ 1, 2, 4.5, 700, 4.3, ""foo"", true, [ 5, 6, 7 ], { 1:2 } ]; " +
                @"var y = [ true, false, ""A"", [ ""b"", ""c"", ""d"" ], { 'e':'f', 'g':'h' }, 1, ""good"", ""bad"", ""bad"" ]; " +
                @"list.combine(x,y);",
                @"{ 1 : True, 2 : False, 4.3 : { e : ""f"", g : ""h"" }, 4.5 : ""A"", 700 : [ ""b"", ""c"", ""d"" ], foo : 1, True : ""good"" }",
                typeof(DekiScriptMap)
            );
        }

        [Test]
        public void Contains_True() {
            _t.Test(
                @"var x = [ 1, ""two"", 3.5 ]; list.contains(x, ""two"");",
                @"true",
                typeof(DekiScriptBool)
            );
        }

        [Test]
        public void Contains_False() {
            _t.Test(
                @"var x = [ 1, ""two"", 3.5 ]; list.contains(x, ""TWO"");",
                @"false",
                typeof(DekiScriptBool)
            );
        }

        [Test]
        public void Contains_StringIgnoreCase_True() {
            _t.Test(
                @"var x = [ 1, ""two"", 3.5 ]; list.contains(x, ""TWO"", true);",
                @"true",
                typeof(DekiScriptBool)
            );
        }

        [Test]
        public void Contains_StringIgnoreCase_False() {
            _t.Test(
                @"var x = [ 1, ""two"", 3.5 ]; list.contains(x, ""deux"", true);",
                @"false",
                typeof(DekiScriptBool)
            );
        }

        [Test]
        public void GroupBy() {
            _t.Test(
                @"var x = [ { true:""x"" }, { false:""y"" }, { true:1, false:2 } ]; list.groupby(x, ""$true"");",
                @"{ 1 : [ { false : 2, true : 1 } ], x : [ { true : ""x"" } ] }",
                typeof(DekiScriptMap)
            );
        }

        [Test]
        public void IndexesOf() {
            _t.Test(
                @"var x = [ 1, ""two"", { 3:""d"" }, [ 5, ""f"" ], false, 7.7, 1, 7.7, [ 5, ""f"" ] ]; list.indexesof(x, 7.7);",
                @"[ 5, 7 ]",
                typeof(DekiScriptList)
            );
        }

        [Test]
        public void IndexOf() {
            _t.Test(
                @"var x = [ 1, ""two"", { 3:""d"" }, [ 5, ""f"" ], false, 7.7, 1, 7.7, [ 5, ""f"" ] ]; list.indexof(x, 1);",
                @"0",
                typeof(DekiScriptNumber)
            );
        }

        [Test]
        public void LastIndexOf() {
            _t.Test(
                @"var x = [ 1, ""two"", { 3:""d"" }, [ 5, ""f"" ], false, 7.7, 1, 7.7, [ 5, ""f"" ] ]; list.lastindexof(x, 1);",
                @"6",
                typeof(DekiScriptNumber)
            );
        }

        [Test]
        public void Max() {
            _t.Test(
                @"var x = [ 1, ""two"", { 3:""d"" }, [ 5, ""f"" ], false, 7.7, 1, 7.7, [ 5, ""f"" ] ]; list.max(x);",
                @"7.7",
                typeof(DekiScriptNumber)
            );
        }

        [Test]
        public void Min() {
            _t.Test(
                @"var x = [ 1, ""two"", { 3:""d"" }, [ 5, ""f"" ], 7.7, 1, 7.7, [ 5, ""f"" ] ]; list.min(x);",
                @"1",
                typeof(DekiScriptNumber)
            );
        }

        [Test]
        public void Intersect() {
            _t.Test(
                @"var x = [ 1, 2, 4.5, 700, 4.3, ""foo"", true, [ 5, 6, 7 ], { 1:2 } ]; " +
                @"var y = [ 2, 4.5, ""foo"", false, [ 5, 6, 7 ] ]; " +
                @"list.intersect(x,y);",
                @"[ 2, 4.5, ""foo"" ]",
                typeof(DekiScriptList)
            );
        }

        [Test]
        public void Intersect_WithCondition() {
            _t.Test(
                @"var x = [ 1, 2, 4.5, 700, 4.3, ""foo"", [ 5, 6, 7 ], { 1:2 } ]; " +
                @"var y = [ 1, 2, 4, ""foo"", false, [ 5, 6, 7 ] ]; " +
                @"list.intersect(x,y, ""$left<3"");",
                @"[ 1, 2 ]",
                typeof(DekiScriptList)
            );
        }

        [Test]
        public void New_Empty() {
            _t.Test(
                @"list.new(5);",
                @"[ nil, nil, nil, nil, nil ]",
                typeof(DekiScriptList)
            );
        }

        [Test]
        public void New_WithDefault() {
            _t.Test(
                @"list.new(3, 7.89);",
                @"[ 7.89, 7.89, 7.89 ]",
                typeof(DekiScriptList)
            );
        }

        [Test]
        public void New_BadNum() {
            _t.Test(
                @"list.new(-5);",
                @"nil",
                typeof(DekiScriptNil)
            );
        }

        [Test]
        [ExpectedException(typeof(Runtime.DekiScriptInvokeException))]
        public void New_NonNumeric() {
            _t.Test(
                @"list.new(""x"");",
                @"--should never happen--",
                typeof(DekiScriptList)
            );
        }

        [Test]
        public void Orderby_Ascending() {
            _t.Test(
                @"var x = [ {Name: ""Foo"", Price: 5}, {Name: ""Bar"", Price: 7}, {Name: ""Zoop"", Price: 6} ];" +
                @"list.orderby(x, ""Price ascending"");",
                @"[ { Name : ""Foo"", Price : 5 }, { Name : ""Zoop"", Price : 6 }, { Name : ""Bar"", Price : 7 } ]",
                typeof(DekiScriptList)
            );
        }

        [Test]
        public void Orderby_Descending() {
            _t.Test(
                @"var x = [ {Name: ""Foo"", Price: 5}, {Name: ""Bar"", Price: 7}, {Name: ""Zoop"", Price: 6} ];" +
                @"list.orderby(x, ""Name descending"");",
                @"[ { Name : ""Zoop"", Price : 6 }, { Name : ""Foo"", Price : 5 }, { Name : ""Bar"", Price : 7 } ]",
                typeof(DekiScriptList)
            );
        }

        [Test]
        public void Orderby_Ascending_MultipleKeys() {
            _t.Test(
                @"var x = [ {y:5, z:7}, {x:8, y:2, z:9}, {y:6, z:3} ];" +
                @"list.orderby(x, [ ""x descending"", ""y"" ]);",
                @"[ { x : 8, y : 2, z : 9 }, { y : 5, z : 7 }, { y : 6, z : 3 } ]",
                typeof(DekiScriptList)
            );
        }

        [Test] // TODO (melder): have key w/ access operator affect search result
        public void Orderby_Ascending_WithAccessOperator() {
            _t.Test(
                @"var x = [ {y:5, z:7}, {x:8, y:2, z:9}, {y:6, z:3} ];" +
                @"list.orderby(x, ""$.y"");",
                @"[ { y : 6, z : 3 }, { x : 8, y : 2, z : 9 }, { y : 5, z : 7 } ]",
                typeof(DekiScriptList)
            );
        }

        [Test]
        public void Random_EmptyList() {
            _t.Test(
                @"list.random([]);",
                @"nil",
                typeof(DekiScriptNil)
            );
        }

        [Test]
        public void Random_Number() {
            _t.Test(
                @"list.random([ 42 ]);",
                @"42",
                typeof(DekiScriptNumber)
            );
        }

        [Test]
        public void Random_Text() {
            _t.Test(
                @"list.random([ ""foo"" ]);",
                @"foo",
                typeof(DekiScriptString)
            );
        }

        [Test]
        public void Reduce() {
            _t.Test(
                @"list.reduce([ 1, 2, 3], ""$value + $item"", 0);",
                @"6",
                typeof(DekiScriptNumber)
            );
        }

        [Test]
        public void Reverse() {
            _t.Test(
                @"list.reverse([ 1, 2, 4.5, 700, 4.3, ""foo"", true, [ 5, 6, 7 ], { 1:2 }]);",
                @"[ { 1 : 2 }, [ 5, 6, 7 ], True, ""foo"", 4.3, 700, 4.5, 2, 1 ]",
                typeof(DekiScriptList)
            );
        }

        [Test]
        public void Select() {
            _t.Test(
                @"list.select([ 1, 2, 4.5, 700, 4.3, ""foo"", true, [ 5, 6, 7 ], { 1:2 }], ""$%2==0"");",
                @"[ 2, 700 ]",
                typeof(DekiScriptList)
            );
        }

        [Test]
        public void Sort_WithComparer() {
            // descending comparer
            string comparer = "\"if ($left > $right) -1; else if ($left == $right) 0; else 1;\"";
            _t.Test(
                @"list.sort([ 1, 2, 4.5, 700, 4.3 ], _, _, " + comparer + ");",
                @"[ 700, 4.5, 4.3, 2, 1 ]",
                typeof(DekiScriptList)
            );
        }

        [Test]
        public void Splice() {
            _t.Test(
                @"list.splice([ 1, 2, 4.5, 700, 4.3, ""foo"", true, [ 5, 6, 7 ], { 1:2 }], 2, 4, [ 8, 16, 32 ]);",
                @"[ 1, 2, 8, 16, 32, True, [ 5, 6, 7 ], { 1 : 2 } ]",
                typeof(DekiScriptList)
            );
        }

        [Test]
        public void Splice_NegativeOffset() {
            _t.Test(
                @"list.splice([ 1, 2, 4.5, 700, 4.3, ""foo"", true, [ 5, 6, 7 ], { 1:2 }], -4, 2, [ 8, 16, 32 ]);",
                @"[ 1, 2, 4.5, 700, 4.3, 8, 16, 32, [ 5, 6, 7 ], { 1 : 2 } ]",
                typeof(DekiScriptList)
            );
        }

        [Test]
        public void Splice_NegativeLength() {
            _t.Test(
                @"list.splice([ 1, 2, 4.5, 700, 4.3, ""foo"", true, [ 5, 6, 7 ], { 1:2 }], 2, -1, [ 8, 16, 32 ]);",
                @"[ 1, 2, 8, 16, 32, { 1 : 2 } ]",
                typeof(DekiScriptList)
            );
        }

        [Test]
        public void Sum() {
            _t.Test(
                @"list.Sum([ 1, 2, 2.5, 3.5, [ ""a"" ], ""two"", ""2"", { 4:5 } ]);",
                @"11",
                typeof(DekiScriptNumber)
            );
        }
        
        public static int TestDouble(int i) {
            return i*2;
        }

        public static int TestCompare(int left, int right) {
            return right.CompareTo(left);
        }
    }
}

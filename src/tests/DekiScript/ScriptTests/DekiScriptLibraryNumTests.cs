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
using MindTouch.Deki.Script.Expr;
using MindTouch.Deki.Script.Tests.Util;
using NUnit.Framework;

namespace MindTouch.Deki.Script.Tests.ScriptTests {

    [TestFixture]
    public class DekiScriptLibraryNumTests {

        //--- Fields ---
        private DekiScriptTester _t;

        [SetUp]
        public void Setup() {
            _t = new DekiScriptTester();
        }

        [Test]
        public void Abs() {
            _t.Test(
                @"Num.abs(-42);",
                @"42",
                typeof(DekiScriptNumber)
            );
        }

        [Test]
        public void Acos() {
            _t.Test(
                @"Num.acos(0);",
                (Math.PI / 2).ToString(),
                typeof(DekiScriptNumber)
            );
        }

        [Test]
        public void Asin() {
            _t.Test(
                @"Num.asin(1);",
                (Math.PI/2).ToString(),
                typeof(DekiScriptNumber)
            );
        }

        [Test]
        public void Atan() {
            _t.Test(
                @"Num.atan(1);",
                (Math.PI / 4).ToString(),
                typeof(DekiScriptNumber)
            );
        }

        [Test]
        public void Atan2() {
            _t.Test(
                @"Num.atan2(1, 1);",
                ( Math.PI / 4).ToString(),
                typeof(DekiScriptNumber)
            );
        }

        [Test]
        public void Cast_StringToNum() {
            _t.Test(
                @"Num.cast(""1234"");",
                "1234",
                typeof(DekiScriptNumber)
            );
        }

        [Test]
        public void Cast_BadType() {
            _t.Test(
                @"Num.cast([ 1, 2, 3 ]);",
                "nil",
                typeof(DekiScriptNil)
            );
        }

        [Test]
        public void Ceiling() {
            _t.Test(
                @"Num.ceiling(12.345);",
                "13",
                typeof(DekiScriptNumber)
            );
        }

        [Test]
        public void Cos() {
            _t.Test(
                @"Num.cos(0);",
                "1",
                typeof(DekiScriptNumber)
            );
        }

        [Test]
        public void CosH() {
            _t.Test(
                @"Num.cosh(0);",
                "1",
                typeof(DekiScriptNumber)
            );
        }

        [Test]
        public void E() {
            _t.Test(
                @"Num.e;",
                "2.71828182845905",
                typeof(DekiScriptNumber)
            );
        }

        [Test]
        public void Epsilon() {
            _t.Test(
                @"Num.epsilon;",
                "4.94065645841247E-324",
                typeof(DekiScriptNumber)
            );
        }

        [Test]
        public void Exp() {
            _t.Test(
                @"Num.exp(10)",
                (Math.Pow(Math.E, 10)).ToString(),
                typeof(DekiScriptNumber)
            );
        }

        [Test]
        public void Floor() {
            _t.Test(
                @"Num.floor(12.345);",
                "12",
                typeof(DekiScriptNumber)
            );
        }

        [Test]
        public void Format() {
            _t.Test(
                @"Num.format(1234567890, ""#### #### ##"");",
                "1234 5678 90",
                typeof(DekiScriptString)
            );
        }

        [Test]
        public void Int() {
            _t.Test(
                @"Num.int(3.14159);",
                "3",
                typeof(DekiScriptNumber)
            );
        } 

        [Test]
        public void IsInfinity() {
            _t.Test(
                @"num.isinfinity(1/0)",
                "true",
                typeof(DekiScriptBool)
            );
        }

        [Test]
        public void IsNaN() {
            _t.Test(
                @"num.isnan(num.acos(3.14))",
                "true",
                typeof(DekiScriptBool)
            );
        }

        [Test]
        public void IsNegativeInfinity() {
            _t.Test(
                @"num.isnegativeinfinity(-1/0)",
                "true",
                typeof(DekiScriptBool)
            );
        }

        [Test]
        public void IsPositiveInfinity() {
            _t.Test(
                @"num.ispositiveinfinity(1/0)",
                "true",
                typeof(DekiScriptBool)
            );
        }

        [Test]
        public void Log() {
            _t.Test(
                @"num.log(1000, 10)",
                "3",
                typeof(DekiScriptNumber)
            );
        }

        [Test]
        public void Max() {
            _t.Test(
                @"num.max(1000, 10)",
                "1000",
                typeof(DekiScriptNumber)
            );
        }

        [Test]
        public void Min() {
            _t.Test(
                @"num.min(1000, 10)",
                "10",
                typeof(DekiScriptNumber)
            );
        }

        [Test]
        public void NaN() {
            _t.Test(
                @"num.nan",
                "NaN",
                typeof(DekiScriptNumber)
            );
        }

        [Test]
        public void NegativeInfinity() {
            _t.Test(
                @"num.negativeinfinity",
                "-Infinity",
                typeof(DekiScriptNumber)
            );
        }

        [Test]
        public void Pi() {
            _t.Test(
                @"num.pi",
                Math.PI.ToString(),
                typeof(DekiScriptNumber)
            );
        }

        [Test]
        public void PositiveInfinity() {
            _t.Test(
                @"num.positiveinfinity",
                "Infinity",
                typeof(DekiScriptNumber)
            );
        }

        [Test]
        public void Pow() {
            _t.Test(
                @"num.pow(256, 0.25)",
                "4",
                typeof(DekiScriptNumber)
            );
        }

        [Test]
        public void Random() { // Not sure how to test this
            _t.Test(
                @"var x = num.random(); num.sign(x+1);",
                "1",
                typeof(DekiScriptNumber)
            );
        }


        [Test]
        public void Round() {
            _t.Test(
                @"num.round(123.14159, -1)",
                "120",
                typeof(DekiScriptNumber)
            );
        }

        [Test]
        public void Round_Decimal() {
            _t.Test(
                @"num.round(3.14159, 2)",
                "3.14",
                typeof(DekiScriptNumber)
            );
        }

        [Test]
        public void Series() {
            _t.Test(
                @"num.series(1, 8, 2)",
                "[ 1, 3, 5, 7 ]",
                typeof(DekiScriptList)
            );
        }

        [Test]
        public void Series_ZeroStep() {
            _t.Test(
                @"num.series(1, 8, 0)",
                "[]",
                typeof(DekiScriptList)
            );
        }

        [Test]
        public void Series_BadDirection() {
            _t.Test(
                @"num.series(1, 8, -2)",
                "[]",
                typeof(DekiScriptList)
            );
        }

        [Test]
        public void Sign() {
            _t.Test(
                @"num.sign(-314)",
                "-1",
                typeof(DekiScriptNumber)
            );
        }

        [Test]
        public void Sin() {
            _t.Test(
                @"num.sin(num.pi*3/2)",
                "-1",
                typeof(DekiScriptNumber)
            );
        }

        [Test]
        public void Sinh() {
            _t.Test(
                @"num.sinh(0)",
                "0",
                typeof(DekiScriptNumber)
            );
        }

        [Test]
        public void Sqrt() {
            _t.Test(
                @"num.sqrt(100)",
                "10",
                typeof(DekiScriptNumber)
            );
        }

        [Test]
        public void Tan() {
            _t.Test(
                @"num.tan(num.pi*3/4)",
                "-1",
                typeof(DekiScriptNumber)
            );
        }

        [Test]
        public void Tanh() {
            _t.Test(
                @"num.tanh(0)",
                "0",
                typeof(DekiScriptNumber)
            );
        } 
    }
}
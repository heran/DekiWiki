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
using System.Collections;
using System.Globalization;

namespace MindTouch.Deki.Script.Runtime.Library {
    public static partial class DekiScriptLibrary {

        //--- Constants ---
        private static readonly Random RANDOM = new Random();

        //--- Class Methods ---
        [DekiScriptFunction("num.format", "Show number in custom format.", IsIdempotent = true)]
        public static string NumberFormat(
            [DekiScriptParam("number")] double number,
            [DekiScriptParam("number format string (http://msdn2.microsoft.com/en-us/library/0c899ak8(VS.80).aspx)")] string format
        ) {
            CultureInfo culture = GetCulture();
            return number.ToString(format, culture);
        }

        [DekiScriptFunction("num.round", "Round to nearest number with given precision.", IsIdempotent = true)]
        public static double NumberRound(
            [DekiScriptParam("number")] double number,
            [DekiScriptParam("digits to keep after decimal point (default: 0)", true)] int? digits
        ) {
            int d = digits ?? 0;
            if(d >= 0) {
                return Math.Round(number, d);
            }
            double ten = Math.Pow(10, -d);
            return Math.Round(number / ten, 0) * ten;
        }

        [DekiScriptFunction("num.abs", "Get absolute value of number.")]
        public static double NumberAbs(
            [DekiScriptParam("number")] double number
        ) {
            return Math.Abs(number);
        }

        [DekiScriptFunction("num.acos", "Get the angle whose cosine is the specified number.")]
        public static double NumberAcos(
            [DekiScriptParam("number")] double number
        ) {
            return Math.Acos(number);
        }

        [DekiScriptFunction("num.asin", "Get the angle whose sine is the specified number.")]
        public static double NumberAsin(
            [DekiScriptParam("number")] double number
        ) {
            return Math.Asin(number);
        }

        [DekiScriptFunction("num.atan", "Get the angle whose tangent is the specified number.")]
        public static double NumberAtan(
            [DekiScriptParam("number")] double number
        ) {
            return Math.Atan(number);
        }

        [DekiScriptFunction("num.atan2", "Get the angle whose tangent is the quotient of two specified numbers.")]
        public static double NumberAtan2(
            [DekiScriptParam("number")] double x,
            [DekiScriptParam("number")] double y
        ) {
            return Math.Atan2(x, y);
        }

        [DekiScriptFunction("num.ceiling", "Get the smallest integer greater or equal to the specified number.")]
        public static double NumberCeiling(
            [DekiScriptParam("number")] double number
        ) {
            return Math.Ceiling(number);
        }

        [DekiScriptFunction("num.cos", "Get the cosine of the specified angle.")]
        public static double NumberCos(
            [DekiScriptParam("angle")] double angle
        ) {
            return Math.Cos(angle);
        }

        [DekiScriptFunction("num.cosh", "Get the hyperbolic cosine of the specified angle.")]
        public static double NumberCosh(
            [DekiScriptParam("angle")] double angle
        ) {
            return Math.Cosh(angle);
        }

        [DekiScriptFunction("num.exp", "Get e raised to the specified number.", IsIdempotent = true)]
        public static double NumberExp(
            [DekiScriptParam("number")] double number
        ) {
            return Math.Exp(number);
        }

        [DekiScriptFunction("num.floor", "Get the largest integer less or equal to the specified number.", IsIdempotent = true)]
        public static double NumberFloor(
            [DekiScriptParam("number")] double number
        ) {
            return Math.Floor(number);
        }

        [DekiScriptFunction("num.log", "Get the logarithm of the specified number in the specified base.", IsIdempotent = true)]
        public static double NumberLog(
            [DekiScriptParam("number")] double number,
            [DekiScriptParam("base (default: e)", true)] double? @base
        ) {
            return @base.HasValue ? Math.Log(number, @base.Value) : Math.Log(number);
        }

        [DekiScriptFunction("num.max", "Get the larger of two numbers.", IsIdempotent = true)]
        public static double NumberMax(
            [DekiScriptParam("first number")] double first,
            [DekiScriptParam("second number")] double second
        ) {
            return Math.Max(first, second);
        }

        [DekiScriptFunction("num.min", "Get the smaller of two numbers.", IsIdempotent = true)]
        public static double NumberMin(
            [DekiScriptParam("first number")] double first,
            [DekiScriptParam("second number")] double second
        ) {
            return Math.Min(first, second);
        }

        [DekiScriptFunction("num.pow", "Get the specified number raised to the specified power.", IsIdempotent = true)]
        public static double NumberPow(
            [DekiScriptParam("base number")] double @base,
            [DekiScriptParam("exponent number")] double exponent
        ) {
            return Math.Pow(@base, exponent);
        }

        [DekiScriptFunction("num.sign", "Get a value indicating the sign of the number.", IsIdempotent = true)]
        public static int NumberSign(
            [DekiScriptParam("number")] double number
        ) {
            return Math.Sign(number);
        }

        [DekiScriptFunction("num.sin", "Get the sine of the specified angle.", IsIdempotent = true)]
        public static double NumberSin(
            [DekiScriptParam("angle")] double angle
        ) {
            return Math.Sin(angle);
        }

        [DekiScriptFunction("num.sinh", "Get the hyperbolic sine of the specified angle.", IsIdempotent = true)]
        public static double NumberSinh(
            [DekiScriptParam("angle")] double angle
        ) {
            return Math.Sinh(angle);
        }

        [DekiScriptFunction("num.sqrt", "Get the square root of the specified number.", IsIdempotent = true)]
        public static double NumberSqrt(
            [DekiScriptParam("number")] double number
        ) {
            return Math.Sqrt(number);
        }

        [DekiScriptFunction("num.tan", "Get the tangent of the specified angle.", IsIdempotent = true)]
        public static double NumberTan(
            [DekiScriptParam("angle")] double angle
        ) {
            return Math.Tan(angle);
        }

        [DekiScriptFunction("num.tanh", "Get the hyperbolic tangent of the specified angle.", IsIdempotent = true)]
        public static double NumberTanh(
            [DekiScriptParam("angle")] double angle
        ) {
            return Math.Tanh(angle);
        }

        [DekiScriptFunction("num.int", "Get the integral part of the specified number.", IsIdempotent = true)]
        public static double NumberInt(
            [DekiScriptParam("number")] double number
        ) {
            return Math.Truncate(number);
        }

        [DekiScriptFunction("num.isnan", "Check if number is not-a-number (NaN).", IsIdempotent = true)]
        public static bool NumberIsNan(
            [DekiScriptParam("number")] double number
        ) {
            return double.IsNaN(number);
        }

        [DekiScriptFunction("num.isinfinity", "Check if number is infinite.", IsIdempotent = true)]
        public static bool NumberIsInfinity(
            [DekiScriptParam("number")] double number
        ) {
            return double.IsInfinity(number);
        }

        [DekiScriptFunction("num.isnegativeinfinity", "Check if number is negative infinity.", IsIdempotent = true)]
        public static bool NumberIsNegativeInfinity(
            [DekiScriptParam("number")] double number
        ) {
            return double.IsNegativeInfinity(number);
        }

        [DekiScriptFunction("num.ispositiveinfinity", "Check if number is positive infinity.", IsIdempotent = true)]
        public static bool NumberIsPositiveInfinity(
            [DekiScriptParam("number")] double number
        ) {
            return double.IsPositiveInfinity(number);
        }

        [DekiScriptFunction("num.cast", "Cast value to a number or nil if not possible.", IsIdempotent = true)]
        public static double? NumberCast(
            [DekiScriptParam("value to cast")] object value
        ) {
            if(value == null) {
                return null;
            }
            try {
                return SysUtil.ChangeType<double>(value);
            } catch {
                return null;
            }
        }

        [DekiScriptFunction("num.series", "Create a series of numbers.", IsIdempotent = true)]
        public static ArrayList NumberSeries(
            [DekiScriptParam("starting value")] double start,
            [DekiScriptParam("ending value")] double end,
            [DekiScriptParam("step value (default: 1 or -1)", true)] double? step
        ) {

            // check if step is zero
            double increment = step ?? (((end - start) >= 0.0) ? 1.0 : -1.0);
            if(increment == 0) {
                return new ArrayList();
            }

            // check if count is going in the wrong direction or if result is too large
            int count;
            try {
                double total = (end - start) / increment;
                if(total < 0) {
                    return new ArrayList();
                }
                count = (int)Math.Min(total, 1000);
            } catch {

                // arithmetic overflow
                return new ArrayList();
            }

            // populate series
            ArrayList result = new ArrayList(count);
            for(int i = 0; i <= count; ++i) {
                result.Add(start + i * increment);
            }
            return result;
        }

        [DekiScriptFunction("num.random", "Return a random number between 0 and 1.")]
        public static double NumberRandom() {
            return RANDOM.NextDouble();
        }
    }
}

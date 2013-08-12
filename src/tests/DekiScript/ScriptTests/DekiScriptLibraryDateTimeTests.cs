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
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using MindTouch.Deki.Script.Runtime.Library;
using MindTouch.Deki.Script.Expr;
using MindTouch.Deki.Script.Tests.Util;
using NUnit.Framework;

namespace MindTouch.Deki.Script.Tests.ScriptTests {
    
    [TestFixture]
    public class DekiScriptLibraryDateTimeTests {

        //--- Fields ---
        private DekiScriptTester _t;

        [SetUp]
        public void Setup() {
            _t = new DekiScriptTester();
        }

        [Test]
        public void CultureDateTime_with_utc_flagless_signature_assumes_utc_if_no_tz_specified() {
            double offset;
            var time = DekiScriptLibrary.CultureDateTimeParse("12:00pm", CultureInfo.InvariantCulture, out offset);
            Assert.AreEqual(DateTimeKind.Utc, time.Kind);
            Assert.AreEqual(12, time.Hour);
            Assert.AreEqual(0d, offset);
        }

        [Test]
        public void CultureDateTime_with_utc_flagless_signature_adjust_time_to_utc() {
            double offset;
            var time = DekiScriptLibrary.CultureDateTimeParse("12:00pm -03:00", CultureInfo.InvariantCulture, out offset);
            Assert.AreEqual(DateTimeKind.Utc, time.Kind);
            Assert.AreEqual(15, time.Hour);
            Assert.AreEqual(-3d, offset);
        }

        [Test]
        public void CultureDateTime_does_not_adjust_kind_if_no_tz_specified_and_utc_flag_is_false() {
            double offset;
            var time = DekiScriptLibrary.CultureDateTimeParse("12:00pm", CultureInfo.InvariantCulture, false, out offset);
            Assert.AreEqual(DateTimeKind.Unspecified, time.Kind);
            Assert.AreEqual(12, time.Hour);
            Assert.AreEqual(0, offset);
        }

        [Test]
        public void CultureDateTime_adjusts_kind_and_time_to_utc_if_tz_specified_and_utc_flag_is_false() {
            double offset;
            var time = DekiScriptLibrary.CultureDateTimeParse("12:00pm -03:00", CultureInfo.InvariantCulture, false, out offset);
            Assert.AreEqual(DateTimeKind.Utc, time.Kind);
            Assert.AreEqual(15, time.Hour);
            Assert.AreEqual(-3d, offset);
        }

        [Test]
        public void DateParse_with_format_assumes_utc_if_no_default_tz_is_given() {
            Assert.AreEqual(
                "Sun, 10 Oct 2010 12:00:00 GMT",
                DekiScriptLibrary.DateParse("2010/10/10 12:00:00", "yyyy/MM/dd HH:mm:ss", CultureInfo.InvariantCulture.IetfLanguageTag,  null));
        }

        [Test]
        public void DateParse_with_format_uses_passed_tz_if_no_default_tz_is_given_and_adjusts_to_utc() {
            Assert.AreEqual(
                "Sun, 10 Oct 2010 15:00:00 GMT",
                DekiScriptLibrary.DateParse("2010/10/10 12:00:00", "yyyy/MM/dd HH:mm:ss", CultureInfo.InvariantCulture.IetfLanguageTag, "-3"));
        }

        [Test]
        public void DateParse_without_format_assumes_utc_if_no_default_tz_is_given() {
            Assert.AreEqual(
                "Sun, 10 Oct 2010 12:00:00 GMT",
                DekiScriptLibrary.DateParse("2010/10/10 12:00:00", null, CultureInfo.InvariantCulture.IetfLanguageTag, null));
        }

        [Test]
        public void DateParse_without_format_uses_passed_tz_if_no_default_tz_is_given_and_adjusts_to_utc() {
            Assert.AreEqual(
                "Sun, 10 Oct 2010 15:00:00 GMT",
                DekiScriptLibrary.DateParse("2010/10/10 12:00:00", null, CultureInfo.InvariantCulture.IetfLanguageTag, "-3"));
        }

        [Test]
        public void DateTimeZone_returns_GMT_if_no_tz_in_date() {
            Assert.AreEqual("GMT",DekiScriptLibrary.DateTimeZone("12:00pm",null));
        }

        [Test]
        public void DateTimeZone_returns_provided_tz_if_no_tz_in_date() {
            Assert.AreEqual("-03:00", DekiScriptLibrary.DateTimeZone("12:00pm", "-3"));
        }

        [Test]
        public void DateTimeZone_returns_tz_from_date() {
            Assert.AreEqual("-03:00", DekiScriptLibrary.DateTimeZone("Sun, 10 Oct 2010 12:00:00 -03:00", null));
        }

        [Test]
        public void DateTimeZone_returns_tz_from_date_ignoring_provided_tz_default() {
            Assert.AreEqual("-05:00", DekiScriptLibrary.DateTimeZone("Sun, 10 Oct 2010 12:00:00 -05:00", "-3"));
        }

        [Test]
        public void AddDays() {
            _t.Test(
                @"Date.adddays(""01/01/2000"", 10);",
                @"Tue, 11 Jan 2000 00:00:00 GMT",
                typeof(DekiScriptString)
            );
        }

        [Test]
        public void AddHours() {
            _t.Test(
                @"Date.addhours(""01/01/2000"", 10);",
                @"Sat, 01 Jan 2000 10:00:00 GMT",
                typeof(DekiScriptString)
            );
        }

        [Test]
        public void AddMinutes() {
            _t.Test(
                @"Date.addminutes(""01/01/2000"", 10);",
                @"Sat, 01 Jan 2000 00:10:00 GMT",
                typeof(DekiScriptString)
            );
        }

        [Test]
        public void AddSeconds() {
            _t.Test(
                @"Date.addseconds(""01/01/2000"", 10);",
                @"Sat, 01 Jan 2000 00:00:10 GMT",
                typeof(DekiScriptString)
            );
        }

        [Test]
        public void AddWeeks() {
            _t.Test(
                @"Date.addweeks(""01/01/2000"", 10);",
                @"Sat, 11 Mar 2000 00:00:00 GMT",
                typeof(DekiScriptString)
            );
        }

        [Test]
        public void AddMonths() {
            _t.Test(
                @"Date.addmonths(""01/01/2000"", 10);",
                @"Wed, 01 Nov 2000 00:00:00 GMT",
                typeof(DekiScriptString)
            );
        }

        [Test]
        public void AddYears() {
            _t.Test(
                @"Date.addyears(""01/01/2000"", 10);",
                @"Fri, 01 Jan 2010 00:00:00 GMT",
                typeof(DekiScriptString)
            );
        }

        [Test]
        public void Calendar() { // Might want to think of a more clever way to test this
            _t.Test(
                @"string.contains(string.serialize(date.calendar(""01/01/2010"")), ""{ date : \""Fri, 01 Jan 2010 00:00:00 GMT\"", day : 1, dayname : \""Friday\"", dayofweek : 5, dayofyear : 1, diffdays : 0, diffmonths : 0, month : 1, monthname : \""January\"", week : 1, year : \""2010\"" }"");",
                @"true",
                typeof(DekiScriptBool)
            );
        }

        [Test]
        public void Calendar_OverAdjust() { // Might want to think of a more clever way to test this
            _t.Test(
                @"string.contains(string.serialize(date.calendar(""01/04/2010"", 10)), ""{ date : \""Fri, 01 Jan 2010 00:00:00 GMT\"", day : 1, dayname : \""Friday\"", dayofweek : 5, dayofyear : 1, diffdays : 0, diffmonths : 0, month : 1, monthname : \""January\"", week : 1, year : \""2010\"" }"");",
                @"false",
                typeof(DekiScriptBool)
            );
        }

        [Test]
        public void Compare() {
            _t.Test(
                @"Date.compare(""01/01/2000"", ""Sat, 01 Jan 2000 00:00:00 GMT"");",
                @"0",
                typeof(DekiScriptNumber)
            );
        }

        [Test]
        public void Date() {
            _t.Test(
                @"Date.date(""Sat, 01 Jan 2000 00:00:00 GMT"");",
                @"Sat, 01 Jan 2000",
                typeof(DekiScriptString)
            );
        }

        [Test]
        public void Day() {
            _t.Test(
                @"Date.day(""Sat, 01 Jan 2000 00:00:00 GMT"");",
                @"01",
                typeof(DekiScriptString)
            );
        }

        [Test]
        public void DayName() {
            _t.Test(
                @"Date.dayname(""Sat, 01 Jan 2000 00:00:00 GMT"");",
                @"Saturday",
                typeof(DekiScriptString)
            );
        }

        [Test]
        public void DayOfWeek() {
            _t.Test(
                @"Date.dayofweek(""Sat, 01 Jan 2000 00:00:00 GMT"");",
                @"6",
                typeof(DekiScriptNumber)
            );
        }

        [Test]
        public void DayOfYear() {
            _t.Test(
                @"Date.dayofyear(""Sat, 31 Aug 2000 00:00:00 GMT"");",
                @"244",
                typeof(DekiScriptNumber)
            );
        }

        [Test]
        public void DaysInMonth() {
            _t.Test(
                @"Date.daysinmonth(""02/05/2000"");",
                @"29",
                typeof(DekiScriptNumber)
            );
        }

        [Test]
        public void DiffDays() {
            _t.Test(
                @"date.diffdays(""01/10/2010"", ""02/05/2007"");",
                @"1070",
                typeof(DekiScriptNumber)
            );
        }

        [Test]
        public void DiffHours() {
            _t.Test(
                @"date.diffhours(""01/10/2010"", ""02/05/2007"");",
                @"25680",
                typeof(DekiScriptNumber)
            );
        }

        [Test]
        public void DiffMonths() {
            _t.Test(
                @"date.diffmonths(""01/10/2010"", ""02/05/2007"");",
                @"35",
                typeof(DekiScriptNumber)
            );
        }

        [Test]
        public void DiffMinutes() {
            _t.Test(
                @"date.diffminutes(""01/10/2010"", ""02/05/2007"");",
                @"1540800",
                typeof(DekiScriptNumber)
            );
        }

        [Test]
        public void DiffSeconds() {
            _t.Test(
                @"date.diffseconds(""01/10/2010"", ""02/05/2007"");",
                @"92448000",
                typeof(DekiScriptNumber)
            );
        }

        [Test]
        public void Format() {
            _t.Test(
                @"date.format(""31 Aug 2000 01:23:45 GMT"", ""yyyy dd MM HH mm ss"");",
                @"2000 31 08 01 23 45",
                typeof(DekiScriptString)
            );
        }

        [Test]
        public void Format_iso_d() {
            _t.Test(
                @"date.format(""31 Aug 2000 01:23:45 GMT"", ""iso-d"");",
                @"4",
                typeof(DekiScriptString)
            );
        }

        [Test]
        public void Format_iso_Www() {
            _t.Test(
                @"date.format(""31 Aug 2000 01:23:45 GMT"", ""iso-Www"");",
                @"W35",
                typeof(DekiScriptString)
            );
        }

        [Test]
        public void Format_iso_yyyy() {
            _t.Test(
                @"date.format(""31 Aug 2000 01:23:45 GMT"", ""iso-yyyy"");",
                @"2000",
                typeof(DekiScriptString)
            );
        }

        [Test]
        public void Format_iso_Www_d() {
            _t.Test(
                @"date.format(""31 Aug 2000 01:23:45 GMT"", ""iso-Www-d"");",
                @"W35-4",
                typeof(DekiScriptString)
            );
        }

        [Test]
        public void Format_iso_yyyy_Www() {
            _t.Test(
                @"date.format(""31 Aug 2000 01:23:45 GMT"", ""iso-yyyy-Www"");",
                @"2000-W35",
                typeof(DekiScriptString)
            );
        }

        [Test]
        public void Format_iso_yyyy_Www_d() {
            _t.Test(
                @"date.format(""31 Aug 2000 01:23:45 GMT"", ""iso-yyyy-Www-d"");",
                @"2000-W35-4",
                typeof(DekiScriptString)
            );
        }

        [Test]
        [ExpectedException("MindTouch.Deki.Script.Runtime.DekiScriptInvokeException")]
        public void Format_badFormat() {
            _t.Test(
                @"date.format(""31 Aug 2000 01:23:45 GMT"", ""iso-error"");",
                @"2000-W35-4",
                typeof(DekiScriptString)
            );
        }

        [Test]
        public void Hours() {
            _t.Test(
                @"date.hours(""31 Aug 2000 01:23:45 GMT"");",
                "1",
                typeof(DekiScriptNumber)
            );
        }

        [Test]
        public void InRange_True() {
            _t.Test(
                @"date.inrange(""31 Aug 2000 01:23:45 GMT"", ""31 Aug 1990 01:23:45 GMT"", ""31 Aug 2010 01:23:45 GMT"");",
                "true",
                typeof(DekiScriptBool)
            );
        }

        [Test]
        public void InRange_False() {
            _t.Test(
                @"date.inrange(""31 Aug 2010 01:23:46 GMT"", ""31 Aug 1990 01:23:45 GMT"", ""31 Aug 2010 01:23:45 GMT"");",
                "false",
                typeof(DekiScriptBool)
            );
        }

        [Test]
        public void IsAfter() {
            _t.Test(
                @"date.isafter(""31 Aug 2010 01:23:46 GMT"", ""31 Aug 2010 01:23:45 GMT"");",
                "true",
                typeof(DekiScriptBool)
            );
        }

        [Test]
        public void IsBefore() {
            _t.Test(
                @"date.isbefore(""31 Aug 2010 01:23:46 GMT"", ""31 Aug 2010 01:23:45 GMT"");",
                "false",
                typeof(DekiScriptBool)
            );
        }

        [Test]
        public void IsoWeek() {
            _t.Test(
                @"date.isoweek(""29 Aug 2010 01:23:46 GMT"");",
                "{ day : 7, week : 34, year : 2010 }",
                typeof(DekiScriptMap)
            );
        }

        [Test]
        public void IsSameDay() {
            _t.Test(
                @"date.issameday(""31 Aug 2010 01:23:46 GMT"", ""31 Aug 2010 22:23:45 GMT"");",
                "true",
                typeof(DekiScriptBool)
            );
        }

        [Test]
        public void IsSameMonth() {
            _t.Test(
                @"date.issamemonth(""31 Aug 2010 01:23:46 GMT"", ""31 Aug 2009 22:23:45 GMT"");",
                "false",
                typeof(DekiScriptBool)
            );
        }

        [Test]
        public void IsSameWeek() {
            _t.Test(
                @"date.issameweek(""31 Aug 2010 01:23:46 GMT"", ""03 Sep 2010 22:23:45 GMT"");",
                "true",
                typeof(DekiScriptBool)
            );
        }

        [Test]
        public void IsValid_EmptyString() {
            _t.Test(
                @"date.isvalid("""");",
                "false",
                typeof(DekiScriptBool)
            );
        }

        [Test]
        public void IsValid_ValidDate() {
            _t.Test(
                @"date.isvalid(""01/01/2001"");",
                "true",
                typeof(DekiScriptBool)
            );
        }

        [Test]
        public void IsValid_BadDate() {
            _t.Test(
                @"date.isvalid(""abcdefg"");",
                "false",
                typeof(DekiScriptBool)
            );
        }

        [Test]
        public void Max() {
            _t.Test(
                @"date.max(""31 Aug 2010 01:23:46 GMT"", ""03 Sep 2010 22:23:45 GMT"");",
                "03 Sep 2010 22:23:45 GMT",
                typeof(DekiScriptString)
            );
        }

        [Test]
        public void Min() {
            _t.Test(
                @"date.min(""31 Aug 2010 01:23:46 GMT"", ""03 Sep 2010 22:23:45 GMT"");",
                "31 Aug 2010 01:23:46 GMT",
                typeof(DekiScriptString)
            );
        }

        [Test]
        public void Minutes() {
            _t.Test(
                @"date.minutes(""31 Aug 2010 01:23:46 GMT"")",
                "23",
                typeof(DekiScriptNumber)
            );
        }

        [Test]
        public void Month() {
            _t.Test(
                @"date.month(""31 Aug 2010 01:23:46 GMT"")",
                "08",
                typeof(DekiScriptString)
            );
        }

        [Test]
        public void MonthName() {
            _t.Test(
                @"date.monthname(""31 Aug 2010 01:23:46 GMT"")",
                "August",
                typeof(DekiScriptString)
            );
        }

        [Test]
        public void New() {
            _t.Test(
                @"date.new(2010, 05, 05, 12, 34, 56, ""-09:00"");",
                "Wed, 05 May 2010 03:34:56 -09:00",
                typeof(DekiScriptString)
            );
        }

        [Test]
        public void Now() { // TODO (melder): think of better way to test this
            _t.Test(
                @"string.contains(date.now, ""GMT"");",
                "true",
                typeof(DekiScriptBool)
            );
        }


        [Test]
        public void ParseISOWeek() {
            _t.Test(
                @"date.parseisoweek(""2010-W24-5"");",
                "6/18/2010",
                typeof(DekiScriptString)
            );
        }

        [Test]
        [ExpectedException("MindTouch.Deki.Script.Runtime.DekiScriptInvokeException")]
        public void ParseISOWeek_BadLength() {
            _t.Test(
                @"date.parseisoweek(""2010-W24-5-123"");",
                "6/18/2010",
                typeof(DekiScriptString)
            );
        }

        [Test]
        [ExpectedException("MindTouch.Deki.Script.Runtime.DekiScriptInvokeException")]
        public void ParseISOWeek_BadYear() {
            _t.Test(
                @"date.parseisoweek(""2ZZ0-W24-5"");",
                "6/18/2010",
                typeof(DekiScriptString)
            );
        }

        [Test]
        [ExpectedException("MindTouch.Deki.Script.Runtime.DekiScriptInvokeException")]
        public void ParseISOWeek_UnsupportedYear() {
            _t.Test(
                @"date.parseisoweek(""0000-W24-5"");",
                "6/18/2010",
                typeof(DekiScriptString)
            );
        }

        [Test]
        [ExpectedException("MindTouch.Deki.Script.Runtime.DekiScriptInvokeException")]
        public void ParseISOWeek_No_W_Separator() {
            _t.Test(
                @"date.parseisoweek(""2010-x24-5"");",
                "6/18/2010",
                typeof(DekiScriptString)
            );
        }

        [Test]
        [ExpectedException("MindTouch.Deki.Script.Runtime.DekiScriptInvokeException")]
        public void ParseISOWeek_BadWeek() {
            _t.Test(
                @"date.parseisoweek(""2010-Wxx-5"");",
                "6/18/2010",
                typeof(DekiScriptString)
            );
        }

        [Test]
        [ExpectedException("MindTouch.Deki.Script.Runtime.DekiScriptInvokeException")]
        public void ParseISOWeek_WeekOutOfRange() {
            _t.Test(
                @"date.parseisoweek(""2010-W54-5"");",
                "6/18/2010",
                typeof(DekiScriptString)
            );
        }

        [Test]
        [ExpectedException("MindTouch.Deki.Script.Runtime.DekiScriptInvokeException")]
        public void ParseISOWeek_No_d_Separator() {
            _t.Test(
                @"date.parseisoweek(""2010-W24x5"");",
                "6/18/2010",
                typeof(DekiScriptString)
            );
        }

        [Test]
        [ExpectedException("MindTouch.Deki.Script.Runtime.DekiScriptInvokeException")]
        public void ParseISOWeek_BadDay() {
            _t.Test(
                @"date.parseisoweek(""2010-W24-X"");",
                "6/18/2010",
                typeof(DekiScriptString)
            );
        }

        [Test]
        [ExpectedException("MindTouch.Deki.Script.Runtime.DekiScriptInvokeException")]
        public void ParseISOWeek_DayOutOfRange() {
            _t.Test(
                @"date.parseisoweek(""2010-W24-8"");",
                "6/18/2010",
                typeof(DekiScriptString)
            );
        }

        [Test]
        public void Parts() {
            _t.Test(
                @"date.parts(""31 Aug 2010 01:23:46 GMT"");",
                @"{ date : ""Tue, 31 Aug 2010 01:23:46 GMT"", day : 31, dayname : ""Tuesday"", dayofweek : 2, dayofyear : 243, daysinmonth : 31, hours : 1, minutes : 23, month : 8, monthname : ""August"", seconds : 46, timezone : ""GMT"", week : 36, year : ""2010"" }",
                typeof(DekiScriptMap)
            );
        }

        [Test]
        public void Seconds() {
            _t.Test(
                @"date.seconds(""31 Aug 2010 01:23:46 GMT"");",
                @"46",
                typeof(DekiScriptNumber)
            );
        }

        [Test]
        public void StartOfDay() {
            _t.Test(
                @"date.startofday(""31 Aug 2010 01:23:46 GMT"");",
                @"Tue, 31 Aug 2010 00:00:00 GMT",
                typeof(DekiScriptString)
            );
        }

        [Test]
        public void StartOfMonth() {
            _t.Test(
                @"date.startofmonth(""31 Aug 2010 01:23:46 GMT"");",
                @"Sun, 01 Aug 2010 00:00:00 GMT",
                typeof(DekiScriptString)
            );
        }

        [Test]
        public void StartOfWeek() {
            _t.Test(
                @"date.startofweek(""31 Aug 2010 01:23:46 GMT"");",
                @"Sun, 29 Aug 2010 00:00:00 GMT",
                typeof(DekiScriptString)
            );
        }

        [Test]
        public void StartOfYear() {
            _t.Test(
                @"date.startofyear(""31 Aug 2010 01:23:46 GMT"");",
                @"Fri, 01 Jan 2010 00:00:00 GMT",
                typeof(DekiScriptString)
            );
        }

        [Test]
        public void ChangeTimeZone() {
            _t.Test(
                @"date.changetimezone(""31 Aug 2010 01:23:46 GMT"", ""-6:00"");",
                @"Mon, 30 Aug 2010 19:23:46 -06:00",
                typeof(DekiScriptString)
            );
        }

        [Test]
        public void TimeOfDay() {
            _t.Test(
                @"date.time(""31 Aug 2010 01:23:46 GMT"");",
                @"01:23",
                typeof(DekiScriptString)
            );
        }

        [Test]
        public void TimeZoneNow() { // TODO: (melder) need to think of better way to test this
            _t.Test(
                @"string.contains(date.timezonenow(""-6:00""), ""-06:00"");",
                @"true",
                typeof(DekiScriptBool)
            );
        }

        [Test]
        public void Week() {
            _t.Test(
                @"date.Week(""31 Aug 2010 01:23:46 GMT"");",
                @"36",
                typeof(DekiScriptNumber)
            );
        }

        [Test]
        public void Year() {
            _t.Test(
                @"date.year(""31 Aug 2010 01:23:46 GMT"");",
                @"2010",
                typeof(DekiScriptString)
            );
        }
    }
}

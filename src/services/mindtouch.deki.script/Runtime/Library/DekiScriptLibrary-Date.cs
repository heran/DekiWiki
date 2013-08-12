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
using System.Text;
using System.Text.RegularExpressions;

namespace MindTouch.Deki.Script.Runtime.Library {
    public static partial class DekiScriptLibrary {

        //--- Constants ---
        private static readonly Regex LEADING_DAYNAME = new Regex(@"^[\p{Ll}\p{Lu}\p{Lt}\p{Lo}\p{Lm}]{1,3},\s*", RegexOptions.Compiled);
        private static readonly Regex TIMEZONE_OFFSET = new Regex(@"((?<sign>[\+\-])?(?<hh>\d{1,2})(:(?<mm>\d{1,2})|))|GMT", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        //--- Class Methos ---
        [DekiScriptFunction("date.now", "Current date-time in GMT format.", IsProperty = true)]
        public static string DateNow() {
            return CultureDateTime(DateTime.UtcNow, GetCulture(), 0.0);
        }

        [DekiScriptFunction("date.today", "Beginning of day for GMT timezone.", IsProperty = true)]
        public static string DateToday() {
            return CultureDateTime(DateTime.UtcNow.Date, GetCulture(), 0.0);
        }

        [DekiScriptFunction("date.timezonenow", "Current date-time in time-zone format.")]
        public static string DateTimeZoneNow(
            [DekiScriptParam("time-zone offset (format: ±hh:mm, default: GMT)", true)] string timezone
        ) {
            double offset = ParseTimeZone(timezone);
            return CultureDateTime(DateTime.UtcNow, GetCulture(), offset);
        }

        [DekiScriptFunction("date.timezonetoday", "Beginning of day for GMT timezone.")]
        public static string DateTimeZoneToday(
            [DekiScriptParam("time-zone offset (format: ±hh:mm, default: GMT)", true)] string timezone
        ) {
            CultureInfo culture = GetCulture();
            DateTime utcNow = DateTime.UtcNow;
            double offset = ParseTimeZone(timezone);

            // NOTE (steveb): first adjust the date-time to be in the desired timezone, 
            //                then remove the time-of-day component, 
            //                then compensate for the offse being applied by CultureDateTime
            DateTime timezoneNow = utcNow.AddHours(offset).Date.AddHours(-offset);
            return CultureDateTime(timezoneNow, culture, offset);
        }

        [DekiScriptFunction("date.changetimezone", "Change time-zone of date-time.", IsIdempotent = true)]
        public static string DateTimeChangeZone(
            [DekiScriptParam("date time string")] string datetime,
            [DekiScriptParam("time-zone offset (format: ±hh:mm, default: GMT)", true)] string timezone
        ) {
            CultureInfo culture = GetCulture();
            double offset;
            DateTime result = CultureDateTimeParse(datetime, culture, out offset);
            offset = ParseTimeZone(timezone);
            return CultureDateTime(result, culture, offset);
        }

        [DekiScriptFunction("date.year", "Get 4-digit year from date-time.", IsIdempotent = true)]
        public static string DateYear(
            [DekiScriptParam("date time string")] string datetime
        ) {
            CultureInfo culture = GetCulture();
            double offset;
            DateTime result = CultureDateTimeParse(datetime, culture, out offset);
            return result.AddHours(offset).ToString("yyyy");
        }

        [DekiScriptFunction("date.week", "Get week of year from date-time.", IsIdempotent = true)]
        public static int DateWeek(
            [DekiScriptParam("date time string")] string datetime
        ) {
            CultureInfo culture = GetCulture();
            double offset;
            DateTime result = CultureDateTimeParse(datetime, culture, out offset);
            return culture.Calendar.GetWeekOfYear(result.AddHours(offset), culture.DateTimeFormat.CalendarWeekRule, culture.DateTimeFormat.FirstDayOfWeek);
        }

        [DekiScriptFunction("date.isoweek", "Convert date-time into ISO 8601 week-date values. (http://en.wikipedia.org/wiki/ISO_8601)", IsIdempotent = true)]
        public static Hashtable DateIsoWeek(
            [DekiScriptParam("date time string")] string datetime
        ) {
            CultureInfo culture = GetCulture();
            double offset;
            DateTime date = CultureDateTimeParse(datetime, culture, out offset);
            int day;
            int week;
            int year;
            DateIsoWeek(date.AddHours(offset), culture, out day, out week, out year);

            // initialize resulting hashtable
            Hashtable result = new Hashtable(StringComparer.OrdinalIgnoreCase);
            result["day"] = day;
            result["week"] = week;
            result["year"] = year;
            return result;
        }

        [DekiScriptFunction("date.parse", "Parse a custom date-time string.", IsIdempotent = true)]
        public static string DateParse(
            [DekiScriptParam("text to parse")] string text,
            [DekiScriptParam("custom date formatting string: see http://msdn2.microsoft.com/EN-US/library/az4se3k1.aspx (default: best effort parsing)", true)] string format,
            [DekiScriptParam("culture code (default: page or site culture)", true)] string culture,
            [DekiScriptParam("timezone offset to use, if none given in the date (format: ±hh:mm, default: GMT)", true)] string timezone
       ) {
            CultureInfo info = (culture != null) ? new CultureInfo(culture) : GetCulture();
            DateTime datetime;
            if(string.IsNullOrEmpty(format)) {
                double offset;
                datetime = CultureDateTimeParse(text, info, false, out offset);
            } else {
                datetime = DateTime.ParseExact(text, format, info.DateTimeFormat);
            }
            if(datetime.Kind == DateTimeKind.Unspecified && !string.IsNullOrEmpty(timezone)) {
                double sourceOffset = ParseTimeZone(timezone);
                datetime = datetime.AddHours(-1 * sourceOffset);
                datetime = new DateTime(datetime.Year, datetime.Month, datetime.Day, datetime.Hour, datetime.Minute, datetime.Second, DateTimeKind.Utc);
            }
            return CultureDateTime(datetime, GetCulture(), 0);
        }

        [DekiScriptFunction("date.parseisoweek", "Parse an ISO 8601 week-date string. (http://en.wikipedia.org/wiki/ISO_8601)", IsIdempotent = true)]
        public static string DateParseIsoWeek(
            [DekiScriptParam("full ISO 8601 week string (e.g. \"yyyy-Www-d\")")] string isoweekdate
        ) {
            CultureInfo culture = GetCulture();
            isoweekdate = isoweekdate.ToUpperInvariant().Trim();
            int year;
            int week;
            int day;

            // strictly validate the week-date passed
            // adhere to ISO 8601 format 'yyyy-Www-d'

            // basic validate of string size
            if(isoweekdate.Length != 10) {
                throw new ArgumentException("date.parseisoweek: 'yyyy-Www-d' length invalid - " + isoweekdate, "isoweekdate");
            }

            // validate 'yyyy' portion. expects value from minyear to maxyear
            if(!int.TryParse(isoweekdate.Substring(0, 4), out year)) {
                throw new ArgumentException("date.parseisoweek: 'yyyy' NaN - " + isoweekdate, "isoweekdate");
            }
            if(year < culture.Calendar.MinSupportedDateTime.Year || year > culture.Calendar.MaxSupportedDateTime.Year) {
                throw new ArgumentException("date.parseisoweek: 'yyyy' out of range [" + culture.Calendar.MinSupportedDateTime.Year.ToString("0000") + " .. " + culture.Calendar.MaxSupportedDateTime.Year.ToString("0000") + "] - " + isoweekdate, "isoweekdate");
            }

            // create new datetime at noon January 1st of the year passed
            // set to first Thursday of that year
            DateTime result = new DateTime(year, 1, 1, 12, 0, 0, culture.Calendar);
            result = (result.DayOfWeek == DayOfWeek.Sunday) ? result.AddDays(-3) : result.AddDays(4 - (int)result.DayOfWeek);
            if(result.Year != year) {
                result = result.AddDays(7);
            }

            // verify '-W' seperator is correct.
            if(isoweekdate.Substring(4, 2) != "-W") {
                throw new ArgumentException("date.parseisoweek: '-W' week separator invalid - " + isoweekdate, "isoweekdate");
            }

            // validate 'ww' portion
            // expects values from 01 to 53 and verifies week 53 does exist in selected year
            // adjusts result datetime by 7*(week-1) in days
            int last_day;
            int last_week;
            int last_year;
            DateIsoWeek(new DateTime(year, 12, 28), culture, out last_day, out last_week, out last_year);
            if(!int.TryParse(isoweekdate.Substring(6, 2), out week)) {
                throw new ArgumentException("date.parseisoweek: 'ww' NaN - " + isoweekdate, "isoweekdate");
            }
            if((week < 1) || (week > last_week)) {
                throw new ArgumentException("date.parseisoweek: 'ww' out of range [01 .. " + last_week.ToString("00") + "] - " + isoweekdate, "isoweekdate");
            }
            result = result.AddDays(7 * (week - 1));

            // verify '-' seperator is correct
            if(isoweekdate.Substring(8, 1) != "-") {
                throw new ArgumentException("date.parseisoweek: '-' day separator invalid - " + isoweekdate, "isoweekdate");
            }

            // validate 'd' portion
            // expect values from 1 to 7
            // adjusts result datetime from Thursday
            if(!int.TryParse(isoweekdate.Substring(9, 1), out day)) {
                throw new ArgumentException("date.parseisoweek: 'd' NaN - " + isoweekdate, "isoweekdate");
            }
            if(day < 1 || day > 7) {
                throw new ArgumentException("date.parseisoweek: 'd' out of range [1 .. 7] - " + isoweekdate, "isoweekdate");
            }
            result = result.AddDays(day - 4);

            // finally return result converted to MindTouch standard date time string
            return result.ToString(culture.DateTimeFormat.ShortDatePattern);
        }

        [DekiScriptFunction("date.time", "Get time from date-time.", IsIdempotent = true)]
        public static string DateTimeOfDay(
            [DekiScriptParam("date time string")] string datetime
        ) {
            CultureInfo culture = GetCulture();
            double offset;
            DateTime result = CultureDateTimeParse(datetime, culture, out offset);
            return result.AddHours(offset).ToString("HH:mm", culture);
        }

        [DekiScriptFunction("date.monthname", "Get name of the month from date-time.", IsIdempotent = true)]
        public static string DateMonthName(
            [DekiScriptParam("date time string")] string datetime
        ) {
            CultureInfo culture = GetCulture();
            double offset;
            DateTime result = CultureDateTimeParse(datetime, culture, out offset);
            return result.AddHours(offset).ToString("MMMM", culture);
        }

        [DekiScriptFunction("date.month", "Get 2-digit month from date-time.", IsIdempotent = true)]
        public static string DateMonth(
            [DekiScriptParam("date time string")] string datetime
        ) {
            CultureInfo culture = GetCulture();
            double offset;
            DateTime result = CultureDateTimeParse(datetime, culture, out offset);
            return result.AddHours(offset).ToString("MM", culture);
        }

        [DekiScriptFunction("date.dayofweek", "Get day of the week from date-time.", IsIdempotent = true)]
        public static int DateDayOfWeek(
            [DekiScriptParam("date time string")] string datetime
        ) {
            CultureInfo culture = GetCulture();
            double offset;
            DateTime result = CultureDateTimeParse(datetime, culture, out offset);
            return (int)culture.Calendar.GetDayOfWeek(result.AddHours(offset));
        }

        [DekiScriptFunction("date.dayofyear", "Get day of the year from date-time.", IsIdempotent = true)]
        public static int DateDayOfYear(
            [DekiScriptParam("date time string")] string datetime
        ) {
            CultureInfo culture = GetCulture();
            double offset;
            DateTime result = CultureDateTimeParse(datetime, culture, out offset);
            return culture.Calendar.GetDayOfYear(result.AddHours(offset));
        }

        [DekiScriptFunction("date.dayname", "Get name of day from date-time.", IsIdempotent = true)]
        public static string DateDayName(
            [DekiScriptParam("date time string")] string datetime
        ) {
            CultureInfo culture = GetCulture();
            double offset;
            DateTime result = CultureDateTimeParse(datetime, culture, out offset);
            return result.AddHours(offset).ToString("dddd", culture);
        }

        [DekiScriptFunction("date.day", "Get day of month from date-time.", IsIdempotent = true)]
        public static string DateDay(
            [DekiScriptParam("date time string")] string datetime
        ) {
            CultureInfo culture = GetCulture();
            double offset;
            DateTime result = CultureDateTimeParse(datetime, culture, out offset);
            return result.AddHours(offset).ToString("dd", culture);
        }

        [DekiScriptFunction("date.format", "Show date-time in custom format.", IsIdempotent = true)]
        public static string DateFormat(
            [DekiScriptParam("date time string")] string datetime,
            [DekiScriptParam("date time format string (one of \"iso-d\", \"iso-Www\", \"iso-yyyy\", \"iso-Www-d\", \"iso-yyyy-Www\", \"iso-yyyy-Www-d\", or custom formatting string: see http://msdn2.microsoft.com/EN-US/library/az4se3k1.aspx)")] string format
        ) {
            CultureInfo culture = GetCulture();
            double offset;
            DateTime date = CultureDateTimeParse(datetime, culture, out offset);

            // check formatting string
            if(format.StartsWithInvariant("iso-")) {
                int day;
                int week;
                int year;
                DateIsoWeek(date, culture, out day, out week, out year);
                switch(format) {
                case "iso-d":

                    // format 'Www-d': week4 monday = "W04-1"
                    return day.ToString();
                case "iso-Www":

                    // format 'Www': week4 = "W04"
                    return "W" + week.ToString("00");
                case "iso-yyyy":

                    // format 'yyyy': 871 A.D = "0871"
                    return year.ToString("0000");
                case "iso-Www-d":

                    // format 'Www-d': week4 monday = "W04-1"
                    return "W" + week.ToString("00") + "-" + day;
                case "iso-yyyy-Www":

                    // format 'yyyy-Www': 871 A.D. week4 = "0871-W04"
                    return year.ToString("0000") + "-W" + week.ToString("00");
                case "iso-yyyy-Www-d":

                    // format 'yyyy-Www-d': 871 A.D. week4 monday = "0871-W04-1"
                    return year.ToString("0000") + "-W" + week.ToString("00") + "-" + day;
                default:

                    // assume standard .NET formatting string
                    throw new ArgumentException("unrecognized ISO 8601 format string", "format");
                }
            } else if(format.EqualsInvariant("xml")) {
                return date.ToString(MindTouch.Xml.XDoc.RFC_DATETIME_FORMAT);
            }

            // assume standard .NET formatting string
            return date.AddHours(offset).ToString(format, culture);
        }

        [DekiScriptFunction("date.addseconds", "Add seconds to date-time.", IsIdempotent = true)]
        public static string DateAddSeconds(
            [DekiScriptParam("date time string")] string datetime,
            [DekiScriptParam("seconds to add")] double seconds
        ) {
            CultureInfo culture = GetCulture();
            double offset;
            DateTime result = CultureDateTimeParse(datetime, culture, out offset);
            return CultureDateTime(result.AddSeconds(seconds), culture, offset);
        }

        [DekiScriptFunction("date.addminutes", "Add minutes to date-time.", IsIdempotent = true)]
        public static string DateAddMinutes(
            [DekiScriptParam("date time string")] string datetime,
            [DekiScriptParam("minutes to add")] double minutes
        ) {
            CultureInfo culture = GetCulture();
            double offset;
            DateTime result = CultureDateTimeParse(datetime, culture, out offset);
            return CultureDateTime(result.AddMinutes(minutes), culture, offset);
        }

        [DekiScriptFunction("date.addhours", "Add hours to date-time.", IsIdempotent = true)]
        public static string DateAddHours(
            [DekiScriptParam("date time string")] string datetime,
            [DekiScriptParam("hours to add")] double hours
        ) {
            CultureInfo culture = GetCulture();
            double offset;
            DateTime result = CultureDateTimeParse(datetime, culture, out offset);
            return CultureDateTime(result.AddHours(hours), culture, offset);
        }

        [DekiScriptFunction("date.adddays", "Add days to date-time.", IsIdempotent = true)]
        public static string DateAddDays(
            [DekiScriptParam("date time string")] string datetime,
            [DekiScriptParam("days to add")] double days
        ) {
            CultureInfo culture = GetCulture();
            double offset;
            DateTime result = CultureDateTimeParse(datetime, culture, out offset);
            return CultureDateTime(result.AddDays(days), culture, offset);
        }

        [DekiScriptFunction("date.addweeks", "Add weeks to date-time.", IsIdempotent = true)]
        public static string DateAddWeeks(
            [DekiScriptParam("date time string")] string datetime,
            [DekiScriptParam("weeks to add")] double weeks
        ) {
            CultureInfo culture = GetCulture();
            double offset;
            DateTime result = CultureDateTimeParse(datetime, culture, out offset);
            return CultureDateTime(result.AddDays(7 * weeks), culture, offset);
        }

        [DekiScriptFunction("date.addmonths", "Add months to date-time.", IsIdempotent = true)]
        public static string DateAddMonths(
            [DekiScriptParam("date time string")] string datetime,
            [DekiScriptParam("months to add")] int months
        ) {
            CultureInfo culture = GetCulture();
            double offset;
            DateTime result = CultureDateTimeParse(datetime, culture, out offset);
            return CultureDateTime(result.AddMonths(months), culture, offset);
        }

        [DekiScriptFunction("date.addyears", "Add years to date-time.", IsIdempotent = true)]
        public static string DateAddYears(
            [DekiScriptParam("date time string")] string datetime,
            [DekiScriptParam("years to add")] int years
        ) {
            CultureInfo culture = GetCulture();
            double offset;
            DateTime result = CultureDateTimeParse(datetime, culture, out offset);
            return CultureDateTime(result.AddYears(years), culture, offset);
        }

        [DekiScriptFunction("date.isafter", "Check if first date-time is after second date-time.", IsIdempotent = true)]
        public static bool DateIsAfter(
            [DekiScriptParam("first date time string")] string first,
            [DekiScriptParam("second date time string")] string second
        ) {
            CultureInfo culture = GetCulture();
            double offsetLeft;
            DateTime left = CultureDateTimeParse(first, culture, out offsetLeft);
            double offsetRight;
            DateTime right = CultureDateTimeParse(second, culture, out offsetRight);
            return left > right;
        }

        [DekiScriptFunction("date.isbefore", "Check if first date-time is before second date-time.", IsIdempotent = true)]
        public static bool DateIsBefore(
            [DekiScriptParam("first date time string")] string first,
            [DekiScriptParam("second date time string")] string second
        ) {
            CultureInfo culture = GetCulture();
            double offsetLeft;
            DateTime left = CultureDateTimeParse(first, culture, out offsetLeft);
            double offsetRight;
            DateTime right = CultureDateTimeParse(second, culture, out offsetRight);
            return left < right;
        }

        [DekiScriptFunction("date.compare", "Compare the first date-time to the second date-time.", IsIdempotent = true)]
        public static int DateCompare(
            [DekiScriptParam("first date time string")] string first,
            [DekiScriptParam("second date time string")] string second
        ) {
            CultureInfo culture = GetCulture();
            double offsetLeft;
            DateTime left = CultureDateTimeParse(first, culture, out offsetLeft);
            double offsetRight;
            DateTime right = CultureDateTimeParse(second, culture, out offsetRight);
            return DateTime.Compare(left, right);
        }

        [DekiScriptFunction("date.max", "Compare the first date-time to the second date-time and return the later one.", IsIdempotent = true)]
        public static string DateMax(
            [DekiScriptParam("first date time string")] string first,
            [DekiScriptParam("second date time string")] string second
        ) {
            return (DateCompare(first, second) < 0) ? second : first;
        }

        [DekiScriptFunction("date.min", "Compare the first date-time to the second date-time and return the earlier one.", IsIdempotent = true)]
        public static string DateMin(
            [DekiScriptParam("first date time string")] string first,
            [DekiScriptParam("second date time string")] string second
        ) {
            return (DateCompare(first, second) > 0) ? second : first;
        }

        [DekiScriptFunction("date.startofday", "Get date-time corresponding to beginning of day (i.e. midnight).", IsIdempotent = true)]
        public static string DateStartOfDay(
            [DekiScriptParam("date time string")] string datetime
        ) {
            CultureInfo culture = GetCulture();
            double offset;
            DateTime result = CultureDateTimeParse(datetime, culture, out offset);
            return CultureDateTime(new DateTime(result.Year, result.Month, result.Day, 0, 0, 0, 0, result.Kind).AddHours(-offset), culture, offset);
        }

        [DekiScriptFunction("date.startofweek", "Get date-time corresponding to beginning of week (i.e. Sunday).", IsIdempotent = true)]
        public static string DateStartOfWeek(
            [DekiScriptParam("date time string")] string datetime
        ) {
            CultureInfo culture = GetCulture();
            double offset;
            DateTime result = CultureDateTimeParse(datetime, culture, out offset);
            return CultureDateTime(new DateTime(result.Year, result.Month, result.Day, 0, 0, 0, 0, result.Kind).AddHours(-offset).AddDays(-(int)result.DayOfWeek), culture, offset);
        }

        [DekiScriptFunction("date.startofmonth", "Get date-time corresponding to beginning of month.", IsIdempotent = true)]
        public static string DateStartOfMonth(
            [DekiScriptParam("date time string")] string datetime
        ) {
            CultureInfo culture = GetCulture();
            double offset;
            DateTime result = CultureDateTimeParse(datetime, culture, out offset);
            return CultureDateTime(new DateTime(result.Year, result.Month, 1, 0, 0, 0, 0, result.Kind).AddHours(-offset), culture, offset);
        }

        [DekiScriptFunction("date.startofyear", "Get date-time corresponding to beginning of year.", IsIdempotent = true)]
        public static string DateStartOfYear(
            [DekiScriptParam("date time string")] string datetime
        ) {
            CultureInfo culture = GetCulture();
            double offset;
            DateTime result = CultureDateTimeParse(datetime, culture, out offset);
            return CultureDateTime(new DateTime(result.Year, 1, 1, 0, 0, 0, 0, result.Kind).AddHours(-offset), culture, offset);
        }

        [DekiScriptFunction("date.issameday", "Check if first date-time and the second date-time fall on the same day.", IsIdempotent = true)]
        public static bool DateIsSameDay(
            [DekiScriptParam("first date time string")] string first,
            [DekiScriptParam("second date time string")] string second
        ) {
            CultureInfo culture = GetCulture();
            double offsetLeft;
            DateTime left = CultureDateTimeParse(first, culture, out offsetLeft);
            double offsetRight;
            DateTime right = CultureDateTimeParse(second, culture, out offsetRight);
            return (left.Year == right.Year) && (left.Month == right.Month) && (left.Day == right.Day);
        }

        [DekiScriptFunction("date.issameweek", "Check if first date-time and the second date-time fall on the same week.", IsIdempotent = true)]
        public static bool DateIsSameWeek(
            [DekiScriptParam("first date time string")] string first,
            [DekiScriptParam("second date time string")] string second
        ) {
            CultureInfo culture = GetCulture();
            double offsetLeft;
            DateTime left = CultureDateTimeParse(first, culture, out offsetLeft);
            int weekLeft = culture.Calendar.GetWeekOfYear(left.AddHours(offsetLeft), culture.DateTimeFormat.CalendarWeekRule, culture.DateTimeFormat.FirstDayOfWeek);
            double offsetRight;
            DateTime right = CultureDateTimeParse(second, culture, out offsetRight);
            int weekRight = culture.Calendar.GetWeekOfYear(right.AddHours(offsetRight), culture.DateTimeFormat.CalendarWeekRule, culture.DateTimeFormat.FirstDayOfWeek);
            return (left.Year == right.Year) && (weekLeft == weekRight);
        }

        [DekiScriptFunction("date.issamemonth", "Check if first date-time and the second date-time fall on the same month.", IsIdempotent = true)]
        public static bool DateIsSameMonth(
            [DekiScriptParam("first date time string")] string first,
            [DekiScriptParam("second date time string")] string second
        ) {
            CultureInfo culture = GetCulture();
            double offsetLeft;
            DateTime left = CultureDateTimeParse(first, culture, out offsetLeft);
            double offsetRight;
            DateTime right = CultureDateTimeParse(second, culture, out offsetRight);
            return (left.Year == right.Year) && (left.Month == right.Month);
        }

        [DekiScriptFunction("date.diffseconds", "Compute the difference between the first and second date-time in seconds.", IsIdempotent = true)]
        public static double DiffSeconds(
            [DekiScriptParam("first date time string")] string first,
            [DekiScriptParam("second date time string")] string second
        ) {
            CultureInfo culture = GetCulture();
            double offsetLeft;
            DateTime left = CultureDateTimeParse(first, culture, out offsetLeft);
            double offsetRight;
            DateTime right = CultureDateTimeParse(second, culture, out offsetRight);
            return left.Subtract(right).TotalSeconds;
        }

        [DekiScriptFunction("date.diffminutes", "Compute the difference between the first and second date-time in minutes.", IsIdempotent = true)]
        public static double DateDiffMinutes(
            [DekiScriptParam("first date time string")] string first,
            [DekiScriptParam("second date time string")] string second
        ) {
            CultureInfo culture = GetCulture();
            double offsetLeft;
            DateTime left = CultureDateTimeParse(first, culture, out offsetLeft);
            double offsetRight;
            DateTime right = CultureDateTimeParse(second, culture, out offsetRight);
            return left.Subtract(right).TotalMinutes;
        }

        [DekiScriptFunction("date.diffhours", "Compute the difference between the first and second date-time in hours.", IsIdempotent = true)]
        public static double DateDiffHours(
            [DekiScriptParam("first date time string")] string first,
            [DekiScriptParam("second date time string")] string second
        ) {
            CultureInfo culture = GetCulture();
            double offsetLeft;
            DateTime left = CultureDateTimeParse(first, culture, out offsetLeft);
            double offsetRight;
            DateTime right = CultureDateTimeParse(second, culture, out offsetRight);
            return left.Subtract(right).TotalHours;
        }

        [DekiScriptFunction("date.diffdays", "Compute the difference between the first and second date-time in days.", IsIdempotent = true)]
        public static double DateDiffDays(
            [DekiScriptParam("first date time string")] string first,
            [DekiScriptParam("second date time string")] string second
        ) {
            CultureInfo culture = GetCulture();
            double offsetLeft;
            DateTime left = CultureDateTimeParse(first, culture, out offsetLeft);
            double offsetRight;
            DateTime right = CultureDateTimeParse(second, culture, out offsetRight);
            return left.Subtract(right).TotalDays;
        }

        [DekiScriptFunction("date.diffmonths", "Compute the difference between the first and second date-time in months.", IsIdempotent = true)]
        public static double DateDiffMonths(
            [DekiScriptParam("first date time string")] string first,
            [DekiScriptParam("second date time string")] string second
        ) {
            CultureInfo culture = GetCulture();
            double offsetLeft;
            DateTime left = CultureDateTimeParse(first, culture, out offsetLeft);
            double offsetRight;
            DateTime right = CultureDateTimeParse(second, culture, out offsetRight);
            return 12 * (left.Year - right.Year) + (left.Month - right.Month);
        }

        [DekiScriptFunction("date.date", "Show only date component of date-time.", IsIdempotent = true)]
        public static string DateDateFormat(
            [DekiScriptParam("date time string")] string datetime
        ) {
            CultureInfo culture = GetCulture();
            double offset;
            DateTime result = CultureDateTimeParse(datetime, culture, out offset);
            return result.AddHours(offset).ToString("ddd, dd MMM yyyy", culture);
        }

        [DekiScriptFunction("date.seconds", "Get the seconds component of date-time.", IsIdempotent = true)]
        public static int DateSeconds(
            [DekiScriptParam("date time string")] string datetime
        ) {
            CultureInfo culture = GetCulture();
            double offset;
            DateTime result = CultureDateTimeParse(datetime, culture, out offset);
            return result.AddHours(offset).Second;
        }

        [DekiScriptFunction("date.minutes", "Get the minutes component of date-time.", IsIdempotent = true)]
        public static int DateMinutes(
            [DekiScriptParam("date time string")] string datetime
        ) {
            CultureInfo culture = GetCulture();
            double offset;
            DateTime result = CultureDateTimeParse(datetime, culture, out offset);
            return result.AddHours(offset).Minute;
        }

        [DekiScriptFunction("date.hours", "Get the hours component of date-time.", IsIdempotent = true)]
        public static int DateHours(
            [DekiScriptParam("date time string")] string datetime
        ) {
            CultureInfo culture = GetCulture();
            double offset;
            DateTime result = CultureDateTimeParse(datetime, culture, out offset);
            return result.AddHours(offset).Hour;
        }

        [DekiScriptFunction("date.timezone", "Get the time-zone component of date-time.", IsIdempotent = true)]
        public static string DateTimeZone(
            [DekiScriptParam("date time string")] string datetime,
            [DekiScriptParam("default timezone (default: GMT)", true)] string @default
        ) {
            CultureInfo culture = GetCulture();
            double offset;
            DateTime result = CultureDateTimeParse(datetime, culture, false, out offset);
            if(result.Kind == DateTimeKind.Unspecified) {
                offset = ParseTimeZone(@default ?? "GMT");
            }
            return RenderTimeZone(offset);
        }

        [DekiScriptFunction("date.daysinmonth", "Get the number of days in date-time month.", IsIdempotent = true)]
        public static int DateDaysInMonth(
            [DekiScriptParam("date time string")] string datetime
        ) {
            CultureInfo culture = GetCulture();
            double offset;
            DateTime result = CultureDateTimeParse(datetime, culture, out offset);
            DateTime start = new DateTime(result.Year, result.Month, 1, 0, 0, 0, 0, result.Kind);
            DateTime end = start.AddMonths(1);
            return (end - start).Days;
        }

        [DekiScriptFunction("date.calendar", "Get list of calendar days for date-time month.", IsIdempotent = true)]
        public static ArrayList DateCalendar(
            [DekiScriptParam("date time string")] string datetime,
            [DekiScriptParam("first day of the week (one of 0 = Sunday, 1 = Monday, etc.; default: 0)", true)] int firstday
        ) {
            CultureInfo culture = GetCulture();
            double offset;
            DateTime date = CultureDateTimeParse(datetime, culture, out offset);

            // loop over all dates for current month
            ArrayList result = new ArrayList();
            DateTime first = new DateTime(date.Year, date.Month, 1, 0, 0, 0, 0, date.Kind);
            DateTime current = first.AddDays((firstday - (int)date.DayOfWeek) % 7);

            // check if the date was over adjusted
            if(current > first) {
                current = current.AddDays(-7);
            } else if(current <= first.AddDays(-7)) {
                current = current.AddDays(7);
            }

            // iterate over each week of the month
            while(((current.Month <= date.Month) && (current.Year <= date.Year)) || (current.Year < date.Year)) {
                ArrayList week = new ArrayList();
                for(int i = 0; i < 7; ++i) {
                    Hashtable day = new Hashtable(StringComparer.OrdinalIgnoreCase);
                    day.Add("date", CultureDateTime(current, culture, offset));
                    day.Add("day", current.Day);
                    day.Add("dayname", current.ToString("dddd", culture));
                    day.Add("dayofweek", (int)culture.Calendar.GetDayOfWeek(current));
                    day.Add("dayofyear", current.DayOfYear);
                    day.Add("diffdays", (current - date).TotalDays);
                    day.Add("diffmonths", 12 * (current.Year - date.Year) + (current.Month - date.Month));
                    day.Add("month", current.Month);
                    day.Add("monthname", current.ToString("MMMM", culture));
                    day.Add("week", culture.Calendar.GetWeekOfYear(current, culture.DateTimeFormat.CalendarWeekRule, culture.DateTimeFormat.FirstDayOfWeek));
                    day.Add("year", current.ToString("yyyy"));
                    week.Add(day);
                    current = current.AddDays(1);
                }
                result.Add(week);
            }
            return result;
        }

        [DekiScriptFunction("date.isvalid", "Check if the string is a valid date-time.", IsIdempotent = true)]
        public static bool DateIsValid(
            [DekiScriptParam("date time string", true)] string datetime
        ) {
            if(string.IsNullOrEmpty(datetime)) {
                return false;
            }
            CultureInfo culture = GetCulture();
            try {
                double offset;
                DateTime result = CultureDateTimeParse(datetime, culture, out offset);
                return true;
            } catch {
                return false;
            }
        }

        [DekiScriptFunction("date.inrange", "Check if date-time occurs between lower and upper date-time, inclusive.", IsIdempotent = true)]
        public static bool DateInRange(
            [DekiScriptParam("date time to check")] string datetime,
            [DekiScriptParam("lower bound for date time")] string lower,
            [DekiScriptParam("upper bound for date time")] string upper,
            [DekiScriptParam("check if the date-time falls within the date range, ignoring time-of-day", true)] bool? dateonly
        ) {
            CultureInfo culture = GetCulture();
            double offsetDate;
            DateTime date = CultureDateTimeParse(datetime, culture, out offsetDate);
            double offsetLeft;
            DateTime first = CultureDateTimeParse(lower, culture, out offsetLeft);
            double offsetRight;
            DateTime second = CultureDateTimeParse(upper, culture, out offsetRight);

            // common mistake when doing date-range checks is to improperly set the boundaries
            if(second < first) {
                DateTime swap = first;
                first = second;
                second = swap;
            }
            
            // check if we only care about the date component and not the 
            if(dateonly ?? false) {
                first = first.Date;
                second = second.Date.AddDays(1);
                return (date >= first) && (date < second);
            }
            return (date >= first) && (date <= second);
        }

        [DekiScriptFunction("date.new", "Create new date-time value.", IsIdempotent = true)]
        public static string DateNew(
            [DekiScriptParam("Year value")] int year,
            [DekiScriptParam("Month value (1-12)")] int month,
            [DekiScriptParam("Day value (1-31)")] int day,
            [DekiScriptParam("Hour value (0-23, default: 0)", true)] int? hour,
            [DekiScriptParam("Minute value (0-59, default: 0)", true)] int? minute,
            [DekiScriptParam("Second value (0-59, default: 0)", true)] int? second,
            [DekiScriptParam("time-zone offset (format: ±hh:mm, default: GMT)", true)] string timezone
        ) {
            CultureInfo culture = GetCulture();
            DateTime result = new DateTime(year, month, day, hour ?? 0, minute ?? 0, second ?? 0);
            double offset = ParseTimeZone(timezone);
            return CultureDateTime(result, culture, offset);
        }

        [DekiScriptFunction("date.parts", "Decompose a date-time into its components parts.", IsIdempotent = true)]
        public static Hashtable DateParts(
            [DekiScriptParam("date time string")] string datetime
        ) {
            CultureInfo culture = GetCulture();
            double offset;
            DateTime current = CultureDateTimeParse(datetime, culture, out offset);
            DateTime startOfMonth = new DateTime(current.Year, current.Month, 1, 0, 0, 0, 0, current.Kind);
            DateTime endOfMonth = startOfMonth.AddMonths(1);

            // create result map
            Hashtable result = new Hashtable(StringComparer.OrdinalIgnoreCase);
            result.Add("date", CultureDateTime(current, culture, offset));
            result.Add("seconds", current.Second);
            result.Add("minutes", current.Minute);
            result.Add("hours", current.Hour);
            result.Add("day", current.Day);
            result.Add("dayname", current.ToString("dddd", culture));
            result.Add("dayofweek", (int)culture.Calendar.GetDayOfWeek(current));
            result.Add("dayofyear", current.DayOfYear);
            result.Add("daysinmonth", (endOfMonth - startOfMonth).Days);
            result.Add("month", current.Month);
            result.Add("monthname", current.ToString("MMMM", culture));
            result.Add("week", culture.Calendar.GetWeekOfYear(current, culture.DateTimeFormat.CalendarWeekRule, culture.DateTimeFormat.FirstDayOfWeek));
            result.Add("year", current.ToString("yyyy"));
            result.Add("timezone", RenderTimeZone(offset));
            return result;
        }

        public static DateTime CultureDateTimeParse(string datetime, CultureInfo culture, out double timeoffset) {
            return CultureDateTimeParse(datetime, culture, true, out timeoffset);
        }

        public static DateTime CultureDateTimeParse(string datetime, CultureInfo culture, bool assumeUniversal, out double timeoffset) {
            timeoffset = 0.0;
            datetime = datetime.Trim();

            // check if the date has a leading 3 letter day, and remove it
            datetime = LEADING_DAYNAME.Replace(datetime, string.Empty);

            // check if date-time ends in 00:00 and replace it with GMT
            if(datetime.EndsWith(" 00:00")) {
                datetime = datetime.Substring(0, datetime.Length - 6) + " GMT";
            }

            // check if date-time has a timezone offset
            int space = datetime.LastIndexOf(' ');
            if(space > 0) {
                timeoffset = ParseTimeZone(datetime.Substring(space + 1));
            }
            var style = assumeUniversal ? DateTimeStyles.AssumeUniversal : DateTimeStyles.None;
            return DateTime.Parse(datetime, culture.DateTimeFormat, style | DateTimeStyles.AdjustToUniversal);
        }

        public static double ParseTimeZone(string timezone) {
            double result = 0.0;
            if(!string.IsNullOrEmpty(timezone)) {
                Match m = TIMEZONE_OFFSET.Match(timezone.Trim());
                if(m.Success) {
                    Group sign = m.Groups["sign"];
                    Group hours = m.Groups["hh"];
                    if(hours.Success) {
                        result = int.Parse(hours.Value);
                        Group minutes = m.Groups["mm"];
                        if(minutes.Success) {
                            result += int.Parse(minutes.Value) / 60.0;
                        }
                    }
                    if(sign.Value.StartsWith("-")) {
                        result = -result;
                    }
                }
            }
            return result;
        }

        public static string RenderTimeZone(double timeoffset) {
            if(timeoffset == 0.0) {
                return "GMT";
            }
            double hh = Math.Truncate(timeoffset);
            double mm = Math.Truncate((timeoffset - hh) * 60);
            return string.Format("{0}{1}:{2}", (Math.Sign(hh) > 0) ? "+" : "-", Math.Abs(hh).ToString("0#"), Math.Abs(mm).ToString("0#"));
        }

        public static string CultureDateTime(DateTime datetime) {
            return CultureDateTime(datetime, GetCulture(), 0.0);
        }

        public static string CultureDateTime(DateTime datetime, CultureInfo culture, double timeoffset) {
            datetime = datetime.ToSafeUniversalTime().AddHours(timeoffset);
            DateTimeFormatInfo format = culture.DateTimeFormat;
            StringBuilder result = new StringBuilder(48);

            // NOTE (steveb): code below is optimized version of the following line
            // return datetime.ToString(string.Format("ddd, dd MMM yyyy HH':'mm':'ss '{0}'", RenderTimeZone(timeoffset)), culture);

            result.Append(format.AbbreviatedDayNames[(int)datetime.DayOfWeek]);
            result.Append(", ");
            int i = datetime.Day;
            if(i < 10) {
                result.Append('0');
            }
            result.Append(i);
            result.Append(' ');
            result.Append(format.AbbreviatedMonthNames[datetime.Month - 1]);
            result.Append(' ');
            result.Append(datetime.Year);
            result.Append(' ');
            i = datetime.Hour;
            if(i < 10) {
                result.Append('0');
            }
            result.Append(i);
            result.Append(':');
            i = datetime.Minute;
            if(i < 10) {
                result.Append('0');
            }
            result.Append(i);
            result.Append(':');
            i = datetime.Second;
            if(i < 10) {
                result.Append('0');
            }
            result.Append(i);
            result.Append(' ');
            result.Append(RenderTimeZone(timeoffset));
            return result.ToString();
        }

        private static void DateIsoWeek(DateTime date, CultureInfo culture, out int day, out int week, out int year) {

            // ISO 8601 week format - Sunday needs to be 7 instead of 0
            if(date.DayOfWeek == DayOfWeek.Sunday) {
                day = 7;
            } else {
                day = (int)date.DayOfWeek;
            }

            // adjust the given datetime to Thursday of its ISO week, account for Sunday = 0 issue
            date = (date.DayOfWeek == DayOfWeek.Sunday) ? date.AddDays(-3) : date.AddDays(4 - (int)date.DayOfWeek);

            // return the week number using FirstFourDayWeek rule, and Monday as the first day of the week
            week = culture.Calendar.GetWeekOfYear(date, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);

            // returning year value of any given Thursday should always be the correct ISO week year
            year = date.Year;
        }
    }
}

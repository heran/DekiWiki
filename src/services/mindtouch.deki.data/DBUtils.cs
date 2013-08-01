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
using System.Data;
using System.Linq;
using System.Text;
using MindTouch.Dream;

namespace MindTouch.Deki.Data {
    public static class DbUtils {

        //--- Class Properties ---

        // TODO (brigettek) : Remove this property once we've removed all static session references
        public static IDekiDataSession CurrentSession {
            get {
                return DreamContext.Current.Container.Resolve<IDekiDataSession>();
            }
        }

        //--- Class Methods ---

        /// <summary>
        /// Constructs a title object from a data reader
        /// </summary>
        public static Title TitleFromDataReader(IDataReader dr, string nsFieldName, string titleFieldName, string displayFieldName) {
            return TitleFromDataReader(dr, nsFieldName, titleFieldName, displayFieldName, null);
        }

        /// <summary>
        /// Constructs a title object from a data reader
        /// </summary>
        public static Title TitleFromDataReader(IDataReader dr, string nsFieldName, string titleFieldName, string displayFieldName, string filenameFieldName) {
            Title title = Title.FromDbPath((NS)DbUtils.Convert.To<Int32>(dr[nsFieldName], 0), dr[titleFieldName].ToString(), DbUtils.Convert.To<string>(dr[displayFieldName], null));
            if(null != filenameFieldName) {
                title.Filename = dr[filenameFieldName].ToString();
            }
            return title;
        }

        public static string InvertTimestamp(string timestamp) {
            if(string.IsNullOrEmpty(timestamp)) {
                return timestamp;
            }
            char[] result = new char[timestamp.Length];
            for(int i = 0; i < timestamp.Length; ++i) {
                result[i] = (char)('9' - (timestamp[i] - '0'));
            }
            return new string(result);
        }

        public static DateTime ToDateTime(string time) {
            if(string.IsNullOrEmpty(time) || (time.Length < 14)) {
                return DateTime.MinValue;
            }

            // NOTE (steveb): this ugly parsing code is a hack to get around a mono bug in DateTime.TryParseExact
            try {
                int year = (time[0] - '0') * 1000 + (time[1] - '0') * 100 + (time[2] - '0') * 10 + (time[3] - '0');
                int month = (time[4] - '0') * 10 + (time[5] - '0');
                int day = (time[6] - '0') * 10 + (time[7] - '0');
                int hour = (time[8] - '0') * 10 + (time[9] - '0');
                int minute = (time[10] - '0') * 10 + (time[11] - '0');
                int second = (time[12] - '0') * 10 + (time[13] - '0');
                return new DateTime(year, month, day, hour, minute, second, DateTimeKind.Utc);
            } catch {
                DateTime dt;
                return DateTimeUtil.TryParseInvariant(time, out dt) ? dt.ToUniversalTime() : DateTime.MinValue;
            }
        }

        public static string ToString(DateTime time) {
            return (time == DateTime.MinValue) ? null : time.ToUniversalTime().ToString("yyyyMMddHHmmss", System.Globalization.CultureInfo.InvariantCulture.DateTimeFormat);
        }

        public static string ToString(byte[] blob) {
            return blob == null ? string.Empty : Encoding.UTF8.GetString(blob);
        }

        public static byte[] ToBlob(string blob) {
            return blob == null ? new byte[0] : Encoding.UTF8.GetBytes(blob);
        }

        public static TArrayType[] ConvertDelimittedStringToArray<TArrayType>(char delimiter, string delimitedString) where TArrayType : struct {
            if(delimitedString == null) {
                return null;
            }
            var chunks = delimitedString.Split(new[] { delimiter }, StringSplitOptions.RemoveEmptyEntries);
            return (from c in chunks select Convert.To<TArrayType>(c) into val where val.HasValue select val.Value).ToArray();
        }

        public static class Convert {

            static public T? To<T>(object value) where T : struct {
                if(value == null) {
                    return null;
                }

                if(value is T) {

                    //Assume universal if datetime kind not specified
                    if(typeof(T) == typeof(DateTime)) {
                        DateTime dt = (DateTime)value;
                        if(dt.Kind == DateTimeKind.Unspecified) {
                            dt = new DateTime(dt.Ticks, DateTimeKind.Utc);
                        }
                        return (T)((object)dt);

                    } else {
                        return (T)value;
                    }
                }

                if(value is DBNull) {
                    return null;
                }

                try {
                    return (T)SysUtil.ChangeType(value, typeof(T));
                } catch {
                    return null;
                }
            }

            static public T To<T>(object value, T def) {
                if(value == null) {
                    return def;
                }

                //null field in the database should be treated as null
                if(value.GetType() == typeof(DBNull)) {
                    return def;
                }

                if(value is T) {
                    return (T)value;
                }

                try {
                    return (T)SysUtil.ChangeType(value, typeof(T));
                } catch {
                    return def;
                }
            }
        }
    }
}

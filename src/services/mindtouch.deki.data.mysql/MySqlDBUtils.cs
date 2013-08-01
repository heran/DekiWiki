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
using System.Data;
using MySql.Data.Types;

namespace MindTouch.Deki.Data.MySql {
    internal static class MySqlDbUtils {

        internal static T Read<T>(this IDataReader dr, string column) {
            try {

                // Check if null was returned for a non-nullable type 
                // For backward compatibility, null DateTime defaults to DateTime.MinValue
                object columnValue = dr[column];
                if((columnValue is DBNull) &&
                    (null == Nullable.GetUnderlyingType(typeof(T))) &&
                    (typeof(T).IsValueType)) {
                    throw new InvalidCastException();
                }
                return DbUtils.Convert.To<T>(columnValue, default(T));
            } catch(MySqlConversionException) {

                // This check is needed to handle 0000-00-00 00:00:00 dates
                if(typeof(T) != typeof(DateTime))
                    throw;
                else
                    return default(T);
            }
        }

        internal static T Read<T>(this IDataReader dr, int columnIdx) {
            try {

                // Check if null was returned for a non-nullable type 
                // For backward compatibility, null DateTime defaults to DateTime.MinValue
                object columnValue = dr[columnIdx];
                if((columnValue is DBNull) &&
                    (null == Nullable.GetUnderlyingType(typeof(T))) &&
                    (typeof(T).IsValueType)) {
                    throw new InvalidCastException();
                }
                return DbUtils.Convert.To<T>(columnValue, default(T));
            } catch(MySqlConversionException) {

                // This check is needed to handle 0000-00-00 00:00:00 dates
                if(typeof(T) != typeof(DateTime))
                    throw;
                else
                    return default(T);
            }
        }

        internal static T Read<T>(this IDataReader dr, string column, T default_value) {
            try {
                return DbUtils.Convert.To<T>(dr[column], default_value);
            } catch (MySqlConversionException) {

                // This check is needed to handle 0000-00-00 00:00:00 dates
                if (typeof(T) != typeof(DateTime))
                    throw;
                else
                    return default(T);
            }
        }
    }
}

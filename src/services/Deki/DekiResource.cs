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
using System.Linq;
using System.Text;

namespace MindTouch.Deki {
    public class DekiResource {

        //--- Fields ---
        public readonly string LocalizationKey;
        public readonly object[] Args;

        //--- Constructors ---
        public DekiResource(string localizationKey, params object[] args) {
            if(localizationKey == null) {
                throw new ArgumentNullException("localizationKey");
            }
            LocalizationKey = localizationKey;
            Args = args ?? new object[0];
        }

        //--- Methods ---
        public override string ToString() {
            var builder = new StringBuilder();
            builder.Append("Resource[");
            builder.Append(LocalizationKey.ReplaceAll("&", "&amp;", "<", "&lt;", ">", "&gt;"));
            builder.Append("]");
            if(Args.Length > 0) {
                builder.Append("(");
                builder.Append(string.Join(",", Args.Select(x => x == null ? null : x.ToString()).ToArray()));
                builder.Append(")");
            }
            return builder.ToString();
        }
    }
}
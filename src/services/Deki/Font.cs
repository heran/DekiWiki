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
using System.Text;

namespace MindTouch.Deki {
    public class DekiFont {

        //--- Fields ---
        Dictionary<char, byte> _widths = new Dictionary<char, byte>();
        private byte _default_width;

        //--- Constructors ---
        public DekiFont(byte[] data) {
            if(data == null) {
                throw new ArgumentNullException("data");
            }
            if(data.Length < 8) {
                throw new ArgumentException("data buffer too small");
            }
            string magic = Encoding.ASCII.GetString(data, 0, 4);
            if(magic != "MTDF") {
                throw new ArgumentException("unknown data signature");
            }

            // read length and default width
            int length = (int)data[4] + ((int)data[5] << 8) + ((int)data[6] << 16);
            _default_width = data[7];

            // read all chars
            if(data.Length != (8 + 3 * length)) {
                throw new ArgumentException("data size mismatch");
            }
            for(int i = 0; i < length; ++i) {
                char c = (char)((char)data[8 + 3 * i] + (char)((char)data[8 + 3 * i + 1] << 8));
                _widths[c] = data[8 + 3 * i + 2];
            }
        }

        //--- Methods ---
        public string Truncate(string text, int max_width) {
            if(max_width <= 0) {
                throw new ArgumentNullException("max_width");
            }
            if(max_width == int.MaxValue) {
                return text;
            }
            StringBuilder result = new StringBuilder(text.Length);
            int total = 0;
            for(int i = 0; i < text.Length; ++i) {
                byte width;
                if(!_widths.TryGetValue(text[i], out width)) {
                    width = _default_width;
                }
                total += width;
                if(total > max_width) {
                    result.Append("...");
                    break;
                }
                result.Append(text[i]);
            }
            return result.ToString();
        }
    }
}

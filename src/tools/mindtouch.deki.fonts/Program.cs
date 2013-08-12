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
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace MindTouch.Deki.Tools {
    internal class FontsProgram {
        private static void Main(string[] args) {
            if(args.Length != 1) {
                Console.WriteLine("MindTouch Font, Copyright (c) 2006-2010 MindTouch Inc.");
                Console.WriteLine("USAGE: mindtouch.deki.font.exe <font>");
                Console.WriteLine("    font                    name of font to generate size file for");
                return;
            }
            using(Font font = new Font(args[0], 64.0f)) {
                ComputeCharWidth(font);
            }
        }

        private static void ComputeCharWidth(Font font) {
            Console.WriteLine("Generating character sizes for {0} font", font.Name);
            Dictionary<char, byte> widths = new Dictionary<char, byte>();
            byte default_width = (byte)TextRenderer.MeasureText("\0", font).Width;
            for(char c = char.MinValue; c < char.MaxValue; ++c) {
                byte width = (byte)TextRenderer.MeasureText(c.ToString(), font).Width;
                if(width != default_width) {
                    widths[c] = width;
                }
            }
            Console.WriteLine("{0} non-default chars found", widths.Count);
            List<char> chars = new List<char>(widths.Keys);
            chars.Sort();
            string filename = font.Name + ".mtdf";
            using(FileStream file = File.OpenWrite(filename)) {
                byte[] magic = Encoding.ASCII.GetBytes("MTDF"); // MindTouch Deki Font
                byte[] length = new byte[4];
                byte[] char_buffer = Encoding.Unicode.GetBytes(chars.ToArray());
                byte[] buffer = new byte[3 * chars.Count];
                length[0] = (byte)(widths.Count >> 0);
                length[1] = (byte)(widths.Count >> 8);
                length[2] = (byte)(widths.Count >> 16);
                length[3] = default_width;
                for(int i = 0; i < chars.Count; ++i) {
                    buffer[3 * i] = (byte)(chars[i] >> 0);
                    buffer[3 * i + 1] = (byte)(chars[i] >> 8);
                    buffer[3 * i + 2] = widths[chars[i]];
                }
                file.Write(magic, 0, magic.Length);
                file.Write(length, 0, length.Length);
                file.Write(buffer, 0, buffer.Length);
            }
            Console.WriteLine("'{0}' file written", filename);
        }
    }
}

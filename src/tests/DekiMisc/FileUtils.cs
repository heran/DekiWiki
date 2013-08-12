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
using System.IO;

using MindTouch.Dream;
using NUnit.Framework;

namespace MindTouch.Deki.Tests
{
    public static class FileUtils
    {
        public static byte[] GenerateRandomContent()
        {
            Random rnd = new Random();
            byte[] data = new byte[rnd.Next(Utils.Settings.SizeOfSmallContent, Utils.Settings.SizeOfBigContent)];
            rnd.NextBytes(data);

            return data;
        }

        public static string CreateRamdomFile(byte[] content)
        {
            string fileName = System.IO.Path.GetTempFileName();

            return CreateFile(content, fileName);
        }

        public static string CreateFile(byte[] content, string fileName)
        {
            using (FileStream writer = File.Create(fileName))
            {
                byte[] data = content ?? GenerateRandomContent();
                writer.Write(data, 0, data.Length);
            }

            return fileName;
        }

        public static DreamMessage UploadRandomFile(Plug p, string pageid, out string fileid)
        {
            string filename = null;

            return UploadRandomFile(p, pageid, out fileid, out filename);
        }

        public static DreamMessage UploadRandomFile(Plug p, string pageid, out string fileid, out string filename)
        {
            return UploadRandomFile(p, pageid, null, string.Empty, out fileid, out filename);
        }

        public static DreamMessage UploadRandomFile(Plug p, string pageid, byte[] content, string description, out string fileid, out string filename)
        {
            filename = FileUtils.CreateRamdomFile(content);
            DreamMessage msg = UploadFile(p, pageid, description, out fileid, filename);
            filename = msg.ToDocument()["filename"].AsText;
            Assert.IsFalse(string.IsNullOrEmpty(filename));

            return msg;
        }

        public static DreamMessage UploadFile(Plug p, string pageid, string description, out string fileid, string filename)
        {
            DreamMessage msg = DreamMessage.FromFile(filename);
            filename = XUri.DoubleEncode(System.IO.Path.GetFileName(filename));
            if (string.IsNullOrEmpty(description))
                msg = p.At("pages", pageid, "files", "=" + filename).Put(msg);
            else
                msg = p.At("pages", pageid, "files", "=" + filename).With("description", description).Put(msg);
            Assert.AreEqual(DreamStatus.Ok, msg.Status);
            fileid = msg.ToDocument()["@id"].AsText;
            Assert.IsFalse(string.IsNullOrEmpty(fileid));

            return msg;
        }

        public static DreamMessage DeleteFile(Plug p, string fileid)
        {
            DreamMessage msg = p.At("files", fileid).Delete();
            Assert.AreEqual(DreamStatus.Ok, msg.Status);

            return msg;
        }

        public static DreamMessage UploadRandomImage(Plug p, string pageid, out string fileid, out string filename)
        {
            filename = Utils.GenerateUniqueName() + ".png";
            System.Drawing.Bitmap pic = new System.Drawing.Bitmap(100, 100);
            using (System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(pic))
                g.DrawRectangle(System.Drawing.Pens.Blue, 10, 10, 80, 80);
            System.IO.MemoryStream stream = new System.IO.MemoryStream();
            pic.Save(stream, System.Drawing.Imaging.ImageFormat.Png);

            p = p.At("pages", pageid, "files", "=" + filename).WithQuery("description=random image");
            DreamMessage msg = p.Put(DreamMessage.Ok(MimeType.PNG, stream.ToArray()));
            Assert.AreEqual(DreamStatus.Ok, msg.Status);
            fileid = msg.ToDocument()["@id"].AsText;

            return msg;
        }
    }
}

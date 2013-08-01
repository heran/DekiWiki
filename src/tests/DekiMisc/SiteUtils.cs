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
using System.Globalization;
using System.Collections.Generic;
using System.Text;

using MindTouch.Dream;
using MindTouch.Dream.Test;
using MindTouch.Xml;
using MindTouch.Tasking;

using NUnit.Framework;

namespace MindTouch.Deki.Tests 
{
    public static class SiteUtils 
    {
        public static void AddConfigKey(Plug p, string key, string value) {

            if (key == null || value == null || key == String.Empty || value == String.Empty) {
                Assert.Fail("Bad key/value");
            }

            // Retrieve existing keys
            DreamMessage msg = RetrieveConfig(p);

            // Add/Replace key/value pair to config
            XDoc config = msg.ToDocument();
            if (config[key].IsEmpty) {
                config.InsertValueAt(key, value);
            } else {
                config[key].ReplaceValue(value);
            }

            // Save new config
            SaveConfig(p, config);
        }

        public static void RemoveConfigKey(Plug p, string key) {

            if (key == null || key == String.Empty) {
                Assert.Fail("Bad key");
            }

            // Retrieve existing keys
            DreamMessage msg = RetrieveConfig(p);

            // Remove key from config
            XDoc config = msg.ToDocument();
            if (config[key].IsEmpty) {
                return;
            }

            config[key].Remove();

            // Save new config
            SaveConfig(p, config);
        }

        public static DreamMessage SaveConfig(Plug p, XDoc config) 
        {
            DreamMessage msg = p.At("site", "settings").Put(config, new Result<DreamMessage>()).Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Failed to update site settings");
            return msg;
        }

        public static DreamMessage RetrieveConfig(Plug p)
        {
            DreamMessage msg = p.At("site", "settings").Get(new Result<DreamMessage>()).Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Failed to retrieve site settings");
            return msg;
        }
    }
}
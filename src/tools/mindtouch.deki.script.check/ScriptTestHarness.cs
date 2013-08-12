/*
 * MindTouch Core - open source enterprise collaborative networking
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

using System.Collections.Generic;

using MindTouch.Dream;
using MindTouch.Dream.Test;
using MindTouch.Xml;

namespace MindTouch.Deki.Script {
    public class ScriptTestHarness {

        //--- Constants ---
        private const string SCRIPT_TEST_ROOT = "script-test";
        private const string REGISTER = "register";
        private const string EXECUTE = "execute";

        //--- Fields ---
        private readonly DreamHostInfo _hostInfo;

        //--- Constructors ---
        public ScriptTestHarness() {
            _hostInfo = DreamTestHelper.CreateRandomPortHost();
            _hostInfo.Host.Self.At("load").With("name", "mindtouch.deki.services").Post(DreamMessage.Ok());
            _hostInfo.Host.Self.At("load").With("name", "mindtouch.deki").Post(DreamMessage.Ok());
            _hostInfo.Host.Self.At("load").With("name", "mindtouch.deki.script.check").Post(DreamMessage.Ok());
            XDoc config = new XDoc("config")
                .Elem("sid", "sid://mindtouch.com/2008/09/script-test")
                .Elem("path", SCRIPT_TEST_ROOT)
                .Elem("debug", true);
            _hostInfo.Host.Self.At("services").Post(config);
        }

        //--- Methods ---
        public XDoc LoadExtension(string path) {
            DreamMessage registrationMessage = Plug
                .New(_hostInfo.Host.LocalMachineUri)
                .At(SCRIPT_TEST_ROOT, REGISTER)
                .WithParams(new KeyValuePair<string, string>[] { new KeyValuePair<string, string>("service-path", path) })
                .Post();
            XDoc manifest = registrationMessage.ToDocument();
            return manifest;
        }

        public string Execute(string expression) {
            DreamMessage executionMessage = Plug
             .New(_hostInfo.Host.LocalMachineUri)
             .At(SCRIPT_TEST_ROOT, EXECUTE)
             .WithParams(new KeyValuePair<string, string>[] { new KeyValuePair<string, string>("expression", expression) })
             .Post();

            return executionMessage.ToText();
        }
    }
}

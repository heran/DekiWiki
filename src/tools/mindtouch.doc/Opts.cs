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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MindTouch.Deki;
using MindTouch.Dream;
using MindTouch.Xml;

namespace Mindtouch.Tools.Documentation {
    public enum Mode {
        Import,
        Generate,
    }

    public class Opts {
        public static string[] Usage = new[] {
            "USAGE: mindtouch.doc.exe [options] [assembly1 .. assemblyN]",
            "",
            "Options:",
            "    (Options must be prefixed by a '-' or '/')",
            "",
            "    General:",
            "      ?|usage                  - display this message",
            "      h|host <host>            - MindTouch host (assumes standard API location)",
            "      u|uri <api-uri>          - specify the full uri to the API (instead of using <host>)",
            "      I|importreltopath <path> - relative uri path for import",
            "      importrelto <id>         - relative page id for import (alternative to importreltopath)",
            "      o|output                 - output path for generated documentation (will skip import)",
            "      R|retries                - Maximum number of retries on import/export item failures (default: 3)",
            "",
            "    Authentication:",
            "      (if no authentication is provided, the program will prompt for user and password interactively)",
            "      A|authtoken <token>      - authtoken to use for user authentication",
            "      U|user <username>        - username for authentication (requires password option",
            "      P|password <password>    - password for username authentcation",
            ""
        };

        public Opts(string[] argArray) {
            var args = new List<string>(argArray);
            var index = 0;
            bool doneWithOptions = false;
            while(index < args.Count) {
                if(WantUsage) {
                    break;
                }
                string key = args[index];
                string value = (index + 1 >= args.Count) ? null : args[index + 1];
                if(!doneWithOptions && (key.StartsWith("-") || key.StartsWith("/"))) {
                    key = key.Remove(0, 1);
                    switch(key) {
                    case "A":
                    case "authtoken":
                        AuthToken = value;
                        index++;
                        break;
                    case "h":
                    case "host":
                        DekiApi = Plug.New(string.Format("http://{0}/@api/deki", value));
                        index++;
                        break;
                    case "I":
                    case "importreltopath":
                        ImportReltoPath = Title.FromUIUri(null, value).AsPrefixedDbPath();
                        index++;
                        break;
                    case "importrelto":
                        ImportRelto = int.Parse(value);
                        index++;
                        break;
                    case "o":
                    case "output":
                        OutputPath = value;
                        Mode = Mode.Generate;
                        index++;
                        break;
                    case "P":
                    case "password":
                        Password = value;
                        index++;
                        break;
                    case "R":
                    case "retries":
                        Retries = int.Parse(value);
                        index++;
                        break;
                    case "U":
                    case "user":
                        User = value;
                        index++;
                        break;
                    case "u":
                    case "uri":
                        DekiApi = Plug.New(value);
                        index++;
                        break;
                    case "?":
                    case "usage":
                        WantUsage = true;
                        break;
                    case "t":
                        Test = true;
                        break;
                    default:
                        throw new ConfigurationException("Unknown option {0}", key);
                    }
                } else {
                    doneWithOptions = true;
                    Assemblies.Add(key);
                }
                index++;
            }
            if(WantUsage) {
                return;
            }
            if(!Assemblies.Any()) {
                throw new ConfigurationException("Missing {0} Directory", Mode);
            }
        }

        public bool WantUsage;
        public Mode Mode = Mode.Import;
        public Plug DekiApi;
        public string ImportReltoPath = "doc";
        public int? ImportRelto;
        public string User;
        public string Password;
        public string AuthToken;
        public bool Test;
        public int Retries = 3;
        public readonly List<string> Assemblies = new List<string>();
        public string OutputPath = Path.Combine(Path.GetTempPath(), StringUtil.CreateAlphaNumericKey(6));
    }
}

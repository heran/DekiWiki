/*
 * MindTouch MediaWiki Converter
 * Copyright (C) 2006-2008 MindTouch, Inc.
 * www.mindtouch.com  oss@mindtouch.com
 *
 * For community documentation and downloads visit www.opengarden.org;
 *  please review the licensing section.
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
using System.Text;

using MindTouch.Dream;
using MindTouch.Deki.Converter;
using MindTouch.Tools;

namespace MindTouch.Deki.Test {
    public class Program {

        //--- Types ---
        internal class TestCase {

            //--- Fields ---
            internal readonly int Line;
            internal readonly Dictionary<string, string> Settings;
            internal readonly string Test;
            internal readonly string[] Expected;

            //--- Constructors ---
            internal TestCase(int line, Dictionary<string, string> settings, string test, string[] expected) {
                this.Line = line;
                this.Settings = settings;
                this.Test = test;
                this.Expected = expected;
            }

            //--- Methods ---
            internal bool RunTest(Plug converter) {
                foreach(KeyValuePair<string, string> setting in Settings) {
                    converter = converter.With(setting.Key, setting.Value);
                }
                DreamMessage message = converter.With("text", Test).PostQuery();
                string result = message.AsText();
                string[] received = result.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None);
                XDoc converted = XDocFactory.From(result, MimeType.HTML);
                Site site = new Site();
                string lang;
                if(Settings.TryGetValue("lang", out lang)) {
                    site.Language = lang;
                }
                string title;
                if(!Settings.TryGetValue("title", out title)) {
                    title = "None";
                }                
                WikiTextProcessor.Convert(site, converted, StringUtil.StartsWithInvariantIgnoreCase(title, "Template:"));
                string[] actual = converted.ToPrettyString().Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None);
                bool success = true;
                if(actual.Length == Expected.Length) {
                    for(int i = 0; i < Expected.Length; ++i) {
                        if(actual[i] != Expected[i]) {
                            success = false;
                            break;
                        }
                    }
                } else {
                    success = false;
                }
                if(!success) {
                    string bug;
                    if(Settings.TryGetValue("bug", out bug)) {
                        Console.WriteLine("Line {0}, Bug# {1}", Line, bug);
                    } else {
                        Console.WriteLine("Line {0}", Line);
                    }
                    Console.WriteLine();
                    Console.WriteLine("Sent");
                    Console.WriteLine("----");
                    Console.WriteLine(Test);
                    Console.WriteLine();
                    Console.WriteLine("Received");
                    Console.WriteLine("--------");
                    foreach(string line in received) {
                        Console.WriteLine(line);
                    }
                    Console.WriteLine();
                    Console.WriteLine("Converted");
                    Console.WriteLine("---------");
                    Console.WriteLine(converted.ToPrettyString());
                    Console.WriteLine();
                    Console.WriteLine("Expected");
                    Console.WriteLine("--------");
                    foreach(string line in Expected) {
                        Console.WriteLine(line);
                    }
                    Console.WriteLine();
                    Console.WriteLine("========================================");
                    return false;
                }
                return true;
            }
        }

        //--- Class Methods ---
        private static void Main(string[] args) {
            if(args.Length < 2) {
                ShowUsage();
                return;
            }
            Plug converter = Plug.New(args[0]);
            string[] lines = File.ReadAllLines(args[1]);
            int index = 0;
            int count = 0;
            int success = 0;
            while(true) {
                TestCase test = ParseTest(lines, ref index);
                if(test == null) {
                    break;
                }
                ++count;
                if(test.RunTest(converter)) {
                    ++success;
                }
            }
            Console.WriteLine("{0} tests: {1} failed, {2} succeeded", count, (count - success), success);
        }

        private static void ShowUsage() {
            Console.WriteLine("USAGE: mindtouch.deki.mwconverter.test URI TEST-CASES");
        }

        private static TestCase ParseTest(string[] lines, ref int index) {

            // skip empty lines
            while((index < lines.Length) && string.IsNullOrEmpty(lines[index].Trim())) {
                ++index;
            }
            if(index >= lines.Length) {
                return null;
            }
            int start = index;

            // check first line is valid
            if(!lines[index].StartsWith("%%")) {
                Console.WriteLine("ERROR (line {0}): expected line to start with %%", index);
                return null;
            }

            // check if line is an exit statement
            if(lines[index].StartsWith("%%%")) {
                return null;
            }

            // read test settings
            Dictionary<string, string> settings = HttpUtil.ParseNameValuePairs(lines[index].Substring(2));
            ++index;

            // read test
            StringBuilder test = new StringBuilder();
            while((index < lines.Length) && !lines[index].StartsWith("%%")) {
                test.AppendLine(lines[index]);
                ++index;
            }

            // check if we reached the end of the file
            if(index++ >= lines.Length) {
                Console.WriteLine("ERROR (line {0}): test result missing", index);
                return null;
            }

            // read test result
            List<string> testResult = new List<string>();
            while((index < lines.Length) && !lines[index].StartsWith("%%")) {
                testResult.Add(lines[index]);
                ++index;
            }

            // return test case
            return new TestCase(start + 1, settings, test.ToString(), testResult.ToArray());
        }
    }
}

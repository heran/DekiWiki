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
using System.IO;

using MindTouch.Dream;
using MindTouch.Xml;

namespace MindTouch.Deki.Script {
    class Program {

        //--- Class Methods ---
        static void Main(string[] args) {

            // check supplied arguments
            if(args.Length == 0) {
                Console.WriteLine("Missing extension path");
                PrintUsage();
                return;
            }
            string extension = args[0];
            string expression = (args.Length > 1) ? args[1] : null;
            try {
                if(!extension.ToLowerInvariant().StartsWith("http")) {
                    if(extension[1] != ':') {
                        extension = Environment.CurrentDirectory + Path.DirectorySeparatorChar + extension;
                    }
                    if(!File.Exists(extension)) {
                        Console.WriteLine("Specified extension not found: {0}", extension);
                        PrintUsage();
                        return;
                    }
                }
            } catch(Exception) {
                Console.WriteLine("Unable to parse extension path: {0}", args[0]);
                PrintUsage();
                return;
            }

            // validate script extension
            ScriptManifestValidator validator = new ScriptManifestValidator();
            ScriptManifestValidationResult validationResult = validator.Validate(extension);
            if(validationResult.IsInvalid) {
                Console.WriteLine("The script has some errors:");
                Console.WriteLine(validationResult.ValidationErrors);
                return;
            }


            // initialize script test service
            ScriptTestHarness harness = new ScriptTestHarness();
            XDoc manifest = null;
            try {
                manifest = harness.LoadExtension(extension);
            } catch(DreamResponseException e) {
                Console.WriteLine("Unable to load script from '{0}': ", extension);
                Console.WriteLine(e.Response.ToText());
                return;
            }

            // list functions
            if(expression == null) {
                Console.WriteLine("Script loaded successfully");
                Console.WriteLine("Available functions:");
                string ns = manifest["namespace"].AsText;
                foreach(XDoc function in manifest["function"]) {
                    string name = function["name"].AsText;
                    Console.WriteLine("  {0}", string.IsNullOrEmpty(ns) ? name : ns + "." + name);
                }
            } else {

                // or execute passed DekiScript
                try {
                    string result = harness.Execute(expression);
                    Console.WriteLine("Script executed successfully:");
                    Console.WriteLine("Script: {0}", expression);
                    Console.WriteLine("Result:");
                    Console.WriteLine(result);
                } catch(DreamResponseException e) {
                    Console.WriteLine("Unable to execute script '{0}': ", expression);
                    Console.WriteLine(e);
                }
            }
        }

        private static void PrintUsage() {
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("  mindtouch.deki.extension_tester.exe <script_path> [<expression>]");
            Console.WriteLine();
            Console.WriteLine("  where: <script_path> can be file or http path to the Extension manifest");
            Console.WriteLine("         <expression> is the DekiScript expression to call");
            Console.WriteLine();
        }
    }
}

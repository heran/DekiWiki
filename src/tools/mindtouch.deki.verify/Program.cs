/*
 * MindTouch Core - open source enterprise collaborative networking
 * Copyright (c) 2006-2010 MindTouch Inc.
 * www.mindtouch.com  oss@mindtouch.com
 *
 * For community documentation and downloads visit wiki.developer.mindtouch.com;
 * please review the licensing section.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 *     http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using MindTouch.Deki.Util;

namespace MindTouch.Deki.Veriy {
    internal class Program {

        //--- Class Fields ---
        private static bool _allowInvalidSsl;

        //--- Class Methods ---
        private static int Main(string[] args) {
            if(args.Length == 0) {
                Usage();
                return -1;
            }
            int status = 0;
            bool verbose = false;

            // setup callback to allow invalid SSL certiicates
            System.Net.ServicePointManager.ServerCertificateValidationCallback += delegate {
                return _allowInvalidSsl;
            };

            // loop over all arguments
            foreach(string arg in args) {

                // check if argument is an option
                if(arg.StartsWith("--")) {
                    switch(arg) {
                    case "--verbose":
                        verbose = true;
                        break;
                    case "--allow-invalid-ssl":
                        _allowInvalidSsl = true;
                        break;
                    default:
                        Console.WriteLine("ERROR: unknown command line option '{0}'", arg);
                        return -1;
                    }
                    continue;
                }

                // process host license
                if(verbose) {
                    Console.WriteLine("Validating: {0}", arg);
                }
                status = 0;
                try {
                    DekiLicense.Validate(arg);
                } catch(DekiLicenseException e) {
                    Console.WriteLine(e.Message);
                    status = (int)e.Reason;
                } catch(Exception e) {
                    Console.WriteLine("UNEXPECTED ERROR: " + e);
                    status = -2;
                }
                if(status == 0) {
                    Console.WriteLine("The server license is valid.");
                }
                if(verbose) {
                    Console.WriteLine("Status: {0}", status);
                    Console.WriteLine();
                }

            }
            return status;
        }

        private static void Usage() {
            Console.Error.WriteLine("MindTouch Verify, Copyright (c) 2006-2010 MindTouch Inc.");
            Console.Error.WriteLine("USAGE: mindtouch.deki.verify.exe options server-uri");
            Console.Error.WriteLine("    --verbose               Show additional information during validation");
            Console.Error.WriteLine("    --allow-invalid-ssl     Allow server uri to have an invalid SSL certificate");
            Console.Error.WriteLine("    server-uri              MindTouch server uri");
            Console.Error.WriteLine();
            Console.Error.WriteLine("Exit codes:");
            Console.Error.WriteLine("    0                       Server license is valid");
            Console.Error.WriteLine("    1                       Invalid server license signature");
            Console.Error.WriteLine("    2                       Invalid server license type");
            Console.Error.WriteLine("    3                       Expired server license");
            Console.Error.WriteLine("    4                       Provided server name does not match any of the licensed server names");
            Console.Error.WriteLine("    5                       Invalid uri");
            Console.Error.WriteLine("    6                       Invalid license");
            Console.Error.WriteLine("    7                       Unable to retrieve server license (status: 404, 405, or 500)");
            Console.Error.WriteLine("    8                       Unable to retrieve server license (expected MIME type: application/xml)");
            Console.Error.WriteLine("    -1                      Invalid command line option");
            Console.Error.WriteLine("    -2                      Unexpected error");
        }
    }
}

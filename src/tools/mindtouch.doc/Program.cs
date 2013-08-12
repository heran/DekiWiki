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
using System.Threading;
using MindTouch.Deki;
using MindTouch.Documentation.Util;
using MindTouch.Deki.Import;
using MindTouch.Dream;
using MindTouch.Tasking;

namespace Mindtouch.Tools.Documentation {
    class Program {

        //--- Methods ---
        static int Main(string[] args) {
            Console.WriteLine("MindTouch Documentation, Copyright (c) 2010 MindTouch Inc.");
            if(args.Length == 0) {
                return ExitWithUsage(-1);
            }
            try {
                var opts = new Opts(args);
                if(opts.WantUsage) {
                    return ExitWithUsage(0);
                }
                switch(opts.Mode) {
                case Mode.Import:
                    Import(opts);
                    break;
                case Mode.Generate:
                    Generate(opts);
                    break;
                }
            } catch(ConfigurationException e) {
                Console.WriteLine("CONFIG ERROR: {0}", e.Message);
                Console.WriteLine();
                return ExitWithUsage(-1);
            } catch(Exception e) {
                Console.WriteLine("ERROR: {0}", e.GetCoroutineStackTrace());
                Console.WriteLine();
                return -1;
            }
            return 0;
        }

        private static void Authenticate(Opts opts) {
            if(opts.DekiApi == null) {
                throw new ConfigurationException("must specify either 'host' or 'uri'");
            }
            Plug authPlug = opts.DekiApi;
            if((string.IsNullOrEmpty(opts.User) || string.IsNullOrEmpty(opts.Password)) && string.IsNullOrEmpty(opts.AuthToken)) {
                if(string.IsNullOrEmpty(opts.User)) {
                    Console.Write("User: ");
                    opts.User = Console.ReadLine();
                }
                if(string.IsNullOrEmpty(opts.Password)) {
                    Console.Write("Password: ");
                    opts.Password = Console.ReadLine();
                }
            }
            if(opts.Test) {
                return;
            }
            Console.WriteLine("Authenticating...");
            authPlug = !string.IsNullOrEmpty(opts.AuthToken) ? authPlug.With("X-Authtoken", opts.AuthToken) : authPlug.WithCredentials(opts.User, opts.Password);
            DreamMessage response = authPlug.At("users", "authenticate").PostAsync().Wait();
            if(!response.IsSuccessful) {
                throw new Exception("Unable to authenticate");
            }
        }

        private static void Generate(Opts opts) {
            if(opts.Test) {
                return;
            }
            Console.WriteLine("Generating Documentation...");
            var builder = new HtmlDocumentationBuilder();
            opts.Assemblies.ForEach(builder.AddAssembly);
            builder.BuildDocumenationPackage(opts.OutputPath);
        }

        private static void Import(Opts opts) {
            Authenticate(opts);
            Generate(opts);
            var managerResult = opts.ImportRelto.HasValue
                ? ImportManager.CreateFileImportManagerAsync(opts.DekiApi, opts.ImportRelto.Value, opts.OutputPath, new Result<ImportManager>())
                : ImportManager.CreateFileImportManagerAsync(opts.DekiApi, opts.ImportReltoPath, opts.OutputPath, new Result<ImportManager>());
            managerResult.Block();
            if(managerResult.HasException) {
                Directory.Delete(opts.OutputPath, true);
                throw new Exception(string.Format("Import failed: {0}", managerResult.Exception.Message), managerResult.Exception);
            }
            ImportManager manager = managerResult.Value;
            manager.MaxRetries = opts.Retries;
            Result result = manager.ImportAsync(new Result());
            int completed = 0;
            Console.WriteLine("Importing Documentation...");
            while(!result.HasFinished) {
                Thread.Sleep(200);
                if(manager.CompletedItems <= completed) {
                    continue;
                }
                Console.WriteLine("  {0}/{1}", manager.CompletedItems, manager.TotalItems);
                completed = manager.CompletedItems;
            }
            Directory.Delete(opts.OutputPath, true);
            if(result.HasException) {
                var importException = result.Exception as ImportException;
                if(importException != null) {
                    Console.WriteLine("Import failed on Item:\r\n{0}", importException.ManifestItem.ToPrettyString());
                }
                throw new Exception(string.Format("Import failed: {0}", result.Exception.Message), result.Exception);
            }
        }

        private static int ExitWithUsage(int code) {
            Console.WriteLine();
            foreach(string line in Opts.Usage) {
                Console.WriteLine(line);
            }
            return code;
        }
    }
}
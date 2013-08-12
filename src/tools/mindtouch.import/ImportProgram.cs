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
using System.Text;
using System.Threading;

using MindTouch.Deki.Export;
using MindTouch.Deki.Import;
using MindTouch.Dream;
using MindTouch.IO;
using MindTouch.Tasking;
using MindTouch.Xml;

namespace MindTouch.Deki.Tools.Import {

    class ImportProgram {

        //--- Class Methods ---
        private static int Main(string[] args) {
            Console.WriteLine("MindTouch Import, Copyright (c) 2010 MindTouch Inc.");
            if(args.Length == 0) {
                return ExitWithUsage(-1);
            }
            Opts opts = new Opts();
            try {
                opts = new Opts(args);
                if(opts.WantUsage) {
                    return ExitWithUsage(0);
                }
                if(opts.GenConfig) {
                    opts.WriteConfig();
                    return 0;
                }
                Authenticate(opts);
                if(opts.TestAuth) {
                    return 0;
                }
                switch(opts.Mode) {
                case Mode.Import:
                    Import(opts);
                    break;
                case Mode.Export:
                    Export(opts);
                    break;
                case Mode.Copy:
                    Copy(opts);
                    break;
                }
            } catch(ConfigurationException e) {
                Console.WriteLine();
                Console.WriteLine("CONFIG ERROR: {0}", e.Message);
                Console.WriteLine();
                return ExitWithUsage(-1);
            } catch(Exception e) {
                Console.WriteLine();
                if(opts.Verbose) {
                    Console.WriteLine("ERROR: {0}", e.GetCoroutineStackTrace());
                } else {
                    Console.WriteLine("ERROR: {0}", e.Message);
                }
                Console.WriteLine();
                return -1;
            }
            return 0;
        }

        private static void Authenticate(Opts opts) {
            if(opts.DekiApi == null) {
                Console.Write("Host: ");
                var value = Console.ReadLine();
                if(!value.StartsWithInvariantIgnoreCase("http://") && !value.StartsWithInvariantIgnoreCase("https://")) {
                    opts.DekiApi = Plug.New(string.Format("http://{0}/@api/deki", value));
                } else {
                    opts.DekiApi = Plug.New(new XUri(value).At("@api", "deki"));
                }
            }
            Plug authPlug = opts.DekiApi;
            if((string.IsNullOrEmpty(opts.User) || string.IsNullOrEmpty(opts.Password)) && string.IsNullOrEmpty(opts.AuthToken)) {
                if(string.IsNullOrEmpty(opts.User)) {
                    Console.Write("User: ");
                    opts.User = Console.ReadLine();
                }
                if(string.IsNullOrEmpty(opts.Password)) {
                    Console.Write("Password: ");
                    opts.Password = ReadPassword();
                }
            }
            if(opts.Test) {
                return;
            }
            authPlug = !string.IsNullOrEmpty(opts.AuthToken) ? authPlug.With("X-Authtoken", opts.AuthToken) : authPlug.WithCredentials(opts.User, opts.Password);
            DreamMessage response = authPlug.At("users", "authenticate").PostAsync().Wait();
            if(!response.IsSuccessful) {
                throw new Exception("Unable to authenticate");
            }
        }

        private static void Import(Opts opts) {
            bool createdTempFile = false;
            if(opts.FilePath.StartsWith("http:") || opts.FilePath.StartsWith("https:")) {
                DreamMessage packageMessage = Plug.New(opts.FilePath).InvokeEx("GET", DreamMessage.Ok(), new Result<DreamMessage>()).Wait();
                if(!packageMessage.IsSuccessful) {
                    throw new Exception(string.Format("Unable to download package from '{0}'", opts.FilePath));
                }
                opts.FilePath = Path.GetTempFileName();
                opts.Archive = true;
                createdTempFile = true;
                using(Stream tempStream = File.Create(opts.FilePath)) {
                    packageMessage.ToStream().CopyTo(tempStream, packageMessage.ContentLength, new Result<long>()).Wait();
                }
            }
            IPackageReader packageReader;
            if(opts.Archive) {
                if(!File.Exists(opts.FilePath)) {
                    throw new ConfigurationException("No such file: {0}", opts.FilePath);
                }
                if(opts.Test) {
                    return;
                }
                packageReader = new ArchivePackageReader(opts.FilePath);
            } else {
                if(!Directory.Exists(opts.FilePath)) {
                    throw new ConfigurationException("No such directory: {0}", opts.FilePath);
                }
                if(opts.Test) {
                    return;
                }
                packageReader = new FilePackageReader(opts.FilePath);
            }
            ImportManager manager;
            try {
                var manifest = packageReader.ReadManifest(new Result<XDoc>()).Wait();
                FixupManifest(manifest, opts);
                var forceOverwrite = !(opts.PreserveLocalChanges ?? true);
                var importer = opts.ImportRelto.HasValue
                    ? Importer.CreateAsync(opts.DekiApi, manifest, opts.ImportRelto.Value, forceOverwrite, new Result<Importer>()).Wait()
                    : Importer.CreateAsync(opts.DekiApi, manifest, opts.ImportReltoPath, forceOverwrite, new Result<Importer>()).Wait();
                manager = new ImportManager(importer, packageReader);
            } catch(Exception e) {
                if(createdTempFile) {
                    File.Delete(opts.FilePath);
                }
                throw new Exception(string.Format("Import failed: {0}", e.Message), e);
            }
            manager.MaxRetries = opts.Retries;
            Result result = manager.ImportAsync(new Result());
            int completed = 0;
            Console.WriteLine("Importing:");
            while(!result.HasFinished) {
                Thread.Sleep(200);
                if(manager.CompletedItems <= completed) {
                    continue;
                }
                if(SysUtil.IsUnix) {
                    Console.WriteLine("  {0} of {1} files ({2:0}%)", manager.CompletedItems, manager.TotalItems, 100.0 * manager.CompletedItems / manager.TotalItems);
                } else {
                    Console.Write("  {0} of {1} files ({2:0}%)      \r", manager.CompletedItems, manager.TotalItems, 100.0 * manager.CompletedItems / manager.TotalItems);
                }
                completed = manager.CompletedItems;
            }
            if(!SysUtil.IsUnix) {
                Console.WriteLine();
            }
            if(createdTempFile) {
                File.Delete(opts.FilePath);
            }
            if(result.HasException) {
                ImportException importException = result.Exception as ImportException;
                if(importException != null) {
                    Console.WriteLine("Import failed on Item:\r\n{0}", importException.ManifestItem.ToPrettyString());
                }
                throw new Exception(string.Format("Import failed: {0}", result.Exception.Message), result.Exception);
            }
        }

        private static void Export(Opts opts) {
            Result<ExportManager> managerResult;
            if(opts.Archive) {
                if(opts.Test) {
                    return;
                }
                managerResult = opts.ExportRelto.HasValue
                    ? ExportManager.CreateArchiveExportManagerAsync(opts.DekiApi, opts.ExportDocument, opts.ExportRelto.Value, opts.FilePath, new Result<ExportManager>())
                    : ExportManager.CreateArchiveExportManagerAsync(opts.DekiApi, opts.ExportDocument, opts.ExportReltoPath, opts.FilePath, new Result<ExportManager>());
            } else {
                if(!Directory.Exists(opts.FilePath)) {
                    try {
                        Directory.CreateDirectory(opts.FilePath);
                    } catch(Exception e) {
                        throw new ConfigurationException(string.Format("Unable to create '{0}': {1}", opts.FilePath, e.Message), e);
                    }
                }
                if(opts.Test) {
                    return;
                }
                managerResult = opts.ExportRelto.HasValue
                    ? ExportManager.CreateFileExportManagerAsync(opts.DekiApi, opts.ExportDocument, opts.ExportRelto.Value, opts.FilePath, new Result<ExportManager>())
                    : ExportManager.CreateFileExportManagerAsync(opts.DekiApi, opts.ExportDocument, opts.ExportReltoPath, opts.FilePath, new Result<ExportManager>());
            }
            managerResult.Block();
            if(managerResult.HasException) {
                throw new Exception(string.Format("Export failed: {0}", managerResult.Exception.Message), managerResult.Exception);
            }
            ExportManager manager = managerResult.Value;
            manager.MaxRetries = opts.Retries;
            Result result = manager.ExportAsync(manifest => FixupManifest(manifest, opts), new Result());
            int completed = 0;
            Console.WriteLine("Exporting: {0}", opts.FilePath);
            if(manager.TotalItems == 0) {
                throw new Exception("nothing to export");
            }
            while(!result.HasFinished) {
                Thread.Sleep(200);
                if(manager.CompletedItems <= completed) {
                    continue;
                }
                if(SysUtil.IsUnix) {
                    Console.WriteLine("  {0} of {1} files ({2:0}%)", manager.CompletedItems, manager.TotalItems, 100.0 * manager.CompletedItems / manager.TotalItems);
                } else {
                    Console.Write("  {0} of {1} files ({2:0}%)      \r", manager.CompletedItems, manager.TotalItems, 100.0 * manager.CompletedItems / manager.TotalItems);
                }
                completed = manager.CompletedItems;
            }
            if(!SysUtil.IsUnix) {
                Console.WriteLine();
            }
            if(result.HasException) {
                ExportException exportException = result.Exception as ExportException;
                if(exportException != null) {
                    Console.WriteLine("Export failed on Item:\r\n{0}", exportException.ManifestItem.ToPrettyString());
                }
                throw new Exception(string.Format("Export failed: {0}", result.Exception.Message), result.Exception);
            }
        }

        private static void FixupManifest(XDoc manifest, Opts opts) {
            if(opts.ImportOnce) {
                manifest.Attr("import-once", true);
            }
            if(opts.InitOnly) {
                manifest.Attr("init-only", true);
            }
            if(manifest["@preserve-local"].IsEmpty) {
                manifest.Attr("preserve-local", opts.PreserveLocalChanges ?? false);
            } else {
                manifest["@preserve-local"].ReplaceValue(opts.PreserveLocalChanges ?? false);
            }
            if(opts.Restriction != Restriction.Default) {
                manifest.Start("security")
                    .Start("permissions.page")
                        .Elem("restriction", GetRestrictionString(opts.Restriction))
                    .End()
                .End();
            }
            foreach(var capability in opts.Capabilities) {
                manifest.Start("capability").Attr("name", capability.Key);
                if(!string.IsNullOrEmpty(capability.Value)) {
                    manifest.Attr("value", capability.Value);
                }
                manifest.End();
            }
        }

        private static string GetRestrictionString(Restriction restriction) {
            switch(restriction) {
            case Restriction.Private:
                return "Private";
            case Restriction.SemiPublic:
                return "Semi-Public";
            default:
                return "Public";
            }
        }

        private static void Copy(Opts opts) {
            if(opts.Test) {
                return;
            }
            BulkCopy copier;
            var forceOverwrite = !(opts.PreserveLocalChanges ?? true);
            if(opts.ImportRelto.HasValue) {
                copier = opts.ExportRelto.HasValue
                    ? BulkCopy.CreateAsync(opts.DekiApi, opts.ExportDocument, opts.ExportRelto.Value, opts.ImportRelto.Value, forceOverwrite, new Result<BulkCopy>()).Wait()
                    : BulkCopy.CreateAsync(opts.DekiApi, opts.ExportDocument, opts.ExportReltoPath, opts.ImportRelto.Value, forceOverwrite, new Result<BulkCopy>()).Wait();
            } else {
                copier = opts.ExportRelto.HasValue
                    ? BulkCopy.CreateAsync(opts.DekiApi, opts.ExportDocument, opts.ExportRelto.Value, opts.ImportReltoPath, forceOverwrite, new Result<BulkCopy>()).Wait()
                    : BulkCopy.CreateAsync(opts.DekiApi, opts.ExportDocument, opts.ExportReltoPath, opts.ImportReltoPath, forceOverwrite, new Result<BulkCopy>()).Wait();
            }
            Result<BulkCopy> result = copier.CopyAsync(new Result<BulkCopy>());
            int completed = 0;
            Console.WriteLine("Exporting: {0}", opts.ExportPath);
            while(!result.HasFinished) {
                Thread.Sleep(200);
                if(copier.CompletedItems <= completed) {
                    continue;
                }
                if(SysUtil.IsUnix) {
                    Console.WriteLine("  {0} of {1} files ({2:0}%)", copier.CompletedItems, copier.TotalItems, 100.0 * copier.CompletedItems / copier.TotalItems);
                } else {
                    Console.Write("  {0} of {1} files ({2:0}%)      \r", copier.CompletedItems, copier.TotalItems, 100.0 * copier.CompletedItems / copier.TotalItems);
                }
                if(!SysUtil.IsUnix) {
                    Console.WriteLine();
                }
                completed = copier.CompletedItems;
            }
            if(result.HasException) {
                throw new Exception(string.Format("Copy failed: {0}", result.Exception.Message), result.Exception);
            }
        }

        private static int ExitWithUsage(int code) {
            Console.WriteLine();
            foreach(string line in Opts.Usage) {
                Console.WriteLine(line);
            }
            return code;
        }

        private static string ReadPassword() {
            StringBuilder password = new StringBuilder();
            for(ConsoleKeyInfo info = Console.ReadKey(true); info.Key != ConsoleKey.Enter; info = Console.ReadKey(true)) {
                if(info.Key == ConsoleKey.Backspace) {
                    if(password.Length> 0) {
                        password = password.Remove(password.Length - 1, 1);
                        if(!SysUtil.IsUnix) {
                            Console.Write("\b \b");
                        }
                    }
                } else {
                    password.Append(info.KeyChar);
                    if(!SysUtil.IsUnix) {
                        Console.Write("*");
                    }
                }
            }
            Console.WriteLine();
            return password.ToString();
        }
    }
}
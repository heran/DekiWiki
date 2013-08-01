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
using MindTouch.Dream;
using MindTouch.Xml;

namespace MindTouch.Deki.Tools.Import {
    public enum Mode {
        Import,
        Export,
        Copy
    }

    public enum Restriction {
        Default,
        Public,
        SemiPublic,
        Private
    }

    public class Opts {

        //--- Class Fields ---
        public static string[] Usage = new string[] {
            "USAGE: mindtouch.import.exe [options] [target]",
            "",
            "  [target] is either a path to a directory or a filepath ending in .mtarc or .mtapp",
            "     (not used for copy operations)",
            "",
            "Options:",
            "    (Options must be prefixed by a '-' or '/')",
            "",
            "    Modes: Import (default), Export or Copy",
            "      e|export                 - export mode",
            "      c|copy                   - copy mode",
            "",
            "    General:",
            "      ?|usage                  - display this message",
            "      C|config <path>          - confguration xml file (see below)",
            "      g|genconfig <path>       - instead of executing the command, generate the config file for later execution",
            "      h|host <host>            - MindTouch host (assumes standard API location)",
            "      u|uri <api-uri>          - specify the full uri to the API",
            "      a|archive                - force the target to be treated as an archive file",
            "      f|folder                 - force the target to be treated as a folder",
            "      I|importreltopath <path> - relative uri path for import and copy destination",
            "      importrelto <id>         - relative page id for import and copy destination (alternative to importreltopath)",
            "      X|exportreltopath <path> - relative uri path for export and copy source",
            "      exportrelto <id>         - relative page id for export and copy source (alternative to exportreltopath)",
            "      D|exportdoc  <filename>  - filename of the export xml document for export or copy",
            "      L|exportlist <filename>  - list of paths or uri's to pages to export (respects 'recursive' flag)",
            "      p|exportpath <path>      - specifies the uri path to export (in lieu of specifying an export document/list)",
            "      r|recursive              - export all child documents of 'exportpath' or 'exportlist'",
            "      o|output                 - output file/directory path for import or export",
            "      R|retries                - Maximum number of retries on import/export item failures (default: 3)",
            "      v|verbose                - Show stack trace with errors",
            "      T|testcred               - Test credentials",
            "      l|preservelocal          - mark package to preserve local changes to existing pages on import",
            "      O|overwritelocal         - mark package to preserve local changes to existing pages on import",
            "                                 (overwrite is the default unless the package has a different value set, in which case",
            "                                  this flag can be used to forces overwrite behavior on import)",
            "      s|securityrestriction (public|semipublic|private)",
            "                               - enforce a page restriction for all imported content",
            "      cap <capability-name>[=<capability-value>]",
            "                               - add a capability requirement for the exported package. The default for capability-value",
            "                                 'enabled'",
            "",
            "    Flags only used by packages imported by the Package Updater Service:",
            "      1|importonce             - mark package to only import once, even if the package updater is called with force option",
            "      i|initonly               - mark package init only, only to be imported on first valid license activation",
            "",
            "    Authentication:",
            "      (if no authentication is provided, the program will prompt for user and password interactively)",
            "      A|authtoken <token>      - authtoken to use for user authentication",
            "      U|user <username>        - username for authentication (requires password option",
            "      P|password <password>    - password for username authentcation",
            "",
            "Xml Configuration:",
            "    Commandline options can be provided by or augmented with an xml configuration document:",
            "      <config [option-longname]='value' ...>",
            "        [ <target>{output/input file/dir for import/export}</target> ]",
            "        [ <export><!--optionally inlined export document--></export> ]",
            "      </config>",
            "    Value-less options are booleans and have an xml attribute value of 'true' or 'false'.",
            "    The 'genconfig' option creates this configuration document from the provided settings for later use.",
            ""
        };

        //--- Fields ---
        public bool InitOnly;
        public bool ImportOnce;
        public bool WantUsage;
        public bool Verbose;
        public bool TestAuth;
        public Mode Mode = Mode.Import;
        public bool Archive;
        public Plug DekiApi;
        public string FilePath;
        public string ImportReltoPath = "/";
        public int? ImportRelto;
        public string ExportReltoPath = "/";
        public int? ExportRelto;
        public XDoc ExportDocument;
        public string User;
        public string Password;
        public string AuthToken;
        public bool Test;
        public bool GenConfig;
        public int Retries = 3;
        public bool? PreserveLocalChanges;
        public string ExportPath;
        public Restriction Restriction = Restriction.Default;
        public List<KeyValuePair<string, string>> Capabilities = new List<KeyValuePair<string, string>>();
        private bool _exportRecursive;
        private string _genConfigPath;

        //--- Methods ---
        public Opts() { }

        public Opts(string[] argArray) {
            List<string> args = new List<string>(argArray);
            int index = 0;
            bool? archive = null;
            Func<XDoc> exportDocumentBuilder = null;
            while(index < args.Count) {
                if(WantUsage) {
                    break;
                }
                string key = args[index];
                string value = (index + 1 >= args.Count) ? null : args[index + 1];
                bool handled = false;
                if(key.StartsWith("-") || key.StartsWith("/")) {
                    handled = true;
                    key = key.Remove(0, 1);
                    switch(key) {
                    case "1":
                    case "importonce":
                        ImportOnce = true;
                        break;
                    case "a":
                    case "archive":
                        archive = true;
                        break;
                    case "A":
                    case "authtoken":
                        AuthToken = value;
                        index++;
                        break;
                    case "C":
                    case "config":
                        index++;
                        List<string> extra = ConfigureFromXml(value);
                        extra.Reverse();
                        foreach(string opt in extra) {
                            args.Insert(index + 1, opt);
                        }
                        break;
                    case "c":
                    case "copy":
                        Mode = Mode.Copy;
                        break;
                    case "cap":
                        index++;
                        var split = value.Split(new[] { '=' }, 2);
                        var name = split[0];
                        var v = (split.Length == 2) ? split[1] : null;
                        Capabilities.Add(new KeyValuePair<string, string>(name, v));
                        break;
                    case "e":
                    case "export":
                        Mode = Mode.Export;
                        break;
                    case "p":
                    case "exportPath":
                        string exportPath = value;
                        ExportPath = exportPath;
                        exportDocumentBuilder = delegate() { return CreateExportDocumentFromSinglePath(exportPath); };
                        index++;
                        break;
                    case "D":
                    case "exportdoc":
                        string exportDocumentPath = value;
                        exportDocumentBuilder = delegate() { return LoadExportDocumentFromFile(exportDocumentPath); };
                        index++;
                        break;
                    case "f":
                    case "folder":
                        archive = false;
                        break;
                    case "L":
                    case "exportlist":
                        string exportListPath = value;
                        exportDocumentBuilder = delegate() { return CreateExportDocumentFromList(exportListPath); };
                        index++;
                        break;
                    case "l":
                    case "preservelocal":
                        PreserveLocalChanges = true;
                        break;
                    case "O":
                    case "overwritelocal":
                        PreserveLocalChanges = false;
                        break;
                    case "g":
                    case "genconfig":
                        GenConfig = true;
                        _genConfigPath = value;
                        index++;
                        break;
                    case "h":
                    case "host":
                        try {
                            if(!value.StartsWithInvariantIgnoreCase("http://") && !value.StartsWithInvariantIgnoreCase("https://")) {
                                DekiApi = Plug.New(string.Format("http://{0}/@api/deki", value));
                            } else {
                                DekiApi = Plug.New(new XUri(value).At("@api", "deki"));
                            }
                        } catch {
                            throw new ConfigurationException("Invalid host format {0}", value);
                        }
                        index++;
                        break;
                    case "i":
                    case "initonly":
                        InitOnly = true;
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
                    case "X":
                    case "exportreltopath":
                        ExportReltoPath = Title.FromUIUri(null, value).AsPrefixedDbPath();
                        index++;
                        break;
                    case "exportrelto":
                        ExportRelto = int.Parse(value);
                        index++;
                        break;
                    case "P":
                    case "password":
                        Password = value;
                        index++;
                        break;
                    case "r":
                    case "recursive":
                        _exportRecursive = true;
                        break;
                    case "R":
                    case "retries":
                        Retries = int.Parse(value);
                        index++;
                        break;
                    case "s":
                    case "securityrestriction":
                        try {
                            Restriction = SysUtil.ChangeType<Restriction>(value);
                        } catch {
                            throw new ConfigurationException("invalid securityrestriction: {0}", value);
                        }
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
                    case "v":
                    case "verbose":
                        Verbose = true;
                        break;
                    case "T":
                    case "testcred":
                        TestAuth = true;
                        break;
                    case "t":
                        Test = true;
                        break;
                    default:
                        handled = false;
                        break;
                    }
                }
                if(!handled) {
                    if(index + 1 == args.Count && Mode != Mode.Copy) {
                        FilePath = key;
                    } else {
                        throw new ConfigurationException("Unknown option {0}", key);
                    }
                }
                index++;
            }
            if(WantUsage) {
                return;
            }
            if(!string.IsNullOrEmpty(FilePath)) {
                string ext = Path.GetExtension(FilePath);
                if(!archive.HasValue) {

                    // Note (arnec): .zip and .mtap are still being handled for backwards compatibility, but have been removed from docs
                    if(ext.EqualsInvariantIgnoreCase(".mtapp") || ext.EqualsInvariantIgnoreCase(".mtarc") ||
                       ext.EqualsInvariantIgnoreCase(".zip") || ext.EqualsInvariantIgnoreCase(".mtap")) {
                        archive = true;
                    }
                }
            }
            Archive = archive ?? false;
            if(Mode != Mode.Import && ExportDocument == null) {
                if(exportDocumentBuilder == null) {
                    exportDocumentBuilder = delegate() { return CreateExportDocumentFromSinglePath(ExportReltoPath); };
                }
                ExportDocument = exportDocumentBuilder();
            }
            if(TestAuth || Mode == Mode.Copy || !string.IsNullOrEmpty(FilePath)) {
                return;
            }
            if(Archive) {
                throw new ConfigurationException("Missing {0} Archive filepath", Mode);
            }
            throw new ConfigurationException("Missing {0} Directory", Mode);
        }

        private XDoc CreateExportDocumentFromSinglePath(string exportPath) {
            return new XDoc("export")
                .Start("page")
                .Attr("path", Title.FromUIUri(null, exportPath).AsPrefixedDbPath())
                .Attr("recursive", _exportRecursive)
                .End();
        }

        private XDoc CreateExportDocumentFromList(string listPath) {
            if(!File.Exists(listPath)) {
                throw new ConfigurationException("No such export list: {0}", listPath);
            }
            XDoc exportDoc = new XDoc("export");
            foreach(string line in File.ReadAllLines(listPath)) {
                if(string.IsNullOrEmpty(line)) {
                    continue;
                }
                if(line.StartsWith("#")) {
                    exportDoc.Comment(line.Remove(0, 1));
                    continue;
                }
                try {
                    bool exportRecursive = _exportRecursive;
                    string path = line.Trim();
                    if(path.EndsWith(" +")) {
                        exportRecursive = true;
                        path = path.Substring(0, path.Length - 2).TrimEnd();
                    } else if(path.EndsWith(" -")) {
                        exportRecursive = false;
                        path = path.Substring(0, path.Length - 2).TrimEnd();
                    }
                    if(!line.StartsWith("/")) {
                        XUri uri = new XUri(path);
                        path = uri.Path;
                        if("/index.php".EqualsInvariantIgnoreCase(path)) {
                            path = uri.GetParam("title");
                        }
                    }
                    exportDoc.Start("page")
                        .Attr("path", Title.FromUIUri(null, path).AsPrefixedDbPath())
                        .Attr("recursive", exportRecursive)
                    .End();
                } catch(Exception) {
                    throw new ConfigurationException("Unable to parse uri: {0}", line.Trim());
                }
            }
            return exportDoc;
        }

        private XDoc LoadExportDocumentFromFile(string exportDocumentPath) {
            if(!File.Exists(exportDocumentPath)) {
                throw new ConfigurationException("No such export document: {0}", exportDocumentPath);
            }
            try {
                return XDocFactory.LoadFrom(exportDocumentPath, MimeType.TEXT_XML);
            } catch(Exception e) {
                throw new ConfigurationException("Unable to load '{0}': {1}", exportDocumentPath, e.Message);
            }
        }

        private List<string> ConfigureFromXml(string configFile) {
            if(!File.Exists(configFile)) {
                throw new ConfigurationException("No such config file: {0}", configFile);
            }
            XDoc config = null;
            try {
                config = XDocFactory.LoadFrom(configFile, MimeType.TEXT_XML);
            } catch(Exception e) {
                throw new ConfigurationException("Unable to load '{0}': {1}", configFile, e.Message);
            }
            List<string> extraOpts = new List<string>();
            foreach(string flagPath in new string[] { "export", "copy", "archive", "recursive" }) {
                XDoc flag = config["@" + flagPath];
                if(!flag.IsEmpty && (flag.AsBool ?? false)) {
                    extraOpts.Add("-" + flagPath);
                }
            }
            foreach(string argPath in new string[] { "host", "uri", "importreltopath", "importrelto", "exportreltopath", "exportrelto", "exportdoc", "exportpath", "exportlist", "authtoken", "user", "password" }) {
                string opt = config["@" + argPath].AsText;
                if(string.IsNullOrEmpty(opt)) {
                    continue;
                }
                extraOpts.Add("-" + argPath);
                extraOpts.Add(opt);
            }
            XDoc export = config["export"];
            if(!export.IsEmpty) {
                ExportDocument = export;
            }
            XDoc target = config["target"];
            if(!target.IsEmpty) {
                FilePath = target.AsText;
            }
            return extraOpts;
        }

        public void WriteConfig() {
            XDoc exportDocument = new XDoc("config")
                .Attr("archive", Archive)
                .Add(ExportDocument);
            switch(Mode) {
            case Mode.Export:
                exportDocument.Attr("export", true);
                break;
            case Mode.Copy:
                exportDocument.Attr("copy", true);
                break;
            }
            if(DekiApi != null) {
                exportDocument.Attr("uri", DekiApi.Uri);
            }
            if(!string.IsNullOrEmpty(User)) {
                exportDocument.Attr("user", User);
            }
            if(!string.IsNullOrEmpty(Password)) {
                exportDocument.Attr("password", Password);
            }
            if(!string.IsNullOrEmpty(AuthToken)) {
                exportDocument.Attr("authtoken", AuthToken);
            }
            if(!string.IsNullOrEmpty(FilePath)) {
                exportDocument.Elem("target", FilePath);
            }
            if(Mode != Import.Mode.Export && !string.IsNullOrEmpty(ImportReltoPath)) {
                exportDocument.Attr("importreltopath", ImportReltoPath);
            }
            if(Mode != Import.Mode.Import && !string.IsNullOrEmpty(ExportReltoPath)) {
                exportDocument.Attr("exportreltopath", ExportReltoPath);
            }
            if(ImportRelto.HasValue) {
                exportDocument.Attr("importrelto", ImportRelto.Value);
            }
            if(ExportRelto.HasValue) {
                exportDocument.Attr("exportrelto", ExportRelto.Value);
            }
            string dir = Path.GetDirectoryName(Path.GetFullPath(_genConfigPath));
            if(!Directory.Exists(dir)) {
                Directory.CreateDirectory(dir);
            }
            exportDocument.Save(_genConfigPath);
        }
    }
}

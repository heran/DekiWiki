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
using System.IO;
using System.Text;
using MindTouch.Deki.Tests.RemoteInstanceTests;
using MindTouch.Dream;
using MindTouch.Dream.Test;
using MindTouch.Tasking;
using MindTouch.Xml;

using NUnit.Framework;

namespace MindTouch.Deki.Tests {
    public static class Utils {

        public static string ToErrorString(this DreamMessage response) {
            if(response == null || response.IsSuccessful) {
                return null;
            }
            var responseText = response.HasDocument ? response.ToDocument().ToPrettyString() : response.ToString();
            return string.Format("Status: {0}\r\nMessage:\r\n{1}", response.Status, responseText);
        }

        public static string PathCombine(this string root, params string[] segments) {
            foreach(var segment in segments) {
                root = Path.Combine(root, segment);
            }
            return root;
        }

        public class TestException : Exception { }

        public class TestSettings {

            //--- Class Fields ---
            private static readonly log4net.ILog _log = LogUtils.CreateLog();
            public static readonly TestSettings Instance = new TestSettings();

            private readonly object padlock = new object();
            private string _basePath;
            private XDoc _dekiConfig;
            private DreamHostInfo _hostInfo;
            private readonly XDoc _xdoc = null;
            public readonly XUri LuceneMockUri = new XUri("mock://mock/testlucene");
            public readonly XUri PackageUpdaterMockUri = new XUri("mock://mock/packageupdater");
            public readonly XUri PageSubscriptionMockUri = new XUri("mock://mock/pagesubscription");
            private string _productKey;
            private string _storageDir;
            private string _licensePath;
            private bool _remote;
            private bool _withoutInstanceApiKey;

            private TestSettings() {
                var branchAttr = typeof(DekiWikiService).Assembly.GetAttribute<SvnBranchAttribute>();
                var branch = "trunk";
                if(branchAttr != null) {
                    branch = branchAttr.Branch;
                }
                _basePath = @"C:\mindtouch\public\dekiwiki\" + branch + @"\";
                MockPlug.DeregisterAll();
                string configfile = "mindtouch.deki.tests.xml";
                if(File.Exists(configfile)) {
                    _xdoc = XDocFactory.LoadFrom(configfile, MimeType.XML);
                } else {
                    _xdoc = new XDoc("config");
                }
            }

            public void SetupWithoutInstanceApiKey() {
                ShutdownHost();
                _storageDir = null;
                _dekiConfig = null;
                _withoutInstanceApiKey = true;
            }

            public void SetupAsLocalInstance() {
                ShutdownHost();
                _storageDir = null;
                _dekiConfig = null;
                _remote = false;
            }

            public void SetupAsRemoteInstance() {
                ShutdownHost();
                _storageDir = null;
                _dekiConfig = null;
                _remote = true;
                RemoteInstanceService.Reset();
            }

            public Plug Server {
                get {
                    return HostInfo.LocalHost.At("deki");
                }
            }

            public DreamHostInfo HostInfo {
                get {
                    if(_hostInfo == null) {
                        lock(padlock) {
                            if(_hostInfo == null) {
                                InitEnv();
                                _hostInfo = DreamTestHelper.CreateRandomPortHost(new XDoc("config").Elem("apikey", Settings.ApiKey).Elem("storage-dir", Settings.StorageDir));
                                _hostInfo.Host.Self.At("load").With("name", "mindtouch.deki").Post(DreamMessage.Ok());
                                _hostInfo.Host.Self.At("load").With("name", "mindtouch.deki.services").Post(DreamMessage.Ok());
                                _hostInfo.Host.Self.At("load").With("name", "mindtouch.indexservice").Post(DreamMessage.Ok());
                                _hostInfo.Host.Self.At("load").With("name", "mindtouch.deki.tests").Post(DreamMessage.Ok());
                                DreamTestHelper.CreateService(_hostInfo, DekiConfig);
                            }
                        }
                    }
                    return _hostInfo;
                }
            }

            private void InitEnv() {
                if(!string.IsNullOrEmpty(_storageDir)) {
                    return;
                }
                _storageDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                Directory.CreateDirectory(_storageDir);
                _licensePath = _storageDir.PathCombine("_x002F_deki", "default", "license.xml");
                Directory.CreateDirectory(Path.GetDirectoryName(_licensePath));
                _productKey = StringUtil.ComputeHashString(ApiKey, Encoding.UTF8);
                LicenseUtil.TestLicense.Save(_licensePath);
                _log.InfoFormat("--- initialized test environment with storage at {0}", _storageDir);
            }

            public void ShutdownHost() {
                _withoutInstanceApiKey = false;
                lock(padlock) {
                    if(_hostInfo != null) {
                        _hostInfo.Dispose();
                        _hostInfo = null;
                    }
                }
            }

            public string ApiKey { get { return GetString("/config/apikey", "123"); } }
            public string InstanceApiKey { get { return _withoutInstanceApiKey ? null : GetString("/config/instanceapikey", "789"); } }
            public string AssetsPath { get { return GetString("/config/assets-path", @"C:\mindtouch\assets"); } }
            public string DekiPath { get { return GetString("/config/deki-path", _basePath + @"web"); } }
            public string DekiResourcesPath { get { return GetString("/config/deki-resources-path", _basePath+ @"web\resources"); } }
            public string ImageMagickConvertPath { get { return GetString("/config/imagemagick-convert-path", _basePath + @"src\tools\mindtouch.dekihost.setup\convert.exe"); } }
            public string ImageMagickIdentifyPath { get { return GetString("/config/imagemagick-identify-path", _basePath + @"src\tools\mindtouch.dekihost.setup\identify.exe"); } }
            public string PrinceXmlPath { get { return GetString("/config/princexml-path", @"C:\Program Files\Prince\Engine\bin\prince.exe"); } }
            public string HostAddress { get { return GetString("/config/host-address", "testdb.mindtouch.com"); } }
            public string DbServer { get { return GetString("/config/db-server", "testdb.mindtouch.com"); } }
            public string DbCatalog { get { return GetString("/config/db-catalog", "wikidb"); } }
            public string DbUser { get { return GetString("/config/db-user", "wikiuser"); } }
            public string DbPassword { get { return GetString("/config/db-password", "password"); } }
            public string UserName { get { return GetString("/config/UserName", "Admin"); } }
            public string Password { get { return GetString("/config/Password", "password"); } }
            public int CountOfRepeats { get { return GetInt("/config/CountOfRepeats", 5); } }
            public int SizeOfBigContent { get { return GetInt("/config/SizeOfBigContent", 4096); } }
            public int SizeOfSmallContent { get { return GetInt("/config/SizeOfSmallContent", 256); } }
            public string SnKeyPath { get { return AssetsPath.PathCombine("keys", "mindtouch.snk"); } }
            public string WikiId { get { return "default"; } }

            public string ProductKey {
                get {
                    InitEnv();
                    return _productKey;
                }
            }

            public string StorageDir {
                get {
                    InitEnv();
                    return _storageDir;
                }
            }

            public string LicensePath {
                get {
                    InitEnv();
                    return _licensePath;
                }
            }

            public XDoc DekiConfig {
                get {
                    if(_dekiConfig == null) {
                        lock(padlock) {
                            _dekiConfig = new XDoc("config")
                                .Elem("apikey", ApiKey)
                                .Elem("path", "deki")
                                .Elem("sid", "http://services.mindtouch.com/deki/draft/2006/11/dekiwiki")
                                .Elem("deki-path", DekiPath)
                                .Elem("deki-resources-path", DekiResourcesPath)
                                .Elem("imagemagick-convert-path", ImageMagickConvertPath)
                                .Elem("imagemagick-identify-path", ImageMagickIdentifyPath)
                                .Elem("princexml-path", PrinceXmlPath)
                                .Start("page-subscription").Elem("accumulation-time", "0").End()
                                .Start("packageupdater").Attr("uri", PackageUpdaterMockUri).End()
                                .Start("indexer").Attr("src", LuceneMockUri).End();
                            if(_remote) {
                                _dekiConfig.Start("wikis").Attr("src", RemoteInstanceService.Instance.Uri).End();
                            } else {
                                _dekiConfig.Start("wikis")
                                    .Start("config")
                                        .Attr("id", WikiId)
                                        .Start("security").Elem("api-key", InstanceApiKey).End()
                                        .Elem("host", "*")
                                        .Start("page-subscription")
                                            .Elem("from-address", "foo@bar.com")
                                        .End()
                                        .Elem("db-server", DbServer)
                                        .Elem("db-port", "3306")
                                        .Elem("db-catalog", DbCatalog)
                                        .Elem("db-user", DbUser)
                                        .Start("db-password").Attr("hidden", "true").Value(DbPassword).End()
                                        .Elem("db-options", "pooling=true; Connection Timeout=5; Protocol=socket; Min Pool Size=2; Max Pool Size=50; Connection Reset=false;character set=utf8;ProcedureCacheSize=25;Use Procedure Bodies=true;")
                                    .End()
                                .End();
                            }
                        }
                    }
                    return _dekiConfig;
                }
            }

            private int GetInt(string path, int defaultValue) {
                return _xdoc[path].AsInt != null ? _xdoc[path].AsInt.Value : defaultValue;
            }

            private string GetString(string path, string defaultValue) {
                return _xdoc[path].AsText ?? defaultValue;
            }
        }

        public static readonly TestSettings Settings = TestSettings.Instance;

        public static Plug BuildPlugForAnonymous() {
            return BuildPlugForUser(null, null);
        }

        public static Plug BuildPlugForAdmin() {
            return BuildPlugForUser(Settings.UserName, Settings.Password);
        }

        public static Plug BuildPlugForUser(string username) {
            return BuildPlugForUser(username, UserUtils.DefaultUserPsw);
        }

        public static Plug BuildPlugForUser(string username, string password) {
            Plug.GlobalCookies.Clear();
            Plug p = Settings.Server;

            if(!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password)) {
                DreamMessage msg = p.WithCredentials(username, password).At("users", "authenticate").PostAsync().Wait();
                Assert.AreEqual(DreamStatus.Ok, msg.Status, "Failed to authenticate.");
            }
            return p;
        }

        public static string GenerateUniqueName() {
            return Guid.NewGuid().ToString().Replace("-", string.Empty);
        }

        public static string GenerateUniqueName(string prefix) {
            return prefix + GenerateUniqueName();
        }

        public static bool ByteArraysAreEqual(byte[] one, byte[] two) {
            if(one == null || two == null)
                throw new ArgumentException();

            if(one.Length != two.Length)
                return false;
            for(int i = 0; i < one.Length; i++)
                if(one[i] != two[i])
                    return false;

            return true;
        }

        private static char[] _alphabet = new char[] { 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'l', 'm', 'n', 'o', 'p', 'r', 's', 't', (char)224, (char)225, (char)226, (char)227, (char)228, (char)229, (char)230, (char)231, (char)232, (char)233, (char)234, (char)235, (char)236, (char)237, (char)238, (char)239, (char)240, (char)241, (char)242 };

        public static string GetRandomTextByAlphabet(int countOfSymbols) {
            System.Text.StringBuilder builder = new StringBuilder(countOfSymbols);
            Random rnd = new Random();
            for(int i = 0; i < countOfSymbols; i++)
                builder.Append(_alphabet[rnd.Next(_alphabet.Length - 1)]);

            return builder.ToString();
        }

        public static string GetRandomText(int countOfSymbols) {
            System.Text.StringBuilder builder = new StringBuilder(countOfSymbols);
            Random rnd = new Random();
            for(int i = 0; i < countOfSymbols; i++) {
                try {
                    int symbolAsInt = rnd.Next(0x10ffff);
                    while(0xD800 <= symbolAsInt && symbolAsInt <= 0xDFFF)
                        symbolAsInt = rnd.Next(0x10ffff);
                    char symbol = char.ConvertFromUtf32(symbolAsInt)[0];
                    if(char.IsDigit(symbol) || char.IsLetter(symbol) || char.IsPunctuation(symbol))
                        builder.Append(symbol);
                    else
                        i--;
                } catch(System.ArgumentOutOfRangeException) {
                    i--;
                }
            }

            return builder.ToString();
        }

        public static string GetBigRandomText() {
            return GetRandomTextByAlphabet(Utils.Settings.SizeOfBigContent);
        }

        public static string GetSmallRandomText() {
            return GetRandomTextByAlphabet(Utils.Settings.SizeOfSmallContent);
        }

        public static string DateToString(DateTime time) {
            return time == DateTime.MinValue ? null : time.ToUniversalTime().ToString("yyyyMMddHHmmss", System.Globalization.CultureInfo.InvariantCulture.DateTimeFormat);
        }

        public static Dictionary<string, string> GetDictionaryFromDoc(XDoc doc, string element, string key, string value) {
            Dictionary<string, string> result = new Dictionary<string, string>();
            foreach(XDoc node in doc[element])
                result[node[key].AsText] = node[value].AsText;

            return result;
        }

        public static void TestSortingOfDocByField(XDoc doc, string resourceName, string element, bool ascorder) {
            TestSortingOfDocByField(doc, resourceName, element, ascorder, null);
        }

        public static void TestSortingOfDocByField(XDoc doc, string resourceName, string element, bool ascorder, Dictionary<string, string> dictData) {
            string previousValue = string.Empty;
            foreach(XDoc node in doc[resourceName]) {
                string currentValue = dictData == null ? node[element].AsText : dictData[node[element].AsText];
                if(!string.IsNullOrEmpty(previousValue) && !string.IsNullOrEmpty(currentValue)) {
                    int x = StringUtil.CompareInvariantIgnoreCase(previousValue, currentValue) * (ascorder ? 1 : -1);
                    Assert.IsTrue(x <= 0, string.Format("Sort assertion failed for '{0}': '{1}' and '{2}'", element, previousValue, currentValue));
                }
                previousValue = currentValue;
                currentValue = string.Empty;
            }
        }

        public static void PingServer() {
            var host = Settings.HostInfo;
        }
    }

    // Note (arnec): this has to exist for nunit-console to pick up the log4net configuration
    [SetUpFixture]
    public class LogSetup {
        [SetUp]
        public void Setup() {
            log4net.Config.XmlConfigurator.Configure();
        }
    }

}

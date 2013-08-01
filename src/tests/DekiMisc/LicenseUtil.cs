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
using System.IO;
using MindTouch.Dream;
using MindTouch.Tasking;
using MindTouch.Xml;
using NUnit.Framework;

namespace MindTouch.Deki.Tests {
    public static class LicenseUtil {
        private static readonly string _expiration = String.Format("{0:yyyyMMdd}", DateTime.Now.AddDays(2));
        private static readonly string[] _testLicenseArgs = new[] {
            "type=commercial",
            "sign=" + Utils.Settings.SnKeyPath,
            "id=123",
            "productkey=" + Utils.Settings.ProductKey,
            "licensee=Acme",
            "address=123",
            "hosts=" + Utils.Settings.HostAddress,
            "name=foo",
            "phone=123-456-7890",
            "email=foo@mindtouch.com",
            "users=infinite",
            "sites=infinite",
            "sid=sid://mindtouch.com/ent",
            "sid=sid://mindtouch.com/std/",
            "sidexpiration=" + _expiration,
            "sid=sid://mindtouch.com/ext/2009/12/anychart",
            "sidexpiration=" + _expiration,
            "sid=sid://mindtouch.com/ext/2009/12/anygantt",
            "sidexpiration=" + _expiration,
            "sid=sid://mindtouch.com/ext/2010/06/analytics.content",
            "sidexpiration=" + _expiration,
            "sid=sid://mindtouch.com/ext/2010/06/analytics.search",
            "capability:anonymous-permissions=ALL",
            "capabilityexpiration=" + _expiration,
            "capability:search-engine=adaptive",
            "capabilityexpiration=" + _expiration,
            "capability:content-rating=enabled",
            "capabilityexpiration=" + _expiration,
        };
        private static string _genLicensePath;
        private static XDoc _testLicense;

        private static void Init() {
            if(_genLicensePath != null) {
                return;
            }
            _genLicensePath = Path.Combine(Utils.Settings.StorageDir, "license-" + StringUtil.CreateAlphaNumericKey(8) + ".xml");
        }

        public static XDoc TestLicense {
            get {
                Init();
                if(_testLicense == null) {
                    _testLicense = GenerateLicense(_testLicenseArgs);
                }
                return _testLicense;
            }
        }

        public static XDoc InactiveLicense {
            get {
                return Plug.New("resource://mindtouch.deki/MindTouch.Deki.Resources.license-inactive.xml").With(DreamOutParam.TYPE, MimeType.XML.ToString()).GetAsync().Wait().ToDocument();
            }
        }

        public static XDoc GenerateLicense(string[] licenseArgs) {
            Init();
            Tuplet<int, Stream, Stream> exitValues = CallLicenseGenerator(licenseArgs);
            Assert.AreEqual(0, exitValues.Item1, "Unexpected return code\n" + GetErrorMsg(exitValues.Item2) + GetErrorMsg(exitValues.Item3));

            // Retrieve generated license
            return !File.Exists(_genLicensePath) ? null : XDocFactory.LoadFrom(_genLicensePath, MimeType.XML);
        }

        public static Tuplet<int, Stream, Stream> CallLicenseGenerator(string[] licenseArgs) {
            Init();
            var licenseGenerator = Utils.Settings.AssetsPath + "/deki.license/mindtouch.license.exe";
            if(!File.Exists(licenseGenerator)) {
                Assert.Fail("Invalid path to license generator.");
            }
            const int timeOut = 60000;
            string cmdlineargs = String.Empty;

            // arrange command line arguments
            for(int i = 0; i < licenseArgs.Length; i++) {
                cmdlineargs += "\"" + licenseArgs[i] + "\" ";
            }
            cmdlineargs += "\"out=" + _genLicensePath + "\"";

            // Run the license generator
            return Async.ExecuteProcess(licenseGenerator,
                                        cmdlineargs,
                                        null,
                                        new Result<Tuplet<int, Stream, Stream>>(TimeSpan.FromMilliseconds(timeOut)))
                .Wait();
        }

        // Error stream -> error string
        private static string GetErrorMsg(Stream error) {
            var sr = new StreamReader(error);
            return sr.ReadToEnd();
        }

    }
}
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
using System.Collections.Generic;
using System.Net;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using log4net;
using MindTouch.Dream;
using MindTouch.Security.Cryptography;
using MindTouch.Xml;

namespace MindTouch.Deki.Util {

    public class DekiLicenseException : Exception {

        //--- Types ---
        public enum ReasonKind {
            VALID_LICENSE = 0,
            INVALID_SIGNATURE,
            INVALID_LICENSE_TYPE,
            EXPIRED_LICENSE,
            HOST_MISMATCH,
            INVALID_URI,
            INVALID_LICENSE,
            NO_LICENSE_FOUND,
            INVALID_RESPONSE
        }

        //--- Fields ---
        public readonly ReasonKind Reason;

        //--- Constructors ---
        public DekiLicenseException(ReasonKind reason, string message) : base(message) {
            this.Reason = reason;
        }
    }

    public static class DekiLicense {

        //--- Constants ---
        private const string MINDTOUCH_PUBLIC_KEY = "0024000004800000940000000602000000240000525341310004000001000100c59c584b5a2a3c335322d6cc44f0855889b5f16de611ab96788b1ae38f061514542ef69091168b01161968191345f509072c7f11c48710869ae14770c99e83dbe14b981aab3ba7306203f86bca0cebe91fe174c525095b31b0387211653b1b569d01d7c9ed889d460b915a91442705655498be9da4cd15e4af1811851e3dbbd7";

        //--- Class Fields ---
        private static RSACryptoServiceProvider _rsa;
        private static readonly Regex _ipRegex = new Regex(@"^\d+\.\d+\.\d+\.\d+\$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        private static readonly ILog _log = LogUtils.CreateLog();

        //--- Class Properties ---
        public static RSACryptoServiceProvider MindTouchPublicKey {
            get {
                if(_rsa == null) {
                    _rsa = RSAUtil.ProviderFrom(StringUtil.BytesFromHexString(MINDTOUCH_PUBLIC_KEY));
                }
                return _rsa;
            }
        }

        //--- Extension Methods ---
        public static bool IsSameLicense(this XDoc lic1, XDoc lic2) {
            if(lic1 == null || lic1.IsEmpty) {
                return false;
            }
            var sig1 = lic1.GetLicenseSignature();
            return !string.IsNullOrEmpty(sig1) && sig1.EqualsInvariant(lic2.GetLicenseSignature());
        }

        public static string GetLicenseSignature(this XDoc doc) {
            if(doc == null || doc.IsEmpty) {
                return null;
            }
            doc.UsePrefix("dsig", XDoc.NS_DSIG);
            return doc["@dsig:dsig"].Contents;
        }

        //--- Class Methods ---
        public static void Validate(string host) {
            XUri uri;

            // check if host is valid; if not, check if hostname is valid with "http://" prefix
            if(!XUri.TryParse(host, out uri) && !XUri.TryParse("http://" + host, out uri)) {
                throw new DekiLicenseException(DekiLicenseException.ReasonKind.INVALID_URI, string.Format("Server URI '{0}' is not valid.", host));
            }
            Validate(uri);
        }

        public static void Validate(XUri host) {
            XDoc license;
            try {
                DreamMessage response = Plug.New(host).At("@api", "deki", "license").Get();

                // make sure the response is application/xml
                if(!response.ContentType.Match(MimeType.XML)) {
                    throw new DekiLicenseException(DekiLicenseException.ReasonKind.INVALID_RESPONSE, string.Format("Server returned an invalid license document. Expected application/xml, but received {0}.", response.ContentType.FullType));
                }
                license = response.ToDocument();
            } catch(DreamResponseException e) {
                string message = null;

                // check status code of response
                switch(e.Response.Status) {
                case DreamStatus.MethodNotAllowed:
                case DreamStatus.NotFound:
                    throw new DekiLicenseException(DekiLicenseException.ReasonKind.NO_LICENSE_FOUND, string.Format("Server did not return license information for '{0}'.", host));
                case DreamStatus.ServiceUnavailable:
                    throw new DekiLicenseException(DekiLicenseException.ReasonKind.NO_LICENSE_FOUND, string.Format("Server at '{0}' appears to be down.", host));
                }
                
                // check if response contains an XML document
                if(e.Response.ContentType.IsXml) {
                    XDoc doc = e.Response.ToDocument();
                    if(doc.HasName("exception") || doc.HasName("error")) {
                        message = doc["message"].AsText;
                    }
                }

                // use respons document by default
                if(message == null) {
                    message = (e.Response != null) ? e.Response.ToText() : "No response returned";
                }
                throw new DekiLicenseException(DekiLicenseException.ReasonKind.INVALID_RESPONSE, string.Format("An error occurred while attempting to connect to '{0}'. {2} (status {1}).", host, (int)e.Response.Status, message));
            }
            Validate(host, license);
        }

        public static void Validate(XDoc license) {
            try {

                // check if license has a valid element name
                if(!license.HasName("license.public") && !license.HasName("license.private")) {
                    throw new DekiLicenseException(DekiLicenseException.ReasonKind.INVALID_LICENSE, string.Format("Server returned an invalid license document. Expected <license.public>, but received <{0}>.", license.Name));
                }

                // check if license is valid using the public key
                if(!license.HasValidSignature(MindTouchPublicKey)) {
                    throw new DekiLicenseException(DekiLicenseException.ReasonKind.INVALID_SIGNATURE, "Server license validation failed. The license signature is not valid.");
                }

                // check license type
                string type = license["@type"].Contents;
                if(!type.EqualsInvariantIgnoreCase("commercial") && !type.EqualsInvariantIgnoreCase("trial") && !type.EqualsInvariantIgnoreCase("community")) {
                    throw new DekiLicenseException(DekiLicenseException.ReasonKind.INVALID_LICENSE_TYPE, string.Format("Server license type must be either 'community', 'commercial' or 'trial' instead of '{0}'.", type ?? "none"));
                }

                // check license issue date
                DateTime issued = license["date.issued"].AsDate ?? DateTime.MinValue;
                if(issued > DateTime.UtcNow) {
                    throw new DekiLicenseException(DekiLicenseException.ReasonKind.INVALID_LICENSE, string.Format("Server license has future issue date of {0}.", issued.ToString("dddd, d MMMM yyyy")));
                }

                // check license expiration date
                DateTime expires = license["date.expiration"].AsDate ?? DateTime.MaxValue;
                if(expires < DateTime.UtcNow) {
                    throw new DekiLicenseException(DekiLicenseException.ReasonKind.EXPIRED_LICENSE, string.Format("Server license expired on {0}.", expires.ToString("dddd, d MMMM yyyy")));
                }
            } catch(Exception e) {
                _log.WarnFormat(e.Message);
                throw;
            }
        }

        public static void Validate(XUri host, XDoc license) {
            Validate(license);

            // check if license requires a matchin host name
            XDoc hostnames = license["host"];
            if(!hostnames.IsEmpty) {
                IPAddress[] ips = null;

                // convert matches to a list of strings
                List<string> hosts = new List<string>();
                foreach(XDoc hostname in hostnames) {
                    hosts.Add(hostname.Contents);
                }

                // check if any of the hostnames match
                bool valid = false;
                foreach(string hostname in hosts) {

                    // check for a partial match if licensed hostname starts with '.'
                    if(hostname.StartsWithInvariantIgnoreCase(".") && (host.Host.EndsWithInvariantIgnoreCase(hostname) || host.Host.EqualsInvariantIgnoreCase(hostname.Substring(1)))) {
                        valid = true;
                        break;
                    }

                    // check for a partial match if licensed hostname starts with '*.'
                    if(hostname.StartsWithInvariantIgnoreCase("*.") && (host.Host.EndsWithInvariantIgnoreCase(hostname.Substring(1)) || host.Host.EqualsInvariantIgnoreCase(hostname.Substring(2)))) {
                        valid = true;
                        break;
                    }

                    // check for an exact match
                    if(host.Host.EqualsInvariantIgnoreCase(hostname)) {
                        valid = true;
                        break;
                    }

                    // check if hostname is an IP address
                    if(_ipRegex.IsMatch(hostname)) {

                        // resolve server name to an IP address
                        if(ips == null) {
                            try {
                                ips = Dns.GetHostAddresses(host.Host);
                            } catch { }
                        }

                        // check if resolution was successful
                        if(ips != null) {
                            foreach(IPAddress ip in ips) {

                                // TODO (steveb): add support for IPv6

                                // check for IPv4
                                if(ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork) {

                                    // check if IP address matches
                                    byte[] parts = ip.GetAddressBytes();
                                    if(hostname.EqualsInvariantIgnoreCase(string.Format("{0}.{1}.{2}.{3}", parts[0], parts[1], parts[2], parts[3]))) {
                                        valid = true;
                                        break;
                                    }
                                }
                            }

                            // check if the inner IP loop found a match
                            if(valid) {
                                break;
                            }
                        }
                    }
                }

                // check if a valid hostname was found
                if(!valid) {
                    throw new DekiLicenseException(DekiLicenseException.ReasonKind.HOST_MISMATCH, string.Format("Server host name '{0}' does not match any of the licensed host name(s): {1}", host.Host, string.Join(", ", hosts.ToArray())));
                }
            }
        }

        public static string GetCapability(XDoc license, string capability) {
            XDoc entry = license["grants"][capability];
            if(!entry.IsEmpty) {
                DateTime? expiration = entry["@date.expire"].AsDate;
                if((expiration == null) || (expiration.Value >= DateTime.UtcNow)) {
                    return entry.AsText;
                }
            }
            return null;
        }
    }
}

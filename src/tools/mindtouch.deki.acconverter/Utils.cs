using System;

using MindTouch.Dream;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Collections.Generic;

namespace MindTouch.Tools.ConfluenceConverter {
    public static class Utils {
        //global constants
        public static int MAXRESULTS = 5;
        public static string DefaultParamName = "default";

        //--- Class Methods ---
        public static string FormatPageDate(DateTime? date) {
            if(!date.HasValue) {
                return string.Empty;
            }
            return date.Value.ToUniversalTime().ToString("yyyyMMddHHmmss", System.Globalization.CultureInfo.InvariantCulture.DateTimeFormat);
        }

        public static string DoubleUrlEncode(string url) {
            return XUri.DoubleEncodeSegment(url);
        }

        public static string DoubleUrlDecode(string url)
        {
            return XUri.DoubleDecode(url);
        }

        public static string GetTopLevelPage(string pagePath)
        {
            string strTopPage;
            if (String.IsNullOrEmpty(pagePath))
                return null;

            // "/level1/level2/"
            string decodedPath = Utils.DoubleUrlDecode(pagePath);
            decodedPath.Trim('\\');
            decodedPath.Trim('\"');
            decodedPath.Trim('/');
            strTopPage = decodedPath.Substring(0, decodedPath.IndexOf('/'));
            return strTopPage;
        }

        public static string GetDekiUserPageByUserName(string userName) {
            return "User:" + userName;
        }

        public static string GetUrlLocalUri(XUri confBaseUri, string url, bool includeQuery, bool decode) {
            if(string.IsNullOrEmpty(url)) {
                return null;
            }

            if(url.StartsWithInvariantIgnoreCase(confBaseUri.ToString())) {
                
                //Remove the wiki path prefix (everything before display generally)
                url = confBaseUri.SchemeHostPort + url.Substring(confBaseUri.ToString().Length);
            }

            XUri uri = XUri.TryParse(url);
            
            if(uri == null) {
                return null;
            }
            string ret = uri.Path;
            if(decode) {
                ret = XUri.Decode(ret);
            }

            if(includeQuery && !string.IsNullOrEmpty(uri.QueryFragment)) {
                ret += uri.QueryFragment;
            }
            return ret;
        }

        public static string ConvertPageUriToPath(string uri) {
            //Converts page paths from MT Page xml (uri.ui) to paths that can be accepted by dekiscript
            XUri xuri = null;
            string ret = null;
            if(XUri.TryParse(uri, out xuri)){
                if(xuri.Path.ToLowerInvariant() == "/index.php") {
                    ret = xuri.GetParam("title", uri);
                } else {
                    ret = xuri.Path;
                }

            }
            return ret;
        }

        public static string GetApiUrl(string url) {
            if(url == null) {
                return null;
            }
            if(url.ToLower().StartsWith("/@api")) {
                return url;
            }
            return "/@api" + url;
        }

        public static bool IsRelativePath(string url) {
            return url.StartsWith("/");
        }

        public static void PersistPageInfo(ACConverterPageInfo pageInfo) {
            Stream fileStream = null;
            string filename = string.Format(@"data\{0}\{1}{2}", pageInfo.ConfluencePage.space, pageInfo.ConfluencePage.id, ".bin");
            BinaryFormatter serializer = new BinaryFormatter();
            //XmlSerializer serializer = new XmlSerializer(typeof(ACConverterPageInfo));
            try {
                FileInfo fi = new FileInfo(filename);
                fi.Directory.Create();
                fileStream = File.OpenWrite(filename);
                serializer.Serialize(fileStream, pageInfo);
                fileStream.Close();
            } catch(Exception x) {
                ACConverter.Log.WarnExceptionFormat(x, "Unable to persist page info to '{0}'", filename);
            } finally {
                if(fileStream != null) {
                    fileStream.Close();
                }
            }
        }

        public static ACConverterPageInfo RestorePageInfo(string space, string pageid) {
            ACConverterPageInfo o = null;
            Stream fileStream = null;
            string filename = string.Format(@"data\{0}\{1}{2}", space, pageid, ".bin");
            BinaryFormatter serializer = new BinaryFormatter();
            //XmlSerializer serializer = new XmlSerializer(typeof(ACConverterPageInfo));

            try {
                fileStream = File.OpenRead(filename);
                o = serializer.Deserialize(fileStream) as ACConverterPageInfo;
            } catch(Exception x) {
                ACConverter.Log.WarnExceptionFormat(x, "Unable to restore page info from '{0}'", filename);
            } finally {
                if(fileStream != null) {
                    fileStream.Close();
                }
            }
            return o;
        }

        public static string[] GetPersistedSpaces() {
            List<string> spaces = new List<string>();
            if(!Directory.Exists("data")) {
                return new string[] { };
            }

            string[] spaceDirs = Directory.GetDirectories("data");
            foreach(string s in spaceDirs) {
                string[] temp = s.Split(new char[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
                if(temp.Length == 2) {
                    spaces.Add(temp[1]);
                }
            }
            spaces.Sort();
            return spaces.ToArray();
        }

        public static string[] GetPersistedPagesInSpace(string space) {
            List<string> ret = new List<string>();
            string[] pages = Directory.GetFiles(string.Format(@"data\{0}", space), "*.bin");
            foreach(string p in pages) {
                string[] temp = p.Split(new string[] { "/", "\\", ".bin" }, StringSplitOptions.RemoveEmptyEntries);
                if(temp.Length == 3) {
                    ret.Add(temp[2]);
                }
            }
            return ret.ToArray();
        }

        public static List<Tuplet<string, string>> GetAllPersistedPageIds() {
            List<Tuplet<string, string>> ret = new List<Tuplet<string, string>>();
            List<string> spaces = new List<string>(GetPersistedSpaces());
            spaces.Sort();
            foreach(string s in spaces) {
                foreach(string p in GetPersistedPagesInSpace(s)) {
                    ret.Add(new Tuplet<string, string>(s, p));
                }
            }
            return ret;
        }       
    }
}
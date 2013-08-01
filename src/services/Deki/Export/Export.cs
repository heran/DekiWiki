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
using System.Text;

using MindTouch.Dream;
using MindTouch.Deki.Logic;
using MindTouch.IO;
using MindTouch.Tasking;
using MindTouch.Xml;

namespace MindTouch.Deki.Export {
    public static class PDFExport {

        //--- Class Methods ---
        public static Stream ExportToPDF(XDoc content) {
            string html = CreatePrinceHtml(content);
            MemoryStream stdIn = new MemoryStream(Encoding.UTF8.GetBytes(html));
            Stream stdOut;
            Stream stdErr;
            int exitCode = 0;
            
            Tuplet<int, Stream, Stream> exitValues;

            if(DekiContext.Current.Deki.PrinceXmlPath != string.Empty) {
                try {
                    string args = string.Format("--input=xml -s {0} --baseurl={1} - -o -", DekiContext.Current.UiUri.AtPath("skins/common/prince.php"), DekiContext.Current.UiUri);
                    exitValues = Async.ExecuteProcess(DekiContext.Current.Deki.PrinceXmlPath, args, stdIn, new Result<Tuplet<int, Stream, Stream>>(TimeSpan.FromMilliseconds(DekiContext.Current.Deki.PrinceXmlTimeout))).Wait();
                    exitCode = exitValues.Item1;
                    stdOut = exitValues.Item2;
                    stdErr = exitValues.Item3;
                    if (exitCode != 0) {
                        string error = string.Empty;
                        using(StreamReader sr = new StreamReader(stdErr)) {
                            error = sr.ReadToEnd();
                        }
                        DekiContext.Current.Instance.Log.DebugFormat("{0} failed with ExitCode: {1}, stdErr: {2}", DekiContext.Current.Deki.PrinceXmlPath, exitCode, error);
                        return null;
                    }
                    return stdOut.ToChunkedMemoryStream(-1, new Result<ChunkedMemoryStream>()).Wait();
                } catch (Exception e) {
                    DekiContext.Current.Instance.Log.Error("Error converting PDF", e);
                }
            }
            return null;
        } 
        public static string ExportToHtml(XDoc content) {
            return CreatePrinceHtml(content);
        }

        private static string CreatePrinceHtml(XDoc content) {
            // use authtoken for embedded images
            string authtoken = AuthBL.CreateAuthTokenForUser(DekiContext.Current.User);

            foreach(XDoc img in content["//img"]) {
                string src = img["@src"].AsText;
                if(StringUtil.StartsWithInvariant(src, "/")) {
                    src = DekiContext.Current.UiUri.Uri.SchemeHostPort + src;
                }

                if(StringUtil.StartsWithInvariantIgnoreCase(src, DekiContext.Current.UiUri.Uri.SchemeHostPort + "/") ||
                   StringUtil.StartsWithInvariantIgnoreCase(src, Utils.MKS_PATH))
                    img["@src"].ReplaceValue(new XUri(src).With("authtoken", authtoken));
            }
            
            // prepend doctype so prince uses xhtml parser
            // need to use strings instead of XDoc since XDoc doesn't allow you to set a doctype
            string doctype = "<!DOCTYPE html PUBLIC \"-//W3C//DTD XHTML 1.0 Transitional//EN\" \"http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd\">";
            return doctype + content.ToXHtml();
        }
    }
}

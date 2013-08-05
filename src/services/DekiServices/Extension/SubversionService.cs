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
using System.Text;
using System.IO;
using System.Text.RegularExpressions;

using MindTouch.Dream;
using MindTouch.Tasking;
using MindTouch.Xml;

namespace MindTouch.Deki.Services.Extension {
    using Yield = IEnumerator<IYield>;

    [DreamService("MindTouch Subversion Extension", "Copyright (c) 2006-2010 MindTouch Inc.",
        Info = "http://developer.mindtouch.com/App_Catalog/Subversion",
        SID = new string[] { 
            "sid://mindtouch.com/2008/02/svn",
            "http://services.mindtouch.com/deki/draft/2008/02/svn" 
        }
    )]

    [DreamServiceConfig("svn-uri", "string", "URI to the SVN repository")]
    [DreamServiceConfig("bugs-uri", "string?", "Optional URI to a bug tracker with the bug# replaced with '$1'. Example: http://bugs.opengarden.org/view.php?id=$1")]
    [DreamServiceConfig("svn-revision-uri", "string?", "Optional URI to a web based SVN revision viewer with the revision# replaced with '$1' . Example: http://dekiwiki.svn.sourceforge.net/viewvc/dekiwiki?view=rev&revision=$1")]
    [DreamServiceConfig("username", "string?", "Optional username for SVN repository")]
    [DreamServiceConfig("password", "string?", "Optional password for SVN repository")]
    [DreamServiceConfig("path-to-svn", "string?", "Path to the 'svn' binary on the file system. (Default: /usr/bin/svn )")]
    [DreamServiceBlueprint("deki/service-type", "extension")]
    [DekiExtLibrary(
        Label = "SVN",
        Namespace = "svn",
        Description = "This extension contains functions for integrating with Subversion source control system."
    )]
    public class SubversionService : DekiExtService {

        //--- Constants ---
        private const string MISSING_FIELD_ERROR = "SVN extension: missing configuration setting";
        private const int DEFAULT_LIMIT = 100;
        private const int DEFAULT_TIMEOUT_SECS = 60;
        private const int DEFAULT_MSG_LENGTH = 100;
        private readonly static Regex BUGLINKREGEX =
            new Regex(@"(bugfix|bug|fixes|\#)+([\s|\W]*[0]*(?<bug>\d+))+", RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture | RegexOptions.Compiled | RegexOptions.CultureInvariant);

        //--- Fields ---
        private string _username;
        private string _password;
        private XUri _uri;
        private string _svnBinPath;
        private XUri _bugUri;
        private XUri _svnRevUri;

        //--- Functions ---
        [DekiExtFunction(Description = "The SVN revision log from a given path")]
        public XDoc Table(
            [DekiExtParam("path", true)] string path,
            [DekiExtParam("revision range", true)] string range,
            [DekiExtParam("limit to maximum entries", true)] int? limit,
            [DekiExtParam("show full log message (default: false)", true)] bool? verbose,
            [DekiExtParam("stop on copy (default: true)", true)] bool? stoponcopy

        ) {
            XDoc svnRet = SvnLog(limit, false, path, range, stoponcopy);
            XDoc ret = new XDoc("html");
            ret.Start("body")
               .Start("div").Attr("class", "DW-table SVN-table table")
               .Start("table").Attr("border", 0).Attr("cellspacing", 0).Attr("cellpadding", 0).Attr("class", "table");

            // header
            ret.Start("tr")
                .Elem("th", "Revision")
                .Elem("th", "Date")
                .Elem("th", "Author")
                .Elem("th", "Message")
            .End();
            int count = 0;
            foreach (XDoc revision in svnRet["logentry"]) {
                string msg = revision["msg"].AsText ?? string.Empty;
                msg = msg.Trim();
                string tdClass = count % 2 == 0 ? "bg1" : "bg2";
                if (!(verbose ?? false)) {

                    //Trim the msg at a work boundary
                    int linebreak = StringUtil.IndexOfInvariantIgnoreCase(msg, "\n");
                    int cutoff;
                    if (linebreak > 0)
                        cutoff = Math.Min(linebreak, DEFAULT_LIMIT);
                    else
                        cutoff = DEFAULT_LIMIT;
                    while (msg.Length > cutoff && cutoff - DEFAULT_MSG_LENGTH <= 10) {
                        if (char.IsWhiteSpace(msg[cutoff]))
                            break;
                        cutoff++;
                    }

                    if (msg.Length > cutoff) {
                        msg = string.Format("{0}...", msg.Substring(0, Math.Min(msg.Length - 1, cutoff)));
                    }
                }

                DateTime d = revision["date"].AsDate ?? DateTime.MinValue;
                string date = d.ToString(DreamContext.Current.Culture);
                ret.Start("tr");
                ret.Start("td").Attr("class", tdClass);
                OutputRevLink(ret, revision["@revision"].AsText).End();
                ret.Start("td").Attr("class", tdClass).Value(date).End();
                ret.Start("td").Attr("class", tdClass).Value(revision["author"].AsText).End();
                ret.Start("td").Attr("class", tdClass);
                ParseRevMessage(ret, msg).End();
                ret.End();//tr

                count++;
            }

            ret.End();//body
            ret.End();//table
            ret.End();//div

            return ret;
        }

        private XDoc ParseRevMessage(XDoc output, string msg) {

            //Parse the log message for bugs
            MatchCollection mc = null;
            if (_bugUri != null) {
                mc = BUGLINKREGEX.Matches(msg);
            }

            if (mc == null || mc.Count == 0) {
                OutputText(output, msg);
            } else {
                int index = 0;
                foreach (Match m in mc) {
                    OutputText(output, msg.Substring(index, m.Groups[1].Index - index));
                    output.Start("a").Attr("href", new XUri(_bugUri.ToString().Replace("$1", m.Groups[1].Value))).Value(m.Groups[1].Value).End();
                    index = m.Groups[1].Index + m.Groups[1].Length;
                }
                OutputText(output, msg.Substring(index));
            }

            return output;
        }

        private XDoc OutputText(XDoc output, string text) {

            //Displays log message text with end lines preserved
            string[] segments = text.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < segments.Length - 1; i++) {
                output.Value(segments[i]).Elem("br", "");
            }
            if (segments.Length > 0) {
                output.Value(segments[segments.Length - 1]);
            }
            return output;
        }

        private XDoc OutputRevLink(XDoc output, string rev) {
            if (_svnRevUri == null) {
                output.Value("r" + rev);
            } else {
                output.Start("a").Attr("href", new XUri(_svnRevUri.ToString().Replace("$1", rev))).Value(rev).End();
            }
            return output;
        }

        private XDoc SvnLog(int? limit, bool? verbose, string path, string revrange, bool? stoponcopy) {
            StringBuilder cmdLine = new StringBuilder(string.Format("log --xml --non-interactive --limit {0} {1}", limit ?? DEFAULT_LIMIT, (verbose ?? false) ? "--verbose" : string.Empty));

            if (!string.IsNullOrEmpty(revrange)) {
                cmdLine.AppendFormat(" --revision {0}", revrange);
            }

            if (stoponcopy ?? true) {
                cmdLine.Append(" --stop-on-copy ");
            }

            if (!string.IsNullOrEmpty(_username)) {
                cmdLine.AppendFormat(" --username {0}", _username);
            }

            if (!string.IsNullOrEmpty(_password)) {
                cmdLine.AppendFormat(" --password {0}", _password);
            }

            cmdLine.AppendFormat(" {0} {1}", _uri.ToString(), path);

            Tuplet<int, Stream, Stream> exitValues = Async.ExecuteProcess(_svnBinPath, cmdLine.ToString(), null, new Result<Tuplet<int, Stream, Stream>>(TimeSpan.FromSeconds(DEFAULT_TIMEOUT_SECS))).Wait();
            int status = exitValues.Item1;
            Stream outputStream = exitValues.Item2;
            Stream errorStream = exitValues.Item3;
            if (status != 0) {
                throw new System.ArgumentException("SVN error: " + new StreamReader(errorStream).ReadToEnd());
            }

            return XDocFactory.From(new StreamReader(outputStream).ReadToEnd(), MimeType.XML);
        }

        protected override Yield Start(XDoc config, Result result) {
            yield return Coroutine.Invoke(base.Start, config, new Result());

            // read configuration settings
            _username = config["username"].AsText;
            _password = config["password"].AsText;
            _uri = config["svn-uri"].AsUri;
            if (_uri == null) {
                throw new ArgumentException(MISSING_FIELD_ERROR, "svn-uri");
            }
            _svnBinPath = config["path-to-svn"].AsText;
            if (string.IsNullOrEmpty(_svnBinPath)) {
                throw new ArgumentException(MISSING_FIELD_ERROR, "path-to-svn");
            }
            if (!System.IO.File.Exists(_svnBinPath)) {
                throw new System.IO.FileNotFoundException("SVN binary not found", _svnBinPath);
            }
            _bugUri = config["bugs-uri"].AsUri;
            _svnRevUri = config["svn-revision-uri"].AsUri;
            result.Return();
        }
    }
}

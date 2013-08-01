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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

using MindTouch.Deki.Script;
using MindTouch.Deki.Script.Runtime.Library;
using MindTouch.Dream;
using MindTouch.Tasking;
using MindTouch.Xml;

namespace MindTouch.Deki.Services {
    using Yield = IEnumerator<IYield>;

    [DreamService("MindTouch MediaWiki Extension", "Copyright (c) 2006-2010 MindTouch Inc.",
        Info = "http://developer.mindtouch.com/App_Catalog/MediaWiki",
        SID = new string[] { 
            "sid://mindtouch.com/2008/04/mediawiki",
            "http://services.mindtouch.com/deki/draft/2008/04/mediawiki" 
        }
    )]
    [DreamServiceBlueprint("deki/service-type", "extension")]
    [DekiExtLibrary(
        Label = "MediaWiki",
        Namespace = "mediawiki",
        Description = "This extension contains functions for embedding content from mediawiki.",
        Logo = "$files/mediawiki-logo.png"
    )]
    [DekiExtLibraryFiles(Prefix = "MindTouch.Deki.Services", Filenames = new string[] { "mediawiki-logo.png" })]
    public class MediWikiService : DekiExtService {

        //--- Class Fields ---
        private static readonly Regex ARG_REGEX = new Regex(@"^([a-zA-Z0-9_]+)$", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Singleline);

        //--- Fields ---
        private Dictionary<string, NS> _namespaceValues;

        private char _pageSeparator;

        //--- Methods ---
        protected override Yield Start(XDoc config, Result result) {
            yield return Coroutine.Invoke(base.Start, config, new Result());

            XDoc pageSeparatorDoc = Config["pageseparator"];
            if (!pageSeparatorDoc.IsEmpty && !String.IsNullOrEmpty(pageSeparatorDoc.Contents)) {
                _pageSeparator = pageSeparatorDoc.Contents[0];
            }

            // populate the namespace name to type mapping
            string projectName = Config["projectname"].AsText;
            _namespaceValues = new Dictionary<string, NS>();
            Type currentType = typeof(MediWikiService);
            using(StreamReader reader = new StreamReader(Assembly.GetAssembly(currentType).GetManifestResourceStream("MindTouch.Deki.Services.namespaces.txt"))) {
                string line;
                while ((line = reader.ReadLine()) != null) {
                    line = line.Trim();
                    int equalIndex = line.IndexOf("=");
                    if (0 < equalIndex) {
                        string namespaceName = line.Substring(0, equalIndex);
                        if (null != projectName) {
                            namespaceName = namespaceName.Replace("$1", projectName);
                        }
                        _namespaceValues[namespaceName.ToLowerInvariant()] = (NS)Int32.Parse(line.Substring(equalIndex + 1));
                    }
                }
            }

            result.Return();
        }

        /// <summary>
        /// Retrieve the type corresponding to the namespace name.
        /// </summary>
        public NS StringToNS(String nsString) {
            NS ns = Title.StringToNS(nsString);
            if (NS.UNKNOWN == ns) {
                if (!_namespaceValues.TryGetValue(nsString.ToLowerInvariant(), out ns)) {
                    ns = NS.UNKNOWN;
                }
            }
            return ns;
        }

        /// <summary>
        /// Helper to retrieve the namespace and path from a relative page path string
        /// </summary>
        private Title PrefixedMWPathToTitle(string fullPath) {
            NS ns = NS.MAIN;
            string path = fullPath.Trim();

            // if there is a namespace, retrieve it
            int nsPos = path.IndexOf(':');
            if (nsPos > 1) {
                ns = StringToNS(path.Substring(0, nsPos));

                // if the namespace is not found, default to using the main namespace 
                // if found, extract the namespace from the title text
                if (NS.UNKNOWN == ns) {
                    ns = NS.MAIN;
                } else {
                    path = path.Substring(nsPos + 1);
                }
            }
            return Title.FromDbPath(ns, path, null);
        }

        public bool CreateLanguageHierarchyForNS(NS ns) {

            // create a namespace heirarchy for everything but the user and template namespaces
            return (NS.USER != ns && NS.USER_TALK != ns && NS.TEMPLATE != ns && NS.TEMPLATE_TALK != ns && NS.SPECIAL != ns);
        }

        /// <summary>
        /// Converts a MediaWiki title to its MindTouch format
        /// </summary>
        /// <param name="mwTitle">The title to convert</param>
        /// <param name="replaceSeparator">If true, replace ":" with "/"</param>
        /// <returns>The converted title</returns>
        public Title MWToDWTitle(string rootPage, Title mwTitle) {
            string dbPath = null;
            string dbPrefix = null;

            // create a language heirarchy in the main namespaces
            if (CreateLanguageHierarchyForNS(mwTitle.Namespace)) {
                dbPrefix = rootPage + "/";
            }

            // prefix template pages with language to make unique accross langauges
            else if (mwTitle.IsTemplate || NS.TEMPLATE_TALK == mwTitle.Namespace || mwTitle.IsSpecial) {
                return mwTitle;
            }

            dbPath = mwTitle.AsUnprefixedDbPath();
            if ('/' != _pageSeparator) {
                dbPath = dbPath.Replace("/", "//");
                if (0 < _pageSeparator) {

                    // Replace page separator with "/"
                    String[] segments = dbPath.Split(_pageSeparator);
                    if (1 < segments.Length) {
                        StringBuilder result = new StringBuilder();
                        for (int i = 0; i < segments.Length; i++) {
                            if ((0 < i && !String.IsNullOrEmpty(segments[i - 1])) &&
                                !(i == segments.Length - 1 && String.IsNullOrEmpty(segments[i]))) {
                                result.Append("/");
                            }
                            if (String.IsNullOrEmpty(segments[i])) {
                                result.Append(_pageSeparator);
                            } else {
                                result.Append(segments[i].Trim(new char[] { '_' }));
                            }
                        }
                        dbPath = result.ToString();
                    }
                }
            }
            dbPath = dbPrefix + dbPath;
            return Title.FromDbPath(mwTitle.Namespace, dbPath.Trim('/'), null, mwTitle.Filename, mwTitle.Anchor, mwTitle.Query);
        }

        //--- Functions ---
        [DekiExtFunction(Description = "Converts MediaWiki interwiki link.")]
        public XDoc InterWiki(
            [DekiExtParam("interwiki prefix")] string prefix,
            [DekiExtParam("interwiki path")] string path,
            [DekiExtParam("title")] string title
       ) {
            if (!String.IsNullOrEmpty(prefix)) {
                string prefixValue = null;
                try { prefixValue = (Config[prefix].AsText); } catch { }
                if (!String.IsNullOrEmpty(prefixValue)) {
                    return new XDoc("html").Start("body").Start("a").Attr("href", prefixValue.Replace("$1", path)).Value(title).End().End();
                }
            }
            throw new DreamInternalErrorException(string.Format("Undefined interwiki prefix: {0}", prefix));
       }

        [DekiExtFunction(Description = "Converts MediaWiki anchorencode function")]
        public string AnchorEncode(
            [DekiExtParam("section anchor name")] string section
        ) {
            return XUri.Encode(section.Trim().Replace(' ', '_')).ReplaceAll("%3A", ":", "%", ".");
        }


        [DekiExtFunction(Description = "Converts MediaWiki grammar function.")]
        public string Grammar(
            [DekiExtParam("case")] string wordCase,
            [DekiExtParam("word to derive")] string word
        ) {
            return word;
        }

        [DekiExtFunction(Description = "Converts MediaWiki variable.")]
        public string Variable(
            [DekiExtParam("MediaWiki variable")] string var
        ) {
            throw new DreamInternalErrorException(string.Format("Undefined variable: {0}", var));
        }

        [DekiExtFunction(Name="NS", Description = "Converts MediaWiki ns function.")]
        public string GetNS(
            [DekiExtParam("namespace value")] string ns
        ) 
        {
            // check if this is a namespace number
            int intValue;
            if (Int32.TryParse(ns, out intValue)) {
                return Title.NSToString((NS)intValue);
            }

            // check if this is a recognized namespace string
            NS nsValue;
            if (_namespaceValues.TryGetValue(ns.Trim().ToLowerInvariant(), out nsValue)) {
                if (Enum.IsDefined(typeof(NS), nsValue)) {
                    return Title.NSToString(nsValue);
                }
            }
            throw new DreamInternalErrorException(string.Format("Undefined namespace: {0}", ns));
        }

        [DekiExtFunction(Description = "Converts from a MediaWiki path to a MindTouch path.")]
        public string Path(
            [DekiExtParam("path")] string path,
            [DekiExtParam("language", true)] string language
            ) {
            bool isMainInclude = false;
            path = path.Trim();

            // check for explicit declaration of the main namespace
            if (path.StartsWith(":")) {
                isMainInclude = true;
                path = path.Substring(1);
            } else {
                Title dwTitle = Title.FromPrefixedDbPath(path, null);
                if (dwTitle.IsMain) {
                    dwTitle.Namespace = NS.TEMPLATE;
                }
                path = dwTitle.AsPrefixedDbPath();
            }
            path = LocalUrl(language, path, null).Trim('/');
            if (isMainInclude) {
                path = ":" + path;
            } 

            return path;
        }

        [DekiExtFunction(Description = "Converts MediaWiki localurl function.")]
        public string LocalUrl(
            [DekiExtParam("language")] string language,
            [DekiExtParam("page")] string page,
            [DekiExtParam("query", true)] string query
        ) {

            page = page.Replace(' ', '_').Trim(new char[] { '_', ':' });
            string result = null;

            // check for interwiki link
            int interWikiEndIndex = page.IndexOf(':', 1);
            if (0 <= interWikiEndIndex) {
                string prefix = page.Substring(0, interWikiEndIndex);
                if (!String.IsNullOrEmpty(prefix)) {
                    // if the link already contains a language, use it
                    string prefixValue = null;
                    try { prefixValue = Config["rootpage-" + prefix].AsText; } catch { }
                    if (!String.IsNullOrEmpty(prefixValue)) {
                        language = prefix;
                        page = page.Substring(interWikiEndIndex + 1);
                    } else {
                        try { prefixValue = Config[prefix].AsText; } catch { }
                        if (!String.IsNullOrEmpty(prefixValue)) {
                            page = page.Substring(interWikiEndIndex + 1);
                            result = prefixValue.Replace("$1", page);
                        }
                    }
                }
            }
            if (null == result) {
                
                // check if we need to map the language
                string rootPage = String.Empty;
                if (!String.IsNullOrEmpty(language)) {
                    try { rootPage = Config["rootpage-" + language].AsText ?? String.Empty; } catch { }
                }

                // normalize the namespace and map it to the mindtouch deki location
                Title mwTitle = PrefixedMWPathToTitle(page);
                if (!StringUtil.EqualsInvariantIgnoreCase(mwTitle.Path, rootPage) &&
                    !StringUtil.StartsWithInvariantIgnoreCase(mwTitle.Path, rootPage + "/")) {
                    Title dwTitle = MWToDWTitle(rootPage, mwTitle);
                    result = dwTitle.AsPrefixedDbPath();
                } else {
                    result = mwTitle.AsPrefixedDbPath();
                }
            }

            if (null != query) {
                result += "?" + query;
            }
            return result; 
        }

        [DekiExtFunction(Description = "Converts MediaWiki localurle function.")]
        public string LocalUrlE(
            [DekiExtParam("language")] string language,
            [DekiExtParam("page")] string page,
            [DekiExtParam("query", true)] string query
        ) {
            return LocalUrl(language, page, query);
        }

        [DekiExtFunction(Description = "Converts the MediaWiki [[ ]] notation.")]
        public XDoc Internal(
            [DekiExtParam("link")] string link,
            [DekiExtParam("language", true)] string language
        ) {

            // extract the link title
            string displayName = null;
            int displayNameIndex = link.IndexOf('|');
            if (0 < displayNameIndex) {
                displayName = link.Substring(displayNameIndex + 1, link.Length - displayNameIndex - 1);
                link = link.Substring(0, displayNameIndex);
            } 

            Title title = Title.FromUIUri(null, link, true);
            if (("." == title.Path) && (title.HasAnchor)) {
                link = "#" + AnchorEncode(title.Anchor);
            } else {
                link = LocalUrl(language, title.AsPrefixedDbPath(), title.Query);
                if (title.HasAnchor) {
                    link += "#" + AnchorEncode(title.Anchor);
                }
            }

            // return the internal link
            XDoc result = new XDoc("html").Start("body").Start("a").Attr("href", link).AddNodes(DekiScriptLibrary.WebHtml(displayName ?? String.Empty, null, null, null)["body"]).End().End();
            return result;
        }

        [DekiExtFunction(Description = "Converts the MediaWiki [ ] notation.")]
        public XDoc External(
            [DekiExtParam("link")] string link
        ) {
            // store the original link value
            string originalLink = link;

            // remove spaces from the link
            link = link.Trim();

            // extract the title if there is one (indicated by the first space)
            string title = String.Empty;
            int titleIndex = link.IndexOf(' ');
            if (0 < titleIndex) {
                title = link.Substring(titleIndex + 1, link.Length - titleIndex - 1);
                link = link.Substring(0, titleIndex);
            } 

            // if the url is valid return it as a link - otherwise return the original text
            XDoc result = new XDoc("html").Start("body");
            XUri uri = null;
            if (XUri.TryParse(link, out uri)) {
                result.Start("a").Attr("href", link).AddNodes(DekiScriptLibrary.WebHtml(title, null, null, null)["body"]).End();
            } else {
                result.AddNodes(DekiScriptLibrary.WebHtml("[" + originalLink + "]", null, null, null)["body"]);
            }
            result.End();
            return result;
        }

        
        [DekiExtFunction(Description = "Converts MediaWiki template arguments to a map.")]
        public Hashtable Args(ArrayList args) {

            // return a map arguments
            // if the argument has a name use it, otherwise use the argument index as the name
            Hashtable result = new Hashtable();
            for (int i = 0; i < args.Count; i++) {
                string arg = String.Empty;
                try {
                    arg = SysUtil.ChangeType<string>(args[i]);
                } catch {}
                int equalIndex = arg.IndexOf("=");
                if (0 < equalIndex) {
                    string id = arg.Substring(0, equalIndex).Trim();
                    if(ARG_REGEX.IsMatch(id)) {
                        result[id] = arg.Substring(equalIndex + 1, arg.Length - equalIndex - 1);
                    } else {
                        result[i.ToString()] = arg;
                    }
                } else {
                    result[i.ToString()] = arg;
                }
            }
            return result;
        }
    }
}
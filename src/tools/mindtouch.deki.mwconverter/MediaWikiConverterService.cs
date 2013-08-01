/*
 * MindTouch MediaWiki Converter
 * Copyright (C) 2006-2008 MindTouch Inc.
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
using System.Collections.Specialized;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

using MindTouch.Deki;
using MindTouch.Deki.Converter;
using MindTouch.Deki.Data;
using MindTouch.Deki.Logic;
using MindTouch.Dream;
using MindTouch.Tasking;
using Mindtouch.Tools;
using MindTouch.Xml;

namespace MindTouch.Tools {
    using Yield = IEnumerator<IYield>;

    [DreamService("MindTouch MediWiki Converter Service", "MindTouch Inc. 2006",
        SID = new string[] { "http://services.mindtouch.com/deki/internal/2007/12/mediawiki-converter" }
    )]
    [DreamServiceConfig("db-mwconnection", "string?", "MediaWiki database connection string.")]
    class MediaWikiConverterService : DekiWikiService {
        private const string REDIRECT_TEXT = "#REDIRECT [[{0}]]";
        private static Regex PARAM_REGEX = new Regex(@"\{\{\{((?<intValue>\d+)|(?<namedValue>[^\{\}]+))\}\}\}", RegexOptions.Compiled);
        private static Regex VARIABLE_REGEX = new Regex(@"^(CURRENTDAY|CURRENTDAY2|CURRENTDAYNAME|CURRENTDOW|CURRENTMONTH|CURRENTMONTHABBREV|CURRENTMONTHNAME|CURRENTTIME|CURRENTHOUR|CURRENTWEEK|CURRENTYEAR|CURRENTTIMESTAMP|PAGENAME|NUMBEROFARTICLES|NUMBEROFUSERS|NAMESPACE|REVISIONDAY|REVISIONDAY2|REVISIONMONTH|REVISIONYEAR|REVISIONTIMESTAMP|SITENAME|SERVER|SERVERNAME)$", RegexOptions.Compiled);
        Dictionary<string, string> _MWToDWUserNameMap = new Dictionary<string, string>();
        Dictionary<string, ulong> _MWToDWOldIDMap = new Dictionary<string, ulong>();
        Dictionary<string, ulong> _MWToDWPageIDMap = new Dictionary<string, ulong>();
        Dictionary<Site, string> _MWInterwikiBySite;
        XDoc log = new XDoc("html");

        public override DreamFeatureStage[] Prologues {
            get {

                return new DreamFeatureStage[] { 
                    new DreamFeatureStage("set-deki-context", this.PrologueDekiContext, DreamAccess.Public), 
                    new DreamFeatureStage("set-mediawikiconverter-context", this.PrologueMediaWikiConverterContext, DreamAccess.Public)
                };
            }
        }

        public override DreamFeatureStage[] Epilogues {
            get {
                return new DreamFeatureStage[] { };
            }
        }

        private Yield PrologueMediaWikiConverterContext(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            MediaWikiConverterContext mwContext = new MediaWikiConverterContext(Config);
            DreamContext.Current.SetState<MediaWikiConverterContext>(mwContext);

            // continue processing
            response.Return(request);
            yield break;
        }

        public string OriginalTemplateDir {
            get {
                return Path.Combine(MediaWikiConverterContext.Current.MWTemplatePath, "Original");
            }
        }

        public string OverrideTemplateDir {
            get {
                return Path.Combine(MediaWikiConverterContext.Current.MWTemplatePath, "Override");
            }
        }

        public string GetTemplateFilename(Title title) {
            return String.Format("{0}.txt", title.Path.ToString());
        }

        /// <summary>
        /// Indicates which page to keep, given a group of pages with the same title but different case.
        /// All other pages will be discarded
        /// </summary>
        /// <param name="pages">Group of pages with the same title</param>
        /// <returns>The page to keep</returns>
        public PageBE GetPredominantPage(List<PageBE> pages) {
            PageBE predominantPage = pages[0];
            foreach(PageBE page in pages) {
                if(((predominantPage.IsRedirect == page.IsRedirect) && (page.TimeStamp > predominantPage.TimeStamp)) || (predominantPage.IsRedirect && !page.IsRedirect)) {
                    predominantPage = page;
                }
            }
            return predominantPage;
        }

        public bool CreateLanguageHierarchyForNS(NS ns) {

            // create a namespace heirarchy for everything but the user and template namespaces
            return (NS.USER != ns && NS.USER_TALK != ns && NS.TEMPLATE != ns && NS.TEMPLATE_TALK != ns && NS.SPECIAL != ns);
        }


        /// <summary>
        /// Converts a MediaWiki title to its MindTouch format
        /// </summary>
        /// <param name="mwTitle">The title to convert</param>
        /// <returns>The converted title</returns>
        public Title MWToDWTitle(Site site, Title mwTitle) {
            return MWToDWTitle(site, mwTitle, true);
        }

        /// <summary>
        /// Converts a MediaWiki title to its MindTouch format
        /// </summary>
        /// <param name="mwTitle">The title to convert</param>
        /// <param name="replaceSeparator">If true, replace ":" with "/"</param>
        /// <returns>The converted title</returns>
        public Title MWToDWTitle(Site site, Title mwTitle, bool useNewFormat) {
            string dbPrefix = null;
            string dbPath = mwTitle.AsUnprefixedDbPath();
            string displayName = mwTitle.DisplayName;


            // create a language heirarchy in the main namespaces
            if(CreateLanguageHierarchyForNS(mwTitle.Namespace)) {
                dbPrefix = site.DWRootPage + "/";
                if(site.MWRootPage == mwTitle.Path && useNewFormat) {
                    displayName = displayName ?? mwTitle.AsUserFriendlyName();
                    dbPath = String.Empty;
                }
            }

            // prefix template pages with language to make unique accross langauges
            else if(mwTitle.IsTemplate || NS.TEMPLATE_TALK == mwTitle.Namespace || mwTitle.IsSpecial) {
                return mwTitle;
            }

            // if this is a user page that corresponds to a renamed user, rename the page.
            else if(mwTitle.IsUser || NS.USER_TALK == mwTitle.Namespace) {
                string parentSegment = mwTitle.AsUnprefixedDbSegments()[0];
                string newParentSegment;
                if(_MWToDWUserNameMap.TryGetValue(parentSegment, out newParentSegment)) {
                    dbPath = newParentSegment + mwTitle.Path.Substring(newParentSegment.Length - 1);
                }
            }

            if('/' != MediaWikiConverterContext.Current.MWPageSeparator) {
                dbPath = dbPath.Replace("/", "//");

                // If desired, replace the page separator with "/"
                if(useNewFormat && 0 < MediaWikiConverterContext.Current.MWPageSeparator) {
                    String[] segments = dbPath.Split(MediaWikiConverterContext.Current.MWPageSeparator);
                    if(1 < segments.Length) {
                        StringBuilder result = new StringBuilder();
                        for(int i = 0; i < segments.Length; i++) {
                            if((0 < i && !String.IsNullOrEmpty(segments[i - 1])) &&
                                !(i == segments.Length - 1 && String.IsNullOrEmpty(segments[i]))) {
                                result.Append("/");
                            }
                            if(String.IsNullOrEmpty(segments[i])) {
                                result.Append(MediaWikiConverterContext.Current.MWPageSeparator);
                            } else {
                                result.Append(segments[i].Trim(new char[] { '_' }));
                            }
                        }
                        dbPath = result.ToString();
                    }
                }
            }

            dbPath = dbPrefix + dbPath;
            return Title.FromDbPath(mwTitle.Namespace, dbPath.Trim('/'), displayName, mwTitle.Filename, mwTitle.Anchor, mwTitle.Query);
        }

        public void MWToDWTitles(Site site, PageBE page, bool isOld, XDoc xml) {

            // set the display name to the title-override value 
            page.Title.DisplayName = xml["//mediawiki:extension[@function='title-override']"].AsText;

            // map each page include to its new MindTouch location
            foreach(XDoc template in xml["//mediawiki:template"].ToList()) {
                XDoc templateName = template["mediawiki:name"];
                if(0 == templateName.Elements.ListLength) {
                    string pagePath = template["mediawiki:name"].Contents.Trim();
                    if(pagePath.StartsWith(":")) {
                        Title mwTitle = Title.FromUIUri(null, pagePath.Substring(1), false);
                        templateName.ReplaceValue(":" + MWToDWTitle(site, mwTitle).AsPrefixedDbPath());
                    } else {
                        Title mwTitle = null;
                        mwTitle = Title.FromUIUri(null, pagePath, false);
                        mwTitle.Namespace = NS.TEMPLATE;
                        templateName.ReplaceValue(MWToDWTitle(site, mwTitle).AsUnprefixedDbPath());
                    }
                }
                #region logging
                if(MediaWikiConverterContext.Current.LoggingEnabled) {
                    // log all template references
                    StringBuilder templateString = new StringBuilder();
                    templateString.Append(template["mediawiki:name"].AsText);
                    templateString.Append('(');
                    bool isFirstArg = true;
                    foreach(XDoc templateArg in template["mediawiki:arg"]) {
                        if(!isFirstArg) {
                            templateString.Append(", ");
                        } else {
                            isFirstArg = false;
                        }
                        templateString.Append(templateArg.Contents);
                    }
                    templateString.Append(")");
                    log["/html/body/table[@id='templates']"].Add(new XDoc("tr").Elem("td", (isOld ? String.Format("Revision ({0}):  ", page.TimeStamp) : String.Empty) + MWToDWTitle(site, page.Title).AsPrefixedDbPath()).Elem("td", templateString.ToString()));
                }
                #endregion
            }

            foreach(XDoc function in xml["//mediawiki:function"].ToList()) {
                #region logging
                if(MediaWikiConverterContext.Current.LoggingEnabled) {

                    // log all function references
                    if(!page.Title.IsTemplate) {
                        string functionString = String.Format("{0}({1})", function["@name"].AsText, function["@arg"].Contents);
                        log["/html/body/table[@id='functions']"].Add(new XDoc("tr").Elem("td", (isOld ? String.Format("Revision ({0}):  ", page.TimeStamp) : String.Empty) + MWToDWTitle(site, page.Title).AsPrefixedDbPath()).Elem("td", functionString));
                    }
                }
                #endregion
            }

            // map links to pages on the wiki in a different language to internal links
            foreach(XDoc link in xml["//mediawiki:link[@type='external']"].ToList()) {
                string href = link["@href"].Contents;
                XUri xuri;
                if(XUri.TryParse(href, out xuri)) {
                    if("lang" == xuri.Scheme.ToLowerInvariant()) {
                        link.Attr("type", "internal");
                    }
                }
            }

            // map file links to image links
            foreach(XDoc link in xml["//mediawiki:link[@type='file']"].ToList()) {
                link.Attr("ns", 6);
                link.Attr("type", "internal");
                link.Attr("href", "Image:" + link["@href"].Contents);
            }

            // map each internal link to its new MindTouch location
            foreach(XDoc link in xml["//mediawiki:link[@type='internal']"].ToList()) {

                Site linkSite = site;

                // if this is a link to a page on the wiki in a different language, map it accordingly
                XUri xuri;
                if(XUri.TryParse(link["@href"].Contents, out xuri)) {
                    if("lang" == xuri.Scheme.ToLowerInvariant()) {
                        linkSite = MediaWikiConverterContext.Current.GetMWSite(xuri.Authority);
                        link.Attr("href", xuri.PathQueryFragment);
                    }
                }

                Title dwTitle = Title.FromUIUri(page.Title, link["@href"].Contents);
                switch(link["@ns"].AsInt) {

                case -1:
                    dwTitle = Title.FromUIUri(page.Title, XUri.Decode(link["@href"].Contents));
                    dwTitle = MWToDWTitle(linkSite, dwTitle);
                    #region logging
                    PageBE specialPage = PageBL.GetPageByTitle(dwTitle);
                    if((specialPage == null || specialPage.ID == 0) && !StringUtil.StartsWithInvariantIgnoreCase(dwTitle.Path, "Contributions/")) {
                        if(MediaWikiConverterContext.Current.LoggingEnabled) {
                            log["/html/body/table[@id='specialpages']"].Add(new XDoc("tr").Elem("td", MWToDWTitle(site, page.Title).AsPrefixedDbPath()).Elem("td", dwTitle.AsPrefixedDbPath()));
                        }
                    }
                    #endregion
                    break;

                // media links are remapped to the a link to the image on the media gallery
                case 6:
                case 7:
                    string filename = dwTitle.Path.Substring(6);
                    dwTitle = DWMediaGalleryTitle(linkSite);
                    dwTitle.Filename = filename;
                    break;

                // category links are remapped to Special:Tags
                case 14:
                case 15:
                    if(string.IsNullOrEmpty(link.Contents)) {
                        link.ReplaceValue(dwTitle.AsUserFriendlyName());
                    }
                    dwTitle = Title.FromDbPath(NS.SPECIAL, "Tags", null, null, null, String.Format("tag={0}&language={1}", dwTitle.Path.Substring(9), linkSite.Language));
                    break;

                default:
                    dwTitle = MWToDWTitle(linkSite, dwTitle);
                    break;
                };
                if(string.IsNullOrEmpty(link.Contents)) {
                    link.ReplaceValue(link["@href"].Contents);
                }
                if(!link["@href"].Contents.StartsWith("#")) {
                    link.Attr("href", dwTitle.AsUiUriPath());
                }
            }

            // map each internal image to its new MindTouch location
            foreach(XDoc image in xml["//mediawiki:image[@type='internal']"].ToList()) {
                Title mediaGalleryTitle = DWMediaGalleryTitle(site);
                mediaGalleryTitle.Filename = image["@href"].Contents;
                image.Attr("href", mediaGalleryTitle.AsUiUriPath());
            }
        }

        public Title DWMediaGalleryTitle(Site site) {
            return MWToDWTitle(site, Title.FromDbPath(NS.MAIN, "Media_Gallery", null));
        }

        public string MWToDWUserName(string mwName) {
            uint suffix = 0;
            UserBE matchingUser = null;
            string dwName;
            do {
                dwName = mwName;
                if(suffix > 0)
                    dwName = dwName + suffix.ToString();

                matchingUser = DbUtils.CurrentSession.Users_GetByName(dwName);
                suffix++;
            } while(matchingUser != null);

            return dwName;
        }

        public bool OverrideTemplate(Site site, PageBE page) {
            if(page.Title.IsTemplate && !String.IsNullOrEmpty(MediaWikiConverterContext.Current.MWTemplatePath)) {
                string templateFilename = GetTemplateFilename(page.Title);
                string templateOriginal = Path.Combine(OriginalTemplateDir, site.Language + "-" + templateFilename);

                // check the template against its base version to see if it's changed
                if(!File.Exists(templateOriginal)) {
                    #region logging
                    if(MediaWikiConverterContext.Current.LoggingEnabled) {
                        log["/html/body/table[@id='outdatedtemplates']"].Add(new XDoc("tr").Elem("td", MWToDWTitle(site, page.Title).AsPrefixedDbPath()));
                    }
                    #endregion
                    File.WriteAllLines(templateOriginal, new string[] { page.TimeStamp.ToString() });
                    File.AppendAllText(templateOriginal, page.GetText(DbUtils.CurrentSession));
                } else {
                    DateTime baseTimestamp = DateTime.MinValue;
                    string[] lines = File.ReadAllLines(templateOriginal);
                    if(0 < lines.Length) {
                        DateTime.TryParse(lines[0], out baseTimestamp);
                    }
                    if(DateTime.MinValue == baseTimestamp || baseTimestamp < page.TimeStamp) {
                        #region logging
                        if(MediaWikiConverterContext.Current.LoggingEnabled) {
                            log["/html/body/table[@id='outdatedtemplates']"].Add(new XDoc("tr").Elem("td", MWToDWTitle(site, page.Title).AsPrefixedDbPath()));
                        }
                        #endregion
                        File.WriteAllLines(templateOriginal, new string[] { page.TimeStamp.ToString() });
                        File.AppendAllText(templateOriginal, page.GetText(DbUtils.CurrentSession));

                    }
                }

                // check if the template's content has been overriden
                string templateOverride = Path.Combine(OverrideTemplateDir, templateFilename);
                if(File.Exists(templateOverride)) {
                    page.SetText(File.ReadAllText(templateOverride));
                    page.ContentType = DekiMimeType.DEKI_TEXT;
                    ParserResult parserResult = DekiXmlParser.Parse(page, ParserMode.SAVE, -1, false);
                    page.IsRedirect = (null != parserResult.RedirectsToTitle) || (null != parserResult.RedirectsToUri);
                    return true;
                }
            }
            return false;
        }

        public void MWToDWContent(Site site, PageBE page, bool isOld) {
            try {

                // check for template content overrides
                if(OverrideTemplate(site, page)) {
                    return;
                }

                Plug converterUri = MediaWikiConverterContext.Current.MWConverterUri;
                if(null != converterUri) {
                    string interwikiInfo = String.Empty;
                    if(null != _MWInterwikiBySite) {
                        _MWInterwikiBySite.TryGetValue(site, out interwikiInfo);
                    }
                    XDoc xml = converterUri.With("title", page.Title.AsPrefixedDbPath()).With("lang", site.Language).With("site", site.Name).With("interwiki", interwikiInfo).With("text", page.GetText(DbUtils.CurrentSession)).PostAsForm().ToDocument();
                    xml.UsePrefix("mediawiki", "#mediawiki");

                    // if this is a redirect, set the page text accordingly]
                    if(page.IsRedirect) {
                        ParserResult result = DekiXmlParser.Parse(page, ParserMode.SAVE, -1, false);
                        if(result.RedirectsToTitle != null) {
                            page.SetText(String.Format(REDIRECT_TEXT, MWToDWTitle(site, Title.FromUIUri(null, xml["//mediawiki:link/@href"].Contents, true)).AsPrefixedDbPath().Replace("&", "&amp;")));
                        } else {
                            page.SetText(String.Format(REDIRECT_TEXT, xml["//mediawiki:link/@href"].Contents.Replace("&", "&amp;"), true));
                        }
                        page.ContentType = DekiMimeType.DEKI_XML0805;
                        return;
                    }

                    // remove extra paragraph tags from templates
                    if(page.Title.IsTemplate) {
                        List<XDoc> topLevelParagraphs = xml["/html/body/p"].ToList();
                        if(1 == topLevelParagraphs.Count) {
                            topLevelParagraphs[0].ReplaceWithNodes(topLevelParagraphs[0]);
                        }
                    }

                    // Map MediaWiki titles to MindTouch
                    MWToDWTitles(site, page, isOld, xml);

                    // Convert from MediaWiki output to MindTouch
                    WikiTextProcessor.Convert(site, xml, page.Title.IsTemplate);

                    // If the page is available in other languages, insert wiki.languages 
                    List<XDoc> languageList = xml["//mediawiki:meta[@type='language']"].ToList();
                    if(0 < languageList.Count) {
                        StringBuilder languageData = new StringBuilder("{{ wiki.languages( { ");
                        for(int i = 0; i < languageList.Count; i++) {
                            if(0 < i) {
                                languageData.Append(", ");
                            }
                            string relatedLanguage = languageList[i]["@language"].AsText;
                            Title relatedTitle = Title.FromUIUri(null, languageList[i].Contents, false);
                            Site relatedSite = MediaWikiConverterContext.Current.GetMWSite(relatedLanguage);
                            languageData.AppendFormat("{0}: {1}", StringUtil.QuoteString(relatedLanguage), StringUtil.QuoteString(MWToDWTitle(relatedSite, relatedTitle).AsPrefixedDbPath()));
                            languageList[i].Remove();
                        }
                        languageData.Append(" } ) }}");
                        xml["//body"].Value(languageData.ToString());
                    }

                    string contents = xml.ToString();
                    int first = contents.IndexOf("<body>");
                    int last = contents.LastIndexOf("</body>");
                    if((first >= 0) && (last >= 0)) {
                        page.SetText(contents.Substring(first + 6, last - (first + 6)));
                        page.ContentType = DekiMimeType.DEKI_TEXT;
                    }
                }
            } catch(Exception e) {
                Console.Out.WriteLine("An unexpected exception has occurred:");
                Console.Out.WriteLine(e.GetCoroutineStackTrace());
                Console.Out.WriteLine("MediaWiki page text that produced the exception:");
                Console.Out.WriteLine(page.GetText(DbUtils.CurrentSession));
            }
        }

        /// <summary>
        /// Convert MediaWiki users to MindTouch users
        /// </summary>
        public void ConvertUsers() {
            Console.Out.Write("Migrating users...  ");

            // Delete any existing users
            if(!MediaWikiConverterContext.Current.Merge) {
                MediaWikiDA.DeleteDWUsers();

                // Create the anonymous user with an unused user ID, so that it won't get trampled
                UserBE anonUser = new UserBE();
                anonUser.Name = DekiWikiService.ANON_USERNAME;
                anonUser.RoleId = 3;
                anonUser.ServiceId = 1;
                uint anonUserID = DbUtils.CurrentSession.Users_Insert(anonUser);
                MediaWikiDA.UpdateDWUserID(anonUserID, 0);
                long maxUserID = 0;

                UserBE[] users = MediaWikiDA.GetUsers();
                foreach(UserBE user in users) {
                    uint oldID = user.ID;
                    maxUserID = Math.Max(oldID, maxUserID);
                    string oldName = user.Name;
                    user.Name = MWToDWUserName(user.Name);
                    if(oldName != user.Name) {
                        _MWToDWUserNameMap.Add(Title.FromUIUsername(oldName).Path, Title.FromUIUsername(user.Name).Path);
                        #region logging
                        if(MediaWikiConverterContext.Current.LoggingEnabled) {
                            log["/html/body/table[@id='renamedUsers']"].Add(new XDoc("tr").Elem("td", oldName).Elem("td", user.Name));
                        }
                        #endregion
                    }
                    uint userId = DbUtils.CurrentSession.Users_Insert(user);
                    MediaWikiDA.UpdateDWUserID(userId, oldID);
                }

                // Update the anonymous user ID to be valid
                MediaWikiDA.UpdateDWUserID(anonUserID, (uint)maxUserID + 1);
            }
            Console.Out.WriteLine("Done!");
        }

        public void ConvertIPBlocks() {
            Console.Out.Write("Migrating ipblocks...  ");

            if(!MediaWikiConverterContext.Current.Merge) {
                MediaWikiDA.DeleteDWIPBlocks();
                IPBlockBE[] ipBlocks = MediaWikiDA.GetIPBlocks();
                foreach(IPBlockBE ipBlock in ipBlocks) {
                    MediaWikiDA.InsertDWIPBlock(ipBlock);
                }
            }
            Console.Out.WriteLine("Done!");
        }

        public void ConvertConfiguration() {
            Console.Out.Write("Migrating configuration...  ");

            // Delete exisiting custom extensions if they exist
            IList<ServiceBE> services = DbUtils.CurrentSession.Services_GetAll();
            foreach(ServiceBE existingService in services) {
                if(StringUtil.EqualsInvariantIgnoreCase(existingService.Description, "Custom RSS Extension") ||
                    StringUtil.EqualsInvariantIgnoreCase(existingService.Description, "MediaWiki Extension")) {
                    MediaWikiDA.DeleteDWServiceById(existingService.Id);
                }
            }

            // Register the Custom RSS Extension
            ServiceBE rssService = new ServiceBE();
            rssService.Type = ServiceType.EXT;
            rssService.SID = "http://services.mindtouch.com/deki/draft/2007/12/dekiscript";
            rssService.Description = "Custom RSS Extension";
            rssService._ServiceEnabled = 1;
            rssService._ServiceLocal = 1;
            rssService.Config.Add("manifest", "http://scripts.mindtouch.com/ajaxrss.xml");
            MediaWikiDA.InsertDWService(rssService);

            // Register the Media Wiki Extension
            ServiceBE mwService = new ServiceBE();
            mwService.Type = ServiceType.EXT;
            mwService.SID = "http://services.mindtouch.com/deki/draft/2008/04/mediawiki";
            mwService.Description = "MediaWiki Extension";
            mwService._ServiceEnabled = 1;
            mwService._ServiceLocal = 1;

            // populate the config keys used for interwiki links
            Dictionary<Site, NameValueCollection> interWikiBySite = MediaWikiDA.GetInterWikiBySite();
            _MWInterwikiBySite = new Dictionary<Site, string>();
            Dictionary<string, string> interWiki = new Dictionary<string, string>();
            foreach(KeyValuePair<Site, NameValueCollection> interWikiPair in interWikiBySite) {
                foreach(string key in interWikiPair.Value.Keys) {
                    if(_MWInterwikiBySite.ContainsKey(interWikiPair.Key)) {
                        _MWInterwikiBySite[interWikiPair.Key] += "," + key;
                    } else {
                        _MWInterwikiBySite[interWikiPair.Key] = key;
                    }
                    string normalized_key = key.Replace(' ', '_').ToLowerInvariant();
                    string value = null;
                    if(interWiki.TryGetValue(normalized_key, out value)) {
                        if(value != interWikiPair.Value[normalized_key]) {
                            #region logging
                            if(MediaWikiConverterContext.Current.LoggingEnabled) {
                                log["/html/body/table[@id='interWikiConflicts']"].Add(new XDoc("tr").Elem("td", normalized_key).Elem("td", value).Elem("td", interWikiPair.Value[key] + "(" + interWikiPair.Key.Language + ")"));
                            }
                            #endregion
                        }
                    } else {
                        interWiki.Add(normalized_key, interWikiPair.Value[key]);
                        mwService.Config.Set(normalized_key, interWikiPair.Value[key]);
                    }
                }
            }

            // populate the config keys used to map from language to language root
            foreach(Site site in MediaWikiConverterContext.Current.MWSites) {
                mwService.Config.Set("rootpage-" + site.Language, site.DWRootPage);
                mwService.Config.Set("projectname", site.Name);
                mwService.Config.Set("pageseparator", MediaWikiConverterContext.Current.MWPageSeparator.ToString());
            }

            MediaWikiDA.InsertDWService(mwService);

            // configure the wiki languages
            StringBuilder languages = new StringBuilder();
            foreach(Site site in MediaWikiConverterContext.Current.MWSites) {
                if(0 < languages.Length) {
                    languages.Append(",");
                }
                languages.Append(site.Language);
            }
            ConfigBL.SetInstanceSettingsValue("languages", languages.ToString());

            Console.Out.WriteLine("Done!");
        }

        public PageBE EnsureParent(bool isRedirect, PageBE page) {
            var resources = DekiContext.Current.Resources;
            PageBE parentPage = null;
            Title parentTitle = page.Title.GetParent();

            // attempt to retrieve the parent and create it if not found
            if(!page.Title.IsTalk && null != parentTitle) {
                parentPage = PageBL.GetPageByTitle(parentTitle);
                if((null == parentPage) || (0 == parentPage.ID) || (!isRedirect && parentPage.IsRedirect)) {
                    parentPage.SetText(resources.Localize(DekiResources.EMPTY_PARENT_ARTICLE_TEXT()));
                    parentPage.ContentType = DekiMimeType.DEKI_TEXT;
                    parentPage.Comment = parentPage.TIP = String.Empty;
                    parentPage.TimeStamp = parentPage.Touched = DateTime.Now;
                    parentPage.Language = page.Language;
                    PageBE grandparentPage = EnsureParent(isRedirect, parentPage);
                    if(null != grandparentPage) {
                        parentPage.ParentID = grandparentPage.Title.IsRoot ? 0 : grandparentPage.ID;
                    }
                    if(0 == parentPage.ID) {
                        uint revisionNumber;
                        ulong parentPageId = DbUtils.CurrentSession.Pages_Insert(parentPage, 0);
                        parentPage.ID = parentPageId;
                    } else {
                        DbUtils.CurrentSession.Pages_Update(parentPage);
                        parentPage = PageBL.GetPageById(parentPage.ID);
                    }
                }
            }
            return parentPage;
        }


        /// <summary>
        /// Convert MediaWiki pages to MindTouch pages
        /// </summary>
        public void ConvertPages(out Title[] titles, out Dictionary<Title, List<PageBE>> oldTitleToPageMap) {
            Console.Out.Write("Migrating pages...  ");

            if(!MediaWikiConverterContext.Current.Merge) {
                MediaWikiDA.DeleteDWLinks();
                MediaWikiDA.DeleteDWPages();
            }

            Dictionary<Site, List<PageBE>> pagesBySite = MediaWikiDA.GetPagesBySite();
            oldTitleToPageMap = new Dictionary<Title, List<PageBE>>();

            foreach(Site site in pagesBySite.Keys) {

                // group by pages having the same title
                foreach(PageBE page in pagesBySite[site]) {

                    // convert from MediaWiki to MindTouch content
                    MWToDWContent(site, page, false);
                    Title oldTitle = MWToDWTitle(site, page.Title, false);
                    page.Title = MWToDWTitle(site, page.Title);
                    List<PageBE> pagesByTitle;
                    oldTitleToPageMap.TryGetValue(oldTitle, out pagesByTitle);
                    if(null == pagesByTitle) {
                        pagesByTitle = new List<PageBE>();
                        pagesByTitle.Add(page);
                        oldTitleToPageMap.Add(oldTitle, pagesByTitle);
                    } else {
                        pagesByTitle.Add(page);
                    }
                }

                // create a media gallery page to hold images
                PageBE mediaGalleryPage = new PageBE();
                mediaGalleryPage.Title = DWMediaGalleryTitle(site);
                mediaGalleryPage.ContentType = DekiMimeType.DEKI_TEXT;
                mediaGalleryPage.Comment = mediaGalleryPage.TIP = String.Empty;
                mediaGalleryPage.TimeStamp = mediaGalleryPage.Touched = DateTime.Now;
                mediaGalleryPage.Language = site.Language;
                mediaGalleryPage.SetText(String.Empty);
                oldTitleToPageMap.Add(mediaGalleryPage.Title, new List<PageBE>(new PageBE[] { mediaGalleryPage }));
            }

            // order by new title length so that parent pages are created before their children
            titles = new Title[oldTitleToPageMap.Keys.Count];
            int[] titleLengths = new int[titles.Length];
            oldTitleToPageMap.Keys.CopyTo(titles, 0);
            for(int i = 0; i < titles.Length; i++) {
                titleLengths[i] = GetPredominantPage(oldTitleToPageMap[titles[i]]).Title.AsUnprefixedDbPath().Length;
            }
            Array.Sort(titleLengths, titles);

            // save the pages
            foreach(Title title in titles) {
                List<PageBE> pagesByTitle = oldTitleToPageMap[title];
                PageBE predominantPage = GetPredominantPage(pagesByTitle);
                ulong oldID = predominantPage.ID;
                string oldLanguage = predominantPage.Language;

                // ensure the parent exists and set the parent id
                PageBE parentPage = EnsureParent(predominantPage.IsRedirect, predominantPage);
                if(null != parentPage) {
                    predominantPage.ParentID = parentPage.Title.IsRoot ? 0 : parentPage.ID;
                }

                // detect conflicts
                List<PageBE> conflictedPages = new List<PageBE>();
                bool differsByLanguage = true;
                foreach(PageBE page in pagesByTitle) {
                    if((predominantPage != page) && (page.GetText(DbUtils.CurrentSession).Trim() != predominantPage.GetText(DbUtils.CurrentSession).Trim()) && !page.IsRedirect) {
                        conflictedPages.Add(page);
                        if(page.Language == predominantPage.Language) {
                            differsByLanguage = false;
                        }
                    }
                }

                // detect if a page with the same title already exists
                PageBE pageWithMatchingName = PageBL.GetPageByTitle(predominantPage.Title);
                if((null != pageWithMatchingName) && (0 != pageWithMatchingName.ID)) {
                    conflictedPages.Add(predominantPage);
                    predominantPage = pageWithMatchingName;
                } else {

                    // for templates, add each language version in a localized section
                    if(predominantPage.Title.IsTemplate) {
                        List<string> languages = new List<string>();
                        if(!File.Exists(Path.Combine(OverrideTemplateDir, GetTemplateFilename(predominantPage.Title)))) {
                            String pageText = String.Empty;
                            foreach(PageBE page in pagesByTitle) {
                                if(page.IsRedirect) {
                                    ParserResult result = DekiXmlParser.Parse(page, ParserMode.SAVE, -1, false);
                                    languages.Add(page.Language);
                                    if(result.RedirectsToTitle != null) {
                                        pageText = String.Format("{0}<span lang='{1}' class='lang lang-{1}'>{{{{wiki.template('{2}', args)}}}}</span>", pageText, page.Language, result.RedirectsToTitle.AsPrefixedDbPath());
                                        if("en" == page.Language || 1 == pagesByTitle.Count) {
                                            pageText = String.Format("{0}<span lang='{1}' class='lang lang-{1}'>{{{{wiki.template('{2}', args)}}}}</span>", pageText, "*", result.RedirectsToTitle.AsPrefixedDbPath());
                                        }
                                    }
                                } else {
                                    languages.Add(page.Language);
                                    pageText = String.Format("{0}<span lang='{1}' class='lang lang-{1}'>{2}</span>", pageText, page.Language, page.GetText(DbUtils.CurrentSession));
                                    if("en" == page.Language || 1 == pagesByTitle.Count) {
                                        pageText = String.Format("{0}<span lang='{1}' class='lang lang-{1}'>{2}</span>", pageText, "*", page.GetText(DbUtils.CurrentSession));
                                    }
                                }
                            }
                            predominantPage.SetText(pageText);
                        }
                        predominantPage.Language = String.Empty;
                        #region logging
                        if(1 < languages.Count && MediaWikiConverterContext.Current.LoggingEnabled) {
                            log["/html/body/table[@id='pagesMerged']"].Add(new XDoc("tr").Elem("td", predominantPage.Title.AsPrefixedDbPath()).Elem("td", String.Join(" ", languages.ToArray())));
                        }
                        #endregion
                    } else if(differsByLanguage && 0 < conflictedPages.Count) {
                        List<string> languages = new List<string>();
                        String pageText = String.Empty;
                        foreach(PageBE page in pagesByTitle) {
                            if(!languages.Contains(page.Language)) {
                                languages.Add(page.Language);
                                if(page.IsRedirect) {
                                    ParserResult result = DekiXmlParser.Parse(page, ParserMode.SAVE, -1, false);

                                    // TODO (steveb): not sure how to handle external redirects

                                    if(result.RedirectsToTitle != null) {
                                        page.SetText("{{wiki.page('" + result.RedirectsToTitle.AsPrefixedDbPath() + "')}}");
                                    }
                                }
                                pageText = String.Format("{0}\n<div style='border: 1px solid #777777; margin: 10px 0px; padding: 0px 10px; background-color: #eeeeee; font-weight: bold; text-align: center;'><p style='margin: 4px 0px;'>Content merged from {1}</p></div>\n{2}", pageText, page.Language, page.GetText(DbUtils.CurrentSession));
                            }
                        }
                        predominantPage.SetText(pageText);
                        predominantPage.Language = String.Empty;
                        #region logging
                        if(MediaWikiConverterContext.Current.LoggingEnabled) {
                            log["/html/body/table[@id='pagesMerged']"].Add(new XDoc("tr").Elem("td", predominantPage.Title.AsPrefixedDbPath()).Elem("td", String.Join(" ", languages.ToArray())));
                        }
                        #endregion
                    }

                    // save the page
                    uint revisionNumber;
                    ulong predominantPageId = DbUtils.CurrentSession.Pages_Insert(predominantPage, 0);
                    predominantPage = PageBL.GetPageById(predominantPageId);
                    if(0 != oldID) {
                        _MWToDWPageIDMap.Add(oldLanguage + oldID, predominantPage.ID);
                    }

                    // generate redirect page if the title has changed
                    Title oldTitle = title;
                    if(predominantPage.Title != oldTitle) {
                        PageBE redirect = new PageBE();
                        redirect.Title = oldTitle;
                        redirect.SetText(String.Format(REDIRECT_TEXT, predominantPage.Title.AsPrefixedDbPath().Replace("&", "&amp;")));
                        redirect.Comment = redirect.TIP = String.Empty;
                        redirect.TimeStamp = redirect.Touched = DateTime.Now;
                        redirect.ContentType = DekiMimeType.DEKI_XML0805;
                        redirect.Language = predominantPage.Language;
                        redirect.IsRedirect = true;
                        uint tempRevisionNumber;
                        DbUtils.CurrentSession.Pages_Insert(redirect, 0);
                    }
                }

                // Report any title conflicts to the user
                if(0 < conflictedPages.Count) {
                    foreach(PageBE page in conflictedPages) {
                        #region logging
                        if(!page.IsRedirect && !differsByLanguage && MediaWikiConverterContext.Current.LoggingEnabled) {
                            log["/html/body/table[@id='removedPages']"].Add(new XDoc("tr").Elem("td", page.Title.AsPrefixedDbPath()));
                        }
                        #endregion
                    }
                }
            }

            Console.Out.WriteLine("Done!");
        }

        public void ConvertFiles() {
            Console.Out.Write("Migrating files...  ");

            if(!MediaWikiConverterContext.Current.Merge) {
                MediaWikiDA.DeleteDWFiles();
            }

            MD5 MD5 = MD5CryptoServiceProvider.Create();
            Dictionary<Site, List<ResourceBE>> filesBySite = MediaWikiDA.GetFilesBySite();
            foreach(Site site in filesBySite.Keys) {
                PageBE mediaGalleryPage = PageBL.GetPageByTitle(DWMediaGalleryTitle(site));
                ResourceBE previous = null;

                foreach(ResourceBE file in filesBySite[site]) {
                    ResourceBE currentFile = file;
                    try {

                        currentFile.ParentPageId = (uint)mediaGalleryPage.ID;
                        if((null != previous) && (previous.Name == currentFile.Name) && (previous.ParentPageId == currentFile.ParentPageId)) {
                            currentFile.ResourceId = previous.ResourceId;
                            currentFile.Revision = previous.Revision + 1;

                        } else {
                            previous = null;
                        }

                        string utf8FileName = currentFile.MetaXml["physicalfilename"].Contents;
                        string latin1FileName = Encoding.GetEncoding("ISO-8859-1").GetString(Encoding.UTF8.GetBytes(utf8FileName));
                        uint fileid = ResourceMapBL.GetNewFileId();
                        currentFile.MetaXml.FileId = fileid;
                        // Save revision to db
                        currentFile = (ResourceBE)DbUtils.CurrentSession.Resources_SaveRevision(currentFile);
                        ResourceMapBL.UpdateFileIdMapping(fileid, currentFile.ResourceId);

                        // store the file on the file system
                        string sourcePath = site.ImageDir;
                        if(currentFile.Name != utf8FileName) {
                            sourcePath = Path.Combine(site.ImageDir, "archive");
                        }

                        string md5HashString = MD5.ComputeHash(Encoding.UTF8.GetBytes(currentFile.Name))[0].ToString("x2");
                        sourcePath = sourcePath + Path.DirectorySeparatorChar + md5HashString[0] + Path.DirectorySeparatorChar + md5HashString;
                        string fileName = Path.Combine(sourcePath, latin1FileName);
                        if(!File.Exists(fileName)) {
                            char[] fileNameChars = latin1FileName.ToCharArray();
                            for(int i = 0; i < fileNameChars.Length; i++) {
                                if(128 <= fileNameChars[i] && 159 >= fileNameChars[i]) {
                                    fileNameChars[i] = '?';
                                }
                            }
                            string[] files = Directory.GetFiles(sourcePath, new string(fileNameChars));
                            if(0 < files.Length) {
                                fileName = Directory.GetFiles(sourcePath, new string(fileNameChars))[0];
                            } else {
                                continue;
                            }
                        }
                        using(FileStream fs = File.OpenRead(fileName)) {
                            try {
                                DekiContext.Current.Instance.Storage.PutFile(currentFile, SizeType.ORIGINAL, new StreamInfo(fs, fs.Length));
                            } catch { }
                        }
                        previous = currentFile;

                    } catch(Exception e) {
                        Console.Out.WriteLine("Error converting " + file.Name + ":");
                        Console.Out.WriteLine(e.GetCoroutineStackTrace());
                    }
                }
            }
            Console.Out.WriteLine("Done!");
        }

        public void ConvertPageData(Title[] titles, Dictionary<Title, List<PageBE>> oldTitleToPageMap) {
            Console.Out.Write("Migrating page data... ");

            // update the page links, usecache, and summary info
            foreach(Title title in titles) {
                PageBE page = GetPredominantPage(oldTitleToPageMap[title]);
                ulong thisoldid;
                if(_MWToDWPageIDMap.TryGetValue(page.Language + page.ID, out thisoldid)) {
                    page.ID = thisoldid;
                    ParserResult parserResult = DekiXmlParser.Parse(page, ParserMode.SAVE, -1, false);
                    page.UseCache = !parserResult.HasScriptContent;
                    page.TIP = parserResult.Summary;
                    page.ContentType = parserResult.ContentType;
                    if(!page.IsRedirect) {
                        page.SetText(parserResult.BodyText);
                    }
                    PageBL.UpdateLinks(page, parserResult.Links.ToArray());
                    MediaWikiDA.UpdateDWPageData(page);
                }
            }

            Console.Out.WriteLine("Done!");
        }

        /// <summary>
        /// Convert from MediaWiki categories to MindTouch tags
        /// </summary>
        public void ConvertCategories() {
            Console.Out.Write("Migrating categories... ");
            Dictionary<string, List<string>> pageToCategoryMap = MediaWikiDA.GetCategoryNamesByPage();
            var tagBL = new TagBL();
            foreach(KeyValuePair<string, List<string>> categoriesByPage in pageToCategoryMap) {
                TagBE[] tags = new TagBE[categoriesByPage.Value.Count];
                for(int i = 0; i < tags.Length; i++) {
                    TagBE categoryTag = new TagBE();
                    categoryTag.Type = TagType.TEXT;
                    categoryTag.Name = categoriesByPage.Value[i];
                    tags[i] = categoryTag;
                }
                ulong pageID;
                if(_MWToDWPageIDMap.TryGetValue(categoriesByPage.Key, out pageID)) {
                    tagBL.InsertTags(pageID, tags);
                }
            }
            Console.Out.WriteLine("Done!");
        }

        /// <summary>
        /// Convert MediaWiki page revisions to MindTouch revisions
        /// </summary>
        public void ConvertRevisions() {
            Console.Out.Write("Migrating revisions...  ");

            if(!MediaWikiConverterContext.Current.Merge) {
                MediaWikiDA.DeleteDWRevisions();
            }

            Dictionary<Site, List<PageBE>> revisionsBySite = MediaWikiDA.GetRevisionsBySite();
            List<PageBE> revisions = new List<PageBE>();
            foreach(Site site in revisionsBySite.Keys) {
                foreach(PageBE revision in revisionsBySite[site]) {
                    revisions.Add(revision);
                }
            }

            // sort the revisions to ensure they are inserted chronologically 
            revisions.Sort(delegate(PageBE left, PageBE right) { return DateTime.Compare(left.TimeStamp, right.TimeStamp); });
            foreach(PageBE revision in revisions) {
                Site site = MediaWikiConverterContext.Current.GetMWSite(revision.Language);
                PageBE populatedRevision = MediaWikiDA.GetPopulatedRevision(site, revision);

                // convert from MediaWiki to MindTouch content
                MWToDWContent(site, populatedRevision, true);
                populatedRevision.Title = MWToDWTitle(site, populatedRevision.Title);

                OldBE old = PageBL.InsertOld(populatedRevision, 0);
                _MWToDWOldIDMap.Add(populatedRevision.Language + populatedRevision.ID, old.ID);
            }
            Console.Out.WriteLine("Done!");
        }

        /// <summary>
        /// Convert from MediaWiki recent changes to MindTouch recent changes
        /// </summary>
        public void ConvertRecentChanges() {
            Console.Out.Write("Migrating recent changes...  ");

            if(!MediaWikiConverterContext.Current.Merge) {
                MediaWikiDA.DeleteDWRecentChanges();
            }

            Dictionary<Site, List<RecentChangeBE>> recentChangesBySite = MediaWikiDA.GetRecentChangesBySite();
            foreach(Site site in recentChangesBySite.Keys) {
                foreach(RecentChangeBE recentChange in recentChangesBySite[site]) {
                    ulong id;
                    if(_MWToDWPageIDMap.TryGetValue(site.Language + recentChange.Page.ID, out id)) {
                        recentChange.Page.ID = id;
                        ulong thisoldid, lastoldid;
                        _MWToDWOldIDMap.TryGetValue(site.Language + recentChange.ThisOldID, out thisoldid);
                        _MWToDWOldIDMap.TryGetValue(site.Language + recentChange.LastOldID, out lastoldid);
                        recentChange.ThisOldID = thisoldid;
                        recentChange.LastOldID = lastoldid;
                        recentChange.Page.Title = MWToDWTitle(site, recentChange.Page.Title);
                        MediaWikiDA.InsertDWRecentChange(recentChange);
                    }
                }
            }
            Console.Out.WriteLine("Done!");
        }

        public void ConvertWatchlist() {
            Console.Out.Write("Migrating user watchlist...  ");

            if(!MediaWikiConverterContext.Current.Merge) {
                MediaWikiDA.DeleteDWWatch();

                Dictionary<Site, List<WatchlistBE>> watchlistBySite = MediaWikiDA.GetWatchlistBySite();
                foreach(Site site in watchlistBySite.Keys) {
                    foreach(WatchlistBE watch in watchlistBySite[site]) {
                        watch.Title = MWToDWTitle(site, watch.Title);
                        MediaWikiDA.InsertDWWatch(watch);
                    }
                }
            }
            Console.Out.WriteLine("Done!");
        }

        public void Convert() {
            try {
                #region logging
                if(MediaWikiConverterContext.Current.LoggingEnabled) {
                    log.Start("head").Elem("title", "MediaWiki to MindTouch Converter Output").End();
                    log.Start("body").Elem("h1", "MediaWiki to MindTouch Converter Output");

                    log.Start("h2").Value("Renamed Users").Elem("br").End();
                    log.Elem("p", "These users were issued a new name since their previous name differed only by case with another user.");
                    log.Start("table").Attr("border", 1).Attr("id", "renamedUsers").Start("tr").Start("td").Elem("strong", "Previous Name").End().Start("td").Elem("strong", "New Name").End().End().End();

                    log.Start("h2").Value("Interwiki Conflicts").Elem("br").End();
                    log.Elem("p", "These interwiki settings differed between languages.");
                    log.Start("table").Attr("border", 1).Attr("id", "interWikiConflicts").Start("tr").Start("td").Elem("strong", "Interwiki Prefix").End().Start("td").Elem("strong", "Interwiki Link").End().Start("td").Elem("strong", "Conflict").End().End().End();

                    log.Start("h2").Value("Page Content Merged").Elem("br").End();
                    log.Elem("p", "These pages previously existed in multiple languages.  Since MindTouch provides single user/template pages across all languages, the content from each language has been merged.");
                    log.Start("table").Attr("border", 1).Attr("id", "pagesMerged").Start("tr").Start("td").Elem("strong", "Page Title").End().Start("td").Elem("strong", "Languages Merged").End().End().End();

                    log.Start("h2").Value("Removed Pages").Elem("br").End();
                    log.Elem("p", "These pages were removed since they differed only by case with an existing page.");
                    log.Start("table").Attr("border", 1).Attr("id", "removedPages").Start("tr").Start("td").Elem("strong", "Removed Page").End().End().End();

                    log.Start("h2").Value("Pages containing links to Special pages that no longer exist").Elem("br").End();
                    log.Elem("p", "These pages link to one or more Special pages that no longer exist.");
                    log.Start("table").Attr("border", 1).Attr("id", "specialpages").Start("tr").Start("td").Elem("strong", "Page Title").End().Start("td").Elem("strong", "Special Page Link").End().End().End();

                    log.Start("h2").Value("Pages containing functions").Elem("br").End();
                    log.Elem("p", "These pages reference one or more parser functions and should be individually reviewed.");
                    log.Start("table").Attr("border", 1).Attr("id", "functions").Start("tr").Start("td").Elem("strong", "Page Title").End().Start("td").Elem("strong", "Function Reference").End().End().End();

                    log.Start("h2").Value("Pages containing templates").Elem("br").End();
                    log.Elem("p", "These pages reference one or more templates and should be individually reviewed.");
                    log.Start("table").Attr("border", 1).Attr("id", "templates").Start("tr").Start("td").Elem("strong", "Page Title").End().Start("td").Elem("strong", "Template Reference").End().End().End();

                    log.Start("h2").Value("Customized templates that are outdated").Elem("br").End();
                    log.Elem("p", "These templates have changed since the last conversion.");
                    log.Start("table").Attr("border", 1).Attr("id", "outdatedtemplates").Start("tr").Start("td").Elem("strong", "Template Title").End().End().End();
                }
                #endregion

                Title[] titles;
                Dictionary<Title, List<PageBE>> oldTitleToPageMap;

                ConvertUsers();
                ConvertIPBlocks();
                ConvertConfiguration();
                ConvertPages(out titles, out oldTitleToPageMap);
                ConvertFiles();
                ConvertPageData(titles, oldTitleToPageMap);
                ConvertCategories();
                ConvertRevisions();
                ConvertRecentChanges();
                ConvertWatchlist();

                #region logging
                if(MediaWikiConverterContext.Current.LoggingEnabled) {
                    log.End();
                    File.WriteAllText(MediaWikiConverterContext.Current.LogPath, log.Contents, Encoding.UTF8);
                    try { System.Diagnostics.Process.Start(MediaWikiConverterContext.Current.LogPath); } catch { }
                }
                #endregion

                Console.Out.WriteLine("Migration completed.  Press enter to continue");
            } catch(Exception e) {
                Console.Out.WriteLine("An unexpected exception has occurred:");
                Console.Out.WriteLine(e.GetCoroutineStackTrace());
            }
        }

        [DreamFeature("POST:", "")]
        public Yield PostConvert(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            Convert();
            response.Return(DreamMessage.Ok());
            yield break;
        }

    }
}

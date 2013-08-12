using System;
using System.Collections.Generic;
using System.Text;

using log4net;
using MindTouch.Xml;
using MindTouch.Dream;
using MindTouch.Tools.ConfluenceConverter.Confluence;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.RegularExpressions;

namespace MindTouch.Tools.ConfluenceConverter
{
    public partial class ACConverter
    {

        public void LogPageConversion(XDoc spaceManifest, ACConverterPageInfo pageInfo) {
            string xpath = string.Format("page[@c.pageid='{0}']", pageInfo.ConfluencePage.id);
            XDoc pageXml = spaceManifest[xpath];

            if(pageXml.IsEmpty) {
                spaceManifest.Start("page");
                spaceManifest.Attr("c.space", pageInfo.ConfluencePage.space);
                spaceManifest.Attr("c.pageid", pageInfo.ConfluencePage.id);
                spaceManifest.Attr("c.parentpageid", pageInfo.ConfluencePage.parentId);
                spaceManifest.Attr("c.path", Utils.GetUrlLocalUri(_confBaseUrl, pageInfo.ConfluencePage.url, true, false));
                spaceManifest.Attr("c.tinyurl", pageInfo.TinyUrl);
                spaceManifest.Attr("mt.pageid", pageInfo.DekiPageId);
                spaceManifest.Attr("mt.path", pageInfo.DekiPageUrl);
                spaceManifest.Attr("title", pageInfo.PageTitle);
                spaceManifest.End();
            }
        }

        public void LogSpaceConversion(XDoc spaceManifest, string space, string confUrl, string mtPath) {
            string xpath = string.Format("space[@space='{0}']", space);
            XDoc spaceXml = spaceManifest[xpath];
            
            if(spaceXml.IsEmpty) {
                spaceManifest.Start("url");
                spaceManifest.Attr("space", space);
                spaceManifest.Attr("c.path", Utils.GetUrlLocalUri(_confBaseUrl, confUrl, false, false));
                spaceManifest.Attr("mt.path", mtPath);
                spaceManifest.End();
            }
        }

        public void LogFileConversion(XDoc spaceManifest, RemoteAttachment fileInfo, string contentUrl) {
            string xpath = string.Format("file[@c.fileid='{0}']", fileInfo.id);

            XDoc fileXml = spaceManifest[xpath];
            if(fileXml.IsEmpty) {
                spaceManifest.Start("file");
                spaceManifest.Attr("c.fileid", fileInfo.id);
                spaceManifest.Attr("c.pageid", fileInfo.pageId);
                spaceManifest.Attr("c.filename", fileInfo.fileName);
                spaceManifest.Attr("c.filesize", fileInfo.fileSize);
                spaceManifest.Attr("c.mimetype", fileInfo.contentType);
                spaceManifest.Attr("c.path", Utils.GetUrlLocalUri(_confBaseUrl, fileInfo.url, false, true));
                spaceManifest.Attr("mt.path", Utils.GetApiUrl(Utils.GetUrlLocalUri(_confBaseUrl, contentUrl, true, true)));
                spaceManifest.End();
            }
        }

        public void LogCommentConversion(XDoc spaceManifest, string space, RemoteComment comment, string mtPath) {
            string xpath = string.Format("comment[@c.commentid='{0}']", comment.id);
            XDoc commentXml = spaceManifest[xpath];

            if(commentXml.IsEmpty) {
                spaceManifest.Start("comment");
                spaceManifest.Attr("c.space", space);
                spaceManifest.Attr("c.commentid", comment.id);
                spaceManifest.Attr("c.pageid", comment.pageId);
                spaceManifest.Attr("c.path", Utils.GetUrlLocalUri(_confBaseUrl, comment.url, true, false));
                spaceManifest.Attr("mt.path", mtPath);
                spaceManifest.End();
            }
        }

        public void LogUserConversion(XDoc spaceManifest, string confUserName, string mtUserId, string mtUserName, string confUserUrl) {
            string xpath = string.Format("user[@c.username='{0}']", confUserName);
            XDoc userXml = spaceManifest[xpath];

            if(userXml.IsEmpty) {
                spaceManifest.Start("user");
                spaceManifest.Attr("c.username", confUserName);
                spaceManifest.Attr("mt.userid", mtUserId);
                spaceManifest.Attr("mt.username", mtUserName);
                spaceManifest.Attr("c.path", Utils.GetUrlLocalUri(_confBaseUrl, confUserUrl, false, true));
                spaceManifest.Attr("mt.path", Utils.GetDekiUserPageByUserName(mtUserName));
                spaceManifest.End();
            }
        }

        public Dictionary<string, string> ReadUrlsFromManifest(Dictionary<string, string> confToMtUrls, XDoc spaceManifest) {
            if(confToMtUrls == null) {
                confToMtUrls = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
            }
            string confUrl = null;
            string confId = null;
            string mtUrl = null;
            foreach(XDoc pageDoc in spaceManifest["page"]) {
                confUrl = pageDoc["@c.path"].AsText ?? string.Empty;
                mtUrl = pageDoc["@mt.path"].AsText ?? string.Empty;
                confToMtUrls[confUrl] = mtUrl;
                confId = pageDoc["@c.pageid"].AsText ?? string.Empty;                
                confToMtUrls[confId] = mtUrl;
            }

            foreach(XDoc fileDoc in spaceManifest["file"]) {
                confUrl = fileDoc["@c.path"].AsText ?? string.Empty;
                mtUrl = fileDoc["@mt.path"].AsText ?? string.Empty;
                confToMtUrls[confUrl] = mtUrl;

                // Add thumbnails to images
                MimeType mime = null;
                if(MimeType.TryParse(fileDoc["@c.mimetype"].AsText ?? string.Empty, out mime)) {
                    if(mime.MainType.EqualsInvariantIgnoreCase("image") && confUrl.StartsWithInvariantIgnoreCase("/download/attachments/")) {
                        confUrl = confUrl.Replace("/download/attachments/", "/download/thumbnails/");
                        mtUrl += "?size=thumb";
                        confToMtUrls[confUrl] = mtUrl;
                    }
                }
            }

            foreach(XDoc commentDoc in spaceManifest["comment"]) {
                confUrl = commentDoc["@c.path"].AsText ?? string.Empty;
                mtUrl = commentDoc["@mt.path"].AsText ?? string.Empty;
                confToMtUrls[confUrl] = mtUrl;
            }

            foreach(XDoc userDoc in spaceManifest["user"]) {
                confUrl = userDoc["@c.path"].AsText ?? string.Empty;
                mtUrl = userDoc["@mt.path"].AsText ?? string.Empty;
                confToMtUrls[confUrl] = mtUrl;


            }

            //TODO (maxm): refactor this to only loop once rather than for each resource type

            return confToMtUrls;
        }

        public Dictionary<string, string> ReadUrlsFromAllManifests() {

            Dictionary<string, string> confToMtUrls = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
            if(Directory.Exists("data")) {
                string[] manifestFiles = Directory.GetFiles("data", "*.manifest.xml", SearchOption.TopDirectoryOnly);
                foreach(string filename in manifestFiles) {
                    XDoc spaceManifest = XDocFactory.LoadFrom(filename, MimeType.XML);
                    confToMtUrls = ReadUrlsFromManifest(confToMtUrls, spaceManifest);
                }
            }

            return confToMtUrls;
        }

        public XDoc GetManifestFromDisk(string space) {
            XDoc ret = null;
            if(string.IsNullOrEmpty(space)) {
                space = "_global";
            }
            string filename = string.Format(@"data\{0}.manifest.xml", space.ToLowerInvariant());
            if(File.Exists(filename)) {
                ret = XDocFactory.LoadFrom(filename, MimeType.XML);
            }

            return ret;
        }

        public void PersistManifestToDisk(string space, XDoc spaceManifest) {
            if(string.IsNullOrEmpty(space)) {
                space = "_global";
            }

            if(!Directory.Exists("data")) {
                Directory.CreateDirectory("data");
            }

            string filename = string.Format(@"data\{0}.manifest.xml", space.ToLowerInvariant());
            spaceManifest.Save(filename);
        }

        public string GetMtPathFromConfluencePath(Dictionary<string, string> pathMap, string confluencePath)
        {
            string temp = null;


            // only match if the page is on the current instance of confluence
            if(!string.IsNullOrEmpty(ACConverter.ConfluenceBaseURL)) {
                if(confluencePath.StartsWithInvariantIgnoreCase(ACConverter.ConfluenceBaseURL)) {
                    confluencePath = confluencePath.Substring(ACConverter.ConfluenceBaseURL.Length);
                } else {
                    XUri confBaseXUri = null;
                    if(XUri.TryParse(ACConverter.ConfluenceBaseURL, out confBaseXUri)) {
                        if(confluencePath.StartsWithInvariantIgnoreCase(confBaseXUri.Path)){
                            confluencePath = confluencePath.Substring(confBaseXUri.Path.Length);
                        }
                    }
                }
            }

            Regex idPathRegex = new Regex(@".*\.action\?pageId=([0-9]*)");
            Match match = idPathRegex.Match(confluencePath);
            if (match.Success)
            {
                // The confluence path uses a pageid, rather than a friendly url.
                // replace the url with the pageid so that the lookup works correctly
                confluencePath = match.Groups[1].Value;
            }

            if(pathMap.TryGetValue(confluencePath, out temp))
            {
                return temp;
            }
            return null;
        }   
  
        public string GetMtPathFromConfluencePath(Dictionary<string, string> pathMap, string confSpaceKey,string confPageTitle)
        {
            string temp = null;            

            string spaceKeyMatch = "/" + confSpaceKey + "/";
            string pageTitleMatch = "/" + System.Web.HttpUtility.UrlEncode(confPageTitle);            

            List<string> list = new List<string>(pathMap.Keys);            
            string key = list.Find(delegate(string sKey)
            {
                if((sKey.ContainsInvariantIgnoreCase(spaceKeyMatch)) && (sKey.ContainsInvariantIgnoreCase(pageTitleMatch)))
                    return true;
                else
                    return false;
                
            });

            if (!String.IsNullOrEmpty(key))
            {
                if (pathMap.TryGetValue(key, out temp))
                {
                    //Convert URI to path
                    temp = Utils.ConvertPageUriToPath(temp);
                    return temp;

                }
            }
            return null;
        }           


        private void SavePageViewPermissions(ACConverterPageInfo pageInfo)
        {
            RemotePermission[] pagePermissions = _confluenceService.GetPagePermissions(pageInfo.ConfluencePage.id);

            //If there no view restrictions on the page, copy parent page restrictions.
            if (Array.Find(pagePermissions,
                delegate(RemotePermission p) { return (p.lockType == ConfluenceViewPermissionName); }) == null)
            {
                if (pageInfo.ParentPage != null)
                {
                    foreach (RemotePermission permission in pageInfo.ParentPage.ConfluenceUsersWithViewPermissions.Values)
                    {
                        pageInfo.ConfluenceUsersWithViewPermissions[permission.lockedBy.ToLower()] = permission;
                    }
                }
                return;
            }

            foreach (RemotePermission permission in pagePermissions)
            {
                if (permission.lockType == ConfluenceViewPermissionName)
                {
                    if ((pageInfo.ParentPage != null) && (pageInfo.ParentPage.ConfluenceUsersWithViewPermissions.Count != 0))
                    {
                        if (!pageInfo.ParentPage.ConfluenceUsersWithViewPermissions.ContainsKey(permission.lockedBy.ToLower()))
                        {
                            //If this user haven't permission to view parent page, then he haven't permission to view this page in Confluence
                            continue;
                        }
                    }
                    pageInfo.ConfluenceUsersWithViewPermissions[permission.lockedBy.ToLower()] = permission;
                }
            }
        }

    }
}

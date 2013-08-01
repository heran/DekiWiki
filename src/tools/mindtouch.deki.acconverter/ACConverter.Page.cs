using System;
using System.Collections.Generic;
using System.Text;

using log4net;
using MindTouch.Xml;
using MindTouch.Dream;
using MindTouch.Tools.ConfluenceConverter.Confluence;
using System.IO;
using MindTouch.Tools.ConfluenceConverter.XMLRPC;
using MindTouch.Tools.ConfluenceConverter.XMLRPC.Types;

namespace MindTouch.Tools.ConfluenceConverter
{
    public partial class ACConverter
    {
        #region Pass 1
        private string MovePageStubs(XDoc spaceManifest, string spaceRoot, string parentPagePath, RemotePageSummary confluenceRemotePageSummary,
           string pageName, string pageTitle, ACConverterPageInfo parentPage)
        {
            RemotePage confluenceRemotePage = _confluenceService.GetPage(confluenceRemotePageSummary.id);
            string tinyUrl = null;

            if(_rpcclient != null) {
                CFRpcExtensions rpcExt = new CFRpcExtensions(_rpcclient);
                tinyUrl = rpcExt.GetTinyUrlForPageId(confluenceRemotePageSummary.id.ToString());
            }

            string pagePath = null;

            //If PageName not null use it, else use Confluence page title.
            if (pageName == null)
            {
                pageName = confluenceRemotePage.title;
            }

            pagePath = pageName;
            if (parentPagePath != null)
            {
                pagePath = parentPagePath + Utils.DoubleUrlEncode("/" + pagePath);
            }

            //If the page title is too long it gets saved as {spaceroot}/misc/{pageid} with the page title set
            if(pagePath.Length > MaxLengthOfPageTitle) {
                pagePath = Utils.DoubleUrlEncode(string.Format(@"{0}/misc/{1}", spaceRoot, confluenceRemotePage.id));
                if(string.IsNullOrEmpty(pageTitle)) {
                    pageTitle = pageName;
                }
                Log.WarnFormat("Page title longer than {0}. Will be placed into {1}. Title: {2}", MaxLengthOfPageTitle, pagePath, pageTitle);
            }

            Plug postPageDekiPlug = (confluenceRemotePage.creator == null) ? _dekiPlug :
                GetPlugForConvertedUser(confluenceRemotePage.creator);

            Log.TraceFormat("Creating page stub in space '{0}'", confluenceRemotePage.space);

            string mtPageUrl;

            // modified date string, according to:
            /* http://developer.mindtouch.com/en/ref/MindTouch_API/POST%3apages%2f%2f%7Bpageid%7D%2f%2fcontents
             * should be formatted as:
             * the edit timestamp - yyyyMMddHHmmss or yyyy-MM-ddTHH:mm:ssZ
             */

            int dekiPageId = CreateDekiPage(postPageDekiPlug, pagePath, pageTitle, confluenceRemotePage.modified, "",
                out mtPageUrl);

            if (dekiPageId == -1)
            {

                //TODO (maxm): page failure needs to be recorded and steps that depend 
                // on it such as attachments and children should be skipped
                return string.Empty;
            }
            ACConverterPageInfo pageInfo = new ACConverterPageInfo(confluenceRemotePage, spaceRoot, mtPageUrl, pagePath, dekiPageId, pageTitle, tinyUrl, parentPage);

            Utils.PersistPageInfo(pageInfo);

            //Confluence view page permission is inherited. Save they to use later in MovePermissions.
            SavePageViewPermissions(pageInfo);

            RemotePageSummary[] childPages = _confluenceService.GetChildren(confluenceRemotePage.id);
            foreach (RemotePageSummary childPageSummary in childPages)
            {
                MovePageStubs(spaceManifest, spaceRoot, pagePath, childPageSummary, null, null, pageInfo);
            }

            SaveCommentsLinks(spaceManifest, pageInfo.ConfluencePage.space, pageInfo.ConfluencePage.id, mtPageUrl);

            MoveAttachments(spaceManifest, pageInfo.DekiPageId,pageInfo.ConfluencePage.id);

            LogPageConversion(spaceManifest, pageInfo);

            return pagePath;
        }

        private void MovePageStubs()
        {
            string rootPageName = string.Empty;
            //string rootPageName = CreateRootPageForExport();

            Log.Info("Retrieving all space information...");
            List<RemoteSpaceSummary> confluenceSpaces = new List<RemoteSpaceSummary>(_confluenceService.GetSpaces());

            //Sort spaces by keyname to help show progress of conversion
            confluenceSpaces.Sort(delegate(RemoteSpaceSummary s1, RemoteSpaceSummary s2) { return string.Compare(s1.key, s2.key); });

            foreach(RemoteSpaceSummary space in confluenceSpaces) {

                if(!_processPesonalSpaces && space.type == ConfluencePersonalSpaceTypeName) {
                    continue;
                }

                //NOTE (maxm): not sure why personal spaces are being processed even if
                // the _spacesToConvert is defined from configuration. Commented it out.
                if((_spacesToConvert.Count > 0)
                    /*&& (space.type != ConfluencePersonalSpaceTypeName)*/ &&
                    (!_spacesToConvert.Contains(space.key.ToLower()))) {
                    continue;
                }

                Log.DebugFormat("Retrieving top pages for {0} space '{1}' ({2})", space.type, space.key, space.name);
                RemotePageSummary[] confluenceRemoteTopPages = _confluenceService.GetTopLevelPages(space.key);

                if(confluenceRemoteTopPages.Length == 0) {
                    Log.WarnFormat("space '{0}' has 0 top level pages!", confluenceRemoteTopPages.Length);
                }

                //Get teamlabel for the space to be utilized in location management                
                string spaceTeamLabel = "";
                if(_rpcclient != null) {
                    Log.DebugFormat("Retrieving team label for space '{0}'", space.key);
                    spaceTeamLabel = GetValidTeamLabel(space.key);
                }

                //Restore manifest from disk
                XDoc spaceManifest = GetManifestFromDisk(space.key) ?? new XDoc("manifest");

                //Compute the location for the root page of the space
                string spaceRootPath = ComputeSpaceRootPath(rootPageName, space, spaceTeamLabel);

                try {

                    //Create page for the space root
                    CreateDekiPage(_dekiPlug, spaceRootPath, space.name, DateTime.Now, DefaultSpaceContents);

                    //Pages within the root are processed recursively starting with the root pages.
                    foreach(RemotePageSummary remotePageSummary in confluenceRemoteTopPages) {
                        MovePageStubs(spaceManifest, spaceRootPath, spaceRootPath, remotePageSummary, Utils.DoubleUrlEncode(remotePageSummary.title),
                            remotePageSummary.title, null);
                    }
                    
                    //Log root page of space
                    LogSpaceConversion(spaceManifest, space.key, space.url, XUri.Decode(spaceRootPath));

                    MoveNewsPagesWithoutContent(spaceManifest, space.key, spaceRootPath);
                } catch(Exception x) {
                    Log.Error("Error during stub creation", x);
                } finally {

                    //Save the space manifest to disk
                    PersistManifestToDisk(space.key, spaceManifest);
                }
            }
        }
        #endregion Pass 1

        #region Pass 2

        private void MovePageContent(Dictionary<string, string> pathMap) {

            foreach(string space in Utils.GetPersistedSpaces()) {

                if((_spacesToConvert.Count > 0) && (!_spacesToConvert.Contains(space.ToLower()))) {
                    continue;
                }

                RemoteSpace spaceInfo = _confluenceService.GetSpace(space);


                foreach(string p in Utils.GetPersistedPagesInSpace(space)) {
                    ACConverterPageInfo pageInfo = Utils.RestorePageInfo(space, p);
                    if(pageInfo != null) {

                        MovePageContent(pathMap, pageInfo);

                        if(pageInfo.ConfluencePage.id == spaceInfo.homePage) {

                            //Set content of the MT space root page after the homepage is processed.
                            string content = "{{" + string.Format("wiki.page(wiki.getpage({0}).path);", pageInfo.DekiPageId) + "}}";
                            CreateDekiPage(_dekiPlug, pageInfo.SpaceRootPath, spaceInfo.name, null, content);

                        }
                    }
                }
            }
        }

        private void MovePageContent(Dictionary<string, string> pathMap, ACConverterPageInfo pageInfo) {

            try {

                //Move the history of the page first
                MovePageHistory(pathMap, pageInfo);

                string stubContent = ReplaceMacrosWithStubs(pageInfo);

                // Move the latest revision
                string confluencePageContent = _confluenceService.RenderContent(pageInfo.ConfluencePage.space, pageInfo.ConfluencePage.id, stubContent);
                confluencePageContent = ConvertStubstoDeki(pathMap,confluencePageContent, pageInfo);

                confluencePageContent = ExtractPageContentAndReplaceLinks(pathMap, confluencePageContent);

                Plug postPageDekiPlug = _dekiPlug;
                if(pageInfo.ConfluenceUsersWithViewPermissions.Count > 0) {
                    if((pageInfo.ConfluencePage.modifier != null) && (pageInfo.ConfluenceUsersWithViewPermissions.ContainsKey(pageInfo.ConfluencePage.modifier.ToLower()))) {
                        postPageDekiPlug = GetPlugForConvertedUser(pageInfo.ConfluencePage.modifier);
                    } else {
                        if((pageInfo.ConfluencePage.creator != null) && (pageInfo.ConfluenceUsersWithViewPermissions.ContainsKey(pageInfo.ConfluencePage.creator.ToLower()))) {
                            postPageDekiPlug = GetPlugForConvertedUser(pageInfo.ConfluencePage.creator);
                        }
                    }
                } else {
                    if(pageInfo.ConfluencePage.modifier != null) {
                        postPageDekiPlug = GetPlugForConvertedUser(pageInfo.ConfluencePage.modifier);
                    } else {
                        if(pageInfo.ConfluencePage.creator != null) {
                            postPageDekiPlug = GetPlugForConvertedUser(pageInfo.ConfluencePage.creator);
                        }
                    }
                }

                CreateDekiPage(postPageDekiPlug, pageInfo.DekiPagePath, pageInfo.PageTitle,
                    pageInfo.ConfluencePage.modified, confluencePageContent);

                // Catch all exceptions when moving labels, comments and permissions.
                try {
                    MoveLabels(pageInfo.DekiPageId, pageInfo.ConfluencePage.id);
                } catch(Exception e) {
                    Log.WarnExceptionMethodCall(e, "MovePageContent", "Error occurred in MoveLabels");
                }

                try {
                    MoveComments(pageInfo.DekiPageId, pageInfo.ConfluencePage.id);
                } catch(Exception e) {
                    Log.WarnExceptionMethodCall(e, "MovePageContent", "Error occurred in MoveComments");
                }

                try {
                    MovePermissions(pageInfo);
                } catch(Exception e) {
                    Log.WarnExceptionMethodCall(e, "MovePageContent", "Error occurred in MovePermissions");
                }


            } catch(Exception e) {
                Log.WarnExceptionMethodCall(e, "MovePageContent", "Error occurred in MovePageContent");       
            }
        }
        
        #endregion Pass 2

    }
}

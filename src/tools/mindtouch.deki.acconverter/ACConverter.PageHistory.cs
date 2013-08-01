using System;
using System.Collections.Generic;
using System.Text;

using log4net;
using MindTouch.Xml;
using MindTouch.Dream;
using MindTouch.Tools.ConfluenceConverter.Confluence;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace MindTouch.Tools.ConfluenceConverter
{
    public partial class ACConverter
    {
        private int CreateDekiPageHistory(Plug p, string pagePath, string pageTitle, DateTime? modified, string content,
           out string dekiPageUrl)
        {

            Log.DebugFormat("Creating page history: '{0}' Content? {1}", XUri.DoubleDecode(pagePath), !string.IsNullOrEmpty(content));

            Plug pagePlug = p.At("pages", "=" + pagePath, "revision");
            pagePlug = pagePlug.With("abort", "never");
            modified = modified ?? DateTime.Now;
            string editTime = Utils.FormatPageDate(modified.Value.ToUniversalTime());
            pagePlug = pagePlug.With("edittime", editTime);
            if (pageTitle != null)
            {
                pagePlug = pagePlug.With("title", pageTitle);
            }
            DreamMessage msg = DreamMessage.Ok(MimeType.TEXT_UTF8, content);

            DreamMessage res = pagePlug.PostAsync(msg).Wait();
            if (res.Status != DreamStatus.Ok)
            {
                WriteLineToConsole("Error converting page \"" + XUri.DoubleDecode(pagePath) + "\"");
                WriteLineToLog("Edit time: " + editTime);
                WriteLineToLog("Page title: " + pageTitle);
                WriteErrorResponse(res);
                WriteErrorRequest(msg);
                Log.WarnFormat("Error converting page: '{0}'. Request: {0}    \nResponse: {1}", msg.ToString(), res.ToString());
            }
            else
            {
                XDoc createdPage = res.AsDocument();
                int pageId = createdPage["page/@id"].AsInt.Value;
                dekiPageUrl = createdPage["page/path"].AsText;

                return pageId;
            }
            dekiPageUrl = null;
            return -1;
        }


        private List<RemotePage> GetHistoryPages(long pageId)
        {
            List<RemotePage> remotePages = new List<RemotePage>();

            RemotePageHistory[] pageHistories = _confluenceService.GetPageHistory(pageId);

            int maxNumRevisions = pageHistories.Length < NumRevisionsToMove ? pageHistories.Length : NumRevisionsToMove;

            for (int i = 0; i < maxNumRevisions; i++)
            {
                remotePages.Add(_confluenceService.GetPage(pageHistories[i].id));
            }

            return remotePages;
        }

        private void MovePageHistory(Dictionary<string, string> pathMap, ACConverterPageInfo pageInfo)
        {
            List<RemotePage> pageHistories = GetHistoryPages(pageInfo.ConfluencePage.id);
            pageHistories.Reverse();
            foreach (RemotePage remoteHistoryPage in pageHistories)
            {
                Log.DebugFormat("Processing PageHistory for pageid: {0} pageHistoryid: {1}", pageInfo.DekiPageId, pageInfo.ConfluencePage.id);

                string confluencePageContent = _confluenceService.RenderContent(pageInfo.ConfluencePage.space,
                        remoteHistoryPage.id, remoteHistoryPage.content);

                confluencePageContent = ExtractPageContentAndReplaceLinks(pathMap, confluencePageContent);

                Plug postPageDekiPlug = _dekiPlug;

                CreateDekiPage(postPageDekiPlug, pageInfo.DekiPagePath, pageInfo.PageTitle, pageInfo.ConfluencePage.modified, confluencePageContent);
            }
        }       
    }
}

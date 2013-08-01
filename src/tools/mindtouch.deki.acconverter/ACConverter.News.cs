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
        private string CreatePageForNewsOnDate(DateTime? Date, string spaceNewsPagePath)
        {
            if (!Date.HasValue)
            {
                string undatedNewsPagePath = spaceNewsPagePath + Utils.DoubleUrlEncode("/" + UndatedNewsPageTitle);
                if (IsDekiPageExists(undatedNewsPagePath))
                {
                    return undatedNewsPagePath;
                }
                CreateDekiPage(_dekiPlug, undatedNewsPagePath, UndatedNewsPageTitle, null, "");
                return undatedNewsPagePath;
            }
            string yearPageName = Date.Value.Year.ToString();
            string yearPagePath = spaceNewsPagePath + Utils.DoubleUrlEncode("/" + yearPageName);
            if (!IsDekiPageExists(yearPagePath))
            {
                CreateDekiPage(_dekiPlug, yearPagePath, yearPageName, null, "");
            }
            string monthPageName = Date.Value.ToString("MM");
            string monthPagePath = yearPagePath + Utils.DoubleUrlEncode("/" + monthPageName);
            if (!IsDekiPageExists(monthPagePath))
            {
                CreateDekiPage(_dekiPlug, monthPagePath, monthPageName, null, "");
            }
            string datePageName = Date.Value.ToString("dd");
            string datePagePath = monthPagePath + Utils.DoubleUrlEncode("/" + datePageName);
            if (!IsDekiPageExists(datePagePath))
            {
                CreateDekiPage(_dekiPlug, datePagePath, datePageName, Date, "");
            }
            return datePagePath;
        }

        private void MoveNewsPagesWithoutContent(XDoc spaceManifest, string spaceKey, string dekiSpacePath)
        {

            if (!_processNewsPages)
            {
                return;
            }

            RemoteBlogEntrySummary[] remoteBlogEntrySummaries = _confluenceService.GetBlogEntries(spaceKey);

            if (remoteBlogEntrySummaries.Length == 0)
            {
                return;
            }

            //Create root page for news
            string newsPageName = GetAllowedDekiPageName(dekiSpacePath, NewsPageTitle);
            string newsPagePath = dekiSpacePath + Utils.DoubleUrlEncode("/" + newsPageName);

            CreateDekiPage(_dekiPlug, newsPagePath, NewsPageTitle, DateTime.Now, "");

            foreach (RemoteBlogEntrySummary remoteBlogEntrySummary in remoteBlogEntrySummaries)
            {
                string datePageNews = CreatePageForNewsOnDate(remoteBlogEntrySummary.publishDate, newsPagePath);

                string dekiNewsPath = datePageNews + Utils.DoubleUrlEncode("/" + remoteBlogEntrySummary.title);

                Plug p = (remoteBlogEntrySummary.author == null) ? _dekiPlug :
                    GetPlugForConvertedUser(remoteBlogEntrySummary.author);

                string dekiNewsUrl;

                int dekiPageId = CreateDekiPage(p, dekiNewsPath, remoteBlogEntrySummary.title,
                    remoteBlogEntrySummary.publishDate, "", out dekiNewsUrl);

                MoveAttachments(spaceManifest, dekiPageId, remoteBlogEntrySummary.id);

                //string dekiNewsUrl = System.Web.HttpUtility.UrlDecode(dekiNewsPath);
                
                //TODO (maxm): this can be persisted as well.
                //SaveConfluenceUrlLocalPath(spaceUrlMap, remoteBlogEntrySummary.url, dekiNewsUrl);
                
                SaveCommentsLinks(spaceManifest, spaceKey, remoteBlogEntrySummary.id, dekiNewsUrl);

                ACConverterNewsInfo newsInfo = new ACConverterNewsInfo(remoteBlogEntrySummary, dekiNewsPath,
                    dekiPageId, remoteBlogEntrySummary.title);
                _convertedNews.Add(newsInfo);
            }
        }

        private void MoveNewsContent(Dictionary<string, string> pathMap)
        {
            if (!_processNewsPages)
            {
                return;
            }

            foreach (ACConverterNewsInfo news in _convertedNews)
            {
                string confluenceNewsContent = _confluenceService.RenderContent(news.ConfluenceNews.space,
                    news.ConfluenceNews.id, null);

                confluenceNewsContent = ExtractPageContentAndReplaceLinks(pathMap, confluenceNewsContent);

                Plug postNewsDekiPlug = (news.ConfluenceNews.author == null) ? _dekiPlug :
                    GetPlugForConvertedUser(news.ConfluenceNews.author);

                CreateDekiPage(postNewsDekiPlug, news.DekiPagePath, news.PageTitle, news.ConfluenceNews.publishDate,
                    confluenceNewsContent);

                MoveLabels(news.DekiPageId, news.ConfluenceNews.id);

                MoveComments(news.DekiPageId, news.ConfluenceNews.id);
            }
        }
    }
}

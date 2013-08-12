using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

using log4net;
using MindTouch.Xml;
using MindTouch.Dream;
using MindTouch.Tools.ConfluenceConverter.Confluence;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using MindTouch.Tools.ConfluenceConverter.XMLRPC;
using MindTouch.Tools.ConfluenceConverter.XMLRPC.Types;

namespace MindTouch.Tools.ConfluenceConverter
{
    public partial class ACConverter
    {

        public string GetValidTeamLabel(string spaceKey)
        {
            string spaceTeamLabel = String.Empty;
            try {
                CFRpcExtensions rpcExt = new CFRpcExtensions(_rpcclient);
                List<CFTeamLabels> teamLabels = rpcExt.GetSpaceTeamLables(spaceKey);
                foreach(CFTeamLabels teamLabel in teamLabels) {
                    if(teamLabel.Label.Contains(TeamLabelPrefix)) {
                        spaceTeamLabel = teamLabel.Label.Substring(TeamLabelPrefix.Length);
                        break;
                    }
                }

                //Apply the fallbackSpacePrefix if it's configured for spaces that don't have a prefix already setup
                if(string.IsNullOrEmpty(spaceTeamLabel) && !string.IsNullOrEmpty(_fallbackSpacePrefix)) {
                    spaceTeamLabel = _fallbackSpacePrefix;
                }

            } catch(Exception x) {
                Log.WarnExceptionFormat(x, "Could not retrieve space label for space '{0}'", spaceKey);
            }
            return spaceTeamLabel;
        }


        //TODO : this needs to be tested
        public string ReplaceMacrosWithStubs(ACConverterPageInfo pageinfo)
        {
            string rawContents = pageinfo.ConfluencePage.content;

            foreach (string macro in Converters.GetSupportedMacros())
            {
                Regex converter = new Regex(@"\{(" + macro + "[^}]*)}");
                rawContents = converter.Replace(rawContents, MacroStubStart + "$1" + MacroStubEnd);
            }

            return rawContents;
        }

        //TODO : Implementation pending
        public string ConvertStubstoDeki(Dictionary<string, string> pathMap,string stubbedPageContent, ACConverterPageInfo pageInfo)
        {
            return Converters.Convert(pathMap, stubbedPageContent, pageInfo);
        }

        private string ReplaceLinksFromString(Dictionary<string, string> pathMap, string fullPageContent)
        {
            //Extracting the conten div of the page
            int contentDivBegin = fullPageContent.IndexOf("<div");
            if (contentDivBegin < 0)
            {
                return fullPageContent;
            }
            contentDivBegin = fullPageContent.IndexOf(">", contentDivBegin) + 1;
            if (contentDivBegin < 0)
            {
                return fullPageContent;
            }
            int contentDivEnd = fullPageContent.LastIndexOf("</div>");
            if (contentDivEnd < 0)
            {
                return fullPageContent;
            }
            if (contentDivEnd < contentDivBegin)
            {
                return fullPageContent;
            }
            string pageContent = fullPageContent.Substring(contentDivBegin, contentDivEnd - contentDivBegin);

            //Replace src attributes of img tags            
            StringBuilder newContent = new StringBuilder();

            int oldPos = 0;

            Match imgMatch = _imgRegex.Match(pageContent);
            while (imgMatch.Success)
            {
                Group urlGroup = imgMatch.Groups["url"];

                string newUrl = GetMtPathFromConfluencePath(pathMap, urlGroup.Value);
                if (newUrl != null)
                {
                    newContent.Append(pageContent.Substring(oldPos, urlGroup.Index - oldPos));
                    newContent.Append(newUrl);
                    oldPos = urlGroup.Index + urlGroup.Length;
                }
                else
                {
                    newContent.Append(pageContent.Substring(oldPos, imgMatch.Index - oldPos));
                    oldPos = imgMatch.Index + imgMatch.Length;
                }
                imgMatch = imgMatch.NextMatch();
            }

            newContent.Append(pageContent.Substring(oldPos, pageContent.Length - oldPos));

            pageContent = newContent.ToString();

            //Replace href attributes of a tags            
            newContent = new StringBuilder();

            oldPos = 0;

            Match aMatch = _aRegex.Match(pageContent);
            while (aMatch.Success)
            {
                Group urlGroup = aMatch.Groups["url"];

                string newUrl = GetMtPathFromConfluencePath(pathMap, urlGroup.Value);
                if (newUrl != null)
                {
                    newContent.Append(pageContent.Substring(oldPos, urlGroup.Index - oldPos));
                    newContent.Append(newUrl);
                    oldPos = urlGroup.Index + urlGroup.Length;
                }
                aMatch = aMatch.NextMatch();
            }

            newContent.Append(pageContent.Substring(oldPos, pageContent.Length - oldPos));

            return newContent.ToString();
        }

        private string ExtractPageContentAndReplaceLinks(Dictionary<string, string> pathMap, string fullPageContent)
        {
            XDoc xFullContent = XDocFactory.From(fullPageContent, MimeType.HTML);

            if (xFullContent.IsEmpty)
            {
                return ReplaceLinksFromString(pathMap, fullPageContent);
            }

            XDoc divDoc = xFullContent["body/div"];

            ReplaceLinks(pathMap, divDoc);

            return divDoc.Contents;
        }

        private void ReplaceLinkAttribute(Dictionary<string, string> pathMap, XDoc doc, string attributeName, bool removeIfNotFound)
        {
            string atAttributeName = "@" + attributeName;
            string link = doc[atAttributeName].AsText;
            if (link != null)
            {
                string dekiUrl = GetMtPathFromConfluencePath(pathMap, link);
                if (dekiUrl != null)
                {
                    doc.Attr(attributeName, dekiUrl);
                }
                else
                {
                    if (removeIfNotFound && Utils.IsRelativePath(link))
                    {
                        doc.Remove();
                    }
                }
            }
        }

        private void ReplaceLinks(Dictionary<string, string> pathMap, XDoc doc)
        {
            foreach (XDoc tag in doc["//a"])
            {
                ReplaceLinkAttribute(pathMap, tag, "href", false);
            }
            foreach (XDoc tag in doc["//img"])
            {
                ReplaceLinkAttribute(pathMap, tag, "src", true);
            }
        }


    }
}

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
        private void MoveComments(int dekiPageId, long confluencePageId)
        {
            RemoteComment[] comments = _confluenceService.GetComments(confluencePageId);

            for (int i = 0; i <= comments.Length - 1; i++)
            {
                Log.DebugFormat("Processing comment for pageid: {0} commentid: {1} author: {2}", comments[i].pageId, comments[i].id, comments[i].creator);

                string commentContent = comments[i].content;
                string commentOriginalCreatedDate = comments[i].created.Value.ToShortDateString() + " " + comments[i].created.Value.ToShortTimeString();
                commentContent = commentContent + "\n [Comment originally added on " + commentOriginalCreatedDate + "]";

                // Deki not supported MimeType.HTML for content
                // commentContent = confluenceService.renderContent(token, spaceKey, confluencePageId, commentContent);
                // commentContent = ExtractPageContentAndReplaceLinks(commentContent);

                DreamMessage msg = DreamMessage.Ok(MimeType.TEXT_UTF8, commentContent);
                //DreamMessage msg = DreamMessage.Ok(MimeType.HTML, commentContent);

                Plug p = (comments[i].creator == null) ? _dekiPlug : GetPlugForConvertedUser(comments[i].creator);
                DreamMessage res = p.At("pages", dekiPageId.ToString(), "comments").PostAsync(msg).Wait();
                if (res.Status != DreamStatus.Ok)
                {
                    WriteLineToLog("Error converting comment");
                    WriteErrorResponse(res);
                    WriteErrorRequest(msg);
                }
            }
        }

        private void SaveCommentsLinks(XDoc spaceManifest, string space, long confluencePageId, string mtPath) {
            RemoteComment[] comments = _confluenceService.GetComments(confluencePageId);

            foreach(RemoteComment comment in comments) {
                LogCommentConversion(spaceManifest, space, comment, mtPath);
            }
        }
    }
}

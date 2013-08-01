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
        private void MoveLabels(int dekiPageId, long confluencePageId)
        {
            RemoteLabel[] labels = _confluenceService.GetLabelsById(confluencePageId);

            if (labels.Length == 0)
            {
                return;
            }

            XDoc tagsDoc = new XDoc("tags");

            foreach (RemoteLabel label in labels)
            {
                tagsDoc.Start("tag").Attr("value", label.name).End();
            }

            DreamMessage res = _dekiPlug.At("pages", dekiPageId.ToString(), "tags").PutAsync(tagsDoc).Wait();
            if (res.Status != DreamStatus.Ok)
            {
                WriteLineToLog("Error oconverting tag");
                WriteErrorResponse(res);
                WriteErrorRequest(tagsDoc);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;

using log4net;
using MindTouch.Xml;
using MindTouch.Dream;
using MindTouch.Tools.ConfluenceConverter.Confluence;
using System.IO;
using System.Collections.Specialized;
using System.ComponentModel;

namespace MindTouch.Tools.ConfluenceConverter
{
    public partial class ACConverter
    {
        public void MoveAttachments(XDoc spaceManifest, int dekiPageID, long confluencePageId)
        {
            RemoteAttachment[] attachments = _confluenceService.GetAttachments(confluencePageId);

            int count = attachments.Length;
            foreach (RemoteAttachment attachment in attachments)
            {
                // This was when a single attachment (the latest) was being handled
                // byte[] attachmentData;
                Log.DebugFormat("Processing file: {0} size: {1} #remaining: {2}", attachment.fileName, attachment.fileSize, --count);

                Plug p = (attachment.creator == null) ? _dekiPlug : GetPlugForConvertedUser(attachment.creator);
                DreamMessage res = p.At("pages", dekiPageID.ToString(), "files", "=" + Utils.DoubleUrlEncode(attachment.fileName), "info")
                    .GetAsync().Wait();

                if (res.Status == DreamStatus.Ok)
                {
                    Log.DebugFormat("Already converted: {0} MindTouch URL: {1}", attachment.fileName, res.AsDocument()["contents/@href"].AsText);
                }
                else
                {
                    MimeType attachmentMimeType;
                    if (!MimeType.TryParse(attachment.contentType, out attachmentMimeType))
                    {
                        attachmentMimeType = MimeType.FromFileExtension(attachment.fileName);
                    }

                    // The attachment URL contains the latest actual version of this attachment e.g.
                    // http://confluencesite.com/download/attachments/3604492/excelspreadsheet.xls?version=1

                    int latestVersion =1 , oldestVersion = 1;                    
                    if (attachment.url.Contains("version="))
                    {
                        try
                        {
                            latestVersion = ParseVersionFromUri(attachment.url);
                        }
                        catch{}
                    }

                    try
                    {                        
                        if (latestVersion > 1)
                        {
                            if ((latestVersion - 5) > 1)
                                oldestVersion = latestVersion - 5;
                        }

                        for (int x = oldestVersion; x <= latestVersion; x++)
                        {
                            try
                            {
                                byte[] ThisAttachmentData = _confluenceService.GetAttachmentData(confluencePageId, attachment.fileName, x);
                                DreamMessage msg = new DreamMessage(DreamStatus.Ok, null, attachmentMimeType, ThisAttachmentData);
                                string AttachmentTimeStampComment = attachment.comment + " - attachment version " + x + " originally created on " + attachment.created.Value.ToShortDateString() + " " + attachment.created.Value.ToShortTimeString();

                                res = p.At("pages", dekiPageID.ToString(), "files", "=" + Utils.DoubleUrlEncode(attachment.fileName))
                                 .With("description", AttachmentTimeStampComment)
                                .PutAsync(msg).Wait();

                                if (res.Status != DreamStatus.Ok)
                                {
                                    Log.WarnFormat("File '{0}' version '{1}' on Confluence pageid '{2}' not converted: {3}", attachment.fileName, x, attachment.pageId, res.ToString());
                                    // added as suggested by Max on the list created here http://projects.mindtouch.com/User:emilyp/Booze_Allen/Conversion_Issues
                                    throw new DreamAbortException(res);
                                }

                                XDoc resDoc = res.ToDocument();
                                string contentUrl = resDoc["contents/@href"].AsText;

                                LogFileConversion(spaceManifest, attachment, contentUrl);
                            }
                            catch (DreamAbortException dEx)
                            {
                                WriteLineToConsole("Error on moving attachment " + attachment.fileName + "version " + x);
                                if (!String.IsNullOrEmpty(dEx.Message))
                                {
                                    WriteLineToLog(dEx.Message);
                                }
                                continue;
                            }
                            catch (System.Web.Services.Protocols.SoapException e)
                            {
                                WriteLineToConsole("Error obtaining attachment data from confluence " + attachment.fileName + "version " + x);
                                if ((e.Detail != null) && (e.Detail.OuterXml != null))
                                {
                                    WriteLineToLog(e.Detail.OuterXml);
                                }
                                continue;
                            }
                        } // for loop to submit revisions
                        
                    }
                    catch (System.Web.Services.Protocols.SoapException e)
                    {
                        WriteLineToConsole("Error on moving attachment " + attachment.fileName);
                        if ((e.Detail != null) && (e.Detail.OuterXml != null))
                        {
                            WriteLineToLog(e.Detail.OuterXml);
                        }
                        continue;
                    }

                }
            }
        }

        public int ParseVersionFromUri(string Uri)
        {
            Uri url = new Uri(Uri);
            string strQuery = url.GetComponents(System.UriComponents.Query, System.UriFormat.UriEscaped);
            NameValueCollection query = System.Web.HttpUtility.ParseQueryString(strQuery);

            int id = query.GetValue<int>("version");

            return id;
        }

       

    }
}

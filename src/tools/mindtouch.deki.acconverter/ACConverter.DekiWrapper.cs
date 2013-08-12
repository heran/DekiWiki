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

        private int CreateDekiPage(Plug p, string pagePath, string pageTitle, DateTime? modified, string content)
        {
            string temp;
            return CreateDekiPage(p, pagePath, pageTitle, modified, content, out temp);
        }

        private int CreateDekiPage(Plug p, string pagePath, string pageTitle, DateTime? modified, string content,
            out string dekiPageUrl)
        {

            Log.DebugFormat("Creating page: '{0}' Content? {1}", XUri.DoubleDecode(pagePath), !string.IsNullOrEmpty(content));

            Plug pagePlug = p.At("pages", "=" + pagePath, "contents");
            pagePlug = pagePlug.With("abort", "never");
            modified = modified ?? DateTime.Now;
            string editTime = Utils.FormatPageDate(modified.Value.ToUniversalTime());
            pagePlug = pagePlug.With("edittime", editTime);
            pagePlug = pagePlug.With("comment", "Created at " + modified.Value.ToShortDateString() + " " + modified.Value.ToShortTimeString());
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
            }
            else
            {
                XDoc createdPage = res.AsDocument();
                int pageId = createdPage["page/@id"].AsInt.Value;

                //dekiPageUrl = createdPage["page/path"].AsText;
                
                //Using the uri.ui instead of path to not worry about encodings for links
                //But this makes the values (mt urls) in the link mapping table unsuitable for page paths 
                //(such as those used in dekiscript for macros). Those need to be converted to paths 
                // via Utils.ConvertPageUriToPath
                dekiPageUrl = createdPage["page/uri.ui"].AsText;

                return pageId;
            }
            dekiPageUrl = null;
            return -1;
        }       
              
    }
}

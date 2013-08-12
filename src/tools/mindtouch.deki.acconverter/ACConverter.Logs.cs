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
        //--- Methods ---
        private void WriteLineToLog(string message)
        {
            Log.Debug(message);
        }

        private void WriteLineToConsole(string message)
        {
            Log.Info(message);
        }

        private void WriteErrorResponse(DreamMessage response)
        {
            WriteLineToLog("Response: " + response.ToString());
            XDoc responseDoc = response.AsDocument();
            if ((responseDoc == null) || (responseDoc.IsEmpty))
            {
                return;
            }
            XDoc messageDoc = responseDoc["message"];
            if ((messageDoc == null) || (messageDoc.IsEmpty))
            {
                return;
            }
            string messageText = messageDoc.AsText;
            if (messageText == null)
            {
                return;
            }
            WriteLineToConsole("Error: " + messageText);
        }

        private void WriteErrorRequest(DreamMessage request)
        {
            try
            {
                WriteLineToLog("Request message: " + request.ToString());
            }
            catch { }
        }

        private void WriteErrorRequest(XDoc request)
        {
            WriteLineToLog("Request deoc: " + request.ToPrettyString());
        }

        
    }
}

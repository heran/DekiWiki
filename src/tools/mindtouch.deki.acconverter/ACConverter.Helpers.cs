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

namespace MindTouch.Tools.ConfluenceConverter
{
    public partial class ACConverter
    {
        private string ComputeSpaceRootPath(string basePath, RemoteSpaceSummary space, string teamlabel)
        {
            string ret = string.Empty;
            if (space.type == ConfluencePersonalSpaceTypeName)
            {

                //Personal space path. Confluence uses "~username" as key of personal space
                string userName = space.key.Substring(1);
                string dekiUserName = userName;
                ACConverterUserInfo userInfo = null;
                if (_convertedUsers.TryGetValue(userName.ToLower(), out userInfo))
                {
                    dekiUserName = userInfo.DekiUserName;
                }

                ret = basePath;
                if (!string.IsNullOrEmpty(basePath))
                {
                    ret += XUri.DoubleEncode("/");
                }
                //append team label path here if it exists
                if (!String.IsNullOrEmpty(teamlabel))
                {
                    ret += Utils.DoubleUrlEncode(teamlabel + "/");
                }

                ret = Utils.DoubleUrlEncode(Utils.GetDekiUserPageByUserName(dekiUserName));

            }
            else
            {

                // Global space path
                ret = basePath;
                if (!string.IsNullOrEmpty(basePath))
                {
                    ret += XUri.DoubleEncode("/");
                }
                //append team label path here if it exists
                if (!String.IsNullOrEmpty(teamlabel))
                {
                    //there is already a slash at the end of the ret so no need to add it
                   // if (( ret.Length == 0) || (ret.LastIndexOf('/') != ret.Length))
                    //    ret += XUri.DoubleEncode("/");
                    ret += Utils.DoubleUrlEncode(teamlabel + "/");
                }
                ret += Utils.DoubleUrlEncode(space.key);
            }
            return ret;
        }

        private string CreateRootPageForExport()
        {
            DateTime currentTime = DateTime.Now;

            string exportedRootPageName = Utils.DoubleUrlEncode("ConfluenceExport at " +
                Utils.FormatPageDate(currentTime));

            CreateDekiPage(_dekiPlug, exportedRootPageName, null, currentTime,
                "Exported from Confluence at " + currentTime.ToString());
            return exportedRootPageName;
        }

        private bool IsDekiPageExists(string pagePath)
        {
            DreamMessage res = _dekiPlug.At("pages", "=" + pagePath, "info").GetAsync().Wait();
            if (res.Status == DreamStatus.Ok)
            {
                return true;
            }
            if (res.Status == DreamStatus.NotFound)
            {
                return false;
            }
            throw new DreamAbortException(res);
        }

        private string GetAllowedDekiPageName(string basePagePath, string startPageName)
        {
            string newPageName = startPageName;

            int pageNum = 0;
            string slash = string.IsNullOrEmpty(basePagePath) ? string.Empty : "/";

            while (IsDekiPageExists(basePagePath + Utils.DoubleUrlEncode(slash + newPageName)))
            {
                pageNum++;
                newPageName = startPageName + pageNum.ToString();
            }

            return newPageName;
        }


    }
}

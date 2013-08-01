using System;
using System.Collections.Generic;

using MindTouch.Dream;

using System.IO;

namespace MindTouch.Tools.TWConverter
{
    class TWConverter
    {
        //--- Constants ---
        private const string ConvertedUsersFileName = "ConvertedTWikiUsers.xml";
        private const string TWikiUserNameAttribute = "TWikiUserName";
        private const string DekiUserNameAttribute = "DekiUserName";
        private const string ConvertedUsersRootName = "ConvertedUsers";
        private const string ConvertedUserTagName = "User";
        private const string ConvertedGroupTagName = "Group";
        private const string TWikiGroupNameAttribute = "TWikiGroupName";
        private const string DekiGroupNameAttribute = "DekiGroupName";

        private const string WebPrefrencesPageName = "WebPreferences.txt";

        //--- Class Methods ---
        public static bool Convert(string dreamAPI, string dekiUserName, string dekiUserPassword,
            string publishContribResultFilesPath, string exportPagesPath, string tWikiPubPath,
            string htpasswdFilePath, string tMainWebWikiDataPath, string tWikiDataDirectoryPath,
            string[] websToConvert, string linksToAttachmentsStartsWith,
            System.IO.TextWriter consoleTextWriter, System.IO.TextWriter logTextWriter)
        {
            TWConverter converter = new TWConverter(consoleTextWriter, logTextWriter);
            try
            {
                converter.ConnectToDeki(dreamAPI, dekiUserName, dekiUserPassword);
            }
            catch (DreamResponseException dre)
            {
                converter.WriteLineToConsole("Can not connect to Dream server.");
                converter.WriteLineToLog(dre.Response.ToString());
                converter.WriteLineToLog(dre.ToString());
                return false;
            }
            
            if (converter._connectedToDeki)
            {
                converter.WriteLineToConsole("Successfully connet to Dream server.");

                converter.Convert(publishContribResultFilesPath, exportPagesPath, tWikiPubPath,
                    htpasswdFilePath, tMainWebWikiDataPath, tWikiDataDirectoryPath, websToConvert,
                    linksToAttachmentsStartsWith);

                return true;
            }

            return false;
        }

        //--- Fields ---
        private Plug _dekiPlug;
        private bool _connectedToDeki = false;
        private Dictionary<string, string> _tWikiDekiUrls = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
        private Dictionary<string, string>_tWikiPages = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
        private Dictionary<string, TWikiUser> _convertedUsers = new Dictionary<string, TWikiUser>(StringComparer.InvariantCultureIgnoreCase);
        private Dictionary<string, TWikiGroup> _convertedGroups = new Dictionary<string, TWikiGroup>(StringComparer.InvariantCultureIgnoreCase);

        private System.IO.TextWriter _consoleTextWriter;
        private System.IO.TextWriter _logTextWriter;

        //--- Constructors ---
        public TWConverter(System.IO.TextWriter consoleTextWriter, System.IO.TextWriter logTextWriter)
        {
            this._consoleTextWriter = consoleTextWriter;
            this._logTextWriter = logTextWriter;
        }

        //--- Methods ---
        private void WriteLineToConsole(string message)
        {
            WriteLineToLog(message);
            if (_consoleTextWriter != null)
            {
                _consoleTextWriter.WriteLine(message);
            }
        }

        private void WriteLineToLog(string message)
        {
            if (_logTextWriter != null)
            {
                _logTextWriter.WriteLine(message);
            }
        }

        private void WriteErrorResponse(DreamMessage msg, string errorText)
        {
            if (msg == null)
            {
                return;
            }
            if (msg.Status == DreamStatus.Ok)
            {
                return;
            }
            WriteLineToConsole(errorText);
            if (msg.Status != DreamStatus.Ok)
            {
                XDoc errorDoc = msg.AsDocument();
                if ((errorDoc != null) && (!errorDoc.IsEmpty))
                {
                    XDoc messageDoc = errorDoc["message"];
                    if ((messageDoc != null) && (!messageDoc.IsEmpty))
                    {
                        string messageText = messageDoc.AsText;
                        if (!string.IsNullOrEmpty(messageText))
                        {
                            WriteLineToConsole(messageText);
                        }
                    }
                    WriteLineToLog(msg.ToString());
                }
            }
        }

        private void WriteLineToConsole()
        {
            WriteLineToConsole(string.Empty);
        }

        private string CreateDekiPage(Plug p, string pagePath, string pageTitle, DateTime? modified, string content)
        {
            Plug pagePlug = p.At("pages", "=" + pagePath, "contents");
            pagePlug = pagePlug.With("abort", "never");
            modified = modified ?? DateTime.Now;
            string editTime = modified.Value.ToUniversalTime().ToString("yyyyMMddHHmmss", 
                System.Globalization.CultureInfo.InvariantCulture.DateTimeFormat);
            pagePlug = pagePlug.With("edittime", editTime);
            if (pageTitle != null)
            {
                pagePlug = pagePlug.With("title", pageTitle);
            }
            DreamMessage msg = DreamMessage.Ok(MimeType.TEXT_UTF8, content);
            DreamMessage res = pagePlug.PostAsync(msg).Wait();
            if (res.Status != DreamStatus.Ok)
            {
                WriteErrorResponse(res, "Error converting page \"" + pageTitle + "\"");
                return null;
            }

            XDoc createdPage = res.AsDocument();
            string dekiPageUrl = createdPage["page/path"].AsText;
            return dekiPageUrl;
        }

        private string GetLocalPathAndQuery(string url)
        {
            XUri uri = XUri.TryParse(url);
            if (uri == null)
            {
                return null;
            }
            return uri.PathQueryFragment;
        }

        private string GetFileApiUrl(string fileUrl)
        {
            if (fileUrl.StartsWith("/@api") || (fileUrl.StartsWith("@api")))
            {
                return fileUrl;
            }
            return "/@api" + fileUrl;
        }

        private string UploadFile(string fullFilePath, string pageName)
        {
            string attachmentName = Path.GetFileName(fullFilePath);
            byte[] attachmentData = File.ReadAllBytes(fullFilePath);

            MimeType attachmentMimeType = MimeType.FromFileExtension(attachmentName);

            DreamMessage msg = new DreamMessage(DreamStatus.Ok, null, attachmentMimeType, attachmentData);

            Plug p = _dekiPlug.At("pages", "=" + XUri.DoubleEncodeSegment(_tWikiPages[pageName]), "files",
                "=" + XUri.DoubleEncodeSegment(attachmentName));
            DreamMessage res = p.PutAsync(msg).Wait();
            if (res.Status != DreamStatus.Ok)
            {
                WriteErrorResponse(res, "File " + attachmentName + " not converted.");
                return null;
            }

            XDoc resDoc = res.AsDocument();
            string fileUrl = resDoc["contents/@href"].AsText;
            fileUrl = GetFileApiUrl(GetLocalPathAndQuery(fileUrl));

            return fileUrl;
        }

        private string GetDekiUrlFromTWikiUrl(string tWikiUrl, string currentWeb)
        {
            string dekiUrl = null;
            if (_tWikiDekiUrls.TryGetValue(tWikiUrl, out dekiUrl))
            {
                return dekiUrl;
            }
            if (_tWikiDekiUrls.TryGetValue(GetPageNameWithWeb(tWikiUrl, currentWeb), out dekiUrl))
            {
                return dekiUrl;
            }
            return dekiUrl;
        }

        private void ReplaceAttribute(XDoc doc, string attributeName, string pageName, string currentWeb)
        {
            string atAttributeName = "@" + attributeName;
            string link = doc[atAttributeName].AsText;
            if (link == null)
            {
                return;
            }
            string dekiUrl = GetDekiUrlFromTWikiUrl(link, currentWeb);
            if (dekiUrl != null)
            {
                doc.Attr(attributeName, dekiUrl);
            }
        }

        private string ReplaceLinksFromString(string pageContent, string currentWeb, string tagName, string hrefName)
        {
            System.Text.RegularExpressions.Regex aRegex =
                new System.Text.RegularExpressions.Regex("<" + tagName + ".+" + hrefName + "=\"(?<url>[^\"]+)\"[^>]*>",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            System.Text.StringBuilder newContent = new System.Text.StringBuilder();

            int oldPos = 0;

            System.Text.RegularExpressions.Match aMatch = aRegex.Match(pageContent);
            while (aMatch.Success)
            {
                System.Text.RegularExpressions.Group urlGroup = aMatch.Groups["url"];

                string newUrl = GetDekiUrlFromTWikiUrl(urlGroup.Value, currentWeb);
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

        private string ReplaceLinks(string content, string pageName, string currentWeb)
        {
            XDoc contentDoc = XDocFactory.From(content, MimeType.HTML);
            if ((contentDoc == null) || (contentDoc.IsEmpty))
            {
                content = ReplaceLinksFromString(content, currentWeb, "img", "src");
                return ReplaceLinksFromString(content, currentWeb, "a", "href");
            }
            foreach (XDoc linkDoc in contentDoc["//a"])
            {
                ReplaceAttribute(linkDoc, "href", pageName, currentWeb);
            }
            foreach (XDoc imgDoc in contentDoc["//img"])
            {
                ReplaceAttribute(imgDoc, "src", pageName, currentWeb);
            }
            return contentDoc.Contents;
        }

        public void ConnectToDeki(string dreamApi, string dekiUserName, string dekiUserPassword)
        {
            _dekiPlug = Plug.New(dreamApi).WithCredentials(dekiUserName, dekiUserPassword);
            _dekiPlug.At("users", "authenticate").Get();

            DreamMessage userResponse = _dekiPlug.At("users", "=" + XUri.DoubleEncodeSegment(dekiUserName)).Get();
            XDoc resDoc = userResponse.AsDocument();
            string roleName = resDoc["permissions.user/role"].AsText;
            if ((roleName == null) || (roleName.ToLower() != "admin"))
            {
                WriteLineToConsole("User " + dekiUserName + " should have Admin role in Deki.");
                return;
            }

            _connectedToDeki = true;
        }

        private TWikiUser[] GetAllGroupMembers(string groupName)
        {
            TWikiGroup tWikiGroup = null;
            if (!_convertedGroups.TryGetValue(groupName, out tWikiGroup))
            {
                return new TWikiUser[0];
            }
            if (tWikiGroup == null)
            {
                return new TWikiUser[0];
            }
            Dictionary<string, TWikiUser> members = new Dictionary<string, TWikiUser>(StringComparer.InvariantCultureIgnoreCase);
            Dictionary<string, TWikiGroup> visitedGroups = new Dictionary<string, TWikiGroup>(StringComparer.InvariantCultureIgnoreCase);
            IntGetGroupMemebers(tWikiGroup, members, visitedGroups);
            TWikiUser[] result = new TWikiUser[members.Count];
            members.Values.CopyTo(result, 0);
            return result;
        }

        private void IntGetGroupMemebers(TWikiGroup tWikiGroup, Dictionary<string, TWikiUser> members, 
            Dictionary<string, TWikiGroup> visitedGroups)
        {
            foreach (string member in tWikiGroup.Members)
            {
                TWikiUser user = null;
                if (_convertedUsers.TryGetValue(member, out user))
                {
                    if (user != null)
                    {
                        members[member] = user;
                    }
                }
                TWikiGroup subGroup = null;
                if (visitedGroups.ContainsKey(member))
                {
                    continue;
                }
                if (_convertedGroups.TryGetValue(member, out subGroup))
                {
                    if (subGroup != null)
                    {
                        visitedGroups[member] = subGroup;
                        IntGetGroupMemebers(subGroup, members, visitedGroups);
                    }
                }
            }
        }

        private void LoadGroups(string tMainWebWikiDataPath)
        {
            foreach (string groupFileName in Directory.GetFiles(tMainWebWikiDataPath, "*Group.txt"))
            {
                string groupName = Path.GetFileNameWithoutExtension(Path.GetFileName(groupFileName));
                
                string groupContent = File.ReadAllText(groupFileName);

                TWikiGroup group = null;
                _convertedGroups.TryGetValue(groupName, out group);
                if (group == null)
                {
                    group = new TWikiGroup(true, groupName);
                }

                string[] members = ExtractNamesList(groupContent, "GROUP");

                foreach (string member in members)
                {
                    group.AddMemeber(member);
                }

                _convertedGroups[groupName] = group;
            }

            DreamMessage msg = _dekiPlug.At("groups").With("limit", int.MaxValue).GetAsync().Wait();
            if (msg.Status != DreamStatus.Ok)
            {
                WriteErrorResponse(msg, "Error while reciving groups from Deki. Groups not converted.");
                return;
            }
            Dictionary<string, string> dekiGroups = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);

            XDoc groupsDoc = msg.AsDocument();
            foreach (XDoc groupDoc in groupsDoc["//group"])
            {
                string dekiGroupName = groupDoc["groupname"].AsText;
                dekiGroups[dekiGroupName.ToLower()] = null;
            }

            foreach (string groupName in _convertedGroups.Keys)
            {
                TWikiGroup group = _convertedGroups[groupName];
                if (group.IsNewGroup)
                {
                    int groupNum = 0;
                    string dekiGroupName = groupName;
                    while (dekiGroups.ContainsKey(dekiGroupName))
                    {
                        groupNum++;
                        dekiGroupName = groupName + groupNum.ToString();
                    }
                    if (dekiGroupName != groupName)
                    {
                        WriteLineToConsole("TWiki group \"" + groupName + "\" converted as \"" + dekiGroupName + "\" becouse of existing same group in Deki");
                    }
                    group.DekiName = dekiGroupName;

                    XDoc newGroupDoc = new XDoc("group");
                    newGroupDoc.Elem("name", group.DekiName);

                    TWikiUser[] members = GetAllGroupMembers(groupName);
                    if (members.Length > 0)
                    {
                        newGroupDoc.Start("users");
                        foreach (TWikiUser member in members)
                        {
                            newGroupDoc.Start("user").Attr("id", member.DekiId).End();
                        }
                        newGroupDoc.End();
                    }

                    DreamMessage res = _dekiPlug.At("groups").PostAsync(newGroupDoc).Wait();
                    WriteErrorResponse(res, "Error converting group\"" + groupName + "\"");
                }
                else
                {
                    XDoc updateGroupDoc = new XDoc("users");
                    TWikiUser[] members = GetAllGroupMembers(groupName);
                    if (members.Length > 0)
                    {
                        foreach (TWikiUser member in members)
                        {
                            updateGroupDoc.Start("user").Attr("id", member.DekiId).End();
                        }
                    }
                    DreamMessage res = _dekiPlug.At("groups", "=" + group.DekiName, "users").PutAsync(updateGroupDoc).Wait();
                    WriteErrorResponse(res, "Error updating group \"" + groupName + "\" users.");
                }
            }
        }

        private string[] ExtractNamesList(string content, string parameterName)
        {
            bool temp = false;
            return ExtractNamesList(content, parameterName, out temp);
        }

        private string[] ExtractNamesList(string content, string parameterName, out bool found)
        {
            System.Text.RegularExpressions.Regex setRegex =
                    new System.Text.RegularExpressions.Regex("\\n\\s*\\*\\s+Set\\s+" + parameterName + "\\s+=(?<names>[^\\r\\n]*)",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            System.Text.RegularExpressions.Match setMatch = setRegex.Match(content);

            if (!setMatch.Success)
            {
                found = false;
                return new string[0];
            }

            found = true;

            string namesList = setMatch.Groups["names"].Value.Trim();
            if (namesList == string.Empty)
            {
                return new string[0];
            }

            string[] names = namesList.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            List<string> results = new List<string>();
            foreach (string name in names)
            {
                string resName = name.Trim();

                if (resName == string.Empty)
                {
                    continue;
                }

                int dotIndex = resName.LastIndexOf('.');
                if (dotIndex >= (resName.Length - 1))
                {
                    continue;
                }
                if (dotIndex >= 0)
                {
                    resName = resName.Substring(dotIndex + 1, resName.Length - dotIndex - 1);
                    resName = resName.Trim();
                }

                results.Add(resName);
            }

            return results.ToArray();
        }

        private List<TWikiUser> AppendUsersList(List<TWikiUser> users, List<TWikiGroup> groups)
        {
            List<TWikiUser> res = new List<TWikiUser>(users);
            foreach (TWikiGroup group in groups)
            {
                TWikiUser[] groupUsers = GetAllGroupMembers(group.TWikiName);
                foreach (TWikiUser groupUser in groupUsers)
                {
                    if (!res.Contains(groupUser))
                    {
                        res.Add(groupUser);
                    }
                }
            }
            return res;
        }

        private void ConvertPagePermissions(string tWikiWebDataPath, string exportPagesPath)
        {
            if (_convertedUsers.Count == 0)
            {
                return;
            }

            List<TWikiUser> allowViewWebUsers = new List<TWikiUser>();
            List<TWikiUser> denyViewWebUsers = new List<TWikiUser>();
            List<TWikiUser> allowChangeWebUsers = new List<TWikiUser>();
            List<TWikiUser> denyChangeWebUsers = new List<TWikiUser>();

            List<TWikiGroup> allowViewWebGroups = new List<TWikiGroup>();
            List<TWikiGroup> denyViewWebGroups = new List<TWikiGroup>();
            List<TWikiGroup> allowChangeWebGroups = new List<TWikiGroup>();
            List<TWikiGroup> denyChangeWebGroups = new List<TWikiGroup>();

            bool noWebChangeRestrictions = true;
            bool noWebRestrictions = true;

            bool foundAllowWebChange = false;
            bool foundAllowWebView = false;
            bool foundDenyWebView = false;
            bool foundDenyWebChange = false;

            string WebPrefrencesPagePath = Path.Combine(tWikiWebDataPath, WebPrefrencesPageName);
            if (File.Exists(WebPrefrencesPagePath))
            {
                string webPrefrencesPageContent = File.ReadAllText(WebPrefrencesPagePath);

                string[] allowChangeWebNames = ExtractNamesList(webPrefrencesPageContent, "ALLOWWEBCHANGE", out foundAllowWebChange);

                string[] denyChangeWebNames = ExtractNamesList(webPrefrencesPageContent, "DENYWEBCHANGE", out foundDenyWebChange);

                string[] allowViewWebNames = ExtractNamesList(webPrefrencesPageContent, "ALLOWWEBVIEW", out foundAllowWebView);
                
                string[] denyViewWebNames = ExtractNamesList(webPrefrencesPageContent, "DENYWEBVIEW", out foundDenyWebView);

                noWebChangeRestrictions = (allowChangeWebNames.Length == 0)
                                  && (denyChangeWebNames.Length == 0);

                noWebRestrictions = noWebChangeRestrictions
                                        && (allowViewWebNames.Length == 0)
                                        && (denyViewWebNames.Length == 0);

                foreach (string name in allowChangeWebNames)
                {
                    TWikiUser user = null;
                    if (_convertedUsers.TryGetValue(name, out user))
                    {
                        allowChangeWebUsers.Add(user);
                    }
                    TWikiGroup group = null;
                    if (_convertedGroups.TryGetValue(name, out group))
                    {
                        allowChangeWebGroups.Add(group);
                    }
                }

                foreach (string name in denyChangeWebNames)
                {
                    TWikiUser user = null;
                    if (_convertedUsers.TryGetValue(name, out user))
                    {
                        denyChangeWebUsers.Add(user);
                    }
                    TWikiGroup group = null;
                    if (_convertedGroups.TryGetValue(name, out group))
                    {
                        denyChangeWebGroups.Add(group);
                    }
                }

                foreach (string name in allowViewWebNames)
                {
                    TWikiUser user = null;
                    if (_convertedUsers.TryGetValue(name, out user))
                    {
                        allowViewWebUsers.Add(user);
                    }
                    TWikiGroup group = null;
                    if (_convertedGroups.TryGetValue(name, out group))
                    {
                        allowViewWebGroups.Add(group);
                    }
                }

                foreach (string name in denyViewWebNames)
                {
                    TWikiUser user = null;
                    if (_convertedUsers.TryGetValue(name, out user))
                    {
                        denyViewWebUsers.Add(user);
                    }
                    TWikiGroup group = null;
                    if (_convertedGroups.TryGetValue(name, out group))
                    {
                        denyViewWebGroups.Add(group);
                    }
                }
            }

            allowChangeWebUsers = AppendUsersList(allowChangeWebUsers, allowChangeWebGroups);
            allowViewWebUsers = AppendUsersList(allowViewWebUsers, allowViewWebGroups);
            denyChangeWebUsers = AppendUsersList(denyChangeWebUsers, denyChangeWebGroups);
            denyViewWebUsers = AppendUsersList(denyViewWebUsers, denyViewWebGroups);

            foreach (string tWikiPageDataFile in Directory.GetFiles(tWikiWebDataPath, "*.txt"))
            {
                if (!(Path.GetExtension(tWikiPageDataFile).TrimStart('.').ToLower() == "txt"))
                {
                    continue;
                }
                string pageName = Path.GetFileNameWithoutExtension(Path.GetFileName(tWikiPageDataFile));
                string pageDekiPath = XUri.DoubleEncodeSegment(exportPagesPath + "/" + pageName);
                string pageText = File.ReadAllText(tWikiPageDataFile);

                bool foundAllowTopicChange = false;
                string[] allowChangeNames = ExtractNamesList(pageText, "ALLOWTOPICCHANGE", out foundAllowTopicChange);

                bool foundDenyTopicChange = false;
                string[] denyChangeNames = ExtractNamesList(pageText, "DENYTOPICCHANGE", out foundDenyTopicChange);

                bool foundAllowTopicView = false;
                string[] allowViewNames = ExtractNamesList(pageText, "ALLOWTOPICVIEW", out foundAllowTopicView);

                bool foundDenyTopicView = false;
                string[] denyViewNames = ExtractNamesList(pageText, "DENYTOPICVIEW", out foundDenyTopicView);

                bool setSemiPublicPermissions = false;

                if ((foundDenyTopicView) && (denyViewNames.Length == 0))
                {
                    //See item 3 of "How TWiki evaluates ALLOW/DENY settings" on TWikiAccessControl page.
                    //This marked as deprecated and may change.
                    continue;
                }

                if (foundDenyTopicChange && (denyChangeNames.Length == 0))
                {
                    //See item 3 of "How TWiki evaluates ALLOW/DENY settings" on TWikiAccessControl page.
                    //This marked as deprecated and may change.
                    continue;
                }

                bool noTopicChangePermisions = (allowChangeNames.Length == 0)
                             && (denyChangeNames.Length == 0);

                bool noTopicRestrictions = noTopicChangePermisions
                                  && (allowViewNames.Length == 0)
                                  && (denyViewNames.Length == 0);

                if (noWebRestrictions && noTopicRestrictions)
                {
                    continue;
                }

                List<TWikiUser> allowViewTopicUsers = new List<TWikiUser>();
                List<TWikiUser> denyViewTopicUsers = new List<TWikiUser>();
                List<TWikiUser> allowChangeTopicUsers = new List<TWikiUser>();
                List<TWikiUser> denyChangeTopicUsers = new List<TWikiUser>();

                List<TWikiGroup> allowViewTopicGroups = new List<TWikiGroup>();
                List<TWikiGroup> denyViewTopicGroups = new List<TWikiGroup>();
                List<TWikiGroup> allowChangeTopicGroups = new List<TWikiGroup>();
                List<TWikiGroup> denyChangeTopicGroups = new List<TWikiGroup>();

                foreach (string name in allowChangeNames)
                {
                    TWikiUser user = null;
                    if (_convertedUsers.TryGetValue(name, out user))
                    {
                        allowChangeTopicUsers.Add(user);
                    }
                    TWikiGroup group = null;
                    if (_convertedGroups.TryGetValue(name, out group))
                    {
                        allowChangeTopicGroups.Add(group);
                    }
                }

                foreach (string name in denyChangeNames)
                {
                    TWikiUser user = null;
                    if (_convertedUsers.TryGetValue(name, out user))
                    {
                        denyChangeTopicUsers.Add(user);
                    }
                    TWikiGroup group = null;
                    if (_convertedGroups.TryGetValue(name, out group))
                    {
                        denyChangeTopicGroups.Add(group);
                    }
                }

                foreach (string name in allowViewNames)
                {
                    TWikiUser user = null;
                    if (_convertedUsers.TryGetValue(name, out user))
                    {
                        allowViewTopicUsers.Add(user);
                    }
                    TWikiGroup group = null;
                    if (_convertedGroups.TryGetValue(name, out group))
                    {
                        allowViewTopicGroups.Add(group);
                    }
                }

                foreach (string name in denyViewNames)
                {
                    TWikiUser user = null;
                    if (_convertedUsers.TryGetValue(name, out user))
                    {
                        denyViewTopicUsers.Add(user);
                    }
                    TWikiGroup group = null;
                    if (_convertedGroups.TryGetValue(name, out group))
                    {
                        denyViewTopicGroups.Add(group);
                    }
                }

                allowViewTopicUsers = AppendUsersList(allowViewTopicUsers, allowViewTopicGroups);
                allowChangeTopicUsers = AppendUsersList(allowChangeTopicUsers, allowChangeTopicGroups);
                denyViewTopicUsers = AppendUsersList(denyViewTopicUsers, denyViewTopicGroups);
                denyChangeTopicUsers = AppendUsersList(denyChangeTopicUsers, denyChangeTopicGroups);

                List<TWikiUser> allowedUserList = new List<TWikiUser>();

                if (foundAllowTopicView && !foundAllowTopicChange)
                {
                    foreach (TWikiUser user in allowViewTopicUsers)
                    {
                        if (!denyViewTopicUsers.Contains(user))
                        {
                            allowedUserList.Add(user);
                        }
                    }
                }

                if (!foundAllowTopicView && foundAllowTopicChange)
                {
                    if (denyViewTopicUsers.Count == 0)
                    {
                        setSemiPublicPermissions = true;
                    }
                    foreach (TWikiUser user in allowChangeTopicUsers)
                    {
                        if ((!denyChangeTopicUsers.Contains(user)) && (!denyViewTopicUsers.Contains(user)))
                        {
                            allowedUserList.Add(user);
                        }
                    }
                }

                if (foundAllowTopicChange && foundAllowTopicView)
                {
                    foreach (TWikiUser user in allowViewTopicUsers)
                    {
                        if (!denyViewTopicUsers.Contains(user))
                        {
                            allowedUserList.Add(user);
                        }
                    }
                    foreach (TWikiUser user in allowChangeTopicUsers)
                    {
                        if ((!denyViewTopicUsers.Contains(user)) && (!denyChangeTopicUsers.Contains(user)))
                        {
                            allowedUserList.Add(user);
                        }
                    }
                }

                if (!foundAllowTopicView && !foundAllowTopicChange)
                {
                    if ((allowViewWebUsers.Count != 0) && (allowChangeWebUsers.Count == 0))
                    {
                        foreach (TWikiUser user in allowViewWebUsers)
                        {
                            if ((!denyViewWebUsers.Contains(user)) && (!denyViewTopicUsers.Contains(user)))
                            {
                                allowedUserList.Add(user);
                            }
                        }
                    }

                    if ((allowViewWebUsers.Count == 0) && (allowChangeWebUsers.Count != 0))
                    {
                        if ((denyViewWebUsers.Count == 0) && (denyViewTopicUsers.Count == 0))
                        {
                            setSemiPublicPermissions = true;
                        }
                        foreach (TWikiUser user in allowChangeWebUsers)
                        {
                            if ((!denyViewWebUsers.Contains(user)) && (!denyChangeWebUsers.Contains(user))
                                && (!denyViewTopicUsers.Contains(user)) && (!denyChangeTopicUsers.Contains(user)))
                            {
                                allowedUserList.Add(user);
                            }
                        }
                    }

                    if ((allowViewWebUsers.Count != 0) && (allowChangeWebUsers.Count != 0))
                    {
                        foreach (TWikiUser user in allowViewWebUsers)
                        {
                            if ((!denyViewWebUsers.Contains(user)) && (!denyViewTopicUsers.Contains(user)))
                            {
                                allowedUserList.Add(user);
                            }
                        }
                        foreach (TWikiUser user in allowChangeWebUsers)
                        {
                            if ((!denyViewWebUsers.Contains(user)) && (!denyChangeWebUsers.Contains(user))
                                && (!denyViewTopicUsers.Contains(user)) && (!denyChangeTopicUsers.Contains(user)))
                            {
                                allowedUserList.Add(user);
                            }
                        }
                    }

                    if ((allowViewWebUsers.Count == 0) && (allowChangeWebUsers.Count == 0))
                    {
                        if ((denyViewTopicUsers.Count == 0) && (denyChangeTopicUsers.Count == 0)
                            && (denyViewWebUsers.Count == 0) && (denyChangeWebUsers.Count == 0))
                        {
                            continue;
                        }
                        if ((denyViewTopicUsers.Count == 0) && (denyViewWebUsers.Count == 0))
                        {
                            setSemiPublicPermissions = true;
                            foreach (TWikiUser user in _convertedUsers.Values)
                            {
                                if ((!denyChangeWebUsers.Contains(user)) && (!denyChangeTopicUsers.Contains(user)))
                                {
                                    allowedUserList.Add(user);
                                }
                            }
                        }
                        else
                        {
                            foreach (TWikiUser user in _convertedUsers.Values)
                            {
                                if ((!denyViewWebUsers.Contains(user)) && (!denyViewTopicUsers.Contains(user)))
                                {
                                    allowedUserList.Add(user);
                                }
                            }
                        }
                    }
                }

                List<TWikiUser> newAllowedUsers = new List<TWikiUser>();
                foreach (TWikiUser user in allowedUserList)
                {
                    if (!newAllowedUsers.Contains(user))
                    {
                        newAllowedUsers.Add(user);
                    }
                }

                if (noWebChangeRestrictions && noTopicChangePermisions)
                {
                    setSemiPublicPermissions = true;
                }

                string dekiRestrictions = "Private";

                if (setSemiPublicPermissions)
                {
                    dekiRestrictions = "Semi-Public";
                }

                XDoc securityDoc = new XDoc("security")
                .Start("permissions.page")
                    .Elem("restriction", dekiRestrictions)
                .End()
                .Start("grants");

                foreach (TWikiUser user in newAllowedUsers)
                {
                    securityDoc
                        .Start("grant")
                        .Start("permissions").Elem("role", "Contributor").End()
                    .Start("user").Attr("id", user.DekiId).End().End();
                }
                securityDoc.End();

                DreamMessage res = _dekiPlug.At("pages", "=" + pageDekiPath, "security").PutAsync(securityDoc).Wait();

                WriteErrorResponse(res, "Error converting page permissions \"" + pageName + "\"");
            }
        }

        private void ConvertUsers(string htpasswdFilePath)
        {
            WriteLineToConsole("Converting users...");
            string[] userLines = File.ReadAllLines(htpasswdFilePath);

            Dictionary<string, string> dekiUsers = new Dictionary<string, string>();

            DreamMessage usersResponse = _dekiPlug.At("users").With("limit", int.MaxValue).GetAsync().Wait();

            if (usersResponse.Status != DreamStatus.Ok)
            {
                WriteErrorResponse(usersResponse, "Error while reciving users from Deki. Users not converted.");
                return;
            }

            XDoc dekiUsersDoc = usersResponse.AsDocument();
            foreach (XDoc userDoc in dekiUsersDoc["//user"])
            {
                string dekiUserName = userDoc["nick"].AsText;
                dekiUsers[dekiUserName.ToLower()] = null;
            }

            foreach (string userLine in userLines)
            {
                string user = userLine.Trim();
                if (string.IsNullOrEmpty(user))
                {
                    continue;
                }
                int colonIndex = user.IndexOf(':');
                if (colonIndex <= 0)
                {
                    continue;
                }
                string userName = user.Substring(0, colonIndex);
                if (_convertedUsers.ContainsKey(userName))
                {
                    continue;
                }
                string eMail = null;
                int lastColonIndex = user.LastIndexOf(':');
                if ((lastColonIndex > 0) && (lastColonIndex > colonIndex) && (lastColonIndex + 1 < user.Length))
                {
                    eMail = user.Substring(lastColonIndex + 1, user.Length - lastColonIndex - 1);
                }

                string newUserName = userName;
                int userNum = 0;
                while (dekiUsers.ContainsKey(newUserName.ToLower()))
                {
                    userNum++;
                    newUserName = userName + userNum.ToString();
                }

                if (newUserName != userName)
                {
                    WriteLineToConsole("TWiki user \"" + userName + "\" converted as \"" + newUserName + "\" becouse of existing same user in Deki");
                }

                XDoc usersDoc = new XDoc("user")
                    .Elem("username", newUserName);
                if (!string.IsNullOrEmpty(eMail))
                {
                    usersDoc.Elem("email", eMail);
                }

                string newPassword = Guid.NewGuid().ToString();

                DreamMessage res = _dekiPlug.At("users").With("accountpassword", newPassword).PostAsync(usersDoc).Wait();
                WriteErrorResponse(res, "Error converting user \"" + userName + "\"");

                XDoc resDoc = res.AsDocument();
                int dekiId = resDoc["@id"].AsInt.Value;

                _convertedUsers[userName] = new TWikiUser(userName, newUserName, dekiId);
            }

            WriteLineToConsole("Users converted!!!");
            WriteLineToConsole();
        }

        private string GetPageNameWithWeb(string pageName, string webName)
        {
            return "..\\" + webName + "\\" + pageName;
        }

        private void ConvertPagesWithoutContent(string publishContribWebPath, string exportPagesPath, string webName)
        {
            foreach (string filePath in Directory.GetFiles(publishContribWebPath))
            {
                string fileExtension = Path.GetExtension(filePath).TrimStart('.').ToLower();
                if ((fileExtension != "htm") && (fileExtension != "html"))
                {
                    continue;
                }
                string pageFileName = Path.GetFileName(filePath);
                string pageName = Path.GetFileNameWithoutExtension(pageFileName);
                string dekiPagePath = XUri.DoubleEncodeSegment(exportPagesPath + "/" + pageName);
                string dekiPageUrl = CreateDekiPage(_dekiPlug, dekiPagePath, pageName, null, "");
                if (dekiPageUrl != null)
                {
                    //_tWikiDekiUrls[pageFileName] = dekiPageUrl;
                    _tWikiDekiUrls[GetPageNameWithWeb(pageFileName, webName)] = dekiPageUrl;
                    _tWikiPages[pageName] = dekiPageUrl;
                }
            }
        }

        private void ConvertAttachments(string tWikiWebAttachmentsPath, string attachmentsLinkStarts)
        {
            if (!Directory.Exists(tWikiWebAttachmentsPath))
            {
                return;
            }
            foreach (string pageAttachmentsDir in Directory.GetDirectories(tWikiWebAttachmentsPath))
            {
                string pageName = Path.GetFileName(pageAttachmentsDir);
                string pageAttachmentsRelativePath = Path.Combine(attachmentsLinkStarts, pageName);
                if (_tWikiPages.ContainsKey(pageName))
                {
                    foreach (string attachmentFilePath in Directory.GetFiles(pageAttachmentsDir))
                    {
                        if (attachmentFilePath.EndsWith(",v"))
                        {
                            continue;
                        }
                        string attachmentFileName = Path.GetFileName(attachmentFilePath);
                        string relativeAttachmentPath = Path.Combine(pageAttachmentsRelativePath, attachmentFileName);
                        string dekiFileUrl = UploadFile(attachmentFilePath, pageName);
                        _tWikiDekiUrls[relativeAttachmentPath.Replace('\\', '/')] = dekiFileUrl;
                    }
                }
            }
        }

        private void ConvertPagesContent(string publishContribWebFilesPath, string exportedWebPagesPath, string currentWeb)
        {
            foreach (string filePath in Directory.GetFiles(publishContribWebFilesPath))
            {
                string fileExtension = Path.GetExtension(filePath).TrimStart('.').ToLower();
                if ((fileExtension != "htm") && (fileExtension != "html"))
                {
                    continue;
                }
                string pageName = Path.GetFileNameWithoutExtension(Path.GetFileName(filePath));
                string dekiPagePath = XUri.DoubleEncodeSegment(exportedWebPagesPath + "/" + pageName);
                string content = null;
                using (StreamReader contentReader = new StreamReader(filePath))
                {
                    content = contentReader.ReadToEnd();
                    contentReader.Close();
                }
                
                content = ReplaceLinks(content, pageName, currentWeb);
                CreateDekiPage(_dekiPlug, dekiPagePath, pageName, null, content);
            }
        }

        public void Convert(string publishContribFilesPath, string exportPagesPath, string tWikiPubPath,
            string htpasswdFilePath, string tMainWebWikiDataPath, string tWikiDataDirectoryPath, 
            string[] websToConvert, string linksToAttachmentsStartsWith)
        {
            if (!_connectedToDeki)
            {
                throw new Exception("You should call ConnectToDeki before call Convert");
            }

            WriteLineToConsole();

            LoadConvertedUsersAndGroups();

            if (!string.IsNullOrEmpty(htpasswdFilePath))
            {
                //Convert users, if htpasswdFilePath specified
                ConvertUsers(htpasswdFilePath);
                if (!string.IsNullOrEmpty(tMainWebWikiDataPath))
                {
                    LoadGroups(tMainWebWikiDataPath);
                }
            }

            SaveConvertedUsersAndGroups();

            WriteLineToConsole("Converting pages without content...");
            foreach (string webName in websToConvert)
            {
                string webPath = Path.Combine(publishContribFilesPath, webName);
                string exportWebPagesPath = exportPagesPath + "/" + webName;
                ConvertPagesWithoutContent(webPath, exportWebPagesPath, webName);
            }
            WriteLineToConsole("Pages converted!!!");
            WriteLineToConsole();

            WriteLineToConsole("Converting attachments...");
            //Convert attachments, if tWikiPubPath specified
            if (!string.IsNullOrEmpty(tWikiPubPath))
            {
                foreach (string webName in websToConvert)
                {
                    string tWikiWebAttachmentsPath = Path.Combine(tWikiPubPath, webName);
                    string webAttachmentsLinkStarts = Path.Combine(linksToAttachmentsStartsWith, webName);
                    ConvertAttachments(tWikiWebAttachmentsPath, webAttachmentsLinkStarts);
                }
            }
            WriteLineToConsole("Attachments converted!!!");
            WriteLineToConsole();

            if (!string.IsNullOrEmpty(tWikiDataDirectoryPath))
            {
                WriteLineToConsole("Converting pages permissions...");
                foreach (string webName in websToConvert)
                {
                    string tWikiWebDataDirectoryPath = Path.Combine(tWikiDataDirectoryPath, webName);
                    string exportWebPagesPath = exportPagesPath + "/" + webName;
                    ConvertPagePermissions(tWikiWebDataDirectoryPath, exportWebPagesPath);
                }
                WriteLineToConsole("Page permissions converted!!!");
            }

            WriteLineToConsole("Converting pages content...");
            //Copy pages content
            foreach (string webName in websToConvert)
            {
                string webPath = Path.Combine(publishContribFilesPath, webName);
                string exportWebPagesPath = exportPagesPath + "/" + webName;
                ConvertPagesContent(webPath, exportWebPagesPath, webName);
            }

            WriteLineToConsole("Pages content converted!!!");
            WriteLineToConsole();
        }

        private void LoadConvertedUsersAndGroups()
        {
            if (!File.Exists(ConvertedUsersFileName))
            {
                return;
            }

            WriteLineToConsole("Loading converted users from " + ConvertedUsersFileName);

            XDoc convertedUsers = XDocFactory.LoadFrom(ConvertedUsersFileName, MimeType.XML);

            if ((convertedUsers == null) || (convertedUsers.IsEmpty))
            {
                return;
            }
            foreach (XDoc userDoc in convertedUsers["//" + ConvertedUserTagName])
            {
                string tWikiUserName = userDoc["@" + TWikiUserNameAttribute].AsText;
                if (tWikiUserName == null)
                {
                    continue;
                }
                string dekiUserName = userDoc["@" + DekiUserNameAttribute].AsText;
                if (dekiUserName == null)
                {
                    continue;
                }

                DreamMessage dekiUserMessage = _dekiPlug.At("users", "=" + XUri.DoubleEncodeSegment(dekiUserName)).GetAsync().Wait();
                if (dekiUserMessage.Status != DreamStatus.Ok)
                {
                    if (dekiUserMessage.Status == DreamStatus.NotFound)
                    {
                        WriteLineToConsole("User \"" + dekiUserName+"\" specified in \""+ ConvertedUsersFileName+ "\" but not found in Deki. New user created." );
                    }
                    else
                    {
                        WriteErrorResponse(dekiUserMessage, "Error reciving user data from Deki.");
                    }
                    continue;
                }
                XDoc dekiUserDoc = dekiUserMessage.AsDocument();
                int dekiUserId = dekiUserDoc["@id"].AsInt.Value;

                _convertedUsers[tWikiUserName] = new TWikiUser(tWikiUserName, dekiUserName, dekiUserId);
            }
            foreach (XDoc groupDoc in convertedUsers["//" + ConvertedGroupTagName])
            {
                string tWikiGroupName = groupDoc["@" + TWikiGroupNameAttribute].AsText;
                if (tWikiGroupName == null)
                {
                    continue;
                }
                string dekiGroupName = groupDoc["@" + DekiGroupNameAttribute].AsText;
                if (dekiGroupName == null)
                {
                    continue;
                }

                _convertedGroups[tWikiGroupName] = new TWikiGroup(dekiGroupName, false, tWikiGroupName);
            }
            WriteLineToConsole("Users loaded!!!");
            WriteLineToConsole();
        }

        private void SaveConvertedUsersAndGroups()
        {
            XDoc usersDoc = new XDoc(ConvertedUsersRootName);
            foreach (string tWikiUserName in _convertedUsers.Keys)
            {
                XDoc userDoc = new XDoc(ConvertedUserTagName);
                userDoc.Attr(TWikiUserNameAttribute, tWikiUserName);
                userDoc.Attr(DekiUserNameAttribute, _convertedUsers[tWikiUserName].DekiName);
                usersDoc.Add(userDoc);
            }
            foreach (string tWikiGroupName in _convertedGroups.Keys)
            {
                XDoc groupDoc = new XDoc(ConvertedGroupTagName);
                groupDoc.Attr(TWikiGroupNameAttribute, tWikiGroupName);
                groupDoc.Attr(DekiGroupNameAttribute, _convertedGroups[tWikiGroupName].DekiName);
                usersDoc.Add(groupDoc);
            }
            usersDoc.Save(ConvertedUsersFileName);
        }

    }
}
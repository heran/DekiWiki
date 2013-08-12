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
        private Plug GetPlugForConvertedUser(string confluenceUserName)
        {
            ACConverterUserInfo userInfo = null;
            if (!_convertedUsers.TryGetValue(confluenceUserName.ToLower(), out userInfo))
            {
                return _dekiPlug;
            }
            if (userInfo == null)
            {
                return _dekiPlug;
            }
            Plug p = Plug.New(_dreamAPI).WithCredentials(userInfo.DekiUserName,
                    userInfo.DekiPassword);
            DreamMessage res = p.At("users", "authenticate").GetAsync().Wait();
            if (res.Status != DreamStatus.Ok)
            {
                WriteLineToConsole("Can't authenticate to Deki using \"" + userInfo.DekiUserName + "\" login");
                WriteLineToLog("Confluence user name \"" + confluenceUserName + "\"");
                WriteLineToLog("Deki user name \"" + userInfo.DekiUserName + "\"");
                WriteErrorResponse(res);
                return _dekiPlug;
            }
            return p;
        }

        private void MoveUsers()
        {
            string[] userNames = _confluenceService.GetActiveUsers(true);

            Dictionary<string, string> dekiUsers = new Dictionary<string, string>();

            //Restore manifest from disk
            XDoc spaceManifest = GetManifestFromDisk(null) ?? new XDoc("manifest");

            DreamMessage usersResponse = _dekiPlug.At("users").With("limit", int.MaxValue).GetAsync().Wait();
            if (usersResponse.Status != DreamStatus.Ok)
            {
                WriteLineToConsole("Error while receiving users from Deki. Users not converted.");
                WriteErrorResponse(usersResponse);
                throw new DreamAbortException(usersResponse);

            }
            XDoc dekiUsersDoc = usersResponse.AsDocument();
            foreach (XDoc userDoc in dekiUsersDoc["//user"])
            {
                string dekiUserName = userDoc["nick"].AsText;
                dekiUsers[dekiUserName.ToLower()] = null;
            }

            foreach (string userName in userNames)
            {
                if (_convertedUsers.ContainsKey(userName.ToLower()))
                {
                    continue;

                    //TODO (maxm): even though a user already exists in MindTouch (perhaps created in previous run), because there is
                    // a correlated user within Confluence, the group and url mapping logic should still run.
                }

                //while user with same name exist try add number to user name.
                int userNum = 0;
                string newUserName = userName;
                while (dekiUsers.ContainsKey(newUserName.ToLower()))
                {
                    userNum++;
                    newUserName = userName + userNum.ToString();
                }

                if (newUserName != userName)
                {
                    WriteLineToConsole("Confluence user \"" + userName + "\" converted as \"" + newUserName + "\" becouse of existing same user in Deki");
                }

                RemoteUser confluenceUser = _confluenceService.GetUser(userName);

                XDoc usersDoc = new XDoc("user")
                    .Elem("username", newUserName)
                    .Elem("email", confluenceUser.email)
                    .Elem("fullname", confluenceUser.fullname);
                //.Start("permissions.user")
                //    .Elem("role", "Contributor")
                //.End();

                string newPassword = Guid.NewGuid().ToString();

                string[] userGroupNames = _confluenceService.GetUserGroups(confluenceUser.name);

                Log.DebugFormat("Creating user: {0} email: {1} fullname: {2}", newUserName, confluenceUser.email, confluenceUser.fullname);

                DreamMessage res = _dekiPlug.At("users").With("accountpassword", newPassword).PostAsync(usersDoc).Wait();
                if (res.Status != DreamStatus.Ok)
                {
                    WriteLineToConsole("Error converting user \"" + userName + "\"");
                    WriteErrorResponse(res);
                    WriteErrorRequest(usersDoc);
                    continue;
                }

                XDoc resUserDoc = res.AsDocument();
                int dekiUserId = resUserDoc["@id"].AsInt.Value;
                //TODO (maxm): retrieve the actual username from the XML

                ACConverterUserInfo newUser = new ACConverterUserInfo(newUserName, newPassword, dekiUserId, userGroupNames);
                _convertedUsers[confluenceUser.name.ToLower()] = newUser;

                LogUserConversion(spaceManifest, confluenceUser.name, dekiUserId.ToString(), newUserName, confluenceUser.url);
            }

            //Save the space manifest to disk
            PersistManifestToDisk(null, spaceManifest);
        }

        private void SaveUsersAndGroupsToXML()
        {
            XDoc doc = new XDoc(UsersAndGroupsXMLRootName);
            foreach (string confluenceUserName in _convertedUsers.Keys)
            {
                ACConverterUserInfo userInfo = _convertedUsers[confluenceUserName];
                XDoc userDoc = new XDoc(UserXMLTagName);
                userDoc.Attr(ConfluenceUserNameXMLAttributeName, confluenceUserName);
                userDoc.Attr(DekiUserNameXMLAttributeName, userInfo.DekiUserName);
                doc.Add(userDoc);
            }
            foreach (string confluenceGroupName in _convertedGroups.Keys)
            {
                ACConverterGroupInfo groupInfo = _convertedGroups[confluenceGroupName];
                XDoc groupDoc = new XDoc(GroupXMLTagName);
                groupDoc.Attr(ConfluenceGroupNameXMLAttributeName, confluenceGroupName);
                groupDoc.Attr(DekiGroupNameXMLAttributeName, groupInfo.DekiGroupName);
                doc.Add(groupDoc);
            }
            doc.Save(ConvertedUsersAndGroupsFileName);
        }

        private void LoadUsersAndGroupsFromXML()
        {
            WriteLineToConsole("Reading groups and users from " + ConvertedUsersAndGroupsFileName);
            Dictionary<string, string> readedDekiUsers = new Dictionary<string, string>();
            XDoc doc = XDocFactory.LoadFrom(ConvertedUsersAndGroupsFileName, MimeType.XML);
            foreach (XDoc user in doc["//" + UserXMLTagName])
            {
                string confluenceUserName = user["@" + ConfluenceUserNameXMLAttributeName].AsText;
                string dekiName = user["@" + DekiUserNameXMLAttributeName].AsText;

                if ((confluenceUserName == null) && (dekiName == null))
                {
                    WriteLineToConsole("Invalid XML attributes in " + ConvertedUsersAndGroupsFileName);
                    WriteLineToConsole(user.ToString());
                    continue;
                }

                if (confluenceUserName == null)
                {
                    WriteLineToConsole(ConfluenceUserNameXMLAttributeName + " not specified for " +
                        dekiName + " in " + ConvertedUsersAndGroupsFileName + ". Record skiped.");
                    continue;
                }

                if (dekiName == null)
                {
                    WriteLineToConsole(DekiUserNameXMLAttributeName + " not specified for " +
                        confluenceUserName + " in " + ConvertedUsersAndGroupsFileName + ". Record skiped.");
                    continue;
                }

                if (readedDekiUsers.ContainsKey(dekiName.ToLower()))
                {
                    WriteLineToConsole("Repeating entry of Deki user \"" + dekiName + "\". Record skiped.");
                    continue;
                }

                DreamMessage dekiUserMessage = _dekiPlug.At("users", "=" + Utils.DoubleUrlEncode(dekiName)).GetAsync().Wait();
                if (dekiUserMessage.Status == DreamStatus.NotFound)
                {
                    WriteLineToConsole("Deki user \"" + dekiName + "\" is specified in " + ConvertedUsersAndGroupsFileName +
                        " but not exists in Deki. New user created.");
                    continue;
                }
                XDoc dekiUserDoc = dekiUserMessage.AsDocument();
                int dekiUserId = dekiUserDoc["@id"].AsInt.Value;

                string newPassword = Guid.NewGuid().ToString();

                DreamMessage pass = DreamMessage.Ok(MimeType.TEXT, newPassword);
                DreamMessage res = _dekiPlug.At("users", dekiUserId.ToString(), "password").PutAsync(pass).Wait();
                if (res.Status != DreamStatus.Ok)
                {
                    WriteLineToConsole("Error converting user \"" + confluenceUserName + "\"");
                    WriteLineToLog("Confluence user name: " + confluenceUserName);
                    WriteLineToLog("Deki user name: " + dekiName);
                    WriteErrorResponse(res);
                    continue;
                }

                string[] userGroupNames = new string[0];
                bool userExistsInConfluence = _confluenceService.HasUser(confluenceUserName);
                if (userExistsInConfluence)
                {
                    userGroupNames = _confluenceService.GetUserGroups(confluenceUserName);
                }
                else
                {
                    WriteLineToConsole("Confluence user name \"" + confluenceUserName +
                        "\" specified in " + ConvertedUsersAndGroupsFileName + " but not exists in Confluence.");
                }
                ACConverterUserInfo userInfo = new ACConverterUserInfo(dekiName, newPassword, dekiUserId,
                    userGroupNames);
                if (_convertedUsers.ContainsKey(confluenceUserName.ToLower()))
                {
                    WriteLineToConsole("Repeating entry of user \"" + confluenceUserName + "\" into " + ConvertedUsersAndGroupsFileName +
                        ". Last record used.");
                }
                _convertedUsers[confluenceUserName.ToLower()] = userInfo;
                readedDekiUsers[dekiName.ToLower()] = confluenceUserName;
            }
            Dictionary<string, string> readedDekiGroups = new Dictionary<string, string>();
            foreach (XDoc group in doc["//" + GroupXMLTagName])
            {
                string confluenceGroupName = group["@" + ConfluenceGroupNameXMLAttributeName].AsText;
                string dekiGroupName = group["@" + DekiGroupNameXMLAttributeName].AsText;

                if ((confluenceGroupName == null) && (dekiGroupName == null))
                {
                    WriteLineToConsole("Invalid XML attributes in " + ConvertedUsersAndGroupsFileName);
                    WriteLineToConsole(group.AsText);
                    continue;
                }

                if (confluenceGroupName == null)
                {
                    WriteLineToConsole(ConfluenceGroupNameXMLAttributeName + " not specified for \"" +
                        dekiGroupName + "\" in " + ConvertedUsersAndGroupsFileName + ". Record skiped.");
                    continue;
                }

                if (dekiGroupName == null)
                {
                    WriteLineToConsole(DekiGroupNameXMLAttributeName + " not specified for \"" +
                        confluenceGroupName + "\" in " + ConvertedUsersAndGroupsFileName + ". Record skiped.");
                    continue;
                }

                if (readedDekiGroups.ContainsKey(dekiGroupName.ToLower()))
                {
                    WriteLineToConsole("Repeating entry of Deki group \"" + dekiGroupName + "\". Record skiped.");
                    continue;
                }

                dekiGroupName = dekiGroupName.Replace(" ", "_");
                DreamMessage dekiGroupMessage = _dekiPlug.At("groups", "=" + dekiGroupName).GetAsync().Wait();
                if (dekiGroupMessage.Status == DreamStatus.NotFound)
                {
                    WriteLineToConsole("Deki group \"" + dekiGroupName + "\" is specified in " + ConvertedUsersAndGroupsFileName +
                        " but not exists in Deki. New group created.");
                    continue;
                }
                XDoc dekiGroupDoc = dekiGroupMessage.AsDocument();
                int dekiGroupId = dekiGroupDoc["@id"].AsInt.Value;

                ACConverterGroupInfo groupInfo = new ACConverterGroupInfo(confluenceGroupName,
                    dekiGroupName, dekiGroupId);
                if (_convertedUsers.ContainsKey(confluenceGroupName.ToLower()))
                {
                    WriteLineToConsole("Repeating entry of group \"" + confluenceGroupName + "\" into " + ConvertedUsersAndGroupsFileName +
                        ". Last record used.");
                }
                _convertedGroups[confluenceGroupName.ToLower()] = groupInfo;
            }
            WriteLineToConsole("Users and groups readed!");
        }
    }
}

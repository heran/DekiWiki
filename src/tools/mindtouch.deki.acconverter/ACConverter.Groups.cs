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
        private void MoveGroups()
        {
            DreamMessage msg = _dekiPlug.At("groups").With("limit", int.MaxValue).GetAsync().Wait();
            if (msg.Status != DreamStatus.Ok)
            {
                WriteLineToConsole("Error while receiving groups from Deki. Groups not converted.");
                WriteErrorResponse(msg);
                return;
            }

            Dictionary<string, string> dekiGroups = new Dictionary<string, string>();

            XDoc groupsDoc = msg.AsDocument();
            foreach (XDoc groupDoc in groupsDoc["//group"])
            {
                string dekiGroupName = groupDoc["groupname"].AsText;
                dekiGroups[dekiGroupName.ToLower()] = null;
            }

            string[] confluenceGroupNames = _confluenceService.GetGroups();

            foreach (string confluenceGroupName in confluenceGroupNames)
            {
                string dekiGroupName;
                if (!_convertedGroups.ContainsKey(confluenceGroupName.ToLower()))
                {
                    int groupNum = 0;
                    dekiGroupName = confluenceGroupName;
                    while (dekiGroups.ContainsKey(dekiGroupName.ToLower()))
                    {
                        groupNum++;
                        dekiGroupName = confluenceGroupName + groupNum.ToString();
                    }
                    if (dekiGroupName != confluenceGroupName)
                    {
                        WriteLineToConsole("Confluence group \"" + confluenceGroupName + "\" converted as \"" + dekiGroupName + "\" becouse of existing same group in Deki");
                    }

                    XDoc newGroupDoc = new XDoc("group");
                    newGroupDoc.Elem("name", dekiGroupName)
                        .Start("users");

                    foreach (ACConverterUserInfo convertedUser in _convertedUsers.Values)
                    {
                        if (Array.IndexOf(convertedUser.ConfluenceUserGroupNames, confluenceGroupName) >= 0)
                        {
                            newGroupDoc.Start("user").Attr("id", convertedUser.DekiUserId).End();
                        }
                    }

                    newGroupDoc.End();

                    Log.DebugFormat("Creating group: {0}", dekiGroupName);

                    DreamMessage res = _dekiPlug.At("groups").PostAsync(newGroupDoc).Wait();
                    if (res.Status != DreamStatus.Ok)
                    {
                        WriteLineToLog("Error converting group \"" + confluenceGroupName + "\"");
                        WriteErrorResponse(res);
                        WriteErrorRequest(newGroupDoc);
                        continue;
                    }

                    XDoc resGroupsDoc = res.AsDocument();
                    int newDekiGroupId = resGroupsDoc["@id"].AsInt.Value;

                    ACConverterGroupInfo convertedGroup =
                        new ACConverterGroupInfo(confluenceGroupName, dekiGroupName, newDekiGroupId);
                    _convertedGroups[confluenceGroupName.ToLower()] = convertedGroup;
                }
                else
                {
                    //This group already converted during previous ACConverter start
                    dekiGroupName = _convertedGroups[confluenceGroupName.ToLower()].DekiGroupName;

                    XDoc usersDoc = new XDoc("users");
                    foreach (ACConverterUserInfo convertedUser in _convertedUsers.Values)
                    {
                        if (Array.IndexOf(convertedUser.ConfluenceUserGroupNames, confluenceGroupName) >= 0)
                        {
                            usersDoc.Start("user").Attr("id", convertedUser.DekiUserId).End();
                        }
                    }
                    DreamMessage res = _dekiPlug.At("groups", _convertedGroups[confluenceGroupName.ToLower()].DekiGroupId.ToString(),
                        "users").PutAsync(usersDoc).Wait();
                    if (res.Status != DreamStatus.Ok)
                    {
                        WriteLineToLog("Error converting group's users");
                        WriteErrorResponse(res);
                        WriteErrorRequest(usersDoc);
                    }
                }
            }
        }
    }
}

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
        private void MovePermissions(ACConverterPageInfo pageInfo)
        {
            RemotePermission[] pagePermissions = _confluenceService.GetPagePermissions(pageInfo.ConfluencePage.id);

            //Change permissions list according to parent page permissoins
            List<RemotePermission> newPermissions = new List<RemotePermission>();
            foreach (RemotePermission permission in pageInfo.ConfluenceUsersWithViewPermissions.Values)
            {
                newPermissions.Add(permission);
            }
            foreach (RemotePermission permission in pagePermissions)
            {
                if (permission.lockType == ConfluenceEditPermissionName)
                {
                    if ((pageInfo.ConfluenceUsersWithViewPermissions.Count == 0) || (pageInfo.ConfluenceUsersWithViewPermissions.ContainsKey(permission.lockedBy.ToLower())))
                    {
                        newPermissions.Add(permission);
                    }
                }
            }

            pagePermissions = newPermissions.ToArray();

            if (pagePermissions.Length == 0)
            {
                return;
            }

            string dekiRestriction;

            //Possible two entry of one user or group in pagePermissions.
            //As View permission and as Edit permission for this group/user.
            //To prevent repeated addition to Deki in this dictionary stored true
            //if permission to this user/group added to Deki.
            Dictionary<string, bool> permissionAddedToDeki = new Dictionary<string, bool>();

            Dictionary<string, bool> userHaveWritePermission = new Dictionary<string, bool>();

            if (_compatibleConvertUserPermissions)
            {
                bool onlyWriteRestrictions = true;
                foreach (RemotePermission permission in pagePermissions)
                {
                    if (permission.lockType == ConfluenceViewPermissionName)
                    {
                        onlyWriteRestrictions = false;
                        break;
                    }
                }
                if (onlyWriteRestrictions)
                {
                    //If there no view restrictions on this Confluence page set Semi-Public restrictions to Deki users
                    dekiRestriction = "Semi-Public";
                }
                else
                {
                    //If there is view restrictions on this Confluence page to allow users/groups with View and Edit
                    //restrictions view this page set Private restriction in Deki.
                    //Users without Edit permission but with View permission in Confluence can edit this page in Deki.
                    dekiRestriction = "Private";
                }
            }
            else
            {
                dekiRestriction = "Private";

                foreach (RemotePermission permission in pagePermissions)
                {
                    if (permission.lockType == ConfluenceEditPermissionName)
                    {
                        userHaveWritePermission[permission.lockedBy.ToLower()] = true;
                    }
                    else
                    {
                        if (!userHaveWritePermission.ContainsKey(permission.lockedBy.ToLower()))
                        {
                            userHaveWritePermission[permission.lockedBy.ToLower()] = false;
                        }
                    }
                }
            }

            XDoc securityDoc = new XDoc("security")
                .Start("permissions.page")
                    .Elem("restriction", dekiRestriction)
                .End()
                .Start("grants");

            foreach (RemotePermission permission in pagePermissions)
            {
                if (permissionAddedToDeki.ContainsKey(permission.lockedBy.ToLower()))
                {
                    continue;
                }

                securityDoc
                .Start("grant")
                    .Start("permissions");
                if (_compatibleConvertUserPermissions)
                {
                    securityDoc.Elem("role", "Contributor");
                }
                else
                {
                    bool haveWritePermission = false;
                    userHaveWritePermission.TryGetValue(permission.lockedBy.ToLower(), out haveWritePermission);
                    if (haveWritePermission)
                    {
                        securityDoc.Elem("role", "Contributor");
                    }
                    else
                    {
                        securityDoc.Elem("role", "Viewer");
                    }
                }
                securityDoc.End();

                //Detect if this is group or user permission
                ACConverterUserInfo dekiUser;
                if (_convertedUsers.TryGetValue(permission.lockedBy.ToLower(), out dekiUser))
                {
                    securityDoc.Start("user").Attr("id", dekiUser.DekiUserId).End();
                }
                else
                {
                    ACConverterGroupInfo dekiGroup = null;
                    if (_convertedGroups.TryGetValue(permission.lockedBy.ToLower(), out dekiGroup))
                    {
                        securityDoc.Start("group").Attr("id", dekiGroup.DekiGroupId).End();
                    }
                    else
                    {
                        WriteLineToConsole("Page " + pageInfo.ConfluencePage.title + " locked by " + permission.lockedBy +
                            " that is not a user and not a group. Restriction ignored.");
                    }
                }
                securityDoc.End();

                permissionAddedToDeki[permission.lockedBy.ToLower()] = true;
            }

            securityDoc.End();

            DreamMessage res = _dekiPlug.At("pages", pageInfo.DekiPageId.ToString(), "security").PutAsync(securityDoc).Wait();
            if (res.Status != DreamStatus.Ok)
            {
                WriteLineToLog("Error converting permissions");
                WriteErrorResponse(res);
                WriteErrorRequest(securityDoc);
            }
        }
    }
}

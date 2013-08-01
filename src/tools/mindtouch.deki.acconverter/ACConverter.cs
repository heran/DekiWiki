
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
using MindTouch.Tools.ConfluenceConverter.XMLRPC;

namespace MindTouch.Tools.ConfluenceConverter {
    public partial class ACConverter : IDisposable {
        //--- Constants ---
        private const string ConfluencePersonalSpaceTypeName = "personal";
        private const string ConfluenceViewPermissionName = "View";
        private const string ConfluenceEditPermissionName = "Edit";
        private const string NewsPageTitle = "News";
        private const string UndatedNewsPageTitle = "No date";
        private const string DefaultSpaceContents = "{{ wiki.tree{} }}";

        private const string ConvertedUsersAndGroupsFileName = "ConvertedConfluenceUsersAndGroups.xml";
        private const string UsersAndGroupsXMLRootName = "UsersAndGroups";
        private const string UserXMLTagName = "user";
        private const string GroupXMLTagName = "group";
        private const string DekiUserNameXMLAttributeName = "DekiName";
        private const string ConfluenceUserNameXMLAttributeName = "ConfluenceName";
        private const string DekiUserIdXMLAttributeName = "DekiUserId";
        private const string DekiGroupNameXMLAttributeName = "DekiName";
        private const string ConfluenceGroupNameXMLAttributeName = "ConfluenceName";
        private const string DekiGroupIdXMLAttributeName = "DekiGroupId";
        private const string TeamLabelPrefix = "mt-";
        public const string MacroStubStart = "‰";
        public const string MacroStubEnd = "‰";
        public static string ConfluenceBaseURL = "";

        private const int NumRevisionsToMove = 5;
        private const int MaxLengthOfPageTitle = 250;

        //--- Class fields ---
        public static log4net.ILog Log = LogManager.GetLogger("Converter");
        public static Regex _imgRegex = new Regex("<img.+src=\"(?<url>[^\"]+)\"[^>]*>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        public static Regex _aRegex = new Regex("<a.+href=\"(?<url>[^\"]+)\"[^>]*>", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        //--- Class Methods ---
        public static bool Convert(string confluenceXMLRPCUrl, string confluenceAPIUrl, string confluenceUserName, string confluenceUserPassword, string dreamAPI,
            string dekiUserName, string dekiUserPassword, bool compatibleConvertUserPermissions,
            List<string> spacesToConvert, bool processNewsPages, bool processPersonalSpaces, string fallbackSpacePrefix) {
            using(ACConverter converter = new ACConverter()) {
                try {
                    Log.Info("Connecting to MindTouch API");
                    converter.ConnectToDeki(dreamAPI, dekiUserName, dekiUserPassword);
                } catch(DreamResponseException dre) {
                    Log.Fatal("Can not connect to MindTouch API server.", dre);
                    return false;
                }

                if(converter._connectedToDeki) {
                    Log.Info("Successfully connected to MindTouch");
                } else {
                    Log.Fatal("Can not connect to MindTouch server.");
                    return false;
                }
                
                try {
                    Log.Info("Connecting to Confluence API");
                    converter.ConnectToConfluence(confluenceAPIUrl, confluenceUserName, confluenceUserPassword);
                } catch(System.Net.WebException e) {
                    Log.ErrorExceptionFormat(e, "Can not connect to Confluence");
                    return false;
                } catch(System.Web.Services.Protocols.SoapException e) {
                    if((e.Detail != null) && (e.Detail.OuterXml != null)) {
                        Log.Fatal("Can not connect to Confluence: "+ e.Detail.OuterXml, e);
                    } else {
                        Log.Fatal("Can not connect to Confluence", e);
                    }
                    return false;
                }

                // The base URL needs to be set globally in a static variable so that macros can access it. 
                // For example so the "include" macro knows if a given link is for a page on the current confluence site or not.
                ConfluenceBaseURL = converter._confluenceService.GetServerInfo().baseUrl;

               Log.Info("Connecting to Confluence XMLRPC API");
               if (!converter.ConnectToConfluenceRPC(confluenceXMLRPCUrl, confluenceUserName, confluenceUserPassword))
               {
                   Log.Fatal("Can not connect to Confluence XML RPC server.");
               }               

                Log.Info("Successfully connected to Confluence");
                RemoteServerInfo confluenceServerInfo = converter._confluenceService.GetServerInfo();
                Log.InfoFormat("Confluence version: {0}.{1}.{2}", confluenceServerInfo.majorVersion.ToString(),
                    confluenceServerInfo.minorVersion.ToString(), confluenceServerInfo.patchLevel.ToString());

                converter.Convert(new XUri(confluenceServerInfo.baseUrl), compatibleConvertUserPermissions, processNewsPages, spacesToConvert, processPersonalSpaces, fallbackSpacePrefix);
                return true;
            }
        }       
      
      
       
        public void Convert(XUri confBaseUrl, bool compatibleConvertUserPermissions, bool processNewsPages, List<string> spacesToConvert, bool processPersonalSpaces, string fallbackSpacePrefix) {
            this._confBaseUrl = confBaseUrl;
            this._compatibleConvertUserPermissions = compatibleConvertUserPermissions;
            this._processNewsPages = processNewsPages;
            this._processPesonalSpaces = processPersonalSpaces;
            this._spacesToConvert = spacesToConvert;
            this._fallbackSpacePrefix = fallbackSpacePrefix;

            if(!_connectedToDeki) {
                throw new Exception("You should call ConnectToDeki before call Convert");
            }

            if(!_confluenceService.Connected) {
                throw new Exception("You should call ConnectToConfluence before call Convert");
            }

            if(System.IO.File.Exists(ConvertedUsersAndGroupsFileName)) {
                LoadUsersAndGroupsFromXML();
            }

            WriteLineToConsole("Converting users...   ");
            MoveUsers();
            WriteLineToConsole("Users converted!");

            WriteLineToConsole("Converting groups...   ");
            MoveGroups();
            WriteLineToConsole("Groups converted!");

            SaveUsersAndGroupsToXML();

            WriteLineToConsole("Converting pages...   ");
            MovePageStubs();
            WriteLineToConsole("Pages converted!");

            Dictionary<string, string> pathMap = ReadUrlsFromAllManifests();

            WriteLineToConsole("Converting pages content...   ");
            
            MovePageContent(pathMap);
            WriteLineToConsole("Pages content converted!");

            WriteLineToConsole("Converting news content...   ");
            MoveNewsContent(pathMap);
            WriteLineToConsole("News content converted!");

            return;
        }

       

        public void Dispose() {
            if(!_disposed) {
                if(_confluenceService.Connected) {
                    _confluenceService.Logout();
                }
                _disposed = true;
            }
        }

        ~ACConverter() {
            Dispose();
        }

    }
}
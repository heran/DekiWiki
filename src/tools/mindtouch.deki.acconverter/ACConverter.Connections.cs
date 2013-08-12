using System;
using System.Collections.Generic;
using System.Text;


using log4net;
using MindTouch.Xml;
using MindTouch.Dream;
using MindTouch.Tools.ConfluenceConverter.Confluence;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using MindTouch.Tools.ConfluenceConverter.XMLRPC;
using MindTouch.Tools.ConfluenceConverter.MacroConverter;

namespace MindTouch.Tools.ConfluenceConverter
{
    public partial class ACConverter
    {
        //--- Fields ---
        private Plug _dekiPlug;
        private string _dreamAPI;
        private XUri _confBaseUrl;
        private bool _compatibleConvertUserPermissions = true;
        private bool _processNewsPages = true;
        private bool _processPesonalSpaces = true;
        private ConfluenceSoapServicesWrapper _confluenceService;
        private CFRpcClient _rpcclient;
        private Dictionary<string, ACConverterUserInfo> _convertedUsers = new Dictionary<string, ACConverterUserInfo>();
        private Dictionary<string, ACConverterGroupInfo> _convertedGroups = new Dictionary<string, ACConverterGroupInfo>();
        private List<ACConverterNewsInfo> _convertedNews = new List<ACConverterNewsInfo>();
        private bool _disposed = false;
        private List<string> _spacesToConvert;
        private string _fallbackSpacePrefix;

        private bool _connectedToDeki = false;

        protected MacroConverters Converters = new MacroConverters();

        //--- Constructors ---
        public ACConverter()
        {
            _confluenceService = new ConfluenceSoapServicesWrapper(this.WriteLineToLog);

            PrepareMacroConverters();
        }

        void PrepareMacroConverters()
        {
            Converters.AddMacroConverter(new IndexMacroConverter());
            Converters.AddMacroConverter(new IncludeMacroConverter());
            Converters.AddMacroConverter(new RssMacroConverter());
            Converters.AddMacroConverter(new ContentByLabelMacroConverter());
            Converters.AddMacroConverter(new ContributorsMacroConverter());
            Converters.AddMacroConverter(new CreateSpaceButtonMacroConverter());
            Converters.AddMacroConverter(new FavPagesMacroConverter());
            //TODO: need to sort out the parsing for body based macros
            Converters.AddMacroConverter(new CodeMacroConverter());
            Converters.AddMacroConverter(new NavMapMacroConverter());

        }

        public void ConnectToDeki(string dreamAPI, string dekiUserName, string dekiUserPassword)
        {
            this._dreamAPI = dreamAPI;
            this._dekiPlug = Plug.New(_dreamAPI).WithCredentials(dekiUserName, dekiUserPassword);

            DreamMessage authResponse = _dekiPlug.At("users", "authenticate").GetAsync().Wait();
            if(!authResponse.IsSuccessful) {
                Log.FatalFormat("Could not connect to MT API '{0}' username: '{1}' password: '{2}' Error:", _dekiPlug.ToString(), dekiUserName, dekiUserPassword, authResponse.ToString());
                throw new DreamAbortException(authResponse);
            }

            //Check that user have admin rights
            DreamMessage userResponse = _dekiPlug.At("users", "=" + Utils.DoubleUrlEncode(dekiUserName)).Get();
            XDoc resDoc = userResponse.AsDocument();
            string roleName = resDoc["permissions.user/role"].AsText;
            if ((roleName == null) || (roleName.ToLower() != "admin"))
            {
                WriteLineToConsole("User " + dekiUserName + " should have Admin role in Deki.");
                return;
            }

            _connectedToDeki = true;
        }

        public void ConnectToConfluence(string confluenceAPIUrl, string confluenceUserName, string confluenceUserPassword)
        {
            _confluenceService.ConnectToConfluence(confluenceAPIUrl, confluenceUserName, confluenceUserPassword);
        }

        public bool ConnectToConfluenceRPC(string confluenceXMLRPCUrl, string confluenceUserName, string confluenceUserPassword)
        {
            if(string.IsNullOrEmpty(confluenceXMLRPCUrl)) {
                return false;
            }

            try
            {
                _rpcclient = new CFRpcClient(confluenceXMLRPCUrl, confluenceUserName, confluenceUserPassword);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }        

    }
}

using System;

using MindTouch.Tools.ConfluenceConverter.Confluence;

internal delegate void LogWriter(string message);

internal class ConfluenceSoapServicesWrapper {
    //--- Constants ---
    private const int MaxMethodCalls = 2;

    //--- Fields ---
    private ConfluenceSoapServiceService _confluenceService;
    private Type _confluenceSoapServiceType = typeof(ConfluenceSoapServiceService);
    private string _token;
    private string _confluenceApiUrl;
    private string _userName;
    private string _password;
    private bool _connected = false;
    private LogWriter _logWriter;

    //--- Constructors ---
    public ConfluenceSoapServicesWrapper(LogWriter logWriter) {
        this._logWriter = logWriter;
    }

    //--- Properties ---
    public string Token {
        get {
            return _token;
        }
    }

    public string ConfluenceApiUrl {
        get {
            return _confluenceApiUrl;
        }
    }

    public bool Connected {
        get {
            return _connected;
        }
    }

    public ConfluenceSoapServiceService SoapService {
        get {
            return _confluenceService;
        }
    }

    //--- Methods ---
    private void WriteToLog(string message) {
        if(_logWriter != null) {
            _logWriter(message);
        }
    }

    public string ConnectToConfluence(string confluenceApiUrl, string userName, string password) {
        this._confluenceApiUrl = confluenceApiUrl;
        this._userName = userName;
        this._password = password;
        string res = Reconnect();
        _connected = true;
        return res;
    }

    public string Reconnect() {
        _confluenceService = new ConfluenceSoapServiceService();
        _confluenceService.Url = _confluenceApiUrl;
        _token = _confluenceService.login(_userName, _password);
        return _token;
    }

    public void Logout() {
        try {
            _confluenceService.logout(_token);
        } catch(System.Web.Services.Protocols.SoapException) {
            return;
        }
    }

    private object InvokeConfluenceMethod(string methodName, params object[] args) {
        int numOfCalls = 0;
        while(true) {
            try {
                object[] newArgs = new object[args.Length + 1];
                newArgs[0] = _token;
                args.CopyTo(newArgs, 1);
                return _confluenceSoapServiceType.InvokeMember(methodName, System.Reflection.BindingFlags.InvokeMethod,
                    null, _confluenceService, newArgs);
            } catch(System.Reflection.TargetInvocationException e) {
                if(e.InnerException == null) {
                    throw;
                }
                System.Web.Services.Protocols.SoapException soapException =
                    e.InnerException as System.Web.Services.Protocols.SoapException;
                if(soapException == null) {
                    throw e.InnerException;
                }
                numOfCalls++;
                if(numOfCalls >= MaxMethodCalls) {
                    throw soapException;
                }

                string message = "Confluence exception. ";
                if(soapException.Detail != null) {
                    message += soapException.Detail.OuterXml;
                }
                WriteToLog(message);

                Reconnect();
            }
        }
    }

    public RemoteServerInfo GetServerInfo() {
        return (RemoteServerInfo)InvokeConfluenceMethod("getServerInfo");
    }

    public bool HasUser(string userName) {
        return (bool)InvokeConfluenceMethod("hasUser", userName);
    }

    public string[] GetUserGroups(string userName) {
        return (string[])InvokeConfluenceMethod("getUserGroups", userName);
    }

    public RemoteUser GetUser(string userName) {
        return (RemoteUser)InvokeConfluenceMethod("getUser", userName);
    }

    public string[] GetGroups() {
        return (string[])InvokeConfluenceMethod("getGroups");
    }

    public RemoteAttachment[] GetAttachments(long pageId) {
        return (RemoteAttachment[])InvokeConfluenceMethod("getAttachments", pageId);
    }

    public byte[] GetAttachmentData(long pageId, string attachmentFileName, int version) {
        return (byte[])InvokeConfluenceMethod("getAttachmentData", pageId, attachmentFileName, version);
    }

    public RemoteComment[] GetComments(long pageId) {
        return (RemoteComment[])InvokeConfluenceMethod("getComments", pageId);
    }

    public RemoteLabel[] GetLabelsById(long pageId) {
        return (RemoteLabel[])InvokeConfluenceMethod("getLabelsById", pageId);
    }

    public RemotePermission[] GetPagePermissions(long pageId) {
        return (RemotePermission[])InvokeConfluenceMethod("getPagePermissions", pageId);
    }

    public RemoteBlogEntrySummary[] GetBlogEntries(string spaceKey) {
        return (RemoteBlogEntrySummary[])InvokeConfluenceMethod("getBlogEntries", spaceKey);
    }

    public RemotePage GetPage(long pageId) {
        return (RemotePage)InvokeConfluenceMethod("getPage", pageId);
    }

    public RemotePageHistory[] GetPageHistory(long pageId)
    {
        return (RemotePageHistory[])InvokeConfluenceMethod("getPageHistory", pageId);
    }

    public string RenderContent(string spaceKey, long pageId, string content) {
        return (string)InvokeConfluenceMethod("renderContent", spaceKey, pageId, content);
    }

    public string[] GetActiveUsers(bool viewAll) {
        return (string[])InvokeConfluenceMethod("getActiveUsers", viewAll);
    }

    public RemotePageSummary[] GetChildren(long pageId) {
        return (RemotePageSummary[])InvokeConfluenceMethod("getChildren", pageId);
    }

    public RemoteSpaceSummary[] GetSpaces() {
        return (RemoteSpaceSummary[])InvokeConfluenceMethod("getSpaces");
    }

    public RemoteSpace GetSpace(string spaceKey) {
        return InvokeConfluenceMethod("getSpace", spaceKey) as RemoteSpace;
    }

    public RemotePageSummary[] GetPages(string spaceKey) {
        return (RemotePageSummary[])InvokeConfluenceMethod("getPages", spaceKey);
    }

    public RemotePageSummary[] GetTopLevelPages(string spaceKey) {
        return (RemotePageSummary[])InvokeConfluenceMethod("getTopLevelPages", spaceKey);
    }
}
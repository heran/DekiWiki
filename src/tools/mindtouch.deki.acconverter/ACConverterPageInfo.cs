
using System;
using MindTouch.Tools.ConfluenceConverter.Confluence;
using System.Collections.Generic;

namespace MindTouch.Tools.ConfluenceConverter {

    [Serializable]
    public class ACConverterPageInfo {
        //--- Fields ---
        private RemotePage _confluenceRemotePage;

        //TODO (maxm): This naming convention is just backwords.
        private string _dekiPagePath; // double URI encoded path segment used for the API call
        private string _dekiPageUrl; // returned from MT API's page xml "path"
        private string _spaceRootPath; // points to the MT root page of the space containing this page
        private int _dekiPageId;
        private string _pageTitle;
        private string _tinyUrl;
        private ACConverterPageInfo _parentPage;
        private Dictionary<string, RemotePermission> _confluenceUsersWithViewPermissions = new Dictionary<string, RemotePermission>();

        //--- Constructors ---
        public ACConverterPageInfo(RemotePage confluenceRemotePage, string spaceRootPath, string dekiPageUrl, string dekiPagePath, int dekiPageId,
            string pageTitle, string tinyUrl, ACConverterPageInfo parentPage) {
            this._confluenceRemotePage = confluenceRemotePage;
            this._dekiPageUrl = dekiPageUrl;
            this._dekiPagePath = dekiPagePath;
            this._dekiPageId = dekiPageId;
            this._pageTitle = pageTitle;
            this._parentPage = parentPage;
            this._tinyUrl = tinyUrl;
            this._spaceRootPath = spaceRootPath;
        }

        //--- Properties ---
        public RemotePage ConfluencePage {
            get {
                return _confluenceRemotePage;
            }
        }

        public string SpaceRootPath {
            get {
                return _spaceRootPath;
            }
        }

        public string DekiPagePath {
            get {
                return _dekiPagePath;
            }
        }

        public string DekiPageUrl {
            get {
                return _dekiPageUrl;
            }
        }

        public int DekiPageId {
            get {
                return _dekiPageId;
            }
        }

        public string PageTitle {
            get {
                return _pageTitle;
            }
        }

        public string TinyUrl {
            get {
                return _tinyUrl;
            }
        }

        public ACConverterPageInfo ParentPage {
            get {
                return _parentPage;
            }
        }

        public Dictionary<string, RemotePermission> ConfluenceUsersWithViewPermissions {
            get {
                return _confluenceUsersWithViewPermissions;
            }
        }
    }
}
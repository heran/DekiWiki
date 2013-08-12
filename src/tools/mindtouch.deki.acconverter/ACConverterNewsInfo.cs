
using System;
using MindTouch.Tools.ConfluenceConverter.Confluence;

namespace MindTouch.Tools.ConfluenceConverter {
    internal class ACConverterNewsInfo {
        //--- Fields ---
        private RemoteBlogEntrySummary _confluenceRemoteNews;
        private string _dekiPagePath;
        private int _dekiPageId;
        private string _pageTitle;

        //--- Constructors ---
        public ACConverterNewsInfo(RemoteBlogEntrySummary confluenceRemoteNews, string dekiPagePath, int dekiPageId, string pageTitle) {
            this._confluenceRemoteNews = confluenceRemoteNews;
            this._dekiPagePath = dekiPagePath;
            this._dekiPageId = dekiPageId;
            this._pageTitle = pageTitle;
        }

        //--- Properties ---
        public RemoteBlogEntrySummary ConfluenceNews {
            get {
                return _confluenceRemoteNews;
            }
        }

        public string DekiPagePath {
            get {
                return _dekiPagePath;
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
    }
}
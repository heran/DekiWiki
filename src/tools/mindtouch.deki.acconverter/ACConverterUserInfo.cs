using System;
using MindTouch.Tools.ConfluenceConverter.Confluence;

namespace MindTouch.Tools.ConfluenceConverter {
    internal class ACConverterUserInfo {
        //--- Fields ---
        private string _dekiUserName;
        private string _dekiPassword;
        private string[] _ConfluenceUserGroupNames;
        private int _dekiUserId;

        //--- Constructors ---
        public ACConverterUserInfo(string dekiUserName, string dekiPassword,
            int DekiUserId, string[] confluenceUserGroupNames) {
            this._dekiUserName = dekiUserName;
            this._dekiPassword = dekiPassword;
            this._ConfluenceUserGroupNames = confluenceUserGroupNames;
            this._dekiUserId = DekiUserId;
        }

        //--- Properties ---
        public string DekiUserName {
            get {
                return _dekiUserName;
            }
        }

        public int DekiUserId {
            get {
                return _dekiUserId;
            }
        }

        public string DekiPassword {
            get {
                return _dekiPassword;
            }
        }

        public string[] ConfluenceUserGroupNames {
            get {
                return _ConfluenceUserGroupNames;
            }
        }
    }
}
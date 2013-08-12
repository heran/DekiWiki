using System;

namespace MindTouch.Tools.ConfluenceConverter {
    internal class ACConverterGroupInfo {
        //--- Fields ---
        private string _confluenceGroupName;
        private string _dekiGroupName;
        private int _dekiGroupId;

        //--- Constructors ---
        public ACConverterGroupInfo(string confluenceGroupName, string dekiGroupName, int dekiGroupId) {
            this._confluenceGroupName = confluenceGroupName;
            this._dekiGroupName = dekiGroupName;
            this._dekiGroupId = dekiGroupId;
        }

        //--- Properties ---
        public string ConfluenceGroupName {
            get {
                return _confluenceGroupName;
            }
        }

        public string DekiGroupName {
            get {
                return _dekiGroupName;
            }
        }

        public int DekiGroupId {
            get {
                return _dekiGroupId;
            }
        }
    }
}
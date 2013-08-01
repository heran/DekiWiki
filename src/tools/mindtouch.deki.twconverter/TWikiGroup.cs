using System;
using System.Collections.Generic;

namespace MindTouch.Tools.TWConverter
{
    class TWikiGroup
    {
        private Dictionary<string, bool> _members = new Dictionary<string, bool>(StringComparer.InvariantCultureIgnoreCase);
        private string _dekiName;
        private string _tWikiName;
        private bool _isNewGroup;

        public TWikiGroup(string dekiName, bool isNewGroup, string tWikiName)
        {
            this._dekiName = dekiName;
            this._isNewGroup = isNewGroup;
            this._tWikiName = tWikiName;
        }

        public TWikiGroup(bool isNewGroup, string tWikiName)
        {
            this._isNewGroup = isNewGroup;
            this._tWikiName = tWikiName;
        }

        public bool HasMemeber(string memeberName)
        {
            if (_members.ContainsKey(memeberName))
            {
                return true;
            }
            return false;
        }

        public void AddMemeber(string memeberName)
        {
            _members[memeberName] = true;
        }

        public string DekiName
        {
            get
            {
                return _dekiName;
            }
            set
            {
                _dekiName = value;
            }
        }

        public string TWikiName
        {
            get
            {
                return _tWikiName;
            }
        }

        public bool IsNewGroup
        {
            get
            {
                return _isNewGroup;
            }
        }

        public string[] Members
        {
            get
            {
                string[] res = new string[_members.Count];
                _members.Keys.CopyTo(res, 0);
                return res;
            }
        }
    }
}
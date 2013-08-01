using System;

namespace MindTouch.Tools.TWConverter
{
    class TWikiUser
    {
        private string _tWikiName;
        private string _dekiName;
        private int _dekiId;

        public TWikiUser(string tWikiName, string dekiName, int dekiId)
        {
            this._tWikiName = tWikiName;
            this._dekiName = dekiName;
            this._dekiId = dekiId;
        }

        public string TWikiName
        {
            get
            {
                return _tWikiName;
            }
        }

        public string DekiName
        {
            get
            {
                return _dekiName;
            }
        }

        public int DekiId
        {
            get
            {
                return _dekiId;
            }
        }
    }
}
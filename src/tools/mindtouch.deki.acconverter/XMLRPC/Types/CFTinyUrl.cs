using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace MindTouch.Tools.ConfluenceConverter.XMLRPC.Types
{
    public struct CFTinyUrl : WikiType
    {
        public string tinyurl;

        #region WikiType Members

        public Hashtable GetAsHashtable()
        {
            Hashtable urlhash = new Hashtable();
            urlhash.Add("tinyurl", this.tinyurl);
            return urlhash;

        }

        public void PopulateFromHashtable(Hashtable hash)
        {
            this.tinyurl = CFUtils.GetHashValueAsNonNullString(hash, "tinyurl");
        }

        #endregion
    }
}

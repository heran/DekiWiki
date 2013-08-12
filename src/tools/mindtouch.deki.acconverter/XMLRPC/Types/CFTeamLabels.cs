using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace MindTouch.Tools.ConfluenceConverter.XMLRPC.Types
{
    public struct CFTeamLabels : WikiType
    {
        public string Label;

        #region WikiType Members

        public System.Collections.Hashtable GetAsHashtable()
        {
            Hashtable urlhash = new Hashtable();
            urlhash.Add("Label", this.Label);
            return urlhash;
        }

        public void PopulateFromHashtable(System.Collections.Hashtable hash)
        {
            this.Label = CFUtils.GetHashValueAsNonNullString(hash, "Label");
        }

        #endregion
    }
}

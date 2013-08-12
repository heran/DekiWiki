using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace MindTouch.Tools.ConfluenceConverter.XMLRPC
{
    public static class CFUtils
    {
        public static string GetHashValueAsNonNullString(Hashtable hashtable, string key)
        {
            string stringResult = (string)hashtable[key];
            return (stringResult == null) ? string.Empty : stringResult;
        }
    }
}

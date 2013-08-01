using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MindTouch.Tools.ConfluenceConverter.XMLRPC.Types;

namespace MindTouch.Tools.ConfluenceConverter.XMLRPC
{
    
    public class CFRpcExtensions
    {
         CFRpcClient rpcClient;


         public CFRpcExtensions(CFRpcClient rpcClient)
        {
            this.rpcClient = rpcClient;
        }

         public List<CFTeamLabels> GetSpaceTeamLables(String spaceKey)
        {

            return rpcClient.CallVectorCFTypeConfluenceMethodWithoutLogin<CFTeamLabels>("headshift.", "getTeamLabels", "CF_ERROR_RETRIEVING_TEAMLABELS", spaceKey);
        }

         public string GetTinyUrlForPageId(String pageId)
         {

             return rpcClient.CallVectorCFTypeConfluenceMethodWithoutLogin("headshift.", "getTinyUrlForPageId", "CF_ERROR_RETRIEVING_TEAMLABELS", pageId);
         }


    }
}

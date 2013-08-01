using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MindTouch.Tools.ConfluenceConverter.MacroConverter
{
    public class RssMacroConverter : MacroConverter
    {

        public override string MacroName
        {
            get { return "rss"; }
        }

        public override bool IsBodyMacro
        {
            get { return false; }
        }

        public override string ConvertMacro(Dictionary<string, string> pathMap, Macro macro, ACConverterPageInfo pageInfo)
        {
            int max = Utils.MAXRESULTS;
            bool result = false;
            string dekiMacro = "";

            if (macro != null)
            {
                if (macro.Arguments.Keys.Contains("max"))
                {
                    result = int.TryParse(macro.Arguments["max"], out max);
                }

                //string treePath = Utils.GetTopLevelPage(pageInfo.DekiPagePath);

                if (macro.Arguments.Keys.Contains("url"))
                {
                    if (result)
                    {
                        // There was a max
                        dekiMacro = "{{ feed.list(\"" + macro.Arguments["url"] + "\", " + max.ToString() + ") }}";
                    }
                    else
                    {
                        // Default to 10
                        dekiMacro = "{{ feed.list(\"" + macro.Arguments["url"] + "\", 10) }}";
                    }
                }
            }

            return dekiMacro;
        }
    }
}

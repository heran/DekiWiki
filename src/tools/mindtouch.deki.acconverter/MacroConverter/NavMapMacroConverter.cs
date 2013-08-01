using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MindTouch.Tools.ConfluenceConverter.MacroConverter
{
    public class NavMapMacroConverter : MacroConverter
    {
        public override string MacroName
        {
            get { return "navmap"; }
        }

        public override bool IsBodyMacro
        {
            get { return false; }
        }

        public override string ConvertMacro(Dictionary<string, string> pathMap, Macro macro, ACConverterPageInfo pageInfo)
        {
            String label = String.Empty;
            String wrapAfter = String.Empty;
            String cellWidth = String.Empty;
            String cellHeight = String.Empty;

            if (macro.Arguments != null)
            {
                
                if (macro.Arguments.Keys.Contains("wrapAfter"))
                {
                    wrapAfter = ", columns:" + macro.Arguments["wrapAfter"].ToString();
                }
                
                ////////////////////
                
                if (macro.Arguments.Keys.Contains("cellWidth"))
                {
                    cellWidth = ", width:" + macro.Arguments["cellWidth"].ToString();
                }

                ////////////////////
                
                if (macro.Arguments.Keys.Contains("cellHeight"))
                {
                    cellHeight = ", height:" + macro.Arguments["cellHeight"].ToString();
                }

                /////////////////// SINGLE LABEL EXPECTED WITH THIS ARGUMENT

                if (macro.Arguments.Keys.Contains(Utils.DefaultParamName))
                {
                    label = macro.Arguments[Utils.DefaultParamName].ToString();
                }
            }
                       
            
            string dekiMacro = "{{ navMap{ tag: \"" + label + wrapAfter + cellWidth + cellHeight + "\" } }}";

            return dekiMacro;
        }
    }
}

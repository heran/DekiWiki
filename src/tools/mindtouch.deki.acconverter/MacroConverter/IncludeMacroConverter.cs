using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MindTouch.Tools.ConfluenceConverter;

namespace MindTouch.Tools.ConfluenceConverter.MacroConverter
{
    public class IncludeMacroConverter : MacroConverter
    {

        public override string MacroName
        {
            get { return "include"; }
        }

        public override bool IsBodyMacro
        {
            get { return false; }
        }

        public override string ConvertMacro(Dictionary<string, string> pathMap, Macro macro, ACConverterPageInfo pageInfo)
        {
            string dekiMacro = "";//"{{ wiki.page(" + objMacro + ") }}";
            if (macro.Arguments != null)
            {
                string spaceKey = (macro.Arguments.Keys.Contains("spaceKey")) ? macro.Arguments["spaceKey"] : pageInfo.ConfluencePage.space;
                string pageTitle = (macro.Arguments.Keys.Contains("pageTitle")) ? macro.Arguments["pageTitle"] : macro.Arguments[Utils.DefaultParamName];

                ACConverter aconverter = new ACConverter();
                dekiMacro = aconverter.GetMtPathFromConfluencePath(pathMap, spaceKey, pageTitle);

                dekiMacro = "{{ wiki.page(\"" + dekiMacro + "\") }}";
            }

            return dekiMacro;
        }
    }
}

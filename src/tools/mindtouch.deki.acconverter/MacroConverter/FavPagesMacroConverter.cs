using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MindTouch.Tools.ConfluenceConverter.MacroConverter
{
    public class FavPagesMacroConverter : MacroConverter
    {

        public override string MacroName
        {
            get { return "favpages"; }
        }

        public override bool IsBodyMacro
        {
            get { return false; }
        }

        public override string ConvertMacro(Dictionary<string, string> pathMap, Macro macro, ACConverterPageInfo pageInfo)
        {
            string dekiMacro = "{{favPages()}}";
            return dekiMacro;
        }
    }
}

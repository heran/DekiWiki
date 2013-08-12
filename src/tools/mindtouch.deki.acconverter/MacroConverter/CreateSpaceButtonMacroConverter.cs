using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MindTouch.Tools.ConfluenceConverter.MacroConverter
{
    public class CreateSpaceButtonMacroConverter : MacroConverter
    {

        public override string MacroName
        {
            get { return "create-space-button"; }
        }

        public override bool IsBodyMacro
        {
            get { return false; }
        }

        public override string ConvertMacro(Dictionary<string, string> pathMap, Macro macro, ACConverterPageInfo pageInfo)
        {
            string dekiMacro = "{{wiki.create()}}";
            return dekiMacro;
        }
    }
}

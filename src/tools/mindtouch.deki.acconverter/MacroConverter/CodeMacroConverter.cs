using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MindTouch.Tools.ConfluenceConverter.MacroConverter
{
    class CodeMacroConverter : MacroConverter
    {
        public override string MacroName
        {
            get { return "code"; }
        }

        public override bool IsBodyMacro
        {
            get { return true; }
        }

        public override string ConvertMacro(Dictionary<string, string> pathMap, Macro macro, ACConverterPageInfo pageInfo)
        {
            if (macro.Arguments == null)
            {
                return "<pre>" + macro.Body + "</pre>";
            }
            else
            {
                if (macro.Arguments.Keys.Contains("default"))
                    return "<pre class=\"deki-transform\" function=\"syntax." + macro.Arguments["default"] + "\">" + macro.Body + "</pre>";
            }
            return "<pre>" + macro.Body + "</pre>";
            
            
        }
    }
}

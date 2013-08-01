using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MindTouch.Tools.ConfluenceConverter.MacroConverter
{
    class IndexMacroConverter : MacroConverter
    {

        public override string MacroName
        {
            get { return "index"; }
        }

        public override bool IsBodyMacro
        {
            get { return false; }
        }

        /// <summary>
        /// Takes a confluence macro (without the surrounding curly bracket), and converts it into dekiscript
        /// </summary>
        /// <param name="macro"></param>
        /// <param name="pageContext"></param>
        /// <returns></returns>
        public override string ConvertMacro(Dictionary<string, string> pathMap, Macro macro, ACConverterPageInfo pageInfo)
        {
            // index doesn't take any arguments, so ignore the macro argument

            // Get the path for the index we return
            string treePath = Utils.GetTopLevelPage(pageInfo.DekiPagePath);

            string dekiMacro = "{{ wiki.tree{path: \"" + treePath + "\"} }}";

            return dekiMacro;
        }
        
    }
}

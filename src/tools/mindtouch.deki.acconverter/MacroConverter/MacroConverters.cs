using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MindTouch.Tools.ConfluenceConverter.MacroConverter
{
    public class MacroConverters
    {
        List<MacroConverter> macroConverters = new List<MacroConverter>();

        public void AddMacroConverter(MacroConverter macroConverter)
        {
            macroConverters.Add(macroConverter);
        }

        internal string Convert(Dictionary<string, string> pathMap, string stubbedPageContent, ACConverterPageInfo pageInfo)
        {
            foreach (MacroConverter converter in macroConverters)
            {
                stubbedPageContent = converter.Convert(pathMap, stubbedPageContent, pageInfo);
            }
            return stubbedPageContent;
        }

        public IEnumerable<string> GetSupportedMacros()
        {
            return macroConverters.Select(mc => mc.MacroName);
        }
    }
}

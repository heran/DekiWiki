using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace MindTouch.Tools.ConfluenceConverter.MacroConverter
{
    public abstract class MacroConverter
    {
        public abstract string MacroName { get; }

        public abstract bool IsBodyMacro { get; }

        public abstract string ConvertMacro(Dictionary<string, string> pathMap,Macro macro, ACConverterPageInfo pageInfo);

        public string Convert(Dictionary<string, string> pathMap, string stubContent, ACConverterPageInfo pageInfo)
        {
            // Find all the macros called MacroName that are in stubContent, and call ConvertMacro

            string regexStubStart = ACConverter.MacroStubStart;
            string regexStubEnd = ACConverter.MacroStubEnd;

            string regexPattern = regexStubStart + MacroName + ":?[^" + ACConverter.MacroStubEnd + "]*" + regexStubEnd;

            string regexPatternClose = regexStubStart + MacroName + regexStubEnd;

            if (IsBodyMacro)
            {
                regexPattern = "(" + regexPattern + ")([^" + ACConverter.MacroStubStart + "]*)(" + regexPatternClose + ")";
            }

            Regex macroRegex = new Regex(regexPattern);

            MatchCollection matches = macroRegex.Matches(stubContent);

            foreach (Match match in matches)
            {
                Macro macro;

                if (IsBodyMacro)
                {
                    string macroHeader = match.Groups[1].Value;
                    string macroContent = match.Groups[2].Value;
                    string macroFooter = match.Groups[3].Value;
                    macro = new Macro(macroHeader, macroContent, macroFooter);
                }
                else
                {
                    macro = new Macro(match.Value);
                }

                string convertedMacro = ConvertMacro(pathMap, macro, pageInfo);
                stubContent = stubContent.Replace(match.Value, convertedMacro);
            }

            return stubContent;
        }

    }
}

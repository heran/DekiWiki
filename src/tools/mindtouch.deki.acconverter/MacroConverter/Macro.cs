using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace MindTouch.Tools.ConfluenceConverter.MacroConverter
{
    public class Macro
    {
        public string Name { get; set; }

        public string Body { get; set; }

        public bool HasBody { get; set; }

        public Dictionary<string, string> Arguments { get; set; }

        public string Original { get; set; }

        public Macro(string macro) : this(macro, false) { }

        /// <summary>
        /// Constructor to create a Macro object. Takes a full macro, including the stub indicators, and if it is a macro
        /// with a body, it will include the full content, and both the start and end macros
        /// </summary>
        /// <param name="rawMacro"></param>
        /// 
        // TODO - Need to add the ability to recognise comma separated parameters and return them appropriately

        public Macro(string macro, bool isBodyMacro)
        {
            Original = macro;
            HasBody = isBodyMacro;
            string macroHeader;
            if (!isBodyMacro)
            {
                macroHeader = macro;
                ParseMacroHeader(macroHeader);
            }
            else
            {
                //throw new NotImplementedException("Macros with a body are not currently supported");

                string regexStubStart = "((("; // ACConverter.MacroStubStart.Replace("(", @"\(");
                string regexStubEnd = ")))"; // ACConverter.MacroStubEnd.Replace(")", @"\)");

                int headStartndex = macro.IndexOf(regexStubStart);
                int headEndIndex = macro.IndexOf(regexStubEnd) + 3;
                macroHeader = macro.Substring(headStartndex, headEndIndex);
                // We are only interested in the first match
               if (macroHeader.Length > 0)
                {                    
                    ParseMacroHeader(macroHeader);
                    string macroClosing = "(((" + Name + ")))";
                    int BodyLength = macro.Length - (macroHeader.Length + macroClosing.Length);
                    Body = macro.Substring(macroHeader.Length, BodyLength);
                }                

            }
            
        }

        /// <param name="rawMacro"></param>
        public Macro(string macroHeader, string macroBody, string macroFooter)
        {
            Original = macroHeader + macroBody + macroFooter;
            Body = macroBody;
            HasBody = true;
            ParseMacroHeader(macroHeader);
        }

        /// <summary>
        /// Parses the macro header, determining the name and arguments for the macro
        /// </summary>
        /// <param name="macroHeader"></param>
        protected void ParseMacroHeader(string macroHeader)
        {
            //remove the macro delimiters if they exist
            string trimmedMacro = macroHeader.Trim('{').Trim('}');
            trimmedMacro = macroHeader.Trim('(').Trim(')');

            //Or the stub start / end
            trimmedMacro = trimmedMacro.Replace(ACConverter.MacroStubStart, "").Replace(ACConverter.MacroStubEnd, "");

            // Get the macro name
            int ParamSeparator = trimmedMacro.IndexOf(':');
            if (ParamSeparator > 0)
            {
                Arguments = new Dictionary<string, string>();

                Name = trimmedMacro.Substring(0, ParamSeparator);
                String Params = trimmedMacro.Substring(ParamSeparator + 1);

                // check for parameter separator
                if (Params.IndexOf('|') > 0)
                {
                    string[] parameters = Params.Split('|');
                    if (parameters.Length > 0)
                    {
                        foreach (string parameter in parameters)
                        {
                            char[] equalsChar = new char[] { '=' };
                            string[] ParamNameValue = parameter.Split(equalsChar, 2);
                            if (ParamNameValue.Length == 2)
                                Arguments.Add(ParamNameValue[0], ParamNameValue[1]);
                            else
                                CheckAndAddDefaultParam(parameter, Arguments);

                        }
                    }
                }
                else
                {                   

                    // this is just a default first parameter, which is generally spacekey and pagename or just pagename
                    CheckAndAddDefaultParam(Params, Arguments);
                }
            }
            else
            {
                Name = trimmedMacro;
            }
        }

        protected void CheckAndAddDefaultParam(string Params, Dictionary<string, string> Arguments)
        {
            if ((Params.Length > 0) && (!Params.Contains('|')) && (!Params.Contains('=')) && (!Params.Contains(':')))
            {
                Arguments.Add(Utils.DefaultParamName, Params);
            }
            else
            {
                if (Params.IndexOf('=') > 0)
                {
                   // string[] ParamNameValue = Params.Split('=');
                   // Arguments.Add(ParamNameValue[0], ParamNameValue[1]);

                    Arguments[Params.Substring(0, Params.IndexOf('='))] = Params.Substring(Params.IndexOf('=') + 1);
                }
                else
                {
                    if (Params.IndexOf(':') > 0)
                    {

                        Arguments["spaceKey"] = Params.Substring(0, Params.IndexOf(':'));
                        Arguments["pageTitle"] = Params.Substring(Params.IndexOf(':') + 1);
                    }
                    else
                    {
                        Arguments.Add(Utils.DefaultParamName, Params);
                    }
                }
                
            }
        }

    }
}

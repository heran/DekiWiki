using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MindTouch.Tools.ConfluenceConverter.MacroConverter
{
    public class ContentByLabelMacroConverter : MacroConverter
    {

        public override string MacroName
        {
            get { return "contentbylabel"; }
        }

        public override bool IsBodyMacro
        {
            get { return false; }
        }

        public override string ConvertMacro(Dictionary<string, string> pathMap, Macro macro, ACConverterPageInfo pageInfo)
        {
            // Go through and gather all possible arguments - insert them into variables

            /*
             * Type - (optional) search for types of content. Accepted values:             
                * page: basic pages
                * comment: comments on pages or blogs
                * blogpost/news: blog posts
                * attachment: attachments to pages or blogs
                * userinfo: personal information
                * spacedesc: space descriptions
                * personalspacedesc: personal space descriptions
                * mail: emails in a space 
             */
            // TODO - Some of these will be multiples that are comma separated, so a function will be needed in the superclass
            // that can split them and remove spaces at either end, leaving just the argument
            // TODO - if we are doing multiple parameters for an argument - also need a way to know that an argument has multiples
            String label = String.Empty;

            if (macro.Arguments != null)
            {

                String type = "page";
                if (macro.Arguments.Keys.Contains("type"))
                {
                    type = macro.Arguments["type"].ToString();
                }

                ////////////////////
                int max = 5;
                if (macro.Arguments.Keys.Contains("max"))
                {
                    bool resMax = int.TryParse(macro.Arguments["max"], out max);
                }
                if (macro.Arguments.Keys.Contains("maxResults"))
                {
                    bool resMaxResults = int.TryParse(macro.Arguments["maxResults"], out max);
                }

                ////////////////////
                String spaces = "spaces";
                if (macro.Arguments.Keys.Contains("spaces"))
                {
                    spaces = macro.Arguments["spaces"].ToString();
                }

                ////////////////////
                String space = "space";
                if (macro.Arguments.Keys.Contains("space"))
                {
                    space = macro.Arguments["space"].ToString();
                }

                /////////////////// SINGLE LABEL EXPECTED WITH THIS ARGUMENT
                
                if (macro.Arguments.Keys.Contains("label"))
                {
                    label = macro.Arguments["label"].ToString();
                }

                if (macro.Arguments.Keys.Contains("labels"))
                {
                    label = macro.Arguments["labels"].ToString();
                }

                /////////////////// WE ARE NOT SUPPORTING MULTIPLE LABELS AT PRESENT
                /*
                String labels = "labels";
                if (macro.Arguments.Keys.Contains("labels"))
                {
                    labels = macro.Arguments["labels"].ToString();
                }
                */

                ///////////////////
                String sort = "creation";
                if (macro.Arguments.Keys.Contains("sort"))
                {
                    sort = macro.Arguments["sort"].ToString();
                }
            }

            // TODO - need to know the exact DekiScript to be able to create this macro
            string dekiMacro = "{{ taggedPages{ tag: \"" + label + "\" } }}";

            return dekiMacro;
        }
    }
}



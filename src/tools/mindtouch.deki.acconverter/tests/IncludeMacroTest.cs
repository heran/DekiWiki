using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using MindTouch.Tools.ConfluenceConverter.MacroConverter;
using NUnit.Framework.SyntaxHelpers;

namespace MindTouch.Tools.ConfluenceConverter.tests
{
    [TestFixture]
    public class IncludeMacroTest
    {
        IncludeMacroConverter _includeConverter;
        Dictionary<string, string> pathMap;
        Macro macro;
        string ConfluenceMacroText = "{include:spaceKey=ds|pageTitle=Email archiving}";
        [SetUp]
        public void Init()
        {
            pathMap = new Dictionary<string, string>();
            pathMap.Add("/display/ds/Breadcrumb+demonstration", "demo/ds/Confluence_Overview/Creating_pages_and_linking/Breadcrumb_demonstration");
            pathMap.Add("/display/ds/Creating+pages+and+linking", "demo/ds/Confluence_Overview/Creating_pages_and_linking");
            pathMap.Add("/display/ds/Email+archiving", "demo/ds/Confluence_Overview/Email_archiving");
            pathMap.Add("/display/ds/Example+Index", "demo/ds/Confluence_Overview/Example_Index");
            macro = new Macro(ConfluenceMacroText);

            _includeConverter = new IncludeMacroConverter();
        }

        [Test]
        public void Convert()
        {       
            string convertedMacro = _includeConverter.ConvertMacro(pathMap, macro, null);
            Assert.That(convertedMacro, Is.EqualTo("{{ wiki.page(\"demo/ds/Confluence_Overview/Email_archiving\") }}"));
        }

    }
}

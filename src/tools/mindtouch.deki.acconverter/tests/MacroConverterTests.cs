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
    public class MacroConverterTests
    {
        [Test]
        public void MacroConvertersTest()
        {
            // Test that the MacroConverters collection correctly operates with a macro converter

            MacroConverters converters = new MacroConverters();
            converters.AddMacroConverter(new RssMacroConverter());
            ACConverter aconverter = new ACConverter();
            Dictionary<string, string> pathMap = aconverter.ReadUrlsFromAllManifests();
            string result = converters.Convert(pathMap, "some page content (((rss:url=http://feeds.feedburner.com/Mashable?format=xml|max=3|showTitlesOnly=true))) more page content", null);

            Assert.That(result, Is.EqualTo("some page content {{ feed.list(\"http://feeds.feedburner.com/Mashable?format=xml\", 3) }} more page content"));
        }
    }
}

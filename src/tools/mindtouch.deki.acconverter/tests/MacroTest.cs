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
    public class MacroTest
    {
        
        [Test]
        public void CreateMacroTest()
        {
            string testMacro = "{include:spaceKey=FOO|pageTitle=Home}";
            Macro objMacro = new Macro(testMacro);
            Assert.AreEqual("include", objMacro.Name);
            Assert.AreEqual("FOO", objMacro.Arguments["spaceKey"]);
            Assert.AreEqual("Home", objMacro.Arguments["pageTitle"]);

            testMacro = ACConverter.MacroStubStart + "include:spaceKey=FOO|pageTitle=Home" + ACConverter.MacroStubEnd;
            objMacro = new Macro(testMacro);
            Assert.AreEqual("include", objMacro.Name);
            Assert.AreEqual("FOO", objMacro.Arguments["spaceKey"]);
            Assert.AreEqual("Home", objMacro.Arguments["pageTitle"]);

            Macro testMacroWithEqualsInArg = new Macro("{rss:url=http://feeds.feedburner.com/Mashable?format=xml|max=3|showTitlesOnly=true}");
            Assert.That(testMacroWithEqualsInArg.Arguments["url"], Is.EqualTo("http://feeds.feedburner.com/Mashable?format=xml"));
        }

        [Test]
        public void CreateBodyMacroTest()
        {
            string codeMacro = "(((code:xml)))Console.WriteLine(\"This is some code\");(((code)))";
            Macro testMacro = new Macro(codeMacro, true);
            Assert.That(testMacro.Name, Is.EqualTo("code"));
            Assert.That(testMacro.HasBody);
            Assert.That(testMacro.Body, Is.EqualTo("Console.WriteLine(\"This is some code\");"));
            Assert.That(testMacro.Original, Is.EqualTo(codeMacro));

            //TODO - add test for a body macro with arguments.
        }


        [Test]
        public void RssMacroTest()
        {
            RssMacroConverter rssConverter = new RssMacroConverter();

            Macro testMacro = new Macro("{rss:url=http://feeds.feedburner.com/Mashable?format=xml|max=3|showTitlesOnly=true}");
            string convertedMacro = rssConverter.ConvertMacro(null,testMacro, null);
            Assert.That(convertedMacro, Is.EqualTo("{{ feed.list(\"http://feeds.feedburner.com/Mashable?format=xml\", 3) }}"));

            // Check it works when no max property is set
            testMacro = new Macro("{rss:url=http://feeds.feedburner.com/Mashable?format=xml|showTitlesOnly=true}");
            convertedMacro = rssConverter.ConvertMacro(null,testMacro, null);
            Assert.That(convertedMacro, Is.EqualTo("{{ feed.list(\"http://feeds.feedburner.com/Mashable?format=xml\", 5) }}"));
        }

        [Test]
        public void ContributorsMacroTest()
        {
            ContributorsMacroConverter contribConverter = new ContributorsMacroConverter();
            Macro testMacro = new Macro("{contributors}");

            string convertedMacro = contribConverter.ConvertMacro(null, testMacro, null);
            Assert.That(convertedMacro, Is.EqualTo("{{wiki.contributors()}}"));
        }

        [Test]
        public void CreateSpaceButtonMacroTest()
        {
            CreateSpaceButtonMacroConverter buttonConverter = new CreateSpaceButtonMacroConverter();
            Macro testMacro = new Macro("{create-space-button}");
            string convertedMacro = buttonConverter.ConvertMacro(null, testMacro, null);
            Assert.That(convertedMacro, Is.EqualTo("{{wiki.create()}}"));
        }

        [Test]
        public void FavPagesMacroTest()
        {
            FavPagesMacroConverter converter = new FavPagesMacroConverter();
            Macro testMacro = new Macro("{favpages}");
            string convertedMacro = converter.ConvertMacro(null, testMacro, null);
            Assert.That(convertedMacro, Is.EqualTo("{{favPages();}}"));
        }

        [Test]
        public void ContentByLabelMacroTest()
        {
            ContentByLabelMacroConverter contentbylabelConverter = new ContentByLabelMacroConverter();
            Macro testMacro = new Macro("{contentbylabel:label=cool}");
            string convertedMacro = contentbylabelConverter.ConvertMacro(null, testMacro, null);
            Assert.That(convertedMacro, Is.EqualTo("{{ taggedPages{ tag: \"cool\" }; }}"));
        }

        [Test]
        public void CodeMacroTest()
        {
            CodeMacroConverter codeConverter = new CodeMacroConverter();
            
            Macro testMacro = new Macro("{code}Console.WriteLine(\"This is some code\");{code}");
            string convertedMacro = codeConverter.ConvertMacro(null,testMacro, null);
            Assert.That(convertedMacro, Is.EqualTo("<pre>Console.WriteLine(\"This is some code\");</pre>"));

        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using MindTouch.Tools.ConfluenceConverter.MacroConverter;
using MindTouch.Tools.ConfluenceConverter;

namespace MindTouch.Tools.ConfluenceConverter.tests
{
    [TestFixture]
    public class IndexMacroConverterTests
    {        
        IndexMacroConverter _indexConverter;
        string pagePath = "\"demospace%252flevel2%252flevel3%252fds%252fIndex\"";

        [SetUp]
        public void Init()
        {
            _indexConverter = new IndexMacroConverter();
        }

        [Test]
        public void GetTopLevelPageTest()
        {
            string topPage = Utils.GetTopLevelPage(pagePath);
            Assert.AreNotEqual(pagePath, topPage, "Not processed atall");
            Assert.IsTrue(!topPage.Contains("/"),"Top page still contains path rather than individual page");
        }

    }
}

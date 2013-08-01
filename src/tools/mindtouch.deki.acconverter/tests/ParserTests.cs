using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using MindTouch.Tools.ConfluenceConverter;

namespace MindTouch.Tools.ConfluenceConverter.tests
{
    [TestFixture]
    public class ParserTests
    {
        ACConverter _converter;

        [SetUp]
        public void Init()
        {
            _converter = new ACConverter();
        }

        [Test]
        public void GetValidTeamLabelTest()
        {
            string teamlabel = _converter.GetValidTeamLabel("ds");
            Assert.IsNotNull("Unable to obtain team label");
        }

    }
}

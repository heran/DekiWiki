using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace MindTouch.Tools.ConfluenceConverter.tests
{
    [TestFixture]
    public class ACConverterTests
    {
        ACConverter _converter;

        [SetUp]
        public void Init()
        {
            _converter = new ACConverter();
        }

        [Test]
        public void ConvertTest()
        {
            _converter.Convert(null, false, false, null, false, null);
        }

    }
}

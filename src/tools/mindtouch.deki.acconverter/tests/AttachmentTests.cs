using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using System.Collections.Specialized;
using System.ComponentModel;

namespace MindTouch.Tools.ConfluenceConverter.tests
{
    [TestFixture]
    public class AttachmentTests
    {
        string sUri = "http://confluencesite.com/download/attachments/3604492/excelspreadsheet.xls?version=1";
        ACConverter converter;
        long nConfluencePageID = 3604492;
        int nDekiPageId = 1234;

        [SetUp]
        public void Init()
        {
            converter = new ACConverter();
        }

        [Test]
        public void GetVersionFromUri()
        {
            int nVersion = converter.ParseVersionFromUri(sUri);
            Assert.AreEqual(1, nVersion,"Invalid return value");
        }       
        
    }
}

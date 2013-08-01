using System;
using System.Collections.Generic;
using System.Text;

using NUnit.Framework;
using MindTouch.Deki.Data;

namespace MindTouch.Deki.Tests {

    [TestFixture]
    public class DateTimeParsingTests {

        [Test]
        public void Parse_MediaWiki_timestamp() {
            DateTime now = UtcNowNoMillis;
            DateTime parsed = DbUtils.ToDateTime(now.ToString("yyyyMMddHHmmss"));
            Assert.AreEqual(now, parsed);
        }

        [Test]
        public void Parse_Xml_timestamp() {
            DateTime now = UtcNowNoMillis;
            DateTime parsed = DbUtils.ToDateTime(now.ToString("yyyy-MM-ddTHH:mm:ssZ"));
            Assert.AreEqual(now, parsed);
        }

        [Test]
        public void InverseTimestamp() {
            string original = "20091023";
            string inverse = DbUtils.InvertTimestamp(original);
            Assert.AreEqual("79908976", inverse);
        }

        static DateTime UtcNowNoMillis {
            get {
                return new DateTime((long)Math.Floor((double)DateTime.UtcNow.ToUniversalTime().Ticks / 10000000) * 10000000, DateTimeKind.Utc).ToUniversalTime();
            }
        }
    }
}

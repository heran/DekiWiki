/*
 * MindTouch Core - open source enterprise collaborative networking
 * Copyright (c) 2006-2010 MindTouch Inc.
 * www.mindtouch.com  oss@mindtouch.com
 *
 * For community documentation and downloads visit developer.mindtouch.com;
 * please review the licensing section.
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License along
 * with this program; if not, write to the Free Software Foundation, Inc.,
 * 59 Temple Place - Suite 330, Boston, MA 02111-1307, USA.
 * http://www.gnu.org/copyleft/gpl.html
 */
using MindTouch.Dream;
using NUnit.Framework;

namespace MindTouch.Deki.Tests.PageTests {
    [TestFixture]
    public class DekiScriptTests {

        private static readonly log4net.ILog _log = LogUtils.CreateLog();

        [Test]
        public void MT9092_WikiLocalize_respects_page_culture() {

            // Fixes: http://youtrack.developer.mindtouch.com/issue/MT-9092
            var p = Utils.BuildPlugForAdmin();

            // Create a page
            string id = null;
            var resource = "activation.activated";
            var content = string.Format("{{{{wiki.localize(\"{0}\")}}}}", resource);
            var title = PageUtils.GenerateUniquePageName();
            title = "=" + XUri.DoubleEncode(title);
            var msg = p.At("pages", title, "contents")
                .With("language", "fr-fr")
                .PostAsync(DreamMessage.Ok(MimeType.TEXT_UTF8, content))
                .Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Page creation failed!");
            id = msg.ToDocument()["page/@id"].AsText;
            _log.DebugFormat("page id: {0}", id);

            // get localized string & make sure we have localization for it
            msg = p.At("site", "localization").With("resource", resource).Get();
            var invariant = msg.ToText();
            msg = p.At("site", "localization").With("resource", resource).With("lang", "fr-fr").Get();
            var french = msg.ToText();
            Assert.AreNotEqual(invariant, french);

            // get french page
            msg = p.At("pages", id, "contents").With("format", "html").Get();
            Assert.AreEqual(french, msg.ToDocument()["body"].Contents);

            // Delete the page
            PageUtils.DeletePageByID(p, id, true);
        }
    }
}

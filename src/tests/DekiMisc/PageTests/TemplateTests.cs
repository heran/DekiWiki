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

using System;
using System.Text;

using NUnit.Framework;
using MindTouch.Tasking;
using MindTouch.Dream;
using MindTouch.Xml;
using MindTouch.Dream.Test;

namespace MindTouch.Deki.Tests.PageTests
{
    [TestFixture]
    public class TemplateTests
    {
        /// <summary>
        ///     Create a template with several subpages and import it to page
        /// </summary>        
        /// <feature>
        /// <name>GET:pages/{pageid}/subpages</name>
        /// <uri>http://developer.mindtouch.com/en/ref/MindTouch_API/GET%3apages%2f%2f%7Bpageid%7D%2f%2fsubpages</uri>
        /// </feature>
        /// <expected>Number of subpages for page refelects number of template subpages</expected>

        [Test]
        public void TemplateWithManyChildren()
        {
            StringBuilder pageContent = new StringBuilder("<p>TEMPLATE_</p><ul>");
            int countOfTemplates = Utils.Settings.CountOfRepeats;

            // Log in as ADMIN
            Plug p = Utils.BuildPlugForAdmin();

            // Create template page
            string templateid = null;
            string templatepath = null;
            DreamMessage msg = PageUtils.CreateRandomPage(p, "TEMPLATE:" + Utils.GenerateUniqueName(), out templateid, out templatepath);

            // Create countOfTemplates subpages for template.
            for (int i = 0; i < countOfTemplates; i++)
            {
                PageUtils.SavePage(p, templatepath + "/TEMPLATE_" + i, "Page content for " + i);
                pageContent.Append(string.Format("<li><a class=\"site\" href=\"/#\" template=\"{0}/TEMPLATE_{1}\">TEMPLATE_{1}</a></li>\n", templatepath, i));
            }
            pageContent.Append("</ul>");

            // Create a page and invoke template to page
            string pageid = null;
            string pagepath = null;
            msg = PageUtils.CreateRandomPage(p, out pageid, out pagepath);
            msg = PageUtils.SavePage(p, pagepath, pageContent.ToString());

            // Make sure template invoked correctly
            msg = p.At("pages", pageid, "subpages").Get();
            XDoc page = msg.ToDocument();
            Assert.IsTrue(page["page.subpage"].ListLength == countOfTemplates, "Number of subpages does not match generated number of templates!");
            for (int i = 0; i < countOfTemplates; i++)
                Assert.IsFalse(page["page.subpage[path='" + pagepath + "/TEMPLATE_" + i + "']"].IsEmpty, "Page doesn't contain one of the subpages defined in template!");

            // Clean up
            PageUtils.DeletePageByID(p, pageid, true);
            PageUtils.DeletePageByID(p, templateid, true);                                
        }

        /// <summary>
        ///     Create a template with a child tree and save the links to page using [] notation
        /// </summary>        
        /// <feature>
        /// <name>GET:pages/{pageid}/subpages</name>
        /// <uri>http://developer.mindtouch.com/en/ref/MindTouch_API/GET%3apages%2f%2f%7Bpageid%7D%2f%2fsubpages</uri>
        /// </feature>
        /// <assumption>child tree: /Temp_1/Temp_2/Temp_3/.../Temp_N</assumption>
        /// <expected>Links point to template child tree</expected>

        [Test]
        public void TemplateWithChildTree()
        {
            int countOfTemplates = Utils.Settings.CountOfRepeats;
            StringBuilder pageContent = new StringBuilder("<p>TEMPLATE_</p><ul>");
            StringBuilder currentTemplateName = new StringBuilder();

            // Log in as ADMIN
            Plug p = Utils.BuildPlugForAdmin();

            // Create template page
            string templateid = null;
            string templatepath = null;
            DreamMessage msg = PageUtils.CreateRandomPage(p, "TEMPLATE:" + Utils.GenerateUniqueName(), out templateid, out templatepath);

            // Create a template page countOfTemplates deep
            for (int i = 0; i < countOfTemplates; i++)
            {
                currentTemplateName.AppendFormat("/TEMPLATE_{0}", i);
                PageUtils.SavePage(p, templatepath + currentTemplateName.ToString(), "Page content for " + i);
                // when i use this then count of subpages is 0
                pageContent.AppendFormat("<li>[[{0}{1}|{1}]]</li>", templatepath, currentTemplateName);
                // when i use this then count of outbound links is 0
                //pageContent.Append(String.Format("<li><a class=\"site\" href=\"/#\" template=\"{0}{1}\">{1}</a></li>\n", templatepath, currentTemplateName));
            }
            pageContent.Append("</ul>");

            // Create a page and invoke template to page
            string pageid = null;
            string pagepath = null;
            msg = PageUtils.CreateRandomPage(p, out pageid, out pagepath);
            msg = PageUtils.SavePage(p, pagepath, pageContent.ToString());

            msg = PageUtils.GetPage(p, pagepath);
            Assert.IsTrue(msg.ToDocument()["outbound/page"].ListLength == countOfTemplates, "Number of outbound links does not match generated number of templates!");

            msg = p.At("pages", pageid, "subpages").Get();
            Assert.IsTrue(msg.ToDocument()["page.subpage"].ListLength == 0, "Page has subpages!");

            PageUtils.DeletePageByID(p, pageid, true);
            PageUtils.DeletePageByID(p, templateid, true);
        }

        /// <summary>
        ///     Create a template with a child tree and save the links to page using HTML tagging
        /// </summary>        
        /// <feature>
        /// <name>GET:pages/{pageid}/subpages</name>
        /// <uri>http://developer.mindtouch.com/en/ref/MindTouch_API/GET%3apages%2f%2f%7Bpageid%7D%2f%2fsubpages</uri>
        /// </feature>
        /// <assumption>child tree: /Temp_1/Temp_2/Temp_3/.../Temp_N</assumption>
        /// <expected>Child tree to descend from page that imported template</expected>

        [Test]
        public void TemplateWithChildTreeThroughATag()
        {
            int countOfTemplates = Utils.Settings.CountOfRepeats;
            StringBuilder pageContent = new StringBuilder("<p>TEMPLATE_</p><ul>");
            StringBuilder currentTemplateName = new StringBuilder();
            
            // Log in as ADMIN
            Plug p = Utils.BuildPlugForAdmin();

            // Create template page
            string templateid = null;
            string templatepath = null;
            DreamMessage msg = PageUtils.CreateRandomPage(p, "TEMPLATE:" + Utils.GenerateUniqueName(), out templateid, out templatepath);

            // Create template with countOfTemplates pages deep
            for (int i = 0; i < countOfTemplates; i++)
            {
                currentTemplateName.AppendFormat("/TEMPLATE_{0}", i);
                PageUtils.SavePage(p, templatepath + currentTemplateName.ToString(), "Page content for " + i);
                // when i use this then count of subpages is 0
                //pageContent.AppendFormat("<li>[[{0}{1}|{1}]]</li>", templatepath, currentTemplateName);
                // when i use this then count of outbound links is 0
                pageContent.Append(String.Format("<li><a class=\"site\" href=\"/#\" template=\"{0}{1}\">{1}</a></li>\n", templatepath, currentTemplateName));
            }
            pageContent.Append("</ul>");

            // Create page and invoke template
            string pageid = null;
            string pagepath = null;
            msg = PageUtils.CreateRandomPage(p, out pageid, out pagepath);
            msg = PageUtils.SavePage(p, pagepath, pageContent.ToString());

            // Check template invoked correctly
            msg = PageUtils.GetPage(p, pagepath);
            Assert.IsTrue(msg.ToDocument()["outbound/page"].ListLength == countOfTemplates, "Number of outbound links does not match generated number of templates!");

            msg = p.At("pages", pageid, "subpages").Get();
            Assert.IsTrue(msg.ToDocument()["page.subpage"].ListLength == 1, "Page does not have exactly 1 subpage!");

            // Clean up
            PageUtils.DeletePageByID(p, pageid, true);
            PageUtils.DeletePageByID(p, templateid, true);
        }

        [Test]
        public void InvokeADMINTemplateWithUnsafeContent() {
            // This test contains two parts:
            // 1. Invoke template (created by admin) by ADMIN
            // 2. Invoke template by user without UNSAFECONTENT permissions
            //
            // Expected: All content (unsafe included) is present

            // Log in as ADMIN
            Plug p = Utils.BuildPlugForAdmin();

            // Create a template with unsafe content
            string safe_content = "<p>This is a template</p>";
            string unsafe_content = "<p><script type=\"text/javascript\">document.write(\"With unsafe content\");</script></p>";
            string template_content = safe_content + unsafe_content;
            string template_name = "test" + DateTime.Now.Ticks.ToString();
            string template_path = "Template:" + template_name;
            DreamMessage msg = p.At("pages", "=" + XUri.DoubleEncode(template_path), "contents")
                .Post(DreamMessage.Ok(MimeType.TEXT_UTF8, template_content), new Result<DreamMessage>()).Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Template page creation failed!");

            // script contents are injected with CDATA sections, so retrieve contents with injection
            msg = p.At("pages", "=" + XUri.DoubleEncode(template_path), "contents").Get(new Result<DreamMessage>()).Wait();
            template_content = msg.ToDocument()["body"].AsText ?? String.Empty;

            // There are 3 different dekiscript methods to invoke templates
            string[] template_call = new string[] { "<pre class=\"script\">Template('" + template_name + "');</pre>",
                                                    "<pre class=\"script\">Template." + template_name + "();</pre>",
                                                    "<pre class=\"script\">wiki.Template('" + template_name + "');</pre>" };

            // Create page that calls template
            string page_id;
            string page_path;
            PageUtils.CreateRandomPage(p, out page_id, out page_path);

            for (int i = 0; i < template_call.Length; i++) {
                // Use template_call[i] as page contents
                msg = p.At("pages", "=" + XUri.DoubleEncode(page_path), "contents")
                          .With("edittime", String.Format("{0:yyyyMMddHHmmss}", DateTime.Now))
                          .Post(DreamMessage.Ok(MimeType.TEXT_UTF8, template_call[i]), new Result<DreamMessage>()).Wait();

                // Retrieve page contents and verify it matches _all_ template content
                msg = p.At("pages", "=" + XUri.DoubleEncode(page_path), "contents").Get(new Result<DreamMessage>()).Wait();
                Assert.AreEqual(template_content, msg.ToDocument()["body"].AsText ?? String.Empty, "Unexpected contents");
            }

            // Part 2: Invoke template as user without USC permissions
            string userid;
            string username;
            msg = UserUtils.CreateRandomContributor(p, out userid, out username);
            p = Utils.BuildPlugForUser(username, "password");

            // Check that user does not have USC permissions
            Assert.IsFalse((msg.ToDocument()["permissions.effective/operations"].AsText ?? "UNSAFECONTENT").Contains("UNSAFECONTENT"), "Created user has UNSAFECONTENT permissions");

            for (int i = 0; i < template_call.Length; i++) {
                // Use template_call[i] as page contents
                msg = p.At("pages", "=" + XUri.DoubleEncode(page_path), "contents")
                          .With("edittime", String.Format("{0:yyyyMMddHHmmss}", DateTime.Now))
                          .Post(DreamMessage.Ok(MimeType.TEXT_UTF8, template_call[i]), new Result<DreamMessage>()).Wait();

                // Retrieve page contents and verify it matches _all_ template content
                msg = p.At("pages", "=" + XUri.DoubleEncode(page_path), "contents").Get(new Result<DreamMessage>()).Wait();
                Assert.AreEqual(template_content, msg.ToDocument()["body"].AsText ?? String.Empty, "Unexpected contents");
            }

            // Clean up
            PageUtils.DeletePageByName(p, template_path, true);
        }

        [Test]
        public void InvokeNoUSCTemplateWithUnsafeContent() {
            // Create a contributor
            string userid;
            string username;
            DreamMessage msg = UserUtils.CreateRandomContributor(Utils.BuildPlugForAdmin(), out userid, out username);
            Plug p = Utils.BuildPlugForUser(username, "password");

            // Check that user does not have USC permissions
            Assert.IsFalse((msg.ToDocument()["permissions.effective/operations"].AsText ?? "UNSAFECONTENT").Contains("UNSAFECONTENT"), "Created user has UNSAFECONTENT permissions");

            // Create a template with unsafe content
            string safe_content = "This is a template";
            string unsafe_content = "<p><script type=\"text/javascript\">document.write(\"With unsafe content\");</script></p>";
            string template_content = safe_content + unsafe_content;
            string template_name = "test" + DateTime.Now.Ticks.ToString();
            string template_path = "Template:" + template_name;
            msg = p.At("pages", "=" + XUri.DoubleEncode(template_path), "contents")
                .Post(DreamMessage.Ok(MimeType.TEXT_UTF8, template_content), new Result<DreamMessage>()).Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Template page creation failed!");

            // Log in as ADMIN
            p = Utils.BuildPlugForAdmin();

            // There are 3 different dekiscript methods to invoke templates
            string[] template_call = new string[] { "<pre class=\"script\">Template('" + template_name + "');</pre>",
                                                    "<pre class=\"script\">Template." + template_name + "();</pre>",
                                                    "<pre class=\"script\">wiki.Template('" + template_name + "');</pre>" };

            // Create page that calls template
            string page_id;
            string page_path;
            PageUtils.CreateRandomPage(p, out page_id, out page_path);

            for (int i = 0; i < template_call.Length; i++) {
                // Use template_call[i] as page contents
                msg = p.At("pages", "=" + XUri.DoubleEncode(page_path), "contents")
                          .With("edittime", String.Format("{0:yyyyMMddHHmmss}", DateTime.Now))
                          .Post(DreamMessage.Ok(MimeType.TEXT_UTF8, template_call[i]), new Result<DreamMessage>()).Wait();

                // Retrieve page contents. Expected only safe content present
                msg = p.At("pages", "=" + XUri.DoubleEncode(page_path), "contents").Get(new Result<DreamMessage>()).Wait();
                Assert.AreEqual(safe_content, msg.ToDocument()["body"].AsText ?? String.Empty, "Unexpected contents");
            }

            // Clean up
            PageUtils.DeletePageByName(p, template_path, true);
        }
    }
}

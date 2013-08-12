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
using NUnit.Framework;

namespace MindTouch.Deki.Tests {

    [TestFixture]
    public class TitleTests {

        #region WithUserFriendlyName
        [Test]
        public void WithUserFriendlyName_child_subpage_of_homepage() {
            Title title = Title.FromPrefixedDbPath("", null);
            title = title.WithUserFriendlyName("child");

            Assert.AreEqual(NS.MAIN, title.Namespace);
            Assert.AreEqual("child", title.AsUnprefixedDbPath());
        }

        [Test]
        public void WithUserFriendlyName_spaces_in_child_subpage_of_homepage() {
            Title title = Title.FromPrefixedDbPath("", null);
            title = title.WithUserFriendlyName("sister brother");

            Assert.AreEqual(NS.MAIN, title.Namespace);
            Assert.AreEqual("sister_brother", title.AsUnprefixedDbPath());
        }

        [Test]
        public void WithUserFriendlyName_child_subpage_of_parent_page() {
            Title title = Title.FromPrefixedDbPath("parent", null);
            title = title.WithUserFriendlyName("child");

            Assert.AreEqual(NS.MAIN, title.Namespace);
            Assert.AreEqual("parent/child", title.AsUnprefixedDbPath());
        }

        [Test]
        public void WithUserFriendlyName_spaces_in_child_subpage_of_parent_page() {
            Title title = Title.FromPrefixedDbPath("parent", null);
            title = title.WithUserFriendlyName("sister brother");

            Assert.AreEqual(NS.MAIN, title.Namespace);
            Assert.AreEqual("parent/sister_brother", title.AsUnprefixedDbPath());
        }

        [Test]
        public void WithUserFriendlyName_user_prefixed_subpage_of_parent_page() {
            Title title = Title.FromPrefixedDbPath("parent", null);
            title = title.WithUserFriendlyName("user:child");

            Assert.AreEqual(NS.MAIN, title.Namespace);
            Assert.AreEqual("parent/user:child", title.AsUnprefixedDbPath());
        }

        [Test]
        public void WithUserFriendlyName_user_prefixed_subpage_of_homepage() {
            Title title = Title.FromPrefixedDbPath("", null);
            title = title.WithUserFriendlyName("user:child");

            Assert.AreEqual(NS.USER, title.Namespace);
            Assert.AreEqual("child", title.AsUnprefixedDbPath());
        }

        [Test]
        public void WithUserFriendlyName_slash_in_chid_page_of_parent_page() {
            Title title = Title.FromPrefixedDbPath("parent", null);
            title = title.WithUserFriendlyName("brother/sister");

            Assert.AreEqual(NS.MAIN, title.Namespace);
            Assert.AreEqual("parent/brother//sister", title.AsUnprefixedDbPath());
        }

        [Test]
        public void WithUserFriendlyName_trailing_slash_in_child_page_of_parent_page() {
            Title title = Title.FromPrefixedDbPath("parent", null);
            title = title.WithUserFriendlyName("child/");

            Assert.AreEqual(NS.MAIN, title.Namespace);
            Assert.AreEqual("parent/child", title.AsUnprefixedDbPath());
        }

        [Test]
        [ExpectedException(ExceptionType = typeof(ArgumentException), ExpectedMessage = "resulting title object is invalid: parent///child\r\nParameter name: name")]
        public void WithUserFriendlyName_leading_slash_in_child_page_of_parent_page() {
            Title title = Title.FromPrefixedDbPath("parent", null);
            title = title.WithUserFriendlyName("/child");

            Assert.AreEqual(NS.MAIN, title.Namespace);
            Assert.AreEqual("parent/child", title.AsUnprefixedDbPath());
        }

        [Test]
        public void WithUserFriendlyName_unknown_prefixed_child_page_of_homepage() {
            Title title = Title.FromPrefixedDbPath("", null);
            title = title.WithUserFriendlyName("unknown:child");

            Assert.AreEqual(NS.MAIN, title.Namespace);
            Assert.AreEqual("unknown:child", title.AsUnprefixedDbPath());
        }

        [Test]
        public void WithUserFriendlyName_double_slash_in_child_page_of_homepage() {
            Title title = Title.FromPrefixedDbPath("", null);
            title = title.WithUserFriendlyName("brother//sister");

            Assert.AreEqual(NS.MAIN, title.Namespace);
            Assert.AreEqual("brother////sister", title.AsUnprefixedDbPath());
        }

        [Test]
        public void WithUserFriendlyName_leading_underscore_child_page_of_homepage() {
            Title title = Title.FromPrefixedDbPath("", null);
            title = title.WithUserFriendlyName("_child");

            Assert.AreEqual(NS.MAIN, title.Namespace);
            Assert.AreEqual("child", title.AsUnprefixedDbPath());
        }

        [Test]
        public void WithUserFriendlyName_trailing_underscore_child_page_of_homepage() {
            Title title = Title.FromPrefixedDbPath("", null);
            title = title.WithUserFriendlyName("child_");

            Assert.AreEqual(NS.MAIN, title.Namespace);
            Assert.AreEqual("child", title.AsUnprefixedDbPath());
        }

        [Test]
        public void WithUserFriendlyName_leading_underscore_child_page_of_parent_page() {
            Title title = Title.FromPrefixedDbPath("parent", null);
            title = title.WithUserFriendlyName("_child");

            Assert.AreEqual(NS.MAIN, title.Namespace);
            Assert.AreEqual("parent/child", title.AsUnprefixedDbPath());
        }

        [Test]
        public void WithUserFriendlyName_trailing_underscore_child_page_of_parent_page() {
            Title title = Title.FromPrefixedDbPath("parent", null);
            title = title.WithUserFriendlyName("child_");

            Assert.AreEqual(NS.MAIN, title.Namespace);
            Assert.AreEqual("parent/child", title.AsUnprefixedDbPath());
        }

        [Test]
        public void WithUserFriendlyName_leading_space_child_page_of_homepage() {
            Title title = Title.FromPrefixedDbPath("", null);
            title = title.WithUserFriendlyName(" child");

            Assert.AreEqual(NS.MAIN, title.Namespace);
            Assert.AreEqual("child", title.AsUnprefixedDbPath());
        }

        [Test]
        public void WithUserFriendlyName_trailing_space_child_page_of_homepage() {
            Title title = Title.FromPrefixedDbPath("", null);
            title = title.WithUserFriendlyName("child ");

            Assert.AreEqual(NS.MAIN, title.Namespace);
            Assert.AreEqual("child", title.AsUnprefixedDbPath());
        }

        [Test]
        public void WithUserFriendlyName_leading_space_child_page_of_parent_page() {
            Title title = Title.FromPrefixedDbPath("parent", null);
            title = title.WithUserFriendlyName(" child");

            Assert.AreEqual(NS.MAIN, title.Namespace);
            Assert.AreEqual("parent/child", title.AsUnprefixedDbPath());
        }

        [Test]
        public void WithUserFriendlyName_trailing_space_child_page_of_parent_page() {
            Title title = Title.FromPrefixedDbPath("parent", null);
            title = title.WithUserFriendlyName("child ");

            Assert.AreEqual(NS.MAIN, title.Namespace);
            Assert.AreEqual("parent/child", title.AsUnprefixedDbPath());
        }
        #endregion

        #region FromUIUsername
        [Test]
        public void FromUIUsername_simple_name() {
            Title title = Title.FromUIUsername("bob");

            Assert.AreEqual(NS.USER, title.Namespace);
            Assert.AreEqual("bob", title.AsUnprefixedDbPath());
        }

        [Test]
        public void FromUIUsername_name_with_slash() {
            Title title = Title.FromUIUsername("bob/alex");

            Assert.AreEqual(NS.USER, title.Namespace);
            Assert.AreEqual("bob//alex", title.AsUnprefixedDbPath());
        }

        [Test]
        public void FromUIUsername_name_with_talk_namespace() {
            Title title = Title.FromUIUsername("talk:bob");

            Assert.AreEqual(NS.USER, title.Namespace);
            Assert.AreEqual("talk:bob", title.AsUnprefixedDbPath());
        }

        [Test]
        public void FromUIUsername_name_with_spaces() {
            Title title = Title.FromUIUsername("talk:john doe");

            Assert.AreEqual(NS.USER, title.Namespace);
            Assert.AreEqual("talk:john_doe", title.AsUnprefixedDbPath());
        }

        [Test]
        [ExpectedException(ExceptionType = typeof(ArgumentException), ExpectedMessage = "username is empty\r\nParameter name: username")]
        public void FromUIUsername_missing_name() {
            Title title = Title.FromUIUsername("");

            Assert.AreEqual(NS.USER, title.Namespace);
            Assert.AreEqual("", title.AsUnprefixedDbPath());
        }
        #endregion

        #region GetParent
        [Test]
        public void GetParent_UserNamespace() {
            Title title = Title.FromPrefixedDbPath("user:foo/bar", null);

            Assert.AreEqual(Title.FromPrefixedDbPath("user:foo", null), title.GetParent());
            Assert.AreEqual(Title.FromPrefixedDbPath("", null), title.GetParent().GetParent());
        }

        [Test]
        public void GetParent_TemplateNamespace() {
            Title title = Title.FromPrefixedDbPath("template:foo/bar", null);

            Assert.AreEqual(Title.FromPrefixedDbPath("template:foo", null), title.GetParent());
            Assert.AreEqual(Title.FromPrefixedDbPath("", null), title.GetParent().GetParent());
        }

        [Test]
        public void GetParent_SpecialNamespace() {
            Title title = Title.FromPrefixedDbPath("special:foo/bar", null);

            Assert.AreEqual(Title.FromPrefixedDbPath("special:foo", null), title.GetParent());
            Assert.AreEqual(Title.FromPrefixedDbPath("special:", null), title.GetParent().GetParent());
            Assert.AreEqual(Title.FromPrefixedDbPath("", null), title.GetParent().GetParent().GetParent());
        }

        [Test]
        [Ignore] // TODO (steveb): re-enable once Title bug relating to Help:/Project: pages is fixed
        public void GetParent_ProjectNamesapce() {
            Title title = Title.FromPrefixedDbPath("project:foo", null);

            Assert.AreEqual(Title.FromPrefixedDbPath("project:", null), title.GetParent());
            Assert.AreEqual(Title.FromPrefixedDbPath("", null), title.GetParent().GetParent());
        }

        [Test]
        [Ignore] // TODO (steveb): re-enable once Title bug relating to Help:/Project: pages is fixed
        public void GetParent_HelpNamesapce() {
            Title title = Title.FromPrefixedDbPath("help:foo", null);

            Assert.AreEqual(Title.FromPrefixedDbPath("help:", null), title.GetParent());
            Assert.AreEqual(Title.FromPrefixedDbPath("", null), title.GetParent().GetParent());
        }

        [Test]
        public void GetParent_MainNamesapce() {
            Title title = Title.FromPrefixedDbPath("main:foo", null);

            Assert.AreEqual(Title.FromPrefixedDbPath("", null), title.GetParent());
            Assert.AreEqual(null, title.GetParent().GetParent());
        }
        #endregion

        #region FromUriPath
        [Test]
        public void FromUriPath_page_subpage_of_homepage() {
            Title title = Title.FromPrefixedDbPath("", null);
            title = Title.FromUIUri(title, "page");

            Assert.AreEqual(NS.MAIN, title.Namespace);
            Assert.AreEqual("page", title.AsUnprefixedDbPath());
        }

        [Test]
        public void FromUriPath_page_subpage_of_parent_page() {
            Title title = Title.FromPrefixedDbPath("parent", null);
            title = Title.FromUIUri(title, "page");

            Assert.AreEqual(NS.MAIN, title.Namespace);
            Assert.AreEqual("page", title.AsUnprefixedDbPath());
        }

        [Test]
        public void FromUriPath_user_prefixed_subpage_of_parent_page() {
            Title title = Title.FromPrefixedDbPath("parent", null);
            title = Title.FromUIUri(title, "user:page");

            Assert.AreEqual(NS.USER, title.Namespace);
            Assert.AreEqual("page", title.AsUnprefixedDbPath());
        }

        [Test]
        public void FromUriPath_relative_subpage_of_homepage() {
            Title title = Title.FromPrefixedDbPath("", null);
            title = Title.FromUIUri(title, "./page");

            Assert.AreEqual(NS.MAIN, title.Namespace);
            Assert.AreEqual("page", title.AsUnprefixedDbPath());
        }

        [Test]
        public void FromUriPath_relative_subpage_of_parentpage() {
            Title title = Title.FromPrefixedDbPath("parent", null);
            title = Title.FromUIUri(title, "./page");

            Assert.AreEqual(NS.MAIN, title.Namespace);
            Assert.AreEqual("parent/page", title.AsUnprefixedDbPath());
        }

        [Test]
        public void FromUriPath_relative_subsubpage_of_parentpage() {
            Title title = Title.FromPrefixedDbPath("parent/subpage", null);
            title = Title.FromUIUri(title, "../page");

            Assert.AreEqual(NS.MAIN, title.Namespace);
            Assert.AreEqual("parent/page", title.AsUnprefixedDbPath());
        }
        #endregion

        #region AsFront

        [Test]
        public void AsFront_main() {
            Title title = Title.FromPrefixedDbPath("page", null);
            Assert.AreEqual("page", title.AsFront().AsPrefixedDbPath());
            Assert.AreEqual("Talk:page", title.AsTalk().AsPrefixedDbPath());
        }

        [Test]
        public void AsFront_main_talk() {
            Title title = Title.FromPrefixedDbPath("talk:page", null);
            Assert.AreEqual("page", title.AsFront().AsPrefixedDbPath());
            Assert.AreEqual("Talk:page", title.AsTalk().AsPrefixedDbPath());
        }

        [Test]
        public void AsFront_user() {
            Title title = Title.FromPrefixedDbPath("user:page", null);
            Assert.AreEqual("User:page", title.AsFront().AsPrefixedDbPath());
            Assert.AreEqual("User_talk:page", title.AsTalk().AsPrefixedDbPath());
        }

        [Test]
        public void AsFront_user_talk() {
            Title title = Title.FromPrefixedDbPath("user_talk:page", null);
            Assert.AreEqual("User:page", title.AsFront().AsPrefixedDbPath());
            Assert.AreEqual("User_talk:page", title.AsTalk().AsPrefixedDbPath());
        }

        [Test]
        public void AsFront_project() {
            Title title = Title.FromPrefixedDbPath("project:page", null);
            Assert.AreEqual("Project:page", title.AsFront().AsPrefixedDbPath());
            Assert.AreEqual("Project_talk:page", title.AsTalk().AsPrefixedDbPath());
        }

        [Test]
        public void AsFront_project_talk() {
            Title title = Title.FromPrefixedDbPath("project_talk:page", null);
            Assert.AreEqual("Project:page", title.AsFront().AsPrefixedDbPath());
            Assert.AreEqual("Project_talk:page", title.AsTalk().AsPrefixedDbPath());
        }

        [Test]
        public void AsFront_template() {
            Title title = Title.FromPrefixedDbPath("template:page", null);
            Assert.AreEqual("Template:page", title.AsFront().AsPrefixedDbPath());
            Assert.AreEqual("Template_talk:page", title.AsTalk().AsPrefixedDbPath());
        }

        [Test]
        public void AsFront_template_talk() {
            Title title = Title.FromPrefixedDbPath("template_talk:page", null);
            Assert.AreEqual("Template:page", title.AsFront().AsPrefixedDbPath());
            Assert.AreEqual("Template_talk:page", title.AsTalk().AsPrefixedDbPath());
        }

        [Test]
        public void AsFront_help() {
            Title title = Title.FromPrefixedDbPath("help:page", null);
            Assert.AreEqual("Help:page", title.AsFront().AsPrefixedDbPath());
            Assert.AreEqual("Help_talk:page", title.AsTalk().AsPrefixedDbPath());
        }

        [Test]
        public void AsFront_help_talk() {
            Title title = Title.FromPrefixedDbPath("help_talk:page", null);
            Assert.AreEqual("Help:page", title.AsFront().AsPrefixedDbPath());
            Assert.AreEqual("Help_talk:page", title.AsTalk().AsPrefixedDbPath());
        }

        [Test]
        public void AsFront_special() {
            Title title = Title.FromPrefixedDbPath("special:page", null);
            Assert.AreEqual("Special:page", title.AsFront().AsPrefixedDbPath());
            Assert.AreEqual("Special_talk:page", title.AsTalk().AsPrefixedDbPath());
        }

        [Test]
        public void AsFront_special_talk() {
            Title title = Title.FromPrefixedDbPath("special_talk:page", null);
            Assert.AreEqual("Special:page", title.AsFront().AsPrefixedDbPath());
            Assert.AreEqual("Special_talk:page", title.AsTalk().AsPrefixedDbPath());
        }

        [Test]
        public void AsFront_admin() {
            Title title = Title.FromPrefixedDbPath("Admin:page", null);
            Assert.AreEqual("Admin:page", title.AsFront().AsPrefixedDbPath());
            Assert.AreEqual(null, title.AsTalk());
        }
        #endregion

        #region Rename
        [Test]
        public void Rename_OneSegment() {
            Assert.AreEqual("bar", Title.FromPrefixedDbPath("foo", null)
                .Rename("bar", null)
                .AsPrefixedDbPath());

            Assert.AreEqual("Special:bar", Title.FromPrefixedDbPath("Special:foo", null)
                .Rename("bar", null)
                .AsPrefixedDbPath());

            Assert.AreEqual("Template:bar", Title.FromPrefixedDbPath("Template:foo", null)
                .Rename("bar", null)
                .AsPrefixedDbPath());

            Assert.AreEqual("Special:bar", Title.FromPrefixedDbPath("Special:foo", null)
                .Rename("special:bar", null)
                .AsPrefixedDbPath());
        }

        [Test]
        public void Rename_OneSegmentPrefixed() {
            Assert.AreEqual("Special:bar", Title.FromPrefixedDbPath("Special:foo", null)
                .Rename("Special:bar", null)
                .AsPrefixedDbPath());
        }

        [Test]
        public void Rename_MultiSegment() {
            Assert.AreEqual("a/c", Title.FromPrefixedDbPath("a/b", null)
                .Rename("c", null)
                .AsPrefixedDbPath());

            Assert.AreEqual("Special:a/c", Title.FromPrefixedDbPath("Special:a/b", null)
                .Rename("c", null)
                .AsPrefixedDbPath());

            Assert.AreEqual("Special:a/special:c", Title.FromPrefixedDbPath("Special:a/b", null)
                .Rename("special:c", null)
                .AsPrefixedDbPath());
        }
        [Test]
        public void Rename_CrossNamespaceAttempt() {
            Assert.AreEqual("Special:Template:bar", Title.FromPrefixedDbPath("Special:foo", null)
                .Rename("Template:bar", null)
                .AsPrefixedDbPath());
        }

        [Test]
        [ExpectedException(ExceptionType = typeof(ArgumentException))]
        public void Rename_CrossNamespaceAttempt_illegal() {
            string s = Title.FromPrefixedDbPath("foo", null)
                .Rename("Template:bar", null)
                .AsPrefixedDbPath();
        }

        [Test]
        public void Rename_RootPageTitles() {
            Title t = null;
            t = Title.FromPrefixedDbPath(string.Empty, null);
            t = t.Rename(null, "foo");
            Assert.IsTrue(t.IsHomepage);
            Assert.AreEqual("foo", t.DisplayName);
            Assert.AreEqual(string.Empty, t.AsPrefixedDbPath());

            t = Title.FromPrefixedDbPath("Special:", null);
            t = t.Rename(null, "foo");
            Assert.AreEqual("foo", t.DisplayName);
            Assert.AreEqual("Special:", t.AsPrefixedDbPath());

            t = Title.FromPrefixedDbPath("User:", null);
            t = t.Rename(null, "foo");
            Assert.AreEqual("foo", t.DisplayName);
            Assert.AreEqual("User:", t.AsPrefixedDbPath());
        }

        [Test]
        public void Rename_TitleEncoding() {

            // % handling
            Title t = TestTitle(Title.FromPrefixedDbPath("A", null), null, "a%b", "a%25b", "a%b"); 
            Assert.IsTrue(t.GetParent().IsHomepage);

            // space handling
            t = TestTitle(Title.FromPrefixedDbPath("A", null), null, "a b", "a_b", "a b"); 
        
            // single slash
            t = TestTitle(Title.FromPrefixedDbPath("A", null), null, "a/b", "a//b", "a/b"); 

            // double slash
            t = TestTitle(Title.FromPrefixedDbPath("A", null), null, "a//b", "a////b", "a//b");

            // triple slash
            t = TestTitle(Title.FromPrefixedDbPath("A", null), null, "a///b", "a//////b", "a///b");

            // %25
            TestTitle(Title.FromPrefixedDbPath("A", null), null, "a%25b", "a%25b", "a%25b");
        }

        [Test]
        [Ignore("Displaynames containing characters that change as a result of Xuri.Decode get incorrent renamed path segments")]
        public void RenameTitleEncodingWithPercentEncoding() {

            // %2f
            TestTitle(Title.FromPrefixedDbPath("A", null), null, "a%2fb", "a%2fb", "a%2fb");

            // %2b (fails due to %2b being changed to %2B
            TestTitle(Title.FromPrefixedDbPath("A", null), null, "a%2bb", "a%2bb", "a%2bb");
        }

        #endregion

        private static Title TestTitle(Title t, string newName, string newDisplayName, string resultPrefixedDbPath, string resultDisplayName) {
            t = t.Rename(newName, newDisplayName);
            Assert.AreEqual(resultDisplayName, t.DisplayName);
            Assert.AreEqual(resultPrefixedDbPath, t.AsPrefixedDbPath());
            return t;
        }
    }
}

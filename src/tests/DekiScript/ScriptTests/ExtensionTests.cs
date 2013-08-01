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
using log4net.Config;
using MindTouch.Deki.Script.Expr;
using MindTouch.Deki.Script.Runtime.Library;
using MindTouch.Deki.Services.Extension;
using MindTouch.Dream;
using MindTouch.Xml;

using NUnit.Framework;

namespace MindTouch.Deki.Script.Tests.ScriptTests {

    [TestFixture]
    public class ExtensionTests {
        private DreamHost _host;

        [TestFixtureSetUp]
        public void GlobalSetup() {
            BasicConfigurator.Configure();
            _host = new DreamHost();
            _host.Self.At("load").With("name", "mindtouch.deki.services").Post(DreamMessage.Ok());
            _host.Self.At("load").With("name", "mindtouch.deki").Post(DreamMessage.Ok());
        }


        // --- Tests ---
        [Test]
        public void CallTestService() {
            _host.Self.At("services").Post(new XDoc("config").Elem("class", typeof(TestService).FullName).Elem("path", "test"));
            Plug test = Plug.New(_host.LocalMachineUri).At("test");
            Assert.IsNotNull(test);
            Assert.AreEqual("/test", test.Uri.Path);
        }

        [Test]
        public void Load_Extension() {
            LoadExtension();
        }

        [Test]
        public void Call_no_arg_func() {
            string path = LoadExtension();
            DreamMessage result = Plug.New(_host.LocalMachineUri)
                .At(path, "Hello")
                .Post(new DekiScriptMap().ToXml());
            Assert.IsTrue(result.IsSuccessful);
            XDoc resultDoc = result.ToDocument();
            Assert.AreEqual("value", resultDoc.Name);
            Assert.AreEqual("hi", resultDoc.Elements.First.AsText);
        }

        [Test]
        public void Call_func_with_arg_list() {
            string path = LoadExtension();
            DekiScriptList args = new DekiScriptList()
                .Add(DekiScriptExpression.Constant("1"))
                .Add(DekiScriptExpression.Constant("2"));
            DreamMessage result = Plug.New(_host.LocalMachineUri).At(path, "Addition").Post(args.ToXml());
            Assert.IsTrue(result.IsSuccessful);
            XDoc resultDoc = result.ToDocument();
            Assert.AreEqual("value", resultDoc.Name);
            Assert.AreEqual(3, resultDoc.Elements.First.AsInt);
        }

        [Test]
        public void Can_execute_function_with_a_defaulted_param() {
            string path = LoadExtension();
            DreamMessage result =
                Plug.New(_host.LocalMachineUri).At(path, "DefaultedParam").Post(new DekiScriptMap().ToXml());
            Assert.IsTrue(result.IsSuccessful);
            XDoc resultDoc = result.ToDocument();
            Assert.AreEqual("value", resultDoc.Name);
            Assert.AreEqual(
                "args.date: "+DekiScriptLibrary.DateNow().Substring(0, 4),
                resultDoc.Elements.First.AsText.Substring(0,15));
        }

        [Test]
        public void Can_access_implicit_environment_in_direct_call() {
            string path = LoadExtension();
            DreamMessage result = Plug.New(_host.LocalMachineUri)
                .At(path, "ReturnFooBar")
                .WithHeader("X-DekiScript-Env", "foo.bar=123")
                .Post(new DekiScriptMap().ToXml());
            Assert.IsTrue(result.IsSuccessful);
            XDoc resultDoc = result.ToDocument();
            Assert.AreEqual("value", resultDoc.Name);
            Assert.AreEqual("123", resultDoc.Elements.First.AsText);
        }

        [Test]
        public void Can_access_implicit_environment_in_indirect_call() {
            string path = LoadExtension();
            DreamMessage result = Plug.New(_host.LocalMachineUri)
                .At(path, "ReturnReturnFooBar")
                .WithHeader("X-DekiScript-Env", "foo.bar=123")
                .WithHeader("X-DekiScript-Env", "baz.bat=abc")
                .Post(new DekiScriptMap().ToXml());
            Assert.IsTrue(result.IsSuccessful);
            XDoc resultDoc = result.ToDocument();
            Assert.AreEqual("value", resultDoc.Name);
            Assert.AreEqual("123", resultDoc.Elements.First.AsText);
        }

        // --- Methods ---
        private string LoadExtension() {
            string path = StringUtil.CreateAlphaNumericKey(8);
            XDoc config = new XDoc("config")
                .Elem("sid", "sid://mindtouch.com/2007/12/dekiscript")
                .Elem("path", path)
                .Elem("manifest", Environment.CurrentDirectory + @"\ScriptTests\ExtensionTests.xml")
                .Elem("debug", true);
            DreamMessage response = _host.Self.At("services").PostAsync(config).Wait();
            Assert.IsTrue(response.IsSuccessful);
            return path;
        }


    }
}

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
using System.IO;
using System.Text;

using NUnit.Framework;

namespace MindTouch.Deki.Script.Tests.ScriptTests {

    [TestFixture]
    public class ScriptValidatorTests {

        // --- Fields ---
        private string _scriptRoot;


        [TestFixtureSetUp]
        public void GlobalSetup() {
            _scriptRoot = System.Configuration.ConfigurationManager.AppSettings["script.root"];
            _scriptRoot = _scriptRoot.IfNullOrEmpty(@"C:\mindtouch\public\dekiwiki\trunk\src\services\Scripts");
        }

        // --- Tests ---
        [Test]
        public void Validator_should_return_an_invalid_result() {
            ScriptManifestValidator validator = new ScriptManifestValidator();
            string scriptPath = Environment.CurrentDirectory + @"\ScriptTests\BadScriptExtension.xml";
            ScriptManifestValidationResult result = validator.Validate(scriptPath);
            Assert.IsTrue(result.IsInvalid);
        }

        [Test]
        public void Validator_should_return_a_valid_result() {
            ScriptManifestValidator validator = new ScriptManifestValidator();
            string scriptPath = Environment.CurrentDirectory + @"\ScriptTests\ExtensionTests.xml";
            ScriptManifestValidationResult result = validator.Validate(scriptPath);
            Assert.IsFalse(result.IsInvalid);
        }

        [Test]
        public void Validate_extension_and_execute_expression() {
            // validate the script first
            ScriptManifestValidator validator = new ScriptManifestValidator();
            string scriptPath = Environment.CurrentDirectory + @"\ScriptTests\ExtensionTests.xml";
            ScriptManifestValidationResult result = validator.Validate(scriptPath);
            Assert.IsFalse(result.IsInvalid);

            // now execute a function against it
            ScriptTestHarness harness = new ScriptTestHarness();
            harness.LoadExtension(scriptPath);
            string executionResult = harness.Execute("test.Hello()");
            Assert.AreEqual(@"""hi""", executionResult);
        }

        [Test]
        public void Test_all_distributed_extension_scripts() {
            ScriptManifestValidator validator = new ScriptManifestValidator();
            StringBuilder errors = new StringBuilder();
            string[] scripts = Directory.GetFiles(_scriptRoot, "*.xml");
            if(scripts == null || scripts.Length == 0) {
                Assert.Fail("No scripts found at " + _scriptRoot);
            }
            foreach(string scriptPath in scripts) {
                if(scriptPath.EndsWithInvariantIgnoreCase("script-template.xml")) {
                    continue;
                }
                ScriptManifestValidationResult result = validator.Validate(scriptPath);
                if(result.IsInvalid) {
                    errors.AppendFormat(" - {0}: {1}\r\n",
                                        Path.GetFileNameWithoutExtension(scriptPath),
                                        result.ValidationErrors);
                }
            }
            if(errors.Length > 0) {
                errors.Insert(0, "The following scripts failed validation:\r\n");
                Assert.Fail(errors.ToString());
            }
        }
    }
}

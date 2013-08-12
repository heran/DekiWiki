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
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using MindTouch.Deki.Search;
using MindTouch.Dream;
using Moq;
using NUnit.Framework;

namespace MindTouch.Deki.Tests {

    [TestFixture]
    public class DekiResourcesTests {
        private static Regex _paramsRegex = new Regex(@"\{(\d+)\}", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        private Mock<IPlainTextResourceManager> _resourceManagerMock;
        private DekiResources _resources;

        [SetUp]
        public void Setup() {
            _resourceManagerMock = new Mock<IPlainTextResourceManager>();
            _resources = new DekiResources(_resourceManagerMock.Object, CultureInfo.InvariantCulture);
        }

        [Test]
        public void Can_localize_no_arg_resource() {
            _resourceManagerMock.Setup(x => x.GetString("System.API.Error.rating_invalid_score", CultureInfo.InvariantCulture, null))
                .Returns("foo").AtMostOnce().Verifiable();
            var resource = DekiResources.RATING_INVALID_SCORE();
            Assert.AreEqual("foo", _resources.Localize(resource));
            _resourceManagerMock.VerifyAll();
        }

        [Test]
        public void Can_localize_resource_with_arguments() {
            _resourceManagerMock.Setup(x => x.GetString("System.API.Error.property_concurrency_error", CultureInfo.InvariantCulture, null))
                .Returns("{0}").AtMostOnce().Verifiable();
            var resource = DekiResources.PROPERTY_CONCURRENCY_ERROR(1234);
            Assert.AreEqual("1234", _resources.Localize(resource));
            _resourceManagerMock.VerifyAll();
        }

        [Test]
        public void Localizing_non_existent_key_retuns_error_string() {
            _resourceManagerMock.Setup(x => x.GetString("System.API.Error.rating_invalid_score", CultureInfo.InvariantCulture, null))
                .Returns((string)null).AtMostOnce().Verifiable();
            var resource = DekiResources.RATING_INVALID_SCORE();
            Assert.AreEqual("[MISSING: System.API.Error.rating_invalid_score]", _resources.Localize(resource));
            _resourceManagerMock.VerifyAll();
        }

        [Test]
        public void Localize_will_localize_resources_passed_as_arguments() {
            _resourceManagerMock.Setup(x => x.GetString("foo", CultureInfo.InvariantCulture, null))
                .Returns("foo").AtMostOnce().Verifiable();
            _resourceManagerMock.Setup(x => x.GetString("bar", CultureInfo.InvariantCulture, null))
                .Returns("--{0}--").AtMostOnce().Verifiable();
            var r1 = new DekiResource("foo");
            var r2 = new DekiResource("bar", r1);
            Assert.AreEqual("--foo--", _resources.Localize(r2));
            _resourceManagerMock.VerifyAll();
        }

        [Test]
        public void Localize_will_localize_DateTime_passed_as_argument() {
            _resourceManagerMock.Setup(x => x.GetString("foo", CultureInfo.InvariantCulture, null))
                .Returns("{0}").AtMostOnce().Verifiable();
            var date = DateTime.Parse("2010/8/13");
            var resource = new DekiResource("foo", date);
            Assert.AreEqual(date.ToString(CultureInfo.InvariantCulture), _resources.Localize(resource));
            _resourceManagerMock.VerifyAll();
        }

        [Test]
        public void Verify_that_default_resource_strings_have_correct_argument_count() {
            var resourceManager = new PlainTextResourceManager(Utils.Settings.DekiResourcesPath);
            var resourceMethods = from method in typeof(DekiResources).GetMethods(BindingFlags.Static | BindingFlags.Public)
                                  where method.ReturnType == typeof(DekiResource)
                                  select method;
            foreach(var method in resourceMethods) {
                var parameters = method.GetParameters();
                var args = new object[parameters.Length];
                for(var i = 0; i < parameters.Length; i++) {
                    var type = parameters[i].ParameterType;
                    if(type == typeof(string)) {
                        args[i] = string.Empty;
                    } else if(type.IsA<DekiResource>()) {

                        // can't test resources that take other resources via this method
                    } else if(type.IsA<MimeType>()) {
                        args[i] = MimeType.TEXT_XML;
                    } else if(type.IsA<SearchQuery>()) {
                        args[i] = new SearchQuery("foo", "bar", new LuceneClauseBuilder(), null);
                    } else if(type.IsA<XUri>()) {
                        args[i] = new XUri("http://foo");
                    } else {
                        try {
                            args[i] = Activator.CreateInstance(parameters[i].ParameterType, false);
                        } catch(Exception) {
                            Assert.Fail(string.Format("{0}: cannot create argument instance of type '{1}'", method.Name, parameters[i].ParameterType));
                        }
                    }
                }
                var resource = (DekiResource)method.Invoke(null, args);
                var format = resourceManager.GetString(resource.LocalizationKey, CultureInfo.InvariantCulture, null);
                Assert.IsNotNull(format, string.Format("{0}: No localization string exists for key '{1}'", method.Name, resource.LocalizationKey));
                var paramSet = new HashSet<int>();
                var matches = _paramsRegex.Matches(format);
                for(int i = 0; i < matches.Count; i++) {
                    paramSet.Add(Convert.ToInt32(matches[i].Groups[1].Value));
                }
                Assert.IsTrue(resource.Args.Length >= paramSet.Count, string.Format("{0}: too many parameters in  string '{1}' ({2} < {3})", method.Name, format, resource.Args.Length, paramSet.Count));
                if(paramSet.Count == 0) {
                    continue;
                }
                Assert.AreEqual(paramSet.Count - 1, paramSet.OrderBy(x => x).Last(), string.Format("{0}: incorrect last parameter index '{1}'", method.Name, format));
            }
        }
    }
}

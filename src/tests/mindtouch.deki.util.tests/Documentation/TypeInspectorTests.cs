/*
 * MindTouch Core - open source enterprise collaborative networking
 * Copyright (c) 2006-2010 MindTouch Inc.
 * www.mindtouch.com  oss@mindtouch.com
 *
 * For community documentation and downloads visit wiki.developer.mindtouch.com;
 * please review the licensing section.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 *     http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using log4net;
using MindTouch.Reflection;
using MindTouch.Dream;
using MindTouch.Xml;
using NUnit.Framework;

namespace MindTouch.Deki.Util.Tests.Documentation {

    [TestFixture]
    public class TypeInspectorTests {

        //--- Static Fields ---
        private static readonly ILog _log = LogUtils.CreateLog();

        private TypeInspector _inspector;
        private XDoc _xmlDocumentation;

        [TestFixtureSetUp]
        public void GlobalSetup() {
            _log.Debug("setup");
            var assemblyPath = @"MindTouch.Deki.Util.Tests.dll";
            _inspector = new TypeInspector();
            _inspector.InspectAssembly(assemblyPath);
            _xmlDocumentation = XDocFactory.LoadFrom(@"MindTouch.Deki.Util.Tests.xml", MimeType.TEXT_XML);
        }

        [TestFixtureTearDown]
        public void GlobalTeardown() {
            //_inspector.Dispose();
        }

        [Test]
        public void Can_reflect_member_attributes() {
            var type = _inspector.Types.Where(x => x.Name == "C").FirstOrDefault();
            Debug.WriteLine("Constructors ------------------");
            foreach(var ctor in type.Constructors) {
                if(ctor.IsHidden) {
                    continue;
                }
                var sig = ctor.MemberAccess.ToString().ToLower() + " ";
                sig += ctor.IsStatic ? "static " : "";
                sig += ctor.DisplayName;
                sig += ctor.IsInherited ? " (Inherited from " + ctor.DeclaringType.DisplayName + ")" : "";
                Debug.WriteLine(sig);
            }
            Debug.WriteLine("Fields ------------------");
            foreach(var field in type.Fields) {
                if(field.IsHidden) {
                    continue;
                }
                var sig = field.MemberAccess.ToString().ToLower() + " ";
                sig += field.IsStatic ? "static " : "";
                sig += field.DisplayName;
                sig += field.IsInherited ? " (Inherited from " + field.DeclaringType.DisplayName + ")" : "";
                Debug.WriteLine(sig);
            }
            Debug.WriteLine("Methods ------------------");
            foreach(var method in type.Methods) {
                if(method.IsHidden) {
                    continue;
                }
                var sig = method.MemberAccess.ToString().ToLower() + " ";
                sig += method.IsStatic ? "static " : "";
                sig += method.IsVirtual ? "virtual " : "";
                sig += method.IsOverride ? "override " : "";
                sig += method.IsNew ? "new " : "";
                sig += method.DisplayName;
                sig += method.IsInherited ? " (Inherited from " + method.DeclaringType.DisplayName + ")" : "";
                Debug.WriteLine(sig);
            }
            Debug.WriteLine("Properties ------------------");
            foreach(var property in type.Properties) {
                //if(property.IsHidden) {
                //    continue;
                //}
                var sig = property.MemberAccess.ToString().ToLower() + " ";
                sig += property.IsStatic ? "static " : "";
                sig += property.IsVirtual ? "virtual " : "";
                sig += property.IsOverride ? "override " : "";
                sig += property.IsNew ? "new " : "";
                sig += property.DisplayName;
                sig += "{ ";
                if(property.MemberAccess != property.GetAccess) {
                    if(property.GetAccess == MemberAccess.Protected) {
                        sig += "protected get; ";
                    }
                } else {
                    sig += "get; ";
                }
                if(property.MemberAccess != property.SetAccess) {
                    if(property.SetAccess == MemberAccess.Protected) {
                        sig += "protected set; ";
                    }
                } else {
                    sig += "set; ";
                }
                sig += "}";
                sig += property.IsInherited ? " (Inherited from " + property.DeclaringType.DisplayName + ")" : "";
                Debug.WriteLine(sig);
            }
        }

        [Test]
        public void All_documentation_signatures_were_reflected() {
            var signatures = new HashSet<string>();
            foreach(var type in _inspector.Types.Where(x => x.Namespace.StartsWith("MindTouch.Deki.Util.Tests.Documentation.Types"))) {
                signatures.Add(type.Signature);
                foreach(var ctor in type.Constructors) {
                    signatures.Add(ctor.Signature);
                }
                foreach(var field in type.Fields) {
                    if(field.IsInherited) {
                        continue;
                    }
                    signatures.Add(field.Signature);
                }
                foreach(var method in type.Methods) {
                    if(method.IsInherited) {
                        continue;
                    }
                    signatures.Add(method.Signature);
                }
                foreach(var property in type.Properties) {
                    if(property.IsInherited) {
                        continue;
                    }
                    signatures.Add(property.Signature);
                }
                foreach(var ev in type.Events) {
                    if(ev.IsInherited) {
                        continue;
                    }
                    signatures.Add(ev.Signature);
                }
            }
            foreach(var signature in signatures.OrderBy(x => x)) {
                _log.Debug(signature);
            }
            var missing = new List<string>();
            foreach(var signature in _xmlDocumentation["members/member/@name"].Select(x => x.AsText).Where(x => x.Contains("MindTouch.Deki.Util.Tests.Documentation.Types"))) {
                if(!signatures.Contains(signature)) {
                    missing.Add(signature);
                }
            }
            if(missing.Any()) {
                var builder = new StringBuilder();
                builder.AppendLine("Missing signatures:");
                foreach(var signature in missing) {
                    builder.AppendLine(signature);
                }
                Assert.Fail(builder.ToString());
            }
        }

        [Test]
        public void Can_inspect_dream_with_appdomain() {
            _log.Debug("starting dream inspection");
            var inspector = new IsolatedTypeInspector();
            inspector.InspectAssembly(@"mindtouch.dream.dll");
            inspector.InspectAssembly(@"mindtouch.dream.test.dll");
            inspector.InspectAssembly(@"mindtouch.core.dll");
            var types = inspector.Types.ToList();
            inspector.Dispose();
        }

        [Test]
        public void Can_inspect_dream_without_appdomain() {
            _log.Debug("starting dream inspection");
            var inspector = new TypeInspector();
            inspector.InspectAssembly(@"mindtouch.dream.dll");
            inspector.InspectAssembly(@"mindtouch.dream.test.dll");
            inspector.InspectAssembly(@"mindtouch.core.dll");
            var types = inspector.Types.ToList();
        }

        [Ignore("Used for manual inspection of output")]
        [Test]
        public void Inspect_self() {
            foreach(var type in _inspector.Types.Where(x => x.Namespace == "MindTouch.Deki.Util.Tests.Documentation.Types")) {
                Console.WriteLine(type.DisplayName);
                Console.WriteLine(type.Signature);
                foreach(var field in type.Fields) {
                    if(field.IsInherited) {
                        continue;
                    }
                    Console.WriteLine("  " + field.Signature);
                }
                foreach(var method in type.Methods) {
                    if(method.IsInherited) {
                        continue;
                    }
                    //Console.WriteLine("  " + method.DisplayName);
                    Console.WriteLine("  " + method.Signature);
                }
                foreach(var property in type.Properties) {
                    if(property.IsInherited) {
                        continue;
                    }
                    Console.WriteLine("  " + property.Signature);
                }
            }
        }

        [Ignore("Used for manual inspection of output")]
        [Test]
        public void Generic_method_definitions() {
            var genericMethods = _inspector.Types.Where(x => x.Name == "GenericMethods").FirstOrDefault();
            foreach(var method in genericMethods.Methods) {
                if(method.IsInherited) {
                    continue;
                }
                Console.WriteLine("  " + method.DisplayName);
                Console.WriteLine("  " + method.Signature);
            }
        }

        [Ignore("Used for manual inspection of output")]
        [Test]
        public void Nested_classes() {
            foreach(var type in _inspector.Types.Where(x => x.Name.StartsWith("Outer") || x.Name.StartsWith("UsingNestedTypeAsParam"))) {
                foreach(var method in type.Methods) {
                    if(method.IsInherited) {
                        continue;
                    }
                    Console.WriteLine("  " + method.DisplayName);
                    Console.WriteLine("  " + method.Signature);
                }
            }
        }

        [Ignore]
        [Test]
        public void Check_specific_class() {
            var type = _inspector.Types.Where(x => x.LocalSignature == "ElasticWorkQueue`1").FirstOrDefault();
        }
    }
}
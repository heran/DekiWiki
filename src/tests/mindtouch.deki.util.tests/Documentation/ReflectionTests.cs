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
using System.Linq;
using System.Reflection;
using System.Text;
using MindTouch.Deki.Util.Tests.Documentation.Types;
using NUnit.Framework;

namespace MindTouch.Deki.Util.Tests.Documentation {

    [TestFixture]
    public class ReflectionTests {

        [Test]
        public void Reflect_methods() {
            var t = typeof(OverrideBar);
            var methods = t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            foreach(var method in methods) {
                if(method.IsPrivate) {
                    continue;
                }
                bool NewSlot = ((method.Attributes & MethodAttributes.NewSlot) == MethodAttributes.NewSlot);
                bool ReuseSlot = ((method.Attributes & MethodAttributes.ReuseSlot) == MethodAttributes.ReuseSlot);
                bool isInherited = method.DeclaringType != t;
                bool isOverride = method.IsVirtual && !NewSlot && ReuseSlot;
                bool isProtected = method.IsFamily && !method.IsPublic && !method.IsPrivate;

                var builder = new StringBuilder();
                if(NewSlot)
                    builder.Append("newslot ");
                if(ReuseSlot)
                    builder.Append("reuseslot ");
                if(method.IsPublic)
                    builder.Append("public ");
                if(method.IsPrivate)
                    builder.Append("private ");
                if(isProtected)
                    builder.Append("protected ");
                if(method.IsVirtual)
                    builder.Append("virtual ");
                if(method.IsStatic)
                    builder.Append("static ");
                if(isOverride)
                    builder.Append("override ");
                if(isInherited)
                    builder.Append("inherited ");
                if(method.IsAssembly)
                    builder.Append("internal ");
                builder.Append(method.Name);
            }
        }

        [Test]
        public void Reflect_Struct() {
            var structType = typeof(Struct);
            var classTypes = typeof(Class);
        }

        [Test]
        public void GenericConstraints() {
            var t = typeof(C).Assembly.GetTypes().Where(x => x.Name == "Constrained`4").FirstOrDefault();
            foreach(var param in t.GetGenericArguments()) {
                var constraints = param.GenericParameterAttributes;
                var MustBeReferenceType = GetConstraint(constraints, GenericParameterAttributes.ReferenceTypeConstraint);
                var MustBeValueType = GetConstraint(constraints, GenericParameterAttributes.NotNullableValueTypeConstraint);
                var MustHaveDefaultConstructor = GetConstraint(constraints, GenericParameterAttributes.DefaultConstructorConstraint);
            }
        }

        private bool GetConstraint(GenericParameterAttributes constraintMask, GenericParameterAttributes constraint) {
            return (constraintMask & constraint) == constraint;
        }

        [Test]
        public void Indexer_with_private_get_method() {
            var t = typeof(IndexerWithPrivateGetMethod);
            var p = t.GetProperties().FirstOrDefault();
            var getMethod = p.GetGetMethod(true);
            var setMethod = p.GetSetMethod(true);
        }

        [Test]
        public void What_is_special_about_ValueType_inherited_methods() {
            var t = typeof(Struct);
            foreach(var method in t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)) {
                if(method.IsSpecialName || method.IsPrivate || method.IsAssembly || method.Name == "Finalize") {
                    continue;
                }
                var isNewSlot = ((method.Attributes & MethodAttributes.NewSlot) == MethodAttributes.NewSlot);
                var isReusedSlot = ((method.Attributes & MethodAttributes.ReuseSlot) == MethodAttributes.ReuseSlot);
                var isOverride = method.IsVirtual && !isNewSlot && isReusedSlot;
                bool isInherited = method.DeclaringType != t;
            }
        }

        [Test]
        public void Determine_class_attributes() {
            foreach(var t in new[] { typeof(Public), typeof(Sealed), typeof(Static), typeof(Abstract) }) {
                string sig = "";
                if(t.IsSealed) {
                    sig += "sealed ";
                }
                if(t.IsNotPublic) {
                    sig += "notpublic ";
                }
                if(t.IsPublic) {
                    sig += "ispublic ";
                }
                if(t.IsAbstract) {
                    sig += "abstract ";
                }
                if(t.IsContextful) {
                    sig += "contextful ";
                }
                if(t.IsVisible) {
                    sig += "visible ";
                }
                sig += t.Name + "( "+ t.Attributes + " )";
            }
        }

        public struct Struct {
            public string X;
            public void Foo() { }
        }

        public class Class {
            public string X;
        }

        public class IndexerWithPrivateGetMethod {
            public int this[string x] { set { } }
        }

        public class Base {
            public virtual void Foo() { }
            public virtual void Bar() { }
            public virtual void NewInFoo() { }
            public virtual void NewInBar() { }
        }

        public class OverrideFoo : Base {
            public override void Foo() { }
            public new void NewInFoo() {}
        }

        public class OverrideBar : OverrideFoo {
            public override void Bar() { }
            public new void NewInBar() { }
        }
    }
    public class Public { }
    public sealed class Sealed { }
    public static class Static { }
    public abstract class Abstract { }

}

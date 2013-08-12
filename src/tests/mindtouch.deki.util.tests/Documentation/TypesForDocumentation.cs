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
using System.Collections;
using System.Collections.Generic;

namespace MindTouch.Deki.Util.Tests.Documentation.Types {

    /// <summary>
    /// 
    /// </summary>
    public class Inspectable {

    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Inspectable<T> {

    }

    /// <summary>
    /// Generic class using <see cref="Inspectable{T}"/> as one parameter
    /// </summary>
    /// <typeparam name="T">Description of type param T</typeparam>
    /// <typeparam name="V">Description of type</typeparam>
    public class Inspectable<T, V> where T : Inspectable<IEnumerable<V>> {

    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IInspectable<T> { }

    /// <summary>
    /// 
    /// </summary>
    public class InspectableInt : Inspectable<Struct>, IInspectable<IEnumerable<ArrayParams>> {

    }

    /// <summary>
    /// 
    /// </summary>
    public struct Struct {

    }

    /// <summary>
    /// 
    /// </summary>
    public class ValueParams {
        public void Foo(int i, byte b) { }
    }

    /// <summary>
    /// 
    /// </summary>
    public class ArrayParams {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="i"></param>
        public void Foo(int[] i) { }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="p"></param>
        public void Bar(params int[] p) { }
    }

    /// <summary>
    /// 
    /// </summary>
    public class OutRefParams {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="i"></param>
        public void Out(out int i) {
            i = 0;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="i"></param>
        public void Ref(ref int i) { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="i"></param>
        public void Params(params int[] i) { }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="x"></param>
        public void ComplexRef<T>(ref IList<T> x) { }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T1">Class and new</typeparam>
    /// <typeparam name="T2">Enumerable of T1</typeparam>
    /// <typeparam name="T3">Enumerrable of int, disposable</typeparam>
    /// <typeparam name="T4">struct only</typeparam>
    public class Constrained<T1, T2, T3, T4>
        where T1 : class, new()
        where T2 : IEnumerable<T1>, IDisposable
        where T3 : Constrained<T1, IEnumerable<Base>>
        where T4 : struct {

    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T1"></typeparam>
    /// <typeparam name="T2"></typeparam>
    public class Constrained<T1, T2> { }

    /// <summary>
    /// Base class
    /// </summary>
    public class Base {
        public virtual int Foo() {
            return 0;
        }
        public virtual int Bar { get; set; }
        public int Baz;
    }

    /// <summary>
    /// Subclass of <see cref="Base"/> providing and override of <see cref="Base.Foo"/> as <see cref="Foo"/>
    /// </summary>
    public class OverrideBase : Base {
        /// <summary>
        /// Override of <see cref="Base.Foo"/>
        /// </summary>
        /// <returns></returns>
        public override int Foo() { return 42; }
        public override int Bar { get; set; }
    }

    /// <summary>
    /// Subclass of <see cref="Base"/> hiding <see cref="Base.Foo"/> with <see cref="Foo"/>
    /// </summary>
    public class NewBase : Base {

        /// <summary>
        /// Hides <see cref="Base.Foo"/>
        /// </summary>
        /// <returns></returns>
        public new virtual int Foo() { return 0; }
        public new virtual int Bar { get; set; }
        public new int Baz;
    }

    /// <summary>
    /// 
    /// </summary>
    public class InheritedBase : Base {

    }

    /// <summary>
    /// 
    /// </summary>
    public class InbetweenBase : Base {

    }

    /// <summary>
    /// 
    /// </summary>
    public class MultiLevelInherited : InbetweenBase {

    }

    /// <summary>
    /// Methods:
    /// <see cref="TArg"/>
    /// <see cref="VArg{V}"/>
    /// <see cref="V1andV2{V1,V2}"/>
    /// <see cref="ListOfTArg"/>
    /// <see cref="ListOfVArg{V}"/>
    /// <see cref="ListsOfTandVArgs{V}"/>
    /// <see cref="ListsOfVandIntArgs{V}"/>
    /// <see cref="NestedGenericOfV{V}"/>
    /// <see cref="NestedGenericOfT"/>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class GenericMethods<T> {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="t"></param>
        public void TArg(T t) { }
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="V"></typeparam>
        /// <param name="v"></param>
        public void VArg<V>(V v) { }
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="V1"></typeparam>
        /// <typeparam name="V2"></typeparam>
        /// <param name="v1"></param>
        /// <param name="v2"></param>
        public void V1andV2<V1, V2>(V1 v1, V2 v2) { }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="l"></param>
        public void ListOfTArg(List<T> l) { }
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="V"></typeparam>
        /// <param name="l"></param>
        public void ListOfVArg<V>(List<V> l) { }
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="V"></typeparam>
        /// <param name="a"></param>
        /// <param name="b"></param>
        public void ListsOfTandVArgs<V>(List<T> a, List<V> b) { }
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="V"></typeparam>
        /// <param name="v"></param>
        /// <param name="i"></param>
        public void ListsOfVandIntArgs<V>(List<V> v, List<int> i) { }
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="V"></typeparam>
        /// <param name="v"></param>
        public void NestedGenericOfV<V>(List<List<V>> v) { }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="t"></param>
        public void NestedGenericOfT(List<List<T>> t) { }
    }

    /// <summary>
    /// Methods:
    /// <see cref="AllGenerics{X,Y}"/>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="V"></typeparam>
    public class GenericMethods<T, V> {
        public void AllGenerics<X, Y>(T t, V v, X x, Y y) { }
    }

    /// <summary>
    /// Properties:
    /// <see cref="Item(System.Collections.Generic.List{System.Collections.Generic.List{T}})"/>
    /// <see cref="Item(T)"/>
    /// <see cref="Item(int,int)"/>
    /// <see cref="Item(int)"/>
    /// <see cref="Foo"/>
    /// <see cref="Bar"/>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Properties<T> {
        /// <summary>
        /// Blah blah blah
        /// </summary>
        /// <param name="x">x marks the pedwalk</param>
        /// <returns>get a number, any number</returns>
        public int this[List<List<T>> x] { get { return 0; } set { } }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public T this[T t] { get { return default(T); } set { } }
        /// <summary>
        /// asdfsdfsdfsdfsdf
        /// </summary>
        /// <param name="x">x sdfsdfdsf</param>
        /// <param name="y">y sfsdfesdfsdfef</param>
        /// <returns>return sdfsdf</returns>
        public T this[int x, int y] { get { return default(T); } set { } }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        public T this[int x] { get { return default(T); } set { } }

        /// <summary>
        /// sdfwefd3ksdpjsdlojpsf
        /// </summary>
        /// <param name="x">sdfsdfwekkwk kskfksdf</param>
        /// <returns>lorem ipsum</returns>
        public int this[string x] { set { } }
        /// <summary>
        /// 
        /// </summary>
        public int Foo { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public T Bar { get; set; }
    }

    /// <summary>
    /// Fields:
    /// <see cref="FieldT"/>
    /// <see cref="IntField"/>
    /// <see cref="Properties{T}.this(System.Collections.Generic.List{System.Collections.Generic.Item})"/>
    /// <see cref="Properties{T}.Item(T)"/>
    /// <see cref="Properties{T}.Item(int,int)"/>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Fields<T> {
        public T FieldT;
        public readonly int IntField;
    }

    /// <summary>
    /// Statics:
    /// <see cref="Constant"/>
    /// <see cref="StaticField"/>
    /// <see cref="StaticMethod"/>
    /// <see cref="StaticProperty"/>
    /// </summary>
    public class StaticsAndConstants {

        /// <summary>
        /// 
        /// </summary>
        public const int Constant = 0;

        /// <summary>
        /// 
        /// </summary>
        public static int StaticField;

        /// <summary>
        /// 
        /// </summary>
        public static void StaticMethod() { }

        /// <summary>
        /// 
        /// </summary>
        public static int StaticProperty { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    public class Single {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        public int this[int i] { get { return 0; } set { } }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public void Foo() { }
    }

    /// <summary>
    /// 
    /// </summary>
    public class Overload {
        /// <summary>
        /// A summary
        /// </summary>
        /// <remarks>And some remarks</remarks>
        /// <param name="i"></param>
        /// <returns>Returns <b>an integer</b> value</returns>
        public int this[int i] { get { return 0; } set { } }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public void Foo() { }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        public int this[string i] { get { return 0; } set { } }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        public void Foo(int i) { }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TOuter"></typeparam>
    public class Outer<TOuter, T> {

        /// <summary>
        /// 
        /// </summary>
        /// <param name="inner"></param>
        public void InnerParam(Middle1.Middle2<bool, byte>.Inner<int, string> inner) { }

        /// <summary>
        /// 
        /// </summary>
        public class Middle1 {
            /// <summary>
            /// 
            /// </summary>
            /// <typeparam name="TMiddle"></typeparam>
            /// <typeparam name="T"></typeparam>
            public class Middle2<TMiddle, T> {
                /// <summary>
                /// 
                /// </summary>
                /// <typeparam name="TInner"></typeparam>
                /// <typeparam name="T"></typeparam>
                public class Inner<TInner, T> {
                    /// <summary>
                    /// 
                    /// </summary>
                    /// <typeparam name="TOuter"></typeparam>
                    /// <typeparam name="TMethod"></typeparam>
                    /// <param name="o"></param>
                    /// <param name="m"></param>
                    /// <param name="i"></param>
                    /// <param name="x"></param>
                    public void Foo<TOuter, TMethod>(TOuter o, TMiddle m, TInner i, TMethod x) { }
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public class InnerNonGeneric {
            /// <summary>
            /// 
            /// </summary>
            /// <param name="o"></param>
            public void Foo(TOuter o) { }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public class UsingNestedTypeAsParam {

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="V"></typeparam>
        /// <typeparam name="Z"></typeparam>
        /// <param name="inner"></param>
        public void InnerParam<T, V, Z>(Outer<T, bool>.Middle1.Middle2<V, byte>.Inner<Z, string> inner) { }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="V"></typeparam>
        /// <typeparam name="Z"></typeparam>
        /// <param name="inner"></param>
        public void InnerParam2<T, V, Z>(Outer<bool, int>.Middle1.Middle2<Z, V>.Inner<T, T> inner) { }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="t"></param>
    /// <returns></returns>
    public delegate int SomeGenericDelegate<T>(T t);

    public class ClassWithDelegateInIt<T> {

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="V"></typeparam>
        /// <param name="v"></param>
        /// <returns></returns>
        public delegate T SomeGenericDelegate<V>(V v);
    }

    /// <summary>
    /// 
    /// </summary>
    public interface ISomeInterface : IDisposable {
        /// <summary>
        /// 
        /// </summary>
        void InterfaceMethod();
    }

    /// <summary>
    /// 
    /// </summary>
    public class SomeImplementer : ISomeInterface, IEnumerable<int> {

        /// <summary>
        /// 
        /// </summary>
        public void Dispose() { }

        /// <summary>
        /// 
        /// </summary>
        public void InterfaceMethod() { }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public IEnumerator<int> GetEnumerator() {
            return null;
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public enum SomeEnum {
        /// <summary>
        /// Value one
        /// </summary>
        One,
        /// <summary>
        /// sdfdsfsdf
        /// </summary>
        Two,
        /// <summary>
        /// blah blah
        /// </summary>
        Three
    }

    /// <summary>
    /// 
    /// </summary>
    public class Events {

        /// <summary>
        /// 
        /// </summary>
        public event EventHandler<EventArgs> SomeEvent;

        /// <summary>
        /// 
        /// </summary>
        public event SomeGenericDelegate<int> SomeDelegatedEvent;
    }

    /// <summary>
    /// 
    /// </summary>
    public class MethodThrows {
        /// <summary>
        /// This one throws
        /// </summary>
        /// <exception cref="Exception">Sometimes it's just a plain one</exception>
        /// <exception cref="DocumentedException">And other times it's this one.</exception>
        public void Throws() { }
    }
    /// <summary>
    /// It's documented and linkable
    /// </summary>
    public class DocumentedException : Exception { }
    /// <summary>
    /// 
    /// </summary>
    public class Constructors {

        /// <summary>
        /// 
        /// </summary>
        /// <param name="i"></param>
        public Constructors(int i) { }

        /// <summary>
        /// 
        /// </summary>
        public Constructors() { }
    }

    /// <summary>
    /// 
    /// </summary>
    public abstract class Abstract {
        /// <summary>
        /// 
        /// </summary>
        protected void Protected() { }
    }

    public class B {
        protected virtual void Virt() { }
        protected virtual void VirtNew() { }
        protected void NotVirt() { }
        public void Inherited() { }
        public static void InheritedStatic() { }
        public override string ToString() {
            return null;
        }
        protected virtual int GetSet { get; set; }
        protected virtual int GetSetNew { get; set; }
        public int InheritedProperty { get; set; }
        public int InheritedField;
    }

    public class C : B {
        private C(string x, int y) { }
        public C(string x) { }
        protected C(int x) { }
        protected override void Virt() { }
        protected new void VirtNew() { }
        public void Public() { }
        public virtual void MyVirt() { }
        private void Private() { }
        public static void Static() { }
        protected new void NotVirt() { }
        public int GetPrivateSet { get; private set; }
        public int GetNoSet {
            get { return 0; }
        }
        public int GetProtectedSet { get; protected set; }
        protected override int GetSet { get; set; }
        protected new int GetSetNew { get; set; }
        public static int StaticProperty { get; set; }
        protected int ProtectedField;
        public int PublicField;
        public static int StaticField;
        internal void Internal() { }
    }

    /// <summary>
    /// 
    /// </summary>
    public static class ExtensionMethods {
        
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="target"></param>
        /// <param name="format"></param>
        /// <returns></returns>
        public static string ToEmptyString<T>(this T target, string format) {
            return "";
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public sealed class SealedClass {}
    
    /// <summary>
    /// 
    /// </summary>
    public abstract class AbstractClass {}

    public interface IBaseInterface {}
    public interface ISubInterface : IBaseInterface {}
    public interface IFinalInterface : ISubInterface {}

    public class ImplementSubInterface : ISubInterface {}
    public class SubclassSubInterfaceImplementor : ImplementSubInterface {}

}

namespace MindTouch.Deki.Util.Tests.Documentation.Types.SubTypes {

    /// <summary>
    /// 
    /// </summary>
    /// <covers>
    /// <feature>GET:pages/{pageid}/contents</feature>
    /// <ref>http://developer.mindtouch.com/Deki/API_Reference/GET%3apages%2f%2f%7bpageid%7d%2f%2fcontents</ref>
    /// </covers>
    public class SubNamespaced {

    }
}

namespace MindTouch.Deki.Util.Tests.Documentation.Sample {

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T1"></typeparam>
    /// <typeparam name="T2"></typeparam>
    public class Constrained<T1, T2> { }

    /// <summary>
    /// 
    /// </summary>
    public class Base1ForSample { }

    /// <summary>
    /// 
    /// </summary>
    public class Base2ForSample<T> : Base1ForSample { }

    /// <summary>
    /// 
    /// </summary>
    public interface ISample { }

    public interface IGeneric<T> { }
    /// <summary>
    /// Class Description with link to <see cref="Base1ForSample"/>.
    /// </summary>
    /// <remarks> Optional Remarks</remarks>
    /// <typeparam name="T1">first generic argument</typeparam>
    /// <typeparam name="T2">second generic argument</typeparam>
    public class SampleDocumentedGenericClass<T1, T2> : Base2ForSample<int>, ISample, IGeneric<Base1ForSample>, IDisposable
        where T1 : class
        where T2 : Constrained<T1, IEnumerable<Base1ForSample>>, new() {

        /// <summary>
        /// Default Constructor
        /// </summary>
        /// <remarks> Optional Remarks</remarks>
        public SampleDocumentedGenericClass() { }

        /// <summary>
        /// Another constructor
        /// </summary>
        /// <param name="i">arg 1</param>
        /// <param name="enumT1">arg 2</param>
        /// <param name="baseArg"> arg 3</param>
        public SampleDocumentedGenericClass(int i, IEnumerable<T1> enumT1, Base1ForSample baseArg) { }

        /// <summary>
        /// A public field
        /// </summary>
        /// <remarks> Optional Remarks</remarks>
        public int PublicField;

        /// <summary>
        /// Protected field
        /// </summary>
        /// <remarks> Optional Remarks</remarks>
        protected Base1ForSample ProtectedField;

        /// <summary>
        /// An indexer property with protexted setter
        /// </summary>
        /// <param name="x">indexer arg</param>
        /// <remarks> Optional Remarks</remarks>
        /// <returns>Return value</returns>
        public int this[string x] { get { return 0; } protected set { } }

        /// <summary>
        /// Another indexer property, this one without setter
        /// </summary>
        /// <param name="x">generic arg</param>
        /// <returns>generic return</returns>
        public T2 this[T1 x] { get { return default(T2); } }

        /// <summary>
        /// public property with getter and setter
        /// </summary>
        public string PublicProperty { get; set; }

        /// <summary>
        /// protected property without setter
        /// </summary>
        protected string ProtectedProperty { get; private set; }

        /// <summary>
        /// Void Method
        /// </summary>
        /// <remarks> Optional Remarks</remarks>
        public void VoidMethod() { }

        /// <summary>
        /// Generic method 
        /// </summary>
        /// <remarks> Optional Remarks</remarks>
        /// <typeparam name="T">generic type</typeparam>
        /// <param name="t1">parameter of class generic type</param>
        /// <param name="constrained"> complex parameter</param>
        public void GenericMethod<T>(T1 t1, Constrained<T, T2> constrained) { }

        /// <summary>
        /// Out modifier arg
        /// </summary>
        /// <remarks> Optional Remarks</remarks>
        /// <param name="Out"> output arg</param>
        public void OutArg(out int Out) { Out = 0; }

        /// <summary>
        /// Params arg
        /// </summary>
        /// <remarks> Optional Remarks</remarks>
        /// <param name="x"> first arg</param>
        /// <param name="p"> list of trailing args</param>
        public void Params(int x, params int[] p) { }

        /// <summary>
        /// Overloaded method
        /// </summary>
        /// <remarks> Optional Remarks</remarks>
        protected void ProtectedMethodWithOverload() { }

        /// <summary>
        /// Overload with int param
        /// </summary>
        /// <remarks> Optional Remarks</remarks>
        /// <param name="x">int param</param>
        protected void ProtectedMethodWithOverload(int x) { }

        /// <summary>
        /// This one throws <see cref="SampleException"/>
        /// </summary>
        /// <exception cref="SampleException">Yeah, this gets thrown.</exception>
        public void Throws() {

        }

        void IDisposable.Dispose() {
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public class SampleException : Exception { }

}
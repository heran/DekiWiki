/*
 * MindTouch DekiScript - embeddable web-oriented scripting runtime
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
using System.Collections;
using System.Xml;
using MindTouch.Deki.Script.Runtime;
using MindTouch.Deki.Script.Runtime.Library;
using MindTouch.Dream;
using MindTouch.Xml;

namespace MindTouch.Deki.Script.Expr {
    public abstract class DekiScriptLiteral : DekiScriptExpression {

        //--- Class Methods ---
        public static DekiScriptType AsScriptType(Type type) {

            // check if type is Nullable<T>
            if(type.IsGenericType && type.GetGenericTypeDefinition().Equals(typeof(Nullable<>))) {

                // use inner type
                type = Nullable.GetUnderlyingType(type);
            }

            // check type
            if(type == typeof(bool)) {
                return DekiScriptType.BOOL;
            } else if(
                (type == typeof(double)) || (type == typeof(float)) ||
                (type == typeof(long)) || (type == typeof(ulong)) ||
                (type == typeof(int)) || (type == typeof(uint)) ||
                (type == typeof(short)) || (type == typeof(ushort)) ||
                (type == typeof(sbyte)) || (type == typeof(byte)) ||
                (type == typeof(decimal))
                ) {
                return DekiScriptType.NUM;
            } else if((type == typeof(string)) || (type == typeof(DateTime))) {
                return DekiScriptType.STR;
            } else if(typeof(XDoc).IsAssignableFrom(type)) {
                return DekiScriptType.XML;
            } else if(typeof(XUri).IsAssignableFrom(type)) {
                return DekiScriptType.URI;
            } else if(typeof(Hashtable).IsAssignableFrom(type)) {
                return DekiScriptType.MAP;
            } else if(typeof(ArrayList).IsAssignableFrom(type)) {
                return DekiScriptType.LIST;
            } else if(type == typeof(System.DBNull)) {
                return DekiScriptType.NIL;
            } else if(type == typeof(object)) {
                return DekiScriptType.ANY;
            }
            throw new DekiScriptUnsupportedTypeException(Location.None, type.FullName);
        }

        public static string AsScriptTypeName(Type type, bool throwException) {
            if(!throwException) {
                try {
                    return AsScriptTypeName(AsScriptType(type));
                } catch(DekiScriptUnsupportedTypeException) {
                    return "#illegal";
                }
            }
            return AsScriptTypeName(AsScriptType(type));
        }

        public static string AsScriptTypeName(Type type) {
            return AsScriptTypeName(type, true);
        }

        public static string AsScriptTypeName(DekiScriptType type) {
            return type.ToString().ToLowerInvariant();
        }

        public static DekiScriptLiteral FromNativeValue(object value) {
            if(value is DekiScriptLiteral) {
                return (DekiScriptLiteral)value;
            }
            if(value != null) {
                switch(AsScriptType(value.GetType())) {
                case DekiScriptType.BOOL:
                    return Constant((bool)value);
                case DekiScriptType.NUM:
                    return Constant(SysUtil.ChangeType<double>(value));
                case DekiScriptType.STR:
                    if(value is DateTime) {
                        return Constant(DekiScriptLibrary.CultureDateTime((DateTime)value));
                    }
                    return Constant(value.ToString());
                case DekiScriptType.MAP:
                    return new DekiScriptMap((Hashtable)value);
                case DekiScriptType.LIST:
                    return new DekiScriptList((ArrayList)value);
                case DekiScriptType.XML:
                    return Constant((XDoc)value);
                case DekiScriptType.URI:
                    return Constant((XUri)value);
                }
            }
            return DekiScriptNil.Value;
        }

        public static DekiScriptLiteral FromXml(XDoc doc) {

            // check if response is an HTML document
            if(doc.HasName("html")) {

                // TODO (steveb): this handling seems to be to specific to belong here.

                return new DekiScriptList().Add(new DekiScriptXml(doc));
            }

            // check if response is a DekiScript XML document
            if(!doc.HasName("value") || (doc["@type"].AsText == null)) {
                throw new ArgumentException("doc");
            }
            switch(doc["@type"].AsText) {
            case "nil":
                return DekiScriptNil.Value;
            case "bool":
                return Constant(doc.AsBool ?? false);
            case "num":
                return Constant(doc.AsDouble ?? 0.0);
            case "str":
                return Constant(doc.AsText ?? string.Empty);
            case "uri": {
                return Constant(doc.AsUri);
            }
            case "map": {
                DekiScriptMap result = new DekiScriptMap();
                foreach(XDoc value in doc["value"]) {
                    result.Add(value["@key"].AsText, FromXml(value));
                }
                return result;
            }
            case "list": {
                DekiScriptList result = new DekiScriptList();
                foreach(XDoc value in doc["value"]) {
                    result.Add(FromXml(value));
                }
                return result;
            }
            case "xml":
                if((doc.AsXmlNode.ChildNodes.Count == 1) && (doc.AsXmlNode.ChildNodes[0].NodeType == XmlNodeType.Element)) {
                    return new DekiScriptXml(doc[doc.AsXmlNode.ChildNodes[0]]);
                }
                return DekiScriptNil.Value;
            default:
                throw new ArgumentException("doc");
            }
        }

        public static bool CoerceValuesToSameType(ref DekiScriptLiteral left, ref DekiScriptLiteral right) {

            // weed out the trivial case where the literals cannot be converted
            switch(left.ScriptType) {
            case DekiScriptType.NIL:
            case DekiScriptType.URI:
            case DekiScriptType.LIST:
            case DekiScriptType.MAP:
            case DekiScriptType.XML:

                // we can't convert complex literals; only succeed if the types match
                return left.ScriptType == right.ScriptType;
            }
            switch(right.ScriptType) {
            case DekiScriptType.NIL:
            case DekiScriptType.URI:
            case DekiScriptType.LIST:
            case DekiScriptType.MAP:
            case DekiScriptType.XML:

                // we can't convert complex literals; only succeed if the types match
                return left.ScriptType == right.ScriptType;
            }

            // now determine what needs to be converted
            switch(left.ScriptType) {
            case DekiScriptType.BOOL:
                switch(right.ScriptType) {
                case DekiScriptType.BOOL:

                    // nothing to do
                    return true;
                case DekiScriptType.NUM:

                    // convert left value from bool to number
                    left = Constant(left.AsNumber());
                    return true;
                case DekiScriptType.STR: {

                    // check if right string can be converted to bool; otherwise convert left bool to string
                    bool? value = right.AsBool();
                    if(value == null) {
                        left = Constant(left.AsString());
                    } else {
                        right = Constant(value);
                    }
                    return true;
                }
                }
                break;
            case DekiScriptType.NUM:
                switch(right.ScriptType) {
                case DekiScriptType.BOOL:

                    // convert right value from bool to number
                    right = Constant(right.AsNumber());
                    return true;
                case DekiScriptType.NUM:

                    // nothing to do
                    return true;
                case DekiScriptType.STR: {

                    // check if right string can be converted to number; otherwise convert left number to string
                    double? value = right.AsNumber();
                    if(value == null) {
                        left = Constant(left.AsString());
                    } else {
                        right = Constant(value);
                    }
                    return true;
                }
                }
                break;
            case DekiScriptType.STR:
                switch(right.ScriptType) {
                case DekiScriptType.BOOL: {

                    // check if left string can be converted to bool; otherwise convert right bool to string
                    bool? value = left.AsBool();
                    if(value == null) {
                        right = Constant(right.AsString());
                    } else {
                        left = Constant(value);
                    }
                    return true;
                }
                case DekiScriptType.NUM: {

                    // check if left string can be converted to number; otherwise convert right number to string
                    double? value = left.AsNumber();
                    if(value == null) {
                        right = Constant(right.AsString());
                    } else {
                        left = Constant(value);
                    }
                    return true;
                }
                case DekiScriptType.STR:

                    // nothing to do
                    return true;
                }
                break;
            }
            throw new InvalidOperationException(string.Format("invalid value pair: left = {0}, right = {1}", left.ScriptTypeName, right.ScriptTypeName));
        }

        //--- Abstract Properties ---
        public abstract DekiScriptType ScriptType { get; }
        public abstract object NativeValue { get; }

        //--- Properties ---
        public string ScriptTypeName { get { return ScriptType.ToString().ToLowerInvariant(); } }
        public bool IsNil { get { return this == DekiScriptNil.Value; } }
        public bool IsNilFalseZero { get { return (this == DekiScriptNil.Value) || (this == DekiScriptBool.False) || (this == DekiScriptNumber.Zero); } }

        //--- Abstract Methods ---
        public abstract bool? AsBool();
        public abstract double? AsNumber();
        public abstract string AsString();
        public abstract DekiScriptLiteral Convert(DekiScriptType type);
        public abstract XDoc AsEmbeddableXml(bool safe);

        //--- Methods ---
        public virtual void AppendXml(XDoc doc) { doc.Value(NativeValue); }
    }
}
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
using System.Collections.Generic;
using MindTouch.Deki.Script.Expr;
using MindTouch.Dream;
using MindTouch.Xml;

namespace MindTouch.Deki.Script.Runtime.TargetInvocation {
    public class DekiScriptXmlRpcInvocationTarget : ADekiScriptInvocationTarget {

        //--- Types ---
        internal class DekiScriptXmlRpcException : Exception {

            //--- Constructors ---
            public DekiScriptXmlRpcException(string message) : base(message) { }
        }

        //--- Class Methods ---
        private static void XmlRpcLiteralRecurse(XDoc xdoc, DekiScriptLiteral value, bool isArgumentList) {
            if(!isArgumentList) {
                xdoc.Start("value");
            }
            switch(value.ScriptType) {
            case DekiScriptType.BOOL:
                xdoc.Elem("boolean", ((DekiScriptBool)value).Value ? "1" : "0");
                break;
            case DekiScriptType.NUM:
                xdoc.Elem("double", ((DekiScriptNumber)value).Value);
                break;
            case DekiScriptType.STR:
                xdoc.Elem("string", ((DekiScriptString)value).Value); // in order to work with php, this may need to be encoded
                break;
            case DekiScriptType.NIL:
                xdoc.Elem("nil");
                break;
            case DekiScriptType.URI:
                xdoc.Start("string").Attr("type", "uri").End();
                break;
            case DekiScriptType.XML:
                xdoc.Start("string").Attr("type", "xml").Value(value.NativeValue.ToString()).End();
                break;
            case DekiScriptType.LIST:
                xdoc.Start(isArgumentList ? "params" : "array");
                if(!isArgumentList)
                    xdoc.Start("data");
                foreach(DekiScriptLiteral entry in ((DekiScriptList)value).Value) {
                    if(isArgumentList) {
                        xdoc.Start("param");
                        XmlRpcLiteralRecurse(xdoc, entry, false);
                        xdoc.End();
                    } else {
                        XmlRpcLiteralRecurse(xdoc, entry, false);
                    }
                }
                if(!isArgumentList)
                    xdoc.End();
                xdoc.End();
                break;
            case DekiScriptType.MAP:
                xdoc.Start("struct");
                foreach(KeyValuePair<string, DekiScriptLiteral> entry in ((DekiScriptMap)value).Value) {
                    xdoc.Start("member");
                    xdoc.Elem("name", entry.Key);
                    XmlRpcLiteralRecurse(xdoc, entry.Value, false);
                    xdoc.End();
                }
                xdoc.End();
                break;
            default:
                throw new ShouldNeverHappenException("unkwown type");
            }
            if(!isArgumentList)
                xdoc.End();
            return;
        }

        private static DekiScriptLiteral FromXmlRpcToDekiScript(XDoc xdoc) {
            if(xdoc.HasName("html")) {
                return new DekiScriptList().Add(new DekiScriptXml(xdoc));
            }
            if(xdoc.HasName("methodResponse")) {
                if(!xdoc["fault"].IsEmpty) {
                    string errorMessage = xdoc["fault/value/struct/member[name='faultString']/value/string"].AsText;
                    throw new DekiScriptXmlRpcException(errorMessage ?? xdoc.ToPrettyString());
                }
                if(!xdoc["params"].IsEmpty) {
                    DekiScriptList result = new DekiScriptList();
                    foreach(XDoc param in xdoc["params/param/value"]) {
                        result.Add(ToDekiScriptRecurse(param));
                    }
                    return result;
                } else {

                    // NOTE: unexpected result, treat it as a nil result

                    DekiScriptList result = new DekiScriptList();
                    result.Add(DekiScriptNil.Value);
                    return result;
                }
            }
            throw new DekiScriptUnsupportedTypeException(Location.None, string.Format("<{0}>", xdoc.Name));
        }

        private static DekiScriptLiteral ToDekiScriptRecurse(XDoc doc) {
            XDoc xdoc = doc.Elements;
            switch(xdoc.Name) {
            case "nil":
                return DekiScriptNil.Value;
            case "boolean":
                if(xdoc.Contents.GetType().Equals(typeof(String))) {
                    return DekiScriptExpression.Constant((xdoc.Contents.Contains("1") || xdoc.Contents.Contains("true")) ? true : false);
                }
                return DekiScriptExpression.Constant(xdoc.AsBool ?? false);
            case "double":
                return DekiScriptExpression.Constant(xdoc.AsDouble ?? 0.0);
            case "int":
            case "i4":
                return DekiScriptExpression.Constant(xdoc.AsDouble ?? 0.0);
            case "string":
                return DekiScriptExpression.Constant(xdoc.AsText ?? string.Empty);
            case "struct": {
                DekiScriptMap result = new DekiScriptMap();
                foreach(XDoc value in xdoc["member"]) {
                    result.Add(value["name"].Contents, ToDekiScriptRecurse(value["value"]));
                }
                return result;
            }
            case "array": {
                DekiScriptList result = new DekiScriptList();
                foreach(XDoc value in xdoc["data/value"]) {
                    result.Add(ToDekiScriptRecurse(value));
                }
                return result;
            }
            default:
                throw new ArgumentException("this type does not exist in the XML-RPC standard");
            }
        }

        public static XDoc DekiScriptToXmlRpc(string function, DekiScriptLiteral arguments) {
            XDoc xdoc = new XDoc("methodCall");
            xdoc.Elem("methodName", function);
            if(arguments.ScriptType.Equals(DekiScriptType.LIST)) {
                XmlRpcLiteralRecurse(xdoc, arguments, true);
            } else {
                xdoc.Start("params").Start("param");
                XmlRpcLiteralRecurse(xdoc, arguments, false);
                xdoc.End().End();
            }
            return xdoc;
        }
        
        //--- Fields ---
        private readonly XUri _endpoint;
        private readonly string _methodname;

        //--- Constructors ---
        public DekiScriptXmlRpcInvocationTarget(XUri endpoint, string methodname) {
            if(endpoint == null) {
                throw new ArgumentNullException("endpoint");
            }
            if(string.IsNullOrEmpty(methodname)) {
                throw new ArgumentNullException("methodname");
            }
            _endpoint = endpoint;
            _methodname = methodname;
        }

        //--- Methods ---
        public override DekiScriptLiteral InvokeList(DekiScriptRuntime runtime, DekiScriptList args) {

            // prepare uri for invocation
            Plug plug = Plug.New(_endpoint);
            plug = runtime.PreparePlug(plug);
            DreamMessage response = plug.Post(DekiScriptToXmlRpc(_methodname, args));

            // convert response to literal
            DekiScriptLiteral list = FromXmlRpcToDekiScript(response.ToDocument());
            if(list.ScriptType != DekiScriptType.LIST) {
                throw new DekiScriptBadReturnTypeException(Location.None, list.ScriptType, new[] { DekiScriptType.LIST });
            }
            return ((DekiScriptList)list)[0];
        }

        public override DekiScriptLiteral InvokeMap(DekiScriptRuntime runtime, DekiScriptMap args) {
            throw new DekiScriptBadTypeException(Location.None, args.ScriptType, new[] { DekiScriptType.LIST });
        }
    }
}
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

using MindTouch.Deki.Script.Expr;
using MindTouch.Dream;
using MindTouch.Xml;

namespace MindTouch.Deki.Script.Runtime.TargetInvocation {
    public class DekiScriptInvocationTargetDescriptor {

        //--- Constructors ---
        public DekiScriptInvocationTargetDescriptor(DreamAccess access, bool isProperty, bool isIdempotent, string name, DekiScriptParameter[] parameters, DekiScriptType returnType, string description, string transform, IDekiScriptInvocationTarget target) {
            this.Access = access;
            this.IsProperty = isProperty;
            this.IsIdempotent = isIdempotent;
            this.Name = name;
            this.Parameters = parameters;
            this.ReturnType = returnType;
            this.Description = description;
            this.Transform = transform;
            this.Target = target;
        }

        //--- Properties ---
        public string Name { get; private set; }
        public DreamAccess Access { get; private set; }
        public string Description { get; private set; }
        public string Transform { get; private set; }
        public bool IsProperty { get; private set; }
        public bool IsIdempotent { get; private set; }
        public DekiScriptParameter[] Parameters { get; private set; }
        public DekiScriptType ReturnType { get; private set; }
        public IDekiScriptInvocationTarget Target { get; private set; }

        public string SystemName {
            get {
                if(IsProperty) {
                    return "$" + Name;
                }
                return Name;
            }
        }

        //--- Methods ---
        public XDoc ToXml(XUri uri) {
            XDoc result = new XDoc("function");
            result.Attr("transform", Transform);
            if(IsProperty) {
                result.Attr("usage", "property");
            }
            result.Elem("name", Name);
            result.Elem("uri", uri);
            result.Elem("description", Description);
            if(Access != DreamAccess.Public) {
                result.Elem("access", Access.ToString().ToLowerInvariant());
            }
            foreach(DekiScriptParameter param in Parameters) {
                param.AppendXml(result);
            }
            result.Start("return").Attr("type", DekiScriptLiteral.AsScriptTypeName(ReturnType)).End();
            return result;
        }
    }
}
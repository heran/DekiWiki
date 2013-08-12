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
using MindTouch.Xml;

namespace MindTouch.Deki.Script.Tests.Util {
    internal class FunctionDefinition : IFunctionDefinition {

        //--- Types ---
        public class Parameter {
            public string Type;
            public string Default;
        }

        //--- Fields ---
        public readonly string Name;

        private readonly TestScriptService _scriptService;
        private string _body;
        private XDoc _htmlBody;
        private string _type;
        private string _access;
        private readonly Dictionary<string, Parameter> _parameters = new Dictionary<string, Parameter>();

        //--- Constructors ---
        public FunctionDefinition(TestScriptService scriptService, string name) {
            _scriptService = scriptService;
            Name = name;
        }

        //--- Methods ---
        public IFunctionDefinition Internal() {
            _access = "internal";
            return this;
        }

        public IFunctionDefinition Private() {
            _access = "private";
            return this;
        }

        public IFunctionDefinition Param(string name, string type, string @default) {
            _parameters[name] = new Parameter { Type = type, Default = @default };
            return this;
        }

        public IFunctionDefinition Param(string name, string type) {
            return Param(name, type, null);
        }

        public IFunctionDefinition Param(string name) {
            return Param(name, "any", null);
        }

        public IFunctionDefinition Body(string body) {
            _body = body;
            return this;
        }

        public ITestScriptService Returns(string type) {
            _type = type;
            _scriptService.Add(this);
            return _scriptService;
        }

        public ITestScriptService ReturnsAny() {
            return Returns("any");
        }

        public ITestScriptService ReturnsNil() {
            return Returns("nil");
        }

        public ITestScriptService ReturnsXml() {
            return Returns("xml");
        }

        public ITestScriptService ReturnsNum() {
            return Returns("num");
        }

        public ITestScriptService ReturnsStr() {
            return Returns("str");
        }

        public ITestScriptService ReturnsBool() {
            return Returns("bool");
        }

        public void ToXml(XDoc parent) {
            parent
                .Start("function")
                    .Elem("name", Name);
            if(!string.IsNullOrEmpty(_access)) {
                parent.Elem("access", _access);
            }
            foreach(var parameter in _parameters) {
                parent
                    .Start("param")
                        .Attr("name", parameter.Key)
                        .Attr("type", parameter.Value.Type);
                if(!string.IsNullOrEmpty(parameter.Value.Default)) {
                    parent.Attr("default", parameter.Value.Default);
                }
                parent.End();
            }
            parent
                    .Start("return")
                        .Attr("type", _type);
            if(_htmlBody != null) {
                parent.AddAll(_htmlBody);
            } else {
                parent.Value(_body);
            }
            parent
                    .End()
                .End();
        }
    }
}
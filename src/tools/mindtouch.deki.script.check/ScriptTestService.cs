/*
 * MindTouch Core - open source enterprise collaborative networking
 * Copyright (c) 2006-2010 MindTouch Inc.
 * www.mindtouch.com  oss@mindtouch.com
 *
 * For community documentation and downloads visit developer.mindtouch.com;
 * please review the licensing section.
 *
 * This library is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 2.1 of the License, or (at your option) any later version.
 * 
 * This library is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 * 
 * You should have received a copy of the GNU Lesser General Public
 * License along with this library; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
 * http://www.gnu.org/copyleft/lesser.html
 */

using System;
using MindTouch.Deki.Script.Compiler;
using MindTouch.Deki.Script.Expr;
using MindTouch.Deki.Script.Runtime;
using MindTouch.Dream;
using MindTouch.Tasking;
using MindTouch.Xml;

namespace MindTouch.Deki.Script {
    using Yield = System.Collections.Generic.IEnumerator<IYield>;

    [DreamService("MindTouch Script Test ", "Copyright (c) 2006-2010 MindTouch Inc.",
        SID = new string[] { "sid://mindtouch.com/2008/09/script-test" }
    )]
    class ScriptTestService : DreamService {
        private DekiScriptRuntime _runtime;
        private DekiScriptEnv _env;

        protected override Yield Start(XDoc config, Result result) {
            yield return Coroutine.Invoke(base.Start, config, new Result());

            // init environment for script execution
            _runtime = new DekiScriptRuntime();
            _env = _runtime.CreateEnv();
            result.Return();
        }

        [DreamFeature("POST:register", "Register a script extension")]
        [DreamFeatureParam("service-path", "str", "Path to the service to register")]
        public Yield Register(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            string servicePath = context.GetParam("service-path");
            string location = StringUtil.CreateAlphaNumericKey(8);

            // register the script
            XDoc config = new XDoc("config")
                .Elem("manifest", servicePath)
                .Elem("debug", true);

            //create the script service
            Result<Plug> res;
            yield return res = CreateService(location, "sid://mindtouch.com/2007/12/dekiscript", config, new Result<Plug>());
            Plug service = res.Value;

            // register script functions in environment
            XDoc manifest = service.Get().ToDocument();
            string ns = manifest["namespace"].AsText;
            foreach(XDoc function in manifest["function"]) {
                string name = function["name"].AsText;
                if(string.IsNullOrEmpty(ns)) {
                    _env.Vars.AddNativeValueAt(name, function["uri"].AsUri);
                } else {
                    _env.Vars.AddNativeValueAt(ns + "." + name, function["uri"].AsUri);
                }
            }
            response.Return(DreamMessage.Ok(MimeType.XML, manifest));
        }

        [DreamFeature("POST:execute", "Register a script extension")]
        [DreamFeatureParam("expression", "str", "Expression to execute")]
        public Yield Execute(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            string expression = context.GetParam("expression");
            DekiScriptExpression expr = DekiScriptParser.Parse(new Location("POST:execute"), expression);
            DekiScriptLiteral result = _runtime.Evaluate(expr, DekiScriptEvalMode.Evaluate, _env);
            if(result.ScriptType == DekiScriptType.XML) {
                response.Return(DreamMessage.Ok(MimeType.XML, (XDoc)result.NativeValue));
            } else {
                response.Return(DreamMessage.Ok(MimeType.TEXT, result.ToString()));
            }
            yield break;
        }

    }
}

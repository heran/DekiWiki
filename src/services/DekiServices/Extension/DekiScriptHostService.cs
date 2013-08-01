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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using log4net;
using MindTouch.Deki.Script;
using MindTouch.Deki.Script.Compiler;
using MindTouch.Deki.Script.Expr;
using MindTouch.Deki.Script.Runtime;
using MindTouch.Deki.Script.Runtime.Library;
using MindTouch.Deki.Script.Runtime.TargetInvocation;
using MindTouch.Dream;
using MindTouch.Tasking;
using MindTouch.Web;
using MindTouch.Xml;

namespace MindTouch.Deki {
    using Yield = IEnumerator<IYield>;

    [DreamService("MindTouch DekiScript Container", "Copyright (c) 2006-2010 MindTouch Inc.",
        Info = "http://developer.mindtouch.com/App_Catalog/DekiScript",
        SID = new[] { 
            "sid://mindtouch.com/2007/12/dekiscript",
            "http://services.mindtouch.com/deki/draft/2007/12/dekiscript" 
        }
    )]
    [DreamServiceConfig("manifest", "string|uri", "Location of service manifest file")]
    [DreamServiceConfig("resources", "(string|uri)?", "Location resource files (default: same location as manifest)")]
    [DreamServiceConfig("debug", "void?", "Reload manifest for every invocation")]
    [DreamServiceConfig("dekiwiki-signature", "string?", "Puglic Digital Signature key to verify incoming requests from MindTouch. (default: none)")]
    [DreamServiceBlueprint("deki/service-type", "extension")]
    public class DekiScriptHostService : DreamService {

        //--- Types ---
        internal class DekiScriptScriptFunctionInvocationTarget : ADekiScriptInvocationTarget {

            //--- Fields ---
            private readonly DekiScriptEnv _env;
            private readonly DekiScriptType _returnType;

            //--- Constructors ---
            public DekiScriptScriptFunctionInvocationTarget(DreamAccess access, DekiScriptParameter[] parameters, DekiScriptExpression expr, DekiScriptEnv env, DekiScriptType returnType) {
                if(parameters == null) {
                    throw new ArgumentNullException("parameters");
                }
                if(expr == null) {
                    throw new ArgumentNullException("expr");
                }
                this.Access = access;
                this.Parameters = parameters;
                this.Expression = expr;
                _env = env;
                _returnType = returnType;
            }

            //--- Properties ---
            public DreamAccess Access { get; private set; }
            public DekiScriptParameter[] Parameters { get; private set; }
            public DekiScriptExpression Expression { get; private set; }

            //--- Methods ---
            public override DekiScriptLiteral InvokeList(DekiScriptRuntime runtime, DekiScriptList args) {
                return InvokeHelper(runtime, DekiScriptParameter.ValidateToMap(Parameters, args));
            }

            public override DekiScriptLiteral InvokeMap(DekiScriptRuntime runtime, DekiScriptMap args) {
                return InvokeHelper(runtime, DekiScriptParameter.ValidateToMap(Parameters, args));
            }

            private DekiScriptLiteral InvokeHelper(DekiScriptRuntime runtime, DekiScriptMap args) {

                // invoke script
                DekiScriptEnv env = _env.NewScope();
                env.Vars.AddRange(DreamContext.Current.GetState<DekiScriptMap>("env.implicit"));
                env.Vars.Add("args", args);
                env.Vars.Add("$", args);
                var result = runtime.Evaluate(Expression, DekiScriptEvalMode.Evaluate, env);
                try {
                    return result.Convert(_returnType);
                } catch(DekiScriptInvalidCastException) {
                    throw new DekiScriptInvalidReturnCastException(Location.None, result.ScriptType, _returnType);
                }
            }
        }

        //--- Class Fields ---
        private static readonly ILog _log = LogUtils.CreateLog();

        //--- Fields ---
        private string _manifestPath;
        private string _resourcesPath;
        private XUri _manifestUri;
        private XUri _resourcesUri;
        private XDoc _manifest;
        private bool _debug;
        private DSACryptoServiceProvider _publicDigitalSignature;
        private string _title;
        private string _label;
        private string _copyright;
        private string _description;
        private string _help;
        private string _logo;
        private string _namespace;
        private DekiScriptRuntime _runtime;
        private Dictionary<XUri, DekiScriptInvocationTargetDescriptor> _functions;
        private DekiScriptEnv _commonEnv;

        //--- Properties ---
        protected DekiScriptRuntime ScriptRuntime { get { return _runtime; } }

        //--- Features ---
        [DreamFeature("GET:", "Retrieve extension description")]
        public Yield GetExtensionLibrary(DreamContext contex, DreamMessage request, Result<DreamMessage> response) {

            // create manifest
            XDoc manifest = new XDoc("extension");
            manifest.Elem("title", _title);
            manifest.Elem("label", _label);
            manifest.Elem("copyright", _copyright);
            manifest.Elem("description", _description);
            manifest.Elem("uri.help", _help);
            manifest.Elem("uri.logo", _logo);
            manifest.Elem("namespace", _namespace);

            // add functions
            foreach(var function in _functions) {
                if(function.Value.Access == DreamAccess.Public) {
                    manifest.Add(function.Value.ToXml(function.Key));
                }
            }
            response.Return(DreamMessage.Ok(manifest));
            yield break;
        }

        [DreamFeature("POST:{function}", "Invoke extension function")]
        public Yield PostExtensionFunction(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            string name = context.GetParam("function");

            // check if we need to reload the manifest
            if(_debug) {
                LoadScript();
            }

            // find function
            DekiScriptInvocationTargetDescriptor descriptor;
            if(!_functions.TryGetValue(Self.At(name), out descriptor)) {
                response.Return(DreamMessage.NotFound(string.Format("function {0} not found", name)));
                yield break;
            }

            // set optional culture from request
            context.Culture = HttpUtil.GetCultureInfoFromHeader(request.Headers.AcceptLanguage, context.Culture);

            // read input arguments for invocation
            DekiScriptLiteral args = DekiScriptLiteral.FromXml(request.ToDocument());

            // create custom environment
            var implicitEnv = DekiExtService.GetImplicitEnvironment(request, _publicDigitalSignature);
            implicitEnv.AddNativeValueAt("self", Self.Uri.AsPublicUri().ToString());
            DreamContext.Current.SetState("env.implicit", implicitEnv);

            // invoke target
            DekiScriptLiteral eval;
            try {
                eval = descriptor.Target.Invoke(ScriptRuntime, args);
            } catch(Exception e) {
                response.Return(DreamMessage.InternalError(e));
                yield break;
            }

            // check if response is embeddable XML
            if(eval is DekiScriptXml) {
                XDoc doc = ((DekiScriptXml)eval).Value;
                if(doc.HasName("html")) {

                    // replace all self: references
                    foreach(XDoc item in doc[".//*[starts-with(@src, 'self:')]/@src | .//*[starts-with(@href, 'self:')]/@href"]) {
                        try {
                            item.ReplaceValue(Self.AtPath(item.Contents.Substring(5)));
                        } catch { }
                    }
                }
            }

            // process response
            DekiScriptList result = new DekiScriptList().Add(eval);
            response.Return(DreamMessage.Ok(result.ToXml()));
        }

        [DreamFeature("GET:*//*", "Retrieve a file")]
        [DreamFeature("HEAD:*//*", "Retrieve file headers")]
        public Yield GetFiles(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            if(_resourcesUri != null) {
                yield return context.Relay(Plug.New(_resourcesUri).AtPath(string.Join("/", context.GetSuffixes(UriPathFormat.Original))), request, response);
            } else {
                response.Return(GetFile(context));
            }
            yield break;
        }

        //--- Methods ---
        protected override Yield Start(XDoc config, Result result) {
            yield return Coroutine.Invoke(base.Start, config, new Result());

            // initialize debug mode
            string debug = config["debug"].AsText;
            _debug = (debug != null) && !debug.EqualsInvariantIgnoreCase("false");

            // check if a public digital signature key was provided
            string dsaKey = config["dekiwiki-signature"].AsText;
            if(dsaKey != null) {
                try {
                    DSACryptoServiceProvider dsa = new DSACryptoServiceProvider();
                    dsa.ImportCspBlob(Convert.FromBase64String(dsaKey));
                    _publicDigitalSignature = dsa;
                } catch {
                    throw new ArgumentException("invalid digital signature provided", "dekiwiki-signature");
                }
            }

            // load script
            LoadScript();
            result.Return();
        }

        protected override Yield Stop(Result result) {
            _publicDigitalSignature = null;
            _manifestPath = null;
            _resourcesPath = null;
            _manifestUri = null;
            _resourcesUri = null;
            _manifest = null;
            _debug = false;
            _runtime = null;
            _commonEnv = null;
            _copyright = null;
            _description = null;
            _functions = null;
            _help = null;
            _label = null;
            _logo = null;
            _namespace = null;
            _title = null;
            yield return Coroutine.Invoke(base.Stop, new Result());
            result.Return();
        }

        private void LoadScript() {
            _manifestPath = null;
            _resourcesPath = null;
            _manifestUri = null;
            _resourcesUri = null;
            _manifest = null;

            // read manifest
            _manifestPath = Config["manifest"].AsText;
            if(string.IsNullOrEmpty(_manifestPath)) {
                throw new ArgumentNullException("manifest");
            }
            _manifestUri = XUri.TryParse(_manifestPath);
            if(_manifestUri != null) {
                _manifestPath = null;
                _manifest = Plug.New(_manifestUri).Get().ToDocument();
                _resourcesUri = Config["resources"].AsUri ?? _manifestUri.WithoutLastSegment();
            } else {
                _manifest = XDocFactory.LoadFrom(_manifestPath, MimeType.XML);
                _resourcesPath = Config["resources"].AsText ?? Path.GetDirectoryName(_manifestPath);
            }
            if(!_manifest.HasName("extension")) {
                throw new ArgumentException("invalid extension manifest");
            }

            // initilize runtime
            _runtime = new DekiScriptRuntime();

            // read manifest settings
            _title = _manifest["title"].AsText;
            _label = _manifest["label"].AsText;
            _copyright = _manifest["copyright"].AsText;
            _description = _manifest["description"].AsText;
            _help = _manifest["uri.help"].AsText;
            _logo = _manifest["uri.logo"].AsText;
            _namespace = _manifest["namespace"].AsText;

            // initialize evaluation environment
            _commonEnv = _runtime.CreateEnv();

            // read functions
            _functions = new Dictionary<XUri, DekiScriptInvocationTargetDescriptor>();
            foreach(var function in _manifest["function"]) {
                var descriptor = ConvertFunction(function);
                if(descriptor != null) {
                    var uri = Self.At(descriptor.SystemName);
                    DekiScriptInvocationTargetDescriptor old;
                    if(_functions.TryGetValue(uri, out old)) {
                        _log.WarnFormat("duplicate function name {0} in script {1}", descriptor.Name, _manifestUri);
                    }
                    _functions[uri] = descriptor;
                }
            }
            _runtime.RegisterExtensionFunctions(_functions);

            // add extension functions to env
            foreach(var function in _functions) {
                _commonEnv.Vars.AddNativeValueAt(function.Value.Name.ToLowerInvariant(), function.Key);
            }

            // add configuration settings
            DreamContext context = DreamContext.Current;
            DekiScriptMap scriptConfig = new DekiScriptMap();
            foreach(KeyValuePair<string, string> entry in Config.ToKeyValuePairs()) {
                XUri local;
                if(XUri.TryParse(entry.Value, out local)) {
                    local = context.AsPublicUri(local);
                    scriptConfig.AddAt(entry.Key.Split('/'), DekiScriptExpression.Constant(local.ToString()));
                } else {
                    scriptConfig.AddAt(entry.Key.Split('/'), DekiScriptExpression.Constant(entry.Value));
                }
            }
            _commonEnv.Vars.Add("config", scriptConfig);
        }

        private DreamMessage GetFile(DreamContext context) {
            DreamMessage message;
            string[] parts = context.GetSuffixes(UriPathFormat.Decoded);
            string filename = _resourcesPath;
            foreach(string part in parts) {
                if(part.EqualsInvariant("..")) {
                    _log.WarnFormat("attempted to access file outside of target folder: {0}", string.Join("/", parts));
                    throw new DreamBadRequestException("paths cannot contain '..'");
                }
                filename = Path.Combine(filename, part);
            }
            try {
                message = DreamMessage.FromFile(filename, context.Verb == Verb.HEAD);
            } catch(FileNotFoundException e) {
                message = DreamMessage.NotFound("resource not found: " + String.Join("/", context.GetSuffixes(UriPathFormat.Decoded)));
            } catch(Exception e) {
                message = DreamMessage.BadRequest("invalid path");
            }
            return message;
        }

        private DekiScriptInvocationTargetDescriptor ConvertFunction(XDoc function) {
            string functionName = function["name"].AsText;
            if(string.IsNullOrEmpty(functionName)) {
                _log.WarnFormat("function without name in script {0}; skipping function definition", _manifestUri);
                return null;
            }

            // determine function access level
            DreamAccess access;
            switch(function["access"].AsText ?? "public") {
            case "private":
                access = DreamAccess.Private;
                break;
            case "internal":
                access = DreamAccess.Internal;
                break;
            case "public":
                access = DreamAccess.Public;
                break;
            default:
                _log.WarnFormat("unrecognized access level '{0}' for function {1} in script {2}; defaulting to public", function["access"].AsText, functionName, _manifestUri);
                access = DreamAccess.Public;
                break;
            }

            // convert parameters
            List<DekiScriptParameter> parameters = new List<DekiScriptParameter>();
            foreach(XDoc param in function["param"]) {
                string paramName = param["@name"].AsText;

                // determine if parameter has a default value
                string paramDefault = param["@default"].AsText;
                DekiScriptLiteral paramDefaultExpression = DekiScriptNil.Value;
                bool paramOptional = false;
                if(paramDefault != null) {
                    paramOptional = true;
                    try {
                        paramDefaultExpression = ScriptRuntime.Evaluate(DekiScriptParser.Parse(Location.Start, paramDefault), DekiScriptEvalMode.Evaluate, ScriptRuntime.CreateEnv());
                    } catch(Exception e) {
                        _log.ErrorExceptionFormat(e, "invalid default value for parameter {0} in function {1} in script {2}; skipping function definition", paramName, functionName, _manifestUri);
                        return null;
                    }
                } else {
                    paramOptional = (param["@optional"].AsText == "true");
                }

                // determine parameter type
                string paramType = param["@type"].AsText ?? "any";
                DekiScriptType paramScriptType;
                if(!SysUtil.TryParseEnum(paramType, out paramScriptType)) {
                    _log.WarnFormat("unrecognized param type '{0}' for parameter {1} in function {2} in script {3}; defaulting to any", paramType, paramName, functionName, _manifestUri);
                    paramScriptType = DekiScriptType.ANY;
                }

                // add parameter
                parameters.Add(new DekiScriptParameter(paramName, paramScriptType, paramOptional, param.Contents, typeof(object), paramDefaultExpression));
            }
            var parameterArray = parameters.ToArray();

            // determine function body
            XDoc ret = function["return"];
            string src = ret["@src"].AsText;
            string type = ret["@type"].AsText;
            DekiScriptExpression expression;
            if(!string.IsNullOrEmpty(src)) {

                // 'src' attribute is set, load the script from it
                XDoc script;
                if(_manifestUri != null) {

                    // check if uri is relative
                    XUri scriptUri = XUri.TryParse(src) ?? _manifestUri.AtPath(src);
                    script = Plug.New(scriptUri).Get().ToDocument();
                } else {

                    // check if filename is relative
                    if(!Path.IsPathRooted(src)) {
                        src = Path.Combine(_resourcesPath, src);
                    }
                    script = XDocFactory.LoadFrom(src, MimeType.XML);
                }
                expression = DekiScriptParser.Parse(script);
                type = type ?? "xml";
            } else if(!ret["html"].IsEmpty) {

                // <return> element contains a <html> node; parse it as a script
                expression = DekiScriptParser.Parse(ret["html"]);
                type = type ?? "xml";
            } else if(!ret.IsEmpty) {

                // <return> element contains something else; use the text contents as deki-script expression
                var location = new Location(string.Format("/function[name={0}]/return", functionName));
                expression = DekiScriptParser.Parse(location, function["return"].AsText ?? string.Empty);
                expression = DekiScriptExpression.ReturnScope(location, expression);
                type = type ?? "any";
            } else {
                _log.WarnFormat("function {0} has no body in script {1}; skipping function definition", functionName, _manifestUri);
                return null;
            }

            // determine return type
            DekiScriptType returnScriptType;
            if(!SysUtil.TryParseEnum(type, out returnScriptType)) {
                _log.WarnFormat("unrecognized return type '{0}' for function {1} in script {2}; defaulting to any", type, functionName, _manifestUri);
                returnScriptType = DekiScriptType.ANY;
            }

            // create function descriptor
            var target = new DekiScriptScriptFunctionInvocationTarget(access, parameterArray, expression, _commonEnv, returnScriptType);
            string description = function["description"].AsText;
            string transform = function["@transform"].AsText;
            return new DekiScriptInvocationTargetDescriptor(access, false, false, functionName, parameterArray, returnScriptType, description, transform, target);
        }
    }
}

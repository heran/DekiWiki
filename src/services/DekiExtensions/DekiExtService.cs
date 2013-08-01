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
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

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

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class DekiExtLibraryAttribute : Attribute {

        //--- Fields ---
        public string Namespace;
        public string Description;
        public string Label;
        public string Logo;
        private XUri _license;

        //--- Properties ---
        public XUri LicenseUri { get { return _license; } }

        public string License {
            get { return (_license != null) ? _license.ToString() : null; }
            set { _license = (value != null) ? new XUri(value) : null; }
        }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class DekiExtLibraryFilesAttribute : Attribute {

        //--- Fields ---
        public string[] Filenames = StringUtil.EmptyArray;
        public string Prefix;
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class DekiExtFunctionAttribute : Attribute {

        //--- Fields ---
        public string Name;
        public string Description;
        public string Transform;
        public bool IsProperty;

        //--- Constructors ---
        public DekiExtFunctionAttribute() { }

        public DekiExtFunctionAttribute(string name) {
            this.Name = name;
        }

        public DekiExtFunctionAttribute(string name, string description) {
            this.Name = name;
            this.Description = description;
        }
    }

    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    public class DekiExtParamAttribute : Attribute {

        //--- Fields ---
        public readonly string Hint;
        public readonly bool Optional;

        //--- Constructors ---
        public DekiExtParamAttribute(string hint) : this(hint, false) { }

        public DekiExtParamAttribute(string hint, bool optional) {
            this.Hint = hint;
            this.Optional = optional;
        }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class DekiExtFunctionScriptAttribute : Attribute {

        //--- Fields ---
        public string Prefix;
        public string Scriptname;

        //--- Constructors ---
        public DekiExtFunctionScriptAttribute(string prefix, string scriptname) {
            this.Prefix = prefix;
            this.Scriptname = scriptname;
        }

        public DekiExtFunctionScriptAttribute(string scriptname) {
            this.Scriptname = scriptname;
        }
    }

    [DreamServiceConfig("deki-signature", "string?", "Puglic Digital Signature key to verify incoming requests from MindTouch. (default: none)")]
    public abstract class DekiExtService : DreamService {

        //--- Constants ---
        public const string DEKI_HEADER = "X-Deki";
        public const string IMPLICIT_ENVIRONMENT_HEADER = "X-DekiScript-Env";
        public const string IMPLICIT_SIGNATURE_HEADER = "X-DekiScript-DSig";

        //--- Class Methods ---
        public static string AsSize(float? size) {
            return DekiScriptLibrary.WebSize(size);
        }

        public static DekiScriptMap GetImplicitEnvironment(DreamMessage message, DSACryptoServiceProvider publicDigitalSignature) {
            DekiScriptMap env = new DekiScriptMap();

            // retrieve implicit arguments
            string[] headers = message.Headers.GetValues(IMPLICIT_ENVIRONMENT_HEADER);
            if(!ArrayUtil.IsNullOrEmpty(headers)) {
                env.AddAt("__implicit", new DekiScriptList(new ArrayList(headers)));
                foreach(string implicitArg in headers) {
                    foreach(KeyValuePair<string, string> arg in HttpUtil.ParseNameValuePairs(implicitArg)) {
                        env.AddNativeValueAt(arg.Key, arg.Value);
                    }
                }
            }
            if(publicDigitalSignature != null) {
                bool valid = false;
                try {
                    Dictionary<string, string> values = HttpUtil.ParseNameValuePairs(message.Headers[IMPLICIT_SIGNATURE_HEADER]);

                    // verify date
                    DateTime date = DateTime.Parse(values["date"]).ToUniversalTime();
                    double delta = DateTime.UtcNow.Subtract(date).TotalSeconds;
                    if((delta < -60) || (delta > 60)) {
                        throw new DreamAbortException(DreamMessage.Forbidden("date in message signature is too far apart from server date"));
                    }

                    // verify message
                    MemoryStream data = new MemoryStream();
                    byte[] bytes = null;

                    // get message bytes
                    bytes = message.AsBytes();
                    data.Write(bytes, 0, bytes.Length);

                    // retrieve headers to verify
                    if(!ArrayUtil.IsNullOrEmpty(headers)) {
                        Array.Sort(headers, StringComparer.Ordinal);
                        bytes = Encoding.UTF8.GetBytes(string.Join(",", headers));
                        data.Write(bytes, 0, bytes.Length);
                    }

                    // add request date
                    bytes = Encoding.UTF8.GetBytes(values["date"]);
                    data.Write(bytes, 0, bytes.Length);

                    // verify signature
                    byte[] signature = Convert.FromBase64String(values["dsig"]);
                    valid = publicDigitalSignature.VerifyData(data.GetBuffer(), signature);
                } catch(Exception e) {
                    if(e is DreamAbortException) {
                        throw;
                    }
                }
                if(!valid) {
                    throw new DreamAbortException(DreamMessage.Forbidden("invalid or missing digital signature"));
                }
            }
            return env;
        }

        //--- Fields ---
        private readonly Dictionary<XUri, DekiScriptInvocationTargetDescriptor> _functions = new Dictionary<XUri, DekiScriptInvocationTargetDescriptor>();
        private readonly Dictionary<string, Plug> _files = new Dictionary<string, Plug>();
        private DSACryptoServiceProvider _publicDigitalSignature;
        private DekiScriptMap _scriptConfig;

        // Only InitializeRuntimeAndEnvironment should ever access these two by hand, all other access needs to use
        // ScriptRuntime and CreateEnvironment()
        private DekiScriptRuntime _runtime;
        private DekiScriptEnv _scriptEnv;

        //--- Property ---
        public Plug Files { get { return Self.At("$files"); } }
        public Plug Functions { get { return Self; } }
        public Dictionary<XUri, DekiScriptInvocationTargetDescriptor> RegisteredFunctions { get { return _functions; } }

        protected virtual DekiScriptRuntime ScriptRuntime {
            get {
                InitializeRuntimeAndEnvironment();
                return _runtime;
            }
        }

        //--- Features ---
        [DreamFeature("GET:", "Retrieve extension description")]
        public Yield GetExtensionLibrary(DreamContext contex, DreamMessage request, Result<DreamMessage> response) {
            XDoc result = new XDoc("extension");
            DreamServiceAttribute service = (DreamServiceAttribute)Attribute.GetCustomAttribute(GetType(), typeof(DreamServiceAttribute));
            DekiExtLibraryAttribute library = (DekiExtLibraryAttribute)Attribute.GetCustomAttribute(GetType(), typeof(DekiExtLibraryAttribute));
            result.Elem("title", service.Name);
            if(service.Copyright != null) {
                result.Elem("copyright", service.Copyright);
            }
            if(service.Info != null) {
                result.Elem("uri.help", service.Info);
            }
            if(library != null) {
                if(!string.IsNullOrEmpty(library.Label)) {
                    result.Elem("label", library.Label);
                }
                if(!string.IsNullOrEmpty(library.Logo)) {
                    XUri logo;
                    if(XUri.TryParse(library.Logo, out logo)) {
                        result.Elem("uri.logo", logo);
                    } else {
                        result.Elem("uri.logo", Self.AtPath(library.Logo).Uri);
                    }
                }
                if(!string.IsNullOrEmpty(library.Namespace)) {
                    result.Elem("namespace", library.Namespace);
                }
                if(!string.IsNullOrEmpty(library.Description)) {
                    result.Elem("description", library.Description);
                }
                if(library.LicenseUri != null) {
                    result.Elem("uri.license", library.LicenseUri);
                }
            }
            foreach(var function in _functions.Values) {
                if(function.Access == DreamAccess.Public) {
                    result.Add(function.ToXml(Functions.At(function.SystemName)));
                }
            }
            response.Return(DreamMessage.Ok(result));
            yield break;
        }

        [DreamFeature("POST:{function}", "Invoke extension function")]
        public Yield PostExtensionFunction(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            var name = context.GetParam("function");
            XUri uri = Self.At(name);

            // check if deki server is permitted to invoke this function
            if(this is IDreamServiceLicense) {
                string deki;
                if(context.ServiceLicense.TryGetValue("deki", out deki) && !deki.EqualsInvariant(request.Headers[DEKI_HEADER] ?? string.Empty)) {
                    throw new DreamAbortException(DreamMessage.Forbidden("deki server is not licensed for this service"));
                }
            }

            // check if any functions were found
            DekiScriptInvocationTargetDescriptor descriptor;
            if(!_functions.TryGetValue(uri, out descriptor)) {
                response.Return(DreamMessage.NotFound(string.Format("function {0} not found", context.GetParam("function"))));
                yield break;
            }

            // check if invoker has access to function
            if((descriptor.Access != DreamAccess.Public) && (descriptor.Access > DetermineAccess(context, request))) {
                response.Return(DreamMessage.Forbidden("insufficient access privileges"));
                yield break;
            }

            // check if request has a requested culture
            context.Culture = HttpUtil.GetCultureInfoFromHeader(request.Headers.AcceptLanguage, context.Culture);

            // check for implicit arguments
            context.SetState(GetImplicitEnvironment(request, _publicDigitalSignature));

            // create custom environment
            DekiScriptEnv env = CreateEnvironment();

            // create custom target for custom environment
            var target = descriptor.Target as DekiScriptExpressionInvocationTarget;
            if(target != null) {

                // TODO (steveb): re-initializing the invocation target works for the first call, but not if the function calls another function in the same extension!

                target = new DekiScriptExpressionInvocationTarget(target.Access, target.Parameters, target.Expression, env);
            }

            // invoke target
            DekiScriptLiteral eval;
            if(target != null) {
                eval = target.Invoke(ScriptRuntime, DekiScriptLiteral.FromXml(request.ToDocument()));
            } else {
                eval = descriptor.Target.Invoke(ScriptRuntime, DekiScriptLiteral.FromXml(request.ToDocument()));
            }

            // invoke function
            response.Return(DreamMessage.Ok(new DekiScriptList().Add(eval).ToXml()));
            yield break;
        }

        [DreamFeature("GET:$files/{name}", "Retrieve file contents")]
        [DreamFeature("HEAD:$files/{name}", "Retrieve file headers")]
        public Yield GetLibraryFiles(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            string name = context.GetParam("name");
            Plug resource;
            if(!_files.TryGetValue(name, out resource)) {
                response.Return(DreamMessage.NotFound("resource not found: " + name));
            } else {
                yield return context.Relay(resource, request, response);
            }
        }

        //--- Methods ---
        public XDoc NewIFrame(XUri uri, float? width, float? height) {
            return new XDoc("html").Start("body").Start("iframe")
                .Attr("src", uri)
                .Attr("width", AsSize(width))
                .Attr("height", AsSize(height))
                .Attr("marginwidth", "0")
                .Attr("marginheight", "0")
                .Attr("hspace", "0")
                .Attr("vspace", "0")
                .Attr("frameborder", "0")
                .Attr("scrolling", "no")
            .End().End();
        }

        public T EnvAt<T>(string path) {
            if(path == null) {
                throw new ArgumentNullException("path");
            }
            DekiScriptMap env = DreamContext.Current.GetState<DekiScriptMap>();
            if(env == null) {
                return default(T);
            }
            DekiScriptLiteral value = env.GetAt(path);
            return SysUtil.ChangeType<T>(value.NativeValue);
        }

        protected override Yield Start(XDoc config, Result result) {
            yield return Coroutine.Invoke(base.Start, config, new Result());

            // loop over all resources
            Type type = GetType();
            string assembly = type.Assembly.FullName.Split(new char[] { ',' }, 2)[0];
            foreach(DekiExtLibraryFilesAttribute files in Attribute.GetCustomAttributes(type, typeof(DekiExtLibraryFilesAttribute))) {
                string prefix = files.Prefix ?? type.Namespace;
                foreach(string filename in files.Filenames) {
                    MimeType mime = MimeType.FromFileExtension(filename);
                    _files[filename] = Plug.New(string.Format("resource://{0}/{1}.{2}", assembly, prefix, filename)).With("dream.out.type", mime.FullType);
                }
            }

            // check if a public digital signature key was provided
            string dsaKey = config["deki-signature"].AsText ?? config["dekiwiki-signature"].AsText;
            if(dsaKey != null) {
                try {
                    DSACryptoServiceProvider dsa = new DSACryptoServiceProvider();
                    dsa.ImportCspBlob(Convert.FromBase64String(dsaKey));
                    _publicDigitalSignature = dsa;
                } catch {
                    throw new ArgumentException("invalid digital signature provided", "deki-signature");
                }
            }

            // loop over all instance methods
            foreach(MethodInfo method in type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)) {

                // check if it has the DekiExtFunction attriute
                DekiExtFunctionAttribute ext = (DekiExtFunctionAttribute)Attribute.GetCustomAttribute(method, typeof(DekiExtFunctionAttribute));
                if(ext != null) {

                    // check if function has an associated script
                    XDekiScript script = null;
                    DekiExtFunctionScriptAttribute scriptAttr = (DekiExtFunctionScriptAttribute)Attribute.GetCustomAttribute(method, typeof(DekiExtFunctionScriptAttribute));
                    if(scriptAttr != null) {
                        DreamMessage scriptresource = Plug.New(string.Format("resource://{0}/{1}.{2}", assembly, scriptAttr.Prefix ?? type.Namespace, scriptAttr.Scriptname)).With("dream.out.type", MimeType.XML.FullType).GetAsync().Wait();
                        if(scriptresource.IsSuccessful) {
                            script = new XDekiScript(scriptresource.ToDocument());
                        }
                        if(script == null) {
                            throw new InvalidOperationException(string.Format("method '{0}' is declard as script, but script could not be loaded", method.Name));
                        }
                    }

                    // add function
                    Add(ext, method, script);
                }
            }

            // add configuration settings
            var context = DreamContext.Current;
            _scriptConfig = new DekiScriptMap();
            foreach(KeyValuePair<string, string> entry in Config.ToKeyValuePairs()) {
                XUri local;
                if(XUri.TryParse(entry.Value, out local)) {
                    local = context.AsPublicUri(local);
                    _scriptConfig.AddAt(entry.Key.Split('/'), DekiScriptExpression.Constant(local.ToString()));
                } else {
                    _scriptConfig.AddAt(entry.Key.Split('/'), DekiScriptExpression.Constant(entry.Value));
                }
            }
            result.Return();
        }

        protected override Yield Stop(Result result) {
            _scriptEnv = null;
            _runtime = null;
            _functions.Clear();
            _files.Clear();
            yield return Coroutine.Invoke(base.Stop, new Result());
            result.Return();
        }

        protected virtual DekiScriptEnv CreateEnvironment() {
            InitializeRuntimeAndEnvironment();
            return _scriptEnv.NewScope();
        }

        public DekiScriptEnv InitializeRuntimeAndEnvironment(DekiScriptRuntime runtime) {
            runtime.RegisterExtensionFunctions(_functions);

            // initialize evaluation environment
            var env = runtime.CreateEnv();

            // add extension functions to env
            foreach(var function in _functions) {
                if(!function.Value.IsProperty || (function.Value.Parameters.Length == 0)) {
                    env.Vars.AddNativeValueAt(function.Value.Name.ToLowerInvariant(), function.Key);
                }
            }
            env.Vars.Add("config", _scriptConfig);
            return env;
        }

        private void InitializeRuntimeAndEnvironment() {
            if(_runtime != null) {
                return;
            }
            lock(_functions) {
                if(_runtime == null) {
                    _runtime = new DekiScriptRuntime();
                    _scriptEnv = InitializeRuntimeAndEnvironment(_runtime);
                }
            }
        }

        private void Add(DekiExtFunctionAttribute ext, MethodInfo method, XDoc script) {

            // convert DekiExtParamAttribute into DekiScriptNativeInvocationTarget.Parameter
            var parameters = from param in method.GetParameters()
                             let attr = (DekiExtParamAttribute[])param.GetCustomAttributes(typeof(DekiExtParamAttribute), false)
                             select ((attr != null) && (attr.Length > 0)) ? new DekiScriptNativeInvocationTarget.Parameter(attr[0].Hint, attr[0].Optional) : null;

            // create native target invocation
            var target = new DekiScriptNativeInvocationTarget(this, method, parameters.ToArray());
            DekiScriptInvocationTargetDescriptor function;

            // check if implementation is provided by a script instead
            if(script != null) {
                var scriptTarget = new DekiScriptExpressionInvocationTarget(target.Access, target.Parameters, DekiScriptParser.Parse(script));
                function = new DekiScriptInvocationTargetDescriptor(target.Access, ext.IsProperty, false, ext.Name ?? method.Name, target.Parameters, target.ReturnType, ext.Description, ext.Transform, scriptTarget);
            } else {
                function = new DekiScriptInvocationTargetDescriptor(target.Access, ext.IsProperty, false, ext.Name ?? method.Name, target.Parameters, target.ReturnType, ext.Description, ext.Transform, target);
            }
            _functions[Self.At(function.SystemName)] = function;
        }
    }
}

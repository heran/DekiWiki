using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using log4net;
using MindTouch.Reflection;
using MindTouch.Dream;
using MindTouch.Xml;

namespace MindTouch.Documentation.Util {

    // TODO (arnec): Need to handle operators
    // TODO (arnec): Need to fixup !: types from xmldoc
    public class HtmlDocumentationBuilder : IDisposable {

        //--- Static Fields ---
        private static readonly ILog _log = LogUtils.CreateLog();

        //--- Fields ---
        private readonly Dictionary<string, string> _assemblies = new Dictionary<string, string>();
        private readonly Dictionary<string, XDoc> _documentationBySignature = new Dictionary<string, XDoc>();
        private readonly Dictionary<string, IReflectedInfo> _memberLookup = new Dictionary<string, IReflectedInfo>();
        private readonly HashSet<string> _namespaces = new HashSet<string>();
        private readonly HashSet<string> _documentedNamespaces = new HashSet<string>();
        private readonly ITypeInspector _typeInspector;
        private int _currentDataId;
        private XDoc _manifest;
        private XDoc _fileMap;
        private string _outputPath;
        private IEnumerable<ReflectedTypeInfo> _types;

        //--- Constructors ---
        public HtmlDocumentationBuilder() {
            _typeInspector = new IsolatedTypeInspector();
        }

        public HtmlDocumentationBuilder(ITypeInspector typeInspector) {
            _typeInspector = typeInspector;
        }

        //--- Methods ---
        public void BuildDocumenationPackage(string outputPath, params string[] namespaces) {
            _types = GetTypes();
            InitNamespaces(namespaces);
            _outputPath = outputPath;
            _currentDataId = 0;
            var package = new XDoc("package").Elem("manifest").Elem("map");
            _manifest = package["manifest"];
            _fileMap = package["map"];
            var root = NewHtmlDocument("", false).Start("div").Attr("class", "doctree").Value("{{wiki.tree()}}").End();
            Save(root, "", "//", "root.xml", null);
            foreach(var type in _types.OrderBy(x => x.LongDisplayName).Where(IsTypeInDocumentation)) {
                EnsureNamespaceRootDocument(type.Namespace);
                var sig = type.Signature;
                var xmlDoc = GetDoc(sig);
                var title = type.DisplayName;
                if(type.IsStatic) {
                    title += " Static";
                }
                switch(type.Kind) {
                case TypeKind.Enum:
                    title += " Enumeration";
                    break;
                case TypeKind.Interface:
                    title += " Interface";
                    break;
                case TypeKind.Struct:
                    title += " Struct";
                    break;
                default:
                    title += " Class";
                    break;
                }
                var html = NewHtmlDocument(title, true);
                html.StartSection("docheader")
                    .CSharpBlock(type.CodeSignature)
                    .NameValueLine("namespace", "Namespace", type.Namespace)
                    .NameValueLine("assembly", "Assembly", type.Assembly);
                BuildInheritanceChain(html, type);
                BuildInterfaceBlock(html, type);
                html
                    .Div("summary", xmlDoc["summary"])
                    .EndSection() // header section
                    .Section(1, "remarks", "Remarks", xmlDoc["remarks"]);
                if(type.Kind == TypeKind.Enum) {
                    BuildEnumFields(html, type.Fields);
                } else {
                    BuildGenericParameterSection(1, html, xmlDoc, type.GenericParameters.Where(x => !x.IsInherited));
                    if(!type.IsDelegate) {
                        AddMemberTables(html, type);
                        BuildConstructorHtml(type.Constructors);
                        BuildFieldHtml(type.Fields);
                        BuildPropertyHtml(type.Properties.Where(x => !x.IsInherited));
                        BuildMethodHtml(type.Methods.Where(x => !x.IsInherited));
                        BuildEventHtml(type.Events.Where(x => !x.IsInherited));
                    }
                }
                AddExceptionSection(html, xmlDoc);
                Save(html, title, type.UriPath, type.FilePath, type.Assembly);

            }
            var packagePath = Path.Combine(_outputPath, "package.xml");
            Directory.CreateDirectory(Path.GetDirectoryName(packagePath));
            using(var stream = File.Create(packagePath)) {
                using(var writer = new StreamWriter(stream)) {
                    writer.Write(package.ToPrettyString());
                }
            }
        }

        private void BuildInterfaceBlock(XDoc html, ReflectedTypeInfo type) {
            if(type.Interfaces.Any()) {
                html.StartNameValueBlock("implements", "Implements")
                    .Start("ul");
                foreach(var interfaceParameter in type.Interfaces) {
                    html.Start("li");
                    BuildParameterMarkup(html, interfaceParameter);
                    html.End();
                }
                html.End() // ul
                    .EndNameValue();
            }
            if(type.Kind == TypeKind.Interface) {
                var implementers = (from t in _types where t.Interfaces.Where(x => x.Type == type).Any() select t).ToList();
                if(implementers.Any()) {
                    html.StartNameValueBlock("implementors", "Implementors")
                        .Start("ul");
                    foreach(var implementor in implementers) {
                        html.Start("li");
                        if(IsTypeInDocumentation(implementor)) {
                            html.Link(implementor.UriPath, implementor.DisplayName);
                        } else {
                            html.Value(implementor.DisplayName);
                        }
                        html.End();
                    }
                    html.End() // ul
                        .EndNameValue();
                }
            }
        }

        private void EnsureNamespaceRootDocument(string ns) {
            if(_documentedNamespaces.Add(ns)) {
                var html = NewHtmlDocument(ns, false).Start("div").Attr("class", "doctree").Value("{{wiki.tree()}}").End();
                Save(html, ns, "//" + ns, "ns." + ns + ".xml", null);
            }
        }

        public void AddAssembly(string assemblyPath) {
            string assembly = _typeInspector.InspectAssembly(assemblyPath);
            _assemblies.Add(assembly, assemblyPath);
        }

        protected IEnumerable<ReflectedTypeInfo> GetTypes() {
            _log.Debug("getting types");
            var types = _typeInspector.Types;
            _log.Debug("build type lookup");
            foreach(ReflectedTypeInfo type in types) {
                _memberLookup[type.Signature] = type;
                foreach(var ctor in type.Constructors) {
                    _memberLookup[ctor.Signature] = ctor;
                }
                foreach(var field in type.Fields) {
                    if(!field.IsInherited) {
                        _memberLookup[field.Signature] = field;
                    }
                }
                foreach(var method in type.Methods) {
                    if(!method.IsInherited) {
                        _memberLookup[method.Signature] = method;
                    }
                }
                foreach(var property in type.Properties) {
                    if(!property.IsInherited) {
                        _memberLookup[property.Signature] = property;
                    }
                }
            }
            _log.Debug("rewriting xml documentation links");
            foreach(var assemblyPath in _assemblies.Values) {
                string docPath = Path.Combine(Path.GetDirectoryName(assemblyPath), Path.GetFileNameWithoutExtension(assemblyPath) + ".xml");
                if(File.Exists(docPath)) {
                    XDoc xmlDocumentation = XDocFactory.LoadFrom(docPath, MimeType.TEXT_XML);
                    foreach(XDoc see in xmlDocumentation[".//see"]) {
                        string cref = see["@cref"].AsText;
                        if(!string.IsNullOrEmpty(cref)) {
                            var info = GetTypeInfo(cref);
                            if(info == null) {
                                see.Replace(new XDoc("b").Value(cref));
                            } else if(IsTypeInDocumentation(info.Type)) {
                                see.Replace(new XDoc("a").Attr("href.path", info.UriPath).Value(info.LongDisplayName));
                            } else {
                                see.Replace(new XDoc("b").Value(info.LongDisplayName));
                            }
                        }
                        string langword = see["@langword"].AsText;
                        if(!string.IsNullOrEmpty(langword)) {
                            see.Replace(new XDoc("b").Value(langword));
                            continue;
                        }
                    }
                    foreach(XDoc member in xmlDocumentation["members/member"]) {
                        string signature = member["@name"].AsText;
                        _documentationBySignature[signature] = member;
                    }
                }
            }
            _log.Debug("finished building type lookup");
            return types.Where(IsTypeInDocumentation).ToList();
        }

        protected void InitNamespaces(string[] namespaces) {
            _namespaces.Clear();
            _documentedNamespaces.Clear();
            foreach(string ns in namespaces) {
                _namespaces.Add(ns);
            }
        }

        protected bool IsTypeInDocumentation(ReflectedTypeInfo x) {
            if(_assemblies.ContainsKey(x.Assembly)) {
                if(!_namespaces.Any()) {
                    return true;
                }
                foreach(string ns in _namespaces) {
                    if(x.Namespace.StartsWith(ns)) {
                        return true;
                    }
                }
            }
            return false;
        }

        protected XDoc GetDoc(string signature) {
            XDoc doc;
            if(!_documentationBySignature.TryGetValue(signature, out doc)) {
                doc = XDoc.Empty;
            }
            return doc;
        }

        protected IReflectedInfo GetTypeInfo(string signature) {
            IReflectedInfo info;
            _memberLookup.TryGetValue(signature, out info);
            return info;
        }

        protected IReflectedInfo GetMember(string signature) {
            IReflectedInfo member;
            if(!_memberLookup.TryGetValue(signature, out member)) {
                return null;
            }
            return member;
        }

        public void Dispose() {
            _typeInspector.Dispose();
        }

        private void AddExceptionSection(XDoc html, XDoc xmlDoc) {
            var exceptions = xmlDoc["exception"];
            if(exceptions.ListLength == 0) {
                return;
            }
            html.StartSection(1, "exceptions", "Exceptions")
                .Start("table")
                    .Start("tr").Elem("th", "Exception").Elem("th", "Condition");
            foreach(var exception in exceptions) {
                var cref = exception["@cref"].AsText;
                var info = GetTypeInfo(cref);
                if(info == null) {
                    continue;
                }
                html.Start("tr")
                    .Start("td");
                if(IsTypeInDocumentation(info.Type)) {
                    html.Link(info.UriPath, info.DisplayName);
                } else {
                    html.Value(info.DisplayName);
                }
                html.End() // td
                    .Start("td").AddNodes(exception).End()
                    .End(); // tr
            }
            html.End() // table
                .EndSection();
        }

        private void BuildEnumFields(XDoc html, IEnumerable<ReflectedFieldInfo> fields) {
            html.Heading(1, "Members")
                .Start("table")
                .Start("tr").Elem("th", "Name").Elem("th", "Description").End();
            foreach(var field in fields.Where(x => x.Name != "value__")) {
                var xmlDoc = GetDoc(field.Signature);
                html.Start("tr")
                    .Elem("td", field.Name)
                    .Start("td").AddNodes(xmlDoc["summary"]).End()
                .End();
            }
            html.End();
        }

        private void BuildGenericParameterSection(int level, XDoc html, XDoc xmlDoc, IEnumerable<ReflectedGenericParameterInfo> genericParameters) {
            if(genericParameters.Any()) {
                html.StartSection(level, "genericparameters", "Generic Parameters");
                foreach(var parameter in genericParameters.OrderBy(x => x.ParameterPosition)) {
                    html.StartSection(level + 1, "genericparameter", "Parameter " + parameter.Name)
                       .Div("description", xmlDoc[string.Format("typeparam[@name='{0}']", parameter.Name)])
                       .StartNameValueBlock("constraints", "Constraints");
                    var constraints = new List<string>();
                    if(parameter.MustBeReferenceType) {
                        constraints.Add("class");
                    }
                    if(parameter.MustBeValueType) {
                        constraints.Add("struct");
                    }
                    if(parameter.MustHaveDefaultConstructor) {
                        constraints.Add("new()");
                    }
                    if(constraints.Any() || parameter.Types.Any()) {
                        html.Start("ul");
                        foreach(var constraint in constraints) {
                            html.Start("li").Elem("b", constraint).End();
                        }
                        foreach(var parameterType in parameter.Types) {
                            html.Start("li");
                            BuildParameterMarkup(html, parameterType);
                            html.End();
                        }
                        html.End(); // ul
                    } else {
                        html.Elem("i", "none");
                    }
                    html.EndNameValue()
                        .EndSection();
                }
                html.EndSection();
            }
        }

        private void BuildParameterMarkup(XDoc html, ReflectedParameterTypeInfo parameterType) {
            if(parameterType.IsGenericType) {
                if(IsTypeInDocumentation(parameterType.Type)) {
                    html.Link(parameterType.Type.UriPath, parameterType.Type.Name);
                } else {
                    html.Value(parameterType.Type.Name);
                }
                html.Value("<");
                bool first = true;
                foreach(var childParam in parameterType.Parameters) {
                    if(!first) {
                        html.Value(",");
                    }
                    first = false;
                    BuildParameterMarkup(html, childParam);
                }
                html.Value(">");
            } else {

                if(!parameterType.IsGenericParameter && IsTypeInDocumentation(parameterType.Type)) {
                    html.Link(parameterType.Type.UriPath, parameterType.DisplayName);
                } else {
                    html.Value(parameterType.DisplayName);
                }
            }
        }

        private void BuildInheritanceChain(XDoc html, ReflectedTypeInfo type) {
            html.StartNameValueBlock("inheritance", "Type Hierarchy");
            var chain = GetParents(type.BaseType).ToList();
            foreach(var link in chain) {
                html.Start("ul")
                        .Start("li");
                BuildParameterMarkup(html, link);
                html.End(); // li
            }
            html.Start("ul")
                .Start("li").Elem("b", type.DisplayName).End();
            var subclasses = (from t in _types
                              where t.BaseType != null && !t.BaseType.IsGenericParameter && t.BaseType.Type == type
                              select t).ToList();
            if(subclasses.Any()) {
                html.Start("ul");
                foreach(var subclass in subclasses) {
                    html.Start("li");
                    if(IsTypeInDocumentation(subclass)) {
                        html.Link(subclass.UriPath, subclass.DisplayName);
                    } else {
                        html.Value(subclass.DisplayName);
                    }
                    html.End(); // li
                }
                html.End(); // subclass ul
            }
            html.End(); // type ul
            for(var i = 0; i < chain.Count; i++) {
                html.End();
            }
            html.EndNameValue();
        }

        private IEnumerable<ReflectedParameterTypeInfo> GetParents(ReflectedParameterTypeInfo type) {
            if(type == null) {
                yield break;
            }
            foreach(var parent in GetParents(type.Type.BaseType)) {
                yield return parent;
            }
            yield return type;
        }

        private void BuildParameterTable(XDoc html, XDoc xmlDoc, IEnumerable<ReflectedParameterInfo> parameters) {
            html.StartSection(2, "parameters", "Parameters")
               .Start("table")
                   .Start("tr").Elem("th", "Name").Elem("th", "Type").Elem("th", "Description").End();
            foreach(var parameter in parameters.OrderBy(x => x.ParameterPosition)) {
                html.Start("tr")
                        .Start("td").Elem("b", parameter.Name).End()
                        .Start("td");
                if(parameter.IsOut) {
                    html.Value("out ");
                } else if(parameter.IsRef) {
                    html.Value("ref ");
                } else if(parameter.IsParams) {
                    html.Value("params ");
                }
                BuildParameterMarkup(html, parameter.Type);
                html
                        .End() // td
                        .Start("td").AddNodes(xmlDoc[string.Format("param[@name='{0}']", parameter.Name)]).End()
                    .End(); // tr
            }
            html
                .End() //table
                .EndSection();
        }

        private void BuildMethodHtml(IEnumerable<ReflectedMethodInfo> methods) {
            var q = from m in methods
                    where !m.IsInherited
                    group m by m.Name
                        into overloads
                        select overloads;
            foreach(var memberOverloads in q) {
                XDoc html = null;
                string href = null;
                string path = null;
                string title = null;
                string assembly = null;
                foreach(var method in memberOverloads.OrderBy(x => x.DisplayName)) {
                    var xmlDoc = GetDoc(method.Signature);
                    if(html == null) {
                        href = method.UriPath;
                        if(string.IsNullOrEmpty(href)) {
                            continue;
                        }
                        path = method.FilePath;
                        title = method.Name + (method.IsExtensionMethod ? " Extension" : "") + " Method";
                        html = NewHtmlDocument(title, true);
                        assembly = method.Assembly;
                    }
                    html
                        .StartSection(1, "method", method.DisplayName)
                        .CSharpBlock(method.CodeSignature)
                        .Div("summary", xmlDoc["summary"])
                        .Section(2, "remarks", "Remarks", xmlDoc["remarks"]);
                    BuildGenericParameterSection(2, html, xmlDoc, method.GenericParameters.Where(x => x.MethodParameter));
                    BuildParameterTable(html, xmlDoc, method.Parameters);
                    html.StartSection(2, "returns", "Returns");
                    if(!method.ReturnType.IsGenericParameter && method.ReturnType.Type.Name == "Void") {
                        html.Value("void");
                    } else {
                        html.StartNameValueLine("type", "Type");
                        BuildParameterMarkup(html, method.ReturnType);
                        html.EndNameValue()
                            .Start("div")
                                .Attr("class", "summary")
                                .AddNodes(xmlDoc["returns"])
                            .End();
                    }
                    html.EndSection(); // return section
                    AddExceptionSection(html, xmlDoc);
                    html.EndSection();
                }
                if(html != null) {
                    Save(html, title, href, path, assembly);
                }
            }
        }

        private void BuildPropertyHtml(IEnumerable<ReflectedPropertyInfo> properties) {
            var q = from p in properties
                    where !p.IsInherited
                    group p by p.Name
                        into overloads
                        select overloads;
            foreach(var memberOverloadsEnumerable in q) {
                XDoc html = null;
                string href = null;
                string path = null;
                string title = null;
                string assembly = null;
                var memberOverloads = memberOverloadsEnumerable.ToList();
                foreach(var member in memberOverloads.OrderBy(x => x.DisplayName)) {
                    var xmlDoc = GetDoc(member.Signature);
                    if(html == null) {
                        href = member.UriPath;
                        if(string.IsNullOrEmpty(href)) {
                            continue;
                        }
                        path = member.FilePath;
                        title = member.Name + " Property";
                        html = NewHtmlDocument(title, true);
                        assembly = member.Assembly;
                    }
                    html.StartSection(1, "property", member.DisplayName);
                    html.CSharpBlock(member.CodeSignature)
                        .Div("summary", xmlDoc["summary"])
                        .Section(2, "remarks", "Remarks", xmlDoc["remarks"]);
                    if(member.IsIndexer) {
                        BuildParameterTable(html, xmlDoc, member.IndexerParameters);
                    }
                    html.StartSection(2, "returns", "Value");
                    html.StartNameValueLine("type", "Type");
                    BuildParameterMarkup(html, member.ReturnType);
                    html.EndNameValue()
                        .Start("div")
                            .Attr("class", "summary")
                            .AddNodes(xmlDoc["returns"])
                        .End()
                        .EndSection(); // returns section
                    AddExceptionSection(html, xmlDoc);
                    html.EndSection();
                }
                if(html != null) {
                    Save(html, title, href, path, assembly);
                }
            }
        }

        private void BuildFieldHtml(IEnumerable<ReflectedFieldInfo> fields) {
            foreach(var field in fields.Where(x => !x.IsInherited).OrderBy(x => x.DisplayName)) {
                var xmlDoc = GetDoc(field.Signature);
                var href = field.UriPath;
                var path = field.FilePath;
                if(string.IsNullOrEmpty(href)) {
                    return;
                }
                var title = field.DisplayName + " Field";
                var html = NewHtmlDocument(title, true);
                html.CSharpBlock(field.CodeSignature)
                    .Div("summary", xmlDoc["summary"])
                    .Section(1, "remarks", "Remarks", xmlDoc["remarks"]);
                html.StartSection(1, "returns", "Type");
                BuildParameterMarkup(html, field.ReturnType);
                html.EndSection();
                Save(html, title, href, path, field.Assembly);
            }
        }

        private void BuildEventHtml(IEnumerable<ReflectedEventInfo> events) {
            foreach(var ev in events.Where(x => !x.IsInherited).OrderBy(x => x.DisplayName)) {
                var xmlDoc = GetDoc(ev.Signature);
                var href = ev.UriPath;
                var path = ev.FilePath;
                if(string.IsNullOrEmpty(href)) {
                    return;
                }
                var title = ev.DisplayName + " Event";
                var html = NewHtmlDocument(title, true);
                html.CSharpBlock(ev.CodeSignature)
                    .Div("summary", xmlDoc["summary"])
                    .Section(1, "remarks", "Remarks", xmlDoc["remarks"]);
                html.StartSection(1, "returns", "Handler");
                BuildParameterMarkup(html, ev.ReturnType);
                html.EndSection();
                Save(html, title, href, path, ev.Assembly);
            }
        }

        private void BuildConstructorHtml(IEnumerable<ReflectedConstructorInfo> ctors) {
            XDoc html = null;
            string href = null;
            string path = null;
            string title = null;
            string assembly = null;
            foreach(var ctor in ctors.OrderBy(x => x.DisplayName)) {
                var xmlDoc = GetDoc(ctor.Signature);
                if(html == null) {
                    href = ctor.UriPath;
                    if(string.IsNullOrEmpty(href)) {
                        continue;
                    }
                    path = ctor.FilePath;
                    title = ctor.Type.Name + " Constructors";
                    html = NewHtmlDocument(title, true);
                    assembly = ctor.Assembly;
                }
                html.StartSection(1, "ctor", ctor.DisplayName)
                    .CSharpBlock(ctor.CodeSignature)
                    .Div("summary", xmlDoc["summary"])
                    .Section(2, "remarks", "Remarks", xmlDoc["remarks"]);
                BuildParameterTable(html, xmlDoc, ctor.Parameters);
                AddExceptionSection(html, xmlDoc);
                html.EndSection();
            }
            if(html != null) {
                Save(html, title, href, path, assembly);
            }
        }

        private void AddMemberTables(XDoc html, ReflectedTypeInfo type) {
            if(!type.Constructors.Any() && !type.Fields.Any() && !type.Properties.Any() && !type.Methods.Any() && !type.Events.Any()) {
                return;
            }
            html.StartSection(1, "members", "Members");

            if(type.Constructors.Any()) {
                html.StartSection(2, "ctors", "Constructors")
                    .Start("table")
                    .Start("tr").Elem("th", "Visibility").Elem("th", "Description").End();
                foreach(var member in type.Constructors.OrderBy(x => x.DisplayName)) {
                    BuildConstructorRow(html, member);
                }
                html.End() // table
                    .EndSection();
            }
            if(type.Fields.Any()) {
                html.StartSection(2, "fields", "Fields")
                    .Start("table")
                    .Start("tr").Elem("th", "Visibility").Elem("th", "Description").End();
                foreach(var member in type.Fields.OrderBy(x => x.DisplayName)) {
                    BuildFieldRow(html, member);
                }
                html.End() // table
                     .EndSection();
            }
            if(type.Properties.Any()) {
                html.StartSection(2, "properties", "Properties")
                    .Start("table")
                    .Start("tr").Elem("th", "Visibility").Elem("th", "Description").End();
                foreach(var member in type.Properties.OrderBy(x => x.DisplayName)) {
                    BuildPropertyRow(html, member);
                }
                html.End() // table
                     .EndSection();
            }
            if(type.Methods.Any()) {
                html.StartSection(2, "methods", "Methods")
                    .Start("table")
                    .Start("tr").Elem("th", "Visibility").Elem("th", "Description").End();
                foreach(var member in type.Methods.OrderBy(x => x.DisplayName)) {
                    BuildMethodRow(html, member);
                }
                html.End() // table
                     .EndSection();
            }
            if(type.Events.Any()) {
                html.StartSection(2, "events", "Events")
                    .Start("table")
                    .Start("tr").Elem("th", "Visibility").Elem("th", "Description").End();
                foreach(var member in type.Events.OrderBy(x => x.DisplayName)) {
                    BuildEventRow(html, member);
                }
                html.End() // table
                     .EndSection();
            }
            html.EndSection(); //members
        }

        private void BuildEventRow(XDoc html, ReflectedEventInfo member) {
            var xmlDoc = GetDoc(member.Signature);
            html.Start("tr")
                    .Elem("td", member.MemberAccess)
                    .Start("td").StartSpan("member");
            if(IsTypeInDocumentation(member.DeclaringType)) {
                html.Link(member.UriPath, member.DisplayName);
            } else {
                html.Value(member.DisplayName);
            }
            html
                    .EndSpan()
                    .StartSpan("description");
            if(member.IsOverride) {
                html.Value("(Override)");
            }
            html.AddNodes(xmlDoc["summary"]);
            if(member.IsInherited) {
                html.Value(string.Format("(Inherited from {0})", member.DeclaringType.DisplayName));
            }
            html
                        .EndSpan()
                    .End() // td
                .End(); // tr
        }

        private void BuildMethodRow(XDoc html, ReflectedMethodInfo member) {
            var xmlDoc = GetDoc(member.Signature);
            html.Start("tr")
                    .Elem("td", member.MemberAccess)
                    .Start("td").StartSpan("member");
            if(IsTypeInDocumentation(member.DeclaringType)) {
                html.Link(member.UriPath, member.DisplayName);
            } else {
                html.Value(member.DisplayName);
            }
            html
                    .EndSpan()
                    .StartSpan("description");
            if(member.IsOverride && !member.IsInherited) {
                html.Value("(Override)");
            }
            if(member.IsExtensionMethod) {
                html.Value("(Extension)");
            }
            html.AddNodes(xmlDoc["summary"]);
            if(member.IsInherited) {
                html.Value(string.Format("(Inherited from {0})", member.DeclaringType.DisplayName));
            }
            html
                        .EndSpan()
                    .End() // td
                .End(); // tr
        }

        private void BuildPropertyRow(XDoc html, ReflectedPropertyInfo member) {
            var xmlDoc = GetDoc(member.Signature);
            html.Start("tr")
                    .Elem("td", member.MemberAccess)
                    .Start("td").StartSpan("member");
            if(IsTypeInDocumentation(member.DeclaringType)) {
                html.Link(member.UriPath, member.DisplayName);
            } else {
                html.Value(member.DisplayName);
            }
            html
                    .EndSpan()
                    .StartSpan("description");
            if(member.IsOverride && !member.IsInherited) {
                html.Value("(Override)");
            }
            html.AddNodes(xmlDoc["summary"]);
            if(member.IsInherited) {
                html.Value(string.Format("(Inherited from {0})", member.DeclaringType.DisplayName));
            }
            html
                        .EndSpan()
                    .End() // td
                .End(); // tr
        }

        private void BuildConstructorRow(XDoc html, ReflectedConstructorInfo member) {
            var xmlDoc = GetDoc(member.Signature);
            html.Start("tr")
                    .Elem("td", member.MemberAccess)
                    .Start("td")
                        .StartSpan("member").Link(member.UriPath, member.DisplayName).EndSpan()
                        .StartSpan("description").AddNodes(xmlDoc["summary"]).EndSpan()
                    .End()
                .End();
        }

        private void BuildFieldRow(XDoc html, ReflectedFieldInfo member) {
            var xmlDoc = GetDoc(member.Signature);
            html.Start("tr")
                    .Elem("td", member.MemberAccess)
                    .Start("td").StartSpan("member");
            if(IsTypeInDocumentation(member.DeclaringType)) {
                html.Link(member.UriPath, member.DisplayName);
            } else {
                html.Value(member.DisplayName);
            }
            html
                        .EndSpan()
                        .StartSpan("description").AddNodes(xmlDoc["summary"]).EndSpan()
                    .End() // td
                .End(); // tr
        }

        private XDoc NewHtmlDocument(string title, bool includeToc) {
            var html = new XDoc("content")
                .Attr("type", "application/x.deki-text")
                .Attr("title", title)
                .Attr("unsafe", false)
                .Start("body")
                    .UsePrefix("eval", "http://mindtouch.com/2007/dekiscript")
                    .Start("div").Attr("class", "xdocs");
            if(includeToc) {
                html.Elem("div", "{{DocToc()}}");
            }
            return html;
        }


        private void Save(XDoc html, string title, string href, string path, string assembly) {
            html.EndAll(); // xdocs div
            var filepath = Path.Combine("relative", path);
            _fileMap.Start("item").Attr("dataid", _currentDataId).Attr("path", filepath).End();
            _manifest.Start("page")
                .Attr("dataid", _currentDataId);
            if(!string.IsNullOrEmpty(title)) {
                _manifest.Elem("title", title);
            }
            _manifest
                .Elem("path", href)
                .Start("contents").Attr("type", "application/x.deki0805+xml").End()
            .End();
            filepath = Path.Combine(_outputPath, filepath);
            Directory.CreateDirectory(Path.GetDirectoryName(filepath));
            using(var stream = File.Create(filepath)) {
                using(var writer = new StreamWriter(stream)) {
                    writer.Write(html.ToPrettyString());
                    writer.Close();
                }
                stream.Close();
            }
            _currentDataId++;
            if(string.IsNullOrEmpty(assembly)) {
                return;
            }

            // add assembly tag to file
            var assemblyString = "assembly:" + assembly;
            var tagDoc = new XDoc("tags")
                .Attr("count", 1)
                .Start("tag")
                    .Attr("value", assemblyString)
                    .Elem("type", "text")
                    .Elem("title", assemblyString)
                .End();
            filepath = Path.Combine("relative", Path.GetFileNameWithoutExtension(path) + ".tags");
            _fileMap.Start("item").Attr("dataid", _currentDataId).Attr("path", filepath).End();
            _manifest.Start("tags")
                    .Attr("dataid", _currentDataId)
                    .Elem("path", href)
                .End();
            filepath = Path.Combine(_outputPath, filepath);
            using(var stream = File.Create(filepath)) {
                using(var writer = new StreamWriter(stream)) {
                    writer.Write(tagDoc.ToPrettyString());
                    writer.Close();
                }
                stream.Close();
            }
            _currentDataId++;
        }
    }
}
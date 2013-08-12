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
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Xsl;
using log4net;
using MindTouch.Deki.Data;
using MindTouch.Deki.Exceptions;
using MindTouch.Deki.Script.Compiler;
using MindTouch.Deki.Script.Expr;
using MindTouch.Deki.Script.Runtime;
using MindTouch.Dream;
using MindTouch.Xml;

namespace MindTouch.Deki.Logic {
    public static class ExtensionBL {

        //--- Constants ---
        public static readonly XDoc TOC = new XDoc("html").Start("body").Start("span").Attr("id", "page.toc").End().End();
        private static readonly XslCompiledTransform _extensionConverterXslt;
        private static readonly ILog _log = LogUtils.CreateLog();

        //--- Class Constructor ---
        static ExtensionBL() {

            // load XSLT for normalizing extensions
            XDoc doc = Plug.New("resource://mindtouch.deki/MindTouch.Deki.Resources.ExtensionConverter.xslt").With(DreamOutParam.TYPE, MimeType.XML.FullType).Get().ToDocument();
            _extensionConverterXslt = new XslCompiledTransform();
            _extensionConverterXslt.Load(new XmlNodeReader(doc.AsXmlNode), null, null);
        }

        //--- Class Methods ---
        public static void StartExtensionService(DekiContext context, ServiceBE service, ServiceRepository.IServiceInfo serviceInfo, bool forceRefresh) {

            // retrieve document describing the extension functions
            XUri uri = new XUri(service.Uri);
            XDoc manifest = null;
            DekiWikiService deki = context.Deki;
            var extension = serviceInfo.Extension;
            if(!service.ServiceLocal) {
                lock(deki.RemoteExtensionLibraries) {
                    deki.RemoteExtensionLibraries.TryGetValue(uri, out manifest);
                }
            }
            if(manifest == null || forceRefresh) {
                manifest = Plug.New(uri).Get().ToDocument();

                // normalize the extension XML
                manifest = manifest.TransformAsXml(_extensionConverterXslt);

                // check if document describes a valid extension: either the extension has no functions, or the functions have end-points
                if(manifest.HasName("extension") && ((manifest["function"].ListLength == 0) || (manifest["function/uri"].ListLength > 0))) {

                    // add source uri for service
                    manifest.Attr("uri", uri);

                    // register service in extension list
                    lock(deki.RemoteExtensionLibraries) {
                        deki.RemoteExtensionLibraries[uri] = manifest;
                    }
                } else {
                    throw new ExtensionRemoveServiceInvalidOperationException(uri);
                }
            }
            extension.Manifest = manifest;

            // add function prefix if one is defined
            serviceInfo.Extension.SetPreference("namespace.custom", service.Preferences["namespace"]);
            string serviceNamespace = service.Preferences["namespace"] ?? manifest["namespace"].AsText;
            if(serviceNamespace != null) {
                serviceNamespace = serviceNamespace.Trim();
                if(string.IsNullOrEmpty(serviceInfo.Namespace)) {

                    // Note (arnec): Namespace from preferences is assigned at service creation. If we do not have one at this
                    // point, it came from the extension manifest and needs to be registered as our default. Otherwise the
                    // preference override persists as the namespace.
                    context.Instance.RunningServices.RegisterNamespace(serviceInfo, serviceNamespace);
                }
                if(serviceNamespace.Length != 0) {
                    if(!DekiScriptParser.IsIdentifier(serviceNamespace)) {
                        throw new ExtensionNamespaceInvalidArgumentException(service.Preferences["namespace"] ?? manifest["namespace"].AsText);
                    }
                } else {
                    serviceNamespace = null;
                }
            }
            serviceNamespace = (serviceNamespace == null) ? string.Empty : (serviceNamespace + ".");

            // add custom library title

            extension.SetPreference("title.custom", service.Preferences["title"]);
            extension.SetPreference("label.custom", service.Preferences["label"]);
            extension.SetPreference("description.custom", service.Preferences["description"]);
            extension.SetPreference("uri.logo.custom", service.Preferences["uri.logo"]);
            extension.SetPreference("functions", service.Preferences["functions"]);
            extension.SetPreference("protected", service.Preferences["protected"]);

            // add each extension function
            bool.TryParse(service.Preferences["protected"], out extension.IsProtected);
            var functions = new List<ServiceRepository.ExtensionFunctionInfo>();
            foreach(XDoc function in manifest["function"]) {
                XUri functionUri = function["uri"].AsUri;
                if(functionUri != null) {
                    functions.Add(new ServiceRepository.ExtensionFunctionInfo(serviceNamespace + function["name"].Contents, functionUri));
                }
            }
            extension.Functions = functions.ToArray();
        }

        public static string GetExtensionPreference(XUri uri, string key) {

            // check if we have a uri to look-up
            if(uri == null) {
                return null;
            }

            // retrieve preferences for this service
            var serviceInfo = DekiContext.Current.Instance.RunningServices[uri];
            if(serviceInfo == null || !serviceInfo.IsExtensionService) {
                return null;
            }
            return serviceInfo.Extension.GetPreference(key);
        }

        public static DekiScriptEnv CreateEnvironment(PageBE page) {
            DekiScriptEnv commonEnv = DekiContext.Current.Instance.CreateEnvironment();

            // need to strip the config value back out for Deki
            commonEnv.Vars["config"] = new DekiScriptMap();

            // initialize environment
            DekiScriptEnv env = commonEnv;
            DekiContext deki = DekiContext.Current;
            DekiInstance instance = deki.Instance;

            // add site variables
            env.Vars.AddNativeValueAt("site.name", instance.SiteName);
            env.Vars.AddNativeValueAt("site.hostname", deki.UiUri.Uri.HostPort);
            env.Vars.AddNativeValueAt("site.api", deki.ApiUri.SchemeHostPortPath);
            env.Vars.AddNativeValueAt("site.language", instance.SiteLanguage);
            env.Vars.AddNativeValueAt("site.uri", deki.UiUri.Uri.ToString());
            env.Vars.AddNativeValueAt("site.pagecount", deki.Deki.PropertyAt("$sitepagecount"));
            env.Vars.AddNativeValueAt("site.usercount", deki.Deki.PropertyAt("$siteusercount"));
            env.Vars.AddNativeValueAt("site.homepage", deki.Deki.PropertyAt("$page", DekiContext.Current.Instance.HomePageId, true));
            env.Vars.AddNativeValueAt("site.feed", deki.ApiUri.At("site", "feed").ToString());
            env.Vars.AddNativeValueAt("site.tags", deki.Deki.PropertyAt("$sitetags"));
            env.Vars.AddNativeValueAt("site.users", deki.Deki.PropertyAt("$siteusers"));
            env.Vars.AddNativeValueAt("site.id", DekiScriptExpression.Constant(instance.Id));
            env.Vars.AddNativeValueAt("site.timezone", DekiScriptExpression.Constant(instance.SiteTimezone));

            // add page variables
            env.Vars.Add("page", deki.Deki.PropertyAt("$page", page.ID, true));

            // add user variables
            env.Vars.Add("user", deki.Deki.PropertyAt("$user", (deki.User != null) ? deki.User.ID : 0));

            // add instance functions & properties
            bool hasUnsafeContentPermission = DekiXmlParser.PageAuthorCanExecute();
            foreach(var service in instance.RunningServices.ExtensionServices) {
                if(service != null) {
                    var extension = service.Extension;
                    if(extension != null) {
                        if(hasUnsafeContentPermission || !extension.IsProtected) {
                            var functions = extension.Functions;
                            if(functions != null) {
                                foreach(var function in functions) {
                                    env.Vars.AddNativeValueAt(function.Name.ToLowerInvariant(), function.Uri);
                                }
                            } else {
                                _log.WarnFormat("CreateEnvironment - null functions (id: {0})", service.ServiceId);
                            }
                        }
                    } else {
                        _log.WarnFormat("CreateEnvironment - null extension (id: {0})", service.ServiceId);
                    }
                } else {
                    _log.Warn("CreateEnvironment - null service");
                }
            }
            return env;
        }

        public static void InitializeCustomDekiScriptHeaders(PageBE page) {
            var current = DreamContext.Current;
            DekiScriptMap env = current.GetState<DekiScriptMap>("pageimplicitenv-" + page.ID);

            // check if we already have an initialized environment
            if(env == null) {
                DekiContext deki = DekiContext.Current;
                DekiInstance instance = deki.Instance;
                env = new DekiScriptMap();

                // add site fields
                DekiScriptMap siteFields = new DekiScriptMap();
                siteFields.Add("name", DekiScriptExpression.Constant(instance.SiteName));
                siteFields.Add("host", DekiScriptExpression.Constant(deki.UiUri.Uri.Host));
                siteFields.Add("language", DekiScriptExpression.Constant(instance.SiteLanguage));
                siteFields.Add("uri", DekiScriptExpression.Constant(deki.UiUri.Uri.ToString()));
                siteFields.Add("id", DekiScriptExpression.Constant(instance.Id));
                env.Add("site", siteFields);

                // add page fields
                DekiScriptMap pageFields = new DekiScriptMap();
                pageFields.Add("title", DekiScriptExpression.Constant(page.Title.AsUserFriendlyName()));
                pageFields.Add("path", DekiScriptExpression.Constant(page.Title.AsPrefixedDbPath()));
                pageFields.Add("namespace", DekiScriptExpression.Constant(Title.NSToString(page.Title.Namespace)));
                pageFields.Add("id", DekiScriptExpression.Constant(page.ID.ToString()));
                pageFields.Add("uri", DekiScriptExpression.Constant(Utils.AsPublicUiUri(page.Title)));
                pageFields.Add("date", DekiScriptExpression.Constant(page.TimeStamp.ToString("R")));
                pageFields.Add("language", DekiScriptExpression.Constant(string.IsNullOrEmpty(page.Language) ? null : page.Language));
                env.Add("page", pageFields);

                // add user fields
                DekiScriptMap userFields = new DekiScriptMap();
                if(deki.User != null) {
                    UserBE user = deki.User;
                    userFields.Add("id", DekiScriptExpression.Constant(user.ID.ToString()));
                    userFields.Add("name", DekiScriptExpression.Constant(user.Name));
                    userFields.Add("uri", DekiScriptExpression.Constant(Utils.AsPublicUiUri(Title.FromDbPath(NS.USER, user.Name, null))));
                    userFields.Add("emailhash", DekiScriptExpression.Constant(StringUtil.ComputeHashString((user.Email ?? string.Empty).Trim().ToLowerInvariant(), Encoding.UTF8)));
                    userFields.Add("anonymous", DekiScriptExpression.Constant(UserBL.IsAnonymous(user).ToString().ToLowerInvariant()));
                    userFields.Add("language", DekiScriptExpression.Constant(string.IsNullOrEmpty(user.Language) ? null : user.Language));
                } else {
                    userFields.Add("id", DekiScriptExpression.Constant("0"));
                    userFields.Add("name", DekiScriptExpression.Constant(string.Empty));
                    userFields.Add("uri", DekiScriptExpression.Constant(string.Empty));
                    userFields.Add("emailhash", DekiScriptExpression.Constant(string.Empty));
                    userFields.Add("anonymous", DekiScriptExpression.Constant("true"));
                    userFields.Add("language", DekiScriptNil.Value);
                }
                env.Add("user", userFields);

                // store env for later
                current.SetState("pageimplicitenv-" + page.ID, env);
            }

            // set implicit environment
            DreamContext.Current.SetState(env);
        }
    }
}

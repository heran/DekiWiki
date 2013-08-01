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
using System.Globalization;
using System.Linq;
using System.Text;
using MindTouch.Deki.Data;
using MindTouch.Deki.Exceptions;
using MindTouch.Deki.Logic;
using MindTouch.Dream;
using MindTouch.Tasking;
using MindTouch.Xml;

namespace MindTouch.Deki {
    using Yield = IEnumerator<IYield>;

    public partial class DekiWikiService {

        //--- Constants ---
        private const string LOGO_LABEL = "LOGO";

        //--- Features ---
        [DreamFeature("GET:site/status", "Get mindtouch instance status information")]
        [DreamFeatureStatus(DreamStatus.Ok, "The request completed successfully")]
        [DreamFeatureStatus(DreamStatus.BadRequest, "Invalid input parameter or request body")]
        [DreamFeatureStatus(DreamStatus.Forbidden, "User must be logged in")]
        public Yield GetSiteStatus(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            PermissionsBL.CheckUserAllowed(DekiContext.Current.User, Permissions.UPDATE);
            var status = new XDoc("status")
                .Elem("state", DekiContext.Current.Instance.Status);
            response.Return(DreamMessage.Ok(status));
            yield break;
        }

        [DreamFeature("GET:site/functions", "Get list of available extensions")]
        [DreamFeatureParam("format", "{html, body, xml}?", "output format (default: html)")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        [DreamFeatureStatus(DreamStatus.Ok, "The request completed successfully")]
        [DreamFeatureStatus(DreamStatus.BadRequest, "Invalid input parameter or request body")]
        [DreamFeatureStatus(DreamStatus.Forbidden, "User must be logged in")]
        public Yield GetSiteFunctions(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            if(UserBL.IsAnonymous(DekiContext.Current.User)) {
                throw new SiteMustBeLoggedInForbiddenException();
            }

            // build set of libraries
            List<XDoc> libraries = DekiContext.Current.Instance.RunningServices.ExtensionServices
                .Select(x => x.Extension.Manifest).ToList();

            // add registered libraries
            libraries.Sort((left, right) => left["title"].Contents.CompareInvariantIgnoreCase(right["title"].Contents));

            // add built-in functions
            XDoc builtinlib = new XDoc("extension");
            builtinlib.Elem("title", "Built-in Functions");
            builtinlib.Elem("label", "Built-in");
            builtinlib.Elem("uri.help", "http://wiki.developer.mindtouch.com/MindTouch_Deki/DekiScript/Reference");
            builtinlib.Elem("description", "The following functions and variables are part the DekiScript and MindTouch runtime environment.");
            foreach(var function in ScriptRuntime.Functions.Values) {
                if(function.Access == DreamAccess.Public) {
                    builtinlib.Add(function.ToXml(null));
                }
            }
            libraries.Insert(0, builtinlib);

            // create composite document
            bool hasUnsafeContentPermission = PermissionsBL.IsUserAllowed(DekiContext.Current.User, Permissions.UNSAFECONTENT);
            XDoc extensions = new XDoc("extensions").AddAll(libraries);
            foreach(XDoc extension in extensions["extension"]) {
                XUri serviceUri = extension["@uri"].AsUri;

                // check if extension is protected
                bool @protected;
                bool.TryParse(ExtensionBL.GetExtensionPreference(serviceUri, "protected"), out @protected);
                if(@protected) {
                    if(!hasUnsafeContentPermission) {
                        extension.Remove();
                        continue;
                    }
                    extension.Attr("protected", @protected);
                }

                // read overwriteable settings
                AddOrReplace(extension, "title", ExtensionBL.GetExtensionPreference(serviceUri, "title.custom"));
                AddOrReplace(extension, "label", ExtensionBL.GetExtensionPreference(serviceUri, "label.custom"));
                AddOrReplace(extension, "uri.logo", ExtensionBL.GetExtensionPreference(serviceUri, "uri.logo.custom"));
                AddOrReplace(extension, "namespace", ExtensionBL.GetExtensionPreference(serviceUri, "namespace.custom"));
                extension.Elem("description.custom", ExtensionBL.GetExtensionPreference(serviceUri, "description.custom"));

                // check which functions to keep
                string[] allowedFunctions = (ExtensionBL.GetExtensionPreference(serviceUri, "functions") ?? string.Empty).Split(new char[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
                if(allowedFunctions.Length > 0) {
                    foreach(XDoc function in extension["function"]) {

                        // check if user specified a list of functions to show
                        string name = function["name"].Contents;
                        if(Array.FindIndex(allowedFunctions, current => current.EqualsInvariantIgnoreCase(name)) < 0) {
                            function.Remove();
                        }
                    }
                }

                // check if extension has any functions
                if(extension["function"].ListLength == 0) {
                    extension.Remove();
                }
            }

            // build response document
            string format = context.GetParam("format", "html");
            if(StringUtil.EqualsInvariant(format, "xml")) {
                response.Return(DreamMessage.Ok(extensions));
            } else {

                // prepare document
                string header = string.Format("{0} - Registered Extensions", DekiContext.Current.Instance.SiteName);
                XDoc result = new XDoc("html").Attr("xmlns", "http://www.w3.org/1999/xhtml")
                    .Start("head")
                        .Elem("title", header)
                        .Start("meta").Attr("http-equiv", "content-type").Attr("content", "text/html;charset=utf-8").End()
                    .End();
                result.Start("body");
                result.Elem("h1", header);

                // build table of contents
                result.Elem("strong", "Table of Contents");
                result.Start("ol");
                int count = 0;
                foreach(XDoc library in extensions["extension"]) {
                    ++count;
                    XUri serviceUri = library["@uri"].AsUri;
                    result.Start("li")
                        .Start("a")
                            .Attr("href", "#section" + count)
                            .Value(ExtensionBL.GetExtensionPreference(serviceUri, "title.custom") ?? library["title"].AsText)
                        .End()
                    .End();
                }
                result.End();

                // enumerate libraries
                count = 0;
                foreach(XDoc library in extensions["extension"]) {
                    ++count;

                    // read overwriteable settings
                    string title = library["title"].AsText;
                    string logo = library["uri.logo"].AsText;
                    string ns = library["namespace"].AsText;
                    bool @protected = library["@protected"].AsBool ?? false;

                    // show & link library name
                    result.Start("h2").Attr("id", "section" + count);
                    if(!string.IsNullOrEmpty(library["uri.help"].AsText)) {
                        result.Start("a").Attr("href", library["uri.help"].AsText).Attr("target", "_blank").Attr("title", library["title"].AsText + " Documentation").Value(title).End();
                    } else {
                        result.Value(title);
                    }
                    if(@protected) {
                        var resources = DekiContext.Current.Resources;
                        var builder = new DekiResourceBuilder();
                        builder.Append(" (");
                        builder.Append(DekiResources.PROTECTED());
                        builder.Append(")");
                        result.Value(builder.Localize(resources));
                    }
                    result.End();

                    // show optional logo
                    if(!string.IsNullOrEmpty(logo)) {
                        result.Start("img").Attr("src", logo).Attr("alt", title).End();
                    }

                    // show descriptions
                    if(library["uri.license"].AsText != null) {
                        result.Start("a").Attr("href", library["uri.license"].AsText).Attr("target", "_blank").Value("Read Library License").End();
                    }
                    if(!string.IsNullOrEmpty(library["description"].AsText)) {
                        result.Elem("p", library["description"].AsText);
                    }
                    if(!string.IsNullOrEmpty(library["description.custom"].AsText)) {
                        result.Elem("p", library["description.custom"].AsText);
                    }

                    // enumerate library functions
                    XDoc functions = new XDoc("functions").AddAll(library["function"]);
                    functions.Sort(delegate(XDoc left, XDoc right) {
                        return StringUtil.CompareInvariantIgnoreCase(left["name"].Contents, right["name"].Contents);
                    });
                    foreach(XDoc function in functions["function"]) {
                        AddFunction(result, ns, function);
                    }
                }
                result.End();
                switch(format) {
                default:
                case "html":
                    response.Return(DreamMessage.Ok(MimeType.HTML, result.ToString()));
                    break;
                case "body":
                    response.Return(DreamMessage.Ok(MimeType.TEXT_UTF8, result["body"].Contents));
                    break;
                }
            }
            yield break;
        }

        // TODO (brigettek): this feature currently always fails.  Commenting out until we need/fix it.
        //  [DreamFeature("POST:site/notifyadmin", "Notifies the site admin")]/
        //  [DreamFeatureParam("subject", "string", "Subject of the notice")]
        //  [DreamFeatureStatus(DreamStatus.Ok, "The request completed successfully")]
        //  [DreamFeatureStatus(DreamStatus.BadRequest, "Invalid input parameter or request body")]
        //  [DreamFeatureStatus(DreamStatus.Forbidden, "User must be logged in")]
        public Yield PostSiteNotifyAdmin(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            if((DekiContext.CurrentOrNull == null) || UserBL.IsAnonymous(DekiContext.Current.User)) {
                throw new SiteMustBeLoggedInForbiddenException();
            }
            var siteBL = new SiteBL();
            siteBL.SendNoticeToAdmin(context.GetParam("subject"), request.AsText(), request.ContentType);
            response.Return(DreamMessage.Ok());
            yield break;
        }

        [DreamFeature("GET:site/localization", "Retrieve a resource string localized for the current user, or provided culture")]
        [DreamFeatureParam("resource", "string", "resource name to retrieve")]
        [DreamFeatureParam("lang", "string?", "Optional language code to use for resource localization")]
        [DreamFeatureStatus(DreamStatus.Ok, "The request completed successfully")]
        [DreamFeatureStatus(DreamStatus.BadRequest, "Invalid input parameter or request body")]
        [DreamFeatureStatus(DreamStatus.NotFound, "Could not find requested resource")]
        public Yield GetLocalizedString(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            string resource = context.GetParam("resource");
            string language = context.GetParam("lang", null);
            CultureInfo culture = CultureUtil.GetNonNeutralCulture(language) ?? DreamContext.Current.Culture;
            string value = ResourceManager.GetString(resource, culture, null);
            if(value == null) {
                throw new SiteNoSuchLocalizationResourceNotFoundException(resource);
            }
            response.Return(DreamMessage.Ok(MimeType.TEXT_UTF8, value));
            yield break;
        }

        #region Config features

        [DreamFeature("GET:site/settings", "Retrieve all configuration settings")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        [DreamFeatureParam("include", "string", "Optional parameter used to include anonymous user and license information (possible values: anonymous, license). By default we do not include them.")]
        [DreamFeatureStatus(DreamStatus.Ok, "The request completed successfully")]
        [DreamFeatureStatus(DreamStatus.Forbidden, "MindTouch API key or Administrator access is required.")]
        internal Yield GetSiteSettings(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            var givenKey = context.GetParam("apikey", null);
            var includes = context.GetParam("include", "");
            
            //If apikey is not given for a request, dont return hidden entries
            var validMasterKey = MasterApiKey.EqualsInvariant(givenKey);
            var retrieve = new SiteSettingsRetrievalSettings {
                IncludeHidden = validMasterKey,
                IncludeAnonymousUser = includes.Contains(UserBL.ANON_USERNAME),
                IncludeLicense = (validMasterKey || PermissionsBL.IsUserAllowed(DekiContext.Current.User, Permissions.ADMIN)) && includes.Contains(ConfigBL.LICENSE),
            };

            var doc = ConfigBL.GetInstanceSettingsAsDoc(retrieve);

            // check if a custom logo was uploaded; if yes, update the config document. 
            // This is being done outside of caching since URIs to the api should be computed for every request
            if(doc[ConfigBL.UI_LOGO_UPLOADED].AsBool ?? false) {
                doc.InsertValueAt(ConfigBL.UI_LOGO_URI, DekiContext.Current.ApiUri.At("site", "logo.png").ToString());
            }
            response.Return(DreamMessage.Ok(doc));
            yield break;
        }

        [DreamFeature("PUT:site/settings", "Set all configuration settings")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        [DreamFeatureStatus(DreamStatus.Ok, "The request completed successfully")]
        [DreamFeatureStatus(DreamStatus.BadRequest, "Invalid input parameter or request body")]
        [DreamFeatureStatus(DreamStatus.Forbidden, "MindTouch API key or Administrator access is required.")]
        internal Yield PutSiteSettings(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            if(!MimeType.XML.Match(request.ContentType)) {
                throw new SiteExpectedXmlContentTypeInvalidArgumentException();
            }
            var dekiContext = DekiContext.Current;
            XDoc settings = request.ToDocument();
            ConfigBL.SetInstanceSettings(settings);
            dekiContext.Instance.EventSink.InstanceSettingsChanged(DekiContext.Current.Now);
            yield return Coroutine.Invoke(ConfigureMailer, settings, new Result()).CatchAndLog(_log);

            // clear out the digital signature key
            dekiContext.Instance.PrivateDigitalSignature = null;

            // clear out banned words
            dekiContext.Instance.BannedWords = null;

            response.Return(DreamMessage.Ok());
            yield break;
        }

        #endregion

        #region Import/Export features

        [DreamFeature("POST:site/export", "Generates export information")]
        [DreamFeatureParam("relto", "int?", "Page used for path normalization (default:  home page)")]
        [DreamFeatureParam("reltopath", "string?", "Page used for path normalization.  Ignored if relto parameter is defined.")]
        [DreamFeatureStatus(DreamStatus.Ok, "The request completed successfully")]
        [DreamFeatureStatus(DreamStatus.BadRequest, "Invalid input parameter or request body")]
        public Yield SiteExport(DreamContext context, DreamMessage request, Result<DreamMessage> response) {

            // Retrieve the title used for path normalization (if any)
            Title relToTitle = Utils.GetRelToTitleFromUrl(context) ?? Title.FromDbPath(NS.MAIN, String.Empty, null);

            // Retrieve data to export
            XDoc requestDoc = request.ToDocument();
            if(requestDoc == null || requestDoc.IsEmpty || !requestDoc.HasName("export")) {
                throw new PostedDocumentInvalidArgumentException("export");
            }

            // Perform the export
            MindTouch.Deki.Export.SiteExportBuilder exportBuilder = new MindTouch.Deki.Export.SiteExportBuilder(relToTitle);
            exportBuilder.Append(requestDoc);
            response.Return(DreamMessage.Ok(exportBuilder.ToDocument()));
            yield break;
        }

        [DreamFeature("POST:site/import", "Generates import information")]
        [DreamFeatureParam("relto", "int?", "Page used for path normalization (default: home page)")]
        [DreamFeatureParam("reltopath", "string?", "Page used for path normalization.  Ignored if relto parameter is defined.")]
        [DreamFeatureParam("forceoverwrite", "bool?", "Force overwrite of destination, even if import content is older.")]
        [DreamFeatureStatus(DreamStatus.Ok, "The request completed successfully")]
        [DreamFeatureStatus(DreamStatus.BadRequest, "Invalid input parameter or request body")]
        public Yield SiteImport(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            var forceOverwrite = context.GetParam("forceoverwrite", false);
            // Retrieve the title used for path normalization (if any)
            Title relToTitle = Utils.GetRelToTitleFromUrl(context) ?? Title.FromDbPath(NS.MAIN, String.Empty, null);

            // Retrieve the manifest describing the import
            XDoc manifestDoc = request.ToDocument();
            if(manifestDoc == null || manifestDoc.IsEmpty || !manifestDoc.HasName("manifest")) {
                throw new PostedDocumentInvalidArgumentException("manifest");
            }

            // Perform the import
            MindTouch.Deki.Export.SiteImportBuilder importBuilder = new MindTouch.Deki.Export.SiteImportBuilder(relToTitle, forceOverwrite);
            importBuilder.Append(manifestDoc);
            response.Return(DreamMessage.Ok(importBuilder.ToDocument()));
            yield break;
        }
        #endregion

        #region Logo features
        [DreamFeature("GET:site/logo", "Retrieve the site logo image")]
        [DreamFeature("GET:site/logo.png", "Retrieve the site logo image")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        public Yield GetSiteLogo(DreamContext context, DreamMessage request, Result<DreamMessage> response) {

            DreamMessage responseMsg = null;
            DateTime modified = DekiContext.Current.Instance.Storage.GetSiteFileTimestamp(LOGO_LABEL);
            try {
                if(modified != DateTime.MinValue) {
                    if(request.CheckCacheRevalidation(modified)) {
                        responseMsg = DreamMessage.NotModified();
                    }
                }

                if(responseMsg == null) {
                    StreamInfo file = DekiContext.Current.Instance.Storage.GetSiteFile(LOGO_LABEL, false);
                    if(file != null) {
                        responseMsg = DreamMessage.Ok(MimeType.PNG, file.Length, file.Stream);

                        //Build the content disposition headers
                        responseMsg.Headers.ContentDisposition = new ContentDisposition(true, file.Modified ?? DateTime.UtcNow, null, null, "logo.png", file.Length);

                        //Set caching headers
                        responseMsg.SetCacheMustRevalidate(modified);
                    } else {
                        responseMsg = DreamMessage.NotFound("Logo has not been uploaded");
                    }
                }
            } catch {
                if(responseMsg != null) {
                    responseMsg.Close();
                }
                throw;
            }
            response.Return(responseMsg);
            yield break;
        }

        [DreamFeature("PUT:site/logo", "Save a new site logo image")]
        [DreamFeature("PUT:site/logo.png", "Save a new site logo image")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        internal Yield PutSiteLogo(DreamContext context, DreamMessage request, Result<DreamMessage> response) {

            //Confirm image type
            if(!new MimeType("image/*").Match(request.ContentType)) {
                throw new SiteImageMimetypeInvalidArgumentException();
            }
            try {
                //Save file to storage provider
                DekiContext.Current.Instance.Storage.PutSiteFile(LOGO_LABEL, new StreamInfo(request.AsStream(), request.ContentLength, request.ContentType));
                ConfigBL.SetInstanceSettingsValue(ConfigBL.UI_LOGO_UPLOADED, "true");
            } catch(Exception x) {
                DekiContext.Current.Instance.Log.Warn("Failed to save logo to storage provider", x);
                ConfigBL.DeleteInstanceSettingsValue(ConfigBL.UI_LOGO_UPLOADED);
                throw;
            }

            StreamInfo file = DekiContext.Current.Instance.Storage.GetSiteFile(LOGO_LABEL, false);
            if(file != null) {
                StreamInfo thumb = AttachmentPreviewBL.BuildThumb(file, FormatType.PNG, RatioType.UNDEFINED, DekiContext.Current.Instance.LogoWidth, DekiContext.Current.Instance.LogoHeight);
                if(thumb != null) {
                    DekiContext.Current.Instance.Storage.PutSiteFile(LOGO_LABEL, thumb);
                } else {
                    DekiContext.Current.Instance.Log.WarnMethodCall("PUT:site/logo", "Unable to process logo through imagemagick");
                    DekiContext.Current.ApiPlug.At("site", "logo").Delete();
                    throw new SiteUnableToProcessLogoInvalidOperationException();
                }
            } else {
                DekiContext.Current.Instance.Log.WarnMethodCall("PUT:site/logo", "Unable to retrieve saved logo");
            }
            response.Return(DreamMessage.Ok());
            yield break;
        }

        [DreamFeature("DELETE:site/logo", "Remove the site logo")]
        [DreamFeature("DELETE:site/logo.png", "Remove the site logo")]
        [DreamFeatureParam("authenticate", "bool?", "Force authentication for request (default: false)")]
        internal Yield DeleteSiteLogo(DreamContext context, DreamMessage request, Result<DreamMessage> response) {
            DekiContext.Current.Instance.Storage.DeleteSiteFile(LOGO_LABEL);
            ConfigBL.DeleteInstanceSettingsValue(ConfigBL.UI_LOGO_UPLOADED);
            response.Return(DreamMessage.Ok());
            yield break;
        }
        #endregion

        //--- Methods ---
        private void AddFunction(XDoc result, string ns, XDoc function) {
            result.Start("blockquote");
            List<Tuplet<string, bool, string, string>> args = new List<Tuplet<string, bool, string, string>>();
            StringBuilder signature = new StringBuilder();
            signature.Append(((ns != null) ? ns + "." : string.Empty) + function["name"].AsText);
            if(string.IsNullOrEmpty(function["@usage"].AsText)) {
                signature.Append("(");

                // enumerate arguments
                int count = 1;
                foreach(XDoc arg in function["param"]) {

                    // add argument to signature
                    if(count > 1) {
                        signature.Append(", ");
                    }
                    string name = arg["@name"].AsText ?? arg["name"].AsText ?? ("arg" + count.ToString());
                    signature.Append(name);
                    string type = arg["@type"].AsText ?? arg["type"].AsText;
                    if(type != null) {
                        signature.Append(" : ");
                        signature.Append(type);
                    }
                    ++count;

                    // add argument to explanation
                    if(!arg["hint"].IsEmpty || !string.IsNullOrEmpty(arg.AsText)) {
                        args.Add(new Tuplet<string, bool, string, string>(name, StringUtil.EqualsInvariant(arg["@optional"].AsText, "true") || !arg["@default"].IsEmpty || !arg["hint[@optional='true']"].IsEmpty, arg["hint"].AsText ?? arg.AsText, arg["@default"].AsText));
                    }
                }
                signature.Append(")");
            }
            signature.Append(" : ").Append(function["return/@type"].AsText ?? "any");
            result.Elem("h3", signature.ToString());
            if(function["description"].AsText != null) {
                result.Elem("p", function["description"].AsText);
            }

            // add argument explanation
            if(args.Count > 0) {
                result.Start("ul");
                foreach(Tuplet<string, bool, string, string> arg in args) {
                    result.Start("li");
                    result.Elem("strong", arg.Item1);
                    if(arg.Item2) {
                        result.Value(" (optional)");
                    }
                    result.Value(": " + arg.Item3);
                    if(arg.Item4 != null) {
                        result.Value(" (default: " + arg.Item4 + ")");
                    }
                    result.End();
                }
                result.End();
            }
            result.Elem("br");
            result.End();
        }

        private void AddOrReplace(XDoc doc, string key, string value) {
            if(value != null) {
                XDoc item = doc[key];
                if(item.IsEmpty) {
                    doc.Elem(key, value);
                } else {
                    item.ReplaceValue(value);
                }
            }
        }
    }
}

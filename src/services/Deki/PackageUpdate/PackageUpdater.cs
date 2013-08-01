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
using System.IO;
using log4net;
using MindTouch.Deki.Import;
using MindTouch.Deki.Util;
using MindTouch.Dream;
using MindTouch.Tasking;
using MindTouch.Xml;
using System.Linq;

namespace MindTouch.Deki.PackageUpdate {

    public class PackageUpdater : IDisposable {

        //--- Constants ---
        private const string PACKAGE_PROPERTY_NS = "mindtouch.packageupdater.imported#";

        //--- Class Fields ---
        private static readonly ILog _log = LogUtils.CreateLog();

        //--- Fields ---
        private readonly string _templatePackagePath;
        private readonly object _syncroot = new object();
        private bool _disposed;
        private PackageUpdaterStatus _status = PackageUpdaterStatus.New;

        //--- Constructors ---
        public PackageUpdater(string templatePackagePath) {
            _templatePackagePath = Path.GetFullPath(templatePackagePath);
        }

        //--- Properties ---
        public PackageUpdaterStatus Status {
            get { return _status; }
        }

        //--- Methods ---
        public Result<XDoc> UpdatePackages(Plug api, string wikiId, string apikey, bool force, bool init, Result<XDoc> result) {
            lock(_syncroot) {
                EnsureInstanceNotDisposed();
                return Coroutine.Invoke(UpdatePackages_Helper, api, wikiId, apikey, force, init, result);
            }
        }

        private IEnumerator<IYield> UpdatePackages_Helper(Plug api, string wikiId, string apikey, bool force, bool init, Result<XDoc> result) {
            try {
                _status = PackageUpdaterStatus.Importing;
                yield return Coroutine.Invoke(AuthenticateImportUser, api, wikiId, apikey, new Result<Plug>()).Set(x => api = x);
                XDoc license = null;
                yield return api.At("license").With("apikey", apikey).Get(new Result<XDoc>()).Set(x => license = x);
                var importReport = new XDoc("packages");
                var first = true;
                foreach(var directory in Directory.GetDirectories(_templatePackagePath)) {
                    var directoryName = Path.GetFileName(directory);
                    string restriction = null;
                    if(directoryName.EqualsInvariantIgnoreCase("public")) {
                        restriction = "Public";
                    } else if(directoryName.EqualsInvariantIgnoreCase("semi-public")) {
                        restriction = "Semi-Public";
                    } else if(directoryName.EqualsInvariantIgnoreCase("private")) {
                        restriction = "Private";
                    }
                    foreach(var package in Directory.GetFiles(directory, "*.mt*").OrderBy(x => x)) {
                        var ext = Path.GetExtension(package);
                        if(!(ext.EqualsInvariantIgnoreCase(".mtarc") || ext.EqualsInvariantIgnoreCase(".mtapp"))) {
                            continue;
                        }
                        if(!first) {
                            importReport.End();
                        }
                        first = false;
                        importReport.Start("package").Elem("path", package);
                        ArchivePackageReader packageReader;
                        _log.DebugFormat("contemplating import of '{0}' for '{1}'", package, wikiId);
                        try {
                            packageReader = new ArchivePackageReader(package);
                        } catch(Exception e) {
                            SetError(importReport, wikiId, "error", e, "Unable to open package.");
                            continue;
                        }
                        Result<XDoc> manifestResult;
                        yield return manifestResult = packageReader.ReadManifest(new Result<XDoc>()).Catch();
                        if(manifestResult.HasException) {
                            SetError(importReport, wikiId, "error", manifestResult.Exception, "Unable to read package manifest.");
                            continue;
                        }
                        var manifest = manifestResult.Value;

                        // check for required capabilities
                        var capabilitiesSatisfied = true;
                        foreach(var capability in manifest["capability"]) {
                            var capabilityName = capability["@name"].AsText;
                            var capabilityValue = capability["@value"].AsText.IfNullOrEmpty("enabled");
                            if(string.IsNullOrEmpty(capabilityName)) {
                                continue;
                            }
                            var licenseCapabilityValue = DekiLicense.GetCapability(license, capabilityName) ?? "";
                            if(!string.IsNullOrEmpty(capabilityName) && licenseCapabilityValue.EqualsInvariant(capabilityValue)) {
                                continue;
                            }
                            capabilitiesSatisfied = false;
                            SetError(importReport, wikiId, "error", null, "Missing capability '{0}' or incorrect capability value, expected '{1}' and got '{2}'.", capabilityName, capabilityValue, licenseCapabilityValue);
                        }
                        if(!capabilitiesSatisfied) {
                            continue;
                        }

                        // add security xml if we are in a restriction enforcing path
                        if(!string.IsNullOrEmpty(restriction)) {
                            manifest.Start("security")
                                .Start("permissions.page")
                                .Elem("restriction", restriction)
                                .End()
                                .End();
                        }

                        // if package predates @date.created, take file modified
                        var dateCreated = manifest["@date.created"].AsDate ?? new FileInfo(package).LastWriteTime;

                        // check whether we should import this package
                        var filename = Path.GetFileName(package);
                        var importOnce = manifest["@import-once"].AsBool ?? false;
                        var initPackage = manifest["@init-only"].AsBool ?? false;
                        importReport.Elem("name", filename).Attr("date.created", dateCreated).Attr("preserve-local", manifest["@preserve-local"].AsBool ?? false);
                        var importPropertyName = XUri.EncodeSegment(PACKAGE_PROPERTY_NS + filename);
                        if(initPackage && !init && !force) {
                            SetError(importReport, wikiId, "skipped", null, "package '{0}' is an init package and neither init or force flags were set, skipping", package);
                            continue;
                        }
                        if(force && !importOnce) {
                            _log.DebugFormat("force is set and package '{0}' is not marked as oneTimeOnly, proceeding with import", package);
                        } else {
                            DreamMessage propertyResponse = null;
                            yield return api.At("site", "properties", importPropertyName).Get(new Result<DreamMessage>()).Set(x => propertyResponse = x);
                            if(propertyResponse.IsSuccessful) {
                                var importedPackage = propertyResponse.ToDocument();
                                var importedPackageDate = importedPackage["date.created"].AsDate;
                                if(!importedPackageDate.HasValue) {
                                    _log.WarnFormat("unable to retrieve imported package date.created for '{0}', treating package as new", package);
                                } else if(dateCreated > importedPackageDate.Value) {
                                    _log.DebugFormat("package '{0}' is newer, proceed with import ({1} > {2}", package, dateCreated, importedPackageDate.Value);
                                } else {
                                    SetError(importReport, wikiId, "skipped", null,
                                             "package '{0}' is not newer, skip import ({1} <= {2}.)",
                                             package,
                                             dateCreated,
                                             importedPackageDate.Value);
                                    continue;
                                }
                            } else if(propertyResponse.Status == DreamStatus.Unauthorized) {
                                _log.WarnFormat("apiuri has lost its authentication, dropping out");
                                throw new UnauthorizedAccessException("Authentication for was lost");
                            } else {
                                _log.DebugFormat("package '{0}' has not previously been imported, proceeding with import", package);
                            }
                        }

                        // import package
                        Result<Importer> importerResult;
                        yield return importerResult = Importer.CreateAsync(api, manifest, "/", new Result<Importer>()).Catch();
                        if(importerResult.HasException) {
                            SetError(importReport, wikiId, "error", importerResult.Exception, "Unable to create importer for package.");
                            continue;
                        }
                        var importer = importerResult.Value;
                        var importManager = new ImportManager(importer, packageReader);
                        Result importResult;
                        yield return importResult = importManager.ImportAsync(new Result()).Catch();
                        if(importResult.HasException) {
                            SetError(importReport, wikiId, "error", importResult.Exception, "Import did not complete successfully.");
                            continue;
                        }

                        // write import data as site property
                        yield return api.At("site", "properties", importPropertyName)
                            .With("abort", "never")
                            .With("description", string.Format("Import of package '{0}'", filename))
                            .Put(new XDoc("package").Elem("date.created", dateCreated), new Result<DreamMessage>());
                        importReport.Start("status").Attr("code", "ok").End();
                        _log.DebugFormat("sucessfully imported package '{0}'", package);
                    }
                }
                if(!first) {
                    importReport.End();
                }
                result.Return(importReport);
                yield break;
            } finally {
                _status = PackageUpdaterStatus.Idle;
            }
        }

        private IEnumerator<IYield> AuthenticateImportUser(Plug api, string wikiId, string apikey, Result<Plug> result) {
            XDoc settings = null;
            yield return api.At("site", "settings").With("apikey", apikey).GetAsync().Set(x => settings = x.ToDocument());
            var username = settings["packageupdater/username"].AsText ?? settings["security/admin-user-for-impersonation"].AsText;
            string authToken = null;
            if(string.IsNullOrEmpty(username)) {
                _log.DebugFormat("[{0}] no user in 'packageupdater/username' config key, trying 'admin'", wikiId);
                username = "admin";
                yield return Coroutine.Invoke(Authenticate, api, username, apikey, new Result<string>()).Set(x => authToken = x);
                if(string.IsNullOrEmpty(authToken)) {
                    _log.DebugFormat("[{0}] 'admin' didn't authenticate, trying 'sysop'", wikiId);
                    username = "sysop";
                }
            }
            if(string.IsNullOrEmpty(authToken)) {
                _log.DebugFormat("[{0}] authenticating as '{1}'", wikiId, username);
                yield return Coroutine.Invoke(Authenticate, api, username, apikey, new Result<string>()).Set(x => authToken = x);
            }
            if(string.IsNullOrEmpty(authToken)) {
                throw new UnauthorizedAccessException(string.Format("Unable to authenticate as '{0}'", username));
            }
            result.Return(api.WithHeader(DekiWikiService.AUTHTOKEN_HEADERNAME, authToken));
        }

        private void SetError(XDoc doc, string wikiId, string status, Exception e, string message, params object[] args) {
            var exceptionMessage = e == null ? "" : e.Message;
            _log.DebugFormat("[" + wikiId + "] " + message + ": " + exceptionMessage, args);
            doc.Start("status")
                .Attr("code", status)
                .Elem("message", string.Format(message, args));
            if(e != null) {
                doc.Add(new XException(e));
            }
            doc.End();
        }

        private IEnumerator<IYield> Authenticate(Plug api, string username, string apikey, Result<string> result) {
            yield return api.At("users", "authenticate")
                .With("apikey", apikey)
                .WithCredentials(username, null)
                .Get(new Result<DreamMessage>()).Set(x => result.Return(x.IsSuccessful ? x.ToText() : null));
        }

        public void Dispose() {
            if(_disposed) {
                return;
            }
            lock(_syncroot) {
                _disposed = true;
            }
        }

        private void EnsureInstanceNotDisposed() {
            if(_disposed) {
                throw new ObjectDisposedException("The package updater instance has been disposed");
            }
        }
    }

    public enum PackageUpdaterStatus {
        New,
        Importing,
        Idle
    }
}
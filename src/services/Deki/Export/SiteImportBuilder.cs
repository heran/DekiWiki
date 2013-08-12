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
using log4net;
using MindTouch.Deki.Data;
using MindTouch.Deki.Exceptions;
using MindTouch.Deki.Logic;
using MindTouch.Dream;
using MindTouch.Xml;

namespace MindTouch.Deki.Export {
    internal class SiteImportBuilder {

        //--- Constants ---
        public const string LAST_IMPORT = "mindtouch.import#info";

        //--- Class Fields ---
        private static readonly ILog _log = LogUtils.CreateLog();

        //--- Fields ---
        private readonly Dictionary<XUri, object> _uris;
        private readonly XDoc _requestDoc;
        private readonly Title _relToTitle;
        private readonly bool _forceoverwrite;

        //--- Constructors ---
        public SiteImportBuilder(Title relToTitle, bool forceoverwrite) {
            _uris = new Dictionary<XUri, object>();
            _requestDoc = new XDoc("requests");
            _relToTitle = relToTitle;
            _forceoverwrite = forceoverwrite;
        }

        //--- Methods ---
        public void Append(XDoc manifestDoc) {
            XUri apiUri = DekiContext.Current.ApiUri;
            _log.DebugFormat("building import plan against api uri: {0}", apiUri.AsPublicUri());
            bool preserveExistingLocalChanges = manifestDoc["@preserve-local"].AsBool ?? false;
            var security = manifestDoc["security"];
            foreach(XDoc importXml in manifestDoc.Elements) {
                try {
                    if(importXml.HasName("page")) {
                        if(importXml["path"].IsEmpty) {
                            throw new PageIdInvalidArgumentException();
                        }

                        // Generate the request needed to import the page
                        Title title = Title.FromRelativePath(_relToTitle, importXml["path"].Contents);
                        XUri uri = apiUri.At("pages", title.AsApiParam(), "contents").With("reltopath", _relToTitle.AsPrefixedDbPath());
                        _uris[uri] = null;
                        uri = uri.With("language", importXml["language"].AsText)
                                 .With("title", importXml["title"].AsText)
                                 .With("edittime", DateTime.MaxValue.ToString("yyyyMMddHHmmss"))
                                 .With("redirects", "0");
                        var importEtag = importXml["etag"].AsText;
                        var importDateModified = (importXml["date.modified"].AsDate ?? DateTime.MinValue).ToSafeUniversalTime();
                        if(importDateModified != DateTime.MinValue) {
                            uri = uri.With("importtime", importDateModified.ToString("yyyyMMddHHmmss"));
                        }

                        // unless we're forcing overwrite...
                        if(!_forceoverwrite) {

                            // ...check for existing page
                            var page = PageBL.GetPageByTitle(title);
                            if(page != null && page.ID != 0 && !page.IsRedirect) {

                                // check for previous import
                                var lastImport = PropertyBL.Instance.GetPageProperty((uint)page.ID, LAST_IMPORT);
                                if(lastImport != null) {
                                    try {
                                        var content = lastImport.Content;
                                        var previousImportXml = XDocFactory.From(content.ToStream(), content.MimeType);
                                        var previousImportEtag = previousImportXml["etag"].AsText;
                                        var previousImportDateModified = (previousImportXml["date.modified"].AsDate ?? DateTime.MinValue).ToSafeUniversalTime();
                                        if(preserveExistingLocalChanges) {         // if we are supposed to preserve local changes
                                            if(importEtag == previousImportEtag || // and page either hasn't changed
                                               previousImportEtag != page.Etag     // or is locally modified
                                            ) {                                    // then skip importing the page
                                                _log.DebugFormat("skipping previously imported page '{0}', because page either hasn't changed or is locally modified", title);
                                                continue;
                                            }
                                        } else if(previousImportDateModified > importDateModified) {

                                            // if the previous import is newer, skip import
                                            _log.DebugFormat("skipping previously imported page '{0}', because previous import is newer", title);
                                            continue;
                                        }
                                    } catch(Exception e) {

                                        // if there was a parsing problem, skip import as a precaution
                                        _log.Warn(string.Format("a parsing problem occured with the import info property and import of page {0} is being skipped", page.ID), e);
                                        continue;
                                    }
                                } else if(preserveExistingLocalChanges) {

                                    // there was no previous import property, so the local content is by definition locally changed, ignore import
                                    _log.DebugFormat("skipping page '{0}', because of local content and preserve-local package preference", title);
                                    continue;
                                }
                            }
                        }
                        AddRequest("POST", uri, importXml["@dataid"].AsText, MimeType.XML.ToString());

                        // if we have security xml for the import, create a request command for it
                        if(!security.IsEmpty) {
                            AddBodyRequest("POST", apiUri.At("pages", title.AsApiParam(), "security"), security, MimeType.XML.ToString());
                        }
                    } else if(importXml.HasName("tags")) {


                        // Ensure that the tags have a corresponding import page
                        Title title = Title.FromRelativePath(_relToTitle, importXml["path"].Contents);
                        XUri pageUri = apiUri.At("pages", title.AsApiParam(), "contents").With("reltopath", _relToTitle.AsPrefixedDbPath());
                        if(!_uris.ContainsKey(pageUri)) {
                            PageBE page = PageBL.GetPageByTitle(title);
                            if((null == page) && (0 >= page.ID)) {
                                throw new PageIdInvalidArgumentException();
                            } else {
                                _uris[pageUri] = null;
                            }
                        }

                        // Generate the request to import the tags
                        XUri tagsUri = apiUri.At("pages", title.AsApiParam(), "tags");
                        _uris[tagsUri] = null;
                        tagsUri = tagsUri.With("redirects", "0");
                        AddRequest("PUT", tagsUri, importXml["@dataid"].AsText, MimeType.XML.ToString());
                    } else if(importXml.HasName("file")) {
                        if(importXml["filename"].IsEmpty) {
                            throw new AttachmentMissingFilenameInvalidArgumentException();
                        }

                        // Ensure that the file has a corresponding import page or that the page already exists in the wiki
                        Title title = Title.FromRelativePath(_relToTitle, importXml["path"].Contents);
                        XUri pageUri = apiUri.At("pages", title.AsApiParam(), "contents").With("reltopath", _relToTitle.AsPrefixedDbPath());
                        if(!_uris.ContainsKey(pageUri)) {
                            PageBE page = PageBL.GetPageByTitle(title);
                            if((null == page) && (0 >= page.ID)) {
                                throw new PageIdInvalidArgumentException();
                            }
                            _uris[pageUri] = null;
                        }

                        // Generate the request to import the file
                        XUri fileUri = apiUri.At("pages", title.AsApiParam(), "files", Title.AsApiParam(importXml["filename"].Contents));
                        _uris[fileUri] = null;
                        fileUri = fileUri.With("redirects", "0");
                        AddRequest("PUT", fileUri, importXml["@dataid"].AsText, importXml["contents/@type"].AsText);
                    } else if(importXml.HasName("property")) {
                        if(importXml["name"].IsEmpty) {
                            throw new SiteImportUndefinedNameInvalidArgumentException("name");
                        }

                        // Ensure that that the property has a corresponding import page/file 
                        Title title = Title.FromRelativePath(_relToTitle, importXml["path"].Contents);
                        XUri uri = apiUri.At("pages", title.AsApiParam());
                        if(importXml["filename"].IsEmpty) {
                            if(!_uris.ContainsKey(uri.At("contents").With("reltopath", _relToTitle.AsPrefixedDbPath()))) {
                                throw new PageIdInvalidArgumentException();
                            }
                        } else {
                            uri = uri.At("files", Title.AsApiParam(importXml["filename"].AsText));
                            if(!_uris.ContainsKey(uri)) {
                                throw new AttachmentFilenameInvalidArgumentException();
                            }
                        }

                        // Generate the request to import the property
                        uri = uri.At("properties", Title.AsApiParam(importXml["name"].Contents).Substring(1));
                        _uris[uri] = null;
                        uri = uri.With("abort", "never")
                                 .With("redirects", "0");
                        AddRequest("PUT", uri, importXml["@dataid"].AsText, importXml["contents/@type"].AsText);
                    } else {
                        throw new DreamResponseException(DreamMessage.NotImplemented(importXml.Name));
                    }
                } catch(ResourcedMindTouchException e) {
                    ErrorImport(e.Message, (int)e.Status, importXml);
                } catch(DreamAbortException e) {
                    ErrorImport(e.Message, (int)e.Response.Status, importXml);
                } catch(Exception e) {
                    ErrorImport(e.Message, (int)DreamStatus.InternalError, importXml);
                }
            }
        }

        private void AddRequest(string method, XUri href, string data, string type) {
            _requestDoc.Start("request")
                .Attr("method", method)
                .Attr("href", href.ToString())
                .Attr("dataid", data)
                .Attr("type", type)
            .End();
        }

        private void AddBodyRequest(string method, XUri href, XDoc body, string type) {
            _requestDoc.Start("request")
                .Attr("method", method)
                .Attr("href", href.ToString())
                .Attr("type", type)
                .Start("body")
                    .Attr("type", "xml")
                    .Add(body)
                .End()
            .End();
        }

        private void ErrorImport(string reason, int status, XDoc doc) {
            _requestDoc.Start("warning").Attr("reason", reason).Attr("status", status).Add(doc).End();
        }

        public XDoc ToDocument() {
            return _requestDoc;
        }
    }
}

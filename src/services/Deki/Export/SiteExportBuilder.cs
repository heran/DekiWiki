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

using MindTouch.Deki.Data;
using MindTouch.Deki.Exceptions;
using MindTouch.Deki.Logic;
using MindTouch.Dream;
using MindTouch.Xml;

namespace MindTouch.Deki.Export {

    [Flags]
    public enum ExportExcludeType {
        NONE = 0x0000,
        FILES = 0x0001,
        PROPS = 0x0002,
        TAGS = 0x0004,
        TALK = 0x0008,
        ALL = FILES | PROPS | TAGS | TALK
    };

    internal class SiteExportBuilder {

        //--- Fields ---
        Dictionary<XUri, object> _uris;
        uint _id;
        XDoc _requestDoc;
        XDoc _manifestDoc;
        Title _relToTitle;

        //--- Constructors ---
        public SiteExportBuilder(Title relToTitle) {
            _uris = new Dictionary<XUri, object>();
            _id = 0;
            _requestDoc = new XDoc("requests");
            _manifestDoc = new XDoc("manifest");
            _relToTitle = relToTitle;
        }

        //--- Methods ---
        public void Append(XDoc exportDoc) {
            _manifestDoc.Attr("date.created", DateTime.UtcNow).Attr("preserve-local", false);
            foreach(XDoc exportXml in exportDoc.Elements) {
                // Retrieve the current page
                try {
                    if(exportXml.HasName("page")) {

                        // Generate the manifest and request needed to export the page
                        PageBE page = null;
                        if(!exportXml["@id"].IsEmpty) {
                            uint pageId = DbUtils.Convert.To<uint>(exportXml["@id"].Contents, 0);
                            if(0 < pageId) {
                                page = PageBL.GetPageById(pageId);
                            }
                        } else if(!exportXml["@path"].IsEmpty) {
                            page = PageBL.GetPageByTitle(Title.FromPrefixedDbPath(exportXml["@path"].Contents, null));
                        }
                        if((null == page) || (0 == page.ID)) {
                            throw new PageNotFoundException();
                        }

                        // Check whether to exclude subpages, files, and/or properties
                        bool recursive = exportXml["@recursive"].AsBool ?? false;
                        ExportExcludeType exclude = ExportExcludeType.NONE;
                        if(!exportXml["@exclude"].IsEmpty) {
                            exclude = SysUtil.ChangeType<ExportExcludeType>(exportXml["@exclude"].Contents);
                        }

                        // Export the page
                        PageExport(recursive, exclude, page);

                    } else if(exportXml.HasName("file")) {

                        // Generate the manifest and request needed to export the file
                        ResourceBE file = null;
                        if(!exportXml["@id"].IsEmpty) {
                            uint fileId = DbUtils.Convert.To<uint>(exportXml["@id"].Contents, 0);
                            if(0 < fileId) {
                                uint resourceId = ResourceMapBL.GetResourceIdByFileId(fileId) ?? 0;
                                if(resourceId > 0) {
                                    file = ResourceBL.Instance.GetResource(resourceId);
                                }
                            }
                        }
                        if(null == file) {
                            throw new AttachmentNotFoundException();
                        }

                        // Check whether to exclude properties
                        ExportExcludeType exclude = ExportExcludeType.NONE;
                        if(!exportXml["@exclude"].IsEmpty) {
                            exclude = SysUtil.ChangeType<ExportExcludeType>(exportXml["@exclude"].Contents);
                        }

                        // Perform the file export
                        PageBE page = PageBL.GetPageById(file.ParentPageId.Value);
                        page = PageBL.AuthorizePage(DekiContext.Current.User, Permissions.READ, page, false);
                        AttachmentExport(exclude, page, file);
                    } else {
                        throw new DreamResponseException(DreamMessage.NotImplemented(exportXml.Name));
                    }
                } catch(ResourcedMindTouchException e) {
                    AddError(e.Message, (int)e.Status, exportXml);
                } catch(DreamAbortException e) {
                    AddError(e.Message, (int)e.Response.Status, exportXml);
                } catch(Exception e) {
                    AddError(e.Message, (int)DreamStatus.InternalError, exportXml);
                }
            }
        }

        private void PageExport(bool recursive, ExportExcludeType exclude, PageBE page) {

            // Validate the page export
            page = PageBL.AuthorizePage(DekiContext.Current.User, Permissions.READ, page, false);
            if(page.IsRedirect) {
                throw new SiteExportRedirectInvalidOperationException();
            }

            // Export the page
            XUri uri = PageBL.GetUriCanonical(page).At("contents")
                                        .With("mode", "edit")
                                        .With("reltopath", _relToTitle.AsPrefixedDbPath())
                                        .With("format", "xhtml");
            if(!_uris.ContainsKey(uri)) {
                _uris.Add(uri, null);
                var dateModified = page.TimeStamp;
                var lastImport = PropertyBL.Instance.GetPageProperty((uint)page.ID, SiteImportBuilder.LAST_IMPORT);
                if(lastImport != null) {
                    var content = lastImport.Content;
                    var importDoc = XDocFactory.From(content.ToStream(), content.MimeType);
                    if(importDoc["etag"].AsText.EqualsInvariant(page.Etag)) {
                        dateModified = importDoc["date.modified"].AsDate ?? DateTime.MinValue;
                    }
                }
                var manifestDoc = new XDoc("page").Elem("title", page.Title.DisplayName)
                                                   .Elem("path", page.Title.AsRelativePath(_relToTitle))
                                                   .Elem("language", page.Language)
                                                   .Elem("etag", page.Etag)
                                                   .Elem("date.modified", dateModified.ToSafeUniversalTime())
                                                   .Start("contents").Attr("type", page.ContentType).End();
                Add(uri, manifestDoc);
            }

            // Export page tags (if not excluded)
            if(ExportExcludeType.NONE == (ExportExcludeType.TAGS & exclude)) {
                XUri tagUri = PageBL.GetUriCanonical(page).At("tags");
                if(!_uris.ContainsKey(tagUri)) {
                    _uris.Add(tagUri, null);
                    XDoc manifestDoc = new XDoc("tags").Elem("path", page.Title.AsRelativePath(_relToTitle));
                    Add(tagUri, manifestDoc);
                }
            }

            // Export page properties (if not excluded)
            if(ExportExcludeType.NONE == (ExportExcludeType.PROPS & exclude)) {
                IList<ResourceBE> properties = PropertyBL.Instance.GetPageProperties(page.ID);
                foreach(ResourceBE property in properties) {
                    if(property.Name.EqualsInvariant(SiteImportBuilder.LAST_IMPORT)) {
                        continue;
                    }
                    PropertyExport(page, null, property);
                }
            }

            // Export page files (if not excluded)
            if(ExportExcludeType.NONE == (ExportExcludeType.FILES & exclude)) {
                IList<ResourceBE> files = AttachmentBL.Instance.GetPageAttachments(page.ID);
                foreach(ResourceBE file in files) {
                    AttachmentExport(exclude, page, file);
                }
            }

            // Export talk page (if not excluded)
            if((ExportExcludeType.NONE == (ExportExcludeType.TALK & exclude)) && (!page.Title.IsTalk)) {
                PageBE talkPage = PageBL.GetPageByTitle(page.Title.AsTalk());
                if((null != talkPage) && (0 < talkPage.ID)) {
                    PageExport(false, exclude, talkPage);
                }
            }

            // Export subpages (if not excluded)
            if(recursive) {
                ICollection<PageBE> children = PageBL.GetChildren(page, true);
                children = PermissionsBL.FilterDisallowed(DekiContext.Current.User, PageBL.GetChildren(page, true), false, Permissions.READ);
                if(children != null) {
                    foreach(PageBE child in children) {
                        PageExport(recursive, exclude, child);
                    }
                }
            }
        }

        private void AttachmentExport(ExportExcludeType exclude, PageBE page, ResourceBE file) {

            // Export the file
            if(!_uris.ContainsKey(AttachmentBL.Instance.GetUriContent(file))) {
                _uris.Add(AttachmentBL.Instance.GetUriContent(file), null);
                XDoc manifestDoc = new XDoc("file").Elem("filename", file.Name)
                                                   .Elem("path", page.Title.AsRelativePath(_relToTitle))
                                                   .Start("contents").Attr("type", file.MimeType.ToString()).End();
                Add(AttachmentBL.Instance.GetUriContent(file), manifestDoc);
            }

            // Export the file properties (if not excluded)
            if(ExportExcludeType.NONE == (ExportExcludeType.PROPS & exclude)) {
                IList<ResourceBE> properties = PropertyBL.Instance.GetAttachmentProperties(file.ResourceId);
                foreach(ResourceBE property in properties) {
                    PropertyExport(page, file, property);
                }
            }
        }

        private void PropertyExport(PageBE page, ResourceBE file, ResourceBE property) {

            // Export the property
            XUri propertyUri = null;
            string filename = null;
            if(null != file) {
                propertyUri = property.PropertyContentUri(AttachmentBL.Instance.GetUri(file));
                filename = file.Name;
            } else {
                propertyUri = property.PropertyContentUri(PageBL.GetUriCanonical(page));
            }
            if(!_uris.ContainsKey(propertyUri)) {
                _uris.Add(propertyUri, null);
                XDoc manifestDoc = new XDoc("property").Elem("name", property.Name)
                                                       .Elem("filename", filename)
                                                       .Elem("path", page.Title.AsRelativePath(_relToTitle))
                                                       .Start("contents").Attr("type", property.MimeType.ToString()).End();
                Add(propertyUri, manifestDoc);
            }
        }

        private void Add(XUri href, XDoc manifestXml) {
            string data = Convert.ToString(_id++);
            AddRequest("GET", href, data);
            manifestXml.Attr("dataid", data);
            _manifestDoc.Add(manifestXml);
        }

        private void AddRequest(string method, XUri href, string data) {
            _requestDoc.Start("request")
                       .Attr("method", method)
                       .Attr("href", href.ToString())
                       .Attr("dataid", data)
                       .End();
        }

        private void AddError(string reason, int status, XDoc doc) {
            _requestDoc.Start("warning").Attr("reason", reason).Attr("status", status).Add(doc).End();
        }

        public XDoc ToDocument() {
            return new XDoc("export").Add(_requestDoc).Add(_manifestDoc);
        }
    }
}


/*
 * MindTouch MediaWiki Converter
 * Copyright (C) 2006-2008 MindTouch Inc.
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
using System.Collections.Generic;
using System.Xml;
using System.Xml.Xsl;

using MindTouch.Data;
using MindTouch.Deki;
using MindTouch.Deki.Data;
using MindTouch.Deki.Data.MySql;
using MindTouch.Dream;
using MindTouch.Xml;

namespace MindTouch.Tools {

    class MediaWikiConverterContext {
        private XDoc _config;
        private XslCompiledTransform _converterXslt;
        private uint? _mergeUserId;
        private string _logPath;
        private string _templatePath;
        private char _pageSeparator;
        private Plug _converterUri;
        private DataCatalog _mwCatalog;
        private Site[] _mwSites;
        private String _userPrefix;


        internal MediaWikiConverterContext(XDoc config) {
            _config = config;
        }

        public static MediaWikiConverterContext Current {
            get {
                MediaWikiConverterContext dc = CurrentOrNull;
                if (dc == null) {
                    throw new InvalidOperationException("DekiContext.Current is not set");
                }
                return dc;
            }
        }

        public static MediaWikiConverterContext CurrentOrNull {
            get {
                return DreamContext.Current.GetState<MediaWikiConverterContext>();
            }
        }

        public XslCompiledTransform MWConverterXslt {
            get {
                if (null == _converterXslt) {
                    XDoc doc = Plug.New("resource://mindtouch.deki.mwconverter/MindTouch.Tools.mediawikixml2dekixml.xslt").With(DreamOutParam.TYPE, MimeType.XML.FullType).Get().ToDocument();
                    _converterXslt = new XslCompiledTransform();
                    _converterXslt.Load(new XmlNodeReader(doc.AsXmlNode), null, null);
                }
                return _converterXslt;
            }
        }

        public bool Merge {
            get {
                return MergeUserId > 0;
            }
        }

        public uint MergeUserId {
            get {
                if (null == _mergeUserId) {
                    try {
                        _mergeUserId = _config["mediawiki/merge-userid"].AsUInt;
                    } catch {
                        _mergeUserId = 0;
                    }
                
                }
                return _mergeUserId ?? 0;
            }
        }

        public bool AttributeViaPageRevComment {
            get {
                return _config["mediawiki/attribute-via-page-rev-comment"].AsBool ?? false;
            }
        }

        public string AttributeViaPageRevCommentPattern {
            get {
                string s = _config["mediawiki/attribute-via-page-rev-comment-pattern"].AsText;
                return string.IsNullOrEmpty(s) ? "{0}; Edited by {1}" : PhpUtil.ConvertToFormatString(s);
            }
        }

        public string LogPath {
            get {
                if (null == _logPath) {
                    _logPath = _config["mediawiki/log"].Contents;
                }
                return _logPath;
            }
        }

        public bool LoggingEnabled {
            get {
                return LogPath != String.Empty;
            }
        }

        public string MWTemplatePath {
            get {
                if (null == _templatePath) {
                    _templatePath = _config["mediawiki/template-path"].Contents;
                }
                return _templatePath;
            }
        }

        public char MWPageSeparator {
            get {
                if (null == _converterUri) {
                    XDoc pageSeparatorDoc = _config["mediawiki/page.separator"];
                    if (!pageSeparatorDoc.IsEmpty && !String.IsNullOrEmpty(pageSeparatorDoc.Contents)) {
                        _pageSeparator = pageSeparatorDoc.Contents[0];
                    }
                }
                return _pageSeparator;
            }
        }

        public Plug MWConverterUri {
            get {
                if (null == _converterUri) {
                    XDoc uriDoc = _config["mediawiki/uri.converter"];
                    if (!uriDoc.IsEmpty && !String.IsNullOrEmpty(uriDoc.Contents)) {
                        _converterUri = Plug.New(uriDoc.Contents);
                    } 
                }
                return _converterUri;
            }
        }

        public DataCatalog MWCatalog {
            get {
                if (null == _mwCatalog) {
                    _mwCatalog = new DataCatalog(new DataFactory(MySql.Data.MySqlClient.MySqlClientFactory.Instance, "?"), _config["mediawiki"]);
                }
                return _mwCatalog;
            }
        }

        public DataCatalog DWCatalog {
            get {
                MySqlDekiDataSession session = DbUtils.CurrentSession as MySqlDekiDataSession;

                // TODO (brigettek):  MediaWiki conversion should not require a MySql backend
                if (null == session) {
                    throw new Exception("MediaWiki Conversion requires a MySql backend");
                }
                return session.Catalog;
            }
        }

        public Site[] MWSites {
            get {
                if (null == _mwSites) {
                    List<Site> sites = new List<Site>();
                    foreach (XDoc siteDoc in _config["mediawiki/sites/site"]) {
                        Site site = new Site();
                        site.DbPrefix = siteDoc["db-prefix"].Contents;
                        site.Language = siteDoc["language"].Contents;
                        site.MWRootPage = siteDoc["mwrootpage"].Contents;
                        site.DWRootPage = siteDoc["dwrootpage"].Contents;
                        site.ImageDir = siteDoc["imagedir"].Contents;
                        site.Name = siteDoc["name"].Contents;
                        sites.Add(site);
                    }
                    _mwSites = sites.ToArray();
                }
                return _mwSites;
            }
        }

        public Site GetMWSite(string language) {
            foreach (Site currentSite in MWSites) {
                if (language == currentSite.Language) {
                    return currentSite;
                }
            }
            return Site.Empty;
        }

        public string MWUserPrefix {
            get {
                if (null == _userPrefix) {
                    _userPrefix = _config["mediawiki/db-userprefix"].Contents;
                }
                return _userPrefix;
            }
        }
    }
}

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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;

using log4net;
using MindTouch.Deki.Script;
using MindTouch.Deki.Script.Expr;
using MindTouch.Deki.Script.Runtime.Library;
using MindTouch.Dream;
using MindTouch.IO;
using MindTouch.Tasking;
using MindTouch.Xml;

namespace MindTouch.Deki.Services.Extension {
    using Yield = IEnumerator<IYield>;

    [DreamService("MindTouch Web Cache Extension", "Copyright (c) 2010 MindTouch Inc.",
        Info = "http://developer.mindtouch.com/App_Catalog/WebCache",
        SID = new string[] {
            "sid://mindtouch.com/2009/02/webcache",
            "sid://mindtouch.com/2009/02/extension/webcache"
        }
    )]
    [DreamServiceConfig("max-size", "long?", "Maximum file size (in bytes) to cache (default: 512KB)")]
    [DreamServiceConfig("memory-cache-time", "double?", "Seconds to keep cache in memory (in addition to disk) after access (default: 60)")]
    [DreamServiceBlueprint("deki/service-type", "extension")]
    [DekiExtLibrary(
        Label = "Web Cache",
        Namespace = "webcache",
        Description = "This extension contains functions caching documents fetched from the web using a disk-backed store."
    )]
    public class WebCacheService : DekiExtService {

        //--- Constants ---
        private const double DEFAULT_MEMORY_CACHE_TIME = 60;
        private const double DEFAULT_CACHE_TTL = 5 * 60;
        private const long DEFAULT_TEXT_LIMIT = 512 * 1024;
        private const string CACHE_DATA = "cache-data";
        private const string CACHE_INFO = "cache-info";
        private static readonly TimeSpan DEFAULT_WEB_TIMEOUT = TimeSpan.FromSeconds(60);
        private static double _memoryCacheTime;

        //--- Types ---
        public class CacheEntry {

            //--- Fields ---
            public readonly string Id;
            public readonly Guid Guid;
            public string Cache;
            public readonly DateTime Expires;
            private readonly TaskTimer _memoryExpire;

            //--- Constructors ---
            public CacheEntry(string id, double? ttl) : this() {
                Id = id;
                Guid = Guid.NewGuid();
                Expires = DateTime.UtcNow.Add(TimeSpan.FromSeconds(ttl ?? DEFAULT_CACHE_TTL));
                ResetMemoryExpiration();
            }

            public CacheEntry(string id, Guid guid, DateTime expires) : this() {
                Id = id;
                Guid = guid;
                Expires = expires;
            }

            private CacheEntry() {
                _memoryExpire = new TaskTimer(delegate(TaskTimer tt) {
                    _log.DebugFormat("flushing memory cache for '{0}'", Id);
                    Cache = null;
                }, null);
            }

            //--- Methods ---
            public void ResetMemoryExpiration() {
                _memoryExpire.Change(TimeSpan.FromSeconds(_memoryCacheTime), TaskEnv.Clone());
            }
        }

        //--- Class Fields ---
        private static readonly ILog _log = LogUtils.CreateLog();

        //--- Fields ---
        private readonly Dictionary<string, CacheEntry> _cacheLookup = new Dictionary<string, CacheEntry>(StringComparer.OrdinalIgnoreCase);
        private Sgml.SgmlDtd _htmlEntitiesDtd;
        private long _insertTextLimit;

        //--- Functions ---
        [DekiExtFunction("clear", "Remove a uri from the cache")]
        public object Clear(
            [DekiExtParam("source uri")] string source
        ) {
            lock(_cacheLookup) {
                CacheEntry entry;
                if(!_cacheLookup.TryGetValue(source, out entry)) {
                    return null;
                }
                _log.DebugFormat("Removed from cache: '{0}'", source);
                _cacheLookup.Remove(source);
                DeleteCacheEntry(entry.Guid);
            }
            return null;
        }

        [DekiExtFunction("text", "Get a text value from a web-service.")]
        public string WebText(
            [DekiExtParam("source text or source uri (default: none)", true)] string source,
            [DekiExtParam("xpath to value (default: none)", true)] string xpath,
            [DekiExtParam("namespaces (default: none)", true)] Hashtable namespaces,
            [DekiExtParam("capture enclosing XML element (default: false)", true)] bool? xml,
            [DekiExtParam("caching duration in seconds (range: 300+; default: 300)", true)] double? ttl,
            [DekiExtParam("return nil if source could not be loaded (default: text with error message)", true)] bool? nilIfMissing
        ) {

            // NOTE (steveb): the following cases need to be covered
            // * source is a string and no xpath given -> return source as is
            // * source is a string and xpath given -> convert source to an XML document and apply xpath
            // * source is a uri pointing to text document and xpath given -> fetch source and convert to string (ignore xpath)
            // * source is a uri pointing to xml document and xpath given -> fetch source, convert to XML document, and apply xpath
            // * source is a uri pointing to text document and no xpath given -> fetch source and convert to string
            // * source is a uri pointing to xml document and no xpath given -> fetch source and convert to string
            source = source ?? string.Empty;
            XUri uri = XUri.TryParse(source);
            if(uri == null) {
                if(xpath == null) {

                    // source is a string and no xpath given -> return source as is
                    return source;
                } else {

                    // source is a string and xpath given -> convert sourcwe to an XML document and apply xpath
                    XDoc doc = XDocFactory.From(source, MimeType.XML);
                    if((doc == null) || doc.IsEmpty) {
                        return "(source is not an xml document)";
                    }
                    return AtXPath(doc, xpath, namespaces, xml ?? false);
                }
            } else {

                // we need to fetch an online document
                string response = CachedWebGet(uri, ttl, nilIfMissing);
                if((response == null) || (xpath == null)) {

                    // source is a uri pointing to text document and no xpath given -> fetch source and convert to string
                    // source is a uri pointing to xml document and no xpath given -> fetch source and convert to string
                    return response;
                } else {
                    XDoc doc = XDocFactory.From(response, MimeType.XML);
                    if(doc.IsEmpty) {
                        doc = XDocFactory.From(response, MimeType.HTML);
                    }
                    if(doc.IsEmpty) {

                        // * source is a uri pointing to text document and xpath given -> fetch source and convert to string (ignore xpath)
                        return response;
                    }

                    // * source is a uri pointing to xml document and xpath given -> fetch source, convert to XML document, and apply xpath
                    return AtXPath(doc, xpath, namespaces, xml ?? false);
                }
            }
        }

        [DekiExtFunction("html", "Convert text to HTML.  The text value can optionally be retrieved from a web-service.")]
        public XDoc WebHtml(
            [DekiExtParam("HTML source text or source uri (default: none)", true)] string source,
            [DekiExtParam("xpath to value (default: none)", true)] string xpath,
            [DekiExtParam("namespaces (default: none)", true)] Hashtable namespaces,
            [DekiExtParam("caching duration in seconds (range: 300+; default: 300)", true)] double? ttl,
            [DekiExtParam("return nil if source could not be loaded (default: text with error message)", true)] bool? nilIfMissing
        ) {
            string text = WebText(source, xpath, namespaces, true, ttl, nilIfMissing);
            if(text == null) {
                return null;
            }

            // convert text to html
            XDoc result = XDoc.Empty;
            using(TextReader reader = new StringReader("<html><body>" + text + "</body></html>")) {

                // NOTE (steveb): we create the sgml reader explicitly since we don't want a DTD to be associated with it; the DTD would force a potentially unwanted HTML structure

                // check if HTML entities DTD has already been loaded
                if(_htmlEntitiesDtd == null) {
                    using(StreamReader dtdReader = new StreamReader(Plug.New("resource://mindtouch.deki.script/MindTouch.Deki.Script.HtmlEntities.dtd").Get().AsStream())) {
                        _htmlEntitiesDtd = Sgml.SgmlDtd.Parse(null, "HTML", dtdReader, null, null, XDoc.XmlNameTable);
                    }
                }

                Sgml.SgmlReader sgmlReader = new Sgml.SgmlReader(XDoc.XmlNameTable);
                sgmlReader.Dtd = _htmlEntitiesDtd;
                sgmlReader.DocType = "HTML";
                sgmlReader.WhitespaceHandling = WhitespaceHandling.All;
                sgmlReader.CaseFolding = Sgml.CaseFolding.ToLower;
                sgmlReader.InputStream = reader;
                try {
                    XmlDocument doc = new XmlDocument(XDoc.XmlNameTable);
                    doc.PreserveWhitespace = true;
                    doc.XmlResolver = null;
                    doc.Load(sgmlReader);

                    // check if a valid document was created
                    if(doc.DocumentElement != null) {
                        result = new XDoc(doc);
                    }
                } catch(Exception) {

                    // swallow parsing exceptions
                }
            }
            return DekiScriptLibrary.CleanseHtmlDocument(result);
        }

        [DekiExtFunction("list", "Get list of values from an XML document or web-service.")]
        public ArrayList WebList(
            [DekiExtParam("XML source text or source uri")] string source,
            [DekiExtParam("xpath to list of values")] string xpath,
            [DekiExtParam("namespaces (default: none)", true)] Hashtable namespaces,
            [DekiExtParam("capture enclosing XML element (default: false)", true)] bool? xml,
            [DekiExtParam("caching duration in seconds (range: 300+; default: 300)", true)] double? ttl,
            [DekiExtParam("return nil if source could not be loaded (default: text with error message)", true)] bool? nilIfMissing
        ) {
            XUri uri = XUri.TryParse(source);
            ArrayList result;
            if(uri == null) {

                // source is a string -> convert sourcwe to an XML document and apply xpath
                XDoc doc = XDocFactory.From(source, MimeType.XML);
                if((doc == null) || doc.IsEmpty) {
                    result = new ArrayList();
                } else {
                    result = AtXPathList(doc, xpath, namespaces, xml ?? false);
                }
            } else {

                // we need to fetch an online document
                string response = CachedWebGet(uri, ttl, nilIfMissing);
                if(response == null) {
                    return null;
                }
                XDoc doc = XDocFactory.From(response, MimeType.XML);
                if(doc.IsEmpty) {
                    doc = XDocFactory.From(response, MimeType.HTML);
                }
                if(doc.IsEmpty) {

                    // * source is a uri pointing to text document -> fetch source and convert to string (ignore xpath)
                    result = new ArrayList();
                    result.Add(response.ToString());
                } else {

                    // * source is a uri pointing to xml document -> fetch source, convert to XML document, and apply xpath
                    result = AtXPathList(doc, xpath, namespaces, xml ?? false);
                }
            }
            return result;
        }

        [DekiExtFunction("xml", "Get an XML document from a web-service.")]
        public XDoc WebXml(
           [DekiExtParam("XML source text or source uri")] string source,
           [DekiExtParam("xpath to value (default: none)", true)] string xpath,
           [DekiExtParam("namespaces (default: none)300+", true)] Hashtable namespaces,
           [DekiExtParam("caching duration in seconds (range: 300+; default: 300)", true)] double? ttl,
           [DekiExtParam("return nil if source could not be loaded (default: text with error message)", true)] bool? nilIfMissing
        ) {
            string text = WebText(source, xpath, namespaces, true, ttl, nilIfMissing);
            if(text == null) {
                return null;
            }
            XDoc result = XDocFactory.From(text, MimeType.XML);
            if(result.IsEmpty) {

                // try again assuming the input is HTML
                if(text.TrimStart().StartsWith("<")) {
                    result = XDocFactory.From(text, MimeType.HTML);
                } else {

                    // wrap response into a valid HTML document
                    result = new XDoc("html").Elem("body", text);
                }
            }
            if(result.HasName("html")) {
                result = DekiScriptLibrary.CleanseHtmlDocument(result);
            }
            return result;
        }

        [DekiExtFunction("json", "Get a JSON value from a web-service.")]
        public object WebJson(
           [DekiExtParam("source text or source uri (default: none)", true)] string source,
           [DekiExtParam("caching duration in seconds (range: 300+; default: 300)", true)] double? ttl,
           [DekiExtParam("return nil if source could not be loaded (default: text with error message)", true)] bool? nilIfMissing,
           DekiScriptRuntime runtime
        ) {
            source = source ?? string.Empty;
            XUri uri = XUri.TryParse(source);
            if(uri == null) {
                return DekiScriptLibrary.JsonParse(source, runtime);
            }

            // we need to fetch an online document
            string response = CachedWebGet(uri, ttl, nilIfMissing);
            return DekiScriptLibrary.JsonParse(response, runtime);
        }

        [DekiExtFunction("fetch", "Fetch a document from the cache.")]
        public object Fetch(
           [DekiExtParam("document id")] string id
       ) {

            // fetch response from cache
            CacheEntry result;
            lock(_cacheLookup) {
                _cacheLookup.TryGetValue(id, out result);
            }

            // check if we have a cached entry
            XDoc document = null;
            if(result != null) {
                _log.DebugFormat("cache hit for '{0}'", result.Id);
                result.ResetMemoryExpiration();
                if(result.Cache != null) {
                    _log.DebugFormat("cache data in memory '{0}'", result.Id);
                    document = XDocFactory.From(result.Cache, MimeType.XML);
                } else {

                    // we have the result on disk, so let's fetch it
                    DreamMessage msg = Storage.At(CACHE_DATA, result.Guid + ".bin").GetAsync().Wait();
                    if(msg.IsSuccessful) {
                        _log.DebugFormat("cache data pulled from disk");
                        result.Cache = Encoding.UTF8.GetString(msg.AsBytes());
                        document = XDocFactory.From(result.Cache, MimeType.XML);
                    } else {
                        _log.DebugFormat("unable to fetch cache data from disk: {0}", msg.Status);
                    }
                }
            }

            // check if we have a document to convert
            if(document != null) {
                try {
                    DekiScriptList list = (DekiScriptList)DekiScriptLiteral.FromXml(document);
                    return list[0];
                } catch {

                    // the cached entry is corrupted, remove it
                    Clear(id);
                }
            }
            return null;
        }

        [DekiExtFunction("store", "Store a document in the cache.")]
        public object Store(
           [DekiExtParam("document id")] string id,
           [DekiExtParam("document to cache", true)] object document,
           [DekiExtParam("caching duration in seconds (range: 300+; default: 300)", true)] double? ttl
        ) {
            if(document == null) {
                return Clear(id);
            }

            // fetch entry from cache
            CacheEntry result;
            bool isNew = true;
            lock(_cacheLookup) {
                _cacheLookup.TryGetValue(id, out result);
            }

            // check if we have a cached entry
            if(result != null) {
                _log.DebugFormat("cache hit for '{0}'", result.Id);
                isNew = false;
                result.ResetMemoryExpiration();
            } else {
                _log.DebugFormat("new cache item for '{0}'", id);
                result = new CacheEntry(id, ttl);
            }

            // update cache with document
            DekiScriptLiteral literal = DekiScriptLiteral.FromNativeValue(document);
            XDoc xml = new DekiScriptList().Add(literal).ToXml();
            result.Cache = xml.ToString();

            // start timer to clean-up cached result
            if(result.Cache != null) {
                XDoc infoDoc = new XDoc("cache-entry")
                    .Elem("guid", result.Guid)
                    .Elem("id", result.Id)
                    .Elem("expires", result.Expires);
                lock(result) {
                    Storage.At(CACHE_DATA, result.Guid + ".bin").PutAsync(new DreamMessage(DreamStatus.Ok, null, MimeType.BINARY, Encoding.UTF8.GetBytes(result.Cache))).Wait();
                    Storage.At(CACHE_INFO, result.Guid + ".xml").PutAsync(infoDoc).Wait();
                }
                if(isNew) {
                    lock(_cacheLookup) {
                        _cacheLookup[id] = result;
                    }

                    // this timer removes the cache entry from disk 
                    SetupCacheTimer(result);
                }
            }
            return document;
        }

        //--- Methods ---
        protected override Yield Start(XDoc config, Result result) {
            yield return Coroutine.Invoke(base.Start, config, new Result());

            // set up defaults
            _insertTextLimit = config["max-size"].AsLong ?? DEFAULT_TEXT_LIMIT;
            _memoryCacheTime = config["memory-cache-time"].AsDouble ?? DEFAULT_MEMORY_CACHE_TIME;
            _log.DebugFormat("max-size: {0}, memory-cache-time: {1}", _insertTextLimit, _memoryCacheTime);

            // load current cache state
            Async.Fork(() => Coroutine.Invoke(RefreshCache, new Result(TimeSpan.MaxValue)), TaskEnv.Clone(), null);
            result.Return();
        }

        protected override Yield Stop(Result result) {

            _cacheLookup.Clear();
            yield return Coroutine.Invoke(base.Stop, new Result());
            result.Return();
        }

        private Yield RefreshCache(Result result) {
            XDoc cacheCatalog = null;
            yield return Storage.At(CACHE_INFO).WithTrailingSlash().GetAsync().Set(v => cacheCatalog = v.ToDocument());
            int itemCount = 0;
            foreach(XDoc file in cacheCatalog["file/name"]) {
                string filename = file.AsText;

                // request cach-info file
                Result<DreamMessage> cacheInfo;
                yield return cacheInfo = Storage.At(CACHE_INFO, filename).GetAsync();
                XDoc doc = null;
                Guid guid = Guid.Empty;
                try {
                    doc = cacheInfo.Value.ToDocument();
                    guid = new Guid(doc["guid"].AsText);
                    string id = doc["id"].AsText;

                    // check if cache entry has already expired
                    DateTime expires = doc["expires"].AsDate.Value;
                    CacheEntry entry = new CacheEntry(id, guid, expires);
                    if(entry.Expires < DateTime.UtcNow) {
                        _log.DebugFormat("removing stale cache item '{0}' at startup", id);
                        DeleteCacheEntry(guid);
                        continue;
                    }

                    // try adding the cache-entry; this might fail if the entry has already been re-added
                    lock(_cacheLookup) {
                        _cacheLookup[id] = entry;
                        ++itemCount;
                        SetupCacheTimer(entry);
                    }
                } catch(Exception e) {
                    _log.Error(string.Format("Bad cacheinfo doc '{0}': {1}", filename, doc), e);
                    if(guid != Guid.Empty) {
                        DeleteCacheEntry(guid);
                    }
                }
            }
            _log.DebugFormat("loaded {0} cache items", itemCount);
            result.Return();
        }

        private string CachedWebGet(XUri uri, double? ttl, bool? nilIfMissing) {
            string id = uri.ToString();

            // fetch message from cache or from the web
            CacheEntry result;
            bool isNew = true;
            lock(_cacheLookup) {
                _cacheLookup.TryGetValue(id, out result);
            }

            // check if we have a cached entry
            if(result != null) {
                _log.DebugFormat("cache hit for '{0}'", result.Id);
                isNew = false;
                result.ResetMemoryExpiration();
                if(result.Cache != null) {
                    _log.DebugFormat("cache data in memory '{0}'", result.Id);
                    return result.Cache;
                }

                // we have the result on disk, so let's fetch it
                DreamMessage msg = Storage.At(CACHE_DATA, result.Guid + ".bin").GetAsync().Wait();
                if(msg.IsSuccessful) {
                    _log.DebugFormat("cache data pulled from disk");
                    result.Cache = Encoding.UTF8.GetString(msg.AsBytes());
                    return result.Cache;
                }
                _log.DebugFormat("unable to fetch cache data from disk: {0}", msg.Status);
            } else {
                _log.DebugFormat("new cache item for '{0}'", id);
                result = new CacheEntry(id, ttl);
            }

            // do the web request
            Result<DreamMessage> response = new Result<DreamMessage>();
            Plug.New(uri).WithTimeout(DEFAULT_WEB_TIMEOUT).InvokeEx("GET", DreamMessage.Ok(), response);
            DreamMessage message = response.Wait();
            try {

                // check message status
                if(!message.IsSuccessful) {
                    if(nilIfMissing.GetValueOrDefault()) {
                        return null;
                    }
                    return message.Status == DreamStatus.UnableToConnect
                        ? string.Format("(unable to fetch text document from uri [status: {0} ({1}), message: \"{2}\"])", (int)message.Status, message.Status, message.ToDocument()["message"].AsText)
                        : string.Format("(unable to fetch text document from uri [status: {0} ({1})])", (int)message.Status, message.Status);
                }

                // check message size
                Result resMemorize = message.Memorize(_insertTextLimit, new Result()).Block();
                if(resMemorize.HasException) {
                    return nilIfMissing.GetValueOrDefault() ? null : "(text document is too large)";
                }

                // detect encoding and decode response
                var stream = message.ToStream();
                var encoding = stream.DetectEncoding() ?? message.ContentType.CharSet;
                result.Cache = encoding.GetString(stream.ReadBytes(-1));
            } finally {
                message.Close();
            }

            // start timer to clean-up cached result
            if(result.Cache != null) {
                XDoc infoDoc = new XDoc("cache-entry")
                    .Elem("guid", result.Guid)
                    .Elem("id", result.Id)
                    .Elem("expires", result.Expires);
                lock(result) {
                    Storage.At(CACHE_DATA, result.Guid + ".bin").PutAsync(new DreamMessage(DreamStatus.Ok, null, MimeType.BINARY, Encoding.UTF8.GetBytes(result.Cache))).Wait();
                    Storage.At(CACHE_INFO, result.Guid + ".xml").PutAsync(infoDoc).Wait();
                }
                if(isNew) {
                    lock(_cacheLookup) {
                        _cacheLookup[id] = result;
                    }

                    // this timer removes the cache entry from disk 
                    SetupCacheTimer(result);
                }
            }
            return result.Cache;
        }

        private void SetupCacheTimer(CacheEntry cacheEntry) {
            TaskTimer.New(cacheEntry.Expires, timer => {
                var entry = (CacheEntry)timer.State;
                _log.DebugFormat("removing '{0}' from cache", entry.Id);
                lock(entry) {

                    // removing from lookup first, since a false return value on Remove indicates that we shouldn't
                    // try to delete the file from disk
                    if(_cacheLookup.Remove(entry.Id)) {
                        DeleteCacheEntry(entry.Guid);
                    }
                }
            }, cacheEntry, TaskEnv.None);
        }

        private void DeleteCacheEntry(Guid guid) {
            DreamMessage msg = Storage.WithCookieJar(Cookies).At(CACHE_DATA, guid + ".bin").DeleteAsync().Wait();
            if(!msg.IsSuccessful) {
                _log.WarnFormat("Unable to delete cache data for {0}: {1}", guid, msg);
            }
            msg = Storage.WithCookieJar(Cookies).At(CACHE_INFO, guid + ".xml").DeleteAsync().Wait();
            if(!msg.IsSuccessful) {
                _log.WarnFormat("Unable to delete cache info for {0}: {1}", guid, msg);
            }
        }

        private XDoc AtXPathNode(XDoc doc, string xpath, Hashtable namespaces) {
            XDoc result = doc;
            if(namespaces != null) {

                // initialize a namespace manager
                XmlNamespaceManager nsm = new XmlNamespaceManager(SysUtil.NameTable);
                foreach(DictionaryEntry ns in namespaces) {
                    nsm.AddNamespace((string)ns.Key, SysUtil.ChangeType<string>(ns.Value));
                }
                result = doc.AtPath(xpath, nsm);
            } else if(!StringUtil.EqualsInvariant(xpath, ".")) {

                // use default namespace manager
                result = doc[xpath];
            }
            return result;
        }

        private string AtXPath(XDoc doc, string xpath, Hashtable namespaces, bool asXml) {
            XDoc node = AtXPathNode(doc, xpath, namespaces);
            if(asXml && !node.IsEmpty) {
                if(node.AsXmlNode.NodeType == XmlNodeType.Attribute) {
                    return ((XmlAttribute)node.AsXmlNode).OwnerElement.OuterXml;
                } else {
                    return node.AsXmlNode.OuterXml;
                }
            } else {
                return node.AsText;
            }
        }

        private ArrayList AtXPathList(XDoc doc, string xpath, Hashtable namespaces, bool asXml) {
            XDoc node;
            if(namespaces != null) {

                // initialize a namespace manager
                XmlNamespaceManager nsm = new XmlNamespaceManager(SysUtil.NameTable);
                foreach(DictionaryEntry ns in namespaces) {
                    nsm.AddNamespace((string)ns.Key, SysUtil.ChangeType<string>(ns.Value));
                }
                node = doc.AtPath(xpath, nsm);
            } else {

                // use default namespace manager
                node = doc[xpath];
            }

            // iterate over all matches
            ArrayList result = new ArrayList();
            foreach(XDoc item in node) {
                if(asXml) {
                    if(item.AsXmlNode.NodeType == XmlNodeType.Attribute) {
                        result.Add(((XmlAttribute)item.AsXmlNode).OwnerElement.OuterXml);
                    } else {
                        result.Add(item.AsXmlNode.OuterXml);
                    }
                } else {
                    result.Add(item.AsText);
                }
            }
            return result;
        }
    }
}
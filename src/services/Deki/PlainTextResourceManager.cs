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
using System.Globalization;
using System.IO;
using System.Resources;

using MindTouch.Dream;

namespace MindTouch.Deki {

    // A custom Resource Reader which takes a plain text file with key=value pairs on each line
    internal class PlainTextResourceReader : IResourceReader {

        //--- Class Fields ---
        private static readonly log4net.ILog _log = LogUtils.CreateLog();

        //--- Fields ---
        private String _filename;

        //--- Constructors --
        public PlainTextResourceReader(String filename) {
            _filename = filename;
        }

        //--- Methods ---
        public IDictionaryEnumerator GetEnumerator() {
            Dictionary<String, String> resources = new Dictionary<String, String>();
            if(System.IO.File.Exists(_filename)) {
                char[] brackets = new char[] { ']' };
                char[] equals = new char[] { '=' };
                string[] parts;

                // read the file into the hashtable
                using(StreamReader sr = new StreamReader(_filename, System.Text.Encoding.UTF8, true)) {
                    int count = 0;
                    string section = null;
                    for(string line = sr.ReadLine(); line != null; line = sr.ReadLine()) {
                        line = line.TrimStart();
                        ++count;

                        // check if line is a comment
                        if(line.StartsWith(";")) {
                            continue;
                        }

                        // check if line is a new section
                        if(line.StartsWith("[")) {
                            parts = line.Substring(1).Split(brackets, 2);
                            if(!string.IsNullOrEmpty(parts[0])) {
                                section = parts[0].Trim();
                            } else {
                                section = null;
                                _log.WarnMethodCall("missing namespace name", _filename, count);
                            }
                            continue;
                        }

                        // parse the line as key=value 
                        parts = line.Split(equals, 2);
                        if(parts.Length == 2) {
                            if(!string.IsNullOrEmpty(parts[0])) {
                                string key;

                                // check if a section is defined
                                if(section != null) {
                                    key = section + "." + parts[0];
                                } else {
                                    key = parts[0];
                                    _log.WarnMethodCall("missing namespace prefix", _filename, count, parts[0]);
                                }

                                // check if key already exists
                                if(resources.ContainsKey(key)) {
                                    _log.WarnMethodCall("duplicate key", _filename, count, key);
                                }
                                resources[key] = PhpUtil.ConvertToFormatString(parts[1]);
                            } else {
                                _log.WarnMethodCall("empty key", _filename, count, line);
                            }
                        } else if(line != string.Empty) {
                            _log.WarnMethodCall("bad key/value pair", _filename, count, line);
                        }
                    }
                    sr.Close();
                }
            }
            return resources.GetEnumerator();
        }

        public void Close() { }

        public void Dispose() { }

        //--- Interface ---
        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }
    }

    public interface IPlainTextResourceManager {
        String GetString(String name, CultureInfo culture, string def);
    }

    public class PlainTextResourceManager : IPlainTextResourceManager {

        //--- Fields ---
        private readonly String _resourcePath;
        private readonly Func<string, IResourceReader> _resourceReaderFactory;
        private readonly Dictionary<String, ResourceSet> _resourceData = new Dictionary<String, ResourceSet>();
        private readonly object _resourceLock = new object();

        //--- Constructors ---

        // TODO (arnec): remove this once DekiChangeSubscriptionService gets its resources keys from the API
        [Obsolete("this constructor will be removed in a future version")]
        public PlainTextResourceManager(ResourceSet testSet) {
            lock(_resourceLock) {
                var key = GetResourceKey(new CultureInfo("en"), false);
                ResourceData[key] = testSet;
                key = GetResourceKey(new CultureInfo("en"), true);
                ResourceData[key] = testSet;
                key = GetResourceKey(new CultureInfo("en-us"), false);
                ResourceData[key] = testSet;
                key = GetResourceKey(new CultureInfo("en-us"), true);
                ResourceData[key] = testSet;
                key = GetResourceKey(CultureInfo.InvariantCulture, false);
                ResourceData[key] = testSet;
                key = GetResourceKey(CultureInfo.InvariantCulture, true);
                ResourceData[key] = testSet;
            }
        }

        public PlainTextResourceManager(string resourcePath) : this(resourcePath, filename => new PlainTextResourceReader(filename)) { }

        public PlainTextResourceManager(string resourcePath, Func<string, IResourceReader> resourceReaderFactory) {
            _resourcePath = resourcePath;
            _resourceReaderFactory = resourceReaderFactory;
        }

        //--- Methods ---
        private String GetResourceKey(CultureInfo culture, bool custom) {
            return _resourcePath + "-" + culture.LCID + (custom ? "-custom" : string.Empty);
        }

        private ResourceSet GetResourceSet(CultureInfo culture, bool custom) {
            ResourceSet resource;
            String resourceKey = GetResourceKey(culture, custom);
            lock(_resourceLock) {

                // if the resource data for this culture has not yet been loaded, load it
                if(!ResourceData.TryGetValue(resourceKey, out resource)) {

                    // set the filename according to the cuture
                    String filename = "resources.";
                    if(custom) {
                        filename += "custom.";
                    }
                    if(!culture.Equals(CultureInfo.InvariantCulture)) {
                        filename += culture.Name.ToLowerInvariant() + ".";
                    }
                    filename += "txt";
                    filename = Path.Combine(_resourcePath, filename);

                    // load the resource set and add it into the resource table
                    resource = new ResourceSet(_resourceReaderFactory(filename));

                    // Note (arnec): We call GetString to force the lazy loading of the resource stream, thereby making
                    // GetString(), at least theoretically, thread-safe for subsequent calls.
                    resource.GetString("", true);
                    ResourceData.Add(resourceKey, resource);
                }
            }
            return resource;
        }

        public String GetString(String name, CultureInfo culture, string def) {
            if(null == culture) {
                culture = CultureInfo.InvariantCulture;
            }
            var result = GetString(name, culture, true) ?? GetString(name, culture, false) ?? def;
            return (result != null) ? result.UnescapeString() : null;
        }

        private String GetString(String name, CultureInfo culture, bool custom) {
            var resourceSet = GetResourceSet(culture, custom);
            var result = resourceSet.GetString(name, true);

            // if not found, attempt to load for the parent culture if available
            if(result == null && culture.Parent != culture) {
                return GetString(name, culture.Parent, custom);
            }
            return result;
        }

        //--- Properties ---
        private Dictionary<String, ResourceSet> ResourceData {
            get {

                // NOTE (steveb): make sure this property is _only_ accessed when a lock is taken!
                return _resourceData;
            }
        }

    }

    public class TestResourceSet : ResourceSet {

        // Note (arnec): This is really a fragile implemementation of a test resource set, but'll do for now.
        private readonly Dictionary<string, string> _resources = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public override string GetString(string name) {
            return _resources.ContainsKey(name) ? _resources[name] : null;
        }

        public override string GetString(string name, bool ignoreCase) {
            return GetString(name);
        }

        public void Add(string key, string value) {
            _resources.Add(key, value);
        }
    }
}

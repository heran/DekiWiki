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
using log4net;
using NUnit.Framework;

namespace MindTouch.Deki.Tests {

    [TestFixture]
    public class PlainTextResourceManagerTests {

        [Test]
        public void Gets_default_on_missing_key() {
            var resourceManager = new MockResourceReaderSet {
                new PairSet("resources.txt") {
                    new Pair("Test.Section.foo","bar")
                }
            }.CreatePlainTextResourceManager();
            Assert.AreEqual("bar", resourceManager.GetString("Test.Section.foo", null, "bar"));
            Assert.AreEqual("baz", resourceManager.GetString("Test.Section.blah", null, "baz"));
        }

        [Test]
        public void GetString_falls_through_resource_culture_hierarchy() {
            var resourceManager = new MockResourceReaderSet {
                new PairSet("resources.txt") {
                    new Pair("default","resources")
                },
                new PairSet("resources.custom.txt") {
                    new Pair("custom","resources.custom")
                },
                new PairSet("resources.fr.txt") {
                    new Pair("french","resources.fr")
                },
                new PairSet("resources.custom.fr.txt") {
                    new Pair("custom.french","resources.custom.fr")
                },
                new PairSet("resources.fr-fr.txt") {
                    new Pair("france","resources.fr-fr")
                },
                new PairSet("resources.custom.fr-fr.txt") {
                    new Pair("custom.france","resources.custom.fr-fr")
                },
            }.CreatePlainTextResourceManager();
            Assert.AreEqual("bzzz", resourceManager.GetString("unkown", new CultureInfo("fr-fr"), "bzzz"));
            Assert.AreEqual("resources", resourceManager.GetString("default", new CultureInfo("fr-fr"), "bzzz"));
            Assert.AreEqual("resources.custom", resourceManager.GetString("custom", new CultureInfo("fr-fr"), "bzzz"));
            Assert.AreEqual("resources.fr", resourceManager.GetString("french", new CultureInfo("fr-fr"), "bzzz"));
            Assert.AreEqual("resources.custom.fr", resourceManager.GetString("custom.french", new CultureInfo("fr-fr"), "bzzz"));
            Assert.AreEqual("resources.fr-fr", resourceManager.GetString("france", new CultureInfo("fr-fr"), "bzzz"));
            Assert.AreEqual("resources.custom.fr-fr", resourceManager.GetString("custom.france", new CultureInfo("fr-fr"), "bzzz"));
        }

        internal class MockResourceReaderSet : IEnumerable<PairSet> {

            //--- Class Fields ---
            private static readonly ILog _log = LogUtils.CreateLog();

            //--- Fields ---
            private readonly Dictionary<string, MockResourceReader> _sets = new Dictionary<string, MockResourceReader>();

            //--- Methods ---
            public PlainTextResourceManager CreatePlainTextResourceManager() {
                return new PlainTextResourceManager("path", filename => {
                    _log.DebugFormat("creating: {0}", filename);
                    MockResourceReader reader;
                    if(!_sets.TryGetValue(filename, out reader)) {
                        _log.DebugFormat("unknown set: {0}", filename);
                        return new MockResourceReader(null);
                    }
                    return _sets[filename];
                });
            }

            public void Add(PairSet set) { _sets[Path.Combine("path", set.File)] = new MockResourceReader(set); }

            IEnumerator<PairSet> IEnumerable<PairSet>.GetEnumerator() { throw new NotImplementedException(); }
            public IEnumerator GetEnumerator() { throw new NotImplementedException(); }
        }

        internal class PairSet : IEnumerable<Pair> {
            public string File { get; private set; }
            public List<Pair> _pairs = new List<Pair>();
            public PairSet(string file) {
                File = file;
            }

            public void Add(Pair reader) {
                _pairs.Add(reader);
            }

            public IEnumerator<Pair> GetEnumerator() {
                return _pairs.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator() {
                return GetEnumerator();
            }
        }

        internal class Pair {
            public string Key { get; private set; }
            public string Value { get; private set; }

            public Pair(string key, string value) {
                Key = key;
                Value = value;
            }
        }

        internal class MockResourceReader : IResourceReader {

            //--- Fields ---
            private readonly Dictionary<string, string> _resources = new Dictionary<string, string>();

            //--- Constructors ---
            public MockResourceReader(PairSet set) {
                if(set == null) {
                    return;
                }
                foreach(var pair in set) {
                    _resources[pair.Key] = pair.Value;
                }
            }

            //--- Methods ---
            public void Close() { }
            public IDictionaryEnumerator GetEnumerator() { return _resources.GetEnumerator(); }
            IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }
            public void Dispose() { }
        }
    }
}

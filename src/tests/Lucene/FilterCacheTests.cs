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
using System.Linq;
using MindTouch.LuceneService;
using NUnit.Framework;

namespace MindTouch.Lucene.Tests {

    [TestFixture]
    public class FilterCacheTests {

        [Test]
        public void NeedsFilterCheck_distincts_output() {
            var cache = new FilterCache();
            Assert.AreEqual(new ulong[] { 1, 2, 3 }, cache.NeedsFilterCheck(new ulong?[] { 1, 2, 1, 3, 2, 1 }).ToArray());
        }

        [Test]
        public void NeedsFilterCheck_only_returns_ids_not_already_cached() {
            var cache = new FilterCache();
            cache.NeedsFilterCheck(new ulong?[] { 1, 3, 5 });
            Assert.AreEqual(new ulong[] { 2, 4, 6 }, cache.NeedsFilterCheck(new ulong?[] { 1, 2, 3, 4, 5, 6 }).ToArray());
        }

        [Test]
        public void NeedsFilterCheck_marks_all_canidates_as_not_filtered() {
            var cache = new FilterCache();
            cache.NeedsFilterCheck(new ulong?[] { 1, 2, 3 });
            Assert.AreEqual(new[] { "1f", "2f", "3f" }, cache.Select(x => x.Key + (x.Value ? "t" : "f")).ToArray());
        }

        [Test]
        public void NeedsFilterCheck_discards_null_ids() {
            var cache = new FilterCache();
            Assert.AreEqual(new ulong[] { 1, 2, 3 }, cache.NeedsFilterCheck(new ulong?[] { 1, null, 2, null, 3 }).ToArray());
        }

        [Test]
        public void MarkAsFiltered_marks_all_provided_ids_as_filtered() {
            var cache = new FilterCache();
            cache.NeedsFilterCheck(new ulong?[] { 1, 2, 3, 4, 5, 6 });
            cache.MarkAsFiltered(new ulong[] { 5, 3, 1 });
            Assert.AreEqual(new[] { "1t", "2f", "3t", "4f", "5t", "6f" }, cache.Select(x => x.Key + (x.Value ? "t" : "f")).ToArray());
        }

        [Test]
        public void IsFiltered_for_null_id_returns_false() {
            var cache = new FilterCache();
            Assert.IsFalse(cache.IsFiltered(null));
        }

        [Test]
        public void IsFiltered_for_unknown_id_returns_false() {
            var cache = new FilterCache();
            Assert.IsFalse(cache.IsFiltered(123));
        }

        [Test]
        public void IsFiltered_for_id_seen_by_NeedsFilterCheck_but_not_marked_by_MarkAsFiltered_returns_false() {
            var cache = new FilterCache();
            cache.NeedsFilterCheck(new ulong?[] {123});
            Assert.IsFalse(cache.IsFiltered(123));
        }

        [Test]
        public void IsFiltered_for_id_marked_by_MarkAsFiltered_returns_true() {
            var cache = new FilterCache();
            cache.NeedsFilterCheck(new ulong?[] { 123 });
            cache.MarkAsFiltered(new ulong[]{123});
            Assert.IsTrue(cache.IsFiltered(123));
        }
    }
}

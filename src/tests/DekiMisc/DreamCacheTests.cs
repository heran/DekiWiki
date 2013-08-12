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
using MindTouch.Deki.Caching;
using MindTouch.Extensions.Time;
using MindTouch.Tasking;
using NUnit.Framework;

namespace MindTouch.Deki.Tests {

    [TestFixture]
    public class DreamCacheTests {

        [Test]
        public void Can_set_and_reset_item_in_cache() {
            var cache = new DreamCache(TaskTimerFactory.Current);
            cache.Set("foo", "bar");
            cache.Set("foo", "baz");
            var cached = cache.Get("foo", string.Empty);
            Assert.AreEqual("baz", cached);
        }

        [Test]
        public void Can_set_and_reset_item_in_cache_with_sliding_expiration() {
            var cache = new DreamCache(TaskTimerFactory.Current);
            cache.Set("foo", "bar", 10.Seconds());
            cache.Set("foo", "baz", 15.Seconds());
            var cached = cache.Get("foo", string.Empty);
            Assert.AreEqual("baz", cached);
        }

        [Test]
        public void Can_set_and_reset_item_in_cache_with_expiration() {
            var cache = new DreamCache(TaskTimerFactory.Current);
            cache.Set("foo", "bar", DateTime.UtcNow.AddSeconds(10));
            cache.Set("foo", "baz", DateTime.UtcNow.AddSeconds(15));
            var cached = cache.Get("foo", string.Empty);
            Assert.AreEqual("baz", cached);
        }

        [Test]
        public void Can_dispose_cache() {
            var cache = new DreamCache(TaskTimerFactory.Current);
            cache.Set("foo", "bar");
            cache.Dispose();
        }
    }
}

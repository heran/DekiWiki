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
using System.Linq;
using System.Text;
using System.Threading;
using MindTouch.Deki.UserSubscription;
using MindTouch.Tasking;
using NUnit.Framework;

namespace MindTouch.Deki.Tests.ChangeSubscriptionTests {
    using Yield = IEnumerator<IYield>;

    [TestFixture]
    public class NotificationDelayQueueTests {
        private Result<NotificationUpdateRecord> expirationResult;

        [Test]
        public void First_modification_date_is_returned_on_dequeue() {
            expirationResult = new Result<NotificationUpdateRecord>(TimeSpan.FromSeconds(10));
            var now = DateTime.UtcNow;
            var queue = new NotificationDelayQueue(TimeSpan.FromMilliseconds(500), ExpirationCallback);
            queue.Enqueue("foo", 1, 1, now);
            queue.Enqueue("foo", 1, 1, now.AddMinutes(1));
            queue.Enqueue("foo", 1, 1, now.AddMinutes(2));
            expirationResult.Wait();
            var pages = expirationResult.Value.Pages.ToList();
            Assert.AreEqual(1,pages.Count);
            Assert.AreEqual(now,pages[0].Item2);
        }

        private Yield ExpirationCallback(NotificationUpdateRecord record, Result result) {
            expirationResult.Return(record);
            result.Return();
            yield break;
        }
    }
}

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
using MindTouch.Deki.Logic;
using MindTouch.Deki.WikiManagement;
using NUnit.Framework;

namespace MindTouch.Deki.Tests.LicenseTests {
    public class LicenseStateTransitionMock {

        //--- Fields ---
        private LicenseData _previous;
        private LicenseData _new;
        private int _called;

        //--- Methods ---
        public void Callback(LicenseData previous, LicenseData @new) {
            _previous = previous;
            _new = @new;
            _called++;
        }

        public void Verify(LicenseStateType previous, LicenseStateType @new) {
            Verify(previous, @new, 1);
        }

        public void Verify(LicenseStateType previous, LicenseStateType @new, int times) {
            Assert.AreEqual(times, _called, "transition was called the wrong number of times");
            Assert.AreEqual(previous, _previous.LicenseState, "previous state was wrong");
            Assert.AreEqual(@new, _new.LicenseState, "new state was wrong");
        }

        public void Never() {
            Assert.AreEqual(0, _called, "Transition was called");
        }
    }
}
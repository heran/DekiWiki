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

using System.Collections.Generic;

namespace MindTouch.LuceneService {

    public class ActionStack {
        private readonly List<bool> _stack = new List<bool>();

        public bool IsAdd {
            get { return (Head && Tail) || (Tail && !Head); }
        }

        public bool IsDelete {
            get { return (!Head && !Tail) || (Tail && !Head); }
        }

        private bool Head { get { return _stack[0]; } }
        private bool Tail { get { return _stack[_stack.Count - 1]; } }

        public void PushAdd() {
            _stack.Add(true);
        }

        public void PushDelete() {
            _stack.Add(false);
        }
    }
}

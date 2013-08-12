/*
 * MindTouch Deki Script - embeddable web-oriented scripting runtime
 * Copyright (C) 2006-2008 MindTouch, Inc.
 * www.mindtouch.com  oss@mindtouch.com
 *
 * For community documentation and downloads visit www.opengarden.org;
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

namespace MindTouch.Deki.Script {
    public enum DekiScriptFlowControl {
        Break,
        Continue
    }

    public class DekiScriptFlowControlException : DekiScriptException {

        //--- Fields ---
        public readonly DekiScriptFlowControl FlowControl;
        public readonly DekiScriptLiteral AccumulatedState;

        //--- Constructors ---
        public DekiScriptFlowControlException(DekiScriptFlowControl flowControl)
            : this(flowControl, DekiScriptNil.Value) {
        }
        public DekiScriptFlowControlException(DekiScriptFlowControl flowControl, DekiScriptLiteral accumulatedState) : base(0, 0, string.Format("Unhandled flow control statement '{0}'", flowControl.ToString().ToLower())) {
            FlowControl = flowControl;
            AccumulatedState = accumulatedState;
        }
    }
}
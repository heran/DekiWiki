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
using System.Text;
using MindTouch.Deki;

namespace MindTouch.Tools {
    class IPBlockBE {
        public string Address;
        public uint UserID;
        public uint ByUserID;
        public string Reason;
        public string Timestamp;
        public uint Auto;
        public uint AnonymousOnly;
        public uint CreateAccount;
        public uint EnableAutoBlock;
        public string Expiry;
    }
}

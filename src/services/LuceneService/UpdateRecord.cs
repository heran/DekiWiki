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
using MindTouch.Dream;
using MindTouch.Xml;

namespace MindTouch.LuceneService {

    public class UpdateRecord {

        //--- Fields ---
        public readonly XUri Id;
        public readonly string WikiId;
        public readonly XDoc Meta;
        public readonly ActionStack ActionStack = new ActionStack();
        public readonly List<long> QueueIds = new List<long>();

        //--- Constructors ---
        public UpdateRecord(XUri id, XDoc meta, string wikiId) {
            Id = id;
            Meta = meta;
            WikiId = wikiId;
        }

        private UpdateRecord(UpdateRecord current, XDoc meta) {
            Id = current.Id;
            ActionStack = current.ActionStack;
            Meta = meta;
            WikiId = current.WikiId;
            QueueIds.AddRange(current.QueueIds);
        }

        //--- Methods ---
        public UpdateRecord With(XDoc meta) {
            return new UpdateRecord(this, meta);
        }
    }
}
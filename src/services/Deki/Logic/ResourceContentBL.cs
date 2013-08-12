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

using System.IO;
using MindTouch.Deki.Data;
using MindTouch.Dream;
using MindTouch.IO;

namespace MindTouch.Deki.Logic {
    public class ResourceContentBL {
        
        //--- Class Fields ---
        private static readonly ResourceContentBL _instance = new ResourceContentBL();

        //--- Class Properties ---
        public static ResourceContentBL Instance { get { return _instance; } }

        //--- Methods ---
        public ResourceContentBE CreateDbSerializedContentFromStream(Stream stream, long length, MimeType type) {
            var memorystream = new ChunkedMemoryStream();
            stream.CopyTo(memorystream, length);
            return new ResourceContentBE(memorystream, type);
        }

        public ResourceContentBE CreateDbSerializedContentFromRequest(DreamMessage request) {
            return CreateDbSerializedContentFromStream(request.AsStream(), request.ContentLength, request.ContentType);
        }

        public ResourceContentBE Get(ResourceBE resource) {
            return resource.Content;
        }
    }
}
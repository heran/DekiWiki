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
using System.Linq;
using System.Text;

namespace MindTouch.Deki {
    public class DekiResourceBuilder {

        //--- Fields ---
        private readonly List<object> _resourceChain = new List<object>();

        //--- Constructors ---
        public DekiResourceBuilder() { }

        public DekiResourceBuilder(DekiResource resource) {
            Append(resource);
        }

        public DekiResourceBuilder(string localizedString) {
            Append(localizedString);
        }

        //--- Properties ---
        public bool IsEmpty { get { return !_resourceChain.Any(); } }

        //--- Methods ---
        public void Append(string localizedString) {
            if(string.IsNullOrEmpty(localizedString)) {
                return;
            }
            _resourceChain.Add(localizedString);
        }

        public void Append(DekiResource resource) {
            _resourceChain.Add(resource);
        }

        public string Localize(DekiResources resources) {
            var _builder = new StringBuilder();
            foreach(var item in _resourceChain) {
                var resource = item as DekiResource;
                if(resource == null) {
                    _builder.Append(item);
                } else {
                    _builder.Append(resources.Localize(resource));
                }
            }
            return _builder.ToString();
        }
    }
}
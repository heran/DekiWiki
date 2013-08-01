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

using MindTouch.Dream;

namespace MindTouch.Deki.Data {
    public class NavBE {

        //--- Fields ---
        private uint _id;
        private string _title;
        private ushort _nameSpace;
        private ulong _parentId;
        private int? _childCount;
        private ulong? _restrictionFlags;
        private string _sortableTitle;
        private string _displayName;

        //--- Properties ---
        public virtual uint Id {
            get { return _id; }
            set { _id = value; }
        }

        public virtual ushort NameSpace {
            get { return _nameSpace; }
            set { _nameSpace = value; }
        }

        public virtual string DisplayName {
            get { return _displayName; }
            set { _displayName = value; }
        }

        public virtual ulong ParentId {
            get { return _parentId; }
            set { _parentId = value; }
        }

        public virtual int? ChildCount {
            get { return _childCount; }
            set { _childCount = value; }
        }


        public virtual ulong? RestrictionFlags {
            get { return _restrictionFlags; }
            set { _restrictionFlags = value; }
        }

        public virtual string Title {
            get { return _title; }
            set {
                _title = value;
                _sortableTitle = XUri.Decode(_title).Replace("//", "\uFFEF").Replace("/", "\t"); ;
            }
        }

        public virtual string SortableTitle { get { return _sortableTitle; } }

        //--- Methods ---
        public virtual NavBE Copy() {
            NavBE np = new NavBE();
            np.ChildCount = ChildCount;
            np.DisplayName = DisplayName;
            np.Id = Id;
            np.NameSpace = NameSpace;
            np.ParentId = ParentId;
            np.RestrictionFlags = RestrictionFlags;
            np.Title = Title;
            return np;
        }
    }

}

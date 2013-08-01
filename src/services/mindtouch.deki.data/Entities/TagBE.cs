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
namespace MindTouch.Deki.Data {

    [Serializable]
    public class TagBE {

        //--- Fields ---
        protected uint _id;
        protected string _name;
        protected uint _type;
        [NonSerialized] protected PageBE _definedTo;
        [NonSerialized] protected PageBE[] _relatedPages;
        [NonSerialized] protected PageBE[] _taggedPages;
        protected int _occuranceCount = int.MinValue;

        //--- Properties ---
        public virtual uint Id {
            get { return _id; }
            set { _id = value; }
        }

        public virtual string Name {
            get { return _name; }
            set { _name = value; }
        }

        public virtual uint _Type {
            get { return _type; }
            set { _type = value; }
        }

        public virtual TagType Type {
            get { return (TagType)_type; }
            set { _type = (uint)value; }
        }

        // TODO (brigettek) : Delay populated field
        public virtual PageBE DefinedTo {
            get { return _definedTo; }
            set { _definedTo = value; }
        }

        // TODO (brigettek) : Delay populated field
        public virtual PageBE[] RelatedPages {
            get { return _relatedPages; }
            set { _relatedPages = value; }
        }

        // TODO (brigettek) : This is a delay populated field
        public virtual int OccuranceCount {
            get { return _occuranceCount; }
            set { _occuranceCount = value; }
        }

        public virtual string Prefix {
            get {
                switch (Type) {
                    case TagType.DEFINE:
                        return TagPrefix.DEFINE;
                    case TagType.DATE:
                        return TagPrefix.DATE;
                    case TagType.USER:
                        return TagPrefix.USER;
                    default:
                        return TagPrefix.TEXT;
                }
            }
        }

        public virtual string PrefixedName {
            get { return Prefix + Name; }
        }

        //--- Methods ---
        public virtual TagBE Copy() {
            TagBE tag = new TagBE();
            tag._Type = _Type;
            tag.Id = Id;
            tag.Name = Name;
            return tag;
        }
    }
}

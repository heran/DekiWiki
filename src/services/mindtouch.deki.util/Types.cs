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
namespace MindTouch.Deki {

    public enum PagePathType : byte {
        UNDEFINED = 0,
        LINKED = 1, // page name is a function of the title
        CUSTOM = 2, // page name and title are not functionally related
        FIXED = 3   // page may not be moved
    }
    
    public enum NS {
        MAIN = 0,
        MAIN_TALK = 1,
        USER = 2,
        USER_TALK = 3,
        PROJECT = 4,
        PROJECT_TALK = 5,
        // IMAGE = 6,
        // IMAGE_TALK = 7,
        // NATIVE = 8,
        // NATIVE_TALK = 9,
        TEMPLATE = 10,
        TEMPLATE_TALK = 11,
        HELP = 12,
        HELP_TALK = 13,
        // CATEGORY = 14,
        // CATEGORY_TALK = 15,
        ATTACHMENT = 16,
        SPECIAL = 101,
        SPECIAL_TALK = 104,

        // Virtual namespaces that do not appear in the page database        
        UNKNOWN = 102,

        // NOTE (steveb): Admin: namespace has been deprecated since "Lyons" release
        ADMIN = 103,
    }
}

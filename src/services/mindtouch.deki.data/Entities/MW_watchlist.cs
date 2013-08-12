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
using MindTouch;

/* TODO (brigette):  Uncomment if/when watchlists are implemented on the API side

namespace MindTouch.Deki {
    /// <summary>
    /// Summary description for MW_watchlist
    /// </summary>
    [DatabaseTable("watchlist")]
    public class watchlist : DatabaseObject {
        // int(5) unsigned NOT NULL,
        private uint wl_user;
        [DatabaseField(Name = "wl_user")]
        public uint UserID {
            get { return wl_user; }
            set { wl_user = value; }
        }

        // tinyint(2) unsigned NOT NULL default '0'
        [DatabaseField(Name = "wl_namespace")]
        public ushort _Namespace {
            get { return (ushort)Title.Namespace; }
            set { Title.Namespace = (NS)value; }
        }

        // varchar(255) binary NOT NULL default ''
        [DatabaseField(Name = "wl_title")]
        public string _Title {
            get { return Title.AsUnprefixedDbPath(); }
            set { Title.Path = value; }
        }

        private Title _title = null;
        public Title Title {
            get {
                if (null == _title) {
                    _title = Title.FromDbPath(NS.UNKNOWN, String.Empty, null);
                }
                return _title;
            }
            set {
                _title = value;
            }
        }

        // UNIQUE KEY (wl_user, wl_namespace, wl_title),
        // KEY namespace_title (wl_namespace,wl_title)

        internal static watchlist fromUserTitle(UserBE user, PageBE title) {
            watchlist wl = new watchlist();
            wl.UserID = user.ID;
            wl.Title = title.Title;
            return wl;
        }

        internal bool isWatched() {
            // Pages and their talk pages are considered equivalent for watching;
            // remember that talk namespaces are numbered as page namespace+1.
            return ObjectExists(DekiContext.Current.Instance.Catalog.ConnectionString, BuildSelectQuery(new string[] { "wl_user", "wl_namespace", "wl_title" }, 0, true),
                "wl_user", UserID,
                "wl_namespace", (int)Title.Namespace,
                "wl_title", Title
            );
        }

        internal bool addWatch() {
            string sql = base.BuildReplaceQuery();
            ExecuteNonQuery(DekiContext.Current.Instance.Catalog.ConnectionString, sql,
                "wl_title", this.Title.AsUnprefixedDbPath(),
                "wl_namespace", (int)Title.Namespace,
                "wl_user", this.UserID
                );
           
            return true;

        }

        internal bool removeWatch() {

            string sql = BuildDeleteQuery(BuildAndList("wl_title", "wl_user", "wl_namespace"));

            ExecuteNonQuery(DekiContext.Current.Instance.Catalog.ConnectionString, sql,
                "wl_title", this.Title.AsUnprefixedDbPath(),
                "wl_namespace", (int)Title.Namespace,
                "wl_user", this.UserID
                );

            return true;
        }
    }
}
*/
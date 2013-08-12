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
using MindTouch.Data;
using MindTouch.Deki.Data.UserSubscription;
using MindTouch.Xml;
using MySql.Data.MySqlClient;

namespace MindTouch.Deki.Data.MySql.UserSubscription {
    public class MySqlPageSubscriptionSessionFactory : IPageSubscriptionDataSessionFactory {

        //--- Class Fields ---
        private static readonly log4net.ILog _log = LogUtils.CreateLog();
        
        //--- Fields ---
        private readonly XDoc _config;
        private readonly DataFactory _dataFactory;

        //--- Constructors ---
        public MySqlPageSubscriptionSessionFactory(XDoc config) {
            _config = config;
            var catalog = config["db-catalog"].AsText;
            _log.DebugFormat("created MySqlPageSubscriptionSessionFactory for catalog '{0}'", catalog);
            _dataFactory = new DataFactory(MySqlClientFactory.Instance, "?");
        }

        //--- Methods ---
        public IPageSubscriptionDataSession Create() {
            return new MySqlPageSubscriptionSession(new DataCatalog(_dataFactory, _config));
        }
    }
}

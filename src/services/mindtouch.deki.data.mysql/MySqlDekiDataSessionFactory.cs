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

using MySql.Data.MySqlClient;
using MindTouch.Data;
using MindTouch.Xml;

namespace MindTouch.Deki.Data.MySql {
    public class MySqlDekiDataSessionFactory : IDekiDataSessionFactory {

        //--- Fields ---
        private XDoc _config;
        private IInstanceSettings _instanceSettings;
        private DataFactory _dataFactory;

        //--- Properties ---
        public XDoc Config { get { return _config; } }

        //--- Methods ---
        public void Dispose() {}
        
        public void Initialize(XDoc config, XDoc license, IInstanceSettings instanceSettings) {
            _dataFactory = new DataFactory(MySqlClientFactory.Instance, "?");
            _config = config;
            _instanceSettings = instanceSettings;
        }

        public IDekiDataSession CreateSession() {
            return new MySqlDekiDataSession(_instanceSettings, new DataCatalog(_dataFactory, _config));
        }
    }
}

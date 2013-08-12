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
using System.Text;
using System.Data;
using System.Collections.Specialized;

using MindTouch.Data;
using MindTouch.Deki.Data;

namespace MindTouch.Deki.Data.MySql {
    public partial class MySqlDekiDataSession {

        //--- Class Methods ---
        public IList<KeyValuePair<string, ConfigValue>> Config_ReadInstanceSettings() {
            List<KeyValuePair<string, ConfigValue>> settings = new List<KeyValuePair<string, ConfigValue>>();
            Catalog.NewQuery(@" /* Config_ReadInstanceSettings */ 
select config_id, config_key, config_value from config;")
                .Execute(delegate(IDataReader dr) {
                while(dr.Read()) {
                    string key = DbUtils.Convert.To<string>(dr["config_key"], string.Empty);
                    string val = DbUtils.Convert.To<string>(dr["config_value"], string.Empty);
                    if(!string.IsNullOrEmpty(key)) {
                        settings.Add(new KeyValuePair<string,ConfigValue>(key, new ConfigValue(val)));
                    }
                }
            });
            return settings;
        }

        public void Config_WriteInstanceSettings(IList<KeyValuePair<string, string>> keyValues) {
            StringBuilder query = new StringBuilder();
            query.Append(@" /* Config_WriteInstanceSettings */ 
DELETE FROM `config`; 
INSERT INTO `config` (`config_key`, `config_value`) VALUES ");
            bool first = true;
            foreach (KeyValuePair<string, string> setting in keyValues) {
                string key = DataCommand.MakeSqlSafe(setting.Key.TrimEnd(new char[] { '/' }));
                string val = DataCommand.MakeSqlSafe(setting.Value);
                query.AppendFormat("{0}('{1}', '{2}' )", first ? string.Empty : ",", key, val);
                first = false;
            }
            query.Append(";");
            Catalog.NewQuery(query.ToString()).Execute();
        }
    }
}

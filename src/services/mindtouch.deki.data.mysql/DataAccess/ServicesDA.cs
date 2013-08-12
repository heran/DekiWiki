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
using MySql.Data.MySqlClient;

namespace MindTouch.Deki.Data.MySql {
    public partial class MySqlDekiDataSession {

        private static readonly IDictionary<ServicesSortField, string> SERVICES_SORT_FIELD_MAPPING = new Dictionary<ServicesSortField, string>() 
            { { ServicesSortField.DESCRIPTION, "service_description" }, 
              { ServicesSortField.INIT, "service_local" },
              { ServicesSortField.LOCAL, "service_local" }, // deprecated
              { ServicesSortField.SID,  "service_sid" }, 
              { ServicesSortField.TYPE, "service_type" },
              { ServicesSortField.URI, "service_uri" },
              { ServicesSortField.ID, "service_id"} };

        public IList<ServiceBE> Services_GetAll() {
            IList<ServiceBE> result = null;
            Catalog.NewQuery(@" /* Services_GetAll */
select *
from  services;

select *
from service_config;

select *
from service_prefs;")
            .Execute(delegate(IDataReader dr) {
                result = Services_Populate(dr);
            });
            return result;
        }

        public IList<ServiceBE> Services_GetByQuery(string serviceType, SortDirection sortDir, ServicesSortField sortField, uint? offset, uint? limit, out uint totalCount, out uint queryCount) {
            IList<ServiceBE> services = null;
            uint totalCountTemp = 0;
            uint queryCountTemp = 0;
            StringBuilder whereQuery = new StringBuilder(" where 1=1");
            if(!string.IsNullOrEmpty(serviceType)) {
                whereQuery.AppendFormat(" AND service_type = '{0}'", DataCommand.MakeSqlSafe(serviceType));
            }

            StringBuilder sortLimitQuery = new StringBuilder();
            string sortFieldString = null;
            
            //Sort by id if no sort specified.
            if(sortField == ServicesSortField.UNDEFINED) {
                sortField = ServicesSortField.ID;
            }
            if(SERVICES_SORT_FIELD_MAPPING.TryGetValue(sortField, out sortFieldString)) {
                sortLimitQuery.AppendFormat(" order by {0} ", sortFieldString);
                if(sortDir != SortDirection.UNDEFINED) {
                    sortLimitQuery.Append(sortDir.ToString());
                }
            } 
            if(limit != null || offset != null) {
                sortLimitQuery.AppendFormat(" limit {0} offset {1}", limit ?? int.MaxValue, offset ?? 0);
            }

            string query = string.Format(@" /* Services_GetByQuery */
select *
from services
{0}
{1};

select service_config.*
from service_config
join (
    select service_id
    from services
    {0}
    {1}
) s on service_config.service_id = s.service_id;

select service_prefs.*
from service_prefs
join (
    select service_id
    from services
    {0}
    {1}
) s on service_prefs.service_id = s.service_id;

select count(*) as totalcount from services;
select count(*) as querycount from services {0};
", whereQuery, sortLimitQuery);

            Catalog.NewQuery(query)
            .Execute(delegate(IDataReader dr) {
                services = Services_Populate(dr);

                if(dr.NextResult() && dr.Read()) {
                    totalCountTemp = DbUtils.Convert.To<uint>(dr["totalcount"], 0);
                }

                if(dr.NextResult() && dr.Read()) {
                    queryCountTemp = DbUtils.Convert.To<uint>(dr["querycount"], 0);
                }

            });

            totalCount = totalCountTemp;
            queryCount = queryCountTemp;

            return services == null ? new List<ServiceBE>() : services;
        }

        public ServiceBE Services_GetById(uint serviceid) {
            IList<ServiceBE> result = null;

            Catalog.NewQuery(@" /* Services_GetById */
select *
from  services
where service_id = ?SERVICEID;

select *
from service_config
where service_id = ?SERVICEID;

select *
from service_prefs
where service_id = ?SERVICEID;")
                .With("SERVICEID", serviceid)
                .Execute(delegate(IDataReader dr) {
                result = Services_Populate(dr);
            });
            return (result.Count > 0) ? result[0] : null;
        }

        public void Services_Delete(uint serviceId) {

            Catalog.NewQuery(@" /* Services_Delete */
delete from services where service_id = ?ID;
delete from service_prefs where service_id = ?ID;
delete from service_config where service_id = ?ID;"
                ).With("ID", serviceId)
                .Execute();
        }

        public uint Services_Insert(ServiceBE service) {

            StringBuilder query = null;
            if(service.Id == 0) {

                //new service
                query = new StringBuilder(@" /* Services_Insert */
insert into services (service_type, service_sid, service_uri, service_description, service_local, service_enabled, service_last_edit, service_last_status)
values (?TYPE, ?SID, ?URI, ?DESC, ?LOCAL, ?ENABLED, ?TIMESTAMP, ?LASTSTATUS);
");
                query.AppendLine("select LAST_INSERT_ID() into @service_id;");
                query.AppendLine("select LAST_INSERT_ID() as service_id;");
            } else {

                //update existing service
                query = new StringBuilder(@" /* Services_Insert (with id) */
insert into services (service_id, service_type, service_sid, service_uri, service_description, service_local, service_enabled, service_last_edit, service_last_status)
values (?ID, ?TYPE, ?SID, ?URI, ?DESC, ?LOCAL, ?ENABLED, ?TIMESTAMP, ?LASTSTATUS);
");
                query.AppendLine(string.Format("select {0} into @service_id;", service.Id));
                query.AppendLine(string.Format("select {0} as service_id;", service.Id));
            }

            if(service.Preferences != null && service.Preferences.Count > 0) {
                query.Append("insert into service_prefs (service_id, pref_name, pref_value) values ");
                for(int i = 0; i < service.Preferences.AllKeys.Length; i++) {
                    string key = DataCommand.MakeSqlSafe(service.Preferences.AllKeys[i]);
                    string val = DataCommand.MakeSqlSafe(service.Preferences[key]);
                    query.AppendFormat("{0}(@service_id, '{1}', '{2}')\n", i > 0 ? "," : string.Empty, key, val);
                }
                query.AppendLine(";");
            }

            if(service.Config != null && service.Config.Count > 0) {
                query.Append("insert into service_config (service_id, config_name, config_value) values ");
                for(int i = 0; i < service.Config.AllKeys.Length; i++) {
                    string key = DataCommand.MakeSqlSafe(service.Config.AllKeys[i]);
                    string val = DataCommand.MakeSqlSafe(service.Config[key]);
                    query.AppendFormat("{0}(@service_id, '{1}', '{2}')\n", i > 0 ? "," : string.Empty, key, val);
                }
                query.AppendLine(";");
            }
            uint serviceId = 0;
            try {
                serviceId = Catalog.NewQuery(query.ToString())
                    .With("ID", service.Id)
                    .With("TYPE", service.Type.ToString())
                    .With("SID", service.SID)
                    .With("URI", service.Uri)
                    .With("DESC", service.Description)
                    .With("LOCAL", service.ServiceLocal)
                    .With("ENABLED", service.ServiceEnabled)
                    .With("TIMESTAMP", service.ServiceLastEdit)
                    .With("LASTSTATUS", service.ServiceLastStatus)
                    .ReadAsUInt() ?? 0;
            } catch(MySqlException e) {

                // catch Duplicate Key (1062)
                if(e.Number == 1062) {
                    serviceId = service.Id;
                } else {
                    throw;
                }
            }
            return serviceId;
        }

        private IList<ServiceBE> Services_Populate(IDataReader dr) {

            // read all services
            List<ServiceBE> orderedServiceList = new List<ServiceBE>();
            Dictionary<uint, ServiceBE> result = new Dictionary<uint, ServiceBE>();
            ServiceBE s = null;
            while(dr.Read()) {
                s = new ServiceBE();
                s._ServiceEnabled = dr.Read<byte>("service_enabled");
                s._ServiceLocal = dr.Read<byte>("service_local");
                s.Description = dr.Read<string>("service_description");
                s.Id = dr.Read<uint>("service_id");
                s.ServiceLastEdit = dr.Read<DateTime>("service_last_edit");
                s.ServiceLastStatus = dr.Read<string>("service_last_status");
                s.SID = dr.Read<string>("service_sid");
                s.Type = dr.Read<ServiceType>("service_type");
                s.Uri = dr.Read<string>("service_uri");

                result.Add(s.Id, s);
                orderedServiceList.Add(s);
            }

            // read config key/value pairs for each service
            dr.NextResult();
            while(dr.Read()) {
                uint serviceId = DbUtils.Convert.To<uint>(dr["service_id"], 0);
                if(serviceId != 0 && result.ContainsKey(serviceId)) {
                    s = result[serviceId];
                    string config_name = DbUtils.Convert.To<string>(dr["config_name"], "");
                    string config_value = DbUtils.Convert.To<string>(dr["config_value"], "");
                    s.Config[config_name] = config_value;
                }
            }

            //  read preference key/value pairs for each service
            dr.NextResult();
            while(dr.Read()) {
                uint serviceId = DbUtils.Convert.To<uint>(dr["service_id"], 0);
                if(serviceId != 0 && result.ContainsKey(serviceId)) {
                    s = result[serviceId];
                    string pref_name = DbUtils.Convert.To<string>(dr["pref_name"], "");
                    string pref_value = DbUtils.Convert.To<string>(dr["pref_value"], "");
                    s.Preferences[pref_name] = pref_value;
                }
            }

            return orderedServiceList;
        }
    }
}

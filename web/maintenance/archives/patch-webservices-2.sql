alter table `services` add column `service_subtype_id` int(4) unsigned after `service_type_id`;
alter table `services` add column `service_uri` varchar(255) after `service_description`;

update `services` set `service_subtype_id` = 1, `service_uri` = '/@api/deki/users/authenticate' where `service_id` = 1;
update `services` set `service_subtype_id` = 2, `service_uri` = '/@api/ldap/users/authenticate' where `service_id` = 2;
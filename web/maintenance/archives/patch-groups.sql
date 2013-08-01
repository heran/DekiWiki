alter table groups add column group_service_id int(4) unsigned;
update groups set group_service_id = '2';
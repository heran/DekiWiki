alter table `services` change column `service_subtype_id` service_sid varchar(255) not null;

update `services` set `service_sid` = 'http://services.mindtouch.com/deki/draft/2006/11/dekiwiki' where `service_id` = '1';
update `services` set `service_sid` = 'http://services.mindtouch.com/deki/draft/2006/12/ldapdirectory' where `service_id` = '2';
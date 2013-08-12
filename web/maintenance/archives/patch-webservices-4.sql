alter table services change service_type_id service_type varchar(255) not null;
update services set service_type = 'auth';
update services set service_uri = concat('http://localhost', service_uri);
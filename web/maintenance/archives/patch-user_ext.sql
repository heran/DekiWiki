alter table `users` add column `user_external_name` varchar (255)  NULL  after `user_active`;
alter table `users` drop key `user_name`;
alter table `users` add unique `user_real_name_service_id` (`user_external_name`, `user_service_id`);
alter table `users` add unique `user_name` (`user_name`);

update `users` set user_external_name = user_name where user_service_id > 1;
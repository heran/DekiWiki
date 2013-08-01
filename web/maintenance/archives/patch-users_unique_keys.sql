ALTER TABLE `groups` ADD UNIQUE KEY `group_name` (`group_name`, `group_service_id`);
ALTER TABLE `users` DROP KEY `user_name`;
ALTER TABLE `users` ADD UNIQUE KEY `user_name` (`user_name`, `user_service_id`);

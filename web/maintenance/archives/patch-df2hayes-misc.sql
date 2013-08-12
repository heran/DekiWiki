INSERT INTO `users` (`user_id`,`user_name`,`user_real_name`,`user_password`,`user_newpassword`,`user_email`,`user_options`,`user_touched`,`user_token`,`user_role_id`,`user_active`,`user_service_id`) values ( NULL,'Anonymous','Anon User','','','','','0','','3','1','1');
UPDATE `users` SET user_builtin=1 WHERE user_id=1;
UPDATE `pages` SET page_title='' WHERE page_title='Home' LIMIT 1;
DELETE FROM `pages` WHERE page_namespace=8;

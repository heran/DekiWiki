ALTER TABLE `users` ADD `user_builtin` tinyint(1) unsigned NOT NULL default 0;
UPDATE `users` set user_builtin=1 WHERE user_name IN ('Sysop', 'MindTouch Support', 'Anonymous');


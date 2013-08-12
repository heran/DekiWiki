ALTER TABLE `roles` ADD `role_hidden` tinyint(1) unsigned not null default 0 AFTER `role_perm_flags`;
UPDATE `roles` SET role_hidden=1 WHERE role_name IN ('None','Guest');
 

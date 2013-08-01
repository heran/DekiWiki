ALTER TABLE `tags` ADD `tag_type` tinyint(2) unsigned NOT NULL default '0';
ALTER TABLE `tags` ADD INDEX (`tag_type`);

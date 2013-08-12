ALTER TABLE `pages` ADD `page_language` VARCHAR( 10 ) NOT NULL DEFAULT '';
ALTER TABLE `old` ADD `old_language` VARCHAR( 10 ) NOT NULL DEFAULT '';
ALTER TABLE `archive` ADD `ar_language` VARCHAR( 10 ) NOT NULL DEFAULT '';

ALTER TABLE `pages` ADD `page_display_name` VARCHAR( 255 ) DEFAULT NULL;
ALTER TABLE `old` ADD `old_display_name` VARCHAR( 255 ) DEFAULT NULL;
ALTER TABLE `archive` ADD `ar_display_name` VARCHAR( 255 ) DEFAULT NULL;
 
ALTER TABLE `archive` ADD `ar_content_type` VARCHAR( 255 ) NOT NULL DEFAULT 'application/x.deki-text';
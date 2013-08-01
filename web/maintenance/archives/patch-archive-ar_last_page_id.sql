ALTER TABLE `archive` ADD `ar_last_page_id` INT( 8 ) UNSIGNED NOT NULL default '0';
ALTER TABLE `archive` ADD KEY `ar_last_page_id` (`ar_last_page_id`);

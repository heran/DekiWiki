CREATE TABLE `ratings` (
	`rating_id` int(10) unsigned NOT NULL auto_increment,
	`rating_user_id` int(10) unsigned NOT NULL,
	`rating_resource_id` int(10) unsigned NOT NULL,
	`rating_resource_type` tinyint(3) unsigned NOT NULL,
	`rating_resource_revision` int(10) unsigned default NULL,
	`rating_score` float unsigned NOT NULL,
	`rating_timestamp` timestamp NOT NULL default '0000-00-00 00:00:00',
	`rating_reset_timestamp` timestamp NULL default NULL,
	PRIMARY KEY (`rating_id`),
	KEY `resource_id_type_resetts_user` (`rating_resource_id`,`rating_resource_type`,`rating_reset_timestamp`,`rating_user_id`)
) ENGINE=MyISAM DEFAULT CHARSET=utf8 COLLATE utf8_general_ci;

CREATE TABLE `ratingscomputed` (
	`ratingscomputed_id` int(10) unsigned NOT NULL auto_increment,
	`ratingscomputed_resource_id` int(10) unsigned NOT NULL,
	`ratingscomputed_resource_type` tinyint(3) unsigned NOT NULL,
	`ratingscomputed_score` float unsigned NOT NULL,
	`ratingscomputed_score_trend` float unsigned NOT NULL,
	`ratingscomputed_count` int(10) unsigned NOT NULL,
	`ratingscomputed_timestamp` timestamp NOT NULL default '0000-00-00 00:00:00',
	PRIMARY KEY (`ratingscomputed_id`),
	UNIQUE KEY `resource_id_type` (`ratingscomputed_resource_id`,`ratingscomputed_resource_type`)
) ENGINE=MyISAM DEFAULT CHARSET=utf8 COLLATE utf8_general_ci;

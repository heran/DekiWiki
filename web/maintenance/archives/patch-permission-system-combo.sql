DROP TABLE if exists `users`;
CREATE TABLE if not exists `users` DEFAULT CHARSET=utf8 SELECT * FROM `user`;
alter table `user` rename to `user_deprecated`;
alter table `user_rights` rename to `user_rights_deprecated`;
alter table `users` modify column `user_id` int(10) unsigned primary key not null auto_increment;
alter table `users` modify column `user_name` varchar(255) unique not null;
alter table `users` modify column `user_real_name` varchar(255) default null;
alter table `users` modify column `user_email` varchar(255) default null;
alter table `users` add column `user_role_id` int(4) unsigned not null;
alter table `users` add column `user_active` tinyint(1) unsigned not null;
ALTER TABLE `users` ADD COLUMN `user_service_id` INT(4) UNSIGNED NOT NULL;
update `users` set `user_role_id` = '4', `user_active` = '1';
update `users` set `user_role_id` = '5' where `user_id` = '1';
UPDATE `users` SET `user_service_id` = '1';


DROP TABLE IF EXISTS `groups`;
CREATE TABLE `groups` (
  `group_id` INT(10) UNSIGNED NOT NULL AUTO_INCREMENT,
  `group_name` VARCHAR(255)  NOT NULL,
  `group_role_id` INT(4) UNSIGNED NOT NULL,
  `group_service_id` int(4) unsigned not null,
  `group_creator_user_id` int(10) unsigned not null,
  `group_last_edit` timestamp,
  PRIMARY KEY(`group_id`)
) ENGINE = MYISAM DEFAULT CHARSET=utf8;
update `groups` set `group_service_id` = '2';

DROP TABLE IF EXISTS `restrictions`;
CREATE TABLE `restrictions` (
  `restriction_id` INT(4) UNSIGNED NOT NULL AUTO_INCREMENT,
  `restriction_name` VARCHAR(255)  NOT NULL,
  `restriction_perm_flags` MEDIUMINT UNSIGNED NOT NULL,
  `restriction_creator_user_id` int(10) unsigned not null,
  `restriction_last_edit` timestamp, 
  PRIMARY KEY(`restriction_id`)
) ENGINE = MYISAM DEFAULT CHARSET=utf8;
INSERT INTO `restrictions` (`restriction_name`, `restriction_perm_flags`, `restriction_creator_user_id`) VALUES
('Public', '2047', '1'),
('Semi-Public', '15', '1'),
('Private', '3', '1');


DROP TABLE IF EXISTS `roles`;
CREATE TABLE `roles` (
  `role_id` INT(4) UNSIGNED NOT NULL AUTO_INCREMENT,
  `role_name` VARCHAR(255)  NOT NULL,
  `role_perm_flags` BIGINT(8) UNSIGNED NOT NULL,
  `role_creator_user_id` int(10) unsigned not null,
  `role_last_edit` timestamp,
  PRIMARY KEY(`role_id`)
) ENGINE = MYISAM DEFAULT CHARSET=utf8;
insert into `roles` (`role_name`, `role_perm_flags`, `role_creator_user_id`) values
('None', '0', '1'),
('Guest', '1', '1' ),
('Viewer', '15', '1' ),
('Contributor', '2047', '1' ),
('Admin', '9223372036854779903', '1' );

DROP TABLE if exists `user_groups`;
CREATE TABLE if not exists `user_groups` (
  `user_id` INT(10)  NOT NULL,
  `group_id` INT(10)  NOT NULL,
  `last_edit` timestamp,
  UNIQUE(`user_id`, `group_id`)
) ENGINE = MYISAM DEFAULT CHARSET=utf8;

DROP TABLE if exists `permissions`;
DROP TABLE if exists `user_ldap_groups`;
DROP TABLE if exists `user_permissions`;

DROP TABLE if exists `pages`;
CREATE TABLE if not exists `pages` DEFAULT CHARSET=utf8 SELECT * FROM `cur`;
alter table `cur` rename to `cur_deprecated`;
alter table `pages` add primary key(`cur_id`);
alter table `pages` add unique name_title(`cur_namespace`, `cur_title`);
alter table `pages` add key `page_title`(`cur_title`(20));
alter table `pages` add KEY `page_timestamp` (`cur_timestamp`);
alter table `pages` add KEY `page_random` (`cur_random`);
alter table `pages` add KEY `name_title_timestamp` (`cur_namespace`,`cur_title`,`inverse_timestamp`);
alter table `pages` add KEY `user_timestamp` (`cur_user`,`inverse_timestamp`);
alter table `pages` add KEY `usertext_timestamp` (`inverse_timestamp`);
alter table `pages` add KEY `namespace_redirect_timestamp` (`cur_namespace`,`cur_is_redirect`,`cur_timestamp`);
alter table `pages` change `cur_id` `page_id` int(8) unsigned not null auto_increment;
alter table `pages` change `cur_namespace` `page_namespace` tinyint(2) unsigned default 0 not null;
alter table `pages` change `cur_title` `page_title` varchar(255) not null;
alter table `pages` change `cur_text` `page_text` mediumtext not null;
alter table `pages` change `cur_comment` `page_comment` blob not null;
alter table `pages` change `cur_user` `page_user_id` int(10) unsigned not null default 0;
alter table `pages` change `cur_timestamp` `page_timestamp` varchar(14) not null;
alter table `pages` change `cur_counter` `page_counter` bigint(20) unsigned not null default 0;
alter table `pages` change `cur_is_redirect` `page_is_redirect` tinyint(1) unsigned not null default 0;
alter table `pages` change `cur_minor_edit` `page_minor_edit` tinyint(1) unsigned not null default 0;
alter table `pages` change `cur_is_new` `page_is_new` tinyint(1) unsigned not null default 0;
alter table `pages` change `cur_random` `page_random` double unsigned not null default 0;
alter table `pages` change `cur_touched` `page_touched` varchar(14) not null;
alter table `pages` change `inverse_timestamp` `page_inverse_timestamp` varchar(14) not null;
alter table `pages` change `cur_usecache` `page_usecache` tinyint(1) unsigned not null default 1;
alter table `pages` change `cur_toc` `page_toc` blob not null;
alter table `pages` change `cur_tip` `page_tip` text not null;
alter table `pages` change `cur_parent` `page_parent` int(8) not null default 0;
alter table `pages` drop `cur_user_text`;
alter table `pages` drop `cur_restrictions`;
ALTER TABLE `pages` ADD COLUMN `page_restriction_id` INT(4) UNSIGNED NOT NULL;
update `pages` set `page_restriction_id` = '1';


DROP TABLE if exists `user_grants`;
CREATE TABLE if not exists `user_grants` (
  `user_grant_id` INT(10) UNSIGNED NOT NULL AUTO_INCREMENT,
  `page_id` INT(10) UNSIGNED NOT NULL,
  `user_id` INT(10) UNSIGNED NOT NULL,
  `role_id` INT(4) UNSIGNED NOT NULL,
  `creator_user_id` int(10) unsigned not null,
  `expire_date` datetime default NULL,
  `last_edit` timestamp,
  PRIMARY KEY(`user_grant_id`),
  UNIQUE(`page_id`, `user_id`)
) ENGINE = MYISAM DEFAULT CHARSET=utf8;


DROP TABLE if exists `group_grants`;
CREATE TABLE if not exists `group_grants` (
  `group_grant_id` INT(10) UNSIGNED NOT NULL AUTO_INCREMENT,
  `page_id` INT(10) UNSIGNED NOT NULL,
  `group_id` INT(10) UNSIGNED NOT NULL,
  `role_id` INT(4) UNSIGNED NOT NULL,
  `creator_user_id` int(10) unsigned not null,
  `expire_date` datetime default NULL,
  `last_edit` timestamp,
  PRIMARY KEY(`group_grant_id`),
  UNIQUE(`page_id`, `group_id`)
) ENGINE = MYISAM DEFAULT CHARSET=utf8;


DROP TABLE if exists `services`;
CREATE TABLE `services` (
  `service_id` INT(4) UNSIGNED NOT NULL AUTO_INCREMENT,
  `service_type` varchar(255) not null,
  `service_sid` varchar(255) not null,
  `service_uri` varchar(255),
  `service_description` mediumtext,
  `service_local` TINYINT(1) UNSIGNED NOT NULL DEFAULT 1,
  `service_enabled` tinyint(1) unsigned not null default 1,
  `service_last_status` text NULL,
  `service_last_edit` timestamp NOT NULL,
  PRIMARY KEY  (`service_id`)
) ENGINE = MYISAM DEFAULT CHARSET=utf8;


INSERT INTO `services` (`service_type`, `service_sid`, `service_uri`, `service_description`) VALUES 
('auth', 'http://services.mindtouch.com/deki/draft/2006/11/dekiwiki', 'http://localhost/@api/deki/', 'Local');

CREATE TABLE `service_config` (
    config_id INT(10) UNSIGNED NOT NULL AUTO_INCREMENT,
    service_id INT(4) UNSIGNED NOT NULL, 
    config_name CHAR(255) NOT NULL,
    config_value TEXT,
    PRIMARY KEY (config_id)
) DEFAULT CHARSET=utf8;

CREATE TABLE `service_prefs` (
    pref_id INT(10) UNSIGNED NOT NULL AUTO_INCREMENT,
    service_id INT(4) UNSIGNED NOT NULL, 
    pref_name CHAR(255) NOT NULL,
    pref_value TEXT,
    PRIMARY KEY (pref_id)
) DEFAULT CHARSET=utf8;


DROP TABLE if exists `services`;
CREATE TABLE `services` (
  `service_id` INT(4) UNSIGNED NOT NULL AUTO_INCREMENT,
  `service_type_id` INT(4) UNSIGNED NOT NULL,
  `service_description` mediumtext,
  `service_config` mediumtext,
  PRIMARY KEY(`service_id`)
)
ENGINE = MYISAM;

ALTER TABLE `users` ADD COLUMN `user_service_id` INT(4) UNSIGNED NOT NULL;

INSERT INTO `services` (`service_type_id`, `service_description`, `service_config`) VALUES ('1', 'Local', NULL);
INSERT INTO `services` (`service_type_id`, `service_description`, `service_config`) VALUES ('1', 'LDAP', NULL);

UPDATE `users` SET `user_service_id` = '1';
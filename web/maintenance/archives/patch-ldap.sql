--
-- MindTouch: ldap schema updates
-- Jan 2006
--
CREATE TABLE `groups` (
	`group_id` INT UNSIGNED NOT NULL AUTO_INCREMENT PRIMARY KEY,
	`group_name` VARCHAR(255) NOT NULL UNIQUE,
	`group_dn` MEDIUMTEXT NOT NULL,
	`group_permission` INT UNSIGNED NOT NULL);

CREATE TABLE `permissions` (
	`permission_id` INT UNSIGNED NOT NULL AUTO_INCREMENT PRIMARY KEY,
	`permission_name` VARCHAR(255) NOT NULL UNIQUE,
	`permission_user_only` tinyint(1) DEFAULT 0);

INSERT INTO `permissions` VALUES ('', 'Administrator', '0');
INSERT INTO `permissions` VALUES ('', 'Contributor', '0');
INSERT INTO `permissions` VALUES ('', 'Viewer', '0');
INSERT INTO `permissions` VALUES ('', 'Guest', '0');
INSERT INTO `permissions` VALUES ('', 'Deactivated', '0');

CREATE TABLE `user_ldap_groups` (
	`user_id` INT UNSIGNED NOT NULL,
	`group_id` INT UNSIGNED NOT NULL,
    	UNIQUE(user_id, group_id));

CREATE TABLE  user_permissions (user_id INT UNSIGNED NOT NULL PRIMARY KEY, permission_id INT UNSIGNED NOT NULL);

ALTER TABLE `user` ADD user_dn MEDIUMTEXT;

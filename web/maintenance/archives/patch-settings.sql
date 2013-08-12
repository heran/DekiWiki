DROP TABLE IF EXISTS `settings`;

CREATE TABLE `config` (
	`config_id` int unsigned NOT NULL auto_increment,
	`config_key` varchar(255) NOT NULL,
	`config_value` text NOT NULL,
PRIMARY KEY  (`config_id`),
KEY `config_key` (`config_key`)
) ENGINE=MyISAM DEFAULT CHARSET=utf8 ; 
insert into `config` (`config_key`, `config_value`) values('storage/s3/prefix', "");
insert into `config` (`config_key`, `config_value`) values('storage/s3/bucket', "");
insert into `config` (`config_key`, `config_value`) values('storage/s3/privatekey', "");
insert into `config` (`config_key`, `config_value`) values('storage/s3/publickey', "");
insert into `config` (`config_key`, `config_value`) values('storage/type', 'fs');
insert into `config` (`config_key`, `config_value`) values('storage/s3/timeout', '');
insert into `config` (`config_key`, `config_value`) values('files/max-file-size','268435456');
insert into `config` (`config_key`, `config_value`) values('files/blocked-extensions','exe, vbs, scr, reg, bat, com');
insert into `config` (`config_key`, `config_value`) values('files/imagemagick-extensions','bmp, jpg, jpeg, png, gif, tiff');
insert into `config` (`config_key`, `config_value`) values('files/imagemagick-max-size','2000000');
insert into `config` (`config_key`, `config_value`) values('ui/banned-words',"");
insert into `config` (`config_key`, `config_value`) values('ui/sitename','DekiWiki (Hayes)');
insert into `config` (`config_key`, `config_value`) values('ui/language','en-us');
insert into `config` (`config_key`, `config_value`) values('admin/smtp-server','localhost');


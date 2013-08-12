CREATE TABLE `tag_map` (
`tagmap_id` INT UNSIGNED NOT NULL AUTO_INCREMENT PRIMARY KEY ,
`tagmap_page_id` INT UNSIGNED NOT NULL ,
`tagmap_tag_id` INT(4) UNSIGNED NOT NULL ,
INDEX ( `tagmap_page_id` , `tagmap_tag_id` )
) ENGINE=MYISAM DEFAULT CHARSET=utf8;
 
CREATE TABLE `tags` (
  `tag_id` INT(4) unsigned NOT NULL AUTO_INCREMENT PRIMARY KEY ,
  `tag_name` varchar(255) NOT NULL default '',
  INDEX (`tag_name`)
) ENGINE=MYISAM DEFAULT CHARSET=utf8;

CREATE TABLE `resourcerevs` (                                      
    `resrev_id`                 int(4) unsigned NOT NULL auto_increment,                       
    `resrev_res_id`             int(4) unsigned NOT NULL default '0',                  
    `resrev_rev`                int(4) unsigned NOT NULL default '0',                          
    `resrev_user_id`            int(4) unsigned NOT NULL default '0',
    `resrev_parent_id`          int(4) unsigned default NULL,
    `resrev_parent_page_id`     int(4) unsigned default NULL,
    `resrev_parent_user_id`     int(4) unsigned default NULL,
    `resrev_change_mask`        smallint(2) unsigned NOT NULL default '0',                 
    `resrev_name`               varchar(255) NOT NULL default '',             
    `resrev_change_description` varchar(255) default NULL,             
    `resrev_timestamp`          datetime NOT NULL default '0001-01-01 00:00:00',
    `resrev_content_id`         int(4) unsigned NOT NULL default '0',
    `resrev_deleted`            tinyint(1) unsigned NOT NULL default '0',  
    `resrev_changeset_id`	    int(4) unsigned NOT NULL default '0',
    `resrev_size`               int(4) unsigned NOT NULL default '0',                     
    `resrev_mimetype`           varchar(255) NOT NULL default '',                    
    `resrev_language`           varchar(255) default NULL,
    `resrev_meta`	            text default NULL,
    PRIMARY KEY (`resrev_id`),
    UNIQUE KEY  `resid_rev` (`resrev_res_id`,`resrev_rev`)
  ) ENGINE=MyISAM DEFAULT CHARSET=utf8 COLLATE utf8_general_ci;

--
-- Table structure for table `resources`
--

CREATE TABLE `resources` (                                      
  `res_id`                      int(4) unsigned NOT NULL auto_increment,
  `res_headrev`                 int(4) unsigned NOT NULL default '0',
  `res_type`                    tinyint(1) unsigned NOT NULL default '0',
  `res_deleted`                 tinyint(1) unsigned NOT NULL default '0',
  `res_create_timestamp`        datetime NOT NULL default '0001-01-01 00:00:00',
  `res_update_timestamp`        datetime NOT NULL default '0001-01-01 00:00:00',
  `res_create_user_id`          int(4) unsigned NOT NULL default '0',
  `res_update_user_id`          int(4) unsigned NOT NULL default '0',
  `resrev_rev`                  int(4) unsigned NOT NULL default '0',                          
  `resrev_user_id`              int(4) unsigned NOT NULL default '0',                  
  `resrev_parent_id`            int(4) unsigned default NULL,                  
  `resrev_parent_page_id`       int(4) unsigned default NULL,
  `resrev_parent_user_id`       int(4) unsigned default NULL,
  `resrev_change_mask`          smallint(2) unsigned NOT NULL default '0',                 
  `resrev_name`                 varchar(255) NOT NULL default '',             
  `resrev_change_description`   varchar(255) default NULL,             
  `resrev_timestamp`            datetime NOT NULL default '0001-01-01 00:00:00',
  `resrev_content_id`           int(4) unsigned NOT NULL default '0' ,
  `resrev_deleted`              tinyint(1) unsigned NOT NULL default '0',  
  `resrev_changeset_id`         int(4) unsigned NOT NULL default '0',
  `resrev_size`                 int(4) unsigned NOT NULL default '0',                     
  `resrev_mimetype`             varchar(255) NOT NULL default '',                    
  `resrev_language`             varchar(255) default NULL,
  `resrev_meta`					text default NULL,
  PRIMARY KEY  (`res_id`),
  KEY `changeset` (`resrev_changeset_id`),
  KEY `parent_resource` (`resrev_parent_id`),
  KEY `parent_page` (`resrev_parent_page_id`),
  KEY `parent_user` (`resrev_parent_user_id`)
) ENGINE=MyISAM DEFAULT CHARSET=utf8 COLLATE utf8_general_ci;

--
-- Table structure for table `resourcecontents`
--
CREATE TABLE `resourcecontents` (                                            
  `rescontent_id`               int(4) unsigned NOT NULL auto_increment,                   
  `rescontent_res_id`           int(4) unsigned default NULL,                              
  `rescontent_res_rev`          int(4) unsigned default NULL,                        
  `rescontent_value`            mediumblob,
  `rescontent_mimetype`         varchar(255) NOT NULL default '',
  `rescontent_size`             int(4) unsigned NOT NULL default '0',
  `rescontent_location`         varchar(255) default NULL,                           
  PRIMARY KEY  (`rescontent_id`),                                            
  UNIQUE KEY `rescontent_res_id` (`rescontent_res_id`,`rescontent_res_rev`)  
) ENGINE=MyISAM DEFAULT CHARSET=utf8 COLLATE utf8_general_ci;

CREATE TABLE `resourcefilemap` (
	`file_id` int(10) unsigned NOT NULL auto_increment,
	`resource_id` int(10) unsigned default NULL,
	PRIMARY KEY (`file_id`),
	UNIQUE KEY `entity_id` (`resource_id`,`file_id`)
) ENGINE=MyISAM DEFAULT CHARSET=utf8 COLLATE utf8_general_ci;

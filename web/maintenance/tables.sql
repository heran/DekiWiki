
--
-- Table structure for table `archive`
--
CREATE TABLE `archive` (
  `ar_id` int(4) unsigned NOT NULL auto_increment,
  `ar_namespace` tinyint(2) unsigned NOT NULL default '0',
  `ar_title` varchar(255) NOT NULL default '',
  `ar_text` mediumtext NOT NULL,
  `ar_comment` tinyblob NOT NULL,
  `ar_user` int(5) unsigned NOT NULL default '0',
  `ar_timestamp` varchar(14) NOT NULL default '',
  `ar_minor_edit` tinyint(1) NOT NULL default '0',
  `ar_last_page_id` int(8) unsigned NOT NULL default '0',
  `ar_old_id` int(8) unsigned NOT NULL default '0',
  `ar_content_type` VARCHAR( 255 ) NOT NULL DEFAULT 'application/x.deki-text',
  `ar_language` VARCHAR( 10 ) NOT NULL default '',  
  `ar_display_name` VARCHAR( 255 ) DEFAULT NULL,  
  `ar_transaction_id` int(4) unsigned  NOT NULL default '0',
  `ar_is_hidden` tinyint UNSIGNED NOT NULL DEFAULT '0',
  `ar_meta` text NULL,
  `ar_revision` int(8) unsigned NOT NULL default '0',
  PRIMARY KEY  (`ar_id`),
  KEY `name_title_timestamp` (`ar_namespace`,`ar_title`,`ar_timestamp`),
  KEY `ar_last_page_id` (`ar_last_page_id`), 
  KEY `ar_transaction_id` (`ar_transaction_id`)
) ENGINE=MyISAM DEFAULT CHARSET=utf8 COLLATE utf8_general_ci;

--
-- Dumping data for table `archive`
--

--
-- Table structure for table `bans`
--
CREATE TABLE `bans` (                                  
          `ban_id` int(4) unsigned NOT NULL auto_increment,    
          `ban_by_user_id` int(4) unsigned NOT NULL,           
          `ban_expires` datetime default NULL,                 
          `ban_reason` text,                                   
          `ban_revokemask` bigint(8) unsigned NOT NULL,        
          `ban_last_edit` datetime default NULL,               
          PRIMARY KEY  (`ban_id`)                              
        ) ENGINE=MyISAM DEFAULT CHARSET=utf8 COLLATE utf8_general_ci;
        
--
-- Table structure for table `banips`
--    
CREATE TABLE `banips` (                                 
          `banip_id` int(10) unsigned NOT NULL auto_increment,  
          `banip_ipaddress` varchar(15) default NULL,           
          `banip_ban_id` int(4) unsigned NOT NULL,              
          PRIMARY KEY  (`banip_id`),                            
          KEY `banip_ipaddress` (`banip_ipaddress`)             
        ) ENGINE=MyISAM DEFAULT CHARSET=utf8 COLLATE utf8_general_ci;  
--
-- Table structure for table `banusers`
--        
CREATE TABLE `banusers` (                                
            `banuser_id` int(4) unsigned NOT NULL auto_increment,  
            `banuser_user_id` int(4) unsigned NOT NULL,            
            `banuser_ban_id` int(4) unsigned NOT NULL,             
            UNIQUE KEY `banuser_id` (`banuser_id`),                
            KEY `banuser_user_id` (`banuser_user_id`)              
          ) ENGINE=MyISAM DEFAULT CHARSET=utf8 COLLATE utf8_general_ci;
          
--
-- Table structure for table `brokenlinks`
--

CREATE TABLE `brokenlinks` (
  `bl_from` int(8) unsigned NOT NULL default '0',
  `bl_to` varchar(255) NOT NULL default '',
  UNIQUE KEY `bl_from` (`bl_from`,`bl_to`),
  KEY `bl_to` (`bl_to`)
) ENGINE=MyISAM DEFAULT CHARSET=utf8 COLLATE utf8_general_ci;


--
-- Table structure for table `comments`
--
CREATE TABLE `comments` (                                           
	`cmnt_id` int(8) unsigned NOT NULL auto_increment,                
	`cmnt_page_id` int(8) unsigned NOT NULL,                          
	`cmnt_number` int(2) unsigned NOT NULL,                           
	`cmnt_poster_user_id` int(4) unsigned NOT NULL,                   
	`cmnt_create_date` timestamp NOT NULL default CURRENT_TIMESTAMP,  
	`cmnt_last_edit` timestamp NULL default NULL,                     
	`cmnt_last_edit_user_id` int(4) unsigned default NULL,            
	`cmnt_content` text NOT NULL,                                     
	`cmnt_content_mimetype` varchar(25) NOT NULL,                     
	`cmnt_title` varchar(50) default NULL,                            
	`cmnt_deleter_user_id` int(8) unsigned default NULL,              
	`cmnt_delete_date` timestamp NULL default NULL,                   
PRIMARY KEY  (`cmnt_id`),                                         
UNIQUE KEY `pageid_number` (`cmnt_page_id`,`cmnt_number`),
KEY `cmnt_poster_user_id` (`cmnt_poster_user_id`) 
) ENGINE=MyISAM DEFAULT CHARSET=utf8 COLLATE utf8_general_ci;

--
-- Table structure for table `config`
--
CREATE TABLE `config` (                                             
          `config_id` int unsigned NOT NULL auto_increment,             
          `config_key` varchar(255) NOT NULL,                               
          `config_value` text NOT NULL,                                                      
          PRIMARY KEY  (`config_id`),                                       
          KEY `config_key` (`config_key`)                                   
        ) ENGINE=MyISAM DEFAULT CHARSET=utf8;

--
-- Table structure for table `group_grants`
--
CREATE TABLE `group_grants` (
  `group_grant_id` INT(10) UNSIGNED NOT NULL AUTO_INCREMENT,
  `page_id` INT(10) UNSIGNED NOT NULL,
  `group_id` INT(10) UNSIGNED NOT NULL,
  `role_id` INT(4) UNSIGNED NOT NULL,
  `creator_user_id` int(10) unsigned not null,
  `expire_date` datetime default NULL,
  `last_edit` timestamp,
  PRIMARY KEY  (`group_grant_id`),
  UNIQUE(`page_id`, `group_id`)
) ENGINE = MYISAM DEFAULT CHARSET=utf8 COLLATE utf8_general_ci;

--
-- Table structure for table `groups`
--

CREATE TABLE `groups` (
  `group_id` INT(10) UNSIGNED NOT NULL AUTO_INCREMENT,
  `group_name` VARCHAR(255)  NOT NULL,
  `group_role_id` INT(4) UNSIGNED NOT NULL,
  `group_service_id` int(4) unsigned not null,
  `group_creator_user_id` int(10) unsigned not null default 0,
  `group_last_edit` timestamp,
  PRIMARY KEY  (`group_id`),
  UNIQUE KEY `group_name` (`group_name`, `group_service_id`)
) ENGINE = MYISAM DEFAULT CHARSET=utf8 COLLATE utf8_general_ci;

--
-- Table structure for table `links`
--

CREATE TABLE `links` (
  `l_from` int(8) unsigned NOT NULL default '0',
  `l_to` int(8) unsigned NOT NULL default '0',
  UNIQUE KEY `l_from` (`l_from`,`l_to`),
  KEY `l_to` (`l_to`)
) ENGINE=MyISAM DEFAULT CHARSET=utf8 COLLATE utf8_general_ci;

--
-- Table structure for table `old`
--

CREATE TABLE `old` (
  `old_id` int(8) unsigned NOT NULL auto_increment,
  `old_text` mediumtext NOT NULL,
  `old_comment` tinyblob NOT NULL,
  `old_user` int(5) unsigned NOT NULL default '0',
  `old_timestamp` varchar(14) NOT NULL default '',
  `old_minor_edit` tinyint(1) NOT NULL default '0',
  `old_content_type` varchar(255) NOT NULL default 'application/x.deki-text',
  `old_language` VARCHAR( 10 ) NOT NULL default '',
  `old_display_name` VARCHAR(255) NOT NULL DEFAULT '',
  `old_is_hidden` tinyint UNSIGNED NOT NULL DEFAULT '0',
  `old_meta` text NULL,
  `old_revision` int(8) unsigned NOT NULL default '0',
  `old_page_id` int(8) unsigned NOT NULL default '0',
  PRIMARY KEY  (`old_id`),
  UNIQUE KEY `old_page` (`old_page_id`,`old_revision`), 
  KEY `old_user` (`old_user`)
) ENGINE=MyISAM DEFAULT CHARSET=utf8 COLLATE utf8_general_ci;

--
-- Table structure for table `pages`
--

CREATE TABLE `pages` (
  `page_id` int(8) unsigned NOT NULL auto_increment,
  `page_namespace` tinyint(2) unsigned default 0 not null,
  `page_title` varchar(255) NOT NULL,
  `page_text` mediumtext NOT NULL,
  `page_comment` blob NOT NULL,
  `page_user_id` int(10) unsigned not null default 0,
  `page_timestamp` varchar(14) NOT NULL,
  `page_counter` bigint(20) unsigned not null default 0,
  `page_is_redirect` tinyint(1) unsigned not null default 0,
  `page_minor_edit` tinyint(1) unsigned not null default 0,
  `page_is_new` tinyint(1) unsigned not null default 0,
  `page_touched` varchar(14) NOT NULL,
  `page_usecache` tinyint(1) unsigned not null default 1,
  `page_toc` blob NOT NULL,
  `page_tip` text NOT NULL,
  `page_parent` int(8) not null default 0,
  `page_restriction_id` int(4) unsigned NOT NULL,
  `page_content_type` varchar(255) NOT NULL default 'application/x.deki-text',
  `page_language` VARCHAR( 10 ) NOT NULL default '', 
  `page_display_name` VARCHAR(255) NOT NULL, 
  `page_template_id` int(8) unsigned default NULL,
  `page_is_hidden` tinyint unsigned NOT NULL DEFAULT '0',
  `page_meta` text NULL,
  `page_revision` int(8) unsigned NOT NULL default '1',
  `page_etag` varchar(32) NOT NULL default '',
  PRIMARY KEY  (`page_id`),
  UNIQUE KEY `name_title` (`page_namespace`,`page_title`),
  KEY `page_title` (`page_title`(20)),
  KEY `page_timestamp` (`page_timestamp`),
  KEY `page_parent` (`page_parent`),
  KEY `page_user_id` (`page_user_id`),
  KEY `namespace_redirect_timestamp` (`page_namespace`,`page_is_redirect`,`page_timestamp`)
) ENGINE=MyISAM DEFAULT CHARSET=utf8 COLLATE utf8_general_ci;

--
-- Table structure for table `page_viewcount`
--

CREATE TABLE `page_viewcount` (
  `page_id` int(8) unsigned NOT NULL,
  `page_counter` bigint(20) unsigned not null default 0,
  PRIMARY KEY  (`page_id`)
) ENGINE=MyISAM DEFAULT CHARSET=utf8 COLLATE utf8_general_ci;

--
-- Table structure for table `pagesub`
--
CREATE TABLE pagesub (
  pagesub_page_id int(8) unsigned not null,
  pagesub_user_id int(10) unsigned not null,
  pagesub_child_pages tinyint unsigned not null default '0',
  primary key (pagesub_page_id, pagesub_user_id)
) ENGINE=MyISAM DEFAULT CHARSET=utf8 COLLATE utf8_general_ci;

--
-- Table structure for table `query_log`
--
CREATE TABLE `query_log` (
  query_id int(10) unsigned NOT NULL auto_increment,
  ref_query_id int(10) unsigned,
  last_result_id int(10) unsigned,
  created datetime NOT NULL,
  raw varchar(1000) NOT NULL,
  sorted_terms varchar(1000) NOT NULL,
  sorted_terms_hash char(32) NOT NULL,
  parsed varchar(2000),
  user_id int(10) unsigned NOT NULL,
  result_count int(8) unsigned NOT NULL,
  PRIMARY KEY(query_id),
  KEY created (created),
  KEY sorted_terms (sorted_terms(100)),
  KEY last_result (last_result_id),
  KEY sorted_terms_hash (sorted_terms_hash)
) ENGINE=MyISAM DEFAULT CHARSET=utf8 COLLATE utf8_general_ci;

--
-- Table structure for table `query_result_popularity`
--
CREATE TABLE `query_result_popularity` (
  sorted_terms_hash char(32) NOT NULL,
  type tinyint unsigned NOT NULL,
  type_id int(8) unsigned NOT NULL,
  selection_count int(8) unsigned,
  PRIMARY KEY(sorted_terms_hash,type,type_id)
) ENGINE=MyISAM DEFAULT CHARSET=utf8 COLLATE utf8_general_ci;

--
-- Table structure for table `query_result_log`
--
CREATE TABLE `query_result_log` (
  query_result_id  int(10) unsigned NOT NULL auto_increment,
  query_id int(10) unsigned NOT NULL,
  created datetime NOT NULL,
  result_position int(2) unsigned NOT NULL,
  result_rank double unsigned not NULL,
  page_id int(8) unsigned NOT NULL,
  type tinyint unsigned NOT NULL,
  type_id int(8) unsigned NOT NULL,
  PRIMARY KEY(query_result_id),
  KEY query_id (query_id)
) ENGINE=MyISAM DEFAULT CHARSET=utf8 COLLATE utf8_general_ci;

--
-- Table structure for table `query_terms`
--
CREATE TABLE `query_terms` (
   query_term_id int(10) unsigned NOT NULL auto_increment primary key,
   query_term varchar(100) NOT NULL UNIQUE
) ENGINE=MyISAM DEFAULT CHARSET=utf8 COLLATE utf8_general_ci;

--
-- Table structure for table `query_term_map`
--
CREATE TABLE `query_term_map` (
    query_term_id int(10) unsigned NOT NULL,
    query_id int(10) unsigned NOT NULL,
    UNIQUE KEY term_query (query_term_id, query_id)
) ENGINE=MyISAM DEFAULT CHARSET=utf8 COLLATE utf8_general_ci;

--
-- Table structure for table `ratings`
--
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

--
-- Table structure for table `ratingscomputed`
--
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

--
-- Table structure for table `resourcerevs`
--

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
    `resrev_is_hidden`		tinyint unsigned NOT NULL default '0',
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
  `resrev_is_hidden`		tinyint unsigned NOT NULL default '0',
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

--
-- Table structure for table `recentchanges`
--

CREATE TABLE `recentchanges` (
  `rc_id` int(8) NOT NULL auto_increment,
  `rc_timestamp` varchar(14) NOT NULL default '',
  `rc_cur_time` varchar(14) NOT NULL default '',
  `rc_user` int(10) unsigned NOT NULL default '0',
  `rc_namespace` tinyint(3) unsigned NOT NULL default '0',
  `rc_title` varchar(255) NOT NULL default '',
  `rc_comment` varchar(255) NOT NULL default '',
  `rc_minor` tinyint(3) unsigned NOT NULL default '0',
  `rc_bot` tinyint(3) unsigned NOT NULL default '0',
  `rc_new` tinyint(3) unsigned NOT NULL default '0',
  `rc_cur_id` int(10) unsigned NOT NULL default '0',
  `rc_this_oldid` int(10) unsigned NOT NULL default '0',
  `rc_last_oldid` int(10) unsigned NOT NULL default '0',
  `rc_type` tinyint(3) unsigned NOT NULL default '0',
  `rc_moved_to_ns` tinyint(3) unsigned NOT NULL default '0',
  `rc_moved_to_title` varchar(255) NOT NULL default '',
  `rc_patrolled` tinyint(3) unsigned NOT NULL default '0',
  `rc_ip` varchar(15) NOT NULL default '',
  `rc_transaction_id` int(10) unsigned NOT NULL default '0',
  PRIMARY KEY  (`rc_id`),
  KEY `rc_timestamp` (`rc_timestamp`),
  KEY `rc_namespace_title` (`rc_namespace`,`rc_title`),
  KEY `rc_cur_id` (`rc_cur_id`),
  KEY `new_name_timestamp` (`rc_new`,`rc_namespace`,`rc_timestamp`),
  KEY `rc_ip` (`rc_ip`),
  KEY `rc_transaction_id` (`rc_transaction_id`),
  KEY `rc_user` (`rc_user`)
) ENGINE=MyISAM DEFAULT CHARSET=utf8 COLLATE utf8_general_ci;

--
-- Table structure for table `resourcefilemap`
--

CREATE TABLE `resourcefilemap` (
	`file_id` int(10) unsigned NOT NULL auto_increment,
	`resource_id` int(10) unsigned default NULL,
	PRIMARY KEY (`file_id`),
	UNIQUE KEY `entity_id` (`resource_id`,`file_id`)
) ENGINE=MyISAM DEFAULT CHARSET=utf8 COLLATE utf8_general_ci;

--
-- Table structure for table `transactions`
--

CREATE TABLE `transactions` (                            
    `t_id` int(4) NOT NULL auto_increment,                 
    `t_timestamp` datetime NOT NULL,                       
    `t_user_id` int(4) NOT NULL,                           
    `t_page_id` int(4) unsigned default NULL,              
    `t_title` varchar(255) default NULL,                   
    `t_namespace` tinyint(2) unsigned default NULL,                 
    `t_type` tinyint(2) default NULL,                      
    `t_reverted` tinyint(1) NOT NULL default '0',          
    `t_revert_user_id` int(4) unsigned default NULL,       
    `t_revert_timestamp` datetime default NULL,            
    `t_revert_reason` varchar(255) default NULL,           
    PRIMARY KEY  (`t_id`),                                 
    KEY `t_timestamp` (`t_timestamp`)                      
  ) ENGINE=MyISAM DEFAULT CHARSET=utf8 COLLATE utf8_general_ci;

--
-- Table structure for table `requestlog`
--

CREATE TABLE `requestlog` (
 `rl_id` int(4) unsigned NOT NULL auto_increment,             
 `rl_servicehost` varchar(64) NOT NULL,           
 `rl_requesthost` varchar(64) NOT NULL,
 `rl_requesthostheader` varchar(64) NOT NULL,                                  
 `rl_requestpath` varchar(512) NOT NULL,                                 
 `rl_requestparams` varchar(512) default NULL,                           
 `rl_requestverb` varchar(8) NOT NULL,                                   
 `rl_dekiuser` varchar(32) default NULL,                              
 `rl_origin` varchar(64) NOT NULL,                                                                     
 `rl_servicefeature` varchar(128) NOT NULL,                              
 `rl_responsestatus` varchar(8) NOT NULL,                                
 `rl_executiontime` int(4) unsigned default NULL,                        
 `rl_response` varchar(2048) default NULL,                              
 `rl_timestamp` timestamp NOT NULL default CURRENT_TIMESTAMP,            
 PRIMARY KEY  (`rl_id`)                                                  
) ENGINE=MyISAM DEFAULT CHARSET=utf8 COLLATE utf8_general_ci ROW_FORMAT=DYNAMIC;

--
-- Table structure for table `requeststats`
--

CREATE TABLE `requeststats` (
 `rs_id` int(4) unsigned NOT NULL auto_increment,                                         
 `rs_numrequests` int(4) unsigned NOT NULL,                                               
 `rs_servicehost` varchar(64) NOT NULL,                                                   
 `rs_requestverb` varchar(8) NOT NULL,                                                    
 `rs_servicefeature` varchar(128) NOT NULL,                                               
 `rs_responsestatus` varchar(8) NOT NULL,                                                 
 `rs_exec_avg` int(4) unsigned NOT NULL,                                                  
 `rs_exec_std` int(4) unsigned NOT NULL,                                                  
 `rs_ts_start` timestamp NOT NULL default CURRENT_TIMESTAMP on update CURRENT_TIMESTAMP,  
 `rs_ts_length` int(4) unsigned NOT NULL,                                                 
 PRIMARY KEY  (`rs_id`)                                                                   
) ENGINE=MyISAM DEFAULT CHARSET=utf8 COLLATE utf8_general_ci ROW_FORMAT=DYNAMIC;

--
-- Table structure for table `restrictions`
--

CREATE TABLE `restrictions` (
  `restriction_id` INT(4) UNSIGNED NOT NULL AUTO_INCREMENT,
  `restriction_name` VARCHAR(255)  NOT NULL,
  `restriction_perm_flags` BIGINT UNSIGNED NOT NULL,
  `restriction_creator_user_id` int(10) unsigned not null,
  `restriction_last_edit` timestamp, 
  PRIMARY KEY  (`restriction_id`)
) ENGINE = MYISAM DEFAULT CHARSET=utf8 COLLATE utf8_general_ci;


--
-- Table structure for table `roles`
--

CREATE TABLE `roles` (
  `role_id` INT(4) UNSIGNED NOT NULL AUTO_INCREMENT,
  `role_name` VARCHAR(255)  NOT NULL,
  `role_perm_flags` BIGINT(8) UNSIGNED NOT NULL,
  `role_creator_user_id` int(10) unsigned not null,
  `role_last_edit` timestamp,
  PRIMARY KEY  (`role_id`)
) ENGINE = MYISAM DEFAULT CHARSET=utf8 COLLATE utf8_general_ci;

--
-- Table structure for table `services`
--

CREATE TABLE `services` (
  `service_id` INT(4) UNSIGNED NOT NULL AUTO_INCREMENT,
  `service_type` varchar(255) not null,
  `service_sid` varchar(255),
  `service_uri` varchar(255),
  `service_description` mediumtext,
  `service_local` TINYINT(1) UNSIGNED NOT NULL DEFAULT 1,
  `service_enabled` tinyint(1) unsigned not null default 1,
  `service_last_status` text NULL,
  `service_last_edit` timestamp NOT NULL,
  PRIMARY KEY  (`service_id`)
) ENGINE = MYISAM DEFAULT CHARSET=utf8 COLLATE utf8_general_ci;

--
-- Table structure for table `service_config`
--

CREATE TABLE `service_config` (
    config_id INT(10) UNSIGNED NOT NULL AUTO_INCREMENT,
    service_id INT(4) UNSIGNED NOT NULL, 
    config_name CHAR(255) NOT NULL,
    config_value TEXT,
    PRIMARY KEY (config_id)
) ENGINE = MYISAM DEFAULT CHARSET=utf8 COLLATE utf8_general_ci;

--
-- Table structure for table `service_prefs`
--

CREATE TABLE `service_prefs` (
    pref_id INT(10) UNSIGNED NOT NULL AUTO_INCREMENT,
    service_id INT(4) UNSIGNED NOT NULL, 
    pref_name CHAR(255) NOT NULL,
    pref_value TEXT,
    PRIMARY KEY (pref_id)
) ENGINE = MYISAM DEFAULT CHARSET=utf8 COLLATE utf8_general_ci;


--
-- Table structure for table `tag_map`
--

CREATE TABLE `tag_map` (
	`tagmap_id` INT UNSIGNED NOT NULL AUTO_INCREMENT,
	`tagmap_page_id` INT UNSIGNED NOT NULL ,
	`tagmap_tag_id` INT(4) UNSIGNED NOT NULL ,
	PRIMARY KEY  (`tagmap_id`),
	UNIQUE KEY `tagmap_page_id` (`tagmap_page_id`, `tagmap_tag_id`),
	KEY `tagmap_tag_id` (`tagmap_tag_id`)
) ENGINE = MYISAM DEFAULT CHARSET=utf8 COLLATE utf8_general_ci;

--
-- Table structure for table `tags`
--

CREATE TABLE `tags` (
  `tag_id` INT(4) unsigned NOT NULL AUTO_INCREMENT,
  `tag_name` varchar(255) NOT NULL default '',
  `tag_type` tinyint(2) unsigned NOT NULL default '0',
  PRIMARY KEY  (`tag_id`),
  UNIQUE KEY `tag_name` (`tag_name`,`tag_type`)
) ENGINE = MYISAM DEFAULT CHARSET=utf8 COLLATE utf8_general_ci;


--
-- Table structure for table `user_grants`
--

CREATE TABLE `user_grants` (
  `user_grant_id` INT(10) UNSIGNED NOT NULL AUTO_INCREMENT,
  `page_id` INT(10) UNSIGNED NOT NULL,
  `user_id` INT(10) UNSIGNED NOT NULL,
  `role_id` INT(4) UNSIGNED NOT NULL,
  `creator_user_id` int(10) unsigned not null,
  `expire_date` datetime default NULL,
  `last_edit` timestamp,
  PRIMARY KEY  (`user_grant_id`),
  UNIQUE(`page_id`, `user_id`)
) ENGINE = MYISAM DEFAULT CHARSET=utf8 COLLATE utf8_general_ci;

--
-- Table structure for table `user_groups`
--

CREATE TABLE `user_groups` (
  `user_id` INT(10) NOT NULL,
  `group_id` INT(10) NOT NULL,
  `last_edit` timestamp,
  UNIQUE(`user_id`, `group_id`)
) ENGINE = MYISAM DEFAULT CHARSET=utf8 COLLATE utf8_general_ci;


--
-- Table structure for table `users`
--

CREATE TABLE `users` (
  `user_id` int(10) unsigned NOT NULL auto_increment,
  `user_name` varchar(255) NOT NULL,
  `user_real_name` varchar(255) default NULL,
  `user_password` tinyblob NOT NULL,
  `user_newpassword` tinyblob NOT NULL,
  `user_email` varchar(255) default NULL,
  `user_touched` varchar(14) NOT NULL default '',
  `user_token` varchar(32) NOT NULL default '',
  `user_role_id` int(4) unsigned not null,
  `user_active` tinyint(1) unsigned NOT NULL,
  `user_external_name` varchar(255) default NULL,
  `user_service_id` int(4) unsigned NOT NULL,
  `user_builtin` tinyint(1) unsigned NOT NULL default 0,
  `user_create_timestamp` datetime NOT NULL default '0001-01-01 00:00:00',
  `user_language` varchar(255) default NULL,
  `user_timezone` varchar(255) default NULL,
  `user_seat` tinyint(1) NOT NULL default 0,
  PRIMARY KEY  (`user_id`),
  UNIQUE KEY `user_real_name_service_id` (`user_external_name`, `user_service_id`),
  UNIQUE KEY `user_name` (`user_name`, `user_service_id`)
) ENGINE=MyISAM DEFAULT CHARSET=utf8 COLLATE utf8_general_ci;

--
-- Table structure for table `watchlist`
--

CREATE TABLE `watchlist` (
  `wl_user` int(5) unsigned NOT NULL default '0',
  `wl_namespace` tinyint(2) unsigned NOT NULL default '0',
  `wl_title` varchar(255) NOT NULL default '',
  UNIQUE KEY `wl_user` (`wl_user`,`wl_namespace`,`wl_title`),
  KEY `namespace_title` (`wl_namespace`,`wl_title`)
) ENGINE=MyISAM DEFAULT CHARSET=utf8 COLLATE utf8_general_ci;

-- Dump completed on 2007-02-17  0:27:06

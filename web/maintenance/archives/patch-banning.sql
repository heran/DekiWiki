CREATE TABLE `bans` (                                  
          `ban_id` int(4) unsigned NOT NULL auto_increment,    
          `ban_by_user_id` int(4) unsigned NOT NULL,           
          `ban_expires` datetime default NULL,                 
          `ban_reason` text,                                   
          `ban_revokemask` bigint(8) unsigned NOT NULL,        
          `ban_last_edit` datetime default NULL,               
          PRIMARY KEY  (`ban_id`)                              
        ) ENGINE=MyISAM DEFAULT CHARSET=utf8 COLLATE utf8_general_ci;
        
CREATE TABLE `banips` (                                 
          `banip_id` int(10) unsigned NOT NULL auto_increment,  
          `banip_ipaddress` varchar(15) default NULL,           
          `banip_ban_id` int(4) unsigned NOT NULL,              
          PRIMARY KEY  (`banip_id`),                            
          KEY `banip_ipaddress` (`banip_ipaddress`)             
        ) ENGINE=MyISAM DEFAULT CHARSET=utf8 COLLATE utf8_general_ci;  
        
CREATE TABLE `banusers` (                                
            `banuser_id` int(4) unsigned NOT NULL auto_increment,  
            `banuser_user_id` int(4) unsigned NOT NULL,            
            `banuser_ban_id` int(4) unsigned NOT NULL,             
            UNIQUE KEY `banuser_id` (`banuser_id`),                
            KEY `banuser_user_id` (`banuser_user_id`)              
          ) ENGINE=MyISAM DEFAULT CHARSET=utf8 COLLATE utf8_general_ci;
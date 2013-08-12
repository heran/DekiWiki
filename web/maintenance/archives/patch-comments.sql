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
	`cmnt_replyto_id` int(8) unsigned default NULL,                   
	`cmnt_deleter_user_id` int(8) unsigned default NULL,              
	`cmnt_delete_date` timestamp NULL default NULL,                   
PRIMARY KEY  (`cmnt_id`),                                         
UNIQUE KEY `pageid_number` (`cmnt_page_id`,`cmnt_number`)         
) ENGINE=MyISAM DEFAULT CHARSET=utf8 COLLATE utf8_general_ci;

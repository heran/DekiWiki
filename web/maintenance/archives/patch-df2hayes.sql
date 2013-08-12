ALTER TABLE archive
	MODIFY `ar_user_text` varchar(255) NOT NULL default '',
	MODIFY `ar_timestamp` varchar(14) NOT NULL default '';

ALTER TABLE attachments
	MODIFY `at_from` int(8) unsigned NOT NULL default '0',
	MODIFY `at_filename` varchar(128) NOT NULL default '',
	MODIFY `at_timestamp` varchar(14) NOT NULL default '',
	MODIFY `at_filesize` int(8) unsigned NOT NULL default '0',
	MODIFY `at_filetype` varchar(32) NOT NULL default '',
	MODIFY `at_extension` varchar(32) NOT NULL default '',
	MODIFY `at_user` int(5) unsigned NOT NULL default '0',
	MODIFY `at_user_text` varchar(255) NOT NULL default '',
	MODIFY `at_name` varchar(128) NOT NULL default '',
	MODIFY `at_removed` varchar(14)	default NULL,
	MODIFY `at_removed_by_text` varchar(255) default NULL;

ALTER TABLE linkscc
	MODIFY `lcc_pageid` int(10) unsigned NOT NULL default '0';

ALTER TABLE logging
	MODIFY `log_type` varchar(10) NOT NULL default '',
	MODIFY `log_action` varchar(10) NOT NULL default '',
	MODIFY `log_timestamp` varchar(14) NOT NULL default '19700101000000';

ALTER TABLE objectcache
	MODIFY `keyname` varchar(255) NOT NULL default '';

ALTER TABLE old
	MODIFY `old_user_text` varchar(255) NOT NULL default '',
	MODIFY `old_timestamp` varchar(14) NOT NULL default '',
	MODIFY `inverse_timestamp` varchar(14) NOT NULL default '';

ALTER TABLE querycache
	MODIFY `qc_type` char(32) NOT NULL default '';

ALTER TABLE recentchanges
	MODIFY `rc_ip` varchar(15) NOT NULL default '';

ALTER TABLE users
	MODIFY `user_touched` varchar(14) NOT NULL default '',
	MODIFY `user_token` varchar(32) NOT NULL default '';

ALTER TABLE watchlist
	MODIFY `wl_user` int(5) unsigned NOT NULL default '0';

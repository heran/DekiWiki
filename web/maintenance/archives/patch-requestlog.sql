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



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
            ) ENGINE=MyISAM DEFAULT CHARSET=utf8 ROW_FORMAT=DYNAMIC;

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
          ) ENGINE=MyISAM DEFAULT CHARSET=utf8 ROW_FORMAT=DYNAMIC;



DELIMITER $$
DROP PROCEDURE IF EXISTS `log_ins_hit`$$
CREATE PROCEDURE `log_ins_hit`(
                       	IN requesthost varchar(64),
			IN requesthostheader varchar(64),
                       	IN requestpath varchar(512),
                       	IN requestparams varchar(512),
                       	IN requestverb varchar(8),
                       	IN dekiuser varchar(32),
                       	IN origin varchar(64),
                       	IN servicehost varchar(64),
                       	IN servicefeature varchar(128),
                       	IN responsestatus varchar(8),
                       	IN executiontime int unsigned,
                       	IN response varchar(2048))
BEGIN
insert delayed into requestlog (
	`rl_requesthost`, `rl_requesthostheader`, `rl_requestpath`, `rl_requestparams`, `rl_requestverb`, 
	`rl_dekiuser`, `rl_origin`, `rl_servicehost`, `rl_servicefeature`, `rl_responsestatus`, `rl_executiontime`, 
	`rl_response`
) values (
	requesthost, requesthostheader, requestpath, requestparams, requestverb, 
	dekiuser, origin, servicehost, servicefeature, responsestatus, executiontime, 
	response
);

END$$
DELIMITER ;

DELIMITER $$

DROP PROCEDURE IF EXISTS `log_ins_statsbyminute`$$

CREATE PROCEDURE `log_ins_statsbyminute`()
    BEGIN

select addtime(timestamp(curdate()), maketime(hour(now()) -2, 0,0)) into @range_start;
select addtime(@range_start, maketime(1, 0, 0)) into @range_end;

insert LOW_PRIORITY into requeststats (rs_numrequests, rs_servicehost, rs_requestverb, rs_servicefeature, rs_responsestatus, rs_exec_avg, rs_exec_std, rs_ts_start, rs_ts_length)
(
	select SQL_NO_CACHE
	count(*) as rs_numrequests,
	rl_servicehost as rs_servicehost,
	rl_requestverb as rs_requestverb,
	rl_servicefeature as rs_servicefeature, 
	rl_responsestatus as rs_responsestatus,
	avg(rl_executiontime) as rs_exec_avg,
	std(rl_executiontime) as rs_exec_std,
	addtime(timestamp(date(rl_timestamp)), maketime(hour(rl_timestamp), minute(rl_timestamp),0)) as rs_ts_start,
	1 as rs_ts_length	
from 	requestlog
where 	rl_timestamp >= @range_start 
AND	rl_timestamp < @range_end
group by rs_servicehost, rs_requestverb, rs_servicefeature, rs_responsestatus, rs_ts_start
order by rs_ts_start
);
	
    END$$

DELIMITER ;

DELIMITER $$

DROP PROCEDURE IF EXISTS `log_ins_statsbyhour`$$

CREATE PROCEDURE `log_ins_statsbyhour`()
    BEGIN

select addtime(timestamp(curdate()), maketime(hour(now()) -2, 0,0)) into @range_start;
select addtime(@range_start, maketime(1, 0, 0)) into @range_end;

insert LOW_PRIORITY into requeststats (rs_numrequests, rs_servicehost, rs_requestverb, rs_servicefeature, rs_responsestatus, rs_exec_avg, rs_exec_std, rs_ts_start, rs_ts_length)
(
	select SQL_NO_CACHE
	count(*) as rs_numrequests,
	rl_servicehost as rs_servicehost,
	rl_requestverb as rs_requestverb,
	rl_servicefeature as rs_servicefeature, 
	rl_responsestatus as rs_responsestatus,
	avg(rl_executiontime) as rs_exec_avg,
	std(rl_executiontime) as rs_exec_std,
	addtime(timestamp(date(rl_timestamp)), maketime(hour(rl_timestamp), 0,0)) as rs_ts_start,
	60 as rs_ts_length	
from 	requestlog
where 	rl_timestamp >= @range_start 
AND	rl_timestamp < @range_end
group by rs_servicehost, rs_requestverb, rs_servicefeature, rs_responsestatus, rs_ts_start
order by rs_ts_start
);
	
    END$$

DELIMITER ;
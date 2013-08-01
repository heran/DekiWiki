/* 
   This script should be run for existing sites that had data since before version 1.10.
   The intent is to link up several page operations that happened in one api call together into a 'transaction'.
   This then allows simpler tracking of batch operations such as page moves, deletes, restores, permissions, etc 
   and makes it easier to put together a better change log.
   
   The `transaction` table lists write operations performed on the api. Recentchanges lists each page/file/whatever affected by the transaction.
   
   After running this script on an existing site, the `logging` table can be safely dropped.   

*/


CREATE TABLE `transactions` (                            
	`t_id` int(4) NOT NULL auto_increment,                 
	`t_timestamp` datetime NOT NULL,                       
	`t_user_id` int(4) NOT NULL,                           
	`t_page_id` int(4) unsigned default NULL,              
	`t_title` varchar(255) default NULL,                   
	`t_namespace` tinyint(2) default NULL,                 
	`t_type` tinyint(2) default NULL,                      
	`t_reverted` tinyint(1) NOT NULL default '0',          
	`t_revert_user_id` int(4) unsigned default NULL,       
	`t_revert_timestamp` datetime default NULL,            
	`t_revert_reason` varchar(255) default NULL,           
	PRIMARY KEY  (`t_id`),                                 
	KEY `t_timestamp` (`t_timestamp`)                      
) ENGINE=MyISAM DEFAULT CHARSET=utf8 COLLATE utf8_general_ci;

ALTER TABLE `recentchanges` ADD `rc_transaction_id` INT( 10 ) UNSIGNED NOT NULL DEFAULT '0';
ALTER TABLE `recentchanges` ADD INDEX ( `rc_transaction_id` ) ;

alter table `archive` add column `ar_transaction_id` int (4)UNSIGNED   NOT NULL DEFAULT '0';
alter table `archive` add index `ar_transaction_id` (`ar_transaction_id`);

-- Create transactions from deletes in logging table
insert into transactions(t_timestamp, t_title, t_namespace, t_user_id, t_type)
select  str_to_date(l3.log_timestamp, '%Y%m%d%H%i%s') as ts,
	l3.log_title as title, 
	l3.log_namespace as ns, 
	l3.log_user as userid,
	5
from logging l3
join  (
	select 	l.log_timestamp as ts,
	(	select l2.log_title
		from logging l2
		where l2.log_timestamp = l.log_timestamp
		and l.log_action = 'delete'
		order by l2.log_title asc
		limit 1
	)	as title,
	(	select l2.log_namespace
		from logging l2
		where l2.log_timestamp = l.log_timestamp
		and l.log_action = 'delete'
		order by l2.log_title asc
		limit 1
	)	as ns
	from logging l
	where	l.log_action = 'delete'
	group by l.log_timestamp
	order by log_timestamp
) lj
on lj.title = l3.log_title
and lj.ts = l3.log_timestamp
and lj.ns = l3.log_namespace;

-- update recent changes with transaction ids
update recentchanges 
left join transactions 
on	addtime(t_timestamp, '00:00:30') > str_to_date(rc_timestamp, '%Y%m%d%H%i%s')
AND	addtime(t_timestamp,'-00:00:30') < str_to_date(rc_timestamp, '%Y%m%d%H%i%s')
AND	rc_title like concat(t_title, '%') 
AND	t_namespace = rc_namespace
set rc_transaction_id = t_id
where 	rc_type = 5;

-- update pageids in transactions
update transactions
join recentchanges
on t_id = rc_transaction_id 
and t_title = rc_title
and t_namespace = rc_namespace
set t_page_id = rc_cur_id
where rc_type = 5;

-- set transaction id in all deleted revisions of a page to the transaction that caused the delete
update archive
join recentchanges
	on rc_cur_id = ar_last_page_id 
join transactions
	on t_id = rc_transaction_id	
set	ar_transaction_id = t_id
where	rc_type = 5
and	t_type = 5
and	t_reverted = 0
and	t_page_id is not null
and	ar_last_page_id > 0;

-- update existing transactions to reverted if no matching page in archive table exists
update transactions
left join archive
	on t_id = ar_transaction_id
set t_reverted = 1
where t_reverted = 0
and ar_id is null
and t_type = 5;
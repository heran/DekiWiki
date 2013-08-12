--
-- Table structure for table `page_counter`
--

CREATE TABLE `page_viewcount` (
  `page_id` int(8) unsigned NOT NULL,
  `page_counter` bigint(20) unsigned not null default 0,
  PRIMARY KEY  (`page_id`)
) ENGINE=MyISAM DEFAULT CHARSET=utf8 COLLATE utf8_general_ci;

INSERT INTO page_viewcount SELECT page_id, page_counter FROM pages;
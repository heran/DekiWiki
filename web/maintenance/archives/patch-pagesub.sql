--
-- Table structure for table `pagesub`
--
CREATE TABLE pagesub (
  pagesub_page_id int(8) unsigned not null,
  pagesub_user_id int(10) unsigned not null,
  pagesub_child_pages tinyint unsigned not null default '0',
  primary key (pagesub_page_id, pagesub_user_id)
) ENGINE=MyISAM DEFAULT CHARSET=utf8 COLLATE utf8_general_ci;

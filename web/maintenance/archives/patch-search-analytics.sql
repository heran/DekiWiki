--
-- Table structure for table `querylog`
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

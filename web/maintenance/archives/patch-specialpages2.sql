LOCK TABLES `pages` WRITE;
DELETE FROM `pages` WHERE page_namespace=101 AND page_title IN ('Tags', 'Events', 'About');
DELETE FROM `pages` WHERE page_namespace=10 AND page_title='' LIMIT 1;
DELETE FROM `pages` WHERE page_namespace=103 AND page_title='AdminVisual';
DELETE FROM `pages` WHERE page_namespace=2 AND page_title='';
DELETE FROM `pages` WHERE page_namespace=103 AND page_title='SiteSettings';
DELETE FROM `pages` WHERE page_namespace=101 AND page_title='';
INSERT INTO `pages` (`page_namespace`, `page_title`, `page_tip`, `page_parent`, `page_restriction_id`, `page_content_type`, `page_text`, `page_comment`, `page_toc`, `page_timestamp`, `page_touched`, `page_inverse_timestamp`) VALUES
        (101, 'Tags', 'Special page', 0, 0, 'text/plain', '', '', '', '', '', ''),
        (101, 'Events', 'Special page', 0, 0, 'text/plain', '', '', '', '', '', ''),
	(101, 'About', 'Special page', 0, 0, 'text/plain', '', '', '', '', '', ''),
	(10, '', 'Template page', 0, 0, 'text/plain', '', '', '', '', '', ''),
	(2, '', 'User page', 0, 0, 'text/plain', '', '', '', '', '', ''),
	(101, '', 'Special page', 0, 2, 'application/x.deki-text', '{{ wiki.tree("Special:") }}', '', '', '', '', ''); 
UNLOCK TABLES;

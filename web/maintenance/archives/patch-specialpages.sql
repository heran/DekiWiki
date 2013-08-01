LOCK TABLES `pages` WRITE;
INSERT INTO `pages` (`page_namespace`, `page_title`, `page_tip`, `page_parent`, `page_restriction_id`, `page_content_type`, `page_text`, `page_comment`, `page_toc`, `page_timestamp`, `page_touched`, `page_inverse_timestamp`) VALUES
        (101, 'Redirects', 'Special page', 0, 0, 'text/plain', '', '', '', '', '', ''),
        (101, 'Userlogin', 'Special page', 0, 0, 'text/plain', '', '', '', '', '', ''),
        (101, 'Userlogout', 'Special page', 0, 0, 'text/plain', '', '', '', '', '', ''),
        (101, 'Preferences', 'Special page', 0, 0, 'text/plain', '', '', '', '', '', ''),
        (101, 'Watchedpages', 'Special page', 0, 0, 'text/plain', '', '', '', '', '', ''),
        (101, 'Recentchanges', 'Special page', 0, 0, 'text/plain', '', '', '', '', '', ''),
        (101, 'Listusers', 'Special page', 0, 0, 'text/plain', '', '', '', '', '', ''),
        (101, 'Listguests', 'Special page', 0, 0, 'text/plain', '', '', '', '', '', ''),
        (101, 'ListTemplates', 'Special page', 0, 0, 'text/plain', '', '', '', '', '', ''),
        (101, 'ListRss', 'Special page', 0, 0, 'text/plain', '', '', '', '', '', ''),
        (101, 'Search', 'Special page', 0, 0, 'text/plain', '', '', '', '', '', ''),
        (101, 'Sitemap', 'Special page', 0, 0, 'text/plain', '', '', '', '', '', ''),
        (101, 'Specialpages', 'Special page', 0, 0, 'text/plain', '', '', '', '', '', ''),
        (101, 'Contributions', 'Special page', 0, 0, 'text/plain', '', '', '', '', '', ''),
        (101, 'Emailuser', 'Special page', 0, 0, 'text/plain', '', '', '', '', '', ''),
        (101, 'Undelete', 'Special page', 0, 0, 'text/plain', '', '', '', '', '', ''),
        (101, 'Popularpages', 'Special page', 0, 0, 'text/plain', '', '', '', '', '', ''),
        (101, 'DeleteAll', 'Special page', 0, 0, 'text/plain', '', '', '', '', '', ''),
        (101, 'Watchlist', 'Special page', 0, 0, 'text/plain', '', '', '', '', '', ''),
        (101, 'Statistics', 'Special page', 0, 0, 'text/plain', '', '', '', '', '', '');
INSERT INTO `pages` (`page_namespace`, `page_title`, `page_tip`, `page_parent`, `page_restriction_id`, `page_content_type`, `page_text`, `page_comment`, `page_toc`, `page_timestamp`, `page_touched`, `page_inverse_timestamp`) VALUES
	(103, '', 'Admin page', 0, 0, 'text/plain', '', '', '', '', '', '');

SELECT LAST_INSERT_ID() INTO @ADMIN_PAGE_ID;
INSERT INTO `pages` (`page_namespace`, `page_title`, `page_tip`, `page_parent`, `page_restriction_id`, `page_content_type`, `page_text`, `page_comment`, `page_toc`, `page_timestamp`, `page_touched`, `page_inverse_timestamp`) VALUES
        (103, 'ServiceManagement', 'Admin page', @ADMIN_PAGE_ID, 0, 'text/plain', '', '', '', '', '', ''),
        (103, 'GroupManagement', 'Admin page', @ADMIN_PAGE_ID, 0, 'text/plain', '', '', '', '', '', ''),
        (103, 'UserManagement', 'Admin page', @ADMIN_PAGE_ID, 0, 'text/plain', '', '', '', '', '', ''),
        (103, 'Configuration', 'Admin page', @ADMIN_PAGE_ID, 0, 'text/plain', '', '', '', '', '', ''),
        (103, 'RecycleBin', 'Admin page', @ADMIN_PAGE_ID, 0, 'text/plain', '', '', '', '', '', ''),
        (103, 'UnusedRedirects', 'Admin page', @ADMIN_PAGE_ID, 0, 'text/plain', '', '', '', '', '', ''),
        (103, 'SiteSettings', 'Admin page', @ADMIN_PAGE_ID, 0, 'text/plain', '', '', '', '', '', ''),
        (103, 'AccountInfo', 'Admin page', @ADMIN_PAGE_ID, 0, 'text/plain', '', '', '', '', '', ''),
        (103, 'ProductActivation', 'Admin page', @ADMIN_PAGE_ID, 0, 'text/plain', '', '', '', '', '', ''),
        (103, 'ServerSettings', 'Admin page', @ADMIN_PAGE_ID, 0, 'text/plain', '', '', '', '', '', ''),
        (103, 'DoubleRedirects', 'Admin page', @ADMIN_PAGE_ID, 0, 'text/plain', '', '', '', '', '', ''),
        (103, 'Visual', 'Admin page', @ADMIN_PAGE_ID, 0, 'text/plain', '', '', '', '', '', ''),
        (103, 'AdminVisual', 'Admin page', @ADMIN_PAGE_ID, 0, 'text/plain', '', '', '', '', '', '');
UNLOCK TABLES;


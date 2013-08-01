-- Default values for config table for the wiki instance
insert into `config` (`config_key`, `config_value`) values('files/max-file-size','268435456');
insert into `config` (`config_key`, `config_value`) values('files/blocked-extensions','html, htm, exe, vbs, scr, reg, bat, com');
insert into `config` (`config_key`, `config_value`) values('files/imagemagick-extensions','bmp, jpg, jpeg, png, gif');
insert into `config` (`config_key`, `config_value`) values('files/imagemagick-max-size','2000000');
insert into `config` (`config_key`, `config_value`) values('ui/banned-words',"");
insert into `config` (`config_key`, `config_value`) values('ui/sitename','Deki');
insert into `config` (`config_key`, `config_value`) values('ui/language','en-us');
insert into `config` (`config_key`, `config_value`) values('ui/analytics-key','UA-68075-12');
insert into `config` (`config_key`, `config_value`) values('security/new-account-role','Contributor');
insert into `config` (`config_key`, `config_value`) values('cache/users','false');
insert into `config` (`config_key`, `config_value`) values('cache/pages','false');
insert into `config` (`config_key`, `config_value`) values('cache/permissions','false');
insert into `config` (`config_key`, `config_value`) values('cache/roles','false');
insert into `config` (`config_key`, `config_value`) values('cache/services','false');
insert into `config` (`config_key`, `config_value`) values('security/cookie-expire-secs','604800');
insert into `config` (`config_key`, `config_value`) values('security/allow-anon-account-creation','true');
insert into `config` (`config_key`, `config_value`) values('editor/safe-html','true');
insert into `config` (`config_key`, `config_value`) values('storage/type','fs');
insert into `config` (`config_key`, `config_value`) values('ui/skin','sky-tangerine');
insert into `config` (`config_key`, `config_value`) values('ui/template','fiesta');
insert into `config` (`config_key`, `config_value`) values('ui/logo-maxwidth','175');
insert into `config` (`config_key`, `config_value`) values('ui/logo-maxheight','175');
insert into `config` (`config_key`, `config_value`) values('languages',"");


INSERT INTO `pages` (`page_namespace`, `page_title`, `page_text`, `page_comment`, `page_timestamp`, `page_touched`, `page_inverse_timestamp`, `page_toc`, `page_tip`, `page_parent`, `page_restriction_id`, `page_content_type`) VALUES 
	(101, 'Userlogin', '', '', '', '', '', '', 'Special page', 0, 0, 'text/plain'),
	(101, 'Userlogout', '', '', '', '', '', '', 'Special page', 0, 0, 'text/plain'),
	(101, 'Preferences', '', '', '', '', '', '', 'Special page', 0, 0, 'text/plain'),
	(101, 'Watchedpages', '', '', '', '', '', '', 'Special page', 0, 0, 'text/plain'),
	(101, 'Recentchanges', '', '', '', '', '', '', 'Special page', 0, 0, 'text/plain'),
	(101, 'Listusers', '', '', '', '', '', '', 'Special page', 0, 0, 'text/plain'),
	(101, 'ListTemplates', '', '', '', '', '', '', 'Special page', 0, 0, 'text/plain'),
	(101, 'ListRss', '', '', '', '', '', '', 'Special page', 0, 0, 'text/plain'),
	(101, 'Search', '', '', '', '', '', '', 'Special page', 0, 0, 'text/plain'),
	(101, 'Sitemap', '', '', '', '', '', '', 'Special page', 0, 0, 'text/plain'),
	(101, 'Contributions', '', '', '', '', '', '', 'Special page', 0, 0, 'text/plain'),
	(101, 'Undelete', '', '', '', '', '', '', 'Special page', 0, 0, 'text/plain'),
	(101, 'Popularpages', '', '', '', '', '', '', 'Special page', 0, 0, 'text/plain'),
	(101, 'Watchlist', '', '', '', '', '', '', 'Special page', 0, 0, 'text/plain'),
	(101, 'About', '', '', '', '', '', '', 'Special page', 0, 0, 'text/plain'),
	(101, 'Statistics', '', '', '', '', '', '', 'Special page', 0, 0, 'text/plain'),
	(101, 'Tags', '', '', '', '', '', '', 'Special page', 0, 0, 'text/plain'),
	(101, 'Events', '', '', '', '', '', '', 'Special page', 0, 0, 'text/plain'),
	(2, '', 'User page', '', '', '', '', '', 'Admin page', 0, 0, 'text/plain'),
	(10, '', '', '', '', '', '', '', 'Template page', 0, 0, 'text/plain');

INSERT into `pages` (`page_namespace`, `page_title`, `page_text`, `page_touched`, `page_user_id`, `page_timestamp`, `page_inverse_timestamp`) 
	VALUES (0, '', '<p>Here are some tips on getting started with this wiki:</p> 
		<div style="width:580px;">
	  <h2>Contribute content</h2> 
		  <div> 
		    <div style="background: transparent url(http://wik.is/skins/v2/icon_collaborate.gif) no-repeat scroll 0% 50%; padding-left: 90px; -moz-background-clip: -moz-initial; -moz-background-origin: -moz-initial; -moz-background-inline-policy: -moz-initial; min-height: 80px; margin-bottom: 30px;"> 
		      <h3>Collaborate</h3> A browser and an internet connection is all you need to start adding content to Deki. Share ideas and plans through the simple interface. Click the &quot;Edit Page&quot; and &quot;New page&quot; buttons on each page to begin contributing content. 
		    </div> 
		    <div style="background: transparent url(http://wik.is/skins/v2/icon_mashup.gif) no-repeat scroll 0% 50%; padding-left: 90px; -moz-background-clip: -moz-initial; -moz-background-origin: -moz-initial; -moz-background-inline-policy: -moz-initial; min-height: 80px; margin-bottom: 30px;"> 
		      <h3>Create mashups</h3> Deki supports boatloads of <a class="external" href="http://wiki.developer.mindtouch.com/MindTouch_Deki/Extensions">extensions</a> - these are little features which allow you to embed content from other websites. For example, you can embed <a class="external" href="http://wiki.developer.mindtouch.com/MindTouch_Deki/Extensions/Google">Google Maps</a>, <a class="external" href="http://wiki.developer.mindtouch.com/MindTouch_Deki/Extensions/WindowsLive">Windows Live Maps</a>, AND <a class="external" href="http://wiki.developer.mindtouch.com/MindTouch_Deki/Extensions/Yahoo">Yahoo! Maps</a>, all on the same page. You can also dynamically manipulate images using our <a class="external" href="http://wiki.developer.mindtouch.com/MindTouch_Deki/Extensions/ImageMagick">ImageMagick extension</a>. 
		      <br /> 
		      <br /> There\'s so much you can do, so learn all about available extensions in your Deki by clicking the &quot;Extensions List&quot; tab (when the editor loads), or by viewing the <a class="external" href="http://wiki.developer.mindtouch.com/MindTouch_Deki/Extensions">Extensions page at the MindTouch Developer Center</a>. 
		    </div> 
		    <div style="background: transparent url(http://wik.is/skins/v2/icon_share_media.gif) no-repeat scroll 0% 50%; padding-left: 90px; -moz-background-clip: -moz-initial; -moz-background-origin: -moz-initial; -moz-background-inline-policy: -moz-initial; min-height: 80px; margin-bottom: 30px;"> 
		      <h3>Share media</h3> Attach photos, files, and all sorts of media to any page to share with your community. Files and images are revisioned, just like pages, so you\'ll always be able to retrieve older files. 
		    </div> 
		    <div style="background: transparent url(http://wik.is/skins/v2/icon_track_changes.gif) no-repeat scroll 0% 50%; padding-left: 90px; -moz-background-clip: -moz-initial; -moz-background-origin: -moz-initial; -moz-background-inline-policy: -moz-initial; min-height: 80px; margin-bottom: 30px;"> 
		      <h3>Track changes</h3> Did somebody make a change to a page that was bad? Use the revert feature to change the contents of a page back to an older version. You\'ll never lose any work in Deki. 
		    </div> 
		  </div> 
		  <h2>Let us help you</h2> 
		  <div> 
		    <div style="background: transparent url(http://wik.is/skins/v2/icon_opengarden.gif) no-repeat scroll 0% 50%; padding-left: 90px; -moz-background-clip: -moz-initial; -moz-background-origin: -moz-initial; -moz-background-inline-policy: -moz-initial; min-height: 80px; margin-bottom: 30px;"> 
		      <h3>Create an MindTouch Developer account</h3> <a class="external" href="http://developer.mindtouch.com">MindTouch Developer Center</a> is for anybody who wants to participate in the growth of Deki - <a class="external" href="http://developer.mindtouch.com/user/register/">creating a free account</a> gives you access to our <a class="external" href="http://forums.developer.mindtouch.com">community forums</a>, our <a class="external" href="http://bugs.developer.mindtouch.com">bug tracker</a>, and to <a class="external" href="http://wiki.developer.mindtouch.com">our wiki</a>. Feel free to drop in, ask questions, or contribute. 
		    </div> 
		  </div>', 0, 1, '20071103195811', '79928896804188');
	
INSERT into `pages` (`page_namespace`, `page_title`, `page_text`, `page_touched`, `page_user_id`, `page_timestamp`, `page_inverse_timestamp`) VALUES
	(0, 'Welcome', '<p>Here are some tips on administering this wiki:</p><div style="width:580px;">
	  <h2>Getting started</h2> 
	  <div> 
	    <div style="background: transparent url(http://wik.is/skins/v2/icon_add_users.gif) no-repeat scroll 0% 50%; padding-left: 90px; -moz-background-clip: -moz-initial; -moz-background-origin: -moz-initial; -moz-background-inline-policy: -moz-initial; min-height: 80px; margin-bottom: 30px;"> 
	      <h3>Add users</h3> Your Deki is configured to allow users to create their own accounts from the login screen. However, you can also create accounts for users manually from the <a class="internal" href="/Admin:Users" title="User management">User Management</a> in your <a class="internal" href="/Admin:" title="Control Panel">Control Panel</a> (See the &quot;Tools&quot; dropdown). Users who have accounts created for them will be notified via email; bootstrapping a community has never been easier. 
	    </div> 
	    <div style="background: transparent url(http://wik.is/skins/v2/icon_select_style.gif) no-repeat scroll 0% 50%; padding-left: 90px; -moz-background-clip: -moz-initial; -moz-background-origin: -moz-initial; -moz-background-inline-policy: -moz-initial; min-height: 80px; margin-bottom: 30px;"> 
	      <h3>Select a new style</h3> Wik.is offers multiple templates and color schemes for you to choose - find a palette which is more appealing for your community. Changing your styles is as easy as going to the <a class="internal" href="/Admin:Styles" title="Visual appearance">Visual Appearances</a> in your <a class="internal" href="/Admin:" title="Control Panel">Control Panel</a>. 
	    </div> 
	    <div style="background: transparent url(http://wik.is/skins/v2/icon_explore_settings.gif) no-repeat scroll 0% 50%; padding-left: 90px; -moz-background-clip: -moz-initial; -moz-background-origin: -moz-initial; -moz-background-inline-policy: -moz-initial; min-height: 80px; margin-bottom: 30px;"> 
	      <h3>Explore your settings</h3> Don\'t be afraid to explore your <a class="internal" href="/Admin:" title="Control Panel">Control Panel</a> - you\'ll find easy ways to manage and customize your Deki. 
	    </div> 
	  </div>{{ wiki.page(&quot;&quot;) }}', 0, 1, '20071103195811', '79928896804188');

-- Default restrictions --
INSERT INTO `restrictions` (`restriction_name`, `restriction_perm_flags`, `restriction_creator_user_id`) VALUES
	('Public', '2047', '1'),
	('Semi-Public', '15', '1'),
	('Private', '1', '1');

-- Default roles --
INSERT INTO `roles` (`role_name`, `role_perm_flags`, `role_creator_user_id`) values
	('None', '1', '1'),
	('Viewer', '15', '1' ),
	('Contributor', '319', '1' ),
	('Manager', '1343', '1' ),
	('Admin', '9223372036854779903', '1' );


-- Default services --
INSERT INTO `services` (`service_type`, `service_sid`, `service_uri`, `service_description`, `service_enabled`, `service_local`) VALUES
	('auth', 'http://services.mindtouch.com/deki/draft/2006/11/dekiwiki', NULL, 'Local', 1, 1),
	('ext', NULL, 'http://ext.mindtouch.com/@api/dekiext/feed', 'Atom/RSS Feeds', 1, 0),
	('ext', NULL, 'http://ext.mindtouch.com/@api/dekiext/digg', 'Digg', 1, 0),
	('ext', NULL, 'http://ext.mindtouch.com/@api/dekiext/dhtml', 'DHtml', 1, 0),
	('ext', NULL, 'http://ext.mindtouch.com/@api/dekiext/flickr', 'Flickr', 1, 0),
	('ext', NULL, 'http://ext.mindtouch.com/@api/dekiext/gabbly', 'Gabbly', 1, 0),
	('ext', NULL, 'http://ext.mindtouch.com/@api/dekiext/media', 'Multimedia', 1, 0),
	('ext', NULL, 'http://ext.mindtouch.com/@api/dekiext/widgetbox', 'WidgetBox', 1, 0),
	('ext', NULL, 'http://wik.is/@api/windows.live', 'Windows Live', 1, 0),
	('ext', NULL, 'http://ext.mindtouch.com/@api/dekiext/syntax', 'Syntax Highlighter', 1, 0),
	('ext', NULL, 'http://ext.mindtouch.com/@api/dekiext/math', 'Math', 1, 0),
	('ext', NULL, 'http://ext.mindtouch.com/@api/dekiext/google', 'Google', 1, 0),
	('ext', NULL, 'http://ext.mindtouch.com/@api/dekiext/graphviz', 'GraphViz', 1, 0),
	('ext', NULL, 'http://ext.mindtouch.com/@api/dekiext/thinkfree', 'ThinkFree Viewer', 1, 0),
	('ext', NULL, 'http://wik.is/@api/yahoo', 'Yahoo', 1, 0),
	('ext', NULL, 'http://ext.mindtouch.com/@api/dekiext/imagemagick', 'Image Manipulation', 1, 0),
	('ext', NULL, 'http://wik.is/@api/dapper', 'Dapper', 1, 0),
	('ext', NULL, 'http://ext.mindtouch.com/@api/dekiext/fevote', 'FeVote', 1, 0),
	('ext', NULL, 'http://wik.is/@api/silverlight', 'Silverlight', 1, 0),
	('ext', NULL, 'http://ext.mindtouch.com/@api/dekiext/scratch', 'Scratch', 1, 0),
	('ext', NULL, 'http://ext.mindtouch.com/@api/dekiext/accuweather', 'AccuWeather', 1, 0),
	('ext', NULL, 'http://ext.mindtouch.com/@api/dekiext/addthis', 'AddThis', 1, 0),
	('ext', NULL, 'http://ext.mindtouch.com/@api/dekiext/ado.net', 'ADO.NET', 1, 0),
	('ext', NULL, 'http://ext.mindtouch.com/@api/dekiext/pagebus', 'Pagebus', 1, 0),
	('ext', NULL, 'http://ext.mindtouch.com/@api/dekiext/twitter', 'Twitter', 1, 0),
	('ext', NULL, 'http://ext.mindtouch.com/@api/dekiext/flowplayer', 'Flowplayer', 1, 0),
	('ext', NULL, 'http://ext.mindtouch.com/@api/dekiext/gravatar', 'Gravatar', 1, 0),
	('ext', NULL, 'http://ext.mindtouch.com/@api/dekiext/spoiler', 'Spoiler', 1, 0),
	('ext', NULL, 'http://ext.mindtouch.com/@api/dekiext/skype', 'Skype', 1, 0),
	('ext', NULL, 'http://ext.mindtouch.com/@api/dekiext/linkedin', 'LinkedIn', 1, 0);



INSERT INTO `users` (`user_name`,`user_real_name`,`user_password`,`user_newpassword`,`user_email`,`user_options`,`user_touched`,`user_token`,`user_role_id`,`user_active`,`user_service_id`, `user_builtin`) VALUES
	('Sysop','','','','','','20061221213835','2158a249b6b8368a738bf81d97627be1','5','1','1','1'),
	('Anonymous','Anonymous User','','','','','0','','2','1','1','1');

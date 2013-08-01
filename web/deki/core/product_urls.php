<?php

class ProductURL
{
	// learn more about adaptive search
	const ADAPTIVE_SEARCH = 'http://www.mindtouch.com/redir/adaptive-search/';
	const CURATION_ANALYTICS = 'http://www.mindtouch.com/redir/curation-analytics/'; // this is hardcoded in resouces.txt
	const PAGE_SELECTOR = 'http://www.mindtouch.com/redir/page-gallery/';
	const CONTENT_RATING = 'http://www.mindtouch.com/redir/community-rating/';
	const DESKTOP_SUITE = 'http://www.mindtouch.com/redir/desktop-suite/';
	const CACHING = 'http://www.mindtouch.com/redir/caching/';
	const COMMERCIAL = 'http://www.mindtouch.com/redir/mindtouch-tcs/';
	const ACTIVATION = 'http://www.mindtouch.com/redir/mindtouch-activation/'; // used in cp banner when expired
	const ACTIVITY_STREAM = 'http://mindtouch.com/redir/activity-stream/'; // this is hardcoded in the user template
	
	// @TODO royk this isn't used uniformly yet
	const PLATFORM = 'http://www.mindtouch.com/redir/mindtouch-platform/';
	
	// informational help
	const CONFIGURATION = 'http://www.mindtouch.com/redir/support-configuration/';
	const HELP = 'http://www.mindtouch.com/redir/support-help/';  // used as $wgHelpUrl in DefaultSettings.php
	const HELP_TRIAL = 'http://www.mindtouch.com/redir/support-trial/';
	const IDF_HELP = 'http://www.mindtouch.com/redir/idf-guide/'; // hardcoded in the IDF package
	
	// @TODO royk update to cp location
	const HELP_CP = 'http://developer.mindtouch.com/en/docs/MindTouch/User_Manual';
	
	// marketing urls
	const SUPPORT = 'http://www.mindtouch.com/redir/support-options/';
	const FAQ = 'http://www.mindtouch.com/redir/support-faq/';
	const HOMEPAGE = 'http://www.mindtouch.com/';
	const UNIVERSITY = 'http://www.mindtouch.com/redir/mindtouch-university/';
	const COMMUNITY = 'http://www.mindtouch.com/redir/community/';
	const COMMUNITY_FORUMS = 'http://www.mindtouch.com/redir/forums/';
	
	// integration urls
	const INTEGRATION_ZENDESK = 'http://www.mindtouch.com/redir/zendesk-integration/';
	
	// installer URLs
	const INSTALL_HELP = 'http://www.mindtouch.com/redir/install-help/';
	
	const MINDTOUCH_TCS = 'http://www.mindtouch.com/redir/mindtouch-tcs/';
	const MINDTOUCH_PLATFORM = 'http://www.mindtouch.com/redir/mindtouch-platform/';
	const MINDTOUCH_CORE = 'http://www.mindtouch.com/redir/mindtouch-core/';
	
	// other products
	const MINDTOUCH_DREAM = 'http://wiki.developer.mindtouch.com/Dream';
	const SGMLREADER = 'http://wiki.developer.mindtouch.com/SgmlReader';
	
	// control panel urls
	const AUTH_HELP_CUSTOM = 'http://developer.mindtouch.com/en/kb/Setting_up_External_Authentication_within_MindTouch';
	const AUTH_HELP_DRUPAL = 'http://developer.mindtouch.com/App_Catalog/Drupal_User_Accounts';
	const AUTH_HELP_HTTP = 'http://developer.mindtouch.com/App_Catalog/HTTP_Passthrough';
	const AUTH_HELP_JOOMLA = 'http://developer.mindtouch.com/App_Catalog/Joomla_User_Accounts';
	const AUTH_HELP_LDAP = 'http://developer.mindtouch.com/App_Catalog/LDAP%2f%2fActiveDirectory%2f%2feDirectory';
	const AUTH_HELP_WORDPRESS = 'http://developer.mindtouch.com/App_Catalog/WordPress_User_Accounts';
	const TRIAL_LICENSE_PURCHASE = 'http://www.mindtouch.com/redir/mindtouch-trial-purchase/';	// used in cloud to upgrade account 
}

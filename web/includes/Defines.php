<?php
/*
 * MindTouch Core - open source enterprise collaborative networking
 *  derived from MediaWiki (www.mediawiki.org)
 * Copyright (c) 2006-2010 MindTouch Inc.
 * www.mindtouch.com oss@mindtouch.com
 *
 * For community documentation and downloads visit www.opengarden.org;
 * please review the licensing section.
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License along
 * with this program; if not, write to the Free Software Foundation, Inc.,
 * 59 Temple Place - Suite 330, Boston, MA 02111-1307, USA.
 * http://www.gnu.org/copyleft/gpl.html
 */

/**
 * A few constants that might be needed during LocalSettings.php
 * @package MediaWiki
 */

/**#@+
 * Database related constants
 */
define( 'DBO_DEBUG', 1 );
define( 'DBO_NOBUFFER', 2 );
define( 'DBO_IGNORE', 4 );
define( 'DBO_TRX', 8 );
define( 'DBO_DEFAULT', 16 );
define( 'DBO_PERSISTENT', 32 );
define( 'DB_MASTER', '');
define( 'DB_SLAVE', '');
/**#@-*/

/**#@+
 * Virtual namespaces; don't appear in the page database
 */
define('NS_ADMIN', 103);
define('NS_MEDIA', -2);
define('NS_SPECIAL', 101);
/**#@-*/

/**#@+
 * Real namespaces
 */
define('NS_MAIN', 0);
define('NS_TALK', 1);
define('NS_USER', 2);
define('NS_USER_TALK', 3);
define('NS_PROJECT', 4);
define('NS_PROJECT_TALK', 5);
define('NS_IMAGE', 6);
define('NS_IMAGE_TALK', 7);
define('NS_MEDIAWIKI', 8);
# MT ursm: define('NS_MEDIAWIKI_TALK', 9);
define('NS_TEMPLATE', 10);
define('NS_TEMPLATE_TALK', 11);
define('NS_HELP', 12);
define('NS_HELP_TALK', 13);
define('NS_CATEGORY', 14);
define('NS_CATEGORY_TALK', 15);
# MT ursm
define('NS_ATTACHMENT', 16);
/**#@-*/

#MT: royk
$wgLockedNamespaces = array(NS_HELP);

/**
 * Available feeds objects
 * Should probably only be defined when a page is syndicated ie when
 * $wgOut->isSyndicated() is true
 */
$wgFeedClasses = array(
	'atom' => 'AtomFeed',
);

/**
 * User rights management
 * a big array of string defining a right, that's how they are saved in the
 * database.
 */
$wgAvailableRights = array('read', 'edit', 'move', 'delete', 'undelete',
'protect', 'block', 'userrights', 'createaccount', 'upload', 'asksql',
'rollback', 'patrol', 'editinterface', 'siteadmin', 'bot', 'controlpanel');

/**
 * Anti-lock flags
 * See DefaultSettings.php for a description
 */
define( 'ALF_PRELOAD_LINKS', 1 );
define( 'ALF_PRELOAD_EXISTENCE', 2 );
define( 'ALF_NO_LINK_LOCK', 4 );

define('TUO_TYPE_HILITE', 1);
define('TUO_TYPE_BOOKMARK', 2);

/**
 * After the Deadline default status
 */
define('ATD_DEFAULT_STATUS', false);


//Variables need to be callable from this file alone w/o including DefaultSettings.php, which cascades the auth plugin
$wgUseBuild = true; //set to false in order to use dynamic caching of Javascript files (not necessary for builds)

$wgConfigMap = 
	array(
		'wgGoogleAnalytics' => 'ui/analytics-key',
		'wgGoogleAnalyticsDomain' => 'ui/analytics-domain',
		'wgDBserver' => 'db-server', 
		'wgDBport' => 'db-port',
		'wgDBuser' => 'db-user', 
		'wgDBpassword' => 'db-password', 
		'wgDBname' => 'db-catalog',
		'wgLicenseProductKey' => 'license/productkey',
		'wgSitename' => 'ui/sitename',
		'wgLanguageCode' => 'ui/language', 
		'wgHelpUrl' => 'ui/help-url', 
		'wgActiveTemplate' => 'ui/template', 
		'wgActiveSkin' => 'ui/skin', 
		'wgStoragePath' => 'storage/fs/path',
		'wgCommentCount' => 'ui/comment-count', 
		'wgNavPaneEnabled' => 'ui/dynamic-nav', 
		'wgNavPaneWidth' => 'ui/nav-max-width',
		'wgDefaultTimezone' => 'ui/site-timezone', 
		'wgEnableSearchHighlight' => 'ui/search-highlight', 
		'wgImageExtensions' => 'files/image-extensions', 
		'wgPasswordSender' => 'admin/email', 
		'wgLogo' => 'ui/logo-uri', 
		'wgTrustedAuth' => 'security/allow-trusted-auth',
		'wgTrustedAuthProvider' => 'security/trusted-auth-provider-id',
		'wgTrustedAuthCgiVariable' => 'security/trusted-auth-cgi-variable-name',
		'wgTrustedAuthCgiPattern' => 'security/trusted-auth-cgi-variable-pattern',
		'wgDisableTextSearch' => 'site/search-disabled',
		'wgNewAccountRole' => 'security/new-account-role', 
		'wgAnonView' => 'security/allow-anon-view', //only used for mindtouch hosted; this is actually a misnomer; it should have been named block-anon-view
		'wgAccountLevel' => 'site/account-level', //only used for mindtouch hosted
		'wgUpgradeKey'	=> 'site/activation',
		'wgAnonAccCreate' => 'security/allow-anon-account-creation', 
		'wgSMTPServers' => 'mail/smtp-servers', 
		'wgSMTPUser' => 'mail/smtp-username', 
		'wgEditorToolbarSet' => 'editor/toolbar',
		'wgSMTPPwd' => 'mail/smtp-password',
		'wgSMTPPort' => 'mail/smtp-port',
		'wgSMTPSecure' => 'mail/smtp-secure',
		'wgCookieExpiration' => 'security/cookie-expire-secs', 
		'wgDefaultAuthServiceId' => 'ui/default-auth-service', 
		'wgLanguagesAllowed' => 'languages',
		'wgHelpUrl' => 'ui/help-url',
		'wgVarnishCache' => 'cache/varnish',
		'wgVarnishMaxage' => 'cache/varnish-maxage', 
		'wgRecaptchaPublicKey' => 'ui/plugins/special_userregistration_recaptcha/public-key', 
		'wgRecaptchaPrivateKey' => 'ui/plugins/special_userregistration_recaptcha/private-key',
		'wgDefaultTimezone' => 'ui/timezone'
);

//map API targets to skin variables
$wgContentTargets = array(
	//apikey => skinname
);

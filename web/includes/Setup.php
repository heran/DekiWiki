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
 * Include most things that's need to customize the site
 * @package MediaWiki
 */

/**
 * This file is not a valid entry point, perform no further processing unless
 * MINDTOUCH_DEKI is defined
 */
if (defined('MINDTOUCH_DEKI')) :

# The main wiki script and things like database
# conversion and maintenance scripts all share a
# common setup of including lots of classes and
# setting up a few globals.
#

global $wgProfiling, $wgProfileSampleRate, $wgIP, $IP;

if( !isset( $wgProfiling ) )
	$wgProfiling = false;

if ( $wgProfiling and (0 == rand() % $wgProfileSampleRate ) ) {
	require_once( 'Profiling.php');
} else {
	$wgFunctionStack = array();

	if ( function_exists("setproctitle") ) {
		function wfProfileIn( $fn = '' ) {
			global $wgFunctionStack, $wgDBname;
			$wgFunctionStack[] = $fn;
			setproctitle($fn . " [$wgDBname]");
		}
		function wfProfileOut( $fn = '' ) {
			global $wgFunctionStack, $wgDBname;
			if (count($wgFunctionStack))
				array_pop($wgFunctionStack);
			if (count($wgFunctionStack))
				setproctitle($wgFunctionStack[count($wgFunctionStack)-1] . " [$wgDBname]");
		}
	} else {
		function wfProfileIn( $fn = '' ) {}
		function wfProfileOut( $fn = '' ) {}
	}
	function wfGetProfilingOutput() {}
	function wfProfileClose() {}
}

$fname = 'Setup.php';
wfProfileIn( $fname );
wfProfileIn( $fname.'-includes');

//---------------

// initialize plug to deki api
// @deprecated guerrics: do not use this plug anymore, use DekiPlug::getInstance()
require_once('dream.php');
$wgDekiPlug = new Plug($wgDreamServer, 'php', $wgDreamHost);
$wgDekiPlug = $wgDekiPlug->At($wgDekiApi);

//---------------

// royk: these are out-bound urls inside the product
require_once('deki/core/product_urls.php');

// guerrics: include libraries
require_once('libraries/dom.php');

// UI core
require_once('WatchedItem.php');
require_once('GlobalFunctions.php');
require_once('Hooks.php');
require_once('deki/core/deki_namespace.php');
require_once('RecentChange.php'); 
require_once('Skin.php');
require_once('OutputPage.php');
require_once('LinkCache.php');
require_once('Title.php');
require_once('Article.php');
require_once('CommentPage.php');
require_once('MagicWord.php');
require_once('WebRequest.php');
require_once('Database.php');
require_once('ProxyTools.php');
require_once('TagsOld.php');
require_once('Config.php');

// MT (royk): Markup for the new messaging
require_once('SkinMessaging.php');

require_once('Breadcrumb.php');

/**
 * Include new deki objects
 * @author guerrics
 */
// include core library files
require_once('deki/core/xarray.php');
require_once('deki/core/xuri.php');
require_once('deki/core/deki_request.php');

require_once('deki/core/http_plug.php');
require_once('deki/core/dream_plug.php');
require_once('deki/core/deki_plug.php');
require_once('deki/core/deki_result.php');

// message handler for DekiResult
if (!class_exists('DekiMessage'))
{
	require_once('includes/deki_message.php');
}
require_once('deki/core/deki_form.php');
require_once('deki/core/deki_token.php');
require_once('deki/core/deki_mailer.php');

// objects represent api data
require_once('deki/core/objects/i_deki_api_object.php');
require_once('deki/core/objects/deki_site.php');
require_once('deki/core/objects/deki_license.php');
require_once('deki/core/objects/deki_license_mock.php');
require_once('deki/core/objects/deki_license_factory.php');
require_once('deki/core/objects/deki_properties.php');
require_once('deki/core/objects/deki_user_properties.php');
require_once('deki/core/objects/wiki_user_compat.php');
require_once('deki/core/objects/deki_user.php');
require_once('deki/core/objects/deki_role.php');
require_once('deki/core/objects/deki_group.php');
require_once('deki/core/objects/deki_service.php');
require_once('deki/core/objects/deki_auth_service.php');
require_once('deki/core/objects/deki_extension.php');
require_once('deki/core/objects/deki_ban.php');
require_once('deki/core/objects/deki_page_info.php');
require_once('deki/core/objects/deki_page_alert.php');
require_once('deki/core/objects/deki_page_rating.php');
require_once('deki/core/objects/deki_page_revision.php');
require_once('deki/core/objects/deki_page_properties.php');
require_once('deki/core/objects/deki_file_properties.php');
require_once('deki/core/objects/deki_tag.php');
require_once('deki/core/objects/deki_template_properties.php');
require_once('deki/core/objects/deki_site_properties.php');
require_once('deki/core/objects/deki_language.php');
require_once('deki/core/objects/deki_object.php');
require_once('deki/core/objects/deki_file.php');

// configure the plug object for deki objects
$DekiPlug = DekiPlug::NewPlug($wgDreamServer, 'php', $wgDreamHost);
$DekiPlug = $DekiPlug->AtRaw($wgDekiApi);
DekiPlug::setInstance($DekiPlug);
// unset DekiPlug to discourage usage
unset($DekiPlug);


wfProfileOut( $fname.'-includes');
wfProfileIn( $fname.'-misc1');
global $wgUser, $wgLang, $wgContLang, $wgOut, $wgTitle;
global $wgLangClass, $wgContLangClass;
global $wgArticle, $wgLinkCache;
global $wgMemc, $wgDebugLogFile;
global $wgCommandLineMode;
global $wgDebugDumpSql;
global $wgDBserver, $wgDBport, $wgDBuser, $wgDBpassword, $wgDBname, $wgDBtype;

global $wgConfiguring;
global $wgFullyInitialised;

$wgIP = wfGetIP();
$wgRequest = new WebRequest();


wfProfileOut( $fname.'-misc1');
wfProfileIn( $fname.'-memcached');

//RoyK: Keep here until we get caching done in API
/**
 * No shared memory
 * @package MediaWiki
 */
class FakeMemCachedClient {
	function add ($key, $val, $exp = 0) { return true; }
	function decr ($key, $amt=1) { return null; }
	function delete ($key, $time = 0) { return false; }
	function disconnect_all () { }
	function enable_compress ($enable) { }
	function forget_dead_hosts () { }
	function get ($key) { return null; }
	function get_multi ($keys) { return array_pad(array(), count($keys), null); }
	function incr ($key, $amt=1) { return null; }
	function replace ($key, $value, $exp=0) { return false; }
	function run_command ($sock, $cmd) { return null; }
	function set ($key, $value, $exp=0){ return true; }
	function set_compress_threshold ($thresh){ }
	function set_debug ($dbg) { }
	function set_servers ($list) { }
}
$wgMemc = new FakeMemCachedClient();


wfProfileOut( $fname.'-memcached');
wfProfileIn( $fname.'-SetupSession');

// MT (steveb): always initialize session; we need it to track the breadcrumbs
if (!$wgCommandLineMode && !$wgConfiguring)
{
	DekiUser::setupSession();
	$wgSessionStarted = true;
}
else
{
	$wgSessionStarted = false;
}

wfProfileOut( $fname.'-SetupSession');
wfProfileIn( $fname.'-database');

if (!$wgConfiguring) {
	//get all configs, stores in global $wgInstanceConfig
	wfLoadConfig();
	wfSetInstanceSettings();
}

if (!$wgConfiguring) {
	if(!empty($wgDBport))
		$wgDBserver .= ':'.$wgDBport; 

	$wgDatabase = Database::newFromParams( $wgDBserver, $wgDBuser, $wgDBpassword, $wgDBname );
}

wfProfileOut( $fname.'-database');
wfProfileIn( $fname.'-language1');

//todo: find a better home?
require_once( "$IP/languages/Language.php" );

wfProfileOut( $fname.'-language1');
wfProfileIn( $fname.'-User');

wfProfileOut( $fname.'-User');
wfProfileIn( $fname.'-language2');

function setupLangObj($langclass) {
	global $IP;

	if( ! class_exists( $langclass ) ) {
		# Default to English/UTF-8
		$baseclass = 'LanguageUtf8';
		require_once( "$IP/languages/LanguageUtf8.php" );
		$lc = strtolower(substr($langclass, 8));
		$snip = "
			class $langclass extends $baseclass {
				function getVariants() {
					return array(\"$lc\");
				}

			}";
		eval($snip);
	}

	$lang = new $langclass();
	return $lang;
}

# $wgLanguageCode may be changed later to fit with user preference.
# The content language will remain fixed as per the configuration,
# so let's keep it.
$wgContLanguageCode = $wgLanguageCode;
$wgContLangClass = 'Language' . str_replace( '-', '_', ucfirst( $wgContLanguageCode ) );

$wgContLang = setupLangObj( $wgContLangClass );
$wgContLang->initEncoding();

if ($wgCommandLineMode || $wgConfiguring)
{
	// Used for some maintenance scripts; user session cookies can screw things up
	// when the database is in an in-between state.
}
else
{
	$wgUser = DekiUser::getCurrent();
}

if (!$wgConfiguring)
{
	if (!$wgUser->isAnonymous() && $wgUser->isDisabled())
	{
		wfMessagePush('general', wfMsg('System.Error.account-deactivated'), 'error');
		DekiUser::logout();
		$wgUser = DekiUser::getAnonymous();
	}
}

// override the defaultsettings.php language code with the user's
if ( !$wgConfiguring && !$wgUser->isAnonymous() && !empty($wgLanguagesAllowed) ) 
{
	$userLanguage = $wgUser->getLanguage();
	if (!is_null($userLanguage))
	{
		$wgLanguageCode = $userLanguage;
	}
}

// create the language class
$wgLangClass = 'Language'. str_replace( '-', '_', ucfirst( $wgLanguageCode ) );
// TODO (guerrics): audit language loading
if( $wgLangClass == $wgContLangClass ) {
	$wgLang = &$wgContLang;
} else {
	wfSuppressWarnings();
	@include_once("$IP/languages/$wgLangClass.php");
	wfRestoreWarnings();
	$wgLang = setupLangObj( $wgLangClass );
}

wfProfileOut( $fname.'-language2');


wfProfileIn( $fname.'-OutputPage');

if(!$wgConfiguring) {
	$wgOut = new OutputPage();
}

wfProfileOut( $fname.'-OutputPage');


wfProfileIn( $fname.'-misc2');

$wgLinkCache = new LinkCache();
$wgMagicWords = array();
if (!$wgConfiguring) 
{
	$wgMwRedir =& MagicWord::get( MAG_REDIRECT );
}

# Placeholders in case of DB error
$wgTitle = Title::makeTitle( NS_SPECIAL, 'Error');
$wgArticle = new Article($wgTitle);

wfProfileOut( $fname.'-misc2');

$wgFullyInitialised = true;
wfProfileOut( $fname );

endif;

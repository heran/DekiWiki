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

if( defined('MINDTOUCH_DEKI') ) {

#
# In general you should not make customizations in these language files
# directly, but should use the MediaWiki: special namespace to customize
# user interface messages through the wiki.
# See http://meta.wikipedia.org/wiki/MediaWiki_namespace
#
# NOTE TO TRANSLATORS: Do not copy this whole file when making translations!
# A lot of common constants and a base class with inheritable methods are
# defined here, which should not be redefined. See the other LanguageXx.php
# files for examples.
#

#--------------------------------------------------------------------------
# Language-specific text
#--------------------------------------------------------------------------

# The names of the namespaces can be set here, but the numbers
# are magical, so don't change or move them!  The Namespace class
# encapsulates some of the magic-ness.
#

if (!isset($wgMetaNamespace) || $wgMetaNamespace === false)
{
	global $wgSitename;
	$wgMetaNamespace = str_replace(' ', '_', (isset($wgSitename) ? $wgSitename : ''));
}

/* private */ $wgNamespaceNamesEn = array(
	NS_ADMIN            => 'Admin',
	NS_MEDIA            => 'Media',
	NS_SPECIAL          => 'Special',
	NS_MAIN	            => '',
	NS_TALK	            => 'Talk',
	NS_USER             => 'User',
	NS_USER_TALK        => 'User_talk',
	NS_PROJECT          => 'Project',
	NS_PROJECT_TALK    => 'Project_talk',
	NS_IMAGE_TALK       => 'Image_comments',
	NS_MEDIAWIKI        => 'MediaWiki',
	NS_TEMPLATE         => 'Template',
	NS_TEMPLATE_TALK    => 'Template_talk',
	NS_HELP             => 'Help',
	NS_HELP_TALK        => 'Help_talk',
	NS_CATEGORY         => 'Category',
	NS_CATEGORY_TALK    => 'Category_comments',
	NS_ATTACHMENT       => 'File',
);

if(isset($wgExtraNamespaces)) {
	$wgNamespaceNamesEn=$wgNamespaceNamesEn+$wgExtraNamespaces;
}

/* private */ $wgDefaultUserOptionsEn = array(
	'skin' => isset($wgDefaultSkin) ? $wgDefaultSkin : 'ace', 
	// MT (steveb): by default breadcrumbs have a length of 5
	'bclen' => 5
);

# Whether to use user or default setting in Language::date()
define( 'MW_DATE_USER_FORMAT', true );

/* private */ $wgUserTogglesEn = array(
	'hover',
	'numberheadings',
	'rememberpassword',
	'advancededitor'
);

# Read language names
global $wgLanguageNames;
require_once( 'Names.php' );

$wgLanguageNamesEn =& $wgLanguageNames;


/* private */ $wgWeekdayNamesEn = array(
	'sunday', 'monday', 'tuesday', 'wednesday', 'thursday',
	'friday', 'saturday'
);


/* private */ $wgMonthNamesEn = array(
	'january', 'february', 'march', 'april', 'may_long', 'june',
	'july', 'august', 'september', 'october', 'november',
	'december'
);
/* private */ $wgMonthNamesGenEn = array(
	'january-gen', 'february-gen', 'march-gen', 'april-gen', 'may-gen', 'june-gen',
	'july-gen', 'august-gen', 'september-gen', 'october-gen', 'november-gen',
	'december-gen'
);

/* private */ $wgMonthAbbreviationsEn = array(
	'jan', 'feb', 'mar', 'apr', 'may', 'jun', 'jul', 'aug',
	'sep', 'oct', 'nov', 'dec'
);

# Note to translators:
#   Please include the English words as synonyms.  This allows people
#   from other wikis to contribute more easily.
#

if (!isset($wgConfiguring) || !$wgConfiguring) 
{
	/* private */ $wgMagicWordsEn = array(
	#   ID                                 CASE  SYNONYMS
		MAG_REDIRECT             => array( 0,    '#redirect', '#symbol'   ),
	);
}

#-------------------------------------------------------------------
# Default messages
#-------------------------------------------------------------------
# Allowed characters in keys are: A-Z, a-z, 0-9, underscore (_) and
# hyphen (-). If you need more characters, you may be able to change
# the regex in MagicWord::initRegex

# required for copyrightwarning
global $wgRightsText;

/**
 * @param name File name of the resource file to load
 * @return
 *
 * @note (guerrics) Function has been updated to work with namespaced keys
 */
function wfLoadLanguageResource($name)
{
	global $wgAllMessagesEn, $wgJavascriptMessages, $wgResourcesDirectory;

	$file = $wgResourcesDirectory . '/' . $name;
	if(file_exists($file))	
	{
		$contents = file($file);
		$namespace = '';

		if (isset($contents[0]))
		{
			// check for utf-8 BOM
			$bom = bin2hex(substr($contents[0], 0, 3));
			if ($bom == 'efbbbf')
			{
				// remove the utf-8 bom from the first line
				$contents[0] = substr($contents[0], 3);
			}
		}


		foreach($contents as $line)
		{
			$line = trim($line); // allows comments & keys to be indented
			// check for a comment or blank line
			if((strlen($line) != 0) && (strncmp($line, ';', 1) != 0))
			{
				// check if this is a namespace definition
				if (strncmp($line, '[', 1) == 0)
				{
					// verify the key has a trailing bracket
					if (strncmp(substr($line, -1), ']', 1) != 0)
					{
						printf("<div>WARNING [Resources.txt]: Namespace definition is missing closing bracket on line: %s</div>", $line);
						$namespace = substr($line, 1);
					}
					else
					{
						// strip first and last character
						$namespace = substr($line, 1, -1);
					}
					// set the new namespace
					$namespace = strtolower($namespace);
				}
				else
				{
					@list($key, $value) = explode('=', $line, 2);
					if (!isset($value))
					{
						printf("<div>WARNING [Resources.txt]: Missing '=' for line: %s</div>", $line);
					}
					$key = strtolower(trim($key));

					// check if we are in a namespace, warn if not
					if (empty($namespace))
					{
						printf("<div>WARNING [Resources.txt]: Empty namespace for key: %s, value: %s</div>", $key, $value);
						$wgAllMessagesEn[$key] = $value;
					}
					// check if this is a javascript message
					else if ($namespace == 'dialog.js')
					{
						$wgJavascriptMessages[$key] = $value;
					}
					else
					{
						$wgAllMessagesEn[$namespace .'.'. $key] = $value;
					}
				}
			}
		}
	}
}

/*
 * Load a resource file from disk into a string
 * @param string $template - template name to search ('email.resource')
 * @return string - text of resource
 */
function wfLoadTemplateResource($template){
	global $wgResourcesDirectory;
	
	$text = null;
	$templatePath = $wgResourcesDirectory . '/templates/' . $template;
	$customTemplatePath = $templatePath . '.custom';
	
	$path = is_file($customTemplatePath) ? $customTemplatePath : $templatePath;
	return wfGetFileContent($path);
}

function wfLoadLanguageResources() {
	global $wgLanguageCode, $wgCacheDirectory, $wgResourcesDirectory;
	global $wgAllMessagesEn, $wgJavascriptMessages;
	
	// defines the language cache file - serialized text
	$cacheFile = $wgCacheDirectory.'/cache-language-'.$wgLanguageCode;
	
	// determine which languages to load
	$load = array('resources.txt');
	if ($wgLanguageCode) 
	{
		$lang = strtolower($wgLanguageCode);
		$culture = explode('-', $lang, 2);
		if ($culture[0] != $lang) 
		{
			// load culture and sub-culture resources files
			$load[] = 'resources.' . $culture[0] . '.txt';
			$load[] = 'resources.' . $lang . '.txt';
		} 
		else 
		{
			// load culture
			$load[] = 'resources.' . $lang . '.txt';
		}
	}
	
	// load custom resource files
	$load[] = 'resources.custom.txt';
	if ($wgLanguageCode) 
	{
		// $culture & $lang from above
		if ($culture[0] != $lang) 
		{
			// load culture and sub-culture custom resources files
			$load[] = 'resources.custom.' . $culture[0] . '.txt';
			$load[] = 'resources.custom.' . $lang . '.txt';
		} 
		else 
		{
			// load culture
			$load[] = 'resources.custom.' . $lang . '.txt';
		}
	}
	
	//get last modified time
	$timestamp = 0;
	foreach ($load as $file)
	{
		$file = $wgResourcesDirectory . '/' . $file;
		if (!is_file($file)) 
		{
			continue;
		}
		$mtime = filemtime($file);
		if ($mtime > $timestamp) 
		{
			$timestamp = $mtime;	
		}
	}
	if (is_file($cacheFile) && filemtime($cacheFile) > $timestamp) 
	{
		$lang = unserialize(wfGetFileContent($cacheFile));
		if (is_array($lang)) 
		{
			// preserve any existing keys that may have been loaded from plugins into $wgAllMessagesEn
			$wgAllMessagesEn = array_merge($lang[0], (array)$wgAllMessagesEn);
			$wgJavascriptMessages = $lang[1];
			return;
		}
	}
	
	// if we couldn't hit the cache sucessfully, re-parse the language source
	foreach ($load as $file) 
	{
		wfLoadLanguageResource($file);	
	}
	
	// write the cache file in a serialized format
	wfSetFileContent($cacheFile, serialize(array($wgAllMessagesEn, $wgJavascriptMessages)));
}

function wfCurrentUserLanguage($lang = 'en-us') 
{
	if (isset($_SERVER['HTTP_ACCEPT_LANGUAGE'])) 
	{
		list($lang) = explode(',', $_SERVER['HTTP_ACCEPT_LANGUAGE']);
		//maybe we need to return the variant later so do 2 separate substr
		$userlang = substr($lang, 0, 2);
		$uservariant = substr($lang, 3, 2);
		return $userlang.'-'.(empty($uservariant) ? $userlang: $uservariant);
	}
	return $lang;
}

// todo: audit this method
function wfLanguageActive($return = null) 
{
	global $wgArticle, $wgUser, $wgRequest;
	
	//sending in a blank language parameter will search all languages
	if (!is_null($wgRequest->getVal('language'))) 
	{
		return $wgRequest->getVal('language');	
	}
	if (is_object($wgArticle) && !is_null($wgArticle->getLanguage())) 
	{
		return $wgArticle->getLanguage();	
	}
	
	// attempt to get the user's language preference
	global $wgLanguagesAllowed;
	if (!empty($wgLanguagesAllowed) && !$wgUser->isAnonymous()) 
	{
		$language = $wgUser->getLanguage();
		if (!is_null($language)) 
		{
			return $language;
		}
	}
	
	global $wgLanguageCode;
	//the default language may not even match against an allowed language!
	//this manifests itself when you search - it will search all namespaces :(
	if (!empty($wgLanguagesAllowed)) 
	{
		if (!array_key_exists($wgLanguageCode, wfAllowedLanguages())) 
		{
			//if a variant, try splitting
			if (strpos($wgLanguageCode, '-') !== false) 
			{
				list($language, $variant) = explode('-', $wgLanguageCode);
				if (array_key_exists($language, wfAllowedLanguages())) 
				{
					return $language;
				}
			}
			//else, what can we do???
		}
	}
	return is_null($return) ? $wgLanguageCode: $return;
}
/**
 * Retrieve the available site languages
 * 
 * @param string $prepend - default option display text
 * @param $prependValue - default option value
 * @return array
 */
function wfAllowedLanguages($prepend = null, $prependValue = '') 
{
	static $languages;
	if (!is_null($languages)) {
		
		if (!is_null($prepend) && !array_key_exists('', $languages)) 
		{
			if (array_key_exists($prependValue, $languages)) 
			{
				$languages[$prependValue] = $prepend;
			}
			else 
			{
				$languages = array($prependValue => $prepend) + $languages;
			}
		}
		return $languages;
	}

	require_once('LanguageList.php');
	$languages = array();
	global $wgLanguagesAllowed;
	if (is_null($wgLanguagesAllowed)) {
		$languages = array();
		return $languages;
	}

	$l = explode(',', $wgLanguagesAllowed);
	if (!is_null($prepend)) {
		$languages[$prependValue] = $prepend;	
	}

	foreach ($l as $val) {
		$languages[$val] = $wgLanguageList[$val];
	}

	return $languages;	
}

function wfAvailableResourcesLanguages() 
{
	global $wgResourceLanguageNames, $wgResourcesDirectory;
	
	//this function may be called multiple times, so let's not hit the file-server so often
	static $result;
	if (!empty($result) > 0) 
	{
		return $result;	
	}
	
	$result = array();
	foreach($wgResourceLanguageNames as $code => $description) 
	{
		if (file_exists($wgResourcesDirectory . '/resources.' . $code . '.txt')) 
		{
			$result[$code] = $description;
		}
	}
	return $result;
}

// load general resources.txt file
$wgAllMessagesEn = array();
$wgJavascriptMessages = array();

#--------------------------------------------------------------------------
# Internationalisation code
#--------------------------------------------------------------------------

class Language {
	function Language(){
		# Copies any missing values in the specified arrays from En to the current language
		$fillin = array( 'wgSysopSpecialPages', 'wgValidSpecialPages', 'wgDeveloperSpecialPages' );
		$name = get_class( $this );
		if( strpos( $name, 'language' ) == 0){
			$lang = ucfirst( substr( $name, 8 ) );
			foreach( $fillin as $arrname ){
				$langver = "{$arrname}{$lang}";
				$enver = "{$arrname}En";
				if( ! isset( $GLOBALS[$langver] ) || ! isset( $GLOBALS[$enver] ))
					continue;
				foreach($GLOBALS[$enver] as $spage => $text){
					if( ! isset( $GLOBALS[$langver][$spage] ) )
						$GLOBALS[$langver][$spage] = $text;
				}
			}
		}
	}

	function getDefaultUserOptions () {
		global $wgDefaultUserOptionsEn;
		return $wgDefaultUserOptionsEn;
	}

	function getNamespaces() {
		global $wgNamespaceNamesEn;
		return $wgNamespaceNamesEn;
	}

	function getNsText( $index ) {
		global $wgNamespaceNamesEn;
		return isset($wgNamespaceNamesEn[$index]) ? $wgNamespaceNamesEn[$index]: '';
	}

	function getNsIndex( $text ) {
		global $wgNamespaceNamesEn;

		foreach ( $wgNamespaceNamesEn as $i => $n ) {
			if ( 0 == strcasecmp( $n, $text ) ) { return $i; }
		}
		return false;
	}

	# short names for language variants used for language conversion links.
	# so far only used by zh
	function getVariantname( $code ) {
		return wfMsgUTF8( 'variantname-' . $code );
	}

	function specialPage( $name ) {
		return $this->getNsText(NS_SPECIAL) . ':' . $name;
	}

	function getUserToggles() {
		global $wgUserTogglesEn;
		return $wgUserTogglesEn;
	}

	function getLanguageName( $code ) {
		global $wgLanguageNamesEn;
		if ( ! array_key_exists( $code, $wgLanguageNamesEn ) ) {
			return "";
		}
		return $wgLanguageNamesEn[$code];
	}

	function getMonthAbbreviation( $key ) {
		global $wgMonthAbbreviationsEn, $wgContLang;
		// see who called us and use the correct message function
		if( get_class( $wgContLang->getLangObj() ) == get_class( $this ) )
			return wfMsgForContentUTF8('System.Common.' .(@$wgMonthAbbreviationsEn[$key-1]));
		else
			return wfMsgUTF8('System.Common.' .(@$wgMonthAbbreviationsEn[$key-1]));
	}

	function userAdjust( $ts )
	{
		global $wgUser;

		$tz = $wgUser->getTimezone(true);
		
		if ( strpos( $tz, ':' ) !== false ) {
			$tzArray = explode( ':', $tz );
			$hrDiff = intval($tzArray[0]);
			$minDiff = intval($hrDiff < 0 ? -$tzArray[1] : $tzArray[1]);
		} else {
			$hrDiff = intval( $tz );
			$minDiff = 0;
		}
		if ( 0 == $hrDiff && 0 == $minDiff ) { return $ts; }
		$t = mktime( (
		  (int)substr( $ts, 8, 2) ) + $hrDiff, # Hours
		  (int)substr( $ts, 10, 2 ) + $minDiff, # Minutes
		  (int)substr( $ts, 12, 2 ), # Seconds
		  (int)substr( $ts, 4, 2 ), # Month
		  (int)substr( $ts, 6, 2 ), # Day
		  (int)substr( $ts, 0, 4 ) ); #Year
		return date( 'YmdHis', $t );
	}

	function date( $ts, $adj = false, $format = MW_DATE_USER_FORMAT ) {
		global $wgAmericanDates, $wgUser, $wgUseDynamicDates;

		$ts=wfTimestamp(TS_MW,$ts);
		if ( $adj ) { $ts = $this->userAdjust( $ts ); }
		if ( $wgUseDynamicDates ) {
			if ( $format == MW_DATE_USER_FORMAT ) {
				$datePreference = $wgUser->getOption( 'date' );
			} else {
				$options = $this->getDefaultUserOptions();
				$datePreference = $options['date'];
			}
			if ( $datePreference == 0 ) {
				$datePreference = $wgAmericanDates ? 1 : 2;
			}
		} else {
			$datePreference = $wgAmericanDates ? 1 : 2;
		}

		$month = $this->getMonthAbbreviation( substr( $ts, 4, 2 ) );
		$day = $this->formatNum( 0 + substr( $ts, 6, 2 ) );
		$year = $this->formatNum( substr( $ts, 0, 4 ) );

		return wfMsg('System.Common.format-date', $day, $month, $year);
	}

	function time( $ts, $adj = false, $seconds = false ) {
		$ts=wfTimestamp(TS_MW,$ts);

		if ( $adj ) { $ts = $this->userAdjust( $ts ); }

		$hour = substr( $ts, 8, 2 );
		$minute = substr( $ts, 10, 2 );
		$second = '';
		if ( $seconds ) {
			$second = ':'.substr( $ts, 12, 2 ); //not localized
		}
		return $this->formatNum( wfMsg('System.Common.format-time', $hour, $minute, $second ));
	}

	function timeanddate( $ts, $adj = false, $format = MW_DATE_USER_FORMAT ) {
		$ts=wfTimestamp(TS_MW,$ts);

		return wfMsg('System.Common.format-datetime', $this->time( $ts, $adj ), $this->date( $ts, $adj, $format ));
	}

	/**
	 * Check whether key exists as resource
	 */
	function keyExists($key) 
	{
		global $wgAllMessagesEn;
		
		// could be called before language files are loaded
		if (empty($wgAllMessagesEn))
		{
			// load dummy key to kick off process
			wfMsg('');
		}
		
		return isset($wgAllMessagesEn[strtolower($key)]);	
	}
	/**
	 * @param key Namespaced key <code>system.error.key-name</code>
	 * @return string Translated message
	 *
	 * @note (guerrics) Function has been updated to work with namespaced keys
	 */
	function getMessage($key)
	{
		global $wgAllMessagesEn;

		$key = strtolower($key);
		if (!isset($wgAllMessagesEn[$key]))
		{
			return '[MISSING: '. htmlspecialchars($key) .']';
		}

		return @$wgAllMessagesEn[$key];
	}

	function iconv( $in, $out, $string ) {
		# For most languages, this is a wrapper for iconv
		return iconv( $in, $out, $string );
	}

	function ucfirst( $string ) {
		# For most languages, this is a wrapper for ucfirst()
		return ucfirst( $string );
	}

	function lcfirst( $s ) {
		return strtolower( $s{0}  ). substr( $s, 1 );
	}

	function checkTitleEncoding( $s ) {
		global $wgInputEncoding;

		# Check for UTF-8 URLs; Internet Explorer produces these if you
		# type non-ASCII chars in the URL bar or follow unescaped links.
		$ishigh = preg_match( '/[\x80-\xff]/', $s);
		$isutf = ($ishigh ? preg_match( '/^([\x00-\x7f]|[\xc0-\xdf][\x80-\xbf]|' .
		         '[\xe0-\xef][\x80-\xbf]{2}|[\xf0-\xf7][\x80-\xbf]{3})+$/', $s ) : true );

		if( ($wgInputEncoding != 'utf-8') and $ishigh and $isutf )
			return @iconv( 'UTF-8', $wgInputEncoding, $s );

		if( ($wgInputEncoding == 'utf-8') and $ishigh and !$isutf )
			return utf8_encode( $s );

		# Other languages can safely leave this function, or replace
		# it with one to detect and convert another legacy encoding.
		return $s;
	}

	function stripForSearch( $in ) {
		# Some languages have special punctuation to strip out
		# or characters which need to be converted for MySQL's
		# indexing to grok it correctly. Make such changes here.
		return strtolower( $in );
	}

	function convertForSearchResult( $termsArray ) {
		# some languages, e.g. Chinese, need to do a conversion
		# in order for search results to be displayed correctly
		return $termsArray;
	}

	function firstChar( $s ) {
		# Get the first character of a string. In ASCII, return
		# first byte of the string. UTF8 and others have to
		# overload this.
		return $s[0];
	}

	function initEncoding() {
		# Some languages may have an alternate char encoding option
		# (Esperanto X-coding, Japanese furigana conversion, etc)
		# If this language is used as the primary content language,
		# an override to the defaults can be set here on startup.
		#global $wgInputEncoding, $wgOutputEncoding, $wgEditEncoding;
	}

	function setAltEncoding() {
		# Some languages may have an alternate char encoding option
		# (Esperanto X-coding, Japanese furigana conversion, etc)
		# If 'altencoding' is checked in user prefs, this gives a
		# chance to swap out the default encoding settings.
		#global $wgInputEncoding, $wgOutputEncoding, $wgEditEncoding;
	}

	function recodeForEdit( $s ) {
		# For some languages we'll want to explicitly specify
		# which characters make it into the edit box raw
		# or are converted in some way or another.
		# Note that if wgOutputEncoding is different from
		# wgInputEncoding, this text will be further converted
		# to wgOutputEncoding.
		global $wgInputEncoding, $wgEditEncoding;
		if( $wgEditEncoding == '' or
		  $wgEditEncoding == $wgInputEncoding ) {
			return $s;
		} else {
			return $this->iconv( $wgInputEncoding, $wgEditEncoding, $s );
		}
	}

	function recodeInput( $s ) {
		# Take the previous into account.
		global $wgInputEncoding, $wgOutputEncoding, $wgEditEncoding;
		if($wgEditEncoding != "") {
			$enc = $wgEditEncoding;
		} else {
			$enc = $wgOutputEncoding;
		}
		if( $enc == $wgInputEncoding ) {
			return $s;
		} else {
			return $this->iconv( $enc, $wgInputEncoding, $s );
		}
	}

	# For right-to-left language support
	function isRTL() { return false; }

	function &getMagicWords() {
		global $wgMagicWordsEn;
		return $wgMagicWordsEn;
	}

	# Fill a MagicWord object with data from here
	function getMagic( &$mw ) {
		$raw =& $this->getMagicWords();
		if( !isset( $raw[$mw->mId] ) ) {
			# Fall back to English if local list is incomplete
			$raw =& Language::getMagicWords();
		}
		$rawEntry = $raw[$mw->mId];
		$mw->mCaseSensitive = $rawEntry[0];
		$mw->mSynonyms = array_slice( $rawEntry, 1 );
	}

	# Italic is unsuitable for some languages
	function emphasize( $text ) {
		return '<em>'.$text.'</em>';
	}


	# Normally we use the plain ASCII digits. Some languages such as Arabic will
	# want to output numbers using script-appropriate characters: override this
	# function with a translator. See LanguageAr.php for an example.
	function formatNum( $number ) {
		return $number;
	}

	function listToText( $l ) {
		$s = '';
		$m = count($l) - 1;
		for ($i = $m; $i >= 0; $i--) {
			if ($i == $m) {
				$s = $l[$i];
			} else if ($i == $m - 1) {
				$s = $l[$i] . ' ' . $this->getMessage('and') . ' ' . $s;
			} else {
				$s = $l[$i] . ', ' . $s;
			}
		}
		return $s;
	}

	# Crop a string from the beginning or end to a certain number of bytes.
	# (Bytes are used because our storage has limited byte lengths for some
	# columns in the database.) Multibyte charsets will need to make sure that
	# only whole characters are included!
	#
	# $length does not include the optional ellipsis.
	# If $length is negative, snip from the beginning
	function truncate( $string, $length, $ellipsis = '' ) {
		if( $length == 0 ) {
			return $ellipsis;
		}
		if ( strlen( $string ) <= abs( $length ) ) {
			return $string;
		}
		if( $length > 0 ) {
			$string = substr( $string, 0, $length );
			return $string . $ellipsis;
		} else {
			$string = substr( $string, $length );
			return $ellipsis . $string;
		}
	}

	# convert text to different variants of a language.
	function convert( $text , $isTitle=false) {
		return $text;
	}

	# returns a list of language variants for conversion.
	# right now mainly used in the Chinese conversion
	function getVariants() {
		$lang = strtolower( substr( get_class( $this ), 8 ) );
		return array( $lang );
	}

	function getPreferredVariant() {
		return strtolower( substr( get_class( $this ), 8 ) );
	}

	/**
	 * A regular expression to match legal word-trailing characters
	 * which should be merged onto a link of the form [[foo]]bar.
	 * FIXME
	 *
	 * @return string
	 * @access public
	 */
	function linkTrail() {
		$trail = $this->getMessage('Skin.Common.regex-link-trail');
		// @note MT (guerrics) Line below commented out since
		//			getmessage will fail if language not an instance
		//if( empty( $trail ) ) $trail = Language::linkTrail();
		return $trail;
	}

	function getLangObj() {
		return $this;
	}
}

}
?>

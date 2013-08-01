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
 * See title.doc
 * 
 * @package MediaWiki
 */

/** */
require_once( 'normal/UtfNormal.php' );

$wgTitleInterwikiCache = array();
define ( 'GAID_FOR_UPDATE', 1 );
if (!defined('HPS_SEPARATOR'))
    define( 'HPS_SEPARATOR', '/' );

# Title::newFromTitle maintains a cache to avoid
# expensive re-normalization of commonly used titles.
# On a batch operation this can become a memory leak
# if not bounded. After hitting this many titles,
# reset the cache.
define( 'MW_TITLECACHE_MAX', 1000 );

/**
 * Title class
 * - Represents a title, which may contain an interwiki designation or namespace
 * - Can fetch various kinds of data from the database, albeit inefficiently. 
 *
 * @package MediaWiki
 */
class Title {
	/**
	 * All member variables should be considered private
	 * Please use the accessor functions
	 */

	 /**#@+
	 * @access private
	 */

	var $mTextform;           # Text form (spaces not underscores) of the main part
	var $mUrlform;            # URL-encoded form of the main part
	var $mDbkeyform;          # Main part with underscores
	var $mNamespace;          # Namespace index, i.e. one of the NS_xxxx constants
	var $mInterwiki;          # Interwiki prefix (or null string)
	var $mFragment;           # Title fragment (i.e. the bit after the #)
	var $mArticleID;          # Article ID, fetched from the link cache on demand
	var $mRestrictions;       # Array of groups allowed to edit this article
                              # Only null or "sysop" are supported
	var $mRestrictionsLoaded; # Boolean for initialisation on demand
	var $mRestricted; 		  # Boolean for whether a page is restricted
	var $mPrefixedText;       # Text form including namespace/interwiki, initialised on demand
	var $mDefaultNamespace;   # Namespace index when there is no namespace
                              # Zero except in {{transclusion}} tags
    var $mRedirect;           # true if redirect
    var $mRedirectForbidden;  # true if cannot be a redirect
	/**#@-*/
	

	/**
	 * Constructor
	 * @access private
	 */
	/* private */ function Title() {
		$this->mInterwiki = $this->mUrlform =
		$this->mTextform = $this->mDbkeyform = '';
		$this->mArticleID = -1;
		$this->mNamespace = 0;
		$this->mRestrictions = array();
		$this->mRestriction = false;
		$this->mRestrictionsLoaded = false;
		$this->mDefaultNamespace = 0;
		$this->mRedirect = false;
		$this->mRedirectForbidden = false;
	}

	/**
	 * Create a new Title from a prefixed DB key
	 * @param string $key The database key, which has underscores
	 *	instead of spaces, possibly including namespace and
	 *	interwiki prefixes
	 * @return Title the new object, or NULL on an error
	 * @static
	 * @access public
	 */
	/* static */ function newFromDBkey( $key ) {
		$t = new Title();
		$t->mDbkeyform = $key;
		if( $t->secureAndSplit() )
			return $t;
		else
			return NULL;
	}
	
	/**
	 * Create a new Title from text, such as what one would
	 * find in a link. Decodes any HTML entities in the text.
	 *
	 * @param string $text the link text; spaces, prefixes,
	 *	and an initial ':' indicating the main namespace
	 *	are accepted
	 * @param int $defaultNamespace the namespace to use if
	 * 	none is specified by a prefix
	 * MT ursm: add context to allow resolving ./ and ../
	 * @param Title $context
	 * @return Title the new object, or NULL on an error
	 * @static
	 * @access public
	 */
	/* static */ function &newFromText( $text, $defaultNamespace = 0, $context = null ) {	
		// temporary workaround until we fix the core issue
		if (is_array($text))
		{
			$text = $text['#text'];
		}
		/**
		 * Wiki pages often contain multiple links to the same page.
		 * Title normalization and parsing can become expensive on
		 * pages with many links, so we can save a little time by
		 * caching them.
		 *
		 * In theory these are value objects and won't get changed...
		 */
		static $titleCache = array();
		if( $defaultNamespace == 0 && isset( $titleCache[$text] ) ) {
			return $titleCache[$text];
		}

		/**
		 * Convert things like &eacute; into real text...
		 */
 		global $wgInputEncoding;
		$text = str_replace(array('&nbsp;',' '),'_',rawurldecode($text));
		$filteredText = do_html_entity_decode( $text, ENT_COMPAT, $wgInputEncoding );

		/**
		 * Convert things like &#257; or &#x3017; into real text...
		 * WARNING: Not friendly to internal links on a latin-1 wiki.
		 */
		$filteredText = wfMungeToUtf8( $filteredText );

		// MT UrsM: allow special chars by encoding them
        $filteredText = wfEncodeTitle( $filteredText, false, true );
        
		$t = new Title();
		$t->mDbkeyform = str_replace( ' ', '_', $filteredText );
		$t->mDefaultNamespace = $defaultNamespace;

		if( $t->secureAndSplit( $context ) ) {
			if( $defaultNamespace == 0 ) {
				if( count( $titleCache ) >= MW_TITLECACHE_MAX ) {
					# Avoid memory leaks on mass operations...
					$titleCache = array();
				}
				$titleCache[$text] =& $t;
			}
			return $t;
		} else {
			$return = null;
			return $return;
		}
	}
	
	/**
	 * Create a new Title from URL-encoded text. Ensures that
	 * the given title's length does not exceed the maximum.
	 * @param string $url the title, as might be taken from a URL
	 * @return Title the new object, or NULL on an error
	 * @static
	 * @access public
	 */
	/* static */ function newFromURL( $url, $context = null ) {
		global $wgLang, $wgServer;
		$t = new Title();
		
		# For compatibility with old buggy URLs. "+" is not valid in titles,
		# but some URLs used it as a space replacement and they still come
		# from some external search tools.
        $s = wfEncodeTitle( $url, false, true );

		$t->mDbkeyform = str_replace( ' ', '_', $s );
		if( $t->secureAndSplit( $context ) ) {
			return $t;
		} else {
			return NULL;
		}
	}
	
	/**
	 * Create a new Title from an article ID
	 * @todo This is inefficiently implemented, the cur row is requested
	 * but not used for anything else
	 * @param int $id the page_id corresponding to the Title to create
	 * @return Title the new object, or NULL on an error
	 * @access public
	 */
	/* static */ function newFromID( $id ) {
		if(!is_numeric($id)) {
			return NULL;
		}
		$fname = 'Title::newFromID';
		$dbr =& wfGetDB( DB_SLAVE );
		$row = $dbr->selectRow( 'pages', array( 'page_namespace', 'page_title' ), 
			array( 'page_id' => $id ), $fname );
		if ( $row !== false ) {
			$title = Title::makeTitle( $row->page_namespace, $row->page_title, $id);
		} else {
			$title = NULL;
		}
		return $title;
	}
	
	/**
	 * Create a new Title from a namespace index and a DB key.
	 * It's assumed that $ns and $title are *valid*, for instance when
	 * they came directly from the database or a special page name.
	 * For convenience, spaces are converted to underscores so that
	 * eg user_text fields can be used directly.
	 *
	 * @param int $ns the namespace of the article
	 * @param string $title the unprefixed database key form
	 * @return Title the new object
	 * @static
	 * @access public
	 */
	/* static */ function &makeTitle( $ns, $title, $id = -1 ) {
		$t = new Title();
		$t->mInterwiki = '';
		$t->mFragment = '';
		$t->mNamespace = intval( $ns );
		$t->mDbkeyform = str_replace( ' ', '_', $title );
		$t->mArticleID = ( $ns >= 0 ) ? $id : 0;
		$t->mUrlform = wfUrlencode( $t->mDbkeyform );
		$t->mTextform = str_replace( '_', ' ', $title );
		return $t;
	}

	/**
	 * Create a new Title from a namespace index and a DB key.
	 * The parameters will be checked for validity, which is a bit slower
	 * than makeTitle() but safer for user-provided data.
	 * @param int $ns the namespace of the article
	 * @param string $title the database key form
	 * @return Title the new object, or NULL on an error
	 * @static
	 * @access public
	 */
	/* static */ function makeTitleSafe( $ns, $title ) {
		$t = new Title();
		$t->mDbkeyform = Title::makeName( $ns, $title );
		if( $t->secureAndSplit() ) {
			return $t;
		} else {
			return NULL;
		}
 	}

	/**
	 * Create a new Title for a redirect
	 * @param string $text the redirect title text
	 * @return Title the new object, or NULL if the text is not a
	 *	valid redirect
	 * @static
	 * @access public
	 */
	/* static */ function newFromRedirect( $text, $followSymbols = true ) {
		global $wgMwRedir;
		$rt = NULL;
		if ( $wgMwRedir->matchStart( $text ) ) {
			if ( preg_match( '/\[{2}(.*?)(?:\||\]{2})/', $text, $m ) ) {
				# categories are escaped using : for example one can enter:
				# #REDIRECT [[:Category:Music]]. Need to remove it.
				if ( substr($m[1],0,1) == ':') {
					# We don't want to keep the ':'
					$m[1] = substr( $m[1], 1 );
				}
				
				global $wgTitle;
				$rt = Title::newFromText( $m[1], 0, $wgTitle );
				if ($rt->isRedirectForbidden())
				{
					return NULL;
				}
				if ( !$followSymbols && !is_null($rt) && preg_match("/^#symbol/i", $text))
				    $rt = NULL;
				# Disallow redirects to Special:Userlogout
				if ( !is_null($rt) && $rt->getNamespace() == NS_SPECIAL && preg_match( '/^Userlogout/i', $rt->getText() ) ) {
					$rt = NULL;
				}
			}
		}
		return $rt;
	}
	
#----------------------------------------------------------------------------
#	Static functions
#----------------------------------------------------------------------------

	/**
	 * Get a regex character class describing the legal characters in a link
	 * @return string the list of characters, not delimited
	 * @static
	 * @access public
	 */
	/* static */ function legalChars() {
		# Missing characters:
		#  * []|# Needed for link syntax
		#  * % and + are corrupted by Apache when they appear in the path
		#
		# % seems to work though
		#
		# The problem with % is that URLs are double-unescaped: once by Apache's 
		# path conversion code, and again by PHP. So %253F, for example, becomes "?".
		# Our code does not double-escape to compensate for this, indeed double escaping
		# would break if the double-escaped title was passed in the query string
		# rather than the path. This is a minor security issue because articles can be
		# created such that they are hard to view or edit. -- TS
		#
		# Theoretically 0x80-0x9F of ISO 8859-1 should be disallowed, but
		# this breaks interlanguage links
		
		$set = " %!\"$&'()*,\\-.\\/0-9:;=?@A-Z>\\\\^_`a-z~\\x80-\\xFF";
		return $set;
	}
	
		
	/*
	 * Make a prefixed DB key from a DB key and a namespace index
	 * @param int $ns numerical representation of the namespace
	 * @param string $title the DB key form the title
	 * @return string the prefixed form of the title
	 */
	/* static */ function makeName( $ns, $title ) {
		global $wgContLang;

		$n = $wgContLang->getNsText( $ns );
		if ( '' == $n ) { return $title; }
		else { return $n.':'.$title; }
	}
	
	/**
	* Returns the URL associated with an interwiki prefix
	* @param string $key the interwiki prefix (e.g. "MeatBall")
	* @return the associated URL, containing "$1", which should be
	*      replaced by an article title
	* @static (arguably)
	* @access public
	*/
	function getInterwikiLink( $key ) {
		return '';
	}

	/**
	 * Determine whether the object refers to a page within
	 * this project. 
	 * 
	 * @return bool TRUE if this is an in-project interwiki link
	 *	or a wikilink, FALSE otherwise
	 * @access public
	 */
	function isLocal() {
		return true;
	}

#----------------------------------------------------------------------------
#	Other stuff
#----------------------------------------------------------------------------

	/** Simple accessors */
	/**
	 * Get the text form (spaces not underscores) of the main part
	 * @return string
	 * @access public
	 */
	function getText() { return $this->mTextform; }
	/**
	 * Get the URL-encoded form of the main part
	 * @return string
	 * @access public
	 */
	function getPartialURL() { return $this->mUrlform; }
	/**
	 * Get the main part with underscores
	 * @return string
	 * @access public
	 */
	function getDBkey() { return $this->mDbkeyform; }
	
	function getPathlessText() {
        Article::splitName($this->getPrefixedText(), $titlePath, $titleName);
        $titleName = wfDecodeTitle($titleName);
        if ($titleName == '') {
	        $titleName = wfHomePageTitle();
        }
        return $titleName;
	}
	
	function getPrefix() {
		return DekiNamespace::getCanonicalName($this->getNamespace()).':';	
	}
	
	/***
	 * Get the display name; only use for HTML output
	 */
	function getDisplayText() 
	{
		$displayTitle = $this->getPrefixedText() == wfHomePageInternalTitle() 
			? wfHomePageTitle() 
			: wfDecodeTitle($this->getPrefixedText());
		return htmlspecialchars($displayTitle); 
	}
	
	/**
	 * Get the namespace index, i.e. one of the NS_xxxx constants
	 * @return int
	 * @access public
	 */
	function getNamespace() { return $this->mNamespace; }
	/**
	 * Get the Title fragment (i.e. the bit after the #)
	 * @return string
	 * @access public
	 */
	function getFragment() { return $this->mFragment; }
	/**
	 * Get the default namespace index, for when there is no namespace
	 * @return int
	 * @access public
	 */
	function getDefaultNamespace() { return $this->mDefaultNamespace; }

	/**
	 * Get the prefixed database key form
	 * @return string the prefixed title, with underscores and
	 * 	any interwiki and namespace prefixes
	 * @access public
	 */
	function getPrefixedDBkey() {
		$s = $this->prefix( $this->mDbkeyform );
		$s = str_replace( ' ', '_', $s );
		return $s;
	}

	/**
	 * Get the prefixed title with spaces.
	 * This is the form usually used for display
	 * @return string the prefixed title, with spaces
	 * @access public
	 */
	function getPrefixedText() {
		global $wgContLang;
		if ( empty( $this->mPrefixedText ) ) {
			$s = $this->prefix( $this->mTextform );
			$s = str_replace( '_', ' ', $s );
			$this->mPrefixedText = $s;
		}
		return $this->mPrefixedText;
	}
	
	/***
	 * This method is used to get a CSS-friendly name
	 */
	function getClassNameText() {
		return 'PageDW-'.preg_replace("/[^\w\s]/", "", str_replace(array(' ', '_'), array('-', ''), trim($this->getPrefixedText())));	
	}
	
	function getInterwiki() { return $this->mInterwiki; }
	
	function escapeText($text) {
	    $text = str_replace(' ','_',$text);
        return str_replace(array('__','_'),array('&nbsp;&nbsp;',' '),htmlspecialchars($text));
	}

	/**
	 * Get the prefixed title with spaces, plus any fragment
	 * (part beginning with '#')
	 * @return string the prefixed title, with spaces and
	 * 	the fragment, including '#'
	 * @access public
	 */
	function getFullText() {
		global $wgContLang;
		$text = $this->getPrefixedText();
		if( '' != $this->mFragment ) {
			$text .= '#' . $this->mFragment;
		}
		return $text;
	}

	/**
	 * Get a URL-encoded title (not an actual URL) including interwiki
	 * @return string the URL-encoded form
	 * @access public
	 */
	function getPrefixedURL() {
		$s = $this->prefix( $this->mDbkeyform );
		$s = rawurlencode( rawurldecode( $s )) ;
		
		# Cleaning up URL to make it look nice -- is this safe?
		$s = str_replace( array('%28','%29','%2F','%3A'), array('(',')','/',':'), $s );

		return $s;
	}

	/**
	 * Get a real URL referring to this title, with interwiki link and
	 * fragment
	 *
	 * @param string $query an optional query string, not used
	 * 	for interwiki links
	 * @param bool $ignorebase will ignore the baseuri get parameter if it's set, allowing for redirection to self, instead an external location
	 * @return string the URL
	 * @access public
	 */
	function getFullURL( $query = '', $ignorebase = false ) {
		global $wgContLang, $wgServer;

		$url = $wgServer . $this->getLocalURL( $query, $ignorebase );
		if ( '' != $this->mFragment ) {
			$url .= '#' . $this->mFragment;
		}
		return $url;
	}

	/**
	 * Get a URL with no fragment or server name
	 * @param string $query an optional query string; if not specified,
	 * 	$wgArticlePath will be used.
	 * @param bool $ignorebase will ignore the baseuri get parameter if it's set, allowing for redirection to self, instead an external location
	 * @return string the URL
	 * @access public
	 */
	function getLocalURL( $query = '', $ignorebase = false, $appendparams = false) {
		global $wgLang, $wgArticlePath, $wgScript, $wgRequest;
		
		if ( $this->isExternal() ) {
			return $this->getFullURL();
		}
		
		$baseuri = $wgRequest->getVal('baseuri');
		$dbkey = trim( rawurlencode( rawurldecode( $this->getPrefixedDBkey() ) ), '/' );
		$dbkey = str_replace( array('%28','%29','%2F','%3A'), array('(',')','/',':'), $dbkey );

		if (!isset($baseuri) && !$ignorebase && 
			$query == '' && 
			$dbkey == rawurldecode($dbkey) && 
			strpos($this->mDbkeyform,'//') === false && 
			(DIRECTORY_SEPARATOR == '/' || strpos($this->mDbkeyform,'\\') === false)) {
			$url = str_replace( '$1', $dbkey, $wgArticlePath );
		} else {
			if ( $query === '-' ) $query = '';
			$url = "{$wgScript}?title=" . str_replace(array('//'),array('%2F%2F'), $dbkey);
			if ( $query !== '' ) $url .= "&{$query}";
		}
		
		/***
		 * overload baseuri from the request path ... this would ideally be done when the title object gets initialized, but 
		 * i don't htink that's possible, given how liberally we use Title::newFromText()
		 */
		if (isset($baseuri) && !$ignorebase)
		{
			// we will have the long version of the URL
			$url = $baseuri.(substr($url, strlen("{$wgScript}?title=")));
			$qs = wfParseUrl($wgRequest->getFullRequestURL());
			unset($qs['query']['title']);
			unset($qs['query']['baseuri']);	
			if (isset($qs['query'])) 
			{
				foreach ($qs['query'] as $key => $val) 
				{
					$url.= '&'.$key.'='.$val;	
				}
			}
		}
		return $url;
	}

	/**
	 * Get an HTML-escaped version of the URL form, suitable for
	 * using in a link, without a server name or fragment
	 * @param string $query an optional query string
	 * @param bool $ignorebase will ignore the baseuri get parameter if it's set, allowing for redirection to self, instead an external location
	 * @return string the URL
	 * @access public
	 */
	function escapeLocalURL( $query = '', $ignorebase = false ) {
		return htmlspecialchars( $this->getLocalURL( $query, $ignorebase ) );
	}

	/**
	 * Get an HTML-escaped version of the URL form, suitable for
	 * using in a link, including the server name and fragment
	 *
	 * @param bool $ignorebase will ignore the baseuri get parameter if it's set, allowing for redirection to self, instead an external location
	 * @param string $query an optional query string
	 * @return string the URL
	 * @access public
	 */
	function escapeFullURL( $query = '', $ignorebase = false ) {
		return htmlspecialchars( $this->getFullURL( $query, $ignorebase ) );
	}

	/** 
	 * Get the URL form for an internal link.
	 *
	 * @param string $query an optional query string
	 * @param bool $ignorebase will ignore the baseuri get parameter if it's set, allowing for redirection to self, instead an external location
	 * @return string the URL
	 * @access public
	 */
	function getInternalURL( $query = '', $ignorebase = false ) {
		global $wgInternalServer;
		return $wgInternalServer . $this->getLocalURL( $query, $ignorebase );
	}

	/**
	 * Get the edit URL for this Title
	 * @param bool $ignorebase will ignore the baseuri get parameter if it's set, allowing for redirection to self, instead an external location
	 * @return string the URL, or a null string if this is an
	 * 	interwiki link
	 * @access public
	 */
	function getEditURL($ignorebase = false) {
		return $this->getLocalURL( 'action=edit', $ignorebase );
	}
	
	/**
	 * Get the HTML-escaped displayable text form.
	 * Used for the title field in <a> tags.
	 * @return string the text, including any prefixes
	 * @access public
	 */
	function getEscapedText() {
		return Title::escapeText( $this->getPrefixedText() );
	}
	
	/**
	 * Is this Title interwiki?
	 * @return boolean
	 * @access public
	 */
	function isExternal() { return false; }
	
	/**
	  * MT ursm
	  *
	  * splitting the URL into attachment name, and title URL and resolving the latter to the articleID
	  */
	function getAttachmentName(&$articleID) {
	    global $wgTitle;

	    # Attachment:Excel.xls/P/Q
		$text = $this->getText(); //non-pretty URLs need $wgTitle, $this just returns the file/, not file/topic
		$posFileStart = strrpos($text,"/");
		if (($posFileStart !== false && $text{$posFileStart-1} == '^') || 
            ($posFileStart === false && $text{0} == '^')
        ) {
		    if ($posFileStart !== false) {
    		    $posFileStart = strrpos(substr($text, 0, $posFileStart),"/");
    		    $attachmentName = substr($text,$posFileStart+1);
    		    $nt = Title::newFromText(substr($text, 0, $posFileStart));
		    } else {
    		    $attachmentName = $text;
                $nt = $wgTitle;
		    }
		} else {
		    if ($posFileStart !== false) {
    		    $attachmentName = substr($text,$posFileStart+1);
    		    $nt = Title::newFromText(substr($text, 0, $posFileStart));
		    } else {
    		    $attachmentName = $text;
                $nt = $wgTitle;
		    }
		}
		$articleID = $nt->getArticleId();
		return str_replace(" ", "_", $attachmentName);
	}

	/**
	 * Is $wgUser is watching this page?
	 * @return boolean
	 * @access public
	 */
	function userIsWatching() {
		global $wgUser;

		if ( -1 == $this->mNamespace ) { return false; }
		if ( $wgUser->isAnonymous() ) { return false; }

		return $wgUser->isWatched( $this );
	}
		
	/***
	 * Is this title an editable one? (for special cases, userCan() method catches general)
	 */
	function isEditable() {		
		if (!$this->canEditNamespace()) {
			return false;
		}
		$ns = $this->getNamespace();
		if (($ns == NS_USER || $ns == NS_TEMPLATE) && $this->getText() == '') {
			return false;
		}
		return true;
	}
	
	/**
	 * Can $wgUser undelete/restore this page?
	 * @return boolean
	 * @access public
	 */
	function userCanUndelete() {
		global $wgUser;
		return $wgUser->isAdmin();
	}
	
    /***
     * if a page is deleted and we have its old page id, get the deleted title
     */
    function getTitleFromDeletedPageId($pageId) {
		$dbw =& wfGetDB( DB_MASTER );
		$ar_title = $dbw->selectField('archive', 'ar_title', array('ar_last_page_id' => $pageId));
		return Title::newFromText($ar_title);
    }
    	
	/**
	 * Can $wgUser edit this namespace?
	 * @return boolean
	 * @access public
	 */
    function canEditNamespace() {
	    global $wgBlockedNamespaces;
	    return !in_array($this->getNamespace(), $wgBlockedNamespaces);
    }
    
    /***
     * mt royk: return encoded path to parents
     */
    function getParentPath($return = '') {
	    $parents = Article::getParentsFromName($this->getPrefixedUrl());
	    if (empty($parents)) {
		    return $return;
	    }
	    array_pop($parents);
	    return implode('/', $parents);
    }
		
	/***
	 * determines whether this title is located in my user page (used for preventing restrictions in user spaces)
	 */
	function isMyUserPage() {
		global $wgUser;
		$_parents = Article::getParentsFromName($this->getPrefixedText());
		reset($_parents);
		$t = Title::newFromText(current($_parents));
		return $t->getNamespace() != NS_USER || $t->getText() == $wgUser->getName();
	}
	
	function isHomepage()
	{
		return $this->getPrefixedText() == wfHomePageInternalTitle();
	}
			
	/***
	 * Is this a special page?
	 */
	function isSpecialPage() {
		return NS_SPECIAL == $this->getNamespace();	
	}
	
	function isAdminPage() {
		return NS_ADMIN == $this->getNamespace();	
	}
	
	function isTalkPage() {
		//there are many different talk namespaces; Talk:, User_Talk:, etc.
		return DekiNamespace::isTalk($this->getNamespace());	
	}
	
	public function isTemplateHomepage()
	{
		return $this->getNamespace() == NS_TEMPLATE && $this->getText() == '';
	}
	
	/**
	 * Is there a version of this page in the deletion archive?
	 * @return int the number of archived revisions
	 * @access public
	 */
	function isDeleted() {
		//todo: GET:archives/pages/{title}
		$fname = 'Title::isDeleted';
		$dbr =& wfGetDB( DB_SLAVE );
		$n = $dbr->selectField( 'archive', 'COUNT(*)', array( 'ar_namespace' => $this->getNamespace(), 
			'ar_title' => $this->getDBkey() ), $fname );
		return (int)$n;
	}

	/**
	 * Get the article ID for this Title from the link cache,
	 * adding it if necessary
	 * @param int $flags a bit field; may be GAID_FOR_UPDATE to select
	 * 	for update
	 * @return int the ID
	 * @access public
	 */
	function getArticleID( $flags = 0 ) {
		global $wgLinkCache;
		
		if ( $flags & GAID_FOR_UPDATE ) {
			$oldUpdate = $wgLinkCache->forUpdate( true );
			$this->mArticleID = $wgLinkCache->addLinkObj( $this );
			$wgLinkCache->forUpdate( $oldUpdate );
		} else {
			if ( -1 == $this->mArticleID ) {
				$this->mArticleID = $wgLinkCache->addLinkObj( $this );
			}
		}
		return $this->mArticleID;
	}

	/**
	 * This clears some fields in this object, and clears any associated
	 * keys in the "bad links" section of $wgLinkCache.
	 *
	 * - This is called from Article::insertNewArticle() to allow
	 * loading of the new page_id. 
	 *
	 * @param int $newid the new Article ID
	 * @access public
	 */
	function resetArticleID( $newid ) {
		global $wgLinkCache;
		$wgLinkCache->clearBadLink( $this->getPrefixedDBkey() );

		if ( 0 == $newid ) { $this->mArticleID = -1; }
		else { $this->mArticleID = $newid; }
		$this->mRestrictionsLoaded = false;
		$this->mRestrictions = array();
	}
	
	/**
	 * Prefix some arbitrary text with the namespace or interwiki prefix
	 * of this object
	 *
	 * @param string $name the text
	 * @return string the prefixed text
	 * @access private
	 */
	/* private */ function prefix( $name ) {
		global $wgContLang;

		$p = '';
		if ( 0 != $this->mNamespace ) {
			$p .= $wgContLang->getNsText( $this->mNamespace ) . ':';
		}
		return $p . $name;
	}

	/**
	 * Secure and split - main initialisation function for this object
	 *
	 * Assumes that mDbkeyform has been set, and is urldecoded
	 * and uses underscores, but not otherwise munged.  This function
	 * removes illegal characters, splits off the interwiki and
	 * namespace prefixes, sets the other forms, and canonicalizes
	 * everything.
	 * MT ursm: add context to allow resolving ./ and ../
	 * @param Title $context
	 * @return bool true on success
	 * @access private
	 */
	/* private */ function secureAndSplit( $context = null ) {
		global $wgContLang, $wgLocalInterwiki;
		
		# Initialisation
		static $rxTc = false;
		if( !$rxTc ) {
			# % is needed as well
			// $rxTc = '/[^' . Title::legalChars() . ']|%[0-9A-Fa-f]{2}/S';
			$rxTc = '/[^' . Title::legalChars() . ']/S';
		}

		$this->mInterwiki = $this->mFragment = '';
		$this->mNamespace = $this->mDefaultNamespace; # Usually NS_MAIN

		# Clean up whitespace
		#
        $t = /*preg_replace( '/[ _]+/', '_',*/ $this->mDbkeyform /*)*/;
		$t = trim( $t, '_' );
		
		if( false !== strpos( $t, UTF8_REPLACEMENT ) ) {
			# Contained illegal UTF-8 sequences or forbidden Unicode chars.
			return false;
		}

		$this->mDbkeyform = $t;

		# Initial colon indicating main namespace
		if ( strlen($t) > 0 && ':' == $t{0} ) {
			$r = substr( $t, 1 );
			$this->mNamespace = NS_MAIN;
		} else {
			# Namespace or interwiki prefix
			$firstPass = true;
			do {
				if ( preg_match( "/^(.+?)_*:_*(.*)$/S", $t, $m ) ) {
					$p = $m[1];
					$lowerNs = strtolower( $p );
					if ( $ns = DekiNamespace::getCanonicalIndex( $lowerNs ) ) {
						# Canonical namespace
						$t = $m[2];
						$this->mNamespace = $ns;
					} elseif ( $ns = $wgContLang->getNsIndex( $lowerNs )) {
						# Ordinary namespace
						$t = $m[2];
						$this->mNamespace = $ns;
					} 
					# If there's no recognized interwiki or namespace,
					# then let the colon expression be part of the title.
				}
				break;
			} while( true );
			$r = $t;
		}

		# We already know that some pages won't be in the database!
		#
		if ( $this->mInterwiki || -1 == $this->mNamespace ) {
			$this->mArticleID = 0;
		}
		$f = strstr( $r, '#' );
		if ( false !== $f ) {
			$this->mFragment = substr( $f, 1 );
			$r = substr( $r, 0, strlen( $r ) - strlen( $f ) );
			# remove whitespace again: prevents "Foo_bar_#"
			# becoming "Foo_bar_"
			$r = preg_replace( '/_*$/', '', $r );
		}

		# Reject illegal characters.
		#
		if( preg_match( $rxTc, $r ) ) {
			return false;
		}

		if ($context == null) {
			/**
			 * Pages with "/./" or "/../" appearing in the URLs will
			 * often be unreachable due to the way web browsers deal
			 * with 'relative' URLs. Forbid them explicitly.
			 */
			if ( strpos( $r, '.' ) !== false &&
			     ( $r === '.' || $r === '..' ||
			       strpos( $r, './' ) === 0  ||
			       strpos( $r, '../' ) === 0 ||
			       strpos( $r, '/./' ) !== false ||
			       strpos( $r, '/../' ) !== false ) )
			{
				return false;
			}
		} elseif ('' == $this->mInterwiki) {
			if (strpos( $r, '..' ) === 0) {
				$parent = $context->getParent();
				if (!$parent) {
				    $r = substr($r, 2);
				} else {
    				if ($this->mNamespace != NS_ATTACHMENT) {
    				    $this->mNamespace = $parent->getNamespace();
    				    $r = '/' . $parent->getDbKey() . substr($r, 2);
    				} else {
    				    $r = '/' . $parent->getPrefixedDbKey() . substr($r, 2);
    				}
				}
			}
			if (strpos( $r, './' ) === 0) {
				// forbid redirect to child
				$this->mRedirectForbidden = true;
				if ($this->mNamespace != NS_ATTACHMENT) {
				    $this->mNamespace = $context->getNamespace();
    				if ($context->getDbKey() == wfHomePageInternalTitle())
        				$r = substr($r, 1);
    				else
        				$r = '/' . $context->getDbKey() . substr($r, 1);
				} else {
    				$r = '/' . $context->getPrefixedDbKey() . substr($r, 1);
				}
			}
			
			$pos = strpos( $r, '/..');
			while ($pos > 0) {
				$tmpContext = Title::newFromText(substr($r, 0, $pos), $this->mNamespace);
				if (!$tmpContext) {
					return false;
				}
				$parent = $tmpContext->getParent();
				if (!$parent) {
    				$r = substr($r, $pos+3);
    				$pos = strpos( $r, '/..');
    				if ($pos === 0) {
    					return false;
    				}
				} else {
    				if ($this->mNamespace != NS_ATTACHMENT) {
    				    $this->mNamespace = $parent->getNamespace();
    				    $r = '/' . $parent->getDBkey() . substr($r, $pos+3);
    				} else {
    				    $r = '/' . $parent->getPrefixedDbKey() . substr($r, $pos+3);
    				}
    				$pos = strpos( $r, '/..');
				}
			}
			$r = str_replace( '/./', '/', $r );
		}

		# We shouldn't need to query the DB for the size.
		#$maxSize = $dbr->textFieldSize( 'pages', 'page_title' );
		if ( strlen( $r ) > 255 ) {
			return false;
		}

		/**
		 * Can't make a link to a namespace alone...
		 * "empty" local links can only be self-links
		 * with a fragment identifier.
		 */
		
		# Fill fields
		$r = trim($r, '/');
		$this->mDbkeyform = $r;
		$this->mUrlform = rawurlencode( rawurldecode($r) );
		
		$this->mTextform = str_replace( '_', ' ', $r );
		
		return true;
	}
	
	function isEmptyNamespace() {
	    return $this->mDbkeyform == '' &&
			$this->mInterwiki == '' &&
			$this->mNamespace != NS_MAIN;
	}
	
	function getParent() {
	    return Article::getParentTitleFromTitle($this);
	}
		
	/* static */ function normalizeParentName( $titleName ) {
		$parts = Article::getParentsFromName( $titleName );
		$parts_len = count( $parts );
		
		// loop over parents from left to right		
		$acc = '';
		for ( $i = 0; $i < $parts_len; $i++ ) {
			$obj = Title::newFromText( $acc . $parts[$i] );
			$notLast = $i < $parts_len-1;
			if ( $notLast && isset( $obj ) && $obj->getArticleID() > 0 ) {
			    $nt = Title::newFromID($obj->getArticleID());
			    $acc = $nt->getPrefixedText();
			} else
                $acc .= $parts[$i];
            if ($notLast)
                $acc .= HPS_SEPARATOR;
		}

	    return $acc;
	}
	
	/**
	 * Move a title to a new location
	 * @param Title &$nt the new title
	 * @param bool $auth indicates whether $wgUser's permissions
	 * 	should be checked
	 * @return mixed true on success, message name on failure
	 * @access public
	 */
	function moveTo( &$nt, $auth = true ) {
		if( !$this || !$nt ) {
			return false;
		}
		
		if ($nt->getText() == wfHomePageInternalTitle()) {
			wfMessagePush('general', wfMsg('Article.Error.cannot-move-home-page'));
			return false;
		}
		
		if (!$this->canEditNamespace() || !$nt->canEditNamespace()) {
			wfMessagePush('general', wfMsg('Article.Error.namespace-locked-for-editing'));
			return false;
		}
		
		$oldId = $this->getArticleID();
		$newId = $nt->getArticleID();
		
		if ( $oldId == $newId ) {
			return true; //short-circuit the call, but this maybe not be necessary for casing changes
		}
		global $wgDekiPlug;
		$r = $wgDekiPlug->At('pages', $oldId, 'move')->With('to', $nt->getPrefixedText())->Post();
		$return = MTMessage::HandleFromDream($r, array(409));
		if ($r['status'] == 409) {
			wfMessagePush('general', wfArrayVal($r, 'body/error/message'));	
		}
		return $return;
	}
	
	/**
	 * Returns an array of parent page titles, including the current page title
	 * @return array of array
	 * @access public
	 */
	function getTitleHierarchy() {
		$result = array();
	
		// extract hierarchical structure of pages title
		$parts = Article::getParentsFromName( $this->getPrefixedDBkey() );
		$parts_len = count( $parts ) - 1;
		
		// loop over parents from left to right		
		$acc = '';
		for ( $i = 0; $i < $parts_len; $i++ ) {
			$acc .= $parts[$i];
			$obj = Title::newFromDBkey( $acc );
			if ( isset( $obj ) ) {
				$result[] = array( 'obj' => $obj, 'text' => str_replace( '_', ' ', $parts[$i] ) );
			} else {
				$result[] = array( 'text' => str_replace( '_', ' ', $parts[$i] ) );
			}
			$acc .= HPS_SEPARATOR;
		}
		
		// finally, add current title
		$result[] = array( 'obj' => $this, 'text' => str_replace( '_', ' ', $parts[$parts_len] ) );
		return $result;
	}
	
	function getParentName(&$nt) {
 		$parent = Article::getParentTitleFromTitle($nt);
 		$parentPage = is_object($parent) && $parent->getPrefixedText() != '' ? $parent->getPrefixedText(): wfHomePageInternalTitle();
 		return $parent;
 	}
	/**
	 * Shows if object is redirect.
	 * @return boolean
	 * @access public
	 */
	function isRedirect() {
	    return $this->mRedirect;
	}
	
	/**
	 * Shows if redirect is allowed or forbidden
	 *
	 * @return bool - true if forbidden
	 */
	function isRedirectForbidden()
	{
		return $this->mRedirectForbidden;
	}
}

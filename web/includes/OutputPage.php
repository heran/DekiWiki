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
 * @package MediaWiki
 */

/**
 * This is not a valid entry point, perform no further processing unless MEDIAWIKI is defined
 */
if( defined( 'MINDTOUCH_DEKI' ) ) {

# See design.doc

/**
 * @todo document
 * @package MediaWiki
 */
class OutputPage {
	var $mHeaders, $mCookies, $mMetatags, $mKeywords, $mSubNav;
	var $mLinktags, $mCssIncludes, $mPagetitle, $mBodytext, $mDebugtext;
	var $mFilestext, $mGallerytext, $mToc;
	var $mFileCount, $mTagCount;
	var $mHTMLtitle, $mRobotpolicy, $mIsArticle, $mPrintable;
	var $mSubtitle, $mRedirect;
	var $mLastModified, $mCategoryLinks;
	var $mScripts, $mLinkColours, $mRedirectPage, $mRedirectPageTitle_;

	var $mSuppressQuickbar;
	var $mOnloadHandler;
	var $mDoNothing;
	var $mContainsOldMagic, $mContainsNewMagic;
	var $mIsArticleRelated;
	var $mShowFeedLinks = false;
	var $mEnableClientCache = true;
	var $mExport = false;
	var $mExportType = '';
	var $mBackLinks = null;
	
	var $mContentTargets = array();

	/**
	 * Constructor
	 * Initialise private variables
	 */
	function OutputPage() {
		$this->mHeaders = $this->mCookies = $this->mMetatags =
		$this->mKeywords = $this->mLinktags = $this->mCssIncludes = $this->mTags = $this->mRelated = array();
		$this->mHTMLtitle = $this->mPagetitle = $this->mBodytext = $this->mSubNav = 
		$this->mFilestext = $this->mCommentstext = $this->mGallerytext = $this->mTagstext = $this->mToc = $this->mRelatedtext = 
		$this->mRedirect = $this->mLastModified =
		$this->mSubtitle = $this->mDebugtext = $this->mRobotpolicy =
		$this->mOnloadHandler = '';
		$this->mIsArticleRelated = $this->mIsArticle = $this->mPrintable = true;
		$this->mSuppressQuickbar = $this->mPrintable = false;
		$this->mLanguageLinks = array();
		$this->mCategoryLinks = array();
		$this->mDoNothing = false;
		$this->mContainsOldMagic = $this->mContainsNewMagic = 0;
		$this->mScripts = '';
		$this->mHeadHTML = '';
		$this->mTailHTML = '';
		$this->mExport = false;
		$this->mExportType = '';
		$this->mFileCount = $this->mTagCount = 0;
		$this->mImageCount = 0;
		$this->mCommentCount = 0;
		$this->mBackLinks = null;
		$this->mPageMetrics = array('views' => 0, 'charcount' => 0, 'revisions' => 0);
		$this->mRedirectPageTitle = $this->mRedirectPage = $this->mRedirectPageTitle_ = '';
	}

	function addHeader( $name, $val ) { array_push( $this->mHeaders, $name.': '.$val ) ; }
	function addCookie( $name, $val ) { array_push( $this->mCookies, array( $name, $val ) ); }
	function redirect( $url, $responsecode = '302' ) { $this->mRedirect = $url; $this->mRedirectCode = $responsecode; }
	function redirectHome() {
		$title = Title::newFromText(wfHomePageInternalTitle());
		$this->redirect($title->getLocalUrl());
	}

	/**
	 * @param $name - name of the meta tag (to add an http-equiv meta tag, precede the name with "http:")
	 * @param $val - content of the meta tag
	 * @param $http - second way to specify a http-equiv meta. see tip above 
	 */
	function addMeta($name, $val, $http = false)
	{
		if ($http)
		{
			$name = 'http:'.$name;
		}
		
		array_push($this->mMetatags, array($name, $val));
	}
	function addKeyword( $text ) { array_push( $this->mKeywords, $text ); }
	function addScript( $script ) { $this->mScripts .= $script; }
	function getScript() { return $this->mScripts; }

	function addHeadHTML( $markup ) { $this->mHeadHTML .= $markup; }
	function getHeadHTML()
	{ 
		$html = $this->mHeadHTML;
		
		// add custom css to the header
		global $wgLocalCssPath;
		if (!empty($wgLocalCssPath))
		{
			$html .= '<link href="'.$wgLocalCssPath.'" rel="stylesheet" type="text/css" />';
		}

		return $html; 
	}
	function addTailHTML( $markup ) { $this->mTailHTML .= $markup; }
	function getTailHTML() { return $this->mTailHTML; }

	function addLink( $linkarr ) {
		# $linkarr should be an associative array of attributes. We'll escape on output.
		array_push( $this->mLinktags, $linkarr );
	}

	public function addCss($href, $media = 'screen')
	{
		array_push($this->mCssIncludes, array('href' => $href, 'media' => $media));
	}
	/**
	 * @param $media - allows the fetched files to be filtered by media
	 */
	public function getCss($media = null)
	{
		$css = '';
		foreach ($this->mCssIncludes as $sheet)
		{
			if (is_null($media) || $sheet['media'] == $media)
			{
				$css .= '<link rel="stylesheet" type="text/css" media="'. $sheet['media'] .'" href="'. $sheet['href'] .'" />';
			}
		}
		return $css;
	}
	
	function addMetadataLink( $linkarr ) {
		# note: buggy CC software only reads first "meta" link
		static $haveMeta = false;
		$linkarr['rel'] = ($haveMeta) ? 'alternate meta' : 'meta';
		$this->addLink( $linkarr );
		$haveMeta = true;
	}

	function getPageTitleActionText () {
		global $action;
		switch($action) {
			case 'edit':
				return wfMsg('Article.Common.action-edit');
			case 'history':
				return wfMsg('Article.Common.action-history');
			// Guerric: these cases should no longer be used since protection
			//			is now handled by the javascript popup
			case 'protect':
				return wfMsg('System.Deprecated.action-protect');
			case 'unprotect':
				return wfMsg('System.Deprecated.action-unprotect');
			// Guerric: but to be safe, they're still here
			case 'delete':
				return wfMsg('Article.Common.action-delete');
			case 'watch':
				return wfMsg('Article.Common.action-watch');
			case 'unwatch':
				return wfMsg('Article.Common.action-unwatch');
			case 'submit':
				return wfMsg('Article.Common.action-preview');
			case 'info':
				return wfMsg('Article.Common.action-info');
			default:
				return '';
		}
	}

	function setRobotpolicy( $str ) { $this->mRobotpolicy = $str; }
	function setHTMLTitle( $name ) {$this->mHTMLtitle = $name; }
	function setPageTitle( $name, $fromApi = false ) {
		if (!$fromApi)
		{
			$name = wfDecodeTitle($name, true);
		}
		global $action, $wgContLang;
		$name = $wgContLang->convert($name, true);
		$this->mPagetitle = $name;
		if(!empty($action)) {
			$taction =  $this->getPageTitleActionText();
			if( !empty( $taction ) ) {
				$name .= ' - '.$taction;
			}
		}
		global $wgSitename;
		if ($name != '') {
			$name .= ' - ' . $wgSitename;
		}
		else {
			$name = $wgSitename;
		}
		$this->setHTMLTitle( $name );
	}
	function getHTMLTitle() { return $this->mHTMLtitle; }
	function getPageTitle() { return $this->mPagetitle; }
	function setSubtitle( $str ) { $this->mSubtitle = $str; }
	
	function setSubNavigation ( $str ) { $this->mSubNav = $str; }
	function addSubNavigation ( $str ) { $this->mSubNav .= $str; }
	function getSubNavigation () { return $this->mSubNav; }
	
	function getRedirectMessage() { return $this->mRedirectPage; }
	function setRedirectMessage( $str ) { $this->mRedirectPage = $str; }
	/* deprecate */function getRedirectLocation() { return $this->mRedirectPageTitle; }
	/* deprecate */function setRedirectLocation( $str ) { $this->mRedirectPageTitle = $str; }
	function getRedirectLocation_() { return $this->mRedirectPageTitle_; }
	function setRedirectLocation_($str) { $this->mRedirectPageTitle_ = $str; }
	function getSubtitle() { return $this->mSubtitle; }
	function isArticle() { return $this->mIsArticle; }
	function setPrintable() { $this->mPrintable = true; }
	function isPrintable() { return $this->mPrintable; }
	function setSyndicated( $show = true ) { $this->mShowFeedLinks = $show; }
	function isSyndicated() { return $this->mShowFeedLinks; }
	function setOnloadHandler( $js ) { $this->mOnloadHandler = $js; }
	function getOnloadHandler() { return $this->mOnloadHandler; }
	function disable() { $this->mDoNothing = true; }
	function setBacklinks($backlinks) { $this->mBackLinks = $backlinks; }
	function getBacklinks($asList = false) 
	{ 
		if ($asList) 
		{
			global $wgUser;
			$sk = $wgUser->getSkin();
			$bl = '';
			$backlinks = $this->mBackLinks;
			$count = count($backlinks);
			$items = array();
			if (!is_null($backlinks) && $count > 0) {
				foreach ($backlinks as $backlink) {
					$t = Title::newFromText($backlink['path']);
					if (!is_null($t))
					{
						$items[] = $sk->makeKnownLink($t->getPrefixedText());
					}
				}
			}
			return Skin::arrayToList($items);
		}
		return $this->mBackLinks; 
	}
	
	/* deprecate */ function getBacklinksAsList() {
		return $this->getBacklinks(true);
	}

	function setArticleRelated( $v ) {
		$this->mIsArticleRelated = $v;
		if ( !$v ) {
			$this->mIsArticle = false;
		}
	}
	function setArticleFlag( $v ) {
		$this->mIsArticle = $v;
		if ( $v ) {
			$this->mIsArticleRelated = $v;
		}
	}

	function isArticleRelated() { return $this->mIsArticleRelated; }

	function getLanguageLinks() { return $this->mLanguageLinks; }
	function addLanguageLinks($newLinkArray) {
		$this->mLanguageLinks += $newLinkArray;
	}
	function setLanguageLinks($newLinkArray) {
		$this->mLanguageLinks = $newLinkArray;
	}

	function getCategoryLinks() {
		return $this->mCategoryLinks;
	}
	function addCategoryLinks($newLinkArray) {
		$this->mCategoryLinks += $newLinkArray;
	}
	function setCategoryLinks($newLinkArray) {
		$this->mCategoryLinks += $newLinkArray;
	}

	function suppressQuickbar() { $this->mSuppressQuickbar = true; }
	function isQuickbarSuppressed() { return $this->mSuppressQuickbar; }

	function getHTML() { return $this->mBodytext; }
	function addHTML( $text ) { $this->mBodytext .= $text; }

	function getFilesHTML() { return $this->mFilestext; }
	function addFilesHTML( $text ) { $this->mFilestext .= $text; }

	function getCommentsHTML() { return $this->mCommentstext; }
	function setCommentsHTML( $text ) { $this->mCommentstext = $text; }

	function getFileCount() { return $this->mFileCount; }
	function setFileCount($count) { $this->mFileCount = $count; }

	function getImageCount() { return $this->mImageCount; }
	function setImageCount($count) { $this->mImageCount = $count; }

	function getCommentCount() { return $this->mCommentCount; }
	function setCommentCount($count) { $this->mCommentCount = $count; }

	function getGalleryHTML() { return $this->mGallerytext; }
	function addGalleryHTML( $text ) { $this->mGallerytext .= $text; }

	/* deprecate */ function getTagsHTML() { return $this->mTagstext; }
	/* deprecate */ function addTagsHTML( $text ) { $this->mTagstext .= $text; }
	
	function setTags($tags) { 
		$this->mTags = $tags; 
	}
	function getTags($asList = false) { 
		if ($asList) 
		{
			return Skin::arrayToList($this->mTags); 
		} 
		return $this->mTags;
	}
	function getRelated($asList = false) {
		if ($asList) 
		{
			return Skin::arrayToList($this->mRelated); 
		} 
		return $this->mRelated;
	}
	/* deprecate */ function getRelatedHTML() {
		return $this->getRelated(true);
	}
	function setRelated( $related ) { $this->mRelated = $related; }

	function getTagCount() { return $this->mTagCount; }
	function setTagCount($count) { $this->mTagCount = $count; }
	
	function getTocHTML() { return $this->mToc; }
	function setTocHTML($text) { $this->mToc = $text; }

	function getTargets() {
		global $wgContentTargets;
		foreach ($this->mContentTargets as $key => $val) 
		{
			//we may need to remap API target keys to skinning variable keys; see in Defines.php
			if (array_key_exists($key, $wgContentTargets)) 
			{
				$this->mContentTargets[$wgContentTargets[$key]] = $val;
				unset($this->mContentTargets[$key]);
			}
		}
		return $this->mContentTargets;
	}
	function getTarget($key, $return = null) {
		if (array_key_exists($key, $this->mContentTargets)) {
			return $this->mContentTargets[$key];
		}
		return $return;
	}
	function setTarget($key, $val) {
		$this->mContentTargets[$key] = $val;	
	}
	
	function clearHTML() { $this->mBodytext = ''; }
	function debug( $text ) { $this->mDebugtext .= $text; }

	function setPageMetrics($metrics = array()) { $this->mPageMetrics = $metrics; }
	function getPageMetrics() { return $this->mPageMetrics; }

	/**
	 * Convert wikitext to HTML and add it to the buffer
	 */
	function addWikiText( $text, $linestart = true ) {
		global $wgTitle;
		$this->addWikiTextTitle($text, $wgTitle, $linestart);
	}

	function addWikiTextWithTitle($text, &$title, $linestart = true) {
		$this->addWikiTextTitle($text, $title, $linestart);
	}

	function addWikiTextTitle( $text, &$title, $linestart = true ) {
		$this->addHTML( $text );
	}

	/**
	 * Add wikitext to the buffer, assuming that this is the primary text for a page view
	 * Saves the text into the parser cache if possible
	 */
	function addPrimaryWikiText( $text, $cacheArticle ) {
		global $wgUser, $wgTitle, $wgRequest;

        $action = $wgRequest->getVal( 'action', 'view' );

		$this->addHTML( $text );
	}

	/**
	 * Add the output of a QuickTemplate to the output buffer
	 * @param QuickTemplate $template
	 */
	function addTemplate( &$template ) {
		ob_start();
		$template->execute();
		$this->addHtml( ob_get_contents() );
		ob_end_clean();
	}

	/**
	 * Parse wikitext and return the HTML. This is for special pages that add the text later
	 */
	function parse( $text, $linestart = true ) {
		return $text; //todo royk stub
	}

	function findFileHref($matches) {
	    $href = $matches[1];
	    $text = $matches[2];
	    global $wgScriptPath, $wgTitle;
	    $isFile = strpos($href,"/{$wgScriptPath}File:") === 0;
	    $isFileUgly = strpos($href,"/{$wgScriptPath}index.php?title=File:") === 0;
	    if ($isFile || $isFileUgly) {
	        $href = substr($href,strlen("/$wgScriptPath"));
	        if ($isFileUgly) $href = substr($href,strlen("index.php?title="));
	        $pos = strpos($href, '&');
	        if ($pos) $href = substr($href, 0, $pos);
	        $href = rawurldecode($href);
	        $nt = Title::newFromURL($href, $wgTitle);
	    }
	}

	/**
	 * Use enableClientCache(false) to force it to send nocache headers
	 * @param $state
	 */
	function enableClientCache( $state ) {
		return wfSetVar( $this->mEnableClientCache, $state );
	}

	function sendCacheControl() {
		global $wgVarnishCache, $wgVarnishMaxage;
		if( !empty( $wgVarnishCache ) ) {
			header( 'Vary: Accept-Encoding' );
			wfDebug( "** Varnish caching **\n", false );
			header( 'Vary: Accept-Encoding' );
			header( "Cache-Control: s-maxage=" . $wgVarnishMaxage . ", must-revalidate, max-age=0" );
			if($this->mLastModified) header( "Last-modified: {$this->mLastModified}" ); // TODO: Not used.  See: http://bugs.opengarden.org/view.php?id=4072
			return;
		}

		# don't serve compressed data to clients who can't handle it
		# maintain different caches for logged-in users and non-logged in ones
		header( 'Vary: Accept-Encoding, Cookie' );
		if( $this->mEnableClientCache ) {

			# We do want clients to cache if they can, but they *must* check for updates
			# on revisiting the page.
			wfDebug( "** private caching; {$this->mLastModified} **\n", false );
			header( "Expires: -1" );
			header( "Cache-Control: private, must-revalidate, max-age=0" );
			if($this->mLastModified) header( "Last-modified: {$this->mLastModified}" );
		} else {
			wfDebug( "** no caching **\n", false );

			# In general, the absence of a last modified header should be enough to prevent
			# the client from using its cache. We send a few other things just to make sure.
			header( 'Expires: -1' );
			header( 'Cache-Control: no-cache, no-store, max-age=0, must-revalidate' );
			header( 'Pragma: no-cache' );
		}
	}

	/**
	 * Finally, all the text has been munged and accumulated into
	 * the object, let's actually output it:
	 */
	function output() {
		global $wgUser, $wgLang, $wgDebugComments, $wgCookieExpiration;
		global $wgInputEncoding, $wgOutputEncoding, $wgContLanguageCode;
		global $wgDebugRedirects, $wgMimeType, $wgProfiler, $wgTitle;
		global $wgDekiPlug, $wgUseGzDisplay, $wgDisabledGzip ;

		if( $this->mDoNothing ){
			return;
		}
		
		// see bug #4471; this is needed for loading contents in an IFRAME for IE7
		header( 'P3P:CP="IDC DSP COR ADM DEVi TAIi PSA PSD IVAi IVDi CONi HIS OUR IND CNT"' );

		if ( $this->mExport ) {
			//we get the URL from the API, then append the URL parameters - having PHP buffer the PDF is problematic
			$r = $wgDekiPlug->At('pages', $wgTitle->getArticleID(), 'info')->Get();
			$url = wfArrayVal($r, 'body/page/@href');
			
			if ($this->mExportType == 'pdf') {
				header("Location:" . $url .'/export?type=pdf');
			} else if ($this->mExportType == "html") {
				header("Location:" . $url .'/export?type=html');
			}
			return;
		}
		$fname = 'OutputPage::output';

		$sk = $wgUser->getSkin();

		if ( '' != $this->mRedirect ) {
			if( substr( $this->mRedirect, 0, 4 ) != 'http' ) {
				# Standards require redirect URLs to be absolute
				global $wgServer;
				$this->mRedirect = $wgServer . $this->mRedirect;
			}
			if( $this->mRedirectCode == '301') {
				if( !$wgDebugRedirects ) {
					header("HTTP/1.1 {$this->mRedirectCode} Moved Permanently");
				}
				$this->mLastModified = gmdate( 'D, j M Y H:i:s' ) . ' GMT';
			}

			$this->sendCacheControl();

			if( $wgDebugRedirects ) {
				$url = htmlspecialchars( $this->mRedirect );
				print "<html>\n<head>\n<title>Redirect</title>\n</head>\n<body>\n";
				print "<p>Location: <a href=\"$url\">$url</a></p>\n";
				print "</body>\n</html>\n";
			} else {
				header( 'Location: '.$this->mRedirect );
			}
			if ( isset( $wgProfiler ) ) { wfDebug( $wgProfiler->getOutput() ); }

			return;
		}


		# Buffer output; final headers may depend on later processing
		ob_start();

		$this->transformBuffer();

		# Disable temporary placeholders, so that the skin produces HTML
		$sk->postParseLinkColour( false );
		
		header( "Content-type: $wgMimeType; charset={$wgOutputEncoding}" );
		header( 'Content-language: '.$wgContLanguageCode );

		$exp = time() + $wgCookieExpiration;
		foreach( $this->mCookies as $name => $val ) {
			setcookie( $name, $val, $exp, '/' );
		}

		wfProfileIn( 'Output-skin' );
		
		// Bugfix MT-10295: Removed gzip compression via PHP; gzip using webserver
	
		if ($this->mExport) {
			$this->out($HTML);
		}
		else {
			$sk->outputPage( $this );
		}

		wfProfileOut( 'Output-skin' );

		$this->sendCacheControl();
		ob_end_flush();

	}

	function out( $ins ) {
		global $wgInputEncoding, $wgOutputEncoding, $wgContLang;
		if ( 0 == strcmp( $wgInputEncoding, $wgOutputEncoding ) ) {
			$outs = $ins;
		} else {
			$outs = $wgContLang->iconv( $wgInputEncoding, $wgOutputEncoding, $ins );
			if ( false === $outs ) { $outs = $ins; }
		}
		print $outs;
	}

	function setEncodings() {
		global $wgInputEncoding, $wgOutputEncoding;
		global $wgUser, $wgContLang;

		$wgInputEncoding = strtolower( $wgInputEncoding );

		// setAltEncoding is a no-op, and getOption is deprecated and returns null.
		/*
		if ($wgUser->Properties->getOption('altencoding')) {
			$wgContLang->setAltEncoding();
			return;
		}
		*/

		if ( empty( $_SERVER['HTTP_ACCEPT_CHARSET'] ) ) {
			$wgOutputEncoding = strtolower( $wgOutputEncoding );
			return;
		}
		
		$wgOutputEncoding = $wgInputEncoding;
	}

	function reportApiTime() {
		global $wgPlugProfile, $wgProfileApi;
		if ($wgProfileApi === false)
		{
			return '';
		}
		
		$totalTime = 0;
		$optimizedTime = 0;
		$logged = array();
		
		$output = sprintf("\t%-9s %-49s %-14s %s\n", 'Verb', 'Path', 'Time(ms)', 'API Stats');
		foreach ($wgPlugProfile as $request)
		{
			$uri = parse_url($request['url']);
			
			$verb = isset($request['verb']) ? $request['verb'] : '';
			$time = sprintf('%8s', number_format(round($request['diff'] * 1000, 2), 2));
			$stats = isset($request['stats']) ? $request['stats'] : '';

			if (!in_array($uri, $logged))
			{
				$optimizedTime += $request['diff'];
				$logged[] = $uri;
			}

			$output .= sprintf("\t%-9s %-49s %-14s %s\n", $verb, $uri['path'], $time, $stats);
			$totalTime += $request['diff'];
		}

		// format the final times
		$totalTime = sprintf('%8s', number_format(round($totalTime * 1000, 2), 2));
		$optimizedTime = sprintf('%8s', number_format(round($optimizedTime * 1000, 2), 2));

		$output .= sprintf("\t%-60s =======\n", '');
		$output .= sprintf("\t%-52s Total: %-14s\n", '', $totalTime);
		if ($totalTime != $optimizedTime)
		{
			$output .= sprintf("\t%-52s Ideal: %-14s\n", '', $optimizedTime);
		}

		return '<!--'."\n". htmlspecialchars($output) .'-->'."\n";
	}

	/**
	 * Returns a HTML comment with the elapsed time since request.
	 * This method has no side effects.
	 */
	function reportTime() {
		global $wgRequestTime, $wgProfiling, $wgProfileToCommentUser, $wgUser;


		if ( $wgProfiling && $wgUser && $wgProfileToCommentUser &&
		  $wgProfileToCommentUser == $wgUser->getName() )
		{
			$prof = wfGetProfilingOutput();
			// Strip end of comment
			$prof = str_replace( '-->', '--&lt;', $prof );
			$com = "<!--\n$prof\n-->\n";
		} else {
			$com = '';
		}


		$now = wfTime();
		list( $usec, $sec ) = explode( ' ', $wgRequestTime );
		$start = (float)$sec + (float)$usec;
		$elapsed = $now - $start;

		# Use real server name if available, so we know which machine
		# in a server farm generated the current page.
		if ( function_exists( 'posix_uname' ) ) {
			$uname = @posix_uname();
		} else {
			$uname = false;
		}
		if( is_array( $uname ) && isset( $uname['nodename'] ) ) {
			$hostname = $uname['nodename'];
		} else {
			# This may be a virtual server.
			$hostname = $_SERVER['SERVER_NAME'];
		}
		$com .= sprintf( "<!-- Served by %s in %01.2f secs. -->\n",
		  $hostname, $elapsed );
		return $com;
	}

	/**
	 * Note: these arguments are keys into wfMsg(), not text!
	 */
	function errorpage( $title, $msg, $oldtitle = '' ) {
		global $wgTitle;

		$this->mDebugtext .= 'Original title: ' .
		  $wgTitle->getPrefixedText() . "\n";
		$this->setPageTitle( wfMsg( $title ) );
		$this->setHTMLTitle( wfMsg( 'Page.Error.page-title' ) );
		$this->setRobotpolicy( 'noindex,nofollow' );
		$this->setArticleRelated( false );
		$this->enableClientCache( false );
		$this->mRedirect = '';

		$this->mBodytext = '';
		$this->addHTML( '<p>' . wfMsg( $msg, $oldtitle ) . "</p>\n" );
		$this->returnToMain( false );

		$this->output();
		wfErrorExit();
	}

	function accessDenied() {
		global $wgUser;

		$this->setPageTitle( wfMsg( 'Page.Error.sysop-access-required' ) );
		$this->setHTMLTitle( wfMsg( 'Page.Error.page-title' ) );
		$this->setRobotpolicy( 'noindex,nofollow' );
		$this->setArticleRelated( false );
		$this->mBodytext = '';
		
		$this->addHTML(wfMessageOutput('<ul><li>'.wfMsg('Article.Common.page-is-restricted').'</li></ul>'));
	}

	function loginToUse() {
		$this->redirectToLogin();
	}

	function databaseError( $fname, $sql, $error, $errno ) {
		global $wgUser, $wgCommandLineMode, $wgShowSQLErrors;

		$this->setPageTitle( wfMsgNoDB( 'Page.Error.database-error' ) );
		$this->setRobotpolicy( 'noindex,nofollow' );
		$this->setArticleRelated( false );
		$this->enableClientCache( false );
		$this->mRedirect = '';

		if( !$wgShowSQLErrors ) {
			$sql = wfMsg( 'Page.Error.sql-query-hidden' );
		}

		if ( $wgCommandLineMode ) {
			$msg = wfMsgNoDB( 'Page.Error.database-query-error', htmlspecialchars( $sql ),
						htmlspecialchars( $fname ), $errno, htmlspecialchars( $error ) );
		} else {
			$msg = wfMsgNoDB( 'Page.Error.database-query-error-html', htmlspecialchars( $sql ),
						htmlspecialchars( $fname ), $errno, htmlspecialchars( $error ) );
		}

		if ( $wgCommandLineMode || !is_object( $wgUser )) {
			print $msg."\n";
			wfErrorExit();
		}
		$this->mBodytext = $msg;
		$this->output();
		wfErrorExit();
	}

	function fatalError( $message ) {
		$this->setPageTitle( wfMsg( 'Page.Error.internal-error' ) );
		$this->setRobotpolicy( "noindex,nofollow" );
		$this->setArticleRelated( false );
		$this->enableClientCache( false );
		$this->mRedirect = '';

		$this->mBodytext = $message;
		$this->output();
		wfErrorExit();
	}

	function unexpectedValueError( $name, $val ) {
		$this->fatalError( wfMsg( 'Page.Error.fatal-unexpected', $name, $val ) );
	}

	function fileCopyError( $old, $new ) {
		$this->fatalError( wfMsg( 'Page.Error.fatal-file-copy-error', $old, $new ) );
	}

	function fileRenameError( $old, $new ) {
		$this->fatalError( wfMsg( 'Page.Error.fatal-file-rename-error', $old, $new ) );
	}

	function fileDeleteError( $name ) {
		$this->fatalError( wfMsg( 'Page.Error.fatal-file-delete-error', $name ) );
	}

	function fileNotFoundError( $name ) {
		$this->fatalError( wfMsg( 'Page.Error.fatal-file-not-found', $name ) );
	}

	function redirectToLogin() {
		global $wgUser;

		$sk = $wgUser->getSkin();
		header('Location: ' . $sk->makeLoginUrl());
	}

	/**
	 * return from error messages or notes
	 * @param $auto automatically redirect the user after 10 seconds
	 * @param $returnto page title to return to. Default is Main Page.
	 */
	function returnToMain( $auto = true, $returnto = NULL, $time = 10 ) {
		global $wgUser, $wgOut, $wgRequest;

		if ( $returnto == NULL ) {

			// MT (steveb): changed default behavior to read most recent page
			//              from breadcrumbs rather than from request;
			//              this produces more reliable results
			// $returnto = $wgRequest->getText( 'returnto' );
			$returnto = breadcrumbMostRecentPage();
		}
		$returnto = htmlspecialchars( $returnto );

		$sk = $wgUser->getSkin();
		if ( '' == $returnto ) {
			$returnto = wfHomePageInternalTitle();
			$link = $sk->makeKnownLink ($returnto, Title::newFromText($returnto)->getPrefixedText() );
		} else {
			$link = $sk->makeKnownLink( $returnto, '' );
		}

		$r = wfMsg( 'Article.Common.return-to', $link );
		if ( $auto && $time == 0 ) {
			$titleObj = Title::newFromText( $returnto );
			$wgOut->addMeta( 'http:Refresh', $time.';url=' . $titleObj->escapeFullURL() );
		}
		$wgOut->addHTML( "\n<div class='b-body'><br><br><p>$r</p></div>\n" );
	}

	/**
 	 * @private
	 */
	function headElement() {
		global $wgDocType, $wgDTD, $wgContLanguageCode, $wgOutputEncoding, $wgMimeType;
		global $wgUser, $wgContLang, $wgRequest;

		if( $wgMimeType == 'text/xml' || $wgMimeType == 'application/xhtml+xml' || $wgMimeType == 'application/xml' ) {
			$ret = "<?xml version=\"1.0\" encoding=\"$wgOutputEncoding\" ?>\n";
		} else {
			$ret = '';
		}

		$ret .= "<!DOCTYPE html PUBLIC \"$wgDocType\"\n        \"$wgDTD\">\n";

		if ( "" == $this->mHTMLtitle ) {
			global $wgSitename;
			$this->mHTMLtitle = wfMsg( 'Article.Common.page-title', $this->mPagetitle, $wgSitename );
		}

		$rtl = $wgContLang->isRTL() ? " dir='RTL'" : '';
		$ret .= "<html xmlns=\"http://www.w3.org/1999/xhtml\" xml:lang=\"$wgContLanguageCode\" lang=\"$wgContLanguageCode\" $rtl>\n";
		$ret .= "<head>\n<title>" . htmlspecialchars( $this->mHTMLtitle ) . "</title>\n";
		array_push( $this->mMetatags, array( "http:Content-type", "$wgMimeType; charset={$wgOutputEncoding}" ) );

		$ret .= $this->getHeadLinks();
		global $wgStylePath;
		if( $this->isPrintable() ) {
			$media = '';
		} else {
			$media = "media='print'";
		}
		$printsheet = htmlspecialchars( "$wgStylePath/common/wikiprintable.css" );
		$ret .= "<link rel='stylesheet' type='text/css' $media href='$printsheet' />\n";

		$sk = $wgUser->getSkin();
		$ret .= $sk->getHeadScripts();
		$ret .= $this->mScripts;
		$ret .= $sk->getUserStyles();
		$ret .= "</head>\n";
		return $ret;
	}

	function getHeadLinks() {
		global $wgRequest, $wgStylePath;
		$ret = '';
		foreach ( $this->mMetatags as $tag ) {
			if ( 0 == strcasecmp( 'http:', substr( $tag[0], 0, 5 ) ) ) {
				$a = 'http-equiv';
				$tag[0] = substr( $tag[0], 5 );
			} else {
				$a = 'name';
			}
			$ret .= "<meta $a=\"{$tag[0]}\" content=\"{$tag[1]}\" />\n";
		}
		$p = $this->mRobotpolicy;
		if ( '' == $p ) { $p = 'index,follow'; }
		$ret .= "<meta name=\"robots\" content=\"$p\" />\n";

		if ( count( $this->mKeywords ) > 0 ) {
			$strip = array(
				"/<.*?" . ">/" => '',
				"/[_]/" => ' '
			);
			$ret .= "<meta name=\"keywords\" content=\"" .
			  htmlspecialchars(preg_replace(array_keys($strip), array_values($strip),implode( ",", $this->mKeywords ))) . "\" />\n";
		}
		
		// keep the order the links are pushed in 
		$this->mLinktags = array_reverse($this->mLinktags); 
		
		foreach ( $this->mLinktags as $tag ) {
			$ret .= '<link';
			foreach( $tag as $attr => $val ) {
				$ret .= " $attr=\"" . htmlspecialchars( $val ) . "\"";
			}
			$ret .= " />\n";
		}
		if( $this->isSyndicated() ) {
			# FIXME: centralize the mime-type and name information in Feed.php
			$link = $wgRequest->escapeAppendQuery( 'feed=rss' );
			$ret .= "<link rel='alternate' type='application/rss+xml' title='RSS 2.0' href='$link' />\n";
			$link = $wgRequest->escapeAppendQuery( 'feed=atom' );
			$ret .= "<link rel='alternate' type='application/rss+atom' title='Atom 0.3' href='$link' />\n";
		}
		
		// set generator tag
		$generatedBy = DekiSite::getProductName();
		DekiPlugin::executeHook(Hooks::HEAD_GENERATOR, array(&$generatedBy));
		
		$ret .= '<meta name="generator" content="'. $generatedBy .'" />'."\n";
		$ret .= '<link rel="search" type="application/opensearchdescription+xml" title="' . wfMsg('System.API.opensearch-shortname', htmlspecialchars(DekiSite::getName())) . '" href="/deki/gui/opensearch.php?type=description" />';
		
		global $wgArticle, $wgTitle;
		if ($wgArticle->userCanEdit() && $wgRequest->getVal('action') != 'edit') 
		{
			//universal edit link, see bug #4368
			$ret .= '<link rel="alternate" type="application/x-wiki" title="'.wfMsg('Skin.Common.edit-page').'" href="'.$wgTitle->getLocalUrl('action=edit').'" />';
		}	
		return $ret;
	}

	/**
	 * Run any necessary pre-output transformations on the buffer text
	 */
	function transformBuffer( $options = 0 ) {
	}


	/**
	 * Turn off regular page output and return an error reponse
	 * for when rate limiting has triggered.
	 * @todo: i18n
	 * @access public
	 */
	function rateLimited() {
		global $wgOut;
		$wgOut->disable();
		wfHttpError( 500, 'Internal Server Error',
			'Sorry, the server has encountered an internal error. ' .
			'Please wait a moment and hit "refresh" to submit the request again.' );
	}

}

}

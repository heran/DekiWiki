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
 *
 * @package MediaWiki
 * @subpackage Skins
 */

/**
 * This is not a valid entry point, perform no further processing unless MEDIAWIKI is defined
 */
if (defined('MINDTOUCH_DEKI')) :

# See skin.doc

# Get a list of all skins available in /skins/
# Build using the regular expression '^(.*).php$'
# Array keys are all lower case, array value keep the case used by filename
#

/**
 * The main skin class that provide methods and properties for all other skins
 * including PHPTal skins.
 * This base class is also the "Standard" skin.
 * @package MediaWiki
 */
class Skin {
	/**#@+
	 * @access private
	 */
	var $lastdate, $lastline;
	var $linktrail ; # linktrail regexp
	var $rc_cache ; # Cache for Enhanced Recent Changes
	var $rcCacheIndex ; # Recent Changes Cache Counter for visibility toggle
	var $rcMoveIndex;
	var $postParseLinkColour = false;
	/**#@-*/

	function Skin() {
		global $wgContLang;
		$this->linktrail = $wgContLang->linkTrail();

		# Cache option lookups done very frequently
		$options = array( 'highlightbroken', 'hover' );
		foreach( $options as $opt ) {
			global $wgUser;
			$this->mOptions[$opt] = $wgUser->getOption( $opt );
		}
	}

	function getStylesheet() {
		return 'common/wikistandard.css';
	}

	function getSkinName() {
		return 'standard';
	}

	/**
	 * Get/set accessor for delayed link colouring
	 */
	function postParseLinkColour( $setting = NULL ) {
		return wfSetVar( $this->postParseLinkColour, $setting );
	}

	function qbSetting() {
		global $wgOut, $wgUser;

		if ( $wgOut->isQuickbarSuppressed() ) { return 0; }
		$q = $wgUser->getOption( 'quickbar' );
		if ( '' == $q ) { $q = 0; }
		return $q;
	}

	function setFavicon( &$out ) {
		global $wgActiveSkin, $wgActiveTemplate, $IP;
		
		// see if the favicon exists in the skin folder
		if (is_file($IP.'/skins/'.$wgActiveTemplate.'/'.$wgActiveSkin.'/favicon.ico')) 
		{
			global $wgStylePath;
			$favicon = $wgStylePath.'/'.$wgActiveTemplate.'/'.$wgActiveSkin.'/favicon.ico';
		}
		elseif (is_file($IP.'/skins/'.$wgActiveTemplate.'/favicon.ico')) 
		{
			global $wgStylePath;
			$favicon = $wgStylePath.'/'.$wgActiveTemplate.'/favicon.ico';
		}
		else 
		{
			global $wgScriptPath;
			$favicon = $wgScriptPath.'/favicon.ico';
		}
		$out->addLink( array( 'rel' => 'shortcut icon', 'href' => $favicon ) );
	}
	
	function initPage( &$out ) {
		$this->setFavicon($out);
	}

	function getParents() {
		global $wgArticle, $wgTitle;
		
		//it's conceivable that we'll have breadcrumb support in the future for non-main namespace pages
		if (!$wgTitle->isEditable()) 
		{
			return array();
		}
		$parents = $wgArticle->mParents;
		
		//special case: for articles, append the current page as unlinked text
		if (isset($wgArticle->mTitleText) && !is_null($wgArticle->mTitleText)) {
			$parents[] = '<a href="'.$wgTitle->getFullUrl().'" class="deki-ns'.strtolower(DekiNamespace::getCanonicalName($wgTitle->getNamespace())).' current">'.htmlspecialchars($wgArticle->mTitleText).'</a>';
		}
		return $parents;
	}
	function getHierarchyAsList() {
		$parents = $this->getParents();
		$count = count($parents);
		$i = 0;
		foreach ($parents as $key => $val) {
			$i++;
			$class = array();
			if ($i == 1) {
				$class[] = 'first';
			}
			if ($i == $count) {
				$class[] = 'last';
			}
			$parents[$key] = '<li'.(!empty($class) ? ' class="'.implode(' ', $class).'"': '').'>'.$val.'</li>';
		}
		return '<ol class="dw-hierarchy deki-hierarchy">'.implode("\n", $parents).'</ol>';
	}
	
	function getHierarchy() {
		$parents = $this->getParents();
		$rootNode = count($parents) == 1;
		
		//special case; when you're on the home page, let's add an extra delimiter at the end, so it doesn't look like freestanding text
		if ($rootNode) {
			$parents[] = '';
		}
		return is_array($parents) 
			? '<span class="dw-hierarchy">'.implode(' '.htmlspecialchars(wfMsg('Skin.Common.breadcrumb-delimiter')).' ', $parents).'</span>'
			: '';
	}
	
	function getRelatedPages() {
		global $wgArticle, $wgOut;

		$html = '';
		if ($wgArticle->getId() == 0) {
			return $html;
		}
		
		$relatedPages = DekiTag::getRelatedPages($wgArticle->getId());
		$links = array();
		
		foreach($relatedPages as $PageInfo)
		{
			$links[] = '<a href="' . $PageInfo->uriUi . '">' . htmlspecialchars($PageInfo->title) . '</a>';
		}
		
		$wgOut->setRelated($links);
		return empty($links) ? '' : '<div id="deki-page-related">' . $wgOut->getRelated(true) . '</div>';
	}
	
	function getExternalLinkAttributes( $link, $text, $class='' ) {
		global $wgContLang;

		$same = ($link == $text);
		$link = urldecode( $link );
		$link = $wgContLang->checkTitleEncoding( $link );
		$link = preg_replace( '/[\\x00-\\x1f_]/', ' ', $link );
		$link = htmlspecialchars( $link );

		if ($class != '') {
			$r = 'class="'.$class.'"';
		}
		else if (strpos($text, "<img ") === false) {
			if (substr($link, 0, 5) == 'https') {
				$class = 'link-https';
			}
			elseif (substr($link, 0, 6) == 'mailto') {
				$class = 'link-mailto';
			}
			elseif (substr($link, 0, 4) == 'news') {
				$class = 'link-news';
			}
			elseif (substr($link, 0, 3) == 'ftp') {
				$class = 'link-ftp';
			}
			elseif (substr($link, 0, 3) == 'irc') {
				$class = 'link-irc';
			}
			else {
				$class = 'external';
			}
			$r = ' rel="external" class="'.$class.'"';
		} else
            return '';

		if( !$same && $this->mOptions['hover'] ) {
			$r .= " title=\"{$link}\"";
		}
		return $r;
	}

	function getInternalLinkAttributes( $link, $text, $broken = false ) {
		$link = urldecode( $link );
		$link = str_replace( '_', ' ', $link );
		$link = htmlspecialchars( $link );

		if( $broken == 'stub' ) {
			$r = ' class="stub"';
		} else if ( $broken == 'yes' ) {
			$r = ' class="new"';
		} else {
			$r = '';
		}

		if( $this->mOptions['hover'] ) {
			$r .= " title=\"{$link}\"";
		}
		return $r;
	}

	/**
	 * @param Title $nt
	 * @param string $text
	 * @param bool $broken
	 * MT ursm:
	 * @param bool $useTitle	set true to use HTML title tag instead of TopicTip
	 * MT royk:
	 * @param bool $isEditLink	set true to define a specific edit section link, which suppresses title
	 */
	function getInternalLinkAttributesObj( &$nt, $text, $broken = false, $useTitle = false, $isEditLink = false, $_params = array() ) {
		global $wgRequest, $wgOut;
        $action = is_null($wgRequest) ? 'view' : $wgRequest->getVal( 'action', 'view' );

        $_class = array();

	    if( $broken == 'stub' ) {
			$_class[] = 'stub';
		} else if ( $broken == 'yes' ) {
			$_class[] = 'new';
		}

		# MT royk: add support for user icons next to user homepage links
		if ($nt->getNamespace() == NS_USER && !$isEditLink && !$wgOut->mExport) {
			$_class[] = 'link-user';
			$rel = 'internal';
		}

		if (isset($_params['class'])) {
			$_class = array($_params['class']);
		}
		$r = count($_class) > 0 ? ' class="'.implode(' ', $_class).'"': '';

		if ((isset($rel) && $rel != '') || (array_key_exists('rel', $_params) && $_params['rel'] != '')) {
			$r .= ' rel="'.(empty($_params['rel']) ? $rel: $_params['rel']).'"';
		}

		# MT royk: this is dirty
		if( $this->mOptions['hover'] && !$wgOut->mExport) {
			if ($isEditLink && !isset($_params['title'])) {
				$r .= ' title="'.wfMsg('Skin.Common.edit-section').'"';
			}
			# MT ursm: use normal title tooltip when specified or for Special NS
			elseif (($useTitle || $nt->getNamespace() == NS_SPECIAL || $nt->getNamespace() == NS_ADMIN || $action == 'print') && !isset($_params['title'])) {
				$r .= ' title="' . $nt->getEscapedText() . '"';
			}
			elseif (isset($_params['title']) && $_params['title'] != '') {
				$r .= ' title="'.$_params['title'].'"';
			}
		}
		if (isset($_params['stat'])) {
		    if ($_params['stat'] !== false)
                $r .= ' stat="'.$_params['stat'].'"';
		} else {
		    //$r .= ' stat="??"';
		}
		if (isset($_params['onclick']) && $_params['onclick'] != '') {
			$r .= ' onclick="'.$_params['onclick'].'"';
		}

		if (isset($_params['accesskey']) && $_params['accesskey'] != '') {
			$r .= ' accesskey="'.$_params['accesskey'].'"';
		}
		return $r;
	}
	
	function arrayToList($items = array()) {
		
		global $wgUser;
		
		$sk = $wgUser->getSkin();
		$count = count($items);
		$html = '<ul>';
		if ($count == 0) {
			return '';
		}
		$i = 0;
		foreach ($items as $item) {
			$i++;
			$class = array();
			if ($i == 1) {
				$class[] = 'first';
			}
			if ($i == $count) {
				$class[] = 'last';
			}
			$html .= '<li'.(!empty($class) ? ' class="'.implode(' ', $class).'"': '').'>'.$item.'</li>';
		}
		$html .= '</ul>';
		return $html;	
	}
	/**
	 * URL to the logo
	 */
	function getLogo() {
		global $wgLogo;
		return $wgLogo;
	}

	function getSiteLogo() {
		global $wgSitename;
		return '<a href="'.$this->makeUrl('').'" title="'.htmlspecialchars($wgSitename).'">'.'<img src="'.wfGetSiteLogo().'" alt="'.htmlspecialchars($wgSitename).'" title="'.htmlspecialchars($wgSitename).'"/></a>';
	}
	
	/**
	 * This will be called immediately before the <body> tag
	 */
	function afterContent()
	{
		return '';
	}

	function pageTitle() {
		global $wgOut, $wgTitle, $wgUser;

		$s = '<h1 class="pagetitle">' . htmlspecialchars( $wgOut->getPageTitle() ) . '</h1>';
		return $s;
	}

	function pageDisplayTitle() {
		global $wgTitle, $wgRequest, $wgOut;

		$ns = $wgTitle->getNamespace();

		//special namespace items need human-friendly titles
		if (($ns == NS_ADMIN || $ns == NS_SPECIAL) || (($ns == NS_USER || $ns == NS_TEMPLATE) && $wgTitle->getText() == '')) {

 			$titlePrefix = $wgTitle->getPrefixedText();
 			// special case
 			if ($titlePrefix == 'Special:Contributions')
			{
	 			$titleName = wfMsg('Page.Contributions.recent-changes-from', $wgRequest->getVal('target'));
 			}
 			else
			{
				@list($namespace, $langPage) = explode(':', $titlePrefix);
				if ($langPage == '')
				{
					// default namespace page titles
					switch ($ns)
					{
						case NS_ADMIN: $langPage = 'ControlPanel'; break;
						case NS_USER: $langPage = 'ListUsers'; break;
						case NS_TEMPLATE: $langPage = 'ListTemplates'; break;
					}
				}
				// new language keys are either Admin or Page for page titles
				$langNamespace = ($ns == NS_ADMIN) ? 'Admin' : 'Page';

				$titleName = wfMsg($langNamespace .'.'. $langPage .'.page-title');
 			}
		}
		else {
	        if ($wgRequest->getVal('wpNewPath'))
	            $titleFull = Article::combineName($wgRequest->getVal('wpNewPath'), $wgRequest->getVal('wpNewTitle'));
	        else
	            $titleFull = $wgTitle->getPrefixedText();
	        $title = Title::newFromText($titleFull);
	        Article::splitName($titleFull, $titlePath, $titleName);
	        $titleName = wfDecodeTitle($titleName);
	        if ($titleName == '') {
		        $titleName = wfHomePageTitle();
	        }	
		}

		return htmlspecialchars($titleName);
	}

 	function getSearchLink() {
 		$searchPage =& Title::makeTitle( NS_SPECIAL, 'Search' );
 		return $searchPage->getLocalURL();
 	}
 	function escapeSearchLink() {
 		return htmlspecialchars( $this->getSearchLink() );
 	}
	function searchForm() {
		global $wgRequest;
		$search = $wgRequest->getText( 'search' );

		$s = '<form name="search" class="inline" method="post" action="'
		  . $this->escapeSearchLink() . "\">\n"
		  . '<input type="text" name="search" size="19" value="'
		  . htmlspecialchars(substr($search,0,256)) . "\" />\n"
		  . '<input type="submit" name="go" value="' . wfMsg ('Skin.Common.submit-find') . '" />&nbsp;'
		  . '<input type="submit" name="fulltext" value="' . wfMsg ('Skin.Common.submit-search') . "\" />\n</form>";

		return $s;
	}

	function getPoweredBy() {
		return '<div class="poweredBy">'.wfMsg('Product.powered', DekiSite::getProductLink()).'</div>';
	}

	/***
	 * given a diff $value in seconds, will return a *fuzzy* human readable time
	 * it sacrifices precision (e.g. "2 weeks, 4 days, 5 hours, etc.") for readability (e.g. "3 weeks")
	 */
	function humanReadableTime($diffInSeconds) {

		if ($diffInSeconds == 0 || !$diffInSeconds) {
			return wfMsg('System.Common.time-a-few-seconds');
		}
		elseif ($diffInSeconds < 60) {
			return wfMsg($diffInSeconds > 1 ? 'System.Common.time-seconds': 'System.Common.time-second', $diffInSeconds);
		}
		elseif ($diffInSeconds < 3600) {
			$val = round($diffInSeconds / 60); //return minutes
			return wfMsg($val > 1 ? 'System.Common.time-minutes': 'System.Common.time-minute', $val);
		}
		elseif ($diffInSeconds < 86400) {
			$val = round($diffInSeconds / 3600); //return hours
			return wfMsg($val > 1 ? 'System.Common.time-hours': 'System.Common.time-hour', $val);
		}
		elseif ($diffInSeconds < 604800) {
			$val = round($diffInSeconds / 86400); //return days
			return wfMsg($val > 1 ? 'System.Common.time-days': 'System.Common.time-day', $val);
		}
		elseif ($diffInSeconds < 2629743) {
			$val = round($diffInSeconds / 604800); //return weeks
			return wfMsg($val > 1 ? 'System.Common.time-weeks': 'System.Common.time-week', $val);
		}
		elseif ($diffInSeconds < 31556926 ) {
			$val = round($diffInSeconds / 2629743); //return months
			return wfMsg($val > 1 ? 'System.Common.time-months': 'System.Common.time-month', $val);
		}
		else {
			$val = round($diffInSeconds / 31556926); //return years
			return wfMsg($val > 1 ? 'System.Common.time-years': 'System.Common.time-year', $val);
		}
	}

	/* MT: royk
	 * generates a date offset that a human can understand (e.g. two hours ago)
	 */
	function lastModifiedHumanReadable() {
		global $wgArticle;
		return Skin::_lastModifiedHumanReadable($wgArticle->getTimestamp());
	}

	function _lastModifiedHumanReadable($articleTimestamp) {
		global $wgArticle, $wgLang;

		$articleTimestamp = wfTimestamp(TS_UNIX, $articleTimestamp);
		$curTime = mktime();
		$diffTimestamp = $curTime - $articleTimestamp;
		if ($diffTimestamp == 0) {
			return wfMsg('System.Common.date-a-few-seconds');
		}
		elseif ($diffTimestamp < 60) {
			return wfMsg('System.Common.date-less-minute-ago');
		}
		elseif ($diffTimestamp < 3600) { //within the hour
			$time = round($diffTimestamp / 60); //return minutes
			return wfMsg($time > 1 ? 'System.Common.date-minutes-ago': 'System.Common.date-minute-ago', $time);
		}
		elseif ($diffTimestamp < 86400) { //today
			$time = round($diffTimestamp / 3600); //return hours
			return wfMsg($time > 1 ? 'System.Common.date-hours-ago': 'System.Common.date-hour-ago', $time);
		}
		elseif ($diffTimestamp < 172800) { //yesterday
			return wfMsg('System.Common.date-yesterday');
		}
		elseif ($diffTimestamp < 604800)  { //within a week
			$time = round($diffTimestamp / 86400); //return days
			return wfMsg($time > 1 ? 'System.Common.date-days-ago': 'System.Common.date-day-ago', $time);
		}
		elseif ($diffTimestamp < 1814400) { //within 21 days
			if (date('m', $curTime) != date('m', $articleTimestamp)) {
				return wfMsg('System.Common.date-month', wfMsg('System.Common.'.strtolower(date('F', $articleTimestamp))));
			}
			$curDay = date('d', $curTime);
			$edtDay = date('d', $articleTimestamp);
			$curDayOfWeek = date('w', $curTime);
			$edtDayOfWeek = date('w', $articleTimestamp);

			//if we're within a "calendar" week
			if (($curDay - $edtDay) < 7) {
				if (($curDayOfWeek - $edtDayOfWeek) > 0) {
					$time = round($diffTimestamp / 86400); //return days
					return wfMsg($time > 1 ? 'System.Common.date-days-ago': 'System.Common.date-day-ago', $time);
				}
			}

			//otherwise, set the current timestamp to the end of last week and calculate weeks
			$start = mktime(0, 0, 0, date('m', $curTime), $curDay - $curDayOfWeek, date('Y', $curTime));
			$time = round(($start - $articleTimestamp) / 604800); //return weeks
			if ($time == 0) {
				return wfMsg('System.Common.date-last-week');
			}
			return wfMsg($time > 1 ? 'System.Common.date-weeks-ago': 'System.Common.date-week-ago', $time);
		}
		elseif ($diffTimestamp < 31556926) { //within a year
			if (date('Y', $articleTimestamp) != date('Y', $curTime)
				&& date('F', $articleTimestamp) == date('F', $curTime)) {
				return wfMsg('System.Common.date-year-ago');
			}
			else {
				return wfMsg('System.Common.date-month', wfMsg('System.Common.'.strtolower(date('F', $articleTimestamp))));
			}
		}
		else { //return the date last modified
			return wfMsg('System.Common.date-over-year-ago');
		}
	}

	function lastModified($disabled = '') {
		global $wgLang, $wgArticle, $wgUser, $wgContLang;

		if ($wgArticle->getId() == 0 || !$wgArticle->getTitle()->isEditable() || !$wgArticle->getTimestamp()) {
			return $disabled;
		}
		$timestamp = $wgArticle->getTimestamp();
		$formattedts = $wgLang->timeanddate( $timestamp, true );
		$historylink = $wgArticle->getTitle()->getLocalUrl('action=history');

		$User = $wgArticle->getDekiUser();
		$UserTitle = $User->getUserTitle();

		$userlink = $wgUser->getSkin()->makeLinkObj($UserTitle, $User->toHtml());

		return wfMsg('Skin.Common.page-last-modified-full', sprintf('<a href="%s" title="%s">%s</a>', $historylink, $formattedts, $formattedts), $userlink);	
	}

	function lastOffset($disabled = '') {
		global $wgLang, $wgArticle, $wgUser, $wgContLang;

		if ($wgArticle->getId() == 0 || !$wgArticle->getTitle()->isEditable() || !$wgArticle->getTimestamp()) {
			return $disabled;
		}
		$timestamp = $wgArticle->getTimestamp();
		$formattedts = $this->lastModifiedHumanReadable();
		$historylink = $wgArticle->getTitle()->getLocalUrl('action=history');
		$userlink = $wgUser->getSkin()->makeLink( $wgContLang->getNsText(NS_USER) . ':' . $wgArticle->getUserText(), $wgArticle->getUserText() );
		return wfMsg('Skin.Common.page-last-modified-full', sprintf('<a href="%s" title="%s">%s</a>', $historylink, $formattedts, $formattedts), $userlink);	
	}

	function whatLinksHere() {
		global $wgTitle, $wgContLang;

		$s = $this->makeKnownLink( $wgContLang->specialPage( 'Whatlinkshere' ),
		  wfMsg( 'Skin.Common.what-links-here' ), 'target=' . $wgTitle->getPrefixedURL() );
		return $s;
	}

	/**
	 * Note: This function MUST call getArticleID() on the link,
	 * otherwise the cache won't get updated properly.  See LINKCACHE.DOC.
	 */
	function makeLink( $title, $text = '', $query = '', $trail = '', $_params = array() ) {
		wfProfileIn( 'Skin::makeLink' );
	 	$nt = Title::newFromText( $title );
		if ($nt) {
			$result = $this->makeLinkObj( $nt, $text, $query, $trail, '', $_params );
		} else {
			wfDebug( 'Invalid title passed to Skin::makeLink(): "'.$title."\"\n" );
			$result = $text == "" ? $title : $text;
		}

		wfProfileOut( 'Skin::makeLink' );
		return $result;
	}

	function makeKnownLink( $title, $text = '', $query = '', $trail = '', $prefix = '',$aprops = '', $useTitle = false, $isEditLink = false, $_params = array()) {
		$nt = Title::newFromText( $title );
		if ($nt) {
			return $this->makeKnownLinkObj( $nt, $text, $query, $trail, $prefix , $aprops, $useTitle, $isEditLink, $_params );
		} else {
			wfDebug( 'Invalid title passed to Skin::makeKnownLink(): "'.$title."\"\n" );
			return $text == '' ? $title : $text;
		}
	}

	function makeBrokenLink( $title, $text = '', $query = '', $trail = '' ) {
		$nt = Title::newFromText( $title );
		if ($nt) {
			return $this->makeBrokenLinkObj( Title::newFromText( $title ), $text, $query, $trail );
		} else {
			wfDebug( 'Invalid title passed to Skin::makeBrokenLink(): "'.$title."\"\n" );
			return $text == '' ? $title : $text;
		}
	}

	function makeStubLink( $title, $text = '', $query = '', $trail = '' ) {
		$nt = Title::newFromText( $title );
		if ($nt) {
			return $this->makeStubLinkObj( Title::newFromText( $title ), $text, $query, $trail );
		} else {
			wfDebug( 'Invalid title passed to Skin::makeStubLink(): "'.$title."\"\n" );
			return $text == '' ? $title : $text;
		}
	}

	/**
	 * Pass a title object, not a title string
	 */
	function makeLinkObj( &$nt, $text= '', $query = '', $trail = '', $prefix = '', $_params = array() ) {
		global $wgOut, $wgUser, $wgInputEncoding;
		$fname = 'Skin::makeLinkObj';


		# Fail gracefully
		if ( ! isset($nt) ) {
			# wfDebugDieBacktrace();

			return "<!-- ERROR -->{$prefix}{$text}{$trail}";
		}

		$ns = $nt->getNamespace();
		$dbkey = $nt->getDBkey();
		if ( $nt->isExternal() ) {
			$u = $nt->getFullURL();
			$link = $nt->getPrefixedURL();
			if ( '' == $text ) {
			    $text = htmlspecialchars($nt->getPrefixedText());
			    $expansion = '';
			}
			elseif ($text != $nt->getPrefixedText())
        		$expansion = '<span class="urlexpansion"> (<em>'.htmlspecialchars($nt->getPrefixedText()).'</em>)</span>';
            else
                $expansion = '';

			$style = $this->getExternalLinkAttributes( $link, $text, 'extiw' );

			$inside = '';
			if ( '' != $trail ) {
				if ( preg_match( '/^([a-z]+)(.*)$$/sD', $trail, $m ) ) {
					$inside = $m[1];
					$trail = $m[2];
				}
			}

			# Check for anchors, normalize the anchor

			$parts = explode( '#', $u, 2 );
			if ( count( $parts ) == 2 ) {
				$anchor = urlencode( do_html_entity_decode( str_replace(' ', '_', $parts[1] ),
									ENT_COMPAT,
									$wgInputEncoding ) );
				$replacearray = array(
					'%3A' => ':',
					'%' => '.'
				);
				$u = $parts[0] . '#' .
				str_replace( array_keys( $replacearray ),
				array_values( $replacearray ),
				$anchor );
			}

			$t = "<a href=\"{$u}\"{$style}>{$text}{$inside}</a>".$expansion;
			return $t;
		} elseif ( 0 == $ns && "" == $dbkey ) {
			# A self-link with a fragment; skip existence check.
			$retVal = $this->makeKnownLinkObj( $nt, $text, $query, $trail, $prefix );
		} elseif ( ( NS_SPECIAL == $ns ) || ( NS_ADMIN == $ns ) || ( NS_IMAGE == $ns ) ) {
			# These are always shown as existing, currently.
			# Special pages don't exist in the database; images may
			# occasionally be present when there is no description
			# page per se, so we always shown them.
			$retVal = $this->makeKnownLinkObj( $nt, $text, $query, $trail, $prefix );
		} elseif ( $this->postParseLinkColour ) {
		} else {
			wfProfileIn( $fname.'-immediate' );
			# Work out link colour immediately
			$aid = $nt->getArticleID() ;
			if ( 0 == $aid ) {
			    if (empty($retVal)) {
					$retVal = $this->makeBrokenLinkObj( $nt, $text, $query, $trail, $prefix );
			    }
			} else {
				$retVal = $this->makeKnownLinkObj( $nt, $text, $query, $trail, $prefix, '', false, false, $_params );
			}
			wfProfileOut( $fname.'-immediate' );
		}

		return $retVal;
	}

	/**
	 * Pass a title object, not a title string
	 * MT: royk: $_params is an array with override values for the link creation. the key/val pair of the array
	 * correspond to the attribute/value on the <a>. if it exists in the $_params, then it should override any
	 * mediawiki defaults. possible keys involve anything that can be an attribute in a <a>; custom keys include:
	 * suppressTT, which will suppress topic tip creation
	 */
	function makeKnownLinkObj( $nt, $text = '', $query = '', $trail = '', $prefix = '' , $aprops = '', $useTitle = false, $isEditLink = false, $_params = array() ) {
		global $wgOut, $wgTitle, $wgInputEncoding;

		$fname = 'Skin::makeKnownLinkObj';

		if ( !is_object( $nt ) ) {
			return $text;
		}

		if (!isset($_params['href'])) {
			$u = $nt->escapeLocalURL( $query );
			if ( '' != $nt->getFragment() ) {
				if( $nt->getPrefixedDbkey() == '' ) {
					$u = '';
					if ( '' == $text ) {
						$text = htmlspecialchars( $nt->getFragment() );
					}
				}
				$anchor = urlencode( do_html_entity_decode( str_replace(' ', '_', $nt->getFragment()), ENT_COMPAT, $wgInputEncoding ) );
				$replacearray = array(
					'%3A' => ':',
					'%' => '.'
				);
				$u .= '#' . str_replace(array_keys($replacearray),array_values($replacearray),$anchor);
			}
		}
		else {
			$u = $_params['href'];
		}
		if ( '' == $text ) {
			if ($nt->getNamespace() == NS_SPECIAL) {
				$text = wfMsg('System.Common.user-nobody');
			}
			else {
				$text = $nt->getDisplayText();
			}
    		$expansion = '';
		}
		elseif (isset($_params['expansion']) && $_params['expansion'] != '') {
			$expansion = $_params['expansion'];
		}
        else {
            $expansion = '';
        }

        $style = $this->getInternalLinkAttributesObj( $nt, $text, false, $useTitle, $isEditLink, $_params );

		$inside = '';
		if ( '' != $trail && !isset($_params['trail'])) {
			if ( preg_match( $this->linktrail, $trail, $m ) ) {
				$inside = $m[1];
				$trail = $m[2];
			}
		}
		elseif (isset($_params['trail']) && $_params['trail'] != '') {
			$trail = $_params['trail'];
		}

		$r = "<a href=\"{$u}\"{$style}{$aprops}>{$prefix}{$text}{$inside}</a>{$trail}".$expansion;

		return $r;
	}

	/**
	 * Pass a title object, not a title string
	 */
	function makeBrokenLinkObj( $nt, $text = '', $query = '', $trail = '', $prefix = '' ) {
		# Fail gracefully
		if ( ! isset($nt) ) {
			# wfDebugDieBacktrace();
			return "<!-- ERROR -->{$prefix}{$text}{$trail}";
		}

		$fname = 'Skin::makeBrokenLinkObj';


		# MT ursm: don't add 'action=edit' to broken links
		$u = $nt->escapeLocalURL( $query );

		if ( '' == $text ) {
			$text = Title::escapeText( wfDecodeTitle( $nt->getPrefixedText() ) );
		}
		$style = $this->getInternalLinkAttributesObj( $nt, $text, "yes" );

		$inside = '';
		if ( '' != $trail ) {
			if ( preg_match( $this->linktrail, $trail, $m ) ) {
				$inside = $m[1];
				$trail = $m[2];
			}
		}
		// make the text safe before outputting
		$text = htmlspecialchars($text);
		if ( $this->mOptions['highlightbroken'] ) {
			$s = "<a href=\"{$u}\"{$style}>{$prefix}{$text}{$inside}</a>{$trail}";
		} else {
			$s = "{$prefix}{$text}{$inside}<a href=\"{$u}\"{$style}>?</a>{$trail}";
		}


		return $s;
	}

	/**
 	 * Pass a title object, not a title string
	 */
	function makeStubLinkObj( $nt, $text = '', $query = '', $trail = '', $prefix = '' ) {
		$link = $nt->getPrefixedURL();

		$u = $nt->escapeLocalURL( $query );

		if ( '' == $text ) {
			$text = Title::escapeText( wfDecodeTitle( $nt->getPrefixedText() ) );
		}
		$style = $this->getInternalLinkAttributesObj( $nt, $text, 'stub' );

		$inside = '';
		if ( '' != $trail ) {
			if ( preg_match( $this->linktrail, $trail, $m ) ) {
				$inside = $m[1];
				$trail = $m[2];
			}
		}
		if ( $this->mOptions['highlightbroken'] ) {
			$s = "<a href=\"{$u}\"{$style}>{$prefix}{$text}{$inside}</a>{$trail}";
		} else {
			$s = "{$prefix}{$text}{$inside}<a href=\"{$u}\"{$style}>!</a>{$trail}";
		}
		return $s;
	}

	/***
	 * a helper function for making a naked link (<a href="$href">$text</a>)
	 * $title can be either the title object or a string; if a string, will assume to be in the  main namespace
	 */
	function makeNakedLink($title, $text, $_attr = array()) {
		if (is_string($title)) {
			if ($title == '') {
				return;
			}
			if ($title == '#') {
				global $wgTitle;
				$title = &$wgTitle;
			}
			else {
				$title = Title::newFromText($title);
			}
		}
		if (!isset($_attr['title'])) {
			$_attr['title'] = '';
		}
		if (!isset($_attr['suppressTT'])) {
			$_attr['suppressTT'] = true;
		}
		if (!isset($_attr['expansion'])) {
			$_attr['expansion'] = '';
		}
		global $wgUser;
		$sk = $wgUser->getSkin();
		return $sk->makeKnownLinkObj($title, $text, '', '', '', '', false, false, $_attr);
	}

	function makeSelfLinkObj( $nt, $text = '', $query = '', $trail = '', $prefix = '' ) {
		$u = $nt->escapeLocalURL( $query );
		if ( '' == $text ) {
			$text = Title::escapeText( wfDecodeTitle( $nt->getPrefixedText() ) );
		}
		$inside = '';
		if ( '' != $trail ) {
			if ( preg_match( $this->linktrail, $trail, $m ) ) {
				$inside = $m[1];
				$trail = $m[2];
			}
		}
		return "<strong>{$prefix}{$text}{$inside}</strong>{$trail}";
	}

	/* these are used extensively in SkinPHPTal, but also some other places */
	/*static*/ function makeSpecialUrl( $name, $urlaction='' ) {
		$title = Title::makeTitle( NS_SPECIAL, $name );
		$this->checkTitle($title, $name);
		return $title->getLocalURL( $urlaction );
	}
	
	/*static*/ function makeRegisterUrl() {
		global $wgAnonAccCreate;
		return $wgAnonAccCreate ? $this->makeSpecialUrl('Userlogin', 'register=true' ): '#';
	}
	
    /*static*/ function makeLogoutUrl() {
	    global $wgTitle, $wgTemplateOverrides;
	    if (array_key_exists('logouturl', $wgTemplateOverrides)) {
		 	return $wgTemplateOverrides['logouturl'];   
	    }
	    return $this->makeSpecialUrl('Userlogout', $wgTitle->getPrefixedURL() != ''? 'returntotitle=' . urlencode($wgTitle->getPrefixedText()) : '');
    }
    
    /*static*/ function makeLoginUrl($ReturnTitle = null) {
	    global $wgTitle, $wgTemplateOverrides;
	    if (array_key_exists('loginurl', $wgTemplateOverrides)) {
		 	return $wgTemplateOverrides['loginurl'];   
	    }
	    
	    $ReturnTitle = is_null($ReturnTitle) ? $wgTitle : $ReturnTitle;
	    
        $returnto = !is_null($ReturnTitle)
        	? 'returntotitle=' . urlencode($ReturnTitle->getPrefixedText())
        	: '';
		return $this->makeSpecialUrl('Userlogin', $returnto );
    }

	/*static*/ function makeAdminUrl($name, $urlaction='')
	{
		if ($name == '')
		{
			$title = Title::makeTitle(NS_SPECIAL, 'Admin');
		}
		else
		{
			$title = Title::makeTitle( NS_ADMIN, $name );
			$this->checkTitle($title, $name);
		}

		return $title->getLocalURL($urlaction);
	}

	/*static*/ function makeUrl ( $name, $urlaction='' ) {
		$title = Title::newFromText( $name );
		$this->checkTitle($title, $name);
		return $title->getLocalURL( $urlaction );
	}

	# If url string starts with http, consider as external URL, else
	# internal
	/*static*/ function makeInternalOrExternalUrl( $name ) {
		return Skin::isExternalUrl( $name ) ? $name : $this->makeUrl( $name );
	}

	# Is this an external link?
	/*static*/ function isExternalUrl( $name ) {
		return strncmp( $name, 'http', 4 ) == 0;
	}

	# this can be passed the NS number as defined in Language.php
	/*static*/ function makeNSUrl( $name, $urlaction='', $namespace=0 ) {
		$title = Title::makeTitleSafe( $namespace, $name );
		$this->checkTitle($title, $name);
		return $title->getLocalURL( $urlaction );
	}

	/* these return an array with the 'href' and boolean 'exists' */
	/*static*/ function makeUrlDetails ( $name, $urlaction='' ) {
		$title = Title::newFromText( $name );
		$this->checkTitle($title, $name);
		return array(
			'href' => $title->getLocalURL( $urlaction ),
			'exists' => $title->getArticleID() != 0?true:false
		);
	}

	# make sure we have some title to operate on
	/*static*/ function checkTitle ( &$title, &$name ) {
		if(!is_object($title)) {
			$title = Title::newFromText( $name );
			if(!is_object($title)) {
				$title = Title::newFromText( '--error: link target missing--' );
			}
		}
	}
		
	/**
	 * Render any custom dashboard header html for a page
	 * @param Article $Article
	 * @param Title $Title
	 * @return string - $html to render
	 */
	function renderUserDashboardHeader($Article, $Title) {
		return '';
	}

	function specialLink( $name, $key = '' ) {
		global $wgContLang;

		if ( '' == $key ) { $key = strtolower( $name ); }
		$pn = $wgContLang->ucfirst( $name );
		return $this->makeKnownLink( $wgContLang->specialPage( $pn ), wfMsg( $key ) );
	}

	function makeExternalLink( $url, $text, $escape = true ) {
		global $wgOut;

		$style = $this->getExternalLinkAttributes( $url, $text );
		global $wgNoFollowLinks;

		$style .= ' rel="nofollow"';

		$url = htmlspecialchars( $url );
		if( $escape ) {
			$text = htmlspecialchars( $text );
		}
		#MT: royk: urlexpansion
		if (!$wgOut->mExport) { //dirty
			$expansion = '<span class="urlexpansion"> (<em>'.$url.'</em>)</span>';
		}

		return '<a href="'.$url.'"'.$style.'>'.$text.'</a>'.(($text != $url) ? $expansion: '');
	}


	/**
	 * This function is called by all recent changes variants, by the page history,
	 * and by the user contributions list. It is responsible for formatting edit
	 * comments. It escapes any HTML in the comment, but adds some CSS to format
	 * auto-generated comments (from section editing) and formats [[wikilinks]].
	 *
	 * The &$title parameter must be a title OBJECT. It is used to generate a
	 * direct link to the section in the autocomment.
	 * @author Erik Moeller <moeller@scireview.de>
	 *
	 * Note: there's not always a title to pass to this function.
	 * Since you can't set a default parameter for a reference, I've turned it
	 * temporarily to a value pass. Should be adjusted further. --brion
	 */
	function formatComment($comment, $title = NULL) {
		$fname = 'Skin::formatComment';


		global $wgContLang;
		// guerrics: hack to support hidden revisions, comes in as array
		if (is_array($comment))
		{
			$comment = '';
		}
		$comment = str_replace( "\n", " ", $comment );
		$comment = htmlspecialchars($comment);

		# The pattern for autogen comments is / * foo * /, which makes for
		# some nasty regex.
		# We look for all comments, match any text before and after the comment,
		# add a separator where needed and format the comment itself with CSS
		while (preg_match('/(.*)\/\*\s*(.*?)\s*\*\/(.*)/', $comment,$match)) {
			$pre=$match[1];
			$auto=$match[2];
			$post=$match[3];
			$link='';
			if($title) {
				$section=$auto;

				# This is hackish but should work in most cases.
				$section=str_replace('[[','',$section);
				$section=str_replace(']]','',$section);
				$title->mFragment=$section;
				$link=$this->makeKnownLinkObj($title, wfMsg('Skin.Common.section-link'));
			}
			$sep='-';
			$auto=$link.$auto;
			if($pre) { $auto = $sep.' '.$auto; }
			if($post) { $auto .= ' '.$sep; }
			$auto='<span class="autocomment">'.$auto.'</span>';
			$comment=$pre.$auto.$post;
		}

		# format regular and media links - all other wiki formatting
		# is ignored
		$medians = $wgContLang->getNsText(NS_MEDIA).':';
		while(preg_match('/\[\[(.*?)(\|(.*?))*\]\](.*)$/',$comment,$match)) {
			# Handle link renaming [[foo|text]] will show link as "text"
			if( "" != $match[3] ) {
				$text = $match[3];
			} else {
				$text = $match[1];
			}
			if( preg_match( '/^' . $medians . '(.*)$/i', $match[1], $submatch ) ) {
				# Media link; trail not supported.
				$linkRegexp = '/\[\[(.*?)\]\]/';
				$thelink = $this->makeMediaLink( $submatch[1], "", $text );
			} else {
				# Other kind of link
				if( preg_match( wfMsgForContent( 'Skin.Common.regex-link-trail' ), $match[4], $submatch ) ) {
					$trail = $submatch[1];
				} else {
					$trail = "";
				}
				$linkRegexp = '/\[\[(.*?)\]\]' . preg_quote( $trail, '/' ) . '/';
				if ($match[1][0] == ':')
					$match[1] = substr($match[1], 1);
				$thelink = $this->makeLink( $match[1], $text, "", $trail );
			}
			$comment = preg_replace( $linkRegexp, $thelink, $comment, 1 );
		}

		return $comment;
	}

	function _editSectionLink( $nt, $section, $sectionDivID, $anchor, $text ) {
		return $text.'<a href="#' . $anchor . '" onclick="doEditSection (\'' . $nt->getPrefixedUrl() . '\',' . $section . ',\'section_' . $sectionDivID . '\', \''.$anchor.'\');return false;" title="'.wfMsg('Skin.Common.edit-section').'" class="editsectionlink">'.Skin::iconify('edit').'</a>';
	}

	function editSectionLink( $nt, $section, $sectionDivID, $anchor ) {
		return "\n".'<span class="editsection">' . Skin::_editSectionLink( $nt, $section, $sectionDivID, $anchor, '<span>' . wfMsg('Skin.Common.edit-section') . '</span>' ) . '</span>'."\n";
	}

	function editSectionHref( $nt, $section, $sectionDivID, $text, $anchor ) {
		global $wgRequest;

		if( $wgRequest->getInt( 'revision' ) && ( $wgRequest->getVal( 'diff' ) != '0' ) ) {
			# Section edit links would be out of sync on an old page.
			# But, if we're diffing to the current page, they'll be
			# correct.
			return $text;
		}

		return Skin::_editSectionLink( $nt, $section, $sectionDivID, $anchor, '<span>' . $text . '</span>' );
	}

	/***
	 * jsLangVars()
	 * mt: royk
	 * takes language.php values and stores them as JS variables so we can keep all messages in one location
	 * @access public
	 *
	 * @note (guerrics) Function has been updated to work with namespaced keys
	 */
	function jsLangVars($_lang = array(), $includeAll = true)
	{
		global $wgJavascriptMessages;

		// @note (guerrics) renamed array 'LocalizationText' to 'aLt' to reduce the amount of text pushed to client
	    $return = 'var aLt = aLt || [];';
		foreach ($_lang as $value)
		{
			$return .= 'aLt["'.$value.'"] = \''.wfEncodeJSString(wfMsg($value)).'\'; ';
		}

		if ($includeAll && is_array($wgJavascriptMessages))
		{
			foreach ($wgJavascriptMessages as $key => $value)
			{
				$return .= 'aLt["'. $key .'"] = \''. wfEncodeJSString($value) .'\'; ';
			}
		}

		$return .= " var wfMsg = wfMsg || function (key) { return aLt[key] ? aLt[key] : 'MISSING: ' + key; };";
		return $return;
	}

	function getSkinDir() {
		global $wgActiveTemplate, $wgActiveSkin, $IP;
		return 	$IP.DIRECTORY_SEPARATOR.'skins'.DIRECTORY_SEPARATOR.$wgActiveTemplate.DIRECTORY_SEPARATOR.$wgActiveSkin;
	}
	function getSkinPath() {
		global $wgActiveTemplate, $wgActiveSkin, $wgStylePath;
		return 	$wgStylePath.'/'.$wgActiveTemplate.'/'.$wgActiveSkin;
	}
	function getTemplatePath() {
		global $wgActiveTemplate, $wgStylePath;
		return 	$wgStylePath.'/'.$wgActiveTemplate;
	}
	function getTemplateDir() {
		global $wgActiveTemplate, $IP;
		return 	$IP.'/skins/'.$wgActiveTemplate;
	}
	function getCommonPath() {
		global $wgStylePath;
		return 	$wgStylePath.'/common';
	}
	
	/**
	 * Static: create an empty div element which is eventually replaced with debugging information
	 */ 
	function getReportHtml() {
		// avoid chance for collision with real DOM element
		global $wgRequestTime;
		return '<div id="mindtouch_debug_report_' . $wgRequestTime . '"></div>';
	}

    /***
     * Stuff that needs to be injected at the bottom of each page
     */
    function getPageFooter() {
	    global $wgOut;
	    return MTMessage::output().'<div id="menuFiller"></div><div id="bodyHeight"></div>'
	    	. Skin::getAnalytics()."\n". Skin::getReportHtml();
    }
	function getPageHeader()
	{
		$html = '';
		$result = DekiPlugin::executeHook(Hooks::SKIN_RENDER_PAGE_HEADER, array(&$html));
		if ($result != DekiPlugin::HANDLED_HALT)
		{
			$productMessage = '';
			$expiryMessage = '';
			$license = strtolower(DekiSite::getStatus());
			if (DekiSite::isDeactivated())
			{
				if (DekiSite::isInactive())
				{
					$productMessage = 
						'<div class="expired">'.
							'<a href="'. wfGetControlPanelUrl('product_activation') .'">'.
								wfMsg('Skin.Common.status.'. $license).
							'</a>'.
						'</div>';
				}
				else
				{
					$productMessage = 
						'<div class="expired">'.
							'<a href="'.ProductURL::COMMERCIAL.'" target="_blank">'.wfMsg('Skin.Common.status.'.$license).'</a>'.
						'</div>';
				}
			}
			else
			{
				$days = DekiSite::willExpire();
				if ($days > 0) 
				{
					$expiryMessage = '<div class="expired"><a href="'.ProductURL::COMMERCIAL.'" target="_blank">'.wfMsg('Common.status.willexpire', $days).'</a></div>';
				}
			}
			
			$html .= $productMessage . $expiryMessage;
		}
		
		return '<noscript><div class="noscript">'. wfMsg('Skin.Common.no-script') .'</div></noscript>'
			. $html
			. wfMessagePrint('header');
    }

    function isNewPage() {
	    global $editor, $wgArticle;
	    return $editor && $wgArticle->getId() == 0;
    }
    function isArticlePage() {
	    global $editor, $wgArticle;
	    if (Skin::isSpecialPage() || Skin::isAdminPage()) {
		    return false;
	    }
	    //User: renders as a "normal" user page, when it should be handled as a Special: page
	    if ($wgArticle->mTitle->getNamespace() == NS_USER && $wgArticle->mTitle->getText() == '') {
		    return false;
	    }
        return $wgArticle->getID() > 0 || $wgArticle->mTitle->isEmptyNamespace();
    }
    function getPageAction() {
	    if (!isset($_GET['action']) || $_GET['action'] == '') {
		    return 'view';
	    }
	    return $_GET['action'];
    }
    function isEditPage() {
	    global $editor;
	    return $editor ? true: false;
    }
    function isViewPage($ignoreNamespace = true) {
	    global $wgRequest, $wgTitle;
	    return ($wgRequest->getVal('action') == '' || $wgRequest->getVal('action') == 'view')
	    	&& (!$wgRequest->getVal('revision') && !$wgRequest->getVal('oldid') && !$wgRequest->getVal('diff') && $wgTitle->canEditNamespace());
    }
    function isTalkPage() {
	    global $wgTitle;
	    return $wgTitle->isTalkPage();
    }
    function isAdminPage() {
	    global $wgTitle;
	    return $wgTitle->isAdminPage();
    }
    function isPrintPage() {
	    global $wgRequest;
	    return $wgRequest->getVal('action') == 'print' || $wgRequest->getVal('action') == 'export';
    }
    function isSpecialPage() {
	    global $wgTitle;
	    return $wgTitle->isSpecialPage();
    }

    function getPrintCSS() {
	    return '<link rel="stylesheet" type="text/css" media="print" href="'.Skin::getCommonPath().'/print.css" />';
    }
    function getScreenCSS() {
	    $css = '<link rel="stylesheet" type="text/css" media="screen" href="'.Skin::getCommonPath().'/css.php" />';
	    if (Skin::isPrintPage()) {
		    $css .= '<link rel="stylesheet" type="text/css" media="screen" href="'.Skin::getCommonPath().'/printview.css" />'
		    	.'<!--[if IE 6]><style type="text/css">@import "'.Skin::getCommonPath().'/printview-ie.css";</style><![endif]-->';
	    }
	    
	    global $wgOut;
	    $css .= $wgOut->getCss();
	    
	    $css .= ' <!--[if IE 7]><style type="text/css">@import "'.Skin::getCommonPath().'/_ie7.css";</style><![endif]-->'
	    	.'<!--[if IE 6]><style type="text/css">@import "'.Skin::getCommonPath().'/_ie.css";</style><![endif]-->';
	    
	    return $css;
    }

    /***
     * RoyK: Gets a list of templates, based on file structure
     * Valid templates exist in folder /skins/{foldername} ({foldername} is lowercased)
     */
    function getTemplates() {
	    global $wgStyleDirectory;
	    $dirs = wfGetDirectories($wgStyleDirectory, array('.svn', 'local', 'common'));
	    foreach ($dirs as $dirname => $dirpath) {
		    if (!Skin::getTemplateFile($dirname)) {
			    unset($dirs[$name]);
		    }
	    }
	    return $dirs;
    }

    /***
     * Gets path to template file itself
     */
    function getTemplateFile($tplname) {
	    global $wgStyleDirectory;
		$files = wfGetFileNames($wgStyleDirectory.DIRECTORY_SEPARATOR.strtolower($tplname));
		foreach ($files as $file => $p) {
		    if (substr($file, strlen($file) - 4, 4) == '.php') {
			 	$tname = substr($file, 0, -4);
			 	if (strtolower($tname) == $tplname) {
				 	return $p;
			 	}
		    }
	    }
	    return false;
    }
    /***
     * Gets path to template name itself (for class instantiation)
     */
    function getTemplateNameFromPath($tplpath) {
	    $path = explode(DIRECTORY_SEPARATOR, $tplpath);
	    $filename = array_pop($path);
	    return substr($filename, strlen($filename) - 4) == '.php' ? substr($filename, 0, -4): $filename;
    }

    /***
     * Returns a list of skins
     */
    function getSkins($tpl) {
	    global $wgStyleDirectory;
	    $tplpath = $wgStyleDirectory.DIRECTORY_SEPARATOR.strtolower($tpl);
	    $dirs = wfGetDirectories($tplpath, array('.svn'));
	    return $dirs;
    }

    function getEmbeddedJavascript() {
	    global $wgStyleDirectory;
	    include($wgStyleDirectory.'/common/javascript.php');

	    //$javascript comes from javascript.php; the output buffering catches it
	    return $javascript;
    }
    function getJavascript()
	{
	    $js = '';
	 	$js.= '<script type="text/javascript" src="'. Skin::getCommonPath() .'/js.php?perms='. Skin::getPermissionFlags() . '"></script>';

	 	return $js;
    }

    function getPermissionFlags() {
	    global $wgUser, $wgArticle;

	    $permissions = array();

	    /***
	     * For articles, check article permission flags before user permission flags, since the user may have grants
	     * on a particular page.
	     *
	     * This can lead to some non-optimal behavior, as users like admin with many flags will actually request a different _jscripts.php
	     * on special pages, which can lead to more UI files.
	     *
	     * TODO: Is it better that admin users get the same big file everytime, or two types of files depending on what page they're on?
	     */
	    if ($wgArticle->getTitle()->canEditNamespace()) 
	    {
		    if ($wgArticle->getId() > 0) 
		    {
			 	$permissions = $wgArticle->getPermissions();
		 	}
		 	//for pages that don't exist, look up parent (see bug #3843)
		 	else 
		 	{
			 	$permissions = $wgArticle->getParentPermissions();
		 	}
	    }
	    else 
	    {
		    $permissions = $wgUser->getPermissions();
	    }
	    
	    //hack hack for perf; don't load editor for non-editable pages
		if (!$wgArticle->getTitle()->isEditable()) 
		{
			$key = array_search('UPDATE', $permissions);
			if ($key !== false) 
			{
				unset($permissions[$key]);
			}
		}
	    return (!is_array($permissions) || empty($permissions)) ? '': implode(',', $permissions);
    }

	function getAnalytics() {
		global $wgDefaultAnalytics, $wgGoogleAnalytics, $wgGoogleAnalyticsDomain;

		if (empty($wgGoogleAnalytics)) {
			return;
		}
		return '<script type="text/javascript">'
			. '$(function() {'
			.'var gaJsHost = (("https:" == document.location.protocol) ? "https://ssl." : "http://www.");'
			.'$.getScript(gaJsHost + "google-analytics.com/ga.js", function() {'
			.'try {'
			.'var pageTracker = _gat._getTracker("'.$wgGoogleAnalytics.'");'
			.(($wgDefaultAnalytics != $wgGoogleAnalytics) && ($wgGoogleAnalyticsDomain == '.mindtouch.com') ? '' : 'pageTracker._setDomainName("' . $wgGoogleAnalyticsDomain. '");')
			.'pageTracker._trackPageview();'
			.'} catch(e) {}});});'
			.'</script>';
	}

	/***
	 * MT: royk
	 * iconifying text
	 */
	function iconify($class = false, $isSmall = false, $parentClass = 'icon') {
		$_class = $isSmall ? array('icon-s'): array();
		if ($class !== false) {
			$_class[] = $class;
		}
		return '<span class="'.$parentClass.'"><img src="/skins/common/icons/icon-trans.gif" '
			.(count($_class) > 0 ? 'class="'.implode(' ', $_class).'"': '').' alt="" /></span>';
	}
	/***
	 * MT: royk
	 * create a <SELECT> form element
	 */
	function formSelect($name, $data, $setValue = false, $params = array()) {
		return wfSelectForm($name, $data, $setValue, $params);
	}
}

endif;

<?php
/*
 * MindTouch Core - open source enterprise collaborative networking
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
 * Breadcrumb object
 * @name Breadcrumb
 * @copyright MindTouch, Inc. 2005
 * @author steveb
 * @package MindTouch
 * @version 1.0
 */

// constants
define('BC_TRAIL', 'bctrail');
define('BC_START', 'bcstart');
define('BC_INDEX', 'bcindex');
define('BC_BEFORE', 'before');
define('BC_AFTER', 'after');
define('BC_DEFAULT_LEN', 5);
define('BC_MIN_LEN', 0);
define('BC_MAX_LEN', 16);

// register hook to clean-up breadcrumbs when user logs out
$wgHooks['UserLogoutComplete'][] = "breadcrumbReset";

function breadcrumbTrail($bc) {
	echo(showBreadcrumbTrail());
}
function showBreadcrumbTrail() {
	$bc = New breadcrumb();
	$bc->restoreState($_SESSION);
	$bc->updateState($_GET);
	// StepanR: We must save params here to provide multipage scroll of breadcrumbs
	$bc->storeState($_SESSION);
	return $bc->getTrail();
}
/**
	breadcrumbReset()

	clears the breadcrumb state from the session table
	
	@access public
*/		
function breadcrumbReset() {
	if (isset($_SESSION)) {
		unset($_SESSION[BC_TRAIL]);
		unset($_SESSION[BC_START]);
		unset($_SESSION[BC_INDEX]);
	}
}

/**
	breadcrumbSet()
	
	sets the breadcrumb state in the session table

	@access public
	@param	string	trail	serialized breadcrumb array
	@param	int		start	leftmost visible page index
	@param	int		index	current index in session
*/		
function breadcrumbSet($trail, $start, $index, $responseTime, $responseSize) {
	if (isset($_SESSION)) {
		$_SESSION[BC_TRAIL] = $trail;
		$_SESSION[BC_START] = $start;
		$_SESSION[BC_INDEX] = $index;
	}
}

/**
	breadcrumbMostRecentPage()
	
	@access public
	@return	string	prefixed db-key of most recently visited page
*/
function breadcrumbMostRecentPage() {
	if (!isset($_SESSION)) {
		return NULL;
	}
	
	// load breadcrumbs
	$bc = new breadcrumb();
	$bc->restoreState($_SESSION);
	$result = $bc->getMostRecentPage();
	
	// check if we have a most-recent page
	if (!isset($result)) {
		$title = Title::newFromText(wfHomePageInternalTitle());
		return $title->getPrefixedDBkey();
	}
	return $result;
}

class breadcrumb { 
	
	//--- States ---
	// $session[BC_TRAIL]	// array: encoded list of visited pages
	// $session[BC_START]	// int: leftmost visible page index
	// $session[BC_INDEX]	// int: current index in session
	// $get['bc']			// int/string: selected breadcrumb command; the value is either
							//             an index (e.g. /index.php?title=Main_Page&bc=4)
							//             or a verb (e.g. /index.php?title=Main_Page&bc=after)
	
	//--- Fields ---
	var $bclen;				// int: length of displayed breadcrumbs trail
	var $curindex;			// int: selected index in breadcrumb
	var $previndex;			// int: previous index in breadcrumb
	var $curpage;			// string: title of current page
	var $crumbs;			// array of string: list of visited pages
	var $startindex;		// int: leftmost visible page index
	var $dirty;				// bool: session state has changed
	
	//--- Constructors ---
	function breadcrumb($bclen = NULL) {
		global $wgUser;
	
		// check if we were given breadcrumbs trail length or if
		// we can extract from the global user instance
		if (isset($bclen)) {
			$this->bclen = $bclen;
		} else if (isset($wgUser)) {
			$this->bclen = intval($wgUser->getOption( 'bclen' ));
		}
		
		// set a default value if length is not set properly
		if (!isset($this->bclen) || !is_int($this->bclen) || ($this->bclen < BC_MIN_LEN) || ($this->bclen > BC_MAX_LEN)) {
			$this->bclen = BC_DEFAULT_LEN;
		}
	}
	
	//--- Methods ---
	/**
		restoreState()
		
		loads the state of the breadcrumb from the session and get tables
		
		@access public
		@param	array	&session	reference to session table
	*/		
	function restoreState(&$session) {
		global $wgTitle;
		
		// identify current page
		$this->curpage = $wgTitle->getPrefixedDBkey();
		
		// check if a breadcrumbs trail was stored in the session
		if (isset($session[BC_TRAIL])) {
			$this->crumbs = unserialize($session[BC_TRAIL]);
			if (!is_array($this->crumbs)) $this->crumbs = array();
		} else {
			$this->crumbs = array();
		}

		// check if leftmost page index was stored in the session
		if (isset($session[BC_START])) {
			$this->startindex = $session[BC_START];
		} else {
			$this->startindex = count($this->crumbs) - 1;
		}
		
		// check if a previous breadcrumb index was stored
		if (isset($session[BC_INDEX])) {
			$this->previndex = $session[BC_INDEX];
			
			// check if current page corresponds to previously selected page
			// USE CASE: user navigates to same page that was selected in breadcrumb;
			//           we don't want to add a new page to the end, but
			//           preserve the previous selection instead.
			if (!isset($this->curindex) && $this->previndex >= 0 && (isset($this->crumbs[$this->previndex]) && $this->crumbs[$this->previndex] === $this->curpage)) {
				$this->curindex = $this->previndex;
			}
		}
		
		// check if current page is the same as most recent page
		// USE CASE: the use is automatically redirected to the most recent page
		//           in the breadcrumbs whe he logs in; however, there is no
		//           previous index to detect the duplicate entry.
		if (!isset($this->curindex) && (count($this->crumbs) > 0)) {
			if (isset($this->crumbs[0]) && $this->crumbs[0] === $this->curpage) {
				$this->curindex = 0;
			}
		}
		
		// check if current page is a page we don't want in the breadcrumb trail
		// USE CASE: user navigated to one of the pages we don't want to track, such
		//		     as all pages in the Help:, Special:, and Template: namespaces.
		if (!isset($this->curindex) && (
		    ($wgTitle->getNamespace() === NS_SPECIAL) ||
		    ($wgTitle->getNamespace() === NS_TEMPLATE) ||
		    ($wgTitle->getNamespace() === NS_ADMIN) ||
		    ($wgTitle->getNamespace() === NS_HELP)
		)) {
			$this->curindex = -1;
		}
	}
	
	/**
		updateState()
		
		read current request parameters and update state accordingly
	
		@access public
		@param	array	&get		reference to get table
	*/
	function updateState(&$get) {
		
		// check if current page request contains a breadcrumb command
		if (isset($get['bc'])) {
			$index = $get['bc'];
			
			// check if breadcrumb is a command
			if ($index === BC_BEFORE) {
				$this->dirty = true;
				$this->startindex = min($this->startindex + $this->bclen, count($this->crumbs) - 1);
			} elseif ($index === BC_AFTER) {
				$this->dirty = true;
				// StepanR: 0 was changed to $this->bclen-1
				$this->startindex = max($this->startindex - $this->bclen, $this->bclen-1);
			} elseif (is_numeric($index)) {
			
				// check if breadcrumb index is valid
				// USE CASE: user may have used an old url with an out-of-date breadcrumb index;
				//           just ignore the breadcrumb index.
				if ($this->crumbs[$index] === $this->curpage) {
					$this->dirty = true;
					$this->curindex = $index;

					// check if we need to change the bounds of the visible trail
					if ($this->curindex > $this->startindex) {
						$this->startindex = min($this->curindex + ($this->bclen - 1), count($this->crumbs) - 1);
					} elseif ($this->curindex < ($this->startindex - ($this->bclen - 1))) {
						$this->startindex = $this->curindex;
					}
				}
			}
		}
								
		// check if we have a new page
		if (!isset($this->curindex)) {
			$this->dirty = true;
			
			// add the new page
			array_unshift($this->crumbs, $this->curpage);
			$this->curindex = 0;
			$this->startindex++;
				
			// check if we need to shift the breadcrumbs' start index
			if ($this->startindex >= $this->bclen) {
				$this->startindex = $this->bclen - 1;
			}
			
			// StepanR: Remove duplicates
			$this->crumbs = array_unique($this->crumbs);
		
			// check if we need to clip the breadcrumbs
			if (count($this->crumbs) > BC_MAX_LEN) {					
				array_pop($this->crumbs);
			}
		}
	}
	
	/**
		storeState()
		
		saves the state of the breadcrumb into the session table

		@access public
		@param	array	&session	reference to session table
	*/		
	function storeState(&$session) {
		if ($this->dirty) {
			$session[BC_TRAIL] = serialize($this->crumbs);
			$session[BC_START] = $this->startindex;
			$session[BC_INDEX] = $this->curindex;
			$this->dirty = false;
		}
	}
		
	/**
		getTrail()
		
		creates the visible part of the breadcrumb trail (this method is const)

		@access public
		@return	string	HTML representation of the visible breadcrumb trail
	*/		
	function getTrail($aUseAJAX = false) {
	    if ($aUseAJAX === true) {
            $lTrail = $this->getTrailWithAjax();
	    }
	    else {
            $lTrail = $this->getTrailSimple();
	    }
	    return ''.implode('<br/>', $lTrail).'';
	}
	
	/**
		getUpdateScript()
		
		creates the javascript to include into the page

		@access public
		@return	string	javascript
	*/		
	function getUpdateScript() {
		$result = "var _sendBC = function() {\n";
		$result .= "  if (typeof _endtime == 'undefined') { setTimeout(_sendBC, 50); return; }\n";
		$result .= "  var timeDiff = _endtime - _starttime;\n";
		$result .= "  x_breadcrumbSet(\"".addslashes(serialize($this->crumbs))."\", {$this->startindex}, {$this->curindex}, timeDiff, _size, function () {});\n";
        $result .= "}\n";
	    $result .= "setTimeout(_sendBC, 50);\n";
		return $result;
	}
	
	/**
		getMostRecentPage()
		
		return the db-key of the most recently visited page or the last page in the breadcrumb trail
		
		@access public
		@return	string	prefixed db-key of most recently visited page
	*/
	function getMostRecentPage() {
	
		// check if we have a breadcrumbs trail
		$count = count($this->crumbs);
		if ($count == 0) {
			return NULL;
		}
		
		// check if we have a previous index
		if (isset($this->previndex) && isset($this->crumbs[$this->previndex])) {
			return $this->crumbs[$this->previndex];
		}
		return $this->crumbs[0];
	}
	
	/**
	 * Get breadcrumb list with AJAX functionality, recent BC_DEFAULT_LEN pages are shown with scrolling.
	 * The whole number of links is BC_MAX_LEN.
	 *
	 * @access private
	 * @return array of HTML lines
	 */
	function getTrailWithAjax()	{
		global $wgTitle, $wgUser;
		
		$count = count($this->crumbs);
		
		// StepanR: Save empty html block for future use.
		$lHtmlEmptyBlock = '<span class="none">&nbsp;</span>';
		
		// extract visible trail
		$endindex = max($this->startindex - ($this->bclen - 1), 0);
		$trail = array_slice($this->crumbs, $endindex, $this->startindex - $endindex + 1);
		
		// convert page names into html links
		$this->convertNamesToLinks($trail);

		// check if we need to add ellipses
		if ($endindex > 0) {
			array_unshift($trail, '<a href="#" onclick="x_breadcrumbTrail(\''.BC_AFTER.'\', breadcrumbLoad);return false;">...</a>');
		}
		// StepanR: if we don't need scrolling up, insert free space span to avoid line jumping
		else 
		{
			array_unshift($trail, $lHtmlEmptyBlock);
		}
		if ($this->startindex < ($count - 1)) {
			//'<a href="'.$wgTitle->getLocalURL('bc='.BC_BEFORE).'">...</a>'
			array_push($trail, '<a href="#" onclick="x_breadcrumbTrail(\''.BC_BEFORE.'\', breadcrumbLoad);return false;">...</a>');
		}
		// StepanR: if we don't need scrolling down, insert free space span to avoid line jumping
		else 
		{
			array_push($trail, $lHtmlEmptyBlock);
		}
		
		// reverse output and concatenate strings
// StepanR: lastest links should be first, no reverse need here
//		$trail = array_reverse($trail);

// StepanR: These array cut causes errors with previous code.
//		while (count($trail) > 5) {
//			array_shift($trail);	
//		}

		// StepanR: I've changed 5 to BC_DEFAULT_LEN + 2 to avoid visual line jumps for empty/short list.
		while (count($trail) < BC_DEFAULT_LEN + 2) {
			$trail[] = $lHtmlEmptyBlock;	
		}
		return $trail;
	}
	
	/**
	 * StepanR:
	 * Get breadcrumb list using static HTML code, no AJAX,
	 * Only last BC_DEFAULT_LEN pages are shown.
	 *
	 * @access private
	 * @return array of HTML lines
	 */
	function getTrailSimple() {
		// Save empty html block for future use.
		$lHtmlEmptyBlock = '<span class="none">&nbsp;</span>';
		
		// extract visible trail
		$trail = array_slice($this->crumbs, 0, BC_DEFAULT_LEN);
		
		// convert page names into html links
		$this->convertNamesToLinks($trail);
		
		// Add empty blocks if trail is less than BC_DEFAULT_LEN
		while (count($trail) < BC_DEFAULT_LEN) {
			$trail[] = $lHtmlEmptyBlock;	
		}
		return $trail;
	}
	
	/**
	 * StepanR:
	 * Converts array of page names to array of web links
	 *
	 * @access private
	 * @param array $trail
	 */
	function convertNamesToLinks(&$trail) {
		global $wgTitle, $wgUser;

		$sk = $wgUser->getSkin();
		foreach ($trail as $key => $value) {
			$index = $key;
			$title = Title::newFromDBkey($value);
			if (!$title) continue;
			$link = '<a href="'.$title->getLocalURL('bc='.$index).'" title="'.$title->getDisplayText().'">'.htmlspecialchars(wfTruncateFilename($title->getPathlessText(), 26)).'</a>';
			if ($this->curindex == $index) {
				// gotta use the span because getInternalLinkAttributesObj() is overriding the class declaration
				$trail[$key] = '<span class="selected">'.$link.'</span>';
			} else {
				$trail[$key] = $link;
			}
		}
	}
}
?>

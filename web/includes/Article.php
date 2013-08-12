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

 // dirty hack, but the title/article mess is still not completely cleaned-up from mediawiki
 // see bug #4520 for more information
 $_PERMISSIONS = array(); 
 
class Article {
	var $mLoaded;
	var $mLoadedPermissions;
	var $mContent;
	protected $User = null;
	var $mUser, $mTimestamp, $mUserText;
	var $mRedirectedFrom;
	var $mTouched, $mTitle;
	var $mForUpdate;
	var $mParents;
	var $mParentID = 0;
	// guerrics: a third way to determine the parent ids
	protected $mParentIds = array();
	// saves the current page's alert status, check with DekiPageAlert class
	protected $PageAlert = null;
	protected $mAlertStatus = null;
	protected $mAlertParentId = -1;
	var $mParentPermissions;
	var $mRedirected;
	var $mSecurity;
	var $mRestrictions;
	var $mRestrictionsLoaded;
	var $mHighlightedText;
	var $mCommentsCount;
	var $mSection;
	var $mPermission;
	var $mPermissionFlags;
	var $mTags;
	var $mFiles;
	var $mMetrics;
	var $mLanguage;
	var $mRevisionCount;
	var $mTargetArticleId; //for other pages that get included, this current article's ID will be passed in to provide context
	var $mContentAlternates; // alternate content types
	var $mContentType;
	var $mUnsafe;
	protected $PageInfo;

	/**
	 * Constructor and clear the article
	 * @param mixed &$title
	 */
	function Article( &$title, $mode = 'view' ) {
		$this->mTitle =& $title;
		$this->clear();

        //RoyK: Need to store if this article is in a redirect=no state
		global $wgRequest;
		$this->mRedirected = $wgRequest->getVal('redirect') == 'no';

		//view mode for API
		$this->mMode = $mode;
	}

	/**
	 * Clear the object
	 */
	function clear() {
		global $wgRequest;

		$this->mCurID = $this->mUser = -1; # Not loaded
		$this->mRedirectedFrom = array();
		$this->User = null;
		$this->mUserText = $this->mTimestamp = '';
		$this->mTouched = '19700101000000';
		$this->mForUpdate = false;
		$this->mParents = array();
        $this->mParentID = 0;
        $this->mParentPermissions = null;
        $this->mRedirected = false;
        $this->mHighlightedText = null;
        $this->mCommentsCount = null;
        $this->mSection = null;
        $this->mMode = 'view';
        $this->mSecurity = null;
        $this->mPermission = null;
        $this->mPermissionFlags = array();
        $this->mLoaded = false;
		$this->mLoadedPermissions = false;
        $this->mTags = null;
        $this->mFiles = null;
		$this->mMetrics = null;
		$this->mRevisionCount = 0;
		$this->mTargetArticleId = 0;
		$this->mContentAlternates = array();
		$this->mUnsafe = null;
		$this->PageInfo = null;
	}
	
	function isViewPage() {
	    global $wgRequest;
		return !DekiNamespace::isTalk($this->getTitle()->getNamespace()) && ($wgRequest->getVal('action') == '' || $wgRequest->getVal('action') == 'view')
	    	&& (!$wgRequest->getVal('revision') && !$wgRequest->getVal('oldid') && !$wgRequest->getVal('diff') && $this->getTitle()->canEditNamespace());
	}
	
	function checkRestricted($status) 
	{
		global $wgUser, $wgOut, $wgPreloadPages;
		//royk, bug #3134: for forbidden pages, see if the user is logged in, and redirect to login page with message
		//royk, bug #4259: also check for status 401 as well as a 403
		if ($status == 403 || $status == 401) 
		{
			if ($wgUser->isAnonymous()) 
			{
				// bugfix 5508: Preloaded pages should not redirect; this causes an endless redirect on login pages
				if (in_array($this->getTitle()->getPrefixedText(), $wgPreloadPages)) 
				{
					return;
				}
				wfMessagePush('general', wfMsg('Article.Common.page-is-restricted-login'));
				$sk = $wgUser->getSkin();
				$wgOut->redirect($sk->makeLoginUrl());
			}
			else 
			{
				wfMessagePush('general', wfMsg('Article.Common.page-is-restricted'));
			}
		}
		else 
		{
			wfMessagePush('general', wfMsg('Article.Error.page-couldnt-be-loaded'));	
		}
	}
	
	/***
	 * 
	 */
	function getParameters($skipignores = false) {
		global $wgRequest;
		
		//extra GET parameters
		$knowns = array(
			//stuff we use on the front-end
			'action', 'title', 'lang', 'revision', 'redirect', 'subpage', 'template', 'diff', 'rdfrom', 'view', 'highlight', 'oldid', 
			
			//stuff used in the API - block these to prevent overloads as a sanity check
			'section', 'format', 'highlight', 'revision', 'mode', 'redirects', 'include'
		);
		
		//we should have a WebRequest method for this, but this'll do for now
		$gets = $_GET;
		
		if (!$skipignores) 
		{
			foreach ($knowns as $key) 
			{
				unset($gets[$key]);	
			}
		}
		return empty($gets) 
			? array()
			: $gets;
	}

	/**
	 * Loads the page's contents from the API
	 *
	 * @param string $mode - determines how the content will be rendered (API parameter)
	 * @param string $section - specifies which section to load
	 * @param string $lang - set the language to load the contents with, used for multi-lang templates
	 */
	function loadContent( $mode = 'view', $section = null, $content = true, $lang = null ) {
		global $wgArticle, $wgRequest;
		global $wgOut, $wgUser;
		global $wgPageExclude;

		// hack hack; todo: clean up when we get the page XML vs. its contents
		if (!is_null($wgRequest->getVal('diff')) && !is_null($wgRequest->getVal('revision')) && $content) 
		{
			return;	
		}
		
		//sometimes lazy loading will attempt to load from a non-editable page
		if (!$this->getTitle()->isEditable()) {
			$this->mLoaded = true;
			$this->mLoadedPermissions = true;
			return;
		}
		
		$User = DekiUser::getCurrent();
		$revision = $this->getRevisionValue();
		$id = $this->getID();
		if ( 0 == $id ) return false;

		$hiddenErrorCodes = array(401, 403);

		# Load the actual page contents
		if ($content && $wgRequest->getVal('action') != 'diff' && $wgRequest->getVal('action') != 'history') 
		{
			$r = DekiPlug::getInstance()->At('pages', $id, 'contents');
			if ($mode == 'include') {
				// template include mode
				$mode = 'edit';
				$r = $r->With('include', 'true');
			}
			$r = $r->With('mode', $mode);
			if (!is_null($revision) && $wgArticle->getId() == $this->getId()) {
				$r = $r->With('revision', $revision);
			}
			if ($mode == 'edit' || $this->mRedirected === true) {
				$r = $r->With('redirects', 0);
			}
			if (!is_null($section)) {
				$r = $r->With('section', $section);
			}
			if (!is_null($lang)) {
				$r = $r->With('lang', $lang);
			}
			$highlight = $this->getHighlightText();
			if (!is_null($highlight) && $User->canHighlightTerms()) {
				$r = $r->With('highlight', $highlight);
				wfMessagePush('general', wfMsg('Article.Common.highlighted-search', htmlspecialchars(urldecode($highlight)),
					'<a href="'. $this->mTitle->getLocalUrl() .'">'. wfMsg('Article.Common.highlighted-search-link') . '</a>'), 'search');
			}
			$parameters = $this->getParameters();
			foreach ($parameters as $key => $val) 
			{
				//bug 5951: Templates cannot be applied to unsaved pages; pageId is also a valid API GET parameter, which conflicts
				if ($key == 'pageId' && $val == 0) 
				{
					continue;
				}
				$r = $r->With($key, $val);	
			}
			if ($this->mTargetArticleId > 0) {
				$r = $r->With('pageid', $this->mTargetArticleId);
			}
			$Result = $r->Get();
			$result = $Result->toArray();
	
			//sometimes the data comes back in a mangled format; throw an error
			if ($result['status'] == 203) {
				wfMessagePush('general', wfMsg('Article.Error.contains-invalid-markup'), 'error');
			}
	
			if (!MTMessage::HandleAPIResponse($result, $hiddenErrorCodes, true)) 
			{
				$this->mLoaded = true;
				$this->mLoadedPermissions = true;
				$this->checkRestricted($result['status']);
				return false;
			}
			
			$content = wfArrayVal($result, 'body/content');
			
			if ($mode == 'edit') 
			{
				$this->mUnsafe = wfArrayVal($content, '@unsafe') == 'true' ? true: false; 
			}
						
			$this->mContentType = $content['@type'];
			
			if (is_string($content['body'])) 
			{
				$this->mContent = $content['body'];	
			}
			else 
			{
				foreach ($content['body'] as $val) 
				{
					if (!is_array($val)) 
					{ 
						//set primary page body
						$this->mContent = $val;
						continue; 
					}
					$bodies[wfArrayVal($val, '@target')] = wfArrayVal($val['#text']);
				}
				
				global $wgTargetSkinVars;
				foreach ($bodies as $key => $val) 
				{
					if (in_array($key, $wgTargetSkinVars)) 
					{
						$wgOut->setTarget($key, $val);
					}
				}
			}
			
			/*
			 * Don't include head/tail content when on the root dashboard page (/User:Admin), bug #8183
			 */
			global $wgTitle;
			$includeHeadTail = true;
			if (NS_USER == $wgTitle->getNamespace() && (strlen($wgTitle->getPartialURL()) > 0)
				&& (strpos($wgTitle->getPrefixedURL(), '/') === false)
				&& defined('USER_DASHBOARD'))
			{
				$includeHeadTail = false;
			}
			
			$head = wfArrayVal($result, 'body/content/head');
			if (!is_null($head) && $includeHeadTail)
			{
				// parse out the meta tags
				$metaTags = array();
				$head = wfParseMetaTags($head, $metaTags);
				
				// set the meta tags
				foreach ($metaTags as $metaTag)
				{
					$parts = wfParseMetaTag($metaTag);
					switch ($parts['name'])
					{
						case 'robots':
							$wgOut->setRobotpolicy($parts['content']);
							break;
						case 'keywords':
							$wgOut->addKeyword($parts['content']);
							break;
						default:
							$wgOut->addMeta($parts['name'], $parts['content'], $parts['http'] == true);
					}
				}
				
				$wgOut->addHeadHTML($head);
			}
			
			$tail = wfArrayVal($result, 'body/content/tail');
			if (!is_null($tail) && $includeHeadTail)
			{
				$wgOut->addTailHTML($tail);
			}
		}

		# Load page information
		$r = DekiPlug::getInstance()->At('pages', $id);
		if (!is_null($revision) && $wgArticle->getId() == $this->getId()) {
			$r = $r->At('revisions')->With('revision', $revision);
		}
		if ($this->mRedirected === true) {
			$r = $r->With('redirects', 0);
		}
		if($wgPageExclude) {
			$r = $r->With('exclude', $wgPageExclude);
		}
		$Result = $r->Get();
		$result = $Result->toArray();

		if (!MTMessage::HandleFromDream($result, $hiddenErrorCodes) || is_null(wfArrayVal($result, 'body/page'))) {
			$this->mLoaded = true;
			$this->mLoadedPermissions = true;
			$this->checkRestricted($result['status']);
			return false;
		}
		
		$page = wfArrayVal($result, 'body/page');
		$this->mRevisionCount = wfArrayVal($page, 'revisions/@count');
		$this->mPermission = wfArrayVal($page, 'security', array());
		global $_PERMISSIONS; 
		$_PERMISSIONS[$id] = $this->mPermission; 
		$this->mPermissionFlags = explode(',', wfArrayVal($page, 'security/permissions.effective/operations/#text', ''));
		$this->User = DekiUser::newFromArray($page['user.author']);
		$this->mUser = $page['user.author']['@id'];
		$this->mUserText = $page['user.author']['nick'];
		$this->mTimestamp = wfTimestamp( TS_MW, $page['date.edited']);
		if (isset($page['date.modified'])) {
			$this->mTouched = wfTimestamp( TS_MW, $page['date.modified']);
		}
		$contentalternates = wfArrayValAll($page, 'contents.alt', array());
		foreach ($contentalternates as $val) 
		{
			$this->mContentAlternates[$val['@type']] = $val['@href'];
		}
		
		// load all of the parent ids
		$this->setParents(wfArrayVal($page, 'page.parent'));
		// parents are read again below
		// get the page alert status if any
		$articleId = ($this->mTitle) ? $this->mTitle->getArticleID() : 0;
		$this->PageAlert = new DekiPageAlert($articleId, $this->mParentIds);

		$this->mCommentsCount = wfArrayVal($page, 'comments/@count');
		$this->mTags = wfArrayValAll($page, 'tags/tag', array());
		
		$files = wfArrayValAll($page, 'files/file', array());
		$this->setFiles($files);
		
		// load some page metrics
		$this->mMetrics = array(
			'charcount' => wfArrayVal($page, 'metrics/metric.charcount', 0),
			'views' => wfArrayVal($page, 'metrics/metric.views', 0)
		);
		$this->mMetrics['revisions'] = wfArrayVal($page, 'revisions/@count', 0);
		
		// only update skinning variables for the global article
		if (!is_null($wgArticle) && ($this->getId() == $wgArticle->getId()))
		{
			// @note guerrics: why is this in here!
			$wgOut->setTagCount(!is_null($this->mTags) ? count($this->mTags) : 0);
			$wgOut->setBackLinks(wfArrayValAll($page, 'inbound/page'));
			$wgOut->setPageMetrics($this->mMetrics); // make variable available for skinning
		}

		// If this page has been redirected from somewhere else, update the title accordingly
		$redirects = wfArrayVal($page, 'page.redirectedfrom');
		if (!is_null($redirects) && $redirects != '')
		{
			$rt = Title::newFromText(wfArrayVal($page, 'page.redirectedfrom/page/path'));
			if ($rt->getArticleId() == $id) {
				$this->mRedirectedFrom[] = wfArrayVal($page, 'page.redirectedfrom/page/path');
			}
		}
		//Reload internal title, for it may be different since API automagically returns content
		$this->mTitle = Title::newFromText($page['path']);
		$this->mTitleText = wfArrayVal($page, 'title');
		$this->mLanguage = wfArrayVal($page, 'properties/language/#text');
		if (empty($this->mLanguage))
		{ // need to force null for later checks
			$this->mLanguage = null;
		}
		$this->mParents = $this->getParentPath(wfArrayVal($page, 'page.parent'));
		$this->mLoaded = true;
		$this->mLoadedPermissions = true;
		
		$this->PageInfo = DekiPageInfo::newFromArray($page);
		return true;
	}
	
	function getInfo()
	{
		return $this->PageInfo;
	}
	function getAlternateContent($mimetype, $return = null) 
	{
		if (!isset($this->mContentAlternates[$mimetype])) 
		{
			return $return;
		}
		return $this->mContentAlternates[$mimetype];	
	}
	function getParentPermissions() 
	{
		if (!is_null($this->mParentPermissions)) {
			return $this->mParentPermissions;
		}
		$pt = $this->getTitle()->getParent();
		if (is_null($pt)) {
			$pt = Title::newFromText('');
		}
		
		// if we're creating a sub-sub-sub child which doesn't exist, it needs to recursively go up the tree for its parents
		while (true) 
		{
			$pa = new Article($pt);
			if ($pa->getId() == 0) 
			{
				$pt = $pt->getParent();
				if (is_null($pt)) {
					$pt = Title::newFromText('');
				}
				continue;
			}
			$perms = $pa->getPermissions();
			$this->mParentPermissions = $perms;
			return $this->mParentPermissions;
		}
	}

	/**
	 * Instance method
	 */
	protected function setParents(&$parents)
	{
		// set the default values
		$this->mParentID = 0;
		$this->mParentIds = array();
		
		while (is_array($parents))
		{
			$id = isset($parents['@id']) ? $parents['@id'] : 0;

			if ($id > 0)
			{
				if ($this->mParentID == 0)
				{
					// set the primary parent id
					$this->mParentID = $id;
				}
			
				// add the parent id to the list
				$this->mParentIds[] = $id;
			}

			if (isset($parents['page.parent']))
			{
				$parents = &$parents['page.parent'];
			}
			else
			{
				$parents = null;
			}
		}
	}
	
	/**
	 * Could be a static method
	 */
	function getParentPath($parents) {
		$path = array();
		if (is_null($parents)) 
		{
			return $path;	
		}
		while (!is_null($parents)) 
		{
			$nt = Title::newFromText(isset($parents['path']) ? $parents['path']: '');
			$paths[] = '<a href="'.$nt->getFullUrl().'" class="deki-ns'.strtolower(DekiNamespace::getCanonicalName($nt->getNamespace())).'">'
				.htmlspecialchars($parents['title'])
				.'</a>';
			$parents = isset($parents['page.parent']) ? $parents['page.parent']: null;
		}
		
		//comes back in reverse order
		$paths = array_reverse($paths);
		return $paths;
	}
	function userCanWatch() {
		if ($this->getId() == 0) 
		{
			return false;
		}
		return $this->getTitle()->isEditable();
	}
	
	function userCanCreate()
	{
		$namespace = $this->getTitle()->getNamespace();

		//special case for the Template: page
		if ($namespace == NS_TEMPLATE && $this->getTitle()->getText() == '') 
		{
			global $wgUser;
			return $wgUser->canCreate();
		}
		
		// special case for Special: pages
		// @TODO guerrics: remove this hard coded check and check against the API
		if ($namespace == NS_SPECIAL)
		{
			global $wgUser;
			return $wgUser->isAdmin();
		}
		
		// is this title editable?
		if (!$this->getTitle()->isEditable()) 
		{
			return false;
		}
		
		// if the page doesn't exist, see if it's a creatable page
		if ($this->getId() == 0) 
		{
			return in_array('CREATE', $this->getParentPermissions());
		}

		// can't create a subpage to a redirect page...
		if ($this->getTitle()->isRedirect()) 
		{
			return false;
		}
		return $this->userCan('CREATE');
	}

	function userCanRestrict() {
	    if (!$this->getTitle()->isEditable()
	    	|| !$this->userCan('UPDATE')
	    	|| !$this->userCan('CHANGEPERMISSIONS')) 
	    {
	       return false;
       	}
		return true;
	}

	function userCanTalk() {
	    if (!$this->getTitle()->isEditable()
	    	|| !$this->userCanRead()) 
	    {
		    return false;
	    }
	    global $wgTitle;
		$ntt = Title::newFromText($wgTitle->getText(), DekiNamespace::getTalk($wgTitle->getNamespace()));
		if (strcmp($wgTitle->getPrefixedText(), $ntt->getPrefixedText()) == 0) 
		{
			return false;
		}
	    return true;
	}
	function userCanEdit() {

		// is this title editable?
		if (!$this->getTitle()->isEditable()) 
		{
			return false;
		}
		
		if ($this->getId() == 0) 
		{
			return $this->userCanCreate();
		}
		
		return $this->userCan('UPDATE');
	}

	function userCanComment() {
		global $wgUser;
		if ($this->getId() == 0) 
		{
			return false;
		}
		if (!$this->getTitle()->isEditable()) 
		{
			return false;
		}
		return $this->userCanRead() && !$wgUser->isAnonymous();
	}

	function userCanRead() {
		if ($this->getTitle()->isSpecialPage()) 
		{
			return true;
		}
		return $this->userCan('READ');
	}

	function userCanEmailPage() {
		global $wgAllowAnonymousEmailing, $wgUser;
		if ($this->getId() == 0) 
		{
			return false;
		}
		if (!$this->getTitle()->isEditable()) 
		{
			return false;
		}
		return $wgAllowAnonymousEmailing || !$wgUser->isAnonymous();
	}

	function userCanTag() {
		if ($this->getId() == 0) 
		{
			return false;
		}
		return $this->userCanEdit();
	}
	function userCanSetOptions() {
		if ($this->getId() == 0) 
		{
			return false;
		}
		return $this->userCanEdit();
	}
	function userCanDelete() {
		if ($this->getTitle()->getPrefixedText() == wfHomePageInternalTitle()) 
		{
			return false;
		}
		if (DekiNamespace::isTalk($this->getTitle()->getNamespace()))
		{
			return false; 
		}
		if (!$this->getTitle()->isEditable()) 
		{
			return false;
		}
		if (!$this->userCanEdit() || !$this->userCan('DELETE')) 
		{
	       return false;
       	}
		return true;
	}

	function userCanAttach() {

		if (!$this->getTitle()->isEditable()) 
		{
			return false;
		}
		return $this->userCan('UPDATE');
	}
	
	function userCanScript() 
	{
		global $wgUser;
		return $this->userCan('UNSAFECONTENT') || $wgUser->isAdmin();
	}

	function userCanMove() {
		if ($this->getTitle()->getPrefixedText() == wfHomePageInternalTitle()
			 || ($this->getTitle()->getNamespace() == NS_USER && !$this->getTitle()->getParent())
			 || ($this->getTitle()->getNamespace() == NS_TEMPLATE && $this->getTitle()->getText() == '')
			 || !$this->getTitle()->isEditable()) {
			return false;
		}
		if (DekiNamespace::isTalk($this->getTitle()->getNamespace()))
		{
			return false; 
		}
		if (!$this->userCan('UPDATE')) {
	       return false;
       	}
       	return true;
    }

	function userCanUndelete() {
		global $wgUser;
		return $wgUser->isAdmin();
	}

	/**
	 * Can a user undertake a certain action?
	 *
	 * @param $action - checks if current user is allowed to undertake a certain option on this page
	 */
	function userCan($action) {

		$action = strtoupper($action);
		//users can't do anything but view a special/admin page
		if ($action != 'READ') {
			if ( !$this->getTitle()->canEditNamespace() ) {
				return false;
			}
		}
		//otherwise, see what's in the perm flag list
		return in_array(strtoupper($action), $this->getPermissions());
	}

	function isRestricted() {
		if (empty($this->mPermission)) {
			return false;
		}
		return strtoupper(wfArrayVal($this->mPermission, 'permissions.page/restriction/#text', 'PUBLIC')) != 'PUBLIC';
	}
	/**
	 * MT ursm
	 *
	 * Get a list of parent nodes by looking at the title and splitting out
	 * each element separated by /.
	 *
	 * @return array of the parent title names (including myself).
	 */
	function getParents() {
	    if (!$this->mTitle) return null;
	    return Article::getParentsFromName($this->mTitle->getPrefixedText());
	}

	/**
	 * @todo REQUIRES REVIEW
	 */
	function getParentsFromName($prefixedName) {
	    $prefixedName = str_replace(HPS_SEPARATOR.HPS_SEPARATOR, '%'.dechex(ord(HPS_SEPARATOR)),$prefixedName);
 		$parents = array();
 		foreach (explode(HPS_SEPARATOR, $prefixedName) as $parent) {
 		    $parents[] = str_replace('%'.dechex(ord(HPS_SEPARATOR)),HPS_SEPARATOR.HPS_SEPARATOR,$parent);
 		}
 		return $parents;
	}

	/**
	 * @todo REQUIRES REVIEW
	 */
	function areChildren(&$nt) {
	    return Article::childrenCount($nt) > 0;
	}

	/***
	 * Returns the number of children for a given title object
	 */
	function childrenCount(&$nt) {
		global $wgDekiPlug;
		if ($nt->getArticleId() == 0) 
		{
			return 0;
		}
		$body = $wgDekiPlug->At('pages', $nt->getArticleId(), 'subpages')->Get();
		$subpages = wfArrayValAll($body, 'body/subpages/page.subpage', array());
		return count($subpages);
	}

	/**
	 * Return a list of children of the current page for use in the popup dialogs
	 * @param bool $includePrivate - if true, private pages will be included in the list
	 */
	function getChildren($includePrivate = false)
	{
		if ($this->getId() == 0)
		{
			return array();
		}
		
		$Plug = DekiPlug::getInstance()->At('pages', $this->getId(), 'subpages');
		if ($includePrivate)
		{
			$Plug = $Plug->WithApiKey();
		}
		
		$Result = $Plug->Get();
		if (!$Result->handleResponse())
		{
			return array();
		}
		
		$pages = $Result->getAll('body/subpages/page.subpage');
		if (is_null($pages))
		{
			return array();
		}
		
		$return = array();
		$prefix = $this->mTitle->getText();
		if ($prefix != '')
		{
			$prefix .= HPS_SEPARATOR;
		}
		
		foreach ($pages as $page)
		{
			$nt = Title::newFromText($page['title']);
			$pt = Title::newFromText($page['path']);
			$return[$prefix.$page['title'].'|'.$this->mTitle->getNamespace().'|'.$page['@id'].'|'.$pt->getPathlessText()] = $page['title'];
		}
		ksort($return);
		
		return $return;
	}
	

	static function getAllChildren($articleId = null, &$Title = null)
	{
		global $wgDekiPlug;

		$at = is_object($Title) ? '='. urlencode(urlencode($Title->getPrefixedText())) : $articleId;
		
		$result = $wgDekiPlug->At('pages', $at, 'subpages')->Get();
		$pages = wfArrayValAll($result, 'body/subpages/page.subpage');
		return is_null($pages) ? array() : $pages;
	}
	
	/**
	 * Create new article from id (default global article object)
	 * @param int $articleId - article to load
	 * @return Article
	 */
	static function newFromId($articleId)
	{
		$Article = null;
		if (!is_null($articleId))
		{
			$Title = Title::newFromID($articleId);
			$Article = new Article($Title);
		}

		return $Article;
	}

	/**
	 * MT ursm
	 *
	 * Get parent title
	 *
	 */
	function getParentTitle() {
	    return Article::getParentTitleFromTitle($this->mTitle);
	}
	
	public function getParentIds($fromRoot = false)
	{
		// must be loaded to retrieve
		if (!$this->mLoaded)
		{
			$this->loadContent('view', null, false);
		}
		// array is store with nearest parent first
		return ($fromRoot) ? array_reverse($this->mParentIds) : $this->mParentIds;
	}

	/**
	 * Static
	 */
	function getParentTitleFromTitle(&$nt) {
	    if (!$nt) return null;
	    return Article::getParentTitleFromText($nt->getPrefixedText());
	}

	/**
	 * Static
	 */
	 function getParentTitleFromText($titleName) {
	    $parents = Article::getParentsFromName($titleName);
	    if (count($parents) <= 1)
	       return null;
	    array_pop($parents);
	    return Title::newFromText(implode(HPS_SEPARATOR, $parents));
	}

	/**
	 * Static
	 */
	function splitName($titleFull, &$titlePath, &$titleName) {
		$titleName = Article::getShortName($titleFull);
		$titlePath = Article::getParentTitleFromText($titleFull);
		if ($titlePath)
            $titlePath = $titlePath->getPrefixedText();
	}

	/**
	 * Static
	 */
	 function combineName($path, $title) {
	    $path = trim($path," ".HPS_SEPARATOR);
        $title = trim($title," ".HPS_SEPARATOR);
        return $path == '' ? $title : $path . HPS_SEPARATOR . $title;
	}

	/**
	 * Static
	 */
 	function getShortName($fullName) {
 	    $p = Article::getParentsFromName($fullName);
 	    return $p[count($p)-1];
 	}

	/**
	 * @todo REQUIRES REVIEW
	 * Return the Article ID
	 */
	function getID() {
		if( $this->mTitle ) {
			return $this->mTitle->getArticleID();
		} else {
			return 0;
		}
	}

	/**
	 * Returns the current title object
	 * @return Title
	 */
	function getTitle() {
		return $this->mTitle;
	}
	
	/**
	 * @return int - status determining how the article is subscribed for alerts
	 * @see DekiPageAlert class for determining what the integer value means
	 */
	public function getAlertStatus()
	{
		$this->loadPageAlerts();
		return $this->mAlertStatus;
	}
	/**
	 * @return int - if status == DekiPageAlert::STATUS_PARENT then this will retrieve
	 * the closest subscribing parent id
	 */
	public function getAlertParentId()
	{
		$this->loadPageAlerts();
		return $this->mAlertParentId;
	}
	
	protected function loadPageAlerts()
	{
		if (is_null($this->mAlertStatus) && is_object($this->PageAlert) && ($this->getId() > 0))
		{
			$this->mAlertStatus = $this->PageAlert->getStatus();
			$this->mAlertParentId = $this->PageAlert->getSubscriberId();
		}
	}
	
	/**
	 * Get the page's contents
	 */
	function getContent() {
		//lazy loading
		if (!$this->mLoaded) {
			$this->loadContent();
		}
		global $wgRequest, $wgUser, $wgOut;
 		$action = $wgRequest->getText( 'action', 'view' );
		if ($this->getId() == 0) 
		{
			// editor will load here, so no need to continue
			if ( 'edit' == $action )
			{
				return '';
			}
			
			// MT-10036: if you're not logged-in, and the page doesn't exist, redirect to the homepage
			if ($wgUser->isAnonymous())
			{

                // if cannot view, you'll be redirect to login page anyway
                if ($wgUser->canView())
                {
				    $loginUrl = $wgUser->getSkin()->makeLoginUrl($this->getTitle());
				    DekiMessage::error(wfMsg('Article.Common.page-is-restricted-login-link', $loginUrl));
                }

				$wgOut->redirectHome();
				return;	
			}
			return '<div class="nocontent">'.wfMsg('Article.Common.no-text-in-page').'</div>';
		}
		return $this->mContent;
	}

	function getDisplayTitle() 
	{
		//lazy loading
		if (!$this->mLoaded) 
		{
			$this->loadContent('view', null, false);
		}
		return $this->mTitleText;	
	}
	
	/**
	 * Returns the revision value that is being passed in through GET to pass to the Deki API
	 */
	function getRevisionValue() {
		global $wgRequest;
		if ($wgRequest->getInt('revision') !== null && $wgRequest->getVal('revision') > 0) {
			return $wgRequest->getInt('revision');
		}
		return null;
	}

	/**
	 * Returns revision count
	 * @return int - revision count
	 */
	public function getRevisionCount()
	{
		if (!$this->mLoaded)
		{
			$this->loadContent();
		}
		return $this->mRevisionCount;
	}

	/**
	 * Tests if the article text represents a redirect
	 */
	function isRedirect( ) {
		return is_array($this->mRedirectedFrom) && count($this->mRedirectedFrom) > 0;
	}

	function setTimestamp($timestamp) {
		$this->mTimestamp = $timestamp;
	}
	function getSection() {
		if (!$this->mLoaded) {
			$this->loadContent();
		}
		return $this->mSection;
	}
	function setSection($section) {
		$this->mSection = $section;
	}
	function getTags() {
		if (is_null($this->mTags)) {
			global $wgDekiPlug;
			$this->mTags = wfArrayValAll($wgDekiPlug->At('pages', $this->getId(), 'tags')->Get(), 'body/tags/tag', array());
		}
		return $this->mTags;
	}
	
	// todo: royk; gotta optimize this call by calling it - properties are not returned with page
	function getFiles() {
		if (is_null($this->mFiles)) {
			global $wgDekiPlug;
			$data = $wgDekiPlug->At('pages', $this->getId(), 'files')->Get();
			$files = wfArrayValAll($data, 'body/files/file', array());
			$this->setFiles($files);
		}

		return $this->mFiles;
	}
	
	/**
	 * Sets and sorts the files for this page
	 * @param array &$files - array of file results from the api
	 */
	protected function setFiles(&$files)
	{
		uasort($files, array($this, 'sortFilesByName'));
		$this->mFiles = $files;
	}
	// sorting helper
	protected function sortFilesByName($fileA, $fileB)
	{
		return strcasecmp($fileA['filename'], $fileB['filename']);
	}
	
	function getTimestamp() {
		if (!$this->mLoaded) {
			$this->loadContent();
		}
		return $this->mTimestamp;
	}

	public function getDekiUser()
	{
		if (!$this->mLoaded)
		{
			$this->loadContent();
		}
		// @TODO guerrics: in what cases is User null?
		return is_null($this->User) ? DekiUser::getAnonymous() : $this->User;
	}

	function getUser() {
		if (!$this->mLoaded) {
			$this->loadContent();
		}
		return $this->mUser;
	}

	function getUserText() {
		if (!$this->mLoaded) {
			$this->loadContent();
		}
		return $this->mUserText;
	}

	function getHighlightText() {
		return $this->mHighlightedText;
	}

	function getSecurity() {
		if (is_null($this->mSecurity)) 
		{
			global $wgDekiPlug;
			$this->mSecurity = $wgDekiPlug->At('pages', $this->getId(), 'security')->With('redirects', 0)->Get();
		}
		return $this->mSecurity;
	}
	
	function getPermissions() {
		global $_PERMISSIONS; 
		if ( !$this->mLoadedPermissions && !is_null($this->getTitle()) && $this->getTitle()->isEditable() && $this->getId() > 0 ) {
			
			// $_PERMISSIONS is a global array of permissions per id
			if (!array_key_exists($this->getId(), $_PERMISSIONS)) 
			{
				global $wgDekiPlug, $wgRequest;
				$r = $wgDekiPlug->At('pages', $this->getId(), 'security');
				if ($wgRequest->getVal('redirect') == 'no') 
				{
					$r = $r->With('redirects', 0);	
				}
				$r = $r->Get();
				if ($r['status'] == 200) {
					$this->mPermissionFlags = explode(',', wfArrayVal($r, 'body/security/permissions.effective/operations/#text', ''));
					$this->mLoadedPermissions = true;
				}
			}
			else
			{
				$this->mPermissionFlags = explode(',', wfArrayVal($_PERMISSIONS[$this->getId()], 'permissions.effective/operations/#text', ''));
				$this->mLoadedPermissions = true;
			}
		}
		return $this->mPermissionFlags;
	}
	
	/**
	 * Determines the language for the current article
	 * @note for new pages this will traverse the parents to determine the page language
	 * 
	 * @return string
	 */
	function getLanguage()
	{
		if (!$this->mLoaded)
		{
			$this->loadContent('view', null, false);
		}

		if (is_null($this->mLanguage))
		{
			// attempt to determine the page language from parent
			$parents = self::getParentsFromName($this->mTitle->getPrefixedText());

			// remove self from the parent list
			array_pop($parents);

			// walk the parents
			while (!empty($parents))
			{
				$parentPath = implode('/', $parents);
				
				// attempt to load the parent page information
				$Parent = DekiPageInfo::loadFromPath($parentPath);

				if (!is_null($Parent))
				{
					$Parent = self::newFromId($Parent->id);
					
					// retrieve the language from the parent page
					$this->mLanguage = $Parent->getLanguage();
					break;
				}
				
				// remove the parent that was just checked
				array_pop($parents);
			}
		}

		return $this->mLanguage;
	}
	
	function getContentType() {
		if (!$this->mLoaded) {
			$this->loadContent('edit');
		}
		return $this->mContentType;
	}

	/**
	 * Automagically redirects to a new page based on localization key (example: "Page Title $n").
	 * (takes into account redirected pages and existing pages)
	 */
	function addsubpage() {
	    global $wgOut, $wgRequest;
	
		$nt = Article::getNextSubpageTitle($this->mTitle, wfMsg('Article.Common.new-page-title', ''));

		$location = $nt->getFullURL('action=edit', true);
		if ($wgRequest->getVal('template')) 
		{
			$location .= '&template='.urlencode($wgRequest->getVal('template'));	
		}
		$params = Article::getParameters();
		foreach ($params as $key => $val) 
		{
			$location .= '&'.urlencode($key).'='.urlencode($val);
		}

        $wgOut->redirect( $location );
	}

	/**
	 * @param Title &$ParentTitle title object of the parent page to create a subpage for
	 * @param string $subpagename the name of the subpage to check availability for
	 *
	 * @return Title reference to an object representing the subpage path
	 */
	static function &getNextSubpageTitle(&$ParentTitle, $subpageName)
	{
	    global $wgOut;
	
		// find an available title
		$index = 0;
	    while (true) {
		    $title = $subpageName . ($index > 1 ? ' ' . $index : '');
	
			// prepend path if this is a subpage
		    if ($ParentTitle->getDBkey() != '' && $ParentTitle->getText() != wfHomePageInternalTitle())
			{
				$title = $ParentTitle->getText() . HPS_SEPARATOR . $title;
	    	}
	
			// MT-9716: ensure no page / redirect exists
			$nt = Title::makeTitle($ParentTitle->getNamespace(), $title);
			if ($nt->getArticleID() == 0)
			{
				break;
			}
			
	        ++$index;
	    }

		return $nt;
	}

	/**
	 * View this article - diff and revision views are also handled here
	 */
	function view()	{
		global $wgUser, $wgOut, $wgRequest;
		$sk = $wgUser->getSkin();

		//Template: and User: are special cases on view
		if (in_array($this->mTitle->getNamespace(), array(NS_TEMPLATE, NS_USER)) && $this->mTitle->getText() == '') 
		{
			//READ required for Template: and User:
			if (!$wgUser->canView()) 
			{
				$wgOut->accessDenied();
				return;
			}

			// guerric: moved setting page title here to avoid overriding plugin titles
			$wgOut->setPageTitle(Skin::pageDisplayTitle());

			switch ($this->mTitle->getNamespace())
			{
				case NS_TEMPLATE: 
					global $wgTitle;
					$page = substr(Hooks::SPECIAL_LIST_TEMPLATES, strlen(Hooks::SPECIAL_PAGE));
					$wgTitle = Title::makeTitle(NS_SPECIAL, $page);

					if (DekiPlugin::HANDLED == DekiPlugin::executeHook(Hooks::SPECIAL_PAGE, array($page)))
					{
						return;
					}
					
					// for skin rendering (create page button, etc.), use the root Template: page 
					$wgTitle = Title::makeTitle(NS_TEMPLATE, '');

					break;

				case NS_USER:
					global $wgTitle;
					$page = substr(Hooks::SPECIAL_LIST_USERS, strlen(Hooks::SPECIAL_PAGE));
					$wgTitle = Title::makeTitle(NS_SPECIAL, $page);

					if (DekiPlugin::HANDLED == DekiPlugin::executeHook(Hooks::SPECIAL_PAGE, array($page)))
					{
						return;
					}
					break;	
			}
			
			return;
		}
		
		// Get variables from query string
		$diff = $wgRequest->getVal( 'diff' ); //diff against
		$revision = $this->getRevisionValue(); //revision
		$rdfrom = $wgRequest->getVal( 'rdfrom' ); //redirected from

		$wgOut->setArticleFlag( true );
		$wgOut->setRobotpolicy( 'index,follow' );

		//if we're doing a diff
		if ( !is_null( $diff ) ) {
			require_once( 'DifferenceEngine.php' );
			$wgOut->setPageTitle( wfEncodeTitle($this->mTitle->getPathlessText()) );
			$de = new DifferenceEngine( $revision, $diff );
			$de->showDiffPage();
			return;
		}
		
		//for viewing an old version of a page
		if ( !is_null( $revision ) ) {
			$this->setOldSubtitle( $revision );
			$wgOut->setRobotpolicy( 'noindex,follow' );
		}

		//from search terms
		$this->setHighlightPhrase();
		if($wgRequest->getVal('action') == 'edit')
		{
			$this->loadContent('edit', null, false);
		}
		else
		{
			$this->loadContent();
		}
		
		//once content comes in, if the language is different, reset language UI
		$pageLanguage = $this->getLanguage();
		if (!is_null($pageLanguage))
		{
			global $wgLanguageCode;
			$wgLanguageCode = $pageLanguage;
		}

		if ( count($this->mRedirectedFrom) > 0 ) {
			$this->setRedirectMessage();
		}

		$text = $this->getContent( false ); # May change mTitle by following a redirect
		
		// need to get the current display title
		$DekiPlug = DekiPlug::getInstance();
		if ($wgRequest->getVal('redirect') == 'no') {
			$DekiPlug = $DekiPlug->With('redirects', 0);	
		}
		$Result = $DekiPlug->At('pages', $this->mTitle->getArticleId(), 'info')->Get();
		$wgOut->setPageTitle( $Result->getVal('body/page/title'), true );

		//show a redirection message
		if ( !empty( $rdfrom ) ) {
			$sk = $wgUser->getSkin();
			$redir = $sk->makeExternalLink( $rdfrom, $rdfrom );
			$s = '<span class="redirectedFrom">'. wfMsg('Article.Common.redirected-from', $redir) .'</span>';
			$wgOut->setRedirectMessage( wfMsg('Article.Common.redirected-from', strip_tags($rdfrom)) );
		}

		if ( $rt = Title::newFromRedirect( $text ) ) {
			$wgOut->addHTML( '<div class="redirectedTo"><span>'. wfMsg('Article.Common.page-content-located-at', $sk->makeLinkObj( $rt )) .'</span></div>' );
		}
		else {
			$wgOut->addHTML( $text );
		}

        $action = is_null($wgRequest) ? 'view' : $wgRequest->getVal( 'action', 'view' );

		//if we're on a valid editable page
        if (empty($revision) && $action != 'export') {
    		if ($this->getID() > 0)
    		{
    			$this->renderAdditionalComponents();
     		}
        }
	}
	
	/**
	 * Output additional page components such as comments, files, tags, etc.
	 * @return N/A
	 */
	function renderAdditionalComponents()
	{
		global $wgOut;
		
		// bug #8249: ensure additional content available when rendering dashboard pages 
		if (!$this->mLoaded)
		{
			// don't need core page content, just supplmental (comments, etc.)
			$this->loadContent('view', null, false);
		}

    	// make a copy of the article title so the plugins can't mess it up
    	$pageId = $this->getID();
    	$ArticleTitle = clone $this->getTitle();
 
    	// render page comments
    	$pluginHtml = '';
    	$ret = DekiPlugin::executeHook(Hooks::PAGE_RENDER_COMMENTS, array($ArticleTitle, &$pluginHtml));
    	if ($ret != DekiPlugin::HANDLED_HALT)
    	{
    		$comments = new CommentPage($this->getTitle());
			$pluginHtml = $comments->format($this->mCommentsCount == 0 ? 0: null);
    	}
    	$wgOut->setCommentsHTML($pluginHtml);
    	
    	// generate an array of file objects for the hooks
    	$pageFiles = $this->getFiles();
    	$files = array();
    	foreach ($pageFiles as &$result)
    	{
    		$File = DekiFile::newFromPagesArray($result, $ArticleTitle);
    		$files[] = $File;
    	}
    	unset($result);
    	
    	// render page files
    	$pluginHtml = '';
    	$fileCount = 0;
     	$ret = DekiPlugin::executeHook(Hooks::PAGE_RENDER_FILES, array($ArticleTitle, &$pluginHtml, $pageId, &$files, &$fileCount));
    	$wgOut->addFilesHTML($pluginHtml);
    	$wgOut->setFileCount($fileCount);
    	
     	// render page images
    	$pluginHtml = '';
    	$imageCount = 0;
     	$ret = DekiPlugin::executeHook(Hooks::PAGE_RENDER_IMAGES, array($ArticleTitle, &$pluginHtml, &$files, &$imageCount));
    	$wgOut->addGalleryHTML($pluginHtml);
    	$wgOut->setImageCount($imageCount);
    	
     	// render page tags
    	$pluginHtml = '';
     	$ret = DekiPlugin::executeHook(Hooks::PAGE_RENDER_TAGS, array(clone $ArticleTitle, &$pluginHtml));
		$wgOut->addTagsHTML($pluginHtml);
		
		// backlinks
		$PageChangesTitle = Title::makeTitle( NS_SPECIAL, 'Article' );
		$wgOut->addLink(array(
			'rel' => 'alternate',
			'type' => 'application/rss+xml',
			'title' => wfMsg('Page.ListRss.page-changes-feed'),
			'href' => $PageChangesTitle->getLocalURL('type=feed&pageid=' . $this->getID())
		));
		$wgOut->addLink(array(
			'rel' => 'alternate',
			'type' => 'application/rss+xml',
			'title' => wfMsg('Page.ListRss.page-subpage-changes-feed'),
			'href' => $PageChangesTitle->getLocalURL('type=feed&feedtype=subpagechanges&pageid=' . $this->getID())
		));
	}

	/**
	 * Returns the phrase to be highlighted in the content
	 */
	function setHighlightPhrase() {
		global $wgRequest;
		$q = null;

		//automagic detection from search engine
		if (isset($_SERVER['HTTP_REFERER']) && $_SERVER['HTTP_REFERER'] != '') {
			$url = wfParseUrl($_SERVER['HTTP_REFERER']);
			if (strpos($url['host'], 'google.com') !== false) {
				$q = $url['query']['q'];
			}

			if (strpos($url['host'], 'search.yahoo.com') !== false) {
				$q = $url['query']['p'];
			}
		}

		//from internal searching
		if ($wgRequest->getVal('highlight')) {
			$q = $wgRequest->getVal('highlight');
		}
		$this->mHighlightedText = !empty($q) ? $q : null;
	}

	/**
	 * Saves the contents of a page
	 *
	 * @param string $text - the page's contents
	 * @param string $section - the section to return to
	 * @param string $editsummary - edit summary for the commit
	 * @return boolean
	 */
	function save($text, $section = null, $editsummary = null)
	{
		global $wgOut, $wgUser, $wgRequest;
		
		// get a plug instance
		$Plug = DekiPlug::getInstance();

		$isNew = $this->getId() == 0;
		if ($isNew)
		{
			$this->mTitle = Title::newFromText(Title::normalizeParentName($this->mTitle->getPrefixedText()));
			$Plug = $Plug->At('pages', '='.$this->mTitle->getPrefixedDBkey(), 'contents')->With('abort', 'exists');
		}
		else
		{
			$Plug = $Plug->At('pages', $this->getID(), 'contents');
		}

		if ($wgRequest->getVal('redirect') == 'no' || $isNew) {
			$Plug = $Plug->With('redirects', 0);
		}
		if ($this->mSection != '') {
			$Plug = $Plug->With('section', $this->mSection);
		}

		if (!empty($this->mTitleText) && strcmp($this->mTitleText, $this->mTitle->getPathlessText()) != 0)
		{
			$Plug = $Plug->With('title', $this->mTitleText);
		}
		
		if (!empty($editsummary)) {
			$Plug = $Plug->With('comment', $editsummary);
		}
		
		// post contents to API
		/* @var $Result DekiResult */
		$Result = $Plug->With('edittime', $this->getTimestamp())->Post($text);

		//fail cases
		if (!$Result->handleResponse(array(409)))
		{
			if ($Result->is(409))
			{
				wfMessagePush('general', wfMsg('Article.Error.page-already-exists'));
			}
			else
			{
				wfMessagePush('general', wfMsg('Article.Error.page-save-failed'));
			}
			return false;
		}

		//if the API returns a conflict error, output the error
		if ($Result->getVal('body/edit/@status') == 'conflict')
		{
			$newid = $Result->getVal('body/edit/page.overwritten/@revision');

			if (!is_null($Result->getVal('body/edit/page.base/@revision')))
			{
				$oldid = $newid;
				wfMessagePush('general', wfMsg('Article.Common.page-created-with-conflict',
											   '<a href="'. $this->mTitle->getLocalUrl('diff=0&revision='.$oldid) .'">'.
											   wfMsg('Article.Common.page-created-with-conflict-link') .'</a>'), 'conflict');
			}
			else
			{
				$oldid = $Result->getVal('body/edit/page.overwritten/@revision') - 1;
				wfMessagePush('general', wfMsg('Article.Common.page-saved-with-conflict',
											   '<a href="'. $this->mTitle->getLocalUrl('diff=0&revision='.$oldid) .'">'.
											   wfMsg('Article.Common.page-saved-with-conflict-link') .'</a>'), 'conflict');
			}
		}

		//reload the title object so we can redirect to the proper location
		if ($isNew)
		{
			$newid = $Result->getVal('body/edit/page/@id');
			$this->mTitle = Title::newFromID( $newid );
			$this->mTitle->mArticleID = $newid;
		}
		
		$redirectUrl = null;
		$hookResult = DekiPlugin::executeHook(HOOKS::PAGE_SAVE_REDIRECT, array($this, &$redirectUrl));
		if ($hookResult == DekiPlugin::UNREGISTERED || $hookResult == DekiPlugin::UNHANDLED || is_null($redirectUrl))
		{
			$this->showArticle($section);
		}
		else
		{
			$wgOut->redirect($redirectUrl);
		}
		
		$this->loadContent('view', $section);
		DekiPlugin::executeHook(Hooks::PAGE_SAVE, array($this));
		
		return true;
	}

	/**
	 * @todo REQUIRES REVIEW
	 * Show the article in the proper URI (right now, does a redirect)
	 */
	function showArticle( $section = '' ) {
		global $wgOut;
		$sectionanchor = ( empty($section) ) ? '' : '#section_' . $section;		
		$wgOut = new OutputPage();
		$wgOut->redirect( $this->mTitle->getFullURL( $this->isRedirect( ) ? 'redirect=no': '' ) . $sectionanchor );
	}

	/**
	 * @todo MOVE TO API
	 * Add this page to $wgUser's watchlist
	 */
	function watch() {

		global $wgUser, $wgOut;

		if ( $wgUser->isAnonymous() ) {
			wfMessagePush('general', wfMsg('Page.WatchList.error-must-be-logged-in'), 'error');
			return;
		}
		if (wfRunHooks('WatchArticle', array(&$wgUser, &$this))) {
			$wgUser->addWatch( $this->mTitle );

			wfRunHooks('WatchArticleComplete', array(&$wgUser, &$this));

			$wgOut->setPagetitle( wfMsg('Article.Common.added-to-watchlist') );
			$wgOut->setRobotpolicy( 'noindex,follow' );

			$link = $this->mTitle->getDisplayText();
			$text = wfMsg( 'Article.Common.page-added-to-watchlist', $link );
			$wgOut->addHTML("\n<div class='b-body'>$text</div>\n");
		}

		$wgOut->returnToMain( true, $this->mTitle->getPrefixedText(), 0 );
	}

	/**
	 * @todo MOVE TO API
	 * Stop watching a page
	 */
	function unwatch() {

		global $wgUser, $wgOut;

		if ( $wgUser->isAnonymous() ) {
			wfMessagePush('general', wfMsg('Page.WatchList.error-must-be-logged-in'), 'error');
			return;
		}
		if (wfRunHooks('UnwatchArticle', array(&$wgUser, &$this))) {

			$wgUser->removeWatch( $this->mTitle );

			wfRunHooks('UnwatchArticleComplete', array(&$wgUser, &$this));

			$wgOut->setPagetitle( wfMsg('Article.Common.remove-from-watchlist') );
			$wgOut->setRobotpolicy( 'noindex,follow' );

			$link = $this->mTitle->getDisplayText();
			$text = wfMsg( 'Article.Common.page-removed-from-watchlist', $link );
			$wgOut->addHTML("\n<div class='b-body'>$text</div>\n");
		}

		$wgOut->returnToMain( true, $this->mTitle->getPrefixedText(), 0 );
	}


	/**
	 * @todo REQUIRES REVIEW
	 */
    function getParentRedirectURL() {
	    $ns = $this->getTitle()->getNamespace();
	    
	    if (NS_TEMPLATE == $ns) {
	    	$redirectTitle = Title::newFromText(DekiNamespace::getCanonicalName(NS_TEMPLATE).':');
	    }
	    elseif (DekiNamespace::isTalk($ns)) {
	    	switch ($ns)
	    	{
	    		case NS_USER_TALK:
	    			$redirectTitle = Title::newFromText($this->getTitle()->getText(), NS_USER);
	    			break;
	    		case NS_CATEGORY_TALK:
	    			$redirectTitle = Title::newFromText($this->getTitle()->getText(), NS_CATEGORY);
	    			break;
	    		case NS_HELP_TALK:
	    			$redirectTitle = Title::newFromText($this->getTitle()->getText(), NS_HELP);
	    			break;
	    		case NS_IMAGE_TALK:
	    			$redirectTitle = Title::newFromText($this->getTitle()->getText(), NS_IMAGE);
	    			break;
	    		case NS_TEMPLATE_TALK:
	    			$redirectTitle = Title::newFromText($this->getTitle()->getText(), NS_TEMPLATE);
	    			break;
        		default:
	    			$redirectTitle = Title::newFromText($this->getTitle()->getText());
	    			break;
	    	}
	    }
	    else {
			$redirectTitle = $this->getParentTitle();
			
			if (!$redirectTitle)
			{
	        	$redirectTitle = Title::newFromText(wfHomePageInternalTitle());
			}
	    }
        return $redirectTitle->getFullURL();
    }

	/**
	 * Performs the deletion of a page
	 *
	 * @param string $redirectTitle - the page to redirect to after deletion
	 * @param boolean $recursive - determines whether all sub-pages should be deleted as well
	 * @return boolean
	 */
	function doDelete(&$redirectTitle, $recursive = false ) {
		global $wgDekiPlug;
	    global $wgOut, $wgUser, $wgContLang;
		$sk = $wgUser->getSkin();

		$r = $wgDekiPlug->At('pages', $this->getId());
		if ($recursive) {
			$r = $r->With('recursive', 'true');
		}
		$r = $r->With('redirects', 0);
		$r = $r->Delete();
		$deleted = $this->mTitle->getEscapedText();
		// generate a link to the page restore in the new control panel
		$restoreLink = '<a href="'. wfGetControlPanelUrl('page_restore') .'">'. wfMsg('Common.title.page_restore') .'</a>';

		$return = MTMessage::HandleFromDream($r);

		if (!$return)
		{
		    $redirectTitle = $this->mTitle->getFullUrl('delete=no'); //this is passed by reference
			wfMessagePush('general', wfMsg('Article.Error.deletion-has-failed'));
		}
		else
		{
		    $redirectTitle = $this->getParentRedirectURL();  //this is passed by reference
			
			wfMessagePush('general', wfMsg('Article.Common.text-has-been-deleted', $this->mTitle->getDisplayText()), 'success');
			if ($wgUser->isAdmin())
			{
				wfMessagePush('general', wfMsg('Article.Common.text-see-restore-for-deletions', $restoreLink), 'success');
			}
		}

	    return $return;

	}

	/**
	 * Revert a modification
	 */
	function rollback() {
		global $wgUser, $wgOut, $wgRequest;

		// Replace all this user's current edits with the next one down
		$tt = $this->mTitle->getDBKey();
		$articleId = $this->mTitle->getArticleID();
		$n = $this->mTitle->getNamespace();
		
		$revertId = $wgRequest->getVal('revertid');
		
		$Plug = DekiPlug::getInstance();
		$Result = $Plug->At('pages', $articleId, 'revert')->With('fromrevision', $revertId)->Post();
		if (!$Result->handleResponse(array()))
		{
			// error
			$wgOut->redirect($this->mTitle->getLocalUrl('action=history'));
			return;
		}

		wfMessagePush('general', wfMsg('Article.Common.page-revert-success', $revertId), 'success');
		$wgOut->redirect($this->mTitle->getLocalUrl());
	}

	/**
	 * @todo REQUIRES REVIEW
	 *
	 * Displays markup relevant to viewing an archive or diff (revert buttons, extra information)
	 *
	 * @param string $revision
	 */
	function setOldSubtitle( $revision = 0 ) {
		global $wgLang, $wgOut, $wgUser;

		$td = $wgLang->timeanddate( $this->mTimestamp, true );
		$sk = $wgUser->getSkin();
		$lnk = $sk->makeKnownLinkObj ( $this->mTitle, wfMsg('Article.Common.view-current-version') );

		$rollback = '<form method="post" action="'.$this->mTitle->getLocalUrl().'">'
				.DekiForm::singleInput('hidden', 'action', 'rollback')
				.DekiForm::singleInput('hidden', 'revertid', $revision)
				.wfMsg('Article.Common.revert-to-this-version', DekiForm::singleInput('submit', '', wfMsg('Article.Common.submit-revert')))
				.'</form>';
		$wgOut->addHTML('<div class="revisionInfo">'
			.'<p><strong>'.wfMsg('Article.Common.version-as-of', $td).'</strong></p>'
			.$rollback
			.'<p>'. wfMsg('Article.Common.return-to', $sk->makeKnownLinkObj($this->mTitle, wfMsg('Article.Common.version-archive'), 'action=history', '', '', '', true, true)) .'</p>'
			.'<p>'.$lnk.'</p>'
			.'</div>');
	}

	/**
	 * Sets the OutputClass with the redirect message to display in the UI
	 */
	function setRedirectMessage() {
		global $wgUser, $wgOut;
		$sk = $wgUser->getSkin();
		$s = '';
		foreach ( $this->mRedirectedFrom as $redirectedFrom) {
			$redir = $sk->makeKnownLink( $redirectedFrom, '', 'redirect=no' );
			if ($s) $s .= "<br/>";
			$s .= '<span class="redirectedFrom">'.wfMsg('Article.Common.redirected-from', $redir).'</span>';
		}
		$t = Title::newFromText($redirectedFrom);
		$wgOut->setRedirectMessage( wfMsg('Article.Common.redirected-from', strip_tags($redir)) );
		$wgOut->setRedirectLocation_( $sk->makeKnownLinkObj($t, '', 'redirect=no') );
		$wgOut->setRedirectLocation( $t->getLocalUrl('redirect=no'));
	}

	/**
	 * StepanR: function deletes entry from breadcrumb list by article name.
	 * It reads and stores current session, requires no other actions with breadcrumb object
	 */
	function removeLinkFromBreadcrumb() {
    	$lCrumbs = new breadcrumb();

    	$lCrumbs->restoreState($_SESSION);

		// Remove latest entry, because we're standing on it
		unset($lCrumbs->crumbs[0]);
		// Set dirty flag to perform session save
		$lCrumbs->dirty = true;
    	// Write changes to session
    	$lCrumbs->storeState($_SESSION);
	}

}

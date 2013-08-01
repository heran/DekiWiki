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

if (defined('MINDTOUCH_DEKI')) :

class ZendeskPlugin extends DekiPlugin
{
	const SPECIAL_PAGE_TITLE = 'ZendeskNewPage';
	
	// Note: you can override the DEFAULT_PAGE_LOCATION by setting $wgSpecialNewPageRoot = 'ParentDirectory'
	const DEFAULT_PAGE_LOCATION = 'kb';
	const DEFAULT_TEMPLATE = 'MindTouch/IDF/Pages/Knowledge_Base_Page';
	const DEFAULT_TAG_PREFIX = '';
	const TICKET_TAG_PREFIX = 'ticket:';
	const DEFAULT_TAG = 'source:zendesk';
	
	const AJAX_FORMATTER = 'zendesk';
	
	// when true, populates the editor with the body contents from the session
	const PARAM_IMPORT_CONTENT = 'importcontent';
	
	const PARAM_TICKET_ID = 'ticket_id';
	const PARAM_PAGE_TITLE = 'newpage_title';
	const PARAM_PAGE_BODY = 'newpage_body';
	const PARAM_PAGE_XML = 'newpage_xml';
	const PARAM_PAGE_TAGS = 'newpage_tags';
	const PARAM_FROM_LOGIN = 'fromlogin';
	protected $pageName = 'ZendeskNewPage';
	
	public static function init()
	{
		/**
		 * Note: The editor cannot assign tags, etc. to a brand new page. Create the page, populate title & tags, then redirect to edit.
		 * On edit, populate the page body to retain privacy (so details never saved until after review)
		 */
		DekiPlugin::registerHook(Hooks::SPECIAL_PAGE . self::SPECIAL_PAGE_TITLE, array(__CLASS__, 'specialHook'));
		DekiPlugin::registerHook(Hooks::EDITOR_FORM, array(__CLASS__, 'editorHook'));
		DekiPlugin::registerHook(Hooks::AJAX_FORMAT . self::AJAX_FORMATTER, array(__CLASS__, 'ajaxHook'));
	}
	
	/**
	 * AJAX endpoint for status and login. Assumes jsonp
	 *
	 * @param string &$body
	 * @param string &$message
	 * @param bool &$success
	 * @param string &$status
	 * @return N/A
	 */
	public static function ajaxHook(&$body, &$message, &$success, &$status)
	{
		// default to failure
		$body = '';
		$success = false;

		$Request = DekiRequest::getInstance();
		$action = $Request->getVal('action');
		$username = $Request->getVal('username');
		
		switch ($action)
		{
			default:
			case 'status':
				// Bugfix #MT-10299 - force withApiKey for private sites
				$User = DekiUser::newFromText($username, true);
				$body = array('user' => array());
				
				// logged in as the specified user?
				$body['user']['isLoggedIn'] = $User && DekiUser::getCurrent()->getId() == $User->getId();
				
				// false if could not find username
				$body['user']['username'] = $User ? $User->getUsername() : false;
				
				$success = !is_null($User);
				break;
			
			case 'login':
				$password = $Request->getVal('password');
				
				$Result = DekiUser::login($username, $password);
				$success = ($Result === true);
				$status = $success ? 200 : $Result;
				break;
		}
	}
	
	public static function editorHook(&$textareaContents, $forEdit, $displayTitle, $section, $pageId, &$prependHtml, &$appendHtml)  
	{
		global $wgArticle;

		// check proper path
		$parents = $wgArticle->getParents();
		if (strcasecmp($parents[0], self::DEFAULT_PAGE_LOCATION) != 0)
		{
			return;
		}

		// check content available
		$params = self::getParams();
		if (!empty($params['xml']) || !empty($params['body']))
		{
			$body = empty($params['xml']) ? self::formatContents($params['body']) : self::parseXML($params['xml']);
			
			// insert template contents
			$tpTitle = Title::newFromURL(DekiNamespace::getCanonicalName(NS_TEMPLATE) . ':' . self::DEFAULT_TEMPLATE);
			
			if ($tpTitle->getArticleId()) {
				$Plug = DekiPlug::getInstance();
				$Plug = $Plug->At('pages', $tpTitle->getArticleId(), 'contents')->With('mode', 'edit')->With('include', 'true');
				$Result = $Plug->Get();

				$Result->handleResponse();
				$templateBody = $Result->isSuccess() ? $Result->getVal('body/content/body') : '';
				
				// insert body into template if possible
				$pattern = '/<[p]\s+id=["]template-resolution-text["]\s*>.*<\/p>/';
				if (preg_match($pattern, $templateBody)) {
					$body = preg_replace($pattern, $body, $templateBody);
				}
				else {
					// append to template
					$body = $templateBody . $body;
				}
			}
			
			// collect tags
			$tagsArray = empty($params['tags']) ? array() : preg_split("/[\s,]+/", $params['tags']);

            // apply prefixes
            foreach($tagsArray as $key => $tag) {
                $prefix = self::DEFAULT_TAG_PREFIX;
                $tagsArray[$key] = $prefix . $tag;
            }

			$tagsArray[] = self::DEFAULT_TAG;

            // build HTML snippet
            $tags = '<p class="template:tag-insert"><em>Tags imported from Zendesk: </em>';
            foreach($tagsArray as $tag) {
                $tags .= '<a href="#">' . $tag . '</a> ';
            }
            $tags .= "</p>";

			$textareaContents = $body . $tags;
			self::clearSession();
		}
	}

	public static function specialHook($pageName, &$pageTitle, &$html, &$subhtml)
	{
		self::execute();
	}
	
	/**
	 * Create a new page based on request variables
	 */
	protected static function execute()
	{
		global $wgUser, $wgSpecialNewPageRoot, $wgOut;
		$Request = DekiRequest::getInstance();
		
		// require POST, except if redirected from login
		if (!$Request->isPost() && !self::isFromLogin())
		{
			DekiMessage::error(wfMsg('Page.NewPage.error.request'));
			$wgOut->redirectHome();
			return;
		}

		if (self::hasRequestParams())
		{
			self::saveSession();
		}
		
		// anonymous must login
		if ($wgUser->isAnonymous())
		{
			DekiMessage::error(wfMsg('Page.NewPage.error.login'));
			self::redirectAnonymous();
			return;
		}

		// retrieve parameters
		$params = self::getParams();

		// get title of new page
		$parentPath = isset($wgSpecialNewPageRoot) ? $wgSpecialNewPageRoot : self::DEFAULT_PAGE_LOCATION;
        $parentTitle = Title::newFromText($parentPath);
		$newTitle = Article::getNextSubpageTitle($parentTitle, $params['title']);

		if (is_null($newTitle))
		{
			DekiMessage::error(wfMsg('Page.NewPage.error.create'));
			$wgOut->redirectHome();
			return;
		}
		
		// send to edit page
		$query = array('action' => 'edit');
		SpecialPagePlugin::redirect($newTitle->getLocalUrl(http_build_query($query)));
	}
	
	/**
	 * Returns true if this request was originally from a login page
	 * @return bool
	 */
	protected static function isFromLogin()
	{
		return DekiRequest::getInstance()->getBool(self::PARAM_FROM_LOGIN);
	}
	
	protected static function hasRequestParams()
	{
		$Request = DekiRequest::getInstance();
		$ticket_id = $Request->getVal(self::PARAM_TICKET_ID, null);
		return !is_null($ticket_id);
	}
	
	/**
	 * Save current parameters in session (useful if redirecting)
	 */
	protected static function saveSession()
	{
		$Request = DekiRequest::getInstance();
		
		$_SESSION[self::PARAM_TICKET_ID] = $Request->getVal(self::PARAM_TICKET_ID);
		$_SESSION[self::PARAM_PAGE_TITLE] = $Request->getVal(self::PARAM_PAGE_TITLE);
		$_SESSION[self::PARAM_PAGE_BODY] = $Request->getVal(self::PARAM_PAGE_BODY);
		$_SESSION[self::PARAM_PAGE_XML] = $Request->getVal(self::PARAM_PAGE_XML);
		$_SESSION[self::PARAM_PAGE_TAGS] = $Request->getVal(self::PARAM_PAGE_TAGS);
	}
	
	/**
	 * Clear all session parameters
	 */
	protected static function clearSession()
	{
		unset($_SESSION[self::PARAM_TICKET_ID]);
		unset($_SESSION[self::PARAM_PAGE_TITLE]);
		unset($_SESSION[self::PARAM_PAGE_BODY]);
		unset($_SESSION[self::PARAM_PAGE_XML]);
		unset($_SESSION[self::PARAM_PAGE_TAGS]);
	}
	
	/**
	 * Extract new page details from session
	 * @return array - contains keys 'title', 'body', 'xml', 'tags', 'ticket_id'
	 */
	protected static function getParams()
	{
		$Request = DekiRequest::getInstance();
		
		$ticket_id = $_SESSION[self::PARAM_TICKET_ID];
		$title = $_SESSION[self::PARAM_PAGE_TITLE];
		$title = is_null($title) || $title == '' ? wfMsg('Page.NewPage.title.default') : $title;
		$body = $_SESSION[self::PARAM_PAGE_BODY];
		$xml = $_SESSION[self::PARAM_PAGE_XML];
		$tags = $_SESSION[self::PARAM_PAGE_TAGS];
		
		return array('title' => $title, 'body' => $body, 'xml' => $xml, 'tags' => $tags, 'ticket_id' => $ticket_id);
	}
	
	/**
	 * Send the anonymous user to the login page, with current page as return
	 */
	protected static function redirectAnonymous()
	{
		global $wgTitle;
		$returnUrl = $wgTitle->getFullUrl(http_build_query(array(self::PARAM_FROM_LOGIN => 1)));
		
		$LoginTitle = Title::newFromText('UserLogin', NS_SPECIAL);
		SpecialPagePlugin::redirect($LoginTitle->getLocalUrl(http_build_query(array('returntourl' => $returnUrl))));
	}
		
	/**
	 * Create page contents from xml string
	 * @param string $xmlString
	 * @return string - page contents
	 */
	protected static function parseXML($xmlString)
	{
		$xml = new SimpleXMLElement($xmlString);
		$html = '';
		
		$i = 1;
		foreach ($xml->comments->comment as $comment)
		{
			$html .= self::formatContents($comment->value);
			$html .= '<h2>Comment ' . $i++ . '</h2>';
		}

		return $html;
	}
	
	/**
	 * Sanitize and format content string for page
	 * @param string $contents - contents to format
	 * @return string - formatted string
	 */
	protected static function formatContents($contents)
	{
		// normalize line endings
		$contents = preg_replace('/\r/', '', $contents);
		
		// entities & whitespace
		$contents = htmlspecialchars($contents);
		$contents = preg_replace("/[\t ][\t ]/", "&#160;", $contents);
		
		// simple sanitization of dekiscript tags
		$contents = preg_replace('/({{.*?}})/', '<span class="plain">$1</span>', $contents);
		
		// split into paragraphs
		$text = "";
		$paragraphs = preg_split("/\n\n+/", $contents);
		foreach ($paragraphs as $p)
		{
			$text .= '<p>' . nl2br($p) . '</p>';
		}
		
		return $text;
	}
}

// initialize the plugin
ZendeskPlugin::init();

endif;

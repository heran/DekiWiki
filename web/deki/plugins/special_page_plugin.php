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

abstract class SpecialPagePlugin extends DekiPlugin
{
	// TODO: handle listing special pages
	protected $pageName = '';
	// required for including css files
	protected $fileName = null;

	/**
	 * Folder that the special page plugin is located under
	 *
	 * @default /deki/$specialFolder/$fileName/
	 * @note if empty, then /deki/$fileName/
	 */
	protected $specialFolder = 'special_page';
	
	// stores the actual requested page name (case might differ)
	protected $requestedName = '';
	// set to true to require a secure connection to this page
	protected $requireSsl = false;
	// configurable checkAccess switches
	// sets whether the special page name must match exactly, UsEr != User
	protected $requireExactName = false;
	// determines if anonymous users can access this feature
	protected $allowAnonymous = true;
	// set the required user operations to view this special page
	// enforced by calling checkUserAccess()
	protected $requiredOperations = array('BROWSE');
	
	public static function redirect($url, $responseCode = '302', $now = false)
	{
		global $wgOut;

		$wgOut->redirect($url, $responseCode);
		if ($now)
		{
			$wgOut->output();
			exit();
		}
	}
	
	public static function redirectHome()
	{
		global $wgOut;
		
		$wgOut->redirectHome();
	}

	/**
	 * Automagically checks for the location to redirect the user to
	 * 
	 * @param $RedirectTitle - title to redirect to
	 * @param $ReturnTitle - title to return to after the redirect
	 * 
	 * @usage
	 * redirectTo($Title, $ReturnTitle); // Page A
	 * redirectTo();					 // Page B redirects to $ReturnTitle
	 */
	public static function redirectTo($RedirectTitle = null, $ReturnTitle = null)
	{
		$Request = DekiRequest::getInstance();
		$returnToTitle = $Request->getVal('returntotitle');
		$returnToUrl = $Request->getVal('returntourl');
		
		if(!empty($returnToUrl)) 
		{
			$parsed = parse_url($returnToUrl);
			if ($parsed !== false && isset($parsed['host']) && $parsed['host'] == $Request->getHost())
			{
				self::redirect($returnToUrl);
				return;
			}
		}

		if (!is_null($RedirectTitle))
		{
			$get = '';
			// append the return title to the request
			if (!is_null($ReturnTitle))
			{
				$get = 'returntotitle=' . urlencode($ReturnTitle->getPrefixedText());
			}
			self::redirect($RedirectTitle->getFullUrl($get), '301');
		}
		else if ($returnToTitle)
		{
			$Title = Title::newFromText($returnToTitle);
			// fix direct comparisons to 'Special', use NS_SPECIAL etc
			if ($Title->getPrefixedText() == 'Special:Userlogin' || $Title->getPrefixedText() == 'Special:Userlogout')
			{
				$Title = Title::newFromText('');
			}
			
    		self::redirect($Title->getFullURL(), '301');
		}
		else
		{
			$lastPage = breadcrumbMostRecentPage();
			$Title = Title::newFromDBkey($lastPage);
			if ($Title->getArticleId() == 0) 
			{
				$Title = Title::newFromText(wfHomePageInternalTitle());	
			}
    		self::redirect($Title->getFullURL(), '301');
		}
	}


	/**
	 * @param $requestedName - sets the exact case of the requested special page
	 * @param $workingFileName - sets the working folder name for the page
	 * @param $checkAccess - determine if permission checks should be performed (might redirect)
	 */
	public function __construct($requestedName = null, $workingFileName = null, $checkAccess = false)
	{
		$this->requestedName = $requestedName;
		if (!is_null($workingFileName))
		{
			$this->fileName = $workingFileName;
		}
		
		if ($checkAccess)
		{
			$this->checkSpecialAccess();
		}
	}
	
	/*
	 * Method called when page is created
	 */
	protected function checkSpecialAccess()
	{
		// check if exact name is enabled
		if ($this->requireExactName && ($this->requestedName != $this->pageName))
		{
			// invalid special page, redirect to home?
			self::redirectTo();
			return;
		}
		
		// check if SSL is required
		$Request = DekiRequest::getInstance();
		if ($this->requireSsl && !$Request->isSsl())
		{
			// TODO: check for redirect loop, i.e. no ssl available
			$uri = $Request->getFullUri();
			if (strncmp(strtolower($uri), 'http', 4) == 0)
			{
				// set the redirect url to https
				$uri = 'https' . substr($uri, 4);

				self::redirect($uri);
				return false;
			}
		}
		
		// check if anonymous access is allowed
		if (!$this->allowAnonymous)
		{
			// make sure the current user is not anonymous
			if (DekiUser::getCurrent()->isAnonymous())
			{
				// display a message saying they must be logged in for this feature
				DekiMessage::error(wfMsg('Page.Specialpages.error.anonymous', $this->getPageTitle()));
				
				// TODO: attach return to
				$Title = Title::newFromText('UserLogin', NS_SPECIAL);
				self::redirect($Title->getLocalUrl(), 302, true);
				return false;
			}
		}
		
		return true;
	}
	
	/*
	 * Should enforce user based access, up to page implementer to call this method
	 */
	protected function checkUserAccess()
	{
		$User = DekiUser::getCurrent();
		// TODO: (guerrics) hack, hack, hack, should be enforced by the api
		// check if the user has the required ops for the current page
		if (!empty($this->requiredOperations))
		{
			foreach ($this->requiredOperations as $op)
			{
				if (!$User->can($op))
				{
					DekiMessage::error(wfMsg('Article.Common.page-is-restricted'));
					return false;
				}
			}
		}
		
		return true;	
	}
	
	// attempt to generate the page display title automatically
	public function getPageTitle()
	{
		return wfMsg('Page.'. $this->pageName .'.page-title');
	}
	
	/**
	 * @return Title - a title object for this special page
	 */
	public function getTitle()
	{
		return Title::newFromText($this->pageName, NS_SPECIAL);
	}
	
	//abstract public function output();
	
	/*
	 * @param bool $syndicated - determines if the current special page has an rss feed
	 */
	public function setRssSyndication($syndicated = false)
	{
		global $wgOut;
		$wgOut->setSyndicated($syndicated);
	}
	
	/*
	 * @param bool $syndicated - determines if the current special page has an rss feed
	 */
	public function setSyndicationFeed($title, $href, $type = 'application/atom+xml') 
	{
		global $wgOut;
		$wgOut->addLink(array(
			'rel' => 'alternate',
			'type' => $type,
			'title' => $title,
			'href' => $href)
		);
	}
	
	public function setRobotPolicy($robots)
	{
		global $wgOut;
		$wgOut->setRobotpolicy($robots);
	}
	
	/**
	 * Helper method to call after performing a successful post
	 * 
	 * @param string $queryParams - any get parameters that you want to include with the request
	 */
	protected function redirectToSelf($queryParams = '')
	{
		self::redirect($this->getTitle()->getLocalUrl($queryParams));	
	}
	
	/*
	 * Returns the path to the special page folder
	 * Assumes special pages are in a web accessible folder under deki
	 */
	protected function getWebPath()
	{
		if (is_null($this->fileName))
		{
			throw new Exception('A file name has not been set for this special page');
		}
		global $wgDekiPluginPath;

		if (!empty($this->specialFolder))
		{
			/**
			 * Special page root is under another folder
			 * Typical MindTouch special pages are located under the "special_page" folder. This is the default.
			 */
			return $wgDekiPluginPath . '/'. $this->specialFolder .'/' . $this->fileName . '/';	
		}
		else
		{
			/**
			 * Special case for special pages not located under another folder
			 */
			return $wgDekiPluginPath . '/' . $this->fileName . '/';	
		}
	}

	/**
	 * @param string $file - name of the css file to include, should be in the same folder as the plugin
	 * @param bool $external - if true, the specified path is taken as web accessible
	 */
	protected function includeSpecialCss($file, $external = false)
	{
		// @todo kalida: consolidate this to use DekiPlugin::includeCss() code path
		global $wgOut;
		// sets the web accessible path to the file
		if (!$external)
		{
			$href = $this->getWebPath() . $file;
		}
		else
		{
			$href = $file;
		}
		$wgOut->addCss($href);
		parent::$cssIncludes[] = $href;
	}

	/**
	 * @param string $file - name of the js file to include, should be in the same folder as the plugin
	 * @param bool $external - if true, the specified path is taken as web accessible
	 */
	protected function includeSpecialJavascript($file, $external = false)
	{
		global $wgOut;
		// sets the web accessible path to the file
		if (!$external)
		{
			$href = $this->getWebPath() . $file;
		}
		else
		{
			$href = $file;
		}
		$wgOut->addHeadHTML('<script type="text/javascript" src="' . $href . '"></script>' . "\n");
		parent::$javascriptIncludes[] = $href;
	}
}

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


class DekiExtension extends DekiService implements IDekiApiObject
{
	/*
	 * List of known DekiScript sids, should be updated if new sids are introduced
	 */
	static $DEKI_SCRIPT_SIDS = array('sid://mindtouch.com/2007/12/dekiscript',
									 'http://services.mindtouch.com/deki/draft/2007/12/dekiscript'
									 );
	
	// @type bool - determines if the service details have been loaded
	private $detailsLoaded = false;
	// extension specific fields set by the service
	private $helpUri = null;
	// user overrides can be set via service->setPref
	private $title = null;
        private $label = null;
	private $description = null;
	private $namespace = null;
	private $logoUri = null;
	/**
	 * Stores a list of functions to show, default 'all'. API hides the rest.
	 * @type string
	 */
	private $functions = '';


	static function getSiteList($isRunning = null)
	{
		return DekiService::getSiteList(self::TYPE_EXTENSION, $isRunning);
	}

	static function newFromArray(&$result)
	{
		if ($result['type'] != self::TYPE_EXTENSION)
		{
			throw new Exception('Cannot create an extension service from an authentication service');
		}
		$Service = parent::newFromArray($result);
		
		return $Service;
	}

	public function __construct($init, $sid, $uri, $description = null, $config = array(), $prefs = array(), $status = DekiService::STATUS_ENABLED)
	{
		parent::__construct($init, DekiService::TYPE_EXTENSION, $sid, $uri, $description, $config, $prefs, $status);
	}

	public function getHelpUri()
	{
		$this->loadDetails();
		return $this->helpUri;
	}

	/*
	 * User overridable preferences
	 */
	public function getTitle()
	{
		$title = $this->getUserTitle();
		return !is_null($title) ? $title : $this->getDefaultTitle();
	}
	public function getDefaultTitle()
	{
		$this->loadDetails();
		return $this->title;
	}
	public function getUserTitle() { return $this->getPref('title'); }

        public function getLabel()
        {
            $label = $this->getUserLabel();
		return !is_null($label) ? $label : $this->getDefaultLabel();
        }
        public function getDefaultLabel()
        {
            $this->loadDetails();
            return $this->label;
        }
        public function getUserLabel() {return $this->getPref('label'); }
        
	public function getDescription()
	{
		$description = $this->getUserDescription();
		return !is_null($description) ? $description : $this->getDefaultDescription();
	}
	public function getDefaultDescription()
	{
		$this->loadDetails();
		return $this->description;
	}
	public function getUserDescription() { return $this->getPref('description'); }


	public function getNamespace()
	{
		$namespace = $this->getUserNamespace();
		return !is_null($namespace) ? $namespace : $this->getDefaultNamespace();
	}
	public function getDefaultNamespace()
	{
		$this->loadDetails();
		return $this->namespace;
	}
	public function getUserNamespace() { return $this->getPref('namespace'); }


	public function getLogoUri()
	{
		$logoUri = $this->getUserLogoUri();
		return !is_null($logoUri) ? $logoUri : $this->getDefaultLogoUri();
	}
	public function getDefaultLogoUri()
	{
		$this->loadDetails();
		return $this->logoUri;
	}
	public function getUserLogoUri() { return $this->getPref('uri.logo'); }

	
	public function getFunctions()
	{
		$logoUri = $this->getUserFunctions();
		return !is_null($logoUri) ? $logoUri : $this->getDefaultFunctions();
	}
	public function getDefaultFunctions()
	{
		$this->loadDetails();
		return $this->functions;
	}
	public function getUserFunctions() { return $this->getPref('functions'); }
	
	/**
	 * Protected extensions are only available to users with UNSAFECONTENT
	 * @return bool
	 */
	public function isProtected()
	{
		return $this->getPref('protected') == 'true' ? true : false;
	}
	/**
	 * Set the protection status for an extension
	 * @param bool $protected
	 */
	public function setProtected($protected = true)
	{
		$this->setPref('protected', (bool)$protected ? 'true' : 'false');
	}

	/**
	 * @return bool - true if the service is a dekiscript service
	 */
	public function isDekiScript()
	{
		if ($this->getType() == self::TYPE_EXTENSION)
		{
			return in_array($this->getSid(), self::$DEKI_SCRIPT_SIDS);
		}

		return false;
	}
	
	/*
	 * Lazy loading function for the extension's details
	 */
	private function loadDetails()
	{
		if (!$this->detailsLoaded)
		{
			$DekiPool = DekiExtensionPool::getInstance();

			$extensionArray = $DekiPool->getExtensionDetails($this->getUri());

			$X = new XArray($extensionArray);
			$this->label = $X->getVal('label', '');
			$this->title = $X->getVal('title');
			$this->description = $X->getVal('description');
			$this->namespace = $X->getVal('namespace');
			$this->logoUri = $X->getVal('uri.logo');
			$this->helpUri = $X->getVal('uri.help');
			$this->functions = $X->getVal('functions', 'all');

			$this->detailsLoaded = true;
		}
	}
}

/**
 * Helper class for DekiExtension
 * Stores the builtin preference information for all started services
 */
class DekiExtensionPool
{
	private static $instance = null;
	/*
	 * Stores the loaded extensions
	 */
	private $extensions = array();

	// Singleton
	static function &getInstance()
	{
		if (is_null(self::$instance))
		{
			self::$instance = new self;
		}

		return self::$instance;
	}


	private function __construct()
	{
		$this->loadExtensions();
	}

	/**
	 * @return DekiResult
	 */
	public function loadExtensions()
	{
		$Plug = DekiPlug::getInstance()->At('site', 'functions')->With('format', 'xml');

		$Result = $Plug->Get();
		if (!$Result->isSuccess())
		{
			return;
		}

		$extensions = $Result->getAll('body/extensions/extension');
		foreach ($extensions as &$extension)
		{
			$Details = new DekiResult($extension);
	
			$uri = $Details->getVal('uri');
			// special handling until the API exposes the service uri
			if (is_null($uri))
			{
				// need to use dirname to drop the function name
				$uri = $Details->getVal('function/uri');
				// need to handle uri's with attributes
				if (is_array($uri))
				{
					$uri = isset($uri['#text']) ? $uri['#text'] : '';
				}
				// chop off the function name to obtain the uri
				$uri = dirname($uri);
			}

			$this->extensions[$uri] = array(
				'label' => $Details->getVal('label'),
				'title' => $Details->getVal('title'),
                                'label' => $Details->getVal('label'),
				'description' => $Details->getVal('description'),
				'namespace' => $Details->getVal('namespace'),
				'uri.logo' => $Details->getVal('uri.logo'),
				'uri.help' => $Details->getVal('uri.help')
			);
		}
		unset($extension);
	}

	public function getExtensionDetails($uri)
	{
		return isset($this->extensions[$uri]) ? $this->extensions[$uri] : array();
	}
}

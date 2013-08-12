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
 * Class handles loading short deki page information
 */
class DekiPageInfo
{
	const PATH_TYPE_LINKED = 'linked';
	const PATH_TYPE_CUSTOM = 'custom';
	const PATH_TYPE_FIXED = 'fixed';

	// as returned by the API
	const NS_TEMPLATE = 'template';
	const NS_USER = 'user';
	const NS_MAIN = 'main';
	
	/* @var int */
	public $id = null;
	/* @var uri - api location for the verbase page xml */
	public $href = null;
	/* @var string - display title */
	public $title = null;
	/* @var string - page path */
	public $path = null;
	/* @var enum - see path type constants */
	public $pathType = null;
	public $namespace = null;
	/* @var uri */
	public $uriUi = null;


	/**
	 * Moves a page. Info object must correspond to a real page
	 * @see API::pages/{id}/move
	 * 
	 * @param DekiPageInfo &$Info - Page to move. On success, this object is updated with the new page location
	 * @param DekiPageInfo $Parent
	 * @param string $pageTitle - page display title
	 * @param string $pageName - url encoded path segment
	 * @return DekiResult
	 */
	public static function move(DekiPageInfo &$Info, $Parent = null, $pageTitle = null, $pageName = null)
	{
		$Plug = DekiPlug::getInstance();
		$Plug = $Plug->At('pages', $Info->id, 'move');

		if (!is_null($Parent))
		{
			$Plug = $Plug->With('parentid', $Parent->id);
		}
		if (!is_null($pageTitle))
		{
			$Plug = $Plug->With('title', $pageTitle);
		}
		if (!is_null($pageName))
		{
			$Plug = $Plug->With('name', $pageName);
		}
		
		$Result = $Plug->Post();
		if ($Result->isSuccess())
		{
			$count = $Result->getVal('body/pages.moved/@count', 0);
			if ($count > 0)
			{
				$Info = DekiPageInfo::newFromArray($Result->getVal('body/pages.moved/page'));
			}
		}
		
		return $Result;
	}

	/**
	 * Loads the page info from the API
	 * 
	 * @TODO: handle 401?
	 * @param int $id
	 * @param optional int $redirects - {0, 1} if you're following redirects;
	 * @return DekiPageInfo
	 */
	public static function loadFromId($id, $redirects = null)
	{
		$Plug = DekiPlug::getInstance();

		$Plug = $Plug->At('pages', $id, 'info');
		if (!is_null($redirects))
		{
			$Plug = $Plug->With('redirects', $redirects);
		}
		$Result = $Plug->Get();
		if (!$Result->isSuccess())
		{
			return null;
		}

		return self::newFromArray($Result->getVal('body/page'));
	}
	
	/**
	 * Loads the page info from the API by path
	 * 
	 * @param string $path
	 * @param optional int $redirects - {0, 1} if you're following redirects;
	 * @return DekiPageInfo
	 */
	public static function loadFromPath($path, $redirects = null)
	{
		$Plug = DekiPlug::getInstance();

		$Plug = $Plug->At('pages', '='.$path, 'info');
		if (!is_null($redirects))
		{
			$Plug = $Plug->With('redirects', $redirects);
		}
		$Result = $Plug->Get();
		if (!$Result->isSuccess())
		{
			return null;
		}

		return self::newFromArray($Result->getVal('body/page'));
	}

	/**
	 * Creates a page info object from an API page info result
	 * 
	 * @param array $result
	 * @return DekiPageInfo
	 */
	public static function newFromArray(&$result)
	{
		if (empty($result))
		{
			return null;
		}

		$X = new XArray($result);

		$Info = new DekiPageInfo();
		$Info->id = $X->getVal('@id');
		$Info->href = $X->getVal('@href');
		$Info->title = $X->getVal('title');
		$Info->namespace = $X->getVal('namespace');
		$Info->uriUi = $X->getVal('uri.ui');

		$Info->pathType = $X->getVal('path/@type');
		if (!is_null($Info->pathType))
		{
			$Info->path = $X->getVal('path/#text');
		}
		else
		{
			// no path type found
			$Info->path = $X->getVal('path');
			$Info->pathType = self::PATH_TYPE_LINKED;
		}

		return $Info;
	}

	/**
	 * Determine if this info corresponds to the homepage
	 * 
	 * @return bool
	 */
	public function isHomepage() { return empty($this->path); }
	
	/**
	 * Return the path with or without the namespace
	 * 
	 * @param bool $includeNamespace - for main, this is implicitly true
	 * @return string
	 */
	public function getPath($includeNamespace = false)
	{
		if ($this->getNamespace() == self::NS_MAIN || $includeNamespace)
		{
			return $this->path;
		}
		else
		{
			return substr($this->path, strlen($this->getNamespace()) + 1);
		}
	}
	
	/**
	 * Return the namespace
	 * 
	 * @return string
	 */
	public function getNamespace()
	{
		return $this->namespace;	
	}

	/**
	 * Retrieve the partial path segment for this info
	 * 
	 * @return string
	 */
	public function getPathName()
	{
		if ($this->isHomepage())
		{
			return '';
		}
		return array_pop($this->getPathNames());
	}
	
	/**
	 * Retrieve an array of parent path names to this page
	 * 
	 * @return array
	 */
	public function getParents()
	{
		$paths = $this->getPathNames();
		array_pop($paths);
		return $paths;
	}
	
	/**
	 * Retrieve an array of path names
	 * 
	 * @return array
	 */
	public function getPathNames() 
	{
		$encoded = str_replace('//', '%2f', $this->getPath(true));
		$paths = explode('/', $encoded); 
		if (empty($paths))
		{
			return array();
		}
		
		foreach ($paths as $k => $v) 
		{
			$paths[$k] = str_replace('%2f', '//', $v);
		}
		return $paths;
	}
}

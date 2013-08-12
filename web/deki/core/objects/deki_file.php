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
 * Files contains 2 classes
 * @class DekiFile
 * @class DekiFilePreview
 */

/**
 * File meta data handler
 * @note does not handle uploads or file binary data. See DekiFilePreview for image previews.
 *
 * @todo create DekiAttachment extending DekiFile for binary handling?
 */
class DekiFile extends DekiObject implements IDekiApiObject
{
	// @type DekiUser
	protected $Creator = null;
	// @type Title
	protected $Parent = null;
	// @type DekiPageInfo
	protected $ParentInfo = null;
	// @type DekiFileProperties
	// @note do not expose full property bag. wrap properties with file methods
	protected $Properties = null;

	protected $dateCreated = null;
	protected $mimeType = null;
	protected $size = 0;
	protected $href = null;
	// @type array - stores a list of changes for this revision
	protected $creatorActions = array();

	protected $description = null;
	// caches the file extension
	protected $extension = null;
	// @type int - revision number
	protected $revision = 0;
	// @type int - total number of revisions for this file
	protected $revisionCount = 0;

	/**
	 * @param int $id
	 * @return DekiFile
	 */
	public static function newFromId($id)
	{
		$result = self::load($id);
		if (is_null($result))
		{
			return null;
		}
		
		return self::newFromArray($result);
	}

	public static function newFromText($title)
	{
		throw new Exception('Not implemented');
	}

	/**
	 * Retrieve an array of file objects corresponding to the revisions
	 * 
	 * @param int $id - file id
	 * @param bool $sortDesc - sort the resulting revisions newest to oldest
	 * @return array<DekiFile> - null if the revisions could not be retrieved
	 */
	public function loadRevisionList($id, $sortDesc = true)
	{
		$revisionList = array();
		// @note bugfix #7840, valid changefilters: CONTENT, NAME, LANGUAGE, META, DELETEFLAG, PARENT
		$Result = DekiPlug::getInstance()->At('files', $id, 'revisions')->With('changefilter', 'PARENT,CONTENT,NAME')->Get();
		if (!$Result->isSuccess())
		{
			return null;
		}
		
		$revisionCount = $Result->getVal('body/files/@totalcount');
		$fileRevisions = $Result->getAll('body/files/file', array());
		foreach ($fileRevisions as $fileRevision)
		{
			$Revision = DekiFile::newFromArray($fileRevision, $revisionCount);
			$revisionList[] = $Revision;
		}

		// reverse the order, api returns in ascending order
		if ($sortDesc)
		{
			$revisionList = array_reverse($revisionList);
		}
		
		return $revisionList;
	}
	
	/**
	 * Pages xml does not include the parent information, use this method
	 * @param array $result
	 * @param Title $Parent
	 * @return DekiFile
	 */
	public static function newFromPagesArray(&$result, $Parent)
	{
		$File = self::newFromArray($result);
		$File->Parent = $Parent;
		return $File;	
	}
	
	/**
	 * @param array $result
	 * @param int $revisionCount - total number of revisions for the file (required for files/{id}/revisions api call)
	 * @param Title $Parent
	 * @return DekiFile
	 */
	public static function newFromArray(&$result, $revisionCount = null)
	{
		$File = new DekiFile();
		self::populateObject($File, $result, $revisionCount);

		return $File;
	}

	/**
	 * Updates a file's meta data
	 * @param DekiFile &$File - File must already exist
	 * @return DekiResult
	 */
	public static function updateDescription(DekiFile &$File, $description)
	{
		$Result = DekiPlug::getInstance()->At('files', $File->getId(), 'description')->Put($description);
		if ($Result->isSuccess())
		{
			self::populateObject($File, $Result->getVal('body/file'));
			return true;
		}

		return $Result;
	}
	
	public static function delete($id)
	{
		$Result = DekiPlug::getInstance()->At('files', $id)->Delete();
		if ($Result->isSuccess())
		{
			return true;
		}

		return $Result;
	}

	protected static function load($id)
	{
		$Result = DekiPlug::getInstance()->At('files', $id, 'info')->Get();
		if ($Result->isSuccess())
		{
			return $Result->getVal('body/file');
		}
		
		return null;
	}

	/**
	 * @param $File - should be of the type family DekiFile
	 * @param array $result
	 */
	protected static function populateObject(&$File, &$result, $revisionCount = null)
	{
		$Result = new XArray($result);

		$File->setId($Result->getVal('@id'));
		$File->setName($Result->getVal('filename'));
		$File->description = $Result->getVal('description');

		$File->mimeType = $Result->getVal('contents/@type');
		$File->size = $Result->getVal('contents/@size');
		$File->href = $Result->getVal('contents/@href');
		
		$File->revision = $Result->getVal('@revision');
		// file info call
		$File->revisionCount = $Result->getVal('revisions/@totalcount');
		if (is_null($File->revisionCount))
		{
			// xml does not contain the total # of revs, use arg
			$File->revisionCount = $revisionCount;
		}
		
		// set the properties
		$File->Properties = DekiFileProperties::newFromArray($File->getId(), $Result->getVal('properties', array()));

		// set the parent
		$File->ParentInfo = DekiPageInfo::newFromArray($Result->getVal('page.parent'));
		$path = $Result->getVal('page.parent/path');
		$File->Parent = Title::newFromText($path);
		
		$File->Creator = DekiUser::newFromArray($Result->getVal('user.createdby'));
		$File->dateCreated = $Result->getVal('date.created');
		
		// revision change information
		$actions = $Result->getVal('user-action/@type');
		// normalize the actions into an array
		$actions = explode(',', $actions);
		foreach ($actions as $action)
		{
			$action = strtoupper(trim($action));
			$File->creatorActions[$action] = $action;
		}
	}

	/**
	 * There are 2 ways to create a new DekiFile: 
	 * 	public function __construct($fileId);
	 * 	public function __construct($fileName, $fileType = null);
	 * 
	 * @param mixed $fileId - if integer, fileId is assumed
	 * @param string $fileType
	 */
	public function __construct($fileId = null, $fileType = null)
	{
		if (is_numeric($fileId))
		{
			$this->setId($fileId);
		}
		else
		{
			$this->setName($fileId);
			$this->setType($fileType);
		}
	}


	/**
	 * @return bool
	 */
	public function isRevision()
	{
		return $this->revision != $this->revisionCount;	
	}	
	public function hasRevisions() { return $this->revisionCount > 1; }

	/**
	 * Methods determine what changed for the current revision
	 * @return bool
	 */
	public function wasMoved() { return ($this->revision > 1) && isset($this->creatorActions['PARENT']); }
	public function wasRenamed() { return ($this->revision > 1) && isset($this->creatorActions['NAME']); }
	public function wasUpdated() { return ($this->revision > 1) && isset($this->creatorActions['CONTENT']); }
	
	public function getHref() { return $this->href; }

	public function getSize($formatted = true) { return $formatted ? wfFormatSize($this->size) : $this->size; }
	public function getExtension()
	{
		if (is_null($this->extension)) 
		{
			$this->extension = pathinfo($this->getName(), PATHINFO_EXTENSION);
			if (empty($this->extension))
			{
				// TODO: determine the file extension via mime-type?
			}
		}

		// Bugfix #MT-9407 - ensure we always lookup styles, etc. by lowercase extension
		return strtolower($this->extension);
	}
	/**
	 * Mime-type
	 * @return string
	 */
	public function getType()
	{
		if (empty($this->mimeType) || $this->mimeType == 'application/octet-stream')
		{
			global $wgMimeTypes;
			
			$extension = $this->getExtension();
			if (array_key_exists($extension, $wgMimeTypes))
			{
				$this->mimeType = $wgMimeTypes[$extension];
			}
		}
		
		return $this->mimeType;
	}
	
	public function getDescription() { return $this->description; }

	/**
	 * @return Title
	 */
	public function getParentTitle() { return $this->Parent; }
	/**
	 * @return DekiPageInfo
	 */
	public function getParentInfo() { return $this->ParentInfo; }
	/**
	 * @return int
	 */
	public function getParentId() { return $this->Parent->getArticleID(); }
	/**
	 * @return DekiUser
	 */
	public function getCreator() { return $this->Creator; }
	public function getTimestamp($formatted = true)
	{
		if ($formatted) {
			global $wgLang;
			return $wgLang->timeanddate($this->dateCreated, true);
		}
		return $this->dateCreated;
	}

	public function setType($fileType)
	{
		$this->mimeType = $fileType;
	}

	// presenation methods
	// TODO: guerrics - consider generating markup elsewhere
	/**
	 * Markup generation function for file
	 * 
	 * @param string $text - contents for the file link, treated as raw, preencode if required
	 * @return string - file anchor
	 */
	public function getLink($text = null)
	{
		$filename = htmlspecialchars($this->getName());
		if (is_null($text))
		{
			$text = $filename;
		}

		return '<a href="'. $this->getHref() .'" class="filelink" title="'. $filename .'">'. $text .'</a>';
	}
	public function getWebDavEditLink()
	{
		global $wgWebDavExtensions;
		$fileext = $this->getExtension();
		if (is_null($fileext) || !in_array($fileext, array_merge($wgWebDavExtensions) ))
		{
			return '';
		}

		return ' <a href="#" class="deki-webdavdoc" onclick="return loadOfficeDoc(\''
				 . wfEncodeJSHTML($this->getHref('authtoken='.DekiToken::get())) .'\');">' 
				 . wfMsg('Article.Attach.edit') .'</a>';
	}
	/**
	 * @return string - span containing the icon classes
	 */
	public function getIcon()
	{
		global $wgStyledExtensions;
		$styledExtension = in_array($this->getExtension(), $wgStyledExtensions)
			? $this->getExtension()
			: 'unknown'
		;

		return Skin::iconify('mt-ext-' . $styledExtension);
	}
	public function getThumbImage() 
	{
		$filename = $this->toHtml();
		$description = htmlspecialchars($this->getDescription());
		
		return '<img src="'. $this->getThumb() .'" title="'. $this->toHtml() .'" alt="'. (empty($description) ? $filename : $description) .'" />'; 
	}
	// /presentation methods


	public function toArray($verbose = false)
	{
		$file = array(
			'filename' => $this->getName(),
			'href' => $this->href
		);

		if (!is_null($this->description) && $verbose)
		{
			$file['description'] = $this->description;
		}

		if (is_object($this->Parent) && $verbose)
		{
			$parentArray = $this->Parent->toArray();
			$file['parent'] = &$parentArray['page'];
		}

		if (is_object($this->Properties) && $verbose)
		{
			$properties = $this->Properties->toArray();
			$file['properties'] = $properties['properties'];
		}

		$id = $this->getId();
		if (!is_null($id))
		{
			$file['@id'] = $id;
		}

		return $file;
	}
}


/**
 * Wraps the preview information for files
 */
class DekiFilePreview extends DekiFile
{
	// Full size information
	protected $width = null;
	protected $height = null;

	/**
	 * Stores the information about the file previews
	 * @var array
	 */
	protected $previews = array();

	/**
	 * @param int $id
	 * @return DekiFilePreview
	 */
	public static function newFromId($id)
	{
		$result = self::load($id);
		if (is_null($result))
		{
			return null;
		}
		
		return self::newFromArray($result);
	}

	public static function newFromArray(&$result)
	{
		$File = new self();
		self::populateObject($File, $result);

		return $File;
	}
 
	protected static function populateObject(&$File, &$result, $revisionCount = null)
	{
		parent::populateObject($File, $result, $revisionCount);

		$X = new XArray($result);
		$previews = $X->getAll('contents.preview');
		if (!is_null($previews))
		{		
			// add the fullsize information
			$File->width = $X->getVal('contents/@width');
			$File->height = $X->getVal('contents/@height');
	
			// grab the preview information
			foreach ($previews as $preview)
			{		
				$File->addPreview(
					$preview['@rel'],
					$preview['@href'],
					$preview['@type'],
					$preview['@maxwidth'],
					$preview['@maxheight']
				);
			}
		}
	}


	/**
	 * @return bool
	 */
	public function hasPreview() { return !empty($this->previews); }
	public function hasThumb() { return isset($this->previews['thumb']); }
	public function hasWebview() { return isset($this->previews['webview']); }
	
	/**
	 * @return int
	 */
	public function getWidth() { return $this->width; }
	public function getHeight() { return $this->height; }

	/**
	 * @return string - url for the fullsize image
	 */	
	public function getFullsize() { return $this->getHref(); }
	/**
	 * @return string - url for the webview
	 */
	public function getWebview()
	{
		return isset($this->previews['webview']['href']) ? $this->previews['webview']['href'] : '';
	}
	/**
	 * @return string - url for the thumbnail
	 */
	public function getThumb()
	{
		return isset($this->previews['thumb']['href']) ? $this->previews['thumb']['href'] : '';
	}
	
	/**
	 * @return string - url for a custom size
	 */
	public function getCustom($maxwidth = null, $maxheight = null)
	{
		throw new Exception('Not implemented');

		$href = $this->getHref();
		// TODO: parse url
		if (!is_null($maxwidth))
		{
			$href .= '&width='.$maxwidth;
		}
		if (!is_null($maxheight))
		{
			$href .= '&height='.$maxheight;
		}
		
		return $href;
	}


	protected function addPreview($rel, $href, $type, $maxwidth, $maxheight)
	{
		$this->previews[$rel] = array(
			'href' => $href,
			'type' => $type
		);
	}
}

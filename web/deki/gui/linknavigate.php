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

require_once('gui_index.php');

/**
 * Called when navigating around pages on the link dialog
 */
class LinkNavigate extends DekiFormatter
{
	protected $contentType = 'application/json';
	//protected $requireXmlHttpRequest = true;
	protected $disableCaching = true;

	// Configuration	
	private $filterMimeType = null;
	private $jsonPageUrl = '/deki/gui/linknavigate.php?page=%s';

	private $intoLink = false;


	public function format()
	{
		global $wgDekiPlug, $wgDekiApi;
		$Request = DekiRequest::getInstance();

		// the requested page/pageid
		// if page is not set then we just enter at the root level
		$page = $Request->getVal('page');
		
		// check if we need to start from a file's page
		$fileId = $Request->getInt('file_id', null); // used for generate json
		
		switch ($page)
		{
			case 'root':
				$page = null;
				break;

			case 'home':
				$page = 'home';
				break;

			case 'template':
				$page = 'template'; 
				break;

			case 'user':
				$page = 'user';
				break;
			
			default:
				// check if we should search by title
				$title = $Request->getVal('title');
				if (!is_null($title))
				{
					// decode the incoming title, encoded by js
					$title = urldecode($title);
					// replace slashes for some reason
					$title = str_replace('\\\\', '\\', $title) . "\n";
					// replace spaces
					$title = str_replace(' ', '_', $title);
					
					// detect if the title is the link to a file
					if ( preg_match('/^\S+?' . $wgDekiApi . '\/files\/(\d+?)\/=(\S+)$/i', $title, $matches) )
					{
						$fileId = $matches[1];
					}
					else
					{
						// only need to encode once since the incoming is already encoded
						$page = urlencode(urlencode($title));
					}
				}
				else
				{
					// search by pageId
					$page = $Request->getInt('page', null);
				}
				break;
		}

		// check if we only want certain children types
		$type = $Request->getVal('type');
		switch ($type)
		{
			case 'page':
			//case 'document':
			case 'image':
				$this->filterMimeType = $type;
				break;
			default: 
		}

		if (!is_null($fileId))
		{
			// figure out the page that this file is located on
			$filePageId = $this->getFilePageId($fileId);
			if (!is_null($filePageId))
			{
				$page = $filePageId;
			}
		}
		
		$useParent = $Request->has('parent') ? true : false;
		$pageDetails = $this->getPageDetails($page, $useParent);

		$this->generateJson($pageDetails, is_numeric($page) ? $page : null, is_null($fileId) ? null : $fileId);
		
		// (karena) see bug#5266
		// user can attach/delete files during editing the page
		// so we should prevent caching of files list
		$this->disableCaching();

		echo $this->body;
	}
	
	/**
	 * Determines the parent page for a file
	 *
	 * @param $fileId the file to find the parent for
	 * @return $pageId of the parent or null if the file could not be found
	 */
	private function getFilePageId($fileId)
	{
		global $wgDekiPlug;

		$result = $wgDekiPlug->At('files', $fileId, 'info')->Get();
		if ($result['status'] != Plug::HTTPSUCCESS)
		{
			return null;
		}
		$parent = wfArrayVal($result, 'body/file/page.parent');
		
		return isset($parent['@id']) ? $parent['@id'] : null;
	}

	/**
	 * @param mixed $pageId can be a string like 'user' or 'home'
	 *						can also be an integer representing the pageid to fetch
	 * @param bool $useParent determines if we should load the page's parent details
	 */
	private function getPageDetails($pageId, $useParent = false)
	{
		global $wgDekiPlug, $wgSitename, $wgLang;

		$parentId = null;
		
		if (is_null($pageId))
		{
			// root section, special case
			// return root page & user page

			$subpages = array();
			$files = array();
			
			// root page
			$subpages[] = array('parentId' => null,
								'id' => 'home',
								'title' => $wgSitename,
								'path' => '/',
								'terminal' => false
								);
			// user page
			$subpages[] = array('parentId' => null,
								'id' => 'user',
								'title' => wfMsg('Page.ListUsers.page-title'),
								'path' => 'User:',
								'terminal' => false
								);

			// template page
			$subpages[] = array('parentId' => null,
								'id' => 'template',
								'title' => wfMsg('Page.ListTemplates.page-title'),
								'path' => 'Template:',
								'terminal' => false
								);

			return array('parentId' => null,
						 'id' => null,
						 'title' => null,
						 'path' => null,

						 'subpages' => $subpages,
						 'files' => array()
						);		
		}
		else
		{
			switch ($pageId)
			{
				case 'user':
					$username = DekiRequest::getInstance()->getVal('name');
					$result = $wgDekiPlug->At('pages', '='. rawurlencode(rawurlencode($wgLang->getNsText(NS_USER) .':'. $username)))->Get();
					break;
				case 'template':
					$result = $wgDekiPlug->At('pages', '='. rawurlencode(rawurlencode($wgLang->getNsText(NS_TEMPLATE) . ':')))->Get();
					break;

				case 'home':
				default:
					if ($pageId != 'home' && !is_numeric($pageId) )
					{
						$pageId = '=' . $pageId ;
					}
					$result = $wgDekiPlug->At('pages', $pageId)->Get();
			}

			if ($result['status'] == Plug::HTTPNOTFOUND)
			{
				$subpages = array();
				$subpages[] = array('parentId' => null,
									'id' => null,
									'title' => 'Page not found',
									'path' => null,
									'href' => null,
									'terminal' => true
									);

				return array('parentId' => null,
							 'id' => null,
							 'title' => null,
							 'path' => null,

							 'subpages' => $subpages,
							 'files' => array()
							);		
			}
			else if ($result['status'] != Plug::HTTPSUCCESS)
			{
				die(wfMsg('System.Error.error'));
			}

			$page = wfArrayVal($result, 'body/page');

			// get the real pageId incase home was specified
			$pageId = isset($page['@id']) ? $page['@id'] : $pageId;
			// get the page's parent id, null if no parent
			$parentId = $this->getParentId($result);

			// make sure the page has a parent
			if ($useParent)
			{
				// hack: special case for missing page parents
				if (isset($page['page.parent']) || ($parentId == 'user' || $parentId == 'template'))
				{
					return $this->getPageDetails($parentId);
				}
			}

			// process the info
			list($subpages, $files) = $this->getPageChildren($pageId);

			return array('parentId' => $parentId,
						 'id' => $pageId,
						 'title' => $page['title'],
						 'path' => $page['path'],

						 'subpages' => $subpages,
						 'files' => $files
						);
		}
	}
	
	private function getParentId(&$result)
	{
		$page = &$result['body']['page'];
		$parentId = null;

		// check if the page parent is root
		if (isset($page['page.parent']))
		{
			$parent = wfArrayVal($page, 'page.parent');
			$parentId = $parent['@id'];
		}
		else if (!empty($page['path']))
		{
			// handle pages without parents
			$Title = Title::newFromText($page['path']);	
			$parentId = 'root';

			if ($Title->getText() != '')
			{
				switch ($Title->getNamespace())
				{
					case NS_USER:
						$parentId = 'user'; break;
					case NS_TEMPLATE:
						$parentId = 'template'; break;
					default:
				}
			}
		}
		else
		{
			// root wiki page
			$parentId = 'root';
		}

		return $parentId;
	}

	private function getPageChildren($pageId)
	{
		global $wgDekiPlug, $wgApi, $wgDeki;

		if ($pageId == null)
		{
			$pageId = 'home';
		}

		$subpages = array();
		$files = array();

		$childResult = $wgDekiPlug->At('pages', $pageId, 'files,subpages')->Get();
		if ($childResult['status'] != Plug::HTTPSUCCESS)
		{
			die('Error!');
		}

		$child = &$childResult['body']['page'];
		if (isset($child['subpages']['page.subpage']))
		{
			$childSubpages = wfArrayValAll($child['subpages'], 'page.subpage');
			foreach ($childSubpages as $childPageDetails)
			{
				$path = $childPageDetails['path'];
				
				//quotes need to be manually escape, using url encoding
				$path = str_replace('"', '%22', $path);

				$subpages[] = array('parentId' => $pageId,
									'id' => $childPageDetails['@id'],
									'title' => $childPageDetails['title'],
									'path' => $path,
									'href' => $childPageDetails['@href'],
									'terminal' => ($childPageDetails['@terminal'] == 'true') ? true : false
									);
			}
		}

		if (isset($child['files']['file']))
		{
			// api location for files
			$basePath = sprintf('/%s/%s/files', $wgApi, $wgDeki);
			//$basePath .= empty($child['path']) ? '' : $child['path'] . '/';

			$childFiles = wfArrayValAll($child['files'], 'file');
			foreach ($childFiles as $childFileDetails)
			{
				if (!is_null($this->filterMimeType))
				{
					list($mime, $type) = explode('/', $childFileDetails['contents']['@type']);
					if ($this->filterMimeType != $mime)
					{
						continue;
					}
				}

				$filePath = sprintf('%s/%s/=%s', $basePath, $childFileDetails['@id'], $childFileDetails['filename']);
				// bugfix #4326: Spaces on image names are replaced by "%20" and not with an "_" on the html code when inserting images 
				$spacedFilename = str_replace(' ', '_', $childFileDetails['filename']);
				$encodedFilePath = sprintf(
					'%s/%s/=%s',
					$basePath,
					$childFileDetails['@id'],
					rawurlencode(rawurlencode($spacedFilename))
				);
				// bugfix #5515: Inserted images containing spaces in their name are broken when running Deki on Windows 
				$filePath = $encodedFilePath;
				
				$hasPreview = isset($childFileDetails['contents.preview']);
				$width = isset($childFileDetails['contents']['@width']) ? $childFileDetails['contents']['@width'] : null;
				$height = isset($childFileDetails['contents']['@height']) ? $childFileDetails['contents']['@height'] : null;

				$files[] = array('id' => $childFileDetails['@id'],
								 'filename' => $childFileDetails['filename'],
								 'filetype' => $childFileDetails['contents']['@type'],
								 'path' => $filePath,
								 'encodedPath' => $encodedFilePath, // used for image preview
								 'href' => $childFileDetails['contents']['@href'],
								 'preview' => $hasPreview,
								 'width' => $width,
								 'height' => $height
								 );
			}
		}

		return array($subpages, $files);
	}

	/*
	 * Removes underscores for links
	 */
	private function formatLink($path)
	{
		$Title = Title::newFromText($path);
		return $this->safeString($Title->getPrefixedURL());
	}

	function safeString($s)
	{
		return wfEscapeString($s);
	}

	private function getAjaxUrl($pageId, $disabledPageId = null)
	{
		$url = sprintf($this->jsonPageUrl, $pageId);
		
		if (!is_null($this->filterMimeType))
		{
			$url .= sprintf('&type=%s', $this->filterMimeType);
		}

		if (!is_null($disabledPageId))
		{
			$url .= sprintf('&disabled_page=%s', $disabledPageId);
		}

		return $url;
	}

	private function generateJson(&$pageDetails, $selectedPageId, $selectedFileId, $disabledPageId = null)
	{
		//$pageTitle = $pageDetails['title'];
		//$pagePath = $pageDetails['path'];
		$parentId = isset($pageDetails['parentId']) ? $pageDetails['parentId'] : null;
		$pageId = $pageDetails['id'];

		$json = '';
		// generate the json for columnav
		$json .= '{ "ul": { "li": [ ';

		// attach the parent id information
		if ( !is_null($parentId) )
		{
			$url = $this->getAjaxUrl($pageDetails['parentId'], $disabledPageId);
			$json .= sprintf('{"a": {"@href": "%s", "@rel": "prevajax", "#text": "", "@style": "display:none;" }},', $url);
		}

		foreach ($pageDetails['subpages'] as $subpage)
		{
			$disabled = false;

			// page
			$path = $this->formatLink($subpage['path']);
			$class = 'page';

			if ($subpage['id'] == $disabledPageId)
			{
				$class .= ' disabled';
				//$path = '';
				$disabled = true;
			}

			if ($subpage['id'] == $selectedPageId)
			{
				$class .= ' columnav-active';
			}

			$json .= '{';
			$title = $this->safeString($subpage['title']);
			
			$titlePath = '';
			$titleName = '';

			$Title = Title::newFromText($subpage['path']);
			$titlePath = $this->safeString($Title->getParentPath());
			$titleName = $this->safeString($Title->getPathlessText());

			if (empty($path))
			{
				// root special case
				$path = $titlePath = $this->safeString('/');
			}

			if ($subpage['terminal'] || $disabled)
			{
				// page does not have children
				// actions for empty page object
				$json .= sprintf('"a": {"@href": "", "@title": "%s", "#text": "%s", "@path": "%s", "@class": "%s", "@pid": "%s", "@pathA": "%s", "@pathB": "%s"}', $title, $title, $path, $class, $subpage['id'], $titlePath, $titleName);
			}
			else
			{
				// page has children
				// actions for ajax page objects
				$url = $this->getAjaxUrl($subpage['id'], $disabledPageId);
				$json .= sprintf('"a": {"@href": "%s", "@rel": "ajax", "@title": "%s", "#text": "%s", "@path": "%s", "@class": "%s", "@pid": "%s", "@pathA": "%s", "@pathB": "%s"}',	$url, $title, $title, $path, $class, $subpage['id'], $titlePath, $titleName);
			}
			$json .= '},';  
		}

		foreach ($pageDetails['files'] as $file)
		{
			$json .= '{';
			// don't remove underscores for files
			//$path = empty($file['path']) ? '' : 'File:'. $file['path'];
			//list($class, $type) = explode('/', $file['filetype'], 2);
			$ex = explode('.', $file['filename']);
			$extension = empty($ex) ? '' : array_pop($ex);
			$class = 'mt-ext-'. $extension;
			if ($file['id'] == $selectedFileId)
			{
				$class .= ' columnav-active';
			}
			$append = '';

			if ($file['preview'])
			{
				// file is an image
				$preview = $file['encodedPath'] . '?size=thumb';
				//$preview = rawurlencode($file['path'] . '?size=thumb');
				$append = sprintf(',"@preview": "%s", "@width": "%s", "@height": "%s"', $preview, $file['width'], $file['height']);
			}

			$name = $this->safeString($file['filename']);
			$json .= sprintf('"a": {"@href": "", "@title": "%s", "#text": "%s", "@path": "%s", "@class": "%s"%s}', $name, $name, $file['path'], $class, $append);
			$json .= '},';
		}

		$json = substr($json, 0, -1);

		$json .= '] } }';
		
		// compress the json a weee bit
		$this->body = str_replace(': "', ':"', $json);
	}

}
new LinkNavigate();

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

error_reporting(0);
require_once($IP . '/includes/Defines.php');
require_once($IP . '/includes/Setup.php');

/**
 * Called when searching for pages with the link dialog
 */
class LinkFormatter extends RequestFormatter
{
	// search related constants
	const RESULT_LIMIT = 6;
	const HIGHLIGHT_CLASS = 'search-highlight';


	public function LinkFormatter()
	{
		parent::__construct(true);
	}

	public function format(&$paths, &$params)
	{
		$method = !empty($paths) ? array_shift($paths) : null;
		switch ($method)
		{
			case 'search':
				$query = isset($params['query']) ? $params['query'] : null;

				$this->formatSearch($query);
				break;

			case 'navigate':
				// assume navigate
				$pageId = isset($params['pageId']) ? $params['pageId'] : null;
				$fileId = isset($params['fileId']) ? (int)$params['fileId'] : null;

				$this->formatNavigate($pageId, $fileId);
				break;

			case 'flickr':
				$tags = isset($params['tags']) ? trim($params['tags']) : null;
				$this->formatFlickr($tags);
				break;
			default:
		}
	}

	// from yahoo
	private function formatFlickr($tags)
	{
		// Yahoo! proxy

		// Hard-code hostname and path:
		$flickrApi = 'http://www.flickr.com/services/rest/';

		// Get all query params
		$query = "?";
		//foreach ($_GET as $key => $value) {
		//	$query .= urlencode($key)."=".urlencode($value)."&";
		//}

		//foreach ($_POST as $key => $value) {
		//	$query .= $key."=".$value."&";
		//}
		$query .= "tags=".$tags;
		$query .= "&method=flickr.photos.search";
		$query .= "&api_key=30cc0cf363608a1ffa3fc1631854c8b8";
		$url = $flickrApi.$query;

		// Open the Curl session
		$session = curl_init($url);

		// Don't return HTTP headers. Do return the contents of the call
		curl_setopt($session, CURLOPT_HEADER, false);
		curl_setopt($session, CURLOPT_RETURNTRANSFER, true);

		// Make the call
		$this->contentType = 'text/xml';
		$this->body = curl_exec($session);

		curl_close($session);
		return;
	}


	/*
	 * Start link search functions
	 */
	private function formatSearch($query = null)
	{
		global $wgDekiPlug, $wgDreamServer, $wgApi, $wgDeki; //$wgDekiApi
		$this->contentType = 'text/plain';

		if (empty($query))
		{
			$this->body = '<no results>';
			return;
		}

		// make the query safe
		$query = str_replace(array(':'), array('\:'), $query);
		// @note (guerric) using the path returns weird result sets
		//$searchString = sprintf('title: "%s" OR path: %s', $query, $query);
		$searchString = sprintf('title: "%s"', $query);

		// added for the image dialog to restrict the search results
		if (isset($_GET['type']))
		{
			switch ($_GET['type'])
			{
				case 'wiki':
				case 'document':
				case 'image':
					$searchString .= sprintf('AND type: "%s"', $_GET['type']);
					break;
				default: 
			}
		}

		// @note (guerric) didn't use with('format', 'search') because it mixes pages and files together
		$result = $wgDekiPlug->At("site", "search")->With('q', addslashes($searchString))->With('limit', self::RESULT_LIMIT)->Get();
		$this->body = '';

		if ($result['status'] == Plug::HTTPSUCCESS)
		{
			$search = &$result['body']['search'];


			if (isset($search['page']))
			{
				// get array regardless of # results
				$pages = wfArrayValAll($search, 'page');

				foreach ($pages as $page)
				{
					$pageId = $page['@id'];
					$pageTitle = $page['title'];
					$pagePath = empty($page['path']) ? '/' : $this->formatLink($page['path']);

					$displayClass = 'mt-ext-txt';

					// add the highlighting tags to the string
					$displayTitle = $this->highlightText($pageTitle, $query, self::HIGHLIGHT_CLASS);

					$this->body .= $this->printResultRow($pageTitle, $displayTitle, $pagePath, $displayClass, $pageId);
				}
			}


			if (isset($search['file']))
			{
				// get array regardless of # results
				$files = wfArrayValAll($search, 'file');

				// api location for files
				// TODO: fix this api url?
				$basePath = '/' . $wgApi . '/' . $wgDeki . '/files';

				foreach ($files as $file)
				{
					$parent = wfArrayValAll($file, 'page.parent');
					$pageId = isset($parent[0]['@id']) ? $parent[0]['@id'] : 0;

					list($mime, $type) = explode('/', $file['contents']['@type'], 2);
					
					$parentPath = empty($file['page.parent']['path']) ? '' : $this->formatLink($file['page.parent']['path']) . '/';
					$filename = $parentPath . $file['filename'];
					
					// set the file path
					$filePath = sprintf('%s/%s/=%s', $basePath, $file['@id'], $file['filename']);

					// add the highlighting tags to the string
					$displayFilename = $this->highlightText($filename, $query, self::HIGHLIGHT_CLASS);

					$this->body .= $this->printResultRow($filename, $displayFilename, $filePath, 'mt-ext-'.$type, $pageId, $file['@id']);
				}
			}
		}
		else
		{
			// TODO: handle the error case
			$this->body .= $this->printResultRow(wfMsg('System.Error.error'), '', wfMsg('System.Error.error'));
		}

		if (empty($this->body))
		{
			$this->body = '<no results>';
		}
	}

	/*
	 * Removes underscores for links
	 */
	private function formatLink($path)
	{
		$Title = Title::newFromText($path);
		return $this->safeString($Title->getPrefixedText());
	}

	// say no to OOP!
	private function printResultRow($title = '', $displayValue = '', $value = '', $displayClass = '', $id = '', $fileId = '')
	{
		return sprintf("%s\t%s\t%s\t%s\t%s\t%s\n", $value, $displayClass, $displayValue, $id, $title, $fileId);
	}

	private function highlightText($subject, $text, $highlightClass)
	{
		// add the highlighting tags to the string
		$pos = stripos($subject, $text);
		if ($pos === false)
		{
			return $subject;
		}

		$length = strlen($text);

		$highlighted = substr($subject, 0, $pos).
					   '<span class="'. $highlightClass .'">'.
					   substr($subject, $pos, $length).
					   '</span>'.
					   substr($subject, $pos+$length);

		return $highlighted;
	}


	/*
	 * Start link navigate functions
	 */
	private function formatNavigate($pageId = null, $fileId = null)
	{
		global $wgDekiPlug;

		$this->contentType = isset($_GET['as']) ? 'text/plain' : 'application/json';
		
		if (!is_null($fileId))
		{
			// figure out the page that this file is located on
			$filePageId = $this->getFilePageId($fileId);
			if (!is_null($filePageId))
			{
				$pageId = $filePageId;
			}
			else
			{
				$fileId = null;
			}
		}

		// get the desired page info & children
		$pageDetails = $this->getPageDetails($pageId);
		$this->generateJson($pageDetails, $pageId, $fileId);
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
	private function getPageDetails($pageId = null)
	{
		global $wgDekiPlug, $wgSitename, $wgLang;

		$parentId = null;
		
		if (is_null($pageId) || ($pageId == 'root'))
		{
			// root display
			$subpages = array();
			$files = array();

			// TODO: localize namespaces
			// root page
			$subpages[] = array('parentId' => null,
								'id' => 'home',
								'title' => $wgSitename,
								'path' => '/',
								'namespace' => NS_MAIN,
								'encodedTitle' => '/',
								'terminal' => false
								);
			// user page
			$subpages[] = array('parentId' => null,
								'id' => 'user',
								'title' => wfMsg('Page.ListUsers.page-title'),
								'path' => 'User:',
								'namespace' => NS_USER,
								'encodedTitle' => 'User:',
								'terminal' => false
								);
			// template page
			$subpages[] = array('parentId' => null,
								'id' => 'template',
								'title' => wfMsg('Page.ListTemplates.page-title'),
								'path' => 'Template:',
								'namespace' => NS_TEMPLATE,
								'encodedTitle' => 'Template:',
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
					$username = isset($_GET['name']) ? trim($_GET['name']) : null;
					$result = $wgDekiPlug->At('pages', '='.rawurlencode(rawurlencode($wgLang->getNsText(NS_USER) .':'. $username)))->Get();
					break;
				case 'template':
					$result = $wgDekiPlug->At('pages', '='.rawurlencode(rawurlencode($wgLang->getNsText(NS_TEMPLATE) . ':')))->Get();
					break;

				case 'home':
				default:
					$result = $wgDekiPlug->At('pages', $pageId)->Get();
			}

			if ($result['status'] == Plug::HTTPNOTFOUND)
			{
				// TODO: Localize or remove this error state and load root?
				$subpages = array();
				$subpages[] = array('parentId' => null,
									'id' => null,
									'title' => 'Page Not Found',
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

			// get the real pageId incase a namespace was specified
			$pageId = isset($page['@id']) ? $page['@id'] : $pageId;
			// get the page's parent id, null if no parent
			$parentId = $this->getParentId($result);

			// process the info
			list($subpages, $files) = $this->getPageChildren($pageId);

			return array('parentId' => $parentId,
						 'id' => $pageId,
						 'title' => $page['title'],
						 'path' => $page['path'],

						 'subpages' => $subpages,
						 'files' => $files
						);
			// left off optimizing json, how to get parent path & id?
		}
	}
	
	private function getParentId(&$result)
	{
		global $wgLang;

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

				$titleParent = '';
				$titleName = '';

				$Title = Title::newFromText($path);
				$parentPath = $Title->getParentPath();  
				$encodedTitle = $Title->getPathlessText();

				$subpages[] = array('parentId' => $pageId,
									'id' => $childPageDetails['@id'],
									'title' => $childPageDetails['title'],
									'encodedTitle' => $encodedTitle,
									'path' => $path,
									'namespace' => $Title->getNamespace(),
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
				$filePath = sprintf('%s/%s/=%s', $basePath, $childFileDetails['@id'], $childFileDetails['filename']);
				$encodedFilePath = sprintf('%s/%s/=%s', $basePath, $childFileDetails['@id'], rawurlencode(rawurlencode($childFileDetails['filename'])));
				
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

	function safeString($s)
	{
		return wfEscapeString($s);
	}

	private function generateJson(&$pageDetails, $selectedPageId, $selectedFileId)
	{
		$pageId = isset($pageDetails['id']) ? $pageDetails['id'] : null;
		$parentId = isset($pageDetails['parentId']) ? $pageDetails['parentId'] : null;
		$parentPath = !empty($pageDetails['path']) ? $pageDetails['path'] : '/'; // attach trailing slash


		$json = '';
		// generate the json for columnav
		$json .= '{ ';
		if (!is_null($parentId))
		{
			// add a special deki header
			$json .= sprintf('"header": { "parentId":"%s", "parentPath":"%s", "pageId":"%s" },',
							 $parentId, $this->formatLink($parentPath), $pageId);
		}

		$json .= '"ul": { "li": [ ';

		foreach ($pageDetails['subpages'] as $subpage)
		{
			$disabled = false;

			// page
			$path = $this->formatLink($subpage['path']);
			$class = 'page';

			if ($subpage['id'] == $selectedPageId)
			{
				$class .= ' columnav-active';
			}
			
			$title = $this->safeString($subpage['title']);


			$json .= '{';

			$json .= sprintf('"a": {"#text": "%s", "@path": "%s", "@pid": "%s", "@href": "", "@class": "%s"',
								$title, $this->safeString($subpage['path']), $subpage['id'], $class);
			if ($subpage['terminal'] || $disabled)
			{
				// page does not have children
				// actions for empty page object
			}
			else
			{
				// page has children
				// actions for ajax page objects
				$json .=',"@rel": "ajax"';
			}
			$json .= '}';// close the anchor
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

?>
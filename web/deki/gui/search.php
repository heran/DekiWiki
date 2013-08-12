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
 * SearchFormatter, search for js!
 */
class DekiSearchFormatter extends DekiFormatter
{
	const DEFAULT_LIMIT = 6;
	const HIGHLIGHT_CLASS = 'deki-ui-highlight';

	protected $contentType = 'application/json';
	//protected $requireXmlHttpRequest = true;

	public function format()
	{
		$Request = DekiRequest::getInstance();

		// $searchQuery - required param
		$searchQuery = $Request->getVal('query', null);
		// $type? - allows querying specific item types
		$type = $Request->getVal('type', null);
		// allow custom constraints
		$constraint = $Request->getVal('constraint', null);
		// $limit? - determine the number of results to retrieve (max: 20)
		$limit = $Request->getInt('limit', null);
		// custom search highlighting
		$highlightClass = $Request->getVal('highlight', self::HIGHLIGHT_CLASS);
		
		// validate the incoming data
		$searchQuery = str_replace(array(':'), array('\:'), $searchQuery);

		$constraints = array();
		if ($constraint)
		{
			$constraints[] = $constraint;
		}
		$parser = 'term';

		// added for the image dialog to restrict the search results
		switch ($type)
		{
			case 'document':
			case 'image':
				$parser = 'filename';
			case 'wiki':
				$constraints[] = sprintf('type: "%s"', str_replace('"', '\\"', $type));
				break;
			default: 
		}
		
		// don't include comments
		// @note negative constraints don't work, see bug#7217
		//$constraints[] = sprintf('-type:"comment"', str_replace('"', '\\"', $type));
		
		if (is_null($limit) || $limit < 1)
		{
			$limit = self::DEFAULT_LIMIT;
		}

		$Plug = DekiPlug::getInstance();
		$Plug = $Plug->At('site', 'opensearch')
			->With('q', $searchQuery)
			->With('limit', $limit)
			->With('parser', $parser);
		if (!empty($constraints))
		{
			$Plug = $Plug->With('constraint', implode(' AND ', $constraints));
		}
		$Result = $Plug->Get();

		if (!$Result->isSuccess())
		{
			echo json_encode(array('success' => false, 'status' => $Result->getStatus(), 'message' => $Result->getError()));
			return;
		}
		
		$entries = $Result->getAll('body/feed/entry', array());
		
		$json = $this->generateJsonArray($entries, $searchQuery, $highlightClass);
		
		echo json_encode(array('success' => true, 'body' => &$json));
	}
	
	protected function generateJsonArray(&$entries, $searchQuery = '', $highlightClass = null)
	{
		$json = array();

		// used for generating file urls
		// TODO: change how this is generated, use href from API
		$basePath = '';
		global $wgApi, $wgDeki;
		if (!empty($wgApi))
		{
			$basePath .= '/'. $wgApi;
		}
		$basePath .= '/'. $wgDeki . '/files';

		foreach ($entries as $entry)
		{
			if (isset($entry['dekilucene:id.file']))
			{					
				// file
				$filename = $entry['title']['#text'];
				$filePath = $entry['link']['@href'];
				$type = strtolower(pathinfo($filename, PATHINFO_EXTENSION));
				
				$parentPageId = isset($entry['dekilucene:page.parent']) ? $entry['dekilucene:page.parent']['@id'] : null;
				$fileId = $entry['dekilucene:id.file'];
				// add the highlighting tags to the string
				$displayFilename = $this->highlightText($filename, $searchQuery, $highlightClass);
	
				// generate a relative file path					
				//Bugfix #5515: Inserted images containing spaces in their name are broken when running Deki on Windows
				$encodedName = rawurlencode(rawurlencode($filename));
				// set the file path
				$filePath = sprintf('%s/%s/=%s', $basePath, $fileId, $encodedName);
				
				
				$json[] = array(
					'search_type' => 'file',
					'search_class' => $type,
					'search_highlight' => $displayFilename,
					'search_title' => $filename,
					'search_path' => $filePath,

					'page_id' => $parentPageId, // sent for symmetry
					'page_parent_id' => $parentPageId,
				
					// response differes from page here
					'file_id' => $fileId
				);	
			}
			else
			{
				// is this a comment?
				if (isset($entry['dekilucene:id.comment']))
				{
					$pageId = $entry['dekilucene:page.parent']['@id'];
				}
				else
				{
					// page
					$pageId = $entry['dekilucene:id.page'];
				}

				$pageTitle = $entry['title']['#text'];
				// if the path is empty then homepage
				$pagePath = empty($entry['dekilucene:path']) ? '/' : $entry['dekilucene:path'];
				$parentPageId = isset($entry['dekilucene:page.parent']) ? $entry['dekilucene:page.parent']['@id'] : null;
		
				// add the highlighting tags to the string
				$displayTitle = self::highlightText($pageTitle, $searchQuery, $highlightClass);
				
				// can't use periods since YUI interprets them in key names
				$json[] = array(
					'search_type' => 'page',
					'search_class' => 'txt',
					'search_highlight' => $displayTitle,
					'search_title' => $pageTitle,
					'search_path' => $pagePath,

					'page_id' => $pageId,
					'page_parent_id' => $parentPageId,
				);
			}
		}

		return $json;
	}

	protected static function highlightText($subject, $text, $highlightClass = null)
	{
		if (empty($highlightClass))
		{
			return $subject;
		}

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
}

new DekiSearchFormatter();

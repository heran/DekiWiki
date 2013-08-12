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

class LinkSearch extends DekiFormatter
{
	const RESULT_LIMIT = 6;
	const HIGHLIGHT_CLASS = 'search-highlight';

	protected $contentType = 'text/plain';
	//protected $requireXmlHttpRequest = true;
	
	public function format()
	{
		global $wgDekiPlug, $wgApi, $wgDeki;
		//$Plug = DekiPlug::getInstance();
		$Request = DekiRequest::getInstance();
		$this->body = '';

		$query = $Request->getVal('query');
		if ($query == '')
		{
			return;
		}

		// @note (guerric) using the path returns weird result sets
		$searchString = $query;

		// added for the image dialog to restrict the search results
		$type = $Request->getVal('type');
		$constraint = '';
		if ($type)
		{
			switch ($_GET['type'])
			{
				case 'wiki':
				case 'document':
				case 'image':
					$constraint = sprintf('type:"%s"', $_GET['type']);
					break;
				default: 
			}
		}

		// @note (guerric) didn't use with('format', 'search') because it mixes pages and files together
		$Plug = DekiPlug::getInstance();
		$Result = $Plug->At('site', 'opensearch')->With('q', $searchString)->With('constraint', $constraint)->With('limit', self::RESULT_LIMIT)->Get();
		$entries = $Result->getAll('body/feed/entry', array());

		$this->body = '';

		if ($Result->isSuccess())
		{
			
			// used for generating file urls
			// TODO: change how this is generated, use href from API
			$basePath = '';
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
					
					$pageId = isset($entry['dekilucene:page.parent']) ? $entry['dekilucene:page.parent']['@id'] : null;
					$fileId = $entry['dekilucene:id.file'];
					// add the highlighting tags to the string
					$displayFilename = $this->highlightText($filename, $query, self::HIGHLIGHT_CLASS);

					// generate a relative file path					
					//Bugfix #5515: Inserted images containing spaces in their name are broken when running Deki on Windows
					$encodedName = rawurlencode(rawurlencode($filename));
					// set the file path
					$filePath = sprintf('%s/%s/=%s', $basePath, $fileId, $encodedName);
					
					$this->body .= $this->printResultRow($filename, $displayFilename, $filePath, 'mt-ext-'.$type, $pageId, $file['@id']);
				}
				else
				{
					// page
					$pageId = $entry['dekilucene:id.page'];
					$pageTitle = $entry['title']['#text'];
					$pagePath = $entry['dekilucene:path']; //empty($page['path']) ? '/' : $this->formatLink($page['path']);
	
					$displayClass = 'mt-ext-txt';
	
					// add the highlighting tags to the string
					$displayTitle = $this->highlightText($pageTitle, $query, self::HIGHLIGHT_CLASS);
	
					$this->body .= $this->printResultRow($pageTitle, $displayTitle, $pagePath, $displayClass, $pageId);					
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
			//$this->body = '<no results>';
		}

		echo $this->body;
	}


	/*
	 * Removes underscores for links
	 */
	private function formatLink($path)
	{
		$Title = Title::newFromText($path);
		return $Title->getPrefixedText();
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
}

// start searching!
new LinkSearch();

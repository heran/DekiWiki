<?php
/*
 * MindTouch Deki - enterprise collaboration and integration platform
 * Copyright (C) 2006-2008 MindTouch, Inc.
 * www.mindtouch.com  oss@mindtouch.com
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
define('DEKI_MOBILE', true); 
require_once('index.php');

class Search extends DekiController
{ 
	const SEARCH_PARAM = 'searchterm';
	const PAGE_PARAM = 'page';

	// needed to determine the template folder
	protected $name = 'search';

	// search variables
	protected $resultsPerPage = 5;  // # results to display per page 
	protected $User;                // user data


	public function index()
	{
		$searchTerm = $this->Request->getVal(self::SEARCH_PARAM);
		$pageNum = $this->Request->getInt(self::PAGE_PARAM, 0);
		
		$this->View->set('head.title', 'MindTouch Deki Mobile | Search');
		$this->View->set('form.action', $this->getUrl());
		$this->View->set('form.search.name', self::SEARCH_PARAM);

		$currentUrl = $this->getUrl('index', array(self::SEARCH_PARAM => $searchTerm, self::PAGE_PARAM => $pageNum));
		// required for the template
		$this->View->set('pageUrl', $currentUrl);

		if (!empty($searchTerm))
		{
			$this->View->set('head.title', 'MindTouch Deki Mobile | Search Results');
			$this->View->set('form.search', $searchTerm);

			$results = $this->getSearchResults($searchTerm, $pageNum, $this->resultsPerPage);
			
			$moreResults = false;
			$numResults = count($results);
			if ($numResults > $this->resultsPerPage)
			{
				// there are more results, remove the last result
				array_pop($results);
				$numResults--;
				$moreResults = true;
			}

			$this->setViewSearchResults($results, $searchTerm);
			$this->View->set('search.pagination', $this->buildPagination($pageNum, $numResults, $searchTerm, $moreResults));
		}
		
		$this->View->output();
	}
	
	/**
	 * @return array - contains search results
	 */
	protected function getSearchResults($searchTerm, $pageNum = 0, $limit = 5)
	{
		$searchQuery = $searchTerm; // create a copy to hide what the actual lucene search is
		if (strncmp('tag:', $searchQuery, 4) == 0)
		{
			$tag = trim(substr($searchQuery, 4));
			$searchQuery = sprintf('tag:"%s"', addslashes($tag));
		}

		// setup plug to get some search results
		$Plug = $this->Plug->At('site', 'search')->With('format', 'search')->With('q', $searchQuery);

		// handle paging
		$offset = $pageNum * $this->resultsPerPage;
		$Plug = $Plug->With('limit', $this->resultsPerPage+1)->With('offset', $offset);
		// go go api
		$Result = $Plug->Get();

		$searchResults = $Result->getAll('body/search/result');
		if (is_null($searchResults))
		{
			$searchResults = array();
		}

		return $searchResults;
	}

	protected function setViewSearchResults(&$searchResults, $searchTerm)
	{
		$viewResults = array();
		for ($i = 0, $i_m = count($searchResults); $i < $i_m; $i++)
		{
			$pageUrl = $this->Request->getLocalUrl('page', null, array('idnum' => $searchResults[$i]['id.page']));
			$title = $this->findKeywords($searchResults[$i]['title'], $searchTerm);
			$preview = $this->findKeywords($searchResults[$i]['preview'], $searchTerm);

			$viewResults[] = array(
				'url' => $pageUrl,
				'title' => $title,
				'preview' => $preview,
				'class' => ($i+1 == $i_m) ? 'last' : ''
			);
		}

		$this->View->setRef('search.results', $viewResults);
	}


	protected function buildPagination($pageNum, $numResults, $searchTerm, $moreResults = false)
	{
		if ($pageNum <= 0)
		{
			$pageNum = 0;
			$prev = '<div class="left disabled"><a href="#"></a></div>';
		}
		else
		{
			$url = $this->getUrl(null, array(self::SEARCH_PARAM => $searchTerm, self::PAGE_PARAM => ($pageNum - 1)), true);
			$prev = '<div class="left"><a href="'. $url .'"></a></div>';
		}

		if (!$moreResults)
		{
			$next = '<div class="right disabled"><a href="#"></a></div>';
		}
		else
		{
			$url = $this->getUrl(null, array(self::SEARCH_PARAM => $searchTerm, self::PAGE_PARAM => ($pageNum + 1)));
			$next = '<div class="right"><a href="'. $url .'"></a></div>';
		}

		$start = $pageNum * $this->resultsPerPage + 1;
		$end = $start + $numResults - 1;
		$info = '<div class="num">' . 'Viewing Results' . ' <strong>' . $start . '-' . $end . '</strong></div>';

		$html = '<div class="pagination">';
		$html .=  $next . $prev. $info;
		$html .= '</div>';

		return $html;
	}

	protected function findKeywords($sentence, $keyword)
	{
		$words = explode(' ', $sentence);
		$keylen = strlen($keyword);
		foreach ($words as $key => &$word)
		{
			// need to remove garbage chars from windows pasting -> utf
			$word = str_replace(array("\r", "\n", "\t", chr(160), chr(190), chr(194)), ' ', $word);

			// collapse multiple spaces
			$word = preg_replace('/\s+/', ' ', $word);

			if (empty($word))
			{
				unset($words[$key]);
				continue;
			}

			if (stripos($word, $keyword) !== false)
			{
				$word = '<span class="keyword">'. htmlspecialchars($word) .'</span>';
			}
			else
			{
				$word = htmlspecialchars($word);
			}
		}
		unset($word);

		return implode(' ', $words);
	}
}

new Search();

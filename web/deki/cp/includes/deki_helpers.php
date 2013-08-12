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

require_once 'libraries/dom_pagination.php';

/**
 * Class builds a DomTable for simplified API listing
 *
 * @usage construct(), getResults(), saveHtml()
 */
class DekiTable extends DomTable
{
	private $controllerName = '';
	private $controllerAction = '';

	private $pagingRequestKey = null;
	private $resultsPerPage = 15;
	private $currentPage = 0;
	private $totalPages = 0;

	private $currentSort = null;
	private $currentSortField = null;

	private $searchFields = array();
	private $currentSearch = null;


	/*
	 * array of allowed fields to sort by
	 */
	private $sortFields = array();


	public function __construct($controllerName, $controllerAction, $sortFields = array(), $pagingKey = null)
	{
		parent::__construct();

		// required for building the sort url's
		$this->controllerName = $controllerName;
		$this->controllerAction = $controllerAction;
		$this->sortFields = $sortFields;
		// determines the key to use
		$this->pagingRequestKey = !is_null($pagingKey) ? $pagingKey : 'page';

		$this->setGetParams();
	}
	

	/**
	 * Sets up a search field for the request if $_GET['query'] isset
	 * @param mixed $field - can be a string or an array
	 */
	public function setSearchField($field)
	{
		if (!is_array($field))
		{
			$field = array($field);
		}
		
		$this->searchFields = !empty($field) ? $field : array();
	}
	public function setResultsPerPage($count)
	{
		if ($count > 0)
		{
			$this->resultsPerPage = $count;
		}
	}
	public function setDefaultSort($field, $sort = SORT_ASC)
	{
		if (is_null($this->currentSortField))
		{
			$this->currentSortField = $field;
			$this->currentSort = $sort;
		}
	}

	// overloading parent DomTable functionality
	public function addSortHeading($title, $field, $colspan = 1)
	{
		$Request = DekiRequest::getInstance();

		$sortMethod = '';
		$sortClass = 'sort';
		if ($this->currentSortField == $field)
		{
			// update the sort method if we are sorting by this field already
			if ($this->currentSort == SORT_ASC)
			{
				$sortMethod = '-';
				$sortClass .= ' desc';
			}
			else
			{
				$sortClass .= ' asc';
			}
		}
		
		// generate a link with class sort-asc or sort-desc
		$get = array(
			// dump the current page //'page' => $this->currentPage,
			'sortby' => $sortMethod . $field,
			'query' => $this->currentSearch
		);
		$href = $Request->getLocalUrl($this->controllerName, $this->controllerAction, $get);

		$html = '<a href="'. $href .'" class="'. $sortClass .'"><span/>' . $title . '</a>';

		return parent::addHeading($html, $colspan);
	}

	/**
	 * @param DekiPlug $Plug - a plug object setup for the require api call without sort filters or paging
	 * @param string $queryKey - the xpath key to obtain the resulting query count, i.e. /body/users/@querycount
	 * @return DekiResult
	 */
	public function getResults($Plug, $queryKey)
	{
		// handle searching
		if (!empty($this->searchFields) && !is_null($this->currentSearch))
		{
			foreach ($this->searchFields as $field)
			{
				$Plug = $Plug->With($field, $this->currentSearch);
			}
		}
		
		// handle sorting
		if (!is_null($this->currentSortField))
		{
			$sortMethod = $this->currentSort == SORT_DESC ? '-' : '';
			$Plug = $Plug->With('sortby', $sortMethod . $this->currentSortField);
		}

		// handle paging
		$offset = ($this->currentPage - 1) * $this->resultsPerPage;
		$Plug = $Plug->With('offset', $offset)->With('limit', $this->resultsPerPage);

		// go go API
		$Result = $Plug->Get();

		// more paging goodness
		$querycount = $Result->getVal($queryKey);
		$this->totalPages = ceil($querycount / $this->resultsPerPage);
		// limit to the last page of results due to hacked up/old url
		if ($this->totalPages <= 1)
		{
			$this->totalPages = 1;
			$this->currentPage = 1;
		}
		else if ($this->currentPage > $this->totalPages)
		{
			// TODO: have a notification/redirect?
			$this->currentPage = $this->totalPages;
		}

		return $Result;
	}

	public function saveHtml()
	{
		$Request = DekiRequest::getInstance();

		// create the paging element
		$get = array();
		foreach ($_GET as $key => $value)
		{
			if ($key != $this->pagingRequestKey)
			{
				$get[$key] = $value;
			}
		}
		
		$baseHref = $Request->getLocalUrl($this->controllerName, $this->controllerAction, $get);
		$Pagination = new DomListingPagination(
			$baseHref,
			$this->totalPages,
			$this->pagingRequestKey,
			Config::RESULTS_PAGING_COUNT
		);

		return '<div class="deki-table">'.parent::saveHtml() .'</div>' . $Pagination->saveHtml();
	}

	public function getCurrentSearch() { return $this->currentSearch; }


	/*
	 * Load the current params from GET
	 */
	private function setGetParams()
	{
		$Request = DekiRequest::getInstance();

		// searching
		$this->currentSearch = $Request->getVal('query');

		// sorting		
		// use API style for sorting to reduce number of GET variables
		// e.g. -username => sort username desc, date => sort date asc
		$this->currentSortField = $Request->getVal('sortby');
		if (!empty($this->currentSortField))
		{
			if (strncmp($this->currentSortField, '-', 1) == 0)
			{
				// currently sorting desc
				$this->currentSort = SORT_DESC;
				$this->currentSortField = substr($this->currentSortField, 1);
			}
			else
			{
				$this->currentSort = SORT_ASC;
			}
		}

		// make sure the sortby field is allowed
		if (!in_array($this->currentSortField, $this->sortFields))
		{
			$this->currentSort = null;
			$this->currentSortField = null;
		}

		// paging
		$this->currentPage = $Request->getVal($this->pagingRequestKey, 1);
		if ($this->currentPage < 1)
		{
			$this->currentPage = 1;
		}
	}
}
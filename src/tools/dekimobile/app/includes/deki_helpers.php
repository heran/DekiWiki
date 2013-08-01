<?php
/*
 * MindTouch Deki - enterprise collaboration and integration platform
 * Copyright (C) 2006 MindTouch, Inc.
 * www.mindtouch.com  oss@mindtouch.com
 *
 * For community documentation and downloads visit www.opengarden.org;
 *  please review the licensing section.
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
 * Class builds a DomTable for simplified API listing
 *
 * @usage construct(), getResults(), saveHtml()
 */
class DekiTable extends DomTable
{
	private $controllerName = '';
	private $controllerAction = '';

	private $resultsPerPage = 15;
	private $currentPage = 0;
	private $totalPages = 0;

	private $currentSort = null;
	private $currentSortField = null;

	private $searchField = null;
	private $currentSearch = null;


	/*
	 * array of allowed fields to sort by
	 */
	private $sortFields = array();


	public function __construct($controllerName, $controllerAction, $sortFields = array())
	{
		parent::__construct();

		// required for building the sort url's
		$this->controllerName = $controllerName;
		$this->controllerAction = $controllerAction;
		$this->sortFields = $sortFields;

		$this->setGetParams();
	}
	

	/**
	 * Sets up a search field for the request if $_GET['query'] isset
	 */
	public function setSearchField($field) { $this->searchField = !empty($field) ? $field : null; }
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
		if (!is_null($this->searchField) && !is_null($this->currentSearch))
		{
			$Plug = $Plug->With($this->searchField, $this->currentSearch);
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
			if ($key != 'page')
			{
				$get[$key] = $value;
			}
		}
		
		$baseHref = $Request->getLocalUrl($this->controllerName, $this->controllerAction, $get) . '&page=';
		$Pagination = new DomPagination($baseHref, $this->currentPage, $this->totalPages);

		return parent::saveHtml() . $Pagination->saveHtml();
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
		$this->currentPage = $Request->getVal('page', 1);
		if ($this->currentPage < 1)
		{
			$this->currentPage = 1;
		}
	}
}

class DekiSite 
{	
	
	public static function getProduct() 
	{
		global $wgProductCompany, $wgProductUrl, $wgProductName, $wgProductVersion, $wgProductType;
		
		// smartly figure out the real version type
		$wgProductType = wfGetConfig('license/state/#text', 'COMMUNITY') == 'COMMUNITY' 
			? DekiView::msg('Product.type.community')
			: DekiView::msg('Product.type.commercial');
			
		return DekiView::msg('Product.Powered',
			DekiView::msg('Product.url', $wgProductUrl), 
			DekiView::msg('Product.company', $wgProductCompany),
			DekiView::msg('Product.name', $wgProductName),
			$wgProductType, 
			DekiView::msg('Product.version', $wgProductVersion)
		);
	}
	
	public static function getStatus() 
	{
		return wfGetConfig('license/state/#text', 'COMMUNITY');
	}
	
	public static function isCommunity() 
	{
		return in_array(DekiSite::getStatus(), array('COMMUNITY'));	
	}
	
	/**
	 * @param int $days - the number of days to check the license expiry
	 * @return bool
	 */
	public static function willExpire($days = 4) 
	{
		$expiry = wfGetConfig('license/expiration/#text', null);
		if (is_null($expiry)) 
		{
			return false;	
		}
		$timestamp = wfTimestamp(TS_UNIX, $expiry);
		$diff = $timestamp - mktime();
		if ($diff > ($days * 86400)) {
			return false;	
		}
		return ceil($diff / 86400);
	}
	
	public static function isTrial() 
	{
		return in_array(DekiSite::getStatus(), array('TRIAL'));	
	}
	public static function isEnterprise() 
	{
		return in_array(DekiSite::getStatus(), array('ENTERPRISE'));	
	}	
	public static function isInvalid() 
	{
		return in_array(DekiSite::getStatus(), array('INVALID'));	
	}
	public static function isInactive() 
	{
		return in_array(DekiSite::getStatus(), array('INACTIVE'));	
	}
	public static function isExpired() 
	{
		return in_array(DekiSite::getStatus(), array('EXPIRED'));	
	}	
	public static function isDeactivated() 
	{
		return DekiSite::isInvalid() || DekiSite::isExpired() || DekiSite::isInactive();
	}
	public static function hasOldKey() 
	{
		return !is_null(wfGetConfig('site/activation', null));
	}
	
	public static function getLicense() 
	{
		global $wgAdminPlug, $wgDekiApiKey;
		return $wgAdminPlug->At('license')->With('apikey', $wgDekiApiKey)->Get();
	}
	
	public static function getLicenseText($return = '') 
	{
		global $wgDekiApiKey, $wgDekiApi, $wgDreamServer;
		
		$Plug = new DekiPlug($wgDreamServer, null);
		$Plug = $Plug->At(array($wgDekiApi));
		$Data = $Plug->At('license')->With('apikey', $wgDekiApiKey)->Get();
		$body = $Data->getVal('body');
		$license = '';
		if (preg_match("/<support-agreement type=\"xhtml\">(.*)<\/support-agreement>/is", $body, $matches)) 
		{
			$license = $matches[1];	
		}
		if (preg_match("/<source-license type=\"xhtml\">(.*)<\/source-license>/is", $body, $matches)) 
		{
			$license.= $matches[1];	
		}
		return $license;
	}
}
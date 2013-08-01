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

abstract class DomPagination extends DomFragment
{
	protected $Request;
	protected $baseHref;
	
	protected $queryPrefix;
	
	protected $Paginator;
	
	public function __construct($baseHref = '')
	{
		$this->setRequest(DekiRequest::getInstance());		
		$this->setBaseHref($baseHref);
		
		$this->Paginator = $this->createElement('div');
		$this->Paginator->addClass('deki-pagination');
		
		$this->appendChild($this->Paginator);
		
		$this->createContainer();
	}
	
	public function setRequest(DekiRequest $Request)
	{
		$this->Request = $Request;
	}
	
	public function setBaseHref($baseHref)
	{
		if ( strlen($baseHref) == 0 && isset($_SERVER['REQUEST_URI']) )
		{
			$baseHref = $_SERVER['REQUEST_URI'];
		}
		
		$this->queryPrefix = strpos($baseHref, '?') === false ? '?' : '&';
		$this->baseHref = $baseHref;
	}
	
	abstract function createContainer();
	
	public function __toString()
	{
		return $this->saveHtml();
	}
}

class DomPagePagination extends DomPagination
{
	const PAGE = 'page';

	protected $totalPages;
	protected $currentPage;
	protected $pageKey;
	
	public function __construct($baseHref = '', $totalPages = 1, $pageKey = null)
	{
		// used to fetch the page information from the request
		$this->pageKey = is_null($pageKey) ? self::PAGE : $pageKey;
		
		$this->totalPages = (int) $totalPages;
		$this->setCurrentPage();
		
		parent::__construct($baseHref);
	}
	
	public function createContainer()
	{
		// create the previous navigation
		$Element = $this->Paginator->createElement('div');
		$Element->setAttribute('class', 'prev');

		$prevUrl = $this->getPrevUrl();
		$html = is_null($prevUrl) ? '<span>'.wfMsg('System.Common.nav-prev').'</span>' : sprintf('<a href="%s" class="prev">%s</a>', $prevUrl, wfMsg('System.Common.nav-prev'));
		$Element->innerHtml($html);
		$this->Paginator->appendChild($Element);
		
		// create the info section
		$Element = $this->Paginator->createElement('div', wfMsg('System.Common.nav-info', $this->getCurrentPage(), $this->totalPages));
		$Element->setAttribute('class', 'info');
		$this->Paginator->appendChild($Element);

		// create the next navigation
		$Element = $this->createElement('div');
		$Element->setAttribute('class', 'next');
		
		$nextUrl = $this->getNextUrl();
		$html = is_null($nextUrl) ? '<span>'.wfMsg('System.Common.nav-next').'</span>' : sprintf('<a href="%s" class="next">%s</a>', $nextUrl, wfMsg('System.Common.nav-next'));
		$Element->innerHtml($html);
		$this->Paginator->appendChild($Element);
	}
	
	// used for generating the paging urls
	public function getQueryPrefix()
	{
		return $this->queryPrefix . $this->pageKey . '=';
	}
	public function getTotalPages() { return $this->totalPages; }
	public function getCurrentPage() { return $this->currentPage; }
	
	public function getPageUrl($pageNumber)
	{
		return $this->baseHref . $this->getQueryPrefix() . $pageNumber;
	}
	public function getPrevUrl()
	{
		if ($this->currentPage <= 1)
		{
			return null;
		}
		
		$prev = $this->currentPage - 1;
		return $this->baseHref . $this->getQueryPrefix() . $prev;
	}
	public function getNextUrl()
	{
		if ($this->currentPage >= $this->totalPages)
		{
			return null;
		}
		
		$next = $this->currentPage + 1;
		return $this->baseHref . $this->getQueryPrefix() . $next;
	}
	
	public function setCurrentPage($currentPage = null)
	{
		if (is_null($currentPage))
		{
			$Request = DekiRequest::getInstance();
			// attempt to fetch from the Request
			$currentPage = $Request->getInt($this->pageKey, 1);
			
		}
		$this->currentPage = (int) $currentPage;
	}
}


class DomListingPagination extends DomPagePagination
{
	// determines how many pages to show in between prev/next
	// current page is in the center, make sure this is an odd #
	protected $displayCount = 5;

	
	public function __construct($baseHref = '', $totalPages = 1, $pageKey = null, $displayCount = 5)
	{
		if ($displayCount % 2 != 1)
		{
			throw new Exception('displayCount must be odd.');
		}
		$this->displayCount = $displayCount;
		parent::__construct($baseHref, $totalPages, $pageKey);
	}
	
	public function createContainer()
	{
		$this->Paginator->addClass('deki-listing-pagination');

		// create the previous navigation
		$Element = $this->Paginator->createElement('div');
		$Element->setAttribute('class', 'prev');
		
		$prevUrl = $this->getPrevUrl();
		if (is_null($prevUrl))
		{
			$html = sprintf(
				'<span class="first">%s</span> <span class="prev">%s</span>',
				wfMsg('System.Common.nav-first'), 
				wfMsg('System.Common.nav-prev')
			);
		}
		else
		{
			$firstUrl = $this->getPageUrl(1);
			$html = sprintf(
				'<a href="%s" class="first">%s</a> <a href="%s" class="prev">%s</a>',
			 	$firstUrl,
			 	wfMsg('System.Common.nav-first'),
			 	$prevUrl,
			 	wfMsg('System.Common.nav-prev')
			);
		}
		$Element->innerHtml($html);
		$this->Paginator->appendChild($Element);
		
		// create the info section
		$Element = $this->Paginator->createElement('div');
		$Element->setAttribute('class', 'pagelist');

		$page = $this->currentPage - (($this->displayCount - 1) / 2);
		if ($page > $this->totalPages - $this->displayCount)
		{
			$page = $this->totalPages - ($this->displayCount - 1);
		}
		
		// sanitize
		if ($page <= 1)
		{
			$page = 1;
		}
		// or add a class
		else if ($page > 1)
		{
			$Element->addClass('list-prev');
		}
		// compute the max page to display
		$pageMax = $page + $this->displayCount;
		// add another class?
		if ($pageMax <= $this->totalPages)
		{
			$Element->addClass('list-next');
		}
		
		// determines whether an item is the "last" element
		$totalCount = $this->totalPages < $pageMax ? $this->totalPages : $pageMax - 1;
		
		// wrap pagination in a list for easier styling
		$Ul = $Element->createElement('ol');
		for (; $page <= $this->totalPages && $page < $pageMax; $page++)
		{
			$Li = $Ul->createElement('li');
			$class = array();
			if ($page == $pageMax - $this->displayCount)
			{
				$class[] = 'first';	
			}
			if ($page == $totalCount) 
			{
				$class[] = 'last';
			}
			if (!empty($class)) 
			{
				$Li->setAttribute('class', implode(' ', $class));
			}
			$A = $Li->createElement('a', $page);
			$A->setAttribute('href', $this->getPageUrl($page));
			if ($page == $this->currentPage)
			{
				$A->addClass('selected');
			}
			$Li->appendChild($A);
			$Ul->appendChild($Li);
		}
		$Element->appendChild($Ul);
		$this->Paginator->appendChild($Element);

		
		// create the next navigation
		$Element = $this->createElement('div');
		$Element->setAttribute('class', 'next');
		
		$nextUrl = $this->getNextUrl();
		if (is_null($nextUrl))
		{
			$html = sprintf(
				'<span class="next">%s</span> <span class="last">%s</span>',
				wfMsg('System.Common.nav-next'), 
				wfMsg('System.Common.nav-last')
			);
		}
		else
		{
			$lastUrl = $this->getPageUrl($this->totalPages);
			$html = sprintf(
				'<a href="%s" class="next">%s</a> <a href="%s" class="last">%s</a>',
			 	$nextUrl,
			 	wfMsg('System.Common.nav-next'),
			 	$lastUrl,
			 	wfMsg('System.Common.nav-last')
			);
		}
		$Element->innerHtml($html);
		$this->Paginator->appendChild($Element);
		
		// add a class if we only have one result page
		if ($this->getTotalPages() <= 1) 
		{
			$this->Paginator->addClass('deki-pagination-single'); 
		}
	}
}


class DomOffsetPagination extends DomPagination
{
	const LIMIT = 'limit';
	const OFFSET = 'offset';
	
	protected $limit;
    protected $offset;

	protected $itemsPerPage;
	protected $isLastPage;
	
	public function __construct($baseHref = '', $itemsPerPage = 100, $isLastPage = true)
	{
		$this->itemsPerPage = (int) $itemsPerPage;
		$this->isLastPage = (bool) $isLastPage;

		parent::__construct($baseHref);
	}
		
	public function createContainer()
	{
		$offset = $this->Request->getInt(self::OFFSET, 0);
		$this->setOffset($offset);
		
		$limit = $this->Request->getInt(self::LIMIT, $this->itemsPerPage);
		$this->setLimit($limit);
		
		$Element = $this->Paginator->createElement('div');
		$Element->setAttribute('class', 'prev');
		
		$query = $this->queryPrefix . self::LIMIT . '=' . $limit . '&' . self::OFFSET . '=';
		
		$prev = wfMsg('Article.Common.previous', $limit);
		
		$offset = $this->Request->getInt(self::OFFSET, 0);
		if ( $offset != 0 )
		{
			$prevOffset = $offset - $limit;
			
			$PrevLink = new DwDomElement('a');
			
			if ( $prevOffset <= 0 )
			{
				$prevOffset = 0;
				$PrevLink->setAttribute('href', $this->baseHref);
			}
			else
			{
				$PrevLink->setAttribute('href', $this->baseHref . $query . $prevOffset);
			}
			$PrevLink->setAttribute('class', 'prev');
			$PrevLink->innerHtml($prev);
			
			$Element->innerHtml($PrevLink->saveHtml());
		}
		else
		{
			$Element->innerHtml('<span>'.$prev.'</span>');
		}
		
		$this->Paginator->appendChild($Element);

		$Element = $this->Paginator->createElement('div', wfMsg('Article.Common.showing-results', $limit, $offset + 1));
		$Element->setAttribute('class', 'info');
		$this->Paginator->appendChild($Element);

		$Element = $this->Paginator->createElement('div');
		$Element->setAttribute('class', 'next');
		
		$nextOffset = $limit + $offset;
		$next = wfMsg('Article.Common.next', $limit);
		
		if ( !$this->isLastPage )
		{
			$NextLink = new DwDomElement('a');
			$NextLink->setAttribute('href', $this->baseHref . $query . $nextOffset);
			$NextLink->setAttribute('class', 'next');
			$NextLink->innerHtml($next);
			
			$Element->innerHtml($NextLink->saveHtml());
		}
		else
		{
			$Element->innerHtml('<span>'.$next.'</span>');
		}
		
		$this->Paginator->appendChild($Element);
	}
	
	public function getLimit()
	{
		return $this->limit;
	}
	
	public function setLimit($limit)
	{
        $this->limit = (int) $limit;
	}
	
	public function getOffset()
	{
		return $this->offset;
	}
	
	public function setOffset($offset)
	{
		$this->offset = (int) $offset;
	}
}

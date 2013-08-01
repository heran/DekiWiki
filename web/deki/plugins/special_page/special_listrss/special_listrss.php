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

if (defined('MINDTOUCH_DEKI'))
{
	DekiPlugin::registerHook(Hooks::SPECIAL_LIST_RSS, 'wfSpecialListRss');
	DekiPlugin::registerHook(Hooks::DATA_GET_SPECIAL_PAGES, array('SpecialListRss', 'getSpecialPagesHook'));
}

function wfSpecialListRss($pageName, &$pageTitle, &$html)
{
	$SpecialListRss = new SpecialListRss($pageName, basename(__FILE__, '.php'));
	// set the page title
	$pageTitle = $SpecialListRss->getPageTitle();
	$html = $SpecialListRss->output();
}

class SpecialListRss extends SpecialPagePlugin
{
	protected $pageName = 'ListRss';
	// in order for this page to function users need these perms
	protected $requiredOperations = array('BROWSE', 'READ');
	
	protected $paginate = true;
	
	protected $Title;
	protected $Request;
	
	protected $itemsPerPage = 50;
	protected $defaultSortField = 'username';
	protected $defaultSort = DomSortTable::SORT_ASC;	

	
	public static function getSpecialPagesHook(&$pages)
	{
		$Special = new self();
		$name = $Special->getPageTitle();
		$href = $Special->getTitle()->getFullURL();
		
		$pages[$name] = array('name' => $name, 'href' => $href);
	}

	public function output()
	{
		// enforce options
		if (!$this->checkUserAccess())
		{
			return '';
		}
				
		$this->includeSpecialCss('special_listrss.css');
		
		$html = '';
		
		$this->Request = DekiRequest::getInstance();
		$this->Title = Title::newFromText($this->requestedName, NS_SPECIAL);
		
		$html .= $this->getGeneralFeeds();
		$html .= $this->getTableHtml();
		
		return $html;
	}
	
	protected function getGeneralFeeds()
	{
		global $wgUser;
		
		$html = '';
		
		$Skin = $wgUser->getSkin( );
		
		$html .= '<h2>'. wfMsg('Page.ListRss.header-general-feeds') .'</h2>';
		
		$html .= '<ul>';
			$html .= '<li>';
				$html .= '<a class="iconitext" href="' . $Skin->makeUrl( 'Special:Recentchanges', 'feed=rss' ) . '">';
				$html .= Skin::iconify('listrss') . '<span class="text">' . wfMsg('Page.ListRss.whats-new-feed') . '</span></a>';
			$html .= '</li>';
				
		if( !$wgUser->isAnonymous() )
		{
			$html .= '<li>';
				$html .= '<a class="iconitext" href="' . $Skin->makeUrl('Special:Watchlist', 'feed=rss&target=' . urlencode($wgUser->getUsername())) . '">';
				$html .= Skin::iconify('listrss') . '<span class="text">' . wfMsg('Page.ListRss.my-watchlist-feed').'</span></a>';
			$html .= '</li>';
		}

		$html .= '</ul>';
				
		return $html;				
	}
	
	protected function getData()
	{
		$currentPage = $this->Request->getInt('page', 1);
		
		$field = $this->Request->getVal('sortby', $this->defaultSortField);
		$method = $this->Request->getVal('sort', $this->defaultSort);
		
		if ( $method == DomSortTable::SORT_DESC )
		{
			$field = '-' . $field;
		}
		
		return DekiUser::getSiteList(array('activatedfilter' => 'true', 'sortby' => $field), $currentPage, $this->itemsPerPage);	
	}
	
	protected function getTotalCount()
	{
		return DekiUser::getSiteCount();
	}
	
	protected function getTableHtml()
	{
		$field = $this->Request->getVal('sortby', $this->defaultSortField);
		$method = $this->Request->getVal('sort', $this->defaultSort);

		$Table = new DomSortTable($this->Title->getLocalURL('-'), $field, $method);
		
		$Table->addRow(false);
		$Table->addSortHeading(wfMsg('Page.ListRss.header-user-name'), 'username');
		$Table->addHeading(wfMsg('Page.ListRss.header-contributions'));
		$Table->addHeading(wfMsg('Page.ListRss.header-watchlist'));
		
		$users = $this->getData();
		$this->formatTableRows($Table, $users);
		
		$paginator = $this->getPaginatorHtml($field, $method);
		$html = '<h2>'. wfMsg('Page.ListRss.header-user-feeds') .'</h2>';
		$html .= $paginator . $Table->saveHtml() . $paginator;
		return $html; 
	}
	
	protected function getPaginatorHtml($sortBy, $sortOrder)
	{
		$html = '';
		$sortBy = htmlspecialchars($sortBy);
		$sortOrder = htmlspecialchars($sortOrder);
		
		if ( $this->paginate )
		{
			$totalPages = ceil($this->getTotalCount() / $this->itemsPerPage);
			
			if ( $sortBy == $this->defaultSortField && $sortOrder == $this->defaultSort )
			{
				$query = '';
			}
			else
			{
				$query = 'sortby=' . $sortBy . '&sort=' . $sortOrder;
			}
			
			$Paginator = new DomPagePagination($this->Title->getLocalURL($query), $totalPages);
			$html = $Paginator->saveHtml();
		}
		
		return $html;
	}
	
	protected function formatTableRows($Table, $users)
	{
		global $wgUser;
		
		$Skin = $wgUser->getSkin();
		
		foreach ($users as $User)
		{
			if ( $User->isAnonymous() )
			{
				continue;
			}
			
			$Table->addRow();
			
			$Title = $User->getUserTitle();
			if ($User->can('ADMIN'))
			{
				$Table->setColClasses('user_admin');
			}
			
			$Table->addCol('<a href="' . $Title->getLocalUrl() . '" class="link-user">' . $User->toHtml() . '</a>');
		 	$Table->addCol($Skin->makeKnownLinkObj( Title::makeTitle( NS_SPECIAL, 'Contributions' ), Skin::iconify('listrss').'<span class="text">' . wfMsg('Page.Contributions.page-title') .'</span>', 'target=' . urlencode($User->getUsername()) . '&feed=rss' ));
		 	$Table->addCol($Skin->makeKnownLinkObj( Title::makeTitle( NS_SPECIAL, 'Watchlist' ), Skin::iconify('listrss').'<span class="text">' . wfMsg('Page.WatchList.page-title') .'</span>', 'target=' . urlencode($User->getUsername()) . '&feed=rss' ));
		}
	}
}

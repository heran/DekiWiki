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

require_once('libraries/dom_pagination.php');

if (defined('MINDTOUCH_DEKI'))
{
	DekiPlugin::registerHook(Hooks::SPECIAL_LIST_USERS, 'wfSpecialListUsers');
	DekiPlugin::registerHook(Hooks::DATA_GET_SPECIAL_PAGES, array('SpecialListUsers', 'getSpecialPagesHook'));
}

function wfSpecialListUsers($pageName, &$pageTitle, &$html)
{
	// include the form helper
	DekiPlugin::requirePhp('special_page', 'special_form.php');

	$SpecialListUsers = new SpecialListUsers($pageName, basename(__FILE__, '.php'), true);
	
	// set the page title
	$pageTitle = $SpecialListUsers->getPageTitle();
	$html = $SpecialListUsers->output();
}

class SpecialListUsers extends SpecialPagePlugin
{
	protected $pageName = 'Listusers';
	protected $allowAnonymous = false;
	
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
		$html = '';
		
		$this->includeSpecialCss('special_listusers.css');
		
		$this->Request = DekiRequest::getInstance();
		$this->Title = Title::newFromText($this->requestedName, NS_SPECIAL);
		
		$userName = $this->Request->getVal('matchuser');
		
		if ($userName)
		{
			$User = DekiUser::newFromText($userName);
			if (!is_null($User)) 
			{
				$Title = $User->getUserTitle();
				parent::redirect($Title->getFullUrl());
				return;
			}
			else
			{
				DekiMessage::error(wfMsg('Page.ListUsers.no-user', htmlspecialchars($userName)));
			}
		}
		
		$html .= SpecialPageForm::getUserAutocomplete($this->Title, wfMsg('Page.ListUsers.header-view-user'), 'matchuser', wfMsg('Page.ListUsers.header-view'));
		$html .= $this->getTableHtml();
		
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
		
		return DekiUser::getSiteList(array('sortby' => $field), $currentPage, $this->itemsPerPage);	
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
		$Table->addSortHeading(wfMsg('Page.ListUsers.header-user-name'), 'username');
		$Table->addHeading(wfMsg('Page.ListUsers.header-contributions'));
		$Table->addSortHeading(wfMsg('Page.ListUsers.header-last-active'), 'date.lastlogin');
		
		$users = $this->getData();
		$this->formatTableRows($Table, $users);
		
		$paginator = $this->getPaginatorHtml($field, $method);
		return $Table->saveHtml() . $paginator;
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
			$Table->addRow();
			
			$Title = $User->getUserTitle();
			if ($User->can('ADMIN'))
			{
				$Table->setColClasses('user_admin');
			}
			
			$Table->addCol('<a href="' . $Title->getLocalUrl() . '" class="link-user">' . $User->toHtml() . '</a>');
			$Table->addCol($Skin->makeKnownLinkObj( Title::makeTitle( NS_SPECIAL, 'Contributions' ), wfMsg('Page.Contributions.page-title'), 'target=' . urlencode($User->getUsername()) ));
			$Table->addCol(date('F jS, Y', wfTimestamp(TS_UNIX, $User->getLastLogin())));
		}
	}
}

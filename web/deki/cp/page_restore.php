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

define('DEKI_ADMIN', true);
require_once('index.php');


class PageRestore extends DekiControlPanel
{
	// needed to determine the template folder
	protected $name = 'page_restore';

	const TYPE_NORMAL = 1;
	const TYPE_MOVE = 2;


	public function index()
	{
		$this->executeAction('listing');
	}

	//TODO: reason = string? : Reason for reverting
	public function restore($restoreId = null)
	{
		$restoreMethod = $this->Request->getVal('restore', null);

		if (is_null($restoreMethod))
		{
			// invalid restore attempt, nothing was posted
			$this->Request->redirect($this->getUrl());
			return;
		}

		$Plug = $this->Plug->At('archive', 'pages');

		$restoreType = $this->Request->getInt('restore') == self::TYPE_MOVE ? self::TYPE_MOVE : self::TYPE_NORMAL;

		if ($restoreType == self::TYPE_MOVE)
		{
			// make sure the user specified a path
			$to = $this->Request->getVal('to', null);
			$newTitle = $this->Request->getVal('newtitle');
			
			if (empty($newTitle))
			{
				DekiMessage::error($this->View->msg('PageRestore.error.path'));
				$this->Request->redirect($this->getUrl('/move/'.$restoreId));
				return;
			}

			$titleText = Article::combineName($to, wfEncodeTitle($newTitle));
			$ToTitle = Title::newFromText($titleText);

			$Result = $Plug->At($restoreId, 'restore')->With('to', $ToTitle->getPrefixedDbKey())->Post();
		}
		else
		{
			// attempt to restore the page
			$Result = $Plug->At($restoreId, 'restore')->Post();
		}

		if ($Result->getStatus() == 409)
		{
			// page restore is conflicted by another page, show the move page
			DekiMessage::error($this->View->msg('PageRestore.error.conflict'));
			$this->Request->redirect($this->getUrl('/move/'.$restoreId));
			return;
		}
		else if (!$Result->handleResponse())
		{
			// an unhandled error has occurred
			DekiMessage::error($this->View->msg('PageRestore.error.failed'));
			$this->Request->redirect($this->getUrl());
			return;
		}
		
		// okay
		$path = $Result->getVal('body/pages.restored/page/path');
		
		$Title = Title::newFromText($path);

		$message = $this->View->msg('PageRestore.success.restore', $this->getUrl('/'));
		DekiMessage::ui($message, 'success');
		
		$this->Request->redirect($Title->getLocalUrl());
		// end restore
	}
	

	public function move($restoreId)
	{
		global $wgLang;
		$Plug = $this->Plug->At('archive', 'pages');
		
		$PageResult = $Plug->At($restoreId)->Get();
		if(!$PageResult->handleResponse())
		{
			$this->Request->redirect($this->getUrl());
			return;
		}

		$ChildResult = $Plug->At($restoreId, 'subpages')->Get();
		if(!$ChildResult->handleResponse())
		{
			$this->Request->redirect($this->getUrl());
			return;
		}


		$Table = new DomTable();
		//$Table->setColWidths('30', '', '100', '100', '175');
		
		// create the table header
		$Table->addRow();
		$Table->addHeading($this->View->msg('PageRestore.data.title'));
		$Table->addHeading($this->View->msg('PageRestore.data.path'));

		// process the api results
		$page = array();
		$pageArchive = $PageResult->getVal('body/page.archive');
		$page['title'] = $pageArchive['title'];
		$page['path'] = $pageArchive['path'];
		$pageChildren = $ChildResult->getAll('body/pages.archive/page.archive');
		if (!is_null($pageChildren))
		{
			foreach ($pageChildren as $child)
			{
				$Table->addRow();
				$Table->addCol($child['title']);
				$Table->addCol($child['path']);
			}
		}

		$restoreUrl = $this->getUrl('/restore/'.$restoreId);
		$page['date.deleted'] = $wgLang->date(strtotime($pageArchive['date.deleted']), true);
		
		// TODO: remove article dependency
		Article::splitName($pageArchive['path'], $parentPath, $titleName);
		$ParentTitle = Title::newFromText($parentPath);
		$NewTitle = Article::getNextSubpageTitle($ParentTitle, $pageArchive['title']);

		
		// show the details about the related pages
		if (count($pageChildren) > 0)
		{
			$this->View->set('childTable', $Table->saveHtml());
		}
		
		$this->View->set('page', $page);
		$this->View->set('move-form.action', $restoreUrl);
		$this->View->set('move-form.cancel', $this->getUrl());
		$this->View->set('move-form.to', $ParentTitle->getPrefixedUrl());
		$this->View->set('move-form.restore-to', $NewTitle->getPathlessText());
		$this->View->output();
		// end move
	}


	public function preview($restoreId = null)
	{
		// TODO: remove wgUser dependency
		global $wgLang, $wgUser;

		/*
		 * @param int $currentPage logical page #1 is first, so -1 to generate offset
		 * @param string $searchQuery page title text to search the deletion log for, if empty, no search
		 */
		$currentPage = $this->Request->getInt('page', 1);
		$searchQuery = $this->Request->getVal('query', null);
		
		$Plug = $this->Plug->At('archive', 'pages', $restoreId);

		$InfoResult = $Plug->At('info')->Get();
		if(!$InfoResult->handleResponse())
		{
			$this->Request->redirect($this->getUrl());
			return;
		}

		$ContentsResult = $Plug->At('contents')->Get();
		if(!$ContentsResult->handleResponse())
		{
			$this->Request->redirect($this->getUrl());
			return;
		}

		$page = array();
		$pageArchive = $InfoResult->getVal('body/page.archive');
		$page['title'] = $pageArchive['title'];
		$page['path'] = $pageArchive['path'];

		$page['date.deleted'] = $wgLang->date(strtotime($pageArchive['date.deleted']), true);
		$deletedUser = $InfoResult->getVal('body/page.archive/user.deleted/username');

		$userPage =& Title::makeTitle(NS_USER, $deletedUser);
		$userLink = $wgUser->getSkin()->makeLinkObj($userPage, $deletedUser);
		$page['user.deleted'] = $userLink;

		$page['preview'] = $ContentsResult->getVal('body/content/body');
		
		$this->View->setRef('page', $page);
		$this->View->set('form.action', $this->getUrl('/restore/'. $restoreId));
		$this->View->set('form.cancel', $this->getUrl('/', array(), true));		


		$this->View->output();
		// end preview
	}

	
	/**
	 * @param int $restoreId the restore group to show expanded
	 */
	public function listing($restoreId = null)
	{
		global $wgLang;


		// build the listing table
		$Table = new DekiTable($this->name, 'listing', array());
		$Table->setResultsPerPage(Config::RESULTS_PER_PAGE);
		// enable searching for this table
		$Table->setSearchField('title');
		// space the columns
		$Table->setColWidths('18', '', '100', '100', '150');
		$Table->addClass('groups');


		// create the table header
		$Table->addRow();
		$Th = $Table->addHeading($this->View->msg('PageRestore.data.path'));
		$Th->setAttribute('colspan', '2');
		$Table->addHeading($this->View->msg('PageRestore.data.date'));
		$Table->addHeading($this->View->msg('PageRestore.data.deleted-by'));
		$th = $Table->addHeading('&nbsp;');
		$th->setAttribute('class', 'last');

		// grab the results
		$Plug = $this->Plug->At('archive', 'pages');
		$Result = $Table->getResults($Plug, '/body/pages.archive/@querycount');
		$Result->handleResponse();

		$pageArchives = $Result->getAll('body/pages.archive/page.archive', array());
		if (empty($pageArchives))
		{
			$Table->setAttribute('class', 'table none');
			$Table->addRow();
			$Td = $Table->addCol('<div class="none">'.$this->View->msg('PageRestore.data.empty').'</div>');
			$Td->setAttribute('colspan', 5);
			$Td->setAttribute('class', 'last');
		}
		else
		{
			foreach ($pageArchives as $pageArchive)
			{
				$localizedTime = $wgLang->date(strtotime($pageArchive['date.deleted']), true);
				$previewUrl = $this->getUrl('/preview/' . $pageArchive['@id'], array(), true);
				$restoreUrl = $this->getUrl('/restore/'.$pageArchive['@id'], array(), true);

				$CurrentRow = $Table->addRow();
				$archivePath = urldecode($pageArchive['path']);
				if ($pageArchive['subpages']['@count'] > 0)
				{
					// deletion includes more than 1 page
					if ($pageArchive['@id'] == $restoreId)
					{
						$buttonUrl = $this->getUrl('/listing', array(), true);
						$iconType = 'contract';
					}
					else
					{
						$buttonUrl = $this->getUrl('/listing/' . $pageArchive['@id'], array(), true);
						$iconType = 'expand';
					}
					$path = sprintf('<a href="%s" class="%s" restoreId="%s">%s</a>',
 							$buttonUrl, $iconType, $pageArchive['@id'], htmlspecialchars($archivePath));
				}
				else
				{
					// deletion is just 1 page
					$path = '<span class="expand">'.htmlspecialchars($archivePath).'</span>';
				}

				$Td = $Table->addCol($path);
				$Td->setAttribute('colspan', 2);
				$Table->addCol($localizedTime);

				// create link to the user's page
				if (isset($pageArchive['user.deleted']))
				{
					$UserTitle = Title::newFromText($pageArchive['user.deleted']['username'], NS_USER);
					$Table->addCol(sprintf('<a href="%s">%s</a>', $UserTitle->getLocalUrl(), $pageArchive['user.deleted']['username']));
				}
				else
				{
					// no user information specified with this deletion
					$Table->addCol('&nbsp;');
				}
						
				$td = $Table->addCol(sprintf('<a href="%s">%s</a>', $previewUrl, 'Restore'));
				$td->setAttribute('class', 'last');

				// check if this archive should be expanded
				if ($pageArchive['@id'] == $restoreId)
				{
					// get the class of the current row
					$rowClass = $CurrentRow->getAttribute('class');

					// get the related pages
					$RelatedResult = $Plug->At($pageArchive['@id'], 'subpages')->Get();
					$relatedPages = $RelatedResult->getAll('body/pages.archive/page.archive');
					foreach ($relatedPages as $relatedPage)
					{
						$Row = $Table->addRow(false);
						$Row->setAttribute('class', $rowClass);

						$Td = $Table->addCol('<span class="expand">'.$relatedPage['path'].'</span>');
						$Td->setAttribute('colspan', 2);
						$Table->addCol('');
						$Table->addCol('');
						$td = $Table->addCol('');
						$td->setAttribute('class', 'last');
					}
				}
			}
			$html = $Table->saveHtml();
		}
		
		// if the table is requested, only return it
		if ($this->Request->getVal('html', null) == 'table')
		{
			echo $html;
			exit(); // stop processing the page
		}
		
		$this->View->set('archives-table', $Table->saveHtml());
		$this->View->set('search-form.action', $this->getUrl('/listing'));
		$this->View->set('search-form.query', $Table->getCurrentSearch());

		$this->View->output();
		// end listing
	}
}

new PageRestore();

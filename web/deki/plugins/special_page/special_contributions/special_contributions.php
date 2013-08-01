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

require_once 'Feed.php';
require_once 'libraries/dom_pagination.php';

if (defined('MINDTOUCH_DEKI'))
{
	DekiPlugin::registerHook(Hooks::SPECIAL_CONTRIBUTIONS, 'wfSpecialContributions');
	DekiPlugin::registerHook(Hooks::DATA_GET_SPECIAL_PAGES, array('SpecialContributions', 'getSpecialPagesHook'));
}

function wfSpecialContributions($pageName, &$pageTitle, &$html, &$subhtml)
{
	$SpecialContributions = new SpecialContributions($pageName);
	
	$SpecialContributions->output($pageTitle, $html, $subhtml);
}

class SpecialContributions extends SpecialPagePlugin
{
	protected $pageName = 'Contributions';
	
	protected $paginate = true;

	protected $Title;
	protected $Request;
	
	protected $User = null;
	
	protected $itemsPerPage = 100;
	protected $isLastPage = true;


	public static function getSpecialPagesHook(&$pages)
	{
		$Special = new self();
		$name = $Special->getPageTitle();
		$href = $Special->getTitle()->getFullURL();
		
		$pages[$name] = array('name' => $name, 'href' => $href);
	}

	public function output(&$pageTitle, &$html, &$subhtml)
	{
		DekiPlugin::requirePhp('special_page', 'special_form.php');
		
		$html = '';
		$subhtml = '';
		
		$this->User = DekiUser::getCurrent();
		$this->Request = DekiRequest::getInstance();
		$this->Title = Title::newFromText($this->requestedName, NS_SPECIAL);
		
		$userName = $this->Request->getVal('target');
		$this->User = DekiUser::newFromText($userName);
		
		if (!$userName || is_null($this->User))
		{
			DekiMessage::error( wfMsg('Page.Contributions.no-changes-found-matching'));

			$Title = Title::newFromText('Recentchanges', NS_SPECIAL);
			parent::redirect($Title->getFullUrl(), '303');
			return;
		}
		
		$feedFormat = $this->Request->getVal('feed');
		
		if ( $feedFormat && !is_null($this->User) )
		{
			$this->outputFeed($feedFormat, $this->User->getId());
			return;
		}
		
		// set the page title
		$pageTitle = wfMsg('Page.Contributions.recent-changes-from', htmlspecialchars($userName));
		$html .= SpecialPageForm::getUserAutocomplete($this->Title, wfMsgForContent('Page.Contributions.view-changes-by'), 'target', wfMsg('Page.Contributions.view'));
		
		if (is_null($this->User))
		{
			$html .= '<p>' . wfMsg('Page.Contributions.no-changes-found-matching') . '</p>' . "\n";
		}
		else
		{
			$html .= $this->getTableHtml();
		}
		
		$all_changes = $this->User->getSkin()->makeLinkObj(Title::newFromText(Hooks::SPECIAL_RECENT_CHANGES), wfMsg('Page.Contributions.all-changes')); 
		$feed = $this->Title->getLocalUrl('feed=rss&target='.urlencode($this->User->getName()));
		$title = wfMsg('Page.Contributions.feed', $this->User->toHtml()); 
				
		$this->setSyndicationFeed($title, $feed);
		
		$subhtml .= '<div class="deki-rc-allchanges"><span class="deki-subnav-return">'.$all_changes.'</span></div>'
			.'<div class="deki-rc-feeds">'
			.'<span class="deki-subnav-label">'.wfMsg('Page.RecentChanges.feed').'</span>'
			.'<ul class="deki-feedlist">'
			.'<li>'
			.$this->User->getSkin()->makeLinkObj(
				$this->Title, 
				'<span>'.wfMsg('Page.Contributions.feed', $this->User->toHtml()).'</span>', 
				'feed=rss&target='.urlencode($this->User->getName())
			).'</li></ul></div>'
			.'<div class="clear"></div>'; 
	}

	protected function getData()
	{
		$limit = $this->Request->getInt('limit', $this->itemsPerPage);
		$offset = $this->Request->getInt('offset', 0);
		
		$Plug = DekiPlug::getInstance()
			->At('users', $this->User->getId(), 'feed')
			->With('format', 'raw')
			->With('limit', $limit + 1)
			->With('offset', $offset);
		
		$Result = $Plug->Get();
		
		$changes = array();
		
		if( $Result->isSuccess() )
		{
			$changes = $Result->getAll('body/table/change');
			
			if ( count($changes) > $limit )
			{
				$this->isLastPage = false;
				unset($changes[$limit]);
			}
		}
			
		return $changes;
	}
	
	protected function getTableHtml()
	{
		$Table = new DomTable();
		
		$Table->setColWidths('16', '45%', '20%', '35%');
		
		$Table->addRow(false);
		$Table->addHeading('&nbsp;');
		$Table->addHeading(wfMsg('Page.Contributions.header-page'));
		$Table->addHeading(wfMsg('Page.Contributions.header-date'));
		$Table->addHeading(wfMsg('Page.Contributions.header-edit-summary'));
				
		$changes = $this->getData();
		if (!empty($changes))
	 	{
			$this->formatTableRows($Table, $changes);
		}
		else
		{
			$Table->addRow(false);
			$Td = $Table->addCol(wfMsg('Page.Contributions.no-changes', htmlspecialchars(DekiSite::getName())));
			$Td->setAttribute('colspan', 4)->addClass('none');
		}
		
		$paginator = $this->getPaginatorHtml();
		return $Table->saveHtml() . $paginator;
	}
	
	protected function getPaginatorHtml()
	{
		$html = '';
		
		if ( $this->paginate )
		{
			$Paginator = new DomOffsetPagination($this->Title->getLocalURL('target=' . urldecode($this->User->getName())), $this->itemsPerPage, $this->isLastPage);
			$html = $Paginator->saveHtml();
		}
		
		return $html;
	}
	
	protected function formatTableRows($Table, $changes)
	{
		global $wgUser, $wgLang;
		
		$Skin = $wgUser->getSkin();
		
		$lastKey = null;
		$j = $k = 0;
		
		$changesCount = count($changes);
		$rows = array();
		
		for ( $i = 0; $i < $changesCount; $i++ )
		{
			$change = $changes[$i];

			$namespace = $change['rc_namespace'];
			$title = $change['rc_title'];
			$timestamp = $change['rc_timestamp'];
			$comment = $change['rc_comment'];
				
			$key = $namespace . ':' . $title;	
			$group = ( $lastKey == $key ) ? true : false;	
			
			// check the next record in the list to see if we need to group them
			if( $i + 1 == $changesCount )
			{
				// if we're at the last record, it can't be a parent
				$parent = false;
			}
			else
			{
				$nextRow = $changes[$i+1];
				$nextKey = $nextRow['rc_namespace'] . ':' . $nextRow['rc_title'];
				$parent = ( $key == $nextKey ) ? true : false;
			}
			
			if( !$group )
			{
				// do some magic to make showing/hiding work
				$j++;
				$rowClass = 'contrib' . $j;
				$colClass = ( ++$k & 1 ) ? 'bg1' : 'bg2';
			}
			
			$rows[] = array(
				'group' => $group, 
				'parent' => $parent,
				'title' => $title, 
				'namespace' => $namespace,
				'timestamp' => $timestamp, 
				'comment' => $comment,
				'rowClass' => $rowClass,
				'colClass' => $colClass 
			);
			
			$lastKey = $key;
		}
		
		foreach ($rows as $row)
		{
			$Title =& Title::makeTitle( $row['namespace'], $row['title'] );

			$link = $Skin->makeKnownLinkObj( $Title, $Title->getDisplayText() );
			$comment = '<em>' . $Skin->formatComment( $row['comment'], $Title ) . '</em> ';
			
			$time = $wgLang->time( $row['timestamp'], true );
			$date = $wgLang->date( $row['timestamp'], true );
		
			$img = '&nbsp;';

			if ( $row['parent'] )
			{
				$img = '<span class="toctoggle">
					<a href="#" onclick="return toggleChangesTable(\'' . $row['rowClass'] . '\')" class="internal">
						<span id="showlink-' . $row['rowClass'] . '" ' . 'style="display:none;">' . Skin::iconify('expand') . '</span>'
		            	. '<span id="hidelink-' . $row['rowClass'] . '">' . Skin::iconify('contract') . '</span>
	            	</a>
            	</span>';
			}
			
			$Row = $Table->addRow();
			
			if ( $row['group'] )
			{
				$img = Skin::iconify( 'dotcontinue' );
				$Row->setAttribute( 'class', $row['rowClass'] );
				$Row->setAttribute( 'style', 'display:none;' );
				$Row->addClass($row['colClass']);
			}
			else
			{
				$Row->setAttribute( 'class' );
				$Row->addClass($row['colClass']);
			}
			
			$Table->setColClasses( $row['colClass'] );

			$Table->addCol( $img );
			$Table->addCol( $link );
			$Table->addCol( $date . ' ' . $time );
			$Table->addCol( $comment );
		}
	}
	
	protected function outputFeed($feedFormat, $userId)
	{
		$feed = new MTFeed('contributions', '', 0, 0, $feedFormat, $userId);
		$feed->output();
	}
}

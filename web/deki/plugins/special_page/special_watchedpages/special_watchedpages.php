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

require_once 'WatchedItem.php';

if (defined('MINDTOUCH_DEKI'))
{
	DekiPlugin::registerHook(Hooks::SPECIAL_WATCHED_PAGES, 'wfSpecialWatchedPages');
	DekiPlugin::registerHook(Hooks::DATA_GET_SPECIAL_PAGES, array('SpecialWatchedPages', 'getSpecialPagesHook'));
}

function wfSpecialWatchedPages($pageName, &$pageTitle, &$html)
{
	$SpecialWatchedPages = new SpecialWatchedPages($pageName);
	
	// set the page title
	$pageTitle = $SpecialWatchedPages->getPageTitle();
	$html = $SpecialWatchedPages->output();
}

class SpecialWatchedPages extends SpecialPagePlugin
{
	protected $pageName = 'Watchedpages';
	
	protected $Title;
	protected $Request;
	protected $User = null;
	protected $Model;
	
	protected $defaultSortField = 'timestamp';
	protected $defaultSort = DomSortTable::SORT_DESC;

	public static function getSpecialPagesHook(&$pages)
	{
		$Special = new self();
		$name = $Special->getPageTitle();
		$href = $Special->getTitle()->getFullURL();
		
		$pages[$name] = array('name' => $name, 'href' => $href);
	}
	
	public function output()
	{
		global $wgUser;
		
		$html = '';
		
		$this->Request = DekiRequest::getInstance();
		$this->Title = Title::newFromText($this->requestedName, NS_SPECIAL);
		$this->User = $wgUser;
		$this->Model =& wfGetDB( DB_SLAVE );
		
		$this->setRobotPolicy( "noindex,nofollow" );
		
		$this->unwatchPages();
		
		if( $this->User->isAnonymous() || $this->getCount() == 0 )
		{
			$html .= wfMsg('Page.WatchedPages.no-items-in-watchlist');
		}
		else
		{
			$html .= $this->getTableHtml();
		}
		
		return $html;
	}
	
	protected function unwatchPages()
	{
		$unwatchPageIDs = $this->Request->getArray('pageId');
		foreach ( $unwatchPageIDs as $unwatchPageID )
		{
			$Title = Title::newFromID($unwatchPageID);
			$WatchedItem = WatchedItem::fromUserTitle($this->User, $Title);
			$WatchedItem->removeWatch();
		}
	}
	
	protected function getData()
	{
		$userId = $this->User->getID();
		
		$sortField = $this->Request->getVal('sortby', $this->defaultSortField);
		$sortMethod = $this->Request->getVal('sort', $this->defaultSort);
		
		$pages = $this->Model->tableName( 'pages' );
		$watchlist = $this->Model->tableName( 'watchlist' );
		
		$sql = '
			SELECT page_namespace, page_title, page_comment, page_id, page_user_id,
				   page_timestamp, page_minor_edit, page_is_new
			FROM' . $watchlist . ', ' . $pages . '
			WHERE wl_user = ' . $userId . '
			AND (wl_namespace = page_namespace OR wl_namespace + 1 = page_namespace)
			AND wl_title = page_title
		';
		
		$sortField = ( $sortField == 'title' ) ? 'wl_title' : 'page_timestamp';
		$sortMethod = ( $sortMethod == DomSortTable::SORT_ASC ) ? DomSortTable::SORT_ASC : DomSortTable::SORT_DESC;
  
		$sql .= ' ORDER BY ' . $sortField . ' ' . $sortMethod;
		
		$result = $this->Model->query( $sql );
		
		return $result;
	}
	
	protected function getCount()
	{
		$userId = $this->User->getID();
		
		$watchlist = $this->Model->tableName( 'watchlist' );
		
		$sql = 'SELECT COUNT(*) AS wl_count FROM ' . $watchlist . ' WHERE wl_user = ' . $userId;
		
		$result = $this->Model->query( $sql );
		$row = $this->Model->fetchObject( $result );
		
		return $row->wl_count;
	}
	
	protected function getTableHtml()
	{
		$Frag = new DomFragment();
		
		$Form = $Frag->createElement('form')
						->setAttribute('name', 'wpForm')
						->setAttribute('method', 'post');
		$Frag->appendChild($Form);
		
		$Div = $Frag->createElement('div');
		$Element = $Frag->createElement('input')
						->setAttribute('type', 'submit')
						->setAttribute('name', 'doSubmit')
						->setAttribute('value', wfMsg('Page.WatchedPages.form.remove-selected'));
		
		$Div->appendChild($Element);
		$Form->appendChild($Div);
		
		$field = $this->Request->getVal('sortby', $this->defaultSortField);
		$method = $this->Request->getVal('sort', $this->defaultSort);
		
		$Table = new DomSortTable($this->Title->getLocalURL(), $field, $method);
		$Form->appendChild($Table);
		
		$Table->setColWidths('16', '40%', '20%', '15%', '25%');
		
		$Table->addRow(false);
		$Table->addHeading('&nbsp;');
		$Table->addSortHeading(wfMsg('Page.WatchedPages.header-page'), 'title');
		$Table->addSortHeading(wfMsg('Page.WatchedPages.header-last-modified'), 'timestamp');
		$Table->addHeading(wfMsg('Page.WatchedPages.header-edited-by'));
		$Table->addHeading(wfMsg('Page.WatchedPages.header-edit-summary'));
		
		$pages = $this->getData();
		
		$counter = 1;

		while ( $obj = $this->Model->fetchObject( $pages ) )
		{
			# Make fake RC entry
			$RecentChange = RecentChange::newFromCurRow( $obj );
			$title = $RecentChange->getTitle();
			$RecentChange->counter = $counter++;
			
			$this->addRow( $Table, $RecentChange, true, $counter - 1 );
		}
		
		return $Frag->saveHtml();
	}
	
	protected function addRow( $Table, $RecentChanges, $watched = false, $i = 0 )
	{
		global $wgLang, $wgContLang;
		
		$Skin = $this->User->getSkin();
		$Title = $RecentChanges->getTitle();
		
		# Extract DB fields into local scope
		extract( $RecentChanges->mAttribs );
		

		$Table->addRow();		
		
		# Checkbox
		$Checkbox = new DwDomElement('input');
		$Checkbox->setAttribute('type', 'checkbox');
		$Checkbox->setAttribute('id', 'wpcb' . $i);
		$Checkbox->setAttribute('name', 'pageId[]');
		$Checkbox->setAttribute('value', $Title->getArticleId());
		
		$Table->addCol($Checkbox->saveHtml());

		# Page title
		$td = '';
		if ( $rc_type == RC_MOVE || $rc_type == RC_MOVE_OVER_REDIRECT )
		{
			# "[[x]] moved to [[y]]"
			$msg = ( $rc_type == RC_MOVE ) ?
				'Page.WatchedPages.x-moved-to-y' : 'Page.WatchedPages.x-moved-to-y-over-redirect';
			
			$td = wfMsg($msg, $Skin->makeKnownLinkObj( $Title, $Title->getDisplayText(), 'redirect=no'),
				$Skin->makeKnownLinkObj( $RecentChanges->getMovedToTitle(), $RecentChanges->getMovedTitle()->getDisplayText() ) );

		}
		elseif( $rc_namespace == NS_SPECIAL && preg_match( '!^Log/(.*)$!', $rc_title, $matches ) )
		{
			# Log updates, etc
			$logtype = $matches[1];
			$logname = LogPage::logName( $logtype );
			$td = '(' . $Skin->makeKnownLinkObj( $Title, $logname ) . ')';
		}
		else
		{
			$td = $Skin->makeKnownLinkObj( $Title,  $Title->getDisplayText() );

			if ( $watched )
			{
				$td = '<strong>' . $td . '</strong>';
			}
			
			if ($rc_type == RC_NEW)
			{
				$td = '<span class="type-new">' . $td . '</span>';	
			}
		}
		
		$Table->addCol($td);

		# Timestamp
		$lastmodTip = $wgLang->timeanddate( $rc_timestamp );
		
		$Link = new DwDomElement('a');
		$Link->setAttribute('href', $Title->getLocalUrl( 'action=history' ));
		$Link->setAttribute('title', $lastmodTip);
		$Link->innerHtml($lastmodTip);
		
		$Table->addCol($Link->saveHtml());

		# Edited by
		$userPage =& Title::makeTitle( NS_USER, $rc_user_text );
		$userLink = $Skin->makeLinkObj( $userPage, $rc_user_text );

		$Table->addCol($userLink);

		# Add comment
		$td = '&nbsp;';
		if ( '' != $rc_comment && '*' != $rc_comment && $rc_type != RC_MOVE && $rc_type != RC_MOVE_OVER_REDIRECT )
		{
			$rc_comment = $Skin->formatComment($rc_comment, $Title);
			$td = $wgContLang->emphasize('(' . $rc_comment . ')');
		}
		
		$Table->addCol($td);

		return $Table;
	}
}

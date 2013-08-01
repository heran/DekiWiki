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
require_once 'ChangesList.php';
require_once 'libraries/dom_pagination.php';

if (defined('MINDTOUCH_DEKI'))
{
	DekiPlugin::registerHook(Hooks::SPECIAL_WATCH_LIST, 'wfSpecialWatchList');
	DekiPlugin::registerHook(Hooks::DATA_GET_SPECIAL_PAGES, array('SpecialWatchList', 'getSpecialPagesHook'));
}

function wfSpecialWatchList($pageName, &$pageTitle, &$html)
{
	$SpecialWatchList = new SpecialWatchList($pageName);
	
	// set the page title
	$pageTitle = $SpecialWatchList->getPageTitle();
	$html = $SpecialWatchList->output();
}

class SpecialWatchList extends SpecialPagePlugin
{
	protected $pageName = 'Watchlist';
	
	protected $paginate = true;

	protected $Title;
	protected $Request;
	
	protected $User = null;
	
	protected $itemsPerPage = 100;
	protected $isLastPage = true;
	
	protected $since;

	
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
		$this->Title = Title::newFromText($this->pageName, NS_SPECIAL);

		$this->since = $this->getSince();
		
		$userName = $this->Request->getVal('target');
		$this->User = ( !is_null($userName) ) ? DekiUser::newFromText($userName) : $wgUser;
		
		if ( is_null($this->User) || $this->User->isAnonymous() )
		{
			$html .= wfMsg( 'Page.WatchList.no-items-in-watchlist' );
			return $html;
		}
		
		# Note: Now only feed output is used
		$feedFormat = $this->Request->getVal('feed');
		if ( $feedFormat )
		{
			$this->outputFeed($feedFormat);
			return;
		}
		
		$this->setRobotPolicy('noindex,nofollow');
		
		$html .= $this->getTableHtml();
		
		return $html;
	}
	
	protected function getSince()
	{
		$since = '';
		
		$days = $this->getDays();
		if ( $days > 0 )
		{
			$timestamp = time() - intval( $days * 86400 );
			$since = wfTimestamp(TS_MW, $timestamp);
		}
		
		return $since;
	}
	
	protected function getDays()
	{
		$days = $this->Request->getVal('days', 0);
		$days = floatval($days);
		
		return $days;
	}
	
	protected function getSinceHtml($numRows)
	{
		global $wgLang;
		
		$html = '';
		
		$days = $this->getDays();
		
		if ( $days >= 1 )
		{
			$html .= wfMsg( 'Page.WatchList.below-last-changes', $wgLang->formatNum( $numRows ), $wgLang->formatNum( $days ) );
		}
		elseif ( $days > 0 )
		{
			$html .= wfMsg( 'Page.WatchList.below-last-changes', $wgLang->formatNum( $numRows ), $wgLang->formatNum( round($days*24) ) );
		}
		
		$html .= '<div>' . $this->getSinceLinks() . '</div>';
		
		return $html;
	}
	
	protected function getSinceLinks()
	{
		global $wgUser, $wgLang, $wgContLang;
		
		$hours = array( 1, 2, 6, 12 );
		$days = array( 1, 3, 7 );

		$hLinks = array();
		$dLinks = array();
		
		$Skin = $wgUser->getSkin();
		
		foreach( $hours as $hour )
		{
			$hLinks[] = $Skin->makeKnownLink(
				$wgContLang->specialPage( $this->pageName ),
				$wgLang->formatNum( $hour ),
				"days=" . ($hour / 24.0) );
		}
		
		foreach( $days as $day )
		{
			$dLinks[] = $Skin->makeKnownLink(
				$wgContLang->specialPage( $this->pageName ),
				$wgLang->formatNum( $day ), 'days=' . $day );
		}
		
		$all = $Skin->makeKnownLink(
			$wgContLang->specialPage( $this->pageName ),
			wfMsg( 'Page.WatchList.all' ));
		
		return wfMsg ('Page.WatchList.show-last-hours-days',
			implode(" | ", $hLinks),
			implode(" | ", $dLinks),
			$all);
	}
	
	protected function getData()
	{
		$limit = $this->Request->getInt('limit', $this->itemsPerPage);
		$offset = $this->Request->getInt('offset', 0);
		
		$Plug = DekiPlug::getInstance()
			->At('users', $this->User->getId(), 'favorites', 'feed')
			->With('format', 'raw')
			->With('limit', $limit + 1)
			->With('offset', $offset);
			
		if ( !empty($this->since) )
		{
			$Plug = $Plug->With('since', $this->since);
		}
			
		$Result = $Plug->Get();
		
		$changes = array();
		
		if( $Result->isSuccess() )
		{
			$changes = $Result->getAll('body/table/change');
			
			if ( count($changes) > $limit )
			{
				$this->isLastPage = false;
				array_pop($changes);
			}
		}
			
		return $changes;
	}
	
	protected function getTableHtml()
	{
		global $wgUser, $wgLang;
		
		$html = '';
		
		$changes = $this->getData();
		$count = count($changes);
		
		if ( $count == 0 )
		{
			return '<p>' . wfMsg( 'Page.WatchList.no-items-edited' ) . '</p>';
		}
		
		$this->setRssSyndication(true);
		
		$html .= $this->getSinceHtml($count);
		
		$Skin = $wgUser->getSkin();
		$limit = $this->Request->getInt('limit', $this->itemsPerPage);
		
		$List = new ChangesList($Skin);
		$List->beginRecentChangesList();

		$lookup = array();
		$sorted = array();

		$i = 0;

		foreach($changes as $obj)
		{
			// case the row to an object
			$obj = (object) $obj;

			if( $limit == 0 )
			{
				break;
			}

			// MT (steveb): check if we already inserted such an item
			if ($obj->rc_cur_id == '0')
			{
				$key = $obj->rc_namespace . '-' . $obj->rc_cur_id . '-' .  $obj->rc_id. '-' . $wgLang->date( $obj->rc_timestamp, true);
			}
			else
			{
				$key = $obj->rc_namespace . '-' . $obj->rc_cur_id . '-' . $wgLang->date( $obj->rc_timestamp, true);
			}
			
			if( $obj->rc_namespace >= 0 && $obj->rc_cur_id > 0 && isset( $lookup[$key] ) )
			{
				// insert object
				$obj->isChild = true;
				$RecentChange = RecentChange::newFromRow( $obj );
				$title = $RecentChange->getTitle();
				$RecentChange->rc_expand_id = md5($title->mUrlform . $i);
				$lookup[$key][0]->mAttribs['isParent'] = true;
				$RecentChange->mAttribs['rc_expand_id'] = $lookup[$key][0]->mAttribs['rc_expand_id'];
				$lookup[$key][] = $RecentChange;
			}
			else
			{
				// insert object
				$RecentChange = RecentChange::newFromRow( $obj );
				$title = $RecentChange->getTitle();
				$RecentChange->mAttribs['rc_expand_id'] = md5($title->mUrlform . $i);
				--$limit;

				// update lookup
				$lookup[$key] = array( $RecentChange );
				$sorted[] = $key;
			}
			
			$i++;
		}

		// MT (steveb): flatten the list
		$_data = array();
		
		foreach( $sorted as $key )
		{
			foreach ( $lookup[$key] as $item )
			{
				$_data[] = $item;
			}
		}
		unset( $lookup );
		unset( $sorted );

		// MT (steveb): emit the table
		$i = 0;
		$Frag = new DomFragment();
		$Table = null;

		if (count($_data) > 0)
		{
			foreach ($_data as $RecentChange)
			{
				if (empty($RecentChange->mAttribs['isChild']))
				{
					$i++;
				}

				$date = $wgLang->date($RecentChange->mAttribs['rc_timestamp'], true);
				if ($date != $List->lastdate)
				{
					// need to add the new header
					$Element = $Frag->createElement('h4', $date);
					$Frag->appendChild($Element);
					$Element = $Frag->createElement('div')->addClass('table');
					$Frag->appendChild($Element);
					
					$Table = new DomTable();
					$Element->appendChild($Table);
					$Table->setColWidths('16', '40%', '10%', '15%', '35%');

					$Table->addRow();
					$Table->addHeading('&nbsp;');
					$Table->addHeading(wfMsg('Article.History.header-page'));
					$Table->addHeading(wfMsg('Article.History.header-time'));
					$Table->addHeading(wfMsg('Article.History.header-edited-by'));
					$Table->addHeading(wfMsg('Article.History.header-edit-summary'));

					$List->lastdate = $date;
				}
					
				// MT (steveb): we invoke the old style directly; why bother, we won't support the new one ever!
				$List->recentChangesLineOld($RecentChange, !empty($RecentChange->mAttribs['wl_user']), $i, $Table);
			}
		}
				
		$paginator = $this->getPaginatorHtml();
		
		$html .= $paginator . $Frag->saveHtml() . $paginator;
		
		return $html;
	}
	
	protected function getPaginatorHtml()
	{
		$html = '';
		
		if ( $this->paginate )
		{
			$query = '';
			
			$days = $this->getDays();
			if ( $days > 0 )
			{
				$query = 'days=' . urlencode($days);
			}
			
			$Paginator = new DomOffsetPagination($this->Title->getLocalURL($query), $this->itemsPerPage, $this->isLastPage);
			$html = $Paginator->saveHtml();
		}
		
		return $html;
	}
	
	protected function outputFeed($feedFormat)
	{
		$feed = new MTFeed('watchlist', $this->since, 0, 0, $feedFormat, $this->User->getId());
		$feed->output();
	}
}

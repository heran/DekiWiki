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

require_once('Feed.php');
require_once('ChangesList.php');
require_once('libraries/dom_pagination.php');

if (defined('MINDTOUCH_DEKI'))
{
	DekiPlugin::registerHook(Hooks::SPECIAL_RECENT_CHANGES, 'wfSpecialRecentChanges');
	DekiPlugin::registerHook(Hooks::DATA_GET_SPECIAL_PAGES, array('SpecialRecentChanges', 'getSpecialPagesHook'));
}

function wfSpecialRecentChanges($pageName, &$pageTitle, &$html, &$subhtml)
{
	// include the form helper
	DekiPlugin::requirePhp('special_page', 'special_form.php');
	
	$SpecialRecentChanges = new SpecialRecentChanges($pageName);
	
	// set the page title
	$pageTitle = $SpecialRecentChanges->getPageTitle();
	$SpecialRecentChanges->output($html, $subhtml);
}

class SpecialRecentChanges extends SpecialPagePlugin
{
	protected $pageName = 'Recentchanges';
	
	protected $paginate = true;

	protected $Title;
	protected $Request;
	
	protected $User = null;
	
	protected $itemsPerPage = 50;
	protected $isLastPage = true;
	
	// filters
	protected $language;
	protected $namespace;
	
	const DEFAULT_NAMESPACE_FILTER = 'main';
	// additional options: attachment, user_talk
	protected static $namespaces = array(
		'all' => '',
		'main' => 'main',
		'user' => 'user'
	);

	
	public static function getSpecialPagesHook(&$pages)
	{
		$Special = new self();
		$name = $Special->getPageTitle();
		$href = $Special->getTitle()->getFullURL();
		
		$pages[$name] = array('name' => $name, 'href' => $href);
	}

	public function output(&$html, &$subhtml)
	{
		$html = '';
		$subhtml = '';
		
		$this->includeCss($this->specialFolder . '/' . 'special_recentchanges', 'recentchanges.css');
		
		$this->Request = DekiRequest::getInstance();
		$this->Title = Title::newFromText($this->requestedName, NS_SPECIAL);
		$this->User = DekiUser::getCurrent();	
		
		$feedFormat = $this->Request->getVal('feed');
		$feedLanguage = $this->Request->getVal('language', DekiLanguage::isSitePolyglot() ? wfLanguageActive(''): null);
		$displayFormat = $this->Request->getVal('format');
		
		if ( $feedFormat )
		{
			$this->outputFeed($feedFormat, $feedLanguage, $displayFormat);
			return;
		}
		
		$Title = Title::newFromText('Contributions', NS_SPECIAL);
		
		$feedLanguage = htmlspecialchars($feedLanguage);
		
		$html .= SpecialPageForm::getUserAutocomplete($Title, wfMsgForContent('Page.RecentChanges.view-changes-by'), 'target', wfMsg('Page.RecentChanges.submit-view'));
		$html .= $this->getTableHtml();
		
		// subhtml here
		$subhtml .= $this->getFilterForm();

		$sk = $this->User->getSkin();
		$subhtml .= '<div class="deki-rc-feeds">'
			.'<span class="deki-subnav-label">'.wfMsg('Page.RecentChanges.feed').'</span>'
			.'<ul class="deki-feedlist">'
			.'<li>'
			.$sk->makeLinkObj(
				$this->Title, 
				'<span>'.wfMsg('Page.RecentChanges.feed-daily').'</span>', 
				'feed=rss&format=daily'.(DekiLanguage::isSitePolyglot() ? '&language='.$feedLanguage : '')
			)
			.'</li>'
			.'<li>'
			.$sk->makeLinkObj(
				$this->Title, 
				'<span>'.wfMsg('Page.RecentChanges.feed-all').'</span>', 
				'feed=rss&format=all'.(DekiLanguage::isSitePolyglot() ? '&language='.$feedLanguage : '')
			)
			.'</li>'
			.'</ul>'
			.'</div>'
			.'<div class="clear"></div>'; 
			
		$this->setSyndicationFeed(wfMsg('Page.RecentChanges.feed-daily-desc'), $this->Title->getLocalUrl('feed=rss&format=daily'.(DekiLanguage::isSitePolyglot() ? '&language='.$feedLanguage : ''))); 
		$this->setSyndicationFeed(wfMsg('Page.RecentChanges.feed-all-desc'), $this->Title->getLocalUrl('feed=rss&format=all'.(DekiLanguage::isSitePolyglot() ? '&language='.$feedLanguage: ''))); 
	}
	
	protected function getFilterForm()
	{		
		$html = '';

		$html = '<form method="get" action="' . $this->Title->getLocalUrl() . '" class="deki-rclanguages deki-rcfilters">';
		
		$html .= '<label class="filter">'. wfMsg('Page.RecentChanges.label.filter') . '</label>';
		
		// build the namespace filtering options
		$options = array();
		foreach (self::$namespaces as $key => $value)
		{
			$options[$key] = wfMsg('Page.RecentChanges.ns.' . $key);
		}
		
		$html .= DekiForm::multipleInput('select', 'namespace', $options, self::DEFAULT_NAMESPACE_FILTER);

		// language filtering
		if (DekiLanguage::isSitePolyglot()) 
		{
			$html .= DekiForm::multipleInput(
				'select',
				'language',
				wfAllowedLanguages(wfMsg('Form.language.filter.all')),
				wfLanguageActive(''),
				null,
				wfMsg('Page.RecentChanges.language-search')
			);
		}
		
		$html .= ' <input type="submit" value="' . wfMsg('Page.RecentChanges.language-submit') . '" />';
		$html .= '</form>';
		
		return $html;
	}

	protected function getData()
	{
		$limit = $this->Request->getInt('limit', $this->itemsPerPage);
		$offset = $this->Request->getInt('offset', 0);
		
		$this->language = $this->Request->getVal('language');
		$this->namespace = $this->Request->getEnum('namespace', array_keys(self::$namespaces), self::DEFAULT_NAMESPACE_FILTER);
		
		$Plug = DekiPlug::getInstance()
			->At('site', 'feed')
			->With('format', 'raw')
			->With('limit', $limit + 1)
			->With('offset', $offset);
		
		// apply the namespace filter
		$namespaceFilter = self::$namespaces[$this->namespace];
		if (!empty($namespaceFilter))
		{
			$Plug = $Plug->With('namespace', $namespaceFilter);
		}
		
		// apply the language filter
		if (!is_null($this->language))
		{
			$Plug = $Plug->With('language', $this->language);
		}
		
		$Result = $Plug->Get();
		
		$changes = array();
		
		if ($Result->isSuccess())
		{
			$changes = $Result->getAll('body/table/change');
			
			if (count($changes) > $limit)
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
		
		$changes = $this->getData();
		if ( count($changes) == 0 )
		{
			return '<p>' . wfMsg('Page.RecentChanges.no-changes-found-matching') . '</p>';
		}
		
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
			if ( $obj->rc_cur_id == '0' )
			{
				$key = $obj->rc_namespace . '-' . $obj->rc_cur_id . '-' .  $obj->rc_id. '-' . $wgLang->date( $obj->rc_timestamp, true );
			}
			else
			{
				$key = $obj->rc_namespace . '-' . $obj->rc_cur_id . '-' . $wgLang->date( $obj->rc_timestamp, true );
			}
			
			if ( $obj->rc_namespace >= 0 &&	$obj->rc_cur_id > 0 &&  isset( $lookup[$key] ) ) 
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
			foreach ($_data as $key => $rc)
			{
				if (empty($rc->mAttribs['isChild']))
				{
					$i++;
				}
				$test = '';
				$date = $wgLang->date($rc->mAttribs['rc_timestamp'], true);
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
				$List->recentChangesLineOld($rc, !empty($rc->mAttribs['wl_user']), $i, $Table);
			}
		}
				
		$paginator = $this->getPaginatorHtml();
		return $Frag->saveHtml() . $paginator;
	}
	
	protected function getPaginatorHtml()
	{
		$html = '';
		
		if ($this->paginate)
		{
			$query = array();

			if (!is_null($this->language))
			{
				$query['language'] = $this->language;
			}
			
			if (!empty($this->namespace))
			{
				$query['namespace'] = $this->namespace;
			}
			
			$Paginator = new DomOffsetPagination(
				$this->Title->getLocalURL(http_build_query($query)),
				$this->itemsPerPage,
				$this->isLastPage
			);
			$html = $Paginator->saveHtml();
		}
		
		return $html;
	}	
	
	protected function outputFeed($feedFormat, $feedLanguage, $displayFormat)
	{
		$feed = new MTFeed('recentchanges', '', 0, 0, $feedFormat, 0, 0, $feedLanguage, $displayFormat);
		$feed->output();
	}
}

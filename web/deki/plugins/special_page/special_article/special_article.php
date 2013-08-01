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

if (defined('MINDTOUCH_DEKI'))
{
	DekiPlugin::registerHook(Hooks::SPECIAL_ARTICLE, array('SpecialArticle', 'output'));
}

class SpecialArticle extends SpecialPagePlugin
{
	protected $name = 'Article';

	static function output($pageName, &$pageTitle, &$html)
	{
		// set the page title
		$pageTitle = wfMsg('Page.Article.backlinks-title');

		$Request = DekiRequest::getInstance();
		$Plug = DekiPlug::getInstance();
		
		// get the request vars
		$pageId = $Request->getVal('pageid');
		if (!$pageId)
		{
			return;
		}
		// make sure a valid operation is specified
		$type = $Request->getVal('type');
		switch ($type)
		{
			case 'backlinks':
				break;
			case 'feed':
				$feedType = $Request->getVal('feedtype', 'pagechanges');
				$feedFormat = $Request->getVal('feedformat', 'rss');
				self::outputFeed($feedType, $feedFormat, $pageId);
				// break;
			default:
				return;
		}

		$direction = $Request->getVal('dir');
		// default 'to'
		$direction = $direction == 'from' ? 'from' : 'to';

		// get the page information
		$Result = $Plug->At('pages', $pageId, 'info')->Get();
		if (!$Result->handleResponse())
		{
			return;
		}

		$Title = Title::newFromText($Result->getVal('body/page/path', ''));
		
		$localKey = 'Page.Article.backlinks-'. $direction;
		$html = '<p>'. wfMsg($localKey, $Title->getLocalUrl(), htmlspecialchars($Result->getVal('body/page/title'))) .'</p>';
	
		// retrieve the page links
		$Result = $Plug->At('pages', $pageId, 'links')->With('dir', $direction)->Get();
		if (!$Result->handleResponse()) 
		{
			return;
		}
		
		$from = $direction == 'from' ? 'outbound' : 'inbound';
		$numLinks = $Result->getVal('body/'.$from.'/@count', 0);
		$links = $Result->getAll('body/'.$from.'/page');

		if ($numLinks > 0 && !is_null($links) && !empty($links))
		{
			$html .= '<ul>';
			foreach ($links as $link)
			{
				$LinkTitle = Title::newFromText($link['path']);	
				$html .= '<li><a href="'. $LinkTitle->getLocalUrl() .'">'. htmlspecialchars($link['title']) .'</a></li>';
			}
			$html .= '<ul>';
		} 
		else 
		{
			$html .= wfMsg('Page.Article.backlinks-none');
		}

		return;
	}
	
	protected static function outputFeed($feedType, $feedFormat, $pageId)
	{
		$Feed = new MTFeed($feedType, '', 0, 0, $feedFormat, 0, $pageId);
		$Feed->output();
	}
}

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
	DekiPlugin::registerHook(Hooks::SPECIAL_EVENTS, array('SpecialEvents', 'output'));
	DekiPlugin::registerHook(Hooks::DATA_GET_SPECIAL_PAGES, array('SpecialEvents', 'getSpecialPagesHook'));
}

class SpecialEvents extends SpecialPagePlugin
{
	protected $pageName = 'Events';


	public static function getSpecialPagesHook(&$pages)
	{
		$Special = new self();
		$name = $Special->getPageTitle();
		$href = $Special->getTitle()->getFullURL();
		
		$pages[$name] = array('name' => $name, 'href' => $href);
	}

	static function output($pageName, &$pageTitle, &$html)
	{
		// set the page title
		$pageTitle = wfMsg('Page.Events.page-title');

		$Request = DekiRequest::getInstance();

		$from = $Request->getVal('from');
		$to = $Request->getVal('to');

		$Title = Title::newFromText('Events', NS_SPECIAL);
		$html = '<form method="get" action="'. $Title->getLocalUrl() .'">' .
					'<p>From: <input name="from" type="text" value="' . htmlspecialchars($from) . '"/> to <input name="to" type="text"  value="' . htmlspecialchars($to) . '"/> ' .
					'<input type="submit" value="View events" /></p>' .
				'</form>';
		$markup = self::getTaggedPagesFromRange($from, $to);
		$html .= $markup;

		return;
	}

	static function getTaggedPagesFromRange($from, $to)
	{
		$html = '';
		
		$tags = DekiTag::getSiteList(DekiTag::TYPE_DATE, null, $from, $to, true);
		
		if( empty($tags) )
		{
			$html .= '<div class="tagresults"><p><strong>' . wfMsg('Page.Tags.no-tags-date-range') . '</strong></p></div>';
		}
		else
		{		
			foreach($tags as $Tag)
			{
				$html .= '<div class="tagresults">';
					$html .= '<h3>' . $Tag->toHtml() . '</h3>';
					$html .= '<ul>';
				
				$pages = DekiTag::getTaggedPages($Tag);
				
				foreach ($pages as $PageInfo)
				{
					$html .= '<li><a href="' . $PageInfo->uriUi . '">' . htmlspecialchars($PageInfo->title) . '</a></li>' . PHP_EOL;
				}
				
				$html.= '</ul></div>';
			}
		}

		return $html;
	}
}

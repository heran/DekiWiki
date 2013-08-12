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
	DekiPlugin::registerHook(Hooks::SPECIAL_POPULAR_PAGES, 'wfSpecialPopularPages');
	DekiPlugin::registerHook(Hooks::DATA_GET_SPECIAL_PAGES, array('SpecialPopularPages', 'getSpecialPagesHook'));
}

function wfSpecialPopularPages($pageName, &$pageTitle, &$html, &$subhtml)
{
	// include the form helper
	DekiPlugin::requirePhp('special_page', 'special_form.php');
	
	$SpecialPage = new SpecialPopularPages($pageName);
	// set the page title
	$pageTitle = $SpecialPage->getPageTitle();
	$SpecialPage->output($html, $subhtml);
}


class SpecialPopularPages extends SpecialPagePlugin
{
	protected $pageName = 'Popularpages';


	public static function getSpecialPagesHook(&$pages)
	{
		$Special = new self();
		$name = $Special->getPageTitle();
		$href = $Special->getTitle()->getFullURL();
		
		$pages[$name] = array('name' => $name, 'href' => $href);
	}

	public function output(&$html, &$subhtml)
	{
		// enforce options
		if (!$this->checkUserAccess())
		{
			return '';
		}
		
		global $wgUser, $wgLang;
		global $wgSpecialPagesCount;
		
		$Request = DekiRequest::getInstance();
		$Plug = DekiPlug::getInstance();
		
		list($limit, $offset) = wfCheckLimits($wgSpecialPagesCount);		
		$Plug = $Plug->At('pages', 'popular')->With('limit', $limit)->With('offset', $offset);
		
		$language = $Request->getVal('language');
		if (!empty($language))
		{
			$Plug = $Plug->With('language', $language);
		}
		
		$Result = $Plug->Get();
		if (!$Result->handleResponse())
		{
			return;
		}
		$pages = $Result->getAll('body/pages.popular/page', array());
		if (empty($pages) || $Result->getVal('body/pages.popular/@count') == 0)
		{
			// no output if there are no popular pages
			return true;
		}
		$sk = $wgUser->getSkin();

		$html = '<ol>';
		foreach ($pages as &$page) 
		{
			$nt = Title::newFromText($page['path']);
			$html .= '<li>'.$sk->makeKnownLinkObj($nt, htmlspecialchars($page['title'])).' ('.wfMsg('Page.PopularPages.count-views', $wgLang->formatNum( $page['metrics']['metric.views'] )).')</li>';
		}
		unset($page);
		$html .= '</ol>';
		
		$subhtml = SpecialPageForm::getLanguageFilter($this->getTitle());
	}
}

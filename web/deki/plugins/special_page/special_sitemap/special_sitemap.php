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
	DekiPlugin::registerHook(Hooks::SPECIAL_SITEMAP, 'wfSpecialSiteMap');
	DekiPlugin::registerHook(Hooks::DATA_GET_SPECIAL_PAGES, array('SpecialSiteMap', 'getSpecialPagesHook'));
}

function wfSpecialSiteMap($pageName, &$pageTitle, &$html)
{
	$SpecialPage = new SpecialSiteMap($pageName);
	// set the page title
	$pageTitle = $SpecialPage->getPageTitle();
	$html = $SpecialPage->output();
}

class SpecialSiteMap extends SpecialPagePlugin
{
	protected $pageName = 'Sitemap';

	
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
		
		global $wgDekiPlug, $wgOut, $wgTitle;
		global $wgLanguagesAllowed;


		if (!empty($wgLanguagesAllowed)) 
		{
			$html .= '<form method="get" action="'.$wgTitle->getLocalUrl().'">'
				.wfMsg('Page.SiteMap.language-search')
				.' '.wfSelectForm('language', wfAllowedLanguages(wfMsg('Page.SiteMap.all-languages')), wfLanguageActive(''))
				.' <input type="submit" value="'.wfMsg('Page.SiteMap.language-submit').'" /></form>';
		}

		$r = $wgDekiPlug->At('pages')->With('format', 'html');

		$language = wfLanguageActive();
		if (!empty($language)) {
			$r = $r->With('language', $language);	
		}
		$r = $r->Get();
		if (!MTMessage::HandleFromDream($r)) {
			return '';	
		}
		global $wgOut;

		//hack for now
		if (strpos($r['type'], 'iso-8859-1') !== false) {
			$r['body'] = utf8_encode($r['body']);
		}

		$html .= $r['body'];
		
		return $html;
	}
}

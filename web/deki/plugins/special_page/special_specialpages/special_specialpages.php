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

if (defined('MINDTOUCH_DEKI')) :

DekiPlugin::registerHook(Hooks::SPECIAL_SPECIAL_PAGES, array('SpecialSpecialPages', 'hook'));


class SpecialSpecialPages extends SpecialPagePlugin
{
	protected $pageName = 'SpecialPages';

	public static function hook($pageName, &$pageTitle, &$html, &$subhtml)
	{
		$Special = new self($pageName, basename(__FILE__, '.php'));
		
		$pageTitle = $Special->getPageTitle();
		
		$Special->execute($html, $subhtml);
	}
	
	protected function execute(&$html, &$subhtml)
	{
		// grab a list of the registered special pages
		$pages = array();
		DekiPlugin::executeHook(Hooks::DATA_GET_SPECIAL_PAGES, array(&$pages));
		
		ksort($pages);
		
		$html .= '<ul>';
		foreach ($pages as $page)
		{
			$html .= '<li><a href="'. $page['href'] .'">'. htmlspecialchars($page['name']) . '</a></li>';
		}
		$html .= '</ul>';
		
		if (DekiUser::getCurrent()->isAdmin())
		{
			// loaded plugins
			global $wgDekiSpecialPages, $wgDefaultDekiSpecialPages;
			$html .= '<h2>'. wfMsg('Page.SpecialPages.data.title') .'</h2>';
			$Table = new DomTable();
			
			$Table->addRow();
			$Table->addHeading(wfMsg('Page.SpecialPages.data.plugins'));
			$Table->addHeading(wfMsg('Page.SpecialPages.data.special-pages'));
			
			$specialPages = array_merge($wgDefaultDekiSpecialPages, $wgDekiSpecialPages);
			$plugins = DekiPlugin::getEnabledSitePlugins();
			foreach ($specialPages as $specialPage)
			{
				$plugin = array_shift($plugins);
				$Table->addRow();
				$Table->addCol($plugin);
				$Table->addCol($specialPage);
			}

			// add any remaining plugins
			foreach ($plugins as $plugin)
			{
				$Table->addRow();
				$Table->addCol($plugin);
				$Table->addCol('&nbsp;');
			}
			
			$html .= $Table->saveHtml();
			//$html .= print_r($wgDekiSpecialPages, 1);
			// verbose hook display
			//$html .= print_r(DekiPlugin::$hooks, 1);
		}
		
		$html .= '</pre>';
	}
}

endif;

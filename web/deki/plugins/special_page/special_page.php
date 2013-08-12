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

abstract class SpecialPageDispatchPlugin extends DekiPlugin
{
	public static function init()
	{
		DekiPlugin::registerHook(Hooks::SPECIAL_PAGE, array('SpecialPageDispatchPlugin', 'dispatch'), 10);
	}
	
	public static function dispatch($pageName)
	{
		global $wgOut;
		global $IP, $wgDekiPluginPath, $wgDefaultDekiSpecialPages, $wgDekiSpecialPages;
		global $wgBlockedNamespaces;
		
		// load the special pages
		self::loadSpecialPages();

		// make sure we don't try to call the special event again
		$hook = is_null($pageName) || ($pageName == '') ? null : 'Special:'. $pageName;

		if (is_null($hook))
		{
			// default to the Special:SpecialPages hook
			$hook = Hooks::SPECIAL_SPECIAL_PAGES;
		}

		// prepare args to pass to plugin
		$pageTitle = '';
		$html = '';
		
		// hack, temporarily block the special namespace to avoid showing edit details
		$lastBlockedNamespaces = $wgBlockedNamespaces;
		$wgBlockedNamespaces[] = NS_SPECIAL;

		// attempt displaying the special page
		$result = DekiPlugin::executeHook($hook, array($pageName, &$pageTitle, &$html, &$subhtml));
		if ($result == DekiPlugin::HANDLED || $result == DekiPlugin::HANDLED_HALT)
		{
			$isPopup = DekiRequest::getInstance()->getBool('popup', false);
			if ($isPopup)
			{
				self::renderSpecialPagePopup($pageName, $pageTitle, $html, $subhtml);

				// execution is halted to stop $wgOut from processing the request
				exit();
			}

			self::includeCss('special_page', 'special_page_form.css');
			$wgOut->setPageTitle($pageTitle);
			$wgOut->setArticleRelated(false);
			
			// bugfix #7228: Special page handler should add a parent node
			$safeName = str_replace(array('<', '>'), '', $pageName);
			$html = '<div id="Special' . $safeName . '">'. $html . '</div>';

			$wgOut->addHtml($html);
			$wgOut->addSubNavigation($subhtml);

			// don't process anymore plugins
			return DekiPlugin::HANDLED_HALT;
		}
		else
		{
			// restore the previous block setting
			$wgBlockedNamespaces = $lastBlockedNamespaces;

			// unhandled/unknown special page, no PHP plugins for this request
			return DekiPlugin::UNHANDLED;
		}
	}
	
	/**
	 * Load all special page plugins
	 * @return N/A
	 */
	public static function loadSpecialPages() 
	{
		global $IP, $wgDekiPluginPath, $wgDefaultDekiSpecialPages, $wgDekiSpecialPages;
			
		$pluginDirectory = $IP .'/'. $wgDekiPluginPath .'/'. basename(__FILE__, '.php');
		$pluginList = array_merge($wgDefaultDekiSpecialPages, $wgDekiSpecialPages);
		// process the special page plugins
		DekiPlugin::loadFromArray($pluginDirectory, $pluginList);
	}
	
	/**
	 * Renders the request as a special page popup
	 * @param string $pageName
	 * @param string $pageTitle
	 * @param string &$html
	 * @param string &$subhtml
	 * @return
	 */
	protected static function renderSpecialPagePopup($pageName, $pageTitle, &$html, &$subhtml)
	{
		global $wgOut, $wgMimeType, $wgOutputEncoding, $wgContLanguageCode;
		
		// grab the folder name
		$pluginFolder = basename(dirname(__FILE__));
		$View = new DekiPluginView($pluginFolder, 'special_page_popup', DekiPluginView::NO_VIEW_FOLDER);
		
		// configure view variables
		$View->set('lang', $wgContLanguageCode);
		$View->set('dir', '');
		$View->set('head.mimetype', $wgMimeType);
		$View->set('head.charset', $wgOutputEncoding);
		
		$View->set('head.title', $wgOut->getPageTitle());
		$View->set('head.commonpath', Skin::getCommonPath());
		
		$View->set('head.includes.css', DekiPlugin::getCssIncludes());
		$View->set('head.includes.javascript', DekiPlugin::getJavascriptIncludes());
		
		$js = '$(function() {' . "\n" . MTMessage::ShowJavascript(false) . "\n" . '});' . "\n";
		$View->setRef('head.javascript', $js);
		
		$View->setRef('contents', $html);
		$View->set('messages', MTMessage::output() . FlashMessage::output());
		
		// start popup output
		// see bug #4471; this is needed for loading contents in an IFRAME for IE7
		header('P3P:CP="IDC DSP COR ADM DEVi TAIi PSA PSD IVAi IVDi CONi HIS OUR IND CNT"');
		
		header('Content-type: ' . $wgMimeType . '; charset=' . $wgOutputEncoding);
		header('Content-language: ' . $wgContLanguageCode);
		echo $View->render();
	}
}

SpecialPageDispatchPlugin::init();

endif;

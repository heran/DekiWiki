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

class MindTouchPageTemplateSelectorPlugin extends SpecialPagePlugin
{
	const PLUGIN_FOLDER = 'page_template_selector';
	const SPECIAL_PAGE = 'PageTemplateSelector';

	/**
	 * Initialize the plugin and hooks into the application
	 */
	public static function load()
	{
		DekiPlugin::registerHook(Hooks::SPECIAL_PAGE . self::SPECIAL_PAGE, array(__CLASS__, 'specialPopupHook'));
		DekiPlugin::registerHook(Hooks::SKIN_OVERRIDE_VARIABLES, array(__CLASS__, 'renderSkinHook'));
	}
	
	/**
	 * Attaches javascript handler for new page
	 * 
	 * @param $Template
	 * @return
	 */
	public static function renderSkinHook(&$Template)
	{
		global $wgUser, $wgArticle, $wgTitle;
		
		if (!$wgArticle->userCanCreate())
		{
			return;
		}

		// no wizard
		if ($wgTitle->getNamespace() == NS_TEMPLATE || $wgTitle->getNamespace() == NS_SPECIAL)
		{
			return;
		}

		// rebuild popup link
		$sk = $wgUser->getSkin();
		
		// don't show wizard on brand-new pages; have default (disabled) ux
		if ($sk->isNewPage())
		{
			return;
		}
		
		$PopupTitle = Title::newFromText(self::SPECIAL_PAGE, NS_SPECIAL);
		$popupHref = $PopupTitle->getLocalUrl('pageId=' . $wgArticle->getId());

		// insert popup
		$title = wfEncodeJSHTML(wfMsg('Page.PageTemplateSelector.label.title'));
		$onclick = "return Deki.Plugin.PageTemplateSelector.ShowPopup('". $title ."', '".  wfEncodeJSHTML($popupHref) ."');";

		// set onclick and regenerate original link
		$sk->onclick->pageadd = $onclick;
		$link = '<a '.
			'href="' . $sk->href->pageadd . '" '.
			'title="' . htmlspecialchars(wfMsg('Skin.Common.new-page')) . '" ' .
			'onclick="' . $sk->onclick->pageadd . '" ' .
			'class="' . $sk->cssclass->pageadd . '">'.
			'<span></span>' . wfMsg('Skin.Common.new-page') .
		'</a>';

		$Template->set('pageadd', $link);
	}
	
	/**
	* Create HTML content for embedded template selector
	*/
	public static function renderEmbedView()
	{
		$View = self::CreateView(self::PLUGIN_FOLDER, 'embed');
		DekiPlugin::includeCss(self::PLUGIN_FOLDER, 'page_template_selector_popup.css');
		self::populateView($View);
		return $View->render(); 
	}

	/**
	 * Create content for special page
	 * 
	 * @param string $pageName - incoming page name
	 * @param string $pageTitle - page title to set
	 * @param string $html - html to output
	 * @return N/A
	 */
	public function specialPopupHook($pageName, &$pageTitle, &$html)
	{
		$Request = DekiRequest::getInstance();
		
		// @note kalida: only support popup ux for now
		if (strcasecmp($Request->getVal('popup'), 'true') != 0)
		{
			self::redirectHome();
			return;
		}

		$html = '';
		$View = self::createView(self::PLUGIN_FOLDER, 'popup');

		$Title = Title::newFromId($Request->getVal('pageId'));
		
		// edge case, if supplied bad pageId
		if (is_null($Title))
		{
			self::redirectHome();
			return;
		}
		
		$baseuri = $Title->getFullUrl('action=addsubpage');
		$View->set('selector.embed', self::renderEmbedView());
		$helpLink = '<a href="'.ProductURL::PAGE_SELECTOR.'" target="_blank">' . wfMsg('Page.PageTemplateSelector.page.templates.help') . '</a>';
		$View->set('templates.help', $helpLink);
		$View->set('templates.baseuri', $baseuri);
		$html .= $View->render();
	}
		
	/**
	* Populate view with details for template
	*
	* @param $View - view to populate
	* @return N/A
	*/
	private static function populateView($View)
	{
		global $wgDekiPluginPath;
		
		$blankIcon = $wgDekiPluginPath . '/' . self::PLUGIN_FOLDER . '/icons/page-blank.png';
		$defaultTemplate = self::renderPageTemplateItem('', wfMsg('Page.PageTemplateSelector.page.blank'), '', $blankIcon);
		$View->set('templates.default', $defaultTemplate);
		
		$pages = array();
		$Result = DekiTemplateProperties::getTemplateXml($pages, DekiTemplateProperties::TYPE_PAGE, wfLanguageActive());
		// bubble any errors
		$Result->handleResponse();
		
		$renderedTemplates = array();

		foreach ($pages as $page)
		{
			$PageInfo = DekiPageInfo::newFromArray($page);

			$Properties = DekiTemplateProperties::newFromArray($page);
			$screenshot = $Properties->getScreenshotHref();
			$description = $Properties->getDescription(null);

			// get path without namespace prefix
			$Title = Title::newFromId($PageInfo->id);
			
			// set anchor with template path
			$uri = '#' . $Title->getPartialUrl();

			$renderedTemplates[] = self::renderPageTemplateItem($uri, $PageInfo->title, $description, $screenshot);
		}
		
		$View->setRef('templates.rendered', $renderedTemplates);
		// include blank template in count
		$View->set('templates.available', wfMsg('Page.PageTemplateSelector.page.templates.available', count($renderedTemplates) + 1));
	}
	
	/**
	 * Create html for a single page template item
	 * 
	 * @param $uri - uri to create item 
	 * @param $title - display title
	 * @param $description - display description
	 * @param $screenshotHref - uri to screenshot to use (if null, then default)
	 * @return string - html to display
	 */
	private static function renderPageTemplateItem($uri, $title, $description, $screenshotHref = null)
	{
		global $wgDekiPluginPath;
		$html = '';

		// dummy item used to store link template
		$html .= '<a href="' . $uri . '" class="page-item"></a>';
		$html .= '<div class="screenshot">';

		$defaultIcon = $wgDekiPluginPath . '/' . self::PLUGIN_FOLDER . '/icons/page-default.png';
		$screenshotHref = is_null($screenshotHref) ? $defaultIcon : $screenshotHref;
		$html .= '<img src="' . $screenshotHref . '" />';

		$html .= '</div>';

		$html .= '<div class="details">';
		$html .= '<h2>' . htmlspecialchars($title) . '</h2>';

		$html .= '<p>' . htmlspecialchars($description) . '</p>';

		$html .= '</div>';

		return $html;
	}
}

// initialize the plugin
MindTouchPageTemplateSelectorPlugin::load();

endif;


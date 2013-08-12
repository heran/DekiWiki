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

class SpecialListTemplates extends SpecialPagePlugin
{
	// template choices; first is default in dropdown
	private static $VALID_TYPES = array(
		DekiTemplateProperties::TYPE_DEFAULT,
		DekiTemplateProperties::TYPE_PAGE,
		DekiTemplateProperties::TYPE_CONTENT
	);
	
	const SPECIAL_EDIT_TEMPLATE = 'Special:EditTemplate';
	const AJAX_FORMATTER = 'ListTemplates';
	protected $pageName = 'ListTemplates';

	public static function init()
	{
		DekiPlugin::registerHook(self::SPECIAL_EDIT_TEMPLATE, array(__CLASS__, 'specialHookEditTemplate'));
		DekiPlugin::registerHook(Hooks::SPECIAL_LIST_TEMPLATES, array(__CLASS__, 'specialHook'));
		DekiPlugin::registerHook(Hooks::DATA_GET_SPECIAL_PAGES, array(__CLASS__, 'getSpecialPagesHook'));
		DekiPlugin::registerHook(Hooks::AJAX_FORMAT . self::AJAX_FORMATTER, array(__CLASS__, 'ajaxHook'));
	}
	
	/**
	 * Default AJAX handler for this special page
	 * @param string $body
	 * @param string $message
	 * @param string $success
	 * @return N/A
	 */
	public static function ajaxHook(&$body, &$message, &$success)
	{
		$Request = DekiRequest::getInstance();
		$action = $Request->getVal('action');
		
		$body = '';
		$success = false;

		switch ($action)
		{
			case 'refresh':
				$Special = new self();
				$body = $Special->output(true);
				$success = true;
				break;
			default:
				$message = 'Invalid action specified';
				break;
		}
	}
	
	/**
	 * Add a this special page to a list of special pages
	 * @param array $pages
	 * @return N/A
	 */
	public static function getSpecialPagesHook(&$pages)
	{
		$Special = new self();
		$name = $Special->getPageTitle();
		$href = $Special->getTitle()->getFullURL();
		
		$pages[$name] = array('name' => $name, 'href' => $href);
	}
	
	public static function specialHook($pageName, &$pageTitle, &$html, &$subhtml)
	{
		$Special = new self($pageName, basename(__FILE__, '.php'));

		// set the page title
		$pageTitle = $Special->getPageTitle();
		$html = $Special->output();
	}
	
	// @note kalida: create parallel methods for outputting the popup
	public static function specialHookEditTemplate($pageName, &$pageTitle, &$html, &$subhtml)
	{
		$Special = new self($pageName, basename(__FILE__, '.php'));

		// set the page title
		$pageTitle = $Special->getPageTitle();
		$html = $Special->outputEditTemplate();
	}
	
	public function outputEditTemplate()
	{
		global $wgUser, $wgTitle;
		
		$Request = DekiRequest::getInstance();
		
		$pageId = $Request->getVal('pageId');
		$html = '';
		$PageInfo = DekiPageInfo::loadFromId($pageId);
		$queryString = 'popup=' . $Request->getBool('popup') . '&pageId=' . $pageId;
		
		if (is_null($PageInfo))
		{
			$html = wfMsg('Page.EditTemplate.error.page');
			return $html;
		}
				
		$Properties = new DekiTemplateProperties($pageId);
		$Title = Title::newFromId($pageId);
		$js = '';
		
		// better way to get current folder?
		$View = $this->createView($this->specialFolder .'/'. $this->fileName, 'popup');
		
		if ($Request->isPost() && $this->handleEditTemplatePost($PageInfo, $Properties))
		{
			// successful update -- close popup and show message in parent
			$js .= 'parent.Deki.Ui.Flash("' . wfEncodeJSString(wfMsg('Page.EditTemplate.popup.update.success')) . '");' . "\n";
			$js .= 'parent.Deki.Plugin.SpecialListTemplates.Refresh();' . "\n";
			$js .= 'Deki.QuickPopup.Hide();';
			
			$View->set('form.js', $js);
		}
		
		// submit back to popup page
		$View->set('form.action', $wgTitle->getLocalUrl($queryString));
		
		// @note kalida: use params like template.title to avoid overlap with regular title param
		$View->set('form.template.url', $PageInfo->uriUi);
		$View->set('form.template.title', $PageInfo->title);
		$View->set('form.template.description', $Properties->getDescription(null));
		
		// construct template choices
		$templates = array();
		foreach (self::$VALID_TYPES as $templateType)
		{
			$key = $templateType;
			$value = wfMsg('Page.EditTemplate.type.' . $templateType);
			$help = wfMsg('Page.EditTemplate.type.' . $templateType . '.help');
					
			$templates[$key] = $value . ' (' . $help . ')';
		}
		$View->set('form.types', $templates);
		$View->set('form.template.type', $Properties->getType());
		
		$View->set('form.template.language', $Properties->getLanguage());
		$View->set('form.languages', wfAllowedLanguages(wfMsg('Form.language.filter.all'), 'all'));
		
		$this->includeSpecialCss('special_listtemplates.css');
		
		$html .= $View->render();
		return $html;
	}
		
	/**
	 * Generate list of templates with link to edit details
	 * @return N/A
	 */
	public function output($ajaxRequest = false)
	{
		global $wgDekiPlug, $wgUser, $wgArticle;
		
		if (!$ajaxRequest)
		{
			$this->includeSpecialCss('special_listtemplates.css');
			$this->includeSpecialJavascript('special_listtemplates.js');
		}
		
		$html = '';
		$RootTitle = Title::newFromText(DekiNamespace::getCanonicalName(NS_TEMPLATE) . ':');
		
		// find all templates, starting from root template page
		$Result = DekiPlug::getInstance()->At('pages', $RootTitle->getArticleId(), 'tree')->Get();
		if (!$Result->handleResponse())
		{
			$html = wfMsg('Page.ListTemplates.page-has-no-text');
			return $html;
		}

		$pages = $Result->getAll('body/pages/page');
		$allpages = array();
		self::collapsePages($pages, $allpages);

		// first item is root template page itself
		unset($allpages[0]);

		// make uri with submit action (becomes save action when routed through index.php)
		$html .= $this->renderTemplateForm($allpages, $wgArticle->getTitle()->getLocalUrl('action=submit'));
		
		return $html; 
	}
	
	/**
	 * Combine array of pages (with subpages) into a single array
	 * @param array $pages - array of pages, with subpages
	 * @param array $allpages - empty initial array to be populated
	 */
	protected static function collapsePages($pages, &$allpages)
	{
		foreach ($pages as &$page)
		{
			$allpages[] = $page;

			if (isset($page['subpages']) && !empty($page['subpages']))
			{
				$data = $page['subpages'];
				$Subpages = new XArray($data);

				self::collapsePages($Subpages->getAll('page'), $allpages);
			}
		}
		unset($page);
	}
	
	/*
	 * Process incoming post request for edit template popup (displays DekiMessage errors/success)
	 * @param DekiPageInfo $PageInfo
	 * @param DekiTemplateProperties $Properties
	 * @return mixed - true if properly updated, and should redirect to blank page
	 */
	protected function handleEditTemplatePost(&$PageInfo, &$Properties)
	{
		global $wgUser;
		$Request = DekiRequest::getInstance();
		
		if (!$wgUser->isAdmin())
		{
			return false;
		}
		
		// don't allow empty title
		if ($Request->has('template.title'))
		{
			$title = $Request->getVal('template.title');
			$Result = DekiPageInfo::move($PageInfo, null, $title);
			if (!$Result->handleResponse())
			{
				return false;
			}
		}
		
		$Properties->setDescription($Request->getVal('template.description'));
	
		$type = $Request->getVal('template.type');
		if (in_array($type, self::$VALID_TYPES))
		{
			$Properties->setType($type);
		}
		
		// setting all languages means removing property
		$language = $Request->getVal('template.language') == 'all' ? '' : $Request->getVal('template.language');
		$Properties->setLanguage($language);
		
		$Result = $Properties->update();
		return $Result->handleResponse();
	}
	
	/**
	 * Generate html form used to change template categories
	 * @param array $pages - array of pages to render (raw api results)
	 * @param string $uri - uri for form submit
	 * @return string - html to display the form
	 */
	protected function renderTemplateForm($pages, $uri)
	{
		global $wgUser;
		
		$html = '';
		$Table = new DomTable();

		$Table->addRow();
		$Table->addHeading(wfMsg('Page.ListTemplates.name'));
		$Table->addHeading(wfMsg('Page.ListTemplates.language'));
		$Table->addHeading(wfMsg('Page.ListTemplates.type'));
		
		if ($wgUser->isAdmin())
		{
			$Table->addHeading('');
		}
		
		$count = 0;
		$classes = array('bg1', 'bg2');
		
		// popup link
		$Title = Title::newFromText(self::SPECIAL_EDIT_TEMPLATE);
		foreach ($pages as $page)
		{
			$Page = DekiPageInfo::newFromArray($page);
			$TemplateProperties = DekiTemplateProperties::newFromArray($page);

			$language = $TemplateProperties->getLanguage();
			$type = $TemplateProperties->getType();

			$Tr = $Table->addRow();
			$Tr->setAttribute('id', 'deki-pagetemplate-' . $Page->id);
			
			$tdhtml = '';

			// indent based on number of subdirectories in path, excluding first
			$encoded = str_replace('//', '%2f', $Page->getPath());			
			$indent = count(explode('/', $encoded)) - 1;
			
			// give children same background as a parent
			if ($indent == 0)
			{
				$count += 1;
			}
			
			$Tr->setAttribute('class', $classes[$count % 2]);
			
			for ($i = 0; $i < $indent; $i++)
			{
				$class = ($i == $indent - 1) ? 'pagetemplates-indent-last' : 'pagetemplates-indent';
				$tdhtml .= '<div class="'. $class . '"></div>';
			}
			
			$tdhtml .= '<a href="' . $Page->uriUi . '">' . htmlspecialchars($Page->title) . '</a>';
			$Td = $Table->addCol($tdhtml);
			
			// language column
			$tdhtml = '';
			if (!empty($language))
			{
				$tdhtml .= $language;
			}
			$Td = $Table->addCol($tdhtml);

			// type column
			$tdhtml = '';
			$typeDisplay = ($type == DekiTemplateProperties::TYPE_DEFAULT) ? '' : wfMsg('Page.EditTemplate.type.' . $type);
			$tdhtml .= $typeDisplay;
			$Td = $Table->addCol($tdhtml);
			
			// action column
			if ($wgUser->isAdmin())
			{
				$tdhtml = '';
				$popupUrl = $Title->getLocalUrl('pageId=' . $Page->id);
				$popupTitle = wfMsg('Page.EditTemplate.label.properties.edit');
			
				$tdhtml .= ' <a href="' . $popupUrl . '" class="edit-template" . title="' . $popupTitle . '">' . wfMsg('Page.ListTemplates.label-action') . '</a>';
			
				$Td = $Table->addCol($tdhtml);
			}
		}

		$html .= $Table->saveHtml();
		return $html;
	}	
}
// initialize the special page plugin
SpecialListTemplates::init();

endif;
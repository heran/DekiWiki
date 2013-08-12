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

class EditorPlugin extends DekiPlugin
{
	/**
	 * Formatters
	 */
	const AJAX_FORMATTER = 'page_editor';
	const CONFIG_FORMATTER = 'page_editor_config';
	const STYLES_FORMATTER = 'page_editor_styles';

	/**
	 * Register hooks
	 */
	public static function init()
	{
		DekiPlugin::registerHook(Hooks::MAIN_PROCESS_OUTPUT, array(__CLASS__, 'renderHook'));
		DekiPlugin::registerHook(Hooks::SKIN_OVERRIDE_VARIABLES, array(__CLASS__, 'skinHook'));
		DekiPlugin::registerHook(Hooks::AJAX_FORMAT . self::AJAX_FORMATTER, array(__CLASS__, 'ajaxHook'));
		DekiPlugin::registerHook(Hooks::AJAX_FORMAT . self::CONFIG_FORMATTER, array(__CLASS__, 'configHook'));
		DekiPlugin::registerHook(Hooks::AJAX_FORMAT . self::STYLES_FORMATTER, array(__CLASS__, 'stylesHook'));
	}

	/**
	 * Rebuild edit page link
	 * 
	 * @param object $Template
	 */
	public static function skinHook(&$Template)
	{
		global $wgArticle, $wgTitle, $wgUser;

		$Request = DekiRequest::getInstance();

		$Skin = $wgUser->getSkin();

		if ($wgArticle && $wgArticle->userCanEdit())
		{
			$Skin->onclick->pageedit = 'Deki.Plugin.Publish(\'Editor.load\'); return false;';
			$Skin->href->pageedit = $wgTitle->getLocalUrl('action=edit'.($Request->getVal('redirect') == 'no'? '&redirect=no': ''));
			$Skin->cssclass->pageedit = '';
		}
		else
		{
			$Skin->cssclass->pageedit = 'disabled';
			$Skin->onclick->pageedit = 'return false';
			$Skin->href->pageedit = '#';
			$Skin->cssclass->pageedit = 'disabled';
		}

		$Template->set('pageedit',
				'<a href="' . $Skin->href->pageedit .
				'" title="' . htmlspecialchars(wfMsg('Skin.Common.edit-page')) .
				'" onclick="' . $Skin->onclick->pageedit .
				'" class="' . $Skin->cssclass->pageedit .
				'"><span></span>' . wfMsg('Skin.Common.edit-page') . '</a>'
		);
	}

	/**
	 * Add inline JavaScript to the page with editor related vars
	 * 
	 * @param object $wgOut
	 */
	public static function renderHook($wgOut)
	{
		global $wgArticle, $wgUser, $wgTitle, $wgContLang, $wgLanguageCode, $wgServer;

		$Request = DekiRequest::getInstance();

		$html = "\n";
		$html .= '<script type="text/javascript">' . "\n";

		if ($wgArticle->getId() > 0 && $wgArticle->userCanEdit() && Skin::isViewPage())
		{
			$html .= 'Deki.$(document).ready(function() { Deki.Plugin.Editor.HookEditSection() });' . "\n";
		}

		if (Skin::isEditPage())
		{
			$Request = DekiRequest::getInstance();
			if ($wgArticle->getId() == 0 && $wgArticle->userCanCreate()
				|| $wgArticle->getId() > 0 && $wgArticle->userCanEdit()
				|| $Request->getVal('action') == 'source')
			{
				$html .= 'Deki.$(document).ready(function() { Deki.Plugin.Publish("Editor.load", [null, "' . $Request->getVal('action') . '"]); });' . "\n";
			}
		}

        if ($Request->getVal('wpNewPath'))
		{
			$titleFull = Article::combineName($Request->getVal('wpNewPath'), $Request->getVal('wpNewTitle'));
		}
		else
		{
			$titleFull = $wgTitle->getPrefixedText();
		}

		$Title = Title::newFromText($titleFull);

		if (is_null($Title))
		{
			$Title = $wgTitle;
			$titleFull = $Title->getPrefixedText();
		}

		Article::splitName($titleFull, $titlePath, $titleName);

		$titleName = wfDecodeTitle($titleName);

		if ($titlePath == '')
		{
			$titlePath = HPS_SEPARATOR;
		}
		
		$html .= "\n";

		if (Skin::isNewPage())
		{
			$cancel_url = wfEncodeJSString($wgArticle->getParentRedirectURL());

			if (empty($cancel_url) && isset($_SERVER['HTTP_REFERER']) && !empty($_SERVER['HTTP_REFERER']) && !empty($_SERVER['HTTP_HOST']))
			{
				// prevent xss attacks here
				$urls = parse_url($_SERVER['HTTP_REFERER']);
				if (strcasecmp($urls['host'], $_SERVER['HTTP_HOST']) == 0)
				{
					$cancel_url = $_SERVER['HTTP_REFERER'];
				}
			}
			// used to cancel editor on new pages
			$html .= "Deki.CancelUrl = '" . str_replace("'", "\'", $cancel_url) . "';" . "\n";
		}
		elseif ($Request->getVal('action') == 'source' || $Request->getVal('action') == 'edit' || $Request->getVal('action') == 'submit')
		{
			// used to cancel editor
			// if page is opened for edit via address line
			// e.g.: index.php?title=&action=edit
			$cancel_url = wfEncodeJSString($wgArticle->getTitle()->getFullURL());
			$html .= "Deki.CancelUrl = '" . str_replace("'", "\'", $cancel_url) . "';" . "\n";
		}

		global $wgLocalCssPath;

		if (empty($titleName))
		{
			$titleName = wfHomePageTitle();
		}

		$html .= "Deki.BaseHref = '" . $wgServer . "';" . "\n";

		$html .= 'Deki.PageId = ' . $Title->getArticleID() . ';' . "\n";
		$html .= "Deki.PageTitle = '" . wfEncodeJSString($titleFull) . "';" . "\n";
		$html .= "Deki.PageLanguageCode = '" . wfEncodeJSString($wgLanguageCode) . "';" . "\n";
		$html .= "Deki.FollowRedirects = " . ($Request->getVal('redirect') == 'no' ? '0' : '1') . ";" . "\n";

		// path to current skin template (e.g. /skins/ace). Isn't used now.
		$html .= "Deki.PathTpl = '" . wfEncodeJSString(Skin::getTemplatePath()) . "';" . "\n";
		// path to current skin (e.g. /skins/ace/blue). Isn't used now.
		$html .= "Deki.PathSkin = '" . wfEncodeJSString(Skin::getSkinPath()) . "';" . "\n";
		// path to common dir (/skins/common)
		$html .= "Deki.PathCommon = '" . wfEncodeJSString(Skin::getCommonPath()) . "';" . "\n";

		// used for autoreplase of ~~~ and ~~~~ in the editor
		$html .= "Deki.UserName = '" . htmlspecialchars(wfEncodeJSString($wgUser->getName())) . "';" . "\n";
		$html .= "Deki.Today = '" . $wgContLang->date(date('YmdHis'), false) . "';" . "\n";

		$isAnonymous = $wgUser->isAnonymous() ? 'true' : 'false';
		$html .= "Deki.UserIsAnonymous = " . $isAnonymous . ";" . "\n";

		$editorConfigCrc = dechex(crc32(self::getConfigCache()->getCache()->getEtag()));
		$editorStylesCrc = dechex(crc32(self::getStylesCache()->getCache()->getEtag()));
		$html .= "Deki.EditorConfigToken = '" . $editorConfigCrc . "';" . "\n";
		$html .= "Deki.EditorStylesToken = '" . $editorStylesCrc . "';" . "\n";
		
		$userCanEdit = $wgArticle->userCanEdit() ? 'true' : 'false';
		$html .= "Deki.PageEditable = " . $userCanEdit . ";" . "\n";

		$html .= '</script>' . "\n";

		$wgOut->addHeadHTML($html);
	}

	/**
	 * Hook for AJAX requests
	 * 
	 * @param string $body
	 * @param string $message
	 * @param boolean $success
	 */
	public static function ajaxHook(&$body, &$message, &$success)
	{
		$Request = DekiRequest::getInstance();
		$method = $Request->getVal('method');

		switch ($method)
		{
			default:
			case 'load':
				$body = self::load();
				$success = true;
				break;
			case 'checkPermissions':
				self::checkPermissions($body, $message, $success);
				break;
			case 'pageTimestamp':
				self::pageTimestamp($body, $message, $success);
				break;
		}
	}

	/**
	 * Output editor configuration
	 * 
	 * @param string $body
	 * @param string $message
	 * @param boolean $success
	 */
	public static function configHook(&$body, &$message, &$success)
	{
		$Cache = self::getConfigCache();
		
		self::sendCacheHeaders();

		// create the cache file
		$Cache->process();
	}
	
	private static function getConfigCache()
	{
		require_once ('libraries/ui_handlers.php');

		// create an instance of a remote etag cache
		$Cache = new RemoteCacheHandler(EtagCache::TYPE_JAVASCRIPT); // charset=utf-8
		// site properties are required for the custom fck config
		$SiteProperties = DekiSiteProperties::getInstance();

		// get the user config from site properties, check the etag
		$etag = $SiteProperties->getFckConfigEtag();
		if (!is_null($etag))
		{
			$uri = $SiteProperties->getFckConfigUri();
			$Cache->addResouce($uri, $etag);
		}

		$jsFiles = array();
		DekiPlugin::executeHook(Hooks::EDITOR_CONFIG, array(&$jsFiles));

		foreach ($jsFiles as $file)
		{
			$Cache->addFile($file);
		}
		
		return $Cache;
	}

	/**
	 * Output css styles for editor content
	 *
	 * @param string $body
	 * @param string $message
	 * @param boolean $success
	 */
	public static function stylesHook(&$body, &$message, &$success)
	{
		$Cache = self::getStylesCache();
		
		self::sendCacheHeaders();

		// create the cache file
		$Cache->process();
	}
	
	private static function getStylesCache()
	{
		global $IP, $wgLocalCssPath, $wgStylePath;

		require_once ('libraries/ui_handlers.php');

		// create an instance of a remote etag cache
		$Remote = new RemoteCacheHandler(EtagCache::TEXT_CSS, 'iso-8859-1');

		if ($wgLocalCssPath == $wgStylePath . '/common/custom_css.php')
		{
			// check if any custom css should be included by fetching the etag
			$SiteProperties = DekiSiteProperties::getInstance();

			// get the site's custom css details
			$etag = $SiteProperties->getCustomCssEtag();
			if (!is_null($etag))
			{
				$uri = $SiteProperties->getCustomCssUri();
				$Remote->addResouce($uri, $etag);
			}
		}
		else if (!empty($wgLocalCssPath))
		{
			$Remote->addFile($IP . $wgLocalCssPath);
		}

		$Remote->addFile($IP . Skin::getTemplatePath() . '/_editor.css');
		$Remote->addFile($IP . Skin::getSkinPath() . '/_content.css');

		$cssFiles = array();
		DekiPlugin::executeHook(Hooks::EDITOR_STYLES, array(&$cssFiles));

		foreach ($cssFiles as $file)
		{
			$Remote->addFile($file);
		}
		
		return $Remote;
	}
	
	private static function sendCacheHeaders()
	{
		$expires = 60 * 60 * 24 * 365; // 365 days
		header('Pragma: public');
		header('Cache-Control: maxage=' . $expires . ', public');
		header('Expires: ' . gmdate('D, d M Y H:i:s', time() + $expires) . ' GMT');
	}

	/**
	 * Load editor
	 * 
	 * @return array - page content, editor javascript
	 */
	protected static function load()
	{
		global $wgTitle, $wgArticle, $wgOut;

		require_once('EditPage.php');

		$Request = DekiRequest::getInstance();

		$text = $Request->getVal('text');
		$pageId = $Request->getVal('pageId');
		$sectionId = $Request->getVal('sectionId');
		$redirect = $Request->getVal('redirect');

		$sectionId = (empty($sectionId)) ? null : $sectionId;
		$pageId = (empty($pageId)) ? 0 : (int) $pageId;

		$wgTitle = Title::newFromText($text);

		$Article = new Article($wgTitle);
		$Article->mRedirected = $redirect === 'no';
		$Article->loadContent('edit', $sectionId);

		if ($pageId > 0)
		{
			$wgArticle = $Article;
		}

		$editorScripts = array();
		$script = '';
		
		$lang = self::getLanguage($Article);

		if ($lang)
		{
			global $wgLanguageCode;
			$wgLanguageCode = $lang;
		}

		$hookResult = DekiPlugin::executeHook(Hooks::EDITOR_LOAD, array($Article, &$editorScripts, &$script));

		if ($lang)
		{
			$script .= "Deki.EditorLang = '" . strtolower($lang) . "';";
		}

		if ($Request->getVal('source') == 'true')
		{
			$script .= "Deki.EditorReadOnly = true;";
		}

		if ($Request->getVal('editor') == 'false')
		{
			$script .= "Deki.EditorWysiwyg = false;";
		}

		$script .= Skin::jsLangVars(array('GUI.Editor.alert-changes-made-without-saving'), false);

		$script = '<script type="text/javascript">' . $script . '</script>';

		$edit = new EditPage($Article);
		$edit->textbox1 = $Article->getContent(true);
		$edit->setSection($sectionId);

		// adds html to wgOut
		$edit->editForm('edit', false, true);

		$editorContent = array(
			'edittime' => $Article->getTimestamp(),
			'content' => $wgOut->getHTML(),
			'script' => $script,
			'scripts' => $editorScripts
		);

		return $editorContent;
	}
	
	/**
	 * Check if user has permissions to save the page
	 * 
	 * @param string $body
	 * @param string $message
	 * @param boolean $success 
	 */
	protected static function checkPermissions(&$body, &$message, &$success)
	{
		$Request = DekiRequest::getInstance();

		$pageId = $Request->getInt('pageId');
		$pageTitle = $Request->getVal('pageTitle');

		$permissions = array();
		$Title = ($pageId > 0) ? Title::newFromID($pageId) : Title::newFromText($pageTitle);

		if ($Title)
		{
			$Article = new Article($Title);
			$permissions = ($pageId > 0) ? $Article->getPermissions() : $Article->getParentPermissions();
		}

		if (is_array($permissions) && in_array('UPDATE', $permissions))
		{
			$success = true;
		}
		else
		{
			$success = false;
			$message = wfMsg('GUI.Editor.error.unable-to-save');
			$body = wfMsg('GUI.Editor.error.session-has-expired');
		}
	}

	/**
	 * Get timestamp of the page
	 *
	 * @param string $body
	 * @param string $message
	 * @param boolean $success
	 */
	protected static function pageTimestamp(&$body, &$message, &$success)
	{
		$Request = DekiRequest::getInstance();

		$pageId = $Request->getInt('pageId');
		$Article = Article::newFromId($Request->getInt('pageId'));

		if ($Article && $Article->getId() > 0 && $Article->userCanEdit())
		{
			$body = $Article->getTimestamp();
			$success = true;
		}
		else
		{
			$success = false;
		}
	}

	/**
	 * Get language of the page
	 * 
	 * @param object $Article
	 * @return string - language code
	 */
	protected static function getLanguage($Article)
	{
		global $wgLanguageCode;

		$lang = $Article->getLanguage();
		
		if (is_null($lang))
		{
			$lang = $wgLanguageCode;
		}

		return $lang;
	}
}

EditorPlugin::init();

endif;

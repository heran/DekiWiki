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

class CKEditorPlugin extends DekiPlugin
{
	const LANG_FORMATTER = 'page_editor_ckeditor_lang';
	
	private static $timestamp = '';
	
	/**
	 * Register hooks
	 */
	public static function init()
	{
		DekiPlugin::registerHook(Hooks::SKIN_OVERRIDE_VARIABLES, array(__CLASS__, 'skinHook'));
		DekiPlugin::registerHook(Hooks::MAIN_PROCESS_OUTPUT, array(__CLASS__, 'renderHook'));
		
		DekiPlugin::registerHook(Hooks::AJAX_FORMAT . self::LANG_FORMATTER, array(__CLASS__, 'langHook'));
		
		// Use priority to load necessary editor,
		// hook should reutrn HANDLED_HALT code to prevent loading of other editors.
		// Use 99 priority to load CKEditor as editor by default
		DekiPlugin::registerHook(Hooks::EDITOR_LOAD, array(__CLASS__, 'load'), 99);
		DekiPlugin::registerHook(Hooks::EDITOR_CONFIG, array(__CLASS__, 'config'), 99);
		DekiPlugin::registerHook(Hooks::EDITOR_STYLES, array(__CLASS__, 'styles'), 99);
		
		global $IP, $wgDekiPluginPath;

		$file = $IP . $wgDekiPluginPath . '/page_editor_ckeditor/ckeditor/mindtouch.js';
		self::$timestamp = dechex(filemtime($file));
	}

	/**
	 * Set CKEditor base path
	 *
	 * @param object $Template
	 */
	public static function skinHook(&$Template)
	{
		global $wgArticle, $wgDekiPluginPath;

		$ckeditorBasePath = $wgDekiPluginPath . '/page_editor_ckeditor/ckeditor/';
		$js = '<script type="text/javascript">CKEDITOR_BASEPATH = "' . $ckeditorBasePath . '";</script>' . "\n";

		if (isset($Template->data['javascript']))
		{
			$js .= $Template->data['javascript'];
		}

		$Template->set('javascript', $js);
	}

	/**
	 * Load CKEditor core
	 *
	 * @param object $wgOut
	 */
	public static function renderHook($wgOut)
	{
		global $wgArticle, $wgPreloadEditorCore, $wgUser;

		if (!$wgArticle || !$wgArticle->userCanEdit())
		{
			return;
		}

		$html = '<script type="text/javascript">' . "\n";

		$html .= "$(window).load(function() {\n";

		$Request = DekiRequest::getInstance();
		if (!$Request->has('cksource'))
		{
			if ($wgPreloadEditorCore)
			{
				$html .= "	CKEDITOR.mindtouch_status = 'unloaded';\n";
				$html .= "	CKEDITOR.on('loaded', function() {\n";
				$html .= "		jQuery.getScript(CKEDITOR.getUrl('mindtouch.js?tt=" . self::$timestamp . "'), function() { CKEDITOR.mindtouch_status = 'loaded' });\n";
				$html .= "	});\n";
				$html .= "	CKEDITOR.loadFullCore && CKEDITOR.loadFullCore();\n";
			}
		}
		else
		{
			$html .= "	CKEDITOR.mindtouch_status = 'loaded';\n";
			$html .= "	Deki.EditorCKSource = true;\n";
			$html .= "	CKEDITOR.loadFullCore && CKEDITOR.loadFullCore();\n";
		}

		$html .= "});\n";
		
		$html .= "Deki.EditorLangs = " . json_encode(self::getLangs()) . ";\n";
		$html .= "Deki.PageRevision = '" . $wgArticle->getRevisionCount() . "';\n";
		$html .= "Deki.EditorTimestamp = '" . self::$timestamp . "';\n";
		
		$html .= "</script>\n";

		$wgOut->addHeadHTML($html);
	}
	
	public static function langHook(&$body, &$message, &$success)
	{
		$body = self::getLangs();
		$success = true;
	}

	/**
	 * Load CKEditor
	 * 
	 * @param object $Article - Article object of editing page
	 * @param array $editorScripts - links to editor JavaScript files
	 * @param string $script - inline JavaScript code
	 * @return integer - result code
	 */
	public static function load($Article, &$editorScripts, &$script)
	{
		global $wgDekiPluginPath, $wgEditorToolbarSet, $wgPreloadEditorCore;

		if (!$wgPreloadEditorCore)
		{
			$editorScripts[] = $wgDekiPluginPath . '/page_editor_ckeditor/ckeditor/ckeditor.js?tt=' . self::$timestamp; // core of the editor
			$editorScripts[] = $wgDekiPluginPath . '/page_editor_ckeditor/ckeditor/mindtouch.js?tt=' . self::$timestamp; // mindtouch plugins
		}

		$script .= "Deki.EditorPath = '" . $wgDekiPluginPath . "/page_editor_ckeditor';";
		$script .= "Deki.EditorToolbarSet = '" . $wgEditorToolbarSet . "';";

		// Don't load other editors
		return self::HANDLED_HALT;
	}

	/**
	 * Load CKEditor styles for content
	 *
	 * @param array $cssFiles - links to editor css content files
	 * @return integer - result code
	 */
	public static function styles(&$cssFiles)
	{
		global $IP, $wgDekiPluginPath;

		$cssFiles[] = $IP . $wgDekiPluginPath . '/page_editor_ckeditor/ckeditor/contents.css';

		// Don't load styles from other editors
		return self::HANDLED_HALT;
	}

	/**
	 * Load CKEditor configuration
	 *
	 * @param array $jsFiles - links to editor config files
	 * @return integer - result code
	 */
	public static function config(&$jsFiles)
	{
		global $IP, $wgDekiPluginPath;

		$jsFiles[] = $IP . $wgDekiPluginPath . '/page_editor_ckeditor/config.js';

		// Don't load configs from other editors
		return self::HANDLED_HALT;
	}

	private static function getLangs()
	{
		$ns = 'redist.cke.';
		$ckeLangs = array(
			'atd.explain',
			'atd.ignore-all',
			'atd.ignore-always',
			'atd.ignore-suggestion',
			'atd.no-errors',
			'atd.no-suggestions',
			'atd.server-error',
			'atd.toolbar',
			'attach-image',
			'autosave.continue-editing',
			'autosave.discard-changes',
			'autosave.draft-exists',
			'autosave.draft-outdated',
			'autosave.edit-version',
			'autosave.local-save',
			'autosave.or',
			'autosave.timeago.day',
			'autosave.timeago.days',
			'autosave.timeago.hour',
			'autosave.timeago.hours',
			'autosave.timeago.just-now',
			'autosave.timeago.minute',
			'autosave.timeago.minutes',
			'autosave.timeago.month',
			'autosave.timeago.months',
			'autosave.timeago.seconds',
			'autosave.timeago.suffix-ago',
			'autosave.timeago.year',
			'autosave.timeago.years',
			'cancel.button',
			'cancel.confirm-cancel',
			'cancel.continue-editing',
			'cancel.discard-changes-title',
			'cancel.discard-changes',
			'definition-desc',
			'definition-list',
			'definition-term',
			'dsbar.col',
			'dsbar.line',
			'extensions.title',
			'extensions.toolbar',
			'menubuttons.insert',
			'menubuttons.view',
			'mindtouch.code',
			'mindtouch.comment',
			'mindtouch.css',
			'mindtouch.dekiscript',
			'mindtouch.format.tag_h1',
			'mindtouch.format.tag_hx',
			'mindtouch.hide-blocks',
			'mindtouch.jem',
			'mindtouch.wysiwyg',
			'mindtouchtemplates.button',
			'tableadvanced.bg-image',
			'tableadvanced.border-style',
			'tableadvanced.border-width',
			'tableadvanced.cell.update-column',
			'tableadvanced.cell.update-row',
			'tableadvanced.cell.update-selected',
			'tableadvanced.cell.update-table',
			'tableadvanced.collapsed',
			'tableadvanced.column-width',
			'tableadvanced.fixed',
			'tableadvanced.flexible',
			'tableadvanced.frame-all',
			'tableadvanced.frame-bottom',
			'tableadvanced.frame-left-hand',
			'tableadvanced.frame-no-sides',
			'tableadvanced.frame-right-hand',
			'tableadvanced.frame-right-left',
			'tableadvanced.frame-top-bottom',
			'tableadvanced.frame-top',
			'tableadvanced.frame',
			'tableadvanced.row.part-of',
			'tableadvanced.row.t-body',
			'tableadvanced.row.t-foot',
			'tableadvanced.row.t-head',
			'tableadvanced.row.title',
			'tableadvanced.row.update-all',
			'tableadvanced.row.update-even',
			'tableadvanced.row.update-odd',
			'tableadvanced.row.update-selected',
			'tableadvanced.rules-all',
			'tableadvanced.rules-cols',
			'tableadvanced.rules-groups',
			'tableadvanced.rules-no',
			'tableadvanced.rules-rows',
			'tableadvanced.rules',
			'tableconvert.other',
			'tableconvert.paragraphs',
			'tableconvert.semicolons',
			'tableconvert.separate-at',
			'tableconvert.tabs',
			'tableconvert.to-table',
			'tableconvert.to-text',
			'tablesort.alphanumeric',
			'tablesort.asc',
			'tablesort.body',
			'tablesort.by',
			'tablesort.column',
			'tablesort.date',
			'tablesort.desc',
			'tablesort.foot',
			'tablesort.head',
			'tablesort.menu',
			'tablesort.numeric',
			'tablesort.order',
			'tablesort.title',
			'tablesort.type',
			'transformations.label',
			'transformations.loading',
			'transformations.no-transformation',
			'transformations.panel-title',
			'transformations.panel-voice-label',
			'transformations.voice-label',
			'video.toolbar'
		);

		$langs = array();
		foreach ($ckeLangs as $key)
		{
			$value = wfMsg($ns . $key);
			$keys = explode('.', $key);

			$keyArray = &$langs;
			for ($i = 0, $count = count($keys) ; $i < $count ; $i++)
			{
				$words = explode('-', $keys[$i]);
				$subKey = $words[0];
				for ($j = 1 ; $j < count($words) ; $j++)
				{
					$subKey .= ucfirst($words[$j]);
				}

				if ($i < $count - 1)
				{
					if (!isset ($keyArray[$subKey]))
					{
						$keyArray[$subKey] = array();
					}
					$keyArray = &$keyArray[$subKey];
				}
				else
				{
					$keyArray[$subKey] = $value;
				}
			}
		}

		return $langs;
	}
}

CKEditorPlugin::init();

endif;

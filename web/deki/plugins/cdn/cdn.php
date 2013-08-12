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

// @Note: This plugin is still under development. Usage instructions: http://developer.mindtouch.com/User:kalida/CDN_Plugin

if (defined('MINDTOUCH_DEKI')) :
global $IP;
require_once($IP . '/includes/libraries/ui_handlers.php');

class MindTouchCDNPlugin extends DekiPlugin
{
	const CDN_PREFIX = 'cdn';
	const NAME_SEPARATOR = '_';
	
	/**
	 * Initialize the plugin and hooks into the application
	 */
	public static function load()
	{
		DekiPlugin::registerHook(Hooks::SKIN_CSS_PATH, array(__CLASS__, 'skinHook'));
		DekiPlugin::registerHook(Hooks::UI_PROCESS_CSS, array(__CLASS__, 'uiHook'));
	}

	/**
	 * Rewrite the css path to use the CDN, if enabled
	 * @param string $path - the original css path to load for the skin
	 */
	public static function skinHook(&$path)
	{
		global $wgCDN;
		
		if (isset($wgCDN))
		{
			// TODO (kalida): support passing in template and skin directories (to override using current skin settings)
			global $wgActiveSkin, $wgActiveTemplate, $wgCacheDirectory;
			
			$fileName = self::getSkinCssName($wgActiveTemplate, $wgActiveSkin);
			
			// only rewrite path is cached files have been generated for this version
			if (file_exists($wgCacheDirectory . '/' . $fileName))
			{
				$path = $wgCDN . Skin::getSkinPath() . '/' . $fileName;
			}
		}
	}
	
	public static function uiHook($CssHandler, $options)
	{
		// For now, require an explicit parameter to enable CDN support for a skin
		if (!is_array($options) || !isset($options['allowCDN']))
		{
			return;
		}
		
		global $wgCacheDirectory;
		$cacheFile = $CssHandler->getCache()->getCacheFile();
		
		$template = basename($CssHandler->getTemplateDirectory());
		$skin = basename($CssHandler->getSkinDirectory());
		$cdnFileName = self::getSkinCssName($template, $skin);
		
		$altCacheFile = $wgCacheDirectory . '/' . $cdnFileName;
		
		// copy to cache folder for manual setup
		if (!file_exists($altCacheFile))
		{
			copy($cacheFile, $altCacheFile);
		}
		
		// attempt to copy to required skin folder
		$skinFile = $CssHandler->getSkinDirectory() . '/' . $cdnFileName;
		if (!file_exists($skinFile))
		{
			copy($cacheFile, $skinFile);
		}
	}
	
	/**
	 * Get unique name for a resource (i.e. with version number, etc.)
	 */
	protected static function getName($name)
	{
		global $wgProductVersion;
		return self::CDN_PREFIX . self::NAME_SEPARATOR . wfGetConfig('version/text') . self::NAME_SEPARATOR . $name;
	}

	public static function getSkinCssName($template, $skin)
	{
		return self::getName($template . self::NAME_SEPARATOR . $skin . '.css');
	}
}

// initialize the plugin
MindTouchCDNPlugin::load();

endif;


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

/**
 * Plugin Configuration
 * 
 * @note guerrics: We have not fully baked the plugin configuration yet. In the meantime,
 * if you want to use this plugin simply copy the below lines into your LocalSettings.php
 * file and uncomment/modify as you see fit.
 */
/**
 * Enable/disable ssl page limitations
 * @type bool
 */
// $wgLimitSslPages = true;
/**
 * Currently you can only limit special pages. This restriction
 * might get lifted if more use cases are encountered or you can always
 * roll your own.
 * 
 * @type array<string>
 */
// $wgLimitSslPageList = array('UserPreferences','Preferences','UserLogin','UserPassword');
/**
 * /Plugin Configuration
 */

if (defined('MINDTOUCH_DEKI')) :

DekiPlugin::registerHook(Hooks::MAIN_PROCESS_TITLE, 'wfLimitSslPages');

/**
 * Limits the scope of SSL availability to specified pages. Ensures the caching
 * layer can function.
 *
 * @param WebRequest $Request
 * @param Title $Title
 */
function wfLimitSslPages(&$Request, &$Title)
{
	global $wgLimitSslPages, $wgLimitSslPageList;
	// do some variable sanitizing incase this feature has not been configured
	$wgLimitSslPages = isset($wgLimitSslPages) ? (bool)$wgLimitSslPages : false;
	$wgLimitSslPageList = isset($wgLimitSslPageList) ? (array)$wgLimitSslPageList : array();
	
	if (!isset($wgLimitSslPages) || $wgLimitSslPages == false)
	{
		// feature is not enabled
		return DekiPlugin::UNHANDLED;
	}
	
	// grab a copy and strtolower for comparing
	$sslPages = $wgLimitSslPageList;
	foreach ($sslPages as &$pageName)
	{
		$pageName = strtolower($pageName);
	}
	unset($pageName);
	
	// get the current SSL connection status
	$isSsl = DekiRequest::getInstance()->isSsl();
	// get the currently request page
	$pageName = strtolower($Title->getText());
	
	// perform checks to make sure we are only requesting the allowed pages via ssl
	if ($Title->isSpecialPage() && in_array($pageName, $sslPages))
	{
		// limited ssl page found
		if (!$isSsl)
		{
			// redirect to ssl
			global $wgOut;
			$url = $Title->getFullURL();
			// quick and dirty rewriting
			$url = 'https' . substr($url, 4); // https
			$wgOut->redirect($url);
			// redirect now
			$wgOut->output();
			
			return DekiPlugin::HANDLED_HALT;
		}
		
		return DekiPlugin::HANDLED;
	}
	else if ($isSsl)
	{
		// redirect to unsecure
		global $wgOut;
		$url = $Title->getFullURL();
		// quick and dirty rewriting
		$url = 'http' . substr($url, 5); // https
		$wgOut->redirect($url);
		// redirect now
		$wgOut->output();
		
		return DekiPlugin::HANDLED_HALT;
	}
	
	return DekiPlugin::HANDLED;
}

// /defined('MINDTOUCH_DEKI');
endif;

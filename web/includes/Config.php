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

/***
 * Gets all the settings from the API and stores them in memory as $wgInstanceConfig
 */
function wfLoadConfig()
{
	global $wgInstanceConfig;
	global $wgDekiSiteHeader, $wgDekiSiteId;
	
	$Result = DekiPlug::getInstance()->At('site', 'settings')->WithApiKey()->Get();
	if (!$Result->isSuccess()) 
	{
		/***
		 * bootstrapping; gotta load language.php here, since normally language is loaded after settings. 
		 * the downside is that determining localization for the error message here will be difficult
		 */
		
		global $IP;
		require_once( "$IP/languages/Language.php" );
		
		// plugins not yet loaded; fire error hook
		global $wgDekiPluginPath;
		require_once($IP . $wgDekiPluginPath . '/deki_plugin.php');
		DekiPlugin::loadSitePlugins();
		
		$result = DekiPlugin::executeHook(Hooks::ERROR_SITE_SETTINGS);
		if ($result != DekiPlugin::HANDLED_HALT) {
			include('skins/error-settings.php');
		}
		exit();
	}
	
	$wgInstanceConfig = $Result->getVal('body/config');
	
	// set the deki site header for this request, key/val pairs
	$wgDekiSiteHeader = array();
	$headers = explode('&', $Result->getHeader('X-Deki-Site'));
	// parse the DekiSite header
	foreach ($headers as $header)
	{
		list($key, $val) = explode('=', $header);
		$wgDekiSiteHeader[$key] = $val;
	}
	// grab the siteId
	if (isset($wgDekiSiteHeader['id']))
	{
		// id comes in wrapped in quotes
		$wgDekiSiteId = trim($wgDekiSiteHeader['id'], '"');
	}
	
	return true;
}

/***
 * Sets a site config value to the config document in memory - be sure to call wfSaveConfig() after!
 * Note: Setting $val to null will remove that value from the doc
 */
function wfSetConfig($key, $val = null) { 
	global $wgInstanceConfig;
	if ($wgInstanceConfig === false) {
		return;
	}
	if ($val === true) {
		$val = 'true';	
	}
	if ($val === false) {
		$val = 'false';
	}
	if (substr($key, 0, 2) == 'wg') {
		global $$key;
		$$key = $val;
		$xkey = wfGetMappedConfig($key);
	}
	else {
		$xkey = $key;
	}
	wfSetArrayVal($wgInstanceConfig, $xkey, $val);
	return $wgInstanceConfig;
}

/*** 
 * Writes the current $wgInstanceConfig to the API
 */
function wfSaveConfig() {
	global $wgInstanceConfig, $wgDekiPlug, $wgDekiApiKey;
	if ($wgInstanceConfig === false) {
		wfMessagePush('general', wfMsg('System.Error.site-settings-couldnt-be-loaded'));
		return false;
	}
	$r = $wgDekiPlug->At('site', 'settings')->With('apikey', $wgDekiApiKey);
	$r = $r->Put(array('config' => $wgInstanceConfig));
	return $r;
}

//get the xpath keys from the wgKeys
function wfGetMappedConfig($wgKey) {
	global $wgConfigMap;
	if (array_key_exists($wgKey, $wgConfigMap)) {
		return $wgConfigMap[$wgKey];
	}
	return null;
}

//get the wgKeys from xpath keys
function wfGetRMappedConfig($xkey) {
	global $wgConfigMap;
	if (($k = array_search($xkey, $wgConfigMap)) !== false) {
		return $wgConfigMap[$k];
	}
	return null;
}

/***
 * Loops through the current instance configurations and sets the global variables to work with the old $wgVariables
 */
function wfSetInstanceSettings() {
	global $wgConfigMap, $wgInstanceConfig;
	foreach ($wgConfigMap as $wgKey => $xkey) {
		$val = wfArrayVal($wgInstanceConfig, $xkey);
		if (!is_null($val)) {
			$val = wfGetConfigValue($val);
			global $$wgKey;
			switch ($val) {
				case 'true': 
					$$wgKey = true;
				break;
				case 'false': 
					$$wgKey = false;
				break;
				default: 
				$$wgKey = $val;
			}
		}
	}	
}

/***
 * if a key's been listed as a readonly/hidden, the data structure changes from a string to an array
 */
function wfGetConfigValue($value) {
	if (is_array($value) 
		&& !is_null(wfArrayVal($value, '#text')) 
		&& (!is_null(wfArrayVal($value, '@readonly')) || !is_null(wfArrayVal($value, '@hidden')))) {
		$value = wfArrayVal($value, '#text');
	}
	return $value;
}

/***
 * Returns a list of site configuration values from the API
 */
function wfGetConfig($key = null, $returnValue = null, $asString = false) {
	global $wgInstanceConfig;
	if (is_null($wgInstanceConfig)) {
		return $returnValue;
	}
	if (is_null($key)) {
		return $wgInstanceConfig;
	}
	$xkey = (substr($key, 0, 2) == 'wg') ? wfGetMappedConfig($key): $key;
	$return = wfArrayVal($wgInstanceConfig, $xkey); 
	
	$return = wfGetConfigValue($return);
	
	if (is_null($return)) {
		return $returnValue;	
	}
	if ($asString === false) {
		if ($return == 'true') {
			$return = true;
		}
		elseif ($return == 'false') {
			$return = false;	
		}
	}
	return $return;
}

//takes the array returned from dekihost and converts it into a flat array
function wfConfigKeys($config, $parent = '') {
	
	static $path, $keys;
	
	foreach ($config as $key => $val) {
		if (empty($parent)) {
			$path = $key;
		}
		else {
			$path = $parent.'/'.$key;
		}
		if (is_array($val)) {
			$keys = wfConfigKeys($val, $path);
		}
		else {
			$keys[$path] = $val;
		}
	}
	return $keys;
}
?>

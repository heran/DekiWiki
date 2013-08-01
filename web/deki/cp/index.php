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
 * Bootstrapping file
 */
require_once('includes/deki_control_panel_config.php');

// Allows configuration values to be overriden locally
@include('local_config.php');
if (!class_exists('Config', false))
{
	class Config extends DekiControlPanelConfig {}
}

if (!file_exists(Config::$DEKI_ROOT . '/LocalSettings.php') || filesize(Config::$DEKI_ROOT . '/LocalSettings.php') <= 0)
{
	header('Location: ../../config/index.php');
	exit();
}

// include libraries from deki web
define('MINDTOUCH_DEKI', true);
require_once(Config::$DEKI_ROOT . '/includes/Defines.php');
require_once(Config::$DEKI_ROOT . '/LocalSettings.php');
require_once(Config::$DEKI_ROOT . '/includes/Setup.php');

/**
 * @note guerrics: as long as we are including the entire front end, no need to bother
 * with including the common libraries. front end's include list is more up to date
 */
/*
// include core library files
require_once(Config::$DEKI_ROOT . '/core/deki_request.php');
require_once(Config::$DEKI_ROOT . '/core/deki_plug.php');
require_once(Config::$DEKI_ROOT . '/core/deki_result.php');
require_once(Config::$DEKI_ROOT . '/core/deki_form.php');

// objects represent api data
require_once(Config::$DEKI_ROOT . '/core/objects/i_deki_api_object.php');
require_once(Config::$DEKI_ROOT . '/core/objects/deki_user.php');
require_once(Config::$DEKI_ROOT . '/core/objects/deki_role.php');
require_once(Config::$DEKI_ROOT . '/core/objects/deki_group.php');
require_once(Config::$DEKI_ROOT . '/core/objects/deki_service.php');
require_once(Config::$DEKI_ROOT . '/core/objects/deki_auth_service.php');
require_once(Config::$DEKI_ROOT . '/core/objects/deki_extension.php');
require_once(Config::$DEKI_ROOT . '/core/objects/deki_ban.php');
*/

// deki control panel specific includes
require_once(Config::$APP_ROOT . '/includes/deki_mvc_request.php');
require_once(Config::$APP_ROOT . '/includes/deki_message.php');
require_once(Config::$APP_ROOT . '/includes/deki_mvc.php');
require_once(Config::$APP_ROOT . '/includes/deki_control_panel.php');
require_once(Config::$APP_ROOT . '/includes/deki_error.php');
require_once(Config::$APP_ROOT . '/includes/deki_helpers.php');

// (guerrics) handles loading plugins and calling hooks
require_once(Config::$DEKI_ROOT . $wgDekiPluginPath . '/deki_plugin.php');
// load plugins
DekiPlugin::loadSitePlugins();

// set the webroot for generating admin urls
$wgAdminRequest = DekiMvcRequest::getInstance(true);
$wgAdminRequest->setWebRoot(Config::WEB_ROOT);

// setup the plug object, needs to be different from frontend plug
$wgAdminPlug = DekiPlug::NewPlug($wgDreamServer);
$wgAdminPlug = $wgAdminPlug->AtRaw($wgDekiApi);
DekiPlug::SetInstance($wgAdminPlug);

// unset wgAdminPlug to discourage usage
unset($wgAdminPlug);

// stores a reference to the active controller for error trapping
$wgAdminController = null;


// determine if a file has been called
if (!defined('DEKI_ADMIN'))
{
	// default to the dashboard
	require_once('dashboard.php');
	exit();

	if (!Config::PRETTY_URLS)
	{
		DekiMvcRequest::error('404');
		exit();
	}
	
	// pretty urls are enabled, determine the file to include
}

<?php
/*
 * MindTouch Deki - enterprise collaboration and integration platform
 * Copyright (C) 2006-2008 MindTouch, Inc.
 * www.mindtouch.com  oss@mindtouch.com
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
require('./includes/base_config.php');
// Allows configuration values to be overriden locally
@include('config.php');
if (!class_exists('Config', false))
{
	class Config extends DekiBaseConfig {}
}
if (CONFIG::DEBUG)
{
	error_reporting(E_ALL);
}

// ui required dependencies to include without using Setup.php
require(Config::$MOBILE_ROOT . '/includes/dependencies.php');
require(Config::$MOBILE_ROOT . '/includes/dream_plug.php');
require(Config::$MOBILE_ROOT . '/includes/dom.php');

// include core library files
require(Config::$MOBILE_ROOT . '/includes/deki_request.php');
require(Config::$MOBILE_ROOT . '/includes/deki_message.php');
require(Config::$MOBILE_ROOT . '/includes/deki_mvc.php');
require(Config::$MOBILE_ROOT . '/includes/deki_error.php');
require(Config::$MOBILE_ROOT . '/includes/deki_plug.php');
require(Config::$MOBILE_ROOT . '/includes/deki_result.php');
require(Config::$MOBILE_ROOT . '/includes/deki_form.php');
require(Config::$MOBILE_ROOT . '/includes/deki_helpers.php');

// objects represent api data
require(Config::$MOBILE_ROOT . '/includes/objects/i_deki_api_object.php');
require(Config::$MOBILE_ROOT . '/includes/objects/deki_user.php');
require(Config::$MOBILE_ROOT . '/includes/objects/deki_role.php');
require(Config::$MOBILE_ROOT . '/includes/objects/deki_group.php');
require(Config::$MOBILE_ROOT . '/includes/objects/deki_service.php');
require(Config::$MOBILE_ROOT . '/includes/objects/deki_auth_service.php');
require(Config::$MOBILE_ROOT . '/includes/objects/deki_extension.php');

// mobile dekiincludes
require(Config::$MOBILE_ROOT . '/includes/objects/deki_title.php');
// need to set app => mobile for the mvc templates & views, fix me?
Config::$APP_ROOT = Config::$MOBILE_ROOT;

// set the webroot for generating admin urls
$wgAdminRequest = DekiRequest::getInstance();
$wgAdminRequest->setWebRoot(Config::WEB_ROOT);

// setup the global plug object, needs to be different from frontend plug
$wgAdminPlug = new DekiPlug(Config::DREAM_SERVER);
$wgAdminPlug = $wgAdminPlug->At(array(Config::DEKI_API));

// stores a reference to the active controller for error trapping
$wgAdminController = null;

if(!defined('DEKI_MOBILE'))
{
	require('page.php');
	exit();
}


?>

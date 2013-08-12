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
require_once('includes/deki_installer_config.php');

// Allows configuration values to be overriden locally
@include('local_config.php');
if (!class_exists('DekiMvcConfig', false))
{
	class DekiMvcConfig extends DekiInstallerConfig {}
}
// include core library files
require_once(DekiMvcConfig::$DEKI_ROOT . '/core/xarray.php');
require_once(DekiMvcConfig::$DEKI_ROOT . '/core/xuri.php');
require_once(DekiMvcConfig::$DEKI_ROOT . '/core/deki_request.php');
require_once(DekiMvcConfig::$DEKI_ROOT . '/core/http_plug.php');
require_once(DekiMvcConfig::$DEKI_ROOT . '/core/dream_plug.php');
require_once(DekiMvcConfig::$DEKI_ROOT . '/core/deki_plug.php');
require_once(DekiMvcConfig::$DEKI_ROOT . '/core/deki_result.php');
// message handler for DekiResult
require_once(DekiMvcConfig::$DEKI_ROOT . '/core/deki_form.php');
require_once(DekiMvcConfig::$DEKI_ROOT . '/core/deki_token.php');
require_once(DekiMvcConfig::$DEKI_ROOT . '/core/deki_mailer.php');

// product urls
require_once(DekiMvcConfig::$DEKI_ROOT . '/core/product_urls.php');

// deki control panel specific includes
require_once(DekiMvcConfig::$CP_ROOT . '/includes/deki_mvc_request.php');
require_once(DekiMvcConfig::$CP_ROOT . '/includes/deki_message.php');
require_once(DekiMvcConfig::$CP_ROOT . '/includes/deki_mvc.php');
require_once(DekiMvcConfig::$CP_ROOT . '/includes/deki_error.php');

// installer specific
require_once(DekiMvcConfig::$APP_ROOT . '/includes/deki_installer_environment.php');
require_once(DekiMvcConfig::$APP_ROOT . '/includes/deki_installer.php');
require_once(DekiMvcConfig::$APP_ROOT . '/includes/deki_installer_view.php');


// set the webroot for generating admin urls
$wgAdminRequest = DekiMvcRequest::getInstance();
$wgAdminRequest->setWebRoot(DekiMvcConfig::WEB_ROOT);

// determine if a file has been called
if (!defined('DEKI_MVC'))
{
	// default to the dashboard
	include('install.php');
}

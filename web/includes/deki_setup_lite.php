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
 * To use this file you need to include Defines.php & LocalSettings.php.
 * 
 * Optional functionality:
 * 		Config // define('DEKI_SETUP_CONFIG', true)
 * 
 * Unavailable functionality:
 *		Database Connection
 *		Localization
 *		Sessions
 * 		Plugins
 */

global $IP;


/**
 * Include new deki objects
 * We aren't using require_once for speed purposes
 * 
 * @author guerrics
 */
$CORE = $IP . '/deki/core/';
$OBJECTS = $IP . '/deki/core/objects/';
$INCLUDES = $IP . '/includes/';


// include core library files
require($CORE . 'xarray.php');
require($CORE . 'deki_request.php');
require($CORE . 'http_plug.php');
require($CORE . 'dream_plug.php');
require($CORE . 'deki_plug.php');
require($CORE . 'deki_result.php');
// message handler for DekiResult
require($INCLUDES . 'deki_message.php');
require($CORE . 'deki_form.php');
require($CORE . 'deki_token.php');

/*
 * Setup the plug object
 */
global $wgDreamServer, $wgDekiApi;

// configure the plug object for deki objects
$DekiPlug = DekiPlug::NewPlug($wgDreamServer);
$DekiPlug = $DekiPlug->AtRaw($wgDekiApi);
DekiPlug::setInstance($DekiPlug);

// unset DekiPlug to discourage usage
unset($DekiPlug);

// objects represent api data
require($OBJECTS . 'i_deki_api_object.php');
require($OBJECTS . 'deki_properties.php');
require($OBJECTS . 'deki_user_properties.php');
require($OBJECTS . 'wiki_user_compat.php');
require($OBJECTS . 'deki_user.php');
require($OBJECTS . 'deki_role.php');
require($OBJECTS . 'deki_group.php');
require($OBJECTS . 'deki_service.php');
require($OBJECTS . 'deki_auth_service.php');
require($OBJECTS . 'deki_extension.php');
require($OBJECTS . 'deki_ban.php');
require($OBJECTS . 'deki_page_alert.php');
require($OBJECTS . 'deki_page_properties.php');
require($OBJECTS . 'deki_tag.php');
require($OBJECTS . 'deki_site_properties.php');

// include extra libraries
require($INCLUDES . 'libraries/ui_handlers.php');
require($INCLUDES . 'GlobalFunctions.php');


/*
 * optional sections to setup, better way to handle this?
 */
if (defined('DEKI_SETUP_CONFIG'))
{
	require($INCLUDES . 'Config.php');
	// load site settings :(
	wfLoadConfig();
	wfSetInstanceSettings();
}


// cleanup
unset($CORE, $INCLUDES, $OBJECTS);

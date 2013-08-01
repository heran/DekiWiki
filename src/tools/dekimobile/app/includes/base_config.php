<?php
/*
 * MindTouch Deki - enterprise collaboration and integration platform
 * Copyright (C) 2006 MindTouch, Inc.
 * www.mindtouch.com  oss@mindtouch.com
 *
 * For community documentation and downloads visit www.opengarden.org;
 *  please review the licensing section.
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

// define('DEKI_ADMIN', true); // entry point variable only to be set in calling scripts

class DekiBaseConfig
{
	const DEBUG				= false;
	const PRETTY_URLS		= false;

	// file to use as the template layout. layout => /templates/layout.php
	const TEMPLATE_NAME		= 'template';
	// location of the admin directory as seen in the url => http://site[/webroot]
	const WEB_ROOT			= '/deki-cp';
	
	const DREAM_SERVER = 'http://localhost:8081';
	const DEKI_API = 'deki';

	// customization values
	// determines the number of results to display in the listing views
	const RESULTS_PER_PAGE	= 15;

	// api key
	static $API_KEY = '';
	// sets the root application folder
	static $APP_ROOT = '';
	// sets the deki root folder
	static $DEKI_ROOT = '';
	// sets the mask of levels to debug against
	static $DEBUG_REPORTING = E_ALL;
}

// below assumes the application is located in a subdirectory of web root
DekiBaseConfig::$APP_ROOT = dirname(dirname(__FILE__));
DekiBaseConfig::$DEKI_ROOT = dirname(DekiBaseConfig::$APP_ROOT);

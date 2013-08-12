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

define('MINDTOUCH_DEKI', true);
define("MEDIAWIKI_INSTALL", true);

class DekiInstallerConfig
{
	const DEBUG				= false;

	// file to use as the template layout. layout => /templates/layout.php
	const TEMPLATE_NAME		= 'template';
	const VIEWS_FOLDER 		= 'views';
	// location of the admin directory as seen in the url => http://site[/webroot]
	const WEB_ROOT			= '/config';
	
	const LICENSE_GENERATOR_URL = 'http://trial.mindtouch.com';
	
	// sets the root application folder
	static $APP_ROOT = '';
	// sets the deki root folder
	static $DEKI_ROOT = '';
	// sets the cp root folder
	static $CP_ROOT = '';
	// sets the mask of levels to debug against
	static $DEBUG_REPORTING = E_ALL;// E_ERROR;
}

// below assumes the application is located in a subdirectory of web root
DekiInstallerConfig::$APP_ROOT = dirname(dirname(__FILE__));
DekiInstallerConfig::$DEKI_ROOT = dirname(DekiInstallerConfig::$APP_ROOT) . '/deki';
DekiInstallerConfig::$CP_ROOT = DekiInstallerConfig::$DEKI_ROOT . '/cp';

// setup global include path (web root)
$IP = dirname(DekiInstallerConfig::$DEKI_ROOT);

// Define required versions of key components of stack
define('REQUIRED_PHP_VERSION', '5.0.0');
define('REQUIRED_MYSQL_VERSION', '5.0.0');
define('REQUIRED_APACHE_VERSION', '2.0.0');

// Used to generate LocalSettings
define('NEWLINE', "\n");

// Do not modify these file values
define('FILE_STARTUP_XML', 'mindtouch.deki.startup.xml');
define('FILE_STARTUP_XML_IN', 'mindtouch.deki.startup.xml.in');
define('FILE_LOCALSETTINGS', 'LocalSettings.php');
define('FILE_HOST_XML', 'mindtouch.host.conf');
define('FILE_HOST_XML_IN', 'mindtouch.host.conf.in');
define('FILE_HOST_WIN_BAT', 'mindtouch.host.bat');
define('FILE_HOST_WIN_BAT_IN', 'mindtouch.host.bat.in');
define('FILE_HOST_WIN_SERVICE_CONF', 'mindtouch.dream.startup.xml');
define('FILE_HOST_WIN_SERVICE_CONF_IN', 'mindtouch.dream.startup.xml.in');

// installation type flags
$wgIsVM = false;
$wgIsMSI = false;
$wgIsWAMP = false;

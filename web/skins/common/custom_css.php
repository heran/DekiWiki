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

// necessary for LocalSettings.php
define('MINDTOUCH_DEKI', true);
// chdir() will attempt to load LocalSettings.php magically;
// if this fails, you will need to explicitly set the path
chdir($_SERVER['DOCUMENT_ROOT']);
require_once('includes/Defines.php');
require_once('LocalSettings.php');
// we need to access the api, so include some libs for just that
define('DEKI_SETUP_CONFIG', true);
require_once('deki_setup_lite.php');

// create an instance of a remote etag cache
$Remote = new RemoteCacheHandler(EtagCache::TEXT_CSS, 'iso-8859-1');

// check if any custom css should be included by fetching the etag
$SiteProperties = DekiSiteProperties::getInstance();

// get the site's custom css details
$etag = $SiteProperties->getCustomCssEtag(); 
if (!is_null($etag))
{
	$uri = $SiteProperties->getCustomCssUri();
	$Remote->addResouce($uri, $etag);
}

// create the cache file
$Remote->process();

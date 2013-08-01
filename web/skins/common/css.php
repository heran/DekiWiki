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
require_once($IP . '/includes/libraries/ui_handlers.php');

// required for cacheable resource loading
require_once($IP . $wgDekiPluginPath . '/deki_plugin.php');
// load caceable resources
DekiPluginResource::loadSiteResources();

$CSS = new CssHandler(__FILE__);
// add css files from the skins/common/ directory
$CSS->addSkin('icons.css');
$CSS->addSkin('general.css');
$CSS->addSkin('templates/general.css');
$CSS->addSkin('_icons.css');
$CSS->addSkin('yui/button/assets/skins/sam/button-skin.css');
$CSS->addSkin('yui/container/assets/skins/sam/container.css');
$CSS->addSkin('popups/css/dialog.css');
$CSS->addSkin('jquery/thickbox/thickbox.css');
$CSS->addSkin('jquery/autocomplete/jquery.autocomplete.css');
$CSS->addSkin('messaging.css');
$CSS->addSkin('ckb/controls.css');
$CSS->addSkin('ckb/reports.css');
$CSS->addSkin('pagination.css');
$CSS->addSkin('2010/2010.css');

// add any css that plugins need to cache
$files = DekiPluginResource::getLoadedCss();
foreach ($files as $file)
{
	$CSS->addFile($file);
}

// create the cache file
$CSS->process();

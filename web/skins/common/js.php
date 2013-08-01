<?php
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

$Js = new JsHandler();

$Js->addYui('yahoo-dom-event');
$Js->addYui('animation');
$Js->addYui('connection');
$Js->addYui('element');
$Js->addYui('dragdrop');
$Js->addYui('json');
$Js->addYui('get');

if ($Js->canUpdate()) 
{
	$Js->addYui('container');
	$Js->addYui('button');
	$Js->addYui('mindtouch', 'dialog.js');
}


// These are JS files that are included for every request
$Js->addCommon('wikibits.js');					// general javascripting
$Js->addCommon('menu.js');						// our menus
$Js->addCommon('messaging.js');					// messaging to the user
$Js->addCommon('general.js');					// more general javascripting (TODO: consolidate with wikibits.js
$Js->addCommon('jquery/jquery.min.js');			// awesome javascript library
$Js->addCommon('jquery/jquery.plugins.js');
$Js->addCommon('Mindtouch.util.js'); 			// depends on jQuery, load after
$Js->addCommon('comments.js');
$Js->addCommon('jquery/thickbox/thickbox.js');
$Js->addCommon('jquery/autocomplete/jquery.autocomplete.js');
$Js->addCommon('jquery/jquery.editable.js');
$Js->addCommon('jquery/jquery.hoverIntent.min.js');
$Js->addCommon('pagebus.js');
$Js->addCommon('deki.js');
$Js->addCommon('quickpopup.js');				// lightweight popup

if ($Js->canUpdate())
{
	$Js->addCommon('dialogs.js');					// general javascripting
	$Js->addCommon('popups/dialog.js');				//handles our inline popup windows 
}

// add any javascript that plugins need to cache
$files = DekiPluginResource::getLoadedJavascript();
foreach ($files as $file)
{
	$Js->addFile($file);
}

// create the cache file
$Js->process();

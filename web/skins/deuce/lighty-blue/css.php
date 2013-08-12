<?php

// necessary for LocalSettings.php
define('MEDIAWIKI', true); 

// chdir() will attempt to load LocalSettings.php magically;
// if this fails, you will need to explicitly set the path
chdir($_SERVER['DOCUMENT_ROOT']);
require_once('includes/Defines.php');
require_once('LocalSettings.php');
require_once($IP . '/includes/libraries/ui_handlers.php');

// required because we want to use the CDN plugin
define('MINDTOUCH_DEKI', true);
require_once('includes/Setup.php');
require_once($IP . $wgDekiPluginPath . '/deki_plugin.php');
DekiPlugin::loadSitePlugins();

$CSS = new CssHandler(__FILE__);

// add some skin css files (located in the skin directory)
$CSS->addTemplate('../common/fonts.css');
$CSS->addTemplate('common.css');
$CSS->addSkin('styles.css');
$CSS->addSkin('_content.css');

// special page styles
$CSS->addTemplate('special.css');

// create the cache file
$CSS->process(array('allowCDN' => true));
?>